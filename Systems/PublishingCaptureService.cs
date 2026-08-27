/// <summary>
/// Publishing &amp; Cover-Song Phase 3b support: label-affiliate publishing capture. Resolves the gap the
/// routing probe exposed -- with every publisher unaffiliated, a major recording commissioned staff
/// material leaked the whole publishing slice, so live routing hit Majors HARDEST (backwards from the
/// "majors capture more via affiliates" intent).
///
/// Model: only COMMISSIONED professional / staff material can be captured in-house. A cover, standard,
/// or public-domain song keeps its publisher (compulsory mechanical -- recording it never transfers the
/// copyright), and artist-originals already route to the artist or the owning label. When a label
/// commissions professional material it may run it through its own publishing arm; the capture rate is
/// tier-scaled (majors have the in-house shops), flipping the record to a <c>LabelBuyout</c> controlled
/// by the recording label so settlement keeps the slice instead of leaking it.
///
/// Determinism: a pure stable hash over (artistId, recordId), NEVER the global GD stream -- same
/// discipline as SongMaterialSelectionService, so it never perturbs the replay schedule. Gated by
/// <see cref="Enabled"/> (CLI --enable-affiliate-capture); off by default so telemetry runs are unchanged.
/// Rates are provisional and re-tuned against the decade leakage trajectory before 3b ships.
/// </summary>
public static class PublishingCaptureService {
	// Publishing & Cover-Song Directive Part II: live by default alongside PublishingRoutingService --
	// without affiliate capture, live routing hits Majors hardest (the exact backwards result this
	// service was built to fix; see the class doc above). --disable-affiliate-capture reproduces the
	// old off-by-default baseline.
	public static bool Enabled = true;

	public static void MaybeCapture(Record record, SelectedSongMaterial material, AILabel label) {
		if (!Enabled || record == null || label == null || material == null) return;
		// Only commissioned professional / staff songs are capturable; covers/standards/PD/originals are not.
		if (material.Source != SongMaterialSource.ExternalProfessional &&
			material.Source != SongMaterialSource.LabelStaffWriter) return;

		float captureRate = label.tier switch {
			LabelTier.Major => 0.75f,
			LabelTier.MidTier => 0.45f,
			LabelTier.Boutique => 0.30f,
			LabelTier.Independent => 0.20f,
			LabelTier.Small => 0.10f,
			_ => 0.25f
		};
		if (StableUnit(record.artistId, record.recordId, "pubcapture") >= captureRate) return;

		// The label's publishing arm controls this master's composition: keep the slice in-house.
		record.publishingControl = PublishingControlType.LabelBuyout;
		record.publishingControllerLabelId = label.labelId;
	}

	// FNV-1a over (artistId|recordId|salt) folded to [0,1). Mirrors SongMaterialSelectionService.
	private static float StableUnit(string artistId, string recordId, string salt) {
		const ulong offset = 14695981039346656037UL;
		const ulong prime = 1099511628211UL;
		ulong hash = offset;
		foreach (char value in $"{artistId}|{recordId}|{salt}|PubCaptureV1") { hash ^= value; hash *= prime; }
		return (hash >> 40) * (1f / 16777216f);
	}
}
