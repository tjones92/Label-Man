"""Calendar-year issues, chart conversion and resource boundaries for the programme screen.

Counts unique model record IDs issued during the observation window, never opening
catalogue, strategy decisions, LP schedules, or B-sides. Directory estimates remain
provisional bands; this report does not certify a historical calibration.
"""
import argparse
import csv
import json
import statistics
from collections import Counter, defaultdict
from pathlib import Path

from analyze_demand_attention import clearing_summary

SINGLES = dict(zip(range(1960, 1965), (5797, 6036, 6700, 6543, 6504)))
ALBUMS = dict(zip(range(1960, 1965), (3112, 2927, 3655, 3686, 3553)))
BIG5 = {f'label_{n:04}' for n in range(1, 6)}


def rows(prefix, stream, optional=False):
    path = Path(f'{prefix}-{stream}.csv')
    if optional and not path.exists():
        return []
    with path.open(encoding='utf-8-sig', newline='') as source:
        return list(csv.DictReader(source))


def annual_issues(census, weeks):
    seen = set()
    counts = defaultdict(Counter)
    tiers = defaultdict(Counter)
    for row in census:
        if row['recordId'] in seen:
            raise ValueError('Duplicate issued/opening record identity')
        seen.add(row['recordId'])
        if row['observationKind'] != 'Launch':
            continue
        year = int(row['observedYear'])
        counts[year][row['format']] += 1
        tiers[year][(row['launchTier'], row['format'])] += 1
    year_weeks = defaultdict(set)
    for row in weeks:
        year_weeks[int(row['year'])].add(int(row['week']))
    if not year_weeks:
        raise ValueError('Calendar-year measurement needs observed week/year coverage')
    result = {}
    for year in sorted(year_weeks):
        single, album = counts[year]['Single'], counts[year]['Album']
        result[year] = {
            'observedWeeks': len(year_weeks[year]), 'completeYear': len(year_weeks[year]) >= 52,
            'singleIssues': single, 'albumIssues': album,
            'lpToSingleRatio': album / single if single else None,
            'issuesByTier': {tier: {fmt: tiers[year][tier, fmt] for fmt in ('Single', 'Album')}
                             for tier in sorted({tier for tier, _ in tiers[year]})},
            'singleIssueSharesByTier': {tier: tiers[year][tier, 'Single'] / single if single else None
                                       for tier in sorted({tier for tier, _ in tiers[year]})},
        }
        if year in SINGLES:
            result[year]['provisionalDirectoryEstimates'] = {'Single': SINGLES[year], 'Album': ALBUMS[year]}
            # Only a sensitivity case, never a hidden target or acceptance gate.
            result[year]['directory90PercentCoverageSensitivity'] = {'Single': SINGLES[year] / .9, 'Album': ALBUMS[year] / .9}
            if result[year]['completeYear']:
                result[year]['singleIssuesToEstimate'] = single / SINGLES[year]
                result[year]['albumIssuesToEstimate'] = album / ALBUMS[year]
    return result


def validate_resources(resources, capacity):
    seen = set()
    counts = Counter()
    for row in resources:
        key = int(row['week']), row['labelId']
        if key in seen:
            raise ValueError('Duplicate resource label/week')
        seen.add(key)
        booked, pool = float(row['promotionBooked']), float(row['promotionPool'])
        if capacity and booked > pool + .1:
            raise ValueError(f'Promotion overbooking {key}: {booked} > {pool}')
        if min(booked, pool, float(row['cashReserved'])) < 0:
            raise ValueError(f'Negative resource reservation {key}')
        if int(row['successes']) > int(row['attempts']):
            raise ValueError('Successes exceed attempts')
        counts['labelWeeks'] += 1
        counts['idleLabelWeeks'] += int(row['successes']) == 0
        counts['multipleSuccessLabelWeeks'] += int(row['successes']) > 1
        counts['dueFollowUpActWeeks'] += int(row.get('dueFollowUps', 0))
        counts['unplannedFollowUpActWeeks'] += int(row.get('unplannedFollowUps', 0))
    return dict(counts)


