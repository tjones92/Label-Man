using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// The founding step "YOUR LABEL'S PAPER": six crest shapes, five period colour pairs, five lettering faces, a
/// monogram and SURPRISE ME, beside a live preview of the 45's centre label and the letterhead.
///
/// Until the player touches a control the brand simply follows the name (a hash of it, via
/// <see cref="LabelBrand.Derive"/>), so skipping the step still yields a look no other label has. The first
/// choice made locks the brand in; RESET hands it back to the name.
/// </summary>
public partial class LabelBrandPicker : VBoxContainer {
	private static readonly Color Ink = PaperTheme.Ink;
	private static readonly Color ChoiceFill = new("ead8ad");

	/// <summary>Raised after any change, so the founding page can remember a customised brand across rebuilds.</summary>
	public event Action Changed;

	private string labelName = "";
	private LabelBrand brand;
	private readonly List<Button> crestButtons = new(), paletteButtons = new(), letteringButtons = new();
	private readonly List<LabelCrest> crestFaces = new();
	private readonly List<Label> letteringSamples = new();
	private LabelCrest disc;
	private VBoxContainer letterheadHost;
	private LineEdit monogramEdit;
	private bool syncing;

	/// <summary>True once the player has chosen something; false while the brand is just the name's default.</summary>
	public bool Customised { get; private set; }

	/// <summary>The brand as it stands (a copy: the caller may keep it).</summary>
	public LabelBrand Current => brand.Clone();

	private string DisplayName => string.IsNullOrWhiteSpace(labelName) ? "Player Records" : labelName.Trim();

	public LabelBrandPicker(LabelBrand chosen, string name) {
		labelName = name ?? "";
		Customised = chosen != null;
		brand = chosen?.Clone() ?? LabelBrand.Derive(DisplayName);
		AddThemeConstantOverride("separation", 10);
		Build();
		Sync();
	}

	/// <summary>The name field changed: follow it until the player has made a choice of their own.</summary>
	public void SetLabelName(string name) {
		labelName = name ?? "";
		if (!Customised) brand = LabelBrand.Derive(DisplayName);
		Sync();
	}

	private void Build() {
		var split = new HBoxContainer();
		split.AddThemeConstantOverride("separation", 28);
		AddChild(split);

		var choices = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		choices.AddThemeConstantOverride("separation", 8);
		split.AddChild(choices);

		choices.AddChild(Caption("CREST"));
		var crestRow = new HBoxContainer();
		crestRow.AddThemeConstantOverride("separation", 6);
		choices.AddChild(crestRow);
		foreach (CrestShape shape in Enum.GetValues<CrestShape>()) {
			CrestShape captured = shape;
			Button button = ChoiceButton(shape.ToString());
			button.CustomMinimumSize = new Vector2(66, 66);
			LabelCrest face = new LabelCrest();
			face.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
			face.OffsetLeft = face.OffsetTop = 8; face.OffsetRight = face.OffsetBottom = -8;
			button.AddChild(face);
			button.Pressed += () => Choose(() => brand.Crest = captured);
			crestRow.AddChild(button);
			crestButtons.Add(button);
			crestFaces.Add(face);
		}

		choices.AddChild(Caption("COLOURS"));
		var paletteRow = new HBoxContainer();
		paletteRow.AddThemeConstantOverride("separation", 6);
		choices.AddChild(paletteRow);
		for (int i = 0; i < LabelBrand.Palettes.Length; i++) {
			int index = i;
			Button button = ChoiceButton(LabelBrand.Palettes[i].Name);
			button.CustomMinimumSize = new Vector2(66, 48);
			var swatch = new LabelCrest().Set(new LabelBrand { Crest = CrestShape.Roundel, PaletteIndex = i, Monogram = "" }, "", 32);
			swatch.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
			swatch.OffsetLeft = swatch.OffsetTop = 7; swatch.OffsetRight = swatch.OffsetBottom = -7;
			button.AddChild(swatch);
			button.Pressed += () => Choose(() => brand.PaletteIndex = index);
			paletteRow.AddChild(button);
			paletteButtons.Add(button);
		}

		choices.AddChild(Caption("LETTERING"));
		var letteringRow = new HBoxContainer();
		letteringRow.AddThemeConstantOverride("separation", 6);
		choices.AddChild(letteringRow);
		foreach (LetteringStyle style in Enum.GetValues<LetteringStyle>()) {
			LetteringStyle captured = style;
			Button button = ChoiceButton(style.ToString());
			button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			button.CustomMinimumSize = new Vector2(0, 52);
			var sample = new Label {
				HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
				ClipText = true, MouseFilter = MouseFilterEnum.Ignore
			};
			sample.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
			sample.OffsetLeft = sample.OffsetRight = 0;
			sample.AddThemeFontOverride("font", PaperTheme.Lettering(style));
			sample.AddThemeFontSizeOverride("font_size", style == LetteringStyle.Script ? 26 : 18);
			sample.AddThemeColorOverride("font_color", Ink);
			button.AddChild(sample);
			button.Pressed += () => Choose(() => brand.Lettering = captured);
			letteringRow.AddChild(button);
			letteringButtons.Add(button);
			letteringSamples.Add(sample);
		}

		var extras = new HBoxContainer();
		extras.AddThemeConstantOverride("separation", 10);
		choices.AddChild(extras);
		extras.AddChild(Caption("MONOGRAM"));
		monogramEdit = new LineEdit { MaxLength = LabelBrand.MaxMonogramLength, CustomMinimumSize = new Vector2(90, 34), Alignment = HorizontalAlignment.Center };
		monogramEdit.TooltipText = "Up to three letters on the crest. Leave it empty to use the label's initials.";
		monogramEdit.TextChanged += text => {
			if (syncing) return;
			string cleaned = LabelBrand.CleanMonogram(text);
			if (cleaned != text) { monogramEdit.Text = cleaned; monogramEdit.CaretColumn = cleaned.Length; }
			Choose(() => brand.Monogram = cleaned, resync: false);
			SyncPreview();
		};
		extras.AddChild(monogramEdit);
		var surprise = new Button { Text = "SURPRISE ME", TooltipText = "Pick a crest, colours and lettering at random." };
		surprise.Pressed += () => {
			var random = new Random();   // a UI roll: never the sim's RNG streams
			Choose(() => {
				brand.Crest = (CrestShape)random.Next(Enum.GetValues<CrestShape>().Length);
				brand.PaletteIndex = random.Next(LabelBrand.Palettes.Length);
				brand.Lettering = (LetteringStyle)random.Next(Enum.GetValues<LetteringStyle>().Length);
			});
		};
		extras.AddChild(surprise);
		var reset = new Button { Text = "USE THE NAME'S OWN", TooltipText = "Let the label's name decide: every name gets its own crest." };
		reset.Pressed += () => {
			Customised = false;
			brand = LabelBrand.Derive(DisplayName);
			Sync();
			Changed?.Invoke();
		};
		extras.AddChild(reset);

		var preview = new VBoxContainer { CustomMinimumSize = new Vector2(330, 0) };
		preview.AddThemeConstantOverride("separation", 10);
		split.AddChild(preview);
		preview.AddChild(Caption("THE 45'S CENTRE LABEL"));
		disc = new LabelCrest { SizeFlagsHorizontal = SizeFlags.ShrinkCenter };
		preview.AddChild(disc);
		preview.AddChild(Caption("THE LETTERHEAD"));
		letterheadHost = new VBoxContainer();
		preview.AddChild(letterheadHost);
	}

