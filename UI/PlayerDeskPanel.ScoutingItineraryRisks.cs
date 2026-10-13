using Godot;

public partial class PlayerDeskPanel {
    private void ScoutingTimeRisk(PlayerDesk desk, Button action, int day, int minute, string exceptId = null) {
        string warning = desk.ScoutingAppointmentRiskText(day, minute, exceptId);
        if (warning == null) return;
        Body(warning);
        action.Text += " · !";
        action.TooltipText += "\n" + warning;
    }
}
