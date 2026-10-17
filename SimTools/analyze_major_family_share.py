"""Decompose chart-unit concentration, tier migration and Independent failures.

Reads existing audits; never runs or changes the simulation. Example:
python SimTools/analyze_major_family_share.py SimLogs/ab5ctl-2002-260 PREFIX --output report.json

Published share uses year-end owner/tier. Frozen-tier share keeps each owner's
first finance-observed tier (including runtime births), not its current tier.
Matching failure exposure across arms is restricted to stable launch label IDs.
"""
import argparse
import json
from pathlib import Path

import pandas as pd

TIERS = ["Small", "Boutique", "Independent", "MidTier", "Major"]
FAMILY = ["MidTier", "Major"]
CLOSED = ["Bankrupt", "Defunct"]
INACTIVE = CLOSED + ["Acquired"]


def shares(frame, column, total):
    return {t: float(100 * frame.loc[frame[column].eq(t), "unitsThisWeek"].sum() / total)
            for t in TIERS}


def analyze(prefix):
    fin = pd.read_csv(prefix + "-label-finance.csv")
    first = fin.groupby("labelId", sort=False).first()
    launch = first[first.week.eq(1)]
    transitions = pd.read_csv(prefix + "-label-tier-transitions.csv")
    ledger = pd.read_csv(prefix + "-deal-ledger.csv")
    conc = pd.read_csv(prefix + "-concentration.csv").set_index("year")
    cols = ["week", "year", "recordId", "artistId", "labelId", "labelTier", "genre",
            "quality", "currentPosition", "unitsThisWeek", "radioHeat", "awareness", "weeksSinceRelease"]
    chunks = []
    for chunk in pd.read_csv(prefix + "-records.csv", usecols=cols, chunksize=150000):
        chunks.append(chunk[chunk.currentPosition.between(1, 100)].copy())
    rec = pd.concat(chunks, ignore_index=True)
    assert not rec.duplicated(["week", "recordId"]).any()
    strategies = pd.read_csv(prefix + "-release-strategy.csv", usecols=[
        "year", "recordId", "labelId", "tier", "artistId", "chosenFormat"])
    estimates = pd.read_csv(prefix + "-calibration-decisions.csv", usecols=["recordId", "qualityEstimate"])
    strategies = strategies.merge(estimates, on="recordId", how="left", validate="one_to_one")
    failures = fin[fin.status.isin(CLOSED)].groupby("labelId", sort=False).first()
    failure_rows = []
    for label_id, row in failures.iterrows():
        history = rec[rec.labelId.eq(label_id)]
        failure_rows.append({"labelId": label_id, "labelName": row.labelName,
            "week": int(row.week), "year": int(row.year), "tierAtClosure": row.labelTier,
            "birthTier": first.loc[label_id, "labelTier"], "launchLabel": label_id in launch.index,
            "priorChartUnits": int(history.loc[history.week.lt(row.week), "unitsThisWeek"].sum()),
            "priorChartRecords": int(history.loc[history.week.lt(row.week), "recordId"].nunique()),
            "allObservedChartUnits": int(history.unitsThisWeek.sum())})
    annual = {}
    for year, rf in rec.groupby("year"):
        yf = fin[fin.year.eq(year)]
        cutoff = int(yf.week.max())
        end = yf.groupby("labelId", sort=False).last()
        parents = dict(ledger.loc[ledger.year.le(year) & ledger.resolution.eq("Absorb"),
                                  ["clientId", "distributorId"]].itertuples(index=False, name=None))

        def owner(label_id):
            seen = set()
            while label_id in parents and label_id not in seen:
                seen.add(label_id)
                label_id = parents[label_id]
            return label_id

        r = rf.copy()
        r["owner"] = r.labelId.map(owner)
        r["endTier"] = r.owner.map(end.labelTier)
        r["birthTier"] = r.owner.map(first.labelTier)
        assert r.endTier.notna().all() and r.birthTier.notna().all()
        total = int(r.unitsThisWeek.sum())
        assert total == int(conc.loc[year, "totalChartUnits"]), (prefix, year, total)
        end_share = shares(r, "endTier", total)
        observed = sum(end_share[t] for t in FAMILY)
        assert abs(observed / 100 - conc.loc[year, "majorFamilyChartShare"]) < .000001, (prefix, year, observed)
        fixed = shares(r, "birthTier", total)
        at_week = shares(r, "labelTier", total)
        by_tier = {}
        for tier in TIERS:
            tr = r[r.endTier.eq(tier)]
            units_by_record = tr.groupby("recordId").unitsThisWeek.sum().sort_values(ascending=False)
            n = int(tr.recordId.nunique())
            ds = strategies[strategies.year.eq(year) & strategies.tier.eq(tier)]
            by_tier[tier] = {"units": int(tr.unitsThisWeek.sum()), "entries": n,
                "unitsPerEntry": float(tr.unitsThisWeek.sum() / n) if n else 0,
                "inRunReleaseChartUnits": int(tr.loc[tr.week.ge(tr.weeksSinceRelease), "unitsThisWeek"].sum()),
                "seededCatalogueChartUnits": int(tr.loc[tr.week.lt(tr.weeksSinceRelease), "unitsThisWeek"].sum()),
                "releaseStrategyDecisions": len(ds), "decisionArtists": int(ds.artistId.nunique()),
                "decisionFormats": {str(k): int(v) for k, v in ds.chosenFormat.value_counts().items()},
                "meanDecisionQualityEstimate": float(ds.qualityEstimate.mean()) if len(ds) else None,
                "top5ShareOfAllChartUnitsPct": float(100 * units_by_record.head(5).sum() / total),
                "genreUnits": {str(k): int(v) for k, v in tr.groupby("genre").unitsThisWeek.sum().items()},
                "unitWeightedQuality": float((tr.quality * tr.unitsThisWeek).sum() / max(1, tr.unitsThisWeek.sum()))}
        label_rows = []
        for label_id, g in r.groupby("owner"):
            label_rows.append({"labelId": label_id, "labelName": first.loc[label_id, "labelName"],
                "birthTier": first.loc[label_id, "labelTier"], "endTier": end.loc[label_id, "labelTier"],
                "units": int(g.unitsThisWeek.sum()), "sharePct": float(100 * g.unitsThisWeek.sum() / total),
                "entries": int(g.recordId.nunique())})
        cohort = {}
        for tier in TIERS:
            ids = launch.index[launch.labelTier.eq(tier)]
            e = end.loc[ids]
            live = e[~e.status.isin(INACTIVE)]
            cohort[tier] = {"initial": len(ids), "closed": int(e.status.isin(CLOSED).sum()),
                "acquired": int(e.status.eq("Acquired").sum()),
                "survivorTiers": {str(k): int(v) for k, v in live.labelTier.value_counts().items()}}
        live = end[~end.status.isin(INACTIVE)]
        ty = transitions[transitions.year.le(year)]
        annual[int(year)] = {"totalChartUnits": total, "endTierSharePct": end_share,
            "frozenBirthTierSharePct": fixed, "chartWeekTierSharePct": at_week,
            "familySharePct": observed, "frozenFamilySharePct": sum(fixed[t] for t in FAMILY),
            "netTierReclassificationPct": observed - sum(fixed[t] for t in FAMILY),
            "birthIndieNowFamilyPct": float(100 * r.loc[~r.birthTier.isin(FAMILY) & r.endTier.isin(FAMILY), "unitsThisWeek"].sum() / total),
            "birthFamilyNowIndiePct": float(100 * r.loc[r.birthTier.isin(FAMILY) & ~r.endTier.isin(FAMILY), "unitsThisWeek"].sum() / total),
            "acquiredOperatingIndieNowFamilyPct": float(100 * r.loc[~r.labelTier.isin(FAMILY) & r.endTier.isin(FAMILY) & r.owner.ne(r.labelId), "unitsThisWeek"].sum() / total),
            "cohorts": cohort,
            "liveTiers": {str(k): int(v) for k, v in live.labelTier.value_counts().items()},
            "cumulativeIndependentToMidTierPromotions": int((ty.fromTier.eq("Independent") & ty.toTier.eq("MidTier")).sum()),
            "cumulativeMidTierToIndependentDemotions": int((ty.fromTier.eq("MidTier") & ty.toTier.eq("Independent")).sum()),
            "closuresThisYearByFailureTier": {str(k): int(v) for k, v in failures[failures.year.eq(year)].labelTier.value_counts().items()},
            "absorptionsCumulative": len(parents), "tiers": by_tier,
            "labels": sorted(label_rows, key=lambda row: -row["units"])}
    entrants = first[first.week.gt(1)]
    final = fin.groupby("labelId", sort=False).last()
    runtime = {}
    for tier in TIERS:
        ids = entrants.index[entrants.labelTier.eq(tier)]
        runtime[tier] = {"births": len(ids), "closed": int(final.loc[ids].status.isin(CLOSED).sum())}
    result = {"prefix": prefix, "weeks": int(fin.week.max()), "annual": annual,
              "failures": failure_rows, "runtimeBirthCohorts": runtime}
    # Keep chart data only in memory for the cross-arm failure exposure diagnostic.
    return result, rec, launch, failures


