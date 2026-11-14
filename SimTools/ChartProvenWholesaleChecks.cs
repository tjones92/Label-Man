using System;
using Godot;

/// <summary>--chart-proven-wholesale-check: off, no position counts as proof; on, positions 1..N count and nothing else,
/// a label is chart-proven only by a record inside that depth, and malformed switches are refused.</summary>
public static class ChartProvenWholesaleChecks {
	public static void Run() {
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Chart-proven wholesale: " + why); }
		int positionWas = ChartProvenWholesale.Position, marketsWas = ChartProvenWholesale.Markets;
		bool weeklyWas = ChartProvenWholesale.Weekly;
		try {
			RecordRuntimeData At(int position) => new RecordRuntimeData { baseRecord = new Record { recordId = "r" + position }, currentPosition = position };
			var onChart = new[] { At(0), At(38) };
			var deep = new[] { At(0), At(55) };

			ChartProvenWholesale.SetForChecks(0);
			Check(!ChartProvenWholesale.Enabled && !ChartProvenWholesale.Qualifies(1), "off must not qualify any position");
			Check(!ChartProvenWholesale.IsChartProven(onChart), "off must not prove a label");

			ChartProvenWholesale.SetForChecks(40);
			Check(ChartProvenWholesale.Qualifies(1) && ChartProvenWholesale.Qualifies(40), "positions 1 and 40 must qualify at depth 40");
			Check(!ChartProvenWholesale.Qualifies(41) && !ChartProvenWholesale.Qualifies(0), "41 and off-chart must not qualify");
			Check(ChartProvenWholesale.IsChartProven(onChart), "a record at 38 must prove the label");
			Check(!ChartProvenWholesale.IsChartProven(deep), "a record at 55 must not prove the label at depth 40");
			Check(!ChartProvenWholesale.IsChartProven(null), "no records must not prove");

			bool Refused(params string[] args) {
				typeof(ChartProvenWholesale).GetField("configured", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
					.SetValue(null, false);
				try { ChartProvenWholesale.Configure(args); return false; } catch (ArgumentException) { return true; }
			}
			Check(Refused("--chart-proven-wholesale=0"), "position 0 must be refused");
			Check(Refused("--chart-proven-wholesale=101"), "position 101 must be refused");
			Check(Refused("--chart-proven-wholesale-markets=2"), "markets without the switch must be refused");
			Check(Refused("--chart-proven-wholesale-weekly"), "weekly without the switch must be refused");
			Check(!Refused("--chart-proven-wholesale=100", "--chart-proven-wholesale-weekly") && ChartProvenWholesale.Weekly, "weekly must configure");
			Check(!Refused("--chart-proven-wholesale=40", "--chart-proven-wholesale-markets=2") &&
				ChartProvenWholesale.Position == 40 && ChartProvenWholesale.Markets == 2, "a valid pair must configure");
			GD.Print("CHART_PROVEN_WHOLESALE_CHECK passed");
		} finally {
			ChartProvenWholesale.SetForChecks(positionWas, marketsWas, weeklyWas);
		}
	}
}
