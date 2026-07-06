using Godot;

[GlobalClass]
public partial class AlbumTrack : Resource {
	[Export] public string sourceRecordId;
	[Export] public string title;
	[Export] public Genre genre;
	[Export(PropertyHint.Range, "0,1")] public float quality;
	[Export] public bool isReleasedSingle;
	// GameDate is a struct and cannot be exported to the Godot inspector natively.
	// Keep the absolute source release date so archived snapshots continue to age.
	public GameDate releaseDate;
	// Read-only source metadata used by deterministic release priors. A value of 0
	// means the Single had not charted when this snapshot was taken.
	[Export] public int peakPosition;
}
