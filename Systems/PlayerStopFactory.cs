using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Builds the player's named-account layer: a small, legible roster of record shops and jukebox
/// operators per city, derived from the same authored retail data the AI independent-distribution
/// layer reads (record store counts, hub/tier flags, regional jukebox counts -- see
/// IndependentDistributorFactory, which this mirrors). Generation runs on its own seeded stream and
/// touches no label, region or record state, so introducing it cannot perturb the global RNG order.
///
/// Only identity is generated here (id, name, city, kind) -- the mutable relationship/stock/balance
/// state lives on PlayerDesk.PlayerStop and is captured/restored by PlayerSaveData, keyed by the
/// stable StopId this factory produces. Regenerating identity fresh each session, rather than saving
/// it, means a stop's name never has to round-trip through JSON and the roster stays a fixed function
/// of the world seed.
/// </summary>
public static class PlayerStopFactory {
	// "plystops" -- stable namespace, isolated from IndependentDistributorFactory's "indiedis" stream.
	private const ulong StreamNamespace = 0x706c7973746f7073UL;

	// Directive §3.1: "a 1960 hometown start ~ 6-12 shops you can actually meet, 1-3 ops... a hub has
	// more." Never the raw recordStoreCount (a big metro's is in the hundreds) -- that number is
	// coverage math for the store engine, not a playable roster.
	private const int MinShopsPerCity = 6;
	private const int MaxShopsPerCity = 12;
	private const int HubShopBonus = 6;
	private const int TierShopBonus = 2;

	private const int MinOpsPerCity = 1;
	private const int MaxOpsPerCity = 3;
	private const int BigJukeboxMarket = 4000; // region-level jukeboxCount above which a city rates a second op

	// Directive §3.1: "Hop/club/church table -- genre + churchNetworkStrength + youthPercentage +
	// concertVenueCount -- retail-at-the-door; early soul/gospel/teen." Weighted off the region, not a
	// flat per-city count like Shop/Op -- a city with no youth culture and no church network legitimately
	// rates zero, where a hub with both can carry as many as a hub carries shops-with-hub-bonus.
	private const int MaxVenuesPerCity = 4;
	private const float VenueConcertWeight = 0.15f;
	private const int VenueConcertCap = 12; // diminishing return past a dozen authored concert venues

	private static readonly string[] ShopSurnames = {
		"Kessler", "Voss", "Randall", "Buford", "Alcott", "Deveraux", "Prather", "Ostrander",
		"Callum", "Whitfield", "Marchetti", "Landry", "Gunderson", "Halloran", "Pruitt", "Segal",
		"Tibbets", "Yarrow", "Costa", "Underhill", "Belknap", "Sorrells", "Novak", "Faraday",
		"Odom", "Petrie", "Vance", "Loomis", "Kittredge", "Marsh"
	};

	private static readonly string[] ShopSuffixes = {
		"Music Shop", "Record Mart", "Records", "Music Center", "Music Store", "Melody Shop",
		"Disc Shop", "Sound Shop", "Music Box", "Platter Shop"
	};

	private static readonly string[] OpWords = {
		"Consolidated", "Regal", "Starlite", "Diamond", "Cardinal", "Liberty", "Trojan", "Rex",
		"Vantage", "Coronet", "Empire", "Continental", "Ace", "Rainbow", "Victory", "Keystone",
		"Sterling", "Paramount"
	};

	private static readonly string[] OpSuffixes = {
		"Amusement Co.", "Music Machines", "Coin Machine Co.", "Vending Co.", "Novelty Co.", "Amusement Service"
	};

	private static readonly string[] OneStopSuffixes = {
		"One-Stop", "Record Distributing Co.", "Record Service", "Distributing Co.", "Record Supply"
	};

	// Two flavors of the same StopKind.Venue -- a church hall and a hop/club draw the same "retail at the
	// door" verb but read differently on the day-sheet. Generic period-idiom names, invented rather than
	// drawn from any real congregation or club, the same discipline as ShopSurnames/OpWords above.
	private static readonly string[] HallWords = {
		"Mount Zion", "Shiloh", "Bethel", "New Hope", "Mount Olive", "Ebenezer", "Pilgrim", "Calvary",
		"Trinity", "St. Luke", "Mount Carmel", "Gethsemane", "Bright Star", "Second Chance"
	};

	private static readonly string[] HallSuffixes = {
		"Fellowship Hall", "Tabernacle", "Community Hall", "Family Hall", "Hall"
	};

	private static readonly string[] ClubWords = {
		"Starlight", "Rainbow", "Blue Note", "Satin", "Velvet", "Rhythm", "Uptown", "Downtown", "Silver",
		"Palace", "Cabana", "Ember"
	};

	private static readonly string[] ClubSuffixes = {
		"Ballroom", "Lounge", "Canteen", "Rec Hall", "Sock Hop", "Teen Club"
	};

