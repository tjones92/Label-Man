using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Godot;

public partial class ChartAuditRunner {
	private string budgetProductDirectory;
	private StreamWriter budgetLpWriter;
	private void OpenBudgetLpAudit(string directory) {
		if (BudgetLpChannel.Enabled) budgetProductDirectory = directory;
		if (!OS.GetCmdlineUserArgs().Contains(BudgetLpCatalogue.AuditSwitch)) return;
		if (!RecordClubChannel.Enabled || !RackJobberChannel.Enabled || !GenreMarketV2.Enabled)
			throw new ArgumentException(BudgetLpCatalogue.AuditSwitch + " requires --record-clubs, --rack-jobbers and the genre-market-v2 settlement.");
		budgetLpWriter = CreateWriter(Path.Combine(directory, $"{runName}-budget-lp-supply.csv"));
		budgetLpWriter.WriteLine("week,year,labelId,labelTier,artistId,sourceKind,knownTitles,appeal,adultAppeal");
	}
	private void CloseBudgetProducts() {
		if (budgetProductDirectory == null) return;
		void Write(string suffix, string header, System.Collections.Generic.IEnumerable<object[]> rows) {
			using var writer = CreateWriter(Path.Combine(budgetProductDirectory, $"{runName}-{suffix}.csv"));
			writer.WriteLine(header);
			foreach (var row in rows) writer.WriteLine(string.Join(",", row.Select(v => Csv(Convert.ToString(v, CultureInfo.InvariantCulture)))));
		}
		Write("budget-region-weekly", "week,year,regionId,shelf,frontUnits,budgetUnits,switchedStoreUnits,budgetOffers,budgetDemand",
			BudgetLpChannel.Regions.Select(r => new object[] { r.Week, r.Year, r.RegionId, r.Shelf, r.FrontUnits, r.BudgetUnits, r.Switched, r.BudgetOffers, r.Demand }));
		Write("lp-channel-choice-weekly", "week,year,regionId,storeBefore,frontPlanned,frontSwitched,budgetSwitched,storeAfter",
			LpChannelExperiment.Rows.Select(r => new object[] { r.Week, r.Year, r.RegionId, r.StoreBefore, r.FrontPlanned, r.FrontSwitched, r.BudgetSwitched, r.StoreAfter }));
		Write("budget-product-weekly", "week,year,regionId,productId,ownerId,provenance,genre,demand,units,switchedStoreUnits",
			BudgetLpChannel.Products.Select(r => new object[] { r.Week, r.Year, r.RegionId, r.ProductId, r.OwnerId, r.Provenance, r.Genre, r.Demand, r.Units, r.Switched }));
		Write("budget-finance-weekly", "week,year,ownerId,units,gross,cogs,royalty,returnWriteOff,net",
			BudgetLpChannel.Finance.Select(r => new object[] { r.Week, r.Year, r.OwnerId, r.Units, r.Gross, r.Cogs, r.Royalty, r.Returns, r.Gross - r.Cogs - r.Royalty - r.Returns }));
		Write("budget-product-catalogue", "productId,ownerId,sourceId,artistId,provenance,genre,releaseWeek,appeal,quality,units",
			(BudgetLpChannel.State?.Products.Values ?? Enumerable.Empty<BudgetLpProduct>()).OrderBy(p => p.Id, StringComparer.Ordinal)
			.Select(p => new object[] { p.Id, p.OwnerId, p.SourceId, p.ArtistId, p.Provenance, p.Genre, p.ReleaseWeek, p.Appeal, p.Quality, p.RegionUnits.Values.Sum() }));
	}
	private void WriteBudgetLpAudit(ChartManager.CompletedWeekSettlement settlement) {
		if (budgetLpWriter == null) return;
		var labels = ChartManager.Instance.GetAllLabels();
		var tiers = labels.Where(l => l != null).GroupBy(l => l.labelId).ToDictionary(g => g.Key, g => g.First().tier.ToString());
		// Pre-retirement boundary: this week's retiring LPs still live here and have not yet
		// folded into the retired ledger. No runtime reference is retained in the output.
		foreach (var source in BudgetLpCatalogue.Snapshot(RecordClubChannel.State, labels,
			ChartManager.Instance.GetAllRecords(), settlement.Date.year))
			budgetLpWriter.WriteLine(string.Join(",", settlement.SettlementId.ToString(CultureInfo.InvariantCulture),
				settlement.Date.year.ToString(CultureInfo.InvariantCulture), Csv(source.LabelId), Csv(tiers[source.LabelId]),
				Csv(source.ArtistId), source.Kind, source.Titles?.ToString(CultureInfo.InvariantCulture) ?? "",
				source.Appeal.ToString("R", CultureInfo.InvariantCulture), source.AdultAppeal.ToString("R", CultureInfo.InvariantCulture)));
	}
}
