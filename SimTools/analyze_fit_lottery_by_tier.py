"""The commercial-fit hit lottery by tier: how many singles are expected to connect, and how many of those are realized.

usage: analyze_fit_lottery_by_tier.py [--sample] <run> [<run> ...]
Singles launched weeks 1-92 of 1960-61. Big-5 = label_0001..label_0005 exactly.

Default table (all launches): a single connects with probability 1 - Phi(kink - launchWeight * (launch awareness - 0.2))
under the profile's kinked fit (hit share 0.15, launch weight 1.25; SingleCommercialFit.National). expConn sums that
over a tier's launches; t40_per_expConn is Top 40 singles per expected connecting single (realization).

--sample: the captured fit from SimLogs/<run>-single-demand-stages.csv, which holds only a sample of records.
A single is "connected" when its largest regional fit is at least 1 (the rest sit at or under the hit-act floor, 0.6).
"""
import sys
from math import erf, sqrt

import pandas as pd

KINK, LAUNCH_WEIGHT, LAUNCH_REFERENCE = 1.0364, 1.25, 0.2

B5 = {f"label_000{i}" for i in range(1, 6)}


def expected(run):
    p = f"SimLogs/{run}-"
    r = pd.read_csv(p + "records.csv", usecols=["year", "recordId", "currentPosition"])
    r = r[(r.year <= 1961) & r.currentPosition.between(1, 100)]
    a = pd.read_csv(p + "single-launch-attribution.csv", usecols=["week", "year", "singleId", "labelId", "initialAwareness"])
    q = pd.read_csv(p + "single-launch-quality.csv", usecols=["singleId", "tier"])
    a = a[(a.year <= 1961) & (a.week <= 92)].merge(q, on="singleId", how="left")
    a.loc[a.labelId.isin(B5), "tier"] = "Big5"
    a.loc[a.tier.isin(["Small", "Boutique"]), "tier"] = "Small/Bout"
    a["pConn"] = a.initialAwareness.map(
        lambda w: 1 - 0.5 * (1 + erf((KINK - LAUNCH_WEIGHT * (w - LAUNCH_REFERENCE)) / sqrt(2))))
    a["h100"] = a.singleId.isin(set(r.recordId))
    a["t40"] = a.singleId.isin(set(r[r.currentPosition <= 40].recordId))
    a["t10w"] = a.singleId.map(r[r.currentPosition <= 10].groupby("recordId").size()).fillna(0)
    g = a.groupby("tier").agg(n=("pConn", "size"), awareness=("initialAwareness", "mean"), pConn=("pConn", "mean"),
                              expConn=("pConn", "sum"), h100=("h100", "sum"), t40=("t40", "sum"), t10w=("t10w", "sum"))
    g["t40_per_expConn"] = g.t40 / g.expConn
    g["t10w_per_expConn"] = g.t10w / g.expConn
    g["launchShare%"] = 100 * g.n / g.n.sum()
    g["expConnShare%"] = 100 * g.expConn / g.expConn.sum()
    g["t40Share%"] = 100 * g.t40 / g.t40.sum()
    g["t10wShare%"] = 100 * g.t10w / g.t10w.sum()
    print("==", run)
    print(g.round(2).to_string())


def main():
    pd.set_option("display.width", 250)
    runs = [x for x in sys.argv[1:] if x != "--sample"]
    if "--sample" not in sys.argv:
        for run in runs:
            expected(run)
        return
    for run in runs:
        p = f"SimLogs/{run}-"
        fit = None
        for chunk in pd.read_csv(p + "single-demand-stages.csv", usecols=["recordId", "commercialFitAtCapture"], chunksize=2_000_000):
            m = chunk.groupby("recordId").commercialFitAtCapture.max()
            fit = m if fit is None else pd.concat([fit, m]).groupby(level=0).max()
        r = pd.read_csv(p + "records.csv", usecols=["year", "recordId", "currentPosition"])
        r = r[(r.year <= 1961) & r.currentPosition.between(1, 100)]
        a = pd.read_csv(p + "single-launch-attribution.csv",
                        usecols=["week", "year", "singleId", "labelId", "quality", "initialAwareness", "initialStock"])
        q = pd.read_csv(p + "single-launch-quality.csv", usecols=["singleId", "tier"])
        a = a[(a.year <= 1961) & (a.week <= 92)].merge(q, on="singleId", how="left")
        a.loc[a.labelId.isin(B5), "tier"] = "Big5"
        a.loc[a.tier.isin(["Small", "Boutique"]), "tier"] = "Small/Bout"
        o = pd.read_csv(p + "release-outcomes.csv", usecols=["recordId", "pressingBookSold"]).drop_duplicates("recordId", keep="last")
        a["sold"] = a.singleId.map(o.set_index("recordId").pressingBookSold)
        a["fit"] = a.singleId.map(fit)
        a = a[a.fit.notna()]
        a["connected"] = a.fit >= 1.0
        a["h100"] = a.singleId.isin(set(r.recordId))
        a["t40"] = a.singleId.isin(set(r[r.currentPosition <= 40].recordId))
        a["t10w"] = a.singleId.map(r[r.currentPosition <= 10].groupby("recordId").size()).fillna(0)
        print("==", run)
        g = a.groupby("tier").agg(n=("fit", "size"), awareness=("initialAwareness", "mean"), connectRate=("connected", "mean"))
        for flag, name in ((True, "conn"), (False, "flop")):
            d = a[a.connected == flag].groupby("tier")
            g[name + "H100"] = d.h100.mean()
            g[name + "T40"] = d.t40.mean()
            g[name + "T10wk"] = d.t10w.mean()
            g[name + "SoldMedian"] = d.sold.median()
        g["flopShareOfT10wk"] = a[~a.connected].groupby("tier").t10w.sum() / a.groupby("tier").t10w.sum()
        print(g.round(3).to_string())
        tot = a.t10w.sum()
        print("share of all Top 10 weeks: connected %.1f%%, flops %.1f%%; Big-5 flops %.1f%%" % (
            100 * a[a.connected].t10w.sum() / tot, 100 * a[~a.connected].t10w.sum() / tot,
            100 * a[(~a.connected) & (a.tier == "Big5")].t10w.sum() / tot))


if __name__ == "__main__":
    main()
