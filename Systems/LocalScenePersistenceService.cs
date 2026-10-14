using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Phase 2: records population-owner decisions and presents a recurring cast. No artist/person births,
/// career decisions, activation, simulated gigs, income, development, or geographic AI recruiting.
/// </summary>
public static class LocalScenePersistenceService {
    public const int SchemaVersion = 1;
    private static ScenePersistenceSaveData state;
    private static ScenePersistenceSaveData previousState;
    private static bool restoring;
    private static readonly Dictionary<string, SortedSet<string>> byScene = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, HashSet<string>> indexedScenes = new(StringComparer.Ordinal);
    private static ScenePersistenceSaveData State => state ??= new();
    public static string SceneIdFor(string placeId) => ScenePlaceRegistry.Get(placeId) is ScenePlace p ? p.PlayableCityId ?? p.Id : null;
    private static int Week => ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
    private static int Year => TimeManager.Instance?.CurrentDate.year ?? 1960;
    private static bool LivingAct(SimulatedArtist a) => a != null && a.lifecycleStatus == ArtistLifecycleStatus.Active && a.isActive &&
        a.careerState is not (CareerState.Disbanded or CareerState.Retired);
    private static bool Performs(SimulatedArtist a) => LivingAct(a) && a.members.Any(m => m.isActive && m.lifeState == MemberLifeState.Active);

    public static void ObserveArtist(SimulatedArtist act) {
        if (!LocalScenes.Persisting || restoring || act == null || string.IsNullOrEmpty(act.artistId)) return;
        string scene = SceneIdFor(act.geography?.basePlaceId);
        var history = act.sceneParticipations;
        if (history == null && scene == null) return; // Unknown geography remains unknown.
        history ??= act.sceneParticipations = new List<SceneParticipation>();
        foreach (var row in history.Where(p => p.relationship == SceneRelationship.Resident && !p.endWeek.HasValue))
            if (!LivingAct(act) || row.sceneId != scene || row.placeId != act.geography?.basePlaceId) row.endWeek = Week;
        if (LivingAct(act) && scene != null && !history.Any(p => p.relationship == SceneRelationship.Resident && !p.endWeek.HasValue && p.sceneId == scene)) {
            double draw = LocalSceneIdentityService.AssignmentUnit(LocalSceneIdentityService.KeyedSeed, "participation-v1|" + act.artistId);
            history.Add(new SceneParticipation { sceneId = scene, placeId = act.geography.basePlaceId,
                relationship = SceneRelationship.Resident, performanceLevel = draw < .1 ? ScenePerformanceLevel.Latent :
                    draw < .35 ? ScenePerformanceLevel.Occasional : ScenePerformanceLevel.Working,
                startYear = Year, startWeek = Week, provenance = "Observed working base; provisional participation prior v1" });
        }
        Reindex(act);
    }
    private static void Reindex(SimulatedArtist act) {
        ForgetArtist(act.artistId);
        if (!LivingAct(act)) return;
        foreach (var row in act.sceneParticipations ?? new List<SceneParticipation>()) {
            if (row.relationship == SceneRelationship.Alumnus || row.relationship == SceneRelationship.Resident && row.endWeek.HasValue || row.endWeek.HasValue && row.endWeek.Value < Week) continue;
            if (row.sceneId == null) continue;
            if (!byScene.TryGetValue(row.sceneId, out var ids)) byScene[row.sceneId] = ids = new(StringComparer.Ordinal);
            ids.Add(act.artistId);
            if (!indexedScenes.TryGetValue(act.artistId, out var scenes)) indexedScenes[act.artistId] = scenes = new(StringComparer.Ordinal);
            scenes.Add(row.sceneId);
        }
    }
    public static void ForgetArtist(string id) {
        if (id == null || !indexedScenes.Remove(id, out var scenes)) return;
        foreach (string scene in scenes) if (byScene.TryGetValue(scene, out var ids)) {
            ids.Remove(id); if (ids.Count == 0) byScene.Remove(scene);
        }
    }
    public static IReadOnlyList<string> MembersOf(string sceneId) => sceneId != null && byScene.TryGetValue(sceneId, out var ids)
        ? Array.AsReadOnly(ids.ToArray()) : Array.Empty<string>();

