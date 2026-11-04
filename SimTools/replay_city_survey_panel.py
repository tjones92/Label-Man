"""Equal-sized city panels: every combination of two LA and one Minneapolis station.
Frozen recorded playlists and national outcomes; reports all combinations, not a
cherry-picked panel. Historical comparator has KFWB+KRLA and KDWB. Model station
formats/list depths still differ, so equal counts are only a sensitivity check.
"""
from itertools import combinations, product
import pandas as pd
from audit_regional_playlist_pipeline import OUT, read, CITIES


def main():
    records=pd.read_csv(OUT/'records.csv');rows=[];precision=[];gaps=[]
    for (tag,seed),d in records.groupby(['tag','seed']):
        x=read(seed,tag,'station-playlists')
        x=x[x.year.eq(1960)&x.format.isin(['Top40','FullService'])&x.city.isin(CITIES)]
        d=d.set_index('recordId')
        first=x.groupby(['recordId','stationId']).week.min().unstack()
        la=sorted(x[x.city.eq(CITIES[0])].stationId.unique())
        mp=sorted(x[x.city.eq(CITIES[1])].stationId.unique())
        both=d.join(first[la].min(axis=1).rename('la')).join(first[mp].min(axis=1).rename('mp'))
        both=both[both.debut.between(7,46)&both.peak.between(41,100)&both.la.notna()&both.mp.notna()]
        both['gap']=(both.la-both.mp).abs()
        both['fitClass']=both.fitAboveCap.le(0).map({True:'at_or_below_cap',False:'above_cap'})
        for cls,g in both.groupby('fitClass'):
            gaps.append(dict(tag=tag,seed=seed,fitClass=cls,n=len(g),gap6=int(g.gap.ge(6).sum()),
                             aboveKink=int(g.aboveKink.sum()),sameWeek=int(g.gap.eq(0).sum())))
        panels=[('all',la,mp)]+[(f'{a}|{b}',list(a),list(b)) for a,b in product(combinations(la,2),combinations(mp,1))]
        for name,a,b in panels:
            f=first[a+b].min(axis=1).reindex(d.index)
            lead=d.debut-f
            gap=(first[a].min(axis=1)-first[b].min(axis=1)).abs().reindex(d.index)
            v=d.debut.between(7,46)
            for group,m in [('indie10',d.peak.le(10)&~d.big5),('big5_10',d.peak.le(10)&d.big5),
                            ('41-100',d.peak.between(41,100)),('all',d.peak.le(100))]:
                mask=m&v
                rows.append(dict(seed=seed,tag=tag,panel=name,group=group,n=int(mask.sum()),lead2=int((mask&lead.ge(2)).sum()),
                                 both=int((mask&gap.notna()).sum()),gap6=int((mask&gap.ge(6)).sum())))
            for city,stations in zip(CITIES,[a,b]):
                fc=first[stations].min(axis=1);fc=fc[fc.between(5,44)]
                precision.append(dict(seed=seed,tag=tag,panel=name,city=city,n=len(fc),never=int(d.reindex(fc.index).peak.isna().sum())))
    r=pd.DataFrame(rows);r.to_csv(OUT/'survey-panels.csv',index=False)
    pd.DataFrame(gaps).to_csv(OUT/'minor-hit-gap-fit.csv',index=False)
    p=pd.DataFrame(precision);p.to_csv(OUT/'survey-panel-precision.csv',index=False)
    r['mode']=r.panel.map(lambda x:'all' if x=='all' else '2LA_1MP')
    # Equal-weight panels within each seed, then record-weight across seeds.
    g=r.groupby(['tag','seed','mode','group'])[['n','lead2','both','gap6']].mean().groupby(['tag','mode','group']).sum()
    g['leadPct']=100*g.lead2/g.n;g['gap6Pct']=100*g.gap6/g.both;g.to_csv(OUT/'survey-panel-summary.csv')
    print(g.leadPct.unstack().round(2).to_string())


if __name__=='__main__':main()
