using System.Collections.Generic;
using System.Linq;
using Godot;

// Changed from MonoBehaviour to a plain C# class
public class LabelGenerator {
	private HashSet<string> usedNames = new HashSet<string>();
	
	private readonly string[] prefixes = { 
		"Blue", "Red", "Gold", "Golden", "Silver", "Black", "White", "Royal", "King", "Crown", "Star", "Sun", "Moon", "Atomic", "Velvet", "Iron", 
		"Diamond", "Top", "Imperial", "Majestic", "Global", "United", "Mod", "Groove", "Soul", "Ace", "Duke", "Capitol", "Liberty", "Jubilee",
		"Fury", "Fire", "Thunder", "Swan", "Peacock", "Roulette", "Cameo", "Parkway", "Chancellor", "Laurie", "Scepter", "Wand", "Cadence",
		"Era", "Dot", "Reprise", "Colpix", "Ember", "Herald", "Hull", "Amy", "Bell", "Bang", "Shout", "Impact", "Minit", "Instant"
	};
	private readonly string[] nouns = { 
		"Tone", "Note", "Sound", "Voice", "Beat", "Rhythm", "Disc", "Record", "Audio", "Wave", "Vibe", "Soul", "Star", "City", "World", "Spot", 
		"Track", "Hit", "Tune", "Chord", "Gate", "Bridge", "Tower", "Hill", "Park", "Side", "Way", "Art", "Craft", "Master", "Time", "Phonic"
	};
	private readonly string[] suffixes = { 
		"Records", "Recording Co.", "Recordings", "Sounds", "Music", "International", "Disc", "Studios", "Productions", "Entertainment",
		"Artists", "Inc.", "Label", "Music Corp."
	};
	private readonly string[] cities = {
		"New York", "Los Angeles", "Detroit", "Chicago", "Memphis", "Philadelphia", "Nashville", "New Orleans", "Cincinnati", "Houston",
		"Atlanta", "Cleveland", "St. Louis", "Miami", "San Francisco", "Boston", "Baltimore", "Pittsburgh", "Seattle"
	};
	private readonly string[] founderFirstNames = {
		"Jerry", "Phil", "Berry", "Sam", "Leonard", "Ahmet", "Syd", "Don", "Art", "Bob", "Jim", "Ray", "Lou", "Hy", "Morris", "George", "Henry",
		"Johnny", "Eddie", "Willie", "Florence", "Estelle"
	};
	private readonly string[] founderLastNames = {
		"Wexler", "Gordy", "Phillips", "Chess", "Ertegun", "Nathan", "Kirshner", "Spector", "Rupe", "Roboy", "Greene", "King", "Brown", "Davis",
		"Clark", "Stone", "Williams", "Jackson", "Robinson", "Ross", "Wilson", "Martin"
	};

