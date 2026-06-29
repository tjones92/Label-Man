using Godot;

[GlobalClass]
public partial class GenrePreference : Resource {
	[Export] public Genre genre;
	[Export(PropertyHint.Range, "0,1")] public float baseAcceptance;
	[Export(PropertyHint.Range, "-1,1")] public float affinity;
	[Export] public bool hasLocalScene;
	[Export(PropertyHint.Range, "-0.1,0.1")] public float yearlyDrift;
	
	public GenrePreference() {}
}
