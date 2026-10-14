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
		Color fillColor = active ? ActiveColor : InactiveColor;
		int stagger = GetIndex() % 3;   // cut at three heights, like a filing folder
		// The dossier leaves a 10px gap between the tab row and the card, so the open tab reaches 12px down to meet it.
		AddThemeStyleboxOverride("normal", FolderTabStyle.Make(fillColor, active, 12f, stagger));
		AddThemeStyleboxOverride("hover", FolderTabStyle.Make(active ? fillColor : fillColor.Lightened(.15f), active, 12f, stagger));
		AddThemeStyleboxOverride("pressed", FolderTabStyle.Make(fillColor, active, 12f, stagger));
		AddThemeStyleboxOverride("disabled", FolderTabStyle.Make(fillColor, active, 12f, stagger));
		AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
		foreach (string name in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_focus_color", "font_disabled_color" })
			AddThemeColorOverride(name, ink);
		AddThemeFontOverride("font", PaperTheme.Elite);
		AddThemeFontSizeOverride("font_size", 16);
		ZIndex = active ? 1 : 0;
	}
}
