"""Phase 4e sizing (SimTools/BandMemberSimulationDirective.md §13): replay Musician.WouldConsiderSoloCareer offline.

Usage:  py SimTools/fit_solo_intent.py [<obs-run>] [--slice-ratio=2.0]

Reads <obs-run>-lineup-pairs.csv (default bms2-obs-1001), unpacks it to member-years in group acts, and keeps the
ones who already clear the other two solo tests (fame bar at the treated slice, half the spotlight, lead or
stagePresence >= .7). stagePresence comes from any bms4d-ctl musician-recognition table (fallback .5). Years in
group are counted from the member's first year in the log; act hits are the running sum of top40Now. Prints, by act
success tier, the share of groups holding a member who would pass the old and the new intent, against the
reference set's 17.6% of 91 top-40 groups. An upper bound on spin-outs: the member must still depart.
"""
import os
import sys

import numpy as np
import pandas as pd

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LOGS = os.path.join(ROOT, "SimLogs")
BAR, SPOT = 0.05, 0.50


def main():
    run, ratio = "bms2-obs-1001", 2.0
    for a in sys.argv[1:]:
        if a.startswith("--slice-ratio="):
            ratio = float(a.split("=", 1)[1])
        else:
            run = a
    cols = ["year", "artistId", "top40Now", "constitution", "personA", "personB", "loyaltyA", "loyaltyB", "egoA",
            "egoB", "ambitionA", "ambitionB", "spotA", "spotB", "recognitionA", "recognitionB", "leadA", "leadB"]
    p = pd.read_csv(os.path.join(LOGS, f"{run}-lineup-pairs.csv"), usecols=cols)
    p = p[p.constitution != "Solo"]
    sides = []
    for s in "AB":
        d = p[["year", "artistId", "person" + s, "loyalty" + s, "ego" + s, "ambition" + s, "spot" + s,
               "recognition" + s, "lead" + s]].copy()
        d.columns = ["year", "artistId", "pid", "loyalty", "ego", "ambition", "spot", "rec", "lead"]
        sides.append(d)
    m = pd.concat(sides).drop_duplicates(["year", "artistId", "pid"])
    act = p.drop_duplicates(["year", "artistId"])[["year", "artistId", "top40Now"]].sort_values(["artistId", "year"])
    act["hits"] = act.groupby("artistId").top40Now.cumsum()
    m = m.merge(act[["year", "artistId", "hits"]], on=["year", "artistId"])
    m["yrs"] = m.year - m.groupby(["artistId", "pid"]).year.transform("min")
    frames = [pd.read_csv(os.path.join(LOGS, f), usecols=["personId", "stagePresence"])
              for f in os.listdir(LOGS) if f.startswith("bms4d-ctl-") and f.endswith("-musician-recognition.csv")]
    sp = pd.concat(frames).drop_duplicates("personId").set_index("personId").stagePresence if frames else pd.Series(dtype=float)
    m["sp"] = m.pid.map(sp).fillna(0.5)
    pre = m[(m.hits > 0) & (m.rec * ratio >= BAR) & (m.spot >= SPOT) & ((m.lead == 1) | (m.sp >= 0.7))]
    urge = pre.ambition * .4 + pre.ego * .3 + pre.sp * .2
    old = (urge + np.where(pre.hits > 5, .2, 0)) > (pre.loyalty * .5 + pre.yrs * .05 + .3)

    def new(pull, base, years=6, full_hits=2.0):
        success = np.minimum(1, pre.hits / full_hits)
        return (urge + pull * np.minimum(pre.yrs + 1, years) * success) > (pre.loyalty * .5 + base)

    print(f"{run}: member-years past bar+spotlight {len(pre)}; old intent passes {old.mean():.1%}")
    best = act.groupby("artistId").hits.max()
    for tier in [1, 3, 5, 8]:
        groups = set(best[best >= tier].index)
        sub = pre.artistId.isin(groups)
        line = f"  hits>={tier}: {len(groups)} groups, bar+spot {pre[sub].artistId.nunique() / len(groups):.1%}," \
               f" old {pre[sub & old].artistId.nunique() / len(groups):.1%}"
        for pull, base in [(0.03, 0.15), (0.05, 0.10), (0.05, 0.15), (0.05, 0.20)]:
            ok = sub & new(pull, base)
            line += f" | {pull}/{base} {pre[ok].artistId.nunique() / len(groups):.1%}"
        print(line)


if __name__ == "__main__":
    main()
