using System;
using System.Collections.Generic;
using System.Linq;

public partial class PlayerDesk {
    // This is a detached projection, not a refresh of appointment status or consent.
    // Reaching a start exactly is safe; passing it without attendance can miss it.
    public IReadOnlyList<SceneConversation> ScoutingAppointmentRisks(int throughDay, int throughMinute, string exceptId = null) {
        var clock = TimeManager.Instance;
        if (Label == null || clock == null || throughMinute < 0 || throughMinute > 1440) return Array.Empty<SceneConversation>();
        long now = (long)LocalSceneRoomService.Day(clock.CurrentDate) * 1440 + clock.CurrentHour * 60 + clock.CurrentMinute;
        long through = (long)throughDay * 1440 + throughMinute;
        if (through <= now) return Array.Empty<SceneConversation>();
        return ScoutingMeetings.Where(m => m.Id != exceptId && m.Status == SceneConversationStatus.Scheduled &&
                (long)m.Day * 1440 + m.StartMinute >= now && (long)m.Day * 1440 + m.StartMinute < through)
            .OrderBy(m => m.Day).ThenBy(m => m.StartMinute).ThenBy(m => m.Id, StringComparer.Ordinal).ToArray();
    }

    public string ScoutingAppointmentRiskText(int throughDay, int throughMinute, string exceptId = null) {
        var risks = ScoutingAppointmentRisks(throughDay, throughMinute, exceptId);
        if (risks.Count == 0) return null;
        return "This choice passes an agreed start: " + string.Join("; ", risks.Select(m =>
            $"{m.ArtistName} · {m.Activity.ToString().ToLowerInvariant()} · {LocalSceneRoomService.Date(m.Day).ToHeadlineString()} {m.StartMinute / 60:00}:{m.StartMinute % 60:00} · " +
            (m.Activity == ScoutingAppointmentActivity.Demo ? "home budget studio" : SceneRoomCatalog.Get(m.RoomId)?.Name))) +
            ". To keep it, attend first. You can also cancel or continue without attending.";
    }
}
