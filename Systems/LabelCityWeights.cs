using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// --label-city-weights (ProceduralLabelCityWeightingHandoff.md): places procedural labels, opening roster and runtime
/// births, by a city-weight table instead of the uniform pickers. One <c>GameRandom.Randf()</c> per placement.
///
/// <b>General weights (sourced).</b> Firms with national chart reach in 1960-61 that are not one of the 43 historical
/// templates or their imprints: every independent with a national Top 10 record in 1960 or 1961
/// (SimLogs/historical-top10-1960, -1961, labels from the Wikipedia infobox / Billboard print), plus every label on
/// both the KDWB (Minneapolis) and KFWB (Los Angeles) 1960 surveys (SimLogs/historical-station-surveys). Imprints merge
/// into their firm (Carlton/Guaranteed, Kapp/Leader, End/Gone, Time/Shad/Brent, Vista/Buena Vista). Labels whose city
/// could not be placed with confidence are left out (University, Terrace, Milestone, Republic, Audicon). 46 firms:
/// New York 24 (Cadence, Carlton, Top Rank, Laurie, Old Town, Herald, End, Roulette, Kapp, Dunes, Scepter, Coed,
/// Beltone, Big Top, Canadian-American, Warwick, Hull, Madison, Everest, Sue, Time, Shell, Promo, Heritage); Los Angeles
/// 12 (Signet, Highland, Hi-Fi, Gregmark, Keen, Challenge, Dore, Eldo, Era, Lute, Indigo, Vista); Philadelphia 3 (Jamie,
/// Swan, V-Tone); New Orleans 2 (Minit, Instant); Memphis 1 (Hi); Chicago 1 (Chief); Seattle 1 (Dolton); Minneapolis 1
/// (Soma); Norfolk 1 (Legrand, placed at Washington D.C., the nearest catalog city in the same region). Rule: weight =
/// firms + 0.5, so every catalog city keeps some regional operators. The Deep South gets 9.0% of the general weight
/// (5.5 of 61).
///
/// <b>Archetype tilts (authored).</b> Four archetypes that the old picker sent to a "southern" list or to Nashville
/// alone get their own tables, from the cities their 1950s-60s independents worked in. The other archetypes keep their
/// existing placement (SoulFactory, TeenHitMachine, FolkBoutique, JazzPrestige).
/// </summary>
public static class LabelCityWeights {
	public const string Switch = "--label-city-weights";
	public static bool Enabled => GameyAuditArguments.Has(Switch);

	internal static readonly IReadOnlyList<(string city, float weight)> General = new (string, float)[] {
		// Newark is left out: it has no DistanceModel node, so a runtime birth there would resolve unmapped.
		("New York", 24.5f), ("Philadelphia", 3.5f), ("Boston", .5f), ("Pittsburgh", .5f), ("Baltimore", .5f),
		("Washington D.C.", 1.5f),
		("Chicago", 1.5f), ("Detroit", .5f), ("Cleveland", .5f), ("Cincinnati", .5f),
		("Minneapolis", 1.5f), ("St. Louis", .5f), ("Kansas City", .5f), ("Omaha", .5f),
		("Memphis", 1.5f), ("Nashville", .5f), ("New Orleans", 2.5f), ("Atlanta", .5f), ("Miami", .5f),
		("Houston", .5f), ("Dallas", .5f), ("San Antonio", .5f), ("Phoenix", .5f), ("Albuquerque", .5f),
		("Denver", .5f), ("Salt Lake City", .5f), ("Billings", .5f),
		("Los Angeles", 12.5f), ("San Francisco", .5f), ("Seattle", 1.5f)
	};

