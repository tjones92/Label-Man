using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>
/// --earned-lp-forecast (LpGenreAllocationFollowup.md §44-45). The LP release gate forecasts units from the act and the
/// market only, and that prior runs 1.5-2.9x low for Major and MidTier LPs while it is about right for small labels (§43).
/// Here each AI label learns its own correction. When one of its LPs reaches <see cref="ObserveAgeWeeks"/> weeks (or
/// retires sooner), the copies sold so far are compared with the units the gate forecast for it at release, and the gate
/// then scales the label's LP unit forecasts by what it has learned, shrunk toward 1 by n / (n + Shrink).
///
/// Two estimators of the label's mean sold / forecast ratio:
/// - Totals (default): exponentially weighted copies sold over forecast units. Unbiased in expectation, but under a
///   right-skewed reception draw (--lp-reception-sigma) a label with a few LPs usually sits below its mean, since the
///   hits that carry the mean have not yet come (§44: small labels drift below 1).
/// - Log (--earned-lp-forecast-log): weighted mean m and variance v of log(sold / forecast), and the ratio exp(m + v / 2),
///   the lognormal mean. The median of a few draws is not skewed, and the label's own spread restores the mean without
///   having to wait for the hit. The moments weight LPs equally until the label has 1 / Alpha of them (then
///   exponentially), v carries the small-sample correction n / (n - 1), and needs two observations; before that it is 0.
///   Single LPs are not clamped (that would cut off the hits v must see); only the learned ratio is.
/// Copies to date at 52 weeks undercount an LP still selling, so both are lower bounds for long sellers.
/// No random draws. Off: nothing is read or written and the gate is unchanged.
/// </summary>
public static class EarnedLpForecast {
	public const string Switch = "--earned-lp-forecast";
	public const string LogSwitch = "--earned-lp-forecast-log";
	public const string ShrinkSwitch = "--earned-lp-forecast-shrink";
	public const int ObserveAgeWeeks = 52;
	public const float Alpha = .1f, DefaultShrink = 4f, MinRatio = .25f, MaxRatio = 6f;
	private static bool configured, enabled, logMode;
	public static bool Enabled => enabled;
	public static bool LogMode => enabled && logMode;
	public static float ShrinkRecords { get; private set; } = DefaultShrink;

	/// <summary>Raised on every observation (telemetry only; the runner writes earned-lp-forecast.csv).</summary>
	public static event Action<Observation> Observed;
	public readonly struct Observation {
		public readonly AILabel Label; public readonly RecordRuntimeData Record; public readonly float Multiplier;
		public Observation(AILabel label, RecordRuntimeData record, float multiplier) { Label = label; Record = record; Multiplier = multiplier; }
	}

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var args = (arguments ?? Array.Empty<string>()).ToArray();
		enabled = args.Contains(Switch);
		logMode = args.Contains(LogSwitch);
		ShrinkRecords = LabelPressingEconomics.ReadParameter(args, ShrinkSwitch + "=", DefaultShrink, 0f, 100f);
		if (!enabled && (logMode || ShrinkRecords != DefaultShrink))
			throw new ArgumentException(LogSwitch + " and " + ShrinkSwitch + " require " + Switch + ".");
		configured = true;
	}
	internal static void SetForChecks(bool value, bool log = false, float shrink = DefaultShrink) {
		configured = true; enabled = value; logMode = log; ShrinkRecords = shrink;
	}

	/// <summary>The label's learned mean ratio of copies sold to forecast, before shrinkage; 1 with no observation.</summary>
	public static float LearnedRatio(AILabel label) {
		if (label == null || label.lpForecastObservations <= 0) return 1f;
		if (logMode) return Math.Clamp(MathF.Exp(label.lpForecastLogMean + LogVariance(label) / 2f), MinRatio, MaxRatio);
		return label.lpForecastMemoryForecast > 0f
			? Math.Clamp(label.lpForecastMemorySold / label.lpForecastMemoryForecast, MinRatio, MaxRatio) : 1f;
	}

	/// <summary>Variance of the label's log ratios with the small-sample correction; 0 before two observations.
	/// Effective count: n while weights are equal, then 2 / Alpha - 1 for the exponential weights.</summary>
	public static float LogVariance(AILabel label) {
		int n = label?.lpForecastObservations ?? 0;
		if (n < 2) return 0f;
		float effective = Math.Min(n, 2f / Alpha - 1f), m = label.lpForecastLogMean;
		return Math.Max(0f, label.lpForecastLogSquare - m * m) * effective / (effective - 1f);
	}

	/// <summary>The factor on the gate's LP unit forecast for this label; 1 while off or before any observation.</summary>
	public static float Multiplier(AILabel label) {
		if (!enabled || label == null || label.isPlayerOwned || label.lpForecastObservations <= 0) return 1f;
		float weight = label.lpForecastObservations / (label.lpForecastObservations + ShrinkRecords);
		return MathF.Exp(weight * MathF.Log(LearnedRatio(label)));
	}

	/// <summary>Folds an LP into its label's memory once: at <see cref="ObserveAgeWeeks"/> or at retirement, whichever
	/// comes first. Records without a release-time forecast (opening catalogue, player records) are skipped.</summary>
	public static void Observe(AILabel label, RecordRuntimeData record) {
		if (!enabled || label == null || label.isPlayerOwned || record?.baseRecord?.format != ReleaseFormat.Album
			|| record.lpForecastObserved || record.releaseTimeExpectedLpUnits <= 0f) return;
		record.lpForecastObserved = true;
		float sold = Math.Max(0, record.pressingBookSold), forecast = record.releaseTimeExpectedLpUnits;
		label.lpForecastMemorySold = (1f - Alpha) * label.lpForecastMemorySold + Alpha * sold;
		label.lpForecastMemoryForecast = (1f - Alpha) * label.lpForecastMemoryForecast + Alpha * forecast;
		// Log moments: +1 copy keeps a zero-sale LP finite. Equal weights (1 / n) until 1 / Alpha LPs, then Alpha.
		float x = MathF.Log((1f + sold) / (1f + forecast));
		float a = Math.Max(Alpha, 1f / (label.lpForecastObservations + 1));
		label.lpForecastLogMean = (1f - a) * label.lpForecastLogMean + a * x;
		label.lpForecastLogSquare = (1f - a) * label.lpForecastLogSquare + a * x * x;
		label.lpForecastObservations++;
		Observed?.Invoke(new Observation(label, record, Multiplier(label)));
	}

	public const string CsvHeader = "week,labelId,tier,recordId,ageWeeks,forecastUnits,soldUnits,observations,totalsRatio,logMean,logVariance,learnedRatio,multiplier";
	public static string CsvRow(int week, Observation o) {
		AILabel l = o.Label; RecordRuntimeData r = o.Record;
		string F(float v) => v.ToString("0.####", CultureInfo.InvariantCulture);
		float totals = l.lpForecastMemoryForecast > 0f ? l.lpForecastMemorySold / l.lpForecastMemoryForecast : 0f;
		float variance = LogVariance(l);
		return string.Join(",", week.ToString(CultureInfo.InvariantCulture), l.labelId, l.tier.ToString(), r.baseRecord?.recordId ?? "",
			r.weeksSinceRelease.ToString(CultureInfo.InvariantCulture), F(r.releaseTimeExpectedLpUnits), r.pressingBookSold.ToString(CultureInfo.InvariantCulture),
			l.lpForecastObservations.ToString(CultureInfo.InvariantCulture), F(totals), F(l.lpForecastLogMean), F(variance), F(LearnedRatio(l)), F(o.Multiplier));
	}
}
