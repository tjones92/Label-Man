"""Reproducible eight-seed observer comparison and captured trial progression.
Reads existing logs only. Outputs under SimLogs/regprobe/playlist-eight-seed.
"""
import json
from pathlib import Path
import numpy as np
import pandas as pd
from score_playlist_pipeline import score, first_events

ROOT = Path(__file__).resolve().parents[1]
LOG = ROOT / 'SimLogs'
OUT = LOG / 'regprobe/playlist-eight-seed'
TAGS = ['ufxal025', 'djb24u-pt3']
PANEL = json.loads((ROOT/'SimTools/regional_observer_panel.json').read_text())
BIG5 = {f'label_000{i}' for i in range(1, 6)}


def path(seed, tag, suffix):
    return LOG / f'hs52op{seed}-SMP-HGRPO-{tag}-{suffix}'


def load(seed, tag):
    r = pd.read_csv(path(seed, tag, 'records.csv'), usecols=['week','year','recordId','labelId','weeksSinceRelease','currentPosition'])
    r = r[r.year.eq(1960)].copy()
    singles = set(pd.read_csv(path(seed, tag, 'single-launch-quality.csv'), usecols=['singleId']).singleId)
    r = r[r.recordId.isin(singles)].copy()
    r['launch'] = r.week - r.weeksSinceRelease + 1
    x = pd.read_csv(path(seed, tag, 'station-playlists.csv'))
    x = x[x.year.eq(1960) & x['format'].isin(['Top40','FullService'])].copy()
    d = r.sort_values('week').drop_duplicates('recordId').set_index('recordId')[['launch','labelId']]
    h = r[r.weeksSinceRelease.between(1,12) & r.currentPosition.between(1,100)]
    d = d.join(h.groupby('recordId').agg(debut=('week','min'),peak=('currentPosition','min')))
    d['bucket'] = pd.cut(d.peak.fillna(999), [0,10,40,100,1000], labels=['top10','11-40','41-100','uncharted12']).astype(str)
    d['big5'] = d.labelId.isin(BIG5)
    return r, x, d


