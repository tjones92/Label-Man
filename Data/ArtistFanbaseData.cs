using System.Collections.Generic;
using System.Text.Json.Serialization;

// Audience equivalents, not identified people or cumulative units. Sparse regional/segment cohorts.
public sealed class ArtistFanbaseData {
	public Dictionary<string, Dictionary<string, double>> regions = new();
	public int lastDecayWeek;
	public int lastSettlementWeek = -1;
	public double openingHeads, recruitedHeads, departedHeads;
	public double newHeadsThisWeek, returningPurchasesThisWeek;
}

// A title can sell once to each member of its launch cohort. Its own success cannot refill that cohort.
public sealed class FanReleaseAudience {
	public int launchWeek;
	public Dictionary<string, FanReleaseRegion> regions = new();
}

public sealed class FanReleaseRegion {
	public Dictionary<string, double> initial = new();
	public Dictionary<string, double> purchased = new();
	[JsonIgnore] public Dictionary<string, double> pending = new();
	[JsonIgnore] public double anonymousRaw, fanRaw, clearedFans;
}
