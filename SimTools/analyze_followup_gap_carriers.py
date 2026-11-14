"""Hit-act follow-up gap: where it sits and what carries it (FollowUpGapFableHandoff.md, finding 2026-11-14).

usage: analyze_followup_gap_carriers.py name=fmt ...      ({s} = seed; seeds 2002, 1001; runs need --followup-chart-trace)

Per arm it prints:
  1. launch frame  - singles by acts with a Top 40 in the 26 weeks before launch, 1961-63: connect, Hot 100, Top 40, Top 10.
  2. history frame - hit act in Y = first Top 10 in Y-1 (HistoricalLabelTierBalanceResearch.md 17aa): launches and Hot 100
                     debuts per act-year, Top 10 per debut. History: debuts 3.28 Big-5 vs 2.18 other; Top 10 per debut 15.96 vs 15.93.
  3. regional gates - connected follow-ups in single-demand-stages.csv: regions open (not held for arrival) and fit-exposed at age 1.
  4. accounting    - logit of Hot 100 entry / Top 40 on the keyed draw, quality, launch awareness, stock and the two gates;
                     field singles are moved onto the Big-5 distribution one input at a time (quantile map, cumulative).
"""
import sys
import numpy as np, pandas as pd
import statsmodels.api as sm
import keyed_fit_draws as kfd

SEEDS = (2002, 1001)
# --subset=new | experienced: only hit acts whose sole charted record before the launch is the hit (and no opening
# career), or the rest. Sections 2 (history frame) is unaffected; it has its own hit-act definition.
SUBSET = next((x.split("=", 1)[1] for x in sys.argv if x.startswith("--subset=")), None)
sys.argv = [x for x in sys.argv if not x.startswith("--subset=")]
B5 = {f"label_000{i}" for i in range(1, 6)}
ORDER = ["Big5", "MidTier", "Independent", "Small/Bout"]
pd.set_option("display.width", 250)


def group(label, year, tiers):
    g = pd.Series(tiers.reindex(pd.MultiIndex.from_arrays([label, year])).fillna("Independent").values, index=label.index)
    g[label.isin(B5)] = "Big5"; g[g.isin(["Small", "Boutique"])] = "Small/Bout"; g[g.eq("Major")] = "MidTier"
    return g


