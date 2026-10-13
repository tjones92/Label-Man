using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>A room the label could book today: the staffed snapshot, the drive to reach it, and whether the door is
/// open. Built fresh for each booking card; nothing here is saved or draws gameplay randomness.</summary>
public sealed class StudioRoomOffer {
    public StudioRoomSnapshot Room;
    public PlayerDesk.StudioTier Tier;
    public int DriveHours;
    public float Gas;
    public bool Local => DriveHours == 0;
    /// <summary>Too far to drive and record the same day: the band drives up and stays the night.</summary>
    public bool Overnight => DriveHours > PlayerDesk.MaxStudioDriveHours;
    /// <summary>The band's motel for a night, should the label drive up the day before.</summary>
    public float BandLodging;
    /// <summary>The date this card's staffing is for: tomorrow for an overnight room, otherwise today.</summary>
    public GameDate StaffedFor;
    /// <summary>A specialist owner's terms for this act. Null at an ordinary room.</summary>
    public StudioHousePlan House;
    /// <summary>Who opened a specialist room's door, in a sentence. Null for an ordinary room.</summary>
    public string Introduction;
    /// <summary>Why this room cannot be booked. Null when it can.</summary>
    public string Locked;
    public List<StudioPeople.Engineer> Engineers = new();
}

/// <summary>Studio slice 2: rooms in other towns, specialist rooms reached through contacts, and asking for an
/// engineer by name. Player-only, inside the incremental studio flow.</summary>
public partial class PlayerDesk {
    /// <summary>The longest drive a studio date will be planned around: it must leave a full date before the hard stop.</summary>
    public const int MaxStudioDriveHours = 6;
    public const int MaxOutOfTownRooms = 3;

    private GameDate StudioToday => TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;

    /// <summary>The rooms to show on the booking sheet: every local room, the town's specialist room, then up to three
    /// rooms in reachable towns whose studios beat home's. A sparse town still gets its modest local service.</summary>
    public List<string> StudioRoomIds() {
        var ids = new List<string>();
        if (Label == null) return ids;
        // Up with the band on a studio trip: the rooms in the town you're in.
        if (StudioExperience.Enabled && studioTrip != null && CurrentCityId == studioTrip.CityId) {
            var here = DistanceModel.GetCityById(studioTrip.CityId);
            ids.AddRange(StudioRoomCatalog.Available(here.cityId, here.parentRegionId).Select(t => StudioRoomCatalog.LocalId(here.cityId, t)));
            if (StudioRoomCatalog.HasSpecialist(here.cityId, here.parentRegionId)) ids.Add(StudioRoomCatalog.SpecialistId(here.cityId));
            if (!ids.Contains(studioTrip.RoomId)) ids.Add(studioTrip.RoomId);
            return ids;
        }
        ids.AddRange(StudioRoomCatalog.Available(Label.homeCityId, Label.homeRegion).Select(t => StudioRoomCatalog.LocalId(Label.homeCityId, t)));
        if (!StudioExperience.Enabled) return ids;
        if (StudioRoomCatalog.HasSpecialist(Label.homeCityId, Label.homeRegion)) ids.Add(StudioRoomCatalog.SpecialistId(Label.homeCityId));
        var home = DistanceModel.GetCityById(Label.homeCityId);
        float homeQuality = StudioQualityIn(Label.homeCityId, Label.homeRegion);
        var reachable = DistanceModel.GetCities()
            .Where(c => c.cityId != Label.homeCityId && CanReach(home, c) && DriveQuote(Label.homeCityId, c.cityId).Hours <= MaxOvernightDriveHours)
            .Select(c => (City: c, Quality: StudioQualityIn(c.cityId, c.parentRegionId), Hours: DriveQuote(Label.homeCityId, c.cityId).Hours))
            .Where(x => x.Quality > homeQuality + .05f || StudioRoomCatalog.HasSpecialist(x.City.cityId, x.City.parentRegionId))
            .OrderByDescending(x => x.Quality).ThenBy(x => x.Hours).ThenBy(x => x.City.cityId, StringComparer.Ordinal).ToList();
        // A day's drive first, then up to two rooms worth driving up for the night before.
        var away = reachable.Where(x => x.Hours <= MaxStudioDriveHours).Take(MaxOutOfTownRooms)
            .Concat(reachable.Where(x => x.Hours > MaxStudioDriveHours).Take(MaxOvernightRooms));
        foreach (var x in away)
            ids.Add(StudioRoomCatalog.HasSpecialist(x.City.cityId, x.City.parentRegionId) ? StudioRoomCatalog.SpecialistId(x.City.cityId)
                : StudioRoomCatalog.LocalId(x.City.cityId, StudioRoomCatalog.Available(x.City.cityId, x.City.parentRegionId)[^1]));
        return ids;
    }

    /// <summary>The bookable room snapshot for a date (today unless given), with its renown and rate, the engineer on
    /// duty (or the one asked for) and any remembered work with this act. Null for an unknown room.</summary>
    public StudioRoomSnapshot StudioRoom(string roomId, string artistId, string engineerId = null, GameDate? on = null) {
        var room = StudioRoomCatalog.ById(roomId, out var tier);
        if (room == null || !StudioExperience.Enabled) return room;
        var date = on ?? StudioToday;
        ApplyStudioRenown(room, artistId, date);
        StudioRoomCatalog.Staff(room, tier, date, engineerId);
        var last = StudioPeople.LastWith(studioVisits, room.EngineerId, artistId);
        room.FamiliarDates = studioVisits.Count(v => v.EngineerId == room.EngineerId && v.ArtistId == artistId);
        room.SetupMinutes = StudioPeople.SetupMinutes(room, last != null);
        return room;
    }

