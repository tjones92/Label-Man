using System;
using System.IO;
using System.Linq;
using Godot;

public partial class ChartAuditRunner {
    private void WriteSceneRoomCensus(string phase) {
        if (!OS.GetCmdlineUserArgs().Contains("--scene-room-audit")) return;
        var world = new WorldSaveData(); LocalSceneRoomService.CaptureWorld(world);
        var state = world.SceneRooms;
        if (state == null) { GD.Print($"SCENE_ROOM_CENSUS phase={phase} disabled=true"); return; }
        var slots = state.Bills.SelectMany(b => b.Appearances.SelectMany(a => a.PersonIds.Select(p => new { Key = $"{b.Day}|{a.StartHour}|{p}", DayPerson = $"{b.Day}|{p}", b.RoomId }))).ToArray();
        int conflicts = slots.Length - slots.Select(s => s.Key).Distinct().Count();
        int remoteDays = slots.GroupBy(s => s.DayPerson).Count(g => g.Select(s => s.RoomId).Distinct().Count() > 1);
        int duplicateBills = state.Bills.Count - state.Bills.Select(b => b.Id).Distinct().Count();
        int badCounts = state.Bills.Count(b => b.Attendance < 0 || b.Attendance > LocalSceneRoomService.CapacityOf(b));
        string dir = ProjectSettings.GlobalizePath("res://SimLogs");
        using (var writer = new StreamWriter(Path.Combine(dir, $"{runName}-scene-room-{phase}.csv"))) {
            writer.WriteLine("billId,roomId,day,year,status,artistId,role,residency,personIds,attendance,fee");
            foreach (var bill in state.Bills) foreach (var slot in bill.Appearances) writer.WriteLine(string.Join(",", new[] {
                Csv(bill.Id), Csv(bill.RoomId), bill.Day.ToString(), bill.Year.ToString(), slot.Status.ToString(),
                Csv(slot.ArtistId), Csv(slot.Role), slot.Residency.ToString(), Csv(string.Join("|", slot.PersonIds)), bill.Attendance.ToString(), slot.Fee.ToString(System.Globalization.CultureInfo.InvariantCulture) }));
        }
        using (var writer = new StreamWriter(Path.Combine(dir, $"{runName}-scene-work-{phase}.csv"))) {
            writer.WriteLine("artistId,personId,year,stageHours,feeShare,attributedHours,backgroundHours,unbudgetedHours,attributedRoadHours,backgroundRoadHours");
            foreach (var row in state.Work) writer.WriteLine(FormattableString.Invariant($"{row.ArtistId},{row.PersonId},{row.Year},{row.StageHours},{row.FeeShare},{row.AttributedHours},{row.BackgroundHours},{row.UnbudgetedHours},{row.AttributedRoadHours},{row.BackgroundRoadHours}"));
        }
        using (var writer = new StreamWriter(Path.Combine(dir, $"{runName}-scene-room-years-{phase}.csv"))) {
            writer.WriteLine("roomId,placeId,kind,terms,calibrated,year,capacity,admission,bills,sets,attendance,receipts,actPay,playerNights");
            foreach (var row in LocalSceneRoomService.RoomYears.OrderBy(r => r.RoomId, StringComparer.Ordinal).ThenBy(r => r.Year)) {
                var room = SceneRoomCatalog.Get(row.RoomId);
                writer.WriteLine(FormattableString.Invariant($"{row.RoomId},{room.PlaceId},{room.Kind},{SceneLiveEconomics.Terms(room.Kind)},{SceneLiveEconomics.Calibrated},{row.Year},{row.Capacity},{row.Admission},{row.Bills},{row.Sets},{row.Attendance},{row.Receipts},{row.ActPay},{row.PlayerNights}"));
            }
        }
        GD.Print($"SCENE_ROOM_CENSUS phase={phase} bills={state.Bills.Count} completed={state.CompletedPerformances} workRows={state.Work.Count} duplicateBills={duplicateBills} personConflicts={conflicts} remoteDays={remoteDays} capacityErrors={badCounts}");
        if (conflicts + remoteDays + duplicateBills + badCounts != 0) throw new InvalidOperationException("Scene room census integrity failed.");
    }
}