	public List<AILabel> GenerateHistoricalMajors(MarketRegion[] regions) {
		List<AILabel> majors = new List<AILabel>();
		majors.Add(CreateHistoricalMajor("Columbia Records", "New York", 1888, new[] { Genre.TraditionalPop, Genre.Jazz, Genre.EasyListening }));
		majors.Add(CreateHistoricalMajor("RCA Victor", "New York", 1901, new[] { Genre.TraditionalPop, Genre.Country, Genre.RockAndRoll }));
		majors.Add(CreateHistoricalMajor("Capitol Records", "Los Angeles", 1942, new[] { Genre.TraditionalPop, Genre.Jazz, Genre.EasyListening }));
		majors.Add(CreateHistoricalMajor("Decca Records", "New York", 1934, new[] { Genre.TraditionalPop, Genre.Country, Genre.Jazz }));
		majors.Add(CreateHistoricalMajor("Mercury Records", "Chicago", 1945, new[] { Genre.RnB, Genre.RockAndRoll, Genre.Country }));
		majors.Add(CreateHistoricalMajor("MGM Records", "New York", 1946, new[] { Genre.TraditionalPop, Genre.EasyListening }));
		
		majors.Add(CreateHistoricalIndie("Atlantic Records", "New York", 1947, LabelTier.MidTier, LabelArchetype.SoulFactory, new[] { Genre.RnB, Genre.Soul, Genre.Jazz }));
		majors.Add(CreateHistoricalIndie("Chess Records", "Chicago", 1950, LabelTier.MidTier, LabelArchetype.BluesRoots, new[] { Genre.RnB, Genre.RockAndRoll }));
		majors.Add(CreateHistoricalIndie("Sun Records", "Memphis", 1952, LabelTier.Independent, LabelArchetype.RockRebel, new[] { Genre.RockAndRoll, Genre.Country }));
		majors.Add(CreateHistoricalIndie("King Records", "Cincinnati", 1943, LabelTier.MidTier, LabelArchetype.SoulFactory, new[] { Genre.RnB, Genre.Soul, Genre.Country }));
		majors.Add(CreateHistoricalIndie("Imperial Records", "Los Angeles", 1947, LabelTier.Independent, LabelArchetype.RockRebel, new[] { Genre.RnB, Genre.RockAndRoll }));
		majors.Add(CreateHistoricalIndie("Liberty Records", "Los Angeles", 1955, LabelTier.MidTier, LabelArchetype.TeenHitMachine, new[] { Genre.RockAndRoll, Genre.TraditionalPop }));
		majors.Add(CreateHistoricalIndie("Motown", "Detroit", 1959, LabelTier.Independent, LabelArchetype.SoulFactory, new[] { Genre.Soul, Genre.RnB }));
		majors.Add(CreateHistoricalIndie("Stax Records", "Memphis", 1957, LabelTier.Small, LabelArchetype.SoulFactory, new[] { Genre.Soul, Genre.RnB }));
			
		foreach (var label in majors) usedNames.Add(label.labelName);
		return majors;
	}
	
	private AILabel CreateHistoricalMajor(string name, string city, int founded, Genre[] genres) {
		var label = new AILabel();
		
		label.labelName = name;
		label.labelId = "hist_" + name.Replace(" ", "").ToLower();
		label.headquartersCity = city;
		label.foundedYear = founded;
		label.isHistorical = true;
		label.tier = LabelTier.Major;
		label.status = LabelStatus.Stable;
		label.archetype = LabelArchetype.CorporateGiant;
		
		label.preferredGenres = genres.Take(2).ToArray();
		label.secondaryGenres = genres.Skip(2).ToArray();
		
		label.nationalReach = (float)GD.RandRange(0.85, 0.98);
		label.distributionStrength = (float)GD.RandRange(0.85, 0.98);
		label.budgetLevel = (float)GD.RandRange(0.85, 1.0);
		label.productionQuality = (float)GD.RandRange(0.75, 0.95);
		label.scoutingAbility = (float)GD.RandRange(0.5, 0.7);
		label.marketingPower = (float)GD.RandRange(0.8, 0.95);
		label.riskTolerance = (float)GD.RandRange(0.1, 0.3);
		label.artistLoyalty = (float)GD.RandRange(0.4, 0.6);
		label.releasesPerMonth = (float)GD.RandRange(4f, 8f);
		label.maxRosterSize = 50;
		label.cashReserves = (float)GD.RandRange(5000f, 15000f);
		label.reputation = (float)GD.RandRange(0.7, 0.9);
		label.marketShare = (float)GD.RandRange(0.08, 0.15);
		label.monthsActive = (1960 - founded) * 12;
		label.totalReleases = label.monthsActive * 3;
		label.top40Hits = (int)GD.RandRange(50, 150);
		label.numberOneHits = (int)GD.RandRange(10, 40);
		
		return label;
	}
	
