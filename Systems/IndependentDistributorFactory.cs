using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;

/// <summary>
/// Builds the regional independent-distribution layer from the authored
/// <see cref="DistributionNetwork"/> already attached to every
/// <see cref="MarketRegion"/> in chart_manager.tscn. That resource carries real
/// retail infrastructure per region -- record store counts, department store
/// counts, inventory depth, difficulty, and the hasIndieDistribution /
/// hasOneStopDistributors flags -- and until now no code read a single field of
/// it. Deriving the layer from authored data rather than a new invented constant
/// keeps the geography of independent distribution consistent with the geography
/// the rest of the model already uses.
///
/// Generation runs on its own seeded stream and touches no label, region or record
/// state, so introducing it cannot perturb the global RNG order (handoff section 12).
/// </summary>
public static class IndependentDistributorFactory {
	// "indiedis" -- stable namespace, mirroring ArtistManager's population stream.
	private const ulong StreamNamespace = 0x696e646965646973UL;

	// A region's house count scales with its authored retail depth. The floor of two
	// keeps any market from depending on a single house; the ceiling reflects that a
	// region here aggregates several real trade markets. Across the authored map this
	// yields roughly two dozen houses nationally, in the historical range once the
	// seven-region granularity is accounted for.
	private const int MinimumHousesPerRegion = 2;
	private const int MaximumHousesPerRegion = 6;
	private const float RecordStoresPerHouse = 150f;

	// Deliberately generous. Independent distribution was not the scarce input in the
	// 1960s -- a hit was. The 24-client major ceiling saturated on every seed and froze
	// the market for a decade (section 32.2); this capacity is instrumented so the same
	// mistake is visible rather than silent.
	private const float BaseClientCapacity = 24f;
	private const float DepthClientCapacity = 26f;

	// Family names and trade words in the idiom of the period's wholesale houses. The pool
	// is larger than the map needs so each house can take its own name.
	private static readonly string[] HouseSurnames = {
		"Schwartz", "Heilicher", "Mangold", "Godwin", "Cosnat", "Malverne", "Seaboard",
		"Bertos", "Chatton", "Delta", "Summit", "Arc", "Pan American", "Standard",
		"Zamoiski", "Marnel", "Leslie", "Beacon", "Commercial", "Allied", "Southland",
		"Big State", "Music City", "Lakeshore", "Gateway", "Tidewater", "Piedmont",
		"Crescent", "Harborlight", "Meridian"
	};

	private static readonly string[] HouseSuffixes = {
		"Brothers", "Distributing", "Distributors", "Record Sales", "Music Sales", "One-Stop"
	};

	/// <summary>
	/// Pure entry point: no singletons, no global RNG, deterministic in
	/// (regions, seed). Probes call this directly without a running simulation.
	/// </summary>
	public static List<IndependentDistributor> Generate(IEnumerable<MarketRegion> regions, ulong seed) {
		var houses = new List<IndependentDistributor>();
		if (regions == null) return houses;

		var rng = new RandomNumberGenerator { Seed = seed ^ StreamNamespace };
		// Draw surnames from a shuffled pool so each house is its own firm rather than a
		// variant of the same name, which is what independent sampling produced.
		string[] surnamePool = ShuffledSurnames(rng);
		int surnameCursor = 0;
		var usedNames = new HashSet<string>(StringComparer.Ordinal);
		int idCounter = 0;

		foreach (MarketRegion region in regions) {
			DistributionNetwork network = region?.distribution;
			// A region authored without an independent distribution trade -- the Rockies --
			// has no houses at all. Labels there must reach an adjacent market's houses,
			// which is the geographic diffusion the historical model describes.
			if (region == null || network == null || !network.hasIndieDistribution) continue;

			int houseCount = HouseCountFor(network);
			for (int index = 0; index < houseCount; index++) {
				houses.Add(new IndependentDistributor {
					distributorId = $"indiedist_{++idCounter:D3}",
					distributorName = NextHouseName(rng, region, surnamePool, ref surnameCursor, usedNames),
					regionId = region.regionId,
					clientCapacity = ClientCapacityFor(network),
					reliability = ReliabilityFor(network, rng),
					paymentTermWeeks = PaymentTermWeeksFor(rng),
					returnAllowance = ReturnAllowanceFor(network, rng),
					reportingHonesty = ReportingHonestyFor(network, rng)
				});
			}
		}
		return houses;
	}

	internal static int HouseCountFor(DistributionNetwork network) {
		if (network == null || !network.hasIndieDistribution) return 0;
		int scaled = MinimumHousesPerRegion +
			Mathf.RoundToInt(Mathf.Max(0, network.recordStoreCount) / RecordStoresPerHouse);
		return Mathf.Clamp(scaled, MinimumHousesPerRegion, MaximumHousesPerRegion);
	}

	internal static int ClientCapacityFor(DistributionNetwork network) =>
		Mathf.Max(1, Mathf.RoundToInt(BaseClientCapacity +
			(Mathf.Clamp(network?.inventoryDepth ?? 0f, 0f, 1f) * DepthClientCapacity)));

	// Payment behaviour tracks authored market difficulty. Houses in thin, hard markets
	// were the notoriously slow payers, which is what turned a regional hit into a cash
	// crisis for the label that had already paid the pressing plant.
	private static float ReliabilityFor(DistributionNetwork network, RandomNumberGenerator rng) =>
		Mathf.Clamp(0.85f - (Difficulty(network) * 0.45f) + rng.RandfRange(-0.08f, 0.08f), 0.25f, 0.95f);

	// 90-120 day terms.
	private static int PaymentTermWeeksFor(RandomNumberGenerator rng) => rng.RandiRange(12, 18);

	private static float ReturnAllowanceFor(DistributionNetwork network, RandomNumberGenerator rng) =>
		Mathf.Clamp(0.15f + (Difficulty(network) * 0.15f) + rng.RandfRange(-0.03f, 0.03f), 0.10f, 0.35f);

	private static float ReportingHonestyFor(DistributionNetwork network, RandomNumberGenerator rng) =>
		Mathf.Clamp(0.92f - (Difficulty(network) * 0.40f) + rng.RandfRange(-0.06f, 0.06f), 0.40f, 0.99f);

	private static float Difficulty(DistributionNetwork network) =>
		Mathf.Clamp(network?.difficulty ?? 0f, 0f, 1f);

	private static string[] ShuffledSurnames(RandomNumberGenerator rng) {
		var pool = (string[])HouseSurnames.Clone();
		for (int index = pool.Length - 1; index > 0; index--) {
			int swap = rng.RandiRange(0, index);
			(pool[index], pool[swap]) = (pool[swap], pool[index]);
		}
		return pool;
	}

	// Self-contained naming. NameGenerator drives label identity; drawing from it here
	// would shift that stream and change which labels exist.
	private static string NextHouseName(RandomNumberGenerator rng, MarketRegion region,
		string[] surnamePool, ref int cursor, HashSet<string> used) {
		string suffix = HouseSuffixes[rng.RandiRange(0, HouseSuffixes.Length - 1)];
		if (cursor < surnamePool.Length) {
			string candidate = $"{surnamePool[cursor++]} {suffix}";
			if (used.Add(candidate)) return candidate;
		}
		// More houses than surnames: fall back to a market-qualified name rather than
		// reusing a firm name in a second city.
		string fallback = $"{region?.regionName ?? "Regional"} {suffix}";
		if (used.Add(fallback)) return fallback;
		string numbered = $"{fallback} {used.Count.ToString(CultureInfo.InvariantCulture)}";
		used.Add(numbered);
		return numbered;
	}
}
