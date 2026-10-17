"""Compare regional-label failure risk and financial paths without targeting universal survival.

Usage: python SimTools/analyze_regional_survival.py CONTROL TREATMENT
Initial labels are matched by immutable ID and name. Runtime births are compared
as separate age/exposure cohorts because their RNG paths and identities diverge.
"""
import argparse
import json
import re
from pathlib import Path

import pandas as pd


def read_run(prefix):
    log = Path(prefix + "-console.log").read_text(encoding="utf-8", errors="replace")
    complete = re.search(r"CHART_AUDIT_COMPLETE run=\S+ weeks=(\d+)", log)
    if not complete:
        raise ValueError("Incomplete run: " + prefix)
    finance = pd.read_csv(prefix + "-label-finance.csv")
    first = finance.groupby("labelId", sort=False).first()
    last = finance.groupby("labelId", sort=False).last()
    closed = finance[finance.status == "Defunct"].groupby("labelId").first().reset_index()
    events = {}
    week = 1
    for line in log.splitlines():
        event = re.search(r"\[LabelManager\] (.*) has closed\. Reason: ([^.]+)", line)
        if event:
            match = closed[(closed.labelName == event[1]) & (closed.week == week)]
            if len(match) != 1:
                raise ValueError(f"Ambiguous closure {week}: {event[1]}")
            events[match.iloc[0].labelId] = (week, event[2])
        if line.startswith("Advanced to Friday,"):
            week += 1
    if week != int(complete[1]) + 1:
        raise ValueError("Unexpected week sequence")
    settlement = pd.read_csv(prefix + "-completed-week-settlement.csv", usecols=[
        "week", "labelId", "format", "totalUnits", "labelNet"])
    singles = settlement[settlement["format"] == "Single"].groupby("labelId").totalUnits.sum()
    records = pd.read_csv(prefix + "-records.csv", usecols=["recordId", "labelId", "quality"])
    quality = records.drop_duplicates("recordId").groupby("labelId").quality.mean()
    out = first[["labelName", "labelTier", "archetype", "isHistorical"]].copy()
    out["birthWeek"] = first.week
    out["endStatus"] = last.status
    out["endTier"] = last.labelTier
    out["endCash"] = last.cashReserves
    out["singleUnits"] = singles.reindex(out.index).fillna(0)
    out["meanSingleQuality"] = quality.reindex(out.index)
    out["closeWeek"] = [events.get(i, (None, None))[0] for i in out.index]
    out["fate"] = [events.get(i, (None, "observed"))[1] for i in out.index]
    out["bankrupt"] = out.fate == "Bankruptcy"
    alive = finance[~finance.status.isin(["Defunct", "Acquired"])]
    out["exposureWeeks"] = alive.groupby("labelId").size().reindex(out.index).fillna(0)
    out["lifetimeBookedNet"] = finance.groupby("labelId").weeklyNet.sum().reindex(out.index).fillna(0)
    details = []
    for label_id, row in out[out.bankrupt].iterrows():
        history = alive[alive.labelId == label_id]
        before = history[history.week <= row.closeWeek]
        recent = before[before.week > row.closeWeek - 13]
        prior = before.iloc[-1] if len(before) else finance[finance.labelId == label_id].iloc[0]
        closure = finance[(finance.labelId == label_id) & (finance.week == row.closeWeek)].iloc[0]
        details.append({
            "labelId": label_id, "labelName": row.labelName, "birthTier": row.labelTier,
            "birthWeek": int(row.birthWeek), "closeWeek": int(row.closeWeek),
            "ageWeeks": int(row.closeWeek - row.birthWeek + 1), "singleUnits": int(row.singleUnits),
            "meanSingleQuality": None if pd.isna(row.meanSingleQuality) else float(row.meanSingleQuality),
            "lastCash": float(prior.cashReserves),
            "cashAtClosure": float(closure.cashReserves),
            "lastReceivables": float(prior.outstandingWholesaleReceivables),
            "lifetimeBookedNet": float(before.weeklyNet.sum()),
            "recent13WeekBookedNet": float(recent.weeklyNet.sum()),
            "recentCashChange": float(recent.cashReserves.iloc[-1] - recent.cashReserves.iloc[0]) if len(recent) > 1 else 0,
        })
    return out, details


