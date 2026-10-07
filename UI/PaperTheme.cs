using Godot;

/// <summary>
/// The one project Theme. Everything the office draws that is not a custom-styled card (buttons, drop-downs,
/// spin boxes, text fields, check boxes, scroll bars, popups, tooltips) takes its look from here, so the stock
/// Godot grey is gone from every screen at once instead of being overridden control by control.
///
/// Three bundled typefaces, all SIL OFL, so the look does not depend on what the player's OS ships:
///   Libre Franklin -- the trade-paper sans: UI text, buttons, headings.
///   Gelasio        -- the press serif (a metric-compatible Georgia): the newspaper.
///   Courier Prime  -- the office typewriter: typed fields and columns of figures.
///   Special Elite  -- the worn typewriter that types a name on an index card or a record sleeve (Apache 2.0).
/// Plus five display faces for a label's own lettering (see <see cref="Lettering"/>).
///
/// Colours are the room's own: ink and ledger paper from the desk panel, rust for headings, oxblood for the
/// one verb on a card that spends time or money, and a highlighter yellow for hover and selection.
/// </summary>
public static class PaperTheme {
	public static readonly Color Ink = new("2b2115");
	public static readonly Color Paper = new("f1e5c8");
	public static readonly Color PaperLight = new("f6ecd0");
	public static readonly Color Field = new("f8f0d9");
	public static readonly Color Rust = new("6b3a1c");
	public static readonly Color Oxblood = new("8f2a22");
	public static readonly Color Edge = new("8a7048");
	public static readonly Color Highlighter = new("f1d96b");
	public static readonly Color Fade = new("5a4a2e");

	/// <summary>Theme type variation for the verb that spends time or money (set <c>ThemeTypeVariation</c>).</summary>
	public const string Primary = "PrimaryButton";

	private static Theme theme;
	private static Font sans, sansSemi, sansBold, serif, serifBold, serifItalic, typed, typedBold, elite;
	private static Font[] lettering;

	/// <summary>Libre Franklin at regular weight: the default UI face.</summary>
	public static Font Sans { get { EnsureFonts(); return sans; } }
	public static Font SansSemiBold { get { EnsureFonts(); return sansSemi; } }
	public static Font SansBold { get { EnsureFonts(); return sansBold; } }
	/// <summary>Gelasio: the newspaper's body face.</summary>
	public static Font Serif { get { EnsureFonts(); return serif; } }
	public static Font SerifBold { get { EnsureFonts(); return serifBold; } }
	public static Font SerifItalic { get { EnsureFonts(); return serifItalic; } }
	/// <summary>Courier Prime: forms, typed fields and figures.</summary>
	public static Font Typed { get { EnsureFonts(); return typed; } }
	public static Font TypedBold { get { EnsureFonts(); return typedBold; } }
	/// <summary>Special Elite: the worn office typewriter, for the name typed on an index card or a sleeve.</summary>
	public static Font Elite { get { EnsureFonts(); return elite; } }

	/// <summary>The face a label prints its name in. Five open-licence display faces (Bevan, Abril Fatface, Alfa Slab One,
	/// Yellowtail, Limelight), one per <see cref="LetteringStyle"/>; Abril Fatface is also the newspaper's nameplate.</summary>
	public static Font Lettering(LetteringStyle style) {
		if (lettering == null) {
			lettering = new Font[] {
				LoadFont("res://UI/Fonts/Bevan-Regular.ttf"),            // Slab
				LoadFont("res://UI/Fonts/AbrilFatface-Regular.ttf"),     // Didone
				LoadFont("res://UI/Fonts/AlfaSlabOne-Regular.ttf"),      // HeavySlab
				LoadFont("res://UI/Fonts/Yellowtail-Regular.ttf"),       // Script
				LoadFont("res://UI/Fonts/Limelight-Regular.ttf")         // Deco
			};
		}
		return lettering[(int)style];
	}

	/// <summary>Puts the theme on the whole window, so every Control and popup under it inherits it.</summary>
	public static void Apply(Window window) {
		if (window != null) window.Theme = Build();
	}

	public static Theme Build() {
		if (theme != null) return theme;
		EnsureFonts();
		theme = new Theme { DefaultFont = sans, DefaultFontSize = 16 };

		BuildButtons();
		BuildFields();
		BuildChoosers();
		BuildScrollBars();
		BuildPopups();
		return theme;
	}

