using Godot;

public partial class ChartEntryUI : Control
{
	[ExportGroup("Text Fields")]
	[Export] private Label rankText;
	[Export] private Label movementText;
	[Export] private Label songText;
	[Export] private Label artistText;
	[Export] private Label labelText;

	// The trade sheet's inks: navy for a rank, olive for a mover, stamp red for a slider -- not the RGB primaries.
	private static readonly Color Navy = new("1b2a52");
	private static readonly Color Olive = new("4f5c2c");
	private static readonly Color StampRed = new("a8322a");
	private static readonly Color Ink = new("2b2115");
	private static readonly Color Cream = new("f6ecd0");

	private RecordRuntimeData myRecord;
	private LabelBrand labelBrand;   // the crest drawn beside the label column; null on an empty row
	private string labelFullName = "";
	private const float CrestSize = 22f;
	public System.Action<RecordRuntimeData> OnEntryClicked;

	public override void _Ready()
	{
		// Make this control detect mouse input
		MouseFilter = MouseFilterEnum.Stop;
		StyleLabels();
	}

	/// <summary>One typographic system on the sheet: the title in the press italic, the artist in the trade sans, and every
	/// column trimmed with an ellipsis so a long name can no longer run into its neighbour.</summary>
	private void StyleLabels() {
		void Style(Label label, Font font, int size, Color color) {
			if (label == null) return;
			label.AddThemeFontOverride("font", font);
			label.AddThemeFontSizeOverride("font_size", size);
			label.AddThemeColorOverride("font_color", color);
			label.ClipText = true;
			label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
			label.AutowrapMode = TextServer.AutowrapMode.Off;
		}
		Style(songText, PaperTheme.SerifItalic, 16, Ink);
		Style(artistText, PaperTheme.SansSemiBold, 15, Ink);
		Style(labelText, PaperTheme.SansSemiBold, 13, PaperTheme.Fade);
		Style(movementText, PaperTheme.SansSemiBold, 13, Ink);
		// The rank is drawn as a circled numeral (see _Draw), so the plain text label stays out of the way.
		if (rankText != null) rankText.Visible = false;
		// The label column opens with its crest chip; the abbreviation moves right to make room.
		if (labelText != null) labelText.OffsetLeft += CrestSize + 4f;
	}

	public override void _Draw() {
		if (myRecord?.baseRecord == null) return;
		bool player = myRecord.baseRecord.isPlayerOwned;
		var bounds = new Rect2(Vector2.Zero, Size);
		// Ruled rows, like the printed sheet.
		DrawLine(new Vector2(0, Size.Y - 0.5f), new Vector2(Size.X, Size.Y - 0.5f), new Color(PaperTheme.Edge, 0.35f), 1f);
		// Your record gets the highlighter swipe a person would give it.
		if (player) DrawRect(bounds, new Color(PaperTheme.Highlighter, 0.6f));

		// The circled rank numeral. A pencilled ring for your own record; navy discs for the rest, olive and
		// stamp red where the row is a bullet or an anchor.
		Vector2 centre = new(24f, Size.Y / 2f);
		Color disc = player ? Cream : myRecord.isBullet ? Olive : myRecord.isAnchor ? StampRed : Navy;
		Color numeral = player ? PaperTheme.Rust : Cream;
		DrawCircle(centre, 13.5f, disc);
		if (player) DrawArc(centre, 13f, 0f, Mathf.Tau, 36, PaperTheme.Rust, 2f, true);
		string text = myRecord.currentPosition.ToString();
		Font font = PaperTheme.SansBold;
		int size = text.Length >= 3 ? 11 : 14;
		Vector2 measure = font.GetStringSize(text, HorizontalAlignment.Left, -1, size);
		DrawString(font, new Vector2(centre.X - measure.X / 2f, centre.Y + (font.GetAscent(size) - font.GetDescent(size)) / 2f),
			text, HorizontalAlignment.Left, -1, size, numeral);
		// The label's crest chip, at the head of the label column.
		if (labelBrand != null && labelText != null) {
			float chipX = labelText.OffsetLeft - CrestSize - 4f;
			LabelCrest.DrawCrest(this, new Rect2(chipX, (Size.Y - CrestSize) / 2f, CrestSize, CrestSize), labelBrand, labelFullName);
		}
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseEvent &&
			mouseEvent.ButtonIndex == MouseButton.Left &&
			mouseEvent.Pressed)
		{
			GD.Print($">>> SLOT CLICKED: {Name} <<<");
			GD.Print($">>> myRecord: {(myRecord != null ? myRecord.baseRecord.title : "NULL")} <<<");
			GD.Print($">>> OnEntryClicked has listeners: {OnEntryClicked != null} <<<");

			if (myRecord != null)
			{
				OnEntryClicked?.Invoke(myRecord);
			}
		}
	}

	public void Populate(RecordRuntimeData record)
	{
		myRecord = record;
		bool playerOwned = record?.baseRecord?.isPlayerOwned ?? false;
		Modulate = Colors.White;

		if (rankText != null) rankText.Text = $"#{record.currentPosition}";   // kept for the node tree; the numeral is drawn
		if (movementText != null) {
			int change = record.lastWeekPosition - record.currentPosition;
			movementText.Text = record.lastWeekPosition <= 0 ? "NEW" : change > 0 ? $"▲ {change}" : change < 0 ? $"▼ {-change}" : "—";
			movementText.AddThemeColorOverride("font_color", change > 0 ? Olive : change < 0 ? StampRed : PaperTheme.Fade);
		}
		if (songText != null) { songText.Text = record.baseRecord.title; songText.TooltipText = record.baseRecord.title; }
		if (artistText != null) { artistText.Text = record.baseRecord.artistName; artistText.TooltipText = record.baseRecord.artistName; }
		if (labelText != null) labelText.Text = playerOwned ? $"YOU · {GetLabelAbbrev(record.baseRecord.labelId)}" : GetLabelAbbrev(record.baseRecord.labelId);
		AILabel owner = ChartManager.Instance?.GetLabelById(record.baseRecord.labelId);
		labelFullName = owner?.labelName ?? "";
		labelBrand = string.IsNullOrEmpty(record.baseRecord.labelId) ? null : owner != null ? LabelBrand.For(owner) : LabelBrand.Derive(record.baseRecord.labelId);
		QueueRedraw();
	}

	public void Clear()
	{
		myRecord = null;
		labelBrand = null;
		Modulate = Colors.White;
		if (rankText != null) rankText.Text = "";
		if (movementText != null) movementText.Text = "";
		if (songText != null) songText.Text = "";
		if (artistText != null) artistText.Text = "";
		if (labelText != null) labelText.Text = "";
		QueueRedraw();
	}

	private string GetLabelAbbrev(string labelId)
	{
		if (string.IsNullOrEmpty(labelId)) return "";

		string fullName = labelId;

		if (ChartManager.Instance != null)
			fullName = ChartManager.Instance.GetLabelName(labelId);

		fullName = fullName.Replace(" Records", "")
						   .Replace(" Recording Co.", "")
						   .Replace(" Sound", "")
						   .Replace(" Music", "")
						   .Replace(" Productions", "");

		if (fullName.Length <= 4)
			return fullName.ToUpper();

		int spaceIndex = fullName.IndexOf(' ');
		if (spaceIndex > 0 && spaceIndex <= 8)
			return fullName.Substring(0, spaceIndex).ToUpper();

		return fullName.Substring(0, 4).ToUpper();
	}
}
