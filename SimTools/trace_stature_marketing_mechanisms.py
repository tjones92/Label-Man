"""Audit common opening acts in existing worlds; frozen-input sensitivities only.

Runtime record IDs are joined within an arm. Opening identities require identical
initial shadow rows and identical week-zero record rows across the paired arms.
Following discounts retain observed reach, peer references and hysteresis boundary:
they are local input screens, not replayed careers or source attribution.
"""
import argparse
import csv
import datetime as dt
import gzip
import json
from collections import Counter, defaultdict
from pathlib import Path

from stature_normalization import parse_date, file_digest

BANDS = ('BelowRising', 'Rising', 'Established', 'Star', 'Superstar')


def rows(directory, name, suffix):
    path = directory / f'{name}-{suffix}.csv'
    if not path.exists():
        path = Path(str(path) + '.gz')
    opener = gzip.open if path.suffix == '.gz' else open
    with opener(path, 'rt', newline='', encoding='utf-8-sig') as stream:
        yield from csv.DictReader(stream)


def initial(directory, name):
    result = {}
    for row in rows(directory, name, 'stature-shadow'):
        if row['date'] != '1/1/1960':
            break
        result[row['artistId']] = row
    return result


def charts(directory, name):
    opening, counts, identity, zero = set(), defaultdict(Counter), {}, []
    for r in rows(directory, name, 'record-acts'):
        identity[r['recordId']] = r['artistId']
    for r in rows(directory, name, 'single-launch-quality'):
        identity[r['singleId']] = r['artistId']
    for r in rows(directory, name, 'release-slate'):
        if r['recordId'] and r['artistId']:
            identity[r['recordId']] = r['artistId']
    for r in rows(directory, name, 'records'):
        artist, year = r['artistId'], int(r['year'])
        pos = int(r['currentPosition'])
        if r['week'] == '0':
            zero.append(r)
            if 1 <= pos <= 100:
                opening.add(artist)
        if 1 <= pos <= 100:
            c = counts[year, artist]
            c['hot100SlotWeeks'] += 1
            c['top10SlotWeeks'] += pos <= 10
            c['chartedUnits'] += float(r['unitsThisWeek'])
    return opening, counts, identity, zero


def sensitivity(row, factor, policy):
    """Change following magnitude only, preserving each observed band boundary.

    Frozen reach retains some following evidence, so this is a partial channel
    screen. Keeping observed boundaries also avoids claiming sequential hysteresis.
    """
    sales = max(float(row[k + 'Score']) for k in ('singleYoung', 'singleAdult', 'albumYoung', 'albumAdult'))
    tv = max(float(row[k + 'Score']) for k in ('tvYoung', 'tvAdult'))
    following = factor * max(float(row[k + 'Score']) for k in ('followingYoung', 'followingAdult'))
    magnitudes = (max(float(row['regionalResponse']), factor * float(row['regionalDraw'])),
                  max(sales, following), max(tv, following))
    reaches = (float(row['recordReach']), float(row['recordReach']), float(row['conversionReach']))
    candidate = 'BelowRising'
    for band, p in zip(BANDS[1:], policy['Bands']):
        support = max(m * (1 - w + w * reach) if eligible else 0.
                      for m, w, reach, eligible in zip(magnitudes, p['ReachWeights'], reaches, p.get('EligibleRoutes') or [True] * 3))
        if support >= float(row[band + 'Boundary']):
            candidate = band
    return candidate


