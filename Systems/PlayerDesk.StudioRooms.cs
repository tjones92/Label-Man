using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>A specialist room's standing rhythm section: the same named regulars, date after date, until one leaves
/// town or stops playing. Formed from the town's session players the first time the room needs it.</summary>
public sealed class StudioHouseCrew {
    public string RoomId, Name;
    public int FormedYear;
    public List<StudioCrewSeat> Seats = new();
    public StudioHouseCrew Copy() => new() { RoomId = RoomId, Name = Name, FormedYear = FormedYear, Seats = Seats.Select(s => s.Copy()).ToList() };
}
public sealed class StudioCrewSeat {
    public MusicianRole Role;
    public string PersonId, Name;
    public int SinceYear;
    public StudioCrewSeat Copy() => (StudioCrewSeat)MemberwiseClone();
}

/// <summary>A charting record another label cut in a room, as the trade knew it.</summary>
public sealed class StudioMarketHit {
    public string RoomId, RecordId, Title, ArtistName, LabelName;
    public GameDate Charted;
    public int Peak;
    public StudioMarketHit Copy() => (StudioMarketHit)MemberwiseClone();
}

/// <summary>
/// Rooms with histories of their own. A specialist room's house band is a standing crew with a name (the M.G.'s at
/// Stax, the FAME rhythm section, Bartholomew's band at J&amp;M); when a regular is out, the owner calls a deputy. And a
/// room's name is made by every label's records cut there, not only the player's: AI labels record in their home town,
/// so their charting records are attributed to that town's rooms (keyed per record, weighted toward the better rooms)
/// and add to a room's renown at a quarter of the weight of the label's own, since the trade knows those through others.
/// Read-only toward the AI economy: AI labels never read any of this.
/// </summary>
public partial class PlayerDesk {
    private readonly List<StudioHouseCrew> studioHouseCrews = new();
    private readonly List<StudioMarketHit> studioMarketHits = new();
    public const float StudioMarketHitWeight = .25f;
    private static readonly MusicianRole[] CrewRoles = { MusicianRole.Drums, MusicianRole.Bass, MusicianRole.Piano, MusicianRole.LeadGuitar };

    public IReadOnlyList<StudioHouseCrew> StudioHouseCrews => studioHouseCrews;
    public IReadOnlyList<StudioMarketHit> StudioMarketHits => studioMarketHits;

    /// <summary>The room's standing crew, formed from the town's session players the first time it's asked for, and
    /// re-seated where a regular has left town, left music or died. Players are chosen by the same reading-and-skill rule
    /// with the room's own key, so the same regulars come up. Seating writes the crew; nothing else changes.</summary>
    internal StudioHouseCrew HouseCrewFor(StudioRoomSnapshot room, int day, int year) {
        var crew = studioHouseCrews.FirstOrDefault(c => c.RoomId == room.Id);
        var town = SessionEmploymentService.ListeningCandidates(room.CityId, day);
        if (crew == null) {
            if (town.Count == 0) return null;
            string owner = OwnerName(room);
            string[] names = { $"the {owner} Rhythm Section", $"the {owner} House Band", $"{owner}'s Regulars" };
            crew = new StudioHouseCrew { RoomId = room.Id, FormedYear = year, Name = names[(int)(StudioTakeEvidence.Unit(room.Id + "|crew-name") * names.Length) % names.Length] };
            studioHouseCrews.Add(crew);
        }
        foreach (var role in CrewRoles) {
            var seat = crew.Seats.FirstOrDefault(s => s.Role == role);
            var held = seat == null ? null : ArtistManager.Instance?.GetMusician(seat.PersonId);
            bool gone = held == null || held.lifeState is MemberLifeState.Deceased or MemberLifeState.Retired ||
                LocalScenePersistenceService.SceneIdFor(held.geography?.basePlaceId) != room.CityId;
            if (seat != null && !gone) continue;
            var pick = town.Where(m => PlaysRole(m, role) && crew.Seats.All(s => s.PersonId != m.personId))
                .OrderByDescending(m => m.technicalSkill + m.sightReading + .5f * (float)StudioTakeEvidence.Unit($"{room.Id}|house|{m.personId}"))
                .ThenBy(m => m.personId, StringComparer.Ordinal).FirstOrDefault();
            if (pick == null) continue;
            if (seat == null) crew.Seats.Add(seat = new StudioCrewSeat { Role = role });
            seat.PersonId = pick.personId; seat.Name = pick.FullName; seat.SinceYear = year;
        }
        return crew;
    }

