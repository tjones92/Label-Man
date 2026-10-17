"""Who goes bankrupt under regional arrival (SimTools/RegionalSyncDesign.md section 6c).

Usage:  py -3 -I SimTools/analyze_arrival_bankruptcies.py SimLogs/<prefix> [SimLogs/<prefix> ...]

Per run: every label that closed for bankruptcy (from <prefix>-console.log), with its tier, archetype, age at closure,
reach, how many regions its singles were covered in, and the share of its single units sold outside its home region.
The same columns are summarised for all labels, so a population shift shows against the base.
"""
import re, sys
import numpy as np, pandas as pd


def labels(prefix):
    fin = pd.read_csv(prefix + "-label-finance.csv", usecols=["week", "labelId", "labelName", "archetype", "isHistorical",
                      "labelTier", "status", "ownedReach", "nationalReach", "cashReserves"])
    first = fin.sort_values("week").groupby("labelId").first()
    alive = fin[fin.status != "Defunct"].sort_values("week").groupby("labelId").last()
    out = first[["labelName", "archetype", "isHistorical", "ownedReach", "nationalReach"]].copy()
    out["birthWeek"] = first.week
    out["tierAtEnd"] = alive.labelTier.reindex(out.index).fillna(first.labelTier)
    out["lastWeek"] = alive.week.reindex(out.index)
    text = open(prefix + "-console.log", encoding="utf-8", errors="replace").read()
    bankrupt = set(re.findall(r"\[LabelManager\] (.*) has closed\. Reason: Bankruptcy", text))
    exited = set(re.findall(r"\[LabelManager\] (.*) has closed\. Reason: Competitive exit", text))
    out["fate"] = np.where(out.labelName.isin(bankrupt), "bankrupt", np.where(out.labelName.isin(exited), "exit", "alive"))

    rec = pd.read_csv(prefix + "-records.csv", usecols=["recordId", "labelId", "coveredRegionCount", "isPlayerOwned"],
                      dtype={"recordId": "category", "labelId": "category"}).drop_duplicates("recordId")
    geo = pd.read_csv(prefix + "-label-geography.csv", usecols=["labelId", "homeRegion"]).set_index("labelId").homeRegion
    st = pd.read_csv(prefix + "-completed-week-settlement-regional.csv", usecols=["recordId", "regionId", "finalCleared"],
                     dtype={"recordId": "category", "regionId": "category"})
    st = st.groupby(["recordId", "regionId"], observed=True).finalCleared.sum().reset_index()
    st = st.merge(rec[["recordId", "labelId"]], on="recordId")
    st["home"] = st.regionId.astype(str) == st.labelId.astype(str).map(geo)
    units = st.groupby("labelId", observed=True).finalCleared.sum()
    away = st[~st.home].groupby("labelId", observed=True).finalCleared.sum()
    out["units"] = units.reindex(out.index).fillna(0)
    out["awayShare"] = (away.reindex(out.index).fillna(0) / out.units.replace(0, np.nan))
    out["covered"] = rec.groupby("labelId", observed=True).coveredRegionCount.mean().reindex(out.index)
    return out


def summary(df):
    g = df.groupby("fate")
    s = pd.DataFrame({"n": g.size(), "ageWk": g.apply(lambda x: (x.lastWeek - x.birthWeek).median(), include_groups=False),
                      "covered": g.covered.median(), "awayShare": g.awayShare.median(),
                      "nationalReach": g.nationalReach.median(), "unitsMedian": g.units.median()})
    return s.round(3)


def main():
    for prefix in sys.argv[1:]:
        df = labels(prefix)
        b = df[df.fate == "bankrupt"]
        print(f"\n=== {prefix}: {len(b)} bankrupt of {len(df)} labels")
        print(summary(df).to_string())
        print("bankrupt by tier:", b.tierAtEnd.value_counts().to_dict())
        print("bankrupt by archetype:", b.archetype.value_counts().to_dict())
        print("bankrupt historical:", int(b.isHistorical.sum()))
        print("bankrupt covered-region bins:", pd.cut(b.covered, [-0.1, 1, 2, 4, 7]).value_counts().sort_index().to_dict())
        print("bankrupt age bins (wk):", pd.cut(b.lastWeek - b.birthWeek, [-1, 13, 26, 52, 104]).value_counts().sort_index().to_dict())
        print("bankrupt with no single units:", int((b.units == 0).sum()))
        s = b[b.units > 0]
        print(f"bankrupt sellers: {len(s)}  tier {s.tierAtEnd.value_counts().to_dict()}")
        print("  covered median %.2f  awayShare median %.3f  units median %.0f  ageWk median %.0f  reach median %.3f" % (
            s.covered.median(), s.awayShare.median(), s.units.median(), (s.lastWeek - s.birthWeek).median(), s.nationalReach.median()))
        print("  covered-region bins:", pd.cut(s.covered, [-0.1, 1, 2, 4, 7]).value_counts().sort_index().to_dict())
        print("  archetype:", s.archetype.value_counts().to_dict())
        sellers = df[df.units > 0]
        rate = sellers.groupby(pd.cut(sellers.covered, [-0.1, 1, 2, 4, 7]), observed=False).fate.apply(
            lambda f: f"{(f == 'bankrupt').sum()}/{len(f)}")
        print("  bankrupt/sellers by coverage:", rate.to_dict())


if __name__ == "__main__":
    main()
