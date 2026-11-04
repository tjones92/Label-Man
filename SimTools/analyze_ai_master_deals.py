"""Audit pickup volume, chains and evidence. Separate initialization (week 0) from the run window."""
import argparse
import collections
import json
from pathlib import Path


def read_events(log):
    events = []
    evidence = {}
    for line in Path(log).read_text(encoding="utf-8-sig").splitlines():
        if not line.startswith(("AI_MASTER_DEAL ", "AI_MASTER_PICKUP_EVIDENCE ")):
            continue
        row = dict(piece.split("=", 1) for piece in line.split()[1:] if "=" in piece)
        if line.startswith("AI_MASTER_PICKUP_EVIDENCE "):
            evidence[(row["week"], row["record"])] = row
        elif row.get("route") == "pickup":
            events.append(row)
    return events, evidence


def summarize(log, weeks):
    events, evidence = read_events(log)
    pickups = [row for row in events if 1 <= int(row["week"]) <= weeks]
    titles = collections.Counter(row["record"] for row in pickups)
    prior = {}
    chained = 0
    for row in events:
        previous = prior.get(row["record"])
        if row in pickups and previous and previous["kind"] == "lease" and previous["buyer"] == row["seller"]:
            chained += 1
        prior[row["record"]] = row
    ev = [evidence[(r["week"], r["record"])] for r in pickups if (r["week"], r["record"]) in evidence]
    funding = [r for r in ev if "fundingNeed" in r and "freeCash" in r]
    return {
        "log": str(log), "weeks": weeks, "initializationPickups": sum(int(r["week"]) == 0 for r in events),
        "pickups": len(pickups), "uniqueTitles": len(titles), "repeatTitlePickups": sum(titles.values()) - len(titles),
        "onwardTransfersDuringLease": chained,
        "sellerToBuyer": dict(collections.Counter(r["sellerTier"] + "->" + r["buyerTier"] for r in pickups)),
        "kinds": dict(collections.Counter(r["kind"] for r in pickups)),
        "withEvidenceTelemetry": len(ev), "withoutRecentHit": sum(int(r["hitMarkets"]) == 0 for r in ev) if ev else None,
        "medianWeeklyUnits": sorted(int(r["units"]) for r in ev)[len(ev) // 2] if ev else None,
        "withFundingTelemetry": len(funding),
        "sellerCouldFundNextRun": sum(float(r["fundingNeed"]) <= max(0, float(r["freeCash"])) for r in funding) if funding else None,
    }


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("logs", nargs="+")
    parser.add_argument("--weeks", type=int, default=105)
    args = parser.parse_args()
    print(json.dumps([summarize(log, args.weeks) for log in args.logs], indent=2))
