using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Funded fictional supporting programs and voluntary domestic moves. These are
/// bounded player/sponsor actions, not full-city employment estimates or another birth clock.</summary>
public static class SceneEcosystemService {
    public const float ProgramCost = 600;
    public const int ProgramDays = 91;
    private static SceneEcosystemSaveData state, previous;
    private static bool restoring;
    private static SceneEcosystemSaveData State => state ??= new();
    public static IReadOnlyList<SceneProgram> Programs => Array.AsReadOnly(LocalSceneRoomService.Copy(state?.Programs.ToArray()) ?? Array.Empty<SceneProgram>());
    public static IReadOnlyList<SceneFundedMove> Moves => Array.AsReadOnly(LocalSceneRoomService.Copy(state?.Moves.ToArray()) ?? Array.Empty<SceneFundedMove>());
    internal static bool HasPlannedMove(string artistId) => state?.Moves.Any(m => m.ArtistId == artistId && m.Status == SceneMoveStatus.Planned) == true;
    private static bool Canonical(AILabel label) => label != null && label.IsActive &&
        (ReferenceEquals(PlayerDesk.Instance?.Label, label) || ChartManager.Instance.GetAllLabels().Any(l => ReferenceEquals(l, label)));
    private static void Pay(AILabel label, float cost) => CompetitorManager.Instance.RecordExpense(label, cost);
    public static int Slots(SceneRoomProfile room, int day) => 2 +
        (LocalScenes.Institutions && room.IsPerformance && state?.Programs.Any(p => p.RoomId == room.Id &&
            p.StartDay <= day && p.ThroughDay >= day) == true ? 1 : 0);
    public static bool FundProgram(AILabel sponsor, string roomId, out string message) {
        message = "Scene institutions are disabled.";
        if (!LocalScenes.Institutions || restoring) return false;
        var room = SceneRoomCatalog.Get(roomId);
        int day = LocalSceneRoomService.Day(TimeManager.Instance.CurrentDate);
        if (!Canonical(sponsor) || room?.IsPerformance != true || day < 0 || day + 14 + ProgramDays > LocalSceneRoomService.Day(GameDate.EndDate)) {
            message = "A current sponsor and a performance room within the playable period are required."; return false;
        }
        if (state?.Programs.Any(p => p.RoomId == roomId && p.ThroughDay >= day) == true) {
            message = "This room already has a funded program."; return false;
        }
        if (!sponsor.CanAffordToSign(ProgramCost)) { message = "The program would consume the label's operating reserve."; return false; }
        var program = new SceneProgram { Id = $"program|{roomId}|{day}", RoomId = roomId,
            SponsorId = sponsor.labelId, StartDay = day + 14, ThroughDay = day + 14 + ProgramDays - 1,
            Cost = ProgramCost, Provenance = "Fictional funded rehearsal/showcase program; one extra set per existing performance night; gameplay cost and duration" };
        Pay(sponsor, ProgramCost); State.Programs.Add(program);
        message = $"Funded {room.Name}: one extra set on each performance night for 13 weeks, starting {LocalSceneRoomService.Date(program.StartDay).ToHeadlineString()} (${ProgramCost:N0}).";
        return true;
    }
    private static Musician[] People(SimulatedArtist a) => (a?.members ?? new()).Where(m => m.isActive && m.lifeState == MemberLifeState.Active)
        .OrderBy(m => m.personId, StringComparer.Ordinal).ToArray();
    public static bool Consents(Musician person, string from, string to, int window) {
        double chance = Math.Clamp(.55 + .30 * person.ambition - (person.hasChildren ? .20 : 0) - .20 * person.fatigue, .1, .95);
        return LocalSceneIdentityService.AssignmentUnit(LocalSceneIdentityService.KeyedSeed,
            $"relocation-consent-v1|{person.personId}|{from}|{to}|{window}") < chance;
    }
    public static bool ProposeMove(AILabel sponsor, string artistId, string programId, out string message) {
        message = "Scene relocation is disabled.";
        if (!LocalScenes.Relocation || !LocalScenes.Institutions || restoring) return false;
        var act = ArtistManager.Instance.GetArtist(artistId);
        var program = state?.Programs.FirstOrDefault(p => p.Id == programId && p.SponsorId == sponsor?.labelId);
        var room = SceneRoomCatalog.Get(program?.RoomId);
        int day = LocalSceneRoomService.Day(TimeManager.Instance.CurrentDate);
        var people = People(act);
        string from = act?.geography?.basePlaceId;
        if (!Canonical(sponsor) || act == null || string.IsNullOrEmpty(from) || act.labelId != sponsor.labelId || !act.isActive ||
            act.lifecycleStatus != ArtistLifecycleStatus.Active || act.careerState is CareerState.Retired or CareerState.Disbanded ||
            program == null || room == null || people.Length == 0 || people.Any(m => string.IsNullOrEmpty(m.personId)) ||
            people.Select(m => m.personId).Distinct().Count() != people.Length) {
            message = "A funded destination program and an active act on this label's roster are required."; return false;
        }
        if (state.Moves.Any(m => m.ArtistId == artistId && m.Status == SceneMoveStatus.Planned) ||
            (act.isPlayerOwned && (PlayerDesk.Instance?.RoadBookingFor(artistId) is { WeeksRemaining: > 0 } || PlayerDesk.Instance?.SessionPlayerFor(artistId) != null)) ||
            LocalSceneRoomService.HasFutureCommitment(artistId, day) ||
            act.sceneParticipations?.Any(p => p.relationship is not (SceneRelationship.Resident or SceneRelationship.Alumnus) &&
                (!p.endWeek.HasValue || p.endWeek >= day / 7)) == true ||
            act.geography.moves?.Any(m => m.year >= TimeManager.Instance.CurrentDate.year - 1) == true ||
            ArtistManager.Instance.GetAllArtists().Any(a => a.artistId != artistId && a.isActive &&
                People(a).Any(m => people.Any(p => p.personId == m.personId)))) {
            message = "Existing engagements, shared personnel, guest travel or a recent move prevent relocation."; return false;
        }
        int year = TimeManager.Instance.CurrentDate.year;
        GenreFamily family = GenreCatalog.Get(GenreCatalog.MapLegacy(act.primaryGenre, year)).Family;
        double baseline = SceneCityPlacement.Weight(from, act.primaryGenre, year);
        if (!room.Families.Contains(family) || !GenreSupplyService.IsAvailableForNewSupply(act.primaryGenre, year) ||
            !(baseline > 0) || SceneCityPlacement.Weight(room.PlaceId, act.primaryGenre, year) / baseline < SceneDynamicsMobility.MinimumOpportunityRatio ||
            !ScenePlaceRegistry.TryDomesticRoadMiles(from, room.PlaceId, out double miles) || miles <= 0 || miles > SceneDynamicsMobility.MaximumRoadMiles) {
            message = "The destination needs suitable programming, a stronger genre opportunity and a feasible domestic route within 350 miles."; return false;
        }
        int departure = Math.Max(day + 1, program.StartDay);
        int arrival = departure + Math.Max(1, (int)Math.Ceiling(miles / 250));
        if (arrival + 28 > program.ThroughDay || state.Moves.Any(m => m.ProgramId == programId && m.Status != SceneMoveStatus.Cancelled)) {
            message = "The program must have four weeks remaining and an unused housing/work introduction."; return false;
        }
        if (people.Any(p => !Consents(p, from, room.PlaceId, day / 91))) {
            message = "The members have not all agreed to move. Their decision holds for this destination this quarter."; return false;
        }
        // Housing + transport are prepaid, separate from the rehearsal/showcase investment.
        float cost = (float)(100 * people.Length + .5 * miles);
        if (!sponsor.CanAffordToSign(cost)) { message = "The label cannot fund travel and temporary housing while retaining its reserve."; return false; }
        Pay(sponsor, cost);
        State.Moves.Add(new SceneFundedMove { Id = $"move|{artistId}|{day}", ArtistId = artistId, SponsorId = sponsor.labelId,
            ProgramId = programId, FromPlaceId = from, ToPlaceId = room.PlaceId, DepartureDay = departure, ArrivalDay = arrival,
            PersonIds = people.Select(p => p.personId).ToList(), Cost = cost,
            Reason = "All active members consented; label prepaid domestic travel/temporary housing; funded destination work introduction" });
        message = $"The members agreed. Travel and temporary housing cost ${cost:N0}; arrival is {LocalSceneRoomService.Date(arrival).ToHeadlineString()}. Their hometown stays the same.";
        return true;
    }
    // Prevent bookings at the old base after departure, including calendars made before arrival.
    public static bool AvailableAt(string artistId, string placeId, int day) =>
        state?.Moves.Any(m => m.ArtistId == artistId && m.Status == SceneMoveStatus.Planned && day >= m.DepartureDay) != true;
    public static void Advance(GameDate date) {
        if (!LocalScenes.Relocation || !LocalScenes.Institutions || restoring || state == null) return;
        int day = LocalSceneRoomService.Day(date);
        if (day <= state.LastProcessedDay) return;
        foreach (var move in state.Moves.Where(m => m.Status == SceneMoveStatus.Planned && m.ArrivalDay <= day)
            .OrderBy(m => m.ArrivalDay).ThenBy(m => m.Id, StringComparer.Ordinal)) {
            var a = ArtistManager.Instance.GetArtist(move.ArtistId);
            var program = state.Programs.FirstOrDefault(p => p.Id == move.ProgramId);
            var destination = SceneRoomCatalog.Get(program?.RoomId);
            var people = People(a);
            double sourceOpportunity = a == null ? 0 : SceneCityPlacement.Weight(move.FromPlaceId, a.primaryGenre, date.year);
            bool suitable = a != null && destination != null && sourceOpportunity > 0 &&
                GenreSupplyService.IsAvailableForNewSupply(a.primaryGenre, date.year) &&
                destination.Families.Contains(GenreCatalog.Get(GenreCatalog.MapLegacy(a.primaryGenre, date.year)).Family) &&
                SceneCityPlacement.Weight(move.ToPlaceId, a.primaryGenre, date.year) / sourceOpportunity >= SceneDynamicsMobility.MinimumOpportunityRatio;
            bool valid = a != null && a.isActive && a.lifecycleStatus == ArtistLifecycleStatus.Active &&
                (ChartManager.Instance.GetAllLabels().Any(l => l.labelId == move.SponsorId && l.IsActive) || PlayerDesk.Instance?.Label is { IsActive: true } player && player.labelId == move.SponsorId) &&
                a.careerState is not (CareerState.Retired or CareerState.Disbanded) && a.labelId == move.SponsorId &&
                a.geography?.basePlaceId == move.FromPlaceId && program?.ThroughDay >= day && suitable &&
                people.Select(p => p.personId).SequenceEqual(move.PersonIds) &&
                !LocalSceneRoomService.HasFutureCommitment(move.ArtistId, move.DepartureDay) &&
                !ArtistManager.Instance.GetAllArtists().Any(other => other.artistId != move.ArtistId && other.isActive &&
                    People(other).Any(p => move.PersonIds.Contains(p.personId)));
            if (!valid) { move.Status = SceneMoveStatus.Cancelled; move.Reason += "; cancelled after career, contract, lineup, genre opportunity or program changed; prepaid travel/housing is spent"; continue; }
            foreach (var geo in people.Select(p => p.geography ??= new()).Prepend(a.geography)) {
                geo.moves ??= new();
                geo.moves.Add(new PlaceMove { fromPlaceId = geo.basePlaceId, toPlaceId = move.ToPlaceId,
                    year = date.year, month = date.month, day = date.day, precision = PlaceDatePrecision.Day,
                    reason = move.Reason, evidence = PlaceEvidence.Simulated });
                geo.basePlaceId = move.ToPlaceId; geo.baseEvidence = PlaceEvidence.Simulated;
            }
            move.Status = SceneMoveStatus.Arrived;
            LocalSceneIdentityService.EnsureArtist(a);
            a.careerEvents.Add($"{date.ToHeadlineString()}: Moved working base to {ScenePlaceRegistry.Get(move.ToPlaceId).Name} through a funded program.");
        }
        state.LastProcessedDay = day;
    }
    public static void CaptureWorld(WorldSaveData world) => world.SceneEcosystem = LocalSceneRoomService.Copy(state);
    public static void ValidateRestore(SceneEcosystemSaveData saved) {
        if (saved == null) return;
        if (saved.SchemaVersion != 1 || saved.LastProcessedDay < -1 || saved.Programs == null || saved.Moves == null ||
            saved.Programs.Select(p => p.Id).Distinct().Count() != saved.Programs.Count ||
            saved.Moves.Select(m => m.Id).Distinct().Count() != saved.Moves.Count ||
            saved.Moves.Where(m => m.Status == SceneMoveStatus.Planned).GroupBy(m => m.ArtistId).Any(g => g.Count() > 1) ||
            saved.Moves.Where(m => m.Status != SceneMoveStatus.Cancelled).GroupBy(m => m.ProgramId).Any(g => g.Count() > 1) ||
            saved.Programs.Any(p => string.IsNullOrEmpty(p.Id) || SceneRoomCatalog.Get(p.RoomId)?.IsPerformance != true ||
                string.IsNullOrEmpty(p.SponsorId) || p.StartDay < 0 || p.ThroughDay < p.StartDay || !float.IsFinite(p.Cost) || p.Cost <= 0) ||
            saved.Moves.Any(m => string.IsNullOrEmpty(m.Id) || string.IsNullOrEmpty(m.ArtistId) ||
                !saved.Programs.Any(p => p.Id == m.ProgramId && p.SponsorId == m.SponsorId && SceneRoomCatalog.Get(p.RoomId).PlaceId == m.ToPlaceId) ||
                m.DepartureDay < 0 || m.ArrivalDay <= m.DepartureDay || m.PersonIds == null || m.PersonIds.Count == 0 ||
                m.PersonIds.Any(string.IsNullOrEmpty) || m.PersonIds.Distinct().Count() != m.PersonIds.Count ||
                !Enum.IsDefined(m.Status) || !float.IsFinite(m.Cost) || m.Cost <= 0 ||
                !ScenePlaceRegistry.TryDomesticRoadMiles(m.FromPlaceId, m.ToPlaceId, out double miles) || miles <= 0))
            throw new InvalidOperationException("Invalid scene ecosystem save schema or references.");
    }
    public static void BeginRestore(SceneEcosystemSaveData saved) { ValidateRestore(saved); previous = state; state = LocalSceneRoomService.Copy(saved); restoring = true; }
    public static void CompleteRestore() { restoring = false; previous = null; }
    public static void CancelRestore() { if (!restoring) return; state = previous; previous = null; restoring = false; }
}
