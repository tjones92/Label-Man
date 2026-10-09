using System;
using System.Linq;
using System.IO;
using Godot;

/// <summary>Placement distribution, existing-identity preservation, and actual cohort census.</summary>
public static class SceneCityPlacementChecks {
    public static void Run() {
        int checks = 0;
        void Check(bool condition, string detail) { if (!condition) throw new InvalidOperationException("SCENE_CITY_PLACEMENT_CHECK_FAILED: " + detail); checks++; }
        var cities = ScenePlaceRegistry.All.Where(p => p.Id == p.PlayableCityId).ToArray();
        Check(cities.Length == 31 && cities.All(p => SceneCityPlacement.Weight(p.Id, Genre.TraditionalPop) > 0), "all cities have positive opportunity");
        Check(SceneCityPlacement.Assign(1001, "unknown", "unknown", Genre.Jazz, ArtistCohort.InitialLegacy) == null, "unknown remains unknown");
        foreach (ulong seed in new ulong[] {1001, 2002}) {
            foreach (ArtistCohort cohort in new[] {ArtistCohort.InitialLegacy, ArtistCohort.EnabledInitialReserve, ArtistCohort.RuntimeFormation}) {
                var placements = Enumerable.Range(1, 3000).Select(i => SceneCityPlacement.Assign(seed, $"artist_{i:D5}", "Rockies", Genre.TraditionalPop, cohort)).ToArray();
                int Count(string city) => placements.Count(p => p == city);
                Check(Count("new_york") > 350 && Count("new_york") > Count("billings") * 20, "NYC dominates Billings across seeds/cohorts");
                Check(Count("los_angeles") > 280 && Count("chicago") > 200 && Count("billings") < 15, "broad centers dominate; small-city tail retained");
                Check(placements.SequenceEqual(Enumerable.Range(1,3000).Reverse().Select(i => SceneCityPlacement.Assign(seed,$"artist_{i:D5}","East Coast",Genre.TraditionalPop,cohort)).Reverse()), "order and legacy sales-region independent");
            }
        }
        double Share(string city, Genre genre, int year = 1960) => SceneCityPlacement.Weight(city, genre, year) / cities.Sum(p => SceneCityPlacement.Weight(p.Id, genre, year));
        Check(Share("nashville",Genre.Country) > Share("nashville",Genre.TraditionalPop) * 2, "country center");
        Check(Share("detroit",Genre.Soul) > Share("detroit",Genre.TraditionalPop), "soul center");
        Check(Share("san_antonio",Genre.TexMex) > Share("san_antonio",Genre.TraditionalPop) * 3, "TexMex center");
        Check(Share("san_francisco",Genre.AcidRock,1966) > Share("san_francisco",Genre.AcidRock,1960), "dated psych opportunity");
        var artist = ArtistManager.Instance.GetAllArtists().First(a => a.geography?.basePlaceId != null);
        string before = System.Text.Json.JsonSerializer.Serialize(artist,SaveGameService.TestJsonOptions);
        LocalSceneIdentityService.EnsureArtist(artist,generated:true,formationPlaceId:"billings");
        Check(before == System.Text.Json.JsonSerializer.Serialize(artist,SaveGameService.TestJsonOptions), "saved base/origin/members and ownership never redistributed on access");
        var acts = ArtistManager.Instance.GetAllArtists().ToArray();
        var launch = acts.Where(a => a.cohort == ArtistCohort.InitialLegacy).ToArray();
        Check(launch.Length == 3000, "national launch count preserved");
        Check(launch.Count(a => a.geography?.basePlaceId == "new_york") > launch.Count(a => a.geography?.basePlaceId == "billings") * 20, "actual launch distribution");
        string path = ProjectSettings.GlobalizePath($"res://SimLogs/city-placement-{LocalSceneIdentityService.KeyedSeed}.csv");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        using (var writer = new StreamWriter(path)) {
            writer.WriteLine("city,cohort,acts,share,signed");
            foreach (var cohort in acts.Select(a => a.cohort).Distinct().OrderBy(c => c)) {
                var group = acts.Where(a => a.cohort == cohort).ToArray();
                foreach (var city in cities.OrderByDescending(p => group.Count(a => a.geography?.basePlaceId == p.Id))) {
                    var residents = group.Where(a => a.geography?.basePlaceId == city.Id).ToArray();
                    writer.WriteLine(FormattableString.Invariant($"{city.Id},{cohort},{residents.Length},{residents.Length/(double)group.Length:F6},{residents.Count(a => !string.IsNullOrEmpty(a.labelId))}"));
                }
            }
        }
        Godot.GD.Print($"SCENE_CITY_PLACEMENT_CHECK_PASS checks={checks} census={path}");
    }
}
