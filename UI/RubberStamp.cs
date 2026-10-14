using Godot;

/// <summary>
/// A rubber-stamp impression: capitals in a double-ruled box, tilted, in one ink, with a little bleed. State on
/// the office's paperwork (a verdict on an act, DUPLICATE on a carbon) reads as something that was done to the
/// page, not as a badge laid over it.
/// </summary>
public partial class RubberStamp : Control {
	public static readonly Color Red = new("a8322a");
	public static readonly Color Blue = new("24457A");

	private string text = "";
	private Color ink = Red;
	private float tilt = -0.11f;
	private int fontSize = 15;
	private float alpha = 0.88f;

	public RubberStamp() {
		MouseFilter = MouseFilterEnum.Ignore;
		SizeFlagsVertical = SizeFlags.ShrinkCenter;
	}

	public RubberStamp Set(string stampText, Color stampInk, float stampTilt = -0.11f, int size = 15, float stampAlpha = 0.88f) {
		text = stampText.ToUpperInvariant();
		ink = stampInk;
		tilt = stampTilt;
		fontSize = size;
		alpha = stampAlpha;
		Vector2 box = BoxSize();
		// The rotated box's bounding rectangle, so neighbours leave room for the corners.
		float c = Mathf.Abs(Mathf.Cos(tilt)), s = Mathf.Abs(Mathf.Sin(tilt));
		CustomMinimumSize = new Vector2(box.X * c + box.Y * s + 6, box.X * s + box.Y * c + 6);
		QueueRedraw();
		return this;
	}

	private Vector2 BoxSize() {
		Font font = PaperTheme.SansBold;
		return font.GetStringSize(text, HorizontalAlignment.Left, -1, fontSize) + new Vector2(34, 16);
	}

	public override void _Draw() {
		if (text.Length == 0) return;
		Font font = PaperTheme.SansBold;
		Vector2 box = BoxSize();
		DrawSetTransform(Size / 2f, tilt, Vector2.One);
		var rect = new Rect2(-box / 2f, box);
		Color solid = new(ink, alpha), soft = new(ink, alpha * 0.55f);
		DrawRect(rect, solid, false, 2.5f);
		DrawRect(rect.Grow(-4.5f), soft, false, 1f);
		Vector2 textSize = font.GetStringSize(text, HorizontalAlignment.Left, -1, fontSize);
		var origin = new Vector2(-textSize.X / 2f, -textSize.Y / 2f + font.GetAscent(fontSize));
		// Two passes a hair apart: a stamp never lays down ink perfectly flat.
		DrawString(font, origin + new Vector2(0.7f, 0.5f), text, HorizontalAlignment.Left, -1, fontSize, soft);
		DrawString(font, origin, text, HorizontalAlignment.Left, -1, fontSize, solid);
		DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
	}
}
