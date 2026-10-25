"""LP age/cohort diagnosis, frozen-state shadows, and a live catalogue-decay comparison.

python SimTools/analyze_lp_demand_probe.py --tag lpd-v2
Shadows hold awareness, sales history, stock, and the population of surviving records fixed.
They are raw-demand sensitivities, not allocated sales or additive causal explanations.
"""
import argparse
import json
from pathlib import Path

import numpy as np
import pandas as pd

from analyze_single_capital_risk import ROOT, manifest, prefix, read, replay


SUMS = ['rawBeforeCannibalization', 'rawDemand', 'serviceable', 'cleared', 'rawLegacyDecay',
        'rawSlowDecay', 'rawNoNormalDecay', 'rawNoExhaustion', 'rawFullAwareness']


def grouped(frame, keys):
    rows = []
    for key, group in frame.groupby(keys, observed=True, dropna=False):
        key = key if isinstance(key, tuple) else (key,)
        row = dict(zip(keys, key))
        row.update(titles=group.recordId.nunique(), activeTitleWeeks=len(group),
                   meanUnitsPerTitleWeek=group.cleared.mean(),
                   poolWeightedAwareness=group.poolAwareSum.sum() / max(1, group.buyerPoolSum.sum()),
                   medianQuality=group.drop_duplicates('recordId').quality.median())
        row.update({column + 'M': group[column].sum() / 1e6 for column in SUMS})
        rows.append(row)
    return pd.DataFrame(rows)


