using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

/// <summary>
/// Reads and writes a save file. The format is a versioned envelope with a fully-serialized
/// <b>player layer</b> (the label, its cash and profile, the roster by id, the songbook, each act's
/// repertoire, the books, the log, the desk's working state, and the player's released records) plus a
/// <b>world layer</b> (<see cref="WorldSaveData"/>) that snapshots the surrounding AI simulation. The
/// world layer is filled in one subsystem at a time; whatever it does not yet cover is left to the
/// freshly generated world, which the player layer restores over.
///
/// Load order is world-first, player-on-top: <see cref="WorldStateService.Apply"/> rehydrates the AI
/// world in place (over the world generated at launch), then <see cref="PlayerDesk.RestoreState"/> puts
/// the player's label, roster, and catalogue back, re-linking against the saved world. A v1 (player-only)
/// save carries no world section and still loads -- the generated world stands and only the player layer
/// is restored.
/// </summary>
public static class SaveGameService {
	// v1: player layer only. v2: adds the full-world section (WorldSaveData). A v1 file loads under v2 with a
	// null World -- the freshly generated world is left standing and the player layer restores over it.
	public const int CurrentVersion = 2;
	private const string SaveDir = "user://saves";

	private static readonly JsonSerializerOptions JsonOptions = new() {
		WriteIndented = true,
		// The roster is persisted as whole SimulatedArtist objects, which expose their state as public
		// FIELDS (not properties), so field serialization must be on. The save DTOs use properties and are
		// unaffected. IncludeFields also picks up SimulatedArtist's nested Musician / evolution graph.
		IncludeFields = true,
		DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
		// The full-world save serializes AILabel (a Godot Resource) whole; this contract restricts it to its
		// own declared fields, dropping computed properties and inherited GodotObject/Resource members. Only
		// AILabel is affected -- every other type keeps the default contract.
		TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver {
			Modifiers = { WorldJsonContracts.FieldsOnly }
		}
	};

	/// <summary>The exact serializer options the save uses (including the AILabel field-only contract). Exposed
	/// for the save round-trip probe so it serializes a <see cref="WorldSaveData"/> the same way the save does.</summary>
	public static JsonSerializerOptions TestJsonOptions => JsonOptions;

	private static string PathFor(string slot) => $"{SaveDir}/{Sanitize(slot)}.json";
	// A tiny uncompressed sidecar so the load menu doesn't have to decompress a multi-hundred-MB body just to
	// read the label name and date. Written next to the (gzipped) body; the body stays the source of truth.
	private static string MetaPathFor(string slot) => $"{SaveDir}/{Sanitize(slot)}.meta";

	// The body is gzip-compressed JSON (a full-world save is ~350 MB at two in-game years, >1 GB a decade, and
	// compresses ~15-20x). Reads are magic-byte aware, so a pre-compression plain-JSON save still loads.
	private static bool LooksGzipped(byte[] data) => data != null && data.Length >= 2 && data[0] == 0x1f && data[1] == 0x8b;

	private static byte[] ReadAllBytes(string path) {
		using Godot.FileAccess file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
		return file?.GetBuffer((long)file.GetLength());
	}

	/// <summary>The lightweight sidecar record the load menu reads per save.</summary>
	private sealed class SaveMeta {
		public int Version { get; set; }
		public string SavedAtUtc { get; set; }
		public int Year { get; set; }
		public int Month { get; set; }
		public int Day { get; set; }
		public string LabelName { get; set; }
	}

	private static void WriteMeta(string slot, SaveEnvelope envelope) {
		try {
			var meta = new SaveMeta {
				Version = envelope.Version, SavedAtUtc = envelope.SavedAtUtc,
				Year = envelope.Year, Month = envelope.Month, Day = envelope.Day,
				LabelName = envelope.Player?.Label?.labelName ?? slot
			};
			using Godot.FileAccess file = Godot.FileAccess.Open(MetaPathFor(slot), Godot.FileAccess.ModeFlags.Write);
			file?.StoreString(JsonSerializer.Serialize(meta));
		} catch { /* the meta sidecar is an optimization; ListSaves falls back to the body header */ }
	}

