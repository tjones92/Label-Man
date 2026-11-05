"""Describe follower launch books for all 1963-64 single launches, by eventual observed chart cohort.

Usage: python SimTools/analyze_veteran_follower.py RUN
The unobserved group includes acts that never charted in the available run. Cohorts are assigned
using the complete available chart history, including future years; they are not launch-time facts.
"""
import csv
import json
import sys
from collections import defaultdict

prefix = 'SimLogs/' + sys.argv[1]
def rows(suffix):
    with open(prefix + suffix, encoding='utf-8-sig', newline='') as f:
        yield from csv.DictReader(f)

first = {}
top = set()
for r in rows('-records.csv'):
    if not 1 <= int(r['currentPosition']) <= 100:
        continue
    year = int(r['year'])
    first[r['artistId']] = min(year, first.get(r['artistId'], year))
    if year in (1963, 1964) and int(r['currentPosition']) <= 10:
        top.add(r['recordId'])
books = {}
for r in rows('-following.csv'):
    if r['kind'] != 'launch' or not r['detail'].startswith('Single|'):
        continue
    parts = r['detail'].split('|')
    values = dict(x.split('=', 1) for x in parts[1:-1] if '=' in x)
    books[parts[-1]] = (float(r['value']), float(values['granted']), float(values['stocked']))
groups = defaultdict(list)
for r in rows('-single-launch-attribution.csv'):
    year = int(r['year'])
    if year not in (1963, 1964):
        continue
    cohort = first.get(r['artistId'])
    group = '1960' if cohort == 1960 else '1961-62' if cohort in (1961, 1962) else '1963-64' if cohort in (1963, 1964) else 'other-or-unobserved'
    asked, granted, stocked = books.get(r['singleId'], (0, 0, 0))
    groups[(year, group)].append((float(r['initialStock']), asked, granted, stocked, r['singleId'] in top, r['singleId'] in books))
out = {}
for (year, group), data in sorted(groups.items()):
    n = len(data)
    out[f'{year} {group}'] = dict(launches=n, closedBooks=sum(x[5] for x in data), top10Records=sum(x[4] for x in data),
        meanInitialStock=round(sum(x[0] for x in data)/n), meanAsked=round(sum(x[1] for x in data)/n),
        meanGranted=round(sum(x[2] for x in data)/n), meanStocked=round(sum(x[3] for x in data)/n))
print(json.dumps(out, indent=2))
