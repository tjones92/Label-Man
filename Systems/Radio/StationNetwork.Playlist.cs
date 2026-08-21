using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// Phase 3: the weekly playlist decision (design doc a 3.2-3.4). Once per reporter station per week
/// it scores every eligible record's candidacy, fills its High/Mid/Light slots, and drops the rest --
/// with tier inertia (stickiness) and the re-add hysteresis that protects the returns-to-#1 metric.
///
/// Runs in SimulateWeek AFTER sales (candidacy reads this week's settled salesSupport) and BEFORE the
/// radioPlay aggregation. All randomness uses the network's own RNG, never the global GD stream, so
/// with REPORTER_PANEL_WEIGHT at 0 this phase still produces byte-identical simulation output; it only
/// starts moving radioPlay once the weight is dialled in (Phase 2b).
/// </summary>
public sealed partial class StationNetwork {

	// ---- candidacy weights (doc a 3.2). Provisional; Phase 2b tunes against the V3.1 economy. ----
	private const float REL_WEIGHT = 0.5f;
	private const float LOYALTY_WEIGHT = 0.3f;
	private const int BURN_ONSET = 8;          // weeks in rotation before burn starts biting
	private const float BURN_DECAY = 0.96f;    // per-week candidacy decay past onset
	private const float HEAT_FOLLOW = 0.5f;    // how hard PDs chase national radioHeat
	private const float TRADE_WEIGHT = 0.3f;   // the trade "pick" bandwagon
	private const float INTEGRATION_CROSSOVER = 0.6f;  // RnB/Soul onto Top40 as integration rises

	// ---- album-rock crossover onto Top 40 (radio branch, 2026-08) ----
	// Top40's admittance set is MainstreamAM + Youth ONLY, so a genre routed to UndergroundFM/CollegeFolk
	// scores ~0.32 on the format that carries ~70% of 1969 panel voice, while Bubblegum/BritishPop/TeenPop
	// score 1.000. That is a 3.2x candidacy gap on the whole panel, and it is why PsychRock, FolkRock,
	// HardRock and CountryRock take 7 decade slots against an 81-slot benchmark on healthy market share.
	// The gap is not historical. Boss Top 40 played the album-rock singles: "Light My Fire", "Crimson and
	// Clover", "Born to Be Wild" were AM hits, and the benchmark's own PsychRock row (9/10 slots at
	// 1967-68) is counting exactly those. Raising the FM reporting weight cannot express this -- FM is 6
	// of 77 stations, ~2.4% of panel voice, so it moved PsychRock's reach-weighted voice 28 -> 30 against
	// Bubblegum's 80 and the family stayed at zero. The access has to come from Top 40 because that is
	// where the audience was.
	//
	// Ramped, not flat: 0 through 1965 (Top 40 was not playing album cuts and the segments barely exist),
	// then linear to 0.45 by 1969. Deliberately partial -- a Boss station played the rock HITS, it did not
	// programme the underground the way an FM station did. Mirrors INTEGRATION_CROSSOVER in shape and
	// intent. Note this also lifts the CollegeFolk-routed genres (ContemporaryFolk, SingerSongwriter);
	// those now carry explicit zero rows in ChartSlotBenchmark so the guard can hold them down.
	private const float ROCK_CROSSOVER_MAX = 0.45f;
	private const int ROCK_CROSSOVER_START_YEAR = 1965;
	private const int ROCK_CROSSOVER_FULL_YEAR = 1969;

	private static float RockCrossover(int year) => year <= ROCK_CROSSOVER_START_YEAR ? 0f
		: ROCK_CROSSOVER_MAX * Mathf.Min(1f, (year - ROCK_CROSSOVER_START_YEAR)
			/ (float)(ROCK_CROSSOVER_FULL_YEAR - ROCK_CROSSOVER_START_YEAR));

	// ---- genre lifecycle vitality (radio branch fix). FormatMatch is a STATIC per-genre segment
	// routing: it never decays, so the panel keeps a format-appropriate but historically fading genre
	// (BritishPop, Bubblegum) in heavy rotation late-decade, and that airplay pumps units (conversion
	// reads radioHeat) -- doubling BritishPop's late marketUnits vs V3.1 at an identical baseline. This
	// pull re-introduces the lifecycle decay the old tail-only radio carried: candidacy is scaled by the
	// genre's CURRENT national acceptance (which decays with zeitgeist/lifecycle), so a fading genre
	// loses panel presence relative to its fixed formatMatch. PULL=0 is byte-identical to the un-scaled
	// panel; PULL=1 makes candidacy fully proportional to acceptance. Applied only when the genre-market
	// v2 acceptance surface is live (neutral 1.0 otherwise, preserving prewarm/non-v2 output).
	private const float PANEL_LIFECYCLE_PULL = 0.6f;

