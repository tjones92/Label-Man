using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class CompetitorManager : Node {
	public static CompetitorManager Instance { get; private set; }
	public const float DealReinvestRate = 0.02f;
	public const float DealReinvestCost = 5000000f;
	public const float DealDependencyLow = 0.35f;
	public const float DealDependencyHigh = 0.56f;
	
	[ExportGroup("Configuration")]
	[Export] private int targetActiveRecords = 500;
	[Export] private int historicalRecordsCount = 150;
	
	[ExportGroup("Economic Settings")]
	[Export] private float baseRoyaltyRate = 0.04f;
	[Export] private Godot.Collections.Dictionary<string, float> pressingCostPerUnitByFormat = new() {
		{ nameof(ReleaseFormat.Single), 0.30f },
		{ nameof(ReleaseFormat.Album), 0.95f },
		{ nameof(ReleaseFormat.EP), 0.55f }
	};
	[Export] private Godot.Collections.Dictionary<string, float> pricePerUnitByFormat = new() {
		{ nameof(ReleaseFormat.Single), 0.89f },
		{ "Album", 3.98f }
	};
	[Export] private float bankruptcyThreshold = 200f;
	[Export] private float monthlyOverheadRate = 0.02f;
	[Export] private bool enableBankruptcy = true;
	[Export] private bool enableAlbums = true;
	[Export(PropertyHint.Range, "0,1,0.01")] private float albumPackagingCostPerUnit = 0.22f;
	[Export] private float albumPackagingFixedCost = 1500f;
	[Export(PropertyHint.Range, "0,2,0.05")] private float compilationProductionMultiplier = 0.60f;
	[Export(PropertyHint.Range, "0,1,0.01")] private float revenueMemoryAlpha = 0.30f;
	[Export(PropertyHint.Range, "0.1,20,0.1")] private float revenueMemoryConfidenceK = 4.0f;
	private const int ResponsiveMemoryMaximumHistoryWeeks = 104;
	private const float ResponsiveMemoryHalfLifeWeeks = 52f;
	private const float ResponsiveMemoryMaximumConfidence = .65f;
	private const float ResponsiveMemoryResidualLimit = 3f;
	private const int MaximumAlbumProjectsPerArtistYear = 2;
	private const int AlbumProjectPressureMinimumDecisions = 100;
	private const float AlbumProjectPressureShare = 0.75f;
	private const float PromoProjectEligibilityWeight = 0.75f;
	// The live route no longer needs an eligibility thumb. It compensated for a
	// binary fork that discarded every near-miss Album, so it had to be biased up
	// to keep crossover projects alive. The probabilistic chooser now carries that
	// behavior explicitly and count-neutrally, leaving this as an unearned live-only
	// project-count uplift. The scale stays plumbed through ResolveAlbumDecision so
	// the seam remains explicit, bounded, and independently probeable.
	private const float LiveAlbumDecisionEligibilityScale = 1f;
	private const float FormatChoiceExplorationFloor = 0.02f;
	private const float FormatChoiceLogitSlope = 10f;
	// The live revision model observes high-variance, annualized outcomes and
	// therefore needs more evidence than the frozen retirement-time EMA. Keeping
	// this separate also preserves the disabled route's legacy K=4 behavior.
	private const float ResponsiveMemoryConfidenceK = 12f;
	// Ceiling retained from the former Major-tier coefficient so a fully-capable
	// label lands where majors were already calibrated; the reference roster depth is
	// the point past which a catalogue can keep an LP program fed on its own.
	private const float AlbumPortfolioCommitmentCeiling = 1.50f;
	private const float AlbumProgramRosterDepth = 12f;
	private const float AlbumProgramReachWeight = .55f;
	// Calibrated against measured 1967 means (overlap .485, awareness headroom .617)
	// so recruited units run about .64 of diverted units per unit of shared exposure.
	// The promo Single stays net-positive on its own margin while remaining mildly
	// dilutive per unit — matching a decade in which promo Singles were the primary
	// LP marketing vehicle rather than a strategy that dies at the LP takeover.
	private const float PromoAlbumConversionK = .50f;
	// A promo Single recruits Album buyers partly on novelty and partly on being the
	// Album's advertisement. Gating recruitment purely on awareness headroom made the
	// second effect vanish exactly where it was strongest: a famous act's hit Single
	// still sold enormous LP volume in 1967-69. Measured headroom falls .90 -> .44
	// across 1966-68 while shelf overlap rises, and that asymmetry — not the level —
	// is what re-opened the crossover a year later.
	private const float PromoAwarenessConversionFloor = .25f;
	private const float AlbumPriorEarlyEraDiscount = .78f;
	private const int AlbumPriorCalibrationBootstrapYear = 1960;
	private const int AlbumPriorCalibrationRetiredYear = 1964;
	[Export] private float priorUnitScalarAlbum = 175000f;
	[Export] private float priorCompHitUnitScalar = 20000f;
	[Export(PropertyHint.Range, "0,1,0.01")] private float hitRecencyDecay = 0.75f;
	[Export(PropertyHint.Range, "0,1,0.01")] private float priorAssumedAlbumPackaging = 0.50f;
	[Export(PropertyHint.Range, "1958,1965,0.1")] private float albumEraWeightStartYear = 1960f;
	[Export(PropertyHint.Range, "1965,1972,0.1")] private float albumEraWeightEndYear = 1968f;
	[Export(PropertyHint.Range, "1960,1967,0.1")] private float albumCohesionRiseStartYear = 1964f;
	[Export(PropertyHint.Range, "1965,1972,0.1")] private float albumCohesionRiseEndYear = 1968f;

	[ExportGroup("Album Project Pipeline")]
	[Export(PropertyHint.Range, "1,12,1")] private int albumDropGapWeeksMin = 3;
	[Export(PropertyHint.Range, "1,12,1")] private int albumDropGapWeeksMax = 5;
	[Export(PropertyHint.Range, "1,100,1")] private int promoFlopThreshold = 80;
	[Export(PropertyHint.Range, "0,1,0.01")] private float promoAwarenessBonusMax = 0.25f;
	[Export(PropertyHint.Range, "0,2,0.01")] private float promoStockBonusMax = 0.80f;
	[Export(PropertyHint.Range, "0,1,0.01")] private float promoStockFlopFloor = 0.85f;
	[Export(PropertyHint.Range, "0,1,0.01")] private float cannibalizationStrength = 0.15f;
	[Export] private float singleNetMarginPerUnit = 0.40f;
	[Export] private float substitutionK = 1.00f;
	// Substitution saturates — a large part of the 45 market never converts to LPs at
	// any level of album dominance — but the binding cap is deliberately loose. Once
	// the absolute promo veto was removed, a declining promo advantage produces the
	// control's gradual .66 -> .44 slide instead of a collapse, so cannibalization is
	// allowed to keep growing. Capping it hard instead (.35) left promo unopposed and
	// overshot the unit ceiling to 1.38 at 1967.
	[Export(PropertyHint.Range, "0,1,0.01")] private float substitutionCap = 0.60f;
	[Export] private float expectedPromoLiftScalar = 10000f;
	[Export] private float expectedOverlapWeeks = 10f;
	// Flattened row-major because Godot does not export multidimensional arrays.
	// Rows are quality quartiles; columns are career bands.
	[Export] private float[] expectedPeakScoreByBucket = {
		0.008805f, 0.042022f, 0.177743f, 0.042022f,
		0.025389f, 0.118921f, 0.177743f, 0.177743f,
		0.056773f, 0.241402f, 0.405063f, 0.405063f,
		0.178960f, 0.505133f, 0.739346f, 0.739346f
	};

	[ExportGroup("Distribution Deals")]
	[Export(PropertyHint.Range, "0,1,0.001")] private float monthlyPullOfferProbability = 0.12f;
	[Export(PropertyHint.Range, "0,1,0.001")] private float monthlyPushOfferProbability = 0.04f;
	[Export(PropertyHint.Range, "0,1,0.001")] private float annualPost1966PushRamp = 0.05f;
	[Export(PropertyHint.Range, "0,1,0.01")] private float pushMastersOwnershipRate = 0.80f;
	[Export(PropertyHint.Range, "0,0.5,0.01")] private float pullMarginSkimMin = 0.15f;
	[Export(PropertyHint.Range, "0,0.5,0.01")] private float pullMarginSkimMax = 0.25f;
	[Export(PropertyHint.Range, "0,0.5,0.01")] private float pushMarginSkimMin = 0.20f;
	[Export(PropertyHint.Range, "0,0.5,0.01")] private float pushMarginSkimMax = 0.35f;
	[Export(PropertyHint.Range, "0,1,0.01")] private float dealReinvestRate = DealReinvestRate;
	[Export] private float dealReinvestCost = DealReinvestCost;
	[Export(PropertyHint.Range, "0,1,0.01")] private float dealDependencyLow = DealDependencyLow;
	[Export(PropertyHint.Range, "0,1,0.01")] private float dealDependencyHigh = DealDependencyHigh;
	
	[ExportGroup("Historical Records")]
	[Export] private Record[] historicalRecords;
	
	[ExportGroup("Debug")]
	[Export] private bool debugMode = false;
	
	private int generatedRecordCounter = 0;
	private long generatedProjectCounter;
	private int pipelineWeek;
	private readonly List<AlbumProject> albumProjects = new();
	private readonly List<AlbumProject> pendingAlbumProjects = new();
	private readonly Dictionary<string, AlbumProject> projectById = new();
	private readonly Dictionary<string, AlbumProject> projectByRecordId = new();
	private readonly Dictionary<(string ArtistId, int Year), int> annualAlbumProjectsByArtist = new();
	private int annualFormatCapacityYear = int.MinValue;
	private int annualFormatDecisions;
	private int annualAlbumProjectsScheduled;
	// Album-with-promo performance contains a market-wide structural component.
	// Pool it for shrinkage, then let each label refine that baseline locally.
	private readonly FormatRevenueMemory pooledAlbumWithPromoMemory = new();
	private Dictionary<string, List<string>> labelActiveRecords = new Dictionary<string, List<string>>();
	private Dictionary<string, LabelFinancialHistory> labelFinancials = new Dictionary<string, LabelFinancialHistory>();
	private readonly Dictionary<string, Dictionary<Genre, int>> annualGenreSupplyByLabel = new();
	private readonly Dictionary<Genre, int> annualGenreSupplyGlobal = new();
	private int genreSupplyYear = int.MinValue;
	
	private List<AILabel> aiLabels;
	private bool distributionOfferProcessingEnabled = true;
	private readonly Dictionary<(string LabelId, ReleaseFormat Format), RevenueTelemetry> weeklyRevenueByLabelAndFormat = new();
	private int lastBookedSettlementId;
	public int DistributorCollapseCount { get; private set; }
	public int WeeklyReleaseRollsFired { get; private set; }
	public int WeeklySuccessfulReleases { get; private set; }
	public int WeeklyFailedReleaseRolls { get; private set; }
	public int WeeklyCooldownMismatchRolls { get; private set; }
	public int WeeklyPipelineAlbumDrops { get; private set; }
	public int WeeklySingleReleases { get; private set; }
	public int WeeklyAlbumProjectsScheduled { get; private set; }
	public float WeeklyProductionSpend { get; private set; }
	public int WeeklyProductionEvents { get; private set; }
	public float WeeklyMarketingSpend { get; private set; }
	public int WeeklyMarketingEvents { get; private set; }
	private readonly Dictionary<LabelTier, ReleaseLifecycleFlow> weeklyReleaseLifecycleByTier = new();

	public readonly struct ReleaseLifecycleFlow {
		public readonly int Attempts;
		public readonly int SuccessfulReleases;
		public readonly int ArtistSelectionFailures;
		public ReleaseLifecycleFlow(int attempts, int successfulReleases, int artistSelectionFailures) {
			Attempts = attempts;
			SuccessfulReleases = successfulReleases;
			ArtistSelectionFailures = artistSelectionFailures;
		}
	}
	public int DistributionOffersGenerated { get; private set; }
	public int DistributionOffersAccepted { get; private set; }
	public float CannibalizationStrength => cannibalizationStrength;
	public float CalculateSubstitutionPropensity(Genre genre, int year) =>
		Mathf.Clamp(substitutionK * CalculateAlbumDemandFactor(genre, year), 0f, substitutionCap);
	private bool lastReleaseAttemptFailedArtistSelection;
	public event System.Action<DistributionDealTelemetry> OnDistributionDealEvent;
	public event System.Action<ReleaseStrategyTelemetry> OnReleaseStrategy;
	public event System.Action<CalibrationDecisionTelemetry> OnCalibrationDecision;
	public event System.Action<ReleaseOutcomeTelemetry> OnReleaseOutcome;
	public event System.Action<FormatMemoryRevisionTelemetry> OnFormatMemoryRevision;
	public event System.Action<SupplySelectionTelemetry> OnSupplySelection;
	
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
	
	public void Initialize(List<AILabel> labels) {
		AlbumModel.EraWeightStartYear = albumEraWeightStartYear;
		AlbumModel.EraWeightEndYear = albumEraWeightEndYear;
		AlbumModel.CohesionRiseStartYear = albumCohesionRiseStartYear;
		AlbumModel.CohesionRiseEndYear = albumCohesionRiseEndYear;
		aiLabels = labels;
		foreach (var label in aiLabels) {
			labelActiveRecords[label.labelId] = new List<string>();
			labelFinancials[label.labelId] = new LabelFinancialHistory();
		}
		PopulateInitialRecords();
		GD.Print($"CompetitorManager: Initialized with {aiLabels.Count} labels");
	}
	
	private void PopulateInitialRecords() {
		int year = TimeManager.Instance?.CurrentDate.year ?? 1960;
		
		if (historicalRecords != null) {
			foreach (var record in historicalRecords) {
				if (record.releaseDate <= TimeManager.Instance.CurrentDate) {
					ChartManager.Instance.ReleaseRecord(record);
				}
			}
		}
		
		int historicalCount = historicalRecords?.Count(r => r.releaseDate <= TimeManager.Instance.CurrentDate) ?? 0;
		int needed = targetActiveRecords - historicalCount;
		
		var releaseQuotas = CalculateInitialQuotas(needed);
		
		foreach (var label in aiLabels) {
			if (!releaseQuotas.TryGetValue(label.labelId, out int quota)) continue;
			for (int i = 0; i < quota; i++) {
				if (label.roster.Count == 0) continue;
				var artist = label.roster[(int)GD.RandRange(0, label.roster.Count - 1)];
				var record = GenerateRecordFromArtist(label, artist, year);
				int weeksAgo = (int)GD.RandRange(1, 20);
				record.releaseDate = TimeManager.Instance.CurrentDate.SubtractWeeks(weeksAgo);
				ChartManager.Instance.ReleaseRecord(record);
				BootstrapPrewarmRecord(record, artist, label, weeksAgo);
				TrackRelease(label.labelId, record.recordId);
				artist.totalReleases++;
				artist.weeksSinceLastRelease = weeksAgo;
				artist.releaseHistory.Add(record.recordId);
				artist.releasedSingleIds.Add(record.recordId);
			}
		}
		if (debugMode) GD.Print($"CompetitorManager: Populated {needed} initial records from rosters");
	}
	
	private Dictionary<string, int> CalculateInitialQuotas(int totalNeeded) {
		var quotas = new Dictionary<string, int>();
		float totalWeight = aiLabels.Sum(l => GetTierWeight(l.tier) * l.roster.Count);
		if (totalWeight <= 0) {
			int perLabel = totalNeeded / Mathf.Max(1, aiLabels.Count);
			foreach (var label in aiLabels) quotas[label.labelId] = perLabel;
			return quotas;
		}
		foreach (var label in aiLabels) {
			float weight = GetTierWeight(label.tier) * label.roster.Count;
			int quota = Mathf.RoundToInt((weight / totalWeight) * totalNeeded);
			quota = Mathf.Min(quota, label.roster.Count * 3);
			quotas[label.labelId] = quota;
		}
		return quotas;
	}
	
	private float GetTierWeight(LabelTier tier) => tier switch {
		LabelTier.Major => 5f, LabelTier.MidTier => 3f, LabelTier.Independent => 2f,
		LabelTier.Small => 1f, LabelTier.Boutique => 1.5f, _ => 1f
	};
	
	private void BootstrapPrewarmRecord(Record record, SimulatedArtist artist, AILabel label, int weeksOld) {
		var runtimeData = ChartManager.Instance.GetRecordRuntimeData(record.recordId);
		if (runtimeData == null) return;
		
		float quality = runtimeData.GetQuality();
		float ageFactor = Mathf.Pow(0.92f, weeksOld);
		
		float campaignImpact = ChartSimulator.GetCampaignImpact(label);
		runtimeData.awareness = Mathf.Clamp(0.15f + (artist.reputation * 0.3f) + (artist.momentum * 0.2f) + (campaignImpact * 0.2f * ageFactor), 0f, 1f);
		runtimeData.radioHeat = Mathf.Clamp((quality * 0.4f + campaignImpact * 0.3f) * ageFactor, 0f, 1f);
		
		if (quality > 0.7f && GD.Randf() < 0.4f) {
			runtimeData.weeksOnChart = (int)GD.RandRange(2, weeksOld);
			runtimeData.peakPosition = (int)GD.RandRange(10, 60);
		} else if (quality > 0.5f && GD.Randf() < 0.3f) {
			runtimeData.weeksOnChart = (int)GD.RandRange(1, weeksOld / 2);
			runtimeData.peakPosition = (int)GD.RandRange(40, 90);
		}
		
		var regions = ChartManager.Instance.GetAllRegions();
		foreach (var region in regions) {
			if (!runtimeData.regionalData.ContainsKey(region.regionId)) {
				runtimeData.regionalData[region.regionId] = new RegionalRecordData(region.regionId);
			}
			var regionalData = runtimeData.regionalData[region.regionId];
			bool isStrongRegion = label.strongRegions?.Contains(region.regionId) ?? false;
			float regionMod = isStrongRegion ? 1.4f : 1f;
			
			regionalData.awareness = runtimeData.awareness * regionMod * (float)GD.RandRange(0.7, 1.1);
			regionalData.radioPlay = runtimeData.radioHeat * regionMod * (float)GD.RandRange(0.6, 1.0);
			regionalData.sentiment = 0.5f + (quality * 0.3f) + (float)GD.RandRange(-0.1, 0.15);
			regionalData.unitsInStores = (int)GD.RandRange(5000, 20000);
			regionalData.unitsSoldTotal = (int)GD.RandRange(1000, 10000);
		}
	}
	
	private void OnWeekEnded(GameDate date) {
		if (historicalRecords != null) {
			foreach (var record in historicalRecords) {
				if (record.releaseDate == date) {
					ChartManager.Instance.ReleaseRecord(record);
					GD.Print($"Historical release: {record.title} by {record.artistName}");
				}
			}
		}
		// The disabled route is a byte-frozen compatibility boundary. It retains
		// the historical prior-week booking timing and never consumes settlement
		// state, spillover, or responsive-memory observations.
		if (!GenreMarketV2.Enabled) ProcessWeeklyRevenue(CreateLegacyRevenueSettlement(date));
		pipelineWeek++;
		ResetWeeklyReleaseCounters();
		ProcessDueAlbumProjects(date);
		ProcessWeeklyReleases(date);
	}

	/// <summary>Explicit, ordered booking transition for a frozen live settlement.</summary>
	public void BookCompletedWeekSettlement(ChartManager.CompletedWeekSettlement settlement) {
		if (settlement == null || !GenreMarketV2.Enabled || ChartManager.Instance?.IsGenreMarketV2Live != true)
			throw new System.InvalidOperationException("Attempted to book a non-live settlement.");
		if (settlement.IsBooked || settlement.SettlementId <= 0 || settlement.SettlementId != lastBookedSettlementId + 1)
			throw new System.InvalidOperationException($"Rejected duplicate, stale, skipped, or out-of-order settlement {settlement.SettlementId} after {lastBookedSettlementId}.");
		ProcessWeeklyRevenue(settlement);
		var projectMemoryUpdates = new HashSet<string>(System.StringComparer.Ordinal);
		foreach (ChartManager.CompletedWeekSettlementEntry entry in settlement.Entries) {
			RecordRuntimeData record = entry.Record;
			if (record?.revenueMemoryEligible != true) continue;
			int age = record.weeksSinceRelease;
			if (age == 13 || age == 26 || (record.baseRecord.format == ReleaseFormat.Album && age == 52))
				UpdateApplicableResponsiveMemoryObservations(record, finalized: false);
			if (!string.IsNullOrEmpty(record.albumProjectId) &&
				record.projectRole is ProjectRecordRole.PromoSingle or ProjectRecordRole.LinkedAlbum &&
				(age == 13 || age == 26 || (record.baseRecord.format == ReleaseFormat.Album && age == 52)))
				projectMemoryUpdates.Add(record.albumProjectId);
		}
		foreach (string projectId in projectMemoryUpdates) if (projectById.TryGetValue(projectId, out AlbumProject project))
			UpdateAlbumWithPromoProjectMemoryObservation(project, finalized: false);
		settlement.IsBooked = true;
		lastBookedSettlementId = settlement.SettlementId;
	}

	private ChartManager.CompletedWeekSettlement CreateLegacyRevenueSettlement(GameDate date) => new() {
		SettlementId = -1,
		Date = date,
		Entries = ChartManager.Instance?.GetAllRecords().Select(record => new ChartManager.CompletedWeekSettlementEntry {
			Record = record, RecordId = record.baseRecord.recordId, LabelId = record.baseRecord.labelId,
			Format = record.baseRecord.format, Units = record.unitsThisWeek
		}).ToArray() ?? System.Array.Empty<ChartManager.CompletedWeekSettlementEntry>()
	};
	
	private void ProcessWeeklyRevenue(ChartManager.CompletedWeekSettlement settlement) {
		weeklyRevenueByLabelAndFormat.Clear();
		foreach (var label in aiLabels) {
			label.weeklyGrossRevenue = 0f;
			label.weeklyCogs = 0f;
			label.weeklyDistributionSkim = 0f;
			label.weeklyArtistRoyalty = 0f;
			label.weeklyNetRevenue = 0f;
			label.weeklyDistributionIncome = 0f;
		}
		foreach (var label in aiLabels) {
			// A frozen enabled settlement may still contain a record from a label whose
			// lifecycle status changed earlier in the week. It remains an economic fact
			// and must be booked exactly once before retirement. The legacy route keeps
			// its historical inactive-label exclusion for byte-identical replay.
			if (!label.IsActive && settlement.SettlementId < 1) continue;
			float weeklyRevenue = CalculateLabelRevenue(label, settlement);
			label.cashReserves += weeklyRevenue;
			label.monthlyRevenue += weeklyRevenue;
			if (labelFinancials.TryGetValue(label.labelId, out var financials)) {
				financials.lastMonthRevenue += weeklyRevenue;
			}
		}
	}
	
	private float CalculateLabelRevenue(AILabel label, ChartManager.CompletedWeekSettlement settlement) {
		if (settlement?.Entries == null) return 0f;
		long profileStart = SimulationPerformanceProfiler.Begin();
		float totalRevenue = 0f;
		
		foreach (ChartManager.CompletedWeekSettlementEntry entry in settlement.Entries.Where(entry => entry.LabelId == label.labelId)) {
			long lookupProfileStart = SimulationPerformanceProfiler.Begin();
			var runtimeData = entry.Record;
			SimulationPerformanceProfiler.EndRecordLookup(lookupProfileStart);
			if (runtimeData == null) continue;
			
			float weeklyUnits = entry.Units;
			ReleaseFormat format = runtimeData.baseRecord.format;
			float pricePerUnit = GetPricePerUnit(format);
			float pressingCost = GetPressingCostPerUnit(format);
			if (format == ReleaseFormat.Album) pressingCost += albumPackagingCostPerUnit * (runtimeData.baseRecord.album?.packaging ?? 0f);
			var artist = ArtistManager.Instance?.GetArtist(runtimeData.baseRecord.artistId);
			float artistRoyalty = artist?.royaltyRate ?? 0.05f;
			float skimFraction = GetSettlementDistributionSkimFraction(label, runtimeData, weeklyUnits,
				liveSettlement: GenreMarketV2.Enabled && settlement.SettlementId > 0);
			float retailGross = weeklyUnits * pricePerUnit;
			// DISTANCE-4B: 4a sums by current-region hub with a neutral cost factor; 4b
			// makes manufacturing margin, skim basis, and artist recoupment region-weighted.
			float cogs = CalculateRegionalCogs(label, runtimeData, pressingCost);
			float grossAfterCogs = Mathf.Max(0f, retailGross - cogs);
			float skimAmount = grossAfterCogs * skimFraction;
			// Keep the existing artist contract convention (royalty on retail). The
			// distribution skim is based on revenue after manufacturing cost.
			float artistPayment = retailGross * artistRoyalty;
			float recordRevenue = grossAfterCogs - skimAmount - artistPayment;
			entry.Gross = retailGross;
			entry.ManufacturingCost = cogs;
			entry.ArtistRoyalty = artistPayment;
			entry.DistributionSkim = skimAmount;
			entry.LabelNet = recordRevenue;
			entry.MarketNet = recordRevenue;
			entry.DistributionIncome = 0f;
			entry.DistributionRecipientLabelId = string.Empty;
			entry.BookedCount = 1;
			runtimeData.lifetimeLabelNet += recordRevenue;
			totalRevenue += recordRevenue;
			label.weeklyGrossRevenue += retailGross;
			label.weeklyCogs += cogs;
			label.weeklyDistributionSkim += skimAmount;
			label.weeklyArtistRoyalty += artistPayment;
			label.weeklyNetRevenue += recordRevenue;
			RevenueTelemetry formatRevenue = GetOrCreateRevenueTelemetry(label.labelId, format);
			formatRevenue.gross += retailGross;
			formatRevenue.cogs += cogs;
			formatRevenue.distributionSkim += skimAmount;
			formatRevenue.artistRoyalty += artistPayment;
			formatRevenue.labelNet += recordRevenue;
			RouteDistributionSkim(label, skimAmount, format, entry);
			
			if (artist != null) {
				float recouped = Mathf.Min(Mathf.Max(0f, artist.unrecoupedAdvance), artistPayment);
				artist.unrecoupedAdvance = Mathf.Max(0f, artist.unrecoupedAdvance - recouped);
				artist.totalRoyaltyEarnings += artistPayment - recouped;
			}
			
		}
		
		SimulationPerformanceProfiler.EndCalculateLabelRevenue(profileStart);
		return totalRevenue;
	}

	/// <summary>
	/// A distribution contract may skim only the regions it actually grants.
	/// Applying its margin to the client's owned or otherwise ungranted sales
	/// transfers unrelated label revenue to the distributor. The frozen no-deal
	/// route retains its established owned-reach formula.
	/// </summary>
	private static float GetSettlementDistributionSkimFraction(AILabel label,
		RecordRuntimeData runtimeData, float weeklyUnits, bool liveSettlement) {
		if (!liveSettlement) return GetLegacyDistributionSkimFraction(label);
		IEnumerable<KeyValuePair<string, int>> regionalUnits = runtimeData?.regionalData?
			.Select(pair => new KeyValuePair<string, int>(pair.Key,
				Mathf.Max(0, pair.Value?.unitsSoldThisWeek ?? 0)));
		return GetSettlementDistributionSkimFraction(label, regionalUnits, weeklyUnits);
	}

	private static float GetLegacyDistributionSkimFraction(AILabel label) =>
		label?.activeDeal != null
			? Mathf.Clamp(label.activeDeal.marginSkim, 0f, 1f)
			: 0.25f * (1f - (label?.ownedReach ?? 0f));

	private static float GetSettlementDistributionSkimFraction(AILabel label,
		IEnumerable<KeyValuePair<string, int>> regionalUnits, float weeklyUnits) {
		if (label?.activeDeal == null) return 0.25f * (1f - Mathf.Clamp(label?.ownedReach ?? 0f, 0f, 1f));
		if (weeklyUnits <= 0f || regionalUnits == null) return 0f;
		var granted = new HashSet<string>(label.activeDeal.grantedRegions ?? System.Array.Empty<string>(),
			System.StringComparer.Ordinal);
		int grantedUnits = regionalUnits
			.Where(pair => granted.Contains(pair.Key))
			.Sum(pair => Mathf.Max(0, pair.Value));
		float grantedShare = Mathf.Clamp(grantedUnits / weeklyUnits, 0f, 1f);
		return Mathf.Clamp(label.activeDeal.marginSkim, 0f, 1f) * grantedShare;
	}

	internal static float GetSettlementDistributionSkimFractionForProbe(AILabel label,
		IReadOnlyDictionary<string, int> regionalUnits, int totalUnits, bool liveSettlement = true) =>
		liveSettlement
			? GetSettlementDistributionSkimFraction(label, regionalUnits, totalUnits)
			: GetLegacyDistributionSkimFraction(label);

	private float CalculateRegionalCogs(AILabel label, RecordRuntimeData runtimeData, float pressingCost) {
		if (runtimeData.regionalData == null || runtimeData.regionalData.Count == 0) {
			return runtimeData.unitsThisWeek * pressingCost * DistanceModel.GetDistributionCostFactor(label, label.headquartersCity);
		}

		float cogs = 0f;
		int regionalUnits = 0;
		foreach (var pair in runtimeData.regionalData) {
			int units = pair.Value?.unitsSoldThisWeek ?? 0;
			if (units <= 0) continue;
			regionalUnits += units;
			string regionHubCity = DistanceModel.GetHubCityIdForRegion(pair.Key);
			cogs += units * pressingCost * DistanceModel.GetDistributionCostFactor(label, regionHubCity);
		}

		int unassignedUnits = runtimeData.unitsThisWeek - regionalUnits;
		if (unassignedUnits != 0) {
			cogs += unassignedUnits * pressingCost * DistanceModel.GetDistributionCostFactor(label, label.headquartersCity);
		}
		return cogs;
	}

	private float GetPricePerUnit(ReleaseFormat format) {
		string key = format.ToString();
		return pricePerUnitByFormat != null && pricePerUnitByFormat.TryGetValue(key, out float price)
			? price
			: 0.89f;
	}

	public float GetPricePerUnitForAudit(ReleaseFormat format) => GetPricePerUnit(format);

	private float GetPressingCostPerUnit(ReleaseFormat format) {
		string key = format.ToString();
		return pressingCostPerUnitByFormat != null && pressingCostPerUnitByFormat.TryGetValue(key, out float cost)
			? cost
			: 0.30f;
	}

	private RevenueTelemetry GetOrCreateRevenueTelemetry(string labelId, ReleaseFormat format) {
		var key = (labelId, format);
		if (!weeklyRevenueByLabelAndFormat.TryGetValue(key, out RevenueTelemetry telemetry)) {
			telemetry = new RevenueTelemetry();
			weeklyRevenueByLabelAndFormat[key] = telemetry;
		}
		return telemetry;
	}

	public IReadOnlyDictionary<(string LabelId, ReleaseFormat Format), RevenueTelemetry> GetWeeklyRevenueByLabelAndFormat() =>
		weeklyRevenueByLabelAndFormat;

	public void SetAlbumsEnabled(bool enabled) => enableAlbums = enabled;

	private void RouteDistributionSkim(AILabel client, float skimAmount, ReleaseFormat format,
		ChartManager.CompletedWeekSettlementEntry sourceEntry = null) {
		DistributionDeal deal = client.activeDeal;
		if (deal == null || skimAmount <= 0f) return;
		AILabel distributor = GetLabel(deal.distributorId);
		if (distributor == null || distributor == client) return;
		if (sourceEntry != null) {
			sourceEntry.DistributionIncome = skimAmount;
			sourceEntry.DistributionRecipientLabelId = distributor.labelId;
			sourceEntry.MarketNet = sourceEntry.LabelNet + skimAmount;
		}

		float recouped = Mathf.Min(Mathf.Max(0f, deal.unrecoupedAdvance), skimAmount);
		deal.unrecoupedAdvance = Mathf.Max(0f, deal.unrecoupedAdvance - recouped);
		distributor.cashReserves += skimAmount;
		distributor.monthlyRevenue += skimAmount;
		distributor.weeklyDistributionIncome += skimAmount;
		GetOrCreateRevenueTelemetry(distributor.labelId, format).distributionIncome += skimAmount;
		if (labelFinancials.TryGetValue(distributor.labelId, out LabelFinancialHistory financials)) {
			financials.lastMonthRevenue += skimAmount;
		}
	}
	
	private void ProcessWeeklyReleases(GameDate date) {
		int releasesThisWeek = 0;
		foreach (var label in aiLabels) {
			if (!label.IsActive) continue;
			if (label.roster.Count == 0) continue;
			
			float releaseChance = CalculateWeeklyReleaseChance(label, date.year, date.month);
			if (GD.Randf() < releaseChance) {
				WeeklyReleaseRollsFired++;
				if (TryReleaseRecord(label, date)) {
					releasesThisWeek++;
					WeeklySuccessfulReleases++;
					RecordReleaseAttempt(label.tier, success: true, artistSelectionFailure: false);
				} else {
					WeeklyFailedReleaseRolls++;
					RecordReleaseAttempt(label.tier, success: false, artistSelectionFailure: lastReleaseAttemptFailedArtistSelection);
					if (lastReleaseAttemptFailedArtistSelection) {
						WeeklyCooldownMismatchRolls++;
					}
				}
			}
		}
		if (debugMode && releasesThisWeek > 0) GD.Print($"Week {date}: {releasesThisWeek} new releases");
	}

	private void ResetWeeklyReleaseCounters() {
		weeklyReleaseLifecycleByTier.Clear();
		WeeklyReleaseRollsFired = 0;
		WeeklySuccessfulReleases = 0;
		WeeklyFailedReleaseRolls = 0;
		WeeklyCooldownMismatchRolls = 0;
		WeeklyPipelineAlbumDrops = 0;
		WeeklySingleReleases = 0;
		WeeklyAlbumProjectsScheduled = 0;
		WeeklyProductionSpend = 0f;
		WeeklyProductionEvents = 0;
		WeeklyMarketingSpend = 0f;
		WeeklyMarketingEvents = 0;
	}

	public ReleaseLifecycleFlow GetWeeklyReleaseLifecycleFlow(LabelTier tier) =>
		weeklyReleaseLifecycleByTier.TryGetValue(tier, out ReleaseLifecycleFlow flow) ? flow : default;

	private void RecordReleaseAttempt(LabelTier tier, bool success, bool artistSelectionFailure) {
		ReleaseLifecycleFlow current = GetWeeklyReleaseLifecycleFlow(tier);
		weeklyReleaseLifecycleByTier[tier] = new ReleaseLifecycleFlow(current.Attempts + 1,
			current.SuccessfulReleases + (success ? 1 : 0), current.ArtistSelectionFailures + (artistSelectionFailure ? 1 : 0));
	}
	
	private float CalculateWeeklyReleaseChance(AILabel label, int year, int month) {
		int availableArtists = GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true
			? label.CountArtistsEligibleForRelease(year)
			: label.roster.Count(a => a.weeksSinceLastRelease >= 10);
		float seasonalityMultiplier = MarketSeasonality.Enabled
			? MarketSeasonality.GetArtistAvailabilityMultiplier(year, month, liveTick: true)
			: 1f;
		return CalculateLabelReleaseCapacityChance(label.releasesPerMonth, label.status, availableArtists, seasonalityMultiplier);
	}

	/// <summary>
	/// Converts a label's modeled monthly release capacity into one weekly release
	/// opportunity. Calendar time must not expand this capacity implicitly; any
	/// future label growth belongs in an explicit investment/capability system.
	/// </summary>
	internal static float CalculateLabelReleaseCapacityChance(float releasesPerMonth, LabelStatus status,
		int availableArtists, float seasonalityMultiplier = 1f) {
		if (availableArtists <= 0) return 0f;
		float weeklyCapacity = Mathf.Max(0f, releasesPerMonth) / 4f;
		float statusMod = status switch {
			LabelStatus.Bankrupt => 0f, LabelStatus.Defunct => 0f, LabelStatus.Dying => 0.3f,
			LabelStatus.Struggling => 0.5f, LabelStatus.Stable => 1f, LabelStatus.Rising => 1.2f,
			LabelStatus.Acquired => 0.8f, _ => 1f
		};
		float availabilityMod = Mathf.Clamp((float)availableArtists / 3f, 0f, 1f);
		return Mathf.Clamp(weeklyCapacity * statusMod * availabilityMod * Mathf.Max(0f, seasonalityMultiplier), 0f, 1f);
	}

	public void RecordRetired(RecordRuntimeData runtimeData) {
		if (runtimeData?.baseRecord == null) return;
		string labelId = runtimeData.baseRecord.labelId;
		string recordId = runtimeData.baseRecord.recordId;
		if (!string.IsNullOrEmpty(labelId) && !string.IsNullOrEmpty(recordId) &&
			labelActiveRecords.TryGetValue(labelId, out var recordIds)) recordIds.Remove(recordId);

		float realizedNet = runtimeData.lifetimeLabelNet - runtimeData.sunkProductionCost;
		OnReleaseOutcome?.Invoke(new ReleaseOutcomeTelemetry {
			labelId = labelId,
			recordId = recordId,
			format = runtimeData.baseRecord.format,
			genre = runtimeData.baseRecord.primaryGenre,
			memoryEligible = runtimeData.revenueMemoryEligible,
			lifetimeLabelNet = runtimeData.lifetimeLabelNet,
			sunkProductionCost = runtimeData.sunkProductionCost,
			realizedNet = realizedNet
		});

		if (!runtimeData.revenueMemoryEligible) return;
		if (GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true)
			UpdateApplicableResponsiveMemoryObservations(runtimeData, finalized: true);
		if (!string.IsNullOrEmpty(runtimeData.albumProjectId) && projectById.TryGetValue(runtimeData.albumProjectId, out AlbumProject project)) {
			if (runtimeData.projectRole == ProjectRecordRole.PromoSingle) {
				project.promoRetired = true;
				project.heldPromoOutcome = realizedNet;
				project.promoOutcomeState = ProjectOutcomeState.Retired;
				if (runtimeData.peakPosition > 0) project.promoPeakAtDrop = runtimeData.peakPosition;
				if (project.terminalState == AlbumProjectTerminalState.Cancelled) RedirectCancelledPromoOutcome(project);
				else TryFoldProjectMemory(project);
				return;
			}
			if (runtimeData.projectRole is ProjectRecordRole.LinkedAlbum or ProjectRecordRole.StandaloneAlbum) {
				project.rawDemandBeforeCannibalization = runtimeData.rawAlbumDemandBeforeCannibalization;
				project.suppressedDemand = runtimeData.suppressedAlbumDemand;
				project.demandWithActiveLinkedPromo = runtimeData.albumDemandWithActiveLinkedPromo;
				project.demandWithInactiveLinkedPromo = runtimeData.albumDemandWithInactiveLinkedPromo;
				project.demandWeightedSingleHeat = runtimeData.albumDemandWeightedSingleHeat;
				project.demandWeightedSubstitutionPropensity = runtimeData.albumDemandWeightedSubstitutionPropensity;
				project.demandWeightedSuppression = runtimeData.albumDemandWeightedSuppression;
				project.albumRetired = true;
				project.heldAlbumOutcome = realizedNet;
				project.albumOutcomeState = ProjectOutcomeState.Retired;
				TryFoldProjectMemory(project);
				return;
			}
		}
		// The release keyed residual is authoritative only in the enabled live
		// route. The frozen disabled route retains its historical EMA update.
		if (!GenreMarketV2.Enabled) ApplyMemoryObservation(labelId, runtimeData.baseRecord.format, realizedNet);
	}

	private void ApplyMemoryObservation(string labelId, ReleaseFormat format, float realizedNet) {
		// Retained only for project-state compatibility and diagnostics. The live
		// decision path uses UpdateResponsiveMemoryObservation instead.
		AILabel label = GetLabel(labelId);
		if (label == null) return;
		FormatRevenueMemory memory = label.GetOrCreateRevenueMemory(format);
		float alpha = Mathf.Clamp(revenueMemoryAlpha, 0f, 1f);
		memory.emaNetPerRelease = memory.releasesObserved == 0 ? realizedNet : Mathf.Lerp(memory.emaNetPerRelease, realizedNet, alpha);
		memory.releasesObserved++;
	}

	private void UpdateApplicableResponsiveMemoryObservations(RecordRuntimeData runtimeData, bool finalized) {
		if (runtimeData == null) return;
		foreach (RevenueEstimatorLane lane in GetApplicableEstimatorLanes(runtimeData.projectRole))
			UpdateResponsiveMemoryObservation(runtimeData, lane, finalized);
	}

	internal static RevenueEstimatorLane[] GetApplicableEstimatorLanes(ProjectRecordRole role) => role switch {
		ProjectRecordRole.OrphanSingle => new[] { RevenueEstimatorLane.OrphanSingle },
		ProjectRecordRole.PromoSingle => new[] { RevenueEstimatorLane.PromoSingle },
		// One physical outcome is applicable to both general Album eligibility
		// and the standalone-strategy estimator. Finance is still posted once.
		ProjectRecordRole.StandaloneAlbum => new[] { RevenueEstimatorLane.AlbumComponent, RevenueEstimatorLane.StandaloneAlbum },
		// Combined promo-plus-Album economics are observed separately by project
		// memory; the physical Album component owns eligibility feedback.
		ProjectRecordRole.LinkedAlbum => new[] { RevenueEstimatorLane.AlbumComponent },
		_ => System.Array.Empty<RevenueEstimatorLane>()
	};

	private void UpdateResponsiveMemoryObservation(RecordRuntimeData runtimeData, RevenueEstimatorLane lane, bool finalized) {
		if (runtimeData?.baseRecord == null || string.IsNullOrEmpty(runtimeData.baseRecord.labelId)) return;
		AILabel label = GetLabel(runtimeData.baseRecord.labelId);
		if (label == null) return;
		FormatRevenueMemory memory = label.GetOrCreateRevenueMemory(lane);
		string releaseId = runtimeData.baseRecord.recordId;
		int age = Mathf.Max(0, runtimeData.weeksSinceRelease);
		float expectedNet = runtimeData.releaseTimeExpectedNet;
		float scale = Mathf.Max(1f, Mathf.Max(runtimeData.releaseTimeOpportunityScale, Mathf.Abs(expectedNet)));
		float terminalAge = runtimeData.baseRecord.format == ReleaseFormat.Album ? 52f : 20f;
		float maturity = finalized ? 1f : Mathf.Clamp((age + 1f) / terminalAge, .05f, 1f);
		float realizedToDate = runtimeData.lifetimeLabelNet - runtimeData.sunkProductionCost;
		// Production is a one-time sunk cost. Annualize only the accumulating
		// revenue, then subtract that cost once; dividing realized net by maturity
		// repeatedly charged the same production cost and made young Albums appear
		// catastrophically unprofitable.
		float estimatedOutcome = EstimateResponsiveMemoryOutcome(runtimeData.lifetimeLabelNet,
			runtimeData.sunkProductionCost, maturity, finalized);
		float ageMatchedExpectedNet = GetAgeMatchedExpectedNet(expectedNet,
			runtimeData.sunkProductionCost, maturity);
		float residual = Mathf.Clamp((estimatedOutcome - expectedNet) / scale, -ResponsiveMemoryResidualLimit, ResponsiveMemoryResidualLimit);
		if (float.IsNaN(residual) || float.IsInfinity(residual)) return;
		FormatMemoryObservation observation = memory.observations.FirstOrDefault(item => item.releaseId == releaseId);
		if (observation == null) {
			observation = new FormatMemoryObservation { releaseId = releaseId, projectId = runtimeData.albumProjectId,
				releaseLane = runtimeData.projectRole, estimatorLane = lane,
				releaseWeek = runtimeData.releaseMemoryWeek, expectedNet = expectedNet, opportunityScale = scale };
			memory.observations.Add(observation);
		}
		if (!TryAdvanceResponsiveMemoryRevision(observation, age, finalized,
			out bool replacedPriorRevision, out int revisionOrdinal)) return;
		observation.normalizedResidual = residual;
		observation.maturityWeight = maturity;
		OnFormatMemoryRevision?.Invoke(new FormatMemoryRevisionTelemetry {
			releaseId = releaseId, labelId = runtimeData.baseRecord.labelId, format = runtimeData.baseRecord.format,
			projectId = runtimeData.albumProjectId, releaseLane = runtimeData.projectRole, estimatorLane = lane,
			genre = runtimeData.baseRecord.primaryGenre, releaseAge = age, revisionKind = finalized ? "Final" : $"Age{age}",
			revisionOrdinal = revisionOrdinal,
			releaseTimeExpectedNet = expectedNet, ageMatchedExpectedNet = ageMatchedExpectedNet,
			realizedNetToDate = realizedToDate, estimatedOutcomeNet = estimatedOutcome, opportunityScale = scale,
			normalizedResidual = residual, maturityWeight = maturity,
			recencyWeight = 1f, replacedPriorRevision = replacedPriorRevision, finalized = observation.finalized
		});
	}

	private static float EstimateResponsiveMemoryOutcome(float lifetimeLabelNet, float sunkProductionCost,
		float maturity, bool finalized) {
		float realizedToDate = lifetimeLabelNet - sunkProductionCost;
		if (finalized) return realizedToDate;
		return lifetimeLabelNet / Mathf.Max(.05f, maturity) - sunkProductionCost;
	}

	private static float GetAgeMatchedExpectedNet(float terminalExpectedNet, float sunkProductionCost, float maturity) =>
		(terminalExpectedNet + sunkProductionCost) * Mathf.Clamp(maturity, .05f, 1f) - sunkProductionCost;

	internal static (float EstimatedOutcome, float AgeMatchedExpected) GetResponsiveMemoryEconomicsForProbe(
		float lifetimeLabelNet, float sunkProductionCost, float terminalExpectedNet, float maturity, bool finalized) =>
		(EstimateResponsiveMemoryOutcome(lifetimeLabelNet, sunkProductionCost, maturity, finalized),
			GetAgeMatchedExpectedNet(terminalExpectedNet, sunkProductionCost, finalized ? 1f : maturity));

	/// <summary>
	/// Advances one release-keyed memory observation. The first revision is not a
	/// replacement; provisional duplicates, backward ages, and all post-final
	/// revisions are rejected. A final revision may replace a provisional row at
	/// the same age.
	/// </summary>
	internal static bool TryAdvanceResponsiveMemoryRevision(FormatMemoryObservation observation, int age, bool finalized,
		out bool replacedPriorRevision, out int revisionOrdinal) {
		replacedPriorRevision = false;
		revisionOrdinal = observation?.revisionOrdinal ?? 0;
		if (observation == null || age < 0 || observation.finalized || age < observation.lastRevisionAge) return false;
		if (!finalized && age <= observation.lastRevisionAge) return false;
		replacedPriorRevision = observation.lastRevisionAge >= 0;
		observation.lastRevisionAge = age;
		observation.revisionOrdinal++;
		observation.finalized = finalized;
		revisionOrdinal = observation.revisionOrdinal;
		return true;
	}

	private (float Residual, float EffectiveWeight, float Confidence) GetResponsiveMemory(FormatRevenueMemory memory, int currentWeek) {
		if (memory?.observations == null) return (0f, 0f, 0f);
		float weightedResidual = 0f, weight = 0f;
		foreach (FormatMemoryObservation observation in memory.observations) {
			int age = Mathf.Max(0, currentWeek - observation.releaseWeek);
			if (age > ResponsiveMemoryMaximumHistoryWeeks) continue;
			float recency = Mathf.Pow(.5f, age / ResponsiveMemoryHalfLifeWeeks);
			float itemWeight = recency * Mathf.Clamp(observation.maturityWeight, .05f, 1f);
			weightedResidual += observation.normalizedResidual * itemWeight;
			weight += itemWeight;
		}
		float confidence = CalculateResponsiveMemoryConfidence(weight);
		return (weight > 0f ? weightedResidual / weight : 0f, weight, confidence);
	}

	private static float CalculateResponsiveMemoryConfidence(float effectiveWeight) =>
		Mathf.Min(ResponsiveMemoryMaximumConfidence,
			Mathf.Max(0f, effectiveWeight) / (Mathf.Max(0f, effectiveWeight) + ResponsiveMemoryConfidenceK));

	internal static float GetResponsiveMemoryConfidenceForProbe(float effectiveWeight) =>
		CalculateResponsiveMemoryConfidence(effectiveWeight);

	private void TryFoldProjectMemory(AlbumProject project) {
		if (project == null || project.albumMemoryFolded || project.heldAlbumOutcome == null) return;
		if (project.strategy == ReleaseStrategy.AlbumWithPromo && project.heldPromoOutcome == null) return;
		float combined = project.heldAlbumOutcome.Value + (project.heldPromoOutcome ?? 0f);
		if (GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true) {
			UpdateAlbumWithPromoProjectMemoryObservation(project, finalized: true);
		} else ApplyMemoryObservation(project.currentLabelId, ReleaseFormat.Album, combined);
		project.projectRealizedNet = combined;
		project.albumMemoryFolded = true;
		project.albumOutcomeState = ProjectOutcomeState.FoldedToAlbum;
		if (project.strategy == ReleaseStrategy.AlbumWithPromo) project.promoOutcomeState = ProjectOutcomeState.FoldedToAlbum;
	}

	private void UpdateAlbumWithPromoProjectMemoryObservation(AlbumProject project, bool finalized) {
		if (project?.strategy != ReleaseStrategy.AlbumWithPromo || string.IsNullOrEmpty(project.projectId)) return;
		AILabel label = GetLabel(project.currentLabelId);
		if (label == null) return;
		FormatRevenueMemory memory = label.GetOrCreateRevenueMemory(RevenueEstimatorLane.AlbumWithPromo);
		FormatMemoryObservation observation = memory.observations.FirstOrDefault(item => item.releaseId == project.projectId);
		if (observation == null) {
			observation = new FormatMemoryObservation {
				releaseId = project.projectId, projectId = project.projectId,
				releaseLane = ProjectRecordRole.LinkedAlbum, estimatorLane = RevenueEstimatorLane.AlbumWithPromo,
				releaseWeek = project.scheduledWeek, expectedNet = project.projectedProjectNet,
				opportunityScale = Mathf.Max(1f, Mathf.Abs(project.projectedProjectNet))
			};
			memory.observations.Add(observation);
		}

		float promoDelta = GetProjectComponentOutcomeDelta(project.promoSingleId, project.heldPromoOutcome,
			project.projectedPromoSingleNet, terminalAge: 20f, finalized, out float promoMaturity);
		float albumDelta = GetProjectComponentOutcomeDelta(project.albumRecord?.recordId, project.heldAlbumOutcome,
			project.projectedAlbumNet, terminalAge: 52f, finalized, out float albumMaturity);
		float estimatedProject = project.projectedProjectNet + promoDelta + albumDelta;
		float residual = Mathf.Clamp((estimatedProject - project.projectedProjectNet) /
			Mathf.Max(1f, Mathf.Abs(project.projectedProjectNet)), -ResponsiveMemoryResidualLimit, ResponsiveMemoryResidualLimit);
		int projectAge = Mathf.Max(0, (ChartManager.Instance?.GetCurrentChartWeek() ?? project.scheduledWeek) - project.scheduledWeek);
		if (!TryAdvanceResponsiveMemoryRevision(observation, projectAge, finalized,
			out bool replacedPriorRevision, out int revisionOrdinal)) return;
		observation.normalizedResidual = residual;
		observation.maturityWeight = finalized ? 1f : Mathf.Clamp((promoMaturity + albumMaturity) * .5f, .05f, 1f);
		UpdatePooledAlbumWithPromoObservation(project, projectAge, finalized, residual, observation.maturityWeight);
		OnFormatMemoryRevision?.Invoke(new FormatMemoryRevisionTelemetry {
			releaseId = project.projectId, projectId = project.projectId, labelId = project.currentLabelId,
			format = ReleaseFormat.Album, releaseLane = ProjectRecordRole.LinkedAlbum,
			estimatorLane = RevenueEstimatorLane.AlbumWithPromo, genre = project.genre,
			releaseAge = projectAge, revisionKind = finalized ? "Final" : $"Age{projectAge}", revisionOrdinal = revisionOrdinal,
			releaseTimeExpectedNet = project.projectedProjectNet, ageMatchedExpectedNet = project.projectedProjectNet,
			realizedNetToDate = (project.heldPromoOutcome ?? 0f) + (project.heldAlbumOutcome ?? 0f),
			estimatedOutcomeNet = estimatedProject, opportunityScale = observation.opportunityScale,
			normalizedResidual = residual, maturityWeight = observation.maturityWeight, recencyWeight = 1f,
			replacedPriorRevision = replacedPriorRevision, finalized = observation.finalized
		});
	}

	private void UpdatePooledAlbumWithPromoObservation(AlbumProject project, int projectAge, bool finalized,
		float residual, float maturityWeight) {
		FormatMemoryObservation observation = pooledAlbumWithPromoMemory.observations
			.FirstOrDefault(item => item.releaseId == project.projectId);
		if (observation == null) {
			observation = new FormatMemoryObservation {
				releaseId = project.projectId, projectId = project.projectId,
				releaseLane = ProjectRecordRole.LinkedAlbum, estimatorLane = RevenueEstimatorLane.AlbumWithPromo,
				releaseWeek = project.scheduledWeek, expectedNet = project.projectedProjectNet,
				opportunityScale = Mathf.Max(1f, Mathf.Abs(project.projectedProjectNet))
			};
			pooledAlbumWithPromoMemory.observations.Add(observation);
		}
		if (!TryAdvanceResponsiveMemoryRevision(observation, projectAge, finalized, out _, out _)) return;
		observation.normalizedResidual = residual;
		observation.maturityWeight = maturityWeight;
	}

	private float GetProjectComponentOutcomeDelta(string recordId, float? heldOutcome, float expectedNet,
		float terminalAge, bool finalized, out float maturity) {
		if (heldOutcome.HasValue) {
			maturity = 1f;
			return heldOutcome.Value - expectedNet;
		}
		RecordRuntimeData runtime = string.IsNullOrEmpty(recordId) ? null : ChartManager.Instance?.GetRecordRuntimeData(recordId);
		if (runtime == null) {
			maturity = finalized ? 1f : 0f;
			return 0f;
		}
		maturity = Mathf.Clamp((runtime.weeksSinceRelease + 1f) / terminalAge, .05f, 1f);
		float estimated = EstimateResponsiveMemoryOutcome(runtime.lifetimeLabelNet, runtime.sunkProductionCost, maturity, finalized: false);
		return estimated - expectedNet;
	}

	private void RedirectCancelledPromoOutcome(AlbumProject project) {
		if (project?.heldPromoOutcome == null || project.promoOutcomeState == ProjectOutcomeState.RedirectedToSingle) return;
		if (!(GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true))
			ApplyMemoryObservation(project.currentLabelId, ReleaseFormat.Single, project.heldPromoOutcome.Value);
		project.promoOutcomeState = ProjectOutcomeState.RedirectedToSingle;
	}
	
	private bool TryReleaseRecord(AILabel label, GameDate date) {
		lastReleaseAttemptFailedArtistSelection = false;
		PrepareAnnualFormatCapacity(date.year);
		var artist = RosterManager.Instance?.GetArtistForRelease(label) ?? label.GetArtistForRelease(date.year);
		if (artist == null) {
			lastReleaseAttemptFailedArtistSelection = true;
			return false;
		}
		if (GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true &&
			!IsEligibleForEnabledFormatDecision(artist)) {
			GD.PushError($"Enabled release invariant failed: terminal artist {artist.artistId} reached release selection.");
			lastReleaseAttemptFailedArtistSelection = true;
			return false;
		}
		Genre artistPrimary = artist.primaryGenre;
		Genre artistSecondary = artist.secondaryGenre;
		GenreSupplyService.GenreSelection projectSelection = ChooseEnabledGenreSupply(label, artist, date.year);
		Genre projectGenre = projectSelection.Genre;
		bool explicitProjectIdentity = ArtistPopulationLifecycle.IsLive;
		SimulatedArtist decisionArtist = explicitProjectIdentity
			? CreateProjectDecisionArtist(artist, projectGenre, artistPrimary)
			: artist;
		if (!explicitProjectIdentity) {
			if (projectGenre != GenreCatalog.MapLegacy(artistPrimary, date.year)) artist.secondaryGenre = GenreCatalog.MapLegacy(artistPrimary, date.year);
			artist.primaryGenre = projectGenre;
		}

		// Snapshot only information available at the release fork. These pure reads are
		// also emitted for album-disabled calibration runs; they consume no RNG.
		DecisionContext decision = BuildDecisionContext(label, decisionArtist, date.year, date.month);
		decision.nonRetainedEmergingProject = IsNonRetainedEmergingProjectForFormatMemory(projectGenre,
			projectSelection.RetainedIdentity, date.year, GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true);
		ReleasePlan plan = DecideRelease(label, decisionArtist, date.year, decision);
		if (!explicitProjectIdentity) {
			artist.primaryGenre = artistPrimary;
			artist.secondaryGenre = artistSecondary;
		}
		if (plan.format == ReleaseFormat.Album) {
			return TryInitiateAlbumProject(label, artist, date, decision, plan, projectGenre);
		}
		var record = GenerateRecordFromArtist(label, artist, date.year, plan.format);
		if (GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true)
			ApplyProjectGenre(record, projectGenre, artistPrimary);
		float realizedQuality = (record.hookStrength + record.productionQuality) / 2f;
		float noiseRange = Mathf.Lerp(0.30f, 0.10f, label.scoutingAbility);
		float perceivedQuality = Mathf.Clamp(realizedQuality + (float)GD.RandRange(-noiseRange, noiseRange), 0f, 1f);
		float perceivedQualityMult = 0.6f + (perceivedQuality * 0.8f);

		float productionCost = CalculateProductionCost(label, record, date);
		float marketingBudget = label.GetMarketingBudget(artist) * perceivedQualityMult;
		float totalCost = productionCost + marketingBudget;
		float minReserve = label.GetMonthlyOverhead();
		
		if (label.cashReserves - totalCost < minReserve) {
			float available = label.cashReserves - minReserve - productionCost;
			if (available < 0) return false;
			marketingBudget = available * 0.8f;
			totalCost = productionCost + marketingBudget;
		}
		
		label.cashReserves -= totalCost;
		label.monthlyExpenses += totalCost;
		artist.unrecoupedAdvance += productionCost;
		WeeklyProductionSpend += productionCost;
		WeeklyProductionEvents++;
		WeeklyMarketingSpend += marketingBudget;
		WeeklyMarketingEvents++;
		if (labelFinancials.TryGetValue(label.labelId, out var financials)) {
			financials.lastMonthExpenses += totalCost;
		}
		
		record.releaseDate = date;
		record.projectRole = ProjectRecordRole.OrphanSingle;
		record.albumProjectId = string.Empty;
		ChartManager.Instance.ReleaseRecord(record);
		var runtimeData = ChartManager.Instance.GetRecordRuntimeData(record.recordId);
		if (runtimeData == null) throw new System.InvalidOperationException($"Released record '{record.recordId}' has no runtime data.");
		runtimeData.sunkProductionCost = productionCost;
		runtimeData.revenueMemoryEligible = true;
		runtimeData.releaseTimeExpectedNet = plan.format == ReleaseFormat.Album ? plan.priorAlbumNet : plan.priorSingleNet;
		runtimeData.releaseTimeOpportunityScale = Mathf.Max(1f, Mathf.Max(Mathf.Abs(runtimeData.releaseTimeExpectedNet), productionCost));
		runtimeData.releaseMemoryWeek = ChartManager.Instance.GetCurrentChartWeek();
		runtimeData.projectRole = record.projectRole;
		ApplyReleasePromotion(record, artist, label, marketingBudget, perceivedQualityMult);
		CaptureSingleOpportunity(runtimeData, label, date.year);
		TrackRelease(label.labelId, record.recordId);
		WeeklySingleReleases++;
		RosterManager.Instance?.RecordReleased(artist, record.recordId);
		artist.weeksSinceLastRelease = 0;
		artist.releaseHistory.Add(record.recordId);
		if (record.format == ReleaseFormat.Single) artist.releasedSingleIds.Add(record.recordId);
		annualFormatDecisions++;
		OnCalibrationDecision?.Invoke(new CalibrationDecisionTelemetry {
			recordId = record.recordId,
			labelId = label.labelId,
			artistId = artist.artistId,
			genre = record.primaryGenre,
			careerState = artist.careerState,
			qualityEstimate = decision.qualityEstimate,
			reachFactor = decision.reachFactor,
			genreSinglesMarketFactor = decision.genreSinglesMarketFactor,
			singleProductionCost = decision.singleProductionCost,
			chosenFormat = plan.format
		});
		if (plan.economicsEvaluated) {
		OnReleaseStrategy?.Invoke(new ReleaseStrategyTelemetry {
				recordId = record.recordId,
				labelId = label.labelId,
				tier = label.tier,
				artistId = artist.artistId,
			genre = record.primaryGenre,
			secondaryGenre = record.secondaryGenre,
				careerState = artist.careerState,
				careerBand = GetCareerBandLabel(plan.careerBand, plan.unexpectedCareerState),
				qualityEstimate = decision.qualityEstimate,
				qualityQuartile = $"Q{plan.qualityQuartile + 1}",
				reachFactor = decision.reachFactor,
				genreSinglesMarketFactor = decision.genreSinglesMarketFactor,
				priorSingleNet = plan.priorSingleNet,
				priorAlbumNet = plan.priorAlbumNet,
				projectedSingleNet = plan.projectedSingleNet,
				projectedAlbumNet = plan.projectedAlbumNet,
				confidenceSingle = plan.confidenceSingle,
				confidenceAlbum = plan.confidenceAlbum,
				rawConfidenceSingle = plan.rawConfidenceSingle, rawConfidenceAlbum = plan.rawConfidenceAlbum,
				singleMemoryCapApplied = plan.singleMemoryCapApplied, albumMemoryCapApplied = plan.albumMemoryCapApplied,
				chosenFormat = plan.format,
				assumedCompilationCost = plan.legacyFourResolvableSingles,
				compCostWeight = plan.compCostWeight,
				expectedFormatMultiplier = plan.expectedFormatMultiplier,
				releasedSingleIdsExamined = plan.releasedSingleIdsExamined,
				resolvedSingles = plan.resolvedSingles,
				chartedSingles = plan.chartedSingles,
				hitScore = plan.hitScore,
				unweightedHitUnits = plan.unweightedHitUnits,
				weightedHitUnits = plan.weightedHitUnits,
				affinityUnits = plan.affinityUnits,
				totalExpectedAlbumUnits = plan.totalExpectedAlbumUnits,
				actualAlbumFormat = record.album?.albumFormat,
				strategy = ReleaseStrategy.OrphanSingle,
				projectedOrphanSingleNet = plan.projectedSingleNet,
				projectedAlbumStandaloneNet = plan.projectedAlbumStandaloneNet,
				projectedAlbumWithPromoNet = plan.projectedAlbumWithPromoNet,
				singlePreTiltContribution = plan.singlePreTiltContribution, singleFormatTilt = plan.singleFormatTilt,
				albumAffinity = plan.albumAffinity, albumOpportunity = plan.albumOpportunity,
				albumFormatTilt = plan.albumFormatTilt, albumPreTiltContribution = plan.albumPreTiltContribution,
				albumProductionCost = plan.albumProductionCost, singleProductionCost = plan.singleProductionCost, singleMemoryEma = plan.singleMemoryEma,
				albumMemoryEma = plan.albumMemoryEma, singleMemoryBlend = plan.singleMemoryBlend,
				albumMemoryBlend = plan.albumMemoryBlend, singleNoiseMultiplier = plan.singleNoiseMultiplier,
				albumChoiceProbability = plan.albumChoiceProbability, formatChoiceRoll = plan.formatChoiceRoll,
				labelFormatMemoryBypassed = plan.labelFormatMemoryBypassed,
				albumNoiseMultiplier = plan.albumNoiseMultiplier
			});
		}
		
		if (debugMode) {
			GD.Print($"🎵 {label.labelName}: '{record.title}' by {artist.stageName} (Quality: {(record.hookStrength + record.productionQuality) / 2f:F2}, Budget: ${totalCost:N0})");
		}
		return true;
	}

	private static SimulatedArtist CreateProjectDecisionArtist(SimulatedArtist source, Genre projectGenre, Genre identityGenre) => new() {
		artistId = source.artistId, stageName = source.stageName, type = source.type, members = source.members,
		primaryGenre = projectGenre, secondaryGenre = projectGenre == identityGenre ? source.secondaryGenre : identityGenre,
		formedYear = source.formedYear, vocalPower = source.vocalPower, musicianship = source.musicianship,
		songwritingAbility = source.songwritingAbility, livePerformance = source.livePerformance,
		studioPerformance = source.studioPerformance, groupCohesion = source.groupCohesion, careerState = source.careerState,
		momentum = source.momentum, reputation = source.reputation, totalReleases = source.totalReleases,
		charted = source.charted, top40Hits = source.top40Hits, top10Hits = source.top10Hits, numberOnes = source.numberOnes,
		weeksSinceLastRelease = source.weeksSinceLastRelease, releasedSingleIds = source.releasedSingleIds
	};
	internal static SimulatedArtist CreateProjectDecisionArtistForProbe(SimulatedArtist source, Genre projectGenre) =>
		CreateProjectDecisionArtist(source, projectGenre, source.primaryGenre);

	private GenreSupplyService.GenreSelection ChooseEnabledGenreSupply(AILabel label, SimulatedArtist artist, int year) {
		if (!GenreMarketV2.Enabled || ChartManager.Instance?.IsGenreMarketV2Live != true)
			return new GenreSupplyService.GenreSelection(artist.primaryGenre, retainedIdentity: true);
		if (genreSupplyYear != year) {
			genreSupplyYear = year;
			annualGenreSupplyByLabel.Clear();
			annualGenreSupplyGlobal.Clear();
		}
		if (!annualGenreSupplyByLabel.TryGetValue(label.labelId, out Dictionary<Genre, int> recent)) {
			recent = new Dictionary<Genre, int>();
			annualGenreSupplyByLabel[label.labelId] = recent;
		}
		MarketRegion region = ChartManager.Instance?.GetRegionById(label.homeRegion);
		Genre[] required = GenreSupplyService.GetAvailableGenres(year)
			.Where(genre => annualGenreSupplyGlobal.GetValueOrDefault(genre) < 3).ToArray();
		bool annualFloor = required.Length > 0;
		GenreSupplyService.GenreSelection selection = GenreSupplyService.ChooseGenreWithSelection(label, artist, region, year, recent,
			GetDeterministicSupplyRoll(label.labelId, artist.artistId, year, pipelineWeek, recent.Values.Sum()),
			annualFloor ? required : null, annualGenreSupplyGlobal, applyPsychedelicTransitionCompatibility: true);
		Genre chosen = selection.Genre;
		OnSupplySelection?.Invoke(new SupplySelectionTelemetry {
			labelId = label.labelId, artistId = artist.artistId, artistIdentity = artist.primaryGenre, chosenProjectGenre = chosen,
			artistIdentityAvailableForNewSupply = GenreSupplyService.IsAvailableForNewSupply(artist.primaryGenre, year),
			annualFloorRequested = annualFloor,
			annualFloorReroutedToNormalCandidates = annualFloor && !selection.RetainedIdentity && !selection.UsedCandidateOverride,
			selectionMode = selection.UsedCandidateOverride ? SupplySelectionMode.AnnualFloor :
				selection.RetainedIdentity ? SupplySelectionMode.Retained : SupplySelectionMode.WeightedTransition
		});
		recent[chosen] = recent.GetValueOrDefault(chosen) + 1;
		annualGenreSupplyGlobal[chosen] = annualGenreSupplyGlobal.GetValueOrDefault(chosen) + 1;
		return selection;
	}

	/// <summary>
	/// A label-wide format-result memory has no genre evidence for a novel,
	/// non-retained late-emerging project. The catalog introduction year, rather
	/// than a one-calendar-year lifecycle enum, keeps the rule applicable to the
	/// Psychedelic projects that remain new to an artist in 1969. Retained
	/// identities and all disabled/prewarm paths preserve the established
	/// label-format memory route exactly.
	/// </summary>
	internal static bool IsNonRetainedEmergingProjectForFormatMemory(Genre projectGenre, bool retainedIdentity, int year, bool live) {
		GenreProfile profile = GenreCatalog.Get(GenreCatalog.MapLegacy(projectGenre, year));
		return live && !retainedIdentity && profile.EmergenceYear >= 1966 && year >= profile.EmergenceYear;
	}

	internal static float GetProjectFormatMemoryConfidence(float labelFormatConfidence, bool nonRetainedEmergingProject) =>
		nonRetainedEmergingProject ? 0f : labelFormatConfidence;

	private static float GetDeterministicSupplyRoll(string labelId, string artistId, int year, int week, int sequence) {
		uint hash = 2166136261u;
		foreach (char value in $"{labelId}|{artistId}|{year}|{week}|{sequence}") {
			hash ^= value;
			hash *= 16777619u;
		}
		return (hash & 0x00ffffffu) / 16777216f;
	}

	private bool TryInitiateAlbumProject(AILabel label, SimulatedArtist artist, GameDate date, DecisionContext decision, ReleasePlan plan, Genre projectGenre) {
		Record album = GenerateRecordFromArtist(label, artist, date.year, ReleaseFormat.Album);
		if (GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true)
			ApplyProjectGenre(album, projectGenre, artist.primaryGenre);
		float albumProductionCost = CalculateProductionCost(label, album, date);
		float albumPerceivedMult;
		float albumMarketingPlanned;
		Record promo = null;
		int gapWeeks = 0;
		PromotionSnapshot albumPromotion = null;
		float promoProductionCost = 0f;
		float promoMarketingBudget = 0f;
		float promoPerceivedMult = 1f;

		if (plan.strategy == ReleaseStrategy.AlbumWithPromo) {
			promo = CreatePromoSingleFromAlbum(album);
			gapWeeks = (int)GD.RandRange(Mathf.Min(albumDropGapWeeksMin, albumDropGapWeeksMax), Mathf.Max(albumDropGapWeeksMin, albumDropGapWeeksMax));
			albumPerceivedMult = DrawPerceivedQualityMultiplier(album, label);
			albumMarketingPlanned = label.GetMarketingBudget(artist) * albumPerceivedMult;
			albumPromotion = BuildPromotionSnapshot(album, artist, albumPerceivedMult);
			promoProductionCost = CalculateProductionCost(label, promo, date);
			promoPerceivedMult = DrawPerceivedQualityMultiplier(promo, label);
			promoMarketingBudget = label.GetMarketingBudget(artist) * promoPerceivedMult;
		} else {
			albumPerceivedMult = DrawPerceivedQualityMultiplier(album, label);
			albumMarketingPlanned = label.GetMarketingBudget(artist) * albumPerceivedMult;
		}

		float firstMarketing = plan.strategy == ReleaseStrategy.AlbumStandalone ? albumMarketingPlanned : promoMarketingBudget;
		float productionTotal = albumProductionCost + promoProductionCost;
		if (!TryClampFirstEventCost(label, productionTotal, ref firstMarketing)) return false;
		if (plan.strategy == ReleaseStrategy.AlbumStandalone) albumMarketingPlanned = firstMarketing;
		else promoMarketingBudget = firstMarketing;
		ChargeProjectCost(label, artist, productionTotal + firstMarketing, productionTotal);
		WeeklyProductionSpend += productionTotal;
		WeeklyProductionEvents += promo == null ? 1 : 2;
		WeeklyMarketingSpend += firstMarketing;
		WeeklyMarketingEvents++;

		string projectId = $"project_{++generatedProjectCounter}";
		var project = new AlbumProject {
			projectId = projectId, creationSequence = generatedProjectCounter,
			originalLabelId = label.labelId, currentLabelId = label.labelId, tierAtSchedule = label.tier,
			artistId = artist.artistId, genre = projectGenre, careerStateAtSchedule = artist.careerState,
			careerStateBeforeDropAtSchedule = artist.careerStateBeforeDrop,
			contractEntryCareerStateAtSchedule = artist.contractEntryCareerState,
			contractSequenceAtSchedule = artist.contractSequence, contractStartWeekAtSchedule = artist.contractStartWeek,
			scheduledWeek = pipelineWeek, scheduledDate = date, dropWeek = pipelineWeek + gapWeeks,
			dropDate = date.AddDays(gapWeeks * 7), strategy = plan.strategy, albumRecord = album,
			promoSingleRecord = promo, promoSingleId = promo?.recordId, albumProductionCost = albumProductionCost,
			promoProductionCost = promoProductionCost, albumPromotionSnapshot = albumPromotion,
			albumMarketingBudgetPlanned = albumMarketingPlanned, releaseTimeAlbumExpectedNet = plan.priorAlbumNet,
			projectedAlbumNet = plan.projectedAlbumStandaloneNet,
			projectedPromoSingleNet = plan.expectedPromoSingleNet, projectedProjectNet = plan.projectedAlbumWithPromoNet,
			albumOutcomeState = ProjectOutcomeState.Pending,
			promoOutcomeState = promo == null ? ProjectOutcomeState.None : ProjectOutcomeState.Pending
		};
		albumProjects.Add(project);
		(string ArtistId, int Year) artistYear = (artist.artistId, date.year);
		annualAlbumProjectsByArtist[artistYear] = annualAlbumProjectsByArtist.GetValueOrDefault(artistYear) + 1;
		annualAlbumProjectsScheduled++;
		annualFormatDecisions++;
		WeeklyAlbumProjectsScheduled++;
		projectById[projectId] = project;
		projectByRecordId[album.recordId] = project;
		if (promo != null) projectByRecordId[promo.recordId] = project;

		if (plan.strategy == ReleaseStrategy.AlbumStandalone) {
			project.terminalState = AlbumProjectTerminalState.Released;
			ReleasePreparedRecord(album, artist, label, date, albumProductionCost, plan.priorAlbumNet,
				ProjectRecordRole.StandaloneAlbum, projectId);
			ApplyReleasePromotion(album, artist, label, albumMarketingPlanned, albumPerceivedMult);
		} else {
			pendingAlbumProjects.Add(project);
			ReleasePreparedRecord(promo, artist, label, date, promoProductionCost, plan.expectedPromoSingleNet,
				ProjectRecordRole.PromoSingle, projectId);
			ApplyReleasePromotion(promo, artist, label, promoMarketingBudget, promoPerceivedMult);
			CaptureSingleOpportunity(ChartManager.Instance.GetRecordRuntimeData(promo.recordId), label, date.year);
		}
		artist.weeksSinceLastRelease = 0;
		EmitAlbumDecisionTelemetry(label, artist, decision, plan, album, project);
		return true;
	}

	private static void ApplyProjectGenre(Record record, Genre projectGenre, Genre artistGenre) {
		if (record == null) return;
		record.primaryGenre = projectGenre;
		if (projectGenre != GenreCatalog.MapLegacy(artistGenre, record.releaseDate.year > 0 ? record.releaseDate.year : null))
			record.secondaryGenre = GenreCatalog.MapLegacy(artistGenre, record.releaseDate.year > 0 ? record.releaseDate.year : null);
		if (record.album == null) return;
		foreach (AlbumTrack track in record.album.GetAllTracks()) if (track != null) track.genre = projectGenre;
	}

	private float DrawPerceivedQualityMultiplier(Record record, AILabel label) {
		float realizedQuality = (record.hookStrength + record.productionQuality) / 2f;
		float noiseRange = Mathf.Lerp(0.30f, 0.10f, label.scoutingAbility);
		float perceivedQuality = Mathf.Clamp(realizedQuality + (float)GD.RandRange(-noiseRange, noiseRange), 0f, 1f);
		return 0.6f + perceivedQuality * 0.8f;
	}

	private bool TryClampFirstEventCost(AILabel label, float productionCost, ref float marketingBudget) {
		float minReserve = label.GetMonthlyOverhead();
		if (label.cashReserves - productionCost - marketingBudget >= minReserve) return true;
		float available = label.cashReserves - minReserve - productionCost;
		if (available < 0f) return false;
		marketingBudget = available * 0.8f;
		return true;
	}

	private void ChargeProjectCost(AILabel label, SimulatedArtist artist, float totalCost, float productionCost) {
		label.cashReserves -= totalCost;
		label.monthlyExpenses += totalCost;
		artist.unrecoupedAdvance += productionCost;
		if (labelFinancials.TryGetValue(label.labelId, out var financials)) financials.lastMonthExpenses += totalCost;
	}

	private void ReleasePreparedRecord(Record record, SimulatedArtist artist, AILabel label, GameDate date, float productionCost,
		float releaseTimeExpectedNet, ProjectRecordRole role, string projectId) {
		record.labelId = label.labelId;
		record.releaseDate = date;
		record.projectRole = role;
		record.albumProjectId = projectId;
		ChartManager.Instance.ReleaseRecord(record);
		if (record.format == ReleaseFormat.Album && record.album?.albumFormat == AlbumFormat.Compilation) {
			foreach (AlbumTrack track in record.album.trackRefs ?? System.Array.Empty<AlbumTrack>()) {
				ChartManager.Instance.RegisterCompUse(track?.sourceRecordId);
			}
		}
		RecordRuntimeData runtime = ChartManager.Instance.GetRecordRuntimeData(record.recordId)
			?? throw new System.InvalidOperationException($"Released record '{record.recordId}' has no runtime data.");
		runtime.sunkProductionCost = productionCost;
		runtime.revenueMemoryEligible = true;
		runtime.releaseTimeExpectedNet = releaseTimeExpectedNet;
		runtime.releaseTimeOpportunityScale = Mathf.Max(1f, Mathf.Max(Mathf.Abs(releaseTimeExpectedNet), productionCost));
		runtime.releaseMemoryWeek = ChartManager.Instance.GetCurrentChartWeek();
		runtime.projectRole = record.projectRole;
		runtime.albumProjectId = record.albumProjectId;
		if (role == ProjectRecordRole.LinkedAlbum && projectById.TryGetValue(projectId, out AlbumProject project)) runtime.linkedPromoSingleId = project.promoSingleId;
		TrackRelease(label.labelId, record.recordId);
		if (artist != null) {
			RosterManager.Instance?.RecordReleased(artist, record.recordId);
			artist.releaseHistory.Add(record.recordId);
			if (record.format == ReleaseFormat.Single) artist.releasedSingleIds.Add(record.recordId);
		}
	}

	private static void CaptureSingleOpportunity(RecordRuntimeData runtime, AILabel label, int year) {
		if (GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true)
			SingleOpportunityLedger.CaptureAtRelease(runtime, label, ChartManager.Instance.GetAllRegions(), year);
	}

	private void EmitAlbumDecisionTelemetry(AILabel label, SimulatedArtist artist, DecisionContext decision, ReleasePlan plan,
		Record album, AlbumProject project) {
		OnCalibrationDecision?.Invoke(new CalibrationDecisionTelemetry {
			recordId = album.recordId, labelId = label.labelId, artistId = artist.artistId, genre = album.primaryGenre,
			careerState = artist.careerState, qualityEstimate = decision.qualityEstimate, reachFactor = decision.reachFactor,
			genreSinglesMarketFactor = decision.genreSinglesMarketFactor, singleProductionCost = decision.singleProductionCost,
			chosenFormat = ReleaseFormat.Album
		});
		OnReleaseStrategy?.Invoke(BuildReleaseStrategyTelemetry(label, artist, decision, plan, album, project));
	}

	private ReleaseStrategyTelemetry BuildReleaseStrategyTelemetry(AILabel label, SimulatedArtist artist, DecisionContext decision,
		ReleasePlan plan, Record album, AlbumProject project) => new() {
		recordId = album.recordId, labelId = label.labelId, tier = label.tier, artistId = artist.artistId,
		// The decision was evaluated with projectGenre, then the artist identity was
		// restored before this event. Telemetry must describe the released project,
		// not that restored identity, or enabled project routing is misclassified.
		genre = album.primaryGenre, secondaryGenre = album.secondaryGenre, careerState = artist.careerState,
		careerBand = GetCareerBandLabel(plan.careerBand, plan.unexpectedCareerState), qualityEstimate = decision.qualityEstimate,
		qualityQuartile = $"Q{plan.qualityQuartile + 1}", reachFactor = decision.reachFactor,
		genreSinglesMarketFactor = decision.genreSinglesMarketFactor, priorSingleNet = plan.priorSingleNet,
		priorAlbumNet = plan.priorAlbumNet, projectedSingleNet = plan.projectedSingleNet, projectedAlbumNet = plan.projectedAlbumNet,
		confidenceSingle = plan.confidenceSingle, confidenceAlbum = plan.confidenceAlbum, chosenFormat = ReleaseFormat.Album,
		rawConfidenceSingle = plan.rawConfidenceSingle, rawConfidenceAlbum = plan.rawConfidenceAlbum,
		singleMemoryCapApplied = plan.singleMemoryCapApplied, albumMemoryCapApplied = plan.albumMemoryCapApplied,
		assumedCompilationCost = plan.legacyFourResolvableSingles, compCostWeight = plan.compCostWeight,
		expectedFormatMultiplier = plan.expectedFormatMultiplier, releasedSingleIdsExamined = plan.releasedSingleIdsExamined,
		resolvedSingles = plan.resolvedSingles, chartedSingles = plan.chartedSingles, hitScore = plan.hitScore,
		unweightedHitUnits = plan.unweightedHitUnits, weightedHitUnits = plan.weightedHitUnits,
		affinityUnits = plan.affinityUnits, totalExpectedAlbumUnits = plan.totalExpectedAlbumUnits,
		actualAlbumFormat = album.album?.albumFormat, projectId = project.projectId, strategy = plan.strategy,
		projectedOrphanSingleNet = plan.projectedOrphanSingleNet, projectedAlbumStandaloneNet = plan.projectedAlbumStandaloneNet,
		projectedAlbumWithPromoNet = plan.projectedAlbumWithPromoNet, promoSingleId = project.promoSingleId,
		albumStrategyEvaluated = plan.albumStrategyEvaluated, singleProductionCost = plan.singleProductionCost,
		singleNetMarginPerUnit = plan.singleNetMarginPerUnit, expectedSingleUnits = plan.expectedSingleUnits,
		albumDemandFactor = plan.albumDemandFactor, substitutionK = plan.substitutionK,
		substitutionCap = plan.substitutionCap, substitutionPropensity = plan.substitutionPropensity,
		expectedOverlapFraction = plan.expectedOverlapFraction, divertedUnits = plan.divertedUnits,
		albumMarginPerUnit = plan.albumMarginPerUnit, cannibalizationLoss = plan.cannibalizationLoss,
		expectedPromoLift = plan.expectedPromoLift, expectedPromoSingleNet = plan.expectedPromoSingleNet,
		promoAdvantage = plan.promoAdvantage, singlePreTiltContribution = plan.singlePreTiltContribution,
		singleFormatTilt = plan.singleFormatTilt, albumAffinity = plan.albumAffinity,
		albumOpportunity = plan.albumOpportunity, albumFormatTilt = plan.albumFormatTilt,
		albumPreTiltContribution = plan.albumPreTiltContribution, albumProductionCost = plan.albumProductionCost,
		singleMemoryEma = plan.singleMemoryEma, albumMemoryEma = plan.albumMemoryEma,
		singleMemoryBlend = plan.singleMemoryBlend, albumMemoryBlend = plan.albumMemoryBlend,
		labelFormatMemoryBypassed = plan.labelFormatMemoryBypassed,
		singleNoiseMultiplier = plan.singleNoiseMultiplier, albumNoiseMultiplier = plan.albumNoiseMultiplier,
		albumChoiceProbability = plan.albumChoiceProbability, formatChoiceRoll = plan.formatChoiceRoll,
		albumCapacityReroute = plan.albumCapacityReroute
	};

	private ReleasePlan DecideRelease(AILabel label, SimulatedArtist artist, int year, DecisionContext decision) {
		if (!enableAlbums) return new() { format = ReleaseFormat.Single, strategy = ReleaseStrategy.OrphanSingle };

		decision.qualityQuartile = GetQualityQuartile(decision.qualityEstimate);
		decision.careerBand = GetCareerBandIndex(artist.careerState, out bool unexpectedCareerState);
		decision.unexpectedCareerState = unexpectedCareerState;
		float compCostWeight = CalculateCompilationCostWeight(artist.primaryGenre, year);
		HitInventory hitInventory = ResolveHitInventory(artist);
		float priorSingle = CalculateSinglePriorNet(decision);
		float priorAlbum = CalculateAlbumPriorNet(label, artist, year, decision, compCostWeight, hitInventory, out AlbumPriorDiagnostics albumPrior);
		bool useResponsiveMemory = GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true;
		// Live choices read only the estimator that answers that exact decision.
		// Legacy format memories remain untouched for disabled replay compatibility.
		FormatRevenueMemory singleMemory = useResponsiveMemory
			? label.GetOrCreateRevenueMemory(RevenueEstimatorLane.OrphanSingle) : label.GetOrCreateRevenueMemory(ReleaseFormat.Single);
		FormatRevenueMemory albumMemory = useResponsiveMemory
			? label.GetOrCreateRevenueMemory(RevenueEstimatorLane.AlbumComponent) : label.GetOrCreateRevenueMemory(ReleaseFormat.Album);
		FormatRevenueMemory standaloneMemory = useResponsiveMemory
			? label.GetOrCreateRevenueMemory(RevenueEstimatorLane.StandaloneAlbum) : albumMemory;
		FormatRevenueMemory promoMemory = useResponsiveMemory
			? label.GetOrCreateRevenueMemory(RevenueEstimatorLane.PromoSingle) : singleMemory;
		FormatRevenueMemory projectMemory = useResponsiveMemory
			? label.GetOrCreateRevenueMemory(RevenueEstimatorLane.AlbumWithPromo) : albumMemory;
		var singleResponsive = GetResponsiveMemory(singleMemory, ChartManager.Instance?.GetCurrentChartWeek() ?? 0);
		var albumResponsive = GetResponsiveMemory(albumMemory, ChartManager.Instance?.GetCurrentChartWeek() ?? 0);
		var standaloneResponsive = GetResponsiveMemory(standaloneMemory, ChartManager.Instance?.GetCurrentChartWeek() ?? 0);
		var promoResponsive = GetResponsiveMemory(promoMemory, ChartManager.Instance?.GetCurrentChartWeek() ?? 0);
		int currentWeek = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		var labelProjectResponsive = GetResponsiveMemory(projectMemory, currentWeek);
		var pooledProjectResponsive = GetResponsiveMemory(pooledAlbumWithPromoMemory, currentWeek);
		float projectLocalBlend = labelProjectResponsive.Confidence /
			Mathf.Max(.000001f, labelProjectResponsive.Confidence + pooledProjectResponsive.Confidence);
		var projectResponsive = (
			Residual: pooledProjectResponsive.Confidence > 0f
				? Mathf.Lerp(pooledProjectResponsive.Residual, labelProjectResponsive.Residual, projectLocalBlend)
				: labelProjectResponsive.Residual,
			EffectiveWeight: pooledProjectResponsive.EffectiveWeight + labelProjectResponsive.EffectiveWeight,
			Confidence: Mathf.Max(pooledProjectResponsive.Confidence, labelProjectResponsive.Confidence));
		float confidenceSingle = useResponsiveMemory ? singleResponsive.Confidence : singleMemory.releasesObserved / (singleMemory.releasesObserved + Mathf.Max(.1f, revenueMemoryConfidenceK));
		float confidenceAlbum = useResponsiveMemory ? albumResponsive.Confidence : albumMemory.releasesObserved / (albumMemory.releasesObserved + Mathf.Max(.1f, revenueMemoryConfidenceK));
		float confidenceStandalone = useResponsiveMemory ? standaloneResponsive.Confidence : confidenceAlbum;
		confidenceSingle = GetProjectFormatMemoryConfidence(confidenceSingle, decision.nonRetainedEmergingProject);
		confidenceAlbum = GetProjectFormatMemoryConfidence(confidenceAlbum, decision.nonRetainedEmergingProject);
		confidenceStandalone = GetProjectFormatMemoryConfidence(confidenceStandalone, decision.nonRetainedEmergingProject);
		float rawConfidenceSingle = confidenceSingle;
		float rawConfidenceAlbum = confidenceAlbum;
		bool applyLiveMemoryCeiling = useResponsiveMemory;
		// Current priors remain the centre of the decision; memory is only a bounded
		// relative-performance adjustment scaled to the opportunity visible today.
		float projectedSingle = useResponsiveMemory
			? priorSingle + confidenceSingle * singleResponsive.Residual * Mathf.Max(1f, Mathf.Abs(priorSingle))
			: Mathf.Lerp(priorSingle, singleMemory.emaNetPerRelease, confidenceSingle);
		float projectedAlbum = useResponsiveMemory
			? priorAlbum + confidenceAlbum * albumResponsive.Residual * Mathf.Max(1f, Mathf.Abs(priorAlbum))
			: Mathf.Lerp(priorAlbum, albumMemory.emaNetPerRelease, confidenceAlbum);
		float projectedStandaloneAlbum = useResponsiveMemory && confidenceStandalone > 0f
			? priorAlbum + confidenceStandalone * standaloneResponsive.Residual * Mathf.Max(1f, Mathf.Abs(priorAlbum))
			: projectedAlbum;

		float noiseRange = Mathf.Lerp(0.50f, 0.15f, Mathf.Clamp(label.scoutingAbility, 0f, 1f));
		float singleNoiseMultiplier = 1f + (float)GD.RandRange(-noiseRange, noiseRange);
		float albumNoiseMultiplier = 1f + (float)GD.RandRange(-noiseRange, noiseRange);
		projectedSingle *= singleNoiseMultiplier;
		projectedAlbum *= albumNoiseMultiplier;
		projectedStandaloneAlbum *= albumNoiseMultiplier;
		// Commitment is a portfolio subsidy, not a revenue estimate, so it is applied
		// as an additive credit on a positive scale rather than as a scalar on a signed
		// net. Scaling the net inverted the intent: an Album projected to lose money is
		// the normal marginal case the commitment exists to carry, and multiplying it
		// pushed it further below the Single hurdle, harder every year as era weight
		// rose. The credit uses the same pre-noise prior scale as the memory residual
		// above, and stays outside the noise draw because it is policy, not estimation.
		//
		// Live-only. The commitment exists to stop the responsive lane split abandoning
		// Albums as the LP market matures — a failure mode the disabled route does not
		// have, because it has no lane split. Applying it there contaminated the gate
		// reference with a mechanism authored for the route being measured against it,
		// and the capacity derivation made that far worse than the old tier lookup: what
		// had reached only Major labels now reached every label, pushing the control's
		// Album decision share to .84/.91/.96 across 1965-69 and taking
		// scheduledAlbumProjects out of band at 1963.
		float albumPortfolioCommitment = useResponsiveMemory ? GetAlbumPortfolioCommitmentMultiplier(label, year) : 1f;
		float albumPortfolioCredit = CalculateAlbumPortfolioCredit(albumPortfolioCommitment, priorAlbum);
		projectedAlbum += albumPortfolioCredit;
		projectedStandaloneAlbum += albumPortfolioCredit;
		float singleFormatTilt = GetFormatPriorMultiplier(artist.primaryGenre, ReleaseFormat.Single, year);
		float singlePreTiltContribution = (priorSingle + decision.singleProductionCost) / Mathf.Max(.000001f, singleFormatTilt);
		float projectedLaunchAwareness = ProjectLaunchAwareness(label, artist, label.GetMarketingBudget(artist));
		float expectedPromoLift = (1f - Mathf.Clamp(projectedLaunchAwareness, 0f, 1f)) * expectedPromoLiftScalar;
		float meanAlbumDropGapWeeks = (albumDropGapWeeksMin + albumDropGapWeeksMax) * 0.5f;
		float expectedOverlapFraction = Mathf.Clamp(
			(expectedOverlapWeeks - meanAlbumDropGapWeeks) / Mathf.Max(1f, expectedOverlapWeeks), 0f, 1f);
		float expectedPromoSingleNet = CalculateSinglePriorNet(decision);
		float promoConfidence = useResponsiveMemory ? promoResponsive.Confidence : confidenceSingle;
		if (useResponsiveMemory) expectedPromoSingleNet += promoConfidence * promoResponsive.Residual * Mathf.Max(1f, Mathf.Abs(expectedPromoSingleNet));
		float expectedSingleUnits = Mathf.Max(0f,
			(expectedPromoSingleNet + decision.singleProductionCost) / Mathf.Max(singleNetMarginPerUnit, 0.000001f));
		float albumDemandFactor = CalculateAlbumDemandFactor(artist.primaryGenre, year);
		float substitutionPropensity = Mathf.Clamp(substitutionK * albumDemandFactor, 0f, substitutionCap);
		float divertedUnits = substitutionPropensity * expectedOverlapFraction * expectedSingleUnits;
		float cannibalizationLoss = divertedUnits * albumPrior.marginPerUnit;
		// A promo Single does not only divert Album buyers, it recruits them — that is
		// the entire reason a label runs one. Only the diversion was modelled, and it
		// scales with albumDemandFactor while the awareness lift is a fixed scalar, so
		// past a crossover year cannibalization exceeded the whole promo proposition
		// and the strategy became permanently non-viable market-wide. Recruitment now
		// scales on the same terms as diversion, so the two stay in proportion as the
		// LP market matures instead of one outgrowing the other without bound.
		//
		// Deliberately NOT gated to the live route, unlike the portfolio credit above.
		// The asymmetry it corrects is shared: cannibalizationLoss scales with
		// albumDemandFactor and expectedPromoLift is a fixed scalar on both routes, so
		// both model a promo Single that steals ever more from the Album and never sells
		// one. That is an error in the economics, not a defect in the live lane split.
		// Only the absorbing state it interacts with is live-specific, and that is fixed
		// separately in ResolveAlbumDecision. Gating this was measured and reverted: it
		// dropped the control's promo share to .37 by 1969, and since a promo project
		// emits two products and a standalone emits one, the control lost the Singles
		// those Album projects would have carried — Single units fell to 99.3M and took
		// totalUnits, grossRevenue, labelNet and marketNet out of band at 1968-69.
		float promoSynergyGain = CalculatePromoAlbumSynergyGain(albumDemandFactor,
			1f - Mathf.Clamp(projectedLaunchAwareness, 0f, 1f), expectedSingleUnits, albumPrior.marginPerUnit);
		float promoAdvantage = expectedPromoLift + promoSynergyGain + expectedPromoSingleNet - cannibalizationLoss;
		float componentProjectedAlbumWithPromo = projectedAlbum + promoAdvantage;
		float projectedAlbumWithPromo = componentProjectedAlbumWithPromo;
		if (useResponsiveMemory) projectedAlbumWithPromo += projectResponsive.Confidence * projectResponsive.Residual *
			Mathf.Max(1f, Mathf.Abs(projectedAlbumWithPromo));
		float promoProjectDelayPremium = meanAlbumDropGapWeeks / 52f;

		(bool economicAlbumWins, bool promoPreferred, float albumGateProjection) = useResponsiveMemory
			? ResolveAlbumDecision(projectedSingle, projectedAlbum, projectedStandaloneAlbum,
				componentProjectedAlbumWithPromo, projectedAlbumWithPromo, promoProjectDelayPremium,
				LiveAlbumDecisionEligibilityScale)
			: (projectedAlbum > projectedSingle, componentProjectedAlbumWithPromo > projectedAlbum, projectedAlbum);
		float decisionSingleHurdle = promoPreferred
			? projectedSingle + Mathf.Max(0f, promoProjectDelayPremium) * Mathf.Max(1f, Mathf.Abs(projectedSingle))
			: projectedSingle;
		float albumChoiceProbability = useResponsiveMemory
			? CalculateAlbumChoiceProbability(decisionSingleHurdle, albumGateProjection)
			: (economicAlbumWins ? 1f : 0f);
		float formatChoiceRoll = useResponsiveMemory
			? GetDeterministicFormatChoiceRoll(label.labelId, artist.artistId, year, currentWeek, annualFormatDecisions)
			: 0f;
		bool albumWins = useResponsiveMemory
			? ResolvePositiveFormatChoice(decisionSingleHurdle, albumGateProjection, formatChoiceRoll)
			: economicAlbumWins;
		bool albumProjectPressure = useResponsiveMemory &&
			IsAlbumProjectSharePressureHigh(annualFormatDecisions, annualAlbumProjectsScheduled);
		bool albumCapacityReroute = false;
		if (albumWins && !CanScheduleAnnualAlbumProject(
			annualAlbumProjectsByArtist.GetValueOrDefault((artist.artistId, year)), albumProjectPressure)) {
			albumWins = false;
			promoPreferred = false;
			albumCapacityReroute = true;
		}
		// Physical Album-component memory owns format eligibility. Standalone and
		// promo component memories rank the strategies. Total-project memory observes
		// the same component outcomes, so stacking its residual would count their
		// underperformance twice; it is only a fail-closed viability guard here.
		float decisionAlbumConfidence = confidenceAlbum;
		float decisionAlbumResidual = albumResponsive.Residual;

		ReleasePlan plan = new() {
			format = albumWins ? ReleaseFormat.Album : ReleaseFormat.Single,
			strategy = albumWins ? (promoPreferred ? ReleaseStrategy.AlbumWithPromo : ReleaseStrategy.AlbumStandalone) : ReleaseStrategy.OrphanSingle,
			economicsEvaluated = true,
			priorSingleNet = priorSingle,
			priorAlbumNet = priorAlbum,
			projectedSingleNet = projectedSingle,
			projectedAlbumNet = albumGateProjection,
			projectedOrphanSingleNet = projectedSingle,
			projectedAlbumStandaloneNet = projectedStandaloneAlbum,
			projectedAlbumWithPromoNet = albumWins ? projectedAlbumWithPromo : projectedAlbum,
			confidenceSingle = confidenceSingle,
			confidenceAlbum = decisionAlbumConfidence,
			rawConfidenceSingle = rawConfidenceSingle,
			rawConfidenceAlbum = rawConfidenceAlbum,
			singleMemoryCapApplied = applyLiveMemoryCeiling && rawConfidenceSingle > confidenceSingle,
			albumMemoryCapApplied = applyLiveMemoryCeiling && rawConfidenceAlbum > confidenceAlbum,
			legacyFourResolvableSingles = hitInventory.resolvedSingles >= 4,
			compCostWeight = compCostWeight,
			expectedFormatMultiplier = albumPrior.expectedFormatMultiplier,
			releasedSingleIdsExamined = hitInventory.idsExamined,
			resolvedSingles = hitInventory.resolvedSingles,
			chartedSingles = hitInventory.chartedSingles,
			hitScore = hitInventory.hitScore,
			unweightedHitUnits = albumPrior.unweightedHitUnits,
			weightedHitUnits = albumPrior.weightedHitUnits,
			affinityUnits = albumPrior.affinityUnits,
			totalExpectedAlbumUnits = albumPrior.totalExpectedUnits,
			qualityQuartile = decision.qualityQuartile,
			careerBand = decision.careerBand,
			unexpectedCareerState = decision.unexpectedCareerState,
			singleProductionCost = decision.singleProductionCost,
			singlePreTiltContribution = singlePreTiltContribution, singleFormatTilt = singleFormatTilt,
			albumAffinity = albumPrior.albumAffinity, albumOpportunity = albumPrior.albumOpportunity,
			albumFormatTilt = albumPrior.formatTilt, albumPreTiltContribution = albumPrior.preTiltAffinityUnits,
			albumProductionCost = albumPrior.productionCost,
			singleMemoryEma = useResponsiveMemory ? singleResponsive.Residual : singleMemory.emaNetPerRelease,
			albumMemoryEma = useResponsiveMemory ? decisionAlbumResidual : albumMemory.emaNetPerRelease,
			singleMemoryBlend = useResponsiveMemory ? projectedSingle : Mathf.Lerp(priorSingle, singleMemory.emaNetPerRelease, confidenceSingle),
			albumMemoryBlend = useResponsiveMemory ? albumGateProjection : Mathf.Lerp(priorAlbum, albumMemory.emaNetPerRelease, confidenceAlbum),
			labelFormatMemoryBypassed = decision.nonRetainedEmergingProject,
			singleNoiseMultiplier = singleNoiseMultiplier, albumNoiseMultiplier = albumNoiseMultiplier,
			expectedPromoSingleNet = expectedPromoSingleNet,
			albumStrategyEvaluated = albumWins,
			singleNetMarginPerUnit = singleNetMarginPerUnit,
			expectedSingleUnits = expectedSingleUnits,
			albumDemandFactor = albumDemandFactor,
			substitutionK = substitutionK,
			substitutionCap = substitutionCap,
			substitutionPropensity = substitutionPropensity,
			expectedOverlapFraction = expectedOverlapFraction,
			divertedUnits = divertedUnits,
			albumMarginPerUnit = albumPrior.marginPerUnit,
			cannibalizationLoss = cannibalizationLoss,
			expectedPromoLift = expectedPromoLift,
			promoAdvantage = promoAdvantage,
			albumChoiceProbability = albumChoiceProbability,
			formatChoiceRoll = formatChoiceRoll,
			albumCapacityReroute = albumCapacityReroute
		};
		return plan;
	}

	internal static (bool AlbumWins, bool PromoPreferred, float AlbumGateProjection) ResolveAlbumDecision(
		float projectedSingle, float projectedAlbumEligibility, float projectedStandaloneAlbum,
		float componentProjectedAlbumWithPromo, float totalProjectMemoryProjection, float promoProjectDelayPremium,
		float albumEligibilityScale = 1f) {
		// Viability is judged on current component economics, not on the memory-adjusted
		// projection. Gating it on total-project memory made the strategy self-trapping:
		// a negative lane residual vetoed promo everywhere at once, and because a vetoed
		// strategy generates no further evidence, the lane could never recover. Promo
		// share collapsed .53 -> .004 in one year with component promoAdvantage at
		// +24,763. Memory still ranks strategies through the component projections; it
		// no longer holds a permanent veto over one.
		bool promoPreferred = componentProjectedAlbumWithPromo > projectedStandaloneAlbum &&
			componentProjectedAlbumWithPromo > 0f;
		// A promo project consumes two release products. Preserve its calibrated
		// portfolio eligibility weight here; the physical Album component still
		// has to beat the orphan-Single alternative after the configured drop
		// delay's annualized opportunity cost. Total-project memory is viability-only.
		float albumGateProjection = promoPreferred
			? Mathf.Min(projectedAlbumEligibility, componentProjectedAlbumWithPromo * PromoProjectEligibilityWeight)
			: projectedAlbumEligibility;
		albumGateProjection *= Mathf.Max(0f, albumEligibilityScale);
		float delayHurdle = promoPreferred
			? projectedSingle + Mathf.Max(0f, promoProjectDelayPremium) * Mathf.Max(1f, Mathf.Abs(projectedSingle))
			: projectedSingle;
		return (albumGateProjection > delayHurdle, promoPreferred, albumGateProjection);
	}

	internal static float CalculateAlbumChoiceProbability(float projectedSingle, float projectedAlbum) {
		if (projectedAlbum <= 0f) return 0f;
		if (projectedSingle <= 0f) return 1f;
		float economicShare = projectedAlbum / Mathf.Max(.000001f, projectedSingle + projectedAlbum);
		// A bounded logistic preserves crossover choices near the economic fork
		// without letting the much larger population of weak Album propositions
		// accumulate a systemic project-count increase. Equal propositions remain
		// exactly 50/50 and the isolated roll preserves deterministic replay.
		float centeredShare = Mathf.Clamp(economicShare, 0f, 1f) - .5f;
		float logisticShare = 1f / (1f + Mathf.Exp(-FormatChoiceLogitSlope * centeredShare));
		return Mathf.Lerp(FormatChoiceExplorationFloor, 1f - FormatChoiceExplorationFloor, logisticShare);
	}

	internal static bool ResolvePositiveFormatChoice(float projectedSingle, float projectedAlbum, float roll) =>
		projectedAlbum > 0f && (projectedSingle <= 0f ||
			Mathf.Clamp(roll, 0f, 1f) < CalculateAlbumChoiceProbability(projectedSingle, projectedAlbum));

	internal static float GetDeterministicFormatChoiceRoll(
		string labelId, string artistId, int year, int week, int sequence) {
		uint hash = 2166136261u;
		foreach (char value in
			$"{SimulationSeedBootstrap.RequestedSeed ?? 0UL}|{labelId}|{artistId}|{year}|{week}|{sequence}|FormatChoiceV1") {
			hash ^= value;
			hash *= 16777619u;
		}
		return (hash & 0x00ffffffu) / 16777216f;
	}

	/// <summary>
	/// LP programs are portfolio commitments, not independent one-week products. The
	/// lane split's short-horizon component memory otherwise makes a label
	/// progressively abandon Albums exactly as the LP market matures.
	///
	/// Commitment is earned, not conferred by tier. Running an LP program needs shelf
	/// space to place it and roster depth to keep it fed; a label holding both commits
	/// like a major whatever its tier reads, and one holding neither stays a jobbing
	/// singles house. The former tier lookup was approximating this, but it reached
	/// only the Major tier — roughly a tenth of release volume — so the market-wide
	/// LP shift could not emerge from it.
	/// </summary>
	private static float GetAlbumPortfolioCommitmentMultiplier(AILabel label, int year) {
		float era = AlbumModel.GetAlbumEraWeight(year);
		if (label == null || era <= 0f) return 1f;
		return 1f + AlbumPortfolioCommitmentCeiling * era *
			CalculateAlbumPortfolioCapacity(label.distributionStrength, label.CurrentRosterSize);
	}

	/// <summary>Shared, fixed-input Album portfolio capacity seam for probes.</summary>
	internal static float CalculateAlbumPortfolioCapacity(float distributionStrength, int rosterSize) =>
		Mathf.Clamp(AlbumProgramReachWeight * Mathf.Clamp(distributionStrength, 0f, 1f) +
			(1f - AlbumProgramReachWeight) *
			Mathf.Clamp(rosterSize / AlbumProgramRosterDepth, 0f, 1f), 0f, 1f);

	internal static float GetAlbumPortfolioCommitmentMultiplierForProbe(
		float distributionStrength, int rosterSize, int year) =>
		1f + AlbumPortfolioCommitmentCeiling * AlbumModel.GetAlbumEraWeight(year) *
			CalculateAlbumPortfolioCapacity(distributionStrength, rosterSize);

	/// <summary>
	/// Album units recruited by a promo Single, on the same terms as the diverted
	/// units it is weighed against: both scale with album demand, the Single's reach
	/// and the Album's margin. Recruitment is gated on awareness headroom because a
	/// Single adds least where the launch is already well known, which is exactly
	/// where diversion is gated on shared shelf overlap instead.
	/// </summary>
	internal static float CalculatePromoAlbumSynergyGain(float albumDemandFactor,
		float awarenessHeadroom, float expectedSingleUnits, float albumMarginPerUnit) =>
		Mathf.Max(0f, PromoAlbumConversionK * Mathf.Max(0f, albumDemandFactor) *
			Mathf.Lerp(PromoAwarenessConversionFloor, 1f, Mathf.Clamp(awarenessHeadroom, 0f, 1f)) *
			Mathf.Max(0f, expectedSingleUnits) * albumMarginPerUnit);

	/// <summary>
	/// The Album prior's opportunity term is flat before `albumDemandRiseStartYear`
	/// while the Album market it predicts is already growing, so the prior overstates a
	/// niche early-decade LP and converges once the market matures. Decomposed against
	/// completed cohorts the error is on the revenue side — realized production cost is
	/// only ~9% of expected revenue — so it is genuinely a unit forecast error.
	///
	/// The correction is deliberately partial. Fitting it fully (.65/.66/.74/1.00, from
	/// measured revenue realization) aborts the gate at 1961 — 798 Album projects against
	/// 1257 — because the control's early-decade Album counts *require* an over-projecting
	/// prior. That is not obviously a defect: the prior is the label's belief, not ground
	/// truth, and early-60s A&R genuinely over-committed to LPs. The error also closes on
	/// its own as the market matures (realized/prior .949 by 1964) with the memory residual
	/// as the learning mechanism, so only the pre-boom years are damped.
	///
	/// Both endpoints are excluded. 1960 is the bootstrap year on a seeded catalog and
	/// already measures nearly correct (.913); 1964 onward is correct without help, and
	/// discounting it over-corrected to 1.106.
	///
	/// Applied on the live route only. It is a calibration of the live Album prior against
	/// live realized outcomes, so applying it to the disabled route would move the gate
	/// reference by the same amount it moves the thing being measured.
	/// </summary>
	internal static float CalculateAlbumPriorEraCalibration(int year) {
		if (year <= AlbumPriorCalibrationBootstrapYear) return 1f;
		if (year >= AlbumPriorCalibrationRetiredYear) return 1f;
		return Mathf.Lerp(AlbumPriorEarlyEraDiscount, 1f, AlbumModel.GetAlbumEraWeight(year));
	}

	/// <summary>
	/// Sign-safe portfolio commitment. Equivalent in magnitude to the former scalar
	/// when the projection sits near its prior, but always moves the Album up, so a
	/// marginal or negative-net proposition is carried rather than pushed away.
	/// </summary>
	internal static float CalculateAlbumPortfolioCredit(float commitmentMultiplier, float priorAlbum) =>
		Mathf.Max(0f, commitmentMultiplier - 1f) * Mathf.Max(1f, Mathf.Abs(priorAlbum));

	private void PrepareAnnualFormatCapacity(int year) {
		if (annualFormatCapacityYear == year) return;
		annualFormatCapacityYear = year;
		annualFormatDecisions = 0;
		annualAlbumProjectsScheduled = 0;
	}

	private static bool IsAlbumProjectSharePressureHigh(int decisions, int albumProjects) =>
		decisions >= AlbumProjectPressureMinimumDecisions &&
		(float)albumProjects / Mathf.Max(1, decisions) >= AlbumProjectPressureShare;
	private static bool CanScheduleAnnualAlbumProject(int projectsAlreadyScheduled, bool albumProjectPressure) =>
		!albumProjectPressure || projectsAlreadyScheduled < MaximumAlbumProjectsPerArtistYear;
	internal static bool IsAlbumProjectSharePressureHighForProbe(int decisions, int albumProjects) =>
		IsAlbumProjectSharePressureHigh(decisions, albumProjects);
	internal static bool CanScheduleAnnualAlbumProjectForProbe(int projectsAlreadyScheduled, bool albumProjectPressure) =>
		CanScheduleAnnualAlbumProject(projectsAlreadyScheduled, albumProjectPressure);

	private float ProjectLaunchAwareness(AILabel label, SimulatedArtist artist, float marketingBudget) {
		float artistAwareness = artist.GetNewReleaseAwarenessBonus();
		float marketingAwareness = GetSeasonalMarketingImpact(marketingBudget, label);
		return Mathf.Clamp(0.04f + artistAwareness + marketingAwareness + label.reputation * 0.1f, 0f, 1f);
	}

	private static readonly float[] SinglePriorQualityCutPoints = { 0.465511f, 0.550559f, 0.623968f };
	// Rows are Q1-Q4; columns are New/Unsigned, Rising, Established, Star/Superstar.
	// Sparse cells contain the prescribed borrowed effective value, not an implicit fallback.
	private static readonly float[,] SinglePriorNormalizedContribution = {
		{ 7765.792292f, 12767.924457f, 12767.924457f, 12767.924457f },
		{ 12230.041078f, 34084.882066f, 34084.882066f, 34084.882066f },
		{ 19197.010405f, 39072.383069f, 119155.241436f, 39072.383069f },
		{ 47135.125181f, 84751.249664f, 119155.241436f, 119155.241436f }
	};
	private static readonly int[,] SinglePriorRawSampleCounts = {
		{ 3481, 105, 3, 0 }, { 3452, 124, 8, 0 },
		{ 3380, 191, 16, 0 }, { 3156, 407, 22, 0 }
	};
	// Encoded as quality-row * 4 + career-column for the effective source bucket.
	private static readonly int[,] SinglePriorSourceBuckets = {
		{ 0, 1, 1, 1 }, { 4, 5, 5, 5 },
		{ 8, 9, 14, 9 }, { 12, 13, 14, 14 }
	};
	private static readonly int[,] SinglePriorSourceSampleCounts = {
		{ 3481, 105, 105, 105 }, { 3452, 124, 124, 124 },
		{ 3380, 191, 22, 191 }, { 3156, 407, 22, 22 }
	};

	private static DecisionContext BuildDecisionContext(AILabel label, SimulatedArtist artist, int year, int month) {
		float qualityEstimate = artist.CalculateBaseQuality();
		float recordingCostMultiplier = MarketSeasonality.Enabled
			? MarketSeasonality.GetRecordingCostMultiplier(year, month, liveTick: true) : 1f;
		float singleProductionCost = MarketSeasonality.Enabled
			? label.GetProductionCost() * recordingCostMultiplier : label.GetProductionCost();
		return new DecisionContext {
			qualityEstimate = qualityEstimate,
			reachFactor = Mathf.Max(0f, label.distributionStrength),
			genreSinglesMarketFactor = CalculateSingleGenreMarketFactor(artist.primaryGenre, year),
			singleProductionCost = singleProductionCost,
			recordingCostMultiplier = recordingCostMultiplier
		};
	}

	private static float CalculateSinglePriorNet(DecisionContext decision) {
		float expectedContribution = SinglePriorNormalizedContribution[decision.qualityQuartile, decision.careerBand]
			* decision.reachFactor * decision.genreSinglesMarketFactor;
		return expectedContribution - decision.singleProductionCost;
	}

	private float CalculateAlbumPriorNet(AILabel label, SimulatedArtist artist, int year, DecisionContext decision,
		float compCostWeight, HitInventory hitInventory, out AlbumPriorDiagnostics diagnostics) {
		float statureMultiplier = artist.careerState switch {
			CareerState.Superstar => 2.5f, CareerState.Star => 2.0f, CareerState.Established => 1.5f,
			CareerState.Rising => 1.2f, _ => 1.0f
		};
		IEnumerable<MarketRegion> regions = ChartManager.Instance != null ? ChartManager.Instance.GetAllRegions() : Enumerable.Empty<MarketRegion>();
		bool live = GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true;
		AlbumPriorExplanation opportunity = GetAlbumPriorExplanation(artist.primaryGenre, regions, year, live);
		float baseAffinityUnits = priorUnitScalarAlbum * decision.qualityEstimate * statureMultiplier *
			decision.reachFactor * (live ? CalculateAlbumPriorEraCalibration(year) : 1f);
		float preTiltAffinityUnits = baseAffinityUnits * opportunity.UntiltedAlbumDemandFactor * opportunity.MarketReconciliation;
		float unweightedHitUnits = priorCompHitUnitScalar * hitInventory.hitScore;
		float weightedHitUnits = compCostWeight * unweightedHitUnits;
		// Format suitability applies to the whole Album proposition. Applying it
		// only to affinity units let compilation/hit inventory bypass the catalog
		// orientation and dominate the late format fork.
		float preTiltExpectedUnits = preTiltAffinityUnits + weightedHitUnits;
		float affinityUnits = preTiltAffinityUnits * opportunity.FormatTilt;
		float expectedUnits = CalculateFormatTiltedAlbumExpectedUnits(
			preTiltAffinityUnits, weightedHitUnits, opportunity.FormatTilt);
		float manufacturingPerUnit = GetPressingCostPerUnit(ReleaseFormat.Album) + albumPackagingCostPerUnit * priorAssumedAlbumPackaging;
		float grossAfterManufacturing = Mathf.Max(0f, GetPricePerUnit(ReleaseFormat.Album) - manufacturingPerUnit);
		float skimFraction = label.activeDeal != null
			? Mathf.Clamp(label.activeDeal.marginSkim, 0f, 1f)
			: 0.25f * (1f - label.ownedReach);
		float royaltyRate = artist?.royaltyRate ?? baseRoyaltyRate;
		float marginPerUnit = grossAfterManufacturing * (1f - skimFraction) - GetPricePerUnit(ReleaseFormat.Album) * royaltyRate;
		float multiplier = compCostWeight * compilationProductionMultiplier + (1f - compCostWeight) * 2.4f;
		float productionCost = MarketSeasonality.Enabled
			? label.GetProductionCost() * decision.recordingCostMultiplier * multiplier + albumPackagingFixedCost * priorAssumedAlbumPackaging
			: label.GetProductionCost() * multiplier + albumPackagingFixedCost * priorAssumedAlbumPackaging;
		float expectedRevenueAtMargin = expectedUnits * marginPerUnit;
		diagnostics = new AlbumPriorDiagnostics {
			expectedFormatMultiplier = multiplier,
			affinityUnits = affinityUnits,
			unweightedHitUnits = unweightedHitUnits,
			weightedHitUnits = weightedHitUnits,
			totalExpectedUnits = expectedUnits,
			expectedRevenueAtMargin = expectedRevenueAtMargin,
			marginPerUnit = marginPerUnit,
			albumAffinity = opportunity.AlbumAffinity,
			albumOpportunity = opportunity.UntiltedAlbumDemandFactor,
			formatTilt = opportunity.FormatTilt,
			preTiltAffinityUnits = preTiltExpectedUnits,
			productionCost = productionCost
		};
		return expectedRevenueAtMargin - productionCost;
	}

	internal static float CalculateFormatTiltedAlbumExpectedUnits(
		float preTiltAffinityUnits, float weightedHitUnits, float formatTilt) =>
		Mathf.Max(0f, preTiltAffinityUnits + weightedHitUnits) * Mathf.Max(0f, formatTilt);

	private static int GetQualityQuartile(float qualityEstimate) {
		if (qualityEstimate <= SinglePriorQualityCutPoints[0]) return 0;
		if (qualityEstimate <= SinglePriorQualityCutPoints[1]) return 1;
		if (qualityEstimate <= SinglePriorQualityCutPoints[2]) return 2;
		return 3;
	}

	private float GetExpectedPeakScore(int qualityQuartile, int careerBand) {
		int index = Mathf.Clamp(qualityQuartile, 0, 3) * 4 + Mathf.Clamp(careerBand, 0, 3);
		return expectedPeakScoreByBucket != null && index < expectedPeakScoreByBucket.Length
			? Mathf.Clamp(expectedPeakScoreByBucket[index], 0f, 1f) : 0f;
	}

	private static float CalculatePromoPeakScore(float peakPosition, int flopThreshold) =>
		peakPosition <= 0f || peakPosition > flopThreshold ? 0f :
			(flopThreshold - peakPosition) / Mathf.Max(1f, flopThreshold - 1f);

	private static int GetCareerBandIndex(CareerState state, out bool unexpected) {
		unexpected = false;
		return state switch {
			CareerState.NewSigning or CareerState.Unsigned => 0,
			CareerState.Rising => 1,
			CareerState.Established => 2,
			CareerState.Star or CareerState.Superstar => 3,
			_ => UnexpectedCareerFallback(out unexpected)
		};
	}

	internal static bool IsEligibleForEnabledFormatDecision(SimulatedArtist artist) =>
		GenreSupplyService.IsEligibleExistingArtistForEnabledRelease(artist);

	private static int UnexpectedCareerFallback(out bool unexpected) { unexpected = true; return 0; }
	private static string GetCareerBandLabel(int band, bool unexpected) => unexpected ? "New/Unsigned (unexpected-state fallback)" :
		band switch { 0 => "New/Unsigned", 1 => "Rising", 2 => "Established", _ => "Star/Superstar" };

	private float CalculateProductionCost(AILabel label, Record record, GameDate date) {
		float baseCost = MarketSeasonality.Enabled
			? label.GetProductionCost() * MarketSeasonality.GetRecordingCostMultiplier(date.year, date.month, liveTick: true)
			: label.GetProductionCost();
		if (record?.format != ReleaseFormat.Album) return baseCost;
		float multiplier = record.album?.albumFormat == AlbumFormat.Compilation
			? compilationProductionMultiplier
			: 2.4f;
		return baseCost * multiplier + albumPackagingFixedCost * (record.album?.packaging ?? 0f);
	}

	private static float CalculateCompilationCostWeight(Genre genre, int year) {
		if (!IsGeneratorAdultGenre(genre)) return 1f;
		return year <= 1963 ? 0.48f : 0f;
	}

	private static bool IsGeneratorAdultGenre(Genre genre) => genre is Genre.Jazz or Genre.EasyListening or Genre.Folk or
		Genre.TraditionalPop or Genre.BossaNova or Genre.Country;

	private HitInventory ResolveHitInventory(SimulatedArtist artist) {
		var result = new HitInventory();
		if (artist?.releasedSingleIds == null || ChartManager.Instance == null) return result;
		foreach (string recordId in artist.releasedSingleIds.AsEnumerable().Reverse()) {
			result.idsExamined++;
			if (!ChartManager.Instance.TryGetTrackSnapshot(recordId, out AlbumTrack track)) continue;
			result.resolvedSingles++;
			if (track.peakPosition is >= 1 and <= 100) {
				result.chartedSingles++;
				float freshness = GetSourceHitFreshness(recordId, track);
				result.hitScore += freshness * (101f - track.peakPosition) / 100f;
			}
			if (result.resolvedSingles >= 4) break;
		}
		return result;
	}

	private float GetSourceHitFreshness(string recordId, AlbumTrack track = null) {
		float perUseFreshness = ChartManager.Instance?.GetCompFreshness(recordId) ?? 1f;
		if (track == null && (ChartManager.Instance == null ||
			!ChartManager.Instance.TryGetTrackSnapshot(recordId, out track))) return perUseFreshness;
		if (track.releaseDate.year <= 0 || TimeManager.Instance == null) return perUseFreshness;

		GameDate currentDate = TimeManager.Instance.CurrentDate;
		int ageWeeks = currentDate >= track.releaseDate ? currentDate.WeeksDifference(track.releaseDate) : 0;
		float annualDecay = Mathf.Clamp(hitRecencyDecay, 0f, 1f);
		return perUseFreshness * Mathf.Pow(annualDecay, ageWeeks / 52f);
	}

	private static float CalculateSingleGenreMarketFactor(Genre genre, int year) {
		IEnumerable<MarketRegion> regions = ChartManager.Instance != null
			? ChartManager.Instance.GetAllRegions()
			: Enumerable.Empty<MarketRegion>();
		MarketRegion[] regionArray = regions.ToArray();
		if (regionArray.Length == 0) return 1f;
		float selected = regionArray.Sum(region => region.GetGenreMarketSize(genre, year));
		// New enabled projects can only be supplied by this year's lifecycle-filtered
		// catalog. Including unavailable future genres in the comparison pool lowers
		// the denominator and turns nearly every live decision into the cap.
		IReadOnlyList<Genre> genres = GenreMarketV2.Enabled
			? GenreSupplyService.GetAvailableGenres(year)
			: GenreDomains.Current;
		float relativeMarket = CalculateRelativeSingleMarketFactor(selected,
			genres.Select(candidate => regionArray.Sum(region => region.GetGenreMarketSize(candidate, year))));
		return relativeMarket * GetFormatPriorMultiplier(genre, ReleaseFormat.Single, year);
	}

	/// <summary>Shared, fixed-input relative-market seam for live AI decisions and audit probes.</summary>
	public static float CalculateRelativeSingleMarketFactor(float selectedMarket, IEnumerable<float> comparisonMarkets) {
		float[] markets = comparisonMarkets?.ToArray() ?? System.Array.Empty<float>();
		if (markets.Length == 0) return 1f;
		float average = markets.Average();
		return Mathf.Clamp(selectedMarket / Mathf.Max(1f, average), 0.70f, 1.30f);
	}

	/// <summary>AI format priors deliberately share the realized demand tilt seam.</summary>
	public static float GetFormatPriorMultiplier(Genre genre, ReleaseFormat format, int year,
		bool? liveOverride = null, float? albumOpportunityOverride = null) {
		bool live = liveOverride ?? (GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true);
		return GenreAcceptanceService.GetLiveFormatMultiplier(genre, genre, format, year,
			albumOpportunityOverride ?? GetNationalAlbumOpportunity(genre, year, live), live);
	}

	private static float GetNationalAlbumOpportunity(Genre genre, int year, bool live) {
		MarketRegion[] regions = ChartManager.Instance?.GetAllRegions()?.Where(region => region != null).ToArray()
			?? System.Array.Empty<MarketRegion>();
		if (regions.Length == 0) return .5f;
		return live
			? CalculateEnabledAlbumOpportunityFactor(genre, regions, year)
			: CalculateAcceptedAlbumOpportunityFactor(genre, regions, year);
	}

	/// <summary>
	/// Shared fixed-input Album opportunity. Both format centering and the Album
	/// AI prior use the accepted pre-tilt Album pool over the accepted legacy
	/// genre pool; neither side may substitute the enabled routed genre market.
	/// </summary>
	public static float CalculateAcceptedAlbumOpportunityFactor(Genre genre, IEnumerable<MarketRegion> regions, float year) {
		MarketRegion[] regionArray = regions?.Where(region => region != null).ToArray() ?? System.Array.Empty<MarketRegion>();
		float acceptedAlbumPool = regionArray.Sum(region => region.GetAcceptedPreTiltAlbumMarketSize(genre, year));
		float acceptedGenrePool = regionArray.Sum(region => region.GetAcceptedLegacyGenreMarketSize(genre, year));
		return Mathf.Clamp(acceptedAlbumPool / Mathf.Max(1f, acceptedGenrePool), 0f, 1f);
	}

	/// <summary>Enabled Album opportunity, weighted by the routed canonical genre market.</summary>
	public static float CalculateEnabledAlbumOpportunityFactor(Genre genre, IEnumerable<MarketRegion> regions, float year) {
		MarketRegion[] regionArray = regions?.Where(region => region != null).ToArray() ?? System.Array.Empty<MarketRegion>();
		float routedGenrePool = regionArray.Sum(region => region.GetGenreMarketSize(genre, (int)year));
		if (routedGenrePool <= 0f) return 0f;
		float weightedOpportunity = regionArray.Sum(region =>
			region.GetGenreMarketSize(genre, (int)year) * region.GetEnabledAlbumOpportunityWeight(genre, year));
		return Mathf.Clamp(weightedOpportunity / routedGenrePool, 0f, 1f);
	}

	public readonly struct AlbumPriorExplanation {
		public readonly float AcceptedAlbumPool, AcceptedLegacyGenrePool, AlbumAffinity, UntiltedAlbumDemandFactor;
		public readonly float MarketReconciliation, FormatTilt, AlbumPrior;
		public AlbumPriorExplanation(float acceptedAlbumPool, float acceptedLegacyGenrePool, float untiltedAlbumDemandFactor,
			float albumAffinity, float marketReconciliation, float formatTilt, float albumPrior) {
			AcceptedAlbumPool = acceptedAlbumPool;
			AcceptedLegacyGenrePool = acceptedLegacyGenrePool;
			AlbumAffinity = albumAffinity;
			UntiltedAlbumDemandFactor = untiltedAlbumDemandFactor;
			MarketReconciliation = marketReconciliation;
			FormatTilt = formatTilt;
			AlbumPrior = albumPrior;
		}
	}

	/// <summary>Fixed-input decomposition for the Album AI-prior audit seam.</summary>
	public static AlbumPriorExplanation GetAlbumPriorExplanation(Genre genre, IEnumerable<MarketRegion> regions, int year, bool live) {
		MarketRegion[] regionArray = regions?.Where(region => region != null).ToArray() ?? System.Array.Empty<MarketRegion>();
		float acceptedAlbumPool = regionArray.Sum(region => region.GetAcceptedPreTiltAlbumMarketSize(genre, year));
		float acceptedGenrePool = regionArray.Sum(region => region.GetAcceptedLegacyGenreMarketSize(genre, year));
		float albumAffinity;
		if (live) {
			float routedGenrePool = regionArray.Sum(region => region.GetGenreMarketSize(genre, year));
			albumAffinity = regionArray.Sum(region =>
				region.GetGenreMarketSize(genre, year) * region.GetAlbumAffinity(genre, year)) /
				Mathf.Max(1f, routedGenrePool);
		} else {
			albumAffinity = regionArray.Sum(region =>
				region.GetAcceptedLegacyGenreMarketSize(genre, year) * region.GetAlbumAffinity(genre, year)) /
				Mathf.Max(1f, acceptedGenrePool);
		}
		float untilted = live
			? CalculateEnabledAlbumOpportunityFactor(genre, regionArray, year)
			: Mathf.Clamp(acceptedAlbumPool / Mathf.Max(1f, acceptedGenrePool), 0f, 1f);
		float marketReconciliation = live
			? CalculateAlbumPriorMarketReconciliation(genre, regionArray, year)
			: 1f;
		float formatTilt = GetFormatPriorMultiplier(genre, ReleaseFormat.Album, year, live, untilted);
		return new AlbumPriorExplanation(acceptedAlbumPool, acceptedGenrePool, untilted, albumAffinity,
			marketReconciliation, formatTilt, untilted * marketReconciliation * formatTilt);
	}

	/// <summary>
	/// Live Album and Single priors use the same routed relative-market factor.
	/// The Album prior's unit scalars were calibrated against the legacy relative
	/// market, so a genre that has one must still be renormalized by it; dropping
	/// that divisor handed every large legacy genre an unearned Album uplift at the
	/// fork. Genres with no legacy comparator keep the bare routed factor, which is
	/// what the divisor form could not express without erasing their Album affinity.
	/// </summary>
	public static float CalculateAlbumPriorMarketReconciliation(Genre genre, IEnumerable<MarketRegion> regions, int year) {
		MarketRegion[] regionArray = regions?.Where(region => region != null).ToArray() ?? System.Array.Empty<MarketRegion>();
		if (regionArray.Length == 0) return 1f;
		float routedSelected = regionArray.Sum(region => region.GetGenreMarketSize(genre, year));
		IReadOnlyList<Genre> supplied = GenreSupplyService.GetAvailableGenres(year);
		float routedRelative = CalculateRelativeSingleMarketFactor(routedSelected,
			supplied.Select(candidate => regionArray.Sum(region => region.GetGenreMarketSize(candidate, year))));
		float acceptedSelected = regionArray.Sum(region => region.GetAcceptedLegacyGenreMarketSize(genre, year));
		if (acceptedSelected <= 0f) return routedRelative;
		float acceptedRelative = CalculateRelativeSingleMarketFactor(acceptedSelected,
			GenreDomains.LegacyDomain.Select(candidate => regionArray.Sum(region => region.GetAcceptedLegacyGenreMarketSize(candidate, year))));
		return Mathf.Clamp(routedRelative / Mathf.Max(.000001f, acceptedRelative), .25f, 4f);
	}

	/// <summary>Side-effect-free binary format decision decomposition for fixed probes.</summary>
	public readonly struct FormatDecisionExplanation {
		public readonly float SinglePreTiltContribution, AlbumPreTiltContribution, AlbumAffinity, AlbumOpportunity;
		public readonly float SingleTilt, AlbumTilt, SingleProductionCost, AlbumProductionCost;
		public readonly float SingleMemoryBlend, AlbumMemoryBlend, SingleNoise, AlbumNoise, FinalSingleMargin, FinalAlbumMargin;
		public readonly ReleaseFormat Choice;
		public FormatDecisionExplanation(float singlePreTiltContribution, float albumPreTiltContribution, float albumAffinity,
			float albumOpportunity, float singleTilt, float albumTilt, float singleProductionCost, float albumProductionCost,
			float singleMemoryBlend, float albumMemoryBlend, float singleNoise, float albumNoise) {
			SinglePreTiltContribution = singlePreTiltContribution;
			AlbumPreTiltContribution = albumPreTiltContribution;
			AlbumAffinity = albumAffinity;
			AlbumOpportunity = albumOpportunity;
			SingleTilt = singleTilt;
			AlbumTilt = albumTilt;
			SingleProductionCost = singleProductionCost;
			AlbumProductionCost = albumProductionCost;
			SingleMemoryBlend = singleMemoryBlend;
			AlbumMemoryBlend = albumMemoryBlend;
			SingleNoise = singleNoise;
			AlbumNoise = albumNoise;
			FinalSingleMargin = singleMemoryBlend * singleNoise;
			FinalAlbumMargin = albumMemoryBlend * albumNoise;
			Choice = FinalAlbumMargin > FinalSingleMargin ? ReleaseFormat.Album : ReleaseFormat.Single;
		}
	}

	public static FormatDecisionExplanation ExplainFixedFormatDecision(float singlePreTiltContribution, float albumPreTiltContribution,
		float albumAffinity, float albumOpportunity, float singleTilt, float albumTilt, float singleProductionCost,
		float albumProductionCost, float singleMemory = 0f, float albumMemory = 0f, float singleNoise = 1f, float albumNoise = 1f) {
		float singlePrior = singlePreTiltContribution * singleTilt - singleProductionCost;
		float albumPrior = albumPreTiltContribution * albumTilt - albumProductionCost;
		float singleBlend = singleMemory == 0f ? singlePrior : singleMemory;
		float albumBlend = albumMemory == 0f ? albumPrior : albumMemory;
		return new FormatDecisionExplanation(singlePreTiltContribution, albumPreTiltContribution, albumAffinity, albumOpportunity, singleTilt, albumTilt,
			singleProductionCost, albumProductionCost, singleBlend, albumBlend, singleNoise, albumNoise);
	}

	public static float CalculateAlbumDemandFactor(Genre genre, int year) {
		IEnumerable<MarketRegion> regions = ChartManager.Instance != null
			? ChartManager.Instance.GetAllRegions()
			: Enumerable.Empty<MarketRegion>();
		bool live = GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true;
		return GetAlbumPriorExplanation(genre, regions, year, live).AlbumPrior;
	}

	private struct ReleasePlan {
		public ReleaseFormat format;
		public ReleaseStrategy strategy;
		public bool economicsEvaluated;
		public float priorSingleNet;
		public float priorAlbumNet;
		public float projectedSingleNet;
		public float projectedAlbumNet;
		public float projectedOrphanSingleNet;
		public float projectedAlbumStandaloneNet;
		public float projectedAlbumWithPromoNet;
		public float expectedPromoSingleNet;
		public bool albumStrategyEvaluated;
		public float singleProductionCost;
		public float singleNetMarginPerUnit;
		public float expectedSingleUnits;
		public float albumDemandFactor;
		public float substitutionK;
		public float substitutionCap;
		public float substitutionPropensity;
		public float expectedOverlapFraction;
		public float divertedUnits;
		public float albumMarginPerUnit;
		public float cannibalizationLoss;
		public float expectedPromoLift;
		public float promoAdvantage;
		public float albumChoiceProbability;
		public float formatChoiceRoll;
		public bool albumCapacityReroute;
		public float singlePreTiltContribution;
		public float singleFormatTilt;
		public float albumAffinity;
		public float albumOpportunity;
		public float albumFormatTilt;
		public float albumPreTiltContribution;
		public float albumProductionCost;
		public float singleMemoryEma;
		public float albumMemoryEma;
	public float singleMemoryBlend;
	public float albumMemoryBlend;
	public bool labelFormatMemoryBypassed;
	public float singleNoiseMultiplier;
		public float albumNoiseMultiplier;
		public float confidenceSingle;
		public float confidenceAlbum;
		public float rawConfidenceSingle;
		public float rawConfidenceAlbum;
		public bool singleMemoryCapApplied;
		public bool albumMemoryCapApplied;
		public bool legacyFourResolvableSingles;
		public float compCostWeight;
		public float expectedFormatMultiplier;
		public int releasedSingleIdsExamined;
		public int resolvedSingles;
		public int chartedSingles;
		public float hitScore;
		public float unweightedHitUnits;
		public float weightedHitUnits;
		public float affinityUnits;
		public float totalExpectedAlbumUnits;
		public int qualityQuartile;
		public int careerBand;
		public bool unexpectedCareerState;
	}

	private struct HitInventory {
		public int idsExamined;
		public int resolvedSingles;
		public int chartedSingles;
		public float hitScore;
	}

	private struct AlbumPriorDiagnostics {
		public float expectedFormatMultiplier;
		public float affinityUnits;
		public float unweightedHitUnits;
		public float weightedHitUnits;
		public float totalExpectedUnits;
		public float expectedRevenueAtMargin;
		public float marginPerUnit;
		public float albumAffinity;
		public float albumOpportunity;
		public float formatTilt;
		public float preTiltAffinityUnits;
		public float productionCost;
	}

	private struct DecisionContext {
		public float qualityEstimate;
		public int qualityQuartile;
		public int careerBand;
		public bool unexpectedCareerState;
		public float reachFactor;
		public float genreSinglesMarketFactor;
		public float singleProductionCost;
		public float recordingCostMultiplier;
		public bool nonRetainedEmergingProject;
	}
	
	private Record GenerateRecordFromArtist(AILabel label, SimulatedArtist artist, int year, ReleaseFormat format = ReleaseFormat.Single) {
		var record = new Record(); // Godot Resource instantiation
		generatedRecordCounter++;
		record.recordId = $"gen_{generatedRecordCounter}";
		record.labelId = label.labelId;
		record.format = format;
		record.isPlayerOwned = false;
		record.artistName = artist.stageName;
		record.artistId = artist.artistId;
		record.primaryGenre = artist.primaryGenre;
		record.secondaryGenre = artist.secondaryGenre;
		
		if (NameGenerator.Instance != null) {
			record.title = NameGenerator.Instance.GenerateSongTitle(record.primaryGenre, year, record.artistName);
		} else {
			record.title = $"Song {generatedRecordCounter}";
		}
		
		float artistQuality = artist.CalculateRecordQuality();
		float studioMod = 1f;
		if (label.strongRegions != null && label.strongRegions.Length > 0) {
			var region = ChartManager.Instance?.GetRegionById(label.strongRegions[0]);
			if (region != null) studioMod = ChartSimulator.GetStudioQualityModifier(region);
		}
		
		float baseQuality = (artistQuality * 0.82f) + (studioMod * 0.18f);
		baseQuality *= studioMod;
		
		record.hookStrength = Mathf.Clamp((artist.songwritingAbility * 0.55f) + (baseQuality * 0.35f) + (float)GD.RandRange(-0.12, 0.18), 0f, 1f);
		record.productionQuality = Mathf.Clamp((label.productionQuality * 0.4f) + (artist.studioPerformance * 0.3f) + (studioMod * 0.2f) + (float)GD.RandRange(-0.05, 0.1), 0f, 1f);
		record.originality = Mathf.Clamp(artist.members.Max(m => m.creativity) * 0.7f + (float)GD.RandRange(0f, 0.3f), 0f, 1f);
		record.danceability = (float)GD.RandRange(0.3, 0.95);
		record.controversy = (float)GD.RandRange(0f, 0.2f);
		
		if (record.primaryGenre == Genre.Gospel) record.controversy = Mathf.Min(record.controversy, 0.05f);
		else if (record.primaryGenre == Genre.RockAndRoll || record.primaryGenre == Genre.GarageRock) record.danceability = Mathf.Max(record.danceability, 0.5f);

		if (format == ReleaseFormat.Album) {
			record.album = GenerateAlbum(label, artist, year);
			record.title = GenerateAlbumTitle(record, year);
			record.hookStrength = record.album.pooledAppeal;
			record.productionQuality = Mathf.Clamp(record.album.pooledAppeal * 0.75f + label.productionQuality * 0.25f, 0f, 1f);
			record.danceability = record.album.pooledAppeal;
		}
		
		return record;
	}

	private Album GenerateAlbum(AILabel label, SimulatedArtist artist, int year) {
		bool useStructuredPromoTracks = GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true;
		bool adultGenre = artist.primaryGenre is Genre.Jazz or Genre.EasyListening or Genre.Folk or
			Genre.TraditionalPop or Genre.BossaNova or Genre.Country;
		float artistTalent = artist.CalculateBaseQuality();
		float luckyRoll = GD.Randf();
		float cohesionCeiling = AlbumModel.GetMaximumAchievableCohesion(year, artistTalent, label.productionQuality, luckyRoll);
		float thematicCohesion = Mathf.Clamp((float)GD.RandRange(0.10, cohesionCeiling), 0f, cohesionCeiling);

		AlbumFormat albumFormat;
		bool statementViable = cohesionCeiling >= 0.72f && thematicCohesion >= 0.62f;
		if (statementViable && ((year >= 1965 && GD.Randf() < 0.24f) || (year < 1965 && luckyRoll > 0.985f))) {
			albumFormat = AlbumFormat.Concept;
			thematicCohesion = Mathf.Max(thematicCohesion, 0.68f);
		} else if (!adultGenre || (year <= 1963 && GD.Randf() < 0.48f)) {
			albumFormat = AlbumFormat.Compilation;
		} else {
			float typeRoll = GD.Randf();
			albumFormat = typeRoll < 0.12f ? AlbumFormat.Soundtrack : typeRoll < 0.24f ? AlbumFormat.Live : AlbumFormat.Standard;
		}

		var referencedSingles = new List<AlbumTrack>();
		if (albumFormat == AlbumFormat.Compilation) {
			foreach (string recordId in artist.releasedSingleIds.AsEnumerable().Reverse()) {
				if (referencedSingles.Count >= 4) break;
				if (ChartManager.Instance.TryResolveTrackSnapshot(recordId, out AlbumTrack track, out _)) referencedSingles.Add(track);
			}
		}

		int targetTracks = (int)GD.RandRange(9, 13);
		var nonSingleTracks = new List<AlbumTrack>();
		float originalMaterialScale = albumFormat == AlbumFormat.Compilation ? 0.68f : albumFormat == AlbumFormat.Live ? 0.80f : 0.88f;
		while (referencedSingles.Count + nonSingleTracks.Count < targetTracks) {
			float trackQuality = Mathf.Clamp(artistTalent * originalMaterialScale + label.productionQuality * 0.12f + (float)GD.RandRange(-0.16, 0.12), 0.12f, 0.95f);
			string trackTitle = NameGenerator.Instance?.GenerateSongTitle(artist.primaryGenre, year, artist.stageName) ?? $"Album Track {nonSingleTracks.Count + 1}";
			(float hook, float production, float dance) = useStructuredPromoTracks
				? GetDeterministicTrackTraits(trackQuality, trackTitle, artist.primaryGenre)
				: (0f, 0f, 0f);
			nonSingleTracks.Add(new AlbumTrack {
				title = trackTitle,
				genre = artist.primaryGenre,
				quality = trackQuality,
				hookStrength = hook, productionQuality = production, danceability = dance,
				isReleasedSingle = false
			});
		}

		float avgTrackMinutes = (float)GD.RandRange(2.45, year >= 1967 ? 4.10 : 3.35);
		float[] referencedFreshness = referencedSingles
			.Select(track => GetSourceHitFreshness(track.sourceRecordId, track)).ToArray();
		int[] referencedCompUses = referencedSingles
			.Select(track => ChartManager.Instance.GetCompUseCount(track.sourceRecordId)).ToArray();
		var album = new Album {
			albumId = $"album_{generatedRecordCounter}",
			albumFormat = albumFormat,
			trackRefs = referencedSingles.ToArray(),
			trackRefFreshnessApplied = referencedFreshness,
			trackRefCompUsesAtGeneration = referencedCompUses,
			nonSingleTracks = nonSingleTracks.ToArray(),
			runtimeMinutes = targetTracks * avgTrackMinutes,
			thematicCohesion = thematicCohesion,
			packaging = Mathf.Clamp(label.productionQuality * Mathf.Lerp(0.35f, 0.85f, AlbumModel.GetAlbumEraWeight(year)) + (float)GD.RandRange(-0.10, 0.12), 0.05f, 1f),
			isStereo = year >= 1968 || GD.Randf() < Mathf.Lerp(0.12f, 0.75f, Mathf.Clamp((year - 1960f) / 8f, 0f, 1f))
		};
		IEnumerable<float> qualities = referencedSingles.Select((track, index) => track.quality * referencedFreshness[index])
			.Concat(nonSingleTracks.Select(track => track.quality));
		album.pooledAppeal = AlbumModel.CalculatePooledAppeal(qualities, album.thematicCohesion, year);
		return album;
	}

	private static string GenerateAlbumTitle(Record record, int year) {
		string generated = NameGenerator.Instance?.GenerateSongTitle(record.primaryGenre, year, record.artistName);
		return string.IsNullOrWhiteSpace(generated) ? $"{record.artistName} Album" : generated;
	}

	private Record CreatePromoSingleFromAlbum(Record albumRecord) {
		Album album = albumRecord.album ?? throw new System.InvalidOperationException("Promo project requires a generated album.");
		if (album.nonSingleTracks == null || album.nonSingleTracks.Length == 0) throw new System.InvalidOperationException("Promo project album has no eligible original track.");
		bool useStructuredPromoTracks = GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true;
		int bestIndex = 0;
		for (int i = 1; i < album.nonSingleTracks.Length; i++) {
			if (useStructuredPromoTracks) {
				int selectionYear = albumRecord.releaseDate.year > 0 ? albumRecord.releaseDate.year : (TimeManager.Instance?.CurrentDate.year ?? 1960);
				float candidate = GetLeadSingleSuitability(album.nonSingleTracks[i], albumRecord.primaryGenre, selectionYear);
				float current = GetLeadSingleSuitability(album.nonSingleTracks[bestIndex], albumRecord.primaryGenre, selectionYear);
				if (candidate > current || (Mathf.IsEqualApprox(candidate, current) && string.CompareOrdinal(album.nonSingleTracks[i].title, album.nonSingleTracks[bestIndex].title) < 0)) bestIndex = i;
			} else if (album.nonSingleTracks[i].quality > album.nonSingleTracks[bestIndex].quality) {
				// Preserve the frozen disabled branch exactly: max scalar quality wins,
				// with the first occurrence retaining ties.
				bestIndex = i;
			}
		}
		AlbumTrack source = album.nonSingleTracks[bestIndex];
		(float hook, float production, float dance) = useStructuredPromoTracks
			? GetTrackTraitsForPromo(source)
			: (source.quality, source.quality, source.quality);
		var promo = new Record {
			recordId = $"gen_{++generatedRecordCounter}", title = source.title, artistName = albumRecord.artistName,
			artistId = albumRecord.artistId, labelId = albumRecord.labelId, format = ReleaseFormat.Single,
			isPlayerOwned = false, isNPC = albumRecord.isNPC, primaryGenre = source.genre,
			secondaryGenre = albumRecord.secondaryGenre, hookStrength = hook,
			productionQuality = production, danceability = dance,
			originality = albumRecord.originality, controversy = albumRecord.controversy
		};
		var remaining = album.nonSingleTracks.ToList();
		remaining.RemoveAt(bestIndex);
		var refs = album.trackRefs?.ToList() ?? new List<AlbumTrack>();
		refs.Add(new AlbumTrack {
			sourceRecordId = promo.recordId, title = source.title, genre = source.genre,
			quality = source.quality,
			hookStrength = useStructuredPromoTracks ? hook : 0f,
			productionQuality = useStructuredPromoTracks ? production : 0f,
			danceability = useStructuredPromoTracks ? dance : 0f,
			isReleasedSingle = true, peakPosition = 0
		});
		album.nonSingleTracks = remaining.ToArray();
		album.trackRefs = refs.ToArray();
		album.trackRefFreshnessApplied = (album.trackRefFreshnessApplied ?? System.Array.Empty<float>()).Append(1f).ToArray();
		album.trackRefCompUsesAtGeneration = (album.trackRefCompUsesAtGeneration ?? System.Array.Empty<int>()).Append(0).ToArray();
		album.leadSingleIds = (album.leadSingleIds ?? System.Array.Empty<string>()).Append(promo.recordId).ToArray();
		IEnumerable<float> qualities = album.trackRefs.Select((track, index) => track.quality *
			(index < album.trackRefFreshnessApplied.Length ? album.trackRefFreshnessApplied[index] : 1f))
			.Concat(album.nonSingleTracks.Select(track => track.quality));
		album.pooledAppeal = AlbumModel.CalculatePooledAppeal(qualities, album.thematicCohesion, albumRecord.releaseDate.year > 0 ? albumRecord.releaseDate.year : (TimeManager.Instance?.CurrentDate.year ?? 1960));
		return promo;
	}

	// This projection is used only for pre-trait saved tracks. It is deterministic,
	// consumes no RNG, and leaves newly generated tracks on their stored values.
	private static (float hook, float production, float dance) GetTrackTraitsForPromo(AlbumTrack track) =>
		track != null && track.HasStoredComponents
			? (track.hookStrength, track.productionQuality, track.danceability)
			: GetDeterministicTrackTraits(track?.quality ?? 0f, track?.title ?? string.Empty, track?.genre ?? Genre.TraditionalPop);

	private static (float hook, float production, float dance) GetDeterministicTrackTraits(float quality, string identity, Genre genre) {
		uint hash = 2166136261u;
		foreach (char value in $"{identity}|{genre}") { hash ^= value; hash *= 16777619u; }
		float a = ((hash & 1023u) / 1023f - .5f) * .18f;
		float b = (((hash >> 10) & 1023u) / 1023f - .5f) * .14f;
		return (Mathf.Clamp(quality + a, .02f, .98f), Mathf.Clamp(quality + b, .02f, .98f),
			Mathf.Clamp(quality - a * .45f - b * .25f, .02f, .98f));
	}

	private static float GetLeadSingleSuitability(AlbumTrack track, Genre albumGenre, int year) {
		(float hook, float production, float dance) = GetTrackTraitsForPromo(track);
		float eraDanceWeight = year >= 1965 ? .24f : .14f;
		float genreContinuity = track.genre == albumGenre ? .08f : 0f;
		return hook * .52f + production * (1f - .52f - eraDanceWeight) + dance * eraDanceWeight + genreContinuity;
	}

	private PromotionSnapshot BuildPromotionSnapshot(Record record, SimulatedArtist artist, float perceivedQualityMult) {
		var snapshot = new PromotionSnapshot {
			careerState = artist.careerState,
			artistAwareness = artist.GetNewReleaseAwarenessBonus(),
			perceivedQualityMultiplier = perceivedQualityMult
		};
		foreach (MarketRegion region in ChartManager.Instance.GetAllRegions()) {
			snapshot.regions.Add(new RegionalPromotionSnapshot {
				regionId = region.regionId,
				awarenessRandom = (float)GD.RandRange(0.8, 1.1),
				radioRandom = 1f,
				sentimentRandom = (float)GD.RandRange(-0.1, 0.15)
			});
		}
		return snapshot;
	}

	private void ApplyPromotionSnapshot(Record record, AILabel label, float marketingBudget, PromotionSnapshot snapshot,
		float awarenessBonus, float stockMultiplier) {
		RecordRuntimeData runtime = ChartManager.Instance.GetRecordRuntimeData(record.recordId);
		if (runtime == null || snapshot == null) return;
		float quality = runtime.GetQuality();
		float marketingAwareness = GetSeasonalMarketingImpact(marketingBudget, label);
		float baseAwareness = 0.04f + snapshot.artistAwareness + marketingAwareness + label.reputation * 0.1f;
		runtime.awareness = Mathf.Clamp(baseAwareness + awarenessBonus, 0f, 1f);
		runtime.radioHeat = 0f;
		float careerStockScale = snapshot.careerState switch {
			CareerState.Superstar => 2.5f, CareerState.Star => 2.0f, CareerState.Established => 1.5f,
			CareerState.Rising => 1.2f, _ => 1.0f
		};
		var regions = snapshot.regions.Select(regional => ChartManager.Instance.GetRegionById(regional.regionId))
			.Where(region => region != null).ToArray();
		var initialStock = new Dictionary<string, int>(System.StringComparer.Ordinal);
		foreach (RegionalPromotionSnapshot regional in snapshot.regions) {
			MarketRegion region = ChartManager.Instance.GetRegionById(regional.regionId);
			if (region == null) continue;
			var data = new RegionalRecordData(region.regionId);
			runtime.regionalData[region.regionId] = data;
			float regionStrength = ChartSimulator.GetRegionalLaunchFactor(label, region.regionId);
			int baseStock = Mathf.RoundToInt(ChartSimulator.CalculateInitialRegionalStock(label, region.regionId,
				careerStockScale * 0.45f, snapshot.perceivedQualityMultiplier) * stockMultiplier);
			initialStock[region.regionId] = baseStock;
			data.unitsInStores = baseStock;
			data.awareness = Mathf.Clamp(runtime.awareness * regionStrength * regional.awarenessRandom, 0f, 1f);
			data.radioPlay = 0f;
			float genreFit = GetGenreFit(record.primaryGenre, region);
			data.sentiment = Mathf.Clamp(quality * 0.6f + genreFit * 0.3f + regional.sentimentRandom, -1f, 1f);
		}
		IReadOnlyDictionary<string, int> allocatedStock = ChartSimulator.RedistributeInitialRegionalStockAllocation(record.primaryGenre,
			record.releaseDate.year, GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true, regions, initialStock);
		foreach (MarketRegion region in regions) {
			if (runtime.regionalData.TryGetValue(region.regionId, out RegionalRecordData data))
				data.unitsInStores = allocatedStock.GetValueOrDefault(region.regionId);
		}
		runtime.initialLaunchAwareness = runtime.awareness;
		runtime.initialLaunchStock = runtime.regionalData.Values.Sum(data => data.unitsInStores);
		runtime.launchCareerState = snapshot.careerState;
		runtime.perceivedQualityMultiplier = snapshot.perceivedQualityMultiplier;
	}

	private void ProcessDueAlbumProjects(GameDate date) {
		long profileStart = SimulationPerformanceProfiler.Begin();
		for (int index = 0; index < pendingAlbumProjects.Count;) {
			AlbumProject project = pendingAlbumProjects[index];
			if (project.dropWeek > pipelineWeek) {
				index++;
				continue;
			}
			AILabel owner = GetLabel(project.currentLabelId);
			if (owner == null || !owner.IsActive) {
				project.terminalState = AlbumProjectTerminalState.Cancelled;
				RedirectCancelledPromoOutcome(project);
				pendingAlbumProjects.RemoveAt(index);
				continue;
			}
			float marketingBudget = project.albumMarketingBudgetPlanned;
			float available = Mathf.Max(0f, owner.cashReserves - owner.GetMonthlyOverhead());
			marketingBudget = Mathf.Min(marketingBudget, available * 0.8f);
			owner.cashReserves -= marketingBudget;
			owner.monthlyExpenses += marketingBudget;
			if (labelFinancials.TryGetValue(owner.labelId, out LabelFinancialHistory financials)) financials.lastMonthExpenses += marketingBudget;
			WeeklyMarketingSpend += marketingBudget;
			WeeklyMarketingEvents++;

			SimulatedArtist artist = ArtistManager.Instance?.GetArtist(project.artistId);
			ReleasePreparedRecord(project.albumRecord, artist, owner, date, project.albumProductionCost,
				project.releaseTimeAlbumExpectedNet, ProjectRecordRole.LinkedAlbum, project.projectId);
			RecordRuntimeData promoRuntime = ChartManager.Instance.GetRecordRuntimeData(project.promoSingleId);
			int promoPeak = promoRuntime?.peakPosition ?? project.promoPeakAtDrop;
			project.promoPeakAtDrop = promoPeak;
			project.promoPeakScore = CalculatePromoPeakScore(promoPeak, promoFlopThreshold);
			project.synergyAwarenessApplied = promoAwarenessBonusMax * project.promoPeakScore;
			project.synergyStockMultiplier = project.promoPeakScore == 0f ? promoStockFlopFloor : 1f + promoStockBonusMax * project.promoPeakScore;
			ApplyPromotionSnapshot(project.albumRecord, owner, marketingBudget, project.albumPromotionSnapshot,
				project.synergyAwarenessApplied, project.synergyStockMultiplier);
			RecordRuntimeData albumRuntime = ChartManager.Instance.GetRecordRuntimeData(project.albumRecord.recordId);
			project.initialLaunchAwareness = albumRuntime?.initialLaunchAwareness ?? 0f;
			project.initialLaunchStock = albumRuntime?.initialLaunchStock ?? 0;
			if (artist != null) artist.weeksSinceLastRelease = 0;
			project.terminalState = AlbumProjectTerminalState.Released;
			WeeklyPipelineAlbumDrops++;
			pendingAlbumProjects.RemoveAt(index);
		}
		SimulationPerformanceProfiler.EndDueAlbumProjects(profileStart);
	}

	public IReadOnlyList<AlbumProject> GetAlbumProjects() => albumProjects;
	public AlbumProject GetAlbumProject(string projectId) => !string.IsNullOrEmpty(projectId) &&
		projectById.TryGetValue(projectId, out AlbumProject project) ? project : null;
	public bool HasPendingProjectForArtist(string artistId) => pendingAlbumProjects.Any(project => project.artistId == artistId);
	
	private void ApplyReleasePromotion(Record record, SimulatedArtist artist, AILabel label, float marketingBudget, float perceivedQualityMult) {
		var runtimeData = ChartManager.Instance.GetRecordRuntimeData(record.recordId);
		if (runtimeData == null) return;
		bool isAlbum = record.format == ReleaseFormat.Album;
		
		float quality = runtimeData.GetQuality();
		float artistAwareness = artist.GetNewReleaseAwarenessBonus();
		float marketingAwareness = GetSeasonalMarketingImpact(marketingBudget, label);
		float labelAwareness = label.reputation * 0.1f;
		
		runtimeData.awareness = Mathf.Clamp((isAlbum ? 0.04f : 0.08f) + artistAwareness + marketingAwareness + labelAwareness, 0f, 1f);
		
		float baseRadio = quality * 0.3f;
		float pushRadio = ChartSimulator.GetCampaignImpact(label) * 0.3f;
		float payolaRadio = label.payolaWillingness * 0.15f;
		runtimeData.radioHeat = isAlbum ? 0f : Mathf.Clamp(baseRadio + pushRadio + payolaRadio, 0f, 1f);
		
		var regions = ChartManager.Instance.GetAllRegions();
		float stockScale = artist.careerState switch {
			CareerState.Superstar => 2.5f, CareerState.Star => 2.0f, CareerState.Established => 1.5f,
			CareerState.Rising => 1.2f, _ => 1.0f
		};
		var initialStock = new Dictionary<string, int>(System.StringComparer.Ordinal);
		foreach (var region in regions) {
			if (!runtimeData.regionalData.ContainsKey(region.regionId)) {
				runtimeData.regionalData[region.regionId] = new RegionalRecordData(region.regionId);
			}
			var regionalData = runtimeData.regionalData[region.regionId];
			float regionStrength = ChartSimulator.GetRegionalLaunchFactor(label, region.regionId);
			int baseStock = ChartSimulator.CalculateInitialRegionalStock(label, region.regionId,
				stockScale * (isAlbum ? 0.45f : 1f), perceivedQualityMult);
			initialStock[region.regionId] = baseStock;
			regionalData.unitsInStores = baseStock;
			regionalData.awareness = Mathf.Clamp(runtimeData.awareness * regionStrength * (float)GD.RandRange(0.8, 1.1), 0f, 1f);
			float radioDifficulty = ChartSimulator.GetRadioDifficulty(region);
			float genreRadio = GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true
				? GenreAcceptanceService.GetRegionalRadioOpportunity(record.primaryGenre, record.secondaryGenre, region,
					TimeManager.Instance?.CurrentDate.year ?? 1960, ChartManager.Instance?.GetGenreMomentum(record.primaryGenre) ?? 0f)
				: 1f;
			if (MarketSeasonality.Enabled) {
				float radioOpportunity = MarketSeasonality.GetRadioOpportunity(TimeManager.Instance?.CurrentDate.year ?? 1960,
					TimeManager.Instance?.CurrentDate.month ?? 1, liveTick: true);
				regionalData.radioPlay = isAlbum ? 0f : Mathf.Clamp(runtimeData.radioHeat * regionStrength / radioDifficulty * (float)GD.RandRange(0.7, 1.0) * radioOpportunity * genreRadio, 0f, 1f);
			} else regionalData.radioPlay = isAlbum ? 0f : Mathf.Clamp(runtimeData.radioHeat * regionStrength / radioDifficulty * (float)GD.RandRange(0.7, 1.0) * genreRadio, 0f, 1f);
			float genreFit = GetGenreFit(record.primaryGenre, region);
			regionalData.sentiment = Mathf.Clamp((quality * 0.6f) + (genreFit * 0.3f) + (float)GD.RandRange(-0.1, 0.15), -1f, 1f);
		}
		IReadOnlyDictionary<string, int> allocatedStock = ChartSimulator.RedistributeInitialRegionalStockAllocation(record.primaryGenre,
			TimeManager.Instance?.CurrentDate.year ?? 1960, GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true,
			regions, initialStock);
		foreach (MarketRegion region in regions) {
			if (runtimeData.regionalData.TryGetValue(region.regionId, out RegionalRecordData data))
				data.unitsInStores = allocatedStock.GetValueOrDefault(region.regionId);
		}

		runtimeData.initialLaunchAwareness = runtimeData.awareness;
		runtimeData.initialLaunchStock = runtimeData.regionalData.Values.Sum(data => data.unitsInStores);
		runtimeData.launchCareerState = artist.careerState;
		runtimeData.perceivedQualityMultiplier = perceivedQualityMult;
		
		if (debugMode) GD.Print($"  Promotion: Awareness={runtimeData.awareness:F2}, Radio={runtimeData.radioHeat:F2}");
	}
	
	private float BudgetToImpact(float budget, LabelTier tier) {
		float baseline = tier switch {
			LabelTier.Major => 3000f, LabelTier.MidTier => 1500f, LabelTier.Independent => 600f,
			LabelTier.Small => 250f, LabelTier.Boutique => 400f, _ => 500f
		};
		float normalized = budget / baseline;
		return Mathf.Clamp((Mathf.Log(1 + normalized * 9) / Mathf.Log(10)) / 1.5f, 0f, 1f);
	}

	private float GetSeasonalMarketingImpact(float marketingBudget, AILabel label) {
		if (!MarketSeasonality.Enabled) return BudgetToImpact(marketingBudget, label.tier) * ChartSimulator.GetCampaignImpact(label) * 0.35f;
		int year = TimeManager.Instance?.CurrentDate.year ?? 1960;
		int month = TimeManager.Instance?.CurrentDate.month ?? 1;
		float impact = BudgetToImpact(marketingBudget, label.tier) *
			MarketSeasonality.GetMarketingEfficiencyMultiplier(year, month, liveTick: true);
		return Mathf.Clamp(impact, 0f, 1f) * ChartSimulator.GetCampaignImpact(label) * 0.35f;
	}
	
	private float GetGenreFit(Genre genre, MarketRegion region) {
		if (region.genrePreferences == null) return 0.6f;
		var pref = region.genrePreferences.FirstOrDefault(p => p.genre == genre);
		return pref != null ? 0.5f + (pref.affinity * 0.5f) : 0.5f;
	}
	
	private void TrackRelease(string labelId, string recordId) {
		if (!labelActiveRecords.ContainsKey(labelId)) labelActiveRecords[labelId] = new List<string>();
		labelActiveRecords[labelId].Add(recordId);
	}
	
	private void OnMonthChanged(GameDate date) {
		foreach (var label in aiLabels) ProcessLabelMonth(label, date);
		ProcessDistributionDeals(date);
		if (debugMode) PrintMonthlyReport(date);
	}
	
	private void ProcessLabelMonth(AILabel label, GameDate date) {
		if (!label.IsActive) return;
		
		var financials = labelFinancials.TryGetValue(label.labelId, out var f) ? f : null;
		if (financials == null) {
			financials = new LabelFinancialHistory();
			labelFinancials[label.labelId] = financials;
		}
		float overhead = label.GetMonthlyOverhead();
		label.cashReserves -= overhead;
		label.monthlyExpenses += overhead;
		financials.lastMonthExpenses += overhead;
		
		float netIncome = financials.lastMonthRevenue - financials.lastMonthExpenses;
		label.lastMonthlyProfit = netIncome;
		ReinvestDistributionProfit(label, netIncome);
		UpdateLabelStatus(label, financials, netIncome);
		
		label.monthlyRevenue = 0f;
		label.monthlyExpenses = 0f;
		financials.lastMonthRevenue = 0f;
		financials.lastMonthExpenses = 0f;
		
		if (date.month == 1) financials.totalReleasesThisYear = 0;
	}

	public void SetDistributionOfferProcessingEnabled(bool enabled) => distributionOfferProcessingEnabled = enabled;

	private void ReinvestDistributionProfit(AILabel label, float netIncome) {
		if (label.activeDeal == null || netIncome <= 0f) return;
		float reinvestment = netIncome * dealReinvestRate;
		label.cashReserves -= reinvestment;
		label.ownedReach = Mathf.Min(1f, label.ownedReach + (reinvestment / Mathf.Max(1f, dealReinvestCost)));
	}

	private void ProcessDistributionDeals(GameDate date) {
		int currentWeek = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		foreach (AILabel client in aiLabels.Where(label => label.activeDeal != null).ToList()) {
			DistributionDeal deal = client.activeDeal;
			AILabel distributor = GetLabel(deal.distributorId);
			if (!client.IsActive) {
				deal.unrecoupedAdvance = 0f;
				EmitDealEvent(client, distributor, deal, DealResolution.ClientClosed, client.DistributionDependency);
				client.activeDeal = null;
				continue;
			}
			if (distributor == null || !distributor.IsActive) {
				EmitDealEvent(client, distributor, deal, DealResolution.DistributorCollapsed, client.DistributionDependency);
				client.activeDeal = null;
				DistributorCollapseCount++;
				continue;
			}
			if (currentWeek >= deal.signedWeek + deal.termWeeks) ResolveDistributionDeal(client, distributor, currentWeek);
		}

		if (!distributionOfferProcessingEnabled) return;
		foreach (AILabel client in aiLabels.Where(label => label.IsActive && label.activeDeal == null).ToList()) {
			TryGenerateDistributionOffer(client, date.year, currentWeek);
		}
	}

	private void TryGenerateDistributionOffer(AILabel client, int year, int currentWeek) {
		bool eligibleTier = client.tier == LabelTier.Small || client.tier == LabelTier.Boutique || client.tier == LabelTier.Independent;
		if (!eligibleTier) return;

		bool pullTrigger = client.nationalReach < 0.40f && HasStrongRegionalChartRecord(client);
		float pushChance = monthlyPushOfferProbability + (Mathf.Max(0, year - 1966) * annualPost1966PushRamp);
		bool pushTrigger = (client.momentumScore > 0.60f || HasRecentTop40Record(client)) && GD.Randf() < pushChance;
		bool pullOffer = pullTrigger && GD.Randf() < monthlyPullOfferProbability;
		if (!pushTrigger && !pullOffer) return;

		DealOrigin origin = pushTrigger ? DealOrigin.DistributorCourted : DealOrigin.LabelSought;
		AILabel distributor = SelectDistributor(client, origin);
		if (distributor == null) return;
		DistributionDeal offer = GenerateDealTerms(client, distributor, origin, year, currentWeek);
		DistributionOffersGenerated++;
		if (!ShouldAcceptDeal(client, offer)) return;
		DistributionOffersAccepted++;

		client.activeDeal = offer;
		client.cashReserves += offer.advance;
		distributor.cashReserves -= offer.advance;
		EmitDealEvent(client, distributor, offer, DealResolution.Signed, client.DistributionDependency);
	}

	private bool HasStrongRegionalChartRecord(AILabel label) {
		if (ChartManager.Instance == null || label.strongRegions == null) return false;
		var strongRegions = label.strongRegions.ToHashSet(System.StringComparer.Ordinal);
		return ChartManager.Instance.GetAllRecords().Any(record =>
			record.baseRecord.labelId == label.labelId && record.currentPosition > 0 && record.GetQuality() > 0.70f &&
			record.regionalData.Any(pair => strongRegions.Contains(pair.Key) && pair.Value.unitsSoldThisWeek > 0));
	}

	private bool HasRecentTop40Record(AILabel label) => ChartManager.Instance != null &&
		ChartManager.Instance.GetAllRecords().Any(record => record.baseRecord.labelId == label.labelId &&
			record.currentPosition > 0 && record.currentPosition <= 40 && record.weeksSinceRelease <= 52);

	private AILabel SelectDistributor(AILabel client, DealOrigin origin) {
		var weighted = new List<(AILabel Label, float Weight)>();
		foreach (AILabel distributor in aiLabels) {
			if (!IsEligibleDistributor(distributor, client, origin)) continue;
			bool genreFit = distributor.preferredGenres?.Intersect(client.preferredGenres ?? System.Array.Empty<Genre>()).Any() ?? false;
			float weight = (distributor.ownedReach * 0.50f) + (distributor.reputation * 0.30f) + (genreFit ? 0.20f : 0f);
			weight *= distributor.tier switch { LabelTier.Major => 6f, LabelTier.MidTier => 1.5f, _ => 1f };
			if (weight > 0f) weighted.Add((distributor, weight));
		}
		if (weighted.Count == 0) return null;

		float total = weighted.Sum(entry => entry.Weight);
		float roll = GD.Randf() * total;
		foreach (var entry in weighted) {
			roll -= entry.Weight;
			if (roll <= 0f) return entry.Label;
		}
		return weighted[^1].Label;
	}

	private bool IsEligibleDistributor(AILabel distributor, AILabel client, DealOrigin origin) {
		if (distributor == null || distributor == client || !distributor.IsActive) return false;
		bool validTier = distributor.tier == LabelTier.Major || distributor.tier == LabelTier.MidTier ||
			(distributor.tier == LabelTier.Independent && distributor.ownedReach >= 0.65f);
		if (!validTier || WouldCreateCircularDeal(client, distributor)) return false;
		int capacity = distributor.tier switch { LabelTier.Major => 12, LabelTier.MidTier => 6, _ => 3 };
		if (aiLabels.Count(label => label.activeDeal?.distributorId == distributor.labelId) >= capacity) return false;
		float minimumAdvance = origin == DealOrigin.DistributorCourted ? client.GetMonthlyOverhead() * 6f : 0f;
		if (distributor.cashReserves - minimumAdvance <= distributor.GetMonthlyOverhead() * 3f) return false;

		var offeredRegions = distributor.distributionRegions ?? System.Array.Empty<string>();
		if (origin == DealOrigin.LabelSought) {
			return (client.strongRegions ?? System.Array.Empty<string>()).Any(region =>
				!client.HasDistributionInRegion(region) && offeredRegions.Contains(region));
		}
		return offeredRegions.Any(region => !client.HasDistributionInRegion(region));
	}

	private static bool WouldCreateCircularDeal(AILabel client, AILabel distributor) {
		var visited = new HashSet<string>(System.StringComparer.Ordinal);
		AILabel current = distributor;
		while (current?.activeDeal != null && visited.Add(current.labelId)) {
			if (current.activeDeal.distributorId == client.labelId) return true;
			current = Instance?.GetLabel(current.activeDeal.distributorId);
		}
		return false;
	}

	private DistributionDeal GenerateDealTerms(AILabel client, AILabel distributor, DealOrigin origin, int year, int currentWeek) {
		bool push = origin == DealOrigin.DistributorCourted;
		string[] availableRegions = (distributor.distributionRegions ?? System.Array.Empty<string>())
			.Where(region => !client.HasDistributionInRegion(region)).Distinct().ToArray();
		string[] grantedRegions = push
			? availableRegions
			: availableRegions.Intersect(client.strongRegions ?? System.Array.Empty<string>()).ToArray();
		float advance = push
			? client.GetMonthlyOverhead() * (float)GD.RandRange(6f, 12f)
			: (GD.Randf() < 0.35f ? 0f : client.GetMonthlyOverhead() * (float)GD.RandRange(0.5f, 2f));
		advance = Mathf.Min(advance, Mathf.Max(0f, distributor.cashReserves - (distributor.GetMonthlyOverhead() * 3f)));
		return new DistributionDeal {
			distributorId = distributor.labelId,
			reachGranted = push ? (float)GD.RandRange(0.50f, 0.80f) : (float)GD.RandRange(0.30f, 0.50f),
			grantedRegions = grantedRegions,
			marginSkim = push ? (float)GD.RandRange(pushMarginSkimMin, pushMarginSkimMax) : (float)GD.RandRange(pullMarginSkimMin, pullMarginSkimMax),
			ownsMasters = GD.Randf() < (push ? pushMastersOwnershipRate : 0.15f),
			advance = advance,
			unrecoupedAdvance = advance,
			signedWeek = currentWeek,
			termWeeks = push ? (int)GD.RandRange(78, year >= 1967 ? 104 : 156) : (int)GD.RandRange(52, 104),
			origin = origin
		};
	}

	private static bool ShouldAcceptDeal(AILabel client, DistributionDeal offer) {
		float currentReach = client.distributionStrength;
		float projectedReach = Mathf.Clamp(client.ownedReach + offer.reachGranted, 0f, 1f);
		if (projectedReach <= currentReach + 0.05f) return false;

		bool cashPressured = client.cashReserves < client.GetMonthlyOverhead() * 6f || client.consecutiveLossMonths >= 3;
		bool momentumHungry = client.momentumScore > 0.55f || client.status == LabelStatus.Rising;
		float acceptance = 0.20f + (cashPressured ? 0.35f : 0f) + (momentumHungry ? 0.30f : 0f);
		if (offer.origin == DealOrigin.LabelSought) acceptance += 0.20f;
		if (client.tier == LabelTier.Independent && client.ownedReach >= 0.45f && !cashPressured) acceptance -= 0.35f;
		return GD.Randf() < Mathf.Clamp(acceptance, 0.05f, 0.95f);
	}

	private void ResolveDistributionDeal(AILabel client, AILabel distributor, int currentWeek) {
		DistributionDeal deal = client.activeDeal;
		float dependency = client.DistributionDependency;
		if (dependency < dealDependencyLow) {
			client.ownedReach = Mathf.Min(1f, client.ownedReach + (deal.reachGranted * 0.50f));
			EmitDealEvent(client, distributor, deal, DealResolution.Exit, dependency);
			client.activeDeal = null;
		} else if (dependency < dealDependencyHigh) {
			EmitDealEvent(client, distributor, deal, DealResolution.Renew, dependency);
			deal.signedWeek = currentWeek;
		} else if (deal.ownsMasters) {
			AbsorbLabel(client, distributor, dependency);
		} else {
			EmitDealEvent(client, distributor, deal, DealResolution.Renew, dependency);
			deal.marginSkim = Mathf.Min(0.50f, deal.marginSkim + 0.05f);
			deal.reachGranted = Mathf.Max(0.10f, deal.reachGranted * 0.85f);
			deal.signedWeek = currentWeek;
		}
	}

	private void AbsorbLabel(AILabel client, AILabel distributor, float dependency) {
		if (client == null || distributor == null || client == distributor || !client.IsActive || !distributor.IsActive) return;
		DistributionDeal deal = client.activeDeal;
		if (deal == null || deal.distributorId != distributor.labelId || WouldCreateCircularDeal(client, distributor)) return;

		distributor.marketShare += client.marketShare;
		distributor.top40Hits += client.top40Hits;
		distributor.numberOneHits += client.numberOneHits;
		foreach (SimulatedArtist artist in client.roster.ToList()) {
			artist.labelId = distributor.labelId;
			artist.isPlayerOwned = false;
			if (!distributor.roster.Contains(artist)) distributor.roster.Add(artist);
		}
		client.roster.Clear();
		if (LabelLifecycleManager.Instance != null) LabelLifecycleManager.Instance.ReconcileAcquisitionRosterTarget(distributor);
		else distributor.maxRosterSize = Mathf.Max(distributor.maxRosterSize, distributor.CurrentRosterSize);

		if (!labelActiveRecords.TryGetValue(distributor.labelId, out List<string> distributorRecords)) {
			distributorRecords = new List<string>();
			labelActiveRecords[distributor.labelId] = distributorRecords;
		}
		if (labelActiveRecords.TryGetValue(client.labelId, out List<string> clientRecords)) {
			foreach (string recordId in clientRecords) if (!distributorRecords.Contains(recordId)) distributorRecords.Add(recordId);
			clientRecords.Clear();
		}
		foreach (RecordRuntimeData record in ChartManager.Instance.GetAllRecords().Where(record => record.baseRecord.labelId == client.labelId)) {
			record.baseRecord.labelId = distributor.labelId;
		}
		foreach (AlbumProject project in albumProjects.Where(project =>
			project.terminalState == AlbumProjectTerminalState.PendingAtAuditEnd && project.currentLabelId == client.labelId)
			.OrderBy(project => project.creationSequence)) {
			project.currentLabelId = distributor.labelId;
			project.albumRecord.labelId = distributor.labelId;
			project.transferCount++;
			project.wasTransferred = true;
		}

		EmitDealEvent(client, distributor, deal, DealResolution.Absorb, dependency);
		client.activeDeal = null;
		if (LabelLifecycleManager.Instance != null) LabelLifecycleManager.Instance.MarkLabelAcquired(client, distributor);
		else client.status = LabelStatus.Acquired;
	}

	private void EmitDealEvent(AILabel client, AILabel distributor, DistributionDeal deal, DealResolution resolution, float dependency) {
		OnDistributionDealEvent?.Invoke(new DistributionDealTelemetry {
			resolution = resolution,
			origin = deal.origin,
			distributorId = deal.distributorId,
			distributorName = distributor?.labelName ?? deal.distributorId,
			clientId = client?.labelId,
			clientName = client?.labelName,
			reachGranted = deal.reachGranted,
			marginSkim = deal.marginSkim,
			ownsMasters = deal.ownsMasters,
			advance = deal.advance,
			signedWeek = deal.signedWeek,
			termWeeks = deal.termWeeks,
			dependency = dependency
		});
	}
	
	private void UpdateLabelStatus(AILabel label, LabelFinancialHistory financials, float netIncome) {
		if (netIncome < 0) financials.consecutiveLossMonths++;
		else financials.consecutiveLossMonths = Mathf.Max(0, financials.consecutiveLossMonths - 1);
		label.consecutiveLossMonths = financials.consecutiveLossMonths;
		
		if (label.cashReserves < bankruptcyThreshold) {
			if (enableBankruptcy && financials.consecutiveLossMonths >= 6) {
				label.status = LabelStatus.Bankrupt;
				GD.Print($"💀 {label.labelName} has gone bankrupt!");
				return;
			}
			label.status = LabelStatus.Dying;
		} else if (label.cashReserves < label.GetMonthlyOverhead() * 3) {
			label.status = LabelStatus.Struggling;
		} else if (LabelLifecycleManager.IsRuntimeFounderInEmergenceRunway(label)) {
			label.status = netIncome > label.GetMonthlyOverhead() * 2f ? LabelStatus.Rising : LabelStatus.Stable;
		} else if (financials.consecutiveLossMonths >= 3) {
			label.status = LabelStatus.Dying;
		} else if (netIncome > label.GetMonthlyOverhead() * 2) {
			label.status = LabelStatus.Rising;
		} else if (netIncome > 0) {
			label.status = LabelStatus.Stable;
		}
	}
	
	public AILabel GetLabel(string labelId) => aiLabels?.FirstOrDefault(l => l.labelId == labelId);
	public IReadOnlyList<AILabel> GetAllLabels() => aiLabels ?? (IReadOnlyList<AILabel>)System.Array.Empty<AILabel>();

	public void RegisterLabel(AILabel label) {
		if (label == null || string.IsNullOrEmpty(label.labelId)) return;
		if (aiLabels != null && !aiLabels.Contains(label)) aiLabels.Add(label);
		if (!labelActiveRecords.ContainsKey(label.labelId)) labelActiveRecords[label.labelId] = new List<string>();
		if (!labelFinancials.ContainsKey(label.labelId)) labelFinancials[label.labelId] = new LabelFinancialHistory();
	}

	public void RecordExpense(AILabel label, float amount) {
		if (label == null || amount <= 0f) return;
		label.cashReserves -= amount;
		label.monthlyExpenses += amount;
		if (labelFinancials.TryGetValue(label.labelId, out LabelFinancialHistory financials)) {
			financials.lastMonthExpenses += amount;
		}
	}
	
	public List<AILabel> GetActiveLabelsByStatus(LabelStatus status) => aiLabels?.Where(l => l.status == status).ToList() ?? new List<AILabel>();
	
	public List<AILabel> GetOperatingLabels() => aiLabels?.Where(l => l.IsActive).ToList() ?? new List<AILabel>();
	
	public int GetLabelActiveRecordCount(string labelId) => labelActiveRecords.TryGetValue(labelId, out var records) ? records.Count : 0;

	public int GetRecentChartingRecordCount(string labelId, int maxAgeWeeks = 52) {
		if (string.IsNullOrEmpty(labelId) || ChartManager.Instance == null) return 0;
		return ChartManager.Instance.GetAllRecords().Count(record =>
			record.baseRecord.labelId == labelId &&
			record.weeksSinceRelease <= maxAgeWeeks &&
			record.weeksOnChart > 0);
	}

	public int GetRecentReleasedRecordCount(string labelId, int maxAgeWeeks = 52) {
		if (string.IsNullOrEmpty(labelId) || ChartManager.Instance == null) return 0;
		return ChartManager.Instance.GetAllRecords().Count(record =>
			record.baseRecord.labelId == labelId &&
			record.weeksSinceRelease <= maxAgeWeeks);
	}
	
	private void PrintMonthlyReport(GameDate date) {
		GD.Print($"=== INDUSTRY REPORT - {date.month}/{date.year} ===");
		var byStatus = aiLabels.GroupBy(l => l.status);
		foreach (var group in byStatus.OrderBy(g => (int)g.Key)) GD.Print($"{group.Key}: {group.Count()} labels");
		
		var topLabels = aiLabels.Where(l => l.status != LabelStatus.Bankrupt).OrderByDescending(l => l.cashReserves).Take(5);
		GD.Print("Top 5 Labels by Cash:");
		foreach (var label in topLabels) {
			int chartingCount = labelActiveRecords.TryGetValue(label.labelId, out var recs) ? recs.Count : 0;
			GD.Print($"  {label.labelName}: ${label.cashReserves:N0} | Roster: {label.roster.Count} | Charting: {chartingCount}");
		}
	}
	
	public void DebugPrintReleaseStats() {
		int totalActive = labelActiveRecords.Values.Sum(l => l.Count);
		GD.Print($"=== RELEASE STATS ===\nTotal Active Records: {totalActive}");
		var topByReleases = labelActiveRecords.OrderByDescending(kvp => kvp.Value.Count).Take(10);
		foreach (var (labelId, records) in topByReleases) {
			var label = GetLabel(labelId);
			GD.Print($"  {label?.labelName ?? labelId}: {records.Count} active");
		}
	}
	
	public void DebugForceRelease() {
		if (aiLabels == null || aiLabels.Count == 0) return;
		var label = aiLabels.Where(l => l.status != LabelStatus.Bankrupt && l.roster.Count > 0).OrderBy(l => GD.Randf()).FirstOrDefault();
		if (label != null) {
			var date = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
			TryReleaseRecord(label, date);
		}
	}
}

