using System;
using System.Linq;
using Godot;

/// <summary>--slate-work-budget-check: argument validation; off leaves capacity and status factors unchanged; Majors and the
/// player are excluded; non-Major singles scale by S with LPs unchanged; Struggling keeps its slots and halves its spend.</summary>
public static class WorkBudgetChecks {
	public static void Run() {
		int checks = 0;
		void Check(bool value, string message) { if (!value) throw new InvalidOperationException("Work budget: " + message); checks++; }
		var labels = ChartManager.Instance.GetAllLabels().Where(l => !l.isPlayerOwned && l.IsActive).ToList();
		var label = labels.First(l => l.tier == LabelTier.MidTier);
		var major = labels.First(l => l.tier == LabelTier.Major);
		var independent = labels.First(l => l.tier == LabelTier.Independent);
		string[] cmdline = OS.GetCmdlineUserArgs();
		float saved = LabelReleaseProgramme.WorkBudgetSingles, savedMid = LabelReleaseProgramme.WorkBudgetMidTierSingles;
		LabelStatus savedStatus = label.status, savedMajorStatus = major.status;
		string slate = LabelReleaseProgramme.AllocationSwitch, capacity = LabelReleaseProgramme.CapacitySwitch;
		try {
			bool Rejects(params string[] args) {
				try { LabelReleaseProgramme.Configure(args); return false; } catch (ArgumentException) { return true; }
			}
			Check(Rejects(slate, "--slate-work-budget=1"), "requires revised capacity");
			Check(Rejects(capacity, "--slate-work-budget=1"), "requires the slate");
			Check(Rejects(slate, capacity, "--slate-work-budget=0.1"), "rejects a budget below .25");
			Check(Rejects(slate, capacity, "--slate-work-budget=4"), "rejects a budget above 3");
			Check(Rejects(slate, capacity, "--slate-work-budget=1", "--slate-preparation-headroom=2"), "rejects headroom alongside the budget");
			LabelReleaseProgramme.Configure(new[] { slate, capacity, "--slate-work-budget=1.25" });
			Check(LabelReleaseProgramme.WorkBudgetSingles == 1.25f, "parses the value");
			LabelReleaseProgramme.Configure(new[] { slate, capacity });
			Check(LabelReleaseProgramme.WorkBudgetSingles == 0f, "defaults to off");
			Check(Rejects(slate, capacity, "--slate-work-budget-midtier=1.5"), "the MidTier budget requires the budget");
			Check(Rejects(slate, capacity, "--slate-work-budget=0.67", "--slate-work-budget-midtier=5"), "rejects a MidTier budget above 3");
			LabelReleaseProgramme.Configure(new[] { slate, capacity, "--slate-work-budget=0.67", "--slate-work-budget-midtier=1.5" });
			Check(LabelReleaseProgramme.WorkBudgetMidTierSingles == 1.5f && LabelReleaseProgramme.WorkBudgetSingles == .67f, "parses both budgets");
			LabelReleaseProgramme.Configure(new[] { slate, capacity });
			Check(LabelReleaseProgramme.WorkBudgetMidTierSingles == 0f, "the MidTier budget defaults to off");

			var off = LabelReleaseProgramme.MonthlyCapacity(label);
			Check(!LabelReleaseProgramme.UsesWorkBudget(label) && off.Singles == label.releasesPerMonth * .67f, "off keeps the .67 single envelope");
			foreach (LabelStatus status in Enum.GetValues<LabelStatus>()) {
				label.status = status;
				Check(LabelReleaseProgramme.SlotStatusFactor(label) == LabelReleaseProgramme.StatusFactor(status) &&
					LabelReleaseProgramme.CampaignStatusFactor(label) == 1f, $"off leaves {status} unchanged");
			}
			LabelReleaseProgramme.SetWorkBudgetForChecks(1.5f);
			var on = LabelReleaseProgramme.MonthlyCapacity(label);
			Check(Math.Abs(on.Singles - label.releasesPerMonth * 1.5f) < 1e-5f, "non-Major singles scale by S");
			Check(on.Albums == off.Albums, "LPs keep their envelope");
			Check(!LabelReleaseProgramme.UsesWorkBudget(major) &&
				LabelReleaseProgramme.MonthlyCapacity(major) == (LabelReleaseProgramme.MajorSinglesPerMonth, LabelReleaseProgramme.MajorAlbumsPerMonth),
				"Majors are excluded");
			Check(Math.Abs(LabelReleaseProgramme.MonthlyCapacity(independent).Singles - independent.releasesPerMonth * 1.5f) < 1e-5f,
				"without a MidTier budget every non-Major takes S");
			LabelReleaseProgramme.SetWorkBudgetForChecks(.67f, 2f);
			Check(Math.Abs(LabelReleaseProgramme.MonthlyCapacity(label).Singles - label.releasesPerMonth * 2f) < 1e-5f, "MidTiers take the MidTier budget");
			Check(Math.Abs(LabelReleaseProgramme.MonthlyCapacity(independent).Singles - independent.releasesPerMonth * .67f) < 1e-5f, "smaller tiers keep S");
			Check(LabelReleaseProgramme.MonthlyCapacity(major) == (LabelReleaseProgramme.MajorSinglesPerMonth, LabelReleaseProgramme.MajorAlbumsPerMonth),
				"Majors ignore the MidTier budget");
			label.status = LabelStatus.Struggling;
			Check(LabelReleaseProgramme.SlotStatusFactor(label) == 1f, "Struggling keeps its slots");
			Check(LabelReleaseProgramme.CampaignStatusFactor(label) == .5f, "Struggling halves its campaign spend");
			foreach (LabelStatus status in Enum.GetValues<LabelStatus>().Where(s => s != LabelStatus.Struggling)) {
				label.status = status;
				Check(LabelReleaseProgramme.SlotStatusFactor(label) == LabelReleaseProgramme.StatusFactor(status) &&
					LabelReleaseProgramme.CampaignStatusFactor(label) == 1f, $"{status} is unchanged");
			}
			label.status = savedStatus;
			GoingConcern(label, independent, major, Check, Rejects, slate, capacity);
			major.status = LabelStatus.Struggling;
			Check(LabelReleaseProgramme.SlotStatusFactor(major) == .5f && LabelReleaseProgramme.CampaignStatusFactor(major) == 1f,
				"a Struggling Major keeps the old slot cut");
		} finally {
			label.status = savedStatus; major.status = savedMajorStatus;
			LabelReleaseProgramme.Configure(cmdline);
			LabelReleaseProgramme.SetWorkBudgetForChecks(saved, savedMid);
			LabelReleaseProgramme.SetGoingConcernReserveForChecks(cmdline.Contains(LabelReleaseProgramme.GoingConcernReserveSwitch));
		}
		GD.Print($"WORK_BUDGET_CHECK passed checks={checks}");
	}

