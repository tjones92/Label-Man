using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Barebones player-facing desk. One panel, a tab strip, and a button per action --
/// everything is built in code so the slice can change shape without a scene edit.
/// It only ever reads and calls <see cref="PlayerDesk"/>; no simulation logic lives here.
/// </summary>
public partial class PlayerDeskPanel : Control {
	private Label titleLabel, clockLabel, statusLabel;
	private HBoxContainer tabs;
	private VBoxContainer content;
	private readonly List<Button> tabButtons = new();
	private Action currentPage;
	private int currentTab;

	private static readonly Color Ink = new("2b2115");
	private static readonly Color Paper = new("f1e5c8");
	private static readonly Color Folder = new("d7b978");

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
		var close = new Button { Text = "CLOSE  ×" };
		close.Pressed += ClosePanel;
		header.AddChild(close);

		clockLabel = new Label();
		clockLabel.AddThemeFontSizeOverride("font_size", 17);
		root.AddChild(clockLabel);

		statusLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
		statusLabel.AddThemeFontSizeOverride("font_size", 16);
		statusLabel.AddThemeColorOverride("font_color", new Color("6b3a1c"));
		root.AddChild(statusLabel);

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

	private void BuildTabs() {
		foreach (Node child in tabs.GetChildren()) child.QueueFree();
		tabButtons.Clear();
		AddTab("SCOUTING", PageScouting);
		AddTab("ROSTER", PageRoster);
		AddTab("SONGS", PageSongs);
		AddTab("STUDIO", PageStudio);
		AddTab("RELEASES", PageReleases);
		AddTab("DISTRIBUTION", PageDistribution);
		AddTab("FINANCES", PageFinances);
		AddTab("LOG", PageLog);
	}

	private void AddTab(string title, Action page) {
		int index = tabButtons.Count;
		var button = new Button { Text = title, CustomMinimumSize = new Vector2(150, 40) };
		button.Pressed += () => { currentTab = index; currentPage = page; Refresh(); };
		tabs.AddChild(button);
		tabButtons.Add(button);
	}

	// ========================================================================
	// REFRESH
	// ========================================================================

	private void Refresh() {
		if (!Visible) return;
		PlayerDesk desk = PlayerDesk.Instance;
		TimeManager time = TimeManager.Instance;

		if (desk == null) { titleLabel.Text = "DESK UNAVAILABLE"; return; }

		if (!desk.HasLabel) {
			titleLabel.Text = "START A LABEL";
			clockLabel.Text = time == null ? "" : $"{time.CurrentDate.ToLongString()}  •  {time.GetTimeString()}";
			foreach (Node child in tabs.GetChildren()) child.QueueFree();
			tabButtons.Clear();
			Clear(content);
			PageFounding();
			return;
		}

		if (tabButtons.Count == 0) { BuildTabs(); currentTab = 0; currentPage = PageScouting; }
		for (int index = 0; index < tabButtons.Count; index++)
			tabButtons[index].Modulate = index == currentTab ? Colors.White : new Color(1, 1, 1, .62f);

		AILabel label = desk.Label;
		titleLabel.Text = label.labelName.ToUpperInvariant();
		string region = ChartManager.Instance?.GetRegionById(label.homeRegion)?.regionName ?? label.homeRegion;
		clockLabel.Text =
			$"{time?.CurrentDate.ToLongString()}  •  {time?.GetTimeString()}  •  {time?.HoursRemaining ?? 0}h left ({time?.GetDayStatus()})\n" +
			$"{region}  |  ${label.cashReserves:N0} cash  |  {label.CurrentRosterSize}/{label.maxRosterSize} acts  |  " +
			$"reach {label.distributionStrength:P0}  |  {label.independentDistributionRegions.Count + (label.distributionRegions?.Length ?? 0)} markets";

		Clear(content);
		(currentPage ?? PageScouting)();
	}

	private void Say(string message) {
		statusLabel.Text = message ?? string.Empty;
	}

