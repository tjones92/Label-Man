using Godot;

[GlobalClass]
public partial class Record : Resource {
	[ExportGroup("Identity")]
	[Export] public string recordId;
	[Export] public string title;
	[Export] public string artistName;
	[Export] public string artistId;
	[Export] public string labelId;
	[Export] public ReleaseFormat format = ReleaseFormat.Single;
	[Export] public Album album;
	[Export] public bool isPlayerOwned;
	[Export] public bool isNPC;
	// Release identity is assigned at creation and is durable independently of an
	// AlbumProject's later lifecycle. None is a compatibility sentinel for saves
	// made before release lanes existed.
	[Export] public ProjectRecordRole projectRole = ProjectRecordRole.None;
	[Export] public string albumProjectId;
	
	[ExportGroup("Musical Attributes")]
	[Export] public Genre primaryGenre;
	[Export] public Genre secondaryGenre;
	// Durable identities for Directive 5 saves and telemetry. Enums remain for Godot/type safety.
	[Export] public int genreSchemaVersion;
	[Export] public string primaryGenreId;
	[Export] public string secondaryGenreId;
	[Export] public string[] genreTagIds = System.Array.Empty<string>();
	[Export(PropertyHint.Range, "0,1")] public float hookStrength;
	[Export(PropertyHint.Range, "0,1")] public float productionQuality;
	[Export(PropertyHint.Range, "0,1")] public float originality;
	[Export(PropertyHint.Range, "0,1")] public float danceability;
	[Export(PropertyHint.Range, "0,1")] public float controversy;
	
	[ExportGroup("Release Info")]
	// GameDate is a struct, cannot be exported to Godot inspector natively
	public GameDate releaseDate; 
}

public enum ReleaseFormat { Single, Album, EP }
