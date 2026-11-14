"""usage: shape.py name=fmt ...  -- chart-shape sanity for 1961 (mean of 4 seeds)"""
import sys, os
import numpy as np, pandas as pd
pd.set_option("display.width", 250)
rows = []
for spec in sys.argv[1:]:
    name, fmt = spec.split("=", 1); per = []
    for s in [2002, 1001, 3003, 6006]:
        r = pd.read_csv(f"SimLogs/{fmt.format(s=s)}-records.csv", usecols=["week", "year", "recordId", "artistId", "currentPosition", "unitsThisWeek", "totalUnitsSold", "weeksSinceRelease"])
        y = r[(r.year == 1961) & r.currentPosition.between(1, 100)]
        t10 = y[y.currentPosition <= 10]; n1 = y[y.currentPosition == 1]
        deb = y.sort_values("week").groupby("recordId").head(1)
        per.append(dict(h100recs=y.recordId.nunique(), t40recs=y[y.currentPosition <= 40].recordId.nunique(), t10recs=t10.recordId.nunique(), t10acts=t10.artistId.nunique(),
                        no1recs=n1.recordId.nunique(), no1wkMean=len(n1) / max(1, n1.recordId.nunique()), no1units=n1.unitsThisWeek.median(), no1maxWk=y.unitsThisWeek.max(),
                        no10units=y[y.currentPosition == 10].unitsThisWeek.median(), no40units=y[y.currentPosition == 40].unitsThisWeek.median(), no100units=y[y.currentPosition == 100].unitsThisWeek.median(),
                        t10wkPerRec=len(t10) / max(1, t10.recordId.nunique()), debutPos=deb.currentPosition.median(), debutAge=deb.weeksSinceRelease.median(),
                        topTotal=y.groupby("recordId").totalUnitsSold.max().max(), millionSellers=(y.groupby("recordId").totalUnitsSold.max() >= 1e6).sum()))
    d = pd.DataFrame(per).mean(); d.name = name; rows.append(d)
print(pd.DataFrame(rows).round(1).T.to_string())
