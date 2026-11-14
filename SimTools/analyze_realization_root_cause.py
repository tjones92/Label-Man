"""Root cause of the label-size realization gap (SimTools/MajorShareRootCauseHandoff.md).

usage: analyze_realization_root_cause.py <run> [<run> ...]       (run = e.g. fund-v1-2002-gc_of; seed read from the name)

Singles launched weeks 1-92 of 1960-61. Big-5 = label_0001..label_0005 exactly.
The commercial-fit draw is recomputed for EVERY single with keyed_fit_draws.py (a replica of the keyed Philox draw;
r = 0.97 against the captured fit), so "connected" here is the record's own draw above the reference median, the same
test the sim uses (SingleCommercialFit.Connected), not an expectation from launch awareness.
Act heat at launch: cold = momentum and recognition both < 0.03; hot = either >= 0.15; warm otherwise.
"""
import re
import sys

import numpy as np
import pandas as pd

import keyed_fit_draws as kfd

B5 = {f"label_000{i}" for i in range(1, 6)}
ORDER = ["Big5", "MidTier", "Independent", "Small/Bout"]


def frame(run):
    p = f"SimLogs/{run}-"
    r = pd.read_csv(p + "records.csv", usecols=["year", "recordId", "currentPosition"])
    r = r[(r.year <= 1961) & r.currentPosition.between(1, 100)]
    a = pd.read_csv(p + "single-launch-attribution.csv")
    q = pd.read_csv(p + "single-launch-quality.csv", usecols=["singleId", "tier", "diagQuality", "careerState"])
    a = a[(a.year <= 1961) & (a.week <= 92)].merge(q, on="singleId", how="left")
    a.loc[a.labelId.isin(B5), "tier"] = "Big5"
    a.loc[a.tier.isin(["Small", "Boutique"]), "tier"] = "Small/Bout"
    a["h100"] = a.singleId.isin(set(r.recordId))
    a["t40"] = a.singleId.isin(set(r[r.currentPosition <= 40].recordId))
    a["t10w"] = a.singleId.map(r[r.currentPosition <= 10].groupby("recordId").size()).fillna(0)
    return a.reset_index(drop=True)


def enrich(run, a):
    seed = int(re.search(r"-(\d{4})-", run).group(1))
    p = f"SimLogs/{run}-"
    lf = pd.read_csv(p + "label-finance.csv", usecols=["week", "labelId", "nationalReach"])
    a = pd.merge_asof(a.sort_values("week"), lf.sort_values("week"), on="week", by="labelId", direction="backward")
    home = pd.read_csv(p + "label-geography.csv", usecols=["labelId", "homeRegion"]).drop_duplicates("labelId").set_index("labelId").homeRegion
    regions = sorted(pd.read_csv(p + "market-clearing-weekly.csv", usecols=["regionId"]).regionId.unique())
    a["z"] = [kfd.national_z(seed, r) for r in a.singleId]
    a["nl"] = [kfd.national_log(z, w) for z, w in zip(a.z, a.initialAwareness)]
    a["conn"] = a.nl > kfd.UNEXPOSED_CAP
    h = a.labelId.map(home)
    # Launch-exposed regions under the label's own reach (label home counted; the act's home is not logged).
    a["exposed"] = [sum(1 for g in regions if g == hh or kfd.launch_exposed(seed, r, g, rc))
                    for r, rc, hh in zip(a.singleId, a.nationalReach.fillna(0), h)]
    a["heat"] = np.where((a.artistMomentum < .03) & (a.effectiveRecognition < .03), "cold",
                         np.where((a.artistMomentum < .15) & (a.effectiveRecognition < .15), "warm", "hot"))
    return a


def report(run, a):
    print("==", run, len(a), "singles")
    g = a.groupby("tier").agg(n=("conn", "size"), z=("z", "mean"), connRate=("conn", "mean"), awareness=("initialAwareness", "mean"),
                              stock=("initialStock", "mean"), exposed=("exposed", "mean"), hotShare=("heat", lambda s: (s == "hot").mean()))
    g["launch%"] = 100 * g.n / g.n.sum()
    c = a[a.conn]
    g["H100|conn"] = c.groupby("tier").h100.mean()
    g["T40|conn"] = c.groupby("tier").t40.mean()
    g["T10wk|conn"] = c.groupby("tier").t10w.mean()
    g["T40|H100"] = a[a.h100].groupby("tier").t40.mean()
    print(g.reindex(ORDER).round(3).to_string())
    t = c.pivot_table(index="heat", columns="tier", values="t40", aggfunc="mean").reindex(columns=ORDER) * 100
    print("T40 % of connected singles by act heat"); print(t.round(1).to_string())
    t10 = a.pivot_table(index="tier", columns="heat", values="t10w", aggfunc="sum")
    print("share of the tier's Top 10 weeks by act heat %"); print((100 * t10.div(t10.sum(axis=1), axis=0)).reindex(ORDER).round(1).to_string())


def main():
    pd.set_option("display.width", 250)
    pd.set_option("display.max_columns", 40)
    for run in sys.argv[1:]:
        report(run, enrich(run, frame(run)))


if __name__ == "__main__":
    main()
