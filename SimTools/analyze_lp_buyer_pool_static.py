"""Frozen-ledger LP diagnostics. Never launches Godot or advances a saved world.

Store interventions scale retained serviceable intentions, not unrecorded raw
per-title buyer pools. Rack interventions hold exposure, exhaustion, assortment
and history fixed. Budget switch intentions are bounded from fulfilled cohorts.
"""
from pathlib import Path
import json
import re

import numpy as np
import pandas as pd

ROOT = Path(__file__).resolve().parents[1]
FOLDER = ROOT / 'SimLogs/lpdc-pool102-decade-holdout'
PREFIX = ROOT / 'SimLogs/lpdc-pool102-decade-holdout-budgetlp098pool102-default-27102702-530'


def reach(year):
    progress = np.clip(np.asarray(year, dtype=np.float32) - np.float32(1960), 0, 9) / np.float32(9)
    return np.float32(.3) + np.float32(.7) * progress


def allocate(demand, shelf):
    """Shared largest-remainder allocation; callers sort by native product ID."""
    demand = np.maximum(0, demand)
    total = min(max(0, int(shelf)), int(np.floor(demand.sum())))
    if total == 0:
        return np.zeros(len(demand), dtype=np.int64)
    exact = demand / demand.sum() * total
    units = np.floor(exact).astype(np.int64)
    order = np.argsort(-(exact - units), kind='stable')
    units[order[:total-int(units.sum())]] += 1
    assert units.sum() == total and (units >= 0).all()
    return units


