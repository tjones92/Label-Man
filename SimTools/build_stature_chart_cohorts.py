"""Derive first-observed artist singles-chart weeks from weekly record rows.

Week 0 is the chart present at run start. For later chart observations, an AI
decision may use the cohort only in a strictly later programme week.
"""
import argparse
import csv
import json
from pathlib import Path

from stature_normalization import file_digest


def build(records, population, output):
    formations, first = {}, {}
    with population.open(newline='', encoding='utf-8-sig') as stream:
        for row in csv.DictReader(stream):
            if row['eventType'] == 'formation' and row['artistId']:
                week = int(row['week'])
                formations[row['artistId']] = min(formations.get(row['artistId'], week), week)
    chart_rows = 0
    with records.open(newline='', encoding='utf-8-sig') as stream:
        for row in csv.DictReader(stream):
            artist = row['artistId']
            position = int(row['currentPosition'] or 0)
            if not artist or not 1 <= position <= 100:
                continue
            chart_rows += 1
            week = int(row['week'])
            candidate = (week, row['recordId'], int(row['year']))
            if artist not in first or candidate < first[artist]:
                first[artist] = candidate
    cohorts = []
    for artist, (week, record, year) in sorted(first.items()):
        formation = formations.get(artist)
        # Runtime formations in the same week can only be used from the next decision week.
        observed_formed = formation is not None and formation <= week
        cohorts.append({'artistId': artist, 'firstObservedChartWeek': week, 'firstObservedChartYear': year,
                        'availableFromDecisionWeek': 1 if week == 0 else week + 1,
                        'leftCensoredAtRunStart': week == 0, 'firstObservedRecordId': record,
                        'observedRuntimeFormationWeek': formation if observed_formed else None,
                        'preRunChartHistory': 'not_applicable_observed_runtime_formation' if observed_formed else 'unknown'})
    if not chart_rows:
        raise ValueError('No artist chart rows')
    result = {'recordsSha256': file_digest(records), 'populationSha256': file_digest(population),
              'observedChartRecordWeeks': chart_rows, 'cohorts': cohorts,
              'caveat': 'Singles-chart observations only. The first observed chart is not necessarily the first career chart. Run-start chart acts are left-censored; pre-run chart history is unknown without an observed runtime formation. Week-zero chart exists at run start; later completed-week chart observations become available to decisions in the following programme week.'}
    with output.open('x', encoding='utf-8') as stream:
        json.dump(result, stream, indent=2, sort_keys=True)
        stream.write('\n')


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('records', type=Path); p.add_argument('population', type=Path); p.add_argument('output', type=Path)
    a = p.parse_args(); build(a.records, a.population, a.output)
