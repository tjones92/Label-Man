using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Domestic selected-cohort opportunity priors for the 1960s, not a musician census.
/// See SimTools/LocalSceneCityPlacement.md for evidence and calibration boundaries.
/// Sales regions are compatibility projections, never the first-stage placement lottery.
/// </summary>
public static class SceneCityPlacement {
    /// <summary>Philadelphia's teen-pop, vocal-group and jazz fits. Opt-in (--enable-philadelphia-recording-center) until a decade A/B:
    /// a 104-week two-seed A/B left totals flat but moved Major chart share up 6-7 points in 1961 on both seeds.</summary>
    public static readonly bool PhiladelphiaRecordingCenter = SimulationArguments.Get().Contains("--enable-philadelphia-recording-center");
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
    /// <summary>Specialist-centre fits. Opt-in (--enable-specialist-scene-placement) until a decade A/B. The legacy fits
    /// (x1.4-6) cannot overcome New York 18 / Los Angeles 14, so on seed 1001 at the end of 1961 New York held as many Country
    /// acts as Nashville (14% vs 19%), Miami 5% of Latin pop, San Antonio 13% of Tex-Mex and Los Angeles 23% of surf. These fits
    /// let each centre lead its own sound (Nashville ~35% of Country, LA ~70% of surf, San Antonio ~30% of Tex-Mex, Chicago ~25% of
    /// blues, San Francisco ~26% of psychedelia from 1965) and thin Black-music genres in the mountain and plains West, where
    /// the 1960 Black population was small. Implied shares are opportunity x fit normalised per genre; mobility reads the same.</summary>
    public static readonly bool SpecialistPlacement = SimulationArguments.Get().Contains("--enable-specialist-scene-placement");
    private static readonly HashSet<string> MountainPlainsWest = new(StringComparer.Ordinal) {
        "seattle", "portland", "denver", "phoenix", "omaha", "salt_lake_city", "albuquerque", "billings", "minneapolis" };
    private static readonly HashSet<string> NortheastAndLakes = new(StringComparer.Ordinal) {
        "new_york", "boston", "philadelphia", "chicago", "detroit", "washington", "baltimore", "cleveland", "pittsburgh" };
    private static double? SpecialistFit(Genre genre, string city, int year) => (genre, city) switch {
        (Genre.Country, "nashville") => 9,
        (Genre.Country, "dallas" or "houston" or "san_antonio") => 2,
        (Genre.Country, "memphis") => 1.5,
        (Genre.Country, "los_angeles") => 1.3, // Bakersfield and the LA country clubs
        (Genre.Country, _) when NortheastAndLakes.Contains(city) => .5,
        (Genre.CountryRock, "los_angeles") => 3,
        (Genre.RnB or Genre.Soul or Genre.Motown or Genre.Funk, "detroit" or "memphis") => 3,
        (Genre.RnB, "new_orleans") => 4, // Imperial, Minit, Fats Domino, Allen Toussaint
        (Genre.RnB, "houston") => 2,     // Duke/Peacock: Bobby Bland, Junior Parker
        (Genre.Soul or Genre.RnB or Genre.Gospel or Genre.Funk, "chicago" or "philadelphia") => 1.4,
        (Genre.Soul or Genre.Gospel, "new_orleans") => 1.4,
        (Genre.Soul or Genre.RnB or Genre.Gospel or Genre.Motown or Genre.Funk or Genre.Blues, _) when MountainPlainsWest.Contains(city) => .5,
        (Genre.Blues, "chicago") => 3.5,
        (Genre.Blues, "memphis" or "houston") => 3,
        (Genre.Blues, "st_louis" or "detroit") => 2,
        (Genre.Blues, "new_york") => .6,
        (Genre.Jazz, "new_york") => 2,
        (Genre.Jazz, "new_orleans") => 4,
        (Genre.Jazz, "chicago" or "kansas_city") => 1.3,
        (Genre.SurfRock or Genre.SunshinePop, "los_angeles") => 9,
        (Genre.SurfRock, _) when NortheastAndLakes.Contains(city) => .3,
        (Genre.GarageRock, "seattle" or "portland") => 4, // the Northwest sound
        (Genre.GarageRock, "detroit" or "minneapolis" or "dallas" or "san_antonio") => 1.7,
        (Genre.PsychedelicRock or Genre.Psychedelic or Genre.AcidRock, "san_francisco") when year >= 1965 => 9,
        (Genre.TexMex, "san_antonio") => 25,
        (Genre.TexMex, "houston" or "dallas") => 4,
        (Genre.TexMex, "los_angeles" or "phoenix" or "albuquerque") => 1.5,
        (Genre.TexMex, "nashville") => .2,
        (Genre.TexMex, _) when NortheastAndLakes.Contains(city) => .2,
        (Genre.LatinPop, "miami") => 6,
        (Genre.LatinPop or Genre.Boogaloo, "new_york") => 2.5,
        (Genre.LatinPop, "los_angeles" or "san_antonio") => 1.5,
        (Genre.Folk or Genre.ContemporaryFolk, "new_york" or "boston" or "san_francisco") => 2,
        _ => null
    };
    public static double Weight(string cityId, Genre genre, int year = 1960) {
        if (!Opportunity.TryGetValue(cityId, out double opportunity)) return 0;
        double fit = (SpecialistPlacement ? SpecialistFit(genre, cityId, year) : null) ?? (genre, cityId) switch {
            (Genre.Country, "nashville") => 4,
            (Genre.Country, "memphis" or "dallas" or "houston" or "san_antonio") => 1.5,
            (Genre.CountryRock, "los_angeles") => 2,
            (Genre.Soul or Genre.Motown or Genre.RnB, "detroit" or "memphis") => 2,
            (Genre.Soul or Genre.RnB or Genre.Gospel, "chicago" or "new_orleans" or "philadelphia") => 1.4,
            // Philadelphia's record business around 1960 was teen and vocal-group pop: Cameo-Parkway, Chancellor, Swan and
            // Jamie, with American Bandstand broadcast from WFIL; and it had a deep jazz scene (Coltrane, Golson, Lee Morgan,
            // Jimmy Smith, Philly Joe Jones). Without these fits it places fewer acts than Memphis. --enable-philadelphia-recording-center
            // turns them on.
            (Genre.TeenPop or Genre.DooWop or Genre.GirlGroup, "philadelphia") when PhiladelphiaRecordingCenter => 1.8,
            (Genre.Jazz, "philadelphia") when PhiladelphiaRecordingCenter => 1.4,
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
    /// <summary>Early-game F3: how far this city leans toward a genre beyond its size -- the fit term of <see cref="Weight"/>.
    /// 1 for a city with no specialism there, or one outside the table. Size cancels, so a sparse city never reads as an
    /// extreme taste. A proxy for local buyer preference, not measured demand.</summary>
    public static double Fit(string cityId, Genre genre, int year = 1960) =>
        cityId != null && Opportunity.TryGetValue(cityId, out double opportunity) && opportunity > 0 ? Weight(cityId, genre, year) / opportunity : 1;
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
