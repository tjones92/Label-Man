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

	public void BuildRosters(IEnumerable<MarketRegion> regions, int year) {
		currentYear = year;
		stationsByRegion.Clear();
		stationsById.Clear();
		djsById.Clear();
		tailFormatReachByRegion.Clear();

		foreach (MarketRegion region in regions) {
			var roster = GenerateRegionRoster(region, year);
			stationsByRegion[region.regionId] = roster;
			foreach (RadioStation s in roster) stationsById[s.stationId] = s;
			RebuildTailFormatReach(region, year);
		}
	}

	/// <summary>Applies era events (Boss conversion, FM emergence) without rebuilding rosters, so
	/// player-cultivated relationships persist across years.</summary>
	public void OnYearChanged(IEnumerable<MarketRegion> regions, int year) {
		currentYear = year;
		foreach (MarketRegion region in regions) {
			ApplyEraEvents(region, year);
			RebuildTailFormatReach(region, year);
		}
	}

	private List<RadioStation> GenerateRegionRoster(MarketRegion region, int year) {
		int slots = ReporterCountForTier(region.tier);
		var formatCounts = AllocateFormats(region, year, slots);

		var roster = new List<RadioStation>(slots);
		int indexInRegion = 0;
		bool west = IsWesternRegion(region);

		foreach (var (format, count) in formatCounts) {
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

	// ---- shared segment->format weighting, used by allocation AND tail-reach ----
	private static Dictionary<StationFormat, float> FormatWeightsFromSegments(MarketRegion region, int year) {
		var shares = region.segmentCapacities?.Shares;
		var weight = new Dictionary<StationFormat, float>();
		void Add(StationFormat f, float w) => weight[f] = weight.GetValueOrDefault(f) + Mathf.Max(0f, w);
		float Seg(AudienceSegment s) => shares != null && shares.TryGetValue(s, out float v) ? v : 0f;

		Add(StationFormat.Top40, Seg(AudienceSegment.MainstreamAM) + Seg(AudienceSegment.Youth) + Seg(AudienceSegment.RegionalLatin));
		Add(StationFormat.RnB, Seg(AudienceSegment.UrbanRnB));
		Add(StationFormat.Country, Seg(AudienceSegment.CountryWestern));
		Add(StationFormat.MOR, Seg(AudienceSegment.AdultMOR) + Seg(AudienceSegment.FamilyChildrens));
		Add(StationFormat.Jazz, Seg(AudienceSegment.JazzHiFiClassical));
		Add(StationFormat.Gospel, Seg(AudienceSegment.GospelChurch));
		// CollegeFolk folds into FullService early, UndergroundFM once FM is viable.
		bool fmViable = RadioEra.FmViability(year) > 0f;
		Add(fmViable ? StationFormat.UndergroundFM : StationFormat.FullService, Seg(AudienceSegment.CollegeFolk));
		if (fmViable)
			Add(StationFormat.UndergroundFM, Seg(AudienceSegment.UndergroundFM) + region.culturalProgressivism * 0.05f);
		return weight;
	}

	// ---- format allocation from the region's audience fingerprint ----
	private List<(StationFormat format, int count)> AllocateFormats(MarketRegion region, int year, int slots) {
		var weight = FormatWeightsFromSegments(region, year);

		// ---- minimum guarantees (region always has the core dial) ----
		var result = new Dictionary<StationFormat, int>();
		void Guarantee(StationFormat f, float threshold) {
			if (weight.GetValueOrDefault(f) >= threshold) result[f] = Math.Max(1, result.GetValueOrDefault(f));
		}
		result[StationFormat.Top40] = 1;                          // every region has Top 40
		Guarantee(StationFormat.RnB, 0.06f);
		Guarantee(StationFormat.Country, 0.10f);
		Guarantee(StationFormat.MOR, 0.10f);
		Guarantee(StationFormat.Gospel, 0.10f);
		Guarantee(StationFormat.Jazz, 0.08f);
		if (RadioEra.FmViability(year) > 0f) Guarantee(StationFormat.UndergroundFM, 0.02f);

		int used = result.Values.Sum();
		int remaining = Math.Max(0, slots - used);

		// ---- largest-remainder allocation of the rest by raw weight ----
		float totalWeight = weight.Values.Sum();
		if (totalWeight > 0f && remaining > 0) {
			var exact = weight.ToDictionary(kv => kv.Key, kv => kv.Value / totalWeight * remaining);
			foreach (var kv in exact) result[kv.Key] = result.GetValueOrDefault(kv.Key) + Mathf.FloorToInt(kv.Value);
			int assigned = result.Values.Sum();
			int leftover = slots - assigned;
			foreach (var f in exact.OrderByDescending(kv => kv.Value - Mathf.FloorToInt(kv.Value))
									.ThenBy(kv => (int)kv.Key).Take(Math.Max(0, leftover)))
				result[f.Key] = result.GetValueOrDefault(f.Key) + 1;
		}

		// ---- personality-era conversion: pre-1963, some Top40 become FullService ----
		if (year < 1963 && result.GetValueOrDefault(StationFormat.Top40) > 1) {
			int convert = Mathf.CeilToInt(result[StationFormat.Top40] * 0.5f);
			convert = Math.Min(convert, result[StationFormat.Top40] - 1);   // keep >=1 Top40
			result[StationFormat.Top40] -= convert;
			result[StationFormat.FullService] = result.GetValueOrDefault(StationFormat.FullService) + convert;
		}

		// Trim to exact slot count deterministically if guarantees overshot.
		var ordered = result.Where(kv => kv.Value > 0)
							.OrderByDescending(kv => kv.Value).ThenBy(kv => (int)kv.Key)
							.Select(kv => (kv.Key, kv.Value)).ToList();
		TrimToSlots(ordered, slots);
		return ordered;
	}

	private static void TrimToSlots(List<(StationFormat, int)> counts, int slots) {
		int total = counts.Sum(c => c.Item2);
		// remove from the largest non-Top40 first, never dropping Top40 below 1
		while (total > slots) {
			int idx = -1;
			for (int i = counts.Count - 1; i >= 0; i--) {
				if (counts[i].Item1 == StationFormat.Top40 && counts[i].Item2 <= 1) continue;
				if (idx < 0 || counts[i].Item2 > counts[idx].Item2) idx = i;
			}
			if (idx < 0) break;
			counts[idx] = (counts[idx].Item1, counts[idx].Item2 - 1);
			total--;
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
	// ERA EVENTS (Boss conversion, FM emergence) - climate from RadioEra
	// ====================================================================
	private void ApplyEraEvents(MarketRegion region, int year) {
		if (!stationsByRegion.TryGetValue(region.regionId, out var roster)) return;

		// Boss Radio conversion - climate from RadioEra, roll per station, record RESULT on station.
		float convertChance = RadioEra.BossConversionChance(year, region.tier);
		if (convertChance > 0f) {
			foreach (RadioStation s in roster.Where(s => s.format is StationFormat.Top40 or StationFormat.FullService)) {
				if (s.djAutonomy > 0.2f && rng.Randf() < convertChance) {
					s.format = StationFormat.Top40;
					s.djAutonomy = 0.1f;
					s.highSlots = 8; s.midSlots = 10; s.lightSlots = 12;
					Deejay dj = GetDeejay(s.leadDjId);
					if (dj != null && dj.archetype == DJArchetype.Personality) dj.archetype = DJArchetype.CompanyMan;
				}
			}
		}

		// FM emergence - viability + region character from RadioEra.
		if (RadioEra.FmReporterViable(year, region.tier, region.culturalProgressivism)) {
			int fmCount = roster.Count(s => s.format == StationFormat.UndergroundFM);
			int targetFm = year >= 1969 && region.tier == RegionTier.Major ? 2 : 1;
			for (int i = fmCount; i < targetFm; i++) {
				bool west = IsWesternRegion(region);
				RadioStation fm = CreateStation(region, StationFormat.UndergroundFM, flagship: false, west, roster.Count);
				Deejay dj = CreateDeejay(fm, region);
				fm.leadDjId = dj.djId;
				djsById[dj.djId] = dj;
				stationsById[fm.stationId] = fm;
				roster.Add(fm);
			}
			NormalizeReporterReachShares(roster);
		}

		// Rare autonomous DJ churn: greed drifts, and a very-greedy jock can be caught and sacked in a
		// payola scandal (Alan Freed, 1962) -- scaled by the era's regulatory heat. Kept rare so it
		// barely touches the simulation; uses the network RNG, never the global stream.
		RollDjGreedAndScandals(roster, region, year);
	}

	private void RollDjGreedAndScandals(List<RadioStation> roster, MarketRegion region, int year) {
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
