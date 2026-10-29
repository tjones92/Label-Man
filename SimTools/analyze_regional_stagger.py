"""RegionalSyncDesign.md §9s-9u stagger metric. Model analogue of hist_spread.py, 1960. 'City survey' = union of a city's Top40 + FullService playlists (ranks 1-35;
top 10 = rank <= 10 on any of its stations). 'Region survey' = the region's weekly cleared-units ranking of singles
(top 40 / top 10). Los Angeles / westcoast vs Minneapolis / greatplains, as in history. National = chart position.
Usage: model_spread.py SEEDS TAG [--source city|region]"""
import sys
import numpy as np
import pandas as pd
L = str(__import__("pathlib").Path(__file__).resolve().parents[1]).replace("\\", "/") + "/SimLogs/"
BIG5 = {f"label_000{i}" for i in range(1, 6)}
seeds = [int(x) for x in sys.argv[1].split(",")]
tag = sys.argv[2]
source = sys.argv[sys.argv.index("--source") + 1] if "--source" in sys.argv else "city"
A, B = ("Los Angeles", "Minneapolis") if source == "city" else ("westcoast", "greatplains")


def pre(seed):
    p = f"hs52op{seed}-SMP-HGRPO" + (f"-{tag}" if tag else "")
    import os
    if tag == "sp" and not os.path.exists(L + p + "-records.csv"): p = f"hs52op{seed}-SMP-HGRPO"
    return p


def lists(p):
    """Per (place, week, recordId): best rank."""
    if source == "city":
        x = pd.read_csv(L + p + "-station-playlists.csv", usecols=["week", "year", "city", "format", "rank", "recordId"])
        x = x[x.year.eq(1960) & x.format.isin(["Top40", "FullService"]) & x.city.isin([A, B])]
        return x.groupby(["city", "week", "recordId"])["rank"].min().reset_index().rename(columns={"city": "place"})
    x = pd.read_csv(L + p + "-single-demand-stages.csv", usecols=["week", "year", "recordId", "region", "clearedUnits"])
    x = x[x.year.eq(1960) & x.region.isin([A, B])]
    x["rank"] = x.groupby(["region", "week"]).clearedUnits.rank(ascending=False, method="first")
    return x[x["rank"].le(40)][["region", "week", "recordId", "rank"]].rename(columns={"region": "place"})


rows = []
for seed in seeds:
    p = pre(seed)
    r = pd.read_csv(L + p + "-records.csv", usecols=["week", "year", "recordId", "labelId", "currentPosition"])
    r = r[r.year.eq(1960)]
    c = r[r.currentPosition.between(1, 100)]
    nat = c.groupby("recordId").agg(debut=("week", "min"), peak=("currentPosition", "min"), labelId=("labelId", "first"))
    nat = nat[nat.debut.between(7, 46)]  # analogue of history's Feb-Nov debut window
    li = lists(p)
    first = li.groupby(["place", "recordId"]).week.min().unstack(0)
    first10 = li[li["rank"].le(10)].groupby(["place", "recordId"]).week.min().unstack(0)
    d = nat.join(first.rename(columns={A: "a", B: "b"})[["a", "b"]] if len(first) else None)
    d = d.join(first10.rename(columns={A: "a10", B: "b10"}).reindex(columns=["a10", "b10"]))
    d["seed"] = seed
    rows.append(d.reset_index())
d = pd.concat(rows)
d["firstCity"] = d[["a", "b"]].min(axis=1)
d["lead"] = d.debut - d.firstCity
d["gap"] = (d.a - d.b).abs()
d["cls"] = np.where(d.labelId.isin(BIG5), "Big-5", "Indie")


def summarize(g, name):
    both = g.a.notna() & g.b.notna()
    one = g.a.notna() ^ g.b.notna()
    br = ((g.a10 < g.b) | (g.b10 < g.a))[both].sum()
    return dict(group=name, n=len(g), onAny=g.firstCity.notna().mean() * 100, both=both.mean() * 100,
                oneOnly=one.mean() * 100, leadMed=g.lead.median(), leadGE2=(g.lead >= 2).mean() * 100,
                gapMed=g.gap[both].median(), gapGE3=(g.gap[both] >= 3).mean() * 100,
                gapGE6=(g.gap[both] >= 6).mean() * 100, cityBreak=br / max(1, both.sum()) * 100)


pd.set_option("display.width", 220)
tab = [summarize(d, "all Hot 100 debuts"), summarize(d[d.peak.le(40)], "national top 40"),
       summarize(d[d.peak.le(10)], "national top 10"),
       summarize(d[d.peak.le(10) & d.cls.eq("Big-5")], "top 10, Big-5"),
       summarize(d[d.peak.le(10) & d.cls.eq("Indie")], "top 10, indie"),
       summarize(d[d.peak.between(41, 100)], "peak 41-100")]
print(f"{tag or 'control'} {source} ({A} vs {B}), seeds {seeds}")
print(pd.DataFrame(tab).round(1).to_string(index=False))
