"""Calibration guards for regional audience sub-steps (SimTools/RegionalAudienceDesign.md, "Targets and guards").

Usage:  py -3 -I SimTools/analyze_regional_guards.py SimLogs/<control-prefix> SimLogs/<treatment-prefix> [...]

Per arm and year: single units, Major-family chart share, C4, chart entries, genre chart-vs-market share gap,
#1 records and weeks, units at #1/#10/#100 (median weekly), airplay share of top-40 chart points, mean chart life
of charted records, and label closures (from <prefix>-console.log when present).
"""
import os, re, sys
import numpy as np, pandas as pd


def airplay_share_top40(frame):
    if "unsampledChartPoints" not in frame.columns:
        return float("nan")
    return float((1 - frame.unitsThisWeek / frame.unsampledChartPoints).clip(0, 1).median())


def arm(prefix):
    out = {}
    roll = pd.read_csv(prefix + "-decade-annual-rollup.csv")
    conc = pd.read_csv(prefix + "-concentration.csv")
    shape = pd.read_csv(prefix + "-genre-decade-shape.csv")
    rec = pd.read_csv(prefix + "-records.csv", usecols=lambda c: c in ["week", "year", "recordId", "currentPosition", "unitsThisWeek", "chartPoints", "unsampledChartPoints"])
    life = pd.read_csv(prefix + "-lifecycles.csv")
    for y in sorted(roll.year.unique()):
        r = {}
        r["singleUnits"] = float(roll.loc[roll.year == y, "singleUnits"].iloc[0])
        c = conc[conc.year == y]
        if len(c):
            r["majorShare"] = float(c.majorFamilyChartShare.iloc[0]); r["c4"] = float(c.c4ChartShare.iloc[0])
            r["chartEntries"] = int(c.chartEntries.iloc[0])
        s = shape[shape.year == y]
        r["genreGap"] = float(s.chartWeekShareMinusMarketShare.abs().sum())
        ry = rec[(rec.year == y) & (rec.currentPosition > 0)]
        r["no1Records"] = int(ry[ry.currentPosition == 1].recordId.nunique())
        r["no1Weeks"] = int((ry.currentPosition == 1).sum())
        med = ry.groupby("currentPosition").unitsThisWeek.median()
        for p in (1, 10, 100):
            r[f"u{p}"] = float(med.get(p, np.nan))
        t40 = ry[(ry.currentPosition <= 40) & (ry.chartPoints > 0)]
        # Published points include a multiplicative survey draw. Their residual
        # against units is not the contribution from physical radio rotation.
        r["sampledPointResidualTop40"] = float((1 - t40.unitsThisWeek / t40.chartPoints).clip(lower=0).median())
        r["airplayShareTop40"] = airplay_share_top40(t40)
        out[int(y)] = r
    charted = life[life.weeksOnChart > 0]
    out["life"] = {"chartedRecords": int(len(charted)), "meanWeeksOnChart": float(charted.weeksOnChart.mean()),
                   "meanWeeksAtNo1": float(charted[charted.weeksAtNumberOne > 0].weeksAtNumberOne.mean())}
    log = prefix + "-console.log"
    if not os.path.exists(log):
        log = None
    if log:
        with open(log, encoding="utf-8", errors="replace") as f:
            text = f.read()
        out["life"]["closures"] = len(re.findall(r"\[LabelManager\] .* has closed\.", text))
        out["life"]["bankruptcies"] = len(re.findall(r"has closed\. Reason: Bankruptcy", text))
    return out


def main():
    arms = {p: arm(p) for p in sys.argv[1:]}
    names = list(arms)
    years = sorted(k for k in arms[names[0]] if isinstance(k, int))
    keys = list(arms[names[0]][years[0]].keys())
    print("metric/year".ljust(22) + "".join(os.path.basename(n)[:24].rjust(26) for n in names))
    for y in years:
        for k in keys:
            vals = [arms[n].get(y, {}).get(k, np.nan) for n in names]
            print(f"{y} {k}".ljust(22) + "".join(f"{v:26.4g}" for v in vals))
    for k in arms[names[0]]["life"]:
        print(f"life {k}".ljust(22) + "".join(f"{arms[n]['life'].get(k, np.nan):26.4g}" for n in names))


if __name__ == "__main__":
    main()
