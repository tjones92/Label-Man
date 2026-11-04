"""Rack-jobber probe: racks arm against its matched control (SimTools/RackJobberChannelDesign.md).

Validates the flag-off replay against the clubs reference run, company cash and the rack telemetry's own
reconciliation (region rows = title rows; units never above shelf), then reports per settlement year:
store, club and rack LP units against Billboard; the rack split by tier, label tier and distribution; the
album-chart clog check from the LP demand audit; singles; closures; record gross; concentration; runtime.
Rack units are emergent from one fitted scale; the 1961 target is front-line racks only (36-42M).
"""
import argparse
import json
from pathlib import Path

import pandas as pd

from analyze_single_capital_risk import ROOT, read, replay

BILLBOARD = {1961: dict(racks=60, stores=68, clubs=45), 1962: dict(racks=76, stores=70, clubs=45),
             1963: dict(racks=79, stores=66, clubs=41), 1964: dict(racks=86, stores=68, clubs=38),
             1965: dict(racks=106, stores=76, clubs=46)}
FRONT_LINE_1961 = (36, 42)
INACTIVE = {'Bankrupt', 'Defunct', 'Acquired'}
TIERS = ['Major', 'MidTier', 'Independent', 'Boutique', 'Small']


def arm_rows(p, arm, seed, racks):
    sales = read(p, 'label-sales-weekly')
    company = read(p, 'company-monthly', usecols=['year', 'labelId', 'tier', 'recordGross', 'recordUnits', 'cashReconciliationError', 'pretaxEarnings', 'costOfSalesAndWriteOffs'])
    finance = read(p, 'label-finance', usecols=['week', 'year', 'labelId', 'labelTier', 'status'])
    concentration = read(p, 'concentration').set_index('year')
    compact = read(p, 'rack-store-weekly') if Path(f'{p}-rack-store-weekly.csv').exists() else None
    lp = None if compact is not None else read(p, 'lp-demand', usecols=['week', 'year', 'recordId', 'tier', 'observationKind', 'currentPosition', 'cleared'])
    clubs = read(p, 'record-club-annual')
    weekly_clubs = read(p, 'rack-club-weekly') if Path(f'{p}-rack-club-weekly.csv').exists() else None
    if company.cashReconciliationError.abs().max() > .02:
        raise ValueError(f'{arm} {seed}: company cash reconciliation fails')
    region = title = None
    rack_finance = None
    week_year = sales.groupby('week').year.first()
    if sales.groupby('week').year.nunique().max() != 1:
        raise ValueError('Settlement week has more than one year')
    if racks:
        region = read(p, 'rack-region-weekly')
        title = read(p, 'rack-title-weekly')
        if (region.units > region.shelf).any():
            raise ValueError('Rack units above shelf')
        if region.groupby(['week', 'regionId']).units.sum().sub(title.groupby(['week', 'regionId']).units.sum(), fill_value=0).abs().max() != 0:
            raise ValueError('Rack region and title rows disagree')
        # Rack raw rows use simulation date; store settlement date is seven days later.
        # Compare channels on one calendar boundary, retaining raw files unchanged.
        for frame in (region, title):
            frame['year'] = frame.week.map(week_year)
            if frame.year.isna().any():
                raise ValueError('Rack week absent from settlement calendar')
        if Path(f'{p}-rack-finance-weekly.csv').exists():
            rack_finance = read(p, 'rack-finance-weekly')
            if rack_finance.groupby('week').units.sum().sub(region.groupby('week').units.sum(), fill_value=0).abs().max() != 0:
                raise ValueError('Rack finance and units disagree')
            rate = {'returns10': .1, 'returns20': .2}.get(arm, 0)
            expected = rack_finance.soldCogs * rate / (1 - rate)
            if (rack_finance.returnWriteOff - expected).abs().gt(0.001 + expected.abs() * 0.00001).any():
                raise ValueError('Rack return costs disagree with the declared arm')
    rows = []
    for year in sorted(sales.year.unique()):
        s = sales[sales.year.eq(year)]
        store = s.loc[s['format'].eq('Album'), 'units'].sum() / 1e6
        club = (weekly_clubs.loc[weekly_clubs.year.eq(year), 'units'].sum() if weekly_clubs is not None else
                clubs.loc[clubs.year.eq(year) & clubs.role.ne('Licensor'), 'units'].sum()) / 1e6
        row = dict(arm=arm, seed=seed, year=year, storeLpM=store, clubLpM=club,
                   weeksObserved=s.week.nunique(), completeYear=52 <= s.week.nunique() <= 53,
                   singleM=s.loc[s['format'].eq('Single'), 'units'].sum() / 1e6,
                   c4ChartShare=concentration.loc[year, 'c4ChartShare'],
                   majorFamilyChartShare=concentration.loc[year, 'majorFamilyChartShare'])
        c = company[company.year.eq(year)]
        row['companyRecordUnitsM'] = c.recordUnits.sum() / 1e6
        for tier in TIERS:
            row[f'gross{tier}M'] = c.loc[c.tier.eq(tier), 'recordGross'].sum() / 1e6
            row[f'pretax{tier}M'] = c.loc[c.tier.eq(tier), 'pretaxEarnings'].sum() / 1e6
        row['companyCogsM'] = c.costOfSalesAndWriteOffs.sum() / 1e6
        yearEnd = finance[finance.year.le(year)].sort_values('week').groupby('labelId').last()
        closed = yearEnd[yearEnd.status.isin(INACTIVE)]
        for tier in TIERS:
            row[f'closed{tier}'] = int(closed.labelTier.eq(tier).sum())
        row['closedAll'] = len(closed)
        row['bankruptLabels'] = int(yearEnd.status.eq('Bankrupt').sum())
        row['defunctLabels'] = int(yearEnd.status.eq('Defunct').sum())
        row['absorbedLabels'] = int(yearEnd.status.eq('Acquired').sum())
        if compact is not None:
            q = compact[compact.year.eq(year)]
            if q.lpUnits.sum() != round(store * 1e6):
                raise ValueError('Compact LP units disagree with store sales')
            row['lpChartEntrants'] = q.launchChartEntrants.sum()
            row['liveLpsMeanWeek'] = q.groupby('week').liveLps.sum().mean()
            row['storeMajorLpShare'] = q.loc[q.labelTier.eq('Major'), 'lpUnits'].sum() / max(1, q.lpUnits.sum())
            row['chartMajorLpTitleWeekShare'] = q.loc[q.labelTier.eq('Major'), 'chartedLps'].sum() / max(1, q.chartedLps.sum())
        else:
            q = lp[lp.year.eq(year)]
            charted = q[q.currentPosition.gt(0)]
            row['lpChartEntrants'] = charted.loc[charted.observationKind.eq('Launch'), 'recordId'].nunique()
            row['liveLpsMeanWeek'] = q.groupby('week').recordId.nunique().mean()
            row['storeMajorLpShare'] = q.loc[q.tier.eq('Major'), 'cleared'].sum() / max(1, q.cleared.sum())
            row['chartMajorLpTitleWeekShare'] = charted.tier.eq('Major').mean()
            row['chartMajorLpUnitShare'] = charted.loc[charted.tier.eq('Major'), 'cleared'].sum() / max(1, charted.cleared.sum())
        rack = 0.0
        if racks:
            r = region[region.year.eq(year)]
            t = title[title.year.eq(year)]
            rack = r.units.sum() / 1e6
            row.update(rackLpM=rack, rackHitM=r.hitUnits.sum() / 1e6, rackCatalogueM=r.catalogueUnits.sum() / 1e6,
                       rackUnshippedShare=r.unshippedUnits.sum() / max(1, r.units.sum()),
                       rackShelfBoundShare=r.shelfBound.mean(), rackDemandM=r.demand.sum() / 1e6,
                       rackShelfM=r.shelf.sum() / 1e6, rackDistinctTitles=t.recordId.nunique(),
                       rackTop40Share=t.loc[t.chartPosition.between(1, 40), 'units'].sum() / max(1, t.units.sum()))
            for tier in TIERS:
                row[f'rack{tier}Share'] = t.loc[t.labelTier.eq(tier), 'units'].sum() / max(1, t.units.sum())
            row['selectedStoreMajorShare'] = t.loc[t.labelTier.eq('Major'), 'storeUnits'].sum() / max(1, t.storeUnits.sum())
            row['rackMajorTitleWeekShare'] = t.labelTier.eq('Major').mean()
            for code, name in ((1, 'Hit'), (2, 'Catalogue')):
                tier_rows = t[t.tier.eq(code)]
                row[f'rack{name}MajorShare'] = tier_rows.loc[tier_rows.labelTier.eq('Major'), 'units'].sum() / max(1, tier_rows.units.sum())
            row['rackMajorVsStorePp'] = 100 * (row['rackMajorShare'] - row['storeMajorLpShare'])
            if rack_finance is not None:
                f = rack_finance[rack_finance.year.eq(year)]
                row['rackGrossM'] = f.gross.sum() / 1e6
                row['rackSoldCogsM'] = f.soldCogs.sum() / 1e6
                row['rackReturnWriteOffM'] = f.returnWriteOff.sum() / 1e6
                row['frozenReturn10CostM'] = f.soldCogs.sum() / 9e6
                row['frozenReturn20CostM'] = f.soldCogs.sum() / 4e6
        row['storesPlusRacksM'] = store + rack
        row['totalLpM'] = store + club + rack
        row['rackShareOfStoresPlusRacks'] = rack / (store + rack) if store + rack else 0
        rows.append(row)
    return rows


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--tag', required=True)
    parser.add_argument('--control-tag', help='Tag holding the control arm, when not in --tag')
    parser.add_argument('--include-tag', action='append', default=[], help='Additional completed probe tags to combine')
    parser.add_argument('--skip-replay', action='store_true', help='Use separately recorded replay evidence')
    args = parser.parse_args()
    plan = json.loads((ROOT / args.tag / 'plan.json').read_text(encoding='utf-8-sig'))
    meta = dict(tag=args.tag, assemblySha256=plan['assemblySha256'], scale=plan['scale'] or .026, scaleOverride=plan['scale'], weeks=plan['weeks'],
                billboard=BILLBOARD, frontLine1961=FRONT_LINE_1961, runs={})
    if not args.skip_replay and any(c['check'] == 'off13' for c in plan['checks']):
        meta['offReplay'] = replay(ROOT / plan['replayRun'], ROOT / f"{args.tag}-off13")
        meta['offReplay'] = dict(throughWeek=meta['offReplay']['throughWeek'], identicalStreams=len(meta['offReplay']['identicalStreams']))
    runs = list(plan['runs'])
    if args.control_tag:
        control = json.loads((ROOT / args.control_tag / 'plan.json').read_text(encoding='utf-8-sig'))
        runs += [r for r in control['runs'] if r['arm'] == 'control']
    for tag in args.include_tag:
        additional = json.loads((ROOT / tag / 'plan.json').read_text(encoding='utf-8-sig'))
        if additional['assemblySha256'] != plan['assemblySha256']:
            raise ValueError('Combined new probes must use the same assembly')
        runs += additional['runs']
    if not runs or any(not r['passed'] for r in runs):
        raise ValueError('Only completed successful runs may be analyzed')
    rows = []
    for run in runs:
        run_rows = arm_rows(ROOT / run['name'], run['arm'], run['seed'], run['arm'] != 'control')
        for row in run_rows:
            row['run'] = run['name']
            row['durationWeeks'] = int(next(a.split('=')[1] for a in run['args'] if a.startswith('--weeks=')))
        rows += run_rows
        meta['runs'][run['name']] = round(run['seconds'])
    table = pd.DataFrame(rows)
    for _, group in table.groupby('run'):
        base = group[group.year.eq(1961) & group.completeYear]
        if len(base) != 1:
            continue
        b = base.iloc[0]
        for index, row in group.iterrows():
            table.loc[index, 'storeGrowthVs1961'] = row.storeLpM / b.storeLpM
            if 'rackLpM' in row and pd.notna(row.rackLpM) and b.rackLpM > 0:
                table.loc[index, 'rackGrowthVs1961'] = row.rackLpM / b.rackLpM
                table.loc[index, 'shelfGrowthVs1961'] = row.rackShelfM / b.rackShelfM
            if row.year in BILLBOARD:
                table.loc[index, 'historicalStoreGrowthVs1961'] = BILLBOARD[row.year]['stores'] / BILLBOARD[1961]['stores']
                table.loc[index, 'historicalRackGrowthVs1961'] = BILLBOARD[row.year]['racks'] / BILLBOARD[1961]['racks']
    out = ROOT / args.tag / 'review'
    out.mkdir(parents=True, exist_ok=True)
    table.to_csv(out / 'annual.csv', index=False)
    # The 106- and 318-week seed-1001 baselines repeat the same early years.
    # Count each independent seed once in the robustness summary.
    independent = table[table.completeYear].drop_duplicates(['arm', 'seed', 'year'])
    metrics = ['storeLpM', 'rackLpM', 'totalLpM', 'singleM', 'storeMajorLpShare', 'rackMajorShare', 'closedAll', 'c4ChartShare']
    independent.reindex(columns=['arm', 'year'] + metrics).groupby(['arm', 'year']).agg(['count', 'mean', 'min', 'max']).to_csv(out / 'seed-summary.csv')
    # Pair only identical seed and observed-year length; a partial final year is never annualized.
    paired = []
    for (duration, _, seed, year), group in table.groupby(['durationWeeks', 'weeksObserved', 'seed', 'year']):
        for _, treated in group[group.arm.ne('control')].iterrows():
            base_arm = 'control' if treated.arm == 'racks' else 'racks'
            baseline = group[group.arm.eq(base_arm)]
            if len(baseline) != 1:
                continue
            base = baseline.iloc[0]
            result = dict(arm=treated.arm, baseline=base_arm, seed=int(seed), year=int(year), durationWeeks=int(duration), completeYear=bool(treated.completeYear))
            for metric in ['storeLpM', 'singleM', 'totalLpM', 'lpChartEntrants', 'liveLpsMeanWeek', 'closedAll',
                           'grossMajorM', 'grossMidTierM', 'pretaxMajorM', 'pretaxMidTierM', 'c4ChartShare',
                           'bankruptLabels', 'defunctLabels', 'absorbedLabels']:
                result[metric + 'Delta'] = treated[metric] - base[metric]
            paired.append(result)
    pd.DataFrame(paired).to_csv(out / 'paired.csv', index=False)
    meta['calendar'] = 'Rack week mapped to frozen store settlement year; compact probes also record club weekly units on that boundary. Older club annual rows retain their original booking year.'
    meta['historicalScope'] = '1961-65 older series only; all-rack historical units include unmodelled budget LPs'
    (out / 'validation.json').write_text(json.dumps(meta, indent=2), encoding='utf-8')
    pd.set_option('display.width', 200)
    columns = ['arm', 'seed', 'year', 'completeYear', 'storeLpM', 'rackLpM', 'totalLpM', 'singleM', 'storeMajorLpShare', 'rackMajorShare', 'closedAll']
    print(table.reindex(columns=columns).to_string(index=False, float_format=lambda v: f'{v:.3f}'))
    print(json.dumps(meta, indent=2))


if __name__ == '__main__':
    main()
