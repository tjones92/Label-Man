using System;
using System.Linq;
using Godot;

public partial class PlayerDeskPanel {
    private string selectedSceneRoomId;
    private bool showScoutingItinerary = true;
    private void SceneRoomsBoard(PlayerDesk desk) {
        ScoutingMeetingAgenda(desk);
        var modes = new HFlowContainer();
        var browse = ScoutingSmallButton(showScoutingItinerary ? "VENUES & DATES" : "NEXT SETS");
        browse.TooltipText = "Browse local venues. Click a venue to open its information and posted calendar.";
        browse.Pressed += () => { showScoutingItinerary = !showScoutingItinerary; Refresh(); };
        modes.AddChild(browse); content.AddChild(modes);
        if (showScoutingItinerary && LocalScenes.ScoutingExperience && !LocalScenes.ScoutingExperienceObserver) ScoutingItineraryBoard(desk);
        else {
            ScoutingText("LOCAL VENUES", 18, ScoutingVenueInk, PaperTheme.SansBold);
            ScoutingText("Click a venue for its calendar and visiting information.", 14, Heard);
            var date = TimeManager.Instance.CurrentDate;
            int hour = TimeManager.Instance.CurrentHour;
            var calendar = LocalSceneRoomService.Calendar(desk.CurrentCityId, date);
            var grid = new GridContainer { Columns = 3, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            grid.AddThemeConstantOverride("h_separation", 12); grid.AddThemeConstantOverride("v_separation", 12);
            foreach (var room in SceneRoomCatalog.ForPlace(desk.CurrentCityId)) {
                bool open = LocalSceneRoomService.CurrentBill(room.Id, date, hour) != null;
                var next = calendar.FirstOrDefault(b => b.RoomId == room.Id && (b.Day > LocalSceneRoomService.Day(date) || b.EndHour > hour));
                var card = VenueHandbill.Make(VenueGlyphs[room.Category], room.Name, room.Programming(date.year),
                    SceneLiveEconomics.Admission(room, date.year) is var admission && admission > 0 ? $"admission {Money(admission)}" : "no admission charge",
                    next == null ? "no announced bill" : $"{LocalSceneRoomService.Date(next.Day).ShortMonthName} {LocalSceneRoomService.Date(next.Day).day} · {Hour12(next.StartHour)}–{Hour12(next.EndHour)}",
                    open, false, (int)room.Kind);
                card.CustomMinimumSize = new Vector2(0, 340);
                card.SetMeta("scouting_room_id", room.Id);
                card.TooltipText = $"{SceneRoomCharacter.KindName(room.Kind)} · about {SceneLiveEconomics.Capacity(room)} people\nClick to open venue details and calendar.";
                card.Pressed += () => { selectedSceneRoomId = room.Id; OpenScoutingVenue(desk, room.Id); };
                grid.AddChild(card);
            }
            content.AddChild(grid);
        }
        BookYourActs(desk);
        ScoutingSceneContacts(desk);
    }

    /// <summary>Early-game §6.3: ask a room's booker for a night for one of your acts, and see what was promised and what
    /// came of it. Only rooms whose music suits the act are offered; the night list is the room's open nights past the
    /// announced calendar. A booking is an opportunity, not a show, until the set is played.</summary>
    private void BookYourActs(PlayerDesk desk) {
        var acts = desk.Roster.ToList();
        if (acts.Count == 0) return;
        var date = TimeManager.Instance.CurrentDate;
        var rooms = SceneRoomCatalog.ForPlace(desk.CurrentCityId).Where(r => r.IsPerformance).ToList();
        ScoutingText("BOOK YOUR ACTS", 18, ScoutingVenueInk, PaperTheme.SansBold);
        ScoutingText($"See a booker in person (1h, from {PlayerDesk.BookerFromHour - 12} p.m. until his room closes) and ask for a night. " +
            "He can say no; a yes goes on that night's bill when it is drawn up, and only a played set counts. The door money is the band's.", 14, Heard);
        foreach (var b in LocalSceneRoomService.Bookings.Where(b => b.LabelId == desk.Label.labelId).OrderByDescending(b => b.Day).Take(6)) {
            var room = SceneRoomCatalog.Get(b.RoomId);
            string state = b.Status == SceneBillStatus.Performed ? "played" : b.Status == SceneBillStatus.Cancelled ? "fell through" : b.Billed ? "on the bill" : "promised";
            ScoutingText($"{ArtistManager.Instance.GetArtist(b.ArtistId)?.stageName ?? "?"} at {room?.Name ?? b.RoomId}, {LocalSceneRoomService.Date(b.Day).ToHeadlineString()}: {state}. {b.Note}", 14, Ink);
        }
        if (rooms.Count == 0) return;
        var form = new HFlowContainer(); form.AddThemeConstantOverride("h_separation", 8);
        var actPick = Option(); foreach (var a in acts) actPick.AddItem(a.stageName);
        var roomPick = Option(); var nightPick = Option();
        var nights = new System.Collections.Generic.List<int>();
        int first = LocalSceneRoomService.FirstBookableDay;
        void FillNights() {
            nightPick.Clear(); nights.Clear();
            var room = rooms[Mathf.Clamp(roomPick.Selected, 0, rooms.Count - 1)];
            for (int d = first; d <= first + LocalSceneRoomService.BookingHorizonDays; d++)
                if (SceneLiveEconomics.OpenOn(room, LocalSceneRoomService.Date(d).DayOfWeek)) { nights.Add(d); nightPick.AddItem(LocalSceneRoomService.Date(d).ToHeadlineString()); }
        }
        foreach (var r in rooms) roomPick.AddItem(r.Name);
        roomPick.ItemSelected += _ => FillNights();
        FillNights();
        var pitch = Btn($"PITCH THE BOOKER  ({PlayerDesk.BookerPitchHours}h)");
        pitch.Pressed += () => Act(() => {
            if (nights.Count == 0) { Say("That room has no open nights in the booking window.", false); return true; }
            var room = rooms[Mathf.Clamp(roomPick.Selected, 0, rooms.Count - 1)];
            var act = acts[Mathf.Clamp(actPick.Selected, 0, acts.Count - 1)];
            bool ok = desk.PitchActToBooker(room.Id, act.artistId, nights[Mathf.Clamp(nightPick.Selected, 0, nights.Count - 1)], out string message);
            Say(message, ok);
            return true;
        });
        form.AddChild(actPick); form.AddChild(roomPick); form.AddChild(nightPick); form.AddChild(pitch);
        content.AddChild(form);
    }
    private void ScoutingSceneContacts(PlayerDesk desk) {
        var news = desk.LocalSceneNews();
        ScoutingSection("scout-scene-contacts", $"LOCAL REPORTS & CONTACTS ({news.Count} unread)", false, () => {
            Body(news.Count == 0 ? "LOCAL SCENE — no unread reports." : "LOCAL SCENE — since your last recap");
            foreach (var item in news) Body($"{LocalSceneRoomService.Date(item.AvailableDay).ToHeadlineString()} · {item.Source}: {item.Text}" +
                (item.ArtistId != null && desk.SceneDiscoveries.Any(d => d.ArtistId == item.ArtistId) ? " You have heard this act before." : ""));
            if (news.Count > 0) { var read = Btn(LocalScenes.ScoutingExperience && !LocalScenes.ScoutingExperienceObserver ? "SAVE LOCAL REPORTS & MARK READ" : "MARK LOCAL REPORTS READ"); read.TooltipText = "Save public signing reports for acts already in your notebook, including archived notes. No time or money spent; reports do not grant a hold."; read.Pressed += () => { desk.MarkSceneNewsRead(); Refresh(); }; content.AddChild(read); }
            foreach (string lead in desk.SceneLeadNotes()) Body(lead);
            if (ContactNetworkService.Enabled) {
                foreach (string contactTip in desk.ContactTipNotes()) Body(contactTip);
                var ask = Btn("ASK YOUR MUSICIANS WHO THEY KNOW  (1h)");
                ask.TooltipText = "Former bandmates, session partners and house-band players your roster knows: who is free, and where the rest play now.";
                ask.Pressed += () => Act(() => { bool ok = desk.AskAround(out string result); Say(result, ok); return ok; });
                content.AddChild(ask);
            }
            if (LocalScenes.ExtendedWorld) {
                Body("SOURCE OFFICES — introductions bring demo submissions to your US label. Contracts and recording arrangements remain separate.");
                var sourcePlaces = SceneSourceService.Profiles.Select(p => p.PlaceId).Concat(SceneSourceService.Institutions.Select(i => i.PlaceId)).ToArray();
                var sourceRow = new HBoxContainer();
                var office = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
                foreach (string place in sourcePlaces) office.AddItem(ScenePlaceRegistry.Get(place).Name +
                    (ScenePlaceRegistry.Get(place).CountryCode == "GB" ? " (UK)" : " (US)"));
                var source = Btn("");
                void UpdateSource() {
                    string place = sourcePlaces[office.Selected];
                    bool connected = SceneSourceService.Connection(desk.Label, place, ChartManager.Instance.GetCurrentChartWeek()) != null;
                    source.Text = connected ? "REVIEW DEMOS  (2h)" : "ARRANGE INTRODUCTION  ($150 · 2h)";
                    source.TooltipText = SceneSourceService.Profile(place)?.BusinessPath ?? SceneSourceService.Institutions.First(i => i.PlaceId == place).Practice;
                }
                office.ItemSelected += _ => UpdateSource(); UpdateSource();
                source.Pressed += () => Act(() => { string place = sourcePlaces[office.Selected]; string result;
                    bool ok = SceneSourceService.Connection(desk.Label, place, ChartManager.Instance.GetCurrentChartWeek()) != null ?
                        desk.ReviewSourceDemos(place, out result) : desk.ArrangeSourceIntroduction(place, out result); Say(result, ok); return ok; });
                sourceRow.AddChild(office); sourceRow.AddChild(source); content.AddChild(sourceRow);
            }
        });
    }

    private void ScoutingItineraryBoard(PlayerDesk desk) {
        var local = desk.ScoutingItinerary();
        var options = local.Where(o => o.Affordable).DistinctBy(o => o.BillId).Take(2).ToArray();
        bool circuit = options.Length == 0;
        if (circuit) options = desk.ScoutingItinerary(true).Where(o => o.Affordable && o.TravelMinutes > 0).DistinctBy(o => o.CityId).Take(2).ToArray();
        ScoutingText(circuit ? "NEARBY TOWNS" : "NEXT SETS", 16, ScoutingVenueInk, PaperTheme.SansBold);
        if (options.Length == 0) {
            Body(local.Count > 0 ? $"Local admission starts at {Money(local.Min(o => o.Admission))}. Check your cash or browse venues."
                : "No unheard local set fits your working hours. Browse venues, check nearby towns, or review a notebook act.");
            return;
        }
        foreach (var option in options) ScoutingSetPreview(desk, option, circuit);
    }
}
