"""Guard the cohort definition and reject incomplete calibration evidence."""
import csv
import importlib.util
import json
import tempfile
import unittest
from unittest.mock import patch
from pathlib import Path

spec = importlib.util.spec_from_file_location("retune_analysis", Path(__file__).with_name("analyze_economy_retune.py"))
analysis = importlib.util.module_from_spec(spec)
spec.loader.exec_module(analysis)
wealth_spec = importlib.util.spec_from_file_location("wealth_analysis", Path(__file__).with_name("compare_economy_wealth.py"))
wealth = importlib.util.module_from_spec(wealth_spec)
wealth_spec.loader.exec_module(wealth)


class EconomyRetuneAnalysisChecks(unittest.TestCase):
    def setUp(self):
        scratch = Path(__file__).resolve().parents[1] / "SimLogs"
        scratch.mkdir(exist_ok=True)
        self.folder = tempfile.TemporaryDirectory(dir=scratch)
        assert Path(self.folder.name).resolve().is_relative_to(scratch.resolve())
        self.addCleanup(self.folder.cleanup)
        self.prefix = str(Path(self.folder.name) / "fixture")
        self.write("label-directory", ["labelId", "initialTier"], [("a", "Small"), ("b", "Small")])
        self.write("label-tier-transitions", ["week", "direction", "labelId"], [(2, "promotion", "a")])
        self.finance()

    def write(self, suffix, fields, records):
        with open(f"{self.prefix}-{suffix}.csv", "w", newline="", encoding="utf-8") as handle:
            writer = csv.writer(handle)
            writer.writerow(fields)
            writer.writerows(records)

    def finance(self, weeks=(1, 2, 3)):
        fields = ["week", "year", "labelId", "labelTier", "status", "cashReserves", "monthlyOverhead",
                  "weeklyGross", "weeklyNet", "weeklyPressing", "weeklyRoyalty", "weeklySkim"]
        self.write("label-finance", fields,
                   [(w, 1960, label, "Independent" if label == "a" and w >= 2 else "Small",
                     "Defunct" if label == "a" and w == 3 else "Stable", 200, 100, 10, 5, 2, 1, 1)
                    for w in weeks for label in ("a", "b", "runtime")])

    def test_closure_after_promotion_is_separate_from_small_exit(self):
        r = analysis.analyze(self.prefix, 3)["startingSmall"]
        self.assertEqual(r["n"], 2)
        self.assertEqual(r["closed"], 1)
        self.assertEqual(r["closedBeforeFirstPromotion"], 0)
        self.assertEqual(r["everPromoted"], 1)
        self.assertEqual(r["survivors"], 1)
        self.assertNotIn("fiveYearClosureInBand", r)

    def test_partial_stream_is_not_a_completed_horizon(self):
        self.finance((1, 3))
        with self.assertRaisesRegex(ValueError, "incomplete finance stream"):
            analysis.analyze(self.prefix, 3)

    def test_matched_horizon_ignores_future_closure(self):
        r = analysis.analyze(self.prefix, 2)["startingSmall"]
        self.assertEqual(r["closed"], 0)
        self.assertEqual(r["survivors"], 2)

    def test_partial_final_week_cannot_silently_use_prior_week(self):
        path = Path(f"{self.prefix}-label-finance.csv")
        with path.open(newline="", encoding="utf-8") as handle:
            reader = csv.reader(handle)
            fields = next(reader)
            records = [r for r in reader if not (r[0] == "3" and r[2] == "b")]
        self.write("label-finance", fields, records)
        with self.assertRaisesRegex(ValueError, "incomplete final-week"):
            analysis.analyze(self.prefix, 3)

    def test_review_requires_three_distinct_seeds_in_one_configuration(self):
        runs = [{"prefix": f"seed-{i}", "startingSmall": {"closedPct": 25, "fiveYearClosureInBand": True}} for i in range(3)]
        entries = [{"name": r["prefix"], "seed": i, "passed": True, "exitCode": 0,
                    "args": ["--weeks=260", "--pressing-repress-weeks=12", f"--seed={i}", f"--run={r['prefix']}"]}
                   for i, r in enumerate(runs)]
        manifest = {"weeks": 260, "assemblySha256": "fixture", "results": entries}
        path = Path(self.folder.name) / "runs.json"
        path.write_text(json.dumps(manifest), encoding="utf-8")
        self.assertEqual(analysis.review(runs, path, 260)["status"], "IN_BAND")
        entries[2]["args"][1] = "--pressing-repress-weeks=8"
        path.write_text(json.dumps(manifest), encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "different configurations"):
            analysis.review(runs, path, 260)
        entries[2]["seed"] = 1
        path.write_text(json.dumps(manifest), encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "distinct"):
            analysis.review(runs, path, 260)

    def test_five_year_horizon_retains_all_1964_chart_weeks(self):
        self.finance(range(1, 261))
        self.write("decade-annual-rollup", ["year", "singleUnits", "singleNet", "albumUnits", "albumNet"],
                   [(year, 100, 10, 50, 20) for year in range(1960, 1966)])
        annual = analysis.analyze(self.prefix, 260)["completedAnnual"]["decade-annual-rollup"]
        self.assertEqual(set(annual), {str(y) for y in range(1960, 1965)})

    def test_wealth_uses_ratio_of_seed_means_and_retains_seed_outliers(self):
        def value(prefix):
            seed = int(prefix[-1])
            return seed, ({1: 10, 2: 100} if prefix.startswith("original") else {1: 20, 2: 80})[seed]
        def annual(prefix):
            seed, v = value(prefix)
            return seed, {y: {"singleNet": v, "albumNet": v} for y in range(1960, 1965)}
        def summary(prefix, weeks):
            seed, v = value(prefix)
            return {"activeMedianCashByCurrentTier": {t: v for t in analysis.TIERS},
                    "startingSmall": {"closedPct": 25}, "activeCountByCurrentTier": {},
                    "survivorMedianCashByStartingTier": {}, "totalActiveCash": v}
        with patch.object(wealth, "annual", side_effect=annual), patch.object(wealth.analysis, "analyze", side_effect=summary):
            r = wealth.compare(["candidate1", "candidate2"], "original{seed}")
            self.assertAlmostEqual(r["metrics"]["singleNet"]["ratio"], 100 / 110)
            self.assertTrue(r["allMeansNearOriginal"])
            self.assertEqual(r["runs"][0]["ratios"]["singleNet"], 2)
            mapped = wealth.compare(["candidate1", "candidate2"], {2: "original2", 1: "original1"})
            self.assertEqual(mapped["metrics"], r["metrics"])
            with self.assertRaisesRegex(ValueError, "distinct"):
                wealth.compare(["candidate1", "candidate1"], "original{seed}")

    def test_wealth_rejects_missing_1964(self):
        self.write("decade-annual-rollup", ["seed", "year", "singleNet", "albumNet", "singleUnits", "albumUnits"],
                   [(1001, year, 1, 1, 1, 1) for year in range(1960, 1964)])
        with self.assertRaisesRegex(ValueError, "missing annual rows"):
            wealth.annual(self.prefix)

    def test_operator_ledger_respects_reinvestment_and_cash_reserves(self):
        fields = ["week", "tier", "operatingCashSurplus", "pay", "cashAfter", "monthlyOverhead", "outstandingPlantPayables", "monthlyCap"]
        self.write("operator-pay", fields, [(2, "Small", 1000, 300, 1000, 150, 0, 800)])
        self.assertEqual(analysis.analyze(self.prefix, 3)["operatorPayByTier"], {"Small": 300})
        for pay, cash in [(400, 1000), (300, 850)]:
            self.write("operator-pay", fields, [(2, "Small", 1000, pay, cash, 150, 0, 800)])
            with self.assertRaisesRegex(ValueError, "inconsistent operator payment"):
                analysis.analyze(self.prefix, 3)


if __name__ == "__main__":
    unittest.main()
