using System;
using Godot;

/// <summary>
/// The album-as-art movement, built as an emergent loop rather than an object. There is no
/// milestone registry and no "art album" type: a landmark record is simply an album that
/// scored high on cohesion and then succeeded in public. The movement is the KNOCK-ON --
/// the exogenous cohesion ramp becomes partly earned by records that actually happened, and
/// peers who were paying attention remember it.
/// <para>
/// One bounded global scalar and a small landmark ring. If the loop never starts, the
/// exogenous ramp carries the decade exactly as it does today, which is the phase's own
/// gate: legitimacy pinned at 0 must reproduce the previous phase.
/// </para>
/// </summary>
public static class AlbumLegitimacyService {
	/// <summary>Hard zero before this year. Nothing had happened yet that could be leaned on.</summary>
	public const int LegitimacyStartYear = 1964;
	/// <summary>
	/// How far legitimacy may pull the cohesion ceiling forward. The existing curve remains
	/// the floor AND the shape; legitimacy can bring it forward in time, not rewrite it.
	/// </summary>
	public const float MaxCeilingLift = .25f;
	/// <summary>A record has to have actually hung together to count.</summary>
	public const float LandmarkCohesionBar = .72f;
	/// <summary>...and it has to have been heard. Cohesion alone in private is not a movement.</summary>
	public const float LandmarkReceptionBar = .45f;
	public const float ContributionPerLandmark = .06f;
	/// <summary>
	/// By 1968 everybody is already making this kind of record, so a 1968 statement moves the
	/// needle far less than a 1965 one. This is the year the earliness premium has decayed away.
	/// </summary>
	public const int EarlinessExhaustedYear = 1969;
	private const int LandmarkRingCapacity = 16;
	/// <summary>Capacity-bounded per artist: an unbounded influence list on 22.5k artists is a leak.</summary>
	public const int MaxInfluencesPerArtist = 8;
	public const int InfluenceMemoryYears = 3;

	public readonly struct Landmark {
		public readonly long Sequence;
		public readonly string ArtistId;
		public readonly Genre Genre;
		public readonly int Year;
		public readonly float Strength;
		public Landmark(long sequence, string artistId, Genre genre, int year, float strength) {
			Sequence = sequence; ArtistId = artistId; Genre = genre; Year = year; Strength = strength;
		}
	}

	private static readonly Landmark[] Ring = new Landmark[LandmarkRingCapacity];
	private static long landmarkSequence;

	public static float Legitimacy { get; private set; }
	public static long LandmarkCount => landmarkSequence;

	/// <summary>How much of the earliness premium a landmark in this year still carries.</summary>
	public static float GetEarliness(int year) => Mathf.Clamp(
		(EarlinessExhaustedYear - year) / (float)(EarlinessExhaustedYear - LegitimacyStartYear), 0f, 1f);

	public static bool IsLandmark(int year, float thematicCohesion, float reception) =>
		year >= LegitimacyStartYear && thematicCohesion >= LandmarkCohesionBar && reception >= LandmarkReceptionBar;

	/// <summary>
	/// The era term with legitimacy applied. Clamped to at most <see cref="MaxCeilingLift"/>
	/// above the exogenous curve and never below it, so the authored ramp is a floor.
	/// </summary>
	public static float ApplyToEraTerm(float exogenousEra, float legitimacy) =>
		Mathf.Clamp(exogenousEra * (1f + MaxCeilingLift * Mathf.Clamp(legitimacy, 0f, 1f)),
			exogenousEra, exogenousEra * (1f + MaxCeilingLift));

	/// <summary>Reads the live global so <see cref="AlbumModel"/> stays free of feature switches.</summary>
	public static float CurrentCeilingMultiplier =>
		ArtistEvolution.AlbumLegitimacyEnabled ? 1f + MaxCeilingLift * Mathf.Clamp(Legitimacy, 0f, 1f) : 1f;

