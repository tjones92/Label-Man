using System;
using System.Collections.Generic;

/// <summary>
/// Career-arc state for one act. Identity is NOT decided here: GenreSupplyService
/// remains the only authority on what genre a record is. This structure is the
/// witness that watches those selections accumulate and, when a majority of the
/// window has drifted onto a musically adjacent genre, ratifies what the act has
/// already been playing. See SimTools/ArtistEvolutionDirective.md section 2.
/// </summary>
[Serializable]
public sealed class ArtistEvolutionProfile {
	// --- disposition, derived once from the lineup. Stable unless membership changes.
	public float artisticAmbition;      // wants to make important records
	public float experimentalAppetite;  // tolerance for the unfamiliar
	public float commercialPragmatism;  // will chase a hit under pressure
	public float rootsAttachment;       // resistance to abandoning the original sound
	public float conceptualThinking;    // album-as-statement inclination
	public float peerSensitivity;       // reacts to other people's records
	public float volatility;            // swings hard after success or failure

	// --- mood, moves with outcomes
	public float confidence;
	public float frustration;

	// --- pressure, recomputed once per PROJECT and then read as two floats by the supply
	// weight. Computing it per candidate genre would run it ~40 times per selection across
	// 60k selections; this runs it once per release.
	public float commercialPressure;
	public float artisticPressure;
	/// <summary>Being taken seriously and wanting more of it. The Pet Sounds motive.</summary>
	public float criticalPressure;
	public float peerPressure;
	public float labelPressure;
	public float internalPressure;
	public float resistance;
	/// <summary>Bounded [0,1] net restlessness. 0 reproduces the neutral identity-fit constants exactly.</summary>
	public float restlessness;
	/// <summary>The band that strips back to the blues: the lift goes backward, to the formation genre.</summary>
	public bool rootsMode;
	public ArtistEvolutionTrigger dominantTrigger;
	/// <summary>
	/// The winning pressure's score against its own salience scale. Kept so a
	/// candidate-dependent motive can be weighed against the act's internal ones.
	/// </summary>
	public float dominantSalience;
	/// <summary>
	/// What kind of record the strongest live influence memory was. Decides whether the peer
	/// channel reads as chasing a hit or as joining the album-as-art movement.
	/// </summary>
	public ArtistInfluenceType dominantInfluence;
	/// <summary>Acclaim as of the previous project, so the critical motive can read a trend.</summary>
	public float acclaimAtLastProject;
	/// <summary>What the act's label has noticed working elsewhere. Null when it has noticed nothing.</summary>
	public Genre? labelWantsGenre;
	/// <summary>What the label is pushing for. The player's only lever on an act's direction.</summary>
	public float labelPressureDirective;
	public ReleaseCreativeIntent lastReleaseIntent;

	// --- arc state
	public ArtistArcPhase phase = ArtistArcPhase.Formative;
	public Genre artisticCenter;        // == artist.primaryGenre; the ratified identity
	public Genre priorArtisticCenter;
	public int lastIdentityChangeYear = -1;
	public int projectsSinceIdentityChange;
	public int dispositionMemberCount;  // lineup size the disposition was derived from

	// --- the drift window: the last N project genres. Fixed capacity ring, oldest overwritten.
	public Genre[] recentProjectGenres = new Genre[ArtistEvolution.DriftWindow];
	public int recentProjectCount;      // total pushes, not the live length; index = count % DriftWindow

	public List<ArtistEraRecord> eras = new List<ArtistEraRecord>();
	public List<ArtistInfluenceMemory> influences = new List<ArtistInfluenceMemory>();
	/// <summary>Read cursor into the global landmark ring. Propagation is lazy and indexed, never a sweep.</summary>
	public long lastLandmarkSequenceSeen;

	/// <summary>Live window length, capped at capacity.</summary>
	public int WindowLength => Math.Min(recentProjectCount, ArtistEvolution.DriftWindow);
	public bool WindowFull => recentProjectCount >= ArtistEvolution.DriftWindow;

	public void PushProjectGenre(Genre genre) {
		recentProjectGenres[recentProjectCount % ArtistEvolution.DriftWindow] = genre;
		recentProjectCount++;
		projectsSinceIdentityChange++;
	}

	/// <summary>Clears the window so a fresh era is judged on its own releases.</summary>
	public void ResetWindow() {
		Array.Clear(recentProjectGenres, 0, recentProjectGenres.Length);
		recentProjectCount = 0;
	}

	public ArtistEraRecord CurrentEra => eras.Count == 0 ? null : eras[^1];
}

public enum ArtistArcPhase {
	Formative, HitSeeking, Breakthrough, Consolidation,
	Experimental, Conceptual, RootsReturn, CommercialPivot, Declining, Legacy
}

public enum ArtistEvolutionTrigger {
	None, CommercialFailure, CommercialBreakthrough, CriticalBreakthrough,
	PeerInfluence, GenreClimateShift, CohesiveAlbumMovement,
	InternalTension, LabelPressure, PersonalAmbition, BackToRoots
}

[Serializable]
public sealed class ArtistEraRecord {
	public int eraIndex;
	public int startYear;
	public int endYear;                 // 0 == current era
	public Genre primaryGenre;
	public Genre secondaryGenre;
	public ArtistArcPhase phase;
	public ArtistEvolutionTrigger trigger;
	/// <summary>
	/// The act whose record caused this one, when a peer record is what moved them. Stored
	/// as an id and resolved to a name only when a panel is open -- a biography that names
	/// the other band is the difference between "somebody else's record" and a scene.
	/// </summary>
	public string influencedByArtistId;
	public string summary;              // one composed line, written at close/open

	// What actually happened during the era. Counted as it happens so the biography is
	// assembled from facts rather than invented, and so no pass over release history is
	// needed to render a discography.
	public int releases;
	public int chartedReleases;
	public int top40Releases;
	public int bestPeakPosition;        // 0 == never charted
	public int cohesiveAlbums;

	public bool IsOpen => endYear == 0;
}

[Serializable]
public struct ArtistInfluenceMemory {
	public string sourceArtistId;
	public Genre sourceGenre;
	public ArtistInfluenceType type;
	public int year;
	public float strength;
}

public enum ArtistInfluenceType { HitSingle, CohesiveAlbum, GenreBreakthrough }

/// <summary>
/// A derived label for what a release was reaching for, computed at release time
/// from pressure and outcome history and written to telemetry. It is flavor with a
/// paper trail: nothing downstream reads it to decide what gets made.
/// </summary>
public enum ReleaseCreativeIntent {
	Unknown, Consolidate, ChaseHit, Experiment, Statement, ReturnToRoots, FollowPeer
}
