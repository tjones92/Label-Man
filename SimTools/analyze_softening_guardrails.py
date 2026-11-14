"""Guardrails for the footprint / craft softening screens (SimTools/MajorShareRootCauseHandoff.md).

usage: analyze_softening_guardrails.py SEEDS RUN_FORMAT [RUN_FORMAT ...]
       e.g. 2002,1001,3003,6006 "fund-v1-{seed}-gc_of" "soft-v1-{seed}-fb_ca"

A softening may narrow the Majors' edge, never remove it. Each block is one guardrail:
 G1 Big-5 1961 chart share and the per-single Top 10 premium, T10 share / launch share (history ~1.2x; accept 1.1-1.6);
    premiumOdds is the same against the field.
 G2/G7 Top 40 per connected single by quality band and tier, pooled (Big-5 >= Independent in every band; MidTier >= Independent).
 G3 median launch stock by quality band and tier (Big-5 still above).
 G5 1961 hit acts: Hot 100 debuts per hit act and Top 10 per debut, by tier (history 3.28 vs 2.18 debuts; 16% conversion both).
 G6 Big-5 and field 1961 record gross and pretax earnings ($M, mean of seeds); labels active at the end by tier.
G4 (the regional stagger) is a separate script.
"""
import os
import sys

import numpy as np
import pandas as pd

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import keyed_fit_draws as kfd  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
L = os.path.join(ROOT, "SimLogs") + os.sep
B5 = {f"label_000{i}" for i in range(1, 6)}
ORDER = ["Big5", "MidTier", "Independent", "Small/Bout"]
BANDS = [0, .70, .74, .78, 2]
BAND_NAMES = ["<.70", ".70-.74", ".74-.78", ".78+"]


def tiers(a):
    a.loc[a.labelId.isin(B5), "tier"] = "Big5"
    a.loc[a.tier.isin(["Small", "Boutique"]), "tier"] = "Small/Bout"
    return a


def launches(run, seed):
    p = L + run + "-"
    r = pd.read_csv(p + "records.csv", usecols=["week", "year", "recordId", "artistId", "labelId", "currentPosition"])
    a = pd.read_csv(p + "single-launch-attribution.csv")
    q = pd.read_csv(p + "single-launch-quality.csv", usecols=["singleId", "tier"])
    a = tiers(a.merge(q, on="singleId", how="left"))
    c = r[(r.year <= 1961) & r.currentPosition.between(1, 100)]
    e = a[(a.year <= 1961) & (a.week <= 92)].copy()
    e["t40"] = e.singleId.isin(set(c[c.currentPosition <= 40].recordId))
    # "Connected" must use the run's own fit shape (launch weight, upside), read from its request file.
    kfd.configure(*kfd.params_for_run(run))
    e["z"] = [kfd.national_z(seed, s) for s in e.singleId]
    act = e.actChartRecordAtLaunch if "actChartRecordAtLaunch" in e else pd.Series(0.0, index=e.index)
    if kfd.ACT_RECORD_WEIGHT > 0 and "actChartRecordAtLaunch" not in e:
        raise ValueError(f"{run}: act-record weight on but single-launch-attribution has no actChartRecordAtLaunch")
    e["conn"] = [kfd.national_log(z, w, ar) > kfd.UNEXPOSED_CAP for z, w, ar in zip(e.z, e.initialAwareness, act)]
    e["band"] = pd.cut(e.quality, BANDS, labels=BAND_NAMES, right=False)
    return r, a, e


def share_1961(r, a):
    c = r[(r.year == 1961) & r.currentPosition.between(1, 100)]
    out = {}
    for name, depth in (("T10", 10), ("T40", 40), ("H100", 100)):
        x = c[c.currentPosition <= depth]
        out[name] = 100 * x.labelId.isin(B5).mean()
    l61 = a[a.year == 1961]
    out["launch%"] = 100 * l61.labelId.isin(B5).mean()
    out["premium"] = out["T10"] / out["launch%"]
    out["premiumOdds"] = (out["T10"] / out["launch%"]) / ((100 - out["T10"]) / (100 - out["launch%"]))
    return out


