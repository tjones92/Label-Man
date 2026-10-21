using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// AI survival follows paid operating costs and liquidity, rather than chart rank or a mortality roll.
/// Recent cash flow includes production, advances and actual wholesale collections. It is a budgeting
/// estimate, not extra revenue. The player's credit line remains owned by PlayerDesk.
/// </summary>
public static class EconomicLabelLifecycle {
	private static bool configured, legacy;
	public static bool Enabled => configured && !legacy && ArtistPopulationLifecycle.Enabled && GenreMarketV2.Enabled;
	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var args = (arguments ?? Array.Empty<string>()).ToArray();
		legacy = args.Contains("--legacy-label-exits");
		if (legacy && args.Contains("--economic-label-exits"))
			throw new ArgumentException("--legacy-label-exits and --economic-label-exits cannot be used together.");
		configured = true;
	}

	internal static void ReviewMonth(AILabel label, float netCashFlow, bool allowBankruptcy) {
		if (label.isPlayerOwned) return;
		label.recentMonthlyCashFlows ??= new List<float>();
		label.recentMonthlyCashFlows.Add(netCashFlow);
		if (label.recentMonthlyCashFlows.Count > 3) label.recentMonthlyCashFlows.RemoveAt(0);
		label.insolventMonthEnds = label.cashReserves < 0f ? label.insolventMonthEnds + 1 : 0;
		float overhead = Math.Max(1f, label.GetMonthlyOverhead());
		float burn = Math.Max(0f, -label.RecentMonthlyCashFlow);
		// Cash below zero represents bills already paid on the firm's overdraft. One monthly
		// review is allowed for existing records/receivables to pay it back; two consecutive
		// negative month ends exhaust that grace. Receivables are never treated as spendable cash.
		if (label.cashReserves < 0f) {
			label.status = allowBankruptcy && !label.isPlayerOwned && label.insolventMonthEnds >= 2
				? LabelStatus.Bankrupt : LabelStatus.Dying;
			return;
		}
		float runway = burn > 0f ? label.cashReserves / burn : float.PositiveInfinity;
		if (label.cashReserves < overhead || runway < 1f) label.status = LabelStatus.Dying;
		else if (label.cashReserves < 3f * overhead || runway < 3f) label.status = LabelStatus.Struggling;
		else label.status = label.RecentMonthlyCashFlow > 2f * overhead ? LabelStatus.Rising : LabelStatus.Stable;
	}

	/// <summary>Cash kept out of new bets. A losing firm reduces new commitments before it misses bills.
	/// Under --plant-credit, cash already owed to the plant is not spendable either.</summary>
	internal static float CommitmentReserve(AILabel label, bool signing) {
		float reserve = Math.Max(label.GetMonthlyOverhead() * (signing ? 2f : 1f),
			Math.Max(0f, -label.RecentMonthlyCashFlow) * (signing ? 3f : 2f));
		if (LabelPressingEconomics.PlantCredit) reserve += label.outstandingPlantPayables;
		return reserve + (float)LabelCorporateModel.TaxAndServiceReserve(label);
	}
}