	// ---- chart-referenced vacuum guard (radio branch fix, phase 2). The vitality PULL fades a
	// decliner, but the panel over-plays format-perfect Top40 pop that is still rising -- Bubblegum
	// (57 of 100 year-end slots at 1969 vs a 9-slot benchmark), TeenPop-66 (39 vs 6) -- and that airplay
	// STEALS year-end chart slots from Soul (6 vs 28) and SunshinePop (~0 vs 12). This is a CHART-side
	// distortion (chart slots, not market units), so the guard is referenced to the hand-counted year-end
	// slot benchmark (GenreCatalog.TryGetChartSlotShare), NOT the market-share target. An earlier
	// market-referenced version fired on early RnR's huge MARKET over and shed a vacuum onto TradPop
	// (+12.8pp) -- damage that traced entirely to the market reference. The chart reference fires on the
	// genres actually over-CHARTING (late Pop), and reroutes to the genres under-charting (Soul/Sunshine),
	// which are under on BOTH channels, so the recipients improve rather than inflate. A DEADBAND plus an
	// absolute MIN_OVER gate keep small/noise overs untouched and let a 0-slot benchmark still trip only
	// on a real over. RnR is EXEMPT: its market is pinned by its baseline keyframe (not the guard) and its
	// late-decade chart counts are flagged misclassification in the catalog, so damping its airplay would
	// only re-crash its market and re-inflate TradPop. PULL=0 is neutral.
	//
	// The guard is GATED to 1965+. The slot-theft distortion is entirely a late-decade phenomenon
	// (Bubblegum 1967+, Soul under 1965+, TeenPop-66); the early decade is a different regime dominated by
	// RnR/TradPop and is tuned by baseline keyframes + per-genre RadioAcceptance, not the panel. Firing the
	// guard early only thrashed it -- rerouting damped-Folk airplay into a TradPop over-chart, and (with
	// RnR exempt) feeding RnR as a sink so it over-charted MORE. Gating past 1965 makes the recipients
	// (Soul/SunshinePop/late-TradPop, all under late) beneficial and makes RnR's exemption moot (RnR ~0).
	private const float CHART_GUARD_PULL = 0.7f;
	private const float CHART_GUARD_FLOOR = 0.35f;      // never damp a single genre below this
	private const float CHART_GUARD_DEADBAND = 1.3f;    // only fire when realized slot share > target * this
	// MIN_OVER 0.03 -> 0.05 with the top-20 realized metric below: the denominator shrank from ~100
	// charting records to ~20, so 0.03 was 0.6 of a record and fired on noise. 0.05 is one full top-20
	// record, the same "a real over, not a rounding" intent the 0.03 carried against the old denominator.
	private const float CHART_GUARD_MIN_OVER = 0.05f;   // ...AND at least this many slots (share) over target

	// ---- REALIZED METRIC: the top 20, not the whole chart (radio branch, station-mix rebuild) ----
	// The guard compared two DIFFERENT OBJECTS. Its target is GenreCatalog.ChartSlotBenchmark -- a
	// hand-count of the year-end recap, which is the top 100 records RANKED BY CUMULATIVE POINTS. Its
	// realized was a genre's share of weekly charting-record COUNT, which is presence, not rank. A genre
	// that charts a few records very high reads low on count and high on the recap; one that charts many
	// records at #60-90 reads the reverse. Measured on mix4-decade at 1969, Bubblegum held 14.8% of
	// charting-record-weeks and 37 of the 100 year-end slots -- so the guard computed a 0.725 damp where
	// the object it is referenced to called for 0.47, and Bubblegum ran 74 decade slots against a 16-slot
	// benchmark. The same error runs the other way: Country and RnB chart broad and low (mean position
	// 60-64, ZERO top-10 weeks in 1969) so the count metric OVER-read them and the guard damped genres
	// that were already under their benchmark.
	//
	// Tallying only the top 20 fixes it. Against the year-end slot count, per genre-year on the
	// radio-chartguard-1001 decade: count share r=0.902 (MAE 1.93 slots), top-20 share r=0.992 (MAE 0.51).
	// Smooth alternatives were tested and are worse -- inverse-position^4 r=0.985, raw chart points r=0.977
	// (points loses because the recap TAKES the top 100 and counts slots, which weights the head harder
	// than points alone). The top 20 is also the honest statement of what the recap measures: a year-end
	// slot is bought at the top of the chart, not by being present on it.
	private const int CHART_GUARD_RANK_DEPTH = 20;
	// START YEAR 1965 -> 1960. The earlier note said firing the guard early "only thrashed it --
	// rerouting damped-Folk airplay into a TradPop over-chart, and (with RnR exempt) feeding RnR as a
	// SINK so it over-charted MORE". That failure is now understood to be a consequence of the
	// exemption, not of the early gate: with the single biggest early over-charter held immune, every
	// slot the guard freed had exactly one place to go. Removing RockAndRoll from ChartGuardExempt (see
	// below) changes the experiment. The TradPop half of that warning has also expired -- TradPop now
	// runs UNDER its early benchmark (39 vs 53 on mix3-156), so it is a valid recipient rather than an
	// over-chart risk.
	private const int CHART_GUARD_START_YEAR = 1960;

