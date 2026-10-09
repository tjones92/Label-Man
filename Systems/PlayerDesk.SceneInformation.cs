using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

public partial class PlayerDesk {
    private SceneInformationKnowledge sceneInformation = new();
    private static SceneInformationKnowledge CopySceneInformation(SceneInformationKnowledge data) =>
        data == null ? new() : JsonSerializer.Deserialize<SceneInformationKnowledge>(
            JsonSerializer.Serialize(data, SaveGameService.TestJsonOptions), SaveGameService.TestJsonOptions);
    private SceneInformationKnowledge CaptureSceneInformation() => CopySceneInformation(sceneInformation);
    private void RestoreSceneInformation(SceneInformationKnowledge data) {
        sceneInformation = CopySceneInformation(data);
        sceneInformation.Contacts ??= new(); sceneInformation.Leads ??= new(); sceneInformation.ReadThroughDay ??= new(); sceneInformation.ReadEvents ??= new();
    }
    private void RememberSceneContact(SceneRoomProfile room, SceneBill bill) {
        var contact = sceneInformation.Contacts.FirstOrDefault(c => c.ContactId == room.ContactId);
        if (contact == null) { contact = new() { ContactId = room.ContactId }; sceneInformation.Contacts.Add(contact); }
        if (!contact.HeardBillIds.Contains(bill.Id)) contact.HeardBillIds.Add(bill.Id);
        if (contact.HeardBillIds.Count > 16) contact.HeardBillIds.RemoveAt(0);
    }
    public int SceneContactFamiliarity(string contactId) => sceneInformation.Contacts
        .FirstOrDefault(c => c.ContactId == contactId)?.HeardBillIds.Distinct().Count() ?? 0;
    public IReadOnlyList<SceneNewsItem> LocalSceneNews() {
        if (!LocalScenes.Rooms) return Array.Empty<SceneNewsItem>();
        int day = LocalSceneRoomService.Day(TimeManager.Instance?.CurrentDate ?? GameDate.StartDate);
        string place = LocalScenePersistenceService.SceneIdFor(CurrentCityId);
        if (place == null) return Array.Empty<SceneNewsItem>();
        return SceneInformationService.Project(place, day,
            ArtistManager.Instance.GetAllArtists().SelectMany(a => a.sceneRecruitmentHistory ?? new()),
            SceneEcosystemService.Programs, SceneEcosystemService.Moves,
            id => ArtistManager.Instance.GetArtist(id)?.stageName ?? "An archived act",
            id => ChartManager.Instance.GetLabelName(id) ?? "a label")
            .Where(n => !sceneInformation.ReadEvents.ContainsKey(n.Id)).Take(8).ToArray();
    }
    public void MarkSceneNewsRead() {
        if (!LocalScenes.Rooms) return;
        foreach (var item in LocalSceneNews()) sceneInformation.ReadEvents[item.Id] = item.AvailableDay;
        int day = LocalSceneRoomService.Day(TimeManager.Instance?.CurrentDate ?? GameDate.StartDate);
        foreach (var id in sceneInformation.ReadEvents.Where(p => day - p.Value > 104 * 7).Select(p => p.Key).ToArray()) sceneInformation.ReadEvents.Remove(id);
        sceneInformation.ReadThroughDay[LocalScenePersistenceService.SceneIdFor(CurrentCityId)] =
            LocalSceneRoomService.Day(TimeManager.Instance?.CurrentDate ?? GameDate.StartDate);
        Changed?.Invoke();
    }
    public bool AskSceneBooker(string roomId, out string message) {
        var room = SceneRoomCatalog.Get(roomId);
        if (Label == null || room == null || !LocalScenes.Rooms || room.PlaceId != LocalScenePersistenceService.SceneIdFor(CurrentCityId)) {
            message = "Visit this room's city to speak with its booker."; return false;
        }
        if (SceneContactFamiliarity(room.ContactId) < 2) {
            message = "Hear two different bills here first so the booker gets to know you."; return false;
        }
        if (!Require(1, out message)) return false;
        var date = TimeManager.Instance.CurrentDate;
        int day = LocalSceneRoomService.Day(date), hour = TimeManager.Instance.CurrentHour;
        if (hour < room.OpenHour || hour >= room.CloseHour) {
            message = "The booker is available during this room's posted hours."; return false;
        }
        var lead = SceneInformationService.Lead(room, LocalSceneRoomService.Calendar(CurrentCityId, date),
            sceneDiscoveries.Keys, sceneInformation.Contacts.First(c => c.ContactId == room.ContactId).HeardBillIds, day, hour);
        if (lead == null || sceneInformation.Leads.Any(l => l.Id == lead.Id)) {
            message = "The booker has no new introduction on the announced bills."; return false;
        }
        Spend(1);
        sceneInformation.Leads.RemoveAll(l => l.ExpiresDay < day);
        sceneInformation.Leads.Add(lead);
        if (sceneInformation.Leads.Count > 64) sceneInformation.Leads.RemoveAt(0);
        message = $"{room.ContactName} suggests hearing {ArtistManager.Instance.GetArtist(lead.ArtistId)?.stageName ?? "the announced act"} at {room.Name} on {LocalSceneRoomService.Date(lead.PerformanceDay).ToHeadlineString()}. Go hear the set before talking terms.";
        Note(message); Changed?.Invoke(); return true;
    }
    public IReadOnlyList<string> SceneLeadNotes() {
        int day = LocalSceneRoomService.Day(TimeManager.Instance?.CurrentDate ?? GameDate.StartDate);
        return sceneInformation.Leads.Where(l => l.ExpiresDay >= day).OrderBy(l => l.PerformanceDay).Select(l => {
            var room = SceneRoomCatalog.Get(l.RoomId);
            var act = ArtistManager.Instance.GetArtist(l.ArtistId);
            var bill = LocalSceneRoomService.Calendar(room?.PlaceId, TimeManager.Instance.CurrentDate).FirstOrDefault(b => b.Id == l.BillId);
            bool valid = bill?.Status == SceneBillStatus.Scheduled && bill.Appearances.Any(a => a.ArtistId == l.ArtistId && a.Status == SceneBillStatus.Scheduled);
            string status = valid ? "check the calendar before going" : "the bill changed or the set has passed; verify with the room";
            return $"{room?.ContactName ?? "Booker"}: {act?.stageName ?? "archived act"} at {room?.Name ?? l.RoomId}, {LocalSceneRoomService.Date(l.PerformanceDay).ToHeadlineString()} — {status}. {l.Evidence}";
        }).ToArray();
    }
}
