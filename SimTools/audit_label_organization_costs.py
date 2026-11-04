"""Describe existing overhead relative to turnover; never infer company net income.

Example: python -I SimTools/audit_label_organization_costs.py PREFIX ...
    --output SimLogs/label-organization-cost-context.json
Inputs are completed 1960-64 finance streams from a single configuration.
"""
import argparse
import csv
import json
import math
import statistics
from collections import defaultdict
from pathlib import Path

TIERS = ("Small", "Boutique", "Independent", "MidTier", "Major")
INACTIVE = {"Bankrupt", "Defunct", "Acquired"}


def median(values):
    return round(statistics.median(values), 4) if values else None


def audit(prefix):
    annual = defaultdict(lambda: {"gross": 0., "contribution": 0.,
                                 "distributionIncome": 0., "activeWeeks": 0})
    end = {}
    seen = set()
    weeks = set()
    label_weeks = defaultdict(set)
    with open(f"{prefix}-label-finance.csv", newline="", encoding="utf-8-sig") as handle:
        for row in csv.DictReader(handle):
            week, year = int(row["week"]), int(row["year"])
            if not 1 <= week <= 260:
                continue
            key = (week, row["labelId"])
            if key in seen:
                raise ValueError(f"{prefix}: duplicate label/week {key}")
            seen.add(key)
            weeks.add(week)
            label_weeks[row["labelId"]].add(week)
            if year != 1960 + (week - 1) // 52:
                raise ValueError(f"{prefix}: unexpected chart year at week {week}")
            for field in ("weeklyGross", "weeklyNet", "weeklyDistributionIncome",
                          "monthlyOverhead", "cashReserves"):
                if not math.isfinite(float(row[field])):
                    raise ValueError(f"{prefix}: nonfinite {field} at {key}")
            if year == 1964:
                totals = annual[row["labelId"]]
                totals["gross"] += float(row["weeklyGross"])
                totals["contribution"] += float(row["weeklyNet"])
                totals["distributionIncome"] += float(row["weeklyDistributionIncome"])
                totals["activeWeeks"] += row["status"] not in INACTIVE
            if week == 260:
                end[row["labelId"]] = row
    if weeks != set(range(1, 261)):
        raise ValueError(f"{prefix}: incomplete weeks 1-260")
    for label_id, observed in label_weeks.items():
        if observed != set(range(min(observed), 261)):
            raise ValueError(f"{prefix}: missing weekly rows for {label_id}")
    # Full-year active endpoint survivors only. Excludes closures and late births;
    # this is explicitly a survivor diagnostic, not an industry profitability census.
    firms = []
    for label_id, row in end.items():
        totals = annual[label_id]
        if row["status"] in INACTIVE or totals["activeWeeks"] != 52 or totals["gross"] <= 0:
            continue
        overhead = float(row["monthlyOverhead"])
        firms.append({"labelId": label_id, "labelName": row["labelName"],
                      "tier": row["labelTier"], "monthlyOverheadAtEnd": overhead,
                      "gross1964": totals["gross"],
                      "recordContribution1964": totals["contribution"],
                      "distributionIncome1964": totals["distributionIncome"],
                      "annualizedEndOverheadPctOfGross": 1200 * overhead / totals["gross"],
                      "recordContributionPctOfGross": 100 * totals["contribution"] / totals["gross"],
                      "cashAtEnd": float(row["cashReserves"])})
    summaries = {}
    for tier in TIERS:
        selected = [firm for firm in firms if firm["tier"] == tier]
        summaries[tier] = {"n": len(selected), **{
            name: median([firm[name] for firm in selected]) for name in (
                "monthlyOverheadAtEnd", "gross1964", "annualizedEndOverheadPctOfGross",
                "recordContributionPctOfGross")}}
    return {"prefix": str(prefix), "tiers": summaries, "firms": firms}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("prefixes", nargs="+")
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    if len(set(args.prefixes)) != len(args.prefixes):
        raise ValueError("Run prefixes must be distinct")
    runs = [audit(prefix) for prefix in args.prefixes]
    means = {tier: {key: round(statistics.mean(run["tiers"][tier][key] for run in runs), 4)
                    if all(run["tiers"][tier][key] is not None for run in runs) else None
                    for key in runs[0]["tiers"][tier] if key != "n"} for tier in TIERS}
    result = {
        "definition": "1964 full-year-active endpoint survivors, grouped by endpoint tier. "
                      "Equal-weight means of run medians, not pooled firms or ratios of medians. "
                      "End overhead times twelve is an annualized scale comparison, not actual annual overhead. "
                      "Record contribution excludes production, campaigns and overhead; it is not company earnings. "
                      "Distribution income is separate. Inputs must be the same configuration; provenance is caller-supplied.",
        "runs": runs, "meanMediansByTier": means}
    Path(args.output).write_text(json.dumps(result, indent=2, allow_nan=False), encoding="utf-8")
    print(json.dumps(means, indent=2))


if __name__ == "__main__":
    main()
