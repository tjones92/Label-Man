"""Validate company cash/earnings statements and summarize completed accounting years.

python -I SimTools/analyze_corporate_economy.py --manifest SimLogs/TAG/runs.json
    --output SimLogs/TAG/review.json
Quarterly distributions occur in the next period; owner transfers cancel within the label sector.
"""
import argparse
import csv
import json
import math
import statistics
from collections import defaultdict
from datetime import date, timedelta
from pathlib import Path

TIERS = ("Small", "Boutique", "Independent", "MidTier", "Major")
INACTIVE = {"Bankrupt", "Defunct", "Acquired"}
ANNUAL = ("recordGross", "serviceIncome", "costOfSalesAndWriteOffs", "pretaxEarnings", "afterTaxEarnings",
          "payroll", "premises", "retainers", "recording", "campaigns", "artistAdvances", "licensing",
          "otherOperating", "restructuring", "taxProvision", "taxPaid", "capitalSpending", "depreciation",
          "distributionOperations", "tradeSupport", "recordExcise", "ownerSalary")


def median(values):
    return statistics.median(values) if values else None


def audit(entry, weeks, root):
    prefix = root / entry["name"]
    result = {"name": entry["name"], "seed": entry["seed"], "mode": entry["mode"], "weeks": weeks}
    initial, end, at260, observed = {}, {}, {}, set()
    with open(f"{prefix}-label-finance.csv", newline="", encoding="utf-8-sig") as source:
        for row in csv.DictReader(source):
            week = int(row["week"])
            if week > weeks:
                continue  # A --weeks truncation of a longer, otherwise identical run.
            observed.add(week)
            if week == 1:
                initial[row["labelId"]] = row["labelTier"]
            if week == weeks:
                end[row["labelId"]] = row
            if week == 260:
                at260[row["labelId"]] = row
    if observed != set(range(1, weeks + 1)) or not set(initial) <= set(end):
        raise ValueError(f"{prefix}: incomplete finance horizon")
    cohort = [label for label, tier in initial.items() if tier == "Small"]
    result["startingSmallClosedPct"] = 100 * sum(end[label]["status"] in INACTIVE for label in cohort) / len(cohort)
    result["startingTierClosedPct"] = {tier: 100 * sum(end[label]["status"] in INACTIVE for label, initial_tier in initial.items() if initial_tier == tier)
        / sum(initial_tier == tier for initial_tier in initial.values()) for tier in TIERS if tier in initial.values()}
    if weeks >= 260:
        result["startingSmallClosedPctWeek260"] = 100 * sum(at260[label]["status"] in INACTIVE for label in cohort) / len(cohort)
        result["startingTierClosedPctWeek260"] = {tier: 100 * sum(at260[label]["status"] in INACTIVE for label, initial_tier in initial.items() if initial_tier == tier)
            / sum(initial_tier == tier for initial_tier in initial.values()) for tier in TIERS if tier in initial.values()}
    result["endpointActiveMedianCash"] = {tier: median([float(row["cashReserves"]) for row in end.values()
        if row["labelTier"] == tier and row["status"] not in INACTIVE]) for tier in TIERS}
    if entry["mode"] == "off":
        return result
    seen, firms, periods_by_label = set(), defaultdict(dict), defaultdict(set)
    residual, profit_residual, max_founder, rows = 0., 0., 0., 0
    monthly = defaultdict(float)
    end_date = date(1960, 1, 1) + timedelta(weeks=weeks)
    expected_periods = set(range(1960 * 12, end_date.year * 12 + end_date.month - 1))
    with open(f"{prefix}-company-monthly.csv", newline="", encoding="utf-8-sig") as source:
        for row in csv.DictReader(source):
            if int(row["period"]) not in expected_periods:
                continue
            for field in ("distributionOperations", "tradeSupport", "recordExcise"):
                row.setdefault(field, "0")  # Pre-calibration journals remain valid reference inputs.
            key = (row["labelId"], int(row["period"]))
            if key in seen:
                raise ValueError(f"{prefix}: duplicate company period {key}")
            seen.add(key)
            founder = float(row.get("founderEquivalent", 0))
            if not math.isfinite(founder) or founder < 0 or founder > 1.000001:
                raise ValueError(f"{prefix}: invalid single-founder work budget at {key}")
            max_founder = max(max_founder, founder)
            periods_by_label[key[0]].add(key[1])
            for field in ANNUAL + ("openingCash", "closingCash", "cashReconciliationError", "cashRounding"):
                if not math.isfinite(float(row[field])):
                    raise ValueError(f"{prefix}: nonfinite {field} at {key}")
            if abs(float(row["pretaxEarnings"]) - float(row["taxProvision"]) - float(row["afterTaxEarnings"])) > .01:
                raise ValueError(f"{prefix}: earnings/tax identity fails at {key}")
            operating_expense = sum(float(row[field]) for field in ("payroll", "premises", "retainers", "recording", "campaigns",
                "artistAdvances", "licensing", "otherOperating", "restructuring", "legacyOverhead", "ownerSalary", "depreciation",
                "distributionOperations", "tradeSupport", "recordExcise"))
            expected = float(row["recordGross"]) + float(row["serviceIncome"]) - float(row["costOfSalesAndWriteOffs"]) - operating_expense + float(row["financingEarningsAdjustment"])
            if abs(expected - float(row["pretaxEarnings"])) > .02:
                raise ValueError(f"{prefix}: direct P&L does not match working-capital bridge at {key}")
            profit_residual = max(profit_residual, abs(expected - float(row["pretaxEarnings"])))
            residual = max(residual, abs(float(row["cashReconciliationError"])))
            for field in ANNUAL:
                monthly[field] += float(row[field])
            firms[(row["labelId"], int(row["year"]))][int(row["month"])] = row
            rows += 1
    if not rows or residual > .01:
        raise ValueError(f"{prefix}: absent statements or unreconciled cash")
    for label in initial:
        observed_periods = periods_by_label[label]
        if observed_periods != expected_periods:
            raise ValueError(f"{prefix}: incomplete company periods for starting label {label}")
    result["monthlyRows"] = rows
    result["maxCashReconciliationError"] = residual
    result["maxProfitReconciliationError"] = profit_residual
    result["maxFounderEquivalent"] = max_founder
    result["allObservedMonthTotals"] = dict(monthly)
    annual = defaultdict(lambda: defaultdict(list))
    partial, inactive = 0, 0
    for (label, year), months in firms.items():
        if set(months) != set(range(1, 13)):
            partial += 1
            continue
        totals = {field: sum(float(row[field]) for row in months.values()) for field in ANNUAL}
        if not any(row["active"].lower() == "true" for row in months.values()) and all(abs(value) < .001 for value in totals.values()):
            inactive += 1
            continue
        totals["companyRevenue"] = totals["recordGross"] + totals["serviceIncome"]
        totals["afterTaxMargin"] = totals["afterTaxEarnings"] / totals["companyRevenue"] if totals["companyRevenue"] > 0 else None
        annual[year][months[12]["tier"]].append(totals)
    result["partialFirmYearsExcludedFromAnnualMedians"] = partial
    result["inactiveFirmYearsExcludedFromAnnualMedians"] = inactive
    result["annualByEndTier"] = {year: {tier: {"firms": len(firms),
        "lossPct": 100 * sum(firm["afterTaxEarnings"] < 0 for firm in firms) / len(firms),
        "medianCompanyRevenue": median([firm["companyRevenue"] for firm in firms]),
        "medianAfterTaxMargin": median([firm["afterTaxMargin"] for firm in firms if firm["afterTaxMargin"] is not None]),
        "aggregateAfterTaxMargin": sum(firm["afterTaxEarnings"] for firm in firms) / sum(firm["companyRevenue"] for firm in firms)
            if sum(firm["companyRevenue"] for firm in firms) > 0 else None,
        "totals": {field: sum(firm[field] for firm in firms) for field in ANNUAL}}
        for tier, firms in tiers.items()} for year, tiers in annual.items()}
    owner_net, studio, tax = 0., 0., 0.
    with open(f"{prefix}-company-cash.csv", newline="", encoding="utf-8-sig") as source:
        for row in csv.DictReader(source):
            if int(row["week"]) > weeks:
                continue
            amount = float(row["amount"])
            if row["kind"] == "OwnerDistribution":
                owner_net += amount
            if row["kind"] == "StudioInvestment":
                studio -= amount
            if row["kind"] == "TaxPayment":
                tax -= amount
    result["netDistributionsToExternalOwners"] = -owner_net
    result["studioCapitalCash"] = studio
    result["taxCashPaid"] = tax
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--weeks", type=int, help="Truncate longer runs to this horizon (a prefix-identical control).")
    args = parser.parse_args()
    manifest = json.loads(Path(args.manifest).read_text(encoding="utf-8-sig"))
    entries = manifest["results"]
    if not entries or any(not entry["passed"] for entry in entries):
        raise ValueError("Manifest contains missing or failed runs")
    if len({(entry["seed"], entry["mode"]) for entry in entries}) != len(entries):
        raise ValueError("Repeated seed/mode pairs")
    if "seeds" in manifest or "modes" in manifest:
        seeds, modes = manifest.get("seeds", []), manifest.get("modes", [])
        if not seeds or not modes or len(set(seeds)) != len(seeds) or len(set(modes)) != len(modes):
            raise ValueError("Invalid planned seed/mode matrix")
        planned = {(seed, mode) for seed in seeds for mode in modes}
        if {(entry["seed"], entry["mode"]) for entry in entries} != planned:
            raise ValueError("Incomplete planned seed/mode matrix")
    root = Path(args.manifest).parent.parent
    weeks = args.weeks or manifest["weeks"]
    if weeks > manifest["weeks"]:
        raise ValueError("--weeks cannot exceed the manifest horizon")
    runs = [audit(entry, weeks, root) for entry in entries]
    result = {"assemblySha256": manifest["assemblySha256"], "runs": runs,
              "definition": "Annual medians include only twelve observed months per firm-year, including subsequent closures; late births are reported as partial. Tier is December tier. Management tax base expenses advances/recording when paid. This is an exploratory model, not historical validation."}
    Path(args.output).write_text(json.dumps(result, indent=2, allow_nan=False), encoding="utf-8")
    for run in runs:
        print(json.dumps({key: run[key] for key in ("name", "startingSmallClosedPct", "endpointActiveMedianCash")}))
        if "monthlyRows" in run:
            print(json.dumps({key: run[key] for key in ("monthlyRows", "maxCashReconciliationError", "netDistributionsToExternalOwners", "studioCapitalCash", "taxCashPaid")}))


if __name__ == "__main__":
    main()
