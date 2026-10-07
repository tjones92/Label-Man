using System;
using System.Linq;
using Godot;

/// <summary>
/// Draws only PolarComparisonRead. Geometry, colors, arrows and prose have no truth access.
///
/// The whole read is one sheet of graph paper pinned to the page, worked in pencil: graphite for the song as heard,
/// blue pencil for the arrangement or performance, green for your read of the act, and red grease pencil only where
/// the song reaches past what the act can do. Fit is a row of indicator lamps: lit to the best guess, glowing dimly
/// through the doubt, dark beyond it. The bezel is the series' pencil colour.
/// </summary>
public partial class PolarComparisonWidget : Control {
	private const float PlateWidth = 890, PlateHeight = 430;
	private const float Plane = 224;   // the identity box: 220px or more, so a dot and its doubt box can be told apart
	private const int Lamps = 10;

	private PolarComparisonRead read;
	private static readonly Color Ink = PaperTheme.Ink, Muted = new("6a5c42");
	private static readonly Color Reference = GreasePencil.Graphite, Proposed = GreasePencil.BluePencil, ActInk = GreasePencil.GreenPencil, Stretch = GreasePencil.RedGrease;
	private Font font;

	public void SetRead(PolarComparisonRead value) { read = value; QueueRedraw(); }
	public override void _Ready() {
		CustomMinimumSize = new Vector2(PlateWidth, PlateHeight);
		SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
		MouseFilter = MouseFilterEnum.Ignore;
		font = PaperTheme.SansSemiBold;
	}

	public override void _Draw() {
		if (read == null || font == null) return;
		GreasePencil.Plate(this, new Rect2(0, 0, PlateWidth, PlateHeight));
		if (read.source != PolarHearingSource.Studio) { DrawEar(); return; }
		var center = new Vector2(196, 196); const float radius = 92;
		RadarFrame(center, radius);
		BandRing(read.reference.axes, center, radius, Reference, 0.13f, 11);
		BandRing(read.proposed.axes, center, radius, Proposed, 0.17f, 23);
		ActRead(center, radius);
		StretchMarks(read.proposed.axes, center, radius, 6);
		Title("DEMAND / CAPABILITY", new Vector2(34, 36));
		Legend(new Vector2(34, 346), ("Your read", ActInk, true), ("Heard material", Reference, false), (read.isRecorded ? "Performance" : "Arrangement", Proposed, false));
		Label("Shaded bands show uncertainty.", new Vector2(34, 396), Muted, 12);

		var plane = new Rect2(376, 70, Plane, Plane);
		Title("IDENTITY PROJECTION", new Vector2(376, 36));
		IdentityPlane(plane);
		Label("Sophisticated", new Vector2(plane.Position.X, plane.Position.Y - 8), Muted, 12);
		Label("Earthy", new Vector2(plane.Position.X, plane.End.Y + 18), Muted, 12);
		Label("Gentle → tough", new Vector2(plane.End.X - font.GetStringSize("Gentle → tough", fontSize: 12).X, plane.End.Y + 18), Muted, 12);
		IdentityDot(read.act.axes, plane, ActInk);
		var from = IdentityDot(read.reference.axes, plane, Reference);
		var to = IdentityDot(read.proposed.axes, plane, Proposed);
		var vector = to - from;
		if (vector.Length() > 3) {
			GreasePencil.Line(this, from, to, new Color(Proposed, .85f), 2.2f, 41);
			var normal = vector.Normalized(); var side = new Vector2(-normal.Y, normal.X);
			GreasePencil.Line(this, to, to - normal * 10 + side * 5, Proposed, 2.2f, 43);
			GreasePencil.Line(this, to, to - normal * 10 - side * 5, Proposed, 2.2f, 47);
		}
		Label("Arrow: perceived arrangement pull", new Vector2(plane.Position.X, plane.End.Y + 38), Muted, 12);
		Label("Full Identity also reads sincerity", new Vector2(plane.Position.X, plane.End.Y + 56), Muted, 12);
		Label("and maturity.", new Vector2(plane.Position.X, plane.End.Y + 72), Muted, 12);

		const float fitX = 632;
		Title("PERCEIVED FIT", new Vector2(fitX - 8, 36));
		string[] fitNames = { "Capability", "Identity", "Moment" };
		for (int i = 0; i < 3; i++) {
			float y = 76 + i * 76;
			Label(fitNames[i], new Vector2(fitX - 8, y), Ink, 14);
			DrawLamps(read.referenceFit[i], fitX, y + 20, 22, 6f, Reference);
			DrawLamps(read.proposedFit[i], fitX, y + 44, 22, 8f, Proposed);
		}
		LowHigh(fitX, 22, 318);
		Label("Heard material / arrangement", new Vector2(fitX - 8, 342), Muted, 12);
		Label(read.hasMarketEvidence ? "Moment remains a market forecast." : "Moment: no recent market evidence.", new Vector2(fitX - 8, 360), Muted, 12);
		Label(read.isRecorded ? "PRINTED MASTER · playback estimate" : "ARRANGEMENT PREVIEW · estimated performance", new Vector2(34, PlateHeight - 16), Muted, 12);
	}

