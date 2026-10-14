using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// A label's crest, drawn procedurally from its <see cref="LabelBrand"/>: a shape in the brand's ground colour with
/// the monogram lettered on it in the brand's own face. A second mode draws the <b>45 centre label</b> (a disc,
/// the spindle hole, the label's name set on an arc, "45 RPM").
///
/// There is no baked texture: a Control's draw commands are retained until it is told to redraw, so a chart page
/// of thirty crests costs thirty small retained command lists, built once per page, not per frame.
/// <see cref="DrawCrest"/> and <see cref="DrawDisc45"/> are static so a row that already custom-draws (the chart's) can
/// paint a crest into itself without a child node.
/// </summary>
public partial class LabelCrest : Control {
	public enum Mode { Crest, Disc45 }

	private LabelBrand brand = LabelBrand.Derive("");
	private string labelName = "";
	private Mode mode;

	public LabelCrest() {
		MouseFilter = MouseFilterEnum.Ignore;
		SizeFlagsVertical = SizeFlags.ShrinkCenter;
		SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
	}

	/// <summary>Sets what to draw and how big (a square of <paramref name="size"/> pixels).</summary>
	public LabelCrest Set(LabelBrand labelBrand, string name, float size, Mode drawMode = Mode.Crest) {
		brand = labelBrand ?? LabelBrand.Derive("");
		labelName = name ?? "";
		mode = drawMode;
		CustomMinimumSize = new Vector2(size, size);
		QueueRedraw();
		return this;
	}

	public override void _Draw() {
		var rect = new Rect2(Vector2.Zero, Size);
		if (mode == Mode.Disc45) DrawDisc45(this, rect, brand, labelName);
		else DrawCrest(this, rect, brand, labelName);
	}

	// ── The crest ───────────────────────────────────────────────────────────────────────────────

	/// <summary>Paints the crest into the largest square that fits <paramref name="rect"/>, centred in it.</summary>
	public static void DrawCrest(CanvasItem canvas, Rect2 rect, LabelBrand brand, string labelName) {
		LabelBrand.Palette pair = brand.Pair;
		float side = Mathf.Min(rect.Size.X, rect.Size.Y);
		if (side < 4f) return;
		Vector2 origin = rect.Position + (rect.Size - new Vector2(side, side)) / 2f;
		Vector2 P(float x, float y) => origin + new Vector2(x, y) * side;
		Vector2[] Poly(IEnumerable<Vector2> unit) {
			var points = new List<Vector2>();
			foreach (Vector2 u in unit) points.Add(P(u.X, u.Y));
			return points.ToArray();
		}
		float line = Mathf.Max(1f, side * 0.045f);
		void Fill(Vector2[] unit) {
			Vector2[] pts = Poly(unit);
			canvas.DrawColoredPolygon(pts, pair.Ground);
			// The keyline: the same outline pulled in, in the mark colour.
			Vector2 centre = P(0.5f, 0.5f);
			var inner = new Vector2[pts.Length + 1];
			for (int i = 0; i < pts.Length; i++) inner[i] = centre + (pts[i] - centre) * 0.82f;
			inner[^1] = inner[0];
			canvas.DrawPolyline(inner, new Color(pair.Mark, 0.9f), line, true);
		}

		Vector2 textCentre = P(0.5f, 0.5f);
		float textWidth = 0.6f, textHeight = 0.5f;
		switch (brand.Crest) {
			case CrestShape.Shield:
				Fill(ShieldPoints());
				textCentre = P(0.5f, 0.42f); textWidth = 0.5f; textHeight = 0.4f;
				break;
			case CrestShape.Banner:
				Fill(new[] { new Vector2(0.04f, 0.2f), new Vector2(0.96f, 0.2f), new Vector2(0.82f, 0.5f), new Vector2(0.96f, 0.8f), new Vector2(0.04f, 0.8f) });
				textCentre = P(0.45f, 0.5f); textWidth = 0.56f; textHeight = 0.32f;
				break;
			case CrestShape.Burst:
				Fill(StarPoints(16, 0.5f, 0.41f));
				textWidth = 0.54f; textHeight = 0.44f;
				break;
			case CrestShape.Star:
				Fill(StarPoints(5, 0.5f, 0.3f));
				textCentre = P(0.5f, 0.56f); textWidth = 0.32f; textHeight = 0.26f;
				break;
			case CrestShape.Bar:
				canvas.DrawRect(new Rect2(P(0.03f, 0.26f), new Vector2(0.94f, 0.48f) * side), pair.Ground);
				canvas.DrawRect(new Rect2(P(0.08f, 0.32f), new Vector2(0.84f, 0.36f) * side), new Color(pair.Mark, 0.9f), false, line);
				textWidth = 0.74f; textHeight = 0.3f;
				break;
			default: // Roundel
				canvas.DrawCircle(textCentre, side * 0.5f, pair.Ground);
				canvas.DrawArc(textCentre, side * 0.41f, 0f, Mathf.Tau, 48, new Color(pair.Mark, 0.9f), line, true);
				textWidth = 0.58f; textHeight = 0.46f;
				break;
		}
		DrawFitted(canvas, PaperTheme.Lettering(brand.Lettering), brand.MonogramFor(labelName), textCentre,
			textWidth * side, textHeight * side, pair.Mark);
	}

