"""Year-1 closures of starting-Small labels by opening-cash band, with releases and gross.

Usage: python SimTools/analyze_small_closure_by_opening_cash.py SimLogs/<prefix>
"""
import sys
import pandas as pd

prefix = sys.argv[1]
CLOSED = ["Defunct", "Bankrupt"]
fin = pd.read_csv(prefix + "-label-finance.csv", usecols=["week", "labelId", "labelTier", "status", "cashReserves",
                                                          "weeklyGross", "monthlyOverhead", "outstandingWholesaleReceivables"])
w1 = fin[fin.week == 1].set_index("labelId")
small = w1[w1.labelTier == "Small"].index
c = fin[fin.labelId.isin(small)]
end = c.sort_values("week").groupby("labelId").last()
closed = end[end.status.isin(CLOSED)].index
rec = pd.read_csv(prefix + "-records.csv", usecols=["recordId", "labelId", "weeksSinceRelease", "week"])
rel = rec[rec.labelId.isin(small)].drop_duplicates("recordId")
rel = rel[rel.week - rel.weeksSinceRelease >= 1]  # released in-run
nrel = rel.groupby("labelId").recordId.nunique()
gross = c.groupby("labelId").weeklyGross.sum()
closeWeek = c[c.status.isin(CLOSED)].groupby("labelId").week.min()
df = pd.DataFrame({"cash0": w1.loc[small].cashReserves, "closed": pd.Series(small.isin(closed), index=small),
                   "releases": nrel.reindex(small).fillna(0), "gross": gross.reindex(small).fillna(0),
                   "closeWeek": closeWeek.reindex(small)})
df["cashBand"] = pd.cut(df.cash0, [0, 800, 1500, 2500, 4000, 9000])
print(df.groupby("cashBand", observed=True).agg(n=("closed", "size"), closedShare=("closed", "mean"),
      releasesMed=("releases", "median"), grossMed=("gross", "median")).round(2))
cl = df[df.closed]
print("\nclosed: releases==0 share %.2f; median close week %.0f; median gross before closing %.0f" % (
    (cl.releases == 0).mean(), cl.closeWeek.median(), cl.gross.median()))
# receivables outstanding at closure
last = c[c.labelId.isin(closed)].sort_values("week").groupby("labelId").last()
print("receivables at last row median %.0f; cash median %.0f" % (last.outstandingWholesaleReceivables.median(), last.cashReserves.median()))
