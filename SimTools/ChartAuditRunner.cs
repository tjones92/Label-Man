using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Godot;

/// <summary>
/// Removable headless audit driver. It advances the real TimeManager event chain
/// and only observes ChartManager's public state after each simulated week.
/// </summary>
public partial class ChartAuditRunner : Node {
	private sealed class LifecycleState {
		public RecordRuntimeData Record;
		public int DebutPosition;
		public int WeeksAtNumberOne;
		public bool WasPresentAtStart;
	}

	private sealed class RevenueRollup {
		public long Units;
		public double Gross;
		public double LabelNet;
		public double DistributionIncome;
	}
	private sealed class FormatMixRollup {
		public int Releases;
		public long Units;
		public double Gross;
		public double Cogs;
		public double Skim;
		public double Royalty;
		public double LabelNet;
	}
	private sealed class GeographyRollup {
		public int RecordCount;
		public long TotalUnits;
		public long ChartedUnits;
		public long Backorders;
		public long HomeRegionUnits;
		public long NonNationalUnits;
		public long NonNationalBackorders;
	}
	private sealed class DecadeAnnualRollup {
		public readonly FormatMixRollup Single = new();
		public readonly FormatMixRollup Album = new();
		public int Decisions;
		public int AlbumDecisions;
		public int AdultDecisions;
		public int AdultAlbumDecisions;
		public int YouthDecisions;
		public int YouthAlbumDecisions;
		public int OrphanDecisions;
		public int PromoDecisions;
		public int StandaloneDecisions;
		public double SingleConfidence;
		public int SingleConfidenceCount;
		public double AlbumConfidence;
		public int AlbumConfidenceCount;
		public long AlbumUnitsOver26Weeks;
		public long AlbumUnitsOver52Weeks;
		public int CompilationAlbums;
		public int CompilationTrackRefs;
		public int FreshnessUse0;
		public int FreshnessUse1;
		public int FreshnessUse2;
		public int FreshnessUse3Plus;
		public double FreshnessSum;
		public float FreshnessMin = float.PositiveInfinity;
		public float FreshnessMax;
		public double SingleMemoryEma;
		public int SingleMemoryLabels;
		public int SingleMemoryN;
		public double AlbumMemoryEma;
		public int AlbumMemoryLabels;
		public int AlbumMemoryN;
		public int CompletedMatched;
		public double CompletedExpected;
		public double CompletedRealized;
		public int YouthCompCompleted;
		public double YouthCompExpected;
		public double YouthCompRealized;
		public int PromoCompleted;
		public double PromoExpected;
		public double PromoRealized;
		public readonly Dictionary<string, (double Quality, int BestPosition)> ChartingSingles = new(StringComparer.Ordinal);
		public readonly List<int> ClosedTop40Weeks = new();
		public int ActiveSingles;
		public int ActiveAlbums;
		public List<int> AlbumAges = new();
		public List<int> AlbumUnits = new();
		public int AlbumsEverReleased;
		public int AlbumsRetired;
	}
	private sealed class DecisionExpectation {
		public float Expected;
		public bool YouthCompilation;
		public bool Promo;
	}
	private sealed class SeasonalityMonthRollup {
		public int LiveWeeks;
		public long SingleUnits;
		public long AlbumUnits;
		public double SingleGross;
		public double AlbumGross;
		public int ReleaseRolls;
		public int SuccessfulReleases;
		public int SingleReleases;
		public int AlbumProjectsScheduled;
		public int AlbumDrops;
		public double ProductionSpend;
		public int ProductionEvents;
		public double MarketingSpend;
		public int MarketingEvents;
		public int ScoutingRolls;
		public int Signings;
		public double RadioPlaySum;
		public int RadioPlayCount;
	}

	private readonly Dictionary<string, LifecycleState> lifecycle = new();
	private HashSet<string> previousChartIds = new();
	private HashSet<string> previousActiveIds = new();
	private StreamWriter recordWriter;
	private StreamWriter weekWriter;
	private StreamWriter lifecycleWriter;
	private StreamWriter breakoutWriter;
	private StreamWriter retirementWriter;
	private StreamWriter tierVolumeWriter;
	private StreamWriter labelFinanceWriter;
	private StreamWriter dealLedgerWriter;
	private StreamWriter labelDirectoryWriter;
	private StreamWriter independentDistributorWriter;
	private StreamWriter independentDistributionEventWriter;
	private StreamWriter independentTradeFailureWriter;
	private StreamWriter concentrationWriter;
	private StreamWriter firstChartEventWriter;
	private StreamWriter distributionOfferAttemptWriter;
	private StreamWriter marketRevenueWriter;
	private StreamWriter releaseCapacityWriter;
	private StreamWriter seasonalityMonthlyWriter;
	private StreamWriter albumChartWriter;
	private StreamWriter albumCompositionWriter;
	private StreamWriter formatMixWriter;
	private StreamWriter retiredTrackWriter;
	private StreamWriter releaseStrategyWriter;
	private StreamWriter releaseOutcomeWriter;
	private StreamWriter singleReleaseLaneWriter;
	private StreamWriter singleDemandStagesWriter;
	private StreamWriter revenueMemoryWriter;
	private StreamWriter liveRecordsSnapshotWriter;
	private StreamWriter priorCostAssumptionWriter;
	private StreamWriter albumTrackLinkWriter;
	private StreamWriter calibrationDecisionWriter;
	private StreamWriter forkRatioWriter;
	private StreamWriter a3EconomicDecisionWriter;
	private StreamWriter albumProjectWriter;
	private StreamWriter albumProjectDemandWriter;
	private StreamWriter albumProjectWeeklyWriter;
	private StreamWriter decadeAnnualRollupWriter;
	private StreamWriter performanceProfileWriter;
	private StreamWriter cityRosterWriter;
	private StreamWriter distanceMatrixWriter;
	private StreamWriter labelGeographyWriter;
	private StreamWriter geographyMetricsWriter;
	private StreamWriter dealMetricsWriter;
	private StreamWriter genreCatalogWriter;
	private StreamWriter genreMarketWeeklyWriter;
	private StreamWriter recordGenreExplanationWriter;
	private StreamWriter albumDemandExplanationWriter;
	private StreamWriter formatDecisionExplanationWriter;
	private StreamWriter formatDecisionCohortWriter;
	private StreamWriter formatDecisionCohortDetailWriter;
	private StreamWriter supplySelectionWriter;
	private StreamWriter traditionalPopFallbackWriter;
	private StreamWriter genreShapeWriter;
	private int genreShapeYear;
	private readonly Dictionary<Genre, GenreShapeYearState> genreShapeByYear = new();
	private readonly HashSet<string> genreShapeSeenRecordIds = new(StringComparer.Ordinal);
	private StreamWriter genreEventsWriter;
	private StreamWriter specialProductsWriter;
	// Enabled-only: absent from disabled runs so the frozen 45-stream boundary is unchanged.
	private StreamWriter rosterLifecycleWriter;
	private StreamWriter labelScoutingVacancyWriter;
	private StreamWriter artistPopulationEventsWriter;
	private StreamWriter artistPopulationWeeklyWriter;
	private StreamWriter artistLaborMarketWeeklyWriter;
	private StreamWriter artistCohortAnnualWriter;
	private StreamWriter artistProjectIdentityWriter;
	private StreamWriter labelOperatingTargetEventWriter;
	private StreamWriter runtimeLabelProfileWriter;
	private StreamWriter dailyTalentMarketWriter;
	private StreamWriter dailyTalentAppointmentWriter;
	private StreamWriter catastrophicFailFastWriter;
	private StreamWriter marketClearingWriter;
	private StreamWriter marketSpilloverWriter;
	private StreamWriter formatMemoryAdjustmentWriter;
	private StreamWriter completedWeekSettlementWriter;
	private StreamWriter completedWeekSettlementRegionalWriter;
	private StreamWriter albumRealizationBridgeWriter;
	private StreamWriter formatMemoryRevisionWriter;
	// Event-owned signing flows use the exact chart week written to the population
	// ledger. This is observational state only and therefore cannot perturb play.
	private readonly Dictionary<int, (int FirstTime, int Repeat)> populationSigningFlowByWeek = new();
	private readonly List<(int Week, string Prefix, string Suffix)> deferredLaborMarketRows = new();
	private readonly HashSet<string> observedPopulationProjectIds = new(StringComparer.Ordinal);
	private readonly Dictionary<string, long> annualChartUnitsByLabel = new(StringComparer.Ordinal);
	private readonly HashSet<string> cumulativeChartingLabelIds = new(StringComparer.Ordinal);
	private readonly HashSet<string> cumulativeChartingLabelNames = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, LabelTier> firstChartTierByLabel = new(StringComparer.Ordinal);
	// Firm headcount and chart-unit share answer different questions than "who is on
	// the chart". A long tail of one-hit independents can be most of the firms while
	// being a small slice of the chart, so tier guardrails stated as headcount cannot
	// detect Major/MidTier over-representation. These track distinct charting records
	// per year by release-imprint tier, which is the analogue of a Billboard chart
	// entry and the figure historical major/independent splits are quoted against.
	private readonly Dictionary<string, LabelTier> annualChartEntryTierByRecord = new(StringComparer.Ordinal);
	private readonly Dictionary<string, LabelTier> annualTop40TierByRecord = new(StringComparer.Ordinal);
	// The imprint tier above is frozen at release; consolidation moves a record's
	// *owner*, not its imprint, so an absorbed independent still counts as an
	// Independent entry. To report the major-distributed share the late-decade
	// 45-52% consolidation target attaches to, key each distinct entry to its
	// current owner id and resolve the acquisition chain at year-end exactly as
	// the unit rollup does. Owner-Major share is orthogonal to imprint breadth.
	private readonly Dictionary<string, string> annualChartEntryOwnerByRecord = new(StringComparer.Ordinal);
	private readonly Dictionary<string, string> annualTop40OwnerByRecord = new(StringComparer.Ordinal);
	private readonly Dictionary<string, LabelTier> birthTierByLabel = new(StringComparer.Ordinal);
	private readonly Dictionary<string, int> signedDealCountByLabel = new(StringComparer.Ordinal);
	private readonly Dictionary<string, int> completedDealCountByLabel = new(StringComparer.Ordinal);
	private readonly Dictionary<(string Tier, string Format), RevenueRollup> annualMarketRevenue = new();
	private readonly Dictionary<string, string> acquiredBy = new(StringComparer.Ordinal);
	private readonly Dictionary<(int Year, string Format), FormatMixRollup> annualFormatMix = new();
	private readonly HashSet<string> observedReleaseIds = new(StringComparer.Ordinal);
	private readonly HashSet<string> singleReleaseLaneIdsWritten = new(StringComparer.Ordinal);
	private readonly HashSet<string> observedAlbumIds = new(StringComparer.Ordinal);
	private readonly Dictionary<string, DecisionExpectation> decisionExpectations = new(StringComparer.Ordinal);
	private sealed class FormatDecisionCohort {
		public int Year;
		public Genre PrimaryGenre;
		public Genre SecondaryGenre;
		public ReleaseFormat Format;
	}
	private readonly Dictionary<string, FormatDecisionCohort> formatDecisionCohorts = new(StringComparer.Ordinal);
	private readonly Dictionary<string, long> retiredDecisionCohortUnits = new(StringComparer.Ordinal);
	private readonly HashSet<string> retiredAlbumIds = new(StringComparer.Ordinal);
	private readonly Dictionary<string, CareerState> lastDecisionCareerState = new(StringComparer.Ordinal);
	private readonly Dictionary<string, int> observedCareerTransitionYear = new(StringComparer.Ordinal);
	private DecadeAnnualRollup decadeAnnual = new();
	private int decadeAnnualYear;
	private int concentrationYear;
	private int marketRevenueYear;
	private MarketRegion[] regions;
	private int currentAuditWeek;
	private int requestedWeeks = 52;
	private string runName = "audit";
	private ulong? requestedSeed;
	private bool aggregateOnly;
	private bool leanProbe;
	private bool profilePerformance;
	private bool forceDistributionDeal;
	private bool disableLabelLifecycle;
	private bool disableDistributionDeals;
	private bool disableAlbums;
	private bool runGenreMarketV2Probes;
	private bool runArtistPopulationLifecycleProbes;
	private bool catastrophicFailFast;
	private bool catastrophicControlPreflight;
	private bool strict1965AcceptanceGate;
	private string gateControlRun;
	private readonly Dictionary<int, FailFastControlYear> failFastControlYears = new();
	private readonly Dictionary<int, FailFastYearAccumulator> failFastActualYears = new();
	private int failFastCaptureYear;
	private bool catastrophicAbortIssued;

	private class FailFastControlYear {
		public int Releases;
		public int SeasonalityReleases;
		public int ScheduledAlbums;
		public long Units, SingleUnits, AlbumUnits;
		public double Gross, LabelNet, MarketNet;
		public int ReleaseRows, SeasonalityMonths, RevenueRows, SingleRevenueRows, AlbumRevenueRows;
		public bool HasReleases, HasAlbums, HasRevenue;
		public bool IsComplete => HasReleases && HasAlbums && HasRevenue;
		public bool HasStrictFormatUnits => SingleRevenueRows == 1 && AlbumRevenueRows == 1;
	}
	private sealed class ControlCsvTable {
		public readonly string Name;
		public readonly Dictionary<string, int> Columns;
		public readonly List<string[]> Rows;
		public ControlCsvTable(string name, Dictionary<string, int> columns, List<string[]> rows) { Name = name; Columns = columns; Rows = rows; }
		public string Field(string[] row, string name) => row[Columns[name]];
	}
	private sealed class FailFastYearAccumulator : FailFastControlYear { }
	private sealed class CatastrophicAbortException : Exception {
		public readonly string Gate, Metric, State;
		public readonly double EnabledValue, ControlValue;
		public readonly int CompletedYear;
		public CatastrophicAbortException(string gate, string metric, double enabledValue, double controlValue,
			string state, int completedYear = 0) : base($"{gate}: {metric}; enabled={enabledValue}; control={controlValue}; {state}") {
			Gate = gate; Metric = metric; EnabledValue = enabledValue; ControlValue = controlValue;
			State = state; CompletedYear = completedYear;
		}
	}
	private AILabel forcedDealClient;
	private AILabel forcedDealDistributor;
	private float forcedDealInitialAdvance;
	private float forcedDealRoutedTotal;
	private string forcedDealResolution;
	private int forcedDealSignedWeek;
	private int forcedClientInitialRoster;
	private float forcedClientInitialNationalReach;
	private int previousTrackResolutionAttempts;
	private int previousTrackResolutionMisses;
	private int previousTrackArchiveHits;
	private int signedDealEvents;
	private readonly Dictionary<(int Year, int Month), SeasonalityMonthRollup> seasonalityMonths = new();

	public override void _Ready() {
		try {
			ParseArguments();
			if (catastrophicFailFast || catastrophicControlPreflight) LoadCatastrophicFailFastControl();
			if (catastrophicControlPreflight) {
				WriteCatastrophicControlPreflight();
				GetTree().Quit(0);
				return;
			}
			SimulationPerformanceProfiler.Enabled = profilePerformance;
			if (TimeManager.Instance == null || ChartManager.Instance == null) {
				throw new InvalidOperationException("The TimeManager and ChartManager autoloads must be available.");
			}

			if (requestedSeed.HasValue) GD.Seed(requestedSeed.Value);
			if (disableLabelLifecycle) LabelLifecycleManager.Instance?.SetProcessingEnabled(false);
			if (disableDistributionDeals) CompetitorManager.Instance.SetDistributionOfferProcessingEnabled(false);
			if (disableAlbums) CompetitorManager.Instance.SetAlbumsEnabled(false);
			if (runGenreMarketV2Probes) foreach (string result in GenreMarketV2ProbeSuite.Run()) GD.Print("D5_PROBE_PASS: " + result);
			if (runArtistPopulationLifecycleProbes) {
				if (!ArtistPopulationLifecycle.Enabled) throw new InvalidOperationException("Artist population lifecycle probes require --enable-artist-population-lifecycle.");
				foreach (string result in ArtistPopulationLifecycleProbeSuite.Run()) GD.Print("D6_PROBE_PASS: " + result);
			}
			regions = ChartManager.Instance.GetAllRegions().ToArray();
			ValidateLiveRegionTaxonomy(regions);
			OpenOutputs();
			if (ArtistPopulationLifecycle.Enabled && ArtistManager.Instance != null) ArtistManager.Instance.OnPopulationEvent += WriteArtistPopulationEvent;
			if (ArtistPopulationLifecycle.Enabled && LabelLifecycleManager.Instance != null) {
				LabelLifecycleManager.Instance.OnOperatingRosterTargetChanged += WriteOperatingRosterTargetEvent;
				LabelLifecycleManager.Instance.OnRuntimeLabelProfileInitialized += WriteRuntimeLabelProfile;
			}
			if (ArtistPopulationLifecycle.Enabled && RosterManager.Instance != null) {
				RosterManager.Instance.OnDailyTalentMarketCleared += WriteDailyTalentMarket;
				RosterManager.Instance.OnDailyTalentMarketAppointment += WriteDailyTalentAppointment;
			}
			CompetitorManager.Instance.OnDistributionDealEvent += OnDistributionDealEvent;
			CompetitorManager.Instance.OnDistributionOfferAttempt += OnDistributionOfferAttempt;
			CompetitorManager.Instance.OnIndependentDistributionSigned += OnIndependentDistributionSigned;
			CompetitorManager.Instance.OnIndependentTradeFailure += OnIndependentTradeFailure;
			CompetitorManager.Instance.OnReleaseStrategy += OnReleaseStrategy;
			CompetitorManager.Instance.OnCalibrationDecision += OnCalibrationDecision;
			CompetitorManager.Instance.OnReleaseOutcome += OnReleaseOutcome;
			CompetitorManager.Instance.OnFormatMemoryRevision += OnFormatMemoryRevision;
			CompetitorManager.Instance.OnSupplySelection += OnSupplySelection;
			GenreSupplyService.OnTraditionalPopFallback += OnTraditionalPopFallback;
			if (forceDistributionDeal) InstallForcedDistributionDeal();
			WriteDistanceSubstrateRows();
			ChartManager.Instance.OnRecordRetired += OnRecordRetired;
			ChartManager.Instance.OnWeekSettlement += OnWeekSettlement;
			InitializeObservedState();

			var annualWallTime = Stopwatch.StartNew();
		for (int week = 1; week <= requestedWeeks; week++) {
			currentAuditWeek = week;
			AdvanceOneChartWeek();
			// The live tick has completed; run the idempotent roster ownership sweep
			// before recording lifecycle telemetry for this week.
			RosterManager.Instance?.ReconcileEnabledLifecycleForCurrentWeek();
			long captureProfileStart = SimulationPerformanceProfiler.Begin();
				CaptureWeek(week);
				SimulationPerformanceProfiler.EndCaptureWeek(captureProfileStart);
				if (week % 52 == 0) {
					WritePerformanceYear(TimeManager.Instance.CurrentDate.year, annualWallTime.Elapsed.TotalSeconds);
					FlushAnnualStreams();
					annualWallTime.Restart();
				}
			}
			WriteActiveOffChartRetirementRows();
			WriteConcentrationYear();
			WriteGenreShapeYear();
			WriteMarketRevenueYear();
			WriteAnnualFormatMixRows();
			WriteDecadeAnnualYear();
		WriteLiveRecordsSnapshot();
		WriteFormatDecisionCohorts();
			WriteAlbumProjectSnapshots();
			WriteDealMetrics();
			if (forceDistributionDeal) ValidateForcedDistributionDeal();

			FlushAndClose();
			GD.Print($"CHART_AUDIT_COMPLETE run={runName} weeks={requestedWeeks}");
			GetTree().Quit(0);
		} catch (CatastrophicAbortException exception) {
			catastrophicAbortIssued = true;
			WriteCatastrophicAbort(exception);
			WriteLiveRecordsSnapshot();
			FlushAndClose();
			GD.Print($"CHART_AUDIT_ABORTED_CATASTROPHIC run={runName} gate={exception.Gate} metric={exception.Metric} week={currentAuditWeek} date={TimeManager.Instance?.CurrentDate}");
			GetTree().Quit(2);
		} catch (Exception exception) {
			GD.PushError($"CHART_AUDIT_FAILED: {exception}");
			FlushAndClose();
			GetTree().Quit(1);
		}
	}

	private static void AdvanceOneChartWeek() {
		int startingChartWeek = ChartManager.Instance.GetCurrentChartWeek();
		while (ChartManager.Instance.GetCurrentChartWeek() == startingChartWeek && !TimeManager.Instance.IsGameOver) {
			TimeManager.Instance.DebugAdvanceWeek();
		}

		if (ChartManager.Instance.GetCurrentChartWeek() == startingChartWeek) {
			throw new InvalidOperationException("The game ended before another chart week could be simulated.");
		}
	}

	private void ParseArguments() {
		foreach (string argument in OS.GetCmdlineUserArgs()) {
			if (argument.StartsWith("--weeks=", StringComparison.Ordinal)) {
				requestedWeeks = int.Parse(argument[8..], CultureInfo.InvariantCulture);
			} else if (argument.StartsWith("--run=", StringComparison.Ordinal)) {
				runName = SanitizeFileName(argument[6..]);
			} else if (argument.StartsWith("--seed=", StringComparison.Ordinal)) {
				requestedSeed = ulong.Parse(argument[7..], CultureInfo.InvariantCulture);
			} else if (argument == "--aggregate-only") {
				aggregateOnly = true;
			} else if (argument == "--lean-probe") {
				leanProbe = true;
				aggregateOnly = true;
			} else if (argument == "--profile-performance") {
				profilePerformance = true;
			} else if (argument == "--force-distribution-deal") {
				forceDistributionDeal = true;
			} else if (argument.StartsWith("--force-deal-resolution=", StringComparison.Ordinal)) {
				forceDistributionDeal = true;
				forcedDealResolution = argument[24..].ToLowerInvariant();
			} else if (argument == "--disable-label-lifecycle") {
				disableLabelLifecycle = true;
			} else if (argument == "--disable-distribution-deals") {
				disableDistributionDeals = true;
			} else if (argument == "--disable-albums") {
				disableAlbums = true;
			} else if (argument == "--genre-market-v2-probes") {
				runGenreMarketV2Probes = true;
			} else if (argument == "--artist-population-lifecycle-probes") {
				runArtistPopulationLifecycleProbes = true;
			} else if (argument == "--catastrophic-fail-fast") {
				catastrophicFailFast = true;
			} else if (argument == "--catastrophic-control-preflight") {
				catastrophicControlPreflight = true;
			} else if (argument == "--strict-1965-acceptance-gate") {
				strict1965AcceptanceGate = true;
			} else if (argument.StartsWith("--gate-control-run=", StringComparison.Ordinal)) {
				gateControlRun = SanitizeFileName(argument[19..]);
			}
		}

		if (requestedWeeks < 1) throw new ArgumentOutOfRangeException(nameof(requestedWeeks));
		if (catastrophicFailFast && catastrophicControlPreflight)
			throw new ArgumentException("--catastrophic-fail-fast and --catastrophic-control-preflight are mutually exclusive.");
		if ((catastrophicFailFast || catastrophicControlPreflight) && string.IsNullOrEmpty(gateControlRun))
			throw new ArgumentException("Catastrophic fail-fast and control preflight require --gate-control-run=<completed-control-run>.");
		if (strict1965AcceptanceGate && !catastrophicFailFast)
			throw new ArgumentException("--strict-1965-acceptance-gate requires --catastrophic-fail-fast and --gate-control-run=<completed-control-run>.");
		if (!catastrophicFailFast && !catastrophicControlPreflight && !string.IsNullOrEmpty(gateControlRun))
			throw new ArgumentException("--gate-control-run is valid only with --catastrophic-fail-fast or --catastrophic-control-preflight.");
	}

	private void LoadCatastrophicFailFastControl() {
		string directory = ProjectSettings.GlobalizePath("res://SimLogs");
		string releases = Path.Combine(directory, $"{gateControlRun}-release-capacity.csv");
		string seasonality = Path.Combine(directory, $"{gateControlRun}-seasonality-monthly.csv");
		string albumProjects = Path.Combine(directory, $"{gateControlRun}-album-projects.csv");
		string revenue = Path.Combine(directory, $"{gateControlRun}-market-revenue.csv");
		if (!File.Exists(releases) || !File.Exists(seasonality) || !File.Exists(albumProjects) || !File.Exists(revenue))
			throw new InvalidOperationException($"Catastrophic fail-fast control '{gateControlRun}' is incomplete: required release-capacity, seasonality-monthly, album-projects, and market-revenue rows must exist.");
		Dictionary<int, FailFastControlYear> parsed = ParseCatastrophicFailFastControl(
			File.ReadLines(releases), File.ReadLines(seasonality), File.ReadLines(albumProjects), File.ReadLines(revenue), 1960, 1969);
		failFastControlYears.Clear();
		foreach ((int year, FailFastControlYear row) in parsed) failFastControlYears[year] = row;
	}

	private static Dictionary<int, FailFastControlYear> ParseCatastrophicFailFastControl(IEnumerable<string> releaseLines,
		IEnumerable<string> seasonalityLines, IEnumerable<string> albumProjectLines, IEnumerable<string> revenueLines,
		int requiredFirstYear, int requiredLastYear) {
		if (requiredFirstYear > requiredLastYear) throw new ArgumentOutOfRangeException(nameof(requiredFirstYear));
		ControlCsvTable releases = ParseControlTable("release-capacity.csv", releaseLines, "week", "year", "successfulReleases");
		ControlCsvTable seasonality = ParseControlTable("seasonality-monthly.csv", seasonalityLines,
			"year", "month", "successfulReleases", "albumProjectsScheduled");
		ControlCsvTable albumProjects = ParseControlTable("album-projects.csv", albumProjectLines, "projectId", "scheduledWeek");
		ControlCsvTable revenue = ParseControlTable("market-revenue.csv", revenueLines,
			"period", "year", "labelTier", "releaseFormat", "totalMarketUnits", "gross", "labelNet", "marketNet");
		var years = new Dictionary<int, FailFastControlYear>();
		FailFastControlYear Year(int year) {
			if (!years.TryGetValue(year, out FailFastControlYear row)) years[year] = row = new FailFastControlYear();
			return row;
		}

		var weekYears = new Dictionary<int, int>();
		long releaseTotal = 0;
		foreach (string[] fields in releases.Rows) {
			int week = ParseControlInt(releases, fields, "week");
			int year = ParseControlInt(releases, fields, "year");
			int successful = ParseControlInt(releases, fields, "successfulReleases");
			if (week <= 0 || !weekYears.TryAdd(week, year)) throw ControlError(releases, $"invalid or duplicate week {week}");
			if (successful < 0) throw ControlError(releases, $"negative successfulReleases for week {week}");
			FailFastControlYear row = Year(year);
			checked { row.Releases += successful; row.ReleaseRows++; releaseTotal += successful; }
			row.HasReleases = true;
		}

		var months = new HashSet<(int Year, int Month)>();
		long seasonalityReleaseTotal = 0;
		long seasonalityAlbumTotal = 0;
		foreach (string[] fields in seasonality.Rows) {
			int year = ParseControlInt(seasonality, fields, "year");
			int month = ParseControlInt(seasonality, fields, "month");
			int successful = ParseControlInt(seasonality, fields, "successfulReleases");
			int scheduled = ParseControlInt(seasonality, fields, "albumProjectsScheduled");
			if (month is < 1 or > 12 || !months.Add((year, month))) throw ControlError(seasonality, $"invalid or duplicate month {year}-{month}");
			if (successful < 0 || scheduled < 0) throw ControlError(seasonality, $"negative completed count for {year}-{month}");
			FailFastControlYear row = Year(year);
			checked {
				row.SeasonalityReleases += successful; row.SeasonalityMonths++;
				seasonalityReleaseTotal += successful; seasonalityAlbumTotal += scheduled;
			}
		}

		long albumProjectTotal = 0;
		var projectIds = new HashSet<string>(StringComparer.Ordinal);
		foreach (string[] fields in albumProjects.Rows) {
			string projectId = albumProjects.Field(fields, "projectId");
			int week = ParseControlInt(albumProjects, fields, "scheduledWeek");
			if (string.IsNullOrEmpty(projectId) || !projectIds.Add(projectId)) throw ControlError(albumProjects, $"missing or duplicate projectId '{projectId}'");
			if (!weekYears.TryGetValue(week, out int year)) throw ControlError(albumProjects, $"scheduledWeek {week} has no release-capacity year mapping");
			FailFastControlYear row = Year(year);
			checked { row.ScheduledAlbums++; albumProjectTotal++; }
			row.HasAlbums = true;
		}
		if (releaseTotal != seasonalityReleaseTotal)
			throw new InvalidDataException($"Fail-fast control whole-run release reconciliation failed: release-capacity={releaseTotal}, seasonality={seasonalityReleaseTotal}.");
		if (albumProjectTotal != seasonalityAlbumTotal)
			throw new InvalidDataException($"Fail-fast control whole-run Album reconciliation failed: album-projects={albumProjectTotal}, seasonality={seasonalityAlbumTotal}.");

		foreach (string[] fields in revenue.Rows) {
			if (revenue.Field(fields, "period") != "annual" || revenue.Field(fields, "labelTier") != "All") continue;
			int year = ParseControlInt(revenue, fields, "year");
			FailFastControlYear row = Year(year);
			string releaseFormat = revenue.Field(fields, "releaseFormat");
			long units = ParseControlLong(revenue, fields, "totalMarketUnits");
			if (units < 0) throw ControlError(revenue, $"invalid annual All/{releaseFormat} units for year {year}");
			if (releaseFormat == "All") {
				if (row.RevenueRows != 0) throw ControlError(revenue, $"duplicate annual All/All row for year {year}");
				row.Units = units;
				row.Gross = ParseControlDouble(revenue, fields, "gross");
				row.LabelNet = ParseControlDouble(revenue, fields, "labelNet");
				row.MarketNet = ParseControlDouble(revenue, fields, "marketNet");
				if (!IsFinite(row.Gross) || !IsFinite(row.LabelNet) || !IsFinite(row.MarketNet))
					throw ControlError(revenue, $"invalid annual All/All value for year {year}");
				row.RevenueRows = 1;
				row.HasRevenue = true;
			} else if (releaseFormat == "Single") {
				if (row.SingleRevenueRows++ != 0) throw ControlError(revenue, $"duplicate annual All/Single row for year {year}");
				row.SingleUnits = units;
			} else if (releaseFormat == "Album") {
				if (row.AlbumRevenueRows++ != 0) throw ControlError(revenue, $"duplicate annual All/Album row for year {year}");
				row.AlbumUnits = units;
			}
		}

		for (int year = requiredFirstYear; year <= requiredLastYear; year++) {
			if (!years.TryGetValue(year, out FailFastControlYear row) || !row.IsComplete)
				throw new InvalidDataException($"Fail-fast control is missing a complete required year {year}.");
			if (row.ReleaseRows <= 0) throw new InvalidDataException($"Fail-fast control year {year} has no release-capacity rows.");
			if (row.SeasonalityMonths != 12) throw new InvalidDataException($"Fail-fast control year {year} has {row.SeasonalityMonths} seasonality months; expected 12.");
			if (row.RevenueRows != 1) throw new InvalidDataException($"Fail-fast control year {year} has {row.RevenueRows} annual All/All revenue rows; expected 1.");
		}
		return years;
	}

	private static ControlCsvTable ParseControlTable(string name, IEnumerable<string> lines, params string[] requiredColumns) {
		string[] materialized = lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
		if (materialized.Length == 0) throw new InvalidDataException($"Fail-fast control stream {name} is empty.");
		string[] header = ParseCsvFields(materialized[0]);
		if (header.Length == 0) throw new InvalidDataException($"Fail-fast control stream {name} has no header.");
		header[0] = header[0].TrimStart('\uFEFF');
		var columns = new Dictionary<string, int>(StringComparer.Ordinal);
		for (int index = 0; index < header.Length; index++)
			if (!columns.TryAdd(header[index], index)) throw new InvalidDataException($"Fail-fast control stream {name} has duplicate column '{header[index]}'.");
		foreach (string required in requiredColumns)
			if (!columns.ContainsKey(required)) throw new InvalidDataException($"Fail-fast control stream {name} is missing required column '{required}'.");
		var rows = new List<string[]>();
		for (int lineNumber = 1; lineNumber < materialized.Length; lineNumber++) {
			string[] row = ParseCsvFields(materialized[lineNumber]);
			if (row.Length != header.Length) throw new InvalidDataException($"Fail-fast control stream {name} line {lineNumber + 1} has {row.Length} fields; expected {header.Length}.");
			rows.Add(row);
		}
		if (rows.Count == 0) throw new InvalidDataException($"Fail-fast control stream {name} has a header but no rows.");
		return new ControlCsvTable(name, columns, rows);
	}

	internal static string[] ParseCsvFields(string line) {
		var fields = new List<string>();
		var field = new StringBuilder();
		bool quoted = false;
		for (int index = 0; index < line.Length; index++) {
			char value = line[index];
			if (value == '"') {
				if (quoted && index + 1 < line.Length && line[index + 1] == '"') { field.Append('"'); index++; }
				else quoted = !quoted;
			} else if (value == ',' && !quoted) {
				fields.Add(field.ToString()); field.Clear();
			} else field.Append(value);
		}
		if (quoted) throw new InvalidDataException("Fail-fast control CSV contains an unterminated quoted field.");
		fields.Add(field.ToString());
		return fields.ToArray();
	}

