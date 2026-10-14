using System;
using System.Linq;
using System.Text.Json;
using Godot;

// Controlled knowledge fixture over canonical artists and an actual announced booking.
public static class ScoutingLossChecks {
    public static int Run(string artistId) {
        int checks = 0;
        void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("SCOUTING_LOSS_FAILED: " + why); checks++; }
        string Json(object value) => JsonSerializer.Serialize(value, SaveGameService.TestJsonOptions);
        var desk = PlayerDesk.Instance; var clock = TimeManager.Instance; var am = ArtistManager.Instance;
        var saved = desk.CaptureState(); var rooms = WorldStateService.Capture().SceneRooms;
        var date = clock.CurrentDate; int hour = clock.CurrentHour, minute = clock.CurrentMinute;
        var bill = LocalSceneRoomService.Calendar(desk.CurrentCityId, date).First(b => b.Status == SceneBillStatus.Scheduled &&
            b.Appearances.Any(a => a.ArtistId != artistId && a.Status == SceneBillStatus.Scheduled && a.EndHour <= clock.HardStopHour));
        var slot = bill.Appearances.First(a => a.ArtistId != artistId && a.Status == SceneBillStatus.Scheduled && a.EndHour <= clock.HardStopHour);
        var lost = am.GetArtist(artistId); var target = am.GetArtist(slot.ArtistId);
        var others = am.GetAllArtists().Where(a => a.artistId != artistId && a.artistId != target.artistId).Take(4).ToArray();
        var actors = others.Append(target).Append(lost).ToArray();
        var originals = actors.Select(a => (act: a, owner: a.labelId, life: a.lifecycleStatus, status: a.prospectMarketStatus)).ToArray();
        string rival = ChartManager.Instance.GetAllLabels().First(l => !l.isPlayerOwned).labelId;
        try {
            clock.RestoreClock(LocalSceneRoomService.Date(bill.Day), slot.StartHour);
            foreach (var actor in actors) { actor.labelId = null; actor.lifecycleStatus = ArtistLifecycleStatus.Active; }
            lost.labelId = rival;
            var fixture = desk.CaptureState(); fixture.ScoutingKnowledge.Dossiers.Clear();
            ScoutingDossier Dossier(SimulatedArtist actor, int day, ScoutingPriority priority = ScoutingPriority.Watch) => new() {
                ArtistId = actor.artistId, ArtistName = actor.stageName, Priority = priority,
                Observations = new() { new() { Kind = ScoutingEvidenceKind.Performance, RoomId = bill.RoomId,
                    RoomName = SceneRoomCatalog.Get(bill.RoomId).Name, OccurredDay = day, AcquiredDay = bill.Day,
                    EventId = "loss-fixture|" + actor.artistId, EndMinute = 1000 } }
            };
            fixture.ScoutingKnowledge.Dossiers.Add(Dossier(lost, bill.Day - 1, ScoutingPriority.Archived));
            fixture.ScoutingKnowledge.Dossiers.Add(Dossier(target, bill.Day - 3, ScoutingPriority.Pursue));
            foreach (var actor in others) fixture.ScoutingKnowledge.Dossiers.Add(Dossier(actor, bill.Day - 1));
            Check(desk.RestoreState(fixture, out _), "controlled known leads restore");
            // Prime the existing bounded calendar before comparing whole-world snapshots.
            desk.ScoutingAlternativeLeads(artistId);
            string player = Json(desk.CaptureState()), world = Json(WorldStateService.Capture());
            GD.Seed(81); uint draw = GD.Randi(); GD.Seed(81);
            var leads = desk.ScoutingAlternativeLeads(artistId);
            Check(leads.Count == 3 && leads[0].ArtistId == target.artistId, "three known leads, pursuit priority first");
            Check(leads.All(l => l.ArtistId != artistId) && leads[0].LastHeardDay == bill.Day - 3,
                "lost act excluded and occurrence date retained rather than receipt date");
            Check(leads[0].NextSet?.EventId == ScenePerformanceService.EventId(bill, slot), "next step references actual unheard appearance");
            desk.ScoutingOtherKnownLeads(artistId); desk.ScoutingAlternativeLeads(artistId);
            Check(Json(desk.CaptureState()) == player && Json(WorldStateService.Capture()) == world, "refresh creates no knowledge, costs or world changes");
            Check(GD.Randi() == draw, "lost-lead previews preserve RNG");
            target.prospectMarketStatus = ProspectMarketStatus.Latent;
            Check(desk.ScoutingAlternativeLeads(artistId)[0].ArtistId == target.artistId && target.prospectMarketStatus == ProspectMarketStatus.Latent,
                "latent act remains a listening lead without activation or consent");
            foreach (var state in new[] { ArtistLifecycleStatus.Inactive, ArtistLifecycleStatus.Retired, ArtistLifecycleStatus.Disbanded }) {
                target.lifecycleStatus = state;
                Check(!desk.ScoutingAlternativeLeads(artistId).Any(l => l.ArtistId == target.artistId), "departed lead excluded: " + state);
            }
            target.lifecycleStatus = ArtistLifecycleStatus.Active; target.labelId = rival;
            Check(!desk.ScoutingAlternativeLeads(artistId).Any(l => l.ArtistId == target.artistId), "signed alternative excluded");
            target.labelId = null; desk.SetScoutingPriority(target.artistId, ScoutingPriority.Archived);
            Check(!desk.ScoutingAlternativeLeads(artistId).Any(l => l.ArtistId == target.artistId), "archived lead stays off attention list");
            desk.SetScoutingPriority(target.artistId, ScoutingPriority.Pursue);
            var claimOnly = desk.CaptureState();
            claimOnly.ScoutingKnowledge.Dossiers.Single(d => d.ArtistId == target.artistId).Observations[0].Kind = ScoutingEvidenceKind.BookerClaim;
            Check(desk.RestoreState(claimOnly, out _) && !desk.ScoutingAlternativeLeads(artistId).Any(l => l.ArtistId == target.artistId),
                "claim does not become a locally heard lead");
            Check(desk.RestoreState(fixture, out _), "reset known performances");
            var preview = desk.ScoutingAlternativeLeads(artistId)[0].NextSet;
            var cancelled = LocalSceneRoomService.Copy(WorldStateService.Capture().SceneRooms);
            cancelled.Bills.Single(b => b.Id == bill.Id).Status = SceneBillStatus.Cancelled;
            LocalSceneRoomService.BeginRestore(cancelled); LocalSceneRoomService.CompleteRestore();
            Check(desk.ScoutingAlternativeLeads(artistId).First(l => l.ArtistId == target.artistId).NextSet?.BillId != bill.Id,
                "cancelled bill removed from next step");
            float cash = desk.Label.cashReserves;
            Check(!desk.ListenSceneAppearance(preview.BillId, preview.EventId, out _) && desk.Label.cashReserves == cash && clock.CurrentHour == slot.StartHour,
                "stale alternative action cannot charge or advance after cancellation");
            foreach (string flag in new[] { "--disable-scouting-experience", "--observe-scouting-experience", "--disable-scene-rooms" }) {
                LocalScenes.Configure(new[] { flag });
                Check(desk.ScoutingAlternativeLeads(artistId).All(l => l.NextSet == null), "disabled encounters retain notes without next actions: " + flag);
            }
            LocalScenes.Configure(Array.Empty<string>());
            foreach (var actor in others) desk.SetScoutingPriority(actor.artistId, ScoutingPriority.Archived);
            desk.SetScoutingPriority(target.artistId, ScoutingPriority.Archived);
            Check(desk.ScoutingAlternativeLeads(artistId).Count == 0 && desk.ScoutingOtherKnownLeads(artistId).Contains("Check announced bills"),
                "empty funnel offers existing discovery route");
            string retained = Json(desk.CaptureState().ScoutingKnowledge);
            var reload = JsonSerializer.Deserialize<PlayerSaveData>(Json(desk.CaptureState()), SaveGameService.TestJsonOptions);
            Check(desk.RestoreState(reload, out _) && Json(desk.CaptureState().ScoutingKnowledge) == retained, "loss preserves archived historical knowledge through reload");
            lost.labelId = desk.Label.labelId;
            Check(desk.ScoutingAlternativeLeads(artistId).Count == 0 && desk.ScoutingOtherKnownLeads(artistId) == null, "own signing is not a lost lead");
            lost.labelId = null;
            Check(desk.ScoutingAlternativeLeads(artistId).Count == 0, "unsigned act is not a lost signing");
        } finally {
            LocalScenes.Configure(Array.Empty<string>());
            foreach (var original in originals) { original.act.labelId = original.owner; original.act.lifecycleStatus = original.life; original.act.prospectMarketStatus = original.status; }
            LocalSceneRoomService.BeginRestore(rooms); LocalSceneRoomService.CompleteRestore();
            desk.RestoreState(saved, out _); clock.RestoreClock(date, hour, minute);
        }
        GD.Print($"SCOUTING_LOSS_CHECK_PASS checks={checks}");
        return checks;
    }
}
