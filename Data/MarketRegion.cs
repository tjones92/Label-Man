using System;
using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class MarketRegion : Resource {
	
	[ExportGroup("Identity")]
	[Export] public string regionId;
	[Export] public string regionName;
	[Export] public string[] majorCities;
	[Export] public RegionTier tier;
	[Export] public RegionType regionType;
	
	[ExportGroup("Demographics - 1960 Baseline")]
	[Export] public float population;
	[Export(PropertyHint.Range, "0,1")] public float urbanization;
	[Export] public float averageIncome;
	[Export(PropertyHint.Range, "0,0.6")] public float youthPercentage;
	[Export(PropertyHint.Range, "0,1")] public float blackPopulation;
	[Export] public int collegeCount;
	
	[ExportGroup("Cultural Factors")]
	[Export(PropertyHint.Range, "0,1")] public float integrationLevel;
	[Export(PropertyHint.Range, "0,1")] public float culturalProgressivism;
	[Export(PropertyHint.Range, "0,1")] public float regionalInsularity;
	[Export(PropertyHint.Range, "0.5,2")] public float trendAdoptionSpeed;
	[Export(PropertyHint.Range, "0,1")] public float churchNetworkStrength = 0.25f;
	
	[ExportGroup("Genre Affinities - 1960 Baseline")]
	// FIX: Changed List to Array for Godot Export compatibility
	[Export] public GenrePreference[] genrePreferences;
	
	[ExportGroup("Infrastructure")]
	[Export] public MediaInfrastructure media;
	[Export] public MusicInfrastructure musicIndustry;
	[Export] public DistributionNetwork distribution;
	
	[ExportGroup("Special Modifiers")]
	// FIX: Changed List to Array for Godot Export compatibility
	[Export] public RegionalModifier[] specialModifiers;

	[ExportGroup("Album Demand Timing")]
	// LP-RATIO RECALIBRATION (2026-08): rise start 1964 -> 1957. The LP was mature by 1960 (it had
	// overtaken the single by revenue ~1957), so pinning era progress at 0 until 1964 erased the
	// early adult album market and is the core reason 1960 read ~1.4% album units against a ~30% goal.
	[Export(PropertyHint.Range, "1955,1970,0.1")] public float albumDemandRiseStartYear = 1957f;
	[Export(PropertyHint.Range, "1960,1975,0.1")] public float albumDemandRiseEndYear = 1972f;
	
		public MarketRegion() {
		media = new MediaInfrastructure();
		musicIndustry = new MusicInfrastructure();
		distribution = new DistributionNetwork();
	}
	
	// Runtime state
	public Dictionary<Genre, float> currentGenreAcceptance;
	public Dictionary<Genre, float> genreMomentum;
	public float currentIntegration;
	public float currentProgressivism;
	private bool genreMarketV2Live;

	/// <summary>
	/// Fixed-input decomposition of the Album buyer-pool seam.  The enabled
	/// routed acceptance may differ materially from the accepted regional Album
	/// baseline, so callers can inspect and normalize the opportunity before any
	/// format tilt or record-specific conversion is applied.
	/// </summary>
	public readonly struct AlbumDemandExplanation {
		public readonly float RoutedAcceptance, LegacyAcceptance, SegregationFactor, AlbumAffinity, PurchaseWillingness;
		public readonly float EnabledPreTiltBuyerPool, AcceptedPreTiltBuyerPool, OpportunityNormalization;

		public AlbumDemandExplanation(float routedAcceptance, float legacyAcceptance, float segregationFactor,
			float albumAffinity, float purchaseWillingness, float enabledPreTiltBuyerPool, float acceptedPreTiltBuyerPool) {
			RoutedAcceptance = routedAcceptance;
			LegacyAcceptance = legacyAcceptance;
			SegregationFactor = segregationFactor;
			AlbumAffinity = albumAffinity;
			PurchaseWillingness = purchaseWillingness;
			EnabledPreTiltBuyerPool = enabledPreTiltBuyerPool;
			AcceptedPreTiltBuyerPool = acceptedPreTiltBuyerPool;
			OpportunityNormalization = acceptedPreTiltBuyerPool / Mathf.Max(.000001f, enabledPreTiltBuyerPool);
		}
	}
	public SegmentCapacityModel segmentCapacities;

	/// <summary>ChartManager switches this only after the legacy prewarm completes.</summary>
	public void SetGenreMarketV2Live(bool live) => genreMarketV2Live = live;
	
	public void InitializeRuntimeState(int startYear) {
		currentGenreAcceptance = new Dictionary<Genre, float>();
		genreMomentum = new Dictionary<Genre, float>();
		
		if (genrePreferences != null) {
			foreach (var pref in genrePreferences) {
				currentGenreAcceptance[pref.genre] = pref.baseAcceptance;
				genreMomentum[pref.genre] = 0f;
			}
		}
		
		currentIntegration = GetEraIntegration(startYear);
		currentProgressivism = culturalProgressivism;
		segmentCapacities = SegmentCapacityModel.Create(this, startYear);
	}

	// --- Integration era curve -------------------------------------------------
	// The record market integrated across the 1960s: R&B and soul crossed steadily
	// from segregated race-record channels into white AM radio and retail.
	// currentIntegration was assigned once from the authored 1960 integrationLevel
	// and re-frozen to it every year by ChartManager.OnYearChanged, holding the
	// white-audience access term in GetSegregationFactor, the Soul/RnB growth term
	// in GetYearEvolution, the V2 MainstreamAM regional factor and the UrbanRnB
	// segment share flat for the whole decade. It now ramps from the authored 1960
	// anchor toward fuller integration, closing IntegrationEraGapClose of the
	// remaining gap to full white access by 1969.
	//
	// The ramp is deliberately NOT linear: it steps up at the civil-rights
	// inflection points that actually opened white radio and retail to black music,
	// rather than tracking any genre's authored acceptance curve. The two largest
	// jumps are 1964 (the Civil Rights Act's public-accommodation desegregation) and
	// 1967 (the soul explosion / Loving v. Virginia / the Stax-Atlantic peak); 1965
	// (Voting Rights Act) and 1968 (Fair Housing Act) carry smaller steps. 1960 is
	// exactly 0, so every integration-gated quantity is unchanged at the decade's
	// start and a 1960-only run is byte-identical to the frozen behaviour; 1969 is
	// the full ramp, so the endpoints match a straight line and only the shape
	// differs. GapClose is the magnitude knob: raising it lifts late-decade Soul/
	// RnB/Gospel reach in white markets, which the genre keyframes are authored
	// against.
	// 0.45 -> 0.70 (2026-08 V3): the Soul-album lever. Soul album charts on UNITS only (no airplay to
	// bypass segregation), so GetSegregationFactor -- blackPop + (1-blackPop)*currentIntegration -- was
	// the binding suppressor, holding soul album at ~7% (vs a 22 aim) while soul SINGLES escaped via
	// radio. 0.45 left 1969 white access at only ~55% of the gap closed, which understates how
	// mainstream soul was by 1969 (Motown/Stax/Atlantic at their crossover peak). Raising it lifts the
	// late-decade Soul units in white markets: soul album ~7->~10 and soul singles ~14->~16 (they were
	// UNDER at 17, so this corrects both toward target in the same direction). R&B baseline is trimmed
	// to absorb its share of the same lift. NOTE the segregation ceiling caps soul album near ~12% even
	// at full integration; reaching the 22 aim would need a deeper album-crossover mechanism, so 22 is
	// treated as aspirational for V3 and ~10-12 as the achievable band.
	public const float IntegrationEraGapClose = 0.70f;

	// Fraction of the decade's total crossover realised by each year. Sampled at
	// integer years by OnYearChanged; the lerp only smooths off-keyframe queries.
	private static readonly (float Year, float Progress)[] IntegrationProgressCurve = {
		(1960f, 0.00f), (1961f, 0.04f), (1962f, 0.08f), (1963f, 0.16f),
		(1964f, 0.38f), (1965f, 0.52f), (1966f, 0.62f), (1967f, 0.80f),
		(1968f, 0.90f), (1969f, 1.00f),
	};

	private static float IntegrationProgress(float year) {
		var curve = IntegrationProgressCurve;
		if (year <= curve[0].Year) return curve[0].Progress;
		if (year >= curve[^1].Year) return curve[^1].Progress;
		for (int i = 1; i < curve.Length; i++) {
			if (year <= curve[i].Year) {
				float t = (year - curve[i - 1].Year) / (curve[i].Year - curve[i - 1].Year);
				return Mathf.Lerp(curve[i - 1].Progress, curve[i].Progress, t);
			}
		}
		return curve[^1].Progress;
	}

	/// <summary>
	/// White-audience integration for the given year, ramped from the authored 1960
	/// <see cref="integrationLevel"/> anchor toward fuller access along the
	/// civil-rights progress curve. Low-integration regions converge fastest in
	/// absolute terms (the gap to full access is larger there), matching the
	/// federally-forced Southern catch-up.
	/// </summary>
	public float GetEraIntegration(float year) {
		return Mathf.Clamp(
			integrationLevel + (1f - integrationLevel) * IntegrationProgress(year) * IntegrationEraGapClose, 0f, 1f);
	}
	
	public float GetGenreMarketSize(Genre genre, int year) {
		float baseMarket = population * 1000000f;
		float buyingPopulation = baseMarket * GetBuyingPopulationPercentage();
		if (GenreMarketV2.Enabled && genreMarketV2Live) {
			float momentum = ChartManager.Instance?.GetGenreMomentum(genre) ?? (genreMomentum != null && genreMomentum.TryGetValue(genre, out float value) ? value : 0f);
			return buyingPopulation * GenreAcceptanceService.GetRegionalDemandAcceptance(genre, genre, this, year, momentum);
		}
		float acceptance = GetGenreAcceptance(genre, year);
		float segregationFactor = GetSegregationFactor(genre);
		return buyingPopulation * acceptance * segregationFactor;
	}

	public float GetAlbumMarketSize(Genre genre, int year) {
		if (GenreMarketV2.Enabled && genreMarketV2Live) {
			AlbumDemandExplanation explanation = GetAlbumDemandExplanation(genre, year);
			// Album demand must retain the same prospective V2 genre acceptance as
			// Singles. The former opportunity normalization exactly restored the
			// legacy buyer pool, canceling authored declines and emerging-genre
			// growth before quality, awareness, stock, or format tilt could act.
			return explanation.EnabledPreTiltBuyerPool;
		}
		float baseMarket = population * 1000000f;
		float buyingPopulation = baseMarket * GetBuyingPopulationPercentage();
		return buyingPopulation * GetGenreAcceptance(genre, year) * GetSegregationFactor(genre) *
			GetAlbumAffinity(genre, year) * GetAlbumPurchaseWillingness(year);
	}

	// Demographic bypass for soundtracks/cast albums (D7 soundtrack subsystem, phase 5). A soundtrack
	// sells to an ADULT/FAMILY audience largely independent of whether its mapped genre has a big
	// radio/album market -- that is how a Sound-of-Music (TraditionalPop) or a beach-film LP (SurfRock)
	// charts despite the genre's thin normal album demand. So its buyer pool is drawn from the
	// AdultMOR + FamilyChildrens segments (+ JazzHiFiClassical for orchestral scores, + a Youth slice
	// for youth-appeal film songs) rather than from GetGenreAcceptance/GetAlbumAffinity. This does NOT
	// touch the genre's own acceptance (handoff §3.4); the record still reports under its real genre.
	public float GetSoundtrackAlbumMarketSize(ExternalMediaProfile profile, int year) {
		float buyingPopulation = population * 1000000f * GetBuyingPopulationPercentage();
		var shares = segmentCapacities?.Shares;
		float Share(AudienceSegment s, float fallback) => shares != null && shares.TryGetValue(s, out float v) ? v : fallback;
		float segReach = Share(AudienceSegment.AdultMOR, 0.14f) + Share(AudienceSegment.FamilyChildrens, 0.04f);
		if (profile.sourceType == ExternalMediaSourceType.FilmScore)
			segReach += Share(AudienceSegment.JazzHiFiClassical, 0.04f);
		// Youth crossover for song-driven films and any high-youth-appeal title (beach/rock pictures).
		if (profile.sourceType == ExternalMediaSourceType.FilmSong || profile.youthAppeal > 0.5f)
			segReach += Share(AudienceSegment.Youth, 0.18f) * profile.youthAppeal * 0.5f;
		return buyingPopulation * segReach * GetAlbumPurchaseWillingness(year);
	}

	public AlbumDemandExplanation GetAlbumDemandExplanation(Genre genre, float year) {
		float buyingPopulation = population * 1000000f * GetBuyingPopulationPercentage();
		float momentum = ChartManager.Instance?.GetGenreMomentum(genre) ?? (genreMomentum != null && genreMomentum.TryGetValue(genre, out float value) ? value : 0f);
		float routedAcceptance = GenreAcceptanceService.GetRegionalDemandAcceptance(genre, genre, this, year, momentum);
		float legacyAcceptance = GetLegacyGenreAcceptance(genre, year);
		float segregation = GetSegregationFactor(genre);
		float affinity = GetAlbumAffinity(genre, (int)year);
		float willingness = GetAlbumPurchaseWillingness((int)year);
		float shared = buyingPopulation * segregation * affinity * willingness;
		return new AlbumDemandExplanation(routedAcceptance, legacyAcceptance, segregation, affinity, willingness,
			shared * routedAcceptance, shared * legacyAcceptance);
	}

	/// <summary>Accepted regional calculation without the live V2 routing branch.</summary>
	public float GetLegacyGenreAcceptance(Genre genre, float year, bool includeMomentum = true) {
		if (currentGenreAcceptance == null || !currentGenreAcceptance.ContainsKey(genre)) return culturalProgressivism * 0.3f;
		float momentum = includeMomentum && genreMomentum != null && genreMomentum.TryGetValue(genre, out float value) ? value : 0f;
		return Mathf.Clamp(currentGenreAcceptance[genre] + GetYearEvolution(genre, (int)year) + momentum, 0f, 1f);
	}

	/// <summary>Accepted pre-tilt Album opportunity as a share of the regional genre buyer pool.</summary>
	public float GetAcceptedAlbumOpportunityWeight(Genre genre, float year) {
		return GetAcceptedPreTiltAlbumMarketSize(genre, year) / Mathf.Max(.000001f, GetAcceptedLegacyGenreMarketSize(genre, year));
	}

	/// <summary>
	/// Live pre-tilt Album opportunity conditional on an enabled genre buyer.
	/// Unlike the legacy-pool ratio, this remains defined for every canonical
	/// genre and is the opportunity actually used by enabled Album demand *sizing*.
	/// It is not the format-centering term; see <see cref="GetMarketAlbumOpportunityWeight"/>.
	/// </summary>
	public float GetEnabledAlbumOpportunityWeight(Genre genre, float year) =>
		Mathf.Clamp(GetAlbumAffinity(genre, (int)year) * GetAlbumPurchaseWillingness((int)year), 0f, 1f);

	/// <summary>
	/// The market's Album share of this era and region, carrying NO genre term.
	///
	/// This is what centers the Single/Album format tilt. GetFormatMultiplier's contract is
	/// "centered relative format suitability, normalized against the accepted era opportunity":
	/// the genre's own tilt is SingleOrientation, and this weight is the market split that tilt
	/// is conserved against. Passing a genre-scoped album affinity here instead put the genre on
	/// BOTH sides of that normalization -- a genre was tilted by its orientation and then had the
	/// size of that tilt set by a second, independently authored statement of the same fact. The
	/// two disagreed: across the sixteen genres with explicit affinities, affinity correlates with
	/// (1 - SingleOrientation) at r = 0.88 while differing by up to .28 (Folk, Gospel, Country).
	/// GenreCatalog.SingleOrientation is the calibrated source and is now the only genre input to
	/// format tilt.
	/// </summary>
	public float GetMarketAlbumOpportunityWeight(float year) =>
		Mathf.Clamp(GetMarketAlbumAffinity((int)year) * GetAlbumPurchaseWillingness((int)year), 0f, 1f);

	/// <summary>
	/// Format-centering opportunity for the active demand route. The live route carries no genre;
	/// the accepted route retains its frozen genre-scoped pool ratio unchanged.
	/// </summary>
	public float GetAlbumOpportunityWeight(Genre genre, float year, bool live) =>
		live ? GetMarketAlbumOpportunityWeight(year) : GetAcceptedAlbumOpportunityWeight(genre, year);

	/// <summary>Accepted legacy genre buyer pool used as the common Album-prior denominator.</summary>
	public float GetAcceptedLegacyGenreMarketSize(Genre genre, float year) {
		float buyingPopulation = population * 1000000f * GetBuyingPopulationPercentage();
		return buyingPopulation * GetLegacyGenreAcceptance(genre, year) * GetSegregationFactor(genre);
	}

	/// <summary>Accepted Album buyer pool before format tilt and record-specific conversion.</summary>
	public float GetAcceptedPreTiltAlbumMarketSize(Genre genre, float year) =>
		GetAlbumDemandExplanation(genre, year).AcceptedPreTiltBuyerPool;

	/// <summary>
	/// Album-buying propensity for one genre. This is a *sizing* term: it scales how much
	/// of a genre's buyer pool is in the market for an LP, and genre belongs in it.
	/// It is deliberately NOT the term that centers the Single/Album format tilt -- see
	/// <see cref="GetMarketAlbumOpportunityWeight"/> for why that one carries no genre.
	/// </summary>
	public float GetAlbumAffinity(Genre genre, int year) =>
		ShapeAlbumAffinity(GetAlbumAffinityBaseline(GenreCatalog.MapLegacy(genre, year)), year);

	/// <summary>
	/// The genre-blind era baseline, i.e. what an average genre's album propensity is.
	/// This is the value every genre without an explicit entry already resolved to, so
	/// using it as the market level leaves the majority of the catalog where it was.
	/// </summary>
	public float GetMarketAlbumAffinity(int year) => ShapeAlbumAffinity(NeutralAlbumAffinityBaseline, year);

	private const float NeutralAlbumAffinityBaseline = 0.40f;

	// Public so the 1960 cold-start prewarm can seed a genre-realistic opening LP catalog: this baseline
	// is the era's album skew (adult/jazz/classical high, teen/rock low), so it doubles as the
	// probability a seeded catalog title is an album.
	public static float GetAlbumSeedAffinity(Genre canonical) => GetAlbumAffinityBaseline(canonical);

	private static float GetAlbumAffinityBaseline(Genre canonical) => canonical switch {
		// DEMAND PULLBACK (2026-08, WIP §4): 0.88 -> 0.65. At 0.88 EL over-routed to albums and fell to
		// 28 singles slots vs a hand count of 52 -- unlike Jazz (~0), EL kept a real singles presence.
		// EL 0.65 -> 0.34 (2026-08 V3): affinity only differentiates EARLY album share (ShapeAlbumAffinity
		// saturates at 1.0 late for any affinity >~0.48), so this carries the whole early album trim; two
		// seeds put EL album at a stable 11.6% in 1960 vs the ~8 target, so 0.42 -> 0.34 to land it. The
		// late album LIFT is done off the raised EL baseline. See GenreCatalog EL note.
		Genre.EasyListening => 0.34f,
		// Classical 0.82 -> 0.45 (2026-08): highest affinity in the table drove a 23% 1960 album share
		// vs ~0-4. Cut halves the early over-presence (paired with the 1960 baseline .40->.26).
		Genre.Classical => 0.45f,
		Genre.PsychedelicRock => 0.78f,
		Genre.Folk => 0.78f,
		// GENRE-ARC ALBUM PASS (2026-08, D7). Two-seed decade album chart-week shares against the
		// author's estimates: Jazz ran 35% at 1960 vs ~2 (biggest error), Country 8->17% vs ~2->8,
		// Comedy 2% vs ~8 early, Soul 0->3% vs 0->22 late. Album chart presence tracks album units
		// directly (no airplay channel), so per-genre album affinity is the direct lever here.
		// Jazz 0.72 -> 0.35: cut the massive album over-presence (jazz stays album-skewed, just far
		// less dominant); the residual is expected to still read a few points over on a first pass.
		// Jazz 0.35 -> 0.20 (2026-08): jazz album 1960 stayed over (13.7% vs 2 in the decade run; volatile
		// with the field), so the early-differentiating affinity is cut harder. Late jazz album is small
		// regardless (affinity clamps late), so this touches only the over-supplied early years.
		Genre.Jazz => 0.20f,
		Genre.BossaNova => 0.72f,
		// Country 0.42 -> 0.26 (2026-08 pass 3): a decade run put country album at 8.1% in 1960 (est ~2),
		// up from the old 2.6% -- the Classical/Jazz/EL affinity cuts freed early-album share that
		// high-affinity country over-absorbed. Cutting the early-differentiating affinity hands it back;
		// late country album is baseline-driven (clamped) and stays near its ~8-10% target. (The country
		// SINGLES "under" vs the CSV is the chart-efficiency divergence -- the CSV is a market/units
		// benchmark; the hand-count chart target is ~3 -- and is deliberately NOT chased here.)
		Genre.Country => 0.26f,
		Genre.Gospel or Genre.Blues => 0.58f,
		// Comedy 0.68 -> 0.56 (2026-08): 0.68 was calibrated against the OLD field; cutting Classical
		// (0.82->0.45, -23% of 1960 album share) and Jazz/EL affinities freed ~11 early album points and
		// high-affinity comedy over-absorbed them (a 1960-62 probe put comedy album at 13.5% vs ~8). A cut
		// to 0.50 then over-trimmed (decade run 4.4% vs the ~8 early target), so eased to 0.56. Still
		// album-skewed (the early-60s comedy LP boom was real -- Newhart/Cosby); the declining baseline
		// still gives the falling 8->1 shape. V3: 0.56 -> 0.44 -- comedy over-absorbed early freed share
		// again (12.8% at 1960 vs 8), so trimmed toward the target.
		Genre.Comedy => 0.44f,
		// TradPop 0.50 -> 0.36 (2026-08 V3): trims the EARLY album over-presence (45% at 1960 vs 36) --
		// TradPop is the biggest album genre so it absorbs the most freed early share; affinity only
		// differentiates early (clamps late), so this pulls 1960-62 down without touching the now-correct
		// late decline. Paired with the mid/late baseline cuts.
		Genre.TraditionalPop => 0.36f,
		// Late-decade additions: album-present rock/pop (CCR/Band/Beatles/Neil Diamond LPs were major),
		// above the pop/rock floor but below the jazz/classical album-centric tier.
		Genre.RootsRock or Genre.PopRock => 0.48f,
		Genre.PsychedelicPop => 0.52f,
		// Soul 0.30 -> 0.55: the late-60s soul LP era (Motown/Stax/Atlantic albums) was a major album
		// presence the flat 0.30 could not produce; the rising Soul baseline (.41->.70) supplies the
		// 0->22 time-shape, the higher affinity supplies the level. Funk stays single-oriented at 0.30.
		Genre.Soul => 0.55f,
		Genre.Funk => 0.30f,
		Genre.RockAndRoll or Genre.TeenPop or Genre.RnB or Genre.DooWop => 0.22f,
		_ => NeutralAlbumAffinityBaseline
	};

	// LP-RATIO RECALIBRATION (2026-08, album-format work). The old additive decadeLift drove every
	// genre to the clamp late (affinity uniformly 1.0 by 1969) and, worse, COMPRESSED the genre
	// spread early -- adding the same lift to jazz and to rock left a singles genre nearly as
	// album-oriented as an LP genre. The author wants affinity load-bearing early: jazz/classical
	// route to albums in 1960, pop/rock mostly stay singles but a handful still chart. So the lift is
	// now MULTIPLICATIVE (baseline x era boost), which scales the LP revolution up while preserving
	// the genre ordering at every era, and the early youth penalty is softened (the adult LP market
	// existed in 1960). The 0.05 floor keeps low-affinity pop/rock non-zero so a handful chart.
	private float ShapeAlbumAffinity(float baseline, int year) {
		float eraProgress = GetAlbumDemandEraProgress(year);
		float eraBoost = 1f + 1.5f * eraProgress;
		float youthPenalty = youthPercentage * Mathf.Lerp(0.40f, 0.12f, eraProgress);
		return Mathf.Clamp(baseline * (1f - youthPenalty) * eraBoost, 0.05f, 1f);
	}

	// LP-RATIO RECALIBRATION (2026-08). Willingness at ~0.15 in 1960 was the dominant suppressor of
	// early album units (the mature adult LP market did not exist in the model). Base raised
	// 0.30 -> 0.45 and the early youth price penalty softened 1.25 -> 0.55, lifting 1960 willingness
	// toward ~0.5. This is the genre-BLIND market level; genre enters the split through affinity above.
	// 1960 COLD-START LIFT (2026-08): base 0.45 -> 0.70. Willingness SIZES the album buyer pool, which
	// is the true 1960 constraint -- titles (priorUnitScalarAlbum) and BasePurchaseRate both saturate
	// ~27% because the pool caps them; only enlarging the pool lifts 1960 units. This is inert above the
	// channel (1961+ saturate the channel regardless), so it moves ONLY 1960: 19.6% -> 29.8% with 1963/
	// 1969 unchanged at 40.8/54.7. Pairs with BasePurchaseRate 0.095 (AlbumSimulator.cs), which converts
	// the relieved pool. Decade-validated seed 1001. See D7SimRuntimeOptimizationHandoff §4.
	public float GetAlbumPurchaseWillingness(int year) {
		float normalizedIncome = Mathf.Clamp((averageIncome - 0.70f) / 0.55f, 0f, 1f);
		float audienceAging = GetAlbumDemandEraProgress(year);
		float youthPricePenalty = youthPercentage * Mathf.Lerp(0.55f, 0.30f, audienceAging);
		return Mathf.Clamp(0.70f + normalizedIncome * 0.48f + audienceAging * 0.25f - youthPricePenalty, 0.08f, 1f);
	}

	public float GetAlbumDemandEraProgress(float year) {
		if (albumDemandRiseEndYear <= albumDemandRiseStartYear)
			return year >= albumDemandRiseEndYear ? 1f : 0f;
		return Mathf.Clamp((year - albumDemandRiseStartYear) /
			(albumDemandRiseEndYear - albumDemandRiseStartYear), 0f, 1f);
	}
	
	public float GetBuyingPopulationPercentage() {
		float youthFactor = 0.3f + (youthPercentage * 0.5f);
		float incomeFactor = Mathf.Sqrt(averageIncome);
		float urbanFactor = 0.6f + (urbanization * 0.4f);
		return Mathf.Clamp(youthFactor * incomeFactor * urbanFactor * 0.032f, 0f, 1f);
	}

	/// <summary>
	/// Weekly record-buying population of this region. Every absolute weekly unit
	/// threshold in the demand and breakout models is implicitly denominated in
	/// this quantity, so it is the correct basis for expressing such a threshold
	/// as a region-relative one.
	/// </summary>
	public float GetRecordBuyingPopulation() =>
		population * 1000000f * GetBuyingPopulationPercentage();

	public float GetGenreAcceptance(Genre genre, int year) => GetGenreAcceptance(genre, (float)year);

	public float GetGenreAcceptance(Genre genre, float year) {
		if (GenreMarketV2.Enabled && genreMarketV2Live) {
			float legacyMomentum = ChartManager.Instance?.GetGenreMomentum(genre) ?? (genreMomentum != null && genreMomentum.TryGetValue(genre, out float value) ? value : 0f);
			return GenreAcceptanceService.GetRegionalDemandAcceptance(genre, genre, this, year, legacyMomentum);
		}
		return GetLegacyGenreAcceptance(genre, year);
	}
	
	public float GetSegregationFactor(Genre genre) {
		bool isBlackGenre = genre == Genre.RnB || genre == Genre.Soul || genre == Genre.Gospel || genre == Genre.DooWop;
		if (!isBlackGenre) return 1f;
		float whiteAccess = currentIntegration;
		float blackMarketShare = blackPopulation;
		float whiteMarketShare = (1f - blackPopulation) * whiteAccess;
		return blackMarketShare + whiteMarketShare;
	}
	
	private float GetYearEvolution(Genre genre, int year) {
		int yearOffset = year - 1960;
		return genre switch {
			Genre.RockAndRoll => yearOffset * 0.02f,
			Genre.Soul => yearOffset * 0.025f * (0.5f + currentIntegration * 0.5f),
			Genre.RnB => yearOffset * 0.02f * (0.5f + currentIntegration * 0.5f),
			Genre.Psychedelic => year >= 1966 ? (year - 1966) * 0.1f : -0.5f,
			Genre.AcidRock => year >= 1967 ? (year - 1967) * 0.08f : -0.8f,
			Genre.Folk => year <= 1965 ? (year - 1960) * 0.04f : 0.2f - (year - 1965) * 0.03f,
			Genre.TraditionalPop => -yearOffset * 0.015f,
			Genre.EasyListening => -yearOffset * 0.01f,
			Genre.BritishInvasion => year >= 1964 && year <= 1967 ? 0.3f : 0f,
			Genre.SurfRock => year >= 1962 && year <= 1965 ? 0.2f : -0.1f,
			Genre.GarageRock => year >= 1965 && year <= 1967 ? 0.15f : 0f,
			Genre.Country => yearOffset * 0.01f,
			Genre.Gospel => 0f,
			_ => 0f
		};
	}
	
	public float GetRadioPlayPotential(Genre genre, int year) {
		float baseGenreAcceptance = GetGenreAcceptance(genre, year);
		float formatBonus = 0f;
		if (media != null) {
			if (media.hasTop40Stations && IsTop40Genre(genre)) formatBonus += 0.3f;
			if (media.hasRnBStations && IsRnBGenre(genre)) formatBonus += 0.4f;
			if (media.hasCountryStations && genre == Genre.Country) formatBonus += 0.5f;
			if (media.hasFMUnderground && year >= 1967 && IsAlbumRockGenre(genre)) formatBonus += 0.4f;
			float payolaFactor = media.payolaSusceptibility;
			return Mathf.Clamp(baseGenreAcceptance + formatBonus, 0f, 1f) * (0.7f + payolaFactor * 0.3f);
		}
		return baseGenreAcceptance;
	}
	
	private bool IsTop40Genre(Genre g) => g == Genre.RockAndRoll || g == Genre.TeenPop || g == Genre.Soul || g == Genre.BritishInvasion || g == Genre.TraditionalPop;
	private bool IsRnBGenre(Genre g) => g == Genre.RnB || g == Genre.Soul || g == Genre.DooWop || g == Genre.Gospel;
	private bool IsAlbumRockGenre(Genre g) => g == Genre.Psychedelic || g == Genre.AcidRock || g == Genre.HardRock || g == Genre.FolkRock || g == Genre.ProgressiveRock;
}

// FIX: Restored to plain enums instead of wrapping in a Resource class
public enum RegionTier { Major, Regional, Secondary, Local }
public enum RegionType { Coastal, Heartland, Southern, Western, Industrial }
