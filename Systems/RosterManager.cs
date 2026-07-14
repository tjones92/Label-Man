using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class RosterManager : Node {
	public static RosterManager Instance { get; private set; }
	
	[ExportGroup("Configuration")]
	[Export] private float weeklyScoutChance = 0.08f;
	[Export] private float monthlyRosterReviewChance = 0.5f;
	
	[ExportGroup("Debug")]
	[Export] private bool debugMode = false;
	public int WeeklyScoutingRolls { get; private set; }
	public int WeeklySignings { get; private set; }
	private readonly Dictionary<LabelTier, RosterLifecycleFlow> weeklyLifecycleFlowByTier = new();
	private readonly HashSet<string> uniquelyResignedArtistIds = new();
	private readonly Dictionary<string, int> lastReSignWeekByArtistId = new();
	// Observational only. These dictionaries never participate in simulation decisions.
	private readonly Dictionary<string, LabelScoutingVacancyObservation> weeklyScoutingVacancyByLabelId = new(System.StringComparer.Ordinal);
	private readonly Dictionary<string, int> consecutiveVacancyWeeksByLabelId = new(System.StringComparer.Ordinal);
	private readonly Dictionary<string, int> consecutiveEmptyWeeksByLabelId = new(System.StringComparer.Ordinal);
	// Enabled-only decision state. This is intentionally separate from the
	// observational vacancy telemetry so a capture can never influence scouting.
	private readonly Dictionary<string, int> scoutingUrgencyAgeByLabelId = new(System.StringComparer.Ordinal);
	private const int ShortWindowRedropWeeks = 26;
	public const int ScoutingUrgencyThresholdWeeks = 12;
	public const float ScoutingUrgencyProbabilityFloor = 0.25f;

	public sealed class LabelScoutingVacancyObservation {
		public string LabelId { get; init; }
		public LabelTier LabelTier { get; init; }
		public int MaxRosterSize { get; init; }
		public int ScoutingRosterSize { get; init; }
		public int ScoutingUnusedRosterSlots { get; init; }
		public bool ScoutingIsEmptyRoster { get; init; }
		public float ScoutingAbility { get; init; }
		public float ScoutingRosterFullness { get; init; }
		public bool HasRecentHit { get; init; }
		public float RecentHitFactor { get; init; }
		public int DecliningArtistCount { get; init; }
		public float DecliningFactor { get; init; }
		public float EstimatedAdvance { get; init; }
		public bool CanAffordEstimatedAdvance { get; init; }
		public float ComputedScoutProbability { get; init; }
		public float? ScoutRandomRoll { get; init; }
		public bool ScoutingGatePassed { get; init; }
		public int? EligibleCandidateCount { get; set; }
		public float? BestCandidateScore { get; set; }
		public bool SigningAttempted { get; set; }
		public bool SigningSucceeded { get; set; }
		public string SigningKind { get; set; }
		public string FailureReason { get; set; }
		public int RosterSize { get; set; }
		public int UnusedRosterSlots { get; set; }
		public bool IsEmptyRoster { get; set; }
		public int ConsecutiveVacancyWeeks { get; set; }
		public int ConsecutiveEmptyWeeks { get; set; }
	}

	public readonly struct RosterLifecycleFlow {
		public readonly int DropsToPool;
		public readonly int FirstTimeSignings;
		public readonly int ReSignings;
		public readonly int UniqueReSignings;
		public readonly int ShortWindowRedrops;
		public readonly int ScoutingGatePasses;
		public readonly int SigningAttempts;
		public readonly int CandidateRejections;
		public readonly int AffordabilityRejections;
		public readonly int PerformanceDrops;
		public readonly int OtherDepartures;
		public readonly int RecentPerformanceReSignings;
		public readonly int PrematureProbationDrops;
		public readonly int NoEligibleCandidatePasses;
		public readonly int ScoreRejections;
		public RosterLifecycleFlow(int dropsToPool, int firstTimeSignings, int reSignings, int uniqueReSignings,
			int shortWindowRedrops, int scoutingGatePasses, int signingAttempts, int candidateRejections, int affordabilityRejections,
			int performanceDrops = 0, int otherDepartures = 0, int recentPerformanceReSignings = 0,
			int prematureProbationDrops = 0, int noEligibleCandidatePasses = 0, int scoreRejections = 0) {
			DropsToPool = dropsToPool;
			FirstTimeSignings = firstTimeSignings;
			ReSignings = reSignings;
			UniqueReSignings = uniqueReSignings;
			ShortWindowRedrops = shortWindowRedrops;
			ScoutingGatePasses = scoutingGatePasses;
			SigningAttempts = signingAttempts;
			CandidateRejections = candidateRejections;
			AffordabilityRejections = affordabilityRejections;
			PerformanceDrops = performanceDrops;
			OtherDepartures = otherDepartures;
			RecentPerformanceReSignings = recentPerformanceReSignings;
			PrematureProbationDrops = prematureProbationDrops;
			NoEligibleCandidatePasses = noEligibleCandidatePasses;
			ScoreRejections = scoreRejections;
		}

		public static RosterLifecycleFlow Combine(RosterLifecycleFlow left, RosterLifecycleFlow right) => new(
			left.DropsToPool + right.DropsToPool, left.FirstTimeSignings + right.FirstTimeSignings,
			left.ReSignings + right.ReSignings, left.UniqueReSignings + right.UniqueReSignings,
			left.ShortWindowRedrops + right.ShortWindowRedrops, left.ScoutingGatePasses + right.ScoutingGatePasses,
			left.SigningAttempts + right.SigningAttempts, left.CandidateRejections + right.CandidateRejections,
			left.AffordabilityRejections + right.AffordabilityRejections, left.PerformanceDrops + right.PerformanceDrops,
			left.OtherDepartures + right.OtherDepartures, left.RecentPerformanceReSignings + right.RecentPerformanceReSignings,
			left.PrematureProbationDrops + right.PrematureProbationDrops,
			left.NoEligibleCandidatePasses + right.NoEligibleCandidatePasses, left.ScoreRejections + right.ScoreRejections);
	}
	
	public override void _EnterTree() {
		if (Instance != null && Instance != this) { QueueFree(); return; }
		Instance = this;
	}

	public override void _Ready() {
		if (TimeManager.Instance != null) {
			TimeManager.Instance.OnWeekEnded += OnWeekEnded;
			TimeManager.Instance.OnMonthChanged += OnMonthChanged;
		}
	}

	public override void _ExitTree() {
		if (TimeManager.Instance != null) {
			TimeManager.Instance.OnWeekEnded -= OnWeekEnded;
			TimeManager.Instance.OnMonthChanged -= OnMonthChanged;
		}
	}
	
	public void InitializeAllRosters(List<AILabel> labels, int year) {
		GD.Print($"RosterManager: Initializing rosters for {labels.Count} labels...");
		foreach (var label in labels) {
			label.InitializeRoster();
			PopulateInitialRoster(label, year);
		}
		if (debugMode) PrintRosterStats(labels);
		GD.Print("RosterManager: Initialization complete");
	}

	public void InitializeRosterForLabel(AILabel label, int year) {
		if (label == null) return;
		label.InitializeRoster();
		PopulateInitialRoster(label, year);
	}
	
	private void PopulateInitialRoster(AILabel label, int year) {
		float fillRatio = label.tier switch {
			LabelTier.Major => (float)GD.RandRange(0.6, 0.85),
			LabelTier.MidTier => (float)GD.RandRange(0.5, 0.75),
			LabelTier.Independent => (float)GD.RandRange(0.4, 0.7),
			LabelTier.Small => (float)GD.RandRange(0.3, 0.6),
			LabelTier.Boutique => (float)GD.RandRange(0.5, 0.8),
			_ => 0.5f
		};
		
		int targetSize = Mathf.RoundToInt(label.maxRosterSize * fillRatio);
		for (int i = 0; i < targetSize; i++) {
			var artist = FindArtistForLabel(label, year);
			if (artist != null) InitialSignArtist(label, artist, year);
		}
	}
	
	private SimulatedArtist FindArtistForLabel(AILabel label, int year) {
		if (ArtistManager.Instance == null) return null;
		var candidates = new List<SimulatedArtist>();
		
		foreach (var genre in label.preferredGenres) {
			candidates.AddRange(ArtistManager.Instance.GetUnsignedByGenre(genre));
		}
		
		if (candidates.Count == 0) candidates = ArtistManager.Instance.GetUnsignedArtists();
		if (candidates.Count == 0) return null;
		
		var scored = candidates
			.Select(a => (artist: a, score: ScoreArtistForLabel(a, label)))
			.Where(x => x.score > 0)
			.OrderByDescending(x => x.score)
			.Take(10)
			.ToList();
		
		if (scored.Count == 0) return null;
		
		float totalScore = scored.Sum(s => s.score);
		float roll = (float)GD.RandRange(0f, totalScore);
		float cumulative = 0f;
		
		foreach (var (artist, score) in scored) {
			cumulative += score;
			if (roll <= cumulative) return artist;
		}
		return scored[0].artist;
	}
	
	private float ScoreArtistForLabel(SimulatedArtist artist, AILabel label) {
		float score = 0f;
		float quality = artist.CalculateBaseQuality();
		score += quality * (0.5f + label.scoutingAbility * 0.5f);
		
		if (label.preferredGenres.Contains(artist.primaryGenre)) score += 0.4f;
		else if (label.secondaryGenres != null && label.secondaryGenres.Contains(artist.primaryGenre)) score += 0.2f;
		
		if (artist.reputation < 0.1f) score *= 0.5f + (label.riskTolerance * 0.5f);
		score *= (float)GD.RandRange(0.8f, 1.2f);
		return score;
	}
	
	private void InitialSignArtist(AILabel label, SimulatedArtist artist, int year) {
		float advanceRange = label.tier switch {
			LabelTier.Major => (float)GD.RandRange(2000f, 8000f),
			LabelTier.MidTier => (float)GD.RandRange(800f, 3000f),
			LabelTier.Independent => (float)GD.RandRange(300f, 1200f),
			LabelTier.Small => (float)GD.RandRange(100f, 500f),
			LabelTier.Boutique => (float)GD.RandRange(200f, 800f),
			_ => (float)GD.RandRange(200f, 800f)
		};
		
		artist.labelId = label.labelId;
		artist.signedYear = year - (int)GD.RandRange(0, 5);
		artist.careerState = CareerState.NewSigning;
		artist.royaltyRate = label.CalculateRoyaltyRate(artist);
		artist.unrecoupedAdvance = advanceRange;
		artist.contractLength = (int)GD.RandRange(3, 6);
		artist.contractExpiresYear = year + artist.contractLength;
		artist.weeksSinceLastRelease = (int)GD.RandRange(0, 52);
		
		if (GD.Randf() < 0.3f) {
			artist.totalReleases = (int)GD.RandRange(1, 5);
			artist.weeksSinceLastRelease = (int)GD.RandRange(4, 30);
			
			if (GD.Randf() < 0.4f) {
				artist.top40Hits = (int)GD.RandRange(1, 3);
				artist.careerState = CareerState.Rising;
				artist.momentum = (float)GD.RandRange(0.1f, 0.4f);
				artist.reputation = (float)GD.RandRange(0.1f, 0.3f);
			}
			if (GD.Randf() < 0.15f) {
				artist.top10Hits = (int)GD.RandRange(1, 2);
				artist.careerState = CareerState.Established;
				artist.momentum = (float)GD.RandRange(0.2f, 0.5f);
				artist.reputation = (float)GD.RandRange(0.2f, 0.5f);
			}
		}
		
		label.roster.Add(artist);
		ArtistManager.Instance.SignArtist(artist, label.labelId, artist.signedYear);
	}
	
	private void OnWeekEnded(GameDate date) {
		ReconcileEnabledLifecycleForCurrentWeek();
		UpdateArtistCooldowns();
		WeeklyScoutingRolls = 0;
		WeeklySignings = 0;
		weeklyLifecycleFlowByTier.Clear();
		weeklyScoutingVacancyByLabelId.Clear();
		WeeklyScoutingRolls++;
		if (IsLiveGenreMarket()) {
			ProcessEnabledVacancyResponsiveScouting(date.year);
		} else if (GD.Randf() < weeklyScoutChance) {
			// Frozen disabled boundary: retain the global throttle and three-label cap.
			ProcessScouting(date.year);
		}
	}
	
	private void UpdateArtistCooldowns() {
		var labels = GetAllLabels();
		if (labels == null) return;
		foreach (var label in labels) {
			foreach (var artist in label.roster) artist.weeksSinceLastRelease++;
		}
	}
	
	private void ProcessScouting(int year) {
		var labels = GetAllLabels();
		if (labels == null) return;
		if (ArtistPopulationLifecycle.Enabled && GenreMarketV2.Enabled) {
			ProcessLegacyScoutingWithTelemetry(labels, year);
			return;
		}
		
		var scoutingLabels = labels.Where(l => l.ShouldScoutNewArtist()).OrderBy(_ => GD.Randf()).Take(3);
		foreach (var label in scoutingLabels) TrySignNewArtist(label, year);
	}

	/// <summary>
	/// The first audit tick occurs before ChartManager marks Genre Market V2 live.
	/// Preserve its legacy three-label throttle while observing each existing gate
	/// evaluation, so the enabled-only stream remains one-row-per-label-per-week.
	/// </summary>
	private void ProcessLegacyScoutingWithTelemetry(List<AILabel> labels, int year) {
		var scoutingLabels = new List<(AILabel Label, LabelScoutingVacancyObservation Observation)>();
		foreach (AILabel label in labels) {
			AILabel.ScoutingGateEvaluation gate = label.EvaluateScoutingGate();
			LabelScoutingVacancyObservation observation = CreateScoutingVacancyObservation(label, gate);
			weeklyScoutingVacancyByLabelId[label.labelId] = observation;
			if (gate.ScoutingGatePassed) scoutingLabels.Add((label, observation));
		}
		foreach ((AILabel label, LabelScoutingVacancyObservation observation) in scoutingLabels.OrderBy(_ => GD.Randf()).Take(3))
			TrySignNewArtist(label, year, observation);
	}

	private void ProcessEnabledVacancyResponsiveScouting(int year) {
		var labels = GetAllLabels();
		if (labels == null) return;
		foreach (AILabel label in labels) {
			if (!IsEligibleForEnabledScouting(label)) {
				// ChartManager retains historical/closed labels for lookup and audit history.
				// They must remain observable, but must never consume scouting RNG or
				// re-acquire artists after LabelLifecycleManager has closed them.
				AILabel.ScoutingGateEvaluation inactivePreview = label.PreviewScoutingGate();
				LabelScoutingVacancyObservation inactiveObservation = CreateScoutingVacancyObservation(label, inactivePreview);
				inactiveObservation.FailureReason = "InactiveLabel";
				weeklyScoutingVacancyByLabelId[label.labelId] = inactiveObservation;
				scoutingUrgencyAgeByLabelId.Remove(label.labelId);
				continue;
			}
			int urgencyAge = GetScoutingUrgencyAgeForWeek(label.HasRosterSpace,
				scoutingUrgencyAgeByLabelId.GetValueOrDefault(label.labelId));
			float minimumProbability = GetScoutingProbabilityFloorForPath(true, urgencyAge);
			AILabel.ScoutingGateEvaluation gate = label.EvaluateScoutingGate(minimumProbability: minimumProbability);
			LabelScoutingVacancyObservation observation = CreateScoutingVacancyObservation(label, gate);
			weeklyScoutingVacancyByLabelId[label.labelId] = observation;
			bool signingSucceeded = false;
			if (gate.ScoutingGatePassed) {
				RecordScoutingGatePass(label.tier);
				// Exactly one scouting roll and one signing attempt per qualifying label per live week.
				signingSucceeded = TrySignNewArtist(label, year, observation);
			}
			UpdateScoutingUrgencyAge(label, urgencyAge, signingSucceeded);
		}
	}

	private static LabelScoutingVacancyObservation CreateScoutingVacancyObservation(AILabel label, AILabel.ScoutingGateEvaluation gate) => new() {
		LabelId = label.labelId,
		LabelTier = label.tier,
		MaxRosterSize = gate.MaxRosterSize,
		ScoutingRosterSize = gate.RosterSize,
		ScoutingUnusedRosterSlots = Mathf.Max(0, gate.MaxRosterSize - gate.RosterSize),
		ScoutingIsEmptyRoster = gate.RosterSize == 0,
		ScoutingAbility = label.scoutingAbility,
		ScoutingRosterFullness = gate.RosterFullness,
		HasRecentHit = gate.HasRecentHit,
		RecentHitFactor = gate.RecentHitFactor,
		DecliningArtistCount = gate.DecliningArtistCount,
		DecliningFactor = gate.DecliningFactor,
		EstimatedAdvance = gate.EstimatedAdvance,
		CanAffordEstimatedAdvance = gate.FailureReason != "EstimatedAdvanceUnaffordable",
		ComputedScoutProbability = gate.ComputedScoutProbability,
		ScoutRandomRoll = gate.RandomRoll,
		ScoutingGatePassed = gate.ScoutingGatePassed,
		FailureReason = gate.FailureReason
	};
	
	private bool TrySignNewArtist(AILabel label, int year, LabelScoutingVacancyObservation observation = null) {
		var candidates = GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true
			? GetEnabledSupplyCandidates(label, year)
			: ArtistManager.Instance.GetTopUnsignedTalent(20, label.preferredGenres.FirstOrDefault());
		if (observation != null) observation.EligibleCandidateCount = candidates.Count;
		if (candidates.Count == 0) {
			if (observation != null) observation.FailureReason = "NoEligibleCandidate";
			if (IsLiveGenreMarket()) RecordNoEligibleCandidatePass(label.tier);
			return false;
		}
		
		AILabel.SigningEvaluation signingEvaluation = label.EvaluateSigning(candidates);
		var bestCandidate = signingEvaluation.BestCandidate;
		if (observation != null) observation.BestCandidateScore = signingEvaluation.BestCandidateScore;
		if (bestCandidate == null) {
			if (observation != null) observation.FailureReason = "CandidateScore";
			if (IsLiveGenreMarket()) RecordScoreRejection(label.tier);
			return false;
		}
		if (observation != null) observation.SigningAttempted = true;
		if (IsLiveGenreMarket()) RecordSigningAttempt(label.tier);
		if (label.CanAffordToSign(label.CalculateAdvanceOffer(bestCandidate))) {
			bool reSigningDroppedArtist = IsLiveGenreMarket() && bestCandidate.careerState == CareerState.Dropped;
			string signingKind = GetSigningKindForTelemetry(bestCandidate);
			float advance = label.SignArtist(bestCandidate, year);
			CompetitorManager.Instance?.RecordExpense(label, advance);
			ArtistManager.Instance.SignArtist(bestCandidate, label.labelId, year);
			WeeklySignings++;
			if (IsLiveGenreMarket()) RecordSigning(label.tier, bestCandidate, reSigningDroppedArtist);
			if (observation != null) {
				observation.SigningSucceeded = true;
				observation.SigningKind = signingKind;
				observation.FailureReason = observation.SigningKind;
			}
			if (debugMode) GD.Print($"SIGNING: {label.labelName} signs {bestCandidate.stageName} ({bestCandidate.primaryGenre})");
			return true;
		} else if (IsLiveGenreMarket()) {
			if (observation != null) observation.FailureReason = "ActualAdvanceUnaffordable";
			RecordAffordabilityRejection(label.tier);
		}
		return false;
	}

	private void FinalizeScoutingVacancyTelemetry(IEnumerable<AILabel> labels) {
		var observedLabelIds = new HashSet<string>(System.StringComparer.Ordinal);
		foreach (AILabel label in labels.Where(label => label != null)) {
			observedLabelIds.Add(label.labelId);
			if (!weeklyScoutingVacancyByLabelId.TryGetValue(label.labelId, out LabelScoutingVacancyObservation observation)) continue;
			observation.RosterSize = label.CurrentRosterSize;
			observation.UnusedRosterSlots = Mathf.Max(0, label.maxRosterSize - observation.RosterSize);
			observation.IsEmptyRoster = observation.RosterSize == 0;
			observation.ConsecutiveVacancyWeeks = AdvanceConsecutiveAge(observation.UnusedRosterSlots > 0,
				consecutiveVacancyWeeksByLabelId.GetValueOrDefault(label.labelId));
			observation.ConsecutiveEmptyWeeks = AdvanceConsecutiveAge(observation.IsEmptyRoster,
				consecutiveEmptyWeeksByLabelId.GetValueOrDefault(label.labelId));
			consecutiveVacancyWeeksByLabelId[label.labelId] = observation.ConsecutiveVacancyWeeks;
			consecutiveEmptyWeeksByLabelId[label.labelId] = observation.ConsecutiveEmptyWeeks;
		}
		foreach (string labelId in consecutiveVacancyWeeksByLabelId.Keys.Where(id => !observedLabelIds.Contains(id)).ToArray()) {
			consecutiveVacancyWeeksByLabelId.Remove(labelId);
			consecutiveEmptyWeeksByLabelId.Remove(labelId);
		}
	}

	public IReadOnlyList<LabelScoutingVacancyObservation> GetWeeklyScoutingVacancyObservations() =>
		weeklyScoutingVacancyByLabelId.Values.OrderBy(observation => observation.LabelId, System.StringComparer.Ordinal).ToArray();

	/// <summary>Finalizes observational roster state at the chart-capture boundary.</summary>
	public void FinalizeScoutingVacancyTelemetryForCapture() {
		if (!ArtistPopulationLifecycle.Enabled || !GenreMarketV2.Enabled) return;
		EnsureScoutingVacancyTelemetrySnapshot();
		List<AILabel> labels = GetAllLabels();
		if (labels != null) FinalizeScoutingVacancyTelemetry(labels);
	}

	/// <summary>
	/// ChartAuditRunner can capture an initial chart snapshot before the first
	/// Friday scouting event. This is observational only: previewing never rolls
	/// RNG and candidate enumeration is intentionally not reached.
	/// </summary>
	public void EnsureScoutingVacancyTelemetrySnapshot() {
		if (!ArtistPopulationLifecycle.Enabled || !GenreMarketV2.Enabled || weeklyScoutingVacancyByLabelId.Count > 0) return;
		List<AILabel> labels = GetAllLabels();
		if (labels == null) return;
		foreach (AILabel label in labels) {
			AILabel.ScoutingGateEvaluation preview = label.PreviewScoutingGate();
			weeklyScoutingVacancyByLabelId[label.labelId] = CreateScoutingVacancyObservation(label, preview);
		}
	}

	public static int AdvanceConsecutiveAge(bool condition, int priorAge) => condition ? priorAge + 1 : 0;
	public static bool IsEligibleForEnabledScouting(AILabel label) => label?.IsActive == true;
	public static int GetScoutingUrgencyAgeForWeek(bool hasRosterSpace, int priorAge) => hasRosterSpace ? priorAge + 1 : 0;
	public static float GetScoutingProbabilityFloorForPath(bool enabledLifecyclePath, int urgencyAge) =>
		enabledLifecyclePath && urgencyAge >= ScoutingUrgencyThresholdWeeks ? ScoutingUrgencyProbabilityFloor : 0f;
	public static int FinalizeScoutingUrgencyAge(bool hasRosterSpace, bool signingSucceeded, int urgencyAge) =>
		signingSucceeded || !hasRosterSpace ? 0 : urgencyAge;

	private void UpdateScoutingUrgencyAge(AILabel label, int urgencyAge, bool signingSucceeded) {
		int finalizedAge = FinalizeScoutingUrgencyAge(label.HasRosterSpace, signingSucceeded, urgencyAge);
		if (finalizedAge == 0) scoutingUrgencyAgeByLabelId.Remove(label.labelId);
		else scoutingUrgencyAgeByLabelId[label.labelId] = finalizedAge;
	}
	public static string GetSigningKindForTelemetry(SimulatedArtist artist) => artist?.careerState == CareerState.Dropped
		? "SignedFreeAgent" : "SignedFirstTime";

	private static List<SimulatedArtist> GetEnabledSupplyCandidates(AILabel label, int year) {
		MarketRegion region = ChartManager.Instance?.GetRegionById(label.homeRegion);
		int currentWeek = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		return ArtistManager.Instance.GetUnsignedArtists()
			.Where(artist => !ArtistPopulationLifecycle.Enabled || ArtistManager.Instance.IsEligibleForPopulationSigning(artist, currentWeek))
			.Where(artist => GenreSupplyService.IsAvailableForNewSupply(artist.primaryGenre, year))
			.OrderByDescending(artist => artist.CalculateBaseQuality() *
				GenreSupplyService.GetSupplyWeight(artist.primaryGenre, label, artist, region, year))
			.ThenBy(artist => artist.artistId, System.StringComparer.Ordinal)
			.Take(40).ToList();
	}
	
	private void OnMonthChanged(GameDate date) {
		var labels = GetAllLabels();
		if (labels == null) return;
		if (IsLiveGenreMarket()) ReconcileEnabledRosterLifecycle(labels, date.year);
		foreach (var label in labels) {
			ProcessContractExpirations(label, date.year);
			if (GD.Randf() < monthlyRosterReviewChance) ProcessRosterReview(label, date.year);
		}
	}
	
	private void ProcessContractExpirations(AILabel label, int year) {
		var expiring = label.roster.Where(a => a.contractExpiresYear <= year).ToList();
		foreach (var artist in expiring) {
			bool wantToResign = ShouldResignArtist(label, artist);
			if (wantToResign && label.CanAffordToSign(label.CalculateAdvanceOffer(artist))) {
				float newAdvance = label.CalculateAdvanceOffer(artist);
				artist.unrecoupedAdvance = newAdvance;
				artist.contractLength = label.CalculateContractLength(artist);
				artist.contractExpiresYear = year + artist.contractLength;
				artist.royaltyRate = label.CalculateRoyaltyRate(artist);
				CompetitorManager.Instance?.RecordExpense(label, newAdvance);
				artist.careerEvents.Add($"{year}: Re-signed with {label.labelName}");
				if (debugMode) GD.Print($"RE-SIGN: {label.labelName} re-signs {artist.stageName}");
			} else {
				TransitionDroppedArtist(label, artist, year, ArtistDropReason.ContractExpired);
				if (debugMode) GD.Print($"CONTRACT END: {artist.stageName} leaves {label.labelName}");
			}
		}
	}
	
	private bool ShouldResignArtist(AILabel label, SimulatedArtist artist) {
		if (artist.careerState >= CareerState.Star) return true;
		if (artist.careerState == CareerState.Rising && artist.momentum > 0.2f) return true;
		if (artist.careerState == CareerState.Established && artist.consecutiveFlops < 2) return true;
		if (artist.careerState == CareerState.Declining) return false;
		if (artist.careerState == CareerState.NewSigning && artist.totalReleases >= 2 && artist.top40Hits == 0) return false;
		return GD.Randf() < label.artistLoyalty;
	}
	
	private void ProcessRosterReview(AILabel label, int year) {
		if (IsLiveGenreMarket()) ReconcileEnabledTerminalRosterMembers(label, year);
		var toReview = label.roster.Where(a => label.ShouldDropArtist(a)).ToList();
		foreach (var artist in toReview) {
			TransitionDroppedArtist(label, artist, year, ArtistDropReason.Performance);
			if (debugMode) GD.Print($"DROPPED: {label.labelName} drops {artist.stageName} (flops: {artist.consecutiveFlops})");
		}
	}
	
	public SimulatedArtist GetArtistForRelease(AILabel label) => label.GetArtistForRelease(TimeManager.Instance?.CurrentDate.year ?? 1960);
	
	public void RecordReleased(SimulatedArtist artist, string recordId) {
		artist.weeksSinceLastRelease = 0;
		artist.totalReleases++;
		artist.releaseHistory.Add(recordId);
	}
	
	public void RecordChartRunComplete(SimulatedArtist artist, RecordRuntimeData record) {
		if (artist == null || record == null) return;
		if (record.artistChartRunCompleted) {
			if (IsLiveGenreMarket() && artist.careerState == CareerState.Dropped) {
				int completedYear = TimeManager.Instance?.CurrentDate.year ?? 1960;
				TransitionDroppedArtist(GetLabelById(artist.labelId), artist, completedYear, ArtistDropReason.Performance);
			}
			return;
		}
		artist.UpdateAfterChartRun(record.peakPosition, record.weeksOnChart, record.totalUnitsSold,
			ArtistManager.CreditsCurrentContract(record, artist));
		record.artistChartRunCompleted = true;
		var label = GetLabelById(artist.labelId);
		int year = TimeManager.Instance?.CurrentDate.year ?? 1960;
		if (IsLiveGenreMarket() && artist.careerState == CareerState.Dropped) {
			TransitionDroppedArtist(label, artist, year, ArtistDropReason.Performance);
			return;
		}
		if (label != null && label.ShouldDropArtist(artist)) {
			TransitionDroppedArtist(label, artist, year, ArtistDropReason.Performance);
		}
	}

	private static bool IsLiveGenreMarket() => GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true;

	/// <summary>
	/// Idempotent live-tick reconciliation used after chart processing as well as
	/// at the normal weekly boundary. It performs no random draws.
	/// </summary>
	public void ReconcileEnabledLifecycleForCurrentWeek() {
		if (!IsLiveGenreMarket()) return;
		var labels = GetAllLabels();
		if (labels != null) ReconcileEnabledRosterLifecycle(labels, TimeManager.Instance?.CurrentDate.year ?? 1960);
	}

	public RosterLifecycleFlow GetWeeklyLifecycleFlow(LabelTier tier) =>
		weeklyLifecycleFlowByTier.TryGetValue(tier, out RosterLifecycleFlow flow) ? flow : default;
	public RosterLifecycleFlow GetAggregateWeeklyLifecycleFlow() =>
		weeklyLifecycleFlowByTier.Values.Aggregate(default(RosterLifecycleFlow), RosterLifecycleFlow.Combine);

	private void RecordSigning(LabelTier tier, SimulatedArtist artist, bool reSigningDroppedArtist) {
		RosterLifecycleFlow current = GetWeeklyLifecycleFlow(tier);
		int uniqueReSignings = current.UniqueReSignings;
		string artistId = artist?.artistId;
		int currentWeek = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		if (reSigningDroppedArtist && uniquelyResignedArtistIds.Add(artistId)) uniqueReSignings++;
		if (reSigningDroppedArtist) lastReSignWeekByArtistId[artistId] = currentWeek;
		bool recentPerformanceReSigning = reSigningDroppedArtist && artist?.lastDropReason == ArtistDropReason.Performance &&
			artist.lastPerformanceDropWeek >= 0 && currentWeek - artist.lastPerformanceDropWeek < ArtistManager.PerformanceDropCooldownWeeks;
		weeklyLifecycleFlowByTier[tier] = new RosterLifecycleFlow(current.DropsToPool,
			current.FirstTimeSignings + (reSigningDroppedArtist ? 0 : 1), current.ReSignings + (reSigningDroppedArtist ? 1 : 0),
			uniqueReSignings, current.ShortWindowRedrops, current.ScoutingGatePasses, current.SigningAttempts,
			current.CandidateRejections, current.AffordabilityRejections, current.PerformanceDrops, current.OtherDepartures,
			current.RecentPerformanceReSignings + (recentPerformanceReSigning ? 1 : 0), current.PrematureProbationDrops,
			current.NoEligibleCandidatePasses, current.ScoreRejections);
	}

	private void RecordScoutingGatePass(LabelTier tier) => UpdateFlow(tier, flow => new RosterLifecycleFlow(flow.DropsToPool,
		flow.FirstTimeSignings, flow.ReSignings, flow.UniqueReSignings, flow.ShortWindowRedrops, flow.ScoutingGatePasses + 1,
		flow.SigningAttempts, flow.CandidateRejections, flow.AffordabilityRejections, flow.PerformanceDrops, flow.OtherDepartures,
		flow.RecentPerformanceReSignings, flow.PrematureProbationDrops, flow.NoEligibleCandidatePasses, flow.ScoreRejections));
	private void RecordSigningAttempt(LabelTier tier) => UpdateFlow(tier, flow => new RosterLifecycleFlow(flow.DropsToPool,
		flow.FirstTimeSignings, flow.ReSignings, flow.UniqueReSignings, flow.ShortWindowRedrops, flow.ScoutingGatePasses,
		flow.SigningAttempts + 1, flow.CandidateRejections, flow.AffordabilityRejections, flow.PerformanceDrops, flow.OtherDepartures,
		flow.RecentPerformanceReSignings, flow.PrematureProbationDrops, flow.NoEligibleCandidatePasses, flow.ScoreRejections));
	private void RecordNoEligibleCandidatePass(LabelTier tier) => UpdateFlow(tier, flow => new RosterLifecycleFlow(flow.DropsToPool,
		flow.FirstTimeSignings, flow.ReSignings, flow.UniqueReSignings, flow.ShortWindowRedrops, flow.ScoutingGatePasses,
		flow.SigningAttempts, flow.CandidateRejections + 1, flow.AffordabilityRejections, flow.PerformanceDrops, flow.OtherDepartures,
		flow.RecentPerformanceReSignings, flow.PrematureProbationDrops, flow.NoEligibleCandidatePasses + 1, flow.ScoreRejections));
	private void RecordScoreRejection(LabelTier tier) => UpdateFlow(tier, flow => new RosterLifecycleFlow(flow.DropsToPool,
		flow.FirstTimeSignings, flow.ReSignings, flow.UniqueReSignings, flow.ShortWindowRedrops, flow.ScoutingGatePasses,
		flow.SigningAttempts, flow.CandidateRejections + 1, flow.AffordabilityRejections, flow.PerformanceDrops, flow.OtherDepartures,
		flow.RecentPerformanceReSignings, flow.PrematureProbationDrops, flow.NoEligibleCandidatePasses, flow.ScoreRejections + 1));
	private void RecordAffordabilityRejection(LabelTier tier) => UpdateFlow(tier, flow => new RosterLifecycleFlow(flow.DropsToPool,
		flow.FirstTimeSignings, flow.ReSignings, flow.UniqueReSignings, flow.ShortWindowRedrops, flow.ScoutingGatePasses,
		flow.SigningAttempts, flow.CandidateRejections, flow.AffordabilityRejections + 1, flow.PerformanceDrops, flow.OtherDepartures,
		flow.RecentPerformanceReSignings, flow.PrematureProbationDrops, flow.NoEligibleCandidatePasses, flow.ScoreRejections));
	private void UpdateFlow(LabelTier tier, System.Func<RosterLifecycleFlow, RosterLifecycleFlow> update) =>
		weeklyLifecycleFlowByTier[tier] = update(GetWeeklyLifecycleFlow(tier));

	private void TransitionDroppedArtist(AILabel label, SimulatedArtist artist, int year, ArtistDropReason reason) {
		if (IsLiveGenreMarket()) {
			bool prematureProbationDrop = ArtistPopulationLifecycle.Enabled && reason == ArtistDropReason.Performance &&
				artist?.careerState == CareerState.NewSigning && artist.contractConsecutiveFlops < 2;
			if (ArtistManager.Instance?.DropArtist(artist, year, label, reason) == true && label != null) {
				RosterLifecycleFlow current = GetWeeklyLifecycleFlow(label.tier);
				int currentWeek = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
				int shortWindowRedrops = current.ShortWindowRedrops;
				if (!string.IsNullOrEmpty(artist.artistId) && lastReSignWeekByArtistId.TryGetValue(artist.artistId, out int reSignWeek) &&
					currentWeek >= reSignWeek && currentWeek - reSignWeek <= ShortWindowRedropWeeks) shortWindowRedrops++;
				weeklyLifecycleFlowByTier[label.tier] = new RosterLifecycleFlow(current.DropsToPool + 1,
					current.FirstTimeSignings, current.ReSignings, current.UniqueReSignings, shortWindowRedrops,
					current.ScoutingGatePasses, current.SigningAttempts, current.CandidateRejections, current.AffordabilityRejections,
					current.PerformanceDrops + (reason == ArtistDropReason.Performance ? 1 : 0),
					current.OtherDepartures + (reason == ArtistDropReason.Performance ? 0 : 1), current.RecentPerformanceReSignings,
					current.PrematureProbationDrops + (prematureProbationDrop ? 1 : 0),
					current.NoEligibleCandidatePasses, current.ScoreRejections);
			}
		} else {
			label?.DropArtist(artist, year, reason.ToString());
			ArtistManager.Instance?.DropArtist(artist, year);
		}
	}

	/// <summary>
	/// Label shutdown is an enabled lifecycle departure, not a performance
	/// failure. Route it through the atomic owner/pool seam so an artist cannot
	/// inherit an earlier performance cooldown and closure flow stays auditable.
	/// </summary>
	public void HandleLabelClosure(AILabel label, SimulatedArtist artist, int year) =>
		TransitionDroppedArtist(label, artist, year, ArtistDropReason.LabelClosure);

	private void ReconcileEnabledRosterLifecycle(IEnumerable<AILabel> labels, int year) {
		AILabel[] allLabels = labels.Where(label => label != null).OrderBy(label => label.labelId, System.StringComparer.Ordinal).ToArray();
		foreach (AILabel label in allLabels) ReconcileEnabledTerminalRosterMembers(label, year);
		var memberships = new Dictionary<SimulatedArtist, List<AILabel>>();
		foreach (AILabel label in allLabels) {
			foreach (SimulatedArtist artist in label.roster.Where(artist => artist != null).Distinct().ToArray()) {
				if (!memberships.TryGetValue(artist, out List<AILabel> owners)) memberships[artist] = owners = new List<AILabel>();
				owners.Add(label);
			}
		}
		foreach ((SimulatedArtist artist, List<AILabel> owners) in memberships) {
			AILabel owner = !string.IsNullOrEmpty(artist.labelId)
				? allLabels.FirstOrDefault(label => label.labelId == artist.labelId)
				: null;
			if (owner == null) {
				foreach (AILabel roster in owners) roster.roster.RemoveAll(candidate => candidate == artist);
				continue;
			}
			foreach (AILabel roster in owners) roster.roster.RemoveAll(candidate => candidate == artist);
			owner.roster.Add(artist);
		}
		ArtistManager.Instance?.ReconcileEnabledUnsignedPool();
	}

	private void ReconcileEnabledTerminalRosterMembers(AILabel label, int year) {
		if (label?.roster == null) return;
		foreach (SimulatedArtist artist in label.roster.Where(artist => artist != null &&
			GenreSupplyService.IsTerminalCareerState(artist.careerState)).ToList()) {
			if (artist.careerState == CareerState.Dropped) {
				TransitionDroppedArtist(label, artist, year, ArtistDropReason.LifecycleReconciliation);
			} else {
				label.roster.RemoveAll(candidate => candidate == artist);
				if (artist.labelId == label.labelId) artist.labelId = null;
			}
		}
	}
	
	private List<AILabel> GetAllLabels() => ChartManager.Instance?.GetAllLabels();
	private AILabel GetLabelById(string labelId) => ChartManager.Instance?.GetLabelById(labelId);
	
	private void PrintRosterStats(List<AILabel> labels) {
		GD.Print("=== ROSTER STATS ===");
		int totalArtists = labels.Sum(l => l.roster.Count);
		int totalCapacity = labels.Sum(l => l.maxRosterSize);
		GD.Print($"Total Artists Signed: {totalArtists} / {totalCapacity} capacity ({100f * totalArtists / totalCapacity:F1}%)");
		
		var byTier = labels.GroupBy(l => l.tier);
		foreach (var group in byTier) {
			int signed = group.Sum(l => l.roster.Count);
			int capacity = group.Sum(l => l.maxRosterSize);
			GD.Print($"  {group.Key}: {signed} / {capacity}");
		}
	}
	
	public void DebugPrintRosterStats() {
		var labels = GetAllLabels();
		if (labels != null) PrintRosterStats(labels);
	}
}
