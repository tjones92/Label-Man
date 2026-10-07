using System.Collections.Generic;
using Godot;

/// <summary>
/// The material of the office's paper. The painted desk has grain, scorch and lamp falloff; a flat
/// <see cref="StyleBoxFlat"/> beside it reads as a glowing screen. This stylebox lays the same four things
/// over any card: a soft contact shadow, a lamp gradient (bright upper left, where the lamp is), fibre grain,
/// and a burn along the edges. Drop it in wherever a card used a flat box:
/// <c>panel.AddThemeStyleboxOverride("panel", PaperStyleBox.Sheet(PaperTheme.Paper))</c>.
///
/// Everything is drawn with the RenderingServer straight onto the host's canvas item, from three small
/// textures built once (<see cref="PaperTextures"/>), so a sheet costs a handful of draw calls and no nodes.
/// </summary>
public partial class PaperStyleBox : StyleBox {
	public Color Fill = PaperTheme.Paper;
	public Color Border = new("8c6f38");
	public int BorderWidth = 1;
	public int Radius = 2;
	public int ShadowSize = 14;
	public float ShadowAlpha = 0.45f;
	public Vector2 ShadowOffset = new(2, 6);
	/// <summary>0..1 strength of each layer; 0 switches it off.</summary>
	public float Grain = 1f, Burn = 1f, Falloff = 1f;
	/// <summary>An index card: a red rule this far below the top, then blue ruled lines under it. 0 = plain paper.</summary>
	public float HeaderRule;
	public float RuleSpacing = 24f;
	/// <summary>Things that have happened to this sheet: a steel paper clip on the top edge, a coffee mug's ring.
	/// <see cref="DecalSeed"/> picks the corner and the angle, so a given sheet always wears them the same way.</summary>
	public bool Clip, Ring;
	public int DecalSeed;

	private StyleBoxFlat baseBox;

	public override void _Draw(Rid toCanvasItem, Rect2 rect) {
		baseBox ??= new StyleBoxFlat();
		baseBox.BgColor = Fill;
		baseBox.BorderColor = Border;
		baseBox.SetBorderWidthAll(BorderWidth);
		baseBox.SetCornerRadiusAll(Radius);
		baseBox.ShadowColor = new Color(0, 0, 0, ShadowSize > 0 ? ShadowAlpha : 0f);
		baseBox.ShadowSize = ShadowSize;
		baseBox.ShadowOffset = ShadowOffset;
		baseBox.Draw(toCanvasItem, rect);

		Rect2 inner = rect.Grow(-BorderWidth);
		if (inner.Size.X < 8 || inner.Size.Y < 8) return;
		if (HeaderRule > 0f) {
			float y = inner.Position.Y + HeaderRule;
			RenderingServer.CanvasItemAddLine(toCanvasItem, new Vector2(inner.Position.X, y), new Vector2(inner.End.X, y), new Color("b5483a", 0.85f), 2f, true);
			for (y += RuleSpacing; y < inner.End.Y - 4f; y += RuleSpacing)
				RenderingServer.CanvasItemAddLine(toCanvasItem, new Vector2(inner.Position.X, y), new Vector2(inner.End.X, y), new Color("9db8cc", 0.42f), 1f, true);
		}
		if (Falloff > 0f)
			RenderingServer.CanvasItemAddTextureRect(toCanvasItem, inner, PaperTextures.Falloff.GetRid(), false, new Color(1, 1, 1, Falloff));
		if (Grain > 0f) {
			Rid grain = PaperTextures.Grain.GetRid();
			Color tint = new(1, 1, 1, Grain);
			const float tile = PaperTextures.GrainSize;
			for (float y = inner.Position.Y; y < inner.End.Y; y += tile)
				for (float x = inner.Position.X; x < inner.End.X; x += tile) {
					float w = Mathf.Min(tile, inner.End.X - x), h = Mathf.Min(tile, inner.End.Y - y);
					RenderingServer.CanvasItemAddTextureRectRegion(toCanvasItem, new Rect2(x, y, w, h), grain, new Rect2(0, 0, w, h), tint);
				}
		}
		if (Burn > 0f)
			RenderingServer.CanvasItemAddNinePatch(toCanvasItem, inner, new Rect2(0, 0, PaperTextures.BurnSize, PaperTextures.BurnSize),
				PaperTextures.Burn.GetRid(), new Vector2(PaperTextures.BurnMargin, PaperTextures.BurnMargin),
				new Vector2(PaperTextures.BurnMargin, PaperTextures.BurnMargin), RenderingServer.NinePatchAxisMode.Stretch,
				RenderingServer.NinePatchAxisMode.Stretch, false, new Color(1, 1, 1, Burn));
		if (Ring) DrawRing(toCanvasItem, inner);
		if (Clip) DrawClip(toCanvasItem, inner);
	}

