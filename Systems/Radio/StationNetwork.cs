using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Plain C# orchestrator owned by ChartManager (design doc a/b). Holds the ~63 reporter stations,
/// generates them from each region's SegmentCapacityModel, and applies the decade's era events
/// (Boss conversion, FM emergence) from <see cref="RadioEra"/>. NOT a Godot Node - it runs inline
/// inside the profiled SimulateWeek loop, like ChartSimulator.
///
/// PHASE 1 (this file): roster generation + era events, all ADDITIVE and inert. Nothing here
/// feeds RegionalRecordData.radioPlay yet - the aggregation swap is Phase 2 in the sibling partial
/// StationNetwork.Aggregation.cs. Building the roster changes no economic behavior; it only stands
/// the panel up so the aggregation has stations to read.
/// </summary>
public sealed partial class StationNetwork {

	// ---- reporter counts by tier, summing to ~63 across the authored regions ----
	private static int ReporterCountForTier(RegionTier tier) => tier switch {
		RegionTier.Major => 11,
		RegionTier.Regional => 9,
		RegionTier.Secondary => 7,
		_ => 6
	};

	private readonly Dictionary<string, List<RadioStation>> stationsByRegion = new(StringComparer.Ordinal);
	private readonly Dictionary<string, RadioStation> stationsById = new(StringComparer.Ordinal);
	private readonly Dictionary<string, Deejay> djsById = new(StringComparer.Ordinal);
	// Tail format reach share per region: format -> share of the region's total reach carried by
	// the aggregate (non-reporter) stations of that format. Derived from region identity + station
	// counts; rebuilt on year change (Boss consolidation / FM emergence). Consumed in Phase 2.
	private readonly Dictionary<string, Dictionary<StationFormat, float>> tailFormatReachByRegion =
		new(StringComparer.Ordinal);

	private readonly RandomNumberGenerator rng = new();
	private int currentYear;

	// Payola candidacy lookup (recordId, stationId) -> active bribe boost. Set by ChartManager to the
	// PayolaLedger (doc d); null in headless audits (payola is player-only), so candidacy reads 0.
	public System.Func<string, string, float> ActivePayolaLookup;

	public StationNetwork(ulong seed) {
		rng.Seed = seed;   // seeded off the sim master seed for reproducibility
	}

	public IReadOnlyList<RadioStation> ReportersInRegion(string regionId) =>
		stationsByRegion.TryGetValue(regionId, out var list) ? list : (IReadOnlyList<RadioStation>)Array.Empty<RadioStation>();
	public RadioStation GetStation(string id) => id != null && stationsById.TryGetValue(id, out var s) ? s : null;
	public Deejay GetDeejay(string id) => id != null && djsById.TryGetValue(id, out var d) ? d : null;
	public int StationCount => stationsById.Count;

	public IEnumerable<RadioStation> AllStations() {
		foreach (var kv in stationsByRegion)
			foreach (RadioStation s in kv.Value) yield return s;
	}

	// ====================================================================
	// ROSTER GENERATION
	// ====================================================================

	// National mean of each format's regional audience weight, computed once at BuildRosters. The
	// region's own weight divided by this is its AFFINITY for that format -- how much more (or less)
	// of it that market carries than the country as a whole. It is what keeps the Deep South heavy on
	// R&B and Gospel and the West Coast light on Country while the NATIONAL mix still follows
	// RadioEra.FormatEraShare.
	private readonly Dictionary<StationFormat, float> nationalFormatMean = new();
	private const float AFFINITY_MIN = 0.30f, AFFINITY_MAX = 3.0f;

