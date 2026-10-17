"""Regression cases for format isolation, absent-market peaks and survey-invariant airplay."""
import math
import tempfile
import unittest
from pathlib import Path

import pandas as pd

from analyze_regional_guards import airplay_share_top40
from analyze_regional_sync import decompose, raw_timing, settlement
from analyze_regional_survival import cohort
from analyze_economic_lifecycle import analyze as economic_lifecycle


class RegionalAnalysisTests(unittest.TestCase):
    def test_legacy_empty_financial_history_is_not_measured_cash_flow(self):
        with tempfile.TemporaryDirectory(dir=Path(__file__).resolve().parents[1] / "SimLogs") as folder:
            prefix = str(Path(folder) / "run")
            log = Path(prefix + "-console.log")
            log.write_text("CHART_AUDIT_COMPLETE run=test weeks=1")
            pd.DataFrame({"week": [1], "year": [1960], "labelId": ["label"], "recordId": ["record"],
                          "currentPosition": [0]}).to_csv(prefix + "-records.csv", index=False)
            for enabled in (False, True):
                pd.DataFrame({"week": [1], "year": [1960], "labelId": ["label"], "labelTier": ["Small"],
                              "status": ["Stable"], "recentMonthlyCashFlow": [100.],
                              "economicLabelLifecycle": [enabled]}).to_csv(prefix + "-label-finance.csv", index=False)
                result = economic_lifecycle(prefix)
                self.assertEqual(result["economicFinancialTelemetry"], enabled)
                self.assertEqual("survivingWithoutSingleChartWithPositiveRecentCashFlow" in result["annual"][1960], enabled)
            log.write_text("[LabelManager] label has closed. Reason: Bankruptcy.\nCHART_AUDIT_COMPLETE run=test weeks=1")
            with self.assertRaises(ValueError):
                economic_lifecycle(prefix)

    def test_survival_counts_promoted_labels_and_separates_closure_causes(self):
        frame = pd.DataFrame({
            "exposureWeeks": [104, 104, 60, 80], "bankrupt": [False, False, True, False],
            "singleUnits": [1000, 10000, 100, 0],
            "fate": ["observed", "observed", "Bankruptcy", "Competitive exit"],
            "endStatus": ["Stable", "Rising", "Defunct", "Defunct"],
            "endTier": ["Small", "Independent", "Small", "Small"],
        })
        result = cohort(frame)
        self.assertEqual(result["closures"], 2)
        self.assertEqual(result["bankruptcies"], 1)
        self.assertEqual(result["competitiveExits"], 1)
        self.assertEqual(result["survivalFraction"], .5)
        self.assertEqual(result["survivingEndTiers"], {"Small": 1, "Independent": 1})

    def test_single_peaks_exclude_albums_and_empty_markets(self):
        rows = []
        for week in range(1, 7):
            for region in ("a", "b", "c"):
                units = 100 if (region == "a" and week == 2) or (region == "b" and week == 4) else 0
                rows.append((week, "single", region, units))
                rows.append((week, "album", region, 10000 if week == 6 else 1000))
        with tempfile.TemporaryDirectory(dir=Path(__file__).resolve().parents[1] / "SimLogs") as folder:
            path = Path(folder) / "regional.csv"
            pd.DataFrame(rows, columns=["week", "recordId", "regionId", "finalCleared"]).to_csv(path, index=False)
            all_formats = settlement(path, 1)
            singles = settlement(path, 1, {"single"})
            self.assertEqual(all_formats["soldPeakSpread"]["records"], 2)
            self.assertEqual(singles["soldPeakSpread"]["records"], 1)
            self.assertEqual(singles["soldPeakSpread"]["median"], 2)
            self.assertEqual(singles["regionsWithoutSales"], 1)

    def test_zero_demand_market_has_no_peak_or_arrival(self):
        rows = [(w, "single", r, 100 if (r == "a" and w == 2) or (r == "b" and w == 4) else 0)
                for w in range(1, 7) for r in ("a", "b", "c")]
        with tempfile.TemporaryDirectory(dir=Path(__file__).resolve().parents[1] / "SimLogs") as folder:
            path = Path(folder) / "stages.csv"
            pd.DataFrame(rows, columns=["week", "recordId", "region", "rawDemand"]).to_csv(path, index=False)
            result = raw_timing(path, 1)
            self.assertEqual(result["rawPeakSpread"]["median"], 2)
            self.assertNotIn("c", result["meanArrivalAgeByRegion"])

    def test_airplay_share_does_not_change_with_survey_draw(self):
        for sample in (.5, 1, 1.5):
            frame = pd.DataFrame({"unitsThisWeek": [80.], "chartPoints": [100 * sample],
                                  "unsampledChartPoints": [100.]})
            self.assertAlmostEqual(airplay_share_top40(frame), .2)
        self.assertTrue(math.isnan(airplay_share_top40(pd.DataFrame({"unitsThisWeek": [80.], "chartPoints": [150.]}))))

    def test_weekly_change_does_not_bridge_missing_weeks(self):
        rows = []
        for record in ("a", "b"):
            for region in range(3):
                for week in (1, 3, 4):
                    raw = 1000 * (region + 1) ** 2 if week == 1 else 100 * (region + 1)
                    if week == 4:
                        raw *= 1.1 + .1 * region
                    if record == "b":
                        raw *= 1.05
                    rows.append((week, record, str(region), 100000, raw / .1, .1, raw))
        with tempfile.TemporaryDirectory(dir=Path(__file__).resolve().parents[1] / "SimLogs") as folder:
            path = Path(folder) / "changes.csv"
            pd.DataFrame(rows, columns=["week", "recordId", "region", "potentialAudience", "awareBuyers",
                                       "intrinsicConversionRate", "rawDemand"]).to_csv(path, index=False)
            result = decompose(path)
            self.assertEqual(result["excludedNonconsecutiveRows"], 6)
            self.assertEqual(result["changeRecordWeeks"], 2)


if __name__ == "__main__":
    unittest.main()
