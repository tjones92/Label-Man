using System;
using System.Collections.Generic;
using System.Linq;

public readonly record struct LabelCompanyCashEvent(AILabel Label, LabelCashKind Kind, double Amount, double Rounding, double CashAfter);

/// <summary>Sidecar accrual bridge and categorized cash journal. Existing advances/recording are expensed
/// when paid (conservative management-accounting policy); receivables, plant payables and inventory accrue.
/// This is an explicit simplified tax base, not a statutory set of financial statements.</summary>
public static class LabelCompanyAccounts {
    public static event Action<AILabel, LabelCompanyStatement> MonthClosed;
    public static event Action<LabelCompanyCashEvent> CashChanged;

    public static void Open(AILabel label) {
        var company = LabelCorporateModel.Ensure(label);
        if (company == null || company.accountsOpened) return;
        LabelCorporateModel.InitializeOpeningOrganization(label);
        company.accountsOpened = true;
        company.openingPeriod = LabelCorporateModel.Period(TimeManager.Instance?.CurrentDate ?? GameDate.StartDate);
        company.taxYear = LabelCorporateModel.Year(company.openingPeriod);
        company.openingCash = label.cashReserves;
        company.openingReceivables = label.outstandingWholesaleReceivables;
        company.openingPlantPayables = label.outstandingPlantPayables;
        company.openingInventory = company.inventoryValue;
        company.contributedCapital = company.openingCash + company.openingReceivables + company.inventoryValue - company.openingPlantPayables;
    }

    /// <summary>One float cash mutation, retaining legacy arithmetic when disabled; journal retains nominal
    /// dollars and the observed float rounding separately. No RNG, hidden balancing item or extra charge.</summary>
    public static void ChangeCash(AILabel label, float amount, LabelCashKind kind) {
        if (label == null) return;
        bool accounts = LabelCorporateModel.Applies(label);
        if (accounts) Open(label);
        float before = label.cashReserves;
        label.cashReserves += amount;
        if (!accounts) return;
        var company = label.company;
        company.cashMovements[kind] = company.cashMovements.GetValueOrDefault(kind) + amount;
        double rounding = (double)label.cashReserves - before - amount;
        company.cashRounding += rounding;
        // A parent may pass received subsidiary dividends on to its outside owners. They are
        // excluded from operating/taxable income below to avoid taxing the same earnings twice.
        if (kind == LabelCashKind.OwnerDistribution && amount > 0) company.undistributedEarnings += amount;
        CashChanged?.Invoke(new LabelCompanyCashEvent(label, kind, amount, rounding, label.cashReserves));
    }

    public static void OverheadPaid(AILabel label, float amount) {
        ChangeCash(label, -amount, LabelCashKind.LegacyOverhead);
        var company = label.company;
        if (!LabelCorporateModel.Applies(label) || (!LabelCorporateModel.Staff && !LabelCorporateModel.Premises)) return;
        double payroll = LabelCorporateModel.Staff ? company.payrollQuote : label.GetLegacyMonthlyOverhead() * .7;
        double founderPay = LabelCorporateModel.Staff ? company.founderPayrollQuote : 0;
        double retainers = LabelCorporateModel.Staff ? company.retainerQuote : 0;
        double premises = LabelCorporateModel.Premises ? company.premisesQuote : label.GetLegacyMonthlyOverhead() * .3;
        double total = payroll + retainers + premises;
        company.cashMovements[LabelCashKind.LegacyOverhead] += amount;
        void Allocate(LabelCashKind kind, double fraction) => company.cashMovements[kind] = company.cashMovements.GetValueOrDefault(kind) - amount * fraction;
        Allocate(LabelCashKind.Payroll, (payroll - founderPay) / total);
        Allocate(LabelCashKind.OwnerSalary, founderPay / total);
        Allocate(LabelCashKind.Retainers, retainers / total);
        Allocate(LabelCashKind.Premises, premises / total);
    }

    public static void RecordSale(AILabel label, double gross, double units) {
        if (!LabelCorporateModel.Applies(label)) return;
        Open(label);
        label.company.earnedRecordGross += gross;
        label.company.recordUnits += units;
    }

