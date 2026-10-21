using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

public static class LabelCompanyChecks {
    private static int checks;
    private static void Check(bool condition, string reason) {
        if (!condition) throw new InvalidOperationException("Company model: " + reason);
        checks++;
    }
    private static bool Near(double a, double b, double tolerance = .01) => Math.Abs(a - b) < tolerance;
    private static AILabel Firm(string id = "company-check", float cash = 10000, LabelTier tier = LabelTier.Independent) =>
        new() { labelId = id, tier = tier, cashReserves = cash, status = LabelStatus.Stable, homeCityId = "memphis" };

    public static void Run() {
        checks = 0;
        var saved = (LabelCorporateModel.Ledger, LabelCorporateModel.Staff, LabelCorporateModel.Premises,
            LabelCorporateModel.Tax, LabelCorporateModel.Distributions, LabelCorporateModel.Capital,
            LabelCorporateModel.Commercial, LabelCorporateModel.RecordExcise);
        try {
            LabelCorporateModel.SetForChecks(false);
            var off = Firm(); float original = off.cashReserves;
            LabelCompanyAccounts.ChangeCash(off, -123.45f, LabelCashKind.OtherOperating);
            Check(off.cashReserves == original - 123.45f && off.company == null, "off retains float cash arithmetic and absent save fields");
            var player = Firm(); player.isPlayerOwned = true;
            LabelCorporateModel.SetForChecks(true, true, true, true, true, true);
            Check(LabelCorporateModel.Ensure(player) == null && LabelCorporateModel.CapacityFactor(player) == 1,
                "AI corporate budgets do not attach to a player founder");

            int january = 1960 * 12, march = january + 2, december = january + 11;
            LabelCorporateModel.SetForChecks(true);
            var ledger = Firm(); LabelCompanyAccounts.Open(ledger);
            LabelCompanyAccounts.ChangeCash(ledger, 800, LabelCashKind.RecordReceipts);
            ledger.outstandingWholesaleReceivables = 100;
            ledger.outstandingPlantPayables = 500;
            ledger.company.inventoryValue = 400;
            var statement = LabelCompanyAccounts.CloseMonth(ledger, january);
            Check(Near(statement.pretaxEarnings, 800) && Near(statement.cashReconciliationError, 0),
                "earned contribution recognizes AR, unpaid plant and unsold stock without charging cash twice");
            Check(ReferenceEquals(LabelCompanyAccounts.CloseMonth(ledger, january), statement) && Near(ledger.company.undistributedEarnings, 800),
                "repeated month close cannot double earnings or settlement");
            float settledCash = ledger.cashReserves;
            CompetitorManager.Instance.ProcessLabelMonthForCompanyChecks(ledger, new GameDate(1960, 2, 1));
            Check(ledger.cashReserves == settledCash, "replaying the monthly handler cannot debit overhead after that month already closed");

            var finance = Firm(); LabelCompanyAccounts.Open(finance);
            LabelCompanyAccounts.ChangeCash(finance, 5000, LabelCashKind.Financing);
            Check(Near(LabelCompanyAccounts.CloseMonth(finance, january).pretaxEarnings, 0), "loan receipts are not profit");
            var investor = Firm(); LabelCompanyAccounts.Open(investor);
            LabelCompanyAccounts.ChangeCash(investor, -1000, LabelCashKind.StudioInvestment);
            LabelCorporateModel.AddAsset(investor, "check", 1000, 10);
            var investment = LabelCompanyAccounts.CloseMonth(investor, january);
            Check(Near(investment.pretaxEarnings, -100) && Near(investment.capitalBookValue, 900) && Near(investment.capitalSpending, 1000),
                "capex consumes cash once; only depreciation affects earnings");

            var stock = Firm(); LabelCompanyAccounts.Open(stock);
            var record = new RecordRuntimeData { baseRecord = new Record { recordId = "inventory-check" }, unitsPressed = 1000, pressingBookSold = 100 };
            LabelCompanyAccounts.Inventory(stock, record, 1000, 0, 100, .1);
            stock.outstandingPlantPayables = 100;
            Check(Near(stock.company.inventoryValue, 90), "sold copies consume weighted-cost inventory");
            LabelCompanyAccounts.RetireInventory(stock, "inventory-check");
            var retired = LabelCompanyAccounts.CloseMonth(stock, january);
            Check(Near(retired.inventoryWriteOffs, 90) && Near(retired.pretaxEarnings, -100) && stock.cashReserves == 10000,
                "unsold retirement recognizes a loss without a second cash debit");

            Check(Near(LabelCompanyTax.Liability(1963, 100000), 46500), "1960-63 progressive corporate brackets");
            Check(Near(LabelCompanyTax.Liability(1964, 100000), 43000), "1964 normal/surtax reduction");
            Check(Near(LabelCompanyTax.Liability(1965, 100000), 41500), "1965 standard brackets");
            Check(Near(LabelCompanyTax.Liability(1968, 100000), 45650) && Near(LabelCompanyTax.Liability(1970, 100000), 42537.5),
                "late-decade annual surcharge schedules");
            Check(LabelCompanyTax.Liability(1963, -1000) == 0, "negative taxable earnings have no positive tax");
            LabelCorporateModel.SetForChecks(true, tax: true);
            var loss = Firm(); LabelCompanyAccounts.Open(loss);
            LabelCompanyTax.Accrue(loss, december, -10000);
            Check(loss.company.losses.Count == 1 && loss.company.losses[0].expiresAfterYear == 1965, "annual losses create expiring forward vintages");
            Check(Near(LabelCompanyTax.Accrue(loss, january + 12, 25000), 4500), "prior losses offset future taxable earnings before brackets");
            var reversal = Firm(cash: 100000); LabelCompanyAccounts.Open(reversal);
            LabelCompanyTax.Accrue(reversal, march, 100000); LabelCompanyTax.PayQuarter(reversal, march);
            Check(Near(reversal.cashReserves, 53500), "quarterly tax obligation is paid");
            Check(Near(LabelCompanyTax.Accrue(reversal, december, -100000), -46500), "later losses reverse the year's provision");
            LabelCompanyTax.PayQuarter(reversal, december);
            Check(Near(reversal.cashReserves, 100000) && Near(reversal.company.taxPayable, 0), "annual true-up refunds overpaid current-year installments");
            var sole = Firm(tier: LabelTier.Small); LabelCompanyAccounts.Open(sole);
            Check(LabelCompanyTax.Accrue(sole, march, 100000) == 0, "owner-operated pass-through income is not taxed again as corporate earnings");

            LabelCorporateModel.SetForChecks(true, true, true);
            var staffed = Firm(cash: 100, tier: LabelTier.MidTier); LabelCompanyAccounts.Open(staffed);
            float bill = staffed.GetMonthlyOverhead(); LabelCompanyAccounts.OverheadPaid(staffed, bill);
            var payroll = LabelCompanyAccounts.CloseMonth(staffed, january);
            Check(staffed.cashReserves < 0 && payroll.payroll > 0, "employees and premises remain payable in losing months");
            Check(Near(payroll.payroll + payroll.ownerSalary + payroll.retainers + payroll.premises, bill) && Near(payroll.legacyOverhead, 0),
                "detailed organization replaces the legacy charge and allocates it once");
            var downsizing = Firm(); LabelCompanyAccounts.Open(downsizing);
            var excess = downsizing.company.departments.First(d => d.department == LabelDepartment.Clerical);
            excess.employees = 10;
            LabelCorporateModel.ReviewOrganization(downsizing, march, 0, 0);
            Check(excess.employees == 10, "staffing does not vanish on the first low-workload review");
            LabelCorporateModel.ReviewOrganization(downsizing, march + 3, 0, 0);
            Check(excess.employees < 10 && downsizing.company.cashMovements[LabelCashKind.Restructuring] < 0,
                "persistent excess capacity contracts after two reviews with severance");

            LabelCorporateModel.SetForChecks(true, distributions: true);
            var child = Firm("child"); var parent = Firm("parent");
            LabelCompanyAccounts.Open(child); LabelCompanyAccounts.Open(parent);
            child.ownerLabelId = parent.labelId;
            child.company.undistributedEarnings = 1000; child.company.lastStatement = new LabelCompanyStatement { afterTaxEarnings = 1000 };
            child.recentMonthlyCashFlows.Add(1000);
            double totalCash = child.cashReserves + parent.cashReserves;
            LabelCorporateModel.PayDistribution(child, march, id => id == parent.labelId ? parent : child);
            Check(Near(child.cashReserves + parent.cashReserves, totalCash) && Near(child.company.undistributedEarnings, 800),
                "a parent distribution transfers cash and consumes undistributed earnings once");
            float once = child.cashReserves;
            LabelCorporateModel.PayDistribution(child, march, id => parent);
            Check(child.cashReserves == once, "a repeated quarterly payout is inert");
            Check(Near(LabelCompanyAccounts.CloseMonth(parent, march).pretaxEarnings, 0), "intra-sector owner receipts are not taxed a second time as operating earnings");
            Check(Near(parent.company.undistributedEarnings, 200), "a parent can subsequently distribute received subsidiary earnings");
            var missing = Firm(); LabelCompanyAccounts.Open(missing); missing.ownerLabelId = "missing";
            missing.company.undistributedEarnings = 1000; missing.company.lastStatement = new LabelCompanyStatement { afterTaxEarnings = 1000 };
            missing.recentMonthlyCashFlows.Add(1000);
            LabelCorporateModel.PayDistribution(missing, march, _ => null);
            Check(missing.cashReserves == 10000, "missing ownership does not silently destroy a dividend");
            var opening = Firm(); LabelCompanyAccounts.Open(opening); opening.recentMonthlyCashFlows.Add(1000);
            opening.company.lastStatement = new LabelCompanyStatement { afterTaxEarnings = 1000 };
            LabelCorporateModel.PayDistribution(opening, march, _ => null);
            Check(opening.cashReserves == 10000, "opening capital cannot be distributed as unearned profits");
            var cyclic = Firm("cycle"); LabelCompanyAccounts.Open(cyclic); cyclic.ownerLabelId = "cycle";
            cyclic.company.undistributedEarnings = 1000; cyclic.company.lastStatement = new LabelCompanyStatement { afterTaxEarnings = 1000 };
            cyclic.recentMonthlyCashFlows.Add(1000);
            LabelCorporateModel.PayDistribution(cyclic, march, _ => cyclic);
            Check(cyclic.cashReserves == 10000, "cyclic ownership cannot create payout money");
            (float parentCash, float childCash) PayoutOrder(bool reverse) {
                var parentFirm = Firm("batch-parent"); var childFirm = Firm("batch-child");
                foreach (var firm in new[] { parentFirm, childFirm }) {
                    LabelCompanyAccounts.Open(firm); firm.company.undistributedEarnings = 1000;
                    firm.company.lastStatement = new LabelCompanyStatement { afterTaxEarnings = 1000 }; firm.recentMonthlyCashFlows.Add(1000);
                }
                childFirm.ownerLabelId = parentFirm.labelId;
                LabelCorporateModel.PayAllDistributions(reverse ? new[] { childFirm, parentFirm } : new[] { parentFirm, childFirm },
                    march, id => id == parentFirm.labelId ? parentFirm : childFirm);
                return (parentFirm.cashReserves, childFirm.cashReserves);
            }
            Check(PayoutOrder(false) == PayoutOrder(true), "group payout decisions are independent of label iteration order");

            LabelCorporateModel.SetForChecks(true, true, true);
            var founder = Firm(tier: LabelTier.Small); LabelCompanyAccounts.Open(founder);
            Check(founder.company.departments.Sum(d => d.founderEquivalent) <= 1.000001 && founder.company.payrollQuote == 0,
                "one founder shares a single time budget without a mandatory imaginary payroll");
            var incorporatedFounder = Firm(tier: LabelTier.Boutique); LabelCompanyAccounts.Open(incorporatedFounder);
            Check(incorporatedFounder.company.ownership == LabelOwnershipForm.PrivateCorporation
                && incorporatedFounder.company.departments.Sum(d => d.employees) == 0
                && incorporatedFounder.company.founderPayrollQuote > 0,
                "a small incorporated founder uses shared work hours and receives pay without hiring an imaginary department team");
            float founderBill = incorporatedFounder.GetMonthlyOverhead(); LabelCompanyAccounts.OverheadPaid(incorporatedFounder, founderBill);
            var founderStatement = LabelCompanyAccounts.CloseMonth(incorporatedFounder, january);
            Check(founderStatement.ownerSalary > 0 && Near(founderStatement.payroll, 0)
                && Near(founderStatement.ownerSalary + founderStatement.retainers + founderStatement.premises, founderBill)
                && Near(founderStatement.pretaxEarnings, -founderBill), "incorporated founder pay is a real cash expense, allocated once");
            LabelCorporateModel.SetForChecks(true, tax: true);
            Check(Near(LabelCompanyTax.Accrue(incorporatedFounder, 1964 * 12, 100000), 43000),
                "founder-managed organization does not grant a private corporation the sole-operator tax exemption");
            LabelCorporateModel.SetForChecks(true, true, true);
            var largeFounder = Firm(tier: LabelTier.Major); LabelCompanyAccounts.Open(largeFounder);
            largeFounder.company.ownership = LabelOwnershipForm.OwnerOperated;
            LabelCorporateModel.InitializeOpeningOrganization(largeFounder);
            Check(largeFounder.company.departments.Sum(d => d.founderEquivalent) <= 1.000001
                && largeFounder.company.departments.Sum(d => d.employees) > 10,
                "a large owner-operated company needs corporate staffing beyond its single founder");
            int leased = downsizing.company.leasedOfficeSuites;
            Check(leased >= 2, "staff reductions do not immediately cancel the annual office lease");

            LabelCorporateModel.SetForChecks(true, capital: true);
            var upgrade = Firm(cash: 100000); LabelCompanyAccounts.Open(upgrade); upgrade.productionQuality = .5f;
            upgrade.recentMonthlyCashFlows.Add(1000); upgrade.company.lastStatement = new LabelCompanyStatement { afterTaxEarnings = 1000 };
            Check(LabelCorporateModel.TryProductionUpgrade(upgrade, 1964, 3) && upgrade.cashReserves < 100000 && upgrade.productionQuality > .5f,
                "a production upgrade buys capability and creates a depreciable asset");
            float after = upgrade.cashReserves;
            Check(!LabelCorporateModel.TryProductionUpgrade(upgrade, 1964, 3) && upgrade.cashReserves == after,
                "production investment cannot repeat in the same quarter");
            var poor = Firm(cash: 0); poor.productionQuality = .5f;
            Check(!LabelCorporateModel.TryProductionUpgrade(poor, 1964, 3) && poor.productionQuality == .5f, "unfunded upgrades confer no quality gain");

            var persisted = Firm(); LabelCompanyAccounts.Open(persisted);
            LabelCompanyAccounts.ChangeCash(persisted, 123.45f, LabelCashKind.RecordReceipts);
            var options = new JsonSerializerOptions { IncludeFields = true };
            string payload = JsonSerializer.Serialize(persisted.company, options);
            var restored = JsonSerializer.Deserialize<LabelCompanyState>(payload, options);
            Check(JsonSerializer.Serialize(restored, options) == payload, "mid-month journal, capacities, inventory, assets and tax fields round-trip");
            persisted.company = restored;
            Check(Near(LabelCompanyAccounts.CloseMonth(persisted, january).pretaxEarnings, 123.45), "a restored mid-month journal closes without losing receipts");
            GameRandom.Seed(547); float expected = GameRandom.Randf(); GameRandom.Seed(547);
            LabelCompanyAccounts.CloseMonth(persisted, january);
            Check(GameRandom.Randf() == expected, "company settlement consumes no simulation RNG");
            LabelCorporateModel.SetForChecks(true, commercial: true, recordExcise: true);
            var commerce = Firm(cash: 10000); LabelCompanyAccounts.Open(commerce);
            var june21 = new GameDate(1965, 6, 21); var june22 = new GameDate(1965, 6, 22);
            Check(Near(LabelCorporateModel.RecordExciseCost(1000, june21), 75)
                && LabelCorporateModel.RecordExciseCost(1000, june22) == 0, "excise stops on the repeal date, not the calendar-year boundary");
            Check(LabelCorporateModel.FulfillmentCost(1000, 0) == 0
                && Near(LabelCorporateModel.FulfillmentCost(1000, 1), 212.5), "outsourced copies do not pay the owned-network service bill");
            Check(LabelCorporateModel.TradeSupportCost(1000, 0) == 0
                && Near(LabelCorporateModel.TradeSupportCost(1000, 1), 50), "outside-house trade support is funded by the house's service fee, not charged to the client again");
            float saleCost = LabelCorporateModel.PaySaleOperatingCosts(commerce, 1000, .8, june21);
            Check(Near(saleCost, LabelCorporateModel.SaleOperatingCost(commerce, 1000, .8, june21)), "quoted commercial costs equal actual cash charges");
            LabelCompanyAccounts.RecordSale(commerce, 1000, 1000);
            LabelCompanyAccounts.ChangeCash(commerce, 1000, LabelCashKind.RecordReceipts);
            var commercialStatement = LabelCompanyAccounts.CloseMonth(commerce, 1965 * 12 + 5);
            Check(Near(commercialStatement.pretaxEarnings, 1000 - saleCost)
                && Near(commercialStatement.costOfSalesAndWriteOffs, 0)
                && Near(commercialStatement.distributionOperations + commercialStatement.tradeSupport + commercialStatement.recordExcise, saleCost)
                && Near(commercialStatement.cashReconciliationError, 0), "commercial bills enter P&L once, outside existing manufacturing, freight and royalties");
            LabelCorporateModel.SetForChecks(true);
            Check(LabelCorporateModel.PaySaleOperatingCosts(commerce, 1000, 1, june21) == 0,
                "observational ledger does not charge commercial services or excise");
            Check(LabelCorporateModel.DefaultBundleRequested(Array.Empty<string>())
                && !LabelCorporateModel.DefaultBundleRequested(new[] { "--no-label-corporate-model" })
                && !LabelCorporateModel.DefaultBundleRequested(new[] { "--label-company-ledger" })
                && !LabelCorporateModel.DefaultBundleRequested(new[] { "--no-plant-credit" }),
                "default corporate economy preserves explicit off, observational and legacy research arms");
            LabelCorporateModel.SetForChecks(true, capital: true, commercial: true);
            var committed = Firm(cash: 100000); LabelCompanyAccounts.Open(committed);
            committed.company.recentCommercialSpend = 50000; committed.productionQuality = .5f;
            committed.recentMonthlyCashFlows.Add(1000); committed.company.lastStatement = new LabelCompanyStatement { afterTaxEarnings = 1000 };
            Check(!LabelCorporateModel.TryProductionUpgrade(committed, 1964, 3) && committed.cashReserves == 100000,
                "capital cannot spend cash already reserved for recurring commercial bills");
            var retrenching = Firm(cash: 100000); LabelCompanyAccounts.Open(retrenching); retrenching.productionQuality = .5f;
            retrenching.recentMonthlyCashFlows.Add(-100); retrenching.company.lastStatement = new LabelCompanyStatement { afterTaxEarnings = 1000 };
            Check(!LabelCorporateModel.TryProductionUpgrade(retrenching, 1964, 3) && retrenching.cashReserves == 100000,
                "a cash-rich but cash-losing firm does not buy discretionary quality upgrades");
            LabelCorporateModel.SetForChecks(true, tax: true, commercial: true);
            var commitments = Firm(); LabelCompanyAccounts.Open(commitments);
            commitments.company.taxPayable = 600; commitments.company.recentCommercialSpend = 100;
            commitments.outstandingPlantPayables = 250;
            float protectedRelease = EconomicLabelLifecycle.CommitmentReserve(commitments, signing: false);
            float protectedSigning = EconomicLabelLifecycle.CommitmentReserve(commitments, signing: true);
            LabelCorporateModel.SetForChecks(true);
            Check(Near(protectedRelease - EconomicLabelLifecycle.CommitmentReserve(commitments, signing: false), 800)
                && Near(protectedSigning - EconomicLabelLifecycle.CommitmentReserve(commitments, signing: true), 800),
                "release and signing budgets protect current tax plus future service bills, without duplicating plant liabilities");
            GD.Print($"LABEL_COMPANY_CHECK passed checks={checks}");
        } finally { LabelCorporateModel.SetForChecks(saved.Item1, saved.Item2, saved.Item3, saved.Item4, saved.Item5, saved.Item6, saved.Item7, saved.Item8); }
    }
}