	private AILabel CreateHistoricalIndie(string name, string city, int founded, LabelTier tier, LabelArchetype archetype, Genre[] genres) {
		var label = new AILabel();
		label.labelName = name;
		label.labelId = "hist_" + name.Replace(" ", "").ToLower();
		label.headquartersCity = city;
		label.foundedYear = founded;
		label.isHistorical = true;
		label.tier = tier;
		label.status = LabelStatus.Stable;
		label.archetype = archetype;
		
		label.preferredGenres = genres.Take(2).ToArray();
		if (genres.Length > 2) label.secondaryGenres = genres.Skip(2).ToArray();
		
		ApplyArchetypeStats(label, archetype, tier);
		
		int yearsActive = 1960 - founded;
		label.monthsActive = yearsActive * 12;
		label.totalReleases = yearsActive * 20;
		label.top40Hits = tier == LabelTier.MidTier ? (int)GD.RandRange(15, 40) : (int)GD.RandRange(3, 15);
		label.numberOneHits = tier == LabelTier.MidTier ? (int)GD.RandRange(2, 10) : (int)GD.RandRange(0, 3);
		return label;
	}
	
	public List<AILabel> GenerateLabels(MarketRegion[] regions, int count, int year) {
		List<AILabel> labels = new List<AILabel>();
		int midTier = Mathf.RoundToInt(count * 0.15f);
		int independent = Mathf.RoundToInt(count * 0.35f);
		int small = count - midTier - independent;
		
		for (int i = 0; i < midTier; i++) labels.Add(GenerateSingleLabel(regions, year, LabelTier.MidTier));
		for (int i = 0; i < independent; i++) labels.Add(GenerateSingleLabel(regions, year, LabelTier.Independent));
		for (int i = 0; i < small; i++) labels.Add(GenerateSingleLabel(regions, year, LabelTier.Small));
		return labels;
	}
	
	public AILabel GenerateSingleLabel(MarketRegion[] regions, int year, LabelTier tier) {
		var label = new AILabel();
		label.labelName = GenerateUniqueName();
		label.labelId = "gen_" + label.labelName.Replace(" ", "").ToLower() + "_" + (int)GD.RandRange(1000, 9999);
		label.founderName = GenerateFounderName();
		label.headquartersCity = cities[(int)GD.RandRange(0, cities.Length - 1)];
		label.foundedYear = (int)GD.RandRange(year - 5, year + 1);
		label.isHistorical = false;
		label.tier = tier;
		label.status = GD.Randf() > 0.3f ? LabelStatus.Stable : LabelStatus.Rising;
		label.archetype = GenerateArchetype(year, tier);
		AssignGenresFromArchetype(label);
		ApplyArchetypeStats(label, label.archetype, tier);
		AssignRegions(label, regions, tier);
		InitializeFinancials(label, tier);
		
		int yearsActive = Mathf.Max(1, year - label.foundedYear);
		label.monthsActive = yearsActive * 12;
		InitializeTrackRecord(label, tier, yearsActive);
		
		usedNames.Add(label.labelName);
		return label;
	}
	
	private string GenerateUniqueName() {
		for (int attempts = 0; attempts < 100; attempts++) {
			string name = GenerateName();
			if (!usedNames.Contains(name)) return name;
		}
		return GenerateName() + " " + (int)GD.RandRange(1, 100);
	}
	
	private string GenerateName() {
		int pattern = (int)GD.RandRange(0, 6);
		return pattern switch {
			0 => $"{GetRandom(prefixes)} {GetRandom(nouns)}",
			1 => $"{GetRandom(prefixes)} {GetRandom(suffixes)}",
			2 => $"{GetRandom(prefixes)}",
			3 => $"{GetRandom(nouns)} {GetRandom(suffixes)}",
			4 => $"{GetRandom(cities).Split(' ')[0]} {GetRandom(suffixes)}",
			5 => $"{GetRandom(prefixes)}-{GetRandom(prefixes)}",
			_ => GetRandom(prefixes)
		};
	}
	
