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
        FounderCompensation.SetForChecks(false);
        Check(Near(quiet.GetMonthlyOverhead(), quiet.company.founderPayrollQuote + 25), "switch off restores existing fixed cost quote");
        GD.Print($"FOUNDER_COMPENSATION_CHECK_PASSED checks={checks}");
    }
}
