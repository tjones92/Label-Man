using Godot;

/// <summary>
/// The printed work orders the label's paperwork is done on. A pressing plant ran its jobs on three-part NCR sets, white
/// top sheet over yellow over pink, with the form's pre-printed lines in blue, a serial number in red, boxed fields to
/// fill in, and a RUSH stamp for a job that could not wait. The Catalog (press orders), Distribution (route and
/// shipping) and Office (staff and services) are each one of those forms, so the pages that are about work being
/// done read as forms being filled in, not as lists of buttons.
///
/// This file is look only: the sheet, its header, the section bands and the captioned fields. What goes on the form
/// is the page's own controls.
/// </summary>
public static partial class WorkOrder {
	/// <summary>Pre-printed form ink: the blue a job printer ran forms in.</summary>
	public static readonly Color FormInk = new("27407a");
	public static readonly Color SerialRed = new("b02a20");
	public static readonly Color Top = new("f8f4e4"), Canary = new("efe08e"), Pink = new("e9b9b1");

	public sealed class Form {
		public PanelContainer Sheet;
		public VBoxContainer Body;
	}

	/// <summary>The sheet: a top white copy with the yellow and pink copies showing under its lower edge.</summary>
	public partial class SheetStyle : StyleBox {
		public const float CopyDepth = 7f;
		private readonly PaperStyleBox top = new() { Fill = Top, Border = new Color("a9a38a"), BorderWidth = 1, Radius = 1, ShadowSize = 0, Burn = 0.5f, Falloff = 0.4f, Grain = 0.6f };

		public SheetStyle() {
			ContentMarginLeft = 26; ContentMarginRight = 26; ContentMarginTop = 18; ContentMarginBottom = 18 + CopyDepth * 2f;
		}

