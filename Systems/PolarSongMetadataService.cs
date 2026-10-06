using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

/// <summary>Data-only master registry. No RNG, fitting, selection, rights routing or market mutation.</summary>
public static class PolarSongMetadataService {
	private static readonly Dictionary<string, SongMasterMetadata> masters = new(StringComparer.Ordinal);
	private static readonly Dictionary<string, List<SongMasterMetadata>> mastersBySong = new(StringComparer.Ordinal);
	// Shadow metadata maintenance only. This flag cannot enable selection or chart behavior.
	public static bool ShadowProfilesEnabled;
	public static IReadOnlyDictionary<string, SongMasterMetadata> Masters => masters;
	public static SongMasterMetadata Get(string id) => !string.IsNullOrEmpty(id) && masters.TryGetValue(id, out var m) ? m : null;
	public static void Reset() { masters.Clear(); mastersBySong.Clear(); }
	public static Dictionary<string, SongMasterMetadata> Capture() => new(masters, StringComparer.Ordinal);
	public static void Restore(Dictionary<string, SongMasterMetadata> saved) {
		Reset();
		foreach (var pair in saved ?? new()) {
			if (pair.Value != null && pair.Key == pair.Value.masterId) { masters.Add(pair.Key, pair.Value); IndexMaster(pair.Value); }
		}
	}

	public static PolarSongTags ClassifyLegacyTags(string[] ids) {
		var production = new List<string>(); var instrumentation = new List<string>();
		var scene = new List<string>(); var release = new List<string>(); var unknown = new List<string>();
		foreach (string id in ids ?? Array.Empty<string>()) {
			switch (id) {
				case "HornSection": case "horn-section": case "Orchestral": case "orchestral": instrumentation.Add(id); break;
				case "WallOfSound": case "wall-of-sound": case "LoFi": case "lo-fi": production.Add(id); break;
				case "Motown": case "motown": case "GirlGroup": case "girl-group": case "British": case "british":
				case "Skiffle": case "skiffle": case "Jamaican": case "jamaican": case "Rockabilly": case "rockabilly": scene.Add(id); break;
				case "Seasonal": case "seasonal": case "Christmas": case "christmas": case "Halloween": case "halloween":
				case "Summer": case "summer": release.Add(id); break;
				// Theme is not delivery. Instrumental is retained until a performer-presence rule is applied.
				default: unknown.Add(id); break;
			}
		}
		return new PolarSongTags { production = production.ToArray(), instrumentation = instrumentation.ToArray(),
			scene = scene.ToArray(), release = release.ToArray(), unclassified = unknown.ToArray() };
	}

	public static void EnsureComposition(SongComposition song) {
		CompositionShapeVariation.Ensure(song);
		if (song == null || song.demoTaxonomy != null) return;
		song.demoTaxonomy = new SongTaxonomy { primaryGenre = song.primaryGenre, secondaryGenre = song.secondaryGenre,
			tags = ClassifyLegacyTags(song.genreTagIds) };
	}

	/// <summary>Freeze only at a committed, explicitly derived first master. No preview-time writes.
	/// Null plasticity stays pending until the shadow deriver supplies a value; never invent tuning.</summary>
	public static bool FreezePlasticity(SongComposition song, string committedMasterId, float value, string provenance) {
		var master = Get(committedMasterId);
		if (song == null || song.plasticityFrozen || master == null || master.songId != song.songId ||
			song.firstCommittedMasterId != committedMasterId) return false;
		if (!float.IsFinite(value) || value < 0f || value > 1f) throw new ArgumentOutOfRangeException(nameof(value));
		song.plasticity = value; song.plasticityFrozen = true;
		song.plasticitySourceMasterId = committedMasterId; song.plasticityProvenance = provenance;
		return true;
	}

	public static void RegisterRecord(Record record, SongComposition song, GameDate date = default, bool legacy = false, PolarArrangementProposal proposal = null) {
		if (record == null || song == null || record.format != ReleaseFormat.Single || string.IsNullOrEmpty(record.recordId)) return;
		EnsureComposition(song);
		if (string.IsNullOrEmpty(record.masterId)) record.masterId = record.recordId;
		var taxonomy = song.demoTaxonomy.Copy();
		// A legacy flipped disc stores its original A-side performance in the flip fields.
		bool flipped = legacy && record.bSideIsPlugSide && !string.IsNullOrEmpty(record.bSideSongId);
		taxonomy.primaryGenre = flipped ? record.bSidePrimaryGenre : record.primaryGenre;
		taxonomy.secondaryGenre = record.secondaryGenre;
		taxonomy.tags = ClassifyLegacyTags(record.genreTagIds);
		if (proposal != null) taxonomy = proposal.taxonomy.Copy();
		Register(record.masterId, song, record.artistId, flipped ? record.bSideTitle : record.title, proposal?.parentRecordingId ?? record.originalRecordId, date,
			taxonomy, legacy, proposal);
	}

	public static void RegisterTrack(AlbumTrack track, SongComposition song, string plannedId, string artistId, GameDate date, bool legacy = false, PolarArrangementProposal proposal = null) {
		if (track == null || song == null) return;
		EnsureComposition(song);
		if (string.IsNullOrEmpty(track.masterId)) track.masterId = !string.IsNullOrEmpty(track.sourceRecordId) ? track.sourceRecordId : plannedId;
		if (string.IsNullOrEmpty(track.masterId)) return;
		var taxonomy = song.demoTaxonomy.Copy(); taxonomy.primaryGenre = track.genre;
		if (proposal != null) taxonomy = proposal.taxonomy.Copy();
		Register(track.masterId, song, artistId, track.title, proposal?.parentRecordingId ?? track.originalRecordId, date,
			taxonomy, legacy, proposal);
	}

