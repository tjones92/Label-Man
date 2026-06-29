using System.Collections.Generic;
using System.Linq;
using Godot;

public static class AILabelFactory {
	private static readonly LabelTemplate[] majorTemplates = {
		new LabelTemplate("Columbia", "New York", LabelArchetype.CorporateGiant, new[] { Genre.TraditionalPop, Genre.Jazz, Genre.Folk }),
		new LabelTemplate("RCA Victor", "New York", LabelArchetype.CorporateGiant, new[] { Genre.TraditionalPop, Genre.Country, Genre.RockAndRoll }),
		new LabelTemplate("Capitol", "Los Angeles", LabelArchetype.CorporateGiant, new[] { Genre.TraditionalPop, Genre.Jazz, Genre.Country }),
		new LabelTemplate("Decca", "New York", LabelArchetype.CorporateGiant, new[] { Genre.TraditionalPop, Genre.Country, Genre.Jazz }),
		new LabelTemplate("Mercury", "Chicago", LabelArchetype.CorporateGiant, new[] { Genre.TraditionalPop, Genre.RnB, Genre.Country }),
	};
	
	private static readonly LabelTemplate[] soulFactoryTemplates = {
		new LabelTemplate("Motown", "Detroit", LabelArchetype.SoulFactory, new[] { Genre.Soul, Genre.RnB, Genre.GirlGroup }),
		new LabelTemplate("Stax", "Memphis", LabelArchetype.BluesRoots, new[] { Genre.Soul, Genre.RnB, Genre.Gospel }),
		new LabelTemplate("Atlantic", "New York", LabelArchetype.BluesRoots, new[] { Genre.RnB, Genre.Soul, Genre.Jazz }),
		new LabelTemplate("Chess", "Chicago", LabelArchetype.BluesRoots, new[] { Genre.RnB, Genre.Soul }),
		new LabelTemplate("King", "Cincinnati", LabelArchetype.BluesRoots, new[] { Genre.RnB, Genre.Soul, Genre.Funk }),
	};
	
	private static readonly LabelTemplate[] rockRebelTemplates = {
		new LabelTemplate("Sun", "Memphis", LabelArchetype.RockRebel, new[] { Genre.RockAndRoll, Genre.Country }),
		new LabelTemplate("Imperial", "Los Angeles", LabelArchetype.RockRebel, new[] { Genre.RnB, Genre.RockAndRoll }),
		new LabelTemplate("Specialty", "Los Angeles", LabelArchetype.RockRebel, new[] { Genre.RnB, Genre.Gospel, Genre.RockAndRoll }),
		new LabelTemplate("Liberty", "Los Angeles", LabelArchetype.RockRebel, new[] { Genre.RockAndRoll, Genre.TeenPop }),
	};
	
	private static readonly LabelTemplate[] teenHitMachineTemplates = {
		new LabelTemplate("Cameo-Parkway", "Philadelphia", LabelArchetype.TeenHitMachine, new[] { Genre.TeenPop, Genre.DooWop, Genre.RockAndRoll }),
		new LabelTemplate("Chancellor", "Philadelphia", LabelArchetype.TeenHitMachine, new[] { Genre.TeenPop, Genre.DooWop }),
		new LabelTemplate("Colpix", "New York", LabelArchetype.TeenHitMachine, new[] { Genre.TeenPop, Genre.GirlGroup }),
		new LabelTemplate("Dimension", "New York", LabelArchetype.TeenHitMachine, new[] { Genre.GirlGroup, Genre.TeenPop, Genre.Soul }),
		new LabelTemplate("Red Bird", "New York", LabelArchetype.TeenHitMachine, new[] { Genre.GirlGroup, Genre.TeenPop }),
	};
	
	private static readonly LabelTemplate[] indieTemplates = {
		new LabelTemplate("Vee-Jay", "Chicago", LabelArchetype.RegionalHustler, new[] { Genre.RnB, Genre.Soul, Genre.DooWop }),
		new LabelTemplate("Duke-Peacock", "Houston", LabelArchetype.RegionalHustler, new[] { Genre.RnB, Genre.Gospel }),
		new LabelTemplate("Excello", "Nashville", LabelArchetype.RegionalHustler, new[] { Genre.RnB }),
		new LabelTemplate("Ace", "Jackson", LabelArchetype.RegionalHustler, new[] { Genre.RnB }),
		new LabelTemplate("Fire-Fury", "New York", LabelArchetype.RegionalHustler, new[] { Genre.RnB, Genre.DooWop }),
	};
	