public class LabelFinancialHistory {
	public float lastMonthRevenue;
	public float lastMonthExpenses;
	public int consecutiveLossMonths;
	public int totalReleasesThisYear;
}

public sealed class RevenueTelemetry {
	public float gross;
	public float cogs;
	public float distributionSkim;
	public float artistRoyalty;
	public float labelNet;
	public float distributionIncome;
	public float MarketNet => labelNet + distributionIncome;
}

public sealed class ReleaseStrategyTelemetry {
	public string recordId;
	public string labelId;
	public LabelTier tier;
	public string artistId;
	public Genre genre;
	/// <summary>Unmapped secondary identity at the format decision seam; retained for offline cohort migration.</summary>
	public Genre secondaryGenre;
	public CareerState careerState;
	public string careerBand;
	public float qualityEstimate;
	public string qualityQuartile;
	public float reachFactor;
	public float genreSinglesMarketFactor;
	public float priorSingleNet;
	public float priorAlbumNet;
	public float projectedSingleNet;
	public float projectedAlbumNet;
	public float confidenceSingle;
	public float confidenceAlbum;
	public float rawConfidenceSingle;
	public float rawConfidenceAlbum;
	public bool singleMemoryCapApplied;
	public bool albumMemoryCapApplied;
	public ReleaseFormat chosenFormat;
	// Preserves the A2 diagnostic column's meaning; no longer participates in the prior.
	public bool assumedCompilationCost;
	public float compCostWeight;
	public float expectedFormatMultiplier;
	public int releasedSingleIdsExamined;
	public int resolvedSingles;
	public int chartedSingles;
	public float hitScore;
	public float unweightedHitUnits;
	public float weightedHitUnits;
	public float affinityUnits;
	public float totalExpectedAlbumUnits;
	public AlbumFormat? actualAlbumFormat;
	public string projectId;
	public ReleaseStrategy strategy;
	public float projectedOrphanSingleNet;
	public float projectedAlbumStandaloneNet;
	public float projectedAlbumWithPromoNet;
	public string promoSingleId;
	public bool albumStrategyEvaluated;
	public float singleProductionCost;
	public float singleNetMarginPerUnit;
	public float expectedSingleUnits;
	public float albumDemandFactor;
	public float substitutionK;
	public float substitutionCap;
	public float substitutionPropensity;
	public float expectedOverlapFraction;
	public float divertedUnits;
	public float albumMarginPerUnit;
	public float cannibalizationLoss;
	public float expectedPromoLift;
	public float expectedPromoSingleNet;
	public float promoAdvantage;
	public float singlePreTiltContribution;
	public float singleFormatTilt;
	public float albumAffinity;
	public float albumOpportunity;
	public float albumFormatTilt;
	public float albumPreTiltContribution;
	public float albumProductionCost;
	public float singleMemoryEma;
	public float albumMemoryEma;
		public float singleMemoryBlend;
		public float albumMemoryBlend;
		public bool labelFormatMemoryBypassed;
		public float singleNoiseMultiplier;
	public float albumNoiseMultiplier;
	public float albumChoiceProbability;
	public float formatChoiceRoll;
	public bool albumCapacityReroute;
}