def hit_acts_1961(r, a):
    c = r[(r.year == 1961) & r.currentPosition.between(1, 100)]
    debut = r[r.currentPosition.between(1, 100)].groupby("recordId").week.min()
    rec = c.groupby("recordId").agg(artistId=("artistId", "first"), labelId=("labelId", "first"), peak=("currentPosition", "min"))
    rec["debutYear"] = rec.index.map(r.groupby("recordId").year.min())
    tier = a.drop_duplicates("labelId").set_index("labelId").tier
    rec["tier"] = rec.labelId.map(tier).fillna("Independent")
    rec.loc[rec.labelId.isin(B5), "tier"] = "Big5"
    deb = rec[rec.debutYear == 1961]
    hit = set(rec[rec.peak <= 10].artistId)
    h = deb[deb.artistId.isin(hit)]
    g = h.groupby("tier").agg(acts=("artistId", "nunique"), debuts=("artistId", "size"), t10=("peak", lambda s: (s <= 10).sum()))
    g["debuts/act"] = g.debuts / g.acts
    g["T10/debut%"] = 100 * g.t10 / g.debuts
    return g


def finance_1961(run):
    m = pd.read_csv(L + run + "-company-monthly.csv", usecols=["year", "labelId", "tier", "active", "recordGross", "pretaxEarnings", "period"])
    y = m[m.year == 1961].copy()
    y["grp"] = np.where(y.labelId.isin(B5), "Big5", np.where(y.tier.eq("MidTier"), "MidTier", "field"))
    f = y.groupby("grp")[["recordGross", "pretaxEarnings"]].sum() / 1e6
    last = m[m.period == m.period.max()]
    last = last.assign(grp=np.where(last.labelId.isin(B5), "Big5", last.tier))
    active = last[last.active.astype(str).str.lower().isin(["true", "1"])].groupby("grp").size()
    return f, active


def main():
    seeds = [int(s) for s in sys.argv[1].split(",")]
    pd.set_option("display.width", 250)
    pd.set_option("display.max_columns", 40)
    for fmt in sys.argv[2:]:
        shares, hits, fins, acts, ev = [], [], [], [], []
        for seed in seeds:
            run = fmt.format(seed=seed)
            if not os.path.exists(L + run + "-records.csv"):
                print("missing", run); continue
            r, a, e = launches(run, seed)
            shares.append(dict(seed=seed, **share_1961(r, a)))
            hits.append(hit_acts_1961(r, a))
            f, act = finance_1961(run)
            fins.append(f); acts.append(act)
            ev.append(e)
        if not ev: continue
        print(f"\n######## {fmt}  seeds {seeds}")
        s = pd.DataFrame(shares).set_index("seed"); s.loc["mean"] = s.mean()
        print("G1 Big-5 1961 share %, launch share %, per-single T10 premium (odds)"); print(s.round(2).to_string())
        e = pd.concat(ev)
        c = e[e.conn]
        g2 = 100 * c.pivot_table(index="band", columns="tier", values="t40", aggfunc="mean", observed=False).reindex(columns=ORDER)
        n2 = c.pivot_table(index="band", columns="tier", values="t40", aggfunc="size", observed=False).reindex(columns=ORDER)
        print("G2/G7 Top 40 % of connected singles by quality band (pooled)"); print(g2.round(1).to_string())
        print("   n"); print(n2.fillna(0).astype(int).to_string())
        g3 = e.pivot_table(index="band", columns="tier", values="initialStock", aggfunc="median", observed=False).reindex(columns=ORDER)
        print("G3 median launch stock by quality band"); print(g3.round(0).to_string())
        h = pd.concat(hits).groupby(level=0)[["acts", "debuts", "t10"]].sum()
        h["debuts/act"] = h.debuts / h.acts; h["T10/debut%"] = 100 * h.t10 / h.debuts
        print("G5 1961 hit acts, pooled"); print(h.reindex(ORDER).round(2).to_string())
        f = sum(fins) / len(fins)
        print("G6 1961 record gross / pretax $M (mean of seeds)"); print(f.round(2).to_string())
        print("   labels active at end (mean)"); print((pd.concat(acts, axis=1).fillna(0).mean(axis=1)).round(1).to_string())


if __name__ == "__main__":
    main()
