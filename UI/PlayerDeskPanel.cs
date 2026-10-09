using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// The player-facing desk. One panel, a tab strip, and a button per action -- everything is built in
/// code so the slice can change shape without a scene edit. It only ever reads and calls
/// <see cref="PlayerDesk"/>; no simulation logic lives here.
///
/// The desk is artist-centric: the macro tabs are the label's departments (A&R, Roster, Catalog,
/// Distribution, Finances, Office), and the day-to-day work with an act -- its repertoire, writing,
/// teaching covers, and cutting a record -- lives inside a MANAGE window opened from the roster, so a
/// single "put a record out" loop doesn't ping-pong across four tabs.
/// </summary>
public partial class PlayerDeskPanel : Control {
	private Label titleLabel, stockLabel, statusLabel;
	private RichTextLabel clockLabel;   // BBCode: the whole block is bold and the cash figure carries its own colour
	private LabelCrest titleCrest;
	private Button redInkLabel, saveLoadButton;
	private Button nextUpLabel;
	private Label feedbackToastText, feedbackToastBadge;
	private PanelContainer feedbackToast;
	private Timer feedbackToastTimer;
	private HBoxContainer tabs, idleRow;
	private Button waitEveningButton;
	private ScrollContainer contentScroll;
	private VBoxContainer content, contentRoot;
	private readonly List<Button> tabButtons = new();
	private readonly List<string> tabTitles = new();
	private Action currentPage;
	private int currentTab;
	// Scouting-page state survives the rebuild-on-refresh: which room is selected, and which act (if
	// any) the player is currently drawing up a contract for.
	private PlayerDesk.ScoutingVenue selectedVenue = PlayerDesk.ScoutingVenue.ClubsAndRoadhouses;
	private bool hasUserSelectedVenue;
	private PlayerDesk.Prospect negotiating;
	// The act whose contract renewal is on screen on the ROSTER tab. Mirrors `negotiating`, but keys
	// off PlayerDesk.PendingRenewal instead of a Prospect -- the renewal isn't a new signing.
	private SimulatedArtist renewingArtist;
	// The act whose MANAGE window is open on the ROSTER tab, and whether its cover-browse list is up.
	private string managingArtistId;
	private bool browsingCovers;
	private string polarCatalogSong;
	// Whether the save/load menu is up (takes over the panel, like founding / game-over).
	private bool browsingSaves;
	private PopupPanel foundingCityPopup;
	// The founding archetype selected on the founding page; persists across Refresh() rebuilds.
	private FoundingArchetype selectedArchetype = FoundingArchetype.TradeInsider;
	private string selectedFoundingCityId;
	private string foundingLabelName = string.Empty;
	private LabelBrand foundingBrand;   // null until the player customises the crest; then the brand they built
	// ROLODEX page state (the tab and card focus live in PlayerDeskPanel.Rolodex.cs): the record the
	// next call will pitch.
	private string rolodexPitchRecordId;
	private GameDate lastStatusDate;
	private bool hasStatusDate;
	// Money sizes chosen before the sentence is spoken -- the number is part of the offer, not a
	// separate button press after he has already answered.
	private PlayerDesk.AdBuyTier adBuyTier = PlayerDesk.AdBuyTier.Small;
	private PlayerDesk.PayolaTier payolaTier = PlayerDesk.PayolaTier.Small;
	// THE STAFF (directive §7): size of envelope for the project promo man. Survives Refresh() like the
	// other tier pickers above.
	private PlayerDesk.ProjectPromoTier projectPromoTier = PlayerDesk.ProjectPromoTier.Small;
	// DISTRIBUTION page state: which stop kinds (record stores, jukebox ops, ...) are expanded in the
	// stops-in-town list. Survives Refresh() rebuilds like the other page state above.
	private readonly HashSet<PlayerDesk.StopKind> expandedStopKinds = new();
	private string ledgerFilter = "ALL";

	private static readonly Color Ink = new("2b2115");
	private static readonly Color Paper = new("f1e5c8");
	private static readonly Color Folder = new("d7b978");
	private static readonly Color Heard = new("6b5a3a");
	private static readonly Color Rust = new("6b3a1c");
	// Secondary text on the manila header: Heard (6b5a3a) measured 3.5:1 there; this is ~6.5:1.
	private static readonly Color HeaderFade = new("45381f");
	private const int LowStockWarningThreshold = 75;

	public override void _Ready() {
		BuildUi();
		Visible = false;
		if (PlayerDesk.Instance != null) PlayerDesk.Instance.Changed += Refresh;
	}

	public override void _ExitTree() {
		if (PlayerDesk.Instance != null) PlayerDesk.Instance.Changed -= Refresh;
	}

	public void Open() {
		Visible = true;
		MoveToFront();
		Refresh();
		UIManager.Instance?.RefreshMainHud();
	}

	/// <summary>Opens the office straight onto a department -- the desk props use this. With no label yet
	/// (or a folded one) it just opens whatever the panel would normally show.</summary>
	public void OpenAtTab(string title) {
		Open();
		PlayerDesk desk = PlayerDesk.Instance;
		if (desk == null || !desk.HasLabel || desk.IsGameOver) return;
		browsingSaves = false;
		switch (title) {
			case "A&R": GoToTab(0, PageAandR); break;
			case "ROSTER": GoToTab(1, PageRoster); break;
			case "CATALOG": GoToTab(2, PageCatalog); break;
			case "DISTRIBUTION": GoToTab(DistributionTab, PageDistribution); break;
			case "FINANCES": GoToTab(4, PageFinances); break;
			case "OFFICE": GoToTab(5, PageOffice); break;
			case "LEDGER": GoToTab(6, PageLedger); break;
			case "ROLODEX": GoToTab(RolodexTab, PageRolodex); break;
			case "BAND ROOM": GoToTab(BandRoomTab, PageBandRoom); break;
		}
	}

	public void ClosePanel() {
		Visible = false;
		if (UIManager.Instance != null) UIManager.Instance.isUIOpen = false;
		UIManager.Instance?.RefreshMainHud();
	}

	// ========================================================================
	// CHROME
	// ========================================================================

	private void BuildUi() {
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Stop;

		var shade = new ColorRect { Color = new Color(0, 0, 0, .42f) };
		shade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(shade);

		var folder = new PanelContainer();
		folder.SetAnchorsPreset(LayoutPreset.Center);
		folder.Position = new Vector2(-600, -430);
		folder.Size = new Vector2(1200, 860);
		// Manila with the desk's own lighting: contact shadow, lamp falloff, grain and scorched edges.
		var folderPaper = new PaperStyleBox {
			Fill = Folder, Border = new Color("70552c"), BorderWidth = 2, Radius = 3, ShadowSize = 24, ShadowAlpha = 0.55f,
			ShadowOffset = new Vector2(0, 10), Burn = 1.15f
		};
		folderPaper.ContentMarginLeft = 30; folderPaper.ContentMarginRight = 30;
		folderPaper.ContentMarginTop = 24; folderPaper.ContentMarginBottom = 24;
		folder.AddThemeStyleboxOverride("panel", folderPaper);
		AddChild(folder);

		var root = new VBoxContainer();
		root.AddThemeConstantOverride("separation", 6);
		folder.AddChild(root);

		var header = new HBoxContainer();
		root.AddChild(header);
		titleCrest = new LabelCrest { Visible = false };
		header.AddChild(titleCrest);
		titleLabel = new Label { SizeFlagsHorizontal = SizeFlags.ExpandFill, VerticalAlignment = VerticalAlignment.Center, ClipText = true, TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis };
		titleLabel.AddThemeFontSizeOverride("font_size", 28);
		titleLabel.AddThemeFontOverride("font", PaperTheme.SansBold);
		titleLabel.AddThemeColorOverride("font_color", Ink);   // was the default near-white on manila (~1.8:1)
		header.AddChild(titleLabel);
		var saveLoad = Btn("SAVE / LOAD");
		saveLoadButton = saveLoad;
		saveLoad.Pressed += () => { browsingSaves = true; Refresh(); };
		header.AddChild(saveLoad);

		var close = Btn("CLOSE  ×");
		close.Pressed += ClosePanel;
		header.AddChild(close);

		// The folder's upper header: set in bold so it stands out from the body copy below it. It is a RichTextLabel
		// because the cash figure alone is struck in green or red, and has to be readable at a glance.
		clockLabel = new RichTextLabel { BbcodeEnabled = true, FitContent = true, ScrollActive = false, MouseFilter = MouseFilterEnum.Stop, AutowrapMode = TextServer.AutowrapMode.Off };
		clockLabel.AddThemeFontOverride("normal_font", PaperTheme.SansBold);
		clockLabel.AddThemeFontOverride("bold_font", PaperTheme.SansBold);
		clockLabel.AddThemeFontSizeOverride("normal_font_size", 17);
		clockLabel.AddThemeFontSizeOverride("bold_font_size", 17);
		clockLabel.AddThemeColorOverride("default_color", Ink);
		root.AddChild(clockLabel);
		stockLabel = new Label { ClipText = true, CustomMinimumSize = new Vector2(0, 22) };
		stockLabel.AddThemeColorOverride("font_color", HeaderFade);
		root.AddChild(stockLabel);
		nextUpLabel = Btn("");
		nextUpLabel.CustomMinimumSize = new Vector2(0, 30);
		nextUpLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		nextUpLabel.Alignment = HorizontalAlignment.Left;
		// A long hint trims with an ellipsis (full text in the tooltip) instead of stretching the folder.
		nextUpLabel.ClipText = true;
		nextUpLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
		nextUpLabel.TooltipText = "Open the department for the next suggested step.";
		nextUpLabel.AddThemeStyleboxOverride("normal", new StyleBoxFlat {
			BgColor = new Color("ead8ad"), BorderColor = new Color("8a7048"),
			BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
			ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 4, ContentMarginBottom = 4
		});
		nextUpLabel.AddThemeColorOverride("font_color", Ink);
		nextUpLabel.Pressed += OpenNextUp;
		root.AddChild(nextUpLabel);

		// One fixed-height alert row: the red-ink chip (only in the red) sits left of the last action's
		// result. The row's height never changes, so neither appearing nor clearing moves anything below it.
		var alertRow = new HBoxContainer { CustomMinimumSize = new Vector2(0, 24) };
		alertRow.AddThemeConstantOverride("separation", 12);
		root.AddChild(alertRow);
		redInkLabel = new Button { Flat = true, Visible = false, FocusMode = FocusModeEnum.None, MouseDefaultCursorShape = CursorShape.PointingHand };
		redInkLabel.AddThemeColorOverride("font_color", Rust);
		redInkLabel.AddThemeColorOverride("font_hover_color", Colors.Black);
		redInkLabel.AddThemeColorOverride("font_pressed_color", Rust);
		redInkLabel.AddThemeFontSizeOverride("font_size", 16);
		redInkLabel.Pressed += () => GoToTab(4, PageFinances);
		alertRow.AddChild(redInkLabel);

		// Action feedback stays on one line. The tooltip retains the full message when it is longer
		// than the header can show.
		statusLabel = new Label { ClipText = true, SizeFlagsHorizontal = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 24) };
		statusLabel.AddThemeFontSizeOverride("font_size", 16);
		statusLabel.AddThemeColorOverride("font_color", Rust);
		alertRow.AddChild(statusLabel);

