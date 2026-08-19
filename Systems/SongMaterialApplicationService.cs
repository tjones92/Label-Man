using Godot;

/// <summary>
/// Applies a chosen <see cref="SelectedSongMaterial"/> to a Record (Publishing & Cover-Song Phase 1):
/// stamps the song identity + credit snapshot, then blends the composition into the record's realized
/// performance attributes. A professional song lifts hook toward its studio-ready value; a cover keeps
/// the performance hook but takes the song's (lower) composition originality, offset by arrangement
/// originality. NO RNG here -- the material already carries deterministic expected values.
///
/// Phase 1 changes chart-facing attributes (hook/originality/production) but NOT money: settlement
/// still keys off SimulatedArtist.labelOwnsPublishing until Phase 3.
/// </summary>
public static class SongMaterialApplicationService {
	public static void Apply(Record record, SelectedSongMaterial material, AILabel label, SimulatedArtist artist) {
		if (record == null || material?.Song == null) return;

		// Identity + credit snapshot + seasonal-tag inheritance (shared with the Phase 0 path).
		CompositionCatalogService.ApplyToRecord(
			record, material.Song, material.Source, material.IsCover,
			material.OriginalRecordId, material.OriginalArtistId,
			material.FamiliarityAtRelease, material.ArrangementOriginality, material.ProfessionalPolish);

		// The existing generated hook is the performance/recording variance; blend it toward the
		// composition's expected hook by how much authority the source's song carries.
		float performanceHook = record.hookStrength;
		float sourceAuthority = material.Source switch {
			SongMaterialSource.ArtistWritten => 0.50f,
			SongMaterialSource.ArtistCowrittenWithProfessional => 0.60f,
			SongMaterialSource.LabelStaffWriter => 0.68f,
			SongMaterialSource.ExternalProfessional => 0.68f,
			SongMaterialSource.CoverRecentHit => 0.72f,
			SongMaterialSource.CoverCatalogSong => 0.65f,
			SongMaterialSource.CoverStandard => 0.62f,
			SongMaterialSource.TraditionalPublicDomain => 0.50f,
			SongMaterialSource.AdaptedTraditional => 0.55f,
			_ => 0.50f
		};
		record.hookStrength = Mathf.Clamp(
			Mathf.Lerp(performanceHook, material.ExpectedHook, sourceAuthority) + material.ProfessionalPolish * 0.04f,
			0f, 1f);

		// Covers are less composition-original but may carry arrangement originality.
		float creativity = artist.members.Count > 0 ? artist.members[0].creativity : 0.4f;
		float sourceOriginality = material.Source switch {
			SongMaterialSource.ArtistWritten =>
				Mathf.Clamp(material.Song.originality * 0.75f + creativity * 0.25f, 0f, 1f),
			SongMaterialSource.ArtistCowrittenWithProfessional =>
				Mathf.Clamp(material.Song.originality * 0.65f + creativity * 0.20f, 0f, 1f),
			SongMaterialSource.LabelStaffWriter or SongMaterialSource.ExternalProfessional =>
				Mathf.Clamp(material.Song.originality * 0.65f, 0f, 1f),
			SongMaterialSource.CoverRecentHit or SongMaterialSource.CoverCatalogSong or SongMaterialSource.CoverStandard =>
				Mathf.Clamp(material.ArrangementOriginality * 0.70f + creativity * 0.15f, 0f, 1f),
			SongMaterialSource.TraditionalPublicDomain or SongMaterialSource.AdaptedTraditional =>
				Mathf.Clamp(material.ArrangementOriginality * 0.80f + creativity * 0.15f, 0f, 1f),
			_ => record.originality
		};
		record.originality = Mathf.Clamp(Mathf.Lerp(record.originality, sourceOriginality, 0.65f), 0f, 1f);

		// A studio-ready professional song records a touch better.
		record.productionQuality = Mathf.Clamp(record.productionQuality + material.ProfessionalPolish * 0.035f, 0f, 1f);
	}

	/// <summary>
	/// Stamps a chosen material's song identity onto a non-single AlbumTrack (Publishing & Cover-Song
	/// §15: give every album cut a composition origin, so a retired track keeps its song biography).
	/// IDENTITY ONLY -- it deliberately does NOT blend the track's quality/hook/production/danceability.
	/// Those already drive album pooledAppeal and lead-single (promo) selection; leaving them untouched
	/// keeps the album economy byte-identical while adding the missing song origin. The performance
	/// blend is the released single's job -- a promo lifted off this track re-selects its own material.
	/// </summary>
	public static void ApplyIdentityToAlbumTrack(AlbumTrack track, SelectedSongMaterial material) {
		if (track == null || material?.Song == null) return;
		SongComposition song = material.Song;
		track.songId = song.songId;
		track.songSource = material.Source;
		track.isCover = material.IsCover;
		track.originalRecordId = material.OriginalRecordId;
		track.originalArtistId = material.OriginalArtistId;
		track.publisherId = song.rights.publisherId;

		int n = song.credits.Count;
		track.songwriterNames = new string[n];
		for (int i = 0; i < n; i++) track.songwriterNames[i] = song.credits[i].writerName;

		track.compositionQuality = song.compositionQuality;
		track.compositionHook = song.commercialHook;
		track.lyricQuality = song.lyricQuality;
		track.songFamiliarityAtRelease = material.FamiliarityAtRelease;
		track.standardDurability = song.standardDurability;
		track.arrangementOriginality = material.ArrangementOriginality;
	}
}
