"""Validate marketing envelopes and summarize an independent experimental reader screen."""
import argparse
import csv
import datetime as dt
import json
import math
import gzip
from collections import Counter, defaultdict
from itertools import zip_longest
from pathlib import Path

from stature_normalization import file_digest, parse_date
from compress_stature_outputs import csv_digest


def rows(path):
    opener = path.open if path.exists() else lambda **kwargs: gzip.open(str(path) + '.gz', 'rt', **kwargs)
    with opener(newline='', encoding='utf-8-sig') as stream:
        yield from csv.DictReader(stream)


def write_neutrality(directory, prefix, seed):
    metadata = directory / 'writing-calibration'
    manifests = {}
    for arm in ('disabled', 'control'):
        name = f'{prefix}-{seed}-{arm}'
        manifest = json.loads((metadata / f'{name}-outputs.json').read_text(encoding='utf-8-sig'))
        manifests[arm] = {}
        for entry in manifest:
            if csv_digest(directory / entry['name']).upper() != entry['sha256'].upper():
                raise ValueError('Output differs from manifest: ' + entry['name'])
            manifests[arm][entry['name'][len(name):]] = entry['sha256'].upper()
    if not manifests['disabled']:
        raise ValueError('No disabled outputs')
    for suffix, digest in manifests['disabled'].items():
        if manifests['control'].get(suffix) != digest:
            raise ValueError('Neutrality failed: ' + suffix)
    allowed = {'-stature-shadow.csv', '-stature-decisions.csv', '-stature-marketing.csv'}
    if set(manifests['control']) - set(manifests['disabled']) != allowed:
        raise ValueError('Unexpected shadow output schema')
    request = json.loads((metadata / f'{prefix}-{seed}-control-request.json').read_text(encoding='utf-8-sig'))
    weeks = next(int(flag.split('=', 1)[1]) for flag in request['flags'] if flag.startswith('--weeks='))
    with (metadata / f'{prefix}-{seed}-neutrality.json').open('x', encoding='utf-8') as stream:
        json.dump(dict(seed=seed, weeks=weeks, commonCsvCount=len(manifests['disabled']), byteIdentical=True), stream)


def money(row, field):
    value = float(row[field])
    if not math.isfinite(value) or value < 0:
        raise ValueError(f'Invalid {field}: {row[field]}')
    return value


def summarize_reads(path, causal, blend=1.0, legacy_campaign_share=False):
    groups = defaultdict(Counter)
    artists = set()
    for row in rows(path):
        if row['status'] not in {'Available', 'Missing', 'Future', 'Stale', 'MissingEvidence', 'PlayerOwned',
                                 'Inactive', 'Terminal', 'NotOwned', 'LifecycleUnresolved'}:
            raise ValueError('Unknown marketing read status')
        old, proposed, requested = (money(row, key) for key in ('legacyBudget', 'proposedBudget', 'requestedBudget'))
        available = row['status'] == 'Available'
        if available:
            age = (parse_date(row['date']) - dt.date.fromisoformat(row['snapshotDate'])).days
            if not 0 <= age <= 7 or not row['standingCareer']:
                raise ValueError('Invalid available snapshot')
        elif row['standingCareer'] or proposed != old:
            raise ValueError('Unavailable read must fall back to legacy budget')
        applied = causal and available and not (legacy_campaign_share and row['context'] == 'programme-campaign-share')
        expected = old + blend * (proposed - old) if applied else old
        exact = not applied or blend in (0, 1)
        valid = (requested == (proposed if applied and blend == 1 else old) if exact
                 else math.isclose(requested, expected, rel_tol=2e-6, abs_tol=.001))
        if not valid:
            raise ValueError('Requested envelope violates reader rule')
        # Same rung must use the identical existing table; allow float rounding for table ratios.
        if available:
            multipliers = {'Superstar': 2.5, 'Star': 2, 'Established': 1.5, 'Rising': 1.2, 'NewSigning': .8}
            ratio = multipliers.get(row['standingCareer'], .5) / multipliers.get(row['legacyCareer'], .5)
            if not math.isclose(proposed, old * ratio, rel_tol=2e-6, abs_tol=.001):
                raise ValueError('Proposed envelope violates existing career table')
        changed = requested != old
        if changed:
            artists.add(row['artistId'])
        keys = ('all', 'year:' + str(parse_date(row['date']).year), 'context:' + row['context'],
                'career:' + row['legacyCareer'], 'status:' + row['status'])
        for key in keys:
            group = groups[key]
            group['reads'] += 1
            group['available'] += available
            group['proposedDifferent'] += proposed != old
            group['changed'] += changed
            group['higher'] += requested > old
            group['lower'] += requested < old
    if not groups['all']['reads']:
        raise ValueError('Empty marketing trace')
    return {'distinctChangedArtists': len(artists), 'groups': {key: dict(value) for key, value in sorted(groups.items())}}


