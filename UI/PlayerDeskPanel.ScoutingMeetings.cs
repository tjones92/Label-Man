using System.Linq;
using Godot;

public partial class PlayerDeskPanel {
    private void ScoutingMeetingAgenda(PlayerDesk desk) {
        var pending = desk.ScoutingMeetings.Where(m => m.Status == SceneConversationStatus.Scheduled).OrderBy(m => m.Day).ThenBy(m => m.StartMinute).ToArray();
        if (pending.Length == 0) return;
        Heading("AGREED MEETINGS");
        foreach (var meeting in pending) {
            var clock = TimeManager.Instance; int day = LocalSceneRoomService.Day(clock.CurrentDate), now = clock.CurrentHour * 60 + clock.CurrentMinute;
            bool ready = day == meeting.Day && now == meeting.StartMinute;
            Body($"{meeting.ArtistName} · {meeting.PersonName} · {(meeting.Activity == ScoutingAppointmentActivity.Demo ? "Home budget studio" : SceneRoomCatalog.Get(meeting.RoomId)?.Name)}, " +
                $"{LocalSceneRoomService.Date(meeting.Day).ToHeadlineString()}, {meeting.StartMinute / 60:00}:{meeting.StartMinute % 60:00}–{meeting.EndMinute / 60:00}:{meeting.EndMinute % 60:00}. " +
                $"{meeting.Activity} · {(meeting.EndMinute - meeting.StartMinute) / 60}h · ${meeting.Fee:0} at attendance. Cancel before starting without charge; consumed sessions are not refunded. No hold on the act.");
            if (meeting.Activity != ScoutingAppointmentActivity.Conversation) Body("Agreed material: " + meeting.Material.FirstOrDefault()?.Title + ". Lineup: " +
                string.Join(", ", meeting.ParticipantIds.Select(id => ArtistManager.Instance.GetArtist(meeting.ArtistId)?.members.FirstOrDefault(m => m.personId == id)?.FullName ?? "unavailable participant")) + ".");
            var actions = new HFlowContainer();
            var attend = Btn(ready ? meeting.Activity == ScoutingAppointmentActivity.Conversation ? "ATTEND MEETING (1h · no fee)" : $"ATTEND {meeting.Activity.ToString().ToUpperInvariant()} ({(meeting.EndMinute - meeting.StartMinute) / 60}h · ${meeting.Fee:0})" : "WAIT FOR MEETING");
            attend.Disabled = !LocalScenes.ScoutingExperience || LocalScenes.ScoutingExperienceObserver || !LocalScenes.Rooms ||
                day > meeting.Day || day == meeting.Day && now > meeting.StartMinute;
            attend.TooltipText = "Waiting advances ordinary expenses and other labels' decisions. Attend at the agreed start; current speaker, place, bookings and willingness are checked again. A substantial conversation creates no simultaneous listening evidence.";
            ScoutingTimeRisk(desk, attend, meeting.Day, ready ? meeting.EndMinute : meeting.StartMinute, meeting.Id);
            attend.Pressed += () => Act(() => {
                bool ok = ready ? desk.AttendScoutingMeeting(meeting.Id, out string result) : desk.WaitForScoutingMeeting(meeting.Id, out result);
                if (ready && ok && meeting.Activity == ScoutingAppointmentActivity.Conversation) negotiating = desk.Slate.FirstOrDefault(p => p.Artist.artistId == meeting.ArtistId && p.HasBaseline);
                Say(result, ok); return ok;
            }); actions.AddChild(attend);
            var cancel = Btn("CANCEL MEETING");
            cancel.TooltipText = "Release this appointment without a fee or action time. Cancelling grants no relationship credit or new musical evidence.";
            cancel.Pressed += () => Act(() => { bool ok = desk.CancelScoutingMeeting(meeting.Id, out string result); Say(result, ok); return ok; });
            actions.AddChild(cancel); content.AddChild(actions);
        }
    }
    private void ScoutingMeetingInvitation(PlayerDesk desk, ScoutingDossier dossier) {
        ScoutingActivityInvitation(desk, dossier, ScoutingAppointmentActivity.Rehearsal);
        ScoutingActivityInvitation(desk, dossier, ScoutingAppointmentActivity.Demo);
        var invitation = desk.PreviewScoutingInvitation(dossier.ArtistId);
        if (invitation == null) return;
        Body($"Ask {invitation.PersonName} after the set about returning to {SceneRoomCatalog.Get(invitation.RoomId)?.Name} on " +
            $"{LocalSceneRoomService.Date(invitation.Day).ToHeadlineString()}, {invitation.StartMinute / 60:00}:{invitation.StartMinute % 60:00}, for a record discussion. " +
            "The time is a proposal; they may decline. Scheduling takes 15min, with no fee.");
        var ask = Btn("ASK FOR RETURN MEETING (15min · no fee)");
        ask.TooltipText = "Ask the actual free member you heard today. A consent response books a named speaker and a conversation slot. It does not reserve the act, grant signing consent, improve listening judgments or establish terms.";
        ask.Pressed += () => Act(() => { bool ok = desk.ArrangeScoutingMeeting(dossier.ArtistId, invitation.Id, out string result); Say(result, ok); return ok; });
        content.AddChild(ask);
    }
    private void ScoutingActivityInvitation(PlayerDesk desk, ScoutingDossier dossier, ScoutingAppointmentActivity activity) {
        var quote = desk.PreviewScoutingInvitation(dossier.ArtistId, activity);
        if (quote == null) return;
        Body($"Propose a {activity.ToString().ToLowerInvariant()} on {LocalSceneRoomService.Date(quote.Day).ToHeadlineString()}, {quote.StartMinute / 60:00}:{quote.StartMinute % 60:00}: " +
            $"{(activity == ScoutingAppointmentActivity.Demo ? "Home budget studio" : SceneRoomCatalog.Get(quote.RoomId)?.Name)} · {quote.ParticipantIds.Count} named participant(s), one song from the heard set, {(quote.EndMinute - quote.StartMinute) / 60}h, ${quote.Fee:0} payable at attendance. Cancellation before starting is free; consumed sessions are not refunded.");
        Body("Proposed material: " + quote.Material.Single().Title + ". Lineup: " + string.Join(", ", quote.ParticipantIds.Select(id =>
            ArtistManager.Instance.GetArtist(dossier.ArtistId).members.First(m => m.personId == id).FullName)) + ".");
        var ask = Btn($"ASK FOR {activity.ToString().ToUpperInvariant()} (15min · no invitation fee)");
        ask.TooltipText = "The existing bandleader agrees for the named lineup; every participant must remain available. No substitution, master, release, audience evidence or promise to sign. A demo uses the home budget studio console.";
        ask.Pressed += () => Act(() => { bool ok = desk.ArrangeScoutingMeeting(dossier.ArtistId, quote.Id, out string result, activity); Say(result, ok); return ok; });
        content.AddChild(ask);
    }

}