def main():
    sales = pd.read_csv(str(PREFIX) + '-label-sales-weekly.csv', usecols=['week', 'year'])
    assert sales.groupby('week').year.nunique().eq(1).all()
    calendar = sales.groupby('week').year.first()

    def read(stream):
        frame = pd.read_csv(str(PREFIX) + '-' + stream + '.csv')
        frame['sourceYear'] = frame.year
        frame['year'] = frame.week.map(calendar)
        assert frame.year.notna().all()
        return frame[frame.year.between(1960, 1969)].copy()

    annual = pd.read_csv(FOLDER / 'lp-decade-annual.csv').query('1960 <= year <= 1969').set_index('year')
    lp, choice, title, budget, racks = map(read, ['lp-capacity', 'lp-channel-choice-weekly',
                                               'rack-title-weekly', 'budget-product-weekly', 'rack-region-weekly'])
    state = lp.merge(choice.drop(columns=['year', 'sourceYear']), on=['week', 'regionId'], validate='one_to_one')
    state = state.merge(racks[['week', 'regionId', 'shelf']], on=['week', 'regionId'], validate='one_to_one')
    state['capacity'] = state.legacyAlbumCapacity + state.addedAlbumCapacity
    assert np.minimum(state.serviceableAlbumDemand, state.capacity).eq(state.storeBefore).all()

    # C# multiplies int store units by float reach before promoting to double.
    title['planned'] = np.floor(title.storeUnits.to_numpy(dtype=np.float32) * reach(title.sourceYear)).astype(np.int64)
    assert title.groupby(['week', 'regionId']).planned.sum().equals(
        choice.set_index(['week', 'regionId']).frontPlanned.sort_index())
    title['autonomous'] = np.maximum(0, title.demand - title.planned)
    assert (title.demand - title.planned).min() > -.01

    # Given floor(units * switchDemand / demand) == switched, the latent
    # switchDemand is in this interval unless native cohort bounding intervened.
    # Test both ends (interior epsilon) and midpoint; no fictitious precision.
    budget['switchLow'] = np.where(budget.units > 0, budget.demand * budget.switchedStoreUnits / budget.units.clip(lower=1), 0)
    budget['switchHigh'] = np.where(budget.units > 0, np.minimum(budget.demand,
        budget.demand * (budget.switchedStoreUnits + 1) / budget.units.clip(lower=1)), budget.demand)

    front_groups = {key: group.sort_values('recordId') for key, group in title.groupby(['week', 'regionId'])}
    budget_groups = {key: group.sort_values('productId') for key, group in budget.groupby(['week', 'regionId'])}
    assert state.groupby('year').storeBefore.sum().div(1e6).sub(annual.storeBeforeChoiceM).abs().max() < 1e-8
    assert title.groupby('year').units.sum().div(1e6).sub(annual.frontRacksM).abs().max() < 1e-8
    assert budget.groupby('year').units.sum().div(1e6).sub(annual.budgetRacksM).abs().max() < 1e-8

    scenarios = {
        'baseline': (1., False, False),
        'store_intentions_minus10pct': (.9, False, False),
        'store_intentions_minus20pct': (.8, False, False),
        'store_intentions_minus30pct': (.7, False, False),
        'store_intentions_minus40pct': (.6, False, False),
        'store_intentions_minus50pct': (.5, False, False),
        'front_rack_reach_held1966': (1., True, False),
        'budget_rack_reach_held1966': (1., False, True),
        'both_rack_reach_held1966': (1., True, True),
        'store_minus20pct_and_rack_reach_held1966': (.8, True, True),
        'store_minus30pct_and_rack_reach_held1966': (.7, True, True),
    }
    rows = []
    native_errors = []
    for name, (store_factor, front_freeze, budget_freeze) in scenarios.items():
        for interval in [0., .5, 1.]:
            for s in state.itertuples(index=False):
                key = s.week, s.regionId
                front, bg = front_groups[key], budget_groups[key]
                new_store = min(s.serviceableAlbumDemand * store_factor, s.capacity)
                cohort_factor = new_store / max(1, s.storeBefore)
                rack_year = int(front.sourceYear.iloc[0])
                pool_factor = float(reach(min(rack_year, 1966)) / reach(rack_year))
                planned = front.planned.to_numpy(float) * cohort_factor
                fd = front.autonomous.to_numpy(float) * (pool_factor if front_freeze else 1) + planned
                low, high = bg.switchLow.to_numpy(float), bg.switchHigh.to_numpy(float)
                epsilon = np.minimum((high-low)*.1, 1e-7)
                switch = (low + epsilon + (high-low-2*epsilon)*interval)
                bd = np.maximum(0, bg.demand.to_numpy(float)-switch) * (pool_factor if budget_freeze else 1) + switch * cohort_factor
                # Native prefix order is budget:* before front:*.
                demand = np.concatenate([bd, fd])
                units = allocate(demand, s.shelf)
                bu, fu = units[:len(bg)], units[len(bg):]
                front_exact = np.minimum(planned, np.divide(fu*planned, fd, out=np.zeros(len(fd)), where=fd>0))
                front_switched = int(np.floor(front_exact.sum()))
                budget_switched = int(np.floor(np.minimum(bu, np.divide(bu*switch*cohort_factor, bd,
                    out=np.zeros(len(bd)), where=bd>0))).sum())
                switched = front_switched + budget_switched
                assert switched <= new_store + 1e-6
                store_after = new_store - switched
                retail = store_after + int(units.sum())
                assert abs(retail - (new_store + int(units.sum()) - switched)) < 1e-6
                rows.append(dict(scenario=name, interval=interval, year=int(s.year), week=int(s.week), regionId=s.regionId,
                    stores=store_after, racks=int(units.sum()), retail=retail,
                    storeBefore=new_store, switched=switched, autonomousRack=int(units.sum())-switched))
                if name == 'baseline' and interval == .5:
                    native_errors.append(abs(int(units.sum()) - int(front.units.sum()+bg.units.sum())))

    detail = pd.DataFrame(rows)
    result = detail.groupby(['scenario', 'interval', 'year'])[['stores', 'racks', 'retail', 'storeBefore', 'switched', 'autonomousRack']].sum()/1e6
    baseline = result.loc[('baseline', .5)]
    for metric in ['stores', 'racks', 'retail']:
        result[metric+'DeltaVsReplayM'] = result[metric] - result.index.get_level_values('year').map(baseline[metric])
    result['anchoredRetailM'] = result.retailDeltaVsReplayM + result.index.get_level_values('year').map(annual.storesAndRacksM)
    result.to_csv(FOLDER / 'buyer-pool-static-scenarios.csv')

    comparison = pd.DataFrame({
        'year':[1966,1967,1968], 'historicalStoresM':[56,56,54], 'historicalRacksM':[77,87,93],
        'historicalSinglesM':[129,130,126], 'historicalClubsM':[47,49,49]}).set_index('year')
    comparison['historicalRetailLpM'] = comparison.historicalStoresM + comparison.historicalRacksM
    comparison['historicalRetailTotalM'] = comparison.historicalRetailLpM + comparison.historicalSinglesM
    comparison['historicalLpShare'] = comparison.historicalRetailLpM / comparison.historicalRetailTotalM
    comparison['modelStoresM'] = annual.storesM
    comparison['modelRacksM'] = annual.allRacksM
    comparison['modelRetailLpM'] = annual.storesAndRacksM
    comparison['modelSinglesM'] = annual.singlesM
    comparison['modelRetailTotalM'] = comparison.modelRetailLpM + comparison.modelSinglesM
    comparison['modelLpShare'] = comparison.modelRetailLpM / comparison.modelRetailTotalM
    comparison['modelLpAtHistoricalShareM'] = comparison.modelRetailTotalM * comparison.historicalLpShare
    comparison['mixGapAtModelTotalM'] = comparison.modelRetailLpM - comparison.modelLpAtHistoricalShareM
    comparison.to_csv(FOLDER/'buyer-guide-late-comparison.csv')

    # An exact accounting decomposition: transfers cancel out of retail totals.
    channels = pd.DataFrame({'storeBeforeM':annual.storeBeforeChoiceM,
        'autonomousFrontM':annual.frontRacksM-annual.frontSwitchedM,
        'autonomousBudgetM':annual.budgetRacksM-annual.switchedStoreM})
    assert channels.sum(axis=1).sub(annual.storesAndRacksM).abs().max()<1e-8
    channels.to_csv(FOLDER/'buyer-pool-channel-components.csv')

    params = []
    for block in re.split(r'\n\[', (ROOT/'chart_manager.tscn').read_text()):
        if 'regionId = ' not in block:
            continue
        props = dict(re.findall(r'^([A-Za-z]\w*) = ([^\n]+)', block, re.M))
        for y in range(1960,1970):
            era = (y-1957)/15
            youth, income = float(props['youthPercentage']), float(props['averageIncome'])
            willingness = np.clip(.70 + np.clip((income-.70)/.55,0,1)*.48 + era*.25 - youth*(.55-.25*era),.08,1)
            affinity = np.clip(.4*(1-youth*(.40-.28*era))*(1+1.5*era),.05,1)
            params.append(dict(regionId=props['regionId'].strip('"'), year=y, era=era,
                neutralAffinity=affinity, willingness=willingness, neutralStorePropensity=affinity*willingness,
                rackReach=float(reach(y))))
    pd.DataFrame(params).to_csv(FOLDER/'buyer-pool-authored-curves.csv',index=False)

    meta = dict(seed=27102702, simulationRunsStarted=0, sourceWindow='complete 1960-1969 settlement years',
        exactChecks=['all regional prechoice LP totals equal min(serviceable, capacity)',
            'planned front-line rack switch cohorts reconstructed exactly with native float arithmetic',
            'annual store/front/budget identities match retained analyzer', 'retail equals prechoice stores plus autonomous rack sales'],
        maxBaselineRegionalRackUnitsError=max(native_errors),
        baselineAnnualRetailErrorM=(baseline.retail-annual.storesAndRacksM).to_dict(),
        caveats=['No per-title store buyer-pool/exhaustion telemetry was retained; serviceable scaling is an aggregate sensitivity, not a formula replay.',
            'Front autonomous demand is inferred from serialized demand minus exact planned switches.',
            'Budget intended switches are interval-inferred from fulfilled switches; endpoints are sensitivity cases, not guaranteed global bounds if native cohort bounding intervened.',
            'Source simulation year drives formulas; settlement year drives annual comparisons.',
            'Pool contractions hold exhaustion fixed; smaller pools can also increase exhaustion. No claim of total pool-change effect.',
            'Title selection, quality, genres, stock, release decisions, price, seasonality, cash and later feedback remain fixed.',
            'Rack reach held in this diagnostic changes autonomous pools only; physical shelf and store-choice reach remain recorded.',
            'Changed store cohorts scale existing title choice and budget switching proportionally; rankings and genre mix are not recomputed.'],
        historicalSource='https://www.worldradiohistory.com/Archive-All-Music/Billboard/60s/1969/Billboard%201969-08-30-Buyer.pdf',
        historicalPrintedPages=[10,11], historicalWindow=[1966,1967,1968], historical1969Available=False)
    assert max(native_errors) <= 1
    assert max(abs(v) for v in meta['baselineAnnualRetailErrorM'].values()) < .000002
    census = pd.read_csv(str(PREFIX) + '-demand-release-census.csv')
    launches = census[(census.format == 'Album') & (census.observationKind == 'Launch')].copy()
    launches['year'] = launches.observedAuditWeek.map(calendar)
    launches = launches[launches.year.between(1960,1969)]
    assert launches.runtimeQualityAtLaunch.notna().all()
    launches['qualityPower'] = launches.runtimeQualityAtLaunch ** 2.5
    launches.groupby('year').agg(launches=('recordId','size'),
        qualityMean=('runtimeQualityAtLaunch','mean'), qualityMedian=('runtimeQualityAtLaunch','median'),
        meanQualityPower=('qualityPower','mean')).to_csv(FOLDER/'buyer-pool-lp-launch-quality.csv')
    meta['launchQualityCaveat'] = 'Quality is launch-cohort weighted, not sales weighted; a rising quality-power mean is not a causal sales attribution.'
    (FOLDER/'buyer-pool-static-method.json').write_text(json.dumps(meta,indent=2)+'\n')
    print('Historical/model comparison:\n', comparison.round(4).to_string())
    print('\n1966-1969 exact retail component changes M:\n', (channels.loc[1969]-channels.loc[1966]).to_string())
    print('\nStatic midpoint scenarios, anchored retail M:\n', result.xs(.5,level='interval').anchoredRetailM.unstack('year').loc[:,1966:1969].round(4).to_string())
    print('\nBaseline annual retail error M:',meta['baselineAnnualRetailErrorM'])
    print('Max baseline regional rack units error:',max(native_errors))


if __name__ == '__main__':
    main()
