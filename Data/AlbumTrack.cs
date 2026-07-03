using Godot;

[GlobalClass]
public partial class AlbumTrack : Resource {
	[Export] public string sourceRecordId;
	[Export] public string title;
	[Export] public Genre genre;
	[Export(PropertyHint.Range, "0,1")] public float quality;
	[Export] public bool isReleasedSingle;
}
