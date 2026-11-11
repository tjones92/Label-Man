"""Replay declared release-geography rules against observed standing inputs."""
import argparse
import itertools
import json
import math
import re
from datetime import date as calendar_date
from collections import Counter, defaultdict
from pathlib import Path

from partition_opening_following_routes import maximum, optional, policy, selected_shadow
from stature_normalization import file_digest, parse_date
from stature_shadow_policy import BANDS, evaluate
from trace_stature_marketing_mechanisms import rows

VARIANTS = ('neutral', 'home_only', 'home_count_average', 'crossover_national')
ORDER = ('BelowRising', *BANDS)
STATUS = {'observed': 'Observed', 'below_rising': 'BelowRising', 'missing_evidence': 'MissingEvidence'}
MULTIPLIERS = {'Superstar': 2.5, 'Star': 2, 'Established': 1.5, 'Rising': 1.2, 'NewSigning': .8}


def close(a, b):
    return math.isclose(a, b, rel_tol=2e-6, abs_tol=.01)


def aliases(scene):
    result = {}
    for block in scene.read_text(encoding='utf-8').split('[sub_resource '):
        identity = re.search(r'^regionId = "([^"]+)"$', block, re.M)
        name = re.search(r'^regionName = "([^"]+)"$', block, re.M)
        if identity and name:
            result[identity[1]] = result[name[1]] = identity[1]
    return result


def allocation(release, population, names, variant):
    if variant == 'neutral':
        total = sum(population.values())
        return {r: p / total for r, p in population.items()}
    origin = names.get(release['origin'])
    count = int(release['markets'])
    if origin not in population or not 1 <= count <= len(population):
        raise ValueError('Unresolved origin or invalid market count')
    if variant == 'crossover_national' and 0 < int(release['peakPosition']) <= 40:
        return allocation(release, population, names, 'neutral')
    if variant == 'home_only':
        return {r: float(r == origin) for r in population}
    if variant not in ('home_count_average', 'crossover_national'):
        raise ValueError('Unknown geography rule')
    footprints = list(itertools.combinations([r for r in population if r != origin], count - 1))
    weights = dict.fromkeys(population, 0.)
    for others in footprints:
        footprint = (origin, *others)
        total = sum(population[r] for r in footprint)
        for region in footprint:
            weights[region] += population[region] / total / len(footprints)
    return weights


def profiles(releases, population, names, variant):
    weights, totals = defaultdict(lambda: dict.fromkeys(population, 0.)), Counter()
    for release in releases:
        distribution = allocation(release, population, names, variant)
        for group, amount in ((str(release['year']), float(release['seedYoung'])), ('adult', float(release['seedAdult']))):
            totals[group] += amount
            for region in population:
                weights[group][region] += amount * distribution[region]
    return {group: {r: value / totals[group] for r, value in distribution.items()}
            for group, distribution in weights.items() if totals[group] > 0}


def relocate(regions, profile, neutral=False):
    if neutral:
        return {r['region']: (optional(r['followingYoung']), optional(r['followingAdult'])) for r in regions}
    cohort_totals, adult = Counter(), 0.
    for region in regions:
        cohorts = json.loads(region['openingYoungByYear'])
        if not close(sum(cohorts.values()), float(region['openingYoung'])):
            raise ValueError('Young cohort source does not match regional total')
        for year, value in cohorts.items():
            cohort_totals[year] += value
        adult += float(region['openingAdult'])
    result = {}
    for region in regions:
        identity = region['region']
        new_young = sum(value * profile[year][identity] for year, value in cohort_totals.items() if value > 0)
        new_adult = adult * profile['adult'][identity] if adult > 0 else 0
        young, old_adult = optional(region['followingYoung']), optional(region['followingAdult'])
        source_young, source_adult = float(region['openingYoung']), float(region['openingAdult'])
        if young is not None and source_young > young + max(.01, young * 2e-6):
            raise ValueError('Young source exceeds actual stock')
        if old_adult is not None and source_adult > old_adult + max(.01, old_adult * 2e-6):
            raise ValueError('Adult source exceeds actual stock')
        result[identity] = (None if young is None else young - source_young + new_young,
                            None if old_adult is None else old_adult - source_adult + new_adult)
    for index, field in enumerate(('followingYoung', 'followingAdult')):
        if all(r[field] != '' for r in regions):
            if not math.isclose(sum(v[index] for v in result.values()), sum(float(r[field]) for r in regions), rel_tol=1e-10, abs_tol=1e-7):
                raise ValueError('Relocation did not conserve national stock')
    return result


