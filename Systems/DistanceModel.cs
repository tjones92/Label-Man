using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

public partial class DistanceModel : Node {
	public static DistanceModel Instance { get; private set; }

	private const float ProjectionLatitudeDegrees = 38f;
	private const float ProjectionScale = 50f;
	private const float ProjectionCosine = 0.788010754f;

	[ExportGroup("Equivalence Mode")]
	[Export] private bool distanceModelEnabled = false;

	[ExportGroup("Reach Parameters")]
	[Export(PropertyHint.Range, "1,10000,1")] private float reachHalfDistance = 65f;
	[Export(PropertyHint.Range, "0.1,4,0.05")] private float falloffCurveShape = 1.25f;
	[Export(PropertyHint.Range, "0,1,0.01")] private float difficultyWeight = 0.35f;
	[Export(PropertyHint.Range, "0,0.05,0.0001")] private float costPerDistance = 0.003f;
	[Export(PropertyHint.Range, "1,5,0.05")] private float maxCostFactor = 2.5f;

	private static readonly Dictionary<string, MarketCity> citiesById = new(StringComparer.Ordinal);
	private static readonly Dictionary<string, MarketCity> citiesByName = new(StringComparer.OrdinalIgnoreCase);
	private static readonly Dictionary<string, string> cityAliases = new(StringComparer.OrdinalIgnoreCase) {
		{ "NYC", "New York" },
		{ "New York City", "New York" },
		{ "Washington D.C.", "Washington" },
		{ "Washington DC", "Washington" },
		{ "D.C.", "Washington" },
		{ "LA", "Los Angeles" },
		{ "L.A.", "Los Angeles" },
		{ "SF", "San Francisco" },
		{ "Oakland", "San Francisco" },
		{ "Hollywood", "Los Angeles" },
		{ "Pasadena", "Los Angeles" },
		{ "Indianapolis", "Cincinnati" },
		{ "Milwaukee", "Chicago" },
		{ "Jackson", "Memphis" },
		{ "KC", "Kansas City" },
		{ "SLC", "Salt Lake City" }
	};
	private static readonly HashSet<string> internationalFallbackCities = new(StringComparer.OrdinalIgnoreCase) {
		"London", "Liverpool", "Manchester", "Birmingham", "Glasgow", "Bristol"
	};
	private static readonly Dictionary<string, string> currentRegionHubCityIds = new(StringComparer.Ordinal) {
		{ "eastcoast", "new_york" },
		{ "greatlakes", "chicago" },
		{ "greatplains", "minneapolis" },
		{ "westcoast", "los_angeles" },
		{ "deepsouth", "nashville" },
		{ "southwest", "dallas" },
		{ "rockies", "denver" }
	};
	private static readonly float[,] distanceMatrix;

	static DistanceModel() {
		RegisterCities();
		distanceMatrix = BuildDistanceMatrix();
	}

	public override void _Ready() {
		Instance = this;
	}

	public override void _ExitTree() {
		if (Instance == this) Instance = null;
	}

	public static IReadOnlyList<MarketCity> GetCities() => citiesById.Values.ToList();

	public static IEnumerable<(MarketCity From, MarketCity To, float Distance)> GetDistanceMatrixRows() {
		var cities = citiesById.Values.ToList();
		for (int from = 0; from < cities.Count; from++) {
			for (int to = 0; to < cities.Count; to++) {
				yield return (cities[from], cities[to], distanceMatrix[from, to]);
			}
		}
	}

	public static string ProjectionDescription =>
		$"x = longitude * cos({ProjectionLatitudeDegrees.ToString(CultureInfo.InvariantCulture)}deg) * {ProjectionScale.ToString(CultureInfo.InvariantCulture)}, y = latitude * {ProjectionScale.ToString(CultureInfo.InvariantCulture)}";

	public static MarketCity GetCityById(string cityId) =>
		!string.IsNullOrEmpty(cityId) && citiesById.TryGetValue(cityId, out MarketCity city) ? city : null;

	public static MarketCity GetCityByName(string cityName) {
		if (string.IsNullOrWhiteSpace(cityName)) return null;
		string canonical = cityAliases.TryGetValue(cityName.Trim(), out string alias) ? alias : cityName.Trim();
		return citiesByName.TryGetValue(canonical, out MarketCity city) ? city : null;
	}

