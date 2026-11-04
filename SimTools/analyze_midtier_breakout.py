"""Matched MidTier repair + breakout-planning validation. Outputs go under SimLogs.

Usage: python SimTools/analyze_midtier_breakout.py --tag mbk-v1
The archived baseline is reusable only after the fresh off replay matches all its week-keyed CSVs.
52-week follow-up cohorts exclude opening records, first hits before week 5, and right-censored acts.
Groups are frozen at the selling label of each act's first non-opening Top 10.
"""
import argparse
import csv
import itertools
import json
import math
import statistics
from pathlib import Path

import pandas as pd

from analyze_hit_act_cadence import annual_metrics
from analyze_offchart_screen import ROOT, path, screen
from analyze_studio_parent import BIG5, HISTORY_BIG5, HISTORY_STUDIO, studio_ids


def replay_identity(tag, baseline, seed, baseline_weeks, through=52):
    prefix = f'{baseline}-default-{seed}-{baseline_weeks}-'
    reference = sorted(Path(ROOT).glob(prefix + '*.csv'))
    if not reference:
        raise ValueError('Missing baseline CSVs')
    checked = []
    for old in reference:
        stream = old.name[len(prefix):-4]
        with old.open(encoding='utf-8-sig', newline='') as f:
            header = next(csv.reader(f))
        if 'week' not in header:
            continue
        new = Path(path(tag, seed, stream, 53))
        if not new.exists():
            raise ValueError(f'Missing replay stream: {stream}')
        def rows(file):
            with file.open(encoding='utf-8-sig', newline='') as f:
                reader = csv.reader(f)
                columns = next(reader)
                if columns != header:
                    raise ValueError(f'Replay schema differs: {stream}')
                index = columns.index('week')
                yield from (row for row in reader if row[index] and int(row[index]) <= through)
        for number, (a, b) in enumerate(itertools.zip_longest(rows(old), rows(new)), 1):
            if a != b:
                raise ValueError(f'Off replay differs: {stream}, filtered row {number}')
        checked.append(stream)
    if len(checked) < 50:
        raise ValueError(f'Incomplete identity check: only {len(checked)} streams')
    return {'seed': seed, 'throughWeek': through, 'streams': len(checked), 'allRowsIdentical': True}


def followup_cohorts(records, launches, opening, end_week):
    hits = records[records.currentPosition.between(1, 10) & ~records.recordId.isin(opening)]
    hits = hits.sort_values(['week', 'currentPosition', 'recordId']).drop_duplicates('artistId')
    hits = hits[hits.week.between(5, end_week - 52)]
    debuts = records[records.currentPosition.between(1, 100) & ~records.recordId.isin(opening)]
    debuts = debuts.sort_values(['week', 'recordId']).drop_duplicates('recordId')
    first_top10 = records[records.currentPosition.between(1, 10)].groupby('recordId').week.min()
    rows = []
    launch_by_act = {key: frame for key, frame in launches.groupby('artistId')}
    debut_by_act = {key: frame for key, frame in debuts.groupby('artistId')}
    for hit in hits.itertuples():
        q = launch_by_act.get(hit.artistId, launches.iloc[:0])
        q = q[q.week.between(hit.week + 1, hit.week + 52)].sort_values('week')
        d = debut_by_act.get(hit.artistId, debuts.iloc[:0])
        d = d[d.week.between(hit.week + 1, hit.week + 52) & (d.recordId != hit.recordId)].sort_values('week')
        charted = q[q.singleId.isin(d.recordId)]
        top10 = q[q.singleId.map(first_top10).between(hit.week + 1, hit.week + 52)]
        rows.append({'artistId': hit.artistId, 'group': hit.group, 'hitYear': hit.year, 'hitWeek': hit.week,
                     'releases52': len(q), 'chartedFollowups52': len(charted), 'top10Followups52': len(top10),
                     'noRelease52': int(q.empty), 'noDebut52': int(d.empty),
                     'firstReleaseGap': None if q.empty else int(q.week.iloc[0] - hit.week),
                     'firstDebutGap': None if d.empty else int(d.week.iloc[0] - hit.week),
                     'subsequentDebutGaps': [int(v) for v in d.week.diff().dropna()]})
    return rows


