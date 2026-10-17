"""Decompose the Top 10 duration excess and the Big-5 premium (HistoricalLabelTierBalanceResearch.md section 17).

Reads logs only. Requires pandas. Inputs:
  - the three five-year review runs (top10-fiveyear-review-v1-default-<seed>-260-*),
  - the 1960-64 Billboard Top 10 reconstruction (SimLogs/historical-top10-<year>/top10_weekly_<year>.csv),
  - two existing paired probe sets: the five-seed 1960 push-spread probe (section 10) and the seed-1001
    no-chart-floors probe from the regional-sync work (sync-ctl / sync-floors).

"Uncensored" records are those whose Top 10 (or chart) run neither touches the first nor the last week of the
window, so their full Top 10 stay is observed. Airplay points are unsampledChartPoints - unitsThisWeek, already
multiplied by the airplay era weight (ChartSimulator.CalculateChartPoints). Positions are published positions.
"""
import argparse
import json
import re
from pathlib import Path

import numpy as np
import pandas as pd

BIG5 = {f"label_000{i}" for i in range(1, 6)}
SEEDS = (1001, 2002, 3003)
REVIEW = "SimLogs/top10-fiveyear-review-v1-default-{seed}-260"
BANDS = ([0, 1, 3, 6, 10], ["#1", "2-3", "4-6", "7-10"])
PUSH_CONTROLS = {1001: "historical-top10-review-v2-default-1001-52", 2002: "historical-top10-review-v2-default-2002-52",
                 3003: "historical-top10-review-v2-default-3003-52",
                 4004: "historical-top10-review-v2-ext-default-4004-52",
                 5005: "historical-top10-review-v2-ext-default-5005-52"}


def hesbacher(rank):
    return 4139 - 4357 * rank / (rank + 10)


def akey(text):
    return re.sub(r"[^a-z0-9]", "", text.lower())


def r2(value):
    return None if value is None or (isinstance(value, float) and np.isnan(value)) else round(float(value), 3)


def history_top10():
    frames = []
    for year in range(1960, 1965):
        w = pd.read_csv(f"SimLogs/historical-top10-{year}/top10_weekly_{year}.csv")
        frames.append(w[w.chartDate.isin(sorted(w.chartDate.unique())[-52:])])   # file opens on the prior year
    w = pd.concat(frames)
    w["week"] = w.chartDate.map({d: i + 1 for i, d in enumerate(sorted(w.chartDate.unique()))})
    w["rid"] = w.title.map(akey) + "|" + w.artist.map(akey)
    return w.rename(columns={"position": "pos"})[["week", "rid", "pos"]]


def load_sim(seed):
    r = pd.read_csv(REVIEW.format(seed=seed) + "-records.csv")
    r = r[r.currentPosition > 0].copy()
    r["air"] = (r.unsampledChartPoints - r.unitsThisWeek).clip(lower=0)
    r["big5"] = r.labelId.isin(BIG5)
    return r


def top10_frame(r):
    return r[r.currentPosition.le(10)].rename(columns={"recordId": "rid", "currentPosition": "pos"})


def record_shapes(t, last_week):
    """One row per record that reached the Top 10: stay, spells, entry-to-peak, peak-to-exit, censoring."""
    t = t.sort_values(["rid", "week"])
    rows = []
    for rid, g in t.groupby("rid", sort=False):
        wk, pos = g.week.values, g.pos.values
        best = pos.min()
        first_best = wk[pos == best][0]
        nxt_in = np.append(np.diff(wk) == 1, False)
        rows.append(dict(rid=rid, weeks=len(wk), spells=1 + int((np.diff(wk) > 1).sum()), best=int(best),
                         toPeak=first_best - wk[0], afterPeak=wk[-1] - first_best, entryPos=int(pos[0]),
                         entryStays=bool(nxt_in[0]), censored=bool(wk[0] == 1 or wk[-1] == last_week)))
    s = pd.DataFrame(rows).set_index("rid")
    s["band"] = pd.cut(s.best, BANDS[0], labels=BANDS[1])
    return s


def shape_summary(s):
    s = s[~s.censored]
    e810 = s[s.entryPos.between(8, 10)]
    out = dict(records=len(s), weeksPerRecord=r2(s.weeks.mean()), entryToPeak=r2(s.toPeak.mean()),
               peakToExit=r2(s.afterPeak.mean()), reentryPct=r2(100 * (s.spells > 1).mean()),
               entries8to10=len(e810), entries8to10GoneNextWeekPct=r2(100 * (~e810.entryStays).mean()),
               peakCountByRank={int(k): int(v) for k, v in s.best.value_counts().sort_index().items()})
    for band, g in s.groupby("band", observed=False):
        out[f"band {band}"] = dict(records=len(g), sharePct=r2(100 * len(g) / len(s)), weeks=r2(g.weeks.mean()),
                                   peakToExit=r2(g.afterPeak.mean()))
    return out


