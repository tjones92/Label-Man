using System;
using System.Globalization;
using System.IO;

public partial class ChartAuditRunner {
	private StreamWriter lpDemandWriter;
	private void OpenLpDemandOutputs(string directory) {
		if (!LpDemandProbe.Audit) return;
		lpDemandWriter = CreateWriter(Path.Combine(directory, $"{runName}-lp-demand.csv"));
		lpDemandWriter.WriteLine("week,year,recordId,observationKind,releaseYear,labelId,tier,genre,albumFormat,ageWeeks,quality,currentPosition,regions,buyerPoolSum,poolAwareSum,rawBeforeCannibalization,rawDemand,serviceable,cleared,stockBeforeSale,storeCapacity,rawLegacyDecay,rawSlowDecay,rawNoNormalDecay,rawNoExhaustion,rawFullAwareness,retiredAfterSettlement");
	}
	private void WriteLpDemandSettlement(ChartManager.CompletedWeekSettlement settlement) {
		if (lpDemandWriter == null) return;
		string D(double value) => value.ToString("R", CultureInfo.InvariantCulture);
		foreach (var entry in settlement.Entries) {
			if (entry.Format != ReleaseFormat.Album || entry.Record == null) continue;
			var record = entry.Record;
			if (!demandObservations.TryGetValue(entry.RecordId, out var observed))
				throw new InvalidOperationException("LP demand audit: record missing from launch census.");
			bool normal = record.baseRecord.album?.albumFormat != AlbumFormat.Soundtrack;
			int age = record.weeksSinceRelease, regionCount = 0;
			double pool = 0, aware = 0, rawBefore = 0, raw = 0, legacy = 0, slow = 0, noDecay = 0, noExhaustion = 0, fullAwareness = 0;
			long serviceable = 0, cleared = 0, stock = 0, capacity = 0;
			foreach (var region in entry.Regions ?? Array.Empty<ChartManager.CompletedWeekSettlementRegion>()) {
				if (!record.regionalData.TryGetValue(region.RegionId, out var data))
					throw new InvalidOperationException("LP demand audit: settlement region missing.");
				regionCount++;
				double r = data.albumRawDemandAfterCannibalizationThisWeek;
				double decay = data.albumCatalogDecayMultiplierThisWeek;
				double neutral = normal ? LpDemandProbe.Rescale(r, decay, 1) : r;
				pool += data.albumBuyerPoolThisWeek; aware += (double)data.albumBuyerPoolThisWeek * data.albumAwarenessThisWeek;
				rawBefore += data.albumRawDemandBeforeCannibalizationThisWeek; raw += r;
				legacy += normal ? neutral * Math.Pow(LpDemandProbe.LegacyDecay, Math.Max(0, age - 8)) : r;
				slow += normal ? neutral * Math.Pow(LpDemandProbe.SlowScenarioDecay, Math.Max(0, age - 8)) : r;
				noDecay += neutral;
				noExhaustion += LpDemandProbe.Rescale(r, data.albumExhaustionThisWeek, 1);
				fullAwareness += (double)data.albumBuyerPoolThisWeek * data.albumConversionThisWeek * (1 - record.cannibalizationSuppression);
				serviceable += region.ServiceableIntent; cleared += region.FinalCleared;
				stock += data.albumUnitsInStoresBeforeSaleThisWeek; capacity += data.storeCapacityThisWeek;
			}
			if (cleared != entry.Units) throw new InvalidOperationException("LP demand audit: settlement units do not reconcile.");
			lpDemandWriter.WriteLine(string.Join(",", new[] {
				settlement.SettlementId.ToString(CultureInfo.InvariantCulture), settlement.Date.year.ToString(CultureInfo.InvariantCulture),
				Csv(entry.RecordId), observed.Kind, record.baseRecord.releaseDate.year.ToString(CultureInfo.InvariantCulture),
				Csv(entry.LabelId), Csv(entry.LabelTier), Csv(entry.Genre), record.baseRecord.album?.albumFormat.ToString() ?? "Unknown",
				age.ToString(CultureInfo.InvariantCulture), D(record.GetQuality()), record.currentPosition.ToString(CultureInfo.InvariantCulture),
				regionCount.ToString(CultureInfo.InvariantCulture), D(pool), D(aware), D(rawBefore), D(raw), D(serviceable), D(cleared), D(stock), D(capacity),
				D(legacy), D(slow), D(noDecay), D(noExhaustion), D(fullAwareness), entry.RetiredAfterSettlement ? "true" : "false"
			}));
		}
	}
}
