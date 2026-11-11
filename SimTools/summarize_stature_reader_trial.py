"""Validate and summarize the opt-in programme album-reader trial."""
import argparse
import csv
import datetime as dt
import json
from collections import Counter, defaultdict
from pathlib import Path

from stature_normalization import file_digest, parse_date


def summarize_arm(path, causal):
    counts, annual, changed_artists = Counter(), defaultdict(Counter), set()
    with path.open(newline='', encoding='utf-8-sig') as stream:
        for row in csv.DictReader(stream):
            counts['candidates'] += 1
            if row['eligible'] != '1':
                continue
            day = parse_date(row['date'])
            group = annual[day.year]
            counts['eligible'] += 1; group['eligible'] += 1
            group['status:' + row['readStatus']] += 1
            old, applied = row['oldMultiplier'], row['appliedMultiplier']
            if not old or not applied:
                raise ValueError('Eligible candidate lacks old or applied multiplier')
            available = row['readStatus'] == 'Available'
            if available:
                age = (day - dt.date.fromisoformat(row['snapshotDate'])).days
                if not 1 <= age <= 7 or not row['proposedMultiplier']:
                    raise ValueError('Invalid available snapshot')
            expected = row['proposedMultiplier'] if causal and available else old
            if applied != expected:
                raise ValueError('Applied multiplier violates the reader rule')
            if float(applied) != float(old):
                counts['changed'] += 1; group['changed'] += 1
                group['higher' if float(applied) > float(old) else 'lower'] += 1
                changed_artists.add(row['artistId'])
    if not counts['eligible']:
        raise ValueError('No eligible programme candidates')
    return {'candidates': counts['candidates'], 'eligible': counts['eligible'], 'changed': counts['changed'],
            'distinctChangedArtists': len(changed_artists), 'years': {str(y): dict(sorted(c.items())) for y, c in sorted(annual.items())}}


def summarize_slate(path):
    states = defaultdict(Counter)
    with path.open(newline='', encoding='utf-8-sig') as stream:
        for row in csv.DictReader(stream):
            states[row['format']][row['state']] += 1
    return {format: dict(sorted(c.items())) for format, c in sorted(states.items())}


def run(control, treatment, control_slate, treatment_slate, output):
    sources = (control, treatment, control_slate, treatment_slate)
    result = {'sha256': {path.name: file_digest(path) for path in sources},
              'control': summarize_arm(control, False), 'treatment': summarize_arm(treatment, True),
              'controlSlateEvents': summarize_slate(control_slate), 'treatmentSlateEvents': summarize_slate(treatment_slate),
              'caveat': 'Programme album-candidate observations may repeat the same act; slate counts are events, not unique releases. Worlds diverge, so arm differences are net simulation effects, not effects on matched acts. This is an experimental programme-ranking reader using the existing unadopted calibration.'}
    with output.open('x', encoding='utf-8') as stream:
        json.dump(result, stream, indent=2, sort_keys=True)
        stream.write('\n')


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    for name in ('control', 'treatment', 'control_slate', 'treatment_slate', 'output'):
        p.add_argument(name, type=Path)
    a = p.parse_args(); run(a.control, a.treatment, a.control_slate, a.treatment_slate, a.output)