def derive(snapshot, regions, stock, reference):
    population = sum(float(r['populationMillions']) for r in regions)
    draws, records, conversions = [], [], []
    for region in regions:
        young, adult = stock[region['region']]
        pop = float(region['populationMillions'])
        density = None if young is None or adult is None else min(1, max(0, young + adult) / pop / reference)
        response = optional(region['salesResponse'])
        draws.append(density)
        conversions.append(None if density is None else density * pop)
        records.append(None if density is None or response is None else max(density, response) * pop)
    draw = maximum(draws)
    record = sum(records) / population if all(v is not None for v in records) else None
    conversion = sum(conversions) / population if all(v is not None for v in conversions) else None
    following = maximum([optional(snapshot['following' + age + 'Score']) for age in ('Young', 'Adult')])
    return {'regional': {'response': optional(snapshot['regionalResponse']), 'draw': draw, 'reach': record},
        'record': {'response': maximum([optional(snapshot[k + 'Score']) for k in ('singleYoung', 'singleAdult', 'albumYoung', 'albumAdult')]), 'draw': following, 'reach': record},
        'conversion': {'response': maximum([optional(snapshot[k + 'Score']) for k in ('tvYoung', 'tvAdult')]), 'draw': following, 'reach': conversion}}


def analyze(directory, name, config, scene):
    census = list(rows(directory, name, 'opening-following-census'))
    artist_names = {r['artistId']: r['artist'] for r in census}
    selected = {r['artistId'] for r in census if r['openingArchetype'] == 'RegionalName'}
    releases = list(rows(directory, name, 'opening-geography-releases'))
    by_artist = defaultdict(list)
    for release in releases:
        by_artist[release['artistId']].append(release)
    for artist in {r['artistId'] for r in census}:
        for age in ('Young', 'Adult'):
            if not close(sum(float(r['seed' + age]) for r in census if r['artistId'] == artist),
                         sum(float(r['seed' + age]) for r in by_artist[artist])):
                raise ValueError('Release contributions do not reconstruct opening census')
    regional = defaultdict(list)
    for r in rows(directory, name, 'opening-geography-regions'):
        regional[parse_date(r['date']).isoformat(), r['artistId']].append(r)
    first = next(iter(regional.values()))
    population = {r['region']: float(r['populationMillions']) for r in first}
    names = aliases(scene)
    profiles_by = {(v, artist): profiles(by_artist[artist], population, names, v) for v in VARIANTS for artist in selected}
    previous, observed, observations, affected = {}, {}, defaultdict(Counter), defaultdict(set)
    weekly, seen = [], set()
    last_dates = {}
    for s in selected_shadow(directory, name, selected):
        key = s['snapshotDate'], s['artistId']
        if not key[0]:
            continue
        if key not in regional or key in seen:
            raise ValueError('Missing or duplicate geography observation')
        seen.add(key)
        date, artist = key
        day = calendar_date.fromisoformat(date)
        if artist in last_dates and (day - last_dates[artist]).days != 7:
            raise ValueError('Nonconsecutive geography observations')
        last_dates[artist] = day
        regions = regional[key]
        if len(regions) != len(population) or {r['region'] for r in regions} != population.keys():
            raise ValueError('Missing or duplicate region')
        for v in VARIANTS:
            stock = relocate(regions, profiles_by[v, artist], v == 'neutral')
            e = derive(s, regions, stock, config['FollowingPerMillionReference'])
            result = evaluate(e, policy(config), date, previous.get((v, artist)))
            band, status = result['band'] or 'BelowRising', STATUS[result['status']]
            if status != 'MissingEvidence':
                previous[v, artist] = result['band']
            if v == 'neutral':
                if status != s['status'] or status != 'MissingEvidence' and band != s['shadowBand']:
                    raise ValueError('Neutral geography band/status reconstruction differs')
                for field, value in (('regionalDraw', e['regional']['draw']), ('recordReach', e['record']['reach']), ('conversionReach', e['conversion']['reach'])):
                    expected = optional(s[field])
                    if (expected is None) != (value is None) or expected is not None and not math.isclose(expected, value, abs_tol=1e-10, rel_tol=1e-10):
                        raise ValueError('Neutral geography input reconstruction differs: ' + field)
            observed[v, date, artist] = band, status
            c = observations[day.year, v]; c['observations'] += 1
            for label, actual_field, value in (
                    ('draw', 'regionalDraw', e['regional']['draw']),
                    ('recordReach', 'recordReach', e['record']['reach']),
                    ('conversionReach', 'conversionReach', e['conversion']['reach'])):
                actual_value = optional(s[actual_field])
                if value is not None and actual_value is not None:
                    c[label + 'Higher'] += value > actual_value + 1e-10
                    c[label + 'Lower'] += value < actual_value - 1e-10
            if status == 'MissingEvidence' or s['status'] == 'MissingEvidence':
                c['missingComparisons'] += 1
            else:
                delta = ORDER.index(band) - ORDER.index(s['shadowBand'])
                c['higherBand'] += delta > 0; c['lowerBand'] += delta < 0
                if delta:
                    affected[day.year, v].add(artist)
            weekly.append(dict(date=date, artistId=artist, variant=v, actualBand=s['shadowBand'], band=band,
                status=status, regionalDraw=e['regional']['draw'], recordReach=e['record']['reach'], conversionReach=e['conversion']['reach']))
    if seen != regional.keys():
        raise ValueError('Unmatched regional observations')
    budgets, changed, budget_acts = defaultdict(Counter), [], defaultdict(set)
    for r in rows(directory, name, 'opening-following-marketing'):
        if r['artistId'] not in selected or r['context'] != 'release-launch' or r['status'] != 'Available':
            continue
        year = parse_date(r['date']).year
        for v in VARIANTS:
            band, status = observed[v, r['snapshotDate'], r['artistId']]
            c = budgets[year, v]
            if status == 'MissingEvidence':
                c['missingComparisons'] += 1; continue
            career = band if band != 'BelowRising' else 'NewSigning' if r['legacyCareer'] == 'NewSigning' else 'Declining'
            budget = float(r['legacyBudget']) * MULTIPLIERS.get(career, .5) / MULTIPLIERS.get(r['legacyCareer'], .5)
            actual = float(r['actualProposedBudget'])
            c['availableReads'] += 1
            delta = budget - actual
            tolerance = max(.001, actual * 2e-6)
            c['higherEnvelope'] += delta > tolerance; c['lowerEnvelope'] += delta < -tolerance
            if v == 'neutral' and abs(delta) > tolerance:
                raise ValueError('Neutral budget reconstruction differs')
            if abs(delta) > tolerance:
                budget_acts[year, v].add(r['artistId'])
                changed.append({**r, 'artist': artist_names[r['artistId']], 'variant': v, 'geographyBand': band, 'geographyBudget': budget, 'delta': delta})
    target_releases = [r for r in releases if r['artistId'] in selected]
    summary = {'qualifyingReleases': len(releases), 'regionalQualifyingReleases': len(target_releases),
        'regionalMarketCounts': dict(Counter(r['markets'] for r in target_releases)),
        'regionalFormats': dict(Counter(r['format'] for r in target_releases)),
        'regionalTop40Releases': sum(0 < int(r['peakPosition']) <= 40 for r in target_releases),
        'regionalTop40Formats': dict(Counter(r['format'] for r in target_releases if 0 < int(r['peakPosition']) <= 40)),
        'regionalTop40MarketCounts': dict(Counter(r['markets'] for r in target_releases if 0 < int(r['peakPosition']) <= 40)),
        'regionalTop40Stock': sum(float(r['seedYoung']) + float(r['seedAdult']) for r in target_releases if 0 < int(r['peakPosition']) <= 40),
        'countOnlyReleases': sum(not r['origin'] for r in releases)}
    return {'releaseCensus': summary, 'neutralMatchesEveryCSharpObservation': True, 'nationalStockConserved': True,
        'bandObservations': [{'year': year, 'variant': v, **c, 'distinctChangedActs': len(affected[year, v])} for (year, v), c in sorted(observations.items())],
        'launchBudgetReads': [{'year': year, 'variant': v, **c, 'distinctChangedActs': len(budget_acts[year, v])} for (year, v), c in sorted(budgets.items())],
        'changedLaunches': changed, 'weekly': weekly}


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--prefix', required=True)
    p.add_argument('--seeds', type=int, nargs='+', default=[1001, 2002])
    p.add_argument('--directory', type=Path, default=Path('SimLogs'))
    p.add_argument('--output', type=Path, required=True)
    a = p.parse_args()
    config_path, scene = Path('SimTools/StatureShadowExperimental.json'), Path('chart_manager.tscn')
    config = json.loads(config_path.read_text())
    report = {'analysisSha256': file_digest(Path(__file__)), 'calibrationSha256': file_digest(config_path),
        'sceneSha256': file_digest(scene), 'protocolSha256': file_digest(Path('SimTools/StatureOpeningGeographyProtocol.md')),
        'helperSha256': {name: file_digest(Path('SimTools') / name) for name in
                        ('partition_opening_following_routes.py', 'stature_normalization.py', 'stature_shadow_policy.py')},
        'scope': 'Standing-only frozen-world geography shadows; averaged allocation is not expected standing. Adult source uses the initial adult release mixture, including later aged transfers. Actual audience, national scores, observed sales/TV/references, and paid budgets are fixed. No causal economic effects.', 'seeds': {}}
    provenance = None
    for seed in a.seeds:
        worlds = {}
        for arm in ('control', 'treatment'):
            name = f'{a.prefix}-{seed}-{arm}'
            meta = a.directory / 'writing-calibration'
            request_path = meta / f'{name}-request.json'
            request = json.loads(request_path.read_text(encoding='utf-8-sig'))
            weeks = next(int(f.split('=', 1)[1]) for f in request['effectiveArguments'] if f.startswith('--weeks='))
            marker = f'CHART_AUDIT_COMPLETE run={name} weeks={weeks}'
            if marker not in (meta / f'{name}.log').read_text().splitlines() or (meta / f'{name}-errors.log').stat().st_size:
                raise ValueError('Run did not finish cleanly: ' + name)
            frozen = {k: request[k] for k in ('assemblySha256', 'engineSha256', 'calibrationSha256', 'sourceContentSha256')}
            if provenance is not None and frozen != provenance:
                raise ValueError('World provenance differs')
            provenance = frozen
            worlds[arm] = analyze(a.directory, name, config, scene)
            worlds[arm]['requestSha256'] = file_digest(request_path)
            worlds[arm]['outputManifestSha256'] = file_digest(meta / f'{name}-outputs.json')
        for suffix in ('opening-following-census', 'opening-geography-releases'):
            if list(rows(a.directory, f'{a.prefix}-{seed}-control', suffix)) != list(rows(a.directory, f'{a.prefix}-{seed}-treatment', suffix)):
                raise ValueError('Paired censuses differ: ' + suffix)
        report['seeds'][str(seed)] = worlds
    with a.output.open('x', encoding='utf-8') as stream:
        json.dump(report, stream, indent=2); stream.write('\n')


if __name__ == '__main__':
    main()
