"""Follow-up singles after an act's first in-run Top 10 week, by the releasing label's tier (Big-5 split out).

usage: analyze_hit_followups_by_tier.py <run> [<run> ...]
Reads SimLogs/<run>-records.csv, -single-launch-attribution.csv and -single-launch-quality.csv; singles launched 1960-61.
"""
import sys
import pandas as pd

B5 = {f"label_000{i}" for i in range(1, 6)}


def main():
    pd.set_option("display.width", 220)
    for run in sys.argv[1:]:
        p = f"SimLogs/{run}-"
        r = pd.read_csv(p + "records.csv", usecols=["week", "year", "recordId", "artistId", "currentPosition"])
        r = r[(r.year <= 1961) & r.currentPosition.between(1, 100)]
        a = pd.read_csv(p + "single-launch-attribution.csv",
                        usecols=["week", "year", "singleId", "labelId", "artistId", "quality", "initialAwareness", "artistMomentum"])
        q = pd.read_csv(p + "single-launch-quality.csv", usecols=["singleId", "tier"])
        a = a[a.year <= 1961].merge(q, on="singleId", how="left")
        a.loc[a.labelId.isin(B5), "tier"] = "Big5"
        a.loc[a.tier.isin(["Small", "Boutique"]), "tier"] = "Small/Bout"
        top = r[r.currentPosition <= 10]
        first = top.groupby("artistId").week.min()
        a["firstT10"] = a.artistId.map(first)
        a["t10"] = a.singleId.map(top.groupby("recordId").size()).fillna(0)
        a["h100"] = a.singleId.isin(set(r.recordId))
        f = a[a.week > a.firstT10]
        acts = a[a.firstT10.notna()].groupby("tier").artistId.nunique()
        g = f.groupby("tier").agg(followups=("t10", "size"), acts=("artistId", "nunique"), momentum=("artistMomentum", "mean"),
                                  awareness=("initialAwareness", "mean"), quality=("quality", "mean"),
                                  h100=("h100", "mean"), t10PerSingle=("t10", "mean"))
        g["hitActs"] = acts
        g["t10WeeksPerHitAct"] = f.groupby("tier").t10.sum() / acts
        print("==", run)
        print(g.round(3).to_string())


if __name__ == "__main__":
    main()
