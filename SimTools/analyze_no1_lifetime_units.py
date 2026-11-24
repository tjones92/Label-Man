"""Lifetime units of #1 records by arm (no1-sales-inside-history-bracket: median ~0.80M, 77% >= 500k; bracket 0.5-1.5M).

usage: analyze_no1_lifetime_units.py SEEDS name=fmt [name=fmt ...]   ({s} = seed)
#1 records whose first chart week is after week 10 (the warm-up cohort is excluded) and that reached #1 by YEAR_MAX.
Lifetime = max totalUnitsSold in the run, so records near the run end are cut short; compare arms at equal length.
"""
import sys
import pandas as pd

seeds = [int(x) for x in sys.argv[1].split(",")]
rows = []
for arg in sys.argv[2:]:
    name, fmt = arg.split("=", 1)
    for s in seeds:
        r = pd.read_csv(f"SimLogs/{fmt.format(s=s)}-records.csv", usecols=["week", "recordId", "currentPosition", "totalUnitsSold"])
        last = r.week.max()
        first = r[r.currentPosition.between(1, 100)].groupby("recordId").week.min()
        n1 = r[r.currentPosition == 1].groupby("recordId").week.min()
        keep = n1[(first.reindex(n1.index) > 10) & (n1 <= last - 20)].index
        life = r[r.recordId.isin(keep)].groupby("recordId").totalUnitsSold.max() / 1e6
        rows.append(dict(arm=name, seed=s, no1s=len(life), median=life.median(), q25=life.quantile(.25), q75=life.quantile(.75),
                         ge500k=100 * (life >= .5).mean(), ge1M=100 * (life >= 1).mean(), max=life.max()))
d = pd.DataFrame(rows)
pd.set_option("display.width", 200)
print(d.round(2).to_string(index=False))
print(d.groupby("arm", sort=False)[["no1s", "median", "q25", "q75", "ge500k", "ge1M", "max"]].mean().round(2).to_string())