	/// <summary>
	/// Pure entry point: no singletons, no global RNG, deterministic in (cities, regions, seed).
	/// </summary>
	public static List<PlayerDesk.PlayerStop> Generate(
		IEnumerable<MarketCity> cities, IReadOnlyDictionary<string, MarketRegion> regionsById, ulong seed) {
		var result = new List<PlayerDesk.PlayerStop>();
		if (cities == null) return result;

		var rng = new RandomNumberGenerator { Seed = seed ^ StreamNamespace };
		// Draw from shuffled pools so two accounts in the same city don't collide on name.
		string[] shopPool = Shuffled(ShopSurnames, rng);
		string[] opPool = Shuffled(OpWords, rng);
		string[] hallPool = Shuffled(HallWords, rng);
		string[] clubPool = Shuffled(ClubWords, rng);
		int shopCursor = 0, opCursor = 0, hallCursor = 0, clubCursor = 0;
		var usedNames = new HashSet<string>(StringComparer.Ordinal);

		foreach (MarketCity city in cities.OrderBy(c => c.cityId, StringComparer.Ordinal)) {
			if (city == null || string.IsNullOrEmpty(city.cityId)) continue;

			int shopCount = Mathf.Clamp(
				MinShopsPerCity + (city.isRegionalHub ? HubShopBonus : 0) + Mathf.Max(0, city.distributionTier) * TierShopBonus,
				MinShopsPerCity, MaxShopsPerCity + HubShopBonus + TierShopBonus * 4);
			for (int i = 0; i < shopCount; i++) {
				string name = NextUniqueName(shopPool, ShopSuffixes, possessive: true, ref shopCursor, usedNames, rng);
				// Promo mechanic directive §7.1: "one or two per city" -- the biggest dealer(s), fixed by
				// index rather than rolled, so the roster is legible and never shifts session to session.
				// A hub gets a second; every other city gets exactly one.
				bool reports = i == 0 || (i == 1 && city.isRegionalHub);
				result.Add(new PlayerDesk.PlayerStop {
					StopId = $"{city.cityId}_shop_{i}", DisplayName = name,
					CityId = city.cityId, Kind = PlayerDesk.StopKind.Shop,
					ReportsToTrades = reports,
				});
			}

			regionsById.TryGetValue(city.parentRegionId, out MarketRegion region);
			int jukeboxCount = region?.media?.jukeboxCount ?? 0;
			int opCount = Mathf.Clamp(
				MinOpsPerCity + (jukeboxCount >= BigJukeboxMarket ? 1 : 0) + (city.isRegionalHub ? 1 : 0),
				MinOpsPerCity, MaxOpsPerCity);
			for (int i = 0; i < opCount; i++) {
				string name = NextUniqueName(opPool, OpSuffixes, possessive: false, ref opCursor, usedNames, rng);
				result.Add(new PlayerDesk.PlayerStop {
					StopId = $"{city.cityId}_op_{i}", DisplayName = name,
					CityId = city.cityId, Kind = PlayerDesk.StopKind.Op
				});
			}

			// Directive §3.1/§6: "one-stop counter -- MarketCity.distribution.hasOneStopDistributors --
			// locked as a customer until inbound demand exists." One per eligible city, named off the same
			// word pool as an op (period trade idiom -- "the one-stop in the back of the appliance store"
			// notwithstanding, this is the metro counter, not a novelty shop front).
			if (city.distribution != null && city.distribution.hasOneStopDistributors) {
				string oneStopName = NextUniqueName(opPool, OneStopSuffixes, possessive: false, ref opCursor, usedNames, rng);
				result.Add(new PlayerDesk.PlayerStop {
					StopId = $"{city.cityId}_onestop", DisplayName = oneStopName,
					CityId = city.cityId, Kind = PlayerDesk.StopKind.OneStop,
					// Directive §7.1: "the one-stop, and the one or two biggest dealers in a hub" -- a
					// metro one-stop always reports to the trades, on top of whichever shops do.
					ReportsToTrades = true,
				});
			}

			// Directive §3.1: hop/club/church table. Weighted off the region's church network, youth share
			// and authored concert-venue count -- unlike Shop/Op there is no floor, so a city with none of
			// those legitimately gets zero tables, and a hub strong in all three tops out at MaxVenuesPerCity.
			float church = region?.churchNetworkStrength ?? 0.25f;
			float youth = region?.youthPercentage ?? 0.2f;
			int concertVenues = region?.media?.concertVenueCount ?? 0;
			int venueCount = Mathf.Clamp(
				Mathf.RoundToInt(church * 3f + youth * 4f + Mathf.Min(concertVenues, VenueConcertCap) * VenueConcertWeight),
				0, MaxVenuesPerCity);
			for (int i = 0; i < venueCount; i++) {
				// Church-hall flavor vs. hop/club flavor, weighted by the same two terms that set the count --
				// a strong church network reads as more halls, a young population as more clubs/hops.
				bool hallFlavor = rng.Randf() < church / Mathf.Max(0.05f, church + youth);
				string name = hallFlavor
					? NextUniqueName(hallPool, HallSuffixes, possessive: false, ref hallCursor, usedNames, rng)
					: NextUniqueName(clubPool, ClubSuffixes, possessive: false, ref clubCursor, usedNames, rng);
				result.Add(new PlayerDesk.PlayerStop {
					StopId = $"{city.cityId}_venue_{i}", DisplayName = name,
					CityId = city.cityId, Kind = PlayerDesk.StopKind.Venue
				});
			}
		}
		return result;
	}

