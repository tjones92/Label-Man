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
        var moves = SceneDynamicsMobility.Capture(TimeManager.Instance.CurrentDate);
        Check(Json(moves) == Json(SceneDynamicsMobility.Capture(TimeManager.Instance.CurrentDate)), "movement reads repeat without events");
        Check(moves.Count <= 31 && moves.Select(m => m.FromPlaceId).Distinct().Count() == moves.Count, "movement attention bounded per source");
        Check(moves.All(m => m.RoadMiles > 0 && m.RoadMiles <= SceneDynamicsMobility.MaximumRoadMiles && m.TravelDays >= 1), "road proposals have nonzero travel time");
        var lead = SceneDynamicsMobility.Evaluate("probe", "oakland", "san_antonio", Genre.Country, 1960, 1, false);
        Check(lead != null && lead.OriginPlaceId == "oakland" && lead.FromPlaceId == "san_antonio" && lead.ToPlaceId != lead.FromPlaceId, "opportunity proposal preserves distinct origin");
        Check(SceneDynamicsMobility.Evaluate("probe", null, "san_antonio", Genre.Country, 1960, 1, true) == null, "commitments block proposals");
        Check(SceneDynamicsMobility.Evaluate("probe", null, "gb_london", Genre.Country, 1960, 1, false) == null, "foreign source has no domestic road proposal");
        Check(SceneDynamicsMobility.Evaluate("probe", null, "milwaukee", Genre.Country, 1960, 1, false) == null, "unknown satellite route has no fabricated travel");
        var attention = SceneDynamicsAttention.Capture(ChartManager.Instance.GetCurrentChartWeek());
        Check(Json(attention) == Json(SceneDynamicsAttention.Capture(ChartManager.Instance.GetCurrentChartWeek())), "attention reads repeat without impulses");
        Check(attention.Cities.Count == 31 && attention.Cities.All(c => c.SigningHeat >= 0 && c.SigningHeat < 1), "attention bounded across every playable city");
        var eventRow = new SceneRecruitmentRecord { ArtistId = "shock-act", LabelId = "shock-label", Week = 10,
            Phase = "DailyMarket", BasePlaceId = "new_york", OriginPlaceId = "oakland", HqPlaceId = "newark", SigningGenre = Genre.Jazz };
        var openingRow = new SceneRecruitmentRecord { ArtistId = "opening-act", LabelId = "opening-label", Week = 0,
            Phase = "Initialization", BasePlaceId = "new_york" };
        var shock = SceneDynamicsAttention.Project(new[] { eventRow, eventRow, openingRow }, 10);
        Check(shock.Evidence.Count == 2 && Math.Abs(shock.Evidence.Sum(e => e.Attribution) - 1) < 1e-10, "one signing budget split across base, origin and HQ with duplicate suppression");
        Check(shock.Evidence.All(e => e.ArtistId == "shock-act" && e.SigningGenre == Genre.Jazz), "initial allocation excluded and event genre retained");
        Check(SceneDynamicsAttention.Project(new[] { eventRow }, 9).Evidence.Count == 0, "future events do not leak");
        Check(Math.Abs(SceneDynamicsAttention.Project(new[] { eventRow }, 18).Evidence.Sum(e => e.DecayedImpulse) - .025) < 1e-10, "signing impulse halves after eight weeks");
        Check(SceneDynamicsAttention.Project(new[] { eventRow }, 115).Evidence.Count == 0, "old routine attention bounded by retention horizon");
        Check(Json(shock) == Json(SceneDynamicsAttention.Project(new[] { openingRow, eventRow, eventRow }, 10)), "attention stable under ledger ordering");
        Check(SceneDynamicsAttention.BoundedHeat(1000) <= 1 && SceneDynamicsAttention.BoundedHeat(0) == 0, "large shock saturates and absence produces zero");
        var oldRow = JsonSerializer.Deserialize<SceneRecruitmentRecord>("{\"ArtistId\":\"legacy\",\"LabelId\":\"label\",\"Week\":10,\"Phase\":\"DailyMarket\",\"BasePlaceId\":\"memphis\"}", SaveGameService.TestJsonOptions);
        Check(oldRow.SigningGenre == null && SceneDynamicsAttention.Project(new[] { oldRow }, 10).Evidence.Single().SigningGenre == null, "legacy event genre stays unknown");
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
        Check(Json(moves) == Json(SceneDynamicsMobility.Capture(TimeManager.Instance.CurrentDate)), "save/reload preserves move proposals");
        Check(Json(attention) == Json(SceneDynamicsAttention.Capture(ChartManager.Instance.GetCurrentChartWeek())), "save/reload preserves attention and event genre");
        LocalScenes.Configure(new[] { "--disable-scene-rooms" });
        bool rejected = false;
        try { SceneDynamicsObservations.Capture(TimeManager.Instance.CurrentDate); } catch (InvalidOperationException) { rejected = true; }
        Check(rejected, "observation cannot create missing room state");
        LocalScenes.Configure(Array.Empty<string>());
        GD.Print($"SCENE_DYNAMICS_CHECK_PASS checks={checks}");
    }
}
