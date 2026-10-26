using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

public partial class ChartAuditRunner {
	// Scalars only: the census must not keep retired runtime objects alive.
	private sealed class DemandObservation {
		public string Id, LabelId, ArtistId, Format, Genre, Kind, ObservedTier, LaunchTier, Quality;
		public int ObservedWeek, ObservedYear, RetiredWeek = -1;
	}
	private readonly Dictionary<string, DemandObservation> demandObservations = new(StringComparer.Ordinal);
	private StreamWriter demandCensusWriter, demandWindowWriter;
	private StreamWriter physicalInventoryWriter;
	private bool demandObservationStarted, demandObservationCompleted;
	private int demandObservationStartWeek;

	private string offChartSinglesPath;
	private string recordClubPath, recordClubCataloguePath;
	private string rackRegionPath, rackTitlePath;
	private StreamWriter labelSalesWriter;

	private void OpenDemandObservationOutputs(string directory) {
		OpenBudgetLpAudit(directory);
		OpenRackValidation(directory);
		if (GenreMarketV2.Enabled) {
			labelSalesWriter = CreateWriter(Path.Combine(directory, $"{runName}-label-sales-weekly.csv"));
			labelSalesWriter.WriteLine("week,year,labelId,format,units,chartedUnits,top10Units");
		}
		if (MajorOffChartSingles.Enabled) offChartSinglesPath = Path.Combine(directory, $"{runName}-offchart-singles.csv");
		if (RecordClubChannel.Enabled) {
			recordClubPath = Path.Combine(directory, $"{runName}-record-club-annual.csv");
			recordClubCataloguePath = Path.Combine(directory, $"{runName}-record-club-catalogue.csv");
		}
		if (RackJobberChannel.Enabled) {
			rackRegionPath = Path.Combine(directory, $"{runName}-rack-region-weekly.csv");
			rackTitlePath = Path.Combine(directory, $"{runName}-rack-title-weekly.csv");
		}
		if (PhysicalPressingInventory.Enabled) {
			physicalInventoryWriter = CreateWriter(Path.Combine(directory, $"{runName}-physical-inventory.csv"));
			physicalInventoryWriter.WriteLine("week,recordId,labelId,format,status,openingUnits,productionReceived,unitsPressed,bookSold,warehouse,shelves,heldForArrival,shipments,saleableReturns,heldReturns,promotionalWithdrawals,damagedCopies,terminalWriteOffs,distributedWriteOffs,productionOrdered,productionDueWeek,unbilledInitial,unbilledRepress,unfilledShipmentRequests,reconciliationDelta");
		}
		demandCensusWriter = CreateWriter(Path.Combine(directory, $"{runName}-demand-release-census.csv"));
		demandCensusWriter.WriteLine("recordId,labelId,artistId,format,genre,observationKind,observedAuditWeek,observedYear,observedTier,launchTier,runtimeQualityAtLaunch,status,retiredAuditWeek");
		demandWindowWriter = CreateWriter(Path.Combine(directory, $"{runName}-demand-observation-window.csv"));
		demandWindowWriter.WriteLine("startAuditWeek,endAuditWeek,completed,openingRecords,launchedRecords,activeRecords,retiredRecords,missingRecords");
	}

	private void BeginDemandObservation() {
		demandObservationStartWeek = firstAuditWeek - 1;
		foreach (var record in ChartManager.Instance.GetAllRecords())
			ObserveDemandRecord(record, ChartManager.Instance.GetLabelById(record.baseRecord.labelId), opening: true);
		ChartManager.Instance.OnRecordReleased += ObserveDemandLaunch;
		ChartManager.Instance.OnRecordRetired += ObserveDemandRetirement;
		demandObservationStarted = true;
	}

	private void ObserveDemandLaunch(RecordRuntimeData record, AILabel label) => ObserveDemandRecord(record, label, opening: false);

	private void ObserveDemandRecord(RecordRuntimeData runtime, AILabel label, bool opening) {
		var record = runtime.baseRecord;
		if (string.IsNullOrEmpty(record.recordId) || demandObservations.ContainsKey(record.recordId))
			throw new InvalidOperationException($"Demand census: missing or repeated release identity {record.recordId}.");
		label ??= ChartManager.Instance.GetLabelById(record.labelId);
		demandObservations.Add(record.recordId, new DemandObservation {
			Id = record.recordId, LabelId = record.labelId, ArtistId = record.artistId,
			Format = record.format.ToString(), Genre = record.primaryGenre.ToString(),
			Kind = opening ? "Opening" : "Launch", ObservedWeek = opening ? demandObservationStartWeek : currentAuditWeek,
			ObservedYear = TimeManager.Instance.CurrentDate.year, ObservedTier = label?.tier.ToString() ?? "Unknown",
			LaunchTier = opening ? "" : label?.tier.ToString() ?? "Unknown",
			Quality = opening ? "" : Exact(runtime.GetQuality())
		});
	}