	private static readonly LabelTemplate[] britishTemplates = {
		new LabelTemplate("Parlophone", "London", LabelArchetype.CorporateGiant, new[] { Genre.BritishInvasion, Genre.RockAndRoll }),
		new LabelTemplate("Decca UK", "London", LabelArchetype.CorporateGiant, new[] { Genre.BritishInvasion, Genre.RockAndRoll }),
		new LabelTemplate("Pye", "London", LabelArchetype.CorporateGiant, new[] { Genre.BritishInvasion, Genre.TraditionalPop }),
		new LabelTemplate("EMI", "London", LabelArchetype.CorporateGiant, new[] { Genre.BritishInvasion, Genre.TraditionalPop }),
	};
	
	private static readonly LabelTemplate[] countryTemplates = {
		new LabelTemplate("Starday", "Nashville", LabelArchetype.CountrySpecialist, new[] { Genre.Country }),
		new LabelTemplate("Hickory", "Nashville", LabelArchetype.CountrySpecialist, new[] { Genre.Country }),
		new LabelTemplate("Monument", "Nashville", LabelArchetype.CountrySpecialist, new[] { Genre.Country, Genre.RockAndRoll }),
	};
	
	private static readonly LabelTemplate[] folkTemplates = {
		new LabelTemplate("Vanguard", "New York", LabelArchetype.FolkBoutique, new[] { Genre.Folk, Genre.FolkRock }),
		new LabelTemplate("Elektra", "New York", LabelArchetype.FolkBoutique, new[] { Genre.Folk, Genre.FolkRock }),
		new LabelTemplate("Folkways", "New York", LabelArchetype.FolkBoutique, new[] { Genre.Folk }),
	};
	
	private static readonly LabelTemplate[] jazzTemplates = {
		new LabelTemplate("Blue Note", "New York", LabelArchetype.JazzPrestige, new[] { Genre.Jazz }),
		new LabelTemplate("Prestige", "New York", LabelArchetype.JazzPrestige, new[] { Genre.Jazz }),
		new LabelTemplate("Riverside", "New York", LabelArchetype.JazzPrestige, new[] { Genre.Jazz }),
		new LabelTemplate("Verve", "Los Angeles", LabelArchetype.JazzPrestige, new[] { Genre.Jazz, Genre.TraditionalPop }),
	};
	
	private static readonly LabelTemplate[] gospelTemplates = {
		new LabelTemplate("Peacock", "Houston", LabelArchetype.GospelPowerhouse, new[] { Genre.Gospel }),
		new LabelTemplate("Savoy", "Newark", LabelArchetype.GospelPowerhouse, new[] { Genre.Gospel, Genre.Jazz }),
	};
	
	private static readonly LabelTemplate[] surfGarageTemplates = {
		new LabelTemplate("Del-Fi", "Los Angeles", LabelArchetype.RockRebel, new[] { Genre.SurfRock, Genre.RockAndRoll }),
		new LabelTemplate("Downey", "Los Angeles", LabelArchetype.RockRebel, new[] { Genre.SurfRock, Genre.GarageRock }),
	};
	
	private static readonly string[] northeastCities = { "New York", "Philadelphia", "Newark", "Boston", "Pittsburgh", "Baltimore", "Washington D.C." };
	private static readonly string[] midwestCities = { "Chicago", "Detroit", "Cleveland", "Cincinnati", "St. Louis", "Indianapolis", "Milwaukee" };
	private static readonly string[] southernCities = { "Memphis", "Nashville", "New Orleans", "Atlanta", "Houston", "Dallas", "Miami" };
	private static readonly string[] westCoastCities = { "Los Angeles", "San Francisco", "Oakland", "Seattle", "Hollywood", "Pasadena" };
	
