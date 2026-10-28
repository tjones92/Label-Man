using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>
/// Regional genre taste built from places (SimTools/RegionalSyncDesign.md §9b). Each <see cref="DistanceModel"/> city and
/// each region's "rest of region" remainder carries specialist audience shares -- Black (UrbanRnB), country-western
/// listening (CountryWestern), Latin (RegionalLatin). A genre's mass in a place is its segment routing read against
/// those shares relative to the nation; a region's taste is the population-weighted mean of its places. Region taste
/// multiplies single conversion; a reporter station's own city taste multiplies its candidacy. Both are normalized to a
/// national population-weighted mean of 1 per genre, so national genre totals hold to first order. Fixed inputs, no draws.
/// Off unless <c>--regional-genre-taste=K</c> (K &gt; 0, the exponent on place mass).
/// </summary>
public static class RegionalGenreTaste {
	private const string Prefix = "--regional-genre-taste=";
	private const float SecondaryWeight = .20f;
	private const float BlackShareFloor = .005f;
	private static bool configured;
	public static float K { get; private set; }
	public static bool Enabled => K > 0f;

	/// <summary>A city's 1960 metro population (millions) and specialist audience shares. Authored approximations of
	/// 1960 census SMSA totals and nonwhite shares, Spanish-surname/Puerto Rican shares, and a judgement of
	/// country-western listening; not sourced row by row. Miami's Latin share rises with Cuban emigration after 1959.</summary>
	private readonly record struct City(string Id, float Population, float Black, float Country, float Latin, float LatinPerYear = 0f, float LatinCap = 0f);

	private static readonly City[] Cities = {
		new("new_york", 10.7f, .12f, .05f, .07f), new("boston", 2.6f, .03f, .05f, .005f), new("philadelphia", 4.3f, .155f, .06f, .01f),
		new("baltimore", 1.7f, .22f, .10f, .005f), new("washington", 2.0f, .24f, .10f, .01f), new("pittsburgh", 2.4f, .07f, .08f, .005f),
		new("chicago", 6.2f, .145f, .08f, .03f), new("detroit", 3.8f, .15f, .10f, .01f), new("cleveland", 1.8f, .14f, .08f, .01f),
		new("cincinnati", 1.07f, .12f, .12f, .005f), new("minneapolis", 1.48f, .015f, .10f, .005f), new("st_louis", 2.1f, .145f, .12f, .005f),
		new("kansas_city", 1.04f, .11f, .15f, .02f), new("omaha", .46f, .05f, .15f, .01f), new("nashville", .40f, .19f, .25f, .005f),
		new("memphis", .63f, .36f, .18f, .005f), new("atlanta", 1.02f, .23f, .15f, .005f), new("new_orleans", .87f, .31f, .10f, .02f),
		new("miami", .94f, .15f, .08f, .05f, .019f, .24f), new("dallas", 1.08f, .14f, .20f, .05f), new("houston", 1.24f, .20f, .18f, .07f),
		new("san_antonio", .69f, .07f, .15f, .41f), new("phoenix", .66f, .04f, .15f, .15f), new("albuquerque", .26f, .02f, .15f, .25f),
		new("denver", .93f, .04f, .15f, .09f), new("salt_lake_city", .38f, .01f, .15f, .03f), new("billings", .08f, .005f, .25f, .02f),
		new("los_angeles", 6.04f, .085f, .10f, .10f), new("san_francisco", 2.78f, .10f, .06f, .07f), new("seattle", 1.1f, .04f, .10f, .01f),
		new("portland", .82f, .025f, .10f, .01f),
	};

	/// <summary>Country-western listening and Latin share in each region's towns and farms (authored).</summary>
	private static readonly Dictionary<string, (float Country, float Latin)> Remainders = new(StringComparer.Ordinal) {
		["eastcoast"] = (.12f, .01f), ["greatlakes"] = (.15f, .01f), ["greatplains"] = (.22f, .01f), ["westcoast"] = (.15f, .08f),
		["deepsouth"] = (.25f, .01f), ["rockies"] = (.22f, .08f), ["southwest"] = (.25f, .15f),
	};

	/// <summary>Every place (31 cities and seven "rest of region" remainders, located at the region hub), for
	/// <see cref="CityReach"/>: id, region, the <see cref="DistanceModel"/> city it sits at, population (millions).</summary>
	internal static IReadOnlyList<(string Id, string RegionId, string CityId, float Population)> Places() {
		EnsureBuilt();
		return places.Select(p => (p.Id, p.RegionId, p.CityId, p.Population)).ToList();
	}

	private sealed class Place {
		public string Id, RegionId, CityId; public float Population, Black, Country, Latin, LatinPerYear, LatinCap;
		public float LatinIn(int year) => LatinPerYear > 0f ? Math.Min(LatinCap, Latin + LatinPerYear * Math.Max(0, year - 1960)) : Latin;
	}

