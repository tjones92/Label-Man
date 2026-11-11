"""Measure the effect of changing only the stature marketing campaign-share denominator."""
import argparse
import json
from collections import Counter, defaultdict
from itertools import zip_longest
from pathlib import Path

from analyze_national_stature_marketing import analyze
from compress_stature_outputs import csv_digest
from stature_normalization import file_digest
from summarize_stature_marketing_trial import money, rows


FLAG = '--stature-marketing-legacy-campaign-share'
MODES = ('standing', 'legacy')
ARMS = ('control', 'treatment')
EXAMPLES = {1001: 'gen_3108', 2002: 'gen_3242'}
RETAINED = {39: 'stature-national-calibration-39w-v1', 104: 'stature-national-calibration-104w-v2'}
PROVENANCE = ('assemblySha256', 'engineSha256', 'sourceContentSha256',
              'sourceRevision', 'referenceRequestSha256', 'gcEnvironment')
TOTALS = ('launchMarketingPaid', 'followUpPaid', 'singleUnits', 'albumUnits',
          'SingleSlateIssues', 'AlbumSlateIssues')
ANNUAL = (*TOTALS, 'Star', 'Superstar', 'standingObservations', 'standingWeeks')


def read_json(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def issue_example(directory, name, record, reference=None):
    read = lambda suffix: rows(directory / f'{name}-{suffix}.csv')
    issues = [r for r in read('release-slate') if r['format'] == 'Album' and r['state'] == 'Issued']
    if reference is None:
        issue, = (r for r in issues if r['recordId'] == record)
    else:
        matched = [r for r in issues if (r['slotId'], r['actualDate'], r['labelId'], r['artistId']) ==
                   (reference['slotId'], reference['date'], reference['labelId'], reference['artistId'])]
        if not matched:
            return None
        issue, = matched
        record = issue['recordId']
    identity = issue['actualDate'], issue['labelId'], issue['artistId']
    budgets = {}
    for context in ('album-launch', 'programme-campaign-share'):
        budget, = (r for r in read('stature-marketing') if r['context'] == context and
                   (r['date'], r['labelId'], r['artistId']) == identity)
        budgets[context] = money(budget, 'requestedBudget')
    payment, = (r for r in read('release-campaign-costs') if r['recordId'] == record and
                r['week'] == issue['actualWeek'] and r['labelId'] == issue['labelId'])
    project, = (r for r in read('album-project-demand') if r['albumRecordId'] == record)
    paid = money(payment, 'marketingCost')
    return dict(recordId=record, slotId=issue['slotId'], date=identity[0], labelId=identity[1], artistId=identity[2],
                paid=paid, reserved=money(issue, 'reservedMarketing'), budgets=budgets,
                share=min(1, paid / max(1, budgets['programme-campaign-share'])),
                rawAlbumDemand=money(project, 'rawDemandBeforeCannibalization'))


def first_market_difference(directory, prefix, seed):
    paths = [directory / f'{prefix}-{seed}-{arm}-market-clearing-weekly.csv' for arm in ARMS]
    for old, new in zip_longest(*(rows(path) for path in paths)):
        if old != new:
            if old is None or new is None or (old['week'], old['regionId']) != (new['week'], new['regionId']):
                return dict(control=old, treatment=new)
            return dict(week=int(old['week']), region=old['regionId'],
                        fields={key: [old[key], new[key]] for key in old if old[key] != new[key]})
    return None


def first_slate_difference(directory, prefix, seed):
    def issued_by_week(arm):
        weeks = defaultdict(Counter)
        for row in rows(directory / f'{prefix}-{seed}-{arm}-release-slate.csv'):
            if row['state'] == 'Issued':
                key = (row['slotId'], row['labelId'], row['artistId'], row['format'])
                weeks[int(row['actualWeek'])][key] += 1
        return weeks

    old, new = (issued_by_week(arm) for arm in ARMS)
    for week in sorted(old.keys() | new.keys()):
        if old[week] != new[week]:
            return dict(week=week, controlOnly=sorted((old[week] - new[week]).elements()),
                        treatmentOnly=sorted((new[week] - old[week]).elements()))
    return None


def first_profit_gate_change(directory, prefix, seed):
    paths = [directory / f'{prefix}-{seed}-{arm}-release-profit-gates.csv' for arm in ARMS]
    identity = ('week', 'labelId', 'artistId', 'strategy')
    for index, (old, new) in enumerate(zip_longest(*(rows(path) for path in paths)), 1):
        if old is None or new is None or any(old[key] != new[key] for key in identity):
            return dict(index=index, result='gate sequences diverged', control=old, treatment=new)
        if old['commit'] != new['commit']:
            return dict(index=index, result='commit decision changed', control=old, treatment=new)
    return None


def compare(directory, prefix, seed, weeks=39):
    metadata = directory / 'writing-calibration'
    requests = {}
    reports = {}
    for mode in MODES:
        reports[mode] = analyze(directory, f'{prefix}-{mode}', seed)
        if reports[mode]['weeks'] != weeks:
            raise ValueError(f'Expected a {weeks}-week comparison')
        for arm in ARMS:
            name = f'{prefix}-{mode}-{seed}-{arm}'
            requests[mode, arm] = read_json(metadata / f'{name}-request.json')

    reference = requests['standing', 'control']
    for (mode, arm), request in requests.items():
        flags = request['effectiveArguments']
        if flags.count(FLAG) != (mode == 'legacy'):
            raise ValueError('Campaign-share mode differs from its request')
        if any(request[field] != reference[field] for field in PROVENANCE):
            raise ValueError('Comparison must use one frozen build and environment')
        original = requests['standing', arm]
        normal = lambda req: [f for f in req['effectiveArguments'] if f != FLAG and not f.startswith('--run=')]
        if normal(request) != normal(original) or request['calibrationSha256'] != original['calibrationSha256']:
            raise ValueError('A comparison changed flags beyond the campaign-share switch')
        if weeks == 104:
            short = read_json(metadata / f'stature-campaign-share-39w-v1-{mode}-{seed}-{arm}-request.json')
            flag_filter = lambda req: [f for f in req['effectiveArguments'] if not f.startswith(('--run=', '--weeks='))]
            if (flag_filter(short) != flag_filter(request) or
                    any(short[field] != request[field] for field in (*PROVENANCE, 'calibrationSha256'))):
                raise ValueError('Long trial differs from the short comparison beyond duration and name')

    examples, retained_output_matches = {}, {}
    for mode in MODES:
        examples[mode] = {}
        for arm in ARMS:
            name = f'{prefix}-{mode}-{seed}-{arm}'
            manifest = read_json(metadata / f'{name}-outputs.json')
            for suffix in ('album-project-demand', 'release-profit-gates'):
                filename = f'{name}-{suffix}.csv'
                digest, = (r['sha256'] for r in manifest if r['name'] == filename)
                if csv_digest(directory / filename).upper() != digest.upper():
                    raise ValueError(f'{suffix} export differs from its manifest')
            if mode == 'standing':
                retained = f'{RETAINED[weeks]}-{seed}-{arm}'
                prior_path = metadata / f'{retained}-outputs.json'
                prior_summary = read_json(metadata / f'{RETAINED[weeks]}-{seed}-summary.json')
                if file_digest(prior_path) != prior_summary['sha256'][prior_path.name]:
                    raise ValueError('Retained manifest differs from its verified summary')
                prior = read_json(prior_path)
                normalize = lambda entries, stem: {r['name'][len(stem):]: r['sha256'] for r in entries}
                current, previous = normalize(manifest, name), normalize(prior, retained)
                retained_output_matches[arm] = dict(outputs=len(current), different=sorted(
                    k for k in current.keys() | previous.keys() if current.get(k) != previous.get(k)))
            examples[mode][arm] = issue_example(directory, name, EXAMPLES[seed],
                                                 examples['standing'].get('control'))

    annual = {mode: {arm: reports[mode]['annual'][arm] for arm in ARMS} for mode in MODES}
    totals = {mode: {arm: {key: sum(row.get(key, 0) for row in annual[mode][arm].values()) for key in TOTALS}
                     for arm in ARMS} for mode in MODES}
    deltas = {mode: {k: totals[mode]['treatment'][k] - totals[mode]['control'][k] for k in TOTALS}
              for mode in MODES}
    years = sorted(annual['standing']['control'])
    annual_deltas = {mode: {year: {key: annual[mode]['treatment'][year].get(key, 0) -
                                       annual[mode]['control'][year].get(key, 0) for key in ANNUAL}
                            for year in years} for mode in MODES}
    return dict(totals=totals, candidateMinusOriginal=deltas, annual=annual,
                candidateMinusOriginalByYear=annual_deltas,
                changeInCalibrationDelta={k: deltas['legacy'][k] - deltas['standing'][k] for k in TOTALS},
                changeInCalibrationDeltaByYear={
                    year: {k: annual_deltas['legacy'][year][k] - annual_deltas['standing'][year][k] for k in ANNUAL}
                    for year in years},
                retainedOutputMatches=retained_output_matches, earlyAlbum=examples,
                firstMarketDifference={mode: first_market_difference(directory, f'{prefix}-{mode}', seed)
                                       for mode in MODES},
                firstSlateDifference={mode: first_slate_difference(directory, f'{prefix}-{mode}', seed)
                                      for mode in MODES},
                firstProfitGateChange={mode: first_profit_gate_change(directory, f'{prefix}-{mode}', seed)
                                       for mode in MODES},
                firstWeeklyLaunchSpendDifference={mode: reports[mode]['firstWeeklyLaunchSpendDifference']
                                                  for mode in MODES},
                firstRequestedDifference={mode: reports[mode]['firstRequestedDifference'] for mode in MODES},
                firstMatchedPaymentDifference={
                    mode: next((row for row in reports[mode]['changedMatchedIssues'] if row['paymentChanged']), None)
                    for mode in MODES},
                pairedSlots={mode: reports[mode]['issueMatchesByFormatAndYear'] for mode in MODES},
                verifiedSummarySha256={mode: reports[mode]['summarySha256'] for mode in MODES})


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--directory', type=Path, default=Path('SimLogs'))
    parser.add_argument('--prefix', default='stature-campaign-share-39w-v1')
    parser.add_argument('--weeks', type=int, choices=tuple(RETAINED), default=39)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    if args.output.exists():
        raise FileExistsError(args.output)
    report = dict(analysisSha256=file_digest(Path(__file__)), weeks=args.weeks,
                  scope='Both pairs use the full marketing reader. Only the legacy denominator pair uses the career table '
                        'for the campaign-share reference. The difference in calibration deltas includes feedback after the initial decision.',
                  seeds={seed: compare(args.directory, args.prefix, seed, args.weeks) for seed in (1001, 2002)})
    with args.output.open('x', encoding='utf-8') as stream:
        json.dump(report, stream, indent=2, sort_keys=True)
        stream.write('\n')


if __name__ == '__main__':
    main()
