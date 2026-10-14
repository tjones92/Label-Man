using System.Linq;
using Godot;

/// <summary>
/// The gold record on the wall: when one of the player's records passes <see cref="GoldUnits"/> copies (the same bar the
/// chart panel calls "Gold Record territory"), a framed gold disc goes up on the right-hand panelling, mounted on velvet
/// with a brass plaque naming the title and the count. A label with several keeps one frame for the best seller and
/// says how many are behind it. It lives in the painted layer, so the office light tints it with the painting.
///
/// Nothing is drawn until a record qualifies; the painted wall is bare there.
/// </summary>
public partial class PaintedGoldRecord : Control {
	public const int GoldUnits = 500_000;

	// The frame's centre and size in the scene's 1920x1080 design space (the wall right of the framed record).
	private static readonly Vector2 FrameCentre = new(1700f, 214f);
	private const float FrameSize = 150f, DiscRadius = 52f;

	private static readonly Color Walnut = new("3b2514"), WalnutLight = new("6b4528"), Velvet = new("2c1f19");
	private static readonly Color Gold = new("d9a935"), GoldDark = new("8a6414"), GoldLight = new("f6dc86");

	private LabelBrand brand;
	private string labelName = "";
	private string title = "", plaqueLine = "";
	private int goldCount;
	private int lastDayKey = -1;
	private readonly Control hotspot = new();

	public PaintedGoldRecord() {
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		Visible = false;
		// A transparent patch over the frame so hovering it tells you what it is.
		hotspot.MouseFilter = MouseFilterEnum.Stop;
		hotspot.Position = FrameCentre - new Vector2(FrameSize / 2f + 2f, FrameSize / 2f);
		hotspot.Size = new Vector2(FrameSize + 4f, FrameSize + 40f);
		AddChild(hotspot);
	}

	/// <summary>Refreshes from the desk. Returns the new title when a record has just gone gold since the last refresh,
	/// else null; the first refresh after load only establishes the baseline so a loaded save does not re-announce.</summary>
	public string Refresh(PlayerDesk desk, LabelBrand labelBrand, string name, int dayKey) {
		brand = labelBrand;
		labelName = name ?? "";
		if (desk?.HasLabel != true) { Forget(); return null; }
		if (dayKey == lastDayKey) return null;   // sales only settle weekly; once a day is plenty
		lastDayKey = dayKey;
		var golds = desk.ReleasedRecords
			.Where(r => r?.baseRecord != null)
			.Select(r => (Record: r, Units: r.totalUnitsSold + desk.PendingTrunkUnits(r.baseRecord.recordId)))
			.Where(x => x.Units >= GoldUnits)
			.OrderByDescending(x => x.Units).ToList();
		int previous = goldCount;
		bool baseline = !hasBaseline;
		hasBaseline = true;
		goldCount = golds.Count;
		Visible = goldCount > 0;
		if (goldCount > 0) {
			var best = golds[0];
			title = best.Record.baseRecord.title ?? "";
			plaqueLine = $"{best.Units:N0} SOLD" + (goldCount > 1 ? $"  ·  {goldCount} GOLD" : "");
			hotspot.TooltipText = $"Gold record: \"{title}\" by {best.Record.baseRecord.artistName}, {best.Units:N0} copies" +
				(goldCount > 1 ? $".\n{goldCount - 1} more of yours have gone gold." : ".");
		}
		QueueRedraw();
		return !baseline && goldCount > previous ? title : null;
	}
	private bool hasBaseline;

	/// <summary>Drops the baseline (a save was loaded), so the next refresh announces nothing.</summary>
	public void Forget() {
		goldCount = 0; hasBaseline = false; lastDayKey = -1; Visible = false;
	}

