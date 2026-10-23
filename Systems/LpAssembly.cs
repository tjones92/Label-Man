using System.Collections.Generic;

/// <summary>
/// Release Judgement 5A (--lp-assembly, requires the slate): an LP is assembled from specific masters the label
/// controls and the new sessions it still needs, as an ordinary studio LP, a hit-led studio LP or an earned
/// greatest-hits package, instead of a compilation lottery over the act's last four singles. The slate's format
/// opportunity policy is unchanged. Off: no draws, no saved fields.
/// </summary>
public static class LpAssembly {
	public const string Switch = "--lp-assembly", GreatestHitsSwitch = "--lp-assembly-gh-hits=";
	// Provisional, to test against alternatives: the directive's three distinct relevant hits, and an honest package
	// length. A relevant hit is a controlled Top 40 single not already on a greatest-hits package.
	public static int GreatestHitsHits { get; internal set; } = 3;
	public const int RelevantHitPeak = 40, MinGreatestHitsTracks = 8, RecentWeeks = 52;
	public const int MaxRecentHits = 3, MaxOtherRecentSingles = 2, MaxShelvedOnStudioLp = 3, ShelfPerAct = 6;
	public const float NewSessionMultiplier = 2.4f;
}

public enum LpProposalKind { Ordinary, HitLed, GreatestHits }

/// <summary>Who controls an issued single's master now (follows sales and leases), and studio-LP reuse.</summary>
public sealed class LpMasterControl {
	public string LabelId;
	public bool FromAlbum;
	public int StudioLpUses;
}

/// <summary>A paid slate master that expired unissued. Its recording cost is sunk; an LP may use it once.</summary>
public sealed class ShelvedMaster {
	public Record Record;
	public string LabelId, ArtistId;
	public int ShelvedWeek;
}

/// <summary>The LP proposition chosen at preparation: the exact reused masters, the new sessions and their pricing.</summary>
public sealed class LpAssemblyPlan {
	public LpProposalKind Kind;
	public List<string> ReleasedMasterIds = new(), ShelvedMasterIds = new();
	public int TrackCount, NewSessions, RelevantHits, ControlledSingles;
	public float ProductionMultiplier, ProjectedNet, OrdinaryNet, HitLedNet, GreatestHitsNet;
	public bool HitLedFeasible, GreatestHitsFeasible;
	public string Reason;
}
