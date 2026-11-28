using Godot;

public partial class PlayerDeskPanel {
	internal static Label TimeMorningLabel() {
		var line = new Label { Name = "FatigueMorning", Visible = false, MouseFilter = MouseFilterEnum.Ignore,
			AutowrapMode = TextServer.AutowrapMode.WordSmart };
		line.AddThemeFontOverride("font", PaperTheme.SerifItalic);
		line.AddThemeFontSizeOverride("font_size", 15);
		line.AddThemeColorOverride("font_color", PaperTheme.Oxblood);
		return line;
	}

	internal static void SetTimeMorning(Label label, PlayerDesk desk) {
		label.Text = desk?.FatigueMorningLine ?? "";
		label.Visible = label.Text.Length > 0;
	}

	internal static string RentInHand(AILabel label) {
		float overhead = label.GetMonthlyOverhead();
		int months = overhead > 0 ? Mathf.FloorToInt(Mathf.Max(0f, label.cashReserves) / overhead) : 0;
		return $"{Plural.Of(months, "month")} of rent in hand";
	}

	private void RefreshTimeCard(TimeManager clock, PlayerDesk desk) {
		deskClock.Visible = timeCard.Visible = clock != null;
		if (clock == null) return;
		deskClock.Set(clock.CurrentHour, clock.CurrentMinute, clock.RegularWorkdayEndHour);
		timeCard.Set(clock, desk);
		deskClock.TooltipText = timeCard.TooltipText;
	}

	internal static void SetWaitPreview(Button button, PlayerDesk desk, int hours) {
		int minutes = desk.WaitMinutes(hours);
		button.Disabled = minutes == 0;
		button.TooltipText = minutes == 0 ? "Turn in for the day." :
			$"Wait {StudioClockStrip.DurationText(minutes)} to {PlayerDesk.ClockText(desk.NowMinute + minutes)}, unless someone calls or knocks.";
		AddTonightPreview(button, desk, desk.NowMinute + minutes);
	}

	private static void AddTonightPreview(Button button, PlayerDesk desk, int end, int extraOwedMinutes = 0) {
		if (!button.Disabled && desk.TonightFatiguePreview(end, extraOwedMinutes) is { } preview)
			button.TooltipText = string.IsNullOrEmpty(button.TooltipText) ? preview : button.TooltipText + "\n" + preview;
	}

	private static void AddTravelTonightPreview(Button button, PlayerDesk desk, int hours) {
		bool bus = desk.ActiveMode == PlayerDesk.TransportMode.Bus;
		if (TimeManager.Instance?.CanAffordHours(hours, allowOvertime: !bus) == true)
			AddTonightPreview(button, desk, desk.NowMinute + hours * 60, bus && hours >= 4 ? 60 : 0);
	}
}
