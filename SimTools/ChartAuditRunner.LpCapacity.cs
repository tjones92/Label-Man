using System.IO;

public partial class ChartAuditRunner {
	private StreamWriter lpCapacityWriter;
	private void OpenLpCapacityOutputs(string directory) {
		if (!LpCapacityScreen.Audit) return;
		lpCapacityWriter = CreateWriter(Path.Combine(directory, $"{runName}-lp-capacity.csv"));
		lpCapacityWriter.WriteLine("week,year,regionId,programmeScale,purchaseShare,baseSingleCapacity,legacyAlbumCapacity,albumCapacity,legacyPurchaseCapacity,purchaseCapacity,singleBudget,albumBudget,singlesBeforeAddition,singlesAfterAddition,addedAlbumCapacity,addedAlbumCleared,albumCleared,rawAlbumDemand,serviceableAlbumDemand,exportBudget,importLimit");
	}
	private void WriteLpCapacityRows(int week, int year) {
		if (lpCapacityWriter == null) return;
		foreach (var s in ChartManager.Instance.GetLastMarketClearingSummaries())
			lpCapacityWriter.WriteLine(string.Join(",", week, year, Csv(s.RegionId), Exact(LpCapacityScreen.ProgrammeScale),
				Exact(LpCapacityScreen.PurchaseShare), s.BasePurchaseCapacity, s.LegacyAlbumChannelCapacity, s.AlbumChannelCapacity,
				s.LegacyPurchaseCapacity, s.PurchaseCapacity, s.SingleFormatBudget, s.AlbumFormatBudget, s.SingleBeforeLpAddition,
				s.ClearedSingleUnits, s.AddedAlbumCapacity, s.AddedAlbumCleared, s.ClearedAlbumUnits, Exact(s.RawAlbumDemand),
				Exact(s.ServiceableAlbumIntent), s.ExportBudget, s.ImportLimit));
	}
	private void CloseLpCapacityOutputs() { lpCapacityWriter?.Dispose(); lpCapacityWriter = null; }
}
