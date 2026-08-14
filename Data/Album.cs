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
	// Layer 1 of the cultural stack: what the record IS, fixed the day it was pressed and
	// never touched again. Stored rather than recomputed so the landmark rule can run on the
	// weekly album chart without a label lookup, and so that "a record nobody bought has
	// exactly the merit it had the day it was pressed" is true by construction.
	[Export(PropertyHint.Range, "0,1")] public float artisticMerit;
	// Album-as-a-body-of-work, distinct from thematicCohesion's concept-album axis. See
	// AlbumModel.GetAlbumIntegrity: this is what makes a Rubber Soul, and unlike cohesion it
	// is reachable in any year because it is a fact about the tracks, not an era ceiling.
	[Export(PropertyHint.Range, "0,1")] public float bodyOfWork;
	[Export] public bool isStereo;

	// Set only on externally-originated soundtrack/cast albums (albumFormat == Soundtrack).
	// Null for every artist-originated album. Carries the box-office demand shape and licensing
	// economics minted by ExternalMediaService; read by AlbumSimulator's soundtrack demand branch.
	// Runtime-only reference (not [Export]) -- soundtracks are never serialized to disk.
	public ExternalMediaProfile externalMedia;

	public AlbumTrack[] GetAllTracks() => trackRefs.Concat(nonSingleTracks).ToArray();
}

public enum AlbumFormat { Standard, Compilation, Concept, Live, Soundtrack, EP }
