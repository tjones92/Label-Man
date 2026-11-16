using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>Common LP purchase-propensity factor. Buyer pools, capacity and channel choice stay fixed. Values below 1
/// were the original trim; values above 1 (up to <see cref="Max"/>) restore LP volume once act-blind LP demand is earned
/// (GameyBehaviorAudit.md 2.6b): demand is then concentrated on known acts, so a common factor returns volume to them.</summary>
public static class LpDemandCalibration {
	public const string Switch = "--lp-demand-scale=";
	public const double Min = .9, Max = 3;
	/// <summary>--lp-demand-scale-stores-only (GameyBehaviorAudit.md 2.9b). Stores and racks share the retail purchase
	/// envelope; lifting rack and budget-autonomous demand with the scale lets racks take the envelope from stores. With
	/// this switch the scale applies to store intent only and rack and budget demand stay as calibrated.</summary>
	public const string StoresOnlySwitch = "--lp-demand-scale-stores-only";
	public static bool StoresOnly { get; private set; }
	/// <summary>--lp-demand-scale-known (GameyBehaviorAudit.md 2.12). A flat scale is regressive: a hit LP is held back by
	/// exhaustion and gains about 1.24x from 1.63x, while an unknown act's LP gains the whole factor. With this switch an
	/// act's LP takes store scale 1 + (S - 1) x Known, Known being <see cref="LpEarnedDiscovery.Known"/> on the act's chart
	/// heat and release-awareness bonus this week; an LP with no roster act takes S. Needs the scale.</summary>
	public const string KnownSwitch = "--lp-demand-scale-known";
	public static bool KnownWeighted { get; private set; }
	public static bool? SavedKnownWeighted => KnownWeighted ? true : null;
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
			throw new ArgumentException(Switch + " requires one finite value from .9 to 3.");
		Scale = values.Length == 0 ? 1 : double.Parse(values[0][Switch.Length..], CultureInfo.InvariantCulture);
		StoresOnly = arguments.Contains(StoresOnlySwitch);
		if (StoresOnly && !Enabled) throw new ArgumentException(StoresOnlySwitch + " requires " + Switch + " other than 1.");
		KnownWeighted = arguments.Contains(KnownSwitch);
		if (KnownWeighted && !Enabled) throw new ArgumentException(KnownSwitch + " requires " + Switch + " other than 1.");
		configured = true;
	}
	internal static float Apply(float intention) => Enabled ? (float)(intention * Scale) : intention;
	internal static double Apply(double intention) => Enabled ? intention * Scale : intention;
	/// <summary>The store scale for an LP whose act is <paramref name="known"/> known. Exactly <see cref="Scale"/> unless
	/// <see cref="KnownWeighted"/>.</summary>
	internal static double StoreScale(float known) => KnownWeighted ? 1 + (Scale - 1) * Math.Clamp(known, 0f, 1f) : Scale;
	/// <summary>Store intent of an LP. Identical to <see cref="Apply(float)"/> unless <see cref="KnownWeighted"/>.</summary>
	internal static float ApplyStore(float intention, float known) => Enabled ? (float)(intention * StoreScale(known)) : intention;
	/// <summary>Rack and budget-autonomous demand: scaled unless <see cref="StoresOnly"/>.</summary>
	internal static float ApplyNonStore(float demand) => StoresOnly ? demand : Apply(demand);
	internal static double ApplyNonStore(double demand) => StoresOnly ? demand : Apply(demand);
	internal static double BudgetDemand(double autonomous, double transfers) => ApplyNonStore(autonomous) + transfers;
	public static void RequireSaved(double? saved, bool? savedStoresOnly = null, bool? savedKnownWeighted = null) {
		if ((savedStoresOnly ?? false) != StoresOnly) throw new InvalidOperationException("Resume with the saved " + StoresOnlySwitch + " setting.");
		if ((savedKnownWeighted ?? false) != KnownWeighted) throw new InvalidOperationException("Resume with the saved " + KnownSwitch + " setting.");
		double scale = saved ?? 1;
		if (!double.IsFinite(scale) || scale < Min || scale > Max || scale != Scale)
			throw new InvalidOperationException("Resume with the saved LP demand scale.");
	}
	internal static void ResetForChecks() { configured = false; Scale = 1; StoresOnly = false; KnownWeighted = false; }
}
