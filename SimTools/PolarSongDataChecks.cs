using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

/// <summary>Ownership and legacy-save probes, run by --polar-song-data-check. No fit tuning.</summary>
public static class PolarSongDataChecks {
	public static void Run() {
		var original = new WorldSaveData();
		CompositionCatalogService.CaptureWorld(original);
		try {
			var song = new SongComposition { songId = "polar_fixture", title = "One composition",
				primaryGenre = Genre.Soul, secondaryGenre = Genre.Blues,
				genreTagIds = new[] { "HornSection", "Romantic", "CustomTag" },
				demoTaxonomy = new SongTaxonomy { archetype = SongArchetype.DeepSoulPleader,
					primaryGenre = Genre.Soul, lyricModes = new[] { SongLyricMode.RomanticAddress } },
				credits = new() { new SongwriterCredit { writerId = "original-writer", share = 1f } },
				rights = new SongRightsProfile { controlType = PublishingControlType.ArtistControlled, controllerArtistId = "original-writer" } };
			var other = new SongComposition { songId = "polar_flip", primaryGenre = Genre.Folk };
			var fixture = new WorldSaveData { Composition = new CompositionSaveData {
				Songs = new() { [song.songId] = song, [other.songId] = other } } };
			CompositionCatalogService.RehydrateWorld(fixture);
			var first = new Record { recordId = "polar_first", songId = song.songId, primaryGenre = Genre.Soul,
				genreTagIds = (string[])song.genreTagIds.Clone(), releaseDate = new GameDate(1960, 1, 1) };
			PolarSongMetadataService.RegisterRecord(first, song, first.releaseDate);
			Check(first.masterId == first.recordId, "single ID linkage");
			var master = PolarSongMetadataService.Get(first.masterId);
			Check(master.taxonomy.archetype == SongArchetype.DeepSoulPleader, "demo seeds first taxonomy");
			master.taxonomy.lyricModes[0] = SongLyricMode.DemandBoast;
			Check(song.demoTaxonomy.lyricModes[0] == SongLyricMode.RomanticAddress, "taxonomy clone isolation");
			var reuse = new AlbumTrack { sourceRecordId = first.recordId, songId = song.songId, genre = Genre.Soul };
			PolarSongMetadataService.RegisterTrack(reuse, song, "unused", "other", new GameDate(1961, 1, 1));
			Check(reuse.masterId == first.masterId && ReferenceEquals(master, PolarSongMetadataService.Get(reuse.masterId)), "album reuses master");
			var cut = new AlbumTrack { songId = song.songId, genre = Genre.Soul };
			PolarSongMetadataService.RegisterTrack(cut, song, "polar_album_t0", "band", new GameDate(1961, 1, 1));
			Check(cut.masterId != first.masterId, "album-only performance gets new master");
			var cover = new Record { recordId = "polar_cover", songId = song.songId, originalRecordId = first.recordId,
				primaryGenre = Genre.RockAndRoll };
			PolarSongMetadataService.RegisterRecord(cover, song, new GameDate(1962, 1, 1));
			Check(PolarSongMetadataService.Get(cover.masterId).parentRecordingId == first.masterId, "cover immediate lineage");
			Check(!PolarSongMetadataService.FreezePlasticity(song, cover.masterId, 1f, "later-master"), "later master cannot establish plasticity");
			Check(PolarSongMetadataService.FreezePlasticity(song, first.masterId, 0f, "probe"), "zero plasticity commits");
			Check(!PolarSongMetadataService.FreezePlasticity(song, cover.masterId, 1f, "cover"), "cover cannot refreeze");
			Check(song.plasticity == 0f && song.plasticitySourceMasterId == first.masterId, "zero distinguished from missing");
			Check(song.credits[0].writerId == "original-writer" && song.rights.controllerArtistId == "original-writer", "rights untouched");
			Check(!PolarSongMetadataService.FreezePlasticity(other, "cancelled-preview", 0.5f, "preview"), "uncommitted preview cannot freeze");
			Check(other.plasticity == null && !other.plasticityFrozen, "absent plasticity preserved");

			var tags = PolarSongMetadataService.ClassifyLegacyTags(new[] { "HornSection", "WallOfSound", "Christmas", "British", "Romantic", "Instrumental", "CustomTag" });
			Check(tags.instrumentation.SequenceEqual(new[] { "HornSection" }) && tags.production.SequenceEqual(new[] { "WallOfSound" }), "production classification");
			Check(tags.unclassified.SequenceEqual(new[] { "Romantic", "Instrumental", "CustomTag" }), "ambiguous and unknown tags retained");
			Check(tags.release.Single() == "Christmas" && tags.scene.Single() == "British", "release and scene classification");
			Check(PolarSongMetadataService.ClassifyLegacyTags(new[] { "motown", "horn-section", "christmas" }).scene.Single() == "motown", "saved canonical string IDs classified");
			var album = new Record { recordId = "polar_album", songId = song.songId, format = ReleaseFormat.Album };
			PolarSongMetadataService.RegisterRecord(album, song);
			Check(album.masterId == null, "album has no one-song profile");

			var flip = new Record { recordId = "polar_b", songId = other.songId, primaryGenre = Genre.Folk };
			PolarSongMetadataService.RegisterRecord(flip, other);
			first.bSideMasterId = flip.masterId;
			first.bSideSongId = other.songId;
			first.bSideIsPlugSide = true;
			Check(first.PlugMasterId == flip.masterId && first.masterId == "polar_first", "flip changes plug reference only");
			var json = JsonSerializer.Serialize(RecordSaveData.From(first), SaveGameService.TestJsonOptions);
			var restoredRecord = JsonSerializer.Deserialize<RecordSaveData>(json, SaveGameService.TestJsonOptions).ToRecord();
			Check(restoredRecord.PlugMasterId == flip.masterId && restoredRecord.songId == song.songId, "player DTO retains both sides");
			var legacyRecord = JsonSerializer.Deserialize<RecordSaveData>("{\"recordId\":\"legacy_single\",\"songId\":\"polar_fixture\"}", SaveGameService.TestJsonOptions).ToRecord();
			PolarSongMetadataService.MigrateRecords(new[] { legacyRecord });
			Check(legacyRecord.masterId == "legacy_single" && PolarSongMetadataService.Get(legacyRecord.masterId).isLegacyInferred, "old player record migration");
			var oldFlip = new Record { recordId = "legacy_flip", songId = song.songId, bSideSongId = other.songId,
				bSideIsPlugSide = true, title = "Original B", primaryGenre = Genre.Folk,
				bSideTitle = "Original A", bSidePrimaryGenre = Genre.Soul };
			PolarSongMetadataService.MigrateRecords(new[] { oldFlip });
			Check(PolarSongMetadataService.Get(oldFlip.masterId).title == "Original A" &&
				PolarSongMetadataService.Get(oldFlip.masterId).taxonomy.primaryGenre == Genre.Soul, "legacy flipped A metadata");
			Check(PolarSongMetadataService.Get(oldFlip.bSideMasterId).title == "Original B" &&
				PolarSongMetadataService.Get(oldFlip.bSideMasterId).taxonomy.primaryGenre == Genre.Folk, "legacy flipped B metadata");

			var archived = new AlbumTrack { sourceRecordId = first.recordId, songId = song.songId };
			var oldAlbum = new Record { recordId = "legacy_album", format = ReleaseFormat.Album,
				album = new Album { nonSingleTracks = new[] { new AlbumTrack { songId = song.songId } } } };
			PolarSongMetadataService.MigrateRecords(new[] { oldAlbum }, new Dictionary<string, AlbumTrack> { [first.recordId] = archived });
			Check(oldAlbum.album.nonSingleTracks[0].masterId == "legacy_album:track:0", "old album cut deterministic ID");
			Check(archived.masterId == first.masterId, "retired archive preserves linkage");
			var saved = new WorldSaveData { Records = new() { new RecordRuntimeData(first), new RecordRuntimeData(oldAlbum) } };
			PolarSongMetadataService.WarmCommittedProfiles(PolarSongTable.Current);
			var cached = master.cachedProfile;
			PolarSongMetadataService.WarmCommittedProfiles(PolarSongTable.Current);
			Check(ReferenceEquals(cached, master.cachedProfile), "unchanged inputs reuse profile cache");
			master.taxonomy.vocalPresence = SongVocalPresence.Instrumental;
			PolarSongMetadataService.WarmCommittedProfiles(PolarSongTable.Current);
			Check(!ReferenceEquals(cached, master.cachedProfile) && master.cachedProfile[SongAxis.VocalPower] == 0, "taxonomy invalidates cache");
			master.taxonomy.vocalPresence = SongVocalPresence.Unknown;
			PolarSongMetadataService.WarmCommittedProfiles(PolarSongTable.Current);
			CompositionCatalogService.CaptureWorld(saved);
			string a = JsonSerializer.Serialize(saved, SaveGameService.TestJsonOptions);
			var loaded = JsonSerializer.Deserialize<WorldSaveData>(a, SaveGameService.TestJsonOptions);
			CompositionCatalogService.RehydrateWorld(loaded);
			CompositionCatalogService.CaptureWorld(loaded);
			string b = JsonSerializer.Serialize(loaded, SaveGameService.TestJsonOptions);
			Check(a == b, "composition/master/track world round-trip");
			var loadedSong = CompositionCatalogService.GetSong(song.songId);
			Check(loadedSong.plasticityFrozen && loadedSong.plasticity == 0f, "frozen state round-trip");
			Check(PolarSongMetadataService.Get(first.masterId).cachedProfile != null &&
				PolarSongMetadataService.Get(first.masterId).cachedProfile.mappingVersion == PolarSongTable.Current.Version, "versioned profile cache round-trip");
			CompositionCatalogService.RehydrateWorld(JsonSerializer.Deserialize<WorldSaveData>(a, SaveGameService.TestJsonOptions));
			Check(PolarSongMetadataService.Get(first.masterId).taxonomy.lyricModes[0] == SongLyricMode.DemandBoast, "repeat load stable taxonomy");
			Console.WriteLine("POLAR_SONG_DATA_PASS ownership=ok legacy=ok saves=ok tags=ok");
		} finally { CompositionCatalogService.RehydrateWorld(original); }
	}
	private static void Check(bool pass, string reason) {
		if (!pass) throw new InvalidOperationException("POLAR_SONG_DATA_FAIL: " + reason);
	}
}