	private static string Sanitize(string slot) =>
		string.IsNullOrWhiteSpace(slot) ? "quicksave"
			: new string(slot.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_').ToArray());

	public static bool HasSave(string slot = "quicksave") => Godot.FileAccess.FileExists(PathFor(slot));

	/// <summary>A save on disk, for the load menu. Read from each file's lightweight header only.</summary>
	public readonly record struct SaveInfo(string Slot, string LabelName, GameDate InGameDate, DateTime SavedAtUtc);

	/// <summary>Every save on disk, newest first. Corrupt or unreadable files are skipped, not thrown.</summary>
	public static List<SaveInfo> ListSaves() {
		var result = new List<SaveInfo>();
		using DirAccess dir = DirAccess.Open(SaveDir);
		if (dir == null) return result;
		foreach (string file in dir.GetFiles()) {
			if (!file.EndsWith(".json")) continue;
			string slot = file.Substring(0, file.Length - ".json".Length);
			try {
				SaveMeta meta = ReadMeta(slot) ?? ReadHeaderFromBody(slot);
				if (meta == null) continue;
				DateTime.TryParse(meta.SavedAtUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime savedAt);
				result.Add(new SaveInfo(slot, meta.LabelName ?? slot, new GameDate(meta.Year, meta.Month, meta.Day), savedAt));
			} catch { /* skip a save we can't read */ }
		}
		result.Sort((a, b) => b.SavedAtUtc.CompareTo(a.SavedAtUtc));
		return result;
	}

	/// <summary>Deletes a save file (and its meta sidecar). Returns false if there was nothing there.</summary>
	public static bool Delete(string slot) {
		string path = PathFor(slot);
		if (!Godot.FileAccess.FileExists(path)) return false;
		bool ok = DirAccess.RemoveAbsolute(path) == Error.Ok;
		if (Godot.FileAccess.FileExists(MetaPathFor(slot))) DirAccess.RemoveAbsolute(MetaPathFor(slot));
		return ok;
	}

	/// <summary>Reads the sidecar meta for a slot, or null if it isn't there.</summary>
	private static SaveMeta ReadMeta(string slot) {
		if (!Godot.FileAccess.FileExists(MetaPathFor(slot))) return null;
		try {
			using Godot.FileAccess file = Godot.FileAccess.Open(MetaPathFor(slot), Godot.FileAccess.ModeFlags.Read);
			return file == null ? null : JsonSerializer.Deserialize<SaveMeta>(file.GetAsText());
		} catch { return null; }
	}

	/// <summary>Fallback for a save with no sidecar: decompress the body and parse just the header fields.</summary>
	private static SaveMeta ReadHeaderFromBody(string slot) {
		byte[] bytes = ReadAllBytes(PathFor(slot));
		if (bytes == null) return null;
		string json = LooksGzipped(bytes)
			? DecompressToString(bytes)
			: System.Text.Encoding.UTF8.GetString(bytes);
		SaveHeader header = JsonSerializer.Deserialize<SaveHeader>(json, JsonOptions);
		if (header == null) return null;
		return new SaveMeta {
			Version = header.Version, SavedAtUtc = header.SavedAtUtc,
			Year = header.Year, Month = header.Month, Day = header.Day,
			LabelName = header.Player?.Label?.labelName ?? slot
		};
	}

	private static string DecompressToString(byte[] data) {
		using var input = new System.IO.MemoryStream(data);
		using var gzip = new System.IO.Compression.GZipStream(input, System.IO.Compression.CompressionMode.Decompress);
		using var output = new System.IO.MemoryStream();
		gzip.CopyTo(output);
		return System.Text.Encoding.UTF8.GetString(output.ToArray());
	}

	/// <summary>Just the top-level fields needed to describe a save, so the load menu doesn't build the whole
	/// player graph per file. Unmapped sections (songs, masters, roster, records) are parsed past, not built.</summary>
	private sealed class SaveHeader {
		public int Version { get; set; }
		public string SavedAtUtc { get; set; }
		public int Year { get; set; }
		public int Month { get; set; }
		public int Day { get; set; }
		public HeaderPlayer Player { get; set; }
		public sealed class HeaderPlayer { public HeaderLabel Label { get; set; } }
		public sealed class HeaderLabel { public string labelName { get; set; } }
	}

	/// <summary>Snapshots the player layer and the clock into a save file. Returns false with a reason.</summary>
	public static bool Save(string slot, out string message) {
		if (PlayerDesk.Instance?.HasLabel != true) { message = "No label to save yet."; return false; }

		var envelope = new SaveEnvelope {
			Version = CurrentVersion,
			SavedAtUtc = DateTime.UtcNow.ToString("o"),
			WorldSeed = SimulationSeedBootstrap.RequestedSeed,
			Player = PlayerDesk.Instance.CaptureState(),
			World = WorldStateService.Capture()
		};
		GameDate now = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		envelope.Year = now.year; envelope.Month = now.month; envelope.Day = now.day;

		try {
			DirAccess.MakeDirRecursiveAbsolute(SaveDir);
			// Stream the envelope straight through gzip into a buffer -- never materializing the whole (huge)
			// JSON string in memory -- then store the compressed bytes.
			byte[] payload;
			using (var buffer = new System.IO.MemoryStream()) {
				using (var gzip = new System.IO.Compression.GZipStream(buffer, System.IO.Compression.CompressionLevel.Optimal, leaveOpen: true))
					JsonSerializer.Serialize(gzip, envelope, JsonOptions);
				payload = buffer.ToArray();
			}
			using (Godot.FileAccess file = Godot.FileAccess.Open(PathFor(slot), Godot.FileAccess.ModeFlags.Write)) {
				if (file == null) { message = $"Couldn't open the save file ({Godot.FileAccess.GetOpenError()})."; return false; }
				file.StoreBuffer(payload);
			}
			WriteMeta(slot, envelope);
		} catch (Exception error) {
			message = $"Save failed: {error.Message}";
			GD.PushError($"[SaveGame] {error}");
			return false;
		}
		message = $"Saved to {slot}.";
		GD.Print($"[SaveGame] wrote {PathFor(slot)}");
		return true;
	}

	/// <summary>Restores the player layer from a save file. Returns false with a reason.</summary>
	public static bool Load(string slot, out string message) {
		if (!HasSave(slot)) { message = "No save in that slot."; return false; }

		SaveEnvelope envelope;
		try {
			byte[] bytes = ReadAllBytes(PathFor(slot));
			if (bytes == null) { message = $"Couldn't open the save file ({Godot.FileAccess.GetOpenError()})."; return false; }
			if (LooksGzipped(bytes)) {
				using var input = new System.IO.MemoryStream(bytes);
				using var gzip = new System.IO.Compression.GZipStream(input, System.IO.Compression.CompressionMode.Decompress);
				envelope = JsonSerializer.Deserialize<SaveEnvelope>(gzip, JsonOptions);
			} else {
				envelope = JsonSerializer.Deserialize<SaveEnvelope>(bytes, JsonOptions);   // pre-compression plain JSON
			}
		} catch (Exception error) {
			message = $"Load failed: {error.Message}";
			GD.PushError($"[SaveGame] {error}");
			return false;
		}
		if (envelope == null) { message = "That save is unreadable."; return false; }
		if (envelope.Version > CurrentVersion) { message = "That save is from a newer version."; return false; }
		if (PlayerDesk.Instance == null) { message = "The desk isn't ready."; return false; }

		// The world rehydrates first (in place, over the generated world), then the player layer restores on
		// top of it -- so roster acts and the player's released records re-link against the saved world, not a
		// fresh one. A v1 save has a null World and this is a no-op.
		GameDate savedDate = new GameDate(envelope.Year, envelope.Month, envelope.Day);
		WorldStateService.Apply(envelope.World, savedDate, envelope.WorldSeed);

		return PlayerDesk.Instance.RestoreState(envelope.Player, out message);
	}
}

// ============================================================================
// SAVE DATA -- plain serializable snapshots of the player layer.
// ============================================================================

/// <summary>The top-level save file. The reserved world section is Phase 4 (full-world save).</summary>
public sealed class SaveEnvelope {
	public int Version { get; set; } = SaveGameService.CurrentVersion;
	public string SavedAtUtc { get; set; }
	public ulong? WorldSeed { get; set; }
	public int Year { get; set; }
	public int Month { get; set; }
	public int Day { get; set; }
	public PlayerSaveData Player { get; set; }
	public WorldSaveData World { get; set; }   // The full mutable simulation (v2+). Null in a v1 save.
}

public sealed class PlayerSaveData {
	public LabelSaveData Label { get; set; }
	// The roster acts themselves, serialized whole -- so a relaunch (or a runtime-signed act the fresh world
	// never generated) brings the real artists back, not just their ids. Field serialization is on for these.
	public List<SimulatedArtist> RosterArtists { get; set; } = new();
	public List<SongSaveData> Songs { get; set; } = new();
	public Dictionary<string, List<RepertoireSaveData>> Repertoire { get; set; } = new();
	public List<string> Log { get; set; } = new();
	public List<WeekBookSaveData> Books { get; set; } = new();

