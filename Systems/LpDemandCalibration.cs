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
	/// <summary>--lp-demand-scale-stores-only (GameyBehaviorAudit.md 2.9b). Stores and racks share the retail purchase
	/// envelope; lifting rack and budget-autonomous demand with the scale lets racks take the envelope from stores. With
	/// this switch the scale applies to store intent only and rack and budget demand stay as calibrated.</summary>
	public const string StoresOnlySwitch = "--lp-demand-scale-stores-only";
	public static bool StoresOnly { get; private set; }
	public static bool? SavedStoresOnly => StoresOnly ? true : null;
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
		StoresOnly = arguments.Contains(StoresOnlySwitch);
		if (StoresOnly && !Enabled) throw new ArgumentException(StoresOnlySwitch + " requires " + Switch + " other than 1.");
		configured = true;
	}
	internal static float Apply(float intention) => Enabled ? (float)(intention * Scale) : intention;
	internal static double Apply(double intention) => Enabled ? intention * Scale : intention;
	/// <summary>Rack and budget-autonomous demand: scaled unless <see cref="StoresOnly"/>.</summary>
	internal static float ApplyNonStore(float demand) => StoresOnly ? demand : Apply(demand);
	internal static double ApplyNonStore(double demand) => StoresOnly ? demand : Apply(demand);
	internal static double BudgetDemand(double autonomous, double transfers) => ApplyNonStore(autonomous) + transfers;
	public static void RequireSaved(double? saved, bool? savedStoresOnly = null) {
		if ((savedStoresOnly ?? false) != StoresOnly) throw new InvalidOperationException("Resume with the saved " + StoresOnlySwitch + " setting.");
		double scale = saved ?? 1;
		if (!double.IsFinite(scale) || scale < Min || scale > Max || scale != Scale)
			throw new InvalidOperationException("Resume with the saved LP demand scale.");
	}
	internal static void ResetForChecks() { configured = false; Scale = 1; StoresOnly = false; }
}
