"""Fit A3's LP affinity and hit-inventory contribution from finalized LP cohorts.

Retains the existing quality, reach, genre and period exposure. Fits four nondecreasing
career affinity coefficients and a nonnegative hit coefficient by least squares in dollars.
Production cost stays additive, so the fit cannot multiply a sunk cost or double-discount
dealer prices. The legacy Superstar premium is retained within the top career band.
This is a conditional forecast of observed launches, not a causal test of debut policy.
Uses only the standard library. Generated reports/tables belong under SimLogs.
"""

import argparse
import hashlib
import json
import sys
from collections import Counter, defaultdict
from pathlib import Path
from statistics import mean

sys.path.insert(0, str(Path(__file__).resolve().parent))
from calibrate_single_prior import BANDS, BAND_NAMES, isotonic, rows


def load(prefix, cutoff):
    # The archived fit is before A4. Its cost basis cannot silently be reused for
    # a new all-in forecast whose marketing input is not in the legacy decision CSV.
    if Path(f"{prefix}-release-profit-gates.csv").exists():
        raise ValueError("Album refit needs a pre-A4 cohort with the documented production-only forecast basis.")
    launches, excluded = {}, Counter()
    for row in rows(prefix, "a3-economic-decisions"):
        if row["chosenFormat"] != "Album" or int(row["week"]) > cutoff:
            continue
        if row["careerState"] not in BANDS:
            excluded[row["careerState"]] += 1
            continue
        rid = row["recordId"]
        if rid in launches:
            raise ValueError(f"Duplicate album launch: {prefix} {rid}")
        launches[rid] = row
    costs = {row["recordId"]: float(row["singleProductionCost"])
             for row in rows(prefix, "calibration-decisions") if row["recordId"] in launches}
    data, observed = [], set()
    for row in rows(prefix, "release-outcomes"):
        rid = row["recordId"]
        if rid not in launches or row["format"] != "Album" or row["memoryEligible"].lower() != "true":
            continue
        if rid in observed:
            raise ValueError(f"Duplicate final outcome: {prefix} {rid}")
        observed.add(rid)
        dec = launches[rid]
        cost = costs[rid] * float(dec["expectedFormatMultiplier"]) + 750.
        old = float(dec["priorAlbumNet"])
        units = float(dec["totalExpectedAlbumUnits"])
        if units <= 0:
            raise ValueError(f"Nonpositive expected units: {prefix} {rid}")
        margin = (old + cost) / units
        affinity = float(dec["affinityUnits"])
        # Star 2.0 and Superstar 2.5 share a fitted top-band coefficient, preserving
        # the extra 25% Superstar premium rather than silently discarding it.
        stature = float(dec["statureMultiplier"])
        top_premium = 1.25 if dec["careerState"] == "Superstar" else 1.
        base = affinity / stature * top_premium * margin
        hits = (units - affinity) * margin
        if hits < -.05:
            raise ValueError(f"Hit contribution below zero: {prefix} {rid}")
        data.append({"id": rid, "band": BANDS[dec["careerState"]], "base": base,
                     "hits": max(0., hits), "cost": cost, "old": old, "net": float(row["realizedNet"]),
                     "year": int(dec["year"]), "genre": dec["genre"], "genreGroup": dec["genreGroup"],
                     "debut": int(dec["resolvedSingles"]) == 0,
                     "chartedSingles": int(dec["chartedSingles"]), "format": dec["actualAlbumFormat"],
                     "actualMasterCostDelta": costs[rid] * 1.8 * (1. - min(4, int(dec["resolvedSingles"])) / 4.)
                         if dec["actualAlbumFormat"] == "Compilation" else 0.,
                     "expectedMasterCostDelta": costs[rid] * float(dec["compCostWeight"]) * 1.8 * (1. - min(4, int(dec["resolvedSingles"])) / 4.)})
    missing = Counter(BAND_NAMES[BANDS[meta["careerState"]]]
                      for rid, meta in launches.items() if rid not in observed)
    if not data:
        raise ValueError(f"No completed LPs: {prefix}")
    return data, {"launches": len(launches), "finalized": len(data), "missingByCareer": dict(missing),
                  "excludedFallbackCareerStates": dict(excluded)}


