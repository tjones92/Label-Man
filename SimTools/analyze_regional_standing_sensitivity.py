"""Isolate regional-route dependence in validated frozen geography replays."""
import argparse
import json
import math
from collections import Counter, defaultdict
from datetime import date as calendar_date
from pathlib import Path

from partition_opening_following_routes import maximum, optional, policy, selected_shadow
from replay_opening_geography import MULTIPLIERS, ORDER, STATUS, VARIANTS
from stature_normalization import file_digest, parse_date
from stature_shadow_policy import BANDS, evaluate
from trace_stature_marketing_mechanisms import rows
from compress_stature_outputs import csv_digest

MODES = ('full', 'without_regional', 'national_routes_for_star_plus')


def evidence(snapshot, weekly, mode):
    following = maximum([optional(snapshot['following' + age + 'Score']) for age in ('Young', 'Adult')])
    return {
        'regional': ({'response': optional(snapshot['regionalResponse']), 'draw': weekly['regionalDraw'],
                      'reach': weekly['recordReach']} if mode != 'without_regional' else dict(response=0., draw=0., reach=0.)),
        'record': dict(response=maximum([optional(snapshot[k + 'Score']) for k in
                       ('singleYoung', 'singleAdult', 'albumYoung', 'albumAdult')]),
                       draw=following, reach=weekly['recordReach']),
        'conversion': dict(response=maximum([optional(snapshot[k + 'Score']) for k in ('tvYoung', 'tvAdult')]),
                           draw=following, reach=weekly['conversionReach'])}


def evaluate_mode(inputs, calibration, date, previous, mode):
    result = evaluate(inputs, calibration, date, previous)
    if mode != 'national_routes_for_star_plus':
        return result
    disabled = evaluate({**inputs, 'regional': dict(response=0., draw=0., reach=0.)}, calibration, date, previous)
    for band in ('Star', 'Superstar'):
        result['bands'][band] = disabled['bands'][band]
    for band in reversed(BANDS):
        status = result['bands'][band]['status']
        if status == 'unknown':
            return dict(band=None, status='missing_evidence', bands=result['bands'])
        if status == 'supported':
            return dict(band=band, status='observed', bands=result['bands'])
    return dict(band=None, status='below_rising', bands=result['bands'])


def quote(band, row):
    career = band if band != 'BelowRising' else 'NewSigning' if row['legacyCareer'] == 'NewSigning' else 'Declining'
    return float(row['legacyBudget']) * MULTIPLIERS.get(career, .5) / MULTIPLIERS.get(row['legacyCareer'], .5)


def reach_requirements(config):
    result = []
    for name, band in zip(BANDS, config['Policy']['Bands']):
        weight = band['ReachWeights'][0]
        row = dict(band=name, regionalReachWeight=weight, supportFloorAtUnitMagnitude=1 - weight)
        for key in ('Entry', 'Exit'):
            threshold = band[key]
            row[key.lower() + 'MinimumReachAtUnitMagnitude'] = (
                max(0., (threshold - (1 - weight)) / weight) if weight else 0. if threshold <= 1 else None)
        result.append(row)
    return result


def compare(counts, prefix, value, baseline):
    if value['status'] == 'MissingEvidence' or baseline['status'] == 'MissingEvidence':
        counts[prefix + 'Missing'] += 1
        return
    delta = ORDER.index(value['band']) - ORDER.index(baseline['band'])
    counts[prefix + 'Higher'] += delta > 0
    counts[prefix + 'Lower'] += delta < 0


