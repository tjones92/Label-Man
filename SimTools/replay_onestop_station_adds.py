"""Ceiling for a station that adds a new record (age <= MAXAGE) once its local units rank among new releases is <= B, the week after."""
import sys; sys.path.insert(0,"SimTools")
import numpy as np, pandas as pd
import replay_regional_relive as R
zs={s:R.loadab(s) for s in range(1001,1009)}
def go(lab,B,maxage=6,lag=1,**kw):
    rows=[]
    for s,z in zs.items():
        r=R.run(z,clock="relive",nat="chart",demote=.28,demote_indie=.073,**kw)
        nat=r["_nat"]; L=z["launch"]; live=L>=1; n=len(L)
        natp=np.where(nat>0,nat,10**6); ch=natp[1:]<=100
        debut=np.where(ch.any(0),ch.argmax(0)+1,0); peak=np.where(ch.any(0),natp[1:].min(0),999)
        # rebuild units from regional ranks is impossible; rerun to get units: use R from run is ranks over all; compute new-release rank from ranks
        RR=r["_R"]
        add={}
        for g in (6,2):
            first=np.full(n,np.nan)
            for t in range(1,53):
                age=t-L+1; young=live&(age>=1)&(age<=maxage)
                rk=np.where(young,RR[t,:,g],10**6); o=np.argsort(rk); nr=np.empty(n,int); nr[o]=np.arange(1,n+1)
                hit=young&(nr<=B)&np.isnan(first)
                first[hit]=t+lag
            add[g]=first
        fa=np.fmin(add[6],add[2])
        m=live&(debut>=7)&(debut<=46)&(peak<=10)&~z["big5"]
        mf=live&(debut==0)
        for i in np.where(m)[0]: rows.append(dict(kind="hit",lead=debut[i]-fa[i],debage=debut[i]-L[i]+1))
        # how many adds per region-week go to records that never chart (flops)
        rows.append(dict(kind="adds",n=np.isfinite(add[6]).sum()+np.isfinite(add[2]).sum(),flop=(np.isfinite(add[6])&mf).sum()+(np.isfinite(add[2])&mf).sum()))
    d=pd.DataFrame(rows); h=d[d.kind=="hit"]; a=d[d.kind=="adds"]
    print(f"{lab:22s} B{B:3d} indie top10 n{len(h)} added before debut {100*(h.lead>=1).mean():4.1f}  lead>=2 {100*(h.lead>=2).mean():4.1f}  never added {100*h.lead.isna().mean():4.1f} | adds/region-wk {a.n.sum()/16/52:.1f}, never-charting {100*a.flop.sum()/a.n.sum():.0f}%",flush=True)
for B in [5,10,20,40]: go("ufxal025 replay",B)
for B in [10,20]: go("ufxal025 lag0 (ceiling)",B,lag=0)
for B in [10,20]: go("p0/.2 launch",B,p0=0,p1=.2)