def fit(data):
    # Block coordinate descent. Each career coefficient has disjoint support;
    # its exact constrained update is weighted isotonic regression. The shared
    # hit term has the exact nonnegative scalar least-squares update.
    by_band = [[row for row in data if row["band"] == band] for band in range(4)]
    weights = [sum(row["base"] ** 2 for row in group) for group in by_band]
    if not all(weight > 0 for weight in weights):
        raise ValueError("All career bands need observed nonzero affinity exposure.")
    hit_weight = sum(row["hits"] ** 2 for row in data)
    if hit_weight <= 0:
        raise ValueError("Hit coefficient is unidentified without hit-inventory exposure.")
    career, hit = [1., 1.2, 1.5, 2.], 1.
    for iteration in range(10000):
        previous = career + [hit]
        raw = [sum(row["base"] * (row["net"] + row["cost"] - hit * row["hits"]) for row in group) / weight
               for group, weight in zip(by_band, weights)]
        career = [max(0., value) for value in isotonic(raw, weights)]
        hit = max(0., sum(row["hits"] * (row["net"] + row["cost"] - career[row["band"]] * row["base"])
                          for row in data) / hit_weight)
        if max(abs(a - b) for a, b in zip(previous, career + [hit])) < 1e-10:
            break
    else:
        raise ValueError("Album fit did not converge.")
    return {"careerAffinity": career, "hitCoefficient": hit, "iterations": iteration + 1,
            "samplesByCareer": [len(group) for group in by_band]}


def predict(model, row):
    return model["careerAffinity"][row["band"]] * row["base"] + model["hitCoefficient"] * row["hits"] - row["cost"]


def evaluate(model, data):
    groups = defaultdict(list)
    for row in data:
        for key in ("all", BAND_NAMES[row["band"]], f"{BAND_NAMES[row['band']]}|{'no-resolved-single' if row['debut'] else 'prior-single'}",
                    f"period|{1960 if row['year'] <= 1962 else 1963 if row['year'] <= 1965 else 1966}"):
            groups[key].append(row)
        if row["band"] == 0 and row["debut"]:
            groups[f"New/Unsigned|no-resolved-single|group:{row['genreGroup']}"].append(row)
            groups[f"New/Unsigned|no-resolved-single|genre:{row['genre']}"].append(row)
    result = {}
    for key, group in groups.items():
        actual = [r["net"] for r in group]
        old = [r["old"] for r in group]
        new = [predict(model, r) for r in group]
        result[key] = {"n": len(group), "meanRealized": mean(actual), "meanOld": mean(old), "meanNew": mean(new),
                       "oldMeanError": mean(p - y for p, y in zip(old, actual)),
                       "newMeanError": mean(p - y for p, y in zip(new, actual)),
                       "oldMeanAbsoluteError": mean(abs(p - y) for p, y in zip(old, actual)),
                       "newMeanAbsoluteError": mean(abs(p - y) for p, y in zip(new, actual)),
                       "actualLossShare": mean(y < 0 for y in actual), "newExpectedLossShare": mean(y <= 0 for y in new)}
        result[key]["fixedSalesMasterCostSensitivity"] = {
            "meanAdditionalBookedCost": mean(r["actualMasterCostDelta"] for r in group),
            "meanRealizedAfterCost": mean(r["net"] - r["actualMasterCostDelta"] for r in group),
            "lossShareAfterCost": mean(r["net"] - r["actualMasterCostDelta"] < 0 for r in group),
            "compilationShare": mean(r["format"] == "Compilation" for r in group),
            "meanAdditionalForecastCost": mean(r["expectedMasterCostDelta"] for r in group),
        }
    return result


def fingerprint(prefix):
    result = {}
    for suffix in ("a3-economic-decisions", "calibration-decisions", "release-outcomes"):
        with Path(f"{prefix}-{suffix}.csv").open("rb") as stream:
            result[suffix] = hashlib.file_digest(stream, "sha256").hexdigest()
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("training", type=Path)
    parser.add_argument("--holdout", type=Path, action="append", default=[])
    parser.add_argument("--cutoff-week", type=int, default=364)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    data, coverage = load(args.training, args.cutoff_week)
    if coverage["missingByCareer"]:
        raise ValueError(f"Training cohort has unfinalized LPs: {coverage}")
    model = fit(data)
    report = {"training": str(args.training), "cutoffWeek": args.cutoff_week, "coverage": coverage,
              "inputSha256": fingerprint(args.training), "model": model, "trainingEvaluation": evaluate(model, data), "holdouts": []}
    del data
    for prefix in args.holdout:
        data, coverage = load(prefix, args.cutoff_week)
        if coverage["missingByCareer"]:
            raise ValueError(f"Holdout cohort has unfinalized LPs: {prefix} {coverage}")
        report["holdouts"].append({"run": str(prefix), "coverage": coverage,
                                   "inputSha256": fingerprint(prefix), "evaluation": evaluate(model, data)})
        del data
    args.output.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"model": model, "coverage": report["coverage"],
                      "training": report["trainingEvaluation"],
                      "holdouts": [{"run": h["run"], "coverage": h["coverage"], "evaluation": h["evaluation"]}
                                   for h in report["holdouts"]]}, indent=2))


if __name__ == "__main__":
    main()