public sealed class CalibrationDecisionTelemetry {
	public string recordId;
	public string labelId;
	public string artistId;
	public Genre genre;
	public CareerState careerState;
	public float qualityEstimate;
	public float reachFactor;
	public float genreSinglesMarketFactor;
	public float singleProductionCost;
	public ReleaseFormat chosenFormat;
}

public enum SupplySelectionMode { Retained, AnnualFloor, WeightedTransition }

public sealed class SupplySelectionTelemetry {
	public string labelId;
	public string artistId;
	public Genre artistIdentity;
	public Genre chosenProjectGenre;
	public bool artistIdentityAvailableForNewSupply;
	public bool annualFloorRequested;
	public bool annualFloorReroutedToNormalCandidates;
	public SupplySelectionMode selectionMode;
}

public sealed class ReleaseOutcomeTelemetry {
	public string labelId;
	public string recordId;
	public ReleaseFormat format;
	public Genre genre;
	public bool memoryEligible;
	public float lifetimeLabelNet;
	public float sunkProductionCost;
	public float realizedNet;
}

public sealed class FormatMemoryRevisionTelemetry {
	public string releaseId, labelId, revisionKind;
	public string projectId;
	public ReleaseFormat format;
	public ProjectRecordRole releaseLane;
	public RevenueEstimatorLane estimatorLane;
	public Genre genre;
	public int releaseAge, revisionOrdinal;
	public float releaseTimeExpectedNet, ageMatchedExpectedNet, realizedNetToDate, estimatedOutcomeNet, opportunityScale;
	public float normalizedResidual, maturityWeight, recencyWeight;
	public bool replacedPriorRevision, finalized;
}
