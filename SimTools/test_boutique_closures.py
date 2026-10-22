"""Cash and cohort boundaries for the Boutique closure investigation."""
import unittest

import pandas as pd

from analyze_boutique_closures import EXPENSES, cash_bridge, early_quality, label_details


class BoutiqueClosureChecks(unittest.TestCase):
    def fixture(self):
        directory = pd.DataFrame([dict(labelId="a", initialTier="Boutique")])
        first = dict(labelId="a", week=1, cashReserves=1000, monthlyOverhead=50,
                     labelName="A", archetype="JazzPrestige", isHistorical=False,
                     status="Stable", labelTier="Boutique", exitReason="None")
        last = {**first, "week": 9, "cashReserves": -20, "status": "Bankrupt",
                "labelTier": "Independent", "exitReason": "Bankruptcy"}
        finance = pd.DataFrame([first, last])
        month = {c: 0.0 for c in EXPENSES}
        month.update(labelId="a", week=5, period=23520, openingCash=1000,
                     closingCash=1050, cashRounding=0, recordReceipts=100, serviceIncome=0,
                     financingNet=0, ownerTransfersNet=0, payroll=50,
                     cashReconciliationError=0, employees=.25, ownership="PrivateCorporation",
                     costOfSalesAndWriteOffs=99999)
        monthly = pd.DataFrame([month, {**month, "week": 10, "period": 23521,
                                       "payroll": 100, "closingCash": 1000}])
        census = pd.DataFrame([dict(recordId="r", labelId="a", observationKind="Launch",
                                   observedAuditWeek=2, format="Album", runtimeQualityAtLaunch=.8)])
        sales = pd.DataFrame([dict(week=3, labelId="a", format="Album", units=10)])
        return directory, finance, monthly, census, sales

    def test_cash_does_not_subtract_accrual_cogs_twice(self):
        monthly = self.fixture()[2]
        self.assertEqual(cash_bridge(monthly).tolist(), [50, 0])

    def test_starting_tier_and_exit_boundary_survive_promotion(self):
        labels, residual = label_details(*self.fixture())
        row = labels.iloc[0]
        self.assertEqual(row.initialTier, "Boutique")
        self.assertEqual(row.finalTier, "Independent")
        self.assertEqual(row.observedMonths, 1)
        self.assertEqual(row.total_payroll, 50)
        self.assertEqual(row.albumLaunches, 1)
        self.assertEqual(row.albumUnits, 10)
        self.assertEqual(residual, 0)

    def test_duplicate_month_rejected(self):
        frames = list(self.fixture())
        frames[2] = pd.concat([frames[2], frames[2].iloc[:1]], ignore_index=True)
        with self.assertRaisesRegex(ValueError, "Repeated rows"):
            label_details(*frames)

    def test_contingent_pay_is_spending_not_a_fixed_overhead_quote(self):
        frames = list(self.fixture())
        frames[2].loc[0, "ownerSalary"] = 25
        frames[2].loc[0, "closingCash"] = 1025
        labels, _ = label_details(*frames)
        row = labels.iloc[0]
        self.assertEqual(row.firstMonthLaborAndPremises, 75)
        self.assertEqual(row.firstWeekMandatoryOverhead, 50)

    def test_liquidity_screen_uses_exit_cash_not_later_collections(self):
        frames = list(self.fixture())
        frames[1]["outstandingWholesaleReceivables"] = [0, 10]
        frames[1]["outstandingPlantPayables"] = 0
        recovered = {**frames[1].iloc[-1].to_dict(), "week": 12, "cashReserves": 100}
        frames[1] = pd.concat([frames[1], pd.DataFrame([recovered])], ignore_index=True)
        labels, _ = label_details(*frames)
        row = labels.iloc[0]
        self.assertEqual(row.exitCash, -20)
        self.assertEqual(row.lastObservedCash, 100)
        self.assertFalse(row.receivablesCoverExitCashPlantAndRecordedTax)

    def test_broken_bridge_and_nonfinite_cash_rejected(self):
        for bad in [1051, float("nan")]:
            frames = list(self.fixture())
            frames[2].loc[0, "closingCash"] = bad
            with self.assertRaises(ValueError):
                label_details(*frames)

    def test_chart_followup_uses_launch_origin_and_fixed_window(self):
        frames = self.fixture()
        labels, _ = label_details(*frames)
        census = pd.concat([frames[3], pd.DataFrame([
            dict(recordId="single", labelId="a", observationKind="Launch", observedAuditWeek=1,
                 format="Single", runtimeQualityAtLaunch=.8),
            dict(recordId="late", labelId="a", observationKind="Launch", observedAuditWeek=14,
                 format="Single", runtimeQualityAtLaunch=.8)])], ignore_index=True)
        chart = pd.DataFrame([dict(recordId="r", week=52, currentPosition=11),
                              dict(recordId="single", week=52, currentPosition=11),
                              dict(recordId="single", week=53, currentPosition=1)])
        result = early_quality(labels, census, chart)
        self.assertEqual(result, [dict(format="Single", qualityBand=">.7",
            originEventuallyBankrupt=True, launches=1, chartedBy52=1, top10By52=0)])


if __name__ == "__main__":
    unittest.main()
