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

	// Publishing & Cover-Song layer (Phase 0). Song identity + credit snapshot beneath the master.
	// A Record is a performance; songId points at the underlying SongComposition in
	// CompositionCatalogService. See SimTools/PublishingCoverSongDirective.md.
	[ExportGroup("Composition / Publishing")]
	[Export] public string songId;
	[Export] public SongMaterialSource songSource = SongMaterialSource.Unknown;
	[Export] public bool isCover;
	[Export] public string originalRecordId;
	[Export] public string originalArtistId;
	[Export] public string publisherId;
	[Export] public string publishingControllerLabelId;
	// The artist who controls the composition's publishing (an artist-owned song). Carried onto covers
	// so a cover of an artist-owned hit pays the WRITER, not the covering performer (Phase 3b goldmine).
	[Export] public string publishingControllerArtistId;
	[Export] public PublishingControlType publishingControl = PublishingControlType.Unknown;
	[Export] public string[] songwriterIds = System.Array.Empty<string>();
	[Export] public string[] songwriterNames = System.Array.Empty<string>();
	// Godot cannot export a custom enum array (GD0102); kept as a plain field.
	public WriterEntityType[] songwriterTypes = System.Array.Empty<WriterEntityType>();
	[Export] public float[] songwriterShares = System.Array.Empty<float>();
	// Composition facts snapshotted at release time.
	[Export(PropertyHint.Range, "0,1")] public float compositionQuality;
	[Export(PropertyHint.Range, "0,1")] public float compositionHook;
	[Export(PropertyHint.Range, "0,1")] public float lyricQuality;
	[Export(PropertyHint.Range, "0,1")] public float songFamiliarityAtRelease;
	[Export(PropertyHint.Range, "0,1")] public float standardDurability;
	[Export(PropertyHint.Range, "0,1")] public float arrangementOriginality;
	[Export(PropertyHint.Range, "0,1")] public float professionalPolish;

	// Player-only (PublishingCoverSongDirective Part II, §II.0): a 45's B-side is never its own market
	// record -- PlayerDesk drops the B-side's Record object once it ships -- so its song-control facts
	// are snapshotted here, onto the A-side, at release time. Lets MechanicalRoyaltyService charge the
	// flip's own 2c/copy for the life of the pressing without keeping a whole second Record alive.
	[Export] public string bSideSongId;
	[Export] public PublishingControlType bSidePublishingControl = PublishingControlType.Unknown;
	[Export] public string bSidePublishingControllerLabelId;
	[Export] public string bSidePublishingControllerArtistId;

	// Dealer-margin-and-flip directive §3.1: the flip is a complete second identity riding on one
	// disc. Everything a record's performance is computed from lives on THIS Record (hookStrength etc.
	// above), so "the sides reverse" is a field swap between these and their plug-side counterparts --
	// nothing downstream needs to know, because every consumer already reads the live fields.
	[Export] public string bSideTitle;
	[Export] public Genre bSidePrimaryGenre;
	[Export(PropertyHint.Range, "0,1")] public float bSideHookStrength;
	[Export(PropertyHint.Range, "0,1")] public float bSideProductionQuality;
	[Export(PropertyHint.Range, "0,1")] public float bSideDanceability;
	[Export(PropertyHint.Range, "0,1")] public float bSideOriginality;
	/// <summary>True once the sides have been reversed at least once (directive invariant 4: a disc
	/// reverses at most once). False means the disc still ships as pressed -- the A-side plugged.</summary>
	[Export] public bool bSideIsPlugSide;
}

public enum ReleaseFormat { Single, Album, EP }
