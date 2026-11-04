using System;
using System.Linq;
using System.Text.Json;
using Godot;

public static class ScoutingCommitmentPlanningChecks {
    public static void Run() {
        int checks = 0;
        void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("SCOUTING_COMMITMENT_PLANNING_FAILED: " + why); checks++; }
        string Json(object value) => JsonSerializer.Serialize(value, SaveGameService.TestJsonOptions);
        var desk = PlayerDesk.Instance; var clock = TimeManager.Instance;
        Check(desk.FoundLabel("Commitment Planning", "nashville", out string message), "found label: " + message);
        var plan = desk.ScoutingItinerary().First(o => o.EndMinute < Math.Min(clock.HardStopHour * 60, SceneRoomCatalog.Get(o.RoomId).CloseHour * 60));
        Check(desk.WaitForSceneAppearance(plan.BillId, plan.EventId, out message) && desk.ListenSceneAppearance(plan.BillId, plan.EventId, out message), "actual heard set: " + message);
        var act = ArtistManager.Instance.GetArtist(plan.ArtistId);
        act.labelId = null; act.careerState = CareerState.Unsigned; act.lifecycleStatus = ArtistLifecycleStatus.Active; act.prospectMarketStatus = ProspectMarketStatus.Seeking; act.manager = ManagerArchetype.None;
        var quote = desk.PreviewScoutingInvitation(act.artistId);
        Check(quote != null && desk.ArrangeScoutingMeeting(act.artistId, quote.Id, out message), "actual consent: " + message);
        var meeting = desk.ScoutingMeetings.Single();
        Check(meeting.Status == SceneConversationStatus.Scheduled, "scheduled appointment");
        Check(desk.ScoutingAppointmentRisks(meeting.Day, meeting.StartMinute).Count == 0, "exact start reachable without missing it");
        Check(desk.ScoutingAppointmentRisks(meeting.Day, meeting.StartMinute + 1).Single().Id == meeting.Id, "one minute beyond start shows the agreed appointment");
        Check(desk.ScoutingAppointmentRisks(meeting.Day, meeting.EndMinute, meeting.Id).Count == 0, "attendance preview excludes the appointment being attended");
        Check(desk.ScoutingAppointmentRisks(meeting.Day - 1, 1440).Count == 0 && desk.ScoutingAppointmentRisks(meeting.Day, -1).Count == 0, "valid forward interval only");
        string warning = desk.ScoutingAppointmentRiskText(meeting.Day, meeting.StartMinute + 1);
        Check(warning.Contains(meeting.ArtistName) && warning.Contains(SceneRoomCatalog.Get(meeting.RoomId).Name) && warning.Contains("continue without attending"), "warning identifies commitment and preserves choice");
        desk.SetScoutingPriority(act.artistId, ScoutingPriority.Archived);
        Check(desk.ScoutingAppointmentRisks(meeting.Day, meeting.StartMinute + 1).Count == 1, "archiving does not hide an agreed obligation");
        string world = Json(WorldStateService.Capture()), player = Json(desk.CaptureState());
        GameRandom.Seed(712); uint draw = GameRandom.Randi(); GameRandom.Seed(712);
        desk.ScoutingAppointmentRisks(meeting.Day, meeting.EndMinute); desk.ScoutingAppointmentRiskText(meeting.Day, meeting.EndMinute); desk.ScoutingItinerary();
        Check(Json(WorldStateService.Capture()) == world && Json(desk.CaptureState()) == player && GameRandom.Randi() == draw, "risk previews preserve complete world, player and RNG");
        var savedPlayer = JsonSerializer.Deserialize<PlayerSaveData>(player, SaveGameService.TestJsonOptions);
        Check(desk.RestoreState(savedPlayer, out message) && desk.ScoutingAppointmentRiskText(meeting.Day, meeting.StartMinute + 1) == warning, "player restore preserves warning");
        string slot = "Commitment " + Guid.NewGuid().ToString("N");
        try {
            Check(SaveGameService.Save(slot, out message), "compressed save: " + message);
            Check(SaveGameService.Load(slot, out message) && desk.ScoutingAppointmentRiskText(meeting.Day, meeting.StartMinute + 1) == warning, "full load preserves commitment and projection: " + message);
        } finally { SaveGameService.Delete(slot); }
        var baseWorld = WorldStateService.Capture(); var basePlayer = desk.CaptureState(); var date = clock.CurrentDate; int hour = clock.CurrentHour, minute = clock.CurrentMinute;
        float cash = desk.Label.cashReserves;
        Check(desk.CancelScoutingMeeting(meeting.Id, out message) && desk.ScoutingAppointmentRiskText(meeting.Day, meeting.EndMinute) == null && desk.Label.cashReserves == cash && clock.CurrentHour == hour && clock.CurrentMinute == minute, "free cancellation releases warning without resources");
        WorldStateService.Apply(LocalSceneRoomService.Copy(baseWorld), date, SimulationSeedBootstrap.RequestedSeed); desk.RestoreState(basePlayer, out _); clock.RestoreClock(date, hour, minute);
        clock.RestoreClock(LocalSceneRoomService.Date(meeting.Day), meeting.StartMinute / 60, meeting.StartMinute % 60);
        var later = desk.ScoutingItinerary().First(o => o.Day > meeting.Day || o.Day == meeting.Day && o.StartMinute > meeting.StartMinute);
        Check(desk.ScoutingAppointmentRisks(later.Day, later.StartMinute).Any(m => m.Id == meeting.Id), "real later set exposes the skipped commitment");
        int evidence = desk.ScoutingDossiers.Sum(d => d.Observations.Count);
        Check(desk.WaitForSceneAppearance(later.BillId, later.EventId, out message), "player may still choose the later set: " + message);
        Check(desk.ScoutingMeetings.Single().Status == SceneConversationStatus.Missed && desk.ScoutingDossiers.Sum(d => d.Observations.Count) == evidence, "ordinary clock misses appointment without fabricated attendance or evidence");
        Check(desk.ScoutingAppointmentRiskText(later.Day, later.EndMinute) == null, "missed commitment no longer presented as pending");
        GD.Print($"SCOUTING_COMMITMENT_PLANNING_PASS checks={checks}");
    }
}
