"""Summarize held-out exploratory component and regional distributions."""
import argparse
import csv
import json
from collections import Counter, defaultdict
from pathlib import Path

from fit_stature_experiment import is_fit, percentile
from stature_normalization import file_digest


def screen(components, regional, output, effective_date):
    values = defaultdict(list)
    status = Counter()
    with components.open(newline='', encoding='utf-8-sig') as stream:
        for row in csv.DictReader(stream):
            if is_fit(row['artistId']) or row['date'] < effective_date:
                continue
            component = row['component']
            status[component + ':' + row['evidenceStatus']] += 1
            for field in ('fixedScore', 'peerScore'):
                if row[field]:
                    values[(component, field)].append(float(row[field]))
    with regional.open(newline='', encoding='utf-8-sig') as stream:
        for row in csv.DictReader(stream):
            if is_fit(row['artistId']):
                continue
            status['regional:' + row['status']] += 1
            for field in ('regionalScore', 'regionalDraw', 'regionalReach', 'recordReach', 'conversionReach'):
                if row[field]:
                    values[('regional', field)].append(float(row[field]))
    summary = {}
    for key, items in sorted(values.items()):
        summary[':'.join(key)] = {'count': len(items), 'zeroFraction': sum(x == 0 for x in items) / len(items),
            'clippedFraction': sum(x == 1 for x in items) / len(items), 'p50': percentile(items, .5),
            'p90': percentile(items, .9), 'p99': percentile(items, .99)}
    data = {'componentsSha256': file_digest(components), 'regionalSha256': file_digest(regional),
            'population': 'held-out artist IDs only, from reference effective date', 'statuses': dict(sorted(status.items())),
            'distributions': summary, 'caveat': 'Exploratory 13-week screen; cohort not available; not causal.'}
    with output.open('x', encoding='utf-8') as out:
        json.dump(data, out, indent=2, sort_keys=True); out.write('\n')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('components', type=Path)
    parser.add_argument('regional', type=Path)
    parser.add_argument('output', type=Path)
    parser.add_argument('--effective-date', default='1960-01-08')
    args = parser.parse_args()
    screen(args.components, args.regional, args.output, args.effective_date)
