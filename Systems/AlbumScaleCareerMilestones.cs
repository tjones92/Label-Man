using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Economy audit findings B1 and B2 (SimTools/EconomyReleaseJudgementAudit-2026-10-20.md), off unless named.
/// --album-scale-career-milestones scores album chart runs on the album chart's own scale and makes a never-charted
/// single the worst momentum outcome.
///
/// <para><b>B1.</b> The album chart fires the same chart events as the Hot 100, and the career ladder, the recognition
/// milestones and the hit-cadence clock read those peaks on Hot 100 thresholds. The album chart is 40 slots until April
/// 1961, 50 until August 1963, then 150, then 200 (<see cref="ChartManager.GetAlbumChartSize"/>). So every 1960 LP
/// scored as a Top 40 hit, and from August 1963 half of charting LPs scored as flops that reset the Star streak. That
/// is chart depth, not performance. With the switch on, an album position is converted to its singles equivalent by
/// its percentile of the album chart's size that week: <c>ceil(position * 100 / size)</c>, clamped to 2..100, with #1
/// staying #1. The best equivalent across the run is the album's career peak, and every career read (chart entry,
/// Top 40, Top 10, #1, flop streak, momentum penalty, contract evidence, recognition gains and run sustain, and
/// through <see cref="SimulatedArtist.RegisterTop40Hit"/> the hit-cadence and senior-cooling clocks) takes that
/// peak in place of the raw one. Top 40 is the top 40% of the album chart (#16 of 40, #20 of 50, #60 of 150, #80 of
/// 200), Top 10 the top 10% (#4, #5, #15, #20) and a flop the bottom 40% (#25+, #31+, #91+, #121+). Albums and singles
/// still share one ladder: late-decade proven acts make 68-78% of their releases LPs, and a ladder that ignored
/// albums would freeze them.</para>
///
/// <para><b>Other readers.</b> The same career peak replaces the raw album peak in the label's hit credit
/// (<see cref="CompetitorManager"/>: label Top 40s and #1s, which feed label health, lifecycle momentum, signing buzz
/// and the player's contract outlook, and the charted-label set the consolidation gate reads); the landmark album
/// recognition read (<see cref="AlbumLegitimacyService"/>); the critical acclaim commercial score
/// (<see cref="ArtistCriticalAcclaimService"/>, which feeds that landmark read); and the evolution era tally
/// (<see cref="ArtistEvolutionService"/>, discography text). Before, an LP past #100 counted as uncharted in all four.</para>
///
/// <para><b>Album #1.</b> An album #1 counts as a full #1 toward Star (one #1) and Superstar (four #1s). The album chart
/// has exactly one #1 a week however deep it is, the same scarcity as the Hot 100's, and a #1 LP was a star-making
/// event in the period (Belafonte, the Kingston Trio, the Beatles). What the conversion removes is the depth bias
/// below #1, not the top slot.</para>
///
/// <para><b>B2.</b> <see cref="SimulatedArtist.CompleteChartRun"/> penalised momentum only for peaks over 40, so a
/// single that never charted (peak 0) cost nothing while a #75 cost -0.10. With the switch on, a never-charted single
/// takes <see cref="NeverChartedSinglePenalty"/>, the schedule's worst bucket. A never-charted album is left
/// unpenalised: at 40-50 slots most LPs never chart, so its absence from the chart says little about the act's heat.
/// It still counts as a flop for the streak, as before.</para>
///
/// Label-blind (the player's acts too), draws no RNG. Off: <see cref="CareerPeak"/> is the raw peak, nothing is
/// written, and no penalty is added.
/// </summary>
public static class AlbumScaleCareerMilestones {
	public const string Switch = "--album-scale-career-milestones";
	public const float NeverChartedSinglePenalty = -0.15f;

	private static bool configured;
	private static bool enabled;
	public static bool Enabled => enabled;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		enabled = (arguments ?? Array.Empty<string>()).Contains(Switch);
		configured = true;
	}

	internal static void SetForChecks(bool on) { configured = true; enabled = on; }

	/// <summary>
	/// The singles-chart equivalent of an album chart position: its percentile of the chart's size, on a 100-slot
	/// scale. #1 stays #1 and nothing else reaches it; a 100-slot chart is the identity.
	/// </summary>
	public static int SinglesEquivalent(int position, int chartSize) {
		if (position <= 0) return 0;
		if (position == 1) return 1;
		if (chartSize <= 0) return position;
		int scaled = (int)Math.Ceiling(position * 100.0 / chartSize);
		return Math.Clamp(scaled, 2, 100);
	}

	private static bool IsAlbum(RecordRuntimeData record) => record?.baseRecord?.format == ReleaseFormat.Album;

	private static int CurrentAlbumChartSize() {
		ChartManager chart = ChartManager.Instance;
		GameDate? date = TimeManager.Instance?.CurrentDate;
		return chart != null && date.HasValue ? chart.GetAlbumChartSize(date.Value) : 100;
	}

	/// <summary>
	/// Called on every chart update before the milestone credits. On, for an album, folds this week's position on
	/// this week's chart into the run's best singles equivalent. Returns the peak the career reads.
	/// </summary>
	public static int ObserveCareerPeak(RecordRuntimeData record) => ObserveCareerPeak(record, CurrentAlbumChartSize());

	internal static int ObserveCareerPeak(RecordRuntimeData record, int albumChartSize) {
		if (!enabled || !IsAlbum(record)) return record?.peakPosition ?? 0;
		// The prewarm replay fires once per record after it may have left the chart; its peak is the best it saw.
		int position = record.currentPosition > 0 ? record.currentPosition : record.peakPosition;
		int scaled = SinglesEquivalent(position, albumChartSize);
		if (scaled > 0 && (record.albumCareerPeak == 0 || scaled < record.albumCareerPeak)) record.albumCareerPeak = scaled;
		return record.albumCareerPeak;
	}

	/// <summary>The peak a completed run is scored on. Off, or for a single, the raw peak.</summary>
	public static int CareerPeak(RecordRuntimeData record) => CareerPeak(record, CurrentAlbumChartSize());

	internal static int CareerPeak(RecordRuntimeData record, int albumChartSize) {
		if (record == null) return 0;
		if (!enabled || !IsAlbum(record)) return record.peakPosition;
		if (record.albumCareerPeak > 0) return record.albumCareerPeak;
		// Charted but never observed (a save from before the switch): score the raw peak on today's chart.
		return SinglesEquivalent(record.peakPosition, albumChartSize);
	}

	/// <summary>True when a never-charted run should take the worst momentum bucket: on, and a single.</summary>
	public static bool PenalizesNeverCharted(RecordRuntimeData record) =>
		enabled && record?.baseRecord != null && record.baseRecord.format == ReleaseFormat.Single;
}
