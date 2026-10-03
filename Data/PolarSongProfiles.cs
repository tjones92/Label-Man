using System;

// Axis ordering is a persistence/display contract, not a tuning choice (Spec A §3).
public enum SongAxis { VocalPower, VocalNuance, Musicianship, Ensemble, LyricDelivery, StudioCraft, Toughness, Sophistication, Sincerity, Maturity }
[Serializable]
public sealed class SongProfile {
	public const int DemandCount = 6, IdentityCount = 4, AxisCount = DemandCount + IdentityCount;
	public float[] axes = new float[AxisCount];
	public float plasticity;
	public float dominantDemand;
	public bool inferredTaxonomy;
	public string mappingVersion;
	public SongArchetype archetype;
	public float this[SongAxis axis] { get => axes[(int)axis]; set => axes[(int)axis] = value; }
	public SongProfile Copy() => new() { axes = (float[])axes.Clone(), plasticity = plasticity, dominantDemand = dominantDemand,
		inferredTaxonomy = inferredTaxonomy, mappingVersion = mappingVersion, archetype = archetype };
}
public sealed class ActProfile {
	public float[] axes = new float[SongProfile.AxisCount];
	public float interpretiveReach, identityRigidity;
	public bool inferredCapability = true;
	public bool inferredSession;
	public bool hasVocalist;
	public string mappingVersion;
	public SongVocalApproach vocalApproach;
	public float this[SongAxis axis] { get => axes[(int)axis]; set => axes[(int)axis] = value; }
}
public sealed class PolarSessionContext { public float producerCraft, studioCraft; }
public sealed class MarketTasteSnapshot {
	public float[] identity = new float[SongProfile.IdentityCount];
	public int asOfWeek;
	public int observations;
}
public readonly struct MaterialFit {
	public readonly float Capability, Identity, Moment, Deficit, Stretch, ReferenceIdentityDistance;
	public readonly SongAxis WorstAxis;
	public readonly bool HasMarketEvidence;
	public MaterialFit(float capability, float identity, float moment, float deficit, SongAxis worst, float stretch, float referenceDistance, bool marketEvidence) {
		Capability = capability; Identity = identity; Moment = moment; Deficit = deficit; WorstAxis = worst;
		Stretch = stretch; ReferenceIdentityDistance = referenceDistance; HasMarketEvidence = marketEvidence;
	}
}
public sealed class PolarArrangementProposal {
	public string plannedMasterId, songId, parentRecordingId, mappingVersion;
	public SongTaxonomy taxonomy;
	public SongProfile referenceProfile, proposedProfile, realizedProfile;
	public MaterialFit fit;
	public bool changedArchetype;
	public bool usedFallback;
}
