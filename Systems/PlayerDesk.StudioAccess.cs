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
        ids.AddRange(StudioRoomCatalog.Available(Label.homeCityId, Label.homeRegion).Select(t => StudioRoomCatalog.LocalId(Label.homeCityId, t)));
        if (!StudioExperience.Enabled) return ids;
        if (StudioRoomCatalog.HasSpecialist(Label.homeCityId, Label.homeRegion)) ids.Add(StudioRoomCatalog.SpecialistId(Label.homeCityId));
        var home = DistanceModel.GetCityById(Label.homeCityId);
        float homeQuality = StudioQualityIn(Label.homeCityId, Label.homeRegion);
        var away = DistanceModel.GetCities()
            .Where(c => c.cityId != Label.homeCityId && CanReach(home, c) && DriveQuote(Label.homeCityId, c.cityId).Hours <= MaxStudioDriveHours)
            .Select(c => (City: c, Quality: StudioQualityIn(c.cityId, c.parentRegionId), Hours: DriveQuote(Label.homeCityId, c.cityId).Hours))
            .Where(x => x.Quality > homeQuality + .05f || StudioRoomCatalog.HasSpecialist(x.City.cityId, x.City.parentRegionId))
            .OrderByDescending(x => x.Quality).ThenBy(x => x.Hours).ThenBy(x => x.City.cityId, StringComparer.Ordinal)
            .Take(MaxOutOfTownRooms);
        foreach (var x in away)
            ids.Add(StudioRoomCatalog.HasSpecialist(x.City.cityId, x.City.parentRegionId) ? StudioRoomCatalog.SpecialistId(x.City.cityId)
                : StudioRoomCatalog.LocalId(x.City.cityId, StudioRoomCatalog.Available(x.City.cityId, x.City.parentRegionId)[^1]));
        return ids;
    }

    /// <summary>The bookable room snapshot for today, with the engineer on duty (or the one asked for) and any
    /// remembered work with this act. Null for an unknown room.</summary>
    public StudioRoomSnapshot StudioRoom(string roomId, string artistId, string engineerId = null) {
        var room = StudioRoomCatalog.ById(roomId, out var tier);
        if (room == null || !StudioExperience.Enabled) return room;
        StudioRoomCatalog.Staff(room, tier, StudioToday, engineerId);
        var last = StudioPeople.LastWith(studioVisits, room.EngineerId, artistId);
        room.FamiliarDates = studioVisits.Count(v => v.EngineerId == room.EngineerId && v.ArtistId == artistId);
        room.SetupMinutes = StudioPeople.SetupMinutes(room, last != null);
        return room;
    }

    public StudioRoomSnapshot StudioRoomFor(StudioTier tier, string artistId) =>
        StudioRoom(StudioRoomCatalog.LocalId(Label.homeCityId, tier), artistId);

    public StudioRoomOffer StudioOffer(string roomId, string artistId, string engineerId = null) {
        var room = StudioRoom(roomId, artistId, engineerId);
        if (room == null) return null;
        StudioRoomCatalog.ById(roomId, out var tier);
        var offer = new StudioRoomOffer { Room = room, Tier = tier };
        if (room.CityId != Label.homeCityId) (offer.DriveHours, offer.Gas) = DriveQuote(Label.homeCityId, room.CityId);
        if (StudioExperience.Enabled) offer.Engineers = StudioPeople.Engineers(room, tier, StudioToday);
        if (room.Specialist) {
            offer.Introduction = StudioIntroduction(room);
            if (offer.Introduction == null) {
                string city = DistanceModel.GetCityById(room.CityId)?.name ?? "town";
                offer.Locked = $"{room.Name} books its regulars and people who come recommended. A disc jockey in {city} who knows you, " +
                    $"or a session player there who knows one of your acts, could introduce you.";
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
