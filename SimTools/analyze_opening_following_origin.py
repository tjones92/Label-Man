"""Validate and summarize read-only opening-stock provenance diagnostics."""
import argparse
import datetime as dt
import json
import math
from collections import Counter, defaultdict
from pathlib import Path

from trace_stature_marketing_mechanisms import rows
from stature_normalization import file_digest, parse_date

BANDS = ('BelowRising', 'Rising', 'Established', 'Star', 'Superstar')
NEW_SUFFIXES = {'opening-following-census.csv', 'opening-following-shadow.csv', 'opening-following-marketing.csv'}


def number(row, key):
    value = float(row[key])
    if not math.isfinite(value) or value < 0:
        raise ValueError('Invalid nonnegative source measurement: ' + key)
    return value


def verify_neutrality(directory, prefix, baseline, seed):
    metadata = directory / 'writing-calibration'
    result = {}
    for arm in ('control', 'treatment'):
        manifests = []
        for p in (baseline, prefix):
            name = f'{p}-{seed}-{arm}'
            manifest = json.loads((metadata / f'{name}-outputs.json').read_text(encoding='utf-8-sig'))
            manifests.append({r['name'][len(name) + 1:]: r['sha256'].upper() for r in manifest})
        old, new = manifests
        if set(new) - set(old) != NEW_SUFFIXES or set(old) - set(new):
            raise ValueError('Unexpected observation output additions')
        if any(new[suffix] != digest for suffix, digest in old.items()):
            raise ValueError('Provenance changed an existing output: ' + arm)
        result[arm] = {'byteIdenticalOriginalCsvs': len(old), 'newDiagnosticCsvs': len(NEW_SUFFIXES)}
    return result


def analyze_world(directory, name):
    census = list(rows(directory, name, 'opening-following-census'))
    if not census:
        raise ValueError('Empty opening census')
    origins, archetypes, seen = defaultdict(Counter), defaultdict(Counter), set()
    for r in census:
        key = r['artistId'], r['region']
        if key in seen:
            raise ValueError('Duplicate opening census key')
        seen.add(key)
        for age in ('Young', 'Adult'):
            value = number(r, 'seed' + age)
            origins[r['artistId']]['seed' + age] += value
            archetypes[r['openingArchetype']]['seed' + age] += value
        origins[r['artistId']]['artist'] = r['artist']
        origins[r['artistId']]['archetype'] = r['openingArchetype']
    actual, dates, totals = {}, {}, defaultdict(Counter)
    bands, band_artists, previous_origin = defaultdict(Counter), defaultdict(set), {}
    for r in rows(directory, name, 'opening-following-shadow'):
        day, artist = parse_date(r['date']), r['artistId']
        if artist not in origins:
            raise ValueError('Diagnostic includes an act absent from opening census')
        if artist in dates and (day - dates[artist]).days != 7:
            raise ValueError('Nonconsecutive provenance observations')
        dates[artist] = day
        for age in ('Young', 'Adult'):
            current, tagged = number(r, 'following' + age), number(r, 'opening' + age)
            if tagged > current + max(.01, current * 2e-6):
                raise ValueError('Source exceeds observed stock')
            totals[day.isoformat()]['following' + age] += current
            totals[day.isoformat()]['opening' + age] += tagged
        actual[day.isoformat(), artist] = r
        source = float(r['openingYoung']) + float(r['openingAdult'])
        prior = previous_origin.get(artist, origins[artist]['seedYoung'] + origins[artist]['seedAdult'])
        if source > prior + max(.1, prior * 2e-6):
            raise ValueError('Original source stock increased without a source inflow')
        previous_origin[artist] = source
        if r['actualStatus'] not in ('Observed', 'BelowRising'):
            continue
        for variant in ('half', 'zero'):
            c = bands[day.year, variant]
            c['availableActualObservations'] += 1
            if r[variant + 'OpeningStatus'] == 'MissingEvidence':
                c['sensitivityMissing'] += 1
                continue
            c['comparableObservations'] += 1
            delta = BANDS.index(r[variant + 'OpeningBand']) - BANDS.index(r['actualBand'])
            if delta > 0:
                raise ValueError('Removing observed source raised a comparable band')
            c['lowerBand'] += delta < 0
            if delta < 0:
                band_artists[day.year, variant].add(artist)
    by_date = {}
    for date, c in sorted(totals.items()):
        c['openingTotal'] = c['openingYoung'] + c['openingAdult']
        c['followingTotal'] = c['followingYoung'] + c['followingAdult']
        c['openingShareOfSeededActsFollowing'] = c['openingTotal'] / c['followingTotal'] if c['followingTotal'] else None
        by_date[date] = dict(c)
    groups, affected, largest = defaultdict(Counter), defaultdict(set), []
    per_artist = defaultdict(Counter)
    for r in rows(directory, name, 'opening-following-marketing'):
        artist, day = r['artistId'], parse_date(r['date'])
        old, proposed = number(r, 'legacyBudget'), number(r, 'actualProposedBudget')
        for variant in ('half', 'zero'):
            budget = number(r, variant + 'OpeningBudget')
            c = groups[day.year, r['context'], variant]
            c['allReads'] += 1
            if r['status'] != 'Available':
                if budget != old:
                    raise ValueError('Unavailable baseline did not retain legacy')
                continue
            key = r['snapshotDate'], artist
            if key not in actual:
                raise ValueError('Available decision lacks a provenance snapshot')
            snapshot = actual[key]
            c['availableActualReads'] += 1
            c['actualUplifts'] += proposed > old
            if not r[variant + 'OpeningBand']:
                c['sensitivityMissing'] += 1
                continue
            c['comparableReads'] += 1
            lower = budget < proposed - max(.001, proposed * 2e-6)
            c['lowerBudget'] += lower
            c['upliftReduced'] += proposed > old and lower
            c['upliftRemoved'] += proposed > old and budget <= old + max(.001, old * 2e-6)
            if lower:
                affected[day.year, r['context'], variant].add(artist)
            if r['context'] == 'release-launch':
                per_artist[artist, day.year, variant]['availableReads'] += 1
                per_artist[artist, day.year, variant]['uplifts'] += proposed > old
                per_artist[artist, day.year, variant]['upliftReduced'] += proposed > old and lower
                per_artist[artist, day.year, variant]['upliftRemoved'] += proposed > old and budget <= old + max(.001, old * 2e-6)
                if lower:
                    largest.append(dict(date=day.isoformat(), artistId=artist, artist=origins[artist]['artist'], variant=variant,
                        legacyBudget=old, proposedBudget=proposed, sensitivityBudget=budget,
                        openingYoung=snapshot['openingYoung'], openingAdult=snapshot['openingAdult']))
    seed_y = sum(r['seedYoung'] for r in origins.values())
    seed_a = sum(r['seedAdult'] for r in origins.values())
    return {'seededActs': len(origins), 'seedRegions': len(census), 'seedYoung': seed_y, 'seedAdult': seed_a, 'seedTotal': seed_y + seed_a,
        'census': census, 'archetypes': dict(archetypes), 'weeklySourceTotals': by_date,
        'bands': [{'year': y, 'variant': v, **c, 'distinctLowerBandActs': len(band_artists[y, v])} for (y, v), c in sorted(bands.items())],
        'budgetReads': [{'year': y, 'context': context, 'variant': v, **c, 'distinctLowerBudgetActs': len(affected[y, context, v])} for (y, context, v), c in sorted(groups.items())],
        'perActLaunches': [{'artistId': artist, 'artist': origins[artist]['artist'], 'year': year, 'variant': variant, **c} for (artist, year, variant), c in sorted(per_artist.items())],
        'largestLaunchBudgetSensitivities': sorted(largest, key=lambda r: r['proposedBudget'] - r['sensitivityBudget'], reverse=True)[:30]}


