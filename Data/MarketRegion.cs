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
	[Export(PropertyHint.Range, "1958,1970,0.1")] public float albumDemandRiseStartYear = 1964f;
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
		
		currentIntegration = integrationLevel;
		currentProgressivism = culturalProgressivism;
		segmentCapacities = SegmentCapacityModel.Create(this, startYear);
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
			// Segment routing supplies texture, but Album opportunity is accepted at
			// the established regional baseline.  Normalize from fixed inputs here,
			// before record quality, awareness, stock, or format tilt can compound it.
			return explanation.EnabledPreTiltBuyerPool * explanation.OpportunityNormalization;
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

	/// <summary>Accepted legacy genre buyer pool used as the common Album-prior denominator.</summary>
	public float GetAcceptedLegacyGenreMarketSize(Genre genre, float year) {
		float buyingPopulation = population * 1000000f * GetBuyingPopulationPercentage();
		return buyingPopulation * GetLegacyGenreAcceptance(genre, year) * GetSegregationFactor(genre);
	}

	/// <summary>Accepted Album buyer pool before format tilt and record-specific conversion.</summary>
	public float GetAcceptedPreTiltAlbumMarketSize(Genre genre, float year) =>
		GetAlbumDemandExplanation(genre, year).AcceptedPreTiltBuyerPool;

	public float GetAlbumAffinity(Genre genre, int year) {
		float baseline = genre switch {
			Genre.Jazz => 0.90f,
			Genre.EasyListening => 0.88f,
			Genre.Folk => 0.78f,
			Genre.TraditionalPop => 0.72f,
			Genre.BossaNova => 0.72f,
			Genre.Country or Genre.Gospel or Genre.Blues => 0.58f,
			Genre.RockAndRoll or Genre.TeenPop or Genre.RnB or Genre.DooWop or Genre.GirlGroup => 0.22f,
			_ => 0.40f
		};
		float eraProgress = GetAlbumDemandEraProgress(year);
		float decadeLift = Mathf.SmoothStep(0f, 0.58f, eraProgress);
		float youthPenalty = youthPercentage * Mathf.Lerp(0.75f, 0.12f, eraProgress);
		return Mathf.Clamp(baseline * (1f - youthPenalty) + decadeLift, 0.05f, 1f);
	}

	public float GetAlbumPurchaseWillingness(int year) {
		float normalizedIncome = Mathf.Clamp((averageIncome - 0.70f) / 0.55f, 0f, 1f);
		float audienceAging = GetAlbumDemandEraProgress(year);
		float youthPricePenalty = youthPercentage * Mathf.Lerp(1.25f, 0.35f, audienceAging);
		return Mathf.Clamp(0.30f + normalizedIncome * 0.48f + audienceAging * 0.25f - youthPricePenalty, 0.08f, 1f);
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
