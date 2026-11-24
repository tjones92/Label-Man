using Godot;

/// <summary>
/// A rubber stamp pressed on the paper: a double-ruled box, a small typed line over a big one, set at a slant in one ink.
/// "FALLS DUE / JAN 1, 1961" in oxblood, "EXTENDED / TO JAN 1, 1962" in green. It draws itself, so the sitting can
/// bring it down with a thunk by tweening <c>scale</c> and <c>modulate</c>.
/// </summary>
public partial class LienStamp : Control {
	private string top = "", bottom = "";
	private Color ink = PaperTheme.Oxblood;
	private float tilt = -0.06f;
	private const int TopSize = 15, BottomSize = 22;
	private const float PadX = 22f, PadY = 12f, Slack = 14f;

	public LienStamp Set(string topLine, string bottomLine, Color stampInk, float tiltRadians = -0.06f) {
		top = (topLine ?? "").ToUpperInvariant();
		bottom = (bottomLine ?? "").ToUpperInvariant();
		ink = stampInk; tilt = tiltRadians;
		MouseFilter = MouseFilterEnum.Ignore;
		SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
		UpdateMinimumSize();
		QueueRedraw();
		return this;
	}

	private Vector2 Plate() {
		Font small = PaperTheme.Elite, big = PaperTheme.TypedBold;
		float w = Mathf.Max(small.GetStringSize(top, HorizontalAlignment.Left, -1, TopSize).X, big.GetStringSize(bottom, HorizontalAlignment.Left, -1, BottomSize).X);
		return new Vector2(w + 2 * PadX, TopSize + BottomSize + 2 * PadY + 8f);
	}

	public override Vector2 _GetMinimumSize() => Plate() + new Vector2(2 * Slack, 2 * Slack);

	public override void _Notification(int what) {
		if (what == NotificationResized) PivotOffset = Size / 2f;
	}

	public override void _Draw() {
		Vector2 plate = Plate();
		DrawSetTransform(Size / 2f, tilt, Vector2.One);
		var rect = new Rect2(-plate / 2f, plate);
		var stamped = new Color(ink, 0.9f);
		DrawRect(rect, stamped, false, 3f);
		DrawRect(rect.Grow(-6f), new Color(ink, 0.75f), false, 1.2f);
		Font small = PaperTheme.Elite, big = PaperTheme.TypedBold;
		float topY = rect.Position.Y + PadY + 4f + small.GetAscent(TopSize);
		DrawString(small, new Vector2(rect.Position.X, topY), top, HorizontalAlignment.Center, plate.X, TopSize, stamped);
		float bottomY = topY + 6f + big.GetAscent(BottomSize) + 2f;
		DrawString(big, new Vector2(rect.Position.X, bottomY), bottom, HorizontalAlignment.Center, plate.X, BottomSize, stamped);
	}
}