def measure(tag, seed, weeks):
    p = lambda stream: path(tag, seed, stream, weeks)
    records = pd.read_csv(p('records'), usecols=['week', 'year', 'artistId', 'recordId', 'labelId', 'currentPosition'])
    launches = pd.read_csv(p('single-launch-quality'))
    if records.duplicated(['week', 'recordId']).any() or launches.singleId.duplicated().any():
        raise ValueError('Duplicate chart observation or launch')
    studios = studio_ids(p)
    records['group'] = records.labelId.map(lambda label: 'big5' if label in BIG5 else 'studio' if label in studios else 'field')
    census = pd.read_csv(p('demand-release-census'), usecols=['recordId', 'observationKind'])
    opening = set(census.loc[census.observationKind == 'Opening', 'recordId'])
    annual = annual_metrics(records, launches, opening)
    cohorts = followup_cohorts(records, launches, opening, weeks)
    fin = pd.read_csv(p('label-finance'), usecols=['week', 'year', 'labelId', 'labelTier', 'status'])
    mid = set(fin.loc[(fin.week == 1) & (fin.labelTier == 'MidTier'), 'labelId'])
    field_mid = mid - BIG5 - studios
    clearing = pd.read_csv(p('market-clearing-weekly'), usecols=['year', 'serviceableSingleIntent', 'clearedSingleUnits'])
    sales = pd.read_csv(p('label-sales-weekly'))
    for year, row in annual.items():
        q = launches[launches.year == year]
        c = clearing[clearing.year == year]
        row['singles'] = len(q)
        row['r'] = c.clearedSingleUnits.sum() / max(1, c.serviceableSingleIntent.sum())
        s = sales[(sales.year == year) & (sales.format == 'Single')]
        row['top10UnitsM'] = s.top10Units.sum() / 1e6
        row['singleUnitsM'] = s.units.sum() / 1e6
        at = fin[fin.year <= year].sort_values('week').drop_duplicates('labelId', keep='last')
        closed = set(at.loc[at.status.isin(['Bankrupt', 'Defunct']), 'labelId'])
        for label, ids in [('openingMid', mid), ('openingFieldMid', field_mid)]:
            row[f'{label}Survivors'] = len(ids - closed)
            row[f'{label}Closed'] = len(ids & closed)
            row[f'{label}SinglesPerInitialLabel'] = q.labelId.isin(ids).sum() / max(1, len(ids))
    guards = screen(tag, seed, weeks)
    slate = pd.read_csv(p('release-slate'), usecols=['slotId', 'state', 'source', 'evidence', 'reason'])
    tagged = slate[slate.evidence.fillna('').str.contains(';breakoutPlanning=1', regex=False)]
    for name, mask in [('reassigned', tagged.reason == 'breakout-budget-slot-reassigned'),
                       ('reviewedOwn', tagged.reason == 'breakout-budget-follow-up-reviewed'),
                       ('issued', tagged.state == 'Issued'), ('cancelled', tagged.state == 'Cancelled')]:
        guards[f'breakout_{name}'] = int(tagged.loc[mask, 'slotId'].nunique())
    outcomes = pd.read_csv(p('release-outcomes'), usecols=['recordId', 'format', 'realizedNet'])
    mature_ids = set(launches.loc[launches.week <= weeks - 52, 'singleId'])
    mature = outcomes[(outcomes.format == 'Single') & outcomes.recordId.isin(mature_ids)].drop_duplicates('recordId')
    guards['matureSinglesRetired'] = len(mature)
    guards['matureSinglesStillActive'] = len(mature_ids - set(mature.recordId))
    guards['matureRetiredSingleLossShare'] = float((mature.realizedNet < 0).mean()) if len(mature) else None
    return annual, cohorts, guards


