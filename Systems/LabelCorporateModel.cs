using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>AI company organization and accounts. Calibrated scenario coefficients are explicitly
/// documented scenario assumptions in SimTools/LabelCorporateModelImplementation.md.</summary>
public static class LabelCorporateModel {
    public static bool Ledger { get; private set; }
    public static bool Staff { get; private set; }
    public static bool Premises { get; private set; }
    public static bool Tax { get; private set; }
    public static bool Distributions { get; private set; }
    public static bool Capital { get; private set; }
    public static bool Commercial { get; private set; }
    public static bool RecordExcise { get; private set; }
    public static bool ChangesEconomy => Staff || Premises || Tax || Distributions || Capital || Commercial || RecordExcise;
    public static double PayrollScale { get; private set; } = 1;
    /// <summary>Opt-in: a founder-run Small, Boutique or Independent firm with no paid staff works from the
    /// founder's home, shop or car, as period micro-labels did. It leases no suite and keeps no standing
    /// legal or accounting retainer (counsel was paid per deal). Hiring anyone leases an office as before.</summary>
    public const string HomeOperatedSwitch = "--home-operated-micro-labels";
    public static bool HomeOperated { get; private set; }
    public static float HomeRunningCost { get; private set; }
    private static bool configured;
    // Individual research arms select only their requested mechanisms. Explicit legacy
    // pricing/credit/market replays also retain their old economy unless full is requested.
    internal static bool DefaultBundleRequested(IEnumerable<string> arguments) {
        var args = (arguments ?? Array.Empty<string>()).ToArray();
        if (args.Contains("--no-label-corporate-model")) return false;
        return !args.Any(a => a is "--label-company-ledger" or "--label-staff-costs" or "--label-premises-costs"
            or "--label-income-tax" or "--label-distributions" or "--label-capital-investment" or "--label-commercial-costs"
            or "--label-record-excise" or "--no-dealer-price-pressing" or "--no-plant-credit"
            or "--disable-genre-market-v2" or "--disable-artist-population-lifecycle");
    }
    internal static bool FullBundleRequested(IEnumerable<string> arguments) {
        var args = (arguments ?? Array.Empty<string>()).ToArray();
        return args.Contains("--label-corporate-model") || DefaultBundleRequested(args);
    }
    public static void Configure(IEnumerable<string> arguments) {
        if (configured) return;
        var args = (arguments ?? Array.Empty<string>()).ToArray();
        if (args.Contains("--label-corporate-model") && args.Contains("--no-label-corporate-model"))
            throw new ArgumentException("Conflicting corporate-model switches.");
        bool all = args.Contains("--label-corporate-model") || DefaultBundleRequested(args);
        Staff = all || args.Contains("--label-staff-costs");
        Premises = all || args.Contains("--label-premises-costs");
        Tax = all || args.Contains("--label-income-tax");
        Distributions = all || args.Contains("--label-distributions");
        Capital = all || args.Contains("--label-capital-investment");
        Commercial = (all && !args.Contains("--no-label-commercial-costs")) || args.Contains("--label-commercial-costs");
        RecordExcise = (all && !args.Contains("--no-label-record-excise")) || args.Contains("--label-record-excise");
        Ledger = ChangesEconomy || args.Contains("--label-company-ledger");
        PayrollScale = LabelPressingEconomics.ReadParameter(args, "--label-payroll-scale=", 1f, .1f, 3f);
        HomeOperated = args.Contains(HomeOperatedSwitch);
        HomeRunningCost = LabelPressingEconomics.ReadParameter(args, "--home-label-running-cost=", 0f, 0f, 100f);
        if (HomeRunningCost > 0f && !HomeOperated)
            throw new ArgumentException("--home-label-running-cost requires " + HomeOperatedSwitch);
        if (HomeOperated && (!Staff || !Premises))
            throw new ArgumentException(HomeOperatedSwitch + " requires the corporate staff and premises costs.");
        if (Ledger && (!LabelPressingEconomics.Enabled || !LabelPressingEconomics.PlantCredit))
            throw new ArgumentException("Company accounts require dealer-price pressing and plant credit.");
        if (Ledger && LabelOperatorPay.MonthlyLimit > 0)
            throw new ArgumentException("Company accounts replace the experimental owner-pay policy; omit --label-operator-monthly-pay.");
        if (Ledger && !DistributionAdvanceRecoupment.Enabled)
            throw new ArgumentException("Company accounts require --distribution-advance-recoupment to distinguish loan recovery from service fees.");
        configured = true;
    }
    internal static void SetForChecks(bool ledger, bool staff = false, bool premises = false,
        bool tax = false, bool distributions = false, bool capital = false, bool commercial = false, bool recordExcise = false) {
        Ledger = ledger; Staff = staff; Premises = premises; Tax = tax;
        Distributions = distributions; Capital = capital;
        Commercial = commercial; RecordExcise = recordExcise;
    }

