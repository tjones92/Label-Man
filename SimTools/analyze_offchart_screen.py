"""Screen analyzer for the demand-probe arms (commercial fit, survival routes, Majors' off-chart base).

Reproduces the tables in DemandAttentionImplementation.md ("What the shares should be" onward) from
ChartAuditRunner output. Each arm is NAME=TAG:SEED[,TAG:SEED...]; TAG is the run tag, and files may sit in
SimLogs/ or SimLogs/<TAG>/. Seeds are reported per run, then as means per arm. Use the same seeds in each arm
when comparing them, and report seed-paired differences (see --paired).

    python SimTools/analyze_offchart_screen.py \
        "nopush=doc-nopush:1001,doc-nopush:2002,doc-np3:3003" \
        "oc12=doc-np-oc12r:1001,doc-np-oc12r:2002,doc-np-oc12r:3003" --trace --paired nopush:oc12

Measures (starting firms' initial tier, runtime firms' birth tier; Major = the Big-5 plus EMI and Decca UK):

- Single units: label-sales-weekly.csv, frozen under the seller at each completed settlement.
  Includes uncharted and retiring singles; excludes pre-window catalog sales. Older full audits
  can use completed-week-settlement.csv. --legacy-final-owner explicitly selects the biased old
  ownership method: lifetime units (potentially before the window) go to the final owner. Both
  methods now classify runtime firms from runtime-label-profiles.csv instead of leaving them unknown.
- Off-chart: offchart-singles.csv (--major-offchart-singles), cumulative per label. majorShareInclOff adds it
  to Major units and to the total.
- Top 10 shares: records.csv slots at positions 1-10 by year; smallFirmTop10 counts slots held by firms with
  under 10 slots that year.
- chartUnits/*: units of charted records in their chart weeks over all single units. A model diagnostic only:
  the ~88% "historical" share was withdrawn (DemandAttentionResearch.md §1).
- Closures: share of labels present in week 1 (label-finance.csv) whose final status is Bankrupt or Defunct.
- --trace (Majors): Rising share of label-weeks, launches from demand-release-census.csv, and the cash ledger
  from company-monthly.csv (campaigns, recording, advances, owner transfers, tax, payroll), plus the
  off-chart net after selling costs.
"""
import argparse
import os
import statistics
import sys

import pandas as pd

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "SimLogs")


def path(tag, seed, stream, weeks):
    name = f"{tag}-default-{seed}-{weeks}-{stream}.csv"
    flat = os.path.join(ROOT, name)
    return flat if os.path.exists(flat) else os.path.join(ROOT, tag, name)


def unquote(series):
    return series.astype(str).str.strip('"')


def label_tiers(p):
    directory = pd.read_csv(p("label-directory"))
    tiers = dict(zip(unquote(directory.labelId), unquote(directory.initialTier)))
    # The directory contains starting firms only. Runtime births have their own
    # immutable birth-tier stream, and must not disappear into an unknown bucket.
    if os.path.exists(p("runtime-label-profiles")):
        births = pd.read_csv(p("runtime-label-profiles"), usecols=["labelId", "birthTier"])
        for label, tier in zip(unquote(births.labelId), unquote(births.birthTier)):
            tiers.setdefault(label, tier)
    return tiers


def sales_units(p, legacy_final_owner=False):
    if not legacy_final_owner:
        if os.path.exists(p("label-sales-weekly")):
            sales = pd.read_csv(p("label-sales-weekly"))
            if sales.duplicated(["week", "labelId", "format"]).any():
                raise ValueError("Repeated weekly label/format sales rows")
            if ((sales.units < 0) | (sales.chartedUnits < 0) | (sales.chartedUnits > sales.units)
                    | (sales.top10Units < 0) | (sales.top10Units > sales.chartedUnits)).any():
                raise ValueError("Invalid weekly sales counts")
            return sales[unquote(sales.format) == "Single"].copy()
        if os.path.exists(p("completed-week-settlement")):
            sales = pd.read_csv(p("completed-week-settlement"), usecols=["labelId", "format", "totalUnits"])
            sales = sales.rename(columns={"totalUnits": "units"})
            return sales[unquote(sales.format) == "Single"].copy()
        raise ValueError("Weekly owner attribution unavailable: rerun this audit, or explicitly use "
                         "--legacy-final-owner to reproduce the biased historical table.")
    outcomes = pd.read_csv(p("release-outcomes"), usecols=["labelId", "format", "pressingBookSold"])
    live = pd.read_csv(p("live-records-snapshot"), usecols=["labelId", "format", "totalUnitsSold"])
    units = pd.concat([
        outcomes[unquote(outcomes.format) == "Single"].rename(columns={"pressingBookSold": "units"})[["labelId", "units"]],
        live[unquote(live.format) == "Single"].rename(columns={"totalUnitsSold": "units"})[["labelId", "units"]],
    ])
    return units


