using System.Collections.Generic;
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

	public override void _Ready()
	{
		if (nextPageButton != null) nextPageButton.Pressed += NextPage;
		if (prevPageButton != null) prevPageButton.Pressed += PrevPage;
		if (closeButton != null) closeButton.Pressed += CloseChart;

		if (ChartManager.Instance != null)
			ChartManager.Instance.OnChartCalculated += UpdateDisplay;

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
		if (ChartManager.Instance != null)
			ChartManager.Instance.OnChartCalculated -= UpdateDisplay;

		if (slots != null)
		{
			foreach (var slot in slots)
			{
				if (slot != null) slot.OnEntryClicked -= HandleEntryClicked;
			}
		}
	}

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
		if ((currentPage + 1) * ITEMS_PER_PAGE < 100)
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

		if (chartData == null)
			chartData = ChartManager.Instance.GetCurrentChart();

		if (dateHeader != null && TimeManager.Instance != null)
			dateHeader.Text = TimeManager.Instance.CurrentDate.ToLongString();

		int startIndex = currentPage * ITEMS_PER_PAGE;

		for (int i = 0; i < slots.Length; i++)
		{
			int recordIndex = startIndex + i;

			if (recordIndex < chartData.Count)
			{
				slots[i].Visible = true;
				slots[i].Populate(chartData[recordIndex]);
			}
			else
			{
				slots[i].Clear();
			}
		}

		if (prevPageButton != null) prevPageButton.Disabled = currentPage <= 0;
		if (nextPageButton != null) nextPageButton.Disabled = (currentPage + 1) * ITEMS_PER_PAGE >= chartData.Count;
	}
}
