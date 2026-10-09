using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Identity projection only. No births, contracts, RNG streams, price/quality changes or booking decisions.
/// ArtistManager and band life still own the population and people. Indices contain IDs, never copies.
/// </summary>
public static class LocalSceneIdentityService {
    public const int AssignmentVersion = 2;
    private const ulong Namespace = 0x7363656e65696431UL; // sceneid1: distinct from population/band-life draws
    private static SceneIdentitySaveData state;
    private static SceneIdentitySaveData previousState;
    private static bool restoring;
    private static readonly Dictionary<string, SortedSet<string>> byBase = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, string> indexedBase = new(StringComparer.Ordinal);

    private static SceneIdentitySaveData State => state ??= new SceneIdentitySaveData {
        ContentVersion = ScenePlaceRegistry.ContentVersion, AssignmentVersion = AssignmentVersion,
        WorldSeed = SimulationSeedBootstrap.RequestedSeed ?? BandLife.WorldSeed
    };
    /// <summary>Stable across processes and saves. Never uses String.GetHashCode or a mutable RNG.</summary>
    public static ulong KeyedSeed => State.WorldSeed;
    public static double AssignmentUnit(ulong seed, string key) {
        unchecked {
            ulong h = 14695981039346656037UL ^ seed ^ Namespace;
            foreach (char c in key ?? string.Empty) { h ^= c; h *= 1099511628211UL; }
            h ^= h >> 30; h *= 0xbf58476d1ce4e5b9UL; h ^= h >> 27; h *= 0x94d049bb133111ebUL; h ^= h >> 31;
            return (h >> 11) * (1.0 / 9007199254740992.0);
        }
    }
    /// <summary>Versioned national opportunity placement; preserves the separate commercial region.</summary>
    public static string InferBase(ulong seed, string entityId, string region, Genre genre, ArtistCohort cohort, int year = 1960) =>
        SceneCityPlacement.Assign(seed, entityId, region, genre, cohort, year);
    public static void EnsureArtist(SimulatedArtist artist, bool generated = false, string formationPlaceId = null) {
        if (!LocalScenes.Observing || restoring || artist == null) return;
        artist.geography ??= new GeographicIdentity();
        GeographicIdentity geo = artist.geography;
        if (string.IsNullOrEmpty(geo.basePlaceId)) {
            string origin = ScenePlaceRegistry.Get(geo.originPlaceId)?.Id;
            string supplied = ScenePlaceRegistry.Get(formationPlaceId)?.Id;
            geo.basePlaceId = supplied ?? origin ?? InferBase(State.WorldSeed, artist.artistId, artist.homeRegion, artist.primaryGenre, artist.cohort, artist.formedYear);
            geo.baseEvidence = generated ? PlaceEvidence.Simulated : PlaceEvidence.Inferred;
            geo.assignmentSource = geo.basePlaceId == null ? "unmapped-legacy-region" :
                supplied != null ? "existing-formation-location" : origin != null ? "existing-origin-base-inference" : "keyed-national-city-opportunity-genre-prior-v2";
            geo.assignmentVersion = AssignmentVersion;
        }
        // Only a newly simulated formation may establish a procedural hometown. Legacy migration cannot.
        if (generated && string.IsNullOrEmpty(geo.originPlaceId) && geo.basePlaceId != null) {
            geo.originPlaceId = geo.basePlaceId; geo.originEvidence = PlaceEvidence.Simulated;
        }
        foreach (Musician person in artist.members ?? new List<Musician>()) EnsurePerson(person, geo.basePlaceId, geo.baseEvidence);
        Index(artist);
        LocalScenePersistenceService.ObserveArtist(artist);
    }
    public static void EnsurePerson(Musician person, string baseId, PlaceEvidence evidence) {
        if (!LocalScenes.Observing || restoring || person == null || string.IsNullOrEmpty(baseId)) return;
        person.geography ??= new GeographicIdentity();
        if (!string.IsNullOrEmpty(person.geography.basePlaceId)) return;
        person.geography.basePlaceId = baseId;
        person.geography.baseEvidence = evidence;
        person.geography.assignmentSource = "existing-act-working-base; personal-origin-unknown";
        person.geography.assignmentVersion = AssignmentVersion;
    }
    /// <summary>Records existing owner decisions; Phase 1 never makes a hire/move decision itself.</summary>
    public static void PersonJoined(Musician person, SimulatedArtist act, int year) {
        if (!LocalScenes.Observing || restoring || act == null || person == null) return;
        EnsureArtist(act);
        string target = act.geography?.basePlaceId;
        if (target == null) return;
        EnsurePerson(person, target, act.geography.baseEvidence);
        GeographicIdentity geo = person.geography;
        if (geo.basePlaceId == target) return;
        geo.moves ??= new List<PlaceMove>();
        geo.moves.Add(new PlaceMove { fromPlaceId = geo.basePlaceId, toPlaceId = target, year = year,
            reason = "Existing band-life lineup join; observe-only", evidence = PlaceEvidence.Simulated });
        geo.basePlaceId = target; geo.baseEvidence = PlaceEvidence.Simulated;
    }
    public static void EnsureLabel(AILabel label) {
        if (!LocalScenes.Observing || restoring || label == null) return;
        GeographicIdentity existing = label.geography;
        // Only repair our own unresolved literal projection. Credible saved identities,
        // origins and moves win; catalog correction never constitutes a relocation.
        if (existing != null && (!string.IsNullOrEmpty(existing.basePlaceId) ||
            !string.IsNullOrEmpty(existing.originPlaceId) || existing.moves?.Count > 0 ||
            existing.assignmentSource != "unresolved-literal-hq; distribution-proxy-not-origin")) return;
        ScenePlace place = ScenePlaceRegistry.ResolveHeadquarters(label.headquartersCity, label.homeRegion);
        if (existing != null) {
            if (place == null) return;
            existing.originPlaceId = existing.basePlaceId = place.Id;
            existing.originEvidence = existing.baseEvidence = PlaceEvidence.Explicit;
            existing.assignmentSource = "resolved-existing-literal-hq-v2; no relocation";
            existing.assignmentVersion = AssignmentVersion;
            return;
        }
        label.geography = new GeographicIdentity { originPlaceId = place?.Id, basePlaceId = place?.Id,
            originEvidence = place == null ? PlaceEvidence.Unknown : PlaceEvidence.Explicit,
            baseEvidence = place == null ? PlaceEvidence.Unknown : PlaceEvidence.Explicit,
            assignmentSource = place == null ? "unresolved-literal-hq; distribution-proxy-not-origin" : "existing-literal-hq",
            assignmentVersion = AssignmentVersion };
        // In particular, do not copy label.homeCityId for a foreign or unmapped HQ.
    }
    private static void Index(SimulatedArtist artist) {
        ForgetArtist(artist.artistId);
        string id = artist.geography?.basePlaceId;
        if (id == null) return;
        if (!byBase.TryGetValue(id, out var ids)) byBase[id] = ids = new(StringComparer.Ordinal);
        ids.Add(artist.artistId); indexedBase[artist.artistId] = id;
    }
    public static void ForgetArtist(string artistId) {
        if (artistId == null || !indexedBase.Remove(artistId, out string baseId)) return;
        if (byBase.TryGetValue(baseId, out var ids)) { ids.Remove(artistId); if (ids.Count == 0) byBase.Remove(baseId); }
    }
    public static IReadOnlyList<string> ArtistsBasedAt(string placeId) =>
        placeId != null && byBase.TryGetValue(placeId, out var ids) ? Array.AsReadOnly(ids.ToArray()) : Array.Empty<string>();
    public static void CaptureWorld(WorldSaveData world) {
        world.SceneIdentity = state == null && !LocalScenes.Observing ? null : new SceneIdentitySaveData {
            ContentVersion = State.ContentVersion, AssignmentVersion = State.AssignmentVersion, WorldSeed = State.WorldSeed
        };
    }
    public static void BeginRestore(SceneIdentitySaveData saved, ulong? seed) {
        if (saved != null && (saved.ContentVersion > ScenePlaceRegistry.ContentVersion || saved.AssignmentVersion > AssignmentVersion))
            throw new InvalidOperationException("This scene identity save requires a newer place/assignment catalog.");
        previousState = state;
        state = saved ?? (LocalScenes.Observing ? new SceneIdentitySaveData { ContentVersion = ScenePlaceRegistry.ContentVersion,
            AssignmentVersion = AssignmentVersion, WorldSeed = seed ?? BandLife.WorldSeed } : null);
        restoring = true;
        byBase.Clear(); indexedBase.Clear();
    }
    /// <summary>Real save load calls this after both world and player acts exist; headless world loads also use it.</summary>
    public static void CompleteRestore() {
        restoring = false;
        byBase.Clear(); indexedBase.Clear();
        foreach (SimulatedArtist artist in (ArtistManager.Instance?.GetAllArtists() ?? Array.Empty<SimulatedArtist>()).OrderBy(a => a.artistId, StringComparer.Ordinal)) {
            EnsureArtist(artist); Index(artist);
        }
        foreach (PooledPerson pooled in PersonPool.All.OrderBy(p => p.person.personId, StringComparer.Ordinal)) {
            string baseId = ArtistManager.Instance?.GetArtist(pooled.lastArtistId)?.geography?.basePlaceId;
            baseId ??= LocalScenes.Observing ? InferBase(State.WorldSeed, pooled.person.personId, pooled.homeRegion, pooled.lastGenre, ArtistCohort.InitialLegacy) : null;
            EnsurePerson(pooled.person, baseId, PlaceEvidence.Inferred);
        }
        foreach (AILabel label in ChartManager.Instance?.GetAllLabels() ?? new List<AILabel>()) EnsureLabel(label);
        EnsureLabel(PlayerDesk.Instance?.Label);
        if (LocalScenes.Observing && state != null) {
            state.ContentVersion = ScenePlaceRegistry.ContentVersion; state.AssignmentVersion = AssignmentVersion;
        }
        previousState = null;
        LocalScenePersistenceService.CompleteRestore();
        LocalSceneRoomService.CompleteRestore();
        SceneEcosystemService.CompleteRestore();
    }
    public static void CompleteDirectPlayerRestore() { if (!restoring) CompleteRestore(); }
    public static void CancelRestore() { SceneEcosystemService.CancelRestore(); LocalSceneRoomService.CancelRestore(); LocalScenePersistenceService.CancelRestore(); if (!restoring) return; restoring = false; state = previousState; previousState = null; byBase.Clear(); indexedBase.Clear(); }
}
