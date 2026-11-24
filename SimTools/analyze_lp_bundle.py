"""LP bundle screen metrics (GameyBehaviorAudit.md 2.9-2.9c).

Run from SimLogs/: python ../SimTools/analyze_lp_bundle.py cand-v3-2002-off bud-v2-2002-lwbr ...
Each argument is a run name (SimLogs/<run>-*.csv; bankruptcies from SimLogs/writing-calibration/<run>.log).
LP cohort = LPs launched weeks 1-52, lifetime at the last snapshot. Acts map through demand-release-census.
"""
import pandas as pd,numpy as np,sys,re
def an(t):
    o={}
    ro=pd.read_csv(f'{t}-release-outcomes.csv',usecols=['recordId','format','pressingBookSold','realizedNet'])
    lv=pd.read_csv(f'{t}-live-records-snapshot.csv'); lv=lv[lv.week==lv.week.max()]
    u=pd.concat([ro.rename(columns={'pressingBookSold':'units','realizedNet':'net'})[['recordId','format','units','net']],
                 lv.rename(columns={'totalUnitsSold':'units','observedNetLowerBound':'net'})[['recordId','format','units','net']]]).drop_duplicates('recordId',keep='last').set_index('recordId')
    api=pd.read_csv(f'{t}-demand-release-census.csv',usecols=['recordId','artistId']).drop_duplicates('recordId').join(u,on='recordId',how='inner')
    dw=pd.read_csv(f'{t}-album-projects.csv',usecols=['albumRecordId','dropWeek']).dropna().drop_duplicates('albumRecordId').set_index('albumRecordId').dropWeek
    api['week']=api.recordId.map(dw)
    sg=api[api.format=='Single'].groupby('artistId').units.max()
    lp=api[(api.format=='Album')&(api.week<=52)].copy(); lp['best']=lp.artistId.map(sg).fillna(0)
    o['LP med, best single <1k']=lp[lp.best<1000].units.median()
    o['LP med, best single 100k+']=lp[lp.best>=100000].units.median()
    o['LPs losing money %']=100*(lp.net<0).mean()
    s=lp.units.sort_values(ascending=False); o['top10% share']=100*s.head(len(s)//10).sum()/s.sum()
    o['cohort LP units M']=s.sum()/1e6; o['top LP store k']=s.max()/1e3
    ru=pd.read_csv(f'{t}-decade-annual-rollup.csv',usecols=['year','albumUnits']).set_index('year').albumUnits
    rk=pd.read_csv(f'{t}-rack-title-weekly.csv',usecols=['year','units']).groupby('year').units.sum()
    bp=pd.read_csv(f'{t}-budget-product-weekly.csv',usecols=['year','productId','provenance','units'])
    by=bp.groupby('year').units.sum()
    cm=pd.read_csv(f'{t}-company-monthly.csv',usecols=['year','recordGross']).groupby('year').recordGross.sum()
    for y in (1960,1961,1962,1963):
        o[f'{y} store/rack/budget M']='%.1f / %.1f / %.1f'%(ru.get(y,0)/1e6,rk.get(y,0)/1e6,by.get(y,0)/1e6)
        o[f'{y} gross $M']=round(cm.get(y,0)/1e6)
    c=pd.read_csv(f'{t}-budget-product-catalogue.csv')
    m=c[c.provenance=='OwnMaster'].copy()
    m['src']=m.sourceId.map(u.units); m['launch']=m.sourceId.map(api.set_index('recordId').week)
    m['age']=m.releaseWeek-m.launch
    rel=m[m.releaseWeek<=208]
    o['own-master editions released']=len(rel); o['their median age at reissue']=rel.age.median()
    o['editions from hits (store>=100k) %']=100*(rel.src>=100000).mean() if len(rel) else 0
    for y in (1961,1963): o[f'{y} budget OwnMaster %']=100*bp[(bp.year==y)&(bp.provenance=='OwnMaster')].units.sum()/max(1,bp[bp.year==y].units.sum())
    o['budget units from OwnMaster %']=100*bp[bp.provenance=='OwnMaster'].units.sum()/max(1,bp.units.sum())
    o['top edition k / its store k']='%.0f / %.0f'%(m.units.max()/1e3, m.loc[m.units.idxmax(),'src']/1e3) if len(m) else '-'
    log=open(f'writing-calibration/{t}.log',encoding='utf-8',errors='ignore').read()
    o['bankruptcies (208 wk)']=len(re.findall('Reason: Bankruptcy',log))
    return o
ts=sys.argv[1:]
r=pd.DataFrame({t:an(t) for t in ts}); pd.set_option('display.width',250); pd.set_option('display.max_colwidth',40); pd.set_option('display.max_columns',20); r.columns=[c.replace('cand-v3-','').replace('bud-v1-','') for c in r.columns]; print(r.to_string())
