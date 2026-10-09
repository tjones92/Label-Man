using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Public, delayed projection of existing evidence. No births, prices, RNG or offers.</summary>
public static class SceneInformationService {
    public static IReadOnlyList<SceneNewsItem> Project(string placeId, int day,
        IEnumerable<SceneRecruitmentRecord> contracts, IEnumerable<SceneProgram> programs,
        IEnumerable<SceneFundedMove> moves, Func<string, string> artistName, Func<string, string> labelName) {
        var news = new List<SceneNewsItem>();
        if (ScenePlaceRegistry.Get(placeId) == null) return news;
        foreach (var r in (contracts ?? Array.Empty<SceneRecruitmentRecord>()).Where(r => r != null &&
            r.Phase is "DailyMarket" or "WeeklyMarket" or "PlayerRival" or "PlayerContract" && r.Week >= 0 &&
            !string.IsNullOrEmpty(r.ArtistId) && !string.IsNullOrEmpty(r.LabelId))) {
            // Contract ledger has chart-week precision. Publish after the entire event week.
            int eventDay = r.Week * 7, available = eventDay + 7;
            if (LocalScenePersistenceService.SceneIdFor(r.BasePlaceId) != placeId || available > day) continue;
            news.Add(new($"contract|{r.ArtistId}|{r.LabelId}|{r.Week}|{r.Phase}", placeId, r.ArtistId,
                eventDay, available, "Local trade report", $"{artistName(r.ArtistId)} signed with {labelName(r.LabelId)}. "));
        }
        foreach (var p in programs ?? Array.Empty<SceneProgram>()) {
            var room = SceneRoomCatalog.Get(p.RoomId);
            if (room?.PlaceId != placeId) continue;
            if (p.StartDay <= day) news.Add(new("program-open|" + p.Id, placeId, null, p.StartDay,
                p.StartDay, room.ContactName, $"{labelName(p.SponsorId)} funded a showcase program at {room.Name}; an extra set is available on performance nights."));
            if (p.ThroughDay + 1 <= day) news.Add(new("program-close|" + p.Id, placeId, null, p.ThroughDay + 1,
                p.ThroughDay + 1, room.ContactName, $"The funded showcase program at {room.Name} has ended."));
        }
        foreach (var m in (moves ?? Array.Empty<SceneFundedMove>()).Where(m => m.Status == SceneMoveStatus.Arrived)) {
            if (m.ArrivalDay + 1 > day) continue;
            if (m.ToPlaceId == placeId) news.Add(new("arrival|" + m.Id, placeId, m.ArtistId, m.ArrivalDay,
                m.ArrivalDay + 1, "Program introduction", $"{artistName(m.ArtistId)} arrived from {ScenePlaceRegistry.Get(m.FromPlaceId)?.Name ?? m.FromPlaceId} for a funded program."));
            if (m.FromPlaceId == placeId) news.Add(new("departure|" + m.Id, placeId, m.ArtistId, m.ArrivalDay,
                m.ArrivalDay + 1, "Program introduction", $"{artistName(m.ArtistId)} completed a move to {ScenePlaceRegistry.Get(m.ToPlaceId)?.Name ?? m.ToPlaceId}."));
        }
        return news.OrderBy(n => n.Text, StringComparer.Ordinal).GroupBy(n => n.Id, StringComparer.Ordinal).Select(g => g.First())
            .Where(n => day - n.AvailableDay <= 104 * 7)
            .OrderByDescending(n => n.AvailableDay).ThenBy(n => n.Id, StringComparer.Ordinal).ToArray();
    }
    public static SceneLead Lead(SceneRoomProfile room, IEnumerable<SceneBill> calendar,
        IEnumerable<string> knownArtists, IEnumerable<string> heardBills, int day, int hour) {
        if (room == null || (heardBills ?? Array.Empty<string>()).Distinct().Take(2).Count() < 2) return null;
        var known = new HashSet<string>(knownArtists ?? Array.Empty<string>(), StringComparer.Ordinal);
        foreach (var bill in calendar.Where(b => b.RoomId == room.Id && b.Status == SceneBillStatus.Scheduled &&
            (b.Day > day || (b.Day == day && b.StartHour > hour))).OrderBy(b => b.Day).ThenBy(b => b.Id, StringComparer.Ordinal)) {
            var appearance = bill.Appearances.FirstOrDefault(a => a.Status == SceneBillStatus.Scheduled && !known.Contains(a.ArtistId));
            if (appearance == null) continue;
            return new SceneLead { Id = room.ContactId + "|" + bill.Id + "|" + appearance.ArtistId,
                ArtistId = appearance.ArtistId, RoomId = room.Id, BillId = bill.Id, ContactId = room.ContactId,
                ReceivedDay = day, PerformanceDay = bill.Day, ExpiresDay = bill.Day,
                Evidence = "The booker recommends an unfamiliar act on this room's announced bill; musical quality and deal availability are unverified." };
        }
        return null;
    }
}
