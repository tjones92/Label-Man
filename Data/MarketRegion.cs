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
	public const float IntegrationEraGapClose = 0.45f;

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
		Genre.EasyListening => 0.65f,
		Genre.Classical => 0.82f,
		Genre.PsychedelicRock => 0.78f,
		Genre.Folk => 0.78f,
		Genre.Jazz => 0.72f,
		Genre.BossaNova => 0.72f,
		Genre.Country or Genre.Gospel or Genre.Blues => 0.58f,
		Genre.TraditionalPop => 0.50f,
		Genre.Soul or Genre.Funk => 0.30f,
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
