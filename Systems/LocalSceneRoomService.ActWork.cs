using System;
using System.Collections.Generic;
using System.Linq;

public static partial class LocalSceneRoomService {
    /// <summary>What an act's own working life looks like from the room ledger: the rooms it is booked into from
    /// <paramref name="day"/> on, whether it holds a standing engagement, and how many shows it has played in all
    /// (pre-rolled history included).
    /// A pure read for the contract outlook -- it never ensures, schedules or advances the calendar.</summary>
    internal static (IReadOnlyList<string> BookedRoomIds, string EngagementRoomId, int LifetimeShows) ObserveActWork(string artistId, int day) {
        if (state == null || string.IsNullOrEmpty(artistId)) return (Array.Empty<string>(), null, 0);
        var booked = state.Bills.Where(b => b.Day >= day && b.Status == SceneBillStatus.Scheduled &&
                b.Appearances.Any(a => a.ArtistId == artistId && a.Status == SceneBillStatus.Scheduled))
            .OrderBy(b => b.Day).ThenBy(b => b.StartHour).ThenBy(b => b.Id, StringComparer.Ordinal)
            .Select(b => b.RoomId).ToList();
        string engaged = state.Engagements.Where(e => e.ArtistId == artistId && e.StartDay <= day && e.ThroughDay >= day)
            .OrderBy(e => e.RoomId, StringComparer.Ordinal).Select(e => e.RoomId).FirstOrDefault();
        int shows = standing.Values.Where(s => s.ArtistId == artistId).Sum(s => s.Shows + s.PriorShows);
        return (booked.AsReadOnly(), engaged, shows);
    }
}
