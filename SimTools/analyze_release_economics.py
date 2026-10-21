"""Check A4 gate decisions and attributed marketing against finalized all-in outcomes.

Streaming, standard library. A short run validates bookkeeping and whether the gate
is reachable; its young LPs cannot establish lifetime loss rates or balance acceptance.
"""
import argparse
import csv
import json
import math
import os
from collections import Counter, defaultdict
from pathlib import Path


def rows(prefix, suffix):
    with Path(f"{prefix}-{suffix}.csv").open(newline="", encoding="utf-8-sig") as stream:
        yield from csv.DictReader(stream)


def summarize(prefix):
    decisions, passed, reasons = Counter(), Counter(), Counter()
    cohort = defaultdict(Counter)
    for row in rows(prefix, "release-profit-gates"):
        if row["commit"] not in ("true", "false"):
            raise ValueError(f"Invalid gate approval value: {prefix} {row}")
        selected, album = float(row["selectedProfit"]), float(row["albumProfit"])
        actual = row["commit"] == "true"
        expected = math.isfinite(selected) and selected > 0 and (row["strategy"] == "OrphanSingle" or math.isfinite(album) and album > 0)
        if actual != expected:
            raise ValueError(f"Gate decision disagrees with recorded profit: {prefix} {row}")
        decisions[row["strategy"]] += 1
        if not actual:
            passed[row["strategy"]] += 1
            reasons["selected-profit" if selected <= 0 else "physical-LP-profit"] += 1
        key = f"{row['careerState']}|{'no-resolved-single' if int(row['resolvedSingles']) == 0 else 'prior-single'}"
        cohort[key]["decisions"] += 1
        cohort[key]["waits"] += not actual
    campaigns, costs = Counter(), {}
    for row in rows(prefix, "release-campaign-costs"):
        for field in ("productionCost", "marketingCost", "releaseTimeExpectedNet"):
            if not math.isfinite(float(row[field])):
                raise ValueError(f"Nonfinite launch value: {prefix} {row}")
        if min(float(row["productionCost"]), float(row["marketingCost"])) < 0:
            raise ValueError(f"Negative booked cost: {prefix} {row}")
        campaigns["launchEvents"] += 1
        campaigns["production"] += float(row["productionCost"])
        campaigns["marketing"] += float(row["marketingCost"])
        costs[row["recordId"]] = float(row["marketingCost"])
    # All direct singles, including week one, must have a known campaign cost.
    # LP commitments may still be awaiting their drop and campaign at run end.
    singles = [r["recordId"] for r in rows(prefix, "release-strategy") if r["chosenFormat"] == "Single"]
    missing = [rid for rid in singles if rid not in costs]
    if missing:
        raise ValueError(f"Funded singles lack attributed campaign costs: {prefix}: {missing[:10]} ({len(missing)} total)")
    # --record-campaign-spend (audit B5): follow-up tranches are attributed after launch.
    follow_ups = Counter()
    if os.path.exists(f"{prefix}-record-campaigns.csv"):
        for row in rows(prefix, "record-campaigns"):
            follow_ups[row["recordId"]] += float(row["spend"])
    campaigns["followUpMarketing"] = sum(follow_ups.values())
    outcomes = defaultdict(Counter)
    for row in rows(prefix, "release-all-in-outcomes"):
        before = float(row["lifetimeLabelNet"]) - float(row["sunkProductionCost"])
        marketing, net = float(row["sunkMarketingCost"]), float(row["realizedNet"])
        tolerance = max(.01, abs(net) * .000001)
        if abs(net - (before - marketing)) > tolerance:
            raise ValueError(f"Outcome cost identity failed: {prefix} {row}")
        expected = costs.get(row["recordId"], math.nan) + follow_ups[row["recordId"]]
        exact = follow_ups[row["recordId"]] == 0
        if row["recordId"] not in costs or (marketing != expected if exact else abs(marketing - expected) > max(.01, expected * 1e-6)):
            raise ValueError(f"Outcome lacks matching launch campaign cost: {prefix} {row}")
        group = outcomes[row["format"]]
        group["finals"] += 1
        group["lossBeforeMarketing"] += before < 0
        group["lossAfterMarketing"] += net < 0
        group["marketing"] += marketing
    return {"run": str(prefix), "decisions": decisions, "waits": passed, "waitReasons": reasons,
            "directSingleCoverage": {"funded": len(singles), "missingCampaignCost": len(missing)},
            "cohorts": dict(cohort), "campaigns": campaigns, "finalOutcomes": dict(outcomes), "pass": bool(decisions)}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("prefixes", nargs="+", type=Path)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    report = [summarize(prefix) for prefix in args.prefixes]
    args.output.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report, indent=2))
    return 0 if all(run["pass"] for run in report) else 1


if __name__ == "__main__":
    raise SystemExit(main())
