using System;
using Godot;

/// <summary>Fictional local recording services, with stable identities. No historical facility claims.</summary>
public sealed class StudioRoomSnapshot {
    public string Id;
    public string CityId;
    public string Name;
    public string Character;
    public string Layout = "Whole ensemble on mono tape";
    public float HourlyRate;
    public float Craft;
    public int CapabilityVersion = 1;
    public bool SupportsToneFilter, SupportsCompression;
    // v3: the engineer on duty that day, the room's acoustics, and remembered work with this act.
    public string EngineerId, EngineerName;
    public bool EngineerHouse;
    public float EngineerEar = .65f, EngineerHands = .5f;
    public StudioEngineerHabit EngineerHabit;
    public float Intimacy = .5f, Liveliness = .5f, Control = .5f;
    public int SetupMinutes = PlayerDesk.StudioSetupMinutes, FamiliarDates;
    // v4: a chief and a second engineer (EngineerHouse is the chief), asking for one by name, and specialist rooms
    // with a feature of their own, reached by an introduction.
    public bool Specialist, SupportsChamber, EngineerRequested;
    public string Specialty;
    public float EngineerPremium;   // per hour, charged only when the label asked for the chief by name
    // v20: renown the room has earned from the label's records cut there, and the rate before it. A room's renown is
    // reputation and demand (busier staff, a higher rate, a full calendar), never a quality stat. Zero on older saves.
    public float Renown, BaseRate;
    public bool ActCutAHit;         // this act cut a charting record here: the owner keeps time and the chief for them
    // v21: the machine (tracks; 0 on older snapshots, read as mono) and whether the owner takes evening work.
    public int Tracks;
    public bool EveningWork;
    // v21: an engineer brought in from another room in town, by that room's id. Null when the room's own staff runs the board.
    public string EngineerHomeRoomId;
    public StudioRoomSnapshot Copy() => (StudioRoomSnapshot)MemberwiseClone();
}

public static class StudioRoomCatalog {
    /// <summary>A specialist room needs a strong recording town. Provisional authored bar, not a facility census.</summary>
    public const float SpecialistQuality = .85f;
    public const float SpecialistRateFactor = 1.4f;

    public static PlayerDesk.StudioTier[] Available(string cityId, string regionId) {
        float quality = PlayerDesk.StudioQualityIn(cityId, regionId);
        return quality >= .78f ? new[] { PlayerDesk.StudioTier.Budget, PlayerDesk.StudioTier.Mid, PlayerDesk.StudioTier.Top }
            : quality >= .5f ? new[] { PlayerDesk.StudioTier.Budget, PlayerDesk.StudioTier.Mid } : new[] { PlayerDesk.StudioTier.Budget };
    }
    public static bool HasSpecialist(string cityId, string regionId) => PlayerDesk.StudioQualityIn(cityId, regionId) >= SpecialistQuality;

    public static string LocalId(string cityId, PlayerDesk.StudioTier tier) => $"studio:{cityId}:{(int)tier}";
    public static string SpecialistId(string cityId) => $"studio:{cityId}:specialist";

    public static StudioRoomSnapshot Local(string cityId, string regionId, PlayerDesk.StudioTier tier) {
        string city = DistanceModel.GetCityById(cityId)?.name ?? "Local";
        return new() {
            Id = LocalId(cityId, tier), CityId = cityId,
            Name = city + (tier == PlayerDesk.StudioTier.Budget ? " Recording Service" : tier == PlayerDesk.StudioTier.Mid ? " Sound Workshop" : " Studio Theatre"),
            Character = tier == PlayerDesk.StudioTier.Budget ? "A straightforward room: close together, with a little room spill."
                : tier == PlayerDesk.StudioTier.Mid ? "A controlled room with a dependable house balance." : "A spacious room with a lively ensemble sound.",
            HourlyRate = PlayerDesk.StudioHourlyRateIn(cityId, regionId, tier),
            Craft = PlayerDesk.StudioQualityIn(cityId, regionId),
            CapabilityVersion = 2, SupportsToneFilter = true, SupportsCompression = tier != PlayerDesk.StudioTier.Budget
        };
    }

