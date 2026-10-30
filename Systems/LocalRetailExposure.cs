using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Research switch (SimTools/LocalRetailExposureInvestigation.md), off unless <c>--local-retail-exposure</c> is named.
/// <para>
/// Off, every rank-keyed term in live single demand reads the NATIONAL chart: the rank exposure weight (rack space,
/// jukebox slots, the listening booth), the top-10/top-40 awareness floors, the chart-visibility discovery signal, the
/// top-20 shelf bonus and rack-jobber access. A record that is national #15 therefore gets #15's shelf in all seven
/// regions, including the ones where nobody is buying it, and a record that is a region's best seller gets nothing for
/// it there until the national total catches up. That is a common factor across every region, and it is the reason
/// a late region has to "relive" the national rise from a log (§9u).
/// </para>
/// <para>
/// On, those terms read the region's own single sales list (<see cref="RegionalRecordData.localSalesRank"/>, last
/// week's settled units, the list the stations already read). Rack jobbers, one-stops and jukebox operators stocked
/// what turned over in their own territory. Rank weights are a permutation within each region: every region still has
/// exactly one #1, ten Top-10 floors and one hundred charted shelf weights, so the region's exposure mass is
/// unchanged and the common purse clears the same units. Only which records hold them changes.
/// </para>
/// <para>
/// The post-peak demand decay (ChartTailProbe) also runs on the region's own sales peak for live releases, so a
/// region that takes a record late is not decayed from the nation's peak while it is still rising. The national chart
/// keeps its trade-attention roles (campaign hold, radio, national awareness and word of mouth, the national
/// exposure spread rule) and is otherwise a measurement.
/// </para>
/// <para>
/// <c>--local-retail-exposure=λ</c> blends the two lists: a region's retail position is the geometric mean of the
/// national and local positions, weighted λ to the local one (an unlisted record counts as #101). Rack jobbers and the
/// one-stops that served jukebox operators bought from the national trade charts; record shops worked from the local
/// station survey. Wholly local (λ = 1, the bare flag) makes the seven regions seven independent cumulative-advantage
/// markets: on lrc52 seed 1001 their top-11 overlap fell from 55% to 29%, under even the 40% of single-city surveys.
/// </para>
/// Fresh worlds only: the local list is recomputed weekly and is not saved.
/// </summary>
public static class LocalRetailExposure {
	public const string Flag = "--local-retail-exposure";
	public static bool Enabled { get; private set; }
	/// <summary>λ: the local list's weight in a region's retail position; 1 for the bare flag.</summary>
	public static float LocalWeight { get; private set; } = 1f;
	private const int Unlisted = 101;
	private static bool configured;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var args = (arguments ?? Array.Empty<string>()).ToArray();
		var weighted = args.Where(a => a.StartsWith(Flag + "=", StringComparison.Ordinal)).ToArray();
		if (weighted.Length > 1 || weighted.Length == 1 && args.Contains(Flag))
			throw new ArgumentException("Name " + Flag + " once.");
		if (weighted.Length == 1) {
			if (!float.TryParse(weighted[0].Substring(Flag.Length + 1), System.Globalization.NumberStyles.Float,
				System.Globalization.CultureInfo.InvariantCulture, out float weight) || !(weight > 0f && weight <= 1f))
				throw new ArgumentException(Flag + "= takes a local weight in (0, 1].");
			LocalWeight = weight;
		}
		Enabled = args.Contains(Flag) || weighted.Length == 1;
		if (Enabled && args.Any(a => a.StartsWith("--resume-world") || a.StartsWith("--save-world")))
			throw new ArgumentException(Flag + " is a fresh-world research switch; the local list is not saved.");
		// Both replace the exposure or the consumer that the mass counterfactual reads.
		if (Enabled && args.Any(a => a.StartsWith("--local-consumption-shelf=") || a.StartsWith("--bounded-audience=")))
			throw new ArgumentException(Flag + " cannot run with the local shelf or bounded audience probes.");
		if (Enabled) Godot.GD.Print($"LOCAL_RETAIL_EXPOSURE on localWeight={LocalWeight:F2}");
		configured = true;
	}

	internal static void SetForChecks(bool enabled) {
		configured = true; Enabled = enabled; seeded = false;
		nationalMass.Clear(); localMass.Clear(); scales.Clear();
	}

	// The mass counterfactual. Keying shelf to the local list puts the best weights on the records each region already
	// buys most, so the same weights raise the region's single intent by about 7% (rearrangement, measured on
	// lrc52-v1-ctl-1001). The purse clears the same units, but before LPs have their own channel the singles/LP split
	// is by intent, so that 7% would take LP units. Each region's singles are scaled by last week's ratio of the
	// national-keyed to the local-keyed intent, which holds the region's single intent where the national chart puts it.
	public const float MinScale = .5f, MaxScale = 1.5f;
	private static readonly Dictionary<string, double> nationalMass = new(StringComparer.Ordinal), localMass = new(StringComparer.Ordinal);
	private static readonly Dictionary<string, float> scales = new(StringComparer.Ordinal);
	private static int weeks;

	/// <summary>This week's scale on the region's single conversion; 1 before the first observed week.</summary>
	public static float Scale(string regionId) => scales.TryGetValue(regionId, out float scale) ? scale : 1f;

	/// <summary>One single's intent this week, keyed to the national chart and (before the scale) to the local list.</summary>
	public static void Observe(string regionId, double national, double local) {
		if (!double.IsFinite(national) || !double.IsFinite(local) || national < 0d || local < 0d)
			throw new InvalidOperationException("Local retail exposure observed a nonfinite or negative intent.");
		nationalMass[regionId] = nationalMass.GetValueOrDefault(regionId) + national;
		localMass[regionId] = localMass.GetValueOrDefault(regionId) + local;
	}

	/// <summary>Before a live week's demand: last week's observations become this week's scales.</summary>
	public static void BeginWeek() {
		if (!Enabled) return;
		foreach (var (regionId, local) in localMass)
			if (local > 0d) scales[regionId] = (float)Math.Clamp(nationalMass.GetValueOrDefault(regionId) / local, MinScale, MaxScale);
		nationalMass.Clear(); localMass.Clear();
		if (++weeks % 13 == 0)
			Godot.GD.Print("LOCAL_RETAIL_EXPOSURE scales " + string.Join(" ", scales.OrderBy(p => p.Key, StringComparer.Ordinal)
				.Select(p => p.Key + "=" + p.Value.ToString("F3", System.Globalization.CultureInfo.InvariantCulture))));
	}

	private static bool seeded;
	/// <summary>
	/// True once, before the first live week's demand. The list is otherwise written after each week's sales, so without
	/// a seed every single would meet the first live week unranked everywhere -- no floors, no shelf -- and that week's
	/// demand fell to about half the purse. The seed reads the last prewarm week's settled units.
	/// </summary>
	public static bool TakeSeed() {
		if (!Enabled || seeded) return false;
		seeded = true;
		return true;
	}

	/// <summary>
	/// The region's list position as a chart position: 1-100 on the region's own top hundred, 0 (uncharted) below it or
	/// unsold, the same scale the national chart gives these terms.
	/// </summary>
	public static int Position(RegionalRecordData data) =>
		data != null && data.localSalesRank >= 1 && data.localSalesRank <= 100 ? data.localSalesRank : 0;

	/// <summary>
	/// The region's retail position for a term that otherwise reads <paramref name="nationalPosition"/>: the local list
	/// at λ = 1, else the λ-weighted geometric mean of the two positions, 0 (uncharted) beyond #100.
	/// </summary>
	public static int Position(int nationalPosition, RegionalRecordData data) {
		int local = Position(data);
		if (LocalWeight >= 1f) return local;
		double national = nationalPosition >= 1 && nationalPosition <= 100 ? nationalPosition : Unlisted;
		double blended = Math.Exp((1d - LocalWeight) * Math.Log(national) + LocalWeight * Math.Log(local > 0 ? local : Unlisted));
		int position = (int)Math.Round(blended);
		return position <= 100 ? Math.Max(1, position) : 0;
	}

	/// <summary>
	/// Weeks past the sales peak the post-peak decay reads: the region's own for a live release. The opening catalogue
	/// keeps the national clock, because its regional peaks only start counting at the first live week.
	/// </summary>
	public static int WeeksSincePeak(RecordRuntimeData record, RegionalRecordData data) =>
		Enabled && data != null && record.baseRecord != null && record.baseRecord.releaseDate > GameDate.StartDate
			? data.WeeksSinceLocalPeak(record.weeksSinceRelease)
			: record.weeksSincePeakUnits;
}
