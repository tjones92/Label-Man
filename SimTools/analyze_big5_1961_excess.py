"""Where the 1961 Big-5 Top 10 excess comes from (LpGenreAllocationFollowup.md §50). Usage from the repo root:
python SimTools/analyze_big5_1961_excess.py RUN [RUN ...]      (RUN = e.g. hm104-2002-sc; reads SimLogs/RUN-*.csv)
Per run, 1961 Top 10 slots: (1) by origin — Big-5 / other label x act with / without a 1960 Top 10 slot, against the
operating-label reconstruction (SimLogs/historical-1960-top10, historical-top10-1961); (2) per 1960 hit act, 1961 slot-weeks;
(3) per Big-5 firm; (4) newcomer (NewSigning/Rising) Top 10 weeks per launch, 1960-61 pooled over runs, by tier and
quality bin, and the Independent / MidTier rate at the Big-5 quality mix."""
import re, sys
import numpy as np, pandas as pd
import os


def gz(path):
    """Arm runs write .csv.gz by default; read whichever exists."""
    return path if os.path.exists(path) or not os.path.exists(path + ".gz") else path + ".gz"

B5_IDS = [f"label_000{i}" for i in range(1, 6)]  # Columbia, RCA Victor, Capitol, Decca, Mercury; never compare ids as strings
B5_HIST = ["Columbia", "RCA Victor", "Capitol", "Decca", "Mercury"]


def norm(a):
    a = re.split(r" with | and his | and her | featuring | & the ", a.lower())[0]
    return re.sub(r"[^a-z0-9 ]", "", a).strip()


def history():
    h60 = pd.read_csv("SimLogs/historical-1960-top10/records_1960_final.csv")
    h61 = pd.read_csv("SimLogs/historical-top10-1961/records_1961_final.csv")
    hit = h60.assign(a=h60.artist.map(norm)).groupby("a").label.agg(lambda s: s.value_counts().index[0])
    h61 = h61.assign(a=h61.artist.map(norm), b5=h61.label.isin(B5_HIST))
    h61["hit60"] = h61.a.isin(hit.index)
    o = {f"{'B5' if b else 'other'} {'1960 hit act' if h else 'no 1960 Top 10'}": 100 * h61[(h61.b5 == b) & (h61.hit60 == h)].slots.sum() / 520
         for b in (True, False) for h in (True, False)}
    for b in (True, False):
        acts = hit[hit.isin(B5_HIST) == b]
        o[f"{'B5' if b else 'other'} 1960 hit acts"] = len(acts)
        o[f"{'B5' if b else 'other'} slot-weeks per 1960 hit act"] = h61[h61.a.isin(acts.index)].slots.sum() / len(acts)
    for lab, i in zip(B5_HIST, B5_IDS):
        o[f"slots {lab}"] = h61[h61.label == lab].slots.sum()
    return o


def one(run):
    p = f"SimLogs/{run}-"
    r = pd.read_csv(gz(p + "records.csv"), usecols=["year", "recordId", "artistId", "labelId", "currentPosition"])
    r = r[r.currentPosition.between(1, 100)]
    t = r[r.currentPosition <= 10]
    hit = t[t.year == 1960].groupby("artistId").labelId.agg(lambda s: s.value_counts().index[0])
    x = t[t.year == 1961].assign(b5=lambda d: d.labelId.isin(B5_IDS), hit60=lambda d: d.artistId.isin(hit.index))
    n = len(x)
    o = {f"{'B5' if b else 'other'} {'1960 hit act' if h else 'no 1960 Top 10'}": 100 * ((x.b5 == b) & (x.hit60 == h)).sum() / n
         for b in (True, False) for h in (True, False)}
    for b in (True, False):
        acts = hit[hit.isin(B5_IDS) == b]
        o[f"{'B5' if b else 'other'} 1960 hit acts"] = len(acts)
        o[f"{'B5' if b else 'other'} slot-weeks per 1960 hit act"] = x.artistId.isin(acts.index).sum() / len(acts)
    for lab, i in zip(B5_HIST, B5_IDS):
        o[f"slots {lab}"] = (x.labelId == i).sum()
    a = pd.read_csv(gz(p + "single-launch-attribution.csv"), usecols=["year", "singleId", "labelId", "career", "quality"])
    q = pd.read_csv(gz(p + "single-launch-quality.csv"), usecols=["singleId", "tier"])
    a = a[a.year.isin([1960, 1961]) & a.career.isin(["NewSigning", "Rising", "Unsigned"])].merge(q, on="singleId", how="left")
    a["g"] = np.where(a.labelId.isin(B5_IDS), "Big5", a.tier.replace({"Boutique": "Small"}))
    a["t10"] = a.singleId.map(t.groupby("recordId").size()).fillna(0)
    return o, a


runs = sys.argv[1:]
res = {"history": history()}
new = []
for run in runs:
    res[run], a = one(run)
    new.append(a)
df = pd.DataFrame(res)
df["sim mean"] = df[runs].mean(axis=1)
pd.set_option("display.width", 200)
print(df.round(2).to_string())
N = pd.concat(new)
N["qb"] = pd.cut(N.quality, [0, .66, .70, .74, .78, 1])
print("\nNewcomer Top 10 weeks per launch, 1960-61 pooled")
tab = N.groupby(["qb", "g"], observed=True).t10.agg(["mean", "size"])
print(tab["mean"].unstack().round(3).to_string())
b = N[N.g == "Big5"]
for ref in ("Independent", "MidTier"):
    rate = N[N.g == ref].groupby("qb", observed=True).t10.mean()
    print(f"{ref:12} actual {N[N.g == ref].t10.mean():.3f}  at Big-5 quality mix {b.qb.map(rate).astype(float).mean():.3f}"
          f"  | Big-5 actual {b.t10.mean():.3f}")