    // Specialist rooms: one feature each, picked by a stable key per town. Provisional authored design roles.
    private static readonly (string Tag, string Feature, float Intimacy, float Liveliness, float Control, bool Chamber)[] Specialties = {
        ("Echo chamber", "a small, dry room with its own echo chamber built under the floor", .65f, .45f, .65f, true),
        ("Theatre floor", "an old movie house with a sloping floor that throws a big, warm band sound", .25f, .90f, .45f, false),
        ("Dead room", "a padded little room where every word lands and nothing rings", .85f, .20f, .80f, false),
        ("Church hall", "a converted church hall, tall and ringing and kind to voices in harmony", .40f, .85f, .40f, true)
    };

    /// <summary>The town's specialist room, or null when the town has none. The owner books regulars and people who
    /// come recommended; access is decided by the desk, not here.</summary>
    public static StudioRoomSnapshot Specialist(string cityId, string regionId) {
        if (!HasSpecialist(cityId, regionId)) return null;
        string id = SpecialistId(cityId);
        var kind = Specialties[(int)(StudioTakeEvidence.Unit(id + "|kind") * Specialties.Length) % Specialties.Length];
        string owner = RolodexDirectory.PersonName("studio-owner:" + id).Split(' ')[^1];
        string name = StudioTakeEvidence.Unit(id + "|name") < .5 ? owner + " Recording" : owner + " Sound Studio";
        return new() {
            Id = id, CityId = cityId, Name = name, Specialist = true, Specialty = kind.Tag,
            Character = char.ToUpperInvariant(kind.Feature[0]) + kind.Feature[1..] + ".",
            HourlyRate = Mathf.Round(PlayerDesk.StudioHourlyRateIn(cityId, regionId, PlayerDesk.StudioTier.Top) * SpecialistRateFactor),
            Craft = PlayerDesk.StudioQualityIn(cityId, regionId),
            CapabilityVersion = 2, SupportsToneFilter = true, SupportsCompression = true, SupportsChamber = kind.Chamber,
            Intimacy = kind.Intimacy, Liveliness = kind.Liveliness, Control = kind.Control
        };
    }

    /// <summary>Resolves a room id to its snapshot (before staffing), or null for an unknown id.</summary>
    public static StudioRoomSnapshot ById(string roomId, out PlayerDesk.StudioTier tier) {
        tier = PlayerDesk.StudioTier.Budget;
        var parts = roomId?.Split(':');
        if (parts is not { Length: 3 } || parts[0] != "studio") return null;
        var city = DistanceModel.GetCityById(parts[1]);
        if (city == null) return null;
        if (parts[2] == "specialist") { tier = PlayerDesk.StudioTier.Top; return Specialist(city.cityId, city.parentRegionId); }
        if (!int.TryParse(parts[2], out int t) || !Enum.IsDefined((PlayerDesk.StudioTier)t)) return null;
        tier = (PlayerDesk.StudioTier)t;
        return Array.IndexOf(Available(city.cityId, city.parentRegionId), tier) < 0 ? null : Local(city.cityId, city.parentRegionId, tier);
    }

    /// <summary>Upgrades a snapshot to v4: acoustics and the engineer on duty on this date, or the one asked for.</summary>
    public static void Staff(StudioRoomSnapshot room, PlayerDesk.StudioTier tier, GameDate date, string requestedEngineerId = null) {
        if (!room.Specialist) (room.Intimacy, room.Liveliness, room.Control) = StudioPeople.Acoustics(tier);
        StudioPeople.StaffFor(room, tier, date, requestedEngineerId);
        room.Tracks = StudioEquipment.TracksFor(room, tier, date.year);
        if (room.Tracks > 1) room.Layout = StudioEquipment.LayoutText(room.Tracks);
        room.EveningWork = StudioTakeEvidence.Unit(room.Id + "|evenings") < .5;
        room.CapabilityVersion = 4;
    }
}
