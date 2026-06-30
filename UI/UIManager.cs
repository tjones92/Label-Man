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
	private Button calendarButton;
	private PopupPanel calendarPopup;
	private SpinBox skipDaysInput;

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
		calendarButton = GetNodeOrNull<Button>("CalendarBtn");
		if (calendarButton != null) {
			calendarButton.GuiInput += OnCalendarGuiInput;
			UpdateCalendarButton(TimeManager.Instance?.CurrentDate ?? GameDate.StartDate);
		}
		if (TimeManager.Instance != null) TimeManager.Instance.OnDayStarted += UpdateCalendarButton;
	}

	public override void _ExitTree()
	{
		if (TimeManager.Instance != null) TimeManager.Instance.OnDayStarted -= UpdateCalendarButton;
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
		TimeManager.Instance.SkipDays(1);
		GD.Print($"Calendar advanced to {TimeManager.Instance.CurrentDate.ToLongString()}.");
	}

	private void OnCalendarGuiInput(InputEvent @event)
	{
		if (@event is not InputEventMouseButton mouse || !mouse.Pressed || mouse.ButtonIndex != MouseButton.Right) return;
		ShowCalendarPopup();
		calendarButton.AcceptEvent();
	}

	private void ShowCalendarPopup()
	{
		if (TimeManager.Instance == null) return;
		if (calendarPopup == null) BuildCalendarPopup();

		var nextEvent = TimeManager.Instance.GetNextEvent();
		var nextButton = calendarPopup.GetNode<Button>("Margin/Options/NextEvent");
		nextButton.Text = nextEvent == null
			? "No upcoming event"
			: $"Skip to next event ({nextEvent.title}, {nextEvent.date.ToHeadlineString()})";
		nextButton.Disabled = nextEvent == null;
		calendarPopup.PopupCentered(new Vector2I(500, 300));
	}

	private void BuildCalendarPopup()
	{
		calendarPopup = new PopupPanel { Name = "CalendarPopup" };
		AddChild(calendarPopup);
		var margin = new MarginContainer { Name = "Margin" };
		margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		margin.AddThemeConstantOverride("margin_left", 22);
		margin.AddThemeConstantOverride("margin_right", 22);
		margin.AddThemeConstantOverride("margin_top", 18);
		margin.AddThemeConstantOverride("margin_bottom", 18);
		calendarPopup.AddChild(margin);
		var options = new VBoxContainer { Name = "Options" };
		options.AddThemeConstantOverride("separation", 10);
		margin.AddChild(options);
		var title = new Label { Text = "ADVANCE CALENDAR" };
		title.AddThemeFontSizeOverride("font_size", 22);
		options.AddChild(title);

		var friday = new Button { Text = "Skip to Friday" };
		friday.Pressed += () => RunCalendarSkip(() => TimeManager.Instance.SkipToFriday());
		options.AddChild(friday);
		var next = new Button { Name = "NextEvent", Text = "Skip to next event" };
		next.Pressed += () => RunCalendarSkip(() => TimeManager.Instance.SkipToNextEvent());
		options.AddChild(next);

		var daysRow = new HBoxContainer();
		skipDaysInput = new SpinBox { MinValue = 1, MaxValue = 365, Value = 7, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		daysRow.AddChild(skipDaysInput);
		var skipDays = new Button { Text = "Skip days" };
		skipDays.Pressed += () => RunCalendarSkip(() => TimeManager.Instance.SkipDays((int)skipDaysInput.Value));
		daysRow.AddChild(skipDays);
		options.AddChild(daysRow);
		var close = new Button { Text = "Close" };
		close.Pressed += calendarPopup.Hide;
		options.AddChild(close);
	}

	private void RunCalendarSkip(System.Func<ScheduledEvent> skip)
	{
		calendarPopup.Hide();
		var interruptedBy = skip();
		string reason = interruptedBy == null ? "" : $" (stopped for {interruptedBy.title})";
		GD.Print($"Calendar advanced to {TimeManager.Instance.CurrentDate.ToLongString()}{reason}.");
	}

	private void UpdateCalendarButton(GameDate date)
	{
		if (calendarButton != null) calendarButton.Text = date.ToHeadlineString();
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
