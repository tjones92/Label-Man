// Scripts/Data/RecordRuntimeData.cs

using System.Collections.Generic;

[System.Serializable]
public class RecordRuntimeData {
	public Record baseRecord;
	public AlbumRuntimeData albumRuntime;
	// The operating owner may change after an acquisition, but the label/imprint
	// printed on a released record does not change retroactively. Keep that
	// release identity so chart-breadth audits do not erase an acquired label's
	// first chart appearance when AbsorbLabel transfers the active catalogue.
	public string releaseLabelId;
	
	// === CHART POSITION TRACKING ===
	public int currentPosition;
	public int lastWeekPosition;
	public int peakPosition;
	public int weeksOnChart;
	public int weeksSinceRelease;
	public int lastChartedAge = -1;
	public int lastSalesAboveRetirementFloorAge = -1;
	public int weeksInTopTen;
	public bool artistChartEntryCredited;
	public bool artistTop40Credited;
	public bool artistTop10Credited;
	public bool artistNumberOneCredited;
	public bool artistChartRunCompleted;
	// Captures the artist's owning contract when this record was released. A
	// later contract must not inherit probation evidence from this chart run.
	public int artistContractSequenceAtRelease = -1;
	
	// === MOMENTUM INDICATORS ===
	public bool isBullet;
	public bool isAnchor;
	public float overallMomentum;
	
	// === SALES TRACKING ===
	public int unitsThisWeek;
	public int unitsPreviousWeek;
	public int totalUnitsSold;
	public float lifetimeLabelNet;
	public float sunkProductionCost;
	public bool revenueMemoryEligible;
	// Immutable-at-launch context for normalized, opportunity-relative memory.
	public float releaseTimeExpectedNet;
	public float releaseTimeOpportunityScale;
	public int releaseMemoryWeek;
	public ProjectRecordRole projectRole;
	public string albumProjectId;
	// Frozen, ex-ante Single cohort opportunity captured at release. These are
	// never recomputed from later chart, revenue, or retirement outcomes.
	public float enabledOpportunityMass;
	public float acceptedOpportunityMass;
	public float cohortOpportunityNormalizer = 1f;
	public bool cohortOpportunityColdStartFallback;
	public string cohortOpportunityNormalizerSource = "Legacy";
	public string linkedPromoSingleId;
	public float cannibalizationSuppression;
	public double rawAlbumDemandBeforeCannibalization;
	public double suppressedAlbumDemand;
	public bool linkedPromoRuntimeActive;
	public float linkedPromoSingleHeat;
	public float albumSubstitutionPropensity;
	public double albumDemandWithActiveLinkedPromo;
	public double albumDemandWithInactiveLinkedPromo;
	public double albumDemandWeightedSingleHeat;
	public double albumDemandWeightedSubstitutionPropensity;
	public double albumDemandWeightedSuppression;
	
	// === SIMULATION FORCES ===
	public float awareness;           // 0-1: Do people know this song exists?
	public float momentum;            // -1 to +1: Is it trending up or down?
	public float saturation;          // 0-1: What % of potential buyers already own it?
	public float radioHeat;           // 0-1: How much are stations playing it?
	public float wordOfMouth;         // 0-1: Are people talking about it?
	
	// === ARTIST FACTORS ===
	public float artistHeat;          // 0-1: Is this artist currently hot?
	public int artistPreviousHits;    // How many top 40 hits has this artist had?
	
	// === LABEL PUSH ===
	public float currentLabelPush;    // 0-1: How hard is the label pushing THIS WEEK
	public float totalLabelInvestment;// Running total of label push

	// === AUDIT TELEMETRY (write-only from launch paths) ===
	public float initialLaunchAwareness;
	public int initialLaunchStock;
	public CareerState launchCareerState;
	public float perceivedQualityMultiplier = 1f;

	// Aggregate breakout/distributor-facing seam. A future deal system can read
	// these outputs without participating in demand creation.
	public int regionalBreakoutCount;
	public int neighboringMarketTestCount;
	public float crossoverCandidateStrength;
	public float peakRegionalBreakoutStrength;
	public float sustainedSalesVelocity;
	public int unmetRegionalDemand;
	public int coveredRegionCount;
	
	// === DERIVED METRICS ===
	public float peakMomentum;        // Highest momentum achieved
	public int weeksPositive;         // Consecutive weeks of positive momentum
	public int weeksNegative;         // Consecutive weeks of negative momentum
	
	// === REGIONAL DATA ===
	public Dictionary<string, RegionalRecordData> regionalData = new Dictionary<string, RegionalRecordData>();
	
	// === AWARDS ===
	public bool isGrammyNominated;
	public bool isGrammyWinner;
	public int weeksOfGrammyBump;
	
	// === CONSTRUCTOR ===
	public RecordRuntimeData(Record record) {
		baseRecord = record;
		releaseLabelId = record?.labelId;
		projectRole = record?.projectRole == ProjectRecordRole.None ? ProjectRecordRole.ExternalOrLegacy : record.projectRole;
		albumProjectId = record?.albumProjectId;
		if (record.format == ReleaseFormat.Album && record.album != null) {
			albumRuntime = new AlbumRuntimeData(record.album, record.releaseDate.year);
		}
		
		// Chart tracking
		currentPosition = 0;
		lastWeekPosition = 0;
		peakPosition = 0;
		weeksOnChart = 0;
		weeksSinceRelease = 0;
		peakPosition = 0;  // 0 = never charted
		
		// Momentum
		isBullet = false;
		isAnchor = false;
		overallMomentum = 0f;
		
		// Sales
		unitsThisWeek = 0;
		unitsPreviousWeek = 0;
		totalUnitsSold = 0;
		lifetimeLabelNet = 0f;
		sunkProductionCost = 0f;
		revenueMemoryEligible = false;
		
		// Simulation forces - all start at zero
		awareness = 0f;
		momentum = 0f;
		saturation = 0f;
		radioHeat = 0f;
		wordOfMouth = 0f;
		
		// Artist factors
		artistHeat = 0f;
		artistPreviousHits = 0;
		
		// Label push
		currentLabelPush = 0f;
		totalLabelInvestment = 0f;
		
		// Derived metrics
		peakMomentum = 0f;
		weeksPositive = 0;
		weeksNegative = 0;
		
		// Awards
		isGrammyNominated = false;
		isGrammyWinner = false;
		weeksOfGrammyBump = 0;
	}
	
	// === HELPER METHODS ===
	
	public float GetAwardMultiplier() {
		if (isGrammyWinner && weeksOfGrammyBump > 0) return 1.4f;
		if (isGrammyNominated) return 1.15f;
		return 1f;
	}
	
	public float GetQuality() {
		if (baseRecord.format == ReleaseFormat.Album && baseRecord.album != null) return baseRecord.album.pooledAppeal;
		return (baseRecord.hookStrength * 0.5f) + 
			   (baseRecord.productionQuality * 0.3f) + 
			   (baseRecord.danceability * 0.2f);
	}
}
