using System;

public enum DealOrigin {
	LabelSought,
	DistributorCourted
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
