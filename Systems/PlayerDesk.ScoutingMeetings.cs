using System;
using System.Collections.Generic;
using System.Linq;

public partial class PlayerDesk {
    public const int ScoutingInvitationMinutes = 15;
    private bool MeetingActionsEnabled => Label != null && LocalScenes.Rooms && LocalScenes.ScoutingExperience && !LocalScenes.ScoutingExperienceObserver;
    public System.Collections.Generic.IReadOnlyList<SceneConversation> ScoutingMeetings => LocalSceneRoomService.Conversations(Label?.labelId);
    private bool ScoutingDiscussionCoolingDown(string artistId) {
        var p = scoutingNegotiations.GetValueOrDefault(artistId) ?? slate.FirstOrDefault(p => p.Artist?.artistId == artistId);
        return p?.CooldownUntil is { } until && TimeManager.Instance.CurrentDate < until;
    }
    private (ScoutingObservation heard, Musician person) InvitationSpeaker(string artistId) {
        var heard = scoutingKnowledge.Dossiers.FirstOrDefault(d => d.ArtistId == artistId)?.Observations
            .LastOrDefault(o => o.Kind == ScoutingEvidenceKind.Performance);
        var act = ArtistManager.Instance.GetArtist(artistId); var room = SceneRoomCatalog.Get(heard?.RoomId); var clock = TimeManager.Instance;
        int day = LocalSceneRoomService.Day(clock.CurrentDate), now = clock.CurrentHour * 60 + clock.CurrentMinute;
        if (heard == null || room == null || act?.lifecycleStatus != ArtistLifecycleStatus.Active || heard.OccurredDay != day ||
            heard.EndMinute > now || now < room.OpenHour * 60 || now + ScoutingInvitationMinutes > room.CloseHour * 60 ||
            room.PlaceId != LocalScenePersistenceService.SceneIdFor(CurrentCityId) || !clock.CanAffordMinutes(ScoutingInvitationMinutes) ||
            !LocalScenePersistenceService.Present(act, room.PlaceId, day / 7) || !SceneEcosystemService.AvailableAt(artistId, room.PlaceId, day)) return default;
        var person = act.members.Where(m => m.isActive && m.lifeState == MemberLifeState.Active && heard.PersonIds.Contains(m.personId))
            .OrderByDescending(m => m.isBandLeader).ThenByDescending(m => m.isLeadVocalist).ThenBy(m => m.personId, StringComparer.Ordinal)
            .FirstOrDefault(m => !LocalSceneRoomService.HasPersonBooking(m.personId, day, now, now + ScoutingInvitationMinutes, room.Id));
        return (heard, person);
    }
    public SceneConversationQuote PreviewScoutingInvitation(string artistId, ScoutingAppointmentActivity activity = ScoutingAppointmentActivity.Conversation) {
        if (!MeetingActionsEnabled) return null;
        var source = InvitationSpeaker(artistId);
        var quote = source.person == null ? null : LocalSceneRoomService.QuoteConversation(Label, artistId, source.person.personId, source.heard.RoomId, source.heard.EventId, activity);
        if (quote == null && activity == ScoutingAppointmentActivity.Rehearsal && source.person != null) {
            foreach (var room in SceneRoomCatalog.ForPlace(CurrentCityId).OrderBy(r => r.Id, StringComparer.Ordinal)) {
                quote = LocalSceneRoomService.QuoteConversation(Label, artistId, source.person.personId, room.Id, source.heard.EventId, activity);
                if (quote != null) break;
            }
        }
        if (quote != null && activity != ScoutingAppointmentActivity.Conversation) {
            quote.Material = LocalSceneRoomService.Copy(source.heard.Set.Take(1).ToList());
            if (quote.Material.Count != 1) return null;
        }
        return quote;
    }
    public bool ArrangeScoutingMeeting(string artistId, string quoteId, out string message, ScoutingAppointmentActivity activity = ScoutingAppointmentActivity.Conversation) {
        if (!MeetingActionsEnabled || !Require(0, out message)) { message = "New scouting meetings are unavailable; existing appointment history is retained."; return false; }
        var quote = PreviewScoutingInvitation(artistId, activity);
        if (quote == null || quote.Id != quoteId) { message = "A free member after a set you heard today and a current proposed slot are required. Review the invitation."; return false; }
        string invitationRoomId = InvitationSpeaker(artistId).heard?.RoomId;
        var clock = TimeManager.Instance; int now = clock.CurrentHour * 60 + clock.CurrentMinute;
        if (!LocalSceneRoomService.RequestConversation(Label, quote, ScoutingDiscussionCoolingDown(artistId), out var reply, out message)) return false;
        bool continuous = scoutingKnowledge.Visit?.Day == LocalSceneRoomService.Day(clock.CurrentDate) && scoutingKnowledge.Visit.EndMinute == now &&
            LocalSceneRoomService.Bill(scoutingKnowledge.Visit.BillId)?.RoomId == invitationRoomId;
        // A short scheduling exchange creates no simultaneous performance evidence. It may
        // remain part of the same paid visit, unlike the later substantial meeting.
        SpendMinutes(ScoutingInvitationMinutes);
        if (continuous) scoutingKnowledge.Visit.EndMinute = clock.CurrentHour * 60 + clock.CurrentMinute;
        else scoutingKnowledge.Visit = null;
        RememberScoutingMeeting(reply, "invitation", reply.Response, reply.PersonId, reply.PersonName, "post-set invitation");
        Note(message); Changed?.Invoke(); return true; // A genuine declined invitation is also a completed conversation.
    }
    private SceneConversation OwnScoutingMeeting(string id) {
        var meeting = LocalSceneRoomService.Conversation(id);
        return meeting?.LabelId == Label?.labelId ? meeting : null;
    }
    private bool MeetingPlace(SceneConversation meeting, out string message) {
        if (SceneRoomCatalog.Get(meeting.RoomId)?.PlaceId != LocalScenePersistenceService.SceneIdFor(CurrentCityId)) {
            message = "Travel to the agreed city before waiting for or attending this meeting."; return false;
        }
        message = ""; return true;
    }
    private void ObserveScoutingMeetingNotice(SceneConversation meeting) {
        if (meeting == null || meeting.Status is SceneConversationStatus.Scheduled or SceneConversationStatus.InProgress || meeting.Status == SceneConversationStatus.Completed && meeting.Activity != ScoutingAppointmentActivity.Conversation) return;
        var room = SceneRoomCatalog.Get(meeting.RoomId);
        RememberScoutingMeeting(meeting, "notice|" + meeting.Status, meeting.Response, room?.ContactId, room?.ContactName, "appointment notice");
    }
    public bool WaitForScoutingMeeting(string id, out string message) {
        message = "This scouting appointment is unavailable.";
        if (!MeetingActionsEnabled || !Require(0, out message)) return false;
        LocalSceneRoomService.RefreshConversationsForClock();
        var meeting = OwnScoutingMeeting(id); var clock = TimeManager.Instance;
        if (meeting?.Status != SceneConversationStatus.Scheduled) { ObserveScoutingMeetingNotice(meeting); message = meeting?.Response ?? message; Changed?.Invoke(); return false; }
        if (!MeetingPlace(meeting, out message)) return false;
        int day = LocalSceneRoomService.Day(clock.CurrentDate), now = clock.CurrentHour * 60 + clock.CurrentMinute;
        if (day > meeting.Day || day == meeting.Day && now > meeting.StartMinute) { message = "The agreed start has passed. This appointment cannot be reset by reopening it."; return false; }
        while (LocalSceneRoomService.Day(clock.CurrentDate) < meeting.Day && !clock.IsGameOver && !IsGameOver) {
            clock.EndDay();
            LocalSceneRoomService.RefreshConversationsForClock();
            meeting = OwnScoutingMeeting(id);
            if (meeting?.Status != SceneConversationStatus.Scheduled) break;
        }
        if (clock.IsGameOver || IsGameOver) { message = "Waiting ended with the label's operating window."; return false; }
        if (meeting?.Status != SceneConversationStatus.Scheduled) {
            ObserveScoutingMeetingNotice(meeting); message = meeting?.Response ?? "The meeting changed while time passed."; Note(message); Changed?.Invoke(); return false;
        }
        if (!MeetingPlace(meeting, out message)) return false;
        int minutes = meeting.StartMinute - (clock.CurrentHour * 60 + clock.CurrentMinute);
        if (minutes > 0 && !clock.SpendMinutes(minutes)) { message = "The meeting cannot be reached within today's time budget."; return false; }
        scoutingKnowledge.Visit = null;
        meeting = OwnScoutingMeeting(id);
        if (meeting?.Status != SceneConversationStatus.Scheduled) { ObserveScoutingMeetingNotice(meeting); message = meeting?.Response ?? "The meeting changed."; Note(message); Changed?.Invoke(); return false; }
        message = "Waited until the agreed start. Attend to hold the hour-long conversation; offers retain their ordinary costs. Waiting gave no hold on the act.";
        Note(message); Changed?.Invoke(); return true;
    }
    public bool AttendScoutingMeeting(string id, out string message) {
        message = "This scouting appointment is unavailable.";
        if (!MeetingActionsEnabled || !Require(0, out message)) return false;
        var meeting = OwnScoutingMeeting(id);
        if (meeting == null) return false;
        if (!MeetingPlace(meeting, out message)) return false;
        if (ScoutingDiscussionCoolingDown(meeting.ArtistId)) {
            LocalSceneRoomService.CancelConversation(id, Label.labelId, out message, "Their side is not ready for another record discussion yet.");
            ObserveScoutingMeetingNotice(OwnScoutingMeeting(id)); Changed?.Invoke(); return false;
        }
        if (!TimeManager.Instance.CanAffordMinutes(meeting.EndMinute - meeting.StartMinute)) { message = "The full appointment will not fit today."; return false; }
        if (meeting.Activity == ScoutingAppointmentActivity.Demo && (!RequireHome(out message) || pendingSession != null)) { message = "The home studio console must be free."; return false; }
        if (Label.cashReserves < meeting.Fee) { message = $"Attendance requires ${meeting.Fee:0}; cancellation before starting is free."; return false; }
        if (!LocalSceneRoomService.BeginConversation(id, Label.labelId, out message)) { ObserveScoutingMeetingNotice(OwnScoutingMeeting(id)); Changed?.Invoke(); return false; }
        scoutingKnowledge.Visit = null;
        if (meeting.Fee > 0) {
            Label.cashReserves -= meeting.Fee; Label.monthlyExpenses += meeting.Fee;
            LocalSceneRoomService.MarkAppointmentPaid(id, Label.labelId);
        }
        if (!SpendMinutes(meeting.EndMinute - meeting.StartMinute)) {
            LocalSceneRoomService.CancelConversation(id, Label.labelId, out message, "The full meeting hour could not be completed.");
            ObserveScoutingMeetingNotice(OwnScoutingMeeting(id)); Changed?.Invoke(); return false;
        }
        if (!LocalSceneRoomService.FinishConversation(id, Label.labelId, out message)) { ObserveScoutingMeetingNotice(OwnScoutingMeeting(id)); Note(message); Changed?.Invoke(); return false; }
        meeting = OwnScoutingMeeting(id);
        if (meeting.Activity != ScoutingAppointmentActivity.Conversation) {
            var dossier = scoutingKnowledge.Dossiers.First(d => d.ArtistId == meeting.ArtistId);
            var interpretation = ScenePerformanceService.Interpret(meeting.Sample, Label.labelId, Label.scoutingAbility, 1);
            dossier.Observations.Add(new() { Id = "session-evidence-v1|" + meeting.Id, EventId = meeting.Id, MeetingId = meeting.Id,
                LabelId = Label.labelId, ArtistId = meeting.ArtistId, ArtistName = meeting.ArtistName,
                Kind = meeting.Activity == ScoutingAppointmentActivity.Demo ? ScoutingEvidenceKind.DemoSample : ScoutingEvidenceKind.RehearsalSample,
                RoomId = meeting.RoomId, RoomName = meeting.Activity == ScoutingAppointmentActivity.Demo ? "Home budget studio" : SceneRoomCatalog.Get(meeting.RoomId)?.Name, PersonIds = meeting.ParticipantIds,
                Set = meeting.Material, OccurredDay = meeting.Day, AcquiredDay = meeting.Day, AcquiredMinute = meeting.EndMinute,
                StartMinute = meeting.StartMinute, EndMinute = meeting.EndMinute, Coverage = 1, Interpretation = interpretation,
                SourceId = meeting.PersonId, SourceName = meeting.PersonName, ConsultationFee = meeting.Fee,
                Conditions = $"{meeting.Activity}: one agreed song with the named lineup; {(meeting.Activity == ScoutingAppointmentActivity.Demo ? "budget home studio" : "private room rehearsal")}. No audience or release evidence.",
                Credibility = "bounded controlled sample; no future ceiling or signing commitment", Claim = "Ensemble in this sample: " + AxisDescription(interpretation.Execution).Replace("set", "sample") + ". " + (interpretation.Vocals.HasValue ? "Vocals: " + AxisDescription(interpretation.Vocals.Value).Replace("set", "sample") + "." : "No lead-vocal evidence.") });
            message = $"Completed the {meeting.Activity.ToString().ToLowerInvariant()}. One controlled sample retained; no master or deal created.";
            Note(message); Changed?.Invoke(); return true;
        }
        RememberScoutingMeeting(meeting, "attended", "Met for the agreed hour to discuss a record deal. Meeting consent is not signing consent.",
            meeting.PersonId, meeting.PersonName, "scheduled conversation");
        if (!OpenScoutingDossier(meeting.ArtistId, out message)) { Note(message); Changed?.Invoke(); return false; }
        var prospect = slate.First(p => p.Artist.artistId == meeting.ArtistId);
        bool opened = ApproachToSign(prospect, out message); // Existing asks, cooldown, roster, identity and eligibility owner.
        Note(message); Changed?.Invoke(); return opened;
    }
    public bool CancelScoutingMeeting(string id, out string message) {
        if (Label == null || OwnScoutingMeeting(id) == null) { message = "This is not your label's appointment."; return false; }
        if (!LocalSceneRoomService.CancelConversation(id, Label.labelId, out message)) return false;
        RememberScoutingMeeting(OwnScoutingMeeting(id), "cancelled", message, Label.labelId, "Your label", "appointment cancellation");
        Note(message); Changed?.Invoke(); return true;
    }
    private void RememberScoutingMeeting(SceneConversation meeting, string occurrence, string claim, string sourceId, string sourceName, string channel) {
        if (!MeetingActionsEnabled || meeting == null) return;
        var dossier = scoutingKnowledge.Dossiers.FirstOrDefault(d => d.ArtistId == meeting.ArtistId);
        if (dossier == null) return;
        string eventId = meeting.Id + "|" + occurrence;
        if (dossier.Observations.Any(o => o.EventId == eventId)) return;
        var clock = TimeManager.Instance; int day = LocalSceneRoomService.Day(clock.CurrentDate); var room = SceneRoomCatalog.Get(meeting.RoomId);
        dossier.Observations.Add(new() { Id = $"meeting-statement-v1|{Label.labelId}|{eventId}", EventId = eventId, MeetingId = meeting.Id,
            LabelId = Label.labelId, ArtistId = meeting.ArtistId, ArtistName = meeting.ArtistName, Kind = ScoutingEvidenceKind.MeetingStatement,
            RoomId = meeting.RoomId, RoomName = room?.Name, PlaceId = room?.PlaceId, SourceId = sourceId, SourceName = sourceName, Channel = channel,
            OccurredDay = occurrence == "invitation" ? meeting.AgreedDay : day, AcquiredDay = day, AcquiredMinute = clock.CurrentHour * 60 + clock.CurrentMinute,
            Credibility = "actual appointment statement for this label; no new listening, contractual hold or promise to sign", Claim = claim });
    }
    public string ScoutingMeetingSummary(string artistId) {
        var last = ScoutingMeetings.LastOrDefault(c => c.ArtistId == artistId);
        if (last == null) return null;
        return $"{last.Activity} with {last.PersonName}: {LocalSceneRoomService.Date(last.Day).ToHeadlineString()}, {last.StartMinute / 60:00}:{last.StartMinute % 60:00} at " +
            $"{(last.Activity == ScoutingAppointmentActivity.Demo ? "Home budget studio" : SceneRoomCatalog.Get(last.RoomId)?.Name)}. {last.Status}. " + (last.Status == SceneConversationStatus.Scheduled ?
                $"Agreed {last.Activity.ToString().ToLowerInvariant()} only; availability will be checked again. Fee ${last.Fee:0} at attendance; no hold or terms." : last.Response);
    }
}
