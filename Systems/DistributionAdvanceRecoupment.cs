using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Audit B3: a distribution advance is a loan against the client's receipts, not a gift.
///
/// On the legacy path, the advance moves from distributor to client cash and is "recouped"
/// only out of the distributor's own skim, which the distributor takes whether or not the
/// advance is repaid, so nothing ever comes back. When live:
///
/// - The skim is the distributor's fee and no longer counts as repayment.
/// - Each week the distributor withholds the client's receipts on records it carries (the
///   granted-region share of each record's label net) until the balance is zero, exactly as
///   artist royalties are withheld against an artist advance. This is financing, not P&amp;L:
///   the client's revenue and monthly profit are unchanged, only the cash is withheld, just
///   as the advance arrived as cash without income.
/// - Renewal carries the balance (the deal object persists). On a voluntary end (Exit,
///   Graduated) the client settles from cash above three months' overhead, the same reserve
///   a distributor keeps when it pays an advance, and the rest is written off. A poaching
///   Major buys the balance out and adds it to the new deal's advance (cross-collateralized).
///   A distributor that drops or absorbs the client, or collapses, and a client that closes,
///   forgive it.
///
/// No new persisted state: the balance already lives on DistributionDeal.unrecoupedAdvance.
/// </summary>
public static class DistributionAdvanceRecoupment {
	public const string Switch = "--distribution-advance-recoupment";
	internal const float ExitReserveMonths = 3f;
	private static bool configured, enabled;
	public static bool Enabled => enabled;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var args = (arguments ?? Array.Empty<string>()).ToArray();
		// Company accounts require repayments to be financing rather than service fees.
		enabled = args.Contains(Switch) || args.Contains("--label-corporate-model") || LabelCorporateModel.DefaultBundleRequested(args);
		configured = true;
	}

	internal static void SetForChecks(bool value) { configured = true; enabled = value; }

	/// <summary>Share of a record's sales the deal carried this week: the skim's own granted-region fraction.</summary>
	internal static float CarriedShare(DistributionDeal deal, float skimFraction) =>
		deal == null || deal.marginSkim <= 0f ? 0f : Math.Clamp(skimFraction / deal.marginSkim, 0f, 1f);

	/// <summary>What the distributor withholds this week: the balance, capped by carried receipts and by what was banked.</summary>
	internal static float Withhold(float balance, float carriedNet, float bankedRevenue) =>
		Math.Max(0f, Math.Min(balance, Math.Min(carriedNet, bankedRevenue)));

	/// <summary>A voluntary exit's settlement: cash above the reserve, up to the balance.</summary>
	internal static float ExitSettlement(float balance, float clientCash, float monthlyOverhead) =>
		Math.Max(0f, Math.Min(balance, clientCash - monthlyOverhead * ExitReserveMonths));
}

public sealed class DistributionAdvanceRecoupmentTelemetry {
	public int Week;
	/// <summary>Withheld, Settled, BoughtOut, WrittenOff or Forgiven.</summary>
	public string Kind;
	public string ClientId, DistributorId, Resolution;
	public float Amount, CarriedNet, BalanceAfter, Advance;
}
