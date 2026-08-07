"""Offline sizing for the station drop: replay the hazard against real support trajectories.

peakWeeklyUnits is a RUNNING max, so support = units / cummax(units) -- reconstructed here exactly
as FinalizeWeeklySales maintains it, including weeksSincePeakUnits.
"""
import sys, numpy as np, pandas as pd
BASE = r"C:\Project\Label-Man\SimLogs"
run = sys.argv[1]
CEIL, FLOOR, MAXH = float(sys.argv[2]), float(sys.argv[3]), float(sys.argv[4])
GRACE, BURN_ON, BURN_FULL = 2, 8, 8

df = pd.read_csv(rf"{BASE}\{run}-records.csv",
                 usecols=["week","recordId","weeksSinceRelease","unitsThisWeek","currentPosition"],
                 low_memory=False)
df = df.sort_values(["recordId","week"])
peak = df[df.currentPosition>=1].groupby("recordId").currentPosition.min()
g = df.groupby("recordId", sort=False)
df["runpeak"] = g.unitsThisWeek.cummax()
df["support"] = np.where(df.runpeak>0, df.unitsThisWeek/df.runpeak.replace(0,np.nan), 1.0)
# weeksSincePeakUnits: weeks since the running max was last raised
df["isnew"] = df.unitsThisWeek >= df.runpeak
grp = df.groupby("recordId", sort=False)
df["blk"] = grp.isnew.cumsum()
df["wsp"] = df.groupby(["recordId","blk"], sort=False).cumcount()

fade = np.clip((CEIL - df.support)/(CEIL-FLOOR), 0, 1)
burn = np.clip((df.wsp - BURN_ON)/BURN_FULL, 0, 1)
df["h"] = np.where(df.wsp < GRACE, 0.0, MAXH*(1-(1-fade)*(1-burn)))

for label, ids in [("top-10", peak[peak<=10].index), ("11-40", peak[peak.between(11,40)].index),
                   ("41-100", peak[peak.between(41,100)].index)]:
    s = df[df.recordId.isin(ids)]
    print(f"\n=== {label} peakers (n={len(ids)}) : hazard ceil={CEIL} floor={FLOOR} max={MAXH}")
    print(f"{'wk':>3}{'n':>6}{'support':>9}{'wsp':>6}{'hazard':>8}{'survive':>9}")
    surv = 1.0
    a = s[s.weeksSinceRelease.between(1,26)].groupby("weeksSinceRelease").agg(
        n=("h","size"), sup=("support","median"), wsp=("wsp","median"), h=("h","mean"))
    for wk, r in a.iterrows():
        surv *= (1-r.h)
        print(f"{wk:>3}{int(r.n):>6}{r.sup:>9.3f}{r.wsp:>6.0f}{r.h:>8.3f}{surv:>9.3f}")
