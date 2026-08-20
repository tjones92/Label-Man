using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization.Metadata;
using Godot;

/// <summary>
/// The AI-world half of a save -- the "full-world save" work. Every mutable simulation collection outside
/// the player layer (artists, musicians, labels, the label lifecycle, competitor records, the charts, the
/// radio panel, the composition/publishing state) lands here, one subsystem per phase.
///
/// <para>Rehydration is <b>in place</b>: the managers are autoload singletons that persist across a scene
/// reload and never re-run <c>_Ready</c>, so a load clears and repopulates the live singletons rather than
/// regenerating a world. The one-time generation in <see cref="ChartManager"/>._Ready still runs at launch
/// and is simply overwritten by a load. This mirrors how the player layer already restores
/// (<see cref="PlayerDesk.RestoreState"/> mutates in place, no scene reload).</para>
///
/// <para>Determinism target is <b>reload-determinism</b>, not byte-for-byte identity with a never-saved run:
/// the ~113 direct global <c>GD.Rand*</c> draws in the weekly economy can't be snapshotted, so on load we
/// reseed the global RNG deterministically from <c>(worldSeed, chartWeek)</c>. Reloading the same save
/// therefore always continues the same way. Hash-seeded streams reproduce from the saved world seed alone.</para>
///
/// <para><b>Player/world boundary.</b> The world excludes everything the player layer already owns --
/// player-owned artists, the player's label, and (in <see cref="ChartManager.RestorePlayerRecords"/>) the
/// player's released records. Load order is world-first, player-on-top, so the player's roster and catalogue
/// re-link against the restored world.</para>
/// </summary>
public sealed class WorldSaveData {
	// --- Clock (Phase 0) ---
	public int ChartWeek { get; set; }
	public int Hour { get; set; }

	// --- Artists & musicians (Phase 1) ---
	// Whole SimulatedArtist objects (each carries its Musician members inline); musicianRegistry is rebuilt
	// from those members on load. Player-owned acts are excluded -- the player layer restores them.
	public List<SimulatedArtist> Artists { get; set; } = new();
	public List<string> UnsignedArtistIds { get; set; } = new();
	public int ArtistIdCounter { get; set; }
	public int MusicianIdCounter { get; set; }
	public int FallbackNameCounter { get; set; }
	public int FormationYear { get; set; }
	public int FormedThisWeek { get; set; }
	public int FormedYtd { get; set; }
	public int FormationYearPeakTarget { get; set; }
	public Dictionary<int, int> RecentRuntimeFormationCounts { get; set; } = new(); // Genre (as int) -> count
	public bool HasPopulationRng { get; set; }
	public ulong PopulationRngState { get; set; }
	public ulong PopulationRngSeed { get; set; }

	// --- Labels (Phase 1) ---
	// Every AI label from ChartManager.aiLabels (== LabelLifecycleManager.activeLabels), the player's own
	// label excluded. AILabel serializes whole via the field-only contract (WorldJsonContracts); the roster
	// is relinked by artist id.
	public List<WorldLabelSaveData> Labels { get; set; } = new();
	public List<string> DefunctLabelIds { get; set; } = new();
	public int LifecycleYear { get; set; }
	public int LifecycleMonth { get; set; }
	public bool LifecycleProcessingEnabled { get; set; } = true;
	public int DefunctThisYear { get; set; }
	public int FoundedThisYear { get; set; }

