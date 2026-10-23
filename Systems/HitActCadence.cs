using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Calibration probe (SimTools/HistoricalLabelTierBalanceResearch.md §17x-§17y), off unless named.
///
/// History's hit acts released a single about every quarter: 2.19 Hot 100 debuts per hit act-year, a 13-week median
/// gap, and 9.4% of their charting weeks with two of their records on the chart. The sim's hit act gets one share
/// of an almost even release lottery over a 30-act roster (1.23 singles a year, a 27-week gap). Its album projects
/// use the same rolls, and 35% of hit acts are let go within a year of the hit. So the sim's chart holds 645 acts a
/// year, each charting briefly, against history's 386.
///
/// <list type="bullet">
/// <item><c>--hit-act-cadence</c>, the follow-up lane: when the label's weekly release roll fires, an eligible roster
/// act with a Top 40 entry in the last <see cref="HotWindowWeeks"/> weeks that has gone <see cref="DueWeeks"/> weeks
/// since its last release takes the roll ahead of the lottery, and the release is a single. The label's capacity
/// (<c>releasesPerMonth</c>) is unchanged, so the hit act's extra singles come out of the rest of the roster's rolls.
/// It comes with the same-act radio split: label-push and recognition radio lift share the act's charting singles,
/// and predecessors' radio heat is cut by <see cref="PredecessorRadioCut"/>. Launch awareness stays whole, matching
/// the working bundle's mtn5-pair control. <c>--hit-act-awareness-split</c> explicitly opts into also sharing
/// artist/campaign launch awareness, preserving the basic discovery floor and physical stock.</item>
/// <item><c>--hit-act-retention</c>: a label re-signs any act with a Top 40 under the current contract. No act is let
/// go within <see cref="RetentionWeeks"/> weeks of its last Top 40 (the lane's own hot window), whether by the label's review or by the career
/// ladder's own fall to Dropped.</item>
/// </list>
/// Label-blind. The lane and retention are AI label decisions, so the player's own releases and renewals are never
/// touched; the split is market-side and applies to every single, the player's included. Off: every hook is the
/// identity and draws no RNG.
/// </summary>
public static class HitActCadence {
	public const string Switch = "--hit-act-cadence";
	public const string RetentionSwitch = "--hit-act-retention";
	public const string AwarenessSwitch = "--hit-act-awareness-split";
	public const int HotWindowWeeks = 52;
	public const int DueWeeks = 13;
	public const int RetentionWeeks = 52;
	public const float PredecessorRadioCut = 0.10f;
	/// <summary>An unset clock; seeded on first read from the act's public history.</summary>
	public const int Unset = int.MinValue;

	private static bool configured;
	private static bool lane;
	private static bool retention;
	private static bool awarenessSplit;
	public static bool LaneEnabled => lane;
	public static bool SplitEnabled => lane;
	public static bool AwarenessSplitEnabled => lane && awarenessSplit;
	public static bool RetentionEnabled => retention;
	public static bool AnyEnabled => lane || retention;
	/// <summary>The hot clock is also read by <see cref="KnownActAirplay"/>, so it is written while either probe is on.</summary>
	public static bool ClockEnabled => AnyEnabled || KnownActAirplay.Enabled;