	public void BuildRosters(IEnumerable<MarketRegion> regions, int year) {
		currentYear = year;
		stationsByRegion.Clear();
		stationsById.Clear();
		djsById.Clear();
		tailFormatReachByRegion.Clear();

		List<MarketRegion> regionList = regions as List<MarketRegion> ?? regions.ToList();
		ComputeNationalFormatMeans(regionList, year);
		var targets = ComputeTargets(regionList, year);

		foreach (MarketRegion region in regionList) {
			var roster = GenerateRegionRoster(region, targets.GetValueOrDefault(region.regionId));
			stationsByRegion[region.regionId] = roster;
			foreach (RadioStation s in roster) stationsById[s.stationId] = s;
			RebuildTailFormatReach(region, year);
		}
	}

	/// <summary>Moves each region's roster toward the year's national format mix, then applies the DJ
	/// churn. Stations MIGRATE rather than being regenerated -- stationId, callsign and the player's
	/// cultivated label rapport all survive a format flip, so a relationship built in 1962 is still
	/// worth something when the station goes Boss Top 40 in 1966.</summary>
	public void OnYearChanged(IEnumerable<MarketRegion> regions, int year) {
		currentYear = year;
		List<MarketRegion> regionList = regions as List<MarketRegion> ?? regions.ToList();
		var targets = ComputeTargets(regionList, year);
		foreach (MarketRegion region in regionList) {
			MigrateFormats(region, targets.GetValueOrDefault(region.regionId));
			RollDjGreedAndScandals(stationsByRegion.GetValueOrDefault(region.regionId), region, year);
			RebuildTailFormatReach(region, year);
		}
		LogRealizedMix(year);
	}

	/// <summary>Realized national format census after migration -- the check that the roster actually
	/// TRACKS the target rather than merely that the target is right.</summary>
	private void LogRealizedMix(int year) {
		var heads = new Dictionary<StationFormat, int>();
		var reach = new Dictionary<StationFormat, float>();
		float reachTotal = 0f;
		foreach (RadioStation s in AllStations()) {
			heads[s.format] = heads.GetValueOrDefault(s.format) + 1;
			reach[s.format] = reach.GetValueOrDefault(s.format) + s.EffectiveReach();
			reachTotal += s.EffectiveReach();
		}
		GD.Print($"[MIXREAL {year}] n={heads.Values.Sum()}  " + string.Join("  ",
			heads.OrderByDescending(kv => kv.Value).ThenBy(kv => (int)kv.Key)
				.Select(kv => $"{kv.Key}={kv.Value}/{100f * reach.GetValueOrDefault(kv.Key) / Mathf.Max(0.0001f, reachTotal):F0}%")));
	}

	private void ComputeNationalFormatMeans(List<MarketRegion> regions, int year) {
		nationalFormatMean.Clear();
		if (regions.Count == 0) return;
		foreach (MarketRegion region in regions) {
			var w = FormatWeightsFromSegments(region, year);
			foreach (var kv in w) nationalFormatMean[kv.Key] = nationalFormatMean.GetValueOrDefault(kv.Key) + kv.Value;
		}
		foreach (StationFormat f in nationalFormatMean.Keys.ToList()) nationalFormatMean[f] /= regions.Count;
	}