def load(name, seed):
    run = "SimLogs/" + name
    r = pd.read_csv(run + "-records.csv", usecols=["week", "year", "recordId", "artistId", "labelId", "currentPosition"])
    tiers = pd.read_csv(run + "-company-monthly.csv", usecols=["year", "labelId", "tier"]).drop_duplicates(["labelId", "year"]).set_index(["labelId", "year"]).tier
    a = pd.read_csv(run + "-single-launch-attribution.csv")
    c = r[r.currentPosition.between(1, 100)]
    a["censored"] = a.week > r.week.max() - 12
    a["g"] = group(a.labelId, a.year, tiers)
    a["peak"] = a.singleId.map(c.groupby("recordId").currentPosition.min())
    a = a.sort_values("week")
    t40 = c[c.currentPosition <= 40][["artistId", "week"]].drop_duplicates().sort_values("week").rename(columns={"week": "t40wk"})
    prev = pd.merge_asof(a[["week", "artistId", "singleId"]], t40, left_on="week", right_on="t40wk", by="artistId", allow_exact_matches=False)
    a["hot"] = (a.week.values - prev.set_index("singleId").reindex(a.singleId).t40wk.values) <= 26
    # New act: the act's only charted record before this launch is its hit, and it is not an opening career (pre-1960 history).
    debut = c.groupby("recordId").agg(artistId=("artistId", "first"), debutWeek=("week", "min")).sort_values("debutWeek")
    debut["n"] = debut.groupby("artistId").cumcount() + 1
    pc = pd.merge_asof(a[["week", "artistId", "singleId"]].sort_values("week"), debut[["artistId", "debutWeek", "n"]],
                       left_on="week", right_on="debutWeek", by="artistId", allow_exact_matches=False)
    a["priorCharted"] = pc.set_index("singleId").reindex(a.singleId).n.fillna(0).values
    a["newAct"] = (a.priorCharted <= 1) & a.openingArchetype.isna()
    kfd.configure(*kfd.params_for_run(name))
    a["z"] = [kfd.national_z(seed, s) for s in a.singleId]
    rec = a.actChartRecordAtLaunch.fillna(0).values if "actChartRecordAtLaunch" in a else np.zeros(len(a))
    a["kink"] = kfd.KINK - kfd.LAUNCH_WEIGHT * (a.initialAwareness.values - kfd.LAUNCH_REF) - kfd.ACT_RECORD_WEIGHT * rec
    a["sizeKink"] = a.kink + (kfd.ACT_RECORD_WEIGHT * rec if kfd.ACT_RECORD_ODDS_ONLY else 0.0)
    a["oddsOnly"] = kfd.ACT_RECORD_ODDS_ONLY   # the act record moves the connect bar only; a hit's size is read from sizeKink
    a["connected"] = a.z >= a.kink
    a["fitLog"] = [kfd.national_log(z, aw, x) for z, aw, x in zip(a.z, a.initialAwareness, rec)]
    a["seed"] = seed
    # regional gates at age 1 from the follow-up trace
    s = pd.read_csv(run + "-single-demand-stages.csv", usecols=["recordId", "age", "potentialAudience", "awareBuyers", "commercialFitWithoutSupportAtCapture"])
    s = s[s.age == 1].merge(a[["singleId", "fitLog"]].rename(columns={"singleId": "recordId"}), on="recordId")
    s["open"] = s.awareBuyers > 0
    s["exposed"] = np.log(s.commercialFitWithoutSupportAtCapture.clip(lower=1e-6)) - s.fitLog > -1.5   # exposed ~ N(-0.25, 0.5); capped <= -2.8
    s["paO"] = s.potentialAudience * s.open; s["paE"] = s.potentialAudience * s.exposed
    p = s.groupby("recordId").agg(nOpen=("open", "sum"), nExposed=("exposed", "sum"), paO=("paO", "sum"), paE=("paE", "sum"), pa=("potentialAudience", "sum"))
    a = a.join(pd.DataFrame({"nOpen": p.nOpen, "nExposed": p.nExposed, "audOpen": p.paO / p.pa, "audExposed": p.paE / p.pa}), on="singleId")
    # history frame
    recs = c.groupby("recordId").agg(artistId=("artistId", "first"), labelId=("labelId", "first"), debutWeek=("week", "min"), debutYear=("year", "min"),
                                     peak=("currentPosition", "min")).join(c[c.currentPosition <= 10].groupby("recordId").agg(firstT10Year=("year", "min")))
    hist, wmax = [], r.week.max()
    for Y in (1961, 1962, 1963):
        hits = recs[recs.firstT10Year == Y - 1].sort_values("debutWeek").drop_duplicates("artistId")
        hits = hits.assign(g=group(hits.labelId, pd.Series(Y - 1, index=hits.index), tiers).values).set_index("artistId")
        deb = recs[(recs.debutYear == Y) & (recs.debutWeek <= wmax - 12) & (recs.debutWeek > 0)].join(hits.g.rename("hg"), on="artistId")
        la = a[(a.year == Y) & (~a.censored)].join(hits.g.rename("hg"), on="artistId")
        frac = min(1.0, (wmax - 12 - 52 * (Y - 1960)) / 52.0)
        for g in ORDER:
            hist.append(dict(tier=g, actYears=(hits.g == g).sum() * frac, launches=(la.hg == g).sum(), entered=la[la.hg == g].peak.notna().sum(),
                             debuts=(deb.hg == g).sum(), t10=((deb.hg == g) & (deb.peak <= 10)).sum()))
    return a[(~a.censored) & a.year.between(1961, 1963)], pd.DataFrame(hist)


def qmap(x, ref):
    return np.quantile(ref, x.rank(pct=True).clip(0, 1))


