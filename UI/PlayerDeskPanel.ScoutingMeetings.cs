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
                $"{meeting.Activity} · {(meeting.EndMinute - meeting.StartMinute) / 60}h · ${meeting.Fee:0} when you go. Cancelling before it starts costs nothing. It doesn't stop another label talking to them.");
            if (meeting.Activity != ScoutingAppointmentActivity.Conversation) Body("Agreed material: " + meeting.Material.FirstOrDefault()?.Title + ". Lineup: " +
                string.Join(", ", meeting.ParticipantIds.Select(id => ArtistManager.Instance.GetArtist(meeting.ArtistId)?.members.FirstOrDefault(m => m.personId == id)?.FullName ?? "unavailable participant")) + ".");
            var actions = new HFlowContainer();
            var attend = Btn(ready ? meeting.Activity == ScoutingAppointmentActivity.Conversation ? "ATTEND MEETING (1h · no fee)" : $"ATTEND {meeting.Activity.ToString().ToUpperInvariant()} ({(meeting.EndMinute - meeting.StartMinute) / 60}h · ${meeting.Fee:0})" : "WAIT FOR MEETING");
            attend.Disabled = !LocalScenes.ScoutingExperience || LocalScenes.ScoutingExperienceObserver || !LocalScenes.Rooms ||
                day > meeting.Day || day == meeting.Day && now > meeting.StartMinute;
            attend.TooltipText = "The month's bills keep running while you wait, and other labels keep working. Be there at the agreed start. They may have changed their minds by then.";
            GreyWhy(attend, "The agreed start has passed.");
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

}