	// --- Records & charts (Phase 2) ---
	// Whole RecordRuntimeData objects for the AI world (player records stay in the player layer and re-enter
	// via ChartManager.RestorePlayerRecords). baseRecord (Record) and retired AlbumTracks serialize whole via
	// the field-only contract. Chart membership travels as record ids and is relinked against the rebuilt index;
	// player records re-join the displayed chart at the next weekly recompute.
	public List<RecordRuntimeData> Records { get; set; } = new();
	public List<string> CurrentChartIds { get; set; } = new();
	public List<string> CurrentAlbumChartIds { get; set; } = new();
	public Dictionary<string, int> BubblingUnderPositions { get; set; } = new();
	public Dictionary<string, int> AlbumBubblingUnderPositions { get; set; } = new();
	public Dictionary<string, float> PreviousChartPoints { get; set; } = new();
	public Dictionary<int, float> GenreMomentum { get; set; } = new();   // Genre (as int) -> momentum
	public Dictionary<string, int> CompUseCountByRecordId { get; set; } = new();
	public Dictionary<string, AlbumTrack> RetiredTrackArchive { get; set; } = new();
	public Dictionary<string, float> RegionalDemandScaleById { get; set; } = new();
	public int RecordIdCounter { get; set; }
	public bool CanonicalLiveIdentitiesApplied { get; set; }

	// --- Competitor economy + roster caches (Phase 3) ---
	public CompetitorSaveData Competitor { get; set; }
	public RosterSaveData Roster { get; set; }

	// --- Composition / publishing (Phase 4) ---
	public CompositionSaveData Composition { get; set; }

	// Phase 4 (cont.): radio.
}

/// <summary>The composition catalogue (CompositionCatalogService). Songs, writers, publishers, and the writer
/// ledger serialize whole; the by-genre/by-family pools and the controller-label index travel as song-id lists
/// (the pools share SongComposition objects with <see cref="Songs"/> and are not a pure function of it, so they
/// are relinked rather than rebuilt). The catalogue's own RNG state is preserved.</summary>
public sealed class CompositionSaveData {
	public Dictionary<string, SongComposition> Songs { get; set; } = new();
	public Dictionary<int, List<string>> StandardsByGenre { get; set; } = new();       // Genre int -> songIds
	public Dictionary<int, List<string>> CatalogByGenre { get; set; } = new();
	public Dictionary<int, List<string>> ProfessionalByGenre { get; set; } = new();
	public Dictionary<int, List<string>> TraditionalByGenre { get; set; } = new();
	public Dictionary<int, List<string>> CoverableHitsByGenre { get; set; } = new();
	public Dictionary<int, List<string>> StandardsByFamily { get; set; } = new();       // GenreFamily int -> songIds
	public Dictionary<int, List<string>> TraditionalByFamily { get; set; } = new();
	public Dictionary<int, List<string>> CoverableHitsByFamily { get; set; } = new();
	public Dictionary<string, List<string>> SongsByControllerLabel { get; set; } = new();
	public List<ProfessionalSongwriter> ProfessionalWriters { get; set; } = new();
	public List<MusicPublisher> Publishers { get; set; } = new();
	public Dictionary<string, CompositionCatalogService.WriterCreditLedgerEntry> WriterLedger { get; set; } = new();
	public int SongCounter { get; set; }
	public bool HasRng { get; set; }
	public ulong RngState { get; set; }
	public ulong RngSeed { get; set; }
	public ulong TitleRngState { get; set; }
	public ulong TitleRngSeed { get; set; }
}

/// <summary>Cross-week roster/scouting state (RosterManager) that gates re-signings and vacancy handling.
/// Weekly/daily telemetry and the label-buzz cache are transient (recomputed) and not saved. All keys are
/// artist/label ids, so nothing needs relinking.</summary>
public sealed class RosterSaveData {
	public List<string> UniquelyResignedArtistIds { get; set; } = new();
	public Dictionary<string, int> LastReSignWeekByArtistId { get; set; } = new();
	public Dictionary<string, int> ConsecutiveVacancyWeeksByLabelId { get; set; } = new();
	public Dictionary<string, int> ConsecutiveEmptyWeeksByLabelId { get; set; } = new();
	public Dictionary<string, int> ServiceDeficitAgeByLabelId { get; set; } = new();
}