	// Player-layer completion: the whole working state of the desk.
	public List<MasterSaveData> Masters { get; set; } = new();          // finished masters on the shelf / assembled / scheduled
	public List<PlannedReleaseSaveData> Planned { get; set; } = new();   // assembled singles, dated or not
	public List<PressStockSaveData> Inventory { get; set; } = new();     // pressed 45s on hand at the office
	public List<PressOrderSaveData> PressOrders { get; set; } = new();   // runs still at the plant
	public List<ConsignmentLotSaveData> Consignment { get; set; } = new(); // stock left in towns' shops
	public Dictionary<string, float> ConsignmentOwed { get; set; } = new(); // money towns are holding for you
	public Dictionary<string, int> WeeklyTrunkUnits { get; set; } = new();  // trunk units to fold into the chart at week end
	public List<string> WorkedCities { get; set; } = new();
	public string CurrentCityId { get; set; }
	public int Counter { get; set; }
	public int MonthsInTheRed { get; set; }

	// Increment 2: the player's own released records, with their chart + regional state.
	public List<RuntimeRecordSaveData> ReleasedRecords { get; set; } = new();
}

public sealed class LabelSaveData {
	public string labelId { get; set; }
	public string labelName { get; set; }
	public string founderName { get; set; }
	public string headquartersCity { get; set; }
	public string homeRegion { get; set; }
	public string homeCityId { get; set; }
	public int archetype { get; set; }
	public int tier { get; set; }
	public int foundedYear { get; set; }
	public float cashReserves { get; set; }
	public float monthlyRevenue { get; set; }
	public float monthlyExpenses { get; set; }
	public float lastMonthlyProfit { get; set; }
	public float reputation { get; set; }
	public int maxRosterSize { get; set; }
	public float nationalReach { get; set; }
	public float ownedReach { get; set; }
	public float distributionStrength { get; set; }
	public float budgetLevel { get; set; }
	public float scoutingAbility { get; set; }
	public float productionQuality { get; set; }
	public float marketingPower { get; set; }
	public float riskTolerance { get; set; }
	public float artistLoyalty { get; set; }
	public float payolaWillingness { get; set; }
	public float releasesPerMonth { get; set; }
	public int totalReleases { get; set; }
	public int top40Hits { get; set; }
	public int numberOneHits { get; set; }
	public int monthsActive { get; set; }
	public float outstandingWholesaleReceivables { get; set; }
	public float lifetimeWholesaleWriteOffs { get; set; }
	public string[] strongRegions { get; set; } = Array.Empty<string>();
	public string[] distributionRegions { get; set; } = Array.Empty<string>();
	public int[] preferredGenres { get; set; } = Array.Empty<int>();
	public int[] secondaryGenres { get; set; } = Array.Empty<int>();
	public List<string> RosterArtistIds { get; set; } = new();

