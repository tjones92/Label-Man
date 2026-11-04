"""Record-club probe: clubs (and hold) arms against matched controls, across seeds and run tags.

Each tag's control is its own 'control' arm when the plan has one, otherwise the external control plan's observe
run for the same seed (rcl-v1 used lpd-v2). Validates the flag-off replay, the channel's exact annual volume and
company cash, then reports store and club LP units, the club split against the FTC's Columbia figures, closures
by tier, record gross by tier and chart concentration, with paired clubs-minus-control deltas across seeds.
Club units are authored from Billboard's series, not earned; the report says so.
"""
import argparse
import json

import numpy as np
import pandas as pd

from analyze_single_capital_risk import ROOT, read, replay

ANNUAL_M = {1960: 45, 1961: 45, 1962: 45, 1963: 41, 1964: 38, 1965: 46, 1966: 47, 1967: 49, 1968: 49, 1969: 49}
INACTIVE = {'Bankrupt', 'Defunct', 'Acquired'}
TIERS = ['Major', 'MidTier', 'Independent', 'Boutique', 'Small']


def album_concentration(sales, year, directory):
    # The album-chart stream is empty in calibration runs; charted LP units by label stand in for it.
    s = sales[sales.year.eq(year) & sales['format'].eq('Album')].groupby('labelId').chartedUnits.sum().sort_values(ascending=False)
    tier = directory.initialTier.reindex(s.index)
    return dict(albumC4=s.head(4).sum() / s.sum(), albumMajorShare=s[tier.eq('Major').values].sum() / s.sum())


def arm_summary(p, arm, seed, clubs):
    sales = read(p, 'label-sales-weekly')
    directory = read(p, 'label-directory').set_index('labelId')
    finance = read(p, 'label-finance', usecols=['week', 'year', 'labelId', 'labelTier', 'status'])
    company = read(p, 'company-monthly', usecols=['year', 'labelId', 'tier', 'recordGross', 'cashReconciliationError'])
    concentration = read(p, 'concentration').set_index('year')
    if company.cashReconciliationError.abs().max() > .02:
        raise ValueError(f'{arm} {seed}: company cash reconciliation fails')
    rows = []
    for year in (1960, 1961):
        s = sales[sales.year.eq(year)]
        row = dict(arm=arm, seed=seed, year=year,
                   storeLpM=s.loc[s['format'].eq('Album'), 'units'].sum() / 1e6,
                   singleM=s.loc[s['format'].eq('Single'), 'units'].sum() / 1e6,
                   c4ChartShare=concentration.loc[year, 'c4ChartShare'],
                   majorFamilyChartShare=concentration.loc[year, 'majorFamilyChartShare'])
        row.update(album_concentration(sales, year, directory))
        c = company[company.year.eq(year)]
        for tier in TIERS:
            row[f'gross{tier}M'] = c.loc[c.tier.eq(tier), 'recordGross'].sum() / 1e6
        # Closures to date by the tier each label held when it closed, including labels founded during the run.
        yearEnd = finance[finance.year.le(year)].sort_values('week').groupby('labelId').last()
        closed = yearEnd[yearEnd.status.isin(INACTIVE)]
        for tier in TIERS:
            row[f'closed{tier}'] = int(closed.labelTier.eq(tier).sum())
        row['closedAll'] = len(closed)
        if clubs is not None:
            a = clubs[clubs.year.eq(year)]
            club = a[a.role.isin(['OperatorOwn', 'OperatorLicensed'])]
            row.update(clubM=a.loc[a.role.ne('Licensor'), 'units'].sum() / 1e6, clubNetM=a.net.sum() / 1e6)
            byOperator = club.groupby('labelId').units.sum()
            names = directory.labelName.reindex(byOperator.index)
            for name, units in zip(names, byOperator):
                row[f'share_{name.split()[0]}'] = units / byOperator.sum()
            col = byOperator.index[names.str.startswith('Columbia')]
            if len(col):
                cu = club[club.labelId.isin(col)]
                row['columbiaLicensedShare'] = cu.loc[cu.role.eq('OperatorLicensed'), 'units'].sum() / cu.units.sum()
            row['totalLpM'] = row['storeLpM'] + row['clubM']
        else:
            row['totalLpM'] = row['storeLpM']
        rows.append(row)
    last = finance.sort_values('week').groupby('labelId').last()
    end = dict(arm=arm, seed=seed, year='runEnd', closedAll=int(last.status.isin(INACTIVE).sum()))
    for tier in TIERS:
        end[f'closed{tier}'] = int((last.status.isin(INACTIVE) & last.labelTier.eq(tier)).sum())
    rows.append(end)
    return rows


