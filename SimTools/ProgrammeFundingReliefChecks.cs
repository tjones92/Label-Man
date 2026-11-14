using System;
using Godot;

/// <summary>--programme-funding-relief-check: the going-concern reserve reaches non-MidTier work-budget labels only with
/// --slate-going-concern-reserve-all-tiers and never a Major; the overdraft draw is the whole shortfall when the unused
/// headroom covers it, else nothing, and nothing when off; malformed combinations are refused.</summary>
public static class ProgrammeFundingReliefChecks {
	public static void Run() {
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Programme funding relief: " + why); }
		bool allWas = ProgrammeFundingRelief.AllTiers, odWas = ProgrammeFundingRelief.OverdraftFunding;
		bool gcWas = LabelReleaseProgramme.GoingConcernReserveEnabled;
		float overdraftWas = ReceivableBackedOverdraft.Share;
		try {
			var mid = new AILabel { labelId = "mid", tier = LabelTier.MidTier };
			var independent = new AILabel { labelId = "ind", tier = LabelTier.Independent };
			var small = new AILabel { labelId = "small", tier = LabelTier.Small };
			var major = new AILabel { labelId = "major", tier = LabelTier.Major };
			LabelReleaseProgramme.SetWorkBudgetForChecks(1f);
			LabelReleaseProgramme.SetGoingConcernReserveForChecks(true);

			ProgrammeFundingRelief.SetForChecks(false, false);
			Check(LabelReleaseProgramme.UsesGoingConcernReserve(mid) && !LabelReleaseProgramme.UsesGoingConcernReserve(independent),
				"off, only MidTiers use the going-concern reserve");
			ProgrammeFundingRelief.SetForChecks(true, false);
			Check(LabelReleaseProgramme.UsesGoingConcernReserve(mid) && LabelReleaseProgramme.UsesGoingConcernReserve(independent) &&
				LabelReleaseProgramme.UsesGoingConcernReserve(small), "all tiers must reach Independent and Small labels");
			Check(!LabelReleaseProgramme.UsesGoingConcernReserve(major), "a Major must never use it");
			LabelReleaseProgramme.SetGoingConcernReserveForChecks(false);
			Check(!LabelReleaseProgramme.UsesGoingConcernReserve(independent), "all tiers must need the going-concern reserve itself");

			ReceivableBackedOverdraft.SetForChecks(.75f);
			ProgrammeFundingRelief.SetForChecks(false, false);
			Check(ProgrammeFundingRelief.Draw(100f, 50f, 1000f) == 0f, "off must draw nothing");
			ProgrammeFundingRelief.SetForChecks(false, true);
			Check(ProgrammeFundingRelief.OverdraftHeadroom(-200f, 1000f) == 550f, "headroom = 0.75 x receivables less the overdraft drawn");
			Check(ProgrammeFundingRelief.Draw(500f, -200f, 1000f) == 500f, "a covered shortfall must be drawn in full");
			Check(ProgrammeFundingRelief.Draw(600f, -200f, 1000f) == 0f, "an uncovered shortfall must draw nothing");
			Check(ProgrammeFundingRelief.Draw(0f, 0f, 1000f) == 0f, "no shortfall must draw nothing");

			bool Refused(params string[] args) {
				typeof(ProgrammeFundingRelief).GetField("configured", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
					.SetValue(null, false);
				try { ProgrammeFundingRelief.Configure(args); return false; } catch (ArgumentException) { return true; }
			}
			Check(Refused(ProgrammeFundingRelief.AllTiersSwitch), "all tiers without the going-concern reserve must be refused");
			Check(Refused(ProgrammeFundingRelief.OverdraftSwitch), "overdraft funding without the overdraft must be refused");
			Check(!Refused(ProgrammeFundingRelief.AllTiersSwitch, LabelReleaseProgramme.GoingConcernReserveSwitch,
				ProgrammeFundingRelief.OverdraftSwitch, ReceivableBackedOverdraft.Switch + "=0.75"), "a valid set must configure");
			GD.Print("PROGRAMME_FUNDING_RELIEF_CHECK passed");
		} finally {
			ProgrammeFundingRelief.SetForChecks(allWas, odWas);
			LabelReleaseProgramme.SetGoingConcernReserveForChecks(gcWas);
			ReceivableBackedOverdraft.SetForChecks(overdraftWas);
		}
	}
}
