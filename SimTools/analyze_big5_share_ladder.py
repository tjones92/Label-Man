"""Big-5 slot share at Top 10 / Top 40 / Hot 100, per-single premium and field entry rate, per run and year.

usage: analyze_big5_share_ladder.py <run> [<run> ...]
Reads SimLogs/<run>-records.csv, -single-launch-attribution.csv and -single-launch-quality.csv.
Big-5 = label_0001..label_0005 exactly (never compare label ids as strings).
"""
import sys
import pandas as pd

B5 = {f"label_000{i}" for i in range(1, 6)}
YEARS = (1960, 1961)


def score(run):
    p = f"SimLogs/{run}-"
    r = pd.read_csv(p + "records.csv", usecols=["year", "recordId", "labelId", "labelTier", "currentPosition", "unitsThisWeek"])
    r = r[r.year.isin(YEARS) & r.currentPosition.between(1, 100)]
    r["grp"] = r.labelTier.where(~r.labelId.isin(B5), "Big5")
    a = pd.read_csv(p + "single-launch-attribution.csv", usecols=["year", "singleId", "labelId"])
    q = pd.read_csv(p + "single-launch-quality.csv", usecols=["singleId", "tier"])
    a = a[a.year.isin(YEARS)].merge(q, on="singleId", how="left")
    a.loc[a.labelId.isin(B5), "tier"] = "Big5"
    t10 = r[r.currentPosition <= 10].groupby("recordId").size()
    a["t10"] = a.singleId.map(t10).fillna(0)
    a["h100"] = a.singleId.isin(set(r.recordId))
    out = {}
    for y in YEARS:
        d = r[r.year == y]
        al = a[a.year == y]
        row = {}
        for name, depth in (("T10", 10), ("T40", 40), ("H100", 100)):
            s = d[d.currentPosition <= depth]
            row[name] = 100 * (s.grp == "Big5").mean()
        top = d[d.currentPosition <= 10]
        for tier in ("MidTier", "Independent", "Boutique", "Small"):
            row[tier[:3] + "T10"] = 100 * (top.grp == tier).mean()
        b, f = al[al.tier == "Big5"], al[al.tier != "Big5"]
        row["launch%"] = 100 * len(b) / max(1, len(al))
        row["premium"] = b.t10.mean() / max(1e-9, f.t10.mean())
        small = al[al.tier.isin(["Independent", "Boutique", "Small"])]
        row["fieldH100%"] = 100 * small.h100.mean()
        row["b5H100%"] = 100 * b.h100.mean()
        row["unitsM"] = d.unitsThisWeek.sum() / 1e6
        out[y] = row
    return out


def main():
    rows = []
    for run in sys.argv[1:]:
        for y, row in score(run).items():
            rows.append({"run": run, "year": y, **row})
    df = pd.DataFrame(rows).set_index(["run", "year"])
    pd.set_option("display.width", 250)
    print(df.round(1).to_string())


if __name__ == "__main__":
    main()
