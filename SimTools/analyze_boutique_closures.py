"""Trace starting-tier closures through monthly cash accounts and actual launches.

Uses existing audit files; changes no simulation behavior. Monthly cash bridges
exclude accrual COGS (already represented by receipts and plant payments).
Final-six-month diagnostics end at the first exit, excluding post-exit accounts.
Associations and arithmetic cash headroom are not behavioral counterfactuals.
"""
import argparse
import json
from pathlib import Path

import numpy as np
import pandas as pd

from analyze_offchart_screen import path


EXPENSES = ["plantPayments", "recording", "campaigns", "artistAdvances", "licensing",
            "otherOperating", "payroll", "premises", "retainers", "restructuring",
            "legacyOverhead", "ownerSalary", "taxPaid", "capitalSpending",
            "distributionOperations", "tradeSupport", "recordExcise"]
# This is observed spending, not necessarily fixed commitments: the experimental
# founder policy makes ownerSalary contingent. Mandatory quotes come from finance.
LABOR_AND_PREMISES = ["payroll", "premises", "retainers", "legacyOverhead", "ownerSalary"]
CLOSED = ["Bankrupt", "Defunct"]
SPECIALIST_ARCHETYPES = {"FolkBoutique", "JazzPrestige", "BluesRoots", "GospelPowerhouse", "CountrySpecialist"}


def cash_bridge(monthly):
    """Return cash change from mutually exclusive statement cash columns."""
    inputs = monthly[["recordReceipts", "serviceIncome", "financingNet", "ownerTransfersNet"]].sum(axis=1)
    return inputs - monthly[EXPENSES].sum(axis=1) + monthly.cashRounding


def checked(frame, keys, numeric):
    if frame.duplicated(keys).any():
        raise ValueError(f"Repeated rows: {keys}")
    if not np.isfinite(frame[numeric].to_numpy(dtype=float)).all():
        raise ValueError("Nonfinite numeric observations")