def run(directory, prefix, seeds, output, neutrality_baseline=None):
    metadata = directory / 'writing-calibration'
    report = {'prefix': prefix, 'analysisSha256': file_digest(Path(__file__)),
        'protocolSha256': file_digest(Path('SimTools/StatureOpeningOriginProtocol.md')),
        'calibrationSha256': file_digest(Path('SimTools/StatureShadowExperimental.json')), 'seeds': {},
        'scope': 'Read-only surviving original opening-stock accounting. Actual sales/TV and lagged peer references are fixed; regional following reach is recomputed. Half/zero variants evolve independent hysteresis. They do not supply actual budgets.',
        'limitations': 'Fresh uninterrupted runs only. Proportional source attribution under adult capacity caps is a convention. New followers and all indirect descendants of prewarm influence remain in evidence. Observation counts and repeated requested envelopes are not causal economic effects or paid spending.'}
    for seed in seeds:
        worlds, requests, provenance = {}, {}, {}
        for arm in ('control', 'treatment'):
            name = f'{prefix}-{seed}-{arm}'
            request = json.loads((metadata / f'{name}-request.json').read_text(encoding='utf-8-sig'))
            requests[arm] = request
            if '--observe-opening-following-origin' not in request['flags']:
                raise ValueError('Run lacks explicit provenance instrumentation')
            provenance[arm] = {suffix: file_digest(metadata / f'{name}-{suffix}.json') for suffix in ('request', 'outputs')}
            weeks = next(int(f.split('=', 1)[1]) for f in request['flags'] if f.startswith('--weeks='))
            if f'CHART_AUDIT_COMPLETE run={name} weeks={weeks}' not in (metadata / f'{name}.log').read_text(encoding='utf-8-sig').splitlines() or (metadata / f'{name}-errors.log').stat().st_size:
                raise ValueError('Run did not complete cleanly')
            worlds[arm] = analyze_world(directory, name)
        if worlds['control']['census'] != worlds['treatment']['census']:
            raise ValueError('Paired opening census differs')
        for field in ('assemblySha256', 'engineSha256', 'calibrationSha256', 'sourceContentSha256'):
            if requests['control'][field] != requests['treatment'][field]:
                raise ValueError('Unmatched paired inputs: ' + field)
        for arm, request in requests.items():
            if ('--stature-marketing-reader' in request['flags']) != (arm == 'treatment'):
                raise ValueError('Unexpected reader configuration')
        worlds['provenance'] = {'inputsMatched': True, 'filesSha256': provenance,
            'validatedPairSummarySha256': file_digest(metadata / f'{prefix}-{seed}-summary.json')}
        if neutrality_baseline:
            worlds['observationNeutrality'] = verify_neutrality(directory, prefix, neutrality_baseline, seed)
        report['seeds'][str(seed)] = worlds
    with output.open('x', encoding='utf-8') as f:
        json.dump(report, f, indent=2)
        f.write('\n')


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--directory', type=Path, default=Path('SimLogs'))
    p.add_argument('--prefix', required=True)
    p.add_argument('--seeds', type=int, nargs='+', default=[1001, 2002])
    p.add_argument('--output', type=Path, required=True)
    p.add_argument('--neutrality-baseline')
    a = p.parse_args()
    run(a.directory, a.prefix, a.seeds, a.output, a.neutrality_baseline)
