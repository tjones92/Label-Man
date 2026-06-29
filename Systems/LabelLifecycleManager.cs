using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class LabelLifecycleManager : Node {
	public static LabelLifecycleManager Instance { get; private set; }
	
	[ExportGroup("Active Labels")]
	private List<AILabel> activeLabels = new List<AILabel>();
	private List<AILabel> defunctLabels = new List<AILabel>();
	
	[ExportGroup("Settings")]
	[Export] private float monthlyBirthChance = 0.15f;
	[Export] private float monthlyDeathCheckFrequency = 1f;
	
	[ExportGroup("References")]
	// FIX: Changed List to Array for Godot Export compatibility
	[Export] private MarketRegion[] regions;
	
	private LabelGenerator generator = new LabelGenerator();
	
	public int TotalActiveLabels => activeLabels.Count(l => l.IsActive);
	public int MajorLabels => activeLabels.Count(l => l.tier == LabelTier.Major && l.IsActive);
	public int DefunctThisYear { get; private set; }
	public int FoundedThisYear { get; private set; }
	
	private int currentYear = 1960;
	private int currentMonth = 1;
	
	public event Action<AILabel, string> OnLabelDefunct;
	public event Action<AILabel> OnLabelFounded;
	public event Action<AILabel, LabelTier, LabelTier> OnLabelPromoted;
	public event Action<AILabel, LabelTier, LabelTier> OnLabelDemoted;
	
	public override void _EnterTree() {
		if (Instance != null && Instance != this) { QueueFree(); return; }
		Instance = this;
	}
	
	private int GetTargetLabelCount(int year) {
		if (year <= 1960) return 100;
		if (year <= 1962) return 120;
		if (year <= 1964) return 140;
		if (year <= 1966) return 160;
		if (year <= 1968) return 150;
		return 145;
	}
	
	public void InitializeLabels(int startYear = 1960) {
		currentYear = startYear;
		currentMonth = 1;
		DefunctThisYear = 0;
		FoundedThisYear = 0;
		
		activeLabels.AddRange(generator.GenerateHistoricalMajors(regions));
		
		int target = GetTargetLabelCount(startYear);
		int remaining = target - activeLabels.Count;
		
		activeLabels.AddRange(generator.GenerateLabels(regions, remaining, startYear));
		GD.Print($"[LabelManager] Initialized {activeLabels.Count} labels for {startYear}");
	}
	
	public void ProcessMonth(int year, int month) {
		currentYear = year;
		currentMonth = month;
		
		if (month == 1) { DefunctThisYear = 0; FoundedThisYear = 0; }
		
		foreach (var label in activeLabels.Where(l => l.IsActive).ToList()) {
			UpdateLabelHealth(label);
			CheckForDeath(label);
		}
		
		CheckForBirths();
		
		if (month % 3 == 0) ProcessQuarterlyChanges();
	}
	
	private void UpdateLabelHealth(AILabel label) {
		label.monthsActive++;
		label.monthlyRevenue = CalculateMonthlyRevenue(label);
		label.monthlyExpenses = CalculateMonthlyExpenses(label);
		label.cashReserves += label.MonthlyProfit;
		
		if (label.MonthlyProfit < 0) label.consecutiveLossMonths++;
		else label.consecutiveLossMonths = 0;
		
		label.momentumScore = Mathf.Lerp(label.momentumScore, CalculateMomentum(label), 0.3f);
		UpdateLabelStatus(label);
	}
	
	private float CalculateMonthlyRevenue(AILabel label) {
		float tierMultiplier = label.tier switch {
			LabelTier.Major => 500f, LabelTier.MidTier => 150f, LabelTier.Independent => 50f,
			LabelTier.Small => 15f, _ => 20f
		};
		float marketFactor = 1f + (label.marketShare * 20f);
		float hitBonus = label.top40Hits * 5f + label.numberOneHits * 25f;
		float rosterFactor = Mathf.Sqrt(label.CurrentRosterSize + 1);
		return tierMultiplier * marketFactor * rosterFactor + hitBonus;
	}
	
	private float CalculateMonthlyExpenses(AILabel label) {
		float baseCost = label.tier switch {
			LabelTier.Major => 400f, LabelTier.MidTier => 100f, LabelTier.Independent => 35f,
			LabelTier.Small => 12f, _ => 15f
		};
		float rosterCost = label.CurrentRosterSize * 2f;
		float productionCost = label.releasesPerMonth * label.productionQuality * 10f;
		float marketingCost = label.marketingPower * 20f;
		return baseCost + rosterCost + productionCost + marketingCost;
	}
	
	private float CalculateMomentum(AILabel label) => Mathf.Clamp(label.reputation + (label.top40Hits * 0.05f), 0f, 1f);
	
	private void UpdateLabelStatus(AILabel label) {
		float health = label.CalculateHealthScore();
		if (health > 0.7f && label.MonthlyProfit > 0) label.status = LabelStatus.Rising;
		else if (health > 0.4f) label.status = LabelStatus.Stable;
		else if (health > 0.2f || label.consecutiveLossMonths < 6) label.status = LabelStatus.Struggling;
		else label.status = LabelStatus.Dying;
	}
	
	private void CheckForDeath(AILabel label) {
		if (label.status != LabelStatus.Dying) return;
		if (label.tier == LabelTier.Major) return;
		
		float deathChance = 0f;
		if (label.cashReserves < -100f) deathChance = 0.8f;
		else if (label.consecutiveLossMonths > 12) deathChance = 0.5f;
		else if (label.consecutiveLossMonths > 6 && label.cashReserves < 50f) deathChance = 0.2f;
		
		if (label.tier == LabelTier.Small) deathChance *= 1.5f;
		if (GD.Randf() < deathChance) KillLabel(label, "Bankruptcy");
	}
	
	private void KillLabel(AILabel label, string reason) {
		label.status = LabelStatus.Defunct;
		defunctLabels.Add(label);
		DefunctThisYear++;
		GD.Print($"[LabelManager] {label.labelName} has closed. Reason: {reason}. Operated for {label.monthsActive} months.");
		OnLabelDefunct?.Invoke(label, reason);
	}
	
	private void CheckForBirths() {
		int currentCount = TotalActiveLabels;
		int targetCount = GetTargetLabelCount(currentYear);
		float spawnModifier = (targetCount - currentCount) / 20f;
		float adjustedChance = monthlyBirthChance + spawnModifier;
		
		if (currentYear >= 1964 && currentYear <= 1966) adjustedChance *= 1.3f;
		if (GD.Randf() < adjustedChance) SpawnNewLabel();
		if (GD.Randf() < adjustedChance * 0.3f) SpawnNewLabel();
	}
	
	private void SpawnNewLabel() {
		LabelTier tier = GD.Randf() < 0.7f ? LabelTier.Small : LabelTier.Independent;
		AILabel newLabel = generator.GenerateSingleLabel(regions, currentYear, tier);
		activeLabels.Add(newLabel);
		FoundedThisYear++;
		GD.Print($"[LabelManager] New label founded: {newLabel.labelName} ({newLabel.archetype})");
		OnLabelFounded?.Invoke(newLabel);
	}
	
	private void ProcessQuarterlyChanges() {
		foreach (var label in activeLabels.Where(l => l.IsActive)) {
			CheckForTierChange(label);
			DriftAttributes(label);
		}
	}
	
	private void CheckForTierChange(AILabel label) {
		float health = label.CalculateHealthScore();
		if (health > 0.8f && label.monthsActive > 24) {
			if (label.tier == LabelTier.Small && label.top40Hits >= 2) PromoteLabel(label, LabelTier.Independent);
			else if (label.tier == LabelTier.Independent && label.top40Hits >= 10) PromoteLabel(label, LabelTier.MidTier);
		}
		if (health < 0.3f && label.consecutiveLossMonths > 12) {
			if (label.tier == LabelTier.MidTier) DemoteLabel(label, LabelTier.Independent);
			else if (label.tier == LabelTier.Independent) DemoteLabel(label, LabelTier.Small);
		}
	}
	
	private void PromoteLabel(AILabel label, LabelTier newTier) {
		var oldTier = label.tier;
		label.tier = newTier;
		label.maxRosterSize = GetMaxRosterForTier(newTier);
		GD.Print($"[LabelManager] {label.labelName} promoted from {oldTier} to {newTier}!");
		OnLabelPromoted?.Invoke(label, oldTier, newTier);
	}
	
	private void DemoteLabel(AILabel label, LabelTier newTier) {
		var oldTier = label.tier;
		label.tier = newTier;
		GD.Print($"[LabelManager] {label.labelName} demoted from {oldTier} to {newTier}");
		OnLabelDemoted?.Invoke(label, oldTier, newTier);
	}
	
	private void DriftAttributes(AILabel label) {
		float drift = 0.02f;
		if (currentYear > 1963) label.productionQuality = Mathf.Min(1f, label.productionQuality + drift * 0.5f);
		label.scoutingAbility += (float)GD.RandRange(-drift, drift);
		label.riskTolerance += (float)GD.RandRange(-drift, drift);
		label.scoutingAbility = Mathf.Clamp(label.scoutingAbility, 0f, 1f);
		label.riskTolerance = Mathf.Clamp(label.riskTolerance, 0f, 1f);
	}
	
	private int GetMaxRosterForTier(LabelTier tier) => tier switch {
		LabelTier.Major => 50, LabelTier.MidTier => 25, LabelTier.Independent => 12, LabelTier.Small => 5, _ => 8
	};
	
	public List<AILabel> GetLabelsByTier(LabelTier tier) => activeLabels.Where(l => l.tier == tier && l.IsActive).ToList();
	public List<AILabel> GetLabelsByGenre(Genre genre) => activeLabels.Where(l => l.preferredGenres.Contains(genre) && l.IsActive).ToList();
	public List<AILabel> GetLabelsInRegion(string regionId) => activeLabels.Where(l => l.strongRegions.Contains(regionId) && l.IsActive).ToList();
	
	public AILabel GetRandomLabelForSigning(Genre artistGenre, float artistQuality) {
		var candidates = activeLabels.Where(l => l.IsActive && l.CurrentRosterSize < l.maxRosterSize && (l.preferredGenres.Contains(artistGenre) || l.riskTolerance > 0.6f)).ToList();
		if (candidates.Count == 0) return null;
		
		float totalWeight = candidates.Sum(l => l.scoutingAbility + l.budgetLevel);
		float roll = GD.Randf() * totalWeight;
		float cumulative = 0f;
		foreach (var label in candidates) {
			cumulative += label.scoutingAbility + label.budgetLevel; // FIX: Applied typo fix from original code
			if (roll <= cumulative) return label;
		}
		return candidates[(int)GD.RandRange(0, candidates.Count - 1)];
	}
	
	public AILabel GetLabelById(string id) {
		var label = activeLabels.FirstOrDefault(l => l.labelId == id);
		if (label == null) label = defunctLabels.FirstOrDefault(l => l.labelId == id);
		return label;
	}
}
