"""Second-rank hit-act funnel. python second_rank.py RUN [RUN...]  (cwd = SimLogs)"""
import sys
import numpy as np, pandas as pd

B5 = [f"label_000{i}" for i in range(1, 6)]
NEW = ["NewSigning", "Rising", "Unsigned"]
pd.set_option("display.width", 250); pd.set_option("display.max_columns", 40)


def grp(lab, tier):
    return np.where(lab.isin(B5), "Big5", tier.replace({"Boutique": "Small"}))


def one(run):
    p = f"{run}-"
    r = pd.read_csv(p + "records.csv", usecols=["week", "year", "recordId", "artistId", "labelId", "currentPosition",
                                                "radioHeat", "coveredRegionCount", "unitsThisWeek", "awareness"])
    c = r[r.currentPosition.between(1, 100)]
    peak = c.groupby("recordId").currentPosition.min()
    t10w = c[c.currentPosition <= 10].groupby("recordId").size()
    a = pd.read_csv(p + "single-launch-attribution.csv",
                    usecols=["week", "year", "singleId", "labelId", "artistId", "career", "quality", "hook", "production",
                             "initialStock", "launchCampaignImpact", "initialAwareness", "actChartRecordAtLaunch"])
    q = pd.read_csv(p + "single-launch-quality.csv", usecols=["singleId", "tier"])
    a = a.merge(q, on="singleId", how="left")
    a["g"] = grp(a.labelId, a.tier.fillna("Small"))
    a["peak"] = a.singleId.map(peak)
    a["h100"] = a.peak.notna()
    a["t40"] = a.peak <= 40
    a["t10"] = a.peak <= 10
    a["t10w"] = a.singleId.map(t10w).fillna(0)
    a["new"] = a.career.isin(NEW)
    a["run"] = run
    # first Top 10 week per act (in run) -> hit act's label/tier at that hit
    t = c[c.currentPosition <= 10].merge(a[["singleId", "g"]], left_on="recordId", right_on="singleId", how="left")
    first = t.sort_values("week").groupby("artistId").first()
    first["run"] = run
    # at-entry features for records that entered, by group
    ent = c.sort_values("week").groupby("recordId").first()
    a = a.join(ent[["radioHeat", "coveredRegionCount", "awareness", "week"]].rename(
        columns=lambda s: "entry_" + s), on="singleId")
    a["entry_age"] = a.entry_week - a.week
    # weeks in H100 and best-week units
    a["h100w"] = a.singleId.map(c.groupby("recordId").size()).fillna(0)
    a["peakunits"] = a.singleId.map(c.groupby("recordId").unitsThisWeek.max())
    return a, first


runs = sys.argv[1:]
A, F = zip(*[one(r) for r in runs])
A = pd.concat(A); F = pd.concat(F)
nlab = A[A.year == 1961].groupby(["run", "g"]).labelId.nunique().groupby("g").mean()

print("=== 1961 launches by tier x career (pooled runs; per-firm = per run per active launching firm)")
x = A[A.year == 1961]
tab = x.groupby(["g", "new"]).agg(launches=("singleId", "size"), q=("quality", "mean"), stock=("initialStock", "median"),
                                    impact=("launchCampaignImpact", "mean"), aware=("initialAwareness", "mean"),
                                    h100=("h100", "mean"), t40=("t40", "mean"), t10=("t10", "mean"), t10w=("t10w", "mean"))
tab["launches"] /= len(runs)
tab["t10|h100"] = x.groupby(["g", "new"]).apply(lambda d: d.t10.sum() / max(d.h100.sum(), 1))
tab["t40|h100"] = x.groupby(["g", "new"]).apply(lambda d: d.t40.sum() / max(d.h100.sum(), 1))
tab["t10|t40"] = x.groupby(["g", "new"]).apply(lambda d: d.t10.sum() / max(d.t40.sum(), 1))
print(tab.round(3).to_string())
print("launching firms per run:", nlab.round(1).to_dict())

print("\n=== entrants (H100), 1961: at entry and run", )
e = x[x.h100]
print(e.groupby(["g", "new"]).agg(n=("singleId", "size"), age=("entry_age", "median"), heat=("entry_radioHeat", "mean"),
                                  regions=("entry_coveredRegionCount", "mean"), aware=("entry_awareness", "mean"),
                                  peakunits=("peakunits", "median"), h100w=("h100w", "mean"), q=("quality", "mean"))
      .assign(n=lambda d: d.n / len(runs)).round(3).to_string())

print("\n=== quality-matched entrant conversion t10|h100, 1961 newcomers + established, by quality bin")
e = e.assign(qb=pd.cut(e.quality, [0, .68, .72, .76, .80, 1]))
print(e.groupby(["qb", "g"], observed=True).t10.mean().unstack().round(3).to_string())
print(e.groupby(["qb", "g"], observed=True).size().unstack().to_string())

print("\n=== hit acts minted (first Top 10 in 1960-61) by tier at hit, per run; and per firm")
F2 = F[F.year.isin([1960, 1961])]
m = F2.groupby(["year", "g"]).size().unstack(fill_value=0) / len(runs)
print(m.round(1).to_string())
print("per launching firm 1961:", (m.loc[1961] / nlab).round(2).to_dict() if 1961 in m.index else "")

print("\n=== follow-up in 52 wk after first Top 10 (hits weeks 1-52 of 1960-61, i.e. first hit week <= 52)")
rows = []
for run in runs:
    a = A[A.run == run]; f = F[(F.run == run) & (F.week <= 52)]
    for act, row in f.iterrows():
        s = a[(a.artistId == act) & (a.week > row.week) & (a.week <= row.week + 52)]
        rows.append(dict(g=row.g, launches=len(s), h100=s.h100.sum(), t10=s.t10.sum(), t10w=s.t10w.sum(),
                         samelabel=(s.labelId == row.labelId).mean() if len(s) else np.nan,
                         q=s.quality.mean(), stock=s.initialStock.median(), aware=s.initialAwareness.mean()))
R = pd.DataFrame(rows)
print(R.groupby("g").agg(acts=("launches", "size"), launches=("launches", "mean"), h100=("h100", "mean"), t10=("t10", "mean"),
                         t10w=("t10w", "mean"), samelabel=("samelabel", "mean"), q=("q", "mean"), stock=("stock", "median"),
                         aware=("aware", "mean")).assign(acts=lambda d: d.acts / len(runs)).round(3).to_string())
