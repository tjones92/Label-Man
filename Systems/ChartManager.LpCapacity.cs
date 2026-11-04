using System;
using System.Collections.Generic;
using System.Linq;

public partial class ChartManager {
	private static void ApplyAdditionalLpCapacity(List<MarketIntent> intents, MarketClearingRegionalSummary summary) {
		summary.LegacyAlbumChannelCapacity = summary.AlbumChannelCapacity;
		summary.LegacyPurchaseCapacity = summary.PurchaseCapacity;
		summary.SingleBeforeLpAddition = intents.Where(i => i.Record.baseRecord.format == ReleaseFormat.Single).Sum(i => i.Cleared);
		int added = LpCapacityScreen.AdditionalAlbumCapacity(summary.BasePurchaseCapacity, summary.AlbumChannelCapacity);
		if (added == 0) return;
		var albums = intents.Where(i => i.Record.baseRecord.format == ReleaseFormat.Album).ToList();
		int before = albums.Sum(i => i.Cleared);
		int residual = albums.Sum(i => i.Serviceable - i.Cleared);
		int allocation = Math.Min(added, residual);
		AllocateProportionalSpillover(albums, allocation, spillover: false);
		summary.AddedAlbumCapacity = added; summary.AddedAlbumCleared = albums.Sum(i => i.Cleared) - before;
		summary.AlbumChannelCapacity += added;
		summary.AlbumFormatBudget += allocation;
		summary.PurchaseCapacity += added;
		summary.LocalClearedAlbumUnits += summary.AddedAlbumCleared;
		summary.LocalClearedUnits += summary.AddedAlbumCleared;
		summary.UnusedAfterLocal += added - summary.AddedAlbumCleared;
		// Export/import limits and every single allocation remain the legacy pass's values. Unspent LP additions
		// are not exportable to singles or another region. Extra sales still need serviceable demand and stock.
	}

	internal static (int Single, int Album, int Added, int Export, int Import) LpAllocationForChecks(int albumDemand) {
		MarketIntent I(string id, ReleaseFormat format, int demand, int cleared) => new() {
			Record = new RecordRuntimeData(new Record { recordId = id, format = format }),
			Data = new RegionalRecordData("native") { localClearedThisWeek = cleared }, Serviceable = demand, Cleared = cleared
		};
		var intents = new List<MarketIntent> { I("single", ReleaseFormat.Single, 200, 80), I("album", ReleaseFormat.Album, albumDemand, 40) };
		var summary = new MarketClearingRegionalSummary { BasePurchaseCapacity = 100, AlbumChannelCapacity = 40, PurchaseCapacity = 140,
			AlbumFormatBudget = 40, SingleFormatBudget = 80, LocalClearedSingleUnits = 80, LocalClearedAlbumUnits = 40,
			LocalClearedUnits = 120, UnusedAfterLocal = 20, ExportBudget = 15, ImportLimit = 21 };
		ApplyAdditionalLpCapacity(intents, summary);
		return (intents[0].Cleared, intents[1].Cleared, summary.AddedAlbumCleared, summary.ExportBudget, summary.ImportLimit);
	}
}