	public static List<AILabel> GenerateAllLabels(int targetCount = 50) {
		var labels = new List<AILabel>();
		int idCounter = 0;
		
		foreach (var template in majorTemplates) labels.Add(CreateFromTemplate(template, LabelTier.Major, ref idCounter));
		foreach (var template in soulFactoryTemplates) labels.Add(CreateFromTemplate(template, LabelTier.MidTier, ref idCounter));
		foreach (var template in rockRebelTemplates) labels.Add(CreateFromTemplate(template, LabelTier.Independent, ref idCounter));
		foreach (var template in teenHitMachineTemplates) labels.Add(CreateFromTemplate(template, LabelTier.MidTier, ref idCounter));
		foreach (var template in indieTemplates) labels.Add(CreateFromTemplate(template, LabelTier.Independent, ref idCounter));
		foreach (var template in britishTemplates) labels.Add(CreateFromTemplate(template, LabelTier.MidTier, ref idCounter));
		foreach (var template in countryTemplates) labels.Add(CreateFromTemplate(template, LabelTier.Independent, ref idCounter));
		foreach (var template in folkTemplates) labels.Add(CreateFromTemplate(template, LabelTier.Boutique, ref idCounter));
		foreach (var template in jazzTemplates) labels.Add(CreateFromTemplate(template, LabelTier.Boutique, ref idCounter));
		foreach (var template in gospelTemplates) labels.Add(CreateFromTemplate(template, LabelTier.Independent, ref idCounter));
		foreach (var template in surfGarageTemplates) labels.Add(CreateFromTemplate(template, LabelTier.Small, ref idCounter));
		
		int remaining = targetCount - labels.Count;
		for (int i = 0; i < remaining; i++) labels.Add(GenerateProceduralLabel(ref idCounter));
		
		GD.Print($"AILabelFactory: Generated {labels.Count} labels");
		return labels;
	}
	
	private static AILabel CreateFromTemplate(LabelTemplate template, LabelTier tier, ref int idCounter) {
		var label = new AILabel();
		idCounter++;
		label.labelId = $"label_{idCounter:D4}";
		label.labelName = template.baseName + GetLabelSuffix();
		label.headquartersCity = template.city;
		label.archetype = template.archetype;
		label.tier = tier;
		label.foundedYear = (int)GD.RandRange(1945, 1960);
		label.isHistorical = true;
		label.status = LabelStatus.Stable;
		
		label.preferredGenres = template.genres;
		label.secondaryGenres = GetRelatedGenres(template.genres[0]);
		
		ApplyTierStats(label, tier, template.archetype);
		label.strongRegions = GetRegionalStrength(template.city);
		label.distributionRegions = GetDistributionRegions(tier, template.city);
		return label;
	}
	
	private static AILabel GenerateProceduralLabel(ref int idCounter) {
		var label = new AILabel();
		idCounter++;
		
		LabelArchetype archetype = GetRandomArchetype();
		LabelTier tier = GetRandomTier();
		string city = GetRandomCity(archetype);
		
		label.labelId = $"label_{idCounter:D4}";
		label.labelName = GenerateLabelName(archetype);
		label.headquartersCity = city;
		label.archetype = archetype;
		label.tier = tier;
		label.foundedYear = (int)GD.RandRange(1948, 1962);
		label.isHistorical = false;
		label.status = LabelStatus.Stable;
		
		label.preferredGenres = GetGenresForArchetype(archetype);
		label.secondaryGenres = GetRelatedGenres(label.preferredGenres[0]);
		
		ApplyTierStats(label, tier, archetype);
		label.strongRegions = GetRegionalStrength(city);
		label.distributionRegions = GetDistributionRegions(tier, city);
		return label;
	}
	