def compare_failure_exposure(control, treatment):
    base, br, bl, bf = control
    trt, _, tl, tf = treatment
    assert bl.labelName.to_dict() == tl.labelName.to_dict(), "Launch identities differ"
    ids = tl.index[tl.labelTier.eq("Independent")]
    treatment_failed = ids.intersection(tf.index)
    extra = treatment_failed.difference(bf.index)
    output = {"control": base["prefix"], "treatment": trt["prefix"],
              "extraFailedStartingIndependentIds": list(extra), "annual": {}}
    for year, r in br.groupby("year"):
        total = int(r.unitsThisWeek.sum())
        # Only observed control sales AFTER the treatment's closure week.
        closure_week = r.labelId.map(tf.week)
        exposed = r[r.labelId.isin(extra) & r.week.ge(closure_week)]
        all_failed_sales = r[r.labelId.isin(treatment_failed)]
        output["annual"][int(year)] = {"controlChartUnitsAfterTreatmentExtraFailure": int(exposed.unitsThisWeek.sum()),
            "pctOfControlChartUnits": float(100 * exposed.unitsThisWeek.sum() / total),
            "controlAnnualShareOfAllTreatmentFailedStartingIndependentsPct": float(100 * all_failed_sales.unitsThisWeek.sum() / total)}
    output["interpretation"] = "Observed matched-launch-label exposure; not a causal effect or a forecast of the failed firms' unrealized sales. Runtime birth IDs are not matched."
    return output


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("prefix", nargs="+")
    parser.add_argument("--output")
    args = parser.parse_args()
    arms = [analyze(p) for p in args.prefix]
    output = {"arms": [a[0] for a in arms], "failureExposureVsFirstArm": [compare_failure_exposure(arms[0], a) for a in arms[1:]],
        "validation": "All reconstructed annual chart-unit totals and Major-family shares match published concentration.csv (share tolerance 0.000001)."}
    payload = json.dumps(output, indent=2, allow_nan=False)
    if args.output:
        Path(args.output).parent.mkdir(parents=True, exist_ok=True)
        Path(args.output).write_text(payload + "\n", encoding="utf-8")
        print(output["validation"])
        print(args.output)
    else:
        print(payload)


if __name__ == "__main__":
    main()