	private static float Wobble(int seed, int i) => ((Portraits.Hash($"decal{seed}:{i}") % 200u) / 100f - 1f);

	/// <summary>A mug stood on the sheet: a broken ring in coffee brown, heavier on one side where the rim sat, with a
	/// fainter second ring and a few spatters.</summary>
	private void DrawRing(Rid canvas, Rect2 inner) {
		bool right = Wobble(DecalSeed, 0) > 0f;
		float radius = 64f + Wobble(DecalSeed, 1) * 8f;
		Vector2 centre = new(right ? inner.End.X - radius - 34f : inner.Position.X + radius + 34f, inner.End.Y - radius - 30f);
		float start = Wobble(DecalSeed, 2) * Mathf.Pi, sweep = Mathf.Pi * (1.55f + Wobble(DecalSeed, 3) * 0.2f);
		for (int pass = 0; pass < 2; pass++) {
			var points = new Vector2[41];
			for (int i = 0; i < points.Length; i++) {
				float a = start + sweep * i / (points.Length - 1);
				float r = radius + pass * 6f + Wobble(DecalSeed + pass * 7, 10 + i) * 1.4f;
				points[i] = centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
			}
			Color coffee = pass == 0 ? new Color(0.36f, 0.21f, 0.07f, 0.36f) : new Color(0.36f, 0.21f, 0.07f, 0.17f);
			var colours = new Color[points.Length];
			for (int i = 0; i < colours.Length; i++) colours[i] = coffee;
			RenderingServer.CanvasItemAddPolyline(canvas, points, colours, pass == 0 ? 5f : 2.4f, true);
		}
		RenderingServer.CanvasItemAddCircle(canvas, centre, radius - 3f, new Color(0.36f, 0.21f, 0.07f, 0.05f));
		for (int i = 0; i < 4; i++) {
			float a = start + Wobble(DecalSeed, 30 + i) * 3f, d = radius + 12f + (Wobble(DecalSeed, 40 + i) + 1f) * 11f;
			RenderingServer.CanvasItemAddCircle(canvas, centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * d, 2.2f + (Wobble(DecalSeed, 50 + i) + 1f) * 1.4f, new Color(0.36f, 0.21f, 0.07f, 0.32f));
		}
	}

	/// <summary>A gem paper clip: three nested loops of steel wire with a shadow beneath and a glint along the wire,
	/// pinned over the top edge at a slight angle.</summary>
	private void DrawClip(Rid canvas, Rect2 inner) {
		bool left = Wobble(DecalSeed, 60) > 0f;
		float angle = (left ? 1f : -1f) * (0.04f + (Wobble(DecalSeed, 61) + 1f) * 0.05f);   // the foot leans out toward the edge
		Vector2 origin = new(left ? inner.Position.X + 5f : inner.End.X - 29f, inner.Position.Y - 10f);
		const float scale = 1.35f;
		var path = new List<Vector2>();
		void Arc(float cx, float cy, float r, float a0, float a1) {
			for (int i = 0; i <= 8; i++) { float a = a0 + (a1 - a0) * i / 8f; path.Add(new Vector2(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r)); }
		}
		path.Add(new Vector2(3, 40)); path.Add(new Vector2(3, 9));
		Arc(9, 9, 6, Mathf.Pi, Mathf.Tau);
		path.Add(new Vector2(15, 44));
		Arc(10.5f, 44, 4.5f, 0f, Mathf.Pi);
		path.Add(new Vector2(6, 18));
		Arc(9, 18, 3, Mathf.Pi, Mathf.Tau);
		path.Add(new Vector2(12, 38));
		float cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);
		Vector2 Place(Vector2 p, Vector2 shift) => origin + shift + new Vector2((p.X * cos - p.Y * sin) * scale, (p.X * sin + p.Y * cos) * scale);
		for (int layer = 0; layer < 3; layer++) {
			var points = new Vector2[path.Count];
			Vector2 shift = layer == 0 ? new Vector2(1.8f, 2.6f) : layer == 2 ? new Vector2(-0.6f, -0.6f) : Vector2.Zero;
			for (int i = 0; i < points.Length; i++) points[i] = Place(path[i], shift);
			Color wire = layer == 0 ? new Color(0, 0, 0, 0.28f) : layer == 1 ? new Color(0.58f, 0.60f, 0.64f) : new Color(0.93f, 0.94f, 0.96f, 0.85f);
			var colours = new Color[points.Length];
			for (int i = 0; i < colours.Length; i++) colours[i] = wire;
			RenderingServer.CanvasItemAddPolyline(canvas, points, colours, layer == 2 ? 0.8f : 2.3f, true);
		}
	}

	/// <summary>Puts a paper clip and/or a coffee ring on this sheet.</summary>
	public PaperStyleBox Decorated(bool clip, bool ring, int seed) { Clip = clip; Ring = ring; DecalSeed = seed; return this; }

	/// <summary>A sheet of paper with the standard margins.</summary>
	public static PaperStyleBox Sheet(Color fill, int marginX = 26, int marginY = 22, int shadow = 14, Color? border = null) {
		var box = new PaperStyleBox { Fill = fill, ShadowSize = shadow };
		if (border != null) box.Border = border.Value;
		box.ContentMarginLeft = marginX; box.ContentMarginRight = marginX;
		box.ContentMarginTop = marginY; box.ContentMarginBottom = marginY;
		return box;
	}
}

