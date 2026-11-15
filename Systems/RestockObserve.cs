using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Godot;

/// <summary>
/// Observation only (SimTools/Top10TurnoverFindings.md §11.13): <c>--restock-observe</c> writes one row per single and
/// region each week to SimLogs/&lt;run&gt;-restock.csv while the single sat in last week's Top 20, so a #1's supply limit
/// can be split into the restock trigger, the request (demand signal × service level), the per-label stock ceiling and
/// pressing. Stock after restock is what next week's sales can draw on. Reads settled state only and makes no draws,
/// so a run is byte-identical with it on or off.
/// </summary>
public static class RestockObserve {
	public const string Switch = "--restock-observe";
	private static bool configured, enabled;
	private static string output;
	private static StreamWriter writer;
	public static bool Enabled => enabled;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		configured = true;
		var args = (arguments ?? Array.Empty<string>()).ToArray();
		enabled = args.Contains(Switch);
		if (!enabled) return;
		string run = args.FirstOrDefault(a => a.StartsWith("--run=", StringComparison.Ordinal))?.Substring(6);
		if (string.IsNullOrEmpty(run) || Path.GetFileName(run) != run)
			throw new ArgumentException(Switch + " requires a plain --run name.");
		output = Path.Combine(ProjectSettings.GlobalizePath("res://SimLogs"), run + "-restock.csv");
		GD.Print("RESTOCK_OBSERVE " + output);
	}

	public static bool Wants(RecordRuntimeData record, RegionalRecordData data) =>
		enabled && record.baseRecord.format != ReleaseFormat.Album && !data.awaitingArrival &&
		record.currentPosition >= 1 && record.currentPosition <= 20;

	/// <param name="stockBefore">Stock left after this week's sales, before restock.</param>
	/// <param name="backordersBefore">Backorders before restock.</param>
	/// <param name="requested">The restock request (0 when no restock was triggered).</param>
	/// <param name="maxCapacity">The per-label stock ceiling (-1 when no restock was triggered).</param>
	/// <param name="afterCeiling">The request after the ceiling (can be negative when stock already exceeds it).</param>
	/// <param name="shipped">What reached the shops after pressing.</param>
	public static void Row(int week, int year, RecordRuntimeData record, MarketRegion region, RegionalRecordData data,
		bool covered, float distributionStrength, bool needsRestock, bool livePhysicalBackorder, int stockBefore,
		int backordersBefore, float demandSignal, float serviceLevel, int requested, int maxCapacity, int afterCeiling,
		int shipped) {
		if (writer == null) {
			writer = new StreamWriter(output);
			writer.WriteLine("week,year,recordId,labelId,region,position,age,covered,distributionStrength,rawDemand,serviceable," +
				"storeCapacity,sold,stockBefore,backordersBefore,needsRestock,livePhysicalBackorder,demandSignal,serviceLevel," +
				"requested,maxCapacity,afterCeiling,shipped,stockAfter");
		}
		var c = CultureInfo.InvariantCulture;
		writer.WriteLine(string.Join(",",
			week.ToString(c), year.ToString(c), record.baseRecord.recordId, record.baseRecord.labelId, region.regionId,
			record.currentPosition.ToString(c), record.weeksSinceRelease.ToString(c), covered ? "1" : "0",
			distributionStrength.ToString("G6", c), data.rawDemandThisWeek.ToString("G7", c),
			data.serviceableIntentThisWeek.ToString(c), data.storeCapacityThisWeek.ToString(c),
			data.unitsSoldThisWeek.ToString(c), stockBefore.ToString(c), backordersBefore.ToString(c),
			needsRestock ? "1" : "0", livePhysicalBackorder ? "1" : "0", demandSignal.ToString("G7", c),
			serviceLevel.ToString("G6", c), requested.ToString(c), maxCapacity.ToString(c), afterCeiling.ToString(c),
			shipped.ToString(c), data.unitsInStores.ToString(c)));
	}

	public static void Flush() => writer?.Flush();
}
