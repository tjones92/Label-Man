using System;
using System.Linq;
using System.Text.Json;

public static class PhysicalInventoryChecks {
	public static void Run() {
		bool enabled = PhysicalPressingInventory.Enabled;
		int checks = 0;
		void Check(bool condition, string why) {
			if (!condition) throw new InvalidOperationException("Physical inventory: " + why);
			checks++;
		}
		RecordRuntimeData Fresh(string id = "physical-check") {
			var r = new RecordRuntimeData(new Record { recordId = id, format = ReleaseFormat.Single,
				releaseDate = new GameDate(1961, 1, 1) });
			r.regionalData["home"] = new RegionalRecordData("home") { unitsInStores = 900 };
			r.regionalData["away"] = new RegionalRecordData("away") { pendingLaunchStock = 900, awaitingArrival = true };
			return r;
		}
		try {
			PhysicalPressingInventory.SetEnabledForChecks(true);
			var label = new AILabel();
			var record = Fresh();
			PhysicalPressingInventory.BeginWeek(record, label, 1);
			Check(record.unitsPressed == 500 && PhysicalPressingInventory.Distributed(record) == 500,
				"launch shipment is bounded by manufacture before any sale");
			Check(record.regionalData["home"].unitsInStores == 250 && record.regionalData["away"].pendingLaunchStock == 250,
				"scarce launch supply retains regional proportions, including held stock");
			var bill = LabelPressingEconomics.Press(record, label, 0);
			Check(bill.InitialRun == 500 && bill.Repressed == 0 && record.pressingBookSold == 0,
				"zero-sales launch still incurs the initial manufacturing bill");
			record.regionalData["home"].unitsInStores -= 200;
			bill = LabelPressingEconomics.Press(record, label, 200);
			Check(bill.InitialRun == 0 && bill.Repressed == 0 && record.unitsPressed == 500,
				"sales consume shipped stock without automatic manufacture or another initial bill");
			int shipped = PhysicalPressingInventory.Ship(record, 300, 1);
			Check(shipped == 0 && record.physicalInventory.OrderUnits == 500 && record.physicalInventory.DueWeek == 2,
				"shortfall orders future production but delivers no phantom copies");
			var saved = RuntimeRecordSaveData.From(record);
			var restored = JsonSerializer.Deserialize<RuntimeRecordSaveData>(JsonSerializer.Serialize(saved, SaveGameService.TestJsonOptions),
				SaveGameService.TestJsonOptions).ToRuntime();
			Check(JsonSerializer.Serialize(RuntimeRecordSaveData.From(restored), SaveGameService.TestJsonOptions)
				== JsonSerializer.Serialize(saved, SaveGameService.TestJsonOptions), "pending order and all ledger fields round-trip");
			PhysicalPressingInventory.BeginWeek(restored, label, 1);
			Check(restored.physicalInventory.Warehouse == 0, "order cannot arrive in its order week");
			PhysicalPressingInventory.BeginWeek(restored, label, 2);
			PhysicalPressingInventory.BeginWeek(restored, label, 2);
			Check(restored.unitsPressed == 1000 && restored.physicalInventory.Warehouse == 500,
				"receipt after restore occurs exactly once and retains the original due week");
			bill = LabelPressingEconomics.Press(restored, label, 0);
			Check(bill.Repressed == 500 && LabelPressingEconomics.Press(restored, label, 0).Repressed == 0,
				"receipt is billed once even with zero sales");
			shipped = PhysicalPressingInventory.Ship(restored, 300, 2);
			restored.regionalData["home"].unitsInStores += shipped;
			PhysicalPressingInventory.Validate(restored);
			PhysicalPressingInventory.ReturnFromStores(restored, "home", 100, 20);
			Check(restored.physicalInventory.Warehouse == 280 && restored.physicalInventory.SaleableReturns == 80
				&& restored.physicalInventory.Damaged == 20 && restored.pressingBookSold == 200,
				"saleable returns transfer copies, damage destroys copies, neither reverses a consumer sale");
			PhysicalPressingInventory.WithdrawPromos(restored, 30);
			Check(restored.physicalInventory.Warehouse == 250 && restored.physicalInventory.Promos == 30,
				"promos consume warehouse copies without sales or new manufacture");
			var away = restored.regionalData["away"];
			away.unitsInStores += away.pendingLaunchStock; away.pendingLaunchStock = 0;
			PhysicalPressingInventory.Validate(restored);
			Check(PhysicalPressingInventory.ReconciliationDelta(restored) == 0, "regional arrival is an internal transfer");
			PhysicalPressingInventory.Replan(restored, () => restored.regionalData["home"].unitsInStores = 5000);
			Check(PhysicalPressingInventory.Distributed(restored) == 750, "ownership redistribution cannot create inventory");
			PhysicalPressingInventory.Retire(restored);
			Check(restored.physicalInventory.WrittenOff == 750 && PhysicalPressingInventory.Distributed(restored) == 0
				&& restored.unitsPressed == 1000, "retirement writes off remaining copies without another plant charge");
			var opening = Fresh("opening");
			opening.baseRecord.releaseDate = new GameDate(1959, 12, 1);
			PhysicalPressingInventory.BeginWeek(opening, label, 1);
			Check(opening.physicalInventory.OpeningUnits == 1800 && LabelPressingEconomics.Press(opening, label, 0).InitialRun == 0,
				"opening catalog has explicit paid opening inventory, not a new run");
			var legacy = Fresh("legacy"); legacy.pressingBookOpened = true; legacy.unitsPressed = 800; legacy.pressingBookSold = 400;
			PhysicalPressingInventory.BeginWeek(legacy, label, 1);
			Check(legacy.unitsPressed == 2200 && legacy.physicalInventory.OpeningUnits == 2200,
				"legacy adoption states its supplied opening balance without pretending it was current production");
			var player = Fresh("player"); player.baseRecord.isPlayerOwned = true;
			PhysicalPressingInventory.BeginWeek(player, label, 1);
			Check(player.physicalInventory == null && player.regionalData["home"].unitsInStores == 900, "PlayerDesk remains its own ledger");
			var corrupt = Fresh("corrupt"); PhysicalPressingInventory.BeginWeek(corrupt, label, 1);
			corrupt.regionalData["home"].unitsInStores++;
			bool rejected = false;
			try { PhysicalPressingInventory.Validate(corrupt); } catch (InvalidOperationException) { rejected = true; }
			Check(rejected, "unbacked shelf copies fail conservation");
			PhysicalPressingInventory.SetEnabledForChecks(false);
			var off = Fresh("off"); PhysicalPressingInventory.BeginWeek(off, label, 1);
			Check(off.physicalInventory == null && off.regionalData["home"].unitsInStores == 900, "disabled path has no inventory effects");
			Godot.GD.Print($"PHYSICAL_INVENTORY_CHECK_PASS checks={checks}");
		} finally { PhysicalPressingInventory.SetEnabledForChecks(enabled); }
	}
}

