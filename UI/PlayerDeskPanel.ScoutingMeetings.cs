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
    private void OpenScoutingConversation(PlayerDesk desk, ScoutingDossier dossier) {
        var modal = PaperModal.Open(this, dossier.ArtistName + " · CONVERSATION", 750);
        WithinScouting(modal.Body, () => {
            ScoutingText("What would you like to talk about?", 17, Ink, PaperTheme.SansBold);
            var prospect = desk.Slate.FirstOrDefault(p => p.Artist?.artistId == dossier.ArtistId);
            if (prospect != null) ProspectCard(prospect, showEvidence: false, beforeConversationAction: modal.Close);
            ScoutingBusinessConversation(desk, dossier, modal.Close);
            ScoutingMeetingInvitation(desk, dossier, modal);
        });
        modal.AddButton("BACK", null);
    }
    private void ScoutingMeetingInvitation(PlayerDesk desk, ScoutingDossier dossier, PaperModal modal) {
        var choices = new HFlowContainer(); content.AddChild(choices);
        foreach (var activity in new[] { ScoutingAppointmentActivity.Rehearsal, ScoutingAppointmentActivity.Demo, ScoutingAppointmentActivity.Conversation }) {
            var quote = desk.PreviewScoutingInvitation(dossier.ArtistId, activity);
            string label = activity == ScoutingAppointmentActivity.Conversation ? "RETURN MEETING" : activity.ToString().ToUpperInvariant();
            var ask = ScoutingSmallButton("ASK FOR " + label);
            ask.Disabled = quote == null;
            ask.TooltipText = quote == null ? "Talk after a set you've heard today, while a member is available to arrange another visit."
                : $"Propose {LocalSceneRoomService.Date(quote.Day).ToHeadlineString()}, {quote.StartMinute / 60:00}:{quote.StartMinute % 60:00} at " +
                  (activity == ScoutingAppointmentActivity.Demo ? "Home budget studio" : SceneRoomCatalog.Get(quote.RoomId)?.Name) +
                  $". Asking takes 15min, with no fee. The act may decline. The visit takes {(quote.EndMinute - quote.StartMinute) / 60}h and costs ${quote.Fee:0} when you attend." +
                  (activity == ScoutingAppointmentActivity.Conversation ? " A meeting doesn't reserve the act or commit them to a deal." :
                  "\nSong: " + quote.Material.Single().Title + ". Players: " + string.Join(", ", quote.ParticipantIds.Select(id => ArtistManager.Instance.GetArtist(dossier.ArtistId).members.First(m => m.personId == id).FullName)) +
                  ". Cancel before starting for free; time and fees already spent aren't refunded.");
            ask.Pressed += () => { modal.Close(); Act(() => { bool ok = desk.ArrangeScoutingMeeting(dossier.ArtistId, quote.Id, out string result, activity); Say(result, ok); return ok; }); };
            choices.AddChild(ask);
        }
    }

}
