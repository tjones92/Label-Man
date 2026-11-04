"""Calendar-year 2x2 LP capacity screen; no new single purchasing or spillover capacity.
python SimTools/analyze_lp_capacity_screen.py --tag lpc-v1
"""
import argparse
import json
from pathlib import Path

import numpy as np
import pandas as pd

from analyze_single_capital_risk import ROOT, manifest, prefix, read, replay
from analyze_studio_parent import BIG5, studio_ids


def validate(p, programme, purchase):
    market = read(p, 'market-clearing-weekly')
    audit = read(p, 'lp-capacity')
    if market.duplicated(['week', 'regionId']).any() or audit.duplicated(['week', 'regionId']).any():
        raise ValueError('Repeated region/week')
    both = market.merge(audit, on=['week', 'year', 'regionId'], suffixes=('', '_lp'), validate='one_to_one')
    if len(both) != len(market) or len(both) != len(audit):
        raise ValueError('Incomplete LP capacity audit')
    if not np.isfinite(audit.select_dtypes(include='number').to_numpy()).all():
        raise ValueError('Nonfinite LP budget')
    if not np.allclose(audit.programmeScale, programme) or not np.allclose(audit.purchaseShare, purchase):
        raise ValueError('Wrong capacity treatment')
    if audit.singlesBeforeAddition.ne(audit.singlesAfterAddition).any() or both.singlesAfterAddition.ne(both.clearedSingleUnits).any():
        raise ValueError('Additional LP budget changed same-state singles allocation')
    if audit.purchaseCapacity.ne(audit.legacyPurchaseCapacity + audit.addedAlbumCapacity).any():
        raise ValueError('Additional capacity did not reconcile')
    if audit.albumCapacity.ne(audit.legacyAlbumCapacity + audit.addedAlbumCapacity).any():
        raise ValueError('LP channel accounting failed')
    if audit.addedAlbumCleared.lt(0).any() or (audit.addedAlbumCleared > audit.addedAlbumCapacity).any():
        raise ValueError('Extra LP sales exceeded their capacity')
    if (audit.addedAlbumCleared > audit.albumCleared).any() or audit.albumCleared.gt(audit.albumBudget).any():
        raise ValueError('LP sales exceeded their budget')
    if purchase:
        share = np.float32(purchase)
        ratio = float(share) / float(np.float32(1) - share)
        expected = np.maximum(0, np.floor(audit.baseSingleCapacity * ratio + .5).astype('int64') - audit.legacyAlbumCapacity)
        if audit.addedAlbumCapacity.ne(expected).any():
            raise ValueError('Wrong LP-only capacity increment')
    elif audit.addedAlbumCapacity.ne(0).any():
        raise ValueError('Capacity changed in a purchase-off arm')
    # Mixed spillover keeps the pre-addition limits. Neither unused extra LP capacity nor the larger total
    # purchase capacity is allowed to expand single transfers.
    legacy_unused = both.unusedAfterLocal - both.addedAlbumCapacity + both.addedAlbumCleared
    if both.exportBudget.ne(np.floor(legacy_unused * .75).astype('int64')).any():
        raise ValueError('Extra LP capacity leaked into mixed export budgets')
    expected_import = np.floor((both.legacyPurchaseCapacity.to_numpy(dtype=np.float32) * np.float32(.15))).astype('int64')
    if (np.abs(both.importLimit - expected_import) > 1).any():
        raise ValueError('Extra LP capacity expanded mixed import limits')
    for c in ['inventoryViolationCount', 'allocationViolationCount', 'reconciliationDelta', 'settlementDelta']:
        if market[c].abs().max() > (1 if 'Delta' in c else 0):
            raise ValueError('Market validation failed: ' + c)
    sales = read(p, 'label-sales-weekly')
    for form, column in [('Single', 'clearedSingleUnits'), ('Album', 'clearedAlbumUnits')]:
        sold = sales[sales['format'].eq(form)].groupby('week').units.sum()
        cleared = market.groupby('week')[column].sum()
        if not sold.reindex(cleared.index, fill_value=0).equals(cleared):
            raise ValueError('Settlement sales fail reconciliation: ' + form)
    company = read(p, 'company-monthly')
    if company.cashReconciliationError.abs().max() > .02:
        raise ValueError('Company cash did not reconcile')
    return market, audit, sales, company