	// ---- fonts -------------------------------------------------------------------------------------

	private static void EnsureFonts() {
		if (sans != null) return;
		FontFile franklin = LoadFont("res://UI/Fonts/LibreFranklin.ttf");
		FontFile gelasio = LoadFont("res://UI/Fonts/Gelasio.ttf");
		FontFile gelasioItalic = LoadFont("res://UI/Fonts/Gelasio-Italic.ttf");
		FontFile courier = LoadFont("res://UI/Fonts/CourierPrime-Regular.ttf");
		FontFile courierBold = LoadFont("res://UI/Fonts/CourierPrime-Bold.ttf");
		sans = Weight(franklin, 500);   // a touch heavier than regular so body text keeps its ink at small sizes
		sansSemi = Weight(franklin, 650);
		sansBold = Weight(franklin, 700);
		serif = gelasio;
		serifBold = Weight(gelasio, 700);
		serifItalic = gelasioItalic;
		typed = courier;
		typedBold = courierBold;
		// Special Elite is the card-name face; a build without the file keeps the old Courier Prime Bold.
		elite = FileAccess.FileExists("res://UI/Fonts/SpecialElite-Regular.ttf") ? LoadFont("res://UI/Fonts/SpecialElite-Regular.ttf") : courierBold;
	}

	/// <summary>The imported font if the editor has imported it; otherwise the raw file bytes, so a missing
	/// import never leaves the office without a typeface.</summary>
	private static FontFile LoadFont(string path) {
		if (ResourceLoader.Exists(path) && GD.Load<FontFile>(path) is FontFile imported) return imported;
		return new FontFile { Data = FileAccess.GetFileAsBytes(path) };
	}

	private static Font Weight(Font baseFont, float weight) {
		long tag = TextServerManager.GetPrimaryInterface().NameToTag("wght");
		return new FontVariation {
			BaseFont = baseFont,
			VariationOpentype = new Godot.Collections.Dictionary { { tag, weight } }
		};
	}

	// ---- shared pieces -------------------------------------------------------------------------------

	private static StyleBoxFlat Box(Color fill, Color border, int width = 2, int radius = 3, int bottom = -1,
			int marginX = 14, int marginY = 7) => new() {
		BgColor = fill, BorderColor = border,
		BorderWidthLeft = width, BorderWidthRight = width, BorderWidthTop = width, BorderWidthBottom = bottom < 0 ? width : bottom,
		CornerRadiusTopLeft = radius, CornerRadiusTopRight = radius, CornerRadiusBottomLeft = radius, CornerRadiusBottomRight = radius,
		ContentMarginLeft = marginX, ContentMarginRight = marginX, ContentMarginTop = marginY, ContentMarginBottom = marginY
	};

	private static StyleBoxFlat FocusRing(int radius = 3) => new() {
		DrawCenter = false, BorderColor = Rust,
		BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
		CornerRadiusTopLeft = radius, CornerRadiusTopRight = radius, CornerRadiusBottomLeft = radius, CornerRadiusBottomRight = radius
	};

	private static void SetColors(string type, Color normal, Color hover, Color pressed, Color disabled, params string[] names) {
		foreach (string name in names) {
			Color color = name.Contains("hover") ? hover : name.Contains("pressed") ? pressed : name.Contains("disabled") ? disabled : normal;
			theme.SetColor(name, type, color);
		}
	}

	/// <summary>A small solid triangle in the office's ink, for drop-down and spin-box arrows.</summary>
	private static ImageTexture Triangle(bool down, Color color, int size = 14) {
		var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
		int rows = size / 2, top = (size - rows) / 2;
		for (int i = 0; i < rows; i++) {
			int row = down ? top + i : top + (rows - 1 - i);
			for (int x = i; x < size - i; x++) image.SetPixel(x, row, color);
		}
		return ImageTexture.CreateFromImage(image);
	}

	/// <summary>The spin box's stacked pair of triangles, one icon.</summary>
	private static ImageTexture UpDown(Color color, int size = 14) {
		var image = Image.CreateEmpty(size, size + 4, false, Image.Format.Rgba8);
		int rows = size / 2 - 1;
		for (int i = 0; i < rows; i++) {
			for (int x = i; x < size - i; x++) {
				image.SetPixel(x, rows - 1 - i, color);          // up triangle, apex at the top
				image.SetPixel(x, size + 4 - rows + i, color);   // down triangle, apex at the bottom
			}
		}
		return ImageTexture.CreateFromImage(image);
	}