    public static void PayProject(AILabel label, float total, float production) {
        ChangeCash(label, -total, LabelCashKind.Recording);
        if (!LabelCorporateModel.Applies(label)) return;
        double campaign = Math.Max(0, total - production);
        label.company.cashMovements[LabelCashKind.Recording] += campaign;
        label.company.cashMovements[LabelCashKind.Campaign] = label.company.cashMovements.GetValueOrDefault(LabelCashKind.Campaign) - campaign;
    }

    public static void Inventory(AILabel label, RecordRuntimeData record, int initial, int repressed, int sold, double unitCost) {
        if (!LabelCorporateModel.Applies(label) || record?.baseRecord == null) return;
        Open(label);
        var company = label.company;
        string id = record.baseRecord.recordId;
        if (!company.inventory.TryGetValue(id, out var inventory)) {
            int openingCopies = Math.Max(0, record.unitsPressed - initial - repressed - (record.pressingBookSold - sold));
            inventory = new LabelInventoryAccount { copies = openingCopies, value = openingCopies * unitCost };
            company.inventory[id] = inventory;
            // Catalog inventory existed before the observation began. It is contributed opening capital,
            // not new earned profit or cash. Setup/freight are never capitalized as copy inventory here.
            company.inventoryValue += inventory.value;
            company.openingInventory += inventory.value;
            company.contributedCapital += inventory.value;
        }
        double before = inventory.value;
        inventory.copies += initial + repressed;
        inventory.value += (initial + repressed) * unitCost;
        double average = inventory.copies > 0 ? inventory.value / inventory.copies : 0;
        int fulfilled = Math.Min(Math.Max(0, sold), inventory.copies);
        inventory.copies -= fulfilled;
        inventory.value = inventory.copies == 0 ? 0 : Math.Max(0, inventory.value - average * fulfilled);
        company.inventoryValue += inventory.value - before;
    }

    public static void RetireInventory(AILabel label, string recordId) {
        if (!LabelCorporateModel.Applies(label)) return;
        var company = LabelCorporateModel.Ensure(label);
        if (!company.inventory.Remove(recordId, out var inventory)) return;
        company.inventoryWriteOffs += inventory.value;
        company.inventoryValue = Math.Max(0, company.inventoryValue - inventory.value);
    }

    public static void ForgiveFinancing(AILabel borrower, AILabel lender, double amount) {
        if (LabelCorporateModel.Applies(borrower)) { Open(borrower); borrower.company.financingEarningsAdjustment += amount; }
        if (LabelCorporateModel.Applies(lender)) { Open(lender); lender.company.financingEarningsAdjustment -= amount; }
    }

