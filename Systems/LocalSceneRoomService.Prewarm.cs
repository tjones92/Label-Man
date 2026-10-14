using System;
using System.Collections.Generic;
using System.Linq;

public static partial class LocalSceneRoomService {
    /// <summary>World creation only: run the existing booking rules over the weeks before the calendar begins, so acts
    /// arrive with the room standing they had already earned. The world as it stands on the start date stands in for
    /// the recent past (who is in town, lineups, the rooms' families); an act formed after a historical year does not
    /// play it. Each historical set settles one fact, room standing (PriorShows), and nothing else: no performance
    /// outcome, attendance, pay, stage hours, contacts, house-band standing or repertoire. The bills and term engagements
    /// are discarded before the live calendar opens; a term ends at the start date, so no historical engagement carries.
    /// Draws are the room owner's keyed draws, so no global RNG is consumed. A loaded or running world never gains it.</summary>
    public static void Prewarm(GameDate start) {
        if (!LocalScenes.Rooms || !LocalScenes.RoomPrewarm || restoring) return;
        int startDay = Day(start);
        if (startDay < 0 || State.LastScheduledWeek >= 0 || State.PriorWeeks > 0 || State.Bills.Count > 0 ||
            State.Standing.Count > 0 || State.Engagements.Count > 0) return;
        // The same reconcile EnsureCalendar makes first; scheduling needs the scene index.
        LocalScenePersistenceService.Advance(ChartManager.Instance?.GetCurrentChartWeek() ?? 0, start.year);
        int weeks = LocalScenes.RoomPrewarmWeeks, firstDay = startDay - 7 * weeks;
        try {
            for (int week = FloorWeek(firstDay); week <= FloorWeek(startDay - 1); week++) {
                Schedule(week, Math.Max(week * 7, firstDay), asOfDay: startDay);
                foreach (var bill in State.Bills.Where(b => b.Day < startDay)) SettlePrior(bill);
                State.Bills.Clear();
            }
        } finally { priorCast.Clear(); }
        State.Engagements.Clear();
        State.PriorWeeks = weeks;
        Godot.GD.Print($"SceneRooms: pre-rolled {weeks} weeks of room history -- {State.Standing.Select(s => s.ArtistId).Distinct().Count()} acts, " +
            $"{State.Standing.Sum(s => s.PriorShows)} prior shows");
    }
    // The world on the start date does not change while the past is booked, so each room's cast is read once a year.
    private static readonly Dictionary<(string Room, int Year), SimulatedArtist[]> priorCast = new();
    private static int FloorWeek(int day) => day >= 0 ? day / 7 : (day - 6) / 7;
    /// <summary>Pre-roll eligibility: acts as they stand on <paramref name="asOfDay"/>, formed by the historical year.</summary>
    private static SimulatedArtist[] EligibleBefore(SceneRoomProfile room, int day, int asOfDay) {
        int year = Date(day).year;
        if (!priorCast.TryGetValue((room.Id, year), out var cast))
            priorCast[(room.Id, year)] = cast = LocalScenePersistenceService.MembersOf(room.PlaceId).Select(ArtistManager.Instance.GetArtist)
                .Where(a => a != null && a.formedYear <= year && Eligible(a, room, asOfDay)).ToArray();
        return cast;
    }
    private static void SettlePrior(SceneBill bill) {
        var room = SceneRoomCatalog.Get(bill.RoomId);
        if (room?.IsPerformance != true) return;
        foreach (var slot in bill.Appearances) {
            string key = StandingKey(room.Id, slot.ArtistId);
            if (!standing.TryGetValue(key, out var rank)) {
                rank = new() { RoomId = room.Id, ArtistId = slot.ArtistId };
                standing.Add(key, rank); State.Standing.Add(rank);
            }
            rank.PriorShows++; rank.LastDay = bill.Day;
        }
    }
}
