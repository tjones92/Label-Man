using Godot;

[GlobalClass]
public partial class MarketCity : Resource {
	[ExportGroup("Identity")]
	[Export] public string cityId;
	[Export] public string name;
	[Export] public Vector2 mapCoords;
	[Export] public string parentRegionId;
	[Export] public bool isRegionalHub;
	[Export] public int distributionTier;

	[ExportGroup("Distribution")]
	[Export] public DistributionNetwork distribution;

	public MarketCity() {
		distribution = new DistributionNetwork();
	}
}
