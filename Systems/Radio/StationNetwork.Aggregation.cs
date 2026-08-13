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

	// ---- HOT 100 REPORTING WEIGHT ----------------------------------------------------------
	// The roster models the whole DIAL; the Hot 100 was a POP panel. Billboard surveyed Top 40 and
	// (early) full-service stations for the Hot 100, while country, R&B, MOR and jazz outlets
	// reported to their own charts -- Hot Country Singles, Hot R&B Singles, Easy Listening. Without
	// this weight every format votes in the Hot 100, and measured on the seed-1001 roster that is
	// 39.8% of panel reach (Country 15.1 + MOR 15.5 + Jazz 9.2) sitting in formats that historically
	// never did. The period counts say the same thing from the other side: full-time country stations
	// were 81 of ~3400 AM outlets in 1961 (2.4%) and 606 of ~4300 in 1969 (14.1%), against a flat
	// 13-of-77 (17%) in our roster -- so the panel most over-states country radio exactly in 1960-62,
	// which is where Country's residual chart over-count sits (16/11/15 slots vs a 9/7/7 benchmark).
	//
	// This multiplies NUMERATOR AND DENOMINATOR alike, so it re-weights the panel rather than cutting
	// its volume. The consequence is the point: a genuine crossover still scores, because it wins
	// slots on the pop stations that carry weight; a country-only hit that charts on country stations
	// alone loses 5.8x of its panel voice. That is exactly the historical distinction between a
	// crossover and a country hit, expressed as a survey-panel fact rather than a candidacy penalty.
	//
	// FullService is high, not 1.0: it IS the early-60s pop reporter (Top 40 had not yet consolidated),
	// but it was block-programmed rather than a pure pop survey.
	// RnB is deliberately NOT near-zero: R&B breakouts were tracked and crossed over, and the R&B
	// panel is how a soul record earns its pop rotation. Revisit this value when the station MIX is
	// rebuilt -- at the current roster RnB is 1 station (1.2% of reach) so it is barely load-bearing,
	// but at a historically-sized 8-10 R&B reporters it becomes a primary Soul lever.
	private static float Hot100ReportingWeight(StationFormat format) => format switch {
		StationFormat.Top40 => 1.00f,
		StationFormat.FullService => 0.85f,
		StationFormat.RnB => 0.35f,
		StationFormat.MOR => 0.25f,          // reported to Easy Listening
		// 0.20 -> 0.50 (radio branch, station-mix rebuild). 0.20 was reasoning about the FORMAT -- album
		// radio, not a singles survey -- but the genres ROUTED through UndergroundFM had genuine Hot 100
		// hits: "Light My Fire" was a #1 single. At 0.20, FM's 6 stations of 77 were ~2.4% of 1969 panel
		// voice and the whole FM/college family (PsychRock, FolkRock, HardRock, CountryRock) took 2 decade
		// slots against an 81-slot benchmark on healthy 3-9% market share. NOTE this lever is small by
		// construction: it moves PsychRock's reach-weighted panel voice from ~28 to ~30 against Bubblegum's
		// ~80, because those genres draw most of their (thin) access from Top40 at a ~0.32 formatMatch, not
		// from FM. The family's chart access is a TOP 40 question; this only stops FM from being silent.
		StationFormat.UndergroundFM => 0.50f, // album radio, but its records crossed to the singles chart
		StationFormat.Country => 0.10f,      // reported to Hot Country Singles
		StationFormat.Jazz => 0.05f,
		StationFormat.Gospel => 0.05f,
		_ => 0.50f
	};

	/// <summary>
	/// Reporter-panel airplay for a record in a region, 0..1. Reach-weighted mean of each reporter's
	/// spin commitment, where each station's reach is scaled by how much its FORMAT reported to the
	/// Hot 100: SUM(spinWeight(tier) * reach * reportWeight) / SUM(reach * reportWeight) over the
	/// region's reporters. A record no reporter is spinning returns 0; one every pop flagship has at
	/// High approaches 1.
	/// </summary>
	public float ReporterRadioPlay(string recordId, string regionId) {
		if (recordId == null || !stationsByRegion.TryGetValue(regionId, out List<RadioStation> roster)) return 0f;
		float weighted = 0f, reachTotal = 0f;
		foreach (RadioStation s in roster) {
			float reach = s.EffectiveReach() * Hot100ReportingWeight(s.format);
			if (reach <= 0f) continue;
			reachTotal += reach;
			SpinTier tier = s.rt?.TierOf(recordId) ?? SpinTier.None;
			if (tier != SpinTier.None) weighted += SpinWeight(tier) * reach;
		}
		return reachTotal > 0f ? weighted / reachTotal : 0f;
	}
}
