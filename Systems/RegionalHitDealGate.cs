using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The default P&amp;D pull-deal evidence gate as a regional hit (SimTools/SmallLabelAttritionAndChartMethod.md §7).
/// --no-regional-hit-deal-gate retains the old comparison.
///
/// The default gate asks whether any record ever reached <c>peakBreakoutScore &gt;= .20</c> in any region. That bar
/// is LocalTraction's entry, and 74% of 1960 Small releases cross it on their launch ramp alone (velocity and
/// sustained growth off a 150-copy floor, plus the static quality and genre-fit terms), usually in release week 3.
/// Because the peak is a running maximum the evidence never expires, so the monthly 40% roll and the acceptance
/// roll only delay a deal: 66% of starting Small labels signed one in 1960.
///
/// Under the switch a record-region is deal evidence only while it is a regional hit: its current breakoutScore is
/// at the RegionalBreakout stage's .40 AND its volume term says it is actually selling (<see cref="VolumeBar"/>
/// is ~1,800 copies a week of demand at region scale). The qualifying week is stamped as a record age
/// (<see cref="RegionalRecordData.dealEvidenceAge"/>) and lapses after <see cref="RecentWeeks"/>, one monthly
/// check interval, so a breakout that peaks between two checks still counts and a label cannot spend a year
/// retrying on one old ramp. This is the deal the history describes: Tamla leased "Come to Me" to UA because it was
/// selling in Detroit that month. The discovery basin keeps its .20 LocalTraction entry; only the deal bar moves.
/// </summary>
public static class RegionalHitDealGate {
	public const string Switch = "--regional-hit-deal-gate";
	private static bool configured, enabled = true;
	public static bool Enabled => enabled;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var args = (arguments ?? Array.Empty<string>()).ToArray();
		enabled = !args.Contains("--no-regional-hit-deal-gate");
		if (args.Contains(Switch) && !enabled) throw new ArgumentException("Conflicting regional-hit deal switches.");
		configured = true;
	}

	internal static void SetEnabledForChecks(bool value) { configured = true; enabled = value; }

	// Model choices sized offline on cleanup1-2002-52 (starting-Small labels, 1960 check weeks): score .40 with
	// volume .50 is met by 33% of labels at a check (89 of 230 inside the four-week window), against 81% for the
	// running .20 peak. ScoreBar is the RegionalBreakout stage's own score; VolumeBar is the §7 proposal.
	public const float ScoreBar = 0.40f;
	public const float VolumeBar = 0.50f;
	public const int RecentWeeks = 4;

	public static bool MeetsBar(float breakoutScore, float volumeInput) =>
		breakoutScore >= ScoreBar && volumeInput >= VolumeBar;

	/// <summary>Stamps this week as qualifying for the region. Called from the breakout pass after the score update.</summary>
	public static void Observe(RegionalRecordData data, float volumeInput, int weeksSinceRelease) {
		// WholesaleSpreadGate reads the same stamp for its neighbour hop; the deal gate only reads it while on.
		if (!(enabled || WholesaleSpreadGate.Enabled) || data == null) return;
		if (MeetsBar(data.breakoutScore, volumeInput)) data.dealEvidenceAge = weeksSinceRelease;
	}

	/// <summary>Whether the region met the bar within the last <see cref="RecentWeeks"/> weeks of the record's life.</summary>
	public static bool IsRecent(RegionalRecordData data, int weeksSinceRelease) =>
		data != null && data.dealEvidenceAge >= 0 && weeksSinceRelease - data.dealEvidenceAge < RecentWeeks;
}
