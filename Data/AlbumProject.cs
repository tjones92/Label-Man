using System.Collections.Generic;

public enum ReleaseStrategy { OrphanSingle, AlbumStandalone, AlbumWithPromo }
public enum AlbumProjectTerminalState { Released, Cancelled, PendingAtAuditEnd }
public enum ProjectRecordRole { None, OrphanSingle, StandaloneAlbum, PromoSingle, LinkedAlbum, ExternalOrLegacy }
public enum ProjectOutcomeState { None, Pending, Retired, RedirectedToSingle, FoldedToAlbum }

public sealed class AlbumProject {
	public string projectId;
	public long creationSequence;
	public string originalLabelId;
	public string currentLabelId;
	public LabelTier tierAtSchedule;
	public string artistId;
	public Genre genre;
	public CareerState careerStateAtSchedule;
	public CareerState careerStateBeforeDropAtSchedule;
	public CareerState contractEntryCareerStateAtSchedule;
	public int contractSequenceAtSchedule;
	public int contractStartWeekAtSchedule = -1;
	public int scheduledWeek;
	public GameDate scheduledDate;
	public int dropWeek;
	public GameDate dropDate;
	public ReleaseStrategy strategy;
	public Record albumRecord;
	public Record promoSingleRecord;
	public string promoSingleId;
	public AlbumProjectTerminalState terminalState = AlbumProjectTerminalState.PendingAtAuditEnd;
	public int transferCount;
	public bool wasTransferred;
	public float albumProductionCost;
	public float promoProductionCost;
	public PromotionSnapshot albumPromotionSnapshot;
	public float albumMarketingBudgetPlanned;
	public float? heldPromoOutcome;
	public float? heldAlbumOutcome;
	public ProjectOutcomeState promoOutcomeState;
	public ProjectOutcomeState albumOutcomeState;
	public bool albumMemoryFolded;
	public int promoPeakAtDrop;
	public float promoPeakScore;
	public float synergyAwarenessApplied;
	public float synergyStockMultiplier = 1f;
	public bool albumRetired;
	public bool promoRetired;
	public float? projectRealizedNet;
	// The deterministic, pre-memory Album prior is the release-time comparison
	// baseline. Keep it with delayed projects so linked Albums do not fall back to
	// the production-cost-only placeholder when they drop.
	public float releaseTimeAlbumExpectedNet;
	public float projectedAlbumNet;
	public float projectedPromoSingleNet;
	public float projectedProjectNet;
	public double rawDemandBeforeCannibalization;
	public double suppressedDemand;
	public double demandWithActiveLinkedPromo;
	public double demandWithInactiveLinkedPromo;
	public double demandWeightedSingleHeat;
	public double demandWeightedSubstitutionPropensity;
	public double demandWeightedSuppression;
	public float initialLaunchAwareness;
	public int initialLaunchStock;
}

public sealed class PromotionSnapshot {
	public CareerState careerState;
	public float artistAwareness;
	public float perceivedQualityMultiplier;
	public readonly List<RegionalPromotionSnapshot> regions = new();
}

public sealed class RegionalPromotionSnapshot {
	public string regionId;
	public float awarenessRandom;
	public float radioRandom;
	public float sentimentRandom;
}
