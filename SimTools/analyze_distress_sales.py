"""Distress master sales: who sold, to whom, at what price, and whether the seller then survived.

Parses the AI_MASTER_DEAL route=distress and AI_MASTER_DISTRESS_EVIDENCE console lines, then joins
each seller to its exit from analyze_boutique_closures.py output (arm name "pay"). Survival after a
sale is observed, not a counterfactual: a seller alive at the end might have survived anyway.
"""
import argparse
import json
import re
from pathlib import Path

import pandas as pd

DEAL = re.compile(r"AI_MASTER_DEAL week=(\d+) route=distress kind=sale seller=(\S+) sellerTier=(\S+) "
                  r"buyer=(\S+) buyerTier=(\S+) record=(\S+) amount=(\d+)")
EVIDENCE = re.compile(r"AI_MASTER_DISTRESS_EVIDENCE week=(\d+) record=(\S+) age=(-?\d+) position=(-?\d+) "
                      r"units=(\d+) totalUnits=(\d+) sellerCash=(-?\d+) need=(\d+) price=(\d+) receivables=(\d+)")


def sales(console, seed):
    deals, evidence = [], {}
    for line in console.read_text(encoding="utf-8", errors="replace").splitlines():
        if m := DEAL.search(line):
            deals.append({"seed": seed, "week": int(m[1]), "seller": m[2], "sellerTier": m[3], "buyer": m[4],
                          "buyerTier": m[5], "recordId": m[6], "price": float(m[7])})
        elif m := EVIDENCE.search(line):
            evidence[(int(m[1]), m[2])] = {"age": int(m[3]), "position": int(m[4]), "units": int(m[5]),
                "totalUnits": int(m[6]), "sellerCash": float(m[7]), "need": float(m[8]), "receivables": float(m[10])}
    for d in deals:
        d.update(evidence[(d["week"], d["recordId"])])
    return deals


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--tag", default="dms-v1-sale")
    parser.add_argument("--seeds", nargs="+", type=int, default=[1001, 2002, 3003])
    parser.add_argument("--weeks", type=int, default=265)
    parser.add_argument("--details", type=Path, default=Path("SimLogs/dms-v1/closures"))
    parser.add_argument("--output", type=Path, default=Path("SimLogs/dms-v1/distress-sales.csv"))
    args = parser.parse_args()
    rows = []
    for seed in args.seeds:
        console = Path("SimLogs") / args.tag / f"{args.tag}-default-{seed}-{args.weeks}-console.log"
        labels = pd.read_csv(args.details / f"pay-{seed}-labels.csv").set_index("labelId")
        for d in sales(console, seed):
            label = labels.loc[d["seller"]] if d["seller"] in labels.index else None
            exit_week = label.exitWeek if label is not None and pd.notna(label.exitWeek) else None
            d["startingSeller"] = label is not None
            d["initialTier"] = label.initialTier if label is not None else None
            d["sellerCause"] = label.cause if label is not None else None
            d["weeksToExit"] = int(exit_week - d["week"]) if exit_week is not None else None
            d["survivedToEnd"] = exit_week is None
            rows.append(d)
    d = pd.DataFrame(rows)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    d.to_csv(args.output, index=False)
    if d.empty:
        print(json.dumps({"sales": 0}))
        return
    observed = d[d.week <= args.weeks - 52]
    summary = {"sales": len(d), "bySeed": d.groupby("seed").size().to_dict(),
        "bySellerTier": d.groupby("sellerTier").size().to_dict(), "byBuyerTier": d.groupby("buyerTier").size().to_dict(),
        "startingSellers": int(d.startingSeller.sum()),
        "startingSellersByInitialTier": d[d.startingSeller].groupby("initialTier").size().to_dict(),
        "repeatSellers": int(d.groupby(["seed", "seller"]).size().gt(1).sum()),
        "priceMedian": float(d.price.median()), "needMedian": float(d.need.median()),
        "priceOverNeedMedian": float((d.price / d.need).median()),
        "receivablesOverOverdraftMedian": float((d.receivables / -d.sellerCash).median()),
        "chartingAtSale": int(d.position.between(1, 100).sum()), "top40AtSale": int(d.position.between(1, 40).sum()),
        "survivedToEnd": int(d.survivedToEnd.sum()),
        "salesWithAYearObserved": len(observed),
        "closedWithin26WeeksOfThose": int((observed.weeksToExit.notna() & observed.weeksToExit.le(26)).sum()),
        "closedWithin52WeeksOfThose": int((observed.weeksToExit.notna() & observed.weeksToExit.le(52)).sum())}
    summary = json.loads(json.dumps(summary, default=lambda v: v.item() if hasattr(v, "item") else str(v)))
    (args.output.with_suffix(".json")).write_text(json.dumps(summary, indent=2), encoding="utf-8")
    print(json.dumps(summary, indent=2))


if __name__ == "__main__":
    main()
