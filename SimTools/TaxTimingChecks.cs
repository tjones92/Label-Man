using System;
using System.Text.Json;
using Godot;

/// <summary>--prior-year-corporate-tax: same accrual and liability, pre-1964 cash timing.</summary>
public static class TaxTimingChecks {
	public static void Run() {
		int checks = 0;
		void Check(bool value, string why) { if (!value) throw new InvalidOperationException("Tax timing: " + why); checks++; }
		bool Near(double a, double b) => Math.Abs(a - b) < .01;
		AILabel Firm(float cash) {
			var label = new AILabel { labelId = "tax-timing-check", tier = LabelTier.Independent, cashReserves = cash,
				status = LabelStatus.Stable, homeCityId = "memphis" };
			LabelCompanyAccounts.Open(label);
			return label;
		}
		// Accrue the given pretax in the first month of a year, then run every quarter-end payment through June next year.
		void Year(AILabel label, int year, double pretax, double decemberPretax = 0) {
			int january = year * 12;
			for (int month = 1; month <= 12; month++) {
				int period = january + month - 1;
				LabelCompanyTax.Accrue(label, period, month == 1 ? pretax : month == 12 ? decemberPretax : 0);
				LabelCompanyTax.PayQuarter(label, period);
			}
		}
		void FirstHalf(AILabel label, int year, double pretax = 0) {
			for (int month = 1; month <= 6; month++) {
				int period = year * 12 + month - 1;
				LabelCompanyTax.Accrue(label, period, month == 1 ? pretax : 0);
				LabelCompanyTax.PayQuarter(label, period);
			}
		}
		LabelCorporateModel.SetForChecks(true, tax: true);
		double small = LabelCompanyTax.Liability(1960, 50000);

		LabelCompanyTax.SetPriorYearForChecks(false);
		var current = Firm(100000);
		Year(current, 1960, 50000);
		Check(Near(current.cashReserves, 100000 - small) && Near(current.company.taxPayable, 0), "off: the year's tax is paid within the year");

		LabelCompanyTax.SetPriorYearForChecks(true);
		var deferred = Firm(100000);
		Year(deferred, 1960, 50000);
		Check(Near(deferred.cashReserves, 100000), "a small corporation pays nothing in the year it earns");
		Check(Near(deferred.company.taxPayable, small) && Near(deferred.company.priorYearTaxDue, small),
			"the whole liability is carried, unchanged, into next year");
		Check(LabelCorporateModel.TaxAndServiceReserve(deferred) >= small - .01, "deferred tax is still reserved from new commitments");
		var restored = JsonSerializer.Deserialize<AILabel>(JsonSerializer.Serialize(deferred, SaveGameService.TestJsonOptions), SaveGameService.TestJsonOptions);
		Check(Near(restored.company.priorYearTaxDue, small) && Near(restored.company.priorYearTaxInstallment, small / 2),
			"the payment schedule resumes from a save");
		FirstHalf(deferred, 1961, 50000);
		Check(Near(deferred.cashReserves, 100000 - small) && Near(deferred.company.priorYearTaxDue, 0),
			"last year's liability is paid in March and June, in full");
		Check(Near(deferred.company.taxPayable, LabelCompanyTax.Liability(1961, 50000)),
			"this year's accrual is not paid with last year's installments");
		Check(Near(deferred.company.lifetimeTaxPaid, current.company.lifetimeTaxPaid), "timing changes, the liability does not");

		var march = Firm(100000);
		Year(march, 1960, 50000);
		LabelCompanyTax.Accrue(march, 1961 * 12, 0); LabelCompanyTax.PayQuarter(march, 1961 * 12 + 2);
		Check(Near(march.cashReserves, 100000 - small / 2), "half is due in March");

		double large = LabelCompanyTax.Liability(1960, 1000000);
		double installment = .25 * (large - LabelCompanyTax.CurrentPaymentThreshold);
		var big = Firm(2000000);
		Year(big, 1960, 1000000);
		Check(Near(big.company.lifetimeTaxPaid, 2 * installment), "half the excess over $100,000 is paid in September and December");
		Check(Near(big.company.priorYearTaxDue, large - 2 * installment), "the remainder is due after the year ends");
		FirstHalf(big, 1961);
		Check(Near(big.company.lifetimeTaxPaid, large) && Near(big.company.taxPayable, 0), "large liabilities are also settled by June");

		var reversed = Firm(2000000);
		Year(reversed, 1960, 1000000, decemberPretax: -1000000);
		Check(Near(reversed.company.lifetimeTaxPaid, installment), "a December loss stops the December installment");
		Check(Near(reversed.company.priorYearTaxDue, -installment), "an overpaid year carries a credit");
		FirstHalf(reversed, 1961);
		Check(Near(reversed.cashReserves, 2000000) && Near(reversed.company.lifetimeTaxPaid, 0), "the overpayment is refunded in March");

		var sole = Firm(100000); sole.company.ownership = LabelOwnershipForm.OwnerOperated;
		Year(sole, 1960, 50000);
		Check(Near(sole.cashReserves, 100000), "owner-operated firms still pay no corporate tax");

		GameRandom.Seed(7310); float next = GameRandom.Randf(); GameRandom.Seed(7310);
		Year(Firm(100000), 1960, 50000);
		Check(next == GameRandom.Randf(), "tax timing consumes no random draws");
		LabelCompanyTax.SetPriorYearForChecks(false);
		LabelCorporateModel.SetForChecks(false);
		GD.Print($"TAX_TIMING_CHECK_PASSED checks={checks}");
	}
}