	public override void _Draw() {
		if (goldCount == 0) return;
		var frame = new Rect2(FrameCentre - new Vector2(FrameSize / 2f, FrameSize / 2f), new Vector2(FrameSize, FrameSize));
		// Shadow on the panelling, then the walnut frame with a bevel, then the velvet.
		DrawRect(new Rect2(frame.Position + new Vector2(4f, 7f), frame.Size), new Color(0f, 0f, 0f, 0.42f));
		DrawRect(frame, Walnut);
		DrawRect(frame.Grow(-2f), WalnutLight, false, 2f);
		var velvet = frame.Grow(-11f);
		DrawRect(velvet, Velvet);
		DrawRect(velvet, new Color(0f, 0f, 0f, 0.55f), false, 2f);

		// The disc: gold, with grooves that catch the light unevenly and a sheen across one shoulder.
		Vector2 c = FrameCentre;
		DrawCircle(c + new Vector2(2f, 4f), DiscRadius, new Color(0f, 0f, 0f, 0.4f));
		DrawCircle(c, DiscRadius, Gold);
		for (float r = DiscRadius - 4f; r > 21f; r -= 3.4f) {
			bool light = ((int)(r / 3.4f)) % 2 == 0;
			DrawArc(c, r, 0f, Mathf.Tau, 56, light ? new Color(GoldLight, 0.34f) : new Color(GoldDark, 0.32f), 1f, true);
		}
		DrawArc(c, DiscRadius, 0f, Mathf.Tau, 64, GoldDark, 1.6f, true);
		// Sheen: two soft wedges across the upper left, the side the lamp is on.
		foreach (float spread in new[] { 0.34f, 0.18f }) {
			var wedge = new Vector2[] {
				c, c + new Vector2(Mathf.Cos(-2.5f - spread), Mathf.Sin(-2.5f - spread)) * (DiscRadius - 1f),
				c + new Vector2(Mathf.Cos(-2.5f + spread), Mathf.Sin(-2.5f + spread)) * (DiscRadius - 1f)
			};
			DrawColoredPolygon(wedge, new Color(1f, 0.96f, 0.78f, spread > 0.3f ? 0.10f : 0.14f));
		}
		// The centre label is the player's own.
		if (brand != null) LabelCrest.DrawDisc45(this, new Rect2(c - Vector2.One * 22f, Vector2.One * 44f), brand, labelName);
		else DrawCircle(c, 21f, new Color("efe3c4"));
		DrawCircle(c, 3f, new Color("120e0b"));

		// The plaque: brass, with two lines of typewriter engraving.
		var plaque = new Rect2(FrameCentre.X - 66f, frame.End.Y + 8f, 132f, 30f);
		DrawRect(new Rect2(plaque.Position + new Vector2(1.5f, 3f), plaque.Size), new Color(0f, 0f, 0f, 0.4f));
		DrawRect(plaque, new Color("b98f3c"));
		DrawRect(plaque, new Color("5f4412"), false, 1.2f);
		DrawRect(new Rect2(plaque.Position + new Vector2(1f, 1f), new Vector2(plaque.Size.X - 2f, 2f)), new Color(GoldLight, 0.7f));
		Font font = PaperTheme.TypedBold;
		string name = Trim($"\"{title}\"", font, 10, plaque.Size.X - 10f);
		string count = Trim(plaqueLine, font, 9, plaque.Size.X - 10f);
		Color engrave = new("2e2008");
		DrawString(font, new Vector2(plaque.Position.X + (plaque.Size.X - font.GetStringSize(name, HorizontalAlignment.Left, -1, 10).X) / 2f, plaque.Position.Y + 13f),
			name, HorizontalAlignment.Left, -1, 10, engrave);
		DrawString(font, new Vector2(plaque.Position.X + (plaque.Size.X - font.GetStringSize(count, HorizontalAlignment.Left, -1, 9).X) / 2f, plaque.Position.Y + 25f),
			count, HorizontalAlignment.Left, -1, 9, new Color(engrave, 0.9f));
	}

	private static string Trim(string text, Font font, int size, float maxWidth) {
		if (font.GetStringSize(text, HorizontalAlignment.Left, -1, size).X <= maxWidth) return text;
		while (text.Length > 2 && font.GetStringSize(text + "…", HorizontalAlignment.Left, -1, size).X > maxWidth) text = text[..^1];
		return text + "…";
	}
}
