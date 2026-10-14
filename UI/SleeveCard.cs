using System.Collections.Generic;
using Godot;

/// <summary>
/// A roster card drawn as a 45 sleeve: a printed paper sleeve with the act's publicity photo pasted on, the name typed
/// across the top, the working figures and verbs beside it, and a die-cut window on the right through which the
/// player's own 45 shows (vinyl, grooves, the label's centre label). It is a PanelContainer so it sizes to its content;
/// the printed accent strip and the window are drawn by a mouse-ignoring child laid over it, because a script draw on a
/// PanelContainer runs before its stylebox and would be painted over.
/// </summary>
public partial class SleeveCard : PanelContainer {
	private LabelBrand brand;
	private string labelName;
	private readonly VBoxContainer text = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
	private readonly VBoxContainer verbs = new() { SizeFlagsVertical = SizeFlags.ShrinkCenter };

	public SleeveCard Set(SimulatedArtist artist, AILabel house, string kicker, bool alert) {
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		brand = LabelBrand.For(house);
		labelName = house?.labelName ?? "";
		uint stock = Portraits.Hash(artist.artistId ?? artist.stageName) % 3u;
		// Sleeve stock: kraft, cream or a faded white.
		Color fill = stock == 0 ? new Color("e6d3a0") : stock == 1 ? new Color("efe4c4") : new Color("f1ebd8");
		var paper = PaperStyleBox.Sheet(fill, 16, 14, 8);
		paper.Burn = 0.8f; paper.Falloff = 0.6f;
		paper.ContentMarginTop = 26;
		AddThemeStyleboxOverride("panel", paper);

		var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		row.AddThemeConstantOverride("separation", 16);
		AddChild(row);
		row.AddChild(new PortraitPhoto().Set(Portraits.ForAct(artist), new Vector2(108, 136), artist.artistId));

		text.AddThemeConstantOverride("separation", 3);
		row.AddChild(text);

		var name = new Label {
			Text = artist.stageName, ClipText = true, TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis, TooltipText = artist.stageName,
			CustomMinimumSize = new Vector2(0, 40), VerticalAlignment = VerticalAlignment.Center
		};
		name.AddThemeFontOverride("font", PaperTheme.Elite);
		name.AddThemeFontSizeOverride("font_size", 26);
		name.AddThemeColorOverride("font_color", PaperTheme.Ink);
		text.AddChild(name);
		var line = new Label { Text = kicker.ToUpperInvariant() };
		line.AddThemeFontOverride("font", PaperTheme.SansSemiBold);
		line.AddThemeFontSizeOverride("font_size", 13);
		line.AddThemeColorOverride("font_color", alert ? new Color("a8322a") : PaperTheme.Rust);
		text.AddChild(line);

		verbs.AddThemeConstantOverride("separation", 6);
		row.AddChild(verbs);
		row.AddChild(new Control { CustomMinimumSize = new Vector2(132, 132), MouseFilter = MouseFilterEnum.Ignore });   // room for the die-cut window

		var overlay = new SleeveOverlay { Owner2 = this, MouseFilter = MouseFilterEnum.Ignore };
		overlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(overlay);
		return this;
	}

	public void AddFact(string fact, Color? color = null) {
		var label = new Label { Text = fact, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill };
		label.AddThemeFontSizeOverride("font_size", 15);
		label.AddThemeColorOverride("font_color", color ?? PaperTheme.Ink);
		text.AddChild(label);
	}

	public void AddVerb(Button button) {
		button.CustomMinimumSize = new Vector2(Mathf.Max(button.CustomMinimumSize.X, 128f), 34f);
		verbs.AddChild(button);
	}

	/// <summary>The printed strip across the top and the die-cut window at the right.</summary>
	private partial class SleeveOverlay : Control {
		public SleeveCard Owner2;

		public override void _Draw() {
			if (Owner2?.brand == null) return;
			LabelBrand brand = Owner2.brand;
			// printed accent strip, in the label's own ink
			// This overlay fills the sleeve's content area, inside its margins; the strip runs out to the sleeve's own edges.
			const float marginX = 16f, marginTop = 26f;
			DrawRect(new Rect2(-marginX + 1f, -marginTop + 1f, Size.X + marginX * 2f - 2f, 8), brand.Pair.Ground);
			DrawRect(new Rect2(-marginX + 1f, -marginTop + 9f, Size.X + marginX * 2f - 2f, 2), new Color(brand.Pair.Accent, 0.9f));
			DrawString(PaperTheme.SansBold, new Vector2(Size.X - 360, -4), "45 RPM  ·  MONAURAL", HorizontalAlignment.Right, 190, 10, new Color(PaperTheme.Fade, 0.8f));

			// the die-cut window: a hole in the sleeve with the record showing through it
			float radius = 58f;
			Vector2 centre = new(Size.X - 16f - 66f, Size.Y / 2f + 6f);
			DrawCircle(centre + new Vector2(1.5f, 2.5f), radius + 3f, new Color(0, 0, 0, 0.22f));     // the edge of the hole, shaded
			DrawCircle(centre, radius + 1.5f, new Color("8a7048"));
			DrawCircle(centre, radius, new Color("14110e"));                                          // vinyl
			for (float r = radius * 0.72f; r < radius - 2f; r += 3.1f)
				DrawArc(centre, r, 0f, Mathf.Tau, 48, new Color("2a241d"), 1f, true);
			DrawArc(centre, radius * 0.84f, -2.6f, -1.2f, 18, new Color(1f, 1f, 1f, 0.10f), 2f, true);   // a lamp glint on the grooves
			float label = radius * 0.62f;
			LabelCrest.DrawDisc45(this, new Rect2(centre - new Vector2(label, label), new Vector2(label * 2f, label * 2f)), Owner2.brand, Owner2.labelName);
			// the lip of the hole: lit lower right, shadowed upper left
			DrawArc(centre, radius + 0.5f, Mathf.Pi * 0.95f, Mathf.Pi * 1.85f, 24, new Color(0, 0, 0, 0.55f), 3f, true);
			DrawArc(centre, radius + 1f, -0.1f, Mathf.Pi * 0.8f, 24, new Color(1f, 0.95f, 0.8f, 0.35f), 2f, true);
		}
	}
}
