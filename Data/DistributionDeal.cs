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
