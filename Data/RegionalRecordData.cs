// Scripts/Data/RegionalRecordData.cs

using System;

public enum RegionalBreakoutStage {
	None,
	LocalTraction,
	NeighboringMarketTest,
	RegionalBreakout,
	NationalCrossoverCandidate
}

[Serializable]
public class RegionalRecordData {
	public string regionId;
	
	// Awareness & Sentiment
	public float awareness;      // 0-1: How many people in this region know about the song
	public float sentiment;      // -1 to 1: How much they like it
	// A5: a linked LP's regional awareness without the promo's launch bonus (null otherwise).
	[System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
	public float? promoShadowAwareness;

	// Media Presence
	public float radioPlay;      // 0-1: Current radio airplay level (the final combined value the
	                             // pipeline consumes; produced by StationNetwork aggregation)
	// The aggregate-station ("tail") contribution to radioPlay (radio design doc a). radioPlay is
	// Lerp(tailRadioPlay, reporterRadioPlay, REPORTER_PANEL_WEIGHT). In the Phase-2a plumbing swap
	// tailAccess=1 and the panel weight is 0, so radioPlay == tailRadioPlay == the former formula
	// (proved byte-identical before any reporter contribution is dialled in).
	public float tailRadioPlay;
	// Big-5 handoff probe: the part of radioPlay the reporter panel put there, this week's blend plus what the
	// tail carried of earlier weeks' blends. Written only under --station-regional-support-panel-free; not saved.
	[System.Text.Json.Serialization.JsonIgnore]
	public float panelExcessRadio;
	// This week's reporter panel rotation, for --station-regional-support-panel-linear's chart payout; not saved.
	[System.Text.Json.Serialization.JsonIgnore]
	public float panelRadioPlay;
	public float jukeboxPlay;    // 0-1: Jukebox presence
	// A 1960s Top 40 playlist was a list of thirty or forty slots, re-cut weekly by a programme
	// director reading local sales reports. A record did not fade off it; it was dropped, and once
	// dropped it moved to gold rotation rather than back onto the survey. This is that latch, held
	// per region because WABC and KHJ cut a record in different weeks.
	//
	// The latch is one-way BY DESIGN, not for convenience: a drop keyed to a noisy weekly sales
	// ratio would otherwise drop and re-add the same record as its sales wobbled, and returns to
	// number one are already the single largest chart defect at 24-28% against a historical 4-5%
	// (handoff 12.4q). A re-add is exactly the wrong direction, so there is no re-add at all.
	public bool stationsDropped;
	// Record age in weeks when this region's stations cut it, or -1. Telemetry only.
	public int stationDropAge = -1;
	
	// Distribution
	public int unitsInStores;    // Physical copies available
	public int unitsBackordered; // Demand that couldn't be met
	
	// Sales Tracking
	public int unitsSoldThisWeek;
	public int unitsSoldTotal;
	// Directive 5 acceptance is evaluated during the sales pass and reused by
	// the later radio pass. These values are transient, week-local simulation
	// state; they do not change audit output or the acceptance calculation.
	public int genreMarketAcceptanceWeek = int.MinValue;
	public float genreDemandAcceptanceThisWeek = 1f;
	public float genreRadioOpportunityThisWeek = 1f;
	// Read-only demand-pass snapshots. The later regional radio pass mutates
	// awareness and radioPlay, so audit telemetry must retain the state actually
	// used to calculate this week's sales.
	public float salesRecordAwarenessThisWeek;
	public float salesRegionalAwarenessThisWeek;
	public float salesEffectiveAwarenessThisWeek;
	public float salesRadioHeatThisWeek;
	public float salesRegionalRadioPlayThisWeek;
	// Derived before the live demand pass; the national campaign's unserved audience allocated to this market.
	public float campaignAwarenessThisWeek;
	public float previousRawDemand;
	public float rawDemandThisWeek;
	// Enabled Single demand-stage audit. These values are captured before supply
	// clearing so raw demand can be reconstructed independently.
	public float demandPotentialAudience, demandBaselineAwareness, demandEarnedDiscoveryExposure, demandAwareBuyers;
	public float demandIntrinsicQualityFactor, demandAcceptanceFactor, demandFormatFactor, demandIntrinsicConversionRate;
	public float demandChartSignal, demandMomentumSignal, demandRadioSignal;
	// Live common-market clearing keeps physical fulfillment distinct from demand
	// displaced by simultaneous competing records.  These are week-local values.
	public int serviceableIntentThisWeek;
	public int storeCapacityThisWeek;
	public int marketDisplacedDemandThisWeek;
	// Population-complete Album realization snapshots. These are assigned during
	// the demand pass and consumed only by the enabled settlement audit.
	public float albumBuyerPoolThisWeek;
	public float albumAwarenessThisWeek;
	public float albumObservedPenetrationThisWeek;
	public float albumEffectivePenetrationThisWeek;
	public float albumExhaustionThisWeek;
	public float albumCatalogDecayMultiplierThisWeek;
	public float albumFormatTiltThisWeek;
	public float albumConversionThisWeek;
	public float albumRawDemandBeforeCannibalizationThisWeek;
	public float albumRawDemandAfterCannibalizationThisWeek;
	public int albumUnitsInStoresBeforeSaleThisWeek;
	// Live Genre-Market-V2 Albums retain their greatest observed regional
	// penetration so buyer-pool growth cannot rejuvenate catalog exhaustion.
	public float albumPeakEffectivePenetration;
	// Immutable settlement inputs captured after the two clearing stages.
	public int localClearedThisWeek;
	public int spilloverClearedThisWeek;
	// Rack-jobber channel: this week's rack units (week-local), the rack exhaustion ledger and the rack tier
	// (RackJobberChannel.Tier*). The last two are saved only when nonzero.
	public int rackUnitsThisWeek;
	public int rackUnitsTotal;
	// Accounting stays in racks; these purchases still exhaust the original store buyer cohort.
	public int rackStoreBuyerUnitsTotal;
	public int rackTier;
	public float salesVelocity;
	public int sustainedGrowthWeeks;
	public int tractionWeeks;
	public int collapseWeeks;

	// Local lifecycle (regional audience design 3a). A region has its own arrival and its own best
	// week, so its clocks can run apart from the national ones. Ages are record ages
	// (weeksSinceRelease), like stationDropAge, so they need no chart-week bookkeeping on load.
	// -1 = the record is not yet in play here.
	public int localIntroAge = -1;
	public int localPeakUnits;
	public int localPeakAge = -1;
	// This week's rank on the region's single sales list (1 = best seller), 0 if unranked or not computed.
	public int localSalesRank;
	// Regional sync §9s: the record age from which its commercial connection is realized here. 0 (the default, so albums,
	// player records, older saves and switched-off runs) is from launch; RegionalFitExposure.NotExposed until a spread
	// rule reaches the region.
	public int fitExposureAge;
	public string fitExposureCause;
	// Regional arrival (RegionalSyncDesign 5a). Only ever set while RegionalAudience.RegionalArrival is on, so the
	// default (false) means "in play" for albums, player records, older saves and switched-off runs. While waiting,
	// the promotion seed drawn for this region is held in the pending fields and lands on arrival. With sparse
	// retail access, unitsInStores can already contain a finite shop allocation and pendingLaunchStock is zero.
	// Campaign focus may redirect pendingLaunchAwareness into served markets, leaving no duplicate deposit here.
	public bool awaitingArrival;
	public int arrivalAge = -1;
	public float pendingLaunchAwareness, pendingLaunchRadio;
	public int pendingLaunchStock;

	/// <summary>Weeks this region has had the record in play, counting the arrival week as 1; 0 before it arrives.</summary>
	public int LocalAge(int weeksSinceRelease) => localIntroAge < 0 ? 0 : Math.Max(1, weeksSinceRelease - localIntroAge + 1);

	/// <summary>Weeks since this region's best week, the regional twin of weeksSincePeakUnits; 0 before any sale.</summary>
	public int WeeksSinceLocalPeak(int weeksSinceRelease) => localPeakAge < 0 ? 0 : Math.Max(0, weeksSinceRelease - localPeakAge);

	// Demand-led discovery state. This is intentionally independent of restocking.
	public float breakoutScore;
	public float peakBreakoutScore;
	public RegionalBreakoutStage breakoutStage;
	public float neighboringMarketTestStrength;
	public string breakoutSourceRegionId;
	// Record age of the last week this region was a regional hit by RegionalHitDealGate's bar, or -1. Written
	// only while that switch is on; the switched-off deal gate reads peakBreakoutScore instead.
	public int dealEvidenceAge = -1;
	public float breakoutVolumeInput;
	public float breakoutVelocityInput;
	public float breakoutAudienceInput;
	public float breakoutMediaInput;
	public float breakoutGenreFitInput;
	public float breakoutQualityInput;
	public float breakoutUnmetDemandInput;
	public float breakoutVisibilityMultiplier = 0.4f;
	public float breakoutAwarenessGain;
	public float breakoutRadioGain;
	public float breakoutWordOfMouthGain;

	// Audit-only snapshot for the first three never-charted release weeks.
	public bool breakoutDiagnosticObserved;
	public int breakoutDiagnosticAge;
	public int breakoutWeekStartStock;
	public int breakoutPreRestockStock;
	public float breakoutRawSales;
	public float breakoutAwareBuyers;
	public float breakoutConversionRate;
	public int breakoutBackordersBeforeRestock;
	public bool breakoutTriggered;
	public int breakoutRequestedRestock;
	public int breakoutAppliedRestock;
	public int breakoutMaxCapacity;
	public bool breakoutCapacityCapped;
	
	/// <summary>Parameterless ctor for the full-world save's deserializer (System.Text.Json). Every field is
	/// set from the snapshot; field initializers (e.g. breakoutVisibilityMultiplier) still run.</summary>
	public RegionalRecordData() { }

	public RegionalRecordData(string regionId) {
		this.regionId = regionId;
		awareness = 0f;
		sentiment = 0f;
		radioPlay = 0f;
		jukeboxPlay = 0f;
		stationsDropped = false;
		stationDropAge = -1;
		localIntroAge = -1;
		localPeakAge = -1;
		unitsInStores = 0;
		unitsBackordered = 0;
		unitsSoldThisWeek = 0;
		unitsSoldTotal = 0;
		breakoutStage = RegionalBreakoutStage.None;
	}
}