def summarize_economy(directory, name):
    path = lambda suffix: directory / f'{name}-{suffix}.csv'
    launch, followup, slates, refusals = Counter(), Counter(), defaultdict(Counter), Counter()
    for row in rows(path('release-campaign-costs')):
        launch['events'] += 1
        launch['marketingCost'] += money(row, 'marketingCost')
        launch['productionCost'] += money(row, 'productionCost')
    for row in rows(path('record-campaigns')):
        followup['events:' + row['kind']] += 1
        followup['spend:' + row['kind']] += money(row, 'spend')
    for row in rows(path('release-slate')):
        slates[row['format']][row['state']] += 1
    for row in rows(path('programme-issue-refusals')):
        refusals[row['format'] + ':' + row['gate']] += 1
    annual = list(rows(path('decade-annual-rollup')))
    if not annual:
        raise ValueError('Missing annual economy rollup')
    return {'releaseCampaignCostEvents': dict(launch), 'followUpCampaignEvents': dict(followup),
            'slateEvents': {key: dict(value) for key, value in sorted(slates.items())},
            'issueRefusals': dict(refusals), 'annualRollup': annual}


def calibration_pair_check(requests):
    from verify_national_stature_policy import candidate_is_scoped

    calibrations, paired_flags = [], []
    for arm in ('control', 'treatment'):
        request = requests[arm]
        flags = request['effectiveArguments']
        if flags.count('--stature-marketing-reader') != 1 or request['validationMode'] != 'calibration-pair':
            raise ValueError('Calibration pair requires a live marketing reader in each arm')
        path, = [Path(f.split('=', 1)[1]) for f in flags if f.startswith('--stature-calibration=')]
        if file_digest(path).upper() != request['calibrationSha256'].upper():
            raise ValueError('Calibration differs from run request')
        calibrations.append(json.loads(path.read_text(encoding='utf-8-sig')))
        paired_flags.append([f for f in flags if not f.startswith(('--run=', '--stature-calibration='))])
    if paired_flags[0] != paired_flags[1]:
        raise ValueError('Calibration pair differs in settings beyond calibration and run name')
    candidate_is_scoped(*calibrations)


def first_requested_difference(control, treatment):
    identity = ('date', 'artistId', 'labelId', 'context', 'snapshotDate', 'legacyCareer', 'legacyBudget')
    for index, (old, new) in enumerate(zip_longest(rows(control), rows(treatment)), 1):
        if old is None or new is None or any(old[k] != new[k] for k in identity):
            return {'matchedReadsBeforeDivergence': index - 1, 'result': 'read sequences diverged'}
        if old['requestedBudget'] != new['requestedBudget']:
            return {'matchedReadsBeforeDivergence': index - 1, 'result': 'requested budget changed',
                    'control': old, 'treatment': new}
    return {'result': 'all requested budgets matched'}


