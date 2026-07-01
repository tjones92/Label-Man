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

	private readonly Dictionary<string, LifecycleState> lifecycle = new();
	private HashSet<string> previousChartIds = new();
	private HashSet<string> previousActiveIds = new();
	private StreamWriter recordWriter;
	private StreamWriter weekWriter;
	private StreamWriter lifecycleWriter;
	private StreamWriter breakoutWriter;
	private MarketRegion[] regions;
	private int requestedWeeks = 52;
	private string runName = "audit";
	private ulong? requestedSeed;
	private bool aggregateOnly;

	public override void _Ready() {
		try {
			ParseArguments();
			if (TimeManager.Instance == null || ChartManager.Instance == null) {
				throw new InvalidOperationException("The TimeManager and ChartManager autoloads must be available.");
			}

			if (requestedSeed.HasValue) GD.Seed(requestedSeed.Value);
			regions = ChartManager.Instance.GetAllRegions().ToArray();
			OpenOutputs();
			InitializeObservedState();

			for (int week = 1; week <= requestedWeeks; week++) {
				AdvanceOneChartWeek();
				CaptureWeek(week);
			}

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
			}
		}

		if (requestedWeeks < 1) throw new ArgumentOutOfRangeException(nameof(requestedWeeks));
	}

	private void OpenOutputs() {
		string outputDirectory = ProjectSettings.GlobalizePath("res://SimLogs");
		Directory.CreateDirectory(outputDirectory);
		recordWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-records.csv"));
		weekWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-weeks.csv"));
		lifecycleWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-lifecycles.csv"));
		breakoutWriter = CreateWriter(Path.Combine(outputDirectory, $"{runName}-breakout-funnel.csv"));

		recordWriter.WriteLine("week,year,recordId,title,artistId,labelId,labelTier,isPlayerOwned,genre,quality,weeksSinceRelease,weeksOnChart,currentPosition,previousPosition,unitsThisWeek,totalUnitsSold,awareness,radioHeat,wordOfMouth,momentum,saturation,chartPoints,chartCutoffPoints,distanceFrom100Cutoff,regionalBreakoutCount,neighboringMarketTestCount,crossoverCandidateStrength,peakRegionalBreakoutStrength,sustainedSalesVelocity,unmetRegionalDemand,coveredRegionCount,initialLaunchAwareness,initialLaunchStock,launchCareerState,perceivedQualityMultiplier");
		weekWriter.WriteLine("week,year,totalChartUnits,totalMarketUnits,numberOneRecordId,numberOneUnitsThisWeek,newEntriesTop100,newEntriesTop40,exitsTop100,activeRecords,newRecords,retiredRecords");
		lifecycleWriter.WriteLine("week,recordId,title,debutPosition,peakPosition,weeksOnChart,weeksAtNumberOne,lifetimeUnitsSold,leftCensoredAtRunStart");
		breakoutWriter.WriteLine("week,recordId,labelTier,careerState,regionId,distributionRegionCoverage,weeksSinceRelease,weekStartStock,preRestockStock,rawSales,unitsSoldThisWeek,unitsBackordered,awareBuyers,conversionRate,restockTriggered,requestedRestockAmount,restockAmount,maxCapacity,capacityCapped,breakoutScore,breakoutStage,tractionWeeks,sustainedGrowthWeeks,salesVelocity,volumeInput,velocityInput,audienceInput,mediaInput,genreFitInput,qualityInput,unmetDemandInput,discoveryVisibilityMultiplier,breakoutAwarenessGain,breakoutRadioGain,breakoutWordOfMouthGain,neighboringMarketTestStrength,breakoutSourceRegionId");
	}

	private static StreamWriter CreateWriter(string path) =>
		new(path, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

	private void InitializeObservedState() {
		foreach (RecordRuntimeData record in ChartManager.Instance.GetAllRecords()) {
			ObserveRecord(record, wasPresentAtStart: true);
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
		List<RecordRuntimeData> records = ChartManager.Instance.GetAllRecords();
		List<RecordRuntimeData> chart = ChartManager.Instance.GetCurrentChart();
		float chartCutoff = chart.Count >= 100 ? ChartSimulator.CalculateChartPoints(chart[99], regions) : 0f;
		var activeIds = records.Select(record => record.baseRecord.recordId).ToHashSet(StringComparer.Ordinal);
		var chartIds = chart.Select(record => record.baseRecord.recordId).ToHashSet(StringComparer.Ordinal);

		foreach (RecordRuntimeData record in records) {
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

			bool covered = label.distributionRegions?.Contains(region.regionId) ?? true;
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

	private static string Csv(string value) {
		value ??= string.Empty;
		return $"\"{value.Replace("\"", "\"\"")}\"";
	}

	private static string SanitizeFileName(string value) {
		foreach (char invalid in Path.GetInvalidFileNameChars()) value = value.Replace(invalid, '_');
		return string.IsNullOrWhiteSpace(value) ? "audit" : value;
	}

	private void FlushAndClose() {
		recordWriter?.Dispose();
		weekWriter?.Dispose();
		lifecycleWriter?.Dispose();
		breakoutWriter?.Dispose();
		recordWriter = null;
		weekWriter = null;
		lifecycleWriter = null;
		breakoutWriter = null;
	}
}
