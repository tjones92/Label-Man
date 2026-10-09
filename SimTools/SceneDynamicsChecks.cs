using System;
using System.Linq;
using System.Text.Json;
using Godot;

public static class SceneDynamicsChecks {
    public static void Run() {
        int checks = 0;
        void Check(bool ok, string detail) { if (!ok) throw new InvalidOperationException("SCENE_DYNAMICS_CHECK_FAILED: " + detail); checks++; }
        string Json(object o) => JsonSerializer.Serialize(o, SaveGameService.TestJsonOptions);
        LocalScenes.Configure(Array.Empty<string>());
        Check(LocalScenes.Recruitment, "recruitment defaults on");
        foreach (string flag in new[] { "--disable-scene-rooms", "--disable-local-scenes", "--observe-local-scenes" }) {
            bool conflict = false;
            try { LocalScenes.Configure(new[] { "--observe-scene-dynamics", flag }); } catch (ArgumentException) { conflict = true; }
            Check(conflict && LocalScenes.Recruitment, "invalid observer dependencies reject before mutation: " + flag);
        }
        LocalScenes.Configure(new[] { "--disable-scene-recruitment" });
        Check(!LocalScenes.Recruitment && LocalScenes.Rooms, "explicit recruitment off retains rooms");
        foreach (string flag in new[] { "--disable-local-scenes", "--disable-persistent-scenes", "--observe-local-scenes", "--disable-genre-market-v2", "--disable-artist-population-lifecycle" }) {
            LocalScenes.Configure(new[] { flag });
            Check(!LocalScenes.Recruitment, "dependency/identity controls suppress default: " + flag);
        }
        LocalScenes.Configure(Array.Empty<string>());
        string before = Json(WorldStateService.Capture());
        GD.Seed(901); uint expected = GD.Randi(); GD.Seed(901);
        var a = SceneDynamicsObservations.Capture(TimeManager.Instance.CurrentDate);
        var b = SceneDynamicsObservations.Capture(TimeManager.Instance.CurrentDate);
        Check(expected == GD.Randi(), "observation preserves global RNG");
        Check(before == Json(WorldStateService.Capture()), "observation preserves complete world state");
        Check(Json(a) == Json(b), "repeated reads reproduce the same facts");
        Check(a.Count == 31 && a.Select(r => r.PlaceId).Distinct().Count() == 31, "every playable city included once");
        Check(a.All(r => r.BookedSlots <= r.PerformanceSlots), "bookings stay within dated supporting-room slots");
        Check(a.All(r => r.SignedResidents <= r.Residents && r.BookableResidents <= r.Residents), "resident denominators reconcile");
        Check(a.All(r => r.PerformanceSlots == 18), "trade listening appointments excluded from stage capacity");
        var saved = WorldStateService.Capture();
        WorldStateService.Apply(JsonSerializer.Deserialize<WorldSaveData>(Json(saved), SaveGameService.TestJsonOptions), TimeManager.Instance.CurrentDate, SimulationSeedBootstrap.RequestedSeed);
        Check(Json(a) == Json(SceneDynamicsObservations.Capture(TimeManager.Instance.CurrentDate)), "save/reload preserves pressure observations");
        LocalScenes.Configure(new[] { "--disable-scene-rooms" });
        bool rejected = false;
        try { SceneDynamicsObservations.Capture(TimeManager.Instance.CurrentDate); } catch (InvalidOperationException) { rejected = true; }
        Check(rejected, "observation cannot create missing room state");
        LocalScenes.Configure(Array.Empty<string>());
        GD.Print($"SCENE_DYNAMICS_CHECK_PASS checks={checks}");
    }
}
