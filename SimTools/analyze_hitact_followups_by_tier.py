import numpy as np, pandas as pd
B5 = {f"label_000{i}" for i in range(1, 6)}
rows = []
import sys
# usage: analyze_hitact_followups_by_tier.py name=fmt ...  ({s} = seed; seeds 2002, 1001)
for name, fmt in [a.split("=", 1) for a in sys.argv[1:]]:
    for s in (2002, 1001):
        run = "SimLogs/" + fmt.format(s=s)
        r = pd.read_csv(run + "-records.csv", usecols=["week", "year", "recordId", "artistId", "labelId", "currentPosition"])
        m = pd.read_csv(run + "-company-monthly.csv", usecols=["year", "labelId", "tier"]).drop_duplicates(["labelId", "year"]).set_index(["labelId", "year"]).tier
        a = pd.read_csv(run + "-single-launch-attribution.csv", usecols=["week", "year", "singleId", "labelId", "artistId"])
        a = a[(a.week <= r.week.max() - 12) & (a.year >= 1961)].copy()
        a["g"] = m.reindex(pd.MultiIndex.from_arrays([a.labelId, a.year])).fillna("Independent").values
        a.loc[a.labelId.isin(B5), "g"] = "Big5"; a.loc[a.g.isin(["Small", "Boutique"]), "g"] = "Small/Bout"; a.loc[a.g.eq("Major"), "g"] = "MidTier"
        c = r[r.currentPosition.between(1, 100)]
        peak = c.groupby("recordId").currentPosition.min(); a["peak"] = a.singleId.map(peak)
        t40 = c[c.currentPosition <= 40][["artistId", "week"]].drop_duplicates().sort_values("week")
        a = a.sort_values("week")
        prev = pd.merge_asof(a[["week", "artistId", "singleId"]], t40.rename(columns={"week": "t40wk"}), left_on="week", right_on="t40wk",
                             by="artistId", allow_exact_matches=False, direction="backward")
        a["since"] = a.week.values - prev.set_index("singleId").reindex(a.singleId).t40wk.values
        a["hot"] = a.since <= 26
        for g, x in a.groupby("g"):
            h = x[x.hot]
            rows.append(dict(arm=name, tier=g, hotLaunches=len(h), hotT40=(h.peak <= 40).sum(), hotT10=(h.peak <= 10).sum(), launches=len(x)))
d = pd.DataFrame(rows).groupby(["arm", "tier"], sort=False).sum()
d["hot share of launches %"] = 100 * d.hotLaunches / d.launches
d["hot T40 %"] = 100 * d.hotT40 / d.hotLaunches
d["hot T10 %"] = 100 * d.hotT10 / d.hotLaunches
print(d.round(1).to_string())