	/// <summary>Away from the studio: the song as heard, the hook, and -- if you watched them -- your read of the act.
	/// No identity projection, no fit, no pushback; those belong to the studio.</summary>
	private void DrawEar() {
		var center = new Vector2(196, 196); const float radius = 92;
		RadarFrame(center, radius);
		BandRing(read.heard.axes, center, radius, Reference, 0.18f, 11);
		if (read.showsAct) { ActRead(center, radius); StretchMarks(read.heard.axes, center, radius, (int)SongAxis.StudioCraft); }
		Title(read.source switch { PolarHearingSource.Venue => "AS THEY PLAYED IT", PolarHearingSource.Playback => "THE PRESSING",
			_ => read.fromSheetMusic ? "AS WRITTEN" : "ON THE RECORD" }, new Vector2(34, 36));
		string heardName = read.fromSheetMusic ? "The song as you read it" : "What you heard";
		if (read.showsAct) Legend(new Vector2(34, 346), (heardName, Reference, false), ("Your read of them", ActInk, true));
		else Legend(new Vector2(34, 346), (heardName, Reference, false));
		Label("Shaded bands show uncertainty.", new Vector2(34, 396), Muted, 12);

		const float midX = 376;
		Title("YOUR EAR", new Vector2(midX, 36));
		Label("Hook", new Vector2(midX, 76), Ink, 14);
		DrawLamps(read.hook, midX + 6, 98, 22, 8.5f, Reference);
		Label(read.hookText ?? "", new Vector2(midX, 134), Ink, 13);
		if (read.showsAct) {
			Label("Tightness", new Vector2(midX, 176), Ink, 14);
			DrawLamps(read.act.axes[(int)SongAxis.Ensemble], midX + 6, 198, 22, 8.5f, ActInk);
			Label(read.tightnessText ?? "", new Vector2(midX, 234), Ink, 13);
		}
		LowHigh(midX + 6, 22, 292);
		Label("Your ear, not the truth.", new Vector2(midX, 326), Muted, 12);
		Label("A better scout narrows the read.", new Vector2(midX, 344), Muted, 12);

		if (read.showsFit && read.referenceFit != null) {
			const float fitX = 632;
			Title($"FIT WITH {read.actName.ToUpperInvariant()}", new Vector2(fitX - 8, 36));
			string[] fitNames = { "Capability", "Identity" };
			for (int i = 0; i < 2; i++) {
				float y = 76 + i * 80;
				Label(fitNames[i], new Vector2(fitX - 8, y), Ink, 14);
				DrawLamps(read.referenceFit[i], fitX, y + 22, 22, 8f, ActInk);
			}
			LowHigh(fitX, 22, 292);
			Label("Arrangement, moment and pushback", new Vector2(fitX - 8, 326), Muted, 12);
			Label("are read in the studio.", new Vector2(fitX - 8, 344), Muted, 12);
		}
		Label(read.source switch { PolarHearingSource.Venue => "HEARD LIVE · ear read", PolarHearingSource.Playback => "PLAYBACK · ear read",
			_ => read.fromSheetMusic ? "SHEET MUSIC · ear read" : "HEARD ON RECORD · ear read" }, new Vector2(34, PlateHeight - 16), Muted, 12);
	}

