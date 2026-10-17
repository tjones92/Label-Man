"""Check that historical review cannot mix units, configurations or repeated seeds."""
import copy
import tempfile
import unittest
from pathlib import Path

import pandas as pd

from analyze_label_tier_historical_benchmarks import analyze, review_1960


class HistoricalConcentrationReviewTests(unittest.TestCase):
    def sample(self, values=((20, 40), (30, 50), (40, 60))):
        runs, results = [], []
        for seed, (cr4, cr8) in enumerate(values, 1):
            name = f"sample-{seed}"
            runs.append({"prefix": f"SimLogs/{name}", "annual": {1960: {
                "chartWeeks": 52, "top10Slots": 520,
                "cr4Top10SlotsPct": cr4, "cr8Top10SlotsPct": cr8}}})
            results.append({"name": name, "passed": True, "exitCode": 0,
                            "args": ["--weeks=52", "--calibration", f"--seed={seed}", f"--run={name}"]})
        return runs, {"assemblySha256": "fixture-assembly", "results": results}

    def test_outlier_seeds_do_not_replace_the_multi_seed_mean(self):
        runs, manifest = self.sample()
        review = review_1960(runs, manifest)
        self.assertEqual(review["status"], "WITHIN_BANDS")
        self.assertEqual(review["metrics"]["cr4Top10SlotsPct"]["meanPct"], 30)
        self.assertEqual(review["metrics"]["cr8Top10SlotsPct"]["meanPct"], 50)

    def test_inclusive_boundaries_and_advisory_departures(self):
        runs, manifest = self.sample(((23, 57),) * 3)
        self.assertEqual(review_1960(runs, manifest)["status"], "WITHIN_BANDS")
        for run in runs:
            run["annual"][1960]["cr4Top10SlotsPct"] = 34
        review = review_1960(runs, manifest)
        self.assertEqual(review["status"], "REVIEW")
        self.assertTrue(review["advisoryOnly"])
        self.assertEqual(len(review["historicalWarnings"]), 1)

    def test_two_seeds_are_provisional_and_duplicate_seeds_are_rejected(self):
        runs, manifest = self.sample(((28, 52),) * 2)
        self.assertEqual(review_1960(runs, manifest)["status"], "INSUFFICIENT_SEEDS")
        manifest["results"][1]["args"][2] = "--seed=1"
        with self.assertRaisesRegex(ValueError, "Duplicate seed"):
            review_1960(runs, manifest)

    def test_partial_failed_or_mismatched_runs_cannot_enter_review(self):
        runs, manifest = self.sample()
        partial = copy.deepcopy(runs)
        partial[0]["annual"][1960]["chartWeeks"] = 51
        with self.assertRaisesRegex(ValueError, "52 weeks"):
            review_1960(partial, manifest)
        failed = copy.deepcopy(manifest)
        failed["results"][0]["passed"] = False
        with self.assertRaisesRegex(ValueError, "did not complete"):
            review_1960(runs, failed)
        with self.assertRaisesRegex(ValueError, "match the manifest"):
            review_1960(runs[:-1], manifest)
        different_flags = copy.deepcopy(manifest)
        different_flags["results"][1]["args"].append("--no-small-home-territory")
        with self.assertRaisesRegex(ValueError, "same flags"):
            review_1960(runs, different_flags)

    def test_top10_slots_do_not_use_unit_concentration_or_rank_11(self):
        root = Path(__file__).resolve().parent.parent / "SimLogs"
        with tempfile.TemporaryDirectory(dir=root) as directory:
            prefix = str(Path(directory) / "fixture")
            rows = [dict(week=week, year=1960, recordId=f"record-{rank}",
                         artistId=f"artist-{rank}", labelId=f"label-{rank}",
                         labelTier="MidTier", genre="TeenPop", currentPosition=rank,
                         unitsThisWeek=1000000 if rank == 11 else 1)
                    for week in range(1, 53) for rank in range(1, 12)]
            records = pd.DataFrame(rows)
            records.to_csv(prefix + "-records.csv", index=False)
            pd.DataFrame(columns=["resolution"]).to_csv(prefix + "-deal-ledger.csv", index=False)
            pd.DataFrame([dict(year=1960, totalChartUnits=int(records.unitsThisWeek.sum()),
                               c4ChartShare=0.99, c8ChartShare=0.999)]).to_csv(
                                   prefix + "-concentration.csv", index=False)
            pd.DataFrame([dict(labelId=f"label-{rank}", labelName=f"Label {rank}")
                          for rank in range(1, 12)]).to_csv(prefix + "-label-directory.csv", index=False)
            pd.DataFrame(columns=["week", "eventType", "artistId", "currentPrimaryGenre",
                                  "labelTier", "priorContractCount"]).to_csv(
                                      prefix + "-artist-population-events.csv", index=False)
            result = analyze(prefix)["annual"][1960]
            self.assertEqual(result["top10Slots"], 520)
            self.assertEqual(result["cr4Top10SlotsPct"], 40)
            self.assertEqual(result["cr8Top10SlotsPct"], 80)
            self.assertEqual(result["operatingLabelsWithAnyTop10"], 10)


if __name__ == "__main__":
    unittest.main()