	/// <summary>
	/// The year's target roster, allocated NATIONALLY and then placed by region affinity.
	///
	/// Allocating per-region instead cannot represent a small format at all: Country's 2.2% national
	/// share in 1960 is 0.24 stations against a region's 11 slots, so it floors to zero in every region
	/// and its mass is absorbed by the large formats (measured: Country and Gospel both vanished, while
	/// R&B landed at 9% against a 4.4% target). Anything below ~4.5% national share was unrepresentable
	/// -- precisely the specialist formats this rebuild exists to model. So the national margin is fixed
	/// FIRST by largest remainder, and affinity then decides only WHERE each station lives.
	///
	/// Formats are placed scarcest-first so a 2-station Country allocation gets its pick of the most
	/// country-leaning markets before Top 40 fills the map, and each format is dealt round-robin down
	/// the affinity order so it spreads rather than piling into one region.
	/// </summary>
	private Dictionary<string, Dictionary<StationFormat, int>> ComputeTargets(List<MarketRegion> regions, int year) {
		var result = new Dictionary<string, Dictionary<StationFormat, int>>(StringComparer.Ordinal);
		var capacity = new Dictionary<string, int>(StringComparer.Ordinal);
		int totalSlots = 0;
		foreach (MarketRegion r in regions) {
			result[r.regionId] = new Dictionary<StationFormat, int>();
			capacity[r.regionId] = ReporterCountForTier(r.tier);
			totalSlots += capacity[r.regionId];
		}
		if (totalSlots == 0) return result;

		// ---- 1. national counts by largest remainder ----
		var share = new Dictionary<StationFormat, float>();
		foreach (StationFormat f in RadioEra.MixFormats()) {
			float s = RadioEra.FormatEraShare(f, year);
			if (s > 0f) share[f] = s;
		}
		float shareTotal = share.Values.Sum();
		if (shareTotal <= 0f) { foreach (var r in regions) result[r.regionId][StationFormat.Top40] = capacity[r.regionId]; return result; }

		var exact = share.ToDictionary(kv => kv.Key, kv => kv.Value / shareTotal * totalSlots);
		var national = exact.ToDictionary(kv => kv.Key, kv => Mathf.FloorToInt(kv.Value));
		foreach (var f in exact.OrderByDescending(kv => kv.Value - Mathf.FloorToInt(kv.Value))
								.ThenBy(kv => (int)kv.Key).Take(Math.Max(0, totalSlots - national.Values.Sum())))
			national[f.Key] = national.GetValueOrDefault(f.Key) + 1;

		// ---- 2. every market keeps a Top 40 outlet, funded from the national Top40 allocation ----
		int used = 0;
		var placed = new Dictionary<string, int>(StringComparer.Ordinal);
		foreach (MarketRegion r in regions) placed[r.regionId] = 0;
		int top40Reserved = Math.Min(national.GetValueOrDefault(StationFormat.Top40), regions.Count);
		for (int i = 0; i < top40Reserved; i++) {
			MarketRegion r = regions[i];
			result[r.regionId][StationFormat.Top40] = 1;
			placed[r.regionId]++; used++;
		}
		national[StationFormat.Top40] = national.GetValueOrDefault(StationFormat.Top40) - top40Reserved;

		// ---- 3. place the rest, scarcest format first, round-robin down the affinity order ----
		foreach (var entry in national.Where(kv => kv.Value > 0).OrderBy(kv => kv.Value).ThenBy(kv => (int)kv.Key)) {
			StationFormat format = entry.Key;
			int remaining = entry.Value;
			var order = regions
				.OrderByDescending(r => RegionAffinity(r, format, year))
				.ThenBy(r => r.regionId, StringComparer.Ordinal).ToList();
			while (remaining > 0) {
				bool anyPlaced = false;
				foreach (MarketRegion r in order) {
					if (remaining == 0) break;
					if (placed[r.regionId] >= capacity[r.regionId]) continue;
					result[r.regionId][format] = result[r.regionId].GetValueOrDefault(format) + 1;
					placed[r.regionId]++; remaining--; used++; anyPlaced = true;
				}
				if (!anyPlaced) break;   // every region full
			}
		}
		return result;
	}

	/// <summary>How much more (or less) of a format this market carries than the national average.
	/// Placement-only: the national margin is already fixed, so this cannot distort the mix.</summary>
	private float RegionAffinity(MarketRegion region, StationFormat format, int year) {
		float mean = nationalFormatMean.GetValueOrDefault(format);
		if (mean <= 0.0001f) return 1f;
		float w = FormatWeightsFromSegments(region, year).GetValueOrDefault(format);
		return Mathf.Clamp(w / mean, AFFINITY_MIN, AFFINITY_MAX);
	}

