"""Compare lagged peer-normalized TV registrations and booked TV joins.

This is an offline sensitivity screen, not a new standing policy. Both inputs
use the same explicit smoothing and peer-floor parameters; source units differ.
"""
import argparse
import csv
import datetime as dt
import json
import math
from collections import defaultdict
from pathlib import Path

from compare_stature_tv_conversion import percentile
from stature_normalization import file_digest, normalize, parse_date, peer_reference, peer_references, smooth
from verify_stature_evidence import verify


def compare(source, output, half_life=4., top_n=20, minimum=25, floor=1., evaluation_start='1960-07-01'):
    if not math.isfinite(half_life) or half_life <= 0 or top_n < 1 or minimum < 1 or not math.isfinite(floor) or floor <= 0:
        raise ValueError('Invalid normalization parameters')
    cutoff = dt.date.fromisoformat(evaluation_start)
    verify(source)
    alpha = 1 - 0.5 ** (1 / half_life)
    columns = ('tvRegisteredYoungEstimate', 'tvJoinYoung', 'tvRegisteredAdultEstimate', 'tvJoinAdult')
    names = ('regYoung', 'joinYoung', 'regAdult', 'joinAdult')
    previous, previous_day, current_day, acts = {}, None, None, {}
    differences = defaultdict(list)
    active_differences = defaultdict(list)

    def process(day, week):
        nonlocal previous, previous_day
        consecutive = previous_day is not None and (day - previous_day).days == 7
        prior = previous if consecutive else {}
        ranked = {name: peer_references(prior, name, top_n) for name in names}
        updated = {}
        for artist, values in sorted(week.items()):
            old = prior.get(artist)
            current = {name: smooth(old.get(name) if old else None, values[i], alpha) for i, name in enumerate(names)}
            updated[artist] = current
            if day < cutoff or not consecutive:
                continue
            scores = {}
            for name in names:
                reference, _, _ = peer_reference(ranked[name], len(prior), artist, old is not None,
                                                  top_n, minimum, floor)
                scores[name] = normalize(current[name], reference)
            for age in ('Young', 'Adult'):
                a, b = scores['reg' + age], scores['join' + age]
                if a is not None and b is not None:
                    differences[age].append(abs(a - b))
                    if current['reg' + age] > 0 or current['join' + age] > 0:
                        active_differences[age].append(abs(a - b))
        previous, previous_day = updated, day

    with source.open(newline='', encoding='utf-8-sig') as stream:
        reader = csv.DictReader(stream)
        if not set(columns).issubset(reader.fieldnames or ()):
            raise ValueError('Source lacks TV conversion measurements')
        for row in reader:
            day = parse_date(row['date'])
            if current_day is not None and day != current_day:
                if day <= current_day:
                    raise ValueError('Input weeks must be ordered')
                process(current_day, acts)
                acts = {}
            current_day = day
            values = acts.setdefault(row['artistId'], [0.] * 4)
            for i, column in enumerate(columns):
                values[i] += float(row[column])
        if current_day is not None:
            process(current_day, acts)
    result = {'sourceSha256': file_digest(source), 'parameters': {'halfLifeWeeks': half_life, 'topN': top_n,
              'minimumPeers': minimum, 'floorInEachInputUnit': floor, 'evaluationStart': cutoff.isoformat()},
              'ages': {age: {'artistWeeksCompared': len(differences[age]),
                  'absoluteDifference': {f'p{p}': percentile(differences[age], p / 100) for p in (0, 50, 90, 99, 100)},
                  'artistWeeksDifferenceOverPoint1': sum(x > .1 for x in differences[age]),
                  'artistWeeksWithSmoothedTvActivity': len(active_differences[age]),
                  'activeAbsoluteDifference': {f'p{p}': percentile(active_differences[age], p / 100) for p in (0, 50, 90, 99, 100)}} for age in ('Young', 'Adult')},
              'caveat': 'AI-roster observations only, with no artist training/holdout split; same numerical floor in different raw units, so floors have different physical meanings. Scores are clipped and lagged; this measures input sensitivity, not band changes, route attribution or causal effects.'}
    with output.open('x', encoding='utf-8') as stream:
        json.dump(result, stream, indent=2, sort_keys=True)
        stream.write('\n')


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('source', type=Path)
    p.add_argument('output', type=Path)
    p.add_argument('--evaluation-start', default='1960-07-01')
    args = p.parse_args()
    compare(args.source, args.output, evaluation_start=args.evaluation_start)
