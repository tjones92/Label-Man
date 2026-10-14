using System;
using System.Linq;

public sealed class ScoutingOpportunity {
    public string ArtistId { get; init; }
    public string ArtistName { get; init; }
    public string CityId { get; init; }
    public int DaysUntil { get; init; }
    public int WaitingMinutes { get; init; }
    public int TravelMinutes { get; init; }
    public float TravelCost { get; init; }
    public bool Affordable { get; init; }
    public string BillId { get; init; }
    public string EventId { get; init; }
    public string RoomId { get; init; }
    public string RoomName { get; init; }
    public int Day { get; init; }
    public int StartMinute { get; init; }
    public int EndMinute { get; init; }
    public int ListeningMinutes { get; init; }
    public float Admission { get; init; }
    public bool Playing { get; init; }
}

public partial class PlayerDesk {
    public bool SetScoutingQuestion(string artistId, ScoutingQuestion question) {
        if (!Enum.IsDefined(question)) return false;
        var dossier = scoutingKnowledge.Dossiers.FirstOrDefault(d => d.ArtistId == artistId);
        if (dossier == null) return false;
        dossier.Question = question;
        Changed?.Invoke();
        return true;
    }

    public static string ScoutingQuestionText(ScoutingQuestion question) => question switch {
        ScoutingQuestion.Vocals => "Do the vocals hold together on another date?",
        ScoutingQuestion.Ensemble => "Does the ensemble hold together on another date?",
        ScoutingQuestion.Material => "How does the material come across on another date?",
        ScoutingQuestion.Stage => "How do they communicate on another date?",
        _ => "No listening question pinned."
    };

    // Both notebook and pinned questions use the same dated, context-aware evidence comparison.
    public static string ScoutingQuestionRead(ScoutingDossier dossier) => dossier.Question == ScoutingQuestion.None ? null :
        ScoutingComparison.Read(dossier, dossier.Question, "Follow-up: ");

    public ScoutingOpportunity NextReachableScoutingSet(string artistId) {
        return ScoutingItinerary().FirstOrDefault(o => o.ArtistId == artistId);
    }

    // Local choices first; circuit choices require an actual drive that fits today.
    // Affordability is a current quote, never a guarantee about cash after future settlement.
    public System.Collections.Generic.IReadOnlyList<ScoutingOpportunity> ScoutingItinerary(bool includeCircuit = false) {
        var choices = new System.Collections.Generic.List<ScoutingOpportunity>();
        if (Label == null || IsGameOver || !LocalScenes.Rooms || !LocalScenes.ScoutingExperience || LocalScenes.ScoutingExperienceObserver) return choices;
        var clock = TimeManager.Instance;
        int day = LocalSceneRoomService.Day(clock.CurrentDate), now = clock.CurrentHour * 60 + clock.CurrentMinute;
        var cities = new[] { CurrentCityId }.Concat(includeCircuit ? ReachableCities().Select(c => c.cityId) : Array.Empty<string>());
        foreach (string city in cities) {
            bool local = city == CurrentCityId;
            var travel = local ? (Hours: 0, Gas: 0f) : DriveQuote(CurrentCityId, city);
            if (travel.Hours * 60 > clock.MinutesRemainingWithOvertime) continue;
            foreach (var bill in LocalSceneRoomService.AnnouncedCalendar(city, clock.CurrentDate).Where(b => b.Status == SceneBillStatus.Scheduled)) {
                var room = SceneRoomCatalog.Get(bill.RoomId);
                if (room == null || bill.Day > LocalSceneRoomService.Day(GameDate.EndDate)) continue;
                var hearable = LocalSceneRoomService.Hear(bill).Select(a => a.artistId).ToHashSet();
                foreach (var slot in bill.Appearances.Where(a => a.Status == SceneBillStatus.Scheduled)) {
                    string eventId = ScenePerformanceService.EventId(bill, slot);
                    int start = slot.StartHour * 60, end = slot.EndHour * 60;
                    if (end > clock.HardStopHour * 60 || !hearable.Contains(slot.ArtistId) || HasObservedPerformance(eventId)) continue;
                    int arrival = bill.Day == day ? now + travel.Hours * 60 : clock.WorkdayStartHour * 60;
                    if (end <= arrival || bill.Day > day && start < arrival || !local && bill.Day == day && start < arrival) continue;
                    bool playing = local && bill.Day == day && start <= now;
                    float admission = local && bill.Day == day ? AdmissionForVisit(bill, room, now) : SceneLiveEconomics.Admission(room, bill.Year);
                    choices.Add(new() { ArtistId = slot.ArtistId, ArtistName = ArtistManager.Instance.GetArtist(slot.ArtistId)?.stageName,
                        CityId = city, DaysUntil = bill.Day - day, WaitingMinutes = Math.Max(0, start - arrival),
                        TravelMinutes = travel.Hours * 60, TravelCost = travel.Gas, Affordable = Label.cashReserves >= admission + travel.Gas,
                        BillId = bill.Id, EventId = eventId, RoomId = room.Id, RoomName = room.Name, Day = bill.Day,
                        StartMinute = start, EndMinute = end, Playing = playing,
                        ListeningMinutes = end - (playing ? now : start), Admission = admission });
                }
            }
        }
        return choices.OrderBy(o => o.TravelMinutes > 0).ThenBy(o => o.Day).ThenBy(o => o.StartMinute)
            .ThenBy(o => o.Admission + o.TravelCost).ThenBy(o => o.EventId, StringComparer.Ordinal).ToArray();
    }
}
