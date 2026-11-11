"""Summarize diagnostic following joins and TV registration estimates by week.

Joins are the following model's proportional, post-saturation attribution;
they are not independently observed people or a causal estimate.
"""
import argparse
import csv
import json
from collections import defaultdict
from pathlib import Path

from stature_normalization import file_digest, parse_date
from verify_stature_evidence import verify


def summarize(source, output):
    rows, weeks, artists = verify(source)
    fields = tuple(source + 'Join' + age for source in ('buy', 'tv', 'press', 'series', 'live') for age in ('Young', 'Adult'))
    totals = defaultdict(lambda: defaultdict(float))
    active = defaultdict(set)
    with source.open(newline='', encoding='utf-8-sig') as stream:
        reader = csv.DictReader(stream)
        if not set(fields).issubset(reader.fieldnames or ()):
            raise ValueError('Source does not contain booked-join measurements')
        for row in reader:
            date = parse_date(row['date']).isoformat()
            for field in (*fields, 'tvRegisteredYoungEstimate', 'tvRegisteredAdultEstimate'):
                value = float(row[field])
                totals[date][field] += value
                if value > 0:
                    active[(date, field)].add(row['artistId'])
    per_week = []
    for date, values in sorted(totals.items()):
        per_week.append({'date': date, 'totals': dict(sorted(values.items())),
                         'artistsWithPositiveValue': {field: len(active[(date, field)]) for field in values}})
    result = {'sourceSha256': file_digest(source), 'rows': rows, 'weeks': weeks, 'artists': artists,
              'weekly': per_week,
              'caveat': 'AI-label roster rows only. Registrations are estimated fresh ordinary-TV viewers; joins are model-booked, source-attributed after shared saturation and before pruning. TV and series are separate; following and derived intents are not added to join counts.'}
    with output.open('x', encoding='utf-8') as stream:
        json.dump(result, stream, indent=2, sort_keys=True)
        stream.write('\n')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source', type=Path)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    summarize(args.source, args.output)