	/// <summary>Box icons drawn in ink: an empty outlined box when off, a filled box with a tick when on.</summary>
	public static ImageTexture CheckIcon(bool on) {
		const int size = 20;
		var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
		Color border = new("4a3a24"), fill = on ? Rust : new Color("fbf3dc");
		for (int y = 1; y < size - 1; y++)
			for (int x = 1; x < size - 1; x++) {
				bool edge = x < 3 || y < 3 || x >= size - 3 || y >= size - 3;
				image.SetPixel(x, y, edge ? border : fill);
			}
		if (on) {
			for (int i = 0; i < 4; i++) for (int t = 0; t < 3; t++) image.SetPixel(5 + i, 10 + i + t - 1, Godot.Colors.White);
			for (int i = 0; i < 7; i++) for (int t = 0; t < 3; t++) image.SetPixel(8 + i, 13 - i + t - 1, Godot.Colors.White);
		}
		return ImageTexture.CreateFromImage(image);
	}

	private static ImageTexture RadioIcon(bool on) {
		const int size = 18;
		var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
		float c = (size - 1) / 2f;
		for (int y = 0; y < size; y++)
			for (int x = 0; x < size; x++) {
				float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
				if (d <= 8f && d > 5.8f) image.SetPixel(x, y, new Color("4a3a24"));
				else if (d <= 5.8f) image.SetPixel(x, y, new Color("fbf3dc"));
				if (on && d <= 3.2f) image.SetPixel(x, y, Rust);
			}
		return ImageTexture.CreateFromImage(image);
	}

	// ---- controls ------------------------------------------------------------------------------------

	private static void BuildButtons() {
		Color disabledInk = new(Ink, 0.42f);

		// Secondary: a ruled outline on paper; the highlighter swipe on hover; a hairline, faded face when off.
		theme.SetFont("font", "Button", sansSemi);
		theme.SetFontSize("font_size", "Button", 15);
		theme.SetStylebox("normal", "Button", Box(new Color("f0e3bd"), Ink, 2, 3, 3));
		theme.SetStylebox("hover", "Button", Box(Highlighter, Ink, 2, 3, 3));
		theme.SetStylebox("pressed", "Button", Box(new Color("e2c95f"), Ink, 2, 3, 2));
		theme.SetStylebox("disabled", "Button", Box(new Color(Paper, 0.55f), new Color(Ink, 0.35f), 1, 3, 1));
		theme.SetStylebox("focus", "Button", FocusRing());
		SetColors("Button", Ink, Ink, Ink, disabledInk,
			"font_color", "font_hover_color", "font_pressed_color", "font_focus_color", "font_hover_pressed_color", "font_disabled_color");

		// Primary: the verb that spends time or money. Solid oxblood with a darker letterpress edge.
		theme.SetTypeVariation(Primary, "Button");
		Color cream = new("f8eed2");
		theme.SetStylebox("normal", Primary, Box(Oxblood, new Color("5e1a15"), 1, 3, 3));
		theme.SetStylebox("hover", Primary, Box(new Color("a63329"), new Color("5e1a15"), 1, 3, 3));
		theme.SetStylebox("pressed", Primary, Box(new Color("74211b"), new Color("5e1a15"), 1, 3, 1));
		theme.SetStylebox("disabled", Primary, Box(new Color(Paper, 0.55f), new Color(Ink, 0.35f), 1, 3, 1));
		theme.SetStylebox("focus", Primary, FocusRing());
		SetColors(Primary, cream, cream, cream, disabledInk,
			"font_color", "font_hover_color", "font_pressed_color", "font_focus_color", "font_hover_pressed_color", "font_disabled_color");

		// Check boxes have no filled face: their label sits on the paper and stays ink in every state.
		ImageTexture off = CheckIcon(false), on = CheckIcon(true);
		theme.SetIcon("unchecked", "CheckBox", off);
		theme.SetIcon("checked", "CheckBox", on);
		theme.SetIcon("unchecked_disabled", "CheckBox", off);
		theme.SetIcon("checked_disabled", "CheckBox", on);
		theme.SetFont("font", "CheckBox", sans);
		SetColors("CheckBox", Ink, Ink, Ink, disabledInk,
			"font_color", "font_hover_color", "font_pressed_color", "font_focus_color", "font_hover_pressed_color", "font_disabled_color");
		theme.SetStylebox("focus", "CheckBox", FocusRing());
	}