	// Country independents: Nashville (Starday, Hickory, Monument, Nashboro's country line), Los Angeles (Fabor,
	// Challenge, Crest), Cincinnati (King's hillbilly catalogue), Houston (D, Starday's Texas origin), Texas and the
	// Southwest (Sarg, Ramco), Memphis (Sun's country sides).
	internal static readonly IReadOnlyList<(string city, float weight)> Country = new (string, float)[] {
		("Nashville", .45f), ("Los Angeles", .15f), ("Cincinnati", .10f), ("Houston", .10f), ("Dallas", .05f),
		("San Antonio", .05f), ("Phoenix", .05f), ("Memphis", .05f)
	};
	// Blues and R&B independents: Chicago (Chess, Vee-Jay, Cobra, Chief), Los Angeles (Modern/Kent/RPM, Aladdin,
	// Specialty), New York (Fire/Fury, Herald/Ember), Detroit (Fortune), Houston (Duke), Memphis, New Orleans, Nashville.
	internal static readonly IReadOnlyList<(string city, float weight)> Blues = new (string, float)[] {
		("Chicago", .35f), ("Los Angeles", .20f), ("New York", .10f), ("Detroit", .10f), ("Houston", .10f),
		("Memphis", .05f), ("New Orleans", .05f), ("Nashville", .05f)
	};
	// Gospel: Chicago (Vee-Jay, Chess/Checker), New York and Newark (Savoy, Gotham), Los Angeles (Specialty),
	// Houston (Peacock), Nashville (Nashboro), Detroit (JVB, HOB), Memphis, Atlanta.
	internal static readonly IReadOnlyList<(string city, float weight)> Gospel = new (string, float)[] {
		("Chicago", .25f), ("New York", .10f), ("Newark", .10f), ("Los Angeles", .15f), ("Houston", .10f),
		("Nashville", .10f), ("Detroit", .10f), ("Memphis", .05f), ("Atlanta", .05f)
	};
	// Rockabilly and rock and roll independents: Los Angeles (Era, Dore, Challenge, Del-Fi), Memphis (Sun, Phillips
	// International, Hi, Fernwood, Judd), New York, the Southwest (Norman Petty's Clovis sessions, Dallas, Houston),
	// Philadelphia, Minneapolis (Soma), Nashville.
	internal static readonly IReadOnlyList<(string city, float weight)> Rock = new (string, float)[] {
		("Los Angeles", .35f), ("Memphis", .15f), ("New York", .10f), ("Albuquerque", .10f), ("Dallas", .10f),
		("Houston", .05f), ("Philadelphia", .05f), ("Minneapolis", .05f), ("Nashville", .05f)
	};

	/// <summary>The table an archetype draws from, or null when it keeps its legacy placement.</summary>
	internal static IReadOnlyList<(string city, float weight)> TableFor(LabelArchetype? archetype) => archetype switch {
		LabelArchetype.CountrySpecialist => Country,
		LabelArchetype.BluesRoots => Blues,
		LabelArchetype.GospelPowerhouse => Gospel,
		LabelArchetype.RockRebel => Rock,
		LabelArchetype.SoulFactory or LabelArchetype.TeenHitMachine or LabelArchetype.FolkBoutique or LabelArchetype.JazzPrestige => null,
		_ => General
	};

	/// <summary>One draw against the cumulative table.</summary>
	internal static string Pick(IReadOnlyList<(string city, float weight)> table) => PickAt(table, GameRandom.Randf());

	internal static string PickAt(IReadOnlyList<(string city, float weight)> table, float roll) {
		float total = table.Sum(entry => entry.weight), target = roll * total, cumulative = 0f;
		foreach (var (city, weight) in table) {
			cumulative += weight;
			if (target < cumulative) return city;
		}
		return table[table.Count - 1].city;
	}

	/// <summary>Share of a table's weight in each region, through the factory's region map.</summary>
	internal static Dictionary<string, float> RegionShares(IReadOnlyList<(string city, float weight)> table, Func<string, string> region) {
		float total = table.Sum(entry => entry.weight);
		return table.GroupBy(entry => region(entry.city)).ToDictionary(g => g.Key, g => g.Sum(entry => entry.weight) / total);
	}
}
