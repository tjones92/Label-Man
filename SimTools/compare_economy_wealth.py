"""Compare complete 1960-64 label contribution and week-260 cash with original runs.

python -I SimTools/compare_economy_wealth.py PREFIX ... --original-template SimLogs/decaboff-{seed}-522 --output SimLogs/review.json
Ratios of equal-weight seed means; cash uses each seed's active-label median by current tier.
"""
import argparse
import csv
import importlib.util
import json
import statistics
from pathlib import Path

spec = importlib.util.spec_from_file_location("economy_retune", Path(__file__).with_name("analyze_economy_retune.py"))
analysis = importlib.util.module_from_spec(spec)
spec.loader.exec_module(analysis)


def annual(prefix):
    with open(f"{prefix}-decade-annual-rollup.csv", newline="", encoding="utf-8-sig") as handle:
        records = list(csv.DictReader(handle))
    seeds = {int(r["seed"]) for r in records}
    if len(seeds) != 1:
        raise ValueError(f"{prefix}: expected one seed")
    selected = {int(r["year"]): r for r in records if 1960 <= int(r["year"]) <= 1964}
    if set(selected) != set(range(1960, 1965)):
        raise ValueError(f"{prefix}: missing annual rows for 1960-64")
    return seeds.pop(), {y: {k: float(r[k]) for k in ("singleNet", "albumNet", "singleUnits", "albumUnits")} for y, r in selected.items()}


def compare(prefixes, original_template, tolerance=.15):
    runs = []
    for prefix in prefixes:
        seed, years = annual(prefix)
        original = original_template[seed] if isinstance(original_template, dict) else original_template.format(seed=seed)
        original_seed, original_years = annual(original)
        if seed != original_seed:
            raise ValueError("Original and candidate seeds must match")
        candidate = analysis.analyze(prefix, 260)
        baseline = analysis.analyze(original, 260)
        def measures(summary, annual_rows):
            return dict({k: sum(r[k] for r in annual_rows.values()) for k in ("singleNet", "albumNet")},
                        **{f"cash{t}": v for t, v in summary["activeMedianCashByCurrentTier"].items()})
        before, after = measures(baseline, original_years), measures(candidate, years)
        runs.append({"seed": seed, "prefix": prefix, "original": original, "baseline": before, "candidate": after,
                     "ratios": {k: after[k] / before[k] for k in before},
                     "closure": candidate["startingSmall"], "annual": years, "originalAnnual": original_years,
                     "tierPopulation": candidate["activeCountByCurrentTier"], "originalTierPopulation": baseline["activeCountByCurrentTier"],
                     "startingTierCash": candidate["survivorMedianCashByStartingTier"], "originalStartingTierCash": baseline["survivorMedianCashByStartingTier"],
                     "totalActiveCash": candidate["totalActiveCash"], "originalTotalActiveCash": baseline["totalActiveCash"],
                     "operatorPayByTier": candidate.get("operatorPayByTier", {})})
    if len({r["seed"] for r in runs}) != len(runs):
        raise ValueError("Comparison seeds must be distinct")
    metrics = {}
    for k in runs[0]["baseline"]:
        before = statistics.mean(r["baseline"][k] for r in runs)
        after = statistics.mean(r["candidate"][k] for r in runs)
        metrics[k] = {"baselineMean": before, "candidateMean": after, "ratio": after / before,
                      "withinTolerance": 1 - tolerance <= after / before <= 1 + tolerance}
    return {"horizon": "1960-64 chart years; cash/closures week 260", "tolerance": tolerance,
            "metrics": metrics, "allMeansNearOriginal": all(r["withinTolerance"] for r in metrics.values()), "runs": runs}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("prefixes", nargs="+")
    parser.add_argument("--original-template", default="SimLogs/decaboff-{seed}-522")
    parser.add_argument("--original-prefix", action="append", help="Explicit same-seed reference; repeat for mixed reference filenames")
    parser.add_argument("--tolerance", type=float, default=.15)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    originals = args.original_template
    if args.original_prefix:
        originals = {annual(p)[0]: p for p in args.original_prefix}
        if len(originals) != len(args.original_prefix):
            raise ValueError("Original reference seeds must be distinct")
    result = compare(args.prefixes, originals, args.tolerance)
    Path(args.output).write_text(json.dumps(result, indent=2, allow_nan=False), encoding="utf-8")
    print(json.dumps({"allMeansNearOriginal": result["allMeansNearOriginal"], "metrics": result["metrics"],
                      "closures": {r["seed"]: r["closure"]["closedPct"] for r in result["runs"]}}, indent=2))


if __name__ == "__main__":
    main()
