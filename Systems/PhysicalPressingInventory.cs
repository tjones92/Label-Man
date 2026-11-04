using System;
using System.Linq;

/// <summary>Opt-in AI inventory experiment. Orders have a one-chart-week production lead time;
/// receipts precede shipment, and shelf stock alone can be sold. No discovery inputs or RNG.</summary>
public static class PhysicalPressingInventory {
	public const string Switch = "--physical-pressing-inventory";
	/// <summary>Screening variant: cover lots are sized from sold plus this week's shortfall, not sold alone.</summary>
	public const string DemandLotSwitch = "--physical-inventory-demand-lot";
	/// <summary>Screening variant: a receipt ships to backordered regions before that week's sales.</summary>
	public const string EarlyReceiptSwitch = "--physical-inventory-early-receipt";
	/// <summary>Screening variant: the initial pressing covers the full launch shelf and held request.</summary>
	public const string ShelfLaunchSwitch = "--physical-inventory-shelf-launch";
	public static bool Enabled { get; private set; }
	public static bool DemandLot { get; private set; }
	public static bool EarlyReceipt { get; private set; }
	public static bool ShelfLaunch { get; private set; }
	public static void Configure(string[] args) {
		Enabled = args.Contains(Switch);
		DemandLot = args.Contains(DemandLotSwitch);
		EarlyReceipt = args.Contains(EarlyReceiptSwitch);
		ShelfLaunch = args.Contains(ShelfLaunchSwitch);
		if (Enabled && (!LabelPressingEconomics.Enabled || !GenreMarketV2.Enabled))
			throw new ArgumentException(Switch + " requires the pressing book and genre-market settlement.");
		if (!Enabled && (DemandLot || EarlyReceipt || ShelfLaunch))
			throw new ArgumentException(DemandLotSwitch + ", " + EarlyReceiptSwitch + " and " + ShelfLaunchSwitch + " require " + Switch + ".");
	}
	internal static void SetEnabledForChecks(bool enabled) => Enabled = enabled;
	public static bool Applies(RecordRuntimeData record) => Enabled && record?.baseRecord != null && !record.baseRecord.isPlayerOwned;

	public static void BeginWeek(RecordRuntimeData record, AILabel label, int week) {
		if (!Applies(record)) return;
		if (record.physicalInventory == null) {
			var book = record.physicalInventory = new PhysicalInventoryState();
			bool opening = record.pressingBookOpened || record.baseRecord.releaseDate <= GameDate.StartDate;
			if (opening) {
				// Explicit adoption balance for seeded catalog or a legacy save. Never bill it again.
				int distributed = Distributed(record);
				book.OpeningUnits = Math.Max(record.unitsPressed, checked(record.pressingBookSold + distributed));
				record.unitsPressed = book.OpeningUnits;
				book.Warehouse = book.OpeningUnits - record.pressingBookSold - distributed;
				book.Shipped = checked(distributed + record.pressingBookSold);
			} else {
				int initial = LabelPressingEconomics.InitialCopies(label, record.baseRecord.format);
				// Shelf-launch variant: press the whole launch request; any forecast excess stays in the warehouse.
				if (ShelfLaunch) initial = Math.Max(initial, Distributed(record));
				book.Warehouse = initial;
				book.Received = initial;
				book.UnbilledInitial = initial;
				record.unitsPressed = initial;
				AllocateLaunch(record);
			}
			record.pressingBookOpened = true;
		}
		var state = record.physicalInventory;
		if (state.OrderUnits > 0 && state.DueWeek <= week) {
			state.Warehouse = checked(state.Warehouse + state.OrderUnits);
			state.Received = checked(state.Received + state.OrderUnits);
			state.UnbilledRepress = checked(state.UnbilledRepress + state.OrderUnits);
			record.unitsPressed = checked(record.unitsPressed + state.OrderUnits);
			state.OrderUnits = 0;
			state.DueWeek = -1;
			if (EarlyReceipt) ShipToBackorders(record);
		}
		Validate(record);
	}

