using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Experimental contingent compensation for an incorporated, no-employee founder.
/// The nominal work quote is a ceiling, not a bill owed regardless of income.
/// No outside income, wage arrears, new randomness or household simulation is invented.</summary>
public static class FounderCompensation {
    public const string Switch = "--founder-surplus-compensation";
    public static bool Enabled { get; private set; }
    public static void Configure(IEnumerable<string> arguments) {
        Enabled = (arguments ?? Array.Empty<string>()).Contains(Switch);
        if (Enabled && (!LabelCorporateModel.HomeOperated || !LabelCorporateModel.Staff || !LabelCorporateModel.Premises))
            throw new ArgumentException(Switch + " requires corporate staff/premises and home operation.");
    }
    internal static void SetForChecks(bool value) => Enabled = value;

    internal static bool Applies(AILabel label, LabelCompanyState company) => Enabled
        && LabelCorporateModel.HomeOperated && LabelCorporateModel.Applies(label)
        && company?.ownership == LabelOwnershipForm.PrivateCorporation
        && LabelCorporateModel.IsFounderRun(label, company);

    internal static double FixedFounderPayroll(AILabel label, LabelCompanyState company) =>
        Applies(label, company) ? 0 : company.founderPayrollQuote;

    internal static float Pay(AILabel label) {
        var company = label?.company;
        if (!Applies(label, company) || !label.IsActive || !company.accountsOpened) return 0;
        double Cash(LabelCashKind kind) => company.cashMovements.GetValueOrDefault(kind);
        // Financing and dividends are not earned income. Capital purchases are not
        // ordinary operating bills, but still reduce the cash available below.
        double surplus = company.cashMovements.Values.Sum() - Cash(LabelCashKind.Financing)
            - Cash(LabelCashKind.OwnerDistribution) - Cash(LabelCashKind.DistributionInvestment)
            - Cash(LabelCashKind.StudioInvestment);
        double alreadyPaid = Math.Max(0, -Cash(LabelCashKind.OwnerSalary));
        if (alreadyPaid > 0) return 0;
        double limit = Math.Max(0, company.founderPayrollQuote - alreadyPaid);
        // Half of collected surplus, capped at the existing work quote, is a disclosed
        // scenario rule. Preserve tax, plant, commercial and reinvestment reserves.
        double available = Math.Max(0, label.cashReserves - LabelCorporateModel.ProtectedCash(label));
        float payment = (float)Math.Min(limit, Math.Min(Math.Max(0, surplus) * .5, available));
        if (payment <= 0) return 0;
        LabelCompanyAccounts.ChangeCash(label, -payment, LabelCashKind.OwnerSalary);
        label.monthlyExpenses += payment;
        return payment;
    }
}
