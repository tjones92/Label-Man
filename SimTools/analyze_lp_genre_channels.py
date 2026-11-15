"""LP genre shares by channel and artistless-LP rack sales (LP channel handoff, 2026-11-15).

Run from SimLogs/: python ../SimTools/analyze_lp_genre_channels.py cand-v3-2002-off bud-v2-2002-lwbr ...
The LP universe is demand-release-census (album-projects misses about 140 artistless launches a run).
Store shares are lifetime units of LPs launched in the year; rack and budget shares are calendar-year units.
"""
import pandas as pd,sys
G=['Folk','Classical','EasyListening','TraditionalPop','Jazz','Soul','Country','Soundtrack','Comedy']
for t in sys.argv[1:]:
    c=pd.read_csv(f'{t}-demand-release-census.csv',usecols=['recordId','artistId','format','genre','observationKind','observedAuditWeek'])
    c=c[c.format=='Album'].drop_duplicates('recordId').set_index('recordId')
    ro=pd.read_csv(f'{t}-release-outcomes.csv',usecols=['recordId','format','pressingBookSold'])
    lv=pd.read_csv(f'{t}-live-records-snapshot.csv'); lv=lv[lv.week==lv.week.max()]
    u=pd.concat([ro[ro.format=='Album'].set_index('recordId').pressingBookSold, lv[lv.format=='Album'].set_index('recordId').totalUnitsSold])
    u=u[~u.index.duplicated(keep='last')]
    rkt=pd.read_csv(f'{t}-rack-title-weekly.csv',usecols=['year','recordId','units'])
    a=c.join(u.rename('store')).join(rkt.groupby('recordId').units.sum().rename('rack')).fillna({'store':0,'rack':0})
    a['launch']=(a.observedAuditWeek-1)//52+1960
    L=a[a.observationKind=='Launch']
    rkt['genre']=rkt.recordId.map(c.genre)
    bp=pd.read_csv(f'{t}-budget-product-weekly.csv',usecols=['year','genre','units'])
    out={}
    for g in G:
        r={}
        for y in (1960,1961):
            s=L[L.launch==y]; r[f'store {y} launches']=100*s[s.genre==g].store.sum()/s.store.sum()
        for y in (1961,1963):
            x=rkt[rkt.year==y]; r[f'rack {y}']=100*x[x.genre==g].units.sum()/x.units.sum()
            b=bp[bp.year==y]; r[f'budget {y}']=100*b[b.genre==g].units.sum()/b.units.sum()
        out[g]=r
    print(t); print(pd.DataFrame(out).round(1).to_string())
    na=L[L.artistId.isna()]
    print(f'  artistless launches: {len(na)} ({100*len(na)/len(L):.1f}% of LP launches); store {na.store.sum()/1e6:.1f}M ({100*na.store.sum()/L.store.sum():.1f}%), rack {na.rack.sum()/1e6:.1f}M ({100*na.rack.sum()/max(1,L.rack.sum()):.1f}% of launched-LP rack units)')
    print('   by genre (n, store M, rack M):', {g:(len(x),round(x.store.sum()/1e6,2),round(x.rack.sum()/1e6,2)) for g,x in na.groupby('genre')})
    top=L.sort_values('rack',ascending=False).head(5)
    print('   top rack titles:', [(g, 'no act' if pd.isna(ar) else 'act', int(r_)//1000, int(s_)//1000) for g,ar,r_,s_ in zip(top.genre,top.artistId,top.rack,top.store)])
    cl=L[L.genre=='Classical']; print(f'   Classical rack/store {cl.rack.sum()/cl.store.sum():.2f} vs all {L.rack.sum()/L.store.sum():.2f}')
