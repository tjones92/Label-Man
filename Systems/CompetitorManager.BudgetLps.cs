using System;
using System.Linq;

public partial class CompetitorManager {
	internal double BudgetLpManufacturingCost => GetPressingCostPerUnit(ReleaseFormat.Album) + BudgetLpChannel.Packaging;
	private float SettleBudgetLps(AILabel label, ChartManager.CompletedWeekSettlement settlement) {
		if (!BudgetLpChannel.Enabled || settlement.SettlementId <= 0) return 0;
		var sales = BudgetLpChannel.Sales(label.labelId).ToList();
		long units = sales.Sum(s => (long)s.Units);
		if (units == 0) return 0;
		// Own-master trade receipts, separate from club licences and original front-line records.
		// Simplified budget sleeve; eleven-track mechanical assumption is charged even in legacy cost mode.
		double gross = units * BudgetLpChannel.Price * BudgetLpChannel.WholesaleShare;
		double manufacturing = BudgetLpManufacturingCost;
		double cogs = units * (manufacturing + LabelPressingEconomics.ExpectedAlbumCompositionCost);
		double returns = RackJobberChannel.ReturnWriteOff((float)(units * manufacturing));
		double royalty = gross * BudgetLpChannel.RoyaltyRate;
		foreach (var sale in sales) {
			var artist = ArtistManager.Instance?.GetArtist(sale.Product.ArtistId ?? "");
			if (artist != null) artist.totalRoyaltyEarnings += (float)(sale.Units * BudgetLpChannel.Price *
				BudgetLpChannel.WholesaleShare * BudgetLpChannel.RoyaltyRate);
		}
		// Unknown artists in synthetic baskets still incur the same royalty expense.
		double net = gross - cogs - returns - royalty;
		LabelCompanyAccounts.RecordSale(label, gross, units);
		label.weeklyGrossRevenue += (float)gross;
		label.weeklyCogs += (float)(cogs + returns);
		label.weeklyArtistRoyalty += (float)royalty;
		label.weeklyNetRevenue += (float)net;
		BudgetLpChannel.RecordFinance(new BudgetLpChannel.FinanceRow { Week = settlement.SettlementId, Year = settlement.Date.year,
			OwnerId = label.labelId, Units = units, Gross = gross, Cogs = cogs, Returns = returns, Royalty = royalty });
		return (float)net;
	}
}
