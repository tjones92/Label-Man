using System;
using System.Linq;
using Godot;

/// <summary>--chart-tail-check: off is exactly 1; on, the decay waits for the grace, compounds weekly from the
/// record's own sales peak and never touches a record that has not charted.</summary>
public static class ChartTailChecks {
	public static void Run() {
		int checks = 0;
		void Check(bool value, string message) { if (!value) throw new InvalidOperationException("Chart tail: " + message); checks++; }
		try {
			ChartTailProbe.SetForChecks(-1f);
			Check(!ChartTailProbe.Enabled && ChartTailProbe.Multiplier(5, 20) == 1f, "off is exactly 1");

			ChartTailProbe.SetForChecks(.8f);
			Check(ChartTailProbe.Multiplier(5, 0) == 1f && ChartTailProbe.Multiplier(5, 3) == 1f, "1 at the peak and through the default grace");
			Check(ChartTailProbe.Multiplier(5, 4) == .8f && Math.Abs(ChartTailProbe.Multiplier(5, 6) - .512f) < 1e-6f, "compounds weekly past the grace");
			Check(ChartTailProbe.Multiplier(0, 12) == 1f, "a record that never charted keeps its demand");
			Check(ChartTailProbe.Multiplier(90, 6) == ChartTailProbe.Multiplier(1, 6), "the same clock at every peak rank");

			ChartTailProbe.SetForChecks(.8f, 0);
			Check(ChartTailProbe.Multiplier(5, 0) == 1f && ChartTailProbe.Multiplier(5, 1) == .8f, "grace 0 starts the week after the peak");

			ChartTailProbe.SetForChecks(.8f, 1, .9f, 3);
			Check(ChartTailProbe.Multiplier(1, 2) == .9f && ChartTailProbe.Multiplier(4, 2) == .8f && ChartTailProbe.Multiplier(40, 2) == .8f,
				"#1 decays at the top rate; below the top rank at the base rate");
			Check(Math.Abs(ChartTailProbe.Rate(2) - (.9f - .1f / 3f)) < 1e-6f && ChartTailProbe.Rate(2) > ChartTailProbe.Rate(3) && ChartTailProbe.Rate(3) > .8f,
				"#2 to #3 interpolate between the rates");
			Check(ChartTailProbe.Multiplier(1, 1) == 1f && ChartTailProbe.Multiplier(0, 9) == 1f, "the top rate keeps the grace and the charted gate");
			ChartTailProbe.SetForChecks(.8f, 1);
			Check(Enumerable.Range(1, 100).All(p => ChartTailProbe.Rate(p) == .8f), "without the top switch every rank decays at the base rate");
			GD.Print($"CHART_TAIL_CHECK passed checks={checks}");
		} finally {
			ChartTailProbe.SetForChecks(-1f);
		}
	}
}