	private static void Register(string id, SongComposition song, string artist, string title, string parent, GameDate date, SongTaxonomy taxonomy, bool legacy, PolarArrangementProposal proposal = null) {
		if (proposal != null && (proposal.songId != song.songId || proposal.plannedMasterId != id))
			throw new InvalidOperationException("Arrangement proposal does not identify this master/composition.");
		if (masters.TryGetValue(id, out var existing)) {
			if (existing.songId != song.songId) throw new InvalidOperationException($"Master {id} cannot change composition.");
			return; // Reuse is not a new performance or taxonomy mutation.
		}
		masters.Add(id, new SongMasterMetadata { masterId = id, songId = song.songId, artistId = artist, title = title,
			parentRecordingId = parent, recordingYear = date.year, recordingMonth = date.month, recordingDay = date.day,
			taxonomy = taxonomy, isLegacyInferred = legacy,
			realizedFit = proposal == null ? null : PolarFitSnapshot.From(proposal.fit, proposal.mappingVersion) });
		IndexMaster(masters[id]);
		if (!legacy && string.IsNullOrEmpty(song.firstCommittedMasterId)) song.firstCommittedMasterId = id;
		if ((ShadowProfilesEnabled || PolarSongBehavior.UsePolarFitSelection) && !legacy) WarmComposition(song, PolarSongTable.Current);
	}

	public static void WarmCommittedProfiles(PolarSongTable table) {
		foreach (var songId in mastersBySong.Keys) {
			var song = CompositionCatalogService.GetSong(songId);
			if (song != null) WarmComposition(song, table);
		}
	}
	private static void WarmComposition(SongComposition song, PolarSongTable table) {
		var first = Get(song.firstCommittedMasterId);
		if (!song.plasticityFrozen && first != null) FreezePlasticity(song, first.masterId,
			SongProfileDeriver.Derive(song, first.taxonomy, table).plasticity, "first-master:" + table.Version);
		foreach (var master in mastersBySong.GetValueOrDefault(song.songId) ?? new()) {
			// Existing recording shapes are historical facts. H affects future resolutions only.
			if (CompositionShapeVariation.ActiveVersion > 0 && master.cachedProfile != null) continue;
			string inputs = JsonSerializer.Serialize(new { master.taxonomy, song.wordDensity, song.defaultMeter, song.defaultForm, song.repertoireVariation,
				song.plasticity, song.plasticityFrozen, table.ContentFingerprint }, new JsonSerializerOptions { IncludeFields = true });
			string fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(inputs)));
			if (master.cachedProfile != null && master.profileFingerprint == fingerprint) continue;
			master.cachedProfile = SongProfileDeriver.Derive(song, master.taxonomy, table);
			master.profileFingerprint = fingerprint;
		}
	}
	private static void IndexMaster(SongMasterMetadata master) {
		if (string.IsNullOrEmpty(master.songId)) return;
		if (!mastersBySong.TryGetValue(master.songId, out var list)) { list = new(); mastersBySong.Add(master.songId, list); }
		list.Add(master);
	}

	/// <summary>Backfill only missing linkage. Order is stable; absent chronology is explicitly inferred.</summary>
	public static void MigrateRecords(IEnumerable<Record> records, IDictionary<string, AlbumTrack> archives = null) {
		foreach (var record in (records ?? Array.Empty<Record>()).Where(r => r != null).OrderBy(r => r.recordId, StringComparer.Ordinal)) {
			var song = CompositionCatalogService.GetSong(record.songId);
			RegisterRecord(record, song, record.releaseDate, legacy: true);
			if (!string.IsNullOrEmpty(record.bSideSongId)) {
				var flipSong = CompositionCatalogService.GetSong(record.bSideSongId);
				if (flipSong != null) {
					if (string.IsNullOrEmpty(record.bSideMasterId)) record.bSideMasterId = $"{record.recordId}:b-side";
					EnsureComposition(flipSong);
					Register(record.bSideMasterId, flipSong, record.artistId, record.bSideIsPlugSide ? record.title : record.bSideTitle, null, record.releaseDate,
						new SongTaxonomy { primaryGenre = record.bSideIsPlugSide ? record.primaryGenre : record.bSidePrimaryGenre,
							secondaryGenre = flipSong.secondaryGenre, tags = ClassifyLegacyTags(flipSong.genreTagIds) }, true);
				}
			}
			if (record.album == null) continue;
			int index = 0;
			foreach (var track in record.album.GetAllTracks()) {
				RegisterTrack(track, CompositionCatalogService.GetSong(track?.songId), $"{record.recordId}:track:{index++}", record.artistId, record.releaseDate, true);
			}
		}
		foreach (var pair in (archives ?? new Dictionary<string, AlbumTrack>()).OrderBy(p => p.Key, StringComparer.Ordinal))
			RegisterTrack(pair.Value, CompositionCatalogService.GetSong(pair.Value?.songId), pair.Key, null, pair.Value?.releaseDate ?? default, true);
		// Legacy chronology is inferred from surviving release dates, never future outcomes or dictionary order.
		foreach (var group in masters.Values.GroupBy(m => m.songId)) {
			var song = CompositionCatalogService.GetSong(group.Key);
			if (song == null || !string.IsNullOrEmpty(song.firstCommittedMasterId)) continue;
			song.firstCommittedMasterId = group.OrderBy(m => m.recordingYear > 0 ? m.recordingYear : int.MaxValue)
				.ThenBy(m => m.recordingMonth).ThenBy(m => m.recordingDay).ThenBy(m => m.masterId, StringComparer.Ordinal).First().masterId;
		}
		if (ShadowProfilesEnabled) WarmCommittedProfiles(PolarSongTable.Current);
	}
}
