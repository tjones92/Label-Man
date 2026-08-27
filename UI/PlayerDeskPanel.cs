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
	private Label titleLabel, clockLabel, statusLabel;
	private HBoxContainer tabs, idleRow;
	private VBoxContainer content;
	private readonly List<Button> tabButtons = new();
	private Action currentPage;
	private int currentTab;
	// Scouting-page state survives the rebuild-on-refresh: which room is selected, and which act (if
	// any) the player is currently drawing up a contract for.
	private PlayerDesk.ScoutingVenue selectedVenue = PlayerDesk.ScoutingVenue.ClubsAndRoadhouses;
	private PlayerDesk.Prospect negotiating;
	// The act whose contract renewal is on screen on the ROSTER tab. Mirrors `negotiating`, but keys
	// off PlayerDesk.PendingRenewal instead of a Prospect -- the renewal isn't a new signing.
	private SimulatedArtist renewingArtist;
	// The act whose MANAGE window is open on the ROSTER tab, and whether its cover-browse list is up.
	private string managingArtistId;
	private bool browsingCovers;
	// Whether the save/load menu is up (takes over the panel, like founding / game-over).
	private bool browsingSaves;
	// The founding archetype selected on the founding page; persists across Refresh() rebuilds.
	private FoundingArchetype selectedArchetype = FoundingArchetype.TradeInsider;
	// ROLODEX page state: which card is focused (index into PlayerDesk.Rolodex) and whether
	// the call view for that card is open.
	private int rolodexFocus;
	private string rolodexPitchRecordId;
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

	private static readonly Color Ink = new("2b2115");
	private static readonly Color Paper = new("f1e5c8");
	private static readonly Color Folder = new("d7b978");
	private static readonly Color Heard = new("6b5a3a");
	private static readonly Color Rust = new("6b3a1c");

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
	}

	public void ClosePanel() {
		Visible = false;
		if (UIManager.Instance != null) UIManager.Instance.isUIOpen = false;
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
		folder.AddThemeStyleboxOverride("panel", new StyleBoxFlat {
			BgColor = Folder, CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
			BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
			BorderColor = new Color("70552c"),
			ContentMarginLeft = 30, ContentMarginRight = 30, ContentMarginTop = 24, ContentMarginBottom = 24
		});
		AddChild(folder);

		var root = new VBoxContainer();
		root.AddThemeConstantOverride("separation", 10);
		folder.AddChild(root);

		var header = new HBoxContainer();
		root.AddChild(header);
		titleLabel = new Label { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		titleLabel.AddThemeFontSizeOverride("font_size", 28);
		header.AddChild(titleLabel);
		var saveLoad = Btn("SAVE / LOAD");
		saveLoad.Pressed += () => { browsingSaves = true; Refresh(); };
		header.AddChild(saveLoad);

		var close = Btn("CLOSE  ×");
		close.Pressed += ClosePanel;
		header.AddChild(close);

		clockLabel = new Label();
		clockLabel.AddThemeFontSizeOverride("font_size", 17);
		clockLabel.AddThemeColorOverride("font_color", Ink);
		root.AddChild(clockLabel);

		statusLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
		statusLabel.AddThemeFontSizeOverride("font_size", 16);
		statusLabel.AddThemeColorOverride("font_color", Rust);
		root.AddChild(statusLabel);

		// Passing time without working, so you can wait out the clock -- the clubs don't open till evening
		// and there's no other way to move the day forward from the desk.
		idleRow = new HBoxContainer();
		idleRow.AddThemeConstantOverride("separation", 8);
		var idleLabel = new Label { Text = "Nothing doing?" };
		idleLabel.AddThemeColorOverride("font_color", Ink);
		idleRow.AddChild(idleLabel);
		var wait1 = Btn("WAIT 1h");
		wait1.Pressed += () => Act(() => { PlayerDesk.Instance.PassTime(1, out string m); Say(m); return true; });
		idleRow.AddChild(wait1);
		var wait3 = Btn("WAIT 3h");
		wait3.Pressed += () => Act(() => { PlayerDesk.Instance.PassTime(3, out string m); Say(m); return true; });
		idleRow.AddChild(wait3);
		var waitEve = Btn("WAIT FOR EVENING");
		waitEve.Pressed += () => Act(() => {
			int h = PlayerDesk.Instance.HoursUntil(17);
			PlayerDesk.Instance.PassTime(h > 0 ? h : 1, out string m); Say(m); return true;
		});
		idleRow.AddChild(waitEve);
		root.AddChild(idleRow);

		tabs = new HBoxContainer();
		tabs.AddThemeConstantOverride("separation", 4);
		root.AddChild(tabs);

		var paper = new PanelContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
		paper.AddThemeStyleboxOverride("panel", new StyleBoxFlat {
			BgColor = Paper, ContentMarginLeft = 26, ContentMarginRight = 26,
			ContentMarginTop = 22, ContentMarginBottom = 22
		});
		root.AddChild(paper);

		var scroll = new ScrollContainer();
		paper.AddChild(scroll);
		content = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		content.AddThemeConstantOverride("separation", 10);
		scroll.AddChild(content);
	}

	// The macro tabs are the label's departments; Songs and Studio are not tabs -- they live inside a
	// roster act's MANAGE window (RenderManage). 1960s trade names in parentheses for flavor.
	private void BuildTabs() {
		foreach (Node child in tabs.GetChildren()) child.QueueFree();
		tabButtons.Clear();
		AddTab("A&R", PageAandR);
		AddTab("ROSTER", PageRoster);
		AddTab("CATALOG", PageCatalog);
		AddTab("DISTRIBUTION", PageDistribution);
		AddTab("FINANCES", PageFinances);
		AddTab("OFFICE", PageOffice);
		AddTab("ROLODEX", PageRolodex);
	}

	private void AddTab(string title, Action page) {
		int index = tabButtons.Count;
		var button = Btn(title);
		button.CustomMinimumSize = new Vector2(150, 40);
		button.Pressed += () => GoToTab(index, page);
		tabs.AddChild(button);
		tabButtons.Add(button);
	}

	// Tab order in BuildTabs: A&R(0), ROSTER(1), CATALOG(2), DISTRIBUTION(3), FINANCES(4), OFFICE(5), ROLODEX(6).
	private const int DistributionTab = 3;
	private const int RolodexTab = 6;

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

		if (desk == null) { titleLabel.Text = "DESK UNAVAILABLE"; return; }

		if (browsingSaves) {
			titleLabel.Text = "SAVE / LOAD";
			clockLabel.Text = time == null ? "" : $"{time.CurrentDate.ToLongString()}  •  {time.GetTimeString()}";
			if (idleRow != null) idleRow.Visible = false;
			foreach (Node child in tabs.GetChildren()) child.QueueFree();
			tabButtons.Clear();
			Clear(content);
			PageSaves(desk);
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

		if (tabButtons.Count == 0) { BuildTabs(); currentTab = 0; currentPage = PageAandR; }
		for (int index = 0; index < tabButtons.Count; index++)
			tabButtons[index].Modulate = index == currentTab ? Colors.White : new Color(1, 1, 1, .62f);

		AILabel label = desk.Label;
		titleLabel.Text = label.labelName.ToUpperInvariant();
		string region = ChartManager.Instance?.GetRegionById(label.homeRegion)?.regionName ?? label.homeRegion;
		string home = string.IsNullOrEmpty(label.headquartersCity) ? region : $"{label.headquartersCity}, {region}";
		string where = desk.AtHome ? $"at the office in {home}" : $"on the road in {desk.CurrentCity?.name ?? "town"}";
		clockLabel.Text =
			$"{time?.CurrentDate.ToLongString()}  •  {time?.GetTimeString()}  •  {time?.HoursRemaining ?? 0}h left ({time?.GetDayStatus()})\n" +
			$"{where}  |  ${label.cashReserves:N0} cash  |  {label.CurrentRosterSize}/{label.maxRosterSize} acts  |  " +
			$"{desk.WorkedCities.Count()} towns worked";
		// A running tab is survivable, but the bank is watching. Spell out the credit line and the clock on it.
		if (label.cashReserves < 0f)
			clockLabel.Text += $"\n⚠ IN THE RED — {desk.MonthsOfGraceLeft} month(s) before the creditors close you " +
				$"(credit line ${-desk.CreditFloor:N0}). Sell out of the trunk and collect what you're owed.";

		Clear(content);
		(currentPage ?? PageAandR)();
	}

	private void Say(string message) => statusLabel.Text = message ?? string.Empty;

	private void Act(Func<bool> action) {
		action();
		Refresh();
	}

	// ========================================================================
	// FOUNDING
	// ========================================================================

	private void PageFounding() {
		Heading("WHO WERE YOU BEFORE THIS?");

		// Archetype selector: a row of buttons, the selected one at full opacity.
		var archetypeRow = new HBoxContainer();
		archetypeRow.AddThemeConstantOverride("separation", 6);
		content.AddChild(archetypeRow);
		foreach (FoundingArchetype arch in System.Enum.GetValues<FoundingArchetype>()) {
			FoundingArchetype captured = arch;
			var btn = Btn(FoundingArchetypeData.Get(arch).Name.ToUpperInvariant());
			btn.CustomMinimumSize = new Vector2(230, 40);
			btn.Modulate = arch == selectedArchetype ? Colors.White : new Color(1, 1, 1, .55f);
			btn.Pressed += () => { selectedArchetype = captured; Refresh(); };
			archetypeRow.AddChild(btn);
		}

		var selected = FoundingArchetypeData.Get(selectedArchetype);
		Body($"{selected.Tagline}  ·  Starting capital: ${selected.Capital:N0}");
		Body(selected.Description);

		// Instinct spread.
		var instincts = selected.Instincts;
		Body($"THE EAR {StarBar(instincts.TheEar / 5f)}   THE STREET {StarBar(instincts.TheStreet / 5f)}   " +
			$"THE SUIT {StarBar(instincts.TheSuit / 5f)}   THE FIXER {StarBar(instincts.TheFixer / 5f)}");

		// Label stats summary line.
		Body($"Scouting {StarBar(selected.ScoutingAbility)}   Production {StarBar(selected.ProductionQuality)}   " +
			$"Marketing {StarBar(selected.MarketingPower)}");

		Heading("NAME THE LABEL AND PICK YOUR TOWN");

		var nameEdit = new LineEdit { PlaceholderText = "Label name", CustomMinimumSize = new Vector2(400, 38) };
		content.AddChild(nameEdit);

		// Towns grouped by market, the regional hubs first, so the pick reads like a map.
		var cityPicker = Option();
		cityPicker.CustomMinimumSize = new Vector2(400, 38);
		List<MarketRegion> regions = ChartManager.Instance?.GetAllRegions() ?? new List<MarketRegion>();
		var regionName = regions.ToDictionary(r => r.regionId, r => r.regionName);
		List<MarketCity> cities = DistanceModel.GetCities()
			.OrderBy(city => regionName.TryGetValue(city.parentRegionId, out string name) ? name : city.parentRegionId)
			.ThenByDescending(city => city.isRegionalHub)
			.ThenBy(city => city.distributionTier)
			.ToList();
		foreach (MarketCity city in cities) {
			string reg = regionName.TryGetValue(city.parentRegionId, out string name) ? name : city.parentRegionId;
			cityPicker.AddItem($"{city.name}  —  {reg}{(city.isRegionalHub ? "  (hub)" : "")}");
		}
		content.AddChild(cityPicker);

		var found = Btn("OPEN THE DOORS");
		found.CustomMinimumSize = new Vector2(240, 44);
		found.Pressed += () => {
			if (cities.Count == 0) { Say("No towns loaded."); return; }
			int index = Mathf.Clamp(cityPicker.Selected, 0, cities.Count - 1);
			PlayerDesk.Instance.FoundLabel(nameEdit.Text, cities[index].cityId, selectedArchetype, out string message);
			Say(message);
			Refresh();
		};
		content.AddChild(found);
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
			Body("Name this save. Using a name that already exists overwrites that slot.");
			var row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 10);
			var nameEdit = new LineEdit {
				PlaceholderText = "Save name", Text = desk.Label.labelName, CustomMinimumSize = new Vector2(360, 38)
			};
			row.AddChild(nameEdit);
			var save = Btn("SAVE");
			save.CustomMinimumSize = new Vector2(140, 38);
			save.Pressed += () => {
				SaveGameService.Save(string.IsNullOrWhiteSpace(nameEdit.Text) ? "quicksave" : nameEdit.Text, out string message);
				Say(message);
				Refresh();
			};
			row.AddChild(save);
			content.AddChild(row);
		} else Body(desk.IsGameOver
			? "The label has folded — you can only load from here."
			: "Open a label before you can save.");

		Heading("LOAD");
		List<SaveGameService.SaveInfo> saves = SaveGameService.ListSaves();
		if (saves.Count == 0) { Body("No saves on disk yet."); return; }
		foreach (SaveGameService.SaveInfo info in saves) {
			var row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 10);
			var text = new Label {
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				Text = $"{info.Slot}  —  {info.LabelName}, {info.InGameDate.ToHeadlineString()}   •   saved {info.SavedAtUtc.ToLocalTime():g}"
			};
			text.AddThemeColorOverride("font_color", Ink);
			row.AddChild(text);
			string slot = info.Slot;
			var load = Btn("LOAD");
			load.CustomMinimumSize = new Vector2(120, 36);
			load.Pressed += () => { SaveGameService.Load(slot, out string message); Say(message); browsingSaves = false; Refresh(); };
			row.AddChild(load);
			var del = Btn("DELETE");
			del.CustomMinimumSize = new Vector2(120, 36);
			del.Pressed += () => { SaveGameService.Delete(slot); Say($"Deleted \"{slot}\"."); Refresh(); };
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
			$"    {label.totalReleases} releases   •   {label.top40Hits} Top 40   •   {label.numberOneHits} #1s\n" +
			$"    ended ${label.cashReserves:N0} cash   •   ${label.outstandingWholesaleReceivables:N0} still owed by the houses");

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

	private static string VenueOptionLabel(PlayerDesk.ScoutingVenue venue) {
		(int open, int close) = PlayerDesk.VenueHours(venue);
		string name = Cap(PlayerDesk.VenueName(venue));
		return $"{name}  ({VenueBlurb[venue]})  —  open {Hour12(open)}–{Hour12(close)}";
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

		var venueRow = new HBoxContainer();
		venueRow.AddThemeConstantOverride("separation", 12);
		var venuePicker = Option();
		venuePicker.CustomMinimumSize = new Vector2(560, 40);
		for (int i = 0; i < VenueOrder.Length; i++) venuePicker.AddItem(VenueOptionLabel(VenueOrder[i]));
		venuePicker.Selected = Array.IndexOf(VenueOrder, selectedVenue);
		venuePicker.ItemSelected += index => selectedVenue = VenueOrder[index];
		venueRow.AddChild(venuePicker);

		var scout = Btn($"GO SCOUTING  ({PlayerDesk.ScoutHours}h)");
		scout.CustomMinimumSize = new Vector2(220, 40);
		scout.Pressed += () => Act(() => { PlayerDesk.Instance.ScoutVenue(selectedVenue, out string message); Say(message); return true; });
		venueRow.AddChild(scout);
		content.AddChild(venueRow);

		if (desk.Slate.Count == 0) { Body("No acts on the pad. Go hear somebody."); return; }

		Heading($"CAUGHT {desk.SlateDate.ToHeadlineString()}  —  {PlayerDesk.VenueName(desk.Slate[0].Venue)}");
		foreach (PlayerDesk.Prospect prospect in desk.Slate.ToList()) ProspectCard(prospect);
	}

	/// <summary>One act on the pad: the read, what you heard them play, and the next move.</summary>
	private void ProspectCard(PlayerDesk.Prospect prospect) {
		var card = new VBoxContainer();
		card.AddThemeConstantOverride("separation", 3);

		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 12);
		string writing = prospect.FollowedUp ? $"   •   writes {StarBar(prospect.Artist.songwritingAbility)}" : "";
		var text = new Label {
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			Text = $"{prospect.Artist.stageName}  —  {GenreNameFormatter.Format(prospect.Artist.primaryGenre)}\n" +
				$"    your read: {StarBar(prospect.ReadQuality)}   •   {prospect.Note}   •   asking ${prospect.AskingAdvance:N0}{writing}"
		};
		text.AddThemeColorOverride("font_color", Ink);
		row.AddChild(text);

		PlayerDesk.Prospect captured = prospect;
		if (!prospect.FollowedUp) {
			var follow = Btn($"FOLLOW UP ({PlayerDesk.FollowUpHours}h)");
			follow.CustomMinimumSize = new Vector2(180, 40);
			follow.Pressed += () => Act(() => { PlayerDesk.Instance.FollowUp(captured, out string message); Say(message); return true; });
			row.AddChild(follow);
		} else {
			var approach = Btn("APPROACH");
			approach.CustomMinimumSize = new Vector2(150, 40);
			approach.Pressed += () => {
				if (PlayerDesk.Instance.ApproachToSign(captured, out string message)) negotiating = captured;
				Say(message);
				Refresh();
			};
			row.AddChild(approach);
		}
		card.AddChild(row);

		// The live set: what you caught on the night, and -- after a follow-up -- the rest of it.
		var setText = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
		setText.AddThemeFontSizeOverride("font_size", 14);
		setText.AddThemeColorOverride("font_color", Heard);
		int shown = Mathf.Min(prospect.HeardCount, prospect.LiveSet.Count);
		var lines = prospect.LiveSet.Take(shown).Select(item =>
			$"    ♪ \"{item.Title}\" ({item.SourceTag}) — hook {StarBar(item.ReadHook)}");
		int hidden = prospect.LiveSet.Count - shown;
		string tail = hidden > 0 ? $"\n    …and {hidden} more you didn't catch — follow up to hear the full set." : "";
		setText.Text = (shown == 0 ? "    (didn't catch their set)" : string.Join("\n", lines)) + tail;
		card.AddChild(setText);
		content.AddChild(card);
	}

	/// <summary>The contract menu: the label's opening offer, editable, then put on the table.
	/// Pushover only -- signing is a single accept-or-walk click.</summary>
	private void ContractForm(PlayerDesk.Prospect prospect) {
		ContractTermSheet b = prospect.Baseline;
		Heading($"CONTRACT — {prospect.Artist.stageName.ToUpperInvariant()}");
		if (!string.IsNullOrEmpty(b.DemandSummary))
			Body($"On the table: {b.DemandSummary}");
		Body("Set the terms and put it to them. The negotiation costs " +
			$"{PlayerDesk.SignHours} hours; the advance is charged when they sign.");

		TermsForm(b, $"OFFER CONTRACT  ({PlayerDesk.SignHours}h)",
			(advance, royalty, term, singles, labelPub, artistControl) => {
				bool signed = PlayerDesk.Instance.OfferContract(prospect, advance, royalty, term, singles,
					labelPub, artistControl, out string message);
				if (signed) negotiating = null;
				Say(message);
				Refresh();
			},
			() => { negotiating = null; Refresh(); });
	}

	/// <summary>The editable grid shared by the plain contract form, every tabling round of a
	/// Firm/Hardball negotiation, and a renewal. Just the fields and the two buttons -- caller
	/// supplies the prefill, what the submit button says and does, and what "not now" does.</summary>
	private void TermsForm(ContractTermSheet prefill, string submitLabel,
			Action<float, float, int, int, bool, bool> onSubmit, Action onCancel) {
		var grid = new GridContainer { Columns = 2 };
		grid.AddThemeConstantOverride("h_separation", 18);
		grid.AddThemeConstantOverride("v_separation", 8);
		content.AddChild(grid);

		grid.AddChild(FormLabel("Advance ($)"));
		// Step of 5, not 25 -- a coarse step silently snapped a typed $35 down to $25 on finalize.
		var advance = Spin(0, 100000, 5, Mathf.Round(prefill.Advance));
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

		grid.AddChild(FormLabel("Publishing"));
		var labelPub = Check("Label keeps the publishing", prefill.LabelOwnsPublishing);
		grid.AddChild(labelPub);

		grid.AddChild(FormLabel("Creative control"));
		var artistControl = Check("Artist has creative control", prefill.ArtistCreativeControl);
		grid.AddChild(artistControl);

		var buttons = new HBoxContainer();
		buttons.AddThemeConstantOverride("separation", 12);
		var offer = Btn(submitLabel);
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
		TermsForm(PlayerDesk.CurrentOffer(talk), label,
			(advance, royalty, term, singles, labelPub, artistControl) => {
				PlayerDesk.Instance.TableOffer(talk, advance, royalty, term, singles, labelPub, artistControl, out string message);
				Say(message);
				CloseTalkIfDone(talk);
				Refresh();
			},
			() => {
				PlayerDesk.Instance.WalkFromTalk(talk, out string message);
				Say(message);
				CloseTalkIfDone(talk);
				Refresh();
			});
	}

	/// <summary>A negotiation scene serves both a new signing (closes `negotiating`) and a renewal
	/// (closes `renewingArtist`) -- clear whichever one this talk actually belongs to once it ends.</summary>
	private void CloseTalkIfDone(ContractTalk talk) {
		if (talk.stage != ContractTalkStage.Done) return;
		if (talk.IsRenewal) renewingArtist = null; else negotiating = null;
	}

	private void NegotiationObjection(ContractTalk talk) {
		Heading("HE COUNTERS");
		var stand = new Label {
			Text = $"Where it stands: roughly {talk.lastOfferValue * 100f:F0}% of what would close it " +
				$"({talk.reservation * 100f:F0}% clears it)   •   {talk.patienceLeft} of {talk.patienceMax} round(s) of patience left",
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
			PlayerDesk.Instance.PlayNegotiationCounter(talk, counter, out string message);
			Say(message);
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
			var card = new VBoxContainer();
			card.AddThemeConstantOverride("separation", 2);

			var row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 12);

			int songs = desk.RepertoireFor(artist.artistId).Count
				+ desk.UnrecordedSongs.Count(s => s.ArtistId == artist.artistId);
			string manager = artist.manager == ManagerArchetype.None ? "" : $"   •   managed by {artist.managerName ?? artist.manager.ToString()}";
			var text = new Label {
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				Text = $"{artist.stageName}  —  {GenreNameFormatter.Format(artist.primaryGenre)}  •  {artist.careerState}\n" +
					$"    {artist.totalReleases} releases   •   {artist.top40Hits} Top 40   •   {songs} in the songbook   •   " +
					$"{artist.royaltyRate:P1} royalty   •   {(matured ? "CONTRACT UP" : $"expires {artist.contractExpiresYear}")}{manager}"
			};
			text.AddThemeColorOverride("font_color", matured ? Rust : Ink);
			row.AddChild(text);

			SimulatedArtist captured = artist;
			if (matured) {
				var renew = Btn("RENEW");
				renew.CustomMinimumSize = new Vector2(120, 40);
				renew.Pressed += () => {
					if (PlayerDesk.Instance.ApproachRenewal(captured, out string message)) renewingArtist = captured;
					Say(message);
					Refresh();
				};
				row.AddChild(renew);
			}

			var manage = Btn("MANAGE");
			manage.CustomMinimumSize = new Vector2(150, 40);
			manage.Pressed += () => { managingArtistId = captured.artistId; browsingCovers = false; Refresh(); };
			row.AddChild(manage);

			var dossier = Btn("DOSSIER");
			dossier.CustomMinimumSize = new Vector2(110, 40);
			dossier.Pressed += () => UIManager.Instance?.OpenArtist(captured.artistId, true);
			row.AddChild(dossier);
			card.AddChild(row);
			content.AddChild(card);
		}
	}

	/// <summary>The renewal menu for a matured contract: Pushover gets the same quick one-click form
	/// as a first signing; Firm/Hardball opens the same negotiation scene, different nouns.</summary>
	private void RenewalScene(RenewalOffer offer) {
		if (offer.Posture != NegotiationPosture.Pushover) { NegotiationScene(offer.Talk); return; }

		Heading($"RENEW — {offer.Artist.stageName.ToUpperInvariant()}");
		if (!string.IsNullOrEmpty(offer.Ask.DemandSummary)) Body($"On the table: {offer.Ask.DemandSummary}");
		Body("Set the terms and put new paper in front of them. The meeting costs " +
			$"{PlayerDesk.NegotiationRoundHours} hours; the advance is charged when they sign.");

		TermsForm(offer.Ask, $"RENEW  ({PlayerDesk.NegotiationRoundHours}h)",
			(advance, royalty, term, singles, labelPub, artistControl) => {
				bool renewed = PlayerDesk.Instance.RenewContract(offer.Artist, advance, royalty, term, singles,
					labelPub, artistControl, out string message);
				if (renewed) renewingArtist = null;
				Say(message);
				Refresh();
			},
			() => { renewingArtist = null; Refresh(); });
	}

	/// <summary>The MANAGE window for one act: repertoire (write / teach a cover) and the studio.</summary>
	private void RenderManage(PlayerDesk desk, SimulatedArtist artist) {
		var back = Btn("‹ BACK TO ROSTER");
		back.CustomMinimumSize = new Vector2(200, 36);
		back.Pressed += () => { managingArtistId = null; browsingCovers = false; Refresh(); };
		content.AddChild(back);

		Heading($"MANAGING — {artist.stageName.ToUpperInvariant()}");
		Body($"{GenreNameFormatter.Format(artist.primaryGenre)}  •  {artist.careerState}  •  " +
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
		write.Pressed += () => Act(() => { PlayerDesk.Instance.WriteSongs(artist, out string message); Say(message); return true; });
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
			commission.Pressed += () => Act(() => { PlayerDesk.Instance.CommissionSong(artist, out string message); Say(message); return true; });
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
			else SongLine($"\"{item.Title}\"", tag, item.ReadHook);
		}
		foreach (PlayerDesk.Song song in written) {
			if (song.Recorded) RecordedLine(desk, $"\"{song.Title}\"", "their own", song.RecordedId, artist.artistId);
			else SongLine($"\"{song.Title}\"", "their own", song.Hook);
		}
		foreach (PlayerDesk.CoverRehearsal r in rehearsing)
			RehearsingLine(r);

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
				Text = $"    ♪ \"{cover.Title}\"  ({cover.Detail})  —  {GenreNameFormatter.Format(cover.Genre)}   •   hook {StarBar(cover.Hook)}"
			};
			text.AddThemeColorOverride("font_color", Ink);
			row.AddChild(text);
			string songId = cover.SongId;
			var take = Btn($"TEACH (~{days}d)");
			take.CustomMinimumSize = new Vector2(150, 36);
			take.Pressed += () => Act(() => { PlayerDesk.Instance.TeachCover(artist, songId, out string message); Say(message); return true; });
			row.AddChild(take);
			content.AddChild(row);
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

		var cost = new Label();
		cost.AddThemeColorOverride("font_color", Rust);
		content.AddChild(cost);
		void UpdateCost() {
			PlayerDesk.StudioTier tier = Tiers[Mathf.Clamp(tierPicker.Selected, 0, Tiers.Length - 1)];
			cost.Text = $"Studio time: ${desk.SessionCost(tier, (int)hoursInput.Value):N0}";
		}
		tierPicker.ItemSelected += _ => UpdateCost();
		hoursInput.ValueChanged += _ => UpdateCost();
		UpdateCost();

		var book = Btn("BOOK THE ROOM");
		book.CustomMinimumSize = new Vector2(300, 44);
		book.Pressed += () => Act(() => {
			var chosen = checks.Where(c => c.Box.ButtonPressed).Select(c => c.Choice).ToList();
			PlayerDesk.StudioTier tier = Tiers[Mathf.Clamp(tierPicker.Selected, 0, Tiers.Length - 1)];
			PlayerDesk.Instance.StartSession(artist, chosen, tier, (int)hoursInput.Value, out string message);
			Say(message);
			return true;
		});
		content.AddChild(book);
	}

	/// <summary>The console: keep a take per song, then print. Selecting a take is free.</summary>
	private void TakesView(PlayerDesk.PendingSession session, SimulatedArtist artist) {
		Body($"ON THE CONSOLE — {PlayerDesk.StudioTierName(session.Tier)}, {session.Hours}h, ${session.Cost:N0} spent. " +
			"Keep the take you want for each song, then print the masters.");

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
				var btn = Btn($"Take {take.Number}{(kept ? "  ✓" : "")}\nhook {StarBar(take.Hook)}\nprod {StarBar(take.Production)}");
				btn.CustomMinimumSize = new Vector2(190, 62);
				btn.ToggleMode = true;
				btn.ButtonPressed = kept;
				int cutIndex = c, takeIndex = t;
				btn.Pressed += () => { PlayerDesk.Instance.KeepTake(cutIndex, takeIndex); Refresh(); };
				takesRow.AddChild(btn);
			}
			content.AddChild(takesRow);
		}

		var buttons = new HBoxContainer();
		buttons.AddThemeConstantOverride("separation", 12);
		var print = Btn("PRINT MASTERS");
		print.CustomMinimumSize = new Vector2(240, 44);
		print.Pressed += () => {
			bool ok = PlayerDesk.Instance.PrintSession(out string message);
			Say(message);
			// A finished master's next stop is the plant, not a release date — take the player straight to
			// DISTRIBUTION to assemble, press, and get a turnaround quote before dating anything.
			if (ok) GoToTab(DistributionTab, PageDistribution);
			else Refresh();
		};
		buttons.AddChild(print);
		var scrap = Btn("SCRAP");
		scrap.CustomMinimumSize = new Vector2(140, 44);
		scrap.Pressed += () => { PlayerDesk.Instance.ScrapSession(); Say("Session scrapped."); Refresh(); };
		buttons.AddChild(scrap);
		content.AddChild(buttons);
	}

	private void SongLine(string title, string tag, float hook) {
		var text = new Label { Text = $"    ♪ {title}  ({tag}) — hook {StarBar(hook)}" };
		text.AddThemeFontSizeOverride("font_size", 15);
		text.AddThemeColorOverride("font_color", Ink);
		content.AddChild(text);
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
	// CATALOG (masters on the shelf + scheduling releases)
	// ========================================================================

	private void PageCatalog() {
		PlayerDesk desk = PlayerDesk.Instance;

		ShelfList(desk);

		Heading("PUTTING A SINGLE OUT");
		Body("Cutting two sides is only the start. Head to DISTRIBUTION to pair them into a 45 and send it to " +
			"the pressing plant — then, once the plant has quoted a turnaround, set the release date there so " +
			"the record ships after the vinyl's actually landed.");

		Heading("READY TO PRESS");
		List<PlayerDesk.PlannedRelease> undated = desk.Planned.Where(entry => !entry.Dated).ToList();
		if (undated.Count == 0) Body("No singles assembled yet.");
		foreach (PlayerDesk.PlannedRelease single in undated)
			Body($"\"{single.Master.SongTitle}\"{(single.BSide != null ? $" b/w \"{single.BSide.SongTitle}\"" : "")} " +
				$"by {single.Master.Record.artistName}  —  assembled, no date yet");

		Heading("ON THE SCHEDULE");
		List<PlayerDesk.PlannedRelease> dated = desk.Planned.Where(entry => entry.Dated).OrderBy(entry => entry.Date).ToList();
		if (dated.Count == 0) Body("Nothing dated.");
		foreach (PlayerDesk.PlannedRelease release in dated)
			Body($"{release.Date.ToHeadlineString()}  —  \"{release.Master.SongTitle}\"" +
				$"{(release.BSide != null ? $" b/w \"{release.BSide.SongTitle}\"" : "")} by {release.Master.Record.artistName}  " +
				$"(${release.MarketingBudget:N0} campaign)");

		Heading("IN THE MARKET");
		List<RecordRuntimeData> released = desk.ReleasedRecords.OrderByDescending(record => record.weeksSinceRelease).ToList();
		if (released.Count == 0) { Body("Nothing out yet."); return; }
		foreach (RecordRuntimeData record in released)
			Body($"\"{record.baseRecord.title}\" by {record.baseRecord.artistName}  —  " +
				$"{(record.peakPosition > 0 ? $"peak #{record.peakPosition}, {record.weeksOnChart} weeks on" : "has not charted")}  •  " +
				$"{record.weeksSinceRelease} weeks out");
	}

	private void ShelfList(PlayerDesk desk) {
		Heading("MASTERS ON THE SHELF");
		List<PlayerDesk.Master> shelf = desk.Masters.Where(master => !master.Scheduled).ToList();
		if (shelf.Count == 0) { Body("Nothing cut and waiting. Cut a record from an act's MANAGE window."); return; }
		foreach (PlayerDesk.Master master in shelf)
			Body($"\"{master.SongTitle}\"  —  {master.Record.artistName}\n" +
				$"    hook {StarBar(master.Record.hookStrength)}   •   production {StarBar(master.Record.productionQuality)}   " +
				$"•   cost ${master.ProductionCost:N0}   •   cut {master.Cut.ToHeadlineString()}");
	}

	// ========================================================================
	// DISTRIBUTION
	// ========================================================================

	private void PageDistribution() {
		PlayerDesk desk = PlayerDesk.Instance;
		AILabel label = desk.Label;
		Heading("DISTRIBUTION");

		// --- WHERE YOU ARE + DRIVING ---
		MarketCity here = desk.CurrentCity;
		string hereName = here?.name ?? "the office";
		Heading(desk.AtHome ? "AT THE OFFICE" : $"ON THE ROAD — {hereName.ToUpperInvariant()}");
		Body(desk.AtHome
			? "Assemble and press your records here, work your own home town out of the trunk, then set out to work " +
			  "the towns you can reach. Trunk sales in the town you're standing in are cash in hand; a town you've " +
			  "left holds your cut until you drive back to collect it."
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
			foreach (MarketCity c in reach) {
				(int h, float g) = desk.DriveQuote(desk.CurrentCityId, c.cityId);
				string region = ChartManager.Instance?.GetRegionById(c.parentRegionId)?.regionName ?? c.parentRegionId;
				cityPicker.AddItem($"{c.name} — {region}  ({h}h, ${g:N0} gas)");
			}
			driveRow.AddChild(cityPicker);
			var drive = Btn("DRIVE THERE");
			drive.CustomMinimumSize = new Vector2(160, 36);
			drive.Pressed += () => Act(() => {
				PlayerDesk.Instance.DriveTo(reach[Mathf.Clamp(cityPicker.Selected, 0, reach.Count - 1)].cityId, out string message);
				Say(message);
				return true;
			});
			driveRow.AddChild(drive);
			content.AddChild(driveRow);
		}

		// --- STOPS IN THIS TOWN: named shops and jukebox operators, each with its own relationship,
		// stock, and terms -- pitch (COD, refusal is real), consign (worse cash, the low-risk fallback),
		// or service (restock + collect, once a stop already has history) ---
		Heading($"STOPS IN {here.name.ToUpper()}");
		List<(string RecordId, string Title, int OnHand)> onHand = desk.PressedSinglesOnHand().ToList();
		List<(string RecordId, string Title, int PromoOnHand)> promoOnHand = desk.PromoSinglesOnHand().ToList();
		List<PlayerDesk.PlayerStop> stopsHere = desk.StopsInCity(here.cityId).ToList();
		// Flags which stops have a call waiting (directive §4) so the player doesn't have to cross-check
		// the OFFICE phone list by hand while deciding who to work first.
		var stopsWithCalls = new HashSet<string>(desk.PendingCalls().Select(c => c.Call.StopId), StringComparer.Ordinal);
		if (onHand.Count == 0 && promoOnHand.Count == 0)
			Body(desk.AtHome
				? "Nothing pressed on hand to sell or service yet — assemble a single and order a run below, then work a stop once it's in."
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

				int estHours = PlayerDesk.EstimatedStopHours(kind);
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
					string relWord = stop.LastVisitWeek == 0 && stop.Relationship <= 0f ? "cold"
						: stop.Relationship < 0.35f ? "acquainted"
						: stop.Relationship < 0.7f ? "friendly"
						: "standing account";

					var row = new HBoxContainer();
					row.AddThemeConstantOverride("separation", 10);
					var stopLabel = new Label {
						Text = (stopsWithCalls.Contains(stop.StopId) ? "    ☎ " : "    ") + $"{stop.DisplayName} ({relWord})"
							+ (stockHere > 0 ? $" — {stockHere:N0} on hand" : "")
							+ (stop.OpenBalance > 0.5f ? $" — ${stop.OpenBalance:N0} owed" : "")
							+ (stopsWithCalls.Contains(stop.StopId) ? " — they called" : "")
							// Directive §7.1: the one or two identified dealers a city's survey/trade
							// numbers actually come from -- flagged so the player can tell them apart, but
							// only once he's EARNED that (worked the counter, or asked at the station whose
							// survey it feeds). "That is the information the early game is actually about,"
							// so it is not printed free on a shop nobody has walked into.
							+ (desk.KnowsWhoReports(stop.StopId) ? " — reports" : ""),
						CustomMinimumSize = new Vector2(400, 32)
					};
					stopLabel.AddThemeColorOverride("font_color", Ink);
					row.AddChild(stopLabel);

					var pitch = Btn($"PITCH (~{estHours}h)");
					pitch.CustomMinimumSize = new Vector2(110, 32);
					pitch.Pressed += () => Act(() => {
						(string recordId, _, _) = onHand[Mathf.Clamp(singlePick.Selected, 0, onHand.Count - 1)];
						PlayerDesk.Instance.PitchAtStop(stop.StopId, recordId, out string message);
						Say(message);
						return true;
					});
					row.AddChild(pitch);

					var consign = Btn($"CONSIGN (~{estHours}h)");
					consign.CustomMinimumSize = new Vector2(130, 32);
					consign.Pressed += () => Act(() => {
						(string recordId, _, _) = onHand[Mathf.Clamp(singlePick.Selected, 0, onHand.Count - 1)];
						PlayerDesk.Instance.ConsignAtStop(stop.StopId, recordId, out string message);
						Say(message);
						return true;
					});
					row.AddChild(consign);

					var service = Btn($"SERVICE (~{estHours}h)");
					service.CustomMinimumSize = new Vector2(130, 32);
					service.Disabled = stop.OnHand.Count == 0;
					service.Pressed += () => Act(() => {
						(string recordId, _, _) = onHand[Mathf.Clamp(singlePick.Selected, 0, onHand.Count - 1)];
						PlayerDesk.Instance.ServiceStop(stop.StopId, recordId, out string message);
						Say(message);
						return true;
					});
					row.AddChild(service);

					// Directive §7.2: the honest report verb -- only offered at a dealer the player has
					// worked out keeps a report (§7.1), and only ever succeeds if he's actually holding and
					// moving the record. The verb itself stays ungated; this is what's on the SCREEN.
					if (stop.ReportsToTrades && desk.KnowsWhoReports(stop.StopId)) {
						var askReport = Btn($"ASK FOR THE REPORT (~{PlayerDesk.AskForReportMinutes}m)");
						askReport.CustomMinimumSize = new Vector2(200, 32);
						askReport.Pressed += () => Act(() => {
							(string recordId, _, _) = onHand[Mathf.Clamp(singlePick.Selected, 0, onHand.Count - 1)];
							PlayerDesk.Instance.AskForTheReport(stop.StopId, recordId, out string message);
							Say(message);
							return true;
						});
						row.AddChild(askReport);
					}

					if (kind == PlayerDesk.StopKind.Shop) {
						// Directive §9: window card -- a bounded, stackable print buy. Needs the record
						// already placed here (BuyWindowCard checks it) -- the second thing you do, not the first.
						var windowCard = Btn($"WINDOW CARD (~{PlayerDesk.WindowCardMinutes / 60}h)");
						windowCard.CustomMinimumSize = new Vector2(150, 32);
						windowCard.Pressed += () => Act(() => {
							(string recordId, _, _) = onHand[Mathf.Clamp(singlePick.Selected, 0, onHand.Count - 1)];
							PlayerDesk.Instance.BuyWindowCard(stop.StopId, recordId, out string message);
							Say(message);
							return true;
						});
						row.AddChild(windowCard);

						// Directive §9: in-store appearance -- needs an act with real local standing, so
						// it's the second thing you do here too.
						var inStore = Btn($"IN-STORE (~{PlayerDesk.InStoreAppearanceHours}h)");
						inStore.CustomMinimumSize = new Vector2(140, 32);
						inStore.Pressed += () => Act(() => {
							(string recordId, _, _) = onHand[Mathf.Clamp(singlePick.Selected, 0, onHand.Count - 1)];
							PlayerDesk.Instance.BookInStoreAppearance(stop.StopId, recordId, out string message);
							Say(message);
							return true;
						});
						row.AddChild(inStore);

						// Directive §7.3: the dishonest verb -- Fixer-gated, only at a reporting dealer,
						// never once he's burned. A small quantity spinner rather than a fixed count --
						// "12 copies" was the historical tell, "1" barely moves a survey.
						if ((stop.ReportsToTrades || stop.ReportsToStationIds.Count > 0) && desk.KnowsWhoReports(stop.StopId) && !stop.HypeBurned
								&& desk.InstinctProfile.TheFixer >= PlayerDesk.HypeTheCountMinFixer) {
							var hypeCount = Spin(1, 25, 1, 5);
							hypeCount.CustomMinimumSize = new Vector2(60, 32);
							row.AddChild(hypeCount);
							var hype = Btn($"HYPE THE COUNT (~{PlayerDesk.HypeTheCountMinutes}m)");
							hype.CustomMinimumSize = new Vector2(180, 32);
							hype.Pressed += () => Act(() => {
								(string recordId, _, _) = onHand[Mathf.Clamp(singlePick.Selected, 0, onHand.Count - 1)];
								PlayerDesk.Instance.HypeTheCount(stop.StopId, recordId, (int)hypeCount.Value, out string message);
								Say(message);
								return true;
							});
							row.AddChild(hype);
						}
					}

					// Directive §7: a hired runner's route is built one stop at a time, right where you'd
					// otherwise work the account yourself -- toggle it on or off here.
					if (desk.HasRunner) {
						bool onRoute = desk.IsOnRunnerRoute(stop.StopId);
						var routeBtn = Btn(onRoute ? "✓ RUNNER" : "SEND RUNNER");
						routeBtn.CustomMinimumSize = new Vector2(120, 32);
						routeBtn.Pressed += () => Act(() => {
							PlayerDesk.Instance.AssignRunnerStop(stop.StopId, !onRoute, out string message);
							Say(message);
							return true;
						});
						row.AddChild(routeBtn);
					}

					content.AddChild(row);
				}
			}
		}

		if (!desk.AtHome) {
			var home = Btn("DRIVE HOME");
			home.CustomMinimumSize = new Vector2(160, 40);
			home.Pressed += () => Act(() => { PlayerDesk.Instance.DriveHome(out string message); Say(message); return true; });
			content.AddChild(home);
		}

		// --- ASSEMBLE A SINGLE (office only): pair two shelf masters so it can be pressed and, later, dated ---
		if (desk.AtHome) {
			Heading("ASSEMBLE A SINGLE");
			Body("A 45 is an A-side (the plug side that chases the chart) and a different B-side on the flip. Pair " +
				"them here, then send the single to the plant below — you set its release date once you know the turnaround.");
			List<PlayerDesk.Master> shelf = desk.Masters.Where(master => !master.Scheduled).ToList();
			if (shelf.Count < 2) Body("You need two masters on the shelf — cut an A-side and a B-side first.");
			else {
				var aRow = new HBoxContainer();
				aRow.AddThemeConstantOverride("separation", 10);
				aRow.AddChild(FormLabel("A-side"));
				var aPicker = Option();
				aPicker.CustomMinimumSize = new Vector2(460, 36);
				foreach (PlayerDesk.Master master in shelf)
					aPicker.AddItem($"\"{master.SongTitle}\" — {master.Record.artistName}");
				aRow.AddChild(aPicker);
				content.AddChild(aRow);

				var bRow = new HBoxContainer();
				bRow.AddThemeConstantOverride("separation", 10);
				bRow.AddChild(FormLabel("B-side"));
				var bPicker = Option();
				bPicker.CustomMinimumSize = new Vector2(460, 36);
				foreach (PlayerDesk.Master master in shelf)
					bPicker.AddItem($"\"{master.SongTitle}\" — {master.Record.artistName}");
				bPicker.Selected = 1;
				bRow.AddChild(bPicker);
				content.AddChild(bRow);

				var assemble = Btn("ASSEMBLE THE SINGLE");
				assemble.CustomMinimumSize = new Vector2(300, 42);
				assemble.Pressed += () => Act(() => {
					PlayerDesk.Master aSide = shelf[Mathf.Clamp(aPicker.Selected, 0, shelf.Count - 1)];
					PlayerDesk.Master bSide = shelf[Mathf.Clamp(bPicker.Selected, 0, shelf.Count - 1)];
					PlayerDesk.Instance.AssembleSingle(aSide, bSide, out string message);
					Say(message);
					return true;
				});
				content.AddChild(assemble);
			}
		}

		// --- PRESSING (office only) ---
		Heading("THE PRESSING PLANT");
		Body($"First pressing needs {PlayerDesk.PressMinimumOrder}. About {PlayerDesk.PressVinylPerUnit + PlayerDesk.PressSleeveLabelPerUnit:F2}/disc " +
			$"plus ${PlayerDesk.PressLacquerSetup:N0} lacquer setup (once per title) and ${PlayerDesk.PressShipping:N0} for sleeves, labels and freight. " +
			$"Once a title's stampers are cut, a repress can run as low as {PlayerDesk.PressReorderMinimum} with no lacquer fee. " +
			"A run is a whole 45 — both sides on the one disc — and the plant takes weeks to turn it round.");
		if (!desk.AtHome) Body("You place a run from the office — drive home to order.");
		else {
			List<(string RecordId, string Title, bool InMarket)> singles = desk.PressableSingles().ToList();
			if (singles.Count == 0) Body("Assemble a single first — you press a finished 45, both sides together.");
			else {
				var pickRow = new HBoxContainer();
				pickRow.AddThemeConstantOverride("separation", 10);
				var singlePicker = Option();
				singlePicker.CustomMinimumSize = new Vector2(460, 36);
				foreach ((string recordId, string title, bool inMarket) in singles) {
					PlayerDesk.PressStock stock = desk.StockFor(recordId);
					bool repressable = desk.HasBeenPressed(recordId);
					singlePicker.AddItem($"\"{title}\"{(inMarket ? "" : " (upcoming)")}  —  {(stock?.Remaining ?? 0):N0} sellable, {(stock?.PromoRemaining ?? 0):N0} promo in the office{(repressable ? " (repress)" : "")}");
				}
				pickRow.AddChild(singlePicker);
				pickRow.AddChild(FormLabel("Qty"));
				int firstMin = desk.MinimumPressRun(singles[0].RecordId);
				var qtyInput = Spin(firstMin, 100000, 100, firstMin);
				pickRow.AddChild(qtyInput);
				pickRow.AddChild(FormLabel("Promo"));
				// Directive §3.1: "Suggested UI default on a first run: 120 of 500" -- roughly a quarter
				// of the minimum run, capped at PressPromoCapFraction. A repress carries no cap, and
				// opens at zero (see PlayerDesk.SuggestedPromoCount).
				var promoInput = Spin(0, firstMin, 10, desk.SuggestedPromoCount(singles[0].RecordId, firstMin));
				pickRow.AddChild(promoInput);
				content.AddChild(pickRow);

				var runCost = new Label();
				runCost.AddThemeColorOverride("font_color", Rust);
				content.AddChild(runCost);
				void UpdateRunCost() {
					string recordId = singles[Mathf.Clamp(singlePicker.Selected, 0, singles.Count - 1)].RecordId;
					bool repress = desk.HasBeenPressed(recordId);
					int qty = (int)qtyInput.Value;
					float cost = PlayerDesk.PressingCost(qty, repress);
					int promoCap = desk.MaxPromoCount(recordId, qty);
					promoInput.MaxValue = promoCap;
					if (promoInput.Value > promoCap) promoInput.Value = promoCap;
					int promo = (int)promoInput.Value;
					float promoValue = promo * PlayerDesk.SinglePrice;
					runCost.Text = $"Run cost: ${cost:N0}  (${cost / Math.Max(1.0, qty):F2}/disc){(repress ? " — repress, no lacquer fee" : "")}"
						+ (promo > 0 ? $"   ·   {promo:N0} promo = ~${promoValue:N0} of sales given away" : "")
						+ (repress ? "" : $"   ·   promo capped at {promoCap:N0} ({PlayerDesk.PressPromoCapFraction:P0})");
				}
				qtyInput.ValueChanged += _ => UpdateRunCost();
				promoInput.ValueChanged += _ => UpdateRunCost();
				singlePicker.ItemSelected += idx => {
					string pickedId = singles[Mathf.Clamp((int)idx, 0, singles.Count - 1)].RecordId;
					int minRun = desk.MinimumPressRun(pickedId);
					qtyInput.MinValue = minRun;
					if (qtyInput.Value < minRun) qtyInput.Value = minRun;
					// A different title is a fresh ticket -- re-open on that title's suggested promo split
					// rather than carrying the last one's number across.
					promoInput.Value = desk.SuggestedPromoCount(pickedId, (int)qtyInput.Value);
					UpdateRunCost();
				};
				UpdateRunCost();

				var order = Btn("ORDER PRESSING");
				order.CustomMinimumSize = new Vector2(240, 42);
				order.Pressed += () => Act(() => {
					(string recordId, _, _) = singles[Mathf.Clamp(singlePicker.Selected, 0, singles.Count - 1)];
					PlayerDesk.Instance.OrderPressing(recordId, (int)qtyInput.Value, (int)promoInput.Value, out string message);
					Say(message);
					return true;
				});
				content.AddChild(order);

				// Press-to-fill (directive §11): size a run off real open-call backlog instead of a guess.
				(string RecordId, string Title, bool InMarket) picked = singles[Mathf.Clamp(singlePicker.Selected, 0, singles.Count - 1)];
				int fillDemand = desk.OpenCallDemand(picked.RecordId);
				if (fillDemand > 0) {
					int fillQty = desk.PressToFillQuantity(picked.RecordId);
					var fill = Btn($"PRESS TO FILL OPEN CALLS  (~{fillQty:N0}, {fillDemand:N0} asked for)");
					fill.CustomMinimumSize = new Vector2(300, 42);
					fill.Pressed += () => Act(() => {
						PlayerDesk.Instance.PressToFill(picked.RecordId, out string message);
						Say(message);
						return true;
					});
					content.AddChild(fill);
				}

				// Plant credit (directive §11: "a mid-game gun, not a tutorial crutch").
				var creditOwed = desk.PlantCreditOwed;
				if (creditOwed.HasValue) {
					var (creditRecordId, amount, weeksAway) = creditOwed.Value;
					string creditTitle = singles.FirstOrDefault(s => s.RecordId == creditRecordId).Title ?? creditRecordId;
					Body($"Plant credit outstanding: ${amount:N0} due on \"{creditTitle}\" in {weeksAway} week(s) — it collects on schedule whether or not the record's still moving.");
				} else if (desk.PlantCreditEligible(picked.RecordId)) {
					float creditCost = PlayerDesk.PressingCost(PlayerDesk.PlantCreditQuantity, desk.HasBeenPressed(picked.RecordId));
					Body($"\"{picked.Title}\" is moving enough that the plant would front a run: {PlayerDesk.PlantCreditQuantity:N0} units, " +
						$"nothing down, ${creditCost:N0} due in {PlayerDesk.PlantCreditTermWeeks} weeks — no questions asked till then.");
					var creditBtn = Btn($"TAKE THE CREDIT RUN  ({PlayerDesk.PlantCreditHours}h)");
					creditBtn.CustomMinimumSize = new Vector2(260, 42);
					creditBtn.Pressed += () => Act(() => {
						PlayerDesk.Instance.RequestPlantCredit(picked.RecordId, out string message);
						Say(message);
						return true;
					});
					content.AddChild(creditBtn);
				}
			}
		}

		var pending = desk.PendingPressings().ToList();
		if (pending.Count > 0) {
			Body("At the plant now:");
			foreach ((string title, int quantity, GameDate arrives) in pending)
				Body($"    {quantity:N0} of \"{title}\"  —  due {arrives.ToHeadlineString()}");
		}

		// --- THE MAILING (office only, directive §5): the only way to touch a market you can't drive to ---
		Heading("THE MAILING");
		Body($"Office only. {ActionCosts.Planning}h for up to {PlayerDesk.MailingFreePieces} pieces, plus an hour per further " +
			$"{PlayerDesk.MailingPiecesPerExtraHour}. About ${PlayerDesk.MailerCostPerCopy:F2}/copy for the mailer and postage. " +
			"Most of it lands in the bin — what lands only just gets him listening.");
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
				var mailCount = Spin(1, 500, 5, 25);
				mailRow.AddChild(mailCount);
				content.AddChild(mailRow);

				var mailBtn = Btn("MAIL THE LIST");
				mailBtn.CustomMinimumSize = new Vector2(200, 42);
				mailBtn.Pressed += () => Act(() => {
					if (promoOnHand.Count == 0) return false;
					string recordId = promoOnHand[Mathf.Clamp(mailSinglePick.Selected, 0, promoOnHand.Count - 1)].RecordId;
					string regionId = regions[Mathf.Clamp(regionPick.Selected, 0, regions.Count - 1)].regionId;
					PlayerDesk.Instance.MailPromoCopies(recordId, regionId, (int)mailCount.Value, out string message);
					Say(message);
					return true;
				});
				content.AddChild(mailBtn);
			}
		}

		// --- THE TRADES (office only, directive §6.1/§6.3): the review desk and the breakout column ---
		Heading("THE TRADES");
		Body($"One submission per record, {ActionCosts.Paperwork}h and one promo copy plus ${PlayerDesk.TradeReviewPostage:F2} postage. " +
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
					var row = new HBoxContainer();
					row.AddThemeConstantOverride("separation", 10);
					row.AddChild(new Label { Text = $"    \"{title}\"", CustomMinimumSize = new Vector2(260, 32) });

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
					row.AddChild(new Label { Text = status, CustomMinimumSize = new Vector2(360, 32) });

					if (!desk.HasEverSubmittedToTrade(recordId)) {
						var submitBtn = Btn("SUBMIT TO REVIEW DESK");
						submitBtn.CustomMinimumSize = new Vector2(220, 32);
						submitBtn.Pressed += () => Act(() => {
							PlayerDesk.Instance.SubmitToReviewDesk(recordId, out string message);
							Say(message);
							return true;
						});
						row.AddChild(submitBtn);
					}
					content.AddChild(row);

					// Directive §6.2: a guaranteed, paid version of the same signal -- $75/$250/$600, era
					// rate, not a genuine gamble tier. A full page is most of an $800 label's cash in one line.
					var adRow = new HBoxContainer();
					adRow.AddThemeConstantOverride("separation", 10);
					adRow.AddChild(new Label { Text = "        Trade ad:", CustomMinimumSize = new Vector2(120, 28) });
					foreach (TradeAdTier tier in new[] { TradeAdTier.QuarterPage, TradeAdTier.HalfPage, TradeAdTier.FullPage }) {
						var adBtn = Btn($"{PlayerDesk.TradeAdTierName(tier).ToUpper()} (${PlayerDesk.TradeAdCost(tier):N0})");
						adBtn.CustomMinimumSize = new Vector2(160, 28);
						adBtn.Pressed += () => Act(() => {
							PlayerDesk.Instance.BuyTradeAd(recordId, tier, out string message);
							Say(message);
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

		// --- SET THE RELEASE DATE (office only): date an assembled single now the plant's quoted a turnaround ---
		if (desk.AtHome) {
			Heading("SET THE RELEASE DATE");
			Body($"Costs {PlayerDesk.ScheduleHours} hours. Date an assembled single for after its vinyl lands — check " +
				"the plant's due dates above. Where it's sold is set below.");
			Body("The campaign is shipping samples and a trade announcement, charged the day it ships — not a way to " +
				"buy a hit. An $800 label leaves it at zero and earns its awareness on the road: promo copies in " +
				"jocks' hands, the mailing, the review desk.");
			List<PlayerDesk.PlannedRelease> undated = desk.UndatedSingles().ToList();
			if (undated.Count == 0) Body("No single assembled and waiting on a date.");
			else {
				var pickRow = new HBoxContainer();
				pickRow.AddThemeConstantOverride("separation", 10);
				var singleDatePick = Option();
				singleDatePick.CustomMinimumSize = new Vector2(460, 36);
				foreach (PlayerDesk.PlannedRelease single in undated)
					singleDatePick.AddItem($"\"{single.Master.SongTitle}\"{(single.BSide != null ? $" b/w \"{single.BSide.SongTitle}\"" : "")} — {single.Master.Record.artistName}");
				pickRow.AddChild(singleDatePick);
				content.AddChild(pickRow);

				var daysRow = new HBoxContainer();
				daysRow.AddThemeConstantOverride("separation", 10);
				daysRow.AddChild(FormLabel("Ships in (days)"));
				var daysInput = Spin(1, 120, 1, 21);
				daysRow.AddChild(daysInput);
				daysRow.AddChild(FormLabel("Campaign ($)"));
				// Promo mechanic directive §11: "default the field to $0." The awareness a player record
				// earns is supposed to come off the verbs on this branch -- serviced jocks, the mailing,
				// the trades, the road -- not off a slider. A pre-filled figure taught the exact opposite
				// lesson on the one screen where it mattered most.
				var budgetInput = Spin(0, 50000, 5, 0);
				daysRow.AddChild(budgetInput);
				content.AddChild(daysRow);

				var setDate = Btn($"SET THE DATE  ({PlayerDesk.ScheduleHours}h)");
				setDate.CustomMinimumSize = new Vector2(300, 44);
				setDate.Pressed += () => Act(() => {
					PlayerDesk.PlannedRelease single = undated[Mathf.Clamp(singleDatePick.Selected, 0, undated.Count - 1)];
					PlayerDesk.Instance.SetReleaseDate(single, (int)daysInput.Value, (float)budgetInput.Value, out string message);
					Say(message);
					return true;
				});
				content.AddChild(setDate);
			}
		}

		// --- ARTIST BUY-IN (office only): an act buys a run of its own pressed single outright, cash now ---
		ArtistBuyInSection(desk);

		// --- STOCK OUT IN THE TOWNS ---
		Heading("OUT IN THE TOWNS");
		var stopStock = desk.StopStock().OrderBy(s => s.CityName).ThenBy(s => s.StopName).ToList();
		if (stopStock.Count == 0) Body("No stock out at any account yet. Press a run, then drive it out and pitch or consign it.");
		else
			foreach ((string cityName, string stopName, string title, int remaining) in stopStock)
				Body($"    {cityName} — {stopName}: {remaining:N0} of \"{title}\" left on the shelves");

		var owedByStop = desk.OpenBalancesByStop().OrderByDescending(t => t.Amount).ToList();
		if (owedByStop.Count > 0) {
			Body("Waiting to be collected (drive back to pocket the lump; a thin wire trickles in meanwhile):");
			foreach ((string cityName, string stopName, float amount) in owedByStop)
				Body($"    {cityName} — {stopName}: ${amount:N0} they're holding for you");
		}

		// --- P&D DISTRIBUTION DEAL (directive §9) ---
		Heading("P&D DISTRIBUTION DEAL");
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
			acceptBtn.Pressed += () => Act(() => { PlayerDesk.Instance.AcceptDistributionOffer(out string message); Say(message); return true; });
			offerRow.AddChild(acceptBtn);
			var declineBtn = Btn("WALK AWAY");
			declineBtn.CustomMinimumSize = new Vector2(140, 40);
			declineBtn.Pressed += () => Act(() => { PlayerDesk.Instance.DeclineDistributionOffer(out string message); Say(message); return true; });
			offerRow.AddChild(declineBtn);
			content.AddChild(offerRow);
		} else {
			Body("A pressing-and-distribution deal covers the whole catalog for the term, not one title: an advance " +
				"(maybe), a real cut of everything, and often the masters, in trade for a distributor's own national " +
				"network. Only worth pitching once a record's proven itself regionally — otherwise nobody's biting.");
			var pitchBtn = Btn($"PITCH FOR A DEAL ({PlayerDesk.SignHours}h)");
			pitchBtn.CustomMinimumSize = new Vector2(220, 40);
			pitchBtn.Pressed += () => Act(() => { PlayerDesk.Instance.PursueDistributionDeal(out string message); Say(message); return true; });
			content.AddChild(pitchBtn);
		}

		// --- WHOLESALE HOUSES (the gamble) ---
		Heading("WHOLESALE HOUSES — THE GAMBLE");
		Body("Some markets are too far to drive a line to. Hand it to a wholesale house out there and they'll press " +
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
				Text = $"{region.regionName}  —  {houses} house(s) with room  —  " +
					(proven ? "they've heard of you here" : "no proof here yet — a cold pitch")
			};
			text.AddThemeColorOverride("font_color", proven ? Ink : Rust);
			row.AddChild(text);

			var place = Btn(proven ? $"TAKE THE MEETING ({PlayerDesk.DistributionHours}h)" : $"GAMBLE A LINE ({PlayerDesk.DistributionHours}h)");
			place.CustomMinimumSize = new Vector2(220, 40);
			string capturedId = region.regionId;
			place.Pressed += () => Act(() => { PlayerDesk.Instance.PlaceLine(capturedId, out string message); Say(message); return true; });
			row.AddChild(place);
			content.AddChild(row);
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

		Heading("ARTIST BUY-IN");
		Body($"An act will take {PlayerDesk.ArtistBuyInMin}-{PlayerDesk.ArtistBuyInMax} of its own single off your hands " +
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
			PlayerDesk.Instance.ArtistBuyIn(artist, recordId, (int)qtyInput.Value, out string message);
			Say(message);
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
		Body($"Cash on hand: ${label.cashReserves:N0}\n" +
			$"Owed to you by wholesalers: ${owed:N0}\n" +
			$"Written off to short payment and under-reporting: ${label.lifetimeWholesaleWriteOffs:N0}\n" +
			$"Monthly overhead: ${label.GetMonthlyOverhead():N0}   •   last month's profit: ${label.lastMonthlyProfit:N0}");

		Heading("LAST WEEK'S SETTLEMENT");
		PlayerDesk.WeekBooks latest = desk.Books.FirstOrDefault();
		if (latest == null) Body("No week has settled yet. The chart settles on Fridays.");
		else {
			Body($"{latest.Units:N0} units sold");
			var settlementRows = new List<string[]> {
				new[] { "retail gross", $"${latest.Gross:N0}" },
				new[] { "− manufacturing", $"${latest.ManufacturingCost:N0}" },
				new[] { "− distributor's skim", $"${latest.DistributionSkim:N0}" },
				new[] { "− artist royalty", $"${latest.ArtistRoyalty:N0}" }
			};
			if (latest.RunnerCommission > 0f)
				settlementRows.Add(new[] { "− runner's commission", $"${latest.RunnerCommission:N0}" });
			settlementRows.Add(new[] { "= earned", $"${latest.Earned:N0}" });
			settlementRows.Add(new[] { "− billed on credit", $"${latest.Deferred:N0}" });
			if (latest.TrunkHeld > 0f)
				settlementRows.Add(new[] { "− held by the towns", $"${latest.TrunkHeld:N0}" });
			settlementRows.Add(new[] { "+ old invoices paid", $"${latest.Collected:N0}" });
			settlementRows.Add(new[] { "= reached the bank", $"${latest.Banked:N0}" });
			Table(null, settlementRows);
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
					PlayerDesk.Instance.FactorReceivable(idx, out string message);
					Say(message);
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
			Table(new[] { "week ending", "units", "earned", "banked", "owed you", "cash" },
				weeks.Select(week => new[] {
					week.Date.ToHeadlineString(), $"{week.Units:N0}", $"${week.Earned:N0}",
					$"${week.Banked:N0}", $"${week.Outstanding:N0}", $"${week.Cash:N0}"
				}));

		Heading("RECORD BY RECORD");
		var released = desk.ReleasedRecords.OrderByDescending(record => record.lifetimeLabelNet).ToList();
		if (released.Count == 0) Body("Nothing released yet.");
		else
			foreach (RecordRuntimeData record in released) {
				float net = record.lifetimeLabelNet;
				float cost = record.sunkProductionCost;
				// Fold in this week's trunk units whose money is already booked but whose count hasn't
				// settled yet, so dollars-per-unit reads straight mid-week.
				long unitsLifetime = record.totalUnitsSold + desk.PendingTrunkUnits(record.baseRecord.recordId);
				string recordId = record.baseRecord.recordId;
				Body($"\"{record.baseRecord.title}\" — {record.baseRecord.artistName}\n" +
					$"    {unitsLifetime:N0} units lifetime   •   {record.unitsThisWeek:N0} this week   •   " +
					$"{(record.peakPosition > 0 ? $"peak #{record.peakPosition}" : "uncharted")}\n" +
					$"    earned ${net:N0} against ${cost:N0} of tape   •   " +
					$"{(net >= cost ? $"in the black by ${net - cost:N0}" : $"${cost - net:N0} still to make back")}");

				// Directive §9: a one-off transaction on this one title, distinct from the P&D deal
				// above (which covers the whole catalog). Only on the table once a station and a
				// one-stop both know it -- MasterDealEligible is the single source of truth for that.
				if (desk.IsMasterOut(recordId)) {
					Body("    the master's out on this one -- not yours to sell right now.");
				} else if (desk.MasterDealEligible(recordId)) {
					var dealRow = new HBoxContainer();
					dealRow.AddThemeConstantOverride("separation", 10);
					var leaseBtn = Btn($"LEASE THE MASTER (${desk.MasterLeaseValue(recordId):N0}, {PlayerDesk.MasterLeaseTermWeeks}wk)");
					leaseBtn.CustomMinimumSize = new Vector2(260, 36);
					leaseBtn.Pressed += () => Act(() => { PlayerDesk.Instance.LeaseMaster(recordId, out string message); Say(message); return true; });
					dealRow.AddChild(leaseBtn);
					var sellBtn = Btn($"SELL THE MASTER (${desk.MasterSaleValue(recordId):N0})");
					sellBtn.CustomMinimumSize = new Vector2(220, 36);
					sellBtn.Pressed += () => Act(() => { PlayerDesk.Instance.SellMaster(recordId, out string message); Say(message); return true; });
					dealRow.AddChild(sellBtn);
					content.AddChild(dealRow);
				}
			}

		Heading("ARTIST ACCOUNTS");
		var roster = desk.Roster.ToList();
		if (roster.Count == 0) { Body("Nobody signed."); return; }
		foreach (SimulatedArtist artist in roster)
			Body($"{artist.stageName} — ${artist.unrecoupedAdvance:N0} unrecouped   •   " +
				$"${artist.totalRoyaltyEarnings:N0} paid through   •   {artist.royaltyRate:P1} of retail");
	}

	// ========================================================================
	// ROLODEX
	// ========================================================================

	// Mouse wheel spins through the Rolodex cards when that tab is open.
	public override void _GuiInput(InputEvent ev) {
		if (currentTab == RolodexTab && PlayerDesk.Instance?.ActiveCall == null && ev is InputEventMouseButton mb && mb.Pressed) {
			var cards = PlayerDesk.Instance?.Rolodex;
			if (cards != null && cards.Count > 1) {
				if (mb.ButtonIndex == MouseButton.WheelDown) {
					rolodexFocus = (rolodexFocus + 1) % cards.Count;
					GetViewport().SetInputAsHandled();
					Refresh();
				} else if (mb.ButtonIndex == MouseButton.WheelUp) {
					rolodexFocus = ((rolodexFocus - 1) + cards.Count) % cards.Count;
					GetViewport().SetInputAsHandled();
					Refresh();
				}
			}
		}
	}

	private void PageRolodex() {
		PlayerDesk desk = PlayerDesk.Instance;

		// A live call takes over the page entirely -- you are on the phone, not browsing a book.
		if (desk.ActiveCall != null && desk.ActiveCall.stage != CallStage.Ended) {
			PageCall(desk, desk.ActiveCall);
			return;
		}

		var cards = desk.Rolodex;
		if (cards.Count == 0) {
			Heading("THE ROLODEX");
			Body("Your book is empty. Nobody in this business knows your name yet, and nobody is going " +
				"to call you first. Get on the phone.");
			RenderWorkThePhones(desk);
			return;
		}

		rolodexFocus = Mathf.Clamp(rolodexFocus, 0, cards.Count - 1);
		RolodexEntry entry = cards[rolodexFocus];

		var navRow = new HBoxContainer();
		navRow.AddThemeConstantOverride("separation", 10);
		if (cards.Count > 1) {
			var prev = Btn("‹");
			prev.CustomMinimumSize = new Vector2(44, 38);
			prev.Pressed += () => { rolodexFocus = ((rolodexFocus - 1) + cards.Count) % cards.Count; Refresh(); };
			navRow.AddChild(prev);
		}
		var cardCount = new Label { Text = $"Card {rolodexFocus + 1} of {cards.Count}", SizeFlagsHorizontal = SizeFlags.ExpandFill };
		cardCount.AddThemeColorOverride("font_color", Heard);
		navRow.AddChild(cardCount);
		if (cards.Count > 1) {
			var next = Btn("›");
			next.CustomMinimumSize = new Vector2(44, 38);
			next.Pressed += () => { rolodexFocus = (rolodexFocus + 1) % cards.Count; Refresh(); };
			navRow.AddChild(next);
		}
		content.AddChild(navRow);

		RenderCard(desk, entry);
	}

	/// <summary>The focused card: portrait monogram, identity, tier, what you know about his hours, and
	/// what he is currently carrying for you.</summary>
	private void RenderCard(PlayerDesk desk, RolodexEntry entry) {
		Deejay dj = ChartManager.Instance?.GetDeejay(entry.djId);
		RadioStation station = ChartManager.Instance?.GetRadioStation(entry.stationId);

		var mono = new Label {
			Text = Monogram(entry.displayName),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			CustomMinimumSize = new Vector2(90, 90)
		};
		mono.AddThemeFontSizeOverride("font_size", 32);
		mono.AddThemeColorOverride("font_color", Paper);
		var monoBack = new PanelContainer { CustomMinimumSize = new Vector2(90, 90) };
		monoBack.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = ArchetypeColor(dj?.archetype ?? DJArchetype.CompanyMan) });
		monoBack.AddChild(mono);
		content.AddChild(monoBack);

		Heading(entry.displayName);
		if (station != null) Body($"{station.callsign}  ·  {station.format}  ·  {station.cityName}");

		float rapport = station?.rt?.Rapport(desk.Label?.labelId ?? "") ?? 0f;
		RapportTier tier = RolodexEntry.EffectiveTier(entry, rapport);
		var tierLabel = new Label { Text = $"{RolodexEntry.TierLabel(tier)}  ·  {entry.state}" };
		tierLabel.AddThemeColorOverride("font_color", RolodexEntry.TierColor(tier));
		content.AddChild(tierLabel);

		if (dj != null) Body(RolodexEntry.ArchetypeBlurb(dj.archetype));

		// When to call him. Learned, not given -- an unreached name comes with no hours.
		if (dj != null) {
			Daypart shift = RolodexShifts.ShiftOf(dj);
			if (entry.shiftKnown) {
				int hour = TimeManager.Instance?.CurrentHour ?? 12;
				bool nowGood = RolodexShifts.ReachableAt(shift, hour);
				var hours = new Label { Text = RolodexShifts.WindowAdvice(shift) + (nowGood ? "  ·  He should be in right now." : "  ·  Wrong time of day.") };
				hours.AddThemeColorOverride("font_color", nowGood ? new Color("4a7a4a") : Rust);
				content.AddChild(hours);
			} else {
				Body("You don't know his hours yet. Call and find out the hard way.");
			}
		}

		if (entry.theyOweThem) Body("He owes you one.");
		if (entry.youOweThem) Body("You owe him one.");
		if (entry.payolaBurned) Body("The cash channel is closed with him.");
		if (entry.professionallyBurned) Body("He doesn't take your word any more.");

		// What he is actually carrying for you right now -- the record-level commitment, not the mood.
		RenderCarrying(desk, entry);

		var openBtn = Btn($"PLACE A CALL  ({PlayerDesk.DialMinutes} min)");
		openBtn.CustomMinimumSize = new Vector2(230, 40);
		openBtn.Pressed += () => {
			string rid = rolodexPitchRecordId ?? desk.ReleasedRecords.FirstOrDefault(r => r.baseRecord != null)?.baseRecord.recordId;
			rolodexPitchRecordId = rid;
			desk.PlaceCall(entry, rid, out string msg);
			if (!string.IsNullOrEmpty(msg)) Say(msg);
			Refresh();
		};
		content.AddChild(openBtn);

		if (entry.log.Count > 0) {
			Heading("HISTORY");
			foreach (string line in entry.log) Body(line);
		}

		RenderWorkThePhones(desk);
	}

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
			RecordRuntimeData rec = desk.ReleasedRecords.FirstOrDefault(r => r.baseRecord?.recordId == a.recordId);
			string title = rec?.baseRecord?.title ?? "(unknown record)";
			int weeksLeft = Mathf.Max(0, a.expiresWeek - week + 1);
			SpinTier tier = chart.SpinTierOf(entry.stationId, a.recordId);
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
			string status = tier != SpinTier.None
				? $"ON THE AIR — {PlayerDesk.TierWord(tier)} rotation"
				: a.expired ? "Not picked up. His argument has run out."
				: $"Not on yet — he's still arguing for it ({weeksLeft} more meeting(s))";
			var head = new Label { Text = $"  \"{title}\" — {status}", AutowrapMode = TextServer.AutowrapMode.WordSmart };
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

		Heading($"CALLING {call.entry.displayName.ToUpperInvariant()}");
		if (c.station != null)
			Body($"{c.station.callsign}  ·  {c.station.format}  ·  {c.station.cityName}  ·  " +
				$"{RolodexEntry.TierLabel(c.tier)}");

		// The record on the table. Switching it rebuilds the situation read.
		var records = desk.ReleasedRecords.Where(r => r.baseRecord != null).ToList();
		if (records.Count > 0 && call.stage is CallStage.Open) {
			var pickRow = new HBoxContainer();
			pickRow.AddThemeConstantOverride("separation", 6);
			pickRow.AddChild(new Label { Text = "On the table:" });
			foreach (RecordRuntimeData rec in records) {
				bool picked = rec.baseRecord.recordId == call.recordId;
				var recBtn = Btn($"{(picked ? "» " : "")}{rec.baseRecord.title}");
				string rid = rec.baseRecord.recordId;
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
		var again = Btn($"TRY AGAIN  ({PlayerDesk.DialMinutes} min)");
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

		var hang = Btn("HANG UP");
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
		var more = Btn("KEEP HIM ON THE LINE");
		more.Pressed += () => { desk.ContinueCall(call); Refresh(); };
		content.AddChild(more);
		var hang = Btn("HANG UP");
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
			PlayerDesk.Instance.WorkThePhones(out string msg);
			Say(msg);
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

	private void PageOffice() {
		PlayerDesk desk = PlayerDesk.Instance;

		// Character card: who you are and what you can read.
		FoundingArchetypeData.ArchetypeProfile archProfile = FoundingArchetypeData.Get(desk.Archetype);
		ExecutiveInstinctProfile inst = desk.InstinctProfile;
		Heading($"{archProfile.Name.ToUpperInvariant()}");
		Body(archProfile.Tagline);
		Body($"THE EAR {StarBar(inst.TheEar / 5f)} ({inst.TheEar})   " +
			$"THE STREET {StarBar(inst.TheStreet / 5f)} ({inst.TheStreet})   " +
			$"THE SUIT {StarBar(inst.TheSuit / 5f)} ({inst.TheSuit})   " +
			$"THE FIXER {StarBar(inst.TheFixer / 5f)} ({inst.TheFixer})");

		PhoneSection(desk);
		StaffSection(desk);

		Heading("THE LEDGER");
		IReadOnlyList<string> entries = desk.Log;
		if (entries.Count == 0) { Body("Nothing has happened yet."); return; }
		foreach (string entry in entries) Body(entry);
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
			hire.Pressed += () => Act(() => { PlayerDesk.Instance.PurchaseAnsweringService(out string message); Say(message); return true; });
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
				hire.Pressed += () => Act(() => { PlayerDesk.Instance.HireRunner(out string message); Say(message); return true; });
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
				var qty = Spin(1, 5000, 10, 100);
				row.AddChild(qty);
				var hand = Btn($"HAND OFF ({PlayerDesk.RunnerHandoffHours}h)");
				hand.Pressed += () => Act(() => {
					(string recordId, _, _) = onHand[Mathf.Clamp(pick.Selected, 0, onHand.Count - 1)];
					PlayerDesk.Instance.HandCartonToRunner(recordId, (int)qty.Value, out string message);
					Say(message);
					return true;
				});
				row.AddChild(hand);
				content.AddChild(row);
			}

			// His route is toggled per account from DISTRIBUTION -- this is just the tally, gathered from
			// every town the player has personally opened (the only towns he's allowed to cover).
			int onRoute = desk.WorkedCities
				.SelectMany(cityId => desk.StopsInCity(cityId))
				.Count(stop => desk.IsOnRunnerRoute(stop.StopId));
			Body($"Route: {onRoute} account(s) across your opened towns. Add or drop one from a stop's row in DISTRIBUTION.");
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
				PlayerDesk.Instance.HireProjectPromo(r.baseRecord.recordId, c.cityId, projectPromoTier, out string message);
				Say(message);
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
		_ => "called"
	};

	// ========================================================================
	// SMALL HELPERS
	// ========================================================================

	private void Heading(string text) {
		var node = new Label { Text = text };
		node.AddThemeFontSizeOverride("font_size", 20);
		node.AddThemeColorOverride("font_color", Rust);
		content.AddChild(node);
	}

	/// <summary>
	/// A real grid, because the default font is proportional and space-padded columns do not line up
	/// in it. Pass null headers for an unheaded two- or three-column list.
	/// </summary>
	private void Table(string[] headers, IEnumerable<string[]> rows) {
		var materialized = rows.ToList();
		int columns = headers?.Length ?? materialized.FirstOrDefault()?.Length ?? 0;
		if (columns == 0) return;

		var grid = new GridContainer { Columns = columns };
		grid.AddThemeConstantOverride("h_separation", 26);
		grid.AddThemeConstantOverride("v_separation", 4);
		content.AddChild(grid);

		if (headers != null)
			foreach (string header in headers) {
				var cell = new Label { Text = header };
				cell.AddThemeFontSizeOverride("font_size", 14);
				cell.AddThemeColorOverride("font_color", new Color("8a6a3a"));
				grid.AddChild(cell);
			}

		foreach (string[] row in materialized)
			for (int column = 0; column < columns; column++) {
				var cell = new Label {
					Text = column < row.Length ? row[column] : string.Empty,
					// Figures read right-aligned; the first column is the label for the row.
					HorizontalAlignment = column == 0 ? HorizontalAlignment.Left : HorizontalAlignment.Right,
					SizeFlagsHorizontal = SizeFlags.ExpandFill
				};
				cell.AddThemeFontSizeOverride("font_size", 15);
				cell.AddThemeColorOverride("font_color", Ink);
				grid.AddChild(cell);
			}
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
				var visit = Btn($"VISIT WAREHOUSE (~{PlayerDesk.OneStopVisitHours}h)");
				visit.CustomMinimumSize = new Vector2(200, 32);
				visit.Pressed += () => Act(() => {
					PlayerDesk.Instance.VisitOneStopWarehouse(stop.StopId, out string message);
					Say(message);
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

		SpinBox qty = Spin(1, PlayerDesk.OneStopCartonMax, 10, PlayerDesk.OneStopCartonDefault);
		row.AddChild(qty);

		var sell = Btn($"SELL CARTON (~{PlayerDesk.OneStopVisitHours}h)");
		sell.CustomMinimumSize = new Vector2(160, 32);
		sell.Pressed += () => Act(() => {
			if (onHand.Count == 0) return false;
			(string recordId, _, _) = onHand[Mathf.Clamp(singlePick.Selected, 0, onHand.Count - 1)];
			PlayerDesk.Instance.SellCartonToOneStop(stop.StopId, recordId, Mathf.RoundToInt((float)qty.Value), out string message);
			Say(message);
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

		var stopLabel = new Label {
			Text = $"    {stop.DisplayName} ({relWord})",
			CustomMinimumSize = new Vector2(400, 32)
		};
		stopLabel.AddThemeColorOverride("font_color", Ink);
		row.AddChild(stopLabel);

		int estHours = PlayerDesk.EstimatedStopHours(PlayerDesk.StopKind.Venue);
		var work = Btn($"WORK THE TABLE (~{estHours}h)");
		work.CustomMinimumSize = new Vector2(170, 32);
		work.Pressed += () => Act(() => {
			if (onHand.Count == 0) return false;
			(string recordId, _, _) = onHand[Mathf.Clamp(singlePick.Selected, 0, onHand.Count - 1)];
			PlayerDesk.Instance.WorkTheHopTable(stop.StopId, recordId, out string message);
			Say(message);
			return true;
		});
		row.AddChild(work);

		// Directive §8: the record hop -- the act appears, an MC'd table moves several times what a
		// bare one does, and (win or lose the room) a jock trusted enough to book gets a real advocacy
		// swing out of it. BookRecordHop itself checks for a trusted-enough jock in this town.
		var hop = Btn($"BOOK A HOP (~{PlayerDesk.RecordHopHours}h)");
		hop.CustomMinimumSize = new Vector2(170, 32);
		hop.Pressed += () => Act(() => {
			if (onHand.Count == 0) return false;
			(string recordId, _, _) = onHand[Mathf.Clamp(singlePick.Selected, 0, onHand.Count - 1)];
			PlayerDesk.Instance.BookRecordHop(stop.StopId, recordId, out string message);
			Say(message);
			return true;
		});
		row.AddChild(hop);
		return row;
	}

	/// <summary>Promo mechanic directive §4: the station stop -- no shelf, no balance, just the jock
	/// you can walk in on. Draws only from the promo picker's pool, never the sellable one.</summary>
	private Control BuildStationRow(PlayerDesk.PlayerStop stop, List<(string RecordId, string Title, int PromoOnHand)> promoOnHand,
			OptionButton promoPick) {
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 8);

		var stopLabel = new Label {
			Text = $"    {stop.DisplayName}",
			CustomMinimumSize = new Vector2(260, 32)
		};
		stopLabel.AddThemeColorOverride("font_color", Ink);
		row.AddChild(stopLabel);

		string RecordId() => promoOnHand.Count == 0 ? null
			: promoOnHand[Mathf.Clamp(promoPick.Selected, 0, promoOnHand.Count - 1)].RecordId;

		var dropOff = Btn($"DROP OFF (~{PlayerDesk.DropOffMinutes / 60}h)");
		dropOff.CustomMinimumSize = new Vector2(120, 32);
		dropOff.Disabled = promoOnHand.Count == 0;
		dropOff.Pressed += () => Act(() => {
			string recordId = RecordId();
			if (recordId == null) return false;
			PlayerDesk.Instance.DropOffAtStation(stop.StopId, recordId, out string message);
			Say(message);
			return true;
		});
		row.AddChild(dropOff);

		var waitFor = Btn($"WAIT FOR HIM (~{PlayerDesk.WaitForHimMinutes / 60}h)");
		waitFor.CustomMinimumSize = new Vector2(150, 32);
		waitFor.Disabled = promoOnHand.Count == 0;
		waitFor.Pressed += () => Act(() => {
			string recordId = RecordId();
			if (recordId == null) return false;
			RolodexCall call = PlayerDesk.Instance.WaitForHimAtStation(stop.StopId, recordId, out string message);
			Say(message);
			if (call != null) currentTab = RolodexTab; // the pitch opens in person -- jump to the call scene
			return true;
		});
		row.AddChild(waitFor);

		var leaveIt = Btn($"LEAVE W/ DESK (~{PlayerDesk.LeaveWithReceptionistMinutes}m)");
		leaveIt.CustomMinimumSize = new Vector2(150, 32);
		leaveIt.Disabled = promoOnHand.Count == 0;
		leaveIt.Pressed += () => Act(() => {
			string recordId = RecordId();
			if (recordId == null) return false;
			PlayerDesk.Instance.LeaveWithReceptionist(stop.StopId, recordId, out string message);
			Say(message);
			return true;
		});
		row.AddChild(leaveIt);

		var survey = Btn($"SURVEY (~{PlayerDesk.AskSurveyMinutes}m, free)");
		survey.CustomMinimumSize = new Vector2(150, 32);
		survey.Pressed += () => Act(() => {
			PlayerDesk.Instance.AskWhatsOnSurvey(stop.StopId, out string message);
			Say(message);
			return true;
		});
		row.AddChild(survey);

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

	private static CheckBox Check(string text, bool pressed) {
		var c = new CheckBox { Text = text, ButtonPressed = pressed };
		c.AddThemeColorOverride("font_color", Ink);
		c.AddThemeColorOverride("font_hover_color", Ink);
		c.AddThemeColorOverride("font_pressed_color", Ink);
		c.AddThemeColorOverride("font_focus_color", Ink);
		c.AddThemeColorOverride("font_hover_pressed_color", Ink);
		return c;
	}

	private static OptionButton Option() => new OptionButton();

	private static SpinBox Spin(double min, double max, double step, double value) =>
		new SpinBox { MinValue = min, MaxValue = max, Step = step, Value = value, CustomMinimumSize = new Vector2(160, 34) };

	private static string StarBar(float value) {
		int filled = Mathf.Clamp(Mathf.RoundToInt(value * 5f), 0, 5);
		return new string('★', filled) + new string('☆', 5 - filled);
	}

	private static string Cap(string text) =>
		string.IsNullOrEmpty(text) ? text : char.ToUpperInvariant(text[0]) + text.Substring(1);

	private static string Hour12(int hour) {
		int h = ((hour + 11) % 12) + 1;
		return $"{h}{(hour < 12 || hour >= 24 ? "am" : "pm")}";
	}

	private static void Clear(Node parent) {
		foreach (Node child in parent.GetChildren()) { parent.RemoveChild(child); child.QueueFree(); }
	}
}
