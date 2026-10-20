using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Godot;

// --mid-tier-promotion-audit adds one row per Independent at its actual quarterly promotion decision.
// Weekly finance snapshots occur after the decision, attribute drift and further daily activity; they cannot
// establish which gate blocked a promotion. This event runs before any promotion resets the capability streak.
public partial class ChartAuditRunner {
	private StreamWriter labelPromotionWriter;

	private void OpenLabelPromotionOutputs(string directory) {
		if (!OS.GetCmdlineUserArgs().Contains("--mid-tier-promotion-audit")) return;
		if (!ArtistPopulationLifecycle.Enabled || LabelLifecycleManager.Instance == null)
			throw new InvalidOperationException("--mid-tier-promotion-audit requires artist population lifecycle and its tier-transition telemetry.");
		labelPromotionWriter = CreateWriter(Path.Combine(directory, $"{runName}-mid-tier-promotion-gates.csv"));
		labelPromotionWriter.WriteLine("week,chartWeek,date,labelId,labelName,recentChartingRecords,monthsActive,sustainedCapabilityQuarters,rosterSize,ownedReach,distributionDependency,capability,status,consecutiveLossMonths,lastMonthlyProfit,cashReserves,monthlyOverhead,eligible");
		LabelLifecycleManager.Instance.OnMidTierPromotionEvaluated += WriteMidTierPromotionGate;
	}

	private void WriteMidTierPromotionGate(AILabel label, int chartingLastYear, bool eligible) {
		if (labelPromotionWriter == null) return;
		static string Number(float value) => value.ToString("R", CultureInfo.InvariantCulture);
		static string Count(int value) => value.ToString(CultureInfo.InvariantCulture);
		labelPromotionWriter.WriteLine(string.Join(",", new[] {
			Count(currentAuditWeek), Count(ChartManager.Instance.GetCurrentChartWeek()),
			Csv(TimeManager.Instance.CurrentDate.ToShortString()), Csv(label.labelId), Csv(label.labelName),
			Count(chartingLastYear), Count(label.monthsActive), Count(label.sustainedCapabilityQuarters),
			Count(label.CurrentRosterSize), Number(label.ownedReach), Number(label.DistributionDependency),
			Number(label.CalculateCapabilityScore()), Csv(label.status.ToString()), Count(label.consecutiveLossMonths),
			Number(label.lastMonthlyProfit), Number(label.cashReserves), Number(label.GetMonthlyOverhead()),
			eligible ? "true" : "false"
		}));
	}

	private void CloseLabelPromotionOutputs() {
		if (LabelLifecycleManager.Instance != null)
			LabelLifecycleManager.Instance.OnMidTierPromotionEvaluated -= WriteMidTierPromotionGate;
		labelPromotionWriter?.Dispose();
		labelPromotionWriter = null;
	}
}
