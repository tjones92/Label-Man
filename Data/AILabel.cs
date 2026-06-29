using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

[GlobalClass]
public partial class AILabel : Resource {
	
	[ExportGroup("Identity")]
	[Export] public string labelId;
	[Export] public string labelName;
	[Export] public string founderName;
	[Export] public string headquartersCity;
	[Export] public LabelArchetype archetype;
	[Export] public LabelTier tier;
	[Export] public int foundedYear;
	[Export] public bool isHistorical;
	
	[ExportGroup("Genres")]
	public Genre[] preferredGenres;
	public Genre[] secondaryGenres;
	
	[ExportGroup("Capabilities")]
	[Export] public float budgetLevel;
	[Export] public float scoutingAbility;
	[Export] public float productionQuality;
	[Export] public float marketingPower;
	[Export] public float distributionStrength;
	[Export] public float nationalReach;
	[Export] public float riskTolerance;
	[Export] public float artistLoyalty;
	[Export] public float payolaWillingness;
	
	[ExportGroup("Regional")]
	[Export] public string[] strongRegions;
	[Export] public string[] distributionRegions;
	
	[ExportGroup("Financials")]
	[Export] public float cashReserves;
	[Export] public float monthlyRevenue;
	[Export] public float monthlyExpenses;
	[Export] public float debtLevel;
	[Export] public float marketShare;
	
	[ExportGroup("Behavior")]
	[Export] public float releasesPerMonth;
	[Export] public LabelStatus status = LabelStatus.Stable;
	
	[ExportGroup("Track Record")]
	[Export] public int monthsActive;
	[Export] public int totalReleases;
	[Export] public int top40Hits;
	[Export] public int numberOneHits;
	[Export] public int consecutiveLossMonths;
	[Export] public float momentumScore;
	
	// Runtime Roster (Not exported, generated at runtime)
	public List<SimulatedArtist> roster = new List<SimulatedArtist>();
	public int maxRosterSize;
	public float reputation;
	
	public int CurrentRosterSize => roster?.Count ?? 0;
	public bool HasRosterSpace => roster == null || roster.Count < maxRosterSize;
	public float MonthlyProfit => monthlyRevenue - monthlyExpenses;
	public bool IsActive => status != LabelStatus.Bankrupt && status != LabelStatus.Defunct;
	
	public void InitializeRoster() {
		if (roster == null) roster = new List<SimulatedArtist>();
		
		if (maxRosterSize == 0) {
			maxRosterSize = tier switch {
				LabelTier.Major => (int)GD.RandRange(35, 60),
				LabelTier.MidTier => (int)GD.RandRange(18, 35),
				LabelTier.Independent => (int)GD.RandRange(8, 18),
				LabelTier.Small => (int)GD.RandRange(3, 10),
				LabelTier.Boutique => (int)GD.RandRange(5, 12),
				_ => 10
			};
		}
		
		if (reputation == 0) {
			reputation = tier switch {
				LabelTier.Major => (float)GD.RandRange(0.7, 0.95),
				LabelTier.MidTier => (float)GD.RandRange(0.4, 0.7),
				LabelTier.Independent => (float)GD.RandRange(0.2, 0.5),
				LabelTier.Small => (float)GD.RandRange(0.05, 0.25),
				LabelTier.Boutique => (float)GD.RandRange(0.3, 0.6),
				_ => 0.3f
			};
		}
	}
	
	public float CalculateHealthScore() {
		float score = 0f;
		float financialHealth = 0f;
		if (cashReserves > 0) {
			float monthsOfRunway = cashReserves / Mathf.Max(1f, GetMonthlyOverhead());
			financialHealth = Mathf.Clamp(monthsOfRunway / 12f, 0f, 1f);
		}
		if (MonthlyProfit > 0) financialHealth += 0.3f;
		if (consecutiveLossMonths == 0) financialHealth += 0.2f;
		score += Mathf.Clamp(financialHealth, 0f, 1f) * 0.4f;
		
		float successRate = 0f;
		if (totalReleases > 0) successRate = (float)top40Hits / totalReleases;
		score += Mathf.Clamp(successRate * 5f, 0f, 1f) * 0.3f;
		
		score += (reputation * 0.15f) + (momentumScore * 0.15f);
		return Mathf.Clamp(score, 0f, 1f);
	}
	
	public bool CanAffordToSign(float advanceCost) {
		float minReserve = GetMonthlyOverhead() * 2f;
		return cashReserves - advanceCost > minReserve;
	}
	
