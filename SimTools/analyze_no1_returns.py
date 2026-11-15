"""#1 returns on 208-week runs: spells and returns, every #1 handover split into survey-noise and real, and a static
replay of the chart top under alternative survey-noise models.

usage: analyze_no1_returns.py NAME=fmt[:seed,seed,...] ...   (fmt holds {s}; seeds default 2002,1001)
       add --no-replay to skip section 3 (it takes a few minutes)

1. per arm: #1 records a year, tenure, spell length, returns (records with two or more #1 spells), one-week #1s.
   History is the recovered Hot 100 cut to the same 209 chart weeks.
2. every #1 handover A (last week's #1) -> B: was B ahead on unsampledChartPoints (the true score) in its first week
   at #1? If not, the handover is survey noise. Then: how often A retakes #1 the next week, and the true slopes.
3. static replay: logged true points of records at #1-30 re-ranked under redrawn survey noise (sigma from units, as
   ChartSimulator.GetSurveySigma). The logged published points reproduce the published #1 exactly. "AR rho" keeps the
   marginal sigma and makes each record's panel error an AR(1) across weeks (a fixed outlet panel). Valid for the #1
   slot because nothing on the bundle pays #1 over #2 (exposure plateau to #20, radio position bonus 0); the Top 10
   columns ignore the #10 awareness floor and inertia, so read them as direction only.
"""
import sys
from pathlib import Path
import numpy as np
import pandas as pd

ROOT = Path(__file__).resolve().parent.parent / "SimLogs"
LAST = 208
COLS = ["week", "recordId", "currentPosition", "unitsThisWeek", "chartPoints", "unsampledChartPoints",
        "weeksSincePeakUnits"]


def history():
    h = pd.read_csv(ROOT / "historical-hot100-1960-64" / "hot100_weekly.csv")
    dates = sorted(h.chartDate.unique())
    h["week"] = h.chartDate.map({d: n for n, d in enumerate(dates)})
    h = h[(h.week <= LAST) & (h.position <= 10)]
    h["rid"] = h.title.str.lower() + "|" + h.artist.str.lower()
    return h.rename(columns={"position": "pos"})[["week", "rid", "pos"]]


def sim(fmt, seeds, depth=40):
    out = []
    for n, s in enumerate(seeds):
        r = pd.read_csv(ROOT / f"{fmt.format(s=s)}-records.csv", usecols=COLS)
        r = r[r.currentPosition.between(1, depth) & (r.week <= LAST)].copy()
        r["rid"] = f"{s}:" + r.recordId
        r["week"] = r.week + 1000 * n  # keeps seeds apart in time
        out.append(r.rename(columns={"currentPosition": "pos"}))
    return pd.concat(out)


def spells(c):
    top = c[c.pos == 1].sort_values("week")[["week", "rid"]].reset_index(drop=True)
    top["new"] = (top.rid != top.rid.shift()) | (top.week != top.week.shift() + 1)
    top["spell"] = top.new.cumsum()
    sp = top.groupby("spell").agg(rid=("rid", "first"), start=("week", "min"), end=("week", "max"))
    sp["len"] = sp.end - sp.start + 1
    sp["k"] = sp.groupby("rid").cumcount()
    return top, sp


def summary(name, c, years):
    top, sp = spells(c)
    per = sp.groupby("rid").agg(n=("len", "size"), wk=("len", "sum"))
    gaps = (sp.start - sp.groupby("rid").end.shift() - 1)[sp.k > 0]
    return dict(arm=name, no1_yr=len(per) / years, tenure=per.wk.mean(), spell=sp.len.mean(),
                one_week_pct=100 * (per.wk == 1).mean(), returns_pct=100 * (per.n > 1).mean(),
                return_spells_yr=(sp.k > 0).sum() / years, one_week_gap_pct=100 * (gaps == 1).mean())


def handovers(name, c, years):
    top, _ = spells(c)
    inc = top.set_index("week").rid
    idx = c.set_index(["week", "rid"])
    ev = []
    for w1 in inc.index:
        if w1 - 1 not in inc.index or w1 + 1 not in inc.index or inc[w1 - 1] == inc[w1]:
            continue
        a, b = inc[w1 - 1], inc[w1]
        try:
            a0, a1, a2 = (idx.loc[(w, a)] for w in (w1 - 1, w1, w1 + 1))
            b0, b1, b2 = (idx.loc[(w, b)] for w in (w1 - 1, w1, w1 + 1))
        except KeyError:
            continue
        p = lambda r: np.log(r.unsampledChartPoints)
        ev.append(dict(real=p(b1) > p(a1), retake=inc[w1 + 1] == a, gap1=p(a1) - p(b1), dA_in=p(a1) - p(a0),
                       dB_in=p(b1) - p(b0), dA_out=p(a2) - p(a1), dB_out=p(b2) - p(b1), A_wsp=a1.weeksSincePeakUnits))
    e = pd.DataFrame(ev)
    rows = []
    for real, g in e.groupby("real"):
        rows.append(dict(arm=name, handover="real" if real else "noise", per_yr=len(g) / years,
                         retake_pct=100 * g.retake.mean(), retakes_yr=g.retake.sum() / years,
                         gap1=g.gap1.median(), dA_in=g.dA_in.median(), dB_in=g.dB_in.median(),
                         dA_out=g.dA_out.median(), dB_out=g.dB_out.median(), A_wks_past_peak=g.A_wsp.median()))
    return rows


