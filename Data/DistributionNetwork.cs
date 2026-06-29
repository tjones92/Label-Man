using Godot;

[GlobalClass]
public partial class DistributionNetwork : Resource {
	[Export(PropertyHint.Range, "0,1")] public float difficulty;
	[Export] public int recordStoreCount;
	[Export] public int departmentStoreCount;
	[Export(PropertyHint.Range, "0,1")] public float inventoryDepth;
	[Export] public bool hasIndieDistribution;
	[Export] public bool hasOneStopDistributors;
	
	public DistributionNetwork() {}
}
