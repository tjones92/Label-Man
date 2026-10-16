"""Regional synchronization metric for audit step 3 (SimTools/RegionalAudienceDesign.md).

Usage:  py -3 -I SimTools/analyze_regional_sync.py SimLogs/<prefix> [--min-units 50000]

Reads <prefix>-single-demand-stages.csv (level/change decomposition) and
<prefix>-completed-week-settlement-regional.csv (peak timing, top-11 overlap).
Each record's log demand in a region is split into the record's cross-region mean
("common") and that region's deviation ("local"). The media 7 -> 0 transition count
is produced by SimTools/analyze_celebrity_audit.py.
"""
import argparse, json, os, sys
import numpy as np, pandas as pd


def decompose(path):
    d = pd.read_csv(path, usecols=["week", "recordId", "region", "potentialAudience", "awareBuyers",
                                   "intrinsicConversionRate", "rawDemand"])
    d = d[(d.rawDemand > 0) & (d.awareBuyers > 0) & (d.potentialAudience > 0) & (d.intrinsicConversionRate > 0)].copy()
    d["lnD"] = np.log(d.rawDemand)
    d["lnAware"] = np.log(d.awareBuyers / d.potentialAudience)
    d["lnConv"] = np.log(d.intrinsicConversionRate)
    full = d.groupby(["week", "recordId"]).region.transform("count") == d.region.nunique()
    d = d[full]
    out = {"recordWeeks": int(d.groupby(["week", "recordId"]).ngroups)}

    def split(frame, cols, prefix):
        g = frame.groupby(["week", "recordId"])
        gw = frame.groupby(["week", "region"])
        tot = gw[cols[0]].var().mean() if prefix == "level" else frame[cols[0]].var()
        res = {}
        for c in cols:
            com = g[c].transform("mean")
            loc = frame[c] - com
            if prefix == "level":
                # within region-week, so the ranking each region actually sees
                f = pd.DataFrame({"w": frame.week, "r": frame.region, "c": com, "l": loc, "D": frame[cols[0]]})
                cc = f.groupby(["w", "r"]).apply(lambda x: np.cov(x.c, x.D)[0, 1], include_groups=False).mean()
                cl = f.groupby(["w", "r"]).apply(lambda x: np.cov(x.l, x.D)[0, 1], include_groups=False).mean()
            else:
                cc = np.cov(com, frame[cols[0]])[0, 1]
                cl = np.cov(loc, frame[cols[0]])[0, 1]
            res[c] = {"commonShare": round(float(cc / tot), 4), "localShare": round(float(cl / tot), 4)}
        return res

    out["level"] = split(d, ["lnD", "lnAware", "lnConv"], "level")
    d = d.sort_values(["recordId", "region", "week"])
    for c in ["lnD", "lnAware", "lnConv"]:
        d["d" + c] = d.groupby(["recordId", "region"])[c].diff()
    ch = d.dropna(subset=["dlnD"])
    ch = ch[ch.groupby(["week", "recordId"]).region.transform("count") == d.region.nunique()]
    out["change"] = split(ch, ["dlnD", "dlnAware", "dlnConv"], "change")
    return out


def settlement(path, min_units):
    d = pd.read_csv(path, usecols=["week", "recordId", "regionId", "finalCleared"],
                    dtype={"recordId": "category", "regionId": "category"})
    out = {}
    tot = d.groupby("recordId", observed=True).finalCleared.sum()
    big = d[d.recordId.isin(tot[tot >= min_units].index)]
    idx = big.groupby(["recordId", "regionId"], observed=True).finalCleared.idxmax()
    pk = big.loc[idx, ["recordId", "week"]]
    s = pk.groupby("recordId", observed=True).week.agg(lambda w: w.max() - w.min())
    out["peakSpread"] = spread_stats(s)
    top = d[d.finalCleared > 0].sort_values("finalCleared", ascending=False).groupby(["week", "regionId"], observed=True).head(11)
    overlaps = []
    for _, wk in top.groupby("week"):
        sets = [set(v) for _, v in wk.groupby("regionId", observed=True).recordId]
        for i in range(len(sets)):
            for j in range(i + 1, len(sets)):
                overlaps.append(len(sets[i] & sets[j]) / 11)
    out["top11Overlap"] = round(float(np.mean(overlaps)), 4) if overlaps else None
    return out


def spread_stats(s):
    return {"records": int(len(s)), "median": float(s.median()), "share0": round(float((s == 0).mean()), 4),
            "shareGe4": round(float((s >= 4).mean()), 4), "shareGe8": round(float((s >= 8).mean()), 4)}


def raw_timing(path, min_units):
    """Peak and arrival timing on raw demand (before stock), so stockouts cannot fake regional timing.
    Arrival: the first week a region's demand reaches 10% of its own peak (RegionalSyncDesign.md section 2)."""
    d = pd.read_csv(path, usecols=["week", "recordId", "region", "rawDemand"],
                    dtype={"recordId": "category", "region": "category"})
    tot = d.groupby("recordId", observed=True).rawDemand.sum()
    d = d[d.recordId.isin(tot[tot >= min_units].index)]
    d = d[d.groupby("recordId", observed=True).region.transform("nunique") == d.region.nunique()]
    start = d.groupby("recordId", observed=True).week.transform("min")
    d = d.assign(age=d.week - start + 1)
    g = d.groupby(["recordId", "region"], observed=True)
    pk = d.loc[g.rawDemand.idxmax(), ["recordId", "region", "age", "rawDemand"]]
    peak = pk.groupby("recordId", observed=True).age.agg(lambda w: w.max() - w.min())
    d = d.merge(pk[["recordId", "region", "rawDemand"]].rename(columns={"rawDemand": "peak"}), on=["recordId", "region"])
    arr = d[d.rawDemand >= 0.1 * d.peak].groupby(["recordId", "region"], observed=True).age.min().reset_index()
    spread = arr.groupby("recordId", observed=True).age.agg(lambda w: w.max() - w.min())
    return {"rawPeakSpread": spread_stats(peak), "arrivalSpread": spread_stats(spread),
            "meanArrivalAgeByRegion": {str(k): round(float(v), 2) for k, v in arr.groupby("region", observed=True).age.mean().items()}}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("prefix")
    ap.add_argument("--min-units", type=float, default=50000)
    a = ap.parse_args()
    res = {"prefix": a.prefix}
    stages = a.prefix + "-single-demand-stages.csv"
    settle = a.prefix + "-completed-week-settlement-regional.csv"
    if os.path.exists(stages):
        res["decomposition"] = decompose(stages)
        res["rawTiming"] = raw_timing(stages, a.min_units)
    if os.path.exists(settle): res["settlement"] = settlement(settle, a.min_units)
    if len(res) == 1: sys.exit("no input CSVs for " + a.prefix)
    print(json.dumps(res, indent=2))


if __name__ == "__main__":
    main()