	private void ObserveDemandRetirement(RecordRuntimeData record) {
		WritePhysicalInventory(record, "Retired");
		if (!demandObservations.TryGetValue(record.baseRecord.recordId, out var observation) || observation.RetiredWeek >= 0)
			throw new InvalidOperationException($"Demand census: unobserved or repeated retirement {record.baseRecord.recordId}.");
		observation.RetiredWeek = currentAuditWeek;
	}

	private void CloseDemandObservationOutputs() {
		CloseBudgetProducts();
		CloseRackValidation();
		budgetLpWriter?.Dispose();
		budgetLpWriter = null;
		labelSalesWriter?.Dispose();
		labelSalesWriter = null;
		physicalInventoryWriter?.Dispose();
		physicalInventoryWriter = null;
		if (offChartSinglesPath != null) {
			// Cumulative over the process, not the observation window: a resumed world starts from zero.
			using StreamWriter writer = CreateWriter(offChartSinglesPath);
			writer.WriteLine("labelId,tier,units,gross,netAfterSellingCosts");
			foreach (var row in MajorOffChartSingles.Cumulative)
				writer.WriteLine(string.Join(",", Csv(row.LabelId), Csv(row.Tier), row.Units.ToString(CultureInfo.InvariantCulture),
					Exact((float)row.Gross), Exact((float)row.Net)));
			offChartSinglesPath = null;
		}
		if (recordClubPath != null) {
			// Cumulative over the process by calendar year; the catalogue rows are January snapshots.
			using (StreamWriter writer = CreateWriter(recordClubPath)) {
				writer.WriteLine("year,labelId,tier,role,units,gross,net,artistRoyalty");
				foreach (var row in RecordClubChannel.Annual)
					writer.WriteLine(string.Join(",", row.Year.ToString(CultureInfo.InvariantCulture), Csv(row.LabelId), Csv(row.Tier), row.Role,
						row.Units.ToString(CultureInfo.InvariantCulture), Exact((float)row.Gross), Exact((float)row.Net), Exact((float)row.ArtistRoyalty)));
			}
			using (StreamWriter writer = CreateWriter(recordClubCataloguePath)) {
				writer.WriteLine("year,labelId,role,licensedTo,entries,titles,earnedAppeal,seedAppeal,totalAppeal");
				foreach (var row in RecordClubChannel.Catalogue)
					writer.WriteLine(string.Join(",", row.Year.ToString(CultureInfo.InvariantCulture), Csv(row.LabelId), row.Role, Csv(row.LicensedTo),
						row.Entries.ToString(CultureInfo.InvariantCulture), row.Titles.ToString(CultureInfo.InvariantCulture),
						Exact((float)row.EarnedAppeal), Exact((float)row.SeedAppeal), Exact((float)row.TotalAppeal)));
			}
			recordClubPath = recordClubCataloguePath = null;
		}
		if (rackRegionPath != null) {
			// Cumulative over the process: a resumed world starts from zero.
			using (StreamWriter writer = CreateWriter(rackRegionPath)) {
				writer.WriteLine("week,year,regionId,pool,shelf,hitTitles,catalogueTitles,demand,units,hitUnits,catalogueUnits,unshippedUnits,shelfBound");
				foreach (var row in RackJobberChannel.RegionRows)
					writer.WriteLine(string.Join(",", row.Week.ToString(CultureInfo.InvariantCulture), row.Year.ToString(CultureInfo.InvariantCulture),
						Csv(row.RegionId), Exact(row.Pool), row.Shelf.ToString(CultureInfo.InvariantCulture), row.HitTitles.ToString(CultureInfo.InvariantCulture),
						row.CatalogueTitles.ToString(CultureInfo.InvariantCulture), Exact(row.Demand), row.Units.ToString(CultureInfo.InvariantCulture),
						row.HitUnits.ToString(CultureInfo.InvariantCulture), row.CatalogueUnits.ToString(CultureInfo.InvariantCulture),
						row.UnshippedUnits.ToString(CultureInfo.InvariantCulture), row.ShelfBound ? "1" : "0"));
			}
			using (StreamWriter writer = CreateWriter(rackTitlePath)) {
				writer.WriteLine("week,year,recordId,labelId,labelTier,regionId,tier,rank,exposure,demand,units,storeUnits,age,chartPosition,labelShips");
				foreach (var row in RackJobberChannel.TitleRows)
					writer.WriteLine(string.Join(",", row.Week.ToString(CultureInfo.InvariantCulture), row.Year.ToString(CultureInfo.InvariantCulture),
						Csv(row.RecordId), Csv(row.LabelId), Csv(row.LabelTier), Csv(row.RegionId), row.Tier.ToString(CultureInfo.InvariantCulture),
						row.Rank.ToString(CultureInfo.InvariantCulture), Exact(row.Exposure), Exact(row.Demand), row.Units.ToString(CultureInfo.InvariantCulture),
						row.StoreUnits.ToString(CultureInfo.InvariantCulture), row.Age.ToString(CultureInfo.InvariantCulture),
						row.ChartPosition.ToString(CultureInfo.InvariantCulture), row.LabelShips ? "1" : "0"));
			}
			rackRegionPath = rackTitlePath = null;
		}
		if (ChartManager.Instance != null) {
			ChartManager.Instance.OnRecordReleased -= ObserveDemandLaunch;
			ChartManager.Instance.OnRecordRetired -= ObserveDemandRetirement;
		}
		if (demandCensusWriter == null) return;
		if (demandObservationStarted) {
			var active = ChartManager.Instance.GetAllRecords().Select(r => r.baseRecord.recordId).ToHashSet(StringComparer.Ordinal);
			// Detect bypasses of ReleaseRecord too: a live identity absent from the census
			// must fail reconciliation instead of silently shrinking the denominator.
			int retired = 0, missing = active.Count(id => !demandObservations.ContainsKey(id)), activeCount = 0;
			foreach (var row in demandObservations.Values.OrderBy(r => r.Id, StringComparer.Ordinal)) {
				string status = row.RetiredWeek >= 0 && !active.Contains(row.Id) ? "Retired"
					: row.RetiredWeek < 0 && active.Contains(row.Id) ? "Active" : "Missing";
				if (status == "Retired") retired++; else if (status == "Active") activeCount++; else missing++;
				demandCensusWriter.WriteLine(string.Join(",", new[] {
					Csv(row.Id), Csv(row.LabelId), Csv(row.ArtistId), Csv(row.Format), Csv(row.Genre), row.Kind,
					row.ObservedWeek.ToString(CultureInfo.InvariantCulture), row.ObservedYear.ToString(CultureInfo.InvariantCulture),
					Csv(row.ObservedTier), Csv(row.LaunchTier), row.Quality, status,
					row.RetiredWeek >= 0 ? row.RetiredWeek.ToString(CultureInfo.InvariantCulture) : ""
				}));
			}
			demandWindowWriter.WriteLine(string.Join(",", new[] {
				demandObservationStartWeek.ToString(CultureInfo.InvariantCulture), currentAuditWeek.ToString(CultureInfo.InvariantCulture),
				demandObservationCompleted ? "true" : "false", demandObservations.Values.Count(r => r.Kind == "Opening").ToString(CultureInfo.InvariantCulture),
				demandObservations.Values.Count(r => r.Kind == "Launch").ToString(CultureInfo.InvariantCulture),
				activeCount.ToString(CultureInfo.InvariantCulture), retired.ToString(CultureInfo.InvariantCulture), missing.ToString(CultureInfo.InvariantCulture)
			}));
		}
		demandCensusWriter.Dispose(); demandWindowWriter.Dispose();
		demandCensusWriter = demandWindowWriter = null;
		demandObservations.Clear();
	}

