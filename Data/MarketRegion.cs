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
	}
	
	public float GetGenreMarketSize(Genre genre, int year) {
		float baseMarket = population * 1000000f;
		float buyingPopulation = baseMarket * GetBuyingPopulationPercentage();
		float acceptance = GetGenreAcceptance(genre, year);
		float segregationFactor = GetSegregationFactor(genre);
		return buyingPopulation * acceptance * segregationFactor;
	}
	
	public float GetBuyingPopulationPercentage() {
		float youthFactor = 0.3f + (youthPercentage * 0.5f);
		float incomeFactor = Mathf.Sqrt(averageIncome);
		float urbanFactor = 0.6f + (urbanization * 0.4f);
		return Mathf.Clamp(youthFactor * incomeFactor * urbanFactor * 0.032f, 0f, 1f);
	}
	
	public float GetGenreAcceptance(Genre genre, int year) {
		if (currentGenreAcceptance == null || !currentGenreAcceptance.ContainsKey(genre)) {
			return culturalProgressivism * 0.3f;
		}
		float baseAcceptance = currentGenreAcceptance[genre];
		float yearModifier = GetYearEvolution(genre, year);
		float momentum = (genreMomentum != null && genreMomentum.ContainsKey(genre)) ? genreMomentum[genre] : 0f;
		return Mathf.Clamp(baseAcceptance + yearModifier + momentum, 0f, 1f);
	}
	
	private float GetSegregationFactor(Genre genre) {
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
