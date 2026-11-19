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
	/// <summary>Diagnostic ablation: <c>=b1</c> runs only the album scale, <c>=b2</c> only the never-charted penalty,
	/// <c>=placebo</c> only a never-charted penalty of <see cref="PlaceboPenalty"/> (the same code path at a size that
	/// cannot matter: it measures how far the run moves on divergence alone). Requires the main switch; without it
	/// both halves run.</summary>
	public const string PartPrefix = "--album-scale-career-milestones-part=";
	public const float NeverChartedSinglePenalty = -0.15f;
	public const float PlaceboPenalty = -0.001f;
	private static float neverChartedPenaltySize = NeverChartedSinglePenalty;
	public static float NeverChartedPenaltySize => neverChartedPenaltySize;

	private static bool configured;
	private static bool enabled;
	private static bool albumScale;
	private static bool neverChartedPenalty;
	public static bool Enabled => enabled;

	// ---- --album-career-importance (LpGenreAllocationFollowup.md §62) ---------------------------------------------
	// The album chart mattered less to an act's stardom in 1960 than in 1969. With this switch the ACT's career reads
	// (milestone latches, run completion, recognition) take an album's singles equivalent divided by the year's
	// importance w: ceil(equivalent / w). At w = 1 that is the plain equivalent; at w 0.55 an LP needs to reach the top
	// 22% of the album chart to count as a Top 40 hit and an album #1 reads as #2. The downside is never harsher than
	// the plain equivalent: an LP that loses its hit credit to the weight reads as #41 (the mildest non-hit bucket).
	// Label hit credit, landmark, acclaim and evolution keep the plain equivalent. Keyframes YEAR:w, linear between,
	// flat outside. The default is the author's LP unit-share target (30/35/40/50/55% in 1960/63/65/67/69) over 1969's.
	public const string ImportanceSwitch = "--album-career-importance";
	public static readonly (int Year, float Weight)[] DefaultImportance =
		{ (1960, .55f), (1963, .64f), (1965, .73f), (1967, .91f), (1969, 1f) };
	private static (int Year, float Weight)[] importance;
	public static bool ImportanceEnabled => importance != null;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		string[] args = (arguments ?? Array.Empty<string>()).ToArray();
		bool on = args.Contains(Switch) || LabelCorporateModel.FullBundleRequested(args);
		string part = args.FirstOrDefault(a => a.StartsWith(PartPrefix, StringComparison.Ordinal))?.Substring(PartPrefix.Length);
		if (part != null && !on) throw new ArgumentException(PartPrefix + " requires " + Switch + ".");
		if (part != null && part != "b1" && part != "b2" && part != "placebo")
			throw new ArgumentException(PartPrefix + " takes b1, b2 or placebo.");
		Set(on, on && part is null or "b1", on && part != "b1");
		neverChartedPenaltySize = part == "placebo" ? PlaceboPenalty : NeverChartedSinglePenalty;
		string imp = args.FirstOrDefault(a => a == ImportanceSwitch || a.StartsWith(ImportanceSwitch + "=", StringComparison.Ordinal));
		importance = imp == null ? null : imp == ImportanceSwitch ? DefaultImportance : ParseImportance(imp.Substring(ImportanceSwitch.Length + 1));
		if (importance != null && !albumScale)
			throw new ArgumentException(ImportanceSwitch + " requires " + Switch + " with its album scale (not part b2).");
		configured = true;
	}

	private static (int Year, float Weight)[] ParseImportance(string spec) {
		var keys = new List<(int Year, float Weight)>();
		foreach (string part in spec.Split(',')) {
			string[] kv = part.Split(':');
			if (kv.Length != 2 || !int.TryParse(kv[0], out int year)
				|| !float.TryParse(kv[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float w)
				|| w <= 0f || w > 1f || (keys.Count > 0 && year <= keys[^1].Year))
				throw new ArgumentException(ImportanceSwitch + "=YEAR:W,... takes rising years and weights in (0, 1]; got '" + spec + "'.");
			keys.Add((year, w));
		}
		if (keys.Count == 0) throw new ArgumentException(ImportanceSwitch + " needs at least one YEAR:W keyframe.");
		return keys.ToArray();
	}

	private static void Set(bool on, bool scale, bool penalty) { enabled = on; albumScale = scale; neverChartedPenalty = penalty; }

	internal static void SetForChecks(bool on, (int Year, float Weight)[] importanceKeys = null) {
		configured = true; Set(on, on, on); neverChartedPenaltySize = NeverChartedSinglePenalty; importance = importanceKeys;
	}

	/// <summary>The album chart's weight in an act's career in <paramref name="year"/>: 1 when the switch is off.</summary>
	public static float Importance(int year) {
		if (importance == null) return 1f;
		if (year <= importance[0].Year) return importance[0].Weight;
		for (int i = 1; i < importance.Length; i++)
			if (year <= importance[i].Year) {
				var (y0, w0) = importance[i - 1]; var (y1, w1) = importance[i];
				return w0 + (w1 - w0) * (year - y0) / (float)(y1 - y0);
			}
		return importance[^1].Weight;
	}

	/// <summary>An album's singles equivalent as the act's career reads it at importance <paramref name="weight"/>:
	/// ceil(equivalent / w) while that is still a Top 40 hit, else the plain equivalent but no better than #41.</summary>
	public static int WeightedActPeak(int equivalent, float weight) {
		if (equivalent <= 0 || weight >= 1f) return equivalent;
		int hit = (int)Math.Ceiling(equivalent / (double)weight - 1e-9);
		return hit <= 40 ? hit : Math.Max(equivalent, 41);
	}

	private static int CurrentYear() => TimeManager.Instance?.CurrentDate.year ?? 1960;

	private static int ActWeighted(RecordRuntimeData record, int peak, int year) =>
		importance == null || !albumScale || !IsAlbum(record) ? peak : WeightedActPeak(peak, Importance(year));

	/// <summary>ObserveCareerPeak as the act's career reads it (milestone latches).</summary>
	public static int ObserveActCareerPeak(RecordRuntimeData record) => ActWeighted(record, ObserveCareerPeak(record), CurrentYear());
	internal static int ObserveActCareerPeak(RecordRuntimeData record, int albumChartSize, int year) =>
		ActWeighted(record, ObserveCareerPeak(record, albumChartSize), year);

	/// <summary>CareerPeak as the act's career reads it (run completion, recognition run credit).</summary>
	public static int ActCareerPeak(RecordRuntimeData record) => ActWeighted(record, CareerPeak(record), CurrentYear());
	internal static int ActCareerPeak(RecordRuntimeData record, int albumChartSize, int year) =>
		ActWeighted(record, CareerPeak(record, albumChartSize), year);

	/// <summary>The factor on an album run's sustain/breadth recognition credit: the year's importance; 1 otherwise.</summary>
	public static float ActCreditScale(RecordRuntimeData record) =>
		importance == null || !albumScale || !IsAlbum(record) ? 1f : Importance(CurrentYear());

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
		if (!albumScale || !IsAlbum(record)) return record?.peakPosition ?? 0;
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
		if (!albumScale || !IsAlbum(record)) return record.peakPosition;
		if (record.albumCareerPeak > 0) return record.albumCareerPeak;
		// Charted but never observed (a save from before the switch): score the raw peak on today's chart.
		return SinglesEquivalent(record.peakPosition, albumChartSize);
	}

	/// <summary>True when a never-charted run should take the worst momentum bucket: on, and a single.</summary>
	public static bool PenalizesNeverCharted(RecordRuntimeData record) =>
		neverChartedPenalty && record?.baseRecord != null && record.baseRecord.format == ReleaseFormat.Single;
}