	public static string GetHubCityIdForRegion(string regionId) =>
		!string.IsNullOrEmpty(regionId) && currentRegionHubCityIds.TryGetValue(regionId, out string cityId)
			? cityId
			: "new_york";

	public static MarketCity GetHubCityForRegion(string regionId) => GetCityById(GetHubCityIdForRegion(regionId));

	// Canonical, symmetric neighbour map for the seven-region board. Independent
	// distribution spread this way: a house that took a label's line had standing
	// arrangements with its peers in bordering markets, so a record breaking in one
	// region became placeable in the next one over long before it was placeable
	// nationally. AILabelFactory keeps its own randomized single-neighbour picker for
	// generation; this is the full deterministic set and does not disturb it.
	private static readonly Dictionary<string, string[]> AdjacentRegionIds = new(StringComparer.Ordinal) {
		["eastcoast"] = new[] { "greatlakes", "deepsouth" },
		["greatlakes"] = new[] { "eastcoast", "greatplains" },
		["greatplains"] = new[] { "greatlakes", "rockies", "southwest" },
		["deepsouth"] = new[] { "eastcoast", "southwest" },
		["southwest"] = new[] { "deepsouth", "greatplains", "rockies", "westcoast" },
		["rockies"] = new[] { "greatplains", "southwest", "westcoast" },
		["westcoast"] = new[] { "rockies", "southwest" }
	};

	public static IReadOnlyList<string> GetAdjacentRegions(string regionId) =>
		!string.IsNullOrEmpty(regionId) && AdjacentRegionIds.TryGetValue(regionId, out string[] neighbours)
			? neighbours
			: Array.Empty<string>();

	public static void AssignHomeCity(AILabel label) {
		if (label == null) return;
		(MarketCity city, string source) = ResolveHomeCity(label);
		label.homeCityId = city?.cityId ?? GetHubCityIdForRegion(label.homeRegion);
		label.homeCityAssignmentSource = source;
	}

	public static (MarketCity City, string Source) ResolveHomeCity(AILabel label) {
		if (label == null) return (GetCityById("new_york"), "domestic-unmapped");
		MarketCity direct = GetCityByName(label.headquartersCity);
		if (direct != null) return (direct, "hq-match");

		string hubId = GetHubCityIdForRegion(label.homeRegion);
		string source = internationalFallbackCities.Contains(label.headquartersCity ?? string.Empty)
			? "international"
			: "domestic-unmapped";
		return (GetCityById(hubId), source);
	}

	public static string[] GetDistributionNodes(AILabel label) {
		bool live = GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true;
		return BuildDistributionNodes(label, live);
	}

	private static string[] BuildDistributionNodes(AILabel label, bool live) {
		if (label == null) return Array.Empty<string>();
		var nodes = new List<string>();
		string homeCityId = !string.IsNullOrEmpty(label.homeCityId)
			? label.homeCityId
			: ResolveHomeCity(label).City?.cityId;
		AddNode(nodes, homeCityId);
		// Owned regional distribution is a physical node, not merely a coverage
		// flag. The live model previously ignored these hubs while recognizing
		// borrowed deal regions, charging covered sales distance from headquarters.
		// Keep the frozen disabled/prewarm route byte-compatible.
		if (live && label.distributionRegions != null) {
			foreach (string regionId in label.distributionRegions) {
				AddNode(nodes, GetHubCityIdForRegion(regionId));
			}
		}
		if (label.activeDeal?.grantedRegions != null) {
			foreach (string regionId in label.activeDeal.grantedRegions) {
				AddNode(nodes, GetHubCityIdForRegion(regionId));
			}
		}
		return nodes.ToArray();
	}

	internal static string[] GetDistributionNodesForProbe(AILabel label, bool live) =>
		BuildDistributionNodes(label, live);

	public static string[] GetDistributionNodeNames(AILabel label) =>
		GetDistributionNodes(label)
			.Select(id => GetCityById(id)?.name)
			.Where(name => !string.IsNullOrEmpty(name))
			.ToArray();

	public static float GetEffectiveReach(AILabel label, string destinationCityIdOrName) {
		if (!(Instance?.distanceModelEnabled ?? false)) return 1f;
		return Instance.CalculateEffectiveReach(label, destinationCityIdOrName);
	}

	public static float GetDistributionCostFactor(AILabel label, string destinationCityIdOrName) {
		if (!(Instance?.distanceModelEnabled ?? false)) return 1f;
		return Instance.CalculateDistributionCostFactor(label, destinationCityIdOrName);
	}

