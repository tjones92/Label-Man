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
	private Label titleLabel, statusLabel;
	private RichTextLabel clockLabel;   // BBCode: the whole block is bold and the cash figure carries its own colour
	private LabelCrest titleCrest;
	private Button redInkLabel;
	public Action MenuClosed;
	public Action StudioClosed;
	private bool studioMode;
	private string studioArtistId;
	private int studioSide;
	private Button nextUpLabel;
	private Label feedbackToastText, feedbackToastBadge;
	private WallClock deskClock;
	private TimeCard timeCard;
	private Control pageStain;   // the coffee ring overlay on the scrolled page
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
	private string polarCatalogSong;   // the catalog cover whose slip is open (TEACH, and the comparison under Polar)
	private string selectedStopId;     // the account whose counter slip is open; null = pick a default, "" = none
	private string selectedDriveCityId;   // the picked line in the destination list (and the map), or null
	// Whether the save/load menu is up (takes over the panel, like founding / game-over).
	private bool browsingSaves;
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
		if (browsingSaves && MenuClosed != null) { MenuClosed(); return; }
		if (studioMode && StudioClosed != null) { StudioClosed(); return; }
		if (UIManager.Instance != null) UIManager.Instance.isUIOpen = false;
		UIManager.Instance?.RefreshMainHud();
	}

	public void OpenGameMenu() {
		browsingSaves = true;
		Open();
	}

	public void OpenStudio(string artistId) {
		studioMode = true; browsingSaves = false;
		if (studioArtistId != artistId) { studioSide = 0; studioSongs.Clear(); studioCompare = studioReview = false; studioOpenTakes.Clear(); }
		studioArtistId = artistId;
		Open();
	}

	// ========================================================================
	// CHROME
	// ========================================================================

	private void BuildUi() {
		Theme = PaperTheme.Build();
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

		// The workday as objects on the wall, not a payroll sentence (period pass 2026-11-05).
		deskClock = new WallClock { Visible = false };
		header.AddChild(deskClock);
		timeCard = new TimeCard { Visible = false };
		header.AddChild(timeCard);
		header.AddChild(new Control { CustomMinimumSize = new Vector2(6, 0) });

		var close = Btn("CLOSE  ×");
		close.SizeFlagsVertical = SizeFlags.ShrinkCenter;
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
		clockLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		clockLabel.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		clockLabel.ClipContents = true;
		// Playtest 2026-11-04: the header was ~270px. It is now three rows -- the title with the clock, one line of date,
		// place, cash and acts with the wait buttons beside it, and NEXT UP. Stock moved to the Catalog tab.
		var alertRow = new HBoxContainer { CustomMinimumSize = new Vector2(0, 34) };
		alertRow.AddThemeConstantOverride("separation", 12);
		root.AddChild(alertRow);
		alertRow.AddChild(clockLabel);
		nextUpLabel = Btn("");
		nextUpLabel.CustomMinimumSize = new Vector2(0, 30);
		nextUpLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		nextUpLabel.Alignment = HorizontalAlignment.Left;
		// Playtest 2026-11-04: the most useful line on the desk was cut off with "…". It wraps to a second line instead.
		nextUpLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		nextUpLabel.TooltipText = "Open the department for the next suggested step.";
		nextUpLabel.AddThemeStyleboxOverride("normal", new StyleBoxFlat {
			BgColor = new Color("ead8ad"), BorderColor = new Color("8a7048"),
			BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
			ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 4, ContentMarginBottom = 4
		});
		nextUpLabel.AddThemeColorOverride("font_color", Ink);
		nextUpLabel.Pressed += OpenNextUp;
		redInkLabel = new Button { Flat = true, Visible = false, FocusMode = FocusModeEnum.None, MouseDefaultCursorShape = CursorShape.PointingHand };
		redInkLabel.AddThemeColorOverride("font_color", Rust);
		redInkLabel.AddThemeColorOverride("font_hover_color", Colors.Black);
		redInkLabel.AddThemeColorOverride("font_pressed_color", Rust);
		redInkLabel.AddThemeFontSizeOverride("font_size", 16);
		redInkLabel.Pressed += () => GoToTab(4, PageFinances);
		alertRow.AddChild(redInkLabel);

		// The last action's result is said once, on the pop-up slip; this line only keeps it for code that reads it back
		// (playtest 2026-11-04: the green header line repeated every slip word for word). It is never shown.
		statusLabel = new Label { Visible = false };
		alertRow.AddChild(statusLabel);

		// Passing time without working, so you can wait out the clock -- the clubs don't open till evening
		// and there's no other way to move the day forward from the desk.
		idleRow = new HBoxContainer();
		idleRow.AddThemeConstantOverride("separation", 8);
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
		alertRow.AddChild(idleRow);
		root.AddChild(nextUpLabel);

		tabs = new HBoxContainer();
		tabs.AddThemeConstantOverride("separation", -2);   // trapezoid tabs meet at the foot and open out at the shoulders
		root.AddChild(tabs);

		var paper = new PanelContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
		// The coffee ring is not part of the frame: it is drawn on the scrolled page below, so it moves with the paper.
		paper.AddThemeStyleboxOverride("panel", PaperStyleBox.Sheet(Paper, 26, 22, 6).Decorated(clip: true, ring: false, seed: 8));
		root.AddChild(paper);

		contentScroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
		paper.AddChild(contentScroll);
		// A right gutter keeps full-width buttons from running under the scrollbar.
		var contentGutter = new MarginContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
		contentGutter.AddThemeConstantOverride("margin_right", 14);
		contentScroll.AddChild(contentGutter);
		content = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		content.AddThemeConstantOverride("separation", 10);
		contentGutter.AddChild(content);
		contentRoot = content;
		// A mug was set down on some of the department's pages (not all), each in its own place. The ring is on the
		// scrolled page, over whatever paper is there, so it moves with the page; it never takes the mouse.
		var stain = pageStain = new Control { MouseFilter = MouseFilterEnum.Ignore, SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
		stain.Draw += () => {
			int tab = Mathf.Max(0, currentTab);
			if (tab % 3 == 1) return;
			float half = CoffeeStain.Size / 2f;
			float y = Mathf.Min(240f + (tab * 97) % 380, stain.Size.Y - half + 30f);
			if (y < half) return;   // a page too short to have been under a mug
			bool right = (tab * 7) % 2 == 0;
			float x = right ? stain.Size.X - half - 10f - (tab * 23) % 60 : half + 10f + (tab * 41) % 80;
			CoffeeStain.Draw(stain.GetCanvasItem(), new Vector2(x, y), 8 + tab * 13);
		};
		stain.Resized += stain.QueueRedraw;
		contentGutter.AddChild(stain);

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
		bool switching = index != currentTab;
		if (switching && contentScroll != null) tabScroll[currentTab] = contentScroll.ScrollVertical;
		currentTab = index;
		currentPage = page;
		managingArtistId = null;
		browsingCovers = false;
		// Leaving the tab hangs up: you cannot hold a man on the line while you go and read the books.
		if (index != RolodexTab) { PlayerDesk.Instance?.EndCall(PlayerDesk.Instance.ActiveCall); rolodexPitchRecordId = null; }
		Refresh();
		// Coming back to a tab puts you where you left it (open sections are remembered separately); a first visit opens at the top.
		if (switching) RestoreScroll(tabScroll.TryGetValue(index, out int kept) ? kept : 0);
	}

	private readonly Dictionary<int, int> tabScroll = new();

	private void RestoreScroll(int value) {
		if (contentScroll == null) return;
		contentScroll.ScrollVertical = value;
		GetTree().CreateTimer(0.03).Timeout += () => { if (IsInstanceValid(contentScroll)) contentScroll.ScrollVertical = value; };
	}

	// ========================================================================
	// REFRESH
	// ========================================================================

	private void Refresh() {
		if (!Visible) return;
		PlayerDesk desk = PlayerDesk.Instance;
		ShowPendingSigningMoment(desk);
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
		nextUpLabel.Visible = inGame;
		if (!inGame) redInkLabel.Visible = false;
		deskClock.Visible = timeCard.Visible = false;   // the in-game desk shows them again below

		if (browsingSaves) {
			tabs.Hide();
			titleLabel.Text = "GAME MENU  •  ESC";
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
		if (studioMode) {
			tabs.Hide(); idleRow.Hide(); nextUpLabel.Hide(); redInkLabel.Hide();
			titleLabel.Text = "THE STUDIO";
			clockLabel.Text = $"{time?.CurrentDate.ToHeadlineString()}  •  {time?.GetTimeString()}  •  {CashMarkup(desk.Label.cashReserves)} cash";
			Clear(content); RenderStudioWindow(desk); return;
		}
		if (idleRow != null) idleRow.Visible = !desk.InStudio;
		// The idle row is built once, so the evening button has to be re-read from the clock every refresh.
		if (waitEveningButton != null) {
			bool eveningStarted = (time?.CurrentHour ?? 9) >= 17;
			waitEveningButton.Disabled = eveningStarted;
			waitEveningButton.Text = eveningStarted ? "EVENING'S HERE" : "WAIT FOR EVENING";
			waitEveningButton.TooltipText = eveningStarted ? "Evening scouting hours have begun." : "Pass time until 5 PM, when clubs begin opening.";
		}

		if (tabButtons.Count == 0) {
			BuildTabs(); currentTab = 0; currentPage = PageAandR; tabScroll.Clear();
			// The founding form was a long scroll; the first desk opens at the top of A&R, not wherever that left off.
			Callable.From(() => { if (IsInstanceValid(contentScroll)) contentScroll.ScrollVertical = 0; }).CallDeferred();
		}
		tabs.Visible = true;
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
		// The workday lives on the wall clock and the time card beside CLOSE; the line keeps only the date and time.
		bool overtime = time?.RegularTimeRemainingText is not { } regularLeft || regularLeft == "0m";
		if (time != null) {
			deskClock.Set(time.CurrentHour, time.CurrentMinute, time.RegularWorkdayEndHour);
			timeCard.Set(ShortClock(time.WorkdayStartHour), ShortClock(time.RegularWorkdayEndHour), ShortClock(time.HardStopHour),
				overtime ? $"{time.OvertimeRemainingText} O.T. left" : $"{time.RegularTimeRemainingText} left");
		}
		deskClock.Visible = timeCard.Visible = time != null;
		// One line: date and time, where, cash, acts. The full place and towns worked are on the hover.
		string place = desk.AtHome ? $"{(string.IsNullOrEmpty(label.headquartersCity) ? region : label.headquartersCity)} office" : $"on the road, {desk.CurrentCity?.name ?? "town"}";
		clockLabel.Text =
			$"{time?.CurrentDate.DayName[..3]} {time?.CurrentDate.ToHeadlineString()}  {time?.GetTimeString()}" + (overtime ? " (overtime)" : "") +
			$"  |  {place}  |  {CashMarkup(label.cashReserves)}  |  {label.CurrentRosterSize}/{label.maxRosterSize} acts";
		deskClock.TooltipText = timeCard.TooltipText = time == null ? string.Empty
			: $"{time.GetTimeString()}. {time.RegularTimeRemainingText} regular until {time.RegularWorkdayEndTime}, then {time.OvertimeRemainingText} overtime to {time.HardStopTime}. " +
			  "There is no overtime surcharge. Each action must finish by the hard stop.";
		clockLabel.TooltipText = time == null ? string.Empty
			: $"{time.CurrentDate.ToLongString()}, {where}. {Plural.Of(desk.WorkedCities.Count(), "town")} worked.\n" +
			  $"{time.RegularTimeRemainingText} regular until {time.RegularWorkdayEndTime}, then {time.OvertimeRemainingText} overtime to {time.HardStopTime} ({time.GetDayStatus()}). " +
			  $"Overtime can carry jobs to the {time.HardStopTime} hard stop. There is no overtime surcharge. Each action must finish by the hard stop.";
		// A running tab is survivable, but the bank is watching. Spell out the credit line and the clock on it.
		redInkLabel.Visible = label.cashReserves < 0f;
		redInkLabel.Text = label.cashReserves < 0f ? $"⚠ IN THE RED {desk.MonthsInTheRed}/3" : string.Empty;
		redInkLabel.TooltipText = label.cashReserves < 0f
			? $"Cash is {Money(label.cashReserves)}; the overdraft ceiling is ${-desk.CreditFloor:N0} -- a limit, not money you can borrow. {PlayerDesk.SolvencyRuleText} {Plural.Of(desk.MonthsOfGraceLeft, "red check")} {(desk.MonthsOfGraceLeft == 1 ? "remains" : "remain")}. Click for the books and CASH OPTIONS: what can bring money in, when, and how sure it is."
			: string.Empty;

		Clear(content);
		(currentPage ?? PageAandR)();
		if (pageStain != null) pageStain.Modulate = Colors.White;   // the founding pages fade it; the office doesn't
		pageStain?.QueueRedraw();   // the page's coffee ring belongs to the open tab
		UpdateTabBadges(desk);
	}

	/// <summary>What is on the shelf, record by record, for the top of the Catalog tab (it used to be a header row on
	/// every tab). Low and sold-out records are flagged in rust.</summary>
	private void StockLine(PlayerDesk desk) {
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
		// Before the first pressing there is no stock to report; the line arrives with the records.
		if (stockNotes.Count == 0) return;
		bool short_ = stockNotes.Any(note => note.Contains("LOW", StringComparison.Ordinal) || note.Contains("SOLD OUT", StringComparison.Ordinal));
		var line = new Label { Text = "ON THE SHELF  •  " + string.Join("   |   ", stockNotes), AutowrapMode = TextServer.AutowrapMode.WordSmart,
			SizeFlagsHorizontal = SizeFlags.ExpandFill };
		line.AddThemeFontSizeOverride("font_size", 15);
		line.AddThemeColorOverride("font_color", short_ ? Rust : HeaderFade);
		content.AddChild(line);
	}

	/// <summary>An hour as a time card prints it: "9:00", "6:00", no AM/PM.</summary>
	private static string ShortClock(int hour) { int h = hour % 12; return $"{(h == 0 ? 12 : h)}:00"; }

	private void UpdateTabBadges(PlayerDesk desk) {
		for (int i = 0; i < tabButtons.Count; i++) {
			string title = tabTitles[i];
			// The open tab never wears a badge: the player is already looking at what it would announce.
			int count = i == currentTab && title is "LEDGER" or "ROLODEX" ? 0 : title switch {
				"LEDGER" => desk.UnreadLogCount,
				"OFFICE" => desk.PendingCalls().Count(),
				"CATALOG" => desk.PendingPressings().Count(),
				"BAND ROOM" => desk.PendingVisitCount,
				"ROLODEX" => desk.ActiveCall != null && desk.ActiveCall.stage != CallStage.Ended ? 1 : 0,
				_ => 0
			};
			tabButtons[i].Text = title + TabStamp.Room(count, title == "LEDGER");
			tabButtons[i].TooltipText = count <= 0 ? "" : title == "LEDGER" ? $"{Plural.Of(count, "new entry")} on the spindle." : $"{count} waiting.";
			TabStamp.Apply(tabButtons[i], count, asSpindle: title == "LEDGER");
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
		if (label.cashReserves < 0f) return ("Cash is tight. See CASH OPTIONS on the books: what can bring money in, when, and how sure it is.", 4);
		if (label.CurrentRosterSize == 0) return (desk.ScoutingDossiers.Count > 0 ? "Discuss terms with a heard act, or choose another announced set." : "Hear an announced set. Observations go into your notebook automatically.", 0);
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
		// Only for a record still waiting on vinyl: once the pressing is in the office, the real 45 does the acetate's job.
		// (Playtest 2026-11-04: NEXT UP offered a $20 acetate for a record already pressed and sitting in the office.)
		bool AwaitingVinyl(PlayerDesk.Master master) => !master.Released && desk.StockFor(master.Record?.recordId) == null;
		if (desk.Masters.Any(master => AwaitingVinyl(master) && desk.AcetatesFor(master.Record?.recordId) > 0))
			options.Add("play your acetate down the phone to a DJ, or hand it to one in person");
		else if (desk.Label.cashReserves >= 20f && desk.Masters.Any(AwaitingVinyl))
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
		if (desk.Session != null) { UIManager.Instance?.OpenStudio(desk.Session.ArtistId); return; }
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

	// The founding page itself lives in PlayerDeskPanel.Founding.cs.

	/// <summary>What it is like to run a label out of this town, in plain words: the rent, the rivals, the
	/// studios, the shipping and the shops at home. Every figure is the one the game will actually use.</summary>
	private static string TownFacts(MarketCity city, bool includeOffice = true) {
		CityProfile profile = CityProfiles.Get(city.cityId);
		float quality = PlayerDesk.StudioQualityIn(city.cityId, city.parentRegionId);
		float mid = PlayerDesk.StudioHourlyRateIn(city.cityId, city.parentRegionId, PlayerDesk.StudioTier.Mid);
		float budget = PlayerDesk.StudioHourlyRateIn(city.cityId, city.parentRegionId, PlayerDesk.StudioTier.Budget);
		float top = PlayerDesk.StudioHourlyRateIn(city.cityId, city.parentRegionId, PlayerDesk.StudioTier.Top);
		return $"{profile.Character}\n\n" +
			(includeOffice ? $"THE OFFICE  ·  Rent is ${profile.Rent:N0} a month, ${CityProfiles.MonthlyOverhead(city.cityId):N0} all in with the phone and postage.\n" : "") +
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

	// ========================================================================
	// SAVE / LOAD
	// ========================================================================

	private void PageSaves(PlayerDesk desk) {
		var back = Btn("RESUME  •  ESC");
		back.CustomMinimumSize = new Vector2(160, 36);
		back.Pressed += ClosePanel;
		content.AddChild(back);
		var exit = Btn("EXIT GAME");
		exit.Pressed += () => {
			var confirm = PaperModal.Open(this, "EXIT GAME?", 540);
			confirm.AddText("Exit to the desktop? Progress since your last save will be lost.");
			confirm.AddButton("STAY HERE", null);
			confirm.AddButton("EXIT GAME", () => GetTree().Quit(), PaperModal.ButtonKind.Danger);
		};
		content.AddChild(exit);

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
			load.Pressed += () => {
				bool ok = SaveGameService.Load(slot, out string message); Say(message, ok);
				if (ok) { UIManager.Instance?.OnClick_CloseAll(); ClosePanel(); } else Refresh();
			};
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
		load.Pressed += () => UIManager.Instance?.OpenGameMenu();
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
		var scoutingTabs = new HFlowContainer();
		scoutingTabs.AddThemeConstantOverride("h_separation", 6);
		foreach (string view in new[] { "FIND A SET", "ACT NOTEBOOK" }) {
			var tab = ChoiceButton(view, scoutingView == view, () => { scoutingView = view; Refresh(); });
			tab.CustomMinimumSize = new Vector2(150, 36);
			scoutingTabs.AddChild(tab);
		}
		content.AddChild(scoutingTabs);
		if (scoutingView == "ACT NOTEBOOK") { ScoutingActsPage(desk); return; }
		// A first-day player needs the one idea the page is built on; after the first listen it is a plain prompt.
		bool firstListen = desk.ScoutingDossiers.Count == 0 && !desk.Roster.Any();
		Body(LocalScenes.Rooms
            ? firstListen
				? "Every signing starts in a club. Go and hear a set: what you notice is saved to your ACT NOTEBOOK, and if you like an act, you talk to them after their set."
				: "Pick a set to hear, or browse the venues' posted calendars."
            : $"Pick a room and go hear who's playing it. Costs {PlayerDesk.ScoutHours} hours, and each room only draws a crowd at its own hours. Your ear shapes the impression; judging the act is your decision.");
		if (LocalScenes.Rooms) SceneRoomsBoard(desk);
        else {
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
		scout.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.ScoutVenue(selectedVenue, out string message); if (ok) scoutingView = "ACT NOTEBOOK"; Say(message, ok); return ok; });
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
        }
	}

	private string scoutingView = "FIND A SET";
	private void ScoutingActsPage(PlayerDesk desk) {
		ScoutingMeetingAgenda(desk);
		if (desk.Slate.Count == 0 && desk.ScoutingDossiers.Count == 0) Body("Hear a set to start your notebook. Every act you hear is saved here automatically.");
		else {
			foreach (PlayerDesk.Prospect prospect in desk.Slate.ToList())
				if (!desk.ScoutingDossiers.Any(d => d.ArtistId == prospect.Artist.artistId)) ProspectCard(prospect);
		}

		int scoutingActs = desk.ScoutingDossiers.Count(d => !SignedByPlayer(desk, d.ArtistId));
		Heading($"A&R NOTEBOOK  ({scoutingActs} {(scoutingActs == 1 ? "act" : "acts")})");
		ScoutingNotebook(desk);
		foreach (PlayerDesk.WatchNote entry in desk.Notebook.ToList()) {
            if (desk.ScoutingDossiers.Any(d => d.ArtistId == entry.Artist?.artistId)) continue;
			SimulatedArtist artist = entry.Artist;
			if (artist == null) continue;
			bool signedByYou = !string.IsNullOrEmpty(artist.labelId) && artist.labelId == desk.Label?.labelId;
			if (signedByYou) continue;
			bool signedElsewhere = !string.IsNullOrEmpty(artist.labelId) && !signedByYou;
			string labelName = signedElsewhere ? ChartManager.Instance?.GetLabelName(artist.labelId) ?? "another label" : null;
			GameDate today = TimeManager.Instance?.CurrentDate ?? entry.LastSeen;
			int ageDays = Mathf.Max(0, (new DateTime(today.year, today.month, today.day) - new DateTime(entry.LastSeen.year, entry.LastSeen.month, entry.LastSeen.day)).Days);
			string freshness = ageDays == 0 ? "seen today" : $"last seen {ageDays} days ago";
			if (ageDays >= 30) freshness += "  •  stale read";
			string status = signedElsewhere ? $"\n    RIVAL SIGNED THEM: {labelName}."
				: signedByYou ? "\n    SIGNED — they're on your roster now."
				: entry.HeldUntil.HasValue ? $"\n    HANDSHAKE: they'll wait for you until {entry.HeldUntil.Value.ToHeadlineString()}."
				: desk.RivalOfferLine(artist.artistId) is string offerLine ? "\n    RIVAL OFFER: " + offerLine
				: desk.RivalInterestLine(artist.artistId) is string interest ? "\n    RIVAL INTEREST: " + interest
				: entry.CirclingResolves.HasValue ? $"\n    {entry.CirclingLabel ?? "A rival"} is circling them — the deal lands {entry.CirclingResolves.Value.ToHeadlineString()} unless you step in."
				: "";
			string potential = entry.Rough?.LastOrDefault();
			Body($"{artist.stageName} — {GenreNameFormatter.Format(artist.primaryGenre)}  •  {freshness}  •  {entry.Note}" +
				$"{(potential != null ? "\n    " + potential : "")}{status}");
			var actions = new HBoxContainer();
			actions.AddThemeConstantOverride("separation", 10);
			// A signed act can't be brought back, so the button is left off rather than greyed (playtest 2026-11-04).
			if (!signedElsewhere) {
				var revisit = Btn("BRING BACK TO THE PAD");
				revisit.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.RevisitNotebookAct(artist.artistId, out string message); Say(message, ok); return ok; });
				actions.AddChild(revisit);
			}
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

    /// <summary>A heard act's evidence and next actions; ordinary cards expose no scalar verdict.</summary>
    private void ProspectCard(PlayerDesk.Prospect prospect, bool showEvidence = true, Action beforeConversationAction = null) {
        var desk = PlayerDesk.Instance;
        if (showEvidence) Heading(prospect.Artist.stageName.ToUpperInvariant());
        Body(GenreNameFormatter.Format(prospect.Artist.primaryGenre) + " · " + PlayerDesk.SceneDealDescription(prospect));
        if (showEvidence) foreach (string line in desk.ScoutingSummary(prospect.Artist.artistId)) Body(line);
        if (showEvidence) foreach (var song in prospect.LiveSet.Take(prospect.HeardCount)) Body($"    ♪ {song.Title} ({song.SourceTag})");
        var row = new HFlowContainer();
        if (prospect.SceneRoomId != null && showEvidence) {
            var follow = Btn("HEAR ANOTHER ANNOUNCED SET");
            follow.Pressed += () => Act(() => { bool ok = desk.FollowUp(prospect, out string result); Say(result, ok); return ok; }); row.AddChild(follow);
            var booker = Btn($"ASK ABOUT THIS ROOM'S BOOKING HISTORY ({PlayerDesk.BookerQuestionMinutes}min)");
            booker.Pressed += () => Act(() => { bool ok = desk.AskScoutingBooker(prospect.SceneRoomId, prospect.Artist.artistId, true, out string result); Say(result, ok); return ok; }); row.AddChild(booker);
        } else if (prospect.SceneRoomId == null && !prospect.FollowedUp) {
            var follow = Btn($"FOLLOW UP ({PlayerDesk.FollowUpHours}h)");
            follow.Pressed += () => Act(() => { bool ok = desk.FollowUp(prospect, out string result); Say(result, ok); return ok; }); row.AddChild(follow);
        }
        bool askFirst = desk.CanAskDirectApproach(prospect.Artist);
        var approach = Primary(askFirst ? "ASK IF THEY'D HEAR AN OFFER" : "DISCUSS TERMS");
        approach.Disabled = !askFirst && (!PlayerDesk.SceneDealAvailable(prospect) || (!prospect.FollowedUp && !desk.HasScoutingEvidence(prospect.Artist.artistId)));
        approach.TooltipText = askFirst ? "They aren't looking for a record deal. Their side may think it over and answer within about a week, or say no for this season. No money or time spent."
            : PlayerDesk.SceneDealDescription(prospect);
        GreyWhy(approach, PlayerDesk.SceneDealAvailable(prospect) ? "Hear them play first: terms are talked over once you have something to go on." : "They aren't ready to talk terms.");
        approach.Pressed += () => { beforeConversationAction?.Invoke(); bool ok = desk.ApproachToSign(prospect, out string result); if (ok) negotiating = prospect; Say(result, ok); Refresh(); };
        row.AddChild(approach); content.AddChild(row);
    }

	/// <summary>The contract menu: the label's opening offer, editable, then put on the table.
	/// Pushover only -- signing is a single accept-or-walk click.</summary>
	private void ContractForm(PlayerDesk.Prospect prospect) {
		ContractTermSheet b = prospect.Baseline;
		ContractTermSheet prefill = prospect.Draft ?? b;
		Heading($"CONTRACT — {prospect.Artist.stageName.ToUpperInvariant()}");
		if (!string.IsNullOrEmpty(b.DemandSummary))
			Body($"Their ask: {b.DemandSummary}");
		OutlookBlock(PlayerDesk.Instance.ContractOutlookLines(prospect.Artist, renewal: false));
		Body(prospect.PushoverCountered
			? "They've named their number. Put it forward again, or step back -- a second offer under it and they walk."
			: "They'll sign near their ask, but not at any price: go too low and they counter once, then walk. " +
				$"The meeting takes {PlayerDesk.PushoverSignHours} hours, signed or not, and the advance is charged when they sign.");

		TermsForm(prefill, $"OFFER CONTRACT  ({PlayerDesk.PushoverSignHours}h)",
			(advance, royalty, term, singles, labelPub, artistControl) => {
				var offer = new ContractTermSheet(advance, royalty, term, singles, labelPub, artistControl,
					b.NegotiationDifficulty, b.Manager, b.ManagerName, b.DemandSummary);
				SubmitEasyOffer(prospect, offer, sheet => {
					bool signed = PlayerDesk.Instance.OfferContract(prospect, sheet.Advance, sheet.RoyaltyRate, sheet.TermYears,
						sheet.SinglesObligation, sheet.LabelOwnsPublishing, sheet.ArtistCreativeControl, out string message);
					GameDate today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
					bool walked = !signed && prospect.CooldownUntil is GameDate until && today < until;
					if (signed || walked) negotiating = null;
					Say(message, signed);
					Refresh();
				});
			},
			() => { negotiating = null; Refresh(); }, b, prospect.Artist.stageName);
	}

	/// <summary>The editable grid shared by the plain contract form, every tabling round of a
	/// Firm/Hardball negotiation, and a renewal. Just the fields and the two buttons -- caller
	/// supplies the prefill, what the submit button says and does, and what "not now" does.</summary>
	private void TermsForm(ContractTermSheet prefill, string submitLabel,
			Action<float, float, int, int, bool, bool> onSubmit, Action onCancel, ContractTermSheet? ask = null, string artistName = null, bool renewal = false) {
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

		// Period pass 2026-11-05: the terms are typed into blanks on the agreement, not spun on number widgets.
		grid.AddChild(FormLabel("Advance"));
		// Step of 5, not 25 -- a coarse step silently snapped a typed $35 down to $25 on finalize.
		var advance = Spin(0, 100000, 1, Mathf.Round(prefill.Advance));
		grid.AddChild(TypedBlank(advance, "$", ""));

		grid.AddChild(FormLabel("Royalty"));
		// Opens on what they expect. You may write it lower -- down to half a point -- but the further
		// under their number you go, the likelier they push back on it.
		var royalty = Spin(PlayerDesk.PlayerRoyaltyFloor * 100f, 15, 0.25,
			Mathf.Round(prefill.RoyaltyRate * 4000f) / 40f);
		grid.AddChild(TypedBlank(royalty, "", "%"));

		grid.AddChild(FormLabel("Term"));
		var term = Spin(1, 7, 1, prefill.TermYears);
		grid.AddChild(TypedBlank(term, "", "years"));

		grid.AddChild(FormLabel("Deliverables"));
		// 2-3 singles a year is the period norm for a new act, tapering to none once a career is
		// established -- the default already reflects that; this just lets the player move off it.
		var singles = Spin(0, 30, 1, prefill.SinglesObligation);
		grid.AddChild(TypedBlank(singles, "", "singles"));

		var publishingLabel = FormLabel("Publishing");
		grid.AddChild(publishingLabel);
		var labelPub = TypedCheck("Label keeps the publishing", prefill.LabelOwnsPublishing);
		grid.AddChild(labelPub);

		var controlLabel = FormLabel("Creative control");
		grid.AddChild(controlLabel);
		var artistControl = TypedCheck("Artist has creative control", prefill.ArtistCreativeControl);
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
			lowball.Text = $"{under:P0} under their ask. If they accept, the concession may affect a later renewal.";
		}
		advance.ValueChanged += _ => UpdateLowball();
		royalty.ValueChanged += _ => UpdateLowball();
		UpdateLowball();

		// What this advance does to the label: a ruled ledger of short rows, with the fine print set small underneath.
		form.AddChild(new ColorRect { Color = new Color(carbonInk, 0.35f), CustomMinimumSize = new Vector2(0, 1), MouseFilter = MouseFilterEnum.Ignore });
		form.AddChild(Typed("WHAT THIS COSTS THE LABEL", 14,carbonInk, true));
		var commitment = new VBoxContainer { Name = "OfferResourcePlan", SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Stop };
		commitment.AddThemeConstantOverride("separation", 6);
		form.AddChild(commitment);
		Label Wrapped(string text, int size, Color color) {
			var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(120, 0) };
			label.AddThemeFontSizeOverride("font_size", size);
			label.AddThemeColorOverride("font_color", color);
			return label;
		}
		void UpdateCommitment() {
			var plan = PlayerDesk.Instance?.PreviewOfferResources((float)advance.Value, renewal);
			foreach (var old in commitment.GetChildren()) { commitment.RemoveChild(old); old.QueueFree(); }
			if (plan == null) { commitment.AddChild(Wrapped("No label cash available.", 15, Rust)); commitment.TooltipText = ""; return; }
			var ledger = new GridContainer { Columns = 2, SizeFlagsHorizontal = SizeFlags.ExpandFill };
			ledger.AddThemeConstantOverride("h_separation", 18);
			ledger.AddThemeConstantOverride("v_separation", 6);
			foreach (var row in plan.Rows) {
				var rubric = Wrapped(row.Rubric, 15,row.Warn ? Rust : new Color("45381f"));
				rubric.SizeFlagsHorizontal = SizeFlags.Fill; rubric.CustomMinimumSize = new Vector2(170, 0);
				ledger.AddChild(rubric);
				ledger.AddChild(Wrapped(row.Value, 15, row.Warn ? Rust : Ink));
			}
			commitment.AddChild(ledger);
			// Early-game §4.1: the release route this advance leaves, past the one-session estimate in the ledger.
			string completion = PlayerDesk.Instance.CompletionWarning((float)advance.Value + plan.PendingAppointmentFees, PlayerDesk.CompletionStageKind.Signing);
			if (completion != null) commitment.AddChild(Wrapped(completion, 15, Rust));
			commitment.AddChild(Wrapped(plan.FinePrint, 13, new Color("6a5c42")));
			commitment.TooltipText = string.Join("\n", plan.FeeDetails);
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
		OutlookBlock(talk.outlookStated);

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
		Body($"Their ask: ${talk.ask.Advance:N0}, {talk.ask.RoyaltyRate:P1}, {Plural.Of(talk.ask.TermYears, "year")}, " +
			$"{Plural.Of(talk.ask.SinglesObligation, "single")}{(talk.ask.LabelOwnsPublishing ? "" : ", they keep the publishing")}{(talk.ask.ArtistCreativeControl ? ", they hold creative control" : "")}. " +
			$"Each table round costs {PlayerDesk.NegotiationRoundHours} hours.");
		Body(talk.draftOffer.HasValue ? "The last meeting could not be held; your entered terms are still below." :
			talk.roundsPlayed == 0 ? "These fields start on your label's standard paper: you keep the publishing and have the final word. Their ask is above; every give you tick is a concession." : "These fields start with your last offer. Change any term before putting it back on the table.");
		if (talk.sessionPromised) {
			Body($"Your offer includes a funded studio session within {PlayerDesk.SessionPromiseDays / 7} weeks of signing — about " +
				$"${PlayerDesk.Instance.SessionPromiseQuote():N0} at the cheapest room, paid when you book it. Signing needs cash for the advance and the session.");
			var dropPromise = Btn("DROP THE SESSION PROMISE");
			dropPromise.Name = "DropSessionPromise";
			dropPromise.Pressed += () => { bool ok = PlayerDesk.Instance.WithdrawSessionPromise(talk, out string result); Say(result, ok); Refresh(); };
			content.AddChild(dropPromise);
		} else {
			var addPromise = Btn("ADD A SESSION PROMISE");
			addPromise.Name = "AddSessionPromise";
			addPromise.TooltipText = $"Commit to book and pay for studio time with them within {PlayerDesk.SessionPromiseDays / 7} weeks of signing " +
				$"(about ${PlayerDesk.Instance.SessionPromiseQuote():N0} at the cheapest room). Miss it and they'll remember at renewal.";
			addPromise.Pressed += () => { bool ok = PlayerDesk.Instance.AddSessionPromise(talk, out string result); Say(result, ok); Refresh(); };
			content.AddChild(addPromise);
		}
		if (PlayerDesk.Instance.RivalOfferLine(talk.Artist.artistId) is string rivalPaper)
			Body("OTHER PAPER: " + rivalPaper + " Your package has to beat theirs, counting what each label can do for them.");
		if (talk.distributionPromised) {
			Body($"Your offer includes a wholesale house carrying their first record within {PlayerDesk.DistributionPromiseDays / 7} weeks of its release. " +
				$"Place a line (or sign a distribution deal) before then. The record itself has to be out within {PlayerDesk.PromotionReleaseDeadlineDays / 7} weeks of signing.");
			var dropLine = Btn("DROP THE DISTRIBUTION PROMISE");
			dropLine.Name = "DropDistributionPromise";
			dropLine.Pressed += () => { bool ok = PlayerDesk.Instance.WithdrawDistributionPromise(talk, out string result); Say(result, ok); Refresh(); };
			content.AddChild(dropLine);
		} else {
			var addLine = Btn("ADD A DISTRIBUTION PROMISE");
			addLine.Name = "AddDistributionPromise";
			addLine.TooltipText = DistributionPromiseHelp();
			addLine.Pressed += () => { bool ok = PlayerDesk.Instance.AddDistributionPromise(talk, out string result); Say(result, ok); Refresh(); };
			content.AddChild(addLine);
		}
		string ad = PlayerDesk.TradeAdTierName(PlayerDesk.PromotionPromiseTier).ToLowerInvariant();
		if (talk.promotionPromised) {
			Body($"Your offer includes a {ad} trade ad for their first record within {PlayerDesk.PromotionPromiseDays / 7} weeks of its release — " +
				$"${PlayerDesk.PromotionPromiseQuote():N0}, paid when you buy it. The record itself has to be out within {PlayerDesk.PromotionReleaseDeadlineDays / 7} weeks of signing.");
			var dropPush = Btn("DROP THE PROMOTION PROMISE");
			dropPush.Name = "DropPromotionPromise";
			dropPush.Pressed += () => { bool ok = PlayerDesk.Instance.WithdrawPromotionPromise(talk, out string result); Say(result, ok); Refresh(); };
			content.AddChild(dropPush);
		} else {
			var addPush = Btn("ADD A PROMOTION PROMISE");
			addPush.Name = "AddPromotionPromise";
			addPush.TooltipText = PromotionPromiseHelp();
			addPush.Pressed += () => { bool ok = PlayerDesk.Instance.AddPromotionPromise(talk, out string result); Say(result, ok); Refresh(); };
			content.AddChild(addPush);
		}
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
			}, talk.ask, talk.Artist.stageName, talk.IsRenewal);
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
        var stand = new Label {
            Text = talk.log.FirstOrDefault() ?? "They have not accepted this package. Consider their stated objection and counterterms.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
		stand.AddThemeColorOverride("font_color", Heard);
		content.AddChild(stand);

		NegotiationCounterButton(talk, ContractCounter.SweetenAxis,
			"SWEETEN IT", talk.capabilityObjection.HasValue
				? "It isn't one number — it's what your label can't do yet. Better terms can still make up for it."
				: "Go back and raise the number he's actually stuck on.");
		if (PlayerDesk.CanTradeAxes(talk))
			NegotiationCounterButton(talk, ContractCounter.TradeAxes,
				"TRADE", "Give back the publishing and the final word; the advance comes down to match.");
		if (!talk.sessionPromised)
			NegotiationCounterButton(talk, ContractCounter.Promise, "PROMISE A SESSION",
				$"Commit to book and pay for studio time with them within {PlayerDesk.SessionPromiseDays / 7} weeks of signing " +
				$"(about ${PlayerDesk.Instance.SessionPromiseQuote():N0} at the cheapest room, paid when you book). Miss it and they'll remember at renewal.");
		if (!talk.promotionPromised)
			NegotiationCounterButton(talk, ContractCounter.PromisePromotion, "PROMISE A PUSH", PromotionPromiseHelp());
		if (!talk.distributionPromised)
			NegotiationCounterButton(talk, ContractCounter.PromiseDistribution, "PROMISE DISTRIBUTION", DistributionPromiseHelp());
		NegotiationCounterButton(talk, ContractCounter.HoldFirm,
			$"HOLD FIRM  ({PlayerDesk.NegotiationRoundHours}h)", "Table it again unchanged and see who blinks.");
		NegotiationCounterButton(talk, ContractCounter.Walk,
			"WALK AWAY", "Step back from the table. Nothing's burned.");
	}

	private static string DistributionPromiseHelp() =>
		$"Commit to get their first record carried by a wholesale house -- a line you place, or a distribution deal -- within {PlayerDesk.DistributionPromiseDays / 7} weeks " +
		$"of its release, and get that record out within {PlayerDesk.PromotionReleaseDeadlineDays / 7} weeks of signing. Only a label with no line yet can promise it. " +
		"Miss it and they'll remember at renewal.";

	private static string PromotionPromiseHelp() =>
		$"Commit to a {PlayerDesk.TradeAdTierName(PlayerDesk.PromotionPromiseTier).ToLowerInvariant()} trade ad (${PlayerDesk.PromotionPromiseQuote():N0}) for their first record " +
		$"within {PlayerDesk.PromotionPromiseDays / 7} weeks of its release, and get that record out within {PlayerDesk.PromotionReleaseDeadlineDays / 7} weeks of signing. " +
		"Miss it and they'll remember at renewal.";

	/// <summary>What the act says it is weighing, and the player's own label in plain facts. Never a score.</summary>
	private void OutlookBlock(IEnumerable<string> lines) {
		var said = lines?.Where(l => !string.IsNullOrWhiteSpace(l)).ToList() ?? new List<string>();
		string own = PlayerDesk.Instance?.OwnLabelSummary();
		if (said.Count == 0 && string.IsNullOrEmpty(own)) return;
		var box = new VBoxContainer { Name = "ContractOutlook" };
		box.AddThemeConstantOverride("separation", 4);
		var head = new Label { Text = "WHAT THEY'RE WEIGHING" };
		head.AddThemeFontSizeOverride("font_size", 14);
		head.AddThemeColorOverride("font_color", Ink);
		box.AddChild(head);
		foreach (string line in said) {
			var lbl = new Label { Text = "•  " + line, AutowrapMode = TextServer.AutowrapMode.WordSmart };
			lbl.AddThemeFontSizeOverride("font_size", 15);
			lbl.AddThemeColorOverride("font_color", Heard);
			box.AddChild(lbl);
		}
		if (!string.IsNullOrEmpty(own)) {
			var lbl = new Label { Name = "OwnLabelSummary", Text = own, AutowrapMode = TextServer.AutowrapMode.WordSmart };
			lbl.AddThemeFontSizeOverride("font_size", 14);
			lbl.AddThemeColorOverride("font_color", Ink);
			box.AddChild(lbl);
		}
		content.AddChild(box);
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
        if (desk.PendingRenewal is { } pending) {
            Body("Renewal discussion retained for " + pending.Artist.stageName + ".");
            if (desk.RenewalDiscussionUnavailable() is { } reason) Body(reason);
            var resumeRenewal = Btn("RESUME RENEWAL DISCUSSION");
            resumeRenewal.Pressed += () => { renewingArtist = pending.Artist; managingArtistId = null; Refresh(); };
            content.AddChild(resumeRenewal);
            var dismissRenewal = Btn("STEP BACK FROM RENEWAL");
            dismissRenewal.Pressed += () => { bool ok = desk.StepBackFromRenewal(out string result); Say(result, ok); Refresh(); };
            content.AddChild(dismissRenewal);
        }

		// Clicking an act opens its MANAGE window in place of the list.
		SimulatedArtist managed = managingArtistId == null ? null
			: desk.Roster.FirstOrDefault(a => a.artistId == managingArtistId);
		if (managed != null) { RenderManage(desk, managed); return; }
		managingArtistId = null;

		Heading("ROSTER");
		if (!desk.Roster.Any()) { Body("Nobody signed yet. Go find an act on the A&R page."); return; }
		Body("Manage an act's songbook, or take them into the studio to make a record.");

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
			foreach (ContractCommitment promise in desk.OpenCommitmentsFor(artist.artistId)) {
				int daysLeft = promise.DueDay - LocalSceneRoomService.Day(TimeManager.Instance?.CurrentDate ?? GameDate.StartDate);
				string due = LocalSceneRoomService.Date(promise.DueDay).ToShortString();
				sleeve.AddFact(promise.Kind switch {
						ContractCommitmentKind.FundedSession => $"Session promised by {due}",
						ContractCommitmentKind.Distribution => promise.ReleaseDay.HasValue ? $"Wholesale line promised by {due}" : $"Record promised out by {due}, then a wholesale line",
						_ => promise.ReleaseDay.HasValue ? $"Trade ad promised by {due}" : $"Record promised out by {due}, then a trade ad",
					},
					daysLeft <= 14 ? Rust : null);
			}
			// Run-out paper is an open door: what the act's side has said about another label's interest or offer.
			if (matured && (desk.RivalOfferLine(artist.artistId) ?? desk.RivalInterestLine(artist.artistId)) is string rivalWord)
				sleeve.AddFact(rivalWord, Rust);

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
			var studio = Btn(desk.Session?.ArtistId == artist.artistId ? "RETURN TO STUDIO" : "TAKE INTO STUDIO");
			studio.Pressed += () => UIManager.Instance?.OpenStudio(captured.artistId);
			sleeve.AddVerb(studio);
			var dossier = Btn("DOSSIER");
			dossier.Pressed += () => UIManager.Instance?.OpenArtist(captured.artistId, true);
			sleeve.AddVerb(dossier);
			content.AddChild(sleeve);
		}
	}

	/// <summary>The renewal menu for a matured contract: Pushover gets the same quick one-click form
	/// as a first signing; Firm/Hardball opens the same negotiation scene, different nouns.</summary>
	private void RenewalScene(RenewalOffer offer) {
        if (PlayerDesk.Instance.RenewalDiscussionUnavailable() is { } reason) Body(reason);
		if (offer.Posture != NegotiationPosture.Pushover) { NegotiationScene(offer.Talk); return; }

		Heading($"RENEW — {offer.Artist.stageName.ToUpperInvariant()}");
		if (!string.IsNullOrEmpty(offer.Ask.DemandSummary)) Body($"Their ask: {offer.Ask.DemandSummary}");
		OutlookBlock(PlayerDesk.Instance.ContractOutlookLines(offer.Artist, renewal: true));
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
			() => { renewingArtist = null; Refresh(); }, offer.Ask, offer.Artist.stageName, true);
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
		var studio = Primary(desk.Session?.ArtistId == artist.artistId ? "RETURN TO STUDIO" : "TAKE INTO STUDIO");
		studio.Pressed += () => UIManager.Instance?.OpenStudio(artist.artistId);
		content.AddChild(studio);
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
		if (ProfessionalWritingMarket.Enabled) {
			// Market choices, quotes and pending work are shown below the ordinary writing actions.
		} else if (desk.IsCommissioning(artist.artistId)) {
			var pending = Btn("WRITER AT WORK…");
			pending.CustomMinimumSize = new Vector2(240, 40);
			pending.Disabled = true; pending.TooltipText = "A writer is working on a song for them; it arrives on the roster when it is finished.";
			actions.AddChild(pending);
		} else {
			var commission = Btn($"COMMISSION A SONG (${PlayerDesk.CommissionFee:N0})");
			commission.CustomMinimumSize = new Vector2(240, 40);
			commission.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.CommissionSong(artist, out string message); Say(message, ok); return ok; });
			actions.AddChild(commission);
		}
		content.AddChild(actions);
		if (ProfessionalWritingMarket.Enabled) ProfessionalWritingSection(desk, artist);

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
		Body("Click a song to hear how it would sit with them, then teach it.");
		// One song open at a time, its comparison and the one TEACH button in a slip beneath it (playtest 2026-11-04:
		// two tall boxes on every row of a long list). The open line and everything above it never move.
		foreach (PlayerDesk.MaterialChoice cover in covers) {
			bool open = polarCatalogSong == cover.SongId;
			Control row;
			var text = new Label {
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				Text = (open ? "▾  " : "▸  ") + (PolarSongBehavior.UsePolarFitSelection ? $"\"{cover.Title}\" ({cover.Detail}) — {GenreNameFormatter.Format(cover.Genre)}" :
					$"\"{cover.Title}\"  ({cover.Detail})  —  {GenreNameFormatter.Format(cover.Genre)}   •   {PolarPlayerPerception.DescribeHook(cover.Hook, 0.8f)}")
					+ (PolarPlayerPerception.DescribeCharacter(cover) is { Length: > 0 } coverCharacter ? "\n     " + coverCharacter : "")
			};
			text.AddThemeColorOverride("font_color", Ink);
			if (PolarSongBehavior.UsePolarFitSelection) {
				// The hook is the song's own pull, not a fit question, so the catalogue shows it as a bar under the title
				// (the same read the non-Polar line prints); fit with the act is in the slip once the song is opened.
				var stack = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
				stack.AddThemeConstantOverride("separation", 2);
				stack.AddChild(text);
				var hookRow = new HBoxContainer();
				hookRow.AddThemeConstantOverride("separation", 10);
				hookRow.AddChild(new Control { CustomMinimumSize = new Vector2(20, 0) });
				hookRow.AddChild(new ReadBar().Set(cover.Hook, 0.8f));
				var hookWords = new Label { Text = PolarPlayerPerception.DescribeHook(cover.Hook, 0.8f) };
				hookWords.AddThemeFontSizeOverride("font_size", 14);
				hookWords.AddThemeColorOverride("font_color", ReadBar.ColorFor(cover.Hook).Darkened(0.2f));
				hookRow.AddChild(hookWords);
				stack.AddChild(hookRow);
				row = stack;
			} else row = text;
			string songId = cover.SongId;
			var line = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
			line.AddThemeConstantOverride("separation", 0);
			line.AddChild(ClickRow(row, open, () => { polarCatalogSong = open ? null : songId; RefreshKeepingScroll(); }));
			if (open) {
				var slip = Slip();
				var column = new VBoxContainer();
				column.AddThemeConstantOverride("separation", 8);
				slip.AddChild(column);
				// The verb first, right under the line just clicked; the comparison (tall) reads below it.
				var teachRow = new HBoxContainer();
				teachRow.AddThemeConstantOverride("separation", 12);
				var take = Primary("TEACH THEM THIS ONE");
				take.CustomMinimumSize = new Vector2(200, 36);
				take.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.TeachCover(artist, songId, out string message); Say(message, ok); return ok; });
				teachRow.AddChild(take);
				var when = new Label { Text = $"{PlayerDesk.TeachHours}h to start them on it, then about {days} day{(days == 1 ? "" : "s")} of rehearsal.", VerticalAlignment = VerticalAlignment.Center };
				when.AddThemeFontOverride("font", PaperTheme.SerifItalic);
				when.AddThemeFontSizeOverride("font_size", 15);
				when.AddThemeColorOverride("font_color", Heard);
				teachRow.AddChild(when);
				column.AddChild(teachRow);
				if (PolarSongBehavior.UsePolarFitSelection)
					column.AddChild(ComparisonCard(artist, cover, PolarEvidenceGate.Demo, "catalogue:" + songId, null, 0,
						hearing: new PolarHearing { source = PolarHearingSource.Record, fitWithAct = true }));
				line.AddChild(slip);
			}
			content.AddChild(line);
		}
	}

	private Control ComparisonCard(SimulatedArtist artist, PlayerDesk.MaterialChoice choice, PolarEvidenceGate gate,
		string eventId, PolarSessionContext session, int slot, Func<SimulatedArtist, PolarSessionContext> sessionForAct = null, string printedMasterId = null,
		PolarHearing hearing = null, bool onSheet = false, bool actFixed = false) {
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
		var names = actFixed ? new List<SimulatedArtist> { artist } : PlayerDesk.Instance.Roster.Prepend(artist).DistinctBy(a => a.artistId).ToList();
		var picker = Option(); foreach (var act in names) picker.AddItem(act.stageName);
		// A kept take belongs to the recorded act. The catalogue can compare material across the roster; a studio booking is for one act.
		// Ear reads are about the song as heard, so there is no act to project it onto.
		picker.Disabled = gate == PolarEvidenceGate.Playback; picker.Visible = hearing?.IsEar != true && !actFixed; column.AddChild(picker);
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
		int hours, IReadOnlyList<string> responses, List<(CheckBox Box, PlayerDesk.MaterialChoice Choice)> checks, StudioBrief brief = StudioBrief.CaptureLiveAct,
		string roomId = null, string engineerId = null, bool backingSingers = false, StudioBookingOptions options = null) {
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
		GreyWhy(own, "They have no songs of their own ready to record.");
		var insist = dialog.AddButton(artist.artistCreativeControl ? "THEIR CALL — CAN'T INSIST" : "INSIST — BOOK THE ROOM", () => {
			bool ok = PlayerDesk.Instance.StartSession(artist, chosen, tier, hours, out string message, overrideRefusal: true, brief: brief, roomId: roomId, engineerId: engineerId, backingSingers: backingSingers, options: options);
			Say(message, ok); Refresh();
		}, PaperModal.ButtonKind.Primary);
		// Creative control is the act's final word on material: the contract you signed takes the override away.
		insist.Disabled = artist.artistCreativeControl;
		if (artist.artistCreativeControl) dialog.AddText($"{artist.stageName} holds creative control under their contract, so you can't overrule this.");
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
		StockLine(desk);
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
				// With the 45s in the office a test disc has no job left; keep only an acetate already cut.
				if (!pressed || desk.AcetatesFor(id) > 0) AcetateControls(desk, id);
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
				() => {
					// Playtest 2026-11-04: no per-record sales figure anywhere; the per-store rows were the only readout.
					(int shelves, int sold, float owed) = desk.RecordSellThrough(id);
					content.AddChild(WorkOrder.Typed("SELL-THROUGH", $"{stock?.Remaining ?? 0:N0} in the office  ·  {shelves:N0} on shop shelves  ·  " +
						$"{sold:N0} sold  ·  {(owed >= 0.5f ? $"about ${owed:N0} owed to you" : "nothing owed")}", 0f, true));
					PressingControls(desk, id, false);
				}, "Repress requisition", order != null ? "ON ORDER" : null, RubberStamp.Blue);
		}
	}

	private static string MasterDetail(PlayerDesk.Master master) =>
		(PolarSongBehavior.UsePolarFitSelection ? "" : $"hook: {PolarPlayerPerception.DescribeHook(master.Record.hookStrength, 1f)}   •   sound: {SoundWords(master.Record.productionQuality)}   •   ") +
		$"cost ${master.ProductionCost:N0}   •   cut {master.Cut.ToHeadlineString()}" +
		(master.StudioHistory is { } h ? $"\n{h.RoomName}{(h.EngineerName != null ? $" with {h.EngineerName}" : "")}  •  kept take {h.KeptTake}  •  {h.ElapsedMinutes} minutes worked\n" +
			(h.SessionPlayers?.Count > 0 ? "Session: " + string.Join("; ", h.SessionPlayers) + "\n" : "") + $"{PlayerDesk.ArrangementName(h.Arrangement)}  •  revision {h.ArrangementRevision}\n{PlayerDesk.FinishName(h.FinishWork?.Finish ?? StudioFinish.House)}\n" + string.Join(" ", h.Notes) + (h.FinishWork == null ? "" : "\n" + h.FinishWork.Note) : "");

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
			RadioStation[] regionStations = desk.Rolodex.Select(card => ChartManager.Instance?.GetRadioStation(card.stationId))
				.Where(station => station != null && station.regionId == desk.CurrentCity?.parentRegionId)
				.Distinct().ToArray();
			RadioStation[] localStations = regionStations.Where(desk.IsStationInCurrentTown).ToArray();
			if (localStations.Length == 0 && regionStations.Length > 0) {
				Body($"Your DJs are out of town ({string.Join(", ", regionStations.Select(s => $"{s.callsign}, {s.cityName}").Distinct())}). Drive there to hand the acetate over in person, or play it to them down the phone.");
			}
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
			content.AddChild(FaintLine($"Plant credit outstanding: ${amount:N0} due on \"{title}\" in {Plural.Of(weeksAway, "week")} — it collects on schedule whether or not the record's still moving."));
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

	private const int DistributionMailStageDays = 3, DistributionHouseStageDays = 14;

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
		// Playtest 2026-11-04: release day opened about thirteen sections at once after a month of gentle unlocks. The
		// desk now opens in three stages: the town first; the mail-order work once copies are out (or after a few
		// days); the houses, deals and buy-ins once a record has two weeks behind it or has broken out somewhere.
		GameDate todayDist = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		var releasedDates = desk.ReleasedRecords.Where(r => r?.baseRecord != null && r.baseRecord.releaseDate.year > 0).Select(r => r.baseRecord.releaseDate).ToList();
		GameDate firstOut = releasedDates.Count > 0 ? releasedDates.OrderBy(d => d.year).ThenBy(d => d.month).ThenBy(d => d.day).First() : todayDist;
		int daysOut = Mathf.Max(0, DaysBetween(firstOut, todayDist));
		bool copiesOut = desk.StopStock().Any() || desk.OpenBalancesByStop().Any();
		bool brokeOut = desk.ReleasedRecords.Any(r => r?.baseRecord != null && desk.BreakoutRegionNames(r.baseRecord.recordId).Any());
		bool stageMail = hasReleased && (copiesOut || daysOut >= DistributionMailStageDays);
		bool stageHouses = hasReleased && (daysOut >= DistributionHouseStageDays || brokeOut);
		if (hasReleased && !stageHouses) {
			string Day(int days) => LocalSceneRoomService.Date(LocalSceneRoomService.Day(firstOut) + days).ToHeadlineString();
			Body(!stageMail
				? $"Start in town: place copies with a shop, or take one to a station. The mailing, the trades and the press desk open once copies are out (or on {Day(DistributionMailStageDays)}); " +
				  $"distribution deals and the wholesale houses on {Day(DistributionHouseStageDays)}, or sooner if a record breaks out."
				: $"Distribution deals, the wholesale houses and artist buy-ins open on {Day(DistributionHouseStageDays)}, or sooner if a record breaks out somewhere.");
		}

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
			// Period pass 2026-11-05: the destinations are typed out like a timetable, not hidden in a drop-down. Clicking a
			// line (or a town on the map) picks it; DRIVE THERE, on the picked line, commits the hours and gas.
			if (selectedDriveCityId != null && reach.All(c => c.cityId != selectedDriveCityId)) selectedDriveCityId = null;
			// One typed column per region, side by side, so a long reach reads like a timetable page rather than a scroll.
			var destinations = new HFlowContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
			destinations.AddThemeConstantOverride("h_separation", 22);
			destinations.AddThemeConstantOverride("v_separation", 10);
			VBoxContainer column = null; string columnRegion = null;
			foreach (MarketCity c in reach) {
				(int h, float g) = desk.DriveQuote(desk.CurrentCityId, c.cityId);
				string region = ChartManager.Instance?.GetRegionById(c.parentRegionId)?.regionName ?? c.parentRegionId;
				if (region != columnRegion) {
					columnRegion = region;
					column = new VBoxContainer { CustomMinimumSize = new Vector2(330, 0) };
					column.AddThemeConstantOverride("separation", 0);
					var caption = new Label { Text = region.ToUpperInvariant() };
					caption.AddThemeFontOverride("font", PaperTheme.TypedBold);
					caption.AddThemeFontSizeOverride("font_size", 13);
					caption.AddThemeColorOverride("font_color", Rust);
					column.AddChild(caption);
					destinations.AddChild(column);
				}
				bool picked = c.cityId == selectedDriveCityId;
				var line = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
				line.AddThemeConstantOverride("separation", 4);
				Label Typed(string text, Color color) {
					var label = new Label { Text = text, VerticalAlignment = VerticalAlignment.Center };
					label.AddThemeFontOverride("font", picked ? PaperTheme.TypedBold : PaperTheme.Typed);
					label.AddThemeFontSizeOverride("font_size", 15);
					label.AddThemeColorOverride("font_color", color);
					return label;
				}
				line.AddChild(Typed(c.name + (desk.WorkedCities.Contains(c.cityId) ? " ✓" : ""), Ink));
				line.AddChild(new DotLeader());
				line.AddChild(Typed($"{h} hrs · ${g:N0}", Ink));
				string id = c.cityId;
				var row = ClickRow(line, picked, () => { selectedDriveCityId = picked ? null : id; RefreshKeepingScroll(); },
					$"{c.name}, {region}: {h}h drive, ${g:N0} gas" + (desk.WorkedCities.Contains(c.cityId) ? ". Worked before." : "."));
				column.AddChild(row);
				if (!picked) continue;
				var drive = Btn($"DRIVE TO {c.name.ToUpperInvariant()}");
				drive.TooltipText = $"{h} hours on the road and ${g:N0} in gas. The office and studio are out of reach until you drive back.";
				drive.Pressed += () => Act(() => {
					bool ok = PlayerDesk.Instance.DriveTo(id, out string message);
					if (ok) selectedDriveCityId = null;
					Say(message, ok);
					return true;
				});
				var driveLine = new MarginContainer();
				driveLine.AddThemeConstantOverride("margin_left", 12);
				driveLine.AddThemeConstantOverride("margin_top", 4);
				driveLine.AddThemeConstantOverride("margin_bottom", 6);
				driveLine.AddChild(drive);
				column.AddChild(driveLine);
			}
			// The road map: where you are, home, the towns you've worked, and every town you can drive to today.
			// Clicking a town picks it in the list above; DRIVE TO still commits the hours and gas.
			var reachIds = reach.Select(c => c.cityId).ToHashSet();
			var roadMap = new UsMap {
				CustomMinimumSize = new Vector2(0, 400), SizeFlagsHorizontal = SizeFlags.ExpandFill, SelectedCityId = selectedDriveCityId,
				HereCityId = desk.CurrentCityId, HomeCityId = desk.Label?.homeCityId, HighlightRegionId = here?.parentRegionId,
				Marked = desk.WorkedCities.ToHashSet(),
				Unavailable = DistanceModel.GetCities().Select(c => c.cityId).Where(id => id != desk.CurrentCityId && !reachIds.Contains(id)).ToHashSet(),
				CityNote = id => {
					if (id == desk.CurrentCityId) return "You are here.";
					if (!reachIds.Contains(id)) return "Too far for one drive: go through a neighbouring region first.";
					(int h, float g) = desk.DriveQuote(desk.CurrentCityId, id);
					return $"{h}h drive, ${g:N0} gas" + (desk.WorkedCities.Contains(id) ? " · worked before" : "");
				}
			};
			roadMap.CityPressed += id => {
				if (!reachIds.Contains(id)) return;
				selectedDriveCityId = id; RefreshKeepingScroll();
			};
			content.AddChild(destinations);
			content.AddChild(roadMap);
			content.AddChild(FaintLine("Flag: you are here  ·  square: home office  ·  green ring: towns you've worked  ·  hollow dots: too far for one drive."));
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
		BuildAcetateCounters(desk, here);
		// Shops and operators matter once there is stock to place, or a stop here already holds copies, owes money or has called.
		bool showStops =onHand.Count > 0 || promoOnHand.Count > 0 ||
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
			// An open account from another town (or one since dropped) re-picks a default here.
			if (!string.IsNullOrEmpty(selectedStopId) && !stopsHere.Any(s => s.StopId == selectedStopId)) selectedStopId = null;
			foreach (var kindGroup in stopsHere.GroupBy(s => s.Kind).OrderBy(g => g.Key)) {
				PlayerDesk.StopKind kind = kindGroup.Key;
				List<PlayerDesk.PlayerStop> kindStops = kindGroup.ToList();
				bool expanded = expandedStopKinds.Contains(kind);

				var header = FoldHeading(StopKindLabel(kind), expanded,
					$"{Plural.Of(kindStops.Count, "account")}  {(expanded ? "· close" : "›")}", 13);
				header.Pressed += () => {
					if (!expandedStopKinds.Remove(kind)) expandedStopKinds.Add(kind);
					RefreshKeepingScroll();
				};
				content.AddChild(header);
				if (!expanded) continue;

				string estHours = PlayerDesk.StopVisitEstimate(kind);
				// What each verb costs, said once for the list rather than on forty buttons; then one account open at a time,
				// the first one not yet worked today opening on its own the first time through.
				content.AddChild(FaintLine(StopVerbCosts(kind, estHours)));
				if (selectedStopId == null) selectedStopId = kindStops.FirstOrDefault(s => !desk.HasWorkedStopToday(s.StopId))?.StopId;
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
					content.AddChild(StopRow(desk, stop, kind, onHand, singlePick, stopsWithCalls));
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
		if (promoOnHand.Count > 0 && stageMail) {
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

					// The list is the region's reporter stations that don't have the record yet; the spinner can't
					// promise more pieces than there are names to send them to.
					var listNote = FormLabel("");
					content.AddChild(listNote);
					void RefreshList() {
						string recordId = promoOnHand[Mathf.Clamp(mailSinglePick.Selected, 0, promoOnHand.Count - 1)].RecordId;
						MarketRegion region = regions[Mathf.Clamp(regionPick.Selected, 0, regions.Count - 1)];
						int names = desk.MailableStationCount(recordId, region.regionId);
						int onHand = promoOnHand[Mathf.Clamp(mailSinglePick.Selected, 0, promoOnHand.Count - 1)].PromoOnHand;
						int most = Mathf.Min(names, onHand);
						mailCount.MaxValue = Mathf.Max(1, most);
						mailCount.Value = Mathf.Clamp((int)mailCount.Value, 1, Mathf.Max(1, most));
						mailCount.Editable = most > 0;
						listNote.Text = names == 0
							? $"Every reporter station in {region.regionName} already has a copy."
							: $"Your {region.regionName} list has {Plural.Of(names, "station")} without this record" +
								(onHand < names ? $"; you have {onHand:N0} promo on hand." : ".");
					}
					mailSinglePick.ItemSelected += _ => RefreshList();
					regionPick.ItemSelected += _ => RefreshList();
					RefreshList();

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
		if (stageMail) {
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

		// --- PRESS & TELEVISION (media directive Phase 2): producers, editors, standards, reporters on deadline ---
		if (!hasReleased || stageMail) MediaSection(desk);

		// --- ARTIST BUY-IN (office only): an act buys a run of its own pressed single outright, cash now ---
		if (stageHouses) ArtistBuyInSection(desk);

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
		if (stageHouses || label.activeDeal != null || desk.PendingDistributionOffer != null) {
			SectionHeading("pnd", "P&D DISTRIBUTION DEAL", label.activeDeal != null || desk.PendingDistributionOffer != null,
				"A pressing-and-distribution deal covers the whole catalog for the term, not one title: an advance " +
				"(maybe), a real cut of everything, and often the masters, in trade for a distributor's own national " +
				"network. Only worth pitching once a record's proven itself regionally — otherwise nobody's biting.");
			if (label.activeDeal != null) {
				var deal = label.activeDeal;
				string distName = CompetitorManager.Instance?.GetLabel(deal.distributorId)?.labelName ?? deal.distributorId;
				int weeksLeft = Mathf.Max(0, deal.signedWeek + deal.termWeeks - (ChartManager.Instance?.GetCurrentChartWeek() ?? 0));
				Body($"Under contract to {distName} — {deal.marginSkim:P0} skim, {Plural.Of(weeksLeft, "week")} left on the term" +
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
				// Ask the pitch's own readiness check before the click, so a closed door says why up front.
				string notYet = null;
				bool ready = CompetitorManager.Instance?.PlayerDealReadiness(label, out notYet) ?? false;
				var pitchBtn = Btn($"PITCH FOR A DEAL ({PlayerDesk.SignHours}h)");
				pitchBtn.CustomMinimumSize = new Vector2(220, 40);
				pitchBtn.Disabled = !ready; GreyWhy(pitchBtn, notYet ?? "Not ready for a distribution deal yet.");
				pitchBtn.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.PursueDistributionDeal(out string message); Say(message, ok); return ok; });
				content.AddChild(pitchBtn);
				if (!ready && !string.IsNullOrEmpty(notYet)) content.AddChild(FaintLine(notYet));
			}
		}

		// --- WHOLESALE HOUSES (the gamble) ---
		if (stageHouses) {
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
					Text = $"{region.regionName}  —  {Plural.Of(houses, "house")} with room  —  " +
						(proven ? "they've heard of you here"
							: WholesaleSpreadGate.Enabled
								? $"no proof here yet — a cold pitch ({WholesaleSpreadGate.SustainedTractionWeeks} straight weeks selling here makes it a sure thing)"
								: "no proof here yet — a cold pitch")
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
			$"The overdraft ceiling is ${-desk.CreditFloor:N0}, but it is not a loan. {PlayerDesk.SolvencyRuleText}");
		var check = desk.NextSolvencyCheck(label.cashReserves);
		Body($"Next check: {check.Date.ToHeadlineString()} ({check.Days} day{(check.Days == 1 ? "" : "s")}), after ${check.Overhead:N0} overhead{(check.BackerPayment > 0.005f ? $" and ${check.BackerPayment:N0} to the backer" : "")}: {Money(check.CashAfter)} if nothing else moves -- {check.Outcome}.");
		BuildReleaseRoute(desk);
		BuildCashOptions(desk);
		BuildBacker(desk);

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

			foreach (RecordRuntimeData record in released)
				if (desk.IsMasterOut(record.baseRecord.recordId))
					Body($"\"{record.baseRecord.title}\" — the master's out on this one; not yours to sell right now.");
		}
		BuildMasterDeals(desk);
		BuildSongPlacements(desk);
		BuildPublishingRoyalties(desk);

		Heading("ARTIST ACCOUNTS");
		var roster = desk.Roster.ToList();
		if (roster.Count == 0) { Body("Nobody signed."); return; }
		content.AddChild(new LedgerTable().Set(null, new[] { "act", "unrecouped", "paid through", "of retail", "as writers" },
			roster.Select(artist => LedgerTable.Entry(artist.stageName, LedgerTable.Money(artist.unrecoupedAdvance),
				LedgerTable.Money(artist.totalRoyaltyEarnings), $"{artist.royaltyRate:P1}", LedgerTable.Money(artist.writerRoyaltyEarnings)))));
		Body("As writers: the writer's half of the 2c mechanical on songs you publish, and their half of the radio money. It isn't recoupable.");
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
				? (a.expired ? "That window's closed -- he never turned it over." : $"He's listening for it ({Plural.Of(weeksLeft, "more week")})")
				: tier != SpinTier.None
					? $"ON THE AIR — {PlayerDesk.TierWord(tier)} rotation"
					: a.expired ? "Not picked up. His argument has run out."
					: $"Not on yet — he's still arguing for it ({Plural.Of(weeksLeft, "more meeting")})";
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

	/// <summary>One thing the player can say: a line of script, not a boxed button. Left-aligned serif behind a rust
	/// lead mark, coloured by the voice that surfaced it and marked with that instinct's linocut, with its cost and its
	/// truthfulness in small italic underneath. A bluff is always labelled as one. The whole row is the hit area.</summary>
	private void RenderOption(CallOption opt, Action onPressed) {
		bool voiced = opt.voice != ExecutiveVoice.None;
		var row = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		row.AddThemeConstantOverride("separation", 0);

		var line = new Button {
			Text = "▸  " + opt.label, Alignment = HorizontalAlignment.Left,
			AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill,
			CustomMinimumSize = new Vector2(0, 34), Disabled = !opt.enabled,
			TooltipText = voiced ? VoiceName(opt.voice) : "",
			MouseDefaultCursorShape = opt.enabled ? CursorShape.PointingHand : CursorShape.Arrow,
		};
		float indent = voiced ? 42f : 10f;
		StyleBoxFlat Face(Color fill, int lead) => new() {
			BgColor = fill, BorderColor = lead > 0 ? Rust : new Color("c9b48a"),
			BorderWidthLeft = lead, BorderWidthBottom = lead > 0 ? 0 : 1,
			ContentMarginLeft = indent - lead, ContentMarginRight = 10, ContentMarginTop = 6, ContentMarginBottom = 6,
		};
		line.AddThemeStyleboxOverride("normal", Face(new Color(0, 0, 0, 0), 0));
		line.AddThemeStyleboxOverride("hover", Face(new Color("f6ecd0"), 3));
		line.AddThemeStyleboxOverride("pressed", Face(new Color("ead8ad"), 3));
		line.AddThemeStyleboxOverride("disabled", Face(new Color(0, 0, 0, 0), 0));
		line.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
		line.AddThemeFontOverride("font", PaperTheme.Serif);
		line.AddThemeFontSizeOverride("font_size", 17);
		Color ink = voiced ? VoiceColor(opt.voice) : Ink;
		foreach (string key in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_focus_color", "font_hover_pressed_color" })
			line.AddThemeColorOverride(key, ink);
		line.AddThemeColorOverride("font_disabled_color", new Color("9a8a6a"));
		if (voiced) {
			// The instinct the line draws on wears its linocut, the same prints as the founding cards.
			var badge = new LinocutIcon().Set(VoiceInstinct(opt.voice), 26f);
			badge.MouseFilter = MouseFilterEnum.Ignore;
			badge.Position = new Vector2(8, 4);
			if (!opt.enabled) badge.Modulate = new Color(1, 1, 1, 0.45f);
			line.AddChild(badge);
		}
		if (opt.enabled) line.Pressed += onPressed;
		row.AddChild(line);

		string sub = opt.enabled ? opt.subLabel : opt.disabledReason;
		if (opt.isBluff) sub = string.IsNullOrEmpty(sub) ? "Not true. He may check." : sub + "  —  not true. He may check.";
		if (!string.IsNullOrEmpty(sub)) {
			var margin = new MarginContainer { MouseFilter = MouseFilterEnum.Ignore };
			margin.AddThemeConstantOverride("margin_left", (int)indent + 18);
			margin.AddThemeConstantOverride("margin_bottom", 4);
			var note = new Label { Text = sub, AutowrapMode = TextServer.AutowrapMode.WordSmart };
			note.AddThemeFontOverride("font", PaperTheme.SerifItalic);
			note.AddThemeFontSizeOverride("font_size", 14);
			note.AddThemeColorOverride("font_color", opt.isBluff ? Rust : Heard);
			margin.AddChild(note);
			row.AddChild(margin);
		}
		content.AddChild(row);
	}

	private static InstinctKind VoiceInstinct(ExecutiveVoice voice) => voice switch {
		ExecutiveVoice.Street => InstinctKind.Street,
		ExecutiveVoice.Suit   => InstinctKind.Suit,
		ExecutiveVoice.Fixer  => InstinctKind.Fixer,
		_ => InstinctKind.Ear,
	};

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
			Body($"You have leads on all {Plural.Of(total, "reporter station")} in your region. Branch out to other markets to grow the book.");
			return;
		}
		Heading("WORK THE PHONES");
		Body($"Your region: {known} of {Plural.Of(total, "reporter station")} in your book. Cold-calling gets you a " +
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
		GreyWhy(all, "Every account he can work is already on his route.");
		all.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.SetRunnerRoute(eligible.Select(s => s.StopId), true, out string message); Say(message, ok); return ok; });
		everyone.AddChild(all);
		var none = Btn("CLEAR HIS ROUTE");
		none.Disabled = onRoute == 0;
		GreyWhy(none, "His route is already empty.");
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
			GreyWhy(townAll, "Every account in this town is already on his route.");
			townAll.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.SetRunnerRoute(stops.Select(s => s.StopId), true, out string message); Say(message, ok); return ok; });
			head.AddChild(townAll);
			var townNone = Btn("NONE");
			townNone.Disabled = townOn == 0;
			GreyWhy(townNone, "None of this town is on his route.");
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

	private static FontVariation spacedCaps;

	/// <summary>A fold-open section heading as a printed form sets one (period pass 2026-11-05): letter-spaced rust capitals
	/// on a ruled line, no box and no ▸/▾ triangle. The right end says, in typewriter, whether the file is open;
	/// <paramref name="note"/> replaces that with a short count ("8 accounts").</summary>
	private static Button FoldHeading(string title, bool open, string note = null, int size = 14) {
		if (spacedCaps == null) {
			// SansBold is itself a weighted FontVariation; wrapping it would drop the weight, so copy it and add spacing.
			spacedCaps = (FontVariation)PaperTheme.SansBold.Duplicate();
			spacedCaps.SpacingGlyph = 2;
		}
		var header = new Button { Text = title.ToUpperInvariant(), SizeFlagsHorizontal = SizeFlags.ExpandFill, Alignment = HorizontalAlignment.Left,
			FocusMode = FocusModeEnum.None, MouseDefaultCursorShape = CursorShape.PointingHand, CustomMinimumSize = new Vector2(0, 32) };
		header.TooltipText = open ? "Click to fold this section away." : "Click to open this section.";
		StyleBoxFlat Rule(Color fill) => new() {
			BgColor = fill, BorderColor = Ink, BorderWidthBottom = 1,
			ContentMarginLeft = 0, ContentMarginRight = 110, ContentMarginTop = 6, ContentMarginBottom = 3
		};
		header.AddThemeStyleboxOverride("normal", Rule(new Color(0, 0, 0, 0)));
		header.AddThemeStyleboxOverride("hover", Rule(new Color(PaperTheme.Highlighter, 0.35f)));
		header.AddThemeStyleboxOverride("pressed", Rule(new Color(PaperTheme.Highlighter, 0.5f)));
		header.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
		header.AddThemeFontOverride("font", spacedCaps);
		header.AddThemeFontSizeOverride("font_size", size);
		foreach (string name in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_focus_color", "font_hover_pressed_color" })
			header.AddThemeColorOverride(name, Rust);
		var state = new Label { Text = note ?? (open ? "close file" : "open file ›"), MouseFilter = MouseFilterEnum.Ignore,
			HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom };
		state.AddThemeFontOverride("font", PaperTheme.Typed);
		state.AddThemeFontSizeOverride("font_size", 13);
		state.AddThemeColorOverride("font_color", Heard);
		state.SetAnchorsAndOffsetsPreset(LayoutPreset.RightWide);
		state.OffsetLeft = -200; state.OffsetBottom = -3;
		header.AddChild(state);
		return header;
	}

	private Button SectionHeading(string key, string title, bool defaultOpen, string help = null) {
		content = contentRoot;   // close the previous section: later rows belong to this one
		bool open = sectionOpen.TryGetValue(key, out bool chosen) ? chosen : defaultOpen;
		Button header;
		if (formDepth > 0) {
			header = Band(title);
			header.Alignment = HorizontalAlignment.Left;
			header.CustomMinimumSize = new Vector2(0, 36);
			header.TooltipText = open ? "Click to fold this section away." : "Click to open this section.";
			header.AddThemeStyleboxOverride("normal", WorkOrder.SectionBand());
			header.AddThemeStyleboxOverride("hover", WorkOrder.SectionBand(true));
			header.AddThemeStyleboxOverride("pressed", WorkOrder.SectionBand());
			header.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
			foreach (string name in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_focus_color" })
				header.AddThemeColorOverride(name, WorkOrder.FormInk);
			header.AddThemeFontSizeOverride("font_size", 15);
			header.AddThemeFontOverride("font", PaperTheme.SansBold);
		} else header = FoldHeading(title, open, size: 15);
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

	/// <summary>Early-game §6.2: before the pressing lands, play an acetate across a shop or operator's counter and ask
	/// him to hold copies. Shown only while an unreleased master has an acetate in hand; each yes is a COD reservation
	/// in the call ledger, shown with what it is worth and when it lapses, never as cash.</summary>
	private void BuildAcetateCounters(PlayerDesk desk, MarketCity here) {
		var acetates = desk.Masters.Where(m => !m.Released && m.Record != null && desk.AcetatesFor(m.Record.recordId) > 0).ToList();
		var counters = desk.StopsInCity(here.cityId).Where(s => s.Kind is PlayerDesk.StopKind.Shop or PlayerDesk.StopKind.Op).ToList();
		if (acetates.Count == 0 || counters.Count == 0) return;
		SectionHeading("advance-orders", $"ADVANCE ORDERS IN {here.name.ToUpper()}", true,
			"Play your acetate for a dealer and ask him to hold copies for the pressing. He can pass. A yes is an order, not money: " +
			"it pays COD when you deliver the copies, and it lapses two weeks after the vinyl is due. One approach a day per account.");
		foreach (var master in acetates) {
			string id = master.Record.recordId;
			var held = desk.AdvanceOrdersFor(id);
			Body($"\"{master.SongTitle}\": {(held.Accounts == 0 ? "no orders held yet" : $"{held.Copies:N0} copies held across {Plural.Of(held.Accounts, "account")}, about {Money(held.CodValue)} COD on delivery")}.");
			var actions = new HFlowContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
			actions.AddThemeConstantOverride("h_separation", 8);
			actions.AddThemeConstantOverride("v_separation", 6);
			foreach (var stop in counters) {
				bool passed = stop.PassedRecordIds.Contains(id), holding = desk.HasOpenCall(stop.StopId, id), worked = desk.HasWorkedStopToday(stop.StopId);
				var play = Btn($"{stop.DisplayName}{(holding ? " — holding" : passed ? " — passed" : worked ? " — tomorrow" : $" ({PlayerDesk.StopVisitEstimate(stop.Kind)})")}");
				play.Disabled = passed || holding || worked;
				play.TooltipText = passed ? "He heard it and passed; that stands until it catches on somewhere."
					: holding ? "Already holding copies for the pressing." : worked ? "You've worked this counter today. Come back tomorrow." : "Play the acetate and ask him to hold copies.";
				play.Pressed += () => Act(() => {
					bool ok = PlayerDesk.Instance.PitchAcetateAtStop(stop.StopId, id, out string message);
					Say(message, ok);
					return true;
				});
				actions.AddChild(play);
			}
			content.AddChild(actions);
		}
	}

	/// <summary>Early-game §4.1 on the books: the cheapest remaining route to a first deliverable single, costed now.</summary>
	private void BuildReleaseRoute(PlayerDesk desk) {
		var plan = desk.PreviewReleaseCompletion();
		if (plan == null || plan.Remaining.Count == 0) return;
		Heading("THE ROUTE TO A FIRST RELEASE");
		var rows = plan.Remaining.Select(s => LedgerTable.Entry(s.Name + (s.Estimate ? " (estimate)" : ""), LedgerTable.Money(s.Cost))).ToList();
		rows.Add(LedgerTable.Total("Still to pay", LedgerTable.Money(plan.RemainingCost)));
		content.AddChild(new LedgerTable().Set("Cheapest credible route from here", null, rows));
		foreach (var s in plan.Remaining) Body($"{s.Name}: {s.Note}.");
		if (plan.OverheadBeforeArrival.Count > 0)
			Body($"Overhead falling before a run would land (about {PlayerDesk.CompletionPlantDaysForecast} days at the plant): " +
				string.Join(", ", plan.OverheadBeforeArrival.Select(o => $"${o.Overhead:N0} on {o.Date.ToShortString()}")) + ".");
		string warning = desk.CompletionWarning(0f);
		if (warning != null) Body(warning.Replace("After this, ", "As things stand, "));
	}

	/// <summary>Early-game §4.2: CASH OPTIONS. Every live route, its prerequisite, cost, expected cash, date and certainty --
	/// and, when nothing can bring money in, an honest empty card. It never adds demand or changes the closing rule.</summary>
	private void BuildCashOptions(PlayerDesk desk) {
		Heading("CASH OPTIONS");
		var options = desk.CashOptions();
		var live = options.Where(o => o.Executable).ToList();
		if (options.Count == 0) {
			Body("Nothing on the books can bring in cash right now: no stock on hand, nothing owed to you, no orders waiting and no deal anyone is offering. " +
				"Selling needs pressed records; a master deal needs airplay and a one-stop that knows the title.");
			return;
		}
		if (live.Count == 0) Body("No route can be acted on right now. What is listed below is blocked or would pay too late; the reason is on each line.");
		foreach (var o in options.OrderByDescending(o => o.Executable).ThenBy(o => o.Certainty)) {
			string sure = o.Certainty switch {
				PlayerDesk.CashCertainty.Owed => "owed to you",
				PlayerDesk.CashCertainty.Accepted => "agreed if you act",
				PlayerDesk.CashCertainty.Speculative => "a pitch, not a promise",
				_ => "blocked"
			};
			string money = o.Expected > 0.5f ? $"about {Money(o.Expected)}" : "no cash figure";
			string when = o.CashDate.HasValue ? $", {o.CashDate.Value.ToShortString()}" : "";
			string cost = o.Cost > 0.5f ? $"; costs {Money(o.Cost)}" : "";
			var line = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart,
				Text = $"{(o.Executable ? "•" : "×")} {o.Route.ToUpperInvariant()} -- {o.Action} ({o.Prerequisite}). {money}{when}, {sure}{cost}. {o.Why}" };
			line.AddThemeFontSizeOverride("font_size", 16);
			line.AddThemeColorOverride("font_color", o.Executable ? Ink : HeaderFade);
			content.AddChild(line);
		}
	}

	/// <summary>Early-game §4.3: the one backer agreement -- who, what he'd want, the open offer, or the loan's ledger.</summary>
	private void BuildBacker(PlayerDesk desk) {
		var loan = desk.Backer;
		var prospect = desk.BackerProspectFor();
		if (loan == null && prospect == null) return;
		Heading("BACKER", "One backer per label. A loan, not income: the money is paid back on the 1st, after the overhead and before the bank's check, " +
			"from cash above $0 only. What can't be paid is arrears, and costs you standing with him.");
		if (loan == null) {
			Body($"{prospect.Name}, {prospect.Title}. Up to ${prospect.Capacity:N0}. He wants {prospect.Wants}. {PlayerDesk.BackerMeetingHours}h {prospect.Where}, {prospect.Hours}.");
			var offer = desk.PendingBackerOffer;
			if (offer != null) {
				if (offer.Objection != null) Body(offer.Objection);
				for (int i = 0; i < offer.Terms.Count; i++) {
					int pick = i;
					var t = offer.Terms[i];
					var accept = Btn($"{t.Answer.ToUpperInvariant()}: ${t.Principal:N0} AT {t.Rate:P0}");
					accept.TooltipText = $"{t.Why} {PlayerDesk.BackerInstallments} monthly payments of a twelfth of the principal plus interest, the first on the {PlayerDesk.BackerGraceFirsts}rd 1st from now.";
					accept.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.AcceptBackerTerms(pick, out string m); Say(m, ok); return ok; });
					content.AddChild(accept);
				}
				Body($"The offer stands until {offer.Expires.ToShortString()}.");
				return;
			}
			string block = desk.BackerMeetingBlock(prospect);
			if (desk.BackerRefusal is { } r && r.Evidence == desk.BackerEvidence()) { Body(r.Reason + " That stands until something changes."); return; }
			var meet = Btn($"MEET {prospect.Name.ToUpperInvariant()} ({PlayerDesk.BackerMeetingHours}h)");
			meet.Disabled = block != null;
			meet.TooltipText = block ?? "He hears you out and answers on what you can show him. He never hears what you haven't cut.";
			meet.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.MeetBacker(out string m); Say(m, ok); return ok; });
			content.AddChild(meet);
			if (block != null) Body(block);
			return;
		}
		var today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		var rows = new List<LedgerTable.Row> {
			LedgerTable.Entry($"Borrowed from {loan.BackerName}, {loan.Signed.ToShortString()}, {loan.Rate:P0} a year", LedgerTable.Money(loan.Principal)),
			LedgerTable.Entry("Principal repaid", LedgerTable.Deduct(loan.PrincipalPaid)),
			LedgerTable.Total("Still owed", LedgerTable.Money(loan.Outstanding)),
			LedgerTable.Entry("Interest charged to date", LedgerTable.Money(loan.InterestAccrued)),
			LedgerTable.Entry("Interest unpaid", LedgerTable.Money(loan.InterestDue)),
			LedgerTable.Entry("Principal overdue", LedgerTable.Money(loan.PrincipalArrears(today))),
		};
		content.AddChild(new LedgerTable().Set(loan.Repaid ? "The backer: paid off" : "The backer", null, rows));
		if (loan.Repaid) { Body($"Square with {loan.BackerName}. One backer per label: there's no second ask."); return; }
		var next = loan.NextDue(today);
		Body($"Payments of ${loan.Installment:N2} plus interest on the 1st, from {loan.FirstDue.ToShortString()}" +
			(next.HasValue ? $"; next {next.Value.ToShortString()}" : "") + $". Missed so far: {loan.Missed}. Standing with him: {loan.Standing:P0}." +
			(loan.Source == PlayerDesk.BackerSource.MerchantLender ? $" Your pressed stock is his security: {PlayerDesk.BackerSeizeAfterMisses} misses in a row and he takes copies at ${PlayerDesk.DealerPrice:F2}." : ""));
		float owedNow = loan.Outstanding + loan.InterestDue;
		var payOff = Btn($"PAY HIM ${Mathf.Min(owedNow, Mathf.Max(0f, desk.Label.cashReserves)):N2}");
		payOff.Disabled = desk.Label.cashReserves <= 0f;
		payOff.TooltipText = "Interest first, then anything overdue, then principal ahead of schedule. Never more than you owe or have.";
		GreyWhy(payOff, "You have no cash to pay him with.");
		payOff.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.RepayBacker(owedNow, out string m); Say(m, ok); return ok; });
		content.AddChild(payOff);
	}

	/// <summary>F7: the overhead is taken on the 1st and the bank checks cash straight after it.</summary>
	private static string MonthEndCashPreview(AILabel label, float cashAfterAction) {
		var check = PlayerDesk.Instance?.NextSolvencyCheck(cashAfterAction);
		if (check == null || label == null) return "";
		var c = check.Value;
		string backer = c.BackerPayment > 0.005f ? $", ${c.BackerPayment:N0} to {PlayerDesk.Instance.Backer?.BackerName ?? "your backer"}" : "";
		return $"  ·  ${c.Overhead:N0} overhead taken {c.Date.ShortMonthName} {c.Date.day} ({c.Days} day{(c.Days == 1 ? "" : "s")}){backer}, then the bank's check: {Money(c.CashAfter)}, {c.Outcome}";
	}

	/// <summary>Directive §6: a one-stop takes no Pitch/Consign/Service -- it's "locked as a customer
	/// until inbound demand exists," then a warehouse visit, then a flat carton sale on COD/net terms.
	/// A distinct row shape from the walk-in Shop/Op accounts above, not a variant of theirs.</summary>
	/// <summary>What each kind of account's verbs cost, said once over its list instead of on every button.</summary>
	private static string StopVerbCosts(PlayerDesk.StopKind kind, string estHours) => kind switch {
		PlayerDesk.StopKind.Shop => $"Click an account to work it.  Pitch, consign or service: {estHours} each  ·  window card: "
			+ $"{PlayerDesk.WindowCardMinutes / 60}h and $8–20  ·  in-store: {PlayerDesk.InStoreAppearanceHours}h.",
		PlayerDesk.StopKind.OneStop => $"Click a one-stop to work it.  A warehouse visit or a carton sale: {PlayerDesk.OneStopVisitHours}h.",
		PlayerDesk.StopKind.Venue => $"Click a table to work it.  Work the table: {estHours}  ·  book a hop: {PlayerDesk.RecordHopHours}h.",
		PlayerDesk.StopKind.Station => $"Click a station to work it.  Drop off: {PlayerDesk.DropOffMinutes / 60}h  ·  wait for him: "
			+ $"{PlayerDesk.WaitForHimMinutes / 60}h  ·  leave it with the desk: ~{PlayerDesk.LeaveWithReceptionistMinutes}m  ·  "
			+ $"ask what's on the survey: ~{PlayerDesk.AskSurveyMinutes}m, free.",
		_ => $"Click an account to work it.  Pitch, consign or service: {estHours} each.",
	};

	/// <summary>Every account, whatever its kind, is one ledger line. Clicking it opens its counter slip underneath with
	/// the verbs that make sense for it right now; one account is open at a time, so the page carries one set of buttons
	/// instead of a row of them on every account (playtest 2026-11-04: eight stores, forty buttons, each repeating its
	/// hours). The open line and everything above it never move.</summary>
	private Control AccountRow(PlayerDesk.PlayerStop stop, string status, bool dim, Func<Control> slip) {
		bool open = selectedStopId == stop.StopId;
		string text = (open ? "▾  " : "▸  ") + status;
		var label = new Label {
			Text = text, SizeFlagsHorizontal = SizeFlags.ExpandFill,
			// One line, always: a result must never insert a row and shove the next account down.
			ClipText = true, TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
		};
		label.AddThemeColorOverride("font_color", dim && !open ? Heard : Ink);
		var box = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		box.AddThemeConstantOverride("separation", 0);
		box.AddChild(ClickRow(label, open, () => { selectedStopId = open ? "" : stop.StopId; RefreshKeepingScroll(); }, status));
		if (open) box.AddChild(slip());
		return box;
	}

	/// <summary>An open account's slip: the verbs in one flowing row (the obvious next move solid, the rest beside it)
	/// and plain sentences for what the solid one does or why there is nothing to do here today.</summary>
	private sealed class CounterSlip {
		public readonly PanelContainer Root = Slip();
		private readonly VBoxContainer column = new();
		private readonly HFlowContainer verbs = new();
		private readonly PlayerDeskPanel panel;
		private string hint;

		public CounterSlip(PlayerDeskPanel panel) {
			this.panel = panel;
			column.AddThemeConstantOverride("separation", 8);
			verbs.AddThemeConstantOverride("h_separation", 8);
			verbs.AddThemeConstantOverride("v_separation", 6);
			Root.AddChild(column);
			column.AddChild(verbs);
		}

		/// <summary>A sentence in the slip, above the verbs.</summary>
		public void Note(string text) {
			var note = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart };
			note.AddThemeFontOverride("font", PaperTheme.SerifItalic);
			note.AddThemeFontSizeOverride("font_size", 15);
			note.AddThemeColorOverride("font_color", Heard);
			column.AddChild(note);
			column.MoveChild(verbs, column.GetChildCount() - 1);
		}

		/// <summary>A verb. The primary one is solid, and its tooltip becomes the sentence under the row.</summary>
		public Button Verb(string text, bool primary, string tip, Func<bool> run) {
			var button = primary ? Primary(text) : Btn(text);
			button.CustomMinimumSize = new Vector2(primary ? 130 : 0, 34);
			button.TooltipText = tip;
			button.Pressed += () => panel.Act(run);
			verbs.AddChild(button);
			if (primary) hint = tip;
			return button;
		}

		public void Add(Control control) => verbs.AddChild(control);

		/// <summary>Finishes the slip: drops an empty verb row and puts the primary verb's sentence under it.</summary>
		public Control Done(string hintLead = null) {
			if (verbs.GetChildCount() == 0) verbs.QueueFree();
			else if (hint != null) {
				var line = new Label { Text = (hintLead ?? "") + hint, AutowrapMode = TextServer.AutowrapMode.WordSmart };
				line.AddThemeFontOverride("font", PaperTheme.SerifItalic);
				line.AddThemeFontSizeOverride("font_size", 15);
				line.AddThemeColorOverride("font_color", Heard);
				column.AddChild(line);
			}
			return Root;
		}
	}

	/// <summary>The runner toggle, on any account he can work.</summary>
	private void RunnerVerb(CounterSlip slip, PlayerDesk desk, PlayerDesk.PlayerStop stop) {
		if (!desk.HasRunner || !PlayerDesk.RunnerCanWork(stop.Kind)) return;
		bool onRoute = desk.IsOnRunnerRoute(stop.StopId);
		slip.Verb(onRoute ? "TAKE OFF THE RUNNER'S ROUTE" : "PUT ON THE RUNNER'S ROUTE", false,
			"He works shops and jukebox operators in towns you've opened, once a chart week, at no hours of yours.", () => {
				bool ok = PlayerDesk.Instance.AssignRunnerStop(stop.StopId, !onRoute, out string message);
				Say(message, ok);
				return true;
			});
	}

	/// <summary>A record store or jukebox operator.</summary>
	private Control StopRow(PlayerDesk desk, PlayerDesk.PlayerStop stop, PlayerDesk.StopKind kind,
			List<(string RecordId, string Title, int OnHand)> onHand, OptionButton singlePick, HashSet<string> stopsWithCalls) {
		int stockHere = stop.OnHand.Values.Sum(lot => lot.Remaining);
		bool workedToday = desk.HasWorkedStopToday(stop.StopId);
		bool called = stopsWithCalls.Contains(stop.StopId);
		string relWord = stop.LastVisitWeek == 0 && stop.Relationship <= 0f ? "cold"
			: stop.Relationship < 0.35f ? "acquainted"
			: stop.Relationship < 0.7f ? "friendly"
			: "standing account";
		string status = (called ? "☎ " : "") + stop.DisplayName + "  ·  " + relWord
			+ (stockHere > 0 ? $"  ·  {stockHere:N0} on shelf" : "")
			+ (stop.OpenBalance > 0.5f ? $"  ·  ${stop.OpenBalance:N0} owed" : "")
			+ (called ? "  ·  they called" : "")
			+ (desk.PreOrderNote(stop.StopId) is string held ? $"  ·  {held}" : "")
			// Directive §7.1: the reporting dealers are flagged only once the player has earned that knowledge.
			+ (desk.KnowsWhoReports(stop.StopId) ? "  ·  reports" : "")
			+ (desk.HasRunner && desk.IsOnRunnerRoute(stop.StopId) ? "  ·  on the runner's route" : "")
			+ (workedToday ? "  ·  worked today" : "");
		return AccountRow(stop, status, workedToday, () => StopSlip(desk, stop, kind, onHand, singlePick, workedToday));
	}

	/// <summary>A store or operator's slip: pitch a cold account, service one that already holds your record, and the
	/// follow-up verbs (window card, in-store) only once there is a record on the shelf for them to sell.</summary>
	private Control StopSlip(PlayerDesk desk, PlayerDesk.PlayerStop stop, PlayerDesk.StopKind kind,
			List<(string RecordId, string Title, int OnHand)> onHand, OptionButton singlePick, bool workedToday) {
		var slip = new CounterSlip(this);
		if (onHand.Count == 0 || workedToday) {
			slip.Note(onHand.Count == 0 ? "No sellable copies on hand. Order another pressing from the CATALOG first."
				: "You've worked this account today. Come back tomorrow.");
			RunnerVerb(slip, desk, stop);
			return slip.Done();
		}
		string RecordId() => onHand[Mathf.Clamp(singlePick.Selected, 0, onHand.Count - 1)].RecordId;
		const string PitchTip = "Try for a COD placement. They can pass; what sells pays cash while you're in town, otherwise it's held until you collect.";
		const string ConsignTip = "Easier shelf space: they'll take it on consignment, but every sale sits on their balance until you collect.";
		const string ServiceTip = "Collect what they owe you and restock from the single picked above.";
		bool placed = stop.OnHand.Count > 0;
		Func<bool> pitch = () => { bool ok = PlayerDesk.Instance.PitchAtStop(stop.StopId, RecordId(), out string m); Say(m, ok); return true; };
		Func<bool> consign = () => { bool ok = PlayerDesk.Instance.ConsignAtStop(stop.StopId, RecordId(), out string m); Say(m, ok); return true; };
		Func<bool> service = () => { bool ok = PlayerDesk.Instance.ServiceStop(stop.StopId, RecordId(), out string m); Say(m, ok); return true; };
		if (placed) {
			slip.Verb("SERVICE", true, ServiceTip, service);
			slip.Verb("PITCH", false, PitchTip, pitch);
		} else slip.Verb("PITCH", true, PitchTip, pitch);
		slip.Verb("CONSIGN", false, ConsignTip, consign);

		// Directive §7.2: the honest report verb, only at a dealer the player has worked out keeps a report (§7.1).
		if (stop.ReportsToTrades && desk.KnowsWhoReports(stop.StopId))
			slip.Verb($"ASK FOR THE REPORT  (~{PlayerDesk.AskForReportMinutes}m)", false, "Ask what this dealer is sending the trades for your record.",
				() => { bool ok = PlayerDesk.Instance.AskForTheReport(stop.StopId, RecordId(), out string m); Say(m, ok); return true; });

		// Directive §9: window card and in-store appearance need the record already on this shelf: the second thing you do.
		if (kind == PlayerDesk.StopKind.Shop && placed) {
			slip.Verb("WINDOW CARD", false, "One hour. Costs $8–20 for the print run; the exact quote is rolled when you place it.",
				() => { bool ok = PlayerDesk.Instance.BuyWindowCard(stop.StopId, RecordId(), out string m); Say(m, ok); return true; });
			slip.Verb("IN-STORE", false, $"{PlayerDesk.InStoreAppearanceHours} hours. Needs an act with real local standing.",
				() => { bool ok = PlayerDesk.Instance.BookInStoreAppearance(stop.StopId, RecordId(), out string m); Say(m, ok); return true; });
			// Directive §7.3: the dishonest verb -- Fixer-gated, only at a reporting dealer, never once he's burned.
			if ((stop.ReportsToTrades || stop.ReportsToStationIds.Count > 0) && desk.KnowsWhoReports(stop.StopId) && !stop.HypeBurned
					&& desk.InstinctProfile.TheFixer >= PlayerDesk.HypeTheCountMinFixer) {
				var hypeCount = Spin(1, 25, 1, 5);
				hypeCount.CustomMinimumSize = new Vector2(60, 34);
				slip.Add(hypeCount);
				slip.Verb($"HYPE THE COUNT  (~{PlayerDesk.HypeTheCountMinutes}m)", false, "Buy back copies to pad what this dealer reports.",
					() => { bool ok = PlayerDesk.Instance.HypeTheCount(stop.StopId, RecordId(), (int)hypeCount.Value, out string m); Say(m, ok); return true; });
			}
		}
		RunnerVerb(slip, desk, stop);
		return slip.Done(placed ? "Service: " : "Pitch: ");
	}

	/// <summary>A list line the player clicks to open: the whole row is the hit area, it lights with a rust rule on
	/// hover, and the open one stays lit with its slip attached beneath.</summary>
	private static PanelContainer ClickRow(Control inner, bool open, Action pick, string tooltip = null) {
		var row = new PanelContainer {
			SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Stop,
			MouseDefaultCursorShape = CursorShape.PointingHand, TooltipText = tooltip ?? "",
		};
		StyleBoxFlat Face(Color fill, bool lead) => new() {
			BgColor = fill, BorderColor = lead ? Rust : new Color("c9b48a"),
			BorderWidthLeft = lead ? 3 : 0, BorderWidthBottom = lead ? 0 : 1,
			ContentMarginLeft = lead ? 9 : 12, ContentMarginRight = 10, ContentMarginTop = 6, ContentMarginBottom = 6,
		};
		StyleBox rest = open ? Face(new Color("ead8ad"), true) : Face(new Color(0, 0, 0, 0), false);
		StyleBox hover = open ? rest : Face(new Color("f6ecd0"), true);
		row.AddThemeStyleboxOverride("panel", rest);
		row.MouseEntered += () => row.AddThemeStyleboxOverride("panel", hover);
		row.MouseExited += () => row.AddThemeStyleboxOverride("panel", rest);
		row.GuiInput += input => {
			if (input is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true }) return;
			row.AcceptEvent();
			Callable.From(pick).CallDeferred();   // the refresh frees this row; never inside its own input callback
		};
		IgnoreMouse(inner);
		row.AddChild(inner);
		return row;
	}

	private static void IgnoreMouse(Control control) {
		control.MouseFilter = MouseFilterEnum.Ignore;
		foreach (Node child in control.GetChildren())
			if (child is Control inner) IgnoreMouse(inner);
	}

	/// <summary>The slip an open row hangs beneath it: a lighter sheet on the same rust rule.</summary>
	private static PanelContainer Slip() {
		var slip = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		slip.AddThemeStyleboxOverride("panel", new StyleBoxFlat {
			BgColor = new Color("f6ecd0"), BorderColor = Rust, BorderWidthLeft = 3, BorderWidthBottom = 1,
			ContentMarginLeft = 26, ContentMarginRight = 12, ContentMarginTop = 10, ContentMarginBottom = 12,
		});
		return slip;
	}

	/// <summary>Directive §6: a one-stop is locked as a customer until an account it serves brings you up (a call); then one
	/// warehouse visit opens it, and after that you sell it cartons. Same ledger line and slip as every other account.</summary>
	private Control BuildOneStopRow(PlayerDesk.PlayerStop stop, List<(string RecordId, string Title, int OnHand)> onHand,
			OptionButton singlePick, HashSet<string> stopsWithCalls) {
		bool hasCall = stopsWithCalls.Contains(stop.StopId);
		string status = (hasCall ? "☎ " : "") + stop.DisplayName + "  ·  "
			+ (!stop.OneStopUnlocked
				? (hasCall ? "heard about you from an account they serve" : "not yet acquainted")
				: stop.OneStopTrusted ? "net terms" : "COD only");
		return AccountRow(stop, status, !stop.OneStopUnlocked && !hasCall, () => {
			var slip = new CounterSlip(this);
			if (!stop.OneStopUnlocked) {
				if (!hasCall) {
					slip.Note("Not yet acquainted: one of the accounts they supply has to bring you up. When one does, they'll call, and a warehouse visit opens the door.");
					return slip.Done();
				}
				slip.Verb("VISIT THE WAREHOUSE", true, "They've heard of you from an account they serve. Go and see them, and they'll take your cartons from then on.",
					() => { bool ok = PlayerDesk.Instance.VisitOneStopWarehouse(stop.StopId, out string m); Say(m, ok); return true; });
				return slip.Done();
			}
			if (onHand.Count == 0) {
				slip.Note("No sellable copies on hand. Order another pressing from the CATALOG first.");
				return slip.Done();
			}
			SpinBox qty = Spin(1, PlayerDesk.OneStopCartonMax, 1, PlayerDesk.OneStopCartonDefault);
			qty.CustomMinimumSize = new Vector2(80, 34);
			qty.TooltipText = "Copies in the carton.";
			slip.Add(qty);
			slip.Verb("SELL A CARTON", true,
				"They buy the carton outright and scatter it to shops and operators you never meet. "
				+ (stop.OneStopTrusted ? "Net terms: they pay on schedule." : "COD: you're nobody to them yet."),
				() => {
					string recordId = onHand[Mathf.Clamp(singlePick.Selected, 0, onHand.Count - 1)].RecordId;
					bool ok = PlayerDesk.Instance.SellCartonToOneStop(stop.StopId, recordId, Mathf.RoundToInt((float)qty.Value), out string m);
					Say(m, ok);
					return true;
				});
			return slip.Done();
		});
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

	/// <summary>Directive §3.3: a church or hop table takes no pitch, consign or service: you work the table for cash, or
	/// (directive §8) book a hop with a jock to MC it. Same ledger line and slip as every other account.</summary>
	private Control BuildVenueRow(PlayerDesk.PlayerStop stop, List<(string RecordId, string Title, int OnHand)> onHand,
			OptionButton singlePick) {
		string relWord = stop.Relationship <= 0f ? "never worked" : stop.Relationship < 0.35f ? "known to you" : "a regular table";
		bool workedToday = PlayerDesk.Instance.HasWorkedStopToday(stop.StopId);
		string status = stop.DisplayName + "  ·  " + relWord + (workedToday ? "  ·  worked today" : "");
		return AccountRow(stop, status, workedToday, () => {
			var slip = new CounterSlip(this);
			if (onHand.Count == 0) {
				slip.Note("No sellable copies on hand. Order another pressing from the CATALOG first.");
				return slip.Done();
			}
			string RecordId() => onHand[Mathf.Clamp(singlePick.Selected, 0, onHand.Count - 1)].RecordId;
			// Early-game F1: where the town's rooms sell, tables run on their show nights (the venues board), not here.
			bool tablesHere = !PlayerDesk.RoomTablesActive(stop.CityId);
			if (!tablesHere) slip.Note("Tables in this town run on the rooms' show nights. Work them from the venues board.");
			else if (workedToday) slip.Note("You've worked this table today. Come back tomorrow.");
			else slip.Verb("WORK THE TABLE", true, "Sell copies across the table, cash in hand, no ledger.",
				() => { bool ok = PlayerDesk.Instance.WorkTheHopTable(stop.StopId, RecordId(), out string m); Say(m, ok); return true; });
			slip.Verb("BOOK A HOP", !tablesHere || workedToday,
				"A jock MCs, your act appears and you sell at the table. Needs a jock in town who trusts you; a weak act on the bill is a real mistake.",
				() => { bool ok = PlayerDesk.Instance.BookRecordHop(stop.StopId, RecordId(), out string m); Say(m, ok); return true; });
			return slip.Done();
		});
	}

	/// <summary>Promo mechanic directive §4: a station has no shelf and no balance, just the jock you can walk in on. Drop it
	/// off first; WAIT FOR HIM and LEAVE IT WITH THE DESK are what a wave-off is for. Draws only from the promo pool.</summary>
	private Control BuildStationRow(PlayerDesk.PlayerStop stop, List<(string RecordId, string Title, int PromoOnHand)> promoOnHand,
			OptionButton promoPick) {
		return AccountRow(stop, stop.DisplayName, false, () => {
			var slip = new CounterSlip(this);
			string RecordId() => promoOnHand.Count == 0 ? null
				: promoOnHand[Mathf.Clamp(promoPick.Selected, 0, promoOnHand.Count - 1)].RecordId;
			if (promoOnHand.Count == 0)
				slip.Note("No promo copies on hand: press some with the next run. You can still ask what's on their survey.");
			else {
				slip.Verb("DROP OFF", true, "Leave promo copies with the jock. He reads the record, the format and you, and he can wave you off.",
					() => { bool ok = PlayerDesk.Instance.DropOffAtStation(stop.StopId, RecordId(), out string m); Say(m, ok); return true; });
				slip.Verb("WAIT FOR HIM", false, "Sit in the lobby until he'll see you, then pitch him in person: a better shot than the phone.",
					() => {
						RolodexCall call = PlayerDesk.Instance.WaitForHimAtStation(stop.StopId, RecordId(), out string m);
						Say(m, call != null);
						if (call != null) GoToTab(RolodexTab, PageRolodex); // the pitch opens in person -- take focus immediately
						return true;
					});
				slip.Verb("LEAVE IT WITH THE DESK", false, "One copy with the receptionist. Quick, and it's what you do when you're behind.",
					() => { bool ok = PlayerDesk.Instance.LeaveWithReceptionist(stop.StopId, RecordId(), out string m); Say(m, ok); return true; });
			}
			slip.Verb("ASK WHAT'S ON THE SURVEY", promoOnHand.Count == 0,
				"Free: how they're spinning your records, and which local dealers report to them.",
				() => { bool ok = PlayerDesk.Instance.AskWhatsOnSurvey(stop.StopId, out string m); Say(m, ok); return true; });
			return slip.Done();
		});
	}

	// --- Styled interactive controls -------------------------------------------------------------
	// The default control theme paints text and selection highlights near-white, which vanishes on the
	// beige paper. These factories force dark ink across every state (normal / hover / pressed / focus)
	// so a highlighted option or a hovered button stays readable.

	// Buttons, OptionButtons and SpinBoxes carry the default control theme: light text on their own dark
	// stylebox, which reads fine on the beige page. Only the CheckBox is special -- it has no filled box,
	// so its label sits straight on the paper and must be dark in every state, or a hover/focus turns it
	// near-white and it vanishes (the "highlighting an option should not be white" note).
	// A button is as wide as its words. Dropped straight into the page's VBox it would otherwise stretch to the
	// full sheet (a 900px bar around "HANG UP"); rows that want to fill say so with ExpandFill, and section
	// bands use Band().
	private static Button Btn(string text) => new Button { Text = text, SizeFlagsHorizontal = SizeFlags.ShrinkBegin };

	/// <summary>A full-width section band (a fold-open header): the one kind of button that spans the sheet.</summary>
	private static Button Band(string text) => new Button { Text = text, SizeFlagsHorizontal = SizeFlags.ExpandFill };

	/// <summary>The one verb on a card that spends time or money: a solid oxblood button.</summary>
	private static Button Primary(string text) => new Button { Text = text, ThemeTypeVariation = PaperTheme.Primary };

	/// <summary>Tab / filter-chip state as real styles instead of alpha: the selected one is dark with
	/// light text, the others are light paper with full-contrast ink (alpha-faded text measured ~2:1).</summary>
	/// <summary>One option of a two-or-three-way choice. The chosen one is the dark, pressed-in face (not a greyed-out
	/// disabled button, which read as "unavailable"); pressing it again does nothing.</summary>
	private static Button ChoiceButton(string text, bool selected, Action choose) {
		var button = Btn(text);
		StyleToggle(button, selected);
		button.CustomMinimumSize = new Vector2(0, 34);
		if (selected) { button.FocusMode = FocusModeEnum.None; button.MouseDefaultCursorShape = CursorShape.Arrow; }
		else button.Pressed += choose;
		return button;
	}

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

	/// <summary>A greyed-out button says why (playtest 2026-11-04: SERVICE was grey on every row with no reason). When
	/// <paramref name="button"/> is disabled, <paramref name="reason"/> leads its tooltip; an enabled button is untouched.</summary>
	private static void GreyWhy(BaseButton button, string reason) {
		if (button == null || !button.Disabled || string.IsNullOrEmpty(reason)) return;
		if (button.TooltipText?.StartsWith(reason, StringComparison.Ordinal) == true) return;
		button.TooltipText = string.IsNullOrEmpty(button.TooltipText) ? reason : reason + "\n" + button.TooltipText;
	}

	private static ImageTexture noArrows;

	/// <summary>A typed blank on a printed form (period pass 2026-11-05): the spin box loses its arrow pads and field box
	/// and becomes typewriter figures on an underline, with an optional printed word either side and small − / + nudges.
	/// The SpinBox itself is unchanged, so callers keep reading <c>Value</c> and <c>ValueChanged</c>.</summary>
	private static HBoxContainer TypedBlank(SpinBox spin, string before, string after) {
		noArrows ??= ImageTexture.CreateFromImage(Image.CreateEmpty(1, 1, false, Image.Format.Rgba8));
		foreach (string icon in new[] { "updown", "up", "up_hover", "up_pressed", "up_disabled", "down", "down_hover", "down_pressed", "down_disabled" })
			spin.AddThemeIconOverride(icon, noArrows);
		foreach (string side in new[] { "up", "down" })
			foreach (string state in new[] { "_background", "_hover_background", "_pressed_background", "_disabled_background" })
				spin.AddThemeStyleboxOverride(side + state, new StyleBoxEmpty());
		spin.AddThemeConstantOverride("buttons_width", 0);
		spin.AddThemeConstantOverride("set_min_buttons_width_from_icons", 0);
		spin.AddThemeConstantOverride("field_and_buttons_separation", 0);
		spin.CustomMinimumSize = new Vector2(96, 30);
		spin.Alignment = HorizontalAlignment.Center;
		LineEdit field = spin.GetLineEdit();
		StyleBoxFlat Blank(Color line, Color fill) => new() { BgColor = fill, BorderColor = line, BorderWidthBottom = 1,
			ContentMarginLeft = 4, ContentMarginRight = 4, ContentMarginTop = 2, ContentMarginBottom = 2 };
		field.AddThemeStyleboxOverride("normal", Blank(Ink, new Color(0, 0, 0, 0)));
		field.AddThemeStyleboxOverride("focus", Blank(Rust, new Color(PaperTheme.Highlighter, 0.45f)));
		field.AddThemeStyleboxOverride("read_only", Blank(new Color(Ink, 0.4f), new Color(0, 0, 0, 0)));
		field.AddThemeFontOverride("font", PaperTheme.Typed);
		field.AddThemeFontSizeOverride("font_size", 17);
		field.AddThemeColorOverride("font_color", Ink);

		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 6);
		Label Printed(string text) {
			var label = new Label { Text = text, VerticalAlignment = VerticalAlignment.Center };
			label.AddThemeFontOverride("font", PaperTheme.Typed);
			label.AddThemeFontSizeOverride("font_size", 16);
			label.AddThemeColorOverride("font_color", Ink);
			return label;
		}
		if (!string.IsNullOrEmpty(before)) row.AddChild(Printed(before));
		row.AddChild(spin);
		if (!string.IsNullOrEmpty(after)) row.AddChild(Printed(after));
		foreach ((string mark, int sign) in new[] { ("−", -1), ("+", 1) }) {
			var nudge = ScoutingQuietLink(mark);
			nudge.AddThemeFontOverride("font", PaperTheme.Typed);
			nudge.AddThemeFontSizeOverride("font_size", 16);
			nudge.TooltipText = sign < 0 ? $"Lower by {spin.Step:G}." : $"Raise by {spin.Step:G}.";
			nudge.Pressed += () => spin.Value += sign * spin.Step;
			row.AddChild(nudge);
		}
		return row;
	}

	/// <summary>A tick box typed on the form: "☒ Label keeps the publishing", no widget face.</summary>
	private static CheckBox TypedCheck(string text, bool pressed) {
		var c = new CheckBox { Text = text, ButtonPressed = pressed, FocusMode = FocusModeEnum.None, MouseDefaultCursorShape = CursorShape.PointingHand };
		ImageTexture off = TypedBox.Icon(false), on = TypedBox.Icon(true);
		c.AddThemeIconOverride("unchecked", off); c.AddThemeIconOverride("unchecked_disabled", off);
		c.AddThemeIconOverride("checked", on); c.AddThemeIconOverride("checked_disabled", on);
		var none = new StyleBoxEmpty { ContentMarginLeft = 0, ContentMarginRight = 6, ContentMarginTop = 2, ContentMarginBottom = 2 };
		var hover = new StyleBoxFlat { BgColor = new Color(PaperTheme.Highlighter, 0.4f), ContentMarginLeft = 0, ContentMarginRight = 6, ContentMarginTop = 2, ContentMarginBottom = 2 };
		foreach (string state in new[] { "normal", "pressed", "focus", "disabled" }) c.AddThemeStyleboxOverride(state, none);
		c.AddThemeStyleboxOverride("hover", hover); c.AddThemeStyleboxOverride("hover_pressed", hover);
		c.AddThemeFontOverride("font", PaperTheme.Typed);
		c.AddThemeFontSizeOverride("font_size", 16);
		foreach (string name in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_focus_color", "font_hover_pressed_color" })
			c.AddThemeColorOverride(name, Ink);
		return c;
	}

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
