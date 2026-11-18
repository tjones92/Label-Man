"""Candidate-3 kill tests (Big5VolumeAndPremiumHandoff.md): arms pooled over seeds. Usage from the repo root:
py SimTools/analyze_footprint_scope.py SEEDS ARM [ARM ...]     (SEEDS = 2002,1001,3003,4004; ARM = run format, e.g. zfg-{s}-sc)
The first arm is the control. Pooled 1960-61 launches to week 92: Top 10 and Hot 100 premium (Big-5 / field) for newcomers
(NewSigning / Unsigned at launch) and established acts; Big-5 Top 10 / Top 40 / Hot 100 slot shares and CR4 by year (per-seed
mean and seeds below control); Big-5 newcomer Top 10 records; Big-5 weekly gross and net summed by year; labels defunct."""
import os
import sys
import numpy as np
import pandas as pd

B5 = {f"label_000{i}" for i in range(1, 6)}
NEW = {"NewSigning", "Unsigned"}


def csv(path):
    return path if os.path.exists(path) or not os.path.exists(path + ".gz") else path + ".gz"


def load(run):
    p = f"SimLogs/{run}-"
    a = pd.read_csv(csv(p + "single-launch-attribution.csv"), usecols=["week", "year", "singleId", "labelId", "career"])
    r = pd.read_csv(csv(p + "records.csv"), usecols=["year", "recordId", "labelId", "currentPosition"])
    c = r[r.currentPosition.between(1, 100)]
    a["peak"] = a.singleId.map(c.groupby("recordId").currentPosition.min())
    a["new"], a["b5"] = a.career.isin(NEW), a.labelId.isin(B5)
    o = {}
    for y in (1960, 1961):
        cy = c[c.year == y]
        for rung, cut in (("T10", 10), ("T40", 40), ("H100", 100)):
            o[f"{y} B5 {rung} share"] = 100 * cy[cy.currentPosition <= cut].labelId.isin(B5).mean()
        t10 = cy[cy.currentPosition <= 10]
        o[f"{y} CR4"] = 100 * t10.labelId.value_counts().head(4).sum() / len(t10)
        o[f"{y} B5 newcomer T10 records"] = int(((a.year == y) & a.b5 & a.new & (a.peak <= 10)).sum())
    f = pd.read_csv(csv(p + "label-finance.csv"), usecols=["week", "year", "labelId", "status", "weeklyGross", "weeklyNet"])
    for y in (1960, 1961):
        fy = f[(f.year == y) & f.labelId.isin(B5)]
        o[f"{y} B5 gross ($M)"] = fy.weeklyGross.sum() / 1e6
        o[f"{y} B5 net ($M)"] = fy.weeklyNet.sum() / 1e6
    o["defunct by wk 104"] = f[f.status == "Defunct"].labelId.nunique()
    return a[a.week <= 92], o


seeds = [int(s) for s in sys.argv[1].split(",")]
arms = sys.argv[2:]
per, pooled = {}, {}
for arm in arms:
    launches = []
    for s in seeds:
        a, o = load(arm.format(s=s))
        launches.append(a)
        per[(arm, s)] = o
    A = pd.concat(launches)
    for lab, sel in (("newcomer", A.new), ("established", ~A.new)):
        b, f = A[sel & A.b5], A[sel & ~A.b5]
        pooled[(arm, lab)] = dict(T10prem=(b.peak <= 10).mean() / (f.peak <= 10).mean(), H100prem=b.peak.notna().mean() / f.peak.notna().mean(),
                                  B5launches=len(b), B5T10=int((b.peak <= 10).sum()), B5T10perLaunch=100 * (b.peak <= 10).mean())
print("Pooled 1960-61 launches to week 92")
print(pd.DataFrame(pooled).T.round(2).to_string())
df = pd.DataFrame(per).T
ctrl = df.loc[arms[0]]
rows = []
for arm in arms:
    m = df.loc[arm]
    rows.append(pd.Series({k: f"{m[k].mean():.1f}" + ("" if arm == arms[0] else f" ({int((m[k].values < ctrl[k].values).sum())}/{len(seeds)} down)")
                           for k in df.columns}, name=arm))
print("\nPer-seed mean (seeds below control)")
print(pd.DataFrame(rows).T.to_string())
print("\nPer seed")
print(df.astype(float).round(1).to_string())
