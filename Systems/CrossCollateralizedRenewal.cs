using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Audit B7: an AI re-signing carries the act's unrecouped balance into the new contract.
///
/// Off (the default), a renewal sets unrecoupedAdvance to the new advance, wiping production charges
/// carried from the old deal. Period contracts cross-collateralized: the new advance was added to what
/// the act still owed. Wiping it made HasRecoupedCurrentContract (the performance-drop exemption)
/// easier to reach on a second deal and inflated those acts' royalty earnings. When on, the new
/// advance is added to the balance. A move to a different label still starts clean: the new label
/// does not inherit the old one's claim. The player's own re-signing path is unchanged.
/// </summary>
public static class CrossCollateralizedRenewal {
	public const string Switch = "--cross-collateralized-renewal";
	private static bool configured, enabled;
	public static bool Enabled => enabled;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var args = (arguments ?? Array.Empty<string>()).ToArray();
		enabled = args.Contains(Switch) || LabelCorporateModel.FullBundleRequested(args);
		configured = true;
	}

	internal static void SetForChecks(bool value) { configured = true; enabled = value; }

	internal static float RenewedBalance(float currentBalance, float newAdvance) =>
		enabled ? Math.Max(0f, currentBalance) + newAdvance : newAdvance;
}
