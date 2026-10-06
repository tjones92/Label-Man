using Godot;

/// <summary>A folder tab. Selected = the paper colour with ink text, so it reads as the open page; the
/// others are darker tan with ink text at full contrast. State is a style, not a Disabled button or a
/// Modulate fade, either of which dragged the selected label down to about 1.6:1.</summary>
public partial class FolderTabButton : Button
{
	[Export] public Color ActiveColor = new("f1e5c8");
	[Export] public Color InactiveColor = new("c6a35f");

	public void SetActive(bool active)
	{
		Modulate = Colors.White;
		Color ink = new("2b2115");
		StyleBoxFlat Box(Color fill) => new() {
			BgColor = fill, BorderColor = new Color("70552c"),
			BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = active ? 0 : 1,
			CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4,
			ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 6, ContentMarginBottom = 6
		};
		Color fillColor = active ? ActiveColor : InactiveColor;
		AddThemeStyleboxOverride("normal", Box(fillColor));
		AddThemeStyleboxOverride("hover", Box(active ? fillColor : fillColor.Lightened(.15f)));
		AddThemeStyleboxOverride("pressed", Box(fillColor));
		AddThemeStyleboxOverride("disabled", Box(fillColor));
		AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
		foreach (string name in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_focus_color", "font_disabled_color" })
			AddThemeColorOverride(name, ink);
	}
}