    public static LabelCompanyStatement CloseMonth(AILabel label, int period) {
        if (!LabelCorporateModel.Applies(label)) return null;
        Open(label);
        var company = label.company;
        if (company.lastClosedPeriod >= period) return company.lastStatement;
        if (period < company.openingPeriod) return null;
        double Cash(LabelCashKind kind) => company.cashMovements.GetValueOrDefault(kind);
        double Out(LabelCashKind kind) => -Cash(kind);
        double depreciation = 0;
        foreach (var asset in company.assets) {
            double charge = Math.Min(asset.bookValue, asset.cost / Math.Max(1, asset.lifeMonths));
            asset.bookValue -= charge;
            depreciation += charge;
        }
        company.assets.RemoveAll(a => a.bookValue < .001);
        double arChange = label.outstandingWholesaleReceivables - company.openingReceivables;
        double inventoryChange = company.inventoryValue - company.openingInventory;
        double apChange = label.outstandingPlantPayables - company.openingPlantPayables;
        double capitalSpending = Out(LabelCashKind.DistributionInvestment) + Out(LabelCashKind.StudioInvestment);
        double pretax = company.cashMovements.Values.Sum() - Cash(LabelCashKind.Financing) - Cash(LabelCashKind.OwnerDistribution)
            + capitalSpending + Out(LabelCashKind.TaxPayment) + arChange + inventoryChange - apChange - depreciation + company.financingEarningsAdjustment;
        double provision = LabelCompanyTax.Accrue(label, period, pretax);
        LabelCompanyTax.PayQuarter(label, period);
        company.undistributedEarnings += pretax - provision;
        var statement = new LabelCompanyStatement {
            period = period, labelId = label.labelId, tier = label.tier, active = label.IsActive,
            openingCash = company.openingCash, closingCash = label.cashReserves,
            cashRounding = company.cashRounding,
            cashReconciliationError = label.cashReserves - company.openingCash - company.cashMovements.Values.Sum() - company.cashRounding,
            recordGross = company.earnedRecordGross, recordUnits = company.recordUnits,
            costOfSalesAndWriteOffs = company.earnedRecordGross - Cash(LabelCashKind.RecordReceipts) - arChange
                + Out(LabelCashKind.PlantPayments) - inventoryChange + apChange,
            serviceIncome = Cash(LabelCashKind.DistributionFees) + Cash(LabelCashKind.PublishingReceipts),
            recordReceipts = Cash(LabelCashKind.RecordReceipts), plantPayments = Out(LabelCashKind.PlantPayments),
            recording = Out(LabelCashKind.Recording), campaigns = Out(LabelCashKind.Campaign),
            artistAdvances = Out(LabelCashKind.ArtistAdvances), licensing = Out(LabelCashKind.Licensing),
            otherOperating = Out(LabelCashKind.OtherOperating), payroll = Out(LabelCashKind.Payroll),
            premises = Out(LabelCashKind.Premises), retainers = Out(LabelCashKind.Retainers),
            restructuring = Out(LabelCashKind.Restructuring), legacyOverhead = Out(LabelCashKind.LegacyOverhead), ownerSalary = Out(LabelCashKind.OwnerSalary),
            receivablesChange = arChange, inventoryChange = inventoryChange, plantPayablesChange = apChange,
            inventoryWriteOffs = company.inventoryWriteOffs, depreciation = depreciation,
            pretaxEarnings = pretax, taxProvision = provision, taxPaid = Out(LabelCashKind.TaxPayment), afterTaxEarnings = pretax - provision,
            financingNet = Cash(LabelCashKind.Financing), ownerTransfersNet = Cash(LabelCashKind.OwnerDistribution),
            capitalSpending = capitalSpending, capitalBookValue = company.assets.Sum(a => a.bookValue),
            taxPayable = company.taxPayable, inventoryValue = company.inventoryValue, undistributedEarnings = company.undistributedEarnings,
            employees = company.departments.Sum(d => d.employees), requiredEmployees = company.departments.Sum(d => d.requiredEmployees),
            organizationCoverage = company.organizationCoverage, leasedOfficeSuites = company.leasedOfficeSuites,
            leasedBranchOffices = company.leasedBranchOffices, financingEarningsAdjustment = company.financingEarningsAdjustment,
            distributionOperations = Out(LabelCashKind.DistributionOperations), tradeSupport = Out(LabelCashKind.TradeSupport),
            recordExcise = Out(LabelCashKind.RecordExcise), founderEquivalent = company.departments.Sum(d => d.founderEquivalent)
        };
        company.recentProjectSpend = (company.recentProjectSpend * 2 + statement.recording + statement.campaigns + statement.artistAdvances + statement.licensing) / 3;
        company.recentPlantSpend = (company.recentPlantSpend * 2 + statement.plantPayments) / 3;
        company.recentCommercialSpend = (company.recentCommercialSpend * 2 + statement.distributionOperations
            + statement.tradeSupport + statement.recordExcise) / 3;
        company.recentRevenue = (company.recentRevenue * 2 + statement.recordGross + statement.serviceIncome) / 3;
        company.lastClosedPeriod = period;
        statement.protectedCash = LabelCorporateModel.ProtectedCash(label);
        if (Math.Abs(statement.cashReconciliationError) > .01)
            throw new InvalidOperationException($"Company cash journal does not reconcile: {label.labelId}, period {period}, residual {statement.cashReconciliationError}");
        company.lastStatement = statement;
        MonthClosed?.Invoke(label, statement);
        company.openingPeriod = period + 1;
        company.openingCash = label.cashReserves;
        company.openingReceivables = label.outstandingWholesaleReceivables;
        company.openingInventory = company.inventoryValue;
        company.openingPlantPayables = label.outstandingPlantPayables;
        company.cashMovements.Clear();
        company.cashRounding = company.inventoryWriteOffs = company.earnedRecordGross = company.recordUnits = 0;
        company.financingEarningsAdjustment = 0;
        return statement;
    }
}