def no1_summary(t):
    one = t[t.pos == 1].sort_values(["rid", "week"])
    spells = one.groupby("rid").week.apply(lambda w: 1 + int((np.diff(w.values) > 1).sum()))
    return dict(no1Records=len(spells), tenure=r2(len(one) / len(spells)), spell=r2(len(one) / spells.sum()),
                returnsPct=r2(100 * (spells > 1).mean()))


def points_curve(r):
    """Median ratio to the #10 record in the same week, by published rank, plus log spacing between ranks."""
    top = r[r.currentPosition.le(20)]
    out = {}
    for col in ("chartPoints", "unitsThisWeek", "air"):
        p = top.pivot_table(index="week", columns="currentPosition", values=col)
        out[col] = p.div(p[10], axis=0).median()
    curve = pd.DataFrame(out)
    curve["logSpacingToRankAbove"] = np.log(curve.chartPoints.shift(1) / curve.chartPoints)
    return curve


ENTRY_TERMS = ["unitsThisWeek", "air", "radioHeat", "momentum", "wordOfMouth", "saturation", "awareness", "quality",
               "weeksSinceRelease", "currentPosition"]


def entry_terms(sims):
    """Chart-points terms in the week each record first enters the Top 10, against its eventual Top 10 stay."""
    frames = []
    for r in sims.values():
        span = r.groupby("recordId").week.agg(["min", "max"])
        t = r[r.currentPosition.le(10)]
        first = t.sort_values("week").drop_duplicates("recordId").set_index("recordId")
        first["stay"] = t.groupby("recordId").size()
        ok = (span.loc[first.index, "min"] > 1) & (span.loc[first.index, "max"] < 260)
        frames.append(first[ok.values])
    d = pd.concat(frames)
    d["stayGroup"] = pd.cut(d.stay, [0, 2, 4, 6, 30], labels=["1-2", "3-4", "5-6", "7+"])
    med = d.groupby("stayGroup", observed=False)[ENTRY_TERMS].median()
    return {"medianByEventualStay": {str(k): {c: r2(v) for c, v in row.items()} for k, row in med.iterrows()},
            "recordsByEventualStay": {str(k): int(v) for k, v in d.stayGroup.value_counts().sort_index().items()},
            "rankCorrelationWithStay": {c: r2(v) for c, v in d[ENTRY_TERMS + ["stay"]].rank().corr()["stay"].items()
                                        if c != "stay"}}


def boundary_step(r):
    """Next-week growth by this week's position, for records still at their own sales peak (climbing)."""
    r = r.sort_values(["recordId", "week"])
    nxt = r.groupby("recordId").shift(-1)
    ok = nxt.week == r.week + 1
    d = r.assign(units=(nxt.unitsThisWeek / r.unitsThisWeek).where(ok), air=(nxt.air / r.air).where(ok),
                 pts=(nxt.unsampledChartPoints / r.unsampledChartPoints).where(ok),
                 dHeat=(nxt.radioHeat - r.radioHeat).where(ok))
    d = d[(d.weeksSincePeakUnits == 0) & ok & d.currentPosition.between(8, 13)]
    return d.groupby("currentPosition")[["units", "air", "pts", "dHeat"]].median()


def inertia(r):
    pts = np.where(r.unitsThisWeek == 0, r.chartPoints * 0.1, r.chartPoints)   # ChartManager step 4
    raw = r.assign(pts=pts).groupby("week").pts.rank(ascending=False, method="first")
    top = r.currentPosition.le(10)
    best, rawbest = r.groupby("recordId").currentPosition.min(), raw.groupby(r.recordId).min()
    denied = (rawbest <= 10) & (best > 10)
    return dict(top10SlotsHeldByInertiaPct=r2(100 * (raw[top] > 10).mean()),
                heldRawRankMedian=r2(raw[top & (raw > 10)].median()),
                big5HeldPct=r2(100 * (raw[top & r.big5] > 10).mean()),
                othersHeldPct=r2(100 * (raw[top & ~r.big5] > 10).mean()),
                recordsDeniedTop10=int(denied.sum()),
                deniedPublishedPeak={int(k): int(v) for k, v in best[denied].value_counts().sort_index().items()})