/// <summary>The three bitmaps behind <see cref="PaperStyleBox"/>, generated once and deterministic.</summary>
public static class PaperTextures {
	public const int GrainSize = 256;
	public const int BurnSize = 96, BurnMargin = 30;
	private const int FalloffSize = 64;

	private static ImageTexture grain, burn, falloff, felt;

	/// <summary>Billiard-table baize: a dark green nap with brighter and darker fibres, tiling seamlessly.</summary>
	public static ImageTexture Felt { get { return felt ??= BuildFelt(); } }

	private static ImageTexture BuildFelt() {
		var rng = new RandomNumberGenerator { Seed = 1961 };
		const int n = 192;
		var image = Image.CreateEmpty(n, n, false, Image.Format.Rgba8);
		for (int y = 0; y < n; y++)
			for (int x = 0; x < n; x++) {
				float nap = (rng.Randf() - 0.5f) * 0.075f;
				image.SetPixel(x, y, new Color(0.145f + nap * 0.7f, 0.255f + nap, 0.185f + nap * 0.8f));
			}
		for (int i = 0; i < 520; i++) {
			float x = rng.Randf() * n, y = rng.Randf() * n, angle = rng.Randf() * Mathf.Tau;
			int length = rng.RandiRange(3, 9);
			bool light = rng.Randf() > 0.45f;
			Color fibre = light ? new Color(0.45f, 0.62f, 0.48f, 0.16f) : new Color(0.03f, 0.08f, 0.05f, 0.22f);
			for (int s = 0; s < length; s++) {
				int px = ((int)(x + Mathf.Cos(angle) * s) % n + n) % n, py = ((int)(y + Mathf.Sin(angle) * s) % n + n) % n;
				image.SetPixel(px, py, image.GetPixel(px, py).Blend(fibre));
			}
		}
		return ImageTexture.CreateFromImage(image);
	}

	/// <summary>Fine speckle, a few drifting blotches (foxing) and short fibres; tiles seamlessly.</summary>
	public static ImageTexture Grain { get { return grain ??= BuildGrain(); } }
	/// <summary>A nine-patch ring: clear in the middle, scorched brown toward the edge.</summary>
	public static ImageTexture Burn { get { return burn ??= BuildBurn(); } }
	/// <summary>Lamp light from the upper left: a warm lift near it, a cool-dark fall toward the far corner.</summary>
	public static ImageTexture Falloff { get { return falloff ??= BuildFalloff(); } }

