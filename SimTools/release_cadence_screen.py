"""Release Judgement directive, Phase 0 standard screen readout.

Reads the default compact files every audit run now writes:
  <run>-act-release-years.csv     one row per act-year: signed weeks, singles by path, LPs scheduled, distinct Hot 100
                                  records, year-start lifetime counters
  <run>-single-launch-quality.csv one row per AI single launch: realized (hook + production) / 2 and the act's gap
                                  since its previous single (-1 when the run has not seen one)
  <run>-records.csv               only for Hot 100 entry by launch-quality quintile

and prints, for one or more runs (seeds pooled):
  1. the original retrospective groups (Hot 100 records over the window: star 8+, mid 3-7, low 1-2, never charted), per
     act-year that released, next to the 17-act discography sample (singles 4.7 / 3.6 / 2.8, LPs 2.45 / 1.67 / 0.38);
  2. the same with active-week denominators: singles per 52 signed weeks and zero-release signed act-years;
  3. lagged-status cohorts, using only what was public at the start of each year (lifetime Top 10 / Top 40 / any chart);
  4. weeks between an act's singles, by cohort and path;
  5. Hot 100 entry by realized-quality quintile, at the directive's V1 edges.

Usage: py SimTools/release_cadence_screen.py SimLogs/<run-prefix> [SimLogs/<run-prefix> ...] [--years 1961-1964]
A run prefix is the path without the "-act-release-years.csv" suffix.
"""
import sys
from io import BytesIO
from pathlib import Path

import numpy as np
import pandas as pd

HISTORY = pd.DataFrame({"singles mean": [4.68, 3.64, 2.8], "LPs mean": [2.45, 1.67, 0.38]}, index=["star", "mid", "low"])
EDGES = [0.529, 0.588, 0.639, 0.696]   # V1 single-launch quality quintile edges (directive "Measured baseline")


def read(path, cols=None):
    raw = Path(path).read_bytes()   # tolerate a run still writing its last line
    return pd.read_csv(BytesIO(raw[:raw.rfind(b"\n") + 1]), usecols=cols)


