using System;
using System.Linq;
using System.Text.Json;

/// <summary>Fixed runtime checks; opt-in, never run from ordinary startup or the economy comparator.</summary>
public static class LocalSceneIdentityChecks {
    public static void Run() {
        bool wasObserving = LocalScenes.Observing;
        WorldSaveData original = WorldStateService.Capture();
        string originalJson = JsonSerializer.Serialize(original, SaveGameService.TestJsonOptions);
        int count = 0;
        void Check(bool value, string detail) { if (!value) throw new InvalidOperationException("SCENE_IDENTITY_CHECK_FAILED: " + detail); count++; }
        string Json(object value) => JsonSerializer.Serialize(value, SaveGameService.TestJsonOptions);
        try {
            LocalScenes.Configure(new[] { "--observe-local-scenes" });
            Check(ScenePlaceRegistry.All.Count(p => p.Id == p.PlayableCityId) == 31, "31 playable identities");
            Check(ScenePlaceRegistry.All.Select(p => p.Id).Distinct().Count() == 56, "56 unique canonical places; no new playable city");
            Check(SceneRegionAdapter.ToId("Deep South") == "deepsouth" && SceneRegionAdapter.ToDisplay("deepsouth") == "Deep South", "region name/ID adapter");
            Check(SceneRegionAdapter.ToId("Atlantis") == null, "unknown region has no hub fallback");
            Check(ScenePlaceRegistry.ByName("Oakland").Id == "oakland" && ScenePlaceRegistry.Get("oakland").PlayableCityId == "san_francisco", "satellite distinct from catchment");
            Check(ScenePlaceRegistry.Get("milwaukee").PlayableCityId == null, "legacy distribution alias is not residence");
            Check(!ScenePlaceRegistry.TryDomesticRoadMiles("unknown", "memphis", out double missing) && double.IsNaN(missing), "unknown route is not zero miles");
            Check(!ScenePlaceRegistry.TryDomesticRoadMiles("gb_london", "new_york", out _), "foreign place not on domestic road graph");
            Check(ScenePlaceRegistry.TryDomesticRoadMiles("memphis", "nashville", out double miles) && miles > 0, "known domestic route");
            Check(LocalSceneIdentityService.AssignmentUnit(1001, "act-a") == LocalSceneIdentityService.AssignmentUnit(1001, "act-a"), "key replay");
            Check(LocalSceneIdentityService.AssignmentUnit(1001, "act-a") != LocalSceneIdentityService.AssignmentUnit(2002, "act-a"), "seed namespaces");
            var legacy = new SimulatedArtist { artistId = "scene-probe-legacy", homeRegion = "Deep South", primaryGenre = Genre.Jazz,
                labelId = "existing-owner", contractSequence = 3, formedYear = 1958, cohort = ArtistCohort.InitialLegacy,
                members = { new Musician { personId = "scene-probe-person" } } };
            LocalSceneIdentityService.EnsureArtist(legacy);
            string assigned = Json(legacy);
            LocalSceneIdentityService.EnsureArtist(legacy);
            Check(Json(legacy) == assigned, "legacy migration idempotent");
            Check(legacy.geography.originPlaceId == null && legacy.geography.baseEvidence == PlaceEvidence.Inferred, "inferred base never manufactures origin");
            Check(legacy.members[0].geography.originPlaceId == null, "personal birthplace remains unknown");
            Check(legacy.homeRegion == "Deep South" && legacy.contractSequence == 3 && legacy.labelId == "existing-owner", "commercial identity/ownership preserved");
            string baseId = legacy.geography.basePlaceId;
            legacy.labelId = "new-owner";
            LocalSceneIdentityService.EnsureArtist(legacy);
            Check(legacy.geography.basePlaceId == baseId && legacy.geography.originPlaceId == null, "signing does not relocate or invent origin");
            var satellite = new SimulatedArtist { artistId = "scene-probe-satellite", homeRegion = "West Coast",
                geography = new GeographicIdentity { originPlaceId = "oakland", originEvidence = PlaceEvidence.Explicit } };
            LocalSceneIdentityService.EnsureArtist(satellite);
            Check(satellite.geography.originPlaceId == "oakland" && satellite.geography.basePlaceId == "oakland", "credible satellite origin retained");
            var foreign = new AILabel { headquartersCity = "Liverpool", homeRegion = "eastcoast", homeCityId = "new_york" };
            LocalSceneIdentityService.EnsureLabel(foreign);
            Check(foreign.geography.basePlaceId == "gb_liverpool" && foreign.homeCityId == "new_york" && foreign.homeRegion == "eastcoast", "foreign HQ and US distribution remain separate");
            var ambiguous = new AILabel { headquartersCity = "Jackson", homeRegion = "deepsouth", homeCityId = "memphis" };
            LocalSceneIdentityService.EnsureLabel(ambiguous);
            Check(ambiguous.geography.basePlaceId == "jackson_ms" && ambiguous.headquartersCity == "Jackson" && ambiguous.homeCityId == "memphis", "Jackson resolved without moving HQ or sales projection");
            Check(ScenePlaceRegistry.ResolveHeadquarters("Jackson", "rockies") == null && ScenePlaceRegistry.ByName("Jackson") == null, "Jackson requires Mississippi region context");
            var repair = new AILabel { headquartersCity = "Newark", homeRegion = "eastcoast", homeCityId = "new_york",
                geography = new GeographicIdentity { assignmentSource = "unresolved-literal-hq; distribution-proxy-not-origin" } };
            LocalSceneIdentityService.EnsureLabel(repair);
            string repaired = Json(repair);
            LocalSceneIdentityService.EnsureLabel(repair);
            Check(Json(repair) == repaired && repair.geography.basePlaceId == "newark" && repair.geography.moves == null, "old unresolved projection repaired once without a move");
            repair.geography.basePlaceId = "oakland"; repair.geography.originPlaceId = "oakland";
            LocalSceneIdentityService.EnsureLabel(repair);
            Check(repair.geography.basePlaceId == "oakland", "credible saved HQ identity is never overwritten");
            var unknown = new SimulatedArtist { artistId = "scene-probe-unknown", homeRegion = "unknown" };
            LocalSceneIdentityService.EnsureArtist(unknown);
            Check(unknown.geography.basePlaceId == null && unknown.homeRegion == "unknown", "unknown region preserved");
            var fresh = new SimulatedArtist { artistId = "scene-probe-fresh", homeRegion = "Deep South", cohort = ArtistCohort.RuntimeFormation };
            LocalSceneIdentityService.EnsureArtist(fresh, generated: true, formationPlaceId: "miami");
            Check(fresh.geography.originPlaceId == "miami" && fresh.geography.originEvidence == PlaceEvidence.Simulated, "budgeted formation has procedural hometown");
            var traveler = new Musician { personId = "scene-probe-traveler", geography = new GeographicIdentity {
                originPlaceId = "oakland", originEvidence = PlaceEvidence.Explicit,
                basePlaceId = "san_francisco", baseEvidence = PlaceEvidence.Explicit } };
            LocalSceneIdentityService.PersonJoined(traveler, fresh, 1964);
            LocalSceneIdentityService.PersonJoined(traveler, fresh, 1964);
            Check(traveler.geography.originPlaceId == "oakland" && traveler.geography.basePlaceId == "miami" &&
                traveler.geography.moves.Count == 1 && traveler.geography.moves[0].year == 1964 &&
                traveler.geography.moves[0].precision == PlaceDatePrecision.Year, "existing join records one dated move and preserves origin");
            Check(Json(JsonSerializer.Deserialize<GeographicIdentity>(Json(traveler.geography), SaveGameService.TestJsonOptions)) ==
                Json(traveler.geography), "move history serializer round trip");
            GeographicIdentity restored = JsonSerializer.Deserialize<GeographicIdentity>(Json(legacy.geography), SaveGameService.TestJsonOptions);
            Check(Json(restored) == Json(legacy.geography), "identity serializer round trip");
            bool rejected = false;
            try { LocalScenes.Configure(new[] { "--observe-local-scenes", "--disable-local-scenes" }); } catch (ArgumentException) { rejected = true; }
            Check(rejected, "contradictory flags rejected");
            rejected = false;
            try { LocalScenes.Configure(new[] { "--observe-local-scenes", "--enable-scene-recruitment" }); } catch (ArgumentException) { rejected = true; }
            Check(rejected, "later phase flags rejected");
            rejected = false;
            try { LocalSceneIdentityService.BeginRestore(new SceneIdentitySaveData { ContentVersion = 99 }, 1001); } catch (InvalidOperationException) { rejected = true; }
            Check(rejected, "newer scene content rejected before restore");
            LocalScenes.Configure(new[] { "--disable-local-scenes" });
            string frozen = Json(legacy.geography);
            LocalSceneIdentityService.EnsureArtist(legacy, generated: true, formationPlaceId: "new_york");
            Check(Json(legacy.geography) == frozen, "disabling freezes saved identity");
            var off = new SimulatedArtist { artistId = "scene-probe-off", homeRegion = "Deep South" };
            LocalSceneIdentityService.EnsureArtist(off, generated: true);
            Check(off.geography == null, "all off does not assign state");
        } finally {
            LocalScenes.Configure(wasObserving ? new[] { "--observe-local-scenes" } : new[] { "--disable-local-scenes" });
            LocalSceneIdentityService.BeginRestore(original.SceneIdentity, SimulationSeedBootstrap.RequestedSeed);
            LocalSceneIdentityService.CompleteRestore();
        }
        string after = JsonSerializer.Serialize(WorldStateService.Capture(), SaveGameService.TestJsonOptions);
        Check(originalJson == after, "fixed checks preserve live world, counters, population RNG and band ledgers");
        Godot.GD.Print($"SCENE_IDENTITY_CHECK_PASS checks={count}");
    }
}