	// ---- tier inertia (doc a 3.3). Bonus resists demotion; scaled by autonomy (Boss churns fast). ----
	private static float TierInertiaBase(SpinTier tier) => tier switch {
		SpinTier.High => 0.18f,
		SpinTier.Mid => 0.10f,
		SpinTier.Light => 0.04f,
		_ => 0f
	};

	// ---- break-claim staking (doc c) ----
	private const int CLAIM_MAX_CHART_POS = 60;    // must be uncharted or below #60 to earn a claim

	// ---- anti-oscillation (doc a 3.4, NON-NEGOTIABLE) ----
	private const int MIN_READD_WEEKS = 4;         // a dropped record is locked out this long
	private const float READD_HYSTERESIS = 1.2f;   // and must beat the light cutoff by this margin
	private const float READD_MIN_CANDIDACY = 0.05f;

	// ---- circulation filter: a record is a fresh candidate only if it is actually in play. ----
	private static bool InCirculation(RecordRuntimeData r, float support) =>
		r.radioHeat > 0.02f || r.currentPosition > 0 || support > 0.05f;

	// Format -> the audience segments its playlist admits, each with an admittance WEIGHT (basis of
	// formatMatch). Weight 1.0 means the format programmes that segment as fully as a specialist
	// would; below 1.0 means it carries the music but not on equal terms. Segment names are stored
	// pre-stringified because GenreProfile.SegmentWeights is keyed by string and this runs per
	// station per record per week -- Enum.ToString() in that loop was a per-call allocation.
	private static readonly Dictionary<StationFormat, (string Seg, float W)[]> FormatSegments = new() {
		// RegionalLatin is NOT admitted by Top40 in general -- only latin-leaning stations carry it
		// (handled in FormatMatch). Admitting it here on every Top40 reporter over-charted LatinPop.
		[StationFormat.Top40] = new[] { ("MainstreamAM", 1f), ("Youth", 1f) },
		[StationFormat.RnB] = new[] { ("UrbanRnB", 1f) },
		[StationFormat.Country] = new[] { ("CountryWestern", 1f) },
		[StationFormat.MOR] = new[] { ("AdultMOR", 1f), ("FamilyChildrens", 1f) },
		[StationFormat.Jazz] = new[] { ("JazzHiFiClassical", 1f) },
		[StationFormat.Gospel] = new[] { ("GospelChurch", 1f) },
		[StationFormat.UndergroundFM] = new[] { ("UndergroundFM", 1f), ("CollegeFolk", 1f) },
		// The personality-era generalist: broad admittance across the mainstream segments. CountryWestern
		// at 0.35, not 1.0 (radio branch, 2026-08): admitting it at full weight made a country record a
		// PERFECT 1.000 match on FullService -- the same score as Bubblegum or British Pop -- on a format
		// carrying 44.7% of 1960 panel reach, which is the mechanical source of Country's early-decade
		// chart over-count. A block-programmed full-service station did carry a country hour; it did not
		// programme country the way a country station did. The other five segments stay at 1.0 so this
		// moves Country and nothing else -- deliberately surgical, since Soul's 1.000 on FullService is
		// the branch's hardest-won calibration (88 -> 175 decade slots) and must not be disturbed here.
		// CollegeFolk 0.40: no CollegeFolk specialist exists on the dial before UndergroundFM arrives in
		// 1967, so a FullService admittance of 1.0 made the generalist Folk's ONLY outlet -- and a
		// perfect one, so Folk ran 35 slots against a 5-slot benchmark once the mix rebuild restored
		// FullService to ~47% of reach. A weekend folk block is not the core rotation. On its own this
		// fixed Folk (35 -> 6) and RnB (err 25 -> 14) but sent RockAndRoll 54 -> 102 against a 38
		// benchmark, because FullService matches RnR, TradPop, TeenPop, Folk and Soul ALL at 1.000, so
		// damping one hands its slots to the next. It is kept here PAIRED with the early chart guard
		// below, which is what absorbs the RnR vacuum. Do not re-enable one without the other.
		// CountryWestern 0.35 -> 0.55 (radio branch, station-mix rebuild): the early-Country boost. The
		// 0.35 was set when the panel over-charted Country 16/14/11 against a 9/7/7 benchmark; paired with
		// the routing am .40->.20 and the 0.10 reporting weight it OVERSHOT, and Country now runs 5/0/3/3
		// early -- a 25-slot deficit over 1960-63 on a healthy 4.6-6.4% market share. This is the right
		// lever for an EARLY-ONLY correction without a year gate: its effect is proportional to
		// FullService's reach, which the station-mix rebuild takes from 49% in 1961 to 7% in 1969, so the
		// same constant is ~7x louder in the years that need it. Country's FullService match goes
		// 0.604 -> 0.712; Top40 (0.308) and the Country-format specialist are untouched. Watch 1966-67,
		// which already run 5/3 against a 2/2 benchmark -- if they inflate, gate this rather than trim it.
		[StationFormat.FullService] = new[] {
			("MainstreamAM", 1f), ("Youth", 1f), ("AdultMOR", 1f),
			("CollegeFolk", 0.40f), ("UrbanRnB", 1f), ("CountryWestern", 0.55f) },
	};

