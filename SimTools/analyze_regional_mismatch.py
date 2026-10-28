"""What regional top-11 lists disagree about: rank order, timing or repertoire (RegionalSyncDesign.md §9).

Usage: python SimTools/analyze_regional_mismatch.py PREFIX [PREFIX ...] [--year 1960] [--regions all|cities]

Reads <prefix>-completed-week-settlement-regional.csv and ranks each region-week by finalCleared. For every record in
one region's top 11 but not another's the same week, the other region holds it:
  rank     in its top 40 that week (the same repertoire, ordered differently);
  timing   in its top 11 within +/-6 weeks;
  top40    in its top 40 within +/-6 weeks only;
  absent   nowhere in its top 40 within +/-6 weeks (a regional repertoire difference).
History (1960 station surveys, one station per city, net of same-city noise): rank ~25%, timing ~20%, top40 ~25%,
absent ~30%; raw cross-city split 54 / 14 / 14 / 18. `cities` restricts to the four survey regions
(eastcoast, greatlakes, greatplains, westcoast).
"""
import argparse
import itertools
from pathlib import Path

import numpy as np
import pandas as pd

ROOT = Path(__file__).resolve().parents[1]
ALL = ["eastcoast", "greatlakes", "greatplains", "westcoast", "deepsouth", "southwest", "rockies"]
CITIES = ["eastcoast", "greatlakes", "greatplains", "westcoast"]
ADJACENT = {frozenset(p) for p in [("eastcoast", "greatlakes"), ("eastcoast", "deepsouth"), ("greatlakes", "greatplains"),
            ("greatlakes", "deepsouth"), ("greatplains", "rockies"), ("greatplains", "southwest"),
            ("southwest", "deepsouth"), ("southwest", "rockies"), ("southwest", "westcoast"), ("rockies", "westcoast")]}


def score(prefix, year, regions):
    path = Path(prefix + "-completed-week-settlement-regional.csv")
    if not path.is_absolute() and not path.exists():
        path = ROOT / "SimLogs" / path
    s = pd.read_csv(path, usecols=["week", "year", "recordId", "regionId", "finalCleared"])
    s = s[s.year.eq(year) & s.regionId.isin(regions) & s.finalCleared.gt(0)].copy()
    s["rk"] = s.groupby(["week", "regionId"]).finalCleared.rank(ascending=False, method="first")
    s = s[s.rk.le(40)]
    top = {k: dict(zip(g.recordId, g.rk)) for k, g in s.groupby(["week", "regionId"])}
    weeks = sorted(s.week.unique())
    kinds = dict(rank=0, timing=0, top40=0, absent=0)
    pair = {}
    for w in weeks:
        for a, b in itertools.permutations(regions, 2):
            ta, tb = top.get((w, a), {}), top.get((w, b), {})
            head = [r for r, k in ta.items() if k <= 11]
            pair.setdefault(frozenset((a, b)), []).append(sum(tb.get(r, 99) <= 11 for r in head) / 11)
            for r in head:
                if tb.get(r, 99) <= 11:
                    continue
                near = min(top.get((x, b), {}).get(r, 99) for x in range(w - 6, w + 7))
                kinds["rank" if tb.get(r, 99) <= 40 else "timing" if near <= 11 else "top40" if near <= 40 else "absent"] += 1
    n = sum(kinds.values())
    means = {p: float(np.mean(v)) for p, v in pair.items()}
    adj = [v for p, v in means.items() if p in ADJACENT]
    far = [v for p, v in means.items() if p not in ADJACENT]
    return dict(run=Path(prefix).name, overlap=np.mean(list(means.values())), adjacent=np.mean(adj) if adj else np.nan,
                nonAdjacent=np.mean(far) if far else np.nan, mismatches=n,
                **{k: v / n for k, v in kinds.items()})


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("prefixes", nargs="+")
    ap.add_argument("--year", type=int, default=1960)
    ap.add_argument("--regions", choices=["all", "cities"], default="all")
    a = ap.parse_args()
    regions = ALL if a.regions == "all" else CITIES
    rows = pd.DataFrame([score(p, a.year, regions) for p in a.prefixes]).set_index("run")
    pct = ["overlap", "adjacent", "nonAdjacent", "rank", "timing", "top40", "absent"]
    rows[pct] = (rows[pct] * 100).round(1)
    print(rows.to_string())


if __name__ == "__main__":
    main()
