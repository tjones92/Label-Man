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

	/// <summary>
	/// A runtime-founded label's name, city, regions and opening finances. With the artist population lifecycle
	/// on (every current run), <see cref="RuntimeLabelProfileFactory"/> then assigns the archetype, genres,
	/// operating profile, founding year and track record from its own stable PRNG, so drawing them here was
	/// overwritten on every birth. <paramref name="legacyIdentity"/> keeps those draws, in their original order,
	/// for the lifecycle-off compatibility path only, which has no other source for them.
	/// </summary>
	public AILabel GenerateSingleLabel(MarketRegion[] regions, int year, LabelTier tier, bool legacyIdentity) {
		var label = new AILabel();
		label.labelName = GenerateUniqueName();
		label.labelId = "gen_" + label.labelName.Replace(" ", "").ToLower() + "_" + (int)GameRandom.RandRange(1000, 9999);
		label.founderName = GenerateFounderName();
		label.headquartersCity = cities[(int)GameRandom.RandRange(0, cities.Length - 1)];
		if (legacyIdentity) label.foundedYear = (int)GameRandom.RandRange(year - 5, year + 1);
		label.isHistorical = false;
		label.tier = tier;
		label.status = GameRandom.Randf() > 0.3f ? LabelStatus.Stable : LabelStatus.Rising;
		if (legacyIdentity) {
			label.archetype = GenerateArchetype(year, tier);
			AssignGenresFromArchetype(label);
		}
		AssignRegions(label, regions, tier);
		InitializeFinancials(label, tier);

		if (legacyIdentity) {
			int yearsActive = Mathf.Max(1, year - label.foundedYear);
			label.monthsActive = yearsActive * 12;
			InitializeTrackRecord(label, tier, yearsActive);
		}

		usedNames.Add(label.labelName);
		return label;
	}
	
	private string GenerateUniqueName() {
		for (int attempts = 0; attempts < 100; attempts++) {
			string name = GenerateName();
			if (!usedNames.Contains(name)) return name;
		}
		return GenerateName() + " " + (int)GameRandom.RandRange(1, 100);
	}
	
	private string GenerateName() {
		int pattern = (int)GameRandom.RandRange(0, 6);
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
		float roll = GameRandom.Randf() * total;
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
				label.secondaryGenres = GameRandom.Randf() > 0.5f ? new Genre[] { Genre.SurfRock } : new Genre[0];
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
				var allGenres = GenreDomains.Current;
				label.preferredGenres = new Genre[] { allGenres[(int)GameRandom.RandRange(0, allGenres.Count - 1)], allGenres[(int)GameRandom.RandRange(0, allGenres.Count - 1)] };
				break;
		}
	}
	
	private void AssignRegions(AILabel label, MarketRegion[] regions, LabelTier tier) {
		if (regions == null || regions.Length == 0) return;  // .Count → .Length
		
		var homeRegion = regions.FirstOrDefault(r => r.majorCities != null && r.majorCities.Contains(label.headquartersCity));
		
		if (homeRegion != null) label.strongRegions = new string[] { homeRegion.regionId };
		else label.strongRegions = new string[] { regions[(int)GameRandom.RandRange(0, regions.Length - 1)].regionId };  // .Count → .Length
		
		int extraRegions = tier switch {
			LabelTier.Major => (int)GameRandom.RandRange(4, regions.Length),
			LabelTier.MidTier => (int)GameRandom.RandRange(2, 4),
			LabelTier.Independent => (int)GameRandom.RandRange(1, 3),
			_ => 0
		};
		
		var availableRegions = regions.Where(r => !label.strongRegions.Contains(r.regionId)).OrderBy(_ => GameRandom.Randf()).Take(extraRegions);
		var distList = new List<string>();
		var strongList = label.strongRegions.ToList();
		
		foreach (var region in availableRegions) {
			if (GameRandom.Randf() > 0.3f) distList.Add(region.regionId);
			else strongList.Add(region.regionId);
		}
		
		label.strongRegions = strongList.ToArray();
		label.distributionRegions = distList.ToArray();
	}
	
	// Births are only ever Small or Independent (LabelLifecycleManager.SelectRuntimeFoundingTier). Opening cash
	// comes from the table the 1960 world uses, so the two cannot drift apart again.
	private void InitializeFinancials(AILabel label, LabelTier tier) {
		if (tier != LabelTier.Small && tier != LabelTier.Independent)
			throw new System.InvalidOperationException($"Runtime-founded labels begin at Small or Independent tier, not {tier}.");
		(float minRep, float maxRep, float minShare, float maxShare) = tier == LabelTier.Small
			? (0.05f, 0.25f, 0.001f, 0.005f)
			: (0.2f, 0.5f, 0.005f, 0.02f);
		label.cashReserves = LabelStartingFinances.DrawCash(tier);
		label.reputation = (float)GameRandom.RandRange(minRep, maxRep);
		label.marketShare = (float)GameRandom.RandRange(minShare, maxShare);
		label.debtLevel = tier == LabelTier.Small ? (float)GameRandom.RandRange(0f, 50f) : 0f;
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
	
	private string GetRandom(string[] list) => list[(int)GameRandom.RandRange(0, list.Length - 1)];
}
