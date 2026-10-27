"""Evaluate production-allocator CSV replay; never launches a simulation."""
from pathlib import Path
import json

import pandas as pd

from analyze_rack_jobber_probe import BILLBOARD

ROOT = Path(__file__).resolve().parents[1]
FOLDER = ROOT / 'SimLogs/retail-envelope-static'


def main():
    replay = pd.read_csv(FOLDER/'native-frozen-replay.csv')
    assert len(replay) == 150 and not replay.duplicated(['rackWeight','switchEstimate','year']).any()
    assert replay[['stores','racks','singles','unspent']].ge(0).all().all()
    assert (replay.stores+replay.racks+replay.unspent).eq(replay.lpBudget).all()
    # Zero means the production year-dependent migration schedule, not zero rack weight.
    chosen = replay.query('rackWeight == 0 and switchEstimate == .5').set_index('year').copy()
    old = pd.read_csv(ROOT/'SimLogs/lpdc-pool102-decade-holdout/lp-decade-annual.csv').query('1960 <= year <= 1969').set_index('year')
    refs = {year: (v['stores'], v['racks']) for year,v in BILLBOARD.items()}
    refs.update({1966:(56,77),1967:(56,87),1968:(54,93)})
    chosen['storesM'] = chosen.stores/1e6
    chosen['racksM'] = chosen.racks/1e6
    chosen['lpM'] = (chosen.stores+chosen.racks)/1e6
    chosen['singlesM'] = chosen.singles/1e6
    chosen['oldStoresM'] = old.storesM
    chosen['oldRacksM'] = old.allRacksM
    chosen['oldLpM'] = old.storesAndRacksM
    chosen['oldSinglesM'] = old.singlesM
    chosen['storeReferenceM'] = pd.Series({y:v[0] for y,v in refs.items()})
    chosen['rackReferenceM'] = pd.Series({y:v[1] for y,v in refs.items()})
    chosen['referenceVintage'] = ['1969 guide' if y>=1966 and y<=1968 else 'earlier series' if y in BILLBOARD else 'none' for y in chosen.index]
    chosen['storeErrorPct'] = (chosen.storesM/chosen.storeReferenceM-1)*100
    chosen['rackErrorPct'] = (chosen.racksM/chosen.rackReferenceM-1)*100
    chosen['meanChannelErrorPct'] = (chosen.storeErrorPct.abs()+chosen.rackErrorPct.abs())/2
    chosen['oldMeanChannelErrorPct'] = ((chosen.oldStoresM/chosen.storeReferenceM-1).abs()+(chosen.oldRacksM/chosen.rackReferenceM-1).abs())*50
    chosen.to_csv(FOLDER/'policy-annual-comparison.csv')
    cases = replay.query('rackWeight == 0').copy()
    width = cases.assign(lp=cases.stores+cases.racks).groupby('year').lp.agg(lambda v:v.max()-v.min())
    report = dict(
        analysisLaunchedSimulationRuns=0, decadeRunsStarted=0, method='Frozen retained seed 27102702; production C# split and cohort allocators; fixed assortment, quality, stocks and exhaustion; existing switched budget cohorts interval-inferred.',
        policy='lp-retail-v2; retail-purchase-envelope=1',
        calibrationYears=[1966,1967,1968], independentValidation=False,
        priorLateMeanChannelErrorPct=float(chosen.loc[1966:1968].oldMeanChannelErrorPct.mean()),
        newLateMeanChannelErrorPct=float(chosen.loc[1966:1968].meanChannelErrorPct.mean()),
        priorEarlyMeanChannelErrorPct=float(chosen.loc[1961:1965].oldMeanChannelErrorPct.mean()),
        newEarlyMeanChannelErrorPct=float(chosen.loc[1961:1965].meanChannelErrorPct.mean()),
        model1966to1969LpGrowthPct=float((chosen.loc[1969].lpM/chosen.loc[1966].lpM-1)*100),
        maxSwitchInferenceRetailWidthUnits=int(width.max()),
        scope='Same published model series as retained audit: live-record singles; stores plus front-line and budget rack LPs. Clubs and synthetic off-chart singles are separate; synthetic off-chart singles have no retained retail/jukebox split.',
        warnings=['Early reference vintage is not replaced. New early-year misses, especially 1965, remain visible.',
            'No endogenous inventory, cash, release, survival or multi-year feedback validation.',
            '1969 uses the earlier authored 55% endpoint, not a historical guide observation.',
            'Per-title store allocations cannot be reconstructed exactly for titles absent from rack ledgers; store cohorts are proportionally rescaled.',
            'Historic single scope and synthetic off-chart catalogue scope require reconciliation before claiming an all-industry fit.'])
    (FOLDER/'policy-review.json').write_text(json.dumps(report,indent=2)+'\n')
    print(chosen[['storesM','racksM','singlesM','lpRetailShare','meanChannelErrorPct','oldMeanChannelErrorPct']].round(4).to_string())
    print(json.dumps(report,indent=2))


if __name__ == '__main__':
    main()
