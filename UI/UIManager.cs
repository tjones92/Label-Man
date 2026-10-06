using Godot;
using System.Collections.Generic;
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
	private RecordJacketWidget recordJacket;
	private Label hudDateCash, hudTicker;
	private Control morningPaper;
	private Label paperDate, paperHeading;
	private VBoxContainer paperBody;
	private SystemFont paperSerif;
	private ScrollContainer paperScroll;
	private bool paperQueued, uiOpenBeforePaper;
	private readonly List<string> queuedMorningDigests = new();

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
			calendarButton.TooltipText = "Click to advance one day. Right-click to choose a skip option.";
			calendarButton.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
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
		if (deskButton != null) {
			deskButton.Text = "OPEN THE OFFICE";
			deskButton.TooltipText = "Open your label's departments and daily work.";
			deskButton.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
			deskButton.AddThemeStyleboxOverride("normal", new StyleBoxFlat {
				BgColor = new Color("342619"), BorderColor = new Color("e4cf9b"),
				BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
				CornerRadiusTopLeft = 5, CornerRadiusTopRight = 5, CornerRadiusBottomLeft = 5, CornerRadiusBottomRight = 5
			});
			deskButton.AddThemeStyleboxOverride("hover", new StyleBoxFlat {
				BgColor = new Color("6b3a1c"), BorderColor = new Color("f1e5c8"),
				BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
				CornerRadiusTopLeft = 5, CornerRadiusTopRight = 5, CornerRadiusBottomLeft = 5, CornerRadiusBottomRight = 5
			});
			deskButton.AddThemeColorOverride("font_color", new Color("f1e5c8"));
			deskButton.AddThemeColorOverride("font_hover_color", Colors.White);
			deskButton.AddThemeFontSizeOverride("font_size", 18);
			deskButton.Pressed += OnClick_Desk;
		}
		billboardButton = GetNodeOrNull<TextureButton>("BillboardButton");
		if (billboardButton != null) {
			billboardButton.TooltipText = "Hot 100 — open the charts.";
			billboardButton.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
		}
		BuildDeskProps();
		BuildMainHud();
		BuildMorningPaper();
		if (PlayerDesk.Instance != null) PlayerDesk.Instance.Changed += UpdateMainHud;
		if (TimeManager.Instance != null) TimeManager.Instance.OnHourChanged += _ => UpdateMainHud();
		UpdateMainHud();
		// A new player should land on the founding card instead of having to guess that the office
		// tab opens the game. Loaded games and an existing label stay on the desk scene as before.
		if (PlayerDesk.Instance?.HasLabel != true) OnClick_Desk();
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

		// The record jacket mirrors the OPEN THE OFFICE button from the opposite corner.
		recordJacket = new RecordJacketWidget { ZIndex = 10 };
		recordJacket.SetAnchorsPreset(Control.LayoutPreset.TopRight);
		recordJacket.OffsetLeft = -(40 + RecordJacketWidget.JacketWidth);
		recordJacket.OffsetRight = -40;
		recordJacket.OffsetTop = 40;
		recordJacket.GrowHorizontal = Control.GrowDirection.Begin;
		recordJacket.Clicked += () => OpenOfficeAt("CATALOG");
		AddChild(recordJacket);
	}

	private void UpdateMainHud() {
		if (hudDateCash == null) return;
		mainHud.Visible = !isUIOpen;
		if (recordJacket != null) {
			recordJacket.Refresh(PlayerDesk.Instance);
			if (isUIOpen) recordJacket.Visible = false;
		}
		foreach (Button prop in deskProps) prop.Visible = !isUIOpen;
		if (billboardButton != null) billboardButton.Visible = !isUIOpen;
		TimeManager time = TimeManager.Instance;
		PlayerDesk desk = PlayerDesk.Instance;
		string cash = desk?.Label == null ? "" : $"  •  {(desk.Label.cashReserves < 0f ? "−" : "")}${Mathf.Abs(desk.Label.cashReserves):N0} cash";
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
		PlayerDesk desk = PlayerDesk.Instance;
		if (desk?.HasLabel == true && !string.IsNullOrEmpty(desk.MorningDigest)
			&& !queuedMorningDigests.Contains(desk.MorningDigest))
			queuedMorningDigests.Add(desk.MorningDigest);
		// Capture each day's summary before a multi-day skip replaces it with the next morning's digest.
		if (paperQueued) return;
		paperQueued = true;
		Callable.From(() => {
			paperQueued = false;
			PlayerDesk currentDesk = PlayerDesk.Instance;
			if (currentDesk?.HasLabel != true || currentDesk.IsGameOver || queuedMorningDigests.Count == 0) {
				queuedMorningDigests.Clear();
				return;
			}
			string[] digests = queuedMorningDigests.ToArray();
			queuedMorningDigests.Clear();
			string DigestDate(string digest) {
				int end = digest.IndexOf(": ", System.StringComparison.Ordinal);
				return end >= 0 ? digest.Substring(0, end) : digest;
			}
			paperDate.Text = digests.Length == 1
				? DigestDate(digests[0]).ToUpperInvariant()
				: $"{DigestDate(digests[0])} — {DigestDate(digests[digests.Length - 1])}".ToUpperInvariant();
			paperHeading.Text = digests.Length == 1 ? "YESTERDAY AT THE LABEL" : "WHILE YOU WERE AWAY";
			string[] dayDigests = digests.Length > 1
				? digests.Where(digest => !digest.EndsWith("a quiet day at the office.", System.StringComparison.Ordinal)).ToArray()
				: digests;
			if (dayDigests.Length == 0) dayDigests = digests.TakeLast(1).ToArray();
			RenderPaper(dayDigests, digests.Length > 1);
			paperScroll.ScrollVertical = 0;
			if (!morningPaper.Visible) uiOpenBeforePaper = isUIOpen;
			morningPaper.Show();
			morningPaper.MoveToFront();
			isUIOpen = true;
			UpdateMainHud();
		}).CallDeferred();
	}

	private Label PaperText(string text, int size, Color color) {
		var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill };
		label.AddThemeFontOverride("font", paperSerif);
		label.AddThemeFontSizeOverride("font_size", size);
		label.AddThemeColorOverride("font_color", color);
		return label;
	}

	/// <summary>Lays the paper out like a paper: the day's front-page story as a headline, the other news under
	/// it in ranked order, the small stuff last. A multi-day skip groups each day under its dateline.</summary>
	private void RenderPaper(string[] dayDigests, bool multiDay) {
		foreach (Node child in paperBody.GetChildren()) child.QueueFree();
		Color ink = new("30291d"), rust = new("6b3a1c");
		string DigestDate(string digest) {
			int end = digest.IndexOf(": ", System.StringComparison.Ordinal);
			return end >= 0 ? digest.Substring(0, end) : digest;
		}
		foreach (string digest in dayDigests) {
			int dateEnd = digest.IndexOf(": ", System.StringComparison.Ordinal);
			string events = dateEnd >= 0 ? digest.Substring(dateEnd + 2) : digest;
			string[] stories = events.Split("  •  ", System.StringSplitOptions.RemoveEmptyEntries)
				.Distinct(System.StringComparer.OrdinalIgnoreCase)
				.Select(story => char.ToUpperInvariant(story[0]) + story.Substring(1)).ToArray();
			if (multiDay) paperBody.AddChild(PaperText(DigestDate(digest).ToUpperInvariant(), 16, rust));
			bool first = true, markedRule = false;
			foreach (string story in stories) {
				bool front = PlayerDesk.IsFrontPageStory(story);
				if (!front && !markedRule && !first) { paperBody.AddChild(new HSeparator()); markedRule = true; }
				// The lead is a real headline; other front-page items are still bigger than the small stuff.
				int size = front ? (first ? 30 : 23) : 19;
				paperBody.AddChild(PaperText(story, size, front ? ink : new Color("4a4132")));
				first = false;
			}
			if (multiDay) paperBody.AddChild(new HSeparator());
		}
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
		paperSerif = serif;
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
		paperHeading = NewspaperText("YESTERDAY AT THE LABEL", 20, HorizontalAlignment.Left);
		column.AddChild(paperHeading);
		paperScroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
		column.AddChild(paperScroll);
		paperBody = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		paperBody.AddThemeConstantOverride("separation", 12);
		paperScroll.AddChild(paperBody);
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

	// Props on the desk art that open a department. Rects are in the scene's 1920x1080 design space.
	private readonly List<Button> deskProps = new();
	private TextureButton billboardButton;

	private void BuildDeskProps()
	{
		var props = new (string Caption, string Tab, Rect2 Area)[] {
			("Phone — the Rolodex: DJs, stations and calls", "ROLODEX", new Rect2(1518, 330, 222, 118)),
			("Record stack — your catalog", "CATALOG", new Rect2(1740, 306, 180, 174)),
			("Typewriter — the ledger of money, acts and records", "LEDGER", new Rect2(1344, 312, 140, 136)),
			("Checkbook — the books and finances", "FINANCES", new Rect2(18, 606, 810, 282)),
			("Notepad — scout and sign acts (A&R)", "A&R", new Rect2(108, 892, 1188, 188)),
		};
		int insertAt = deskPanel != null ? deskPanel.GetIndex() : GetChildCount();
		foreach (var (caption, tab, area) in props) {
			var button = new Button {
				Flat = true, FocusMode = Control.FocusModeEnum.None, TooltipText = caption,
				MouseDefaultCursorShape = Control.CursorShape.PointingHand,
				Position = area.Position, Size = area.Size, Name = "Prop" + tab.Replace("&", "")
			};
			var hover = new StyleBoxFlat {
				BgColor = new Color(1f, 0.92f, 0.62f, 0.16f), BorderColor = new Color(0.95f, 0.88f, 0.65f, 0.85f),
				BorderWidthLeft = 3, BorderWidthRight = 3, BorderWidthTop = 3, BorderWidthBottom = 3,
				CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10, CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10
			};
			button.AddThemeStyleboxOverride("hover", hover);
			button.AddThemeStyleboxOverride("pressed", hover);
			button.AddThemeStyleboxOverride("normal", new StyleBoxEmpty());
			string target = tab;
			button.Pressed += () => OpenOfficeAt(target);
			AddChild(button);
			MoveChild(button, insertAt++);   // beneath the office panel and the paper, above the bare scene
			deskProps.Add(button);
		}
	}

	private void OpenOfficeAt(string tab)
	{
		if (morningPaper?.Visible == true || isUIOpen || deskPanel == null) return;
		deskPanel.OpenAtTab(tab);
		isUIOpen = true;
		UpdateMainHud();
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

	public void AdvanceOneDayFromDesk()
	{
		if (PlayerDesk.Instance?.HasLabel != true || TimeManager.Instance == null) return;
		TimeManager.Instance.SkipDays(1);
	}

	public void OpenCalendarSkipOptionsFromDesk() => ShowCalendarPopup();

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
		if (calendarButton != null) calendarButton.Text = $"NEXT DAY\n{date.ToHeadlineString()}";
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
