"""Offline experimental normalization of raw weekly stature evidence.

The caller must supply all parameters and a dated fixed reference. This produces
components, not standing bands or AI decisions. The peer reference uses the
preceding complete week's AI-roster distribution, excluding the evaluated act.
"""
import argparse
import csv
import datetime as dt
import hashlib
import json
import math
from pathlib import Path


COMPONENTS = ('single_young_share', 'single_adult_share', 'album_young_share', 'album_adult_share',
              'tv_registration_young', 'tv_registration_adult', 'following_young', 'following_adult')
SALES = ('singleYoungUnits', 'singleAdultUnits', 'albumYoungUnits', 'albumAdultUnits')


def parse_date(value):
    return dt.datetime.strptime(value, '%m/%d/%Y').date()


def smooth(previous, current, alpha):
    if current is None:
        return None
    return current if previous is None else alpha * current + (1 - alpha) * previous


def peer_references(previous, component, n):
    """Sort once, deterministically; callers remove self before taking the top N."""
    return sorted(((values[component], artist) for artist, values in previous.items()
                   if values[component] is not None), key=lambda item: (-item[0], item[1]))[:n + 1]


def peer_reference(ranked, peer_count, artist, own_present, n, minimum, floor):
    available = peer_count - int(own_present)
    if available < minimum:
        return None, available, 'insufficient_peers'
    peers = [value for value, key in ranked if key != artist][:n]
    if not peers:
        return None, available, 'insufficient_peers'
    return max(floor, sum(peers) / len(peers)), available, 'ok'


def normalize(value, denominator):
    return None if value is None or denominator is None else min(1.0, max(0.0, value / denominator))


def file_digest(path):
    digest = hashlib.sha256()
    with path.open('rb') as source:
        for block in iter(lambda: source.read(1024 * 1024), b''):
            digest.update(block)
    return digest.hexdigest()


def rows_by_week(path):
    """Stream sorted weekly traces, rejecting duplicates and incomplete region sets."""
    with path.open(newline='', encoding='utf-8-sig') as stream:
        current, acts, market, last = None, {}, {}, None
        for row in csv.DictReader(stream):
            date = parse_date(row['date'])
            if current is not None and date != current:
                if date <= current:
                    raise ValueError('Input weeks must be ordered and unique')
                yield current, acts, market
                last, acts, market = current, {}, {}
            if last is not None and date <= last:
                raise ValueError('Input weeks must be ordered and unique')
            current = date
            region, artist = row['regionId'], row['artistId']
            denominators = tuple(float(row['market' + key[0].upper() + key[1:]]) for key in SALES)
            if any(not math.isfinite(x) or x < 0 for x in denominators):
                raise ValueError('Invalid market denominator')
            if region in market and market[region] != denominators:
                raise ValueError('Inconsistent market denominator')
            market[region] = denominators
            if artist not in acts:
                acts[artist] = {'regions': set(), 'sales': [0.0] * 4, 'tv': [0.0, 0.0], 'following': [0.0, 0.0],
                                'status': row['followingStatus'], 'tier': row['labelTier'], 'genre': row['genre'],
                                'rung': row['careerState']}
            act = acts[artist]
            if region in act['regions']:
                raise ValueError('Duplicate act-region-week')
            if (row['followingStatus'], row['labelTier'], row['genre'], row['careerState']) != (act['status'], act['tier'], act['genre'], act['rung']):
                raise ValueError('Inconsistent act metadata within week')
            act['regions'].add(region)
            fields = [float(row[key]) for key in SALES]
            tv = [float(row['tvRegisteredYoungEstimate']), float(row['tvRegisteredAdultEstimate'])]
            following = [float(row['followingYoung']), float(row['followingAdult'])]
            if any(not math.isfinite(x) or x < 0 for x in fields + tv + following):
                raise ValueError('Invalid evidence value')
            if row['followingStatus'] not in ('observed', 'no_following_record'):
                raise ValueError('Invalid following status')
            if row['followingStatus'] == 'no_following_record' and any(following):
                raise ValueError('Absent following has nonzero population')
            for i, value in enumerate(fields):
                act['sales'][i] += value
            for i in range(2):
                act['tv'][i] += tv[i]
                act['following'][i] += following[i]
        if current is not None:
            yield current, acts, market