    // Owned distribution buys warehouse/order/field-selling services in addition to office staff.
    // 85% of the existing 25% channel tariff and 5% owned-channel trade support are scenario costs.
    public static double FulfillmentCost(double dealerGross, double selfDistributedShare) =>
        Math.Max(0, dealerGross) * Math.Clamp(selfDistributedShare, 0, 1) * .25 * .85;
    public static double TradeSupportCost(double dealerGross, double selfDistributedShare) =>
        Math.Max(0, dealerGross) * Math.Clamp(selfDistributedShare, 0, 1) * .05;
    // Federal manufacturers' tax ended June 22, 1965. 75% of dealer receipts is the
    // disclosed factory-price proxy; neither corporate form nor format exempts a sale.
    public static double RecordExciseCost(double dealerGross, GameDate date) =>
        date < new GameDate(1965, 6, 22) ? Math.Max(0, dealerGross) * .75 * .10 : 0;

    public static float SaleOperatingCost(AILabel label, double gross, double selfShare, GameDate date) {
        if (!Applies(label)) return 0;
        return (float)((Commercial ? FulfillmentCost(gross, selfShare) + TradeSupportCost(gross, selfShare) : 0)
            + (RecordExcise ? RecordExciseCost(gross, date) : 0));
    }

    public static float PaySaleOperatingCosts(AILabel label, double gross, double selfShare, GameDate date) {
        if (!Applies(label)) return 0;
        float total = 0;
        void Pay(double cost, LabelCashKind kind) {
            float amount = (float)cost;
            if (amount <= 0) return;
            LabelCompanyAccounts.ChangeCash(label, -amount, kind);
            total += amount;
        }
        if (Commercial) {
            Pay(FulfillmentCost(gross, selfShare), LabelCashKind.DistributionOperations);
            Pay(TradeSupportCost(gross, selfShare), LabelCashKind.TradeSupport);
        }
        if (RecordExcise) Pay(RecordExciseCost(gross, date), LabelCashKind.RecordExcise);
        return total;
    }
    public static bool Applies(AILabel label) => Ledger && label != null && !label.isPlayerOwned;
    public static int Period(GameDate date) => date.year * 12 + date.month - 1;
    public static int Year(int period) => period / 12;
    public static int Month(int period) => period % 12 + 1;
    public static double WageIndex(int year) => Math.Pow(1.03, Math.Clamp(year - 1960, 0, 14));

    public static LabelCompanyState Ensure(AILabel label) {
        if (!Applies(label)) return null;
        if (label.company != null) return label.company;
        var company = label.company = new LabelCompanyState {
            ownership = label.IsSubsidiary ? LabelOwnershipForm.CorporateDivision : label.tier == LabelTier.Small ? LabelOwnershipForm.OwnerOperated
                : label.tier == LabelTier.Major ? (label.archetype == LabelArchetype.CorporateGiant
                    ? LabelOwnershipForm.CorporateDivision : LabelOwnershipForm.PublicCorporation)
                : LabelOwnershipForm.PrivateCorporation
        };
        SetRequiredStaff(label, company, 0, 0);
        foreach (var department in company.departments) department.employees = department.requiredEmployees;
        Quote(label, company, TimeManager.Instance?.CurrentDate.year ?? 1960);
        return company;
    }

    internal static void InitializeOpeningOrganization(AILabel label) {
        var company = Ensure(label);
        SetRequiredStaff(label, company, 0, 0);
        foreach (var department in company.departments) department.employees = department.requiredEmployees;
        Quote(label, company, TimeManager.Instance?.CurrentDate.year ?? 1960);
    }

