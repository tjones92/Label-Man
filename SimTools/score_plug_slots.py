"""Promotion capacity screen (Big5ChartOpportunityInvestigation.md, "Promotion capacity"). Usage from the repo root:
python SimTools/score_plug_slots.py ARM [ARM ...]       (ARM = e.g. plug4; reads SimLogs/ARM-{1001,2002,3003,4004}-sc-*)
Per arm, pooled over seeds: Big-5 singles launched in weeks 1-92 by plug outcome (from PROMO_PLUG lines in the run log),
with Hot 100 and Top 10 within launch +12; then Top 10 titles a year and per issue for each tier (history: Big-5 ~20 of
~1,080, about 1.9%; all others ~85 of ~4,950, about 1.7%)."""
import os
import re
import sys
import pandas as pd

SEEDS = (1001, 2002, 3003, 4004)
B5 = {f"label_000{i}" for i in range(1, 6)}
PLUG = re.compile(r"PROMO_PLUG week=(\d+) label=(\S+) tier=(\S+) record=(\S+) artist=(\S*) career=(\S*) hit=(\S+) quality=(\S+) outcome=(\S+)")


def csv(path):
    return path if os.path.exists(path) or not os.path.exists(path + ".gz") else path + ".gz"


def one(arm, seed):
    run = f"{arm}-{seed}-sc"
    a = pd.read_csv(csv(f"SimLogs/{run}-single-launch-attribution.csv"), usecols=["week", "singleId", "labelId", "career"])
    q = pd.read_csv(csv(f"SimLogs/{run}-single-launch-quality.csv"), usecols=["singleId", "tier"])
    r = pd.read_csv(csv(f"SimLogs/{run}-records.csv"), usecols=["week", "recordId", "currentPosition"])
    a = a.merge(q, on="singleId", how="left")
    a = a[a.week <= 92].copy()
    c = r[r.currentPosition.between(1, 100)].merge(a[["singleId", "week"]].rename(columns={"singleId": "recordId", "week": "lw"}), on="recordId")
    peak = c[c.week <= c.lw + 12].groupby("recordId").currentPosition.min()
    a["peak"] = a.singleId.map(peak).fillna(0)
    a["h100"] = a.peak > 0
    a["t10"] = a.peak.between(1, 10)
    log = f"SimLogs/writing-calibration/{run}.log"
    plugs = {}
    if os.path.exists(log):
        with open(log, encoding="utf-8", errors="ignore") as f:
            for line in f:
                m = PLUG.search(line)
                if m: plugs[m.group(4).strip('"')] = (m.group(9), m.group(7) == "True")
    a["outcome"] = a.singleId.map(lambda x: plugs.get(x, ("NotRouted", None))[0])
    a["hit"] = a.singleId.map(lambda x: plugs.get(x, (None, None))[1])
    a.loc[a.labelId.isin(B5), "tier"] = "Big5"
    a.loc[a.tier.isin(["Small", "Boutique"]), "tier"] = "Small"
    a["seed"] = seed
    return a


for arm in sys.argv[1:]:
    a = pd.concat(one(arm, s) for s in SEEDS)
    print(f"== {arm}: Big-5 singles by plug outcome (launches wk 1-92, +12, per seed)")
    b = a[a.tier == "Big5"]
    t = b.groupby(["outcome", "hit"], dropna=False).agg(n=("t10", "size"), h100=("h100", "mean"), t10=("t10", "mean"))
    t["n"] /= len(SEEDS); t["h100"] *= 100; t["t10"] *= 100
    print(t.round(2))
    y = a.groupby("tier").agg(n=("t10", "size"), t10=("t10", "sum"))
    y = y * 52 / 92 / len(SEEDS)
    y["per issue %"] = 100 * y.t10 / y.n
    f = y.drop("Big5", errors="ignore").sum()
    print(y.round(1))
    print(f"   field: {f.n:.0f} singles a year, {f.t10:.1f} Top 10 titles, {100 * f.t10 / f.n:.2f}% per issue; "
          f"Big-5 / field per issue {y.loc['Big5', 'per issue %'] / (100 * f.t10 / f.n):.2f}x")
    print()
