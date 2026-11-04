"""Paired founder-compensation screen: survival, owner income and annual chart guards.

Run analyze_boutique_closures.py first into --details, with arms named control/pay.
Full-run sales include the five extra weeks; annual chart guards use 1960-64 only.
Genre deltas compare each seed's observed shares, not a new historical target.
"""
import argparse
import json
import statistics
from pathlib import Path

import pandas as pd

from analyze_boutique_closures import SPECIALIST_ARCHETYPES
from analyze_offchart_screen import path, screen


def guards(tag, seed, weeks):
    p = lambda name: path(tag, seed, name, weeks)
    records = pd.read_csv(p("records"), usecols=["year", "week", "recordId", "artistId", "currentPosition"])
    records = records[records.currentPosition.between(1, 100)]
    first = records.sort_values("week").groupby("recordId").head(1)
    life = pd.read_csv(p("lifecycles"))
    life = life[~life.leftCensoredAtRunStart & life.weeksOnChart.gt(0)]
    shape = pd.read_csv(p("genre-decade-shape"))
    end = pd.read_csv(p("year-end-hot100"))
    units = pd.read_csv(p("label-sales-weekly"))
    annual = {}
    for year in range(1960, 1965):
        r = records[records.year.eq(year)]
        if r.week.nunique() != 52 or len(r) != 5200 or r.duplicated(["week", "currentPosition"]).any():
            raise ValueError("Incomplete or repeated annual Hot 100 slots")
        one = r[r.currentPosition.eq(1)]
        genre = shape[shape.year.eq(year)]
        slots = end[end.year.eq(year)].set_index("genre").yearEndSlots
        if int(slots.sum()) != 100:
            raise ValueError("Year-end genre slots do not sum to 100")
        u = units[units.year.eq(year)].groupby("format").units.sum()
        annual[year] = {"hot100Acts": int(r.artistId.nunique()),
            "hot100Entries": int((first.year.eq(year) & first.week.gt(1)).sum()),
            "meanDebutPosition": float(first.loc[first.year.eq(year) & first.week.gt(1), "currentPosition"].mean()),
            "numberOneRecords": int(one.recordId.nunique()), "numberOneMeanTenure": 52 / one.recordId.nunique(),
            "lpLiveUnitShare": float(u.get("Album", 0) / u.sum()),
            "genreMarketShare": genre.set_index("genre").marketUnitsShare.to_dict(),
            "genreChartWeekShare": genre.set_index("genre").chartWeekShare.to_dict(),
            "yearEndSlots": slots.to_dict()}
    return {"closedUncensoredMeanChartLife": float(life.weeksOnChart.mean()), "annual": annual}


def income_and_survival(labels):
    boutique = labels[labels.initialTier.eq("Boutique")]
    specialist = boutique[boutique.archetype.isin(SPECIALIST_ARCHETYPES)]
    survivors = boutique[~boutique.cause.isin(["Bankruptcy", "Dormancy", "Acquired"])]
    return {"specialistProxyLabels": len(specialist),
            "specialistProxyClosedPct": float(100 * specialist.cause.isin(["Bankruptcy", "Dormancy"]).mean()),
            "boutiqueMedianObservedMonthlyFounderSalary": float((boutique.total_ownerSalary / boutique.observedMonths).median()),
            "boutiqueSurvivorMedianObservedMonthlyFounderSalary": float((survivors.total_ownerSalary / survivors.observedMonths).median()),
            "boutiqueTotalFounderSalary": float(boutique.total_ownerSalary.sum()),
            "boutiqueNetOwnerDistributionsOut": float(-boutique.total_ownerTransfersNet.sum()),
            "boutiqueSurvivorMedianCash": float(survivors.lastObservedCash.median()),
            "boutiqueSurvivorMedianFinalMandatoryOverhead": float(survivors.lastObservedMandatoryOverhead.median()),
            "boutiqueSurvivorMedianFinalSixMonthLaborAndPremises": float(survivors.lastSixMonthLaborAndPremises.median())}


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--tag", default="bcs-v1-pay")
    parser.add_argument("--control", default="dfs-v1-founder")
    parser.add_argument("--seeds", nargs="+", type=int, default=[1001, 2002, 3003])
    parser.add_argument("--weeks", type=int, default=265)
    parser.add_argument("--details", type=Path, default=Path("SimLogs/bcs-v1/closures"))
    parser.add_argument("--output", type=Path, default=Path("SimLogs/bcs-v1/screen.json"))
    args = parser.parse_args()
    runs = {}
    for arm, tag in [("control", args.control), ("pay", args.tag)]:
        for seed in args.seeds:
            labels = pd.read_csv(args.details / f"{arm}-{seed}-labels.csv")
            runs[f"{arm}/{seed}"] = {"economy": screen(tag, seed, args.weeks),
                "ownerAndSurvival": income_and_survival(labels), "chartGuards": guards(tag, seed, args.weeks)}
    pairs = {}
    for seed in args.seeds:
        ctl, pay = runs[f"control/{seed}"], runs[f"pay/{seed}"]
        shifts = {}
        for year in range(1960, 1965):
            a, b = ctl["chartGuards"]["annual"][year], pay["chartGuards"]["annual"][year]
            shifts[year] = {}
            for name, factor in [("genreMarketShare", 100), ("genreChartWeekShare", 100), ("yearEndSlots", 1)]:
                keys = a[name].keys() | b[name].keys()
                shifts[year][name + "L1Delta"] = factor * sum(abs(a[name].get(k, 0) - b[name].get(k, 0)) for k in keys)
        pairs[seed] = {"annualGenreShifts": shifts}
    summary = {}
    for section in ("economy", "ownerAndSurvival"):
        summary[section] = {}
        for key in runs[f"control/{args.seeds[0]}"][section]:
            a = [runs[f"control/{s}"][section][key] for s in args.seeds]
            b = [runs[f"pay/{s}"][section][key] for s in args.seeds]
            diff = [y - x for x, y in zip(a, b)]
            summary[section][key] = {"control": statistics.mean(a), "pay": statistics.mean(b),
                "pairedDifference": statistics.mean(diff),
                "pairedSE": statistics.stdev(diff) / len(diff) ** .5 if len(diff) > 1 else None}
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps({"runs": runs, "pairs": pairs, "summary": summary}, indent=2, allow_nan=False), encoding="utf-8")
    print(json.dumps(summary, indent=2, allow_nan=False))


if __name__ == "__main__":
    main()