def comparisons():
    metrics, listings, events, windows, histories, coverage, inventory = [], [], [], [], [], [], []
    for tag in TAGS:
        for seed in range(1001,1009):
            print('score',seed,tag,flush=True)
            console = LOG/'regprobe'/f'hs52op{seed}-SMP-HGRPO-{tag}-console.log'
            text = console.read_text(errors='replace')
            assert 'CHART_AUDIT_COMPLETE' in text
            inventory.append(dict(seed=seed,tag=tag,complete=True,console=str(console.relative_to(ROOT)),
                                  trial=next((s for s in text.splitlines() if s.startswith('STATION_TRIAL')), ''),
                                  picks=next((s for s in text.splitlines() if s.startswith('STATION_PICKS')), ''),
                                  exposure=next((s for s in text.splitlines() if s.startswith('REGIONAL_FIT_EXPOSURE')), ''),
                                  decisions=path(seed,tag,'station-decisions.jsonl').exists()))
            r,x,d = load(seed,tag)
            m,e,l = score(r,x,PANEL)
            for frame,dest in [(m,metrics),(e,events),(l,listings)]:
                frame['seed']=seed;frame['tag']=tag;dest.append(frame)
            last=r.groupby('recordId').week.max().reindex(d.index)
            release=d.launch.between(5,38)
            coverage.append(dict(seed=seed,tag=tag,releases=int(release.sum()),
                                 endedBefore12=int((release & last.lt(d.launch+11)).sum())))
            # Bridge to the original debut-window / run-end-peak headlines; then
            # separate cohort-window changes from panel and persistence changes.
            national=r[r.currentPosition.between(1,100)].groupby('recordId').agg(debut=('week','min'),peak=('currentPosition','min'))
            original=d[['launch','big5']].join(national)
            city=x[x.city.isin(PANEL)]
            for mode,z in [('full-world',city),('fixed-2+1',city[city.stationId.isin(sum(PANEL.values(),[]))])]:
                first,sustained=first_events(z)
                for contract,base in [('debut7-46_run-peak',original[original.debut.between(7,46)]),
                                      ('release5-38_peak12',d[d.launch.between(5,38)])]:
                    v=base.join(first.rename('first')).join(sustained.rename('sustained'))
                    v['opportunity']=v.debut-v.launch>=2
                    v['debutAge']=v.debut-v.launch+1
                    la=z[z.city.eq('Los Angeles')].groupby('recordId').week.min()
                    mp=z[z.city.eq('Minneapolis')].groupby('recordId').week.min()
                    v['gap']=(la-mp).abs()
                    masks={'indie10':v.peak.le(10)&~v.big5,'big5_10':v.peak.le(10)&v.big5,
                           '41-100':v.peak.between(41,100),'all':v.peak.notna()}
                    for cohort,mask in masks.items():
                        q=v[mask]
                        windows.append(dict(seed=seed,tag=tag,panel=mode,contract=contract,cohort=cohort,n=len(q),
                                            opportunity=int(q.opportunity.sum()),lead2=int((q.debut-q['first']).ge(2).sum()),
                                            early=int((q['first']-q.launch+1).le(2).sum()),
                                            both=int(q.gap.notna().sum()),gap6=int(q.gap.ge(6).sum()),
                                            debutAgeMedian=q.debutAge.median()))
                for cityname in PANEL:
                    fc=z[z.city.eq(cityname)].groupby('recordId').week.min()
                    fresh=fc[fc.between(5,44)]
                    # Bridge only: run-end outcome, so late listings get less follow-up.
                    listings.append(pd.DataFrame([dict(seed=seed,tag=tag,panel=mode,city=cityname,
                        window='first5-44_run-outcome',firstListings=len(fresh),perWeek=len(fresh)/40,
                        unchartedByRunEnd=int((~fresh.index.isin(national.index)).sum()))]))
            # Observed first spells and returns across all eight seeds. These are
            # not labelled trials without captured provenance.
            for (sid,rid),g in x.groupby(['stationId','recordId'],sort=False):
                if rid not in d.index:continue
                rec=d.loc[rid]
                if not 5<=rec.launch<=38:continue
                ws=sorted(set(g.week)); ws=[w for w in ws if w<=rec.launch+11]
                if not ws:continue
                first=ws[0]; age=first-rec.launch+1
                end=first
                while end+1 in ws:end+=1
                drop=end+1
                later=[w for w in ws if w>drop]
                histories.append(dict(seed=seed,tag=tag,stationId=sid,recordId=rid,city=g.city.iloc[0],
                    regionId=g.regionId.iloc[0],bucket=rec.bucket,big5=rec.big5,firstAge=age,spell=end-first+1,
                    dropped=drop<=rec.launch+11,dropAge=drop-rec.launch+1,
                    returned=bool(later),returnLag=later[0]-drop if later else np.nan))
    for name,frames in [('metrics',metrics),('events',events),('first-listings',listings)]:
        pd.concat(frames,ignore_index=True).to_csv(OUT/f'{name}.csv',index=False)
    for name,rows in [('window-comparison',windows),('station-histories',histories),('followup-coverage',coverage),('inventory',inventory)]:
        pd.DataFrame(rows).to_csv(OUT/f'{name}.csv',index=False)
    w=pd.DataFrame(windows)
    for metric,num,den in [('leadPct','lead2','n'),('adjustedPct','lead2','opportunity'),('gap6Pct','gap6','both'),('earlyPct','early','n')]:
        w[metric]=100*w[num]/w[den].replace(0,np.nan)
    w.to_csv(OUT/'window-comparison.csv',index=False)
    keys=['panel','contract','cohort']
    pooled=w.groupby(['tag']+keys)[['n','opportunity','lead2','early','both','gap6']].sum()
    for metric,num,den in [('leadPct','lead2','n'),('adjustedPct','lead2','opportunity'),('gap6Pct','gap6','both'),('earlyPct','early','n')]:
        pooled[metric]=100*pooled[num]/pooled[den].replace(0,np.nan)
    pooled.to_csv(OUT/'pooled-comparison.csv')
    paired=[]
    for k,g in w.groupby(keys):
        for metric in ['leadPct','adjustedPct','gap6Pct','earlyPct']:
            p=g.pivot(index='seed',columns='tag',values=metric).dropna()
            delta=p[TAGS[1]]-p[TAGS[0]]
            paired.append(dict(zip(keys,k),metric=metric,seeds=len(delta),baseline=p[TAGS[0]].mean(),trial=p[TAGS[1]].mean(),
                               delta=delta.mean(),se=delta.std(ddof=1)/np.sqrt(len(delta))))
    pd.DataFrame(paired).to_csv(OUT/'paired-comparison.csv',index=False)


