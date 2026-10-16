"""Why regions stay in sync: two-way separability of each single's regional demand (RegionalSyncDesign.md).

Usage:  py -3 -I SimTools/analyze_regional_separability.py SimLogs/<prefix> [--min-units 20000] [--min-weeks 8]

For every single sold in all seven regions for at least --min-weeks weeks, each log series X[r, t] is fitted with
the additive model  X = mu + alpha_r + beta_t + eps  (region gain, common week shape, interaction). Only eps can
move a record's timing or rank in one region relative to another; alpha_r is a fixed level offset, beta_t the
national lifecycle. Reported per stage, units-weighted: the variance shares of alpha, beta, eps, and how much of
the demand interaction each stage carries (cov(eps_X, eps_lnD) / var(eps_lnD)).

Stages: lnD (raw demand), lnAware (aware buyers / potential), lnConv (intrinsic conversion), lnCleared
(settled units, after stock, capacity, rationing, spillover and the +-4% jitter), and lnPot (potential audience).
Also: peak-week spread on raw demand vs cleared units, and the interaction a pure +-4% jitter would give.
"""
import argparse, json
import numpy as np, pandas as pd


def two_way(m):
    """m: regions x weeks array (finite). Returns alpha, beta, eps."""
    mu = m.mean()
    a = m.mean(axis=1, keepdims=True) - mu
    b = m.mean(axis=0, keepdims=True) - mu
    return a, b, m - mu - a - b


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("prefix")
    ap.add_argument("--min-units", type=float, default=20000)
    ap.add_argument("--min-weeks", type=int, default=8)
    o = ap.parse_args()
    st = pd.read_csv(o.prefix + "-single-demand-stages.csv",
                     usecols=["week", "recordId", "region", "age", "potentialAudience", "awareBuyers",
                              "intrinsicConversionRate", "rawDemand", "baselineAwareness", "chartSignal",
                              "inventoryFulfillmentRate", "marketFulfillmentRate"],
                     dtype={"recordId": "category", "region": "category"})
    se = pd.read_csv(o.prefix + "-completed-week-settlement-regional.csv",
                     usecols=["week", "recordId", "regionId", "finalCleared"],
                     dtype={"recordId": "category", "regionId": "category"}).rename(columns={"regionId": "region"})
    st["recordId"] = st.recordId.astype(str); st["region"] = st.region.astype(str)
    se["recordId"] = se.recordId.astype(str); se["region"] = se.region.astype(str)
    d = st.merge(se, on=["week", "recordId", "region"], how="left")
    d = d[(d.rawDemand > 0) & (d.awareBuyers > 0) & (d.intrinsicConversionRate > 0) & (d.finalCleared > 0)]
    nreg = d.region.nunique()
    full = d.groupby(["week", "recordId"]).region.transform("count") == nreg
    d = d[full].copy()
    tot = d.groupby("recordId").finalCleared.sum()
    d = d[d.recordId.isin(tot[tot >= o.min_units].index)]
    d["lnD"] = np.log(d.rawDemand)
    d["lnAware"] = np.log(d.awareBuyers / d.potentialAudience)
    d["lnConv"] = np.log(d.intrinsicConversionRate)
    d["lnCleared"] = np.log(d.finalCleared)
    d["lnPot"] = np.log(d.potentialAudience)
    d["lnBase"] = np.log(d.baselineAwareness.clip(lower=1e-6))
    d["lnChart"] = np.log(d.chartSignal.clip(lower=1e-6))
    d["lnInv"] = np.log(d.inventoryFulfillmentRate.clip(lower=1e-3))
    d["lnMkt"] = np.log(d.marketFulfillmentRate.clip(lower=1e-3))
    d["lnSupply"] = d.lnCleared - d.lnD
    stages = ["lnD", "lnAware", "lnBase", "lnChart", "lnConv", "lnPot", "lnCleared", "lnSupply", "lnInv", "lnMkt"]

    acc = {s: {"a": 0.0, "b": 0.0, "e": 0.0, "cov": 0.0} for s in stages}
    eD_var = 0.0; wsum = 0.0; nrec = 0
    peak_raw, peak_clr, peak_raw_t, first_full = [], [], [], []
    for rid, g in d.groupby("recordId", sort=False):
        weeks = np.sort(g.week.unique())
        if len(weeks) < o.min_weeks: continue
        w = float(g.finalCleared.sum()); nrec += 1; wsum += w
        eps = {}
        for s in stages:
            m = g.pivot(index="region", columns="week", values=s).to_numpy()
            a, b, e = two_way(m)
            n = m.size
            acc[s]["a"] += w * (a ** 2).sum() * m.shape[1] / n
            acc[s]["b"] += w * (b ** 2).sum() * m.shape[0] / n
            acc[s]["e"] += w * (e ** 2).sum() / n
            eps[s] = e
        eD_var += w * (eps["lnD"] ** 2).mean()
        for s in stages: acc[s]["cov"] += w * (eps[s] * eps["lnD"]).mean()
        raw = g.pivot(index="region", columns="week", values="rawDemand")
        clr = g.pivot(index="region", columns="week", values="finalCleared")
        pr = raw.idxmax(axis=1); pc = clr.idxmax(axis=1)
        peak_raw.append(pr.max() - pr.min()); peak_clr.append(pc.max() - pc.min())
        first_full.append(int(weeks[0]))

    res = {"prefix": o.prefix, "records": nrec, "minUnits": o.min_units}
    for s in stages:
        a, b, e = (acc[s][k] / wsum for k in "abe")
        t = a + b + e
        res[s] = {"var": round(t, 4), "regionGain": round(a / t, 4), "commonWeek": round(b / t, 4),
                  "interaction": round(e / t, 4), "interactionVar": round(e, 5),
                  "shareOfDemandInteraction": round(acc[s]["cov"] / eD_var, 4)}
    # a pure U(0.96, 1.04) multiplicative jitter: var(ln U) ~ 0.04^2 / 3
    res["jitterInteractionVar"] = round(0.04 ** 2 / 3, 5)
    def spread(x):
        x = np.array(x)
        return {"median": float(np.median(x)), "shareGe4": round(float((x >= 4).mean()), 4),
                "shareGe8": round(float((x >= 8).mean()), 4)}
    res["peakSpreadRawDemand"] = spread(peak_raw)
    res["peakSpreadCleared"] = spread(peak_clr)
    print(json.dumps(res, indent=2))


if __name__ == "__main__":
    main()
