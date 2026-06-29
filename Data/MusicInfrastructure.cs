using Godot;

[GlobalClass]
public partial class MusicInfrastructure : Resource {
	[Export] public int recordingStudioCount;
	[Export(PropertyHint.Range, "0,1")] public float studioQuality;
	[Export] public bool hasSignatureSound;
	[Export] public string signatureSoundDescription;
	[Export] public int localLabelCount;
	[Export] public bool hasMajorLabelPresence;
	[Export(PropertyHint.Range, "0,1")] public float talentPool;
	[Export(PropertyHint.Range, "0,1")] public float talentDevelopment;
	[Export] public int clubCount;
	[Export] public int theaterCount;
	[Export] public bool hasChitlinCircuitVenues;
	
	public MusicInfrastructure() {}
}
