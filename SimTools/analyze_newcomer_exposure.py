"""Connected newcomer singles at launch, Big-5 vs field (Big5VolumeAndPremiumHandoff.md, candidate 3). Usage from the repo root:
py SimTools/analyze_newcomer_exposure.py RUN [RUN ...]      (needs RUN-newcomer-connect-trace.csv[.gz], from --newcomer-connect-trace)
Per run: connected AI newcomer singles traced (first 12 weeks), regions covered and fit-exposed at age 1, launch stock and
regional awareness at age 1, cleared units in weeks 1-3, Hot 100 entry within the trace and Top 10 (peak from records.csv)."""
import os
import sys
import pandas as pd

B5 = {f"label_000{i}" for i in range(1, 6)}


def csv(path):
    return path if os.path.exists(path) or not os.path.exists(path + ".gz") else path + ".gz"


def one(run):
    p = f"SimLogs/{run}-"
    t = pd.read_csv(csv(p + "newcomer-connect-trace.csv"), usecols=["recordId", "labelId", "region", "age", "covered",
                                                                     "fitExposureAge", "unitsInStores", "awareness", "cleared", "position"])
    r = pd.read_csv(csv(p + "records.csv"), usecols=["recordId", "currentPosition"])
    peak = r[r.currentPosition.between(1, 100)].groupby("recordId").currentPosition.min()
    a1 = t[t.age == 1]
    rec = a1.groupby("recordId").agg(labelId=("labelId", "first"), covered=("covered", "sum"),
                                     exposed=("fitExposureAge", lambda s: (s == 0).sum()), stock=("unitsInStores", "sum"),
                                     aware=("awareness", "mean"))
    rec["units13"] = t[t.age.between(1, 3)].groupby("recordId").cleared.sum()
    rec["h100"] = t[t.position.between(1, 100)].groupby("recordId").size().reindex(rec.index).fillna(0) > 0
    rec["t10"] = rec.index.map(peak).to_series(index=rec.index).le(10)
    rec["b5"] = rec.labelId.isin(B5)
    g = rec.groupby("b5").agg(n=("labelId", "size"), covered=("covered", "mean"), exposed=("exposed", "mean"), stock=("stock", "median"),
                              aware=("aware", "mean"), units13=("units13", "median"), h100=("h100", "mean"), t10=("t10", "mean"))
    g.index = ["field", "Big-5"]
    return g.T


for run in sys.argv[1:]:
    print(f"== {run}")
    print(one(run).round(3).to_string())
