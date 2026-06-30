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

	public event System.Action<RecordRuntimeData> OnViewArtistClicked;
	public event System.Action<RecordRuntimeData> OnViewLabelClicked;

	public override void _Ready()
	{
		if (closeButton != null) closeButton.Pressed += Close;
		if (viewArtistButton != null) viewArtistButton.Pressed += HandleViewArtist;
		if (viewLabelButton != null) viewLabelButton.Pressed += HandleViewLabel;

		if (panelRoot != null) panelRoot.Visible = false;
	}

	// === PUBLIC API ===

	public void Show(RecordRuntimeData record)
	{
		if (record == null) return;

		currentRecord = record;

		PopulateHeader(record);
		PopulateChartPosition(record);
		PopulateNarrativeDescriptions(record);
		PopulateSalesSummary(record);
		PopulateReputationTags(record);
		UpdateVisualIndicators(record);

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
				else if (record.currentPosition <= 10)
					positionText.AddThemeColorOverride("font_color", Colors.White);
				else if (record.currentPosition <= 40)
					positionText.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f));
				else
					positionText.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
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
			tagLabel.AddThemeColorOverride("font_color", Colors.White);
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