def screen(tag, seed, weeks, legacy_final_owner=False):
    p = lambda stream: path(tag, seed, stream, weeks)
    tier = label_tiers(p)
    units = sales_units(p, legacy_final_owner)
    if not legacy_final_owner and "week" in units:
        window = pd.read_csv(p("demand-observation-window"))
        if len(window) != 1 or str(window.iloc[0].completed).lower() != "true":
            raise ValueError("Demand observation did not complete")
        market = pd.read_csv(p("market-clearing-weekly"), usecols=["week", "clearedSingleUnits"])
        sold = units.groupby("week").units.sum()
        cleared = market.groupby("week").clearedSingleUnits.sum()
        if not sold.index.equals(cleared.index) or not (sold == cleared).all():
            raise ValueError("Weekly seller units do not reconcile to cleared single units")
    units["tier"] = unquote(units.labelId).map(tier)
    if units.tier.isna().any():
        raise ValueError("Sales reference labels absent from opening/birth tier metadata")
    by_tier = units.groupby("tier").units.sum()
    off_path = p("offchart-singles")
    off_by_tier = pd.Series(dtype="float64")
    if os.path.exists(off_path):
        off_rows = pd.read_csv(off_path)
        off_rows["tier"] = unquote(off_rows.labelId).map(tier)
        if off_rows.tier.isna().any():
            raise ValueError("Off-chart sales reference labels absent from label-directory.csv")
        off_by_tier = off_rows.groupby("tier").units.sum()
    off = off_by_tier.sum()
    live_total, major = by_tier.sum(), by_tier.get("Major", 0)
    r = {
        "liveSingles(M)": live_total / 1e6, "offChart(M)": off / 1e6,
        "majorLiveShare": major / live_total,
        "majorShareInclOff": (major + off_by_tier.get("Major", 0)) / (live_total + off),
    }
    for t in ("Small", "Boutique", "Independent", "MidTier"):
        r[f"{t.lower()}Share"] = (by_tier.get(t, 0) + off_by_tier.get(t, 0)) / (live_total + off)

    records = pd.read_csv(p("records"), usecols=["year", "labelId", "currentPosition", "unitsThisWeek"])
    records["tier"] = unquote(records.labelId).map(tier)
    top10 = records[(records.currentPosition > 0) & (records.currentPosition <= 10)]
    for year in sorted(top10.year.unique()):
        slots = top10[top10.year == year]
        r[f"majTop10_{year}"] = (slots.tier == "Major").mean()
        r[f"smallFirmTop10_{year}"] = slots.labelId.map(slots.labelId.value_counts()).lt(10).mean()
    charted = records[records.currentPosition > 0]
    chart_units = units.chartedUnits.sum() if "chartedUnits" in units else charted.unitsThisWeek.sum()
    r["chartUnits/live"] = chart_units / live_total
    r["chartUnits/all"] = chart_units / (live_total + off)
    r["no1Units(k)"] = records[records.currentPosition == 1].unitsThisWeek.mean() / 1e3
    r["majChartUnitShare"] = (units.loc[units.tier == "Major", "chartedUnits"].sum() / chart_units
                             if "chartedUnits" in units else
                             charted.unitsThisWeek[charted.tier == "Major"].sum() / charted.unitsThisWeek.sum())

    finance = pd.read_csv(p("label-finance"), usecols=lambda c: c in ("week", "labelId", "status", "cashReserves", "exitReason"))
    finance["labelId"], finance["status"] = unquote(finance.labelId), unquote(finance.status)
    start = finance[finance.week == finance.week.min()].labelId.unique()
    last = finance.sort_values("week").groupby("labelId").tail(1).set_index("labelId")
    for t in ("Small", "Boutique", "Independent"):
        ids = [i for i in start if tier.get(i) == t]
        r[f"closed_{t}"] = sum(last.loc[i, "status"] in ("Bankrupt", "Defunct") for i in ids) / max(1, len(ids))
        if "exitReason" in last:
            for reason in ("Bankruptcy", "Dormancy"):
                r[f"{reason.lower()}_{t}"] = sum(last.loc[i, "status"] in ("Bankrupt", "Defunct")
                                                    and last.loc[i, "exitReason"] == reason for i in ids) / max(1, len(ids))
    majors = [i for i in start if tier.get(i) == "Major"]
    r["majorEndCash(M)"] = last.loc[majors, "cashReserves"].sum() / 1e6
    return r


