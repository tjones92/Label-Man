"""Matched-horizon economy review. Partial finance streams are rejected.

python -I SimTools/analyze_economy_retune.py PREFIX ... --weeks 260 --output SimLogs/review.json
The 20-35% closure band is a design target, not a historical confidence interval.
Cash and chart comparisons are descriptive; no new acceptance tolerance is inferred.
"""
import argparse
import csv
import json
import statistics
from collections import Counter, defaultdict
from pathlib import Path

CLOSED = {"Bankrupt", "Defunct"}
INACTIVE = CLOSED | {"Acquired"}
TIERS = ("Small", "Boutique", "Independent", "MidTier", "Major")


def rows(prefix, suffix):
    path = Path(f"{prefix}-{suffix}.csv")
    if path.exists():
        with path.open(encoding="utf-8-sig", newline="") as handle:
            yield from csv.DictReader(handle)


def median(values):
    return round(statistics.median(values), 2) if values else None


def analyze(prefix, weeks):
    initial = {r["labelId"]: r["initialTier"] for r in rows(prefix, "label-directory")}
    if not initial:
        raise ValueError(f"{prefix}: missing starting-label directory")
    end, first_close, first_promotion = {}, {}, {}
    flows = defaultdict(Counter)
    observed_weeks = set()
    last_year = 1960
    for r in rows(prefix, "label-finance"):
        week = int(r["week"])
        if week > weeks:
            continue
        observed_weeks.add(week)
        last_year = max(last_year, int(r["year"]))
        label = r["labelId"]
        end[label] = r
        if r["status"] in CLOSED:
            first_close.setdefault(label, week)
        c = flows[r["labelTier"]]
        for k in ("weeklyGross", "weeklyNet", "weeklyPressing", "weeklyRoyalty", "weeklySkim"):
            c[k] += float(r[k])
    if observed_weeks != set(range(1, weeks + 1)):
        raise ValueError(f"{prefix}: incomplete finance stream for weeks 1-{weeks}")
    if set(initial) - set(end):
        raise ValueError(f"{prefix}: missing starting labels in finance")
    if any(int(end[i]["week"]) != weeks for i in initial):
        raise ValueError(f"{prefix}: incomplete final-week starting cohort")
    for r in rows(prefix, "label-tier-transitions"):
        if int(r["week"]) <= weeks and r["direction"] == "promotion":
            first_promotion.setdefault(r["labelId"], int(r["week"]))
    cohort = [i for i, tier in initial.items() if tier == "Small"]
    live = [end[i] for i in cohort if end[i]["status"] not in INACTIVE]
    closed = sum(i in first_close for i in cohort)
    before = sum(i in first_close and first_close[i] < first_promotion.get(i, float("inf")) for i in cohort)
    result = {"prefix": str(prefix), "weeks": weeks,
              "startingSmall": {"n": len(cohort), "closed": closed, "closedPct": round(100 * closed / len(cohort), 3),
                                "closedBeforeFirstPromotion": before, "everPromoted": sum(i in first_promotion for i in cohort),
                                "survivors": len(live), "survivorMedianCash": median([float(r["cashReserves"]) for r in live]),
                                "survivorMedianRunwayMonths": median([float(r["cashReserves"]) / max(1, float(r["monthlyOverhead"])) for r in live])},
              "activeMedianCashByCurrentTier": {t: median([float(r["cashReserves"]) for r in end.values()
                                                         if r["labelTier"] == t and r["status"] not in INACTIVE]) for t in TIERS},
              "flowsByCurrentTier": {t: {k: round(v, 2) for k, v in c.items()} for t, c in flows.items()}}
    result["activeCountByCurrentTier"] = {t: sum(r["labelTier"] == t and r["status"] not in INACTIVE for r in end.values()) for t in TIERS}
    result["survivorMedianCashByStartingTier"] = {
        t: median([float(end[i]["cashReserves"]) for i, tier in initial.items() if tier == t and end[i]["status"] not in INACTIVE]) for t in TIERS}
    result["totalActiveCash"] = round(sum(float(r["cashReserves"]) for r in end.values() if r["status"] not in INACTIVE), 2)
    operator_pay = Counter()
    for r in rows(prefix, "operator-pay"):
        if int(r["week"]) <= weeks:
            if "monthlyOverhead" in r:
                overhead, pay = float(r["monthlyOverhead"]), float(r["pay"])
                surplus, cash = float(r["operatingCashSurplus"]), float(r["cashAfter"])
                reserve = 6 * overhead + max(0, float(r["outstandingPlantPayables"]))
                if pay <= 0 or pay > float(r["monthlyCap"]) + .02 or pay > .5 * max(0, surplus - 2 * overhead) + .02 or cash < reserve - max(.02, reserve * 1e-6):
                    raise ValueError(f"{prefix}: inconsistent operator payment ledger")
            operator_pay[r["tier"]] += float(r["pay"])
    result["operatorPayByTier"] = {t: round(v, 2) for t, v in operator_pay.items()}
    result["bookedContributionMarginByCurrentTier"] = {
        t: round(c["weeklyNet"] / max(1, c["weeklyGross"]), 5) for t, c in flows.items()}
    result["marginDefinition"] = "Booked record contribution after plant bills, before production, marketing and overhead; not operating profit"
    if weeks == 260:
        result["startingSmall"]["fiveYearClosureInBand"] = 20 <= 100 * closed / len(cohort) <= 35
    inventory = defaultdict(Counter)
    for r in rows(prefix, "release-outcomes"):
        if int(r["week"]) > weeks or r["memoryEligible"].lower() != "true":
            continue
        c = inventory[r["format"]]
        c["retiredRecords"] += 1
        c["pressed"] += int(r["unitsPressed"])
        c["sold"] += int(r["pressingBookSold"])
    result["retiredInventory"] = {t: dict(c, unsoldShare=round(1 - c["sold"] / max(1, c["pressed"]), 5)) for t, c in inventory.items()}
    outcomes = defaultdict(list)
    for r in rows(prefix, "release-all-in-outcomes"):
        if int(r["week"]) <= weeks:
            outcomes[r["format"]].append(float(r["realizedNet"]))
    result["retiredAllIn"] = {t: {"n": len(v), "lossPct": round(100 * sum(x < 0 for x in v) / len(v), 3), "median": median(v)}
                             for t, v in outcomes.items()}
    ladder = [r for r in rows(prefix, "career-ladder") if int(r["week"]) <= weeks]
    if ladder:
        result["careerFinal"] = {k: int(ladder[-1][k]) for k in ("Rising", "Established", "Star", "Superstar")}
        result["careerMoves"] = {k: sum(int(r[k]) for r in ladder)
                                 for k in ("toStar", "toSuperstar", "starToEstablished")}
    # Week 260 is Dec 25, the final chart week of 1964. The original full runs
    # confirm that 1964 is weeks 209-260, so its annual row is comparable in full.
    last_complete_year = 1964 if weeks == 260 else last_year - 1
    result["completedAnnual"] = {}
    for suffix, columns in {
        "decade-annual-rollup": ("singleUnits", "singleNet", "albumUnits", "albumNet"),
        "concentration": ("c4ChartShare", "majorFamilyChartShare", "firmsCharting"),
        "chart-breadth": ("hot100Acts", "top40Acts", "top10Acts", "albumChartActs"),
    }.items():
        result["completedAnnual"][suffix] = {r["year"]: {k: float(r[k]) for k in columns}
                                              for r in rows(prefix, suffix) if int(r["year"]) <= last_complete_year}
    return result