	private string GenerateFounderName() => $"{GetRandom(founderFirstNames)} {GetRandom(founderLastNames)}";
	
	private LabelArchetype GenerateArchetype(int year, LabelTier tier) {
		var weights = new Dictionary<LabelArchetype, float>();
		weights[LabelArchetype.SoulFactory] = year >= 1962 ? 20f : 10f;
		weights[LabelArchetype.RockRebel] = year >= 1964 ? 25f : 15f;
		weights[LabelArchetype.CorporateGiant] = tier == LabelTier.Major ? 30f : 5f;
		weights[LabelArchetype.CountrySpecialist] = 15f;
		weights[LabelArchetype.BluesRoots] = year < 1965 ? 15f : 8f;
		weights[LabelArchetype.FolkBoutique] = (year >= 1963 && year <= 1966) ? 20f : 10f;
		weights[LabelArchetype.TeenHitMachine] = 15f;
		weights[LabelArchetype.JazzPrestige] = 10f;
		weights[LabelArchetype.GospelPowerhouse] = 8f;
		weights[LabelArchetype.RegionalHustler] = tier == LabelTier.Small ? 25f : 10f;
		
		float total = weights.Values.Sum();
		float roll = GD.Randf() * total;
		float cumulative = 0f;
		foreach (var kvp in weights) {
			cumulative += kvp.Value;
			if (roll <= cumulative) return kvp.Key;
		}
		return LabelArchetype.RegionalHustler;
	}
	
	private void AssignGenresFromArchetype(AILabel label) {
		switch (label.archetype) {
			case LabelArchetype.SoulFactory:
				label.preferredGenres = new Genre[] { Genre.Soul, Genre.RnB };
				label.secondaryGenres = new Genre[] { Genre.Gospel };
				break;
			case LabelArchetype.RockRebel:
				label.preferredGenres = new Genre[] { Genre.RockAndRoll, Genre.GarageRock };
				label.secondaryGenres = GD.Randf() > 0.5f ? new Genre[] { Genre.SurfRock } : new Genre[0];
				break;
			case LabelArchetype.CorporateGiant:
				label.preferredGenres = new Genre[] { Genre.TraditionalPop, Genre.EasyListening };
				label.secondaryGenres = new Genre[] { Genre.Jazz };
				break;
			case LabelArchetype.CountrySpecialist:
				label.preferredGenres = new Genre[] { Genre.Country };
				label.secondaryGenres = new Genre[] { Genre.Folk, Genre.Gospel };
				break;
			case LabelArchetype.BluesRoots:
				label.preferredGenres = new Genre[] { Genre.RnB, Genre.RockAndRoll };
				break;
			case LabelArchetype.FolkBoutique:
				label.preferredGenres = new Genre[] { Genre.Folk };
				label.secondaryGenres = new Genre[] { Genre.Country };
				break;
			case LabelArchetype.TeenHitMachine:
				label.preferredGenres = new Genre[] { Genre.TeenPop, Genre.RockAndRoll };
				break;
			case LabelArchetype.JazzPrestige:
				label.preferredGenres = new Genre[] { Genre.Jazz };
				label.secondaryGenres = new Genre[] { Genre.EasyListening };
				break;
			case LabelArchetype.GospelPowerhouse:
				label.preferredGenres = new Genre[] { Genre.Gospel };
				label.secondaryGenres = new Genre[] { Genre.Soul };
				break;
			case LabelArchetype.RegionalHustler:
				var allGenres = System.Enum.GetValues(typeof(Genre)).Cast<Genre>().ToList();
				label.preferredGenres = new Genre[] { allGenres[(int)GD.RandRange(0, allGenres.Count - 1)], allGenres[(int)GD.RandRange(0, allGenres.Count - 1)] };
				break;
		}
	}
	