	/// <summary>Early-receipt variant: deliver warehouse stock to reached regions' backlogs before sales,
	/// pro rata with largest-remainder rounding. Never ships more than a region's backlog.</summary>
	private static void ShipToBackorders(RecordRuntimeData record) {
		var state = record.physicalInventory;
		var regions = record.regionalData.OrderBy(p => p.Key, StringComparer.Ordinal)
			.Select(p => p.Value).Where(r => !r.awaitingArrival && r.unitsBackordered > 0).ToArray();
		long total = regions.Sum(r => (long)r.unitsBackordered);
		int available = (int)Math.Min(state.Warehouse, total);
		if (available <= 0) return;
		int[] allocations = regions.Select(r => (int)((long)available * r.unitsBackordered / total)).ToArray();
		int remainder = available - allocations.Sum();
		foreach (int index in Enumerable.Range(0, regions.Length)
			.OrderByDescending(i => (long)available * regions[i].unitsBackordered % total).ThenBy(i => i)) {
			if (remainder == 0) break;
			if (allocations[index] < regions[index].unitsBackordered) { allocations[index]++; remainder--; }
		}
		for (int i = 0; i < regions.Length; i++) {
			regions[i].unitsInStores = checked(regions[i].unitsInStores + allocations[i]);
			regions[i].unitsBackordered -= allocations[i];
		}
		state.Warehouse -= available;
		state.Shipped = checked(state.Shipped + available);
	}

	private static void AllocateLaunch(RecordRuntimeData record) {
		// Deterministic proportional allocation retains the requested regional mix. Held arrival
		// stock is already reserved inventory, not a second supply created when the region opens.
		var requests = record.regionalData.OrderBy(p => p.Key, StringComparer.Ordinal)
			.SelectMany(p => new[] { (Data: p.Value, Pending: false, Units: p.Value.unitsInStores),
				(Data: p.Value, Pending: true, Units: p.Value.pendingLaunchStock) }).ToArray();
		long total = requests.Sum(p => (long)p.Units);
		int available = (int)Math.Min(record.physicalInventory.Warehouse, total);
		int[] allocations = requests.Select(p => total == 0 ? 0 : (int)((long)available * p.Units / total)).ToArray();
		int remainder = available - allocations.Sum();
		foreach (int index in Enumerable.Range(0, requests.Length)
			.OrderByDescending(i => total == 0 ? 0 : (long)available * requests[i].Units % total).ThenBy(i => i)) {
			if (remainder == 0) break;
			if (allocations[index] < requests[index].Units) { allocations[index]++; remainder--; }
		}
		for (int i = 0; i < requests.Length; i++) {
			if (requests[i].Pending) requests[i].Data.pendingLaunchStock = allocations[i];
			else requests[i].Data.unitsInStores = allocations[i];
		}
		record.physicalInventory.Warehouse -= available;
		record.physicalInventory.Shipped += available;
	}

	/// <summary>Ship on-hand copies only. An unmet request schedules a plant lot for next week;
	/// it confers no shelf stock or current-week sales. Multiple regions share that one order.</summary>
	public static int Ship(RecordRuntimeData record, int requested, int week) {
		var state = record.physicalInventory ?? throw new InvalidOperationException("Inventory must open before shipment.");
		if (requested < 0) throw new ArgumentOutOfRangeException(nameof(requested));
		int shipped = Math.Min(requested, state.Warehouse);
		state.Warehouse -= shipped;
		state.Shipped = checked(state.Shipped + shipped);
		int shortfall = requested - shipped;
		if (shortfall > 0) {
			state.UnfilledRequests = checked(state.UnfilledRequests + shortfall);
			// Accumulate requests, but do not repeatedly order the same backorder while a lot is due.
			state.RequestedThisWeek = checked(state.RequestedThisWeek + shortfall);
			int weekly = Math.Max(record.unitsThisWeek, record.unitsPreviousWeek);
			// Demand-lot variant: a stockout suppresses sales, so sold alone would undersize the lot.
			if (DemandLot) weekly = checked(record.unitsThisWeek + state.RequestedThisWeek);
			int cover = LabelPressingEconomics.RepressLot(weekly, record.baseRecord.format);
			int desired = Math.Max(cover, state.RequestedThisWeek);
			state.OrderUnits = Math.Max(state.OrderUnits, desired);
			state.DueWeek = state.DueWeek < 0 ? week + 1 : state.DueWeek;
		}
		return shipped;
	}

	public static LabelPressingEconomics.PressingWeek BookSales(RecordRuntimeData record, int sold) {
		var state = record.physicalInventory ?? throw new InvalidOperationException("Sales preceded physical inventory initialization.");
		if (sold < 0) throw new ArgumentOutOfRangeException(nameof(sold));
		record.pressingBookSold = checked(record.pressingBookSold + sold);
		var result = new LabelPressingEconomics.PressingWeek(state.UnbilledInitial, state.UnbilledRepress);
		state.UnbilledInitial = state.UnbilledRepress = state.RequestedThisWeek = 0;
		Validate(record);
		return result;
	}

