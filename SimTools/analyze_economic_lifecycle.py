"""Cohort survival, cash-funded local firms and insolvency boundaries by completed year.

Usage: python SimTools/analyze_economic_lifecycle.py PREFIX [PREFIX ...]
National Single-chart participation is an observation, never an input to survival.
"""
import argparse
import json
import re
from pathlib import Path

import pandas as pd


def analyze(prefix):
    log = Path(prefix + "-console.log").read_text(encoding="utf-8", errors="replace")
    if not re.search(r"CHART_AUDIT_COMPLETE run=\S+ weeks=\d+", log):
        raise ValueError("Incomplete run: " + prefix)
    finance = pd.read_csv(prefix + "-label-finance.csv")
    financial_telemetry = "economicLabelLifecycle" in finance and finance.economicLabelLifecycle.eq(True).any()
    first = finance.groupby("labelId", sort=False).first()
    ids = first[(first.week == 1) & (first.labelTier == "Small")].index
    records = pd.read_csv(prefix + "-records.csv", usecols=["week", "year", "labelId", "recordId", "currentPosition"])
    annual = {}
    for year, year_finance in finance.groupby("year"):
        last = year_finance.sort_values("week").groupby("labelId").last()
        initial = last.loc[ids]
        active = ~initial.status.isin(["Bankrupt", "Defunct", "Acquired"])
        charted = set(records.loc[(records.week > year_finance.week.max() - 52) &
                                 (records.week <= year_finance.week.max()) & (records.currentPosition > 0), "labelId"])
        local = initial[active & ~initial.index.isin(charted)]
        r = {"startingSmall": len(ids), "survivors": int(active.sum()),
             "closures": int(initial.status.eq("Defunct").sum()),
             "acquired": int(initial.status.eq("Acquired").sum()),
             "survivingEndTiers": {str(k): int(v) for k, v in initial.loc[active, "labelTier"].value_counts().items()},
             "survivingWithoutNationalSingleChartInLast52Weeks": len(local),
             "allActiveLabels": int((~last.status.isin(["Bankrupt", "Defunct", "Acquired"])).sum())}
        if financial_telemetry:
            r["survivingWithoutSingleChartWithPositiveRecentCashFlow"] = int(local.recentMonthlyCashFlow.gt(0).sum())
            r["medianSurvivingWithoutSingleChartRecentCashFlow"] = float(local.recentMonthlyCashFlow.median()) if len(local) else None
        annual[int(year)] = r
    events = re.findall(r"LABEL_INSOLVENCY id=(\S+) cash=([-\d.]+) monthlyCashFlow=([-\d.]+) missedMonthEnds=(\d+)", log)
    invalid = [e for e in events if float(e[1]) >= 0 or int(e[3]) < 2]
    financial_closures = len(re.findall(r"has closed\. Reason: Bankruptcy", log))
    if financial_telemetry and (invalid or len(events) != financial_closures):
        raise ValueError("Financial closures lack valid insolvency evidence: " + prefix)
    return {"prefix": prefix, "economicFinancialTelemetry": bool(financial_telemetry), "annual": annual,
            "closures": len(re.findall(r"has closed\. Reason:", log)),
            "competitiveExits": len(re.findall(r"has closed\. Reason: Competitive exit", log)),
            "financialClosures": financial_closures,
            "insolvencyEvents": len(events),
            "invalidInsolvencyEvents": invalid}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("prefix", nargs="+")
    args = parser.parse_args()
    print(json.dumps([analyze(p) for p in args.prefix], indent=2))