	public static LabelSaveData From(AILabel l) => new() {
		labelId = l.labelId, labelName = l.labelName, founderName = l.founderName,
		headquartersCity = l.headquartersCity, homeRegion = l.homeRegion, homeCityId = l.homeCityId,
		archetype = (int)l.archetype, tier = (int)l.tier, foundedYear = l.foundedYear,
		cashReserves = l.cashReserves, monthlyRevenue = l.monthlyRevenue, monthlyExpenses = l.monthlyExpenses,
		lastMonthlyProfit = l.lastMonthlyProfit, reputation = l.reputation, maxRosterSize = l.maxRosterSize,
		nationalReach = l.nationalReach, ownedReach = l.ownedReach, distributionStrength = l.distributionStrength,
		budgetLevel = l.budgetLevel, scoutingAbility = l.scoutingAbility, productionQuality = l.productionQuality,
		marketingPower = l.marketingPower, riskTolerance = l.riskTolerance, artistLoyalty = l.artistLoyalty,
		payolaWillingness = l.payolaWillingness, releasesPerMonth = l.releasesPerMonth,
		totalReleases = l.totalReleases, top40Hits = l.top40Hits, numberOneHits = l.numberOneHits,
		monthsActive = l.monthsActive,
		outstandingWholesaleReceivables = l.outstandingWholesaleReceivables,
		lifetimeWholesaleWriteOffs = l.lifetimeWholesaleWriteOffs,
		strongRegions = l.strongRegions ?? Array.Empty<string>(),
		distributionRegions = l.distributionRegions ?? Array.Empty<string>(),
		preferredGenres = (l.preferredGenres ?? Array.Empty<Genre>()).Select(g => (int)g).ToArray(),
		secondaryGenres = (l.secondaryGenres ?? Array.Empty<Genre>()).Select(g => (int)g).ToArray(),
		RosterArtistIds = (l.roster ?? new List<SimulatedArtist>()).Select(a => a.artistId).ToList()
	};

	public void ApplyTo(AILabel l) {
		l.labelId = labelId; l.labelName = labelName; l.founderName = founderName;
		l.headquartersCity = headquartersCity; l.homeRegion = homeRegion; l.homeCityId = homeCityId;
		l.archetype = (LabelArchetype)archetype; l.tier = (LabelTier)tier; l.foundedYear = foundedYear;
		l.cashReserves = cashReserves; l.monthlyRevenue = monthlyRevenue; l.monthlyExpenses = monthlyExpenses;
		l.lastMonthlyProfit = lastMonthlyProfit; l.reputation = reputation; l.maxRosterSize = maxRosterSize;
		l.nationalReach = nationalReach; l.ownedReach = ownedReach; l.distributionStrength = distributionStrength;
		l.budgetLevel = budgetLevel; l.scoutingAbility = scoutingAbility; l.productionQuality = productionQuality;
		l.marketingPower = marketingPower; l.riskTolerance = riskTolerance; l.artistLoyalty = artistLoyalty;
		l.payolaWillingness = payolaWillingness; l.releasesPerMonth = releasesPerMonth;
		l.totalReleases = totalReleases; l.top40Hits = top40Hits; l.numberOneHits = numberOneHits;
		l.monthsActive = monthsActive;
		l.outstandingWholesaleReceivables = outstandingWholesaleReceivables;
		l.lifetimeWholesaleWriteOffs = lifetimeWholesaleWriteOffs;
		l.strongRegions = strongRegions ?? Array.Empty<string>();
		l.distributionRegions = distributionRegions ?? Array.Empty<string>();
		l.preferredGenres = (preferredGenres ?? Array.Empty<int>()).Select(g => (Genre)g).ToArray();
		l.secondaryGenres = (secondaryGenres ?? Array.Empty<int>()).Select(g => (Genre)g).ToArray();
	}
}

public sealed class SongSaveData {
	public string SongId { get; set; }
	public string Title { get; set; }
	public string ArtistId { get; set; }
	public int Genre { get; set; }
	public float Hook { get; set; }
	public float Originality { get; set; }
	public float Danceability { get; set; }
	public int Year { get; set; }
	public int Month { get; set; }
	public int Day { get; set; }
	public bool Recorded { get; set; }

	public static SongSaveData From(PlayerDesk.Song s) => new() {
		SongId = s.SongId, Title = s.Title, ArtistId = s.ArtistId, Genre = (int)s.Genre,
		Hook = s.Hook, Originality = s.Originality, Danceability = s.Danceability,
		Year = s.Written.year, Month = s.Written.month, Day = s.Written.day, Recorded = s.Recorded
	};

	public PlayerDesk.Song ToSong() => new() {
		SongId = SongId, Title = Title, ArtistId = ArtistId, Genre = (Genre)Genre,
		Hook = Hook, Originality = Originality, Danceability = Danceability,
		Written = new GameDate(Year, Month, Day), Recorded = Recorded
	};
}

public sealed class RepertoireSaveData {
	public string Title { get; set; }
	public string SourceTag { get; set; }
	public bool IsOriginal { get; set; }
	public string SongId { get; set; }
	public int Genre { get; set; }
	public float ReadHook { get; set; }
	public float ReadQuality { get; set; }

