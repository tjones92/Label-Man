"""How much of a #1's airplay build comes from the per-region build clock (findings §11.9).

usage: analyze_radio_build_clock.py [--last=W] NAME=fmt[:seed,...] ...   (fmt holds {s}; seeds default 2002,1001,3003;
       --last caps the chart weeks read, default 209, so a run still in progress can be scored)

Reads <run>-regional-radio.csv (--regional-radio-build-observe) and <run>-records.csv.

1. checks: the logged regional airplay sums to (unsampledChartPoints - units) / era weight; the Python airplay formula
   and the rotation recursion reproduce the logged values.
2. late regions: a region serviced after launch (arrival age above 1) restarts the six-week radio build ramp at its own
   arrival under --regional-radio-build-clock. Its share of a #1's airplay around the reign start.
3. static counterfactual: each late region's rotation replayed from its arrival with the build weight read at release
   age (the clock off), everything else held as logged. Airplay at reign weeks -2..+3, actual against counterfactual.
"""
import sys
from pathlib import Path
import numpy as np
import pandas as pd

ROOT = Path(__file__).resolve().parent.parent / "SimLogs"
HOLD, LERP = 0.92, 0.15
SETTLE = LERP / (1 - HOLD * (1 - LERP))
DROP_RESIDUAL = 0.30
REF, PTS, CEIL, SHARP = 0.30, 2720.0, 0.85, 8.0
BUILD_FULL, BUILD_FLOOR = 6, 0.28
LAST = 209


def era(year):
    return np.clip(0.60 + (year - 1960) * 0.05, 0.60, 1.00)


def build_weight(age):
    age = np.asarray(age, dtype=float)
    prog = np.clip((age - 1) / (BUILD_FULL - 1), 0, 1)
    return np.where(age >= BUILD_FULL, 1.0, BUILD_FLOOR + (1 - BUILD_FLOOR) * prog)


def airplay(rp, access, scale, convexity=5.0, ceiling=CEIL):
    x = rp / access
    if ceiling > 0:
        x = np.where(x > 0, x / (1 + (x / ceiling) ** SHARP) ** (1 / SHARP), x)
    return REF * (x / REF) ** convexity * access * scale * PTS


def next_rotation(cur, target, rise):
    authored = cur * HOLD + (target - cur * HOLD) * LERP
    if rise <= 0:
        return authored
    settle = SETTLE * target
    return np.where(settle <= cur, authored, np.maximum(authored, cur + (settle - cur) * rise))


def load(fmt, seeds):
    regs, recs = [], []
    for n, s in enumerate(seeds):
        run = fmt.format(s=s)
        g = pd.read_csv(ROOT / f"{run}-regional-radio.csv")
        g = g[g.week <= LAST]
        g["rid"] = f"{s}:" + g.recordId
        g["seed"] = s
        g["wk"] = g.week + 1000 * n
        regs.append(g)
        r = pd.read_csv(ROOT / f"{run}-records.csv", usecols=["week", "year", "recordId", "labelTier", "weeksSinceRelease",
                                                              "currentPosition", "unitsThisWeek", "unsampledChartPoints"])
        r = r[r.week <= LAST].copy()
        r["rid"] = f"{s}:" + r.recordId
        r["wk"] = r.week + 1000 * n
        recs.append(r)
    return pd.concat(regs, ignore_index=True), pd.concat(recs, ignore_index=True)


def replay(g, rise):
    """Late regions only: rotation replayed from arrival, as logged (identity) and with the build clock off.

    Breakout discovery's add after the regional update (radioPlay - step3Play) and any add between weeks (event hooks,
    startPlay - last week's radioPlay) are carried as logged; the counterfactual adds them unclamped."""
    late = g[g.arrivalAge > 1].sort_values(["rid", "region", "wk"]).copy()
    late["key"] = late.rid + "|" + late.region
    first = ~late.key.duplicated()
    late["cont"] = ~first & (late.wk.diff() == 1)
    late["post"] = late.radioPlay - late.step3Play
    late["between"] = np.where(late.cont, late.startPlay - late.radioPlay.shift(), 0.0)
    out_step, out_id, out_cf = np.empty(len(late)), np.empty(len(late)), np.empty(len(late))
    heat, reach, age, buildage = late.heatTarget.values, late.cityReach.values, late.age.values, late.buildAge.values
    start, rep, w, drop = late.startPlay.values, late.reporter.values, late.panelWeight.values, late.dropped.values
    post, between, cont = late.post.values, late.between.values, late.cont.values

    def step(cur, i, b):
        tail = cur * DROP_RESIDUAL if drop[i] else float(next_rotation(cur, heat[i] * b * reach[i], rise))
        return tail + (rep[i] - tail) * w[i]

    prev_id = prev_cf = 0.0
    for i in range(len(late)):
        cur_id = prev_id + between[i] if cont[i] else start[i]
        cur_cf = prev_cf + between[i] if cont[i] else start[i]
        out_step[i] = step(start[i], i, build_weight(buildage[i]))
        out_id[i] = prev_id = step(cur_id, i, build_weight(buildage[i])) + post[i]
        out_cf[i] = prev_cf = step(cur_cf, i, build_weight(age[i])) + post[i]
    late["step_id"], late["rp_id"], late["rp_cf"] = out_step, out_id, out_cf
    return late


