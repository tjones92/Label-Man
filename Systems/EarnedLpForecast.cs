using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// --earned-lp-forecast (LpGenreAllocationFollowup.md §44). The LP release gate forecasts units from the act and the
/// market only, and that prior runs 1.5-2.9x low for Major and MidTier LPs while it is about right for small labels (§43).
/// Here each AI label learns its own correction. When one of its LPs reaches <see cref="ObserveAgeWeeks"/> weeks (or
/// retires sooner), the copies sold so far and the units the gate forecast for it at release join two exponentially
/// weighted totals. The gate then scales the label's LP unit forecast by their ratio. A ratio of totals, not a mean of
/// per-record ratios, so a mean-preserving reception draw (--lp-reception-sigma) does not bias it. The correction is
/// shrunk toward 1 by n / (n + <see cref="ShrinkRecords"/>) while the label has few observations. Copies to date at 52
/// weeks undercount an LP still selling, so the learned ratio is a lower bound for long sellers.
/// No random draws. Off: nothing is read or written and the gate is unchanged.
/// </summary>
public static class EarnedLpForecast {
	public const string Switch = "--earned-lp-forecast";
	public const int ObserveAgeWeeks = 52;
	public const float Alpha = .1f, ShrinkRecords = 4f, MinRatio = .25f, MaxRatio = 6f;
	private static bool configured, enabled;
	public static bool Enabled => enabled;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		enabled = (arguments ?? Array.Empty<string>()).Contains(Switch);
		configured = true;
	}
	internal static void SetForChecks(bool value) { configured = true; enabled = value; }

	/// <summary>The factor on the gate's LP unit forecast for this label; 1 while off or before any observation.</summary>
	public static float Multiplier(AILabel label) {
		if (!enabled || label == null || label.isPlayerOwned || label.lpForecastObservations <= 0 || label.lpForecastMemoryForecast <= 0f) return 1f;
		float ratio = Math.Clamp(label.lpForecastMemorySold / label.lpForecastMemoryForecast, MinRatio, MaxRatio);
		float weight = label.lpForecastObservations / (label.lpForecastObservations + ShrinkRecords);
		return MathF.Exp(weight * MathF.Log(ratio));
	}

	/// <summary>Folds an LP into its label's memory once: at <see cref="ObserveAgeWeeks"/> or at retirement, whichever
	/// comes first. Records without a release-time forecast (opening catalogue, player records) are skipped.</summary>
	public static void Observe(AILabel label, RecordRuntimeData record) {
		if (!enabled || label == null || label.isPlayerOwned || record?.baseRecord?.format != ReleaseFormat.Album
			|| record.lpForecastObserved || record.releaseTimeExpectedLpUnits <= 0f) return;
		record.lpForecastObserved = true;
		label.lpForecastMemorySold = (1f - Alpha) * label.lpForecastMemorySold + Alpha * Math.Max(0, record.pressingBookSold);
		label.lpForecastMemoryForecast = (1f - Alpha) * label.lpForecastMemoryForecast + Alpha * record.releaseTimeExpectedLpUnits;
		label.lpForecastObservations++;
	}
}
