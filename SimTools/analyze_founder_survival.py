"""Closure causes, inactive survivors and annual Major singles shares in a founder survival audit.

Arguments are the same NAME=TAG:SEED,... specs as analyze_offchart_screen.py.
Annual off-chart volume is reconstructed from the configured weekly pool only when
all recipients have initial Major tier and the reconstruction reconciles exactly.
"""
import argparse
import json
import struct
from pathlib import Path

import pandas as pd

from analyze_offchart_screen import collect, label_tiers, path, screen, show, unquote


def detail(tag, seed, weeks, base_share):
    p = lambda stream: path(tag, seed, stream, weeks)
    tier = label_tiers(p)
    finance = pd.read_csv(p("label-finance"), usecols=[
        "week", "year", "labelId", "status", "cashReserves", "exitReason", "founderQuietMonths",
        "founderLastReleasePeriod", "monthlyOverhead"])
    finance = finance.sort_values("week")
    start = finance[finance.week == finance.week.min()].labelId.unique()
    end = finance.groupby("labelId").tail(1).set_index("labelId")
    exits = finance[finance.status.isin(["Bankrupt", "Defunct"])].groupby("labelId").head(1)
    if not exits.exitReason.isin(["Bankruptcy", "Dormancy"]).all():
        raise ValueError("Closed labels have missing or unrecognized causes")
    dormant = exits[exits.exitReason == "Dormancy"]
    if ((dormant.cashReserves < 0) | (dormant.founderQuietMonths < 12)).any():
        raise ValueError("Dormancy violates solvency or the quiet-year grace")
    result = {"seed": seed, "weeks": weeks, "dormancyEvents": len(dormant), "cohorts": {}}
    clock_measured = bool(finance.founderLastReleasePeriod.ge(0).any())
    for t in ("Small", "Boutique", "Independent"):
        ids = [i for i in start if tier.get(i) == t]
        cohort = end.loc[ids]
        closed = cohort.status.isin(["Bankrupt", "Defunct"])
        live = cohort[~cohort.status.isin(["Bankrupt", "Defunct", "Acquired"])]
        result["cohorts"][t] = {
            "startingLabels": len(ids), "closed": int(closed.sum()),
            "bankrupt": int((closed & cohort.exitReason.eq("Bankruptcy")).sum()),
            "dormant": int((closed & cohort.exitReason.eq("Dormancy")).sum()),
            "closedPct": float(100 * closed.mean()),
            "survivorCashMedian": float(live.cashReserves.median()),
            "survivorOverheadMedian": float(live.monthlyOverhead.median()),
            "trackedQuietYearSurvivors": int(live.founderQuietMonths.ge(12).sum()) if clock_measured else None,
        }
    sales = pd.read_csv(p("label-sales-weekly"))
    singles = sales[sales.format == "Single"].copy()
    singles["initialTier"] = singles.labelId.map(tier)
    off = pd.read_csv(p("offchart-singles"))
    only_major_recipients = off.labelId.map(tier).eq("Major").all()
    weekly = singles.groupby(["week", "year"], as_index=False).units.sum()
    # The allocator multiplies by its parsed float32 share promoted to double.
    configured_share = struct.unpack("f", struct.pack("f", base_share))[0]
    weekly["baseUnits"] = (weekly.units * configured_share).round().astype("int64")
    exact_base = only_major_recipients and int(weekly.baseUnits.sum()) == int(off.units.sum())
    result["annualBaseReconciled"] = bool(exact_base)
    result["annualSingles"] = {}
    for year, group in singles.groupby("year"):
        total = int(group.units.sum())
        major = int(group.loc[group.initialTier.eq("Major"), "units"].sum())
        annual_base = int(weekly.loc[weekly.year.eq(year), "baseUnits"].sum()) if exact_base else None
        result["annualSingles"][int(year)] = {
            "weeks": int(group.week.nunique()), "liveUnits": total, "majorLiveUnits": major,
            "majorLiveSharePct": 100 * major / max(1, total), "baseUnits": annual_base,
            "majorShareInclBasePct": 100 * (major + annual_base) / (total + annual_base) if exact_base else None,
        }
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("arms", nargs="+")
    parser.add_argument("--weeks", type=int, default=265)
    parser.add_argument("--base-share", type=float, default=.12)
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()
    show(collect(args.arms, args.weeks, screen), "weekly-owner screen")
    details = {}
    for spec in args.arms:
        name, runs = spec.split("=", 1)
        for run in runs.split(","):
            tag, seed = run.split(":")
            details[f"{name}/{seed}"] = detail(tag, int(seed), args.weeks, args.base_share)
    payload = json.dumps(details, indent=2)
    print(payload)
    if args.output:
        args.output.write_text(payload, encoding="utf-8")


if __name__ == "__main__":
    main()