def audit(directory, prefix, seed, policy):
    names = {a: f'{prefix}-{seed}-{a}' for a in ('control', 'treatment')}
    initials = {a: initial(directory, n) for a, n in names.items()}
    if not initials['control'] or initials['control'] != initials['treatment']:
        raise ValueError('Opening identity evidence differs')
    opening = set(initials['control'])
    chart = {a: charts(directory, n) for a, n in names.items()}
    if chart['control'][3] != chart['treatment'][3]:
        raise ValueError('Prewarmed week-zero chart differs')
    fixed_chart = chart['control'][0]
    selected, rankings = set(), {}
    for year in (1964, 1968):
        changes = [(chart['treatment'][1][year, x]['top10SlotWeeks'] - chart['control'][1][year, x]['top10SlotWeeks'], x) for x in opening]
        winners = sorted((d, x) for d, x in changes if d > 0)[-5:][::-1]
        losers = sorted((d, x) for d, x in changes if d < 0)[:5]
        rankings[str(year)] = {'winners': winners, 'losers': losers}
        selected.update(x for _, x in winners + losers)
    result = {'verifiedOpeningActs': len(opening), 'fixedWeekZeroChartActs': len(fixed_chart),
              'rankingsTop10SlotWeekDelta': rankings, 'arms': {}}
    for arm, name in names.items():
        print(f'{name}: campaign and audience joins', flush=True)
        _, counts, identity, _ = chart[arm]
        annual, traces, snapshots = defaultdict(Counter), {x: {'initial': initials[arm][x], 'annual': {}, 'reads': [], 'following': []} for x in selected}, {}
        following0 = {}
        for r in rows(directory, name, 'following'):
            if r['kind'] != 'act':
                continue
            if r['date'] == '1/1/1960':
                following0[r['artistId']] = r
            if r['artistId'] in selected:
                traces[r['artistId']]['following'].append(r)
        for (year, artist), c in counts.items():
            group = 'openingWeekZeroChart' if artist in fixed_chart else 'otherOpening' if artist in opening else 'runtime'
            annual[year, group].update(c)
            if artist in selected:
                traces[artist]['annual'][str(year)] = dict(c)
        def add(artist, year, key, value):
            if artist in traces:
                traces[artist]['annual'].setdefault(str(year), {})[key] = traces[artist]['annual'].get(str(year), {}).get(key, 0) + value
        for r in rows(directory, name, 'release-campaign-costs'):
            artist = identity.get(r['recordId'])
            # Weekly dates begin 1960-01-01 and use the calendar, not week//52.
            year = (dt.date(1960, 1, 1) + dt.timedelta(weeks=int(r['week']) - 1)).year
            add(artist, year, 'launchMarketingPaid', float(r['marketingCost']))
            add(artist, year, 'launchCostEvents', 1)
        for r in rows(directory, name, 'record-campaigns'):
            year = (dt.date(1960, 1, 1) + dt.timedelta(weeks=int(r['week']) - 1)).year
            add(r['artistId'], year, 'followUpPaid', float(r['spend']))
        for r in rows(directory, name, 'act-release-years'):
            add(r['artistId'], int(r['year']), 'singlesIssued', int(r['singles']))
            add(r['artistId'], int(r['year']), 'standaloneLps', int(r['standaloneLps']))
        print(f'{name}: scanning standing components', flush=True)
        for r in rows(directory, name, 'stature-shadow'):
            if r['artistId'] in selected and r['snapshotDate']:
                snapshots[r['artistId'], r['snapshotDate']] = r
        sensitivity_counts, budget_groups = defaultdict(Counter), defaultdict(Counter)
        for r in rows(directory, name, 'stature-marketing'):
            artist = r['artistId']
            old, proposed = float(r['legacyBudget']), float(r['proposedBudget'])
            year = parse_date(r['date']).year
            if r['context'] == 'release-launch':
                group = 'openingWeekZeroChart' if artist in fixed_chart else 'otherOpening' if artist in opening else 'runtime'
                c = budget_groups[year, group]
                c['reads'] += 1; c['uplifts'] += proposed > old; c['cuts'] += proposed < old
                c['absoluteFullChange'] += abs(proposed - old)
                c['absoluteHalfChange'] += abs(.5 * (proposed - old))
            if artist not in selected:
                continue
            snapshot = snapshots.get((artist, r['snapshotDate']))
            trace = dict(r)
            if snapshot:
                trace['snapshot'] = snapshot
                if r['status'] == 'Available':
                    baseline = sensitivity(snapshot, 1, policy)
                    if baseline != snapshot['shadowBand']:
                        raise ValueError('Frozen-input policy does not reproduce snapshot')
                    for factor in (.5, 0):
                        b = sensitivity(snapshot, factor, policy)
                        trace[f'followingMagnitude{factor}Band'] = b
                        c = sensitivity_counts[year, r['context'], factor]
                        c['reads'] += 1
                        c['lowerBand'] += BANDS.index(b) < BANDS.index(baseline)
                        c['baselineBudgetUplifts'] += proposed > old
                        multipliers = {'Superstar': 2.5, 'Star': 2, 'Established': 1.5, 'Rising': 1.2, 'NewSigning': .8}
                        career = b if b != 'BelowRising' else 'NewSigning' if r['legacyCareer'] == 'NewSigning' else 'Declining'
                        sensitive_budget = old * multipliers.get(career, .5) / multipliers.get(r['legacyCareer'], .5)
                        c['upliftRemoved'] += proposed > old and sensitive_budget <= old * (1 + 2e-6)
            traces[artist]['reads'].append(trace)
        for artist, t in traces.items():
            t['openingFollowing'] = following0.get(artist)
        result['arms'][arm] = {'fixedCohorts': [{'year': y, 'group': g, **c} for (y, g), c in sorted(annual.items())],
            'singleLaunchBudgets': [{'year': y, 'group': g, **c} for (y, g), c in sorted(budget_groups.items())],
            'selectedActFollowingSensitivity': [{'year': y, 'context': context, 'factor': f, **c} for (y, context, f), c in sorted(sensitivity_counts.items())],
            'acts': traces}
        print(f'{name}: done', flush=True)
    if {x: r['openingFollowing'] for x, r in result['arms']['control']['acts'].items()} != {x: r['openingFollowing'] for x, r in result['arms']['treatment']['acts'].items()}:
        raise ValueError('Opening following differs between arms')
    return result


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--directory', type=Path, default=Path('SimLogs'))
    p.add_argument('--prefix', default='stature-marketing-decade-v2')
    p.add_argument('--seeds', nargs='+', type=int, default=[1001, 2002])
    p.add_argument('--output', type=Path, required=True)
    a = p.parse_args()
    if a.output.exists():
        raise FileExistsError(a.output)
    config = Path('SimTools/StatureShadowExperimental.json')
    report = {'sourcePrefix': a.prefix, 'analysisSha256': file_digest(Path(__file__)), 'calibrationSha256': file_digest(config),
        'caveats': ['Selected acts are largest opening-act Top 10 winners and losers in 1964 and 1968, selected after outcomes.',
                    'Runtime IDs are never matched across arms. Charted units cover chart rows only, not all sales.',
                    'All budget reads are repeated pre-cap requests, not paid dollars. Launch and follow-up payments are separate streams.',
                    'Following sensitivity changes normalized magnitudes only; observed reach, references, sales, TV and boundaries are frozen. It does not remove follower-origin sales or identify opening-seed descendants.',
                    'Initial rows and week-zero charts are checked exactly. No later outcome difference is an isolated act-level causal effect.'],
        'seeds': {}}
    policy = json.loads(config.read_text())['Policy']
    for seed in a.seeds:
        report['seeds'][str(seed)] = audit(a.directory, a.prefix, seed, policy)
    with a.output.open('x', encoding='utf-8') as f:
        json.dump(report, f, indent=2)
        f.write('\n')


if __name__ == '__main__':
    main()
