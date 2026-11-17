"""New hit acts (first Top 10 record launched as NewSigning/Rising/Unsigned) and their 52-week follow-ups, by tier."""
import sys
import numpy as np, pandas as pd

B5 = [f"label_000{i}" for i in range(1, 6)]
NEW = ["NewSigning", "Rising", "Unsigned"]
pd.set_option("display.width", 250); pd.set_option("display.max_columns", 40)
rows, lanes, srcs = [], [], []
for run in sys.argv[1:]:
    p = f"{run}-"
    r = pd.read_csv(p + "records.csv", usecols=["week", "recordId", "artistId", "currentPosition"])
    c = r[r.currentPosition.between(1, 100)]
    peak = c.groupby("recordId").currentPosition.min()
    t10w = c[c.currentPosition <= 10].groupby("recordId").size()
    a = pd.read_csv(p + "single-launch-attribution.csv", usecols=["week", "singleId", "labelId", "artistId", "career", "quality",
                                                                  "hook", "production", "labelProduction", "initialStock", "initialAwareness"])
    q = pd.read_csv(p + "single-launch-quality.csv", usecols=["singleId", "tier"])
    a = a.merge(q, on="singleId", how="left")
    a["g"] = np.where(a.labelId.isin(B5), "Big5", a.tier.fillna("Small").replace({"Boutique": "Small"}))
    a["peak"] = a.singleId.map(peak)
    a["t10w"] = a.singleId.map(t10w).fillna(0)
    ln = pd.read_csv(p + "single-release-lanes.csv", usecols=["recordId", "releaseLane"]).drop_duplicates("recordId")
    w = pd.read_csv(p + "writing-launches.csv", usecols=["recordId", "source", "compositionHook"]).drop_duplicates("recordId")
    a = a.merge(ln, left_on="singleId", right_on="recordId", how="left").drop(columns="recordId")
    a = a.merge(w, left_on="singleId", right_on="recordId", how="left").drop(columns="recordId")
    hits = a[a.peak <= 10].sort_values("week").groupby("artistId").first()
    hits = hits[(hits.week <= 78) & hits.career.isin(NEW)]  # new hit acts, first hit by mid-1961, 52 wk window inside 104
    for act, h in hits.iterrows():
        s = a[(a.artistId == act) & (a.week > h.week) & (a.week <= h.week + 52)]
        rows.append(dict(run=run, g=h.g, hitq=h.quality, hitt10w=h.t10w, n=len(s), h100=s.peak.notna().sum(), t10=(s.peak <= 10).sum(),
                         t10w=s.t10w.sum(), q=s.quality.mean(), hook=s.hook.mean(), prod=s.production.mean(),
                         lprod=s.labelProduction.mean(), stock=s.initialStock.median(), aware=s.initialAwareness.mean(),
                         moved=(s.labelId != h.labelId).any()))
        for l in s.releaseLane.fillna("?"): lanes.append((h.g, l))
        for l in s.source.fillna("?"): srcs.append((h.g, l))
R = pd.DataFrame(rows); k = len(sys.argv) - 1
print(R.groupby("g").agg(acts=("n", "size"), hitq=("hitq", "mean"), hit_t10w=("hitt10w", "mean"), launches=("n", "mean"), h100=("h100", "mean"),
                         t10=("t10", "mean"), t10w=("t10w", "mean"), q=("q", "mean"), hook=("hook", "mean"), prod=("prod", "mean"),
                         lprod=("lprod", "mean"), stock=("stock", "median"), aware=("aware", "mean"), moved=("moved", "mean"))
      .assign(acts=lambda d: d.acts / k).round(3).to_string())
L = pd.DataFrame(lanes, columns=["g", "lane"]); print("\nfollow-up lanes (share)"); print(pd.crosstab(L.lane, L.g, normalize="columns").round(2).to_string())
S = pd.DataFrame(srcs, columns=["g", "src"]); print("\nfollow-up song source (share)"); print(pd.crosstab(S.src, S.g, normalize="columns").round(2).to_string())
