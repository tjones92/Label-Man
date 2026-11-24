"""Verify shared route-eligibility kernels against the retained regional sensitivity.

Historical helper hashes identify the old reference implementation; current
helper hashes are recorded separately because this checks its replacement.
"""
import argparse
import copy
import json
from collections import Counter
from pathlib import Path

from analyze_regional_standing_sensitivity import evidence, MODES
from compress_stature_outputs import csv_digest
from partition_opening_following_routes import policy, selected_shadow
from replay_opening_geography import STATUS, VARIANTS
from stature_normalization import file_digest
from stature_shadow_policy import BANDS, ROUTES, evaluate, validate


def candidate_is_scoped(legacy, candidate):
    normalized = copy.deepcopy(candidate)
    normalized['Version'] = legacy['Version']
    normalized['Policy']['Version'] = legacy['Policy']['Version']
    if candidate['Version'] == legacy['Version'] or candidate['Policy']['Version'] == legacy['Policy']['Version']:
        raise ValueError('Candidate must have independent configuration and policy versions')
    for index, band in enumerate(normalized['Policy']['Bands']):
        if band.pop('EligibleRoutes', None) != [index < 2, True, True]:
            raise ValueError('Candidate eligibility differs from declared national-high-tier rule')
    if normalized != legacy:
        raise ValueError('Candidate changes settings beyond version and route eligibility')
    validate(policy(candidate))


def support_values(supports):
    return [[s['lower'], s['upper'], s['boundary'],
             *[s['routes'][r][bound] for r in ROUTES for bound in ('lower', 'upper')]]
            for s in (supports[b] for b in BANDS)]