def analyze(replay, snapshots, marketing, config):
    saved = {}
    for row in replay['weekly']:
        key = row['date'], row['artistId'], row['variant']
        if key in saved:
            raise ValueError('Duplicate geography replay row')
        saved[key] = row
    previous, last, computed, seen = {}, {}, {}, set()
    counts, weekly, changed = defaultdict(Counter), [], []
    calibration = policy(config)
    for snapshot in snapshots:
        date, artist = snapshot['snapshotDate'], snapshot['artistId']
        if not date:
            continue
        key = date, artist
        if key in seen:
            raise ValueError('Duplicate standing observation')
        seen.add(key)
        day = calendar_date.fromisoformat(date)
        if artist in last and (day - last[artist]).days != 7:
            raise ValueError('Nonconsecutive standing observations')
        if artist not in last and date != calibration['effectiveDate']:
            raise ValueError('Missing initial effective-date observation')
        last[artist] = day
        current = {}
        for variant in VARIANTS:
            source = saved[date, artist, variant]
            for mode in MODES:
                prior = previous.get((artist, variant, mode))
                inputs = evidence(snapshot, source, mode)
                result = evaluate_mode(inputs, calibration, date, prior, mode)
                band, status = result['band'] or 'BelowRising', STATUS[result['status']]
                if status != 'MissingEvidence':
                    previous[artist, variant, mode] = result['band']
                if mode == 'full' and (band, status) != (source['band'], source['status']):
                    raise ValueError('Full policy differs from saved geography replay')
                if variant == 'neutral' and mode == 'full' and (band, status) != (snapshot['shadowBand'], snapshot['status']):
                    raise ValueError('Neutral policy differs from C#')
                selected = result['bands'].get(band)
                exclusive = (selected is not None and status != 'MissingEvidence' and
                    selected['routes']['regional']['lower'] >= selected['boundary'] and
                    all(selected['routes'][r]['upper'] < selected['boundary'] for r in ('record', 'conversion')))
                following_only = evaluate({r: ({**v, 'response': 0.} if r != 'regional'
                    else dict(response=0., draw=0., reach=0.)) for r, v in inputs.items()}, calibration, date, prior)
                following_supports = (band in ('Star', 'Superstar') and status != 'MissingEvidence' and
                    following_only['bands'][band]['lower'] >= selected['boundary'])
                item = dict(date=date, artistId=artist, variant=variant, mode=mode, band=band, status=status,
                            previousBand=prior, regionalExclusivelySupportsSelectedBand=exclusive,
                            nationalFollowingAloneSupportsSelectedHighBand=following_supports,
                            supports=result['bands'])
                current[variant, mode] = item
                computed[date, artist, variant, mode] = item
                weekly.append(item)
        for variant in VARIANTS:
            c = counts[day.year, variant]; c['observations'] += 1
            full, disabled = (current[variant, m] for m in MODES[:2])
            compare(c, 'withoutRegionalVersusFull', disabled, full)
            compare(c, 'nationalHighBandsVersusFull', current[variant, MODES[2]], full)
            c['regionalExclusiveAtFullThreshold'] += full['regionalExclusivelySupportsSelectedBand']
            for mode in MODES:
                compare(c, mode + 'GeographyVersusNeutral', current[variant, mode], current['neutral', mode])
                c[mode + 'HighBand'] += current[variant, mode]['status'] != 'MissingEvidence' and current[variant, mode]['band'] in ('Star', 'Superstar')
                c[mode + 'NationalFollowingAloneSupportsHighBand'] += current[variant, mode]['nationalFollowingAloneSupportsSelectedHighBand']
    if set(saved) != {(d, a, v) for d, a in seen for v in VARIANTS}:
        raise ValueError('Unmatched geography replay rows')
    budgets = defaultdict(Counter)
    selected_artists = {a for _, a in seen}
    for row in marketing:
        if row['artistId'] not in selected_artists or row['context'] != 'release-launch' or row['status'] != 'Available':
            continue
        year = parse_date(row['date']).year
        for variant in VARIANTS:
            c = budgets[year, variant]; c['availableReads'] += 1
            values = {m: computed[row['snapshotDate'], row['artistId'], variant, m] for m in MODES}
            baseline = {m: computed[row['snapshotDate'], row['artistId'], 'neutral', m] for m in MODES}
            if any(x['status'] == 'MissingEvidence' for x in (*values.values(), *baseline.values())):
                c['missingComparisons'] += 1
                continue
            quotes = {m: quote(x['band'], row) for m, x in values.items()}
            neutral = {m: quote(x['band'], row) for m, x in baseline.items()}
            actual = float(row['actualProposedBudget']); tolerance = max(.001, actual * 2e-6)
            if variant == 'neutral' and not math.isclose(quotes['full'], actual, abs_tol=tolerance, rel_tol=0):
                raise ValueError('Neutral launch proposal differs from C#')
            c['withoutRegionalLowerThanFull'] += quotes['without_regional'] < quotes['full'] - tolerance
            c['withoutRegionalHigherThanFull'] += quotes['without_regional'] > quotes['full'] + tolerance
            c['nationalHighBandsLowerThanFull'] += quotes[MODES[2]] < quotes['full'] - tolerance
            c['nationalHighBandsHigherThanFull'] += quotes[MODES[2]] > quotes['full'] + tolerance
            for mode in MODES:
                c[mode + 'GeographyHigherThanNeutral'] += quotes[mode] > neutral[mode] + tolerance
                c[mode + 'GeographyLowerThanNeutral'] += quotes[mode] < neutral[mode] - tolerance
            if abs(quotes['full'] - quotes['without_regional']) > tolerance or abs(quotes['full'] - neutral['full']) > tolerance:
                changed.append(dict(**row, variant=variant, fullBand=values['full']['band'],
                    withoutRegionalBand=values['without_regional']['band'], nationalHighBandsBand=values[MODES[2]]['band'],
                    proposals=quotes, neutralProposals=neutral))
    return dict(fullMatchesEveryGeographyReplay=True, neutralMatchesCSharp=True,
        bandObservations=[dict(year=y, variant=v, **c) for (y, v), c in sorted(counts.items())],
        launchBudgetReads=[dict(year=y, variant=v, **c) for (y, v), c in sorted(budgets.items())],
        changedLaunches=changed, weekly=weekly)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--replay', type=Path, required=True)
    parser.add_argument('--prefix', required=True)
    parser.add_argument('--directory', type=Path, default=Path('SimLogs'))
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    if args.output.exists():
        raise FileExistsError(args.output)
    replay = json.loads(args.replay.read_text())
    paths = {'analysisSha256': Path('SimTools/replay_opening_geography.py'),
             'calibrationSha256': Path('SimTools/StatureShadowExperimental.json'),
             'sceneSha256': Path('chart_manager.tscn'),
             'protocolSha256': Path('SimTools/StatureOpeningGeographyProtocol.md')}
    for key, path in paths.items():
        if file_digest(path) != replay[key]:
            raise ValueError('Stale geography replay dependency: ' + str(path))
    for name, digest in replay['helperSha256'].items():
        if file_digest(Path('SimTools') / name) != digest:
            raise ValueError('Stale geography replay helper: ' + name)
    config = json.loads(paths['calibrationSha256'].read_text())
    report = dict(analysisSha256=file_digest(Path(__file__)), replaySha256=file_digest(args.replay),
                  protocolSha256=file_digest(Path('SimTools/StatureRegionalSensitivityProtocol.md')),
                  helperSha256={p: file_digest(Path('SimTools') / p) for p in
                      ('trace_stature_marketing_mechanisms.py', 'compress_stature_outputs.py', 'partition_opening_following_routes.py',
                       'stature_normalization.py', 'stature_shadow_policy.py', 'replay_opening_geography.py')},
                  calibrationSha256=replay['calibrationSha256'], reachRequirements=reach_requirements(config),
                  scope='Frozen-world regional route ablation, including regional sales response. Independent hysteresis. '
                        'Same-policy geography comparisons. No replacement calibration or causal economic outcome.', seeds={})
    provenance = None
    for seed, worlds in replay['seeds'].items():
        report['seeds'][seed] = {}
        for arm, world in worlds.items():
            name = f'{args.prefix}-{seed}-{arm}'
            meta = args.directory / 'writing-calibration'
            request_path = meta / f'{name}-request.json'
            manifest_path = meta / f'{name}-outputs.json'
            if file_digest(request_path) != world['requestSha256'] or file_digest(manifest_path) != world['outputManifestSha256']:
                raise ValueError('World metadata differs from replay: ' + name)
            request = json.loads(request_path.read_text(encoding='utf-8-sig'))
            manifest = json.loads(manifest_path.read_text(encoding='utf-8-sig'))
            required = {f'{name}-{suffix}.csv' for suffix in
                        ('stature-shadow', 'opening-following-marketing', 'opening-following-census', 'opening-geography-releases')}
            entries = {entry['name']: entry for entry in manifest if entry['name'] in required}
            if entries.keys() != required:
                raise ValueError('Input manifest is incomplete')
            for filename, entry in entries.items():
                if csv_digest(args.directory / filename).upper() != entry['sha256'].upper():
                    raise ValueError('Input differs from output manifest: ' + filename)
            weeks = next(int(f.split('=', 1)[1]) for f in request['effectiveArguments'] if f.startswith('--weeks='))
            if (f'CHART_AUDIT_COMPLETE run={name} weeks={weeks}' not in (meta / f'{name}.log').read_text().splitlines()
                    or (meta / f'{name}-errors.log').stat().st_size):
                raise ValueError('World did not complete cleanly: ' + name)
            frozen = {k: request[k] for k in ('assemblySha256', 'engineSha256', 'calibrationSha256', 'sourceContentSha256')}
            if provenance is not None and frozen != provenance:
                raise ValueError('World provenance differs')
            provenance = frozen
            artists = {r['artistId'] for r in world['weekly']}
            report['seeds'][seed][arm] = analyze(world, selected_shadow(args.directory, name, artists),
                                               rows(args.directory, name, 'opening-following-marketing'), config)
        for suffix in ('opening-following-census', 'opening-geography-releases'):
            if list(rows(args.directory, f'{args.prefix}-{seed}-control', suffix)) != list(rows(args.directory, f'{args.prefix}-{seed}-treatment', suffix)):
                raise ValueError('Paired censuses differ')
    with args.output.open('x', encoding='utf-8') as stream:
        json.dump(report, stream, indent=2); stream.write('\n')


if __name__ == '__main__':
    main()
