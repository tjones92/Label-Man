using Godot;
using System.Linq;

/// <summary>The appointment book (OnboardingTutorialDirective.md §4c, T5 step 1): the Label Man's diary, opened from the
/// desk header or the book on the desk. It lists what's booked, soonest first, and the visits still waiting on a time. It
/// never speaks: no badge, no count, no "Banks at 3". He has to open it and look.</summary>
public partial class PlayerDeskPanel {
	/// <summary>The book's modal while it's open, so a pick or a GO can close it and open it again on the new page.</summary>
	private PaperModal appointmentBook;

	public void OpenAppointmentBook(PlayerDesk desk, Node host = null) {
		if (desk?.Label == null) return;
		if (appointmentBook != null && IsInstanceValid(appointmentBook)) appointmentBook.Close();
		var modal = appointmentBook = PaperModal.Open(host ?? this, "THE BOOK", 760);
		var clock = TimeManager.Instance;
		int today = LocalSceneRoomService.Day(clock.CurrentDate);
		modal.AddText($"{clock.CurrentDate.DayName}, {clock.CurrentDate.ShortMonthName} {clock.CurrentDate.day}, {PlayerDesk.ClockText(clock.CurrentHour * 60 + clock.CurrentMinute)}. " +
			"Nobody reminds you. If it's written here, it's on you to be there.");
		var entries = desk.AppointmentBook();
		if (entries.Count == 0) modal.AddText("Nothing written down.");
		var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(0, Mathf.Min(520, 90 + entries.Count * 74)), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
		var list = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		list.AddThemeConstantOverride("separation", 6);
		scroll.AddChild(list);
		if (entries.Count > 0) modal.Body.AddChild(scroll);
		int? lastDay = null;
		foreach (var e in entries) {
			int day = e.Kind == BookEntryKind.Sitting ? System.Math.Max(e.Day, today) : e.Day;
			if (day != lastDay) {
				lastDay = day;
				var d = LocalSceneRoomService.Date(day);
				string when = day == today ? "TODAY · " : day == today + 1 ? "TOMORROW · " : "";
				var head = new Label { Text = when + $"{d.DayName}, {d.ShortMonthName} {d.day}".ToUpperInvariant() };
				head.AddThemeFontOverride("font", PaperTheme.SansSemiBold);
				head.AddThemeFontSizeOverride("font_size", 14);
				head.AddThemeColorOverride("font_color", PaperTheme.Rust);
				list.AddChild(head);
				list.AddChild(new ColorRect { Color = new Color(PaperTheme.Rust, 0.4f), CustomMinimumSize = new Vector2(0, 1), MouseFilter = MouseFilterEnum.Ignore });
			}
			list.AddChild(BookRow(desk, e, host));
		}
		// What he's set in motion and is waiting to hear about. No times: he doesn't know when the phone will ring, only that
		// it might, and that it won't ring for him while he's out (PlayerDesk.Phone).
		var waiting = desk.WaitingOn();
		if (waiting.Count > 0) {
			var head = new Label { Text = "WAITING ON" };
			head.AddThemeFontOverride("font", PaperTheme.SansSemiBold);
			head.AddThemeFontSizeOverride("font_size", 14);
			head.AddThemeColorOverride("font_color", PaperTheme.Rust);
			modal.Body.AddChild(head);
			modal.Body.AddChild(new ColorRect { Color = new Color(PaperTheme.Rust, 0.4f), CustomMinimumSize = new Vector2(0, 1), MouseFilter = MouseFilterEnum.Ignore });
			foreach (string w in waiting.Take(8)) {
				var line = new Label { Text = "· " + w, AutowrapMode = TextServer.AutowrapMode.WordSmart };
				line.AddThemeFontOverride("font", PaperTheme.Serif);
				line.AddThemeFontSizeOverride("font_size", 15);
				line.AddThemeColorOverride("font_color", PaperTheme.Ink);
				modal.Body.AddChild(line);
			}
			if (waiting.Count > 8) modal.AddText($"And {waiting.Count - 8} more.");
			modal.AddText("Nobody picks up while you're out.");
		}
		modal.AddButton("CLOSE THE BOOK", () => appointmentBook = null, PaperModal.ButtonKind.Primary);
	}

	private Control BookRow(PlayerDesk desk, BookEntry e, Node host) {
		var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		row.AddThemeConstantOverride("separation", 12);
		string time = e.Minute is { } m ? PlayerDesk.ClockText(m) + (e.Hours > 0 ? $"\n{e.Hours}h" : "")
			: e.Kind switch { BookEntryKind.PickATime => "no time\nyet", BookEntryKind.HoldoutSet => "their\nset", _ => "any day" };
		var t = new Label { Text = time, CustomMinimumSize = new Vector2(78, 0) };
		t.AddThemeFontOverride("font", PaperTheme.TypedBold);
		t.AddThemeFontSizeOverride("font_size", 15);
		t.AddThemeColorOverride("font_color", PaperTheme.Ink);
		row.AddChild(t);
		string where = string.IsNullOrEmpty(e.Place) ? "" : e.Kind == BookEntryKind.PickATime ? "" : "\n" + (e.Place.StartsWith("at ") || e.Place.StartsWith("the ") ? e.Place : "at " + e.Place);
		var what = new Label { Text = e.What + where, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill };
		what.AddThemeFontOverride("font", PaperTheme.Serif);
		what.AddThemeFontSizeOverride("font_size", 16);
		what.AddThemeColorOverride("font_color", PaperTheme.Ink);
		row.AddChild(what);
		if (e.Kind == BookEntryKind.PickATime) {
			var times = new VBoxContainer();
			for (int i = 0; i < e.Offered.Count; i++) {
				int choice = i;
				var o = e.Offered[i];
				var d = LocalSceneRoomService.Date(o.Day);
				var pick = Btn($"{d.ShortMonthName.ToUpperInvariant()} {d.day} · {PlayerDesk.ClockText(o.Minute)}");
				pick.TooltipText = $"Write it in: {PlayerDesk.WhenText(o.Day, o.Minute)}. Free.";
				pick.SetMeta("book_time", true);
				pick.Pressed += () => {
					bool ok = desk.BookApproachStep(e.ArtistId, e.Step, choice, out string message);
					SayBookResult(host, message, ok);
					OpenAppointmentBook(desk, host);
				};
				times.AddChild(pick);
			}
			row.AddChild(times);
		} else if (e.CanGo) {
			// What going now costs, on the button: the hours spent waiting there before the start.
			int waitHours = (int)System.Math.Ceiling(System.Math.Max(0, (e.Minute ?? 0) - desk.NowMinute) / 60.0);
			var go = Primary(waitHours >= 1 ? $"GO THERE · WAIT {waitHours}h" : "GO THERE");
			go.TooltipText = waitHours >= 1
				? $"Go now and wait there for {PlayerDesk.ClockText(e.Minute ?? 0)}: about {waitHours}h of waiting, then {e.Hours}h. The hours before it are yours if you go later."
				: $"Go now. It starts at {PlayerDesk.ClockText(e.Minute ?? 0)} and takes {e.Hours}h.";
			go.Pressed += () => {
				appointmentBook?.Close(); appointmentBook = null;
				bool ok = desk.TakeApproachStep(e.ArtistId, e.Step, out string message);
				SayBookResult(host, message, ok);
			};
			row.AddChild(go);
		}
		return row;
	}

	private void SayBookResult(Node host, string message, bool ok) {
		if (host is UIManager room) room.ShowOfficeTimeMessage(message);
		else { Say(message, ok); Refresh(); }
	}
}