def validate(p):
    q = read(p, 'lp-demand')
    if q.duplicated(['week', 'recordId']).any():
        raise ValueError('Repeated LP record/week')
    numeric = q.select_dtypes(include='number')
    if not np.isfinite(numeric.to_numpy()).all() or (numeric < 0).any().any():
        raise ValueError('Invalid LP demand values')
    if (q.cleared > q.serviceable).any() or (q.serviceable > q.stockBeforeSale).any():
        raise ValueError('LP allocation or stock violation')
    for shadow in ['rawNoNormalDecay', 'rawNoExhaustion', 'rawFullAwareness', 'rawBeforeCannibalization']:
        if (q[shadow] + np.maximum(.01, q.rawDemand * 1e-6) < q.rawDemand).any():
            raise ValueError('Nonmonotone shadow: ' + shadow)
    market = read(p, 'market-clearing-weekly')
    sales = read(p, 'label-sales-weekly')
    m = market.groupby(['week', 'year'])
    demand = q.groupby(['week', 'year'])
    pd.testing.assert_series_equal(demand.cleared.sum(), m.clearedAlbumUnits.sum(), check_names=False, check_dtype=False)
    pd.testing.assert_series_equal(demand.serviceable.sum(), m.serviceableAlbumIntent.sum(), check_names=False, check_dtype=False)
    if not np.allclose(demand.rawDemand.sum(), m.rawAlbumDemand.sum(), rtol=5e-6, atol=.1):
        raise ValueError('Raw LP demand fails market reconciliation')
    lp_sales = sales[sales['format'].eq('Album')].groupby(['week', 'year']).units.sum()
    pd.testing.assert_series_equal(demand.cleared.sum(), lp_sales, check_names=False, check_dtype=False)
    if market[['inventoryViolationCount', 'allocationViolationCount', 'reconciliationDelta', 'settlementDelta']].abs().max().max() > 0:
        raise ValueError('Market invariant violation')
    company = read(p, 'company-monthly')
    if company.cashReconciliationError.abs().max() > .02:
        raise ValueError('Company cash reconciliation fails')
    census = read(p, 'demand-release-census')
    census = census[census['format'].eq('Album')].set_index('recordId', verify_integrity=True)
    if not q.recordId.isin(census.index).all():
        raise ValueError('LP not in census')
    if not q.observationKind.eq(q.recordId.map(census.observationKind)).all():
        raise ValueError('Opening/launch identity mismatch')
    q['cohort'] = np.where(q.observationKind.eq('Opening'), 'opening', q.releaseYear.astype(str))
    q['ageBand'] = pd.cut(q.ageWeeks, [-1, 8, 26, 52, 104, np.inf], labels=['0-8', '9-26', '27-52', '53-104', '105+'])
    q['soundtrack'] = q.albumFormat.eq('Soundtrack')
    return q, census, market, sales


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--tag', default='lpd-v2')
    args = parser.parse_args()
    plan = json.loads((ROOT / args.tag / 'plan.json').read_text(encoding='utf-8-sig'))
    if len(plan['checks']) != 3 or not all(c['passed'] for c in plan['checks']):
        raise ValueError('Native, off replay run, and roundtrip must pass')
    reference = manifest(plan['reference'])
    if reference['extraFlags'] != plan['bundle']:
        raise ValueError('Reference bundle differs')
    out = ROOT / args.tag / 'review'
    out.mkdir(exist_ok=True)
    outputs = {name: [] for name in ['annual', 'cohorts', 'ages', 'formats', 'tiers', 'genres', 'first26', 'weekly', 'retirements', 'chart-spells']}
    observed_capacities = {}
    validation = []
    replays = []
    first_seed = plan['seeds'][0]
    off = replay(prefix(plan['reference'], first_seed, reference['weeks']), ROOT / (args.tag + '-off13'), 12)
    replays.append(dict(kind='off', seed=first_seed, through=12, streams=len(off['identicalStreams'])))
    for seed in plan['seeds']:
        runs = [r for r in plan['runs'] if r['seed'] == seed]
        if {r['arm'] for r in runs} != {'observe', 'slow'} or len(runs) != 2 or not all(r['passed'] and r['exitCode'] == 0 for r in runs):
            raise ValueError('Incomplete matched run pair')
        for run in runs:
            arm = run['arm']
            p = ROOT / run['name']
            q, census, market, sales = validate(p)
            if arm == 'observe':
                replayed = replay(prefix(plan['reference'], seed, reference['weeks']), p, min(105, plan['weeks'] - 1))
                replays.append(dict(kind='audit', seed=seed, through=min(105, plan['weeks'] - 1), streams=len(replayed['identicalStreams'])))
                if not np.allclose(q.rawLegacyDecay, q.rawDemand, rtol=5e-6, atol=.01):
                    raise ValueError('Identity decay shadow differs')
            else:
                if abs(plan['weeklyDecay'] - .985) < 1e-10 and not np.allclose(q.rawSlowDecay, q.rawDemand, rtol=5e-6, atol=.01):
                    raise ValueError('Live slow scenario disagrees with shadow formula')
            if not np.allclose(q.loc[q.soundtrack, 'rawNoNormalDecay'], q.loc[q.soundtrack, 'rawDemand']):
                raise ValueError('Soundtrack altered by normal-decay shadow')
            observed_capacities[arm, seed] = market[['week', 'regionId', 'baseCapacity', 'albumChannelCapacity', 'purchaseCapacity']].sort_values(['week', 'regionId']).reset_index(drop=True)
            full = q[q.year.isin([1960, 1961])]
            for name, keys in [('cohorts', ['year', 'cohort']), ('ages', ['year', 'ageBand']),
                               ('formats', ['year', 'albumFormat']), ('tiers', ['year', 'tier']),
                               ('genres', ['year', 'genre']), ('weekly', ['year', 'week'])]:
                outputs[name].append(grouped(full, keys).assign(arm=arm, seed=seed))
            for year in [1960, 1961]:
                y = full[full.year.eq(year)]
                m = market[market.year.eq(year)]
                if m.week.nunique() != 52:
                    raise ValueError('Incomplete settlement year')
                s = sales[sales.year.eq(year)]
                launches = census[census.observationKind.eq('Launch') & census.observedYear.eq(year)]
                a = grouped(y, ['year']).iloc[0].to_dict()
                a.update(arm=arm, seed=seed, lpIssues=len(launches),
                         singleUnitsM=s.loc[s['format'].eq('Single'), 'units'].sum() / 1e6,
                         capacityM=m.albumChannelCapacity.sum() / 1e6,
                         unitsOlderThan52M=y.loc[y.ageWeeks.gt(52), 'cleared'].sum() / 1e6,
                         openingUnitsM=y.loc[y.observationKind.eq('Opening'), 'cleared'].sum() / 1e6,
                         chartedUnitsM=y.loc[y.currentPosition.gt(0), 'cleared'].sum() / 1e6,
                         activeAtLeastOneWeek=y.recordId.nunique())
                outputs['annual'].append(pd.DataFrame([a]))
            # Include all eligible launches, including records retired before age 26: omitted weeks contribute zero.
            mature = census[census.observationKind.eq('Launch') & census.observedAuditWeek.le(plan['weeks'] - 27)]
            first26 = q[q.ageWeeks.le(26)].groupby('recordId').cleared.sum().reindex(mature.index, fill_value=0).rename('unitsFirst26').to_frame()
            first26['launchYear'] = mature.observedYear
            first26['launchTier'] = mature.launchTier
            first26['quality'] = mature.runtimeQualityAtLaunch
            first26['retired'] = mature.retiredAuditWeek.notna()
            first26['arm'] = arm; first26['seed'] = seed
            outputs['first26'].append(first26.reset_index())
            spells = []
            for record_id, history in q[q.observationKind.eq('Launch')].groupby('recordId', sort=False):
                chart_weeks = set(history.loc[history.currentPosition.gt(0), 'week'])
                if not chart_weeks:
                    continue
                start = min(chart_weeks)
                for window in [26, 52]:
                    if start > plan['weeks'] - window:
                        continue
                    length = 0
                    while length < window and start + length in chart_weeks:
                        length += 1
                    spells.append(dict(recordId=record_id, entryWeek=start, window=window,
                                       firstSpellWeeksCapped=length, reachesWindow=length == window, arm=arm, seed=seed))
            outputs['chart-spells'].append(pd.DataFrame(spells))
            retired = q[q.retiredAfterSettlement.astype(str).str.lower().eq('true')]
            outputs['retirements'].append(retired[['year', 'recordId', 'ageWeeks', 'albumFormat', 'currentPosition', 'cleared']].assign(arm=arm, seed=seed))
            validation.append(dict(arm=arm, seed=seed, lpRecordWeeks=len(q), rawReconciliationMaxUnits=float((q.groupby('week').rawDemand.sum() - market.groupby('week').rawAlbumDemand.sum()).abs().max()),
                                   salesReconcile=True, serviceableReconciles=True, shadowsFinite=True, seconds=run['seconds']))
        left, right = observed_capacities['observe', seed], observed_capacities['slow', seed]
        # Authored capacities must match; purchaseCapacity also reads live state and is reported, not asserted.
        pd.testing.assert_frame_equal(left.drop(columns='purchaseCapacity'), right.drop(columns='purchaseCapacity'))
        drift = (right.purchaseCapacity - left.purchaseCapacity) / left.purchaseCapacity.where(left.purchaseCapacity > 0)
        validation.append(dict(seed=seed, purchaseCapacityRowsDiffer=float(drift.ne(0).mean()),
                               purchaseCapacityMaxRelDiff=float(drift.abs().max())))
    for name, frames in outputs.items():
        pd.concat(frames, ignore_index=True).to_csv(out / (name + '.csv'), index=False)
    meta = dict(assemblySha256=plan['assemblySha256'], weeks=plan['weeks'], seeds=plan['seeds'], replays=replays,
                capacityIdentical=True, calibrationAccepted=False, validation=validation,
                caveats=['Frozen-state raw-demand shadows are nonadditive and are not allocated purchases.',
                         'Opening catalogue size/age/genre composition has not been independently calibrated.',
                         'One-seed live treatment includes the subsequent stochastic reshuffle.',
                         'No hard LP issue-count acceptance gate; Billboard review/import/reissue scope remains qualified.'])
    (out / 'validation.json').write_text(json.dumps(meta, indent=2), encoding='utf-8')
    annual = pd.concat(outputs['annual'], ignore_index=True)
    print(annual[['arm', 'year', 'lpIssues', 'clearedM', 'rawDemandM', 'serviceableM', 'rawSlowDecayM', 'rawNoNormalDecayM', 'rawNoExhaustionM', 'rawFullAwarenessM', 'unitsOlderThan52M', 'openingUnitsM', 'singleUnitsM']].round(4).to_string(index=False))
    print(json.dumps(meta, indent=2))


if __name__ == '__main__':
    main()