/// <summary>The competitor economy (CompetitorManager). Album projects and independent distributors serialize
/// whole; their id-keyed indices and the record object refs inside a project are relinked on load. Transient
/// weekly telemetry (per-label revenue/lifecycle accumulators, reset each week) is intentionally not saved.</summary>
public sealed class CompetitorSaveData {
	public List<AlbumProject> AlbumProjects { get; set; } = new();
	public List<AlbumProject> PendingAlbumProjects { get; set; } = new();
	public Dictionary<string, string> ProjectById { get; set; } = new();        // index key -> projectId
	public Dictionary<string, string> ProjectByRecordId { get; set; } = new();  // record key -> projectId
	public List<AnnualArtistProjectCount> AnnualAlbumProjectsByArtist { get; set; } = new();
	public Dictionary<string, List<string>> LabelActiveRecords { get; set; } = new();
	public Dictionary<string, List<CompetitorManager.LabelRecordHistoryEntry>> RetiredLabelRecordHistory { get; set; } = new();
	public List<IndependentDistributor> IndependentDistributors { get; set; } = new();
	public Dictionary<string, List<string>> IndependentDistributorsByRegion { get; set; } = new(); // region -> distributorIds
	public List<string> CreditedLabelTop40RecordIds { get; set; } = new();
	public List<string> CreditedLabelNumberOneRecordIds { get; set; } = new();
	public List<string> ChartedLabelIds { get; set; } = new();
	public List<string> ForcedConsolidationClients { get; set; } = new();
	public Dictionary<string, LabelFinancialHistory> LabelFinancials { get; set; } = new();
	public Dictionary<string, Dictionary<int, int>> AnnualGenreSupplyByLabel { get; set; } = new(); // labelId -> (Genre int -> count)
	public Dictionary<int, int> AnnualGenreSupplyGlobal { get; set; } = new();                      // Genre int -> count
	public int ConsolidationAbsorptionsThisDecade { get; set; }
}

/// <summary>One entry of the (artistId, year) -> project-count map, flattened for JSON (tuple keys don't
/// serialize as dictionary keys).</summary>
public sealed class AnnualArtistProjectCount {
	public string ArtistId { get; set; }
	public int Year { get; set; }
	public int Count { get; set; }
}

/// <summary>One AI label: the whole <see cref="AILabel"/> (roster excluded by contract) plus its roster as
/// artist ids, re-linked against the artist registry on load so label.roster shares identity with it.</summary>
public sealed class WorldLabelSaveData {
	public AILabel Label { get; set; }
	public List<string> RosterArtistIds { get; set; } = new();

	public static WorldLabelSaveData From(AILabel label) => new() {
		Label = label,
		RosterArtistIds = (label.roster ?? new List<SimulatedArtist>())
			.Where(a => a != null && !string.IsNullOrEmpty(a.artistId)).Select(a => a.artistId).ToList()
	};
}

/// <summary>System.Text.Json contract tweaks for the world save.</summary>
public static class WorldJsonContracts {
	/// <summary>Restricts the world's whole-serialized entities to their declared fields, dropping every
	/// property. Applies to:
	/// <list type="bullet">
	/// <item>every Godot <see cref="GodotObject"/>-derived type (AILabel, Record, Album, AlbumTrack, ...) --
	/// dropping their computed properties (e.g. AILabel.distributionStrength, whose setter would corrupt
	/// ownedReach on load) and the inherited GodotObject/Resource members (NativeInstance, ResourcePath, ...)
	/// that are otherwise unserializable. Applying this to <i>all</i> GodotObject types means a new Resource
	/// added to the graph is handled automatically -- no enumeration to maintain;</item>
	/// <item><see cref="GameDate"/>, a struct whose computed properties build a System.DateTime (DayOfWeek,
	/// IsFriday, ...); a default (year 0) GameDate anywhere in the graph would otherwise throw
	/// "un-representable DateTime" on serialize. Fields-only serializes it as {year,month,day}.</item>
	/// </list>
	/// AILabel's roster field is additionally dropped -- it is relinked by id
	/// (<see cref="WorldLabelSaveData.RosterArtistIds"/>) to keep object identity with the artist registry.
	/// A future Resource that holds a <i>shared</i> object reference (like roster) would need the same id-relink
	/// treatment; the round-trip probe surfaces any field that fails to restore.</summary>
	public static void FieldsOnly(JsonTypeInfo typeInfo) {
		if (typeInfo.Kind != JsonTypeInfoKind.Object) return;
		bool fieldOnly = typeInfo.Type == typeof(GameDate) || typeof(GodotObject).IsAssignableFrom(typeInfo.Type);
		if (!fieldOnly) return;
		bool isLabel = typeInfo.Type == typeof(AILabel);
		for (int i = typeInfo.Properties.Count - 1; i >= 0; i--) {
			JsonPropertyInfo member = typeInfo.Properties[i];
			if (member.AttributeProvider is not FieldInfo field) { typeInfo.Properties.RemoveAt(i); continue; }
			if (isLabel && field.Name == "roster") typeInfo.Properties.RemoveAt(i);
		}
	}
}

