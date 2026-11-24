"""Fit a diagnostic band mapping on training artists and past weeks only.

This candidate matches median legacy-rung support, not a target chart outcome.
Reach weights and exit ratio are experimental assumptions; this does not select
or authorize a causal policy. Requires complete observations for all routes.
"""
import argparse
import csv
import json
from collections import defaultdict
from pathlib import Path

from fit_stature_experiment import is_fit, percentile
from stature_normalization import COMPONENTS, file_digest
from stature_shadow_policy import BANDS, ROUTES, evaluate
from stature_shadow_trace import number, max_known


def fit(components, regional, output, effective, mode='rung-median', score_field='peerScore'):
    if score_field not in ('peerScore', 'fixedScore'):
        raise ValueError('Unknown score field')
    reaches = {}
    with regional.open(newline='', encoding='utf-8-sig') as source:
        for row in csv.DictReader(source):
            if row['date'] < effective and is_fit(row['artistId']):
                reaches[(row['date'], row['artistId'])] = row
    weights = {name: dict.fromkeys(ROUTES, value) for name, value in zip(BANDS, (0, .25, .5, .75))}
    base = {'version': 'training-only', 'effectiveDate': '1900-01-01', 'bands': {
        name: {'entry': 0, 'exit': 0, 'reachWeight': weights[name]} for name in BANDS}}
    samples = defaultdict(list)
    pooled = defaultdict(list)
    with components.open(newline='', encoding='utf-8-sig') as source:
        current, group = None, {}

        def consume(key, rows):
            if not key or key[0] >= effective or not is_fit(key[1]) or set(rows) != set(COMPONENTS):
                return
            rung = rows[COMPONENTS[0]]['careerState']
            if key not in reaches:
                return
            region = reaches[key]
            scores = {k: number(v[score_field]) for k, v in rows.items()}
            regional_response = number(region['regionalScore'])
            regional_draw = number(region['regionalDraw'])
            rs = {r: number(region[r + 'Reach']) for r in ROUTES}
            data = {'regional': {'response': regional_response, 'draw': regional_draw, 'reach': rs['regional']},
                    'record': {'response': max_known([scores[k] for k in COMPONENTS[:4]]),
                               'draw': max_known([scores[k] for k in COMPONENTS[6:]]), 'reach': rs['record']},
                    'conversion': {'response': max_known([scores[k] for k in COMPONENTS[4:6]]),
                                   'draw': max_known([scores[k] for k in COMPONENTS[6:]]), 'reach': rs['conversion']}}
            if any(value is None for route in data.values() for value in route.values()):
                return
            result = evaluate(data, base, key[0])
            for name in BANDS:
                pooled[name].append(result['bands'][name]['lower'])
            if rung in BANDS:
                samples[rung].append(result['bands'][rung]['lower'])

        for row in csv.DictReader(source):
            key = (row['date'], row['artistId'])
            if key != current and current is not None:
                consume(current, group); group = {}
            if row['component'] in group:
                raise ValueError('Duplicate component')
            current = key; group[row['component']] = row
        consume(current, group)
    selected = samples if mode == 'rung-median' else pooled
    counts = {name: len(selected[name]) for name in BANDS}
    report = {'counts': counts, 'componentsSha256': file_digest(components), 'regionalSha256': file_digest(regional),
              'effectiveDate': effective, 'training': 'fit artist partition before effective date, complete observations only',
              'mode': mode, 'scoreField': score_field, 'caveat': 'Experimental mapping; not a recommendation or a causal policy.'}
    if min(counts.values()) < 30:
        report['status'] = 'insufficient_complete_observations'
        with output.open('x', encoding='utf-8') as target:
            json.dump(report, target, indent=2, sort_keys=True); target.write('\n')
        return
    quantiles = dict(zip(BANDS, (.5, .75, .90, .97))) if mode == 'pooled-quantiles' else dict.fromkeys(BANDS, .5)
    if any(percentile(selected[name], quantiles[name]) <= 0 for name in BANDS):
        report['status'] = 'unusable_zero_median'
        with output.open('x', encoding='utf-8') as target:
            json.dump(report, target, indent=2, sort_keys=True); target.write('\n')
        return
    bands, previous = {}, 0
    report['status'] = 'candidate_only'
    report['rungSupport'] = {}
    for name in BANDS:
        vals = selected[name]
        median = percentile(vals, .5)
        entry = max(previous, percentile(vals, quantiles[name]))
        bands[name] = {'entry': entry, 'exit': entry * .8, 'reachWeight': weights[name]}
        report['rungSupport'][name] = {'p10': percentile(vals, .1), 'median': median, 'p90': percentile(vals, .9),
                                       'quantile': quantiles[name], 'orderedEntry': entry}
        previous = entry
    # Clipped support can collapse several fitted thresholds to one. Publishing
    # that as a candidate would make nominal bands indistinguishable.
    if any(bands[a]['entry'] >= bands[b]['entry'] for a, b in zip(BANDS, BANDS[1:])):
        report['status'] = 'degenerate_overlapping_boundaries'
    else:
        reference = 'peer' if score_field == 'peerScore' else 'fixed'
        report['calibration'] = {'version': f'experimental-{mode}-{reference}-v1', 'effectiveDate': effective, 'bands': bands}
    with output.open('x', encoding='utf-8') as target:
        json.dump(report, target, indent=2, sort_keys=True); target.write('\n')


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('components', type=Path); p.add_argument('regional', type=Path); p.add_argument('output', type=Path)
    p.add_argument('--effective-date', required=True)
    p.add_argument('--mode', choices=('rung-median', 'pooled-quantiles'), default='rung-median')
    p.add_argument('--score', choices=('peerScore', 'fixedScore'), default='peerScore')
    a = p.parse_args(); fit(a.components, a.regional, a.output, a.effective_date, a.mode, a.score)
