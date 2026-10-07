using Godot;

/// <summary>
/// A horizontal gauge for a read that is an estimate: a filled bar to the best guess, a pale band around it
/// as wide as the player's doubt, and a colour that follows the same words the text uses ("weak" .. "standout").
/// It replaces five-star rows, which invite picking whatever has the most stars; a bar with a fog band reads as
/// what it is -- how the thing sounded to you, not a grade.
/// </summary>
public partial class ReadBar : Control {
	private static readonly Color Track = new("d8c7a0");
	private static readonly Color Outline = new("8a7a55");
	private static readonly Color Weak = new("8b7d62");
	private static readonly Color Fair = new("be8840");
	private static readonly Color Strong = new("2f7b7d");
	private static readonly Color Standout = new("3f6b2f");

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
		DrawRect(new Rect2(Vector2.Zero, size), Track);
		Color color = ColorFor(value);
		DrawRect(new Rect2(0, 0, size.X * value, size.Y), color);
		if (doubt > 0.001f) {
			float lo = Mathf.Max(0f, value - doubt), hi = Mathf.Min(1f, value + doubt);
			DrawRect(new Rect2(size.X * lo, 0, size.X * (hi - lo), size.Y), new Color(color, 0.30f));
		}
		DrawRect(new Rect2(Vector2.Zero, size), Outline, false, 1f);
	}
}