def progression(seed=1001):
    tag=TAGS[1]; _,x,d=load(seed,tag)
    station_ids=set(x.stationId)
    trial, regional, admissions = {}, {}, []
    selected={(sid,int(w)):set(g.recordId) for (sid,w),g in x.groupby(['stationId','week'])}
    count=0
    with path(seed,tag,'station-decisions.jsonl').open() as stream:
        for line in stream:
            cap=json.loads(line);m=cap['Input'];sid=m['StationId'];week=m['Week']
            if sid not in station_ids:continue
            seats={s['Id']:s for s in cap['Result']['Seats']}
            assert set(seats)==selected.get((sid,week),set()), (sid,week)
            candidates={c['Id']:c for c in m['Candidates']}
            # Measure admission pressure separately from retention and graduation.
            eligible=[c for c in m['Candidates'] if c['Admit'] and c['PickScore']>0 and c['Rejection'] is None]
            admissions.append(dict(week=week,stationId=sid,city=m['City'],eligible=len(eligible),
                unseated=sum(c['Id'] not in seats for c in eligible),
                newTrials=sum(s['Source']=='trial' and not candidates[s['Id']]['ActiveTrial'] for s in seats.values()),
                continuing=sum(s['Source']=='trial' and candidates[s['Id']]['ActiveTrial'] for s in seats.values()),
                graduated=sum(s['Source']=='graduated' for s in seats.values())))
            for rid,c in candidates.items():
                if rid not in d.index:continue
                rk=(rid,m['RegionId'])
                reg=regional.setdefault(rk,dict(recordId=rid,regionId=m['RegionId'],launchExposed=c['LaunchExposed'],firstBreak=np.nan))
                if 1<=c['LocalRank']<=40 and np.isnan(reg['firstBreak']):reg['firstBreak']=week
                reg['exposureAge']=c['ExposureAge'];reg['exposureCause']=c['ExposureCause']
                rec=d.loc[rid]
                if not 5<=rec.launch<=38 or not rec.launch<=week<=rec.launch+11:continue
                key=(sid,rid);seat=seats.get(rid)
                if seat and seat['Source']=='trial' and key not in trial:
                    trial[key]=dict(seed=seed,stationId=sid,recordId=rid,city=m['City'],regionId=m['RegionId'],
                        bucket=rec.bucket,big5=bool(rec.big5),launch=int(rec.launch),admit=week,admitAge=c['ReleaseAge'],
                        launchExposed=c['LaunchExposed'],graduation=np.nan,sustained=np.nan,drop=np.nan,returned=np.nan,
                        firstBreakAfterDrop=np.nan,top40AfterCooldown=False,lastOrdinary=-1,ordinaryStreak=0,
                        trialWeeks=0,dropReason='',dropScoreRatio=np.nan,rankAtDrop=np.nan)
                if key not in trial:continue
                t=trial[key]
                if seat:
                    if seat['Source']=='trial':t['trialWeeks']+=1
                    else:
                        if np.isnan(t['graduation']) and seat['Source']=='graduated':t['graduation']=week
                        t['ordinaryStreak']=t['ordinaryStreak']+1 if t['lastOrdinary']==week-1 else 1
                        t['lastOrdinary']=week
                        if t['ordinaryStreak']>=2 and np.isnan(t['sustained']):t['sustained']=week
                    if not np.isnan(t['drop']) and np.isnan(t['returned']):t['returned']=week
                elif np.isnan(t['drop']):
                    t['drop']=week;t['dropReason']=cap['Result']['Rejections'].get(rid,'missing')
                    t['rankAtDrop']=c['LocalRank']
                    ordinary=[candidates[s['Id']]['Score'] for s in seats.values() if s['Tier']==1 and s['Source'] in ('ranked','graduated')]
                    if ordinary and min(ordinary)>0:t['dropScoreRatio']=c['Score']/min(ordinary)
                if not np.isnan(t['drop']) and 1<=c['LocalRank']<=40:
                    if np.isnan(t['firstBreakAfterDrop']):t['firstBreakAfterDrop']=week
                    if week>=t['drop']+4:t['top40AfterCooldown']=True
            count+=1
            if count%400==0:print('capture meetings',count,flush=True)
    tr=pd.DataFrame(trial.values()).drop(columns=['lastOrdinary','ordinaryStreak'])
    reg=pd.DataFrame(regional.values())
    tr=tr.merge(reg[['recordId','regionId','firstBreak']],on=['recordId','regionId'],how='left')
    tr['localBreak12']=tr.firstBreak.le(tr.launch+11)
    tr['graduated']=tr.graduation.notna();tr['sustainedOrdinary']=tr.sustained.notna()
    tr['expiredWithoutGraduation']=tr['drop'].notna() & (tr.graduation.isna() | tr['drop'].lt(tr.graduation))
    footprint=reg.pivot(index='recordId',columns='regionId',values='launchExposed')
    tr['bothCityRegionsLaunchExposed']=tr.recordId.map((footprint.westcoast & footprint.greatplains).to_dict())
    tr.to_csv(OUT/'explicit-trials-1001.csv',index=False)
    reg.to_csv(OUT/'regional-evidence-1001.csv',index=False)
    pd.DataFrame(admissions).to_csv(OUT/'admission-pressure-1001.csv',index=False)
    rows=[]
    for scope,z in [('all-Top40-FullService',tr),('two-cities',tr[tr.city.isin(PANEL)]),
                    ('fixed-2+1',tr[tr.stationId.isin(sum(PANEL.values(),[]))])]:
        for bucket,g in z.groupby('bucket'):
            rows.append(dict(scope=scope,bucket=bucket,n=len(g),records=g.recordId.nunique(),
                graduated=int(g.graduated.sum()),sustained=int(g.sustainedOrdinary.sum()),localBreak12=int(g.localBreak12.sum()),
                expired=int(g.expiredWithoutGraduation.sum()),returned=int(g.returned.notna().sum()),
                top40AfterCooldown=int(g.top40AfterCooldown.sum()),
                returnedAfterLocalBreak=int((g.top40AfterCooldown & g.returned.notna()).sum()),
                medianDropScoreRatio=g.loc[g.expiredWithoutGraduation,'dropScoreRatio'].median()))
    pd.DataFrame(rows).to_csv(OUT/'trial-progression-summary.csv',index=False)
    print('explicit trials',len(tr),'meetings matched',count,flush=True)


if __name__=='__main__':
    import argparse
    p=argparse.ArgumentParser();p.add_argument('--capture-only',action='store_true');p.add_argument('--comparison-only',action='store_true');a=p.parse_args()
    OUT.mkdir(parents=True,exist_ok=True)
    if not a.capture_only:comparisons()
    if not a.comparison_only:progression()
