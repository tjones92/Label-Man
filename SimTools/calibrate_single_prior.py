"""Refit audit A2's single contribution prior from completed standalone releases.

Standard-library streaming joins keep this usable while the desktop is in use. The training cohort ends
104 weeks before the full run ends; unfinalized launches are counted and reported, never treated as zero.
Arithmetic means estimate expected money. Medians are reported as distribution diagnostics, not fitted as
expectations. Weighted isotonic regression enforces nondecreasing contribution in quality and career;
empty cells have the pooled quality-bin mean with one effective observation. Interpolation is continuous.

Example:
  python calibrate_single_prior.py SimLogs/dec-awrw-on-1001-522 --holdout SimLogs/dec-awrw-on-2002-522 \
      --output SimLogs/single-prior-fit.json --csharp SimLogs/single-prior-table.txt
All generated evidence belongs in SimLogs. No simulation, network or third-party package is used.
"""

import argparse
import bisect
import csv
import hashlib
import json
from collections import Counter, defaultdict
from pathlib import Path
from statistics import mean, median

BANDS = {"NewSigning": 0, "Unsigned": 0, "Rising": 1, "Established": 2, "Star": 3, "Superstar": 3}
BAND_NAMES = ("New/Unsigned", "Rising", "Established", "Star/Superstar")
QUALITY_EDGES = (.4, .45, .5, .55, .6, .625, .65, .675, .7, .75, .8)


def rows(prefix, suffix):
    with Path(f"{prefix}-{suffix}.csv").open(encoding="utf-8-sig", newline="") as stream:
        yield from csv.DictReader(stream)


def load(prefix, cutoff):
    # Tuple: quality, career, current reach x genre exposure, production cost, launch year, launch week.
    launches, excluded = {}, Counter()
    for row in rows(prefix, "calibration-decisions"):
        if row["chosenFormat"] != "Single" or int(row["week"]) > cutoff:
            continue
        if row["careerState"] not in BANDS:
            excluded[row["careerState"]] += 1
            continue
        launches[row["recordId"]] = (
            float(row["qualityEstimate"]), BANDS[row["careerState"]],
            float(row["reachFactor"]) * float(row["genreSinglesMarketFactor"]),
            float(row["singleProductionCost"]), int(row["year"]), int(row["week"]),
        )
    old = {row["recordId"]: float(row["priorSingleNet"])
           for row in rows(prefix, "a3-economic-decisions") if row["recordId"] in launches}
    data, observed = [], set()
    for row in rows(prefix, "release-outcomes"):
        rid = row["recordId"]
        if row["format"] != "Single" or row["memoryEligible"].lower() != "true" or rid not in launches:
            continue
        if rid in observed:
            raise ValueError(f"Duplicate final outcome: {prefix} {rid}")
        observed.add(rid)
        q, career, exposure, cost, year, week = launches[rid]
        if exposure <= 0:
            raise ValueError(f"Nonpositive exposure: {prefix} {rid}")
        data.append((q, career, exposure, cost, float(row["realizedNet"]), old[rid], year))
    missing = Counter(BAND_NAMES[meta[1]] for rid, meta in launches.items() if rid not in observed)
    if not data:
        raise ValueError(f"No completed standalone singles: {prefix}")
    return data, {"launches": len(launches), "finalized": len(data), "missingByCareer": dict(missing),
                  "excludedFallbackCareerStates": dict(excluded)}


def isotonic(values, weights):
    blocks = []
    for i, (value, weight) in enumerate(zip(values, weights)):
        blocks.append([i, i + 1, value * weight, weight])
        while len(blocks) >= 2 and blocks[-2][2] / blocks[-2][3] > blocks[-1][2] / blocks[-1][3]:
            right, left = blocks.pop(), blocks.pop()
            blocks.append([left[0], right[1], left[2] + right[2], left[3] + right[3]])
    result = [0.] * len(values)
    for start, end, total, weight in blocks:
        result[start:end] = [total / weight] * (end - start)
    return result