def verify_world(world, snapshots, legacy, candidate, stream, name):
    expected = {}
    for row in world['weekly']:
        key = row['date'], row['artistId'], row['variant'], row['mode']
        if key in expected:
            raise ValueError('Duplicate retained reference row')
        expected[key] = row
    priors, dates, seen, counts = {}, {}, set(), Counter()
    from datetime import date as calendar_date
    for snapshot in snapshots:
        date, artist = snapshot['snapshotDate'], snapshot['artistId']
        if not date:
            continue
        day = calendar_date.fromisoformat(date)
        if artist in dates:
            if (day - dates[artist]).days != 7:
                raise ValueError('Duplicate or nonconsecutive source observation')
        elif date != legacy['Policy']['EffectiveDate'][:10]:
            raise ValueError('Missing effective-date observation')
        dates[artist] = day
        for variant in VARIANTS:
            source = expected[date, artist, variant, 'full']
            # The retained sensitivity contains support, not inputs. Regional
            # inputs come from the separately bound geography replay (attached below).
            geography = world['_geography'][date, artist, variant]
            for mode in MODES:
                key = date, artist, variant, mode
                wanted = expected[key]
                calibration = candidate if mode == MODES[2] else legacy
                inputs = evidence(snapshot, geography, mode)
                previous = priors.get((artist, variant, mode))
                if previous != wanted['previousBand']:
                    raise ValueError('Retained history differs from independently evolved history')
                actual = evaluate(inputs, policy(calibration), date, previous)
                if (actual['band'] or 'BelowRising', STATUS[actual['status']]) != (wanted['band'], wanted['status']):
                    raise ValueError('Shared Python policy differs from retained sensitivity')
                if support_values(actual['bands']) != support_values(wanted['supports']):
                    raise ValueError('Shared Python support differs from retained sensitivity')
                if actual['status'] != 'missing_evidence':
                    priors[artist, variant, mode] = actual['band']
                seen.add(key)
                counts['verifiedCases'] += 1
                counts[mode + 'HighBand1961'] += date.startswith('1961') and actual['band'] in ('Star', 'Superstar') and variant == 'neutral'
                stream.write(json.dumps(dict(key=f'{name}/{artist}/{variant}/{mode}', date=date,
                    calibration='candidate' if mode == MODES[2] else 'legacy',
                    routes=[[inputs[r][k] for k in ('response', 'draw', 'reach')] for r in ROUTES],
                    expected=dict(band=wanted['band'], status=wanted['status'], supports=support_values(wanted['supports']))),
                    separators=(',', ':')) + '\n')
    if seen != expected.keys():
        raise ValueError('Unmatched retained reference rows')
    return dict(counts)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--reference', type=Path, required=True)
    parser.add_argument('--geography', type=Path, required=True)
    parser.add_argument('--prefix', required=True)
    parser.add_argument('--candidate', type=Path, default=Path('SimTools/StatureShadowNationalHighTiers.json'))
    parser.add_argument('--directory', type=Path, default=Path('SimLogs'))
    parser.add_argument('--cases', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    if args.cases.exists() or args.output.exists() or args.cases.resolve() == args.output.resolve():
        raise FileExistsError('Cases and report must be separate new files')
    legacy_path = Path('SimTools/StatureShadowExperimental.json')
    reference = json.loads(args.reference.read_text())
    geography = json.loads(args.geography.read_text())
    if reference['replaySha256'] != file_digest(args.geography):
        raise ValueError('Reference is not bound to this geography replay')
    if reference['calibrationSha256'] != file_digest(legacy_path) or geography['calibrationSha256'] != file_digest(legacy_path):
        raise ValueError('Legacy calibration changed')
    legacy, candidate = (json.loads(p.read_text()) for p in (legacy_path, args.candidate))
    candidate_is_scoped(legacy, candidate)
    report = dict(analysisSha256=file_digest(Path(__file__)), referenceSha256=file_digest(args.reference),
        geographySha256=file_digest(args.geography), legacyCalibrationSha256=file_digest(legacy_path),
        candidateCalibrationSha256=file_digest(args.candidate),
        currentHelperSha256={n: file_digest(Path('SimTools') / n) for n in
            ('stature_shadow_policy.py', 'partition_opening_following_routes.py', 'replay_opening_geography.py',
             'analyze_regional_standing_sensitivity.py', 'compress_stature_outputs.py',
             'trace_stature_marketing_mechanisms.py', 'stature_normalization.py')},
        historicalHelperSha256=reference['helperSha256'],
        scope='Shared Python kernel parity against retained full, ablated and national-high-tier histories. '
              'JSONL cases for independent shared C# kernel replay. Historical worlds are fixed; no causal simulation.', seeds={})
    expected_cases = sum(len(w['weekly']) for worlds in reference['seeds'].values() for w in worlds.values())
    provenance = None
    with args.cases.open('x', encoding='utf-8') as stream:
        stream.write(json.dumps(dict(calibrations=dict(legacy=legacy['Policy'], candidate=candidate['Policy']),
                                     expectedCases=expected_cases)) + '\n')
        for seed, worlds in reference['seeds'].items():
            report['seeds'][seed] = {}
            for arm, world in worlds.items():
                name = f'{args.prefix}-{seed}-{arm}'
                old = geography['seeds'][seed][arm]
                meta = args.directory / 'writing-calibration'
                request_path, manifest_path = (meta / f'{name}-{suffix}.json' for suffix in ('request', 'outputs'))
                if file_digest(request_path) != old['requestSha256'] or file_digest(manifest_path) != old['outputManifestSha256']:
                    raise ValueError('Historical request or manifest changed')
                request = json.loads(request_path.read_text(encoding='utf-8-sig'))
                frozen = {k: request[k] for k in ('assemblySha256', 'engineSha256', 'calibrationSha256', 'sourceContentSha256')}
                if provenance is not None and provenance != frozen:
                    raise ValueError('World provenance differs')
                provenance = frozen
                weeks = next(int(f.split('=', 1)[1]) for f in request['effectiveArguments'] if f.startswith('--weeks='))
                if f'CHART_AUDIT_COMPLETE run={name} weeks={weeks}' not in (meta / f'{name}.log').read_text().splitlines() or (meta / f'{name}-errors.log').stat().st_size:
                    raise ValueError('Historical world did not complete cleanly')
                manifest = json.loads(manifest_path.read_text(encoding='utf-8-sig'))
                entry, = [r for r in manifest if r['name'] == f'{name}-stature-shadow.csv']
                if csv_digest(args.directory / entry['name']).upper() != entry['sha256'].upper():
                    raise ValueError('Snapshot CSV differs from historical manifest')
                world['_geography'] = {(r['date'], r['artistId'], r['variant']): r for r in old['weekly']}
                artists = {r['artistId'] for r in world['weekly']}
                report['seeds'][seed][arm] = verify_world(world, selected_shadow(args.directory, name, artists), legacy, candidate, stream, name)
                print('Verified Python histories: ' + name, flush=True)
    report['casesSha256'] = file_digest(args.cases)
    report['verifiedPythonCases'] = expected_cases
    report['csharpParity'] = 'pending; run the headless policy check with --stature-policy-replay=<cases>'
    with args.output.open('x', encoding='utf-8') as stream:
        json.dump(report, stream, indent=2); stream.write('\n')


if __name__ == '__main__':
    main()
