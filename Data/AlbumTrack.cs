using Godot;

[GlobalClass]
public partial class AlbumTrack : Resource {
	[Export] public string sourceRecordId;
	[Export] public string title;
	[Export] public Genre genre;
	[Export(PropertyHint.Range, "0,1")] public float quality;
	[Export] public bool isReleasedSingle;
	// Read-only source metadata used by deterministic release priors. A value of 0
	// means the Single had not charted when this snapshot was taken.
	[Export] public int peakPosition;
}
