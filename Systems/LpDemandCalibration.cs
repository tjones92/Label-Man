using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>Common LP purchase-propensity factor. Buyer pools, capacity and channel choice stay fixed.</summary>
public static class LpDemandCalibration {
	public const string Switch = "--lp-demand-scale=";
	public static double Scale { get; private set; } = 1;
	public static bool Enabled => Scale != 1;
	public static double? SavedScale => Enabled ? Scale : null;
	private static bool configured;
	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var values = arguments.Where(a => a.StartsWith(Switch, StringComparison.Ordinal)).ToArray();
		if (values.Length > 1 || values.Length == 1 && (!double.TryParse(values[0][Switch.Length..],
			NumberStyles.Float, CultureInfo.InvariantCulture, out double value) || !double.IsFinite(value) || value < .9 || value > 1))
			throw new ArgumentException(Switch + " requires one finite value from .9 to 1.");
		Scale = values.Length == 0 ? 1 : double.Parse(values[0][Switch.Length..], CultureInfo.InvariantCulture);
		configured = true;
	}
	internal static float Apply(float intention) => Enabled ? (float)(intention * Scale) : intention;
	internal static double Apply(double intention) => Enabled ? intention * Scale : intention;
	internal static double BudgetDemand(double autonomous, double transfers) => Apply(autonomous) + transfers;
	public static void RequireSaved(double? saved) {
		double scale = saved ?? 1;
		if (!double.IsFinite(scale) || scale < .9 || scale > 1 || scale != Scale)
			throw new InvalidOperationException("Resume with the saved LP demand scale.");
	}
	internal static void ResetForChecks() { configured = false; Scale = 1; }
}