	public static RepertoireSaveData From(PlayerDesk.RepertoireItem r) => new() {
		Title = r.Title, SourceTag = r.SourceTag, IsOriginal = r.IsOriginal, SongId = r.SongId,
		Genre = (int)r.Genre, ReadHook = r.ReadHook, ReadQuality = r.ReadQuality
	};

	public PlayerDesk.RepertoireItem ToItem() => new() {
		Title = Title, SourceTag = SourceTag, IsOriginal = IsOriginal, SongId = SongId,
		Genre = (Genre)Genre, ReadHook = ReadHook, ReadQuality = ReadQuality
	};
}

public sealed class WeekBookSaveData {
	public int Week { get; set; }
	public int Year { get; set; }
	public int Month { get; set; }
	public int Day { get; set; }
	public long Units { get; set; }
	public float Gross { get; set; }
	public float ManufacturingCost { get; set; }
	public float DistributionSkim { get; set; }
	public float ArtistRoyalty { get; set; }
	public float Earned { get; set; }
	public float Deferred { get; set; }
	public float Collected { get; set; }
	public float Banked { get; set; }
	public float Outstanding { get; set; }
	public float Cash { get; set; }

	public static WeekBookSaveData From(PlayerDesk.WeekBooks w) => new() {
		Week = w.Week, Year = w.Date.year, Month = w.Date.month, Day = w.Date.day, Units = w.Units,
		Gross = w.Gross, ManufacturingCost = w.ManufacturingCost, DistributionSkim = w.DistributionSkim,
		ArtistRoyalty = w.ArtistRoyalty, Earned = w.Earned, Deferred = w.Deferred, Collected = w.Collected,
		Banked = w.Banked, Outstanding = w.Outstanding, Cash = w.Cash
	};

	public PlayerDesk.WeekBooks ToWeekBooks() => new() {
		Week = Week, Date = new GameDate(Year, Month, Day), Units = Units,
		Gross = Gross, ManufacturingCost = ManufacturingCost, DistributionSkim = DistributionSkim,
		ArtistRoyalty = ArtistRoyalty, Earned = Earned, Deferred = Deferred, Collected = Collected,
		Banked = Banked, Outstanding = Outstanding, Cash = Cash
	};
}

// ============================================================================
// RECORDS -- the base master data, and (for released records) the runtime chart/regional state.
// ============================================================================

/// <summary>The durable identity + audio + composition fields of a <see cref="Record"/>. Week-local
/// simulation state lives on the runtime wrapper, not here.</summary>
public sealed class RecordSaveData {
	public string recordId { get; set; }
	public string title { get; set; }
	public string artistName { get; set; }
	public string artistId { get; set; }
	public string labelId { get; set; }
	public int format { get; set; }
	public bool isPlayerOwned { get; set; }
	public int projectRole { get; set; }
	public string albumProjectId { get; set; }
	public int primaryGenre { get; set; }
	public int secondaryGenre { get; set; }
	public int genreSchemaVersion { get; set; }
	public string primaryGenreId { get; set; }
	public string secondaryGenreId { get; set; }
	public string[] genreTagIds { get; set; } = Array.Empty<string>();
	public float hookStrength { get; set; }
	public float productionQuality { get; set; }
	public float originality { get; set; }
	public float danceability { get; set; }
	public float controversy { get; set; }
	public int Year { get; set; }
	public int Month { get; set; }
	public int Day { get; set; }
	// Composition / publishing identity.
	public string songId { get; set; }
	public int songSource { get; set; }
	public bool isCover { get; set; }
	public string originalRecordId { get; set; }
	public string originalArtistId { get; set; }
	public string publisherId { get; set; }
	public string publishingControllerLabelId { get; set; }
	public string publishingControllerArtistId { get; set; }
	public int publishingControl { get; set; }
	public string[] songwriterIds { get; set; } = Array.Empty<string>();
	public string[] songwriterNames { get; set; } = Array.Empty<string>();
	public float[] songwriterShares { get; set; } = Array.Empty<float>();
	public float compositionQuality { get; set; }
	public float compositionHook { get; set; }
	public float lyricQuality { get; set; }
	public float songFamiliarityAtRelease { get; set; }
	public float standardDurability { get; set; }
	public float arrangementOriginality { get; set; }
	public float professionalPolish { get; set; }

	public static RecordSaveData From(Record r) => new() {
		recordId = r.recordId, title = r.title, artistName = r.artistName, artistId = r.artistId,
		labelId = r.labelId, format = (int)r.format, isPlayerOwned = r.isPlayerOwned,
		projectRole = (int)r.projectRole, albumProjectId = r.albumProjectId,
		primaryGenre = (int)r.primaryGenre, secondaryGenre = (int)r.secondaryGenre,
		genreSchemaVersion = r.genreSchemaVersion, primaryGenreId = r.primaryGenreId, secondaryGenreId = r.secondaryGenreId,
		genreTagIds = r.genreTagIds ?? Array.Empty<string>(),
		hookStrength = r.hookStrength, productionQuality = r.productionQuality, originality = r.originality,
		danceability = r.danceability, controversy = r.controversy,
		Year = r.releaseDate.year, Month = r.releaseDate.month, Day = r.releaseDate.day,
		songId = r.songId, songSource = (int)r.songSource, isCover = r.isCover,
		originalRecordId = r.originalRecordId, originalArtistId = r.originalArtistId, publisherId = r.publisherId,
		publishingControllerLabelId = r.publishingControllerLabelId, publishingControllerArtistId = r.publishingControllerArtistId,
		publishingControl = (int)r.publishingControl,
		songwriterIds = r.songwriterIds ?? Array.Empty<string>(),
		songwriterNames = r.songwriterNames ?? Array.Empty<string>(),
		songwriterShares = r.songwriterShares ?? Array.Empty<float>(),
		compositionQuality = r.compositionQuality, compositionHook = r.compositionHook, lyricQuality = r.lyricQuality,
		songFamiliarityAtRelease = r.songFamiliarityAtRelease, standardDurability = r.standardDurability,
		arrangementOriginality = r.arrangementOriginality, professionalPolish = r.professionalPolish
	};

