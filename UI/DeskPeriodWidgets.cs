using Godot;

// Period pass 2026-11-05 ("Restrained"): small drawn objects that replace modern UI furniture in the office header,
// tabs and forms -- a wall clock and a time card instead of a payroll sentence, pencil tallies and a spindle instead of
// counter badges, dot leaders instead of a drop-down, a typed box instead of a checkbox widget.

/// <summary>A schoolroom wall clock showing the office time, with the rest of the regular day shaded on the dial.</summary>
public partial class WallClock : Control {
	private int hour = 9, minute;
	private int endHour = 18;

	public WallClock() {
		CustomMinimumSize = new Vector2(54, 54);
		MouseFilter = MouseFilterEnum.Stop;
		SizeFlagsVertical = SizeFlags.ShrinkCenter;
	}

	public void Set(int currentHour, int currentMinute, int workdayEndHour) {
		hour = currentHour; minute = currentMinute; endHour = workdayEndHour;
		QueueRedraw();
	}

	public override void _Draw() {
		Vector2 c = Size / 2f;
		float r = Mathf.Min(Size.X, Size.Y) / 2f - 2f;
		Color ink = PaperTheme.Ink;
		DrawCircle(c, r, PaperTheme.Field);
		// The rest of the regular day, a faint oxblood wedge from now to quitting time (only when inside 12 hours).
		float now = (hour % 12) + minute / 60f;
		int left = endHour * 60 - (hour * 60 + minute);
		if (left > 0 && left < 12 * 60) {
			float a0 = now / 12f * Mathf.Tau - Mathf.Pi / 2f, sweep = left / 720f * Mathf.Tau;
			var points = new System.Collections.Generic.List<Vector2> { c };
			for (int i = 0; i <= 24; i++) points.Add(c + Vector2.FromAngle(a0 + sweep * i / 24f) * (r - 2f));
			DrawColoredPolygon(points.ToArray(), new Color(PaperTheme.Oxblood, 0.13f));
		}
		DrawArc(c, r, 0, Mathf.Tau, 48, ink, 3f, true);
		for (int i = 0; i < 12; i++) {
			Vector2 dir = Vector2.FromAngle(i / 12f * Mathf.Tau - Mathf.Pi / 2f);
			float inner = i % 3 == 0 ? r - 7f : r - 4f;
			DrawLine(c + dir * inner, c + dir * (r - 1.5f), ink, i % 3 == 0 ? 2f : 1f, true);
		}
		Vector2 hourHand = Vector2.FromAngle(now / 12f * Mathf.Tau - Mathf.Pi / 2f) * (r * 0.5f);
		Vector2 minuteHand = Vector2.FromAngle(minute / 60f * Mathf.Tau - Mathf.Pi / 2f) * (r * 0.78f);
		DrawLine(c, c + hourHand, ink, 3f, true);
		DrawLine(c, c + minuteHand, ink, 2f, true);
		DrawCircle(c, 2.5f, PaperTheme.Oxblood);
	}
}

/// <summary>A clocking-in card: the regular hours and the overtime stop, typed, with how long is left.</summary>
public partial class TimeCard : PanelContainer {
	private readonly Label text;

	public TimeCard() {
		SizeFlagsVertical = SizeFlags.ShrinkCenter;
		MouseFilter = MouseFilterEnum.Stop;
		AddThemeStyleboxOverride("panel", new StyleBoxFlat {
			BgColor = new Color("efe0b8"), BorderColor = PaperTheme.Edge,
			BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
			ContentMarginLeft = 18, ContentMarginRight = 10, ContentMarginTop = 4, ContentMarginBottom = 4
		});
		text = new Label { MouseFilter = MouseFilterEnum.Ignore };
		text.AddThemeFontOverride("font", PaperTheme.Typed);
		text.AddThemeFontSizeOverride("font_size", 13);
		text.AddThemeColorOverride("font_color", PaperTheme.Ink);
		text.AddThemeConstantOverride("line_spacing", -2);
		AddChild(text);
	}

	/// <summary>Times are short ("9:00", "6:00"); <paramref name="left"/> is the remaining stretch ("5 hrs left").</summary>
	public void Set(string clockIn, string clockOut, string overtimeTo, string left) {
		text.Text = $"IN {clockIn}   OUT {clockOut}\nO.T. to {overtimeTo}  ·  {left}";
	}

	public override void _Draw() {
		// The two punched holes down the card's margin.
		Color hole = new("2a1d14");
		DrawCircle(new Vector2(8, Size.Y * 0.32f), 3f, hole);
		DrawCircle(new Vector2(8, Size.Y * 0.70f), 3f, hole);
	}
}

/// <summary>An expanding run of dots between a destination and its fare, as on a printed timetable.</summary>
public partial class DotLeader : Control {
	public DotLeader() {
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		CustomMinimumSize = new Vector2(24, 0);
		MouseFilter = MouseFilterEnum.Ignore;
	}

	public override void _Draw() {
		float y = Size.Y * 0.72f;
		for (float x = 4f; x < Size.X - 4f; x += 6f) DrawCircle(new Vector2(x, y), 1f, new Color(PaperTheme.Edge, 0.9f));
	}
}

/// <summary>Typewriter check boxes: a struck square, empty or with an X typed over it.</summary>
public static class TypedBox {
	public static ImageTexture Icon(bool on) {
		const int size = 18;
		var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
		Color ink = PaperTheme.Ink;
		for (int i = 2; i < size - 2; i++) {
			image.SetPixel(i, 2, ink); image.SetPixel(i, size - 3, ink);
			image.SetPixel(2, i, ink); image.SetPixel(size - 3, i, ink);
		}
		if (on) {
			for (int i = 4; i < size - 4; i++) {
				image.SetPixel(i, i, ink); image.SetPixel(i, size - 1 - i, ink);
				image.SetPixel(i + 1, i, ink); image.SetPixel(i + 1, size - 1 - i, ink);
			}
		}
		return ImageTexture.CreateFromImage(image);
	}
}