	private List<RadioStation> GenerateRegionRoster(MarketRegion region, Dictionary<StationFormat, int> formatCounts) {
		int slots = ReporterCountForTier(region.tier);
		formatCounts ??= new Dictionary<StationFormat, int> { [StationFormat.Top40] = slots };

		var roster = new List<RadioStation>(slots);
		int indexInRegion = 0;
		bool west = IsWesternRegion(region);

		// Ordered by the YEAR'S national share, so index 0 -- the flagship, which carries 1.5x reach
		// weight plus a 40% clear-channel chance -- is the era's dominant format: a full-service
		// clear-channel powerhouse in 1960, a Boss Top 40 signal by 1969. Forcing Top40 to the front
		// regardless of year (to keep a Gospel specialist out of the flagship slot) instead made every
		// market a Top-40-flagship world in 1960, which put 31-33% of panel REACH behind 21% of heads.
		// Top 40 scores pure-AM genres near 1.0, so that handed the early chart to TeenPop: 108 slots
		// against a 66 benchmark on the 156-week A/B, the same failure mode as the FullService
		// discrimination arm. Era ordering keeps the specialists out of the flagship slot anyway,
		// because FullService or Top40 leads the mix in every year of the decade.
		foreach (var (format, count) in formatCounts.OrderByDescending(kv => RadioEra.FormatEraShare(kv.Key, currentYear))
													.ThenByDescending(kv => kv.Value).ThenBy(kv => (int)kv.Key)
													.Select(kv => (kv.Key, kv.Value))) {
			for (int i = 0; i < count; i++) {
				bool flagship = indexInRegion == 0;
				RadioStation station = CreateStation(region, format, flagship, west, indexInRegion);
				Deejay dj = CreateDeejay(station, region);
				station.leadDjId = dj.djId;
				djsById[dj.djId] = dj;
				roster.Add(station);
				indexInRegion++;
			}
		}

		NormalizeReporterReachShares(roster);
		return roster;
	}

	// ====================================================================
	// FORMAT MIGRATION - the roster tracks the dial across the decade
	// ====================================================================

	/// <summary>Reconcile a region's actual format counts against the year's target by converting
	/// surplus stations into deficit formats, one at a time, largest gap first.</summary>
	private void MigrateFormats(MarketRegion region, Dictionary<StationFormat, int> target) {
		if (target == null) return;
		if (!stationsByRegion.TryGetValue(region.regionId, out List<RadioStation> roster) || roster.Count == 0) return;
		var actual = new Dictionary<StationFormat, int>();
		foreach (RadioStation s in roster) actual[s.format] = actual.GetValueOrDefault(s.format) + 1;

		var formats = new HashSet<StationFormat>(actual.Keys);
		foreach (StationFormat f in target.Keys) formats.Add(f);

		for (int step = 0; step < roster.Count; step++) {
			StationFormat over = default, under = default;
			int overBy = 0, underBy = 0;
			foreach (StationFormat f in formats) {
				int gap = actual.GetValueOrDefault(f) - target.GetValueOrDefault(f);
				if (gap > overBy) { overBy = gap; over = f; }
				if (-gap > underBy) { underBy = -gap; under = f; }
			}
			if (overBy <= 0 || underBy <= 0) break;
			RadioStation donor = PickMigrationDonor(roster, over);
			if (donor == null) break;
			ConvertStation(donor, under, region);
			actual[over]--;
			actual[under] = actual.GetValueOrDefault(under) + 1;
		}
		NormalizeReporterReachShares(roster);
	}

	/// <summary>The station a format gives up first: the weakest signal, never the region flagship.
	/// A market's dominant outlet does not flip format because the national mix moved.</summary>
	private static RadioStation PickMigrationDonor(List<RadioStation> roster, StationFormat format) {
		RadioStation pick = null;
		for (int i = 1; i < roster.Count; i++) {          // index 0 is the flagship
			RadioStation s = roster[i];
			if (s.format != format) continue;
			if (pick == null || s.regionReachShare < pick.regionReachShare) pick = s;
		}
		// Only the flagship carries this format -- allow it rather than stall the migration.
		if (pick == null && roster[0].format == format) pick = roster[0];
		return pick;
	}

