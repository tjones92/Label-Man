using System;
using System.Linq;

/// <summary>
/// --album-career-credit-off (diagnostic probe; LpGenreAllocationFollowup.md §61). The album chart fires the same chart
/// events as the Hot 100, and <see cref="ArtistManager"/> credits them to the act's career: chart entry, Top 40, Top 10
/// and #1 latches (career ladder, hit-cadence and senior-cooling clocks, contract evidence) and the recognition milestone
/// and run-complete gains. With the probe on, an LP's chart run credits none of these to the act; singles are unchanged,
/// and so is every other album-chart reader (label hit credit, landmark, acclaim, the genre-market run log). It measures
/// how much of a result runs through LPs climbing the singles career ladder. No draws. Off: not read.
/// </summary>
public static class AlbumCareerCreditProbe {
	public const string Switch = "--album-career-credit-off";
	private static bool? enabled;
	public static bool Enabled => enabled ??= SimulationArguments.Get().Contains(Switch);
	internal static void SetForChecks(bool? value) => enabled = value;

	/// <summary>True when the probe withholds this record's chart run from the act's career.</summary>
	public static bool Withholds(RecordRuntimeData record) =>
		Enabled && record?.baseRecord?.format == ReleaseFormat.Album;
}
