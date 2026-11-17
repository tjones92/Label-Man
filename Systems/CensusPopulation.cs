using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// --census-population (LpGenreAllocationFollowup.md §29). The authored regions total 157.9M against the 1960 census's
/// 178.5M (48 states and DC, by the regions in Data/Map/UsStates.json), and nearly all of the gap is one badly sourced
/// figure: the Deep South at 15.0M against 29.9M. Every region's infrastructure was authored at one per-capita density
/// of its authored population (about 24 stations, 2,800 jukeboxes and 10-12 record stores per million), so the Deep
/// South's is undersized by the same factor.
///
/// With the switch on, each region takes its census population and its infrastructure counts scale by census /
/// authored (national totals then read about 4,380 stations against Broadcasting's 4,339 AM+FM at the end of 1961, and
/// about 497,000 jukeboxes). The economy was calibrated against the authored record-buying population, so
/// <see cref="EconomicScale"/> (about 0.90) is set to hold the national record-buying population where it was: every
/// consumer that turns population or store counts into units, points, pools or fans reads
/// <see cref="MarketRegion.EconomicPopulation"/> or multiplies its store capacity by the scale. Regional shares move
/// (the Deep South from 7.8% to 13.9% of buyers); the national level does not. Off: scale 1, nothing mutated.
/// </summary>
public static class CensusPopulation {
	public const string Switch = "--census-population";
	// 1960 census, millions, by the region of each state in Data/Map/UsStates.json (Alaska and Hawaii are not mapped).
	internal static readonly IReadOnlyDictionary<string, float> Census1960 = new Dictionary<string, float>(StringComparer.Ordinal) {
		["greatlakes"] = 39.3f, ["greatplains"] = 15.4f, ["eastcoast"] = 54.8f, ["westcoast"] = 20.3f,
		["deepsouth"] = 29.9f, ["rockies"] = 4.6f, ["southwest"] = 14.2f,
	};
	public static bool Enabled => GameyAuditArguments.Has(Switch);
	/// <summary>--census-black-share (LpGenreAllocationFollowup.md §30, requires --census-population): each region's
	/// blackPopulation from the 1960 census by state. The authored shares were set for the authored populations; on census
	/// populations they imply 22.2M Black Americans (12.4%) against the census's 18.86M (10.6%), almost all of it a Deep
	/// South 0.33 that the nine census states put at 0.263.</summary>
	public const string BlackShareSwitch = "--census-black-share";
	public static bool BlackShareEnabled => Enabled && GameyAuditArguments.Has(BlackShareSwitch);
	// Negro population / total population, 1960 census, summed over each region's states in Data/Map/UsStates.json.
	// Source: Gibson and Jung, Historical Census Statistics on Population Totals by Race, 1790 to 1990 (Census Bureau
	// Population Division Working Paper 56, 2002), Tables 15-65. The 49 rows sum to 178,464,236 and 18,860,117 (its
	// Table 1 conterminous totals). Idaho, Montana and North Dakota (each under 0.3% Black) are approximate.
	internal static readonly IReadOnlyDictionary<string, float> CensusBlackShare1960 = new Dictionary<string, float>(StringComparer.Ordinal) {
		["greatlakes"] = .079f, ["greatplains"] = .036f, ["eastcoast"] = .090f, ["westcoast"] = .047f,
		["deepsouth"] = .263f, ["rockies"] = .014f, ["southwest"] = .099f,
	};
	public static float EconomicScale { get; private set; } = 1f;
	private static bool applied;

	/// <summary>Sets census populations and scaled infrastructure on the authored regions, once per process.</summary>
	public static void Apply(IReadOnlyList<MarketRegion> regions) {
		if (applied || !Enabled || regions == null || regions.Count == 0) return;
		ApplyCore(regions);
	}

	internal static void ApplyCore(IReadOnlyList<MarketRegion> regions) {
		var live = regions.Where(r => r != null).ToArray();
		foreach (var region in live)
			if (!Census1960.ContainsKey(region.regionId ?? ""))
				throw new InvalidOperationException(Switch + " has no census population for region " + region.regionId + ".");
		// GetBuyingPopulationPercentage reads only youth, income and urbanisation, so it is the same before and after.
		double before = live.Sum(r => (double)r.population * r.GetBuyingPopulationPercentage());
		foreach (var region in live) {
			float census = Census1960[region.regionId];
			double ratio = region.population > 0f ? census / region.population : 1d;
			Scale(region, ratio);
			region.population = census;
			if (BlackShareEnabled) region.blackPopulation = CensusBlackShare1960[region.regionId];
		}
		double after = live.Sum(r => (double)r.population * r.GetBuyingPopulationPercentage());
		EconomicScale = after > 0 ? (float)(before / after) : 1f;
		applied = true;
		GD.Print($"CensusPopulation: regions={live.Length} population={live.Sum(r => r.population):F1}M " +
			$"economicScale={EconomicScale:F4} blackShare={(BlackShareEnabled ? "census" : "authored")}");
	}

	private static int S(int count, double ratio) => (int)Math.Round(Math.Max(0, count) * ratio, MidpointRounding.AwayFromZero);

	private static void Scale(MarketRegion region, double ratio) {
		region.collegeCount = S(region.collegeCount, ratio);
		if (region.media != null) {
			region.media.totalRadioStations = S(region.media.totalRadioStations, ratio);
			region.media.jukeboxCount = S(region.media.jukeboxCount, ratio);
			region.media.concertVenueCount = S(region.media.concertVenueCount, ratio);
		}
		if (region.musicIndustry != null) {
			region.musicIndustry.recordingStudioCount = S(region.musicIndustry.recordingStudioCount, ratio);
			region.musicIndustry.localLabelCount = S(region.musicIndustry.localLabelCount, ratio);
			region.musicIndustry.clubCount = S(region.musicIndustry.clubCount, ratio);
			region.musicIndustry.theaterCount = S(region.musicIndustry.theaterCount, ratio);
		}
		if (region.distribution != null) {
			region.distribution.recordStoreCount = S(region.distribution.recordStoreCount, ratio);
			region.distribution.departmentStoreCount = S(region.distribution.departmentStoreCount, ratio);
		}
	}

	internal static void SetForChecks(float scale) { applied = true; EconomicScale = scale; }
	internal static void ResetForChecks() { applied = false; EconomicScale = 1f; }
}
