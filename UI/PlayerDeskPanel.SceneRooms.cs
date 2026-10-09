using System;
using System.Linq;
using Godot;

public partial class PlayerDeskPanel {
    private string selectedSceneRoomId;
    private void SceneRoomsBoard(PlayerDesk desk) {
        var rooms = SceneRoomCatalog.ForPlace(desk.CurrentCityId);
        var date = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
        int hour = TimeManager.Instance?.CurrentHour ?? 9;
        var calendar = LocalSceneRoomService.Calendar(desk.CurrentCityId, date);
        if (!rooms.Any(r => r.Id == selectedSceneRoomId)) selectedSceneRoomId =
            rooms.FirstOrDefault(r => LocalSceneRoomService.CurrentBill(r.Id, date, hour) != null)?.Id ?? rooms.FirstOrDefault()?.Id;
        Body("Named rooms keep their own bills. A return visit hears the same scheduled people; a record deal is a separate conversation.");
        var grid = new GridContainer { Columns = 3, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        grid.AddThemeConstantOverride("h_separation", 12); grid.AddThemeConstantOverride("v_separation", 12);
        foreach (var room in rooms) {
            bool open = LocalSceneRoomService.CurrentBill(room.Id, date, hour) != null;
            var next = calendar.FirstOrDefault(b => b.RoomId == room.Id && (b.Day > LocalSceneRoomService.Day(date) || b.EndHour > hour));
            var card = VenueHandbill.Make(VenueGlyphs[room.Category], room.Name, room.Programming(date.year),
                room.Admission > 0 ? $"admission {Money(room.Admission)}" : "no admission charge",
                next == null ? "no announced bill" : $"{LocalSceneRoomService.Date(next.Day).ShortMonthName} {LocalSceneRoomService.Date(next.Day).day} · {Hour12(next.StartHour)}–{Hour12(next.EndHour)}",
                open, room.Id == selectedSceneRoomId, (int)room.Kind);
            card.CustomMinimumSize = new Vector2(0, 340);
            card.TooltipText = $"{room.Kind} · capacity {room.Capacity} · {room.ContactName}\nFictional supporting room. {room.Source}.";
            string id = room.Id; card.Pressed += () => { selectedSceneRoomId = id; Refresh(); }; grid.AddChild(card);
        }
        content.AddChild(grid);
        var selected = rooms.FirstOrDefault(r => r.Id == selectedSceneRoomId);
        if (selected == null) { Body("No detailed rooms are available for this place."); return; }
        Body($"{selected.ContactName} posts these bills. {selected.Kind} · {selected.Programming(date.year)}.");
        foreach (var bill in calendar.Where(b => b.RoomId == selected.Id).Take(4)) {
            string names = string.Join("; ", bill.Appearances.Select(a =>
                $"{ArtistManager.Instance.GetArtist(a.ArtistId)?.stageName ?? "unavailable act"} ({a.Role}{(a.Residency ? ", regular engagement" : "")})"));
            Body($"{LocalSceneRoomService.Date(bill.Day).ToHeadlineString()} · {Hour12(bill.StartHour)}–{Hour12(bill.EndHour)} — " +
                (names.Length == 0 ? "No act announced." : names) + (bill.Status == SceneBillStatus.Cancelled ? " Cancelled." : ""));
        }
        bool available = LocalSceneRoomService.CurrentBill(selected.Id, date, hour) != null;
        var listen = Btn(available ? $"GO HEAR THE BILL  ({PlayerDesk.ScoutHours}h)" : "NO BILL TO CATCH NOW");
        listen.Disabled = !available;
        listen.TooltipText = "A visit needs two hours within the announced bill and covers admission once. Free community and coffeehouse visits need no drinks.";
        listen.Pressed += () => Act(() => { bool ok = desk.ScoutSceneRoom(selected.Id, out string message); Say(message, ok); return ok; });
        content.AddChild(listen);
    }
}