	/// <summary>Re-format a station in place. Identity and the player's label rapport SURVIVE; the
	/// programming attributes are rebuilt, and a DJ whose archetype no longer suits the format is
	/// re-cast (keeping the djId, so a cultivated relationship is not silently deleted).</summary>
	private void ConvertStation(RadioStation station, StationFormat to, MarketRegion region) {
		bool bossFlip = station.format is StationFormat.FullService && to == StationFormat.Top40;
		station.format = to;
		var (high, mid, light) = SlotsForFormat(to);
		station.highSlots = high; station.midSlots = mid; station.lightSlots = light;
		station.band = to == StationFormat.UndergroundFM ? RadioBand.FM : RadioBand.AM;
		station.latinLeaning = to == StationFormat.Top40 && region?.regionType == RegionType.Western
			&& RegionalLatinShare(region) > 0.05f;

		Deejay dj = GetDeejay(station.leadDjId);
		if (bossFlip) {
			// The Boss Radio conversion: the tight-playlist, low-autonomy format that gutted the
			// personality era. RadioEra.BossRadioAdoption describes the same phenomenon as the
			// FullService->Top40 leg of the mix curve, so the character change lands here.
			station.djAutonomy = 0.1f;
			station.highSlots = 8; station.midSlots = 10; station.lightSlots = 12;
			if (dj != null && dj.archetype == DJArchetype.Personality) dj.archetype = DJArchetype.CompanyMan;
		} else {
			station.djAutonomy = Mathf.Clamp(BaseAutonomyForFormat(to) + (rng.Randf() - 0.5f) * 0.1f, 0f, 1f);
			if (dj != null) dj.archetype = PickArchetype(to);
		}
	}

	// ---- region audience fingerprint per format. This is now a RELATIVE signal only: divided by the
	// national mean it becomes the region's affinity, and RadioEra.FormatEraShare supplies the absolute
	// national quantity. It therefore no longer gates on FM viability (the FM era share is zero before
	// 1967, which is what actually holds FM out of the roster). ----
	private static Dictionary<StationFormat, float> FormatWeightsFromSegments(MarketRegion region, int year) {
		var shares = region.segmentCapacities?.Shares;
		float Seg(AudienceSegment s) => shares != null && shares.TryGetValue(s, out float v) ? v : 0f;
		return new Dictionary<StationFormat, float> {
			[StationFormat.Top40] = Seg(AudienceSegment.MainstreamAM) + Seg(AudienceSegment.Youth) + Seg(AudienceSegment.RegionalLatin),
			// The block-programmed generalist tracks the same mainstream audience as Top 40, plus a
			// share of the adult listener its "good music" block served.
			[StationFormat.FullService] = Seg(AudienceSegment.MainstreamAM) + Seg(AudienceSegment.Youth)
				+ Seg(AudienceSegment.AdultMOR) * 0.5f + Seg(AudienceSegment.CollegeFolk) * 0.5f,
			[StationFormat.RnB] = Seg(AudienceSegment.UrbanRnB),
			[StationFormat.Country] = Seg(AudienceSegment.CountryWestern),
			[StationFormat.MOR] = Seg(AudienceSegment.AdultMOR) + Seg(AudienceSegment.FamilyChildrens),
			[StationFormat.Jazz] = Seg(AudienceSegment.JazzHiFiClassical),
			[StationFormat.Gospel] = Seg(AudienceSegment.GospelChurch),
			[StationFormat.UndergroundFM] = Seg(AudienceSegment.UndergroundFM) + Seg(AudienceSegment.CollegeFolk)
				+ region.culturalProgressivism * 0.05f,
		};
	}

