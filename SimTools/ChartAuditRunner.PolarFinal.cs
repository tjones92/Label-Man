using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Godot;

public partial class ChartAuditRunner {
	private StreamWriter polarCensusWriter, polarFinalWeekWriter;
	private readonly HashSet<string> polarFinalSeen = new(StringComparer.Ordinal);
	private int polarCensusMonth = -1;

	private void OpenPolarFinalAudit() {
		OpenPolarFollowup();
		if (!OS.GetCmdlineUserArgs().Contains("--polar-final-audit")) return;
		string directory = ProjectSettings.GlobalizePath("res://SimLogs");
		OpenPolarResearch(directory);
		polarCensusWriter = CreateWriter(Path.Combine(directory, runName + "-polar-live-census.csv"));
		// Legacy recentCovers is retained as the sum, not described as recent hits.
		polarCensusWriter.WriteLine("seed,date,year,month,genre,sets,slots,originals,standards,traditional,recentCovers,shortSets,duplicateCovers,recentHitCovers,ordinaryCovers");
		OpenPolarDiversity(directory);
		polarFinalWeekWriter = CreateWriter(Path.Combine(directory, runName + "-polar-final-weekly.csv"));
		polarFinalWeekWriter.WriteLine("week,date,year,singles,albums,newSingleCount,meanNewSingleQuality,marketUnits,labelNet,endingCash,dyingLabels,bankruptLabels,invalidFit,newFittedMasters");
		foreach (var record in ChartManager.Instance.GetAllRecords()) polarFinalSeen.Add(record.baseRecord.recordId);
		GD.Print($"POLAR_FINAL_START date={TimeManager.Instance.CurrentDate.ToShortString()} enabled={PolarSongBehavior.UsePolarFitSelection}");
	}

	private void CapturePolarCensus() => CaptureSampledPolarCensus();

	private static ulong CensusSeed(string key) {
		ulong value = 14695981039346656037UL;
		foreach (char c in key) { value ^= c; value = unchecked(value * 1099511628211UL); }
		return value;
	}

	private void CapturePolarFinalWeek(int week) {
		if (polarFinalWeekWriter == null) return;
		CaptureDirective2Week();
		var date = TimeManager.Instance.CurrentDate;
		var records = ChartManager.Instance.GetAllRecords();
		var fresh = records.Where(r => polarFinalSeen.Add(r.baseRecord.recordId)).ToArray();
		var singles = fresh.Where(r => r.baseRecord.format == ReleaseFormat.Single).ToArray();
		var labels = ChartManager.Instance.GetAllLabels();
		int invalid = 0, fitted = 0;
		foreach (var record in fresh) {
			var master = PolarSongMetadataService.Get(record.baseRecord.masterId);
			if (master?.realizedFit == null) continue;
			fitted++;
			var fit = master.realizedFit;
			if (new[] { fit.capability, fit.identity, fit.moment, fit.stretch }.Any(v => !float.IsFinite(v) || v < 0 || v > 1)) invalid++;
		}
		polarFinalWeekWriter.WriteLine(string.Join(",", week, date.ToShortString(), date.year,
			CompetitorManager.Instance.WeeklySingleReleases, CompetitorManager.Instance.WeeklyPipelineAlbumDrops, singles.Length,
			(singles.Length == 0 ? 0 : singles.Average(r => r.GetQuality())).ToString("R", CultureInfo.InvariantCulture),
			ChartManager.Instance.GetLastCompletedWeekSettlement()?.TotalUnits ?? records.Sum(r => (long)r.unitsThisWeek),
			labels.Sum(l => (double)l.weeklyNetRevenue).ToString("R", CultureInfo.InvariantCulture),
			labels.Sum(l => (double)l.cashReserves).ToString("R", CultureInfo.InvariantCulture),
			labels.Count(l => l.status.ToString() == "Dying"), labels.Count(l => l.status.ToString() == "Bankrupt"), invalid, fitted));
		if (invalid > 0) throw new InvalidOperationException("Nonfinite/out-of-range committed polar fit.");
		if (week % 52 == 0) { polarFinalWeekWriter.Flush(); GD.Print($"POLAR_FINAL_PROGRESS run={runName} week={week} date={date.ToShortString()}"); }
	}

	private void ClosePolarFinalAudit() { CloseDirective2(); ClosePolarDiversity(); ClosePolarFollowup(); ClosePolarResearch(); polarCensusWriter?.Dispose(); polarFinalWeekWriter?.Dispose(); polarCensusWriter = polarFinalWeekWriter = null; }
}