	private void RadarFrame(Vector2 center, float radius) {
		for (int ring = 1; ring <= 4; ring++)
			DrawPolyline(Radar(null, center, radius * ring / 4), new Color(Muted, ring == 4 ? .5f : .3f), 1, true);
		string[] names = { "Vocal power", "Vocal nuance", "Musicianship", "Ensemble", "Lyric delivery", "Studio craft" };
		for (int i = 0; i < 6; i++) {
			DrawLine(center, Spoke(center, radius, i), new Color(Muted, .34f), 1, true);
			// Names sit outside the rim on their own side, so a long one never runs back across the polygon.
			Vector2 outward = Spoke(Vector2.Zero, 1f, i), at = Spoke(center, radius + 10, i);
			float width = font.GetStringSize(names[i], fontSize: 12).X;
			Vector2 origin = outward.X > 0.3f ? at + new Vector2(2, 4) : outward.X < -0.3f ? at + new Vector2(-width - 2, 4)
				: new Vector2(at.X - width / 2, outward.Y < 0 ? at.Y - 4 : at.Y + 13);
			Label(names[i], origin, Muted, 12);
		}
	}

	/// <summary>The act is a read, never a fact: a faint band with a dashed green best guess through it.</summary>
	private void ActRead(Vector2 center, float radius) {
		BandRing(read.act.axes, center, radius, ActInk, .10f, 31, band: false);
		var guess = new Vector2[7];
		for (int i = 0; i < 6; i++) guess[i] = Spoke(center, radius * read.act.axes[i].Center, i);
		guess[6] = guess[0];
		for (int i = 0; i < 6; i++) GreasePencil.Dashed(this, guess[i], guess[i + 1], ActInk, 2.4f, 7f, 5f);
	}

	/// <summary>Where the song reaches past what the act can do: a heavy red stroke along the spoke.</summary>
	private void StretchMarks(PolarBand[] song, Vector2 center, float radius, int axes) {
		for (int i = 0; i < axes; i++)
			if (song[i].lo > read.act.axes[i].hi)
				GreasePencil.Line(this, Spoke(center, radius * read.act.axes[i].hi, i), Spoke(center, radius * song[i].lo, i), new Color(Stretch, .95f), 5f, 71 + i);
	}

	/// <summary>A pencil stroke as a key, then the name in ink, one per line: series colours alone are not enough to read as text.</summary>
	private void Legend(Vector2 at, params (string Text, Color Color, bool Dashed)[] items) {
		foreach (var (text, color, dashed) in items) {
			if (dashed) GreasePencil.Dashed(this, new Vector2(at.X, at.Y - 4), new Vector2(at.X + 18, at.Y - 4), color, 2.6f, 5f, 3f);
			else GreasePencil.Line(this, new Vector2(at.X, at.Y - 4), new Vector2(at.X + 18, at.Y - 4), color, 3f, 5);
			Label(text, new Vector2(at.X + 24, at.Y), Ink, 12);
			at.Y += 16;
		}
	}

	private void Title(string text, Vector2 at) => DrawString(PaperTheme.SansBold, at, text, fontSize: 14, modulate: Ink);
	private void Label(string text, Vector2 at, Color color, int size) => DrawString(font, at, text, fontSize: size, modulate: color);
	private Vector2 Spoke(Vector2 center, float radius, int axis) { float angle = -Mathf.Pi / 2 + axis * Mathf.Tau / 6; return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius; }
	private Vector2[] Radar(PolarBand[] bands, Vector2 center, float radius, bool high = true) {
		var points = new Vector2[7]; for (int i = 0; i < 6; i++) points[i] = Spoke(center, radius * (bands == null ? 1 : high ? bands[i].hi : bands[i].lo), i); points[6] = points[0]; return points;
	}