	private static Vector2[] ShieldPoints() {
		var points = new List<Vector2> { new(0.14f, 0.08f), new(0.86f, 0.08f), new(0.86f, 0.5f) };
		Vector2 Bezier(Vector2 a, Vector2 control, Vector2 b, float t) =>
			(1 - t) * (1 - t) * a + 2 * (1 - t) * t * control + t * t * b;
		for (int i = 1; i <= 8; i++) points.Add(Bezier(new Vector2(0.86f, 0.5f), new Vector2(0.86f, 0.8f), new Vector2(0.5f, 0.96f), i / 8f));
		for (int i = 1; i <= 8; i++) points.Add(Bezier(new Vector2(0.5f, 0.96f), new Vector2(0.14f, 0.8f), new Vector2(0.14f, 0.5f), i / 8f));
		return points.ToArray();
	}

	private static Vector2[] StarPoints(int spikes, float outer, float inner) {
		var points = new Vector2[spikes * 2];
		for (int i = 0; i < points.Length; i++) {
			float angle = i * Mathf.Pi / spikes - Mathf.Pi / 2f;
			float radius = i % 2 == 0 ? outer : inner;
			points[i] = new Vector2(0.5f + radius * Mathf.Cos(angle), 0.5f + radius * Mathf.Sin(angle));
		}
		return points;
	}

	/// <summary>Letters <paramref name="text"/> centred on a point, as large as fits inside the given box.</summary>
	private static void DrawFitted(CanvasItem canvas, Font font, string text, Vector2 centre, float maxWidth, float maxHeight, Color color) {
		if (string.IsNullOrEmpty(text)) return;
		int size = Mathf.Max(5, Mathf.RoundToInt(maxHeight));
		Vector2 measure = font.GetStringSize(text, HorizontalAlignment.Left, -1, size);
		while ((measure.X > maxWidth) && size > 5) {
			size--;
			measure = font.GetStringSize(text, HorizontalAlignment.Left, -1, size);
		}
		float baseline = centre.Y + (font.GetAscent(size) - font.GetDescent(size)) / 2f;
		canvas.DrawString(font, new Vector2(centre.X - measure.X / 2f, baseline), text, HorizontalAlignment.Left, -1, size, color);
	}

	// ── The 45 centre label ─────────────────────────────────────────────────────────────────────

