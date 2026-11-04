"""Regression for the distinction between firm closure and first exit from Small."""
import tempfile
import unittest
from pathlib import Path

import pandas as pd

from analyze_small_cost_pilots import arm


class SmallExitDefinitionTests(unittest.TestCase):
    def test_later_bankruptcy_does_not_undo_promotion_or_acquisition(self):
        with tempfile.TemporaryDirectory(dir=Path(__file__).resolve().parent.parent / "SimLogs") as directory:
            prefix = str(Path(directory) / "fixture")
            rows = []
            for label in ["fails-small", "fails-independent", "sold", "demoted-back", "survives"]:
                rows.append(dict(week=1, labelId=label, labelTier="Small", status="Stable", cashReserves=1000,
                                 weeklyGross=100, weeklyPressing=20))
                tier = "Independent" if label == "fails-independent" else "Small"
                status = "Bankrupt" if label in ["fails-small", "fails-independent", "demoted-back"] else (
                    "Acquired" if label == "sold" else "Stable")
                rows.append(dict(week=5, labelId=label, labelTier=tier, status=status, cashReserves=-10,
                                 weeklyGross=100, weeklyPressing=20))
            pd.DataFrame(rows).to_csv(prefix + "-label-finance.csv", index=False)
            pd.DataFrame([
                dict(week=3, labelId="fails-independent", direction="promotion"),
                dict(week=3, labelId="demoted-back", direction="promotion"),
                dict(week=4, labelId="demoted-back", direction="demotion"),
            ]).to_csv(prefix + "-label-tier-transitions.csv", index=False)
            result = arm(prefix, 5)
            self.assertEqual(result["Small_closed_pct"], 60)
            self.assertEqual(result["Small_exit_before_promotion_pct"], 20)
            early = arm(prefix, 1)
            self.assertEqual(early["Small_closed_pct"], 0)
            self.assertEqual(early["Small_exit_before_promotion_pct"], 0)


if __name__ == "__main__":
    unittest.main()
