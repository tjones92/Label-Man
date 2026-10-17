using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Default home-only starting territory for 1960 Small labels (SmallLabelAttritionAndChartMethod.md §12).
/// --no-small-home-territory retains the former multi-region factory comparison.
///
/// The factory gave a 1960 Small label its home region plus 0-2 more distribution regions, and a 40% chance of a
/// second "strong" (deep-stock, 1.35x launch) region next door. One of the model's regions is already a multi-state
/// territory, so a storefront label starts in its own: other markets come later, through houses or a deal. Runtime
/// births already start home-only (<see cref="RuntimeLabelProfileFactory"/>). The factory's draws are all still
/// made and then discarded, so every label after a Small one draws the same as before.
/// </summary>
public static class SmallLabelHomeTerritory {
	public const string Switch = "--small-home-territory";
	public const string DisableSwitch = "--no-small-home-territory";
	private static bool configured, enabled = true;
	public static bool Enabled => enabled;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var args = (arguments ?? Array.Empty<string>()).ToArray();
		enabled = !args.Contains(DisableSwitch);
		if (args.Contains(Switch) && !enabled) throw new ArgumentException("Conflicting Small home-territory switches.");
		configured = true;
	}

	internal static void SetEnabledForChecks(bool value) { configured = true; enabled = value; }

	/// <summary>Cuts an already-drawn Small label's territory to its home region. No draws.</summary>
	public static void Apply(AILabel label, LabelTier tier) {
		if (!enabled || label == null || tier != LabelTier.Small) return;
		string home = !string.IsNullOrEmpty(label.homeRegion) ? label.homeRegion : label.distributionRegions?.FirstOrDefault();
		if (string.IsNullOrEmpty(home)) return;
		label.distributionRegions = new[] { home };
		label.strongRegions = new[] { home };
	}
}