def run(directory, prefix, seed, causal_only=False, calibration_pair=False):
    causal_only = causal_only or calibration_pair
    metadata = directory / 'writing-calibration'
    result = {'seed': seed, 'arms': {}, 'sha256': {}, 'analysisSha256': file_digest(Path(__file__)),
              'caveat': 'Repeated budget reads are requested envelopes before caps, not unique investments or spending. '
              'Launch costs and follow-up spends are separate event streams, not a combined all-in spend measure. '
              'Slate states are events. Worlds diverge; arm differences are net system effects. '
              'Run duration and separate cohort/trajectory analyses determine evaluation coverage; this summary alone does not authorize adoption of the experimental calibration.'}
    requests, manifests = {}, {}
    arms = ('control', 'treatment') if causal_only else ('disabled', 'control', 'treatment')
    baseline = 'control' if causal_only else 'disabled'
    for arm in arms:
        name = f'{prefix}-{seed}-{arm}'
        request_path = metadata / f'{name}-request.json'
        request = json.loads(request_path.read_text(encoding='utf-8-sig'))
        requests[arm] = request
        completion = f"CHART_AUDIT_COMPLETE run={name} weeks="
        weeks = next(int(flag.split('=', 1)[1]) for flag in request['flags'] if flag.startswith('--weeks='))
        if completion + str(weeks) not in (metadata / f'{name}.log').read_text(encoding='utf-8-sig').splitlines():
            raise ValueError('Missing exact completion marker')
        if (metadata / f'{name}-errors.log').stat().st_size:
            raise ValueError('Run has nonempty stderr')
        for suffix in ('.log', '-errors.log', '-engine.log'):
            log = metadata / f'{name}{suffix}'
            result['sha256'][log.name] = file_digest(log)
        forbidden = ('--stature-programme-album-reader', '--stature-career-display')
        if any(flag in request['flags'] for flag in forbidden):
            raise ValueError('Screen enables another stature reader')
        active_reader = arm == 'treatment' or calibration_pair
        if ('--stature-marketing-reader' in request['flags']) != active_reader:
            raise ValueError('Invalid causal arm flags')
        result['sha256'][request_path.name] = file_digest(request_path)
        result['arms'][arm] = summarize_economy(directory, name)
        if arm != 'disabled':
            blends = [float(flag.split('=', 1)[1]) for flag in request['flags'] if flag.startswith('--stature-marketing-blend=')]
            if len(blends) > 1 or blends and (not math.isfinite(blends[0]) or not 0 <= blends[0] <= 1):
                raise ValueError('Invalid marketing blend')
            result['arms'][arm]['marketingBlend'] = blends[0] if blends else 1.0
            legacy_share = '--stature-marketing-legacy-campaign-share' in request['effectiveArguments']
            if legacy_share and not active_reader:
                raise ValueError('Legacy campaign share requires a live marketing reader')
            result['arms'][arm]['marketing'] = summarize_reads(directory / f'{name}-stature-marketing.csv', active_reader,
                                                               blends[0] if blends else 1.0, legacy_share)
        manifest_path = metadata / f'{name}-outputs.json'
        manifest = json.loads(manifest_path.read_text(encoding='utf-8-sig'))
        manifests[arm] = {item['name'][len(name):]: item['sha256'].upper() for item in manifest}
        if len(manifests[arm]) != len(manifest):
            raise ValueError('Duplicate output manifest entry')
        actual_names = [path.name for path in directory.glob(f'{name}-*.csv')] + [path.name[:-3] for path in directory.glob(f'{name}-*.csv.gz')]
        if len(actual_names) != len(set(actual_names)) or set(actual_names) != {item['name'] for item in manifest}:
            raise ValueError('Output set differs from manifest')
        for item in manifest:
            if csv_digest(directory / item['name']).upper() != item['sha256'].upper():
                raise ValueError('Output changed since run: ' + item['name'])
        result['sha256'][manifest_path.name] = file_digest(manifest_path)
    for field in ('assemblySha256', 'engineSha256', 'calibrationSha256', 'sourceContentSha256', 'sourceRevision'):
        if calibration_pair and field == 'calibrationSha256':
            continue
        if any(requests[arm][field] != requests[baseline][field] for arm in arms):
            raise ValueError('Unmatched trial input: ' + field)
    # Only the opt-in/diagnostic flags and run name may differ; all economic configuration is paired.
    def economic_flags(request):
        return [flag for flag in request['effectiveArguments'] if flag != '--profile-performance' and not flag.startswith(('--run=', '--stature-', '--observe-stature-', '--persist-stature-'))]
    if any(economic_flags(requests[arm]) != economic_flags(requests[baseline]) for arm in arms):
        raise ValueError('Unmatched economic configuration')
    if calibration_pair:
        calibration_pair_check(requests)
        result['comparison'] = 'Both arms apply the marketing reader. The candidate restricts Star and Superstar to national routes.'
        result['firstRequestedDifference'] = first_requested_difference(*(directory / f'{prefix}-{seed}-{arm}-stature-marketing.csv' for arm in arms))
    if causal_only:
        if set(manifests['control']) - {'-performance-profile.csv'} != set(manifests['treatment']) - {'-performance-profile.csv'}:
            raise ValueError('Causal-pair output schemas differ')
        neutrality = {'performed': False, 'reason': 'Independent causal pair; disabled/shadow neutrality is not repeated at this duration.'}
    else:
        neutrality = json.loads((metadata / f'{prefix}-{seed}-neutrality.json').read_text(encoding='utf-8-sig'))
        if neutrality['byteIdentical'] is not True:
            raise ValueError('Neutrality gate failed')
        if not manifests['disabled'] or neutrality['commonCsvCount'] != len(manifests['disabled']):
            raise ValueError('Invalid neutrality output count')
        for suffix, digest in manifests['disabled'].items():
            if manifests['control'].get(suffix) != digest:
                raise ValueError('Neutrality output differs: ' + suffix)
        allowed = {'-stature-shadow.csv', '-stature-decisions.csv', '-stature-marketing.csv'}
        if set(manifests['control']) - set(manifests['disabled']) != allowed:
            raise ValueError('Unexpected shadow output schema')
    result['neutrality'] = neutrality
    with (metadata / f'{prefix}-{seed}-summary.json').open('x', encoding='utf-8') as stream:
        json.dump(result, stream, indent=2, sort_keys=True)
        stream.write('\n')
    print(json.dumps({'seed': seed, 'neutrality': neutrality, 'treatment': result['arms']['treatment']['marketing']['groups']['all']}))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--directory', type=Path, default=Path('SimLogs'))
    parser.add_argument('--prefix', required=True)
    parser.add_argument('--seed', type=int, required=True)
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument('--write-neutrality', action='store_true')
    mode.add_argument('--causal-only', action='store_true')
    mode.add_argument('--calibration-pair', action='store_true')
    args = parser.parse_args()
    if args.write_neutrality:
        write_neutrality(args.directory, args.prefix, args.seed)
    else:
        run(args.directory, args.prefix, args.seed, args.causal_only, args.calibration_pair)
