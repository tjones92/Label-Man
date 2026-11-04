"""Summarize the first material-search review per mature single intention.

Programme search is only one route; its losses are not a complete access census.
"""
import argparse
import csv
import json
from collections import Counter, defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / 'SimLogs'


def rows(run, suffix):
    with (ROOT / f'{run}-{suffix}.csv').open(encoding='utf-8-sig', newline='') as f:
        yield from csv.DictReader(f)


def summarize(run, weeks):
    ends = max(int(r['week']) for r in rows(run, 'records'))
    if ends != weeks:
        raise ValueError(f'{run}: expected {weeks} weeks, got {ends}')
    plans = {}
    for r in rows(run, 'release-slate'):
        p = int(r['proposedWeek'])
        if r['format'] == 'Single' and 1 <= p <= weeks - 26:
            plans[r['slotId']] = p
    first = {}
    for r in rows(run, 'material-funnel'):
        if r['slotId'] in plans and int(r['week']) <= plans[r['slotId']] + 26:
            first.setdefault(r['slotId'], r)
    counts = Counter()
    bands = defaultdict(Counter)
    for r in first.values():
        c = counts
        band = 'low' if float(r['craft']) < .4 else 'middle' if float(r['craft']) < .6 else 'high'
        for c in (counts, bands[band]):
            c['reviews'] += 1
            for key in ('pool', 'draws', 'unique', 'accessible', 'owned', 'future', 'held', 'placement', 'license', 'fitRefused', 'positiveProfessional', 'positiveCover'):
                c[key] += int(r[key])
            c['emptyPool'] += int(int(r['pool']) == 0)
            c['noAccessibleOrOwned'] += int(int(r['accessible']) == 0 and int(r['owned']) == 0)
            c['positiveProReviews'] += int(int(r['positiveProfessional']) > 0)
            c['positiveCoverReviews'] += int(int(r['positiveCover']) > 0)
            c['selected_' + r['selected']] += 1
            c['started_' + r['selected']] += int(r['started'])
            if r['selected'] != 'none' and not int(r['started']) and r.get('startBlock'):
                c['blocked_' + r['selected'] + '_' + r['startBlock']] += 1
            c['proPositiveButCoverSelected'] += int(int(r['positiveProfessional']) > 0 and r['selected'] == 'cover')
    arrival_path = ROOT / f'{run}-professional-arrivals.csv'
    arrivals = defaultdict(Counter)
    if arrival_path.exists():
        for r in rows(run, 'professional-arrivals'):
            arrivals[r['provenance']][r['outcome']] += 1
    return dict(run=run, weeks=weeks, maturePlans=len(plans), observedFirstReviews=len(first), totals=dict(counts),
                craftBands={k:dict(v) for k,v in sorted(bands.items())},
                arrivalOutcomes={k:dict(v) for k,v in sorted(arrivals.items())} if arrival_path.exists() else None)


if __name__ == '__main__':
    p = argparse.ArgumentParser()
    p.add_argument('runs', nargs='+')
    p.add_argument('--weeks', type=int, default=104)
    p.add_argument('--output', type=Path, required=True)
    args = p.parse_args()
    results = [summarize(run, args.weeks) for run in args.runs]
    manifests = [json.loads((ROOT / 'writing-calibration' / f'{run}-manifest.json').read_text(encoding='utf-8-sig')) for run in args.runs]
    if len({m['assemblySha256'] for m in manifests}) != 1:
        raise ValueError('Mixed assemblies')
    args.output.write_text(json.dumps(results, indent=2) + '\n', encoding='utf-8')