def review(runs, manifest_path, weeks):
    manifest = json.loads(Path(manifest_path).read_text(encoding="utf-8-sig"))
    entries = manifest["results"]
    names = {Path(r["prefix"]).name for r in runs}
    if manifest["weeks"] != weeks or names != {r["name"] for r in entries} or len(entries) != len(runs):
        raise ValueError("Review requires exactly the completed manifest runs at the same horizon")
    if not manifest.get("assemblySha256") or not all(r["passed"] and r["exitCode"] == 0 for r in entries):
        raise ValueError("Review requires a recorded assembly and successful native completion")
    seeds = [r["seed"] for r in entries]
    if len(seeds) != len(set(seeds)):
        raise ValueError("Review seeds must be distinct")
    configurations = {tuple(a for a in r["args"] if not a.startswith(("--run=", "--seed="))) for r in entries}
    if len(configurations) != 1:
        raise ValueError("Review cannot pool different configurations")
    rates = [r["startingSmall"]["closedPct"] for r in runs]
    status = "INSUFFICIENT_SEEDS" if len(seeds) < 3 else "DIAGNOSTIC_HORIZON"
    if len(seeds) >= 3 and weeks == 260:
        status = "IN_BAND" if all(r["startingSmall"]["fiveYearClosureInBand"] for r in runs) else "OUT_OF_BAND"
    return {"status": status, "seeds": seeds, "assemblySha256": manifest["assemblySha256"],
            "equalWeightMeanClosedPct": round(statistics.mean(rates), 3),
            "criterion": "Each seed: 20-35% of starting-Small firms financially closed by week 260; cash and margins diagnostic"}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("prefixes", nargs="+")
    parser.add_argument("--weeks", type=int, default=260)
    parser.add_argument("--output", required=True)
    parser.add_argument("--review-manifest")
    args = parser.parse_args()
    result = [analyze(p, args.weeks) for p in args.prefixes]
    document = {"runs": result}
    if args.review_manifest:
        document["review"] = review(result, args.review_manifest, args.weeks)
        print(json.dumps(document["review"]))
    Path(args.output).write_text(json.dumps(document, indent=2, allow_nan=False), encoding="utf-8")
    for r in result:
        print(json.dumps({"prefix": r["prefix"], "startingSmall": r["startingSmall"],
                          "retiredInventory": r["retiredInventory"], "retiredAllIn": r["retiredAllIn"]}))


if __name__ == "__main__":
    main()
