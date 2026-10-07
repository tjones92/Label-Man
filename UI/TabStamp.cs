using Godot;

/// <summary>
/// The unread count on a folder tab, as a red rubber-stamp circle struck over the tab's shoulder: a double ring
/// round the number, a hair crooked, the ink a little patchy. It is a child of the tab, ignores the mouse, and
/// sits one z-level up so the neighbouring tab does not paint over it.
/// </summary>
public partial class TabStamp : Control {
	private const string NodeName = "UnreadStamp";
	private int count;

	/// <summary>Puts, updates or removes the stamp on <paramref name="tab"/>.</summary>
	public static void Apply(Button tab, int count) {
		TabStamp stamp = tab.GetNodeOrNull<TabStamp>(NodeName);
		if (count <= 0) { stamp?.Hide(); return; }
		if (stamp == null) {
			stamp = new TabStamp { Name = NodeName, MouseFilter = MouseFilterEnum.Ignore, ZIndex = 2 };
			stamp.SetAnchorsPreset(LayoutPreset.TopRight);
			stamp.OffsetLeft = -30; stamp.OffsetRight = -2; stamp.OffsetTop = -8; stamp.OffsetBottom = 20;
			stamp.PivotOffset = new Vector2(14, 14);
			stamp.RotationDegrees = -9f;
			tab.AddChild(stamp);
		}
		stamp.count = count;
		stamp.Show();
		stamp.QueueRedraw();
	}

	public override void _Draw() {
		Color ink = RubberStamp.Red;
		Vector2 centre = Size / 2f;
		float radius = Mathf.Min(Size.X, Size.Y) / 2f - 1f;
		DrawCircle(centre, radius, new Color("f4e9cb", 0.92f));
		DrawArc(centre, radius, 0f, Mathf.Tau, 32, new Color(ink, 0.9f), 2.2f, true);
		DrawArc(centre, radius - 3.4f, 0.25f, Mathf.Tau - 0.2f, 28, new Color(ink, 0.7f), 1f, true);
		string text = count > 99 ? "99+" : count.ToString();
		int size = text.Length > 2 ? 10 : text.Length > 1 ? 13 : 15;
		Font font = PaperTheme.SansBold;
		Vector2 measured = font.GetStringSize(text, HorizontalAlignment.Left, -1, size);
		DrawString(font, new Vector2(centre.X - measured.X / 2f, centre.Y + measured.Y * 0.3f), text, HorizontalAlignment.Left, -1, size, new Color(ink, 0.95f));
	}
}
