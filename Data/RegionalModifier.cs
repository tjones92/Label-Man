using Godot;

[GlobalClass]
public partial class RegionalModifier : Resource {
	[Export] public string modifierId;
	[Export] public string description;
	[Export] public ModifierType type;
	[Export] public float value;
	[Export] public int startYear;
	[Export] public int endYear;
	
	// Required empty constructor for Godot Resources
	public RegionalModifier() {}
}

public enum ModifierType {
	GenreBoost, GenrePenalty, RadioBoost, SalesMultiplier, TrendSpeed, IntegrationBoost, PayolaResistance
}
