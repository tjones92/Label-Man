using Godot;

public partial class UIManager : Control
{
	public static UIManager Instance;

	[ExportGroup("Views")]
	[Export] private Control ledgerPanel;
	[Export] private Control dialoguePanel;
	[Export] private Control chartPanel;
	[Export] private ArtistDetailPanel artistDetailPanel;
	[Export] private LabelDetailPanel labelDetailPanel;

	[ExportGroup("State")]
	public bool isUIOpen = false;

	public override void _EnterTree()
	{
		Instance = this;
	}

	public override void _Ready()
	{
		if (artistDetailPanel != null) artistDetailPanel.LabelRequested += id => OpenLabel(id);
		if (labelDetailPanel != null) labelDetailPanel.ArtistRequested += id => OpenArtist(id);
	}

	public void OpenArtist(string artistId, bool isOwnedByPlayer = false)
	{
		if (string.IsNullOrEmpty(artistId) || artistDetailPanel == null) return;
		artistDetailPanel.ShowArtist(artistId, isOwnedByPlayer);
		isUIOpen = true;
	}

	public void OpenLabel(string labelId, bool isOwnedByPlayer = false)
	{
		if (string.IsNullOrEmpty(labelId) || labelDetailPanel == null) return;
		labelDetailPanel.ShowLabel(labelId, isOwnedByPlayer);
		isUIOpen = true;
	}

	public void OnClick_Ledger()
	{
		if (isUIOpen) return;
		OpenPanel(ledgerPanel);
		GD.Print("Ledger Opened: Time to check the books.");
	}

	public void OnClick_Charts()
	{
		if (isUIOpen) return;

		if (chartPanel != null)
		{
			chartPanel.Visible = true;
			isUIOpen = true;

			// Assuming ChartUI script is attached directly to the chartPanel node
			var chartController = chartPanel as ChartUI;
			if (chartController != null)
			{
				chartController.OpenChart();
			}
			else
			{
				GD.PushWarning("ChartUI component not found on chartPanel!");
			}
			
			GD.Print("Charts Opened: Viewing the Hot 100.");
		}
		else
		{
			GD.PushWarning("Chart Panel is not assigned in UIManager!");
		}
	}

	public void OnClick_Phone()
	{
		if (isUIOpen) return;
		OpenPanel(dialoguePanel);
		GD.Print("Phone Answered: Narrative event starting...");
	}

	public void OnClick_Calendar()
	{
		if (isUIOpen) return;
		if (TimeManager.Instance == null) return;

		var interruptedBy = TimeManager.Instance.SkipToFriday();
		if (interruptedBy != null)
			GD.Print($"Calendar advanced to {TimeManager.Instance.CurrentDate.ToLongString()} for {interruptedBy.title}.");
		else
			GD.Print($"Calendar advanced to Friday, {TimeManager.Instance.CurrentDate.ToLongString()}.");
	}

	public void OnClick_CloseAll()
	{
		if (ledgerPanel != null) ledgerPanel.Visible = false;
		if (dialoguePanel != null) dialoguePanel.Visible = false;
		if (chartPanel != null) chartPanel.Visible = false;
		if (artistDetailPanel != null) artistDetailPanel.Visible = false;
		if (labelDetailPanel != null) labelDetailPanel.Visible = false;
		
		isUIOpen = false;
	}

	private void OpenPanel(Control panel)
	{
		if (panel != null)
		{
			panel.Visible = true;
			isUIOpen = true;
		}
	}
}
