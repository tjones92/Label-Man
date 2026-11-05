"""Freeze an exploratory first-week reference on a disjoint artist partition.

Fits are descriptive plumbing candidates, not recommendations or policy.
Evaluation must exclude the fit artist IDs, including across seeds.
"""
import argparse
import csv
import datetime as dt
import hashlib
import json
from pathlib import Path

from stature_normalization import COMPONENTS, SALES, file_digest, parse_date


def is_fit(artist):
    return hashlib.sha256(artist.encode('utf-8')).digest()[0] < 128


def percentile(values, p):
    values = sorted(values)
    if not values:
        return 0.0
    index = (len(values) - 1) * p
    lo, hi = int(index), min(int(index) + 1, len(values) - 1)
    return values[lo] + (index - lo) * (values[hi] - values[lo])


def fit(source, destination):
    acts, markets, local_sales, local_density = {}, {}, [], []
    day = None
    with source.open(newline='', encoding='utf-8-sig') as stream:
        for row in csv.DictReader(stream):
            current = parse_date(row['date'])
            if day is None:
                day = current
            if current != day:
                break
            region = row['regionId']
            denominators = [float(row['market' + k[0].upper() + k[1:]]) for k in SALES]
            if region in markets and markets[region] != denominators:
                raise ValueError('Inconsistent regional markets')
            markets[region] = denominators
            if not is_fit(row['artistId']):
                continue
            act = acts.setdefault(row['artistId'], {'sales': [0.] * 4, 'tv': [0.] * 2, 'following': [0.] * 2,
                                                     'observed': row['followingStatus'] == 'observed'})
            sales = [float(row[k]) for k in SALES]
            for i in range(4):
                act['sales'][i] += sales[i]
                if denominators[i] > 0 and sales[i] > 0:
                    local_sales.append(sales[i] / denominators[i])
            for i, age in enumerate(('Young', 'Adult')):
                act['tv'][i] += float(row['tvRegistered' + age + 'Estimate'])
                act['following'][i] += float(row['following' + age])
            pop = float(row['regionPopulationMillions'])
            if act['observed'] and pop > 0:
                local_density.append((float(row['followingYoung']) + float(row['followingAdult'])) / pop)
    if not acts or not markets:
        raise ValueError('No fit data')
    totals = [sum(v[i] for v in markets.values()) for i in range(4)]
    samples = {key: [] for key in COMPONENTS}
    for act in acts.values():
        for i, key in enumerate(COMPONENTS[:4]):
            if totals[i] > 0:
                samples[key].append(act['sales'][i] / totals[i])
        for i, key in enumerate(COMPONENTS[4:6]):
            samples[key].append(act['tv'][i])
        if act['observed']:
            for i, key in enumerate(COMPONENTS[6:]):
                samples[key].append(act['following'][i])
    floors = {k: 1e-6 if k.endswith('_share') else 1 for k in COMPONENTS}
    effective = (day + dt.timedelta(days=7)).isoformat()
    ref = {'version': 'exploratory-first-week-fit-p95-v1', 'effectiveDate': effective,
           'values': {k: max(floors[k], percentile(samples[k], .95)) for k in COMPONENTS}, 'denominatorFloors': floors}
    regional = {'version': 'exploratory-first-week-fit-p90-v1', 'effectiveDate': effective, 'halfLifeWeeks': 4,
                'localSalesShareReference': max(1e-6, percentile(local_sales, .90)),
                'followingPerMillionReference': max(1, percentile(local_density, .90)), 'marketFloorUnits': 100}
    manifest = {'sourceSha256': file_digest(source), 'sourceWeek': day.isoformat(), 'effectiveDate': effective,
                'fitArtistCount': len(acts), 'partition': 'SHA256(artistId)[0] < 128; evaluate complementary IDs only',
                'samples': {k: len(v) for k, v in samples.items()}, 'localSalesNonzeroSamples': len(local_sales),
                'localDensitySamples': len(local_density), 'caveat': 'Exploratory first-week frozen reference, not calibrated policy.'}
    destination.mkdir(parents=True, exist_ok=True)
    for filename, data in [('reference.json', ref), ('regional.json', regional), ('manifest.json', manifest)]:
        with (destination / filename).open('x', encoding='utf-8') as out:
            json.dump(data, out, indent=2, sort_keys=True); out.write('\n')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source', type=Path)
    parser.add_argument('destination', type=Path)
    args = parser.parse_args()
    fit(args.source, args.destination)
