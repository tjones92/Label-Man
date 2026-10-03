using Godot;

public partial class ChartEntryUI : Control
{
	[ExportGroup("Text Fields")]
	[Export] private Label rankText;
	[Export] private Label songText;
	[Export] private Label artistText;
	[Export] private Label labelText;

	private RecordRuntimeData myRecord;
	public System.Action<RecordRuntimeData> OnEntryClicked;

	public override void _Ready()
	{
		// Make this control detect mouse input
		MouseFilter = MouseFilterEnum.Stop;
	}

	public override void _Draw() {
		if (myRecord?.baseRecord?.isPlayerOwned != true) return;
		var bounds = new Rect2(Vector2.Zero, Size);
		DrawRect(bounds, new Color("e2efd9"));
		DrawRect(bounds, new Color("608050"), false, 1.5f);
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

		if (rankText != null) rankText.Text = record.currentPosition.ToString();
		if (songText != null) songText.Text = record.baseRecord.title;
		if (artistText != null) artistText.Text = record.baseRecord.artistName;
		if (labelText != null) labelText.Text = playerOwned ? $"YOU · {GetLabelAbbrev(record.baseRecord.labelId)}" : GetLabelAbbrev(record.baseRecord.labelId);
		QueueRedraw();

		// Color code rank text
		if (rankText != null)
		{
			if (playerOwned) rankText.AddThemeColorOverride("font_color", new Color("184f2a"));
			else if (record.isBullet) rankText.AddThemeColorOverride("font_color", Colors.Green);
			else if (record.isAnchor) rankText.AddThemeColorOverride("font_color", Colors.Red);
			else rankText.AddThemeColorOverride("font_color", Colors.Black);
		}
	}

	public void Clear()
	{
		myRecord = null;
		Modulate = Colors.White;
		if (rankText != null) rankText.Text = "";
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