	private static string NextUniqueName(string[] wordPool, string[] suffixes, bool possessive,
		ref int cursor, HashSet<string> used, RandomNumberGenerator rng) {
		for (int attempt = 0; attempt < wordPool.Length; attempt++) {
			string word = wordPool[cursor % wordPool.Length];
			cursor++;
			string suffix = suffixes[rng.RandiRange(0, suffixes.Length - 1)];
			string name = possessive ? $"{word}'s {suffix}" : $"{word} {suffix}";
			if (used.Add(name)) return name;
		}
		// Pool exhausted (a big hub outran the word list) -- a numbered fallback rather than a collision.
		string fallbackWord = wordPool[cursor % wordPool.Length];
		string fallbackSuffix = suffixes[rng.RandiRange(0, suffixes.Length - 1)];
		string fallback = possessive ? $"{fallbackWord}'s {fallbackSuffix} #{cursor}" : $"{fallbackWord} {fallbackSuffix} #{cursor}";
		cursor++;
		used.Add(fallback);
		return fallback;
	}

	// Directive §10: "Jukebox weight is an explicit down-curve on JukeboxOp across the decade; ops crater
	// by 1966-67." The op roster itself is generated once per session and never regrows or shrinks (see
	// Generate above) -- what changes year to year is how much route business a given op still has to
	// place, which is a runtime quantity, not an identity one. Keyframed the same way MarketRegion's
	// integration curve is (year, weight) pairs with a linear lerp between them) so the shape reads next
	// to that curve: flat through the British Invasion, then the same 1965-67 collapse that killed the
	// jukebox trade as bars and diners went to home stereo and FM.
	private static readonly (float Year, float Weight)[] JukeboxEraCurve = {
		(1960f, 1.00f), (1963f, 1.00f), (1964f, 0.90f), (1965f, 0.80f),
		(1966f, 0.60f), (1967f, 0.40f), (1968f, 0.28f), (1969f, 0.20f),
	};

	/// <summary>How much of an operator's usual route business is still there in a given year -- multiplies
	/// the quantity an <see cref="PlayerDesk.StopKind.Op"/> account will take, never its identity or count.</summary>
	public static float JukeboxEraWeight(float year) {
		var curve = JukeboxEraCurve;
		if (year <= curve[0].Year) return curve[0].Weight;
		if (year >= curve[^1].Year) return curve[^1].Weight;
		for (int i = 1; i < curve.Length; i++) {
			if (year <= curve[i].Year) {
				float t = (year - curve[i - 1].Year) / (curve[i].Year - curve[i - 1].Year);
				return Mathf.Lerp(curve[i - 1].Weight, curve[i].Weight, t);
			}
		}
		return curve[^1].Weight;
	}

	// Promo mechanic directive §8: "hops are strongest 1960-63, fade through the mid-decade, and are
	// largely gone by 1967-68 -- the same shape as JukeboxEraCurve." Authored as a second keyframed
	// curve right next to that one so the two read together -- the payola-hearings-legal DJ relationship
	// and the jukebox trade both belong to the same early-60s road, and both get swept away by the same
	// mid-decade shift to home stereo, FM and the tightening Boss-radio playlist.
	private static readonly (float Year, float Weight)[] HopEraCurve = {
		(1960f, 1.00f), (1963f, 1.00f), (1964f, 0.85f), (1965f, 0.65f),
		(1966f, 0.40f), (1967f, 0.22f), (1968f, 0.12f), (1969f, 0.08f),
	};

	/// <summary>How much a hop is still worth in a given year -- scales BookRecordHop's advocacy and
	/// awareness payoff, never its availability outright: a hop late in the decade is a lesser move, not
	/// an impossible one.</summary>
	public static float HopEraWeight(float year) {
		var curve = HopEraCurve;
		if (year <= curve[0].Year) return curve[0].Weight;
		if (year >= curve[^1].Year) return curve[^1].Weight;
		for (int i = 1; i < curve.Length; i++) {
			if (year <= curve[i].Year) {
				float t = (year - curve[i - 1].Year) / (curve[i].Year - curve[i - 1].Year);
				return Mathf.Lerp(curve[i - 1].Weight, curve[i].Weight, t);
			}
		}
		return curve[^1].Weight;
	}

	private static string[] Shuffled(string[] source, RandomNumberGenerator rng) {
		string[] pool = (string[])source.Clone();
		for (int i = pool.Length - 1; i > 0; i--) {
			int j = rng.RandiRange(0, i);
			(pool[i], pool[j]) = (pool[j], pool[i]);
		}
		return pool;
	}
}
