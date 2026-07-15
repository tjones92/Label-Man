using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

[GlobalClass]
public partial class AILabel : Resource {
	public const float CapabilityOwnedReachWeight = 0.35f;
	public const float CapabilityNationalReachWeight = 0.25f;
	public const float CapabilityRunwayWeight = 0.20f;
	public const float CapabilityMarketingWeight = 0.20f;
	
	[ExportGroup("Identity")]
	[Export] public string labelId;
	[Export] public string labelName;
	[Export] public string founderName;
	[Export] public string headquartersCity;
	[Export] public LabelArchetype archetype;
	[Export] public LabelTier tier;
	[Export] public int foundedYear;
	[Export] public bool isHistorical;
	[Export] public bool isPlayerOwned;
	
	[ExportGroup("Genres")]
	public Genre[] preferredGenres;
	public Genre[] secondaryGenres;
	
	[ExportGroup("Capabilities")]
	[Export] public float budgetLevel;
	[Export] public float scoutingAbility;
	[Export] public float productionQuality;
	[Export] public float marketingPower;
	[Export] public float ownedReach;
	[Export] public float nationalReach;
	[Export] public float riskTolerance;
	[Export] public float artistLoyalty;
	[Export] public float payolaWillingness;
	
	[ExportGroup("Regional")]
	[Export] public string homeRegion;
	[Export] public string homeCityId;
	[Export] public string homeCityAssignmentSource;
	[Export] public string[] strongRegions;
	[Export] public string[] distributionRegions;
	
	[ExportGroup("Financials")]
	[Export] public float cashReserves;
	[Export] public float monthlyRevenue;
	[Export] public float monthlyExpenses;
	public float lastMonthlyProfit;
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
	[Export] public int sustainedCapabilityQuarters;
	[Export] public int sustainedLowCapabilityQuarters;
	
	// Runtime Roster (Not exported, generated at runtime)
	public List<SimulatedArtist> roster = new List<SimulatedArtist>();
	public int maxRosterSize;
	public float reputation;
	public DistributionDeal activeDeal;

	// Runtime finance telemetry (reset and populated by CompetitorManager each week)
	public float weeklyGrossRevenue;
	public float weeklyCogs;
	public float weeklyDistributionSkim;
	public float weeklyArtistRoyalty;
	public float weeklyNetRevenue;
	public float weeklyDistributionIncome;
	public Dictionary<ReleaseFormat, FormatRevenueMemory> revenueMemory = new();

	public FormatRevenueMemory GetOrCreateRevenueMemory(ReleaseFormat format) {
		if (!revenueMemory.TryGetValue(format, out FormatRevenueMemory memory)) {
			memory = new FormatRevenueMemory();
			revenueMemory[format] = memory;
		}
		return memory;
	}

	// Backward-compatible combined reach. Existing generators assign owned reach
	// through this property; fulfillment code automatically sees borrowed reach.
	public float distributionStrength {
		get => Mathf.Clamp(ownedReach + borrowedReach, 0f, 1f);
		set => ownedReach = Mathf.Clamp(value, 0f, 1f);
	}
	public float borrowedReach => Mathf.Clamp(activeDeal?.reachGranted ?? 0f, 0f, 1f);

	public bool HasDistributionInRegion(string regionId) =>
		!string.IsNullOrEmpty(regionId) &&
		((distributionRegions?.Contains(regionId) ?? false) ||
		(activeDeal?.grantedRegions?.Contains(regionId) ?? false));
	
	public int CurrentRosterSize => roster?.Count ?? 0;
	public bool HasRosterSpace => roster == null || roster.Count < maxRosterSize;
	[Export] public int operatingRosterTarget;
	public int OperatingRosterTarget => Mathf.Clamp(operatingRosterTarget > 0 ? operatingRosterTarget : maxRosterSize, 1, Mathf.Max(1, maxRosterSize));
	public bool HasOperatingRosterSpace => roster == null || roster.Count < OperatingRosterTarget;
	public string operatingRosterTargetSource = "Unset";
	public void SetOperatingRosterTargetFromCurrent() {
		operatingRosterTarget = Mathf.Clamp(Mathf.Max(1, CurrentRosterSize), 1, Mathf.Max(1, maxRosterSize));
		operatingRosterTargetSource = CurrentRosterSize > 0 ? "PopulatedLaunchRoster" : "OneArtistBootstrap";
	}
	public float MonthlyProfit => monthlyRevenue - monthlyExpenses;
	public bool IsActive => status != LabelStatus.Bankrupt && status != LabelStatus.Defunct && status != LabelStatus.Acquired;
	public float DistributionDependency => borrowedReach / (borrowedReach + ownedReach + 0.01f);

