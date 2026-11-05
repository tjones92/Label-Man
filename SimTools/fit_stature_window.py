"""Fit an exploratory frozen reference on a disjoint partition and past weeks."""
import argparse
import csv
import datetime as dt
import json
from pathlib import Path

from fit_stature_experiment import is_fit, percentile
from stature_normalization import COMPONENTS, SALES, file_digest, parse_date


def fit(source, destination, weeks):
    if weeks < 1:
        raise ValueError('Positive fit window required')
    samples = {key: [] for key in COMPONENTS}
    local_sales, local_density, artists = [], [], set()
    day, last_day, acts, markets, count = None, None, {}, {}, 0

    def flush():
        totals = [sum(v[i] for v in markets.values()) for i in range(4)]
        for act in acts.values():
            for i, key in enumerate(COMPONENTS[:4]):
                if totals[i] > 0:
                    samples[key].append(act['sales'][i] / totals[i])
            for i, key in enumerate(COMPONENTS[4:6]):
                samples[key].append(act['tv'][i])
            if act['observed']:
                for i, key in enumerate(COMPONENTS[6:]):
                    samples[key].append(act['following'][i])

    with source.open(newline='', encoding='utf-8-sig') as stream:
        for row in csv.DictReader(stream):
            current = parse_date(row['date'])
            if day is None:
                day = current
            elif current != day:
                if (current - day).days != 7:
                    raise ValueError('Training weeks must be consecutive')
                flush()
                count += 1
                last_day = day
                if count == weeks:
                    break
                day, acts, markets = current, {}, {}
            region = row['regionId']
            denominators = [float(row['market' + k[0].upper() + k[1:]]) for k in SALES]
            if region in markets and markets[region] != denominators:
                raise ValueError('Inconsistent market')
            markets[region] = denominators
            artist = row['artistId']
            if not is_fit(artist):
                continue
            artists.add(artist)
            act = acts.setdefault(artist, {'sales': [0.] * 4, 'tv': [0.] * 2, 'following': [0.] * 2,
                                            'observed': row['followingStatus'] == 'observed'})
            sales = [float(row[k]) for k in SALES]
            for i in range(4):
                act['sales'][i] += sales[i]
                if sales[i] > 0 and denominators[i] > 0:
                    local_sales.append(sales[i] / denominators[i])
            for i, age in enumerate(('Young', 'Adult')):
                act['tv'][i] += float(row['tvRegistered' + age + 'Estimate'])
                act['following'][i] += float(row['following' + age])
            pop = float(row['regionPopulationMillions'])
            if act['observed'] and pop > 0:
                local_density.append((float(row['followingYoung']) + float(row['followingAdult'])) / pop)
        else:
            if day is not None:
                flush(); count += 1; last_day = day
    if count != weeks or not artists:
        raise ValueError('Input does not contain requested complete fit window')
    # A nonzero 90th percentile is an explicit sparse-response alternative;
    # preserve its sample counts and retain a hard minimum of 30 observations.
    positive = {key: [v for v in vals if v > 0] for key, vals in samples.items()}
    if any(len(v) < 30 for v in positive.values()) or len(local_sales) < 30 or len(local_density) < 30:
        raise ValueError('Sparse fit: fewer than 30 positive samples in a component')
    floors = {k: 1e-6 if k.endswith('_share') else 1 for k in COMPONENTS}
    effective = (last_day + dt.timedelta(days=7)).isoformat()
    ref = {'version': f'exploratory-{weeks}week-nonzero-p90-v1', 'effectiveDate': effective,
           'values': {k: max(floors[k], percentile(positive[k], .9)) for k in COMPONENTS}, 'denominatorFloors': floors}
    regional = {'version': f'exploratory-{weeks}week-regional-p90-v1', 'effectiveDate': effective, 'halfLifeWeeks': 4,
                'localSalesShareReference': max(1e-6, percentile(local_sales, .9)),
                'followingPerMillionReference': max(1, percentile(local_density, .9)), 'marketFloorUnits': 100}
    regional_p99 = dict(regional, version=f'exploratory-{weeks}week-regional-p99-v1',
                        localSalesShareReference=max(1e-6, percentile(local_sales, .99)),
                        followingPerMillionReference=max(1, percentile(local_density, .99)))
    manifest = {'sourceSha256': file_digest(source), 'fitWeeks': weeks, 'lastFitWeek': last_day.isoformat(),
                'effectiveDate': effective, 'fitArtistCount': len(artists), 'sampleCounts': {k: len(v) for k, v in samples.items()},
                'positiveCounts': {k: len(v) for k, v in positive.items()}, 'localSalesNonzeroSamples': len(local_sales),
                'localDensitySamples': len(local_density), 'partition': 'SHA256(artistId)[0] < 128; evaluate complementary IDs only',
                'caveat': 'Exploratory fixed reference using positive raw weekly observations; not a selected policy.'}
    destination.mkdir(parents=True, exist_ok=True)
    for name, obj in [('reference.json', ref), ('regional.json', regional), ('regional-p99.json', regional_p99), ('manifest.json', manifest)]:
        with (destination / name).open('x', encoding='utf-8') as out:
            json.dump(obj, out, indent=2, sort_keys=True); out.write('\n')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source', type=Path)
    parser.add_argument('destination', type=Path)
    parser.add_argument('--weeks', type=int, required=True)
    args = parser.parse_args()
    fit(args.source, args.destination, args.weeks)
