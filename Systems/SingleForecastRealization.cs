using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

/// <summary>
/// Opt-in (SimTools/ReleaseVolumeCalibrationPlan.md, "Forecasts that see the market"): a label's orphan-single forecast
/// is its prior times the label's own recent realized-to-prior ratio, in dollars. It replaces the single lane's residual
/// term in <c>CompetitorManager.DecideRelease</c>, whose confidence (K 12, at most .65) leaves a tail label's forecast
/// within a few percent of its prior while its singles realize a third to a half of it.
/// The ratio sums the same maturity-weighted, recency-weighted revisions the residual memory keeps (latest estimate per
/// release, 52-week half-life, 104-week window), so a hit counts at its full value rather than at a clipped residual.
/// It is shrunk toward 1 by <see cref="PseudoReleases"/> releases of evidence at the label's own mean prior, so a label
/// with no evidence keeps its prior and evidence that ages out returns it there. No firm, tier or market number enters.
/// A probe, not a sourced rate.
/// </summary>
public static class SingleForecastRealization {
	public const string Switch = "--single-forecast-realization=";
	// The ratio cannot invert a forecast's sign or treble it on a lucky streak.
	internal const float MaximumRatio = 3f;
	private const int MaximumHistoryWeeks = 104;
	private const float HalfLifeWeeks = 52f;
	private static bool configured;
	public static float PseudoReleases { get; private set; }
	public static bool Enabled => PseudoReleases > 0;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		string value = (arguments ?? Array.Empty<string>()).SingleOrDefault(a => a.StartsWith(Switch, StringComparison.Ordinal));
		if (value != null) {
			if (!float.TryParse(value[Switch.Length..], System.Globalization.NumberStyles.Float,
					System.Globalization.CultureInfo.InvariantCulture, out float k) || !float.IsFinite(k) || k < .5f || k > 100f)
				throw new ArgumentException(Switch + " requires a pseudo-release count between 0.5 and 100.");
			PseudoReleases = k;
		}
		configured = true;
	}

	internal static void SetForChecks(float k) { configured = true; PseudoReleases = k; }
	internal static void ResetForChecks() { configured = false; PseudoReleases = 0; }

	private sealed class Summary { public int Week = int.MinValue, Count = -1; public float Ratio = 1f; }
	private static readonly ConditionalWeakTable<List<FormatMemoryObservation>, Summary> summaries = new();
	internal static void Invalidate(List<FormatMemoryObservation> observations) { if (observations != null) summaries.Remove(observations); }

	/// <summary>The shrunk realized-to-prior ratio of the label's recent orphan singles; 1 without evidence.</summary>
	internal static float Ratio(List<FormatMemoryObservation> observations, int currentWeek) {
		if (observations == null) return 1f;
		Summary summary = summaries.GetValue(observations, _ => new());
		if (summary.Week == currentWeek && summary.Count == observations.Count) return summary.Ratio;
		double weight = 0, expected = 0, realized = 0;
		foreach (FormatMemoryObservation observation in observations) {
			if (observation.estimatedOutcomeNet is not float outcome) continue;
			int age = Math.Max(0, currentWeek - observation.releaseWeek);
			if (age > MaximumHistoryWeeks) continue;
			double w = Math.Pow(.5, age / HalfLifeWeeks) * Math.Clamp(observation.maturityWeight, .05f, 1f);
			weight += w; expected += w * observation.expectedNet; realized += w * outcome;
		}
		summary.Week = currentWeek; summary.Count = observations.Count;
		return summary.Ratio = Shrunk(weight, expected, realized, PseudoReleases);
	}

	/// <summary>(realized + K x mean prior) / (expected + K x mean prior): the raw ratio pulled toward 1 by K releases
	/// of evidence. No positive expected dollars, no evidence.</summary>
	internal static float Shrunk(double weight, double expected, double realized, float pseudoReleases) {
		if (weight <= 0 || expected <= 0) return 1f;
		double pseudo = pseudoReleases * expected / weight;
		return (float)Math.Clamp((realized + pseudo) / (expected + pseudo), 0, MaximumRatio);
	}

	/// <summary>The forecast at ratio <paramref name="ratio"/>: the prior moved by (ratio - 1) of its own size, so a
	/// prior loss is made worse by a ratio below 1, never better.</summary>
	internal static float Forecast(float prior, float ratio) => prior + (ratio - 1f) * Math.Max(1f, Math.Abs(prior));
}
