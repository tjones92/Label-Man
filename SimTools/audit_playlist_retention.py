"""Follow early trial plays through drops, lockouts and local sales evidence.
Requires audit_regional_playlist_pipeline.py outputs. Static logs only; no new seeds.
"""
import argparse
import numpy as np
import pandas as pd
from audit_regional_playlist_pipeline import OUT, read


def run(seed, tag, records):
    d = records[(records.seed == seed)&(records.tag == tag)].set_index('recordId')
    x = read(seed,tag,'station-playlists')
    x = x[x.year.eq(1960)&x.format.isin(['Top40','FullService'])].copy()
    st = read(seed,tag,'single-demand-stages',['week','year','recordId','region','clearedUnits','age'])
    st = st[st.year.eq(1960)].sort_values(['week','region','recordId'])
    st['localRank'] = st.groupby(['week','region']).clearedUnits.rank(method='first',ascending=False)
    st.loc[st.clearedUnits.le(0),'localRank'] = np.inf
    ranks = {(rid,g,w):r for rid,g,w,r in zip(st.recordId,st.region,st.week,st.localRank)}
    rows = []
    for (sid,rid),g in x.groupby(['stationId','recordId']):
        if rid not in d.index: continue
        rec = d.loc[rid]
        if not rec.liveSingle or not 5 <= rec.launch <= 38: continue
        weeks = sorted(g.week.unique()); first = weeks[0]; age=first-rec.launch+1
        if age > 2: continue
        end=first
        while end+1 in weeks: end+=1
        drop=end+1
        if drop > 48: continue
        region=g.regionId.iloc[0]
        later=[w for w in weeks if w>drop]
        ranked=[w for w in range(drop, min(53,int(rec.launch+12))) if ranks.get((rid,region,w),np.inf)<=40]
        locked=[w for w in ranked if w<drop+4]
        after=[w for w in ranked if w>=drop+4]
        rows.append(dict(seed=seed,tag=tag,stationId=sid,recordId=rid,bucket=rec.bucket,
            first=first,firstAge=age,drop=drop,dropAge=drop-rec.launch+1,firstSpell=end-first+1,
            returned=bool(later),returnLag=later[0]-drop if later else np.nan,
            top40DuringLock=bool(locked),top40AfterLock=bool(after),
            missedAfterLock=bool(after) and not any(w in weeks for w in after)))
    return pd.DataFrame(rows)


def main():
    p=argparse.ArgumentParser();p.add_argument('--seeds',default='1001,1002,1003,1004');a=p.parse_args()
    records=pd.read_csv(OUT/'records.csv'); frames=[]
    for tag in ['ufxal025','djb24u']:
        for seed in map(int,a.seeds.split(',')):
            print(tag,seed,flush=True);frames.append(run(seed,tag,records))
    d=pd.concat(frames,ignore_index=True);d.to_csv(OUT/'early-drops.csv',index=False)
    summary=d.groupby(['tag','bucket']).agg(n=('recordId','size'),spell=('firstSpell','median'),
        dropAge=('dropAge','median'),returned=('returned','mean'),top40DuringLock=('top40DuringLock','mean'),
        top40AfterLock=('top40AfterLock','mean'),missedAfterLock=('missedAfterLock','mean'))
    summary.to_csv(OUT/'retention-summary.csv');print(summary.round(3).to_string())


if __name__=='__main__':main()
