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
    d["weekGap"] = d.groupby(["recordId", "region"]).week.diff()
    for c in ["lnD", "lnAware", "lnConv"]:
        d["d" + c] = d.groupby(["recordId", "region"])[c].diff()
    ch = d.dropna(subset=["dlnD"])
    out["excludedNonconsecutiveRows"] = int(ch.weekGap.gt(1).sum())
    ch = ch[ch.weekGap == 1]
    ch = ch[ch.groupby(["week", "recordId"]).region.transform("count") == d.region.nunique()]
    out["changeRecordWeeks"] = int(ch.groupby(["week", "recordId"]).ngroups)
    out["change"] = split(ch, ["dlnD", "dlnAware", "dlnConv"], "change")
    return out


def settlement(path, min_units, record_ids=None):
    d = pd.read_csv(path, usecols=["week", "recordId", "regionId", "finalCleared"],
                    dtype={"recordId": "category", "regionId": "category"})
    if record_ids is not None:
        d = d[d.recordId.isin(record_ids)]
    out = {}
    tot = d.groupby("recordId", observed=True).finalCleared.sum()
    big = d[d.recordId.isin(tot[tot >= min_units].index)]
    idx = big.groupby(["recordId", "regionId"], observed=True).finalCleared.idxmax()
    pk = big.loc[idx, ["recordId", "week"]]
    s = pk.groupby("recordId", observed=True).week.agg(lambda w: w.max() - w.min())
    out["peakSpread"] = spread_stats(s)
    # An all-zero market has no sales peak. Its first observed row must not be
    # mistaken for an early regional peak in a delayed-distribution experiment.
    sold = big[big.finalCleared > 0]
    sold_pk = sold.loc[sold.groupby(["recordId", "regionId"], observed=True).finalCleared.idxmax(),
                       ["recordId", "week"]]
    sold_pk = sold_pk[sold_pk.groupby("recordId", observed=True).week.transform("size") >= 2]
    out["soldPeakSpread"] = spread_stats(sold_pk.groupby("recordId", observed=True).week.agg(lambda w: w.max() - w.min()))
    out["regionsWithoutSales"] = int(big.groupby(["recordId", "regionId"], observed=True).finalCleared.sum().eq(0).sum())
    top = d[d.finalCleared > 0].sort_values("finalCleared", ascending=False).groupby(["week", "regionId"], observed=True).head(11)
    overlaps, by_pair = [], {}
    for _, wk in top.groupby("week"):
        sets = sorted((str(r), set(v)) for r, v in wk.groupby("regionId", observed=True).recordId)
        for i in range(len(sets)):
            for j in range(i + 1, len(sets)):
                ov = len(sets[i][1] & sets[j][1]) / 11
                overlaps.append(ov)
                by_pair.setdefault(sets[i][0] + "-" + sets[j][0], []).append(ov)
    out["top11Overlap"] = round(float(np.mean(overlaps)), 4) if overlaps else None
    out["top11OverlapMedian"] = round(float(np.median(overlaps)), 4) if overlaps else None
    # Historical station pairs fall off with distance (SmallLabelAttritionAndChartMethod.md section 4).
    out["top11OverlapByPair"] = {p: round(float(np.mean(v)), 4) for p, v in sorted(by_pair.items())}
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
    positive = d[d.rawDemand > 0]
    pk = positive.loc[positive.groupby(["recordId", "region"], observed=True).rawDemand.idxmax(),
                      ["recordId", "region", "age", "rawDemand"]]
    pk = pk[pk.groupby("recordId", observed=True).region.transform("size") >= 2]
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
    if os.path.exists(settle):
        national = a.prefix + "-completed-week-settlement.csv"
        if not os.path.exists(national):
            sys.exit("Single-format settlement classification requires " + national)
        formats = pd.read_csv(national, usecols=["recordId", "format"])
        single_ids = set(formats.loc[formats["format"] == "Single", "recordId"])
        res["settlementFormat"] = "Single"
        res["settlement"] = settlement(settle, a.min_units, single_ids)
    if len(res) == 1: sys.exit("no input CSVs for " + a.prefix)
    print(json.dumps(res, indent=2))


if __name__ == "__main__":
    main()
