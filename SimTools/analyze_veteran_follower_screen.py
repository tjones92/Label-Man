"""Annual paired-screen metrics from completed chart audit CSVs. Usage: python ... RUN [RUN ...]."""
import csv
import json
import sys
from collections import defaultdict

def read(path):
    with open(path, encoding='utf-8-sig', newline='') as file:
        yield from csv.DictReader(file)

def analyze(name):
    prefix = 'SimLogs/' + name
    # A live audit can leave the last row partially written; completed years still remain usable.
    rows = [r for r in read(prefix + '-records.csv')
            if r.get('week') and r.get('year') and r.get('currentPosition') and r.get('artistId') and r.get('recordId')]
    first, entries = {}, {}
    yearly = defaultdict(lambda: dict(slots=0, top=0, old_top=0, early_top=0, new_top=0,
                                      major_top=0, entrants=0, repeat=0))
    act_weeks = defaultdict(int)
    for r in rows:
        pos, year, week = int(r['currentPosition']), int(r['year']), int(r['week'])
        if not 1 <= pos <= 100:
            continue
        artist = r['artistId']
        first[artist] = min(year, first.get(artist, year))
        act_weeks[(year, artist)] += 1
        if pos <= 40:
            key = r['recordId']
            if key not in entries or week < entries[key][0]:
                entries[key] = (week, year, artist)
    for r in rows:
        pos, year = int(r['currentPosition']), int(r['year'])
        if not 1 <= pos <= 100:
            continue
        a, cohort = yearly[year], first[r['artistId']]
        a['slots'] += 1
        if pos <= 10:
            a['top'] += 1
            a['old_top'] += cohort == 1960
            a['early_top'] += cohort <= 1964
            a['new_top'] += year - 1 <= cohort <= year
            a['major_top'] += r['labelTier'] == 'Major'
    by_artist = defaultdict(list)
    for week, year, artist in sorted(entries.values()):
        a = yearly[year]
        a['entrants'] += 1
        a['repeat'] += any(0 < week - previous <= 104 for previous in by_artist[artist])
        by_artist[artist].append(week)
    for year, a in yearly.items():
        old = [n for (y, artist), n in act_weeks.items() if y == year and first[artist] == 1960]
        new = [n for (y, artist), n in act_weeks.items() if y == year and year - 1 <= first[artist] <= year]
        a['old_charting'] = len(old)
        a['old_weeks_per_act'] = round(sum(old) / len(old), 2) if old else None
        a['new_charting'] = len(new)
        a['new_weeks_per_act'] = round(sum(new) / len(new), 2) if new else None
    for r in read(prefix + '-weeks.csv'):
        yearly[int(r['year'])].setdefault('market_units', 0)
        yearly[int(r['year'])]['market_units'] += int(r['totalMarketUnits'])
    return dict(run=name, annual=dict(sorted(yearly.items())))

for name in sys.argv[1:]:
    print(json.dumps(analyze(name), indent=2))