	private static int ParseControlInt(ControlCsvTable table, string[] row, string column) =>
		int.TryParse(table.Field(row, column), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value :
		throw ControlError(table, $"column '{column}' contains non-integer value '{table.Field(row, column)}'");
	private static long ParseControlLong(ControlCsvTable table, string[] row, string column) =>
		long.TryParse(table.Field(row, column), NumberStyles.Integer, CultureInfo.InvariantCulture, out long value) ? value :
		throw ControlError(table, $"column '{column}' contains non-integer value '{table.Field(row, column)}'");
	private static double ParseControlDouble(ControlCsvTable table, string[] row, string column) =>
		double.TryParse(table.Field(row, column), NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ? value :
		throw ControlError(table, $"column '{column}' contains non-numeric value '{table.Field(row, column)}'");
	private static InvalidDataException ControlError(ControlCsvTable table, string message) => new($"Fail-fast control stream {table.Name}: {message}.");

	internal static (int Releases, int ScheduledAlbums, long Units) ParseCatastrophicFailFastControlForProbe(
		IEnumerable<string> releases, IEnumerable<string> seasonality, IEnumerable<string> albumProjects,
		IEnumerable<string> revenue, int year) {
		Dictionary<int, FailFastControlYear> parsed = ParseCatastrophicFailFastControl(releases, seasonality, albumProjects, revenue, year, year);
		FailFastControlYear row = parsed[year];
		return (row.Releases, row.ScheduledAlbums, row.Units);
	}

	private void WriteCatastrophicControlPreflight() {
		foreach ((int year, FailFastControlYear row) in failFastControlYears.Where(pair => pair.Value.IsComplete).OrderBy(pair => pair.Key))
			GD.Print($"FAIL_FAST_CONTROL_YEAR year={year} releases={row.Releases} scheduledAlbums={row.ScheduledAlbums} units={row.Units} gross={row.Gross.ToString("R", CultureInfo.InvariantCulture)} labelNet={row.LabelNet.ToString("R", CultureInfo.InvariantCulture)} marketNet={row.MarketNet.ToString("R", CultureInfo.InvariantCulture)}");
		GD.Print($"CHART_AUDIT_CONTROL_PREFLIGHT_COMPLETE control={gateControlRun} years=1960-1969");
	}

	private void CaptureFailFastWeekly(GameDate date, List<RecordRuntimeData> records) {
		if (!catastrophicFailFast) return;
		if (failFastCaptureYear == 0) failFastCaptureYear = date.year;
		else if (ShouldValidateCompletedFailFastYear(failFastCaptureYear, date.year)) {
			ValidateCatastrophicCompletedYear(failFastCaptureYear);
			failFastCaptureYear = date.year;
		}
		if (!failFastActualYears.TryGetValue(date.year, out FailFastYearAccumulator row)) failFastActualYears[date.year] = row = new FailFastYearAccumulator();
		CompetitorManager competitors = CompetitorManager.Instance;
		if (competitors.WeeklySuccessfulReleases < 0 || competitors.WeeklyAlbumProjectsScheduled < 0)
			throw new CatastrophicAbortException("NegativeImpossibleCount", "weeklyReleaseFlow", competitors.WeeklySuccessfulReleases,
				competitors.WeeklyAlbumProjectsScheduled, $"date={date}");
		RecordRuntimeData negativeUnits = records.FirstOrDefault(record => record.unitsThisWeek < 0);
		if (negativeUnits != null)
			throw new CatastrophicAbortException("NegativeImpossibleCount", "weeklyRecordUnits", negativeUnits.unitsThisWeek, 0d,
				$"record={negativeUnits.baseRecord?.recordId} date={date}");
		row.Releases += competitors.WeeklySuccessfulReleases;
		row.ScheduledAlbums += competitors.WeeklyAlbumProjectsScheduled;
		row.Units += records.Sum(record => (long)record.unitsThisWeek);
		row.SingleUnits += records.Where(record => record.baseRecord.format == ReleaseFormat.Single).Sum(record => (long)record.unitsThisWeek);
		row.AlbumUnits += records.Where(record => record.baseRecord.format == ReleaseFormat.Album).Sum(record => (long)record.unitsThisWeek);
		foreach (AILabel label in competitors.GetAllLabels()) {
			ValidateFiniteFinance(label, "cashReserves", label.cashReserves);
			ValidateFiniteFinance(label, "weeklyGrossRevenue", label.weeklyGrossRevenue);
			ValidateFiniteFinance(label, "weeklyCogs", label.weeklyCogs);
			ValidateFiniteFinance(label, "weeklyDistributionSkim", label.weeklyDistributionSkim);
			ValidateFiniteFinance(label, "weeklyArtistRoyalty", label.weeklyArtistRoyalty);
			ValidateFiniteFinance(label, "weeklyNetRevenue", label.weeklyNetRevenue);
			ValidateFiniteFinance(label, "weeklyDistributionIncome", label.weeklyDistributionIncome);
			row.Gross += label.weeklyGrossRevenue; row.LabelNet += label.weeklyNetRevenue;
			row.MarketNet += label.weeklyNetRevenue + label.weeklyDistributionIncome;
		}
		ValidateCatastrophicStructural(date);
	}

	private static void ValidateFiniteFinance(AILabel label, string metric, double value) {
		if (!IsFinite(value)) throw new CatastrophicAbortException("InvalidFinance", metric, value, 0d,
			$"label={label?.labelId} status={label?.status}");
	}

	private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
	internal static bool IsInvalidFailFastFinanceValueForProbe(double value) => !IsFinite(value);
	internal static bool ShouldValidateCompletedFailFastYear(int capturedYear, int currentYear) => capturedYear > 0 && currentYear != capturedYear;

	private void ValidateCatastrophicCompletedYear(int year) {
		if (!failFastControlYears.TryGetValue(year, out FailFastControlYear control) || !control.IsComplete ||
			!failFastActualYears.TryGetValue(year, out FailFastYearAccumulator actual))
			throw new CatastrophicAbortException("MissingControlRow", "completedYear", year, year,
				$"completedYear={year} control={gateControlRun}", year);
		CheckCatastrophicRatio(year, "successfulReleases", actual.Releases, control.Releases);
		ReportCompletedYearRatio(year, "scheduledAlbumProjects", actual.ScheduledAlbums, control.ScheduledAlbums);
		CheckCatastrophicRatio(year, "totalUnits", actual.Units, control.Units);
		CheckCatastrophicRatio(year, "grossRevenue", actual.Gross, control.Gross);
		CheckCatastrophicRatio(year, "labelNet", actual.LabelNet, control.LabelNet);
		CheckCatastrophicRatio(year, "marketNet", actual.MarketNet, control.MarketNet);
		if (strict1965AcceptanceGate && year == 1965) ValidateStrict1965Acceptance(control, actual);
	}

	private void ValidateStrict1965Acceptance(FailFastControlYear control, FailFastYearAccumulator actual) {
		if (!control.HasStrictFormatUnits)
			throw new CatastrophicAbortException("MissingControlRow", "strict1965FormatUnits", 0d, 0d,
				$"completedYear=1965 control={gateControlRun} requires annual All/Single and All/Album market-revenue rows", 1965);
		CheckStrict1965Floor("singleUnits", actual.SingleUnits, control.SingleUnits, .85d);
		CheckStrict1965Floor("albumUnits", actual.AlbumUnits, control.AlbumUnits, .80d);
		CheckStrict1965Floor("totalUnits", actual.Units, control.Units, .85d);
		CheckStrict1965Floor("grossRevenue", actual.Gross, control.Gross, .85d);
		CheckStrict1965Floor("labelNet", actual.LabelNet, control.LabelNet, .85d);
		CheckStrict1965Floor("marketNet", actual.MarketNet, control.MarketNet, .85d);
	}

	private void CheckStrict1965Floor(string metric, double enabled, double control, double floor) {
		if (!IsFinite(enabled) || !IsFinite(control) || control <= 0d)
			throw new CatastrophicAbortException("InvalidAnnualComparison", metric, enabled, control,
				$"completedYear=1965 strictFloor={floor.ToString("F2", CultureInfo.InvariantCulture)}", 1965);
		double ratio = enabled / control;
		if (!IsFinite(ratio) || ratio < floor)
			throw new CatastrophicAbortException("Strict1965Acceptance", metric, enabled, control,
				$"completedYear=1965 ratio={ratio.ToString("F6", CultureInfo.InvariantCulture)} floor={floor.ToString("F2", CultureInfo.InvariantCulture)}", 1965);
	}

	// scheduledAlbumProjects is reported, not enforced, because it is not an independent
	// measurement. albumProjectsScheduled + singleReleases == successfulReleases holds exactly
	// in every month of every run recorded, so the ratio factorises without residue:
	//
	//     scheduledAlbumProjects_ratio = successfulReleases_ratio x albumShare_ratio
	//
	// The mix term is invariant to the changes this gate exists to police - across every
	// configuration measured, from head to the worst abort, it moves 1.1602 -> 1.1798 at 1966
	// while the volume term moves 1.0715 -> 1.1341. Enforcing the product therefore charged each
	// change a fixed ~1.17 multiplier for the enabled route's authored LP-transition schedule,
	// converting the declared 1.30 ceiling into an undeclared 1.11 ceiling on release volume -
	// a quantity successfulReleases already bands at 1.30 and scores at 1.07-1.13.
	//
	// The transition difference squeezes the metric from both ends. On the unmodified head run
	// the mix term sits 0.099 below the ceiling at 1960 and 0.036 above the floor at 1962, with
	// nothing under test, so banding the mix alone only relocates the squeeze rather than
	// removing it. Volume and economics remain fatal and unchanged: successfulReleases,
	// totalUnits, grossRevenue, labelNet and marketNet all police this route with wide margins.
	private void ReportCompletedYearRatio(int completedYear, string metric, double enabled, double control) {
		double ratio = IsFinite(enabled) && IsFinite(control) && control != 0d ? enabled / control : double.NaN;
		string state = $"completedYear={completedYear} ratio=" +
			(IsFinite(ratio) ? ratio.ToString("F6", CultureInfo.InvariantCulture) : "undefined") + " band=reported";
		GD.Print($"COMPLETED_YEAR_RATIO_REPORTED metric={metric} completedYear={completedYear} " +
			$"enabled={enabled.ToString("R", CultureInfo.InvariantCulture)} " +
			$"control={control.ToString("R", CultureInfo.InvariantCulture)} state={state}");
		catastrophicFailFastWriter?.WriteLine(string.Join(",", new[] {
			Csv("CompletedYearRatioReported"), Csv(metric), enabled.ToString("R", CultureInfo.InvariantCulture),
			control.ToString("R", CultureInfo.InvariantCulture), completedYear.ToString(CultureInfo.InvariantCulture),
			currentAuditWeek.ToString(CultureInfo.InvariantCulture), Csv(TimeManager.Instance?.CurrentDate.ToString()), Csv(state) }));
		catastrophicFailFastWriter?.Flush();
	}

	private void CheckCatastrophicRatio(int completedYear, string metric, double enabled, double control) {
		if (!IsFinite(enabled) || !IsFinite(control))
			throw new CatastrophicAbortException("InvalidAnnualComparison", metric, enabled, control,
				$"completedYear={completedYear} non-finite completed-year value", completedYear);
		if (control == 0d) {
			if (metric is "successfulReleases" or "scheduledAlbumProjects" && enabled == 0d) return;
			return; // Zero-denominator values are logged in the endpoint stream, never inferred catastrophic.
		}
		double ratio = enabled / control;
		if (IsCatastrophicFailFastRatioForProbe(enabled, control))
			throw new CatastrophicAbortException("CompletedYearCatastrophicDivergence", metric, enabled, control,
				FormatCompletedYearRatioState(completedYear, ratio), completedYear);
	}

	private static string FormatCompletedYearRatioState(int completedYear, double ratio) =>
		$"completedYear={completedYear} ratio={ratio.ToString("F6", CultureInfo.InvariantCulture)} band=[0.70,1.30]";
	internal static string FormatCompletedYearRatioStateForProbe(int completedYear, double ratio) =>
		FormatCompletedYearRatioState(completedYear, ratio);

	internal static bool IsCatastrophicFailFastRatioForProbe(double enabled, double control) {
		if (!IsFinite(enabled) || !IsFinite(control)) return true;
		if (control == 0d) return false;
		double ratio = enabled / control;
		return !IsFinite(ratio) || ratio < .70d || ratio > 1.30d;
	}

	private void ValidateCatastrophicStructural(GameDate date) {
		var owners = new Dictionary<SimulatedArtist, string>();
		foreach (AILabel label in ChartManager.Instance.GetAllLabels().Where(label => label?.roster != null)) {
			if (!label.IsActive && label.CurrentRosterSize > 0)
				throw new CatastrophicAbortException("TerminalRoster", "rosterHeadcount", label.CurrentRosterSize, 0d, $"label={label.labelId} status={label.status}");
			if (label.IsActive && (label.CurrentRosterSize > label.OperatingRosterTarget || label.CurrentRosterSize > label.maxRosterSize))
				throw new CatastrophicAbortException("RosterCapacity", "rosterHeadcount", label.CurrentRosterSize, Math.Min(label.OperatingRosterTarget, label.maxRosterSize), $"label={label.labelId}");
			foreach (SimulatedArtist artist in label.roster) {
				if (artist == null || !owners.TryAdd(artist, label.labelId)) throw new CatastrophicAbortException("OwnershipConflict", "artistOwnership", 2d, 1d, $"artist={artist?.artistId}");
				if (artist.labelId != label.labelId)
					throw new CatastrophicAbortException("OwnershipConflict", "artistLabelId", 0d, 1d, $"label={label.labelId} artist={artist.artistId} artistLabel={artist.labelId}");
				if (!artist.isActive || artist.lifecycleStatus is ArtistLifecycleStatus.Retired or ArtistLifecycleStatus.Disbanded)
					throw new CatastrophicAbortException("TerminalRoster", "artistLifecycle", 1d, 0d, $"label={label.labelId} artist={artist.artistId} state={artist.lifecycleStatus} date={date}");
			}
		}
	}

	internal static bool IsRuntimeBirthWeekSigningViolationForProbe(string eventType, AILabel label, int eventWeek) =>
		eventType is "signing" or "re-signing" && label?.populationOrigin == LabelPopulationOrigin.RuntimeFounded &&
		label.runtimeBirthWeek > 0 && eventWeek <= label.runtimeBirthWeek;

	private void WriteCatastrophicAbort(CatastrophicAbortException exception) {
		catastrophicFailFastWriter?.WriteLine(string.Join(",", new[] { Csv(exception.Gate), Csv(exception.Metric), exception.EnabledValue.ToString("R", CultureInfo.InvariantCulture), exception.ControlValue.ToString("R", CultureInfo.InvariantCulture),
			exception.CompletedYear > 0 ? exception.CompletedYear.ToString(CultureInfo.InvariantCulture) : string.Empty,
			currentAuditWeek.ToString(CultureInfo.InvariantCulture), Csv(TimeManager.Instance?.CurrentDate.ToString()), Csv(exception.State) }));
		catastrophicFailFastWriter?.Flush();
	}

	private static void ValidateLiveRegionTaxonomy(MarketRegion[] liveRegions) {
		string[] expected = { "eastcoast", "greatlakes", "greatplains", "deepsouth", "southwest", "rockies", "westcoast" };
		string[] actual = liveRegions.Select(region => region?.regionId).OrderBy(id => id, StringComparer.Ordinal).ToArray();
		string[] expectedSorted = expected.OrderBy(id => id, StringComparer.Ordinal).ToArray();
		if (!actual.SequenceEqual(expectedSorted, StringComparer.Ordinal))
			throw new InvalidOperationException($"Live region taxonomy mismatch: [{string.Join(",", actual)}].");
		if (DistanceModel.GetHubCityIdForRegion("greatlakes") != "chicago" ||
			DistanceModel.GetHubCityIdForRegion("greatplains") != "minneapolis")
			throw new InvalidOperationException("Great Lakes/Great Plains hub resolution is not live.");
		if (liveRegions.Any(region => DistanceModel.GetHubCityIdForRegion(region.regionId) == "new_york" && region.regionId != "eastcoast"))
			throw new InvalidOperationException("A live non-East-Coast region resolved to the New York fallback hub.");
		if (ChartManager.GetNeighborRegionIds("greatlakes").Length == 0 ||
			ChartManager.GetNeighborRegionIds("greatplains").Length == 0)
			throw new InvalidOperationException("Great Lakes/Great Plains breakout neighbor graph is empty.");
	}

	private void InstallForcedDistributionDeal() {
		if (forcedDealResolution != null && forcedDealResolution is not ("exit" or "renew" or "absorb")) {
			throw new ArgumentException($"Unknown forced deal resolution '{forcedDealResolution}'.");
		}
		// CaptureWeek asserts the client's weekly skim equals its distributor's weekly
		// distribution income, which only holds while that distributor carries exactly one
		// client. The resolution cases have always disabled offer processing and so held it;
		// the plain case did not, and once the section 33 market began signing freely the
		// forced distributor (the first active Major) picked up three more clients in week 5
		// and the equality became unsatisfiable. Disable it for every forced-deal run so the
		// harness tests the routing it means to test rather than the ambient deal market.
		CompetitorManager.Instance.SetDistributionOfferProcessingEnabled(false);
		IReadOnlyList<AILabel> labels = CompetitorManager.Instance.GetAllLabels();
		forcedDealClient = labels.FirstOrDefault(label => label.tier != LabelTier.Major &&
			label.IsActive && CompetitorManager.Instance.GetLabelActiveRecordCount(label.labelId) > 0);
		forcedDealDistributor = labels.FirstOrDefault(label => label.tier == LabelTier.Major && label.IsActive);
		if (forcedDealClient == null || forcedDealDistributor == null) {
			throw new InvalidOperationException("Could not find labels for a forced distribution deal.");
		}

		string grantedRegion = regions.Select(region => region.regionId)
			.FirstOrDefault(regionId => !forcedDealClient.HasDistributionInRegion(regionId));
		grantedRegion ??= regions.First().regionId;
		forcedDealInitialAdvance = 5000f;
		forcedDealSignedWeek = ChartManager.Instance.GetCurrentChartWeek();
		forcedClientInitialRoster = forcedDealClient.CurrentRosterSize;
		forcedClientInitialNationalReach = forcedDealClient.nationalReach;
		if (forcedDealResolution == "exit") forcedDealClient.ownedReach = 0.95f;
		else if (forcedDealResolution == "renew") forcedDealClient.ownedReach = 0.50f;
		else if (forcedDealResolution == "absorb") {
			forcedDealClient.ownedReach = 0.05f;
			// Absorption is no longer triggered by ownsMasters; it is gated to the late-decade
			// consolidation window, Major acquirers and charted indie clients. This harness
			// installs its deal in 1960, so force this one client's expiry to absorb.
			CompetitorManager.Instance.ForceConsolidationForTest(forcedDealClient.labelId);
		}
		forcedDealClient.activeDeal = new DistributionDeal {
			distributorId = forcedDealDistributor.labelId,
			reachGranted = forcedDealResolution == "absorb" ? 0.80f : forcedDealResolution == "exit" ? 0.10f : 0.50f,
			grantedRegions = new[] { grantedRegion },
			marginSkim = 0.20f,
			ownsMasters = forcedDealResolution == "absorb",
			advance = forcedDealInitialAdvance,
			unrecoupedAdvance = forcedDealInitialAdvance,
			signedWeek = forcedDealSignedWeek,
			termWeeks = forcedDealResolution == null ? 52 : 1,
			origin = DealOrigin.LabelSought
		};
		if (!forcedDealClient.HasDistributionInRegion(grantedRegion)) {
			throw new InvalidOperationException("Forced deal did not grant its configured region.");
		}
	}

	private void ValidateForcedDistributionDeal() {
		float expectedRemaining = Mathf.Max(0f, forcedDealInitialAdvance - forcedDealRoutedTotal);
		if (forcedDealResolution == null) {
			if (!Mathf.IsEqualApprox(forcedDealClient.activeDeal.unrecoupedAdvance, expectedRemaining)) {
				throw new InvalidOperationException($"Deal recoup mismatch: expected {expectedRemaining}, got {forcedDealClient.activeDeal.unrecoupedAdvance}.");
			}
			return;
		}
		if (forcedDealResolution == "exit" && forcedDealClient.activeDeal != null) {
			throw new InvalidOperationException("Forced exit deal did not terminate.");
		}
		if (forcedDealResolution == "renew" && (forcedDealClient.activeDeal == null || forcedDealClient.activeDeal.signedWeek <= forcedDealSignedWeek)) {
			throw new InvalidOperationException("Forced renewal did not reset its signed week.");
		}
		if (forcedDealResolution is "exit" or "renew" && forcedDealClient.nationalReach <= forcedClientInitialNationalReach) {
			throw new InvalidOperationException("A completed forced deal did not leave the client with earned national reach.");
		}
		// Subsidiary outcome: the client rolled up to the Major (ownerLabelId set, deal converted
		// to ownership) but is NOT shut down -- it stays in GetOperatingLabels and keeps its
		// roster (retention, not the old transfer-to-zero). Roster size churns over the run, so
		// the retention check is that it still operates a non-empty roster, not exact equality.
		if (forcedDealResolution == "absorb" && (!forcedDealClient.IsSubsidiary ||
			forcedDealClient.ownerLabelId != forcedDealDistributor.labelId ||
			!CompetitorManager.Instance.GetOperatingLabels().Contains(forcedDealClient) ||
			forcedDealClient.activeDeal != null || forcedDealClient.CurrentRosterSize <= 0 ||
			forcedClientInitialRoster <= 0)) {
			throw new InvalidOperationException("Forced absorption did not convert the client into a retained subsidiary.");
		}
	}

	private void OpenOutputs() {
		string outputDirectory = ProjectSettings.GlobalizePath("res://SimLogs");
		Directory.CreateDirectory(outputDirectory);
		recordWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-records.csv"));
		weekWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-weeks.csv"));
		lifecycleWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-lifecycles.csv"));
		breakoutWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-breakout-funnel.csv"));
		retirementWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-retirement.csv"));
		tierVolumeWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-tier-volume.csv"));
		labelFinanceWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-label-finance.csv"));
		dealLedgerWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-deal-ledger.csv"));
		labelDirectoryWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-label-directory.csv"));
		independentDistributorWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-independent-distributors.csv"));
		independentDistributionEventWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-independent-distribution-events.csv"));
		independentTradeFailureWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-independent-trade-failures.csv"));
		concentrationWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-concentration.csv"));
		marketRevenueWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-market-revenue.csv"));
		releaseCapacityWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-release-capacity.csv"));
		seasonalityMonthlyWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-seasonality-monthly.csv"));
		albumChartWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-album-chart.csv"));
		albumCompositionWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-album-composition.csv"));
		formatMixWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-format-mix.csv"));
		retiredTrackWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-retired-track-availability.csv"));
		releaseStrategyWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-release-strategy.csv"));
		traditionalPopFallbackWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-traditional-pop-fallbacks.csv"));
		genreShapeWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-genre-decade-shape.csv"));
		releaseOutcomeWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-release-outcomes.csv"));
		if (GenreMarketV2.Enabled) {
			singleReleaseLaneWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-single-release-lanes.csv"));
			// single-demand-stages is a per-record per-region per-week diagnostic (~1.8 GB/decade);
			// suppress under --lean-probe like the heavy settlement dumps below.
			if (!leanProbe) singleDemandStagesWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-single-demand-stages.csv"));
		}
		revenueMemoryWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-revenue-memory.csv"));
		liveRecordsSnapshotWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-live-records-snapshot.csv"));
		priorCostAssumptionWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-prior-cost-assumptions.csv"));
		albumTrackLinkWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-album-track-links.csv"));
		calibrationDecisionWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-calibration-decisions.csv"));
		forkRatioWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-fork-ratios.csv"));
		a3EconomicDecisionWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-a3-economic-decisions.csv"));
		albumProjectWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-album-projects.csv"));
		albumProjectDemandWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-album-project-demand.csv"));
		albumProjectWeeklyWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-album-project-weekly.csv"));
		decadeAnnualRollupWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-decade-annual-rollup.csv"));
		cityRosterWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-city-roster.csv"));
		distanceMatrixWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-distance-matrix.csv"));
		labelGeographyWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-label-geography.csv"));
		geographyMetricsWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-geography-metrics.csv"));
		dealMetricsWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-deal-metrics.csv"));
		genreCatalogWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-genre-catalog.csv"));
		genreMarketWeeklyWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-genre-market-weekly.csv"));
		recordGenreExplanationWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-record-genre-explanation.csv"));
		albumDemandExplanationWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-album-demand-explanation.csv"));
		formatDecisionExplanationWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-format-decision-explanation.csv"));
		formatDecisionCohortWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-format-decision-cohorts.csv"));
		formatDecisionCohortDetailWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-format-decision-cohort-details.csv"));
		supplySelectionWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-supply-selections.csv"));
		genreEventsWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-genre-events.csv"));
		specialProductsWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-special-products.csv"));
		if (GenreMarketV2.Enabled) rosterLifecycleWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-roster-lifecycle.csv"));
		if (ArtistPopulationLifecycle.Enabled && GenreMarketV2.Enabled)
			labelScoutingVacancyWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-label-scouting-vacancy-weekly.csv"));
		if (GenreMarketV2.Enabled) {
			marketClearingWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-market-clearing-weekly.csv"));
			marketSpilloverWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-market-spillover-weekly.csv"));
			formatMemoryAdjustmentWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-format-memory-adjustment.csv"));
			// Per-settlement per-region per-week economic-debug dumps (album-realization-bridge
			// ~4.6 GB, settlement-regional ~1.8 GB, settlement ~0.75 GB per decade). Not consumed by
			// the breadth/owner-Major/album-project analysis, so suppress under --lean-probe. The
			// AcknowledgeSettlementAudit sim invariant in OnWeekSettlement still runs regardless.
			if (!leanProbe) {
				completedWeekSettlementWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-completed-week-settlement.csv"));
				completedWeekSettlementRegionalWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-completed-week-settlement-regional.csv"));
				albumRealizationBridgeWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-album-realization-bridge.csv"));
			}
			formatMemoryRevisionWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-format-memory-revisions.csv"));
		}
		if (ArtistPopulationLifecycle.Enabled) {
			artistPopulationEventsWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-artist-population-events.csv"));
			artistPopulationWeeklyWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-artist-population-weekly.csv"));
			artistLaborMarketWeeklyWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-artist-labor-market-weekly.csv"));
			artistCohortAnnualWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-artist-cohort-annual.csv"));
			artistProjectIdentityWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-artist-project-identity.csv"));
			labelOperatingTargetEventWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-label-operating-target-events.csv"));
			runtimeLabelProfileWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-runtime-label-profiles.csv"));
			firstChartEventWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-first-chart-events.csv"));
			distributionOfferAttemptWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-distribution-offer-attempts.csv"));
			dailyTalentMarketWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-daily-talent-market.csv"));
			dailyTalentAppointmentWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-daily-talent-appointments.csv"));
			if (catastrophicFailFast) catastrophicFailFastWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-catastrophic-fail-fast.csv"));
		}
		if (profilePerformance) performanceProfileWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-performance-profile.csv"));

		recordWriter.WriteLine("week,year,recordId,title,artistId,labelId,labelTier,isPlayerOwned,genre,quality,weeksSinceRelease,weeksOnChart,currentPosition,previousPosition,unitsThisWeek,totalUnitsSold,awareness,radioHeat,wordOfMouth,momentum,saturation,chartPoints,chartCutoffPoints,distanceFrom100Cutoff,regionalBreakoutCount,neighboringMarketTestCount,crossoverCandidateStrength,peakRegionalBreakoutStrength,sustainedSalesVelocity,unmetRegionalDemand,coveredRegionCount,initialLaunchAwareness,initialLaunchStock,launchCareerState,perceivedQualityMultiplier");
		weekWriter.WriteLine("week,year,totalChartUnits,totalMarketUnits,numberOneRecordId,numberOneUnitsThisWeek,newEntriesTop100,newEntriesTop40,exitsTop100,activeRecords,newRecords,retiredRecords");
		lifecycleWriter.WriteLine("week,recordId,title,debutPosition,peakPosition,weeksOnChart,weeksAtNumberOne,lifetimeUnitsSold,leftCensoredAtRunStart");
		breakoutWriter.WriteLine("week,recordId,labelTier,careerState,regionId,distributionRegionCoverage,weeksSinceRelease,weekStartStock,preRestockStock,rawSales,unitsSoldThisWeek,unitsBackordered,awareBuyers,conversionRate,restockTriggered,requestedRestockAmount,restockAmount,maxCapacity,capacityCapped,breakoutScore,breakoutStage,tractionWeeks,sustainedGrowthWeeks,salesVelocity,volumeInput,velocityInput,audienceInput,mediaInput,genreFitInput,qualityInput,unmetDemandInput,discoveryVisibilityMultiplier,breakoutAwarenessGain,breakoutRadioGain,breakoutWordOfMouthGain,neighboringMarketTestStrength,breakoutSourceRegionId");
		retirementWriter.WriteLine("week,status,recordId,labelTier,weeksSinceRelease,weeksOnChart,weeksSinceLastTop100,weeksSinceSalesAboveFloor,floorBreachAge,unitsThisWeek,totalRadioPlay");
		tierVolumeWriter.WriteLine("week,labelTier,launchRecords,launchUnits,middleRecords,middleUnits,catalogTailRecords,catalogTailUnits,totalRecords,totalUnits");
		labelFinanceWriter.WriteLine("week,year,labelId,labelName,archetype,isHistorical,labelTier,status,cashReserves,monthlyRevenue,monthlyExpenses,weeklyGross,weeklyCogs,weeklySkim,weeklyRoyalty,weeklyNet,weeklyDistributionIncome,ownedReach,borrowedReach,nationalReach,capability,dealDistributorId,dealUnrecoupedAdvance,outstandingWholesaleReceivables,lifetimeWholesaleWriteOffs,independentRegions");
		dealLedgerWriter.WriteLine("eventWeek,year,resolution,origin,distributorId,distributorName,clientId,clientName,reachGranted,marginSkim,ownsMasters,advance,signedWeek,termWeeks,dependency");
		labelDirectoryWriter.WriteLine("labelId,labelName,archetype,isHistorical,initialTier");
		independentDistributorWriter.WriteLine("distributorId,distributorName,regionId,regionPopulation,recordStoreCount," +
			"departmentStoreCount,inventoryDepth,difficulty,clientCapacity,reliability,paymentTermWeeks,returnAllowance,reportingHonesty");
		independentDistributionEventWriter.WriteLine("week,year,labelId,labelName,labelTier,distributorId,distributorName," +
			"regionId,provenInRegion,coveredRegionCount,coveredMarketShare,ownedReachBefore,ownedReachAfter,houseClientCount,houseClientCapacity");
		independentTradeFailureWriter.WriteLine("year,distributorId,distributorName,regionId,clientsDropped,reachLostPerClient,housesRemaining,survivalRate");
		concentrationWriter.WriteLine("year,c4ChartShare,c8ChartShare,firmsCharting,indieFamilyChartShare,majorFamilyChartShare,totalChartUnits,smallFirmsCharting,boutiqueFirmsCharting,independentFirmsCharting,midTierFirmsCharting,majorFirmsCharting,cumulativeFirmsCharting,cumulativeSmallFirmsCharting,cumulativeBoutiqueFirmsCharting,cumulativeIndependentFirmsCharting,cumulativeMidTierFirmsCharting,cumulativeMajorFirmsCharting,cumulativeExactLabelNamesCharting,chartEntries,chartEntriesSmall,chartEntriesBoutique,chartEntriesIndependent,chartEntriesMidTier,chartEntriesMajor,top40Entries,top40Small,top40Boutique,top40Independent,top40MidTier,top40Major,ownerMajorEntries,ownerMajorFamilyEntries,ownerMajorTop40Entries,ownerMajorFamilyTop40Entries");
		firstChartEventWriter?.WriteLine("week,year,date,observationKind,leftCensoredAtRunStart,recordId,title,releaseLabelId,currentOwnerLabelId,labelName,labelOrigin,runtimeBirthWeek,birthTier,firstChartTier,labelStatus,isHistorical,recordAge,currentPosition,unitsThisWeek,chartPoints,publishedCutoffPoints,quality,peakRegionalBreakoutStrength,bestStrongRegionPeak,regionalBreakoutCount,coveredRegionCount,signedDealCount,completedDealCount,activeDeal,dealOrigin,dealSignedWeek,permanentNationalReach,borrowedReach,effectiveNationalReach,ownedReach,distributionStrength,permanentRegionCount,grantedRegionCount,initialLaunchAwareness,initialLaunchStock");
		distributionOfferAttemptWriter?.WriteLine("week,year,clientId,clientName,clientTier,clientOrigin,monthsActive,ownedReach,nationalReach,bestAnyRegionPeak,bestStrongRegionPeak,bestPersistentEvidenceQuality,persistentRegionalEvidence,legacyQualityAndCurrentSalesEvidence,legacyNationalReachGate,pushEvidence,pushChancePassed,pullChancePassed,outcome,distributorId");
		marketRevenueWriter.WriteLine("period,week,year,labelTier,releaseFormat,totalMarketUnits,gross,labelNet,distributionIncome,marketNet");
		marketClearingWriter?.WriteLine("week,year,regionId,activeIntentCount,rawSingleDemand,rawAlbumDemand,rawTotalDemand,serviceableSingleIntent,serviceableAlbumIntent,effectiveAlbumIntent,albumOverlapPressure,singleFormatBudget,albumFormatBudget,serviceableTotalIntent,purchaseCapacity,baseCapacity,albumChannelCapacity,localCleared,unusedAfterLocal,exportBudget,exportedCapacity,importLimit,importedCapacity,spilloverCleared,clearedSingleUnits,clearedAlbumUnits,clearedTotalUnits,unusedCapacity,rationingFactor,physicalBackorders,marketDisplacedDemand,residualDisplacedDemand,inventoryViolationCount,allocationViolationCount,reconciliationDelta,settlementDelta");
		marketSpilloverWriter?.WriteLine("week,year,donorRegionId,recipientRegionId,donorUnusedLocal,donorExportBudget,recipientResidualDemand,recipientImportLimit,transferredCapacity,clearedSingleUnits,clearedAlbumUnits,edgeViolationCount,reconciliationDelta");
		completedWeekSettlementWriter?.WriteLine("week,year,settlementId,recordId,labelId,labelTier,format,releaseLane,genre,regionalUnits,totalUnits,gross,manufacturingCost,artistRoyalty,distributionSkim,labelNet,distributionRecipientLabelId,distributionIncome,marketNet,retiredAfterSettlement,bookedCount,auditedCount");
		completedWeekSettlementRegionalWriter?.WriteLine("week,year,settlementId,recordId,regionId,rawIntent,serviceableIntent,localCleared,spilloverCleared,finalCleared,physicalBackorders,marketDisplacedDemand,inventoryMovement");
		albumRealizationBridgeWriter?.WriteLine("week,year,settlementId,recordId,labelId,labelTier,genre,regionId,releaseYear,ageWeeks,buyerPool,awareness,observedPenetration,effectivePenetration,peakEffectivePenetration,exhaustion,catalogDecayMultiplier,formatTilt,conversion,cannibalizationSuppression,rawDemandBeforeCannibalization,rawDemandAfterCannibalization,roundedRawIntent,unitsInStoresBeforeSale,storeCapacity,serviceableIntent,localCleared,spilloverCleared,finalCleared,physicalBackorders,marketDisplacedDemand,currentPosition,weeksSinceLastCharted,weeksSinceSalesAboveFloor,retirementFloor,retiredAfterSettlement");
		formatMemoryRevisionWriter?.WriteLine("week,year,releaseId,labelId,projectId,releaseLane,estimatorLane,format,genre,releaseAge,revisionKind,revisionOrdinal,releaseTimeExpectedNet,ageMatchedExpectedNet,realizedNetToDate,estimatedOutcomeNet,opportunityScale,normalizedResidual,maturityWeight,recencyWeight,replacedPriorRevision,finalized,nonFiniteViolation");
		formatMemoryAdjustmentWriter?.WriteLine("week,year,recordId,labelId,memoryScope,rawSingleConfidence,rawAlbumConfidence,effectiveSingleConfidence,effectiveAlbumConfidence,singleCapApplied,albumCapApplied");
		releaseCapacityWriter.WriteLine("week,year,releaseRollsFired,successfulReleases,failedReleaseRolls,cooldownMismatchRolls,otherFailedRolls,failedRollRate,cooldownMismatchRate");
		seasonalityMonthlyWriter.WriteLine("seed,enabled,year,month,liveWeeks,singleSalesMultiplier,albumSalesMultiplier,radioOpportunity,venueAttendanceMultiplier,recordingCostMultiplier,marketingEfficiencyMultiplier,artistAvailabilityMultiplier,singleUnits,albumUnits,singleGross,albumGross,releaseRolls,successfulReleases,singleReleases,albumProjectsScheduled,albumDrops,productionSpend,productionEvents,marketingSpend,marketingEvents,scoutingRolls,signings,meanRadioPlay");
		albumChartWriter.WriteLine("week,year,month,chartSize,position,previousPosition,recordId,title,artistId,labelId,genre,albumFormat,unitsThisWeek,totalUnitsSold,weeksOnChart,pooledAppeal,thematicCohesion,packaging");
		albumCompositionWriter.WriteLine("week,year,recordId,artistId,genre,albumFormat,thematicCohesion,pooledAppeal,trackCount,reusedSingleTracks,nonSingleTracks,compTrackShare,runtimeMinutes,packaging,isStereo");
		formatMixWriter.WriteLine("period,week,year,releaseFormat,releases,releaseShare,units,unitShare,gross,revenueShare,cogs,distributionSkim,artistRoyalty,labelNet");
		retiredTrackWriter.WriteLine("week,year,resolutionAttempts,retiredArchiveHits,unarchivedMisses,cumulativeAttempts,cumulativeRetiredArchiveHits,cumulativeUnarchivedMisses");
		releaseStrategyWriter.WriteLine("week,year,recordId,labelId,tier,artistId,genre,rawSecondaryGenre,careerState,projectedSingleNet,projectedAlbumNet,confidenceSingle,confidenceAlbum,chosenFormat,projectId,strategy,projectedOrphanSingleNet,projectedAlbumStandaloneNet,projectedAlbumWithPromoNet,promoSingleId,bucketMeanNet,singleProductionCost,singleNetMarginPerUnit,expectedSingleUnits,albumDemandFactor,substitutionK,substitutionCap,substitutionPropensity,expectedOverlapFraction,divertedUnits,albumMarginPerUnit,cannibalizationLoss,cannibalizationCharged,expectedPromoLift,expectedPromoSingleNet,promoAdvantage,albumChoiceProbability,formatChoiceRoll,albumCapacityReroute");
		releaseOutcomeWriter.WriteLine("week,year,labelId,recordId,format,genre,memoryEligible,lifetimeLabelNet,sunkProductionCost,realizedNet");
		singleReleaseLaneWriter?.WriteLine("week,year,recordId,projectId,releaseLane,labelId,tier,artistId,genre,careerState,hookStrength,productionQuality,danceability,quality,enabledOpportunityMass,acceptedOpportunityMass,cohortNormalizer,normalizerSource,coldStartFallback");
		singleDemandStagesWriter?.WriteLine("week,year,recordId,releaseLane,region,age,potentialAudience,baselineAwareness,earnedDiscoveryExposure,awareBuyers,intrinsicQualityFactor,acceptanceFactor,formatFactor,intrinsicConversionRate,rawDemand,serviceableDemand,clearedUnits,chartSignal,momentumSignal,radioSignal,inventoryFulfillmentRate,marketFulfillmentRate");
		revenueMemoryWriter.WriteLine("week,year,labelId,format,emaNetPerRelease,releasesObserved");
		liveRecordsSnapshotWriter.WriteLine("week,year,recordId,labelId,artistId,format,ageWeeks,lifetimeLabelNet,sunkProductionCost,observedNetLowerBound,currentPosition,totalUnitsSold");
		priorCostAssumptionWriter.WriteLine("week,year,recordId,assumedCompilationCost,actualAlbumFormat");
		albumTrackLinkWriter.WriteLine("week,year,albumRecordId,artistId,sourceRecordId,freshnessApplied,timesCompUsedAtGeneration,sourceHitAgeWeeks");
		calibrationDecisionWriter.WriteLine("week,year,recordId,labelId,artistId,genre,careerState,qualityEstimate,reachFactor,genreSinglesMarketFactor,singleProductionCost,chosenFormat");
		forkRatioWriter.WriteLine("week,year,recordId,labelId,artistId,genre,rawSecondaryGenre,genreGroup,careerState,careerBand,qualityEstimate,qualityQuartile,reachFactor,genreSinglesMarketFactor,priorSingleNet,priorAlbumNet,projectedSingleNet,projectedAlbumNet,albumMinusSingleNet,albumToSingleRatio,chosenFormat");
		a3EconomicDecisionWriter.WriteLine("week,year,recordId,labelId,artistId,genre,rawSecondaryGenre,genreGroup,careerState,careerBand,qualityEstimate,qualityQuartile,statureMultiplier,careerStateTransitionOccurredThisYear,reachFactor,albumDemandFactor,hitInventoryCohort,compCostWeight,expectedFormatMultiplier,actualAlbumFormat,releasedSingleIdsExamined,resolvedSingles,chartedSingles,hitScore,unweightedHitUnits,weightedHitUnits,affinityUnits,totalExpectedAlbumUnits,priorSingleNet,priorAlbumNet,projectedSingleNet,projectedAlbumNet,chosenFormat");
		albumProjectWriter.WriteLine("projectId,creationSequence,originalLabelId,currentLabelId,tierAtSchedule,genre,careerStateAtSchedule,scheduledWeek,dropWeek,strategy,albumRecordId,promoSingleId,promoPeakAtDrop,promoPeakScore,synergyAwarenessApplied,synergyStockMultiplier,terminalState,wasTransferred,transferCount,albumRetired,promoRetired,projectRealizedNet");
		albumProjectDemandWriter.WriteLine("projectId,strategy,albumRecordId,rawDemandBeforeCannibalization,suppressedDemand,demandWeightedSuppression,initialLaunchAwareness,initialLaunchStock,linkedPromoId,demandWithActiveLinkedPromo,demandWithInactiveLinkedPromo,demandWeightedSingleHeat,demandWeightedSubstitutionPropensity,reconciledDemandWeightedSuppression");
		albumProjectWeeklyWriter.WriteLine("week,year,pipelineAlbumDrops");
		decadeAnnualRollupWriter.WriteLine("seed,year,singleUnits,singleGross,singleNet,albumUnits,albumGross,albumNet,albumToSingleGross,albumGrossOver26WeeksShare,albumGrossOver52WeeksShare,decisions,albumDecisionShare,adultDecisions,adultAlbumShare,youthDecisions,youthAlbumShare,orphanShare,promoShare,standaloneShare,meanSingleConfidence,meanAlbumConfidence,compilationAlbums,compilationTrackRefs,freshnessUse0,freshnessUse1,freshnessUse2,freshnessUse3Plus,meanFreshness,minFreshness,maxFreshness,singleMemoryMeanEma,singleMemoryN,albumMemoryMeanEma,albumMemoryN,completedMatched,completedMeanExpected,completedMeanRealized,completedSignedError,youthCompCompleted,youthCompMeanExpected,youthCompMeanRealized,youthCompSignedError,promoCompleted,promoMeanExpected,promoMeanRealized,promoSignedError,singlePearson,singlePearsonN,closedTop40Median,closedTop40N,activeSingles,activeAlbums,albumAgeMedian,albumAgeP90,albumUnitsMin,albumUnitsP25,albumUnitsMedian,albumUnitsP75,albumUnitsP90,albumUnitsMax,albumsBelowSalesFloor,albumsAtOrAboveSalesFloor,albumsBelowSalesFloorShare,albumsEverReleased,albumsRetired,albumsNeverRetiredShare");
		cityRosterWriter.WriteLine("cityId,name,mapX,mapY,parentRegionId,futureRegionId,isRegionalHub,distributionTier,difficulty,recordStoreCount,departmentStoreCount,inventoryDepth,hasOneStopDistributors,hasIndieDistribution,projection");
		distanceMatrixWriter.WriteLine("fromCityId,fromCityName,toCityId,toCityName,distance");
		labelGeographyWriter.WriteLine("labelId,labelName,headquartersCity,homeRegion,homeCityId,homeCityName,assignmentSource,nodeSet");
		geographyMetricsWriter.WriteLine("week,year,regionId,destinationTier,labelTier,genre,recordCount,totalUnits,chartedUnits,backorders,homeRegionUnits,nonNationalUnits,nonNationalBackorders");
		dealMetricsWriter.WriteLine("offersGenerated,offersAccepted,acceptanceRate,signedDeals");
		genreCatalogWriter.WriteLine("id,genre,family,emergenceYear,deathYear,baseline1960,baseline1962,baseline1964,baseline1966,baseline1967,baseline1968,baseline1969,audienceLean,singleOrientation,segmentWeights,status");
		genreMarketWeeklyWriter.WriteLine("seed,enabled,year,month,week,region,segment,genre,baseline,lifecycleState,zeitgeistFactor,regionalFactor,segmentReach,preShock,decay,positiveImpulse,adjacentImpulse,donorPressure,postShock,emergenceAdvanceWeeks,effectiveAcceptance,eligibleRecords,chartedRecords,units,radioPlay");
		recordGenreExplanationWriter.WriteLine("seed,enabled,year,month,week,recordId,region,primaryGenreId,secondaryGenreId,primaryWeight,secondaryWeight,tags,segmentBlend,formatTilt,genericSeasonality,recordSeasonalFactor,radioFactor,finalAcceptance,finalDemandSeam,legacyAcceptanceComparator,legacySingleDemandMultiplier,enabledSingleDemandMultiplier,singleDemandTransferRatio,chartVisibilityMultiplier,radioSalesMultiplier,sentimentMultiplier,awardMultiplier,distributionMultiplier,conversionSeasonalityMultiplier,catalogBaselineAcceptance,regionalAdjustedAcceptance,segmentRoutedAcceptance,primaryWeightedRoutedAcceptance,secondaryBlendAcceptanceContribution,legacyMomentum,legacyMomentumAcceptanceContribution,acceptanceClampDelta,salesRecordAwareness,salesRegionalAwareness,salesEffectiveAwareness,salesRadioHeat,salesRegionalRadioPlay");
		albumDemandExplanationWriter.WriteLine("seed,enabled,year,month,week,recordId,region,genre,routedAcceptance,legacyAcceptance,segregationFactor,albumAffinity,purchaseWillingness,enabledPreTiltBuyerPool,acceptedPreTiltBuyerPool,opportunityNormalization,actualPreTiltBuyerPool,formatTilt,finalAlbumOpportunity");
		formatDecisionExplanationWriter.WriteLine("week,year,recordId,labelId,artistId,genre,rawSecondaryGenre,careerState,careerBand,chosenFormat,singlePreTiltContribution,albumPreTiltContribution,albumAffinity,activeAlbumOpportunity,singleFormatTilt,albumFormatTilt,singleProductionCost,albumProductionCost,singleMemoryEma,albumMemoryEma,confidenceSingle,confidenceAlbum,singleMemoryBlend,albumMemoryBlend,singleNoise,albumNoise,finalSingleMargin,finalAlbumMargin,albumChoiceProbability,formatChoiceRoll,memoryScope,memoryScopeGenre");
		formatDecisionCohortWriter.WriteLine("year,genre,format,decisions,realizedUnits,realizedUnitsPerDecision");
		formatDecisionCohortDetailWriter.WriteLine("year,recordId,rawPrimaryGenre,rawSecondaryGenre,format,realizedUnits");
			supplySelectionWriter.WriteLine("week,year,labelId,artistId,artistIdentity,chosenProjectGenre,artistIdentityAvailableForNewSupply,annualFloorRequested,annualFloorReroutedToNormalCandidates,selectionMode");
			traditionalPopFallbackWriter.WriteLine("week,year,source,requestedGenre");
		// marketUnitsShare is whole-market commercial weight; chartWeekShare is chart presence;
		// chartWeekShareMinusMarketShare is the divergence between them.
		genreShapeWriter.WriteLine("seed,year,genre,family,emergenceYear,deathYear,baseline,lifecycleState,newReleases,activeRecordsYearEnd,marketUnits,marketUnitsShare,chartRecordWeeks,chartWeekShare,uniqueChartingRecords,chartUnits,chartUnitsShare,top40RecordWeeks,top10RecordWeeks,numberOneWeeks,meanChartPosition,chartWeekShareMinusMarketShare");
		genreEventsWriter.WriteLine("seed,enabled,year,month,week,eventType,sourceRecordId,recipientGenreId,donorGenreId,field,amount,detail");
		specialProductsWriter.WriteLine("seed,enabled,year,recordId,subtype,externalProfile,correlatedProfileBucket,costs,promotion,tieIn,units,chartResult,catalogTail,financialReconciliation");
		rosterLifecycleWriter?.WriteLine("week,year,labelTier,rosterSize,emptyRosterLabels,releaseEligibleArtists,dropsToFreeAgentPool,firstTimeSignings,reSignings,uniqueReSignings,shortWindowRedrops26Weeks,scoutingGatePasses,signingAttempts,candidateRejections,affordabilityRejections,freeAgentPoolSize,terminalArtistsStillRostered,ownershipConflicts,duplicatePoolEntries,releaseAttempts,successfulReleases,artistSelectionFailures");
		labelScoutingVacancyWriter?.WriteLine("week,year,labelId,labelTier,isActiveLabel,maxRosterSize,operatingRosterTarget,operatingRosterTargetSource,labelOrigin,runtimeBirthWeek,runtimeBirthDate,operatingTargetReason,organicGrowthCount,lastOrganicGrowthWeek,lastOrganicGrowthBlockingReason,rosterSize,unusedRosterSlots,unusedOperatingRosterSlots,isEmptyRoster,consecutiveVacancyWeeks,consecutiveEmptyWeeks,scoutingAbility,rosterFullness,hasRecentHit,recentHitFactor,decliningArtistCount,decliningFactor,estimatedAdvance,canAffordEstimatedAdvance,computedScoutProbability,scoutRandomRoll,scoutingGatePassed,eligibleCandidateCount,discoveryPoolCount,bestCandidateScore,neverSignedSlateCount,qualifyingNeverSignedCount,bestNeverSignedScore,thirdPlusPerformanceComebackCount,overallBestContractSequence,freshPreferenceApplied,repeatComebackDeferred,freshPreferenceFallbackReason,signingAttempted,signingSucceeded,signingKind,failureReason,scoutingRosterSize,scoutingUnusedRosterSlots,scoutingUnusedOperatingRosterSlots,scoutingIsEmptyRoster,releaseEligibleArtistCount,requiredReleaseLanes,headcountDeficit,releaseLaneDeficit,serviceDeficit,serviceDeficitAge,serviceMode,scoutingGateBypassed,freshLaneCount,experiencedLaneCount,freshDiscoveryScope,bestFreshPotentialScore,bestExperiencedProductionScore,selectedLane,recoveryThresholdFallbackUsed,recoveryFailureReason");
		labelOperatingTargetEventWriter?.WriteLine("week,date,labelId,labelOrigin,birthWeek,birthDate,reason,priorTarget,newTarget,hardCapacity,organicGrowthCount,weeksSincePriorOrganicIncrease,eligibilityResult,blockingReason,status,tier,rosterSize,releaseEligibleCount,recentChartingCount,recentReleaseCount,lastMonthlyProfit,consecutiveLossMonths,cashReserves,monthlyOverhead,runwayMonths");
		runtimeLabelProfileWriter?.WriteLine("seed,birthWeek,birthDate,labelId,labelName,birthTier,archetype,headquartersCity,homeRegion,homeCityId,homeCityAssignmentSource,preferredGenres,secondaryGenres,budgetLevel,scoutingAbility,productionQuality,marketingPower,ownedReach,nationalReach,riskTolerance,artistLoyalty,payolaWillingness,releasesPerMonth,cashReserves,reputation,marketShare,debtLevel,foundedYear,monthsActive,totalReleases,top40Hits,numberOneHits,maxRosterSize,operatingRosterTarget,profileVersion");
		dailyTalentMarketWriter?.WriteLine("date,chartWeek,eligibleVacancies,dueLabels,supplySnapshotCount,freshSupplySnapshotCount,experiencedSupplySnapshotCount,nominations,uniqueNominatedArtists,collisionArtists,collisionOffers,acceptedOffers,collisionLosers,invalidatedBeforeCommit");
		dailyTalentAppointmentWriter?.WriteLine("date,chartWeek,labelId,labelOrigin,labelTier,vacancyGeneration,vacancyOpenedDate,scheduledScoutingDate,actualScoutingDate,appointmentOrdinal,serviceMode,freshLaneCount,experiencedLaneCount,selectedArtistId,selectedLane,offerOutcome,collisionOfferCount,winnerLabelId,artistChoiceUtility,genreUtility,localityUtility,royaltyUtility,advanceUtility,reputationUtility,reachUtility,rosterOpportunityUtility,affinityUtility,nextScoutingDate");
		catastrophicFailFastWriter?.WriteLine("gate,metric,enabledValue,controlValue,completedYear,week,date,state");
		artistPopulationEventsWriter?.WriteLine("seed,week,date,eventType,artistId,artistType,cohort,formedYear,formationPrimaryGenre,formationSecondaryGenre,currentPrimaryGenre,homeRegion,lifecycleStatus,careerState,prospectMarketStatus,prospectMarketStatusBeforeContract,careerStateBeforeDrop,contractEntryCareerState,labelId,labelTier,dropReason,performanceDropCount,requiredPerformanceCooldownWeeks,contractSequence,priorContractCount,contractStartWeek,contractTop40Hits,contractConsecutiveFlops,contractCompletedChartRuns,performanceEvaluationMode,requiredPerformanceCompletedRuns,requiredPerformanceConsecutiveFlops,contractProbationPending,weeksSincePerformanceDrop,weeksContinuouslyUnowned,artistAge,leadMemberAge");
		artistPopulationWeeklyWriter?.WriteLine("week,year,labelTier,registryTotal,activeTotal,rostered,neverSignedUnsigned,eligibleDropped,cooldownBlockedDropped,inactive,retired,disbanded,formedThisWeek,formedYtd,firstTimeSignings,reSignings,performanceDrops,otherDepartures,recentPerformanceReSignings,prematureProbationDrops,noEligibleCandidatePasses,scoreRejections,affordabilityRejections,ownershipConflicts,duplicateRosterEntries,duplicatePoolEntries,terminalRostered,terminalReleaseEligible");
		artistLaborMarketWeeklyWriter?.WriteLine("seed,week,date,registryPopulation,initialLegacyPopulation,enabledInitialReservePopulation,runtimeFormationPopulation,activeRostered,experiencedFreeAgents,seekingProspects,latentProspects,freshSeeking,freshLatent,affordableHiringVacancies,requestedProspectActivations,actualProspectActivations,prospectSearchSpellExpirations,firstTimeSignings,repeatSignings,meanSeekingQuality,meanLatentQuality,activationMeanQuality,activationQ1,activationQ2,activationQ3,activationQ4,maxProspectMarketSpellCount,duplicateSeekingEntries,latentUnsignedPoolEntries,seekingMissingFromUnsignedPool,prospectStatusContractConflicts,latentRotations");
		artistCohortAnnualWriter?.WriteLine("year,cohort,formationPrimaryGenre,lifecycleStatus,currentRosterTier,count,firstTimeSignings,repeatSignings,releases,activeUnsigned,seekingProspects,latentProspects,medianActAge,medianMemberAge,inactivityCount,retirementCount,disbandmentCount,activePopulationShare,signedRosterShare");
		artistProjectIdentityWriter?.WriteLine("week,year,recordId,projectId,artistId,formedYear,cohort,formationPrimaryGenre,currentArtistGenre,projectGenre,nativeIdentityProject,transitionedProject,labelId,labelTier,format,careerStateAtProject,careerStateBeforeDropAtProject,contractEntryCareerStateAtProject,contractSequenceAtProject,contractStartWeekAtProject,weeksSinceContractStart,experiencedFreeAgentContract");
		WriteGenreCatalogRows();
		// bookSettlementSeconds is inclusive of calculateLabelRevenueSeconds. The live-record
		// inertness columns answer handoff 35.4: whether the album pile-up in the hot loop is
		// economically live catalog or stock-less, awareness-less residue.
		performanceProfileWriter?.WriteLine("seed,year,wallSeconds,activeRecords,simulateWeekSeconds,calculateLabelRevenueSeconds,recordLookupSeconds,revenueArithmeticSeconds,albumUpdateSeconds,processDueAlbumProjectsSeconds,captureWeekSeconds,recordLookups,freezeSettlementSeconds,bookSettlementSeconds,settlementAuditEventSeconds,genreMomentumSeconds,cullDeadRecordsSeconds,populationLifecycleSeconds,competitorWeekSeconds,rosterWeekSeconds,dailyTalentMarketSeconds,labelLifecycleMonthSeconds,activeAlbums,activeSingles,albumsOffChart,albumsZeroStock,albumsZeroAwareness,albumsZeroUnitsThisWeek,inertAlbums,inertSingles");
		foreach (AILabel label in CompetitorManager.Instance.GetAllLabels().OrderBy(label => label.labelId, StringComparer.Ordinal)) {
			birthTierByLabel[label.labelId] = label.tier;
			labelDirectoryWriter.WriteLine(string.Join(",", new[] { Csv(label.labelId), Csv(label.labelName), Csv(label.archetype.ToString()),
				label.isHistorical ? "true" : "false", Csv(label.tier.ToString()) }));
		}
		WriteIndependentDistributorRows();
	}

	// The independent-distribution layer is emitted alongside its authored regional inputs
	// so house counts and capacity can be read against the retail infrastructure they derive
	// from. The major client ceiling saturated unnoticed for a decade (handoff section 32.2);
	// this layer's occupancy must be inspectable from the first run that has one.
	private void WriteIndependentDistributorRows() {
		if (independentDistributorWriter == null) return;
		var regionsById = (ChartManager.Instance?.GetAllRegions() ?? new List<MarketRegion>())
			.Where(region => !string.IsNullOrEmpty(region?.regionId))
			.GroupBy(region => region.regionId, StringComparer.Ordinal)
			.ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
		foreach (IndependentDistributor house in
			CompetitorManager.Instance.GetIndependentDistributors().OrderBy(house => house.distributorId, StringComparer.Ordinal)) {
			regionsById.TryGetValue(house.regionId ?? string.Empty, out MarketRegion region);
			DistributionNetwork network = region?.distribution;
			independentDistributorWriter.WriteLine(string.Join(",", new[] {
				Csv(house.distributorId), Csv(house.distributorName), Csv(house.regionId),
				F(region?.population ?? 0f),
				(network?.recordStoreCount ?? 0).ToString(CultureInfo.InvariantCulture),
				(network?.departmentStoreCount ?? 0).ToString(CultureInfo.InvariantCulture),
				F(network?.inventoryDepth ?? 0f), F(network?.difficulty ?? 0f),
				house.clientCapacity.ToString(CultureInfo.InvariantCulture),
				F(house.reliability), house.paymentTermWeeks.ToString(CultureInfo.InvariantCulture),
				F(house.returnAllowance), F(house.reportingHonesty)
			}));
		}
	}

	private void WriteDistanceSubstrateRows() {
		foreach (MarketCity city in DistanceModel.GetCities().OrderBy(city => city.cityId, StringComparer.Ordinal)) {
			DistributionNetwork network = city.distribution;
			cityRosterWriter.WriteLine(string.Join(",", new[] {
				Csv(city.cityId), Csv(city.name), F(city.mapCoords.X), F(city.mapCoords.Y), Csv(city.parentRegionId),
				string.Empty, city.isRegionalHub ? "true" : "false",
				city.distributionTier.ToString(CultureInfo.InvariantCulture), F(network.difficulty),
				network.recordStoreCount.ToString(CultureInfo.InvariantCulture),
				network.departmentStoreCount.ToString(CultureInfo.InvariantCulture), F(network.inventoryDepth),
				network.hasOneStopDistributors ? "true" : "false", network.hasIndieDistribution ? "true" : "false",
				Csv(DistanceModel.ProjectionDescription)
			}));
		}

		foreach (var row in DistanceModel.GetDistanceMatrixRows()) {
			distanceMatrixWriter.WriteLine(string.Join(",", new[] {
				Csv(row.From.cityId), Csv(row.From.name), Csv(row.To.cityId), Csv(row.To.name), F(row.Distance)
			}));
		}

		foreach (AILabel label in CompetitorManager.Instance.GetAllLabels().OrderBy(label => label.labelId, StringComparer.Ordinal)) {
			var resolved = DistanceModel.ResolveHomeCity(label);
			string[] nodeNames = DistanceModel.GetDistributionNodeNames(label);
			labelGeographyWriter.WriteLine(string.Join(",", new[] {
				Csv(label.labelId), Csv(label.labelName), Csv(label.headquartersCity), Csv(label.homeRegion),
				Csv(label.homeCityId), Csv(resolved.City?.name), Csv(label.homeCityAssignmentSource),
				Csv(string.Join("|", nodeNames))
			}));
		}
	}

	private void WriteGenreCatalogRows() {
		foreach (GenreProfile profile in GenreCatalog.All.OrderBy(profile => profile.Id, StringComparer.Ordinal)) {
			string weights = string.Join(";", profile.SegmentWeights.OrderBy(pair => pair.Key, StringComparer.Ordinal)
				.Select(pair => $"{pair.Key}:{pair.Value.ToString("0.######", CultureInfo.InvariantCulture)}"));
			genreCatalogWriter.WriteLine(string.Join(",", new[] {
				Csv(profile.Id), Csv(profile.Genre.ToString()), Csv(profile.Family.ToString()),
				F(profile.EmergenceYear), profile.DeathYear.HasValue ? F(profile.DeathYear.Value) : string.Empty,
				F(profile.BaselineKeyframes[0]), F(profile.BaselineKeyframes[1]), F(profile.BaselineKeyframes[2]),
				F(profile.BaselineKeyframes[3]), F(profile.BaselineKeyframes[4]), F(profile.BaselineKeyframes[5]), F(profile.BaselineKeyframes[6]),
				F(profile.AudienceLean), F(profile.SingleOrientation), Csv(weights), "phase2-segment-routing"
			}));
		}
	}

	/// <summary>
	/// Phase-2 telemetry is observational only: it reads the resolved weekly state
	/// after sales and never draws RNG or mutates demand. The aggregate rows use an
	/// explicit all-segment rollup so annual genre units can be summed without
	/// double-counting a record across overlapping reception channels.
	/// </summary>
	private void WriteGenreMarketRows(int week, GameDate date, List<RecordRuntimeData> records) {
		if (genreMarketWeeklyWriter == null || !GenreMarketV2.Enabled) return;
		float continuousYear = GetContinuousYear(date);
		var byGenre = records.GroupBy(record => GenreCatalog.MapLegacy(record.baseRecord.primaryGenre, date.year))
			.ToDictionary(group => group.Key, group => group.ToArray());
		foreach (MarketRegion region in regions.OrderBy(region => region.regionId, StringComparer.Ordinal)) {
			foreach (GenreProfile profile in GenreCatalog.All.OrderBy(profile => profile.Id, StringComparer.Ordinal)) {
				RecordRuntimeData[] matching = byGenre.TryGetValue(profile.Genre, out RecordRuntimeData[] grouped)
					? grouped : Array.Empty<RecordRuntimeData>();
				int eligible = matching.Length;
				int charted = matching.Count(record => record.currentPosition > 0);
				long units = 0;
				float radioPlay = 0f;
				foreach (RecordRuntimeData record in matching) {
					if (!record.regionalData.TryGetValue(region.regionId, out RegionalRecordData data)) continue;
					units += data.unitsSoldThisWeek;
					radioPlay += data.radioPlay;
				}
				float momentum = ChartManager.Instance.GetGenreMomentum(profile.Genre);
				float effective = GenreAcceptanceService.GetRegionalDemandAcceptance(profile.Genre, profile.Genre, region, continuousYear, momentum);
				float regionalFactor = GetAggregateRegionalFactor(profile.Genre, region, continuousYear, momentum);
				genreMarketWeeklyWriter.WriteLine(string.Join(",", new[] {
					requestedSeed?.ToString(CultureInfo.InvariantCulture) ?? string.Empty, "true",
					date.year.ToString(CultureInfo.InvariantCulture), date.month.ToString(CultureInfo.InvariantCulture), week.ToString(CultureInfo.InvariantCulture),
					Csv(region.regionId), Csv("AllSegments"), Csv(profile.Id), F(profile.GetBaseline(continuousYear)),
					Csv(profile.GetLifecycle(continuousYear).ToString()), F(1f), F(regionalFactor), F(1f),
					F(momentum), F(0f), F(0f), F(0f), F(0f), F(momentum), "0", F(effective),
					eligible.ToString(CultureInfo.InvariantCulture), charted.ToString(CultureInfo.InvariantCulture), units.ToString(CultureInfo.InvariantCulture), F(radioPlay)
				}));
			}
		}
	}

	private void WriteRecordGenreExplanationRows(int week, GameDate date, List<RecordRuntimeData> records) {
		if (recordGenreExplanationWriter == null || !GenreMarketV2.Enabled) return;
		float continuousYear = GetContinuousYear(date);
		// Top-40 records plus a stable 1-in-16 launch sample are a bounded,
		// deterministic audit population. They cover mature radio behavior and the
		// launch demand seam without turning a decade run into a multi-gigabyte log.
		foreach (RecordRuntimeData record in records.Where(record =>
			record.currentPosition is > 0 and <= 40 ||
			(record.weeksSinceRelease <= 3 && IsGenreExplanationLaunchSample(record.baseRecord.recordId)))
			.OrderBy(record => record.baseRecord.recordId, StringComparer.Ordinal)) {
			Record baseRecord = record.baseRecord;
			float secondaryWeight = baseRecord.primaryGenre == baseRecord.secondaryGenre ? 0f : .20f;
			float primaryWeight = 1f - secondaryWeight;
			float genericSeasonality = baseRecord.format == ReleaseFormat.Album
				? MarketSeasonality.GetAlbumSalesMultiplier(date.year, date.month, liveTick: true)
				: MarketSeasonality.GetSingleSalesMultiplier(date.year, date.month, liveTick: true);
			float momentum = ChartManager.Instance.GetGenreMomentum(baseRecord.primaryGenre);
			foreach (MarketRegion region in regions.OrderBy(region => region.regionId, StringComparer.Ordinal)) {
				if (!record.regionalData.TryGetValue(region.regionId, out RegionalRecordData data)) continue;
				float formatTilt = GenreAcceptanceService.GetFormatMultiplier(baseRecord.primaryGenre,
					baseRecord.secondaryGenre, baseRecord.format, continuousYear, region.GetAlbumDemandEraProgress(continuousYear));
				float acceptance = baseRecord.format == ReleaseFormat.Single && data.genreMarketAcceptanceWeek == week
					? data.genreDemandAcceptanceThisWeek
					: GenreAcceptanceService.GetRegionalDemandAcceptance(baseRecord.primaryGenre, baseRecord.secondaryGenre, region, continuousYear, momentum);
				float radioFactor = baseRecord.format == ReleaseFormat.Single && data.genreMarketAcceptanceWeek == week
					? data.genreRadioOpportunityThisWeek
					: GenreAcceptanceService.GetRegionalRadioOpportunity(baseRecord.primaryGenre, baseRecord.secondaryGenre, region, continuousYear, momentum);
				float demandSeam = baseRecord.format == ReleaseFormat.Album
					? acceptance * region.GetSegregationFactor(baseRecord.primaryGenre) * formatTilt
					: GenreAcceptanceService.GetEnabledSingleDemandMultiplier(acceptance) * formatTilt;
				float legacyAcceptance = region.GetLegacyGenreAcceptance(baseRecord.primaryGenre, continuousYear) * primaryWeight;
				if (secondaryWeight > 0f) legacyAcceptance += region.GetLegacyGenreAcceptance(baseRecord.secondaryGenre, continuousYear) * secondaryWeight;
				float legacySingleDemandMultiplier = .6f + legacyAcceptance * .5f;
				float enabledSingleDemandMultiplier = GenreAcceptanceService.GetEnabledSingleDemandMultiplier(acceptance);
				RegionalDemandAcceptanceComponents components = GenreAcceptanceService.GetRegionalDemandAcceptanceComponents(
					baseRecord.primaryGenre, baseRecord.secondaryGenre, region, continuousYear, momentum);
				float chartVisibility = data.breakoutVisibilityMultiplier;
				float radioSalesMultiplier = .75f + record.radioHeat * .5f;
				float sentimentMultiplier = .75f + Mathf.Max(0f, data.sentiment) * .25f;
				float awardMultiplier = record.GetAwardMultiplier();
				float distributionMultiplier = 1f - region.distribution.difficulty * .3f;
				recordGenreExplanationWriter.WriteLine(string.Join(",", new[] {
					requestedSeed?.ToString(CultureInfo.InvariantCulture) ?? string.Empty, "true",
					date.year.ToString(CultureInfo.InvariantCulture), date.month.ToString(CultureInfo.InvariantCulture), week.ToString(CultureInfo.InvariantCulture),
					Csv(baseRecord.recordId), Csv(region.regionId), Csv(GenreCatalog.Get(GenreCatalog.MapLegacy(baseRecord.primaryGenre, date.year)).Id),
					Csv(secondaryWeight > 0f ? GenreCatalog.Get(GenreCatalog.MapLegacy(baseRecord.secondaryGenre, date.year)).Id : string.Empty),
					F(primaryWeight), F(secondaryWeight), Csv(string.Join("|", baseRecord.genreTagIds ?? Array.Empty<string>())),
					Csv(GetSegmentBlend(baseRecord.primaryGenre, baseRecord.secondaryGenre, region, continuousYear, momentum)), F(formatTilt), F(genericSeasonality), F(1f), F(radioFactor), F(acceptance), F(demandSeam),
					F(legacyAcceptance), F(legacySingleDemandMultiplier), F(enabledSingleDemandMultiplier), F(enabledSingleDemandMultiplier / Mathf.Max(.000001f, legacySingleDemandMultiplier)),
					F(chartVisibility), F(radioSalesMultiplier), F(sentimentMultiplier), F(awardMultiplier), F(distributionMultiplier), F(genericSeasonality),
					F(components.CatalogBaseline), F(components.RegionalAdjusted), F(components.SegmentRouted), F(components.PrimaryWeightedRouted),
					F(components.SecondaryBlendContribution), F(components.LegacyMomentum), F(components.LegacyMomentumAcceptanceContribution), F(components.ClampDelta),
					F(data.salesRecordAwarenessThisWeek), F(data.salesRegionalAwarenessThisWeek), F(data.salesEffectiveAwarenessThisWeek),
					F(data.salesRadioHeatThisWeek), F(data.salesRegionalRadioPlayThisWeek)
				}));
			}
		}
	}

	private void WriteAlbumDemandExplanationRows(int week, GameDate date, List<RecordRuntimeData> records) {
		if (albumDemandExplanationWriter == null || !GenreMarketV2.Enabled) return;
		float continuousYear = GetContinuousYear(date);
		foreach (RecordRuntimeData record in records.Where(record => record.baseRecord.format == ReleaseFormat.Album &&
			(record.currentPosition is > 0 and <= 40 ||
			(record.weeksSinceRelease <= 3 && IsGenreExplanationLaunchSample(record.baseRecord.recordId)))
			).OrderBy(record => record.baseRecord.recordId, StringComparer.Ordinal)) {
			foreach (MarketRegion region in regions.OrderBy(region => region.regionId, StringComparer.Ordinal)) {
				if (!record.regionalData.ContainsKey(region.regionId)) continue;
				MarketRegion.AlbumDemandExplanation explanation = region.GetAlbumDemandExplanation(record.baseRecord.primaryGenre, continuousYear);
				float actualPreTilt = region.GetAlbumMarketSize(record.baseRecord.primaryGenre, date.year);
				float formatTilt = GenreAcceptanceService.GetFormatMultiplier(record.baseRecord.primaryGenre,
					record.baseRecord.secondaryGenre, ReleaseFormat.Album, continuousYear,
					region.GetAlbumOpportunityWeight(record.baseRecord.primaryGenre, continuousYear,
						GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true));
				albumDemandExplanationWriter.WriteLine(string.Join(",", new[] {
					requestedSeed?.ToString(CultureInfo.InvariantCulture) ?? string.Empty, "true",
					date.year.ToString(CultureInfo.InvariantCulture), date.month.ToString(CultureInfo.InvariantCulture), week.ToString(CultureInfo.InvariantCulture),
					Csv(record.baseRecord.recordId), Csv(region.regionId), Csv(record.baseRecord.primaryGenre.ToString()),
					F(explanation.RoutedAcceptance), F(explanation.LegacyAcceptance), F(explanation.SegregationFactor),
					F(explanation.AlbumAffinity), F(explanation.PurchaseWillingness), F(explanation.EnabledPreTiltBuyerPool),
					F(explanation.AcceptedPreTiltBuyerPool), F(explanation.OpportunityNormalization), F(actualPreTilt),
					F(formatTilt), F(actualPreTilt * formatTilt)
				}));
			}
		}
	}

	private static float GetAggregateRegionalFactor(Genre genre, MarketRegion region, float year, float momentum) {
		float weightedFactor = 0f;
		float totalCapacity = 0f;
		foreach (AudienceSegment segment in SegmentCapacityModel.All) {
			float capacity = region.segmentCapacities?.Shares.TryGetValue(segment, out float share) == true ? share : 0f;
			weightedFactor += capacity * GenreAcceptanceService.Evaluate(genre, region, segment, year, momentum).RegionalFactor;
			totalCapacity += capacity;
		}
		return totalCapacity > 0f ? weightedFactor / totalCapacity : 1f;
	}

	private static string GetSegmentBlend(Genre primary, Genre secondary, MarketRegion region, float year, float momentum) {
		float secondaryWeight = primary == secondary ? 0f : .20f;
		float primaryWeight = 1f - secondaryWeight;
		var contributions = new List<(AudienceSegment Segment, float Value)>();
		float total = 0f;
		foreach (AudienceSegment segment in SegmentCapacityModel.All) {
			float capacity = region.segmentCapacities?.Shares.TryGetValue(segment, out float share) == true ? share : 0f;
			float acceptance = GenreAcceptanceService.Evaluate(primary, region, segment, year, momentum).Effective * primaryWeight;
			if (secondaryWeight > 0f) acceptance += GenreAcceptanceService.Evaluate(secondary, region, segment, year, momentum).Effective * secondaryWeight;
			float contribution = capacity * acceptance;
			contributions.Add((segment, contribution));
			total += contribution;
		}
		return string.Join("|", contributions.Select(item => $"{item.Segment}:{F(total > 0f ? item.Value / total : 0f)}"));
	}

	private static float GetContinuousYear(GameDate date) {
		int daysInYear = DateTime.IsLeapYear(date.year) ? 366 : 365;
		int dayOfYear = new DateTime(date.year, date.month, date.day).DayOfYear;
		return date.year + (dayOfYear - 1f) / daysInYear;
	}

	private static bool IsGenreExplanationLaunchSample(string recordId) {
		uint hash = 2166136261;
		foreach (char character in recordId ?? string.Empty) {
			hash ^= character;
			hash *= 16777619;
		}
		return hash % 16u == 0u;
	}

	private void WriteGeographyMetricRows(int week, int year, List<RecordRuntimeData> records) {
		var rollups = new Dictionary<(string RegionId, int DestinationTier, string LabelTier, string Genre), GeographyRollup>();
		foreach (RecordRuntimeData record in records) {
			AILabel label = ChartManager.Instance.GetLabelById(record.baseRecord.labelId);
			if (label == null || record.regionalData == null) continue;
			string labelTier = label.tier.ToString();
			string genre = record.baseRecord.primaryGenre.ToString();
			bool nonNational = label.nationalReach < 0.50f;
			foreach (var pair in record.regionalData) {
				RegionalRecordData data = pair.Value;
				if (data == null) continue;
				int destinationTier = DistanceModel.GetHubCityForRegion(pair.Key)?.distributionTier ?? 0;
				var key = (pair.Key, destinationTier, labelTier, genre);
				if (!rollups.TryGetValue(key, out GeographyRollup row)) {
					row = new GeographyRollup();
					rollups[key] = row;
				}
				row.RecordCount++;
				row.TotalUnits += data.unitsSoldThisWeek;
				if (record.currentPosition > 0) row.ChartedUnits += data.unitsSoldThisWeek;
				row.Backorders += data.unitsBackordered;
				if (pair.Key == label.homeRegion) row.HomeRegionUnits += data.unitsSoldThisWeek;
				if (nonNational) {
					row.NonNationalUnits += data.unitsSoldThisWeek;
					row.NonNationalBackorders += data.unitsBackordered;
				}
			}
		}

		foreach (var pair in rollups.OrderBy(pair => pair.Key.RegionId, StringComparer.Ordinal)
			.ThenBy(pair => pair.Key.DestinationTier).ThenBy(pair => pair.Key.LabelTier, StringComparer.Ordinal)
			.ThenBy(pair => pair.Key.Genre, StringComparer.Ordinal)) {
			GeographyRollup row = pair.Value;
			geographyMetricsWriter.WriteLine(string.Join(",", new[] {
				week.ToString(CultureInfo.InvariantCulture), year.ToString(CultureInfo.InvariantCulture), Csv(pair.Key.RegionId),
				pair.Key.DestinationTier.ToString(CultureInfo.InvariantCulture), Csv(pair.Key.LabelTier), Csv(pair.Key.Genre),
				row.RecordCount.ToString(CultureInfo.InvariantCulture), row.TotalUnits.ToString(CultureInfo.InvariantCulture),
				row.ChartedUnits.ToString(CultureInfo.InvariantCulture), row.Backorders.ToString(CultureInfo.InvariantCulture),
				row.HomeRegionUnits.ToString(CultureInfo.InvariantCulture), row.NonNationalUnits.ToString(CultureInfo.InvariantCulture),
				row.NonNationalBackorders.ToString(CultureInfo.InvariantCulture)
			}));
		}
	}

	private void WriteDealMetrics() {
		int generated = CompetitorManager.Instance?.DistributionOffersGenerated ?? 0;
		int accepted = CompetitorManager.Instance?.DistributionOffersAccepted ?? 0;
		dealMetricsWriter.WriteLine(string.Join(",", new[] {
			generated.ToString(CultureInfo.InvariantCulture), accepted.ToString(CultureInfo.InvariantCulture),
			generated > 0 ? F((double)accepted / generated) : string.Empty,
			signedDealEvents.ToString(CultureInfo.InvariantCulture)
		}));
	}

	private void OnDistributionDealEvent(DistributionDealTelemetry dealEvent) {
		int year = TimeManager.Instance?.CurrentDate.year ?? 1960;
		if (dealEvent.resolution == DealResolution.Signed) {
			signedDealEvents++;
			if (!string.IsNullOrEmpty(dealEvent.clientId))
				signedDealCountByLabel[dealEvent.clientId] = signedDealCountByLabel.GetValueOrDefault(dealEvent.clientId) + 1;
		} else if (dealEvent.resolution is DealResolution.Exit or DealResolution.Renew or DealResolution.Absorb
			or DealResolution.Poached or DealResolution.Graduated or DealResolution.Dropped) {
			if (!string.IsNullOrEmpty(dealEvent.clientId))
				completedDealCountByLabel[dealEvent.clientId] = completedDealCountByLabel.GetValueOrDefault(dealEvent.clientId) + 1;
		}
		dealLedgerWriter.WriteLine(string.Join(",", new[] {
			currentAuditWeek.ToString(CultureInfo.InvariantCulture), year.ToString(CultureInfo.InvariantCulture), Csv(dealEvent.resolution.ToString()),
			Csv(dealEvent.origin.ToString()), Csv(dealEvent.distributorId), Csv(dealEvent.distributorName), Csv(dealEvent.clientId), Csv(dealEvent.clientName),
			F(dealEvent.reachGranted), F(dealEvent.marginSkim), dealEvent.ownsMasters ? "true" : "false", F(dealEvent.advance),
			dealEvent.signedWeek.ToString(CultureInfo.InvariantCulture), dealEvent.termWeeks.ToString(CultureInfo.InvariantCulture), F(dealEvent.dependency)
		}));
		if (dealEvent.resolution == DealResolution.Absorb && !string.IsNullOrEmpty(dealEvent.clientId) && !string.IsNullOrEmpty(dealEvent.distributorId)) {
			acquiredBy[dealEvent.clientId] = dealEvent.distributorId;
		}
	}

	private void OnIndependentTradeFailure(IndependentTradeFailureTelemetry failure) {
		if (independentTradeFailureWriter == null || failure == null) return;
		independentTradeFailureWriter.WriteLine(string.Join(",", new[] {
			failure.year.ToString(CultureInfo.InvariantCulture), Csv(failure.distributorId), Csv(failure.distributorName),
			Csv(failure.regionId), failure.clientsDropped.ToString(CultureInfo.InvariantCulture),
			F(failure.reachLostPerClient), failure.housesRemaining.ToString(CultureInfo.InvariantCulture), F(failure.survivalRate)
		}));
	}

	private void OnIndependentDistributionSigned(IndependentDistributionTelemetry signing) {
		if (independentDistributionEventWriter == null || signing == null) return;
		int year = TimeManager.Instance?.CurrentDate.year ?? 0;
		independentDistributionEventWriter.WriteLine(string.Join(",", new[] {
			signing.week.ToString(CultureInfo.InvariantCulture), year.ToString(CultureInfo.InvariantCulture),
			Csv(signing.labelId), Csv(signing.labelName), Csv(signing.labelTier.ToString()),
			Csv(signing.distributorId), Csv(signing.distributorName), Csv(signing.regionId),
			signing.provenInRegion ? "true" : "false",
			signing.coveredRegionCount.ToString(CultureInfo.InvariantCulture),
			F(signing.coveredMarketShare), F(signing.ownedReachBefore), F(signing.ownedReachAfter),
			signing.houseClientCount.ToString(CultureInfo.InvariantCulture),
			signing.houseClientCapacity.ToString(CultureInfo.InvariantCulture)
		}));
	}

	private void OnDistributionOfferAttempt(DistributionOfferAttemptTelemetry attempt) {
		if (distributionOfferAttemptWriter == null || attempt == null) return;
		distributionOfferAttemptWriter.WriteLine(string.Join(",", new[] {
			attempt.week.ToString(CultureInfo.InvariantCulture),
			attempt.year.ToString(CultureInfo.InvariantCulture),
			Csv(attempt.clientId),
			Csv(attempt.clientName),
			Csv(attempt.clientTier.ToString()),
			Csv(attempt.clientOrigin.ToString()),
			attempt.monthsActive.ToString(CultureInfo.InvariantCulture),
			F(attempt.ownedReach),
			F(attempt.nationalReach),
			F(attempt.bestAnyRegionPeak),
			F(attempt.bestStrongRegionPeak),
			F(attempt.bestPersistentEvidenceQuality),
			attempt.persistentRegionalEvidence ? "true" : "false",
			attempt.legacyQualityAndCurrentSalesEvidence ? "true" : "false",
			attempt.legacyNationalReachGate ? "true" : "false",
			attempt.pushEvidence ? "true" : "false",
			attempt.pushChancePassed ? "true" : "false",
			attempt.pullChancePassed ? "true" : "false",
			Csv(attempt.outcome),
			Csv(attempt.distributorId)
		}));
	}

	private void OnSupplySelection(SupplySelectionTelemetry selection) {
		int year = TimeManager.Instance?.CurrentDate.year ?? 1960;
		supplySelectionWriter.WriteLine(string.Join(",", new[] {
			currentAuditWeek.ToString(CultureInfo.InvariantCulture), year.ToString(CultureInfo.InvariantCulture), Csv(selection.labelId),
			Csv(selection.artistId), Csv(selection.artistIdentity.ToString()), Csv(selection.chosenProjectGenre.ToString()),
			selection.artistIdentityAvailableForNewSupply ? "true" : "false", selection.annualFloorRequested ? "true" : "false",
			selection.annualFloorReroutedToNormalCandidates ? "true" : "false", Csv(selection.selectionMode.ToString())
		}));
	}

	private void OnTraditionalPopFallback(GenreSupplyService.TraditionalPopFallbackTelemetry fallback) {
		int year = TimeManager.Instance?.CurrentDate.year ?? 1960;
		traditionalPopFallbackWriter?.WriteLine(string.Join(",", new[] {
			currentAuditWeek.ToString(CultureInfo.InvariantCulture), year.ToString(CultureInfo.InvariantCulture),
			Csv(fallback.Source), Csv(fallback.RequestedGenre.ToString())
		}));
	}

	private void OnWeekSettlement(ChartManager.CompletedWeekSettlement settlement) {
		if (settlement?.Entries == null) return;
		ChartManager.Instance?.AcknowledgeSettlementAudit(settlement); // sim invariant -- must run even when telemetry is suppressed
		if (completedWeekSettlementWriter == null) return; // heavy settlement telemetry suppressed under --lean-probe
		foreach (ChartManager.CompletedWeekSettlementEntry entry in settlement.Entries) {
			completedWeekSettlementWriter.WriteLine(string.Join(",", new[] {
				settlement.SettlementId.ToString(CultureInfo.InvariantCulture), settlement.Date.year.ToString(CultureInfo.InvariantCulture), settlement.SettlementId.ToString(CultureInfo.InvariantCulture),
				Csv(entry.RecordId), Csv(entry.LabelId), Csv(entry.LabelTier), Csv(entry.Format.ToString()),
				Csv(entry.Record?.projectRole.ToString() ?? ProjectRecordRole.ExternalOrLegacy.ToString()), Csv(entry.Genre),
				entry.Regions?.Sum(region => region.FinalCleared).ToString(CultureInfo.InvariantCulture) ?? "0", entry.Units.ToString(CultureInfo.InvariantCulture),
				F(entry.Gross), F(entry.ManufacturingCost), F(entry.ArtistRoyalty), F(entry.DistributionSkim), F(entry.LabelNet), Csv(entry.DistributionRecipientLabelId), F(entry.DistributionIncome), F(entry.MarketNet),
				entry.RetiredAfterSettlement ? "true" : "false", entry.BookedCount.ToString(CultureInfo.InvariantCulture), entry.AuditedCount.ToString(CultureInfo.InvariantCulture)
			}));
			foreach (ChartManager.CompletedWeekSettlementRegion region in entry.Regions ?? Array.Empty<ChartManager.CompletedWeekSettlementRegion>()) {
				completedWeekSettlementRegionalWriter?.WriteLine(string.Join(",", new[] {
					settlement.SettlementId.ToString(CultureInfo.InvariantCulture), settlement.Date.year.ToString(CultureInfo.InvariantCulture), settlement.SettlementId.ToString(CultureInfo.InvariantCulture), Csv(entry.RecordId), Csv(region.RegionId),
					region.RawIntent.ToString(CultureInfo.InvariantCulture), region.ServiceableIntent.ToString(CultureInfo.InvariantCulture), region.LocalCleared.ToString(CultureInfo.InvariantCulture), region.SpilloverCleared.ToString(CultureInfo.InvariantCulture), region.FinalCleared.ToString(CultureInfo.InvariantCulture),
					region.PhysicalBackorders.ToString(CultureInfo.InvariantCulture), region.MarketDisplacedDemand.ToString(CultureInfo.InvariantCulture), region.InventoryMovement.ToString(CultureInfo.InvariantCulture)
				}));
				if (entry.Format == ReleaseFormat.Album && entry.Record != null &&
					entry.Record.regionalData.TryGetValue(region.RegionId, out RegionalRecordData data)) {
					albumRealizationBridgeWriter?.WriteLine(string.Join(",", new[] {
						settlement.SettlementId.ToString(CultureInfo.InvariantCulture),
						settlement.Date.year.ToString(CultureInfo.InvariantCulture),
						settlement.SettlementId.ToString(CultureInfo.InvariantCulture),
						Csv(entry.RecordId), Csv(entry.LabelId), Csv(entry.LabelTier), Csv(entry.Genre), Csv(region.RegionId),
						entry.Record.baseRecord.releaseDate.year.ToString(CultureInfo.InvariantCulture),
						entry.Record.weeksSinceRelease.ToString(CultureInfo.InvariantCulture),
						F(data.albumBuyerPoolThisWeek), F(data.albumAwarenessThisWeek),
						F(data.albumObservedPenetrationThisWeek), F(data.albumEffectivePenetrationThisWeek),
						F(data.albumPeakEffectivePenetration), F(data.albumExhaustionThisWeek),
						F(data.albumCatalogDecayMultiplierThisWeek), F(data.albumFormatTiltThisWeek),
						F(data.albumConversionThisWeek), F(entry.Record.cannibalizationSuppression),
						F(data.albumRawDemandBeforeCannibalizationThisWeek), F(data.albumRawDemandAfterCannibalizationThisWeek),
						region.RawIntent.ToString(CultureInfo.InvariantCulture),
						data.albumUnitsInStoresBeforeSaleThisWeek.ToString(CultureInfo.InvariantCulture),
						data.storeCapacityThisWeek.ToString(CultureInfo.InvariantCulture),
						region.ServiceableIntent.ToString(CultureInfo.InvariantCulture),
						region.LocalCleared.ToString(CultureInfo.InvariantCulture),
						region.SpilloverCleared.ToString(CultureInfo.InvariantCulture),
						region.FinalCleared.ToString(CultureInfo.InvariantCulture),
						region.PhysicalBackorders.ToString(CultureInfo.InvariantCulture),
						region.MarketDisplacedDemand.ToString(CultureInfo.InvariantCulture),
						entry.Record.currentPosition.ToString(CultureInfo.InvariantCulture),
						ChartManager.Instance.GetWeeksSinceLastCharted(entry.Record).ToString(CultureInfo.InvariantCulture),
						ChartManager.Instance.GetWeeksSinceSalesAboveRetirementFloor(entry.Record).ToString(CultureInfo.InvariantCulture),
						ChartManager.Instance.GetAlbumCatalogSalesFloor().ToString(CultureInfo.InvariantCulture),
						entry.RetiredAfterSettlement ? "true" : "false"
					}));
				}
			}
		}
	}

	private void OnFormatMemoryRevision(FormatMemoryRevisionTelemetry revision) {
		if (formatMemoryRevisionWriter == null || revision == null) return;
		bool nonFinite = float.IsNaN(revision.normalizedResidual) || float.IsInfinity(revision.normalizedResidual) ||
			float.IsNaN(revision.opportunityScale) || float.IsInfinity(revision.opportunityScale);
		formatMemoryRevisionWriter.WriteLine(string.Join(",", new[] {
			currentAuditWeek.ToString(CultureInfo.InvariantCulture), (TimeManager.Instance?.CurrentDate.year ?? 1960).ToString(CultureInfo.InvariantCulture),
			Csv(revision.releaseId), Csv(revision.labelId), Csv(revision.projectId), Csv(revision.releaseLane.ToString()), Csv(revision.estimatorLane.ToString()),
			Csv(revision.format.ToString()), Csv(revision.genre.ToString()), revision.releaseAge.ToString(CultureInfo.InvariantCulture), Csv(revision.revisionKind),
			revision.revisionOrdinal.ToString(CultureInfo.InvariantCulture),
			F(revision.releaseTimeExpectedNet), F(revision.ageMatchedExpectedNet), F(revision.realizedNetToDate), F(revision.estimatedOutcomeNet), F(revision.opportunityScale),
			F(revision.normalizedResidual), F(revision.maturityWeight), F(revision.recencyWeight), revision.replacedPriorRevision ? "true" : "false", revision.finalized ? "true" : "false", nonFinite ? "true" : "false"
		}));
	}

	private void OnReleaseStrategy(ReleaseStrategyTelemetry strategy) {
		int year = TimeManager.Instance?.CurrentDate.year ?? 1960;
		if (formatMemoryAdjustmentWriter != null && ChartManager.Instance?.IsGenreMarketV2Live == true) {
			formatMemoryAdjustmentWriter.WriteLine(string.Join(",", new[] {
				currentAuditWeek.ToString(CultureInfo.InvariantCulture), year.ToString(CultureInfo.InvariantCulture), Csv(strategy.recordId), Csv(strategy.labelId),
				Csv(strategy.labelFormatMemoryBypassed ? "ProjectPrior" : "LabelFormat"), F(strategy.rawConfidenceSingle), F(strategy.rawConfidenceAlbum),
				F(strategy.confidenceSingle), F(strategy.confidenceAlbum), strategy.singleMemoryCapApplied ? "true" : "false", strategy.albumMemoryCapApplied ? "true" : "false"
			}));
		}
		if (!string.IsNullOrEmpty(strategy.recordId)) formatDecisionCohorts[strategy.recordId] = new FormatDecisionCohort {
			Year = year, PrimaryGenre = strategy.genre, SecondaryGenre = strategy.secondaryGenre, Format = strategy.chosenFormat
		};
		formatDecisionExplanationWriter.WriteLine(string.Join(",", new[] {
			currentAuditWeek.ToString(CultureInfo.InvariantCulture), year.ToString(CultureInfo.InvariantCulture), Csv(strategy.recordId),
			Csv(strategy.labelId), Csv(strategy.artistId), Csv(strategy.genre.ToString()), Csv(strategy.secondaryGenre.ToString()),
			Csv(strategy.careerState.ToString()), Csv(strategy.careerBand), Csv(strategy.chosenFormat.ToString()), F(strategy.singlePreTiltContribution),
			F(strategy.albumPreTiltContribution), F(strategy.albumAffinity), F(strategy.albumOpportunity),
			F(strategy.singleFormatTilt), F(strategy.albumFormatTilt), F(strategy.singleProductionCost), F(strategy.albumProductionCost),
			F(strategy.singleMemoryEma), F(strategy.albumMemoryEma), F(strategy.confidenceSingle), F(strategy.confidenceAlbum),
			F(strategy.singleMemoryBlend), F(strategy.albumMemoryBlend), F(strategy.singleNoiseMultiplier), F(strategy.albumNoiseMultiplier),
			F(strategy.projectedSingleNet), F(strategy.projectedAlbumNet),
			F(strategy.albumChoiceProbability), F(strategy.formatChoiceRoll),
			strategy.labelFormatMemoryBypassed ? "ProjectPrior" : "LabelFormat", string.Empty
		}));
		EnsureDecadeAnnualYear(year);
		bool careerTransitionThisYear = ObserveCareerTransition(strategy.artistId, strategy.careerState, year);
		float statureMultiplier = GetStatureMultiplier(strategy.careerState);
		float albumDemandFactor = CompetitorManager.CalculateAlbumDemandFactor(strategy.genre, year);
		string hitInventoryCohort = HasSingleReleasedBeforeYear(strategy.artistId, year) ? "carryover" : "newEntrant";
		releaseStrategyWriter.WriteLine(string.Join(",", new[] {
			currentAuditWeek.ToString(CultureInfo.InvariantCulture), year.ToString(CultureInfo.InvariantCulture),
			Csv(strategy.recordId), Csv(strategy.labelId), Csv(strategy.tier.ToString()), Csv(strategy.artistId),
			Csv(strategy.genre.ToString()), Csv(strategy.secondaryGenre.ToString()), Csv(strategy.careerState.ToString()), F(strategy.projectedSingleNet),
			F(strategy.projectedAlbumNet), F(strategy.confidenceSingle), F(strategy.confidenceAlbum), Csv(strategy.chosenFormat.ToString()),
			Csv(strategy.projectId), Csv(strategy.strategy.ToString()), F(strategy.projectedOrphanSingleNet),
			F(strategy.projectedAlbumStandaloneNet), F(strategy.projectedAlbumWithPromoNet), Csv(strategy.promoSingleId),
			strategy.albumStrategyEvaluated ? F(strategy.priorSingleNet) : string.Empty,
			strategy.albumStrategyEvaluated ? F(strategy.singleProductionCost) : string.Empty,
			strategy.albumStrategyEvaluated ? F(strategy.singleNetMarginPerUnit) : string.Empty,
			strategy.albumStrategyEvaluated ? F(strategy.expectedSingleUnits) : string.Empty,
			strategy.albumStrategyEvaluated ? F(strategy.albumDemandFactor) : string.Empty,
			strategy.albumStrategyEvaluated ? F(strategy.substitutionK) : string.Empty,
			strategy.albumStrategyEvaluated ? F(strategy.substitutionCap) : string.Empty,
			strategy.albumStrategyEvaluated ? F(strategy.substitutionPropensity) : string.Empty,
			strategy.albumStrategyEvaluated ? F(strategy.expectedOverlapFraction) : string.Empty,
			strategy.albumStrategyEvaluated ? F(strategy.divertedUnits) : string.Empty,
			strategy.albumStrategyEvaluated ? F(strategy.albumMarginPerUnit) : string.Empty,
			strategy.albumStrategyEvaluated ? F(strategy.cannibalizationLoss) : string.Empty,
			strategy.albumStrategyEvaluated ? F(strategy.cannibalizationCharged) : string.Empty,
			strategy.albumStrategyEvaluated ? F(strategy.expectedPromoLift) : string.Empty,
			strategy.albumStrategyEvaluated ? F(strategy.expectedPromoSingleNet) : string.Empty,
			strategy.albumStrategyEvaluated ? F(strategy.promoAdvantage) : string.Empty,
			F(strategy.albumChoiceProbability), F(strategy.formatChoiceRoll),
			strategy.albumCapacityReroute ? "true" : "false"
		}));
		priorCostAssumptionWriter.WriteLine(string.Join(",", new[] {
			currentAuditWeek.ToString(CultureInfo.InvariantCulture), year.ToString(CultureInfo.InvariantCulture),
			Csv(strategy.recordId), strategy.assumedCompilationCost ? "true" : "false",
			Csv(strategy.actualAlbumFormat?.ToString())
		}));
		float difference = strategy.projectedAlbumNet - strategy.projectedSingleNet;
		string ratio = strategy.projectedAlbumNet > 0f && strategy.projectedSingleNet > ForkRatioEpsilon
			? F(strategy.projectedAlbumNet / strategy.projectedSingleNet) : string.Empty;
		forkRatioWriter.WriteLine(string.Join(",", new[] {
			currentAuditWeek.ToString(CultureInfo.InvariantCulture), year.ToString(CultureInfo.InvariantCulture),
			Csv(strategy.recordId), Csv(strategy.labelId), Csv(strategy.artistId), Csv(strategy.genre.ToString()), Csv(strategy.secondaryGenre.ToString()),
			Csv(GetGenreGroup(strategy.genre)), Csv(strategy.careerState.ToString()), Csv(strategy.careerBand),
			F(strategy.qualityEstimate), Csv(strategy.qualityQuartile), F(strategy.reachFactor), F(strategy.genreSinglesMarketFactor),
			F(strategy.priorSingleNet), F(strategy.priorAlbumNet), F(strategy.projectedSingleNet), F(strategy.projectedAlbumNet),
			F(difference), ratio, Csv(strategy.chosenFormat.ToString())
		}));
		a3EconomicDecisionWriter.WriteLine(string.Join(",", new[] {
			currentAuditWeek.ToString(CultureInfo.InvariantCulture), year.ToString(CultureInfo.InvariantCulture),
			Csv(strategy.recordId), Csv(strategy.labelId), Csv(strategy.artistId), Csv(strategy.genre.ToString()), Csv(strategy.secondaryGenre.ToString()),
			Csv(GetGenreGroup(strategy.genre)), Csv(strategy.careerState.ToString()), Csv(strategy.careerBand), F(strategy.qualityEstimate), Csv(strategy.qualityQuartile),
			F(statureMultiplier), careerTransitionThisYear ? "true" : "false", F(strategy.reachFactor), F(albumDemandFactor),
			Csv(hitInventoryCohort), F(strategy.compCostWeight),
			F(strategy.expectedFormatMultiplier), Csv(strategy.actualAlbumFormat?.ToString()),
			strategy.releasedSingleIdsExamined.ToString(CultureInfo.InvariantCulture), strategy.resolvedSingles.ToString(CultureInfo.InvariantCulture),
			strategy.chartedSingles.ToString(CultureInfo.InvariantCulture), F(strategy.hitScore), F(strategy.unweightedHitUnits),
			F(strategy.weightedHitUnits), F(strategy.affinityUnits), F(strategy.totalExpectedAlbumUnits),
			F(strategy.priorSingleNet), F(strategy.priorAlbumNet), F(strategy.projectedSingleNet), F(strategy.projectedAlbumNet),
			Csv(strategy.chosenFormat.ToString())
		}));
		decadeAnnual.Decisions++;
		bool albumDecision = strategy.chosenFormat == ReleaseFormat.Album;
		bool adult = IsAdultGenre(strategy.genre);
		bool youth = IsYouthGenre(strategy.genre);
		if (albumDecision) decadeAnnual.AlbumDecisions++;
		if (adult) {
			decadeAnnual.AdultDecisions++;
			if (albumDecision) decadeAnnual.AdultAlbumDecisions++;
		}
		if (youth) {
			decadeAnnual.YouthDecisions++;
			if (albumDecision) decadeAnnual.YouthAlbumDecisions++;
		}
		if (strategy.strategy == ReleaseStrategy.OrphanSingle) decadeAnnual.OrphanDecisions++;
		else if (strategy.strategy == ReleaseStrategy.AlbumWithPromo) decadeAnnual.PromoDecisions++;
		else if (strategy.strategy == ReleaseStrategy.AlbumStandalone) decadeAnnual.StandaloneDecisions++;
		if (albumDecision) {
			decadeAnnual.AlbumConfidence += strategy.confidenceAlbum;
			decadeAnnual.AlbumConfidenceCount++;
		} else {
			decadeAnnual.SingleConfidence += strategy.confidenceSingle;
			decadeAnnual.SingleConfidenceCount++;
		}
		float expected = strategy.strategy switch {
			ReleaseStrategy.AlbumWithPromo => strategy.projectedAlbumWithPromoNet,
			ReleaseStrategy.AlbumStandalone => strategy.projectedAlbumStandaloneNet,
			_ => strategy.projectedOrphanSingleNet
		};
		if (!string.IsNullOrEmpty(strategy.recordId)) decisionExpectations[strategy.recordId] = new DecisionExpectation {
			Expected = expected,
			YouthCompilation = youth && strategy.actualAlbumFormat == AlbumFormat.Compilation,
			Promo = strategy.strategy == ReleaseStrategy.AlbumWithPromo
		};
	}

	private bool ObserveCareerTransition(string artistId, CareerState careerState, int year) {
		if (lastDecisionCareerState.TryGetValue(artistId, out CareerState previous) && previous != careerState) {
			observedCareerTransitionYear[artistId] = year;
		}
		lastDecisionCareerState[artistId] = careerState;
		return observedCareerTransitionYear.TryGetValue(artistId, out int transitionYear) && transitionYear == year;
	}

	private static float GetStatureMultiplier(CareerState careerState) => careerState switch {
		CareerState.Superstar => 2.5f,
		CareerState.Star => 2.0f,
		CareerState.Established => 1.5f,
		CareerState.Rising => 1.2f,
		_ => 1.0f
	};

	private static bool HasSingleReleasedBeforeYear(string artistId, int year) {
		SimulatedArtist artist = ArtistManager.Instance?.GetArtist(artistId);
		if (artist?.releasedSingleIds == null || ChartManager.Instance == null) return false;
		foreach (string recordId in artist.releasedSingleIds) {
			if (ChartManager.Instance.TryGetTrackSnapshot(recordId, out AlbumTrack track) &&
				track.releaseDate.year > 0 && track.releaseDate.year < year) return true;
		}
		return false;
	}

	private const float ForkRatioEpsilon = 0.000001f;
	private static bool IsAdultGenre(Genre genre) => genre is Genre.Jazz or Genre.EasyListening or Genre.Folk or
		Genre.TraditionalPop or Genre.BossaNova or Genre.Country;
	private static bool IsYouthGenre(Genre genre) => genre is Genre.RockAndRoll or Genre.TeenPop or Genre.RnB or
		Genre.DooWop or Genre.GirlGroup;
	private static string GetGenreGroup(Genre genre) => IsAdultGenre(genre) ? "Adult" : IsYouthGenre(genre) ? "Youth" : "Other";

	private void OnCalibrationDecision(CalibrationDecisionTelemetry decision) {
		int year = TimeManager.Instance?.CurrentDate.year ?? 1960;
		calibrationDecisionWriter.WriteLine(string.Join(",", new[] {
			currentAuditWeek.ToString(CultureInfo.InvariantCulture), year.ToString(CultureInfo.InvariantCulture),
			Csv(decision.recordId), Csv(decision.labelId), Csv(decision.artistId), Csv(decision.genre.ToString()),
			Csv(decision.careerState.ToString()), F(decision.qualityEstimate), F(decision.reachFactor),
			F(decision.genreSinglesMarketFactor), F(decision.singleProductionCost), Csv(decision.chosenFormat.ToString())
		}));
	}

	private void WriteLiveRecordsSnapshot() {
		int year = TimeManager.Instance?.CurrentDate.year ?? 1960;
		foreach (RecordRuntimeData record in ChartManager.Instance.GetAllRecords().OrderBy(record => record.baseRecord.recordId, StringComparer.Ordinal)) {
			liveRecordsSnapshotWriter.WriteLine(string.Join(",", new[] {
				currentAuditWeek.ToString(CultureInfo.InvariantCulture), year.ToString(CultureInfo.InvariantCulture),
				Csv(record.baseRecord.recordId), Csv(record.baseRecord.labelId), Csv(record.baseRecord.artistId),
				Csv(record.baseRecord.format.ToString()), record.weeksSinceRelease.ToString(CultureInfo.InvariantCulture),
				F(record.lifetimeLabelNet), F(record.sunkProductionCost), F(record.lifetimeLabelNet - record.sunkProductionCost),
				record.currentPosition.ToString(CultureInfo.InvariantCulture), record.totalUnitsSold.ToString(CultureInfo.InvariantCulture)
			}));
		}
	}

	private void WriteAlbumProjectSnapshots() {
		foreach (AlbumProject project in CompetitorManager.Instance.GetAlbumProjects().OrderBy(project => project.creationSequence)) {
			albumProjectWriter.WriteLine(string.Join(",", new[] {
				Csv(project.projectId), project.creationSequence.ToString(CultureInfo.InvariantCulture), Csv(project.originalLabelId),
				Csv(project.currentLabelId), Csv(project.tierAtSchedule.ToString()), Csv(project.genre.ToString()),
				Csv(project.careerStateAtSchedule.ToString()), project.scheduledWeek.ToString(CultureInfo.InvariantCulture),
				project.dropWeek.ToString(CultureInfo.InvariantCulture), Csv(project.strategy.ToString()), Csv(project.albumRecord?.recordId),
				Csv(project.promoSingleId), project.promoPeakAtDrop.ToString(CultureInfo.InvariantCulture), F(project.promoPeakScore),
				F(project.synergyAwarenessApplied), F(project.synergyStockMultiplier), Csv(project.terminalState.ToString()),
				project.wasTransferred ? "true" : "false", project.transferCount.ToString(CultureInfo.InvariantCulture),
				project.albumRetired ? "true" : "false", project.promoRetired ? "true" : "false",
				project.projectRealizedNet.HasValue ? F(project.projectRealizedNet.Value) : string.Empty
			}));
			RecordRuntimeData albumRuntime = ChartManager.Instance.GetRecordRuntimeData(project.albumRecord?.recordId);
			double raw = albumRuntime?.rawAlbumDemandBeforeCannibalization ?? project.rawDemandBeforeCannibalization;
			double suppressed = albumRuntime?.suppressedAlbumDemand ?? project.suppressedDemand;
			double activeLinked = albumRuntime?.albumDemandWithActiveLinkedPromo ?? project.demandWithActiveLinkedPromo;
			double inactiveLinked = albumRuntime?.albumDemandWithInactiveLinkedPromo ?? project.demandWithInactiveLinkedPromo;
			double weightedHeat = albumRuntime?.albumDemandWeightedSingleHeat ?? project.demandWeightedSingleHeat;
			double weightedPropensity = albumRuntime?.albumDemandWeightedSubstitutionPropensity ?? project.demandWeightedSubstitutionPropensity;
			double weightedSuppression = albumRuntime?.albumDemandWeightedSuppression ?? project.demandWeightedSuppression;
			albumProjectDemandWriter.WriteLine(string.Join(",", new[] {
				Csv(project.projectId), Csv(project.strategy.ToString()), Csv(project.albumRecord?.recordId), F(raw), F(suppressed),
				raw > 0d ? F(suppressed / raw) : string.Empty, F(project.initialLaunchAwareness),
				project.initialLaunchStock.ToString(CultureInfo.InvariantCulture), Csv(project.promoSingleId), F(activeLinked), F(inactiveLinked),
				raw > 0d ? F(weightedHeat / raw) : string.Empty,
				raw > 0d ? F(weightedPropensity / raw) : string.Empty,
				raw > 0d ? F(weightedSuppression / raw) : string.Empty
			}));
		}
	}

	private void OnReleaseOutcome(ReleaseOutcomeTelemetry outcome) {
		int year = TimeManager.Instance?.CurrentDate.year ?? 1960;
		EnsureDecadeAnnualYear(year);
		releaseOutcomeWriter.WriteLine(string.Join(",", new[] {
			currentAuditWeek.ToString(CultureInfo.InvariantCulture), year.ToString(CultureInfo.InvariantCulture),
			Csv(outcome.labelId), Csv(outcome.recordId), Csv(outcome.format.ToString()), Csv(outcome.genre.ToString()), outcome.memoryEligible ? "true" : "false",
			F(outcome.lifetimeLabelNet), F(outcome.sunkProductionCost), F(outcome.realizedNet)
		}));
		if (!string.IsNullOrEmpty(outcome.recordId) && decisionExpectations.TryGetValue(outcome.recordId, out DecisionExpectation expectation)) {
			decadeAnnual.CompletedMatched++;
			decadeAnnual.CompletedExpected += expectation.Expected;
			decadeAnnual.CompletedRealized += outcome.realizedNet;
			if (expectation.YouthCompilation) {
				decadeAnnual.YouthCompCompleted++;
				decadeAnnual.YouthCompExpected += expectation.Expected;
				decadeAnnual.YouthCompRealized += outcome.realizedNet;
			}
			if (expectation.Promo) {
				decadeAnnual.PromoCompleted++;
				decadeAnnual.PromoExpected += expectation.Expected;
				decadeAnnual.PromoRealized += outcome.realizedNet;
			}
			decisionExpectations.Remove(outcome.recordId);
		}
	}

	private void OnRecordRetired(RecordRuntimeData record) {
		if (formatDecisionCohorts.ContainsKey(record.baseRecord.recordId)) retiredDecisionCohortUnits[record.baseRecord.recordId] = record.totalUnitsSold;
		if (record.baseRecord.format == ReleaseFormat.Single) WriteRetirementRow("retired", record);
		else if (record.baseRecord.album != null) {
			observedAlbumIds.Add(record.baseRecord.album.albumId);
			retiredAlbumIds.Add(record.baseRecord.album.albumId);
		}
	}

	private void WriteFormatDecisionCohorts() {
		var liveUnits = ChartManager.Instance.GetAllRecords().ToDictionary(record => record.baseRecord.recordId, record => (long)record.totalUnitsSold, StringComparer.Ordinal);
		foreach (var group in formatDecisionCohorts.GroupBy(pair => (pair.Value.PrimaryGenre, pair.Value.Format)).OrderBy(group => group.Key.PrimaryGenre).ThenBy(group => group.Key.Format)) {
			long units = group.Sum(pair => liveUnits.TryGetValue(pair.Key, out long live) ? live : retiredDecisionCohortUnits.GetValueOrDefault(pair.Key));
			formatDecisionCohortWriter.WriteLine(string.Join(",", new[] {
				(TimeManager.Instance?.CurrentDate.year ?? 1960).ToString(CultureInfo.InvariantCulture), Csv(group.Key.PrimaryGenre.ToString()), Csv(group.Key.Format.ToString()),
				group.Count().ToString(CultureInfo.InvariantCulture), units.ToString(CultureInfo.InvariantCulture), F((float)units / Math.Max(1, group.Count()))
			}));
		}
		foreach (var pair in formatDecisionCohorts.OrderBy(pair => pair.Value.Year).ThenBy(pair => pair.Key, StringComparer.Ordinal)) {
			long units = liveUnits.TryGetValue(pair.Key, out long live) ? live : retiredDecisionCohortUnits.GetValueOrDefault(pair.Key);
			FormatDecisionCohort cohort = pair.Value;
			formatDecisionCohortDetailWriter.WriteLine(string.Join(",", new[] {
				cohort.Year.ToString(CultureInfo.InvariantCulture), Csv(pair.Key), Csv(cohort.PrimaryGenre.ToString()),
				Csv(cohort.SecondaryGenre.ToString()), Csv(cohort.Format.ToString()), units.ToString(CultureInfo.InvariantCulture)
			}));
		}
	}

	private void WriteActiveOffChartRetirementRows() {
		foreach (RecordRuntimeData record in ChartManager.Instance.GetAllRecords().Where(record => record.baseRecord.format == ReleaseFormat.Single && record.currentPosition == 0)) {
			WriteRetirementRow("active_off_chart_week52", record);
		}
	}

	private void WriteRetirementRow(string status, RecordRuntimeData record) {
		AILabel label = ChartManager.Instance.GetLabelById(record.baseRecord.labelId);
		retirementWriter.WriteLine(string.Join(",", new[] {
			currentAuditWeek.ToString(CultureInfo.InvariantCulture), Csv(status), Csv(record.baseRecord.recordId),
			Csv(label?.tier.ToString()), record.weeksSinceRelease.ToString(CultureInfo.InvariantCulture),
			record.weeksOnChart.ToString(CultureInfo.InvariantCulture),
			ChartManager.Instance.GetWeeksSinceLastCharted(record).ToString(CultureInfo.InvariantCulture),
			ChartManager.Instance.GetWeeksSinceSalesAboveRetirementFloor(record).ToString(CultureInfo.InvariantCulture),
			(record.lastSalesAboveRetirementFloorAge + 1).ToString(CultureInfo.InvariantCulture),
			record.unitsThisWeek.ToString(CultureInfo.InvariantCulture), F(ChartManager.Instance.GetRetirementRadioPlay(record))
		}));
	}

	private static StreamWriter CreateWriter(string path) =>
		new(path, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), 64 * 1024);

	private void InitializeObservedState() {
		foreach (RecordRuntimeData record in ChartManager.Instance.GetAllRecords().Where(record => record.baseRecord.format == ReleaseFormat.Single)) {
			ObserveRecord(record, wasPresentAtStart: true);
			observedReleaseIds.Add(record.baseRecord.recordId);
		}
		List<RecordRuntimeData> initialChart = ChartManager.Instance.GetCurrentChart();
		// A decade audit begins with an already-live chart. Seed those observed
		// identities before week one so a left-censored prewarm record that falls
		// off on the first live tick is not silently omitted from decade breadth.
		ObserveFirstChartIdentities(initialChart, leftCensoredAtRunStart: true);
		previousChartIds = initialChart
			.Select(record => record.baseRecord.recordId)
			.ToHashSet(StringComparer.Ordinal);
		previousActiveIds = ChartManager.Instance.GetAllRecords()
			.Select(record => record.baseRecord.recordId)
			.ToHashSet(StringComparer.Ordinal);
	}

	private void CaptureWeek(int week) {
		GameDate date = TimeManager.Instance.CurrentDate;
		GameDate salesDate = date.IsFriday ? date : date.AddDays(-1);
		EnsureDecadeAnnualYear(date.year);
		// Flush the fully completed prior-year revenue row before fail-fast may
		// abort at the new-year boundary. Do not emit a partial current-week row.
		AdvanceMarketRevenueYear(date.year);
		albumProjectWeeklyWriter.WriteLine(string.Join(",", new[] {
			week.ToString(CultureInfo.InvariantCulture), date.year.ToString(CultureInfo.InvariantCulture),
			CompetitorManager.Instance.WeeklyPipelineAlbumDrops.ToString(CultureInfo.InvariantCulture)
		}));
		List<RecordRuntimeData> records = ChartManager.Instance.GetAllRecords();
		CaptureFailFastWeekly(date, records);
		foreach (RecordRuntimeData album in records.Where(record => record.baseRecord.format == ReleaseFormat.Album)) {
			if (album.weeksSinceRelease > 26) decadeAnnual.AlbumUnitsOver26Weeks += album.unitsThisWeek;
			if (album.weeksSinceRelease > 52) decadeAnnual.AlbumUnitsOver52Weeks += album.unitsThisWeek;
		}
		List<RecordRuntimeData> singleRecords = records.Where(record => record.baseRecord.format == ReleaseFormat.Single).ToList();
		List<RecordRuntimeData> chart = ChartManager.Instance.GetCurrentChart();
		List<RecordRuntimeData> albumChart = ChartManager.Instance.GetCurrentAlbumChart();
		AccumulateConcentration(date.year, chart);
		AccumulateGenreShape(date.year, records, chart);
		// Skim routing to the distributor is only defined while the deal is active. Once the
		// deal resolves -- an exit that nulls it, or a subsidiary absorption that converts it
		// to ownership -- the label self-distributes and its residual skim fraction
		// (0.25*(1-ownedReach)) is not routed to anyone, so the equality no longer applies.
		if (forceDistributionDeal && forcedDealClient.activeDeal != null) {
			if (!Mathf.IsEqualApprox(forcedDealClient.weeklyDistributionSkim, forcedDealDistributor.weeklyDistributionIncome)) {
				throw new InvalidOperationException("Forced deal skim was not credited to its distributor.");
			}
			forcedDealRoutedTotal += forcedDealClient.weeklyDistributionSkim;
		}
		float chartCutoff = chart.Count >= 100 ? ChartSimulator.CalculateChartPoints(chart[99], regions) : 0f;
		var activeIds = singleRecords.Select(record => record.baseRecord.recordId).ToHashSet(StringComparer.Ordinal);
		var chartIds = chart.Select(record => record.baseRecord.recordId).ToHashSet(StringComparer.Ordinal);
		foreach (RecordRuntimeData record in singleRecords.Where(record => record.currentPosition > 0)) {
			string id = record.baseRecord.recordId;
			if (!decadeAnnual.ChartingSingles.TryGetValue(id, out var observed)) {
				decadeAnnual.ChartingSingles[id] = (record.GetQuality(), record.currentPosition);
			} else if (record.currentPosition < observed.BestPosition) {
				decadeAnnual.ChartingSingles[id] = (observed.Quality, record.currentPosition);
			}
		}

		foreach (RecordRuntimeData record in singleRecords) {
			WriteSingleLaneDiagnostics(week, date.year, record);
			LifecycleState state = ObserveRecord(record, wasPresentAtStart: false);
			if (state.DebutPosition == 0 && record.currentPosition > 0) {
				state.DebutPosition = record.currentPosition;
			}
			if (record.currentPosition == 1) state.WeeksAtNumberOne++;
			if (!aggregateOnly) WriteRecordRow(week, date.year, record, chartCutoff);
			if (!leanProbe) WriteBreakoutRows(week, record);
		}

		foreach ((string id, LifecycleState state) in lifecycle.ToArray()) {
			if (!activeIds.Contains(id)) {
				WriteLifecycleRow(week, state);
				if (state.Record.peakPosition > 0 && state.Record.peakPosition <= 40) decadeAnnual.ClosedTop40Weeks.Add(state.Record.weeksOnChart);
				lifecycle.Remove(id);
			}
		}

		RecordRuntimeData numberOne = chart.FirstOrDefault();
		int totalChartUnits = chart.Sum(record => record.unitsThisWeek);
		// The completed-week ledger includes records retired after their final sale;
		// active-record enumeration is intentionally not authoritative for this total.
		int totalMarketUnits = GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true
			? ChartManager.Instance.GetLastCompletedWeekSettlement()?.TotalUnits ?? records.Sum(record => record.unitsThisWeek)
			: records.Sum(record => record.unitsThisWeek);
		CaptureSeasonalityMonth(salesDate, records);
		int newTop100 = chartIds.Count(id => !previousChartIds.Contains(id));
		int newTop40 = chart.Take(40).Count(record => !previousChartIds.Contains(record.baseRecord.recordId));
		int exits = previousChartIds.Count(id => !chartIds.Contains(id));
		int newRecords = activeIds.Count(id => !previousActiveIds.Contains(id));
		int retiredRecords = previousActiveIds.Count(id => !activeIds.Contains(id));
		WriteTierVolumeRows(week, records);
		WriteLabelFinanceRows(week, date.year);
		WriteMarketRevenueRows(week, date.year, records);
		WriteMarketClearingRows(week, date.year);
		WriteReleaseCapacityRow(week, date.year);
		WriteRosterLifecycleRows(week, date.year);
		WriteLabelScoutingVacancyRows(week, date.year);
		WriteArtistPopulationRows(week, date.year, records);
		WriteAlbumRows(week, date, records, albumChart);
		WriteFormatMixRows(week, date.year, records);
		WriteRevenueMemoryRows(week, date.year);
		WriteGeographyMetricRows(week, date.year, records);
		WriteGenreMarketRows(week, date, records);
		// The detailed causal seams are established by the fully instrumented
		// 52-week checkpoints. Decade lean runs retain aggregate genre history but
		// suppress these high-volume per-record decompositions.
		if (!leanProbe) {
			WriteRecordGenreExplanationRows(week, date, records);
			WriteAlbumDemandExplanationRows(week, date, records);
		}
		CaptureRetirementCohortSnapshot(records);

		weekWriter.WriteLine(string.Join(",", new[] {
			week.ToString(CultureInfo.InvariantCulture),
			date.year.ToString(CultureInfo.InvariantCulture),
			totalChartUnits.ToString(CultureInfo.InvariantCulture),
			totalMarketUnits.ToString(CultureInfo.InvariantCulture),
			Csv(numberOne?.baseRecord.recordId),
			(numberOne?.unitsThisWeek ?? 0).ToString(CultureInfo.InvariantCulture),
			newTop100.ToString(CultureInfo.InvariantCulture),
			newTop40.ToString(CultureInfo.InvariantCulture),
			exits.ToString(CultureInfo.InvariantCulture),
			records.Count.ToString(CultureInfo.InvariantCulture),
			newRecords.ToString(CultureInfo.InvariantCulture),
			retiredRecords.ToString(CultureInfo.InvariantCulture)
		}));

		previousChartIds = chartIds;
		previousActiveIds = activeIds;
	}

	private void WriteSingleLaneDiagnostics(int week, int year, RecordRuntimeData record) {
		if (singleReleaseLaneWriter == null || record?.baseRecord == null) return;
		AILabel label = ChartManager.Instance.GetLabelById(record.baseRecord.labelId);
		if (singleReleaseLaneIdsWritten.Add(record.baseRecord.recordId)) singleReleaseLaneWriter.WriteLine(string.Join(",", new[] {
			week.ToString(CultureInfo.InvariantCulture), year.ToString(CultureInfo.InvariantCulture), Csv(record.baseRecord.recordId), Csv(record.albumProjectId),
			Csv(record.projectRole.ToString()), Csv(record.baseRecord.labelId), Csv(label?.tier.ToString()), Csv(record.baseRecord.artistId),
			Csv(record.baseRecord.primaryGenre.ToString()), Csv(record.launchCareerState.ToString()), F(record.baseRecord.hookStrength),
			F(record.baseRecord.productionQuality), F(record.baseRecord.danceability), F(record.GetQuality()), F(record.enabledOpportunityMass),
			F(record.acceptedOpportunityMass), F(record.cohortOpportunityNormalizer), Csv(record.cohortOpportunityNormalizerSource), record.cohortOpportunityColdStartFallback ? "true" : "false" }));
		if (singleDemandStagesWriter != null) foreach (MarketRegion region in regions) if (record.regionalData.TryGetValue(region.regionId, out RegionalRecordData data)) {
			float inventoryRate = data.rawDemandThisWeek > 0f ? Mathf.Clamp(data.serviceableIntentThisWeek / data.rawDemandThisWeek, 0f, 1f) : 1f;
			float marketRate = data.serviceableIntentThisWeek > 0 ? Mathf.Clamp((float)(data.localClearedThisWeek + data.spilloverClearedThisWeek) / data.serviceableIntentThisWeek, 0f, 1f) : 1f;
			singleDemandStagesWriter.WriteLine(string.Join(",", new[] { week.ToString(CultureInfo.InvariantCulture), year.ToString(CultureInfo.InvariantCulture),
				Csv(record.baseRecord.recordId), Csv(record.projectRole.ToString()), Csv(region.regionId), record.weeksSinceRelease.ToString(CultureInfo.InvariantCulture),
				F(data.demandPotentialAudience), F(data.demandBaselineAwareness), F(data.demandEarnedDiscoveryExposure), F(data.demandAwareBuyers),
				F(data.demandIntrinsicQualityFactor), F(data.demandAcceptanceFactor), F(data.demandFormatFactor), F(data.demandIntrinsicConversionRate),
				F(data.rawDemandThisWeek), data.serviceableIntentThisWeek.ToString(CultureInfo.InvariantCulture), (data.localClearedThisWeek + data.spilloverClearedThisWeek).ToString(CultureInfo.InvariantCulture),
				F(data.demandChartSignal), F(data.demandMomentumSignal), F(data.demandRadioSignal), F(inventoryRate), F(marketRate) }));
		}
	}

	private void WriteRosterLifecycleRows(int week, int year) {
		if (rosterLifecycleWriter == null) return;
		AILabel[] labels = ChartManager.Instance.GetAllLabels().Where(label => label?.roster != null).ToArray();
		var membershipCounts = new Dictionary<SimulatedArtist, int>();
		foreach (AILabel label in labels) {
			foreach (SimulatedArtist artist in label.roster.Where(artist => artist != null))
				membershipCounts[artist] = membershipCounts.GetValueOrDefault(artist) + 1;
		}
		ArtistManager artists = ArtistManager.Instance;
		RosterManager roster = RosterManager.Instance;
		CompetitorManager competitors = CompetitorManager.Instance;
		int freeAgentPoolSize = artists?.GetEnabledFreeAgentPoolSize() ?? 0;
		int duplicatePoolEntries = artists?.GetEnabledDuplicatePoolEntries() ?? 0;
		int poolOwnershipConflicts = artists?.GetEnabledPoolOwnershipConflicts() ?? 0;
		int totalRosterSize = 0;
		int totalEmptyRosters = 0;
		int totalEligible = 0;
		int totalTerminalRostered = 0;
		int totalOwnershipConflicts = poolOwnershipConflicts;
		int totalDrops = 0;
		int totalFirstTimeSignings = 0;
		int totalReSignings = 0;
		int totalUniqueReSignings = 0;
		int totalShortWindowRedrops = 0;
		int totalScoutingGatePasses = 0;
		int totalSigningAttempts = 0;
		int totalCandidateRejections = 0;
		int totalAffordabilityRejections = 0;
		int totalAttempts = 0;
		int totalSuccessful = 0;
		int totalSelectionFailures = 0;
		foreach (LabelTier tier in new[] { LabelTier.Major, LabelTier.MidTier, LabelTier.Independent, LabelTier.Small, LabelTier.Boutique }) {
			AILabel[] tierLabels = labels.Where(label => label.tier == tier).ToArray();
			SimulatedArtist[] tierArtists = tierLabels.SelectMany(label => label.roster).Where(artist => artist != null).ToArray();
			int rosterSize = tierArtists.Length;
			int emptyRosters = tierLabels.Count(label => label.roster.Count == 0);
			int eligible = tierLabels.Sum(label => label.CountArtistsEligibleForRelease(year));
			int terminalRostered = tierArtists.Count(artist => GenreSupplyService.IsTerminalCareerState(artist.careerState));
			int ownershipConflicts = tierArtists.Count(artist => artist.labelId != tierLabels.FirstOrDefault(label => label.roster.Contains(artist))?.labelId ||
				membershipCounts.GetValueOrDefault(artist) != 1);
			RosterManager.RosterLifecycleFlow rosterFlow = roster?.GetWeeklyLifecycleFlow(tier) ?? default;
			CompetitorManager.ReleaseLifecycleFlow releaseFlow = competitors?.GetWeeklyReleaseLifecycleFlow(tier) ?? default;
			WriteRosterLifecycleRow(week, year, tier.ToString(), rosterSize, emptyRosters, eligible, rosterFlow, freeAgentPoolSize,
				terminalRostered, ownershipConflicts, duplicatePoolEntries, releaseFlow);
			totalRosterSize += rosterSize;
			totalEmptyRosters += emptyRosters;
			totalEligible += eligible;
			totalTerminalRostered += terminalRostered;
			totalOwnershipConflicts += ownershipConflicts;
			totalDrops += rosterFlow.DropsToPool;
			totalFirstTimeSignings += rosterFlow.FirstTimeSignings;
			totalReSignings += rosterFlow.ReSignings;
			totalUniqueReSignings += rosterFlow.UniqueReSignings;
			totalShortWindowRedrops += rosterFlow.ShortWindowRedrops;
			totalScoutingGatePasses += rosterFlow.ScoutingGatePasses;
			totalSigningAttempts += rosterFlow.SigningAttempts;
			totalCandidateRejections += rosterFlow.CandidateRejections;
			totalAffordabilityRejections += rosterFlow.AffordabilityRejections;
			totalAttempts += releaseFlow.Attempts;
			totalSuccessful += releaseFlow.SuccessfulReleases;
			totalSelectionFailures += releaseFlow.ArtistSelectionFailures;
		}
		WriteRosterLifecycleRow(week, year, "All", totalRosterSize, totalEmptyRosters, totalEligible,
			new RosterManager.RosterLifecycleFlow(totalDrops, totalFirstTimeSignings, totalReSignings, totalUniqueReSignings,
				totalShortWindowRedrops, totalScoutingGatePasses, totalSigningAttempts, totalCandidateRejections, totalAffordabilityRejections), freeAgentPoolSize,
			totalTerminalRostered, totalOwnershipConflicts, duplicatePoolEntries,
			new CompetitorManager.ReleaseLifecycleFlow(totalAttempts, totalSuccessful, totalSelectionFailures));
	}

	private void WriteArtistPopulationEvent(string eventType, SimulatedArtist artist) {
		if (artist == null) return;
		int year = TimeManager.Instance?.CurrentDate.year ?? artist.formedYear;
		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		AILabel label = ChartManager.Instance?.GetLabelById(artist.labelId);
		if (catastrophicFailFast && IsRuntimeBirthWeekSigningViolationForProbe(eventType, label, week))
			throw new CatastrophicAbortException("BirthWeekSigning", "runtimeFounderContract", week, label.runtimeBirthWeek,
				$"label={label.labelId} artist={artist.artistId} date={TimeManager.Instance?.CurrentDate}");
		if (artistPopulationEventsWriter == null) return;
		if (eventType is "signing" or "re-signing") {
			(int firstTime, int repeat) = populationSigningFlowByWeek.GetValueOrDefault(week);
			populationSigningFlowByWeek[week] = eventType == "re-signing" ? (firstTime, repeat + 1) : (firstTime + 1, repeat);
		}
		Musician lead = artist.GetLeadSinger() ?? artist.members.FirstOrDefault(member => member.isActive);
		artistPopulationEventsWriter.WriteLine(string.Join(",", new[] {
			(SimulationSeedBootstrap.RequestedSeed ?? 0UL).ToString(CultureInfo.InvariantCulture), week.ToString(CultureInfo.InvariantCulture),
			Csv(TimeManager.Instance?.CurrentDate.ToString()), Csv(eventType), Csv(artist.artistId), Csv(artist.type.ToString()), Csv(artist.cohort.ToString()),
			artist.formedYear.ToString(CultureInfo.InvariantCulture), Csv(artist.formationPrimaryGenre.ToString()), Csv(artist.formationSecondaryGenre.ToString()),
			Csv(artist.primaryGenre.ToString()), Csv(artist.homeRegion), Csv(artist.lifecycleStatus.ToString()), Csv(artist.careerState.ToString()),
			Csv(artist.prospectMarketStatus.ToString()), Csv(artist.prospectMarketStatusBeforeContract.ToString()), Csv(artist.careerStateBeforeDrop.ToString()), Csv(artist.contractEntryCareerState.ToString()), Csv(artist.labelId),
			Csv(label?.tier.ToString() ?? ""), Csv(artist.lastDropReason.ToString()), artist.performanceDropCount.ToString(CultureInfo.InvariantCulture),
			ArtistManager.GetPerformanceDropCooldownWeeks(artist).ToString(CultureInfo.InvariantCulture), artist.contractSequence.ToString(CultureInfo.InvariantCulture),
			Mathf.Max(0, artist.contractSequence - 1).ToString(CultureInfo.InvariantCulture),
			artist.contractStartWeek.ToString(CultureInfo.InvariantCulture), artist.contractTop40Hits.ToString(CultureInfo.InvariantCulture),
			artist.contractConsecutiveFlops.ToString(CultureInfo.InvariantCulture), artist.contractCompletedChartRuns.ToString(CultureInfo.InvariantCulture),
			Csv((eventType is "performance-departure" or "performance-exhaustion" ? artist.lastPerformanceEvaluationMode : artist.GetPerformanceEvaluationMode()).ToString()),
			(eventType is "performance-departure" or "performance-exhaustion" ? artist.lastRequiredPerformanceCompletedRuns : artist.RequiredPerformanceCompletedRuns).ToString(CultureInfo.InvariantCulture),
			(eventType is "performance-departure" or "performance-exhaustion" ? artist.lastRequiredPerformanceConsecutiveFlops : artist.RequiredPerformanceConsecutiveFlops).ToString(CultureInfo.InvariantCulture),
			(eventType is "performance-departure" or "performance-exhaustion" ? artist.lastContractProbationPending : artist.IsContractPerformanceProbationPending()) ? "true" : "false",
			ArtistManager.Instance.GetWeeksSincePerformanceDrop(artist, week).ToString(CultureInfo.InvariantCulture), artist.weeksContinuouslyUnowned.ToString(CultureInfo.InvariantCulture),
			(year - artist.formedYear).ToString(CultureInfo.InvariantCulture), (lead?.GetAge(year) ?? 0).ToString(CultureInfo.InvariantCulture)
		}));
	}

	private void WriteLabelScoutingVacancyRows(int week, int year) {
		if (labelScoutingVacancyWriter == null || RosterManager.Instance == null) return;
		RosterManager.Instance.FinalizeScoutingVacancyTelemetryForCapture();
		foreach (RosterManager.LabelScoutingVacancyObservation observation in RosterManager.Instance.GetWeeklyScoutingVacancyObservations()) {
			AILabel label = ChartManager.Instance?.GetLabelById(observation.LabelId);
			string birthDate = label != null && label.runtimeBirthYear > 0 ? $"{label.runtimeBirthMonth}/{label.runtimeBirthDay}/{label.runtimeBirthYear}" : "";
			labelScoutingVacancyWriter.WriteLine(string.Join(",", new[] {
				week.ToString(CultureInfo.InvariantCulture), year.ToString(CultureInfo.InvariantCulture), Csv(observation.LabelId), Csv(observation.LabelTier.ToString()),
				observation.IsActiveLabel ? "true" : "false",
				observation.MaxRosterSize.ToString(CultureInfo.InvariantCulture), (label?.OperatingRosterTarget ?? observation.OperatingRosterTarget).ToString(CultureInfo.InvariantCulture),
				Csv(label?.operatingRosterTargetSource ?? "Unset"), Csv(label?.populationOrigin.ToString() ?? "Unspecified"),
				(label?.runtimeBirthWeek ?? 0).ToString(CultureInfo.InvariantCulture), Csv(birthDate), Csv(label?.operatingRosterTargetReason.ToString() ?? "Unset"),
				(label?.organicRosterTargetGrowthCount ?? 0).ToString(CultureInfo.InvariantCulture), (label?.lastOrganicRosterTargetGrowthWeek ?? -1).ToString(CultureInfo.InvariantCulture), Csv(label?.lastOrganicGrowthBlockingReason ?? "Unset"),
				observation.RosterSize.ToString(CultureInfo.InvariantCulture), observation.UnusedRosterSlots.ToString(CultureInfo.InvariantCulture),
				observation.UnusedOperatingRosterSlots.ToString(CultureInfo.InvariantCulture), observation.IsEmptyRoster ? "true" : "false",
				observation.ConsecutiveVacancyWeeks.ToString(CultureInfo.InvariantCulture), observation.ConsecutiveEmptyWeeks.ToString(CultureInfo.InvariantCulture),
				F(observation.ScoutingAbility), F(observation.ScoutingRosterFullness), observation.HasRecentHit ? "true" : "false", F(observation.RecentHitFactor),
				observation.DecliningArtistCount.ToString(CultureInfo.InvariantCulture), F(observation.DecliningFactor), F(observation.EstimatedAdvance),
				observation.CanAffordEstimatedAdvance ? "true" : "false", F(observation.ComputedScoutProbability),
				observation.ScoutRandomRoll.HasValue ? F(observation.ScoutRandomRoll.Value) : "", observation.ScoutingGatePassed ? "true" : "false",
				observation.EligibleCandidateCount?.ToString(CultureInfo.InvariantCulture) ?? "", observation.DiscoveryPoolCount?.ToString(CultureInfo.InvariantCulture) ?? "",
				observation.BestCandidateScore.HasValue ? F(observation.BestCandidateScore.Value) : "",
				observation.NeverSignedSlateCount?.ToString(CultureInfo.InvariantCulture) ?? "", observation.QualifyingNeverSignedCount?.ToString(CultureInfo.InvariantCulture) ?? "",
				observation.BestNeverSignedScore.HasValue ? F(observation.BestNeverSignedScore.Value) : "",
				observation.ThirdPlusPerformanceComebackCount?.ToString(CultureInfo.InvariantCulture) ?? "", observation.OverallBestContractSequence?.ToString(CultureInfo.InvariantCulture) ?? "",
				observation.FreshPreferenceApplied ? "1" : "0", observation.RepeatComebackDeferred ? "1" : "0", Csv(observation.FreshPreferenceFallbackReason),
				observation.SigningAttempted ? "true" : "false", observation.SigningSucceeded ? "true" : "false", Csv(observation.SigningKind), Csv(observation.FailureReason),
				observation.ScoutingRosterSize.ToString(CultureInfo.InvariantCulture), observation.ScoutingUnusedRosterSlots.ToString(CultureInfo.InvariantCulture),
				observation.ScoutingUnusedOperatingRosterSlots.ToString(CultureInfo.InvariantCulture), observation.ScoutingIsEmptyRoster ? "true" : "false",
				observation.ReleaseEligibleArtistCount.ToString(CultureInfo.InvariantCulture), observation.RequiredReleaseLanes.ToString(CultureInfo.InvariantCulture),
				observation.HeadcountDeficit.ToString(CultureInfo.InvariantCulture), observation.ReleaseLaneDeficit.ToString(CultureInfo.InvariantCulture),
				observation.ServiceDeficit.ToString(CultureInfo.InvariantCulture), observation.ServiceDeficitAge.ToString(CultureInfo.InvariantCulture), Csv(observation.ServiceMode),
				observation.ScoutingGateBypassed ? "1" : "0", observation.FreshLaneCount.ToString(CultureInfo.InvariantCulture), observation.ExperiencedLaneCount.ToString(CultureInfo.InvariantCulture),
				Csv(observation.FreshDiscoveryScope), observation.BestFreshPotentialScore.HasValue ? F(observation.BestFreshPotentialScore.Value) : "",
				observation.BestExperiencedProductionScore.HasValue ? F(observation.BestExperiencedProductionScore.Value) : "", Csv(observation.SelectedLane),
				observation.RecoveryThresholdFallbackUsed ? "1" : "0", Csv(observation.RecoveryFailureReason)
			}));
		}
	}

	private void WriteMarketClearingRows(int week, int year) {
		if (marketClearingWriter == null || ChartManager.Instance?.IsGenreMarketV2Live != true) return;
		foreach (ChartManager.MarketClearingRegionalSummary row in ChartManager.Instance.GetLastMarketClearingSummaries()
			.OrderBy(row => row.RegionId, StringComparer.Ordinal)) {
			int cleared = row.ClearedTotalUnits;
			int serviceable = row.ServiceableTotalIntent;
			int unused = Mathf.Max(0, row.PurchaseCapacity - cleared);
			float factor = serviceable > 0 ? Mathf.Min(1f, (float)cleared / serviceable) : 1f;
			marketClearingWriter.WriteLine(string.Join(",", new[] {
				week.ToString(CultureInfo.InvariantCulture), year.ToString(CultureInfo.InvariantCulture), Csv(row.RegionId),
				row.ActiveIntentCount.ToString(CultureInfo.InvariantCulture), F(row.RawSingleDemand), F(row.RawAlbumDemand), F(row.RawSingleDemand + row.RawAlbumDemand),
				F(row.ServiceableSingleIntent), F(row.ServiceableAlbumIntent),
				F(row.EffectiveAlbumIntent), F(row.AlbumOverlapPressure),
				row.SingleFormatBudget.ToString(CultureInfo.InvariantCulture), row.AlbumFormatBudget.ToString(CultureInfo.InvariantCulture),
				serviceable.ToString(CultureInfo.InvariantCulture),
				row.PurchaseCapacity.ToString(CultureInfo.InvariantCulture), row.BasePurchaseCapacity.ToString(CultureInfo.InvariantCulture),
				row.AlbumChannelCapacity.ToString(CultureInfo.InvariantCulture),
				row.LocalClearedUnits.ToString(CultureInfo.InvariantCulture), row.UnusedAfterLocal.ToString(CultureInfo.InvariantCulture),
				row.ExportBudget.ToString(CultureInfo.InvariantCulture), row.ExportedCapacity.ToString(CultureInfo.InvariantCulture),
				row.ImportLimit.ToString(CultureInfo.InvariantCulture), row.ImportedCapacity.ToString(CultureInfo.InvariantCulture), row.SpilloverClearedUnits.ToString(CultureInfo.InvariantCulture),
				row.ClearedSingleUnits.ToString(CultureInfo.InvariantCulture), row.ClearedAlbumUnits.ToString(CultureInfo.InvariantCulture), cleared.ToString(CultureInfo.InvariantCulture), unused.ToString(CultureInfo.InvariantCulture),
				F(factor), row.PhysicalBackorders.ToString(CultureInfo.InvariantCulture), row.MarketDisplacedDemand.ToString(CultureInfo.InvariantCulture),
				row.MarketDisplacedDemand.ToString(CultureInfo.InvariantCulture), row.InventoryViolationCount.ToString(CultureInfo.InvariantCulture), row.AllocationViolationCount.ToString(CultureInfo.InvariantCulture),
				row.ReconciliationDelta.ToString(CultureInfo.InvariantCulture), row.ReconciliationDelta.ToString(CultureInfo.InvariantCulture)
			}));
		}
		foreach (ChartManager.MarketSpilloverTransfer transfer in ChartManager.Instance.GetLastMarketSpilloverTransfers()
			.OrderBy(row => row.DonorRegionId, StringComparer.Ordinal).ThenBy(row => row.RecipientRegionId, StringComparer.Ordinal)) {
			marketSpilloverWriter?.WriteLine(string.Join(",", new[] {
				week.ToString(CultureInfo.InvariantCulture), year.ToString(CultureInfo.InvariantCulture), Csv(transfer.DonorRegionId), Csv(transfer.RecipientRegionId),
				transfer.DonorUnusedLocal.ToString(CultureInfo.InvariantCulture), transfer.DonorExportBudget.ToString(CultureInfo.InvariantCulture),
				transfer.RecipientResidualDemand.ToString(CultureInfo.InvariantCulture), transfer.RecipientImportLimit.ToString(CultureInfo.InvariantCulture),
				transfer.TransferredCapacity.ToString(CultureInfo.InvariantCulture), transfer.ClearedSingleUnits.ToString(CultureInfo.InvariantCulture), transfer.ClearedAlbumUnits.ToString(CultureInfo.InvariantCulture),
				transfer.EdgeViolationCount.ToString(CultureInfo.InvariantCulture), transfer.ReconciliationDelta.ToString(CultureInfo.InvariantCulture)
			}));
		}
	}

	private void WriteOperatingRosterTargetEvent(LabelLifecycleManager.OperatingRosterTargetEvent targetEvent) {
		if (labelOperatingTargetEventWriter == null || targetEvent?.Label == null) return;
		AILabel label = targetEvent.Label;
		string birthDate = label.runtimeBirthYear > 0 ? $"{label.runtimeBirthMonth}/{label.runtimeBirthDay}/{label.runtimeBirthYear}" : "";
		float overhead = label.GetMonthlyOverhead();
		labelOperatingTargetEventWriter.WriteLine(string.Join(",", new[] {
			targetEvent.Week.ToString(CultureInfo.InvariantCulture), Csv(targetEvent.Date.ToString()), Csv(label.labelId), Csv(label.populationOrigin.ToString()),
			label.runtimeBirthWeek.ToString(CultureInfo.InvariantCulture), Csv(birthDate), Csv(targetEvent.Reason.ToString()),
			targetEvent.PriorTarget.ToString(CultureInfo.InvariantCulture), targetEvent.NewTarget.ToString(CultureInfo.InvariantCulture), label.maxRosterSize.ToString(CultureInfo.InvariantCulture),
			label.organicRosterTargetGrowthCount.ToString(CultureInfo.InvariantCulture), targetEvent.WeeksSincePreviousOrganicIncrease.ToString(CultureInfo.InvariantCulture), Csv(targetEvent.EligibilityResult), Csv(targetEvent.BlockingReason),
			Csv(label.status.ToString()), Csv(label.tier.ToString()), label.CurrentRosterSize.ToString(CultureInfo.InvariantCulture), label.CountArtistsEligibleForRelease(targetEvent.Date.year).ToString(CultureInfo.InvariantCulture),
			targetEvent.RecentChartingCount.ToString(CultureInfo.InvariantCulture), targetEvent.RecentReleaseCount.ToString(CultureInfo.InvariantCulture),
			F(label.lastMonthlyProfit), label.consecutiveLossMonths.ToString(CultureInfo.InvariantCulture), F(label.cashReserves), F(overhead), F(overhead > 0f ? label.cashReserves / overhead : 0f)
		}));
	}

	private void WriteRuntimeLabelProfile(RuntimeLabelProfileFactory.Result profile) {
		if (profile?.Label == null) return;
		AILabel label = profile.Label;
		birthTierByLabel[label.labelId] = label.tier;
		if (runtimeLabelProfileWriter == null) return;
		runtimeLabelProfileWriter.WriteLine(string.Join(",", new[] {
			profile.Seed.ToString(CultureInfo.InvariantCulture), profile.BirthWeek.ToString(CultureInfo.InvariantCulture), Csv(profile.BirthDate.ToString()),
			Csv(label.labelId), Csv(label.labelName), Csv(label.tier.ToString()), Csv(label.archetype.ToString()), Csv(label.headquartersCity),
			Csv(label.homeRegion), Csv(label.homeCityId), Csv(label.homeCityAssignmentSource),
			Csv(string.Join(";", label.preferredGenres ?? Array.Empty<Genre>())), Csv(string.Join(";", label.secondaryGenres ?? Array.Empty<Genre>())),
			F(label.budgetLevel), F(label.scoutingAbility), F(label.productionQuality), F(label.marketingPower), F(label.ownedReach), F(label.nationalReach),
			F(label.riskTolerance), F(label.artistLoyalty), F(label.payolaWillingness), F(label.releasesPerMonth), F(label.cashReserves), F(label.reputation),
			F(label.marketShare), F(label.debtLevel), label.foundedYear.ToString(CultureInfo.InvariantCulture), label.monthsActive.ToString(CultureInfo.InvariantCulture),
			label.totalReleases.ToString(CultureInfo.InvariantCulture), label.top40Hits.ToString(CultureInfo.InvariantCulture), label.numberOneHits.ToString(CultureInfo.InvariantCulture),
			label.maxRosterSize.ToString(CultureInfo.InvariantCulture), label.OperatingRosterTarget.ToString(CultureInfo.InvariantCulture), Csv(RuntimeLabelProfileFactory.ProfileVersion)
		}));
	}

	private void WriteDailyTalentMarket(RosterManager.DailyTalentMarketSummary summary) {
		if (dailyTalentMarketWriter == null || summary == null) return;
		dailyTalentMarketWriter.WriteLine(string.Join(",", new[] {
			Csv(summary.Date.ToString()), summary.ChartWeek.ToString(CultureInfo.InvariantCulture), summary.EligibleVacancies.ToString(CultureInfo.InvariantCulture),
			summary.DueLabels.ToString(CultureInfo.InvariantCulture), summary.SupplySnapshotCount.ToString(CultureInfo.InvariantCulture), summary.FreshSupplySnapshotCount.ToString(CultureInfo.InvariantCulture),
			summary.ExperiencedSupplySnapshotCount.ToString(CultureInfo.InvariantCulture), summary.Nominations.ToString(CultureInfo.InvariantCulture), summary.UniqueNominatedArtists.ToString(CultureInfo.InvariantCulture),
			summary.CollisionArtists.ToString(CultureInfo.InvariantCulture), summary.CollisionOffers.ToString(CultureInfo.InvariantCulture), summary.AcceptedOffers.ToString(CultureInfo.InvariantCulture),
			summary.CollisionLosers.ToString(CultureInfo.InvariantCulture), summary.InvalidatedBeforeCommit.ToString(CultureInfo.InvariantCulture)
		}));
	}

	private void WriteDailyTalentAppointment(RosterManager.DailyTalentMarketAppointment appointment) {
		if (dailyTalentAppointmentWriter == null || appointment?.Label == null) return;
		AILabel label = appointment.Label; RosterManager.ArtistChoiceUtility utility = appointment.Choice;
		string opened = label.vacancyOpenedYear > 0 ? label.VacancyOpenedDate.ToString() : "";
		dailyTalentAppointmentWriter.WriteLine(string.Join(",", new[] {
			Csv(appointment.Date.ToString()), (ChartManager.Instance?.GetCurrentChartWeek() ?? 0).ToString(CultureInfo.InvariantCulture), Csv(label.labelId), Csv(label.populationOrigin.ToString()), Csv(label.tier.ToString()),
			label.vacancyGeneration.ToString(CultureInfo.InvariantCulture), Csv(opened), Csv(appointment.ScheduledDate.ToString()), Csv(appointment.Date.ToString()), label.scoutingAppointmentOrdinal.ToString(CultureInfo.InvariantCulture),
			Csv("DailyTwoPhase"), appointment.FreshLaneCount.ToString(CultureInfo.InvariantCulture), appointment.ExperiencedLaneCount.ToString(CultureInfo.InvariantCulture), Csv(appointment.SelectedArtist?.artistId), Csv(appointment.SelectedLane), Csv(appointment.Outcome),
			appointment.CollisionOfferCount.ToString(CultureInfo.InvariantCulture), Csv(appointment.WinnerLabelId), F(utility.Total), F(utility.Genre), F(utility.Locality), F(utility.Royalty), F(utility.Advance), F(utility.Reputation), F(utility.Reach), F(utility.RosterOpportunity), F(utility.Affinity),
			Csv(label.HasNextScoutingDate ? label.NextScoutingDate.ToString() : "")
		}));
	}

	private void WriteArtistPopulationRows(int week, int year, List<RecordRuntimeData> records) {
		if (artistPopulationWeeklyWriter == null) return;
		ArtistManager manager = ArtistManager.Instance;
		if (artistLaborMarketWeeklyWriter != null) {
			ArtistManager.LaborMarketWeeklySnapshot market = manager.GetLaborMarketWeeklySnapshot();
			string[] fields = {
				(SimulationSeedBootstrap.RequestedSeed ?? 0UL).ToString(CultureInfo.InvariantCulture), week.ToString(CultureInfo.InvariantCulture), Csv(TimeManager.Instance?.CurrentDate.ToString()),
				market.registryPopulation.ToString(CultureInfo.InvariantCulture), market.initialLegacyPopulation.ToString(CultureInfo.InvariantCulture), market.enabledInitialReservePopulation.ToString(CultureInfo.InvariantCulture), market.runtimeFormationPopulation.ToString(CultureInfo.InvariantCulture),
				market.activeRostered.ToString(CultureInfo.InvariantCulture), market.experiencedFreeAgents.ToString(CultureInfo.InvariantCulture), market.seekingProspects.ToString(CultureInfo.InvariantCulture), market.latentProspects.ToString(CultureInfo.InvariantCulture),
					market.freshSeeking.ToString(CultureInfo.InvariantCulture), market.freshLatent.ToString(CultureInfo.InvariantCulture),
				market.affordableHiringVacancies.ToString(CultureInfo.InvariantCulture), market.requestedProspectActivations.ToString(CultureInfo.InvariantCulture), market.actualProspectActivations.ToString(CultureInfo.InvariantCulture), market.prospectSearchSpellExpirations.ToString(CultureInfo.InvariantCulture),
				F(market.meanSeekingQuality), F(market.meanLatentQuality), F(market.activationMeanQuality), F(market.activationQ1), F(market.activationQ2), F(market.activationQ3), F(market.activationQ4),
				market.maxProspectMarketSpellCount.ToString(CultureInfo.InvariantCulture), market.duplicateSeekingEntries.ToString(CultureInfo.InvariantCulture), market.latentUnsignedPoolEntries.ToString(CultureInfo.InvariantCulture), market.seekingMissingFromUnsignedPool.ToString(CultureInfo.InvariantCulture), market.prospectStatusContractConflicts.ToString(CultureInfo.InvariantCulture),
					market.latentRotations.ToString(CultureInfo.InvariantCulture)
			};
			// The split is where firstTimeSignings/repeatSignings are spliced in once the
			// event-owned flows for the week are known: everything through
			// prospectSearchSpellExpirations, then the quality distribution and integrity
			// counters. Adding a column before that point moves the index.
			deferredLaborMarketRows.Add((week, string.Join(",", fields.Take(17)), string.Join(",", fields.Skip(17))));
		}
		SimulatedArtist[] artists = manager.GetAllArtists().OrderBy(artist => artist.artistId, StringComparer.Ordinal).ToArray();
		AILabel[] labels = ChartManager.Instance.GetAllLabels().Where(label => label != null).ToArray();
		var memberships = labels.SelectMany(label => label.roster ?? new List<SimulatedArtist>())
			.Where(artist => artist != null).GroupBy(artist => artist).ToDictionary(group => group.Key, group => group.Count());
		int currentWeek = ChartManager.Instance.GetCurrentChartWeek();
		IEnumerable<(string Tier, SimulatedArtist[] Scope)> scopes = new[] { ("All", artists) }.Concat(
			labels.GroupBy(label => label.tier).Select(group => (group.Key.ToString(), artists.Where(artist => labels.Any(label => label.tier == group.Key && label.roster.Contains(artist))).ToArray())));
		foreach ((string tier, SimulatedArtist[] scope) in scopes) {
			int rostered = scope.Count(artist => !string.IsNullOrEmpty(artist.labelId));
			int eligibleDropped = scope.Count(artist => artist.careerState == CareerState.Dropped && manager.IsEligibleForPopulationSigning(artist, currentWeek));
			int cooldown = scope.Count(artist => artist.careerState == CareerState.Dropped && artist.lastDropReason == ArtistDropReason.Performance && !manager.IsEligibleForPopulationSigning(artist, currentWeek));
			int duplicates = memberships.Where(pair => pair.Value > 1 && scope.Contains(pair.Key)).Sum(pair => pair.Value - 1);
			RosterManager.RosterLifecycleFlow flow = tier == "All"
				? RosterManager.Instance.GetAggregateWeeklyLifecycleFlow()
				: RosterManager.Instance.GetWeeklyLifecycleFlow(Enum.Parse<LabelTier>(tier));
			int terminalRostered = scope.Count(artist => !string.IsNullOrEmpty(artist.labelId) &&
				(GenreSupplyService.IsTerminalCareerState(artist.careerState) || artist.lifecycleStatus is ArtistLifecycleStatus.Retired or ArtistLifecycleStatus.Disbanded));
			int terminalReleaseEligible = scope.Count(artist => GenreSupplyService.IsTerminalCareerState(artist.careerState) &&
				GenreSupplyService.IsEligibleExistingArtistForEnabledRelease(artist));
			artistPopulationWeeklyWriter.WriteLine(string.Join(",", new[] {
				week.ToString(CultureInfo.InvariantCulture), year.ToString(CultureInfo.InvariantCulture), Csv(tier), scope.Length.ToString(CultureInfo.InvariantCulture),
				scope.Count(artist => artist.lifecycleStatus == ArtistLifecycleStatus.Active).ToString(CultureInfo.InvariantCulture), rostered.ToString(CultureInfo.InvariantCulture),
				scope.Count(artist => artist.careerState == CareerState.Unsigned && string.IsNullOrEmpty(artist.labelId)).ToString(CultureInfo.InvariantCulture), eligibleDropped.ToString(CultureInfo.InvariantCulture), cooldown.ToString(CultureInfo.InvariantCulture),
				scope.Count(artist => artist.lifecycleStatus == ArtistLifecycleStatus.Inactive).ToString(CultureInfo.InvariantCulture), scope.Count(artist => artist.lifecycleStatus == ArtistLifecycleStatus.Retired).ToString(CultureInfo.InvariantCulture), scope.Count(artist => artist.lifecycleStatus == ArtistLifecycleStatus.Disbanded).ToString(CultureInfo.InvariantCulture),
				(tier == "All" ? manager.FormedThisWeek : 0).ToString(CultureInfo.InvariantCulture), (tier == "All" ? manager.FormedYtd : 0).ToString(CultureInfo.InvariantCulture),
				flow.FirstTimeSignings.ToString(CultureInfo.InvariantCulture), flow.ReSignings.ToString(CultureInfo.InvariantCulture),
				flow.PerformanceDrops.ToString(CultureInfo.InvariantCulture), flow.OtherDepartures.ToString(CultureInfo.InvariantCulture),
				flow.RecentPerformanceReSignings.ToString(CultureInfo.InvariantCulture), flow.PrematureProbationDrops.ToString(CultureInfo.InvariantCulture),
				flow.NoEligibleCandidatePasses.ToString(CultureInfo.InvariantCulture), flow.ScoreRejections.ToString(CultureInfo.InvariantCulture),
				flow.AffordabilityRejections.ToString(CultureInfo.InvariantCulture), manager.GetEnabledPoolOwnershipConflicts().ToString(CultureInfo.InvariantCulture),
				duplicates.ToString(CultureInfo.InvariantCulture), manager.GetEnabledDuplicatePoolEntries().ToString(CultureInfo.InvariantCulture),
				terminalRostered.ToString(CultureInfo.InvariantCulture), terminalReleaseEligible.ToString(CultureInfo.InvariantCulture)
			}));
		}
		foreach (RecordRuntimeData record in records.Where(record => record?.baseRecord != null && observedPopulationProjectIds.Add(record.baseRecord.recordId))) {
			SimulatedArtist artist = manager.GetArtist(record.baseRecord.artistId);
			if (artist == null) continue;
			AILabel label = ChartManager.Instance.GetLabelById(record.baseRecord.labelId);
			bool native = record.baseRecord.primaryGenre == artist.formationPrimaryGenre;
			AlbumProject project = CompetitorManager.Instance.GetAlbumProject(record.albumProjectId);
			CareerState projectCareerState = project?.careerStateAtSchedule ?? artist.careerState;
			CareerState projectPreDropState = project?.careerStateBeforeDropAtSchedule ?? artist.careerStateBeforeDrop;
			CareerState projectEntryState = project?.contractEntryCareerStateAtSchedule ?? artist.contractEntryCareerState;
			int projectContractSequence = project?.contractSequenceAtSchedule ?? artist.contractSequence;
			int projectContractStartWeek = project?.contractStartWeekAtSchedule ?? artist.contractStartWeek;
			int projectWeek = project?.scheduledWeek ?? week;
			artistProjectIdentityWriter.WriteLine(string.Join(",", new[] { week.ToString(CultureInfo.InvariantCulture), year.ToString(CultureInfo.InvariantCulture), Csv(record.baseRecord.recordId), Csv(record.albumProjectId), Csv(artist.artistId), artist.formedYear.ToString(CultureInfo.InvariantCulture), Csv(artist.cohort.ToString()), Csv(artist.formationPrimaryGenre.ToString()), Csv(artist.primaryGenre.ToString()), Csv(record.baseRecord.primaryGenre.ToString()), native ? "true" : "false", native ? "false" : "true", Csv(record.baseRecord.labelId), Csv(label?.tier.ToString() ?? ""), Csv(record.baseRecord.format.ToString()), Csv(projectCareerState.ToString()), Csv(projectPreDropState.ToString()), Csv(projectEntryState.ToString()), projectContractSequence.ToString(CultureInfo.InvariantCulture), projectContractStartWeek.ToString(CultureInfo.InvariantCulture), (projectContractStartWeek < 0 ? -1 : projectWeek - projectContractStartWeek).ToString(CultureInfo.InvariantCulture), projectContractSequence > 1 ? "true" : "false" }));
		}
		if (week % 52 != 0) return;
		int active = artists.Count(artist => artist.lifecycleStatus == ArtistLifecycleStatus.Active);
		int signed = artists.Count(artist => !string.IsNullOrEmpty(artist.labelId));
		foreach (var group in artists.GroupBy(artist => (artist.cohort, artist.formationPrimaryGenre, artist.lifecycleStatus, Tier: ChartManager.Instance.GetLabelById(artist.labelId)?.tier.ToString() ?? "Unsigned"))) {
			SimulatedArtist[] cohort = group.ToArray();
			artistCohortAnnualWriter.WriteLine(string.Join(",", new[] { year.ToString(CultureInfo.InvariantCulture), Csv(group.Key.cohort.ToString()), Csv(group.Key.formationPrimaryGenre.ToString()), Csv(group.Key.lifecycleStatus.ToString()), Csv(group.Key.Tier), cohort.Length.ToString(CultureInfo.InvariantCulture), "0", "0", cohort.Sum(artist => artist.totalReleases).ToString(CultureInfo.InvariantCulture), cohort.Count(artist => artist.lifecycleStatus == ArtistLifecycleStatus.Active && string.IsNullOrEmpty(artist.labelId)).ToString(CultureInfo.InvariantCulture), cohort.Count(artist => artist.prospectMarketStatus == ProspectMarketStatus.Seeking).ToString(CultureInfo.InvariantCulture), cohort.Count(artist => artist.prospectMarketStatus == ProspectMarketStatus.Latent).ToString(CultureInfo.InvariantCulture), Median(cohort.Select(artist => year - artist.formedYear)).ToString(CultureInfo.InvariantCulture), Median(cohort.SelectMany(artist => artist.members).Select(member => member.GetAge(year))).ToString(CultureInfo.InvariantCulture), cohort.Count(artist => artist.lifecycleStatus == ArtistLifecycleStatus.Inactive).ToString(CultureInfo.InvariantCulture), cohort.Count(artist => artist.lifecycleStatus == ArtistLifecycleStatus.Retired).ToString(CultureInfo.InvariantCulture), cohort.Count(artist => artist.lifecycleStatus == ArtistLifecycleStatus.Disbanded).ToString(CultureInfo.InvariantCulture), F(active == 0 ? 0f : (float)cohort.Count(artist => artist.lifecycleStatus == ArtistLifecycleStatus.Active) / active), F(signed == 0 ? 0f : (float)cohort.Count(artist => !string.IsNullOrEmpty(artist.labelId)) / signed) }));
		}
	}

	private static int Median(IEnumerable<int> values) {
		int[] ordered = values.OrderBy(value => value).ToArray();
		return ordered.Length == 0 ? 0 : ordered[ordered.Length / 2];
	}

	private void WriteRosterLifecycleRow(int week, int year, string tier, int rosterSize, int emptyRosters, int releaseEligible,
		RosterManager.RosterLifecycleFlow rosterFlow, int freeAgentPoolSize, int terminalRostered, int ownershipConflicts,
		int duplicatePoolEntries, CompetitorManager.ReleaseLifecycleFlow releaseFlow) {
		rosterLifecycleWriter.WriteLine(string.Join(",", new[] {
			week.ToString(CultureInfo.InvariantCulture), year.ToString(CultureInfo.InvariantCulture), Csv(tier),
			rosterSize.ToString(CultureInfo.InvariantCulture), emptyRosters.ToString(CultureInfo.InvariantCulture),
			releaseEligible.ToString(CultureInfo.InvariantCulture),
			rosterFlow.DropsToPool.ToString(CultureInfo.InvariantCulture), rosterFlow.FirstTimeSignings.ToString(CultureInfo.InvariantCulture),
			rosterFlow.ReSignings.ToString(CultureInfo.InvariantCulture), rosterFlow.UniqueReSignings.ToString(CultureInfo.InvariantCulture),
			rosterFlow.ShortWindowRedrops.ToString(CultureInfo.InvariantCulture), rosterFlow.ScoutingGatePasses.ToString(CultureInfo.InvariantCulture),
			rosterFlow.SigningAttempts.ToString(CultureInfo.InvariantCulture), rosterFlow.CandidateRejections.ToString(CultureInfo.InvariantCulture),
			rosterFlow.AffordabilityRejections.ToString(CultureInfo.InvariantCulture), freeAgentPoolSize.ToString(CultureInfo.InvariantCulture),
			terminalRostered.ToString(CultureInfo.InvariantCulture), ownershipConflicts.ToString(CultureInfo.InvariantCulture),
			duplicatePoolEntries.ToString(CultureInfo.InvariantCulture), releaseFlow.Attempts.ToString(CultureInfo.InvariantCulture),
			releaseFlow.SuccessfulReleases.ToString(CultureInfo.InvariantCulture), releaseFlow.ArtistSelectionFailures.ToString(CultureInfo.InvariantCulture)
		}));
	}

	private void CaptureSeasonalityMonth(GameDate date, List<RecordRuntimeData> records) {
		var key = (date.year, date.month);
		if (!seasonalityMonths.TryGetValue(key, out SeasonalityMonthRollup rollup)) {
			rollup = new SeasonalityMonthRollup();
			seasonalityMonths[key] = rollup;
		}
		CompetitorManager competitors = CompetitorManager.Instance;
		RosterManager roster = RosterManager.Instance;
		long singleUnits = records.Where(record => record.baseRecord.format == ReleaseFormat.Single).Sum(record => (long)record.unitsThisWeek);
		long albumUnits = records.Where(record => record.baseRecord.format == ReleaseFormat.Album).Sum(record => (long)record.unitsThisWeek);
		rollup.LiveWeeks++;
		rollup.SingleUnits += singleUnits;
		rollup.AlbumUnits += albumUnits;
		rollup.SingleGross += singleUnits * competitors.GetPricePerUnitForAudit(ReleaseFormat.Single);
		rollup.AlbumGross += albumUnits * competitors.GetPricePerUnitForAudit(ReleaseFormat.Album);
		rollup.ReleaseRolls += competitors.WeeklyReleaseRollsFired;
		rollup.SuccessfulReleases += competitors.WeeklySuccessfulReleases;
		rollup.SingleReleases += competitors.WeeklySingleReleases;
		rollup.AlbumProjectsScheduled += competitors.WeeklyAlbumProjectsScheduled;
		rollup.AlbumDrops += competitors.WeeklyPipelineAlbumDrops;
		rollup.ProductionSpend += competitors.WeeklyProductionSpend;
		rollup.ProductionEvents += competitors.WeeklyProductionEvents;
		rollup.MarketingSpend += competitors.WeeklyMarketingSpend;
		rollup.MarketingEvents += competitors.WeeklyMarketingEvents;
		rollup.ScoutingRolls += roster?.WeeklyScoutingRolls ?? 0;
		rollup.Signings += roster?.WeeklySignings ?? 0;
		foreach (RegionalRecordData data in records.SelectMany(record => record.regionalData.Values)) { rollup.RadioPlaySum += data.radioPlay; rollup.RadioPlayCount++; }
	}

	private void WriteSeasonalityMonthlyRows() {
		if (seasonalityMonthlyWriter == null) return;
		foreach (var pair in seasonalityMonths.OrderBy(pair => pair.Key.Year).ThenBy(pair => pair.Key.Month)) {
			int year = pair.Key.Year;
			int month = pair.Key.Month;
			SeasonalityMonthRollup row = pair.Value;
			seasonalityMonthlyWriter.WriteLine(string.Join(",", new[] {
				requestedSeed?.ToString(CultureInfo.InvariantCulture) ?? string.Empty, MarketSeasonality.Enabled ? "true" : "false",
				year.ToString(CultureInfo.InvariantCulture), month.ToString(CultureInfo.InvariantCulture), row.LiveWeeks.ToString(CultureInfo.InvariantCulture),
				F(MarketSeasonality.GetSingleSalesMultiplier(year, month, true)), F(MarketSeasonality.GetAlbumSalesMultiplier(year, month, true)), F(MarketSeasonality.GetRadioOpportunity(year, month, true)),
				F(MarketSeasonality.GetVenueAttendanceMultiplier(year, month, true)), F(MarketSeasonality.GetRecordingCostMultiplier(year, month, true)), F(MarketSeasonality.GetMarketingEfficiencyMultiplier(year, month, true)), F(MarketSeasonality.GetArtistAvailabilityMultiplier(year, month, true)),
				row.SingleUnits.ToString(CultureInfo.InvariantCulture), row.AlbumUnits.ToString(CultureInfo.InvariantCulture), F(row.SingleGross), F(row.AlbumGross),
				row.ReleaseRolls.ToString(CultureInfo.InvariantCulture), row.SuccessfulReleases.ToString(CultureInfo.InvariantCulture), row.SingleReleases.ToString(CultureInfo.InvariantCulture), row.AlbumProjectsScheduled.ToString(CultureInfo.InvariantCulture), row.AlbumDrops.ToString(CultureInfo.InvariantCulture),
				F(row.ProductionSpend), row.ProductionEvents.ToString(CultureInfo.InvariantCulture), F(row.MarketingSpend), row.MarketingEvents.ToString(CultureInfo.InvariantCulture),
				row.ScoutingRolls.ToString(CultureInfo.InvariantCulture), row.Signings.ToString(CultureInfo.InvariantCulture), F(row.RadioPlayCount > 0 ? row.RadioPlaySum / row.RadioPlayCount : 0d)
			}));
		}
	}

	private void WriteRevenueMemoryRows(int week, int year) {
		EnsureDecadeAnnualYear(year);
		decadeAnnual.SingleMemoryEma = 0d;
		decadeAnnual.SingleMemoryLabels = 0;
		decadeAnnual.SingleMemoryN = 0;
		decadeAnnual.AlbumMemoryEma = 0d;
		decadeAnnual.AlbumMemoryLabels = 0;
		decadeAnnual.AlbumMemoryN = 0;
		ReleaseFormat[] formats = { ReleaseFormat.Single, ReleaseFormat.Album };
		foreach (AILabel label in CompetitorManager.Instance.GetAllLabels().OrderBy(label => label.labelId, StringComparer.Ordinal)) {
			foreach (ReleaseFormat format in formats) {
				label.revenueMemory.TryGetValue(format, out FormatRevenueMemory memory);
				float ema = memory?.emaNetPerRelease ?? 0f;
				int observations = memory?.releasesObserved ?? 0;
				if (format == ReleaseFormat.Single) {
					decadeAnnual.SingleMemoryEma += ema;
					decadeAnnual.SingleMemoryLabels++;
					decadeAnnual.SingleMemoryN += observations;
				} else {
					decadeAnnual.AlbumMemoryEma += ema;
					decadeAnnual.AlbumMemoryLabels++;
					decadeAnnual.AlbumMemoryN += observations;
				}
				revenueMemoryWriter.WriteLine(string.Join(",", new[] {
					week.ToString(CultureInfo.InvariantCulture), year.ToString(CultureInfo.InvariantCulture), Csv(label.labelId), Csv(format.ToString()),
					F(ema), observations.ToString(CultureInfo.InvariantCulture)
				}));
			}
		}
	}

	private sealed class GenreShapeYearState {
		public long MarketUnits, ChartUnits, ChartRecordWeeks, Top40RecordWeeks, Top10RecordWeeks, NumberOneWeeks, PositionSum;
		public int NewReleases, ActiveRecordsYearEnd;
		public readonly HashSet<string> ChartingRecordIds = new(StringComparer.Ordinal);
	}

	/// <summary>
	/// Decade genre shape. Read-only: it observes the resolved weekly state after sales and
	/// never draws RNG or mutates anything, so it is safe on any run and is deliberately not
	/// gated behind --lean-probe -- the decade runs that matter all pass that flag.
	///
	/// The point of the file is that market influence and chart influence are separate
	/// questions. A genre can hold a quarter of the chart on records that barely sell, which
	/// is exactly what easy-listening and country do here, and no single share number shows
	/// it. So units are accumulated over the whole live population, chart presence over the
	/// chart, and the two shares are reported side by side with their difference.
	/// </summary>
	private void AccumulateGenreShape(int year, List<RecordRuntimeData> records, List<RecordRuntimeData> chart) {
		if (genreShapeWriter == null) return;
		if (genreShapeYear == 0) genreShapeYear = year;
		if (year != genreShapeYear) {
			WriteGenreShapeYear();
			genreShapeByYear.Clear();
			genreShapeYear = year;
		}

		GenreShapeYearState State(Genre genre) {
			if (!genreShapeByYear.TryGetValue(genre, out GenreShapeYearState state)) {
				state = new GenreShapeYearState();
				genreShapeByYear[genre] = state;
			}
			return state;
		}

		foreach (RecordRuntimeData record in records) {
			Genre genre = GenreCatalog.MapLegacy(record.baseRecord.primaryGenre, year);
			GenreShapeYearState state = State(genre);
			state.MarketUnits += record.unitsThisWeek;
			// First sight of a record with no meaningful age is a release this year. Prewarm
			// titles enter already aged, so they are excluded rather than banked onto 1960.
			if (genreShapeSeenRecordIds.Add(record.baseRecord.recordId) && record.weeksSinceRelease <= 1) state.NewReleases++;
		}

		foreach (RecordRuntimeData record in chart) {
			int position = record.currentPosition;
			if (position <= 0) continue;
			GenreShapeYearState state = State(GenreCatalog.MapLegacy(record.baseRecord.primaryGenre, year));
			state.ChartRecordWeeks++;
			state.ChartUnits += record.unitsThisWeek;
			state.PositionSum += position;
			if (position <= 40) state.Top40RecordWeeks++;
			if (position <= 10) state.Top10RecordWeeks++;
			if (position == 1) state.NumberOneWeeks++;
			state.ChartingRecordIds.Add(record.baseRecord.recordId);
		}

		foreach (GenreShapeYearState state in genreShapeByYear.Values) state.ActiveRecordsYearEnd = 0;
		foreach (RecordRuntimeData record in records)
			State(GenreCatalog.MapLegacy(record.baseRecord.primaryGenre, year)).ActiveRecordsYearEnd++;
	}

	private void WriteGenreShapeYear() {
		if (genreShapeWriter == null || genreShapeYear == 0 || genreShapeByYear.Count == 0) return;
		long totalMarketUnits = genreShapeByYear.Values.Sum(state => state.MarketUnits);
		long totalChartUnits = genreShapeByYear.Values.Sum(state => state.ChartUnits);
		long totalChartWeeks = genreShapeByYear.Values.Sum(state => state.ChartRecordWeeks);
		string seed = requestedSeed?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
		foreach (var pair in genreShapeByYear.OrderBy(pair => pair.Key.ToString(), StringComparer.Ordinal)) {
			GenreShapeYearState state = pair.Value;
			GenreProfile profile = GenreCatalog.All.FirstOrDefault(candidate => candidate.Genre == pair.Key);
			float marketShare = totalMarketUnits > 0 ? (float)state.MarketUnits / totalMarketUnits : 0f;
			float chartWeekShare = totalChartWeeks > 0 ? (float)state.ChartRecordWeeks / totalChartWeeks : 0f;
			float chartUnitShare = totalChartUnits > 0 ? (float)state.ChartUnits / totalChartUnits : 0f;
			genreShapeWriter.WriteLine(string.Join(",", new[] {
				seed, genreShapeYear.ToString(CultureInfo.InvariantCulture), Csv(pair.Key.ToString()),
				Csv(profile?.Family.ToString() ?? "Unknown"),
				profile != null ? F(profile.EmergenceYear) : string.Empty,
				profile?.DeathYear != null ? F(profile.DeathYear.Value) : string.Empty,
				profile != null ? F(profile.GetBaseline(genreShapeYear)) : string.Empty,
				Csv(profile?.GetLifecycle(genreShapeYear).ToString() ?? "Unknown"),
				state.NewReleases.ToString(CultureInfo.InvariantCulture),
				state.ActiveRecordsYearEnd.ToString(CultureInfo.InvariantCulture),
				state.MarketUnits.ToString(CultureInfo.InvariantCulture), F(marketShare),
				state.ChartRecordWeeks.ToString(CultureInfo.InvariantCulture), F(chartWeekShare),
				state.ChartingRecordIds.Count.ToString(CultureInfo.InvariantCulture),
				state.ChartUnits.ToString(CultureInfo.InvariantCulture), F(chartUnitShare),
				state.Top40RecordWeeks.ToString(CultureInfo.InvariantCulture),
				state.Top10RecordWeeks.ToString(CultureInfo.InvariantCulture),
				state.NumberOneWeeks.ToString(CultureInfo.InvariantCulture),
				state.ChartRecordWeeks > 0 ? F((float)state.PositionSum / state.ChartRecordWeeks) : string.Empty,
				// The diagnostic column: chart presence a genre's sales do not support.
				F(chartWeekShare - marketShare)
			}));
		}
	}

	private void AccumulateConcentration(int year, List<RecordRuntimeData> chart) {
		if (concentrationYear == 0) concentrationYear = year;
		if (year != concentrationYear) {
			WriteConcentrationYear();
			annualChartUnitsByLabel.Clear();
			annualChartEntryTierByRecord.Clear();
			annualTop40TierByRecord.Clear();
			annualChartEntryOwnerByRecord.Clear();
			annualTop40OwnerByRecord.Clear();
			concentrationYear = year;
		}
		foreach (RecordRuntimeData record in chart) {
			string currentOwnerId = record.baseRecord.labelId;
			if (string.IsNullOrEmpty(currentOwnerId)) continue;
			annualChartUnitsByLabel[currentOwnerId] =
				annualChartUnitsByLabel.GetValueOrDefault(currentOwnerId) + record.unitsThisWeek;

			string recordId = record.baseRecord.recordId;
			if (string.IsNullOrEmpty(recordId)) continue;
			string releaseLabelId = string.IsNullOrEmpty(record.releaseLabelId) ? currentOwnerId : record.releaseLabelId;
			AILabel releaseLabel = CompetitorManager.Instance.GetLabel(releaseLabelId);
			LabelTier entryTier = releaseLabel?.tier
				?? CompetitorManager.Instance.GetLabel(currentOwnerId)?.tier
				?? LabelTier.Small;
			annualChartEntryTierByRecord[recordId] = entryTier;
			annualChartEntryOwnerByRecord[recordId] = currentOwnerId;
			if (record.currentPosition >= 1 && record.currentPosition <= 40) {
				annualTop40TierByRecord[recordId] = entryTier;
				annualTop40OwnerByRecord[recordId] = currentOwnerId;
			}
		}
		ObserveFirstChartIdentities(chart, leftCensoredAtRunStart: false);
	}

	private void ObserveFirstChartIdentities(IEnumerable<RecordRuntimeData> chart, bool leftCensoredAtRunStart) {
		RecordRuntimeData[] observedChart = (chart ?? Enumerable.Empty<RecordRuntimeData>()).ToArray();
		float publishedCutoff = observedChart.Length > 0
			? ChartSimulator.CalculateChartPoints(observedChart[^1], regions)
			: 0f;
		foreach (RecordRuntimeData record in observedChart) {
			string currentOwnerId = record?.baseRecord?.labelId;
			if (string.IsNullOrEmpty(currentOwnerId)) continue;
			// An acquisition transfers economics and operating control, but it does
			// not retroactively change the imprint printed on a released single.
			// Count that immutable release identity for decade breadth; annual firm
			// concentration continues to follow the audit's owner rollup.
			string releaseLabelId = string.IsNullOrEmpty(record.releaseLabelId)
				? currentOwnerId
				: record.releaseLabelId;
			if (cumulativeChartingLabelIds.Add(releaseLabelId)) {
				AILabel releaseLabel = CompetitorManager.Instance.GetLabel(releaseLabelId);
				AILabel currentOwner = CompetitorManager.Instance.GetLabel(currentOwnerId);
				LabelTier firstTier = releaseLabel?.tier ?? currentOwner?.tier ?? LabelTier.Small;
				firstChartTierByLabel[releaseLabelId] = firstTier;
				string labelName = releaseLabel?.labelName ?? currentOwner?.labelName ?? releaseLabelId;
				if (!string.IsNullOrWhiteSpace(labelName)) cumulativeChartingLabelNames.Add(labelName.Trim());
				WriteFirstChartEvent(record, releaseLabelId, currentOwnerId,
					releaseLabel, currentOwner, firstTier, publishedCutoff, leftCensoredAtRunStart);
			}
		}
	}

	private void WriteFirstChartEvent(RecordRuntimeData record, string releaseLabelId,
		string currentOwnerId, AILabel releaseLabel, AILabel currentOwner, LabelTier firstTier,
		float publishedCutoff, bool leftCensoredAtRunStart) {
		if (firstChartEventWriter == null || record?.baseRecord == null) return;
		AILabel capabilityLabel = currentOwner ?? releaseLabel;
		var strongRegions = (releaseLabel?.strongRegions ?? System.Array.Empty<string>())
			.ToHashSet(StringComparer.Ordinal);
		float bestStrongPeak = record.regionalData
			.Where(pair => strongRegions.Contains(pair.Key))
			.Select(pair => pair.Value?.peakBreakoutScore ?? 0f)
			.DefaultIfEmpty(0f)
			.Max();
		LabelTier birthTier = birthTierByLabel.TryGetValue(releaseLabelId, out LabelTier observedBirthTier)
			? observedBirthTier
			: firstTier;
		GameDate date = TimeManager.Instance?.CurrentDate ?? new GameDate(1960, 1, 1);
		DistributionDeal deal = capabilityLabel?.activeDeal;
		float points = ChartSimulator.CalculateChartPoints(record, regions);
		firstChartEventWriter.WriteLine(string.Join(",", new[] {
			currentAuditWeek.ToString(CultureInfo.InvariantCulture),
			date.year.ToString(CultureInfo.InvariantCulture),
			Csv(date.ToString()),
			Csv(leftCensoredAtRunStart ? "RunStartChart" : "LiveFirstObserved"),
			leftCensoredAtRunStart ? "true" : "false",
			Csv(record.baseRecord.recordId),
			Csv(record.baseRecord.title),
			Csv(releaseLabelId),
			Csv(currentOwnerId),
			Csv(releaseLabel?.labelName ?? currentOwner?.labelName),
			Csv((releaseLabel?.populationOrigin ?? LabelPopulationOrigin.Unspecified).ToString()),
			(releaseLabel?.runtimeBirthWeek ?? 0).ToString(CultureInfo.InvariantCulture),
			Csv(birthTier.ToString()),
			Csv(firstTier.ToString()),
			Csv((releaseLabel?.status ?? currentOwner?.status ?? LabelStatus.Stable).ToString()),
			releaseLabel?.isHistorical == true ? "true" : "false",
			record.weeksSinceRelease.ToString(CultureInfo.InvariantCulture),
			record.currentPosition.ToString(CultureInfo.InvariantCulture),
			record.unitsThisWeek.ToString(CultureInfo.InvariantCulture),
			F(points),
			F(publishedCutoff),
			F(record.GetQuality()),
			F(record.peakRegionalBreakoutStrength),
			F(bestStrongPeak),
			record.regionalBreakoutCount.ToString(CultureInfo.InvariantCulture),
			record.coveredRegionCount.ToString(CultureInfo.InvariantCulture),
			signedDealCountByLabel.GetValueOrDefault(releaseLabelId).ToString(CultureInfo.InvariantCulture),
			completedDealCountByLabel.GetValueOrDefault(releaseLabelId).ToString(CultureInfo.InvariantCulture),
			deal != null ? "true" : "false",
			Csv(deal?.origin.ToString()),
			(deal?.signedWeek ?? 0).ToString(CultureInfo.InvariantCulture),
			F(capabilityLabel?.nationalReach ?? 0f),
			F(capabilityLabel?.borrowedReach ?? 0f),
			F(capabilityLabel?.effectiveNationalReach ?? 0f),
			F(capabilityLabel?.ownedReach ?? 0f),
			F(capabilityLabel?.distributionStrength ?? 0f),
			(capabilityLabel?.distributionRegions?.Distinct(StringComparer.Ordinal).Count() ?? 0).ToString(CultureInfo.InvariantCulture),
			(deal?.grantedRegions?.Distinct(StringComparer.Ordinal).Count() ?? 0).ToString(CultureInfo.InvariantCulture),
			F(record.initialLaunchAwareness),
			record.initialLaunchStock.ToString(CultureInfo.InvariantCulture)
		}));
	}

	private void WriteConcentrationYear() {
		if (concentrationYear == 0 || annualChartUnitsByLabel.Count == 0 || concentrationWriter == null) return;
		var rolledUp = new Dictionary<string, long>(StringComparer.Ordinal);
		foreach (var pair in annualChartUnitsByLabel) {
			string ownerId = ResolveCurrentOwner(pair.Key);
			rolledUp[ownerId] = rolledUp.GetValueOrDefault(ownerId) + pair.Value;
		}
		long total = rolledUp.Values.Sum();
		long indieUnits = rolledUp.Sum(pair => IsIndieFamily(CompetitorManager.Instance.GetLabel(pair.Key)) ? pair.Value : 0L);
		long majorUnits = total - indieUnits;
		long[] ranked = rolledUp.Values.OrderByDescending(value => value).ToArray();
		var firmsByTier = rolledUp.Keys
			.Select(labelId => CompetitorManager.Instance.GetLabel(labelId)?.tier)
			.Where(tier => tier.HasValue)
			.GroupBy(tier => tier.Value)
			.ToDictionary(group => group.Key, group => group.Count());
		var cumulativeFirmsByTier = firstChartTierByLabel.Values
			.GroupBy(tier => tier)
			.ToDictionary(group => group.Key, group => group.Count());
		float c4 = total > 0 ? (float)ranked.Take(4).Sum() / total : 0f;
		float c8 = total > 0 ? (float)ranked.Take(8).Sum() / total : 0f;
		var (ownerMajorEntries, ownerMajorFamilyEntries) = CountOwnerFamilyEntries(annualChartEntryOwnerByRecord);
		var (ownerMajorTop40Entries, ownerMajorFamilyTop40Entries) = CountOwnerFamilyEntries(annualTop40OwnerByRecord);
		concentrationWriter.WriteLine(string.Join(",", new[] {
			concentrationYear.ToString(CultureInfo.InvariantCulture), F(c4), F(c8), rolledUp.Count.ToString(CultureInfo.InvariantCulture),
			F(total > 0 ? (float)indieUnits / total : 0f), F(total > 0 ? (float)majorUnits / total : 0f), total.ToString(CultureInfo.InvariantCulture),
			firmsByTier.GetValueOrDefault(LabelTier.Small).ToString(CultureInfo.InvariantCulture),
			firmsByTier.GetValueOrDefault(LabelTier.Boutique).ToString(CultureInfo.InvariantCulture),
			firmsByTier.GetValueOrDefault(LabelTier.Independent).ToString(CultureInfo.InvariantCulture),
			firmsByTier.GetValueOrDefault(LabelTier.MidTier).ToString(CultureInfo.InvariantCulture),
			firmsByTier.GetValueOrDefault(LabelTier.Major).ToString(CultureInfo.InvariantCulture),
			cumulativeChartingLabelIds.Count.ToString(CultureInfo.InvariantCulture),
			cumulativeFirmsByTier.GetValueOrDefault(LabelTier.Small).ToString(CultureInfo.InvariantCulture),
			cumulativeFirmsByTier.GetValueOrDefault(LabelTier.Boutique).ToString(CultureInfo.InvariantCulture),
			cumulativeFirmsByTier.GetValueOrDefault(LabelTier.Independent).ToString(CultureInfo.InvariantCulture),
			cumulativeFirmsByTier.GetValueOrDefault(LabelTier.MidTier).ToString(CultureInfo.InvariantCulture),
			cumulativeFirmsByTier.GetValueOrDefault(LabelTier.Major).ToString(CultureInfo.InvariantCulture),
			cumulativeChartingLabelNames.Count.ToString(CultureInfo.InvariantCulture),
			annualChartEntryTierByRecord.Count.ToString(CultureInfo.InvariantCulture),
			CountEntriesAtTier(annualChartEntryTierByRecord, LabelTier.Small),
			CountEntriesAtTier(annualChartEntryTierByRecord, LabelTier.Boutique),
			CountEntriesAtTier(annualChartEntryTierByRecord, LabelTier.Independent),
			CountEntriesAtTier(annualChartEntryTierByRecord, LabelTier.MidTier),
			CountEntriesAtTier(annualChartEntryTierByRecord, LabelTier.Major),
			annualTop40TierByRecord.Count.ToString(CultureInfo.InvariantCulture),
			CountEntriesAtTier(annualTop40TierByRecord, LabelTier.Small),
			CountEntriesAtTier(annualTop40TierByRecord, LabelTier.Boutique),
			CountEntriesAtTier(annualTop40TierByRecord, LabelTier.Independent),
			CountEntriesAtTier(annualTop40TierByRecord, LabelTier.MidTier),
			CountEntriesAtTier(annualTop40TierByRecord, LabelTier.Major),
			ownerMajorEntries.ToString(CultureInfo.InvariantCulture),
			ownerMajorFamilyEntries.ToString(CultureInfo.InvariantCulture),
			ownerMajorTop40Entries.ToString(CultureInfo.InvariantCulture),
			ownerMajorFamilyTop40Entries.ToString(CultureInfo.InvariantCulture)
		}));
	}

	private static string CountEntriesAtTier(Dictionary<string, LabelTier> entriesByRecord, LabelTier tier) =>
		entriesByRecord.Values.Count(value => value == tier).ToString(CultureInfo.InvariantCulture);

	// Resolve each distinct entry's current owner through the acquisition chain and
	// bucket by owner tier. ownerMajor counts records a Major distributes -- the
	// historically grounded "major-distributed" share the 45-52% consolidation
	// target attaches to; ownerMajorFamily additionally counts MidTier, matching the
	// Major+MidTier grouping of majorFamilyChartShare. An absorbed independent's
	// record thus leaves the Independent imprint bucket for the owner-Major bucket
	// while its immutable release imprint still counts once for cumulative breadth.
	private (int ownerMajor, int ownerMajorFamily) CountOwnerFamilyEntries(Dictionary<string, string> ownerByRecord) {
		int ownerMajor = 0, ownerMajorFamily = 0;
		foreach (var pair in ownerByRecord) {
			AILabel owner = CompetitorManager.Instance.GetLabel(ResolveCurrentOwner(pair.Value));
			if (owner == null) continue;
			bool major = owner.tier == LabelTier.Major;
			bool family = !IsIndieFamily(owner);
			// Control-based ownership (section 27.1): the historical late-60s consolidation wave was
			// mostly majors gaining CONTROL of independents through distribution deals in which the
			// major owned the masters -- not outright buyouts. Owning the masters is owning the
			// record, so a record a Major distributes under a master-owning deal counts as
			// Major-owned even without a formal acquisition. This is what lets consolidation move the
			// entry-share metric, which absorbing individually low-volume small labels cannot.
			if (!major && IsMajorMasterControlled(pair.Value, pair.Key)) { major = true; family = true; }
			if (major) ownerMajor++;
			if (family) ownerMajorFamily++;
		}
		return (ownerMajor, ownerMajorFamily);
	}

	// The record's operating label (release imprint) holds the distribution deal. It is
	// Major-controlled when that deal is active, owns the masters, covers this specific record
	// (per-song scope, section 11), and the distributor is a Major.
	private bool IsMajorMasterControlled(string operatingLabelId, string recordId) {
		AILabel label = CompetitorManager.Instance.GetLabel(operatingLabelId);
		DistributionDeal deal = label?.activeDeal;
		if (deal == null || !deal.ownsMasters || !label.RecordCoveredByActiveDeal(recordId)) return false;
		AILabel distributor = CompetitorManager.Instance.GetLabel(deal.distributorId);
		return distributor != null && distributor.tier == LabelTier.Major;
	}

	private string ResolveCurrentOwner(string labelId) {
		var visited = new HashSet<string>(StringComparer.Ordinal);
		while (acquiredBy.TryGetValue(labelId, out string ownerId) && visited.Add(labelId)) labelId = ownerId;
		return labelId;
	}

	private static bool IsIndieFamily(AILabel label) => label != null &&
		(label.tier == LabelTier.Independent || label.tier == LabelTier.Boutique || label.tier == LabelTier.Small);

	private void WriteLabelFinanceRows(int week, int year) {
		foreach (AILabel label in CompetitorManager.Instance.GetAllLabels().OrderBy(label => label.labelId, StringComparer.Ordinal)) {
			labelFinanceWriter.WriteLine(string.Join(",", new[] {
				week.ToString(CultureInfo.InvariantCulture), year.ToString(CultureInfo.InvariantCulture), Csv(label.labelId),
				Csv(label.labelName), Csv(label.archetype.ToString()), label.isHistorical ? "true" : "false",
				Csv(label.tier.ToString()), Csv(label.status.ToString()), F(label.cashReserves), F(label.monthlyRevenue),
				F(label.monthlyExpenses), F(label.weeklyGrossRevenue), F(label.weeklyCogs),
				F(label.weeklyDistributionSkim), F(label.weeklyArtistRoyalty), F(label.weeklyNetRevenue),
				F(label.weeklyDistributionIncome), F(label.ownedReach), F(label.borrowedReach), F(label.nationalReach), F(label.CalculateCapabilityScore()),
				Csv(label.activeDeal?.distributorId), F(label.activeDeal?.unrecoupedAdvance ?? 0f),
				F(label.outstandingWholesaleReceivables), F(label.lifetimeWholesaleWriteOffs),
				label.independentDistributionRegions.Count.ToString(CultureInfo.InvariantCulture)
			}));
		}
	}

	private void WriteTierVolumeRows(int week, List<RecordRuntimeData> records) {
		foreach (var group in records
			.Where(record => !record.baseRecord.isPlayerOwned)
			.GroupBy(record => ChartManager.Instance.GetLabelById(record.baseRecord.labelId)?.tier.ToString() ?? "Unknown")
			.OrderBy(group => group.Key, StringComparer.Ordinal)) {
			RecordRuntimeData[] launch = group.Where(record => record.weeksSinceRelease <= 3).ToArray();
			RecordRuntimeData[] middle = group.Where(record => record.weeksSinceRelease >= 4 && record.weeksSinceRelease <= 8).ToArray();
			RecordRuntimeData[] tail = group.Where(record => record.weeksSinceRelease > 8).ToArray();
			tierVolumeWriter.WriteLine(string.Join(",", new[] {
				week.ToString(CultureInfo.InvariantCulture), Csv(group.Key),
				launch.Length.ToString(CultureInfo.InvariantCulture), launch.Sum(record => record.unitsThisWeek).ToString(CultureInfo.InvariantCulture),
				middle.Length.ToString(CultureInfo.InvariantCulture), middle.Sum(record => record.unitsThisWeek).ToString(CultureInfo.InvariantCulture),
				tail.Length.ToString(CultureInfo.InvariantCulture), tail.Sum(record => record.unitsThisWeek).ToString(CultureInfo.InvariantCulture),
				group.Count().ToString(CultureInfo.InvariantCulture), group.Sum(record => record.unitsThisWeek).ToString(CultureInfo.InvariantCulture)
			}));
		}
	}

	private void WriteMarketRevenueRows(int week, int year, List<RecordRuntimeData> records) {
		AdvanceMarketRevenueYear(year);

		var weekly = new Dictionary<(string Tier, string Format), RevenueRollup>();
		IReadOnlyList<AILabel> labels = CompetitorManager.Instance.GetAllLabels();
		AddLabelRevenue(weekly, ("All", "All"), labels);
		foreach (IGrouping<LabelTier, AILabel> tierGroup in labels.GroupBy(label => label.tier)) {
			AddLabelRevenue(weekly, (tierGroup.Key.ToString(), "All"), tierGroup);
		}

		IReadOnlyDictionary<(string LabelId, ReleaseFormat Format), RevenueTelemetry> formatRevenue =
			CompetitorManager.Instance.GetWeeklyRevenueByLabelAndFormat();
		foreach (var pair in formatRevenue) {
			string tier = CompetitorManager.Instance.GetLabel(pair.Key.LabelId)?.tier.ToString() ?? "Unknown";
			AddFormatRevenue(weekly, (tier, pair.Key.Format.ToString()), pair.Value);
			AddFormatRevenue(weekly, ("All", pair.Key.Format.ToString()), pair.Value);
		}

		ChartManager.CompletedWeekSettlement settlement = GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true
			? ChartManager.Instance.GetLastCompletedWeekSettlement() : null;
		if (settlement?.Entries != null) {
			weekly[("All", "All")].Units = settlement.TotalUnits;
			foreach (ChartManager.CompletedWeekSettlementEntry entry in settlement.Entries) {
				string tier = ChartManager.Instance.GetLabelById(entry.LabelId)?.tier.ToString() ?? "Unknown";
				string format = entry.Format.ToString();
				AddUnits(weekly, (tier, "All"), entry.Units);
				AddUnits(weekly, ("All", format), entry.Units);
				AddUnits(weekly, (tier, format), entry.Units);
			}
		} else {
			weekly[("All", "All")].Units = records.Sum(record => (long)record.unitsThisWeek);
			foreach (RecordRuntimeData record in records) {
				string tier = ChartManager.Instance.GetLabelById(record.baseRecord.labelId)?.tier.ToString() ?? "Unknown";
				string format = record.baseRecord.format.ToString();
				AddUnits(weekly, (tier, "All"), record.unitsThisWeek);
				AddUnits(weekly, ("All", format), record.unitsThisWeek);
				AddUnits(weekly, (tier, format), record.unitsThisWeek);
			}
		}

		foreach (var pair in weekly.OrderBy(pair => pair.Key.Tier, StringComparer.Ordinal)
			.ThenBy(pair => pair.Key.Format, StringComparer.Ordinal)) {
			WriteMarketRevenueRow("weekly", week.ToString(CultureInfo.InvariantCulture), year, pair.Key, pair.Value);
			AccumulateAnnualMarketRevenue(pair.Key, pair.Value);
		}
	}

	private void AdvanceMarketRevenueYear(int year) {
		if (marketRevenueYear == 0) {
			marketRevenueYear = year;
			return;
		}
		if (year != marketRevenueYear) {
			WriteMarketRevenueYear();
			annualMarketRevenue.Clear();
			marketRevenueYear = year;
		}
	}

	private static void AddLabelRevenue(
		Dictionary<(string Tier, string Format), RevenueRollup> rows,
		(string Tier, string Format) key,
		IEnumerable<AILabel> labels) {
		RevenueRollup row = GetRevenueRow(rows, key);
		foreach (AILabel label in labels) {
			row.Gross += label.weeklyGrossRevenue;
			row.LabelNet += label.weeklyNetRevenue;
			row.DistributionIncome += label.weeklyDistributionIncome;
		}
	}

	private static void AddFormatRevenue(
		Dictionary<(string Tier, string Format), RevenueRollup> rows,
		(string Tier, string Format) key,
		RevenueTelemetry telemetry) {
		RevenueRollup row = GetRevenueRow(rows, key);
		row.Gross += telemetry.gross;
		row.LabelNet += telemetry.labelNet;
		row.DistributionIncome += telemetry.distributionIncome;
	}

	private static void AddUnits(
		Dictionary<(string Tier, string Format), RevenueRollup> rows,
		(string Tier, string Format) key,
		int units) => GetRevenueRow(rows, key).Units += units;

	private static RevenueRollup GetRevenueRow(
		Dictionary<(string Tier, string Format), RevenueRollup> rows,
		(string Tier, string Format) key) {
		if (!rows.TryGetValue(key, out RevenueRollup row)) {
			row = new RevenueRollup();
			rows[key] = row;
		}
		return row;
	}

	private void AccumulateAnnualMarketRevenue((string Tier, string Format) key, RevenueRollup weekly) {
		RevenueRollup annual = GetRevenueRow(annualMarketRevenue, key);
		annual.Units += weekly.Units;
		annual.Gross += weekly.Gross;
		annual.LabelNet += weekly.LabelNet;
		annual.DistributionIncome += weekly.DistributionIncome;
	}

	private void WriteMarketRevenueYear() {
		if (marketRevenueYear == 0 || annualMarketRevenue.Count == 0 || marketRevenueWriter == null) return;
		foreach (var pair in annualMarketRevenue.OrderBy(pair => pair.Key.Tier, StringComparer.Ordinal)
			.ThenBy(pair => pair.Key.Format, StringComparer.Ordinal)) {
			WriteMarketRevenueRow("annual", string.Empty, marketRevenueYear, pair.Key, pair.Value);
		}
	}

	private void WriteMarketRevenueRow(
		string period,
		string week,
		int year,
		(string Tier, string Format) key,
		RevenueRollup revenue) {
		marketRevenueWriter.WriteLine(string.Join(",", new[] {
			period, week, year.ToString(CultureInfo.InvariantCulture), Csv(key.Tier), Csv(key.Format),
			revenue.Units.ToString(CultureInfo.InvariantCulture), F(revenue.Gross), F(revenue.LabelNet),
			F(revenue.DistributionIncome), F(revenue.LabelNet + revenue.DistributionIncome)
		}));
	}

	private void WriteReleaseCapacityRow(int week, int year) {
		CompetitorManager manager = CompetitorManager.Instance;
		int otherFailures = manager.WeeklyFailedReleaseRolls - manager.WeeklyCooldownMismatchRolls;
		float failedRate = manager.WeeklyReleaseRollsFired > 0
			? (float)manager.WeeklyFailedReleaseRolls / manager.WeeklyReleaseRollsFired : 0f;
		float mismatchRate = manager.WeeklyReleaseRollsFired > 0
			? (float)manager.WeeklyCooldownMismatchRolls / manager.WeeklyReleaseRollsFired : 0f;
		releaseCapacityWriter.WriteLine(string.Join(",", new[] {
			week.ToString(CultureInfo.InvariantCulture), year.ToString(CultureInfo.InvariantCulture),
			manager.WeeklyReleaseRollsFired.ToString(CultureInfo.InvariantCulture),
			manager.WeeklySuccessfulReleases.ToString(CultureInfo.InvariantCulture),
			manager.WeeklyFailedReleaseRolls.ToString(CultureInfo.InvariantCulture),
			manager.WeeklyCooldownMismatchRolls.ToString(CultureInfo.InvariantCulture),
			otherFailures.ToString(CultureInfo.InvariantCulture), F(failedRate), F(mismatchRate)
		}));
	}

	private void WriteAlbumRows(int week, GameDate date, List<RecordRuntimeData> records, List<RecordRuntimeData> albumChart) {
		int chartSize = ChartManager.Instance.GetAlbumChartSize(date);
		foreach (RecordRuntimeData record in albumChart) {
			Album album = record.baseRecord.album;
			albumChartWriter.WriteLine(string.Join(",", new[] {
				week.ToString(CultureInfo.InvariantCulture), date.year.ToString(CultureInfo.InvariantCulture), date.month.ToString(CultureInfo.InvariantCulture),
				chartSize.ToString(CultureInfo.InvariantCulture), record.currentPosition.ToString(CultureInfo.InvariantCulture), record.lastWeekPosition.ToString(CultureInfo.InvariantCulture),
				Csv(record.baseRecord.recordId), Csv(record.baseRecord.title), Csv(record.baseRecord.artistId), Csv(record.baseRecord.labelId), Csv(record.baseRecord.primaryGenre.ToString()),
				Csv(album?.albumFormat.ToString()), record.unitsThisWeek.ToString(CultureInfo.InvariantCulture), record.totalUnitsSold.ToString(CultureInfo.InvariantCulture),
				record.weeksOnChart.ToString(CultureInfo.InvariantCulture), F(album?.pooledAppeal ?? 0f), F(album?.thematicCohesion ?? 0f), F(album?.packaging ?? 0f)
			}));
		}

		foreach (RecordRuntimeData record in records.Where(record => record.baseRecord.format == ReleaseFormat.Album)) {
			Album album = record.baseRecord.album;
			if (album == null || !observedAlbumIds.Add(album.albumId)) continue;
			int reused = album.trackRefs?.Length ?? 0;
			int originals = album.nonSingleTracks?.Length ?? 0;
			int total = reused + originals;
			albumCompositionWriter.WriteLine(string.Join(",", new[] {
				week.ToString(CultureInfo.InvariantCulture), date.year.ToString(CultureInfo.InvariantCulture), Csv(record.baseRecord.recordId), Csv(record.baseRecord.artistId),
				Csv(record.baseRecord.primaryGenre.ToString()), Csv(album.albumFormat.ToString()), F(album.thematicCohesion), F(album.pooledAppeal),
				total.ToString(CultureInfo.InvariantCulture), reused.ToString(CultureInfo.InvariantCulture), originals.ToString(CultureInfo.InvariantCulture),
				F(total > 0 ? (float)reused / total : 0f), F(album.runtimeMinutes), F(album.packaging), album.isStereo ? "true" : "false"
			}));
			AlbumTrack[] trackRefs = album.trackRefs ?? Array.Empty<AlbumTrack>();
			if (album.albumFormat == AlbumFormat.Compilation) {
				EnsureDecadeAnnualYear(date.year);
				decadeAnnual.CompilationAlbums++;
				decadeAnnual.CompilationTrackRefs += trackRefs.Length;
			}
			for (int trackIndex = 0; trackIndex < trackRefs.Length; trackIndex++) {
				AlbumTrack track = trackRefs[trackIndex];
				float freshness = trackIndex < (album.trackRefFreshnessApplied?.Length ?? 0)
					? album.trackRefFreshnessApplied[trackIndex] : 1f;
				int timesCompUsed = trackIndex < (album.trackRefCompUsesAtGeneration?.Length ?? 0)
					? album.trackRefCompUsesAtGeneration[trackIndex] : 0;
				if (album.albumFormat == AlbumFormat.Compilation) {
					decadeAnnual.FreshnessSum += freshness;
					decadeAnnual.FreshnessMin = Mathf.Min(decadeAnnual.FreshnessMin, freshness);
					decadeAnnual.FreshnessMax = Mathf.Max(decadeAnnual.FreshnessMax, freshness);
					if (timesCompUsed <= 0) decadeAnnual.FreshnessUse0++;
					else if (timesCompUsed == 1) decadeAnnual.FreshnessUse1++;
					else if (timesCompUsed == 2) decadeAnnual.FreshnessUse2++;
					else decadeAnnual.FreshnessUse3Plus++;
				}
				albumTrackLinkWriter.WriteLine(string.Join(",", new[] {
					week.ToString(CultureInfo.InvariantCulture), date.year.ToString(CultureInfo.InvariantCulture),
					Csv(record.baseRecord.recordId), Csv(record.baseRecord.artistId), Csv(track.sourceRecordId), F(freshness),
					timesCompUsed.ToString(CultureInfo.InvariantCulture), SourceHitAgeWeeks(track, date)
				}));
			}
		}

		int attempts = ChartManager.Instance.RetiredTrackResolutionAttempts;
		int misses = ChartManager.Instance.RetiredTrackResolutionMisses;
		int archiveHits = ChartManager.Instance.RetiredTrackArchiveHits;
		retiredTrackWriter.WriteLine(string.Join(",", new[] {
			week.ToString(CultureInfo.InvariantCulture), date.year.ToString(CultureInfo.InvariantCulture),
			(attempts - previousTrackResolutionAttempts).ToString(CultureInfo.InvariantCulture),
			(archiveHits - previousTrackArchiveHits).ToString(CultureInfo.InvariantCulture),
			(misses - previousTrackResolutionMisses).ToString(CultureInfo.InvariantCulture),
			attempts.ToString(CultureInfo.InvariantCulture), archiveHits.ToString(CultureInfo.InvariantCulture), misses.ToString(CultureInfo.InvariantCulture)
		}));
		previousTrackResolutionAttempts = attempts;
		previousTrackResolutionMisses = misses;
		previousTrackArchiveHits = archiveHits;
	}

	private void WriteFormatMixRows(int week, int year, List<RecordRuntimeData> records) {
		EnsureDecadeAnnualYear(year);
		var newReleases = records.Where(record => observedReleaseIds.Add(record.baseRecord.recordId)).ToList();
		var releasesByFormat = newReleases.GroupBy(record => record.baseRecord.format.ToString()).ToDictionary(group => group.Key, group => group.Count());
		ChartManager.CompletedWeekSettlement settlement = GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true
			? ChartManager.Instance.GetLastCompletedWeekSettlement() : null;
		var unitsByFormat = settlement?.Entries != null
			? settlement.Entries.GroupBy(entry => entry.Format.ToString()).ToDictionary(group => group.Key, group => (long)group.Sum(entry => entry.Units))
			: records.GroupBy(record => record.baseRecord.format.ToString()).ToDictionary(group => group.Key, group => (long)group.Sum(record => record.unitsThisWeek));
		var revenueByFormat = new Dictionary<string, FormatMixRollup>(StringComparer.Ordinal);
		foreach (var pair in CompetitorManager.Instance.GetWeeklyRevenueByLabelAndFormat()) {
			string format = pair.Key.Format.ToString();
			if (!revenueByFormat.TryGetValue(format, out FormatMixRollup row)) revenueByFormat[format] = row = new FormatMixRollup();
			row.Gross += pair.Value.gross;
			row.Cogs += pair.Value.cogs;
			row.Skim += pair.Value.distributionSkim;
			row.Royalty += pair.Value.artistRoyalty;
			row.LabelNet += pair.Value.labelNet;
		}
		var formats = releasesByFormat.Keys.Concat(unitsByFormat.Keys).Concat(revenueByFormat.Keys).Distinct().OrderBy(value => value, StringComparer.Ordinal).ToList();
		int totalReleases = releasesByFormat.Values.Sum();
		long totalUnits = unitsByFormat.Values.Sum();
		double totalGross = revenueByFormat.Values.Sum(row => row.Gross);
		foreach (string format in formats) {
			int releases = releasesByFormat.GetValueOrDefault(format);
			long units = unitsByFormat.GetValueOrDefault(format);
			FormatMixRollup revenue = revenueByFormat.GetValueOrDefault(format) ?? new FormatMixRollup();
			WriteFormatMixRow("weekly", week.ToString(CultureInfo.InvariantCulture), year, format, releases, totalReleases, units, totalUnits, revenue, totalGross);
			var key = (year, format);
			if (!annualFormatMix.TryGetValue(key, out FormatMixRollup annual)) annualFormatMix[key] = annual = new FormatMixRollup();
			annual.Releases += releases;
			annual.Units += units;
			annual.Gross += revenue.Gross;
			annual.Cogs += revenue.Cogs;
			annual.Skim += revenue.Skim;
			annual.Royalty += revenue.Royalty;
			annual.LabelNet += revenue.LabelNet;
			FormatMixRollup decadeFormat = format == ReleaseFormat.Single.ToString() ? decadeAnnual.Single
				: format == ReleaseFormat.Album.ToString() ? decadeAnnual.Album : null;
			if (decadeFormat != null) {
				decadeFormat.Releases += releases;
				decadeFormat.Units += units;
				decadeFormat.Gross += revenue.Gross;
				decadeFormat.Cogs += revenue.Cogs;
				decadeFormat.Skim += revenue.Skim;
				decadeFormat.Royalty += revenue.Royalty;
				decadeFormat.LabelNet += revenue.LabelNet;
			}
		}
	}

	private void EnsureDecadeAnnualYear(int year) {
		if (decadeAnnualYear == 0) {
			decadeAnnualYear = year;
			return;
		}
		if (year == decadeAnnualYear) return;
		WriteDecadeAnnualYear();
		FlushAnnualStreams();
		decadeAnnual = new DecadeAnnualRollup();
		decadeAnnualYear = year;
	}

	private void WriteDecadeAnnualYear() {
		if (decadeAnnualYear == 0 || decadeAnnualRollupWriter == null) return;
		double singleNet = decadeAnnual.Single.LabelNet;
		double albumNet = decadeAnnual.Album.LabelNet;
		List<int> albumAges = decadeAnnual.AlbumAges;
		List<int> albumUnits = decadeAnnual.AlbumUnits;
		// ClosedTop40Weeks accumulates in closure order; Statistic requires a sorted list.
		List<int> closedTop40Weeks = decadeAnnual.ClosedTop40Weeks.OrderBy(value => value).ToList();
		int albumsBelowFloor = albumUnits.Count(value => value < 10);
		int albumsAtOrAboveFloor = albumUnits.Count - albumsBelowFloor;
		double? pearson = Correlation(decadeAnnual.ChartingSingles.Values.Select(value => (value.Quality, 101d - value.BestPosition)));
		decadeAnnualRollupWriter.WriteLine(string.Join(",", new[] {
			(requestedSeed?.ToString(CultureInfo.InvariantCulture) ?? string.Empty),
			decadeAnnualYear.ToString(CultureInfo.InvariantCulture),
			decadeAnnual.Single.Units.ToString(CultureInfo.InvariantCulture), F(decadeAnnual.Single.Gross), F(singleNet),
			decadeAnnual.Album.Units.ToString(CultureInfo.InvariantCulture), F(decadeAnnual.Album.Gross), F(albumNet),
			Ratio(decadeAnnual.Album.Gross, decadeAnnual.Single.Gross),
			Ratio(decadeAnnual.AlbumUnitsOver26Weeks, decadeAnnual.Album.Units),
			Ratio(decadeAnnual.AlbumUnitsOver52Weeks, decadeAnnual.Album.Units),
			decadeAnnual.Decisions.ToString(CultureInfo.InvariantCulture), Ratio(decadeAnnual.AlbumDecisions, decadeAnnual.Decisions),
			decadeAnnual.AdultDecisions.ToString(CultureInfo.InvariantCulture), Ratio(decadeAnnual.AdultAlbumDecisions, decadeAnnual.AdultDecisions),
			decadeAnnual.YouthDecisions.ToString(CultureInfo.InvariantCulture), Ratio(decadeAnnual.YouthAlbumDecisions, decadeAnnual.YouthDecisions),
			Ratio(decadeAnnual.OrphanDecisions, decadeAnnual.Decisions), Ratio(decadeAnnual.PromoDecisions, decadeAnnual.Decisions),
			Ratio(decadeAnnual.StandaloneDecisions, decadeAnnual.Decisions),
			Ratio(decadeAnnual.SingleConfidence, decadeAnnual.SingleConfidenceCount), Ratio(decadeAnnual.AlbumConfidence, decadeAnnual.AlbumConfidenceCount),
			decadeAnnual.CompilationAlbums.ToString(CultureInfo.InvariantCulture), decadeAnnual.CompilationTrackRefs.ToString(CultureInfo.InvariantCulture),
			decadeAnnual.FreshnessUse0.ToString(CultureInfo.InvariantCulture), decadeAnnual.FreshnessUse1.ToString(CultureInfo.InvariantCulture),
			decadeAnnual.FreshnessUse2.ToString(CultureInfo.InvariantCulture), decadeAnnual.FreshnessUse3Plus.ToString(CultureInfo.InvariantCulture),
			Ratio(decadeAnnual.FreshnessSum, decadeAnnual.CompilationTrackRefs),
			decadeAnnual.CompilationTrackRefs > 0 ? F(decadeAnnual.FreshnessMin) : string.Empty,
			decadeAnnual.CompilationTrackRefs > 0 ? F(decadeAnnual.FreshnessMax) : string.Empty,
			Ratio(decadeAnnual.SingleMemoryEma, decadeAnnual.SingleMemoryLabels), decadeAnnual.SingleMemoryN.ToString(CultureInfo.InvariantCulture),
			Ratio(decadeAnnual.AlbumMemoryEma, decadeAnnual.AlbumMemoryLabels), decadeAnnual.AlbumMemoryN.ToString(CultureInfo.InvariantCulture),
			decadeAnnual.CompletedMatched.ToString(CultureInfo.InvariantCulture),
			Ratio(decadeAnnual.CompletedExpected, decadeAnnual.CompletedMatched), Ratio(decadeAnnual.CompletedRealized, decadeAnnual.CompletedMatched),
			Ratio(decadeAnnual.CompletedExpected - decadeAnnual.CompletedRealized, decadeAnnual.CompletedMatched),
			decadeAnnual.YouthCompCompleted.ToString(CultureInfo.InvariantCulture), Ratio(decadeAnnual.YouthCompExpected, decadeAnnual.YouthCompCompleted),
			Ratio(decadeAnnual.YouthCompRealized, decadeAnnual.YouthCompCompleted),
			Ratio(decadeAnnual.YouthCompExpected - decadeAnnual.YouthCompRealized, decadeAnnual.YouthCompCompleted),
			decadeAnnual.PromoCompleted.ToString(CultureInfo.InvariantCulture), Ratio(decadeAnnual.PromoExpected, decadeAnnual.PromoCompleted),
			Ratio(decadeAnnual.PromoRealized, decadeAnnual.PromoCompleted),
			Ratio(decadeAnnual.PromoExpected - decadeAnnual.PromoRealized, decadeAnnual.PromoCompleted),
			pearson.HasValue ? F(pearson.Value) : string.Empty,
			decadeAnnual.ChartingSingles.Count.ToString(CultureInfo.InvariantCulture),
			Statistic(closedTop40Weeks, 0.5), closedTop40Weeks.Count.ToString(CultureInfo.InvariantCulture),
			decadeAnnual.ActiveSingles.ToString(CultureInfo.InvariantCulture), decadeAnnual.ActiveAlbums.ToString(CultureInfo.InvariantCulture),
			Statistic(albumAges, 0.5), Statistic(albumAges, 0.9),
			Statistic(albumUnits, 0), Statistic(albumUnits, 0.25), Statistic(albumUnits, 0.5),
			Statistic(albumUnits, 0.75), Statistic(albumUnits, 0.9), Statistic(albumUnits, 1),
			albumsBelowFloor.ToString(CultureInfo.InvariantCulture), albumsAtOrAboveFloor.ToString(CultureInfo.InvariantCulture),
			Ratio(albumsBelowFloor, decadeAnnual.ActiveAlbums), decadeAnnual.AlbumsEverReleased.ToString(CultureInfo.InvariantCulture),
			decadeAnnual.AlbumsRetired.ToString(CultureInfo.InvariantCulture),
			Ratio(decadeAnnual.AlbumsEverReleased - decadeAnnual.AlbumsRetired, decadeAnnual.AlbumsEverReleased)
		}));
	}

	private static string SourceHitAgeWeeks(AlbumTrack track, GameDate currentDate) {
		GameDate releaseDate = track.releaseDate;
		if (releaseDate.year <= 0 && !string.IsNullOrEmpty(track.sourceRecordId) &&
			ChartManager.Instance.TryGetTrackSnapshot(track.sourceRecordId, out AlbumTrack currentSnapshot)) {
			releaseDate = currentSnapshot.releaseDate;
		}
		return releaseDate.year > 0
			? currentDate.WeeksDifference(releaseDate).ToString(CultureInfo.InvariantCulture)
			: string.Empty;
	}

	private void CaptureRetirementCohortSnapshot(List<RecordRuntimeData> records) {
		decadeAnnual.ActiveSingles = records.Count(record => record.baseRecord.format == ReleaseFormat.Single);
		var albums = records.Where(record => record.baseRecord.format == ReleaseFormat.Album).ToList();
		decadeAnnual.ActiveAlbums = albums.Count;
		decadeAnnual.AlbumAges = albums.Select(record => record.weeksSinceRelease).OrderBy(value => value).ToList();
		decadeAnnual.AlbumUnits = albums.Select(record => record.unitsThisWeek).OrderBy(value => value).ToList();
		decadeAnnual.AlbumsEverReleased = observedAlbumIds.Count;
		decadeAnnual.AlbumsRetired = retiredAlbumIds.Count;
	}

	private static string Ratio(double numerator, double denominator) => denominator != 0d ? F(numerator / denominator) : string.Empty;
	private static string Statistic(IReadOnlyList<int> sorted, double quantile) {
		if (sorted.Count == 0) return string.Empty;
		double position = Math.Clamp(quantile, 0d, 1d) * (sorted.Count - 1);
		int lower = (int)Math.Floor(position);
		int upper = (int)Math.Ceiling(position);
		double value = sorted[lower] + (sorted[upper] - sorted[lower]) * (position - lower);
		return F(value);
	}
	private static double? Correlation(IEnumerable<(double X, double Y)> values) {
		var pairs = values.ToList();
		if (pairs.Count < 2) return null;
		double meanX = pairs.Average(pair => pair.X);
		double meanY = pairs.Average(pair => pair.Y);
		double covariance = 0d;
		double varianceX = 0d;
		double varianceY = 0d;
		foreach (var pair in pairs) {
			double dx = pair.X - meanX;
			double dy = pair.Y - meanY;
			covariance += dx * dy;
			varianceX += dx * dx;
			varianceY += dy * dy;
		}
		return varianceX > 0d && varianceY > 0d ? covariance / Math.Sqrt(varianceX * varianceY) : null;
	}

	private void WritePerformanceYear(int year, double wallSeconds) {
		if (performanceProfileWriter == null) return;
		SimulationPerformanceProfiler.Snapshot profile = SimulationPerformanceProfiler.TakeSnapshotAndReset();
		List<RecordRuntimeData> liveRecords = ChartManager.Instance.GetAllRecords();
		LiveRecordInertness inertness = MeasureLiveRecordInertness(liveRecords);
		string N(int value) => value.ToString(CultureInfo.InvariantCulture);
		performanceProfileWriter.WriteLine(string.Join(",", new[] {
			requestedSeed?.ToString(CultureInfo.InvariantCulture) ?? string.Empty, year.ToString(CultureInfo.InvariantCulture), F(wallSeconds),
			N(liveRecords.Count), F(profile.SimulateWeekSeconds),
			F(profile.CalculateLabelRevenueSeconds), F(profile.RecordLookupSeconds), F(profile.RevenueArithmeticSeconds),
			F(profile.AlbumUpdateSeconds), F(profile.DueAlbumProjectsSeconds), F(profile.CaptureWeekSeconds),
			profile.RecordLookups.ToString(CultureInfo.InvariantCulture),
			F(profile.FreezeSettlementSeconds), F(profile.BookSettlementSeconds), F(profile.SettlementAuditEventSeconds),
			F(profile.GenreMomentumSeconds), F(profile.CullDeadRecordsSeconds), F(profile.PopulationLifecycleSeconds),
			F(profile.CompetitorWeekSeconds), F(profile.RosterWeekSeconds), F(profile.DailyTalentMarketSeconds),
			F(profile.LabelLifecycleMonthSeconds),
			N(inertness.Albums), N(inertness.Singles), N(inertness.AlbumsOffChart), N(inertness.AlbumsZeroStock),
			N(inertness.AlbumsZeroAwareness), N(inertness.AlbumsZeroUnits), N(inertness.InertAlbums), N(inertness.InertSingles)
		}));
	}

	private readonly record struct LiveRecordInertness(int Albums, int Singles, int AlbumsOffChart,
		int AlbumsZeroStock, int AlbumsZeroAwareness, int AlbumsZeroUnits, int InertAlbums, int InertSingles);

	/// <summary>
	/// Handoff 35.4 diagnostic. An inert record is off-chart, sold nothing this week, holds no
	/// stock in any region and has no awareness left to convert -- it cannot influence any future
	/// week yet is still walked by every per-record pass. Read-only; nothing here mutates state.
	/// </summary>
	private static LiveRecordInertness MeasureLiveRecordInertness(List<RecordRuntimeData> records) {
		int albums = 0, singles = 0, albumsOffChart = 0, albumsZeroStock = 0;
		int albumsZeroAwareness = 0, albumsZeroUnits = 0, inertAlbums = 0, inertSingles = 0;
		foreach (RecordRuntimeData record in records ?? new List<RecordRuntimeData>()) {
			bool isAlbum = record.baseRecord.format == ReleaseFormat.Album;
			if (isAlbum) albums++; else singles++;
			int stock = 0;
			float regionalAwareness = 0f;
			foreach (KeyValuePair<string, RegionalRecordData> pair in record.regionalData) {
				stock += Math.Max(0, pair.Value?.unitsInStores ?? 0);
				regionalAwareness = Math.Max(regionalAwareness, pair.Value?.awareness ?? 0f);
			}
			bool offChart = record.currentPosition == 0;
			bool noStock = stock == 0;
			bool noAwareness = Math.Max(record.awareness, regionalAwareness) < InertAwarenessFloor;
			bool noUnits = record.unitsThisWeek == 0;
			if (isAlbum) {
				if (offChart) albumsOffChart++;
				if (noStock) albumsZeroStock++;
				if (noAwareness) albumsZeroAwareness++;
				if (noUnits) albumsZeroUnits++;
			}
			if (!(offChart && noStock && noAwareness && noUnits)) continue;
			if (isAlbum) inertAlbums++; else inertSingles++;
		}
		return new LiveRecordInertness(albums, singles, albumsOffChart, albumsZeroStock,
			albumsZeroAwareness, albumsZeroUnits, inertAlbums, inertSingles);
	}

	private const float InertAwarenessFloor = 0.001f;

	private void FlushAnnualStreams() {
		decadeAnnualRollupWriter?.Flush();
		performanceProfileWriter?.Flush();
		weekWriter?.Flush();
		lifecycleWriter?.Flush();
		concentrationWriter?.Flush();
		marketRevenueWriter?.Flush();
		marketClearingWriter?.Flush();
		marketSpilloverWriter?.Flush();
		completedWeekSettlementWriter?.Flush();
		completedWeekSettlementRegionalWriter?.Flush();
		albumRealizationBridgeWriter?.Flush();
		formatMemoryRevisionWriter?.Flush();
		formatMemoryAdjustmentWriter?.Flush();
		releaseCapacityWriter?.Flush();
		formatMixWriter?.Flush();
		albumProjectWeeklyWriter?.Flush();
		cityRosterWriter?.Flush();
		distanceMatrixWriter?.Flush();
		labelGeographyWriter?.Flush();
		geographyMetricsWriter?.Flush();
		dealMetricsWriter?.Flush();
		genreMarketWeeklyWriter?.Flush();
		recordGenreExplanationWriter?.Flush();
		albumDemandExplanationWriter?.Flush();
		formatDecisionExplanationWriter?.Flush();
		formatDecisionCohortWriter?.Flush();
		formatDecisionCohortDetailWriter?.Flush();
		supplySelectionWriter?.Flush();
		traditionalPopFallbackWriter?.Flush();
		genreShapeWriter?.Flush();
	}

	private void WriteAnnualFormatMixRows() {
		foreach (var yearGroup in annualFormatMix.GroupBy(pair => pair.Key.Year).OrderBy(group => group.Key)) {
			int totalReleases = yearGroup.Sum(pair => pair.Value.Releases);
			long totalUnits = yearGroup.Sum(pair => pair.Value.Units);
			double totalGross = yearGroup.Sum(pair => pair.Value.Gross);
			foreach (var pair in yearGroup.OrderBy(pair => pair.Key.Format, StringComparer.Ordinal)) {
				WriteFormatMixRow("annual", string.Empty, yearGroup.Key, pair.Key.Format, pair.Value.Releases, totalReleases, pair.Value.Units, totalUnits, pair.Value, totalGross);
			}
		}
	}

	private void WriteFormatMixRow(string period, string week, int year, string format, int releases, int totalReleases, long units, long totalUnits, FormatMixRollup row, double totalGross) {
		formatMixWriter.WriteLine(string.Join(",", new[] {
			period, week, year.ToString(CultureInfo.InvariantCulture), Csv(format), releases.ToString(CultureInfo.InvariantCulture),
			F(totalReleases > 0 ? (double)releases / totalReleases : 0d), units.ToString(CultureInfo.InvariantCulture),
			F(totalUnits > 0 ? (double)units / totalUnits : 0d), F(row.Gross), F(totalGross > 0d ? row.Gross / totalGross : 0d),
			F(row.Cogs), F(row.Skim), F(row.Royalty), F(row.LabelNet)
		}));
	}

	private LifecycleState ObserveRecord(RecordRuntimeData record, bool wasPresentAtStart) {
		string id = record.baseRecord.recordId;
		if (lifecycle.TryGetValue(id, out LifecycleState state)) return state;

		state = new LifecycleState {
			Record = record,
			DebutPosition = record.currentPosition,
			WasPresentAtStart = wasPresentAtStart
		};
		lifecycle[id] = state;
		return state;
	}

	private void WriteRecordRow(int week, int year, RecordRuntimeData record, float chartCutoff) {
		AILabel label = ChartManager.Instance.GetLabelById(record.baseRecord.labelId);
		float chartPoints = ChartSimulator.CalculateChartPoints(record, regions);
		recordWriter.WriteLine(string.Join(",", new[] {
			week.ToString(CultureInfo.InvariantCulture),
			year.ToString(CultureInfo.InvariantCulture),
			Csv(record.baseRecord.recordId),
			Csv(record.baseRecord.title),
			Csv(record.baseRecord.artistId),
			Csv(record.baseRecord.labelId),
			Csv(label?.tier.ToString()),
			record.baseRecord.isPlayerOwned ? "true" : "false",
			Csv(record.baseRecord.primaryGenre.ToString()),
			F(record.GetQuality()),
			record.weeksSinceRelease.ToString(CultureInfo.InvariantCulture),
			record.weeksOnChart.ToString(CultureInfo.InvariantCulture),
			record.currentPosition.ToString(CultureInfo.InvariantCulture),
			record.lastWeekPosition.ToString(CultureInfo.InvariantCulture),
			record.unitsThisWeek.ToString(CultureInfo.InvariantCulture),
			record.totalUnitsSold.ToString(CultureInfo.InvariantCulture),
			F(record.awareness),
			F(record.radioHeat),
			F(record.wordOfMouth),
			F(record.momentum),
			F(record.saturation),
			F(chartPoints),
			F(chartCutoff),
			F(chartPoints - chartCutoff),
			record.regionalBreakoutCount.ToString(CultureInfo.InvariantCulture),
			record.neighboringMarketTestCount.ToString(CultureInfo.InvariantCulture),
			F(record.crossoverCandidateStrength),
			F(record.peakRegionalBreakoutStrength),
			F(record.sustainedSalesVelocity),
			record.unmetRegionalDemand.ToString(CultureInfo.InvariantCulture),
			record.coveredRegionCount.ToString(CultureInfo.InvariantCulture),
			F(record.initialLaunchAwareness),
			record.initialLaunchStock.ToString(CultureInfo.InvariantCulture),
			Csv(record.launchCareerState.ToString()),
			F(record.perceivedQualityMultiplier)
		}));
	}

	private void WriteLifecycleRow(int week, LifecycleState state) {
		RecordRuntimeData record = state.Record;
		lifecycleWriter.WriteLine(string.Join(",", new[] {
			week.ToString(CultureInfo.InvariantCulture),
			Csv(record.baseRecord.recordId),
			Csv(record.baseRecord.title),
			state.DebutPosition.ToString(CultureInfo.InvariantCulture),
			record.peakPosition.ToString(CultureInfo.InvariantCulture),
			record.weeksOnChart.ToString(CultureInfo.InvariantCulture),
			state.WeeksAtNumberOne.ToString(CultureInfo.InvariantCulture),
			record.totalUnitsSold.ToString(CultureInfo.InvariantCulture),
			state.WasPresentAtStart ? "true" : "false"
		}));
	}

	private void WriteBreakoutRows(int week, RecordRuntimeData record) {
		AILabel label = ChartManager.Instance.GetLabelById(record.baseRecord.labelId);
		if (label == null || record.baseRecord.isPlayerOwned) return;

		foreach (MarketRegion region in regions) {
			if (!record.regionalData.TryGetValue(region.regionId, out RegionalRecordData data) ||
				!data.breakoutDiagnosticObserved) continue;

			bool covered = label.HasDistributionInRegion(region.regionId);
			breakoutWriter.WriteLine(string.Join(",", new[] {
				week.ToString(CultureInfo.InvariantCulture), Csv(record.baseRecord.recordId), Csv(label.tier.ToString()),
				Csv(record.launchCareerState.ToString()), Csv(region.regionId), covered ? "true" : "false",
				data.breakoutDiagnosticAge.ToString(CultureInfo.InvariantCulture), data.breakoutWeekStartStock.ToString(CultureInfo.InvariantCulture),
				data.breakoutPreRestockStock.ToString(CultureInfo.InvariantCulture), F(data.breakoutRawSales),
				data.unitsSoldThisWeek.ToString(CultureInfo.InvariantCulture), data.breakoutBackordersBeforeRestock.ToString(CultureInfo.InvariantCulture),
				F(data.breakoutAwareBuyers), F(data.breakoutConversionRate), data.breakoutTriggered ? "true" : "false",
				data.breakoutRequestedRestock.ToString(CultureInfo.InvariantCulture), data.breakoutAppliedRestock.ToString(CultureInfo.InvariantCulture),
				data.breakoutMaxCapacity.ToString(CultureInfo.InvariantCulture), data.breakoutCapacityCapped ? "true" : "false",
				F(data.breakoutScore), Csv(data.breakoutStage.ToString()), data.tractionWeeks.ToString(CultureInfo.InvariantCulture),
				data.sustainedGrowthWeeks.ToString(CultureInfo.InvariantCulture), F(data.salesVelocity), F(data.breakoutVolumeInput),
				F(data.breakoutVelocityInput), F(data.breakoutAudienceInput), F(data.breakoutMediaInput), F(data.breakoutGenreFitInput),
				F(data.breakoutQualityInput), F(data.breakoutUnmetDemandInput), F(data.breakoutVisibilityMultiplier), F(data.breakoutAwarenessGain), F(data.breakoutRadioGain),
				F(data.breakoutWordOfMouthGain), F(data.neighboringMarketTestStrength), Csv(data.breakoutSourceRegionId)
			}));
			data.breakoutDiagnosticObserved = false;
		}
	}

	private static string F(float value) => value.ToString("0.######", CultureInfo.InvariantCulture);
	private static string F(double value) => value.ToString("0.######", CultureInfo.InvariantCulture);

	private static string Csv(string value) {
		value ??= string.Empty;
		return $"\"{value.Replace("\"", "\"\"")}\"";
	}

	private static string SanitizeFileName(string value) {
		foreach (char invalid in Path.GetInvalidFileNameChars()) value = value.Replace(invalid, '_');
		return string.IsNullOrWhiteSpace(value) ? "audit" : value;
	}

	private void FlushAndClose() {
		if (ChartManager.Instance != null) {
			ChartManager.Instance.OnRecordRetired -= OnRecordRetired;
			ChartManager.Instance.OnWeekSettlement -= OnWeekSettlement;
		}
		if (ArtistManager.Instance != null) ArtistManager.Instance.OnPopulationEvent -= WriteArtistPopulationEvent;
		if (LabelLifecycleManager.Instance != null) {
			LabelLifecycleManager.Instance.OnOperatingRosterTargetChanged -= WriteOperatingRosterTargetEvent;
			LabelLifecycleManager.Instance.OnRuntimeLabelProfileInitialized -= WriteRuntimeLabelProfile;
		}
		if (RosterManager.Instance != null) {
			RosterManager.Instance.OnDailyTalentMarketCleared -= WriteDailyTalentMarket;
			RosterManager.Instance.OnDailyTalentMarketAppointment -= WriteDailyTalentAppointment;
		}
		if (CompetitorManager.Instance != null) {
			CompetitorManager.Instance.OnDistributionDealEvent -= OnDistributionDealEvent;
			CompetitorManager.Instance.OnDistributionOfferAttempt -= OnDistributionOfferAttempt;
			CompetitorManager.Instance.OnIndependentDistributionSigned -= OnIndependentDistributionSigned;
			CompetitorManager.Instance.OnIndependentTradeFailure -= OnIndependentTradeFailure;
			CompetitorManager.Instance.OnReleaseStrategy -= OnReleaseStrategy;
			CompetitorManager.Instance.OnCalibrationDecision -= OnCalibrationDecision;
			CompetitorManager.Instance.OnReleaseOutcome -= OnReleaseOutcome;
			CompetitorManager.Instance.OnFormatMemoryRevision -= OnFormatMemoryRevision;
			CompetitorManager.Instance.OnSupplySelection -= OnSupplySelection;
		}
		WriteSeasonalityMonthlyRows();
		if (artistLaborMarketWeeklyWriter != null) {
			foreach ((int week, string prefix, string suffix) in deferredLaborMarketRows.OrderBy(row => row.Week)) {
				(int firstTime, int repeat) = populationSigningFlowByWeek.GetValueOrDefault(week);
				artistLaborMarketWeeklyWriter.WriteLine($"{prefix},{firstTime.ToString(CultureInfo.InvariantCulture)},{repeat.ToString(CultureInfo.InvariantCulture)},{suffix}");
			}
		}
		recordWriter?.Dispose();
		weekWriter?.Dispose();
		lifecycleWriter?.Dispose();
		breakoutWriter?.Dispose();
		retirementWriter?.Dispose();
		tierVolumeWriter?.Dispose();
		labelFinanceWriter?.Dispose();
		dealLedgerWriter?.Dispose();
		labelDirectoryWriter?.Dispose();
		independentDistributorWriter?.Dispose();
		independentDistributionEventWriter?.Dispose();
		independentTradeFailureWriter?.Dispose();
		concentrationWriter?.Dispose();
		firstChartEventWriter?.Dispose();
		distributionOfferAttemptWriter?.Dispose();
		marketRevenueWriter?.Dispose();
		marketClearingWriter?.Dispose();
		marketSpilloverWriter?.Dispose();
		completedWeekSettlementWriter?.Dispose();
		completedWeekSettlementRegionalWriter?.Dispose();
		albumRealizationBridgeWriter?.Dispose();
		formatMemoryRevisionWriter?.Dispose();
		formatMemoryAdjustmentWriter?.Dispose();
		releaseCapacityWriter?.Dispose();
		seasonalityMonthlyWriter?.Dispose();
		albumChartWriter?.Dispose();
		albumCompositionWriter?.Dispose();
		formatMixWriter?.Dispose();
		retiredTrackWriter?.Dispose();
		releaseStrategyWriter?.Dispose();
		releaseOutcomeWriter?.Dispose();
		singleReleaseLaneWriter?.Dispose();
		singleDemandStagesWriter?.Dispose();
		revenueMemoryWriter?.Dispose();
		liveRecordsSnapshotWriter?.Dispose();
		priorCostAssumptionWriter?.Dispose();
		albumTrackLinkWriter?.Dispose();
		calibrationDecisionWriter?.Dispose();
		forkRatioWriter?.Dispose();
		a3EconomicDecisionWriter?.Dispose();
		albumProjectWriter?.Dispose();
		albumProjectDemandWriter?.Dispose();
		albumProjectWeeklyWriter?.Dispose();
		decadeAnnualRollupWriter?.Dispose();
		performanceProfileWriter?.Dispose();
		cityRosterWriter?.Dispose();
		distanceMatrixWriter?.Dispose();
		labelGeographyWriter?.Dispose();
		geographyMetricsWriter?.Dispose();
		dealMetricsWriter?.Dispose();
		genreCatalogWriter?.Dispose();
		genreMarketWeeklyWriter?.Dispose();
		recordGenreExplanationWriter?.Dispose();
		albumDemandExplanationWriter?.Dispose();
		formatDecisionExplanationWriter?.Dispose();
		formatDecisionCohortWriter?.Dispose();
		formatDecisionCohortDetailWriter?.Dispose();
		supplySelectionWriter?.Dispose();
		traditionalPopFallbackWriter?.Dispose();
		genreShapeWriter?.Dispose();
		genreEventsWriter?.Dispose();
		specialProductsWriter?.Dispose();
		rosterLifecycleWriter?.Dispose();
		labelScoutingVacancyWriter?.Dispose();
		artistPopulationEventsWriter?.Dispose();
		artistPopulationWeeklyWriter?.Dispose();
		artistLaborMarketWeeklyWriter?.Dispose();
		artistCohortAnnualWriter?.Dispose();
		artistProjectIdentityWriter?.Dispose();
		labelOperatingTargetEventWriter?.Dispose();
		runtimeLabelProfileWriter?.Dispose();
		dailyTalentMarketWriter?.Dispose();
		dailyTalentAppointmentWriter?.Dispose();
		catastrophicFailFastWriter?.Dispose();
		recordWriter = null;
		weekWriter = null;
		lifecycleWriter = null;
		breakoutWriter = null;
		retirementWriter = null;
		tierVolumeWriter = null;
		labelFinanceWriter = null;
		dealLedgerWriter = null;
		labelDirectoryWriter = null;
		independentDistributorWriter = null;
		independentDistributionEventWriter = null;
		independentTradeFailureWriter = null;
		concentrationWriter = null;
		firstChartEventWriter = null;
		distributionOfferAttemptWriter = null;
		marketRevenueWriter = null;
		marketClearingWriter = null;
		marketSpilloverWriter = null;
		completedWeekSettlementWriter = null;
		completedWeekSettlementRegionalWriter = null;
		albumRealizationBridgeWriter = null;
		formatMemoryRevisionWriter = null;
		formatMemoryAdjustmentWriter = null;
		releaseCapacityWriter = null;
		seasonalityMonthlyWriter = null;
		albumChartWriter = null;
		albumCompositionWriter = null;
		formatMixWriter = null;
		retiredTrackWriter = null;
		releaseStrategyWriter = null;
		releaseOutcomeWriter = null;
		singleReleaseLaneWriter = null;
		singleDemandStagesWriter = null;
		revenueMemoryWriter = null;
		liveRecordsSnapshotWriter = null;
		priorCostAssumptionWriter = null;
		albumTrackLinkWriter = null;
		calibrationDecisionWriter = null;
		forkRatioWriter = null;
		a3EconomicDecisionWriter = null;
		albumProjectWriter = null;
		albumProjectDemandWriter = null;
		albumProjectWeeklyWriter = null;
		decadeAnnualRollupWriter = null;
		performanceProfileWriter = null;
		cityRosterWriter = null;
		distanceMatrixWriter = null;
		labelGeographyWriter = null;
		geographyMetricsWriter = null;
		dealMetricsWriter = null;
		genreCatalogWriter = null;
		genreMarketWeeklyWriter = null;
		recordGenreExplanationWriter = null;
		albumDemandExplanationWriter = null;
		formatDecisionExplanationWriter = null;
		formatDecisionCohortWriter = null;
		formatDecisionCohortDetailWriter = null;
		supplySelectionWriter = null;
		traditionalPopFallbackWriter = null;
		genreShapeWriter = null;
		genreEventsWriter = null;
		specialProductsWriter = null;
		rosterLifecycleWriter = null;
		labelScoutingVacancyWriter = null;
		artistPopulationEventsWriter = null;
		artistPopulationWeeklyWriter = null;
		artistCohortAnnualWriter = null;
		artistProjectIdentityWriter = null;
		labelOperatingTargetEventWriter = null;
		runtimeLabelProfileWriter = null;
		dailyTalentMarketWriter = null;
		dailyTalentAppointmentWriter = null;
		catastrophicFailFastWriter = null;
	}
}