	private float CalculateEffectiveReach(AILabel label, string destinationCityIdOrName) {
		MarketCity destination = ResolveDestination(destinationCityIdOrName);
		if (label == null || destination == null) return 1f;
		float nearest = GetNearestNodeDistance(label, destination);
		float distanceFactor = 1f / (1f + Mathf.Pow(nearest / Mathf.Max(1f, reachHalfDistance), falloffCurveShape));
		float networkQuality = Mathf.Clamp(1f - (destination.distribution.difficulty * difficultyWeight) + (destination.distribution.inventoryDepth * 0.15f), 0.15f, 1.15f);
		return Mathf.Clamp(distanceFactor * networkQuality, 0.05f, 1.25f);
	}

	private float CalculateDistributionCostFactor(AILabel label, string destinationCityIdOrName) {
		MarketCity destination = ResolveDestination(destinationCityIdOrName);
		if (label == null || destination == null) return 1f;
		float nearest = GetNearestNodeDistance(label, destination);
		float difficultyScale = 1f + (destination.distribution.difficulty * difficultyWeight);
		return Mathf.Clamp(1f + nearest * costPerDistance * difficultyScale, 1f, maxCostFactor);
	}

	private static float GetNearestNodeDistance(AILabel label, MarketCity destination) {
		float nearest = float.PositiveInfinity;
		foreach (string nodeId in GetDistributionNodes(label)) {
			MarketCity node = GetCityById(nodeId);
			if (node == null) continue;
			nearest = Mathf.Min(nearest, node.mapCoords.DistanceTo(destination.mapCoords));
		}
		return float.IsPositiveInfinity(nearest) ? 0f : nearest;
	}

	private static MarketCity ResolveDestination(string destinationCityIdOrName) {
		MarketCity destination = GetCityById(destinationCityIdOrName);
		return destination ?? GetCityByName(destinationCityIdOrName);
	}

	private static void AddNode(List<string> nodes, string cityId) {
		if (string.IsNullOrEmpty(cityId) || nodes.Contains(cityId)) return;
		nodes.Add(cityId);
	}

	private static float[,] BuildDistanceMatrix() {
		var cities = citiesById.Values.ToList();
		var matrix = new float[cities.Count, cities.Count];
		for (int from = 0; from < cities.Count; from++) {
			for (int to = 0; to < cities.Count; to++) {
				matrix[from, to] = cities[from].mapCoords.DistanceTo(cities[to].mapCoords);
			}
		}
		return matrix;
	}

