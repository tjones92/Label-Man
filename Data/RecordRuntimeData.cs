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
	// Separate from artistChartRunCompleted on purpose. A record that CHARTED has the flag
	// above set by ArtistManager.OnRecordLeftChart before RosterManager ever sees it, and is
	// then handed to RosterManager a second time when the record retires -- so the flag above
	// answers "has the commercial outcome been credited", which is a different question from
	// "has the critical and cultural read been taken". Sharing one flag between the two meant
	// the narrative reads only ever ran on records that never charted.
	public bool culturalRunCompleted;
	// A record becomes a landmark ONCE, at the moment it is recognised -- which is while it is
	// climbing, not when it finally falls off the chart a year and a half later.
	public bool landmarkPublished;
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
	// Running maximum, not the eventual peak: it equals unitsThisWeek all the way up the climb and
	// only starts to exceed it once the record turns over, so unitsThisWeek/peakWeeklyUnits is a
	// clean "how far past its peak is this record" signal that stays neutral during the rise.
	//
	// Consumed by the station drop (ChartSimulator.GetStationDropChance): a programme director read
	// the local sales reports and cut a record once it was visibly slipping, so the record's own peak
	// is the reference the decision was actually made against.
	public int peakWeeklyUnits;
	// Weeks since peakWeeklyUnits was last raised: zero while the record is still setting new highs,
	// counting up once it turns over. This is the record's own clock, which is why the radio fatigue
	// term is keyed to it rather than to weeksSinceRelease -- a fixed week-8 clock started fatiguing
	// a hit before it peaked (the sales peak is now week 9) and let a marginal record that peaked at
	// week 4 keep full rotation for five weeks after it was finished.
	public int weeksSincePeakUnits;
	// Share of the national radio panel -- regions weighted by radio reach x population, the same
	// weighting CalculateChartPoints pays airplay on -- whose stations still carry the record.
	// Starts at 1 and only ever falls, because a station drop is latched. Derived state, recomputed
	// each week by the regional radio pass; kept here so telemetry can read the drop without
	// re-walking seven regions, and NOT read back by any mechanic.
	public float radioPanelShare = 1f;
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
	// This week's survey draw. Billboard did not count units before 1973 -- it polled about 110
	// outlets by hand, so the published chart was a small, coarsely graded SAMPLE of popularity and
	// not a census of it. Drawn once per record per week in ChartManager and stored here rather than
	// computed inside CalculateChartPoints, because that method is called from five sites including
	// the audit telemetry and a redraw would let the telemetry disagree with the ranking it reports.
	public float surveySampleThisWeek = 1f;
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
		peakWeeklyUnits = 0;
		weeksSincePeakUnits = 0;
		radioPanelShare = 1f;
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
