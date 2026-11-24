"""Summarize live shadow scores by year, cohort proxy, tier and genre.

Runtime formation is identified from observed population events, not inferred
from chart appearances. Acts without such an event are explicitly unclassified.
"""
import argparse
import csv
import json
from collections import Counter, defaultdict
from pathlib import Path

from compare_stature_tv_conversion import percentile
from fit_stature_experiment import is_fit
from stature_normalization import file_digest, parse_date
from verify_stature_evidence import verify

COMPONENTS = ('singleYoungScore', 'singleAdultScore', 'albumYoungScore', 'albumAdultScore',
              'tvYoungScore', 'tvAdultScore', 'followingYoungScore', 'followingAdultScore')


def summarize(shadow, evidence, population, output):
    verify(evidence)
    formations = {}
    with population.open(newline='', encoding='utf-8-sig') as stream:
        for row in csv.DictReader(stream):
            if row['eventType'] == 'formation':
                date = parse_date(row['date'])
                artist = row['artistId']
                formations[artist] = min(formations.get(artist, date), date)
    roster = {}
    with evidence.open(newline='', encoding='utf-8-sig') as stream:
        for row in csv.DictReader(stream):
            key = (row['date'], row['artistId'])
            roster.setdefault(key, (row['labelTier'], row['genre']))
    counts, scores = defaultdict(Counter), defaultdict(list)
    matched = 0
    with shadow.open(newline='', encoding='utf-8-sig') as stream:
        for row in csv.DictReader(stream):
            artist, date = row['artistId'], parse_date(row['date'])
            info = roster.get((row['date'], artist))
            if info is None or row['status'] == 'warming_up':
                continue
            matched += 1
            cohort = 'observed_runtime_formation' if artist in formations and formations[artist] <= date else 'not_observed_formed_since_start'
            group = (str(date.year), cohort, info[0], info[1], 'fit' if is_fit(artist) else 'holdout')
            counts[group]['artistWeeks'] += 1
            counts[group]['status:' + row['status']] += 1
            if row['status'] in ('Observed', 'BelowRising'):
                counts[group]['band:' + row['shadowBand']] += 1
            for component in COMPONENTS:
                if row[component] != '':
                    value = float(row[component])
                    scores[(group, component)].append(value)
    groups = []
    for key, values in sorted(counts.items()):
        groups.append({'year': key[0], 'formationCohort': key[1], 'tier': key[2], 'genre': key[3], 'partition': key[4],
            'counts': dict(sorted(values.items())),
            'components': {component: {'n': len(scores[(key, component)]),
                'p50': percentile(scores[(key, component)], .5), 'p90': percentile(scores[(key, component)], .9),
                'p99': percentile(scores[(key, component)], .99),
                'clippedAtOne': sum(v >= 1 for v in scores[(key, component)])} for component in COMPONENTS}})
    if not matched:
        raise ValueError('No calibrated roster snapshots matched')
    result = {'shadowSha256': file_digest(shadow), 'evidenceSha256': file_digest(evidence),
              'populationEventsSha256': file_digest(population), 'matchedArtistWeeks': matched, 'groups': groups,
              'caveat': 'Roster snapshot rows only; runtime formation events identify new acts, but all others remain unclassified, including left-censored opening acts. Fit/holdout is an artist-ID partition, not an independent seed. Years may cover different numbers of calibrated weeks. Counts and score quantiles are descriptive artist-weeks, not decisions or causal effects.'}
    with output.open('x', encoding='utf-8') as stream:
        json.dump(result, stream, indent=2, sort_keys=True)
        stream.write('\n')


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('shadow', type=Path)
    p.add_argument('evidence', type=Path)
    p.add_argument('population', type=Path)
    p.add_argument('output', type=Path)
    a = p.parse_args()
    summarize(a.shadow, a.evidence, a.population, a.output)