	public Record ToRecord() => new() {
		recordId = recordId, title = title, artistName = artistName, artistId = artistId,
		labelId = labelId, format = (ReleaseFormat)format, isPlayerOwned = isPlayerOwned,
		projectRole = (ProjectRecordRole)projectRole, albumProjectId = albumProjectId,
		primaryGenre = (Genre)primaryGenre, secondaryGenre = (Genre)secondaryGenre,
		genreSchemaVersion = genreSchemaVersion, primaryGenreId = primaryGenreId, secondaryGenreId = secondaryGenreId,
		genreTagIds = genreTagIds ?? Array.Empty<string>(),
		hookStrength = hookStrength, productionQuality = productionQuality, originality = originality,
		danceability = danceability, controversy = controversy,
		releaseDate = new GameDate(Year, Month, Day),
		songId = songId, songSource = (SongMaterialSource)songSource, isCover = isCover,
		originalRecordId = originalRecordId, originalArtistId = originalArtistId, publisherId = publisherId,
		publishingControllerLabelId = publishingControllerLabelId, publishingControllerArtistId = publishingControllerArtistId,
		publishingControl = (PublishingControlType)publishingControl,
		songwriterIds = songwriterIds ?? Array.Empty<string>(),
		songwriterNames = songwriterNames ?? Array.Empty<string>(),
		songwriterShares = songwriterShares ?? Array.Empty<float>(),
		compositionQuality = compositionQuality, compositionHook = compositionHook, lyricQuality = lyricQuality,
		songFamiliarityAtRelease = songFamiliarityAtRelease, standardDurability = standardDurability,
		arrangementOriginality = arrangementOriginality, professionalPolish = professionalPolish
	};
}

/// <summary>The durable per-region state of a released record. Week-local demand/audit fields are
/// recomputed by the engine and not persisted.</summary>
public sealed class RegionalRecordSaveData {
	public string regionId { get; set; }
	public float awareness { get; set; }
	public float sentiment { get; set; }
	public float radioPlay { get; set; }
	public float tailRadioPlay { get; set; }
	public float jukeboxPlay { get; set; }
	public bool stationsDropped { get; set; }
	public int stationDropAge { get; set; }
	public int unitsInStores { get; set; }
	public int unitsBackordered { get; set; }
	public int unitsSoldTotal { get; set; }
	public float breakoutScore { get; set; }
	public float peakBreakoutScore { get; set; }
	public int breakoutStage { get; set; }
	public float salesVelocity { get; set; }
	public int sustainedGrowthWeeks { get; set; }
	public int tractionWeeks { get; set; }
	public int collapseWeeks { get; set; }

	public static RegionalRecordSaveData From(RegionalRecordData d) => new() {
		regionId = d.regionId, awareness = d.awareness, sentiment = d.sentiment, radioPlay = d.radioPlay,
		tailRadioPlay = d.tailRadioPlay, jukeboxPlay = d.jukeboxPlay, stationsDropped = d.stationsDropped,
		stationDropAge = d.stationDropAge, unitsInStores = d.unitsInStores, unitsBackordered = d.unitsBackordered,
		unitsSoldTotal = d.unitsSoldTotal, breakoutScore = d.breakoutScore, peakBreakoutScore = d.peakBreakoutScore,
		breakoutStage = (int)d.breakoutStage, salesVelocity = d.salesVelocity,
		sustainedGrowthWeeks = d.sustainedGrowthWeeks, tractionWeeks = d.tractionWeeks, collapseWeeks = d.collapseWeeks
	};

