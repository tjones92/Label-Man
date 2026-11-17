using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>
/// Calibration probe (LpGenreAllocationFollowup.md §52), off unless named.
/// --earned-head-capacity=R lets a non-Major label earn singles slate capacity from its own chart record:
/// <c>singles per month = max(envelope, min(Major envelope, R x HotRecords52 / 12))</c>, where HotRecords52 is the label's
/// records that charted on the Hot 100 within the last 52 weeks (the promotion gate's own count).
///
/// Under --historical-majors the 1961 second rank already exists and already has near-Major reach (6-7 regions,
/// owned reach ~.75), but it issues 17-24 singles a year against history's 65-130 for MGM, ABC-Paramount, United
/// Artists, Warner Bros., Dot and Liberty (LabelReleaseProgrammeImplementation.md). Its slate capacity
/// (releasesPerMonth x work budget, about 2 a month) binds. History's head independents issued about 4-5 singles
/// per Hot 100 record, so R = 4 puts a label with 25 Hot 100 records a year near 100 singles. An Independent with
/// 2-3 records stays on its own envelope. Capacity is earned and falls back when the records stop. Cash, the
/// going-concern reserve, roster and material still decide what is issued. Albums, Majors and the player are untouched.
/// Off: <see cref="Singles"/> returns the envelope bit-for-bit and nothing is counted.
/// </summary>
public static class EarnedHeadCapacity {
	public const string SwitchPrefix = "--earned-head-capacity=";
	private static bool configured;
	private static float perRecord;
	public static float SinglesPerRecord => perRecord;
	public static bool Enabled => perRecord > 0f;
	// One count per label per chart week: MonthlyCapacity is read several times a week per label.
	private static readonly Dictionary<string, (int Week, int Count)> cache = new(StringComparer.Ordinal);

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var values = (arguments ?? Array.Empty<string>()).Where(arg => arg.StartsWith(SwitchPrefix, StringComparison.Ordinal)).ToArray();
		if (values.Length > 1) throw new ArgumentException("Name --earned-head-capacity once.");
		if (values.Length == 1) {
			string raw = values[0].Substring(SwitchPrefix.Length);
			if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) || !(parsed > 0f && parsed <= 12f))
				throw new ArgumentException("--earned-head-capacity needs singles per Hot 100 record in (0, 12].");
			perRecord = parsed;
		}
		configured = true;
	}

	internal static void SetForChecks(float value) { configured = true; perRecord = value; cache.Clear(); }

	/// <summary>The earned monthly singles rate for a label with <paramref name="hotRecords52"/> recent Hot 100 records.</summary>
	internal static float EarnedRate(int hotRecords52, float majorCap) =>
		Math.Min(majorCap, perRecord * Math.Max(0, hotRecords52) / 12f);

	/// <summary>Pure form for checks: the envelope when off or out of scope, otherwise the larger of envelope and earned rate.</summary>
	internal static float Singles(float envelope, bool inScope, int hotRecords52, float majorCap) =>
		!Enabled || !inScope ? envelope : Math.Max(envelope, EarnedRate(hotRecords52, majorCap));

	public static bool InScope(AILabel label) => label != null && !label.isPlayerOwned && label.tier != LabelTier.Major;

	/// <summary>Live read used by <see cref="LabelReleaseProgramme.MonthlyCapacity"/>.</summary>
	public static float Singles(AILabel label, float envelope, float majorCap) {
		if (!Enabled || !InScope(label)) return envelope;
		return Singles(envelope, true, HotRecords52(label.labelId), majorCap);
	}

	private static int HotRecords52(string labelId) {
		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		if (cache.TryGetValue(labelId, out var hit) && hit.Week == week) return hit.Count;
		int count = CompetitorManager.Instance?.GetRecentChartingRecordCount(labelId, 52) ?? 0;
		cache[labelId] = (week, count);
		return count;
	}
}
