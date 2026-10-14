using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Domestic selected-cohort opportunity priors for the 1960s, not a musician census.
/// See SimTools/LocalSceneCityPlacement.md for evidence and calibration boundaries.
/// Sales regions are compatibility projections, never the first-stage placement lottery.
/// </summary>
public static class SceneCityPlacement {
    // All 31 playable cities remain eligible. Broad centers dominate; specialized centers
    // receive genre adjustments below. Values are relative weights, normalized per genre.
    private static readonly IReadOnlyDictionary<string, double> Opportunity = new Dictionary<string, double>(StringComparer.Ordinal) {
        ["new_york"] = 18, ["los_angeles"] = 14, ["chicago"] = 10,
        ["detroit"] = 6, ["nashville"] = 6, ["philadelphia"] = 5, ["memphis"] = 5,
        ["san_francisco"] = 4, ["boston"] = 3, ["new_orleans"] = 3,
        ["atlanta"] = 2.5, ["dallas"] = 2.5, ["houston"] = 2.5,
        ["st_louis"] = 2, ["cleveland"] = 2, ["washington"] = 2,
        ["baltimore"] = 1.5, ["pittsburgh"] = 1.5, ["minneapolis"] = 1.5,
        ["miami"] = 1.5, ["seattle"] = 1.5, ["kansas_city"] = 1.5,
        ["cincinnati"] = 1.5, ["san_antonio"] = 1.5,
        ["denver"] = 1.2, ["portland"] = 1, ["phoenix"] = 0.7,
        ["omaha"] = 0.6, ["salt_lake_city"] = 0.5, ["albuquerque"] = 0.4, ["billings"] = 0.1
    };
    public static double Weight(string cityId, Genre genre, int year = 1960) {
        if (!Opportunity.TryGetValue(cityId, out double opportunity)) return 0;
        double fit = (genre, cityId) switch {
            (Genre.Country, "nashville") => 4,
            (Genre.Country, "memphis" or "dallas" or "houston" or "san_antonio") => 1.5,
            (Genre.CountryRock, "los_angeles") => 2,
            (Genre.Soul or Genre.Motown or Genre.RnB, "detroit" or "memphis") => 2,
            (Genre.Soul or Genre.RnB or Genre.Gospel, "chicago" or "new_orleans" or "philadelphia") => 1.4,
            (Genre.Blues, "chicago") => 2,
            (Genre.Blues, "memphis" or "st_louis" or "houston") => 1.5,
            (Genre.Jazz, "new_york" or "new_orleans") => 1.7,
            (Genre.Jazz, "chicago" or "kansas_city") => 1.3,
            (Genre.Folk or Genre.ContemporaryFolk, "new_york" or "boston" or "san_francisco") => 1.5,
            (Genre.SurfRock or Genre.SunshinePop, "los_angeles") => 3,
            (Genre.GarageRock, "seattle" or "portland" or "detroit" or "minneapolis") => 1.7,
            (Genre.PsychedelicRock or Genre.Psychedelic or Genre.AcidRock, "san_francisco") when year >= 1965 => 3,
            (Genre.ProtoPunk, "detroit" or "new_york") when year >= 1966 => 1.7,
            (Genre.TexMex, "san_antonio") => 6,
            (Genre.TexMex, "houston" or "dallas") => 2,
            (Genre.LatinPop, "miami") => 4,
            (Genre.LatinPop or Genre.Boogaloo, "new_york") => 2,
            _ => 1
        };
        return opportunity * fit;
    }
    public static string Assign(ulong seed, string entityId, string region, Genre genre, ArtistCohort cohort, int year = 1960) {
        // Unknown/foreign legacy geography must not silently acquire a domestic identity.
        if (SceneRegionAdapter.ToId(region) == null) return null;
        var places = ScenePlaceRegistry.All.Where(p => p.CountryCode == "US" && p.Id == p.PlayableCityId).ToArray();
        double total = places.Sum(p => Weight(p.Id, genre, year));
        if (!(total > 0)) throw new InvalidOperationException("Domestic scene placement has no eligible opportunity weights.");
        double draw = LocalSceneIdentityService.AssignmentUnit(seed, $"v2|national-base|{entityId}|{(int)cohort}|{(int)genre}") * total;
        foreach (ScenePlace place in places) { draw -= Weight(place.Id, genre, year); if (draw < 0) return place.Id; }
        return places[^1].Id;
    }
}