	/// <summary>Unsold dealer returns: saleable copies return to the warehouse; damaged copies
	/// are removed. Neither is another sale, another plant bill, or a consumer refund.</summary>
	public static void ReturnFromStores(RecordRuntimeData record, string regionId, int units, int damaged) {
		var state = record.physicalInventory ?? throw new InvalidOperationException("Inventory is not open.");
		var region = record.regionalData[regionId];
		if (units < 0 || damaged < 0 || damaged > units || units > region.unitsInStores)
			throw new ArgumentOutOfRangeException(nameof(units));
		region.unitsInStores -= units;
		state.SaleableReturns = checked(state.SaleableReturns + units - damaged);
		state.Damaged = checked(state.Damaged + damaged);
		state.Warehouse = checked(state.Warehouse + units - damaged);
		Validate(record);
		LabelCompanyAccounts.ReconcilePhysicalInventory(ChartManager.Instance?.GetLabelById(record.baseRecord.labelId), record);
	}

	public static void WithdrawPromos(RecordRuntimeData record, int units) {
		var state = record.physicalInventory ?? throw new InvalidOperationException("Inventory is not open.");
		if (units < 0 || units > state.Warehouse) throw new ArgumentOutOfRangeException(nameof(units));
		state.Warehouse -= units;
		state.Promos = checked(state.Promos + units);
		Validate(record);
		LabelCompanyAccounts.ReconcilePhysicalInventory(ChartManager.Instance?.GetLabelById(record.baseRecord.labelId), record);
	}

	/// <summary>An ownership change moves existing stock, never creates copies. The callback
	/// supplies the new regional targets; these are bounded by the same on-hand inventory.</summary>
	public static void Replan(RecordRuntimeData record, Action plan) {
		var state = record.physicalInventory;
		if (state == null) { plan(); return; }
		Validate(record);
		int shelves = record.regionalData.Values.Sum(r => r.unitsInStores);
		int held = record.regionalData.Values.Sum(r => r.pendingLaunchStock);
		state.SaleableReturns = checked(state.SaleableReturns + shelves);
		state.HeldReturns = checked(state.HeldReturns + held);
		state.Warehouse = checked(state.Warehouse + shelves + held);
		foreach (var region in record.regionalData.Values) region.unitsInStores = region.pendingLaunchStock = 0;
		plan();
		AllocateLaunch(record);
		Validate(record);
	}

	public static void Retire(RecordRuntimeData record) {
		if (record.physicalInventory == null) return;
		Validate(record);
		var state = record.physicalInventory;
		if (state.OrderUnits != 0 || state.UnbilledInitial != 0 || state.UnbilledRepress != 0)
			throw new InvalidOperationException("A committed plant run must be received and billed before retirement.");
		// Terminal withdrawal is explicit destruction, not a fictitious customer return/refund.
		state.WrittenOff = checked(state.WrittenOff + state.Warehouse + Distributed(record));
		state.DistributedWrittenOff = checked(state.DistributedWrittenOff + Distributed(record));
		state.Warehouse = state.OrderUnits = 0;
		state.DueWeek = -1;
		foreach (var region in record.regionalData.Values) region.unitsInStores = region.pendingLaunchStock = 0;
		Validate(record);
	}

	public static int Distributed(RecordRuntimeData record) => checked(record.regionalData.Values.Sum(r => checked(r.unitsInStores + r.pendingLaunchStock)));
	public static long ReconciliationDelta(RecordRuntimeData record) {
		var s = record.physicalInventory;
		return (long)s.OpeningUnits + s.Received - record.pressingBookSold - s.Warehouse - Distributed(record) - s.Promos - s.Damaged - s.WrittenOff;
	}
	public static void Validate(RecordRuntimeData record) {
		var s = record.physicalInventory;
		if (s == null || s.Warehouse < 0 || s.OrderUnits < 0 || record.regionalData.Values.Any(r => r.unitsInStores < 0 || r.pendingLaunchStock < 0)
			|| (long)record.unitsPressed != (long)s.OpeningUnits + s.Received || ReconciliationDelta(record) != 0
			|| (long)s.Shipped - s.SaleableReturns - s.HeldReturns - s.Damaged - record.pressingBookSold - s.DistributedWrittenOff != Distributed(record))
			throw new InvalidOperationException($"Physical inventory failed reconciliation: {record.baseRecord.recordId}.");
	}
}

/// <summary>Saved unit ledger. Plant orders are commitments, not receipts; returns and shipments
/// are transfers. Manufacturing is expensed once by the existing pressing/plant settlement.</summary>
public sealed class PhysicalInventoryState {
	public int OpeningUnits, Received, Warehouse, Shipped, SaleableReturns, Promos, Damaged, WrittenOff;
	public int HeldReturns, DistributedWrittenOff;
	public int OrderUnits, DueWeek = -1, UnbilledInitial, UnbilledRepress, UnfilledRequests, RequestedThisWeek;
	public PhysicalInventoryState Copy() => (PhysicalInventoryState)MemberwiseClone();
}
