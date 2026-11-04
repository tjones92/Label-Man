"""Compact matched-horizon Small-label audit, including fast finance-only runs."""
import argparse
import json
from pathlib import Path

import pandas as pd


def arm(prefix, weeks):
    f = pd.read_csv(prefix + "-label-finance.csv")
    f = f[f.week <= weeks]
    initial = f[f.week == 1].set_index("labelId")
    end = f.groupby("labelId").last().reindex(initial.index)
    small = initial.index[initial.labelTier == "Small"]
    result = {"prefix": prefix, "weeks": int(f.week.max()), "small_cohort": len(small)}
    for tier in ["Small", "Boutique", "Independent", "MidTier", "Major"]:
        e = end.loc[initial.labelTier == tier]
        result[tier + "_closed_pct"] = 100 * e.status.isin(["Bankrupt", "Defunct"]).mean()
    result["small_curve"] = {}
    transitions = pd.read_csv(prefix + "-label-tier-transitions.csv")
    promotions = transitions[(transitions.direction == "promotion") & transitions.labelId.isin(small)]
    first_promotion = promotions.groupby("labelId").week.min()
    first_closure = f[f.labelId.isin(small) & f.status.isin(["Bankrupt", "Defunct"])].groupby("labelId").week.min()
    before_promotion = first_closure < first_promotion.reindex(first_closure.index).fillna(float("inf"))
    result["Small_exit_before_promotion_pct"] = 100 * before_promotion.sum() / max(1, len(small))
    for w in [13, 26, 52, 104, 156, 208, 260]:
        if w > f.week.max():
            continue
        e = f[(f.week <= w) & f.labelId.isin(small)].groupby("labelId").last()
        result["small_curve"][w] = int(e.status.isin(["Bankrupt", "Defunct"]).sum())
    s = f[f.labelId.isin(small)]
    e = end.loc[small]
    live = e[~e.status.isin(["Bankrupt", "Defunct", "Acquired"])]
    result["small_survivor_median_cash"] = float(live.cashReserves.median())
    result["small_survivors_promoted"] = int((live.labelTier != "Small").sum())
    result["small_pressing_over_gross"] = float(s.weeklyPressing.sum() / max(1, s.weeklyGross.sum()))
    path = Path(prefix + "-release-outcomes.csv")
    if path.exists() and path.stat().st_size:
        o = pd.read_csv(path)
        o = o[(o.week <= weeks) & o.labelId.isin(small) & (o.format == "Single")]
        result["small_retired_singles"] = len(o)
        result["small_single_median_units"] = float(o.pressingBookSold.median())
        result["small_single_loss_pct"] = float(100 * (o.realizedNet <= 0).mean())
    for suffix in ["concentration", "decade-annual-rollup"]:
        path = Path(prefix + "-" + suffix + ".csv")
        if path.exists() and path.stat().st_size:
            table = pd.read_csv(path)
            table = table[table.year < 1960 + weeks // 52] if weeks >= 52 else table.iloc[:0]
            columns = [c for c in ["year", "singleUnits", "albumUnits", "albumDecisionShare", "majorFamilyChartShare", "c4ChartShare", "firmsCharting", "chartEntries"] if c in table]
            result[suffix] = table[columns].to_dict("records")
    return result


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("prefixes", nargs="+")
    parser.add_argument("--weeks", type=int, default=260)
    args = parser.parse_args()
    print(json.dumps([arm(p, args.weeks) for p in args.prefixes], indent=2))
