using System;
using System.Linq;
using Godot;

/// <summary>--slate-preparation-headroom-check: argument validation; off passes the rate through; Majors are excluded;
/// non-Majors scale by H, and the keyed slot draw rounds the scaled rate.</summary>
public static class PreparationHeadroomChecks {
	public static void Run() {
		int checks = 0;
		void Check(bool value, string message) { if (!value) throw new InvalidOperationException("Preparation headroom: " + message); checks++; }
		var labels = ChartManager.Instance.GetAllLabels().Where(l => !l.isPlayerOwned && l.IsActive).ToList();
		var label = labels.First(l => l.tier == LabelTier.MidTier);
		var major = labels.First(l => l.tier == LabelTier.Major);
		string[] cmdline = OS.GetCmdlineUserArgs();
		float saved = LabelReleaseProgramme.PreparationHeadroom;
		try {
			bool Rejects(params string[] args) {
				try { LabelReleaseProgramme.Configure(args); return false; } catch (ArgumentException) { return true; }
			}
			Check(Rejects("--slate-preparation-headroom=1.5"), "requires the slate");
			Check(Rejects(LabelReleaseProgramme.AllocationSwitch, "--slate-preparation-headroom=0.5"), "rejects headroom below 1");
			Check(Rejects(LabelReleaseProgramme.AllocationSwitch, "--slate-preparation-headroom=5"), "rejects headroom above 4");
			LabelReleaseProgramme.Configure(new[] { LabelReleaseProgramme.AllocationSwitch, "--slate-preparation-headroom=1.5" });
			Check(LabelReleaseProgramme.PreparationHeadroom == 1.5f, "parses the value");
			LabelReleaseProgramme.Configure(new[] { LabelReleaseProgramme.AllocationSwitch });
			Check(LabelReleaseProgramme.PreparationHeadroom == 1f, "defaults to 1");

			const float rate = .37f;
			Check(LabelReleaseProgramme.PreparationRate(label, rate) == rate && LabelReleaseProgramme.PreparationRate(major, rate) == rate,
				"off passes the planning rate through unchanged");
			LabelReleaseProgramme.SetPreparationHeadroomForChecks(2f);
			Check(LabelReleaseProgramme.PreparationRate(major, rate) == rate, "Majors are excluded");
			Check(Math.Abs(LabelReleaseProgramme.PreparationRate(label, rate) - .74f) < 1e-6, "non-Majors scale by H");
			Check(LabelReleaseProgramme.PreparationRate(label, 0f) == 0f, "a zero rate (bankrupt, Defunct) stays zero");
			// The slot draw is the same keyed roll at both rates, so headroom only adds draws: never fewer slots.
			int fewer = 0, more = 0;
			for (int i = 0; i < 1000; i++) {
				double roll = (i + .5) / 1000.0;
				int off = LabelReleaseProgramme.Slots(rate, roll), on = LabelReleaseProgramme.Slots(LabelReleaseProgramme.PreparationRate(label, rate), roll);
				if (on < off) fewer++;
				more += on;
			}
			Check(fewer == 0, "headroom never removes a preparation slot");
			Check(Math.Abs(more / 1000f - .74f) < .005f, "mean slots equal the scaled rate");
		} finally {
			LabelReleaseProgramme.Configure(cmdline);
			LabelReleaseProgramme.SetPreparationHeadroomForChecks(saved);
		}
		GD.Print($"PREPARATION_HEADROOM_CHECK passed checks={checks}");
	}
}
