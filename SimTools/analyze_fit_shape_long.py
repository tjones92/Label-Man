"""Fit-shape arms over 156-208 weeks: share by year, stars, closures, #1 lifetime units.

usage: analyze_fit_shape_long.py SEEDS name=fmt [name=fmt ...]   ({s} = seed)
 A  Big-5 Top 10 / 40 / Hot 100 slot-week share, and MidTier / Independent / Small Top 10 share, by year from 1961.
 B  stars: Big-5 + MidTier singles launched at awareness >= 0.7 (launched >= 12 weeks before the run end):
    share reaching the Top 40 / Top 10, and Top 40 records a year per star act (an act with such a launch that year).
 C  closures by year and tier (a label active in one month and inactive at the run end, dated by its last active month);
    labels active at each year end by tier.
 D  #1 records (first chart week > 10, #1 at least 20 weeks before the end): lifetime units, by year of first #1.
Tier is the label's tier in company-monthly in that year; Big-5 = label_0001-label_0005.
"""
import sys
import numpy as np
import pandas as pd

B5 = {f"label_000{i}" for i in range(1, 6)}
seeds = [int(x) for x in sys.argv[1].split(",")]
pd.set_option("display.width", 250); pd.set_option("display.max_columns", 40)


def grp(label, tier):
    return np.where(pd.Series(label).isin(B5), "Big5", np.where(pd.Series(tier).isin(["Small", "Boutique"]), "Small", tier))


A, B, C, Cact, D = [], [], [], [], []
for arg in sys.argv[2:]:
    name, fmt = arg.split("=", 1)
    for s in seeds:
        run = f"SimLogs/{fmt.format(s=s)}"
        r = pd.read_csv(run + "-records.csv", usecols=["week", "year", "recordId", "artistId", "labelId", "currentPosition", "totalUnitsSold"])
        m = pd.read_csv(run + "-company-monthly.csv", usecols=["year", "month", "labelId", "tier", "active"])
        m["active"] = m.active.astype(str).str.lower().isin(["true", "1"])
        m["g"] = grp(m.labelId.values, m.tier.values)
        ytier = m.drop_duplicates(["labelId", "year"]).set_index(["labelId", "year"]).g
        last = r.week.max()
        c = r[r.currentPosition.between(1, 100)].copy()
        c["g"] = ytier.reindex(pd.MultiIndex.from_arrays([c.labelId, c.year])).fillna("Independent").values
        for y in sorted(c.year.unique()):
            if y < 1961: continue
            x = c[c.year == y]
            row = dict(arm=name, seed=s, year=y)
            for nm, d in (("T10", 10), ("T40", 40), ("H100", 100)):
                row["B5 " + nm] = 100 * (x[x.currentPosition <= d].g == "Big5").mean()
            for g in ("MidTier", "Independent", "Small"):
                row[g + " T10"] = 100 * (x[x.currentPosition <= 10].g == g).mean()
            A.append(row)
        # B stars
        a = pd.read_csv(run + "-single-launch-attribution.csv", usecols=["week", "year", "singleId", "labelId", "artistId", "initialAwareness"])
        a = a[a.week <= last - 12]
        a["g"] = ytier.reindex(pd.MultiIndex.from_arrays([a.labelId, a.year])).fillna("Independent").values
        peak = c.groupby("recordId").currentPosition.min()
        a["peak"] = a.singleId.map(peak)
        st = a[(a.initialAwareness >= .7) & a.g.isin(["Big5", "MidTier"])]
        for y in sorted(a.year.unique()):
            sy = st[st.year == y]
            acts = a[(a.year == y) & a.artistId.isin(sy.artistId)]
            B.append(dict(arm=name, seed=s, year=y, starSingles=len(sy), T40pct=100 * (sy.peak <= 40).mean(), T10pct=100 * (sy.peak <= 10).mean(),
                          starActs=sy.artistId.nunique(), T40perActYr=(acts.peak <= 40).sum() / max(1, sy.artistId.nunique())))
        # C closures
        act = m[m.active]
        lastact = act.sort_values(["year", "month"]).groupby("labelId").last()
        endstate = m.sort_values(["year", "month"]).groupby("labelId").active.last()
        closed = lastact[~endstate.reindex(lastact.index).fillna(False)]
        for (y, g), n in closed.groupby(["year", "g"]).size().items():
            C.append(dict(arm=name, seed=s, year=y, g=g, closures=n))
        ye = act.sort_values("month").groupby(["year", "labelId"]).last().reset_index()
        ye = ye[ye.month == ye.groupby("year").month.transform("max")]
        for (y, g), n in ye.groupby(["year", "g"]).size().items():
            Cact.append(dict(arm=name, seed=s, year=y, g=g, active=n))
        # D #1 lifetime
        first = c.groupby("recordId").week.min()
        n1 = c[c.currentPosition == 1].groupby("recordId").agg(week=("week", "min"), year=("year", "min"))
        n1 = n1[(first.reindex(n1.index) > 10) & (n1.week <= last - 20)]
        life = r[r.recordId.isin(n1.index)].groupby("recordId").totalUnitsSold.max() / 1e6
        n1["life"] = life
        for y, x in n1.groupby("year"):
            D.append(dict(arm=name, seed=s, year=y, no1s=len(x), median=x.life.median(), ge1M=100 * (x.life >= 1).mean(), max=x.life.max()))

A = pd.DataFrame(A)
print("A  share by year (mean of seeds); per-seed Big-5 T10 after")
print(A.groupby(["year", "arm"], sort=False).mean(numeric_only=True).drop(columns="seed").round(1).to_string())
print(A.pivot_table(index=["year", "arm"], columns="seed", values="B5 T10", sort=False).round(1).to_string())
B = pd.DataFrame(B)
print("\nB  stars (Big-5 + MidTier singles at launch awareness >= 0.7), mean of seeds")
print(B.groupby(["year", "arm"], sort=False).mean(numeric_only=True).drop(columns="seed").round(2).to_string())
C = pd.DataFrame(C)
print("\nC  closures by year and tier (sum over seeds)")
if len(C): print(C.pivot_table(index=["year", "arm"], columns="g", values="closures", aggfunc="sum", fill_value=0, sort=False).to_string())
Cact = pd.DataFrame(Cact)
print("   labels active at year end (mean of seeds)")
print(Cact.pivot_table(index=["year", "arm"], columns="g", values="active", aggfunc="mean", fill_value=0, sort=False).round(1).to_string())
D = pd.DataFrame(D)
print("\nD  #1 lifetime units (M) by year of first #1, mean of seeds")
print(D.groupby(["year", "arm"], sort=False).mean(numeric_only=True).drop(columns="seed").round(2).to_string())
