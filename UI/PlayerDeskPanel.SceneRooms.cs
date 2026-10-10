using System;
using System.Linq;
using Godot;

public partial class PlayerDeskPanel {
    private string selectedSceneRoomId;
    private void SceneRoomsBoard(PlayerDesk desk) {
        Body($"Available today: {TimeManager.Instance?.HoursRemainingWithOvertime ?? 0}h · cash {Money(desk.Label.cashReserves)}. Waiting advances the ordinary world, including expenses and other labels' decisions.");
        var rooms = SceneRoomCatalog.ForPlace(desk.CurrentCityId);
        var date = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
        int hour = TimeManager.Instance?.CurrentHour ?? 9;
        var calendar = LocalSceneRoomService.Calendar(desk.CurrentCityId, date);
        if (!rooms.Any(r => r.Id == selectedSceneRoomId)) selectedSceneRoomId =
            rooms.FirstOrDefault(r => LocalSceneRoomService.CurrentBill(r.Id, date, hour) != null)?.Id ?? rooms.FirstOrDefault()?.Id;
        var news = desk.LocalSceneNews();
        Body(news.Count == 0 ? "LOCAL SCENE — no unread reports." : "LOCAL SCENE — since your last recap");
        foreach (var item in news) Body($"{LocalSceneRoomService.Date(item.AvailableDay).ToHeadlineString()} · {item.Source}: {item.Text}" +
            (item.ArtistId != null && desk.SceneDiscoveries.Any(d => d.ArtistId == item.ArtistId) ? " You have heard this act before." : ""));
        if (news.Count > 0) { var read = Btn("MARK LOCAL REPORTS READ"); read.Pressed += () => { desk.MarkSceneNewsRead(); Refresh(); }; content.AddChild(read); }
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
        Body("Named rooms keep their own bills. A return visit hears the same scheduled people; a record deal is a separate conversation.");
        var grid = new GridContainer { Columns = 3, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        grid.AddThemeConstantOverride("h_separation", 12); grid.AddThemeConstantOverride("v_separation", 12);
        foreach (var room in rooms) {
            bool open = LocalSceneRoomService.CurrentBill(room.Id, date, hour) != null;
            var next = calendar.FirstOrDefault(b => b.RoomId == room.Id && (b.Day > LocalSceneRoomService.Day(date) || b.EndHour > hour));
            var card = VenueHandbill.Make(VenueGlyphs[room.Category], room.Name, room.Programming(date.year),
                SceneLiveEconomics.Admission(room, date.year) is var admission && admission > 0 ? $"admission {Money(admission)}"
                    : SceneLiveEconomics.Calibrated && SceneLiveEconomics.Terms(room.Kind) == SceneRoomPayTerms.Basket ? "no admission · the basket goes round"
                    : "no admission charge",
                next == null ? "no announced bill" : $"{LocalSceneRoomService.Date(next.Day).ShortMonthName} {LocalSceneRoomService.Date(next.Day).day} · {Hour12(next.StartHour)}–{Hour12(next.EndHour)}",
                open, room.Id == selectedSceneRoomId, (int)room.Kind);
            card.CustomMinimumSize = new Vector2(0, 340);
            card.TooltipText = $"{room.Kind} · capacity {SceneLiveEconomics.Capacity(room)} · {room.ContactName}\nFictional supporting room. {room.Source}.";
            string id = room.Id; card.Pressed += () => { selectedSceneRoomId = id; Refresh(); }; grid.AddChild(card);
        }
        content.AddChild(grid);
        var selected = rooms.FirstOrDefault(r => r.Id == selectedSceneRoomId);
        if (selected == null) { Body("No detailed rooms are available for this place."); return; }
        Body($"{selected.ContactName} posts these bills. {selected.Kind} · {selected.Programming(date.year)}.");
        var tip = Btn($"ASK {selected.ContactName.ToUpperInvariant()} FOR A LISTENING TIP  (1h)");
        tip.Disabled = hour < selected.OpenHour || hour >= selected.CloseHour;
        tip.TooltipText = "Ask during posted hours. Tips refer to actual announced performances; hearing a set can justify an approach.";
        tip.Pressed += () => Act(() => { bool ok = desk.AskSceneBooker(selected.Id, out string result); Say(result, ok); return ok; });
        content.AddChild(tip);
        if (LocalScenes.Institutions && selected.IsPerformance) {
            var program = SceneEcosystemService.Programs.FirstOrDefault(p => p.RoomId == selected.Id && p.ThroughDay >= LocalSceneRoomService.Day(date));
            if (program == null) {
                var fund = Btn($"FUND 13-WEEK REHEARSAL & SHOWCASE PROGRAM  ({Money(SceneEcosystemService.ProgramCost)})");
                fund.TooltipText = "One extra set per performance night, starting in two weeks. The label pays upfront and must retain its operating reserve.";
                fund.Pressed += () => Act(() => { bool ok = desk.FundSceneProgram(selected.Id, out string result); Say(result, ok); return ok; });
                content.AddChild(fund);
            } else Body($"Funded program: {LocalSceneRoomService.Date(program.StartDay).ToHeadlineString()} through {LocalSceneRoomService.Date(program.ThroughDay).ToHeadlineString()}; one extra set on each performance night.");
        }
        foreach (var bill in calendar.Where(b => b.RoomId == selected.Id).Take(4)) {
            Body($"{LocalSceneRoomService.Date(bill.Day).ToHeadlineString()} · {Hour12(bill.StartHour)}–{Hour12(bill.EndHour)}" + (bill.Status == SceneBillStatus.Cancelled ? " · cancelled" : ""));
            foreach (var slot in bill.Appearances) {
                var artist = ArtistManager.Instance.GetArtist(slot.ArtistId);
                string eventId = ScenePerformanceService.EventId(bill, slot);
                Body($"    {Hour12(slot.StartHour)}–{Hour12(slot.EndHour)} · {artist?.stageName ?? "unavailable act"} · {slot.Role} · {slot.Status}");
                if (slot.Status != SceneBillStatus.Scheduled || bill.Status != SceneBillStatus.Scheduled) continue;
                int now = hour * 60 + (TimeManager.Instance?.CurrentMinute ?? 0);
                bool playing = bill.Day == LocalSceneRoomService.Day(date) && slot.StartHour * 60 <= now && slot.EndHour * 60 > now;
                bool future = bill.Day > LocalSceneRoomService.Day(date) || bill.Day == LocalSceneRoomService.Day(date) && slot.StartHour * 60 > now;
                if (!playing && !future) continue;
                var row = new HBoxContainer();
                if (playing) {
                    float admission = desk.AdmissionForVisit(bill, selected, now);
                    var hear = Btn($"LISTEN ({slot.EndHour * 60 - now}min · {Money(admission)} admission)");
                    hear.Pressed += () => Act(() => { bool ok = desk.ListenSceneAppearance(bill.Id, eventId, out string result); Say(result, ok); return ok; }); row.AddChild(hear);
                } else {
                    var wait = Btn($"WAIT UNTIL {LocalSceneRoomService.Date(bill.Day).ToHeadlineString()} · {Hour12(slot.StartHour)}");
                    wait.Disabled = slot.EndHour > (TimeManager.Instance?.HardStopHour ?? 21);
                    wait.TooltipText = "Time passes through the ordinary clock. No admission is charged until listening; check the bill again on arrival.";
                    wait.Pressed += () => Act(() => { bool ok = desk.WaitForSceneAppearance(bill.Id, eventId, out string result); Say(result, ok); return ok; }); row.AddChild(wait);
                }
                content.AddChild(row);
            }
        }

        bool available = LocalSceneRoomService.CurrentBill(selected.Id, date, hour) != null;
        var listen = Btn(available ? $"GO HEAR THE BILL  ({PlayerDesk.ScoutHours}h)" : "NO BILL TO CATCH NOW");
        listen.Disabled = !available;
        listen.TooltipText = "Hear the next two hours of actual appearances. Consecutive listening shares the visit admission. Free community and coffeehouse visits need no drinks.";
        listen.Pressed += () => Act(() => { bool ok = desk.ScoutSceneRoom(selected.Id, out string message); Say(message, ok); return ok; });
        content.AddChild(listen);
    }
}