	/// <summary>The doubt around a radar read: a wash between the low and high rings, a firm pencil edge on the high ring
	/// and a lighter one on the low. <paramref name="band"/> false draws the wash only.</summary>
	private void BandRing(PolarBand[] bands, Vector2 center, float radius, Color color, float wash, int seed, bool band = true) {
		var low = Radar(bands, center, radius, false); var high = Radar(bands, center, radius, true);
		for (int i = 0; i < 6; i++) {
			var polygon = new[] { low[i], high[i], high[i + 1], low[i + 1] }.Distinct().ToArray();
			if (polygon.Length >= 3) DrawColoredPolygon(polygon, new Color(color, wash));
		}
		if (!band) return;
		GreasePencil.Path(this, high, new Color(color, .95f), 2.4f, seed);
		GreasePencil.Path(this, low, new Color(color, .55f), 1.5f, seed + 5);
	}

	/// <summary>The identity box: a ruled square, a dashed cross through the middle, no fill (the paper is the fill).</summary>
	private void IdentityPlane(Rect2 plane) {
		var corners = new[] { plane.Position, new Vector2(plane.End.X, plane.Position.Y), plane.End, new Vector2(plane.Position.X, plane.End.Y) };
		GreasePencil.Outline(this, corners, new Color(Ink, .7f), 1.8f, 3);
		GreasePencil.Dashed(this, plane.Position + new Vector2(0, Plane / 2), plane.Position + new Vector2(Plane, Plane / 2), new Color(Muted, .55f), 1.2f, 5f, 4f);
		GreasePencil.Dashed(this, plane.Position + new Vector2(Plane / 2, 0), plane.Position + new Vector2(Plane / 2, Plane), new Color(Muted, .55f), 1.2f, 5f, 4f);
	}

	private Vector2 IdentityDot(PolarBand[] axes, Rect2 plane, Color color) {
		var x = axes[(int)SongAxis.Toughness]; var y = axes[(int)SongAxis.Sophistication];
		var uncertainty = new Rect2(plane.Position + new Vector2(x.lo * plane.Size.X, (1 - y.hi) * plane.Size.Y), new Vector2((x.hi - x.lo) * plane.Size.X, (y.hi - y.lo) * plane.Size.Y));
		DrawRect(uncertainty, new Color(color, .14f));
		DrawRect(uncertainty, new Color(color, .65f), false, 1.4f);
		var point = plane.Position + new Vector2(x.Center * plane.Size.X, (1 - y.Center) * plane.Size.Y);
		GreasePencil.Mark(this, point, color); return point;
	}

	/// <summary>A row of indicator lamps for a fit read: lit up to the best guess, a dim glow through the doubt, dark beyond.
	/// The bezel carries the series' pencil colour.</summary>
	private void DrawLamps(PolarBand band, float x, float y, float pitch, float radius, Color bezel) {
		for (int i = 0; i < Lamps; i++) {
			float from = i / (float)Lamps, to = (i + 1) / (float)Lamps;
			bool lit = to <= band.Center + 0.0001f;
			bool dim = !lit && from < band.hi && to > band.lo;
			var at = new Vector2(x + pitch * (i + .5f) - 0f, y);
			if (lit) DrawCircle(at, radius + 3.5f, new Color(GreasePencil.Amber, .26f));
			DrawCircle(at, radius, lit ? new Color("f4b840") : dim ? new Color(GreasePencil.Amber, .36f) : new Color("a99f84"));
			if (lit) DrawCircle(at + new Vector2(-radius * .3f, -radius * .3f), radius * .32f, new Color(1f, .97f, .85f, .9f));
			DrawArc(at, radius, 0f, Mathf.Tau, 24, new Color(bezel, lit || dim ? .95f : .6f), 1.8f, true);
		}
	}

	/// <summary>"Low" under the first lamp and "High" under the last, for a lamp row that starts at <paramref name="x"/>.</summary>
	private void LowHigh(float x, float pitch, float y) {
		Label("Low", new Vector2(x, y), Muted, 12);
		float end = x + pitch * Lamps;
		Label("High", new Vector2(end - font.GetStringSize("High", fontSize: 12).X, y), Muted, 12);
	}
}