def label_details(directory, finance, monthly, census, sales):
    checked(directory, ["labelId"], [])
    checked(finance, ["labelId", "week"], ["week", "cashReserves", "monthlyOverhead"])
    checked(monthly, ["labelId", "period"], EXPENSES +
            ["week", "period", "openingCash", "closingCash", "cashRounding", "recordReceipts",
             "serviceIncome", "financingNet", "ownerTransfersNet", "cashReconciliationError"])
    residual = monthly.closingCash - monthly.openingCash - cash_bridge(monthly)
    if residual.abs().max() > .011 or monthly.cashReconciliationError.abs().max() > .011:
        raise ValueError("Monthly cash bridge does not reconcile")
    checked(census, ["recordId"], ["observedAuditWeek"])
    checked(sales, ["week", "labelId", "format"], ["week", "units"])
    if (sales.units < 0).any():
        raise ValueError("Negative weekly sales")
    finance = finance.sort_values("week")
    start = finance[finance.week.eq(finance.week.min())].set_index("labelId")
    end = finance.groupby("labelId").tail(1).set_index("labelId")
    tiers = directory.set_index("labelId").initialTier
    if not start.index.isin(tiers.index).all():
        raise ValueError("Starting label absent from directory")
    exits = finance[finance.status.isin(CLOSED)].groupby("labelId").head(1).set_index("labelId")
    monthly_groups = dict(tuple(monthly.sort_values("period").groupby("labelId")))
    launches = census[census.observationKind.eq("Launch")]
    launch_groups = dict(tuple(launches.groupby("labelId")))
    sales_groups = dict(tuple(sales.groupby("labelId")))
    rows = []
    for label_id, first in start.iterrows():
        last = end.loc[label_id]
        exit_row = exits.loc[label_id] if label_id in exits.index else None
        stop = int(exit_row.week) if exit_row is not None else int(finance.week.max())
        cause = str(exit_row.exitReason) if exit_row is not None else str(last.status)
        if exit_row is not None and cause not in ("Bankruptcy", "Dormancy"):
            raise ValueError("Closed label missing its exit cause")
        if label_id not in monthly_groups:
            raise ValueError("Starting label missing monthly accounts")
        accounts = monthly_groups[label_id]
        accounts = accounts[accounts.week.le(stop)]
        if accounts.empty:
            raise ValueError("No monthly account before exit")
        # Calendar months, not the last six available rows across a gap.
        tail = accounts[accounts.period.gt(accounts.period.max() - 6)]
        new = launch_groups.get(label_id, launches.iloc[:0])
        new = new[new.observedAuditWeek.le(stop)]
        sold = sales_groups.get(label_id, sales.iloc[:0])
        sold = sold[sold.week.le(stop)]
        opening = accounts.iloc[0]
        row = {
            "labelId": label_id, "name": first.labelName, "initialTier": tiers.loc[label_id],
            "archetype": first.archetype, "historical": bool(first.isHistorical),
            "cause": cause, "finalTier": last.labelTier, "exitWeek": stop if exit_row is not None else None,
            "observedMonths": len(accounts), "firstMonthOwnership": opening.ownership,
            "firstMonthEmployees": float(opening.employees), "firstMonthCash": float(opening.openingCash),
            "lastObservedCash": float(last.cashReserves),
            "firstMonthLaborAndPremises": float(opening[LABOR_AND_PREMISES].sum()),
            "firstWeekMandatoryOverhead": float(first.monthlyOverhead),
            "lastObservedMandatoryOverhead": float(exit_row.monthlyOverhead if exit_row is not None else last.monthlyOverhead),
            "exitCash": float(exit_row.cashReserves) if exit_row is not None else None,
            "lastSixMonthLaborAndPremisesMedian": float(tail[LABOR_AND_PREMISES].sum(axis=1).median()),
            "lastSixMonthEmployeesMedian": float(tail.employees.median()),
            "lastSixMonthNetOperatingCash": float((tail.recordReceipts + tail.serviceIncome
                - tail[[c for c in EXPENSES if c not in ("capitalSpending", "taxPaid")]].sum(axis=1)).sum()),
            "lastSixMonthRecordReceipts": float(tail.recordReceipts.sum()),
            "lastSixMonthLaborAndPremises": float(tail[LABOR_AND_PREMISES].sum().sum()),
            "lastSixMonthFounderSalary": float(tail.ownerSalary.sum()),
            "lastSixMonthServiceIncome": float(tail.serviceIncome.sum()),
            "lastSixMonthProjectCosts": float(tail[["recording", "campaigns", "artistAdvances", "licensing"]].sum().sum()),
            "exitReceivables": float(exit_row.get("outstandingWholesaleReceivables", 0)) if exit_row is not None else None,
            "exitPlantPayables": float(exit_row.get("outstandingPlantPayables", 0)) if exit_row is not None else None,
            "exitRecordedTaxPayable": float(accounts.iloc[-1].get("taxPayable", 0)) if exit_row is not None else None,
            "launches": len(new), "singleLaunches": int(new.format.eq("Single").sum()),
            "albumLaunches": int(new.format.eq("Album").sum()),
            "singleLaunchQualityMedian": nullable_median(new.loc[new.format.eq("Single"), "runtimeQualityAtLaunch"]),
            "albumLaunchQualityMedian": nullable_median(new.loc[new.format.eq("Album"), "runtimeQualityAtLaunch"]),
            "singleUnits": int(sold.loc[sold.format.eq("Single"), "units"].sum()),
            "albumUnits": int(sold.loc[sold.format.eq("Album"), "units"].sum()),
        }
        for column in EXPENSES + ["recordReceipts", "serviceIncome", "financingNet", "ownerTransfersNet"]:
            row["total_" + column] = float(accounts[column].sum())
        row["receivablesCoverExitCashPlantAndRecordedTax"] = bool(exit_row is not None and
            row["exitCash"] + row["exitReceivables"] - row["exitPlantPayables"] - row["exitRecordedTaxPayable"] > 0)
        rows.append(row)
    return pd.DataFrame(rows), float(residual.abs().max())


def early_quality(labels, census, chart):
    """Single launch weeks 1-13, Hot 100 observations through week 52, origin cohort.

    Does not assign a transferred master's historical sales to its final owner.
    Album chart telemetry is suppressed in these compact runs. This diagnostic
    therefore covers singles only; Hot 100 entry is not profitability.
    """
    origins = labels[labels.initialTier.eq("Boutique")].set_index("labelId")
    early = census[census.observationKind.eq("Launch") & census.format.eq("Single")
                   & census.observedAuditWeek.between(1, 13)
                   & census.labelId.isin(origins.index)].copy()
    early["originEventuallyBankrupt"] = early.labelId.map(origins.cause).eq("Bankruptcy")
    early["qualityBand"] = pd.cut(early.runtimeQualityAtLaunch,
        [-float("inf"), .4, .5, .6, .7, float("inf")], labels=["<=.4", "(.4,.5]", "(.5,.6]", "(.6,.7]", ">.7"])
    if early.qualityBand.isna().any():
        raise ValueError("Actual launch missing runtime quality")
    panel = chart[chart.week.le(52) & chart.currentPosition.between(1, 100)]
    early["chartedBy52"] = early.recordId.isin(panel.recordId)
    early["top10By52"] = early.recordId.isin(panel.loc[panel.currentPosition.le(10), "recordId"])
    rows = []
    for (fmt, quality, bankrupt), g in early.groupby(["format", "qualityBand", "originEventuallyBankrupt"], observed=True):
        rows.append({"format": fmt, "qualityBand": str(quality), "originEventuallyBankrupt": bool(bankrupt),
                     "launches": len(g), "chartedBy52": int(g.chartedBy52.sum()), "top10By52": int(g.top10By52.sum())})
    return rows