    // BLS occupational proxies plus disclosed specialist assumptions, monthly 1960 dollars.
    public static double Wage(LabelDepartment department) => department switch {
        LabelDepartment.Executive => 1250, LabelDepartment.Clerical => 325,
        LabelDepartment.FinanceRoyalties => 600, LabelDepartment.BusinessAffairs => 1000,
        LabelDepartment.ArtistRepertoire => 800, LabelDepartment.Production => 550,
        LabelDepartment.PromotionPublicity => 650, LabelDepartment.SalesDistribution => 500,
        LabelDepartment.International => 750, _ => throw new ArgumentOutOfRangeException(nameof(department))
    };

    private static void SetRequiredStaff(AILabel label, LabelCompanyState company, double projectsPerMonth, int clients) {
        double acts = Math.Max(0, label.CurrentRosterSize);
        double projects = Math.Max(projectsPerMonth, Math.Min(acts / 3, label.releasesPerMonth));
        // Founder supplies one management/A&R/promotional equivalent across functions, not one per function.
        bool founder = company.ownership is LabelOwnershipForm.OwnerOperated or LabelOwnershipForm.PrivateCorporation;
        bool smallOrganization = founder && label.tier is LabelTier.Small or LabelTier.Boutique or LabelTier.Independent;
        double scale = label.tier switch { LabelTier.Major => 4, LabelTier.MidTier => 1.8,
            LabelTier.Independent => .65, LabelTier.Boutique => .4, _ => .12 };
        double direct = Math.Clamp(label.ownedReach, 0, 1);
        double regions = Math.Max(1, label.distributionRegions?.Length ?? 1);
        var targets = new Dictionary<LabelDepartment, double> {
            [LabelDepartment.Executive] = Math.Max(.25, scale + acts / 40),
            [LabelDepartment.Clerical] = Math.Max(.1, scale * 1.5 + acts / 12 + clients / 4.0),
            [LabelDepartment.FinanceRoyalties] = scale * .7 + acts / 16 + clients / 3.0,
            [LabelDepartment.BusinessAffairs] = Math.Max(0, scale * .4 + acts / 40 - .2),
            [LabelDepartment.ArtistRepertoire] = acts / 6 + projects / 2,
            [LabelDepartment.Production] = projects / 2 + scale * .4,
            [LabelDepartment.PromotionPublicity] = projects * .8 + scale,
            [LabelDepartment.SalesDistribution] = direct * (regions * .3 + scale * 2) + clients * .6,
            [LabelDepartment.International] = label.tier is LabelTier.Major or LabelTier.MidTier ? scale * .5 : 0
        };
        if (smallOrganization) {
            // Micro-labels begin with work, not an imaginary paid team. One founder's 160 hours is
            // shared across functions; staffing buys only work exceeding that single time budget.
            targets[LabelDepartment.Executive] = .12;
            targets[LabelDepartment.Clerical] = .025 + acts / 80 + projects / 20;
            targets[LabelDepartment.FinanceRoyalties] = .025 + acts / 60 + projects / 30;
            targets[LabelDepartment.BusinessAffairs] = 0; // external legal retainer
            targets[LabelDepartment.ArtistRepertoire] = acts / 24 + projects / 10;
            targets[LabelDepartment.Production] = projects / 8;
            targets[LabelDepartment.PromotionPublicity] = projects / 5;
            targets[LabelDepartment.SalesDistribution] = direct * (regions * .08 + projects * .1) + clients * .15;
            targets[LabelDepartment.International] = 0;
        }
        double founderTime = founder ? 1 : 0;
        // Work sharing/part-time contracts in quarter-role increments; no per-firm random draw.
        foreach (var entry in targets) {
            var department = company.departments.FirstOrDefault(d => d.department == entry.Key);
            if (department == null) company.departments.Add(department = new LabelDepartmentState { department = entry.Key });
            department.founderEquivalent = Math.Min(founderTime, entry.Value);
            founderTime -= department.founderEquivalent;
            department.requiredEmployees = Math.Ceiling(Math.Max(0, entry.Value - department.founderEquivalent) * 4) / 4;
        }
    }

