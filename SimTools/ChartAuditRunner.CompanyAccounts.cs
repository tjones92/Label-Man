using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

public partial class ChartAuditRunner {
    private StreamWriter companyStatementWriter, companyDepartmentWriter, companyCashWriter;
    private static readonly FieldInfo[] companyStatementFields = typeof(LabelCompanyStatement).GetFields().OrderBy(f => f.MetadataToken).ToArray();

    private void OpenCompanyOutputs(string directory) {
        if (!LabelCorporateModel.Ledger) return;
        companyStatementWriter = CreateWriter(Path.Combine(directory, $"{runName}-company-monthly.csv"));
        companyStatementWriter.WriteLine("week,year,month,labelName,ownership,ownerLabelId," + string.Join(",", companyStatementFields.Select(f => f.Name)));
        companyDepartmentWriter = CreateWriter(Path.Combine(directory, $"{runName}-company-departments.csv"));
        companyDepartmentWriter.WriteLine("week,period,labelId,tier,department,employees,requiredEmployees,founderEquivalent,monthlyBaseWage,payrollQuote,premisesQuote,retainerQuote,coverage");
        companyCashWriter = CreateWriter(Path.Combine(directory, $"{runName}-company-cash.csv"));
        companyCashWriter.WriteLine("week,year,month,labelId,kind,amount,rounding,cashAfter");
        LabelCompanyAccounts.MonthClosed += WriteCompanyMonth;
        LabelCompanyAccounts.CashChanged += WriteCompanyCash;
    }

    private static string CompanyNumber(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    private void WriteCompanyCash(LabelCompanyCashEvent entry) {
        var date = TimeManager.Instance.CurrentDate;
        companyCashWriter?.WriteLine(string.Join(",", new[] { currentAuditWeek.ToString(CultureInfo.InvariantCulture),
            date.year.ToString(CultureInfo.InvariantCulture), date.month.ToString(CultureInfo.InvariantCulture), Csv(entry.Label.labelId),
            entry.Kind.ToString(), CompanyNumber(entry.Amount), CompanyNumber(entry.Rounding), CompanyNumber(entry.CashAfter) }));
    }
    private void WriteCompanyMonth(AILabel label, LabelCompanyStatement statement) {
        string Value(FieldInfo field) {
            object value = field.GetValue(statement);
            return value is double number ? CompanyNumber(number) : Csv(Convert.ToString(value, CultureInfo.InvariantCulture));
        }
        companyStatementWriter?.WriteLine(string.Join(",", new[] { currentAuditWeek.ToString(CultureInfo.InvariantCulture),
            LabelCorporateModel.Year(statement.period).ToString(CultureInfo.InvariantCulture), LabelCorporateModel.Month(statement.period).ToString(CultureInfo.InvariantCulture),
            Csv(label.labelName), label.company.ownership.ToString(), Csv(label.ownerLabelId) }.Concat(companyStatementFields.Select(Value))));
        foreach (var department in label.company.departments.OrderBy(d => d.department))
            companyDepartmentWriter?.WriteLine(string.Join(",", new[] { currentAuditWeek.ToString(CultureInfo.InvariantCulture),
                statement.period.ToString(CultureInfo.InvariantCulture), Csv(label.labelId), label.tier.ToString(), department.department.ToString(),
                CompanyNumber(department.employees), CompanyNumber(department.requiredEmployees), CompanyNumber(department.founderEquivalent), CompanyNumber(LabelCorporateModel.Wage(department.department)),
                CompanyNumber(label.company.payrollQuote), CompanyNumber(label.company.premisesQuote), CompanyNumber(label.company.retainerQuote), CompanyNumber(label.company.organizationCoverage) }));
    }
    private void CloseCompanyOutputs() {
        LabelCompanyAccounts.MonthClosed -= WriteCompanyMonth;
        LabelCompanyAccounts.CashChanged -= WriteCompanyCash;
        companyStatementWriter?.Dispose(); companyDepartmentWriter?.Dispose(); companyCashWriter?.Dispose();
        companyStatementWriter = companyDepartmentWriter = companyCashWriter = null;
    }
}
