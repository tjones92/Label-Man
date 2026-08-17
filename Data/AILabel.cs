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
	// Markets where the label has placed its line with an independent wholesale house
	// (handoff section 33). This is the label's own asset: it survives a distribution
	// deal being signed and terminated, and unlike a deal it confers no ownership on
	// anybody. Unlike the per-song scope of a P&D contract (section 11), a wholesaler
	// carried the label's whole line in its market, so this coverage is label-wide.
	public readonly HashSet<string> independentDistributionRegions = new(StringComparer.Ordinal);
	// Money a wholesale house owes but has not paid yet (handoff section 33.1 stage 3).
	// Distributors took 90-120 day terms with full return privileges while the label had
	// already paid to press and ship, so a hit consumed a small label's cash long before it
	// produced any -- which is what made a major's offer attractive.
	public readonly List<WholesaleReceivable> wholesaleReceivables = new();
	public float outstandingWholesaleReceivables;
	public float lifetimeWholesaleWriteOffs;
	
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
	[Export] public int sustainedLowChartingQuarters;
	
	// Runtime Roster (Not exported, generated at runtime)
	public List<SimulatedArtist> roster = new List<SimulatedArtist>();
	public int maxRosterSize;
	public float reputation;
	public DistributionDeal activeDeal;

	// Set when a high-dependency independent is absorbed into a Major's corporate family
	// (section 24 consolidation lever). A subsidiary keeps operating -- its own roster,
	// release imprint and chart access -- while ownership rolls up to the parent. This is
	// orthogonal to status/IsActive: a subsidiary stays operationally Rising/Stable and
	// IsActive true, unlike LabelStatus.Acquired, which is a dead shut-down state.
	public string ownerLabelId;
	public bool IsSubsidiary => !string.IsNullOrEmpty(ownerLabelId);

	// A minority "Stax" archetype (section 27): a genuinely hit-making label that stays
	// financially dependent on its distributor. It has strong creative capability (it charts)
	// but low owned reach and it deliberately does NOT build its own national network -- it
	// reinvests in music, not distribution infrastructure -- so it leans on the major's network,
	// stays high-dependency, and is absorbed late-decade contributing real chart volume. This is
	// distinct from the common weak one-or-two-hit dependents, and from a Motown that builds its
	// own reach and exits. Set at generation for a fraction of runtime founders.
	public bool distributionDependentHitmaker;

	// Runtime finance telemetry (reset and populated by CompetitorManager each week)
	public float weeklyGrossRevenue;
	public float weeklyCogs;
	public float weeklyDistributionSkim;
	public float weeklyArtistRoyalty;
	public float weeklyNetRevenue;
	public float weeklyDistributionIncome;
	// The two halves of the wholesale lag, split out so a week's earnings can be told
	// apart from a week's takings: what was billed to houses and is now waiting on their
	// terms, and what finally arrived from invoices billed months ago.
	public float weeklyWholesaleDeferred;
	public float weeklyWholesaleCollected;
	public Dictionary<ReleaseFormat, FormatRevenueMemory> revenueMemory = new();
	public Dictionary<RevenueEstimatorLane, FormatRevenueMemory> laneRevenueMemory = new();

	public FormatRevenueMemory GetOrCreateRevenueMemory(ReleaseFormat format) {
		if (!revenueMemory.TryGetValue(format, out FormatRevenueMemory memory)) {
			memory = new FormatRevenueMemory();
			revenueMemory[format] = memory;
		}
		return memory;
	}
	public FormatRevenueMemory GetOrCreateRevenueMemory(RevenueEstimatorLane lane) {
		if (!laneRevenueMemory.TryGetValue(lane, out FormatRevenueMemory memory)) {
			memory = new FormatRevenueMemory();
			laneRevenueMemory[lane] = memory;
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
	// A distributor supplies temporary national access while a deal is active. Keep the
	// permanent nationalReach field separate so termination removes borrowed capability and
	// only the bounded completed-term retention in CompetitorManager survives.
	public float effectiveNationalReach => Mathf.Clamp(nationalReach + borrowedReach, 0f, 1f);

	public bool HasDistributionInRegion(string regionId) =>
		!string.IsNullOrEmpty(regionId) &&
		((distributionRegions?.Contains(regionId) ?? false) ||
		independentDistributionRegions.Contains(regionId) ||
		(activeDeal?.grantedRegions?.Contains(regionId) ?? false));

	// A distribution deal carries specific records, not the whole catalog. The
	// label-wide members above remain the right answer for questions about the firm
	// -- deal eligibility, dependency, capability -- while the per-record members
	// below are what physical fulfillment and demand for one release must use, so a
	// deal cannot retroactively push a years-old B-side into national distribution.
	public bool RecordCoveredByActiveDeal(string recordId) =>
		activeDeal != null && activeDeal.CoversRecord(recordId);

	public float BorrowedReachForRecord(string recordId) =>
		RecordCoveredByActiveDeal(recordId) ? borrowedReach : 0f;

	public float DistributionStrengthForRecord(string recordId) =>
		Mathf.Clamp(ownedReach + BorrowedReachForRecord(recordId), 0f, 1f);

	public float EffectiveNationalReachForRecord(string recordId) =>
		Mathf.Clamp(nationalReach + BorrowedReachForRecord(recordId), 0f, 1f);

	public bool HasDistributionInRegionForRecord(string regionId, string recordId) =>
		!string.IsNullOrEmpty(regionId) &&
		((distributionRegions?.Contains(regionId) ?? false) ||
		independentDistributionRegions.Contains(regionId) ||
		(RecordCoveredByActiveDeal(recordId) && (activeDeal.grantedRegions?.Contains(regionId) ?? false)));

	/// <summary>
	/// Every market the label can physically ship to, ignoring the per-song deal scope.
	/// Owned reach is anchored to this, so a label's national presence cannot exceed the
	/// share of the map it actually serves.
	/// </summary>
	public IEnumerable<string> AllCoveredRegions() =>
		(distributionRegions ?? Array.Empty<string>())
			.Concat(independentDistributionRegions)
			.Distinct(StringComparer.Ordinal);

	public int CurrentRosterSize => roster?.Count ?? 0;
	public bool HasRosterSpace => roster == null || roster.Count < maxRosterSize;
	[Export] public int operatingRosterTarget;
	public int OperatingRosterTarget => Mathf.Clamp(operatingRosterTarget > 0 ? operatingRosterTarget : maxRosterSize, 1, Mathf.Max(1, maxRosterSize));
	public bool HasOperatingRosterSpace => roster == null || roster.Count < OperatingRosterTarget;
	public string operatingRosterTargetSource = "Unset";
	public LabelPopulationOrigin populationOrigin = LabelPopulationOrigin.Unspecified;
	/// <summary>True while the label holds a tier it earned at runtime rather than at launch.
	/// Set on promotion, cleared on demotion, so it tracks current standing rather than a
	/// one-time event. Maintained but not yet read: it is the intended input to
	/// LabelLifecycleManager.IsOrganicGrowthEligibleOrigin, so that a promoted launch label
	/// can grow into the capacity its promotion granted. That was measured at 522 weeks and
	/// breaches the album-project gate — see D7LabelPopulationChartCapacityHandoff.</summary>
	public bool hasEarnedTierPromotion;
	public int runtimeBirthWeek;
	public int runtimeBirthYear;
	public int runtimeBirthMonth;
	public int runtimeBirthDay;
	public LabelOperatingTargetReason operatingRosterTargetReason = LabelOperatingTargetReason.Unset;
	public int operatingRosterTargetLastChangeWeek;
	public int organicRosterTargetGrowthCount;
	public int lastOrganicRosterTargetGrowthWeek = -1;
	public string lastOrganicGrowthBlockingReason = "Unset";
	public int lastOrganicGrowthEligibilityWeek = -1;
	// Enabled daily-talent-market state. Dates are stored as scalar fields so old
	// saves can safely omit them and the scheduler can reconstruct a first visit.
	[Export] public int vacancyGeneration;
	[Export] public int vacancyOpenedYear;
	[Export] public int vacancyOpenedMonth;
	[Export] public int vacancyOpenedDay;
	[Export] public int nextScoutingYear;
	[Export] public int nextScoutingMonth;
	[Export] public int nextScoutingDay;
	[Export] public int lastScoutingYear;
	[Export] public int lastScoutingMonth;
	[Export] public int lastScoutingDay;
	[Export] public string lastScoutingOutcome = "NoVacancy";
	[Export] public int scoutingAppointmentOrdinal;

	public bool HasNextScoutingDate => nextScoutingYear > 0;
	public GameDate NextScoutingDate => new(nextScoutingYear, nextScoutingMonth, nextScoutingDay);
	public GameDate VacancyOpenedDate => new(vacancyOpenedYear, vacancyOpenedMonth, vacancyOpenedDay);
	public GameDate LastScoutingDate => new(lastScoutingYear, lastScoutingMonth, lastScoutingDay);
	public void SetNextScoutingDate(GameDate date) { nextScoutingYear = date.year; nextScoutingMonth = date.month; nextScoutingDay = date.day; }
	public void SetVacancyOpenedDate(GameDate date) { vacancyOpenedYear = date.year; vacancyOpenedMonth = date.month; vacancyOpenedDay = date.day; }
	public void SetLastScoutingDate(GameDate date) { lastScoutingYear = date.year; lastScoutingMonth = date.month; lastScoutingDay = date.day; }
	public void ClearScoutingAppointment() { nextScoutingYear = 0; nextScoutingMonth = 0; nextScoutingDay = 0; }

	public void SetOperatingRosterTargetFromCurrent() {
		operatingRosterTarget = Mathf.Clamp(Mathf.Max(1, CurrentRosterSize), 1, Mathf.Max(1, maxRosterSize));
		operatingRosterTargetSource = CurrentRosterSize > 0 ? "PopulatedLaunchRoster" : "OneArtistBootstrap";
	}
	public void SetOperatingRosterTarget(int target, LabelOperatingTargetReason reason, int changeWeek) {
		operatingRosterTarget = Mathf.Clamp(target, 1, Mathf.Max(1, maxRosterSize));
		operatingRosterTargetReason = reason;
		operatingRosterTargetSource = reason.ToString();
		operatingRosterTargetLastChangeWeek = changeWeek;
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
	
	/// <summary>
	/// Term in years. The authored ordering — the bigger the act, the shorter the deal —
	/// is leverage and is preserved; what changed is the new-signing end, which was
	/// 4-7 years. That is not what a new act signed: the norm was one to two years with
	/// the label holding the options, which this model already expresses as the label's
	/// renewal decision at expiry. Five to seven years remains reachable, but only as an
	/// established act at a label that locks its roster down.
	/// <para>
	/// The 4-7 default was also the reason contract expiry was 1.5% of all roster exits,
	/// leaving the performance drop as the only working turnover channel — and turnover
	/// is the only door onto a roster for a genre with no incumbents.
	/// </para>
	/// </summary>
	public int CalculateContractLength(SimulatedArtist artist) {
		int baseTerm = artist.careerState switch {
			CareerState.Superstar => (int)GD.RandRange(1, 3),
			CareerState.Star => (int)GD.RandRange(2, 4),
			CareerState.Established => (int)GD.RandRange(3, 5),
			CareerState.Rising => (int)GD.RandRange(2, 3),
			_ => (int)GD.RandRange(1, 2)
		};
		return Mathf.Clamp(baseTerm + GetContractTermBias(), 1, 7);
	}

	/// <summary>
	/// Terms varied by house, not just by act. A major with a loyal roster wrote long
	/// exclusive deals; a small independent cut a short one-off and let the act walk.
	/// </summary>
	private int GetContractTermBias() {
		int bias = 0;
		if (tier is LabelTier.Major or LabelTier.MidTier) bias++;
		if (artistLoyalty > 0.65f) bias++;
		if (tier is LabelTier.Small or LabelTier.Boutique) bias--;
		return bias;
	}

	/// <summary>
	/// Early-to-mid decade deals were written as a delivery commitment in sides. The
	/// obligation retires as the album deal takes over, after which a term is a term.
	/// Zero means no sides obligation and the year term governs alone.
	/// </summary>
	public int CalculateContractSinglesObligation(SimulatedArtist artist, int year) {
		if (year > SinglesObligationFinalYear) return 0;
		return artist.careerState switch {
			CareerState.Superstar or CareerState.Star or CareerState.Established => 0,
			CareerState.Rising => (int)GD.RandRange(4, 8),
			_ => (int)GD.RandRange(3, 6)
		};
	}
	public const int SinglesObligationFinalYear = 1966;
	
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
		artist.contractExpiresWeek = (ChartManager.Instance?.GetCurrentChartWeek() ?? 0) + artist.contractLength * 52;
		artist.contractSinglesObligation = CalculateContractSinglesObligation(artist, currentYear);
		artist.contractReleases = 0;

		roster.Add(artist);
		artist.careerEvents.Add($"{currentYear}: Signed to {labelName} (${advance:N0} advance, {artist.contractLength}yr" +
			(artist.contractSinglesObligation > 0 ? $", {artist.contractSinglesObligation} sides)" : ")"));
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

public enum RevenueEstimatorLane { OrphanSingle, PromoSingle, StandaloneAlbum, AlbumWithPromo, AlbumComponent }

public sealed class FormatRevenueMemory {
	// Legacy fields remain diagnostic-only for old saved state; live decisions use
	// the bounded, release-time normalized observations below.
	public float emaNetPerRelease;
	public int releasesObserved;
	public List<FormatMemoryObservation> observations = new();
}

public sealed class FormatMemoryObservation {
	public string releaseId;
	public string projectId;
	public ProjectRecordRole releaseLane;
	public RevenueEstimatorLane estimatorLane;
	public int releaseWeek;
	public float expectedNet;
	public float opportunityScale;
	public float normalizedResidual;
	public float maturityWeight;
	// -1 is the unobserved sentinel. Age zero is a valid first observation and
	// must not be reported as a replacement.
	public int lastRevisionAge = -1;
	public int revisionOrdinal;
	public bool finalized;
}