def measure(prefix):
    census, weeks = rows(prefix, 'demand-release-census'), rows(prefix, 'weeks')
    market_rows = rows(prefix, 'market-revenue')
    clearing_rows = rows(prefix, 'market-clearing-weekly')
    if not weeks:
        # Calibration mode omits week/revenue diagnostics but retains complete regional clearing accounting.
        weeks = clearing_rows
    report = {'prefix': str(prefix), 'annual': annual_issues(census, weeks)}
    launches = {r['recordId']: r for r in census if r['observationKind'] == 'Launch'}
    chart = rows(prefix, 'records')
    hot, top, top40_acts = defaultdict(set), defaultdict(Counter), defaultdict(set)
    peaks = defaultdict(lambda: 101)
    seen = set()
    for r in chart:
        key = int(r['week']), r['recordId']
        if key in seen:
            raise ValueError('Duplicate chart record/week')
        seen.add(key)
        position, year = int(r['currentPosition']), int(r['year'])
        if 0 < position <= 100:
            hot[year].add(r['artistId'])
            peaks[r['recordId']] = min(peaks[r['recordId']], position)
        if 0 < position <= 10:
            top[year][r['labelId']] += 1
        if 0 < position <= 40:
            top40_acts[year].add(r['artistId'])
    for year, row in report['annual'].items():
        slots = sum(top[year].values())
        row.update(hot100Acts=len(hot[year]), big5Top10SlotShare=sum(n for label, n in top[year].items() if label in BIG5) / slots if slots else None,
                   cr4Top10SlotShare=sum(sorted(top[year].values(), reverse=True)[:4]) / slots if slots else None)
        major = [r for r in launches.values() if r['format'] == 'Single' and r['launchTier'] == 'Major' and int(r['observedYear']) == year]
        row['majorTop10ConversionThroughRunEnd'] = sum(peaks[r['recordId']] <= 10 for r in major) / len(major) if major else None
        row['majorConversionCohort'] = len(major)
        row['majorConversionIsRightCensored'] = True
        cohort = top40_acts[year - 1]
        row['priorYearTop40Acts'] = len(cohort)
        row['singlesPerPriorYearTop40Act'] = sum(r['format'] == 'Single' and int(r['observedYear']) == year and r['artistId'] in cohort
                                                for r in launches.values()) / len(cohort) if cohort else None
    market = defaultdict(Counter)
    for r in market_rows:
        if r['period'] == 'weekly' and r['labelTier'] == 'All':
            market[int(r['year'])][r['releaseFormat']] += int(r['totalMarketUnits'])
    if not market:
        for r in clearing_rows:
            market[int(r['year'])]['Single'] += int(r['clearedSingleUnits'])
            market[int(r['year'])]['Album'] += int(r['clearedAlbumUnits'])
    for year, row in report['annual'].items():
        single, album = market[year]['Single'], market[year]['Album']
        row['liveSingleMarketUnits'] = single
        row['liveAlbumMarketUnits'] = album
        row['liveLpUnitShare'] = album / (single + album) if single + album else None
    report['clearing'] = clearing_summary(prefix)
    finalized = defaultdict(list)
    for row in rows(prefix, 'release-outcomes'):
        if row['recordId'] not in launches:
            continue
        net = row.get('bookedContributionAfterMarketing') or row['realizedNet']
        finalized[row['format']].append(float(net))
    report['finalizedReleaseLosses'] = {fmt: {'finalized': len(net), 'losses': sum(n < 0 for n in net),
                                                   'lossShareAmongFinalized': sum(n < 0 for n in net) / len(net)}
                                        for fmt, net in finalized.items()}
    report['activeLaunchesAtRunEnd'] = sum(r['status'] == 'Active' for r in launches.values())
    resources = rows(prefix, 'programme-resources', optional=True)
    # The metadata in a benchmark invocation, or the group manifest, provides the actual flags.
    invocation = Path(f'{prefix}-invocation.json')
    flags = []
    if invocation.exists():
        flags = json.loads(invocation.read_text(encoding='utf-8-sig'))['arguments']
    else:
        for manifest in Path(prefix).parent.glob('*/runs.json'):
            data = json.loads(manifest.read_text(encoding='utf-8-sig'))
            matches = [r for r in data.get('results', []) if r['name'] == Path(prefix).name]
            if matches:
                if len(matches) != 1 or not matches[0]['passed']:
                    raise ValueError('Ambiguous or incomplete run manifest')
                flags = matches[0]['args']
                report['runtimeSeconds'] = matches[0]['seconds']
                report['assemblySha256'] = data['assemblySha256']
                break
    report['flags'] = flags
    if resources and not flags:
        raise ValueError('Resource audit requires its native invocation or run manifest')
    if any(flag in flags for flag in ('--label-release-slate', '--label-release-capacity')) and not resources:
        raise ValueError('Enabled programme is missing its resource ledger')
    report['resources'] = validate_resources(resources, '--label-release-capacity' in flags)
    slate = rows(prefix, 'release-slate', optional=True)
    terminal = {}
    for row in slate:
        terminal[row['slotId']] = row
        if row['state'] == 'Issued' and (not row['recordId'] or int(row['actualWeek']) < int(row['readyWeek'])):
            raise ValueError('Issued slate item has no record or precedes readiness')
        # An LP issued behind its advance single drops after the existing gap, possibly after the run ends.
        if row['state'] == 'Issued' and row['recordId'] not in launches and row['reason'] != 'issued-advance-single-lp-to-follow':
            raise ValueError('Issued slate item is absent from the actual release census')
    report['slateLatestStates'] = dict(Counter(r['state'] for r in terminal.values()))
    report['slateReasons'] = dict(Counter(r['reason'] for r in slate))
    advances = [r for r in terminal.values() if r['state'] == 'Issued' and r['reason'] == 'issued-advance-single-lp-to-follow']
    report['slateAdvanceSingles'] = len(advances)
    report['slateAdvanceLpsPendingAtRunEnd'] = sum(r['recordId'] not in launches for r in advances)
    report['missedPlannedFollowUps'] = sum(r['missedFollowUp'] == '1' for r in terminal.values())
    report['cancelledPreparedProductionCost'] = sum(float(r.get('paidProduction') or 0) for r in terminal.values() if r['state'] == 'Cancelled')
    report['unissuedPreparedProductionCost'] = sum(float(r.get('paidProduction') or 0) for r in terminal.values() if r['state'] not in ('Issued', 'Cancelled'))
    finance = rows(prefix, 'label-finance')
    initial = {r['labelId'] for r in rows(prefix, 'label-directory') if r['initialTier'] == 'Small'}
    last = {r['labelId']: r for r in finance}
    report['startingSmall'] = {'firms': len(initial), 'closed': sum(last.get(i, {}).get('status') in ('Bankrupt', 'Defunct') for i in initial)}
    gaps = [int(r['weeksSinceLastSingle']) for r in rows(prefix, 'single-launch-quality') if int(r['weeksSinceLastSingle']) >= 0]
    report['medianSingleLaunchGap'] = statistics.median(gaps) if gaps else None
    report['limitations'] = ['Unique model issues are not a verified territorial/master census.',
                             'Partial years are not annualized or compared with directory totals.',
                             'Finalized loss rates exclude active releases; conversion is right censored.',
                             'Operating labels remain the firm proxy; corporate-family aggregation needs a verified mapping.']
    return report


