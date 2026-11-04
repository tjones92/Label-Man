"""Control vs --dealer-price-pressing treatment: Small attrition, the pressing book and market guards.

Usage: python SimTools/analyze_pressing_economics.py SimLogs/<control-prefix> SimLogs/<treatment-prefix>
Read-only. Survival rows use the starting-Small cohort (Small at week 1); a promotion out of Small is a survivor.
Market guards (Major-family chart share, airplay, #1s) live in analyze_regional_guards.py.
"""
import argparse
import json

import numpy as np
import pandas as pd

CLOSED = ["Defunct", "Bankrupt"]


def q(series):
    return {k: round(float(series.quantile(v)), 1) for k, v in [("p10", .1), ("median", .5), ("p90", .9)]} if len(series) else None


def arm(prefix):
    fin = pd.read_csv(prefix + "-label-finance.csv", usecols=lambda c: c in [
        "week", "year", "labelId", "labelTier", "status", "cashReserves", "weeklyPressing"])
    first = fin.groupby("labelId", sort=False).first()
    ids = first[(first.week == 1) & first.labelTier.eq("Small")].index
    out = {"startingSmall": int(len(ids)), "annual": {}}
    cohort = fin[fin.labelId.isin(ids)]
    for year, group in cohort.groupby("year"):
        end = group.sort_values("week").groupby("labelId").last()
        live = end[~end.status.isin(CLOSED + ["Acquired"])]
        out["annual"][int(year)] = {
            "closedCumulative": int(end.status.isin(CLOSED).sum()),
            "closedShare": round(float(end.status.isin(CLOSED).mean()), 4),
            "acquired": int(end.status.eq("Acquired").sum()),
            "survivorsStillSmall": int(live.labelTier.eq("Small").sum()),
            "survivorsPromoted": int((~live.labelTier.eq("Small")).sum()),
            "survivorCash": q(live.cashReserves)}
    last = fin.sort_values("week").groupby("labelId").last()
    out["allClosuresByFirstTier"] = {str(k): int(v) for k, v in
                                     first.loc[last.status.isin(CLOSED)[lambda s: s].index, "labelTier"].value_counts().items()}

    settle = pd.read_csv(prefix + "-completed-week-settlement.csv", usecols=lambda c: c in [
        "year", "labelTier", "format", "totalUnits", "gross", "labelNet", "pressingCost"])
    units = settle.groupby(["year", "format"]).totalUnits.sum().unstack(fill_value=0)
    tier = settle.groupby(["year", "labelTier"]).totalUnits.sum().unstack(fill_value=0)
    out["market"] = {int(y): {"units": int(units.loc[y].sum()),
                              "albumUnitShare": round(float(units.loc[y].get("Album", 0) / units.loc[y].sum()), 4),
                              "smallUnitShare": round(float(tier.loc[y].get("Small", 0) / tier.loc[y].sum()), 4),
                              "majorUnitShare": round(float(tier.loc[y].get("Major", 0) / tier.loc[y].sum()), 4)}
                     for y in units.index}
    if "pressingCost" in settle:
        out["pressingSpendByTier"] = {str(k): round(float(v), 0) for k, v in settle.groupby("labelTier").pressingCost.sum().items()}

    outcomes = pd.read_csv(prefix + "-release-outcomes.csv")
    small = outcomes[outcomes.labelId.isin(ids)]
    out["startingSmallRetired"] = {}
    for fmt, g in small.groupby("format"):
        r = {"count": int(len(g)), "lossShare": round(float(g.realizedNet.le(0).mean()), 4), "realizedNet": q(g.realizedNet)}
        if "unitsPressed" in g and g.unitsPressed.gt(0).any():
            b = g[g.unitsPressed > 0]
            r["unsoldShareOfPressed"] = round(float(1 - b.pressingBookSold.sum() / b.unitsPressed.sum()), 4)
            r["medianUnsoldShare"] = round(float((1 - b.pressingBookSold / b.unitsPressed).median()), 4)
        out["startingSmallRetired"][str(fmt)] = r
    if "unitsPressed" in outcomes and outcomes.unitsPressed.gt(0).any():
        b = outcomes[outcomes.unitsPressed > 0]
        out["allRetiredUnsoldShareOfPressed"] = round(float(1 - b.pressingBookSold.sum() / b.unitsPressed.sum()), 4)

    rel = outcomes.assign(small=outcomes.labelId.isin(ids)).groupby(["year", "small"]).size().unstack(fill_value=0)
    out["retiredReleasesPerYear"] = {int(y): {"startingSmall": int(r.get(True, 0)), "others": int(r.get(False, 0))}
                                     for y, r in rel.iterrows()}
    return out


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("control")
    parser.add_argument("treatment")
    args = parser.parse_args()
    result = {"control": args.control, "treatment": args.treatment,
              "arms": {"control": arm(args.control), "treatment": arm(args.treatment)}}
    print(json.dumps(result, indent=1, default=lambda o: o.item() if isinstance(o, np.generic) else str(o)))


if __name__ == "__main__":
    main()
