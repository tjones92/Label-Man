using System.Collections.Generic;

/// <summary>
/// Phase 2 aggregation: turns the reporter panel's per-station playlists into the single
/// reporterRadioPlay value the ChartManager blends with the tail (design doc a 3.5). Kept in its
/// own partial so the Phase-1 roster code and this Phase-2 producer stay separable.
///
/// PHASE NOTE: through the Phase-2a plumbing swap the ChartManager holds REPORTER_PANEL_WEIGHT at 0
/// and never calls this; and until StationNetwork.UpdatePlaylists() runs (Phase 3) the playlists are
/// empty, so this returns 0. It is wired now only so the combine site compiles and the reporter term
/// has a home to grow into.
/// </summary>
public sealed partial class StationNetwork {

	// Spin weight per tier, normalized so High == 1.0 (the reference the tail's 0..1 shares against).
	private static float SpinWeight(SpinTier tier) => tier switch {
		SpinTier.High => 1.0f,
		SpinTier.Mid => 0.5f,
		SpinTier.Light => 0.2f,
		_ => 0f
	};

	/// <summary>
	/// Reporter-panel airplay for a record in a region, 0..1. Reach-weighted mean of each reporter's
	/// spin commitment: SUM(spinWeight(tier) * EffectiveReach) / SUM(EffectiveReach) over the region's
	/// reporters. A record no reporter is spinning returns 0; one every flagship has at High approaches 1.
	/// </summary>
	public float ReporterRadioPlay(string recordId, string regionId) {
		if (recordId == null || !stationsByRegion.TryGetValue(regionId, out List<RadioStation> roster)) return 0f;
		float weighted = 0f, reachTotal = 0f;
		foreach (RadioStation s in roster) {
			float reach = s.EffectiveReach();
			if (reach <= 0f) continue;
			reachTotal += reach;
			SpinTier tier = s.rt?.TierOf(recordId) ?? SpinTier.None;
			if (tier != SpinTier.None) weighted += SpinWeight(tier) * reach;
		}
		return reachTotal > 0f ? weighted / reachTotal : 0f;
	}
}
