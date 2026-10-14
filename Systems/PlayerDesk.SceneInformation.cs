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
        sceneInformation.Tips ??= new();
    }
    private void RememberSceneContact(SceneRoomProfile room, SceneBill bill) {
        var contact = sceneInformation.Contacts.FirstOrDefault(c => c.ContactId == room.ContactId);
        if (contact == null) { contact = new() { ContactId = room.ContactId }; sceneInformation.Contacts.Add(contact); }
        if (!contact.HeardBillIds.Contains(bill.Id)) contact.HeardBillIds.Add(bill.Id);
        if (contact.HeardBillIds.Count > 16) contact.HeardBillIds.RemoveAt(0);
    }
    public int SceneContactFamiliarity(string contactId) => sceneInformation.Contacts
        .FirstOrDefault(c => c.ContactId == contactId)?.HeardBillIds.Distinct().Count() ?? 0;
    public bool KnowsSceneBooker(string roomId) {
        var room = SceneRoomCatalog.Get(roomId);
        return room != null && (sceneInformation.Contacts.Any(c => c.ContactId == room.ContactId && c.IntroducedDay.HasValue) ||
            scoutingKnowledge.Dossiers.Any(d => d.Observations.Any(o => o.SourceId == room.ContactId && o.Kind == ScoutingEvidenceKind.BookerClaim)) ||
            sceneInformation.Leads.Any(l => l.ContactId == room.ContactId));
    }
    public IReadOnlyList<SceneRoomProfile> IntroducedSceneBookers => SceneRoomCatalog.All
        .Where(r => KnowsSceneBooker(r.Id)).ToArray();
    private void RememberBookerIntroduction(SceneRoomProfile room) {
        var contact = sceneInformation.Contacts.FirstOrDefault(c => c.ContactId == room.ContactId);
        if (contact == null) { contact = new() { ContactId = room.ContactId }; sceneInformation.Contacts.Add(contact); }
        contact.IntroducedDay ??= LocalSceneRoomService.Day(TimeManager.Instance.CurrentDate);
    }
    public bool CanIntroduceSceneBooker(string roomId, out string reason) {
        var room = SceneRoomCatalog.Get(roomId); var clock = TimeManager.Instance;
        reason = "";
        if (Label == null || !LocalScenes.Rooms || room == null || room.PlaceId != LocalScenePersistenceService.SceneIdFor(CurrentCityId))
            reason = "Visit the venue's city to introduce yourself.";
        else if (KnowsSceneBooker(roomId)) reason = "You have already met this booker.";
        else if (clock.CurrentHour * 60 + clock.CurrentMinute < room.OpenHour * 60 ||
                 clock.CurrentHour * 60 + clock.CurrentMinute + 15 > room.CloseHour * 60)
            reason = "Introduce yourself during posted venue hours.";
        else if (!clock.CanAffordMinutes(15)) reason = "Allow 15 minutes for an introduction.";
        return reason.Length == 0;
    }
    public bool IntroduceSceneBooker(string roomId, out string message) {
        if (!CanIntroduceSceneBooker(roomId, out message) || !Require(0, out message)) return false;
        var room = SceneRoomCatalog.Get(roomId);
        var clock = TimeManager.Instance;
        bool continuous = scoutingKnowledge.Visit?.Day == LocalSceneRoomService.Day(clock.CurrentDate) &&
            scoutingKnowledge.Visit.EndMinute == clock.CurrentHour * 60 + clock.CurrentMinute &&
            LocalSceneRoomService.Bill(scoutingKnowledge.Visit.BillId)?.RoomId == room.Id;
        SpendMinutes(15);
        RememberBookerIntroduction(room);
        if (continuous) scoutingKnowledge.Visit.EndMinute = clock.CurrentHour * 60 + clock.CurrentMinute;
        else scoutingKnowledge.Visit = null;
        message = $"You meet {room.ContactName}, who books {room.Name}. Added to your Rolodex.";
        Note(message); Changed?.Invoke(); return true;
    }
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
        foreach (var item in LocalSceneNews()) {
            RememberScoutingReport(item);
            sceneInformation.ReadEvents[item.Id] = item.AvailableDay;
        }
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
        if (!Require(1, out message)) return false;
        var date = TimeManager.Instance.CurrentDate;
        int day = LocalSceneRoomService.Day(date), hour = TimeManager.Instance.CurrentHour;
        if (hour < room.OpenHour || hour >= room.CloseHour) {
            message = "The booker is available during this room's posted hours."; return false;
        }
        var lead = SceneInformationService.Lead(room, LocalSceneRoomService.Calendar(CurrentCityId, date),
            sceneDiscoveries.Keys, sceneInformation.Contacts.FirstOrDefault(c => c.ContactId == room.ContactId)?.HeardBillIds ?? new(), day, hour);
        if (lead == null || sceneInformation.Leads.Any(l => l.Id == lead.Id)) {
            message = "The booker has no new introduction on the announced bills."; return false;
        }
        Spend(1);
        RememberBookerIntroduction(room);
        sceneInformation.Leads.RemoveAll(l => l.ExpiresDay < day);
        sceneInformation.Leads.Add(lead);
        if (sceneInformation.Leads.Count > 64) sceneInformation.Leads.RemoveAt(0);
        message = $"{room.ContactName} suggests hearing {ArtistManager.Instance.GetArtist(lead.ArtistId)?.stageName ?? "the announced act"} at {room.Name} on {LocalSceneRoomService.Date(lead.PerformanceDay).ToHeadlineString()}. Go hear the set before talking terms.";
        Note(message); Changed?.Invoke(); return true;
    }
    public const int TipsPerAsk = 3;
    /// <summary>Ask the people on your roster who they know: up to three new tips, each from a real contact edge,
    /// about someone between bands or where a former bandmate or session partner plays now. Fallouts are not passed on.
    /// One hour.</summary>
    public bool AskAround(out string message) {
        message = "";
        if (Label == null || !ContactNetworkService.Enabled) { message = "Nobody to ask."; return false; }
        var roster = ArtistManager.Instance.GetAllArtists().Where(a => a.labelId == Label.labelId && a.lifecycleStatus == ArtistLifecycleStatus.Active)
            .OrderBy(a => a.artistId, StringComparer.Ordinal).ToList();
        var mine = roster.SelectMany(a => a.members.Where(m => m.isActive).Select(m => (act: a, m))).ToList();
        if (mine.Count == 0) { message = "Sign an act first; your musicians are the ones who know people."; return false; }
        var actOf = new Dictionary<string, SimulatedArtist>(StringComparer.Ordinal);
        foreach (var a in ArtistManager.Instance.GetAllArtists().Where(x => x.lifecycleStatus == ArtistLifecycleStatus.Active))
            foreach (var m in a.members) if (m.isActive && m.personId != null) actOf[m.personId] = a;
        var myPeople = mine.Select(x => x.m.personId).ToHashSet(StringComparer.Ordinal);
        int day = LocalSceneRoomService.Day(TimeManager.Instance?.CurrentDate ?? GameDate.StartDate);
        var fresh = new List<ContactTip>();
        foreach (var (act, m) in mine) {
            foreach (string other in ContactNetworkService.ContactsOf(m.personId).OrderBy(o => o, StringComparer.Ordinal)
                .Select(o => (o, e: ContactNetworkService.Edge(m.personId, o))).Where(x => !x.e.Fallout)
                .OrderByDescending(x => x.e.Jobs).ThenByDescending(x => x.e.LastYear).ThenBy(x => x.o, StringComparer.Ordinal).Select(x => x.o)) {
                if (myPeople.Contains(other) || fresh.Count >= TipsPerAsk) continue;
                var person = ArtistManager.Instance.GetMusician(other);
                if (person == null || person.lifeState != MemberLifeState.Active) continue;
                actOf.TryGetValue(other, out var where);
                bool pooled = PersonPool.Contains(other);
                if (where == null && !pooled) continue;
                string id = other + "|" + (where?.artistId ?? "pool");
                if (sceneInformation.Tips.Any(t => t.Id == id) || fresh.Any(t => t.Id == id)) continue;
                string how = ContactNetworkService.HowTheyKnow(m.personId, other);
                string state = where == null ? "is between bands"
                    : $"now plays in {where.stageName}{(string.IsNullOrEmpty(where.labelId) ? ", unsigned" : $", on {ChartManager.Instance.GetLabelName(where.labelId) ?? "a label"}")}";
                fresh.Add(new ContactTip { Id = id, FromPersonId = m.personId, AboutPersonId = other, AboutArtistId = where?.artistId, ReceivedDay = day,
                    Text = $"{m.FullName}{(act.stageName == m.FullName ? "" : $" ({act.stageName})")}: {person.FullName} {state}; {how}." });
            }
        }
        if (fresh.Count == 0) { message = "Your musicians have nothing new on anyone they know."; return false; }
        if (!Require(1, out message)) return false;
        Spend(1);
        sceneInformation.Tips.AddRange(fresh);
        if (sceneInformation.Tips.Count > 48) sceneInformation.Tips.RemoveRange(0, sceneInformation.Tips.Count - 48);
        message = string.Join(" ", fresh.Select(t => t.Text));
        Note(message); Changed?.Invoke(); return true;
    }
    public IReadOnlyList<string> ContactTipNotes() => sceneInformation.Tips.OrderByDescending(t => t.ReceivedDay).Take(6).Select(t => {
        bool pooled = t.AboutArtistId == null;
        bool stale = pooled ? !PersonPool.Contains(t.AboutPersonId)
            : ArtistManager.Instance.GetArtist(t.AboutArtistId)?.members.Any(m => m.isActive && m.personId == t.AboutPersonId) != true;
        return $"{LocalSceneRoomService.Date(t.ReceivedDay).ToHeadlineString()} · {t.Text}{(stale ? " (That has changed since.)" : "")}";
    }).ToArray();
    public IReadOnlyList<string> SceneLeadNotes() {
        int day = LocalSceneRoomService.Day(TimeManager.Instance?.CurrentDate ?? GameDate.StartDate);
        return sceneInformation.Leads.Where(l => l.ExpiresDay >= day).OrderBy(l => l.PerformanceDay).Select(l => {
            var room = SceneRoomCatalog.Get(l.RoomId);
            var act = ArtistManager.Instance.GetArtist(l.ArtistId);
            var bill = LocalSceneRoomService.Calendar(room?.PlaceId, TimeManager.Instance.CurrentDate).FirstOrDefault(b => b.Id == l.BillId);
            bool valid = bill?.Status == SceneBillStatus.Scheduled && bill.Appearances.Any(a => a.ArtistId == l.ArtistId && a.Status == SceneBillStatus.Scheduled);
            string status = valid ? "check the calendar before going" : "the bill changed or the set has passed; verify with the room";
            return $"{room?.ContactTitle ?? "Booker"}: {act?.stageName ?? "archived act"}, {LocalSceneRoomService.Date(l.PerformanceDay).ToHeadlineString()} — {status}. {l.Evidence}";
        }).ToArray();
    }
}