    private static void Quote(AILabel label, LabelCompanyState company, int year, int period = 0) {
        double employees = company.departments.Sum(d => d.employees);
        double founder = company.departments.Sum(d => d.founderEquivalent);
        double index = WageIndex(year);
        // 12% employment burden is a scenario allowance for employer contributions/benefits, not a tax rate.
        // An incorporated working owner is paid once for the same shared hours; a sole
        // operator instead draws after-tax business earnings through the owner pool.
        company.founderPayrollQuote = company.ownership == LabelOwnershipForm.PrivateCorporation
            ? company.departments.Sum(d => d.founderEquivalent * Wage(d.department)) * 1.12 * index * PayrollScale : 0;
        company.payrollQuote = company.departments.Sum(d => d.employees * Wage(d.department)) * 1.12 * index * PayrollScale
            + company.founderPayrollQuote;
        bool home = HomeOperated && IsFounderRun(label, company);
        company.retainerQuote = home ? 0 : (company.departments.First(d => d.department == LabelDepartment.BusinessAffairs).employees < 1 ? 35 : 0)
            + (company.departments.First(d => d.department == LabelDepartment.FinanceRoyalties).employees < 1 ? 20 : 0);
        company.retainerQuote *= index;
        // Office commitments are leased in suites; founder room uses the existing city cost ladder. A home-run
        // firm leases none, but a suite already leased is paid until its annual review.
        double suites = home ? 0 : Math.Max(1, Math.Ceiling((employees + founder) / 5));
        double room = CityProfiles.MonthlyOverhead(label.homeCityId);
        double branchOffices = label.tier == LabelTier.Major ? Math.Ceiling(Math.Clamp(label.ownedReach, 0, 1) * 4)
            : label.tier == LabelTier.MidTier && label.ownedReach > .6 ? 1 : 0;
        if (period == 0) period = LabelCorporateModel.Period(TimeManager.Instance?.CurrentDate ?? GameDate.StartDate);
        if (company.leasedOfficeSuites == 0 || period >= company.nextLeaseReviewPeriod) {
            company.leasedOfficeSuites = (int)suites;
            company.leasedBranchOffices = (int)branchOffices;
            company.nextLeaseReviewPeriod = period + 12;
        } else {
            company.leasedOfficeSuites = Math.Max(company.leasedOfficeSuites, (int)suites);
            company.leasedBranchOffices = Math.Max(company.leasedBranchOffices, (int)branchOffices);
        }
        double paidEquivalents = home ? 0 : employees + (company.ownership == LabelOwnershipForm.PrivateCorporation ? founder : 0);
        company.premisesQuote = (room * company.leasedOfficeSuites + company.leasedBranchOffices * room * 1.5 + 12 * paidEquivalents) * index
            + company.assets.Sum(a => a.bookValue > 0 ? a.cost * .0025 : 0) + (home ? HomeRunningCost * index : 0);
        double required = company.departments.Sum(d => d.requiredEmployees) + founder;
        company.organizationCoverage = required <= 0 ? 1 : Math.Clamp((employees + founder) / required, .25, 1);
    }

    internal static bool IsFounderRun(AILabel label, LabelCompanyState company) => label != null && company != null
        && !label.isPlayerOwned && !label.IsSubsidiary
        && company.departments.Sum(d => d.employees) <= 0
        && company.ownership is (LabelOwnershipForm.OwnerOperated or LabelOwnershipForm.PrivateCorporation)
        && label.tier is (LabelTier.Small or LabelTier.Boutique or LabelTier.Independent);

    internal static void SetHomeForChecks(bool home, float cost) { HomeOperated = home; HomeRunningCost = cost; }

    public static float MonthlyOverhead(AILabel label, float legacy) {
        var company = Ensure(label);
        if (company == null || (!Staff && !Premises)) return legacy;
        // Partial research arms replace the corresponding legacy allocation; combined arm replaces all.
        double payroll = FounderCompensation.Applies(label, company)
            ? company.payrollQuote - company.founderPayrollQuote : company.payrollQuote;
        return (float)((Staff ? payroll + company.retainerQuote : legacy * .7)
            + (Premises ? company.premisesQuote : legacy * .3));
    }

    public static float CapacityFactor(AILabel label) => Staff && Applies(label)
        ? (float)Ensure(label).organizationCoverage : 1f;