	private static void BuildFields() {
		// A typed field: ledger-paper fill, a ruled underline, typewriter figures.
		StyleBoxFlat field = Box(Field, Ink, 1, 2, 2, 10, 5);
		StyleBoxFlat focused = Box(Field, Rust, 2, 2, 2, 10, 5);
		StyleBoxFlat readOnly = Box(new Color("eadfc0"), Edge, 1, 2, 1, 10, 5);
		theme.SetFont("font", "LineEdit", typedBold);
		theme.SetFontSize("font_size", "LineEdit", 16);
		theme.SetStylebox("normal", "LineEdit", field);
		theme.SetStylebox("focus", "LineEdit", focused);
		theme.SetStylebox("read_only", "LineEdit", readOnly);
		theme.SetColor("font_color", "LineEdit", Ink);
		theme.SetColor("font_uneditable_color", "LineEdit", Fade);
		theme.SetColor("font_selected_color", "LineEdit", Ink);
		theme.SetColor("font_placeholder_color", "LineEdit", new Color("6b5a3a"));
		theme.SetColor("caret_color", "LineEdit", Ink);
		theme.SetColor("selection_color", "LineEdit", new Color(Highlighter, 0.8f));

		// Spin box: the field above plus stamped up/down triangles. (The inner field takes the LineEdit type.)
		ImageTexture up = Triangle(false, Ink), down = Triangle(true, Ink), upHot = Triangle(false, Rust), downHot = Triangle(true, Rust);
		theme.SetIcon("updown", "SpinBox", UpDown(Ink));
		theme.SetIcon("up", "SpinBox", up); theme.SetIcon("up_hover", "SpinBox", upHot); theme.SetIcon("up_pressed", "SpinBox", upHot); theme.SetIcon("up_disabled", "SpinBox", up);
		theme.SetIcon("down", "SpinBox", down); theme.SetIcon("down_hover", "SpinBox", downHot); theme.SetIcon("down_pressed", "SpinBox", downHot); theme.SetIcon("down_disabled", "SpinBox", down);
		StyleBoxFlat Pad(Color fill) => Box(fill, Edge, 1, 0, 1, 0, 0);
		foreach (string side in new[] { "up", "down" }) {
			theme.SetStylebox(side + "_background", "SpinBox", Pad(new Color("e9d8a8")));
			theme.SetStylebox(side + "_hover_background", "SpinBox", Pad(Highlighter));
			theme.SetStylebox(side + "_pressed_background", "SpinBox", Pad(new Color("e2c95f")));
			theme.SetStylebox(side + "_disabled_background", "SpinBox", Pad(new Color("eadfc0")));
		}
		theme.SetConstant("buttons_width", "SpinBox", 22);
	}

	private static void BuildChoosers() {
		// Drop-down button: the secondary button's face with an ink arrow.
		theme.SetFont("font", "OptionButton", sans);
		theme.SetFontSize("font_size", "OptionButton", 15);
		theme.SetStylebox("normal", "OptionButton", Box(new Color("f0e3bd"), Ink, 2, 3, 3, 12, 6));
		theme.SetStylebox("hover", "OptionButton", Box(Highlighter, Ink, 2, 3, 3, 12, 6));
		theme.SetStylebox("pressed", "OptionButton", Box(new Color("e2c95f"), Ink, 2, 3, 2, 12, 6));
		theme.SetStylebox("disabled", "OptionButton", Box(new Color(Paper, 0.55f), new Color(Ink, 0.35f), 1, 3, 1, 12, 6));
		theme.SetStylebox("focus", "OptionButton", FocusRing());
		SetColors("OptionButton", Ink, Ink, Ink, new Color(Ink, 0.42f),
			"font_color", "font_hover_color", "font_pressed_color", "font_focus_color", "font_hover_pressed_color", "font_disabled_color");
		theme.SetIcon("arrow", "OptionButton", Triangle(true, Ink));
		theme.SetConstant("arrow_margin", "OptionButton", 10);
		theme.SetConstant("h_separation", "OptionButton", 8);
	}