/// <summary>Captures and rehydrates the AI world in place against the live autoload singletons.</summary>
public static class WorldStateService {
	/// <summary>Snapshots the live AI world into a serializable DTO. Null-safe if a subsystem isn't ready.</summary>
	public static WorldSaveData Capture() {
		var world = new WorldSaveData {
			ChartWeek = ChartManager.Instance?.GetCurrentChartWeek() ?? 0,
			Hour = TimeManager.Instance?.CurrentHour ?? 0
		};
		ArtistManager.Instance?.CaptureWorld(world);
		ChartManager.Instance?.CaptureLabels(world);
		LabelLifecycleManager.Instance?.CaptureLifecycle(world);
		ChartManager.Instance?.CaptureRecords(world);
		CompetitorManager.Instance?.CaptureEconomy(world);
		RosterManager.Instance?.CaptureCaches(world);
		CompositionCatalogService.CaptureWorld(world);
		return world;
	}

	/// <summary>Rehydrates the live AI world from a snapshot, in place. A null world (a v1, player-only save)
	/// is a no-op -- the freshly generated world is left standing and the player layer restores over it.
	/// Order matters: artists first, then labels (whose rosters relink to those artists), then the lifecycle
	/// (whose active list is the very same object as the label list).</summary>
	public static void Apply(WorldSaveData world, GameDate date, ulong? worldSeed) {
		if (world == null) return;

		TimeManager.Instance?.RestoreClock(date, world.Hour);
		ArtistManager.Instance?.RehydrateWorld(world);
		ChartManager.Instance?.RehydrateLabels(world);
		LabelLifecycleManager.Instance?.RehydrateLifecycle(world, ChartManager.Instance?.GetAllLabels());
		ChartManager.Instance?.RehydrateRecords(world);
		CompetitorManager.Instance?.RehydrateEconomy(world);
		RosterManager.Instance?.RehydrateCaches(world);
		CompositionCatalogService.RehydrateWorld(world);
		ChartManager.Instance?.RestoreChartWeek(world.ChartWeek);
		ChartManager.Instance?.RebuildRadioForLoad();   // reporter panel is seed-reproducible; rebuild for the restored year

		// Reseed the global RNG deterministically so repeated loads of this save continue identically.
		GD.Seed(DeriveRngSeed(worldSeed ?? 0UL, world.ChartWeek));
	}

	/// <summary>SplitMix64-style mix of the world seed and the resumed week into a stable global RNG seed.</summary>
	private static ulong DeriveRngSeed(ulong worldSeed, int week) {
		unchecked {
			ulong h = worldSeed ^ 0x9E3779B97F4A7C15UL;
			h = (h ^ (ulong)(uint)week) * 0xBF58476D1CE4E5B9UL;
			h ^= h >> 31;
			h *= 0x94D049BB133111EBUL;
			h ^= h >> 31;
			return h;
		}
	}
}
