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
	
	// Distribution
	public int unitsInStores;    // Physical copies available
	public int unitsBackordered; // Demand that couldn't be met
	
	// Sales Tracking
	public int unitsSoldThisWeek;
	public int unitsSoldTotal;
	public float previousRawDemand;
	public float rawDemandThisWeek;
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
		unitsInStores = 0;
		unitsBackordered = 0;
		unitsSoldThisWeek = 0;
		unitsSoldTotal = 0;
		breakoutStage = RegionalBreakoutStage.None;
	}
}