	/// <summary>Diagnostic: the projected roster at each mix keyframe, without simulating to it. Lets a
	/// one-week run verify the whole decade's format curve.</summary>
	public void LogProjectedMix(IEnumerable<MarketRegion> regions) {
		var list = regions as List<MarketRegion> ?? regions.ToList();
		foreach (int y in new[] { 1960, 1963, 1965, 1967, 1969 }) {
			var targets = ComputeTargets(list, y);
			var national = new Dictionary<StationFormat, int>();
			foreach (var kv in targets)
				foreach (var fc in kv.Value) national[fc.Key] = national.GetValueOrDefault(fc.Key) + fc.Value;
			int total = national.Values.Sum();
			GD.Print($"[MIX {y}] n={total}  " + string.Join("  ", national.OrderByDescending(kv => kv.Value)
				.ThenBy(kv => (int)kv.Key).Select(kv => $"{kv.Key}={kv.Value}({100f * kv.Value / Mathf.Max(1, total):F0}%)")));
			if (y is 1960 or 1969)
				foreach (var kv in targets.OrderBy(kv => kv.Key, StringComparer.Ordinal))
					GD.Print($"   [MIX {y}] {kv.Key}: " + string.Join(", ", kv.Value.Where(f => f.Value > 0)
						.OrderByDescending(f => f.Value).ThenBy(f => (int)f.Key).Select(f => $"{f.Key}x{f.Value}")));
		}
	}

	// ---- per-station attributes ----
	private static (int high, int mid, int light) SlotsForFormat(StationFormat f) => f switch {
		StationFormat.Top40 or StationFormat.RnB => (8, 12, 15),   // tight, high-turnover
		StationFormat.FullService => (6, 10, 20),                 // looser, DJ variety
		StationFormat.UndergroundFM => (4, 8, 25),                // deep cuts, low spins, wide catalog
		_ => (6, 12, 18)                                          // Country / MOR / Gospel / Jazz
	};

	private static float BaseAutonomyForFormat(StationFormat f) => f switch {
		StationFormat.FullService => 0.85f,
		StationFormat.UndergroundFM => 0.85f,
		StationFormat.Top40 or StationFormat.RnB => 0.5f,
		_ => 0.5f
	};

	private RadioStation CreateStation(MarketRegion region, StationFormat format, bool flagship, bool west, int indexInRegion) {
		var (high, mid, light) = SlotsForFormat(format);
		bool clearChannel = rng.Randf() < (flagship ? 0.40f : 0.08f);
		float basePayola = region.media?.payolaSusceptibility ?? 0.3f;
		float payola = Mathf.Clamp(basePayola + (rng.Randf() - 0.5f) * 0.3f, 0f, 1f);

		return new RadioStation {
			stationId = $"{region.regionId}-stn-{indexInRegion:D2}",
			callsign = GenerateCallsign(west),
			cityName = region.majorCities is { Length: > 0 } ? region.majorCities[0] : region.regionName,
			regionId = region.regionId,
			latinLeaning = format == StationFormat.Top40 && region.regionType == RegionType.Western
				&& RegionalLatinShare(region) > 0.05f,
			band = format == StationFormat.UndergroundFM ? RadioBand.FM : RadioBand.AM,
			wattage = clearChannel ? 50000 : rng.RandiRange(1000, 10000),
			clearChannel = clearChannel,
			format = format,
			highSlots = high, midSlots = mid, lightSlots = light,
			djAutonomy = Mathf.Clamp(BaseAutonomyForFormat(format) + (rng.Randf() - 0.5f) * 0.1f, 0f, 1f),
			payolaSusceptibility = payola,
			integrityRisk = Mathf.Clamp(payola * 0.6f + rng.Randf() * 0.3f, 0f, 1f),
			rt = new StationRuntime()
		};
	}

	private static float RegionalLatinShare(MarketRegion region) =>
		region.segmentCapacities?.Shares.TryGetValue(AudienceSegment.RegionalLatin, out float v) == true ? v : 0f;

