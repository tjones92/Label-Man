using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

public static class SceneRecruitmentChecks {
    public static void Run() {
        if (!LocalScenes.Recruitment) throw new InvalidOperationException("Enable scene recruitment for these checks.");
        int checks = 0;
        void Check(bool condition, string why) {
            if (!condition) throw new InvalidOperationException("SCENE_RECRUITMENT_CHECK_FAILED: " + why);
            checks++;
        }
        AILabel Label(string place, LabelTier tier = LabelTier.Small) => new() {
            labelId = "geographic-probe", tier = tier, scoutingAbility = .75f, nationalReach = .8f,
            geography = new GeographicIdentity { basePlaceId = place }, homeRegion = "deepsouth"
        };
        SimulatedArtist Act(string place) => new() {
            artistId = "geographic-probe-act", geography = new GeographicIdentity { basePlaceId = place, originPlaceId = place }
        };
        var indie = Label("memphis", LabelTier.Independent);
        var near = Act("nashville"); var far = Act("seattle");
        Check(SceneRecruitmentService.Explain(indie, Act("memphis"), 1).Route == "Catchment", "same scene accessible");
        Check(SceneRecruitmentService.Explain(indie, near, 1).Route == "RoadCircuit", "Memphis can reach Nashville");
        Check(!SceneRecruitmentService.Explain(indie, far, 1).Eligible, "Memphis indie cannot shop Seattle without a route");
        Check(SceneRecruitmentService.Explain(Label("san_francisco"), Act("oakland"), 1).Route == "Catchment", "satellites keep identity with catchment access");
        Check(SceneRecruitmentService.Explain(Label("billings"), Act("billings"), 1).Eligible, "small-town home opportunity");
        Check(!SceneRecruitmentService.Explain(Label("muscle_shoals"), near, 1).Eligible, "unknown road does not become zero miles");
        Check(SceneRecruitmentService.Explain(Label(null), near, 1).Route == "UnknownPlace", "unmapped HQ never uses sales proxy");
        Check(!SceneRecruitmentService.Explain(Label("memphis", LabelTier.Major), Act("gb_liverpool"), 1).Eligible, "no invented international licensing link");
        Check(SceneRecruitmentService.Explain(Label("gb_liverpool"), Act("gb_liverpool"), 1).Eligible, "foreign local life predates US entry");
        Check(SceneRecruitmentService.Explain(Label("new_york", LabelTier.Major), far, 1).Route == "NationalAr", "capable major has bounded national attention");
        var weakMajor = Label("new_york", LabelTier.Major); weakMajor.nationalReach = .1f;
        Check(!SceneRecruitmentService.Explain(weakMajor, far, 1).Eligible, "major tier alone does not confer national A&R");
        indie.ownedReach = 1f; indie.nationalReach = 1f;
        Check(!SceneRecruitmentService.Explain(indie, far, 1).Eligible, "distribution reach cannot grant indie national scouting");
        far.sceneParticipations = new List<SceneParticipation> { new() {
            sceneId = "memphis", placeId = "memphis", relationship = SceneRelationship.TouringGuest,
            startWeek = 2, endWeek = 3, provenance = "probe: dated support bill"
        } };
        Check(!SceneRecruitmentService.Explain(indie, far, 1).Eligible, "future guest unavailable");
        Check(SceneRecruitmentService.Explain(indie, far, 2).Route == "LocalVisitor", "real distant guest eligible");
        Check(!SceneRecruitmentService.Explain(indie, far, 4).Eligible, "expired guest cannot bypass commit");
        Check(far.geography.basePlaceId == "seattle" && far.geography.originPlaceId == "seattle", "query never relocates guest");
        SceneRecruitmentService.RecordSigning(indie, far, 2, 1960, "Probe");
        string recordJson = JsonSerializer.Serialize(far, SaveGameService.TestJsonOptions);
        var clone = JsonSerializer.Deserialize<SimulatedArtist>(recordJson, SaveGameService.TestJsonOptions);
        Check(clone.sceneRecruitmentHistory.Single().Route == "LocalVisitor", "signing evidence serializes");
        Check(clone.sceneRecruitmentHistory.Single().AccessEvidence == "probe: dated support bill", "visitor evidence retained after expiry");
        Check(clone.geography.originPlaceId == "seattle" && clone.geography.basePlaceId == "seattle", "contract retains base and origin");
        var pool = Enumerable.Range(0, 25).Select(i => new SimulatedArtist {
            artistId = "local-" + i, geography = new GeographicIdentity { basePlaceId = "memphis" }
        }).Append(Act("seattle")).ToList();
        var slate = SceneRecruitmentService.Slate(indie, pool, 1, 0, 4, out int count);
        Check(count == 25 && slate.Count == 4, "finite attention over actual accessible population");
        Check(slate.Select(a => a.artistId).Distinct().Count() == 4, "slate unique");
        Check(slate.Select(a => a.artistId).SequenceEqual(SceneRecruitmentService.Slate(indie, pool.AsEnumerable().Reverse(), 1, 0, 4, out _).Select(a => a.artistId)), "stable slate independent of input ordering");
        var original = WorldStateService.Capture();
        GD.Seed(88712); float next = GD.Randf(); GD.Seed(88712);
        _ = SceneRecruitmentService.Slate(indie, pool, 1, 0, 4, out _);
        Check(GD.Randf() == next, "recruitment query preserves global RNG");
        Check(WorldStateService.Capture().PopulationRngState == original.PopulationRngState, "recruitment query preserves population RNG");
        var world = ArtistManager.Instance.GetAllArtists().ToList();
        var histories = world.SelectMany(a => a.sceneRecruitmentHistory ?? new()).ToList();
        Check(histories.Count > 0 && histories.All(r => r.Route is "Catchment" or "RoadCircuit" or "NationalAr" or "LocalVisitor"), "opening roster signings all explained");
        Check(histories.All(r => r.BasePlaceId == ArtistManager.Instance.GetArtist(r.ArtistId).geography.basePlaceId), "opening contracts do not teleport acts");
        var labels = ChartManager.Instance.GetAllLabels();
        Check(labels.SelectMany(l => l.roster).All(a => a.sceneRecruitmentHistory?.Any(r => r.LabelId == a.labelId) == true), "no initialization signing bypass");
        var live = labels.First(l => l.roster.Count > 0 && l.geography?.basePlaceId != null);
        var supply = ArtistManager.Instance.GetUnsignedArtists();
        int year = TimeManager.Instance.CurrentDate.year;
        var normal = RosterManager.SceneSupplyForProbe(live, year, false, supply, out int normalCount);
        var recovery = RosterManager.SceneSupplyForProbe(live, year, true, supply, out int recoveryCount);
        Check(normalCount == recoveryCount && normal.Select(a => a.artistId).SequenceEqual(recovery.Select(a => a.artistId)), "recovery cannot widen geographic access or attention");
        Check(normal.All(a => SceneRecruitmentService.Explain(live, a, ChartManager.Instance.GetCurrentChartWeek()).Eligible), "native supply selector filters geography");
        // Access-first fallback: a globally available but distant preferred style must not
        // suppress an accessible secondary style (the former initialization defect).
        var styleLabel = Label("memphis");
        styleLabel.preferredGenres = new[] { Genre.Country };
        styleLabel.secondaryGenres = new[] { Genre.RnB };
        var remoteStyle = Act("seattle"); remoteStyle.artistId = "remote-country"; remoteStyle.primaryGenre = Genre.Country;
        var localStyle = Act("memphis"); localStyle.artistId = "local-rnb"; localStyle.primaryGenre = Genre.RnB;
        var otherStyle = Act("memphis"); otherStyle.artistId = "local-jazz"; otherStyle.primaryGenre = Genre.Jazz;
        Check(RosterManager.SceneInitialPool(styleLabel, new[] { remoteStyle, localStyle, otherStyle })
            .Select(a => a.artistId).SequenceEqual(new[] { localStyle.artistId }), "accessible secondary survives remote preferred supply");
        var localPreferred = Act("memphis"); localPreferred.artistId = "local-country"; localPreferred.primaryGenre = Genre.Country;
        Check(RosterManager.SceneInitialPool(styleLabel, new[] { localPreferred, localStyle }).Single() == localPreferred,
            "reachable preferred style retains priority");
        Check(RosterManager.SceneInitialPool(styleLabel, new[] { remoteStyle, otherStyle }).Single() == otherStyle,
            "broader style fallback remains inside reach");
        Check(RosterManager.SceneInitialPool(styleLabel, new[] { remoteStyle }).Count == 0,
            "no style fallback grants unexplained national access");
        var ordering = new[] { Label("memphis"), Label("memphis", LabelTier.Major), Label("memphis", LabelTier.Independent) };
        for (int i = 0; i < ordering.Length; i++) ordering[i].labelId = "order-" + i;
        Check(RosterManager.SceneInitializationOrder(ordering, 0).First().tier == LabelTier.Major,
            "majors retain first opportunity each round");
        Check(RosterManager.SceneInitializationOrder(ordering, 3).Select(l => l.labelId)
            .SequenceEqual(RosterManager.SceneInitializationOrder(ordering.Reverse(), 3).Select(l => l.labelId)),
            "allocation order is independent of list storage");
        var observations = RosterManager.Instance.SceneInitialization;
        Check(observations.Count == labels.Count, "all launch labels have desired headcount diagnostics");
        Check(observations.Values.All(o => o.Filled <= o.Target), "rounds never exceed tier launch targets");
        Check(observations.Values.Where(o => o.Filled < o.Target).All(o => o.AccessibleAny == 0),
            "underfilled launch rosters identify actual exhausted accessible supply");
        var appointments = RosterManager.Instance.SceneInitializationAppointments;
        Check(appointments.Count == observations.Values.Sum(o => o.Filled), "every launch allocation retains an appointment");
        Check(appointments.GroupBy(a => (a.Round, a.LabelId)).All(g => g.Count() == 1),
            "no label fills twice before its competitors get a round");
        Check(appointments.Select(a => a.ArtistId).Distinct().Count() == appointments.Count,
            "initial competition never assigns an act twice");
        Check(appointments.All(a => a.SlateSize <= RosterManager.GetDiscoverySlateSize(labels.First(l => l.labelId == a.LabelId).scoutingAbility)),
            "opening attention remains bounded by native scouting");
        Check(appointments.Any(a => Math.Abs(a.TrueQuality - a.PerceivedQuality) > .01f),
            "launch allocation uses imperfect reads rather than true quality");

        string json = JsonSerializer.Serialize(original, SaveGameService.TestJsonOptions);
        WorldStateService.Apply(JsonSerializer.Deserialize<WorldSaveData>(json, SaveGameService.TestJsonOptions), TimeManager.Instance.CurrentDate, SimulationSeedBootstrap.RequestedSeed);
        Check(json == JsonSerializer.Serialize(WorldStateService.Capture(), SaveGameService.TestJsonOptions), "world roundtrip retains recruitment evidence");
        string[] invalid = { "--disable-local-scenes", "--disable-persistent-scenes", "--observe-local-scenes", "--disable-genre-market-v2", "--disable-scene-recruitment" };
        foreach (string flag in invalid) {
            bool rejected = false;
            try { LocalScenes.Configure(new[] { "--enable-scene-recruitment", flag }); } catch (ArgumentException) { rejected = true; }
            Check(rejected && LocalScenes.Recruitment, "contradictory flags reject before mutation: " + flag);
        }
        GD.Print($"SCENE_RECRUITMENT_CHECK_PASS checks={checks} contracts={histories.Count}");
    }
}
