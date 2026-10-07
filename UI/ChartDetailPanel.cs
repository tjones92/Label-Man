using System.Collections.Generic;
using System.Text;
using Godot;

public partial class ChartDetailPanel : Control
{
	[ExportGroup("Panel")]
	[Export] private Control panelRoot;

	[ExportGroup("Header Section")]
	[Export] private Label titleText;
	[Export] private Label artistText;
	[Export] private Label labelGenreText;
	[Export] private Label releaseDateText;

	[ExportGroup("Chart Performance")]
	[Export] private Label positionText;
	[Export] private Label movementText;
	[Export] private Label chartStatsText;

	[ExportGroup("Narrative Descriptions")]
	[Export] private Label recordDescriptionText;
	[Export] private Label chartCommentaryText;
	[Export] private Label regionalHintText;

	[ExportGroup("Sales Summary")]
	[Export] private Label salesSummaryText;

	[ExportGroup("Tags")]
	[Export] private Control tagsContainer;
	// Note: We'll create tags as simple Label nodes instead of prefabs

	[ExportGroup("Buttons")]
	[Export] private Button closeButton;
	[Export] private Button viewArtistButton;
	[Export] private Button viewLabelButton;

	[ExportGroup("Visual Indicators")]
	[Export] private Control bulletIndicator;
	[Export] private Control anchorIndicator;
	[Export] private Control newEntryIndicator;
	[Export] private Control numberOneIndicator;
	[Export] private ColorRect backgroundRect; // ColorRect instead of Image

	[ExportGroup("Colors")]
	[Export] private Color risingColor = new Color(0.2f, 0.8f, 0.2f);
	[Export] private Color fallingColor = new Color(0.8f, 0.2f, 0.2f);
	[Export] private Color steadyColor = new Color(0.5f, 0.5f, 0.5f);
	[Export] private Color newEntryColor = new Color(0.2f, 0.6f, 1f);
	[Export] private Color numberOneColor = new Color(1f, 0.84f, 0f);

	private RecordRuntimeData currentRecord;
	private List<Control> spawnedTags = new List<Control>();
	private LabelCrest labelDisc;
	private HBoxContainer stampRow;

	public event System.Action<RecordRuntimeData> OnViewArtistClicked;
	public event System.Action<RecordRuntimeData> OnViewLabelClicked;

	public override void _Ready()
	{
		if (closeButton != null) closeButton.Pressed += Close;
		if (viewArtistButton != null) viewArtistButton.Pressed += HandleViewArtist;
		if (viewLabelButton != null) viewLabelButton.Pressed += HandleViewLabel;

		// The card is beige (scene bg 0.91,0.81,0.58) but the scene labels carry no font-color override,
		// so they inherited the near-white default and vanished. Force dark ink on every text label; the
		// position/movement labels set their own per-tier colours during populate, so leave those.
		var ink = new Color("2b2115");
		foreach (Label l in new[] { titleText, artistText, labelGenreText, releaseDateText, chartStatsText,
			recordDescriptionText, chartCommentaryText, regionalHintText, salesSummaryText })
			l?.AddThemeColorOverride("font_color", ink);

		Restyle();
		if (panelRoot != null) panelRoot.Visible = false;
	}