def cohort(frame):
    exposure = float(frame.exposureWeeks.sum())
    banks = int(frame.bankrupt.sum())
    sellers = frame[frame.singleUnits > 0]
    alive = ~frame.endStatus.isin(["Defunct", "Acquired"])
    return {
        "labels": len(frame), "bankruptcies": banks,
        "bankruptFraction": banks / len(frame) if len(frame) else None,
        "closures": int(frame.endStatus.eq("Defunct").sum()),
        "acquired": int(frame.endStatus.eq("Acquired").sum()),
        "survivors": int(alive.sum()),
        "survivalFraction": float(alive.mean()) if len(frame) else None,
        "survivingEndTiers": {str(k): int(v) for k, v in frame.loc[alive, "endTier"].value_counts().items()},
        "exposureLabelWeeks": exposure,
        "bankruptciesPer1000LabelWeeks": banks / exposure * 1000 if exposure else None,
        "competitiveExits": int(frame.fate.eq("Competitive exit").sum()),
        "medianObservedAgeWeeks": float(frame.exposureWeeks.median()) if len(frame) else None,
        "singleSellers": len(sellers), "bankruptSingleSellers": int(sellers.bankrupt.sum()),
        "medianSellerUnits": float(sellers.singleUnits.median()) if len(sellers) else None,
    }


def compare(control, treatment):
    a, ad = read_run(control)
    b, bd = read_run(treatment)
    cohorts = {}
    for name, predicate in [
        ("launchSmall", lambda f: (f.labelTier == "Small") & (f.birthWeek == 1)),
        ("runtimeSmall", lambda f: (f.labelTier == "Small") & (f.birthWeek > 1)),
        ("allSmall", lambda f: f.labelTier == "Small"),
    ]:
        cohorts[name] = {"control": cohort(a[predicate(a)]), "regional": cohort(b[predicate(b)])}
    match = a[(a.birthWeek == 1) & (a.labelTier == "Small")].join(
        b[(b.birthWeek == 1) & (b.labelTier == "Small")], how="inner", lsuffix="Control", rsuffix="Regional")
    if not match.labelNameControl.eq(match.labelNameRegional).all():
        raise ValueError("Initial label identities differ")
    changed = match[match.bankruptControl != match.bankruptRegional]
    pairs = []
    lookup_a, lookup_b = {r["labelId"]: r for r in ad}, {r["labelId"]: r for r in bd}
    for label_id, row in changed.iterrows():
        pairs.append({
            "labelId": label_id, "name": row.labelNameControl,
            "controlFate": row.fateControl, "regionalFate": row.fateRegional,
            "controlUnits": int(row.singleUnitsControl), "regionalUnits": int(row.singleUnitsRegional),
            "controlMeanQuality": None if pd.isna(row.meanSingleQualityControl) else float(row.meanSingleQualityControl),
            "regionalMeanQuality": None if pd.isna(row.meanSingleQualityRegional) else float(row.meanSingleQualityRegional),
            "controlBankruptcy": lookup_a.get(label_id), "regionalBankruptcy": lookup_b.get(label_id),
        })
    return {"control": control, "regional": treatment, "cohorts": cohorts,
            "matchedLaunchSmallLabels": len(match),
            "bankruptOnlyControl": int((match.bankruptControl & ~match.bankruptRegional).sum()),
            "bankruptOnlyRegional": int((~match.bankruptControl & match.bankruptRegional).sum()),
            "changedInitialFailures": pairs,
            "regionalSmallBankruptcies": [r for r in bd if r["birthTier"] == "Small"]}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("control")
    parser.add_argument("treatment")
    args = parser.parse_args()
    print(json.dumps(compare(args.control, args.treatment), indent=2))
