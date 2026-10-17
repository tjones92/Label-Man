"""Read-only financial and chart diagnostics for the historical research memo.

Usage: python SimTools/analyze_historical_calibration_snapshot.py PREFIX [PREFIX ...]
Writes nothing unless stdout is explicitly redirected. No simulation is run.
"""
import argparse
import json

import pandas as pd


def quantiles(series):
    return {name: float(series.quantile(q)) for name, q in [("p10", .1), ("median", .5), ("p90", .9)]}


def analyze(prefix):
    finance = pd.read_csv(prefix + "-label-finance.csv")
    first = finance.groupby("labelId", sort=False).first()
    ids = first[(first.week == 1) & first.labelTier.eq("Small")].index
    initial = first.loc[ids]
    result = {"prefix": prefix, "startingSmall": len(ids),
              "week1CashAfterFirstWeeklyActivity": quantiles(initial.cashReserves),
              "week1CashToMonthlyOverhead": quantiles(initial.cashReserves / initial.monthlyOverhead),
              "annual": {}}
    for year, group in finance[finance.labelId.isin(ids)].groupby("year"):
        end = group.groupby("labelId", sort=False).last()
        live = end[~end.status.isin(["Defunct", "Bankrupt", "Acquired"])]
        result["annual"][int(year)] = {"survivors": len(live),
            "stillSmall": int(live.labelTier.eq("Small").sum()),
            "cash": quantiles(live.cashReserves),
            "cashToMonthlyOverhead": quantiles(live.cashReserves / live.monthlyOverhead),
            "positiveRecentCashFlow": int(live.recentMonthlyCashFlow.gt(0).sum()),
            "recentMonthlyCashFlow": quantiles(live.recentMonthlyCashFlow),
            "outstandingWholesaleReceivables": float(live.outstandingWholesaleReceivables.sum())}
    # This is booked release contribution less production. It excludes marketing,
    # overhead, working-capital timing and invoice write-offs: never call it profit.
    outcomes = pd.read_csv(prefix + "-release-outcomes.csv")
    singles = outcomes[outcomes.labelId.isin(ids) & outcomes["format"].eq("Single") &
                       outcomes.memoryEligible.eq(True) & outcomes.sunkProductionCost.gt(0)]
    result["retiredCohortSingles"] = {"count": len(singles),
        "positiveBookedContributionMinusProduction": int(singles.realizedNet.gt(0).sum()),
        "bookedContributionMinusProduction": quantiles(singles.realizedNet),
        "limitation": "Retired, costed, memory-eligible Singles only; not after-marketing/overhead cash profit"}
    records = pd.read_csv(prefix + "-records.csv", usecols=[
        "year", "currentPosition", "unitsThisWeek", "unsampledChartPoints", "weeksSinceRelease"])
    result["airplay"] = {}
    for year, group in records.groupby("year"):
        top = group[group.currentPosition.between(1, 40) & group.unsampledChartPoints.gt(0)]
        share = (1 - top.unitsThisWeek / top.unsampledChartPoints).clip(0, 1)
        ages = {}
        for label, lo, hi in [("age1to4", 1, 4), ("age5to8", 5, 8), ("age9to12", 9, 12), ("age13plus", 13, 99999)]:
            selected = top[top.weeksSinceRelease.between(lo, hi)]
            ages[label] = {"recordWeeks": len(selected), "medianShare":
                float((1 - selected.unitsThisWeek / selected.unsampledChartPoints).clip(0, 1).median()) if len(selected) else None}
        ranks = {}
        for label, lo, hi in [("rank1", 1, 1), ("rank1to10", 1, 10), ("rank11to40", 11, 40)]:
            selected = top[top.currentPosition.between(lo, hi)]
            ranks[label] = {"medianShare": float((1 - selected.unitsThisWeek / selected.unsampledChartPoints).clip(0, 1).median()),
                           "aggregatePointShare": float(1 - selected.unitsThisWeek.sum() / selected.unsampledChartPoints.sum())}
        result["airplay"][int(year)] = {"top40RecordWeeks": len(top), "medianRecordWeekShare": float(share.median()),
            "aggregatePointShare": float(1 - top.unitsThisWeek.sum() / top.unsampledChartPoints.sum()),
            "byReleaseAge": ages, "byRank": ranks}
    return result


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("prefix", nargs="+")
    args = parser.parse_args()
    print(json.dumps([analyze(prefix) for prefix in args.prefix], indent=2, allow_nan=False))
