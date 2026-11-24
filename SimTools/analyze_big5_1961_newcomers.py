"""Who the 1961 Big-5 'no 1960 Top 10' Top 10s are (LpGenreAllocationFollowup.md §75). Usage from the repo root:
python SimTools/analyze_big5_1961_newcomers.py ARM [ARM ...]   (ARM = e.g. aud05; reads SimLogs/ARM-{seeds}-sc-*)
Per arm, pooled over seeds: singles launched weeks 40-92 by acts with no Top 10 before launch, by tier (Big-5 / MidTier /
field), career at launch, prior chart best (none / 41-100 / 11-40), opening-roster act or not, and plug outcome (Big-5).
Counts are per seed; T10 = reaches the Top 10 at any time; slot = 1961 Top 10 slot-weeks per seed."""
import os
import re
import sys
import numpy as np
import pandas as pd

SEEDS = [int(s) for s in os.environ.get("SEEDS", "1001,2002").split(",")]
B5 = {f"label_000{i}" for i in range(1, 6)}
PLUG = re.compile(r"PROMO_PLUG week=(\d+) label=(\S+) tier=(\S+) record=(\S+) artist=(\S*) career=(\S*) hit=(\S+) quality=(\S+) outcome=(\S+)")


def gz(p):
    return p if os.path.exists(p) or not os.path.exists(p + ".gz") else p + ".gz"


def one(arm, seed):
    run = f"{arm}-{seed}-sc"
    p = f"SimLogs/{run}-"
    a = pd.read_csv(gz(p + "single-launch-attribution.csv"),
                    usecols=["week", "year", "singleId", "labelId", "artistId", "career", "openingArchetype", "quality"])
    q = pd.read_csv(gz(p + "single-launch-quality.csv"), usecols=["singleId", "tier"])
    a = a.merge(q, on="singleId", how="left")
    r = pd.read_csv(gz(p + "records.csv"), usecols=["week", "year", "recordId", "artistId", "currentPosition"])
    r = r[r.currentPosition.between(1, 100)]
    peak = r.groupby("recordId").currentPosition.min()
    first = r.groupby("recordId").week.min()
    a["peak"] = a.singleId.map(peak).fillna(999)
    a["slot61"] = a.singleId.map(r[(r.year == 1961) & (r.currentPosition <= 10)].groupby("recordId").size()).fillna(0)
    # act's best chart peak on records that charted before this launch
    ch = pd.DataFrame({"recordId": peak.index, "pk": peak.values, "fw": first.reindex(peak.index).values})
    ch = ch.merge(a[["singleId", "artistId"]].rename(columns={"singleId": "recordId"}), on="recordId", how="left")
    ch = ch.merge(r[["recordId", "artistId"]].drop_duplicates("recordId").rename(columns={"artistId": "aid2"}), on="recordId")
    ch["artistId"] = ch.artistId.fillna(ch.aid2)
    best = []
    for aid, wk in zip(a.artistId, a.week):
        c = ch[(ch.artistId == aid) & (ch.fw < wk)].pk
        best.append(c.min() if len(c) else 999)
    a["prior"] = best
    a["grp"] = np.where(a.labelId.isin(B5), "Big5", np.where(a.tier == "MidTier", "MidTier", "field"))
    a["opening"] = a.openingArchetype.fillna("None").ne("None")
    a["priorBand"] = pd.cut(a.prior, [0, 10, 40, 100, 1000], labels=["T10", "11-40", "41-100", "none"])
    plugs = {}
    log = f"SimLogs/writing-calibration/{run}.log"
    if os.path.exists(log):
        with open(log, encoding="utf-8", errors="ignore") as f:
            for line in f:
                m = PLUG.search(line)
                if m:
                    plugs[m.group(4).strip('"')] = m.group(9) + ("/hit" if m.group(7) == "True" else "")
    a["plug"] = a.singleId.map(plugs).fillna("NotRouted")
    a["t10"] = a.peak <= 10
    a["seed"] = seed
    return a


def table(d, by, n):
    t = d.groupby(by, observed=True).agg(issues=("t10", "size"), t10=("t10", "sum"), slot61=("slot61", "sum"))
    t[["issues", "t10", "slot61"]] /= n
    t["t10 %"] = 100 * t.t10 / t.issues
    return t.round(2)


pd.set_option("display.width", 200)
for arm in sys.argv[1:]:
    A = pd.concat(one(arm, s) for s in SEEDS)
    n = len(SEEDS)
    w = A[A.week.between(40, 92) & (A.priorBand != "T10")]
    print(f"===== {arm} (seeds {SEEDS}); launches wk 40-92 by acts with no prior Top 10, per seed")
    print(table(w, ["grp"], n).to_string())
    print("\n-- by prior chart best")
    print(table(w, ["priorBand", "grp"], n).unstack("grp").to_string())
    print("\n-- by career at launch")
    print(table(w, ["career", "grp"], n).unstack("grp").to_string())
    print("\n-- Big-5: opening-roster act?")
    print(table(w[w.grp == "Big5"], ["opening"], n).to_string())
    print("\n-- Big-5: plug outcome")
    print(table(w[w.grp == "Big5"], ["plug"], n).to_string())
    print("\n-- Big-5: plug x prior band")
    print(table(w[w.grp == "Big5"], ["plug", "priorBand"], n).to_string())
    qb = pd.cut(w.quality, [0, .66, .70, .74, .78, 1])
    print("\n-- T10 % by quality bin")
    t = w.assign(qb=qb).groupby(["qb", "grp"], observed=True).t10.agg(["mean", "size"])
    t["mean"] *= 100
    print(t.unstack("grp").round(2).to_string())
    print()