	// Per-record factors that don't vary by station, cached once per week to keep the 63-station
	// sweep cheap.
	private struct RecordFactors {
		public RecordRuntimeData rec;
		public string id, labelId, artistId;
		public Genre canonical;
		public GenreFamily family;
		public GenreProfile profile;   // market-side routing (SegmentWeights); may be null
		public float support;          // GetSalesSupportRatio (base; exponent applied per station)
		public float quality;          // 0..1
		public float heatPull;         // 1 + radioHeat*HEAT_FOLLOW + tradePick*TRADE_WEIGHT
		public float vitality;         // genre lifecycle acceptance scaling (PANEL_LIFECYCLE_PULL); 1 = off
		public bool inCirculation;     // false = fading catalog; a candidate only if already on a playlist
	}

	// RockAndRoll's exemption is REMOVED (radio branch, station-mix rebuild). The original reason was
	// that damping RnR's airplay would re-crash the market its baseline keyframe pins -- but that
	// reasoning came from the RadioAcceptance experiment, and RA is a DIFFERENT CHANNEL: it multiplies
	// genreAcceptance inside UpdateRadioHeat, so it feeds radioHeat -> conversionRate -> units. The
	// panel guard damps station CANDIDACY only. Nothing downstream of the playlist meeting writes
	// radioHeat, so the guard reaches the chart (via radioPlay) without the market leak that made RA
	// the wrong lever. And RnR is by far the largest early over-charter -- 102 slots against a 38
	// benchmark on mix3-156 -- so exempting it made it the sink for every slot the guard freed.
	// Empty set retained rather than deleted: it is the right place for a genre that genuinely must
	// not be touched, and the Latin/novelty genres may yet need it.
	private static readonly HashSet<Genre> ChartGuardExempt = new();

	/// <summary>Ceiling on the Rolodex advocacy term. Sized below the payola cap on purpose: a favour
	/// talked out of a DJ opens the door wider than nothing and less wide than cash.</summary>
	private const float ADVOCACY_CAP = 0.9f;

	/// <summary>
	/// How many Light slots a DJ may hand out on his own judgement, to records he has personally
	/// committed to on a call. This is the structural half of the Rolodex, and it is the half that
	/// actually matters: a 26% candidacy bump cannot lift an unknown record into a 36-slot sheet that
	/// is ranking the whole national field, so a pitch that only bought a multiplier read as inert.
	///
	/// A real personality jock has a couple of picks that are his and nobody else's. That is what this
	/// is. It is scaled by autonomy (a Boss Radio station at 0.10 has NONE -- the sheet is the sheet),
	/// capped hard, and it never displaces an incumbent: the picks come out of the Light tier and the
	/// record must still clear format. It buys a hearing for the advocacy's duration, not a permanent
	/// slot -- once the argument expires, the record competes on merit like everything else.
	/// </summary>
	private static int DiscretionaryPicks(float autonomy) =>
		autonomy >= 0.75f ? 2 : autonomy >= 0.45f ? 1 : 0;

