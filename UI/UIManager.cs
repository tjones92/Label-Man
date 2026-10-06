using Godot;
using System.Linq;

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
	private PlayerDeskPanel deskPanel;
	private Button deskButton;
	private PanelContainer mainHud;
	private Label hudDateCash, hudTicker;
	private Control morningPaper;
	private Label paperDate, paperStories;
	private ScrollContainer paperScroll;
	private bool paperQueued, uiOpenBeforePaper;

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
		if (TimeManager.Instance != null) {
			TimeManager.Instance.OnDayStarted += OnMorningStarted;
			TimeManager.Instance.OnClockRestored += OnClockRestored;
			TimeManager.Instance.OnDayStarted += UpdateCalendarButton;
			TimeManager.Instance.OnClockRestored += UpdateCalendarButton;
		}

		deskPanel = GetNodeOrNull<PlayerDeskPanel>("PlayerDeskPanel");
		deskButton = GetNodeOrNull<Button>("DeskBtn");
		if (deskButton != null) deskButton.Pressed += OnClick_Desk;
		BuildMainHud();
		BuildMorningPaper();
		if (PlayerDesk.Instance != null) PlayerDesk.Instance.Changed += UpdateMainHud;
		if (TimeManager.Instance != null) TimeManager.Instance.OnHourChanged += _ => UpdateMainHud();
		UpdateMainHud();
	}

	private void BuildMainHud() {
		mainHud = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore, ZIndex = 10, Size = new Vector2(510, 86) };
		mainHud.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft);
		// The desk shortcut occupies the upper corner. Keep the news underneath its hit area.
		mainHud.Position = new Vector2(18, 110);
		mainHud.CustomMinimumSize = new Vector2(510, 86);
		mainHud.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.96f, 0.91f, 0.78f, 0.91f), ContentMarginLeft = 14, ContentMarginRight = 14, ContentMarginTop = 8, ContentMarginBottom = 8 });
		var stack = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		hudDateCash = new Label { MouseFilter = Control.MouseFilterEnum.Ignore };
		hudDateCash.AddThemeFontSizeOverride("font_size", 17);
		hudDateCash.AddThemeColorOverride("font_color", new Color("2b2115"));
		hudTicker = new Label { MouseFilter = Control.MouseFilterEnum.Ignore, ClipText = true, CustomMinimumSize = new Vector2(480, 0) };
		hudTicker.AddThemeColorOverride("font_color", new Color("6b3a1c"));
		stack.AddChild(hudDateCash);
		stack.AddChild(hudTicker);
		mainHud.AddChild(stack);
		AddChild(mainHud);
	}

	private void UpdateMainHud() {
		if (hudDateCash == null) return;
		mainHud.Visible = !isUIOpen;
		TimeManager time = TimeManager.Instance;
		PlayerDesk desk = PlayerDesk.Instance;
		string cash = desk?.Label == null ? "" : $"  •  ${desk.Label.cashReserves:N0} cash";
		hudDateCash.Text = time == null ? "" : $"{time.CurrentDate.ToHeadlineString()}  •  {time.GetTimeString()}{cash}";
		hudTicker.Text = desk?.Log.FirstOrDefault() ?? "No office news yet.";
	}

	public void RefreshMainHud() => UpdateMainHud();

	private void OnClockRestored(GameDate date) {
		DismissMorningPaper();
		UpdateMainHud();
	}

	private void OnMorningStarted(GameDate date) {
		UpdateMainHud();
		// Wait for daily simulation listeners, and coalesce a multi-day calendar skip into one paper.
		if (paperQueued) return;
		paperQueued = true;
		Callable.From(() => {
			paperQueued = false;
			PlayerDesk desk = PlayerDesk.Instance;
			if (desk?.HasLabel != true || desk.IsGameOver || string.IsNullOrEmpty(desk.MorningDigest)) return;
			paperDate.Text = TimeManager.Instance.CurrentDate.ToLongString().ToUpperInvariant();
			string digest = desk.MorningDigest;
			int dateEnd = digest.IndexOf(": ", System.StringComparison.Ordinal);
			if (dateEnd >= 0) digest = digest.Substring(dateEnd + 2);
			paperStories.Text = string.Join("\n\n", digest.Split("  •  ", System.StringSplitOptions.RemoveEmptyEntries));
			paperScroll.ScrollVertical = 0;
			if (!morningPaper.Visible) uiOpenBeforePaper = isUIOpen;
			morningPaper.Show();
			morningPaper.MoveToFront();
			isUIOpen = true;
			UpdateMainHud();
		}).CallDeferred();
	}

	private void BuildMorningPaper() {
		morningPaper = new Control { Name = "MorningPaper", Visible = false, ZIndex = 30, MouseFilter = MouseFilterEnum.Stop };
		morningPaper.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(morningPaper);
		var shade = new ColorRect { Color = new Color(0, 0, 0, .55f) };
		shade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		shade.GuiInput += input => {
			if (input is InputEventMouseButton mouse && mouse.Pressed && mouse.ButtonIndex == MouseButton.Left) {
				DismissMorningPaper();
				shade.AcceptEvent();
			}
		};
		morningPaper.AddChild(shade);
		var sheet = new PanelContainer { MouseFilter = MouseFilterEnum.Stop };
		sheet.SetAnchorsPreset(LayoutPreset.Center);
		sheet.Position = new Vector2(-380, -310);
		sheet.Size = new Vector2(760, 620);
		sheet.AddThemeStyleboxOverride("panel", new StyleBoxFlat {
			BgColor = new Color("eee2c5"), BorderColor = new Color("867655"),
			BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
			ContentMarginLeft = 38, ContentMarginRight = 38, ContentMarginTop = 30, ContentMarginBottom = 26,
			ShadowColor = new Color(0, 0, 0, .4f), ShadowSize = 12
		});
		morningPaper.AddChild(sheet);
		var column = new VBoxContainer();
		column.AddThemeConstantOverride("separation", 16);
		sheet.AddChild(column);
		var serif = new SystemFont { FontNames = new[] { "Georgia", "Times New Roman" } };
		Label NewspaperText(string text, int size, HorizontalAlignment alignment) {
			var label = new Label { Text = text, HorizontalAlignment = alignment, AutowrapMode = TextServer.AutowrapMode.WordSmart };
			label.AddThemeFontOverride("font", serif);
			label.AddThemeFontSizeOverride("font_size", size);
			label.AddThemeColorOverride("font_color", new Color("30291d"));
			return label;
		}
		column.AddChild(NewspaperText("The Morning Paper", 42, HorizontalAlignment.Center));
		column.AddChild(new HSeparator());
		paperDate = NewspaperText("", 16, HorizontalAlignment.Center);
		column.AddChild(paperDate);
		column.AddChild(new HSeparator());
		column.AddChild(NewspaperText("YESTERDAY AT THE LABEL", 20, HorizontalAlignment.Left));
		paperScroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
		column.AddChild(paperScroll);
		paperStories = NewspaperText("", 21, HorizontalAlignment.Left);
		paperStories.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		paperScroll.AddChild(paperStories);
		var dismiss = new Button { Text = "FOLD THE PAPER  ×", CustomMinimumSize = new Vector2(0, 42) };
		dismiss.Pressed += DismissMorningPaper;
		column.AddChild(dismiss);
	}

	private void DismissMorningPaper() {
		if (morningPaper?.Visible != true) return;
		morningPaper.Hide();
		isUIOpen = uiOpenBeforePaper;
		UpdateMainHud();
	}

	public override void _Input(InputEvent input) {
		if (morningPaper?.Visible == true && input.IsActionPressed("ui_cancel")) {
			DismissMorningPaper();
			GetViewport().SetInputAsHandled();
		}
	}

	public void OnClick_Desk()
	{
		if (morningPaper?.Visible == true) return;
		if (deskPanel == null) { GD.PushWarning("PlayerDeskPanel is missing from the scene!"); return; }
		if (isUIOpen && !deskPanel.Visible) return;
		deskPanel.Open();
		isUIOpen = true;
		UpdateMainHud();
	}

	public override void _ExitTree()
	{
		if (PlayerDesk.Instance != null) PlayerDesk.Instance.Changed -= UpdateMainHud;
		if (TimeManager.Instance != null) {
			TimeManager.Instance.OnDayStarted -= OnMorningStarted;
			TimeManager.Instance.OnClockRestored -= OnClockRestored;
			TimeManager.Instance.OnDayStarted -= UpdateCalendarButton;
			TimeManager.Instance.OnClockRestored -= UpdateCalendarButton;
		}
	}

	public void OpenArtist(string artistId, bool isOwnedByPlayer = false, int startTab = 0)
	{
		if (string.IsNullOrEmpty(artistId) || artistDetailPanel == null) return;
		artistDetailPanel.ShowArtist(artistId, isOwnedByPlayer, startTab);
		isUIOpen = true;
		UpdateMainHud();
	}

	/// <summary>Opens an act's dossier straight to its DISCOGRAPHY page (tab index 1).</summary>
	public void OpenDiscography(string artistId, bool isOwnedByPlayer = false) => OpenArtist(artistId, isOwnedByPlayer, 1);

	public void OpenLabel(string labelId, bool isOwnedByPlayer = false)
	{
		if (string.IsNullOrEmpty(labelId) || labelDetailPanel == null) return;
		labelDetailPanel.ShowLabel(labelId, isOwnedByPlayer);
		isUIOpen = true;
		UpdateMainHud();
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
			UpdateMainHud();

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
		UpdateMainHud();
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
		DismissMorningPaper();
		if (ledgerPanel != null) ledgerPanel.Visible = false;
		if (dialoguePanel != null) dialoguePanel.Visible = false;
		if (chartPanel != null) chartPanel.Visible = false;
		if (artistDetailPanel != null) artistDetailPanel.Visible = false;
		if (labelDetailPanel != null) labelDetailPanel.Visible = false;
		if (deskPanel != null) deskPanel.Visible = false;

		isUIOpen = false;
		UpdateMainHud();
	}

	private void OpenPanel(Control panel)
	{
		if (panel != null)
		{
			panel.Visible = true;
			isUIOpen = true;
			UpdateMainHud();
		}
	}
}
