using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

public static class SceneFeedbackChecks {
    public static void Run() {
        int checks = 0;
        void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("SCENE_FEEDBACK_CHECK_FAILED: " + why); checks++; }
        string Json(object value) => JsonSerializer.Serialize(value, SaveGameService.TestJsonOptions);
        const string enable = "--enable-scene-attention-feedback";
        LocalScenes.Configure(Array.Empty<string>());
        Check(LocalScenes.AttentionFeedback, "validated attention defaults on");
        LocalScenes.Configure(new[] { enable });
        Check(LocalScenes.AttentionFeedback && LocalScenes.Recruitment && LocalScenes.Rooms, "live dependencies enabled");
        foreach (string flag in new[] { "--disable-local-scenes", "--disable-persistent-scenes", "--disable-scene-rooms",
            "--disable-scene-recruitment", "--disable-genre-market-v2", "--disable-artist-population-lifecycle",
            "--observe-local-scenes", "--disable-scene-attention-feedback" }) {
            bool rejected = false;
            try { LocalScenes.Configure(new[] { enable, flag }); } catch (ArgumentException) { rejected = true; }
            Check(rejected && LocalScenes.AttentionFeedback, "invalid dependencies reject atomically: " + flag);
        }
        var row = new SceneRecruitmentRecord { ArtistId = "probe", LabelId = "probe-label", Week = 10,
            Phase = "DailyMarket", BasePlaceId = "memphis", OriginPlaceId = "memphis", HqPlaceId = "memphis", SigningGenre = Genre.Country };
        double Boost(int week) => new SceneAttentionFeedbackSnapshot(new[] { row }, week).Multiplier("memphis", Genre.Country);
        Check(Boost(10) == 1 && Boost(11) > 1, "same-week signing cannot feed back until next week");
        Check(Math.Abs(Boost(11) - (1 + .25 * (1 - Math.Exp(-.05)))) < 1e-12, "single conserved signing budget drives modest boost");
        Check(Math.Abs(Boost(19) - (1 + .25 * (1 - Math.Exp(-.025)))) < 1e-12, "eight-week half-life retained after lag");
        Check(Boost(116) == 1, "expired attention returns to baseline");
        var snap = new SceneAttentionFeedbackSnapshot(new[] { row, row }, 11);
        Check(snap.Multiplier("memphis", Genre.Country) == Boost(11), "duplicates cannot multiply feedback");
        Check(snap.Multiplier("memphis", Genre.Jazz) == 1 && snap.Multiplier("seattle", Genre.Country) == 1,
            "feedback follows event genre and attributed place");
        Check(snap.Multiplier(null, Genre.Country) == 1 && snap.Multiplier("unknown-place", Genre.Country) == 1, "unknown place stays neutral");
        row.SigningGenre = null;
        Check(Boost(11) == 1, "unknown legacy signing genre supplies no genre boost");
        row.SigningGenre = Genre.Country; row.Phase = "Initialization";
        Check(Boost(11) == 1, "opening allocation is not a success shock");
        row.Phase = "DailyMarket";
        var shocks = Enumerable.Range(0, 1000).Select(i => new SceneRecruitmentRecord {
            ArtistId = "shock-" + i, LabelId = "label", Week = 10, Phase = "DailyMarket",
            BasePlaceId = "new_york", OriginPlaceId = "newark", HqPlaceId = "new_york", SigningGenre = Genre.Jazz }).ToArray();
        var large = new SceneAttentionFeedbackSnapshot(shocks, 11);
        Check(large.Multiplier("newark", Genre.Jazz) == large.Multiplier("new_york", Genre.Jazz), "satellite claims merge into catchment");
        Check(large.Multiplier("new_york", Genre.Jazz) <= 1.25 && large.Multiplier("new_york", Genre.Jazz) > 1.24, "large shock saturates at bounded multiplier");
        Check(large.Multiplier("new_york", Genre.Jazz) == new SceneAttentionFeedbackSnapshot(shocks.Reverse(), 11).Multiplier("new_york", Genre.Jazz), "stable projection independent of record order");