	// ---- DJ generation: archetype weighted by format (doc a 2.6) ----
	private Deejay CreateDeejay(RadioStation station, MarketRegion region) {
		DJArchetype archetype = PickArchetype(station.format);
		var dj = new Deejay {
			djId = $"{station.stationId}-dj",
			djName = $"DJ {station.callsign}",   // procedural placeholder; NamingEngine wiring is a later nicety
			homeStationId = station.stationId,
			archetype = archetype
		};
		// Attributes by archetype.
		switch (archetype) {
			case DJArchetype.Personality: dj.influence = 0.75f; dj.taste = 0.65f; dj.greed = 0.35f; dj.ego = 0.7f; break;
			case DJArchetype.Tastemaker:  dj.influence = 0.6f;  dj.taste = 0.85f; dj.greed = 0.15f; dj.ego = 0.5f; break;
			case DJArchetype.Hustler:     dj.influence = 0.5f;  dj.taste = 0.3f;  dj.greed = 0.85f; dj.ego = 0.3f; break;
			case DJArchetype.CompanyMan:  dj.influence = 0.35f; dj.taste = 0.45f; dj.greed = 0.3f;  dj.ego = 0.25f; break;
			case DJArchetype.Regional:    dj.influence = 0.4f;  dj.taste = 0.5f;  dj.greed = 0.25f; dj.ego = 0.35f; break;
		}
		float j(float v) => Mathf.Clamp(v + (rng.Randf() - 0.5f) * 0.15f, 0f, 1f);
		dj.influence = j(dj.influence); dj.taste = j(dj.taste); dj.greed = j(dj.greed); dj.ego = j(dj.ego);
		return dj;
	}

	private DJArchetype PickArchetype(StationFormat format) {
		// Weighted draw per format (pre-Boss baseline; Boss conversion rewrites Personality->CompanyMan).
		(DJArchetype a, float w)[] table = format switch {
			StationFormat.Top40 => new[] {
				(DJArchetype.Personality, 0.4f), (DJArchetype.Tastemaker, 0.2f), (DJArchetype.Hustler, 0.3f), (DJArchetype.CompanyMan, 0.1f) },
			StationFormat.FullService => new[] {
				(DJArchetype.Personality, 0.5f), (DJArchetype.Tastemaker, 0.3f), (DJArchetype.Regional, 0.2f) },
			StationFormat.RnB => new[] {
				(DJArchetype.Personality, 0.4f), (DJArchetype.Hustler, 0.3f), (DJArchetype.Tastemaker, 0.3f) },
			StationFormat.UndergroundFM => new[] {
				(DJArchetype.Tastemaker, 0.7f), (DJArchetype.Personality, 0.3f) },
			_ => new[] {   // Country / MOR / Gospel / Jazz
				(DJArchetype.Regional, 0.5f), (DJArchetype.CompanyMan, 0.3f), (DJArchetype.Tastemaker, 0.2f) }
		};
		float total = table.Sum(t => t.w);
		float roll = rng.Randf() * total;
		foreach (var (a, w) in table) { roll -= w; if (roll <= 0f) return a; }
		return table[^1].a;
	}

	// ---- callsign: W east of Mississippi, K west (historical convention; cosmetic authenticity) ----
	private string GenerateCallsign(bool west) {
		const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
		Span<char> c = stackalloc char[4];
		c[0] = west ? 'K' : 'W';
		for (int i = 1; i < 4; i++) c[i] = letters[rng.RandiRange(0, letters.Length - 1)];
		return new string(c);
	}

	private static bool IsWesternRegion(MarketRegion region) {
		if (region.regionType == RegionType.Western) return true;
		string id = (region.regionId ?? "") + " " + (region.regionName ?? "");
		id = id.ToLowerInvariant();
		foreach (string w in new[] { "west", "pacific", "mountain", "southwest", "california", "texas", "midwest", "plains", "rockies" })
			if (id.Contains(w)) return true;
		return false;
	}

