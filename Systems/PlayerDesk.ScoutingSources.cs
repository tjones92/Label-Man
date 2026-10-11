using System;
using System.Linq;

public enum ScoutingSourceQuestion { BookingHistory, NextDate, AnnouncedMaterial, BillAudience }

// Ephemeral quote from the existing room owner. No reservation or new world fact.
public sealed class ScoutingSourceAnswer {
    public string EventId { get; init; }
    public string QuoteId => EventId + "|" + Claim;
    public string BillId { get; init; }
    public string RoomId { get; init; }
    public string PlaceName { get; init; }
    public string SourceId { get; init; }
    public string SourceName { get; init; }
    public string Claim { get; init; }
    public int? OccurredDay { get; init; }
    public int? BillAttendance { get; init; }
    public int BillCapacity { get; init; }
    public int? BillListedActs { get; init; }
    public bool Available { get; init; }
    public string UnavailableReason { get; init; }
}

public partial class PlayerDesk {
    public static string ScoutingSourceQuestionText(ScoutingSourceQuestion question) => question switch {
        ScoutingSourceQuestion.BookingHistory => "How often have they played here?",
        ScoutingSourceQuestion.NextDate => "When are they booked here next?",
        ScoutingSourceQuestion.AnnouncedMaterial => "What material is announced for their next set?",
        ScoutingSourceQuestion.BillAudience => "How many attended their last completed bill here?",
        _ => "Unknown question"
    };

    public ScoutingSourceAnswer PreviewScoutingSource(string roomId, string artistId, ScoutingSourceQuestion question) {
        if (!Enum.IsDefined(question) || Label == null || !LocalScenes.Rooms || !LocalScenes.ScoutingExperience || LocalScenes.ScoutingExperienceObserver) return null;
        var room = SceneRoomCatalog.Get(roomId);
        if (room == null || room.PlaceId != LocalScenePersistenceService.SceneIdFor(CurrentCityId) || ArtistManager.Instance.GetArtist(artistId) == null) return null;
        var clock = TimeManager.Instance;
        int day = LocalSceneRoomService.Day(clock.CurrentDate), minute = clock.CurrentHour * 60 + clock.CurrentMinute;
        var calendar = LocalSceneRoomService.Calendar(CurrentCityId, clock.CurrentDate);
        var upcoming = calendar.Where(b => b.RoomId == roomId && b.Status == SceneBillStatus.Scheduled &&
                (b.Day > day || b.Day == day && b.EndHour * 60 > minute))
            .SelectMany(b => b.Appearances.Where(a => a.ArtistId == artistId && a.Status == SceneBillStatus.Scheduled &&
                (b.Day > day || a.EndHour * 60 > minute)).Select(a => (bill: b, slot: a)))
            .OrderBy(x => x.bill.Day).ThenBy(x => x.slot.StartHour).ThenBy(x => x.bill.Id, StringComparer.Ordinal).FirstOrDefault();
        string eventId, claim, billId = null; int? occurred = null, attendance = null, listedActs = null; int capacity = 0;
        switch (question) {
            case ScoutingSourceQuestion.BookingHistory:
                int shows = LocalSceneRoomService.ShowsAt(roomId, artistId);
                eventId = $"booker-history|{roomId}|{artistId}|{shows}";
                claim = $"Our retained work record has {shows} completed appearance(s) here. That says nothing about punctuality or audiences in another room.";
                break;
            case ScoutingSourceQuestion.BillAudience:
                var completed = LocalSceneRoomService.LastCompletedBill(roomId, artistId, day, minute);
                billId = completed?.Id; occurred = completed?.Day;
                attendance = completed?.Attendance; capacity = completed?.Capacity ?? 0;
                listedActs = completed?.Appearances.Select(a => a.ArtistId).Distinct(StringComparer.Ordinal).Count();
                eventId = $"booker-audience|{roomId}|{artistId}|{billId ?? "none"}";
                claim = completed == null ? "No completed bill with a capacity and attendance record for this act remains in our calendar. Their individual draw is unknown."
                    : $"Our completed bill record for {LocalSceneRoomService.Date(completed.Day).ToHeadlineString()} counts {attendance} attendees against a capacity of {capacity}. That is the whole bill, including the other acts; it does not show how many came for this act or would follow them elsewhere.";
                break;
            default:
                billId = upcoming.bill?.Id; occurred = upcoming.bill?.Day;
                string appearance = upcoming.bill == null ? "none" : ScenePerformanceService.EventId(upcoming.bill, upcoming.slot);
                if (question == ScoutingSourceQuestion.NextDate) {
                    eventId = $"booker-date|{roomId}|{artistId}|{billId ?? "none"}";
                    claim = upcoming.bill == null ? "No further booking for this act is announced here." :
                        $"They are announced here on {LocalSceneRoomService.Date(upcoming.bill.Day).ToHeadlineString()}, {upcoming.slot.StartHour:00}:00–{upcoming.slot.EndHour:00}:00. The calendar can still change.";
                } else {
                    // Content is part of identity: a genuine programme revision is a new answer.
                    string material = upcoming.slot == null ? "" : string.Join("; ", upcoming.slot.Set.Select(s => s.Title + (s.Original ? " (original)" : " (standard)")));
                    eventId = $"booker-material|{roomId}|{artistId}|{appearance}|{material}";
                    claim = upcoming.bill == null ? "No further set for this act is announced here; there is no programme to report." :
                        $"The programme for {LocalSceneRoomService.Date(upcoming.bill.Day).ToHeadlineString()}, {upcoming.slot.StartHour:00}:00 lists " +
                        (material.Length == 0 ? "no individual titles." : material + ".") + " These are announced titles, not evidence that you heard them or that the arrangements are ready. The set can change.";
                }
                break;
        }
        string blocked = null;
        if (scoutingKnowledge.Dossiers.FirstOrDefault(d => d.ArtistId == artistId)?.Observations.Any(o => o.EventId == eventId) == true)
            blocked = "You already recorded this answer. There is no new information to buy.";
        else if (minute < room.OpenHour * 60 || minute + 60 > room.CloseHour * 60)
            blocked = "Allow an hour within the booker's posted room hours.";
        else if (minute + 60 > clock.HardStopHour * 60 || clock.HoursRemainingWithOvertime < 1)
            blocked = "There is not an hour left for this conversation today.";
        return new() { EventId = eventId, BillId = billId, RoomId = room.Id, SourceId = room.ContactId, SourceName = room.ContactName,
            PlaceName = ScenePlaceRegistry.Get(room.PlaceId)?.Name,
            Claim = claim, OccurredDay = occurred, BillAttendance = attendance, BillCapacity = capacity,
            BillListedActs = listedActs,
            Available = blocked == null, UnavailableReason = blocked };
    }