	private static ImageTexture BuildGrain() {
		var rng = new RandomNumberGenerator { Seed = 1960 };
		const int n = GrainSize;
		var image = Image.CreateEmpty(n, n, false, Image.Format.Rgba8);
		// Blotches: an 8x8 lattice of soft values, bilinearly wrapped.
		var lattice = new float[8, 8];
		for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++) lattice[x, y] = rng.Randf();
		for (int y = 0; y < n; y++)
			for (int x = 0; x < n; x++) {
				float fx = x / (float)n * 8f, fy = y / (float)n * 8f;
				int x0 = (int)fx, y0 = (int)fy, x1 = (x0 + 1) % 8, y1 = (y0 + 1) % 8;
				float tx = fx - x0, ty = fy - y0;
				float blot = Mathf.Lerp(Mathf.Lerp(lattice[x0, y0], lattice[x1, y0], tx), Mathf.Lerp(lattice[x0, y1], lattice[x1, y1], tx), ty);
				float speck = rng.Randf() * 2f - 1f;
				float a = speck >= 0f ? speck * speck * 0.05f : speck * speck * 0.09f;   // few dark specks, finer light ones
				Color c = speck >= 0f ? new Color(1f, 0.97f, 0.88f, a) : new Color(0.24f, 0.15f, 0.06f, a);
				// foxing: a faint brown lift where the lattice is high
				if (blot > 0.62f) c = c.Blend(new Color(0.55f, 0.36f, 0.14f, (blot - 0.62f) * 0.16f));
				image.SetPixel(x, y, c);
			}
		for (int i = 0; i < 120; i++) {
			float x = rng.Randf() * n, y = rng.Randf() * n, angle = rng.Randf() * Mathf.Tau;
			int length = rng.RandiRange(5, 15);
			Color fibre = new(0.43f, 0.30f, 0.15f, rng.RandfRange(0.07f, 0.15f));
			for (int s = 0; s < length; s++) {
				int px = ((int)(x + Mathf.Cos(angle) * s) % n + n) % n, py = ((int)(y + Mathf.Sin(angle) * s) % n + n) % n;
				image.SetPixel(px, py, image.GetPixel(px, py).Blend(fibre));
			}
		}
		return ImageTexture.CreateFromImage(image);
	}

	private static ImageTexture BuildBurn() {
		var image = Image.CreateEmpty(BurnSize, BurnSize, false, Image.Format.Rgba8);
		Color scorch = new(0.24f, 0.13f, 0.04f);
		for (int y = 0; y < BurnSize; y++)
			for (int x = 0; x < BurnSize; x++) {
				float dx = Mathf.Min(x, BurnSize - 1 - x), dy = Mathf.Min(y, BurnSize - 1 - y);
				float ax = 1f - Mathf.SmoothStep(0f, BurnMargin, dx), ay = 1f - Mathf.SmoothStep(0f, BurnMargin, dy);
				float a = 1f - (1f - ax * ax) * (1f - ay * ay);   // corners take both edges
				image.SetPixel(x, y, new Color(scorch, a * 0.24f));
			}
		return ImageTexture.CreateFromImage(image);
	}

	private static ImageTexture BuildFalloff() {
		var image = Image.CreateEmpty(FalloffSize, FalloffSize, false, Image.Format.Rgba8);
		for (int y = 0; y < FalloffSize; y++)
			for (int x = 0; x < FalloffSize; x++) {
				float u = x / (FalloffSize - 1f), v = y / (FalloffSize - 1f);
				float d = Mathf.Clamp(Mathf.Sqrt((u + 0.15f) * (u + 0.15f) + (v + 0.2f) * (v + 0.2f)) / 1.35f, 0f, 1f);
				Color c = d < 0.4f
					? new Color(1f, 0.92f, 0.72f, 0.13f * (1f - d / 0.4f))
					: new Color(0.10f, 0.06f, 0.02f, 0.20f * Mathf.Pow((d - 0.4f) / 0.6f, 1.3f));
				image.SetPixel(x, y, c);
			}
		return ImageTexture.CreateFromImage(image);
	}
}

/// <summary>
/// A real folder tab: a trapezoid with sloped shoulders instead of a rounded rectangle. The open tab is the
/// colour of the card beneath it and reaches down over the card's top edge so the two read as one sheet; the
/// others sit a little lower in manila shade. Drawn for the tab row's <see cref="Button"/>s in every state.
/// </summary>
public partial class FolderTabStyle : StyleBox {
	public Color Fill = PaperTheme.Paper;
	public Color Border = new("70552c");
	public bool Active;
	public const float Slope = 12f, Drop = 6f;
	/// <summary>How far the open tab reaches below its own rectangle, to cover the gap and the card's top rule.</summary>
	public float Bridge = 8f;
	/// <summary>0..2. A closed tab sits 3px lower per step, so a row of them is cut at three heights like a filing folder.</summary>
	public int Stagger;
	/// <summary>A typed label stuck on the tab; <see cref="Tilt"/> is how crooked it went on (radians).</summary>
	public bool Sticker = true;
	public float Tilt;

