using System;
using System.Linq;
using System.Text.Json;
using Godot;

public static class ScoutingAppointmentChecks {
    public static void Run() {
        int checks = 0;
        void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("SCOUTING_APPOINTMENT_FAILED: " + why); checks++; }
        string Json(object value) => JsonSerializer.Serialize(value, SaveGameService.TestJsonOptions);
        var desk = PlayerDesk.Instance; var clock = TimeManager.Instance; var am = ArtistManager.Instance;
        Check(desk.FoundLabel("Meeting Checks", "nashville", out string message), "label: " + message);
        bool Feasible(SceneBill candidate) {
            if (candidate.Status != SceneBillStatus.Scheduled || candidate.Appearances.Count == 0) return false;
            var appearance = candidate.Appearances[0]; var room = SceneRoomCatalog.Get(candidate.RoomId);
            if (appearance.Set.Count == 0) return false;
            if (appearance.EndHour >= Math.Min(clock.HardStopHour, room.CloseHour)) return false;
            clock.RestoreClock(LocalSceneRoomService.Date(candidate.Day), appearance.EndHour);
            var actCandidate = am.GetArtist(appearance.ArtistId);
            var leader = actCandidate.members.FirstOrDefault(m => m.isActive && m.lifeState == MemberLifeState.Active &&
                (m.isBandLeader || actCandidate.members.Count == 1) && appearance.PersonIds.Contains(m.personId) &&
                !LocalSceneRoomService.HasPersonBooking(m.personId, candidate.Day, appearance.EndHour * 60, appearance.EndHour * 60 + 15, room.Id));
            if (leader == null) return false;
            string eventId = ScenePerformanceService.EventId(candidate, appearance);
            return SceneRoomCatalog.ForPlace(desk.CurrentCityId).Any(r => LocalSceneRoomService.QuoteConversation(desk.Label, appearance.ArtistId, leader.personId, r.Id, eventId, ScoutingAppointmentActivity.Rehearsal) != null) &&
                LocalSceneRoomService.QuoteConversation(desk.Label, appearance.ArtistId, leader.personId, room.Id, eventId, ScoutingAppointmentActivity.Demo) != null;
        }
        var bill = LocalSceneRoomService.Calendar(desk.CurrentCityId, clock.CurrentDate).First(Feasible);
        var slot = bill.Appearances[0]; string artistId = slot.ArtistId;
        clock.RestoreClock(LocalSceneRoomService.Date(bill.Day), slot.StartHour);
        Check(desk.ListenSceneAppearance(bill.Id, ScenePerformanceService.EventId(bill, slot), out message), "actual first listening: " + message);
        var act = am.GetArtist(artistId);
        act.labelId = null; act.careerState = CareerState.Unsigned; act.lifecycleStatus = ArtistLifecycleStatus.Active;
        act.prospectMarketStatus = ProspectMarketStatus.Seeking; act.manager = ManagerArchetype.None;
        desk.Label.cashReserves = 100000;
        var baseWorld = WorldStateService.Capture(); var basePlayer = desk.CaptureState();
        var baseDate = clock.CurrentDate; int baseHour = clock.CurrentHour, baseMinute = clock.CurrentMinute;
        void Reset() {
            LocalScenes.Configure(Array.Empty<string>());
            WorldStateService.Apply(LocalSceneRoomService.Copy(baseWorld), baseDate, SimulationSeedBootstrap.RequestedSeed);
            if (!desk.RestoreState(LocalSceneRoomService.Copy(basePlayer), out message)) throw new InvalidOperationException(message);
            clock.RestoreClock(baseDate, baseHour, baseMinute);
        }
        foreach (var activity in new[] { ScoutingAppointmentActivity.Rehearsal, ScoutingAppointmentActivity.Demo }) {
            Reset();
            var quote = desk.PreviewScoutingInvitation(artistId, activity);
            Check(quote != null, "feasible activity quote: " + activity);
            string worldBefore = Json(WorldStateService.Capture());
            GD.Seed(432); uint next = GD.Randi(); GD.Seed(432);
            desk.PreviewScoutingInvitation(artistId, activity); desk.ScoutingSummary(artistId);
            Check(Json(WorldStateService.Capture()) == worldBefore && GD.Randi() == next, "pure activity preview");
            Check(!desk.ArrangeScoutingMeeting(artistId, "stale", out _, activity), "stale quote rejected");
            float cash = desk.Label.cashReserves, expenses = desk.Label.monthlyExpenses;
            Check(desk.ArrangeScoutingMeeting(artistId, quote.Id, out message, activity), "consent: " + message);
            var booked = desk.ScoutingMeetings.Single();
            Check(booked.Activity == activity && booked.ParticipantIds.Count == am.GetArtist(artistId).members.Count(m => m.isActive && m.lifeState == MemberLifeState.Active), "full named lineup");
            Check(booked.Material.Count == 1 && desk.Label.cashReserves == cash && !booked.Paid, "one agreed song and no booking charge");
            Check(booked.ParticipantIds.All(id => LocalSceneRoomService.HasPersonBooking(id, booked.Day, booked.StartMinute, booked.EndMinute, booked.RoomId)), "every participant reserved");
            Check(!desk.ArrangeScoutingMeeting(artistId, quote.Id, out _, activity), "no duplicate consent");
            var invalidRooms = LocalSceneRoomService.Copy(WorldStateService.Capture().SceneRooms);
            invalidRooms.Conversations.Single().ParticipantIds.Add(invalidRooms.Conversations.Single().PersonId);
            bool rejected = false; try { LocalSceneRoomService.ValidateRestore(invalidRooms); } catch (InvalidOperationException) { rejected = true; }
            Check(rejected, "duplicate participants rejected before restore");
            var saved = WorldStateService.Capture(); var player = desk.CaptureState();
            LocalSceneRoomService.ValidateRestore(saved.SceneRooms);
            WorldStateService.Apply(LocalSceneRoomService.Copy(saved), clock.CurrentDate, SimulationSeedBootstrap.RequestedSeed);
            Check(desk.RestoreState(LocalSceneRoomService.Copy(player), out message) && Json(WorldStateService.Capture().SceneRooms) == Json(saved.SceneRooms), "appointment restore");
            string saveSlot = "Unsigned probe " + Guid.NewGuid().ToString("N");
            try {
                Check(SaveGameService.Save(saveSlot, out message) && SaveGameService.Load(saveSlot, out message) &&
                    desk.ScoutingMeetings.Single().Activity == activity && desk.ScoutingMeetings.Single().ParticipantIds.SequenceEqual(booked.ParticipantIds), "compressed save/load preserves consent and participants");
            } finally { SaveGameService.Delete(saveSlot); }
            Check(desk.CancelScoutingMeeting(booked.Id, out message) && desk.Label.cashReserves == cash, "free cancellation");
            Check(booked.ParticipantIds.All(id => !LocalSceneRoomService.HasPersonBooking(id, booked.Day, booked.StartMinute, booked.EndMinute, booked.RoomId)), "all reservations released");
            Reset(); quote = desk.PreviewScoutingInvitation(artistId, activity);
            Check(desk.ArrangeScoutingMeeting(artistId, quote.Id, out message, activity), "rebook fixture");
            booked = desk.ScoutingMeetings.Single();
            clock.RestoreClock(LocalSceneRoomService.Date(booked.Day), booked.StartMinute / 60, booked.StartMinute % 60);
            if (activity == ScoutingAppointmentActivity.Demo) {
                desk.Label.cashReserves = 0;
                Check(!desk.AttendScoutingMeeting(booked.Id, out message) && desk.ScoutingMeetings.Single().Status == SceneConversationStatus.Scheduled && clock.CurrentHour * 60 + clock.CurrentMinute == booked.StartMinute, "insufficient cash costs no time");
                desk.Label.cashReserves = cash;
            }
            Check(desk.AttendScoutingMeeting(booked.Id, out message), "attend: " + message);
            var completed = desk.ScoutingMeetings.Single();
            Check(completed.Status == SceneConversationStatus.Completed && completed.Sample != null && completed.Paid == (activity == ScoutingAppointmentActivity.Demo), "completion snapshot and payment");
            Check(clock.CurrentHour * 60 + clock.CurrentMinute == booked.EndMinute && desk.Label.cashReserves == cash - booked.Fee && desk.Label.monthlyExpenses == expenses + booked.Fee, "exact duration and accounting");
            var evidence = desk.ScoutingDossiers.Single().Observations;
            Check(evidence.Count(o => o.Kind is ScoutingEvidenceKind.RehearsalSample or ScoutingEvidenceKind.DemoSample) == 1 && evidence.Count(o => o.Kind == ScoutingEvidenceKind.Performance) == 1, "bounded sample separate from live evidence");
            Check(desk.Session == null && am.GetArtist(artistId).labelId == null && am.GetArtist(artistId).unrecoupedAdvance == 0, "no master, signing or artist debt");
            int count = evidence.Count;
            Check(!desk.AttendScoutingMeeting(booked.Id, out _) && desk.ScoutingDossiers.Single().Observations.Count == count && desk.Label.cashReserves == cash - booked.Fee, "no farming or double payment");
            Reset(); quote = desk.PreviewScoutingInvitation(artistId, activity);
            Check(desk.ArrangeScoutingMeeting(artistId, quote.Id, out message, activity), "lineup change setup"); booked = desk.ScoutingMeetings.Single();
            am.GetArtist(artistId).members.First(m => m.personId == booked.PersonId).isActive = false;
            clock.RestoreClock(LocalSceneRoomService.Date(booked.Day), booked.StartMinute / 60, booked.StartMinute % 60);
            Check(!desk.AttendScoutingMeeting(booked.Id, out _) && desk.ScoutingMeetings.Single().Status == SceneConversationStatus.Cancelled && desk.Label.cashReserves == cash, "changed participant cancels without charge");
            Reset(); quote = desk.PreviewScoutingInvitation(artistId, activity);
            Check(desk.ArrangeScoutingMeeting(artistId, quote.Id, out message, activity), "rival setup"); booked = desk.ScoutingMeetings.Single();
            am.GetArtist(artistId).labelId = "external-label";
            clock.RestoreClock(LocalSceneRoomService.Date(booked.Day), booked.StartMinute / 60, booked.StartMinute % 60);
            Check(!desk.AttendScoutingMeeting(booked.Id, out _) && desk.Label.cashReserves == cash, "external ownership blocks attendance");
            Reset(); quote = desk.PreviewScoutingInvitation(artistId, activity);
            Check(desk.ArrangeScoutingMeeting(artistId, quote.Id, out message, activity), "in-hour setup"); booked = desk.ScoutingMeetings.Single();
            clock.RestoreClock(LocalSceneRoomService.Date(booked.Day), booked.StartMinute / 60, booked.StartMinute % 60);
            void SignDuringHour(int hour) { am.GetArtist(artistId).labelId = "external-label"; }
            clock.OnHourChanged += SignDuringHour;
            try {
                Check(!desk.AttendScoutingMeeting(booked.Id, out _) && desk.Label.cashReserves == cash - booked.Fee &&
                    desk.ScoutingMeetings.Single().Status == SceneConversationStatus.Cancelled &&
                    desk.ScoutingDossiers.Single().Observations.All(o => o.Kind is not ScoutingEvidenceKind.DemoSample and not ScoutingEvidenceKind.RehearsalSample), "consumed cancelled session retains cost and creates no stale evidence");
            } finally { clock.OnHourChanged -= SignDuringHour; }
            Reset(); quote = desk.PreviewScoutingInvitation(artistId, activity);
            Check(desk.ArrangeScoutingMeeting(artistId, quote.Id, out message, activity), "off cancellation setup"); booked = desk.ScoutingMeetings.Single();
            LocalScenes.Configure(new[] { "--disable-scouting-experience" });
            Check(desk.PreviewScoutingInvitation(artistId, activity) == null && desk.CancelScoutingMeeting(booked.Id, out _), "off retains commitment and permits cancellation");
        }
        Reset();
        GD.Print($"SCOUTING_APPOINTMENT_OK checks={checks}");
    }
}