	/// <summary>Read-only probe: would this station's format admit this genre at all, and how strongly?
	/// Same function the meeting scores with, exposed so the Rolodex can tell the player the truth
	/// ("it is not on his sheet") instead of inventing an objection.</summary>
	public float FormatAdmittance(Genre genre, RadioStation station, int year, float integration) {
		if (station == null) return 0f;
		Genre canon = GenreCatalog.MapLegacy(genre, year);
		GenreCatalog.TryGet(canon, out GenreProfile profile);
		return FormatMatch(profile, station.format, profile?.Family ?? GenreFamily.Pop,
			integration, RockCrossover(year), station.latinLeaning);
	}

	private readonly List<RecordFactors> factorScratch = new();
	private readonly Dictionary<string, RecordFactors> factorById = new(StringComparer.Ordinal);
	private readonly Dictionary<Genre, float> genreVitalityCache = new();
	// Chart-referenced guard scratch: this-week charting-record count per genre vs the year-end slot
	// benchmark. Totals complete in the cache pass before scoring resolves the guard.
	private readonly Dictionary<Genre, int> genreChartCount = new();
	private readonly Dictionary<Genre, float> genreChartGuardCache = new();
	private int totalChartCount;
	private int guardYear;
	private bool chartGuardLive;

	/// <summary>The reporter playlist meeting. Populates each station's rt.playlist for the week.</summary>
	public void UpdatePlaylists(IReadOnlyList<RecordRuntimeData> records, MarketRegion[] regions, int week, int year) {
		if (records == null || regions == null) return;

		// 1. Cache per-record station-invariant factors once.
		factorScratch.Clear();
		factorById.Clear();
		genreVitalityCache.Clear();
		genreChartCount.Clear();
		genreChartGuardCache.Clear();
		totalChartCount = 0;
		guardYear = year;
		// Vitality/guard apply only when the genre-market v2 acceptance surface is live; otherwise neutral
		// so prewarm and non-v2 runs stay byte-identical.
		bool vitalityLive = PANEL_LIFECYCLE_PULL > 0f && GenreMarketV2.Enabled
			&& ChartManager.Instance?.IsGenreMarketV2Live == true;
		chartGuardLive = CHART_GUARD_PULL > 0f && year >= CHART_GUARD_START_YEAR && GenreMarketV2.Enabled
			&& ChartManager.Instance?.IsGenreMarketV2Live == true;
		foreach (RecordRuntimeData r in records) {
			if (r?.baseRecord == null || r.baseRecord.format == ReleaseFormat.Album) continue;
			float support = ChartSimulator.GetSalesSupportRatio(r);
			bool circulating = InCirculation(r, support);
			// A record already on a playlist stays a candidate even once it leaves circulation (so it
			// can burn out gracefully rather than vanish); everything else must be in play.
			Genre canon = GenreCatalog.MapLegacy(r.baseRecord.primaryGenre, year);
			GenreCatalog.TryGet(canon, out GenreProfile profile);
			bool tradePick = r.currentPosition > 0 && r.currentPosition <= 40;
			// Chart-guard tally: this genre's grip on the current TOP 20 (the weekly proxy for the
			// year-end points-ranked slot benchmark -- see CHART_GUARD_RANK_DEPTH for why depth, not
			// presence). Totals complete before scoring resolves the guard.
			if (chartGuardLive && r.currentPosition >= 1 && r.currentPosition <= CHART_GUARD_RANK_DEPTH) {
				genreChartCount[canon] = genreChartCount.GetValueOrDefault(canon) + 1;
				totalChartCount++;
			}
			factorScratch.Add(new RecordFactors {
				rec = r, id = r.baseRecord.recordId, labelId = r.baseRecord.labelId, artistId = r.baseRecord.artistId,
				canonical = canon, family = profile?.Family ?? GenreFamily.Pop, profile = profile,
				support = support, quality = r.GetQuality(),
				heatPull = 1f + r.radioHeat * HEAT_FOLLOW + (tradePick ? TRADE_WEIGHT : 0f),
				vitality = GenreVitality(canon, year, vitalityLive),
				inCirculation = circulating,
			});
			factorById[r.baseRecord.recordId] = factorScratch[^1];
		}

		// 2. Per region, per reporter: score and fill.
		foreach (MarketRegion region in regions) {
			if (!stationsByRegion.TryGetValue(region.regionId, out List<RadioStation> roster)) continue;
			float integration = region.currentIntegration;
			float rockCrossover = RockCrossover(year);
			foreach (RadioStation station in roster)
				DecideStationPlaylist(station, integration, rockCrossover, week);
		}
	}

	private readonly List<(string id, float adjusted, SpinTier prior)> primaryScratch = new();
	private readonly List<(string id, float adjusted)> readdScratch = new();

