"""Model reporter-station playlists scored like the 1960 station surveys (RegionalSyncDesign.md §9f).

Usage: python SimTools/analyze_station_playlist_overlap.py PREFIX [PREFIX ...] [--year 1960] [--format Top40]

Reads <prefix>-station-playlists.csv (ChartAuditRunner --station-playlists): each reporter station's weekly ranked
playlist. Rank 1-11 is the "top 11"; the whole playlist (35 slots: High 8, Mid 12, Light 15) stands in for a survey's
top 40. For every top-11 record missing from another station's top 11 the same week, the other station has it:
rank (on its playlist that week), timing (top 11 within +/-6 weeks), top40 (on its playlist within +/-6 weeks only)
or absent. Pairs are same-city or cross-city; `survey cities` restricts to New York, Chicago, Minneapolis and Los
Angeles, the cities of the 1960 sample.

History (1960, analyze_station_survey_overlap.py and §9): top-11 overlap same city 69.7%, cross city 40.3%; cross-city
mismatches 53.8 rank / 14.3 timing / 14.1 top40 / 17.8 absent; same city 82.9 / 6.9 / 2.5 / 7.8.
"""
import argparse
import itertools
from collections import defaultdict
from pathlib import Path

import numpy as np
import pandas as pd

ROOT = Path(__file__).resolve().parents[1]
SURVEY_CITIES = {"New York", "Chicago", "Minneapolis", "Los Angeles"}
HISTORY = {"same city": (69.7, 82.9, 6.9, 2.5, 7.8), "cross city": (40.3, 53.8, 14.3, 14.1, 17.8)}


def score(prefix, year, fmt):
    path = ROOT / "SimLogs" / f"{prefix}-station-playlists.csv"
    d = pd.read_csv(path)
    d = d[d.year.eq(year)]
    if fmt:
        d = d[d.format.isin(fmt.split(","))]
    lists = {(w, s): g.sort_values("rank") for (w, s), g in d.groupby(["week", "stationId"])}
    rank_of = {k: dict(zip(g.recordId, g["rank"])) for k, g in lists.items()}
    city = d.drop_duplicates("stationId").set_index("stationId").city.to_dict()
    weeks = sorted(d.week.unique())
    stations = sorted(city)
    out = []
    for subset in ["all cities", "survey cities"]:
        st = [s for s in stations if subset == "all cities" or city[s] in SURVEY_CITIES]
        acc = defaultdict(lambda: dict(n=0, ov=[], rank=0, timing=0, top40=0, absent=0))
        for a, b in itertools.permutations(st, 2):
            kind = "same city" if city[a] == city[b] else "cross city"
            r = acc[kind]
            for w in weeks:
                ta, tb = rank_of.get((w, a)), rank_of.get((w, b))
                if not ta or not tb:
                    continue
                head = [x for x, k in ta.items() if k <= 11]
                r["ov"].append(sum(tb.get(x, 99) <= 11 for x in head) / 11)
                for x in head:
                    if tb.get(x, 99) <= 11:
                        continue
                    r["n"] += 1
                    near = min(rank_of.get((v, b), {}).get(x, 99) for v in range(w - 6, w + 7))
                    r["rank" if x in tb else "timing" if near <= 11 else "top40" if near < 99 else "absent"] += 1
        for kind, r in acc.items():
            n = max(1, r["n"])
            out.append(dict(run=prefix, cities=subset, pairs=kind, stations=len(st), overlap=100 * np.mean(r["ov"]),
                            rank=100 * r["rank"] / n, timing=100 * r["timing"] / n, top40=100 * r["top40"] / n,
                            absent=100 * r["absent"] / n))
    for kind, (ov, rk, tm, t40, ab) in HISTORY.items():
        out.append(dict(run="history 1960", cities="survey cities", pairs=kind, stations=np.nan, overlap=ov, rank=rk,
                        timing=tm, top40=t40, absent=ab))
    return out, d


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("prefixes", nargs="+")
    ap.add_argument("--year", type=int, default=1960)
    ap.add_argument("--format", default="Top40", help="comma-separated station formats to keep ('' for all)")
    a = ap.parse_args()
    rows = []
    for p in a.prefixes:
        r, d = score(p, a.year, a.format)
        rows += r
        print(p, "stations by city:", d.drop_duplicates("stationId").groupby("city").size().to_dict())
    t = pd.DataFrame(rows).drop_duplicates(["run", "cities", "pairs"])
    print(t.set_index(["run", "cities", "pairs"]).round(1).to_string())


if __name__ == "__main__":
    main()
