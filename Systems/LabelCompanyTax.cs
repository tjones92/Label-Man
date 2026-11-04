using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>1960s ordinary US corporate brackets on the documented management-earnings tax proxy.
/// Five-year forward loss pool; no carryback, state tax, investment credit or group relief is simulated.</summary>
public static class LabelCompanyTax {
    /// <summary>Pre-1964 payment timing (SimTools/WorkingCapitalResearch.md): only half of a year's liability over
    /// $100,000 is paid in the current year (September, December); the rest is paid after the year ends, half in
    /// March and the remainder in June. Accrual and liability are unchanged; only when cash leaves changes. The 1964
    /// and 1966 Acts' acceleration of the over-threshold share is not phased in; all years use the pre-1964 rule.</summary>
    public const string PriorYearSwitch = "--prior-year-corporate-tax";
    public const double CurrentPaymentThreshold = 100000;
    public static bool PriorYear { get; private set; }
    public static void Configure(IEnumerable<string> arguments) {
        PriorYear = (arguments ?? Array.Empty<string>()).Contains(PriorYearSwitch);
        if (PriorYear && (!LabelCorporateModel.Tax || !LabelCorporateModel.ChangesEconomy))
            throw new ArgumentException(PriorYearSwitch + " requires the corporate income tax.");
    }
    internal static void SetPriorYearForChecks(bool value) => PriorYear = value;

    public static double Liability(int year, double taxableIncome) {
        double low = year <= 1963 ? .30 : .22;
        double high = year <= 1963 ? .52 : year == 1964 ? .50 : .48;
        // Calendar-year effective surcharge approximation (annual tables); fiscal proration omitted.
        if (year is 1968 or 1969) { low *= 1.10; high *= 1.10; }
        if (year == 1970) { low *= 1.025; high *= 1.025; }
        double income = Math.Max(0, taxableIncome);
        return Math.Min(25000, income) * low + Math.Max(0, income - 25000) * high;
    }

    public static double Accrue(AILabel label, int period, double pretaxEarnings) {
        var company = LabelCorporateModel.Ensure(label);
        if (company == null || !LabelCorporateModel.Tax || company.ownership == LabelOwnershipForm.OwnerOperated) return 0;
        int year = LabelCorporateModel.Year(period);
        if (company.taxYear != year) {
            company.taxYear = year;
            company.yearPretaxEarnings = company.yearTaxProvision = company.yearTaxPaid = 0;
        }
        company.losses.RemoveAll(loss => loss.expiresAfterYear < year || loss.amount < .001);
        company.yearPretaxEarnings += pretaxEarnings;
        double availableLoss = company.losses.Sum(loss => loss.amount);
        double target = Liability(year, company.yearPretaxEarnings - availableLoss);
        double provision = target - company.yearTaxProvision;
        company.yearTaxProvision = target;
        company.taxPayable += provision;
        if (LabelCorporateModel.Month(period) == 12) {
            double consume = Math.Max(0, company.yearPretaxEarnings);
            foreach (var loss in company.losses.OrderBy(loss => loss.expiresAfterYear)) {
                double used = Math.Min(loss.amount, consume);
                loss.amount -= used; consume -= used;
            }
            if (company.yearPretaxEarnings < 0)
                company.losses.Add(new LabelTaxLoss { expiresAfterYear = year + 5, amount = -company.yearPretaxEarnings });
        }
        return provision;
    }

    public static void PayQuarter(AILabel label, int period) {
        var company = LabelCorporateModel.Ensure(label);
        if (company == null || !LabelCorporateModel.Tax || company.ownership == LabelOwnershipForm.OwnerOperated) return;
        int month = LabelCorporateModel.Month(period);
        if (month % 3 != 0) return;
        if (PriorYear) { PayPriorYearSchedule(label, company, month); return; }
        // A later loss reverses the provision. A tax credit is refunded only at the annual true-up.
        double due = month == 12 ? company.taxPayable : Math.Max(0, company.taxPayable);
        Pay(label, company, due);
    }

    /// <summary>Tax cash that leaves at the month-ends of this month and the next <paramref name="months"/> - 1 on the current
    /// books: the schedule PayQuarter would run, with this period's unprovided tax counted in the year's provision. Never more
    /// than what is payable. Read-only; the slate's going-concern reserve uses it.</summary>
    internal static double DueWithin(AILabel label, int month, int months, double unprovided) {
        var company = LabelCorporateModel.Ensure(label);
        if (company == null || !LabelCorporateModel.Tax || company.ownership == LabelOwnershipForm.OwnerOperated) return 0;
        double payable = Math.Max(0, company.taxPayable + unprovided);
        double Excess(double liability) => Math.Max(0, liability - CurrentPaymentThreshold);
        double provision = company.yearTaxProvision + unprovided, prior = company.priorYearTaxDue, installments = company.currentYearTaxInstallments;
        double total = 0;
        for (int k = 0; k < months; k++) {
            int m = (month - 1 + k) % 12 + 1;
            if (m % 3 != 0) continue;
            if (!PriorYear) return payable;
            if (m == 3) { double due = prior < 0 ? 0 : Math.Min(company.priorYearTaxInstallment, prior); total += due; prior -= due; }
            else if (m == 6) { total += Math.Max(0, prior); prior = 0; }
            else if (m == 9) { double due = .25 * Excess(provision); total += due; installments += due; }
            else total += Math.Max(0, .5 * Excess(provision) - installments);
        }
        return Math.Min(total, payable);
    }

    private static void Pay(AILabel label, LabelCompanyState company, double due) {
        if (Math.Abs(due) < .001) return;
        LabelCompanyAccounts.ChangeCash(label, -(float)due, LabelCashKind.TaxPayment);
        company.taxPayable -= (float)due;
        company.yearTaxPaid += (float)due;
        company.lifetimeTaxPaid += (float)due;
    }

    private static void PayPriorYearSchedule(AILabel label, LabelCompanyState company, int month) {
        double Excess(double liability) => Math.Max(0, liability - CurrentPaymentThreshold);
        if (month == 3) {
            // An overpaid prior year (current installments above the final liability) is refunded here.
            double due = company.priorYearTaxDue < 0 ? company.priorYearTaxDue : Math.Min(company.priorYearTaxInstallment, company.priorYearTaxDue);
            Pay(label, company, due);
            company.priorYearTaxDue -= due;
        } else if (month == 6) {
            Pay(label, company, company.priorYearTaxDue);
            company.priorYearTaxDue = 0;
        } else if (month == 9) {
            double due = .25 * Excess(company.yearTaxProvision);
            Pay(label, company, due);
            company.currentYearTaxInstallments += due;
        } else {
            double due = Math.Max(0, .5 * Excess(company.yearTaxProvision) - company.currentYearTaxInstallments);
            Pay(label, company, due);
            // Everything still payable is this year's remainder, due next March and June.
            company.priorYearTaxDue = company.taxPayable;
            company.priorYearTaxInstallment = .5 * company.taxPayable;
            company.currentYearTaxInstallments = 0;
        }
    }
}