	/// <summary>
	/// The scene gives this card as a flat tan box of default labels. Dress it as a release card on the label's own paper:
	/// the title in a serif, the act typed, the label's 45 in the corner, the chart position circled like the rank on a chart
	/// row, the state of the record struck as stamps, the sales in a ruled box and the reputation tags as typed stickers.
	/// Everything here is presentation; the nodes and the numbers are the scene's.
	/// </summary>
	private void Restyle()
	{
		var folder = GetNodeOrNull<PanelContainer>("Folder");
		var body = GetNodeOrNull<VBoxContainer>("Folder/Margin/Body");
		if (folder == null || body == null) return;
		var ink = new Color("2b2115");
		// The scene's light-theme tints are unreadable on cream; use the same inks as the chart rows.
		risingColor = new Color("2f6b2a"); fallingColor = new Color("a8322a"); steadyColor = new Color("6b5a3a");
		newEntryColor = new Color("24457A"); numberOneColor = new Color("9c6a00");

		var sheet = PaperStyleBox.Sheet(new Color("f1e5c8"), 0, 0, 24, new Color("654a27")).Decorated(clip: true, ring: true, seed: 7);
		sheet.BorderWidth = 2; sheet.Radius = 3; sheet.ShadowAlpha = 0.55f; sheet.ShadowOffset = new Vector2(0, 10);
		folder.AddThemeStyleboxOverride("panel", sheet);
		// The scene fixes the card at 700px tall; let it be as tall as its contents and grow from the middle.
		folder.OffsetTop = 0; folder.OffsetBottom = 0;
		folder.GrowVertical = Control.GrowDirection.Both;
		var margin = GetNodeOrNull<MarginContainer>("Folder/Margin");
		margin?.AddThemeConstantOverride("margin_left", 44);
		margin?.AddThemeConstantOverride("margin_right", 44);
		margin?.AddThemeConstantOverride("margin_top", 34);
		margin?.AddThemeConstantOverride("margin_bottom", 30);

		static void Face(Label label, Font font, int size, Color? colour = null)
		{
			if (label == null) return;
			label.AddThemeFontOverride("font", font);
			label.AddThemeFontSizeOverride("font_size", size);
			if (colour != null) label.AddThemeColorOverride("font_color", colour.Value);
		}
		Face(titleText, PaperTheme.SerifBold, 36, ink);
		Face(artistText, PaperTheme.Elite, 24, ink);
		Face(labelGenreText, PaperTheme.SansSemiBold, 14, PaperTheme.Rust);
		Face(releaseDateText, PaperTheme.SerifItalic, 17, ink);
		Face(chartStatsText, PaperTheme.Typed, 16, ink);
		Face(recordDescriptionText, PaperTheme.Serif, 18, ink);
		Face(chartCommentaryText, PaperTheme.SerifItalic, 17, ink);
		Face(regionalHintText, PaperTheme.Serif, 16, PaperTheme.Rust);
		foreach (Label wrapped in new[] { titleText, chartCommentaryText, regionalHintText })
			if (wrapped != null) wrapped.AutowrapMode = TextServer.AutowrapMode.WordSmart;

		// Header: the words on the left, the label's 45 on the right.
		var head = new HBoxContainer();
		head.AddThemeConstantOverride("separation", 18);
		var headText = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		headText.AddThemeConstantOverride("separation", 2);
		body.AddChild(head);
		body.MoveChild(head, 0);
		head.AddChild(headText);
		foreach (Label line in new[] { titleText, artistText, labelGenreText, releaseDateText })
			if (line != null) line.Reparent(headText, false);
		labelDisc = new LabelCrest { Visible = false };
		head.AddChild(labelDisc);
		var headRule = Rule(2f, new Color(ink, 0.85f));
		body.AddChild(headRule);
		body.MoveChild(headRule, 1);

		// The chart position, circled like the rank on a chart row.
		if (positionText != null)
		{
			Face(positionText, PaperTheme.TypedBold, 42);
			positionText.HorizontalAlignment = HorizontalAlignment.Center;
			positionText.AddThemeStyleboxOverride("normal", new StyleBoxFlat {
				BgColor = new Color(1f, 1f, 1f, 0.28f), BorderColor = ink,
				BorderWidthLeft = 3, BorderWidthRight = 3, BorderWidthTop = 3, BorderWidthBottom = 3,
				CornerRadiusTopLeft = 40, CornerRadiusTopRight = 40, CornerRadiusBottomLeft = 40, CornerRadiusBottomRight = 40,
				ContentMarginLeft = 20, ContentMarginRight = 20, ContentMarginTop = 0, ContentMarginBottom = 2
			});
			positionText.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
		}
		if (movementText != null)
		{
			Face(movementText, PaperTheme.SansBold, 26);
			movementText.VerticalAlignment = VerticalAlignment.Center;
		}
		var chartLine = GetNodeOrNull<HBoxContainer>("Folder/Margin/Body/ChartLine");
		chartLine?.AddThemeConstantOverride("separation", 20);

		// What has happened to the record, struck on the card.
		stampRow = new HBoxContainer { Visible = false };
		stampRow.AddThemeConstantOverride("separation", 14);
		if (chartLine != null) { body.AddChild(stampRow); body.MoveChild(stampRow, chartLine.GetIndex() + 1); }

		// Sales go in a ruled box, set in the typewriter.
		if (salesSummaryText != null)
		{
			Face(salesSummaryText, PaperTheme.Typed, 17, ink);
			salesSummaryText.AddThemeStyleboxOverride("normal", new StyleBoxFlat {
				BgColor = new Color("f9f2dc"), BorderColor = new Color("8c6f38"),
				BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
				ContentMarginLeft = 14, ContentMarginRight = 14, ContentMarginTop = 10, ContentMarginBottom = 10
			});
		}
		tagsContainer?.AddThemeConstantOverride("separation", 10);
	}