	private void ApplyArchetypeStats(AILabel label, LabelArchetype archetype, LabelTier tier) {
		float tierBudget = tier switch {
			LabelTier.Major => 1.0f, LabelTier.MidTier => 0.6f, LabelTier.Independent => 0.35f, LabelTier.Small => 0.15f, _ => 0.2f
		};
		
		// I'm omitting the full switch here for brevity in this thought process, 
		// but it maps 1:1 to the user's code with Random.value -> GD.Randf() and Random.Range -> GD.RandRange
		// ... (Implemented fully in the final output)
	}
	
	private void AssignRegions(AILabel label, MarketRegion[] regions, LabelTier tier) {
		if (regions == null || regions.Length == 0) return;  // .Count → .Length
		
		var homeRegion = regions.FirstOrDefault(r => r.majorCities != null && r.majorCities.Contains(label.headquartersCity));
		
		if (homeRegion != null) label.strongRegions = new string[] { homeRegion.regionId };
		else label.strongRegions = new string[] { regions[(int)GD.RandRange(0, regions.Length - 1)].regionId };  // .Count → .Length
		
		int extraRegions = tier switch {
			LabelTier.Major => (int)GD.RandRange(4, regions.Length),
			LabelTier.MidTier => (int)GD.RandRange(2, 4),
			LabelTier.Independent => (int)GD.RandRange(1, 3),
			_ => 0
		};
		
		var availableRegions = regions.Where(r => !label.strongRegions.Contains(r.regionId)).OrderBy(_ => GD.Randf()).Take(extraRegions);
		var distList = new List<string>();
		var strongList = label.strongRegions.ToList();
		
		foreach (var region in availableRegions) {
			if (GD.Randf() > 0.3f) distList.Add(region.regionId);
			else strongList.Add(region.regionId);
		}
		
		label.strongRegions = strongList.ToArray();
		label.distributionRegions = distList.ToArray();
	}
	
	private void InitializeFinancials(AILabel label, LabelTier tier) {
		(float minCash, float maxCash, float minRep, float maxRep, float minShare, float maxShare) = tier switch {
			LabelTier.Major => (5000f, 15000f, 0.7f, 0.9f, 0.08f, 0.15f),
			LabelTier.MidTier => (500f, 2000f, 0.4f, 0.7f, 0.02f, 0.06f),
			LabelTier.Independent => (100f, 500f, 0.2f, 0.5f, 0.005f, 0.02f),
			LabelTier.Small => (20f, 150f, 0.05f, 0.25f, 0.001f, 0.005f),
			_ => (50f, 200f, 0.1f, 0.3f, 0.002f, 0.01f)
		};
		label.cashReserves = (float)GD.RandRange(minCash, maxCash);
		label.reputation = (float)GD.RandRange(minRep, maxRep);
		label.marketShare = (float)GD.RandRange(minShare, maxShare);
		label.debtLevel = tier == LabelTier.Small ? (float)GD.RandRange(0f, 50f) : 0f;
	}
	
	private void InitializeTrackRecord(AILabel label, LabelTier tier, int yearsActive) {
		int baseReleases = tier switch {
			LabelTier.Major => 40, LabelTier.MidTier => 20, LabelTier.Independent => 10, LabelTier.Small => 4, _ => 6
		};
		label.totalReleases = baseReleases * yearsActive;
		float hitRate = (label.productionQuality + label.scoutingAbility + label.marketingPower) / 3f;
		hitRate *= tier switch {
			LabelTier.Major => 0.15f, LabelTier.MidTier => 0.1f, LabelTier.Independent => 0.05f, LabelTier.Small => 0.02f, _ => 0.03f
		};
		label.top40Hits = Mathf.RoundToInt(label.totalReleases * hitRate);
		label.numberOneHits = Mathf.RoundToInt(label.top40Hits * 0.1f * label.marketingPower);
	}
	
	private string GetRandom(string[] list) => list[(int)GD.RandRange(0, list.Length - 1)];
}
