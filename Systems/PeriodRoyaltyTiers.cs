using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Audit B4: royalty rates ordered by the label's ability to pay, not against it.
///
/// Off (the default), base rates run Major 3%, MidTier 5%, Independent 8%, Small 10%, so the
/// weakest labels win contested acts on royalty (artist choice weights it at 0.15) and acts
/// earn most at the labels that could least afford it. In period practice the majors paid the
/// standard 3-5% and paid established acts the most; independents paid 2-3%, often less.
/// When on, the base runs Major 4%, MidTier 3.5%, Independent and Boutique 3%, Small 2.5%,
/// and career leverage (unchanged: up to 8 points for a Superstar) is scaled by what the tier
/// can carry, so a star's rate climbs fastest at a Major. A big label's pull still comes mainly
/// from promotion and distribution; royalty no longer runs against it.
/// </summary>
public static class PeriodRoyaltyTiers {
	public const string Switch = "--period-royalty-tiers";
	private static bool configured, enabled;
	public static bool Enabled => enabled;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		enabled = (arguments ?? Array.Empty<string>()).Contains(Switch);
		configured = true;
	}

	internal static void SetForChecks(bool value) { configured = true; enabled = value; }

	internal static float BaseRate(LabelTier tier) => tier switch {
		LabelTier.Major => .04f, LabelTier.MidTier => .035f, LabelTier.Independent => .03f,
		LabelTier.Boutique => .03f, LabelTier.Small => .025f, _ => .03f
	};

	/// <summary>Share of an act's career leverage the tier can pay.</summary>
	internal static float LeverageShare(LabelTier tier) => tier switch {
		LabelTier.Major => 1f, LabelTier.MidTier => .8f, LabelTier.Independent => .6f,
		LabelTier.Boutique => .6f, LabelTier.Small => .4f, _ => .6f
	};
}