	public RegionalRecordData ToRegional() {
		var d = new RegionalRecordData(regionId) {
			awareness = awareness, sentiment = sentiment, radioPlay = radioPlay, tailRadioPlay = tailRadioPlay,
			jukeboxPlay = jukeboxPlay, stationsDropped = stationsDropped, stationDropAge = stationDropAge,
			unitsInStores = unitsInStores, unitsBackordered = unitsBackordered, unitsSoldTotal = unitsSoldTotal,
			breakoutScore = breakoutScore, peakBreakoutScore = peakBreakoutScore,
			breakoutStage = (RegionalBreakoutStage)breakoutStage, salesVelocity = salesVelocity,
			sustainedGrowthWeeks = sustainedGrowthWeeks, tractionWeeks = tractionWeeks, collapseWeeks = collapseWeeks
		};
		return d;
	}
}

/// <summary>A released player record: its base master data plus the durable runtime chart/sales/regional
/// state, so a reload resumes its chart run rather than starting it over.</summary>
public sealed class RuntimeRecordSaveData {
	public RecordSaveData Record { get; set; }
	public List<RegionalRecordSaveData> Regions { get; set; } = new();
	public int currentPosition { get; set; }
	public int lastWeekPosition { get; set; }
	public int peakPosition { get; set; }
	public int weeksOnChart { get; set; }
	public int weeksSinceRelease { get; set; }
	public int lastChartedAge { get; set; }
	public int lastSalesAboveRetirementFloorAge { get; set; }
	public int weeksInTopTen { get; set; }
	public bool artistChartEntryCredited { get; set; }
	public bool artistTop40Credited { get; set; }
	public bool artistTop10Credited { get; set; }
	public bool artistNumberOneCredited { get; set; }
	public bool artistChartRunCompleted { get; set; }
	public bool culturalRunCompleted { get; set; }
	public bool landmarkPublished { get; set; }
	public int artistContractSequenceAtRelease { get; set; }
	public bool isBullet { get; set; }
	public bool isAnchor { get; set; }
	public float overallMomentum { get; set; }
	public int unitsThisWeek { get; set; }
	public int unitsPreviousWeek { get; set; }
	public int totalUnitsSold { get; set; }
	public int peakWeeklyUnits { get; set; }
	public int weeksSincePeakUnits { get; set; }
	public float radioPanelShare { get; set; }
	public float lifetimeLabelNet { get; set; }
	public float sunkProductionCost { get; set; }
	public bool revenueMemoryEligible { get; set; }
	public float releaseTimeExpectedNet { get; set; }
	public float releaseTimeOpportunityScale { get; set; }
	public int releaseMemoryWeek { get; set; }
	public float awareness { get; set; }
	public float momentum { get; set; }
	public float saturation { get; set; }
	public float radioHeat { get; set; }
	public float wordOfMouth { get; set; }
	public float artistHeat { get; set; }
	public int artistPreviousHits { get; set; }
	public float currentLabelPush { get; set; }
	public float totalLabelInvestment { get; set; }
	public float peakMomentum { get; set; }
	public int weeksPositive { get; set; }
	public int weeksNegative { get; set; }
	public bool isGrammyNominated { get; set; }
	public bool isGrammyWinner { get; set; }
	public int weeksOfGrammyBump { get; set; }
	public int initialLaunchStock { get; set; }
	public float perceivedQualityMultiplier { get; set; }

	public static RuntimeRecordSaveData From(RecordRuntimeData r) => new() {
		Record = RecordSaveData.From(r.baseRecord),
		Regions = (r.regionalData ?? new Dictionary<string, RegionalRecordData>()).Values
			.Select(RegionalRecordSaveData.From).ToList(),
		currentPosition = r.currentPosition, lastWeekPosition = r.lastWeekPosition, peakPosition = r.peakPosition,
		weeksOnChart = r.weeksOnChart, weeksSinceRelease = r.weeksSinceRelease, lastChartedAge = r.lastChartedAge,
		lastSalesAboveRetirementFloorAge = r.lastSalesAboveRetirementFloorAge, weeksInTopTen = r.weeksInTopTen,
		artistChartEntryCredited = r.artistChartEntryCredited, artistTop40Credited = r.artistTop40Credited,
		artistTop10Credited = r.artistTop10Credited, artistNumberOneCredited = r.artistNumberOneCredited,
		artistChartRunCompleted = r.artistChartRunCompleted, culturalRunCompleted = r.culturalRunCompleted,
		landmarkPublished = r.landmarkPublished, artistContractSequenceAtRelease = r.artistContractSequenceAtRelease,
		isBullet = r.isBullet, isAnchor = r.isAnchor, overallMomentum = r.overallMomentum,
		unitsThisWeek = r.unitsThisWeek, unitsPreviousWeek = r.unitsPreviousWeek, totalUnitsSold = r.totalUnitsSold,
		peakWeeklyUnits = r.peakWeeklyUnits, weeksSincePeakUnits = r.weeksSincePeakUnits, radioPanelShare = r.radioPanelShare,
		lifetimeLabelNet = r.lifetimeLabelNet, sunkProductionCost = r.sunkProductionCost, revenueMemoryEligible = r.revenueMemoryEligible,
		releaseTimeExpectedNet = r.releaseTimeExpectedNet, releaseTimeOpportunityScale = r.releaseTimeOpportunityScale,
		releaseMemoryWeek = r.releaseMemoryWeek, awareness = r.awareness, momentum = r.momentum, saturation = r.saturation,
		radioHeat = r.radioHeat, wordOfMouth = r.wordOfMouth, artistHeat = r.artistHeat, artistPreviousHits = r.artistPreviousHits,
		currentLabelPush = r.currentLabelPush, totalLabelInvestment = r.totalLabelInvestment, peakMomentum = r.peakMomentum,
		weeksPositive = r.weeksPositive, weeksNegative = r.weeksNegative, isGrammyNominated = r.isGrammyNominated,
		isGrammyWinner = r.isGrammyWinner, weeksOfGrammyBump = r.weeksOfGrammyBump, initialLaunchStock = r.initialLaunchStock,
		perceivedQualityMultiplier = r.perceivedQualityMultiplier
	};