def trace(tag, seed, weeks):
    p = lambda stream: path(tag, seed, stream, weeks)
    tier = label_tiers(p)
    majors = lambda df: df[unquote(df.labelId).map(tier) == "Major"]
    finance = majors(pd.read_csv(p("label-finance"), usecols=["labelId", "status"]))
    census = majors(pd.read_csv(p("demand-release-census")))
    census = census[unquote(census.observationKind) == "Launch"]
    monthly = majors(pd.read_csv(p("company-monthly")))
    events = majors(pd.read_csv(p("label-operating-target-events")))
    off_path = p("offchart-singles")
    r = {
        "risingShare": (unquote(finance.status) == "Rising").mean(),
        "singleLaunches": (unquote(census.format) == "Single").sum(),
        "lpLaunches": (unquote(census.format) == "Album").sum(),
    }
    for k in ("recordGross", "campaigns", "recording", "artistAdvances", "capitalSpending", "ownerTransfersNet", "taxPaid", "payroll"):
        r[f"{k}(k)"] = monthly[k].sum() / 1e3
    r["campaign/launch"] = monthly.campaigns.sum() / max(1, len(census))
    r["orgCoverage"] = monthly.organizationCoverage.mean()
    r["organicGrowth"] = unquote(events.reason).str.contains("Organic").sum()
    r["offChartNet(k)"] = pd.read_csv(off_path).netAfterSellingCosts.sum() / 1e3 if os.path.exists(off_path) else 0.0
    return r


def collect(arms, weeks, measure):
    rows = {}
    for spec in arms:
        name, runs = spec.split("=", 1)
        for run in runs.split(","):
            tag, seed = run.split(":")
            rows[f"{name}/{seed}"] = measure(tag, int(seed), weeks)
    frame = pd.DataFrame(rows).T
    frame["arm"] = [index.split("/")[0] for index in frame.index]
    return frame


def show(frame, title):
    pd.set_option("display.width", 250)
    print(f"== {title}, per run")
    print(frame.drop(columns="arm").T.astype(float).round(3).to_string())
    print(f"== {title}, arm means")
    print(frame.groupby("arm", sort=False).mean(numeric_only=True).T.round(3).to_string())


def paired(frame, base, other):
    """Seed-paired difference (other - base): mean and standard error across shared seeds."""
    seeds = lambda arm: {i.split("/")[1]: i for i in frame.index if i.startswith(arm + "/")}
    a, b = seeds(base), seeds(other)
    shared = sorted(set(a) & set(b))
    if len(shared) < 2:
        print(f"== paired {other} - {base}: needs two or more shared seeds"); return
    print(f"== paired {other} - {base}, seeds {', '.join(shared)}: mean (SE)")
    for column in frame.columns.drop("arm"):
        d = [float(frame.loc[b[s], column]) - float(frame.loc[a[s], column]) for s in shared]
        print(f"  {column:24s} {statistics.mean(d):+.4f} ({statistics.stdev(d) / len(d) ** .5:.4f})")


def main():
    parser = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    parser.add_argument("arms", nargs="+", help="NAME=TAG:SEED[,TAG:SEED...]")
    parser.add_argument("--weeks", type=int, default=105)
    parser.add_argument("--trace", action="store_true", help="also report the Majors' release and cash trace")
    parser.add_argument("--legacy-final-owner", action="store_true", help="reproduce old, biased lifetime-owner shares")
    parser.add_argument("--paired", action="append", default=[], metavar="BASE:OTHER")
    args = parser.parse_args()
    attribution = "LEGACY final owner (biased)" if args.legacy_final_owner else "weekly settlement owner"
    print(f"Attribution: {attribution}")
    frames = [("screen", collect(args.arms, args.weeks,
                               lambda tag, seed, weeks: screen(tag, seed, weeks, args.legacy_final_owner)))]
    if args.trace: frames.append(("Major trace", collect(args.arms, args.weeks, trace)))
    for title, frame in frames:
        show(frame, title)
        for pair in args.paired: paired(frame, *pair.split(":"))
    return 0


if __name__ == "__main__":
    sys.exit(main())