	/// <summary>The paper label at the middle of a 45: ring, spindle hole, the name set on an arc across the top,
	/// the monogram and "45 RPM" below the hole.</summary>
	public static void DrawDisc45(CanvasItem canvas, Rect2 rect, LabelBrand brand, string labelName) {
		LabelBrand.Palette pair = brand.Pair;
		float side = Mathf.Min(rect.Size.X, rect.Size.Y);
		if (side < 24f) { DrawCrest(canvas, rect, brand, labelName); return; }
		Vector2 c = rect.Position + rect.Size / 2f;
		float radius = side / 2f;
		float line = Mathf.Max(1f, radius * 0.025f);
		canvas.DrawCircle(c, radius, pair.Ground);
		canvas.DrawArc(c, radius * 0.93f, 0f, Mathf.Tau, 72, new Color(pair.Mark, 0.9f), line, true);
		canvas.DrawArc(c, radius * 0.2f, 0f, Mathf.Tau, 32, new Color(pair.Mark, 0.7f), line, true);
		canvas.DrawCircle(c, radius * 0.075f, new Color("120e0b"));

		// The name, set along the top of the label, glyph by glyph.
		Font face = PaperTheme.Lettering(brand.Lettering);
		string name = brand.DisplayName(string.IsNullOrWhiteSpace(labelName) ? "Records" : labelName);
		float baseRadius = radius * 0.62f;
		float fontSize = radius * (brand.Lettering == LetteringStyle.Script ? 0.3f : 0.2f);
		float arcAvailable = 3.0f * baseRadius;
		float width = face.GetStringSize(name, HorizontalAlignment.Left, -1, Mathf.RoundToInt(fontSize)).X;
		if (width > arcAvailable) fontSize = Mathf.Max(5f, fontSize * arcAvailable / width);
		int size = Mathf.Max(5, Mathf.RoundToInt(fontSize));
		width = face.GetStringSize(name, HorizontalAlignment.Left, -1, size).X;
		float startAngle = -Mathf.Pi / 2f - width / baseRadius / 2f;
		float travelled = 0f;
		foreach (char glyph in name) {
			string ch = glyph.ToString();
			float advance = face.GetStringSize(ch, HorizontalAlignment.Left, -1, size).X;
			float angle = startAngle + (travelled + advance / 2f) / baseRadius;
			canvas.DrawSetTransform(c + baseRadius * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)), angle + Mathf.Pi / 2f, Vector2.One);
			canvas.DrawString(face, new Vector2(-advance / 2f, 0f), ch, HorizontalAlignment.Left, -1, size, pair.Mark);
			travelled += advance;
		}
		canvas.DrawSetTransform(Vector2.Zero, 0f, Vector2.One);

		DrawFitted(canvas, face, brand.MonogramFor(labelName), c + new Vector2(0f, radius * 0.42f), radius * 0.5f, radius * 0.26f, pair.Mark);
		DrawFitted(canvas, PaperTheme.SansBold, "45 RPM", c + new Vector2(0f, radius * 0.72f), radius * 0.5f, radius * 0.11f, new Color(pair.Mark, 0.85f));
	}

	/// <summary>The largest size up to <paramref name="size"/> at which the text fits a width (0 = no limit).</summary>
	private static int FitSize(Font font, string text, int size, float maxWidth) {
		if (maxWidth <= 0f) return size;
		while (size > 10 && font.GetStringSize(text, HorizontalAlignment.Left, -1, size).X > maxWidth) size--;
		return size;
	}

	// ── Letterhead ──────────────────────────────────────────────────────────────────────────────

	/// <summary>The label's name set in its own lettering beside its crest, over a rule in its accent colour.</summary>
	public static Control Letterhead(LabelBrand brand, string labelName, int crestSize = 40, int nameSize = 26, float maxNameWidth = 0f) {
		var box = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		box.AddThemeConstantOverride("separation", 3);
		var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		row.AddThemeConstantOverride("separation", 10);
		row.AddChild(new LabelCrest().Set(brand, labelName, crestSize));
		var name = new Label { Text = brand.DisplayName(labelName), SizeFlagsHorizontal = SizeFlags.ExpandFill, VerticalAlignment = VerticalAlignment.Center, ClipText = true, MouseFilter = MouseFilterEnum.Ignore };
		name.AddThemeFontOverride("font", PaperTheme.Lettering(brand.Lettering));
		name.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
		name.AddThemeFontSizeOverride("font_size", FitSize(PaperTheme.Lettering(brand.Lettering), name.Text, nameSize, maxNameWidth));
		name.AddThemeColorOverride("font_color", brand.Pair.Ink);
		row.AddChild(name);
		box.AddChild(row);
		box.AddChild(new ColorRect { Color = brand.Pair.Accent, CustomMinimumSize = new Vector2(0, 3), MouseFilter = MouseFilterEnum.Ignore });
		return box;
	}
}
