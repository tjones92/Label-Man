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

		// --- WORK THIS TOWN (home or away): choose a single and how many to leave here ---
		List<(string RecordId, string Title, int OnHand)> onHand = desk.PressedSinglesOnHand().ToList();
		if (onHand.Count == 0)
			Body(desk.AtHome
				? "Nothing pressed on hand to sell yet — assemble a single and order a run below, then work the town once it's in."
				: "Nothing pressed on hand to leave here — you carry stock out from the office.");
		else {
			var workRow = new HBoxContainer();
			workRow.AddThemeConstantOverride("separation", 10);
			var singlePick = Option();
			singlePick.CustomMinimumSize = new Vector2(360, 36);
			foreach ((string _, string title, int inHand) in onHand)
				singlePick.AddItem($"\"{title}\" — {inHand:N0} on hand");
			workRow.AddChild(singlePick);
			workRow.AddChild(FormLabel("Leave"));
			int suggested = desk.SuggestedPlacement(here);
			var leaveInput = Spin(1, Mathf.Max(1, onHand[0].OnHand), 25, Mathf.Min(suggested, Mathf.Max(1, onHand[0].OnHand)));
			workRow.AddChild(leaveInput);
			singlePick.ItemSelected += index => {
				int max = Mathf.Max(1, onHand[Mathf.Clamp((int)index, 0, onHand.Count - 1)].OnHand);
				leaveInput.MaxValue = max;
				leaveInput.Value = Mathf.Min(desk.SuggestedPlacement(here), max);
			};
			var work = Btn($"WORK THIS TOWN ({PlayerDesk.DistributionHours}h)");
			work.CustomMinimumSize = new Vector2(260, 40);
			work.Pressed += () => Act(() => {
				(string recordId, _, _) = onHand[Mathf.Clamp(singlePick.Selected, 0, onHand.Count - 1)];
				PlayerDesk.Instance.WorkThisTown(recordId, (int)leaveInput.Value, out string message);
				Say(message);
				return true;
			});
			workRow.AddChild(work);
			content.AddChild(workRow);
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
		Body($"Minimum run {PlayerDesk.PressMinimumOrder}. About {PlayerDesk.PressVinylPerUnit + PlayerDesk.PressSleeveLabelPerUnit:F2}/disc " +
			$"plus ${PlayerDesk.PressLacquerSetup:N0} lacquer setup and ${PlayerDesk.PressShipping:N0} for sleeves, labels and freight. " +
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
					singlePicker.AddItem($"\"{title}\"{(inMarket ? "" : " (upcoming)")}  —  {(stock?.Remaining ?? 0):N0} in the office");
				}
				pickRow.AddChild(singlePicker);
				pickRow.AddChild(FormLabel("Qty"));
				var qtyInput = Spin(PlayerDesk.PressMinimumOrder, 100000, 100, PlayerDesk.PressMinimumOrder);
				pickRow.AddChild(qtyInput);
				content.AddChild(pickRow);

				var runCost = new Label();
				runCost.AddThemeColorOverride("font_color", Rust);
				content.AddChild(runCost);
				void UpdateRunCost() => runCost.Text = $"Run cost: ${PlayerDesk.PressingCost((int)qtyInput.Value):N0}  (${PlayerDesk.PressingCost((int)qtyInput.Value) / qtyInput.Value:F2}/disc)";
				qtyInput.ValueChanged += _ => UpdateRunCost();
				UpdateRunCost();

				var order = Btn("ORDER PRESSING");
				order.CustomMinimumSize = new Vector2(240, 42);
				order.Pressed += () => Act(() => {
					(string recordId, _, _) = singles[Mathf.Clamp(singlePicker.Selected, 0, singles.Count - 1)];
					PlayerDesk.Instance.OrderPressing(recordId, (int)qtyInput.Value, out string message);
					Say(message);
					return true;
				});
				content.AddChild(order);
			}
		}

		var pending = desk.PendingPressings().ToList();
		if (pending.Count > 0) {
			Body("At the plant now:");
			foreach ((string title, int quantity, GameDate arrives) in pending)
				Body($"    {quantity:N0} of \"{title}\"  —  due {arrives.ToHeadlineString()}");
		}

		// --- SET THE RELEASE DATE (office only): date an assembled single now the plant's quoted a turnaround ---
		if (desk.AtHome) {
			Heading("SET THE RELEASE DATE");
			Body($"Costs {PlayerDesk.ScheduleHours} hours. Date an assembled single for after its vinyl lands — check " +
				"the plant's due dates above. The campaign is charged the day it ships; where it's sold is set below.");
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
				var budgetInput = Spin(0, 50000, 5, 400);
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

		// --- STOCK OUT IN THE TOWNS ---
		Heading("OUT IN THE TOWNS");
		var townStock = desk.TownStock().OrderBy(s => s.CityName).ToList();
		if (townStock.Count == 0) Body("No stock out in any town yet. Press a run, then drive it out and work the shops.");
		else
			foreach ((string cityName, string title, int remaining) in townStock)
				Body($"    {cityName}: {remaining:N0} of \"{title}\" left on the shelves");

		var owedByTown = desk.ConsignmentOwedByTown().OrderByDescending(t => t.Amount).ToList();
		if (owedByTown.Count > 0) {
			Body("Waiting to be collected (drive back to pocket the lump; a thin wire trickles in meanwhile):");
			foreach ((string cityName, float amount) in owedByTown)
				Body($"    {cityName}: ${amount:N0} the shops are holding for you");
		}

		// --- WHOLESALE HOUSES (the gamble) ---
		Heading("WHOLESALE HOUSES — THE GAMBLE");
		Body("Some markets are too far to drive a line to. Hand it to a wholesale house out there and they'll press " +
			"it into shops you'll never reach — but they pay on their own terms months later, skim their cut, and " +
			"only for what they admit they sold. A real shot, and a real risk.");
		List<MarketRegion> open = desk.GetPlaceableMarkets().ToList();
		if (open.Count == 0) { Body("No house anywhere has room for another line right now."); return; }
		foreach (MarketRegion region in open) {
			var row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 12);
			int houses = CompetitorManager.Instance?.GetIndependentDistributorsInRegion(region.regionId)
				.Count(house => house.HasCapacity && !house.CarriesLabel(label.labelId)) ?? 0;
			var text = new Label {
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				Text = $"{region.regionName}  —  {houses} house(s) with room"
			};
			text.AddThemeColorOverride("font_color", Ink);
			row.AddChild(text);

			var place = Btn($"GAMBLE A LINE ({PlayerDesk.DistributionHours}h)");
			place.CustomMinimumSize = new Vector2(220, 40);
			string capturedId = region.regionId;
			place.Pressed += () => Act(() => { PlayerDesk.Instance.PlaceLine(capturedId, out string message); Say(message); return true; });
			row.AddChild(place);
			content.AddChild(row);
		}
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
				new[] { "− artist royalty", $"${latest.ArtistRoyalty:N0}" },
				new[] { "= earned", $"${latest.Earned:N0}" },
				new[] { "− billed on credit", $"${latest.Deferred:N0}" }
			};
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
		else
			foreach ((string houseName, string regionName, float amount, int weeksAway) in invoices)
				Body($"{(amount < 1f ? "under $1" : $"${amount:N0}")}  —  {houseName} ({regionName})  —  " +
					$"{(weeksAway == 0 ? "due now" : $"due in {weeksAway} week{(weeksAway == 1 ? "" : "s")}")}");

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
				Body($"\"{record.baseRecord.title}\" — {record.baseRecord.artistName}\n" +
					$"    {unitsLifetime:N0} units lifetime   •   {record.unitsThisWeek:N0} this week   •   " +
					$"{(record.peakPosition > 0 ? $"peak #{record.peakPosition}" : "uncharted")}\n" +
					$"    earned ${net:N0} against ${cost:N0} of tape   •   " +
					$"{(net >= cost ? $"in the black by ${net - cost:N0}" : $"${cost - net:N0} still to make back")}");
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

		Heading("THE LEDGER");
		IReadOnlyList<string> entries = desk.Log;
		if (entries.Count == 0) { Body("Nothing has happened yet."); return; }
		foreach (string entry in entries) Body(entry);
	}

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