	private static void RegisterCities() {
		AddCity("new_york", "New York", 40.7f, -74.0f, "eastcoast", true, 1, 0.10f, 0.95f, true, true, 16.0f);
		AddCity("boston", "Boston", 42.4f, -71.1f, "eastcoast", false, 2, 0.24f, 0.70f, true, true, 2.6f);
		AddCity("philadelphia", "Philadelphia", 40.0f, -75.2f, "eastcoast", false, 2, 0.22f, 0.72f, true, true, 4.3f);
		AddCity("baltimore", "Baltimore", 39.3f, -76.6f, "eastcoast", false, 2, 0.30f, 0.62f, true, true, 2.0f);
		AddCity("washington", "Washington", 38.9f, -77.0f, "eastcoast", false, 3, 0.38f, 0.50f, true, true, 2.0f);
		// LabelGenerator founds labels in Pittsburgh, but it had no node here, so every
		// Pittsburgh firm resolved as "domestic-unmapped" and was charged distance from
		// the New York hub it does not sit in.
		AddCity("pittsburgh", "Pittsburgh", 40.4f, -80.0f, "eastcoast", false, 2, 0.30f, 0.60f, true, true, 2.4f);

		AddCity("chicago", "Chicago", 41.9f, -87.6f, "greatlakes", true, 1, 0.12f, 0.90f, true, true, 6.2f);
		AddCity("detroit", "Detroit", 42.3f, -83.0f, "greatlakes", false, 2, 0.22f, 0.72f, true, true, 3.8f);
		AddCity("cleveland", "Cleveland", 41.5f, -81.7f, "greatlakes", false, 2, 0.28f, 0.62f, true, true, 2.1f);
		AddCity("cincinnati", "Cincinnati", 39.1f, -84.5f, "greatlakes", false, 2, 0.30f, 0.60f, true, true, 1.3f);

		AddCity("minneapolis", "Minneapolis", 45.0f, -93.3f, "greatplains", true, 2, 0.32f, 0.58f, true, true, 1.5f);
		AddCity("st_louis", "St. Louis", 38.6f, -90.2f, "greatplains", false, 2, 0.30f, 0.60f, true, true, 2.1f);
		AddCity("kansas_city", "Kansas City", 39.1f, -94.6f, "greatplains", false, 3, 0.42f, 0.48f, true, true, 1.0f);
		AddCity("omaha", "Omaha", 41.3f, -95.9f, "greatplains", false, 3, 0.48f, 0.40f, true, false, 0.5f);

		AddCity("nashville", "Nashville", 36.2f, -86.8f, "deepsouth", true, 2, 0.24f, 0.70f, true, true, 0.7f);
		AddCity("memphis", "Memphis", 35.1f, -90.0f, "deepsouth", false, 2, 0.34f, 0.56f, true, true, 0.9f);
		AddCity("atlanta", "Atlanta", 33.7f, -84.4f, "deepsouth", false, 2, 0.32f, 0.58f, true, true, 1.3f);
		AddCity("new_orleans", "New Orleans", 30.0f, -90.1f, "deepsouth", false, 3, 0.44f, 0.48f, true, true, 0.9f);
		AddCity("miami", "Miami", 25.8f, -80.2f, "deepsouth", false, 3, 0.45f, 0.45f, true, false, 1.0f);

		AddCity("dallas", "Dallas", 32.8f, -96.8f, "southwest", true, 2, 0.30f, 0.60f, true, true, 1.4f);
		AddCity("houston", "Houston", 29.8f, -95.4f, "southwest", false, 3, 0.40f, 0.50f, true, true, 1.6f);
		AddCity("san_antonio", "San Antonio", 29.4f, -98.5f, "southwest", false, 3, 0.50f, 0.38f, true, false, 0.7f);
		AddCity("phoenix", "Phoenix", 33.4f, -112.1f, "southwest", false, 4, 0.66f, 0.30f, false, false, 0.7f);
		AddCity("albuquerque", "Albuquerque", 35.1f, -106.7f, "southwest", false, 4, 0.72f, 0.24f, false, false, 0.3f);

		AddCity("denver", "Denver", 39.7f, -105.0f, "rockies", true, 3, 0.42f, 0.50f, true, true, 1.0f);
		AddCity("salt_lake_city", "Salt Lake City", 40.8f, -111.9f, "rockies", false, 4, 0.66f, 0.30f, false, false, 0.6f);
		AddCity("billings", "Billings", 45.8f, -108.5f, "rockies", false, 4, 0.82f, 0.18f, false, false, 0.1f);

		AddCity("los_angeles", "Los Angeles", 34.1f, -118.2f, "westcoast", true, 1, 0.12f, 0.88f, true, true, 7.8f);
		AddCity("san_francisco", "San Francisco", 37.8f, -122.4f, "westcoast", false, 2, 0.28f, 0.65f, true, true, 3.0f);
		AddCity("seattle", "Seattle", 47.6f, -122.3f, "westcoast", false, 3, 0.42f, 0.46f, true, true, 1.1f);
		AddCity("portland", "Portland", 45.5f, -122.7f, "westcoast", false, 3, 0.48f, 0.42f, true, false, 0.8f);
	}

	private static void AddCity(
		string cityId,
		string name,
		float latitude,
		float longitude,
		string parentRegionId,
		bool isRegionalHub,
		int distributionTier,
		float difficulty,
		float inventoryDepth,
		bool hasOneStop,
		bool hasIndie,
		float metroPopulationMillions) {
		var city = new MarketCity {
			cityId = cityId,
			name = name,
			mapCoords = new Vector2(longitude * ProjectionCosine * ProjectionScale, latitude * ProjectionScale),
			parentRegionId = parentRegionId,
			isRegionalHub = isRegionalHub,
			distributionTier = distributionTier,
			distribution = new DistributionNetwork {
				difficulty = difficulty,
				inventoryDepth = inventoryDepth,
				hasOneStopDistributors = hasOneStop,
				hasIndieDistribution = hasIndie,
				recordStoreCount = Mathf.RoundToInt(metroPopulationMillions * 28f * (0.75f + inventoryDepth * 0.5f)),
				departmentStoreCount = Mathf.RoundToInt(metroPopulationMillions * 7f)
			}
		};
		citiesById[city.cityId] = city;
		citiesByName[city.name] = city;
	}
}