	private void Act(Func<bool> action) {
		action();
		Refresh();
	}

	// ========================================================================
	// PAGES
	// ========================================================================

	private void PageFounding() {
		Heading("OPEN FOR BUSINESS");
		Body("Name the label and pick the market you work out of. You start with $9,000, one market's " +
			"distribution, and no roster.");

		var nameEdit = new LineEdit { PlaceholderText = "Label name", CustomMinimumSize = new Vector2(400, 38) };
		content.AddChild(nameEdit);

		var regionPicker = new OptionButton { CustomMinimumSize = new Vector2(400, 38) };
		List<MarketRegion> regions = ChartManager.Instance?.GetAllRegions() ?? new List<MarketRegion>();
		foreach (MarketRegion region in regions) regionPicker.AddItem(region.regionName);
		content.AddChild(regionPicker);

		var found = new Button { Text = "OPEN THE DOORS", CustomMinimumSize = new Vector2(240, 44) };
		found.Pressed += () => {
			int index = Mathf.Clamp(regionPicker.Selected, 0, Mathf.Max(0, regions.Count - 1));
			if (regions.Count == 0) { Say("No markets loaded."); return; }
			PlayerDesk.Instance.FoundLabel(nameEdit.Text, regions[index].regionId, out string message);
			Say(message);
			Refresh();
		};
		content.AddChild(found);
	}