	public float CalculateAdvanceOffer(SimulatedArtist artist) {
		float baseAdvance = tier switch {
			LabelTier.Major => 5000f, LabelTier.MidTier => 2000f, LabelTier.Independent => 800f,
			LabelTier.Small => 300f, LabelTier.Boutique => 500f, _ => 500f
		};
		float talentMult = 0.5f + (artist.CalculateBaseQuality() * 1.5f);
		float reputationMult = 1f + (artist.reputation * 2f) + (artist.momentum * 1.5f);
		float competitionMult = tier == LabelTier.Major ? 1.5f : 1f;
		return baseAdvance * talentMult * reputationMult * competitionMult;
	}
	
	public float CalculateRoyaltyRate(SimulatedArtist artist) {
		float baseRate = tier switch {
			LabelTier.Major => 0.03f, LabelTier.MidTier => 0.05f, LabelTier.Independent => 0.08f,
			LabelTier.Small => 0.10f, LabelTier.Boutique => 0.07f, _ => 0.05f
		};
		float artistLeverage = artist.careerState switch {
			CareerState.Superstar => 0.08f, CareerState.Star => 0.05f, CareerState.Established => 0.03f,
			CareerState.Rising => 0.01f, _ => 0f
		};
		return Mathf.Clamp(baseRate + artistLeverage, 0.02f, 0.15f);
	}
	
	public int CalculateContractLength(SimulatedArtist artist) {
		return artist.careerState switch {
			CareerState.Superstar => (int)GD.RandRange(1, 3),
			CareerState.Star => (int)GD.RandRange(2, 4),
			CareerState.Established => (int)GD.RandRange(3, 5),
			_ => (int)GD.RandRange(4, 7)
		};
	}
	
	public void SignArtist(SimulatedArtist artist, int currentYear) {
		if (roster == null) roster = new List<SimulatedArtist>();
		float advance = CalculateAdvanceOffer(artist);
		
		artist.labelId = labelId;
		artist.signedYear = currentYear;
		artist.careerState = artist.careerState == CareerState.Unsigned ? CareerState.NewSigning : artist.careerState;
		artist.royaltyRate = CalculateRoyaltyRate(artist);
		artist.unrecoupedAdvance = advance;
		artist.contractLength = CalculateContractLength(artist);
		artist.contractExpiresYear = currentYear + artist.contractLength;
		
		cashReserves -= advance;
		roster.Add(artist);
		artist.careerEvents.Add($"{currentYear}: Signed to {labelName} (${advance:N0} advance, {artist.contractLength}yr)");
	}
	
	public void DropArtist(SimulatedArtist artist, int currentYear, string reason = "dropped") {
		roster?.Remove(artist);
		artist.labelId = null;
		artist.careerState = CareerState.Dropped;
		artist.careerEvents.Add($"{currentYear}: Released from {labelName} ({reason})");
	}
	
	public SimulatedArtist GetArtistForRelease(int currentYear) {
		if (roster == null || roster.Count == 0) return null;
		var candidates = new List<(SimulatedArtist artist, float priority)>();
		
		foreach (var artist in roster) {
			if (!artist.isActive) continue;
			int minWeeks = GetMinimumReleaseCooldown(artist);
			if (artist.weeksSinceLastRelease < minWeeks) continue;
			float priority = CalculateReleasePriority(artist, currentYear);
			if (priority > 0) candidates.Add((artist, priority));
		}
		
		if (candidates.Count == 0) return null;
		float totalWeight = candidates.Sum(c => c.priority);
		float roll = (float)GD.RandRange(0f, totalWeight);
		float cumulative = 0f;
		
		foreach (var (artist, priority) in candidates.OrderByDescending(c => c.priority)) {
			cumulative += priority;
			if (roll <= cumulative) return artist;
		}
		return candidates[0].artist;
	}
	
	private int GetMinimumReleaseCooldown(SimulatedArtist artist) {
		int baseCooldown = 12;
		if (artist.careerState == CareerState.Superstar && tier == LabelTier.Major) baseCooldown = 8;
		else if (artist.careerState >= CareerState.Star) baseCooldown = 10;
		return baseCooldown;
	}
	
	private float CalculateReleasePriority(SimulatedArtist artist, int currentYear) {
		float priority = 0f;
		priority += artist.GetCareerPriority();
		priority += artist.momentum * 0.4f;
		float cooldownBonus = Mathf.Clamp((artist.weeksSinceLastRelease - 12) / 20f, 0f, 1f) * 0.2f;
		priority += cooldownBonus;
		
		if (ChartManager.Instance != null) {
			float genreHeat = ChartManager.Instance.GetEffectiveGenreAcceptance(artist.primaryGenre);
			priority += (genreHeat - 0.5f) * 0.2f;
		}
		priority += (float)GD.RandRange(0f, 0.15f);
		return Mathf.Max(0f, priority);
	}
	