	public float CalculateCapabilityScore() {
		float annualOverhead = Mathf.Max(1f, GetMonthlyOverhead() * 12f);
		float runwayNorm = Mathf.Clamp(cashReserves / annualOverhead, 0f, 1f);
		return Mathf.Clamp(
			(CapabilityOwnedReachWeight * ownedReach) +
			(CapabilityNationalReachWeight * nationalReach) +
			(CapabilityRunwayWeight * runwayNorm) +
			(CapabilityMarketingWeight * marketingPower), 0f, 1f);
	}
	
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
		if (lastMonthlyProfit > 0) financialHealth += 0.3f;
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
	
	public float SignArtist(SimulatedArtist artist, int currentYear) {
		if (roster == null) roster = new List<SimulatedArtist>();
		float advance = CalculateAdvanceOffer(artist);
		
		artist.labelId = labelId;
		artist.isPlayerOwned = isPlayerOwned;
		artist.signedYear = currentYear;
		// ArtistManager owns the atomic career-state transition after it captures
		// the pre-contract history needed to distinguish first contracts from
		// experienced free-agent returns.
		artist.royaltyRate = CalculateRoyaltyRate(artist);
		artist.unrecoupedAdvance = advance;
		artist.contractLength = CalculateContractLength(artist);
		artist.contractExpiresYear = currentYear + artist.contractLength;
		
		roster.Add(artist);
		artist.careerEvents.Add($"{currentYear}: Signed to {labelName} (${advance:N0} advance, {artist.contractLength}yr)");
		return advance;
	}
	
	public void DropArtist(SimulatedArtist artist, int currentYear, string reason = "dropped") {
		roster?.Remove(artist);
		artist.labelId = null;
		artist.isPlayerOwned = false;
		artist.careerState = CareerState.Dropped;
		artist.careerEvents.Add($"{currentYear}: Released from {labelName} ({reason})");
	}

	public LabelPublicProfile GetPublicProfile() {
		return new LabelPublicProfile {
			labelId = labelId, labelName = labelName, founderName = founderName,
			headquartersCity = headquartersCity, archetype = archetype, tier = tier,
			foundedYear = foundedYear, preferredGenres = preferredGenres ?? Array.Empty<Genre>(),
			totalReleases = totalReleases, top40Hits = top40Hits, numberOneHits = numberOneHits,
			rosterArtistNames = roster?.Where(a => a != null).Select(a => a.stageName).ToList() ?? new List<string>(),
			statusImpression = GetStatusImpression(),
			descriptionBlurb = JournalisticDescriptor.DescribeLabel(this)
		};
	}

	private string GetStatusImpression() => status switch {
		LabelStatus.Rising => "Industry talk says they're on the way up.",
		LabelStatus.Stable => "Word is business remains steady.",
		LabelStatus.Struggling => "The trade press hears they're struggling.",
		LabelStatus.Dying => "Insiders wonder how long they can keep the doors open.",
		LabelStatus.Bankrupt or LabelStatus.Defunct => "The operation has gone quiet.",
		LabelStatus.Acquired => "They now operate under new ownership.",
		_ => "Little is being said about their present condition."
	};
	
