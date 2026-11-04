using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>Opt-in LP catalogue sensitivity, plus observational demand shadows. No RNG or saved fields.</summary>
public static class LpDemandProbe {
	public const string AuditSwitch = "--lp-demand-audit", DecaySwitch = "--lp-catalog-weekly-decay=";
	public const float LegacyDecay = .93f, SlowScenarioDecay = .985f;
	public static float WeeklyDecay { get; private set; } = LegacyDecay;
	public static bool Audit { get; private set; }
	private static bool configured;
	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var args = (arguments ?? Array.Empty<string>()).ToArray();
		var values = args.Where(a => a.StartsWith(DecaySwitch, StringComparison.Ordinal)).ToArray();
		if (values.Length > 1 || values.Length == 1 && (!float.TryParse(values[0][DecaySwitch.Length..],
			NumberStyles.Float, CultureInfo.InvariantCulture, out float value) || !float.IsFinite(value) || value < LegacyDecay || value > 1))
			throw new ArgumentException(DecaySwitch + " requires one finite value from .93 to 1.");
		WeeklyDecay = values.Length == 0 ? LegacyDecay : float.Parse(values[0][DecaySwitch.Length..], CultureInfo.InvariantCulture);
		Audit = args.Contains(AuditSwitch) || values.Length > 0;
		configured = true;
	}
	// A frozen-state raw-demand shadow, not a purchase allocation or a lifetime forecast.
	internal static double Rescale(double raw, double actualFactor, double alternativeFactor) {
		if (raw == 0) return 0;
		if (!double.IsFinite(raw) || raw < 0 || !double.IsFinite(actualFactor) || actualFactor <= 0 ||
			!double.IsFinite(alternativeFactor) || alternativeFactor < 0) throw new InvalidOperationException("Invalid LP demand shadow.");
		return raw / actualFactor * alternativeFactor;
	}
	internal static void ResetForChecks() { configured = false; WeeklyDecay = LegacyDecay; Audit = false; }
}
