"""Regional-economy outcomes and the identity national units == sum(regional units).

Usage: python SimTools/analyze_regional_economy.py SimLogs/control SimLogs/treatment
Writes JSON to stdout. Only completed runs qualify as evidence. Closure events are
matched to immutable label IDs through their closure week, so reused names cannot
assign a dead label's bankruptcy to its successor.
"""
import argparse
import json
import re
from pathlib import Path

import pandas as pd


def analyze(prefix):
    log = Path(prefix + "-console.log").read_text(encoding="utf-8", errors="replace")
    completion = re.search(r"CHART_AUDIT_COMPLETE run=(\S+) weeks=(\d+)", log)
    if not completion:
        raise ValueError(f"Incomplete simulation: {prefix}")
    weeks = int(completion[2])
    finance = pd.read_csv(prefix + "-label-finance.csv", usecols=[
        "week", "labelId", "labelName", "labelTier", "status", "cashReserves"])
    first = finance.groupby("labelId", sort=False).first()
    last = finance.groupby("labelId", sort=False).last()
    closed = finance[finance.status == "Defunct"].groupby("labelId", sort=False).first().reset_index()
    bankrupt_ids = set()
    event_week = 1
    for line in log.splitlines():
        event = re.search(r"\[LabelManager\] (.*) has closed\. Reason: ([^.]+)", line)
        if event:
            matches = closed[(closed.labelName == event[1]) & (closed.week == event_week)]
            if len(matches) != 1:
                raise ValueError(f"Cannot identify closure at week {event_week}: {event[1]}")
            if event[2] == "Bankruptcy":
                bankrupt_ids.add(matches.iloc[0].labelId)
        if line.startswith("Advanced to Friday,"):
            event_week += 1
    if event_week != weeks + 1:
        raise ValueError("Unexpected audit week sequence in console log")
    first["bankrupt"] = first.index.isin(bankrupt_ids)
    duplicate_names = first[first.labelName.duplicated(False)].labelName.unique().tolist()
    national = pd.read_csv(prefix + "-completed-week-settlement.csv", usecols=[
        "week", "recordId", "labelId", "labelTier", "format", "totalUnits", "labelNet",
        "bookedCount", "auditedCount"])
    keys = ["week", "recordId"]
    if national.duplicated(keys).any():
        raise ValueError("Duplicate national settlements")
    regional = []
    invalid_regional_rows = 0
    for chunk in pd.read_csv(prefix + "-completed-week-settlement-regional.csv", usecols=[
            "week", "recordId", "finalCleared", "localCleared", "spilloverCleared", "inventoryMovement"],
            chunksize=200000):
        invalid_regional_rows += int(((chunk.finalCleared < 0) |
            (chunk.finalCleared != chunk.localCleared + chunk.spilloverCleared) |
            (chunk.finalCleared != chunk.inventoryMovement)).sum())
        regional.append(chunk.groupby(keys).finalCleared.sum())
    regions = pd.concat(regional).groupby(level=[0, 1]).sum()
    totals = national.set_index(keys).totalUnits
    comparison = pd.concat({"national": totals, "regions": regions}, axis=1)
    mismatches = int((comparison.isna().any(axis=1) | (comparison.national != comparison.regions)).sum())
    singles = national[national["format"] == "Single"]
    per_label = singles.groupby("labelId").totalUnits.sum()
    first["singleUnits"] = per_label.reindex(first.index).fillna(0)
    first["singleNet"] = singles.groupby("labelId").labelNet.sum().reindex(first.index).fillna(0)
    out = {
        "prefix": prefix, "weeks": weeks,
        "closures": len(re.findall(r"\[LabelManager\] .* has closed\.", log)),
        "bankruptcies": len(re.findall(r"has closed\. Reason: Bankruptcy", log)),
        "singleUnits": int(singles.totalUnits.sum()),
        "unitsBySettlementTier": {str(k): int(v) for k, v in singles.groupby("labelTier").totalUnits.sum().items()},
        "netBySettlementTier": {str(k): float(v) for k, v in singles.groupby("labelTier").labelNet.sum().items()},
        "initialTierCohorts": {},
        "conservation": {"nationalRegionalMismatches": mismatches, "invalidRegionalRows": invalid_regional_rows,
                         "invalidBookingCounts": int(((national.bookedCount != 1) | (national.auditedCount != 1)).sum())},
        "duplicateLabelNames": duplicate_names,
    }
    for tier in ("Small", "Independent", "Boutique"):
        labels = first[first.labelTier == tier]
        sellers = labels[labels.singleUnits > 0]
        alive = last.reindex(sellers.index)
        alive = alive[alive.status != "Defunct"]
        out["initialTierCohorts"][tier] = {
            "labels": len(labels), "sellers": len(sellers),
            "bankruptSellers": int(sellers.bankrupt.sum()),
            "bankruptNonSellers": int(labels[labels.singleUnits == 0].bankrupt.sum()),
            "singleUnits": int(sellers.singleUnits.sum()),
            "singleNet": float(sellers.singleNet.sum()),
            "medianSellerUnits": float(sellers.singleUnits.median()) if len(sellers) else None,
            "medianSellerSingleNet": float(sellers.singleNet.median()) if len(sellers) else None,
            "survivingSellers": len(alive),
            "medianSurvivorCash": float(alive.cashReserves.median()) if len(alive) else None,
        }
    return out


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("prefix", nargs="+")
    args = parser.parse_args()
    print(json.dumps([analyze(prefix) for prefix in args.prefix], indent=2))
