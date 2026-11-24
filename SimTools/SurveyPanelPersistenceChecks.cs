using System;
using System.Linq;
using Godot;

/// <summary>--survey-panel-persistence-check: off returns the fresh draw bit for bit; on, a record's first draw is its
/// error, later draws follow AR(1) with the marginal variance kept at 1, and records never share an error.</summary>
public static class SurveyPanelPersistenceChecks {
	public static void Run() {
		int checks = 0;
		void Check(bool value, string message) { if (!value) throw new InvalidOperationException("Survey panel persistence: " + message); checks++; }
		try {
			SurveyPanelPersistence.SetForChecks(0f);
			var a = new RecordRuntimeData();
			Check(!SurveyPanelPersistence.Enabled && SurveyPanelPersistence.Persist(a, 1.234f) == 1.234f
				&& SurveyPanelPersistence.Persist(a, -0.5f) == -0.5f, "off returns the fresh draw");

			SurveyPanelPersistence.SetForChecks(0.8f);
			var b = new RecordRuntimeData();
			var c = new RecordRuntimeData();
			Check(SurveyPanelPersistence.Persist(b, 1f) == 1f, "a record's first draw is its error");
			Check(SurveyPanelPersistence.Persist(c, -2f) == -2f, "a second record starts from its own draw");
			Check(Math.Abs(SurveyPanelPersistence.Persist(b, 0f) - 0.8f) < 1e-6f, "a zero draw decays the error by rho");
			Check(Math.Abs(SurveyPanelPersistence.Persist(b, 1f) - (0.64f + 0.6f)) < 1e-6f, "the innovation is scaled by sqrt(1 - rho^2)");
			Check(Math.Abs(SurveyPanelPersistence.Persist(c, 0f) + 1.6f) < 1e-6f, "records keep separate errors");
			Check(SurveyPanelPersistence.Persist(null, 0.3f) == 0.3f, "no record, no memory");

			// Marginal variance stays 1 and lag-1 correlation is rho, on a long deterministic normal stream.
			var rng = new Random(7);
			float Normal() { double u1 = Math.Max(1e-12, rng.NextDouble()), u2 = rng.NextDouble(); return (float)(Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2)); }
			var d = new RecordRuntimeData();
			var z = Enumerable.Range(0, 200000).Select(_ => (double)SurveyPanelPersistence.Persist(d, Normal())).ToArray();
			double mean = z.Average(), variance = z.Select(x => (x - mean) * (x - mean)).Average();
			double lag1 = Enumerable.Range(1, z.Length - 1).Select(i => (z[i] - mean) * (z[i - 1] - mean)).Average() / variance;
			Check(Math.Abs(variance - 1) < 0.03 && Math.Abs(lag1 - 0.8) < 0.01, FormattableString.Invariant($"variance {variance:F3}, lag-1 {lag1:F3}"));
			GD.Print($"SURVEY_PANEL_PERSISTENCE_CHECK passed checks={checks}");
		} finally {
			SurveyPanelPersistence.SetForChecks(0f);
		}
	}
}