    public static void ReviewOrganization(AILabel label, int period, int clients, double projectsPerMonth) {
        var company = Ensure(label);
        if (company == null || company.lastOrganizationReviewPeriod >= period) return;
        company.lastOrganizationReviewPeriod = period;
        SetRequiredStaff(label, company, projectsPerMonth, clients);
        if (Staff && (Month(period) % 3 == 0)) {
            foreach (var department in company.departments) {
                double difference = department.requiredEmployees - department.employees;
                if (difference > 0) {
                    department.excessReviews = 0;
                    if (label.cashReserves > ProtectedCash(label) + company.payrollQuote * 3)
                        department.employees += Math.Min(difference, Math.Max(.25, department.employees * .25));
                } else if (difference < -.24) {
                    // Two quarterly excess-capacity reviews before contraction; one month's severance.
                    if (++department.excessReviews >= 2) {
                        double removed = Math.Min(-difference, Math.Max(.25, department.employees * .25));
                        LabelCompanyAccounts.ChangeCash(label, (float)(-removed * Wage(department.department) * WageIndex(Year(period))), LabelCashKind.Restructuring);
                        department.employees -= removed;
                    }
                } else department.excessReviews = 0;
            }
        }
        Quote(label, company, Year(period + 1), period + 1);
    }

    public static double ProtectedCash(AILabel label) {
        var company = Ensure(label);
        if (company == null) return 0;
        // Future operating/release needs only. Plant liabilities are added once; AR/inventory are not subtracted.
        double overhead = label.GetMonthlyOverhead();
        double projects = Math.Max(company.recentProjectSpend, label.releasesPerMonth * label.GetProductionCost());
        return 3 * overhead + 2 * projects + Math.Max(0, company.recentPlantSpend)
            + Math.Max(0, label.outstandingPlantPayables) + TaxAndServiceReserve(label)
            + Math.Max(0, -label.RecentMonthlyCashFlow) * 2;
    }

    // Shared by project/signing commitments and the larger investment/payout buffer.
    // This reserves cash without charging any expense a second time. Plant AP remains
    // at the caller's existing boundary; it is deliberately not included here.
    public static double TaxAndServiceReserve(AILabel label) {
        if (!ChangesEconomy || !Applies(label)) return 0;
        var company = Ensure(label);
        return Math.Max(0, company.taxPayable) + UnprovidedTax(label, company)
            + 2 * Math.Max(0, company.recentCommercialSpend);
    }

    private static double UnprovidedTax(AILabel label, LabelCompanyState company) {
        if (!Tax || company.ownership == LabelOwnershipForm.OwnerOperated || company.lastClosedPeriod >= company.openingPeriod) return 0;
        double Cash(LabelCashKind kind) => company.cashMovements.GetValueOrDefault(kind);
        double current = company.cashMovements.Values.Sum() - Cash(LabelCashKind.Financing) - Cash(LabelCashKind.OwnerDistribution)
            - Cash(LabelCashKind.DistributionInvestment) - Cash(LabelCashKind.StudioInvestment) - Cash(LabelCashKind.TaxPayment)
            + label.outstandingWholesaleReceivables - company.openingReceivables + company.inventoryValue - company.openingInventory
            - label.outstandingPlantPayables + company.openingPlantPayables;
        int year = Year(company.openingPeriod);
        double prior = company.taxYear == year ? company.yearPretaxEarnings : 0;
        double provision = company.taxYear == year ? company.yearTaxProvision : 0;
        double losses = company.losses.Where(loss => loss.expiresAfterYear >= year).Sum(loss => loss.amount);
        return Math.Max(0, LabelCompanyTax.Liability(year, prior + current - losses) - provision);
    }