def fit(data):
    buckets, qualities = defaultdict(list), defaultdict(list)
    for q, career, exposure, cost, net, _, _ in data:
        bucket = bisect.bisect_right(QUALITY_EDGES, q)
        buckets[bucket, career].append((net + cost) / exposure)
        qualities[bucket].append(q)
    size = len(QUALITY_EDGES) + 1
    if any(not qualities[i] for i in range(size)):
        raise ValueError("A whole quality bin is empty; choose supported bins rather than inventing a fit.")
    knots = [mean(qualities[i]) for i in range(size)]
    pooled = [mean(value for career in range(4) for value in buckets[i, career]) for i in range(size)]
    counts = [[len(buckets[i, career]) for career in range(4)] for i in range(size)]
    weights = [[max(1, count) for count in row] for row in counts]
    raw = [[mean(buckets[i, career]) if buckets[i, career] else pooled[i] for career in range(4)]
           for i in range(size)]
    # Dykstra projections in the weighted Euclidean norm: quality columns, then career rows.
    fitted = [row[:] for row in raw]
    p, r = ([[0.] * 4 for _ in range(size)] for _ in range(2))
    for iteration in range(10000):
        before = [row[:] for row in fitted]
        quality = [[0.] * 4 for _ in range(size)]
        for career in range(4):
            values = [fitted[i][career] + p[i][career] for i in range(size)]
            projected = isotonic(values, [weights[i][career] for i in range(size)])
            for i in range(size):
                quality[i][career] = projected[i]
                p[i][career] = values[i] - projected[i]
        for i in range(size):
            values = [quality[i][career] + r[i][career] for career in range(4)]
            fitted[i] = isotonic(values, weights[i])
            r[i] = [values[career] - fitted[i][career] for career in range(4)]
        if max(abs(fitted[i][career] - before[i][career]) for i in range(size) for career in range(4)) < 1e-8:
            break
    else:
        raise ValueError("Isotonic fit failed to converge.")
    tolerance = 1e-6
    assert all(fitted[i][b] <= fitted[i][b + 1] + tolerance for i in range(size) for b in range(3))
    assert all(fitted[i][b] <= fitted[i + 1][b] + tolerance for i in range(size - 1) for b in range(4))
    # Store exactly the precision emitted to C#, so evaluations use the authored table.
    return {"qualityKnots": [round(q, 6) for q in knots],
            "normalizedContribution": [[round(v, 3) for v in row] for row in fitted],
            "sampleCounts": counts, "rawMeans": raw, "iterations": iteration + 1}


def predict(model, q, career, exposure, cost):
    knots, values = model["qualityKnots"], model["normalizedContribution"]
    upper = bisect.bisect_right(knots, q)
    if upper == 0:
        contribution = values[0][career]
    elif upper == len(knots):
        contribution = values[-1][career]
    else:
        fraction = (q - knots[upper - 1]) / (knots[upper] - knots[upper - 1])
        contribution = values[upper - 1][career] + fraction * (values[upper][career] - values[upper - 1][career])
    return contribution * exposure - cost


def evaluate(model, data):
    result = {}
    for career in range(4):
        selected = [row for row in data if row[1] == career]
        if not selected:
            continue
        actual = [row[4] for row in selected]
        old = [row[5] for row in selected]
        new = [predict(model, *row[:4]) for row in selected]
        result[BAND_NAMES[career]] = {
            "n": len(selected), "meanActual": mean(actual), "meanOld": mean(old), "meanNew": mean(new),
            "oldAggregateError": sum(old) / sum(actual) - 1, "newAggregateError": sum(new) / sum(actual) - 1,
            "medianActual": median(actual), "medianOld": median(old), "medianNew": median(new),
            "oldMeanAbsoluteError": mean(abs(p - y) for p, y in zip(old, actual)),
            "newMeanAbsoluteError": mean(abs(p - y) for p, y in zip(new, actual)),
        }
    return result


def fingerprint(prefix):
    result = {}
    for suffix in ("calibration-decisions", "a3-economic-decisions", "release-outcomes"):
        path = Path(f"{prefix}-{suffix}.csv")
        with path.open("rb") as stream:
            result[suffix] = hashlib.file_digest(stream, "sha256").hexdigest()
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("training", type=Path)
    parser.add_argument("--holdout", type=Path, action="append", default=[])
    parser.add_argument("--cutoff-week", type=int, default=418)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--csharp", type=Path)
    args = parser.parse_args()
    data, coverage = load(args.training, args.cutoff_week)
    if coverage["missingByCareer"]:
        raise ValueError("Training cohort has unfinalized launches; use an earlier cutoff or a completed longer run.")
    model = fit(data)
    report = {"training": str(args.training), "cutoffWeek": args.cutoff_week, "coverage": coverage,
              "inputSha256": fingerprint(args.training), "model": model, "trainingEvaluation": evaluate(model, data),
              "holdouts": []}
    del data
    for prefix in args.holdout:
        data, coverage = load(prefix, args.cutoff_week)
        if coverage["missingByCareer"]:
            raise ValueError(f"Holdout cohort has unfinalized launches: {prefix}; use an earlier cutoff.")
        report["holdouts"].append({"run": str(prefix), "coverage": coverage, "inputSha256": fingerprint(prefix),
                                   "evaluation": evaluate(model, data)})
        del data
    args.output.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    if args.csharp:
        table = "private static readonly float[] QualityKnots = { " + ", ".join(f"{q:.6f}f" for q in model["qualityKnots"]) + " };\n"
        table += "private static readonly float[,] NormalizedContribution = {\n"
        table += "\n".join("    { " + ", ".join(f"{v:.3f}f" for v in row) + " }," for row in model["normalizedContribution"])
        args.csharp.write_text(table + "\n};\n", encoding="utf-8")
    compact = {"training": report["training"], "coverage": report["coverage"],
               "trainingEvaluation": report["trainingEvaluation"], "holdouts": report["holdouts"]}
    print(json.dumps(compact, indent=2))


if __name__ == "__main__":
    main()
