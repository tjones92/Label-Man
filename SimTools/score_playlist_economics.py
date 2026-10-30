"""Demand and label-tier unit shares, 1960, paired by seed against the first tag.
Complements score_big5_handoff.py (Big-5 chart share, airplay, artists) and score_bankruptcies.py.
Usage: score_playlist_economics.py SEEDS BASE TAG [TAG ...]"""
import os, sys
import pandas as pd
L = "C:/Project/Label-Man/SimLogs/"


def row(tag, seed):
    pre = L + f"hs52op{seed}-SMP-HGRPO-{tag}"
    console = L + f"regprobe/hs52op{seed}-SMP-HGRPO-{tag}-console.log"
    if not os.path.exists(console) or "CHART_AUDIT_COMPLETE" not in open(console, errors="replace").read(): return None
    r = pd.read_csv(pre + "-records.csv", usecols=["week", "year", "labelId", "currentPosition", "unitsThisWeek"])
    r = r[r.year.eq(1960)]
    f = pd.read_csv(pre + "-label-finance.csv", usecols=["week", "year", "labelId", "labelTier", "status"])
    f = f[f.year.eq(1960)]
    # Tier as of each week: promotions and closures move a label between buckets.
    r = r.merge(f[["week", "labelId", "labelTier"]], on=["week", "labelId"], how="left")
    units = r.unitsThisWeek.sum()
    tiers = r.groupby(r.labelTier.fillna("?")).unitsThisWeek.sum() / units * 100
    last = f[f.week.eq(f.week.max())]
    return dict(arm=tag, seed=seed, unitsM=units / 1e6,
                chartUnitsM=r.unitsThisWeek[r.currentPosition.between(1, 100)].sum() / 1e6,
                operating=int(last.status.ne("Defunct").sum()),
                **{f"share{t}": v for t, v in tiers.items()})


seeds = [int(s) for s in sys.argv[1].split(",")]
tags = sys.argv[2:]
d = pd.DataFrame([x for tag in tags for s in seeds if (x := row(tag, s))]).fillna(0)
metrics = [c for c in d.columns if c not in ("arm", "seed")]
pd.set_option("display.width", 250)
print(d.round(2).to_string(index=False))
print("\nmeans\n" + d.groupby("arm", sort=False)[metrics].mean().round(2).to_string())
ctl = d[d.arm.eq(tags[0])].set_index("seed")[metrics]
out = {}
for arm, g in d.groupby("arm", sort=False):
    if arm == tags[0]: continue
    dd = (g.set_index("seed")[metrics] - ctl).dropna()
    out[arm] = {m: f"{dd[m].mean():+.2f} ({dd[m].std() / len(dd) ** .5:.2f})" for m in metrics} | {"n": len(dd)}
print(f"\nd paired vs {tags[0]} (SE)\n" + pd.DataFrame(out).T.to_string())