	private void DecideStationPlaylist(RadioStation station, float integration, float rockCrossover, int week) {
		StationRuntime rt = station.rt;
		if (rt == null) return;
		Deejay dj = GetDeejay(station.leadDjId);
		float autonomy = station.djAutonomy;

		primaryScratch.Clear();
		readdScratch.Clear();

		foreach (RecordFactors f in factorScratch) {
			float quality = f.quality;
			SpinTier prior = rt.TierOf(f.id);
			bool incumbent = prior != SpinTier.None;

			float formatMatch = FormatMatch(f.profile, station.format, f.family, integration, rockCrossover, station.latinLeaning);
			if (formatMatch <= 0f) continue;                        // format never admits this genre
			if (!incumbent && !f.inCirculation) continue;           // not on the air and not circulating

			// Re-add hysteresis: a record this station dropped cannot re-enter for MIN_READD_WEEKS.
			bool everDropped = rt.droppedOnWeek.TryGetValue(f.id, out int droppedWeek);
			bool locked = !incumbent && everDropped && week - droppedWeek < MIN_READD_WEEKS;
			if (locked) continue;

			float candidacy = Candidacy(f, quality, formatMatch, station, dj, autonomy, rt);
			if (candidacy <= 0f) continue;

			if (!incumbent && everDropped) {
				// A cleared re-add: Light tier only, and only if it clears the hysteresis margin below.
				readdScratch.Add((f.id, candidacy));
			} else {
				float inertia = TierInertiaBase(prior) * (0.3f + autonomy * 0.7f);
				primaryScratch.Add((f.id, candidacy * (1f + inertia), prior));
			}
		}

		// Rank incumbents + fresh, fill High -> Mid -> Light.
		primaryScratch.Sort((a, b) => b.adjusted.CompareTo(a.adjusted));
		var next = new Dictionary<string, SpinTier>(StringComparer.Ordinal);
		int high = station.highSlots, mid = station.midSlots, light = station.lightSlots;

		// The DJ's own picks come off the top of the Light tier, before the ranking runs. Only records
		// he personally committed to, only if the format admits them, only while the commitment lasts.
		// Nothing here is reachable without a player action, so an AI-only run takes the null path.
		int picksLeft = DiscretionaryPicks(autonomy);
		if (picksLeft > 0 && AdvocacyReservationLookup != null) {
			IReadOnlyList<string> reserved = AdvocacyReservationLookup(station.stationId);
			if (reserved != null) {
				foreach (string id in reserved) {
					if (picksLeft <= 0 || light <= 0) break;
					if (next.ContainsKey(id)) continue;
					if (!factorById.TryGetValue(id, out RecordFactors rf)) continue;
					if (FormatMatch(rf.profile, station.format, rf.family, integration, rockCrossover,
							station.latinLeaning) <= 0f) continue;      // format is still the wall
					next[id] = SpinTier.Light;
					light--; picksLeft--;
				}
			}
		}

		float lightCutoff = float.MaxValue;   // the weakest retained score at the Light boundary
		int filled = 0;
		foreach (var c in primaryScratch) {
			if (next.ContainsKey(c.id)) continue;   // already seated by a discretionary pick
			SpinTier tier;
			if (filled < high) tier = SpinTier.High;
			else if (filled < high + mid) tier = SpinTier.Mid;
			else if (filled < high + mid + light) { tier = SpinTier.Light; lightCutoff = c.adjusted; }
			else break;
			next[c.id] = tier;
			filled++;
		}

		// Re-adds compete only for LEFTOVER Light capacity, and must beat the light cutoff by the
		// hysteresis margin -- a dropped record cannot bump an incumbent, only fill genuine slack.
		int lightUsed = 0;
		foreach (var kv in next) if (kv.Value == SpinTier.Light) lightUsed++;
		int lightFree = Mathf.Max(0, light - lightUsed);
		if (lightFree > 0 && readdScratch.Count > 0) {
			readdScratch.Sort((a, b) => b.adjusted.CompareTo(a.adjusted));
			float floor = lightCutoff == float.MaxValue ? READD_MIN_CANDIDACY : lightCutoff * READD_HYSTERESIS;
			foreach (var c in readdScratch) {
				if (lightFree <= 0) break;
				if (c.adjusted < floor || c.adjusted < READD_MIN_CANDIDACY) continue;
				next[c.id] = SpinTier.Light;
				lightFree--;
			}
		}

		CommitPlaylist(rt, next, week);
		StakeBreakClaims(station, rt, next, week);
	}