	// ---- reach shares: reporters within a region normalized to sum 1.0; flagship/clear-channel weighted ----
	private static void NormalizeReporterReachShares(List<RadioStation> roster) {
		float total = 0f;
		var raw = new float[roster.Count];
		for (int i = 0; i < roster.Count; i++) {
			RadioStation s = roster[i];
			float w = 1f;
			if (i == 0) w += 1.5f;                 // flagship carries the market
			if (s.clearChannel) w += 1.0f;         // clear-channel signal
			w += s.wattage / 50000f;               // wattage tilt
			raw[i] = w; total += w;
		}
		for (int i = 0; i < roster.Count; i++)
			roster[i].regionReachShare = total > 0f ? raw[i] / total : 1f / Math.Max(1, roster.Count);
	}

	// ---- tail format reach: mapped segment shares by format, normalized; FM scaled by station share ----
	private void RebuildTailFormatReach(MarketRegion region, int year) {
		var weight = FormatWeightsFromSegments(region, year);
		// The FM tail's physical presence grows across the decade independent of the audience mix.
		if (weight.ContainsKey(StationFormat.UndergroundFM))
			weight[StationFormat.UndergroundFM] *= Mathf.Max(0.0001f, RadioEra.FmStationShare(year) / RadioEra.FmStationShare(1969));
		float total = weight.Values.Sum();
		var map = new Dictionary<StationFormat, float>();
		if (total > 0f)
			foreach (var kv in weight) map[kv.Key] = kv.Value / total;
		tailFormatReachByRegion[region.regionId] = map;
	}

	// ====================================================================
	// DJ CHURN
	//
	// The Boss-conversion and FM-emergence paths that used to live here are GONE, subsumed by the
	// format migration above. Both were describing the same thing the mix curve now describes -- the
	// FullService->Top40 collapse and the 1967+ arrival of underground FM -- but as independent
	// mechanics they could not be reconciled against a national target, and the FM path physically
	// ADDED stations (77 -> 91 by 1969), so the panel changed size mid-decade. The panel is now a
	// fixed-size sample whose COMPOSITION tracks the dial. RadioEra.BossRadioAdoption/FmViability
	// remain as the authored climate; ConvertStation applies the Boss character change on the
	// FullService->Top40 leg.
	// ====================================================================
	private void RollDjGreedAndScandals(List<RadioStation> roster, MarketRegion region, int year) {
		if (roster == null) return;
		float heat = RadioEra.RegulatoryHeat(year);
		foreach (RadioStation s in roster) {
			Deejay dj = GetDeejay(s.leadDjId);
			if (dj == null) continue;
			// Greed drifts a little year to year.
			dj.greed = Mathf.Clamp(dj.greed + (rng.Randf() - 0.5f) * 0.1f, 0f, 1f);
			// A very greedy jock draws scrutiny; the sack chance is small and peaks with the crackdown.
			if (dj.greed > 0.8f) {
				float sackChance = heat * 0.04f * (dj.greed - 0.8f) / 0.2f;   // <=~0.036/yr at the 1960 peak
				if (rng.Randf() < sackChance) SackDeejay(s, region);
			}
		}
	}

	/// <summary>Remove a DJ (payola bust / era churn) and install a replacement, resetting the station's
	/// relationships built with the departed jock. Cheaper, lower-greed successor by default.</summary>
	public void SackDeejay(RadioStation station, MarketRegion region) {
		if (station == null) return;
		if (station.leadDjId != null) djsById.Remove(station.leadDjId);
		Deejay replacement = CreateDeejay(station, region);
		replacement.greed = Mathf.Min(replacement.greed, 0.4f);   // a burned station hires cautious
		station.leadDjId = replacement.djId;
		djsById[replacement.djId] = replacement;
		station.rt?.labelRapport.Clear();   // rapport was with the departed jock
	}

	/// <summary>Sack overload for callers without a MarketRegion handle (CreateDeejay does not need it).</summary>
	public void SackDeejay(RadioStation station) => SackDeejay(station, null);
}
