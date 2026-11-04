"""Compare completed live worlds, with equal outcome horizons and explicit small-cohort counts."""
import argparse, json, pathlib
import numpy as np
import pandas as pd
from score_playlist_pipeline import score

ROOT = pathlib.Path(__file__).resolve().parents[1]
BIG5 = [f'label_000{i}' for i in range(1,6)]

def read(prefix, suffix, **kw):
    return pd.read_csv(str(prefix) + '-' + suffix + '.csv', **kw)

def analyze(run, horizon, start, end):
    pre = ROOT / 'SimLogs' / run['name']
    r = read(pre,'records')
    launches = read(pre,'single-launch-quality')
    singles = r[r.recordId.isin(launches.singleId)].copy()
    playlists = read(pre,'station-playlists')
    f = read(pre,'label-finance')
    market = read(pre,'market-clearing-weekly')
    result = {k:run[k] for k in ('name','arm','seed','weeks')}
    result |= dict(totalUnitsM=market.clearedTotalUnits.sum()/1e6, exportedRecordUnitsM=r.unitsThisWeek.sum()/1e6, singleUnitsM=market.clearedSingleUnits.sum()/1e6,
                   albumUnitsM=market.clearedAlbumUnits.sum()/1e6, rawSinglesM=market.rawSingleDemand.sum()/1e6,
                   unservedM=market.physicalBackorders.sum()/1e6,
                   displacedM=market.marketDisplacedDemand.sum()/1e6)
    guard = ['inventoryViolationCount','allocationViolationCount','reconciliationDelta','settlementDelta']
    result['integrityErrors'] = float(market[guard].abs().sum().sum())
    if result['integrityErrors']: raise ValueError('Accounting violations: '+run['name'])
    top = r[r.currentPosition.between(1,10)]
    result['big5Top10Pct'] = 100*top.labelId.isin(BIG5).mean()
    result['top10Artists'] = top.artistId.nunique()
    top40=r[r.currentPosition.between(1,40)]
    result['airplayTop40Pct']=100*(1-top40.unitsThisWeek.sum()/top40.unsampledChartPoints.sum())
    result['top10WeeklyUnitsMedian']=top.unitsThisWeek.median()
    top_slots=top.groupby('labelId').size().sort_values(ascending=False)
    result['c4Top10SlotPct']=100*top_slots.head(4).sum()/len(top)
    result['c8Top10SlotPct']=100*top_slots.head(8).sum()/len(top)
    ones=r[r.currentPosition.eq(1)].sort_values('week')
    returns=ones.groupby('recordId').week.diff().gt(1)
    result['numberOneTitles']=ones.recordId.nunique()
    result['numberOneReturnEvents']=int(returns.sum())
    result['numberOneReturningTitles']=ones.loc[returns,'recordId'].nunique()
    sold = r.sort_values(['week','unitsThisWeek'],ascending=[True,False]).groupby('week').head(10)
    result['big5SalesTop10Pct'] = 100*sold.labelId.isin(BIG5).mean()
    result['top10UnitSharePct'] = 100*sold.unitsThisWeek.sum()/r.unitsThisWeek.sum()
    result['closures'] = f.loc[f.status.eq('Defunct'),'labelId'].nunique()
    closed=f[f.status.eq('Defunct')].drop_duplicates('labelId')
    result['bankruptcies']=int(closed.exitReason.fillna('').eq('Bankruptcy').sum())
    opening=f[f.week.eq(f.week.min())].set_index('labelId').labelTier
    starting_closed=closed[closed.labelId.isin(opening.index)].copy()
    starting_closed['openingTier']=starting_closed.labelId.map(opening)
    for tier in ['Major','MidTier','Independent','Boutique','Small']:
        result[tier+'UnitPct'] = 100*r.loc[r.labelTier.eq(tier),'unitsThisWeek'].sum()/r.unitsThisWeek.sum()
        result[tier+'StartingClosures']=int(starting_closed.openingTier.eq(tier).sum())
    panel = json.loads((ROOT/'SimTools/regional_observer_panel.json').read_text())
    metrics, events, listings = score(singles,playlists,panel,start,end,horizon)
    for group in ['indie10','big5_10','41-100','all-debuts']:
        for panelname, key in [('full-world',''),('fixed-2+1','fixed_')]:
            row = metrics[(metrics.panel==panelname)&(metrics.cohort==group)&(metrics.event=='firstAudition')].iloc[0]
            result[key+group+'_n'] = int(row.n)
            result[key+group+'_lead2'] = int(row.lead2)
            result[key+group+'_leadPct'] = row.leadPct
            result[key+group+'_eligible'] = int(row.eligible)
            result[key+group+'_opportunityLeadPct'] = row.opportunityAdjustedPct
    meta = events[events.panel.eq('full-world')]
    indie = meta[meta.peak.le(10)&~meta.big5]
    result['indieDebutAge'] = (indie.debut-indie.launch+1).mean()
    result['indieListedByAge2Pct']=100*(indie.firstAudition-indie.launch+1).le(2).mean()
    result['indiePeakN'] = len(indie)
    result['cohortN'] = len(meta)
    result['cohortTop10Pct'] = 100*meta.peak.le(10).mean()
    result['cohortUnchartedPct'] = 100*meta.peak.isna().mean()
    cohort_sales=singles[singles.weeksSinceRelease.between(1,horizon)].groupby('recordId').unitsThisWeek.sum().reindex(meta.recordId,fill_value=0)
    result['cohortUnitsMedian']=cohort_sales.median()
    result['cohortUnitsP90']=cohort_sales.quantile(.9)
    result['cohortUnitsP99']=cohort_sales.quantile(.99)
    result['cohortOver100k']=int(cohort_sales.ge(100000).sum())
    result['cohortOver250k']=int(cohort_sales.ge(250000).sum())
    result['cohortOverMillion']=int(cohort_sales.ge(1000000).sum())
    result['cohortUnder2kPct']=100*cohort_sales.lt(2000).mean()
    for genre,value in r.groupby('genre').unitsThisWeek.sum().items():
        result['genreUnitsPct_'+genre]=100*value/r.unitsThisWeek.sum()
    local = playlists.groupby('recordId').week.min()
    all_events = meta.set_index('recordId').join(local.rename('anyLocal'))
    result['anyLocalIndie10Lead2'] = int(((all_events.debut-all_events.anyLocal).ge(2)&all_events.peak.le(10)&~all_events.big5).sum())
    observed = pathlib.Path(str(pre)+'-local-consumption.csv')
    if observed.exists():
        o = pd.read_csv(observed)
        if len(o):
            if not np.isfinite(o.select_dtypes('number')).all().all(): raise ValueError('Nonfinite probe telemetry')
            if not o.afterAware.between(-1e-6,1.000001).all(): raise ValueError('Awareness out of bounds')
            result['probeRawGainPct'] = 100*(o.rawAfter.sum()/o.rawBefore.sum()-1)
            result['panelPositivePct'] = 100*o.panel.gt(0).mean()
            for name, mask in [('uncharted',o.position.eq(0)),('chart10',o.position.between(1,10))]:
                q = o[mask]
                result[name+'AwareMedian'] = q.aware.median()
                result[name+'PanelMedian'] = q.panel.median()
                result[name+'ConversionMedian'] = q.conversion.median()
    return result, metrics.assign(arm=run['arm'],seed=run['seed']), events.assign(arm=run['arm'],seed=run['seed'])

