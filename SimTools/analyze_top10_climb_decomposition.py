"""What carries a climbing record's weekly demand growth (Top10TurnoverFindings.md, section 4).

usage: analyze_top10_climb_decomposition.py [name=fmt ...]    ({s} = seed; default control + he095, seeds 2002 + 1001)

Needs single-demand-stages.csv (the bundle's --followup-chart-trace; it covers ~75% of Top 10 records). Per record-week,
regions are summed: A = aware buyers, R = raw demand, Cl = cleared units; conversion = R / A. A climbing step is
week t -> t+1 with published position falling (improving) and the record at #11-40 or #1-10 in week t.
Sales in week t+1 read the position published in week t, so the rank-keyed purchase-exposure term moves by
log w(pos_t) - log w(pos_t-1), w = ChartSimulator.GetChartExposureWeight (Hesbacher^0.62, flat inside the Top 10).
Awareness floors (0.4 inside the Top 40, 0.7 inside the Top 10) act on aware buyers; the release ramp on conversion
until week 6. Medians of weekly log changes; "shareOfR" = median component / median dlogR.
"""
import sys
from pathlib import Path

import numpy as np
import pandas as pd

ROOT = Path(__file__).resolve().parent.parent / "SimLogs"
SEEDS = (2002, 1001)
ARMS = {"base": "fit208-v1-{s}-base", "lw0up1": "fit208-v1-{s}-lw0up1", "he095": "fit208-v5-{s}-lw0up1_ao2_lh1_he095"}
pd.set_option("display.width", 250)


def hesb(x):
    return 4139 - 4357 * x / (x + 10)


def exposure(pos):
    x = np.clip(np.where(pos <= 0, 100, pos), 10, 100)
    return np.power(hesb(x) / hesb(100), 0.62)


def ramp(age):
    return np.where(age >= 6, 1.0, 0.2 + (1 - 0.2) * np.clip((age - 1) / 5, 0, 1))   # push term ignored


def load(run):
    d = pd.read_csv(ROOT / f"{run}-single-demand-stages.csv",
                    usecols=["week", "recordId", "age", "baselineAwareness", "awareBuyers", "rawDemand", "clearedUnits",
                             "chartSignal", "radioSignal", "momentumSignal"])
    d["wAware"] = d.baselineAwareness * d.awareBuyers
    n = d.groupby(["recordId", "week"]).agg(A=("awareBuyers", "sum"), R=("rawDemand", "sum"), Cl=("clearedUnits", "sum"),
                                              age=("age", "max"), chart=("chartSignal", "mean"),
                                              radio=("radioSignal", "mean"), mom=("momentumSignal", "mean"))
    r = pd.read_csv(ROOT / f"{run}-records.csv", usecols=["week", "recordId", "currentPosition", "unitsThisWeek",
                                                           "unsampledChartPoints"])
    r = r.set_index(["recordId", "week"])
    n = n.join(r, how="inner").reset_index().sort_values(["recordId", "week"])
    n["air"] = (n.unsampledChartPoints - n.unitsThisWeek).clip(lower=1)
    g = n.groupby("recordId")
    n["posPrev"] = g.currentPosition.shift(1).fillna(0)
    nxt = g.shift(-1)
    ok = nxt.week == n.week + 1
    pos, posn = n.currentPosition, nxt.currentPosition
    step = pd.DataFrame({
        "band": np.where(pos.between(1, 10), "1-10", np.where(pos.between(11, 40), "11-40", "other")),
        "climb": ok & (posn > 0) & (posn < pos) & (pos > 0),
        "fall": ok & (pos > 0) & ((posn > pos) | (posn == 0)),
        "dlogR": np.log(nxt.R / n.R), "dlogA": np.log(nxt.A / n.A), "dlogConv": np.log((nxt.R / nxt.A) / (n.R / n.A)),
        "dlogJ": np.log(exposure(pos.values) / exposure(n.posPrev.values)),
        "dlogRamp": np.log(ramp(nxt.age.values) / ramp(n.age.values)),
        "dlogClear": np.log((nxt.Cl / nxt.R) / (n.Cl / n.R)), "dlogUnits": np.log(nxt.unitsThisWeek.clip(lower=1) / n.unitsThisWeek.clip(lower=1)),
        "dlogAir": np.log(nxt.air / n.air), "age": n.age, "crossT10": (posn <= 10) & (pos > 10),
        "crossT40": (posn <= 40) & (pos > 40)})
    return step.replace([np.inf, -np.inf], np.nan)


def main():
    arms = dict(a.split("=", 1) for a in sys.argv[1:] if "=" in a) or ARMS
    cols = ["dlogR", "dlogA", "dlogConv", "dlogJ", "dlogRamp", "dlogClear", "dlogUnits", "dlogAir"]
    for name, fmt in arms.items():
        s = pd.concat([load(fmt.format(s=seed)) for seed in SEEDS])
        print(f"== {name}")
        for kind in ("climb", "fall"):
            x = s[s[kind] & s.band.isin(["1-10", "11-40"])]
            t = x.groupby("band")[cols].median()
            t["n"] = x.groupby("band").size()
            t["medAge"] = x.groupby("band").age.median()
            print(f"-- {kind}ing steps (medians of weekly log change)\n{t.round(3).to_string()}")
            m = x.groupby("band")[cols].mean()
            print(f"   means\n{m.round(3).to_string()}")
        c = s[s.climb & s.band.eq("11-40")]
        print(f"-- climbs from #11-40: share crossing into the Top 10 {100 * c.crossT10.mean():.1f}%; "
              f"age <6 share {100 * (c.age < 6).mean():.1f}%")
        print()


if __name__ == "__main__":
    main()