def main(argv):
    years = None
    prefixes = []
    it = iter(argv)
    for a in it:
        if a == "--years":
            lo, hi = next(it).split("-")
            years = (int(lo), int(hi))
        else:
            prefixes.append(a)
    if not prefixes:
        print(__doc__)
        return 2
    ay, launches, entries = [], [], []
    for i, p in enumerate(prefixes):
        a = read(f"{p}-act-release-years.csv")
        a["run"] = i
        ay.append(a)
        q = read(f"{p}-single-launch-quality.csv")
        q["run"] = i
        launches.append(q)
        rec = Path(f"{p}-records.csv")
        if rec.exists():
            r = read(rec, ["recordId", "currentPosition"])
            e = set(r.loc[r.currentPosition.between(1, 100), "recordId"])
            q["entered"] = q.singleId.isin(e)
    ay = pd.concat(ay)
    q = pd.concat(launches)
    if years:
        ay = ay[ay.year.between(*years)]
        q = q[q.year.between(*years)]
    n_runs = len(prefixes)
    window = f"{ay.year.min()}-{ay.year.max()}"

    # 1. Retrospective groups over the window (reproduces the original table's grouping).
    recs = ay.groupby(["run", "artistId"]).hot100Records.sum()
    n = pd.Series(list(zip(ay.run, ay.artistId))).map(recs).fillna(0).to_numpy()
    ay["group"] = np.select([n >= 8, n >= 3, n >= 1], ["star", "mid", "low"], "never charted")
    released = ay[ay.singles + ay.lpsScheduled > 0]
    g = released.groupby("group")
    t = pd.DataFrame({
        "act-years / run": g.size() / n_runs, "singles mean": g.singles.mean(), "singles median": g.singles.median(),
        "singles p25-p75": g.singles.apply(lambda x: f"{x.quantile(.25):.0f}-{x.quantile(.75):.0f}"),
        "LPs mean": g.lpsScheduled.mean(), "share of singles %": 100 * g.singles.sum() / released.singles.sum(),
    }).reindex(["star", "mid", "low", "never charted"])
    t = t.join(HISTORY.add_prefix("history "))
    print(f"1. Per act-year with a release, {window}, {n_runs} run(s); groups by Hot 100 records over the window")
    print(t.round(2).to_string())

    # 2. Active-week denominators and zero-release signed years.
    signed = ay[ay.signedWeeks > 0]
    g = signed.groupby("group")
    t = pd.DataFrame({
        "signed act-years / run": g.size() / n_runs, "signed weeks mean": g.signedWeeks.mean(),
        "singles per 52 signed wk": 52 * g.singles.sum() / g.signedWeeks.sum(),
        "LPs per 52 signed wk": 52 * g.lpsScheduled.sum() / g.signedWeeks.sum(),
        "zero-release signed years %": 100 * g.apply(lambda x: ((x.singles + x.lpsScheduled) == 0).mean()),
    }).reindex(["star", "mid", "low", "never charted"])
    print(f"\n2. Signed act-years (an act-year counts the weeks it sat on an AI roster)")
    print(t.round(2).to_string())

    # 3. Lagged status: what a label could know on January 1.
    ay["lagged"] = np.select([ay.top10AtStart > 0, ay.top40AtStart > 0, ay.chartedAtStart > 0], ["top10 act", "top40 act", "charted"],
                             "never charted")
    signed = ay[ay.signedWeeks > 0]
    g = signed.groupby("lagged")
    t = pd.DataFrame({
        "signed act-years / run": g.size() / n_runs, "singles per 52 signed wk": 52 * g.singles.sum() / g.signedWeeks.sum(),
        "orphan share %": 100 * g.orphanSingles.sum() / g.singles.sum().replace(0, np.nan),
        "LPs per 52 signed wk": 52 * g.lpsScheduled.sum() / g.signedWeeks.sum(),
        "Hot 100 records per act-year": g.hot100Records.mean(),
        "zero-release years %": 100 * g.apply(lambda x: ((x.singles + x.lpsScheduled) == 0).mean()),
    }).reindex(["top10 act", "top40 act", "charted", "never charted"])
    print("\n3. Lagged-status cohorts (lifetime counters at the start of each year; no future chart knowledge)")
    print(t.round(2).to_string())

    # 4. Gaps between an act's singles, within the run.
    gaps = q[q.weeksSinceLastSingle >= 0]
    last = ay.drop_duplicates(["run", "artistId", "year"]).set_index(["run", "artistId", "year"]).lagged
    gaps = gaps.assign(lagged=[last.get((r, a, y), "never charted") for r, a, y in zip(gaps.run, gaps.artistId, gaps.year)])
    def qs(x):
        return f"{x.median():.0f} ({x.quantile(.25):.0f}-{x.quantile(.75):.0f})"
    t = gaps.groupby(["lagged", "strategy"]).weeksSinceLastSingle.agg(["size", qs]).rename(columns={"size": "gaps", "qs": "median (p25-p75)"})
    print("\n4. Weeks since the act's previous single (history: stars 14 (11-17) between CHARTING events)")
    print(t.to_string())

    # 5. Entry by realized quality.
    if "entered" in q:
        q["quintile"] = pd.cut(q.diagQuality, [-1] + EDGES + [2], labels=["Q1", "Q2", "Q3", "Q4", "Q5"])
        t = q.pivot_table(index="strategy", columns="quintile", values="entered", aggfunc="mean", observed=False) * 100
        t["launches / run"] = q.groupby("strategy").size() / n_runs
        t["mean quality"] = q.groupby("strategy").diagQuality.mean()
        print("\n5. Hot 100 entry % by realized-quality quintile (V1 edges 0.529 / 0.588 / 0.639 / 0.696)")
        print(t.round(2).to_string())
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
