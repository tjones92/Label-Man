using System;
using System.Text.Json;
using Godot;

public static class FounderCompensationChecks {
    public static void Run() {
        int checks = 0;
        void Check(bool value, string why) { if (!value) throw new InvalidOperationException("Founder compensation: " + why); checks++; }
        bool Near(double a, double b) => Math.Abs(a - b) < .01;
        int january = 1960 * 12;
        LabelCorporateModel.SetForChecks(true, staff: true, premises: true);
        LabelCorporateModel.SetHomeForChecks(true, 25);
        AILabel Firm() {
            var label = new AILabel { labelId = "compensation-check", labelName = "Compensation Check",
                tier = LabelTier.Boutique, status = LabelStatus.Stable, homeCityId = "memphis", cashReserves = 100000,
                company = new LabelCompanyState { ownership = LabelOwnershipForm.PrivateCorporation } };
            LabelCompanyAccounts.Open(label);
            return label;
        }
        FounderCompensation.SetForChecks(false);
        var off = Firm();
        double quote = off.company.founderPayrollQuote;
        Check(quote > 0 && Near(off.GetMonthlyOverhead(), quote + 25), "off preserves fixed incorporated founder salary");
        Check(FounderCompensation.Pay(off) == 0, "off pays no extra compensation");
        FounderCompensation.SetForChecks(true);
        var quiet = Firm();
        Check(Near(quiet.GetMonthlyOverhead(), 25) && quiet.company.founderPayrollQuote > 0,
            "nominal work quote is retained but no longer a fixed bill");
        Check(quiet.company.organizationCoverage == 1, "founder work coverage remains present");
        Check(FounderCompensation.Pay(quiet) == 0, "opening cash is not earned pay");
        LabelCompanyAccounts.ChangeCash(quiet, 1000, LabelCashKind.Financing);
        Check(FounderCompensation.Pay(quiet) == 0, "borrowing cannot fund compensation");
        LabelCompanyAccounts.ChangeCash(quiet, 1000, LabelCashKind.OwnerDistribution);
        Check(FounderCompensation.Pay(quiet) == 0, "incoming dividends are not operating earnings");
        quiet.outstandingWholesaleReceivables = 1000;
        Check(FounderCompensation.Pay(quiet) == 0, "uncollected sales do not fund pay");
        var earning = Firm();
        LabelCompanyAccounts.RecordSale(earning, 100, 100);
        LabelCompanyAccounts.ChangeCash(earning, 100, LabelCashKind.RecordReceipts);
        LabelCompanyAccounts.OverheadPaid(earning, earning.GetMonthlyOverhead());
        float paid = FounderCompensation.Pay(earning);
        Check(Near(paid, 37.5), "half of collected surplus after fixed bills is paid");
        Check(FounderCompensation.Pay(earning) == 0, "a second call cannot pay twice in the same month");
        var statement = LabelCompanyAccounts.CloseMonth(earning, january);
        Check(Near(statement.ownerSalary, paid) && Near(statement.payroll, 0) && Near(statement.premises, 25),
            "contingent pay is booked once as owner salary, separate from fixed costs");
        Check(Near(statement.pretaxEarnings, 37.5) && Near(statement.cashReconciliationError, 0),
            "company earnings and cash journal reconcile after compensation");
        var hit = Firm(); LabelCompanyAccounts.ChangeCash(hit, 100000, LabelCashKind.RecordReceipts);
        Check(Near(FounderCompensation.Pay(hit), hit.company.founderPayrollQuote), "pay cannot exceed the nominal work ceiling");
        foreach (string reserve in new[] { "tax", "plant", "commercial" }) {
            var protectedFirm = Firm(); LabelCompanyAccounts.ChangeCash(protectedFirm, 1000, LabelCashKind.RecordReceipts);
            if (reserve == "tax") protectedFirm.company.taxPayable = 200000;
            if (reserve == "plant") protectedFirm.outstandingPlantPayables = 200000;
            if (reserve == "commercial") protectedFirm.company.recentCommercialSpend = 200000;
            Check(FounderCompensation.Pay(protectedFirm) == 0, reserve + " reserves remain unavailable");
        }
        var losing = Firm(); LabelCompanyAccounts.ChangeCash(losing, -100, LabelCashKind.OtherOperating);
        Check(FounderCompensation.Pay(losing) == 0, "losses do not create compensation or wage arrears");
        var staffed = Firm(); staffed.company.departments[0].employees = .25;
        Check(!FounderCompensation.Applies(staffed, staffed.company), "paid-staff firms retain ordinary compensation");
        var sole = Firm(); sole.company.ownership = LabelOwnershipForm.OwnerOperated;
        Check(!FounderCompensation.Applies(sole, sole.company), "sole operators retain existing earnings draws");
        var player = Firm(); player.isPlayerOwned = true;
        Check(!FounderCompensation.Applies(player, player.company), "player finances are excluded");
        var subsidiary = Firm(); subsidiary.ownerLabelId = "parent";
        Check(!FounderCompensation.Applies(subsidiary, subsidiary.company), "subsidiary employment is excluded");
        var large = Firm(); large.tier = LabelTier.MidTier;
        Check(!FounderCompensation.Applies(large, large.company), "larger employers retain their ordinary wage bills");
        var restored = JsonSerializer.Deserialize<AILabel>(JsonSerializer.Serialize(earning, SaveGameService.TestJsonOptions), SaveGameService.TestJsonOptions);
        Check(Near(restored.company.lastStatement.ownerSalary, paid) && Near(restored.GetMonthlyOverhead(), 25),
            "saved accounts and nominal quote resume with the policy enabled");
        GameRandom.Seed(4832); float next = GameRandom.Randf(); GameRandom.Seed(4832);
        FounderCompensation.Pay(Firm());
        Check(next == GameRandom.Randf(), "compensation consumes no random draws");
        // --graduated-founder-overhead (§37): up to T paid staff keep home premises and contingent founder pay.
        AILabel Staffed(double employees) {
            var firm = Firm();
            firm.company.departments[0].employees = employees;
            LabelCorporateModel.ReviewOrganization(firm, january + 1, 0, 0); // February: requote, no quarterly hiring
            return firm;
        }
        GraduatedFounderOverhead.SetForChecks(false);
        var cliff = Staffed(.25);
        float cliffOverhead = cliff.GetMonthlyOverhead();
        Check(!FounderCompensation.Applies(cliff, cliff.company) && cliff.company.leasedOfficeSuites >= 1
            && cliffOverhead > cliff.company.founderPayrollQuote, "off: a quarter-employee switches on fixed founder pay and a suite");
        GraduatedFounderOverhead.SetForChecks(true, 2);
        var graded = Staffed(.25);
        double staffWages = graded.company.payrollQuote - graded.company.founderPayrollQuote;
        Check(FounderCompensation.Applies(graded, graded.company) && graded.company.leasedOfficeSuites == 0,
            "on: a founder with a quarter-employee stays home-run on contingent pay");
        Check(staffWages > 0 && Near(graded.GetMonthlyOverhead(), staffWages + 12 * .25 + 25),
            "on: overhead is the hired wages, a desk allowance and home running cost");
        Check(cliffOverhead - graded.GetMonthlyOverhead() >= cliff.company.founderPayrollQuote + cliff.company.premisesQuote - 25 - 12 * .25 - .01,
            $"on: the founder salary and suite are removed ({cliffOverhead:F0} -> {graded.GetMonthlyOverhead():F0})");
        Check(!LabelCorporateModel.IsFounderRun(graded, graded.company), "founder dormancy still needs a firm with no paid staff");
        var grown = Staffed(2.5);
        Check(!FounderCompensation.Applies(grown, grown.company) && grown.company.leasedOfficeSuites >= 1,
            "on: staff above the limit lease an office and salary the founder as before");
        var bare = Staffed(0);
        Check(Near(bare.GetMonthlyOverhead(), 25), "on: a firm with no staff is unchanged");
        GameRandom.Seed(4833); float nextGraded = GameRandom.Randf(); GameRandom.Seed(4833);
        Staffed(1);
        Check(nextGraded == GameRandom.Randf(), "graduated overhead consumes no random draws");
        GraduatedFounderOverhead.SetForChecks(false);
        foreach (var args in new[] { new[] { "--graduated-founder-overhead=0.1" }, new[] { "--graduated-founder-overhead=x" } }) {
            bool threw = false;
            try { GraduatedFounderOverhead.Configure(args); } catch (ArgumentException) { threw = true; }
            Check(threw, "bad staff limits are refused: " + args[0]);
        }
        GraduatedFounderOverhead.Configure(new[] { "--graduated-founder-overhead" });
        Check(GraduatedFounderOverhead.Enabled && Near(GraduatedFounderOverhead.StaffLimit, 2), "bare switch reads two paid staff");
        GraduatedFounderOverhead.Configure(new[] { "--graduated-founder-overhead=1.5" });
        Check(Near(GraduatedFounderOverhead.StaffLimit, 1.5), "explicit staff limit is read");
        GraduatedFounderOverhead.Configure(Array.Empty<string>());
        Check(!GraduatedFounderOverhead.Enabled && GraduatedFounderOverhead.PaidStaffAllowance == 0, "absent switch is off");
        FounderCompensation.SetForChecks(false);
        Check(Near(quiet.GetMonthlyOverhead(), quiet.company.founderPayrollQuote + 25), "switch off restores existing fixed cost quote");
        GD.Print($"FOUNDER_COMPENSATION_CHECK_PASSED checks={checks}");
    }
}
