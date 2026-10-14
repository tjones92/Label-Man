using Godot;

/// <summary>
/// The tooltip body for anything that explains itself on hover. Godot's stock tooltip never wraps, so a
/// sentence-long explanation draws as one line across the screen; this gives it a fixed measure and sets
/// the text centred, like a typed tag.
/// </summary>
public static class PaperTip {
	public const float Width = 340f;

	public static Control Make(string text) {
		var label = new Label {
			Text = text,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			HorizontalAlignment = HorizontalAlignment.Center,
			CustomMinimumSize = new Vector2(Width, 0)
		};
		label.AddThemeFontOverride("font", PaperTheme.Sans);
		label.AddThemeFontSizeOverride("font_size", 15);
		label.AddThemeColorOverride("font_color", PaperTheme.Ink);
		return label;
	}
}

/// <summary>A label whose hover tooltip wraps and centres (the "?" badges).</summary>
public partial class TipLabel : Label {
	public override GodotObject _MakeCustomTooltip(string forText) => PaperTip.Make(forText);
}

/// <summary>A button whose hover tooltip wraps and centres.</summary>
public partial class TipButton : Button {
	public override GodotObject _MakeCustomTooltip(string forText) => PaperTip.Make(forText);
}