	/// <summary>
	/// Called once per completed album chart run. A record contributes only if it cleared a
	/// real bar in public: high cohesion AND genuine reception.
	/// </summary>
	public static void OnAlbumChartRunComplete(SimulatedArtist artist, RecordRuntimeData record, int year) {
		if (!ArtistEvolution.AlbumLegitimacyEnabled || artist == null) return;
		Album album = record?.baseRecord?.album;
		if (album == null || record.baseRecord.format != ReleaseFormat.Album) return;
		float reception = Mathf.Max(ArtistCriticalAcclaimService.GetCommercialScore(record.peakPosition),
			Mathf.Clamp(artist.criticalAcclaim, 0f, 1f));
		if (!IsLandmark(year, album.thematicCohesion, reception)) return;

		float earliness = GetEarliness(year);
		float strength = Mathf.Clamp(album.thematicCohesion * reception * earliness, 0f, 1f);
		Legitimacy = Mathf.Clamp(Legitimacy + ContributionPerLandmark * strength, 0f, 1f);
		Ring[(int)(landmarkSequence % LandmarkRingCapacity)] = new Landmark(landmarkSequence,
			artist.artistId, record.baseRecord.primaryGenre, year, strength);
		landmarkSequence++;
	}

	/// <summary>
	/// Lazy, indexed propagation. Called at the artist's own next evaluation, reading only
	/// the landmarks that have appeared since they last looked. There is no sweep over
	/// GetAllArtists per landmark and there must not be one.
	/// </summary>
	public static void AbsorbLandmarks(SimulatedArtist artist, int year) {
		if (!ArtistEvolution.AlbumLegitimacyEnabled) return;
		ArtistEvolutionProfile profile = artist?.evolution;
		if (profile == null || profile.lastLandmarkSequenceSeen >= landmarkSequence) return;
		long from = Math.Max(profile.lastLandmarkSequenceSeen, landmarkSequence - LandmarkRingCapacity);
		Genre identity = GenreCatalog.MapLegacy(artist.primaryGenre, year);
		for (long sequence = from; sequence < landmarkSequence; sequence++) {
			Landmark landmark = Ring[(int)(sequence % LandmarkRingCapacity)];
			if (landmark.ArtistId == null || landmark.ArtistId == artist.artistId) continue;
			// You have to have been paying attention, and it has to be music you could
			// plausibly have made. This is the only place peer influence enters.
			float adjacency = GenreMarketMomentumService.GetAdjacency(identity, landmark.Genre);
			if (adjacency < ArtistEvolution.AdjacencyFloor) continue;
			float strength = Mathf.Clamp(landmark.Strength * adjacency * profile.peerSensitivity, 0f, 1f);
			if (strength <= .02f) continue;
			Remember(profile, new ArtistInfluenceMemory {
				sourceArtistId = landmark.ArtistId, sourceGenre = landmark.Genre,
				type = ArtistInfluenceType.CohesiveAlbum, year = landmark.Year, strength = strength
			}, year);
		}
		profile.lastLandmarkSequenceSeen = landmarkSequence;
	}

	/// <summary>Keeps the strongest few and forgets anything stale, on insert.</summary>
	private static void Remember(ArtistEvolutionProfile profile, ArtistInfluenceMemory memory, int year) {
		profile.influences.RemoveAll(existing => year - existing.year > InfluenceMemoryYears);
		profile.influences.Add(memory);
		if (profile.influences.Count <= MaxInfluencesPerArtist) return;
		int weakest = 0;
		for (int index = 1; index < profile.influences.Count; index++)
			if (profile.influences[index].strength < profile.influences[weakest].strength) weakest = index;
		profile.influences.RemoveAt(weakest);
	}

	internal static void ResetForProbe() {
		Legitimacy = 0f;
		landmarkSequence = 0;
		Array.Clear(Ring, 0, Ring.Length);
	}

	internal static void SetLegitimacyForProbe(float value) => Legitimacy = Mathf.Clamp(value, 0f, 1f);
}
