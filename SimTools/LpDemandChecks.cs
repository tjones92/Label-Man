using System;
using Godot;

public static class LpDemandChecks {
	public static void Run() {
		int checks = 0;
		void Check(bool condition, string name) { if (!condition) throw new InvalidOperationException("LP demand: " + name); checks++; }
		try {
			bool Rejects(params string[] args) {
				try { LpDemandProbe.ResetForChecks(); LpDemandProbe.Configure(args); return false; } catch (ArgumentException) { return true; }
			}
			foreach (string value in new[] { "0", ".92", "1.01", "NaN", "Infinity", "x" })
				Check(Rejects(LpDemandProbe.DecaySwitch + value), "invalid decay " + value);
			Check(Rejects(LpDemandProbe.DecaySwitch + ".985", LpDemandProbe.DecaySwitch + "1"), "duplicate decay");
			LpDemandProbe.ResetForChecks(); LpDemandProbe.Configure(Array.Empty<string>());
			Check(!LpDemandProbe.Audit && LpDemandProbe.WeeklyDecay == .93f, "off defaults");
			float soundtrack = AlbumSimulator.GetCatalogDecayMultiplier(AlbumFormat.Soundtrack, 52);
			foreach (int age in new[] { 0, 8, 9, 26, 52, 104 })
				Check(AlbumSimulator.GetCatalogDecayMultiplier(AlbumFormat.Standard, age) == (age <= 8 ? 1 : Mathf.Pow(.93f, age - 8)), "legacy exact " + age);
			LpDemandProbe.ResetForChecks(); LpDemandProbe.Configure(new[] { LpDemandProbe.AuditSwitch });
			Check(LpDemandProbe.Audit && AlbumSimulator.GetCatalogDecayMultiplier(AlbumFormat.Standard, 52) == Mathf.Pow(.93f, 44), "audit inert");
			LpDemandProbe.ResetForChecks(); LpDemandProbe.Configure(new[] { LpDemandProbe.DecaySwitch + ".985" });
			Check(LpDemandProbe.Audit && AlbumSimulator.GetCatalogDecayMultiplier(AlbumFormat.Standard, 52) == Mathf.Pow(.985f, 44), "live scenario");
			Check(AlbumSimulator.GetCatalogDecayMultiplier(AlbumFormat.Soundtrack, 52) == soundtrack, "soundtrack untouched");
			Check(AlbumSimulator.GetCatalogDecayMultiplier(AlbumFormat.Standard, 8) == 1, "early demand untouched");
			Check(Math.Abs(LpDemandProbe.Rescale(100, .25, .5) - 200) < 1e-10, "frozen rescaling");
			Check(LpDemandProbe.Rescale(0, 0, 1) == 0, "zero demand");
			Check(LpDemandProbe.Rescale(100, .5, .5) == 100, "identity shadow");
			bool invalid = false;
			try { LpDemandProbe.Rescale(1, 0, 1); } catch (InvalidOperationException) { invalid = true; }
			Check(invalid, "undefined positive-demand shadow rejected");
			LpDemandProbe.ResetForChecks(); LpDemandProbe.Configure(new[] { LpDemandProbe.DecaySwitch + "1" });
			Check(AlbumSimulator.GetCatalogDecayMultiplier(AlbumFormat.Compilation, 104) == 1, "no-decay endpoint");
		} finally { LpDemandProbe.ResetForChecks(); LpDemandProbe.Configure(OS.GetCmdlineUserArgs()); }
		GD.Print($"LP_DEMAND_CHECK passed checks={checks}");
	}
}
