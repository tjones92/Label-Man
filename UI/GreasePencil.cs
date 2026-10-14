using System;
using Godot;

/// <summary>
/// The office's drawing tools for anything charted by hand: a graphite stub, a blue and a green pencil, a red grease
/// pencil, graph paper, and the brass pins that hold a plate to the sheet under it. A stroke is never ruler-straight:
/// it wanders a pixel or so, and a second, fainter pass lies beside the first, which is what a soft pencil does.
///
/// Everything is static and draws into whichever <see cref="CanvasItem"/> is mid-<c>_Draw</c>. The wander comes from a
/// hash of the stroke's seed and the vertex index, never from a random generator, so a redraw is stable and nothing here can touch
/// a seeded sim stream.
/// </summary>
public static class GreasePencil {
	public static readonly Color Graphite = new("3a3328");
	public static readonly Color BluePencil = new("2a4a86");
	public static readonly Color GreenPencil = new("3b6a35");
	public static readonly Color RedGrease = new("b3361f");
	public static readonly Color Amber = new("e8a93a");
	public static readonly Color Highlighter = new("f1d96b");
	public static readonly Color GraphPaper = new("f5f1de");
	public static readonly Color GraphLine = new("8fb0c4");
	public static readonly Color Brass = new("b08a3c");

	/// <summary>A stable value in [-1, 1] for a (seed, index) pair.</summary>
	private static float Wander(int seed, int index) {
		unchecked {
			uint h = (uint)(seed * 73856093) ^ (uint)(index * 19349663);
			h ^= h >> 13; h *= 0x5bd1e995u; h ^= h >> 15;
			return (h & 0xFFFF) / 65535f * 2f - 1f;
		}
	}

	/// <summary>A segment broken into short pieces, each interior vertex nudged sideways a little.</summary>
	private static void AddWobbled(System.Collections.Generic.List<Vector2> into, Vector2 a, Vector2 b, int seed, float amplitude, bool first) {
		float length = a.DistanceTo(b);
		int pieces = Math.Max(1, (int)(length / 16f));
		Vector2 direction = length > 0.01f ? (b - a) / length : Vector2.Right;
		var normal = new Vector2(-direction.Y, direction.X);
		for (int i = first ? 0 : 1; i <= pieces; i++) {
			Vector2 p = a.Lerp(b, i / (float)pieces);
			if (i > 0 && i < pieces) p += normal * Wander(seed, i) * amplitude;
			into.Add(p);
		}
	}

	/// <summary>A pencil line through the given points: a firm pass, then a lighter one a hair to the side.</summary>
	public static void Path(CanvasItem canvas, Vector2[] points, Color color, float width = 2.4f, int seed = 1, float amplitude = 0.8f) {
		if (points == null || points.Length < 2) return;
		var firm = new System.Collections.Generic.List<Vector2>();
		var soft = new System.Collections.Generic.List<Vector2>();
		for (int i = 0; i < points.Length - 1; i++) {
			AddWobbled(firm, points[i], points[i + 1], seed + i * 13, amplitude, i == 0);
			AddWobbled(soft, points[i] + new Vector2(0.6f, 0.5f), points[i + 1] + new Vector2(0.6f, 0.5f), seed + i * 13 + 7, amplitude, i == 0);
		}
		canvas.DrawPolyline(firm.ToArray(), new Color(color, color.A * 0.9f), width, true);
		canvas.DrawPolyline(soft.ToArray(), new Color(color, color.A * 0.42f), Math.Max(1f, width * 0.55f), true);
	}

	public static void Line(CanvasItem canvas, Vector2 a, Vector2 b, Color color, float width = 2.4f, int seed = 1) =>
		Path(canvas, new[] { a, b }, color, width, seed);

	/// <summary>A dashed pencil line: short strokes laid end to end with a gap.</summary>
	public static void Dashed(CanvasItem canvas, Vector2 a, Vector2 b, Color color, float width = 2.2f, float dash = 8f, float gap = 5f) {
		float length = a.DistanceTo(b);
		if (length < 0.5f) return;
		Vector2 direction = (b - a) / length;
		for (float at = 0f; at < length; at += dash + gap)
			canvas.DrawLine(a + direction * at, a + direction * Math.Min(length, at + dash), new Color(color, color.A * 0.92f), width, true);
	}

	/// <summary>A closed outline (first point repeated by the caller or not) drawn with the wandering line.</summary>
	public static void Outline(CanvasItem canvas, Vector2[] ring, Color color, float width = 2.2f, int seed = 1) {
		var closed = new Vector2[ring.Length + 1];
		Array.Copy(ring, closed, ring.Length);
		closed[^1] = ring[0];
		Path(canvas, closed, color, width, seed);
	}

	/// <summary>A pencilled dot with a ring round it.</summary>
	public static void Mark(CanvasItem canvas, Vector2 at, Color color, float radius = 4.5f) {
		canvas.DrawCircle(at, radius, new Color(color, 0.92f));
		canvas.DrawArc(at, radius + 3f, 0f, Mathf.Tau, 20, new Color(color, 0.55f), 1.3f, true);
	}

	/// <summary>Graph paper: a pale ground, a fine grid every <paramref name="cell"/> pixels and a heavier one every fifth.</summary>
	public static void Graph(CanvasItem canvas, Rect2 rect, float cell = 14f) {
		canvas.DrawRect(rect, GraphPaper);
		int column = 0;
		for (float x = rect.Position.X + cell; x < rect.End.X - 1f; x += cell, column++)
			canvas.DrawLine(new Vector2(x, rect.Position.Y), new Vector2(x, rect.End.Y), new Color(GraphLine, (column + 1) % 5 == 0 ? 0.62f : 0.30f), 1f);
		int row = 0;
		for (float y = rect.Position.Y + cell; y < rect.End.Y - 1f; y += cell, row++)
			canvas.DrawLine(new Vector2(rect.Position.X, y), new Vector2(rect.End.X, y), new Color(GraphLine, (row + 1) % 5 == 0 ? 0.62f : 0.30f), 1f);
	}

	/// <summary>A brass drawing pin seen from above, with its little shadow on the paper.</summary>
	public static void Pin(CanvasItem canvas, Vector2 at) {
		canvas.DrawCircle(at + new Vector2(1.5f, 2.6f), 6.5f, new Color(0f, 0f, 0f, 0.32f));
		canvas.DrawCircle(at, 6f, Brass);
		canvas.DrawArc(at, 5.2f, 0f, Mathf.Tau, 20, new Color("6f5220"), 1.4f, true);
		canvas.DrawCircle(at + new Vector2(-1.6f, -1.8f), 2.1f, new Color("ecd48c"));
	}

	/// <summary>A sheet of graph paper pinned at its four corners: a drop shadow, the grid, a pencilled edge and the pins.</summary>
	public static void Plate(CanvasItem canvas, Rect2 rect) {
		canvas.DrawRect(new Rect2(rect.Position + new Vector2(2f, 4f), rect.Size), new Color(0.1f, 0.06f, 0.02f, 0.28f));
		Graph(canvas, rect);
		canvas.DrawRect(rect, new Color("8a7a55"), false, 1f);
		const float inset = 11f;
		Pin(canvas, rect.Position + new Vector2(inset, inset));
		Pin(canvas, new Vector2(rect.End.X - inset, rect.Position.Y + inset));
		Pin(canvas, new Vector2(rect.Position.X + inset, rect.End.Y - inset));
		Pin(canvas, rect.End - new Vector2(inset, inset));
	}
}
