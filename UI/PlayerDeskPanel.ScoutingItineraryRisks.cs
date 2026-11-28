using Godot;

public partial class PlayerDeskPanel {
    private void ScoutingTimeRisk(PlayerDesk desk, Button action, int day, int minute, string exceptId = null) {
        if (day == LocalSceneRoomService.Day(TimeManager.Instance.CurrentDate)) AddTonightPreview(action, desk, minute);
        string warning = desk.ScoutingAppointmentRiskText(day, minute, exceptId);
        if (warning == null) return;
        Body(warning);
        action.Text += " · !";
        action.TooltipText += "\n" + warning;
    }
}
