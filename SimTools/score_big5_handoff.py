"""Big-5 handoff, 1960: per arm and seed, Big-5 (label_0001-0005) share of Top-10 slots, the share in a top 10 ranked by
units in the same run, the offset (actual - units-only), Big-5 share of all units in the file and of chart-top-100 units,
airplay (pooled top 40) and distinct top-10 artists. Paired deltas against the first and second tags.
Usage: big5.py SEEDS TAG [TAG ...]   ('' = HGRPO control)"""
import os, sys
import pandas as pd
L = "C:/Project/Label-Man/SimLogs/"
BIG5 = {f"label_000{i}" for i in range(1, 6)}


def top10_big5(c, key):
    t = c.sort_values(["week", key], ascending=[True, False]).groupby("week").head(10)
    return t.labelId.isin(BIG5).mean() * 100


def row(tag, seed):
    pre = f"hs52op{seed}-SMP-HGRPO" + (f"-{tag}" if tag else "")
    if tag == "sp" and not os.path.exists(L + pre + "-records.csv"): pre = f"hs52op{seed}-SMP-HGRPO"
    if not os.path.exists(L + pre + "-records.csv"): return None
    cols = ["week", "year", "recordId", "artistId", "labelId", "currentPosition", "unitsThisWeek", "unsampledChartPoints"]
    r = pd.read_csv(L + pre + "-records.csv", usecols=cols)
    r = r[r.year.eq(1960)]
    c = r[r.currentPosition.between(1, 100)].copy()
    t40, t10 = c[c.currentPosition.le(40)], c[c.currentPosition.le(10)]
    act, sales = top10_big5(c.assign(k=-c.currentPosition), "k"), top10_big5(c, "unitsThisWeek")
    b = r.labelId.isin(BIG5)
    return dict(arm="control" if tag in ("", "sp") else tag, seed=seed, big5=act, unitsOnly=sales, offset=act - sales,
                unitShareAll=r.unitsThisWeek[b].sum() / r.unitsThisWeek.sum() * 100,
                unitShareT100=c.unitsThisWeek[c.labelId.isin(BIG5)].sum() / c.unitsThisWeek.sum() * 100,
                airplay=(1 - t40.unitsThisWeek.sum() / t40.unsampledChartPoints.sum()) * 100,
                artists=t10.artistId.nunique())


seeds = [int(s) for s in sys.argv[1].split(",")]
tags = sys.argv[2:]
d = pd.DataFrame([x for tag in tags for s in seeds if (x := row(tag, s))])
pd.set_option("display.width", 250)
print(d.round(1).to_string(index=False))
metrics = ["big5", "unitsOnly", "offset", "unitShareAll", "unitShareT100", "airplay", "artists"]
print("\nmeans\n" + d.groupby("arm", sort=False)[metrics].mean().round(1).to_string())
for base in ["control" if t in ("", "sp") else t for t in tags[:2]]:
    ctl = d[d.arm.eq(base)].set_index("seed")[metrics]
    out = {}
    for arm, g in d.groupby("arm", sort=False):
        if arm == base: continue
        dd = (g.set_index("seed")[metrics] - ctl).dropna()
        out[arm] = {m: f"{dd[m].mean():+.1f} ({dd[m].std() / len(dd) ** .5:.1f})" for m in metrics} | {"n": len(dd)}
    print(f"\nd paired vs {base} (SE)\n" + pd.DataFrame(out).T.to_string())