	private static void BuildPopups() {
		// The list a drop-down pulls out: an index card, the current row swiped with the highlighter.
		StyleBoxFlat card = Box(PaperLight, Ink, 2, 2, 2, 6, 6);
		theme.SetStylebox("panel", "PopupMenu", card);
		theme.SetStylebox("hover", "PopupMenu", Box(Highlighter, Highlighter, 0, 2, 0, 8, 4));
		theme.SetStylebox("separator", "PopupMenu", new StyleBoxLine { Color = new Color(Ink, 0.4f), Thickness = 1 });
		theme.SetStylebox("labeled_separator_left", "PopupMenu", new StyleBoxLine { Color = new Color(Ink, 0.4f), Thickness = 1 });
		theme.SetStylebox("labeled_separator_right", "PopupMenu", new StyleBoxLine { Color = new Color(Ink, 0.4f), Thickness = 1 });
		theme.SetFont("font", "PopupMenu", sans);
		theme.SetFontSize("font_size", "PopupMenu", 15);
		SetColors("PopupMenu", Ink, Ink, Ink, new Color(Ink, 0.42f),
			"font_color", "font_hover_color", "font_accelerator_color", "font_disabled_color");
		theme.SetColor("font_separator_color", "PopupMenu", Rust);
		theme.SetConstant("v_separation", "PopupMenu", 8);
		theme.SetConstant("item_start_padding", "PopupMenu", 8);
		theme.SetConstant("item_end_padding", "PopupMenu", 8);
		ImageTexture radioOff = RadioIcon(false), radioOn = RadioIcon(true);
		theme.SetIcon("radio_unchecked", "PopupMenu", radioOff);
		theme.SetIcon("radio_checked", "PopupMenu", radioOn);
		theme.SetIcon("radio_unchecked_disabled", "PopupMenu", radioOff);
		theme.SetIcon("radio_checked_disabled", "PopupMenu", radioOn);
		theme.SetIcon("unchecked", "PopupMenu", CheckIcon(false));
		theme.SetIcon("checked", "PopupMenu", CheckIcon(true));

		// Free-standing popups (the calendar's skip menu) are paper too, not translucent grey.
		theme.SetStylebox("panel", "PopupPanel", Box(PaperLight, Ink, 2, 3, 2, 0, 0));

		// Tooltips: a paper tag, not a grey bubble.
		StyleBoxFlat tip = Box(PaperLight, Rust, 1, 2, 1, 10, 6);
		tip.ShadowColor = new Color(0, 0, 0, 0.3f);
		tip.ShadowSize = 6;
		theme.SetStylebox("panel", "TooltipPanel", tip);
		theme.SetColor("font_color", "TooltipLabel", Ink);
		theme.SetFont("font", "TooltipLabel", sans);
		theme.SetFontSize("font_size", "TooltipLabel", 15);
	}

	private static void BuildScrollBars() {
		// A paper-edge track with a graphite thumb, thin enough to stay out from under the controls it scrolls.
		StyleBoxFlat Bar(Color fill, bool vertical) => new() {
			BgColor = fill,
			CornerRadiusTopLeft = 3, CornerRadiusTopRight = 3, CornerRadiusBottomLeft = 3, CornerRadiusBottomRight = 3,
			ContentMarginLeft = vertical ? 5 : 0, ContentMarginRight = vertical ? 5 : 0,
			ContentMarginTop = vertical ? 0 : 5, ContentMarginBottom = vertical ? 0 : 5
		};
		foreach ((string type, bool vertical) in new[] { ("VScrollBar", true), ("HScrollBar", false) }) {
			theme.SetStylebox("scroll", type, Bar(new Color(0.60f, 0.50f, 0.30f, 0.22f), vertical));
			theme.SetStylebox("scroll_focus", type, Bar(new Color(0.60f, 0.50f, 0.30f, 0.30f), vertical));
			theme.SetStylebox("grabber", type, Bar(new Color("7d6a45"), vertical));
			theme.SetStylebox("grabber_highlight", type, Bar(new Color("5e4d2f"), vertical));
			theme.SetStylebox("grabber_pressed", type, Bar(new Color("3d311d"), vertical));
		}
		theme.SetStylebox("separator", "HSeparator", new StyleBoxLine { Color = new Color("867655"), Thickness = 1 });
		theme.SetConstant("separation", "HSeparator", 10);
	}
}