        var original = JsonSerializer.Deserialize<WorldSaveData>(Json(WorldStateService.Capture()), SaveGameService.TestJsonOptions);
        var carrier = ArtistManager.Instance.GetAllArtists().First(a => a.sceneRecruitmentHistory?.Count > 0);
        // A focused committed-ledger intervention, then a full-world restore; no contract or birth factory.
        carrier.sceneRecruitmentHistory.AddRange(shocks);
        var label = new AILabel { labelId = "feedback-label", tier = LabelTier.Small,
            geography = new GeographicIdentity { basePlaceId = "new_york" } };
        var pool = Enumerable.Range(0, 80).Select(i => new SimulatedArtist {
            artistId = "candidate-" + i, primaryGenre = i % 2 == 0 ? Genre.Jazz : Genre.Country,
            geography = new GeographicIdentity { basePlaceId = "new_york" } }).Append(new SimulatedArtist {
                artistId = "distant", primaryGenre = Genre.Jazz, geography = new GeographicIdentity { basePlaceId = "seattle" } }).ToArray();
        int changed = 0;
        for (int window = 0; window < 64; window++) {
            LocalScenes.Configure(new[] { "--disable-scene-attention-feedback" });
            var baseline = SceneRecruitmentService.Slate(label, pool, 11, window, 8, out int offCount);
            LocalScenes.Configure(new[] { enable });
            var live = SceneRecruitmentService.Slate(label, pool, 11, window, 8, out int onCount);
            if (!baseline.Select(a => a.artistId).OrderBy(id => id, StringComparer.Ordinal).SequenceEqual(live.Select(a => a.artistId).OrderBy(id => id, StringComparer.Ordinal))) changed++;
            Check(onCount == offCount && onCount == 80 && live.Count == 8 && !live.Any(a => a.artistId == "distant"), "fixed slate budget and geographic access: " + window);
        }
        Check(changed > 0, "controlled signing shock changes actual live slates");
        var saved = WorldStateService.Capture(); string before = Json(saved);
        GD.Seed(90142); uint expected = GD.Randi(); GD.Seed(90142);
        var ids = SceneRecruitmentService.Slate(label, pool, 11, 5, 8, out _).Select(a => a.artistId).ToArray();
        Check(expected == GD.Randi(), "live selection does not consume global RNG");
        Check(before == Json(WorldStateService.Capture()), "live query preserves complete saved world and population RNG");
        Check(ids.SequenceEqual(SceneRecruitmentService.Slate(label, pool.Reverse(), 11, 5, 8, out _).Select(a => a.artistId)), "live slate stable under pool ordering");
        WorldStateService.Apply(JsonSerializer.Deserialize<WorldSaveData>(before, SaveGameService.TestJsonOptions), TimeManager.Instance.CurrentDate, SimulationSeedBootstrap.RequestedSeed);
        Check(ids.SequenceEqual(SceneRecruitmentService.Slate(label, pool, 11, 5, 8, out _).Select(a => a.artistId)), "full save/reload reproduces live slate after cache invalidation");
        WorldStateService.Apply(original, TimeManager.Instance.CurrentDate, SimulationSeedBootstrap.RequestedSeed);
        Check(SceneAttentionFeedback.Capture(11).Multiplier("new_york", Genre.Jazz) == 1, "rollback to same week cannot retain future shock cache");
        var sameWeek = SceneRecruitmentService.Slate(label, pool, 0, 5, 8, out _).Select(a => a.artistId).ToArray();
        LocalScenes.Configure(Array.Empty<string>());
        Check(sameWeek.SequenceEqual(SceneRecruitmentService.Slate(label, pool, 0, 5, 8, out _).Select(a => a.artistId)), "neutral live opening equals disabled path");
        LocalScenes.Configure(new[] { enable });
        GD.Print($"SCENE_FEEDBACK_CHECK_PASS checks={checks} changedShockSlates={changed}");
    }
}


