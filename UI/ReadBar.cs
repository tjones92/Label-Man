using Godot;

/// <summary>
/// A horizontal gauge for a read that is an estimate: a pencil stroke to the best guess along a ruled strip of graph
/// paper, a highlighter wash around it as wide as the player's doubt, and a pencil colour that follows the same words
/// the text uses ("weak" .. "standout").
/// It replaces five-star rows, which invite picking whatever has the most stars; a stroke with a fog band reads as
/// what it is -- how the thing sounded to you, not a grade.
/// </summary>
public partial class ReadBar : Control {
	private static readonly Color Ground = new("f3ecd2");
	private static readonly Color Outline = new("6f6244");
	private static readonly Color Weak = new("786d58");      // soft graphite
	private static readonly Color Fair = new("b0691c");      // amber crayon
	private static readonly Color Strong = GreasePencil.BluePencil;
	private static readonly Color Standout = GreasePencil.GreenPencil;

	private float value;
	private float doubt;

	public ReadBar() {
		CustomMinimumSize = new Vector2(150, 14);
		SizeFlagsVertical = SizeFlags.ShrinkCenter;
		MouseFilter = MouseFilterEnum.Ignore;
	}

	/// <summary>Best guess in [0, 1]; <paramref name="confidence"/> in [0, 1] (1 = a close read, no fog).</summary>
	public ReadBar Set(float newValue, float confidence = 1f) {
		value = Mathf.Clamp(newValue, 0f, 1f);
		doubt = Mathf.Clamp(1f - confidence, 0f, 1f) * 0.22f;
		QueueRedraw();
		return this;
	}

	/// <summary>The bucket colour for a value; the words in the UI use the same cut points.</summary>
	public static Color ColorFor(float v) => v >= 0.75f ? Standout : v >= 0.55f ? Strong : v >= 0.35f ? Fair : Weak;

	public override void _Draw() {
		Vector2 size = Size;
		DrawRect(new Rect2(Vector2.Zero, size), Ground);
		// Fog first, so the pencil lies over it: a highlighter swipe the width of the doubt.
		if (doubt > 0.001f) {
			float lo = Mathf.Max(0f, value - doubt), hi = Mathf.Min(1f, value + doubt);
			DrawRect(new Rect2(size.X * lo, 1, size.X * (hi - lo), size.Y - 2), new Color(GreasePencil.Highlighter, 0.85f));
		}
		// Graph-paper ticks at the quarters.
		for (int q = 1; q < 4; q++) DrawLine(new Vector2(size.X * q / 4f, 1), new Vector2(size.X * q / 4f, size.Y - 1), new Color(GreasePencil.GraphLine, 0.7f), 1f);
		Color color = ColorFor(value);
		float end = Mathf.Max(3f, size.X * value);
		float mid = size.Y / 2f, weight = Mathf.Max(3f, size.Y * 0.56f);
		// The stroke: a firm pass and a fainter one a pixel off, which is what makes it read as pencil, not a fill.
		DrawLine(new Vector2(2f, mid), new Vector2(end, mid), new Color(color, 0.92f), weight, true);
		DrawLine(new Vector2(2f, mid - weight * 0.3f), new Vector2(end - 1f, mid - weight * 0.3f), new Color(color, 0.45f), Mathf.Max(1f, weight * 0.4f), true);
		DrawRect(new Rect2(Vector2.Zero, size), Outline, false, 1f);
	}
}
