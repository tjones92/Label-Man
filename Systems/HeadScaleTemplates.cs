using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Calibration probe (SimTools/HistoricalLabelTierBalanceResearch.md §17l), off unless named.
/// --seed-head-templates seeds the national non-major hitmakers of 1960 at head scale.
///
/// The Big-5 hold about 9 points too much of the 1961-64 Top 10 collectively (§17k) because only five firms have
/// head-scale launch access (§11). History's second rank was MGM, ABC-Paramount, Warner Bros., Cameo-Parkway and
/// Liberty: about 4.5% of the 1960-62 Top 10 each, two-thirds to three-quarters of a Big-5 operating label. With the
/// switch on, <see cref="AILabelFactory"/> adds MGM, ABC-Paramount and Warner Bros. after the existing templates,
/// raises Cameo-Parkway and Liberty to the head envelope, and moves Vee-Jay to a standard MidTier. All six are
/// MidTier, so none becomes a Major distributor or enters a tier-based Major share; the Big-5 keep label_0001-0005.
/// The switch reserves no chart slots and scripts no records: it only sets starting traits.
/// </summary>
public static class HeadScaleTemplates {
	public const string Switch = "--seed-head-templates";
	private static bool configured;
	private static bool enabled;
	public static bool Enabled => enabled;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		enabled = (arguments ?? Array.Empty<string>()).Any(arg => arg == Switch);
		configured = true;
	}

	internal static void SetEnabledForChecks(bool value) { configured = true; enabled = value; }
}
