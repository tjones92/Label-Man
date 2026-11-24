using System;
using System.Linq;

/// <summary>
/// --away-restock-lag=L (LpGenreAllocationFollowup.md §105). §104 found the Big-5 1960 cost of local promotion runs
/// through the store inventory loop: a promoted small single stays stocked and its label keeps selling. Restating that
/// loop at the pre-shift geography gave the Big-5 back, but only by draining small labels' away shelves with copies
/// they never sold. This is the honest version of that. A small label (Independent, Small, Boutique) had no branch
/// outside its footprint (home, owned distribution, strong regions). There it reordered through independent
/// distributors and one-stops, and a reorder took weeks to arrive. Such a reorder now lands L weeks after it is placed,
/// and the region places no new order while one is in transit. Pressing is drawn when the order is placed.
/// <para>`--away-restock-lag-scope=promoted` (default) applies it only to singles local promotion shifts
/// (<see cref="LocalPromotion.Shifts"/>); `all` applies it to every small-label AI record. Off: never read.</para>
/// </summary>
public static class AwayRestockLag {
	public const string Switch = "--away-restock-lag", ScopeSwitch = "--away-restock-lag-scope", WhereSwitch = "--away-restock-lag-where";
	private static bool read, promotedOnly = true, uncoveredOnly;
	private static int lag;

	private static void Read() {
		if (read) return;
		lag = (int)Math.Round(GameyAuditArguments.Value(Switch, 1f, 12f) ?? 0f);
		string scopeArg = SimulationArguments.Get().FirstOrDefault(a => a.StartsWith(ScopeSwitch + "=", StringComparison.Ordinal));
		if (scopeArg != null) {
			if (lag <= 0) throw new ArgumentException(ScopeSwitch + " needs " + Switch + "=L.");
			promotedOnly = scopeArg.Substring(ScopeSwitch.Length + 1) switch {
				"promoted" => true, "all" => false, _ => throw new ArgumentException(ScopeSwitch + " takes promoted or all.") };
		}
		string whereArg = SimulationArguments.Get().FirstOrDefault(a => a.StartsWith(WhereSwitch + "=", StringComparison.Ordinal));
		if (whereArg != null) {
			if (lag <= 0) throw new ArgumentException(WhereSwitch + " needs " + Switch + "=L.");
			uncoveredOnly = whereArg.Substring(WhereSwitch.Length + 1) switch {
				"footprint" => false, "uncovered" => true, _ => throw new ArgumentException(WhereSwitch + " takes footprint or uncovered.") };
		}
		if (promotedOnly && lag > 0 && !LocalPromotion.Enabled) throw new ArgumentException(Switch + " scope promoted needs " + LocalPromotion.Switch + "=K.");
		if (lag > 0) Godot.GD.Print($"AWAY_RESTOCK_LAG weeks={lag} scope={(promotedOnly ? "promoted" : "all")} where={(uncoveredOnly ? "uncovered" : "footprint")}");
		read = true;
	}

	internal static void SetForChecks(int weeks, bool promoted = false, bool uncovered = false) { read = true; lag = weeks; promotedOnly = promoted; uncoveredOnly = uncovered; }

	public static bool Enabled { get { Read(); return lag > 0; } }
	public static int Weeks { get { Read(); return lag; } }

	public static bool SmallTier(LabelTier tier) => tier is LabelTier.Independent or LabelTier.Small or LabelTier.Boutique;

	/// <summary>True when this record's reorders in this region go through the lag. `--away-restock-lag-where=uncovered`
	/// (§106): only where no distributor ships the record either (a P&amp;D or wholesale house reorders weekly through its
	/// own branches); `footprint` (default): everywhere outside the label's promotion footprint.</summary>
	public static bool Applies(RecordRuntimeData record, AILabel label, string regionId, bool covered = false) {
		if (!Enabled || label == null || record?.baseRecord == null || record.baseRecord.isPlayerOwned || label.isPlayerOwned) return false;
		if (!SmallTier(label.tier)) return false;
		if (promotedOnly && !LocalPromotion.Shifts(record, label)) return false;
		if (uncoveredOnly && covered) return false;
		return !LocalPromotion.Footprint(label).Contains(regionId);
	}

	/// <summary>Lands an order whose week has come: stock up, backorders down. Returns true while one is still in transit.</summary>
	public static bool Land(RegionalRecordData data, int week) {
		if (data.restockInTransit <= 0) return false;
		if (week < data.restockArrivalWeek) return true;
		data.unitsInStores += data.restockInTransit;
		data.unitsBackordered = Math.Max(0, data.unitsBackordered - data.restockInTransit);
		data.restockInTransit = 0;
		return false;
	}

	/// <summary>Places an order of units that lands L weeks on.</summary>
	public static void Order(RegionalRecordData data, int units, int week) {
		data.restockInTransit = units;
		data.restockArrivalWeek = week + Weeks;
	}
}
