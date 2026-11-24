"""Top 10 per Hot 100 chart event by the act's prior chart best (LpGenreAllocationFollowup.md §75). From the repo root:
SEEDS=1001,2002 python SimTools/score_climb_by_prior.py ARM [ARM ...]      (ARM = e.g. aud05; default seeds 1001-4004)
A chart event is a Hot 100 debut 4+ weeks after the act's previous debut. Its band is the best peak of the act's earlier
records ('none' = a first chart entry). History: the Hot 100, debuts 1961-63, label-blind (labels exist only for Top 10s).
Sim: debuts in weeks 40-92, by the record's label group (Big-5 / MidTier / field), and by the label's covered regions at
the debut week for first entries. Events are per seed-year."""
import os
import re
import sys
import pandas as pd

SEEDS = [int(s) for s in os.environ.get("SEEDS", "1001,2002,3003,4004").split(",")]
B5 = {f"label_000{i}" for i in range(1, 6)}
BANDS = ["T10", "11-40", "41-100", "none"]


def key(a):
    a = re.split(r" with | and his | and her | featuring | & the ", a.lower())[0]
    return re.sub(r"[^a-z0-9]", "", a)


def gz(p):
    return p if os.path.exists(p) or not os.path.exists(p + ".gz") else p + ".gz"


def events(rec, lo, hi):
    rec = rec.sort_values(["act", "debut"]).copy()
    rec["prior"] = rec.groupby("act").peak.transform(lambda p: p.cummin().shift(1)).fillna(999)
    rec["gap"] = rec.groupby("act").debut.diff().fillna(99)
    e = rec[(rec.gap >= 4) & rec.debut.between(lo, hi)].copy()
    e["band"] = pd.cut(e.prior, [0, 10, 40, 100, 1000], labels=BANDS)
    return e


def rates(e, by, years):
    t = e.groupby(by, observed=True).peak.agg(events="size", t10=lambda p: 100 * (p <= 10).mean(), t40=lambda p: 100 * (p <= 40).mean())
    t["events"] /= years
    return t


pd.set_option("display.width", 220)
w = pd.read_csv("SimLogs/historical-hot100-1960-64/hot100_weekly.csv", parse_dates=["chartDate"])
w["act"] = w.artist.map(key)
w["rec"] = w.act + "|" + w.title.str.lower()
w["wk"] = (w.chartDate - w.chartDate.min()).dt.days // 7
hr = w.groupby(["act", "rec"]).agg(debut=("wk", "min"), peak=("position", "min")).reset_index()
print("== history 1961-63, label-blind: events/yr, T10 %, T40 % per event")
print(rates(events(hr, 52, 207), "band", 3).round(1).to_string())
for arm in sys.argv[1:]:
    parts = []
    for s in SEEDS:
        r = pd.read_csv(gz(f"SimLogs/{arm}-{s}-sc-records.csv"),
                        usecols=["week", "recordId", "artistId", "labelId", "labelTier", "currentPosition", "coveredRegionCount"])
        r = r[r.currentPosition.between(1, 100)]
        g = r.sort_values("week").groupby("recordId")
        rr = g.agg(act=("artistId", "first"), lab=("labelId", "first"), tier=("labelTier", "first"), debut=("week", "min"),
                   peak=("currentPosition", "min"), cov=("coveredRegionCount", "first")).reset_index()
        rr = rr[rr.debut > 1]
        rr["act"] = f"{s}|" + rr.act
        parts.append(events(rr, 40, 92))
    e = pd.concat(parts)
    e["grp"] = e.tier.where(~e.lab.isin(B5), "Big-5").replace({"Independent": "field", "Boutique": "field", "Small": "field", "Major": "MidTier"})
    years = len(SEEDS) * 53 / 52
    print(f"\n== {arm} (seeds {SEEDS}), debuts weeks 40-92")
    print(rates(e, ["band", "grp"], years).unstack("grp").round(1).to_string())
    print("all:", rates(e, "band", years).t10.round(1).to_dict())
    f = e[e.band == "none"].copy()
    f["covBand"] = pd.cut(f["cov"], [0, 3, 5, 7], labels=["1-3", "4-5", "6-7"])
    print("first entries by label coverage at debut (T10 %, events/yr):")
    print(rates(f, ["covBand", "grp"], years)[["events", "t10"]].unstack("grp").round(1).to_string())
