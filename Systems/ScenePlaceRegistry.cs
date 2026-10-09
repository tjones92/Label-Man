using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

/// <summary>Compatibility only. Never infer true origin from a regional sales projection.</summary>
public static class SceneRegionAdapter {
    private static readonly Dictionary<string, string> Displays = new(StringComparer.Ordinal) {
        ["eastcoast"] = "East Coast", ["greatlakes"] = "Great Lakes", ["greatplains"] = "Great Plains",
        ["deepsouth"] = "Deep South", ["southwest"] = "Southwest", ["rockies"] = "Rockies", ["westcoast"] = "West Coast"
    };
    public static string ToId(string value) {
        if (string.IsNullOrWhiteSpace(value)) return null;
        string normalized = new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
        return Displays.ContainsKey(normalized) ? normalized : null;
    }
    public static string ToDisplay(string value) => ToId(value) is string id ? Displays[id] : null;
}

/// <summary>
/// Identity catalog v2. Domestic market coordinates reuse the game's approximate map; satellite/UK
/// coordinates are deliberately unknown until sourced. Catchments are design links, not birthplace aliases.
/// </summary>
public static class ScenePlaceRegistry {
    public const int ContentVersion = 2;
    private static readonly IReadOnlyDictionary<string, ScenePlace> Places;
    private static readonly Dictionary<string, string> Names = new(StringComparer.OrdinalIgnoreCase);
    public static IReadOnlyList<ScenePlace> All { get; }
    static ScenePlaceRegistry() {
        var places = new Dictionary<string, ScenePlace>(StringComparer.Ordinal);
        foreach (MarketCity city in DistanceModel.GetCities().OrderBy(c => c.cityId, StringComparer.Ordinal)) {
            Add(places, new ScenePlace { Id = city.cityId, Name = city.name, CountryCode = "US",
                MarketRegionId = city.parentRegionId, PlayableCityId = city.cityId,
                Latitude = city.mapCoords.Y / 50.0, Longitude = city.mapCoords.X / (0.788010754 * 50.0),
                CoordinateSource = "DistanceModel approximate game map; not a historical population source" });
        }
        Satellite(places, "newark", "Newark", "eastcoast", "new_york");
        Satellite(places, "oakland", "Oakland", "westcoast", "san_francisco");
        Satellite(places, "berkeley", "Berkeley", "westcoast", "san_francisco");
        Satellite(places, "cambridge", "Cambridge", "eastcoast", "boston");
        Satellite(places, "tacoma", "Tacoma", "westcoast", "seattle");
        Satellite(places, "st_paul", "St. Paul", "greatplains", "minneapolis");
        Satellite(places, "muscle_shoals", "Muscle Shoals", "deepsouth", null);
        Satellite(places, "gary", "Gary", "greatlakes", "chicago");
        Satellite(places, "bakersfield", "Bakersfield", "westcoast", null);
        Satellite(places, "clovis_nm", "Clovis, New Mexico", "southwest", null);
        Satellite(places, "macon", "Macon", "deepsouth", null);
        Satellite(places, "augusta", "Augusta", "deepsouth", null);
        Satellite(places, "fort_worth", "Fort Worth", "southwest", "dallas");
        Satellite(places, "denton", "Denton", "southwest", "dallas");
        Satellite(places, "hollywood", "Hollywood", "westcoast", "los_angeles");
        Satellite(places, "pasadena", "Pasadena", "westcoast", "los_angeles");
        RoadPlace(places, "milwaukee", "Milwaukee", "greatlakes", 43.063348, -87.966695, "55");
        RoadPlace(places, "indianapolis", "Indianapolis", "greatlakes", 39.776664, -86.145935, "18");
        RoadPlace(places, "jackson_ms", "Jackson, Mississippi", "deepsouth", 32.315834, -90.21285, "28");
        foreach (string name in new[] { "London", "Liverpool", "Manchester", "Birmingham", "Glasgow", "Bristol" })
            Add(places, new ScenePlace { Id = "gb_" + name.ToLowerInvariant(), Name = name, CountryCode = "GB" });
        // Textual synonyms only. Oakland, Milwaukee, etc. retain their own identities.
        Names["NYC"] = Names["New York City"] = "new_york";
        Names["Washington DC"] = Names["Washington D.C."] = Names["D.C."] = "washington";
        Names["LA"] = Names["L.A."] = "los_angeles";
        Names["SF"] = "san_francisco";
        Names["KC"] = "kansas_city";
        Names["SLC"] = "salt_lake_city";
        Names["Saint Paul"] = "st_paul";
        Places = new ReadOnlyDictionary<string, ScenePlace>(places);
        All = Array.AsReadOnly(places.Values.OrderBy(p => p.Id, StringComparer.Ordinal).ToArray());
    }
    private static void Add(Dictionary<string, ScenePlace> places, ScenePlace place) {
        places.Add(place.Id, place); Names.Add(place.Name, place.Id);
    }
    private static void Satellite(Dictionary<string, ScenePlace> places, string id, string name, string region, string catchment) =>
        Add(places, new ScenePlace { Id = id, Name = name, CountryCode = "US", MarketRegionId = region, PlayableCityId = catchment });
    // Census representative coordinates are modern map anchors only. No population,
    // historical venue or catchment is inferred from these data.
    private static void RoadPlace(Dictionary<string, ScenePlace> places, string id, string name,
        string region, double lat, double lon, string state) => Add(places, new ScenePlace {
            Id = id, Name = name, CountryCode = "US", MarketRegionId = region,
            Latitude = lat, Longitude = lon,
            CoordinateSource = $"https://www2.census.gov/geo/docs/maps-data/data/gazetteer/2024_Gazetteer/2024_gaz_place_{state}.txt; INTPTLAT/INTPTLONG; approximate game route anchor"
        });
    public static ScenePlace ResolveHeadquarters(string name, string region) {
        string regionId = SceneRegionAdapter.ToId(region);
        ScenePlace place = ByName(name);
        if (place == null && regionId == "deepsouth" && string.Equals(name?.Trim(), "Jackson", StringComparison.OrdinalIgnoreCase))
            place = Get("jackson_ms");
        return place?.CountryCode == "US" && regionId != null && place.MarketRegionId != regionId ? null : place;
    }
    public static ScenePlace Get(string id) => id != null && Places.TryGetValue(id, out var place) ? place : null;
    public static ScenePlace ByName(string name) => !string.IsNullOrWhiteSpace(name) && Names.TryGetValue(name.Trim(), out var id) ? Get(id) : null;
    public static IReadOnlyList<ScenePlace> PlayableInRegion(string region) {
        string id = SceneRegionAdapter.ToId(region);
        return Array.AsReadOnly(All.Where(p => p.Id == p.PlayableCityId && p.MarketRegionId == id && id != null).ToArray());
    }
    /// <summary>Unknown coordinates and overseas routes never become a zero-mile drive.</summary>
    public static bool TryDomesticRoadMiles(string fromId, string toId, out double miles) {
        miles = double.NaN;
        ScenePlace a = Get(fromId), b = Get(toId);
        if (a?.CountryCode != "US" || b?.CountryCode != "US") return false;
        if (a.Id == b.Id) { miles = 0; return true; }
        if (!a.Latitude.HasValue || !a.Longitude.HasValue || !b.Latitude.HasValue || !b.Longitude.HasValue) return false;
        double lat = a.Latitude.Value - b.Latitude.Value;
        double lon = (a.Longitude.Value - b.Longitude.Value) * Math.Cos((a.Latitude.Value + b.Latitude.Value) * Math.PI / 360.0);
        miles = 69 * 1.17 * Math.Sqrt(lat * lat + lon * lon);
        return true;
    }
}
