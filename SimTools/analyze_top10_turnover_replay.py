"""Static re-rank of the logged chart: how much Top 10 turnover the inertia base and the singles ration carry.

usage: analyze_top10_turnover_replay.py [name=fmt ...]   ({s} = seed; default the fit208 control and he095, seeds 2002 + 1001)

Each week is re-ranked from the logged record rows (records.csv: the Hot 100, bubbling under and near misses) with
ChartManager step 4-5 and ChartSimulator.GetInertiaPositionCap re-implemented:
  points = (units + air) x surveySample, x0.1 when units = 0; air = unsampledChartPoints - units (era-weighted already);
  inertia cap with base B, quality override 0.15, momentum bonus 0.3, sales gate 8,000, weeksNegative from momentum.
Previous positions come from the replay itself (internal chart 115 deep), so the identity arm (B = 0.4, g = 1.35) must
reproduce the published Top 10 closely before the other arms mean anything.

Ration counterfactual g' : each week units' = S x u^(g'/g) / sum(u^(g'/g)) over the logged rows, S = their logged
units. That undoes clearing = lambda x intent^g for uncapped titles nationally (regional lambdas and intent caps are
ignored) and keeps the logged purse. Static: no feedback through radio position bonus, momentum or awareness.

Mechanism tables (logged chart): median points ratio of each rank to #10 (Hesbacher 1.91 at #1), weekly log change of
points / units / air for Top 10 records, and the share of Top 10 slot-weeks held by the inertia cap.
"""
import math
import sys
from pathlib import Path

import numpy as np
import pandas as pd

sys.path.insert(0, str(Path(__file__).resolve().parent))
from analyze_top10_turnover import B5, ROOT, SEEDS, year_of

ARMS = {"base": "fit208-v1-{s}-base", "lw0up1": "fit208-v1-{s}-lw0up1", "ao2": "fit208-v3-{s}-lw0up1_ao2",
        "he095": "fit208-v5-{s}-lw0up1_ao2_lh1_he095"}
G_BUNDLE = 1.35
DEPTH = 115
COLS = ["week", "recordId", "artistId", "labelId", "currentPosition", "unitsThisWeek", "unsampledChartPoints", "chartPoints",
        "surveySample", "momentum", "quality", "previousPosition"]
# (label, inertia base, ration g, airplay power k of 5, smooth air, smooth units)
REPLAYS = [("identity B.4 g1.35", 0.4, 1.35, 5, False, False), ("B.8 (authored)", 0.8, 1.35, 5, False, False),
           ("B0", 0.0, 1.35, 5, False, False), ("g1.0", 0.4, 1.0, 5, False, False),
           ("air k3", 0.4, 1.35, 3, False, False), ("air k1", 0.4, 1.35, 1, False, False),
           ("air 3wk", 0.4, 1.35, 5, True, False), ("units 3wk", 0.4, 1.35, 5, False, True),
           ("both 3wk", 0.4, 1.35, 5, True, True)]
# Purchase-exposure loop closed on the replay's own positions (units x (w_cf / w_logged)^g; airplay, awareness floors
# and momentum stay open-loop): softer exponent, or exposure that reads the slower / mean of the last two positions.
JREPLAYS = [("J identity", 0.62, None), ("J E.45", 0.45, None), ("J E.30", 0.30, None),
            ("J slower-of-2", 0.62, "slower2"), ("J mean-of-2", 0.62, "mean2")]


def exposure_weight(pos, e):
    x = np.clip(np.where(np.asarray(pos) <= 0, 100, pos), 10, 100)
    ranks = np.clip(np.arange(1, 101), 10, 100)
    mean = np.power(hesbacher(ranks) / hesbacher(100), e).mean()
    return np.power(hesbacher(x) / hesbacher(100), e) / mean
pd.set_option("display.width", 250)


def hesbacher(rank):
    return 4139 - 4357 * rank / (rank + 10)


def load(run):
    r = pd.read_csv(ROOT / f"{run}-records.csv", usecols=COLS)
    r = r[r.week <= 208].sort_values(["recordId", "week"]).copy()
    r["air"] = (r.unsampledChartPoints - r.unitsThisWeek).clip(lower=0)
    # weeksNegative: consecutive weeks with momentum < -0.02, reset by momentum > 0.02, unchanged in between
    wn = np.zeros(len(r), dtype=int)
    rid, mom = r.recordId.values, r.momentum.values
    for i in range(len(r)):
        prev = wn[i - 1] if i and rid[i - 1] == rid[i] else 0
        wn[i] = prev + 1 if mom[i] < -0.02 else (0 if mom[i] > 0.02 else prev)
    r["weeksNegative"] = wn
    r["big5"] = r.labelId.isin(B5)
    return r


