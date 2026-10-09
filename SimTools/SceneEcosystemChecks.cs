using System;
using System.Linq;
using System.Text.Json;
using Godot;

public static class SceneEcosystemChecks {
    public static void Run() {
        int checks = 0;
        void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("SCENE_ECOSYSTEM_CHECK_FAILED: " + why); checks++; }
        string Json(object o) => JsonSerializer.Serialize(o, SaveGameService.TestJsonOptions);
        var date = TimeManager.Instance.CurrentDate;
        var original = JsonSerializer.Deserialize<WorldSaveData>(Json(WorldStateService.Capture()), SaveGameService.TestJsonOptions);
        var flags = new[] { "--enable-scene-institutions", "--enable-scene-relocation", "--enable-scene-price-feedback" };
        try {
            LocalScenes.Configure(Array.Empty<string>());
            Check(LocalScenes.AttentionFeedback && LocalScenes.Institutions && LocalScenes.Relocation && LocalScenes.PriceFeedback, "independent defaults");
            foreach (string slice in new[] { "institutions", "relocation", "price-feedback" }) {
                foreach (string off in new[] { "--disable-local-scenes", "--disable-scene-rooms", "--disable-scene-recruitment", "--disable-scene-" + slice }) {
                    bool rejected = false;
                    try { LocalScenes.Configure(flags.Concat(new[] { off })); } catch (ArgumentException) { rejected = true; }
                    Check(rejected && LocalScenes.Institutions, "dependency rejection atomic: " + slice + off);
                }
            }
            bool missing = false;
            try { LocalScenes.Configure(new[] { "--enable-scene-relocation", "--disable-scene-institutions" }); } catch (ArgumentException) { missing = true; }
            Check(missing, "relocation needs institution owner");
            var lead = SceneDynamicsMobility.Evaluate("probe", null, "san_antonio", Genre.Country, 1960, 1, false);
            Check(lead != null, "travel feasible destination fixture");
            string destination = lead.ToPlaceId;
            var a = ArtistManager.Instance.GetAllArtists().First(a => !a.isPlayerOwned && !string.IsNullOrEmpty(a.labelId) && a.isActive &&
                a.members.Count(m => m.isActive && m.lifeState == MemberLifeState.Active) == 1 &&
                a.members.Where(m => m.isActive && m.lifeState == MemberLifeState.Active).All(m => SceneEcosystemService.Consents(m, "san_antonio", destination, 0)));
            var sponsor = ChartManager.Instance.GetAllLabels().First(l => l.labelId == a.labelId);
            sponsor.cashReserves = 1000000;
            a.primaryGenre = Genre.Country;
            a.geography.basePlaceId = "san_antonio";
            LocalSceneIdentityService.EnsureArtist(a);
            string origin = a.geography.originPlaceId;
            var members = a.members.Where(m => m.isActive && m.lifeState == MemberLifeState.Active).ToArray();
            string personOrigin = members[0].geography.originPlaceId;
            LocalSceneRoomService.BeginRestore(new()); LocalSceneRoomService.CompleteRestore();
            SceneEcosystemService.BeginRestore(null); SceneEcosystemService.CompleteRestore();
            LocalScenes.Configure(flags);
            string roomId = destination + ":road";
            float cash = sponsor.cashReserves, expenses = sponsor.monthlyExpenses;
            sponsor.cashReserves = 0;
            Check(!SceneEcosystemService.FundProgram(sponsor, roomId, out _) && SceneEcosystemService.Programs.Count == 0, "unfunded investment cannot add opportunity");
            sponsor.cashReserves = cash;
            Check(!SceneEcosystemService.FundProgram(sponsor, destination + ":trade", out _) && sponsor.cashReserves == cash, "trade appointment cannot add stage capacity or spend");
            Check(SceneEcosystemService.FundProgram(sponsor, roomId, out _), "fund program");
            var p = SceneEcosystemService.Programs.Single();
            Check(sponsor.cashReserves == cash - SceneEcosystemService.ProgramCost && sponsor.monthlyExpenses == expenses + SceneEcosystemService.ProgramCost, "real expense accounting");
            Check(!SceneEcosystemService.FundProgram(sponsor, roomId, out _) && SceneEcosystemService.Programs.Count == 1, "duplicate funding cannot farm slots");
            var room = SceneRoomCatalog.Get(roomId);
            Check(SceneEcosystemService.Slots(room, p.StartDay - 1) == 2 && SceneEcosystemService.Slots(room, p.StartDay) == 3 && SceneEcosystemService.Slots(room, p.ThroughDay + 1) == 2, "dated opening and expiry");
            var detached = SceneEcosystemService.Programs.Single(); detached.StartDay = 0;
            Check(SceneEcosystemService.Programs.Single().StartDay == 14, "program queries return detached state");
            Check(SceneEcosystemService.ProposeMove(sponsor, a.artistId, p.Id, out string message), "funded consented move: " + message);
            var move = SceneEcosystemService.Moves.Single();
            Check(a.geography.basePlaceId == "san_antonio" && move.ArrivalDay > move.DepartureDay, "planning does not teleport");
            Check(!SceneEcosystemService.ProposeMove(sponsor, a.artistId, p.Id, out _), "pending move cannot spend twice");
            Check(SceneEcosystemService.AvailableAt(a.artistId, "san_antonio", move.DepartureDay - 1) &&
                !SceneEcosystemService.AvailableAt(a.artistId, destination, move.DepartureDay), "in transit unavailable for stage work");
            if (OS.GetCmdlineUserArgs().Contains("--scene-ecosystem-timeline-check")) {
                LocalSceneRoomService.EnsureCalendar(LocalSceneRoomService.Date(move.DepartureDay - 1));
                Check(!LocalSceneRoomService.HasFutureCommitment(a.artistId, move.DepartureDay), "new calendars cannot create source engagements across departure");
            }
            var snapshot = JsonSerializer.Deserialize<WorldSaveData>(Json(WorldStateService.Capture()), SaveGameService.TestJsonOptions);
            int count = snapshot.Artists.Count;
            WorldStateService.Apply(JsonSerializer.Deserialize<WorldSaveData>(Json(snapshot), SaveGameService.TestJsonOptions), date, SimulationSeedBootstrap.RequestedSeed);
            Check(Json(snapshot) == Json(WorldStateService.Capture()), "pending move and program roundtrip");
            LocalScenes.Configure(new[] { "--enable-scene-institutions", "--disable-scene-relocation" });
            SceneEcosystemService.Advance(LocalSceneRoomService.Date(move.ArrivalDay));
            Check(SceneEcosystemService.Moves.Single().Status == SceneMoveStatus.Planned, "disabling relocation freezes commitments without erasing them");
            LocalScenes.Configure(flags);
            a = ArtistManager.Instance.GetArtist(move.ArtistId);
            GD.Seed(199); uint draw = GD.Randi(); GD.Seed(199);
            SceneEcosystemService.Advance(LocalSceneRoomService.Date(move.ArrivalDay));
            Check(GD.Randi() == draw, "arrival consumes no global RNG");
            Check(a.geography.basePlaceId == destination && a.geography.originPlaceId == origin &&
                a.members.First(m => m.personId == move.PersonIds[0]).geography.originPlaceId == personOrigin, "arrival changes bases, preserves act and person origins");
            Check(LocalSceneIdentityService.ArtistsBasedAt(destination).Contains(a.artistId) &&
                LocalScenePersistenceService.MembersOf(destination).Contains(a.artistId) && !LocalScenePersistenceService.MembersOf("san_antonio").Contains(a.artistId), "indices and resident histories reconcile");
            string arrived = Json(WorldStateService.Capture());
            SceneEcosystemService.Advance(LocalSceneRoomService.Date(move.ArrivalDay));
            Check(arrived == Json(WorldStateService.Capture()) && count == WorldStateService.Capture().Artists.Count, "arrival idempotent and conserves population");
            if (OS.GetCmdlineUserArgs().Contains("--scene-ecosystem-timeline-check")) {
                var bills = LocalSceneRoomService.Calendar(destination, LocalSceneRoomService.Date(move.ArrivalDay)).Where(b => b.RoomId == roomId).ToArray();
                Check(bills.Any(b => b.Appearances.Count == 3) && bills.All(b => b.Appearances.All(s => s.EndHour <= b.EndHour)), "funded opportunity becomes a real third set within room hours");
                Check(bills.Where(b => b.Appearances.Count == 3).All(b => b.Appearances.Select(s => s.Role).SequenceEqual(new[] { "opening set", "middle set", "closing set" })), "funded three-set bill has distinct billed roles");
            }
            WorldStateService.Apply(JsonSerializer.Deserialize<WorldSaveData>(Json(snapshot), SaveGameService.TestJsonOptions), date, SimulationSeedBootstrap.RequestedSeed);
            a = ArtistManager.Instance.GetArtist(move.ArtistId); a.labelId = null;
            SceneEcosystemService.Advance(LocalSceneRoomService.Date(move.ArrivalDay));
            Check(SceneEcosystemService.Moves.Single().Status == SceneMoveStatus.Cancelled && a.geography.basePlaceId == "san_antonio", "stale contract cancels without moving");
            WorldStateService.Apply(JsonSerializer.Deserialize<WorldSaveData>(Json(snapshot), SaveGameService.TestJsonOptions), date, SimulationSeedBootstrap.RequestedSeed);
            a = ArtistManager.Instance.GetArtist(move.ArtistId); a.members[0].isActive = false;
            SceneEcosystemService.Advance(LocalSceneRoomService.Date(move.ArrivalDay));
            Check(SceneEcosystemService.Moves.Single().Status == SceneMoveStatus.Cancelled && a.geography.basePlaceId == "san_antonio", "stale lineup cancels without moving");
            WorldStateService.Apply(JsonSerializer.Deserialize<WorldSaveData>(Json(snapshot), SaveGameService.TestJsonOptions), date, SimulationSeedBootstrap.RequestedSeed);
            a = ArtistManager.Instance.GetArtist(move.ArtistId); a.primaryGenre = Genre.Jazz;
            SceneEcosystemService.Advance(LocalSceneRoomService.Date(move.ArrivalDay));
            Check(SceneEcosystemService.Moves.Single().Status == SceneMoveStatus.Cancelled && a.geography.basePlaceId == "san_antonio", "changed genre cannot move into an unsuitable funded program");
            WorldStateService.Apply(JsonSerializer.Deserialize<WorldSaveData>(Json(snapshot), SaveGameService.TestJsonOptions), date, SimulationSeedBootstrap.RequestedSeed);
            a = ArtistManager.Instance.GetArtist(move.ArtistId);
            var carrier = ArtistManager.Instance.GetAllArtists().First(x => x.sceneRecruitmentHistory?.Count > 0);
            carrier.sceneRecruitmentHistory.AddRange(Enumerable.Range(0, 1000).Select(i => new SceneRecruitmentRecord {
                ArtistId = "price-" + i, LabelId = "label", Week = 10, Phase = "DailyMarket", SigningGenre = Genre.Country,
                BasePlaceId = "san_antonio", OriginPlaceId = "san_antonio", HqPlaceId = "san_antonio" }));
            ChartManager.Instance.RestoreChartWeek(10); SceneAttentionFeedback.Reset();
            Check(ScenePriceFeedback.Multiplier(a) == 1, "price uses one-week lag");
            ChartManager.Instance.RestoreChartWeek(11); SceneAttentionFeedback.Reset();
            float multiplier = ScenePriceFeedback.Multiplier(a);
            Check(multiplier > 1.099f && multiplier <= 1.1f, "price saturation bounded to ten percent");
            var label = ChartManager.Instance.GetAllLabels().First(l => l.labelId == a.labelId);
            float priced = label.CalculateManagerAdjustedAdvance(a);
            var askMethod = typeof(PlayerDesk).GetMethod("VenueAdvanceAsk", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            float pricedPlayer = (float)askMethod.Invoke(null, new object[] { a, PlayerDesk.ScoutingVenue.HonkyTonks, .1f });
            LocalScenes.Configure(new[] { "--enable-scene-institutions", "--enable-scene-relocation", "--disable-scene-attention-feedback", "--disable-scene-price-feedback" });
            Check(ScenePriceFeedback.Multiplier(a) == 1 && Math.Abs(priced / label.CalculateManagerAdjustedAdvance(a) - multiplier) < .00001f, "shared advance gates and term sheets use price exactly once; discovery independently off");
            float baselinePlayer = (float)askMethod.Invoke(null, new object[] { a, PlayerDesk.ScoutingVenue.HonkyTonks, .1f });
            Check(Math.Abs(pricedPlayer / baselinePlayer - multiplier) < .00001f, "low-dollar player asks retain the price cap after rounding");
            LocalScenes.Configure(flags.Concat(new[] { "--disable-scene-attention-feedback" }));
            Check(ScenePriceFeedback.Multiplier(a) > 1, "price is independent of discovery enablement");
            ChartManager.Instance.RestoreChartWeek(116); SceneAttentionFeedback.Reset();
            Check(ScenePriceFeedback.Multiplier(a) == 1, "price feedback expires to baseline");
            var invalid = LocalSceneRoomService.Copy(snapshot.SceneEcosystem); invalid.Moves[0].ArrivalDay = -1;
            bool invalidRejected = false;
            try { SceneEcosystemService.ValidateRestore(invalid); } catch (InvalidOperationException) { invalidRejected = true; }
            Check(invalidRejected, "invalid movement dates rejected before restore");
        } finally { WorldStateService.Apply(original, date, SimulationSeedBootstrap.RequestedSeed); LocalScenes.Configure(Array.Empty<string>()); }
        GD.Print($"SCENE_ECOSYSTEM_CHECK_PASS checks={checks}");
    }
}