def big5_premium(r, shapes):
    q = r.groupby("recordId").agg(q=("quality", "first"), big5=("big5", "first"), stock=("initialLaunchStock", "first"),
                                  aw=("initialLaunchAwareness", "first"), career=("launchCareerState", "first"),
                                  genre=("genre", "first"), first=("week", "min"), last=("week", "max"))
    s = shapes[~shapes.censored].join(q)
    s = s[(s["first"] > 1) & (s["last"] < 260)]
    return s


def premium_tables(pooled):
    qmed = pooled.q.median()
    pooled = pooled.assign(upper=pooled.q >= qmed)
    out = {"qualityMedian": r2(qmed), "bySeed": {}}
    for seed, g in pooled.groupby("seed"):
        out["bySeed"][int(seed)] = {f"big5={b},upper={u}": dict(records=len(x), weeks=r2(x.weeks.mean()))
                                    for (b, u), x in g.groupby(["big5", "upper"])}
    mix = {}
    for (b, u), x in pooled.groupby(["big5", "upper"]):
        mix[f"big5={b},upper={u}"] = {band: dict(sharePct=r2(100 * len(y) / len(x)), weeks=r2(y.weeks.mean()))
                                      for band, y in x.groupby("band", observed=False)}
    out["peakBandMixAndWeeks"] = mix
    a, b = pooled[pooled.big5 & pooled.upper], pooled[~pooled.big5 & pooled.upper]
    pa, pb = a.band.value_counts(normalize=True), b.band.value_counts(normalize=True)
    ca, cb = a.groupby("band", observed=False).weeks.mean(), b.groupby("band", observed=False).weeks.mean()
    out["split"] = dict(big5Upper=r2(a.weeks.mean()), othersUpper=r2(b.weeks.mean()),
                        big5WeeksAtOthersMix=r2((ca * pb).sum()), othersWeeksAtBig5Mix=r2((cb * pa).sum()))
    # OLS with launch controls, then also holding the peak band fixed
    d = pooled.copy()
    x = pd.get_dummies(d[["career", "genre", "seed"]].astype(str), drop_first=True).astype(float)
    x["big5"], x["q"], x["lstock"], x["aw"], x["const"] = d.big5.astype(float), d.q, np.log1p(d.stock), d.aw, 1.0
    coef = lambda m: float(np.linalg.lstsq(m.values, d.weeks.values, rcond=None)[0][list(m.columns).index("big5")])
    out["olsBig5Weeks"] = r2(coef(x))
    for band in BANDS[1][1:]:
        x[f"band{band}"] = (d.band == band).astype(float)
    out["olsBig5WeeksWithinPeakBand"] = r2(coef(x))
    return out, qmed


def event_profiles(sims, keys_upper):
    """Aligned at each record's own peak-units week, upper-half quality, records peaking at #1-3."""
    frames = []
    for seed, r in sims.items():
        cut = r[r.currentPosition == 10].set_index("week").chartPoints
        d = r[(r.seed_key.isin(keys_upper))].copy()
        peak = d.loc[d.groupby("recordId").unitsThisWeek.idxmax()].set_index("recordId")
        d["k"] = d.week - d.recordId.map(peak.week)
        d["unitsOverTop10Cutoff"] = d.unitsThisWeek / d.week.map(cut)
        d["airOverTop10Cutoff"] = d.air / d.week.map(cut)
        frames.append(d)
    d = pd.concat(frames)
    d = d[d.k.between(-2, 6)]
    out = {}
    for b, g in d.groupby("big5"):
        prof = g.groupby("k").agg(inTop10Pct=("currentPosition", lambda p: 100 * p.le(10).mean()),
                                  units=("unitsOverTop10Cutoff", "median"), air=("airOverTop10Cutoff", "median"),
                                  heat=("radioHeat", "median"), panel=("radioPanelShare", "median"))
        out["big5" if b else "others"] = {int(k): {c: r2(v) for c, v in row.items()} for k, row in prof.iterrows()}
    return out