	private static ColorRect Rule(float height, Color colour) =>
		new() { Color = colour, CustomMinimumSize = new Vector2(0, height), MouseFilter = Control.MouseFilterEnum.Ignore };

	/// <summary>The label's own 45 in the card's corner.</summary>
	private void UpdateLabelDisc(RecordRuntimeData record)
	{
		if (labelDisc == null) return;
		string id = record.baseRecord.labelId;
		AILabel house = string.IsNullOrEmpty(id) ? null : ChartManager.Instance?.GetLabelById(id) ?? LabelLifecycleManager.Instance?.GetLabelById(id);
		labelDisc.Visible = house != null;
		if (house != null) labelDisc.Set(LabelBrand.For(house), house.labelName, 112f, LabelCrest.Mode.Disc45);
	}

	private void RefreshStamps(RecordRuntimeData record)
	{
		if (stampRow == null) return;
		foreach (Node child in stampRow.GetChildren()) { stampRow.RemoveChild(child); child.QueueFree(); }
		void Stamp(string text, Color colour, float tilt) => stampRow.AddChild(new RubberStamp().Set(text, colour, tilt, 15));
		if (record.currentPosition == 1) Stamp("No. 1", RubberStamp.Red, -0.08f);
		if (record.weeksOnChart == 1) Stamp("New entry", RubberStamp.Blue, 0.05f);
		if (record.isBullet) Stamp("Bullet", new Color("2f6b2a"), -0.04f);
		if (record.isAnchor) Stamp("Anchor", RubberStamp.Red, 0.04f);
		if (record.totalUnitsSold >= 1000000) Stamp("Million seller", RubberStamp.Red, -0.06f);
		else if (record.totalUnitsSold >= 500000) Stamp("Gold record", new Color("9c6a00"), 0.05f);
		stampRow.Visible = stampRow.GetChildCount() > 0;
	}

	// === PUBLIC API ===

	public void Show(RecordRuntimeData record)
	{
		if (record == null) return;

		currentRecord = record;

		PopulateHeader(record);
		UpdateLabelDisc(record);
		PopulateChartPosition(record);
		PopulateNarrativeDescriptions(record);
		PopulateSalesSummary(record);
		PopulateReputationTags(record);
		UpdateVisualIndicators(record);
		RefreshStamps(record);

		if (panelRoot != null) panelRoot.Visible = true;
	}

	public void Close()
	{
		if (panelRoot != null) panelRoot.Visible = false;
		currentRecord = null;
		ClearTags();
	}

	public bool IsOpen => panelRoot != null && panelRoot.Visible;

	// === POPULATION METHODS ===

	private void PopulateHeader(RecordRuntimeData record)
	{
		var baseRecord = record.baseRecord;

		if (titleText != null)
			titleText.Text = $"\"{baseRecord.title}\"";

		if (artistText != null)
			artistText.Text = baseRecord.artistName;

		if (releaseDateText != null)
			releaseDateText.Text = $"Released {baseRecord.releaseDate.ToHeadlineString()}";

		if (labelGenreText != null)
		{
			string label = GetLabelDisplayName(baseRecord.labelId);
			string genre = GenreNameFormatter.Format(baseRecord.primaryGenre);
			labelGenreText.Text = $"{label}  •  {genre}";
		}
	}