		// Passing time without working, so you can wait out the clock -- the clubs don't open till evening
		// and there's no other way to move the day forward from the desk.
		idleRow = new HBoxContainer();
		idleRow.AddThemeConstantOverride("separation", 8);
		var idleLabel = new Label { Text = "Nothing doing?" };
		idleLabel.AddThemeColorOverride("font_color", Ink);
		idleRow.AddChild(idleLabel);
		var wait1 = Btn("WAIT 1h");
		wait1.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.PassTime(1, out string m); Say(m, ok); return ok; });
		idleRow.AddChild(wait1);
		var wait3 = Btn("WAIT 3h");
		wait3.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.PassTime(3, out string m); Say(m, ok); return ok; });
		idleRow.AddChild(wait3);
		var waitEve = Btn("WAIT FOR EVENING");
		waitEveningButton = waitEve;
		waitEve.Pressed += () => Act(() => {
			int h = PlayerDesk.Instance.HoursUntil(17);
			if (h <= 0) { Say("Evening hours have already begun.", false); return false; }
			bool ok = PlayerDesk.Instance.PassTime(h, out string m); Say(m, ok); return ok;
		});
		idleRow.AddChild(waitEve);
		var endDay = Btn("END THE DAY");
		endDay.TooltipText = "Close today's office and see the Morning Paper.";
		endDay.Pressed += () => UIManager.Instance?.AdvanceOneDayFromDesk();
		idleRow.AddChild(endDay);
		var skip = Btn("SKIP DAYS…");
		skip.TooltipText = "Choose a date or upcoming event to skip to.";
		skip.Pressed += () => UIManager.Instance?.OpenCalendarSkipOptionsFromDesk();
		idleRow.AddChild(skip);
		root.AddChild(idleRow);

		tabs = new HBoxContainer();
		tabs.AddThemeConstantOverride("separation", -2);   // trapezoid tabs meet at the foot and open out at the shoulders
		root.AddChild(tabs);

		var paper = new PanelContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
		paper.AddThemeStyleboxOverride("panel", PaperStyleBox.Sheet(Paper, 26, 22, 6).Decorated(clip: true, ring: true, seed: 8));
		root.AddChild(paper);

		contentScroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
		paper.AddChild(contentScroll);
		// A right gutter keeps full-width buttons from running under the scrollbar.
		var contentGutter = new MarginContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		contentGutter.AddThemeConstantOverride("margin_right", 14);
		contentScroll.AddChild(contentGutter);
		content = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		content.AddThemeConstantOverride("separation", 10);
		contentGutter.AddChild(content);
		contentRoot = content;

		feedbackToast = new PanelContainer {
			Name = "ActionFeedbackToast", Visible = false, ZIndex = 25,
			MouseFilter = MouseFilterEnum.Ignore, CustomMinimumSize = new Vector2(300, 0)
		};
		feedbackToast.SetAnchorsPreset(LayoutPreset.TopLeft);
		ApplyFeedbackStyle(feedbackToast, FeedbackKind.Info);
		var toastRow = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		toastRow.AddThemeConstantOverride("separation", 10);
		feedbackToast.AddChild(toastRow);
		// The badge makes severity readable without relying on colour alone.
		feedbackToastBadge = new Label {
			CustomMinimumSize = new Vector2(24, 24), HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center, SizeFlagsVertical = SizeFlags.ShrinkCenter,
			MouseFilter = MouseFilterEnum.Ignore
		};
		feedbackToastBadge.AddThemeFontSizeOverride("font_size", 24);
		toastRow.AddChild(feedbackToastBadge);
		feedbackToastText = new Label {
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			MouseFilter = MouseFilterEnum.Ignore
		};
		feedbackToastText.AddThemeColorOverride("font_color", Ink);
		feedbackToastText.AddThemeFontSizeOverride("font_size", 16);
		feedbackToastText.CustomMinimumSize = new Vector2(250, 0);
		toastRow.AddChild(feedbackToastText);
		AddChild(feedbackToast);
		feedbackToastTimer = new Timer { OneShot = true, WaitTime = 3.5 };
		feedbackToastTimer.Timeout += () => feedbackToast.Hide();
		AddChild(feedbackToastTimer);
	}

	// The macro tabs are the label's departments; Songs and Studio are not tabs -- they live inside a
	// roster act's MANAGE window (RenderManage). 1960s trade names in parentheses for flavor.
	private void BuildTabs() {
		foreach (Node child in tabs.GetChildren()) child.QueueFree();
		tabButtons.Clear();
		tabTitles.Clear();
		AddTab("A&R", PageAandR);
		AddTab("ROSTER", PageRoster);
		AddTab("CATALOG", PageCatalog);
		AddTab("DISTRIBUTION", PageDistribution);
		AddTab("FINANCES", PageFinances);
		AddTab("OFFICE", PageOffice);
		AddTab("LEDGER", PageLedger);
		AddTab("ROLODEX", PageRolodex);
		AddTab("BAND ROOM", PageBandRoom);
	}

	private void AddTab(string title, Action page) {
		int index = tabButtons.Count;
		tabTitles.Add(title);
		var button = Btn(title);
		button.CustomMinimumSize = new Vector2(125, 40);
		button.Pressed += () => GoToTab(index, page);
		tabs.AddChild(button);
		tabButtons.Add(button);
	}

	// Tab order: A&R(0), ROSTER(1), CATALOG(2), DISTRIBUTION(3), FINANCES(4), OFFICE(5), LEDGER(6), ROLODEX(7), BAND ROOM(8).
	private const int CatalogTab = 2;
	private const int DistributionTab = 3;
	private const int RolodexTab = 7;
	private const int BandRoomTab = 8;

	/// <summary>Switches the desk to a macro tab, dropping any open MANAGE/cover-browse sub-state.</summary>
	private void GoToTab(int index, Action page) {
		currentTab = index;
		currentPage = page;
		managingArtistId = null;
		browsingCovers = false;
		// Leaving the tab hangs up: you cannot hold a man on the line while you go and read the books.
		if (index != RolodexTab) { PlayerDesk.Instance?.EndCall(PlayerDesk.Instance.ActiveCall); rolodexPitchRecordId = null; }
		Refresh();
	}

	// ========================================================================
	// REFRESH
	// ========================================================================

	private void Refresh() {
		if (!Visible) return;
		PlayerDesk desk = PlayerDesk.Instance;
		TimeManager time = TimeManager.Instance;
		content = contentRoot;   // a Distribution section may have left `content` pointing at a child
		nextUpLabel.Text = string.Empty;
		if (time != null) {
			if (hasStatusDate && time.CurrentDate != lastStatusDate) {
				statusLabel.Text = string.Empty;
				statusLabel.TooltipText = string.Empty;
				feedbackToast?.Hide();
			}
			lastStatusDate = time.CurrentDate;
			hasStatusDate = true;
		}

		ApplyTitleBrand(null);
		if (desk == null) { titleLabel.Text = "DESK UNAVAILABLE"; return; }

		// Founding, save/load and game-over have no stock, next step or red-ink state; an empty bordered
		// NEXT UP bar over the founding form read as a broken input.
		bool inGame = desk.HasLabel && !desk.IsGameOver && !browsingSaves;
		stockLabel.Visible = nextUpLabel.Visible = inGame;
		if (!inGame) redInkLabel.Visible = false;
		saveLoadButton.Visible = !browsingSaves;   // already on that page

		if (browsingSaves) {
			titleLabel.Text = "SAVE / LOAD";
			clockLabel.Text = time == null ? "" : $"{time.CurrentDate.ToLongString()}  •  {time.GetTimeString()}";
			if (idleRow != null) idleRow.Visible = false;
			foreach (Node child in tabs.GetChildren()) child.QueueFree();
			tabButtons.Clear();
			Clear(content);
			PageSaves(desk);
			Callable.From(() => { if (IsInstanceValid(contentScroll)) contentScroll.ScrollVertical = 0; }).CallDeferred();
			return;
		}

		if (!desk.HasLabel) {
			titleLabel.Text = "START A LABEL";
			clockLabel.Text = time == null ? "" : $"{time.CurrentDate.ToLongString()}  •  {time.GetTimeString()}";
			if (idleRow != null) idleRow.Visible = false;
			foreach (Node child in tabs.GetChildren()) child.QueueFree();
			tabButtons.Clear();
			Clear(content);
			PageFounding();
			return;
		}

		if (desk.IsGameOver) {
			titleLabel.Text = "OUT OF BUSINESS";
			clockLabel.Text = time == null ? "" : $"{time.CurrentDate.ToLongString()}  •  {time.GetTimeString()}";
			if (idleRow != null) idleRow.Visible = false;
			foreach (Node child in tabs.GetChildren()) child.QueueFree();
			tabButtons.Clear();
			Clear(content);
			PageGameOver(desk);
			return;
		}
		if (idleRow != null) idleRow.Visible = true;
		// The idle row is built once, so the evening button has to be re-read from the clock every refresh.
		if (waitEveningButton != null) {
			bool eveningStarted = (time?.CurrentHour ?? 9) >= 17;
			waitEveningButton.Disabled = eveningStarted;
			waitEveningButton.Text = eveningStarted ? "EVENING'S HERE" : "WAIT FOR EVENING";
			waitEveningButton.TooltipText = eveningStarted ? "Evening scouting hours have begun." : "Pass time until 5 PM, when clubs begin opening.";
		}

		if (tabButtons.Count == 0) { BuildTabs(); currentTab = 0; currentPage = PageAandR; }
		for (int index = 0; index < tabButtons.Count; index++)
			StyleFolderTab(tabButtons[index], index == currentTab);
		UpdateTabBadges(desk);

		AILabel label = desk.Label;
		if (nextUpLabel != null) {
			nextUpLabel.Text = $"NEXT UP  •  {NextUpHint(desk)}";
			nextUpLabel.TooltipText = nextUpLabel.Text;
		}
		ApplyTitleBrand(label);
		string region = ChartManager.Instance?.GetRegionById(label.homeRegion)?.regionName ?? label.homeRegion;
		string home = string.IsNullOrEmpty(label.headquartersCity) ? region : $"{label.headquartersCity}, {region}";
		string where = desk.AtHome ? $"at the office in {home}" : $"on the road in {desk.CurrentCity?.name ?? "town"}";
		clockLabel.Text =
			$"{time?.CurrentDate.ToLongString()}  •  {time?.GetTimeString()}  •  " +
			$"{time?.RegularTimeRemainingText ?? "0m"} regular until {time?.RegularWorkdayEndTime ?? "6:00 PM"} + " +
			$"{time?.OvertimeRemainingText ?? "0m"} overtime to {time?.HardStopTime ?? "9:00 PM"}, no overtime fee ({time?.GetDayStatus()})\n" +
			$"{where}  |  {CashMarkup(label.cashReserves)} cash  |  {label.CurrentRosterSize}/{label.maxRosterSize} acts  |  " +
			$"{desk.WorkedCities.Count()} {CountWord(desk.WorkedCities.Count(), "town")} worked";
		clockLabel.TooltipText = time == null ? string.Empty
			: $"Regular workday ends at {time.RegularWorkdayEndTime}; overtime can carry jobs to the {time.HardStopTime} hard stop. There is no overtime surcharge. Each action must finish by the hard stop.";
		var stockNotes = desk.ReleasedRecords
			.Where(record => record?.baseRecord != null)
			.Select(record => {
				string recordId = record.baseRecord.recordId;
				PlayerDesk.PressStock stock = desk.StockFor(recordId);
				if (stock == null || stock.TotalPressed <= 0) return null;
				string note = $"\"{record.baseRecord.title}\" — {stock.Remaining:N0} sellable, {stock.PromoRemaining:N0} promo";
				if (stock.Remaining <= LowStockWarningThreshold) {
					PlayerDesk.PressOrder incoming = desk.PressingOrderFor(recordId);
					note += incoming != null
						? $" · LOW — next run due {incoming.Arrives.ToHeadlineString()}"
						: stock.Remaining == 0 ? " · SOLD OUT — another run takes about 2–4 weeks"
						: " · LOW — another run takes about 2–4 weeks";
				}
				return note;
			})
			.Where(note => !string.IsNullOrEmpty(note)).ToList();
		stockLabel.Text = stockNotes.Count == 0 ? "STOCK  •  no released records on hand" : "STOCK  •  " + string.Join("   |   ", stockNotes);
		stockLabel.TooltipText = stockLabel.Text;
		stockLabel.AddThemeColorOverride("font_color", stockNotes.Any(note => note.Contains("LOW", StringComparison.Ordinal) || note.Contains("SOLD OUT", StringComparison.Ordinal)) ? Rust : HeaderFade);
		// A running tab is survivable, but the bank is watching. Spell out the credit line and the clock on it.
		redInkLabel.Visible = label.cashReserves < 0f;
		redInkLabel.Text = label.cashReserves < 0f ? $"⚠ IN THE RED {desk.MonthsInTheRed}/3" : string.Empty;
		redInkLabel.TooltipText = label.cashReserves < 0f
			? $"Cash is {Money(label.cashReserves)}; the overdraft ceiling is ${-desk.CreditFloor:N0} -- a limit, not money you can borrow. Each month-end below $0 adds a red month (the bank closes you at 3); getting back above $0 resets the count. {desk.MonthsOfGraceLeft} red month-end(s) remain. Sell from the trunk or a hop table, buy stock back from an act, or collect receivables. Click for the books."
			: string.Empty;

		Clear(content);
		(currentPage ?? PageAandR)();
		UpdateTabBadges(desk);
	}

	private void UpdateTabBadges(PlayerDesk desk) {
		for (int i = 0; i < tabButtons.Count; i++) {
			string title = tabTitles[i];
			int count = title switch {
				"LEDGER" => desk.UnreadLogCount,
				"OFFICE" => desk.PendingCalls().Count(),
				"CATALOG" => desk.PendingPressings().Count(),
				"BAND ROOM" => desk.PendingVisitCount,
				"ROLODEX" => desk.ActiveCall != null && desk.ActiveCall.stage != CallStage.Ended ? 1 : 0,
				_ => 0
			};
			tabButtons[i].Text = title;
			TabStamp.Apply(tabButtons[i], count);
		}
	}

	/// <summary>
	/// The next suggested step and the tab that does it. The strip only speaks up about money when the step it
	/// names is out of reach -- an affordable step stays a plain instruction -- and then it names what the desk can
	/// still do for free, so a short purse points at work instead of repeating the same bill for weeks.
	/// </summary>
	private static (string Text, int Tab) NextUpStep(PlayerDesk desk) {
		AILabel label = desk.Label;
		if (desk.PendingVisits().Any(v => v.kind is BandVisitKind.Death or BandVisitKind.Ultimatum or BandVisitKind.Departure))
			return ("Somebody is waiting for you in the Band Room.", BandRoomTab);
		if (desk.Session != null) return ("Choose takes and print the masters.", 1);
		if (desk.Masters.Any(master => !master.Scheduled && !master.Released))
			return ("Assemble a single from the masters on your shelf.", CatalogTab);
		if (desk.Planned.Any(single => !single.Dated)) {
			PlayerDesk.PlannedRelease waiting = desk.Planned.First(single => !single.Dated);
			PlayerDesk.PressOrder order = desk.PressingOrderFor(waiting.Master.Record.recordId);
			if (order != null) return ($"Set the release date; the plant run lands {order.Arrives.ToHeadlineString()}.", CatalogTab);
			if ((desk.StockFor(waiting.Master.Record.recordId)?.Remaining ?? 0) > 0)
				return ("Set the release date; pressed stock is already in the office.", CatalogTab);
			int minimum = desk.MinimumPressRun(waiting.Master.Record.recordId);
			float cost = PlayerDesk.PressingCost(minimum, desk.HasBeenPressed(waiting.Master.Record.recordId));
			if (label.cashReserves >= cost)
				return ($"Order a pressing for the assembled single (${cost:N0}, leaves {Money(label.cashReserves - cost)}).", CatalogTab);
			return ($"A pressing is ${cost:N0}; you have {Money(label.cashReserves)}. " +
				$"{NextBillNote(desk)} {WhileYouWait(desk)}", WaitingTab(desk));
		}
		if (desk.ReleasedRecords.Any(record => (desk.StockFor(record.baseRecord.recordId)?.Remaining ?? 0) > 0))
			return (desk.AtHome ? "Take sellable stock to a town and work an account." : "Work an account in this town or drive back to the office.", DistributionTab);
		if (desk.PendingPressings().Any()) {
			var incoming = desk.PendingPressings().OrderBy(item => item.Arrives).First();
			return ($"Pressing on the way: {incoming.Quantity:N0} of \"{incoming.Title}\" due {incoming.Arrives.ToHeadlineString()}. {WhileYouWait(desk)}", WaitingTab(desk));
		}
		if (label.cashReserves < 0f) return ("Cash is tight. Collect receivables, sell from the trunk, or check the red-ink recovery options.", 4);
		if (label.CurrentRosterSize == 0) return ("Hear an act and add one to the A&R notebook.", 0);
		if (label.roster.Any(artist => desk.RepertoireFor(artist.artistId).Any(item => !item.Recorded)))
			return ("Manage an act to choose material and cut a record.", 1);
		if (label.CurrentRosterSize < label.maxRosterSize) return ("Scout an act or review the latest office news.", 0);
		return ("Check the ledger and stock outlook, or plan the next record.", 1);
	}

	public static string NextUpHint(PlayerDesk desk) => NextUpStep(desk).Text;

	/// <summary>Where "waiting on money or the plant" work happens: the phones if they can still land a name,
	/// otherwise the roster, where songs get written and covers taught.</summary>
	private static int WaitingTab(PlayerDesk desk) =>
		desk.AtHome && desk.CanStillWorkThePhones() ? RolodexTab : desk.Label.CurrentRosterSize > 0 ? 1 : 0;

	/// <summary>A soft warning, not a block: when a spend takes the bank from at least one month's overhead to under it,
	/// say so before the money goes. Opens a paper modal and returns true (the caller stops; the modal's go-ahead button
	/// runs <paramref name="proceed"/>). Returns false when the spend is safe and the caller should just carry on.</summary>
	private bool WarnIfUnderOverhead(string what, float cost, string goLabel, Action proceed) {
		AILabel label = PlayerDesk.Instance?.Label;
		if (label == null) return false;
		float overhead = label.GetMonthlyOverhead();
		float before = label.cashReserves, after = before - cost;
		if (before < overhead || after >= overhead) return false;
		var modal = PaperModal.Open(this, "THAT LEAVES YOU THIN", 620);
		modal.AddText($"{what} costs ${cost:N0}. You'd have {Money(after)} left, which is under one month's overhead. {NextBillNote(PlayerDesk.Instance)}");
		var landing = PlayerDesk.Instance.PendingPressings().Select(item => (GameDate?)item.Arrives).FirstOrDefault();
		modal.AddText(landing.HasValue
			? $"Nothing comes in from sales until the vinyl does ({landing.Value.ToHeadlineString()}). The bank will wait a few months, but it will not wait forever."
			: "Nothing comes in until a record is pressed and selling. The bank will wait a few months, but it will not wait forever.");
		modal.AddButton("NOT YET", null);
		modal.AddButton(goLabel, proceed, PaperModal.ButtonKind.Primary);
		return true;
	}

	/// <summary>The one bill the player is running toward, named only when cash is already short.</summary>
	private static string NextBillNote(PlayerDesk desk) {
		GameDate today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		GameDate due = new GameDate(today.year, today.month, 1).AddDays(DateTime.DaysInMonth(today.year, today.month));
		return $"${desk.Label.GetMonthlyOverhead():N0} overhead due {due.ShortMonthName} {due.day}.";
	}

	/// <summary>What the desk can still do while cash or the plant is the holdup. Only verbs that exist and are
	/// available right now are listed, so the line never promises a call that cannot land.</summary>
	private static string WhileYouWait(PlayerDesk desk) {
		var options = new List<string>();
		if (desk.AtHome && desk.CanStillWorkThePhones()) options.Add("work the phones (free)");
		// The acetate is the one thing that can pay off before the plant delivers: a test disc a local DJ can play.
		if (desk.Masters.Any(master => !master.Released && desk.AcetatesFor(master.Record?.recordId) > 0))
			options.Add("play your acetate down the phone to a DJ, or hand it to one in person");
		else if (desk.Label.cashReserves >= 20f && desk.Masters.Any(master => !master.Released))
			options.Add("cut a $20 acetate (a test disc a DJ can play now)");
		if (desk.Label.CurrentRosterSize > 0) options.Add("work up songs");
		else options.Add("scout a room (hours only)");
		return "Meanwhile: " + (options.Count == 1 ? options[0]
			: string.Join(", ", options.Take(options.Count - 1)) + ", or " + options[^1]) + ".";
	}

	/// <summary>The folder's title in the label's own dress: its crest beside the name in its lettering and ink.
	/// A null label (founding, save/load, game over) puts the plain office heading back.</summary>
	private void ApplyTitleBrand(AILabel label) {
		if (label == null) {
			titleCrest.Visible = false;
			titleLabel.AddThemeFontOverride("font", PaperTheme.SansBold);
			titleLabel.AddThemeFontSizeOverride("font_size", 28);
			titleLabel.AddThemeColorOverride("font_color", Ink);
			return;
		}
		LabelBrand brand = LabelBrand.For(label);
		titleCrest.Set(brand, label.labelName, 46f);
		titleCrest.Visible = true;
		titleLabel.Text = brand.DisplayName(label.labelName);
		titleLabel.AddThemeFontOverride("font", PaperTheme.Lettering(brand.Lettering));
		titleLabel.AddThemeFontSizeOverride("font_size", brand.Lettering == LetteringStyle.Script ? 40 : 30);
		titleLabel.AddThemeColorOverride("font_color", brand.Pair.Ink);
	}

	private void OpenNextUp() {
		PlayerDesk desk = PlayerDesk.Instance;
		if (desk == null) return;
		switch (NextUpStep(desk).Tab) {
			case 0: GoToTab(0, PageAandR); break;
			case 1: GoToTab(1, PageRoster); break;
			case CatalogTab: GoToTab(CatalogTab, PageCatalog); break;
			case 4: GoToTab(4, PageFinances); break;
			case RolodexTab: GoToTab(RolodexTab, PageRolodex); break;
			case BandRoomTab: GoToTab(BandRoomTab, PageBandRoom); break;
			default: GoToTab(DistributionTab, PageDistribution); break;
		}
	}

	/// <summary>How an action's result reads: a neutral note, a completed action, or a refusal.</summary>
	private enum FeedbackKind { Info, Success, Warning }

	private static readonly Color FeedbackGreen = new("3f6b2f");
	private static readonly Color FeedbackRed = new("9a2b1a");

	private static void ApplyFeedbackStyle(PanelContainer toast, FeedbackKind kind) {
		// A refusal is a Post-it stuck beside the control that was refused; anything else is a telegram slip.
		if (kind == FeedbackKind.Warning) {
			var note = PaperStyleBox.Sheet(new Color("f5df73"), 16, 12, 8, new Color("c9b04a"));
			note.ShadowAlpha = 0.5f; note.ShadowOffset = new Vector2(3, 5); note.Burn = 0f; note.Falloff = 0.5f; note.Grain = 0.45f; note.Radius = 1; note.BorderWidth = 1;
			toast.AddThemeStyleboxOverride("panel", note);
		} else {
			Color band = kind == FeedbackKind.Success ? new Color("2f4a8a") : Rust;
			toast.AddThemeStyleboxOverride("panel", new StyleBoxFlat {
				BgColor = new Color("f3ead2"), BorderColor = band,
				BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 6, BorderWidthBottom = 1,
				ContentMarginLeft = 14, ContentMarginRight = 14, ContentMarginTop = 8, ContentMarginBottom = 9,
				ShadowColor = new Color(0, 0, 0, .35f), ShadowSize = 8, ShadowOffset = new Vector2(1, 3)
			});
		}
	}

	/// <summary>A neutral note (hints, navigation, "nothing to do").</summary>
	private void Say(string message) => Say(message, FeedbackKind.Info);

	/// <summary>The result of an action: <paramref name="ok"/> false means the desk refused it.</summary>
	private void Say(string message, bool ok) => Say(message, ok ? FeedbackKind.Success : FeedbackKind.Warning);

	private void Say(string message, FeedbackKind kind) {
		Color accent = kind switch { FeedbackKind.Success => FeedbackGreen, FeedbackKind.Warning => FeedbackRed, _ => Rust };
		statusLabel.Text = message ?? string.Empty;
		statusLabel.TooltipText = statusLabel.Text;
		statusLabel.AddThemeColorOverride("font_color", accent);
		if (string.IsNullOrWhiteSpace(message) || feedbackToast == null) return;

		ApplyFeedbackStyle(feedbackToast, kind);
		bool note = kind == FeedbackKind.Warning;
		feedbackToastBadge.Text = kind switch { FeedbackKind.Success => "✓", FeedbackKind.Warning => "!", _ => "i" };
		feedbackToastBadge.AddThemeColorOverride("font_color", note ? new Color("a3261a") : accent);
		// The Post-it is scrawled in grease pencil; the telegram is typed.
		feedbackToastText.AddThemeFontOverride("font", note ? PaperTheme.Elite : PaperTheme.Typed);
		feedbackToastText.AddThemeColorOverride("font_color", note ? new Color("4a1812") : Ink);
		feedbackToastText.Text = message;
		feedbackToast.TooltipText = message;
		feedbackToast.ResetSize();
		feedbackToast.RotationDegrees = note ? -2.4f : 0f;

		// Where it goes: a refusal sticks beside the control the player just pressed, so the reason is at the point of the
		// mistake. Anything with no control under the cursor (a keyboard action, a timer) falls back to the top of the desk.
		Control pressed = note ? PressedControl() : null;
		feedbackToast.Modulate = new Color(1, 1, 1, 0);
		feedbackToast.Show();
		PlaceSlip(pressed?.GetGlobalRect(), pressed != null ? GetGlobalMousePosition() : (Vector2?)null);
		// A refusal stays up a little longer: the player needs to read what to fix.
		feedbackToastTimer.Start(kind == FeedbackKind.Warning ? 6.0 : 3.5);
	}

	/// <summary>The button (or field) under the cursor, if it belongs to this desk: the control a click just landed on.</summary>
	private Control PressedControl() {
		Control hovered = GetViewport()?.GuiGetHoveredControl();
		for (Control node = hovered; node != null; node = node.GetParent() as Control) {
			if (node == feedbackToast || feedbackToast.IsAncestorOf(node)) return null;
			if ((node is BaseButton || node is SpinBox || node is LineEdit) && IsAncestorOf(node) && node.IsVisibleInTree()) return node;
		}
		return null;
	}

	private async void PlaceSlip(Rect2? target, Vector2? mouse) {
		// Wait a frame so the slip has its real size (its width is fixed, its height is its words).
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		if (!IsInstanceValid(feedbackToast) || !feedbackToast.Visible) return;
		Vector2 viewport = GetViewportRect().Size, size = feedbackToast.Size;
		Vector2 at;
		if (target is Rect2 rect && mouse is Vector2 pointer) {
			// Stuck on the control's upper edge, under the pointer: half over the button, half over the page above it.
			float x = Mathf.Clamp(pointer.X - size.X * 0.35f, rect.Position.X - 6f, Mathf.Max(rect.Position.X, rect.End.X - size.X + 6f));
			float above = rect.Position.Y - size.Y + 14f;
			float y = above >= 8f ? above : rect.End.Y - 12f;
			at = new Vector2(x, y);
		} else {
			at = new Vector2((viewport.X - size.X) / 2f, 12f);
		}
		at = new Vector2(Mathf.Clamp(at.X, 8f, Mathf.Max(8f, viewport.X - size.X - 8f)), Mathf.Clamp(at.Y, 8f, Mathf.Max(8f, viewport.Y - size.Y - 8f)));
		feedbackToast.PivotOffset = size / 2f;
		feedbackToast.GlobalPosition = at;
		feedbackToast.Scale = new Vector2(0.86f, 0.86f);
		var tween = CreateTween().SetParallel(true);
		tween.TweenProperty(feedbackToast, "scale", Vector2.One, 0.14).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
		tween.TweenProperty(feedbackToast, "modulate:a", 1f, 0.08);
	}

	private void Act(Func<bool> action) {
		action();
		Refresh();
	}

	// ========================================================================
	// FOUNDING
	// ========================================================================

	private void PageFounding() {
		content.AddChild(FoundingTitleCard());
		// A returning player's first button: the newest save of any kind, autosaves included.
		SaveGameService.SaveInfo? newest = SaveGameService.NewestSave();
		if (newest.HasValue) {
			SaveGameService.SaveInfo last = newest.Value;
			string lastSlot = last.Slot;
			Heading("WELCOME BACK");
			Body($"{last.LabelName}, {last.InGameDate.ToHeadlineString()}  •  {(SaveGameService.IsAutosaveSlot(lastSlot) ? "autosave" : $"saved as \"{lastSlot}\"")}, {last.SavedAtUtc.ToLocalTime():g}");
			var cont = Btn("CONTINUE");
			cont.CustomMinimumSize = new Vector2(260, 46);
			PaperModal.StylePrimary(cont, Rust);
			cont.Pressed += () => { bool ok = SaveGameService.Load(lastSlot, out string message); Say(message, ok); Refresh(); };
			content.AddChild(cont);
			Body("Or start a new label below, or open SAVE / LOAD to pick another save.");
		}

		Heading("WHO WERE YOU BEFORE THIS?");
		Body("Start here: choose your background, name the label, pick a home town, then open the doors.");

		// Archetype selector: a row of buttons, the selected one at full opacity.
		var archetypeRow = new HBoxContainer();
		archetypeRow.AddThemeConstantOverride("separation", 6);
		content.AddChild(archetypeRow);
		foreach (FoundingArchetype arch in System.Enum.GetValues<FoundingArchetype>()) {
			FoundingArchetype captured = arch;
			var btn = Btn(FoundingArchetypeData.Get(arch).Name.ToUpperInvariant());
			btn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			btn.CustomMinimumSize = new Vector2(0, 40);
			btn.TooltipText = ArchetypeMechanics(arch);
			bool isSelected = arch == selectedArchetype;
			btn.AddThemeStyleboxOverride("normal", new StyleBoxFlat {
				BgColor = isSelected ? Rust : new Color("ead8ad"),
				BorderColor = isSelected ? new Color("f1e5c8") : new Color("8a7048"),
				BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
				CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4
			});
			btn.AddThemeColorOverride("font_color", isSelected ? Paper : Ink);
			btn.Pressed += () => { selectedArchetype = captured; Refresh(); };
			// Each origin wears the print of what it is best at (a linocut, on a paper patch when the button is dark).
			ExecutiveInstinctProfile spread = FoundingArchetypeData.Get(arch).Instincts;
			btn.AddThemeStyleboxOverride("hover", (StyleBox)btn.GetThemeStylebox("normal").Duplicate());
			btn.AddThemeStyleboxOverride("pressed", (StyleBox)btn.GetThemeStylebox("normal").Duplicate());
			btn.Alignment = HorizontalAlignment.Right;
			var badge = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore, Position = new Vector2(8, 5), Size = new Vector2(34, 34) };
			badge.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color("f1e5c8"), CornerRadiusTopLeft = 3, CornerRadiusTopRight = 3, CornerRadiusBottomLeft = 3, CornerRadiusBottomRight = 3 });
			badge.AddChild(new LinocutIcon().Set(StrongestInstinct(spread), 30f));
			btn.AddChild(badge);
			archetypeRow.AddChild(btn);
		}

		var selected = FoundingArchetypeData.Get(selectedArchetype);
		Body($"{selected.Tagline}  ·  Starting capital: ${selected.Capital:N0}");
		Body(selected.Description);

		// Instinct spread: four linocuts with a vinyl rating each, then the label's three stats the same way.
		content.AddChild(InstinctRow(selected.Instincts, false));
		content.AddChild(StatRow(("Scouting", selected.ScoutingAbility), ("Production", selected.ProductionQuality), ("Marketing", selected.MarketingPower)));
		float firstRunCost = PlayerDesk.PressingCost(PlayerDesk.PressMinimumOrder);
		Body($"MONEY AND RUNWAY  ·  ${selected.Capital:N0} starting cash; a first {PlayerDesk.PressMinimumOrder:N0}-copy pressing costs about ${firstRunCost:N0}. " +
			$"Monthly overhead is the office rent of the town you pick plus ${CityProfiles.OfficeBaseOverhead:N0} for the phone and postage: ${CityProfiles.OverheadRange().Low:N0} to ${CityProfiles.OverheadRange().High:N0} a month. The overdraft ceiling is three months of overhead below zero (not borrowed cash); the bank closes the label after three red month-ends in a row. Climb back above $0 at month-end to reset the count.");
		Body("Instincts (1–5) affect what you can read and which contact options appear. Label stats (0–1) affect scouting, recording quality, and promotion.");
		Body($"Your roster can hold up to {PlayerDesk.PlayerRosterCapacity} acts.");

		Heading("NAME THE LABEL AND PICK YOUR TOWN");

		var nameEdit = new LineEdit { PlaceholderText = "Label name", Text = foundingLabelName, CustomMinimumSize = new Vector2(400, 38) };
		StyleField(nameEdit);
		LabelBrandPicker brandPicker = null;
		nameEdit.TextChanged += value => { foundingLabelName = value; brandPicker?.SetLabelName(value); };
		content.AddChild(nameEdit);

		// A town is a founding decision, so show the local advantages on a card instead of hiding them in a dropdown.
		List<MarketRegion> regions = ChartManager.Instance?.GetAllRegions() ?? new List<MarketRegion>();
		var regionName = regions.ToDictionary(r => r.regionId, r => r.regionName);
		List<MarketCity> cities = DistanceModel.GetCities()
			.OrderBy(city => regionName.TryGetValue(city.parentRegionId, out string name) ? name : city.parentRegionId)
			.ThenByDescending(city => city.isRegionalHub)
			.ThenBy(city => city.distributionTier)
			.ToList();
		if (string.IsNullOrEmpty(selectedFoundingCityId) && cities.Count > 0) selectedFoundingCityId = cities[0].cityId;
		MarketCity selectedCity = cities.FirstOrDefault(city => city.cityId == selectedFoundingCityId) ?? cities.FirstOrDefault();
		selectedFoundingCityId = selectedCity?.cityId;
		var townRow = new HBoxContainer();
		var town = FormLabel(selectedCity == null ? "No towns loaded." : $"{selectedCity.name}  —  {regionName.GetValueOrDefault(selectedCity.parentRegionId, selectedCity.parentRegionId)}");
		town.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		townRow.AddChild(town);
		var browse = Btn("CHOOSE TOWN  →");
		browse.Disabled = cities.Count == 0;
		browse.Pressed += () => ShowFoundingCities(cities, regions);
		townRow.AddChild(browse);
		content.AddChild(townRow);

		Heading("YOUR LABEL'S PAPER");
		Body("A crest, a pair of period colours and a lettering for the name. They go on your office, your contracts, the chart and the paper; skip it and the name picks for you.");
		brandPicker = new LabelBrandPicker(foundingBrand, foundingLabelName);
		brandPicker.Changed += () => foundingBrand = brandPicker.Customised ? brandPicker.Current : null;
		content.AddChild(brandPicker);

		var found = Primary("OPEN THE DOORS");
		found.CustomMinimumSize = new Vector2(240, 44);
		found.TooltipText = "Start the label with the selected background, name, town, and starting cash.";
		found.Pressed += () => {
			if (cities.Count == 0) { Say("No towns loaded.", false); return; }
			bool ok = PlayerDesk.Instance.FoundLabel(nameEdit.Text, selectedFoundingCityId ?? cities[0].cityId, selectedArchetype, brandPicker.Current, out string message);
			Say(message, ok);
			Refresh();
		};
		content.AddChild(found);
	}

	/// <summary>What it is like to run a label out of this town, in plain words: the rent, the rivals, the
	/// studios, the shipping and the shops at home. Every figure is the one the game will actually use.</summary>
	private static string TownFacts(MarketCity city) {
		CityProfile profile = CityProfiles.Get(city.cityId);
		float quality = PlayerDesk.StudioQualityIn(city.cityId, city.parentRegionId);
		float mid = PlayerDesk.StudioHourlyRateIn(city.cityId, city.parentRegionId, PlayerDesk.StudioTier.Mid);
		float budget = PlayerDesk.StudioHourlyRateIn(city.cityId, city.parentRegionId, PlayerDesk.StudioTier.Budget);
		float top = PlayerDesk.StudioHourlyRateIn(city.cityId, city.parentRegionId, PlayerDesk.StudioTier.Top);
		return $"{profile.Character}\n\n" +
			$"THE OFFICE  ·  Rent is ${profile.Rent:N0} a month, ${CityProfiles.MonthlyOverhead(city.cityId):N0} all in with the phone and postage.\n" +
			$"THE SCENE  ·  {CityProfiles.CrowdingText(profile)} {CityProfiles.AskText(profile)}\n" +
			$"THE STUDIOS  ·  Rooms run ${budget:N0} to ${top:N0} an hour (about ${mid:N0} for a middling one), with {CityProfiles.StudioSoundText(quality)}.\n" +
			$"SHIPPING  ·  {CityProfiles.ShippingText(city)}\n" +
			$"AT HOME  ·  About {PlayerStopFactory.ShopCountFor(city)} record shops you can call on without leaving town.";
	}

	private static string ArchetypeMechanics(FoundingArchetype archetype) => archetype switch {
		FoundingArchetype.PawnShopOwner => "The Suit and Fixer help with business reads, negotiation, and risky contact options. Lower Ear means fuzzier reads on records and station taste. Starts with the most cash.",
		FoundingArchetype.ExMusician => "The Ear and Street improve reads on record quality, DJ taste, and local momentum. Thin starting cash makes the first pressing a major share of the budget.",
		FoundingArchetype.PromoMan => "The Fixer helps read suspicion and unlocks harder-edged contact options; Street helps read local momentum. Stronger promotion, but modest cash.",
		_ => "A balanced start. Suit helps with business terms and reach; Ear and Fixer guide record and contact reads. Moderate starting cash and label stats."
	};

	private void ShowFoundingCities(List<MarketCity> cities, List<MarketRegion> regions) {
		if (foundingCityPopup != null) foundingCityPopup.QueueFree();
		foundingCityPopup = new PopupPanel { Size = new Vector2I(1000, 680) };
		var popup = foundingCityPopup;
		var regionName = regions.ToDictionary(region => region.regionId, region => region.regionName);
		popup.AddThemeStyleboxOverride("panel", new StyleBoxFlat {
			BgColor = Paper, BorderColor = Rust,
			BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
			ContentMarginLeft = 28, ContentMarginRight = 28, ContentMarginTop = 24, ContentMarginBottom = 24
		});
		AddChild(popup);
		var card = new VBoxContainer();
		card.AddThemeConstantOverride("separation", 10);
		popup.AddChild(card);
		var heading = new Label { Text = "CHOOSE YOUR HOME TOWN", CustomMinimumSize = new Vector2(0, 34) };
		heading.AddThemeFontSizeOverride("font_size", 26);
		heading.AddThemeColorOverride("font_color", Ink);
		card.AddChild(heading);
		var search = new LineEdit { PlaceholderText = "Search by town or region…", CustomMinimumSize = new Vector2(0, 36) };
		card.AddChild(search);
		var selection = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
		selection.AddThemeConstantOverride("separation", 18);
		card.AddChild(selection);
		var cityList = new ItemList { CustomMinimumSize = new Vector2(260, 0), SizeFlagsVertical = SizeFlags.ExpandFill };
		cityList.AddThemeColorOverride("font_color", Ink);
		cityList.AddThemeColorOverride("font_selected_color", Paper);
		cityList.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color("ead8ad"), BorderColor = new Color("8a7048"),
			BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1 });
		cityList.AddThemeStyleboxOverride("selected", new StyleBoxFlat { BgColor = Rust });
		cityList.AddThemeStyleboxOverride("selected_focus", new StyleBoxFlat { BgColor = Rust });
		selection.AddChild(cityList);
		var detail = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		detail.AddThemeConstantOverride("separation", 12);
		selection.AddChild(detail);
		Label CardText(int size) {
			var label = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill };
			label.AddThemeFontSizeOverride("font_size", size);
			label.AddThemeColorOverride("font_color", Ink);
			detail.AddChild(label);
			return label;
		}
		var cityName = CardText(28);
		var location = CardText(16);
		detail.AddChild(new HSeparator());
		var details = CardText(16);
		var neighbors = CardText(16);
		var select = Btn("MAKE THIS MY HOME");
		select.CustomMinimumSize = new Vector2(0, 42);
		detail.AddChild(select);
		string popupCityId = selectedFoundingCityId;
		int index = Math.Max(0, cities.FindIndex(city => city.cityId == popupCityId));
		List<MarketCity> filteredCities = new();
		void ShowCard() {
			if (index < 0 || index >= filteredCities.Count) return;
			MarketCity city = filteredCities[index];
			popupCityId = city.cityId;
			MarketRegion region = regions.FirstOrDefault(candidate => candidate.regionId == city.parentRegionId);
			cityName.Text = city.name.ToUpperInvariant();
			location.Text = $"{region?.regionName ?? city.parentRegionId}{(city.isRegionalHub ? "  •  REGIONAL HUB" : "")}";
			string tastes = string.Join(", ", (region?.genrePreferences ?? Array.Empty<GenrePreference>())
				.OrderByDescending(pref => pref.affinity).Take(3).Select(pref => GenreNameFormatter.Format(pref.genre)));
			details.Text = TownFacts(city) + $"\n\nThe region leans toward {tastes}.";
			neighbors.Text = "THE ROAD  ·  Nearest towns: " + string.Join(", ", cities.Where(other => other.cityId != city.cityId)
				.OrderBy(other => DistanceModel.GetRoadMilesBetween(city.cityId, other.cityId)).Take(3)
				.Select(other => $"{other.name} ({PlayerDesk.Instance.DriveQuote(city.cityId, other.cityId).Hours}h)"));
		}
		void FilterCities(string query) {
			string keepCityId = popupCityId;
			string term = (query ?? string.Empty).Trim();
			filteredCities = cities.Where(city => string.IsNullOrEmpty(term)
				|| city.name.Contains(term, StringComparison.OrdinalIgnoreCase)
				|| (regionName.GetValueOrDefault(city.parentRegionId, city.parentRegionId) ?? "").Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
			cityList.Clear();
			foreach (MarketCity city in filteredCities)
				cityList.AddItem($"{city.name}  ·  {regionName.GetValueOrDefault(city.parentRegionId, city.parentRegionId)}");
			index = filteredCities.FindIndex(city => city.cityId == keepCityId);
			if (index < 0 && filteredCities.Count > 0) index = 0;
			if (index >= 0) { popupCityId = filteredCities[index].cityId; cityList.Select(index); ShowCard(); }
			else { cityName.Text = "NO TOWNS MATCH"; location.Text = "Try a different town or region."; details.Text = ""; neighbors.Text = ""; }
			select.Disabled = filteredCities.Count == 0;
		}
		cityList.ItemSelected += item => { index = (int)item; popupCityId = filteredCities[index].cityId; ShowCard(); };
		search.TextChanged += FilterCities;
		select.Pressed += () => { if (index < 0 || index >= filteredCities.Count) return; selectedFoundingCityId = popupCityId; popup.Hide(); Refresh(); };
		FilterCities(search.Text);
		popup.PopupCentered(new Vector2I(1000, 680));
	}

	// ========================================================================
	// SAVE / LOAD
	// ========================================================================

	private void PageSaves(PlayerDesk desk) {
		var back = Btn("‹ BACK");
		back.CustomMinimumSize = new Vector2(160, 36);
		back.Pressed += () => { browsingSaves = false; Refresh(); };
		content.AddChild(back);

		Heading("SAVE");
		if (desk.HasLabel && !desk.IsGameOver) {
			Body("Save names allow letters, numbers, spaces, periods, apostrophes, parentheses, hyphens, and underscores. Existing names require confirmation before they are replaced.");
			var row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 10);
			var nameEdit = new LineEdit {
				PlaceholderText = "Save name", Text = desk.Label.labelName, CustomMinimumSize = new Vector2(500, 38),
				MaxLength = 48
			};
			StyleField(nameEdit);
			row.AddChild(nameEdit);
			var save = Btn("SAVE");
			save.CustomMinimumSize = new Vector2(140, 38);
			save.Pressed += () => {
				string saveName = nameEdit.Text.Trim();
				if (!SaveGameService.IsValidSlotName(saveName, out string reason)) { Say(reason, false); return; }
				void SaveNamedSlot() {
					bool ok = SaveGameService.Save(saveName, out string message);
					Say(message, ok);
					Refresh();
				}
				if (SaveGameService.HasSave(saveName)) {
					var overwrite = PaperModal.Open(this, "REPLACE THIS SAVE?", 560);
					overwrite.AddText($"A save named \"{saveName}\" already exists. Replace it with the current label and date?");
					overwrite.AddButton("KEEP THE OLD SAVE", null);
					overwrite.AddButton("REPLACE IT", SaveNamedSlot, PaperModal.ButtonKind.Primary);
				} else SaveNamedSlot();
			};
			row.AddChild(save);
			content.AddChild(row);
		} else Body(desk.IsGameOver
			? "The label has folded — you can only load from here."
			: "Open a label before you can save.");

		Heading("LOAD");
		// The slot you are playing on first, then the rolling autosaves, then everything else newest-first.
		string currentSlot = SaveGameService.CurrentSlot;
		List<SaveGameService.SaveInfo> saves = SaveGameService.ListSaves()
			.OrderBy(info => info.Slot == currentSlot ? 0 : SaveGameService.IsAutosaveSlot(info.Slot) ? 1 : 2)
			.ThenByDescending(info => info.SavedAtUtc)
			.ToList();
		if (saves.Count == 0) { Body("No saves on disk yet."); return; }
		foreach (SaveGameService.SaveInfo info in saves) {
			var row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 10);
			row.AddChild(new LabelCrest().Set(info.Brand, info.LabelName, 38));
			string tag = info.Slot == currentSlot ? "CURRENT  •  " : SaveGameService.IsAutosaveSlot(info.Slot) ? "AUTOSAVE  •  " : "";
			var text = new Label {
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				Text = $"{tag}{info.Slot}  —  {info.LabelName}, {info.InGameDate.ToHeadlineString()}   •   saved {info.SavedAtUtc.ToLocalTime():g}"
			};
			text.AddThemeColorOverride("font_color", Ink);
			row.AddChild(text);
			string slot = info.Slot;
			var load = Btn("LOAD");
			load.CustomMinimumSize = new Vector2(120, 36);
			load.Pressed += () => { bool ok = SaveGameService.Load(slot, out string message); Say(message, ok); browsingSaves = false; Refresh(); };
			row.AddChild(load);
			var del = Btn("DELETE");
			del.CustomMinimumSize = new Vector2(120, 36);
			del.Pressed += () => {
				var confirm = PaperModal.Open(this, "DELETE THIS SAVE?", 520);
				confirm.AddText($"Permanently delete the save \"{slot}\"? This can't be undone.");
				confirm.AddButton("KEEP IT", null);
				confirm.AddButton("DELETE IT", () => {
					bool deleted = SaveGameService.Delete(slot);
					Say(deleted ? $"Deleted \"{slot}\"." : $"Couldn't delete \"{slot}\".", deleted);
					Refresh();
				}, PaperModal.ButtonKind.Danger);
			};
			row.AddChild(del);
			content.AddChild(row);
		}
	}

	// ========================================================================
	// GAME OVER
	// ========================================================================

	private void PageGameOver(PlayerDesk desk) {
		AILabel label = desk.Label;
		Heading("THE DOORS CLOSE");
		Body(desk.GameOverReason ?? "The label has folded.");

		Body($"\n{label.labelName} — founded {label.foundedYear}\n" +
			$"    {label.totalReleases} {CountWord(label.totalReleases, "release")}   •   {label.top40Hits} Top 40   •   {label.numberOneHits} #1s\n" +
			$"    ended {Money(label.cashReserves)} cash   •   ${label.outstandingWholesaleReceivables:N0} still owed by the houses");

		Body("\nThat's the business. You can pick the label back up from your last save, or close the desk and start a new one.");

		var buttons = new HBoxContainer();
		buttons.AddThemeConstantOverride("separation", 12);
		var load = Btn("LOAD A SAVE");
		load.CustomMinimumSize = new Vector2(220, 44);
		load.Pressed += () => { browsingSaves = true; Refresh(); };
		buttons.AddChild(load);
		var close = Btn("CLOSE DESK");
		close.CustomMinimumSize = new Vector2(160, 44);
		close.Pressed += ClosePanel;
		buttons.AddChild(close);
		content.AddChild(buttons);
	}

	// ========================================================================
	// A&R (scouting funnel)
	// ========================================================================

	private static readonly PlayerDesk.ScoutingVenue[] VenueOrder = {
		PlayerDesk.ScoutingVenue.ClubsAndRoadhouses,
		PlayerDesk.ScoutingVenue.TheatresAndSupperClubs,
		PlayerDesk.ScoutingVenue.HonkyTonks,
		PlayerDesk.ScoutingVenue.IndustryMeets
	};

	private static readonly Dictionary<PlayerDesk.ScoutingVenue, string> VenueBlurb = new() {
		[PlayerDesk.ScoutingVenue.ClubsAndRoadhouses] = "rock, R&B, soul",
		[PlayerDesk.ScoutingVenue.TheatresAndSupperClubs] = "pop, jazz",
		[PlayerDesk.ScoutingVenue.HonkyTonks] = "country, folk",
		[PlayerDesk.ScoutingVenue.IndustryMeets] = "the trade, better acts"
	};

	/// <summary>What the room is like to buy from -- the quality half of the price/quality line.</summary>
	private static readonly Dictionary<PlayerDesk.ScoutingVenue, string> VenueCharacter = new() {
		[PlayerDesk.ScoutingVenue.ClubsAndRoadhouses] = "The local scene: raw, unrepresented, and they take pocket money.",
		[PlayerDesk.ScoutingVenue.TheatresAndSupperClubs] = "Working pros with a going rate; steadier, and they know it.",
		[PlayerDesk.ScoutingVenue.HonkyTonks] = "Cheap and plentiful: a fifth and a steak dinner buys a signature.",
		[PlayerDesk.ScoutingVenue.IndustryMeets] = "The priciest room: polished youth-pop product, usually with someone speaking for them."
	};

	private static readonly Dictionary<PlayerDesk.ScoutingVenue, string> VenueTitle = new() {
		[PlayerDesk.ScoutingVenue.ClubsAndRoadhouses] = "Clubs & roadhouses",
		[PlayerDesk.ScoutingVenue.TheatresAndSupperClubs] = "Theatres & supper clubs",
		[PlayerDesk.ScoutingVenue.HonkyTonks] = "Honky-tonks",
		[PlayerDesk.ScoutingVenue.IndustryMeets] = "Industry meets"
	};

	private static readonly Dictionary<PlayerDesk.ScoutingVenue, VenueGlyph> VenueGlyphs = new() {
		[PlayerDesk.ScoutingVenue.ClubsAndRoadhouses] = VenueGlyph.Club,
		[PlayerDesk.ScoutingVenue.TheatresAndSupperClubs] = VenueGlyph.Theatre,
		[PlayerDesk.ScoutingVenue.HonkyTonks] = VenueGlyph.HonkyTonk,
		[PlayerDesk.ScoutingVenue.IndustryMeets] = VenueGlyph.Meet
	};

	private static string TypicalAskText(PlayerDesk.ScoutingVenue venue) {
		(float low, float high) = PlayerDesk.Instance.TypicalAsk(venue);
		return $"asks ${low:N0}–{high:N0}";
	}

	private static string VenueOptionLabel(PlayerDesk.ScoutingVenue venue) {
		(int open, int close) = PlayerDesk.VenueHours(venue);
		string name = Cap(PlayerDesk.VenueName(venue));
		return $"{name}  ({VenueBlurb[venue]})  —  {TypicalAskText(venue)}  —  open {Hour12(open)}–{Hour12(close)}";
	}

	private void PageAandR() {
		PlayerDesk desk = PlayerDesk.Instance;

		// If the player is mid-negotiation, the contract menu takes over the page. A Pushover act
		// gets the plain single-click form; a Firm/Hardball act gets the negotiation scene instead.
		if (negotiating != null && desk.Slate.Contains(negotiating) && negotiating.HasBaseline) {
			if (negotiating.Talk != null) NegotiationScene(negotiating.Talk);
			else ContractForm(negotiating);
			return;
		}
		negotiating = null;

		Heading("A&R — WORK THE SCENE");
		Body($"Pick a room and go hear who's playing it. Costs {PlayerDesk.ScoutHours} hours, and each room only " +
			"draws a crowd at its own hours. What you hear is your read on the act, not the truth — a better ear " +
			"narrows the gap.");
		if (!hasUserSelectedVenue) {
			int now = TimeManager.Instance?.CurrentHour ?? 9;
			selectedVenue = VenueOrder.FirstOrDefault(venue => {
				(int open, int close) = PlayerDesk.VenueHours(venue);
				return now >= open && now < close;
			});
		}

		// The rooms are handbills in a row, the picked one pinned up. Which one is picked is the state the dropdown used to hold.
		int hourNow = TimeManager.Instance?.CurrentHour ?? 9;
		var board = new HBoxContainer();
		board.AddThemeConstantOverride("separation", 14);
		for (int i = 0; i < VenueOrder.Length; i++) {
			PlayerDesk.ScoutingVenue venue = VenueOrder[i];
			(int open, int close) = PlayerDesk.VenueHours(venue);
			var handbill = VenueHandbill.Make(VenueGlyphs[venue], VenueTitle[venue], VenueBlurb[venue], TypicalAskText(venue),
				$"open {Hour12(open)}–{Hour12(close)}", hourNow >= open && hourNow < close, venue == selectedVenue, i);
			handbill.TooltipText = VenueCharacter[venue];
			PlayerDesk.ScoutingVenue picked = venue;
			handbill.Pressed += () => { selectedVenue = picked; hasUserSelectedVenue = true; Refresh(); };
			board.AddChild(handbill);
		}
		content.AddChild(board);

		(int selectedOpen, int selectedClose) = PlayerDesk.VenueHours(selectedVenue);
		int currentHour = hourNow;
		bool selectedVenueOpen = currentHour >= selectedOpen && currentHour < selectedClose;
		var scout = Btn(selectedVenueOpen
			? $"GO SCOUTING  ({PlayerDesk.ScoutHours}h)"
			: currentHour < selectedOpen ? $"OPENS AT {Hour12(selectedOpen)}" : "CLOSED FOR TONIGHT");
		scout.Disabled = !selectedVenueOpen;
		scout.TooltipText = selectedVenueOpen
			? $"This room is open until {Hour12(selectedClose)}."
			: currentHour < selectedOpen ? $"This room opens at {Hour12(selectedOpen)}." : $"This room has closed for tonight; it opens at {Hour12(selectedOpen)}.";
		scout.CustomMinimumSize = new Vector2(220, 40);
		scout.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.ScoutVenue(selectedVenue, out string message); Say(message, ok); return ok; });
		var venueRow = new HBoxContainer();
		venueRow.AddThemeConstantOverride("separation", 14);
		var venueNote = new Label {
			Text = $"{VenueCharacter[selectedVenue]} Typical {TypicalAskText(selectedVenue)} — you have {Money(desk.Label.cashReserves)}.",
			AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill, VerticalAlignment = VerticalAlignment.Center
		};
		venueNote.AddThemeFontOverride("font", PaperTheme.Serif);
		venueNote.AddThemeFontSizeOverride("font_size", 17);
		venueNote.AddThemeColorOverride("font_color", Ink);
		venueRow.AddChild(venueNote);
		venueRow.AddChild(scout);
		content.AddChild(venueRow);

		if (desk.Slate.Count == 0) Body("No acts on the pad. Go hear somebody, or bring a notebook entry back without another scouting trip.");
		else {
			Heading($"CAUGHT {desk.SlateDate.ToHeadlineString()}  —  {PlayerDesk.VenueName(desk.Slate[0].Venue)}");
			foreach (PlayerDesk.Prospect prospect in desk.Slate.ToList()) ProspectCard(prospect);
		}

		Heading($"A&R NOTEBOOK  ({desk.Notebook.Count}/6)");
		if (desk.Notebook.Count == 0) Body("Keep an eye on an act you cannot sign yet.");
		foreach (PlayerDesk.WatchNote entry in desk.Notebook.ToList()) {
			SimulatedArtist artist = entry.Artist;
			if (artist == null) continue;
			bool signedByYou = !string.IsNullOrEmpty(artist.labelId) && artist.labelId == desk.Label?.labelId;
			bool signedElsewhere = !string.IsNullOrEmpty(artist.labelId) && !signedByYou;
			string labelName = signedElsewhere ? ChartManager.Instance?.GetLabelName(artist.labelId) ?? "another label" : null;
			GameDate today = TimeManager.Instance?.CurrentDate ?? entry.LastSeen;
			int ageDays = Mathf.Max(0, (new DateTime(today.year, today.month, today.day) - new DateTime(entry.LastSeen.year, entry.LastSeen.month, entry.LastSeen.day)).Days);
			string freshness = ageDays == 0 ? "seen today" : $"last seen {ageDays} days ago";
			if (ageDays >= 30) freshness += "  •  stale read";
			string status = signedElsewhere ? $"\n    RIVAL SIGNED THEM: {labelName}."
				: signedByYou ? "\n    SIGNED — they're on your roster now."
				: entry.HeldUntil.HasValue ? $"\n    HANDSHAKE: they'll wait for you until {entry.HeldUntil.Value.ToHeadlineString()}."
				: entry.CirclingResolves.HasValue ? $"\n    {entry.CirclingLabel ?? "A rival"} is circling them — the deal lands {entry.CirclingResolves.Value.ToHeadlineString()} unless you step in."
				: "";
			string potential = entry.Rough?.LastOrDefault();
			Body($"{artist.stageName} — {GenreNameFormatter.Format(artist.primaryGenre)}  •  {freshness}  •  {entry.Note}" +
				$"{(potential != null ? "\n    " + potential : "")}{status}");
			var actions = new HBoxContainer();
			actions.AddThemeConstantOverride("separation", 10);
			var revisit = Btn("BRING BACK TO THE PAD");
			revisit.Disabled = signedElsewhere || signedByYou;
			revisit.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.RevisitNotebookAct(artist.artistId, out string message); Say(message, ok); return ok; });
			actions.AddChild(revisit);
			if (!signedElsewhere && !signedByYou) {
				// Time instead of cash: shake on it and a rival cannot sign them for a week.
				bool canHold = desk.CanHoldWithHandshake(entry, out string holdWhy);
				var hold = Btn($"HOLD THEM — HANDSHAKE ({PlayerDesk.HandshakeHours}h)");
				hold.Disabled = !canHold;
				hold.TooltipText = canHold
					? $"No money changes hands: {PlayerDesk.HandshakeHours} hours of talk buys {PlayerDesk.HandshakeDays} days when no rival can sign them. Two weeks at most."
					: holdWhy;
				string heldId = artist.artistId;
				hold.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.HoldWithHandshake(heldId, out string message); Say(message, ok); return ok; });
				actions.AddChild(hold);
			}
			var remove = Btn("REMOVE");
			remove.Pressed += () => Act(() => { PlayerDesk.Instance.RemoveFromNotebook(artist.artistId); Say("Removed from the notebook.", true); return true; });
			actions.AddChild(remove);
			content.AddChild(actions);
		}
	}

	/// <summary>One act on the pad, as a 3x5 index card: the name typed on the red rule, the verdict stamped on the
	/// card, what you heard them play, and the next move as small buttons along the foot.</summary>
	private void ProspectCard(PlayerDesk.Prospect prospect) {
		var sheet = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		var indexPaper = PaperStyleBox.Sheet(new Color("f3ebd0"), 20, 10, 8);
		indexPaper.HeaderRule = 52f;
		indexPaper.Burn = 0.7f; indexPaper.Falloff = 0.6f;
		sheet.AddThemeStyleboxOverride("panel", indexPaper);
		var card = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		card.AddThemeConstantOverride("separation", 4);
		// The publicity glossy pasted to the card: a halftone plate of the act, coded by genre and who is in the band.
		var cardBody = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		cardBody.AddThemeConstantOverride("separation", 16);
		cardBody.AddChild(new PortraitPhoto().Set(Portraits.ForAct(prospect.Artist), new Vector2(108, 136), prospect.Artist.artistId));
		cardBody.AddChild(card);
		sheet.AddChild(cardBody);

		float quality = prospect.ReadQuality;
		string qualityRead = PlayerDesk.ReadVerdict(quality);
		(string stampText, Color stampInk) = quality >= 0.72f ? ("Strong prospects", RubberStamp.Blue)
			: quality >= 0.50f ? ("Promising", RubberStamp.Blue)
			: quality >= 0.30f ? ("Another look", PaperTheme.Fade) : ("Long shot", RubberStamp.Red);

		// The red rule sits under this row: name in the typewriter face, genre, then the stamp.
		var head = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 42) };
		head.AddThemeConstantOverride("separation", 12);
		var name = new Label {
			Text = prospect.Artist.stageName, SizeFlagsHorizontal = SizeFlags.ExpandFill, VerticalAlignment = VerticalAlignment.Center,
			ClipText = true, TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis, TooltipText = prospect.Artist.stageName
		};
		name.AddThemeFontOverride("font", PaperTheme.Elite);
		name.AddThemeFontSizeOverride("font_size", 24);
		name.AddThemeColorOverride("font_color", Ink);
		head.AddChild(name);
		var genre = new Label { Text = GenreNameFormatter.Format(prospect.Artist.primaryGenre).ToUpperInvariant(), VerticalAlignment = VerticalAlignment.Center };
		genre.AddThemeFontOverride("font", PaperTheme.SansSemiBold);
		genre.AddThemeFontSizeOverride("font_size", 13);
		genre.AddThemeColorOverride("font_color", Rust);
		head.AddChild(genre);
		var stamp = new RubberStamp { MouseFilter = MouseFilterEnum.Stop, TooltipText = $"Your read: {qualityRead}{(prospect.ReadConfidence >= 0.7f ? " (close read)" : " (rough read)")}." }
			.Set(stampText, stampInk, -0.1f, 15);
		head.AddChild(stamp);
		card.AddChild(head);

		var meta = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill,
			Text = $"{prospect.Note}   •   asking ${prospect.AskingAdvance:N0}   •   {(prospect.ReadConfidence >= 0.7f ? "close read" : "rough read")}" };
		meta.AddThemeFontSizeOverride("font_size", 15);
		meta.AddThemeColorOverride("font_color", Ink);
		card.AddChild(meta);
		// Scouting the rough: Execution / Identity / Potential, read from tells -- never the ceiling itself.
		if (prospect.Rough is { Count: > 0 })
			foreach (string line in prospect.Rough) card.AddChild(FaintLine("    " + line));

		PlayerDesk.Prospect captured = prospect;
		// What the second look changed. The bar and the fog moved on every follow-up but the headline phrase seldom does,
		// so without this the two hours look like they did nothing.
		if (prospect.Learned is { Count: > 0 }) {
			var learned = new Label { Text = "WHAT THE SECOND LOOK TURNED UP" };
			learned.AddThemeFontSizeOverride("font_size", 13);
			learned.AddThemeColorOverride("font_color", Rust);
			card.AddChild(learned);
			for (int i = 0; i < prospect.Learned.Count; i++) {
				Label line = FaintLine("    • " + prospect.Learned[i]);
				if (i == 0) line.AddThemeColorOverride("font_color", Ink);
				card.AddChild(line);
			}
		}

		// The live set: what you caught on the night, and -- after a follow-up -- the rest of it.
		int shown = Mathf.Min(prospect.HeardCount, prospect.LiveSet.Count);
		int hidden = prospect.LiveSet.Count - shown;
		// The hook read is the ear on the tune itself, so it shows in both modes; fit is a studio question. Each tune
		// gets a bar and a few words and the set gets one sentence, so the choice is about the act, not a star count.
		if (shown == 0) card.AddChild(FaintLine("    (didn't catch their set)"));
		foreach (PlayerDesk.RepertoireItem item in prospect.LiveSet.Take(shown))
			card.AddChild(SongRow($"    ♪ \"{item.Title}\" ({item.SourceTag}){(prospect.NewlyHeard.Contains(item.Title) ? "  — new" : "")}", item.ReadHook, prospect.ReadConfidence,
				PolarPlayerPerception.DescribeCharacter(new PlayerDesk.MaterialChoice { SongId = item.SongId })));
		string setSummary = SetSummary(prospect.LiveSet.Take(shown).Select(item => item.ReadHook).ToList(), prospect.ReadConfidence);
		if (setSummary.Length > 0) card.AddChild(FaintLine("    THE SET: " + setSummary));
		if (hidden > 0) card.AddChild(FaintLine($"    …and {hidden} more you didn't catch — follow up to hear the full set."));

		// Small buttons along the foot, not a full-width bar each.
		var foot = new HBoxContainer();
		foot.AddThemeConstantOverride("separation", 10);
		if (!prospect.FollowedUp) {
			var follow = Primary($"FOLLOW UP ({PlayerDesk.FollowUpHours}h)");
			follow.CustomMinimumSize = new Vector2(0, 36);
			follow.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.FollowUp(captured, out string message); Say(message, ok); return ok; });
			foot.AddChild(follow);
		} else {
			var approach = Primary("APPROACH");
			approach.CustomMinimumSize = new Vector2(0, 36);
			approach.Pressed += () => {
				bool ok = PlayerDesk.Instance.ApproachToSign(captured, out string message);
				if (ok) negotiating = captured;
				Say(message, ok);
				Refresh();
			};
			foot.AddChild(approach);
		}
		if (!PlayerDesk.Instance.Notebook.Any(entry => entry.Artist?.artistId == prospect.Artist.artistId)) {
			var note = Btn("ADD TO NOTEBOOK");
			note.CustomMinimumSize = new Vector2(0, 36);
			note.Disabled = PlayerDesk.Instance.Notebook.Count >= 6;
			note.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.AddToNotebook(captured, out string message); Say(message, ok); return ok; });
			foot.AddChild(note);
		}
		if (PolarSongBehavior.UsePolarFitSelection && shown > 0) {
			var compare = Btn("COMPARE HEARD MATERIAL");
			compare.CustomMinimumSize = new Vector2(0, 36);
			compare.Pressed += () => {
				var preview = PaperModal.OpenClipboard(this, "A&R — HEARD MATERIAL", 1040);
				var column = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
				var scroll = ComparisonScroll(); preview.Body.AddChild(scroll); scroll.AddChild(column);
				var songs = Option();
				foreach (var item in prospect.LiveSet.Take(shown)) {
					string character = PolarPlayerPerception.DescribeCharacter(new PlayerDesk.MaterialChoice { SongId = item.SongId });
					songs.AddItem(character.Length == 0 ? item.Title : $"{item.Title}  —  {character}");
				}
				column.AddChild(songs); var host = new VBoxContainer(); column.AddChild(host);
				void Update() {
					Clear(host); var heard = prospect.LiveSet[songs.Selected];
					var material = new PlayerDesk.MaterialChoice { Title = heard.Title, SongId = heard.SongId, ReferenceMasterId = heard.ReferenceMasterId,
						Kind = heard.IsOriginal ? PlayerDesk.MaterialKind.Original : PlayerDesk.MaterialKind.LiveCover, Detail = heard.SourceTag };
					host.AddChild(ComparisonCard(prospect.Artist, material, prospect.FollowedUp ? PolarEvidenceGate.FollowUp : PolarEvidenceGate.FirstListen,
						"venue:" + PlayerDesk.Instance.SlateDate + ":" + prospect.Artist.artistId, null, 0, hearing: new PolarHearing {
							source = PolarHearingSource.Venue, place = PlayerDesk.VenueName(prospect.Venue), when = PlayerDesk.Instance.SlateDate,
							heardHook = heard.ReadHook, heardHookConfidence = prospect.ReadConfidence }, onSheet: true));
				}
				songs.ItemSelected += _ => Update(); Update();
				preview.AddButton("DONE", null, PaperModal.ButtonKind.Primary);
			};
			foot.AddChild(compare);
		}
		var footGap = new Control { CustomMinimumSize = new Vector2(0, 4) };
		card.AddChild(footGap);
		card.AddChild(foot);
		content.AddChild(sheet);
	}

	/// <summary>The contract menu: the label's opening offer, editable, then put on the table.
	/// Pushover only -- signing is a single accept-or-walk click.</summary>
	private void ContractForm(PlayerDesk.Prospect prospect) {
		ContractTermSheet b = prospect.Baseline;
		ContractTermSheet prefill = prospect.Draft ?? b;
		Heading($"CONTRACT — {prospect.Artist.stageName.ToUpperInvariant()}");
		if (!string.IsNullOrEmpty(b.DemandSummary))
			Body($"Their ask: {b.DemandSummary}");
		Body(prospect.PushoverCountered
			? "They've named their number. Put it forward again, or step back -- a second offer under it and they walk."
			: "They'll sign near their ask, but not at any price: go too low and they counter once, then walk. " +
				$"The meeting takes {PlayerDesk.PushoverSignHours} hours, signed or not, and the advance is charged when they sign.");

		TermsForm(prefill, $"OFFER CONTRACT  ({PlayerDesk.PushoverSignHours}h)",
			(advance, royalty, term, singles, labelPub, artistControl) => {
				bool signed = PlayerDesk.Instance.OfferContract(prospect, advance, royalty, term, singles,
					labelPub, artistControl, out string message);
				GameDate today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
				bool walked = !signed && prospect.CooldownUntil is GameDate until && today < until;
				if (signed || walked) negotiating = null;
				Say(message, signed);
				Refresh();
			},
			() => { negotiating = null; Refresh(); }, b, prospect.Artist.stageName);
	}

	/// <summary>The editable grid shared by the plain contract form, every tabling round of a
	/// Firm/Hardball negotiation, and a renewal. Just the fields and the two buttons -- caller
	/// supplies the prefill, what the submit button says and does, and what "not now" does.</summary>
	private void TermsForm(ContractTermSheet prefill, string submitLabel,
			Action<float, float, int, int, bool, bool> onSubmit, Action onCancel, ContractTermSheet? ask = null, string artistName = null) {
		// The agreement is a carbon duplicate: onionskin with the label's letterhead, typed field rubrics, and a
		// signature line at the foot. The buttons below it are what you do with the sheet.
		Color carbonInk = new("3d4a8f");
		var sheet = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		var carbon = PaperStyleBox.Sheet(new Color("e9e6d8"), 28, 18, 8, new Color("a7a28e"));
		carbon.Grain = 0.8f; carbon.Burn = 0.6f; carbon.Falloff = 0.5f;
		sheet.AddThemeStyleboxOverride("panel", carbon);
		content.AddChild(sheet);
		var form = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		form.AddThemeConstantOverride("separation", 10);
		sheet.AddChild(form);

		Label Typed(string text, int size, Color color, bool bold = false) {
			var label = new Label { Text = text };
			label.AddThemeFontOverride("font", bold ? PaperTheme.TypedBold : PaperTheme.Typed);
			label.AddThemeFontSizeOverride("font_size", size);
			label.AddThemeColorOverride("font_color", color);
			return label;
		}
		var letterhead = new HBoxContainer();
		letterhead.AddThemeConstantOverride("separation", 12);
		AILabel house = PlayerDesk.Instance?.Label;
		LabelBrand houseBrand = LabelBrand.For(house);
		string houseName = house?.labelName ?? "The Label";
		letterhead.AddChild(new LabelCrest().Set(houseBrand, houseName, 48f));
		var heading = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ShrinkCenter };
		heading.AddThemeConstantOverride("separation", 0);
		// The house name is printed in the label's own lettering and ink; the rest of the sheet stays carbon blue.
		var houseLine = new Label { Text = houseBrand.DisplayName(houseName), ClipText = true, TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis };
		houseLine.AddThemeFontOverride("font", PaperTheme.Lettering(houseBrand.Lettering));
		houseLine.AddThemeFontSizeOverride("font_size", houseBrand.Lettering == LetteringStyle.Script ? 34 : 24);
		houseLine.AddThemeColorOverride("font_color", houseBrand.Pair.Ink);
		heading.AddChild(houseLine);
		heading.AddChild(Typed("ARTIST RECORDING AGREEMENT  ·  DUPLICATE", 13, carbonInk));
		letterhead.AddChild(heading);
		letterhead.AddChild(new RubberStamp().Set("Duplicate", carbonInk, -0.13f, 14, 0.5f));
		form.AddChild(letterhead);
		form.AddChild(new ColorRect { Color = new Color(carbonInk, 0.55f), CustomMinimumSize = new Vector2(0, 2), MouseFilter = MouseFilterEnum.Ignore });

		var grid = new GridContainer { Columns = 2 };
		grid.AddThemeConstantOverride("h_separation", 18);
		grid.AddThemeConstantOverride("v_separation", 8);
		form.AddChild(grid);

		grid.AddChild(FormLabel("Advance ($)"));
		// Step of 5, not 25 -- a coarse step silently snapped a typed $35 down to $25 on finalize.
		var advance = Spin(0, 100000, 1, Mathf.Round(prefill.Advance));
		grid.AddChild(advance);

		grid.AddChild(FormLabel("Royalty (%)"));
		// Opens on what they expect. You may write it lower -- down to half a point -- but the further
		// under their number you go, the likelier they push back on it.
		var royalty = Spin(PlayerDesk.PlayerRoyaltyFloor * 100f, 15, 0.25,
			Mathf.Round(prefill.RoyaltyRate * 4000f) / 40f);
		grid.AddChild(royalty);

		grid.AddChild(FormLabel("Term (years)"));
		var term = Spin(1, 7, 1, prefill.TermYears);
		grid.AddChild(term);

		grid.AddChild(FormLabel("Deliverables (singles)"));
		// 2-3 singles a year is the period norm for a new act, tapering to none once a career is
		// established -- the default already reflects that; this just lets the player move off it.
		var singles = Spin(0, 30, 1, prefill.SinglesObligation);
		grid.AddChild(singles);

		var publishingLabel = FormLabel("Publishing");
		grid.AddChild(publishingLabel);
		var labelPub = Check("Label keeps the publishing", prefill.LabelOwnsPublishing);
		grid.AddChild(labelPub);

		var controlLabel = FormLabel("Creative control");
		grid.AddChild(controlLabel);
		var artistControl = Check("Artist has creative control", prefill.ArtistCreativeControl);
		grid.AddChild(artistControl);

		// The two gives are not free, but the price lives in a hover rather than on the page: the long-tail cost
		// (mechanicals at 1,000 / 10,000 / 100,000 copies; the studio veto) leads with the box's current state.
		foreach (Label rowLabel in new[] { publishingLabel, controlLabel }) rowLabel.MouseFilter = MouseFilterEnum.Stop;
		void UpdateGives() {
			publishingLabel.TooltipText = labelPub.TooltipText = PlayerDesk.PublishingTooltip(labelPub.ButtonPressed);
			controlLabel.TooltipText = artistControl.TooltipText = PlayerDesk.CreativeControlTooltip(artistControl.ButtonPressed);
		}
		labelPub.Toggled += _ => UpdateGives();
		artistControl.Toggled += _ => UpdateGives();
		UpdateGives();

		var lowball = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
		lowball.AddThemeFontSizeOverride("font_size", 15);
		lowball.AddThemeColorOverride("font_color", Rust);
		form.AddChild(lowball);
		void UpdateLowball() {
			if (ask is not ContractTermSheet theirs || theirs.Advance <= 0f) { lowball.Visible = false; return; }
			float under = PlayerDesk.UnderAskFraction(theirs, new ContractTermSheet((float)advance.Value, (float)royalty.Value / 100f,
				(int)term.Value, (int)singles.Value, labelPub.ButtonPressed, artistControl.ButtonPressed,
				theirs.NegotiationDifficulty, theirs.Manager, theirs.ManagerName, theirs.DemandSummary));
			lowball.Visible = under >= 0.20f;
			lowball.Text = $"{under:P0} under their ask. They'll sign, and they'll remember it: the renewal comes back priced higher.";
		}
		advance.ValueChanged += _ => UpdateLowball();
		royalty.ValueChanged += _ => UpdateLowball();
		UpdateLowball();

		var commitment = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
		commitment.AddThemeColorOverride("font_color", Ink);
		form.AddChild(commitment);
		void UpdateCommitment() {
			AILabel label = PlayerDesk.Instance?.Label;
			if (label == null) { commitment.Text = "No label cash available."; return; }
			float reserve = label.GetMonthlyOverhead() * 2f;
			float after = label.cashReserves - (float)advance.Value;
			bool clears = after > reserve;
			commitment.Text = $"Cash after advance: {Money(after)}  ·  signing reserve: ${reserve:N0} (2 months of ${label.GetMonthlyOverhead():N0} overhead)  ·  " +
				(clears ? "reserve covered" : $"must leave more than ${reserve:N0} — lower the advance or wait for more cash");
			commitment.AddThemeColorOverride("font_color", clears ? Ink : Rust);
		}
		advance.ValueChanged += _ => UpdateCommitment();
		UpdateCommitment();

		// The foot of the agreement: where the artist signs, where the label does, and the day.
		var signRow = new HBoxContainer { CustomMinimumSize = new Vector2(0, 48) };
		signRow.AddThemeConstantOverride("separation", 36);
		void SignatureLine(string caption, float weight) {
			var column = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsStretchRatio = weight, SizeFlagsVertical = SizeFlags.ShrinkEnd };
			column.AddThemeConstantOverride("separation", 2);
			column.AddChild(new ColorRect { Color = new Color("2b2115"), CustomMinimumSize = new Vector2(0, 1), MouseFilter = MouseFilterEnum.Ignore });
			column.AddChild(Typed(caption, 13, new Color("45381f")));
			signRow.AddChild(column);
		}
		SignatureLine(string.IsNullOrEmpty(artistName) ? "Signed for the artist" : $"Signed for the artist  ({artistName})", 1.4f);
		SignatureLine("For the label", 1f);
		SignatureLine($"Date  {(TimeManager.Instance?.CurrentDate ?? GameDate.StartDate).ToHeadlineString()}", 0.8f);
		form.AddChild(signRow);

		var buttons = new HBoxContainer();
		buttons.AddThemeConstantOverride("separation", 12);
		var offer = Primary(submitLabel);
		offer.CustomMinimumSize = new Vector2(260, 44);
		offer.Pressed += () => onSubmit((float)advance.Value, (float)royalty.Value / 100f,
			(int)term.Value, (int)singles.Value, labelPub.ButtonPressed, artistControl.ButtonPressed);
		buttons.AddChild(offer);

		var cancel = Btn("NOT NOW");
		cancel.CustomMinimumSize = new Vector2(150, 44);
		cancel.Pressed += () => onCancel();
		buttons.AddChild(cancel);
		content.AddChild(buttons);
	}

	// ========================================================================
	// CONTRACT NEGOTIATION -- Firm/Hardball acts (see SimTools/ContractNegotiationDirective.md Part 2)
	// ========================================================================

	/// <summary>The negotiation scene: table an offer, or answer the objection it drew. Same
	/// take-over-the-page shape as PageCall for the Rolodex -- this IS that loop, different nouns.</summary>
	private void NegotiationScene(ContractTalk talk) {
		string verb = talk.IsRenewal ? "RENEWING" : "NEGOTIATING";
		Heading($"{verb} — {talk.Artist.stageName.ToUpperInvariant()}  ({talk.posture.ToString().ToUpperInvariant()})");
		if (talk.roundsPlayed == 0) Body(talk.ask.DemandSummary);

		if (talk.log.Count > 0) {
			var frame = new PanelContainer();
			frame.AddThemeStyleboxOverride("panel", new StyleBoxFlat {
				BgColor = new Color("e7d8b4"), ContentMarginLeft = 12, ContentMarginRight = 12,
				ContentMarginTop = 10, ContentMarginBottom = 10,
			});
			var box = new VBoxContainer();
			box.AddThemeConstantOverride("separation", 6);
			frame.AddChild(box);
			content.AddChild(frame);
			foreach (string line in talk.log.Take(4)) {
				var lbl = new Label { Text = line, AutowrapMode = TextServer.AutowrapMode.WordSmart };
				lbl.AddThemeFontSizeOverride("font_size", 15);
				lbl.AddThemeColorOverride("font_color", Ink);
				box.AddChild(lbl);
			}
		}

		// Done is never rendered: every path that reaches it (sign or walk) clears `negotiating` and
		// refreshes in the same handler, same as the plain ContractForm's sign button does today.
		switch (talk.stage) {
			case ContractTalkStage.Tabling:   NegotiationTablingForm(talk);  break;
			case ContractTalkStage.Objection: NegotiationObjection(talk);    break;
		}
	}

	private void NegotiationTablingForm(ContractTalk talk) {
		string label = talk.roundsPlayed == 0
			? $"TABLE OFFER  ({PlayerDesk.NegotiationRoundHours}h)"
			: $"TABLE AGAIN  ({PlayerDesk.NegotiationRoundHours}h)";
		float advanceFloor = PlayerDesk.MinimumAdvanceForAcceptance(talk);
		Body($"Their ask: ${talk.ask.Advance:N0}, {talk.ask.RoyaltyRate:P1}, {talk.ask.TermYears} year(s), " +
			$"{talk.ask.SinglesObligation} single(s){(talk.ask.LabelOwnsPublishing ? "" : ", they keep the publishing")}{(talk.ask.ArtistCreativeControl ? ", they hold creative control" : "")}. This {talk.posture.ToString().ToLowerInvariant()} act needs at least ${advanceFloor:N0} up front plus terms that meet their overall threshold. " +
			$"Each table round costs {PlayerDesk.NegotiationRoundHours} hours.");
		Body(talk.draftOffer.HasValue ? "The last meeting could not be held; your entered terms are still below." :
			talk.roundsPlayed == 0 ? "These fields start on your label's standard paper: you keep the publishing and have the final word. Their ask is above; every give you tick is a concession." : "These fields start with your last offer. Change any term before putting it back on the table.");
		TermsForm(PlayerDesk.CurrentOffer(talk), label,
			(advance, royalty, term, singles, labelPub, artistControl) => {
				bool ok = PlayerDesk.Instance.TableOffer(talk, advance, royalty, term, singles, labelPub, artistControl, out string message);
				Say(message, ok);
				CloseTalkIfDone(talk);
				Refresh();
			},
			() => {
				bool ok = PlayerDesk.Instance.WalkFromTalk(talk, out string message);
				Say(message, ok);
				CloseTalkIfDone(talk);
				Refresh();
			}, talk.ask, talk.Artist.stageName);
	}

	/// <summary>A negotiation scene serves both a new signing (closes `negotiating`) and a renewal
	/// (closes `renewingArtist`) -- clear whichever one this talk actually belongs to once it ends.</summary>
	private void CloseTalkIfDone(ContractTalk talk) {
		if (talk.stage != ContractTalkStage.Done) return;
		if (talk.IsRenewal) renewingArtist = null; else negotiating = null;
	}

	private void NegotiationObjection(ContractTalk talk) {
		Heading("HE COUNTERS");
		ContractTermSheet current = PlayerDesk.CurrentOffer(talk);
		float advanceFloor = PlayerDesk.MinimumAdvanceForAcceptance(talk);
		var stand = new Label {
			Text = PlayerDesk.HardLineBroken(talk, current).HasValue
				? $"A hard line: they keep the publishing, whatever the money. No advance moves this; tick it back to them and table again. {talk.patienceLeft} of {talk.patienceMax} round(s) of patience left."
				: (talk.lastOfferValue >= talk.reservation && current.Advance < advanceFloor
					? $"The terms read well overall ({talk.lastOfferValue * 100f:F0}%), but they won't go below ${advanceFloor:N0} up front; you offered ${current.Advance:N0}. "
					: $"Package read: {talk.lastOfferValue * 100f:F0}% (needs {talk.reservation * 100f:F0}%). Advance floor: ${advanceFloor:N0}; you offered ${current.Advance:N0}. ") +
				$"{talk.patienceLeft} of {talk.patienceMax} round(s) of patience left.",
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
		};
		stand.AddThemeColorOverride("font_color", Heard);
		content.AddChild(stand);

		NegotiationCounterButton(talk, ContractCounter.SweetenAxis,
			"SWEETEN IT", "Go back and raise the number he's actually stuck on.");
		if (PlayerDesk.CanTradeAxes(talk))
			NegotiationCounterButton(talk, ContractCounter.TradeAxes,
				"TRADE", "Give back the publishing and the final word; the advance comes down to match.");
		NegotiationCounterButton(talk, ContractCounter.Promise,
			$"PROMISE  ({PlayerDesk.NegotiationRoundHours}h)", "More sides, a real push -- costs nothing today.");
		NegotiationCounterButton(talk, ContractCounter.HoldFirm,
			$"HOLD FIRM  ({PlayerDesk.NegotiationRoundHours}h)", "Table it again unchanged and see who blinks.");
		NegotiationCounterButton(talk, ContractCounter.Walk,
			"WALK AWAY", "Step back from the table. Nothing's burned.");
	}

	private void NegotiationCounterButton(ContractTalk talk, ContractCounter counter, string label, string sub) {
		var btn = Btn(label);
		btn.CustomMinimumSize = new Vector2(0, 38);
		btn.Pressed += () => {
			bool ok = PlayerDesk.Instance.PlayNegotiationCounter(talk, counter, out string message);
			Say(message, ok);
			CloseTalkIfDone(talk);
			Refresh();
		};
		content.AddChild(btn);
		var note = new Label { Text = "     " + sub, AutowrapMode = TextServer.AutowrapMode.WordSmart };
		note.AddThemeFontSizeOverride("font_size", 14);
		note.AddThemeColorOverride("font_color", Heard);
		content.AddChild(note);
	}


	// ========================================================================
	// ROSTER  +  the MANAGE window (Songs + Studio folded in)
	// ========================================================================

	private void PageRoster() {
		PlayerDesk desk = PlayerDesk.Instance;

		// A renewal in progress takes over the page, same as a signing does on A&R.
		if (renewingArtist != null && desk.PendingRenewal?.Artist == renewingArtist) {
			RenewalScene(desk.PendingRenewal);
			return;
		}
		renewingArtist = null;

		// Clicking an act opens its MANAGE window in place of the list.
		SimulatedArtist managed = managingArtistId == null ? null
			: desk.Roster.FirstOrDefault(a => a.artistId == managingArtistId);
		if (managed != null) { RenderManage(desk, managed); return; }
		managingArtistId = null;

		Heading("ROSTER");
		if (!desk.Roster.Any()) { Body("Nobody signed yet. Go find an act on the A&R page."); return; }
		Body("Click an act to manage them — their songbook, teaching covers, and cutting records all live in there.");

		int year = TimeManager.Instance?.CurrentDate.year ?? 1960;
		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		foreach (SimulatedArtist artist in desk.Roster.ToList()) {
			bool matured = RosterManager.IsContractMatured(artist, year, week);
			int songs = desk.RepertoireFor(artist.artistId).Count
				+ desk.UnrecordedSongs.Count(s => s.ArtistId == artist.artistId);
			string manager = artist.manager == ManagerArchetype.None ? "" : $"   •   managed by {artist.managerName ?? "a manager"}";
			// Each act is a 45 sleeve: the publicity photo, the name typed across it, the figures, and the player's disc in the window.
			var sleeve = new SleeveCard().Set(artist, desk.Label,
				$"{GenreNameFormatter.Format(artist.primaryGenre)}  •  {Words(artist.careerState.ToString())}", matured);
			sleeve.AddFact($"{artist.totalReleases} {CountWord(artist.totalReleases, "release")}   •   {artist.top40Hits} Top 40   •   {songs} in the songbook");
			sleeve.AddFact($"{artist.royaltyRate:P1} royalty   •   {(matured ? "CONTRACT UP" : $"expires {artist.contractExpiresYear}")}{manager}", matured ? Rust : null);

			SimulatedArtist captured = artist;
			if (matured) {
				var renew = Primary("RENEW");
				renew.Pressed += () => {
					bool ok = PlayerDesk.Instance.ApproachRenewal(captured, out string message);
					if (ok) renewingArtist = captured;
					Say(message, ok);
					Refresh();
				};
				sleeve.AddVerb(renew);
			}
			var manage = Btn("MANAGE");
			manage.Pressed += () => { managingArtistId = captured.artistId; browsingCovers = false; Refresh(); };
			sleeve.AddVerb(manage);
			var dossier = Btn("DOSSIER");
			dossier.Pressed += () => UIManager.Instance?.OpenArtist(captured.artistId, true);
			sleeve.AddVerb(dossier);
			content.AddChild(sleeve);
		}
	}

	/// <summary>The renewal menu for a matured contract: Pushover gets the same quick one-click form
	/// as a first signing; Firm/Hardball opens the same negotiation scene, different nouns.</summary>
	private void RenewalScene(RenewalOffer offer) {
		if (offer.Posture != NegotiationPosture.Pushover) { NegotiationScene(offer.Talk); return; }

		Heading($"RENEW — {offer.Artist.stageName.ToUpperInvariant()}");
		if (!string.IsNullOrEmpty(offer.Ask.DemandSummary)) Body($"Their ask: {offer.Ask.DemandSummary}");
		Body("Your terms are shown below. The meeting costs " +
			$"{PlayerDesk.NegotiationRoundHours} hours; the advance is charged when they sign.");

		TermsForm(offer.Draft ?? offer.Ask, $"RENEW  ({PlayerDesk.NegotiationRoundHours}h)",
			(advance, royalty, term, singles, labelPub, artistControl) => {
				bool renewed = PlayerDesk.Instance.RenewContract(offer.Artist, advance, royalty, term, singles,
					labelPub, artistControl, out string message);
				if (renewed) renewingArtist = null;
				Say(message, renewed);
				Refresh();
			},
			() => { renewingArtist = null; Refresh(); }, offer.Ask, offer.Artist.stageName);
	}

	/// <summary>The MANAGE window for one act: repertoire (write / teach a cover) and the studio.</summary>
	private void RenderManage(PlayerDesk desk, SimulatedArtist artist) {
		var back = Btn("‹ BACK TO ROSTER");
		back.CustomMinimumSize = new Vector2(200, 36);
		back.Pressed += () => { managingArtistId = null; browsingCovers = false; Refresh(); };
		content.AddChild(back);

		Heading($"MANAGING — {artist.stageName.ToUpperInvariant()}");
		Body($"{GenreNameFormatter.Format(artist.primaryGenre)}  •  {Words(artist.careerState.ToString())}  •  " +
			$"{artist.royaltyRate:P1} royalty  •  ${artist.unrecoupedAdvance:N0} unrecouped  •  contract to {artist.contractExpiresYear}");
		var openDossier = Btn("OPEN FULL DOSSIER");
		openDossier.CustomMinimumSize = new Vector2(220, 36);
		openDossier.Pressed += () => UIManager.Instance?.OpenArtist(artist.artistId, true);
		content.AddChild(openDossier);

		if (!desk.AtHome) {
			var banner = new Label {
				Text = $"You're on the road in {desk.CurrentCity?.name ?? "town"} — writing, teaching covers and the studio " +
					"need the office. Drive home (DISTRIBUTION) to work with the act.",
				AutowrapMode = TextServer.AutowrapMode.WordSmart
			};
			banner.AddThemeColorOverride("font_color", Rust);
			content.AddChild(banner);
		}

		RepertoireSection(desk, artist);
		StudioSection(desk, artist);
	}

	private void RepertoireSection(PlayerDesk desk, SimulatedArtist artist) {
		Heading("REPERTOIRE");
		Body("What this act can play. Have them write their own, or teach them a cover from the catalog — either way " +
			"it's ready to record.");

		var actions = new HBoxContainer();
		actions.AddThemeConstantOverride("separation", 12);
		var write = Btn($"WRITE A NEW SONG ({PlayerDesk.WriteHours}h)");
		write.CustomMinimumSize = new Vector2(260, 40);
		write.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.WriteSongs(artist, out string message); Say(message, ok); return ok; });
		actions.AddChild(write);

		var teach = Btn(browsingCovers ? "HIDE THE CATALOG" : "TEACH A COVER");
		teach.CustomMinimumSize = new Vector2(200, 40);
		teach.Pressed += () => { browsingCovers = !browsingCovers; Refresh(); };
		actions.AddChild(teach);

		// Commissioning is now its own step: a writer delivers a specific song into the set, by name and
		// with a read, before the studio -- no more blind "cut a professional song" at the console.
		if (desk.IsCommissioning(artist.artistId)) {
			var pending = Btn("WRITER AT WORK…");
			pending.CustomMinimumSize = new Vector2(240, 40);
			pending.Disabled = true;
			actions.AddChild(pending);
		} else {
			var commission = Btn($"COMMISSION A SONG (${PlayerDesk.CommissionFee:N0})");
			commission.CustomMinimumSize = new Vector2(240, 40);
			commission.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.CommissionSong(artist, out string message); Say(message, ok); return ok; });
			actions.AddChild(commission);
		}
		content.AddChild(actions);

		// The act's set: their own numbers and taught covers, plus anything you wrote, plus covers still in
		// rehearsal. A number they've already cut shows as recorded (linked to its record once it's out) and
		// drops out of the studio's material list.
		var have = desk.RepertoireFor(artist.artistId).ToList();
		var written = desk.SongsFor(artist.artistId).ToList();
		var rehearsing = desk.RehearsalsFor(artist.artistId).ToList();
		if (have.Count == 0 && written.Count == 0 && rehearsing.Count == 0)
			Body("    Nothing in the set yet.");
		foreach (PlayerDesk.RepertoireItem item in have) {
			string tag = item.IsOriginal ? "their own" : item.SourceTag;
			if (item.Recorded) RecordedLine(desk, $"\"{item.Title}\"", tag, item.RecordedId, artist.artistId);
			else if (PolarSongBehavior.UsePolarFitSelection) content.AddChild(SongRow($"    ♪ \"{item.Title}\" ({tag})", item.ReadHook, 0.8f,
				PolarPlayerPerception.DescribeCharacter(new PlayerDesk.MaterialChoice { SongId = item.SongId })));
			else SongLine($"\"{item.Title}\"", tag, item.ReadHook);
		}
		foreach (PlayerDesk.Song song in written) {
			if (song.Recorded) RecordedLine(desk, $"\"{song.Title}\"", "their own", song.RecordedId, artist.artistId);
			else if (PolarSongBehavior.UsePolarFitSelection) Body($"    ♪ \"{song.Title}\" (their own) — provisional demo");
			else SongLine($"\"{song.Title}\"", "their own", song.Hook);
		}
		foreach (PlayerDesk.CoverRehearsal r in rehearsing)
			RehearsingLine(r);
		var uncut = have.Where(item => !item.Recorded).Select(item => item.ReadHook).Concat(PolarSongBehavior.UsePolarFitSelection ? Enumerable.Empty<float>() : written.Where(song => !song.Recorded).Select(song => song.Hook)).ToList();
		string setLine = SetSummary(uncut, 0.8f);
		if (setLine.Length > 0) content.AddChild(FaintLine("    THE SET: " + setLine + (PolarSongBehavior.UsePolarFitSelection ? " Compare the songs in the studio below." : "")));

		if (browsingCovers) CoverBrowser(desk, artist);
	}

	private void CoverBrowser(PlayerDesk desk, SimulatedArtist artist) {
		Heading("THE CATALOG — TEACH THEM A COVER");
		// An act works up only one cover at a time; while one's in rehearsal the catalog is closed to them.
		if (desk.IsRehearsing(artist.artistId)) {
			Body($"{artist.stageName} is already working a cover up — let them finish it before starting another.");
			return;
		}
		int days = desk.EstimateCoverLearnDays(artist);
		Body($"Pick a song for {artist.stageName} to work up. It takes a short setup ({PlayerDesk.TeachHours}h) to start " +
			$"them on it, then about {days} day{(days == 1 ? "" : "s")} of rehearsal — they're a quicker study the more " +
			"capable they are — before it's in their set by name.");
		List<PlayerDesk.MaterialChoice> covers = desk.CoverCatalogFor(artist).ToList();
		if (covers.Count == 0) { Body("    Nothing in the catalog they don't already play."); return; }
		foreach (PlayerDesk.MaterialChoice cover in covers) {
			var row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 12);
			var text = new Label {
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				Text = (PolarSongBehavior.UsePolarFitSelection ? $"    ♪ \"{cover.Title}\" ({cover.Detail}) — {GenreNameFormatter.Format(cover.Genre)}" :
					$"    ♪ \"{cover.Title}\"  ({cover.Detail})  —  {GenreNameFormatter.Format(cover.Genre)}   •   {PolarPlayerPerception.DescribeHook(cover.Hook, 0.8f)}")
					+ (PolarPlayerPerception.DescribeCharacter(cover) is { Length: > 0 } coverCharacter ? "\n      " + coverCharacter : "")
			};
			text.AddThemeColorOverride("font_color", Ink);
			if (PolarSongBehavior.UsePolarFitSelection) {
				// The hook is the song's own pull, not a fit question, so the catalogue shows it as a bar under the title
				// (the same read the non-Polar line prints); fit with the act stays behind COMPARE / PREVIEW.
				var stack = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
				stack.AddThemeConstantOverride("separation", 2);
				text.SizeFlagsHorizontal = SizeFlags.ExpandFill;
				stack.AddChild(text);
				var hookRow = new HBoxContainer();
				hookRow.AddThemeConstantOverride("separation", 10);
				hookRow.AddChild(new Control { CustomMinimumSize = new Vector2(24, 0) });
				hookRow.AddChild(new ReadBar().Set(cover.Hook, 0.8f));
				var hookWords = new Label { Text = PolarPlayerPerception.DescribeHook(cover.Hook, 0.8f) };
				hookWords.AddThemeFontSizeOverride("font_size", 14);
				hookWords.AddThemeColorOverride("font_color", ReadBar.ColorFor(cover.Hook).Darkened(0.2f));
				hookRow.AddChild(hookWords);
				stack.AddChild(hookRow);
				row.AddChild(stack);
			} else row.AddChild(text);
			string songId = cover.SongId;
			var take = Btn($"TEACH (~{days}d)");
			take.CustomMinimumSize = new Vector2(150, 36);
			take.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.TeachCover(artist, songId, out string message); Say(message, ok); return ok; });
			row.AddChild(take);
			if (PolarSongBehavior.UsePolarFitSelection) {
				var compare = Btn("COMPARE / PREVIEW");
				compare.Pressed += () => { polarCatalogSong = cover.SongId; Refresh(); };
				row.AddChild(compare);
			}
			content.AddChild(row);
			if (PolarSongBehavior.UsePolarFitSelection && polarCatalogSong == cover.SongId)
				content.AddChild(ComparisonCard(artist, cover, PolarEvidenceGate.Demo, "catalogue:" + cover.SongId, null, 0,
					hearing: new PolarHearing { source = PolarHearingSource.Record, fitWithAct = true }));
		}
	}

	private static readonly PlayerDesk.StudioTier[] Tiers =
		{ PlayerDesk.StudioTier.Budget, PlayerDesk.StudioTier.Mid, PlayerDesk.StudioTier.Top };

	private void StudioSection(PlayerDesk desk, SimulatedArtist artist) {
		Heading("THE STUDIO");

		// One session sits on the console at a time. If it's this act's, show it; if another act's, say so.
		if (desk.Session != null) {
			if (desk.Session.ArtistId == artist.artistId) { TakesView(desk.Session, artist); return; }
			SimulatedArtist other = ArtistManager.Instance?.GetArtist(desk.Session.ArtistId);
			Body($"A session is on the console with {other?.stageName ?? "another act"} — print or scrap it before you " +
				"book this one.");
			return;
		}

		Body("When you decide they're ready, book the room. A 45 is an A-side and a B-side, so cut at least two. " +
			"Longer over fewer songs buys more takes; you keep the best of each.");
		Body("Budget, Mid, and Top rooms raise production quality and hourly cost. Your town's studio quality also shifts the quote and the session read, so the same room can cost and sound a little different from city to city.");

		Body("Songs to cut:");
		List<PlayerDesk.MaterialChoice> options = desk.MaterialOptionsFor(artist).ToList();
		if (options.Count <= 1)
			Body("    They've nothing worked up yet — write one or teach a cover above first.");
		var checks = new List<(CheckBox Box, PlayerDesk.MaterialChoice Choice)>();
		foreach (PlayerDesk.MaterialChoice option in options) {
			var box = Check(option.Describe(), false);
			content.AddChild(box);
			checks.Add((box, option));
		}

		var roomRow = new HBoxContainer();
		roomRow.AddThemeConstantOverride("separation", 12);
		roomRow.AddChild(FormLabel("Room"));
		var tierPicker = Option();
		tierPicker.CustomMinimumSize = new Vector2(320, 36);
		foreach (PlayerDesk.StudioTier tier in Tiers)
			tierPicker.AddItem($"{PlayerDesk.StudioTierName(tier)} — ${desk.StudioHourlyRate(tier):N0}/hr");
		tierPicker.Selected = 1;
		roomRow.AddChild(tierPicker);
		roomRow.AddChild(FormLabel("Hours"));
		var hoursInput = Spin(PlayerDesk.MinSessionHours, PlayerDesk.MaxSessionHours, 1, 4);
		roomRow.AddChild(hoursInput);
		content.AddChild(roomRow);

		var cost = new Label { SizeFlagsHorizontal = SizeFlags.ExpandFill, AutowrapMode = TextServer.AutowrapMode.WordSmart };
		cost.AddThemeColorOverride("font_color", Rust);
		void UpdateCost() {
			PlayerDesk.StudioTier tier = Tiers[Mathf.Clamp(tierPicker.Selected, 0, Tiers.Length - 1)];
			int hours = (int)hoursInput.Value;
			float total = desk.SessionCost(tier, hours);
			int songCount = checks.Count(check => check.Box.ButtonPressed);
			float after = desk.Label.cashReserves - total;
			cost.Text = $"BOOKING SUMMARY  ·  {songCount} song(s)  ·  {PlayerDesk.StudioTierName(tier)}  ·  {hours}h  ·  ${total:N0}  ·  cash after: {Money(after)}{MonthEndCashPreview(desk.Label, after)}";
		}
		tierPicker.ItemSelected += _ => UpdateCost();
		hoursInput.ValueChanged += _ => UpdateCost();
		foreach (var item in checks) item.Box.Toggled += _ => UpdateCost();
		UpdateCost();
		if (PolarSongBehavior.UsePolarFitSelection && options.Count > 0) {
			Body("Pick two songs below to compare side by side. The read estimates audience demand, this act's delivery, and how the arrangement changes what listeners hear; uncertain reads are shown as a range. Use the checkboxes above to decide what to cut.");
			var comparePickers = new HBoxContainer();
			comparePickers.AddThemeConstantOverride("separation", 10);
			comparePickers.AddChild(FormLabel("SONG A"));
			var songA = Option(); songA.SizeFlagsHorizontal = SizeFlags.ExpandFill; songA.ClipText = true; songA.CustomMinimumSize = new Vector2(200, 36);
			comparePickers.AddChild(songA);
			comparePickers.AddChild(FormLabel("SONG B"));
			var songB = Option(); songB.SizeFlagsHorizontal = SizeFlags.ExpandFill; songB.ClipText = true; songB.CustomMinimumSize = new Vector2(200, 36);
			comparePickers.AddChild(songB);
			for (int i = 0; i < options.Count; i++) {
				string character = PolarPlayerPerception.DescribeCharacter(options[i]);
				string pickerText = character.Length == 0 ? options[i].Describe() : $"{options[i].Describe()}  —  {character}";
				songA.AddItem(pickerText);
				songB.AddItem(pickerText);
			}
			songA.Selected = 0;
			songB.Selected = options.Count > 1 ? 1 : 0;
			songB.Disabled = options.Count < 2;
			// The same song twice would only draw one chart, so each picker rules out whatever the other has chosen.
			void RuleOutDuplicate() {
				for (int i = 0; i < options.Count; i++) {
					songA.SetItemDisabled(i, options.Count > 1 && i == songB.Selected);
					songB.SetItemDisabled(i, options.Count > 1 && i == songA.Selected);
				}
			}
			RuleOutDuplicate();
			content.AddChild(comparePickers);
			var previewHost = new VBoxContainer(); content.AddChild(previewHost);
			void UpdatePreview() {
				foreach (Node child in previewHost.GetChildren()) { previewHost.RemoveChild(child); child.QueueFree(); }
				var cards = new HFlowContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
				cards.AddThemeConstantOverride("h_separation", 12);
				cards.AddThemeConstantOverride("v_separation", 10);
				foreach (int pickIndex in new[] { (int)songA.Selected, (int)songB.Selected }.Distinct()) {
					PlayerDesk.MaterialChoice option = options[Mathf.Clamp(pickIndex, 0, options.Count - 1)];
					string songId = option.SongId ?? option.Title;
					int slot = checks.Take(pickIndex).Count(c => c.Box.ButtonPressed);
					var gate = option.Kind == PlayerDesk.MaterialKind.LiveCover ? PolarEvidenceGate.Rehearsal : PolarEvidenceGate.Demo;
					Control card = ComparisonCard(artist, option, gate, "repertoire:" + artist.artistId + ":" + songId,
						desk.PreviewSessionContext(Tiers[tierPicker.Selected], artist), slot,
						selected => desk.PreviewSessionContext(Tiers[tierPicker.Selected], selected));
					card.CustomMinimumSize = new Vector2(460, 0);
					card.SizeFlagsHorizontal = SizeFlags.ExpandFill;
					cards.AddChild(card);
				}
				previewHost.AddChild(cards);
			}
			songA.ItemSelected += _ => { RuleOutDuplicate(); UpdatePreview(); };
			songB.ItemSelected += _ => { RuleOutDuplicate(); UpdatePreview(); };
			tierPicker.ItemSelected += _ => UpdatePreview();
			UpdatePreview();
		}

		var book = Primary("BOOK THE ROOM");
		book.CustomMinimumSize = new Vector2(300, 44);
		book.Pressed += () => {
			var chosen = checks.Where(c => c.Box.ButtonPressed).Select(c => c.Choice).ToList();
			PlayerDesk.StudioTier tier = Tiers[Mathf.Clamp(tierPicker.Selected, 0, Tiers.Length - 1)];
			var responses = PlayerDesk.Instance.MaterialRefusals(artist, chosen, tier);
			if (responses.Count > 0) { RefusalDialog(artist, chosen, tier, (int)hoursInput.Value, responses, checks); return; }
			int bookHours = (int)hoursInput.Value;
			void Book() {
				bool ok = PlayerDesk.Instance.StartSession(artist, chosen, tier, bookHours, out string message);
				Say(message, ok);
				Refresh();
			}
			float roomCost = desk.SessionCost(tier, bookHours);
			if (!WarnIfUnderOverhead($"{PlayerDesk.StudioTierName(tier)} for {bookHours}h", roomCost, $"BOOK IT — ${roomCost:N0}", Book)) Book();
		};
		content.AddChild(cost);
		content.AddChild(book);
	}

	private Control ComparisonCard(SimulatedArtist artist, PlayerDesk.MaterialChoice choice, PolarEvidenceGate gate,
		string eventId, PolarSessionContext session, int slot, Func<SimulatedArtist, PolarSessionContext> sessionForAct = null, string printedMasterId = null,
		PolarHearing hearing = null, bool onSheet = false) {
		var card = new PanelContainer();
		// On a clipboard the sheet is the modal's own paper; inline it is a sheet of its own.
		card.AddThemeStyleboxOverride("panel", onSheet ? new StyleBoxEmpty() : PaperStyleBox.Sheet(Paper, 12, 12, 8));
		var column = new VBoxContainer(); column.AddThemeConstantOverride("separation", 7); card.AddChild(column);
		Label Copy(string text, Color color) {
			var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart };
			label.AddThemeColorOverride("font_color", color); label.AddThemeFontSizeOverride("font_size", 16);
			column.AddChild(label); return label;
		}
		Copy($"“{choice.Title}”", Ink);
		string songCharacter = PolarPlayerPerception.DescribeCharacter(choice);
		if (songCharacter.Length > 0) Copy(songCharacter, Heard);
		var names = PlayerDesk.Instance.Roster.Prepend(artist).DistinctBy(a => a.artistId).ToList();
		var picker = Option(); foreach (var act in names) picker.AddItem(act.stageName);
		// A kept take belongs to the recorded act; studio previews can compare the same material across the roster.
		// Ear reads are about the song as heard, so there is no act to project it onto.
		picker.Disabled = gate == PolarEvidenceGate.Playback; picker.Visible = hearing?.IsEar != true; column.AddChild(picker);
		var evidence = Copy("", Heard);
		var explanation = Copy("", Ink); var resistance = Copy("", Rust);
		var graph = new PolarComparisonWidget(); column.AddChild(graph);
		void Update() {
			var selected = names[picker.Selected];
			var read = PolarPlayerPerception.Compare(choice, selected, selected.artistId == artist.artistId ? gate : PolarEvidenceGate.Demo, eventId,
				PlayerDesk.Instance.PreviewMasterId(slot), sessionForAct?.Invoke(selected) ?? session, printedMasterId, hearing);
			evidence.Text = read.subjectLabel + " · " + read.evidenceLabel;
			graph.SetRead(read); explanation.Text = (read.arrangement + " " + read.explanation).Trim();
			explanation.Visible = explanation.Text != "";
			resistance.Text = read.resistance; resistance.Visible = read.resistance != "";
		}
		picker.ItemSelected += _ => Update(); Update(); return card;
	}
	private static ScrollContainer ComparisonScroll() => new() {
		CustomMinimumSize = new Vector2(0, 540), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
	};

	private void RefusalDialog(SimulatedArtist artist, List<PlayerDesk.MaterialChoice> chosen, PlayerDesk.StudioTier tier,
		int hours, IReadOnlyList<string> responses, List<(CheckBox Box, PlayerDesk.MaterialChoice Choice)> checks) {
		var dialog = PaperModal.Open(this, "THE ACT PUSHES BACK", 780);
		var material = chosen.First(c => responses.Any(r => r.Contains("“" + c.Title + "”", StringComparison.Ordinal)));
		var read = PolarPlayerPerception.Compare(material, artist, PolarEvidenceGate.Rehearsal, "booking:" + artist.artistId + ":" + material.Title,
			PlayerDesk.Instance.PreviewMasterId(chosen.IndexOf(material)), PlayerDesk.Instance.PreviewSessionContext(tier, artist));
		foreach (string response in responses) dialog.AddText(response);
		dialog.AddText("Your staff's read: " + (read.mayResist ? read.resistance + " " : "") + read.explanation);
		dialog.AddText("The room has not been booked. How do you answer?");
		void Choose(string action) {
			foreach (var item in checks) item.Box.ButtonPressed = action == "own" && item.Choice.Kind == PlayerDesk.MaterialKind.Original;
			Say(action == "own" ? "Their own material is selected. Review it before booking." : "The material is set aside. Choose another song when you're ready.");
		}
		dialog.AddButton("SET THIS ASIDE", () => Choose("shelve"));
		var own = dialog.AddButton("USE THEIR OWN MATERIAL", () => Choose("own"));
		own.Disabled = !checks.Any(c => c.Choice.Kind == PlayerDesk.MaterialKind.Original);
		var insist = dialog.AddButton(artist.artistCreativeControl ? "THEIR CALL — CAN'T INSIST" : "INSIST — BOOK THE ROOM", () => {
			bool ok = PlayerDesk.Instance.StartSession(artist, chosen, tier, hours, out string message, overrideRefusal: true);
			Say(message, ok); Refresh();
		}, PaperModal.ButtonKind.Primary);
		// Creative control is the act's final word on material: the contract you signed takes the override away.
		insist.Disabled = artist.artistCreativeControl;
		if (artist.artistCreativeControl) dialog.AddText($"{artist.stageName} holds creative control under their contract, so you can't overrule this.");
	}

	/// <summary>The console: keep a take per song, then print. Selecting a take is free.</summary>
	private void TakesView(PlayerDesk.PendingSession session, SimulatedArtist artist) {
		Body($"ON THE CONSOLE — {PlayerDesk.StudioTierName(session.Tier)}, {session.Hours}h, ${session.Cost:N0} spent. " +
			"Keep the take you want for each song, then print the masters.");
		Body("✓ marks the take currently selected to print. Take selection is free; choose another if its vocal, band, or production read suits the record better.");

		for (int c = 0; c < session.Cuts.Count; c++) {
			PlayerDesk.SessionCut cut = session.Cuts[c];
			var title = new Label { Text = $"\"{cut.Choice.Title}\"  ({cut.Choice.Detail})" };
			title.AddThemeColorOverride("font_color", Ink);
			content.AddChild(title);

			var takesRow = new HBoxContainer();
			takesRow.AddThemeConstantOverride("separation", 8);
			for (int t = 0; t < cut.Takes.Count; t++) {
				PlayerDesk.SessionTake take = cut.Takes[t];
				bool kept = t == cut.KeptTake;
				// What each take actually is -- the hook the act got on tape and the sound of the room -- as bars with a
				// few words, so the choice is a trade (a livelier pass against a cleaner one) and not a star count.
				// The radar below is the read of how the take sits with the act.
				var takeCard = new VBoxContainer { CustomMinimumSize = new Vector2(220, 0) };
				takeCard.AddThemeConstantOverride("separation", 3);
				var btn = Btn($"Take {take.Number}{(kept ? "  ✓" : "")}");
				btn.CustomMinimumSize = new Vector2(220, 40);
				btn.ToggleMode = true;
				btn.ButtonPressed = kept;
				int cutIndex = c, takeIndex = t;
				btn.Pressed += () => { PlayerDesk.Instance.KeepTake(cutIndex, takeIndex); Refresh(); };
				takeCard.AddChild(btn);
				takeCard.AddChild(FaintLine("hook: " + PolarPlayerPerception.DescribeHook(take.Hook, 1f)));
				takeCard.AddChild(new ReadBar { CustomMinimumSize = new Vector2(220, 12) }.Set(take.Hook));
				takeCard.AddChild(FaintLine("sound: " + SoundWords(take.Production)));
				takeCard.AddChild(new ReadBar { CustomMinimumSize = new Vector2(220, 12) }.Set(take.Production));
				takesRow.AddChild(takeCard);
			}
			content.AddChild(takesRow);
			if (PolarSongBehavior.UsePolarFitSelection) {
				var take = cut.Takes[Mathf.Clamp(cut.KeptTake, 0, cut.Takes.Count - 1)];
				content.AddChild(ComparisonCard(artist, cut.Choice, PolarEvidenceGate.Playback,
					$"session:{session.Date}:{artist.artistId}:{c}:take:{take.Number}", new PolarSessionContext {
						producerCraft = PlayerDesk.Instance.Label.productionQuality, studioCraft = take.Production }, c));
			}
		}

		string keptSummary = SetSummary(session.Cuts.Select(cut => cut.Takes[Mathf.Clamp(cut.KeptTake, 0, cut.Takes.Count - 1)].Hook).ToList(), 1f);
		if (keptSummary.Length > 0) content.AddChild(FaintLine("THE SIDES AS KEPT: " + keptSummary));

		var buttons = new HBoxContainer();
		buttons.AddThemeConstantOverride("separation", 12);
		var print = Primary("PRINT MASTERS");
		print.CustomMinimumSize = new Vector2(240, 44);
		print.Pressed += () => {
			bool ok = PlayerDesk.Instance.PrintSession(out string message);
			Say(message, ok);
			// A finished master's next stop is its card on the Catalog board, where it is assembled into a 45.
			if (ok) GoToTab(CatalogTab, PageCatalog);
			else Refresh();
		};
		buttons.AddChild(print);
		var scrap = Btn("SCRAP");
		scrap.CustomMinimumSize = new Vector2(140, 44);
		scrap.Pressed += () => { PlayerDesk.Instance.ScrapSession(); Say("Session scrapped.", true); Refresh(); };
		buttons.AddChild(scrap);
		content.AddChild(buttons);
	}

	private void SongLine(string title, string tag, float hook) {
		content.AddChild(SongRow($"    ♪ {title}  ({tag})", hook, 0.8f));
	}

	/// <summary>A number the act has cut: shown with its status, and (once it's out) a link to the discography.</summary>
	private void RecordedLine(PlayerDesk desk, string title, string tag, string recordId, string artistId) {
		bool released = desk.IsRecordReleased(recordId);
		bool bSide = desk.IsRecordReleasedAsBSide(recordId);
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 12);
		var text = new Label {
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			Text = $"    ♪ {title}  ({tag}) — {(bSide ? "OUT — on a B-side" : released ? "RELEASED" : "cut, not out yet")}"
		};
		text.AddThemeFontSizeOverride("font_size", 15);
		text.AddThemeColorOverride("font_color", Ink);
		row.AddChild(text);
		if (released) {
			var view = Btn("VIEW ▸");
			view.CustomMinimumSize = new Vector2(120, 32);
			view.Pressed += () => UIManager.Instance?.OpenDiscography(artistId, true);
			row.AddChild(view);
		}
		content.AddChild(row);
	}

	/// <summary>A cover being worked up, or a commission out with a writer -- either way, not yet in the set.</summary>
	private void RehearsingLine(PlayerDesk.CoverRehearsal r) {
		var text = new Label {
			Text = r.IsCommission
				? $"    ♪ \"{r.Title}\"  (commissioned) — writer delivering {r.ReadyDate.ToHeadlineString()}"
				: $"    ♪ \"{r.Title}\"  ({r.SourceTag}) — rehearsing, ready {r.ReadyDate.ToHeadlineString()}"
		};
		text.AddThemeFontSizeOverride("font_size", 15);
		text.AddThemeColorOverride("font_color", Rust);
		content.AddChild(text);
	}

	// ========================================================================
	// CATALOG (the stage board: every record's next step is a button on its own row)
	// ========================================================================

	// Titles whose "order a pressing" form is open. An assembled single that has never been pressed always
	// shows it; a run already in the plant, or a released title (a repress), keeps it behind a button so a
	// second order never looks like a first one.
	private readonly HashSet<string> openPressForms = new();

	private void PageCatalog() {
		PlayerDesk desk = PlayerDesk.Instance;
		PipelineBoard(desk);
		MarketBoard(desk);
	}

	/// <summary>One card per record that is not out yet, nearest to the shops first. The card names the stage
	/// the record is in and carries that stage's controls, so nothing has to be looked up on another tab.</summary>
	private void PipelineBoard(PlayerDesk desk) {
		Heading("SINGLE PIPELINE", "A record's trip from tape to the shops: cut the sides, assemble them into a 45, " +
			"order the pressing, then date the release for after the vinyl lands. Each card carries the button for its next step.");
		GameDate today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		List<PlayerDesk.PlannedRelease> singles = desk.Planned
			.OrderByDescending(single => single.Dated).ThenByDescending(single => desk.PressingOrderFor(single.Master.Record.recordId) != null).ToList();
		List<PlayerDesk.Master> shelf = desk.Masters.Where(master => !master.Scheduled && !master.Released).ToList();
		if (singles.Count == 0 && shelf.Count == 0) {
			Body("Nothing cut and waiting. Cut a record from an act's MANAGE window.");
			return;
		}

		foreach (PlayerDesk.PlannedRelease single in singles) {
			string id = single.Master.Record.recordId;
			PlayerDesk.PressOrder order = desk.PressingOrderFor(id);
			bool pressed = (desk.StockFor(id)?.TotalPressed ?? 0) > 0;
			string ships = single.Dated ? $"ships {single.Date.ToHeadlineString()}" : null;
			string stage = order != null ? PipelinePressStage(order, today) + (ships != null ? $"  •  {ships}" : "")
				: ships != null ? $"DATED  •  {ships}"
				: pressed ? "ASSEMBLED  •  pressed, needs a release date"
				: "ASSEMBLED  •  press it, then date it";
			string title = $"\"{single.Master.SongTitle}\"{(single.BSide != null ? $" b/w \"{single.BSide.SongTitle}\"" : "")} — {single.Master.Record.artistName}";
			// RUSH: dated to ship within ten days and the vinyl is not in the office yet.
			bool rush = single.Dated && DaysBetween(today, single.Date) <= 10 && (order != null || !pressed);
			StageCard(title, stage, MasterDetail(single.Master), () => {
				if (!single.Dated && desk.EarliestReleaseDays(single) > 0) DateControls(desk, single);
				if (order == null && !pressed) PressingControls(desk, id, true);
				else if (order != null || pressed) PressingControls(desk, id, false);
				AcetateControls(desk, id);
				CompareControls(desk, single.Master);
			}, "Pressing order", rush ? "RUSH" : null);
		}

		foreach (PlayerDesk.Master master in shelf) {
			string id = master.Record.recordId;
			StageCard($"\"{master.SongTitle}\" — {master.Record.artistName}",
				"MASTER ON THE SHELF  •  " + (desk.AcetatesFor(id) > 0 ? "acetate ready, " : "") + "pair it with a flip side",
				MasterDetail(master), () => {
					AssembleControls(desk, master);
					AcetateControls(desk, id);
					CompareControls(desk, master);
				}, "Master receiving slip");
		}
	}

	/// <summary>Records already out: how they are doing, what is left in the office, and the repress button.</summary>
	private void MarketBoard(PlayerDesk desk) {
		Heading("IN THE MARKET");
		List<RecordRuntimeData> released = desk.ReleasedRecords.OrderByDescending(record => record.weeksSinceRelease).ToList();
		if (released.Count == 0) { Body("Nothing out yet."); return; }
		GameDate today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		foreach (RecordRuntimeData record in released) {
			string id = record.baseRecord.recordId;
			PlayerDesk.PressOrder order = desk.PressingOrderFor(id);
			PlayerDesk.PressStock stock = desk.StockFor(id);
			string stockLine = order != null ? $"REPRESS  •  {PipelinePressStage(order, today)}"
				: stock?.Remaining > 0 ? $"IN OFFICE  •  {stock.Remaining:N0} copies"
				: "No copies left in the office";
			StageCard($"\"{record.baseRecord.title}\" — {record.baseRecord.artistName}", stockLine,
				(record.peakPosition > 0 ? $"peak #{record.peakPosition}, {record.weeksOnChart} {CountWord(record.weeksOnChart, "week")} on the chart" : "has not charted") +
				$"  •  {record.weeksSinceRelease} {CountWord(record.weeksSinceRelease, "week")} out",
				() => PressingControls(desk, id, false), "Repress requisition", order != null ? "ON ORDER" : null, RubberStamp.Blue);
		}
	}

	private static string MasterDetail(PlayerDesk.Master master) =>
		(PolarSongBehavior.UsePolarFitSelection ? "" : $"hook: {PolarPlayerPerception.DescribeHook(master.Record.hookStrength, 1f)}   •   sound: {SoundWords(master.Record.productionQuality)}   •   ") +
		$"cost ${master.ProductionCost:N0}   •   cut {master.Cut.ToHeadlineString()}";

	/// <summary>A record's card on the board, set as the plant's work order for it: the plant's name over the form, the
	/// serial, the job typed into boxes, and a stamp if the job is urgent. The controls callback adds to the form, not the page.</summary>
	private void StageCard(string title, string stage, string detail, Action controls, string formType = "Pressing order",
			string stamp = null, Color? stampInk = null) {
		PlayerDesk desk = PlayerDesk.Instance;
		WorkOrder.Form form = WorkOrder.Begin(RolodexDirectory.PlantFirm(desk?.Label), PaperTheme.Lettering(LetteringStyle.HeavySlab), 19,
			formType, WorkOrder.Serial("job:" + title), stamp, stampInk);
		form.Body.AddChild(WorkOrder.Typed("JOB", title, 0f, true));
		if (stage != null) form.Body.AddChild(WorkOrder.Typed("STATUS", stage, 0f, true, Rust));
		VBoxContainer page = content;
		page.AddChild(form.Sheet);
		content = form.Body;
		formDepth++;
		try {
			if (!string.IsNullOrEmpty(detail)) form.Body.AddChild(FaintLine(detail));
			controls?.Invoke();
		} finally {
			formDepth--;
			content = page;
		}
	}

	/// <summary>MASTER stage: pair this cut with a flip side from the shelf.</summary>
	private void AssembleControls(PlayerDesk desk, PlayerDesk.Master master) {
		List<PlayerDesk.Master> flips = desk.Masters.Where(other => !other.Scheduled && !other.Released && other != master).ToList();
		if (flips.Count == 0) {
			content.AddChild(FaintLine("A 45 needs two cuts. Record another song for the flip side, then assemble."));
			return;
		}
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 10);
		row.AddChild(FormLabel("Flip side"));
		var flipPicker = Option();
		flipPicker.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		flipPicker.CustomMinimumSize = new Vector2(0, 36);
		foreach (PlayerDesk.Master flip in flips) flipPicker.AddItem($"\"{flip.SongTitle}\" — {flip.Record.artistName}");
		row.AddChild(flipPicker);
		content.AddChild(row);

		var assemble = Primary("ASSEMBLE THE SINGLE");
		assemble.CustomMinimumSize = new Vector2(300, 42);
		assemble.TooltipText = "The A-side is the plug side that chases the chart; the flip rides along on the same disc.";
		assemble.Pressed += () => Act(() => {
			bool ok = PlayerDesk.Instance.AssembleSingle(master, flips[Mathf.Clamp(flipPicker.Selected, 0, flips.Count - 1)], out string message);
			Say(message, ok);
			return true;
		});
		content.AddChild(assemble);
	}

	private void AcetateControls(PlayerDesk desk, string recordId) {
		var controls = new HBoxContainer();
		if (desk.AcetatesFor(recordId) == 0) {
			var cut = Btn("CUT AN ACETATE  ($20, 1h)");
			cut.TooltipText = "One test disc a local DJ can play now, long before the pressing lands.";
			cut.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.CutAcetate(recordId, out string message); Say(message, ok); return ok; });
			controls.AddChild(cut);
		} else {
			RadioStation[] localStations = desk.Rolodex.Select(card => ChartManager.Instance?.GetRadioStation(card.stationId))
				.Where(station => station != null && station.regionId == desk.CurrentCity?.parentRegionId)
				.Distinct().ToArray();
			if (localStations.Length > 0) {
				var stationPicker = Option();
				foreach (RadioStation station in localStations) stationPicker.AddItem(station.callsign);
				controls.AddChild(stationPicker);
				var deliver = Btn("HAND DELIVER  (1h)");
				deliver.Pressed += () => Act(() => {
					string stationId = localStations[Mathf.Clamp(stationPicker.Selected, 0, localStations.Length - 1)].stationId;
					bool ok = PlayerDesk.Instance.DeliverAcetateToStation(recordId, stationId, out string message);
					Say(message, ok);
					return ok;
				});
				controls.AddChild(deliver);
			}
		}
		if (controls.GetChildCount() > 0) content.AddChild(controls);
	}

	private void CompareControls(PlayerDesk desk, PlayerDesk.Master master) {
		var artist = ArtistManager.Instance.GetArtist(master.Record.artistId);
		if (!PolarSongBehavior.UsePolarFitSelection || artist == null || PolarSongMetadataService.Get(master.Record.masterId) == null) return;
		var compare = Btn("COMPARE PLAYBACK");
		content.AddChild(compare);
		compare.Pressed += () => {
			var dialog = PaperModal.OpenClipboard(this, "PRINTED MASTER — PLAYBACK", 1040);
			var choice = new PlayerDesk.MaterialChoice { Title = master.SongTitle, SongId = master.Record.songId };
			var scroll = ComparisonScroll(); dialog.Body.AddChild(scroll);
			scroll.AddChild(ComparisonCard(artist, choice, PolarEvidenceGate.Playback, "master:" + master.Record.masterId,
				new PolarSessionContext { producerCraft = desk.Label.productionQuality, studioCraft = master.Record.productionQuality }, 0, printedMasterId: master.Record.masterId,
				hearing: new PolarHearing { source = PolarHearingSource.Playback }, onSheet: true));
			dialog.AddButton("DONE", null, PaperModal.ButtonKind.Primary);
		};
	}

	/// <summary>PRESSING stage: the run form for one title. With <paramref name="alwaysOpen"/> the form is the
	/// card's main control; otherwise a button opens it, because a second order is a deliberate act.</summary>
	private void PressingControls(PlayerDesk desk, string recordId, bool alwaysOpen) {
		bool open = alwaysOpen || openPressForms.Contains(recordId);
		if (!alwaysOpen) {
			PlayerDesk.PressOrder inPlant = desk.PressingOrderFor(recordId);
			var toggle = Btn(open ? "HIDE THE ORDER FORM" : inPlant != null ? "ORDER ANOTHER PRESSING…" : "ORDER A REPRESS…");
			toggle.CustomMinimumSize = new Vector2(260, 36);
			toggle.Pressed += () => {
				if (!openPressForms.Remove(recordId)) openPressForms.Add(recordId);
				RefreshKeepingScroll();
			};
			content.AddChild(toggle);
			if (!open) return;
		}
		if (!desk.AtHome) { content.AddChild(FaintLine("You place a run from the office — drive home to order.")); return; }

		string title = desk.PressableSingles().FirstOrDefault(single => single.RecordId == recordId).Title ?? recordId;
		int initialMin = desk.MinimumPressRun(recordId);
		var runSizeRow = new HBoxContainer();
		runSizeRow.AddThemeConstantOverride("separation", 10);
		var qtyInput = Spin(initialMin, 100000, 1, initialMin);
		runSizeRow.AddChild(WorkOrder.Field("Qty", qtyInput));
		// A first run opens on roughly a quarter promo (capped); a repress opens at zero (SuggestedPromoCount).
		var promoInput = Spin(0, initialMin, 1, desk.SuggestedPromoCount(recordId, initialMin));
		runSizeRow.AddChild(WorkOrder.Field("Promo (of qty)", promoInput));
		// What the plant prints on every 45 order: the speed, and both sides of the one disc.
		runSizeRow.AddChild(WorkOrder.Typed("Speed", "45 rpm", 96f));
		runSizeRow.AddChild(WorkOrder.Typed("Sides", "A + B", 96f));
		content.AddChild(runSizeRow);

		var runCost = new Label { SizeFlagsHorizontal = SizeFlags.ExpandFill, AutowrapMode = TextServer.AutowrapMode.WordSmart };
		runCost.AddThemeColorOverride("font_color", Rust);
		content.AddChild(runCost);
		void UpdateRunCost() {
			PlayerDesk.PressOrder inPlant = desk.PressingOrderFor(recordId);
			bool repress = desk.HasBeenPressed(recordId);
			int qty = (int)qtyInput.Value;
			float cost = PlayerDesk.PressingCost(qty, repress);
			int promoCap = desk.MaxPromoCount(recordId, qty);
			promoInput.MaxValue = promoCap;
			if (promoInput.Value > promoCap) promoInput.Value = promoCap;
			int promo = (int)promoInput.Value;
			// Promo copies come out of the run, they are not extra discs: spell out the split so nobody reads it as a second bill.
			runCost.Text =
				(inPlant != null ? $"ALREADY ON ORDER: {inPlant.Quantity:N0} due {inPlant.Arrives.ToHeadlineString()}. This would be a second run.  ·  " : "") +
				$"Run cost: ${cost:N0}  (${cost / Math.Max(1.0, qty):F2}/disc){(repress ? " — repress, no lacquer fee" : "")}" +
				(promo > 0 ? $"   ·   {qty - promo:N0} sellable + {promo:N0} promo, out of this {qty:N0}-unit run" : "") +
				(repress ? "" : $"   ·   promo capped at {promoCap:N0} ({PlayerDesk.PressPromoCapFraction:P0})") +
				$"   ·   cash after order: {Money(desk.Label.cashReserves - cost)}{MonthEndCashPreview(desk.Label, desk.Label.cashReserves - cost)}";
		}
		qtyInput.ValueChanged += _ => UpdateRunCost();
		promoInput.ValueChanged += _ => UpdateRunCost();
		UpdateRunCost();

		void SubmitPress(bool confirmAdditionalRun, int runQuantity, int runPromo) => Act(() => {
			bool ordered = PlayerDesk.Instance.OrderPressing(recordId, runQuantity, runPromo, out string message, confirmAdditionalRun);
			Say(message, ordered);
			if (ordered) openPressForms.Remove(recordId);
			return true;
		});
		var order = Primary(desk.PressingOrderFor(recordId) != null ? "ORDER ANOTHER PRESSING…" : "ORDER PRESSING");
		order.CustomMinimumSize = new Vector2(240, 42);
		order.TooltipText = $"A first pressing needs {PlayerDesk.PressMinimumOrder}. About {PlayerDesk.PressVinylPerUnit + PlayerDesk.PressSleeveLabelPerUnit:F2}/disc " +
			$"plus ${PlayerDesk.PressLacquerSetup:N0} lacquer setup (once per title) and ${PlayerDesk.PressShipping:N0} for sleeves, labels and freight. " +
			$"Once a title's stampers are cut, a repress can run as low as {PlayerDesk.PressReorderMinimum} with no lacquer fee. " +
			"A run is a whole 45, both sides on the one disc, and the plant takes weeks to turn it round.";
		order.Pressed += () => {
			PlayerDesk.PressOrder pending = desk.PressingOrderFor(recordId);
			int runQty = (int)qtyInput.Value, runPromo = (int)promoInput.Value;
			float runPrice = PlayerDesk.PressingCost(runQty, desk.HasBeenPressed(recordId));
			if (pending == null) {
				if (!WarnIfUnderOverhead($"A run of {runQty:N0}", runPrice, $"PRESS IT — ${runPrice:N0}", () => SubmitPress(false, runQty, runPromo)))
					SubmitPress(false, runQty, runPromo);
				return;
			}
			var confirm = PaperModal.Open(this, "ORDER ANOTHER PRESSING?", 600);
			confirm.AddText($"A run of \"{title}\" is already due {pending.Arrives.ToHeadlineString()}.");
			bool canAfford = desk.Label.cashReserves >= runPrice;
			confirm.AddText(canAfford
				? $"Order another {runQty:N0} now for ${runPrice:N0}? You'd have {Money(desk.Label.cashReserves - runPrice)} left."
				: $"Another {runQty:N0} costs ${runPrice:N0}, and you're {Money(runPrice - desk.Label.cashReserves)} short.");
			confirm.AddButton("NOT NOW", null);
			confirm.AddButton($"ORDER ANOTHER ${runPrice:N0}", () => SubmitPress(true, runQty, runPromo), PaperModal.ButtonKind.Primary).Disabled = !canAfford;
		};
		content.AddChild(order);

		int fillDemand = desk.OpenCallDemand(recordId);
		if (fillDemand > 0) {
			var fill = Btn($"PRESS TO FILL OPEN CALLS  (~{desk.PressToFillQuantity(recordId):N0}, {fillDemand:N0} asked for)");
			fill.CustomMinimumSize = new Vector2(300, 42);
			fill.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.PressToFill(recordId, out string message); Say(message, ok); return true; });
			content.AddChild(fill);
		}

		var creditOwed = desk.PlantCreditOwed;
		if (creditOwed.HasValue && creditOwed.Value.RecordId == recordId) {
			var (_, amount, weeksAway) = creditOwed.Value;
			content.AddChild(FaintLine($"Plant credit outstanding: ${amount:N0} due on \"{title}\" in {weeksAway} week(s) — it collects on schedule whether or not the record's still moving."));
		} else if (!creditOwed.HasValue && desk.PlantCreditEligible(recordId)) {
			float creditCost = PlayerDesk.PressingCost(PlayerDesk.PlantCreditQuantity, desk.HasBeenPressed(recordId));
			var creditBtn = Btn($"TAKE THE CREDIT RUN  ({PlayerDesk.PlantCreditHours}h)");
			creditBtn.CustomMinimumSize = new Vector2(260, 42);
			creditBtn.TooltipText = $"\"{title}\" is moving enough that the plant would front a run: {PlayerDesk.PlantCreditQuantity:N0} units, nothing down, " +
				$"${creditCost:N0} due in {PlayerDesk.PlantCreditTermWeeks} weeks.";
			creditBtn.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.RequestPlantCredit(recordId, out string message); Say(message, ok); return true; });
			content.AddChild(creditBtn);
		}
		if (desk.CanReverseTheSides(recordId)) {
			int reverseQty = desk.MinimumPressRun(recordId);
			var reverseBtn = Btn("REVERSE THE SIDES");
			reverseBtn.CustomMinimumSize = new Vector2(240, 42);
			reverseBtn.TooltipText = $"\"{title}\" has a flip that hasn't broken yet. Reverse the sides with a {reverseQty:N0}-unit all-promo repress " +
				$"(${PlayerDesk.PressingCost(reverseQty, true):N0}); stations already carrying it will be re-serviced.";
			reverseBtn.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.ReverseTheSidesManually(recordId, out string message); Say(message, ok); return true; });
			content.AddChild(reverseBtn);
		}
	}

	/// <summary>SHIP DATE stage: date an assembled single for after its vinyl lands.</summary>
	private void DateControls(PlayerDesk desk, PlayerDesk.PlannedRelease single) {
		var daysRow = new HBoxContainer();
		daysRow.AddThemeConstantOverride("separation", 10);
		var daysInput = Spin(desk.EarliestReleaseDays(single), 120, 1, desk.SuggestedReleaseDays(single));
		daysRow.AddChild(WorkOrder.Field("Ship in (days)", daysInput));
		var campaignLabel = new Label();
		campaignLabel.MouseFilter = Control.MouseFilterEnum.Stop;
		// The awareness a player record earns is supposed to come off the verbs on this branch (serviced jocks,
		// the mailing, the review desk, the road), not off a slider, so the field opens at $0.
		campaignLabel.TooltipText = "The campaign is shipping samples and a trade announcement, charged the day it ships. It is not a way to buy a hit: " +
			"an $800 label leaves it at zero and earns its awareness on the road, with promo copies in jocks' hands, the mailing and the review desk.";
		var budgetInput = Spin(0, 50000, 1, 0);
		Control campaignField = WorkOrder.Field("Campaign ($)", budgetInput);
		campaignField.MouseFilter = Control.MouseFilterEnum.Stop;
		campaignField.TooltipText = campaignLabel.TooltipText;
		daysRow.AddChild(campaignField);
		content.AddChild(daysRow);

		var datePreview = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
		datePreview.AddThemeColorOverride("font_color", Rust);
		content.AddChild(datePreview);
		void UpdateDatePreview() {
			GameDate shipDate = (TimeManager.Instance?.CurrentDate ?? GameDate.StartDate).AddDays((int)daysInput.Value);
			PlayerDesk.PressOrder inPlant = desk.PressingOrderFor(single.Master.Record.recordId);
			string vinyl = inPlant != null ? $"The vinyl lands {inPlant.Arrives.ToHeadlineString()}; it can't ship before. " : "";
			datePreview.Text = $"{vinyl}Setting the date takes {PlayerDesk.ScheduleHours}h. It ships {shipDate.ToHeadlineString()}; " +
				$"campaign ${budgetInput.Value:N0} is charged on release.";
		}
		daysInput.ValueChanged += _ => UpdateDatePreview();
		budgetInput.ValueChanged += _ => UpdateDatePreview();
		UpdateDatePreview();

		var setDate = Primary($"SET THE DATE  ({PlayerDesk.ScheduleHours}h)");
		setDate.CustomMinimumSize = new Vector2(300, 44);
		setDate.Pressed += () => Act(() => {
			bool ok = PlayerDesk.Instance.SetReleaseDate(single, (int)daysInput.Value, (float)budgetInput.Value, out string message);
			Say(message, ok);
			return true;
		});
		content.AddChild(setDate);
	}

	/// <summary>The press stage of a title in the plant, as a strip: MASTER ✓ → MAILED ✓ → [PLATING] → ...</summary>
	internal static string PipelinePressStage(PlayerDesk.PressOrder order, GameDate today) {
		int stage = today < order.Mailed ? 1 : today < order.PlatingComplete ? 2 : today < order.QueueComplete ? 3 : today < order.Arrives ? 4 : 5;
		string[] labels = { "MASTER", "MAILED", "PLATING", "PLANT QUEUE", "SHIPPED", "IN OFFICE" };
		string strip = string.Join(" → ", labels.Select((label, index) => index < stage ? $"{label} ✓" : index == stage ? $"[{label}]" : label));
		int eta = Mathf.Max(0, (new DateTime(order.Arrives.year, order.Arrives.month, order.Arrives.day) - new DateTime(today.year, today.month, today.day)).Days);
		return $"{strip}  •  ETA {eta}d ({order.Arrives.ToHeadlineString()})";
	}

	/// <summary>Which step of the plant's route a run is on right now, and the day that step ends.</summary>
	internal static (string Name, GameDate Ends) PipelinePressStep(PlayerDesk.PressOrder order, GameDate today) =>
		today < order.Mailed ? ("Mailed", order.Mailed)
		: today < order.PlatingComplete ? ("Plating", order.PlatingComplete)
		: today < order.QueueComplete ? ("Plant queue", order.QueueComplete)
		: today < order.Arrives ? ("Shipped", order.Arrives)
		: ("In the office", order.Arrives);

	// ========================================================================
	// DISTRIBUTION
	// ========================================================================

	private void DistributionBody() {
		PlayerDesk desk = PlayerDesk.Instance;
		AILabel label = desk.Label;
		Heading("DISTRIBUTION", "The places your records go: the towns you drive to, the shops and operators in them, " +
			"the stations and trades you work by mail, and the houses and distributors that carry you where you can't. " +
			"Assembling, pressing and dating a record happen on its row in the CATALOG.");

		// Sections whose precondition isn't met stay out of the page entirely; one line says what unlocks them.
		bool hasReleased = desk.ReleasedRecords.Any(record => record?.baseRecord != null);
		if (!hasReleased)
			Body("More of this desk — the mailing, the trades, distribution deals and the wholesale houses — opens once your first single is out.");

		// --- WHERE YOU ARE + DRIVING ---
		MarketCity here = desk.CurrentCity;
		string hereName = here?.name ?? "the office";
		SectionHeading("travel", desk.AtHome ? "AT THE OFFICE" : $"ON THE ROAD — {hereName.ToUpperInvariant()}", !desk.AtHome || desk.PressedSinglesOnHand().Any(),
			desk.AtHome
				? "Work your own home town out of the trunk, then set out to work the towns you can reach. Trunk sales in the town " +
				  "you're standing in are cash in hand; a town you've left holds your cut until you drive back to collect it."
				: $"You're in {hereName}. Work this town out of the trunk, drive on to a neighbouring town, or head home. " +
				  "Working or driving into a town collects whatever its shops have been holding for you. The office and " +
				  "studio are out of reach until you're back — and every night away is a motel bill.");

		List<MarketCity> reach = desk.ReachableCities()
			.OrderBy(c => ChartManager.Instance?.GetRegionById(c.parentRegionId)?.regionName ?? c.parentRegionId)
			.ThenBy(c => c.name)
			.ToList();
		if (reach.Count > 0) {
			var driveRow = new HBoxContainer();
			driveRow.AddThemeConstantOverride("separation", 10);
			var cityPicker = Option();
			cityPicker.CustomMinimumSize = new Vector2(500, 36);
			cityPicker.AddItem("Choose a town to drive to…");
			foreach (MarketCity c in reach) {
				(int h, float g) = desk.DriveQuote(desk.CurrentCityId, c.cityId);
				string region = ChartManager.Instance?.GetRegionById(c.parentRegionId)?.regionName ?? c.parentRegionId;
				cityPicker.AddItem($"{c.name} — {region}  ({h}h, ${g:N0} gas)");
			}
			driveRow.AddChild(cityPicker);
			var drive = Btn("DRIVE THERE");
			drive.CustomMinimumSize = new Vector2(160, 36);
			drive.Disabled = true;
			cityPicker.ItemSelected += index => drive.Disabled = index <= 0;
			drive.Pressed += () => Act(() => {
				if (cityPicker.Selected <= 0) return false;
				bool ok = PlayerDesk.Instance.DriveTo(reach[Mathf.Clamp(cityPicker.Selected - 1, 0, reach.Count - 1)].cityId, out string message);
				Say(message, ok);
				return true;
			});
			driveRow.AddChild(drive);
			content.AddChild(driveRow);
		}

		// --- STOPS IN THIS TOWN: named shops and jukebox operators, each with its own relationship,
		// stock, and terms -- pitch (COD, refusal is real), consign (worse cash, the low-risk fallback),
		// or service (restock + collect, once a stop already has history) ---
		List<(string RecordId, string Title, int OnHand)> onHand = desk.PressedSinglesOnHand().ToList();
		List<(string RecordId, string Title, int PromoOnHand)> promoOnHand = desk.PromoSinglesOnHand().ToList();
		List<PlayerDesk.PlayerStop> stopsHere = desk.StopsInCity(here.cityId).ToList();
		// Flags which stops have a call waiting (directive §4) so the player doesn't have to cross-check
		// the OFFICE phone list by hand while deciding who to work first.
		var stopsWithCalls = new HashSet<string>(desk.PendingCalls().Select(c => c.Call.StopId), StringComparer.Ordinal);
		// Shops and operators matter once there is stock to place, or a stop here already holds copies, owes money or has called.
		bool showStops = onHand.Count > 0 || promoOnHand.Count > 0 ||
			stopsHere.Any(stop => stop.OnHand.Values.Sum(lot => lot.Remaining) > 0 || stop.OpenBalance > 0.5f || stopsWithCalls.Contains(stop.StopId));
		if (showStops)
			SectionHeading("stops", $"STOPS IN {here.name.ToUpper()}", onHand.Count > 0 || promoOnHand.Count > 0,
				"PITCH places copies on COD terms: as customers buy them, you get paid in cash while you're in that town; " +
				"when you're away, the account holds your cut until you return (with a small daily wire). CONSIGN is easier " +
				"shelf space, but every sale goes onto the store's balance until you collect it. Account rows show shelf stock " +
				"and money owed; SERVICE collects and restocks. The pressing cost was paid up front.");
		if (!showStops) { }
		else if (onHand.Count == 0 && promoOnHand.Count == 0)
			Body(desk.AtHome
				? "Nothing pressed on hand to sell or service right now — order a run from the CATALOG."
				: "Nothing pressed on hand to leave here — you carry stock out from the office.");
		else if (stopsHere.Count == 0)
			Body("No named accounts in this town yet.");
		else {
			var pickRow = new HBoxContainer();
			pickRow.AddThemeConstantOverride("separation", 10);
			pickRow.AddChild(FormLabel("Single"));
			var singlePick = Option();
			singlePick.CustomMinimumSize = new Vector2(320, 36);
			foreach ((string _, string title, int inHand) in onHand)
				singlePick.AddItem($"\"{title}\" — {inHand:N0} on hand");
			pickRow.AddChild(singlePick);
			content.AddChild(pickRow);

			// Directive §4: a separate picker for promo stock -- the two pools never convert into each
			// other, so servicing a station draws from a different list than pitching a shop.
			var promoPickRow = new HBoxContainer();
			promoPickRow.AddThemeConstantOverride("separation", 10);
			promoPickRow.AddChild(FormLabel("Promo copy"));
			var promoPick = Option();
			promoPick.CustomMinimumSize = new Vector2(320, 36);
			foreach ((string _, string title, int inHand) in promoOnHand)
				promoPick.AddItem($"\"{title}\" — {inHand:N0} promo on hand");
			promoPickRow.AddChild(promoPick);
			if (promoOnHand.Count == 0) promoPickRow.AddChild(FormLabel("(none pressed)"));
			content.AddChild(promoPickRow);

			// One click puts every shop and operator in this town on the runner's route (or takes them all off).
			if (desk.HasRunner) {
				List<PlayerDesk.PlayerStop> runnable = stopsHere.Where(s => PlayerDesk.RunnerCanWork(s.Kind)).ToList();
				if (runnable.Count > 0) {
					int already = runnable.Count(s => desk.IsOnRunnerRoute(s.StopId));
					bool allOn = already == runnable.Count;
					var routeAll = Btn(allOn ? $"TAKE ALL {runnable.Count} ACCOUNTS HERE OFF HIS ROUTE"
						: $"SEND RUNNER TO ALL {runnable.Count} ACCOUNTS HERE" + (already > 0 ? $"  ({already} already)" : ""));
					routeAll.CustomMinimumSize = new Vector2(360, 36);
					routeAll.TooltipText = "He works shops and jukebox operators in towns you've opened, once a chart week, at no hours of yours.";
					routeAll.Pressed += () => Act(() => {
						bool ok = PlayerDesk.Instance.SetRunnerRoute(runnable.Select(s => s.StopId), !allOn, out string message);
						Say(message, ok);
						return ok;
					});
					content.AddChild(routeAll);
				}
			}

			// Grouped into an expandable list per account kind ("Record Stores", "Jukebox Operators", ...
			// whatever kinds exist) rather than one flat roster -- a hub town runs a dozen-plus accounts
			// and a single mixed list stopped being legible.
			foreach (var kindGroup in stopsHere.GroupBy(s => s.Kind).OrderBy(g => g.Key)) {
				PlayerDesk.StopKind kind = kindGroup.Key;
				List<PlayerDesk.PlayerStop> kindStops = kindGroup.ToList();
				bool expanded = expandedStopKinds.Contains(kind);

				var header = Btn($"{(expanded ? "▾" : "▸")}  {StopKindLabel(kind).ToUpperInvariant()}  ({kindStops.Count})");
				header.CustomMinimumSize = new Vector2(360, 36);
				header.Pressed += () => {
					if (!expandedStopKinds.Remove(kind)) expandedStopKinds.Add(kind);
					Refresh();
				};
				content.AddChild(header);
				if (!expanded) continue;

				string estHours = PlayerDesk.StopVisitEstimate(kind);
				foreach (PlayerDesk.PlayerStop stop in kindStops) {
					if (kind == PlayerDesk.StopKind.OneStop) {
						content.AddChild(BuildOneStopRow(stop, onHand, singlePick, stopsWithCalls));
						continue;
					}
					if (kind == PlayerDesk.StopKind.Venue) {
						content.AddChild(BuildVenueRow(stop, onHand, singlePick));
						continue;
					}
					if (kind == PlayerDesk.StopKind.Station) {
						content.AddChild(BuildStationRow(stop, promoOnHand, promoPick));
						continue;
					}
					int stockHere = stop.OnHand.Values.Sum(lot => lot.Remaining);
					bool workedToday = desk.HasWorkedStopToday(stop.StopId);
					string relWord = stop.LastVisitWeek == 0 && stop.Relationship <= 0f ? "cold"
						: stop.Relationship < 0.35f ? "acquainted"
						: stop.Relationship < 0.7f ? "friendly"
						: "standing account";

					var row = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
					row.AddThemeConstantOverride("separation", 10);
					var actions = new HFlowContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
					actions.AddThemeConstantOverride("h_separation", 8);
					actions.AddThemeConstantOverride("v_separation", 6);
					var stopLabel = new Label {
						Text = (stopsWithCalls.Contains(stop.StopId) ? "    ☎ " : "    ") + $"{stop.DisplayName} ({relWord})"
							+ (stockHere > 0 ? $" — {stockHere:N0} on shelf" : "")
							+ (stop.OpenBalance > 0.5f ? $" — ${stop.OpenBalance:N0} owed" : "")
							+ (stopsWithCalls.Contains(stop.StopId) ? " — they called" : "")
							+ (desk.PreOrderNote(stop.StopId) is string held ? $": {held}" : "")
							// Directive §7.1: the one or two identified dealers a city's survey/trade
							// numbers actually come from -- flagged so the player can tell them apart, but
							// only once he's EARNED that (worked the counter, or asked at the station whose
							// survey it feeds). "That is the information the early game is actually about,"
							// so it is not printed free on a shop nobody has walked into.
							+ (desk.KnowsWhoReports(stop.StopId) ? " — reports" : ""),
						SizeFlagsHorizontal = SizeFlags.ExpandFill,
						AutowrapMode = TextServer.AutowrapMode.WordSmart
					};
					stopLabel.AddThemeColorOverride("font_color", Ink);
					row.AddChild(stopLabel);
					if (workedToday) {
						var tomorrow = new Label { Text = "Worked today — available tomorrow.", AutowrapMode = TextServer.AutowrapMode.WordSmart };
						tomorrow.AddThemeColorOverride("font_color", Heard);
						row.AddChild(tomorrow);
					}
					row.AddChild(actions);

					var pitch = Btn($"PITCH ({estHours})");
					pitch.Disabled = workedToday;
					pitch.TooltipText = "Try for a COD placement. The store can pass; sell-through pays cash if you are in town, otherwise it is held until collection.";
					pitch.CustomMinimumSize = new Vector2(110, 32);
					pitch.Pressed += () => Act(() => {
						(string recordId, _, _) = onHand[Mathf.Clamp(singlePick.Selected, 0, onHand.Count - 1)];
						bool ok = PlayerDesk.Instance.PitchAtStop(stop.StopId, recordId, out string message);
						Say(message, ok);
						return true;
					});
					actions.AddChild(pitch);

					var consign = Btn($"CONSIGN ({estHours})");
					consign.Disabled = workedToday;
					consign.TooltipText = "The store accepts a smaller/easier consignment placement. All proceeds are held on its balance until collected.";
					consign.CustomMinimumSize = new Vector2(130, 32);
					consign.Pressed += () => Act(() => {
						(string recordId, _, _) = onHand[Mathf.Clamp(singlePick.Selected, 0, onHand.Count - 1)];
						bool ok = PlayerDesk.Instance.ConsignAtStop(stop.StopId, recordId, out string message);
						Say(message, ok);
						return true;
					});
					actions.AddChild(consign);

					var service = Btn($"SERVICE ({estHours})");
					service.CustomMinimumSize = new Vector2(130, 32);
					service.Disabled = stop.OnHand.Count == 0 || workedToday;
					service.TooltipText = "Collect this town's outstanding balances and restock this account from the selected single.";
					service.Pressed += () => Act(() => {
						(string recordId, _, _) = onHand[Mathf.Clamp(singlePick.Selected, 0, onHand.Count - 1)];
						bool ok = PlayerDesk.Instance.ServiceStop(stop.StopId, recordId, out string message);
						Say(message, ok);
						return true;
					});
					actions.AddChild(service);

					// Directive §7.2: the honest report verb -- only offered at a dealer the player has
					// worked out keeps a report (§7.1), and only ever succeeds if he's actually holding and
					// moving the record. The verb itself stays ungated; this is what's on the SCREEN.
					if (stop.ReportsToTrades && desk.KnowsWhoReports(stop.StopId)) {
						var askReport = Btn($"ASK FOR THE REPORT (~{PlayerDesk.AskForReportMinutes}m)");
						askReport.CustomMinimumSize = new Vector2(200, 32);
						askReport.Pressed += () => Act(() => {
							(string recordId, _, _) = onHand[Mathf.Clamp(singlePick.Selected, 0, onHand.Count - 1)];
							bool ok = PlayerDesk.Instance.AskForTheReport(stop.StopId, recordId, out string message);
							Say(message, ok);
							return true;
						});
						actions.AddChild(askReport);
					}

					if (kind == PlayerDesk.StopKind.Shop) {
						// Directive §9: window card -- a bounded, stackable print buy. Needs the record
						// already placed here (BuyWindowCard checks it) -- the second thing you do, not the first.
						var windowCard = Btn($"WINDOW CARD ({PlayerDesk.WindowCardMinutes / 60}h, $8–20)");
						windowCard.TooltipText = "One hour. Costs $8–20 for the print run; any promo copies are listed in the result. The exact quote is rolled when you place it.";
						windowCard.CustomMinimumSize = new Vector2(150, 32);
						windowCard.Pressed += () => Act(() => {
							(string recordId, _, _) = onHand[Mathf.Clamp(singlePick.Selected, 0, onHand.Count - 1)];
							bool ok = PlayerDesk.Instance.BuyWindowCard(stop.StopId, recordId, out string message);
							Say(message, ok);
							return true;
						});
						actions.AddChild(windowCard);

						// Directive §9: in-store appearance -- needs an act with real local standing, so
						// it's the second thing you do here too.
						var inStore = Btn($"IN-STORE ({PlayerDesk.InStoreAppearanceHours}h)");
						inStore.CustomMinimumSize = new Vector2(140, 32);
						inStore.Pressed += () => Act(() => {
							(string recordId, _, _) = onHand[Mathf.Clamp(singlePick.Selected, 0, onHand.Count - 1)];
							bool ok = PlayerDesk.Instance.BookInStoreAppearance(stop.StopId, recordId, out string message);
							Say(message, ok);
							return true;
						});
						actions.AddChild(inStore);

						// Directive §7.3: the dishonest verb -- Fixer-gated, only at a reporting dealer,
						// never once he's burned. A small quantity spinner rather than a fixed count --
						// "12 copies" was the historical tell, "1" barely moves a survey.
						if ((stop.ReportsToTrades || stop.ReportsToStationIds.Count > 0) && desk.KnowsWhoReports(stop.StopId) && !stop.HypeBurned
								&& desk.InstinctProfile.TheFixer >= PlayerDesk.HypeTheCountMinFixer) {
							var hypeCount = Spin(1, 25, 1, 5);
							hypeCount.CustomMinimumSize = new Vector2(60, 32);
							actions.AddChild(hypeCount);
							var hype = Btn($"HYPE THE COUNT (~{PlayerDesk.HypeTheCountMinutes}m)");
							hype.CustomMinimumSize = new Vector2(180, 32);
							hype.Pressed += () => Act(() => {
								(string recordId, _, _) = onHand[Mathf.Clamp(singlePick.Selected, 0, onHand.Count - 1)];
								bool ok = PlayerDesk.Instance.HypeTheCount(stop.StopId, recordId, (int)hypeCount.Value, out string message);
								Say(message, ok);
								return true;
							});
							actions.AddChild(hype);
						}
					}

					// Directive §7: a hired runner's route is built one stop at a time, right where you'd
					// otherwise work the account yourself -- toggle it on or off here.
					if (desk.HasRunner) {
						bool onRoute = desk.IsOnRunnerRoute(stop.StopId);
						var routeBtn = Btn(onRoute ? "✓ RUNNER" : "SEND RUNNER");
						routeBtn.CustomMinimumSize = new Vector2(120, 32);
						routeBtn.Pressed += () => Act(() => {
							bool ok = PlayerDesk.Instance.AssignRunnerStop(stop.StopId, !onRoute, out string message);
							Say(message, ok);
							return true;
						});
						actions.AddChild(routeBtn);
					}

					// Every verb above draws from the sellable "Single" picker; with only promos left it is empty.
					if (onHand.Count == 0)
						foreach (Node child in actions.GetChildren())
							if (child is Button verb && !verb.Text.Contains("RUNNER", StringComparison.Ordinal)) {
								verb.Disabled = true;
								verb.TooltipText = "No sellable copies on hand — order another pressing first.";
							}

					content.AddChild(row);
				}
			}
		}

		if (!desk.AtHome) {
			var home = Btn("DRIVE HOME");
			home.CustomMinimumSize = new Vector2(160, 40);
			home.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.DriveHome(out string message); Say(message, ok); return ok; });
			content.AddChild(home);
		}

		// --- THE MAILING (office only, directive §5): the only way to touch a market you can't drive to ---
		if (promoOnHand.Count > 0) {
			SectionHeading("mailing", "THE MAILING", false,
				$"Office only. {ActionCosts.Planning}h for up to {PlayerDesk.MailingFreePieces} pieces, plus an hour per further " +
				$"{PlayerDesk.MailingPiecesPerExtraHour}. About ${PlayerDesk.MailerCostPerCopy:F2}/copy for the mailer and postage. " +
				"The only way to touch a market you can't drive to. Most of it lands in the bin — what lands only just gets him listening.");
			if (!desk.AtHome) {
				Body("You mail from the office — drive home to work the list.");
			} else if (promoOnHand.Count == 0) {
				Body("No promo copies on hand to mail — press some, or strike a repress all-promo.");
			} else {
				List<MarketRegion> regions = (ChartManager.Instance?.GetAllRegions() ?? Enumerable.Empty<MarketRegion>())
					.OrderBy(r => r.regionName).ToList();
				if (regions.Count == 0) Body("No market data.");
				else {
					var mailRow = new HBoxContainer();
					mailRow.AddThemeConstantOverride("separation", 10);
					var mailSinglePick = Option();
					mailSinglePick.CustomMinimumSize = new Vector2(320, 36);
					foreach ((string _, string title, int inHand) in promoOnHand)
						mailSinglePick.AddItem($"\"{title}\" — {inHand:N0} promo on hand");
					mailRow.AddChild(mailSinglePick);

					var regionPick = Option();
					regionPick.CustomMinimumSize = new Vector2(240, 36);
					foreach (MarketRegion r in regions) regionPick.AddItem(r.regionName);
					mailRow.AddChild(regionPick);

					mailRow.AddChild(FormLabel("Copies"));
					var mailCount = Spin(1, 500, 1, 25);
					mailRow.AddChild(mailCount);
					content.AddChild(mailRow);

					var mailBtn = Btn("MAIL THE LIST");
					mailBtn.CustomMinimumSize = new Vector2(200, 42);
					mailBtn.Pressed += () => Act(() => {
						if (promoOnHand.Count == 0) return false;
						string recordId = promoOnHand[Mathf.Clamp(mailSinglePick.Selected, 0, promoOnHand.Count - 1)].RecordId;
						string regionId = regions[Mathf.Clamp(regionPick.Selected, 0, regions.Count - 1)].regionId;
						bool ok = PlayerDesk.Instance.MailPromoCopies(recordId, regionId, (int)mailCount.Value, out string message);
						Say(message, ok);
						return true;
					});
					content.AddChild(mailBtn);
				}
			}
		}

		// --- THE TRADES (office only, directive §6.1/§6.3): the review desk and the breakout column ---
		if (hasReleased) {
			SectionHeading("trades", "THE TRADES", false,
				$"One submission per record, {ActionCosts.Paperwork}h and one promo copy plus ${PlayerDesk.TradeReviewPostage:F2} postage. " +
				"A week or two later you hear back — most records got nothing. A real pick talks to distributors " +
				"and one-stops, not to the public.");
			if (!desk.AtHome) {
				Body("You work the trades from the office.");
			} else {
				List<RecordRuntimeData> tradeReleased = desk.ReleasedRecords.Where(r => r?.baseRecord != null).ToList();
				if (tradeReleased.Count == 0) Body("Nothing released yet to submit.");
				else {
					foreach (RecordRuntimeData rec in tradeReleased) {
						string recordId = rec.baseRecord.recordId;
						string title = rec.baseRecord.title;
						var row = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
						row.AddThemeConstantOverride("separation", 4);
						var titleLabel = new Label { Text = $"    \"{title}\"", SizeFlagsHorizontal = SizeFlags.ExpandFill };
						titleLabel.AddThemeColorOverride("font_color", Ink);
						row.AddChild(titleLabel);

						string status;
						if (desk.HasPendingTradeSubmission(recordId)) status = "at the desk, waiting to hear back";
						else {
							TradeOutcome outcome = desk.ActiveTradeOutcome(recordId);
							status = outcome != TradeOutcome.Nothing ? $"{PlayerDesk.TradeOutcomeLabel(outcome)} — live"
								: desk.HasEverSubmittedToTrade(recordId) ? "came back with nothing" : "not submitted";
						}
						List<string> breakouts = desk.BreakoutRegionNames(recordId).ToList();
						if (breakouts.Count > 0) status += $"  ·  BREAKOUT: {string.Join(", ", breakouts)}";
						TradeAdTier? activeAd = desk.ActiveTradeAdTier(recordId);
						if (activeAd.HasValue) status += $"  ·  {PlayerDesk.TradeAdTierName(activeAd.Value)} ad running";
						var statusLabel = new Label {
							Text = status, SizeFlagsHorizontal = SizeFlags.ExpandFill,
							AutowrapMode = TextServer.AutowrapMode.WordSmart
						};
						statusLabel.AddThemeColorOverride("font_color", Ink);
						row.AddChild(statusLabel);

						if (!desk.HasEverSubmittedToTrade(recordId)) {
							var submitBtn = Btn("SUBMIT TO REVIEW DESK");
							submitBtn.CustomMinimumSize = new Vector2(220, 32);
							submitBtn.Pressed += () => Act(() => {
								bool ok = PlayerDesk.Instance.SubmitToReviewDesk(recordId, out string message);
								Say(message, ok);
								return true;
							});
							row.AddChild(submitBtn);
						}
						content.AddChild(row);

						// Directive §6.2: a guaranteed, paid version of the same signal -- $75/$250/$600, era
						// rate, not a genuine gamble tier. A full page is most of an $800 label's cash in one line.
						var adRow = new HBoxContainer();
						adRow.AddThemeConstantOverride("separation", 10);
						var adLabel = FormLabel("        Trade ad:");
						adLabel.CustomMinimumSize = new Vector2(120, 28);
						adRow.AddChild(adLabel);
						foreach (TradeAdTier tier in new[] { TradeAdTier.QuarterPage, TradeAdTier.HalfPage, TradeAdTier.FullPage }) {
							var adBtn = Btn($"{PlayerDesk.TradeAdTierName(tier).ToUpper()} (${PlayerDesk.TradeAdCost(tier):N0})");
							adBtn.CustomMinimumSize = new Vector2(160, 28);
							adBtn.Pressed += () => Act(() => {
								bool ok = PlayerDesk.Instance.BuyTradeAd(recordId, tier, out string message);
								Say(message, ok);
								return true;
							});
							adRow.AddChild(adBtn);
						}
						content.AddChild(adRow);
					}
				}

				List<(string LabelName, string Title, string RegionName)> rivalBreakouts = desk.RivalBreakoutListings().Take(6).ToList();
				if (rivalBreakouts.Count > 0) {
					Body("This week's breakout column, elsewhere:");
					foreach ((string labelName, string title, string regionName) in rivalBreakouts)
						Body($"    {labelName} — \"{title}\" breaking out in {regionName}");
				}
			}
		}

		// --- ARTIST BUY-IN (office only): an act buys a run of its own pressed single outright, cash now ---
		ArtistBuyInSection(desk);

		// --- STOCK OUT IN THE TOWNS ---
		var stopStock = desk.StopStock().OrderBy(s => s.CityName).ThenBy(s => s.StopName).ToList();
		var owedByStop = desk.OpenBalancesByStop().OrderByDescending(t => t.Amount).ToList();
		if (stopStock.Count > 0 || owedByStop.Count > 0) {
			SectionHeading("towns", "OUT IN THE TOWNS", false,
				"Copies sitting on shelves at accounts you've placed, and the money those accounts are holding for you.");
			if (stopStock.Count > 0)
				foreach ((string cityName, string stopName, string title, int remaining) in stopStock)
					Body($"    {cityName} — {stopName}: {remaining:N0} of \"{title}\" left on the shelves");

			if (owedByStop.Count > 0) {
				Body("Waiting to be collected (drive back to pocket the lump; a thin wire trickles in meanwhile):");
				foreach ((string cityName, string stopName, float amount) in owedByStop)
					Body($"    {cityName} — {stopName}: ${amount:N0} they're holding for you");
			}
		}

		// --- P&D DISTRIBUTION DEAL (directive §9) ---
		if (hasReleased || label.activeDeal != null || desk.PendingDistributionOffer != null) {
			SectionHeading("pnd", "P&D DISTRIBUTION DEAL", label.activeDeal != null || desk.PendingDistributionOffer != null,
				"A pressing-and-distribution deal covers the whole catalog for the term, not one title: an advance " +
				"(maybe), a real cut of everything, and often the masters, in trade for a distributor's own national " +
				"network. Only worth pitching once a record's proven itself regionally — otherwise nobody's biting.");
			if (label.activeDeal != null) {
				var deal = label.activeDeal;
				string distName = CompetitorManager.Instance?.GetLabel(deal.distributorId)?.labelName ?? deal.distributorId;
				int weeksLeft = Mathf.Max(0, deal.signedWeek + deal.termWeeks - (ChartManager.Instance?.GetCurrentChartWeek() ?? 0));
				Body($"Under contract to {distName} — {deal.marginSkim:P0} skim, {weeksLeft} week(s) left on the term" +
					(deal.ownsMasters ? ", masters signed away." : ", masters still yours.") +
					(deal.unrecoupedAdvance > 0.5f ? $" ${deal.unrecoupedAdvance:N0} of the advance still unrecouped." : ""));
			} else if (desk.PendingDistributionOffer != null) {
				var offer = desk.PendingDistributionOffer;
				string distName = desk.PendingDistributionOfferDistributorName ?? offer.distributorId;
				Body($"{distName} is offering: {offer.marginSkim:P0} skim, {offer.termWeeks}-week term" +
					(offer.advance > 0f ? $", ${offer.advance:N0} advance" : ", no advance") +
					(offer.ownsMasters ? ", and they take the masters." : ", masters stay yours.") +
					" Decide before pursuing anything else.");
				var offerRow = new HBoxContainer();
				offerRow.AddThemeConstantOverride("separation", 10);
				var acceptBtn = Btn("SIGN");
				acceptBtn.CustomMinimumSize = new Vector2(140, 40);
				acceptBtn.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.AcceptDistributionOffer(out string message); Say(message, ok); return ok; });
				offerRow.AddChild(acceptBtn);
				var declineBtn = Btn("WALK AWAY");
				declineBtn.CustomMinimumSize = new Vector2(140, 40);
				declineBtn.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.DeclineDistributionOffer(out string message); Say(message, ok); return ok; });
				offerRow.AddChild(declineBtn);
				content.AddChild(offerRow);
			} else {
				var pitchBtn = Btn($"PITCH FOR A DEAL ({PlayerDesk.SignHours}h)");
				pitchBtn.CustomMinimumSize = new Vector2(220, 40);
				pitchBtn.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.PursueDistributionDeal(out string message); Say(message, ok); return ok; });
				content.AddChild(pitchBtn);
			}
		}

		// --- WHOLESALE HOUSES (the gamble) ---
		if (hasReleased) {
			SectionHeading("wholesale", "WHOLESALE HOUSES — THE GAMBLE", false,
				"Some markets are too far to drive a line to. Hand it to a wholesale house out there and they'll press " +
				"it into shops you'll never reach — but they pay on their own terms months later, skim their cut, and " +
				"only for what they admit they sold. Without a real regional breakout to show them it's a cold pitch they " +
				"can turn down, or take on worse terms; break out there first and they come courting instead.");
			List<MarketRegion> open = desk.GetPlaceableMarkets().ToList();
			if (open.Count == 0) { Body("No house anywhere has room for another line right now."); return; }
			foreach (MarketRegion region in open) {
				var row = new HBoxContainer();
				row.AddThemeConstantOverride("separation", 12);
				int houses = CompetitorManager.Instance?.GetIndependentDistributorsInRegion(region.regionId)
					.Count(house => house.HasCapacity && !house.CarriesLabel(label.labelId)) ?? 0;
				bool proven = desk.IsProvenInRegion(region.regionId);
				var text = new Label {
					SizeFlagsHorizontal = SizeFlags.ExpandFill,
					AutowrapMode = TextServer.AutowrapMode.WordSmart,
					Text = $"{region.regionName}  —  {houses} house(s) with room  —  " +
						(proven ? "they've heard of you here" : "no proof here yet — a cold pitch")
				};
				text.AddThemeColorOverride("font_color", proven ? Ink : Rust);
				row.AddChild(text);

				var place = Btn(proven ? $"TAKE THE MEETING ({PlayerDesk.DistributionHours}h)" : $"GAMBLE A LINE ({PlayerDesk.DistributionHours}h)");
				place.CustomMinimumSize = new Vector2(220, 40);
				string capturedId = region.regionId;
				place.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.PlaceLine(capturedId, out string message); Say(message, ok); return ok; });
				row.AddChild(place);
				content.AddChild(row);
			}
		}
	}

	/// <summary>Directive §3.3's "one-stop with legs": an act buys a run of its own pressed single
	/// outright, cash to the label now, at a discount instead of a stop or a royalty. Office-only, like
	/// the rest of the plant/stock side of this window. Lives here rather than under an individual act's
	/// MANAGE window because it's a distribution channel, not a repertoire/studio decision -- and it
	/// spans the whole roster instead of forcing the player to click into each act to find who's eligible.</summary>
	private void ArtistBuyInSection(PlayerDesk desk) {
		if (!desk.AtHome) return;
		List<(SimulatedArtist Artist, string RecordId, string Title, int OnHand)> eligible = desk.BuyInEligible().ToList();
		if (eligible.Count == 0) return;

		SectionHeading("buyin", "ARTIST BUY-IN", desk.Label.cashReserves < 0f,
			$"An act will take {PlayerDesk.ArtistBuyInMin}-{PlayerDesk.ArtistBuyInMax} of its own single off your hands " +
			$"outright, cash on the spot -- ${PlayerDesk.ArtistBuyInPrice:F2}/copy, a discount for the volume, and it's theirs " +
			"to work on their own.");

		var pickRow = new HBoxContainer();
		pickRow.AddThemeConstantOverride("separation", 10);
		pickRow.AddChild(FormLabel("Act / single"));
		var singlePick = Option();
		singlePick.CustomMinimumSize = new Vector2(360, 36);
		foreach ((SimulatedArtist artist, string _, string title, int onHand) in eligible)
			singlePick.AddItem($"{artist.stageName} — \"{title}\" — {onHand:N0} on hand");
		pickRow.AddChild(singlePick);
		pickRow.AddChild(FormLabel("Qty"));
		int firstMax = Mathf.Min(PlayerDesk.ArtistBuyInMax, eligible[0].OnHand);
		var qtyInput = Spin(PlayerDesk.ArtistBuyInMin, firstMax, 5, firstMax);
		pickRow.AddChild(qtyInput);
		singlePick.ItemSelected += index => {
			int max = Mathf.Min(PlayerDesk.ArtistBuyInMax, eligible[Mathf.Clamp((int)index, 0, eligible.Count - 1)].OnHand);
			qtyInput.MaxValue = max;
			qtyInput.Value = Mathf.Min(qtyInput.Value, max);
		};
		content.AddChild(pickRow);

		var buyIn = Btn($"BUY IN  ({PlayerDesk.ArtistBuyInHours}h)");
		buyIn.CustomMinimumSize = new Vector2(200, 40);
		buyIn.Pressed += () => Act(() => {
			(SimulatedArtist artist, string recordId, _, _) = eligible[Mathf.Clamp(singlePick.Selected, 0, eligible.Count - 1)];
			bool ok = PlayerDesk.Instance.ArtistBuyIn(artist, recordId, (int)qtyInput.Value, out string message);
			Say(message, ok);
			return true;
		});
		content.AddChild(buyIn);
	}

	// ========================================================================
	// FINANCES
	// ========================================================================

	private void PageFinances() {
		PlayerDesk desk = PlayerDesk.Instance;
		AILabel label = desk.Label;

		Heading("THE BOOKS");
		float owed = label.outstandingWholesaleReceivables;
		float reserved = label.outstandingWholesaleReturnsReserve;
		var books = new List<LedgerTable.Row> {
			LedgerTable.Entry("Cash on hand", LedgerTable.Money(label.cashReserves)),
			LedgerTable.Entry("Owed to you by wholesalers", LedgerTable.Money(owed))
		};
		if (reserved > 0.5f) books.Add(LedgerTable.Entry("Held back against returns", LedgerTable.Deduct(reserved)));
		books.Add(LedgerTable.Entry("Written off: short pay, under-reporting, dead returns", LedgerTable.Deduct(label.lifetimeWholesaleWriteOffs)));
		books.Add(LedgerTable.Entry("Monthly overhead", LedgerTable.Deduct(label.GetMonthlyOverhead())));
		books.Add(LedgerTable.Entry("Signing reserve (two months of overhead)", LedgerTable.Money(2f * label.GetMonthlyOverhead())));
		books.Add(LedgerTable.Total("Last month's profit", LedgerTable.Money(label.lastMonthlyProfit)));
		content.AddChild(new LedgerTable().Set("The label's position", null, books));
		if (reserved > 0.5f) Body("Money held back against returns is released, or written off, when the window closes.");
		Body($"Signing advances must leave more than ${2f * label.GetMonthlyOverhead():N0} cash (two months of overhead). " +
			$"The overdraft ceiling is ${-desk.CreditFloor:N0}, but it is not a loan; each month-end below $0 advances the closure count, which resets when cash is above $0.");
		if (label.cashReserves < 0f)
			Body($"Recovery: sell a few copies from the trunk or at a hop table for cash, buy eligible stock back from an act at ${PlayerDesk.ArtistBuyInPrice:F2}/copy, collect town balances by working or revisiting that town, and factor eligible wholesale invoices below.");

		Heading("LAST WEEK'S SETTLEMENT");
		PlayerDesk.WeekBooks latest = desk.Books.FirstOrDefault();
		if (latest == null) Body("No week has settled yet. The chart settles on Fridays.");
		else {
			var settlementRows = new List<LedgerTable.Row> {
				LedgerTable.Entry("Units sold", $"{latest.Units:N0}"),
				LedgerTable.Entry("Retail gross", LedgerTable.Money(latest.Gross)),
				LedgerTable.Entry("Less: manufacturing", LedgerTable.Deduct(latest.ManufacturingCost)),
				LedgerTable.Entry("Less: distributor's skim", LedgerTable.Deduct(latest.DistributionSkim)),
				LedgerTable.Entry("Less: artist royalty", LedgerTable.Deduct(latest.ArtistRoyalty))
			};
			if (latest.RunnerCommission > 0f)
				settlementRows.Add(LedgerTable.Entry("Less: runner's commission", LedgerTable.Deduct(latest.RunnerCommission)));
			settlementRows.Add(LedgerTable.Total("Earned", LedgerTable.Money(latest.Earned)));
			settlementRows.Add(LedgerTable.Entry("Less: billed on credit", LedgerTable.Deduct(latest.Deferred)));
			if (latest.TrunkHeld > 0f)
				settlementRows.Add(LedgerTable.Entry("Less: held by the towns", LedgerTable.Deduct(latest.TrunkHeld)));
			settlementRows.Add(LedgerTable.Entry("Plus: old invoices paid", LedgerTable.Money(latest.Collected)));
			settlementRows.Add(LedgerTable.Total("Reached the bank", LedgerTable.Money(latest.Banked)));
			content.AddChild(new LedgerTable().Set("Week ending " + latest.Date.ToHeadlineString(), null, settlementRows));
			if (latest.Deferred > 0f)
				Body($"${latest.Deferred:N0} of what you earned this week went out on credit — " +
					"the houses pay on their own terms.");
			if (latest.TrunkHeld > 0f)
				Body($"${latest.TrunkHeld:N0} is out on consignment in towns you weren't standing in — " +
					"you collect it when you drive back.");
		}

		Heading("WHY THE MONEY IS LATE");
		Body("A wholesale house presses nothing and pays nothing up front: it takes the line, sells it, " +
			"and settles on its own terms months later — and only for what it admits it sold. Markets you " +
			"ship to yourself pay on the spot. That gap is what bankrupts a small label on a hit record.");

		Heading("OUT WITH THE HOUSES");
		var invoices = desk.OutstandingInvoices().ToList();
		if (invoices.Count == 0) Body("Nothing outstanding.");
		else {
			Body("Factoring sells an invoice now, at a discount, to whoever's willing to carry the wait and " +
				"the risk of it — the house still owes it, just not to you any more.");
			for (int i = 0; i < invoices.Count; i++) {
				(string houseName, string regionName, float amount, int weeksAway) = invoices[i];
				var row = new HBoxContainer();
				row.AddThemeConstantOverride("separation", 10);
				var line = new Label {
					Text = $"{(amount < 1f ? "under $1" : $"${amount:N0}")}  —  {houseName} ({regionName})  —  " +
						$"{(weeksAway == 0 ? "due now" : $"due in {weeksAway} week{(weeksAway == 1 ? "" : "s")}")}",
					CustomMinimumSize = new Vector2(420, 32),
					AutowrapMode = TextServer.AutowrapMode.WordSmart
				};
				line.AddThemeColorOverride("font_color", Ink);
				row.AddChild(line);
				int idx = i;
				var factorBtn = Btn($"FACTOR (~{desk.FactorRatePreview(idx):P0})");
				factorBtn.CustomMinimumSize = new Vector2(150, 32);
				factorBtn.Pressed += () => Act(() => {
					bool ok = PlayerDesk.Instance.FactorReceivable(idx, out string message);
					Say(message, ok);
					return true;
				});
				row.AddChild(factorBtn);
				content.AddChild(row);
			}
		}

		Heading("WEEK BY WEEK");
		var weeks = desk.Books.Take(14).ToList();
		if (weeks.Count == 0) Body("Nothing settled yet.");
		else
			content.AddChild(new LedgerTable().Set(null, new[] { "week ending", "units", "earned", "banked", "owed you", "cash" },
				weeks.Select(week => LedgerTable.Entry(week.Date.ToHeadlineString(), $"{week.Units:N0}", LedgerTable.Money(week.Earned),
					LedgerTable.Money(week.Banked), LedgerTable.Money(week.Outstanding), LedgerTable.Money(week.Cash)))));

		Heading("RECORD BY RECORD");
		var released = desk.ReleasedRecords.OrderByDescending(record => record.lifetimeLabelNet).ToList();
		if (released.Count == 0) Body("Nothing released yet.");
		else {
			var recordRows = new List<LedgerTable.Row>();
			foreach (RecordRuntimeData record in released) {
				float net = record.lifetimeLabelNet;
				float cost = record.sunkProductionCost;
				// Fold in this week's trunk units whose money is already booked but whose count hasn't
				// settled yet, so dollars-per-unit reads straight mid-week.
				long unitsLifetime = record.totalUnitsSold + desk.PendingTrunkUnits(record.baseRecord.recordId);
				recordRows.Add(LedgerTable.Entry($"\"{record.baseRecord.title}\" — {record.baseRecord.artistName}",
					$"{unitsLifetime:N0}", $"{record.unitsThisWeek:N0}", record.peakPosition > 0 ? $"#{record.peakPosition}" : "—",
					LedgerTable.Money(net), LedgerTable.Money(cost), LedgerTable.Money(net - cost)));
			}
			content.AddChild(new LedgerTable().Set(null, new[] { "record", "units", "this wk", "peak", "earned", "tape", "net" }, recordRows));
			Body("Net is what a record has earned against its tape. In parentheses, it still has that much to make back.");

			foreach (RecordRuntimeData record in released) {
				string recordId = record.baseRecord.recordId;
				// Directive §9: a one-off transaction on this one title, distinct from the P&D deal
				// above (which covers the whole catalog). Only on the table once a station and a
				// one-stop both know it -- MasterDealEligible is the single source of truth for that.
				if (desk.IsMasterOut(recordId)) {
					Body($"\"{record.baseRecord.title}\" — the master's out on this one; not yours to sell right now.");
				} else if (desk.MasterDealEligible(recordId)) {
					Body($"\"{record.baseRecord.title}\" — a one-off deal on the master:");
					var dealRow = new HBoxContainer();
					dealRow.AddThemeConstantOverride("separation", 10);
					var leaseBtn = Btn($"LEASE THE MASTER (${desk.MasterLeaseValue(recordId):N0}, {PlayerDesk.MasterLeaseTermWeeks}wk)");
					leaseBtn.CustomMinimumSize = new Vector2(260, 36);
					leaseBtn.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.LeaseMaster(recordId, out string message); Say(message, ok); return ok; });
					dealRow.AddChild(leaseBtn);
					var sellBtn = Btn($"SELL THE MASTER (${desk.MasterSaleValue(recordId):N0})");
					sellBtn.CustomMinimumSize = new Vector2(220, 36);
					sellBtn.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.SellMaster(recordId, out string message); Say(message, ok); return ok; });
					dealRow.AddChild(sellBtn);
					content.AddChild(dealRow);
				}
			}
		}

		Heading("ARTIST ACCOUNTS");
		var roster = desk.Roster.ToList();
		if (roster.Count == 0) { Body("Nobody signed."); return; }
		content.AddChild(new LedgerTable().Set(null, new[] { "act", "unrecouped", "paid through", "of retail" },
			roster.Select(artist => LedgerTable.Entry(artist.stageName, LedgerTable.Money(artist.unrecoupedAdvance),
				LedgerTable.Money(artist.totalRoyaltyEarnings), $"{artist.royaltyRate:P1}"))));
	}

	// ========================================================================
	// ROLODEX
	// ========================================================================

	/// <summary>The live advocacy this station is holding for you: the record-specific promise a won
	/// call actually buys, with the weeks left on it.</summary>
	private void RenderCarrying(PlayerDesk desk, RolodexEntry entry) {
		var chart = ChartManager.Instance;
		if (chart == null) return;
		int week = chart.GetCurrentChartWeek();
		var live = chart.Advocacy.ForStation(entry.stationId);
		if (live.Count == 0) return;

		Heading("WHERE IT STANDS");
		foreach (StationAdvocacy a in live) {
			// Dealer-margin-and-flip directive §3.4: a "work the flip" pitch writes advocacy under a
			// synthetic recordId (FlipAdvocacyKey) rather than the record's own -- strip the suffix to
			// find the real record, and render it as its own status line rather than the normal
			// on-air/meeting one, since there's no spin tier to report for a side that isn't out yet.
			bool isFlip = a.recordId != null && a.recordId.EndsWith("|flip", StringComparison.Ordinal);
			string realRecordId = isFlip ? a.recordId[..^"|flip".Length] : a.recordId;
			RecordRuntimeData rec = desk.ReleasedRecords.FirstOrDefault(r => r.baseRecord?.recordId == realRecordId);
			string baseTitle = rec?.baseRecord?.title ?? "(unknown record)";
			string title = isFlip ? $"the flip of \"{baseTitle}\"" : $"\"{baseTitle}\"";
			int weeksLeft = Mathf.Max(0, a.expiresWeek - week + 1);
			SpinTier tier = isFlip ? SpinTier.None : chart.SpinTierOf(entry.stationId, a.recordId);
			string how = a.method switch {
				AdvocacyMethod.PersonalPitch  => "on your word",
				AdvocacyMethod.FavorCalledIn  => "as a favour",
				AdvocacyMethod.AdvertisingBuy => "as an advertiser",
				AdvocacyMethod.RivalPressure  => "to beat the competition",
				AdvocacyMethod.DealerReport   => "on a dealer's report",
				AdvocacyMethod.RecordHop      => "after a hop he watched himself",
				_ => "",
			};

			// The headline is what the station is ACTUALLY doing, not what you bought.
			string status = isFlip
				? (a.expired ? "That window's closed -- he never turned it over." : $"He's listening for it ({weeksLeft} more week(s))")
				: tier != SpinTier.None
					? $"ON THE AIR — {PlayerDesk.TierWord(tier)} rotation"
					: a.expired ? "Not picked up. His argument has run out."
					: $"Not on yet — he's still arguing for it ({weeksLeft} more meeting(s))";
			var head = new Label { Text = $"  {title} — {status}", AutowrapMode = TextServer.AutowrapMode.WordSmart };
			head.AddThemeColorOverride("font_color",
				tier != SpinTier.None ? new Color("4a7a4a") : a.expired ? Rust : Heard);
			content.AddChild(head);

			var detail = new Label {
				Text = $"      Taken {how}." + (a.expired ? "" : $" Worth +{a.candidacyBoost * 100f:F0}% to it in his meeting."),
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
			};
			detail.AddThemeFontSizeOverride("font_size", 14);
			detail.AddThemeColorOverride("font_color", Heard);
			content.AddChild(detail);

			// Directive §10: "a station whose spin tier is sliding should be visible on the card BEFORE
			// it drops" -- stationsDropped is a one-way latch, so this is the one warning the player gets.
			if (tier != SpinTier.None && desk.IsSlidingTowardDrop(a.recordId, entry.stationId)) {
				var warn = new Label {
					Text = "      Slipping — drive back and work it, or lose the spot for good.",
					AutowrapMode = TextServer.AutowrapMode.WordSmart,
				};
				warn.AddThemeFontSizeOverride("font_size", 14);
				warn.AddThemeColorOverride("font_color", Rust);
				content.AddChild(warn);
			}
		}
		Body("A station playing your record is not the same as a station keeping it. Next week's meeting " +
			"judges it on sales like anything else.");
	}

	// ========================================================================
	// THE CALL
	// ========================================================================

	/// <summary>
	/// One phone call, rendered as a transcript plus whatever the current beat offers. The scene lives
	/// on PlayerDesk (so a Refresh does not drop the call mid-sentence); this only draws it.
	/// </summary>
	private void PageCall(PlayerDesk desk, RolodexCall call) {
		RolodexCallContext c = call.ctx;

		bool inPerson = call.inPersonBonus > 0f;
		Heading(inPerson
			? $"TALKING WITH {call.entry.displayName.ToUpperInvariant()}"
			: $"CALLING {call.entry.displayName.ToUpperInvariant()}");
		if (c.station != null)
			Body($"{(inPerson ? "IN THE LOBBY  ·  " : "")}{c.station.callsign}  ·  {c.station.format}  ·  {c.station.cityName}  ·  " +
				$"{RolodexEntry.TierLabel(c.tier)}");

		// The record on the table. Switching it rebuilds the situation read.
		var records = desk.RecordsOnTheTable().ToList();
		if (records.Count > 0 && call.stage is CallStage.Open) {
			var pickRow = new HBoxContainer();
			pickRow.AddThemeConstantOverride("separation", 6);
			pickRow.AddChild(FormLabel("On the table:"));
			foreach ((string recordId, string recordTitle, bool isAcetate) in records) {
				bool picked = recordId == call.recordId;
				var recBtn = Btn($"{(picked ? "» " : "")}{recordTitle}{(isAcetate ? " (acetate)" : "")}");
				string rid = recordId;
				recBtn.Pressed += () => { rolodexPitchRecordId = rid; desk.SetCallRecord(call, rid); Refresh(); };
				pickRow.AddChild(recBtn);
			}
			content.AddChild(pickRow);
		}

		RenderTranscript(call);

		switch (call.stage) {
			case CallStage.NotConnected: RenderNotConnected(desk, call); break;
			case CallStage.Open:         RenderApproaches(desk, call);   break;
			case CallStage.Pushback:     RenderCounters(desk, call);     break;
			case CallStage.Resolved:     RenderResolved(desk, call);     break;
		}
	}

	private void RenderTranscript(RolodexCall call) {
		var frame = new PanelContainer();
		frame.AddThemeStyleboxOverride("panel", new StyleBoxFlat {
			BgColor = new Color("e7d8b4"), ContentMarginLeft = 12, ContentMarginRight = 12,
			ContentMarginTop = 10, ContentMarginBottom = 10,
		});
		var box = new VBoxContainer();
		box.AddThemeConstantOverride("separation", 8);
		frame.AddChild(box);
		content.AddChild(frame);

		foreach (CallLine line in call.transcript) {
			if (line.voice != ExecutiveVoice.None) {
				// An instinct read: a voice in your own head, not a line on the phone.
				var read = new Label {
					Text = $"{VoiceName(line.voice)} — {line.text}",
					AutowrapMode = TextServer.AutowrapMode.WordSmart,
				};
				read.AddThemeFontSizeOverride("font_size", 15);
				read.AddThemeColorOverride("font_color", VoiceColor(line.voice));
				box.AddChild(read);
				continue;
			}
			if (!string.IsNullOrEmpty(line.speaker)) {
				var who = new Label { Text = line.speaker.ToUpperInvariant() };
				who.AddThemeFontSizeOverride("font_size", 13);
				who.AddThemeColorOverride("font_color", Heard);
				box.AddChild(who);
			}
			var body = new Label {
				Text = string.IsNullOrEmpty(line.speaker) ? line.text : $"“{line.text.Trim('“', '”', '"')}”",
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
			};
			body.AddThemeFontSizeOverride("font_size", 16);
			body.AddThemeColorOverride("font_color", line.isPlayer ? new Color("4a3a6b") : Ink);
			box.AddChild(body);
		}
	}

	private void RenderNotConnected(PlayerDesk desk, RolodexCall call) {
		// He is off shift or on the air: ringing again in five minutes is the wrong answer, so the better one leads.
		bool wrongTime = desk.CanScheduleCallback(call);
		if (wrongTime) {
			(GameDate callDate, int callHour) = desk.CallbackSlotFor(call);
			GameDate today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
			string when = callDate == today ? "today" : "tomorrow";
			var later = Btn($"CALL BACK AT {RolodexShifts.ClockLabel(callHour)}  ({when}, no time spent)");
			later.CustomMinimumSize = new Vector2(360, 40);
			later.Pressed += () => { desk.ScheduleCallback(call, out string msg); if (!string.IsNullOrEmpty(msg)) Say(msg); Refresh(); };
			content.AddChild(later);
		}
		var again = Btn(wrongTime ? $"TRY HIM ANYWAY  ({PlayerDesk.DialMinutes} min)" : $"TRY AGAIN  ({PlayerDesk.DialMinutes} min)");
		again.Pressed += () => {
			desk.EndCall(call);
			desk.PlaceCall(call.entry, call.recordId, out string msg);
			if (!string.IsNullOrEmpty(msg)) Say(msg);
			Refresh();
		};
		content.AddChild(again);
		var hang = Btn("PUT THE PHONE DOWN");
		hang.Pressed += () => { desk.EndCall(call); Refresh(); };
		content.AddChild(hang);
		Body("Every attempt costs you the same five minutes, and the more you burn on one man " +
			"the worse the rest of the day's calls go.");
	}

	private void RenderApproaches(PlayerDesk desk, RolodexCall call) {
		Heading("WHAT DO YOU SAY");
		foreach (CallOption opt in desk.ApproachOptions(call)) {
			if (opt.approach == RolodexApproach.HangUp) continue;
			RenderOption(opt, () => {
				object payload = null;
				if (opt.approach == RolodexApproach.CommercialPitch) payload = adBuyTier;
				if (opt.approach == RolodexApproach.OfferPayola) payload = payolaTier;
				desk.ChooseApproach(call, opt.approach, payload, out string msg);
				if (!string.IsNullOrEmpty(msg)) Say(msg);
				Refresh();
			});
			// Money verbs carry a size selector inline, so the number is chosen before the sentence.
			if (opt.enabled && opt.approach == RolodexApproach.CommercialPitch)
				RenderTierRow("Size of buy:", new[] { PlayerDesk.AdBuyTier.Small, PlayerDesk.AdBuyTier.Medium, PlayerDesk.AdBuyTier.Large },
					t => $"{PlayerDesk.AdBuyTierName(t)} ${PlayerDesk.AdBuyCost(t):N0}", t => adBuyTier = t, adBuyTier);
			if (opt.enabled && opt.approach == RolodexApproach.OfferPayola)
				RenderTierRow("Size of envelope:", new[] { PlayerDesk.PayolaTier.Small, PlayerDesk.PayolaTier.Medium, PlayerDesk.PayolaTier.Large },
					t => $"{PlayerDesk.PayolaTierName(t)} ${PlayerDesk.PayolaCost(t):N0}", t => payolaTier = t, payolaTier);
		}

		var hang = Btn(call.inPersonBonus > 0f ? "END THE CONVERSATION" : "HANG UP");
		hang.Pressed += () => { desk.EndCall(call); Refresh(); };
		content.AddChild(hang);
	}

	private void RenderTierRow<T>(string label, T[] tiers, Func<T, string> name, Action<T> set, T current) where T : Enum {
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 6);
		var lbl = new Label { Text = label };
		lbl.AddThemeColorOverride("font_color", Heard);
		row.AddChild(lbl);
		foreach (T tier in tiers) {
			bool picked = Equals(tier, current);
			var b = Btn($"{(picked ? "» " : "")}{name(tier)}");
			T captured = tier;
			b.Pressed += () => { set(captured); Refresh(); };
			row.AddChild(b);
		}
		content.AddChild(row);
	}

	private void RenderCounters(PlayerDesk desk, RolodexCall call) {
		Heading("HOW DO YOU ANSWER THAT");
		var odds = new Label { Text = $"As it stands: roughly {call.EffectiveChance * 100f:F0}% he says yes." };
		odds.AddThemeColorOverride("font_color", Heard);
		content.AddChild(odds);

		foreach (CallOption opt in desk.CounterOptions(call)) {
			RenderOption(opt, () => {
				desk.PlayCounter(call, opt.counter, out string msg);
				if (!string.IsNullOrEmpty(msg)) Say(msg);
				Refresh();
			});
		}
	}

	private void RenderResolved(PlayerDesk desk, RolodexCall call) {
		var more = Btn(call.inPersonBonus > 0f ? "KEEP TALKING" : "KEEP HIM ON THE LINE");
		more.Pressed += () => { desk.ContinueCall(call); Refresh(); };
		content.AddChild(more);
		var hang = Btn(call.inPersonBonus > 0f ? "END THE CONVERSATION" : "HANG UP");
		hang.Pressed += () => { desk.EndCall(call); Refresh(); };
		content.AddChild(hang);
	}

	/// <summary>One option button, coloured by the voice that surfaced it, with its cost and its
	/// truthfulness spelled out underneath. A bluff is always labelled as one.</summary>
	private void RenderOption(CallOption opt, Action onPressed) {
		string prefix = opt.voice == ExecutiveVoice.None ? "" : $"[{VoiceName(opt.voice)}] ";
		var btn = Btn(prefix + opt.label);
		btn.CustomMinimumSize = new Vector2(0, 38);
		btn.Disabled = !opt.enabled;
		if (opt.enabled) btn.Pressed += onPressed;
		if (opt.voice != ExecutiveVoice.None) {
			btn.AddThemeColorOverride("font_color", VoiceColor(opt.voice));
			btn.AddThemeColorOverride("font_hover_color", VoiceColor(opt.voice));
		}
		content.AddChild(btn);

		string sub = opt.enabled ? opt.subLabel : opt.disabledReason;
		if (!string.IsNullOrEmpty(sub)) {
			var note = new Label { Text = "     " + sub, AutowrapMode = TextServer.AutowrapMode.WordSmart };
			note.AddThemeFontSizeOverride("font_size", 14);
			note.AddThemeColorOverride("font_color", opt.isBluff ? Rust : Heard);
			content.AddChild(note);
		}
		if (opt.isBluff) {
			var warn = new Label { Text = "     (Not true. He may check.)" };
			warn.AddThemeFontSizeOverride("font_size", 13);
			warn.AddThemeColorOverride("font_color", Rust);
			content.AddChild(warn);
		}
	}

	private static string VoiceName(ExecutiveVoice voice) => voice switch {
		ExecutiveVoice.Ear    => "THE EAR",
		ExecutiveVoice.Street => "THE STREET",
		ExecutiveVoice.Suit   => "THE SUIT",
		ExecutiveVoice.Fixer  => "THE FIXER",
		_ => "",
	};

	private static Color VoiceColor(ExecutiveVoice voice) => voice switch {
		ExecutiveVoice.Ear    => new Color("6b3a5a"),
		ExecutiveVoice.Street => new Color("3a6b4a"),
		ExecutiveVoice.Suit   => new Color("3a4a6b"),
		ExecutiveVoice.Fixer  => new Color("6b4a1c"),
		_ => new Color("2b2115"),
	};

	private void RenderWorkThePhones(PlayerDesk desk) {
		var allReporters = ChartManager.Instance?.ReporterStationsInRegion(desk.Label?.homeRegion ?? "");
		if (allReporters == null) return;
		int known = desk.Rolodex.Count;
		int total = allReporters.Count;
		if (known >= total) {
			Body($"You have leads on all {total} reporter station(s) in your region. Branch out to other markets to grow the book.");
			return;
		}
		Heading("WORK THE PHONES");
		Body($"Your region: {known} of {total} reporter station(s) in your book. Cold-calling gets you a " +
			"name more often than it gets you a man, and it gets you nothing at all more often than either.");
		int attempts = desk.CallAttemptsToday;
		if (attempts >= 2) {
			var warn = new Label { Text = attempts >= 4
				? "You've been on the phone all day. Nobody's taking your call now."
				: $"{attempts} rounds of calls today already. The odds are getting worse." };
			warn.AddThemeColorOverride("font_color", Rust);
			content.AddChild(warn);
		}
		var callBtn = Btn($"WORK THE PHONES  ({PlayerDesk.WorkThePhonesMinMinutes}-{PlayerDesk.WorkThePhonesMaxMinutes} min)");
		callBtn.CustomMinimumSize = new Vector2(280, 42);
		callBtn.Pressed += () => {
			bool ok = PlayerDesk.Instance.WorkThePhones(out string msg);
			Say(msg, ok);
			Refresh();
		};
		content.AddChild(callBtn);
	}

	// Portrait monogram: two initials from the display name.
	private static string Monogram(string name) {
		if (string.IsNullOrWhiteSpace(name)) return "?";
		string[] parts = name.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length == 1) return parts[0][0].ToString().ToUpperInvariant();
		return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
	}

	private static Color ArchetypeColor(DJArchetype arch) => arch switch {
		DJArchetype.Personality => new Color("5a3a6b"),
		DJArchetype.Tastemaker  => new Color("3a4a6b"),
		DJArchetype.Hustler     => new Color("6b3a3a"),
		DJArchetype.CompanyMan  => new Color("3a4a3a"),
		DJArchetype.Regional    => new Color("5a4a2a"),
		_                        => new Color("3a3a3a")
	};

	// ========================================================================
	// OFFICE (the ledger / log)
	// ========================================================================

	private void OfficeBody() {
		PlayerDesk desk = PlayerDesk.Instance;

		// Character card: who you are and what you can read.
		FoundingArchetypeData.ArchetypeProfile archProfile = FoundingArchetypeData.Get(desk.Archetype);
		ExecutiveInstinctProfile inst = desk.InstinctProfile;
		Heading($"{archProfile.Name.ToUpperInvariant()}");
		Body(archProfile.Tagline);
		content.AddChild(InstinctRow(inst, true));

		PhoneSection(desk);
		ReturnsSection(desk);
		StaffSection(desk);
	}

	private void PageLedger() {
		PlayerDesk desk = PlayerDesk.Instance;
		desk.MarkLogRead();
		Heading("THE LEDGER");
		Body("A running account of money, acts, records, and calls.");
		var filters = new HBoxContainer();
		foreach (string filter in new[] { "ALL", "MONEY", "ACTS", "RECORDS", "CALLS" }) {
			string captured = filter;
			var button = Btn(filter);
			StyleToggle(button, filter == ledgerFilter);
			button.Pressed += () => { ledgerFilter = captured; Refresh(); };
			filters.AddChild(button);
		}
		content.AddChild(filters);
		IEnumerable<string> entries = desk.Log;
		if (ledgerFilter != "ALL") entries = entries.Where(entry => LedgerCategory(entry) == ledgerFilter);
		string[] visible = entries.ToArray();
		if (visible.Length == 0) { Body("Nothing in this section yet."); return; }
		foreach (string entry in visible) Body(entry);
	}

	private static string LedgerCategory(string entry) {
		string value = (entry ?? string.Empty).ToLowerInvariant();
		if (value.Contains("call") || value.Contains("phone") || value.Contains("called")) return "CALLS";
		if (value.Contains("record") || value.Contains("master") || value.Contains("press") || value.Contains("release") || value.Contains("single")) return "RECORDS";
		if (value.Contains("$") || value.Contains("cash") || value.Contains("paid") || value.Contains("cost") || value.Contains("revenue")) return "MONEY";
		return "ACTS";
	}

	/// <summary>Directive §4's "office call list" -- who's phoned in demand, and the answering-service
	/// unlock that decides whether a call raised while the player's on the road even gets logged.</summary>
	private void PhoneSection(PlayerDesk desk) {
		Heading("THE PHONE");
		if (desk.HasAnsweringService)
			Body("An answering service is on the line -- calls get caught whether you're at the desk or out on the road.");
		else {
			Body($"Nobody's here to pick up when you're out of town -- calls that come in while you're on the road never " +
				$"reach you. An answering service (${AILabel.AnsweringServiceMonthlyCost:N0}/mo) fixes that.");
			var hire = Btn($"HIRE ANSWERING SERVICE  (${AILabel.AnsweringServiceMonthlyCost:N0}/mo)");
			hire.CustomMinimumSize = new Vector2(280, 40);
			hire.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.PurchaseAnsweringService(out string message); Say(message, ok); return ok; });
			content.AddChild(hire);
		}

		var calls = desk.PendingCalls().ToList();
		if (calls.Count == 0) { Body("The phone's quiet."); return; }
		foreach (var (call, stopName, cityName, title, expiresIn) in calls) {
			string termsHint = call.ConsignmentTerms ? "consignment" : "COD";
			var line = new Label {
				Text = $"    {stopName} ({cityName}) -- {CallReasonText(call.Reason)} on \"{title}\"  •  " +
					$"about {call.RequestedQty:N0}, {termsHint}  •  {(expiresIn <= 0 ? "won't wait much longer" : $"{expiresIn} week{(expiresIn == 1 ? "" : "s")} before they give up")}",
				AutowrapMode = TextServer.AutowrapMode.WordSmart
			};
			line.AddThemeColorOverride("font_color", Ink);
			content.AddChild(line);
		}
	}

	/// <summary>Dealer-margin-and-flip directive §4: R1, dead consignment come back -- a stop this old
	/// won't take a fresh lot until you clear the one that's dead. R2, a one-stop's return privilege on
	/// a carton that died. Two different claims (inventory sitting on a shelf vs. a wholesale firm sale),
	/// so two lists, but the same office readout.</summary>
	private void ReturnsSection(PlayerDesk desk) {
		List<(string StopId, string StopName, string CityName, string RecordId, string Title, int Remaining)> pending =
			desk.PendingReturns().ToList();
		if (pending.Count > 0) {
			Heading("WHAT'S COMING BACK");
			Body("These accounts want dead stock off the shelf -- they won't take a fresh lot of the title until you take it back. No money changes hands; it's yours, less a scuff loss.");
			foreach (var (stopId, stopName, cityName, recordId, title, remaining) in pending) {
				var row = new HBoxContainer();
				row.AddThemeConstantOverride("separation", 10);
				var lbl = new Label {
					Text = $"    {stopName} ({cityName}) -- {remaining:N0} of \"{title}\" gone dead",
					AutowrapMode = TextServer.AutowrapMode.WordSmart,
				};
				lbl.AddThemeColorOverride("font_color", Rust);
				row.AddChild(lbl);
				var accept = Btn("TAKE IT BACK");
				accept.Pressed += () => Act(() => {
					bool ok = PlayerDesk.Instance.AcceptReturn(stopId, recordId, out string message);
					Say(message, ok);
					return true;
				});
				row.AddChild(accept);
				content.AddChild(row);
			}
		}

		List<(string SaleId, string StopName, string CityName, string Title, int Returnable, int ExpiresInWeeks)> cartons =
			desk.PendingOneStopReturns().ToList();
		if (cartons.Count > 0) {
			Heading("CARTON RETURN PRIVILEGE");
			Body("A one-stop's firm sale still carries a return privilege on whatever doesn't move. Exercising it credits what they owe you first, cash only for the rest.");
			foreach (var (saleId, stopName, cityName, title, returnable, expiresIn) in cartons) {
				var row = new HBoxContainer();
				row.AddThemeConstantOverride("separation", 10);
				var lbl = new Label {
					Text = $"    {stopName} ({cityName}) -- up to {returnable:N0} of \"{title}\" returnable  •  " +
						(expiresIn <= 0 ? "window closing" : $"{expiresIn} week{(expiresIn == 1 ? "" : "s")} left"),
					AutowrapMode = TextServer.AutowrapMode.WordSmart,
				};
				lbl.AddThemeColorOverride("font_color", Heard);
				row.AddChild(lbl);
				var exercise = Btn("RETURN IT");
				exercise.Pressed += () => Act(() => {
					bool ok = PlayerDesk.Instance.ExerciseOneStopReturn(saleId, returnable, out string message);
					Say(message, ok);
					return true;
				});
				row.AddChild(exercise);
				content.AddChild(row);
			}
		}
	}

	/// <summary>The runner's route as an account checklist, grouped by town: every shop and jukebox operator he
	/// may cover, with its name beside its toggle, plus one-click ALL / NONE for the whole route or a town.</summary>
	private void RunnerRouteSection(PlayerDesk desk) {
		List<PlayerDesk.PlayerStop> eligible = desk.RunnerEligibleStops().ToList();
		int onRoute = eligible.Count(stop => desk.IsOnRunnerRoute(stop.StopId));
		Heading("HIS ROUTE");
		Body("He works record shops and jukebox operators in towns you've opened yourself, once a chart week, " +
			"off one carton of one single, and it costs you no hours. Pick who he covers; the work happens by itself.");
		if (eligible.Count == 0) {
			Body("No accounts to give him yet. Work a town's shops and operators yourself first.");
			return;
		}
		Body($"{onRoute} of {eligible.Count} accounts on his route.");

		var everyone = new HFlowContainer();
		everyone.AddThemeConstantOverride("h_separation", 8);
		everyone.AddThemeConstantOverride("v_separation", 6);
		var all = Btn($"PUT ALL {eligible.Count} ON HIS ROUTE");
		all.Disabled = onRoute == eligible.Count;
		all.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.SetRunnerRoute(eligible.Select(s => s.StopId), true, out string message); Say(message, ok); return ok; });
		everyone.AddChild(all);
		var none = Btn("CLEAR HIS ROUTE");
		none.Disabled = onRoute == 0;
		none.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.SetRunnerRoute(eligible.Select(s => s.StopId), false, out string message); Say(message, ok); return ok; });
		everyone.AddChild(none);
		content.AddChild(everyone);

		foreach (var town in eligible.GroupBy(stop => stop.CityId)) {
			List<PlayerDesk.PlayerStop> stops = town.ToList();
			int townOn = stops.Count(stop => desk.IsOnRunnerRoute(stop.StopId));
			var head = new HBoxContainer();
			head.AddThemeConstantOverride("separation", 8);
			var townLabel = new Label { Text = $"{(DistanceModel.GetCityById(town.Key)?.name ?? town.Key).ToUpperInvariant()}  —  {townOn} of {stops.Count}", SizeFlagsHorizontal = SizeFlags.ExpandFill };
			townLabel.AddThemeColorOverride("font_color", Heard);
			head.AddChild(townLabel);
			var townAll = Btn("ALL");
			townAll.Disabled = townOn == stops.Count;
			townAll.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.SetRunnerRoute(stops.Select(s => s.StopId), true, out string message); Say(message, ok); return ok; });
			head.AddChild(townAll);
			var townNone = Btn("NONE");
			townNone.Disabled = townOn == 0;
			townNone.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.SetRunnerRoute(stops.Select(s => s.StopId), false, out string message); Say(message, ok); return ok; });
			head.AddChild(townNone);
			content.AddChild(head);

			var grid = new GridContainer { Columns = 2, SizeFlagsHorizontal = SizeFlags.ExpandFill };
			grid.AddThemeConstantOverride("h_separation", 16);
			grid.AddThemeConstantOverride("v_separation", 2);
			foreach (PlayerDesk.PlayerStop stop in stops) {
				string knows = desk.RunnerFamiliarityAt(stop.StopId) > 0.01f ? ", he knows it" : "";
				var box = Check($"{stop.DisplayName}  ({StopKindLabel(stop.Kind).TrimEnd('s')}{knows})", desk.IsOnRunnerRoute(stop.StopId));
				box.SizeFlagsHorizontal = SizeFlags.ExpandFill;
				box.ClipText = true;
				box.TooltipText = box.Text;
				string stopId = stop.StopId;
				box.Toggled += on => Act(() => { bool ok = PlayerDesk.Instance.AssignRunnerStop(stopId, on, out string message); Say(message, ok); return ok; });
				grid.AddChild(box);
			}
			content.AddChild(grid);
		}
	}

	/// <summary>Directive §7: contractors, not payroll. The commission runner (route + carton) and the
	/// project promo man (a one-off record/city radio push) both live here, next to the answering-service
	/// hire they're philosophically the same shape as -- a spend decision, never a salary line.</summary>
	private void StaffSection(PlayerDesk desk) {
		Heading("THE STAFF");

		// --- COMMISSION RUNNER ---
		if (!desk.HasRunner) {
			if (!desk.RunnerUnlocked)
				Body($"Nobody's asking to run your route yet. Keep servicing standing accounts in one town " +
					$"({PlayerDesk.RunnerUnlockReorders} reorders gets his attention), or let demand ring in from " +
					$"{PlayerDesk.RunnerUnlockCities} towns the same week.");
			else {
				Body("A runner's willing to cover your route on commission -- no salary, just a cut of what he collects, paid when the shop pays.");
				var hire = Btn("HIRE A COMMISSION RUNNER");
				hire.CustomMinimumSize = new Vector2(280, 40);
				hire.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.HireRunner(out string message); Say(message, ok); return ok; });
				content.AddChild(hire);
			}
		} else {
			Body($"Your runner keeps {PlayerDesk.RunnerCommission:P0} of what he collects, taken the instant it lands -- no salary of his own.");
			string cartonTitle = desk.RunnerCartonRecordId != null
				? desk.PressableSingles().FirstOrDefault(s => s.RecordId == desk.RunnerCartonRecordId).Title ?? desk.RunnerCartonRecordId
				: null;
			Body(desk.RunnerCartonRemaining > 0
				? $"Carrying {desk.RunnerCartonRemaining:N0} of \"{cartonTitle}\"."
				: "Carton's empty -- hand him stock, or he sits idle.");

			List<(string RecordId, string Title, int OnHand)> onHand = desk.PressedSinglesOnHand().ToList();
			if (onHand.Count > 0) {
				var row = new HBoxContainer();
				row.AddThemeConstantOverride("separation", 10);
				row.AddChild(FormLabel("Hand him"));
				var pick = Option();
				pick.CustomMinimumSize = new Vector2(260, 36);
				foreach (var (_, title, inHand) in onHand) pick.AddItem($"\"{title}\" -- {inHand:N0} on hand");
				row.AddChild(pick);
				var qty = Spin(1, 5000, 1, 100);
				row.AddChild(qty);
				var hand = Btn($"HAND OFF ({PlayerDesk.RunnerHandoffHours}h)");
				hand.Pressed += () => Act(() => {
					(string recordId, _, _) = onHand[Mathf.Clamp(pick.Selected, 0, onHand.Count - 1)];
					bool ok = PlayerDesk.Instance.HandCartonToRunner(recordId, (int)qty.Value, out string message);
					Say(message, ok);
					return true;
				});
				row.AddChild(hand);
				content.AddChild(row);
			}

			RunnerRouteSection(desk);
		}

		// --- PROJECT PROMO ---
		Heading("PROJECT PROMO");
		Body("A one-off, city-scoped radio push, not a hire -- spins and rumors, never units. Payola-adjacent: " +
			"it can get burned, and a burn freezes the market there.");
		List<RecordRuntimeData> released = desk.ReleasedRecords.Where(r => r.baseRecord != null).ToList();
		List<MarketCity> cities = desk.WorkedCities.Select(DistanceModel.GetCityById).Where(c => c != null).ToList();
		if (released.Count == 0 || cities.Count == 0)
			Body("Needs a released single, and a town you've already opened yourself.");
		else {
			var recRow = new HBoxContainer();
			recRow.AddThemeConstantOverride("separation", 10);
			recRow.AddChild(FormLabel("Record"));
			var recPick = Option();
			recPick.CustomMinimumSize = new Vector2(260, 36);
			foreach (RecordRuntimeData r in released) recPick.AddItem($"\"{r.baseRecord.title}\"");
			recRow.AddChild(recPick);
			content.AddChild(recRow);

			var cityRow = new HBoxContainer();
			cityRow.AddThemeConstantOverride("separation", 10);
			cityRow.AddChild(FormLabel("Town"));
			var cityPick = Option();
			cityPick.CustomMinimumSize = new Vector2(260, 36);
			foreach (MarketCity c in cities) cityPick.AddItem(c.name);
			cityRow.AddChild(cityPick);
			content.AddChild(cityRow);

			RenderTierRow("Size of push:", new[] { PlayerDesk.ProjectPromoTier.Small, PlayerDesk.ProjectPromoTier.Medium, PlayerDesk.ProjectPromoTier.Large },
				t => $"{t} ${PlayerDesk.ProjectPromoCost(t):N0}", t => projectPromoTier = t, projectPromoTier);

			var hirePromo = Btn($"HIRE PROMO MAN (${PlayerDesk.ProjectPromoCost(projectPromoTier):N0})");
			hirePromo.CustomMinimumSize = new Vector2(260, 42);
			hirePromo.Pressed += () => Act(() => {
				RecordRuntimeData r = released[Mathf.Clamp(recPick.Selected, 0, released.Count - 1)];
				MarketCity c = cities[Mathf.Clamp(cityPick.Selected, 0, cities.Count - 1)];
				bool ok = PlayerDesk.Instance.HireProjectPromo(r.baseRecord.recordId, c.cityId, projectPromoTier, out string message);
				Say(message, ok);
				return true;
			});
			content.AddChild(hirePromo);
		}
	}

	private static string CallReasonText(PlayerDesk.InboundCallReason reason) => reason switch {
		PlayerDesk.InboundCallReason.SoldOut => "sold out, wants more",
		PlayerDesk.InboundCallReason.StationAdded => "it's on the air there",
		PlayerDesk.InboundCallReason.Requests => "getting requests for it",
		PlayerDesk.InboundCallReason.AdjacentCity => "heard about it from next door",
		PlayerDesk.InboundCallReason.PreOrder => "heard it on the acetate, wants some held for the pressing",
		_ => "called"
	};

	// ========================================================================
	// SMALL HELPERS
	// ========================================================================

	// Distribution is a long pipeline, so each stage is a collapsible section. The page opens only the
	// stages that have something to do right now; whatever the player toggles is remembered.
	private readonly Dictionary<string, bool> sectionOpen = new();

	private Button SectionHeading(string key, string title, bool defaultOpen, string help = null) {
		content = contentRoot;   // close the previous section: later rows belong to this one
		bool open = sectionOpen.TryGetValue(key, out bool chosen) ? chosen : defaultOpen;
		var header = Btn($"{(open ? "▾" : "▸")}  {title}");
		header.Alignment = HorizontalAlignment.Left;
		header.CustomMinimumSize = new Vector2(0, 36);
		header.TooltipText = open ? "Click to collapse this stage." : "Click to open this stage.";
		StyleBoxFlat Box(Color fill) => new() {
			BgColor = fill, BorderColor = Rust,
			BorderWidthBottom = 2, ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 4, ContentMarginBottom = 4
		};
		header.AddThemeStyleboxOverride("normal", formDepth > 0 ? WorkOrder.SectionBand() : Box(new Color("e4d09f")));
		header.AddThemeStyleboxOverride("hover", formDepth > 0 ? WorkOrder.SectionBand(true) : Box(new Color("f1e2b8")));
		header.AddThemeStyleboxOverride("pressed", formDepth > 0 ? WorkOrder.SectionBand() : Box(new Color("e4d09f")));
		header.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
		foreach (string name in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_focus_color" })
			header.AddThemeColorOverride(name, formDepth > 0 ? WorkOrder.FormInk : Rust);
		header.AddThemeFontSizeOverride("font_size", formDepth > 0 ? 15 : 18);
		if (formDepth > 0) header.AddThemeFontOverride("font", PaperTheme.SansBold);
		header.Pressed += () => {
			int keep = contentScroll.ScrollVertical;
			sectionOpen[key] = !open;
			Refresh();
			GetTree().CreateTimer(0.03).Timeout += () => { if (IsInstanceValid(contentScroll)) contentScroll.ScrollVertical = keep; };
		};
		if (help == null) contentRoot.AddChild(header);
		else {
			header.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			var headerRow = new HBoxContainer();
			headerRow.AddThemeConstantOverride("separation", 8);
			headerRow.AddChild(header);
			headerRow.AddChild(HelpBadge(help));
			contentRoot.AddChild(headerRow);
		}
		var body = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, Visible = open };
		body.AddThemeConstantOverride("separation", 10);
		contentRoot.AddChild(body);
		content = body;
		return header;
	}

	/// <summary>A single is waiting on vinyl, or a released one has sold out with no run on the way.</summary>
	private static bool NeedsPressing(PlayerDesk desk) {
		bool unpressedSingle = desk.Planned.Any(single => {
			string id = single.Master.Record.recordId;
			return desk.PressingOrderFor(id) == null && (desk.StockFor(id)?.TotalPressed ?? 0) == 0;
		});
		bool soldOut = desk.ReleasedRecords.Any(record => {
			string id = record.baseRecord.recordId;
			PlayerDesk.PressStock stock = desk.StockFor(id);
			return stock != null && stock.TotalPressed > 0 && stock.Remaining <= 0 && desk.PressingOrderFor(id) == null;
		});
		return unpressedSingle || soldOut;
	}

	private Label Heading(string text, string help = null) {
		var node = new Label { Text = text };
		node.AddThemeFontSizeOverride("font_size", 20);
		node.AddThemeFontOverride("font", PaperTheme.SansBold);
		node.AddThemeColorOverride("font_color", Rust);
		if (formDepth > 0) StyleFormHeading(node);
		if (help == null) { content.AddChild(node); return node; }
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 8);
		row.AddChild(node);
		row.AddChild(HelpBadge(help));
		content.AddChild(row);
		return node;
	}

	/// <summary>A small "?" that explains a section on hover, so the page can say what to do and the tooltip can say why.</summary>
	private static Control HelpBadge(string help) {
		var badge = new TipLabel {
			Text = "?", TooltipText = help, MouseFilter = Control.MouseFilterEnum.Stop,
			MouseDefaultCursorShape = Control.CursorShape.Help, SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
			HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
			CustomMinimumSize = new Vector2(24, 24)
		};
		badge.AddThemeFontSizeOverride("font_size", 15);
		badge.AddThemeColorOverride("font_color", Rust);
		badge.AddThemeStyleboxOverride("normal", new StyleBoxFlat {
			BgColor = new Color("f6ecd0"), BorderColor = Rust,
			BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
			CornerRadiusTopLeft = 12, CornerRadiusTopRight = 12, CornerRadiusBottomLeft = 12, CornerRadiusBottomRight = 12
		});
		return badge;
	}

	/// <summary>Rebuilds the page and puts the scroll back, for a toggle that opens a form in place.</summary>
	private void RefreshKeepingScroll() {
		int keep = contentScroll.ScrollVertical;
		Refresh();
		GetTree().CreateTimer(0.03).Timeout += () => { if (IsInstanceValid(contentScroll)) contentScroll.ScrollVertical = keep; };
	}


	private void Body(string text) {
		var node = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart };
		node.AddThemeFontSizeOverride("font_size", 16);
		node.AddThemeColorOverride("font_color", Ink);
		content.AddChild(node);
	}

	private Label FormLabel(string text) {
		var node = new Label { Text = text };
		node.AddThemeColorOverride("font_color", Ink);
		return node;
	}

	private static string MonthEndCashPreview(AILabel label, float cashAfterAction) {
		GameDate today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		int days = DateTime.DaysInMonth(today.year, today.month) - today.day + 1;
		float overhead = label?.GetMonthlyOverhead() ?? 0f;
		return $"  ·  ${overhead:N0} overhead due at month-end ({days} day{(days == 1 ? "" : "s")}), leaving {Money(cashAfterAction - overhead)}";
	}

	/// <summary>Directive §6: a one-stop takes no Pitch/Consign/Service -- it's "locked as a customer
	/// until inbound demand exists," then a warehouse visit, then a flat carton sale on COD/net terms.
	/// A distinct row shape from the walk-in Shop/Op accounts above, not a variant of theirs.</summary>
	private Control BuildOneStopRow(PlayerDesk.PlayerStop stop, List<(string RecordId, string Title, int OnHand)> onHand,
			OptionButton singlePick, HashSet<string> stopsWithCalls) {
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 10);
		bool hasCall = stopsWithCalls.Contains(stop.StopId);

		if (!stop.OneStopUnlocked) {
			var label = new Label {
				Text = (hasCall ? "    ☎ " : "    ") + $"{stop.DisplayName} — "
					+ (hasCall ? "heard about it from an account they serve" : "not yet acquainted; a known account has to bring you up"),
				CustomMinimumSize = new Vector2(520, 32)
			};
			label.AddThemeColorOverride("font_color", Ink);
			row.AddChild(label);
			if (hasCall) {
				var visit = Btn($"VISIT WAREHOUSE ({PlayerDesk.OneStopVisitHours}h)");
				visit.CustomMinimumSize = new Vector2(200, 32);
				visit.Pressed += () => Act(() => {
					bool ok = PlayerDesk.Instance.VisitOneStopWarehouse(stop.StopId, out string message);
					Say(message, ok);
					return true;
				});
				row.AddChild(visit);
			}
			return row;
		}

		var stopLabel = new Label {
			Text = $"    {stop.DisplayName} — {(stop.OneStopTrusted ? "net terms" : "COD only")}",
			CustomMinimumSize = new Vector2(300, 32)
		};
		stopLabel.AddThemeColorOverride("font_color", Ink);
		row.AddChild(stopLabel);

		SpinBox qty = Spin(1, PlayerDesk.OneStopCartonMax, 1, PlayerDesk.OneStopCartonDefault);
		row.AddChild(qty);

		var sell = Btn($"SELL CARTON ({PlayerDesk.OneStopVisitHours}h)");
		sell.CustomMinimumSize = new Vector2(160, 32);
		sell.Pressed += () => Act(() => {
			if (onHand.Count == 0) return false;
			(string recordId, _, _) = onHand[Mathf.Clamp(singlePick.Selected, 0, onHand.Count - 1)];
			bool ok = PlayerDesk.Instance.SellCartonToOneStop(stop.StopId, recordId, Mathf.RoundToInt((float)qty.Value), out string message);
			Say(message, ok);
			return true;
		});
		row.AddChild(sell);
		return row;
	}

	// Period-idiom plural for each account kind's expand header. Falls back to "<Kind>s" so a future
	// StopKind (racks -- directive §6, still open) reads sanely before anyone gets around to naming it here.
	private static string StopKindLabel(PlayerDesk.StopKind kind) => kind switch {
		PlayerDesk.StopKind.Shop => "Record Stores",
		PlayerDesk.StopKind.Op => "Jukebox Operators",
		PlayerDesk.StopKind.OneStop => "One-Stops",
		PlayerDesk.StopKind.Venue => "Church & Hop Tables",
		PlayerDesk.StopKind.Station => "Radio Stations",
		_ => kind + "s"
	};

	/// <summary>Directive §3.3: a Venue takes no Pitch/Consign/Service -- one verb, WorkTheHopTable, cash
	/// at the table with no ledger to show. A distinct row shape from the walk-in Shop/Op accounts, same
	/// reasoning as BuildOneStopRow above.</summary>
	private Control BuildVenueRow(PlayerDesk.PlayerStop stop, List<(string RecordId, string Title, int OnHand)> onHand,
			OptionButton singlePick) {
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 10);
		string relWord = stop.Relationship <= 0f ? "never worked" : stop.Relationship < 0.35f ? "known to you" : "a regular table";
		bool workedToday = PlayerDesk.Instance.HasWorkedStopToday(stop.StopId);

		var stopLabel = new Label {
			Text = $"    {stop.DisplayName} ({relWord})",
			CustomMinimumSize = new Vector2(400, 32)
		};
		stopLabel.AddThemeColorOverride("font_color", Ink);
		row.AddChild(stopLabel);
		if (workedToday) {
			var tomorrow = new Label { Text = "Worked today — available tomorrow.", AutowrapMode = TextServer.AutowrapMode.WordSmart };
			tomorrow.AddThemeColorOverride("font_color", Heard);
			row.AddChild(tomorrow);
		}

		string estHours = PlayerDesk.StopVisitEstimate(PlayerDesk.StopKind.Venue);
		var work = Btn($"WORK THE TABLE ({estHours})");
		work.Disabled = workedToday;
		work.CustomMinimumSize = new Vector2(170, 32);
		work.Pressed += () => Act(() => {
			if (onHand.Count == 0) return false;
			(string recordId, _, _) = onHand[Mathf.Clamp(singlePick.Selected, 0, onHand.Count - 1)];
			bool ok = PlayerDesk.Instance.WorkTheHopTable(stop.StopId, recordId, out string message);
			Say(message, ok);
			return true;
		});
		row.AddChild(work);

		// Directive §8: the record hop -- the act appears, an MC'd table moves several times what a
		// bare one does, and (win or lose the room) a jock trusted enough to book gets a real advocacy
		// swing out of it. BookRecordHop itself checks for a trusted-enough jock in this town.
		var hop = Btn($"BOOK A HOP ({PlayerDesk.RecordHopHours}h)");
		hop.CustomMinimumSize = new Vector2(170, 32);
		hop.Pressed += () => Act(() => {
			if (onHand.Count == 0) return false;
			(string recordId, _, _) = onHand[Mathf.Clamp(singlePick.Selected, 0, onHand.Count - 1)];
			bool ok = PlayerDesk.Instance.BookRecordHop(stop.StopId, recordId, out string message);
			Say(message, ok);
			return true;
		});
		row.AddChild(hop);
		return row;
	}

	/// <summary>Promo mechanic directive §4: the station stop -- no shelf, no balance, just the jock
	/// you can walk in on. Draws only from the promo picker's pool, never the sellable one.</summary>
	private Control BuildStationRow(PlayerDesk.PlayerStop stop, List<(string RecordId, string Title, int PromoOnHand)> promoOnHand,
			OptionButton promoPick) {
		var row = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		row.AddThemeConstantOverride("separation", 6);

		var stopLabel = new Label {
			Text = $"    {stop.DisplayName}",
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		stopLabel.AddThemeColorOverride("font_color", Ink);
		row.AddChild(stopLabel);
		var actions = new HFlowContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		actions.AddThemeConstantOverride("h_separation", 8);
		actions.AddThemeConstantOverride("v_separation", 6);
		row.AddChild(actions);

		string RecordId() => promoOnHand.Count == 0 ? null
			: promoOnHand[Mathf.Clamp(promoPick.Selected, 0, promoOnHand.Count - 1)].RecordId;

		var dropOff = Btn($"DROP OFF ({PlayerDesk.DropOffMinutes / 60}h)");
		dropOff.CustomMinimumSize = new Vector2(120, 32);
		dropOff.Disabled = promoOnHand.Count == 0;
		dropOff.Pressed += () => Act(() => {
			string recordId = RecordId();
			if (recordId == null) return false;
			bool ok = PlayerDesk.Instance.DropOffAtStation(stop.StopId, recordId, out string message);
			Say(message, ok);
			return true;
		});
		actions.AddChild(dropOff);

		var waitFor = Btn($"WAIT FOR HIM ({PlayerDesk.WaitForHimMinutes / 60}h)");
		waitFor.CustomMinimumSize = new Vector2(150, 32);
		waitFor.Disabled = promoOnHand.Count == 0;
		waitFor.Pressed += () => Act(() => {
			string recordId = RecordId();
			if (recordId == null) return false;
			RolodexCall call = PlayerDesk.Instance.WaitForHimAtStation(stop.StopId, recordId, out string message);
			Say(message, call != null);
			if (call != null) GoToTab(RolodexTab, PageRolodex); // the pitch opens in person -- take focus immediately
			return true;
		});
		actions.AddChild(waitFor);

		var leaveIt = Btn($"LEAVE W/ DESK (~{PlayerDesk.LeaveWithReceptionistMinutes}m)");
		leaveIt.CustomMinimumSize = new Vector2(150, 32);
		leaveIt.Disabled = promoOnHand.Count == 0;
		leaveIt.Pressed += () => Act(() => {
			string recordId = RecordId();
			if (recordId == null) return false;
			bool ok = PlayerDesk.Instance.LeaveWithReceptionist(stop.StopId, recordId, out string message);
			Say(message, ok);
			return true;
		});
		actions.AddChild(leaveIt);

		var survey = Btn($"SURVEY (~{PlayerDesk.AskSurveyMinutes}m, free)");
		survey.CustomMinimumSize = new Vector2(150, 32);
		survey.Pressed += () => Act(() => {
			bool ok = PlayerDesk.Instance.AskWhatsOnSurvey(stop.StopId, out string message);
			Say(message, ok);
			return true;
		});
		actions.AddChild(survey);

		return row;
	}

	// --- Styled interactive controls -------------------------------------------------------------
	// The default control theme paints text and selection highlights near-white, which vanishes on the
	// beige paper. These factories force dark ink across every state (normal / hover / pressed / focus)
	// so a highlighted option or a hovered button stays readable.

	// Buttons, OptionButtons and SpinBoxes carry the default control theme: light text on their own dark
	// stylebox, which reads fine on the beige page. Only the CheckBox is special -- it has no filled box,
	// so its label sits straight on the paper and must be dark in every state, or a hover/focus turns it
	// near-white and it vanishes (the "highlighting an option should not be white" note).
	private static Button Btn(string text) => new Button { Text = text };

	/// <summary>The one verb on a card that spends time or money: a solid oxblood button.</summary>
	private static Button Primary(string text) => new Button { Text = text, ThemeTypeVariation = PaperTheme.Primary };

	/// <summary>Tab / filter-chip state as real styles instead of alpha: the selected one is dark with
	/// light text, the others are light paper with full-contrast ink (alpha-faded text measured ~2:1).</summary>
	private static void StyleToggle(Button button, bool active) {
		StyleBoxFlat Box(Color fill) => new() {
			BgColor = fill, BorderColor = new Color("70552c"),
			BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
			CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4,
			ContentMarginLeft = 8, ContentMarginRight = 8, ContentMarginTop = 4, ContentMarginBottom = 4
		};
		Color normal = active ? new Color("4a3a24") : new Color("e9d8a8");
		Color hover = active ? normal : new Color("f6ecd0");
		Color text = active ? Paper : Ink;
		button.AddThemeStyleboxOverride("normal", Box(normal));
		button.AddThemeStyleboxOverride("hover", Box(hover));
		button.AddThemeStyleboxOverride("pressed", Box(normal));
		button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
		foreach (string name in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_focus_color" })
			button.AddThemeColorOverride(name, text);
	}

	/// <summary>The department tabs as real folder tabs: the open one is the card's own paper and overlaps its top
	/// edge so they read as one sheet; the rest sit lower in manila shade.</summary>
	internal static void StyleFolderTab(Button button, bool active) {
		Color fill = active ? Paper : new Color("c6a35f");
		Color hover = active ? fill : new Color("d6b676");
		int stagger = button.GetIndex() % 3;   // cut at three heights, like a filing folder
		button.AddThemeStyleboxOverride("normal", FolderTabStyle.Make(fill, active, 8f, stagger));
		button.AddThemeStyleboxOverride("hover", FolderTabStyle.Make(hover, active, 8f, stagger));
		button.AddThemeStyleboxOverride("pressed", FolderTabStyle.Make(fill, active, 8f, stagger));
		button.AddThemeStyleboxOverride("disabled", FolderTabStyle.Make(fill, active, 8f, stagger));
		button.AddThemeFontOverride("font", PaperTheme.Elite);
		button.AddThemeFontSizeOverride("font_size", 15);
		button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
		foreach (string name in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_focus_color", "font_disabled_color" })
			button.AddThemeColorOverride(name, Ink);
		button.ZIndex = active ? 1 : 0;
	}

	private static void StyleField(LineEdit edit) {
		var box = new StyleBoxFlat {
			BgColor = Paper, BorderColor = new Color("8a7048"),
			BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
			ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 4, ContentMarginBottom = 4
		};
		edit.AddThemeStyleboxOverride("normal", box);
		edit.AddThemeStyleboxOverride("focus", box);
		edit.AddThemeColorOverride("font_color", Ink);
		edit.AddThemeColorOverride("font_placeholder_color", Heard);
		edit.AddThemeColorOverride("caret_color", Ink);
	}

	private static ImageTexture checkOffIcon, checkOnIcon;

	/// <summary>Box icons drawn in the office's ink: an empty outlined box when off, a filled box with a tick
	/// when on. The stock glyphs read backwards on the paper (a filled square meant "off").</summary>
	private static void EnsureCheckIcons() {
		if (checkOffIcon != null) return;
		checkOffIcon = PaperTheme.CheckIcon(false);
		checkOnIcon = PaperTheme.CheckIcon(true);
	}

	private static CheckBox Check(string text, bool pressed) {
		EnsureCheckIcons();
		var c = new CheckBox { Text = text, ButtonPressed = pressed };
		c.AddThemeIconOverride("unchecked", checkOffIcon);
		c.AddThemeIconOverride("checked", checkOnIcon);
		c.AddThemeIconOverride("unchecked_disabled", checkOffIcon);
		c.AddThemeIconOverride("checked_disabled", checkOnIcon);
		c.AddThemeColorOverride("font_color", Ink);
		c.AddThemeColorOverride("font_hover_color", Ink);
		c.AddThemeColorOverride("font_pressed_color", Ink);
		c.AddThemeColorOverride("font_focus_color", Ink);
		c.AddThemeColorOverride("font_hover_pressed_color", Ink);
		return c;
	}

	private static OptionButton Option() => new OptionButton();

	private static SpinBox Spin(double min, double max, double step, double value) =>
		new SpinBox {
			MinValue = min, MaxValue = max, Step = step, Value = value,
			TooltipText = $"Type a value, then press Enter or leave the field to apply it. Arrow step: {step:G}. Range: {min:G}–{max:G}.",
			CustomMinimumSize = new Vector2(160, 34)
		};

	private Label FaintLine(string text) {
		var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill };
		label.AddThemeFontSizeOverride("font_size", 14);
		label.AddThemeColorOverride("font_color", Heard);
		return label;
	}

	/// <summary>One tune in a list: its name, a bar for how the hook sounded (with a fog band when the read is rough),
	/// and a few words. The words use the same cut points as the bar's colour.</summary>
	private Control SongRow(string left, float hook, float confidence, string character = "") {
		var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		row.AddThemeConstantOverride("separation", 10);
		var title = new Label { Text = left, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill };
		title.AddThemeFontSizeOverride("font_size", 15);
		title.AddThemeColorOverride("font_color", Ink);
		if (string.IsNullOrEmpty(character)) row.AddChild(title);
		else {
			// The song's archetype, mood and lyrical turn under its name, as in the catalogue and the studio pickers.
			var stack = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
			stack.AddThemeConstantOverride("separation", 0);
			stack.AddChild(title);
			var traits = FaintLine("      " + character);
			traits.AddThemeFontSizeOverride("font_size", 13);
			stack.AddChild(traits);
			row.AddChild(stack);
		}
		row.AddChild(new ReadBar().Set(hook, confidence));
		var words = new Label { Text = PolarPlayerPerception.DescribeHook(hook, confidence), CustomMinimumSize = new Vector2(190, 0) };
		words.AddThemeFontSizeOverride("font_size", 14);
		words.AddThemeColorOverride("font_color", ReadBar.ColorFor(hook).Darkened(0.2f));
		row.AddChild(words);
		return row;
	}

	/// <summary>The whole set in a sentence: how strong the hooks are and what a crowd would do with them. Reads the
	/// shape of the set (a showpiece, a steady hand, filler) rather than ranking it, and hedges when the read is rough.</summary>
	private static string SetSummary(IReadOnlyList<float> hooks, float confidence) {
		if (hooks == null || hooks.Count == 0) return "";
		float peak = hooks.Max(), mean = hooks.Average();
		// One tune is not a set: say what the one tune would do, and don't invent company for it.
		string body = hooks.Count == 1
			? peak >= 0.75f ? "the one tune you caught would stop a room."
				: peak >= 0.55f ? "the one tune you caught is a good one; a crowd would enjoy it."
				: peak >= 0.38f ? "the one tune you caught was a fair one." : "the one tune you caught wouldn't hold a crowd."
			: peak >= 0.75f && mean >= 0.55f ? "a standout number and good company around it. The room stays warm all night."
			: peak >= 0.75f ? "one number that would stop a room, with filler around it. The crowd wakes up once."
			: mean >= 0.55f ? "solid all the way through, with no single showstopper. Nobody leaves, and nobody hums it on the way out."
			: mean >= 0.38f ? "a fair set. Pleasant enough; the room never quite leans in."
			: "a weak set. Polite applause at best.";
		string lead = confidence >= 0.7f ? "" : confidence >= 0.45f ? "From what you caught, probably " : "A rough read, but maybe ";
		return lead.Length == 0 ? char.ToUpperInvariant(body[0]) + body[1..] : lead + body;
	}

	/// <summary>What a take's production sounds like, in the same buckets as the bar's colour.</summary>
	private static string SoundWords(float v) => v >= 0.75f ? "a label-quality sound" : v >= 0.55f ? "clean and full" : v >= 0.35f ? "serviceable" : "thin and rough";


	private static string Cap(string text) =>
		string.IsNullOrEmpty(text) ? text : char.ToUpperInvariant(text[0]) + text.Substring(1);

	private static string Words(string value) => string.IsNullOrEmpty(value) ? value
		: string.Concat(value.Select((character, index) => index > 0 && char.IsUpper(character) ? " " + character : character.ToString()));

	private static string CountWord(int count, string singular) => count == 1 ? singular : singular + "s";

	/// <summary>Dollars with the sign in front of the symbol: -$75, never $-75.</summary>
	private static string Money(float amount) => amount < 0f ? $"−${-amount:N0}" : $"${amount:N0}";

	// Cash in the header: green in the black, red in the red, ink at exactly nothing. Both inks are darkened from the
	// chart's rising/falling pair so they hold against the manila folder.
	private static readonly Color CashBlack = new("17501a"), CashRed = new("8f1f18");
	private static string CashMarkup(float cash) {
		string ink = cash > 0f ? CashBlack.ToHtml(false) : cash < 0f ? CashRed.ToHtml(false) : Ink.ToHtml(false);
		return $"[font_size=21][color=#{ink}]{Money(cash)}[/color][/font_size]";
	}

	private static string Hour12(int hour) {
		int h = ((hour + 11) % 12) + 1;
		return $"{h}{(hour < 12 || hour >= 24 ? "am" : "pm")}";
	}

	private static void Clear(Node parent) {
		foreach (Node child in parent.GetChildren()) { parent.RemoveChild(child); child.QueueFree(); }
	}
}