    public bool AskScoutingSource(string roomId, string artistId, ScoutingSourceQuestion question, string expectedQuoteId, out string message) {
        var answer = PreviewScoutingSource(roomId, artistId, question);
        if (answer == null) { message = "Visit the room's city with scouting encounters enabled to ask its booker."; return false; }
        if (expectedQuoteId != null && expectedQuoteId != answer.QuoteId) { message = "The booker's information changed. Review the question again before spending an hour."; return false; }
        if (!answer.Available) { message = answer.UnavailableReason; return false; }
        if (!Require(1, out message)) return false;
        var clock = TimeManager.Instance;
        // Snapshot the source's answer before the ordinary world advances; later changes do not rewrite it.
        Spend(1);
        scoutingKnowledge.Visit = null;
        var artist = ArtistManager.Instance.GetArtist(artistId);
        var room = SceneRoomCatalog.Get(roomId);
        Dossier(artistId, artist.stageName).Observations.Add(new() { Id = $"claim-v1|{Label.labelId}|{answer.EventId}", EventId = answer.EventId,
            BillId = answer.BillId, LabelId = Label.labelId, ArtistId = artistId, ArtistName = artist.stageName,
            Kind = ScoutingEvidenceKind.BookerClaim, RoomId = roomId, RoomName = room.Name, SourceId = answer.SourceId, SourceName = answer.SourceName,
            Channel = "booker conversation", PlaceId = room.PlaceId, PlaceName = answer.PlaceName,
            OccurredDay = answer.OccurredDay, AcquiredDay = LocalSceneRoomService.Day(clock.CurrentDate),
            AcquiredMinute = clock.CurrentHour * 60 + clock.CurrentMinute, Credibility = "local booking record; limited to this room", Claim = answer.Claim,
            BillAttendance = answer.BillAttendance, BillCapacity = answer.BillCapacity, BillListedActs = answer.BillListedActs });
        message = answer.SourceName + ": " + answer.Claim; Note(message); Changed?.Invoke(); return true;
    }
}