def push_spread_probe():
    rows = []
    for seed, control in PUSH_CONTROLS.items():
        for arm, prefix in (("control", control), ("push0.5", f"push-spread-050-default-{seed}-52")):
            r = pd.read_csv(f"SimLogs/{prefix}-records.csv", usecols=["week", "recordId", "labelId", "currentPosition",
                                                                       "quality"])
            per = r[r.currentPosition.between(1, 10)].groupby("recordId").agg(
                w=("week", "size"), q=("quality", "first"), lab=("labelId", "first"))
            upper, big5 = per.q >= per.q.median(), per.lab.isin(BIG5)
            rows.append(dict(seed=seed, arm=arm, top10Records=len(per), weeksPerRecord=per.w.mean(),
                             big5Upper=per[big5 & upper].w.mean(), othersUpper=per[~big5 & upper].w.mean()))
    d = pd.DataFrame(rows)
    mean = d.groupby("arm").mean(numeric_only=True).drop(columns="seed")
    return {"mean": {arm: {k: r2(v) for k, v in row.items()} for arm, row in mean.iterrows()},
            "perSeed": [{k: (r2(v) if isinstance(v, float) else v) for k, v in row.items()} for row in d.to_dict("records")]}


def floors_probe():
    out = {}
    for arm in ("sync-ctl-1001-52", "sync-floors-1001-52"):
        r = pd.read_csv(f"SimLogs/{arm}-records.csv", usecols=["week", "recordId", "currentPosition", "unitsThisWeek"])
        r = r[r.currentPosition > 0].sort_values(["recordId", "week"])
        t = top10_frame(r)
        s = record_shapes(t[["week", "rid", "pos"]], 52)
        nxt = r.groupby("recordId").shift(-1)
        growth = (nxt.unitsThisWeek / r.unitsThisWeek).where(nxt.week == r.week + 1).groupby(r.currentPosition).median()
        out[arm] = dict(top10Records=len(s), weeksPerRecord=r2(s.weeks.mean()),
                        peak7to10=int(s.best.between(7, 10).sum()),
                        entries8to10GoneNextWeekPct=r2(100 * (~s[s.entryPos.between(8, 10)].entryStays).mean()),
                        unitsGrowthStep10over11=r2(growth.loc[10] / growth.loc[11]))
    stages = pd.read_csv("SimLogs/sync-ctl-1001-52-single-demand-stages.csv",
                         usecols=["week", "recordId", "baselineAwareness", "awareBuyers", "potentialAudience",
                                  "chartSignal"])
    pos = pd.read_csv("SimLogs/sync-ctl-1001-52-records.csv", usecols=["week", "recordId", "currentPosition"])
    pos = pos[pos.currentPosition.between(9, 12)].assign(week=lambda p: p.week + 1)   # sales read last week's rank
    st = stages.merge(pos, on=["week", "recordId"])
    st["awareFraction"] = st.awareBuyers / st.potentialAudience
    out["controlDemandStagesByRankUsed"] = {
        int(k): {c: r2(v) for c, v in row.items()}
        for k, row in st.groupby("currentPosition")[["baselineAwareness", "chartSignal", "awareFraction"]].median().iterrows()}
    return out


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()

    hist = history_top10()
    hist_shapes = record_shapes(hist, 260)
    result = {"history": {"shape": shape_summary(hist_shapes), "no1": no1_summary(hist),
                          "hesbacherRatioToRank10": {k: r2(hesbacher(k) / hesbacher(10)) for k in range(1, 21)}},
              "runs": {}}
    sims, premium_rows = {}, []
    for seed in SEEDS:
        r = load_sim(seed)
        r["seed_key"] = f"{seed}:" + r.recordId
        sims[seed] = r
        t = top10_frame(r)
        shapes = record_shapes(t[["week", "rid", "pos"]], 260)
        lc = pd.read_csv(REVIEW.format(seed=seed) + "-lifecycles.csv")
        lc = lc[(lc.leftCensoredAtRunStart.astype(str).str.lower() != "true") & (lc.weeksOnChart > 0)]
        entries = r.sort_values("week").drop_duplicates("recordId")
        t40 = r[r.currentPosition.le(40)].sort_values("week").drop_duplicates("recordId")
        within = t.groupby("year").apply(lambda g: len(g) / g.rid.nunique(), include_groups=False)
        curve = points_curve(r)
        all_best = r.groupby("recordId").currentPosition.min()
        span = r.groupby("recordId").week.agg(["min", "max"])
        unc_best = all_best[(span["min"] > 1) & (span["max"] < 260)]
        genre = t.groupby("genre").size()
        result["runs"][seed] = {
            "shape": shape_summary(shapes),
            "withinYearTop10WeeksPerRecord": {int(y): r2(v) for y, v in within.items()},
            "chartPeakCountByRank1to20": {int(k): int(v) for k, v in unc_best.value_counts().sort_index().loc[1:20].items()},
            "pointsCurve": {int(k): {c: r2(v) for c, v in row.items()} for k, row in curve.iterrows()},
            "rank1OverRank10ByYear": {int(y): r2(v) for y, v in r[r.currentPosition.isin([1, 10])].pivot_table(
                index=["year", "week"], columns="currentPosition", values="chartPoints").pipe(
                lambda p: (p[1] / p[10]).groupby(level=0).median()).items()},
            "boundaryStep": {int(k): {c: r2(v) for c, v in row.items()} for k, row in boundary_step(r).iterrows()},
            "inertia": inertia(r),
            "guards": dict(no1Summary=no1_summary(t), closedChartLife=r2(lc.weeksOnChart.mean()),
                           hot100EntriesPerWeek=r2((entries.week > 1).sum() / 259),
                           top40EntriesPerWeek=r2((t40.week > 1).sum() / 259),
                           top10SlotSharePctByGenre={g: r2(100 * v / genre.sum()) for g, v in genre.nlargest(10).items()}),
        }
        premium_rows.append(big5_premium(r, shapes).assign(seed=seed))
    pooled = pd.concat(premium_rows)
    premium, qmed = premium_tables(pooled)
    keys_upper = {f"{s}:{rid}" for s, rid, q, best in zip(pooled.seed, pooled.index, pooled.q, pooled.best)
                  if q >= qmed and best <= 3}
    premium["eventProfilesPeak1to3UpperHalf"] = event_profiles(sims, keys_upper)
    result["big5Premium"] = premium
    result["entryWeekTerms"] = entry_terms(sims)

    # history's band mix applied to the simulation's per-band stays
    hist_mix = hist_shapes[~hist_shapes.censored].band.value_counts(normalize=True)
    cf = {}
    for seed, run in result["runs"].items():
        sim_weeks = pd.Series({b: run["shape"][f"band {b}"]["weeks"] for b in BANDS[1]})
        cf[seed] = dict(simWeeks=run["shape"]["weeksPerRecord"], atHistoryPeakMix=r2((hist_mix * sim_weeks).sum()))
    result["peakMixCounterfactual"] = dict(historyWeeks=result["history"]["shape"]["weeksPerRecord"], bySeed=cf)
    result["pushSpreadProbe1960"] = push_spread_probe()
    result["noChartFloorsProbe1960"] = floors_probe()

    Path(args.output).parent.mkdir(parents=True, exist_ok=True)
    Path(args.output).write_text(json.dumps(result, indent=2, default=str) + "\n", encoding="utf-8")

    h = result["history"]["shape"]
    print("Top 10 stays, uncensored records over 1960-64 (history vs seeds):")
    for name, s in [("history", h)] + [(str(k), v["shape"]) for k, v in result["runs"].items()]:
        print(f"  {name:8s} records={s['records']} weeks={s['weeksPerRecord']} entry->peak={s['entryToPeak']} "
              f"peak->exit={s['peakToExit']} reentry%={s['reentryPct']} gone-after-8..10-entry%={s['entries8to10GoneNextWeekPct']} | "
              + " ".join(f"{b}:{s[f'band {b}']['sharePct']}%/{s[f'band {b}']['weeks']}w" for b in BANDS[1]))
    print("  peak counts by rank 1-10, history:", list(h["peakCountByRank"].values()))
    for seed, run in result["runs"].items():
        print(f"  seed {seed} peaks 1-14:", list(run["chartPeakCountByRank1to20"].values())[:14],
              "| #1/#10 points by year:", run["rank1OverRank10ByYear"], "| inertia:", run["inertia"]["top10SlotsHeldByInertiaPct"],
              "% held,", run["inertia"]["recordsDeniedTop10"], "denied")
    print("  counterfactual weeks at history's peak mix:", result["peakMixCounterfactual"])
    print("Entry-week rank correlation with eventual Top 10 stay:", result["entryWeekTerms"]["rankCorrelationWithStay"])
    print("Big-5 premium:", premium["split"], "OLS", premium["olsBig5Weeks"], "within band", premium["olsBig5WeeksWithinPeakBand"])
    print("Push-spread probe mean:", result["pushSpreadProbe1960"]["mean"])
    print("No-floors probe:", {k: v for k, v in result["noChartFloorsProbe1960"].items() if k.startswith("sync")})
    for seed, run in result["runs"].items():
        g = run["guards"]
        print(f"  guards {seed}: {g['no1Summary']} life={g['closedChartLife']} hot100/wk={g['hot100EntriesPerWeek']} "
              f"top40/wk={g['top40EntriesPerWeek']}")
    print("  history #1:", result["history"]["no1"])


if __name__ == "__main__":
    main()