def summarize(labels):
    def group(frame):
        n = len(frame)
        return {"labels": n, "bankrupt": int(frame.cause.eq("Bankruptcy").sum()),
                "dormant": int(frame.cause.eq("Dormancy").sum()),
                "closedPct": float(frame.cause.isin(["Bankruptcy", "Dormancy"]).mean() * 100),
                "medianExitWeek": nullable_median(frame.exitWeek),
                **{c + "Median": nullable_median(frame[c]) for c in [
                    "firstMonthCash", "firstMonthLaborAndPremises", "lastSixMonthLaborAndPremisesMedian",
                    "firstWeekMandatoryOverhead", "lastObservedMandatoryOverhead",
                    "lastSixMonthEmployeesMedian", "lastSixMonthRecordReceipts", "lastSixMonthLaborAndPremises",
                    "lastSixMonthProjectCosts", "lastSixMonthNetOperatingCash", "singleLaunches", "albumLaunches",
                    "singleUnits", "albumUnits", "singleLaunchQualityMedian", "albumLaunchQualityMedian"]}}
    result = {"tiers": {str(k): group(g) for k, g in labels.groupby("initialTier")}}
    boutique = labels[labels.initialTier.eq("Boutique")].copy()
    boutique["firstMonthStaffed"] = boutique.firstMonthEmployees.gt(0)
    # A transparent proxy for curated intent, not a claim that genre determines
    # strategy or that these archetypes can never expand commercially.
    boutique["specialistProxy"] = boutique.archetype.isin(SPECIALIST_ARCHETYPES)
    for name in ["cause", "archetype", "firstMonthOwnership", "firstMonthStaffed", "finalTier", "specialistProxy"]:
        result["boutiqueBy_" + name] = {str(k): group(g) for k, g in boutique.groupby(name)}
    failed = boutique[boutique.cause.eq("Bankruptcy")]
    labor_premises_total = failed[["total_" + c for c in LABOR_AND_PREMISES]].sum().sum()
    result["boutiqueBankruptcyDiagnostics"] = {
        "bankrupt": len(failed), "neverLaunchedAlbum": int(failed.albumLaunches.eq(0).sum()),
        "lastSixMonthMedianEmployeesZero": int(failed.lastSixMonthEmployeesMedian.eq(0).sum()),
        "founderSalaryShareOfCumulativeLaborAndPremises": float(failed.total_ownerSalary.sum() / labor_premises_total) if labor_premises_total else None,
        "lastSixMonthOperatingCashNonnegativeAddingBackSalary": int((failed.lastSixMonthNetOperatingCash + failed.lastSixMonthFounderSalary).ge(0).sum()),
        "receivablesCoverExitCashPlantAndRecordedTax": int(failed.receivablesCoverExitCashPlantAndRecordedTax.sum()),
        "positiveCashAtEndDespiteBankruptcy": int(failed.lastObservedCash.gt(0).sum()),
    }
    return result


def nullable_median(series):
    value = series.median() if series.notna().any() else None
    return float(value) if value is not None else None


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("arms", nargs="+", help="NAME=TAG:SEED,...")
    parser.add_argument("--weeks", type=int, default=265)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    if args.weeks < 52:
        parser.error("At least 52 weeks are required for the common chart follow-up")
    args.output.mkdir(parents=True, exist_ok=True)
    results = {}
    for spec in args.arms:
        name, runs = spec.split("=", 1)
        for run in runs.split(","):
            tag, seed = run.split(":")
            p = lambda stream: path(tag, int(seed), stream, args.weeks)
            window = pd.read_csv(p("demand-observation-window"))
            if (len(window) != 1 or not bool(window.iloc[0].completed)
                    or int(window.iloc[0].startAuditWeek) != 0
                    or int(window.iloc[0].endAuditWeek) != args.weeks
                    or int(window.iloc[0].missingRecords) != 0):
                raise ValueError("A completed, unrestarted audit with no missing records is required")
            frames = [pd.read_csv(p(s)) for s in ["label-directory", "label-finance", "company-monthly",
                                                   "demand-release-census", "label-sales-weekly"]]
            labels, residual = label_details(*frames)
            labels.to_csv(args.output / f"{name}-{seed}-labels.csv", index=False)
            chart = pd.read_csv(p("records"), usecols=["recordId", "week", "currentPosition"])
            results[f"{name}/{seed}"] = {"maxMonthlyCashBridgeResidual": residual,
                "earlySingleLaunchQuality": early_quality(labels, frames[3], chart), **summarize(labels)}
            print(f"Analyzed {name}/{seed}", flush=True)
    (args.output / "results.json").write_text(json.dumps(results, indent=2, allow_nan=False), encoding="utf-8")
    print(f"Saved {len(results)} runs to {args.output}")


if __name__ == "__main__":
    main()
