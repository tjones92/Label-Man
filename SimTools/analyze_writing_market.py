"""Compare completed native writing-market screens; no eventual-peak lookahead.

Usage: python SimTools/analyze_writing_market.py RUN [RUN ...]
Requires --chart-history --followup-chart-trace --small-label-finance.
Hit cohorts exclude opening titles and require a full 52-week follow-up.
Supplier ledgers and orders are cumulative snapshots, never summed across weeks.
"""
import argparse
import csv
import json
import math
from collections import Counter
from pathlib import Path

LOGS = Path(__file__).resolve().parents[1] / "SimLogs"


def rows(run, suffix):
    path = LOGS / f"{run}-{suffix}.csv"
    if not path.exists():
        return []
    with path.open(encoding="utf-8-sig", newline="") as stream:
        return list(csv.DictReader(stream))


def latest(data, key):
    result = {}
    for row in sorted(data, key=lambda r: int(r["week"])):
        result[row[key]] = row
    return list(result.values())


def share(counts, fraction):
    values = sorted(counts.values(), reverse=True)
    return sum(values[:max(1, math.ceil(len(values) * fraction))]) / sum(values) if values else None


def summarize(run):
    chart = [r for r in rows(run, "records") if 1 <= int(r["currentPosition"]) <= 100]
    if not chart:
        raise ValueError(f"{run}: missing chart history")
    end = max(int(r["week"]) for r in chart)
    opening = {r["recordId"] for r in chart if int(r["week"]) == 0}
    first = {}
    for r in sorted(chart, key=lambda r: int(r["week"])):
        if int(r["currentPosition"]) <= 40:
            first.setdefault(r["recordId"], r)
    hits = [r for rid, r in first.items() if rid not in opening and int(r["week"]) > 0]
    counts = Counter(r["artistId"] for r in hits)
    slots = Counter(r["artistId"] for r in chart if int(r["week"]) > 0)
    cohort = {}
    for r in hits:
        cohort.setdefault(r["artistId"], int(r["week"]))
    mature = {a: w for a, w in cohort.items() if w + 52 <= end}
    subsequent = {a: sum(w < int(r["week"]) <= w + 52 and r["artistId"] == a for r in hits) for a, w in mature.items()}
    launches = rows(run, "single-launch-quality")
    launch_counts = Counter(r["artistId"] for r in launches if int(r["week"]) > 0)
    silent_after_hit = sum(not any(r["artistId"] == a and w < int(r["week"]) <= w + 52 for r in launches) for a, w in mature.items())
    finance = latest(rows(run, "label-finance"), "labelId")
    supplier_rows = rows(run, "writing-suppliers")
    suppliers = latest(supplier_rows, "writerId")
    orders = latest(rows(run, "writing-orders"), "id")
    commission = sum(float(r["commissionFees"]) for r in suppliers)
    order_fees = sum(float(r["fee"]) for r in orders)
    if abs(commission - order_fees) > max(1, .00001 * commission):
        raise ValueError(f"{run}: commission ledger does not reconcile")
    partners = latest([dict(r, key="|".join(r[k] for k in ("writerId", "labelId", "artistId"))) for r in rows(run, "writing-partnerships")], "key")
    concentrations = rows(run, "concentration")
    act_years = rows(run, "act-release-years")
    annual_silence = []
    for year in sorted({r["year"] for r in act_years}):
        signed = [r for r in act_years if r["year"] == year and int(r["signedWeeks"]) >= 26]
        annual_silence.append(dict(year=int(year), signedAtLeast26Weeks=len(signed), noSingles=sum(int(r["singles"]) == 0 for r in signed)))
    return dict(run=run, weeks=end, top40Entries=len(hits), hitActs=len(counts),
        top5PctActEntryShare=share(counts, .05), top10PctActSlotShare=share(slots, .10),
        mature52WeekHitActs=len(mature), noFurtherHit52=sum(v == 0 for v in subsequent.values()),
        oneToThreeFurtherHits52=sum(1 <= v <= 3 for v in subsequent.values()),
        fourPlusFurtherHits52=sum(v >= 4 for v in subsequent.values()),
        noSingleReleaseAfterHit52=silent_after_hit,
        singleReleases=sum(launch_counts.values()), releasingActs=len(launch_counts),
        annualSilentActs=annual_silence,
        labelsObserved=len(finance), labelsFailed=sum(r["status"] in ("Bankrupt", "Defunct") for r in finance),
        labelsNegativeCash=sum(float(r["cashReserves"]) < 0 for r in finance),
        ownerConcentration=concentrations,
        sourceMix=dict(Counter(r["songSource"] for r in rows(run, "song-material") if int(r["week"]) > 0)) or None,
        shortlistSelectedSourceMix=dict(Counter(r["selectedSource"] or "Unspecified" for r in rows(run, "release-judgements-shadow") if int(r["auditWeek"]) > 0)),
        suppliers=len(suppliers), orders=len(orders), pending=sum(r["status"] == "Pending" for r in orders),
        linkedPlanOrders=sum(bool(r.get('releasePlanId')) for r in orders),
        offeredDeliveries=sum(r.get('deliveryOffered','').lower() == 'true' for r in orders) if any('deliveryOffered' in r for r in orders) else None,
        delivered=sum(r["status"] == "Delivered" for r in orders),
        meanPendingSlotShare=sum(int(r["pending"]) for r in supplier_rows) / len(supplier_rows) if supplier_rows else None,
        unusedLicensedAtEnd=sum(int(r["licensed"]) for r in suppliers),
        recordedAtEnd=sum(int(r["recorded"]) for r in suppliers),
        activeContracts=sum(r["contract"] != "Freelance" for r in suppliers),
        retainedSuppliersObserved=len({r["writerId"] for r in supplier_rows if r["contract"] != "Freelance"}),
        top10PctSupplierRevenueShare=share({r["writerId"]: sum(float(r[k]) for k in ("commissionFees", "licenseFees", "retainerFees")) for r in suppliers}, .10),
        repeatRecordingPartnerships=sum(int(r["recordings"]) >= 2 for r in partners),
        commissionFees=commission, licenseFees=sum(float(r["licenseFees"]) for r in suppliers),
        retainerFees=sum(float(r["retainerFees"]) for r in suppliers))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("runs", nargs="+")
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()
    result = json.dumps([summarize(run) for run in args.runs], indent=2)
    if args.output:
        args.output.write_text(result + "\n", encoding="utf-8")
    else:
        print(result)
