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
	
	// Media Presence
	public float radioPlay;      // 0-1: Current radio airplay level
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
	public float salesVelocity;
	public int sustainedGrowthWeeks;
	public int tractionWeeks;
	public int collapseWeeks;

	// Demand-led discovery state. This is intentionally independent of restocking.
	public float breakoutScore;
	public float peakBreakoutScore;
	public RegionalBreakoutStage breakoutStage;
	public float neighboringMarketTestStrength;
	public string breakoutSourceRegionId;
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
	
	public RegionalRecordData(string regionId) {
		this.regionId = regionId;
		awareness = 0f;
		sentiment = 0f;
		radioPlay = 0f;
		jukeboxPlay = 0f;
		stationsDropped = false;
		stationDropAge = -1;
		unitsInStores = 0;
		unitsBackordered = 0;
		unitsSoldThisWeek = 0;
		unitsSoldTotal = 0;
		breakoutStage = RegionalBreakoutStage.None;
	}
}
