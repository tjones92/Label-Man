using System;

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