def historical():
    out = {}
    for year in range(1960, 1965):
        h = pd.read_csv(Path(ROOT) / f'historical-top10-{year}' / f'records_{year}_grouped.csv')
        h = h.sort_values('first')
        assignments = h.drop_duplicates('artist').set_index('artist').owned
        counts = h.groupby('artist').title.nunique()
        row = {'repeat2': int((counts >= 2).sum()), 'repeat3': int((counts >= 3).sum())}
        for group, owners in [('big5', HISTORY_BIG5), ('studio', HISTORY_STUDIO)]:
            row[f'{group}_repeat2'] = int(((counts >= 2) & assignments.isin(owners)).sum())
        row['field_repeat2'] = row['repeat2'] - row['big5_repeat2'] - row['studio_repeat2']
        out[year] = row
    return out


def validate_manifests(plan):
    baseline = json.loads((Path(ROOT) / plan['baseline'] / 'runs.json').read_text(encoding='utf-8-sig'))
    base_flags = baseline['extraFlags']
    repairs = ['--slate-work-budget=0.67', '--slate-work-budget-midtier=1', '--slate-going-concern-reserve']
    for arm, tag in [('baseline', plan['baseline']), *plan['arms'].items()]:
        m = json.loads((Path(ROOT) / tag / 'runs.json').read_text(encoding='utf-8-sig'))
        expected = base_flags + (repairs if arm != 'baseline' else []) + (['--slate-breakout-planning'] if arm == 'breakout' else [])
        if sorted(m['extraFlags']) != sorted(expected) or m['profile'] != 'game' or m['weeks'] != plan['weeks']:
            raise ValueError(f'Mismatched configuration: {arm}')
        if arm != 'baseline' and m['assemblySha256'] != plan['assemblySha256']:
            raise ValueError(f'Assembly differs: {arm}')
        if sorted(m['seeds']) != sorted(plan['seeds']) or m['modes'] != ['default']:
            raise ValueError(f'Mismatched seeds or modes: {arm}')
        if len(m['results']) != len(plan['seeds']) or {r['seed'] for r in m['results']} != set(plan['seeds']) or not all(r['passed'] for r in m['results']):
            raise ValueError(f'Incomplete run cohort: {arm}')


