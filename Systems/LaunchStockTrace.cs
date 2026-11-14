using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Godot;

/// <summary>
/// --launch-stock-trace: one LAUNCH_STOCK_TRACE log line per AI launch, giving each region's coverage flags, distance
/// reach factor, drawn stock and final (post-specialist-allocation) stock. Observe-only: reads state, draws no random
/// numbers, so a traced run is byte-identical to the untraced one.
/// </summary>
public static class LaunchStockTrace {
	public const string Switch = "--launch-stock-trace";
	private static bool configured;
	public static bool Enabled { get; private set; }

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		Enabled = (arguments ?? Array.Empty<string>()).Contains(Switch);
		configured = true;
	}

	public static void Emit(RecordRuntimeData record, AILabel label, IEnumerable<MarketRegion> regions,
		IReadOnlyDictionary<string, int> drawn, IReadOnlyDictionary<string, int> allocated, float perceivedQualityMult, int year) {
		if (!Enabled || record?.baseRecord == null || label == null || label.isPlayerOwned) return;
		string id = record.baseRecord.recordId;
		var line = new StringBuilder(256);
		line.Append("LAUNCH_STOCK_TRACE rec=").Append(id).Append(" label=").Append(label.labelId)
			.Append(" tier=").Append(label.tier).Append(" year=").Append(year)
			.Append(" fmt=").Append(record.baseRecord.format)
			.Append(" dist=").Append(label.DistributionStrengthForRecord(id).ToString("F3", CultureInfo.InvariantCulture))
			.Append(" pqm=").Append(perceivedQualityMult.ToString("F3", CultureInfo.InvariantCulture))
			.Append(" regions=");
		bool first = true;
		foreach (MarketRegion region in regions) {
			if (region == null) continue;
			string r = region.regionId;
			bool covered = label.HasDistributionInRegionForRecord(r, id);
			bool strong = label.strongRegions?.Contains(r) ?? false;
			bool home = !string.IsNullOrEmpty(label.homeRegion) && label.homeRegion == r;
			float reach = DistanceModel.GetEffectiveReach(label, DistanceModel.GetHubCityIdForRegion(r));
			if (!first) line.Append('|');
			first = false;
			line.Append(r).Append(':').Append(covered ? 1 : 0).Append(strong ? 1 : 0).Append(home ? 1 : 0)
				.Append(':').Append(reach.ToString("F3", CultureInfo.InvariantCulture))
				.Append(':').Append(drawn?.GetValueOrDefault(r) ?? 0)
				.Append(':').Append(allocated?.GetValueOrDefault(r) ?? 0);
		}
		GD.Print(line.ToString());
	}
}
