using System.Linq;
using Godot;

public partial class PlayerDeskPanel {
    private void ScoutingListenerControls(PlayerDesk desk, SceneBill bill, string eventId) {
        var listeners = desk.ScoutingListeners(bill.Id, eventId).Concat(desk.HiredScoutingListeners(bill.Id, eventId)).ToArray();
        if (listeners.Length == 0) return;
        var choices = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var select = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        foreach (var listener in listeners) select.AddItem(listener.Name + " — " + listener.Channel + "; " + listener.Expertise);
        select.TooltipText = "Choose a roster companion or hire a local session-qualified musician for this set. Current performers and booked musicians cannot accompany you. Musical skills are listening proxies; this is no guarantee of a correct judgment.";
        var together = Btn("");
        var prices = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        prices.AddThemeColorOverride("font_color", Ink);
        void Update() {
            var listener = listeners[select.Selected];
            float playerAdmission = desk.AdmissionForVisit(bill, SceneRoomCatalog.Get(bill.RoomId),
                TimeManager.Instance.CurrentHour * 60 + TimeManager.Instance.CurrentMinute);
            float total = playerAdmission + listener.Admission + listener.ConsultationFee;
            prices.Text = $"Your admission ${playerAdmission:0.00} · listener admission ${listener.Admission:0.00} · consultation ${listener.ConsultationFee:0.00}.";
            together.Text = listener.ConsultationFee > 0 ? $"HIRE LISTENER (${total:0.00} total · ${listener.ConsultationFee:0.00} consultation)" : $"LISTEN TOGETHER ({Money(total)} total admission)";
            together.Disabled = desk.Label.cashReserves < total;
            together.TooltipText = "Both hear the same remaining set. Pay each visitor's admission once per continuous visit. A hired musician charges the displayed consultation fee for each set; a roster companion charges no consultation fee. No additional action time. Availability and the quote are checked again. Time and agreed costs are consumed if the set cancels during the visit.";
            string risk = desk.ScoutingAppointmentRiskText(bill.Day, bill.Appearances.First(a => ScenePerformanceService.EventId(bill, a) == eventId).EndHour * 60);
            if (risk != null) { together.Text += " · PASSES APPOINTMENT START"; prices.Text += "\n" + risk; together.TooltipText += "\n" + risk; }
        }
        select.ItemSelected += _ => Update();
        together.Pressed += () => Act(() => {
            var listener = listeners[select.Selected];
            bool ok = desk.ListenSceneAppearanceWithListener(bill.Id, eventId, listener.PersonId, listener.QuoteId, out string result);
            Say(result, ok); return ok;
        });
        Update(); choices.AddChild(select); choices.AddChild(prices); choices.AddChild(together); content.AddChild(choices);
    }
}
