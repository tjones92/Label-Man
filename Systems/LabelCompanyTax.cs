using System;
using System.Linq;

/// <summary>1960s ordinary US corporate brackets on the documented management-earnings tax proxy.
/// Five-year forward loss pool; no carryback, state tax, investment credit or group relief is simulated.</summary>
public static class LabelCompanyTax {
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
        // A later loss reverses the provision. A tax credit is refunded only at the annual true-up.
        double due = month == 12 ? company.taxPayable : Math.Max(0, company.taxPayable);
        if (Math.Abs(due) < .001) return;
        LabelCompanyAccounts.ChangeCash(label, -(float)due, LabelCashKind.TaxPayment);
        company.taxPayable -= (float)due;
        company.yearTaxPaid += (float)due;
        company.lifetimeTaxPaid += (float)due;
    }
}
