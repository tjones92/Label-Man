"""Summarize experimental component normalization by year, cohort, tier and genre."""
import argparse
import csv
import math
from collections import defaultdict
from pathlib import Path
from fit_stature_experiment import is_fit


def percentile(values, fraction):
    if not values:
        return None
    ordered = sorted(values)
    index = (len(ordered) - 1) * fraction
    lower = math.floor(index)
    upper = math.ceil(index)
    return ordered[lower] + (ordered[upper] - ordered[lower]) * (index - lower)


def summarize(source, target, holdout_only=False):
    groups = defaultdict(lambda: {'rows': 0, 'fixed': [], 'peer': [], 'paired': [], 'missing': 0, 'cold': 0})
    with source.open(newline='', encoding='utf-8-sig') as stream:
        for row in csv.DictReader(stream):
            if holdout_only and is_fit(row['artistId']):
                continue
            key = (row['date'][:4], row['cohort'], row['tier'], row['genre'], row['component'])
            group = groups[key]
            group['rows'] += 1
            group['missing'] += row['evidenceStatus'] != 'observed'
            group['cold'] += row['peerStatus'] != 'ok'
            fixed = float(row['fixedScore']) if row['fixedScore'] else None
            peer = float(row['peerScore']) if row['peerScore'] else None
            if fixed is not None: group['fixed'].append(fixed)
            if peer is not None: group['peer'].append(peer)
            if fixed is not None and peer is not None: group['paired'].append(peer - fixed)
    if not groups:
        raise ValueError('No component rows')
    fields = ['year', 'cohort', 'tier', 'genre', 'component', 'rows', 'missingEvidence', 'unavailablePeerReference',
              'fixedCount', 'fixedMedian', 'fixedP90', 'peerCount', 'peerMedian', 'peerP90', 'pairedCount', 'meanPeerMinusFixed']
    with target.open('x', newline='', encoding='utf-8') as stream:
        writer = csv.DictWriter(stream, fieldnames=fields)
        writer.writeheader()
        for key, g in sorted(groups.items()):
            output = dict(zip(fields[:5], key))
            output.update(rows=g['rows'], missingEvidence=g['missing'], unavailablePeerReference=g['cold'],
                          fixedCount=len(g['fixed']), fixedMedian=percentile(g['fixed'], 0.5), fixedP90=percentile(g['fixed'], 0.9),
                          peerCount=len(g['peer']), peerMedian=percentile(g['peer'], 0.5), peerP90=percentile(g['peer'], 0.9),
                          pairedCount=len(g['paired']), meanPeerMinusFixed=(sum(g['paired']) / len(g['paired']) if g['paired'] else None))
            writer.writerow(output)
    return len(groups)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('input', type=Path)
    parser.add_argument('output', type=Path)
    parser.add_argument('--holdout-only', action='store_true', help='Exclude all artists in the frozen reference fit partition')
    args = parser.parse_args()
    print(f'Wrote {summarize(args.input, args.output, args.holdout_only)} component groups')
