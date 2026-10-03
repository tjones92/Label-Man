using System;
using System.Collections.Generic;

public enum PolarEvidenceGate { FirstListen, FollowUp, Demo, Rehearsal, Playback }
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
	public float BandWidth { get; set; }
	public float ArrangementDescriptionThreshold { get; set; }
	public Dictionary<string, float> GateScale { get; set; }
	public Dictionary<string, float> Standing { get; set; }
	public int MaximumObservations { get; set; }
}