    public StudioRoomSnapshot StudioRoomFor(StudioTier tier, string artistId) =>
        StudioRoom(StudioRoomCatalog.LocalId(Label.homeCityId, tier), artistId);

    public StudioRoomOffer StudioOffer(string roomId, string artistId, string engineerId = null) {
        var shape = StudioRoomCatalog.ById(roomId, out var tier);
        if (shape == null) return null;
        // Drives are quoted from home; a room in the town you're already in for a studio trip is no drive at all.
        var offer = new StudioRoomOffer { Tier = tier };
        if (shape.CityId != Label.homeCityId && shape.CityId != CurrentCityId) (offer.DriveHours, offer.Gas) = DriveQuote(Label.homeCityId, shape.CityId);
        offer.StaffedFor = offer.Overnight ? StudioToday.NextDay() : StudioToday;
        var room = offer.Room = StudioRoom(roomId, artistId, engineerId, offer.StaffedFor);
        var artist = ArtistManager.Instance?.GetArtist(artistId);
        if (StudioExperience.Enabled) {
            offer.Engineers = StudioPeople.Engineers(room, tier, offer.StaffedFor);
            if (artist != null) offer.BandLodging = BandLodgingPerNight(ActivePeople(artist).Count);
        }
        if (room.Specialist) {
            offer.Introduction = StudioIntroduction(room);
            if (offer.Introduction == null) {
                string city = DistanceModel.GetCityById(room.CityId)?.name ?? "town";
                offer.Locked = $"{room.Name} books its regulars and people who come recommended. A disc jockey in {city} who knows you, " +
                    $"or a session player there who knows one of your acts, could introduce you.";
            }
        }
        if (StudioExperience.Enabled) {
            offer.Locked ??= StudioBookedSolid(room, offer.StaffedFor);
            if (room.Specialist && artist != null) {
                // The house band is booked for the working day the date would fall in.
                var clock = TimeManager.Instance;
                int start = offer.Overnight ? (clock?.WorkdayStartHour ?? 9) * 60 : (clock?.CurrentHour ?? 9) * 60 + (clock?.CurrentMinute ?? 0) + offer.DriveHours * 60;
                offer.House = StudioHouseBand(room, artist, offer.StaffedFor, start, (clock?.HardStopHour ?? 21) * 60);
                offer.Locked ??= offer.House?.Locked;
            }
        }
        return offer;
    }

    /// <summary>Hourly room rate plus the chief's premium when asked for by name, for the booked hours.</summary>
    public static float StudioRoomCost(StudioRoomSnapshot room, int hours) =>
        (room.HourlyRate + room.EngineerPremium) * Mathf.Clamp(hours, MinSessionHours, MaxSessionHours);

    /// <summary>Who could get the label through a specialist room's door, in a sentence; null when nobody can. Read-only:
    /// it books nobody, spends nothing and draws no randomness.</summary>
    public string StudioIntroduction(StudioRoomSnapshot room) {
        if (room == null) return null;
        if (studioVisits.Any(v => v.RoomId == room.Id)) return "You have cut a date here before; the owner knows the label.";
        var city = DistanceModel.GetCityById(room.CityId);
        foreach (var entry in rolodex.Where(e => e.state >= DiscoveryState.Known).OrderBy(e => e.djId, StringComparer.Ordinal)) {
            var station = ChartManager.Instance?.GetRadioStation(entry.stationId);
            if (station != null && city != null && station.cityName == city.name)
                return $"{entry.displayName} at {station.callsign} put in a word with the owner.";
        }
        var members = Roster.Where(a => a.lifecycleStatus == ArtistLifecycleStatus.Active)
            .SelectMany(a => a.members.Where(m => m.isActive && !string.IsNullOrEmpty(m.personId)).Select(m => (Act: a, Member: m))).ToList();
        foreach (var player in SessionEmploymentService.ListeningCandidates(room.CityId, LocalSceneRoomService.Day(StudioToday))) {
            foreach (var (act, member) in members) {
                if (member.personId == player.personId) continue;
                var edge = ContactNetworkService.Edge(player.personId, member.personId);
                if (edge == null || edge.Fallout) continue;
                return $"{player.FullName} works sessions there and vouched for the label: {ContactNetworkService.HowTheyKnow(player.personId, member.personId)} " +
                    $"with {member.firstName} of {act.stageName}.";
            }
        }
        return null;
    }

    /// <summary>Studio work needs the player at the booked room: home for a local date, the room's town for one away.</summary>
    private bool RequireAtStudio(PendingSession session, out string message) {
        string city = session.Room?.CityId ?? Label?.homeCityId;
        if (CurrentCityId == city) { message = string.Empty; return true; }
        message = $"You're not at {session.Room?.Name ?? "the studio"}. Drive back to {DistanceModel.GetCityById(city)?.name ?? "town"} to keep working.";
        return false;
    }

    /// <summary>Where the player stands after a date in another town: there, with the drive home still to make.</summary>
    private string AwayFromHomeNote() => AtHome ? "" : $" You're in {CurrentCity?.name}; drive home when you're ready.";
}
