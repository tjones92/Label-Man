"""Top-11 overlap between 1960 station surveys of the same week, by station and by city (RegionalSyncDesign.md).

Usage: python SimTools/analyze_station_survey_overlap.py [--year 1960] [--top 11]

Reads SimLogs/historical-station-surveys/surveys_YEAR.csv (research_station_surveys.py). Records match on title words
as in analyze_survey_overlap.py: same first significant word, or two shared words among the first four. Surveys
pair when dated within 3 days.

Three readings:
  same-city station pairs   the noise floor of one station's list against another's in the same market;
  cross-city station pairs  directly comparable with the 1963-64 WLS/KRLA/KDWB figures;
  city lists                each city's stations averaged by rank (a record missing from one list takes rank top+1),
                            which is closer to a regional sales aggregate than one station.
"""
import argparse
import itertools
import re
import statistics
from datetime import date
from pathlib import Path

import pandas as pd

ROOT = Path(__file__).resolve().parents[1]
STOP = {"the", "a", "i", "my", "you", "to", "of", "in", "on", "be", "and", "it", "s", "me", "is"}
# Great-circle miles between the survey cities, for the distance gradient.
MILES = {frozenset(p): m for p, m in {
    ("New York", "Chicago"): 713, ("New York", "Minneapolis"): 1018, ("New York", "Los Angeles"): 2451,
    ("Chicago", "Minneapolis"): 355, ("Chicago", "Los Angeles"): 1745, ("Minneapolis", "Los Angeles"): 1524}.items()}


def words(text):
    text = re.sub(r"^(?:[\d*#|.\-]+\s+|HB\s+|new\s+|debut\s+|--\s+)+", "", text.strip(), flags=re.I).lstrip("*")
    title = re.split(r"\s+-\s+|\s+--?\s*|\s+by\s+|\||\(|\.{2,}", text)[0]
    title = title.lower().replace("'", "").replace("’", "")
    return [w for w in re.findall(r"[a-z]+", title) if w not in STOP][:4]


def same(a, b):
    return bool(a and b) and (a[0] == b[0] or len(set(a) & set(b)) >= 2)


def overlap(ta, tb, top):
    used, hits = set(), 0
    for a in ta[:top]:
        j = next((j for j, b in enumerate(tb[:top]) if j not in used and same(a, b)), None)
        if j is not None:
            used.add(j)
            hits += 1
    return hits / top


def city_list(lists, top):
    """Average rank across a city's stations; titles keyed on the first station that lists them."""
    keys, ranks = [], []
    for li in lists:
        for r, w in enumerate(li, 1):
            k = next((i for i, kw in enumerate(keys) if same(kw, w)), None)
            if k is None:
                keys.append(w)
                ranks.append({})
                k = len(keys) - 1
            ranks[k].setdefault(id(li), r)
    score = [sum(rk.get(id(li), len(li) + 1) for li in lists) / len(lists) for rk in ranks]
    return [keys[i] for i in sorted(range(len(keys)), key=lambda i: score[i])][:top]


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--year", type=int, default=1960)
    ap.add_argument("--top", type=int, default=11)
    a = ap.parse_args()
    d = pd.read_csv(ROOT / "SimLogs" / "historical-station-surveys" / f"surveys_{a.year}.csv")
    surveys = {}
    for (st, city, day), g in d.groupby(["station", "city", "date"]):
        li = [words(t) for t in g.sort_values("rank").text]
        if sum(bool(w) for w in li[:a.top]) >= a.top - 1:
            surveys[(st, city, date.fromisoformat(day))] = li
    print({st: sum(k[0] == st for k in surveys) for st in sorted({k[0] for k in surveys})})

    pairs = []
    for (ka, la), (kb, lb) in itertools.combinations(surveys.items(), 2):
        if ka[0] == kb[0] or abs((ka[2] - kb[2]).days) > 3:
            continue
        pairs.append(dict(a=ka[0], b=kb[0], cityA=ka[1], cityB=kb[1], day=min(ka[2], kb[2]),
                          ov=overlap(la, lb, a.top)))
    p = pd.DataFrame(pairs)
    p["kind"] = (p.cityA == p.cityB).map({True: "same city", False: "cross city"})
    p["pair"] = [" - ".join(sorted(x)) for x in zip(p.a, p.b)]
    print(f"\nStation pairs (top {a.top}, dated within 3 days):")
    print(p.groupby(["kind", "pair"]).ov.agg(["count", "mean", "median"]).round(3).to_string())
    print(p.groupby("kind").ov.agg(["count", "mean", "median"]).round(3).to_string())

    # City lists: each city-week from all its stations dated within 3 days of a Monday anchor.
    by_city = {}
    for (st, city, day), li in surveys.items():
        anchor = day.toordinal() - (day.toordinal() % 7)
        by_city.setdefault((city, anchor), {}).setdefault(st, li)
    cities = {k: city_list(list(v.values()), a.top) for k, v in by_city.items()}
    rows = []
    for (ca, wa), (cb, wb) in itertools.combinations(cities, 2):
        if ca != cb and wa == wb:
            rows.append(dict(pair=" - ".join(sorted((ca, cb))), miles=MILES[frozenset((ca, cb))],
                             ov=overlap(cities[(ca, wa)], cities[(cb, wb)], a.top)))
    c = pd.DataFrame(rows)
    print(f"\nCity lists (stations averaged by rank):")
    print(c.groupby(["pair", "miles"]).ov.agg(["count", "mean", "median"]).round(3).sort_index(level=1).to_string())
    print("all city pairs: mean %.3f, median %.3f; corr(miles, overlap) by pair %.2f" % (
        c.ov.mean(), c.ov.median(), c.groupby("miles").ov.mean().reset_index().corr().iloc[0, 1]))


if __name__ == "__main__":
    main()
