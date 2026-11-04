"""Single-only regional timing, stratified by initial distribution footprint and censoring.

Usage: python SimTools/analyze_regional_timing_detail.py PREFIX [PREFIX ...]
The interior-release sensitivity excludes prewarm records and releases within 26
weeks of the run end. Nationwide campaigns and regional releases are reported separately.
"""
import argparse
import json

import pandas as pd

from analyze_regional_sync import settlement


def peak_stats(frame, fraction=0):
    totals = frame.groupby("recordId", observed=True).finalCleared.sum()
    by_market = frame.groupby(["recordId", "regionId"], observed=True).finalCleared.sum().reset_index()
    by_market = by_market[(by_market.finalCleared > 0) &
                          (by_market.finalCleared >= by_market.recordId.map(totals).astype(float) * fraction)]
    sold = frame[frame.finalCleared > 0].merge(by_market[["recordId", "regionId"]], on=["recordId", "regionId"])
    peaks = sold.loc[sold.groupby(["recordId", "regionId"], observed=True).finalCleared.idxmax()]
    spread = peaks.groupby("recordId", observed=True).week.agg(["min", "max", "size"])
    spread = spread[spread["size"] >= 2]
    delta = spread["max"] - spread["min"]
    return {"records": len(spread), "median": float(delta.median()) if len(delta) else None,
            "shareGe4": float(delta.ge(4).mean()) if len(delta) else None,
            "shareGe8": float(delta.ge(8).mean()) if len(delta) else None,
            "meanSalesMarkets": float(spread["size"].mean()) if len(spread) else None}


def analyze(prefix):
    national = pd.read_csv(prefix + "-completed-week-settlement.csv", usecols=["recordId", "format"])
    single_ids = set(national.loc[national["format"] == "Single", "recordId"])
    path = prefix + "-completed-week-settlement-regional.csv"
    legacy = settlement(path, 50000)
    correct = settlement(path, 50000, single_ids)
    data = pd.read_csv(path, usecols=["week", "recordId", "regionId", "finalCleared"],
                       dtype={"recordId": "category", "regionId": "category"})
    data = data[data.recordId.isin(single_ids)]
    metadata = pd.read_csv(prefix + "-records.csv", usecols=[
        "week", "recordId", "weeksSinceRelease", "coveredRegionCount", "labelTier"])
    first = metadata.groupby("recordId", sort=False).first()
    max_week = int(data.week.max())
    units = data.groupby("recordId", observed=True).finalCleared.sum()
    interior = first[(first.weeksSinceRelease == 1) & (first.week <= max_week - 26)]
    interior = interior[interior.index.isin(units[units >= 50000].index)]
    groups = {"interiorAllSingles": interior.index,
              "interiorNationwide": interior[interior.coveredRegionCount == 7].index,
              "interiorRegional": interior[interior.coveredRegionCount < 7].index}
    for tier in ("Small", "Boutique", "Independent", "MidTier", "Major"):
        groups["interiorRegional" + tier] = interior[(interior.coveredRegionCount < 7) & (interior.labelTier == tier)].index
    out = {"prefix": prefix, "legacyMixedFormats": legacy, "singleOnly": correct, "groups": {}}
    for group, ids in groups.items():
        selected = data[data.recordId.isin(ids)]
        out["groups"][group] = {"allPositiveMarkets": peak_stats(selected),
                                "marketsWithAtLeast1PercentOfRecordSales": peak_stats(selected, .01)}
    return out


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("prefix", nargs="+")
    args = parser.parse_args()
    print(json.dumps([analyze(prefix) for prefix in args.prefix], indent=2))
