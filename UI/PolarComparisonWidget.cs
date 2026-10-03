using System;
using System.Linq;
using Godot;

/// <summary>Draws only PolarComparisonRead. Geometry, colors, arrows and prose have no truth access.</summary>
public partial class PolarComparisonWidget : Control {
	private PolarComparisonRead read;
	private readonly Color ink = new("30291d"), muted = new("75674d"), teal = new("247c7e"), gold = new("be8840"), violet = new("725989"), rust = new("a44836");
	private Font font;
	public void SetRead(PolarComparisonRead value) { read = value; QueueRedraw(); }
	public override void _Ready() { CustomMinimumSize = new Vector2(870, 390); MouseFilter = MouseFilterEnum.Ignore; font = ThemeDB.FallbackFont; }
	public override void _Draw() {
		if (read == null || font == null) return;
		var center = new Vector2(168, 160); const float radius = 95;
		for (int ring = 1; ring <= 4; ring++) DrawPolyline(Radar(null, center, radius * ring / 4), new Color(muted, .16f), 1, true);
		string[] names = { "Vocal power", "Vocal nuance", "Musicianship", "Ensemble", "Lyric delivery", "Studio craft" };
		for (int i = 0; i < 6; i++) {
			var point = Spoke(center, radius, i);
			DrawLine(center, point, new Color(muted, .22f), 1, true);
			var position = Spoke(center, radius + 24, i);
			Label(names[i], position - new Vector2(font.GetStringSize(names[i], fontSize: 12).X / 2, 0), muted, 12);
		}
		BandRing(read.reference.axes, center, radius, new Color(gold, .16f));
		BandRing(read.proposed.axes, center, radius, new Color(violet, .27f));
		DrawPolyline(Radar(read.act.axes, center, radius, false), new Color(teal, .48f), 1.4f, true);
		DrawPolyline(Radar(read.act.axes, center, radius, true), teal, 2, true);
		for (int i = 0; i < 6; i++) {
			if (read.proposed.axes[i].lo > read.act.axes[i].hi)
				DrawLine(Spoke(center, radius * read.act.axes[i].hi, i), Spoke(center, radius * read.proposed.axes[i].lo, i), rust, 4, true);
		}
		Label("DEMAND / CAPABILITY", new Vector2(42, 24), ink, 14);
		Label("Act range", new Vector2(28, 301), teal, 12);
		Label("Heard material", new Vector2(108, 301), gold, 12);
		Label(read.isRecorded ? "Performance" : "Arrangement", new Vector2(214, 301), violet, 12);
		Label("Shaded bands show uncertainty.", new Vector2(28, 323), muted, 12);

		var plane = new Rect2(368, 67, 170, 170);
		DrawRect(plane, new Color(muted, .05f)); DrawRect(plane, new Color(muted, .3f), false);
		DrawLine(plane.Position + new Vector2(0, 85), plane.Position + new Vector2(170, 85), new Color(muted, .12f));
		DrawLine(plane.Position + new Vector2(85, 0), plane.Position + new Vector2(85, 170), new Color(muted, .12f));
		Label("IDENTITY PROJECTION", new Vector2(355, 24), ink, 14);
		Label("Sophisticated", new Vector2(367, 53), muted, 12);
		Label("Earthy", new Vector2(368, 258), muted, 12);
		Label("Gentle → tough", new Vector2(421, 280), muted, 12);
		IdentityDot(read.act.axes, plane, teal);
		var from = IdentityDot(read.reference.axes, plane, gold);
		var to = IdentityDot(read.proposed.axes, plane, violet);
		var vector = to - from;
		if (vector.Length() > 3) {
			DrawLine(from, to, new Color(violet, .7f), 2, true);
			var normal = vector.Normalized(); var side = new Vector2(-normal.Y, normal.X);
			DrawLine(to, to - normal * 9 + side * 4, violet, 2, true); DrawLine(to, to - normal * 9 - side * 4, violet, 2, true);
		}
		Label("Arrow: perceived arrangement pull", new Vector2(349, 301), muted, 12);
		Label("Full Identity also reads sincerity", new Vector2(349, 323), muted, 12);
		Label("and maturity.", new Vector2(349, 342), muted, 12);

		Label("PERCEIVED FIT", new Vector2(601, 24), ink, 14);
		string[] fitNames = { "Capability", "Identity", "Moment" };
		for (int i = 0; i < 3; i++) {
			float y = 70 + i * 69;
			Label(fitNames[i], new Vector2(601, y), ink, 14);
			DrawFit(read.referenceFit[i], new Rect2(603, y + 10, 213, 7), gold);
			DrawFit(read.proposedFit[i], new Rect2(603, y + 22, 213, 10), violet);
		}
		Label("Heard material / arrangement", new Vector2(601, 301), muted, 12);
		Label(read.hasMarketEvidence ? "Moment remains a market forecast." : "Moment: no recent market evidence.", new Vector2(601, 323), muted, 12);
		Label("Low                              High", new Vector2(603, 280), muted, 12);
		Label(read.isRecorded ? "PRINTED MASTER · playback estimate" : "ARRANGEMENT PREVIEW · estimated performance", new Vector2(28, 376), muted, 12);
	}
	private void Label(string text, Vector2 at, Color color, int size) => DrawString(font, at, text, fontSize: size, modulate: color);
	private Vector2 Spoke(Vector2 center, float radius, int axis) { float angle = -Mathf.Pi / 2 + axis * Mathf.Tau / 6; return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius; }
	private Vector2[] Radar(PolarBand[] bands, Vector2 center, float radius, bool high = true) {
		var points = new Vector2[7]; for (int i = 0; i < 6; i++) points[i] = Spoke(center, radius * (bands == null ? 1 : high ? bands[i].hi : bands[i].lo), i); points[6] = points[0]; return points;
	}
	private void BandRing(PolarBand[] bands, Vector2 center, float radius, Color color) {
		var low = Radar(bands, center, radius, false); var high = Radar(bands, center, radius, true);
		for (int i = 0; i < 6; i++) {
			var polygon = new[] { low[i], high[i], high[i + 1], low[i + 1] }.Distinct().ToArray();
			if (polygon.Length >= 3) DrawColoredPolygon(polygon, color);
		}
		DrawPolyline(high, new Color(color, .8f), 1.3f, true); DrawPolyline(low, new Color(color, .6f), 1, true);
	}
	private Vector2 IdentityDot(PolarBand[] axes, Rect2 plane, Color color) {
		var x = axes[(int)SongAxis.Toughness]; var y = axes[(int)SongAxis.Sophistication];
		var uncertainty = new Rect2(plane.Position + new Vector2(x.lo * plane.Size.X, (1 - y.hi) * plane.Size.Y), new Vector2((x.hi - x.lo) * plane.Size.X, (y.hi - y.lo) * plane.Size.Y));
		DrawRect(uncertainty, new Color(color, .13f)); DrawRect(uncertainty, new Color(color, .45f), false);
		var point = plane.Position + new Vector2(x.Center * plane.Size.X, (1 - y.Center) * plane.Size.Y); DrawCircle(point, 3.8f, color); return point;
	}
	private void DrawFit(PolarBand band, Rect2 track, Color color) {
		DrawRect(track, new Color(muted, .13f));
		float width = Math.Max(2, track.Size.X * (band.hi - band.lo));
		var segment = new Rect2(track.Position + new Vector2(Math.Min(track.Size.X * band.lo, track.Size.X - width), 0), new Vector2(width, track.Size.Y));
		DrawRect(segment, new Color(color, .42f)); DrawRect(segment, color, false);
	}
}
