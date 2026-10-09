using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Identity projection only. No births, contracts, RNG streams, price/quality changes or booking decisions.
/// ArtistManager and band life still own the population and people. Indices contain IDs, never copies.
/// </summary>
public static class LocalSceneIdentityService {
    public const int AssignmentVersion = 1;
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
    public static double AssignmentUnit(ulong seed, string key) {
        unchecked {
            ulong h = 14695981039346656037UL ^ seed ^ Namespace;
            foreach (char c in key ?? string.Empty) { h ^= c; h *= 1099511628211UL; }
            h ^= h >> 30; h *= 0xbf58476d1ce4e5b9UL; h ^= h >> 27; h *= 0x94d049bb133111ebUL; h ^= h >> 31;
            return (h >> 11) * (1.0 / 9007199254740992.0);
        }
    }
    /// <summary>
    /// Provisional placement priors, not historical genre shares or economic weights. Every city in a
    /// legacy region remains eligible; small qualitative fits avoid assigning all legacy acts to a hub.
    /// </summary>
    public static string InferBase(ulong seed, string entityId, string region, Genre genre, ArtistCohort cohort) {
        var places = ScenePlaceRegistry.PlayableInRegion(region);
        if (places.Count == 0) return null;
        double Fit(ScenePlace p) => (genre, p.Id) switch {
            (Genre.Country, "nashville") or (Genre.Jazz, "new_orleans") or (Genre.LatinPop, "miami") or
            (Genre.TexMex, "san_antonio") or (Genre.Soul, "memphis") or (Genre.Soul, "detroit") or
            (Genre.Folk, "boston") or (Genre.Folk, "san_francisco") => 1.5,
            _ => 1.0
        };
        double draw = AssignmentUnit(seed, $"v1|base|{entityId}|{(int)cohort}|{(int)genre}") * places.Sum(Fit);
        foreach (ScenePlace place in places) { draw -= Fit(place); if (draw < 0) return place.Id; }
        return places[^1].Id;
    }
    public static void EnsureArtist(SimulatedArtist artist, bool generated = false, string formationPlaceId = null) {
        if (!LocalScenes.Observing || restoring || artist == null) return;
        artist.geography ??= new GeographicIdentity();
        GeographicIdentity geo = artist.geography;
        if (string.IsNullOrEmpty(geo.basePlaceId)) {
            string origin = ScenePlaceRegistry.Get(geo.originPlaceId)?.Id;
            string supplied = ScenePlaceRegistry.Get(formationPlaceId)?.Id;
            geo.basePlaceId = supplied ?? origin ?? InferBase(State.WorldSeed, artist.artistId, artist.homeRegion, artist.primaryGenre, artist.cohort);
            geo.baseEvidence = generated ? PlaceEvidence.Simulated : PlaceEvidence.Inferred;
            geo.assignmentSource = geo.basePlaceId == null ? "unmapped-legacy-region" :
                supplied != null ? "existing-formation-location" : origin != null ? "existing-origin-base-inference" : "keyed-region-genre-cohort-prior-v1";
            geo.assignmentVersion = AssignmentVersion;
        }
        // Only a newly simulated formation may establish a procedural hometown. Legacy migration cannot.
        if (generated && string.IsNullOrEmpty(geo.originPlaceId) && geo.basePlaceId != null) {
            geo.originPlaceId = geo.basePlaceId; geo.originEvidence = PlaceEvidence.Simulated;
        }
        foreach (Musician person in artist.members ?? new List<Musician>()) EnsurePerson(person, geo.basePlaceId, geo.baseEvidence);
        Index(artist);
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
        if (!LocalScenes.Observing || restoring || label == null || label.geography != null) return;
        ScenePlace place = ScenePlaceRegistry.ByName(label.headquartersCity);
        // Domestic literal matches must agree with known region context. Never confuse Augusta, ME/GA.
        string region = SceneRegionAdapter.ToId(label.homeRegion);
        if (place?.CountryCode == "US" && region != null && place.MarketRegionId != region) place = null;
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
    }
    public static void CompleteDirectPlayerRestore() { if (!restoring) CompleteRestore(); }
    public static void CancelRestore() { if (!restoring) return; restoring = false; state = previousState; previousState = null; byBase.Clear(); indexedBase.Clear(); }
}
