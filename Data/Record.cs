using Godot;

[GlobalClass]
public partial class Record : Resource {
	[ExportGroup("Identity")]
	[Export] public string recordId;
	[Export] public string title;
	[Export] public string artistName;
	[Export] public string artistId;
	[Export] public string labelId;
	[Export] public bool isPlayerOwned;
	[Export] public bool isNPC;
	
	[ExportGroup("Musical Attributes")]
	[Export] public Genre primaryGenre;
	[Export] public Genre secondaryGenre;
	[Export(PropertyHint.Range, "0,1")] public float hookStrength;
	[Export(PropertyHint.Range, "0,1")] public float productionQuality;
	[Export(PropertyHint.Range, "0,1")] public float originality;
	[Export(PropertyHint.Range, "0,1")] public float danceability;
	[Export(PropertyHint.Range, "0,1")] public float controversy;
	
	[ExportGroup("Release Info")]
	// GameDate is a struct, cannot be exported to Godot inspector natively
	public GameDate releaseDate; 
}
