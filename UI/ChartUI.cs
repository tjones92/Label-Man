using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class ChartUI : Control
{
	[ExportGroup("Configuration")]
	[Export] private Control chartPanel;
	[Export] private Label dateHeader;

	[ExportGroup("The 30 Slots")]
	[Export] private ChartEntryUI[] slots;

	[ExportGroup("Navigation")]
	[Export] private Button nextPageButton;
	[Export] private Button prevPageButton;
	[Export] private Button closeButton;

	[ExportGroup("Detail Panel")]
	[Export] private ChartDetailPanel detailPanel;

	private int currentPage = 0;
	private const int ITEMS_PER_PAGE = 30;

	// The trade sheet prints several lists; the tabs on its right edge choose which one is on the page.
	private TradeChartKind kind = TradeChartKind.Hot100;
	private string regionId;
	private TradeChartView view = new();
	private readonly Dictionary<TradeChartKind, Button> tabButtons = new();
	private readonly Dictionary<string, Button> regionButtons = new();
	private HBoxContainer regionRow;
	private Label titleLabel, noteLabel;

	private static readonly Dictionary<TradeChartKind, Color> TabInk = new() {
		{ TradeChartKind.Hot100, new Color("1b2a52") },        // navy: the flagship
		{ TradeChartKind.TopLps, new Color("5a2d4a") },        // plum
		{ TradeChartKind.RnB, new Color("8f2a22") },           // oxblood
		{ TradeChartKind.Country, new Color("8a5a1c") },       // saddle brown
		{ TradeChartKind.EasyListening, new Color("4f5c2c") }, // olive
		{ TradeChartKind.Regional, new Color("2f5560") },      // slate teal
	};

	public override void _Ready()
	{
		// The chart sheet is paper under the same lamp as the office: grain, scorched edges, a contact shadow.
		if (GetNodeOrNull<PanelContainer>("ChartPaper") is PanelContainer paper) {
			var sheet = PaperStyleBox.Sheet(new Color("f0e4c3"), 24, 20, 22, new Color("6a4c29"));
			sheet.BorderWidth = 2;
			paper.AddThemeStyleboxOverride("panel", sheet);
		}
		titleLabel = GetNodeOrNull<Label>("ChartTitle");
		if (titleLabel != null) titleLabel.AddThemeFontOverride("font", PaperTheme.SansBold);
		BuildTabs();
		BuildRegionRow();
		BuildNoteLabel();
		if (nextPageButton != null) nextPageButton.Pressed += NextPage;
		if (prevPageButton != null) prevPageButton.Pressed += PrevPage;
		if (closeButton != null) closeButton.Pressed += CloseChart;
		if (detailPanel != null) {
			detailPanel.OnViewArtistClicked += HandleViewArtist;
			detailPanel.OnViewLabelClicked += HandleViewLabel;
		}

		if (ChartManager.Instance != null) {
			ChartManager.Instance.OnChartCalculated += UpdateDisplay;
			ChartManager.Instance.OnAlbumChartCalculated += UpdateDisplay;
		}

		if (slots != null)
		{
			foreach (var slot in slots)
			{
				if (slot != null) slot.OnEntryClicked += HandleEntryClicked;
			}
		}
	}

	public override void _ExitTree()
	{
		if (detailPanel != null) {
			detailPanel.OnViewArtistClicked -= HandleViewArtist;
			detailPanel.OnViewLabelClicked -= HandleViewLabel;
		}
		if (ChartManager.Instance != null) {
			ChartManager.Instance.OnChartCalculated -= UpdateDisplay;
			ChartManager.Instance.OnAlbumChartCalculated -= UpdateDisplay;
		}

		if (slots != null)
		{
			foreach (var slot in slots)
			{
				if (slot != null) slot.OnEntryClicked -= HandleEntryClicked;
			}
		}
	}

	private void HandleViewArtist(RecordRuntimeData record) => UIManager.Instance?.OpenArtist(record?.baseRecord?.artistId, record?.baseRecord?.isPlayerOwned ?? false);
	private void HandleViewLabel(RecordRuntimeData record) => UIManager.Instance?.OpenLabel(record?.baseRecord?.labelId, record?.baseRecord?.isPlayerOwned ?? false);

	private void HandleEntryClicked(RecordRuntimeData record)
	{
		GD.Print($"Entry clicked: {record?.baseRecord?.title ?? "NULL"}");

		if (detailPanel != null && record != null)
		{
			detailPanel.Show(record);
		}
		else
		{
			GD.PrintErr($"Cannot show detail! detailPanel assigned: {detailPanel != null}, record: {record != null}");
		}
	}

	public void OpenChart()
	{
		if (chartPanel == null)
		{
			// Assume this node IS the panel
			Visible = true;
			currentPage = 0;
			UpdateDisplay(null);
			return;
		}

		chartPanel.Visible = true;
		currentPage = 0;
		UpdateDisplay(null);
	}

	public void CloseChart()
	{
		if (chartPanel != null) chartPanel.Visible = false;
		else Visible = false;

		if (detailPanel != null && detailPanel.IsOpen)
			detailPanel.Close();
	}

	private void NextPage()
	{
		if ((currentPage + 1) * ITEMS_PER_PAGE < view.Rows.Count)
		{
			currentPage++;
			UpdateDisplay(null);
		}
	}

	private void PrevPage()
	{
		if (currentPage > 0)
		{
			currentPage--;
			UpdateDisplay(null);
		}
	}

	private void UpdateDisplay(List<RecordRuntimeData> chartData)
	{
		bool isActive = chartPanel != null ? chartPanel.Visible : Visible;
		if (!isActive) return;

		GameDate today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		if (regionId == null) regionId = ChartManager.Instance?.GetAllRegions().FirstOrDefault()?.regionId;
		view = TradeCharts.Build(kind, regionId, today);
		int count = view.Rows.Count;
		int pages = Math.Max(1, (count + ITEMS_PER_PAGE - 1) / ITEMS_PER_PAGE);
		currentPage = Math.Clamp(currentPage, 0, pages - 1);

		// The chart is dated to the week-ending Saturday it was computed for, frozen until the next weekly
		// recompute -- not today's date, which would tick every day the panel is reopened.
		int startIndex = currentPage * ITEMS_PER_PAGE;
		int endRank = Math.Min(startIndex + ITEMS_PER_PAGE, count);
		if (titleLabel != null) titleLabel.Text = view.Title;
		if (dateHeader != null && ChartManager.Instance != null) {
			string week = $"WEEK ENDING {ChartManager.Instance.ChartWeekEndingDate.ToLongString().ToUpperInvariant()}";
			dateHeader.Text = count == 0 ? week : $"{week}   •   {view.Noun} #{startIndex + 1}–#{endRank}";
		}
		if (noteLabel != null) { noteLabel.Text = view.Note ?? ""; noteLabel.Visible = count == 0 && !string.IsNullOrEmpty(view.Note); }

		bool alternate = kind != TradeChartKind.Hot100 && kind != TradeChartKind.TopLps;
		for (int i = 0; i < slots.Length; i++)
		{
			int rowIndex = startIndex + i;

			if (rowIndex < count)
			{
				TradeChartRow row = view.Rows[rowIndex];
				slots[i].Visible = true;
				slots[i].Populate(row.Record, row.Rank, row.LastRank, alternate);
			}
			else
			{
				slots[i].Clear();
			}
		}

		if (prevPageButton != null) prevPageButton.Disabled = currentPage <= 0;
		if (nextPageButton != null) nextPageButton.Disabled = (currentPage + 1) * ITEMS_PER_PAGE >= count;
		RefreshTabs();
	}

	// ---- the tabs on the sheet's right edge ----------------------------------------------------------

	private void BuildTabs()
	{
		var column = new VBoxContainer { Name = "TradeTabs", Position = new Vector2(1478f, 150f) };
		column.AddThemeConstantOverride("separation", 7);
		AddChild(column);
		// Behind the paper, so each tab's inner edge is tucked under the sheet like a binder divider.
		if (GetNodeOrNull("ChartPaper") is Node paper) MoveChild(column, paper.GetIndex());
		foreach (TradeChartKind each in Enum.GetValues<TradeChartKind>())
		{
			TradeChartKind captured = each;
			var tab = new Button {
				Text = TradeCharts.TabName(each), FocusMode = FocusModeEnum.None, Alignment = HorizontalAlignment.Left,
				SizeFlagsHorizontal = SizeFlags.ShrinkBegin, MouseDefaultCursorShape = CursorShape.PointingHand,
				TooltipText = each == TradeChartKind.EasyListening ? "First printed July 1961." : ""
			};
			tab.Pressed += () => SelectKind(captured);
			column.AddChild(tab);
			tabButtons[each] = tab;
		}
		RefreshTabs();
	}

	private void RefreshTabs()
	{
		foreach (var pair in tabButtons)
		{
			bool open = pair.Key == kind;
			Color ink = TabInk[pair.Key];
			Button tab = pair.Value;
			tab.CustomMinimumSize = new Vector2(open ? 208f : 184f, 46f);
			var box = new StyleBoxFlat {
				BgColor = open ? ink : ink.Darkened(0.12f).Lerp(PaperTheme.Ink, 0.18f),
				BorderColor = new Color("2b2115"), CornerRadiusTopRight = 9, CornerRadiusBottomRight = 9,
				BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
				ContentMarginLeft = 26, ContentMarginRight = 10, ContentMarginTop = 6, ContentMarginBottom = 6,
				ShadowColor = new Color(0, 0, 0, 0.35f), ShadowSize = 5, ShadowOffset = new Vector2(2, 3)
			};
			foreach (string state in new[] { "normal", "pressed", "disabled", "focus" }) tab.AddThemeStyleboxOverride(state, box);
			var hover = (StyleBoxFlat)box.Duplicate();
			hover.BgColor = box.BgColor.Lightened(0.15f);
			tab.AddThemeStyleboxOverride("hover", hover);
			tab.AddThemeFontOverride("font", PaperTheme.SansBold);
			tab.AddThemeFontSizeOverride("font_size", open ? 16 : 14);
			foreach (string name in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_focus_color" })
				tab.AddThemeColorOverride(name, new Color("f6ecd0"));
		}
		if (regionRow != null) regionRow.Visible = kind == TradeChartKind.Regional;
		foreach (var pair in regionButtons)
		{
			bool open = pair.Key == regionId;
			Button chip = pair.Value;
			var box = new StyleBoxFlat {
				BgColor = open ? TabInk[TradeChartKind.Regional] : new Color("e4d6b0"),
				BorderColor = new Color("6a4c29"), CornerRadiusTopLeft = 5, CornerRadiusTopRight = 5, CornerRadiusBottomLeft = 5, CornerRadiusBottomRight = 5,
				BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
				ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 5, ContentMarginBottom = 5
			};
			foreach (string state in new[] { "normal", "pressed", "disabled", "focus", "hover" }) chip.AddThemeStyleboxOverride(state, box);
			foreach (string name in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_focus_color" })
				chip.AddThemeColorOverride(name, open ? new Color("f6ecd0") : PaperTheme.Ink);
		}
	}

	private void BuildRegionRow()
	{
		regionRow = new HBoxContainer { Name = "RegionRow", Position = new Vector2(135f, 712f), Visible = false };
		regionRow.AddThemeConstantOverride("separation", 8);
		AddChild(regionRow);
		var heading = new Label { Text = "MARKET:", VerticalAlignment = VerticalAlignment.Center };
		heading.AddThemeFontOverride("font", PaperTheme.SansBold);
		heading.AddThemeFontSizeOverride("font_size", 13);
		heading.AddThemeColorOverride("font_color", PaperTheme.Rust);
		regionRow.AddChild(heading);
		if (ChartManager.Instance == null) return;
		foreach (MarketRegion region in ChartManager.Instance.GetAllRegions())
		{
			string id = region.regionId;
			var chip = new Button { Text = region.regionName, FocusMode = FocusModeEnum.None, MouseDefaultCursorShape = CursorShape.PointingHand };
			chip.AddThemeFontOverride("font", PaperTheme.SansSemiBold);
			chip.AddThemeFontSizeOverride("font_size", 13);
			chip.Pressed += () => { regionId = id; currentPage = 0; UpdateDisplay(null); };
			regionRow.AddChild(chip);
			regionButtons[id] = chip;
		}
	}

	private void BuildNoteLabel()
	{
		noteLabel = new Label {
			Name = "TradeNote", Position = new Vector2(135f, 330f), Size = new Vector2(1320f, 60f), Visible = false,
			HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore
		};
		noteLabel.AddThemeFontOverride("font", PaperTheme.SerifItalic);
		noteLabel.AddThemeFontSizeOverride("font_size", 20);
		noteLabel.AddThemeColorOverride("font_color", PaperTheme.Fade);
		AddChild(noteLabel);
	}

	private void SelectKind(TradeChartKind next)
	{
		if (next == kind) return;
		kind = next;
		currentPage = 0;
		if (detailPanel != null && detailPanel.IsOpen) detailPanel.Close();
		UpdateDisplay(null);
	}
}
