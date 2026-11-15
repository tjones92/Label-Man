"""Quick side-by-side of chart-top metrics for short screens (one seed, a capped window), read from records.csv only.

usage: analyze_short_screen.py [--last=W] NAME=run ...   (run is the full run name; --last caps chart weeks, default 104)

Per year over weeks 1..W: #1 records, one-week #1 %, first reigns of 1 / 2-3 / 4+ weeks, #1 returns %; Top 10 records,
their Top 10 weeks (records still in the Top 10 at W are left out), one-week Top 10 %; Top 40 records; median debut
position; airplay share of true points in Top 10 slots; Big-5 (label_0001-0005) share of Top 10 slot-weeks per year;
#1 airplay (k true points) at reign weeks 0 and 2 for 4+ week reigns; #1 peak weekly units and lifetime units (records
that first reached #1 26+ weeks before W).
Sampling noise at one seed is large (Big-5 share swings about ±10 points per seed-year); read direction only.
"""
import sys
from pathlib import Path
import numpy as np
import pandas as pd

ROOT = Path(__file__).resolve().parent.parent / "SimLogs"
B5 = {f"label_000{i}" for i in range(1, 6)}
COLS = ["week", "year", "recordId", "labelId", "currentPosition", "unitsThisWeek", "unsampledChartPoints", "totalUnitsSold"]


def score(run, last):
    r = pd.read_csv(ROOT / f"{run}-records.csv", usecols=COLS)
    r = r[r.week.between(1, last)]
    c = r[r.currentPosition.between(1, 100)].copy()
    yrs = last / 52.0
    top = c[c.currentPosition == 1].sort_values("week")
    new = (top.recordId != top.recordId.shift()) | (top.week != top.week.shift() + 1)
    top = top.assign(sp=new.cumsum())
    sp = top.groupby("sp").agg(rid=("recordId", "first"), start=("week", "min"), end=("week", "max"))
    sp["len"] = sp.end - sp.start + 1
    sp["k"] = sp.groupby("rid").cumcount()
    done = sp[sp.end < last]
    first = done[done.k == 0]
    per = sp.groupby("rid").len.sum()
    out = {
        "#1 records/yr": len(per) / yrs,
        "one-week #1 %": 100 * (per == 1).mean(),
        "reigns 1 / 2-3 / 4+ per yr": " / ".join(f"{x / yrs:.1f}" for x in
                                               [(first.len == 1).sum(), first.len.between(2, 3).sum(), (first.len >= 4).sum()]),
        "#1 returns %": 100 * (sp.k > 0).sum() / len(sp),
    }
    t10 = c[c.currentPosition <= 10]
    wk = t10.groupby("recordId").week.agg(["size", "max"])
    unc = wk[wk["max"] < last]
    ones = per.index
    peak = r[r.recordId.isin(ones)].groupby("recordId").unitsThisWeek.max()
    first_top = sp.groupby("rid").start.min()
    settled = first_top[first_top <= last - 26].index  # lifetime read 26+ weeks after first reaching #1
    life = r[r.recordId.isin(settled)].groupby("recordId").totalUnitsSold.max()
    out["#1 peak week units (k, median)"] = peak.median() / 1e3
    out["#1 lifetime units (M, median)"] = life.median() / 1e6
    out["Top 10 records/yr"] = len(wk) / yrs
    out["Top 10 weeks"] = unc["size"].mean()
    out["one-week Top 10 %"] = 100 * (unc["size"] == 1).mean()
    out["Top 40 records/yr"] = c[c.currentPosition <= 40].recordId.nunique() / yrs
    debut = c.sort_values("week").drop_duplicates("recordId")
    out["debut position median"] = debut[debut.week > 1].currentPosition.median()
    out["airplay share, Top 10 slots %"] = 100 * (1 - t10.unitsThisWeek.sum() / t10.unsampledChartPoints.sum())
    for y, g in t10.groupby("year"):
        out[f"Big-5 Top 10 share {y} %"] = 100 * g.labelId.isin(B5).mean()
    pts = c.set_index(["recordId", "week"])
    air = lambda rid, w: (pts.loc[(rid, w)].unsampledChartPoints - pts.loc[(rid, w)].unitsThisWeek) \
        if (rid, w) in pts.index else np.nan
    long = first[first.len >= 4]
    a0 = np.array([air(s.rid, s.start) for _, s in long.iterrows()])
    a2 = np.array([air(s.rid, s.start + 2) for _, s in long.iterrows()])
    out["4+ reigns: #1 airplay j0 -> j2 (k), growth"] = \
        f"{np.nanmedian(a0) / 1e3:.0f} -> {np.nanmedian(a2) / 1e3:.0f}, x{np.nanmedian(a2 / a0):.2f}" if len(long) else "-"
    return out


if __name__ == "__main__":
    last = 104
    arms = []
    for a in sys.argv[1:]:
        if a.startswith("--last="):
            last = int(a.split("=", 1)[1])
        else:
            arms.append(a.split("=", 1))
    table = pd.DataFrame({name: score(run, last) for name, run in arms})
    pd.set_option("display.width", 200)
    print(f"weeks 1-{last}")
    print(table.to_string(float_format=lambda v: f"{v:.2f}" if abs(v) < 10 else f"{v:.1f}"))