EFFECT_METRICS = ('singleIssues', 'albumIssues', 'lpToSingleRatio', 'big5Top10SlotShare',
                  'cr4Top10SlotShare', 'hot100Acts', 'majorTop10ConversionThroughRunEnd',
                  'liveSingleMarketUnits', 'liveAlbumMarketUnits', 'liveLpUnitShare')


def factorial_effects(cells):
    """Pure per-seed 2x2 effects, followed by equal-weight seed means. No acceptance inference."""
    effects = []
    seeds = sorted({seed for studio, arm, seed in cells})
    for studio in ('off', 'on'):
        for seed in seeds:
            arms = {arm: cells[studio, arm, seed]['annual'] for arm in ('control', 'allocation', 'capacity', 'both')}
            years = set.intersection(*(set(annual) for annual in arms.values()))
            for year in sorted(years):
                if not all(arms[arm][year]['completeYear'] for arm in arms):
                    continue
                row = {'studio': studio, 'seed': seed, 'year': year, 'effects': {}}
                control_issues = arms['control'][year]['singleIssues'] + arms['control'][year]['albumIssues']
                allocation_issues = arms['allocation'][year]['singleIssues'] + arms['allocation'][year]['albumIssues']
                row['allocationTotalIssueRatioToControl'] = allocation_issues / control_issues if control_issues else None
                row['interpretation'] = 'Allocation contrasts include any measured preparation, feasibility, LP-coupling and throughput changes; inspect the issue ratio before attributing chart effects to allocation.'
                for metric in EFFECT_METRICS:
                    c, a, b, ab = (arms[arm][year].get(metric) for arm in ('control', 'allocation', 'capacity', 'both'))
                    if any(v is None for v in (c, a, b, ab)):
                        continue
                    row['effects'][metric] = {'allocationMinusControl': a - c, 'capacityMinusControl': b - c,
                                              'bothMinusControl': ab - c, 'interaction': ab - a - b + c}
                effects.append(row)
    means = defaultdict(lambda: defaultdict(list))
    for row in effects:
        for metric, contrasts in row['effects'].items():
            for contrast, value in contrasts.items():
                means[row['studio'], row['year']][f'{metric}:{contrast}'].append(value)
    return {'distinctSeeds': len(seeds), 'multiSeedEvidence': len(seeds) >= 3, 'perSeed': effects,
            'equalWeightMeans': [{'studio': studio, 'year': year, 'effects': {key: statistics.mean(v) for key, v in values.items()}}
                                for (studio, year), values in sorted(means.items())]}