    public static bool TryProductionUpgrade(AILabel label, int year, int month) {
        if (!Capital || !Applies(label) || year <= 1963 || !label.IsActive) return false;
        var company = Ensure(label);
        int quarter = year * 4 + (month - 1) / 3;
        if (company.lastProductionUpgradeQuarter >= quarter || label.productionQuality >= .95f) return false;
        company.lastProductionUpgradeQuarter = quarter;
        double cost = (label.tier switch { LabelTier.Major => 100000, LabelTier.MidTier => 25000,
            LabelTier.Independent => 6000, LabelTier.Boutique => 4000, _ => 1500 }) * WageIndex(year);
        // A discretionary quality increment is not a rescue plan. Fund at most one year's
        // recent operating cash surplus, and require a profitable latest statement. This
        // prevents repeated quarterly upgrades from spending a retrenching firm's reserves.
        if (label.RecentMonthlyCashFlow <= 0 || company.lastStatement?.afterTaxEarnings <= 0
            || company.lastStatement == null || cost > label.RecentMonthlyCashFlow * 12) return false;
        // Small firms buy equipment/access improvements, not a compulsory factory or whole studio.
        if (label.cashReserves - ProtectedCash(label) < cost) return false;
        LabelCompanyAccounts.ChangeCash(label, -(float)cost, LabelCashKind.StudioInvestment);
        AddAsset(label, "production equipment and access", cost, 84);
        label.productionQuality = Math.Min(.95f, label.productionQuality + .01f);
        return true;
    }
    public static void AddAsset(AILabel label, string purpose, double cost, int lifeMonths) {
        var company = Ensure(label);
        if (company == null || cost <= 0) return;
        company.assets.Add(new LabelCapitalAsset { purpose = purpose, cost = cost, bookValue = cost, lifeMonths = lifeMonths });
        company.lifetimeCapitalSpend += cost;
    }

    private sealed record DistributionPlan(AILabel Label, AILabel Owner, float Amount);
    public static void PayAllDistributions(IEnumerable<AILabel> labels, int closedPeriod, Func<string, AILabel> findOwner) {
        // Snapshot all funding decisions before any recipient receives cash. A parent cannot
        // spend this quarter's incoming dividend merely because its child precedes it in a list.
        var plans = labels.Select(label => PlanDistribution(label, closedPeriod, findOwner)).Where(plan => plan != null).ToArray();
        foreach (var plan in plans) ExecuteDistribution(plan);
    }
    public static void PayDistribution(AILabel label, int closedPeriod, Func<string, AILabel> findOwner) {
        var plan = PlanDistribution(label, closedPeriod, findOwner);
        if (plan != null) ExecuteDistribution(plan);
    }
    private static DistributionPlan PlanDistribution(AILabel label, int closedPeriod, Func<string, AILabel> findOwner) {
        if (!Distributions || !Applies(label) || !label.IsActive || Month(closedPeriod) % 3 != 0) return null;
        var company = Ensure(label);
        int quarter = Year(closedPeriod) * 4 + (Month(closedPeriod) - 1) / 3;
        if (company.lastDistributionQuarter >= quarter) return null;
        company.lastDistributionQuarter = quarter;
        if (label.RecentMonthlyCashFlow <= 0 || company.lastStatement == null || company.lastStatement.afterTaxEarnings <= 0) return null;
        AILabel owner = null;
        if (!string.IsNullOrEmpty(label.ownerLabelId)) {
            owner = findOwner?.Invoke(label.ownerLabelId);
            // Missing/cyclic ownership must not silently become an external sink.
            if (owner == null || owner == label || !owner.IsActive || owner.isPlayerOwned) return null;
            var visited = new HashSet<string> { label.labelId };
            var cursor = owner;
            while (cursor != null) {
                if (!visited.Add(cursor.labelId)) return null;
                string next = cursor.ownerLabelId;
                cursor = string.IsNullOrEmpty(next) ? null : findOwner(next);
            }
        }
        double share = company.ownership switch { LabelOwnershipForm.OwnerOperated => .5,
            LabelOwnershipForm.PrivateCorporation => .2, LabelOwnershipForm.PublicCorporation => .35, _ => .65 };
        double payment = Math.Min(Math.Max(0, company.undistributedEarnings) * share,
            Math.Max(0, label.cashReserves - ProtectedCash(label)));
        return payment < .01 ? null : new DistributionPlan(label, owner, (float)payment);
    }
    private static void ExecuteDistribution(DistributionPlan plan) {
        LabelCompanyAccounts.ChangeCash(plan.Label, -plan.Amount, LabelCashKind.OwnerDistribution);
        if (plan.Owner != null) LabelCompanyAccounts.ChangeCash(plan.Owner, plan.Amount, LabelCashKind.OwnerDistribution);
        plan.Label.company.undistributedEarnings -= plan.Amount;
        plan.Label.company.lifetimeDistributions += plan.Amount;
    }
}
