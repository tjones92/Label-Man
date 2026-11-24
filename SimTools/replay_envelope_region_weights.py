"""Offline re-split of the retail purchase envelope with other regional album-purse weights (LpGenreAllocationFollowup.md §28).

Run from SimLogs/: python ../SimTools/replay_envelope_region_weights.py <prefix> [rackWeight] (seeds 2002 1001 3003, 1961).
Applies RetailPurchaseEnvelope.Split to logged region-week intents; national purse fixed, regional shares changed."""
import sys
PREFIX=sys.argv[1]; RW=float(sys.argv[2]) if len(sys.argv)>2 else 1.3
import pandas as pd, numpy as np, math
R={'greatlakes':(36.1,.76,1.08,.35),'greatplains':(15.5,.56,.98,.35),'eastcoast':(52.2,.70,1.15,.32),'westcoast':(20.3,.82,1.18,.38),'deepsouth':(15.0,.48,.78,.40),'rockies':(4.6,.58,.95,.39),'southwest':(14.2,.62,.90,.41)}
def w0(p,u,i,y): return p*(.3+.5*y)*math.sqrt(i)*(.6+.4*u)
W={'current':w0,
   'pop*sqrtInc':lambda p,u,i,y:p*math.sqrt(i),
   'adult*sqrtInc*urb':lambda p,u,i,y:p*(1-y)*math.sqrt(i)*(.6+.4*u),
   'adult*inc*urb':lambda p,u,i,y:p*(1-y)*i*(.6+.4*u),
   'adult*inc^1.5*urb':lambda p,u,i,y:p*(1-y)*i**1.5*(.6+.4*u)}
def share(f):
    t=sum(f(*R[r]) for r in R); return {r:f(*R[r])/t for r in R}
W={k:W[k] for k in ["current","adult*sqrtInc*urb","adult*inc*urb"]}
for k,f in W.items(): print(k,{r:round(v,3) for r,v in share(f).items()})
def split(store,auto,purse,rw=1.3):
    rw=RW
    if store+auto<=purse: return store,auto
    if store==0: return 0,min(purse,auto)
    s=math.floor(purse*store/(store+auto*rw)); a=min(purse-s,auto); s=min(store,purse-a); return s,a
for s in ['2002','1001','3003']:
    e=pd.read_csv(f'{PREFIX}-{s}-sc-retail-purchase-envelope.csv'); 
    ch=pd.read_csv(f'{PREFIX}-{s}-sc-lp-channel-choice-weekly.csv')
    rt=pd.read_csv(f'{PREFIX}-{s}-sc-rack-title-weekly.csv').groupby(['week','regionId']).demand.sum().rename('rdem')
    bp=pd.read_csv(f'{PREFIX}-{s}-sc-budget-product-weekly.csv'); bp['ba']=(bp.demand-bp.switchedStoreUnits).clip(lower=0)
    bp=bp.groupby(['week','regionId']).ba.sum()
    d=e.merge(ch[['week','regionId','frontPlanned']],on=['week','regionId']).set_index(['week','regionId']).join(rt).join(bp).fillna(0).reset_index()
    d=d[d.year==1961]
    d['autoI']=np.maximum(d.rdem-d.frontPlanned,0)+d.ba
    d['autoI']=np.maximum(d.autoI,d.autonomousAllowance)
    tot=d.groupby('week').budget.transform('sum')
    for k,f in W.items():
        sh=share(f); purse=tot*d.regionId.map(sh)
        res=[split(a,b,c) for a,b,c in zip(d.storeBefore,d.autoI,purse)]
        sa=np.array([r[0] for r in res]); aa=np.array([r[1] for r in res])
        swr=np.where(d.storeAllocated>0,d.switchers/d.storeAllocated.clip(lower=1),0)
        final=sa*(1-swr)
        print(s,k,'stores %.1f'%(final.sum()/1e6),'(logged %.1f)'%(d.finalStores.sum()/1e6),'autoAllow %.1f'%(aa.sum()/1e6),'(logged %.1f)'%(d.autonomousAllowance.sum()/1e6), 'bound %.0f%%'%(100*np.mean(d.storeBefore+d.autoI>purse)))
