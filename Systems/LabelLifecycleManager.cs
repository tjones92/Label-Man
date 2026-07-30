using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class LabelLifecycleManager : Node {
	public sealed class OperatingRosterTargetEvent {
		public AILabel Label;
		public LabelOperatingTargetReason Reason;
		public int PriorTarget;
		public int NewTarget;
		public int Week;
		public GameDate Date;
		public string EligibilityResult;
		public string BlockingReason;
		public int RecentChartingCount;
		public int RecentReleaseCount;
		public int WeeksSincePreviousOrganicIncrease;
	}
	private const float IndependentPromotionCapability = 0.30f;
	private const float BoutiquePromotionCapability = 0.32f;
	private const float MidTierPromotionCapability = 0.55f;
	private const float MajorPromotionCapability = 0.78f;
	private const float DemotionHysteresis = 0.08f;
	private const int BoutiqueAuteurRosterThreshold = 8;
	// A Boutique pivot to Independent is a deliberate commercial breakout, not a roster-cap
	// trigger. Set above the Small->Independent bar (1) so only a genuine hitmaker crosses it.
	private const int BoutiquePivotMinimumRecentChartingRecords = 3;
	private const int MidTierPromotionMinimumOperatingMonths = 18;
	private const int MidTierPromotionMinimumSustainedQuarters = 4;
	private const int MidTierPromotionMinimumRoster = 6;
	private const int MidTierPromotionMinimumRecentChartingRecords = 2;
	private const int IndependentPromotionMinimumRecentChartingRecords = 1;
	// Section 28: a heavily distributor-dependent hitmaker (Stax, A&M) reached MidTier footprint on
	// a major's P&D deal without ever building its own national network. The dependent-footprint
	// promotion route requires a stronger sustained chart-and-roster showing than the organic route.
	private const int MidTierPromotionDependentChartingRecords = 4;
	private const int MidTierPromotionDependentRoster = 8;
	// Section 28: months of overhead runway a label needs before it can fund studio upgrades and
	// keep pace with the post-1963 production-quality climb; below it, production stagnates.
	private const float StudioUpgradeRunwayMonths = 6f;
	private const float MidTierPromotionMinimumRunwayMonths = 6f;
	private const int LaunchCompetitionMinimumOperatingMonths = 6;
	private const int RuntimeCompetitionMinimumOperatingMonths = 18;
	private const int RuntimeEmergenceRunwayMonths = 18;
	private const int RuntimeEmergenceReleaseLaneTarget = 3;
	private const int CompetitiveExitSafeHarborChartingRecords = 2;
	private const float CompetitiveExitOneChartMultiplier = 0.35f;
	private const float CompetitiveExitStableBaseChance = 0.08f;
	private const float CompetitiveExitProfitableMultiplier = 0.65f;
	private const float CompetitiveExitLowRunwayMultiplier = 1.75f;
	private const float CompetitiveExitMaximumChance = 0.50f;
	private const int MajorRosterThreshold = 25;
	private const float DependencyLowThreshold = 0.35f;
	public static LabelLifecycleManager Instance { get; private set; }
	
	[ExportGroup("Active Labels")]
	private List<AILabel> activeLabels = new List<AILabel>();
	private List<AILabel> defunctLabels = new List<AILabel>();
	
	[ExportGroup("Settings")]
	[Export] private float monthlyBirthChance = 0.15f;
	[Export] private float monthlyDeathCheckFrequency = 1f;
	[Export] private int targetLabels1960 = 600;
	[Export] private int targetLabels1961To1962 = 620;
	[Export] private int targetLabels1963To1964 = 650;
	[Export] private int targetLabels1965To1966 = 675;
	[Export] private int targetLabels1967To1968 = 645;
	[Export] private int targetLabels1969Plus = 625;
	// The live population sits far enough below the authored target that both of
	// CheckForBirths' deficit terms saturate, so this cap — not the target — is what
	// actually sets founding volume, and through it the equilibrium level. See the
	// comment on CheckForBirths.
	//
	// History: this stayed at 6 because 8 breached the 1966 scheduledAlbumProjects gate
	// (D7LabelPopulationChartCapacityHandoff). That gate is now REPORTED, NOT ENFORCED
	// (ChartAuditRunner.ReportCompletedYearRatio) -- enforcing the album-project product
	// charged a fixed ~1.17 LP-transition mix multiplier that turned the declared 1.30
	// ceiling into an undeclared ~1.11 volume ceiling. The metrics actually policed now
	// (successfulReleases, totalUnits, grossRevenue, labelNet, marketNet, band [0.70,1.30])
	// scored 1.07-1.13 at the worst configuration measured, so the old wall no longer
	// binds. Combined with the healthier post-sec28 album economy (0.4% project
	// cancellation at decade end), the cap is raised to let the real mid-60s micro-label
	// explosion express in birth volume -- adding Independent breadth and diluting the
	// owner-Major chart-share overshoot with non-Major entrants. Still staged below the
	// 600-675 authored target so the population never approaches the volume ceiling.
	[Export] private int maxMonthlyBirths = 9;
	// Most firms in the independent record business are represented by Independent rather
	// than the deliberately shoestring Small tier. Keeping a Small tail while making
	// Independent the common runtime entrant matches the desired below-MidTier composition.
	[Export(PropertyHint.Range, "0,1,0.01")] private float runtimeSmallFoundingShare = 0.25f;
	
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
	private bool processingEnabled = true;
	
	public event Action<AILabel, string> OnLabelDefunct;
	public event Action<AILabel> OnLabelFounded;
	public event Action<AILabel, LabelTier, LabelTier> OnLabelPromoted;
	public event Action<AILabel, LabelTier, LabelTier> OnLabelDemoted;
	public event Action<OperatingRosterTargetEvent> OnOperatingRosterTargetChanged;
	public event Action<RuntimeLabelProfileFactory.Result> OnRuntimeLabelProfileInitialized;
	
	public override void _EnterTree() {
		if (Instance != null && Instance != this) { QueueFree(); return; }
		Instance = this;
	}

	public override void _Ready() {
		if (TimeManager.Instance != null) TimeManager.Instance.OnMonthChanged += OnMonthChanged;
	}

	public override void _ExitTree() {
		if (TimeManager.Instance != null) TimeManager.Instance.OnMonthChanged -= OnMonthChanged;
	}
	
	private int GetTargetLabelCount(int year) {
		if (year <= 1960) return targetLabels1960;
		if (year <= 1962) return targetLabels1961To1962;
		if (year <= 1964) return targetLabels1963To1964;
		if (year <= 1966) return targetLabels1965To1966;
		if (year <= 1968) return targetLabels1967To1968;
		return targetLabels1969Plus;
	}
	
	public void InitializeLabels(List<AILabel> labels, int startYear = 1960) {
		currentYear = startYear;
		currentMonth = 1;
		DefunctThisYear = 0;
		FoundedThisYear = 0;
		
		activeLabels = labels ?? new List<AILabel>();
		defunctLabels.Clear();
		GD.Print($"[LabelManager] Attached lifecycle to {activeLabels.Count} live labels for {startYear}");
	}

	private void OnMonthChanged(GameDate date) => ProcessMonth(date.year, date.month);
	public void SetProcessingEnabled(bool enabled) => processingEnabled = enabled;
	
	public void ProcessMonth(int year, int month) {
		if (!processingEnabled) return;
		currentYear = year;
		currentMonth = month;
		
		if (month == 1) { DefunctThisYear = 0; FoundedThisYear = 0; }
		foreach (var label in activeLabels.Where(l => l.status == LabelStatus.Bankrupt).ToList()) {
			KillLabel(label, "Bankruptcy");
		}
		
		foreach (var label in activeLabels.Where(l => l.IsActive).ToList()) {
			UpdateLabelHealth(label);
			CheckForDeath(label);
		}
		
		CheckForBirths();
		
		if (month % 3 == 0) ProcessQuarterlyChanges();
	}
	
	private void UpdateLabelHealth(AILabel label) {
		label.monthsActive++;
		label.momentumScore = Mathf.Lerp(label.momentumScore, CalculateMomentum(label), 0.3f);
	}
	
	private float CalculateMomentum(AILabel label) => Mathf.Clamp(label.reputation + (label.top40Hits * 0.05f), 0f, 1f);
	
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
		if (label.status == LabelStatus.Defunct || label.status == LabelStatus.Acquired) return;
		foreach (SimulatedArtist artist in label.roster.ToList()) {
			if (ArtistPopulationLifecycle.Enabled && RosterManager.Instance != null)
				RosterManager.Instance.HandleLabelClosure(label, artist, currentYear);
			else
				ArtistManager.Instance?.DropArtist(artist, currentYear);
		}
		label.roster.Clear();
		label.status = LabelStatus.Defunct;
		defunctLabels.Add(label);
		DefunctThisYear++;
		GD.Print($"[LabelManager] {label.labelName} has closed. Reason: {reason}. Operated for {label.monthsActive} months.");
		OnLabelDefunct?.Invoke(label, reason);
	}

	public void MarkLabelAcquired(AILabel label, AILabel distributor) {
		if (label == null || distributor == null || label == distributor || !label.IsActive) return;
		label.status = LabelStatus.Acquired;
		defunctLabels.Add(label);
		DefunctThisYear++;
		string reason = $"Absorbed by {distributor.labelName}";
		GD.Print($"[LabelManager] {label.labelName} acquired by {distributor.labelName}.");
		OnLabelDefunct?.Invoke(label, reason);
	}

	// Subsidiary absorption (section 24): the label rolls up to the parent's corporate family
	// but keeps operating and charting. Unlike MarkLabelAcquired, status stays operational and
	// the label is NOT added to defunctLabels or counted as defunct -- it is not a shut-down.
	public void MarkLabelSubsidiary(AILabel label, AILabel distributor) {
		if (label == null || distributor == null || label == distributor || !label.IsActive) return;
		GD.Print($"[LabelManager] {label.labelName} is now a subsidiary of {distributor.labelName}.");
	}
	
	/// <summary>
	/// Founding is written as a deficit-seeking controller, but only its anti-overshoot
	/// half is ever live. Below roughly 17 labels short of target the chance term clamps
	/// to 1.0 and the attempt term clamps to maxMonthlyBirths, and the live population
	/// runs hundreds short all decade, so founding is a flat maxMonthlyBirths per month
	/// regardless of the deficit. Both terms only start modulating in the last stretch
	/// before target, which is what stops the population overshooting it.
	///
	/// That makes maxMonthlyBirths the population governor. Equilibrium is founding
	/// volume divided by the annual death rate, so the cap sets a level rather than a
	/// ceiling, and it must clear the death rate for the population to hold at all.
	/// It is deliberately staged below what the authored target would require: reaching
	/// 600-675 live labels breaches the catastrophic gate on album-project volume, which
	/// is a release-economics problem rather than a lifecycle one.
	/// </summary>
	private void CheckForBirths() {
		int currentCount = TotalActiveLabels;
		int targetCount = GetTargetLabelCount(currentYear);
		if (currentCount >= targetCount) return;
		float spawnModifier = (targetCount - currentCount) / 20f;
		float adjustedChance = Mathf.Clamp(monthlyBirthChance + spawnModifier, 0f, 1f);
		
		if (currentYear >= 1964 && currentYear <= 1966) adjustedChance = Mathf.Min(1f, adjustedChance * 1.3f);
		int attempts = Mathf.Min(maxMonthlyBirths, Mathf.Max(1, Mathf.CeilToInt((targetCount - currentCount) / 12f)));
		for (int attempt = 0; attempt < attempts && TotalActiveLabels < targetCount; attempt++) {
			if (GD.Randf() < adjustedChance) SpawnNewLabel();
		}
	}
	
	private void SpawnNewLabel() {
		LabelTier tier = SelectRuntimeFoundingTier(GD.Randf(), runtimeSmallFoundingShare);
		AILabel newLabel = generator.GenerateSingleLabel(regions, currentYear, tier);
		if (ArtistPopulationLifecycle.Enabled) {
			GameDate birthDate = TimeManager.Instance?.CurrentDate ?? new GameDate(currentYear, currentMonth, 1);
			newLabel.populationOrigin = LabelPopulationOrigin.RuntimeFounded;
			newLabel.runtimeBirthWeek = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
			newLabel.runtimeBirthYear = birthDate.year;
			newLabel.runtimeBirthMonth = birthDate.month;
			newLabel.runtimeBirthDay = birthDate.day;
			RuntimeLabelProfileFactory.Result profile = RuntimeLabelProfileFactory.Initialize(newLabel, regions, newLabel.runtimeBirthWeek,
				birthDate, SimulationSeedBootstrap.RequestedSeed ?? 0UL);
			RosterManager.Instance?.InitializeRuntimeRosterForLabel(newLabel);
			OnRuntimeLabelProfileInitialized?.Invoke(profile);
		} else {
			RosterManager.Instance?.InitializeRuntimeRosterForLabel(newLabel);
		}
		if (ArtistPopulationLifecycle.Enabled) EmitTargetEvent(newLabel, LabelOperatingTargetReason.RuntimeBootstrap, 0,
			newLabel.OperatingRosterTarget, "Initialized", "None", 0);
		activeLabels.Add(newLabel);
		ChartManager.Instance?.RegisterLabel(newLabel);
		CompetitorManager.Instance?.RegisterLabel(newLabel);
		FoundedThisYear++;
		GD.Print($"[LabelManager] New label founded: {newLabel.labelName} ({newLabel.archetype})");
		OnLabelFounded?.Invoke(newLabel);
	}

	internal static LabelTier SelectRuntimeFoundingTier(float roll, float smallShare) =>
		roll < Mathf.Clamp(smallShare, 0f, 1f) ? LabelTier.Small : LabelTier.Independent;
	
	private void ProcessQuarterlyChanges() {
		foreach (var label in activeLabels.Where(l => l.IsActive).ToList()) {
			CheckForTierChange(label);
			if (!label.IsActive) continue;
			TryAuthorizeOrganicGrowth(label);
			DriftAttributes(label);
			TryApplyCompetitiveExit(label);
		}
	}

	/// <summary>
	/// The enabled talent market can keep a label rostered even when its releases
	/// never establish demand. A quarterly, isolated-hash competition review lets
	/// those marginal labels exit without removing daily scouting, runtime entry,
	/// or the ordinary signing/release paths from viable labels.
	/// </summary>
	private void TryApplyCompetitiveExit(AILabel label) {
		if (!ArtistPopulationLifecycle.Enabled) return;
		int chartingLastYear = CompetitorManager.Instance?.GetRecentChartingRecordCount(label.labelId, 52) ?? 0;
		float chance = GetCompetitiveExitChance(label, chartingLastYear);
		if (chance <= 0f) return;
		ulong seed = SimulationSeedBootstrap.RequestedSeed ?? 0UL;
		float roll = GetCompetitiveExitRoll(seed, label.labelId, currentYear, currentMonth);
		if (roll < chance) KillLabel(label, "Competitive exit");
	}

	internal static float GetCompetitiveExitChance(AILabel label, int chartingLastYear) {
		if (label == null || !label.IsActive || label.tier == LabelTier.Major ||
			chartingLastYear >= CompetitiveExitSafeHarborChartingRecords) return 0f;
		int minimumMonths = label.populationOrigin == LabelPopulationOrigin.RuntimeFounded
			? RuntimeCompetitionMinimumOperatingMonths
			: LaunchCompetitionMinimumOperatingMonths;
		if (label.monthsActive < minimumMonths) return 0f;

		float statusMultiplier = label.status switch {
			LabelStatus.Rising => 0.65f,
			LabelStatus.Stable => 1f,
			LabelStatus.Struggling => 2f,
			LabelStatus.Dying => 3f,
			_ => 0f
		};
		if (statusMultiplier <= 0f) return 0f;
		float chance = CompetitiveExitStableBaseChance * statusMultiplier;
		if (chartingLastYear == 1) chance *= CompetitiveExitOneChartMultiplier;
		if (label.lastMonthlyProfit > 0f) chance *= CompetitiveExitProfitableMultiplier;
		if (label.cashReserves < label.GetMonthlyOverhead() * 6f) chance *= CompetitiveExitLowRunwayMultiplier;
		chance *= label.tier switch {
			LabelTier.MidTier => 1.25f,
			LabelTier.Independent => 1.15f,
			_ => 1f
		};
		return Mathf.Clamp(chance, 0f, CompetitiveExitMaximumChance);
	}

	internal static float GetCompetitiveExitRoll(ulong seed, string labelId, int year, int month) {
		ulong hash = 14695981039346656037UL;
		foreach (char value in $"{seed}|{labelId}|{year}|{month}|LabelCompetitionV1") {
			hash ^= value;
			hash *= 1099511628211UL;
		}
		return (hash >> 40) * (1f / 16777216f);
	}

	private void TryAuthorizeOrganicGrowth(AILabel label) {
		if (!ArtistPopulationLifecycle.Enabled || !IsOrganicGrowthEligibleOrigin(label)) return;
		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		int chartingCount = CompetitorManager.Instance?.GetRecentChartingRecordCount(label.labelId, 52) ?? 0;
		int recentReleaseCount = CompetitorManager.Instance?.GetRecentReleasedRecordCount(label.labelId, 52) ?? 0;
		string blockingReason = GetOrganicGrowthBlockingReason(label, chartingCount, recentReleaseCount, week);
		label.lastOrganicGrowthEligibilityWeek = week;
		label.lastOrganicGrowthBlockingReason = blockingReason;
		if (blockingReason != "Eligible") return;
		int priorTarget = label.OperatingRosterTarget;
		int weeksSincePreviousOrganicIncrease = label.lastOrganicRosterTargetGrowthWeek >= 0 ? week - label.lastOrganicRosterTargetGrowthWeek : 0;
		label.SetOperatingRosterTarget(priorTarget + 1, LabelOperatingTargetReason.OrganicGrowth, week);
		label.organicRosterTargetGrowthCount++;
		label.lastOrganicRosterTargetGrowthWeek = week;
		EmitTargetEvent(label, LabelOperatingTargetReason.OrganicGrowth, priorTarget, label.OperatingRosterTarget,
			"Eligible", "None", chartingCount, weeksSincePreviousOrganicIncrease, recentReleaseCount);
	}

	internal static bool IsRuntimeFounderInEmergenceRunway(AILabel label) =>
		ArtistPopulationLifecycle.Enabled &&
		label?.populationOrigin == LabelPopulationOrigin.RuntimeFounded &&
		label.monthsActive <= RuntimeEmergenceRunwayMonths;

	/// <summary>
	/// Which labels may earn operating-target growth. Runtime founders grow out of
	/// their one-artist bootstrap as before. Launch-population Majors and MidTiers are
	/// added because appetite could otherwise only move through promotion or
	/// acquisition reconciliation, which froze them at their 1960 rosters: mean Major
	/// appetite pinned at exactly 25.10 from 1964 and MidTier at 10.67 from 1961, which
	/// removes the demand-side counterpart of the LP takeover, when Columbia, Warner
	/// and Atlantic all scaled up. They grow into the hard capacity they already have
	/// (50 and 25) under the same earned rules below, so the slots are earned rather
	/// than conferred by a tier lookup. Launch Small, Boutique and Independent labels
	/// stay frozen deliberately: a handful of acts or death is the right shape for a
	/// sixties independent, and growing them would be fitting the aggregate rather
	/// than modelling it.
	///
	/// A promoted launch label stays frozen too, and that is a known compromise rather
	/// than a design position. ReconcileCapacityForTierChange hands it a larger
	/// maxRosterSize while preserving its old operating target, and nothing can raise
	/// that target, so promotion moves the tier without moving appetite. Admitting
	/// promoted labels here (via AILabel.hasEarnedTierPromotion, which is maintained for
	/// exactly this purpose) was measured at 522 weeks and breaches the catastrophic gate:
	/// thirteen promoted Boutiques growing from eight slots toward twelve carried
	/// scheduledAlbumProjects to 1.3219 at 1966 against a 1.30 ceiling. It is available
	/// once album-project economics have slack — see D7LabelPopulationChartCapacityHandoff.
	/// </summary>
	internal static bool IsOrganicGrowthEligibleOrigin(AILabel label) => label != null &&
		(label.populationOrigin == LabelPopulationOrigin.RuntimeFounded ||
			(label.populationOrigin == LabelPopulationOrigin.LaunchPopulation && label.tier is LabelTier.Major or LabelTier.MidTier));

	internal static string GetOrganicGrowthBlockingReason(AILabel label, int chartingCount, int recentReleaseCount, int week) {
		if (!IsOrganicGrowthEligibleOrigin(label)) return "NotGrowthEligible";
		if (!label.IsActive) return "InactiveLabel";
		if (label.lastOrganicRosterTargetGrowthWeek == week) return "AlreadyReviewedThisQuarter";
		if (label.CurrentRosterSize < label.OperatingRosterTarget) return "OperatingTargetUnfilled";
		if (label.OperatingRosterTarget >= label.maxRosterSize) return "HardCapacityFull";
		if (label.status != LabelStatus.Stable && label.status != LabelStatus.Rising) return "UnhealthyStatus";
		if (label.lastMonthlyProfit <= 0f) return "NotProfitable";
		if (label.cashReserves < 6f * label.GetMonthlyOverhead()) return "InsufficientRunway";
		if (label.OperatingRosterTarget < RuntimeEmergenceReleaseLaneTarget)
			return recentReleaseCount < 1 ? "NoRecentRelease" : "Eligible";
		if (label.consecutiveLossMonths != 0) return "ConsecutiveLosses";
		if (chartingCount < 1) return "NoRecentCharting";
		return "Eligible";
	}

	internal static bool TryAuthorizeOrganicGrowthForProbe(AILabel label, int chartingCount, int recentReleaseCount, int week) {
		string blockingReason = GetOrganicGrowthBlockingReason(label, chartingCount, recentReleaseCount, week);
		label.lastOrganicGrowthEligibilityWeek = week;
		label.lastOrganicGrowthBlockingReason = blockingReason;
		if (blockingReason != "Eligible") return false;
		int priorTarget = label.OperatingRosterTarget;
		label.SetOperatingRosterTarget(priorTarget + 1, LabelOperatingTargetReason.OrganicGrowth, week);
		label.organicRosterTargetGrowthCount++;
		label.lastOrganicRosterTargetGrowthWeek = week;
		return label.OperatingRosterTarget == priorTarget + 1;
	}
	
	private void CheckForTierChange(AILabel label) {
		float capability = label.CalculateCapabilityScore();
		float promotionFloor = GetPromotionFloor(label.tier);
		if (promotionFloor >= 0f && capability >= promotionFloor) label.sustainedCapabilityQuarters++;
		else label.sustainedCapabilityQuarters = 0;

		float demotionFloor = GetCapabilityBandFloor(label.tier) - DemotionHysteresis;
		if (label.tier != LabelTier.Boutique && demotionFloor > 0f && capability < demotionFloor) {
			label.sustainedLowCapabilityQuarters++;
		} else {
			label.sustainedLowCapabilityQuarters = 0;
		}

		if (TryPromoteLabel(label)) return;
		if (label.tier == LabelTier.Boutique) return;
		if (label.sustainedLowCapabilityQuarters >= 2 || label.consecutiveLossMonths > 12) {
			LabelTier? lowerTier = GetLowerTier(label.tier);
			if (lowerTier.HasValue) DemoteLabel(label, lowerTier.Value);
		}
	}

	// The charting evidence a promotion asks for scales with the roster that has to
	// produce it. Small carries five slots against Independent's twelve and MidTier's
	// twenty-five, so requiring the MidTier bar of two charting records off five slots
	// made the bottom rung unreachable: across a gated decade of 300-480 live labels,
	// Small -> Independent fired zero times while twelve labels demoted the other way.
	// Two records is also exactly CompetitiveExitSafeHarborChartingRecords, so the only
	// Small labels that qualified were the ones already immune from competitive exit.
	// One record is the same signal the exit rule already credits through
	// CompetitiveExitOneChartMultiplier, and the operating-months and sustained-capability
	// gates below still carry the evidence requirement.
	private bool TryPromoteLabel(AILabel label) {
		int chartingLastYear = CompetitorManager.Instance?.GetRecentChartingRecordCount(label.labelId) ?? 0;
		switch (label.tier) {
			case LabelTier.Small when label.sustainedCapabilityQuarters >= 2 && label.monthsActive > 18 &&
				chartingLastYear >= IndependentPromotionMinimumRecentChartingRecords:
				PromoteLabel(label, LabelTier.Independent);
				return true;
			// A Boutique is an auteur-driven business model (its roster cap of 8 is the point,
			// not a ceiling to grow out of), so promotion to Independent must be a deliberate
			// strategic pivot rather than the mere fact of filling every slot. The former
			// roster-at-capacity trigger promoted any successful Boutique -- exactly backwards.
			// Two gates now express the pivot: (1) the archetype must be a growth-oriented one
			// (a RegionalHustler is literally "trying to break out"); the auteur/niche archetypes
			// (JazzPrestige, BluesRoots, FolkBoutique, GospelPowerhouse, CountrySpecialist) never
			// promote, they stay curated boutiques by design. (2) genuine commercial breakout,
			// proven by a stronger recent charting showing than the Small->Independent bar, not a
			// full roster. The operating-months and sustained-capability gates are unchanged and
			// still guard against the launch population promoting en masse on seeded state.
			case LabelTier.Boutique when label.sustainedCapabilityQuarters >= 2 && label.monthsActive > 18 &&
				IsBoutiqueGrowthArchetype(label.archetype) &&
				chartingLastYear >= BoutiquePivotMinimumRecentChartingRecords:
				PromoteLabel(label, LabelTier.Independent);
				return true;
			case LabelTier.Independent when IsIndependentReadyForMidTier(label, chartingLastYear):
				PromoteLabel(label, LabelTier.MidTier);
				return true;
			case LabelTier.MidTier when label.sustainedCapabilityQuarters >= 4 && label.CurrentRosterSize >= MajorRosterThreshold && CanSupportMajorBranches(label):
				PromoteLabel(label, LabelTier.Major);
				return true;
			default:
				return false;
		}
	}

	// MidTier represents a large, proven independent rather than a capability-only
	// classification. Requiring observed operating scale and success prevents the
	// launch population from promoting en masse at its second quarterly review.
	internal static bool IsIndependentReadyForMidTier(AILabel label, int chartingLastYear) {
		if (label == null || label.tier != LabelTier.Independent || !label.IsActive) return false;
		if (label.monthsActive <= MidTierPromotionMinimumOperatingMonths ||
			label.sustainedCapabilityQuarters < MidTierPromotionMinimumSustainedQuarters ||
			label.CurrentRosterSize < MidTierPromotionMinimumRoster ||
			chartingLastYear < MidTierPromotionMinimumRecentChartingRecords) return false;
		// MidTier footprint has two historical routes. The organic route builds its own national
		// reach and sheds distributor dependency (the pre-existing test). The dependent-hitmaker
		// route reaches MidTier scale on a major's P&D deal -- high owned reach was never required
		// of a Stax or an A&M -- and instead proves footprint through a stronger sustained chart
		// and roster showing. Requiring one of the two keeps promotion earned, not automatic.
		bool organicReachRoute = label.ownedReach >= 0.50f && GetDependency(label) < DependencyLowThreshold;
		bool dependentFootprintRoute = chartingLastYear >= MidTierPromotionDependentChartingRecords &&
			label.CurrentRosterSize >= MidTierPromotionDependentRoster;
		if (!organicReachRoute && !dependentFootprintRoute) return false;
		if (label.status != LabelStatus.Stable && label.status != LabelStatus.Rising) return false;
		if (label.consecutiveLossMonths != 0 || label.lastMonthlyProfit <= 0f) return false;
		return label.cashReserves >= MidTierPromotionMinimumRunwayMonths * label.GetMonthlyOverhead();
	}

	// The auteur/niche archetypes are curated business models that do not seek to scale out of
	// the boutique tier; the growth-oriented ones can pivot to Independent on a commercial breakout.
	private static bool IsBoutiqueGrowthArchetype(LabelArchetype archetype) => archetype switch {
		LabelArchetype.JazzPrestige or LabelArchetype.BluesRoots or LabelArchetype.FolkBoutique
			or LabelArchetype.GospelPowerhouse or LabelArchetype.CountrySpecialist => false,
		_ => true
	};

	private static float GetPromotionFloor(LabelTier tier) => tier switch {
		LabelTier.Small => IndependentPromotionCapability,
		LabelTier.Boutique => BoutiquePromotionCapability,
		LabelTier.Independent => MidTierPromotionCapability,
		LabelTier.MidTier => MajorPromotionCapability,
		_ => -1f
	};

	private static float GetCapabilityBandFloor(LabelTier tier) => tier switch {
		LabelTier.Major => 0.75f,
		LabelTier.MidTier => 0.50f,
		LabelTier.Independent => 0.30f,
		LabelTier.Boutique => 0.15f,
		_ => 0f
	};

	private static LabelTier? GetLowerTier(LabelTier tier) => tier switch {
		LabelTier.Major => LabelTier.MidTier,
		LabelTier.MidTier => LabelTier.Independent,
		LabelTier.Independent => LabelTier.Small,
		_ => null
	};

	private static float GetDependency(AILabel label) =>
		label.borrowedReach / (label.borrowedReach + label.ownedReach + 0.01f);

	private static bool CanSupportMajorBranches(AILabel label) {
		float monthlyMajorOverhead = 3000f + (label.CurrentRosterSize * 200f);
		return label.cashReserves >= monthlyMajorOverhead * 12f;
	}
	
	private void PromoteLabel(AILabel label, LabelTier newTier) {
		var oldTier = label.tier;
		int priorTarget = label.OperatingRosterTarget;
		label.tier = newTier;
		label.hasEarnedTierPromotion = true;
		ReconcileCapacityForTierChange(label, newTier, LabelOperatingTargetReason.PromotionReconciliation, priorTarget);
		label.sustainedCapabilityQuarters = 0;
		label.sustainedLowCapabilityQuarters = 0;
		GD.Print($"[LabelManager] {label.labelName} promoted from {oldTier} to {newTier}!");
		OnLabelPromoted?.Invoke(label, oldTier, newTier);
	}
	
	private void DemoteLabel(AILabel label, LabelTier newTier) {
		var oldTier = label.tier;
		int priorTarget = label.OperatingRosterTarget;
		label.tier = newTier;
		label.hasEarnedTierPromotion = false;
		ReconcileCapacityForTierChange(label, newTier, LabelOperatingTargetReason.DemotionReconciliation, priorTarget);
		label.sustainedCapabilityQuarters = 0;
		label.sustainedLowCapabilityQuarters = 0;
		GD.Print($"[LabelManager] {label.labelName} demoted from {oldTier} to {newTier}");
		OnLabelDemoted?.Invoke(label, oldTier, newTier);
	}
	
	private void DriftAttributes(AILabel label) {
		float drift = 0.02f;
		// Section 28: the mid-60s multitrack studio era (3->4->8 track, orchestral arrangements)
		// was exorbitantly expensive. The old code granted every active label a free +0.01/quarter
		// production lift after 1963 -- up to +0.24 by 1969 -- which erased the capital barrier that
		// let Majors pull acoustically ahead and is a driver of the ahistorical late-decade indie
		// chart surge. Only a label with capital to fund studio upgrades keeps pace now; a
		// cash-starved small label's production stagnates, as it historically did.
		if (currentYear > 1963 && label.cashReserves >= label.GetMonthlyOverhead() * StudioUpgradeRunwayMonths)
			label.productionQuality = Mathf.Min(1f, label.productionQuality + drift * 0.5f);
		label.scoutingAbility += (float)GD.RandRange(-drift, drift);
		label.riskTolerance += (float)GD.RandRange(-drift, drift);
		label.scoutingAbility = Mathf.Clamp(label.scoutingAbility, 0f, 1f);
		label.riskTolerance = Mathf.Clamp(label.riskTolerance, 0f, 1f);
	}
	
	public static int GetRosterCapacityForTier(LabelTier tier) => tier switch {
		LabelTier.Major => 50, LabelTier.MidTier => 25, LabelTier.Independent => 12,
		LabelTier.Boutique => 8, LabelTier.Small => 5, _ => 8
	};

	private void ReconcileCapacityForTierChange(AILabel label, LabelTier tier, LabelOperatingTargetReason reason, int priorTarget) {
		label.maxRosterSize = Mathf.Max(GetRosterCapacityForTier(tier), label.CurrentRosterSize);
		int reconciledTarget = Mathf.Max(label.CurrentRosterSize, Mathf.Min(priorTarget, label.maxRosterSize));
		label.SetOperatingRosterTarget(reconciledTarget, reason, ChartManager.Instance?.GetCurrentChartWeek() ?? 0);
		if (ArtistPopulationLifecycle.Enabled) EmitTargetEvent(label, reason, priorTarget, label.OperatingRosterTarget,
			"Reconciled", "None", CompetitorManager.Instance?.GetRecentChartingRecordCount(label.labelId, 52) ?? 0);
	}

	public void ReconcileAcquisitionRosterTarget(AILabel distributor) {
		if (distributor == null) return;
		int priorTarget = distributor.OperatingRosterTarget;
		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		ReconcileAcquisitionCapacity(distributor, week);
		if (ArtistPopulationLifecycle.Enabled) EmitTargetEvent(distributor, LabelOperatingTargetReason.AcquisitionReconciliation,
			priorTarget, distributor.OperatingRosterTarget, "Reconciled", "None",
			CompetitorManager.Instance?.GetRecentChartingRecordCount(distributor.labelId, 52) ?? 0);
	}

	internal static void ReconcileAcquisitionRosterTargetForProbe(AILabel distributor, int week) => ReconcileAcquisitionCapacity(distributor, week);

	private static void ReconcileAcquisitionCapacity(AILabel distributor, int week) {
		distributor.maxRosterSize = Mathf.Max(Mathf.Max(distributor.maxRosterSize, GetRosterCapacityForTier(distributor.tier)), distributor.CurrentRosterSize);
		distributor.SetOperatingRosterTarget(Mathf.Max(distributor.OperatingRosterTarget, distributor.CurrentRosterSize),
			LabelOperatingTargetReason.AcquisitionReconciliation, week);
	}

	private void EmitTargetEvent(AILabel label, LabelOperatingTargetReason reason, int priorTarget, int newTarget,
		string eligibilityResult, string blockingReason, int chartingCount, int weeksSincePreviousOrganicIncrease = 0,
		int recentReleaseCount = 0) {
		if (!ArtistPopulationLifecycle.Enabled) return;
		OnOperatingRosterTargetChanged?.Invoke(new OperatingRosterTargetEvent {
			Label = label, Reason = reason, PriorTarget = priorTarget, NewTarget = newTarget,
			Week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0,
			Date = TimeManager.Instance?.CurrentDate ?? new GameDate(currentYear, currentMonth, 1),
			EligibilityResult = eligibilityResult, BlockingReason = blockingReason, RecentChartingCount = chartingCount,
			RecentReleaseCount = recentReleaseCount,
			WeeksSincePreviousOrganicIncrease = weeksSincePreviousOrganicIncrease
		});
	}
	
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