def rise_of(run):
    """The run's --regional-radio-rise-lerp, from its request file (0 when off)."""
    import json
    flags = json.load(open(ROOT / "writing-calibration" / f"{run}-request.json", encoding="utf-8-sig"))["flags"]
    vals = [f.split("=", 1)[1] for f in flags if f.startswith("--regional-radio-rise-lerp=")]
    return float(vals[0]) if vals else 0.0


def reigns(recs):
    top = recs[recs.currentPosition == 1].sort_values("wk")[["wk", "rid"]].reset_index(drop=True)
    top["new"] = (top.rid != top.rid.shift()) | (top.wk != top.wk.shift() + 1)
    top["spell"] = top.new.cumsum()
    sp = top.groupby("spell").agg(rid=("rid", "first"), start=("wk", "min"), end=("wk", "max"))
    sp["len"] = sp.end - sp.start + 1
    sp["k"] = sp.groupby("rid").cumcount()
    return sp[sp.k == 0]


def main(arms):
    for name, fmt, seeds in arms:
        g, recs = load(fmt, seeds)
        rise = rise_of(fmt.format(s=seeds[0]))
        print(f"\n=== {name}: {fmt}  seeds {seeds}  rise {rise}")
        # 1. checks
        g["air_py"] = airplay(g.radioPlay.values, g.access.values, g.scale.values)
        rel = (g.air_py - g.airplay).abs() / g.airplay.clip(lower=1)
        print(f"python airplay formula: max rel err {rel.max():.2e}  (rows {len(g):,})")
        full = g[g.position.between(1, 100) & (g.week >= 1)].groupby(["wk", "rid"]).agg(air=("airplay", "sum"), year=("year", "first"))
        m = full.join(recs.set_index(["wk", "rid"])[["unitsThisWeek", "unsampledChartPoints"]], how="inner")
        m["obs"] = (m.unsampledChartPoints - m.unitsThisWeek) / era(m.year)
        err = ((m.obs - m.air).abs() / m.obs.clip(lower=1))
        print(f"regional sum = (unsampled - units)/era: median rel err {err.median():.2e}, 99th {err.quantile(.99):.2e}, n {len(m):,}")
        late = replay(g, rise)
        one = (late.step_id - late.step3Play).abs()
        ident = (late.rp_id - late.radioPlay).abs()
        print(f"rotation replay on {len(late):,} late-region rows: one-step max abs err {one.max():.2e}, "
              f"recursion max abs err {ident.max():.2e}; breakout add mean {late.post.mean():.4f}, "
              f"between-week add mean {late.between.mean():.4f}")
        late["air_cf"] = airplay(late.rp_cf.values, late.access.values, late.scale.values)
        late["air_id"] = airplay(late.rp_id.values, late.access.values, late.scale.values)

        # 2-3. reigns
        sp = reigns(recs)
        sp = sp[(sp.end % 1000) < LAST - 2]  # drop reigns cut off by the end of the window
        sp["cls"] = pd.cut(sp.len, [0, 1, 3, 99], labels=["1", "2-3", "4+"])
        gi = g.set_index(["rid", "wk"]).sort_index()
        lt = late.set_index(["rid", "wk"]).sort_index()
        rows = []
        for _, s in sp.iterrows():
            for j in range(-3, 4):
                wk = s.start + j
                if (s.rid, wk) not in gi.index:
                    continue
                d = gi.loc[(s.rid, wk)]
                year = d.year.iloc[0]
                tot = d.airplay.sum() * era(year)
                late_now = d[d.arrivalAge > 1]
                lr = lt.loc[(s.rid, wk)] if (s.rid, wk) in lt.index else None
                cf_delta = 0.0 if lr is None else float((lr.air_id - lr.air_cf).sum()) * era(year)
                ramp = late_now[late_now.radioBuild < 0.999]
                rows.append(dict(rid=s.rid, cls=s.cls, j=j, air=tot, late=late_now.airplay.sum() * era(year),
                                 air_cf=tot - cf_delta, n_late=len(late_now), n_ramp=len(ramp),
                                 late_scale=late_now.scale.sum() / max(d.scale.sum(), 1e-9),
                                 arr_age=(late_now.arrivalAge.median() if len(late_now) else np.nan),
                                 age=d.age.iloc[0]))
        R = pd.DataFrame(rows)
        R.to_csv(ROOT / f"radio-build-clock-{name}-reigns.csv", index=False)
        print(f"first reigns: {len(sp)} ({sp.cls.value_counts().to_dict()})")
        for cls in ["1", "2-3", "4+"]:
            c = R[R.cls == cls]
            if c.empty:
                continue
            print(f"\n  reign {cls} weeks (n {c.rid.nunique()}): by reign week j (k points, medians; shares are pooled)")
            print("   j | age | airplay | clock off | late-region airplay share | late regions (in ramp) | late reach share | arrival age")
            for j in range(-3, 4):
                cj = c[c.j == j]
                if cj.empty:
                    continue
                print(f"  {j:+d} | {cj.age.median():3.0f} | {cj.air.median()/1e3:7.1f} | {cj.air_cf.median()/1e3:9.1f} | "
                      f"{cj.late.sum()/cj.air.sum():25.2f} | {cj.n_late.mean():5.2f} ({cj.n_ramp.mean():4.2f}) | "
                      f"{cj.late_scale.mean():16.2f} | {cj.arr_age.median():4.0f}")
            # build after #1: growth j0 -> j2, per record
            p = c.pivot_table(index="rid", columns="j", values=["air", "air_cf", "late"])
            if 0 in p["air"].columns and 2 in p["air"].columns:
                ok = p["air"][[0, 2]].notna().all(axis=1)
                a0, a2 = p["air"][0][ok], p["air"][2][ok]
                c0, c2 = p["air_cf"][0][ok], p["air_cf"][2][ok]
                l0, l2 = p["late"][0][ok], p["late"][2][ok]
                d = (a2 - a0).sum()
                print(f"   j0 -> j2 (n {ok.sum()}): airplay x{(a2/a0).median():.2f} actual, x{(c2/c0).median():.2f} clock off; "
                      f"pooled build {d/1e3/ok.sum():.1f}k per record, of which late regions {(l2-l0).sum()/d:.0%}, "
                      f"the build clock {((a2-a0)-(c2-c0)).sum()/d:.0%}")
            if -2 in p["air"].columns and 0 in p["air"].columns:
                ok = p["air"][[-2, 0]].notna().all(axis=1)
                a, b = p["air"][-2][ok], p["air"][0][ok]
                ca, cb = p["air_cf"][-2][ok], p["air_cf"][0][ok]
                print(f"   j-2 -> j0 (n {ok.sum()}): airplay x{(b/a).median():.2f} actual, x{(cb/ca).median():.2f} clock off; "
                      f"the build clock {((b-a)-(cb-ca)).sum()/(b-a).sum():.0%} of the pooled rise")

        # all late regions: how many there are and when they arrive, for Top 10 records
        t10 = recs[recs.currentPosition.between(1, 10)].rid.unique()
        lg = g[g.rid.isin(t10)].drop_duplicates(["rid", "region"])
        per = lg.groupby("rid").agg(late=("arrivalAge", lambda a: (a > 1).sum()), regions=("region", "size"))
        la = lg[lg.arrivalAge > 1]
        print(f"\nTop 10 records: {len(per)}; late regions per record mean {per.late.mean():.2f} of {per.regions.mean():.1f}; "
              f"share with any {(per.late > 0).mean():.0%}; arrival age median {la.arrivalAge.median():.0f} "
              f"(quartiles {la.arrivalAge.quantile(.25):.0f}-{la.arrivalAge.quantile(.75):.0f})")


if __name__ == "__main__":
    arms = []
    for a in [x for x in sys.argv[1:] if x.startswith("--last=")]:
        LAST = int(a.split("=", 1)[1])
    for a in [x for x in sys.argv[1:] if not x.startswith("--")]:
        name, rest = a.split("=", 1)
        fmt, _, sd = rest.partition(":")
        arms.append((name, fmt, [int(x) for x in sd.split(",")] if sd else [2002, 1001, 3003]))
    main(arms)
