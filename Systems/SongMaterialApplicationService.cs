using Godot;

/// <summary>
/// Applies a chosen <see cref="SelectedSongMaterial"/> to a Record (Publishing & Cover-Song Phase 1):
/// stamps composition rights and master lineage, then realizes the performance without RNG.
/// Enabled recordings blend the raw composition hook with performance, apply a gradual ceiling,
/// and lose hook/production only through Capability. Disabled recordings retain the legacy blend.
/// </summary>
public static class SongMaterialApplicationService {
	public static void Apply(Record record, SelectedSongMaterial material, AILabel label, SimulatedArtist artist) {
		if (record == null || material?.Song == null) return;

		// Identity + credit snapshot + seasonal-tag inheritance (shared with the Phase 0 path).
		CompositionCatalogService.ApplyToRecord(
			record, material.Song, material.Source, material.IsCover,
			material.OriginalRecordId, material.OriginalArtistId,
			material.FamiliarityAtRelease, material.ArrangementOriginality, material.ProfessionalPolish,
			PolarSongBehavior.UsePolarFitSelection ? material.PolarProposal : null);

		// Phase 3b support: a label may capture publishing on commissioned professional material through
		// its own arm (majors most). Deterministic, gated; only rewrites the record's publishing control.
		PublishingCaptureService.MaybeCapture(record, material, label);
		if (PolarSongBehavior.UsePolarFitSelection && !PolarSongBehavior.AuditLegacyRealization && material.PolarProposal != null) {
			// Composition hook remains raw material, independent of performer capability.
			float execution = PolarSongBehavior.ExecutionFactor(material.PolarProposal.fit.Capability);
			record.hookStrength = Mathf.Clamp(PolarSongBehavior.RealizeHook(record.hookStrength, material.Song.commercialHook) * execution, 0, 1);
			record.productionQuality = Mathf.Clamp(record.productionQuality * execution, 0, 1);
			record.arrangementOriginality = material.PolarProposal.fit.Stretch;
			return;
		}

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
	/// Disabled behavior stamps identity only. Enabled cuts use the same realization as singles;
	/// their latent quality changes by the hook/production deltas used by single quality. Promotion
	/// reuses this performance and must not apply the material or execution loss a second time.
	/// </summary>
	public static void ApplyIdentityToAlbumTrack(AlbumTrack track, SelectedSongMaterial material) {
		if (track == null || material?.Song == null) return;
		SongComposition song = material.Song;
		CompositionCatalogService.AdmitUnpublished(song,song.originYear,material.PolarProposal?.plannedMasterId??track.masterId);
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
		if (PolarSongBehavior.UsePolarFitSelection && !PolarSongBehavior.AuditLegacyRealization && material.PolarProposal != null) {
			float execution = PolarSongBehavior.ExecutionFactor(material.PolarProposal.fit.Capability);
			float priorHook = track.hookStrength, priorProduction = track.productionQuality;
			track.hookStrength = Mathf.Clamp(PolarSongBehavior.RealizeHook(priorHook, song.commercialHook) * execution, 0, 1);
			track.productionQuality = Mathf.Clamp(priorProduction * execution, 0, 1);
			track.quality = PolarSongBehavior.AuditOriginalRealization ? track.quality * execution :
				Mathf.Clamp(track.quality + PolarSongTable.Current.N("recordingQualityHookWeight") * (track.hookStrength - priorHook)
					+ PolarSongTable.Current.N("recordingQualityProductionWeight") * (track.productionQuality - priorProduction), 0, 1);
		}
	}
}