def analyze(tag):
    output = Path(ROOT) / tag
    plan = json.loads((output / 'plan.json').read_text(encoding='utf-8-sig'))
    if plan['status'] not in ('native-complete', 'review-complete') or not all(c['passed'] for c in plan['checks']) or len(plan['checks']) != 4:
        raise ValueError('Native validation incomplete')
    validate_manifests(plan)
    identity = replay_identity(tag + '-off', plan['baseline'], plan['seeds'][0], plan['weeks'])
    annual_rows, cohort_rows, guard_rows = [], [], []
    for arm, run in [('baseline', plan['baseline']), *plan['arms'].items()]:
        for seed in plan['seeds']:
            annual, cohorts, guards = measure(run, seed, plan['weeks'])
            annual_rows.extend(dict(arm=arm, seed=seed, year=year, **row) for year, row in annual.items())
            cohort_rows.extend(dict(arm=arm, seed=seed, **row) for row in cohorts)
            guard_rows.append(dict(arm=arm, seed=seed, **guards))
    a, c, g = pd.DataFrame(annual_rows), pd.DataFrame(cohort_rows), pd.DataFrame(guard_rows)
    a.to_csv(output / 'annual.csv', index=False); g.to_csv(output / 'guards.csv', index=False)
    c.to_json(output / 'cohorts.json', orient='records', indent=2)
    cohort_summary = []
    for (arm, seed, group), rows in c.groupby(['arm', 'seed', 'group']):
        gaps = list(itertools.chain.from_iterable(rows.subsequentDebutGaps))
        cohort_summary.append(dict(arm=arm, seed=int(seed), group=group, acts=len(rows),
            **{key: rows[key].mean() for key in ['releases52', 'chartedFollowups52', 'top10Followups52', 'noRelease52', 'noDebut52']},
            firstReleaseGap=rows.firstReleaseGap.median(), firstDebutGap=rows.firstDebutGap.median(),
            subsequentDebutGap=statistics.median(gaps) if gaps else None))
    cs = pd.DataFrame(cohort_summary); cs.to_csv(output / 'cohort-summary.csv', index=False)
    hist = historical()
    lines = ['# MidTier budget and earlier follow-ups', '',
             'Three matched seeds, 265 weeks. Baseline reused after a fresh off replay: ' + str(identity['streams']) + ' identical week-keyed streams through week 52.',
             'Native checks include whole-world/gzip save-load and resume. Counts include annual carryovers.', '',
             '| Year | History 2+ / 3+ | Baseline | Repair | Repair + earlier planning |', '|---|---:|---:|---:|---:|']
    means = a.groupby(['arm', 'year']).mean(numeric_only=True)
    for year, h in hist.items():
        cells = [f"{means.loc[(arm, year), 'repeat2']:.1f} / {means.loc[(arm, year), 'repeat3']:.1f}" for arm in ['baseline', 'repair', 'breakout']]
        lines.append(f"| {year} | {h['repeat2']} / {h['repeat3']} | " + ' | '.join(cells) + ' |')
    lines += ['', '| Year | History field 2+ | Baseline | Repair | Repair + earlier planning |', '|---|---:|---:|---:|---:|']
    for year, h in hist.items():
        lines.append(f"| {year} | {h['field_repeat2']} | " + ' | '.join(f"{means.loc[(arm, year), 'field_repeat2']:.1f}" for arm in ['baseline', 'repair', 'breakout']) + ' |')
    lines += ['', '| Field hit cohort (seed mean) | Baseline | Repair | Repair + earlier planning |', '|---|---:|---:|---:|']
    cm = cs[cs.group == 'field'].groupby('arm').mean(numeric_only=True)
    for key in ['acts', 'releases52', 'chartedFollowups52', 'noRelease52', 'firstReleaseGap', 'firstDebutGap', 'subsequentDebutGap']:
        lines.append('| ' + key + ' | ' + ' | '.join(f'{cm.loc[arm, key]:.2f}' for arm in ['baseline', 'repair', 'breakout']) + ' |')
    paired = {}
    for treatment, control in [('repair', 'baseline'), ('breakout', 'repair')]:
        paired[treatment + '-minus-' + control] = {}
        for year in range(1960, 1965):
            for metric in ['repeat2', 'repeat3', 'field_repeat2', 'big5_repeat2', 'studio_repeat2', 'r', 'top10UnitsM', 'openingFieldMidSurvivors']:
                values = []
                for seed in plan['seeds']:
                    select = a[(a.seed == seed) & (a.year == year)].set_index('arm')
                    values.append(float(select.loc[treatment, metric] - select.loc[control, metric]))
                paired[treatment + '-minus-' + control][f'{year}_{metric}'] = dict(mean=statistics.mean(values), se=statistics.stdev(values) / math.sqrt(len(values)), perSeed=values)
    (output / 'review.json').write_text(json.dumps(dict(identity=identity, history=hist, paired=paired), indent=2), encoding='utf-8')
    lines += ['', '52-week cohorts require a first non-opening Top 10 in weeks 5 through end minus 52. Silent acts remain in denominators;',
              'gap medians describe only acts with the corresponding event. Timing alone is not adoption: read annual.csv and guards.csv',
              'for units, rationing, owner-group repeats, closures and first-hit breadth. Paired seed differences and SEs: review.json.',
              'Four releases a year is an inferred working aim, not a historical quota.']
    (output / 'review.md').write_text('\n'.join(lines), encoding='utf-8')
    print('\n'.join(lines))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--tag', default='mbk-v1')
    parser.add_argument('--identity-only', action='store_true')
    args = parser.parse_args()
    if args.identity_only:
        plan = json.loads((Path(ROOT) / args.tag / 'plan.json').read_text(encoding='utf-8-sig'))
        print(json.dumps(replay_identity(args.tag + '-off', plan['baseline'], plan['seeds'][0], plan['weeks'])))
    else:
        analyze(args.tag)