def measure(p, market, audit, sales, company):
    all_census = read(p, 'demand-release-census')
    census = all_census
    if census.recordId.duplicated().any():
        raise ValueError('Repeated physical release identity')
    census = census[census.observationKind.eq('Launch')]
    directory = read(p, 'label-directory').set_index('labelId')
    studios = studio_ids(lambda stream: str(p) + '-' + stream + '.csv')
    fin = read(p, 'label-finance')
    chart = read(p, 'records', usecols=['week', 'year', 'artistId', 'labelId', 'currentPosition'])
    slate = read(p, 'release-slate')
    outcomes = read(p, 'release-outcomes')
    groups = {'BigFive': BIG5, 'studio': studios}
    for tier in ['MidTier', 'Independent', 'Small', 'Boutique']:
        groups['opening_' + tier] = set(directory[directory.initialTier.eq(tier)].index) - BIG5 - studios
    annual, finance, pipeline, losses, heads = [], [], [], [], []
    for year, hs, ha, hsu, hau in [(1960, 5797, 3112, None, None), (1961, 6036, 2927, 132, 128)]:
        releases = census[census.observedYear.eq(year)]
        # Release dates occur before the audit advances the date. Close-of-year releases end one tick later
        # than the sales settlement year; retain each stream's own calendar year instead of truncating both.
        end_tick = int(releases.observedAuditWeek.max())
        start_tick = int(releases.observedAuditWeek.min())
        s = sales[sales.year.eq(year)]
        m = market[market.year.eq(year)]
        a = audit[audit.year.eq(year)]
        if m.week.nunique() != 52:
            raise ValueError('Incomplete sales calendar year')
        last_fin = fin[fin.week.le(end_tick)].drop_duplicates('labelId', keep='last')
        c = chart[chart.year.eq(year) & chart.currentPosition.between(1, 100)]
        if not c.groupby('week').size().eq(100).all() or c.week.nunique() != 52:
            raise ValueError('Incomplete Hot 100 year')
        top = c[c.currentPosition.le(10)]
        single_n = int(releases['format'].eq('Single').sum())
        lp_n = int(releases['format'].eq('Album').sum())
        single_u = int(s.loc[s['format'].eq('Single'), 'units'].sum())
        lp_u = int(s.loc[s['format'].eq('Album'), 'units'].sum())
        existing_lps = all_census[all_census['format'].eq('Album') &
            (all_census.observationKind.eq('Opening') | all_census.observedAuditWeek.le(end_tick)) &
            (all_census.retiredAuditWeek.isna() | all_census.retiredAuditWeek.gt(end_tick))]
        annual.append(dict(year=year, singles=single_n, lps=lp_n, singleCountToHistory=single_n / hs, lpCountToHistory=lp_n / ha,
            singleUnitsM=single_u / 1e6, lpUnitsM=lp_u / 1e6, lpUnitShare=lp_u / (lp_u + single_u), lpUnitsPerIssue=lp_u / lp_n,
            singleUnitsHistoryM=hsu, lpUnitsHistoryM=hau, authoredSingleCapacity=m.baseCapacity.sum(),
            lpChannelCapacityM=a.albumCapacity.sum() / 1e6, extraLpCapacityM=a.addedAlbumCapacity.sum() / 1e6,
            extraLpUnitsM=a.addedAlbumCleared.sum() / 1e6, rawLpDemandM=m.rawAlbumDemand.sum() / 1e6,
            serviceableLpDemandM=m.serviceableAlbumIntent.sum() / 1e6,
            openingLps=int((all_census['format'].eq('Album') & all_census.observationKind.eq('Opening')).sum()),
            activeLpsAtYearClose=len(existing_lps), localSingleUnitsM=m.localClearedSingleUnits.sum() / 1e6,
            spilloverSingleUnitsM=(m.clearedSingleUnits - m.localClearedSingleUnits).sum() / 1e6,
            top10SingleUnitsM=s.loc[s['format'].eq('Single'), 'top10Units'].sum() / 1e6,
            big5Top10Share=top.labelId.isin(BIG5).mean(), studioTop10Share=top.labelId.isin(studios).mean(),
            hot100Acts=c.artistId.nunique(), closed=last_fin.status.isin(['Bankrupt', 'Defunct']).sum(),
            big5Singles=int(releases[releases['format'].eq('Single')].labelId.isin(BIG5).sum()),
            studioSingles=int(releases[releases['format'].eq('Single')].labelId.isin(studios).sum())))
        months = company[company.year.eq(year)]
        year_end = company[company.year.le(year)].drop_duplicates('labelId', keep='last')
        # Follow named firms separately: cohort medians can hide opposite liquidity changes.
        for label_id, label in directory[directory.isHistorical.astype(str).str.lower().eq('true')].iterrows():
            statement = year_end[year_end.labelId.eq(label_id)]
            if statement.empty:
                continue
            statement = statement.iloc[-1]
            liquidity = last_fin[last_fin.labelId.eq(label_id)].iloc[-1]
            firm_releases = releases[releases.labelId.eq(label_id)]
            heads.append(dict(year=year, labelId=label_id, labelName=label.labelName,
                initialTier=label.initialTier, status=liquidity.status,
                singles=int(firm_releases['format'].eq('Single').sum()), lps=int(firm_releases['format'].eq('Album').sum()),
                pretaxEarnings=months.loc[months.labelId.eq(label_id), 'pretaxEarnings'].sum(),
                closingCash=statement.closingCash, corporateHold=statement.protectedCash,
                cashLessCorporateHold=statement.closingCash-statement.protectedCash,
                receivables=liquidity.outstandingWholesaleReceivables, plantPayables=liquidity.outstandingPlantPayables))
        for group, ids in groups.items():
            flows = months[months.labelId.isin(ids)]
            cash = year_end[year_end.labelId.isin(ids) & year_end.active.astype(str).str.lower().eq('true')]
            liquidity = last_fin[last_fin.labelId.isin(ids) & ~last_fin.status.isin(['Bankrupt', 'Defunct', 'Acquired'])]
            finance.append(dict(year=year, group=group, recordGrossM=flows.recordGross.sum() / 1e6,
                pretaxM=flows.pretaxEarnings.sum() / 1e6, closingCashM=cash.closingCash.sum() / 1e6,
                medianClosingCash=cash.closingCash.median(), surviving=int(len(liquidity)),
                recordReceiptsM=flows.recordReceipts.sum() / 1e6, plantPaymentsM=flows.plantPayments.sum() / 1e6,
                recordingM=flows.recording.sum() / 1e6, artistAdvancesM=flows.artistAdvances.sum() / 1e6,
                ownerTransfersM=flows.ownerTransfersNet.sum() / 1e6, ownerSalaryM=flows.ownerSalary.sum() / 1e6,
                medianCashLessCorporateHold=(cash.closingCash - cash.protectedCash).median(),
                medianCashMonths=(liquidity.cashReserves / liquidity.monthlyOverhead.clip(lower=1)).median(),
                medianReceivables=liquidity.outstandingWholesaleReceivables.median(),
                medianPlantPayables=liquidity.outstandingPlantPayables.median(),
                closures=int(last_fin[last_fin.labelId.isin(ids)].status.isin(['Bankrupt', 'Defunct']).sum())))
        lp_slate = slate[slate['format'].eq('Album') & slate.week.le(end_tick)]
        latest = lp_slate.drop_duplicates('slotId', keep='last')
        proposed = lp_slate[lp_slate.state.eq('Proposed') & lp_slate.week.ge(start_tick)]
        pipeline.append(dict(year=year, lpIntentionsProposed=proposed.slotId.nunique(), lpSlotsSeen=latest.slotId.nunique(),
            issued=int(latest.state.eq('Issued').sum()), cancelled=int(latest.state.eq('Cancelled').sum()),
            paidCancelled=int((latest.state.eq('Cancelled') & latest.paidProduction.gt(0)).sum()),
            paidCancelledRecording=latest.loc[latest.state.eq('Cancelled'), 'paidProduction'].sum(),
            insufficientCashEncounters=int(lp_slate.reason.str.contains('insufficient-unreserved-cash', na=False).sum()),
            materialEconomicsFundingEncounters=int(lp_slate.reason.str.contains('material-economics|preparation-material', na=False).sum())))
        mature = releases[releases.observedAuditWeek.le(end_tick - 26)].set_index('recordId').launchTier
        retired = outcomes[outcomes.recordId.isin(mature.index) & outcomes.week.le(end_tick)].drop_duplicates('recordId')
        retired['tier'] = retired.recordId.map(mature)
        for (form, tier), o in retired.groupby(['format', 'tier']):
            losses.append(dict(year=year, format=form, tier=tier, retired=len(o), lossShare=o.realizedNet.lt(0).mean(),
                meanNet=o.realizedNet.mean(), medianNet=o.realizedNet.median()))
    lp_chart = read(p, 'album-chart')
    genre = lp_chart[lp_chart.position.le(10) & lp_chart.year.isin([1960, 1961])].groupby(['year', 'genre']).size().rename('slotWeeks').reset_index()
    genre['share'] = genre.slotWeeks / genre.groupby('year').slotWeeks.transform('sum')
    return annual, finance, pipeline, losses, genre, heads


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--tag', default='lpc-v1')
    args = parser.parse_args()
    plan = json.loads((ROOT / args.tag / 'plan.json').read_text(encoding='utf-8-sig'))
    if len(plan['checks']) != 2 or not all(c['passed'] for c in plan['checks']):
        raise ValueError('Native and full-world round-trip checks required')
    if {g['arm'] for g in plan['groups']} != {'control', 'programme', 'purchase', 'both'}:
        raise ValueError('Four factorial arms required')
    out = ROOT / args.tag / 'review'
    out.mkdir(parents=True, exist_ok=True)
    annual, finance, pipeline, losses, genres, heads, validation = [], [], [], [], [], [], []
    capacity = {}
    for group in plan['groups']:
        arm, tag = group['arm'], group['tag']
        m = manifest(tag)
        if m['assemblySha256'] != plan['assemblySha256'] or m['extraFlags'] != plan['bundle'] + group['flags'] or m['weeks'] != plan['weeks']:
            raise ValueError('Mismatched factorial configuration')
        for seed in plan['seeds']:
            runs = [r for r in m['results'] if r['seed'] == seed and r['passed'] and r['exitCode'] == 0]
            if len(runs) != 1:
                raise ValueError('Incomplete run')
            p = prefix(tag, seed, m['weeks'])
            market, audit, sales, company = validate(p, plan['programmeScale'] if arm in ['programme', 'both'] else 1,
                plan['purchaseShare'] if arm in ['purchase', 'both'] else 0)
            capacity[arm, seed] = audit[['week', 'regionId', 'baseSingleCapacity', 'legacyAlbumCapacity']].sort_values(['week', 'regionId']).reset_index(drop=True)
            a, f, pipe, loss, genre, head = measure(p, market, audit, sales, company)
            annual.extend(dict(arm=arm, seed=seed, **r) for r in a)
            finance.extend(dict(arm=arm, seed=seed, **r) for r in f)
            pipeline.extend(dict(arm=arm, seed=seed, **r) for r in pipe)
            losses.extend(dict(arm=arm, seed=seed, **r) for r in loss)
            heads.extend(dict(arm=arm, seed=seed, **r) for r in head)
            genres.append(genre.assign(arm=arm, seed=seed))
            validation.append(dict(arm=arm, seed=seed, regionWeeks=len(audit), singleAllocationChanges=0,
                inventoryViolations=0, allocationViolations=0, maxCashReconciliationError=company.cashReconciliationError.abs().max(),
                seconds=runs[0]['seconds']))
    for (arm, seed), c in capacity.items():
        pd.testing.assert_frame_equal(c, capacity['control', seed])
    a = pd.DataFrame(annual)
    effects = []
    for seed in plan['seeds']:
        for year in [1960, 1961]:
            q = a[a.seed.eq(seed) & a.year.eq(year)].set_index('arm')
            for metric in ['lps', 'lpUnitsM', 'singles', 'singleUnitsM', 'closed']:
                c, p, b, j = (q.loc[x, metric] for x in ['control', 'programme', 'purchase', 'both'])
                effects.append(dict(seed=seed, year=year, metric=metric, programmeEffect=p-c, purchaseEffect=b-c,
                    jointEffect=j-c, interaction=j-p-b+c))
    for name, frame in [('annual', a), ('finance', pd.DataFrame(finance)), ('pipeline', pd.DataFrame(pipeline)),
                        ('losses', pd.DataFrame(losses)), ('head-companies', pd.DataFrame(heads)),
                        ('lp-chart-genres', pd.concat(genres, ignore_index=True)),
                        ('effects', pd.DataFrame(effects)), ('validation', pd.DataFrame(validation))]:
        frame.to_csv(out / (name + '.csv'), index=False)
    reference = manifest(plan['reference'])
    seed = plan['seeds'][0]
    identity = replay(prefix(plan['reference'], seed, reference['weeks']), prefix(args.tag+'-control', seed, plan['weeks']), 105)
    meta = dict(seeds=plan['seeds'], weeks=plan['weeks'], assemblySha256=plan['assemblySha256'],
        controlReplayStreams=len(identity['identicalStreams']), controlReplayThrough=105,
        singleCapacityIdentical=True, additionalSingleAllocation=0, calibrationAccepted=False,
        caveat='Calendar-year releases and sales use their own year fields; one seed is a mechanism screen. Purchase additions are local LP-only capacity, not a demand multiplier.')
    (out / 'metadata.json').write_text(json.dumps(meta, indent=2), encoding='utf-8')
    print('Annual\n' + a.round(4).to_string(index=False))
    print('\nFinance\n' + pd.DataFrame(finance).round(2).to_string(index=False))
    print('\nPipeline\n' + pd.DataFrame(pipeline).round(2).to_string(index=False))
    print('\nEffects\n' + pd.DataFrame(effects).round(4).to_string(index=False))
    print(json.dumps(meta, indent=2))


if __name__ == '__main__':
    main()
