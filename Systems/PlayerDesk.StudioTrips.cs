using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>A drive up to a studio town with the band, the day before the date (or to start it that day with what
/// light is left). The band stays in a motel until the date starts; the trip holds the act for the date and lapses
/// at the end of the next day.</summary>
public sealed class StudioTrip {
    public string ArtistId, RoomId, CityId;
    public List<string> PersonIds = new();
    public int ArrivedDay, Nights, DriveHours;
    public float Gas, LodgingPerNight, LodgingPaid;
    public int LastDay => ArrivedDay + 1;
    public StudioTrip Copy() => new() {
        ArtistId = ArtistId, RoomId = RoomId, CityId = CityId, PersonIds = new(PersonIds ?? new()), ArrivedDay = ArrivedDay, Nights = Nights,
        DriveHours = DriveHours, Gas = Gas, LodgingPerNight = LodgingPerNight, LodgingPaid = LodgingPaid
    };
}

/// <summary>Overnight studio trips. Out-of-town dates were a fact of the period: Buddy Holly and the Crickets drove ninety
/// miles from Lubbock to Norman Petty's Clovis studio, recorded late at night beside a machine shop and slept in the guest
/// room by day; singers drove to Muscle Shoals and Memphis to cut with the house bands there. A room farther than a same-day
/// drive is reached by driving up the day before and staying the night; a nearer one can be done the same way to start
/// the date with a full day. The band's motel is the label's, like gas: not studio cost and not recouped.</summary>
public partial class PlayerDesk {
    /// <summary>The longest drive to a studio the label will make in a day, to record the next.</summary>
    public const int MaxOvernightDriveHours = 12;   // a 9 a.m. start reaches the 9 p.m. hard stop
    public const int MaxOvernightRooms = 2;
    private StudioTrip studioTrip;
    public StudioTrip ActiveStudioTrip => studioTrip?.Copy();

    /// <summary>The band's motel rooms for a night: two to a room, at the player's own motel rate.</summary>
    public static float BandLodgingPerNight(int people) => (float)Math.Ceiling(Math.Max(1, people) / 2.0) * HotelNightly;

    /// <summary>The trip this act is on, if the player is in the trip's town and the trip hasn't lapsed.</summary>
    private StudioTrip StudioTripHere(string artistId) =>
        studioTrip != null && studioTrip.ArtistId == artistId && CurrentCityId == studioTrip.CityId &&
        LocalSceneRoomService.Day(StudioToday) <= studioTrip.LastDay ? studioTrip : null;

    /// <summary>Whether this person is held by the trip on this day: the band is away with the label.</summary>
    internal bool StudioTripHolds(string personId, int day) =>
        studioTrip != null && day >= studioTrip.ArrivedDay && day <= studioTrip.LastDay && studioTrip.PersonIds.Contains(personId);

    /// <summary>Drives the act up to the room's town today, so the date can start there (today or tomorrow). The drive is
    /// the ordinary road owner's; the band's motel is billed each night they stay before the date.</summary>
    public bool DriveUpForStudio(SimulatedArtist artist, string roomId, out string message) {
        if (Label == null) { message = "You don't have a label yet."; return false; }
        if (InStudio) { message = "Finish or abandon the active studio date first."; return false; }
        if (studioTrip != null) { message = "You already have a band on the road to a studio."; return false; }
        if (!RequireHome(out message)) return false;
        if (artist == null || artist.labelId != Label.labelId) { message = "That act isn't on your roster."; return false; }
        var offer = StudioRoomIds().Contains(roomId) ? StudioOffer(roomId, artist.artistId) : null;
        if (offer == null || offer.Local) { message = "That room is not one you drive up to."; return false; }
        if (offer.Locked != null) { message = offer.Locked; return false; }
        var people = ActivePeople(artist).Select(m => m.personId).Distinct().OrderBy(id => id, StringComparer.Ordinal).ToList();
        if (people.Count == 0) { message = "This act has no available performers."; return false; }
        var clock = TimeManager.Instance;
        int now = clock.CurrentHour * 60 + clock.CurrentMinute, today = LocalSceneRoomService.Day(clock.CurrentDate);
        // The band is on the road today and in the studio town tomorrow.
        if (!LocalSceneRoomService.StudioParticipantsFree(Label.labelId, people, clock.CurrentDate, now, 24 * 60) ||
            !LocalSceneRoomService.StudioParticipantsFree(Label.labelId, people, clock.CurrentDate.NextDay(), 0, 24 * 60)) {
            message = "A performer has a show or appointment today or tomorrow. Choose another day for the trip."; return false;
        }
        float lodging = BandLodgingPerNight(people.Count);
        if (Label.cashReserves < offer.Gas + lodging) { message = $"The gas and a night's motel for the band cost ${offer.Gas + lodging:N0}; you are ${offer.Gas + lodging - Label.cashReserves:N0} short."; return false; }
        if (!Travel(offer.Room.CityId, out message)) return false;
        studioTrip = new StudioTrip {
            ArtistId = artist.artistId, RoomId = roomId, CityId = offer.Room.CityId, PersonIds = people, ArrivedDay = today,
            DriveHours = offer.DriveHours, Gas = offer.Gas, LodgingPerNight = lodging
        };
        string city = DistanceModel.GetCityById(offer.Room.CityId)?.name ?? "town";
        Note($"Drove {artist.stageName} up to {city} for a date at {offer.Room.Name} ({offer.DriveHours}h).");
        message = $"You're in {city} with {artist.stageName}. Book {offer.Room.Name} today if there's light left, or tomorrow after a night at the motel (${lodging:N0} for the band).";
        Changed?.Invoke(); return true;
    }

    /// <summary>Called by the road owner before any drive: leaving the trip's town before the date sends the band home.</summary>
    private void LeaveStudioTripFor(string destinationCityId) {
        if (studioTrip == null || destinationCityId == studioTrip.CityId || InStudio) return;
        Note($"Drove off before the date; {ArtistManager.Instance?.GetArtist(studioTrip.ArtistId)?.stageName ?? "the band"} went home on their own.");
        studioTrip = null;
    }

    /// <summary>Morning: bill the band's night at the motel, or let a lapsed trip go.</summary>
    private void ProcessStudioTripDawn(GameDate date) {
        if (studioTrip == null) return;
        int today = LocalSceneRoomService.Day(date);
        if (today > studioTrip.LastDay || CurrentCityId != studioTrip.CityId) {
            Note($"The studio trip lapsed; {ArtistManager.Instance?.GetArtist(studioTrip.ArtistId)?.stageName ?? "the band"} went home.");
            studioTrip = null; return;
        }
        Label.cashReserves -= studioTrip.LodgingPerNight; Label.monthlyExpenses += studioTrip.LodgingPerNight;
        studioTrip.Nights++; studioTrip.LodgingPaid += studioTrip.LodgingPerNight;
        Note($"Band motel in {CurrentCity?.name ?? "town"} -- ${studioTrip.LodgingPerNight:N0}.");
    }

    /// <summary>The date has started: the trip has done its job.</summary>
    private void CompleteStudioTrip(PendingSession session) {
        if (studioTrip == null || studioTrip.ArtistId != session.ArtistId) return;
        session.ActionLog.Insert(0, $"Drove {studioTrip.DriveHours}h to {DistanceModel.GetCityById(studioTrip.CityId)?.name} with the band (${studioTrip.Gas:N0} gas)" +
            (studioTrip.Nights > 0 ? $" and stayed {studioTrip.Nights} night(s) (band motel ${studioTrip.LodgingPaid:N0})." : "; the date started the same day."));
        studioTrip = null;
    }

    private static bool ValidateStudioTrip(StudioTrip trip, out string message) {
        bool valid = trip == null || !string.IsNullOrEmpty(trip.ArtistId) && StudioRoomCatalog.ById(trip.RoomId, out _) != null && !string.IsNullOrEmpty(trip.CityId) &&
            trip.PersonIds is { Count: > 0 } && trip.PersonIds.All(id => !string.IsNullOrEmpty(id)) && trip.ArrivedDay >= 0 && trip.Nights is >= 0 and <= 2 &&
            trip.DriveHours is > 0 and <= MaxOvernightDriveHours && new[] { trip.Gas, trip.LodgingPerNight, trip.LodgingPaid }.All(v => float.IsFinite(v) && v >= 0);
        message = valid ? "" : "The saved studio trip is damaged.";
        return valid;
    }
}
