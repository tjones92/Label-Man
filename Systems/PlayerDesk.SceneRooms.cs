using System;
using System.Linq;
using Godot;

public partial class PlayerDesk {
    public bool ScoutSceneRoom(string roomId, out string message) => ListenSceneRoom(roomId, null, out message);
    public bool ListenSceneAppearance(string billId, string eventId, out string message) {
        var bill = LocalSceneRoomService.Bill(billId);
        if (bill == null) { message = "That bill is no longer on the calendar."; return false; }
        return ListenSceneRoom(bill.RoomId, eventId, out message);
    }
    private bool ListenSceneRoom(string roomId, string eventId, out string message, ScoutingListener listener = null) {
        if (!LocalScenes.ScoutingExperience || LocalScenes.ScoutingExperienceObserver) { message = "Scouting encounters are disabled or in observer mode. Existing evidence is retained."; return false; }
        if (!LocalScenes.Rooms) { message = "The room calendar is disabled."; return false; }
        var room = SceneRoomCatalog.Get(roomId);
        if (room?.PlaceId != LocalScenePersistenceService.SceneIdFor(CurrentCityId)) { message = "You need to be in this room's city."; return false; }
        if (!Require(0, out message)) return false;
        var clock = TimeManager.Instance; var date = clock.CurrentDate;
        int now = clock.CurrentHour * 60 + clock.CurrentMinute;
        var bill = LocalSceneRoomService.Calendar(CurrentCityId, date).FirstOrDefault(b => b.RoomId == roomId && b.Day == LocalSceneRoomService.Day(date)
            && b.Status == SceneBillStatus.Scheduled && b.StartHour * 60 <= now && b.EndHour * 60 > now);
        if (bill == null) { message = "No set or listening appointment is open now. Wait for an announced appearance."; return false; }
        var slots = bill.Appearances.Where(a => a.Status == SceneBillStatus.Scheduled && a.StartHour * 60 <= now && a.EndHour * 60 > now
            && (eventId == null || ScenePerformanceService.EventId(bill, a) == eventId)).ToArray();
        if (eventId == null) slots = bill.Appearances.Where(a => a.Status == SceneBillStatus.Scheduled && a.EndHour * 60 > now
            && a.StartHour * 60 < now + ScoutHours * 60).ToArray();
        if (slots.Length == 0) { message = "That appearance is not playing now. You can wait for its announced start."; return false; }
        slots = slots.Where(a => !HasObservedPerformance(ScenePerformanceService.EventId(bill, a))).ToArray();
        if (slots.Length == 0) { message = "This performance is already in your notebook. Another date can answer a new question."; return false; }
        var hearable = LocalSceneRoomService.Hear(bill, clock.CurrentHour, (int)Math.Ceiling((now + ScoutHours * 60) / 60.0)).Select(a => a.artistId).ToHashSet();
        slots = slots.Where(a => hearable.Contains(a.ArtistId)).ToArray();
        if (slots.Length == 0) { message = "The announced lineup cannot go ahead. No replacement act has been created."; return false; }
        // Snapshot coverage before time advances. A later appearance is never credited unless we stay through it.
        int end = eventId != null ? slots.Max(a => a.EndHour) * 60 : Math.Min(now + ScoutHours * 60, slots.Max(a => a.EndHour) * 60);
        int duration = end - now;
        if (!clock.CanAffordMinutes(duration)) { message = "This listening would pass today's hard stop. Choose an earlier set."; return false; }
        float admission = AdmissionForVisit(bill, room, now) + (listener?.Admission ?? 0);
        float postedAdmission = SceneLiveEconomics.Admission(room, bill.Year);
        int listedActs = bill.Appearances.Select(a => a.ArtistId).Distinct(StringComparer.Ordinal).Count();
        var admittedListeners = scoutingKnowledge.Visit is { } priorVisit && priorVisit.BillId == bill.Id && priorVisit.Day == bill.Day && priorVisit.EndMinute == now
            ? (priorVisit.ListenerIds ?? new()).ToList() : new System.Collections.Generic.List<string>();
        if (listener != null && !admittedListeners.Contains(listener.PersonId)) admittedListeners.Add(listener.PersonId);
        float consultation = listener?.ConsultationFee ?? 0;
        if (Label.cashReserves < admission + consultation) { message = "You cannot cover admission and any listening consultation."; return false; }
        AdoptLegacyDiscoveries(); Label.cashReserves -= admission + consultation; Label.monthlyExpenses += consultation;
        clock.SpendMinutes(duration);
        scoutingKnowledge.Visit = new() { BillId = bill.Id, Day = bill.Day, EndMinute = end, AdmissionPaid = admission, ListenerIds = admittedListeners };
        var resolved = LocalSceneRoomService.Bill(bill.Id);
        slate.Clear(); SlateDate = date;
        foreach (var booked in slots) {
            var slot = resolved?.Appearances.FirstOrDefault(a => a.ArtistId == booked.ArtistId && a.StartHour == booked.StartHour);
            if (slot?.Status != SceneBillStatus.Performed || slot.Performance == null) continue;
            int from = Math.Max(now, slot.StartHour * 60), through = Math.Min(end, slot.EndHour * 60);
            float coverage = (float)(through - from) / ((slot.EndHour - slot.StartHour) * 60);
            if (coverage <= 0) continue;
            var artist = ArtistManager.Instance.GetArtist(slot.ArtistId);
            if (artist == null) continue;
            string identity = ScenePerformanceService.EventId(bill, slot);
            var dossier = Dossier(artist.artistId, artist.stageName);
            dossier.Observations.Add(new() { Id = $"observation-v1|{Label.labelId}|{identity}", LabelId = Label.labelId, ArtistId = artist.artistId,
                ArtistName = artist.stageName, Kind = ScoutingEvidenceKind.Performance, EventId = identity, BillId = bill.Id, RoomId = room.Id, RoomName = room.Name,
                SourceId = Label.labelId, SourceName = "Your listening", PlaceId = room.PlaceId,
                PlaceName = ScenePlaceRegistry.Get(room.PlaceId)?.Name, Channel = room.IsPerformance ? "live set" : "listening appointment",
                OccurredDay = bill.Day, AcquiredDay = LocalSceneRoomService.Day(clock.CurrentDate), AcquiredMinute = clock.CurrentHour * 60 + clock.CurrentMinute, StartMinute = from, EndMinute = through, Coverage = coverage,
                Credibility = coverage >= .9 ? "full set impression; observer judgment remains uncertain" : "partial set impression; limited coverage",
                PersonIds = slot.PersonIds.ToList(), BackingPersonIds = slot.BackingPersonIds?.ToList() ?? new(), Set = LocalSceneRoomService.Copy(slot.Set),
                Interpretation = ScenePerformanceService.Interpret(slot.Performance, Label.labelId, Label.scoutingAbility, coverage, room.Kind),
                BillAttendance = resolved.Status == SceneBillStatus.Performed ? resolved.Attendance : null, BillCapacity = LocalSceneRoomService.CapacityOf(resolved),
                BillListedActs = listedActs, BillAdmissionQuote = postedAdmission,
                // You saw who came in for the set and who stayed only if you were there for most of it.
                SetPresent = coverage >= .5f ? slot.Performance.Audience?.Present : null,
                SetArrivals = coverage >= .5f ? slot.Performance.Audience?.ArrivedFor : null,
                SetStayedShare = coverage >= .5f ? slot.Performance.Audience?.StayedShare : null,
                SetRequests = coverage >= .5f ? slot.Performance.Audience?.Requests : null,
                Conditions = $"{slot.Role}; {SceneRoomCharacter.KindName(room.Kind)}; {(slot.Residency ? "regular engagement" : "announced booking")}. Repertoire is the scheduled set; partial coverage does not identify every song heard." });
            if (listener != null) RememberScoutingListener(dossier, dossier.Observations.Last(), slot.Performance, listener);
            var prospect = new Prospect { Artist = artist, Venue = room.Category, CityId = CurrentCityId, SceneRoomId = room.Id, SceneBillId = bill.Id,
                AskingAdvance = VenueAdvanceAsk(artist, room.Category, AskScaleFor(CurrentCityId)), Note = $"Heard the {slot.Role} at {room.Name}." };
            if (coverage >= .9) prospect.LiveSet.AddRange(ReadSceneRoomSet(artist, resolved, slot));
            prospect.HeardCount = prospect.LiveSet.Count;
            var watched = notebook.FirstOrDefault(n => n.Artist?.artistId == artist.artistId);
            if (watched != null) {
                watched.SceneRoomId = room.Id; watched.SceneBillId = bill.Id; watched.LastSeen = date;
                watched.LiveSet.Clear(); watched.LiveSet.AddRange(prospect.LiveSet); watched.HeardCount = prospect.HeardCount;
                watched.Note = prospect.Note; watched.FollowedUp = true;
            }
            slate.Add(prospect); RememberRoomEncounter(artist.artistId, room, bill, date);
        }
        RememberSceneContact(room, bill);
        message = slate.Count == 0 ? "The scheduled set changed during the visit. Time and admission were consumed; no performance evidence was invented."
            : $"Heard {slate.Count} {(slate.Count == 1 ? "act" : "acts")} at {room.Name}. Evidence saved automatically. You can discuss terms now or choose a question for another date.";
        if (listener != null && slate.Count > 0) message += $" {listener.Name} left a separate judgment of this same performance.";
        Note(message); Changed?.Invoke(); return true;
    }
    private bool HasObservedPerformance(string eventId) => scoutingKnowledge.Dossiers.Any(d => d.Observations.Any(o => o.EventId == eventId && o.Kind == ScoutingEvidenceKind.Performance));
    public float AdmissionForVisit(SceneBill bill, SceneRoomProfile room, int now) =>
        scoutingKnowledge.Visit?.BillId == bill.Id && scoutingKnowledge.Visit.Day == bill.Day && scoutingKnowledge.Visit.EndMinute == now
            ? 0 : SceneLiveEconomics.Admission(room, bill.Year);
    public bool WaitForSceneAppearance(string billId, string eventId, out string message) {
        if (!LocalScenes.Rooms || !Require(0, out message)) { message = "A room calendar and an active label are required."; return false; }
        var bill = LocalSceneRoomService.Bill(billId); var clock = TimeManager.Instance;
        var slot = bill?.Appearances.FirstOrDefault(a => ScenePerformanceService.EventId(bill, a) == eventId);
        if (slot == null || slot.Status != SceneBillStatus.Scheduled || bill.Status != SceneBillStatus.Scheduled) { message = "That appearance is no longer scheduled."; return false; }
        if (SceneRoomCatalog.Get(bill.RoomId)?.PlaceId != LocalScenePersistenceService.SceneIdFor(CurrentCityId)) { message = "Travel to this city before waiting at the room."; return false; }
        int day = LocalSceneRoomService.Day(clock.CurrentDate), now = clock.CurrentHour * 60 + clock.CurrentMinute;
        if (bill.Day < day || bill.Day == day && slot.StartHour * 60 < now) { message = "The set has already started; catch the remaining performance or choose another date."; return false; }
        if (slot.EndHour > clock.HardStopHour) { message = "This appearance ends after your hard stop. Choose an earlier set."; return false; }
        if (bill.Day > day && slot.StartHour < clock.WorkdayStartHour) { message = "This start is before your working day. Choose a later set."; return false; }
        while (LocalSceneRoomService.Day(clock.CurrentDate) < bill.Day && !clock.IsGameOver && !IsGameOver) clock.EndDay();
        if (clock.IsGameOver || IsGameOver) { message = "The waiting ended with the label's operating window."; return false; }
        bool staying = scoutingKnowledge.Visit?.BillId == bill.Id && scoutingKnowledge.Visit.Day == bill.Day &&
            scoutingKnowledge.Visit.EndMinute == clock.CurrentHour * 60 + clock.CurrentMinute;
        int minutes = slot.StartHour * 60 - (clock.CurrentHour * 60 + clock.CurrentMinute);
        if (minutes > 0 && !clock.SpendMinutes(minutes)) { message = "The start cannot be reached within today's time budget."; return false; }
        if (staying) scoutingKnowledge.Visit.EndMinute = slot.StartHour * 60;
        var current = LocalSceneRoomService.Bill(billId);
        message = current?.Appearances.Any(a => ScenePerformanceService.EventId(current, a) == eventId && a.Status == SceneBillStatus.Scheduled) == true
            ? "Waited until the announced start. Check the bill and listen when ready." : "Time passed normally, but the announced set changed. Check another bill.";
        Note(message); Changed?.Invoke(); return true;
    }
    private bool FollowUpScoutingEvidence(Prospect prospect, out string message) {
        var date = TimeManager.Instance.CurrentDate; int now = TimeManager.Instance.CurrentHour;
        var next = LocalSceneRoomService.Calendar(CurrentCityId, date).SelectMany(b => b.Appearances.Select(a => (b, a)))
            .FirstOrDefault(x => x.b.Day == LocalSceneRoomService.Day(date) && x.a.ArtistId == prospect.Artist.artistId && x.a.StartHour <= now && x.a.EndHour > now
                && x.a.Status == SceneBillStatus.Scheduled && !HasObservedPerformance(ScenePerformanceService.EventId(x.b, x.a)));
        if (next.b == null) { message = NextScoutingOpportunity(prospect.Artist.artistId); return false; }
        if (!ListenSceneAppearance(next.b.Id, ScenePerformanceService.EventId(next.b, next.a), out message)) return false;
        var heard = slate.FirstOrDefault(p => p.Artist.artistId == prospect.Artist.artistId);
        if (heard != null) {
            prospect.SceneRoomId = heard.SceneRoomId; prospect.SceneBillId = heard.SceneBillId;
            prospect.LiveSet.Clear(); prospect.LiveSet.AddRange(heard.LiveSet); prospect.HeardCount = heard.HeardCount;
            prospect.Note = heard.Note; prospect.FollowedUp = true;
            slate.Remove(heard); slate.Add(prospect);
        }
        return true;
    }
    private System.Collections.Generic.List<RepertoireItem> ReadSceneRoomSet(SimulatedArtist artist, SceneBill bill, SceneAppearance appearance = null) {
        var set = new System.Collections.Generic.List<RepertoireItem>();
        float noise = ScoutingReadNoise(Label.scoutingAbility);
        foreach (var song in (appearance ?? bill.Appearances.First(s => s.ArtistId == artist.artistId)).Set) {
                var canonical = string.IsNullOrEmpty(song.SongId) ? null : CompositionCatalogService.GetSong(song.SongId);
                float readNoise = (float)(LocalSceneIdentityService.AssignmentUnit(LocalSceneIdentityService.KeyedSeed,
                    $"room-read-v1|{Label.labelId}|{bill.Id}|{artist.artistId}|{song.SongId ?? song.Title}") * 2 - 1) * noise;
                set.Add(new RepertoireItem { Title = song.Title, SongId = song.SongId, Genre = song.Genre,
                    IsOriginal = song.Original, ContentContext = song.ContentContext, SourceTag = song.Original ? "their own" : "standard",
                    ReadHook = Mathf.Clamp((canonical?.commercialHook ?? artist.songwritingAbility) + readNoise, 0, 1),
                    ReadQuality = Mathf.Clamp((canonical?.GetCraftScore() ?? artist.songwritingAbility) + readNoise, 0, 1) });
        }
        return set;
    }
    private void RememberRoomEncounter(string artistId, SceneRoomProfile room, SceneBill bill, GameDate date) {
        RememberSceneDiscovery(artistId, CurrentCityId, room.Category, ChartManager.Instance.GetCurrentChartWeek());
        var knowledge = sceneDiscoveries[artistId];
        knowledge.RoomId = room.Id; knowledge.BillId = bill.Id; knowledge.ContactId = room.ContactId;
        knowledge.LastDay = LocalSceneRoomService.Day(date);
    }
    private bool ScoutScheduledCategory(ScoutingVenue category, out string message) {
        var date = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
        int hour = TimeManager.Instance?.CurrentHour ?? 9;
        var room = SceneRoomCatalog.ForPlace(CurrentCityId).FirstOrDefault(r => r.Category == category &&
            LocalSceneRoomService.CurrentBill(r.Id, date, hour) != null);
        if (room == null) { message = "No announced bill in this kind of room is open now. Check the named rooms' calendars."; return false; }
        return ScoutSceneRoom(room.Id, out message);
    }
}
