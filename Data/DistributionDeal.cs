using System;
using System.Collections.Generic;

public enum DealOrigin {
	LabelSought,
	DistributorCourted
}

public enum DealResolution {
	Signed,
	Exit,
	Renew,
	Absorb,
	DistributorCollapsed,
	ClientClosed
}

[Serializable]
public class DistributionDeal {
	public string distributorId;
	public float reachGranted;
	public string[] grantedRegions = Array.Empty<string>();
	public float marginSkim;
	public bool ownsMasters;
	public float advance;
	public float unrecoupedAdvance;
	public int signedWeek;
	public int termWeeks;
	public DealOrigin origin;

	/// <summary>
	/// Records this deal actually carries. A 1960s distribution agreement was struck
	/// for the record that was breaking regionally and then carried the label's output
	/// for the contract term; it did not retroactively put the back catalog into the
	/// distributor's network. Coverage is therefore the record whose regional breakout
	/// earned the deal plus everything released while the deal is active.
	/// </summary>
	public readonly HashSet<string> coveredRecordIds = new(StringComparer.Ordinal);

	public bool CoversRecord(string recordId) =>
		!string.IsNullOrEmpty(recordId) && coveredRecordIds.Contains(recordId);

	public void Cover(string recordId) {
		if (!string.IsNullOrEmpty(recordId)) coveredRecordIds.Add(recordId);
	}
}

public sealed class DistributionDealTelemetry {
	public DealResolution resolution;
	public DealOrigin origin;
	public string distributorId;
	public string distributorName;
	public string clientId;
	public string clientName;
	public float reachGranted;
	public float marginSkim;
	public bool ownsMasters;
	public float advance;
	public int signedWeek;
	public int termWeeks;
	public float dependency;
}

public sealed class IndependentDistributionTelemetry {
	public int week;
	public string labelId;
	public string labelName;
	public LabelTier labelTier;
	public string distributorId;
	public string distributorName;
	public string regionId;
	/// <summary>True when the label had proven a record here; false when it spread from a bordering market.</summary>
	public bool provenInRegion;
	public int coveredRegionCount;
	public float coveredMarketShare;
	public float ownedReachBefore;
	public float ownedReachAfter;
	public int houseClientCount;
	public int houseClientCapacity;
}

public sealed class DistributionOfferAttemptTelemetry {
	public int week;
	public int year;
	public string clientId;
	public string clientName;
	public LabelTier clientTier;
	public LabelPopulationOrigin clientOrigin;
	public int monthsActive;
	public float ownedReach;
	public float nationalReach;
	public float bestAnyRegionPeak;
	public float bestStrongRegionPeak;
	public float bestPersistentEvidenceQuality;
	public bool persistentRegionalEvidence;
	public bool legacyQualityAndCurrentSalesEvidence;
	public bool legacyNationalReachGate;
	public bool pushEvidence;
	public bool pushChancePassed;
	public bool pullChancePassed;
	public string outcome;
	public string distributorId;
}