# ---------- 3. static replay
def sigma(units, k=1.0):
    outlets = 110 * np.clip(np.nan_to_num(units) / 30000, 0.06, 1)
    return np.minimum(0.30, k / np.sqrt(np.maximum(1, outlets)))


def matrices(run):
    r = pd.read_csv(ROOT / f"{run}-records.csv", usecols=COLS)
    r = r[r.currentPosition.between(1, 30) & (r.week <= LAST)]
    t = r.pivot_table(index="week", columns="recordId", values="unsampledChartPoints", aggfunc="first")
    u = r.pivot_table(index="week", columns="recordId", values="unitsThisWeek", aggfunc="first").reindex_like(t)
    p = r.pivot_table(index="week", columns="recordId", values="chartPoints", aggfunc="first").reindex_like(t)
    return np.log(t.values), u.values, np.log(p.values)


def score(pub):
    """(weeks, records) published log points -> Top 10 records, one-week Top 10 %, #1 records, one-week #1 %,
    returns %."""
    order = np.argsort(-pub, axis=1)
    in10 = np.zeros_like(pub, bool)
    np.put_along_axis(in10, order[:, :10], True, axis=1)
    wk10 = (in10 & np.isfinite(pub)).sum(0)
    top = order[:, 0]
    ids, wk1 = np.unique(top, return_counts=True)
    change = np.r_[True, top[1:] != top[:-1]]
    _, nsp = np.unique(top[change], return_counts=True)
    return np.array([(wk10 > 0).sum(), 100 * (wk10[wk10 > 0] == 1).mean(), len(ids), 100 * (wk1 == 1).mean(),
                     100 * (nsp > 1).mean()])


MODELS = [("as built (iid)", 1.0, 0.0), ("no survey noise", 0.0, 0.0), ("iid, sigma x0.5", 0.5, 0.0),
          ("AR rho .5", 1.0, 0.5), ("AR rho .8", 1.0, 0.8), ("AR rho .9", 1.0, 0.9)]


def replay(name, fmt, seeds, reps=100):
    data = [matrices(fmt.format(s=s)) for s in seeds]
    years = len(seeds) * 209 / 52
    res = {"logged (identity)": np.mean([score(np.nan_to_num(p, nan=-np.inf)) for t, u, p in data], axis=0)}
    for label, k, rho in MODELS:
        rng, acc = np.random.default_rng(11), []
        for t, u, _ in data:
            s = sigma(u, k)
            weeks, n = t.shape
            for _ in range(reps):
                z = np.empty((weeks, n))
                z[0] = rng.standard_normal(n)
                e = rng.standard_normal((weeks, n))
                for w in range(1, weeks):
                    z[w] = rho * z[w - 1] + np.sqrt(1 - rho ** 2) * e[w]
                acc.append(score(np.where(np.isnan(t), -np.inf, t + s * z - s * s / 2)))
        res[label] = np.mean(acc, axis=0)
    return [dict(arm=name, model=m, top10_yr=v[0] * len(seeds) / years, top10_one_week_pct=v[1],
                 no1_yr=v[2] * len(seeds) / years, no1_one_week_pct=v[3], returns_pct=v[4]) for m, v in res.items()]


def main():
    arms = {}
    for a in (x for x in sys.argv[1:] if "=" in x):
        name, spec = a.split("=", 1)
        fmt, _, seeds = spec.partition(":")
        arms[name] = (fmt, [int(x) for x in (seeds or "2002,1001").split(",")])
    pd.set_option("display.width", 250)
    rows = [summary("history", history(), 209 / 52)]
    ho = []
    for name, (fmt, seeds) in arms.items():
        c = sim(fmt, seeds)
        years = len(seeds) * 209 / 52
        rows.append(summary(name, c, years))
        ho += handovers(name, c, years)
    print("== 1. #1 spells and returns (history: 1960-63, same 209 chart weeks)")
    print(pd.DataFrame(rows).set_index("arm").round(2).T.to_string())
    print("\n== 2. #1 handovers A -> B; noise = B behind A on true points in its first #1 week; retake = A back next week")
    print(pd.DataFrame(ho).set_index(["arm", "handover"]).round(3).to_string())
    if "--no-replay" in sys.argv:
        return
    print("\n== 3. static replay of the top under survey-noise models (mean of redraws)")
    out = []
    for name, (fmt, seeds) in arms.items():
        out += replay(name, fmt, seeds)
    print(pd.DataFrame(out).round(1).to_string(index=False))


if __name__ == "__main__":
    main()