    /// <summary>Weekly: attributes other labels' charting records to rooms in their home town. Keyed per record, so the
    /// same record always lands in the same room; a better room is likelier, a specialist likelier still.</summary>
    private void UpdateStudioMarketHits(GameDate date) {
        var chart = ChartManager.Instance?.GetCurrentChart();
        if (chart == null) return;
        foreach (var rec in chart) {
            var b = rec?.baseRecord;
            if (b == null || b.isPlayerOwned || rec.peakPosition is <= 0 or > 40) continue;
            var hit = studioMarketHits.FirstOrDefault(h => h.RecordId == b.recordId);
            if (hit != null) { hit.Peak = Math.Min(hit.Peak, rec.peakPosition); continue; }
            var label = ChartManager.Instance.GetLabelById(b.labelId);
            string city = LocalScenePersistenceService.SceneIdFor(label?.geography?.basePlaceId);
            var node = city == null ? null : DistanceModel.GetCityById(city);
            if (node == null) continue;
            var rooms = StudioRoomCatalog.Available(node.cityId, node.parentRegionId).Select(t => (Id: StudioRoomCatalog.LocalId(node.cityId, t), Weight: 1f + (int)t * .5f)).ToList();
            if (StudioRoomCatalog.HasSpecialist(node.cityId, node.parentRegionId)) rooms.Add((StudioRoomCatalog.SpecialistId(node.cityId), 3f));
            double pick = StudioTakeEvidence.Unit("market-room|" + b.recordId) * rooms.Sum(r => r.Weight);
            string roomId = rooms[^1].Id;
            foreach (var r in rooms) { pick -= r.Weight; if (pick < 0) { roomId = r.Id; break; } }
            studioMarketHits.Add(new StudioMarketHit { RoomId = roomId, RecordId = b.recordId, Title = b.title, ArtistName = b.artistName,
                LabelName = label?.labelName ?? "another label", Charted = date, Peak = rec.peakPosition });
        }
    }

    /// <summary>Other labels' hits as a room's renown sees them: a quarter of the label's own weight.</summary>
    private float StudioMarketPull(string roomId, GameDate date) =>
        studioMarketHits.Where(h => h.RoomId == roomId).Sum(h => StudioMarketHitWeight * StudioHitWeight(h.Peak) * StudioHitPull(LocalSceneRoomService.Day(date) - LocalSceneRoomService.Day(h.Charted)));

    private List<StudioHouseCrew> CaptureStudioHouseCrews() => studioHouseCrews.Select(c => c.Copy()).ToList();
    private List<StudioMarketHit> CaptureStudioMarketHits() => studioMarketHits.Select(h => h.Copy()).ToList();
    private static bool ValidateStudioRooms(List<StudioHouseCrew> crews, List<StudioMarketHit> hits, out string message) {
        bool valid = (crews ?? new()).All(c => c != null && StudioRoomCatalog.ById(c.RoomId, out _) != null && !string.IsNullOrEmpty(c.Name) &&
                (c.Seats ?? new()).All(s => s != null && Enum.IsDefined(s.Role) && !string.IsNullOrEmpty(s.PersonId) && !string.IsNullOrEmpty(s.Name)) &&
                (c.Seats ?? new()).GroupBy(s => s.Role).All(g => g.Count() == 1)) &&
            (crews ?? new()).GroupBy(c => c.RoomId).All(g => g.Count() == 1) &&
            (hits ?? new()).All(h => h != null && StudioRoomCatalog.ById(h.RoomId, out _) != null && !string.IsNullOrEmpty(h.RecordId) && h.Peak is >= 1 and <= 100) &&
            (hits ?? new()).GroupBy(h => h.RecordId).All(g => g.Count() == 1);
        message = valid ? "" : "The saved studio room histories are damaged.";
        return valid;
    }
    private void RestoreStudioRooms(List<StudioHouseCrew> crews, List<StudioMarketHit> hits) {
        studioHouseCrews.Clear(); studioHouseCrews.AddRange((crews ?? new()).Select(c => c.Copy()));
        studioMarketHits.Clear(); studioMarketHits.AddRange((hits ?? new()).Select(h => h.Copy()));
    }
}
