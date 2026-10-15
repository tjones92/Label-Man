using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Early-game directive V2 §6.3: nights a room's booker gives an act the player pitched. A booking is only a claim on a
/// future night the calendar has not built yet; when that week is scheduled the act goes onto the bill after the room's
/// own closing regular, under every ordinary rule (presence, the room's music, people free, double rooms). It displaces
/// nobody already announced. Only a completed set is a performance; a booking that could not be billed, or whose set did
/// not go ahead, is cancelled with the reason. AI-only worlds never hold a booking, so none of this runs for them.
/// </summary>
public static partial class LocalSceneRoomService {
    /// <summary>How far ahead a booker will pencil a night: six weeks past the announced calendar.</summary>
    public const int BookingHorizonDays = 42;

    /// <summary>The first night a booker can still give: the day after the announced calendar ends.</summary>
    public static int FirstBookableDay => (State.LastScheduledWeek + 1) * 7;

    public static IReadOnlyList<SceneBooking> Bookings => Copy(State.Bookings ?? new()).AsReadOnly();
    public static SceneBookerPitch LastPitch(string roomId, string artistId) =>
        Copy((State.BookerPitches ?? new()).LastOrDefault(p => p.RoomId == roomId && p.ArtistId == artistId));

    /// <summary>Called by the player desk once a booker has said yes. Validation is the desk's; this only records.</summary>
    internal static void AddBooking(SceneBooking booking) => (State.Bookings ??= new()).Add(booking);
    internal static void RecordPitch(SceneBookerPitch pitch) {
        State.BookerPitches ??= new();
        State.BookerPitches.RemoveAll(p => p.RoomId == pitch.RoomId && p.ArtistId == pitch.ArtistId);
        State.BookerPitches.Add(pitch);
    }

    private static bool HasBookings => State.Bookings is { Count: > 0 };

    /// <summary>Tonight's order with booked acts placed right after the closing regular (or first, when the room has none
    /// tonight). A booked act must still be one the room could book tonight (it is among the eligible members).</summary>
    private static IEnumerable<SimulatedArtist> WithBookedActs(SceneRoomProfile room, int day, IEnumerable<SimulatedArtist> candidates, HashSet<string> regularIds) {
        var booked = State.Bookings.Where(b => b.RoomId == room.Id && b.Day == day && b.Status == SceneBillStatus.Scheduled && !b.Billed)
            .Select(b => b.ArtistId).ToHashSet(StringComparer.Ordinal);
        if (booked.Count == 0) return candidates;
        var list = candidates.ToList();
        var bookedActs = list.Where(a => booked.Contains(a.artistId)).ToList();
        var rest = list.Where(a => !booked.Contains(a.artistId)).ToList();
        int lead = rest.Count > 0 && regularIds.Contains(rest[0].artistId) ? 1 : 0;
        return rest.Take(lead).Concat(bookedActs).Concat(rest.Skip(lead)).ToList();
    }

    /// <summary>After a night is billed: a booking whose act made the bill is billed; one that did not is cancelled now,
    /// with the reason the scheduler can name.</summary>
    private static void PlaceBookings(SceneBill bill, SceneRoomProfile room) {
        foreach (var booking in State.Bookings.Where(b => b.RoomId == bill.RoomId && b.Day == bill.Day && b.Status == SceneBillStatus.Scheduled && !b.Billed)) {
            if (bill.Appearances.Any(a => a.ArtistId == booking.ArtistId)) { booking.Billed = true; booking.Note = "On the bill."; continue; }
            var act = ArtistManager.Instance?.GetArtist(booking.ArtistId);
            booking.Status = SceneBillStatus.Cancelled;
            booking.Note = act == null ? "The act is gone."
                : !Eligible(act, room, bill.Day) ? "The act wasn't working that scene or that kind of room by then."
                : "It couldn't be fitted on the bill that night: another booking, a shared player, or a night the act couldn't work.";
        }
    }

    /// <summary>Early-game F3: the people who came in for an act's sets in a town and stayed, summed over the town's rooms
    /// from each room's own draw record (<see cref="SceneRoomStanding.DrawTotal"/> x the average share that stayed). Each
    /// room's tally fades with <paramref name="halfLifeDays"/> from the act's last night there. Repeat listeners are in the
    /// sum more than once; the caller saturates it. A pure read: nothing here is written, so AI worlds are untouched.</summary>
    public static float LocalDraw(string artistId, string placeId, int day, float halfLifeDays) {
        if (state == null || string.IsNullOrEmpty(artistId) || halfLifeDays <= 0f) return 0f;
        float heads = 0f;
        foreach (var room in SceneRoomCatalog.ForPlace(placeId)) {
            if (!room.IsPerformance || !standing.TryGetValue(StandingKey(room.Id, artistId), out var s) || s.DrawSets <= 0) continue;
            float stayed = s.StayedTotal / s.DrawSets;
            heads += s.DrawTotal * stayed * (float)Math.Pow(0.5, Math.Max(0, day - s.LastDay) / halfLifeDays);
        }
        return heads;
    }

    /// <summary>When a billed set resolves, the booking takes its outcome.</summary>
    private static void SettleBooking(SceneBill bill, SceneAppearance slot) {
        foreach (var booking in State.Bookings.Where(b => b.RoomId == bill.RoomId && b.Day == bill.Day && b.ArtistId == slot.ArtistId && b.Billed && b.Status == SceneBillStatus.Scheduled)) {
            booking.Status = slot.Status;
            booking.Note = slot.Status == SceneBillStatus.Performed ? "Played." : slot.CancellationReason ?? "The set did not go ahead.";
        }
    }
}
