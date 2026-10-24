using System;
using System.Linq;
using Godot;

/// <summary>--chart-entry-feedback-check: off is the authored curve and bonus; a wider plateau is flat, lower at the
/// top and still mean one; the radio bonus scales.</summary>
public static class ChartEntryFeedbackChecks {
	public static void Run() {
		int checks = 0;
		void Check(bool value, string message) { if (!value) throw new InvalidOperationException("Chart entry feedback: " + message); checks++; }
		float[] Curve() => Enumerable.Range(1, 100).Select(ChartSimulator.GetChartExposureWeight).ToArray();
		try {
			ChartEntryFeedbackProbe.SetForChecks(-1, -1f);
			var authored = Curve();
			// The authored curve, restated independently of the probe.
			float Raw(int p) => Mathf.Pow((4139f - 4357f * Math.Max(p, 10) / (Math.Max(p, 10) + 10f)) / (4139f - 4357f * 100f / 110f), .62f);
			// Accumulate in float, as the curve does; LINQ's Sum would accumulate in double.
			float total = 0f;
			for (int p = 1; p <= 100; p++) total += Raw(p);
			float mean = total / 100f;
			Check(Enumerable.Range(1, 100).All(p => authored[p - 1] == Raw(p) / mean), "off reproduces the authored curve bit-for-bit");
			Check(ChartSimulator.GetRadioPositionBonus(5, 1e6f) == .25f && ChartSimulator.GetRadioPositionBonus(30, 1e6f) == .1f,
				"off keeps the authored radio bonus");

			ChartEntryFeedbackProbe.SetForChecks(10, -1f);
			Check(Curve().SequenceEqual(authored), "naming the authored rank changes nothing");

			ChartEntryFeedbackProbe.SetForChecks(20, -1f);
			var wide = Curve();
			Check(Math.Abs(wide.Average() - 1f) < 1e-4f, "a wider plateau keeps mean exposure one");
			Check(wide.Take(20).Distinct().Count() == 1, "ranks 1-20 share one exposure");
			Check(wide[9] < authored[9] && wide[19] > authored[19] && wide[29] > authored[29], "the Top 10 loses exposure; #20 and below gain");
			Check(Math.Abs(wide[9] / wide[14] - 1f) < 1e-6f, "entering the Top 10 from #15 buys no exposure");

			ChartEntryFeedbackProbe.SetForChecks(3, -1f);
			var steep = Curve();
			Check(steep[0] > steep[9] && Math.Abs(steep.Average() - 1f) < 1e-4f, "a narrower plateau steepens the Top 10 at mean one");

			ChartEntryFeedbackProbe.SetForChecks(-1, 0f);
			Check(ChartSimulator.GetRadioPositionBonus(5, 1e6f) == 0f && ChartSimulator.GetRadioPositionBonus(30, 1e6f) == 0f, "scale 0 removes the bonus");
			Check(Curve().SequenceEqual(authored), "the radio switch leaves exposure alone");
			ChartEntryFeedbackProbe.SetForChecks(-1, .5f);
			Check(ChartSimulator.GetRadioPositionBonus(5, 1e6f) == .125f && ChartSimulator.GetRadioPositionBonus(5, 7500f) == .0625f,
				"the bonus scales and keeps its sales gate");
			GD.Print($"CHART_ENTRY_FEEDBACK_CHECK passed checks={checks}");
		} finally {
			ChartEntryFeedbackProbe.SetForChecks(-1, -1f);
		}
	}
}