	public RecordRuntimeData ToRuntime() {
		var runtime = new RecordRuntimeData(Record.ToRecord()) {
			currentPosition = currentPosition, lastWeekPosition = lastWeekPosition, peakPosition = peakPosition,
			weeksOnChart = weeksOnChart, weeksSinceRelease = weeksSinceRelease, lastChartedAge = lastChartedAge,
			lastSalesAboveRetirementFloorAge = lastSalesAboveRetirementFloorAge, weeksInTopTen = weeksInTopTen,
			artistChartEntryCredited = artistChartEntryCredited, artistTop40Credited = artistTop40Credited,
			artistTop10Credited = artistTop10Credited, artistNumberOneCredited = artistNumberOneCredited,
			artistChartRunCompleted = artistChartRunCompleted, culturalRunCompleted = culturalRunCompleted,
			landmarkPublished = landmarkPublished, artistContractSequenceAtRelease = artistContractSequenceAtRelease,
			isBullet = isBullet, isAnchor = isAnchor, overallMomentum = overallMomentum,
			unitsThisWeek = unitsThisWeek, unitsPreviousWeek = unitsPreviousWeek, totalUnitsSold = totalUnitsSold,
			peakWeeklyUnits = peakWeeklyUnits, weeksSincePeakUnits = weeksSincePeakUnits, radioPanelShare = radioPanelShare,
			lifetimeLabelNet = lifetimeLabelNet, sunkProductionCost = sunkProductionCost, revenueMemoryEligible = revenueMemoryEligible,
			releaseTimeExpectedNet = releaseTimeExpectedNet, releaseTimeOpportunityScale = releaseTimeOpportunityScale,
			releaseMemoryWeek = releaseMemoryWeek, awareness = awareness, momentum = momentum, saturation = saturation,
			radioHeat = radioHeat, wordOfMouth = wordOfMouth, artistHeat = artistHeat, artistPreviousHits = artistPreviousHits,
			currentLabelPush = currentLabelPush, totalLabelInvestment = totalLabelInvestment, peakMomentum = peakMomentum,
			weeksPositive = weeksPositive, weeksNegative = weeksNegative, isGrammyNominated = isGrammyNominated,
			isGrammyWinner = isGrammyWinner, weeksOfGrammyBump = weeksOfGrammyBump, initialLaunchStock = initialLaunchStock,
			perceivedQualityMultiplier = perceivedQualityMultiplier
		};
		runtime.regionalData = (Regions ?? new List<RegionalRecordSaveData>())
			.ToDictionary(region => region.regionId, region => region.ToRegional(), StringComparer.Ordinal);
		return runtime;
	}
}

// ============================================================================
// THE DESK -- masters, releases, pressing, and the trunk.
// ============================================================================

public sealed class MasterSaveData {
	public RecordSaveData Record { get; set; }
	public string ArtistId { get; set; }
	public string SongTitle { get; set; }
	public float ProductionCost { get; set; }
	public int Year { get; set; }
	public int Month { get; set; }
	public int Day { get; set; }
	public bool Scheduled { get; set; }
	public bool Released { get; set; }

	public static MasterSaveData From(PlayerDesk.Master m) => new() {
		Record = RecordSaveData.From(m.Record), ArtistId = m.ArtistId, SongTitle = m.SongTitle,
		ProductionCost = m.ProductionCost, Year = m.Cut.year, Month = m.Cut.month, Day = m.Cut.day,
		Scheduled = m.Scheduled, Released = m.Released
	};

	public PlayerDesk.Master ToMaster() => new() {
		Record = Record.ToRecord(), ArtistId = ArtistId, SongTitle = SongTitle, ProductionCost = ProductionCost,
		Cut = new GameDate(Year, Month, Day), Scheduled = Scheduled, Released = Released
	};

	/// <summary>The master's stable key -- its record id -- used to re-link planned releases on load.</summary>
	public string Key => Record?.recordId;
}

public sealed class PlannedReleaseSaveData {
	public string ASideRecordId { get; set; }
	public string BSideRecordId { get; set; }
	public bool Dated { get; set; }
	public int Year { get; set; }
	public int Month { get; set; }
	public int Day { get; set; }
	public float MarketingBudget { get; set; }

	public static PlannedReleaseSaveData From(PlayerDesk.PlannedRelease p) => new() {
		ASideRecordId = p.Master?.Record?.recordId, BSideRecordId = p.BSide?.Record?.recordId,
		Dated = p.Dated, Year = p.Date.year, Month = p.Date.month, Day = p.Date.day, MarketingBudget = p.MarketingBudget
	};
}

public sealed class PressStockSaveData {
	public string RecordId { get; set; }
	public int Remaining { get; set; }
	public int TotalPressed { get; set; }
	public float TotalSpent { get; set; }
}

public sealed class PressOrderSaveData {
	public string RecordId { get; set; }
	public int Quantity { get; set; }
	public float Cost { get; set; }
	public int OrderedYear { get; set; }
	public int OrderedMonth { get; set; }
	public int OrderedDay { get; set; }
	public int ArrivesYear { get; set; }
	public int ArrivesMonth { get; set; }
	public int ArrivesDay { get; set; }
}

public sealed class ConsignmentLotSaveData {
	public string CityId { get; set; }
	public string RecordId { get; set; }
	public int Remaining { get; set; }
	public int Placed { get; set; }
	public int DaysSinceRestock { get; set; }
}
