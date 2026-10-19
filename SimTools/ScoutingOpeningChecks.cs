using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;

public static class ScoutingOpeningChecks {
    public static void Run() {
        int checks = 0;
        void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("SCOUTING_OPENING_FAILED: " + why); checks++; }
        string Json(object value) => JsonSerializer.Serialize(value, SaveGameService.TestJsonOptions);
        var desk = PlayerDesk.Instance; var clock = TimeManager.Instance;
        Check(desk.FoundLabel("Opening Checks", "nashville", out string message), "found label: " + message);
        var opening = desk.CaptureState();
        // Audit actual posted topology from every founding city, with each starting cash profile.
        // Moving the fixture's viewpoint creates no people, bills, introductions or signings.
        var rows = new List<object>();
        foreach (var city in DistanceModel.GetCities().OrderBy(c => c.cityId, StringComparer.Ordinal)) {
            opening.CurrentCityId = city.cityId;
            Check(desk.RestoreState(opening, out message), "viewpoint: " + city.cityId + " " + message);
            foreach (var profile in FoundingArchetypeData.All) {
                desk.Label.cashReserves = profile.Value.Capital;
                var local = desk.ScoutingItinerary();
                var circuit = desk.ScoutingItinerary(true);
                Check(local.All(o => o.TravelMinutes == 0 && o.CityId == city.cityId), "local geography: " + city.cityId);
                Check(circuit.All(o => o.EndMinute <= clock.HardStopHour * 60 && o.ListeningMinutes > 0 && o.TravelMinutes <= clock.MinutesRemainingWithOvertime), "reachable clock: " + city.cityId);
                Check(circuit.All(o => o.Affordable == (o.Admission + o.TravelCost <= profile.Value.Capital)), "actual quote: " + city.cityId);
                var next = circuit.FirstOrDefault(o => o.Affordable);
                rows.Add(new { city = city.cityId, archetype = profile.Key.ToString(), cash = profile.Value.Capital,
                    localSets = local.Count, affordableLocal = local.Count(o => o.Affordable), circuitSets = circuit.Count(o => o.TravelMinutes > 0 && o.Affordable),
                    nextDay = next?.DaysUntil, nextRoom = next?.RoomName, nextCity = next?.CityId, admission = next?.Admission, gas = next?.TravelCost });
                Check(next != null, "no affordable posted route from " + city.cityId + " for " + profile.Key);
            }
        }
        string dir = ProjectSettings.GlobalizePath("res://SimLogs"); Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "scouting-opening-audit-" + OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--seed="))?.Substring(7) + ".json"), Json(rows));
        opening.CurrentCityId = "nashville";
        Check(desk.RestoreState(opening, out message), "restore home viewpoint");
        desk.Label.cashReserves = 800;
        string world = Json(WorldStateService.Capture()), player = Json(desk.CaptureState());
        GameRandom.Seed(917); uint draw = GameRandom.Randi(); GameRandom.Seed(917);
        var plans = desk.ScoutingItinerary(true); desk.ScoutingItinerary(true);
        Check(Json(WorldStateService.Capture()) == world && Json(desk.CaptureState()) == player && GameRandom.Randi() == draw, "planning preserves world, player and RNG");
        Check(plans.Select(o => o.EventId).Distinct().Count() == plans.Count, "no duplicated appearances");
        Check(plans.Any(o => !string.IsNullOrEmpty(ArtistManager.Instance.GetArtist(o.ArtistId)?.labelId)), "signed performers remain useful listening choices");
        var savedPlayer = JsonSerializer.Deserialize<PlayerSaveData>(player, SaveGameService.TestJsonOptions);
        string itinerary = Json(plans);
        Check(desk.RestoreState(savedPlayer, out _) && Json(desk.ScoutingItinerary(true)) == itinerary, "save restore retains identical logistical choices");
        // Cash changes filter logistics only, not the posted cast or any assessment.
        desk.Label.cashReserves = 0;
        var poor = desk.ScoutingItinerary();
        Check(poor.Select(o => o.EventId).SequenceEqual(plans.Where(o => o.TravelMinutes == 0).Select(o => o.EventId)) && poor.Any(o => o.Affordable), "cash changes affordability, not the cast; free authored local path");
        desk.Label.cashReserves = 800;
        var plan = desk.ScoutingItinerary().First(o => o.Affordable && !o.Playing);
        float cash = desk.Label.cashReserves;
        Check(desk.WaitForSceneAppearance(plan.BillId, plan.EventId, out message), "ordinary wait: " + message);
        Check(desk.ScoutingDossiers.Count == 0 && desk.CurrentCityId == "nashville", "waiting does not discover or travel");
        var now = desk.ScoutingItinerary().Single(o => o.EventId == plan.EventId);
        Check(now.Playing && now.WaitingMinutes == 0 && clock.CurrentHour * 60 + clock.CurrentMinute == plan.StartMinute, "arrival at exact announced start");
        Check(desk.ListenSceneAppearance(now.BillId, now.EventId, out message), "listen: " + message);
        Check(desk.ScoutingDossiers.Count == 1 && desk.Label.cashReserves == cash - now.Admission && desk.Label.roster.Count == 0, "listening acquires evidence, not a signing");
        Check(!desk.ScoutingItinerary().Any(o => o.EventId == now.EventId), "heard event no longer proposed");
        int minute = clock.CurrentHour * 60 + clock.CurrentMinute;
        cash = desk.Label.cashReserves;
        Check(!desk.ListenSceneAppearance(now.BillId, now.EventId, out _) && clock.CurrentHour * 60 + clock.CurrentMinute == minute && desk.Label.cashReserves == cash, "repeat click consumes no cost");
        clock.RestoreClock(clock.CurrentDate, clock.HardStopHour);
        Check(desk.ScoutingItinerary().All(o => o.DaysUntil > 0) && desk.ScoutingItinerary(true).All(o => o.TravelMinutes == 0), "late-day plans defer listening and driving");
        foreach (string flag in new[] { "--disable-scouting-experience", "--observe-scouting-experience" }) {
            LocalScenes.Configure(new[] { flag });
            Check(desk.ScoutingItinerary(true).Count == 0, "no actionable plan in " + flag);
        }
        LocalScenes.Configure(Array.Empty<string>());
        GD.Print($"SCOUTING_OPENING_CHECK_PASS checks={checks} cities={DistanceModel.GetCities().Count} profiles={FoundingArchetypeData.All.Count}");
    }
}