def validate_clubs(clubs, arm, seed):
    for year in (1960, 1961):
        sold = clubs.loc[clubs.year.eq(year) & clubs.role.ne('Licensor'), 'units'].sum()
        if sold != ANNUAL_M[year] * 1_000_000:
            raise ValueError(f'{arm} {seed} {year}: club units {sold} != authored volume')
        inflow = clubs.loc[clubs.year.eq(year) & clubs.role.eq('OperatorLicensed'), 'units'].sum()
        outflow = clubs.loc[clubs.year.eq(year) & clubs.role.eq('Licensor'), 'units'].sum()
        if inflow != outflow:
            raise ValueError(f'{arm} {seed} {year}: licensed units do not balance')


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--tags', nargs='+', default=['rcl-v1'])
    parser.add_argument('--out', default=None)
    args = parser.parse_args()
    rows, meta = [], dict(tags={}, calibrationAccepted=False,
                          caveats=['Club volume is authored from Billboard, not earned.',
                                   'Paired seed deltas include each seed\'s stochastic reshuffle.',
                                   'Opening backlist size is an assumption (150 first weeks of LP sales).'])
    for tag in args.tags:
        plan = json.loads((ROOT / tag / 'plan.json').read_text(encoding='utf-8-sig'))
        if not all(c['passed'] for c in plan['checks']):
            raise ValueError(f'{tag}: a pre-run check failed')
        runs = {(r['arm'], r['seed']): r for r in plan['runs']}
        if not all(r['passed'] and r['exitCode'] == 0 for r in runs.values()):
            raise ValueError(f'{tag}: failed run')
        external = None
        if not any(arm == 'control' for arm, _ in runs):
            external = json.loads((ROOT / plan['control'] / 'plan.json').read_text(encoding='utf-8-sig'))
        def control_path(seed):
            if external is None:
                return ROOT / runs['control', seed]['name']
            return ROOT / next(r['name'] for r in external['runs'] if r['arm'] == 'observe' and r['seed'] == seed)
        off = replay(control_path(plan['seeds'][0]), ROOT / (tag + '-off13'), 12)
        meta['tags'][tag] = dict(assemblySha256=plan['assemblySha256'], seeds=plan['seeds'], offReplayStreams=len(off['identicalStreams']),
                                 seconds={f'{a}-{s}': r['seconds'] for (a, s), r in runs.items()})
        for seed in plan['seeds']:
            rows += arm_summary(control_path(seed), 'control', seed, None)
            for (arm, s), run in sorted(runs.items()):
                if s != seed or arm == 'control':
                    continue
                clubs = read(ROOT / run['name'], 'record-club-annual')
                validate_clubs(clubs, arm, seed)
                rows += arm_summary(ROOT / run['name'], arm, seed, clubs)
    summary = pd.DataFrame(rows)
    out = ROOT / (args.out or args.tags[-1]) / 'review'
    out.mkdir(exist_ok=True)
    summary.to_csv(out / 'summary-seeds.csv', index=False)

    # Paired clubs-minus-control deltas by seed, with the mean and its standard error.
    metrics = [c for c in summary.columns if c not in ('arm', 'seed', 'year') and pd.api.types.is_numeric_dtype(summary[c])]
    keyed = summary.set_index(['arm', 'seed', 'year'])
    deltas = []
    for (arm, seed, year), row in keyed.iterrows():
        if arm == 'control':
            continue
        base = keyed.loc[('control', seed, year)]
        deltas.append(dict(arm=arm, seed=seed, year=year, **{m: row[m] - base[m] for m in metrics if pd.notna(base.get(m))}))
    deltas = pd.DataFrame(deltas)
    deltas.to_csv(out / 'paired-deltas.csv', index=False)
    pooled = deltas.groupby(['arm', 'year'])[[m for m in metrics if m in deltas]].agg(['mean', 'sem', 'count'])
    pooled.to_csv(out / 'paired-deltas-pooled.csv')
    (out / 'validation-seeds.json').write_text(json.dumps(meta, indent=2), encoding='utf-8')
    pd.set_option('display.width', 250)
    pd.set_option('display.max_columns', 60)
    show = ['storeLpM', 'totalLpM', 'singleM', 'closedAll', 'closedMajor', 'closedMidTier', 'closedIndependent', 'closedBoutique', 'closedSmall',
            'c4ChartShare', 'majorFamilyChartShare', 'albumC4', 'albumMajorShare', 'grossMajorM', 'grossMidTierM', 'grossIndependentM',
            'share_Columbia', 'share_Capitol', 'share_RCA', 'columbiaLicensedShare']
    print(summary.set_index(['arm', 'seed', 'year'])[[c for c in show if c in summary]].round(3).T.to_string())
    print(deltas.groupby(['arm', 'year'])[[c for c in show if c in deltas]].agg(['mean', 'sem']).round(3).T.to_string())
    print(json.dumps(meta, indent=2))


if __name__ == '__main__':
    main()
