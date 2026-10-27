using System;
using System.Text.Json;
using Godot;

public static class LpDemandCalibrationChecks {
	public static void Run() {
		int checks = 0;
		void Check(bool condition, string name) { if (!condition) throw new InvalidOperationException("LP calibration: " + name); checks++; }
		void Set(params string[] args) { LpDemandCalibration.ResetForChecks(); LpDemandCalibration.Configure(args); }
		try {
			foreach (string value in new[] { "0", ".899", "1.01", "NaN", "Infinity", "bad" }) {
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
		} finally { LpDemandCalibration.ResetForChecks(); LpDemandCalibration.Configure(OS.GetCmdlineUserArgs()); }
		GD.Print($"LP_DEMAND_CALIBRATION_CHECKS_OK {checks}");
	}
}
