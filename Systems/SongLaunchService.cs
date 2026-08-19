using Godot;

/// <summary>
/// Publishing & Cover-Song Phase 2: a small, bounded LAUNCH lift for familiar material. A known
/// standard or a recent-hit cover starts with a head-start in awareness/radio because the public
/// already knows the song -- but it is NOT a weekly chart steroid. The lift applies only at release
/// (in ChartManager.PromoteRecordAI) and is capped hard.
///
/// DETERMINISM: pure function of the record's release-time fields -- no RNG. It is added AFTER the
/// existing GD.RandRange launch draws so the RNG schedule is unchanged. Kill-switch:
/// <see cref="LaunchInputEnabled"/> false makes Phase 2 inert (charts revert to material-only).
/// </summary>
public static class SongLaunchService {
	public static bool LaunchInputEnabled = true;

	/// <summary>
	/// Bounded regional awareness head-start from song familiarity. Recent-hit covers help most, then
	/// catalog covers, then standards; originals get nothing (familiarity 0). Capped at +0.08.
	/// </summary>
	public static float GetSongAwarenessLift(Record record, int year) {
		if (!LaunchInputEnabled || record == null) return 0f;
		float familiarity = Mathf.Clamp(record.songFamiliarityAtRelease, 0f, 1f);
		float durability = Mathf.Clamp(record.standardDurability, 0f, 1f);
		float sourceMultiplier = record.songSource switch {
			SongMaterialSource.CoverRecentHit => 0.070f,
			SongMaterialSource.CoverCatalogSong => 0.045f,
			SongMaterialSource.CoverStandard => 0.040f,
			SongMaterialSource.TraditionalPublicDomain => 0.025f,
			SongMaterialSource.AdaptedTraditional => 0.030f,
			_ => 0f
		};
		float standardBonus = record.songSource == SongMaterialSource.CoverStandard ? durability * 0.012f : 0f;
		return Mathf.Clamp(familiarity * sourceMultiplier + standardBonus, 0f, 0.08f);
	}

	/// <summary>
	/// Small radio head-start: a studio-ready professional song, or a familiar recent-hit/standard
	/// cover, seeds a touch more airplay. The caller scales it by the same launch factors as the base
	/// radio seed.
	/// </summary>
	public static float GetRadioLift(Record record) {
		if (!LaunchInputEnabled || record == null) return 0f;
		return record.songSource switch {
			SongMaterialSource.ExternalProfessional or SongMaterialSource.LabelStaffWriter =>
				record.professionalPolish * 0.025f,
			SongMaterialSource.CoverRecentHit => record.songFamiliarityAtRelease * 0.030f,
			SongMaterialSource.CoverStandard => record.songFamiliarityAtRelease * 0.015f,
			_ => 0f
		};
	}
}