	private void PopulateChartPosition(RecordRuntimeData record)
	{
		if (positionText != null)
		{
			if (record.currentPosition > 0)
			{
				positionText.Text = $"#{record.currentPosition}";

				if (record.currentPosition == 1)
					positionText.AddThemeColorOverride("font_color", numberOneColor);
				// The card background is always light (cream/white), so the position must read dark --
				// the old white / light-grey tiers vanished on it.
				else if (record.currentPosition <= 10)
					positionText.AddThemeColorOverride("font_color", new Color("2b2115"));
				else if (record.currentPosition <= 40)
					positionText.AddThemeColorOverride("font_color", new Color("4a3f2f"));
				else
					positionText.AddThemeColorOverride("font_color", new Color("6b5a3a"));
			}
			else
			{
				positionText.Text = "—";
				positionText.AddThemeColorOverride("font_color", steadyColor);
			}
		}

		if (movementText != null)
			PopulateMovementText(record);

		if (chartStatsText != null)
		{
			var sb = new StringBuilder();

			if (record.peakPosition > 0)
				sb.Append($"Peak: #{record.peakPosition}");

			if (record.weeksOnChart > 0)
			{
				if (sb.Length > 0) sb.Append("  |  ");
				sb.Append($"Weeks: {record.weeksOnChart}");
			}

			if (record.lastWeekPosition > 0)
			{
				if (sb.Length > 0) sb.Append("  |  ");
				sb.Append($"Last Week: #{record.lastWeekPosition}");
			}
			else if (record.weeksOnChart == 1)
			{
				if (sb.Length > 0) sb.Append("  |  ");
				sb.Append("NEW ENTRY");
			}

			chartStatsText.Text = sb.ToString();
		}
	}

	private void PopulateMovementText(RecordRuntimeData record)
	{
		if (record.lastWeekPosition == 0 && record.currentPosition > 0)
		{
			movementText.Text = "NEW";
			movementText.AddThemeColorOverride("font_color", newEntryColor);
		}
		else if (record.currentPosition == 0)
		{
			movementText.Text = "OUT";
			movementText.AddThemeColorOverride("font_color", fallingColor);
		}
		else
		{
			int change = record.lastWeekPosition - record.currentPosition;

			if (change > 0)
			{
				movementText.Text = record.isBullet ? $"▲▲{change}" : $"▲{change}";
				movementText.AddThemeColorOverride("font_color", risingColor);
			}
			else if (change < 0)
			{
				movementText.Text = record.isAnchor ? $"▼▼{Mathf.Abs(change)}" : $"▼{Mathf.Abs(change)}";
				movementText.AddThemeColorOverride("font_color", fallingColor);
			}
			else
			{
				movementText.Text = "—";
				movementText.AddThemeColorOverride("font_color", steadyColor);
			}
		}
	}

	private void PopulateNarrativeDescriptions(RecordRuntimeData record)
	{
		if (recordDescriptionText != null)
			recordDescriptionText.Text = JournalisticDescriptor.DescribeRecord(record);

		if (chartCommentaryText != null)
			chartCommentaryText.Text = JournalisticDescriptor.GetChartMovementComment(record);

		if (regionalHintText != null)
		{
			var regions = ChartManager.Instance?.GetAllRegions();
			if (regions != null && regions.Count > 0)
				regionalHintText.Text = JournalisticDescriptor.GetRegionalPerformanceHint(record, regions);
			else
				regionalHintText.Text = "";
		}
	}

	private void PopulateSalesSummary(RecordRuntimeData record)
	{
		if (salesSummaryText == null) return;

		var sb = new StringBuilder();
		sb.Append($"Sold {FormatNumber(record.unitsThisWeek)} units this week");

		if (record.unitsPreviousWeek > 0)
		{
			float changePercent = ((float)record.unitsThisWeek / record.unitsPreviousWeek - 1f) * 100f;

			// Godot uses BBCode for rich text - requires RichTextLabel instead of Label
			// For now we'll keep it plain
			if (changePercent >= 0)
				sb.Append($" (+{changePercent:F0}%)");
			else
				sb.Append($" ({changePercent:F0}%)");
		}

		sb.AppendLine();
		sb.Append($"Total sales: {FormatNumber(record.totalUnitsSold)}");
		sb.AppendLine();
		sb.Append(GetSalesTierDescription(record.totalUnitsSold));

		salesSummaryText.Text = sb.ToString();
	}

	public static string GetSalesTierDescription(int totalSales)
	{
		if (totalSales >= 1000000) return "★ MILLION SELLER ★";
		else if (totalSales >= 500000) return "Gold Record territory.";
		else if (totalSales >= 250000) return "A certified hit.";
		else if (totalSales >= 100000) return "Solid commercial performance.";
		else if (totalSales >= 50000) return "Respectable sales.";
		else if (totalSales >= 10000) return "Modest returns so far.";
		else return "Still finding its audience.";
	}

