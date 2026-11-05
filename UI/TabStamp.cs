using Godot;

/// <summary>
/// What is waiting behind a folder tab, marked the way an office would: a pencilled tally beside the tab's name
/// (strokes up to a gate of five, two gates at most), or for the ledger a spindle with up to three slips on it.
/// Neither shows an exact running number, so a busy week reads as "a few things" rather than a phone badge climbing
/// to 48 (period pass 2026-11-05). The mark sits inside the tab, after its name (the caller pads the name with
/// <see cref="Room"/>), ignores the mouse, and the exact count is on the tab's tooltip.
/// </summary>
public partial class TabStamp : Control {
	private const string NodeName = "UnreadStamp";
	private int count;
	private bool spindle;

	/// <summary>Spaces to append to a tab's name so the mark has room inside the tab.</summary>
	public static string Room(int count, bool asSpindle) =>
		count <= 0 ? "" : asSpindle ? "      " : new string(' ', 2 + Mathf.Min(count, 10) + (count > 5 ? 2 : 0));

	/// <summary>Puts, updates or removes the mark on <paramref name="tab"/>. <paramref name="asSpindle"/> draws slips
	/// on a spike (the ledger's news) instead of a tally.</summary>
	public static void Apply(Button tab, int count, bool asSpindle = false) {
		TabStamp stamp = tab.GetNodeOrNull<TabStamp>(NodeName);
		if (count <= 0) { stamp?.Hide(); return; }
		if (stamp == null) {
			stamp = new TabStamp { Name = NodeName, MouseFilter = MouseFilterEnum.Ignore, ZIndex = 2 };
			stamp.SetAnchorsPreset(LayoutPreset.RightWide);
			tab.AddChild(stamp);
		}
		stamp.spindle = asSpindle;
		int marks = Mathf.Min(count, 10);
		float width = asSpindle ? 26f : 6f + Mathf.Min(marks, 4) * 4.5f + (marks > 5 ? 5f + (marks - 5) * 4.5f : 0f);
		stamp.OffsetRight = -8f; stamp.OffsetLeft = -8f - width; stamp.OffsetTop = 5f; stamp.OffsetBottom = -3f;
		stamp.count = count;
		stamp.Show();
		stamp.QueueRedraw();
	}

	private static readonly Color Graphite = new("4a4a52");

	public override void _Draw() {
		if (spindle) DrawSpindle(); else DrawTally();
	}

	private void DrawTally() {
		// Hand-drawn strokes: each a hair off vertical, a hair different in length, in soft graphite.
		int marks = Mathf.Min(count, 10);
		float x = 3f, top = 2f, bottom = Size.Y - 2f;
		var wobble = new RandomNumberGenerator { Seed = 1960 };
		for (int gate = 0; gate * 5 < marks; gate++) {
			int strokes = Mathf.Min(4, marks - gate * 5);
			float start = x;
			for (int s = 0; s < strokes; s++) {
				float lean = wobble.RandfRange(-1.2f, 1.2f), t = wobble.RandfRange(-1f, 1f);
				DrawLine(new Vector2(x + lean, top + t), new Vector2(x - lean * 0.5f, bottom), new Color(Graphite, 0.9f), 1.7f, true);
				x += 4.5f;
			}
			if (marks - gate * 5 >= 5)   // the fifth stroke strikes through the four
				DrawLine(new Vector2(start - 2f, bottom - 3f), new Vector2(x - 1f, top + 4f), new Color(Graphite, 0.9f), 1.7f, true);
			x += 5f;
		}
	}

	private void DrawSpindle() {
		float cx = Size.X / 2f;
		int slips = Mathf.Min(count, 3);
		float[] tilt = { -0.16f, 0.12f, -0.06f };
		Color[] paper = { PaperTheme.Field, new Color("efe0b8"), PaperTheme.Field };
		float baseY = Size.Y - 3f;
		DrawLine(new Vector2(cx, 0f), new Vector2(cx, baseY), Graphite, 1.6f, true);
		for (int i = 0; i < slips; i++) {
			float y = baseY - 4f - i * 5f;
			DrawSetTransformMatrix(new Transform2D(tilt[i], new Vector2(cx, y)));
			var slip = new Rect2(-12f, -2.5f, 24f, 5f);
			DrawRect(slip, paper[i]);
			DrawRect(slip, PaperTheme.Edge, false, 1f);
			DrawSetTransformMatrix(Transform2D.Identity);
		}
		DrawLine(new Vector2(cx, 0f), new Vector2(cx, baseY - 4f - (slips - 1) * 5f - 2.5f), Graphite, 1.6f, true);   // the spike through them
		DrawRect(new Rect2(cx - 8f, baseY, 16f, 3f), PaperTheme.Ink);   // the weighted base
	}
}
