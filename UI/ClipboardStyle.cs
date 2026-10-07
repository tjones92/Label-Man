using Godot;

/// <summary>
/// A clipboard as a card background: a hardboard backing with a hanging hole, a sheet of paper laid on it, and the
/// brass clip across the top edge holding the sheet down. Content goes on the paper, below the clip: the margins keep it clear.
///
/// The whole thing is drawn by the stylebox (backing, paper, clip), so the card needs no child nodes for the hardware and
/// grows with whatever is put on the sheet. The paper reuses <see cref="PaperStyleBox"/>, so it has the same grain
/// and lamp falloff as every other sheet in the office.
/// </summary>
public partial class ClipboardStyle : StyleBox {
	public const float BoardPad = 20f, ClipTop = 46f;
	private static readonly Color Hardboard = new("7b5733");
	private static readonly Color BoardEdge = new("3f2a15");
	private static readonly Color Brass = new("c19b47"), BrassDark = new("785a1e"), BrassLight = new("ecd089");

	private readonly PaperStyleBox sheet = new() { Fill = PaperTheme.Paper, Border = new Color("8c6f38"), ShadowSize = 5, ShadowAlpha = 0.3f, ShadowOffset = new Vector2(1, 2), Radius = 1 };
	private StyleBoxFlat backing;

	public ClipboardStyle() {
		// Content sits on the paper, below the clip's jaw.
		ContentMarginLeft = BoardPad + 26f; ContentMarginRight = BoardPad + 26f;
		ContentMarginTop = ClipTop + 24f; ContentMarginBottom = BoardPad + 18f;
	}

	public override void _Draw(Rid toCanvasItem, Rect2 rect) {
		backing ??= new StyleBoxFlat();
		backing.BgColor = Hardboard;
		backing.BorderColor = BoardEdge;
		backing.SetBorderWidthAll(2);
		backing.SetCornerRadiusAll(9);
		backing.ShadowColor = new Color(0f, 0f, 0f, 0.55f);
		backing.ShadowSize = 20;
		backing.ShadowOffset = new Vector2(0f, 10f);
		backing.Draw(toCanvasItem, rect);

		// Hardboard grain: long faint streaks, placed by a hash so the board looks the same every time it is drawn.
		for (int i = 0; i < 46; i++) {
			float y = rect.Position.Y + 8f + Hash(i, 1) * (rect.Size.Y - 16f);
			float x0 = rect.Position.X + 8f + Hash(i, 2) * rect.Size.X * 0.5f, x1 = Mathf.Min(rect.End.X - 8f, x0 + 60f + Hash(i, 3) * rect.Size.X * 0.5f);
			bool dark = Hash(i, 4) > 0.45f;
			RenderingServer.CanvasItemAddLine(toCanvasItem, new Vector2(x0, y), new Vector2(x1, y),
				dark ? new Color(0.17f, 0.1f, 0.04f, 0.2f) : new Color(0.75f, 0.55f, 0.32f, 0.12f), 1f, true);
		}
		// The hole it hangs by.
		Vector2 hole = new(rect.Position.X + rect.Size.X / 2f, rect.End.Y - 11f);
		RenderingServer.CanvasItemAddCircle(toCanvasItem, hole + new Vector2(0f, 1f), 6.5f, new Color(0.78f, 0.6f, 0.36f, 0.5f));
		RenderingServer.CanvasItemAddCircle(toCanvasItem, hole, 6f, new Color("1a1109"));

		// The paper.
		var paper = new Rect2(rect.Position + new Vector2(BoardPad, ClipTop), new Vector2(rect.Size.X - BoardPad * 2f, rect.Size.Y - ClipTop - BoardPad));
		if (paper.Size.X > 20f && paper.Size.Y > 20f) sheet.Draw(toCanvasItem, paper);

		DrawClip(toCanvasItem, rect.Position.X + rect.Size.X / 2f, rect.Position.Y);
	}

	/// <summary>The brass clip: a rolled-over handle, a flat plate with two rivets, and the jaw lying across the top of the sheet.</summary>
	private static void DrawClip(Rid canvas, float cx, float top) {
		void Quad(float x, float y, float w, float h, Color color) =>
			RenderingServer.CanvasItemAddRect(canvas, new Rect2(x, y, w, h), color);
		// A soft shadow cast on the sheet below the jaw.
		Quad(cx - 66f, top + 54f, 132f, 6f, new Color(0f, 0f, 0f, 0.16f));
		// The plate.
		Quad(cx - 58f, top + 12f, 116f, 32f, Brass);
		Quad(cx - 58f, top + 12f, 116f, 3f, BrassLight);
		Quad(cx - 58f, top + 41f, 116f, 3f, BrassDark);
		// The jaw, over the paper's edge.
		Quad(cx - 68f, top + 40f, 136f, 15f, Brass);
		Quad(cx - 68f, top + 40f, 136f, 2.5f, BrassLight);
		Quad(cx - 68f, top + 52f, 136f, 3f, BrassDark);
		// The handle: a rolled bar, with the spring loop beneath it.
		Quad(cx - 30f, top + 3f, 60f, 10f, Brass);
		Quad(cx - 30f, top + 3f, 60f, 2.5f, BrassLight);
		Quad(cx - 30f, top + 10f, 60f, 3f, BrassDark);
		RenderingServer.CanvasItemAddCircle(canvas, new Vector2(cx - 38f, top + 28f), 3.6f, BrassDark);
		RenderingServer.CanvasItemAddCircle(canvas, new Vector2(cx + 38f, top + 28f), 3.6f, BrassDark);
		RenderingServer.CanvasItemAddCircle(canvas, new Vector2(cx - 38.8f, top + 27.2f), 1.6f, BrassLight);
		RenderingServer.CanvasItemAddCircle(canvas, new Vector2(cx + 37.2f, top + 27.2f), 1.6f, BrassLight);
	}

	/// <summary>A stable value in [0, 1) for an (index, salt) pair.</summary>
	private static float Hash(int index, int salt) {
		unchecked {
			uint h = (uint)(index * 2654435761u) ^ (uint)(salt * 40503u);
			h ^= h >> 15; h *= 0x2c1b3c6du; h ^= h >> 12;
			return (h & 0xFFFF) / 65536f;
		}
	}
}