	private void PageScouting() {
		PlayerDesk desk = PlayerDesk.Instance;
		Heading("SCOUT THE LOCAL SCENE");
		Body($"An afternoon working your own market. Costs {PlayerDesk.ScoutHours} hours. What you hear is your " +
			"read on the act, not the truth — a better ear narrows the gap.");

		var scout = new Button { Text = $"GO SCOUTING  ({PlayerDesk.ScoutHours}h)", CustomMinimumSize = new Vector2(280, 42) };
		scout.Pressed += () => Act(() => { PlayerDesk.Instance.ScoutLocally(out string message); Say(message); return true; });
		content.AddChild(scout);

		if (desk.Slate.Count == 0) { Body("No acts on the pad. Go hear somebody."); return; }

		Heading($"HEARD {desk.SlateDate.ToHeadlineString()}");
		foreach (PlayerDesk.Prospect prospect in desk.Slate.ToList()) {
			var row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 12);
			var text = new Label {
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				Text = $"{prospect.Artist.stageName}  —  {GenreNameFormatter.Format(prospect.Artist.primaryGenre)}\n" +
					$"    your read: {StarBar(prospect.ReadQuality)}   •   {prospect.Note}   •   asking ${prospect.AskingAdvance:N0}"
			};
			text.AddThemeColorOverride("font_color", Ink);
			row.AddChild(text);

			var sign = new Button { Text = $"SIGN ({PlayerDesk.SignHours}h)", CustomMinimumSize = new Vector2(150, 40) };
			PlayerDesk.Prospect captured = prospect;
			sign.Pressed += () => Act(() => { PlayerDesk.Instance.SignProspect(captured, out string message); Say(message); return true; });
			row.AddChild(sign);
			content.AddChild(row);
		}
	}

	private void PageRoster() {
		PlayerDesk desk = PlayerDesk.Instance;
		Heading("ROSTER");
		if (!desk.Roster.Any()) { Body("Nobody signed yet."); return; }

		foreach (SimulatedArtist artist in desk.Roster.ToList()) {
			var row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 12);
			var text = new Label {
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				Text = $"{artist.stageName}  —  {GenreNameFormatter.Format(artist.primaryGenre)}  •  {artist.careerState}\n" +
					$"    {artist.totalReleases} releases   •   {artist.top40Hits} Top 40   •   " +
					$"${artist.unrecoupedAdvance:N0} unrecouped   •   {artist.royaltyRate:P0} royalty   •   " +
					$"expires {artist.contractExpiresYear}"
			};
			text.AddThemeColorOverride("font_color", Ink);
			row.AddChild(text);

			var write = new Button { Text = $"WRITE ({PlayerDesk.WriteHours}h)", CustomMinimumSize = new Vector2(160, 40) };
			SimulatedArtist captured = artist;
			write.Pressed += () => Act(() => { PlayerDesk.Instance.WriteSongs(captured, out string message); Say(message); return true; });
			row.AddChild(write);

			var view = new Button { Text = "FILE", CustomMinimumSize = new Vector2(90, 40) };
			view.Pressed += () => UIManager.Instance?.OpenArtist(captured.artistId, true);
			row.AddChild(view);
			content.AddChild(row);
		}
	}

	private void PageSongs() {
		PlayerDesk desk = PlayerDesk.Instance;
		Heading("THE SONGBOOK");
		Body("Unrecorded material. Book a session to cut one as a master.");

		List<PlayerDesk.Song> unrecorded = desk.UnrecordedSongs.ToList();
		if (unrecorded.Count == 0) { Body("Nothing written. Send an act to write on the ROSTER page."); return; }

		foreach (PlayerDesk.Song song in unrecorded) {
			SimulatedArtist artist = ArtistManager.Instance?.GetArtist(song.ArtistId);
			var text = new Label {
				Text = $"\"{song.Title}\"  —  {artist?.stageName ?? "?"}  •  {GenreNameFormatter.Format(song.Genre)}\n" +
					$"    hook {StarBar(song.Hook)}   •   originality {StarBar(song.Originality)}   •   written {song.Written.ToHeadlineString()}"
			};
			text.AddThemeColorOverride("font_color", Ink);
			content.AddChild(text);
		}
	}

	private void PageStudio() {
		PlayerDesk desk = PlayerDesk.Instance;
		Heading("BOOK A SESSION");
		Body($"A full day tracking, {PlayerDesk.SessionHours} hours. Money buys the recording, not the song — " +
			"a bigger budget lifts production, never the hook.");

		List<PlayerDesk.Song> unrecorded = desk.UnrecordedSongs.ToList();
		if (unrecorded.Count == 0) { Body("Nothing to cut."); }
		else {
			var songPicker = new OptionButton { CustomMinimumSize = new Vector2(520, 38) };
			foreach (PlayerDesk.Song song in unrecorded) {
				SimulatedArtist artist = ArtistManager.Instance?.GetArtist(song.ArtistId);
				songPicker.AddItem($"\"{song.Title}\" — {artist?.stageName ?? "?"}");
			}
			content.AddChild(songPicker);

			var budgetPicker = new OptionButton { CustomMinimumSize = new Vector2(520, 38) };
			float basic = desk.Label.GetProductionCost();
			budgetPicker.AddItem($"Demo session — ${basic * 0.5f:N0}");
			budgetPicker.AddItem($"Standard date — ${basic:N0}");
			budgetPicker.AddItem($"Full production — ${basic * 2f:N0}");
			budgetPicker.Selected = 1;
			content.AddChild(budgetPicker);

			var book = new Button { Text = $"BOOK THE ROOM  ({PlayerDesk.SessionHours}h)", CustomMinimumSize = new Vector2(300, 44) };
			book.Pressed += () => Act(() => {
				int index = Mathf.Clamp(songPicker.Selected, 0, unrecorded.Count - 1);
				float multiplier = budgetPicker.Selected switch { 0 => 0.5f, 2 => 2f, _ => 1f };
				PlayerDesk.Instance.BookSession(unrecorded[index], multiplier, out string message);
				Say(message);
				return true;
			});
			content.AddChild(book);
		}

		Heading("MASTERS ON THE SHELF");
		List<PlayerDesk.Master> shelf = desk.Masters.Where(master => !master.Scheduled).ToList();
		if (shelf.Count == 0) { Body("Nothing cut and waiting."); return; }
		foreach (PlayerDesk.Master master in shelf) {
			var text = new Label {
				Text = $"\"{master.SongTitle}\"  —  {master.Record.artistName}\n" +
					$"    hook {StarBar(master.Record.hookStrength)}   •   production {StarBar(master.Record.productionQuality)}   " +
					$"•   cost ${master.ProductionCost:N0}   •   cut {master.Cut.ToHeadlineString()}"
			};
			text.AddThemeColorOverride("font_color", Ink);
			content.AddChild(text);
		}
	}

	private void PageReleases() {
		PlayerDesk desk = PlayerDesk.Instance;
		Heading("SCHEDULE A RELEASE");
		Body($"Costs {PlayerDesk.ScheduleHours} hours. The campaign is charged the day the record ships. " +
			"Where it can actually be bought is set on the DISTRIBUTION page.");

		List<PlayerDesk.Master> shelf = desk.Masters.Where(master => !master.Scheduled).ToList();
		if (shelf.Count == 0) { Body("No masters waiting."); }
		else {
			var masterPicker = new OptionButton { CustomMinimumSize = new Vector2(520, 38) };
			foreach (PlayerDesk.Master master in shelf)
				masterPicker.AddItem($"\"{master.SongTitle}\" — {master.Record.artistName}");
			content.AddChild(masterPicker);

			var daysRow = new HBoxContainer();
			daysRow.AddThemeConstantOverride("separation", 10);
			daysRow.AddChild(new Label { Text = "Ships in (days)" });
			var daysInput = new SpinBox { MinValue = 1, MaxValue = 120, Value = 14, CustomMinimumSize = new Vector2(140, 36) };
			daysRow.AddChild(daysInput);
			daysRow.AddChild(new Label { Text = "Campaign ($)" });
			var budgetInput = new SpinBox { MinValue = 0, MaxValue = 50000, Step = 50, Value = 400, CustomMinimumSize = new Vector2(180, 36) };
			daysRow.AddChild(budgetInput);
			content.AddChild(daysRow);

			var schedule = new Button { Text = $"SET THE DATE  ({PlayerDesk.ScheduleHours}h)", CustomMinimumSize = new Vector2(300, 44) };
			schedule.Pressed += () => Act(() => {
				int index = Mathf.Clamp(masterPicker.Selected, 0, shelf.Count - 1);
				PlayerDesk.Instance.ScheduleRelease(shelf[index], (int)daysInput.Value, (float)budgetInput.Value, out string message);
				Say(message);
				return true;
			});
			content.AddChild(schedule);
		}

		Heading("ON THE SCHEDULE");
		if (desk.Planned.Count == 0) Body("Nothing booked.");
		foreach (PlayerDesk.PlannedRelease release in desk.Planned.OrderBy(entry => entry.Date))
			Body($"{release.Date.ToHeadlineString()}  —  \"{release.Master.SongTitle}\" by {release.Master.Record.artistName}  " +
				$"(${release.MarketingBudget:N0} campaign)");

		Heading("IN THE MARKET");
		List<RecordRuntimeData> released = desk.ReleasedRecords.OrderByDescending(record => record.weeksSinceRelease).ToList();
		if (released.Count == 0) { Body("Nothing out yet."); return; }
		foreach (RecordRuntimeData record in released)
			Body($"\"{record.baseRecord.title}\" by {record.baseRecord.artistName}  —  " +
				$"{(record.peakPosition > 0 ? $"peak #{record.peakPosition}, {record.weeksOnChart} weeks on" : "has not charted")}  •  " +
				$"{record.weeksSinceRelease} weeks out");
	}

	private void PageDistribution() {
		PlayerDesk desk = PlayerDesk.Instance;
		AILabel label = desk.Label;
		Heading("DISTRIBUTION");
		Body($"A wholesale house carries your line into shops in one market. Costs {PlayerDesk.DistributionHours} hours " +
			"of travel and pitching. Without a house in a market, the record is not on sale there.");

		Heading("MARKETS YOU COVER");
		var covered = label.AllCoveredRegions().ToList();
		Body(covered.Count == 0 ? "None." : string.Join("\n", covered.Select(regionId =>
			$"{ChartManager.Instance?.GetRegionById(regionId)?.regionName ?? regionId}" +
			$"{(label.distributionRegions?.Contains(regionId) ?? false ? "  (your own trucks)" : "  (wholesale house)")}")));

		Heading("MARKETS OPEN TO YOU");
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

			var place = new Button { Text = $"PLACE LINE ({PlayerDesk.DistributionHours}h)", CustomMinimumSize = new Vector2(200, 40) };
			string capturedId = region.regionId;
			place.Pressed += () => Act(() => { PlayerDesk.Instance.PlaceLine(capturedId, out string message); Say(message); return true; });
			row.AddChild(place);
			content.AddChild(row);
		}
	}

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
			Table(null, new[] {
				new[] { "retail gross", $"${latest.Gross:N0}" },
				new[] { "− manufacturing", $"${latest.ManufacturingCost:N0}" },
				new[] { "− distributor's skim", $"${latest.DistributionSkim:N0}" },
				new[] { "− artist royalty", $"${latest.ArtistRoyalty:N0}" },
				new[] { "= earned", $"${latest.Earned:N0}" },
				new[] { "− billed on credit", $"${latest.Deferred:N0}" },
				new[] { "+ old invoices paid", $"${latest.Collected:N0}" },
				new[] { "= reached the bank", $"${latest.Banked:N0}" }
			});
			if (latest.Deferred > 0f)
				Body($"${latest.Deferred:N0} of what you earned this week went out on credit — " +
					"the houses pay on their own terms.");
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
		var released = desk.ReleasedRecords
			.OrderByDescending(record => record.lifetimeLabelNet)
			.ToList();
		if (released.Count == 0) { Body("Nothing released yet."); }
		else
			foreach (RecordRuntimeData record in released) {
				float net = record.lifetimeLabelNet;
				float cost = record.sunkProductionCost;
				Body($"\"{record.baseRecord.title}\" — {record.baseRecord.artistName}\n" +
					$"    {record.totalUnitsSold:N0} units lifetime   •   {record.unitsThisWeek:N0} this week   •   " +
					$"{(record.peakPosition > 0 ? $"peak #{record.peakPosition}" : "uncharted")}\n" +
					$"    earned ${net:N0} against ${cost:N0} of tape   •   " +
					$"{(net >= cost ? $"in the black by ${net - cost:N0}" : $"${cost - net:N0} still to make back")}");
			}

		Heading("ARTIST ACCOUNTS");
		var roster = desk.Roster.ToList();
		if (roster.Count == 0) { Body("Nobody signed."); return; }
		foreach (SimulatedArtist artist in roster)
			Body($"{artist.stageName} — ${artist.unrecoupedAdvance:N0} unrecouped   •   " +
				$"${artist.totalRoyaltyEarnings:N0} paid through   •   {artist.royaltyRate:P0} of retail");
	}

	private void PageLog() {
		Heading("DESK LOG");
		IReadOnlyList<string> entries = PlayerDesk.Instance.Log;
		if (entries.Count == 0) { Body("Nothing has happened yet."); return; }
		foreach (string entry in entries) Body(entry);
	}

	// ========================================================================
	// SMALL HELPERS
	// ========================================================================

	private void Heading(string text) {
		var node = new Label { Text = text };
		node.AddThemeFontSizeOverride("font_size", 20);
		node.AddThemeColorOverride("font_color", new Color("6b3a1c"));
		content.AddChild(node);
	}

	/// <summary>
	/// A real grid, because the default font is proportional and space-padded columns do
	/// not line up in it. Pass null headers for an unheaded two- or three-column list.
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

	private static string StarBar(float value) {
		int filled = Mathf.Clamp(Mathf.RoundToInt(value * 5f), 0, 5);
		return new string('★', filled) + new string('☆', 5 - filled);
	}

	private static void Clear(Node parent) {
		foreach (Node child in parent.GetChildren()) { parent.RemoveChild(child); child.QueueFree(); }
	}
}