	private static Label Caption(string text) {
		var label = new Label { Text = text };
		label.AddThemeFontOverride("font", PaperTheme.SansSemiBold);
		label.AddThemeFontSizeOverride("font_size", 13);
		label.AddThemeColorOverride("font_color", PaperTheme.Fade);
		return label;
	}

	private static Button ChoiceButton(string tooltip) => new() { TooltipText = tooltip, ToggleMode = false, FocusMode = FocusModeEnum.None };

	private void Choose(Action change, bool resync = true) {
		change();
		Customised = true;
		if (resync) Sync();
		Changed?.Invoke();
	}

	private static void Mark(Button button, bool selected) {
		string[] states = { "normal", "hover", "pressed", "focus" };
		foreach (string state in states) {
			if (!selected) { button.RemoveThemeStyleboxOverride(state); continue; }
			button.AddThemeStyleboxOverride(state, new StyleBoxFlat {
				BgColor = PaperTheme.Highlighter, BorderColor = PaperTheme.Rust,
				BorderWidthLeft = 3, BorderWidthRight = 3, BorderWidthTop = 3, BorderWidthBottom = 3,
				CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4
			});
		}
	}

	private void Sync() {
		syncing = true;
		for (int i = 0; i < crestButtons.Count; i++) {
			Mark(crestButtons[i], (int)brand.Crest == i);
			LabelBrand face = brand.Clone();
			face.Crest = (CrestShape)i;
			crestFaces[i].Set(face, DisplayName, 40);
		}
		for (int i = 0; i < paletteButtons.Count; i++) Mark(paletteButtons[i], brand.PaletteIndex == i);
		for (int i = 0; i < letteringButtons.Count; i++) {
			Mark(letteringButtons[i], (int)brand.Lettering == i);
			bool script = i == (int)LetteringStyle.Script;
			string word = DisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
			letteringSamples[i].Text = script ? word : word.ToUpperInvariant();
		}
		monogramEdit.PlaceholderText = LabelBrand.InitialsOf(DisplayName);
		if (monogramEdit.Text != brand.Monogram) monogramEdit.Text = brand.Monogram;
		syncing = false;
		SyncPreview();
	}

	private void SyncPreview() {
		disc.Set(brand, DisplayName, 170f, LabelCrest.Mode.Disc45);
		foreach (Node child in letterheadHost.GetChildren()) { letterheadHost.RemoveChild(child); child.QueueFree(); }
		letterheadHost.AddChild(LabelCrest.Letterhead(brand, DisplayName, 44, 26, 270f));
	}
}
