using System;
using System.Collections.Generic;

public enum PolarEvidenceGate { FirstListen, FollowUp, Demo, Rehearsal, Playback }

/// <summary>Where the player is hearing the material. Studio projects an act onto the song;
/// every other source is the player's ear on the song itself.</summary>
public enum PolarHearingSource { Studio, Venue, Record, Playback }

/// <summary>The listening situation a comparison is drawn for. Display context only; never truth.</summary>
public sealed class PolarHearing {
	public static readonly PolarHearing Studio = new() { source = PolarHearingSource.Studio };
	public PolarHearingSource source;
	/// <summary>Venue: the room, already phrased for display ("the honky-tonks").</summary>
	public string place;
	public GameDate? when;
	/// <summary>Venue: the hook read the set list already showed, so list and graph agree.</summary>
	public float? heardHook;
	public float heardHookConfidence;
	/// <summary>Choosing material for the player's own act: add a small fit-with-act read.</summary>
	public bool fitWithAct;
	public bool IsEar => source != PolarHearingSource.Studio;
	public bool WatchedAct => source == PolarHearingSource.Venue;
}

[Serializable]
public struct PolarBand {
	public float lo, hi;
	public float Center => (lo + hi) * .5f;
	public PolarBand(float low, float high) { lo = Math.Clamp(low, 0, 1); hi = Math.Clamp(high, lo, 1); }
}

/// <summary>Saved observational evidence. Contains estimated bands, never a truth profile.</summary>
[Serializable]
public sealed class PolarObservation {
	public string key, observerId, subjectId, kind, eventId, subjectVersion, perceptionVersion;
	public PolarEvidenceGate gate;
	public float demandError, identityError;
	public PolarBand[] axes;
	public PolarBand plasticity, reach, rigidity, ambition;
}

/// <summary>The renderer's complete input contract: observed estimates and player-known labels only.
/// No Record, SongComposition, SongProfile, ActProfile, MaterialFit or internal selection score.</summary>
public sealed class PolarComparisonRead {
	public string songTitle, actName, subjectLabel, evidenceLabel, explanation, resistance, arrangement;
	public PolarObservation reference, proposed, act;
	public PolarBand[] referenceFit, proposedFit;
	public bool mayResist, hasMarketEvidence, isRecorded;
	// Ear views: the song as the player heard it, the hook read, and (venue only) the act read.
	public PolarHearingSource source;
	public PolarObservation heard;
	public PolarBand hook;
	public string hookText, tightnessText;
	public bool showsAct, showsFit, fromSheetMusic;
}

public sealed class PolarPerceptionTable {
	public string Version { get; set; }
	public string Provenance { get; set; }
	public float DemandErrorWeak { get; set; }
	public float DemandErrorStrong { get; set; }
	public float IdentityErrorWeak { get; set; }
	public float IdentityErrorStrong { get; set; }
	public float MarketErrorWeak { get; set; }
	public float MarketErrorStrong { get; set; }
	public float HookErrorWeak { get; set; }
	public float HookErrorStrong { get; set; }
	public float BandWidth { get; set; }
	public float ArrangementDescriptionThreshold { get; set; }
	public Dictionary<string, float> GateScale { get; set; }
	public Dictionary<string, float> Standing { get; set; }
	public int MaximumObservations { get; set; }
}
