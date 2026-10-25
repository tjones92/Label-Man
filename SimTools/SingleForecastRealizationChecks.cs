using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

/// <summary>--single-forecast-realization-check: argument validation; no evidence keeps the prior; the ratio is dollar
/// weighted and shrunk by K releases; aged, unestimated and off-switch observations are ignored; a revision invalidates
/// the weekly cache; a ratio below 1 never improves a prior loss; the outcome field round-trips and is absent when null.</summary>
public static class SingleForecastRealizationChecks {
	public static void Run() {
		int checks = 0;
		void Check(bool value, string message) { if (!value) throw new InvalidOperationException("Single forecast realization: " + message); checks++; }
		bool Near(float a, float b) => Math.Abs(a - b) < 1e-4f;
		bool Rejects(string arg) {
			try { SingleForecastRealization.ResetForChecks(); SingleForecastRealization.Configure(new[] { arg }); return false; }
			catch (ArgumentException) { return true; }
		}
		float saved = SingleForecastRealization.PseudoReleases;
		try {
			Check(Rejects(SingleForecastRealization.Switch + "0"), "rejects zero");
			Check(Rejects(SingleForecastRealization.Switch + "x"), "rejects a non-number");
			Check(Rejects(SingleForecastRealization.Switch + "500"), "rejects a count above 100");
			Check(!Rejects(SingleForecastRealization.Switch + "4") && SingleForecastRealization.PseudoReleases == 4f, "parses the count");
			SingleForecastRealization.ResetForChecks(); SingleForecastRealization.Configure(Array.Empty<string>());
			Check(!SingleForecastRealization.Enabled, "off without the switch");
			SingleForecastRealization.SetForChecks(4f);

			var list = new List<FormatMemoryObservation>();
			Check(SingleForecastRealization.Ratio(list, 10) == 1f && SingleForecastRealization.Ratio(null, 10) == 1f, "no evidence is ratio 1");
			Check(SingleForecastRealization.Forecast(5000f, 1f) == 5000f, "ratio 1 keeps the prior");
			// Four full-weight releases at the current week: one hit (40k) and three flops (0) on a 10k prior each.
			for (int i = 0; i < 4; i++) list.Add(new FormatMemoryObservation { releaseId = $"r{i}", releaseWeek = 10, expectedNet = 10_000,
				maturityWeight = 1, estimatedOutcomeNet = i == 0 ? 40_000 : 0 });
			// Raw ratio 40k/40k = 1 in dollars, though three of four residuals are -1: a hit counts at its value.
			Check(Near(SingleForecastRealization.Ratio(list, 10), 1f), "dollar weighted, not a mean residual");
			list[0].estimatedOutcomeNet = 0;
			Check(Near(SingleForecastRealization.Ratio(list, 10), 1f), "a revision without invalidation keeps the week's cache");
			SingleForecastRealization.Invalidate(list);
			// Realized 0 of 40k with K = 4 at a 10k mean prior: (0 + 40k) / (40k + 40k) = .5.
			Check(Near(SingleForecastRealization.Ratio(list, 10), .5f), "four releases of evidence against four pseudo-releases halve");
			Check(Near(SingleForecastRealization.Forecast(5000f, .5f), 2500f), "the ratio scales a positive prior");
			Check(Near(SingleForecastRealization.Forecast(-2000f, .5f), -3000f), "a ratio below 1 deepens a prior loss");
			list.Add(new FormatMemoryObservation { releaseId = "unestimated", releaseWeek = 10, expectedNet = 1_000_000, maturityWeight = 1 });
			Check(Near(SingleForecastRealization.Ratio(list, 10), .5f), "an observation kept with the switch off is ignored");
			Check(Near(SingleForecastRealization.Ratio(list, 10 + 105), 1f), "evidence older than 104 weeks ages out");
			// At 52 weeks each weight halves: (0 + 4 x 10k) / (20k + 40k) = 2/3.
			Check(Near(SingleForecastRealization.Ratio(list, 62), 2f / 3f), "a 52-week half-life");
			Check(Near(SingleForecastRealization.Shrunk(1, 1000, 1e9, 4), SingleForecastRealization.MaximumRatio), "the ratio is bounded above");
			Check(SingleForecastRealization.Shrunk(1, 1000, -1e9, 4) == 0f, "the ratio never turns negative");
			Check(SingleForecastRealization.Shrunk(3, -500, 900, 4) == 1f, "no positive expected dollars, no evidence");

			var options = SaveGameService.TestJsonOptions;
			string with = JsonSerializer.Serialize(new FormatMemoryObservation { releaseId = "a", estimatedOutcomeNet = 123.5f }, options);
			Check(JsonSerializer.Deserialize<FormatMemoryObservation>(with, options).estimatedOutcomeNet == 123.5f, "the outcome round-trips");
			Check(!JsonSerializer.Serialize(new FormatMemoryObservation { releaseId = "a" }, options).Contains("estimatedOutcomeNet"),
				"a null outcome is not written");
		} finally {
			SingleForecastRealization.SetForChecks(saved);
		}
		GD.Print($"SINGLE_FORECAST_REALIZATION_CHECK passed checks={checks}");
	}
}
