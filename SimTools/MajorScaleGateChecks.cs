using System;
using System.Collections.Generic;
using Godot;

/// <summary>--major-scale-gate-check: off and ledger-less pass every candidate; on, a MidTier passes only at or above
/// the ratio times the smallest active Major's recent gross, ignoring inactive Majors and the candidate itself.</summary>
public static class MajorScaleGateChecks {
	public static void Run() {
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Major scale gate: " + why); }
		float ratioWas = MajorScaleGate.Ratio;
		bool ledger = LabelCorporateModel.Ledger, staff = LabelCorporateModel.Staff, premises = LabelCorporateModel.Premises,
			tax = LabelCorporateModel.Tax, distributions = LabelCorporateModel.Distributions, capital = LabelCorporateModel.Capital,
			commercial = LabelCorporateModel.Commercial, excise = LabelCorporateModel.RecordExcise;
		AILabel Label(string id, LabelTier tier, double revenue, LabelStatus status = LabelStatus.Stable) => new AILabel {
			labelId = id, labelName = id, tier = tier, status = status,
			company = new LabelCompanyState { accountsOpened = true, recentRevenue = revenue } };
		try {
			var big = Label("big", LabelTier.Major, 2_000_000);
			var small = Label("small", LabelTier.Major, 1_000_000);
			var gone = Label("gone", LabelTier.Major, 100_000, LabelStatus.Defunct);
			var cameo = Label("cameo", LabelTier.MidTier, 500_000);
			var labels = new List<AILabel> { big, small, gone, cameo };

			LabelCorporateModel.SetForChecks(true);
			MajorScaleGate.SetRatioForChecks(0f);
			Check(!MajorScaleGate.Enabled && MajorScaleGate.Passes(cameo, labels), "off must pass every candidate");

			MajorScaleGate.SetRatioForChecks(0.75f);
			Check(MajorScaleGate.SmallestMajorRevenue(labels, cameo) == 1_000_000, "the bar is the smallest ACTIVE Major");
			Check(!MajorScaleGate.Passes(cameo, labels), "0.50x the smallest Major must hold at 0.75");
			cameo.company.recentRevenue = 750_000;
			Check(MajorScaleGate.Passes(cameo, labels), "exactly the bar must pass");
			cameo.company.recentRevenue = 500_000;
			MajorScaleGate.SetRatioForChecks(0.5f);
			Check(MajorScaleGate.Passes(cameo, labels), "the ratio must be the configured one");

			MajorScaleGate.SetRatioForChecks(0.75f);
			Check(MajorScaleGate.Passes(cameo, new List<AILabel> { cameo }), "no Major to compare with must pass");
			LabelCorporateModel.SetForChecks(false);
			Check(MajorScaleGate.Passes(cameo, labels), "without the company ledger the gate must pass");
			GD.Print("MAJOR_SCALE_GATE_CHECK passed");
		} finally {
			MajorScaleGate.SetRatioForChecks(ratioWas);
			LabelCorporateModel.SetForChecks(ledger, staff, premises, tax, distributions, capital, commercial, excise);
		}
	}
}