	/// <summary>Stake a break-claim (doc c) for any record this station just committed to High rotation
	/// while it is still nationally un-validated -- capturing when and how strongly it was breaking in
	/// this station's region, to judge prescience when the record later (maybe) becomes a hit.</summary>
	private void StakeBreakClaims(RadioStation station, StationRuntime rt, Dictionary<string, SpinTier> next, int week) {
		foreach (var kv in next) {
			if (kv.Value != SpinTier.High) continue;
			string id = kv.Key;
			if (rt.breakClaims.ContainsKey(id)) continue;             // keep the EARLIEST commit
			if (!factorById.TryGetValue(id, out RecordFactors f)) continue;
			int pos = f.rec.currentPosition;
			if (!(pos == 0 || pos > CLAIM_MAX_CHART_POS)) continue;   // only while un-validated
			float regionalStrength = f.rec.regionalData != null
				&& f.rec.regionalData.TryGetValue(station.regionId, out RegionalRecordData d) ? d.peakBreakoutScore : 0f;
			rt.breakClaims[id] = new StationRuntime.BreakClaim {
				firstHighWeek = week, chartPosAtFirstHigh = pos, regionalStrengthAtClaim = regionalStrength, settled = false
			};
		}
	}

	/// <summary>A post-peak DECLINE penalty for the genre, scaled by PANEL_LIFECYCLE_PULL. Retention =
	/// baseline(year)/peak-so-far is 1.0 while a genre is still rising to its peak (emergent genres are
	/// never penalised -- this is what a raw-acceptance signal got wrong, sinking Funk/Bubblegum) and
	/// falls only once the genre is past its peak, re-introducing the lifecycle fade the static
	/// formatMatch lacks. Cached per genre per week; neutral 1.0 when the v2 surface is not live.</summary>
	private float GenreVitality(Genre canon, int year, bool live) {
		if (!live) return 1f;
		if (genreVitalityCache.TryGetValue(canon, out float cached)) return cached;
		float v = 1f;
		if (GenreCatalog.TryGet(canon, out GenreProfile prof)) {
			float peak = prof.GetBaselinePeakThrough(year);
			float retention = peak > 0.0001f ? Mathf.Clamp(prof.GetBaseline(year) / peak, 0f, 1f) : 1f;
			v = 1f - PANEL_LIFECYCLE_PULL * (1f - retention);
		}
		genreVitalityCache[canon] = v;
		return v;
	}

	/// <summary>Chart-referenced vacuum guard, scaled by CHART_GUARD_PULL. When a genre holds more of the
	/// current TOP 20 than its historical year-end slot benchmark (by more than the deadband AND
	/// an absolute MIN_OVER), its candidacy is damped toward -- but never below -- the benchmark, so it
	/// stops over-charting and the freed airplay reroutes to genres under their slot benchmark. A genre at
	/// or below benchmark, an exempt genre (RnR), or one with no benchmark row is never touched. The damp
	/// is self-limiting (vanishes as realized approaches target). Cached per genre per week; needs the
	/// loop's chart totals complete, so it resolves lazily during scoring. Neutral 1.0 when the v2 surface
	/// is not live.</summary>
	private float GenreChartGuard(Genre canon) {
		if (!chartGuardLive || ChartGuardExempt.Contains(canon)) return 1f;
		if (genreChartGuardCache.TryGetValue(canon, out float cached)) return cached;
		float g = 1f;
		if (totalChartCount > 0 && GenreCatalog.TryGetChartSlotShare(canon, guardYear, out float target)) {
			float realized = genreChartCount.GetValueOrDefault(canon) / (float)totalChartCount;
			if (realized > target * CHART_GUARD_DEADBAND && realized - target > CHART_GUARD_MIN_OVER) {
				float toward = target > 0.0001f ? target / realized : 0f;   // 0-slot benchmark -> damp to floor
				g = Mathf.Clamp(1f - CHART_GUARD_PULL * (1f - toward), CHART_GUARD_FLOOR, 1f);
			}
		}
		genreChartGuardCache[canon] = g;
		return g;
	}