def transform(r, k, smooth_air, smooth_units):
    """Static counterfactual columns: airplay re-powered to k/5 (weekly air sum kept), or a centred 3-week mean."""
    r = r.copy()
    if k != 5:
        x = np.power(r.air.clip(lower=0), k / 5.0)
        r["air"] = x * (r.groupby("week").air.transform("sum") / x.groupby(r.week).transform("sum").replace(0, 1))
    roll = lambda col: r.groupby("recordId")[col].transform(lambda v: v.rolling(3, center=True, min_periods=1).mean())
    if smooth_air:
        r["air"] = roll("air")
    if smooth_units:
        r["unitsThisWeek"] = np.where(r.unitsThisWeek > 0, roll("unitsThisWeek"), 0)
    return r


def replay(r, base, g, jexp=None, jmode=None):
    out = []
    prev = {}
    prev2 = {}
    for week, w in r.groupby("week", sort=True):
        u = w.unitsThisWeek.values.astype(float)
        if jexp is not None:
            pub = lambda d: np.array([d.get(x, 0) if d.get(x, 0) <= 100 else 0 for x in w.recordId.values])
            p1 = pub(prev)
            if jmode == "slower2":
                p2 = pub(prev2)
                pcf = np.where((p1 > 0) & (p2 > 0), np.maximum(p1, p2), np.where(p2 > 0, p2, p1))
                pcf = np.where(p2 <= 0, 0, pcf)   # a record new to the chart last week reads the off-chart weight
            elif jmode == "mean2":
                p2 = pub(prev2)
                pcf = np.where(p1 > 0, (np.where(p1 > 0, p1, 100) + np.where(p2 > 0, p2, 100)) / 2, 0)
            else:
                pcf = p1
            factor = np.power(exposure_weight(pcf, jexp) / exposure_weight(w.previousPosition.values, 0.62), G_BUNDLE)
            u = u * factor
        if g != G_BUNDLE:
            x = np.power(np.maximum(u, 0), g / G_BUNDLE)
            u = np.where(u > 0, u.sum() * x / x.sum(), 0)
        pts = (u + w.air.values) * w.surveySample.values
        pts = np.where(u <= 0, pts * 0.1, pts)
        keep = pts > 0
        ids = w.recordId.values[keep]
        order = np.lexsort((-u[keep], -pts[keep]))
        raw = np.empty(len(order), dtype=int)
        raw[order] = np.arange(1, len(order) + 1)
        uk, mk, qk, nk = u[keep], w.momentum.values[keep], w.quality.values[keep], w.weeksNegative.values[keep]
        eff = raw.copy()
        for i, rec in enumerate(ids):
            p = prev.get(rec, 0)
            if p <= 0 or raw[i] <= p or uk[i] <= 0 or nk[i] >= 3 or mk[i] <= -0.20:
                continue
            gate = min(max(uk[i] / 8000.0, 0.0), 1.0)
            inertia = max(0.0, base - (1 - qk[i]) * 0.15) * gate
            if p <= 40 and mk[i] > 0:
                inertia = min(base, inertia + mk[i] * 0.3 * gate)
            eff[i] = p + max(1, math.ceil((raw[i] - p) * (1 - inertia)))
        rank = np.lexsort((raw, eff))[:DEPTH]
        prev2 = prev
        prev = {ids[j]: n + 1 for n, j in enumerate(rank)}
        out.append(pd.DataFrame({"week": week, "rid": ids[rank], "pos": np.arange(1, len(rank) + 1), "raw": raw[rank]}))
    return pd.concat(out)