	public float DropFor => Drop - 3f + Stagger * 3f;

	public static FolderTabStyle Make(Color fill, bool active, float bridge = 8f, int stagger = 0) {
		var style = new FolderTabStyle { Fill = fill, Active = active, Bridge = bridge, Stagger = Mathf.Clamp(stagger, 0, 2) };
		style.Tilt = (style.Stagger - 1) * 0.006f + (active ? 0.004f : 0f);
		style.ContentMarginLeft = Slope + 6; style.ContentMarginRight = Slope + 6;
		style.ContentMarginTop = 5 + (active ? 0 : style.DropFor); style.ContentMarginBottom = 5;
		return style;
	}

	public override void _Draw(Rid toCanvasItem, Rect2 rect) {
		float left = rect.Position.X, right = rect.End.X;
		float top = rect.Position.Y + (Active ? 0f : DropFor), bottom = rect.End.Y + (Active ? Bridge : 0f);
		const float s = Slope;
		var points = new Vector2[] {
			new(left, bottom), new(left + s - 3, top + 7), new(left + s + 1, top + 2), new(left + s + 5, top),
			new(right - s - 5, top), new(right - s - 1, top + 2), new(right - s + 3, top + 7), new(right, bottom)
		};
		var colors = new Color[points.Length];
		for (int i = 0; i < colors.Length; i++) colors[i] = Fill;
		RenderingServer.CanvasItemAddPolygon(toCanvasItem, points, colors);
		// Shoulders and top only for the open tab (it opens into the card); a closed outline for the rest.
		var outline = new Vector2[Active ? points.Length : points.Length + 1];
		for (int i = 0; i < points.Length; i++) outline[i] = points[i];
		if (!Active) outline[^1] = points[0];
		var lineColors = new Color[outline.Length];
		for (int i = 0; i < lineColors.Length; i++) lineColors[i] = Border;
		RenderingServer.CanvasItemAddPolyline(toCanvasItem, outline, lineColors, 1.5f, true);
		if (Sticker) DrawSticker(toCanvasItem, new Rect2(left + s + 1f, top + 7f, rect.Size.X - 2f * (s + 1f), Mathf.Max(8f, rect.End.Y - 4f - (top + 7f))));
	}

	/// <summary>The typed label: a cream slip with a hairline edge and a drop of shadow, put on a hair crooked.</summary>
	private void DrawSticker(Rid canvas, Rect2 slip) {
		Vector2 centre = slip.GetCenter();
		float cos = Mathf.Cos(Tilt), sin = Mathf.Sin(Tilt);
		Vector2 Corner(float dx, float dy, float lift = 0f) {
			Vector2 d = new(dx * slip.Size.X / 2f, dy * slip.Size.Y / 2f);
			return centre + new Vector2(d.X * cos - d.Y * sin, d.X * sin + d.Y * cos) + new Vector2(lift, lift);
		}
		var shadow = new[] { Corner(-1, -1, 1.2f), Corner(1, -1, 1.2f), Corner(1, 1, 1.2f), Corner(-1, 1, 1.2f) };
		RenderingServer.CanvasItemAddPolygon(canvas, shadow, new[] { new Color(0, 0, 0, 0.16f), new Color(0, 0, 0, 0.16f), new Color(0, 0, 0, 0.16f), new Color(0, 0, 0, 0.16f) });
		Color slipColour = Active ? new Color("fbf4df") : new Color("f0e4c2");
		var face = new[] { Corner(-1, -1), Corner(1, -1), Corner(1, 1), Corner(-1, 1) };
		RenderingServer.CanvasItemAddPolygon(canvas, face, new[] { slipColour, slipColour, slipColour, slipColour });
		var edge = new[] { face[0], face[1], face[2], face[3], face[0] };
		Color hairline = new(0.44f, 0.33f, 0.17f, 0.55f);
		RenderingServer.CanvasItemAddPolyline(canvas, edge, new[] { hairline, hairline, hairline, hairline, hairline }, 1f, true);
	}
}