	/// <summary>candidacy = formatMatch x qualityTaste x salesSupport x relationship x advocacy x payola x freshness x heatPull x vitality x chartGuard.</summary>
	private float Candidacy(RecordFactors f, float quality, float formatMatch, RadioStation station, Deejay dj, float autonomy, StationRuntime rt) {
		// qualityTaste: the DJ's ear, weighted by autonomy. Boss station (autonomy~0) -> ~1 (neutral).
		float affinity = dj?.GenreAffinity(f.canonical) ?? 1f;
		float tasteRead = (0.6f + quality * 0.8f) * affinity;
		float noiseMag = (1f - (dj?.taste ?? 0.5f)) * 0.4f;
		float judgment = Mathf.Max(0.05f, tasteRead + (rng.Randf() - 0.5f) * 2f * noiseMag);
		float qualityTaste = Mathf.Lerp(1f, judgment, autonomy);

		// salesSupport: Boss stations follow sales sharply; personality stations tolerate a slow seller.
		float salesSupport = Mathf.Pow(Mathf.Max(0f, f.support), 1f + (1f - autonomy) * 2f);

		// relationship: cultivation edge (Phase 4 cultivation writes these; 0 until then).
		float relationship = 1f + rt.Rapport(f.labelId) * REL_WEIGHT + rt.Loyalty(f.artistId) * LOYALTY_WEIGHT;

		// payola (doc d): the player's active bribes on this (record, station), read via the ledger
		// lookup. Player-only, so this is 0 (neutral) in headless audits -- payola never perturbs the
		// base simulation. Capped so a bribe buys candidacy, never a runaway.
		float payola = 1f + Mathf.Clamp(ActivePayolaLookup?.Invoke(f.id, station.stationId) ?? 0f, 0f, 1.5f);

		// advocacy (Rolodex): a DJ agreed, on a call, to carry THIS record into THIS meeting. Unlike
		// rapport -- which is label-wide and helps everything you release equally -- advocacy is the
		// record-specific promise a Personal Pitch actually extracts, and it expires. It is a term in the
		// product, never a bypass: format still excludes what the format excludes, and a record with no
		// sales support still loses to one that has it. Player-only, so 0 in headless audits.
		float advocacy = 1f + Mathf.Clamp(ActiveAdvocacyLookup?.Invoke(f.id, station.stationId) ?? 0f, 0f, ADVOCACY_CAP);

		// freshness: per-station burn, replacing the aggregate STATION_DROP_BURN.
		int weeks = rt.weeksInPlaylist.TryGetValue(f.id, out int w) ? w : 0;
		float freshness = weeks > BURN_ONSET ? Mathf.Pow(BURN_DECAY, weeks - BURN_ONSET) : 1f;

		return formatMatch * qualityTaste * salesSupport * relationship * advocacy * payola * freshness * f.heatPull
			* f.vitality * GenreChartGuard(f.canonical);
	}

	/// <summary>0 excludes the record entirely; else the genre's routed reach into the format's segments,
	/// plus an integration-scaled RnB/Soul crossover onto Top40.</summary>
	private static float FormatMatch(GenreProfile profile, StationFormat format, GenreFamily family, float integration, float rockCrossover, bool latinLeaning) {
		if (profile == null || !FormatSegments.TryGetValue(format, out (string Seg, float W)[] segments)) return 0f;
		float m = 0f;
		foreach ((string seg, float w) in segments)
			m += profile.SegmentWeights.TryGetValue(seg, out float v) ? v * w : 0f;
		if (format == StationFormat.Top40 && family == GenreFamily.RhythmAndSoul)
			m += (profile.SegmentWeights.TryGetValue("UrbanRnB", out float rb) ? rb : 0f) * integration * INTEGRATION_CROSSOVER;
		// Album-rock/college routing crossing to Boss Top 40 as the era turns (see ROCK_CROSSOVER_MAX).
		if (format == StationFormat.Top40 && rockCrossover > 0f) {
			float underground = (profile.SegmentWeights.TryGetValue("UndergroundFM", out float fm) ? fm : 0f)
				+ (profile.SegmentWeights.TryGetValue("CollegeFolk", out float col) ? col : 0f);
			m += underground * rockCrossover;
		}
		// Latin-leaning Top40 stations (Southwest flavor) additionally admit the RegionalLatin routing.
		if (format == StationFormat.Top40 && latinLeaning)
			m += profile.SegmentWeights.TryGetValue("RegionalLatin", out float lat) ? lat : 0f;
		return Mathf.Clamp(m, 0f, 1f);
	}

	private static void CommitPlaylist(StationRuntime rt, Dictionary<string, SpinTier> next, int week) {
		// Drops: records that were on the playlist and no longer are -> record the drop week (hysteresis).
		foreach (var kv in rt.playlist) {
			if (!next.ContainsKey(kv.Key)) {
				rt.droppedOnWeek[kv.Key] = week;
				rt.weeksInPlaylist.Remove(kv.Key);
			}
		}
		// Retained/added: advance the burn clock; clear any stale drop record on (re-)add.
		foreach (var kv in next) {
			if (rt.playlist.ContainsKey(kv.Key)) rt.weeksInPlaylist[kv.Key] = rt.weeksInPlaylist.GetValueOrDefault(kv.Key) + 1;
			else { rt.weeksInPlaylist[kv.Key] = 1; rt.droppedOnWeek.Remove(kv.Key); }
		}
		rt.playlist.Clear();
		foreach (var kv in next) rt.playlist[kv.Key] = kv.Value;
	}
}