for name, fmt in [x.split("=", 1) for x in sys.argv[1:]]:
    parts = [load(fmt.format(s=s), s) for s in SEEDS]
    d = pd.concat([p[0] for p in parts], ignore_index=True)
    if SUBSET: d = d[d.newAct == (SUBSET == "new")]
    h = d[d.hot]
    print(f"\n######## {name}")
    rows = []
    for g in ORDER:
        x = h[h.g == g]; e = x[x.peak.notna()]
        rows.append(dict(tier=g, n=len(x), connect=100 * x.connected.mean(), h100=100 * x.peak.notna().mean(), t40=100 * (x.peak <= 40).mean(),
                         t10=100 * (x.peak <= 10).mean(), t40_per_entry=100 * (e.peak <= 40).mean(), quality=x.quality.mean(), awareness=x.initialAwareness.mean(),
                         stock_k=x.initialStock.median() / 1e3))
    print("1. launch frame: singles by acts with a Top 40 in the prior 26 weeks, 1961-63")
    print(pd.DataFrame(rows).set_index("tier").round(2).to_string())

    t = pd.concat([p[1] for p in parts]).groupby("tier").sum().reindex(ORDER)
    t.loc["non-Big5"] = t.loc[ORDER[1:]].sum()
    t = pd.DataFrame({"launches/actYr": t.launches / t.actYears, "entry %": 100 * t.entered / t.launches, "debuts/actYr": t.debuts / t.actYears,
                      "T10/debut %": 100 * t.t10 / t.debuts})
    print("2. history frame (hit act = first Top 10 in Y-1); history: debuts 3.28 vs 2.18, T10/debut 15.96 vs 15.93")
    print(t.round(2).to_string())
    print("   Big-5 / non-Big-5: debuts %.2f (history 1.51), T10 per debut %.2f (history 1.00)" % (
        t.loc["Big5", "debuts/actYr"] / t.loc["non-Big5", "debuts/actYr"], t.loc["Big5", "T10/debut %"] / t.loc["non-Big5", "T10/debut %"]))

    tr = d[d.nOpen.notna()].copy()
    c = tr[tr.connected]
    print("3. regional gates at launch, connected follow-ups in the trace (of 7 regions; share of potential audience)")
    print(c.groupby("g").agg(n=("nOpen", "size"), open=("nOpen", "mean"), fitExposed=("nExposed", "mean"), audOpen=("audOpen", "mean"),
                             audExposed=("audExposed", "mean")).reindex(ORDER).round(2).to_string())

    tr["h100"] = tr.peak.notna().astype(float); tr["t40"] = (tr.peak <= 40).astype(float)
    tr["aw_artist"] = 0.5 * tr.artistMomentum + 0.3 * tr.artistReputation + 0.115 * tr.effectiveRecognition
    tr["aw_label"] = tr.initialAwareness - 0.08 - tr.aw_artist

    def feats(f):
        o = pd.DataFrame(index=f.index)
        conn = (f.z >= f.kink).astype(float)
        o["conn"] = conn; o["hitSize"] = np.clip(f.z - f.sizeKink, 0, None); o["below"] = np.clip(f.kink - f.z, 0, None)
        o["lq"] = np.log(f.quality); o["law"] = np.log(f.initialAwareness.clip(.02, 1)); o["lstock"] = np.log(f.initialStock.clip(lower=1))
        o["audOpen"] = f.audOpen; o["connExposed"] = conn * f.audExposed
        return o
    tiers = ["T_" + g for g in ORDER[1:]]
    T = pd.DataFrame({k: (tr.g == k[2:]).astype(float) for k in tiers}, index=tr.index)
    yr = pd.get_dummies(tr.year.astype(str) + "_" + tr.seed.astype(str), drop_first=True).astype(float)
    F = feats(tr); b5 = tr[tr.g == "Big5"]
    print("4. accounting on the trace sample (points of Hot 100 entry / Top 40; field moved onto the Big-5 distribution, cumulative)")
    for y in ("h100", "t40"):
        X0 = sm.add_constant(pd.concat([T, F.drop(columns=["audOpen", "connExposed"]), yr], axis=1))
        m0 = sm.Logit(tr[y], X0).fit(disp=0, maxiter=300)
        X = sm.add_constant(pd.concat([T, F, yr], axis=1))
        m = sm.Logit(tr[y], X).fit(disp=0, maxiter=300)

        def pred(f, zero=False):
            Xf = X.loc[f.index].copy(); Xf[F.columns] = feats(f)
            if zero: Xf[tiers] = 0.0
            return 100 * m.predict(Xf).mean()
        steps = [("quality", lambda f: f.assign(quality=qmap(f.quality, b5.quality))),
                 ("awareness: act", lambda f: f.assign(initialAwareness=(0.08 + f.aw_label + qmap(f.aw_artist, b5.aw_artist)).clip(0, 1))),
                 ("awareness: label", lambda f: f.assign(initialAwareness=(f.initialAwareness - f.aw_label + qmap(f.aw_label, b5.aw_label)).clip(0, 1))),
                 ("launch stock", lambda f: f.assign(initialStock=qmap(f.initialStock, b5.initialStock))),
                 ("connect bar", lambda f: f.assign(sizeKink=np.where(f.oddsOnly, f.sizeKink, qmap(f.kink, b5.kink)), kink=qmap(f.kink, b5.kink))),
                 ("regions held at launch", lambda f: f.assign(audOpen=qmap(f.audOpen, b5.audOpen))),
                 ("regions not fit-exposed", lambda f: f.assign(audExposed=qmap(f.audExposed, b5.audExposed[b5.connected])))]
        cols = {}
        for g in ORDER[2:]:
            f = tr[tr.g == g]; prev = pred(f)
            col = {"actual": 100 * f[y].mean(), "Big-5": 100 * b5[y].mean()}
            for nm, fn in steps:
                f = fn(f); cur = pred(f); col[nm] = cur - prev; prev = cur
            cur = pred(f, zero=True); col["residual tier term"] = cur - prev; col["end"] = cur
            cols[g] = col
        print(f"   {y}: tier log-odds vs Big-5 without the gates", {k[2:]: f"{m0.params[k]:+.2f} (z {m0.params[k] / m0.bse[k]:+.1f})" for k in tiers},
              "| with", {k[2:]: f"{m.params[k]:+.2f} (z {m.params[k] / m.bse[k]:+.1f})" for k in tiers})
        print("   audOpen %+.2f (z %+.1f), connected x audExposed %+.2f (z %+.1f)" % (m.params["audOpen"], m.params["audOpen"] / m.bse["audOpen"],
                                                                                 m.params["connExposed"], m.params["connExposed"] / m.bse["connExposed"]))
        print(pd.DataFrame(cols).round(1).to_string())