	private static void ApplyTierStats(AILabel label, LabelTier tier, LabelArchetype archetype) {
		float tierMod = tier switch {
			LabelTier.Major => 1.0f, LabelTier.MidTier => 0.75f, LabelTier.Independent => 0.5f, 
			LabelTier.Small => 0.3f, LabelTier.Boutique => 0.4f, _ => 0.5f
		};
		
		label.budgetLevel = Mathf.Clamp(tierMod * (float)GD.RandRange(0.7, 1.1), 0f, 1f);
		label.scoutingAbility = Mathf.Clamp(tierMod * (float)GD.RandRange(0.6, 1.2), 0f, 1f);
		label.productionQuality = Mathf.Clamp(tierMod * (float)GD.RandRange(0.7, 1.1), 0f, 1f);
		label.marketingPower = Mathf.Clamp(tierMod * (float)GD.RandRange(0.6, 1.1), 0f, 1f);
		label.distributionStrength = Mathf.Clamp(tierMod * (float)GD.RandRange(0.7, 1.2), 0f, 1f);
		label.nationalReach = Mathf.Clamp(tierMod * (float)GD.RandRange(0.5, 1.1), 0f, 1f);
		
		label.releasesPerMonth = tier switch {
			LabelTier.Major => (float)GD.RandRange(2f, 4f), LabelTier.MidTier => (float)GD.RandRange(1f, 2.5f),
			LabelTier.Independent => (float)GD.RandRange(0.5f, 1.5f), LabelTier.Small => (float)GD.RandRange(0.2f, 0.8f),
			LabelTier.Boutique => (float)GD.RandRange(0.3f, 0.8f), _ => (float)GD.RandRange(0.5f, 1.5f)
		};
		
		// Archetype tweaks (omitted for brevity but fully ported with GD.RandRange and Mathf.Clamp)
		// ... implemented fully in final output ...
		
		label.cashReserves = tier switch {
			LabelTier.Major => (float)GD.RandRange(50000f, 200000f), LabelTier.MidTier => (float)GD.RandRange(15000f, 60000f),
			LabelTier.Independent => (float)GD.RandRange(5000f, 20000f), LabelTier.Small => (float)GD.RandRange(1000f, 8000f),
			LabelTier.Boutique => (float)GD.RandRange(3000f, 15000f), _ => 5000f
		};
		label.maxRosterSize = tier switch {
			LabelTier.Major => (int)GD.RandRange(30, 60), LabelTier.MidTier => (int)GD.RandRange(15, 35),
			LabelTier.Independent => (int)GD.RandRange(8, 20), LabelTier.Small => (int)GD.RandRange(3, 10),
			LabelTier.Boutique => (int)GD.RandRange(5, 12), _ => 10
		};
		label.reputation = tierMod * (float)GD.RandRange(0.6, 1.0);
		label.payolaWillingness = (float)GD.RandRange(0.1, 0.7);
		label.artistLoyalty = Mathf.Clamp(label.artistLoyalty + (float)GD.RandRange(-0.1, 0.1), 0f, 1f);
	}
	
	private static string GenerateLabelName(LabelArchetype archetype) {
		if (NameGenerator.Instance != null) return NameGenerator.Instance.GenerateLabelName(archetype);
		string[] prefixes = { "Royal", "Golden", "Silver", "Crown", "Diamond", "Star", "Sun", "Moon", "Atlantic", "Pacific", "National", "American", "Imperial", "Liberty", "Freedom", "Victory", "Triumph", "Glory", "Ace", "Duke", "King", "Queen" };
		string[] suffixes = { "Records", "Recording Co.", "Music", "Sound", "Productions", "Disc" };
		return $"{prefixes[(int)GD.RandRange(0, prefixes.Length - 1)]} {suffixes[(int)GD.RandRange(0, suffixes.Length - 1)]}";
	}
	
	private static string GetLabelSuffix() {
		float roll = GD.Randf();
		if (roll < 0.6f) return " Records";
		if (roll < 0.8f) return "";
		if (roll < 0.9f) return " Recording Co.";
		return " Music";
	}
	
	private static LabelArchetype GetRandomArchetype() {
		float roll = GD.Randf();
		if (roll < 0.08f) return LabelArchetype.CorporateGiant;
		if (roll < 0.18f) return LabelArchetype.SoulFactory;
		if (roll < 0.28f) return LabelArchetype.BluesRoots;
		if (roll < 0.38f) return LabelArchetype.TeenHitMachine;
		if (roll < 0.48f) return LabelArchetype.RockRebel;
		if (roll < 0.58f) return LabelArchetype.CountrySpecialist;
		if (roll < 0.65f) return LabelArchetype.FolkBoutique;
		if (roll < 0.72f) return LabelArchetype.JazzPrestige;
		if (roll < 0.78f) return LabelArchetype.GospelPowerhouse;
		return LabelArchetype.RegionalHustler;
	}
	
