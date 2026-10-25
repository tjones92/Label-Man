using System;
using System.Linq;
using Godot;

public static class LpCapacityChecks {
	public static void Run() {
		int checks = 0;
		void Check(bool condition, string name) { if (!condition) throw new InvalidOperationException("LP capacity: " + name); checks++; }
		var args = OS.GetCmdlineUserArgs();
		float programme = LpCapacityScreen.ProgrammeScale, purchase = LpCapacityScreen.PurchaseShare;
		bool audit = LpCapacityScreen.Audit;
		try {
			bool Rejects(params string[] a) {
				try { LpCapacityScreen.ResetForChecks(); LpCapacityScreen.Configure(a); return false; } catch (ArgumentException) { return true; }
			}
			foreach (string value in new[] { "0", "NaN", "5", "x" })
				Check(Rejects("--label-release-slate", "--label-release-capacity", LpCapacityScreen.ProgrammeSwitch + value), "invalid programme scale");
			Check(Rejects(LpCapacityScreen.ProgrammeSwitch + "2"), "programme requires scheduler");
			foreach (string value in new[] { "0", ".9", "Infinity", "x" }) Check(Rejects(LpCapacityScreen.PurchaseSwitch + value), "invalid purchase share");
			Check(Rejects(LpCapacityScreen.PurchaseSwitch + ".48", LpCapacityScreen.PurchaseSwitch + ".5"), "duplicate share");
			Check(Rejects(LpCapacityScreen.PurchaseSwitch + ".48", "--album-purchase-volume=.5"), "no overlapping budget intervention");
			LpCapacityScreen.ResetForChecks(); LpCapacityScreen.Configure(Array.Empty<string>());
			Check(LpCapacityScreen.ProgrammeScale == 1 && !LpCapacityScreen.PurchaseEnabled && !LpCapacityScreen.Audit, "off defaults");
			LabelReleaseProgramme.Configure(new[] { "--label-release-slate", "--label-release-capacity" });
			foreach (var label in CompetitorManager.Instance.GetAllLabels().Where(l => !l.isPlayerOwned).GroupBy(l => l.tier).Select(g => g.First())) {
				LpCapacityScreen.SetForChecks(1, 0); var before = LabelReleaseProgramme.MonthlyCapacity(label);
				LpCapacityScreen.SetForChecks(2, 0); var after = LabelReleaseProgramme.MonthlyCapacity(label);
				Check(before.Singles == after.Singles && after.Albums == 2 * before.Albums, "only album rate scales: " + label.tier);
			}
			Check(LpCapacityScreen.AlbumRate(new AILabel { isPlayerOwned = true }, 5) == 5, "player excluded");
			LpCapacityScreen.SetForChecks(1, .5f);
			Check(LpCapacityScreen.AdditionalAlbumCapacity(100, 40) == 60, "share translated to incremental album capacity");
			Check(LpCapacityScreen.AdditionalAlbumCapacity(100, 120) == 0, "never withdraw existing capacity");
			var high = ChartManager.LpAllocationForChecks(200);
			Check(high.Single == 80 && high.Album == 100 && high.Added == 60, "existing single residual cannot claim extra LP capacity");
			Check(high.Export == 15 && high.Import == 21, "mixed spillover limits unchanged");
			var low = ChartManager.LpAllocationForChecks(40);
			Check(low.Single == 80 && low.Album == 40 && low.Added == 0, "no forced LP demand or sales");
			LpCapacityScreen.SetForChecks(1, 0);
			Check(ChartManager.LpAllocationForChecks(200).Album == 40, "off leaves old allocation");
		} finally {
			LpCapacityScreen.SetForChecks(programme, purchase, audit); LabelReleaseProgramme.Configure(args);
		}
		GD.Print($"LP_CAPACITY_CHECK passed checks={checks}");
	}
}