	// --slate-going-concern-reserve: switch validation, scope (work-budget MidTiers only), the reserve identity, and the
	// pre-1964 tax schedule month by month on fixed books.
	private static void GoingConcern(AILabel mid, AILabel independent, AILabel major, Action<bool, string> Check,
		Func<string[], bool> Rejects, string slate, string capacity) {
		string sw = LabelReleaseProgramme.GoingConcernReserveSwitch;
		Check(Rejects(new[] { slate, capacity, sw }), "the going-concern reserve requires the work budget");
		LabelReleaseProgramme.Configure(new[] { slate, capacity, "--slate-work-budget=0.67", sw });
		Check(LabelReleaseProgramme.GoingConcernReserveEnabled, "parses the going-concern switch");
		var cm = CompetitorManager.Instance;
		Check(LabelReleaseProgramme.UsesGoingConcernReserve(mid) && !LabelReleaseProgramme.UsesGoingConcernReserve(independent) &&
			!LabelReleaseProgramme.UsesGoingConcernReserve(major), "applies to work-budget MidTiers only");
		Check(cm.ProgrammeCommitmentReserve(independent) == EconomicLabelLifecycle.CommitmentReserve(independent, false) &&
			cm.ProgrammeCommitmentReserve(major) == EconomicLabelLifecycle.CommitmentReserve(major, false), "other tiers keep the ordinary reserve");
		var company = LabelCorporateModel.Ensure(mid);
		if (company == null || !LabelCorporateModel.Tax || company.ownership == LabelOwnershipForm.OwnerOperated) {
			Check(cm.ProgrammeCommitmentReserve(mid) == EconomicLabelLifecycle.CommitmentReserve(mid, false), "untaxed MidTier keeps the ordinary reserve");
			LabelReleaseProgramme.Configure(new[] { slate, capacity });
			return;
		}
		var saved = (company.taxPayable, company.priorYearTaxDue, company.priorYearTaxInstallment, company.yearTaxProvision, company.currentYearTaxInstallments);
		bool priorYear = LabelCompanyTax.PriorYear;
		try {
			company.taxPayable = 260000; company.priorYearTaxDue = 60000; company.priorYearTaxInstallment = 30000;
			company.yearTaxProvision = 300000; company.currentYearTaxInstallments = 0;
			LabelCompanyTax.SetPriorYearForChecks(true);
			double Due(int month, int months = 2) => LabelCompanyTax.DueWithin(mid, month, months, 0);
			Check(Due(1) == 0 && Due(4) == 0 && Due(7) == 0 && Due(10) == 0, "no quarter-end in the lead owes nothing");
			Check(Due(2) == 30000 && Due(3) == 30000, "March pays the prior-year installment");
			Check(Due(5) == 60000 && Due(6) == 60000, "June pays the prior-year remainder");
			Check(Due(1, 6) == 60000, "March then June pay the prior year once");
			Check(Due(8) == 50000, "September pays a quarter of the excess over $100,000");
			Check(Due(11) == 100000 && Due(8, 5) == 100000, "December brings current payments to half the excess");
			company.taxPayable = 20000;
			Check(Due(11) == 20000, "never more than is payable");
			LabelCompanyTax.SetPriorYearForChecks(false);
			Check(Due(2) == 20000 && Due(1) == 0, "without the prior-year rule a quarter-end pays everything payable");
			double expected = EconomicLabelLifecycle.CommitmentReserve(mid, false) - LabelCorporateModel.TaxAndServiceReserve(mid) +
				LabelCorporateModel.ProgrammeTaxDue(mid, TimeManager.Instance?.CurrentDate.month ?? 1, LabelReleaseProgramme.GoingConcernTaxMonths);
			Check(Math.Abs(cm.ProgrammeCommitmentReserve(mid) - expected) < .5, "MidTier reserve swaps the tax-and-service hold for tax due in the lead");
			Check(cm.ProgrammeCommitmentReserve(mid) <= EconomicLabelLifecycle.CommitmentReserve(mid, false) + .5f, "the going-concern reserve never exceeds the ordinary one");
		} finally {
			(company.taxPayable, company.priorYearTaxDue, company.priorYearTaxInstallment, company.yearTaxProvision, company.currentYearTaxInstallments) = saved;
			LabelCompanyTax.SetPriorYearForChecks(priorYear);
			LabelReleaseProgramme.Configure(new[] { slate, capacity });
		}
	}
}