def top10_stats(t, big5_ids, act_of=None):
    t = t[t.pos <= 10]
    acts = {}
    if act_of is not None:
        f = t.sort_values("week").drop_duplicates("rid")
        f = f.assign(act=f.rid.map(act_of)).drop_duplicates("act")
        f = f[(f.week > 0) & (year_of(f.week.values) >= 1961) & (year_of(f.week.values) <= 1963)]
        acts = dict(newActsB5=round(f.rid.isin(big5_ids).sum() / 3, 1), newActsField=round((~f.rid.isin(big5_ids)).sum() / 3, 1))
    span = t.groupby("rid").week.agg(["min", "max", "size"])
    unc = span[(span["min"] > 0) & (span["max"] < 208)]
    first = span["min"]
    yr = pd.Series(year_of(first.values), index=first.index)
    slots = t[year_of(t.week.values) >= 1961]
    return dict(t10PerYr=round(len(span[yr <= 1963]) / 4, 1), wk=round(unc["size"].mean(), 2),
                oneWkPct=round(100 * (unc["size"] == 1).mean(), 1),
                b5T10PerYr=round(span[(yr <= 1963) & span.index.isin(big5_ids)].shape[0] / 4, 1),
                b5Share6163=round(100 * slots.rid.isin(big5_ids).mean(), 1),
                heldByInertiaPct=round(100 * (t.raw > 10).mean(), 1), **acts)


def mechanism(r):
    top = r[r.currentPosition.between(1, 20)]
    p = top.pivot_table(index="week", columns="currentPosition", values="chartPoints")
    ratio = p.div(p[10], axis=0).median()
    hes = pd.Series({k: hesbacher(k) / hesbacher(10) for k in ratio.index})
    spacing = pd.DataFrame({"sim": ratio, "hesbacher": hes}).loc[[1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 15, 20]].round(2)
    nxt = r.groupby("recordId").shift(-1)
    ok = (nxt.week == r.week + 1) & r.currentPosition.between(1, 10)
    d = pd.DataFrame({"dPts": np.log(nxt.unsampledChartPoints / r.unsampledChartPoints),
                      "dUnits": np.log(nxt.unitsThisWeek.clip(lower=1) / r.unitsThisWeek.clip(lower=1)),
                      "dAir": np.log(nxt.air.clip(lower=1) / r.air.clip(lower=1)),
                      "airShare": r.air / r.unsampledChartPoints, "rising": nxt.currentPosition.lt(r.currentPosition)
                      & nxt.currentPosition.gt(0)})[ok]
    vel = d.groupby("rising")[["dPts", "dUnits", "dAir", "airShare"]].median().round(3)
    vel.index = ["falling/held", "rising"]
    return spacing, vel


def main():
    arms = dict(a.split("=", 1) for a in sys.argv[1:] if "=" in a) or ARMS
    for name, fmt in arms.items():
        rows = []
        for seed in SEEDS:
            r = load(fmt.format(s=seed))
            published = r[r.currentPosition.between(1, 100)].rename(columns={"recordId": "rid", "currentPosition": "pos"})
            published = published.assign(raw=0)[["week", "rid", "pos", "raw"]]
            b5 = set(r.recordId[r.big5])
            act_of = r.drop_duplicates("recordId").set_index("recordId").artistId
            rows.append(dict(seed=seed, arm="published", **top10_stats(published, b5, act_of)))
            ident = None
            for label, e, mode in JREPLAYS:
                rows.append(dict(seed=seed, arm=label, **top10_stats(replay(r, 0.4, G_BUNDLE, e, mode), b5, act_of)))
            for label, base, g, k, sa, su in REPLAYS:
                t = replay(transform(r, k, sa, su) if (k != 5 or sa or su) else r, base, g)
                if ident is None:
                    ident = t
                    a = published[published.pos <= 10].set_index(["week", "pos"]).rid
                    b = t[t.pos <= 10].set_index(["week", "pos"]).rid
                    agree = (a == b.reindex(a.index)).mean()
                    sa, sb = set(zip(*published[published.pos <= 10][["week", "rid"]].values.T)), set(zip(*t[t.pos <= 10][["week", "rid"]].values.T))
                    print(f"{name} {seed}: identity replay Top 10 same rank {100 * agree:.1f}%, same membership {100 * len(sa & sb) / len(sa):.1f}%")
                rows.append(dict(seed=seed, arm=label, **top10_stats(t, b5, act_of)))
            if seed == SEEDS[0]:
                spacing, vel = mechanism(r)
                print(f"-- {name} {seed}: points ratio to #10 (median week)\n{spacing.T.to_string()}")
                print(f"-- {name} {seed}: weekly log change, Top 10 records\n{vel.to_string()}")
        t = pd.DataFrame(rows)
        print(f"\n== {name}: replays (per seed; mean of seeds last) ==")
        print(t.to_string(index=False))
        print(t.groupby("arm", sort=False).mean(numeric_only=True).drop(columns="seed").round(2).to_string())
        print()


if __name__ == "__main__":
    main()
