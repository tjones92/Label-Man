"""Capacity-constrained trial retention on saved djb24u proposals.

Freeze the recorded sales-ranked prefix, national outcomes and original proposals.
The two trailing seats are identifiable pick seats when rank exceeds capacity-2.
Keep an admitted trial for H weeks unless it graduates into that week's ranked
prefix; new proposals compete only for free seats. A refused or expired proposal
cannot start again. This is a conservative subset replay (no unseen alternatives,
no demand feedback, and no full reproduction of station candidacy or drop state).
"""
import pandas as pd
from audit_regional_playlist_pipeline import OUT, read, metric, CITIES


def replay(x,hold):
    selected=[]
    for sid,g in x.groupby('stationId',sort=False):
        active={};seen=set()
        for week,w in g.groupby('week',sort=True):
            ranked=w[~w.tailPickSeat]
            seated=set(ranked.recordId)
            active={rid:(start,row) for rid,(start,row) in active.items()
                    if week-start<hold and rid not in seated}
            candidates=w[w.tailPickSeat].sort_values('rank')
            for _,row in candidates.iterrows():
                rid=row.recordId
                if rid in seen:continue
                seen.add(rid)
                if len(active)<2 and rid not in seated:active[rid]=(week,row)
            selected.append(ranked)
            for rid,(start,row) in active.items():
                row=row.copy();row['week']=week
                selected.append(pd.DataFrame([row]))
    return pd.concat(selected,ignore_index=True)


def main():
    records=pd.read_csv(OUT/'records.csv');metrics=[];counts=[]
    for seed in sorted(records.seed.unique()):
        d=records[(records.seed==seed)&records.tag.eq('djb24u')].set_index('recordId')
        x=read(seed,'djb24u','station-playlists')
        x=x[x.year.eq(1960)&x.format.isin(['Top40','FullService'])].copy()
        x['tailPickSeat']=x['rank'].gt(x['format'].map({'Top40':33,'FullService':34}))
        for hold in [1,2,3,4]:
            print(seed,hold,flush=True)
            y=replay(x,hold)
            metrics.extend(metric(d,y,f'trial_hold_{hold}',seed,'djb24u'))
            for city,g in y[y.city.isin(CITIES)].groupby('city'):
                f=g.groupby('recordId').week.min();f=f[f.between(5,44)]
                counts.append(dict(seed=seed,hold=hold,city=city,n=len(f),never=int(d.reindex(f.index).peak.isna().sum())))
    pd.DataFrame(metrics).to_csv(OUT/'trial-slots-per-seed.csv',index=False)
    m=pd.DataFrame(metrics).groupby(['scenario','group'])[['n','lead2']].sum()
    m['leadPct']=100*m.lead2/m.n;m.to_csv(OUT/'trial-slots-pooled.csv')
    pd.DataFrame(counts).to_csv(OUT/'trial-slots-precision.csv',index=False)
    print(m.leadPct.unstack().round(1).to_string())


if __name__=='__main__':main()