def main():
    p=argparse.ArgumentParser()
    p.add_argument('tag')
    p.add_argument('--horizon',type=int,default=12)
    p.add_argument('--start',type=int,default=5)
    p.add_argument('--end',type=int)
    p.add_argument('--output',type=pathlib.Path,help='Optional separate directory for a sensitivity contract')
    p.add_argument('--control',default='ctl',help='Reference arm for seed-paired deltas')
    a=p.parse_args()
    folder=ROOT/'SimLogs'/a.tag
    output=a.output or folder
    output.mkdir(parents=True,exist_ok=True)
    plan=json.loads((folder/'plan.json').read_text())
    rows=[]; metrics=[]; events=[]
    for run in plan:
        receipt=folder/(run['name']+'-receipt.json')
        if not receipt.exists(): continue
        receipt=json.loads(receipt.read_text())
        if not receipt['complete'] or receipt['exitCode'] or receipt['assembly']!=receipt['assemblyAfter']: continue
        end=a.end or min(38,run['weeks']-a.horizon+1)
        if end<a.start: raise ValueError('Run too short for specified cohort')
        row,m,e=analyze(run,a.horizon,a.start,end)
        row |= dict(horizon=a.horizon,launchStart=a.start,launchEnd=end)
        rows.append(row); metrics.append(m); events.append(e)
    if not rows: return
    d=pd.DataFrame(rows)
    genre_cols=[c for c in d if c.startswith('genreUnitsPct_')]
    d[genre_cols]=d[genre_cols].fillna(0)
    d.to_csv(output/'metrics.csv',index=False)
    pd.concat(metrics).to_csv(output/'lead-metrics.csv',index=False)
    pd.concat(events).to_csv(output/'cohort-events.csv',index=False)
    numeric=d.select_dtypes('number').columns.drop('seed')
    d.groupby('arm',sort=False)[numeric].mean().to_csv(output/'means.csv')
    deltas=[]
    control=d[d.arm.eq(a.control)].set_index('seed')[numeric]
    for arm,g in d.groupby('arm',sort=False):
        if arm==a.control: continue
        diff=g.set_index('seed')[numeric].subtract(control).dropna(how='all')
        for metric in numeric:
            v=diff[metric].dropna()
            deltas.append(dict(arm=arm,metric=metric,n=len(v),delta=v.mean(),se=v.std()/len(v)**.5 if len(v)>1 else None))
    pd.DataFrame(deltas).to_csv(output/'paired.csv',index=False)
    show=['arm','seed','singleUnitsM','rawSinglesM','big5Top10Pct','top10UnitSharePct','closures','indie10_n','indie10_lead2','indie10_leadPct','41-100_n','41-100_leadPct','cohortUnchartedPct']
    print(d[show].round(2).to_string(index=False))

if __name__=='__main__': main()