	public bool ShouldScoutNewArtist() {
		if (!HasRosterSpace) return false;
		float estimatedAdvance = tier switch {
			LabelTier.Major => 5000f, LabelTier.MidTier => 2000f, _ => 800f
		};
		if (!CanAffordToSign(estimatedAdvance)) return false;
		
		float rosterFullness = (float)CurrentRosterSize / maxRosterSize;
		float scoutChance = (1f - rosterFullness) * scoutingAbility;
		float recentHits = roster?.Count(a => a.consecutiveHits > 0) ?? 0;
		if (recentHits > 0) scoutChance *= 0.7f;
		
		float decliningArtists = roster?.Count(a => a.careerState == CareerState.Declining) ?? 0;
		if (decliningArtists > CurrentRosterSize * 0.3f) scoutChance *= 1.3f;
		
		return (float)GD.RandRange(0f, 1f) < scoutChance * 0.15f;
	}
	
	public SimulatedArtist EvaluateForSigning(List<SimulatedArtist> candidates) {
		if (candidates == null || candidates.Count == 0) return null;
		var scored = new List<(SimulatedArtist artist, float score)>();
		
		foreach (var artist in candidates) {
			float score = 0f;
			score += artist.CalculateBaseQuality() * scoutingAbility * 2f;
			
			if (preferredGenres != null && preferredGenres.Contains(artist.primaryGenre)) score += 0.3f;
			else if (secondaryGenres != null && secondaryGenres.Contains(artist.primaryGenre)) score += 0.15f;
			else score -= 0.2f;
			
			score += artist.momentum * 0.5f;
			score += artist.reputation * 0.3f;
			if (artist.reputation < 0.1f) score *= 0.5f + (riskTolerance * 0.5f);
			
			float estimatedCost = CalculateAdvanceOffer(artist);
			float costRatio = estimatedCost / Mathf.Max(1f, cashReserves);
			if (costRatio > 0.3f) score *= 0.7f;
			scored.Add((artist, score));
		}
		
		var best = scored.OrderByDescending(s => s.score).FirstOrDefault();
		if (best.score < 0.3f) return null;
		return best.artist;
	}
	
	public bool ShouldDropArtist(SimulatedArtist artist) {
		if (artist.careerState == CareerState.Superstar) return false;
		if (artist.consecutiveFlops >= 3 && artist.careerState <= CareerState.Rising) return (float)GD.RandRange(0f, 1f) < 0.6f;
		if (artist.consecutiveFlops >= 4 && artist.careerState == CareerState.Established) return (float)GD.RandRange(0f, 1f) < 0.4f;
		if (artist.careerState == CareerState.Declining && artist.consecutiveFlops >= 2) return (float)GD.RandRange(0f, 1f) < 0.5f;
		if (artistLoyalty > 0.7f) return false;
		return false;
	}
	
	public float GetMonthlyOverhead() {
		float baseOverhead = tier switch {
			LabelTier.Major => 3000f, LabelTier.MidTier => 1200f, LabelTier.Independent => 400f,
			LabelTier.Small => 150f, LabelTier.Boutique => 250f, _ => 300f
		};
		float perArtist = tier switch {
			LabelTier.Major => 200f, LabelTier.MidTier => 80f, _ => 30f
		};
		return baseOverhead + (CurrentRosterSize * perArtist);
	}
	
	public float GetProductionCost() {
		return tier switch {
			LabelTier.Major => 4000f, LabelTier.MidTier => 2000f, LabelTier.Independent => 800f,
			LabelTier.Small => 350f, LabelTier.Boutique => 600f, _ => 500f
		};
	}
	
	public float GetMarketingBudget(SimulatedArtist artist) {
		float baseBudget = tier switch {
			LabelTier.Major => 3000f, LabelTier.MidTier => 1200f, LabelTier.Independent => 400f,
			LabelTier.Small => 150f, LabelTier.Boutique => 300f, _ => 300f
		};
		float artistMult = artist.careerState switch {
			CareerState.Superstar => 2.5f, CareerState.Star => 2.0f, CareerState.Established => 1.5f,
			CareerState.Rising => 1.2f, CareerState.NewSigning => 0.8f, _ => 0.5f
		};
		return baseBudget * artistMult * marketingPower;
	}
}