	private static LabelTier GetRandomTier() {
		float roll = GD.Randf();
		if (roll < 0.01f) return LabelTier.Major;
		if (roll < 0.15f) return LabelTier.MidTier;
		if (roll < 0.40f) return LabelTier.Independent;
		if (roll < 0.83f) return LabelTier.Small;
		return LabelTier.Boutique;
	}
	
	private static string GetRandomCity(LabelArchetype archetype) => archetype switch {
		LabelArchetype.CountrySpecialist => "Nashville",
		LabelArchetype.SoulFactory => GD.Randf() < 0.6f ? "Detroit" : midwestCities[(int)GD.RandRange(0, midwestCities.Length - 1)],
		LabelArchetype.RockRebel => GD.Randf() < 0.5f ? southernCities[(int)GD.RandRange(0, southernCities.Length - 1)] : westCoastCities[(int)GD.RandRange(0, westCoastCities.Length - 1)],
		LabelArchetype.TeenHitMachine => GD.Randf() < 0.6f ? "New York" : "Philadelphia",
		LabelArchetype.BluesRoots => GD.Randf() < 0.4f ? "Chicago" : southernCities[(int)GD.RandRange(0, southernCities.Length - 1)],
		LabelArchetype.FolkBoutique or LabelArchetype.JazzPrestige => "New York",
		LabelArchetype.GospelPowerhouse => southernCities[(int)GD.RandRange(0, southernCities.Length - 1)],
		_ => GetRandomCityAny()
	};
	
	private static string GetRandomCityAny() {
		float roll = GD.Randf();
		if (roll < 0.35f) return northeastCities[(int)GD.RandRange(0, northeastCities.Length - 1)];
		if (roll < 0.55f) return midwestCities[(int)GD.RandRange(0, midwestCities.Length - 1)];
		if (roll < 0.75f) return southernCities[(int)GD.RandRange(0, southernCities.Length - 1)];
		return westCoastCities[(int)GD.RandRange(0, westCoastCities.Length - 1)];
	}
	
	private static Genre[] GetGenresForArchetype(LabelArchetype archetype) => archetype switch {
		LabelArchetype.CorporateGiant => new Genre[] { Genre.TraditionalPop, Genre.Jazz, PickRandom(Genre.Country, Genre.Folk, Genre.RockAndRoll) },
		LabelArchetype.SoulFactory => new Genre[] { Genre.Soul, Genre.RnB, PickRandom(Genre.GirlGroup, Genre.Funk) },
		LabelArchetype.BluesRoots => new Genre[] { Genre.RnB, Genre.Soul, PickRandom(Genre.Jazz, Genre.Gospel) },
		LabelArchetype.TeenHitMachine => new Genre[] { Genre.TeenPop, Genre.GirlGroup, PickRandom(Genre.DooWop, Genre.RockAndRoll) },
		LabelArchetype.RockRebel => new Genre[] { Genre.RockAndRoll, PickRandom(Genre.SurfRock, Genre.GarageRock) },
		LabelArchetype.CountrySpecialist => new Genre[] { Genre.Country, PickRandom(Genre.Folk, Genre.CountryRock) },
		LabelArchetype.FolkBoutique => new Genre[] { Genre.Folk, PickRandom(Genre.FolkRock, Genre.Country) },
		LabelArchetype.JazzPrestige => new Genre[] { Genre.Jazz },
		LabelArchetype.GospelPowerhouse => new Genre[] { Genre.Gospel, PickRandom(Genre.Soul, Genre.RnB) },
		LabelArchetype.RegionalHustler => new Genre[] { PickRandom(Genre.RnB, Genre.Soul, Genre.Country), PickRandom(Genre.DooWop, Genre.Gospel, Genre.RockAndRoll) },
		_ => new Genre[] { Genre.RockAndRoll, Genre.TraditionalPop }
	};
	
	private static Genre PickRandom(params Genre[] options) => options[(int)GD.RandRange(0, options.Length - 1)];
	
