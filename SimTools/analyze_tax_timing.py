"""Corporate tax paid in cash before starting-tier bankruptcies, squeeze vs other.

Run analyze_boutique_closures.py first into --details. Tax is accrued on management
earnings that include receivables growth and paid quarterly in the current year
(LabelCompanyTax.PayQuarter). This reports how much was paid in the final six
calendar months against the overdraft at exit. Association, not a counterfactual.
"""
import argparse
import json
from pathlib import Path

import pandas as pd

from analyze_offchart_screen import path

TIERS = ["Small", "Boutique", "Independent"]


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--tag", default="bcs-v1-pay")
    parser.add_argument("--arm", default="control")
    parser.add_argument("--seeds", nargs="+", type=int, default=[1001, 2002, 3003])
    parser.add_argument("--weeks", type=int, default=265)
    parser.add_argument("--details", type=Path, default=Path("SimLogs/dms-v1/closures"))
    args = parser.parse_args()
    result = {}
    for seed in args.seeds:
        labels = pd.read_csv(args.details / f"{args.arm}-{seed}-labels.csv")
        failed = labels[labels.cause.eq("Bankruptcy") & labels.initialTier.isin(TIERS)].copy()
        failed["squeeze"] = failed.exitReceivables >= 2 * -failed.exitCash
        monthly = pd.read_csv(path(args.tag, seed, "company-monthly", args.weeks),
                              usecols=["labelId", "week", "period", "taxPaid", "pretaxEarnings", "receivablesChange"])
        for squeeze, group in failed.groupby("squeeze"):
            rows = []
            for _, label in group.iterrows():
                tail = monthly[monthly.labelId.eq(label.labelId) & monthly.week.le(label.exitWeek)].sort_values("period").tail(6)
                rows.append((tail.taxPaid.sum(), -label.exitCash, tail.receivablesChange.sum(), tail.pretaxEarnings.sum()))
            d = pd.DataFrame(rows, columns=["tax", "overdraft", "receivablesGrowth", "pretax"])
            bucket = result.setdefault("squeeze" if squeeze else "other",
                {"bankrupt": 0, "taxAtLeastOverdraft": 0, "taxAtLeastHalfOverdraft": 0, "taxMedianBySeed": {},
                 "receivablesGrowthMedianBySeed": {}, "pretaxMedianBySeed": {}})
            bucket["bankrupt"] += len(d)
            bucket["taxAtLeastOverdraft"] += int((d.tax >= d.overdraft).sum())
            bucket["taxAtLeastHalfOverdraft"] += int((d.tax >= .5 * d.overdraft).sum())
            bucket["taxMedianBySeed"][seed] = float(d.tax.median())
            bucket["receivablesGrowthMedianBySeed"][seed] = float(d.receivablesGrowth.median())
            bucket["pretaxMedianBySeed"][seed] = float(d.pretax.median())
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    main()