	private void PopulateReputationTags(RecordRuntimeData record)
	{
		ClearTags();
		if (tagsContainer == null) return;

		var tags = GenerateRecordTags(record);

		foreach (var tag in tags)
		{
			// Create a simple Label as a tag
				var tagLabel = new Label();
			tagLabel.Text = tag.ToDisplayString();
			// Tags are typed stickers stuck on the card.
			tagLabel.AddThemeColorOverride("font_color", new Color("2b2115"));
			tagLabel.AddThemeFontOverride("font", PaperTheme.Elite);
			tagLabel.AddThemeFontSizeOverride("font_size", 15);
			tagLabel.AddThemeStyleboxOverride("normal", new StyleBoxFlat {
				BgColor = new Color("f9f2dc"), BorderColor = new Color(0.44f, 0.33f, 0.17f, 0.6f),
				BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
				ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 3, ContentMarginBottom = 3
			});
			tagsContainer.AddChild(tagLabel);
			spawnedTags.Add(tagLabel);
		}
	}

	private List<ReputationTag> GenerateRecordTags(RecordRuntimeData record)
	{
		var tags = new List<ReputationTag>();
		var r = record.baseRecord;

		if (record.currentPosition == 1) tags.Add(ReputationTag.HitMachine);
		else if (record.peakPosition <= 10 && record.weeksOnChart >= 10) tags.Add(ReputationTag.MainstreamAppeal);

		if (record.isBullet) tags.Add(ReputationTag.RisingStar);
		if (record.weeksOnChart >= 20) tags.Add(ReputationTag.Established);
		if (record.weeksOnChart == 1 && record.currentPosition <= 40) tags.Add(ReputationTag.ArtistToWatch);

		if (r.hookStrength > 0.8f) tags.Add(ReputationTag.RadioFriendly);
		if (r.originality > 0.8f) tags.Add(ReputationTag.Innovator);
		else if (r.originality < 0.25f) tags.Add(ReputationTag.Derivative);
		if (r.controversy > 0.6f) tags.Add(ReputationTag.Controversial);
		if (r.productionQuality > 0.85f) tags.Add(ReputationTag.Professional);

		if (tags.Count > 4) tags = tags.GetRange(0, 4);

		return tags;
	}

	private void ClearTags()
	{
		foreach (var tag in spawnedTags)
		{
			if (tag != null) tag.QueueFree(); // QueueFree() is Godot's Destroy()
		}
		spawnedTags.Clear();
	}

	private void UpdateVisualIndicators(RecordRuntimeData record)
	{
		if (bulletIndicator != null) bulletIndicator.Visible = record.isBullet;
		if (anchorIndicator != null) anchorIndicator.Visible = record.isAnchor;
		if (newEntryIndicator != null) newEntryIndicator.Visible = record.weeksOnChart == 1;
		if (numberOneIndicator != null) numberOneIndicator.Visible = record.currentPosition == 1;

		if (backgroundRect != null)
		{
			if (record.currentPosition == 1)
				backgroundRect.Color = new Color(1f, 0.98f, 0.9f);
			else if (record.isBullet)
				backgroundRect.Color = new Color(0.95f, 1f, 0.95f);
			else if (record.isAnchor)
				backgroundRect.Color = new Color(1f, 0.95f, 0.95f);
			else
				backgroundRect.Color = Colors.White;
		}
	}

	// === BUTTON HANDLERS ===

	private void HandleViewArtist()
	{
		if (currentRecord != null)
		{
			OnViewArtistClicked?.Invoke(currentRecord);
			GD.Print($"View Artist: {currentRecord.baseRecord.artistName}");
		}
	}

	private void HandleViewLabel()
	{
		if (currentRecord != null)
		{
			OnViewLabelClicked?.Invoke(currentRecord);
			GD.Print($"View Label: {currentRecord.baseRecord.labelId}");
		}
	}

	// === FORMATTING HELPERS ===

	private string GetLabelDisplayName(string labelId)
	{
		if (string.IsNullOrEmpty(labelId)) return "Independent";

		if (ChartManager.Instance != null)
		{
			string labelName = ChartManager.Instance.GetLabelName(labelId);
			if (labelName != labelId) return labelName;
		}

		if (LabelLifecycleManager.Instance != null)
		{
			var label = LabelLifecycleManager.Instance.GetLabelById(labelId);
			if (label != null) return label.labelName;
		}

		return labelId;
	}

	private string FormatNumber(int number)
	{
		if (number >= 1000000) return $"{number / 1000000f:F2}M";
		else if (number >= 1000) return $"{number / 1000f:F1}K";
		return number.ToString("N0");
	}
}