	/// <summary>True when the last <see cref="AILabel.GetArtistForRelease"/> call was decided by the lane.</summary>
	public static bool LastPickWasLane { get; private set; }
	public static int LanePicks { get; private set; }
	public static int LaneAlbumOverrides { get; private set; }
	public static int SplitLaunches { get; private set; }
	public static int RetentionRenewals { get; private set; }
	public static int RetentionProtectedReviews { get; private set; }

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		string[] args = (arguments ?? Array.Empty<string>()).ToArray();
		bool programme = args.Contains(LabelReleaseProgramme.AllocationSwitch) || args.Contains(LabelReleaseProgramme.CapacitySwitch);
		lane = programme || args.Contains(Switch);
		retention = programme || args.Contains(RetentionSwitch);
		awarenessSplit = args.Contains(AwarenessSwitch);
		configured = true;
	}

	internal static void SetForChecks(bool laneOn, bool retentionOn, bool? awarenessOn = null) {
		configured = true; lane = laneOn; retention = retentionOn;
		if (awarenessOn.HasValue) awarenessSplit = awarenessOn.Value;
		LastPickWasLane = false; LanePicks = LaneAlbumOverrides = SplitLaunches = RetentionRenewals = RetentionProtectedReviews = 0;
	}

	private static int CurrentWeek => ChartManager.Instance?.GetCurrentChartWeek() ?? 0;

	// ---- clocks, written only while a switch is on --------------------------------------------------

	public static void OnTop40(SimulatedArtist artist) { if (ClockEnabled && artist != null) artist.hitCadenceLastTop40Week = CurrentWeek; }
	internal static void OnTop40(SimulatedArtist artist, int week) { if (ClockEnabled && artist != null) artist.hitCadenceLastTop40Week = week; }

	/// <summary>
	/// Weeks since the act's last in-run Top 40. An opening career's last pre-1960 Top 40 is placed mid-year
	/// (1959 -> 26 weeks before the run); an act with none has never been hot, read as a very large gap.
	/// </summary>
	public static int WeeksSinceTop40(SimulatedArtist artist, int week) {
		if (artist.hitCadenceLastTop40Week == Unset) artist.hitCadenceLastTop40Week = SeedWeek(artist);
		return artist.hitCadenceLastTop40Week == Unset ? int.MaxValue : Math.Max(0, week - artist.hitCadenceLastTop40Week);
	}

	private static int SeedWeek(SimulatedArtist artist) {
		int lastYear = artist.publicHistory?.Where(e => e.peakPosition > 0 && e.peakPosition <= 40).Select(e => e.year)
			.DefaultIfEmpty(0).Max() ?? 0;
		return lastYear > 0 ? (lastYear - 1960) * 52 + 26 : Unset;
	}

	// ---- the follow-up lane -----------------------------------------------------------------------

	public static bool IsDue(SimulatedArtist artist, int week) =>
		artist != null && artist.weeksSinceLastRelease >= DueWeeks && WeeksSinceTop40(artist, week) <= HotWindowWeeks;

	/// <summary>
	/// The lane's pick from the label's release-eligible acts: the due act that has waited longest (ties by artist id),
	/// or null to fall through to the lottery. Draws no RNG.
	/// </summary>
	public static SimulatedArtist PickDue(IEnumerable<SimulatedArtist> eligible) => PickDue(eligible, CurrentWeek);

	internal static SimulatedArtist PickDue(IEnumerable<SimulatedArtist> eligible, int week) {
		SimulatedArtist best = null;
		foreach (SimulatedArtist artist in eligible) {
			if (!IsDue(artist, week)) continue;
			if (best == null || artist.weeksSinceLastRelease > best.weeksSinceLastRelease ||
				(artist.weeksSinceLastRelease == best.weeksSinceLastRelease && string.CompareOrdinal(artist.artistId, best.artistId) < 0))
				best = artist;
		}
		return best;
	}

	public static void MarkPick(bool byLane) {
		LastPickWasLane = byLane;
		if (byLane) LanePicks++;
	}

	/// <summary>A lane release is a single: the act's album, if the format decision wanted one, waits for a later roll.</summary>
	public static bool ShouldForceSingle(bool albumChosen) {
		if (!lane || !LastPickWasLane || !albumChosen) return false;
		LaneAlbumOverrides++;
		return true;
	}

	// ---- the same-act split -----------------------------------------------------------------------

	/// <summary>The act's other singles now on the Hot 100 (excluding the record being launched).</summary>
	public static List<RecordRuntimeData> ChartingPredecessors(string artistId, string launchingRecordId) {
		if (!lane || string.IsNullOrEmpty(artistId) || ChartManager.Instance == null) return new List<RecordRuntimeData>();
		return FilterPredecessors(ChartManager.Instance.GetCurrentChart(), artistId, launchingRecordId);
	}

	internal static List<RecordRuntimeData> FilterPredecessors(IEnumerable<RecordRuntimeData> chart, string artistId, string launchingRecordId) {
		var list = new List<RecordRuntimeData>();
		foreach (RecordRuntimeData r in chart)
			if (r?.baseRecord != null && r.baseRecord.artistId == artistId && r.baseRecord.recordId != launchingRecordId &&
				r.baseRecord.format == ReleaseFormat.Single && r.currentPosition is > 0 and <= 100) list.Add(r);
		return list;
	}

	/// <summary>The new single's share of the act's fame and push: 1 / (1 + charting predecessors). 1 when off.</summary>
	public static float LaunchShare(int predecessors) => !lane || predecessors <= 0 ? 1f : 1f / (1 + predecessors);

	/// <summary>Share the artist and campaign awareness, preserving the single's ordinary discovery floor.</summary>
	public static float LaunchAwareness(float discovery, float artist, float campaign, float label, float share) =>
		Mathf.Clamp(discovery + (artist + campaign + label) * share, 0f, 1f);

	public static void CutPredecessors(List<RecordRuntimeData> predecessors) {
		if (!lane || predecessors == null || predecessors.Count == 0) return;
		SplitLaunches++;
		foreach (RecordRuntimeData r in predecessors) r.radioHeat = Mathf.Clamp(r.radioHeat * (1f - PredecessorRadioCut), 0f, 1f);
	}

	/// <summary>A cumulative console line at each year end while a switch is on (the console is not a compared CSV).</summary>
	public static void ReportForWeek(int week) {
		if (!AnyEnabled || week <= 0 || week % 52 != 0) return;
		GD.Print($"HIT_ACT_CADENCE week={week} lanePicks={LanePicks} laneAlbumOverrides={LaneAlbumOverrides} " +
			$"splitLaunches={SplitLaunches} retentionRenewals={RetentionRenewals} retentionProtectedReviews={RetentionProtectedReviews}");
	}

	// ---- retention --------------------------------------------------------------------------------

	/// <summary>True when retention renews the act outright (any Top 40 under the current contract).</summary>
	public static bool ShouldRenew(SimulatedArtist artist) {
		if (!retention || artist == null || artist.contractTop40Hits <= 0) return false;
		RetentionRenewals++;
		return true;
	}

	/// <summary>True when retention protects the act from a performance drop (a Top 40 within the last year: the population the lane treats as hot).</summary>
	public static bool ProtectsFromDrop(SimulatedArtist artist) => ProtectsFromDrop(artist, CurrentWeek);

	internal static bool ProtectsFromDrop(SimulatedArtist artist, int week) {
		if (!retention || artist == null || WeeksSinceTop40(artist, week) > RetentionWeeks) return false;
		RetentionProtectedReviews++;
		return true;
	}
}
