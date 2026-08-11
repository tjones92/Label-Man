using Godot;

/// <summary>
/// Single authored source for the decade's radio-climate arcs (design doc f). Every radio
/// subsystem (StationNetwork roster/era events, and later PayolaLedger / relationships) reads
/// these instead of holding its own copy of the same curve.
///
/// CONTRACT: every method here is a PURE FUNCTION of date. No runtime state, no per-station
/// variance, no simulation feedback. These describe the CLIMATE; entities roll against the
/// climate and hold their own weather (a station's Boss status, a DJ's suspicion, etc.).
///
/// All curves are keyed to the sim's 1960 start. Values before 1960 are defined so a
/// pre-hearing backstory (1957-59) reads correctly if ever simulated, but the live decade is
/// 1960-1969.
///
/// PHASE NOTE: In Phase 1 only the roster/era arcs (Boss, FM, station counts) are consumed --
/// the payola/regulatory arcs are authored now so the later phases have a single source, but
/// nothing reads RegulatoryHeat until the payola ledger lands.
/// </summary>
public static class RadioEra {

	// =====================================================================
	// REGULATORY HEAT - the payola-hearings arc.
	// The sim opens in 1960, the crackdown peak. Rampant/semi-legal before the 1959-60
	// hearings; a sustained elevated plateau after, never returning to the pre-hearing
	// free-for-all. Consumed by: PayolaLedger (Phase 4) and gift-cultivation risk.
	// =====================================================================
	public static float RegulatoryHeat(int year, int month = 6) {
		float y = ContinuousYear(year, month);
		if (y < 1959f) return 0.15f;                                  // rampant, semi-legal
		if (y < 1960f) return Mathf.Lerp(0.15f, 0.90f, y - 1959f);    // the hearings ramp
		if (y < 1961f) return 0.90f;                                  // crackdown peak (sim start)
		// Normalizes to an elevated plateau over ~4 years - never back to pre-hearing levels.
		return Mathf.Lerp(0.90f, 0.55f, Mathf.Clamp((y - 1961f) / 4f, 0f, 1f));
	}

	// =====================================================================
	// BOSS RADIO ADOPTION - the national CLIMATE, not any one station's status.
	// Returns 0..1: how strongly the tight-playlist, low-DJ-autonomy format is spreading.
	// The format arrived ~1965 (KHJ Los Angeles), spread through major markets, and was
	// dominant by the late 60s. This is the PRESSURE; StationNetwork.ApplyEraEvents rolls
	// individual stations against it, weighting big markets to convert first.
	// =====================================================================
	public static float BossRadioAdoption(int year, int month = 6) {
		float y = ContinuousYear(year, month);
		if (y < 1965f) return 0f;                       // personality era intact
		if (y >= 1969f) return 1f;                      // dominant
		return Mathf.Clamp((y - 1965f) / 4f, 0f, 1f);   // 1965-69 spread
	}

	/// <summary>
	/// Per-year Boss-conversion probability for a station of the given market tier, derived from
	/// the national adoption climate. Big markets convert first (KHJ, WABC led). This is a rate,
	/// not a state - StationNetwork rolls it and records the RESULT on the station.
	/// </summary>
	public static float BossConversionChance(int year, RegionTier tier, int month = 6) {
		float adoption = BossRadioAdoption(year, month);
		if (adoption <= 0f) return 0f;
		float tierLead = tier switch {
			RegionTier.Major => 1.4f,       // flagship markets Boss-ify first
			RegionTier.Regional => 1.0f,
			RegionTier.Secondary => 0.7f,
			_ => 0.5f
		};
		// Kept modest so conversion is spread across the window rather than a single-year cliff;
		// StationNetwork applies it at year change.
		return Mathf.Clamp(adoption * 0.5f * tierLead, 0f, 0.8f);
	}

	// =====================================================================
	// FM EMERGENCE - the underground-FM viability arc.
	// FM existed in 1960 (mostly simulcast/easy-listening background) but was not a chart force
	// until the 1967 underground-FM movement. Progressive/major markets led.
	// =====================================================================
	public static float FmViability(int year, int month = 6) {
		float y = ContinuousYear(year, month);
		if (y < 1967f) return 0f;                            // background only, no chart weight
		return Mathf.Clamp((y - 1966f) * 0.30f, 0f, 1f);     // ramps from 1967
	}

	/// <summary>Whether a region should carry an underground-FM reporter this year, given its
	/// character. Combines the viability arc with the region's progressivism and market size.</summary>
	public static bool FmReporterViable(int year, RegionTier tier, float culturalProgressivism) =>
		FmViability(year) > 0f && (tier == RegionTier.Major || culturalProgressivism > 0.5f);

	// =====================================================================
	// STATION POPULATION - the historical count arc (authored numbers).
	// AM: 3400 (1960) -> 4000 (1965) -> 4300 (1969).  FM: 750 -> 1400 -> 2200.
	// These drive the TAIL field's reach scaling, not object counts (the tail is statistical).
	// Piecewise-linear through the three anchors.
	// =====================================================================
	public static int AmStationCount(int year) => LerpAnchors(year,
		(1960, 3400), (1965, 4000), (1969, 4300));
	public static int FmStationCount(int year) => LerpAnchors(year,
		(1960, 750), (1965, 1400), (1969, 2200));

	/// <summary>
	/// FM's share of total stations - the structural shift that grows underground-FM's tail reach
	/// across the decade. Note this is the physical FM presence; FmViability gates its CHART weight.
	/// </summary>
	public static float FmStationShare(int year) {
		int am = AmStationCount(year), fm = FmStationCount(year);
		return am + fm > 0 ? fm / (float)(am + fm) : 0f;
	}

	// =====================================================================
	// AIRPLAY CHART WEIGHT - cross-reference, NOT re-authored here.
	// ChartSimulator.GetAirplayEraWeight (0.60 @1960 -> 1.00 @1968) is the SAME historical
	// phenomenon as Boss adoption + FM growth: radio's consolidation and rising chart power.
	// It is calibration-FROZEN in ChartSimulator (private consts AIRPLAY_ERA_WEIGHT_EARLY/LATE)
	// with an extensive comment, so it is NOT moved or duplicated here. These mirror constants
	// exist only so a future calibrator sees the causal link and does not author a fourth,
	// conflicting radio-power curve. If ever unified, do it as a measured calibration change.
	// =====================================================================
	public const int AirplayWeightStartYear = 1960;
	public const int AirplayWeightFullYear = 1968;
	// (Weight values live in ChartSimulator.AIRPLAY_ERA_WEIGHT_EARLY/LATE - do not duplicate.)

	// ---- helpers ----
	private static float ContinuousYear(int year, int month) =>
		year + Mathf.Clamp(month - 1, 0, 11) / 12f;

	private static int LerpAnchors(int year, (int y, int v) a, (int y, int v) b, (int y, int v) c) {
		if (year <= a.y) return a.v;
		if (year >= c.y) return c.v;
		if (year <= b.y) return Mathf.RoundToInt(Mathf.Lerp(a.v, b.v, (year - a.y) / (float)(b.y - a.y)));
		return Mathf.RoundToInt(Mathf.Lerp(b.v, c.v, (year - b.y) / (float)(c.y - b.y)));
	}
}