	public SimulatedArtist GetArtistForRelease(int currentYear) {
		if (roster == null || roster.Count == 0) return null;
		var candidates = new List<(SimulatedArtist artist, float priority)>();
		
		foreach (var artist in roster) {
			if (!IsEligibleForRelease(artist, currentYear)) continue;
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

	public int CountArtistsEligibleForRelease(int currentYear) => roster?.Count(artist => IsEligibleForRelease(artist, currentYear)) ?? 0;

	private bool IsEligibleForRelease(SimulatedArtist artist, int currentYear) {
		bool liveGenreMarket = GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true;
		if (liveGenreMarket
			? !GenreSupplyService.IsEligibleExistingArtistForEnabledRelease(artist)
			: !GenreSupplyService.IsEligibleExistingArtistForRelease(artist)) return false;
		// Lifecycle only governs new identity creation. Existing artists retain release capacity.
		return artist.weeksSinceLastRelease >= GetMinimumReleaseCooldown(artist);
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
	
	public readonly struct ScoutingGateEvaluation {
		public readonly int RosterSize;
		public readonly int MaxRosterSize;
		public readonly float EstimatedAdvance;
		public readonly float RosterFullness;
		public readonly bool HasRecentHit;
		public readonly float RecentHitFactor;
		public readonly int DecliningArtistCount;
		public readonly float DecliningFactor;
		public readonly float ComputedScoutProbability;
		public readonly float? RandomRoll;
		public readonly bool ScoutingGatePassed;
		public readonly string FailureReason;

		public ScoutingGateEvaluation(int rosterSize, int maxRosterSize, float estimatedAdvance, float rosterFullness,
			bool hasRecentHit, float recentHitFactor, int decliningArtistCount, float decliningFactor,
			float computedScoutProbability, float? randomRoll, bool scoutingGatePassed, string failureReason) {
			RosterSize = rosterSize;
			MaxRosterSize = maxRosterSize;
			EstimatedAdvance = estimatedAdvance;
			RosterFullness = rosterFullness;
			HasRecentHit = hasRecentHit;
			RecentHitFactor = recentHitFactor;
			DecliningArtistCount = decliningArtistCount;
			DecliningFactor = decliningFactor;
			ComputedScoutProbability = computedScoutProbability;
			RandomRoll = randomRoll;
			ScoutingGatePassed = scoutingGatePassed;
			FailureReason = failureReason;
		}
	}

	public readonly struct SigningEvaluation {
		public readonly SimulatedArtist BestCandidate;
		public readonly float? BestCandidateScore;
		public readonly SimulatedArtist HighestScoredCandidate;
		public readonly IReadOnlyList<SigningCandidateScore> CandidateScores;
		public SigningEvaluation(SimulatedArtist bestCandidate, float? bestCandidateScore,
			SimulatedArtist highestScoredCandidate = null, IReadOnlyList<SigningCandidateScore> candidateScores = null) {
			BestCandidate = bestCandidate;
			BestCandidateScore = bestCandidateScore;
			HighestScoredCandidate = highestScoredCandidate;
			CandidateScores = candidateScores ?? System.Array.Empty<SigningCandidateScore>();
		}
	}

	/// <summary>One candidate's unchanged signing score, retained for deterministic policy selection.</summary>
	public readonly struct SigningCandidateScore {
		public readonly SimulatedArtist Artist;
		public readonly float Score;
		public SigningCandidateScore(SimulatedArtist artist, float score) {
			Artist = artist;
			Score = score;
		}
	}

	public bool ShouldScoutNewArtist() => EvaluateScoutingGate().ScoutingGatePassed;

	/// <summary>
	/// Captures the one existing live scouting roll. Callers may supply a roll source
	/// for probes; production passes none and consumes precisely the historical draw.
	/// </summary>
	public ScoutingGateEvaluation EvaluateScoutingGate(System.Func<float> rollSource = null, float minimumProbability = 0f,
		bool useOperatingRosterTarget = false) =>
		EvaluateScoutingGateCore(rollSource, true, minimumProbability, useOperatingRosterTarget);

	/// <summary>Observational snapshot for a chart capture with no scouting tick; never consumes RNG.</summary>
	public ScoutingGateEvaluation PreviewScoutingGate(bool useOperatingRosterTarget = false) =>
		EvaluateScoutingGateCore(null, false, 0f, useOperatingRosterTarget);

	private ScoutingGateEvaluation EvaluateScoutingGateCore(System.Func<float> rollSource, bool consumeRoll, float minimumProbability,
		bool useOperatingRosterTarget) {
		int rosterSize = CurrentRosterSize;
		int rosterCapacity = useOperatingRosterTarget ? OperatingRosterTarget : maxRosterSize;
		float estimatedAdvance = tier switch {
			LabelTier.Major => 5000f, LabelTier.MidTier => 2000f, _ => 800f
		};
		if (rosterSize >= rosterCapacity) return new ScoutingGateEvaluation(rosterSize, rosterCapacity, estimatedAdvance, 1f,
			false, 1f, 0, 1f, 0f, null, false, "RosterFull");
		if (!CanAffordToSign(estimatedAdvance)) return new ScoutingGateEvaluation(rosterSize, rosterCapacity, estimatedAdvance,
			rosterCapacity == 0 ? 1f : (float)rosterSize / rosterCapacity, false, 1f, 0, 1f, 0f, null, false, "EstimatedAdvanceUnaffordable");

		float rosterFullness = rosterCapacity == 0 ? 1f : (float)rosterSize / rosterCapacity;
		float scoutChance = (1f - rosterFullness) * scoutingAbility;
		bool hasRecentHit = (roster?.Count(a => a.consecutiveHits > 0) ?? 0) > 0;
		float recentHitFactor = hasRecentHit ? 0.7f : 1f;
		scoutChance *= recentHitFactor;
		int decliningArtists = roster?.Count(a => a.careerState == CareerState.Declining) ?? 0;
		float decliningFactor = decliningArtists > rosterSize * 0.3f ? 1.3f : 1f;
		scoutChance *= decliningFactor;
		float scoutingMultiplier = GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true ? 0.20f : 0.15f;
		float probability = Mathf.Max(scoutChance * scoutingMultiplier, minimumProbability);
		if (!consumeRoll) return new ScoutingGateEvaluation(rosterSize, rosterCapacity, estimatedAdvance, rosterFullness, hasRecentHit,
			recentHitFactor, decliningArtists, decliningFactor, probability, null, false, null);
		float roll = rollSource?.Invoke() ?? (float)GD.RandRange(0f, 1f);
		bool passed = roll < probability;
		return new ScoutingGateEvaluation(rosterSize, rosterCapacity, estimatedAdvance, rosterFullness, hasRecentHit,
			recentHitFactor, decliningArtists, decliningFactor, probability, roll, passed,
			passed ? null : "ScoutingRandomGate");
	}
	
	public SimulatedArtist EvaluateForSigning(List<SimulatedArtist> candidates) => EvaluateSigning(candidates).BestCandidate;

	public SigningEvaluation EvaluateSigning(List<SimulatedArtist> candidates) {
		if (candidates == null || candidates.Count == 0) return new SigningEvaluation(null, null);
		var scored = new List<SigningCandidateScore>();
		
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
			scored.Add(new SigningCandidateScore(artist, score));
		}
		
		SigningCandidateScore best = scored.OrderByDescending(candidate => candidate.Score).First();
		return new SigningEvaluation(best.Score < 0.3f ? null : best.Artist, best.Score, best.Artist, scored);
	}

	/// <summary>Evaluates an unsigned prospect from potential, not career evidence.</summary>
	public SigningEvaluation EvaluateFreshPotential(List<SimulatedArtist> candidates) {
		if (candidates == null || candidates.Count == 0) return new SigningEvaluation(null, null);
		var scored = new List<SigningCandidateScore>();
		foreach (var artist in candidates) {
			float score = artist.CalculateBaseQuality() * scoutingAbility * 2f;
			if (preferredGenres != null && preferredGenres.Contains(artist.primaryGenre)) score += 0.3f;
			else if (secondaryGenres != null && secondaryGenres.Contains(artist.primaryGenre)) score += 0.15f;
			else score -= 0.2f;
			float estimatedCost = CalculateAdvanceOffer(artist);
			if (estimatedCost / Mathf.Max(1f, cashReserves) > 0.3f) score *= 0.7f;
			scored.Add(new SigningCandidateScore(artist, score));
		}
		SigningCandidateScore best = scored.OrderByDescending(candidate => candidate.Score).First();
		return new SigningEvaluation(best.Score < 0.3f ? null : best.Artist, best.Score, best.Artist, scored);
	}
	
	public bool ShouldDropArtist(SimulatedArtist artist) {
		if (artist.careerState == CareerState.Superstar) return false;
		// Contract probation excludes stale history only while it is unresolved.
		// Once a current-contract Top 40 clears probation, ordinary career-state
		// review is again authoritative.
		if (ArtistPopulationLifecycle.Enabled && artist.IsContractPerformanceProbationPending())
			return artist.ShouldDepartForCurrentContractPerformance();
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

public sealed class FormatRevenueMemory {
	public float emaNetPerRelease;
	public int releasesObserved;
}