def measure_plan(plan_path):
    plan = json.loads(plan_path.read_text(encoding='utf-8-sig'))
    seeds = plan['seeds']
    if not seeds or len(seeds) != len(set(seeds)):
        raise ValueError('Screen seeds must be distinct')
    if {(c['studio'], c['check']) for c in plan['checks'] if c['passed']} != {(s, c) for s in ('on', 'off') for c in ('mechanism', 'roundtrip')}:
        raise ValueError('Both studio settings require completed mechanisms and save/resume checks')
    cells, runs = {}, []
    root = plan_path.parent.parent
    for group in plan['groups']:
        studio, arm = group['studio'], group['arm']
        manifest = json.loads((root / group['tag'] / 'runs.json').read_text(encoding='utf-8-sig'))
        expected = set(plan['bundle'])
        if studio == 'on':
            expected.add('--studio-parent-labels')
        if arm in ('allocation', 'both'):
            expected.add('--label-release-slate')
        if arm in ('capacity', 'both'):
            expected.add('--label-release-capacity')
        if set(manifest['extraFlags']) != expected or manifest['assemblySha256'] != plan['assemblySha256'] or manifest['weeks'] != plan['weeks']:
            raise ValueError('Mixed flags, assembly, or duration in screen')
        if manifest['profile'] != 'game' or manifest['modes'] != ['default']:
            raise ValueError('Screen must use the working game profile')
        if sorted(r['seed'] for r in manifest['results'] if r['passed']) != sorted(seeds) or len(manifest['results']) != len(seeds):
            raise ValueError('Incomplete or repeated native seed')
        for run in manifest['results']:
            key = studio, arm, run['seed']
            if key in cells:
                raise ValueError('Repeated factorial cell')
            report = measure(root / run['name'])
            report.update(studio=studio, arm=arm, seed=run['seed'])
            cells[key] = report
            runs.append(report)
    if set(cells) != {(s, a, seed) for s in ('on', 'off') for a in ('control', 'allocation', 'capacity', 'both') for seed in seeds}:
        raise ValueError('All eight arms and all seeds are required')
    return {'runs': runs, 'factorial': factorial_effects(cells), 'calibrationAccepted': False}


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('prefix', type=Path, nargs='*')
    parser.add_argument('--plan', type=Path)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    if bool(args.plan) == bool(args.prefix):
        parser.error('Supply either run prefixes or --plan')
    result = measure_plan(args.plan) if args.plan else {'runs': [measure(prefix) for prefix in args.prefix], 'calibrationAccepted': False}
    args.output.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    print(f'Validated {len(result["runs"])} runs; report: {args.output}')
