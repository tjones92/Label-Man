using System;
using System.Collections.Generic;
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
	private StreamWriter concentrationWriter;
	private StreamWriter marketRevenueWriter;
	private StreamWriter releaseCapacityWriter;
	private StreamWriter albumChartWriter;
	private StreamWriter albumCompositionWriter;
	private StreamWriter formatMixWriter;
	private StreamWriter retiredTrackWriter;
	private StreamWriter releaseStrategyWriter;
	private StreamWriter releaseOutcomeWriter;
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
	private readonly Dictionary<string, long> annualChartUnitsByLabel = new(StringComparer.Ordinal);
	private readonly Dictionary<(string Tier, string Format), RevenueRollup> annualMarketRevenue = new();
	private readonly Dictionary<string, string> acquiredBy = new(StringComparer.Ordinal);
	private readonly Dictionary<(int Year, string Format), FormatMixRollup> annualFormatMix = new();
	private readonly HashSet<string> observedReleaseIds = new(StringComparer.Ordinal);
	private readonly HashSet<string> observedAlbumIds = new(StringComparer.Ordinal);
	private int concentrationYear;
	private int marketRevenueYear;
	private MarketRegion[] regions;
	private int currentAuditWeek;
	private int requestedWeeks = 52;
	private string runName = "audit";
	private ulong? requestedSeed;
	private bool aggregateOnly;
	private bool forceDistributionDeal;
	private bool disableLabelLifecycle;
	private bool disableDistributionDeals;
	private bool disableAlbums;
	private AILabel forcedDealClient;
	private AILabel forcedDealDistributor;
	private float forcedDealInitialAdvance;
	private float forcedDealRoutedTotal;
	private string forcedDealResolution;
	private int forcedDealSignedWeek;
	private int forcedClientInitialRoster;
	private int previousTrackResolutionAttempts;
	private int previousTrackResolutionMisses;
	private int previousTrackArchiveHits;

	public override void _Ready() {
		try {
			ParseArguments();
			if (TimeManager.Instance == null || ChartManager.Instance == null) {
				throw new InvalidOperationException("The TimeManager and ChartManager autoloads must be available.");
			}

			if (requestedSeed.HasValue) GD.Seed(requestedSeed.Value);
			if (disableLabelLifecycle) LabelLifecycleManager.Instance?.SetProcessingEnabled(false);
			if (disableDistributionDeals) CompetitorManager.Instance.SetDistributionOfferProcessingEnabled(false);
			if (disableAlbums) CompetitorManager.Instance.SetAlbumsEnabled(false);
			regions = ChartManager.Instance.GetAllRegions().ToArray();
			OpenOutputs();
			CompetitorManager.Instance.OnDistributionDealEvent += OnDistributionDealEvent;
			CompetitorManager.Instance.OnReleaseStrategy += OnReleaseStrategy;
			CompetitorManager.Instance.OnCalibrationDecision += OnCalibrationDecision;
			CompetitorManager.Instance.OnReleaseOutcome += OnReleaseOutcome;
			if (forceDistributionDeal) InstallForcedDistributionDeal();
			ChartManager.Instance.OnRecordRetired += OnRecordRetired;
			InitializeObservedState();

			for (int week = 1; week <= requestedWeeks; week++) {
				currentAuditWeek = week;
				AdvanceOneChartWeek();
				CaptureWeek(week);
			}
			WriteActiveOffChartRetirementRows();
			WriteConcentrationYear();
			WriteMarketRevenueYear();
			WriteAnnualFormatMixRows();
			WriteLiveRecordsSnapshot();
			WriteAlbumProjectSnapshots();
			if (forceDistributionDeal) ValidateForcedDistributionDeal();

			FlushAndClose();
			GD.Print($"CHART_AUDIT_COMPLETE run={runName} weeks={requestedWeeks}");
			GetTree().Quit(0);
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
			}
		}

		if (requestedWeeks < 1) throw new ArgumentOutOfRangeException(nameof(requestedWeeks));
	}

	private void InstallForcedDistributionDeal() {
		if (forcedDealResolution != null && forcedDealResolution is not ("exit" or "renew" or "absorb")) {
			throw new ArgumentException($"Unknown forced deal resolution '{forcedDealResolution}'.");
		}
		if (forcedDealResolution != null) CompetitorManager.Instance.SetDistributionOfferProcessingEnabled(false);
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
		if (forcedDealResolution == "exit") forcedDealClient.ownedReach = 0.95f;
		else if (forcedDealResolution == "renew") forcedDealClient.ownedReach = 0.50f;
		else if (forcedDealResolution == "absorb") forcedDealClient.ownedReach = 0.05f;
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
		if (forcedDealResolution == "absorb" && (forcedDealClient.status != LabelStatus.Acquired ||
			CompetitorManager.Instance.GetOperatingLabels().Contains(forcedDealClient) || forcedDealClient.CurrentRosterSize != 0 || forcedClientInitialRoster <= 0)) {
			throw new InvalidOperationException("Forced absorption did not fully retire and transfer the client.");
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
		concentrationWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-concentration.csv"));
		marketRevenueWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-market-revenue.csv"));
		releaseCapacityWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-release-capacity.csv"));
		albumChartWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-album-chart.csv"));
		albumCompositionWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-album-composition.csv"));
		formatMixWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-format-mix.csv"));
		retiredTrackWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-retired-track-availability.csv"));
		releaseStrategyWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-release-strategy.csv"));
		releaseOutcomeWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-release-outcomes.csv"));
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

		recordWriter.WriteLine("week,year,recordId,title,artistId,labelId,labelTier,isPlayerOwned,genre,quality,weeksSinceRelease,weeksOnChart,currentPosition,previousPosition,unitsThisWeek,totalUnitsSold,awareness,radioHeat,wordOfMouth,momentum,saturation,chartPoints,chartCutoffPoints,distanceFrom100Cutoff,regionalBreakoutCount,neighboringMarketTestCount,crossoverCandidateStrength,peakRegionalBreakoutStrength,sustainedSalesVelocity,unmetRegionalDemand,coveredRegionCount,initialLaunchAwareness,initialLaunchStock,launchCareerState,perceivedQualityMultiplier");
		weekWriter.WriteLine("week,year,totalChartUnits,totalMarketUnits,numberOneRecordId,numberOneUnitsThisWeek,newEntriesTop100,newEntriesTop40,exitsTop100,activeRecords,newRecords,retiredRecords");
		lifecycleWriter.WriteLine("week,recordId,title,debutPosition,peakPosition,weeksOnChart,weeksAtNumberOne,lifetimeUnitsSold,leftCensoredAtRunStart");
		breakoutWriter.WriteLine("week,recordId,labelTier,careerState,regionId,distributionRegionCoverage,weeksSinceRelease,weekStartStock,preRestockStock,rawSales,unitsSoldThisWeek,unitsBackordered,awareBuyers,conversionRate,restockTriggered,requestedRestockAmount,restockAmount,maxCapacity,capacityCapped,breakoutScore,breakoutStage,tractionWeeks,sustainedGrowthWeeks,salesVelocity,volumeInput,velocityInput,audienceInput,mediaInput,genreFitInput,qualityInput,unmetDemandInput,discoveryVisibilityMultiplier,breakoutAwarenessGain,breakoutRadioGain,breakoutWordOfMouthGain,neighboringMarketTestStrength,breakoutSourceRegionId");
		retirementWriter.WriteLine("week,status,recordId,labelTier,weeksSinceRelease,weeksOnChart,weeksSinceLastTop100,weeksSinceSalesAboveFloor,floorBreachAge,unitsThisWeek,totalRadioPlay");
		tierVolumeWriter.WriteLine("week,labelTier,launchRecords,launchUnits,middleRecords,middleUnits,catalogTailRecords,catalogTailUnits,totalRecords,totalUnits");
		labelFinanceWriter.WriteLine("week,year,labelId,labelName,archetype,isHistorical,labelTier,status,cashReserves,monthlyRevenue,monthlyExpenses,weeklyGross,weeklyCogs,weeklySkim,weeklyRoyalty,weeklyNet,weeklyDistributionIncome,ownedReach,borrowedReach,capability,dealDistributorId,dealUnrecoupedAdvance");
		dealLedgerWriter.WriteLine("eventWeek,year,resolution,origin,distributorId,distributorName,clientId,clientName,reachGranted,marginSkim,ownsMasters,advance,signedWeek,termWeeks,dependency");
		labelDirectoryWriter.WriteLine("labelId,labelName,archetype,isHistorical,initialTier");
		concentrationWriter.WriteLine("year,c4ChartShare,c8ChartShare,firmsCharting,indieFamilyChartShare,majorFamilyChartShare,totalChartUnits");
		marketRevenueWriter.WriteLine("period,week,year,labelTier,releaseFormat,totalMarketUnits,gross,labelNet,distributionIncome,marketNet");
		releaseCapacityWriter.WriteLine("week,year,releaseRollsFired,successfulReleases,failedReleaseRolls,cooldownMismatchRolls,otherFailedRolls,failedRollRate,cooldownMismatchRate");
		albumChartWriter.WriteLine("week,year,month,chartSize,position,previousPosition,recordId,title,artistId,labelId,genre,albumFormat,unitsThisWeek,totalUnitsSold,weeksOnChart,pooledAppeal,thematicCohesion,packaging");
		albumCompositionWriter.WriteLine("week,year,recordId,artistId,genre,albumFormat,thematicCohesion,pooledAppeal,trackCount,reusedSingleTracks,nonSingleTracks,compTrackShare,runtimeMinutes,packaging,isStereo");
		formatMixWriter.WriteLine("period,week,year,releaseFormat,releases,releaseShare,units,unitShare,gross,revenueShare,cogs,distributionSkim,artistRoyalty,labelNet");
		retiredTrackWriter.WriteLine("week,year,resolutionAttempts,retiredArchiveHits,unarchivedMisses,cumulativeAttempts,cumulativeRetiredArchiveHits,cumulativeUnarchivedMisses");
		releaseStrategyWriter.WriteLine("week,year,recordId,labelId,tier,artistId,genre,careerState,projectedSingleNet,projectedAlbumNet,confidenceSingle,confidenceAlbum,chosenFormat,projectId,strategy,projectedOrphanSingleNet,projectedAlbumStandaloneNet,projectedAlbumWithPromoNet,promoSingleId,bucketMeanNet,singleProductionCost,singleNetMarginPerUnit,expectedSingleUnits,albumDemandFactor,substitutionK,substitutionCap,substitutionPropensity,expectedOverlapFraction,divertedUnits,albumMarginPerUnit,cannibalizationLoss,expectedPromoLift,expectedPromoSingleNet,promoAdvantage");
		releaseOutcomeWriter.WriteLine("week,year,labelId,recordId,format,memoryEligible,lifetimeLabelNet,sunkProductionCost,realizedNet");
		revenueMemoryWriter.WriteLine("week,year,labelId,format,emaNetPerRelease,releasesObserved");
		liveRecordsSnapshotWriter.WriteLine("week,year,recordId,labelId,artistId,format,ageWeeks,lifetimeLabelNet,sunkProductionCost,observedNetLowerBound,currentPosition,totalUnitsSold");
		priorCostAssumptionWriter.WriteLine("week,year,recordId,assumedCompilationCost,actualAlbumFormat");
		albumTrackLinkWriter.WriteLine("week,year,albumRecordId,artistId,sourceRecordId,freshnessApplied,timesCompUsedAtGeneration");
		calibrationDecisionWriter.WriteLine("week,year,recordId,labelId,artistId,genre,careerState,qualityEstimate,reachFactor,genreSinglesMarketFactor,singleProductionCost,chosenFormat");
		forkRatioWriter.WriteLine("week,year,recordId,labelId,artistId,genre,genreGroup,careerState,careerBand,qualityEstimate,qualityQuartile,reachFactor,genreSinglesMarketFactor,priorSingleNet,priorAlbumNet,projectedSingleNet,projectedAlbumNet,albumMinusSingleNet,albumToSingleRatio,chosenFormat");
		a3EconomicDecisionWriter.WriteLine("week,year,recordId,labelId,artistId,genre,genreGroup,careerState,compCostWeight,expectedFormatMultiplier,actualAlbumFormat,releasedSingleIdsExamined,resolvedSingles,chartedSingles,hitScore,unweightedHitUnits,weightedHitUnits,affinityUnits,totalExpectedAlbumUnits,priorSingleNet,priorAlbumNet,projectedSingleNet,projectedAlbumNet,chosenFormat");
		albumProjectWriter.WriteLine("projectId,creationSequence,originalLabelId,currentLabelId,tierAtSchedule,genre,careerStateAtSchedule,scheduledWeek,dropWeek,strategy,albumRecordId,promoSingleId,promoPeakAtDrop,promoPeakScore,synergyAwarenessApplied,synergyStockMultiplier,terminalState,wasTransferred,transferCount,albumRetired,promoRetired,projectRealizedNet");
		albumProjectDemandWriter.WriteLine("projectId,strategy,albumRecordId,rawDemandBeforeCannibalization,suppressedDemand,demandWeightedSuppression,initialLaunchAwareness,initialLaunchStock,linkedPromoId,demandWithActiveLinkedPromo,demandWithInactiveLinkedPromo,demandWeightedSingleHeat,demandWeightedSubstitutionPropensity,reconciledDemandWeightedSuppression");
		albumProjectWeeklyWriter.WriteLine("week,year,pipelineAlbumDrops");
		foreach (AILabel label in CompetitorManager.Instance.GetAllLabels().OrderBy(label => label.labelId, StringComparer.Ordinal)) {
			labelDirectoryWriter.WriteLine(string.Join(",", new[] { Csv(label.labelId), Csv(label.labelName), Csv(label.archetype.ToString()),
				label.isHistorical ? "true" : "false", Csv(label.tier.ToString()) }));
		}
	}

	private void OnDistributionDealEvent(DistributionDealTelemetry dealEvent) {
		int year = TimeManager.Instance?.CurrentDate.year ?? 1960;
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

	private void OnReleaseStrategy(ReleaseStrategyTelemetry strategy) {
		int year = TimeManager.Instance?.CurrentDate.year ?? 1960;
		releaseStrategyWriter.WriteLine(string.Join(",", new[] {
			currentAuditWeek.ToString(CultureInfo.InvariantCulture), year.ToString(CultureInfo.InvariantCulture),
			Csv(strategy.recordId), Csv(strategy.labelId), Csv(strategy.tier.ToString()), Csv(strategy.artistId),
			Csv(strategy.genre.ToString()), Csv(strategy.careerState.ToString()), F(strategy.projectedSingleNet),
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
			strategy.albumStrategyEvaluated ? F(strategy.expectedPromoLift) : string.Empty,
			strategy.albumStrategyEvaluated ? F(strategy.expectedPromoSingleNet) : string.Empty,
			strategy.albumStrategyEvaluated ? F(strategy.promoAdvantage) : string.Empty
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
			Csv(strategy.recordId), Csv(strategy.labelId), Csv(strategy.artistId), Csv(strategy.genre.ToString()),
			Csv(GetGenreGroup(strategy.genre)), Csv(strategy.careerState.ToString()), Csv(strategy.careerBand),
			F(strategy.qualityEstimate), Csv(strategy.qualityQuartile), F(strategy.reachFactor), F(strategy.genreSinglesMarketFactor),
			F(strategy.priorSingleNet), F(strategy.priorAlbumNet), F(strategy.projectedSingleNet), F(strategy.projectedAlbumNet),
			F(difference), ratio, Csv(strategy.chosenFormat.ToString())
		}));
		a3EconomicDecisionWriter.WriteLine(string.Join(",", new[] {
			currentAuditWeek.ToString(CultureInfo.InvariantCulture), year.ToString(CultureInfo.InvariantCulture),
			Csv(strategy.recordId), Csv(strategy.labelId), Csv(strategy.artistId), Csv(strategy.genre.ToString()),
			Csv(GetGenreGroup(strategy.genre)), Csv(strategy.careerState.ToString()), F(strategy.compCostWeight),
			F(strategy.expectedFormatMultiplier), Csv(strategy.actualAlbumFormat?.ToString()),
			strategy.releasedSingleIdsExamined.ToString(CultureInfo.InvariantCulture), strategy.resolvedSingles.ToString(CultureInfo.InvariantCulture),
			strategy.chartedSingles.ToString(CultureInfo.InvariantCulture), F(strategy.hitScore), F(strategy.unweightedHitUnits),
			F(strategy.weightedHitUnits), F(strategy.affinityUnits), F(strategy.totalExpectedAlbumUnits),
			F(strategy.priorSingleNet), F(strategy.priorAlbumNet), F(strategy.projectedSingleNet), F(strategy.projectedAlbumNet),
			Csv(strategy.chosenFormat.ToString())
		}));
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
		releaseOutcomeWriter.WriteLine(string.Join(",", new[] {
			currentAuditWeek.ToString(CultureInfo.InvariantCulture), year.ToString(CultureInfo.InvariantCulture),
			Csv(outcome.labelId), Csv(outcome.recordId), Csv(outcome.format.ToString()), outcome.memoryEligible ? "true" : "false",
			F(outcome.lifetimeLabelNet), F(outcome.sunkProductionCost), F(outcome.realizedNet)
		}));
	}

	private void OnRecordRetired(RecordRuntimeData record) {
		if (record.baseRecord.format == ReleaseFormat.Single) WriteRetirementRow("retired", record);
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
		new(path, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

	private void InitializeObservedState() {
		foreach (RecordRuntimeData record in ChartManager.Instance.GetAllRecords().Where(record => record.baseRecord.format == ReleaseFormat.Single)) {
			ObserveRecord(record, wasPresentAtStart: true);
			observedReleaseIds.Add(record.baseRecord.recordId);
		}
		previousChartIds = ChartManager.Instance.GetCurrentChart()
			.Select(record => record.baseRecord.recordId)
			.ToHashSet(StringComparer.Ordinal);
		previousActiveIds = ChartManager.Instance.GetAllRecords()
			.Select(record => record.baseRecord.recordId)
			.ToHashSet(StringComparer.Ordinal);
	}

	private void CaptureWeek(int week) {
		GameDate date = TimeManager.Instance.CurrentDate;
		albumProjectWeeklyWriter.WriteLine(string.Join(",", new[] {
			week.ToString(CultureInfo.InvariantCulture), date.year.ToString(CultureInfo.InvariantCulture),
			CompetitorManager.Instance.WeeklyPipelineAlbumDrops.ToString(CultureInfo.InvariantCulture)
		}));
		List<RecordRuntimeData> records = ChartManager.Instance.GetAllRecords();
		List<RecordRuntimeData> singleRecords = records.Where(record => record.baseRecord.format == ReleaseFormat.Single).ToList();
		List<RecordRuntimeData> chart = ChartManager.Instance.GetCurrentChart();
		List<RecordRuntimeData> albumChart = ChartManager.Instance.GetCurrentAlbumChart();
		AccumulateConcentration(date.year, chart);
		if (forceDistributionDeal) {
			if (!Mathf.IsEqualApprox(forcedDealClient.weeklyDistributionSkim, forcedDealDistributor.weeklyDistributionIncome)) {
				throw new InvalidOperationException("Forced deal skim was not credited to its distributor.");
			}
			forcedDealRoutedTotal += forcedDealClient.weeklyDistributionSkim;
		}
		float chartCutoff = chart.Count >= 100 ? ChartSimulator.CalculateChartPoints(chart[99], regions) : 0f;
		var activeIds = singleRecords.Select(record => record.baseRecord.recordId).ToHashSet(StringComparer.Ordinal);
		var chartIds = chart.Select(record => record.baseRecord.recordId).ToHashSet(StringComparer.Ordinal);

		foreach (RecordRuntimeData record in singleRecords) {
			LifecycleState state = ObserveRecord(record, wasPresentAtStart: false);
			if (state.DebutPosition == 0 && record.currentPosition > 0) {
				state.DebutPosition = record.currentPosition;
			}
			if (record.currentPosition == 1) state.WeeksAtNumberOne++;
			if (!aggregateOnly) WriteRecordRow(week, date.year, record, chartCutoff);
			WriteBreakoutRows(week, record);
		}

		foreach ((string id, LifecycleState state) in lifecycle.ToArray()) {
			if (!activeIds.Contains(id)) {
				WriteLifecycleRow(week, state);
				lifecycle.Remove(id);
			}
		}

		RecordRuntimeData numberOne = chart.FirstOrDefault();
		int totalChartUnits = chart.Sum(record => record.unitsThisWeek);
		int totalMarketUnits = records.Sum(record => record.unitsThisWeek);
		int newTop100 = chartIds.Count(id => !previousChartIds.Contains(id));
		int newTop40 = chart.Take(40).Count(record => !previousChartIds.Contains(record.baseRecord.recordId));
		int exits = previousChartIds.Count(id => !chartIds.Contains(id));
		int newRecords = activeIds.Count(id => !previousActiveIds.Contains(id));
		int retiredRecords = previousActiveIds.Count(id => !activeIds.Contains(id));
		WriteTierVolumeRows(week, records);
		WriteLabelFinanceRows(week, date.year);
		WriteMarketRevenueRows(week, date.year, records);
		WriteReleaseCapacityRow(week, date.year);
		WriteAlbumRows(week, date, records, albumChart);
		WriteFormatMixRows(week, date.year, records);
		WriteRevenueMemoryRows(week, date.year);

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

	private void WriteRevenueMemoryRows(int week, int year) {
		ReleaseFormat[] formats = { ReleaseFormat.Single, ReleaseFormat.Album };
		foreach (AILabel label in CompetitorManager.Instance.GetAllLabels().OrderBy(label => label.labelId, StringComparer.Ordinal)) {
			foreach (ReleaseFormat format in formats) {
				label.revenueMemory.TryGetValue(format, out FormatRevenueMemory memory);
				revenueMemoryWriter.WriteLine(string.Join(",", new[] {
					week.ToString(CultureInfo.InvariantCulture), year.ToString(CultureInfo.InvariantCulture), Csv(label.labelId), Csv(format.ToString()),
					F(memory?.emaNetPerRelease ?? 0f), (memory?.releasesObserved ?? 0).ToString(CultureInfo.InvariantCulture)
				}));
			}
		}
	}

	private void AccumulateConcentration(int year, List<RecordRuntimeData> chart) {
		if (concentrationYear == 0) concentrationYear = year;
		if (year != concentrationYear) {
			WriteConcentrationYear();
			annualChartUnitsByLabel.Clear();
			concentrationYear = year;
		}
		foreach (RecordRuntimeData record in chart) {
			string labelId = record.baseRecord.labelId;
			if (string.IsNullOrEmpty(labelId)) continue;
			annualChartUnitsByLabel[labelId] = annualChartUnitsByLabel.GetValueOrDefault(labelId) + record.unitsThisWeek;
		}
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
		float c4 = total > 0 ? (float)ranked.Take(4).Sum() / total : 0f;
		float c8 = total > 0 ? (float)ranked.Take(8).Sum() / total : 0f;
		concentrationWriter.WriteLine(string.Join(",", new[] {
			concentrationYear.ToString(CultureInfo.InvariantCulture), F(c4), F(c8), rolledUp.Count.ToString(CultureInfo.InvariantCulture),
			F(total > 0 ? (float)indieUnits / total : 0f), F(total > 0 ? (float)majorUnits / total : 0f), total.ToString(CultureInfo.InvariantCulture)
		}));
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
				F(label.weeklyDistributionIncome), F(label.ownedReach), F(label.borrowedReach), F(label.CalculateCapabilityScore()),
				Csv(label.activeDeal?.distributorId), F(label.activeDeal?.unrecoupedAdvance ?? 0f)
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
		if (marketRevenueYear == 0) marketRevenueYear = year;
		if (year != marketRevenueYear) {
			WriteMarketRevenueYear();
			annualMarketRevenue.Clear();
			marketRevenueYear = year;
		}

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

		weekly[("All", "All")].Units = records.Sum(record => (long)record.unitsThisWeek);
		foreach (RecordRuntimeData record in records) {
			string tier = ChartManager.Instance.GetLabelById(record.baseRecord.labelId)?.tier.ToString() ?? "Unknown";
			string format = record.baseRecord.format.ToString();
			AddUnits(weekly, (tier, "All"), record.unitsThisWeek);
			AddUnits(weekly, ("All", format), record.unitsThisWeek);
			AddUnits(weekly, (tier, format), record.unitsThisWeek);
		}

		foreach (var pair in weekly.OrderBy(pair => pair.Key.Tier, StringComparer.Ordinal)
			.ThenBy(pair => pair.Key.Format, StringComparer.Ordinal)) {
			WriteMarketRevenueRow("weekly", week.ToString(CultureInfo.InvariantCulture), year, pair.Key, pair.Value);
			AccumulateAnnualMarketRevenue(pair.Key, pair.Value);
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
			for (int trackIndex = 0; trackIndex < trackRefs.Length; trackIndex++) {
				AlbumTrack track = trackRefs[trackIndex];
				float freshness = trackIndex < (album.trackRefFreshnessApplied?.Length ?? 0)
					? album.trackRefFreshnessApplied[trackIndex] : 1f;
				int timesCompUsed = trackIndex < (album.trackRefCompUsesAtGeneration?.Length ?? 0)
					? album.trackRefCompUsesAtGeneration[trackIndex] : 0;
				albumTrackLinkWriter.WriteLine(string.Join(",", new[] {
					week.ToString(CultureInfo.InvariantCulture), date.year.ToString(CultureInfo.InvariantCulture),
					Csv(record.baseRecord.recordId), Csv(record.baseRecord.artistId), Csv(track.sourceRecordId), F(freshness),
					timesCompUsed.ToString(CultureInfo.InvariantCulture)
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
		var newReleases = records.Where(record => observedReleaseIds.Add(record.baseRecord.recordId)).ToList();
		var releasesByFormat = newReleases.GroupBy(record => record.baseRecord.format.ToString()).ToDictionary(group => group.Key, group => group.Count());
		var unitsByFormat = records.GroupBy(record => record.baseRecord.format.ToString()).ToDictionary(group => group.Key, group => (long)group.Sum(record => record.unitsThisWeek));
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
		}
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
		if (ChartManager.Instance != null) ChartManager.Instance.OnRecordRetired -= OnRecordRetired;
		if (CompetitorManager.Instance != null) {
			CompetitorManager.Instance.OnDistributionDealEvent -= OnDistributionDealEvent;
			CompetitorManager.Instance.OnReleaseStrategy -= OnReleaseStrategy;
			CompetitorManager.Instance.OnCalibrationDecision -= OnCalibrationDecision;
			CompetitorManager.Instance.OnReleaseOutcome -= OnReleaseOutcome;
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
		concentrationWriter?.Dispose();
		marketRevenueWriter?.Dispose();
		releaseCapacityWriter?.Dispose();
		albumChartWriter?.Dispose();
		albumCompositionWriter?.Dispose();
		formatMixWriter?.Dispose();
		retiredTrackWriter?.Dispose();
		releaseStrategyWriter?.Dispose();
		releaseOutcomeWriter?.Dispose();
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
		recordWriter = null;
		weekWriter = null;
		lifecycleWriter = null;
		breakoutWriter = null;
		retirementWriter = null;
		tierVolumeWriter = null;
		labelFinanceWriter = null;
		dealLedgerWriter = null;
		labelDirectoryWriter = null;
		concentrationWriter = null;
		marketRevenueWriter = null;
		releaseCapacityWriter = null;
		albumChartWriter = null;
		albumCompositionWriter = null;
		formatMixWriter = null;
		retiredTrackWriter = null;
		releaseStrategyWriter = null;
		releaseOutcomeWriter = null;
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
	}
}
