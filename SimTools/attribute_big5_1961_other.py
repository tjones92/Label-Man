"""LpGenreAllocationFollowup.md §92: who holds the 1961 Big-5 Top 10 slots of acts with no 1960 Top 10. From the repo root:
python SimTools/attribute_big5_1961_other.py SEEDS ARM [ARM ...]      (SEEDS = e.g. 1001,2002,3003,4004)
Points = 100 x slot-weeks / all 1961 Top 10 slot-weeks. History: SimLogs/historical-top10-1961 (operating labels), the act's
best 1960 Hot 100 peak from hot100_weekly. Sim: the act's best peak in sim year 1960; the record's launch career, opening
archetype and plug outcome (PROMO_PLUG lines in SimLogs/writing-calibration/ARM-SEED-sc-engine.log)."""
import os, re, sys
import pandas as pd
B5 = {f"label_000{i}" for i in range(1, 6)}
B5H = ["Columbia", "RCA Victor", "Capitol", "Decca", "Mercury"]
def gz(p): return p if os.path.exists(p) else p + ".gz"
def norm(a):
    a = re.split(r" with | and his | and her | featuring | & the ", str(a).lower())[0]
    return re.sub(r"[^a-z0-9]", "", a)
def band(p): return "none in 1960" if p > 100 else "1960 #41-100" if p > 40 else "1960 #11-40"
pd.set_option("display.width", 220)

w = pd.read_csv("SimLogs/historical-hot100-1960-64/hot100_weekly.csv", parse_dates=["chartDate"])
w["a"] = w.artist.map(norm)
best60 = w[w.chartDate.dt.year <= 1960].groupby("a").position.min()
h60 = pd.read_csv("SimLogs/historical-1960-top10/records_1960_final.csv"); hit60 = set(h60.artist.map(norm))
h61 = pd.read_csv("SimLogs/historical-top10-1961/records_1961_final.csv"); h61["a"] = h61.artist.map(norm)
tot = h61.slots.sum()
o = h61[h61.label.isin(B5H) & ~h61.a.isin(hit60)].copy()
o["band"] = o.a.map(best60).fillna(999).map(band)
print("== history 1961: Big-5 slots by acts with no 1960 Top 10 (points)")
print((100 * o.groupby("band").slots.sum() / tot).round(1).to_string(), "\n  total", round(100 * o.slots.sum() / tot, 1))
print(o.sort_values("slots", ascending=False)[["artist", "title", "label", "band", "slots"]].to_string(index=False))

seeds = [int(s) for s in sys.argv[1].split(",")]
for arm in sys.argv[2:]:
    parts = []
    for s in seeds:
        p = f"SimLogs/{arm}-{s}-sc-"
        r = pd.read_csv(gz(p + "records.csv"), usecols=["year", "recordId", "artistId", "labelId", "currentPosition"])
        c = r[r.currentPosition.between(1, 100)]
        b60 = c[c.year == 1960].groupby("artistId").currentPosition.min()
        hit = set(c[(c.year == 1960) & (c.currentPosition <= 10)].artistId)
        t = c[(c.year == 1961) & (c.currentPosition <= 10)]
        tot = len(t)
        x = t[t.labelId.isin(B5) & ~t.artistId.isin(hit)].groupby(["recordId", "artistId"]).size().rename("slots").reset_index()
        a = pd.read_csv(gz(p + "single-launch-attribution.csv"), usecols=["singleId", "week", "career", "openingArchetype"]).drop_duplicates("singleId").set_index("singleId")
        plug = {}
        for line in open(f"SimLogs/writing-calibration/{arm}-{s}-sc-engine.log", encoding="utf-8", errors="ignore"):
            if line.startswith("PROMO_PLUG"):
                d = dict(kv.split("=", 1) for kv in line.split()[1:]); plug[d["record"]] = d["outcome"]
        x["band"] = x.artistId.map(b60).fillna(999).map(band)
        x["career"] = x.recordId.map(a.career).fillna("(album/promo/prewarm)")
        x["opening"] = x.recordId.map(a.openingArchetype).fillna("None").ne("None").map({True: "opening career", False: "no pre-1960 career"})
        x["plug"] = x.recordId.map(plug).fillna("not routed")
        x["launch"] = x.recordId.map(a.week).map(lambda w: "1960 launch" if w <= 52 else "1961 launch" if w == w else "?")
        x["pts"] = 100 * x.slots / tot / len(seeds)
        parts.append(x)
    x = pd.concat(parts)
    print(f"\n== {arm} (seeds {seeds}) 1961: Big-5 points by acts with no 1960 Top 10; total {x.pts.sum():.1f}")
    for col in ["band", "career", "plug", "opening", "launch"]:
        print(x.groupby(col).pts.sum().round(1).to_string(), "\n")
    print(x.pivot_table(index="band", columns="career", values="pts", aggfunc="sum", fill_value=0).round(1).to_string())
    print(x.pivot_table(index="band", columns="plug", values="pts", aggfunc="sum", fill_value=0).round(1).to_string())
