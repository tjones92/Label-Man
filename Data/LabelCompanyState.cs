using System.Collections.Generic;

public enum LabelOwnershipForm { OwnerOperated, PrivateCorporation, PublicCorporation, CorporateDivision }
public enum LabelDepartment { Executive, Clerical, FinanceRoyalties, BusinessAffairs, ArtistRepertoire, Production, PromotionPublicity, SalesDistribution, International }
public enum LabelCashKind { RecordReceipts, DistributionFees, PublishingReceipts, PlantPayments, Recording, Campaign,
    ArtistAdvances, Licensing, OtherOperating, LegacyOverhead, Payroll, Premises, Restructuring, OwnerSalary,
    DistributionInvestment, StudioInvestment, TaxPayment, OwnerDistribution, Financing, Retainers,
    DistributionOperations, TradeSupport, RecordExcise, ParentCapital }

/// <summary>Persisted aggregate staff. Equivalents include part-time employment; retainers are separate.</summary>
public sealed class LabelDepartmentState {
    public LabelDepartment department;
    public double employees, requiredEmployees, founderEquivalent;
    public int excessReviews;
}

public sealed class LabelCapitalAsset {
    public string purpose;
    public double cost, bookValue;
    public int lifeMonths;
}

public sealed class LabelInventoryAccount {
    public int copies;
    public double value;
}

public sealed class LabelTaxLoss {
    public int expiresAfterYear;
    public double amount;
}

/// <summary>Separate company accounts; legacy cash-surplus decision fields retain their meaning.
/// All amounts are nominal period dollars. No opening cash is assumed to be distributable profit.</summary>
public sealed class LabelCompanyState {
    public int version = 1;
    public LabelOwnershipForm ownership;
    public List<LabelDepartmentState> departments = new();
    public List<LabelCapitalAsset> assets = new();
    public Dictionary<string, LabelInventoryAccount> inventory = new();
    public List<LabelTaxLoss> losses = new();
    public bool accountsOpened;
    public int openingPeriod, lastClosedPeriod, lastDistributionQuarter, lastProductionUpgradeQuarter;
    public int lastOrganizationReviewPeriod;
    public int leasedOfficeSuites, leasedBranchOffices, nextLeaseReviewPeriod;
    public double openingCash, openingReceivables, openingInventory, openingPlantPayables;
    public double inventoryValue, undistributedEarnings, contributedCapital;
    public Dictionary<LabelCashKind, double> cashMovements = new();
    public double cashRounding, inventoryWriteOffs, earnedRecordGross, serviceIncome, recordUnits;
    public double financingEarningsAdjustment;
    public double payrollQuote, founderPayrollQuote, premisesQuote, retainerQuote, organizationCoverage = 1;
    public double recentProjectSpend, recentPlantSpend, recentRevenue, recentCommercialSpend;
    public int taxYear;
    public double yearPretaxEarnings, yearTaxProvision, yearTaxPaid, taxPayable;
    // --prior-year-corporate-tax: last year's liability still to pay next March/June, its first
    // installment, and this year's current installments on liability over the threshold.
    public double priorYearTaxDue, priorYearTaxInstallment, currentYearTaxInstallments;
    public double lifetimeTaxPaid, lifetimeDistributions, lifetimeCapitalSpend;
    public LabelCompanyStatement lastStatement;
}

public sealed class LabelCompanyStatement {
    public int period;
    public string labelId;
    public LabelTier tier;
    public bool active;
    public double openingCash, closingCash, cashRounding, cashReconciliationError;
    public double recordGross, serviceIncome, recordUnits;
    public double costOfSalesAndWriteOffs;
    public double recordReceipts, plantPayments, recording, campaigns, artistAdvances, licensing, otherOperating;
    public double payroll, premises, retainers, restructuring, legacyOverhead, ownerSalary;
    public double receivablesChange, inventoryChange, plantPayablesChange, inventoryWriteOffs;
    public double depreciation, pretaxEarnings, taxProvision, taxPaid, afterTaxEarnings;
    public double financingNet, ownerTransfersNet, capitalSpending, capitalBookValue;
    public double taxPayable, inventoryValue, undistributedEarnings, protectedCash;
    public double distributionOperations, tradeSupport, recordExcise;
    public double employees, requiredEmployees, organizationCoverage;
    public int leasedOfficeSuites, leasedBranchOffices;
    public double financingEarningsAdjustment;
    public double founderEquivalent;
}