		public override void _Draw(Rid toCanvasItem, Rect2 rect) {
			var shadow = new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0.0f), ShadowColor = new Color(0, 0, 0, 0.34f), ShadowSize = 9, ShadowOffset = new Vector2(1, 5) };
			shadow.Draw(toCanvasItem, new Rect2(rect.Position, new Vector2(rect.Size.X, rect.Size.Y - CopyDepth * 0.5f)));
			// pink copy, then yellow, each a little narrower and lower than the one over it
			DrawCopy(toCanvasItem, new Rect2(rect.Position.X + 9f, rect.Position.Y + CopyDepth * 2f, rect.Size.X - 18f, rect.Size.Y - CopyDepth * 2f), Pink);
			DrawCopy(toCanvasItem, new Rect2(rect.Position.X + 4f, rect.Position.Y + CopyDepth, rect.Size.X - 8f, rect.Size.Y - CopyDepth * 2f), Canary);
			var sheet = new Rect2(rect.Position, new Vector2(rect.Size.X, rect.Size.Y - CopyDepth * 2f));
			top.Draw(toCanvasItem, sheet);
			// the pre-printed border: a heavy rule and a hairline inside it
			Rect2 inner = sheet.Grow(-8f);
			RenderingServer.CanvasItemAddRect(toCanvasItem, new Rect2(inner.Position, new Vector2(inner.Size.X, 1.6f)), new Color(FormInk, 0.75f));
			RenderingServer.CanvasItemAddRect(toCanvasItem, new Rect2(inner.Position.X, inner.End.Y - 1.6f, inner.Size.X, 1.6f), new Color(FormInk, 0.75f));
			RenderingServer.CanvasItemAddRect(toCanvasItem, new Rect2(inner.Position, new Vector2(1.6f, inner.Size.Y)), new Color(FormInk, 0.75f));
			RenderingServer.CanvasItemAddRect(toCanvasItem, new Rect2(inner.End.X - 1.6f, inner.Position.Y, 1.6f, inner.Size.Y), new Color(FormInk, 0.75f));
			Rect2 hair = sheet.Grow(-11f);
			RenderingServer.CanvasItemAddRect(toCanvasItem, new Rect2(hair.Position, new Vector2(hair.Size.X, 0.8f)), new Color(FormInk, 0.35f));
			RenderingServer.CanvasItemAddRect(toCanvasItem, new Rect2(hair.Position.X, hair.End.Y - 0.8f, hair.Size.X, 0.8f), new Color(FormInk, 0.35f));
			RenderingServer.CanvasItemAddRect(toCanvasItem, new Rect2(hair.Position, new Vector2(0.8f, hair.Size.Y)), new Color(FormInk, 0.35f));
			RenderingServer.CanvasItemAddRect(toCanvasItem, new Rect2(hair.End.X - 0.8f, hair.Position.Y, 0.8f, hair.Size.Y), new Color(FormInk, 0.35f));
		}

		private static void DrawCopy(Rid canvas, Rect2 rect, Color fill) {
			var box = new StyleBoxFlat { BgColor = fill, BorderColor = fill.Darkened(0.35f), BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1 };
			box.Draw(canvas, rect);
		}
	}

	/// <summary>Starts a form: the sheet, and a header with the issuing department on the left, the serial number on the
	/// right and, if the job is urgent or special, a stamp between them.</summary>
	public static Form Begin(string department, Font departmentFont, int departmentSize, string title, string number,
			string stamp = null, Color? stampInk = null, LabelBrand crest = null, string crestName = null, string copyTag = "ORIGINAL") {
		var sheet = new PanelContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		sheet.AddThemeStyleboxOverride("panel", new SheetStyle());
		var body = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		body.AddThemeConstantOverride("separation", 8);
		sheet.AddChild(body);

		var head = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		head.AddThemeConstantOverride("separation", 14);
		body.AddChild(head);
		if (crest != null) head.AddChild(new LabelCrest().Set(crest, crestName ?? "", 42f));
		var issuer = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		issuer.AddThemeConstantOverride("separation", 0);
		var name = new Label { Text = department, ClipText = true, TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis };
		name.AddThemeFontOverride("font", departmentFont);
		name.AddThemeFontSizeOverride("font_size", departmentSize);
		name.AddThemeColorOverride("font_color", FormInk);
		issuer.AddChild(name);
		var kind = new Label { Text = title.ToUpperInvariant() };
		kind.AddThemeFontOverride("font", PaperTheme.SansBold);
		kind.AddThemeFontSizeOverride("font_size", 13);
		kind.AddThemeColorOverride("font_color", new Color(FormInk, 0.85f));
		issuer.AddChild(kind);
		head.AddChild(issuer);

		if (!string.IsNullOrEmpty(stamp)) head.AddChild(new RubberStamp().Set(stamp, stampInk ?? RubberStamp.Red, -0.14f, 22));

		var serial = new VBoxContainer { SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
		serial.AddThemeConstantOverride("separation", 0);
		var no = new Label { Text = "No. " + number, HorizontalAlignment = HorizontalAlignment.Right };
		no.AddThemeFontOverride("font", PaperTheme.TypedBold);
		no.AddThemeFontSizeOverride("font_size", 20);
		no.AddThemeColorOverride("font_color", SerialRed);
		serial.AddChild(no);
		var tag = new Label { Text = copyTag.ToUpperInvariant() + " COPY", HorizontalAlignment = HorizontalAlignment.Right };
		tag.AddThemeFontOverride("font", PaperTheme.SansSemiBold);
		tag.AddThemeFontSizeOverride("font_size", 10);
		tag.AddThemeColorOverride("font_color", new Color(FormInk, 0.7f));
		serial.AddChild(tag);
		head.AddChild(serial);

		body.AddChild(new ColorRect { Color = FormInk, CustomMinimumSize = new Vector2(0, 2.5f), MouseFilter = Control.MouseFilterEnum.Ignore });
		return new Form { Sheet = sheet, Body = body };
	}

	/// <summary>A section of the form: a pale blue band with the heading printed on it and a rule under.</summary>
	public static StyleBoxFlat SectionBand(bool hover = false) => new() {
		BgColor = hover ? new Color("e6ecf6", 0.95f) : new Color("dbe3f0", 0.75f), BorderColor = FormInk, BorderWidthBottom = 1,
		ContentMarginLeft = 8, ContentMarginRight = 8, ContentMarginTop = 3, ContentMarginBottom = 3
	};

	public static Label Section(string text) {
		var label = new Label { Text = text.ToUpperInvariant() };
		label.AddThemeFontOverride("font", PaperTheme.SansBold);
		label.AddThemeFontSizeOverride("font_size", 14);
		label.AddThemeColorOverride("font_color", FormInk);
		label.AddThemeStyleboxOverride("normal", SectionBand());
		return label;
	}

	/// <summary>A field with its printed caption above it. The control is whatever the player fills in (a spin box, a
	/// picker); a typed read-out is <see cref="Typed"/>.</summary>
	public static Control Field(string caption, Control value, float minWidth = 0f, bool expand = false) {
		var box = new VBoxContainer { SizeFlagsHorizontal = expand ? Control.SizeFlags.ExpandFill : Control.SizeFlags.ShrinkBegin };
		box.AddThemeConstantOverride("separation", 1);
		var cap = new Label { Text = caption.ToUpperInvariant() };
		cap.AddThemeFontOverride("font", PaperTheme.SansBold);
		cap.AddThemeFontSizeOverride("font_size", 10);
		cap.AddThemeColorOverride("font_color", new Color(FormInk, 0.85f));
		box.AddChild(cap);
		if (minWidth > 0f) value.CustomMinimumSize = new Vector2(Mathf.Max(minWidth, value.CustomMinimumSize.X), value.CustomMinimumSize.Y);
		value.SizeFlagsHorizontal = expand ? Control.SizeFlags.ExpandFill : value.SizeFlagsHorizontal;
		box.AddChild(value);
		return box;
	}

	/// <summary>A box filled in on the typewriter: the value typed in Courier inside a ruled box.</summary>
	public static Control Typed(string caption, string value, float minWidth = 0f, bool expand = false, Color? ink = null) {
		var panel = new PanelContainer();
		panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat {
			BgColor = new Color("fffdf2"), BorderColor = new Color(FormInk, 0.7f),
			BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
			ContentMarginLeft = 8, ContentMarginRight = 8, ContentMarginTop = 4, ContentMarginBottom = 4
		});
		var text = new Label { Text = value, ClipText = true, TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis, TooltipText = value };
		text.AddThemeFontOverride("font", PaperTheme.Typed);
		text.AddThemeFontSizeOverride("font_size", 16);
		text.AddThemeColorOverride("font_color", ink ?? PaperTheme.Ink);
		panel.AddChild(text);
		return Field(caption, panel, minWidth, expand);
	}

	/// <summary>The serial number printed on a form: stable for the thing it is about.</summary>
	public static string Serial(string key, int digits = 4) {
		uint h = Portraits.Hash(key);
		int mod = digits == 5 ? 90000 : 9000, floor = digits == 5 ? 10000 : 1000;
		return ((int)(h % (uint)mod) + floor).ToString();
	}
}