	// Frozen settlement ids, not the mutable runtime owner. Includes uncharted and retiring
	// records, excludes sales before the observation window, and survives lean/calibration mode.
	private void WriteLabelSales(ChartManager.CompletedWeekSettlement settlement) {
		if (labelSalesWriter == null) return;
		foreach (var group in settlement.Entries.GroupBy(e => (e.LabelId, e.Format)))
			labelSalesWriter.WriteLine(string.Join(",", new[] {
				settlement.SettlementId.ToString(CultureInfo.InvariantCulture), settlement.Date.year.ToString(CultureInfo.InvariantCulture),
				Csv(group.Key.LabelId), group.Key.Format.ToString(),
				group.Sum(e => (long)e.Units).ToString(CultureInfo.InvariantCulture),
				group.Where(e => e.Record.currentPosition > 0).Sum(e => (long)e.Units).ToString(CultureInfo.InvariantCulture),
				group.Where(e => e.Record.currentPosition is > 0 and <= 10).Sum(e => (long)e.Units).ToString(CultureInfo.InvariantCulture)
			}));
	}

	private void CapturePhysicalInventoryWeek() {
		if (physicalInventoryWriter == null) return;
		foreach (var record in ChartManager.Instance.GetAllRecords()) WritePhysicalInventory(record, "Active");
	}

	private void WritePhysicalInventory(RecordRuntimeData record, string status) {
		var s = record.physicalInventory;
		if (physicalInventoryWriter == null || s == null) return;
		PhysicalPressingInventory.Validate(record);
		physicalInventoryWriter.WriteLine(string.Join(",", new[] {
			currentAuditWeek.ToString(CultureInfo.InvariantCulture), Csv(record.baseRecord.recordId), Csv(record.baseRecord.labelId),
			record.baseRecord.format.ToString(), status
		}.Concat(new long[] { s.OpeningUnits, s.Received, record.unitsPressed, record.pressingBookSold, s.Warehouse,
			record.regionalData.Values.Sum(r => r.unitsInStores), record.regionalData.Values.Sum(r => r.pendingLaunchStock),
			s.Shipped, s.SaleableReturns, s.HeldReturns, s.Promos, s.Damaged, s.WrittenOff, s.DistributedWrittenOff,
			s.OrderUnits, s.DueWeek, s.UnbilledInitial, s.UnbilledRepress, s.UnfilledRequests,
			PhysicalPressingInventory.ReconciliationDelta(record) }.Select(value => value.ToString(CultureInfo.InvariantCulture)))));
	}
}
