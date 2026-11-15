using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>Common LP purchase-propensity factor. Buyer pools, capacity and channel choice stay fixed. Values below 1
/// were the original trim; values above 1 (up to <see cref="Max"/>) restore LP volume once act-blind LP demand is earned
/// (GameyBehaviorAudit.md 2.6b): demand is then concentrated on known acts, so a common factor returns volume to them.</summary>
public static class LpDemandCalibration {
	public const string Switch = "--lp-demand-scale=";
	public const double Min = .9, Max = 2;
	public static double Scale { get; private set; } = 1;
	public static bool Enabled => Scale != 1;
	public static double? SavedScale => Enabled ? Scale : null;
	private static bool configured;
	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var values = arguments.Where(a => a.StartsWith(Switch, StringComparison.Ordinal)).ToArray();
		if (values.Length > 1 || values.Length == 1 && (!double.TryParse(values[0][Switch.Length..],
			NumberStyles.Float, CultureInfo.InvariantCulture, out double value) || !double.IsFinite(value) || value < Min || value > Max))
			throw new ArgumentException(Switch + " requires one finite value from .9 to 2.");
		Scale = values.Length == 0 ? 1 : double.Parse(values[0][Switch.Length..], CultureInfo.InvariantCulture);
		configured = true;
	}
	internal static float Apply(float intention) => Enabled ? (float)(intention * Scale) : intention;
	internal static double Apply(double intention) => Enabled ? intention * Scale : intention;
	internal static double BudgetDemand(double autonomous, double transfers) => Apply(autonomous) + transfers;
	public static void RequireSaved(double? saved) {
		double scale = saved ?? 1;
		if (!double.IsFinite(scale) || scale < Min || scale > Max || scale != Scale)
			throw new InvalidOperationException("Resume with the saved LP demand scale.");
	}
	internal static void ResetForChecks() { configured = false; Scale = 1; }
}