    /// <summary>Only an existing owner with a real dated presence may add a guest. Visits never call this.</summary>
    public static bool RecordGuestPresence(string artistId, ScenePlace place, int fromWeek, int toWeek, SceneRelationship role, string provenance) {
        if (!LocalScenes.Persisting || restoring || place == null || ScenePlaceRegistry.Get(place.Id) == null ||
            fromWeek < 0 || toWeek < fromWeek || string.IsNullOrWhiteSpace(provenance) ||
            role is SceneRelationship.Resident or SceneRelationship.Alumnus) return false;
        SimulatedArtist act = ArtistManager.Instance?.GetArtist(artistId);
        if (!LivingAct(act)) return false;
        ObserveArtist(act);
        act.sceneParticipations ??= new();
        string scene = SceneIdFor(place.Id);
        if (act.sceneParticipations.Any(p => p.relationship is not (SceneRelationship.Resident or SceneRelationship.Alumnus) &&
            p.sceneId != scene && p.startWeek <= toWeek && (!p.endWeek.HasValue || p.endWeek.Value >= fromWeek))) return false;
        if (!act.sceneParticipations.Any(p => p.sceneId == scene && p.relationship == role && p.startWeek == fromWeek && p.endWeek == toWeek))
            act.sceneParticipations.Add(new SceneParticipation { sceneId = scene, placeId = place.Id, relationship = role,
                performanceLevel = ScenePerformanceLevel.Working, startYear = Year, startWeek = fromWeek,
                endWeek = toWeek, provenance = provenance });
        Reindex(act);
        return true;
    }
    internal static bool Present(SimulatedArtist act, string scene, int week) {
        if (!Performs(act) || act.sceneParticipations == null) return false;
        var current = act.sceneParticipations.Where(p => p.startWeek <= week && (!p.endWeek.HasValue || p.endWeek.Value >= week) &&
            p.relationship != SceneRelationship.Alumnus).ToArray();
        bool hasAwayPresence = current.Any(p => p.relationship != SceneRelationship.Resident && p.sceneId != scene);
        return current.Any(p => p.sceneId == scene && (p.relationship != SceneRelationship.Resident ||
            !hasAwayPresence && !p.endWeek.HasValue && SceneIdFor(act.geography?.basePlaceId) == scene));
    }
    private static bool Fits(SimulatedArtist act, PlayerDesk.ScoutingVenue venue, int year) {
        var family = GenreCatalog.Get(GenreCatalog.MapLegacy(act.primaryGenre, year)).Family;
        return AlbumLegitimacyService.IsEligibleFamily(family) &&
            PlayerDesk.AdmitsGenre(venue, venue == PlayerDesk.ScoutingVenue.IndustryMeets, PlayerDesk.FamiliesFor(venue), act.primaryGenre, year);
    }
    /// <summary>Deterministic candidate cast. Does not claim that a dated gig has occurred (Phase 3).</summary>
    public static IReadOnlyList<string> PreviewCast(string scene, PlayerDesk.ScoutingVenue venue, int week, int year) {
        if (!LocalScenes.Persisting || scene == null) return Array.Empty<string>();
        ulong seed = LocalSceneIdentityService.KeyedSeed;
        var candidates = MembersOf(scene).Select(id => ArtistManager.Instance?.GetArtist(id))
            .Where(a => Present(a, scene, week) && Fits(a, venue, year));
        bool PublicThisWeek(SimulatedArtist a) {
            var rows = a.sceneParticipations.Where(p => p.sceneId == scene && p.startWeek <= week && (!p.endWeek.HasValue || p.endWeek.Value >= week));
            ScenePerformanceLevel level = rows.Select(p => p.performanceLevel).DefaultIfEmpty(ScenePerformanceLevel.Latent).Max();
            double chance = level == ScenePerformanceLevel.Latent ? .1 : level == ScenePerformanceLevel.Occasional ? .5 : 1;
            return LocalSceneIdentityService.AssignmentUnit(seed, $"public-week-v1|{a.artistId}|{week}") < chance;
        }
        int target = 2 + (int)(LocalSceneIdentityService.AssignmentUnit(seed, $"cast-size-v1|{scene}|{(int)venue}|{week}") *
            (venue == PlayerDesk.ScoutingVenue.IndustryMeets ? 2 : 3));
        return Array.AsReadOnly(candidates.Where(PublicThisWeek)
            .OrderBy(a => LocalSceneIdentityService.AssignmentUnit(seed, $"cast-v1|{scene}|{(int)venue}|{week}|{a.artistId}"))
            .ThenBy(a => a.artistId, StringComparer.Ordinal).Take(target).Select(a => a.artistId).ToArray());
    }
    /// <summary>One world-owned pass per chart week. Advances every place whether visited or not.</summary>
    public static void Advance(int week, int year) {
        if (!LocalScenes.Persisting || restoring || week <= State.LastProcessedWeek) return;
        Reconcile();
        State.Windows.Clear();
        foreach (string scene in ScenePlaceRegistry.All.Select(p => SceneIdFor(p.Id)).Where(s => s != null).Distinct().OrderBy(s => s, StringComparer.Ordinal))
            foreach (PlayerDesk.ScoutingVenue venue in Enum.GetValues<PlayerDesk.ScoutingVenue>())
                State.Windows.Add(new SceneEncounterWindow { SceneId = scene, Venue = (int)venue, Week = week,
                    ArtistIds = PreviewCast(scene, venue, week, year).ToList() });
        State.LastProcessedWeek = week;
    }
    public static IReadOnlyList<SimulatedArtist> Encounter(string placeId, PlayerDesk.ScoutingVenue venue, int week, int year) {
        if (!LocalScenes.Persisting) return Array.Empty<SimulatedArtist>();
        Advance(week, year);
        string scene = SceneIdFor(placeId);
        var window = State.Windows.FirstOrDefault(w => w.SceneId == scene && w.Venue == (int)venue && w.Week == week);
        return Array.AsReadOnly((window?.ArtistIds ?? new List<string>()).Select(id => ArtistManager.Instance?.GetArtist(id))
            .Where(a => Present(a, scene, week)).ToArray());
    }
    private static void Reconcile() {
        byScene.Clear(); indexedScenes.Clear();
        foreach (var act in (ArtistManager.Instance?.GetAllArtists() ?? Array.Empty<SimulatedArtist>()).OrderBy(a => a.artistId, StringComparer.Ordinal)) ObserveArtist(act);
    }
    public static void CaptureWorld(WorldSaveData world) {
        world.ScenePersistence = state == null ? null : new ScenePersistenceSaveData { SchemaVersion = state.SchemaVersion,
            LastProcessedWeek = state.LastProcessedWeek, Windows = state.Windows.Select(w => new SceneEncounterWindow {
                SceneId = w.SceneId, Venue = w.Venue, Week = w.Week, ArtistIds = w.ArtistIds.ToList() }).ToList() };
    }
    public static void ValidateRestore(ScenePersistenceSaveData saved) {
        if (saved?.SchemaVersion > SchemaVersion) throw new InvalidOperationException("This scene world requires a newer persistence schema.");
    }
    public static void BeginRestore(ScenePersistenceSaveData saved) {
        ValidateRestore(saved); previousState = state; state = saved; restoring = true;
        byScene.Clear(); indexedScenes.Clear();
    }
    public static void CompleteRestore() { restoring = false; if (LocalScenes.Persisting) Reconcile(); previousState = null; }
    public static void CancelRestore() {
        if (!restoring) return; restoring = false; state = previousState; previousState = null;
        byScene.Clear(); indexedScenes.Clear();
    }
}
