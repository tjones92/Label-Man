using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MapKind = CityShopMap.Kind;

public partial class PlayerDeskPanel {
	internal const string BackToCityMap = "‹ BACK TO CITY MAP";
	internal bool StudioReturnsToMap { get; set; }

	private enum StreetPage { Map, Sets, Road, BandRoom, StationCall }
	private const string OfficePin = "office", StudioPin = "studio", BandRoomPin = "band_room";
	private StreetPage? streetPage;
	private MapKind? streetFilter;
	private string streetCityId, selectedTownPin;
	private PanelContainer streetOverlay;
	private VBoxContainer streetMapColumn, streetContent, officeContent;
	private ScrollContainer streetContentScroll, officeContentScroll;
	private HBoxContainer streetCommands;
	private HFlowContainer streetShortcuts;
	private Label streetTitle, streetDate;
	private TimeCard streetTime;
	private Button streetBack;

	private void BuildStreetOverlay() {
		streetOverlay = new PanelContainer { Name = "CityMapOverlay", Visible = false };
		streetOverlay.AddThemeStyleboxOverride("panel", PaperStyleBox.Sheet(Paper, 24, 18, 8));
		AddChild(streetOverlay);
		streetOverlay.SetAnchorsAndOffsetsPreset(LayoutPreset.Center);
		streetOverlay.GrowHorizontal = streetOverlay.GrowVertical = GrowDirection.Both;
		streetOverlay.OffsetLeft = -740;
		streetOverlay.OffsetRight = 740;
		streetOverlay.OffsetTop = -450;
		streetOverlay.OffsetBottom = 450;

		var stack = new VBoxContainer();
		stack.AddThemeConstantOverride("separation", 12);
		streetOverlay.AddChild(stack);
		var header = new HBoxContainer();
		header.AddThemeConstantOverride("separation", 14);
		stack.AddChild(header);
		var identity = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		header.AddChild(identity);
		streetTitle = new Label();
		streetTitle.AddThemeFontOverride("font", PaperTheme.Elite);
		streetTitle.AddThemeFontSizeOverride("font_size", 28);
		streetTitle.AddThemeColorOverride("font_color", Ink);
		identity.AddChild(streetTitle);
		var status = new HBoxContainer();
		status.AddThemeConstantOverride("separation", 12);
		identity.AddChild(status);
		streetDate = new Label { VerticalAlignment = VerticalAlignment.Center };
		streetDate.AddThemeFontOverride("font", PaperTheme.TypedBold);
		streetDate.AddThemeColorOverride("font_color", Ink);
		status.AddChild(streetDate);
		streetTime = new TimeCard();
		status.AddChild(streetTime);
		streetBack = Btn("‹ CITY MAP");
		streetBack.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		streetBack.Pressed += () => {
			if (streetPage == StreetPage.StationCall) PlayerDesk.Instance.EndCall(PlayerDesk.Instance.ActiveCall);
			ShowStreetPage(StreetPage.Map);
		};
		header.AddChild(streetBack);
		var close = Btn("RETURN TO OFFICE");
		close.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		close.Pressed += ClosePanel;
		header.AddChild(close);

		var split = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
		split.AddThemeConstantOverride("separation", 16);
		stack.AddChild(split);
		streetMapColumn = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsStretchRatio = 1.85f };
		streetMapColumn.AddThemeConstantOverride("separation", 10);
		split.AddChild(streetMapColumn);
		var inspector = new PanelContainer { Name = "CityMapInspector", SizeFlagsHorizontal = SizeFlags.ExpandFill };
		inspector.AddThemeStyleboxOverride("panel", PaperStyleBox.Sheet(new Color("f7ecd2"), 16, 12, 4));
		split.AddChild(inspector);
		streetContentScroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
		var inspection = new VBoxContainer();
		inspection.AddThemeConstantOverride("separation", 10);
		inspector.AddChild(inspection);
		streetContentScroll.SizeFlagsVertical = SizeFlags.ExpandFill;
		inspection.AddChild(streetContentScroll);
		streetShortcuts = new HFlowContainer { Visible = false };
		streetShortcuts.AddThemeConstantOverride("h_separation", 8);
		streetShortcuts.AddThemeConstantOverride("v_separation", 6);
		inspection.AddChild(streetShortcuts);
		streetContent = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		streetContent.AddThemeConstantOverride("separation", 10);
		streetContentScroll.AddChild(streetContent);
		streetCommands = new HBoxContainer();
		stack.AddChild(streetCommands);
	}

	private void UseStreetLayout(bool show) {
		if (liveSet != null && (!show || streetPage != StreetPage.Sets || (theSetQueue.Count == 0 && !VenueTalkActive))) {
			liveSet.Hide();
			gigShade.Hide();
			Clear(liveSet);
		}
		if (streetOverlay.Visible && !show) {
			Clear(streetContent);
			Clear(streetMapColumn);
			Clear(streetShortcuts);
		}
		folder.Visible = !show;
		streetOverlay.Visible = show;
		contentRoot = show ? streetContent : officeContent;
		contentScroll = show ? streetContentScroll : officeContentScroll;
		var commands = show ? streetCommands : alertRow;
		if (idleRow.GetParent() != commands) idleRow.Reparent(commands);
		var bookParent = show ? streetCommands : folderHeader;
		if (bookButton.GetParent() != bookParent) {
			bookButton.Reparent(bookParent);
			if (!show) folderHeader.MoveChild(bookButton, 2);
		}
	}

	internal void OpenCityMap(string pin = null) {
		streetFilter = null;
		streetCityId = PlayerDesk.Instance?.CurrentCityId;
		selectedTownPin = pin;
		OpenStreet(StreetPage.Map);
	}

	private void OpenStreet(StreetPage page) {
		if (PlayerDesk.Instance is not { HasLabel: true, IsGameOver: false }) return;
		browsingSaves = false;
		PlayerDesk.Instance.EndCall(PlayerDesk.Instance.ActiveCall);
		streetContentScroll.ScrollVertical = 0;
		streetPage = page;
		Open();
	}

	private void ShowStreetPage(StreetPage page) {
		streetPage = page;
		streetContentScroll.ScrollVertical = 0;
		Refresh();
	}

	private void RefreshStreet(PlayerDesk desk, TimeManager time) {
		if (streetCityId != desk.CurrentCityId) {
			streetCityId = desk.CurrentCityId;
			selectedTownPin = null;
			streetFilter = null;
		}
		streetTitle.Text = (desk.CurrentCity?.name ?? "Town").ToUpperInvariant() + " — HIT THE TOWN";
		streetDate.Text = $"{time.CurrentDate.DayName[..3]} {time.CurrentDate.ToHeadlineString()}  ·  {time.GetTimeString()}  ·  {Money(desk.Label.cashReserves)} cash";
		streetTime.Set(time, desk);
		if (streetPage == StreetPage.StationCall && (desk.ActiveCall == null || desk.ActiveCall.stage == CallStage.Ended))
			streetPage = StreetPage.Map;
		streetBack.Visible = streetPage != StreetPage.Map;
		idleRow.Visible = !desk.InStudio;
		bookButton.Visible = !desk.InStudio;
		SetWaitPreview(waitOneButton, desk, 1);
		SetWaitPreview(waitThreeButton, desk, 3);
		int evening = desk.HoursUntil(17);
		SetWaitPreview(waitEveningButton, desk, evening);
		waitEveningButton.Disabled |= evening <= 0;
		waitEveningButton.Text = evening <= 0 ? "EVENING'S HERE" : "WAIT FOR EVENING";
		if (evening <= 0) waitEveningButton.TooltipText = "Evening scouting hours have begun.";
		RefreshPickUp(desk);

		Clear(streetMapColumn);
		Clear(content);
		streetMapColumn.Visible = streetPage == StreetPage.Map;
		Clear(streetShortcuts);
		streetShortcuts.Hide();
		streetContent.CustomMinimumSize = streetMapColumn.Visible ? new Vector2(420, 0) : Vector2.Zero;
		switch (streetPage) {
			case StreetPage.Map: DrawCityMap(desk, time); break;
			case StreetPage.Sets: if (!RenderTheSet(desk)) PageAandR(); break;
			case StreetPage.Road: PageStreetDistribution(); break;
			case StreetPage.BandRoom: PageBandRoom(); break;
			case StreetPage.StationCall: PageCall(desk, desk.ActiveCall); break;
		}
		SyncLienStage(desk);
		ShowLienMorningIfPending(desk);
		ShowLienNoticeIfPending(desk);
		ShowHolderMoveIfPending(desk);
		HighlightFirstWeekTarget(desk);
	}

	private void DrawCityMap(PlayerDesk desk, TimeManager time) {
		int today = LocalSceneRoomService.Day(time.CurrentDate);
		var sets = desk.ScoutingItinerary().Where(set => set.Day == today).ToList();
		var rooms = LocalScenes.Rooms
			? SceneRoomCatalog.ForPlace(LocalScenePersistenceService.SceneIdFor(desk.CurrentCityId)).Where(room => room.IsPerformance).ToArray()
			: Array.Empty<SceneRoomProfile>();
		var accounts = desk.StopsInCity(desk.CurrentCityId)
			.Where(stop => stop.Kind is PlayerDesk.StopKind.Shop or PlayerDesk.StopKind.Station).ToArray();
		bool studioInTown = desk.AtHome || desk.ActiveStudioTrip is { } trip && trip.CityId == desk.CurrentCityId;
		bool canBook = studioInTown && desk.Roster.Any() && !desk.FirstWeekStudioTomorrow;
		bool bandAvailable = BandLife.RosterChurn && desk.Roster.Any();
		var pins = new List<CityShopMap.Stop>();
		foreach (var stop in accounts) {
			bool shop = stop.Kind == PlayerDesk.StopKind.Shop;
			string note = desk.GermainLedgerStopVerb(stop.StopId) ?? (shop ? desk.PreOrderNote(stop.StopId) : null);
			pins.Add(new(stop.StopId, stop.DisplayName, shop ? MapKind.Shop : MapKind.Radio, Active: note != null, Note: note));
		}
		foreach (var venue in LocalScenes.Rooms ? Array.Empty<PlayerDesk.ScoutingVenue>() : VenueOrder) {
			var (opens, closes) = PlayerDesk.VenueHours(venue);
			pins.Add(new(venue.ToString(), VenueTitle[venue], MapKind.Venue, Active: time.CurrentHour >= opens && time.CurrentHour < closes));
		}
		foreach (var room in rooms) {
			var showings = sets.Where(set => set.RoomId == room.Id).ToArray();
			pins.Add(new(room.Id, room.Name, MapKind.Venue, Active: showings.Length > 0,
				Note: showings.Any(set => set.Playing) ? "Playing now" : showings.Length > 0 ? "Set today" : null));
		}
		pins.Add(new(OfficePin, desk.AtHome ? desk.Label.labelName + " office" : "Road to the office", MapKind.Office));
		pins.Add(new(StudioPin, "Studio", MapKind.Studio, Active: desk.Session != null, Muted: desk.Session == null));
		pins.Add(new(BandRoomPin, "Band Room", MapKind.BandRoom, Muted: !bandAvailable));

		var filters = new HFlowContainer();
		filters.AddThemeConstantOverride("h_separation", 8);
		streetMapColumn.AddChild(filters);
		void Filter(string caption, MapKind? kind) {
			filters.AddChild(ChoiceButton(caption, kind == streetFilter, () => {
				streetFilter = kind;
				selectedTownPin = null;
				ShowStreetPage(StreetPage.Map);
			}));
		}
		Filter("ALL", null);
		Filter("VENUES", MapKind.Venue);
		Filter("RECORD SHOPS", MapKind.Shop);
		Filter("RADIO", MapKind.Radio);
		Filter("STUDIO & BAND ROOM", MapKind.Studio);

		var map = new CityShopMap { Name = "TownMap" };
		string initial = desk.Label.labelName?.Trim() is { Length: > 0 } name ? name[..1].ToUpperInvariant() : "O";
		map.SetLocations(desk.CurrentCityId, pins, selectedTownPin, initial,
			stop => streetFilter == null || stop.Category == MapKind.Office || stop.Category == streetFilter
				|| streetFilter == MapKind.Studio && stop.Category == MapKind.BandRoom,
			id => { selectedTownPin = id; ShowStreetPage(StreetPage.Map); });
		streetMapColumn.AddChild(map);
		var legend = FaintLine($"{initial} Office    ·    ◆ Venues    ·    ● Record shops    ·    ▲ Radio    ·    ■ Studio / Band room");
		streetMapColumn.AddChild(legend);
		var road = Btn("REGIONAL TRAVEL & OTHER STOPS");
		road.Pressed += () => ShowStreetPage(StreetPage.Road);
		streetMapColumn.AddChild(road);

		var selected = pins.FirstOrDefault(pin => pin.Id == selectedTownPin);
		if (selected == null) {
			Heading("PLAYING TODAY");
			if (sets.Count == 0) Body("No more sets to catch today.");
			foreach (var set in sets) ScoutingSetPreview(desk, set, linkVenue: false, compact: true);
			StreetScoutingLink("ALL UPCOMING SETS / VENUES", "FIND A SET");
			StreetScoutingLink("ACT NOTEBOOK", "ACT NOTEBOOK");
			streetShortcuts.Show();
			if (!desk.AtHome) DrawDriveHome(desk);
			return;
		}

		var back = Btn("‹ BACK TO TODAY'S LIST");
		back.Pressed += () => { selectedTownPin = null; ShowStreetPage(StreetPage.Map); };
		content.AddChild(back);
		var title = Heading(selected.Name);
		title.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		if (selected.Category != MapKind.Office || desk.AtHome) content.AddChild(FaintLine(map.District(selected.Id)));
		switch (selected.Category) {
			case MapKind.Office:
				Body(desk.AtHome ? "Your label's office." : $"The office is in {desk.Label.headquartersCity}.");
				if (!desk.AtHome) DrawDriveHome(desk);
				break;
			case MapKind.Venue when !LocalScenes.Rooms:
				var venue = Enum.Parse<PlayerDesk.ScoutingVenue>(selected.Id);
				Body(VenueCharacter[venue]);
				content.AddChild(LegacyScoutButton(venue));
				break;
			case MapKind.Venue:
				var room = rooms.First(r => r.Id == selected.Id);
				Body(Phrase.Capitalize(room.Programming(time.CurrentDate.year)) + ".");
				Body(SceneRoomCharacter.Crowd(room.Kind));
				Body($"Usual hours {Hour12(room.OpenHour)}–{Hour12(room.CloseHour)}");
				var bill = sets.Where(set => set.RoomId == room.Id).ToArray();
				Heading(bill.Length > 0 ? "PLAYING TODAY" : "NO MORE SETS TO CATCH TODAY");
				foreach (var set in bill) ScoutingSetPreview(desk, set, linkVenue: false, compact: true);
				var calendar = Btn("VENUE CALENDAR & BOOKER");
				calendar.Pressed += () => OpenScoutingVenue(desk, room.Id);
				content.AddChild(calendar);
				break;
			case MapKind.Shop:
			case MapKind.Radio:
				var stop = accounts.First(account => account.StopId == selected.Id);
				DrawLedgerLead(desk, stop);
				if (selected.Category == MapKind.Shop) DrawRecordShop(desk, stop);
				else DrawRadioStation(desk, stop);
				break;
			case MapKind.Studio:
				Body(desk.Session is { } session ? $"Recording date open · {session.Room?.Name ?? "Studio"}" : "No session booked.");
				var studio = Btn(desk.Session != null ? "RETURN TO STUDIO" : "TAKE INTO STUDIO");
				studio.Disabled = desk.Session == null && !canBook;
				if (studio.Disabled) Body(!studioInTown ? "Drive home to book a room with your act."
					: !desk.Roster.Any() ? "Sign an act before booking a room." : "Turn in and book the room tomorrow.");
				studio.Pressed += () => UIManager.Instance?.OpenOfficeStudio();
				content.AddChild(studio);
				break;
			case MapKind.BandRoom:
				Body(!BandLife.RosterChurn ? "Lineup churn is switched off."
					: !desk.Roster.Any() ? "Sign an act to start working with the band."
					: $"{Plural.Of(desk.PendingVisits().Count, "visit")} waiting.");
				var band = Btn("ENTER BAND ROOM");
				band.Disabled = !bandAvailable;
				band.Pressed += () => ShowStreetPage(StreetPage.BandRoom);
				content.AddChild(band);
				break;
		}
	}

	private void StreetScoutingLink(string caption, string view) {
		var button = Btn(caption);
		button.Pressed += () => {
			scoutingView = view;
			showScoutingItinerary = true;
			ShowStreetPage(StreetPage.Sets);
		};
		streetShortcuts.AddChild(button);
	}

	private void DrawRadioStation(PlayerDesk desk, PlayerDesk.PlayerStop station) {
		if (ChartManager.Instance?.GetRadioStation(station.StationId) is { } broadcast)
			Body($"{broadcast.band} · {broadcast.format} · {broadcast.wattage:N0} watts");
		Body(station.LastVisitWeek > 0 || station.Relationship > 0
			? $"Station relationship {station.Relationship:P0}." : "You haven't called on this station.");
		var promo = desk.PromoSinglesOnHand().ToList();
		OptionButton record = null;
		if (promo.Count > 0) {
			record = Option();
			foreach (var single in promo) record.AddItem($"“{single.Title}” — {single.PromoOnHand:N0} promo on hand");
			content.AddChild(record);
		}
		content.AddChild(StationSlip(station, promo, record));
	}

	private void DrawLedgerLead(PlayerDesk desk, PlayerDesk.PlayerStop stop, bool showLocation = false) {
		if (desk.GermainLedgerStopVerb(stop.StopId) is not { } clue) return;
		if (showLocation) Body(stop.DisplayName);
		var follow = Btn(clue);
		follow.TooltipText = "Follow up on Jack's black book at " + stop.DisplayName + ".";
		follow.Pressed += () => Act(() => {
			bool ok = desk.FollowGermainLedgerLead(stop.StopId, out string message);
			Say(message, ok);
			return ok;
		});
		content.AddChild(follow);
	}

	private void DrawDriveHome(PlayerDesk desk) {
		int hours = desk.TravelQuote(desk.CurrentCityId, desk.Label.homeCityId).Hours;
		var home = Btn($"DRIVE HOME  ({hours}h)");
		AddTravelTonightPreview(home, desk, hours);
		home.Pressed += () => Act(() => { bool ok = desk.DriveHome(out string message); Say(message, ok); return ok; });
		content.AddChild(home);
	}
}
