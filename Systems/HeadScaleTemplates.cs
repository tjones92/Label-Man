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
/// raises Cameo-Parkway to the head envelope, and moves Liberty and Vee-Jay to a standard MidTier (§17s: both had
/// their head years after 1960). All six are MidTier, so none becomes a Major distributor or enters a tier-based
/// Major share; the Big-5 keep label_0001-0005. None may promote to Major in its first year (a stand-in, see below).
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

	// STAND-IN, NOT THE FIX (§17s). In the §17r screen Liberty promoted MidTier -> Major at week 48 of 1960 in all five
	// seeds, and Warner Bros. in two (two more in 1962). Neither was a historical major. The MidTier -> Major gate's
	// only chart evidence is 16 Hot 100 records in 52 weeks, a count that head-envelope release volume clears on
	// launches alone; it reads nothing of head performance (Top 10 slots, chart share) or of more than one year.
	// Until that gate is given real chart-performance evidence -- which it needs for every label, not only these --
	// the probe templates simply may not promote to Major in their first year. Remove this once the gate is fixed.
	internal const int MajorPromotionHoldMonths = 12;

	internal static bool HoldsMajorPromotion(AILabel label) =>
		enabled && label != null && label.isHistorical && label.monthsActive < MajorPromotionHoldMonths &&
		AILabelFactory.IsHeadScaleProbeTemplate(label.labelName);
}