	private static List<Place> places;
	private static Dictionary<string, Place> cityByName;
	private static readonly Dictionary<(Genre, Genre, string, int), float> regionCache = new();
	private static readonly Dictionary<(Genre, string, int), float> cityCache = new();
	private static readonly Dictionary<(Genre, Genre, int), float> normalizer = new();

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		string raw = (arguments ?? Array.Empty<string>()).FirstOrDefault(a => a.StartsWith(Prefix, StringComparison.Ordinal))?.Substring(Prefix.Length);
		if (raw != null && (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float k) || k < 0f || k > 4f))
			throw new ArgumentException(Prefix + " takes a number from 0 to 4.");
		K = raw == null ? 0f : float.Parse(raw, CultureInfo.InvariantCulture);
		configured = true;
	}

	internal static void SetForChecks(float k, IEnumerable<MarketRegion> regions) {
		configured = true; K = k; places = null; regionCache.Clear(); cityCache.Clear(); normalizer.Clear();
		Build(regions);
	}

	/// <summary>Multiplier on a single's conversion in a region. 1 when off.</summary>
	public static float Demand(Genre primary, Genre secondary, string regionId, int year) {
		if (!Enabled || regionId == null) return 1f;
		var key = (primary, secondary, regionId, year);
		if (regionCache.TryGetValue(key, out float cached)) return cached;
		EnsureBuilt();
		float pop = 0f, mass = 0f;
		foreach (Place p in places) if (p.RegionId == regionId) { pop += p.Population; mass += p.Population * Mass(primary, secondary, p, year); }
		float value = pop > 0f ? MathF.Pow(mass / pop, K) / Normalizer(primary, secondary, year) : 1f;
		return regionCache[key] = value;
	}

	/// <summary>Multiplier on a station's candidacy from its own city's taste; falls back to the region's.</summary>
	public static float Station(Genre canonical, string cityName, string regionId, int year) {
		if (!Enabled) return 1f;
		EnsureBuilt();
		if (cityName == null || !cityByName.TryGetValue(cityName, out Place city)) return Demand(canonical, canonical, regionId, year);
		var key = (canonical, city.Id, year);
		if (cityCache.TryGetValue(key, out float cached)) return cached;
		return cityCache[key] = MathF.Pow(Mass(canonical, canonical, city, year), K) / Normalizer(canonical, canonical, year);
	}

	/// <summary>National population-weighted mean of region taste^K, so the regional multipliers average 1.</summary>
	private static float Normalizer(Genre primary, Genre secondary, int year) {
		var key = (primary, secondary, year);
		if (normalizer.TryGetValue(key, out float cached)) return cached;
		var byRegion = new Dictionary<string, (float pop, float mass)>(StringComparer.Ordinal);
		foreach (Place p in places) {
			byRegion.TryGetValue(p.RegionId, out var t);
			byRegion[p.RegionId] = (t.pop + p.Population, t.mass + p.Population * Mass(primary, secondary, p, year));
		}
		float total = 0f, weighted = 0f;
		foreach (var (pop, mass) in byRegion.Values) { total += pop; weighted += pop * MathF.Pow(mass / pop, K); }
		return normalizer[key] = total > 0f ? weighted / total : 1f;
	}

	private static float Mass(Genre primary, Genre secondary, Place p, int year) {
		float m = Mass(primary, p, year);
		return primary == secondary ? m : (1f - SecondaryWeight) * m + SecondaryWeight * Mass(secondary, p, year);
	}

	/// <summary>m_p(g) = Σ_s w_gs × a_ps / ā_s; segments without a place attribute read 1.</summary>
	private static float Mass(Genre genre, Place p, int year) {
		GenreProfile profile = GenreCatalog.Get(GenreCatalog.MapLegacy(genre, year));
		float total = 0f, m = 0f;
		foreach ((string segment, float w) in profile.SegmentWeights) {
			if (w <= 0f) continue;
			total += w;
			m += w * segment switch {
				"UrbanRnB" => p.Black / NationalMean(q => q.Black),
				"CountryWestern" => p.Country / NationalMean(q => q.Country),
				"RegionalLatin" => p.LatinIn(year) / NationalMean(q => q.LatinIn(year)),
				_ => 1f
			};
		}
		return total > 0f ? m / total : 1f;
	}

	private static float NationalMean(Func<Place, float> share) {
		float pop = 0f, sum = 0f;
		foreach (Place p in places) { pop += p.Population; sum += p.Population * share(p); }
		return Math.Max(1e-6f, sum / pop);
	}

	private static void EnsureBuilt() {
		if (places != null) return;
		Build(ChartManager.Instance?.GetAllRegions() ?? throw new InvalidOperationException("Regional genre taste needs the market regions."));
	}

	private static void Build(IEnumerable<MarketRegion> regions) {
		places = new List<Place>();
		cityByName = new Dictionary<string, Place>(StringComparer.Ordinal);
		foreach (MarketRegion region in regions) {
			float cityPop = 0f, cityBlack = 0f;
			foreach (City c in Cities) {
				MarketCity mc = DistanceModel.GetCityById(c.Id);
				if (mc == null || mc.parentRegionId != region.regionId) continue;
				var place = new Place { Id = c.Id, RegionId = region.regionId, CityId = c.Id, Population = c.Population, Black = c.Black, Country = c.Country,
					Latin = c.Latin, LatinPerYear = c.LatinPerYear, LatinCap = c.LatinCap };
				places.Add(place);
				cityByName[mc.name] = place;
				cityPop += c.Population; cityBlack += c.Population * c.Black;
			}
			float rest = Math.Max(.1f * region.population, region.population - cityPop);
			(float country, float latin) = Remainders.TryGetValue(region.regionId, out var r) ? r : (.15f, .01f);
			places.Add(new Place { Id = region.regionId + ":rest", RegionId = region.regionId,
				CityId = DistanceModel.GetHubCityIdForRegion(region.regionId), Population = rest,
				Black = Math.Max(BlackShareFloor, (region.blackPopulation * (cityPop + rest) - cityBlack) / rest), Country = country, Latin = latin });
		}
	}
}