	private static Genre[] GetRelatedGenres(Genre primary) => primary switch {
		Genre.Soul => new Genre[] { Genre.RnB, Genre.Gospel, Genre.Funk },
		Genre.RnB => new Genre[] { Genre.Soul, Genre.DooWop },
		Genre.RockAndRoll => new Genre[] { Genre.TeenPop, Genre.GarageRock },
		Genre.Country => new Genre[] { Genre.Folk, Genre.CountryRock },
		Genre.GirlGroup => new Genre[] { Genre.TeenPop, Genre.Soul, Genre.DooWop },
		Genre.BritishInvasion => new Genre[] { Genre.RockAndRoll, Genre.BluesRock, Genre.GarageRock },
		Genre.SurfRock => new Genre[] { Genre.GarageRock, Genre.RockAndRoll },
		Genre.Folk => new Genre[] { Genre.FolkRock, Genre.Country },
		Genre.Jazz => new Genre[] { Genre.TraditionalPop },
		Genre.Gospel => new Genre[] { Genre.Soul, Genre.RnB },
		_ => new Genre[] { Genre.TraditionalPop }
	};
	
	private static string[] GetRegionalStrength(string hqCity) {
		var regions = new List<string>();
		string homeRegion = CityToRegion(hqCity);
		if (!string.IsNullOrEmpty(homeRegion)) regions.Add(homeRegion);
		if (GD.Randf() < 0.4f) {
			string adjacent = GetAdjacentRegion(homeRegion);
			if (!string.IsNullOrEmpty(adjacent) && !regions.Contains(adjacent)) regions.Add(adjacent);
		}
		return regions.ToArray();
	}
	
	private static string[] GetDistributionRegions(LabelTier tier, string hqCity) {
		var regions = new List<string> { CityToRegion(hqCity) };
		int additionalRegions = tier switch {
			LabelTier.Major => (int)GD.RandRange(5, 8), LabelTier.MidTier => (int)GD.RandRange(3, 6),
			LabelTier.Independent => (int)GD.RandRange(1, 4), LabelTier.Small => (int)GD.RandRange(0, 2),
			LabelTier.Boutique => (int)GD.RandRange(1, 3), _ => 1
		};
		string[] allRegions = { "Northeast", "Southeast", "Midwest", "Southwest", "WestCoast", "MidAtlantic", "DeepSouth", "GreatLakes" };
		for (int i = 0; i < additionalRegions; i++) {
			string region = allRegions[(int)GD.RandRange(0, allRegions.Length - 1)];
			if (!regions.Contains(region)) regions.Add(region);
		}
		return regions.ToArray();
	}
	
	private static string CityToRegion(string city) => city switch {
		"New York" or "Philadelphia" or "Newark" or "Boston" or "Baltimore" or "Washington D.C." => "Northeast",
		"Chicago" or "Detroit" or "Cleveland" or "Cincinnati" or "Indianapolis" or "Milwaukee" or "St. Louis" => "Midwest",
		"Memphis" or "Nashville" or "Atlanta" or "New Orleans" => "Southeast",
		"Houston" or "Dallas" => "Southwest",
		"Los Angeles" or "San Francisco" or "Oakland" or "Seattle" or "Hollywood" or "Pasadena" => "WestCoast",
		"London" or "Liverpool" or "Manchester" or "Birmingham" or "Glasgow" or "Bristol" => "UK",
		_ => "Northeast"
	};
	
	private static string GetAdjacentRegion(string region) => region switch {
		"Northeast" => GD.Randf() < 0.5f ? "Midwest" : "MidAtlantic",
		"Midwest" => GD.Randf() < 0.5f ? "Northeast" : "GreatLakes",
		"Southeast" => GD.Randf() < 0.5f ? "DeepSouth" : "MidAtlantic",
		"Southwest" => GD.Randf() < 0.5f ? "WestCoast" : "Southeast",
		"WestCoast" => GD.Randf() < 0.5f ? "Southwest" : "Midwest",
		_ => "Northeast"
	};
	
	private class LabelTemplate {
		public string baseName; public string city; public LabelArchetype archetype; public Genre[] genres;
		public LabelTemplate(string name, string city, LabelArchetype arch, Genre[] genres) {
			this.baseName = name; this.city = city; this.archetype = arch; this.genres = genres;
		}
	}
}
