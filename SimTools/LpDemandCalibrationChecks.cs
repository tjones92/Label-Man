using System;
using System.Text.Json;
using Godot;

public static class LpDemandCalibrationChecks {
	public static void Run() {
		int checks = 0;
		void Check(bool condition, string name) { if (!condition) throw new InvalidOperationException("LP calibration: " + name); checks++; }
		void Set(params string[] args) { LpDemandCalibration.ResetForChecks(); LpDemandCalibration.Configure(args); }
		try {
			foreach (string value in new[] { "0", ".899", "3.01", "NaN", "Infinity", "bad" }) {
				bool rejected = false;
				try { Set(LpDemandCalibration.Switch + value); } catch (ArgumentException) { rejected = true; }
				Check(rejected, "invalid factor " + value);
			}
			bool duplicate = false;
			try { Set(LpDemandCalibration.Switch + ".96", LpDemandCalibration.Switch + ".98"); } catch (ArgumentException) { duplicate = true; }
			Check(duplicate, "duplicate factors rejected");
			Set();
			float original = AlbumSimulator.CalculateRawDemandBeforeCannibalization(2000, .4f, .095f);
			Check(!LpDemandCalibration.Enabled && LpDemandCalibration.Apply(1.234567f) == 1.234567f &&
				LpDemandCalibration.BudgetDemand(100, 25) == 125, "off path preserves intentions exactly");
			Check(!JsonSerializer.Serialize(new WorldSaveData()).Contains("LpDemandScale"), "old saves omit the new field");
			Set(LpDemandCalibration.Switch + "1");
			Check(!LpDemandCalibration.Enabled && LpDemandCalibration.SavedScale == null &&
				AlbumSimulator.CalculateRawDemandBeforeCannibalization(2000, .4f, .095f) == original, "explicit one is exactly off");
			Set(LpDemandCalibration.Switch + ".96");
			Check(AlbumSimulator.CalculateRawDemandBeforeCannibalization(2000, .4f, .095f) == (float)(original * .96),
				"store intentions scale before cannibalization, inventory and clearing");
			Check(LpDemandCalibration.BudgetDemand(100, 25) == 121 && LpDemandCalibration.BudgetDemand(0, 25) == 25,
				"only autonomous budget intentions scale; transfers are not reduced twice");
			Check(LpDemandCalibration.Apply(0f) == 0 && LpDemandCalibration.Apply(100d) == 96, "zero and autonomous double intentions");
			var saved = WorldStateService.Capture();
			Check(saved.LpDemandScale == .96 && SaveGameService.WritingVersion == 35, "active scale is captured in v35");
			var restored = JsonSerializer.Deserialize<WorldSaveData>(JsonSerializer.Serialize(new WorldSaveData { LpDemandScale = .96 }));
			LpDemandCalibration.RequireSaved(restored.LpDemandScale);
			Check(restored.LpDemandScale == .96, "saved scale round-trips");
			int week = ChartManager.Instance.GetCurrentChartWeek();
			foreach (double? wrong in new double?[] { null, .98, double.NaN, .5 }) {
				bool rejected = false;
				try { WorldStateService.Apply(new WorldSaveData { LpDemandScale = wrong }, default, null); }
				catch (InvalidOperationException) { rejected = true; }
				Check(rejected && ChartManager.Instance.GetCurrentChartWeek() == week, "mismatched/corrupt scale rejected before world mutation");
			}
			Set();
			bool missing = false;
			try { LpDemandCalibration.RequireSaved(.96); } catch (InvalidOperationException) { missing = true; }
			Check(missing, "active save requires its factor on resume");
			LpDemandCalibration.RequireSaved(null);
			// --lp-demand-scale-stores-only: store intent scales; rack and budget-autonomous demand do not.
			bool orphan = false;
			try { Set(LpDemandCalibration.StoresOnlySwitch); } catch (ArgumentException) { orphan = true; }
			Check(orphan, "stores-only needs an active scale");
			Set(LpDemandCalibration.Switch + "1.6");
			Check(LpDemandCalibration.ApplyNonStore(100d) == 160 && LpDemandCalibration.BudgetDemand(100, 25) == 185, "above-one scale lifts every channel");
			Set(LpDemandCalibration.Switch + "1.6", LpDemandCalibration.StoresOnlySwitch);
			Check(AlbumSimulator.CalculateRawDemandBeforeCannibalization(2000, .4f, .095f) == (float)(original * 1.6) &&
				LpDemandCalibration.ApplyNonStore(100f) == 100f && LpDemandCalibration.BudgetDemand(100, 25) == 125, "stores-only leaves rack and budget demand");
			Check(WorldStateService.Capture().LpDemandScaleStoresOnly == true, "stores-only is captured");
			bool mismatch = false;
			try { LpDemandCalibration.RequireSaved(1.6); } catch (InvalidOperationException) { mismatch = true; }
			Check(mismatch, "resume refuses a save without stores-only");
			LpDemandCalibration.RequireSaved(1.6, true);
			// --lp-demand-scale-known: an act's LP takes 1 + (S - 1) x Known; Known 1 is the flat scale.
			bool knownOrphan = false;
			try { Set(LpDemandCalibration.KnownSwitch); } catch (ArgumentException) { knownOrphan = true; }
			Check(knownOrphan, "known weighting needs an active scale");
			Set(LpDemandCalibration.Switch + "1.6", LpDemandCalibration.StoresOnlySwitch);
			Check(LpDemandCalibration.ApplyStore(100f, 0f) == 160f && LpDemandCalibration.ApplyStore(100f, 0f) == LpDemandCalibration.Apply(100f),
				"without the switch the store scale ignores Known");
			Set(LpDemandCalibration.Switch + "2.5", LpDemandCalibration.StoresOnlySwitch, LpDemandCalibration.KnownSwitch);
			Check(LpDemandCalibration.ApplyStore(100f, 0f) == 100f && LpDemandCalibration.ApplyStore(100f, 1f) == 250f &&
				LpDemandCalibration.ApplyStore(100f, .5f) == 175f && LpDemandCalibration.ApplyStore(100f, 2f) == 250f, "store scale is 1 + (S - 1) x Known, clamped");
			Check(AlbumSimulator.CalculateRawDemandBeforeCannibalization(2000, .4f, .095f, 0f) == 2000 * .4f * .095f,
				"an unknown act's LP takes no scale");
			Check(LpDemandCalibration.ApplyNonStore(100f) == 100f, "known weighting leaves rack demand");
			Check(WorldStateService.Capture().LpDemandScaleKnown == true, "known weighting is captured");
			bool knownMismatch = false;
			try { LpDemandCalibration.RequireSaved(2.5, true); } catch (InvalidOperationException) { knownMismatch = true; }
			Check(knownMismatch, "resume refuses a save without known weighting");
			LpDemandCalibration.RequireSaved(2.5, true, true);
		} finally { LpDemandCalibration.ResetForChecks(); LpDemandCalibration.Configure(OS.GetCmdlineUserArgs()); }
		GD.Print($"LP_DEMAND_CALIBRATION_CHECKS_OK {checks}");
	}
}
