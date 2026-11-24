"""What limits a #1's servable demand, from --restock-observe (findings §11.13).

usage: analyze_restock_binding.py [--last=W] RUN ...   (RUN is the full run name; --last caps chart weeks, default 104)

Reads <run>-restock.csv and <run>-records.csv. A restock row for week t holds that week's raw and servable demand, and
the stock left after its restock, which is what week t+1's sales draw on. Each Top 20 region-week is classed by what
bound its servable demand:
  demand    servable >= 95.5% of raw demand (the regional sales jitter, 0.96-1.04, comes after the clip)
  shelf     servable at the shelf sales cap (storeCapacity)
  stock     servable at last week's stock after restock; then by why that stock fell short of this week's demand:
    none      no restock was triggered last week
    ceiling   last week's request was cut by the per-label stock ceiling
    pressing  the plant shipped less than the ceiling allowed
    request   the request itself (demand signal x service level) was below what this week needed
    unlogged  no restock row last week (the record entered the Top 20 this week)
  other     none of the above
Shares are of servable units. First reigns at #1 are classed 1 / 2-3 / 4+ weeks, by reign week j.
"""
import sys
from pathlib import Path
import numpy as np
import pandas as pd

ROOT = Path(__file__).resolve().parent.parent / "SimLogs"


def classify(r):
    r = r.sort_values(["recordId", "region", "week"]).copy()
    key = r.recordId + "|" + r.region
    prev = r.groupby(key, sort=False).shift(1)
    cont = (r.week - prev.week) == 1
    r["prevStockAfter"] = np.where(cont, prev.stockAfter, np.nan)
    need = r.rawDemand - prev.stockBefore.where(cont)  # what last week's restock had to add for this week's demand
    cls = np.full(len(r), "other", dtype=object)
    demand = r.serviceable >= 0.955 * r.rawDemand  # the regional jitter (0.96-1.04) is applied after the clip
    shelf = ~demand & (r.storeCapacity > 0) & (r.serviceable >= 0.955 * r.storeCapacity - 1)
    # Stock at the start of the week is what is left plus what sold; servable at it means the shelf stock bound.
    at_stock = ~demand & ~shelf & (r.serviceable >= 0.955 * (r.stockBefore + r.sold) - 1)
    unlogged = at_stock & ~cont
    stock = at_stock & cont
    p_need, p_req, p_max = prev.needsRestock.where(cont), prev.requested.where(cont), prev.afterCeiling.where(cont)
    p_ship = prev.shipped.where(cont)
    none = stock & (p_need == 0)
    ceiling = stock & ~none & (p_max < p_req)
    pressing = stock & ~none & ~ceiling & (p_ship < p_max.clip(lower=0))
    request = stock & ~none & ~ceiling & ~pressing & (p_req < need)
    for name, m in [("demand", demand), ("shelf", shelf), ("stock:unlogged", unlogged), ("stock:other", stock), ("stock:none", none),
                    ("stock:ceiling", ceiling), ("stock:pressing", pressing), ("stock:request", request)]:
        cls[m.fillna(False).to_numpy()] = name
    r["bind"] = cls
    return r


def main(runs, last):
    for run in runs:
        r = pd.read_csv(ROOT / f"{run}-restock.csv")
        r = r[(r.week >= 1) & (r.week <= last)]
        r = classify(r)
        rec = pd.read_csv(ROOT / f"{run}-records.csv", usecols=["week", "recordId", "currentPosition"])
        rec = rec[rec.week <= last]
        top = rec[rec.currentPosition == 1].sort_values("week")
        new = (top.recordId != top.recordId.shift()) | (top.week != top.week.shift() + 1)
        top = top.assign(sp=new.cumsum())
        sp = top.groupby("sp").agg(rid=("recordId", "first"), start=("week", "min"), end=("week", "max"))
        sp["len"] = sp.end - sp.start + 1
        sp = sp[~sp.rid.duplicated() & (sp.end < last - 2)]
        print(f"\n=== {run}, weeks 1-{last}: first reigns {len(sp)}")
        cols = ["demand", "shelf", "stock:ceiling", "stock:request", "stock:pressing", "stock:none", "stock:other",
                "stock:unlogged", "other"]
        rows = []
        for _, s in sp.iterrows():
            cls = "1" if s.len == 1 else "2-3" if s.len <= 3 else "4+"
            for j in range(-2, 5):
                x = r[(r.recordId == s.rid) & (r.week == s.start + j)]
                if x.empty:
                    continue
                tot = x.serviceable.sum()
                row = {"cls": cls, "j": j, "raw_over_serv": x.rawDemand.sum() / max(tot, 1), "serv_k": tot / 1e3}
                for c in cols:
                    row[c] = x.serviceable[x.bind == c].sum() / max(tot, 1)
                # ceiling headroom: stock ceiling against this week's raw demand, where the ceiling bound
                cx = x[x.bind == "stock:ceiling"]
                row["ceiling_over_raw"] = (cx.serviceable.sum() / cx.rawDemand.sum()) if len(cx) else np.nan
                rows.append(row)
        R = pd.DataFrame(rows)
        pd.set_option("display.width", 220)
        for cls, c in R.groupby("cls"):
            print(f"\n  reign {cls} weeks (n {len(sp[(sp.len == 1) if cls == '1' else (sp.len.between(2, 3) if cls == '2-3' else (sp.len >= 4))])}): "
                  "share of servable units by what bound it (means), raw / servable (median)")
            t = c.groupby("j").agg(**{k: (k, "mean") for k in cols},
                                   raw_over_serv=("raw_over_serv", "median"), serv_k=("serv_k", "median"))
            print(t.round(2).to_string())
        top20 = r[r.position.between(1, 20)]
        print("\n  all Top 20 region-weeks, share of servable units:",
              (top20.groupby("bind").serviceable.sum() / top20.serviceable.sum()).round(3).to_dict())


if __name__ == "__main__":
    last = 104
    runs = []
    for a in sys.argv[1:]:
        if a.startswith("--last="):
            last = int(a.split("=", 1)[1])
        else:
            runs.append(a)
    main(runs, last)
