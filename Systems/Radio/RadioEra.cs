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

	// =====================================================================
	// FORMAT MIX - the national dial composition, keyframed 1960/63/65/67/69.
	//
	// The roster is a stratified SAMPLE OF THE DIAL; StationNetwork.Hot100ReportingWeight is what
	// converts it into a Hot 100 survey. So this curve answers "what did American radio look like",
	// not "who reported to Billboard" -- the two jobs were previously conflated in one static
	// allocation and that is what produced a 77-station panel with 13 country reporters and 1 R&B.
	//
	// SOURCES / REASONING per row:
	//  Country      - CMA full-time country station counts (81 in 1961, 208 in 1965, 606 in 1969)
	//                 against AmStationCount above (3400/4000/4300) => 2.4% / 5.2% / 14.1% of the
	//                 dial. Country radio was TINY in 1960 and exploded; the old flat 17% allocation
	//                 had it backwards, over-stating it ~7x in exactly the years Country over-charts.
	//  RnB          - ~65 full-time black-appeal stations in 1960 (~1.9%) plus ~187 airing block
	//                 programming; ~400 stations carried some black-appeal content by the late 50s.
	//                 A panel that samples where the music actually played sits between the full-time
	//                 and block figures, so this runs 4% -> 9% rather than 2% -> 5%.
	//  FullService  - the block-programmed personality station: dominant in 1960, gutted by the Boss
	//                 Radio conversion across 1965-69. This is the same phenomenon BossRadioAdoption
	//                 describes; the curve sets the QUANTITY, Boss supplies the character change.
	//  Top40        - absorbs most of what FullService sheds.
	//  MOR          - "good music"/beautiful music grew steadily (Billboard split out an Easy
	//                 Listening chart in 1961) and accelerated on FM late.
	//  Jazz         - shrinking as a full-time commercial AM format across the decade.
	//  Gospel       - small, real, and heavily Southern; the region affinity term concentrates it.
	//  UndergroundFM- zero until the 1967 underground-FM movement, matching FmViability.
	//
	// Rows need not sum to 1: StationNetwork normalizes. Keep them as READABLE dial shares.
	private static readonly int[] FormatMixYears = { 1960, 1963, 1965, 1967, 1969 };
	private static readonly System.Collections.Generic.Dictionary<StationFormat, float[]> FormatMix = new() {
		//                                  1960   1963   1965   1967   1969
		[StationFormat.FullService]   = new[]{ .42f,  .30f,  .20f,  .10f,  .04f },
		[StationFormat.Top40]         = new[]{ .16f,  .24f,  .32f,  .38f,  .42f },
		[StationFormat.MOR]           = new[]{ .18f,  .19f,  .20f,  .21f,  .22f },
		[StationFormat.Country]       = new[]{ .02f,  .035f, .05f,  .09f,  .14f },
		[StationFormat.RnB]           = new[]{ .04f,  .05f,  .06f,  .075f, .09f },
		[StationFormat.Jazz]          = new[]{ .06f,  .05f,  .04f,  .03f,  .02f },
		[StationFormat.Gospel]        = new[]{ .02f,  .02f,  .02f,  .025f, .03f },
		[StationFormat.UndergroundFM] = new[]{ 0f,    0f,    0f,    .04f,  .08f },
	};

	/// <summary>National share of the dial carried by a format in a year (piecewise-linear between the
	/// 1960/63/65/67/69 keyframes, clamped outside). Shares are relative, not normalized.</summary>
	public static float FormatEraShare(StationFormat format, int year) {
		if (!FormatMix.TryGetValue(format, out float[] kf)) return 0f;
		if (year <= FormatMixYears[0]) return kf[0];
		if (year >= FormatMixYears[^1]) return kf[^1];
		for (int i = 0; i < FormatMixYears.Length - 1; i++) {
			if (year <= FormatMixYears[i + 1]) {
				float t = (year - FormatMixYears[i]) / (float)(FormatMixYears[i + 1] - FormatMixYears[i]);
				return Mathf.Lerp(kf[i], kf[i + 1], t);
			}
		}
		return kf[^1];
	}

	/// <summary>Every format carrying a non-zero share in any year -- the migration's working set.</summary>
	public static System.Collections.Generic.IEnumerable<StationFormat> MixFormats() => FormatMix.Keys;

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