def candidates(path, reference, half_life, top_n, min_peers):
    if not math.isfinite(half_life) or half_life <= 0 or top_n < 1 or min_peers < 1:
        raise ValueError('Parameters must be positive')
    fixed = reference['values']
    floors = reference['denominatorFloors']
    if not reference.get('version') or set(fixed) != set(COMPONENTS) or set(floors) != set(COMPONENTS):
        raise ValueError('Reference needs a version, values and floors for each component')
    if any(not isinstance(fixed[k], (int, float)) or not math.isfinite(fixed[k]) or fixed[k] < 0 for k in COMPONENTS):
        raise ValueError('Fixed reference values must be finite and nonnegative')
    if any(not isinstance(floors[k], (int, float)) or not math.isfinite(floors[k]) or floors[k] <= 0 for k in COMPONENTS):
        raise ValueError('Denominator floors must be finite and positive')
    effective = dt.date.fromisoformat(reference['effectiveDate'])
    alpha = 1.0 - math.pow(0.5, 1.0 / half_life)
    previous, previous_date = {}, None
    for date, acts, market in rows_by_week(path):
        if not market:
            raise ValueError('No markets in week')
        for act in acts.values():
            if act['regions'] != set(market):
                raise ValueError('Incomplete regional coverage for an act')
        consecutive = previous_date is not None and (date - previous_date).days == 7
        prior = previous if consecutive else {}
        ranked = {key: peer_references(prior, key, top_n) for key in COMPONENTS}
        counts = {key: sum(v[key] is not None for v in prior.values()) for key in COMPONENTS}
        totals = [sum(value[i] for value in market.values()) for i in range(4)]
        next_state = {}
        for artist, act in sorted(acts.items()):
            if any(act['sales'][i] > totals[i] + max(0.01, totals[i] * 1e-6) for i in range(4)):
                raise ValueError('Act sales exceed the market denominator')
            raw = {key: act['sales'][i] / totals[i] if totals[i] > 0 else None for i, key in enumerate(COMPONENTS[:4])}
            raw.update(tv_registration_young=act['tv'][0], tv_registration_adult=act['tv'][1],
                       following_young=act['following'][0] if act['status'] == 'observed' else None,
                       following_adult=act['following'][1] if act['status'] == 'observed' else None)
            old = prior.get(artist)
            values = {key: smooth(old.get(key) if old else None, raw[key], alpha) if not key.startswith('following_') else raw[key]
                      for key in COMPONENTS}
            next_state[artist] = values
            for key in COMPONENTS:
                lagged, peers, reason = peer_reference(ranked[key], counts[key], artist,
                    old is not None and old[key] is not None, top_n, min_peers, floors[key])
                fixed_ref = max(floors[key], fixed[key]) if date >= effective else None
                yield {'date': date.isoformat(), 'artistId': artist, 'tier': act['tier'], 'genre': act['genre'],
                       'careerState': act['rung'], 'cohort': 'unavailable', 'component': key,
                       'raw': raw[key], 'smoothed': values[key], 'fixedReference': fixed_ref,
                       'fixedScore': normalize(values[key], fixed_ref), 'fixedReferenceVersion': reference['version'],
                       'fixedEffectiveDate': effective.isoformat(), 'peerReference': lagged,
                       'peerScore': normalize(values[key], lagged), 'peerReferenceDate': previous_date.isoformat() if consecutive else '',
                       'peerCount': peers, 'peerStatus': reason if consecutive else 'cold_start_or_week_gap',
                       'historyStatus': 'continued' if old is not None else 'new_or_gap',
                       'evidenceStatus': ('missing_following_record' if key.startswith('following_') and raw[key] is None else
                                          'missing_market' if raw[key] is None else 'observed')}
        previous, previous_date = next_state, date


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('input', type=Path)
    p.add_argument('--fixed-reference', type=Path, required=True)
    p.add_argument('--half-life-weeks', type=float, required=True)
    p.add_argument('--top-n', type=int, required=True)
    p.add_argument('--min-peers', type=int, required=True)
    p.add_argument('--output', type=Path, required=True)
    p.add_argument('--manifest', type=Path, required=True)
    args = p.parse_args()
    if not math.isfinite(args.half_life_weeks):
        p.error('Half-life must be finite')
    ref = json.loads(args.fixed_reference.read_text(encoding='utf-8-sig'))
    if args.output.exists() or args.manifest.exists():
        raise FileExistsError('Output and manifest must both be new files')
    records = candidates(args.input, ref, args.half_life_weeks, args.top_n, args.min_peers)
    first = next(records, None)
    if first is None:
        raise ValueError('Input contains no rows')
    with args.output.open('x', encoding='utf-8', newline='') as out:
        writer = csv.DictWriter(out, fieldnames=first.keys())
        writer.writeheader(); writer.writerow(first); writer.writerows(records)
    manifest = {'algorithm': 'offline-components-v2', 'inputSha256': file_digest(args.input),
                'fixedReferenceSha256': file_digest(args.fixed_reference), 'outputSha256': file_digest(args.output),
                'halfLifeWeeks': args.half_life_weeks, 'topN': args.top_n, 'minPeers': args.min_peers,
                'fixedReference': ref,
                'peerReference': 'previous week top-N mean, self excluded; deterministic artist-id ties',
                'caveats': 'AI-label roster only; modeled age split and TV registration estimate; no ranking policy'}
    with args.manifest.open('x', encoding='utf-8') as out:
        json.dump(manifest, out, indent=2, sort_keys=True); out.write('\n')


if __name__ == '__main__':
    main()
