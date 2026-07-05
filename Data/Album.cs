using System.Linq;
using Godot;

[GlobalClass]
public partial class Album : Resource {
	[ExportGroup("Identity")]
	[Export] public string albumId;
	[Export] public AlbumFormat albumFormat = AlbumFormat.Standard;

	[ExportGroup("Composition")]
	[Export] public AlbumTrack[] trackRefs = System.Array.Empty<AlbumTrack>();
	// Index-aligned with trackRefs. Captured when the Album is assembled so audit
	// output never has to re-derive freshness from a later counter state.
	[Export] public float[] trackRefFreshnessApplied = System.Array.Empty<float>();
	[Export] public int[] trackRefCompUsesAtGeneration = System.Array.Empty<int>();
	[Export] public AlbumTrack[] nonSingleTracks = System.Array.Empty<AlbumTrack>();
	[Export] public string[] leadSingleIds = System.Array.Empty<string>();
	[Export] public float runtimeMinutes;

	[ExportGroup("Appeal")]
	[Export(PropertyHint.Range, "0,1")] public float pooledAppeal;
	[Export(PropertyHint.Range, "0,1")] public float thematicCohesion;
	[Export(PropertyHint.Range, "0,1")] public float packaging;
	[Export] public bool isStereo;

	public AlbumTrack[] GetAllTracks() => trackRefs.Concat(nonSingleTracks).ToArray();
}

public enum AlbumFormat { Standard, Compilation, Concept, Live, Soundtrack, EP }
