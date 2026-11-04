"""Trace cash, receivables and recent charting before each starting-tier bankruptcy.

Run analyze_boutique_closures.py first into --details. A bankruptcy is a
"squeeze" when receivables outstanding at exit are at least twice the overdraft:
the firm was owed far more than it was short. Associations, not counterfactuals;
the age profile of receivables is not logged, so no rescue count is implied.
"""
import argparse
import json
from pathlib import Path

import pandas as pd

from analyze_offchart_screen import path

FINANCE = ["week", "labelId", "cashReserves", "outstandingWholesaleReceivables", "weeklyPressing",
           "weeklyPlantPaid", "outstandingPlantPayables", "weeklyGross", "monthlyOverhead"]


def trace(tag, seed, weeks, labels, tier):
    failed = labels[labels.initialTier.eq(tier) & labels.cause.eq("Bankruptcy")]
    finance = pd.read_csv(path(tag, seed, "label-finance", weeks), usecols=FINANCE)
    finance = finance[finance.labelId.isin(failed.labelId)]
    chart = pd.read_csv(path(tag, seed, "records", weeks), usecols=["week", "recordId", "labelId", "currentPosition"])
    chart = chart[chart.currentPosition.between(1, 100) & chart.labelId.isin(failed.labelId)]
    rows = []
    for _, label in failed.iterrows():
        exit_week = int(label.exitWeek)
        f = finance[finance.labelId.eq(label.labelId) & finance.week.le(exit_week)].sort_values("week")
        last, w26 = f.iloc[-1], f[f.week.gt(exit_week - 26)]
        c = chart[chart.labelId.eq(label.labelId)]
        rows.append({"seed": seed, "labelId": label.labelId, "archetype": label.archetype, "exitWeek": exit_week,
            "overdraft": -float(last.cashReserves), "receivables": float(last.outstandingWholesaleReceivables),
            "plantPayables": float(last.outstandingPlantPayables), "overhead": float(last.monthlyOverhead),
            "receivablesMin26": float(w26.outstandingWholesaleReceivables.min()),
            "gross26": float(w26.weeklyGross.sum()), "plantPaid26": float(w26.weeklyPlantPaid.sum()),
            "chartingRecords52": int(c[c.week.between(exit_week - 52, exit_week)].recordId.nunique()),
            "chartingLast8": bool(c.week.between(exit_week - 8, exit_week).any()),
            "top40Last26": bool(c[c.week.between(exit_week - 26, exit_week)].currentPosition.le(40).any())})
    return rows


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--tag", default="bcs-v1-pay")
    parser.add_argument("--arm", default="pay")
    parser.add_argument("--tier", default="Boutique")
    parser.add_argument("--seeds", nargs="+", type=int, default=[1001, 2002, 3003])
    parser.add_argument("--weeks", type=int, default=265)
    parser.add_argument("--details", type=Path, default=Path("SimLogs/bcs-v1/closures"))
    parser.add_argument("--output", type=Path, default=Path("SimLogs/bcs-v1/working-capital"))
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    rows, tiers = [], {}
    for seed in args.seeds:
        labels = pd.read_csv(args.details / f"{args.arm}-{seed}-labels.csv")
        rows += trace(args.tag, seed, args.weeks, labels, args.tier)
        failed = labels[labels.cause.eq("Bankruptcy")]
        for tier, g in failed.groupby("initialTier"):
            t = tiers.setdefault(tier, {"labels": 0, "bankrupt": 0, "squeeze": 0})
            t["labels"] += int(labels.initialTier.eq(tier).sum())
            t["bankrupt"] += len(g)
            t["squeeze"] += int((g.exitReceivables >= 2 * -g.exitCash).sum())
    d = pd.DataFrame(rows)
    d["squeeze"] = d.receivables >= 2 * d.overdraft
    d.to_csv(args.output / f"{args.arm}-{args.tier}-bankruptcies.csv", index=False)
    summary = {"tier": args.tier, "bankrupt": len(d), "byTier": tiers, "groups": {}}
    for name, g in d.groupby("squeeze"):
        summary["groups"]["squeeze" if name else "other"] = {
            "labels": len(g), **{k + "Median": float(g[k].median()) for k in
                ["overdraft", "receivables", "plantPayables", "gross26", "plantPaid26", "exitWeek"]},
            "chartingLast8": int(g.chartingLast8.sum()), "top40Last26": int(g.top40Last26.sum()),
            "chartingRecords52": {str(k): int(v) for k, v in g.chartingRecords52.value_counts().sort_index().items()},
            "atLeast2In52AndChartingLast8": int((g.chartingRecords52.ge(2) & g.chartingLast8).sum()),
            "atLeast3In52AndChartingLast8": int((g.chartingRecords52.ge(3) & g.chartingLast8).sum())}
    (args.output / f"{args.arm}-{args.tier}-summary.json").write_text(json.dumps(summary, indent=2), encoding="utf-8")
    print(json.dumps(summary, indent=2))


if __name__ == "__main__":
    main()
