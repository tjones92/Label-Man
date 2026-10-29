"""RegionalSyncDesign.md §9u. Replay v2 of exposure-gated fit realization (§9s/§9t), 1960, on a base run's ungated demand.

v1 (replay_exposure_gated_fit.py) ran a late region on its whole local clock, so a late region relived the awareness ramp
too, and gave indie gap >= 6 of 15% where the built game (§9t) gave ~1%. In the game a late region enters its top 40
the week it is exposed: awareness is national and only the post-week-8 decay is local. Clocks here:
  national  late region reads the record's national-age demand (= the §9t game; calibrated against ufx/ufxl075)
  local     v1: the whole demand curve on the local clock
  hybrid    awareness national, conversion on the local clock
  relive    the §9u game rule: national-age demand x chart-exposure weight (Hesbacher) at the position the record held
            at the local age / at its current one, x release ramp and age decay on the local age
Rules: launch = home + keyed draw < p0 + p1*reach; spread for connected records: neighbour of an exposed region in its
local top KN after DN weeks; every region after DH once the national NAT rank (units, or an in-replay chart = game
units and airplay scaled by the replay/base demand ratio, airplay^beta) is <= KH; radio = a region adds records in an
adjacent region's top KR, at most RADCAP a week, after DR. --demote f / --demote_indie f: share of connected Big-5 / indie singles that never
connect. From P(hit) = Phi(LW (launchAwareness - 0.2) - kink) on 1960 launches, relative to LW 1.25:
LW 0.75 = 0.16 / 0.043, 0.5 = 0.22 / 0.061, 0.25 = 0.28 / 0.073, 0 = 0.34 / 0.085. Metrics as model_spread.py on regional demand ranks (westcoast vs greatplains).
Usage: replay_regional_relive.py SEEDS [--tag ufw085] [--clock relive] [--nat chart] [--demote .16] ...
"""
import argparse, hashlib, os
import numpy as np, pandas as pd
L=str(__import__("pathlib").Path(__file__).resolve().parents[1]).replace("\\","/")+"/SimLogs/"; SP=L+"regprobe/replay2/"
os.makedirs(SP,exist_ok=True)
BIG5={f"label_000{i}" for i in range(1,6)}
REG=["eastcoast","greatlakes","greatplains","deepsouth","southwest","rockies","westcoast"]
A,W=80,52
def load(seed,tag):
    f=SP+f"{seed}_{tag}.npz"
    if os.path.exists(f):
        z=np.load(f,allow_pickle=True); return {k:z[k] for k in z.files}
    pre=L+f"hs52op{seed}-SMP-HGRPO-{tag}-"
    st=pd.read_csv(pre+"single-demand-stages.csv",usecols=["week","year","recordId","region","age","rawDemand","clearedUnits","intrinsicConversionRate","intrinsicQualityFactor"])
    st=st[st.year.eq(1960)]
    ids=np.array(sorted(st.recordId.unique())); ix={r:i for i,r in enumerate(ids)}
    st["ri"]=st.recordId.map(ix); st["gi"]=st.region.map({g:i for i,g in enumerate(REG)})
    launch=(st.week-st.age+1).groupby(st.ri).min().reindex(range(len(ids))).to_numpy()
    D=np.zeros((len(ids),7,A),np.float32); C=np.zeros_like(D); O=np.full_like(D,np.nan)
    ok=st.age.between(1,A-1); s=st[ok]
    D[s.ri,s.gi,s.age]=s.rawDemand; C[s.ri,s.gi,s.age]=s.clearedUnits
    O[s.ri,s.gi,s.age]=np.log((s.intrinsicConversionRate/s.intrinsicQualityFactor.clip(lower=1e-9)).clip(lower=1e-12))
    oc=np.log((st.intrinsicConversionRate/st.intrinsicQualityFactor.clip(lower=1e-9)).clip(lower=1e-12))
    e=st[st.age.between(1,3)].assign(oc=oc); e["rel"]=e.oc-e.groupby(["region","age","week"]).oc.transform("median")
    fit=e.groupby("ri").rel.max().reindex(range(len(ids))).fillna(0).to_numpy()  # max over regions: the realized national draw where exposed
    rec=pd.read_csv(pre+"records.csv",usecols=["week","year","recordId","labelId","currentPosition","unitsThisWeek","unsampledChartPoints","surveySample"])
    rec=rec[rec.year.eq(1960)&rec.recordId.isin(ix)]
    lab=pd.Series(ids).map(rec.drop_duplicates("recordId").set_index("recordId").labelId)
    big5=lab.isin(BIG5).to_numpy()
    home=lab.map(pd.read_csv(pre+"label-geography.csv").set_index("labelId").homeRegion).map({g:i for i,g in enumerate(REG)}).fillna(-1).astype(int).to_numpy()
    lr=pd.read_csv(pre+"label-finance.csv",usecols=["labelId","nationalReach"]).groupby("labelId").nationalReach.median()
    reach=lab.map(lr).fillna(0).to_numpy()
    P=np.zeros((W+1,len(ids)),np.int32); U=np.zeros((W+1,len(ids)),np.float32); AIR=np.zeros_like(U); SS=np.ones_like(U)
    rr=rec.recordId.map(ix).to_numpy(); ww=rec.week.to_numpy()
    P[ww,rr]=rec.currentPosition; U[ww,rr]=rec.unitsThisWeek; AIR[ww,rr]=(rec.unsampledChartPoints-rec.unitsThisWeek).clip(lower=0); SS[ww,rr]=rec.surveySample
    out=dict(ids=ids,D=D,C=C,O=O,launch=launch,fit=fit,big5=big5,home=home,reach=reach,P=P,U=U,AIR=AIR,SS=SS)
    np.savez(f,**out); return out
def loadab(seed,tag="ufw085"):
    f=SP+f"{seed}_{tag}_ab.npy"
    z=load(seed,tag)
    if not os.path.exists(f):
        st=pd.read_csv(L+f"hs52op{seed}-SMP-HGRPO-{tag}-single-demand-stages.csv",usecols=["year","recordId","region","age","awareBuyers"])
        st=st[st.year.eq(1960)&st.age.between(1,79)]
        ix={r:i for i,r in enumerate(z["ids"])}
        AB=np.zeros(z["D"].shape,np.float32); AB[st.recordId.map(ix),st.region.map({g:i for i,g in enumerate(REG)}),st.age]=st.awareBuyers
        np.save(f,AB)
    z["AB"]=np.load(f); return z
WC,GP=6,2
def ranks(units):  # units (W+1,n,G) -> regional rank
    Wp,n,G=units.shape; R=np.full(units.shape,10**6,np.int32)
    for t in range(1,Wp):
        for g in range(G):
            d=units[t,:,g]; o=np.argsort(-d); rk=np.empty(n,np.int32); rk[o]=np.arange(1,n+1); R[t,:,g]=np.where(d>0,rk,10**6)
    return R
def measure(units,nat,big5,live,reg_rank=None):
    """units (W+1,n,G) weekly regional units; nat (W+1,n) national position (0 = off)."""
    R=ranks(units) if reg_rank is None else reg_rank
    natp=np.where(nat>0,nat,10**6)
    ch=natp[1:]<=100
    debut=np.where(ch.any(0),ch.argmax(0)+1,0); peak=np.where(ch.any(0),natp[1:].min(0),999)
    first=lambda m: np.where(m.any(0),m.argmax(0)+1,np.nan)
    df=pd.DataFrame(dict(debut=debut,peak=peak,a=first(R[1:,:,WC]<=40),b=first(R[1:,:,GP]<=40),a10=first(R[1:,:,WC]<=10),b10=first(R[1:,:,GP]<=10),big5=big5,live=live))
    df=df[df.live&df.debut.between(7,46)]
    df["lead"]=df.debut-df[["a","b"]].min(axis=1); df["gap"]=(df.a-df.b).abs()
    out={}
    for name,g in [("i10",df[(df.peak<=10)&~df.big5]),("b10",df[(df.peak<=10)&df.big5]),("p41",df[df.peak.between(41,100)])]:
        both=g.a.notna()&g.b.notna()
        out[name]=dict(n=len(g),nb=int(both.sum()),g3=int((g.gap[both]>=3).sum()),g6=int((g.gap[both]>=6).sum()),
                       first=int(((g.a10<g.b)|(g.b10<g.a))[both].sum()),lead=int((g.lead>=2).sum()))
    tot=units[1:].sum((0,2)); t10=(natp[1:]<=10)
    out["head"]=dict(b5u=tot[big5].sum()/tot.sum()*100,b5t10=t10[:,big5].sum()/max(1,t10.sum())*100,indie=tot[~big5&live].sum()/1e6)
    return out
def pool(outs):
    s={}
    for k in ["i10","b10","p41"]:
        a={f:sum(o[k][f] for o in outs) for f in outs[0][k]}
        nb=max(1,a["nb"]); s[k]=dict(n=a["n"],g3=100*a["g3"]/nb,g6=100*a["g6"]/nb,first=100*a["first"]/nb,lead=100*a["lead"]/max(1,a["n"]))
    s["head"]={f:np.mean([o["head"][f] for o in outs]) for f in outs[0]["head"]}
    return s
def fmt(s):
    i,b,p,h=s["i10"],s["b10"],s["p41"],s["head"]
    return (f"indie10 n{i['n']:3d} g3 {i['g3']:4.1f} g6 {i['g6']:4.1f} first {i['first']:4.1f} lead {i['lead']:4.1f} | "
            f"B5-10 g6 {b['g6']:4.1f} | p41 g6 {p['g6']:4.1f} | B5 units {h['b5u']:4.1f} B5 t10 {h['b5t10']:4.1f} indie {h['indie']:5.0f}M")
NB={"eastcoast":["greatlakes","deepsouth"],"greatlakes":["eastcoast","deepsouth","greatplains"],"greatplains":["greatlakes","rockies","southwest"],
    "deepsouth":["eastcoast","greatlakes","southwest"],"southwest":["deepsouth","rockies","westcoast","greatplains"],"rockies":["greatplains","southwest","westcoast"],"westcoast":["rockies","southwest"]}
NBI=[[REG.index(x) for x in NB[g]] for g in REG]
def u(rid,g): return int.from_bytes(hashlib.blake2b(f"{rid}|{g}".encode(),digest_size=8).digest(),"little")/2**64
def run(z,p0=.3,p1=.7,kn=40,dn=2,nat="units",kh=40,dh=1,beta=0.0,kr=0,dr=1,radcap=99,cap=0.0,demote=0.0,demote_indie=0.0,clock="local",rf=0.22,rr=5,expo=0.62,src="D",W=52,A=80,breadth=0.0,bk=40,bq=1.0,bfloor=0.0,bthresh=0.0,unexp=1.0,unexp_b5=False):
    ids,D,launch,fit,big5,home,reach=z["ids"],z[src],z["launch"],z["fit"],z["big5"],z["home"],z["reach"]
    n,G=len(ids),7; live=launch>=1
    if "U01" not in z: z["U01"]=np.array([[u(r,g) for g in REG] for r in ids]); z["V"]=np.array([u(r,"demote") for r in ids])
    E=np.full((n,G),10**6); E[~live]=-10**6
    p=np.clip(p0+p1*reach,0,1)
    held=live[:,None]&(z["U01"]>=p[:,None]); held[np.arange(n),np.clip(home,0,6)]&=~(home>=0)
    E[live]=np.where(held[live],10**6,launch[live][:,None])
    conn=fit>cap
    if demote>0: E[live&big5&conn&(z["V"]<demote)]=10**6
    if demote_indie>0: E[live&~big5&conn&(z["V"]<demote_indie)]=10**6
    capf=np.exp(np.minimum(0,cap-fit)).astype(np.float32)
    units=np.zeros((W+1,n,G),np.float32); R=np.full((W+1,n,G),10**6,np.int32); NR=np.zeros((W+1,n),np.int32)
    base=np.zeros((W+1,n),np.float32)
    ar=np.arange(n)
    pw=D.sum((0,2)); pw=pw/pw.sum()  # survey panel per region ~ its share of the market
    for t in range(1,W+1):
        age=t-launch+1; act=(age>=1)&(age<A); ac=np.clip(age,0,A-1)
        for g in range(G):
            ex=E[:,g]<=t; late=ex&(E[:,g]>launch)&live
            la=np.clip(np.where(late&(clock not in ("national","ramp")),t-E[:,g]+1,age),0,A-1)
            if clock=="hybrid":  # awareness national, conversion on the local clock
                cr=np.where(z["AB"][ar,g,la]>0,D[ar,g,la]/np.maximum(z["AB"][ar,g,la],1e-9),0); full=z["AB"][ar,g,ac]*cr
            elif clock=="awlocal":  # awareness local, conversion national
                cr=np.where(z["AB"][ar,g,ac]>0,D[ar,g,ac]/np.maximum(z["AB"][ar,g,ac],1e-9),0); full=z["AB"][ar,g,la]*cr
            elif clock=="ramp":  # national demand x generic late-region ramp on weeks since exposure
                lg=np.log(rf)*np.clip(1-(t-E[:,g])/rr,0,1); full=D[ar,g,ac]*np.where(late,np.exp(lg),1)
            elif clock=="relive":  # game rule: chart exposure at the record's position at local age, release ramp + decay on local age
                def hz(age_):
                    tt=np.clip(launch+age_-1,1,W); pos=np.where(age_>=1,z["P"][tt,ar],0); pos=np.where(pos>0,pos,100)
                    if expo<0: return 4139-4357*pos/(pos+10)  # v2 first cut: raw Hesbacher (21x)
                    x=np.maximum(pos,10); return ((4139-4357*x/(x+10))/(4139-4357*100/110))**expo  # game: plateau at 10, ^0.62
                rmp=lambda a_: np.where(a_>=6,1,.28+.72*np.clip((a_-1)/5,0,1))
                dec=lambda a_: .91**np.maximum(0,a_-8)
                f=(hz(la)/hz(age))*(rmp(la)/rmp(age))*(dec(la)/dec(age))
                full=D[ar,g,ac]*np.where(late,f,1)
            else: full=D[ar,g,la]
            ug=np.where(big5&~unexp_b5,1.0,unexp) if unexp!=1.0 else 1.0  # awareness gate: an unexposed region hears less of the record
            v=np.where(ex,full,D[ar,g,ac]*capf*ug)
            units[t,:,g]=np.where(act&(la>=1),v,0)
            base[t]+=np.where(act,D[ar,g,ac],0)
            d=units[t,:,g]; o=np.argsort(-d); rk=np.empty(n,np.int32); rk[o]=np.arange(1,n+1); R[t,:,g]=np.where(d>0,rk,10**6)
        tot=units[t].sum(1)
        if nat=="chart":
            ratio=np.where(base[t]>0,tot/np.maximum(base[t],1e-9),0)
            score=(z["U"][t]*ratio+z["AIR"][t]*ratio**beta)*z["SS"][t]
        else: score=tot
        if breadth>0:  # breadth: the panel's reporters sit in regions; a record reaches those where it is a local seller
            cov=(pw[None,:]*np.minimum(1,(bk/np.maximum(R[t],1))**bq)).sum(1)
            score=score*np.maximum(bfloor,cov)**breadth
        if bthresh>0:  # threshold: too few reporters list it -> ranked below every record that clears the bar (bubbling under)
            cov=(pw[None,:]*(R[t]<=bk)).sum(1)
            score=np.where(score>0,score+np.where(cov>=bthresh,1e12,0),0)
        o=np.argsort(-score); rk=np.empty(n,np.int32); rk[o]=np.arange(1,n+1); NR[t]=np.where(score>0,rk,0)
        pend=(E>t)&conn[:,None]
        if kh>0:
            h=(NR[t]>=1)&(NR[t]<=kh); m=h[:,None]&pend&(E>t+dh); E[m]=t+dh
        if kn>0:
            for g in range(G):
                s=np.zeros(n,bool)
                for hh in NBI[g]: s|=(E[:,hh]<=t)&(R[t,:,hh]<=kn)
                m=s&pend[:,g]&(E[:,g]>t+dn); E[m,g]=t+dn
        if kr>0:
            for g in range(G):
                best=np.full(n,10**6)
                for hh in NBI[g]: best=np.minimum(best,np.where(E[:,hh]<=t,R[t,:,hh],10**6))
                cand=np.where((best<=kr)&pend[:,g]&(E[:,g]>t+dr))[0]
                cand=cand[np.argsort(best[cand])][:radcap]; E[cand,g]=t+dr
    natpos=np.where((NR>=1)&(NR<=100),NR,0)
    res=measure(units,natpos,big5,live,R)
    res["_E"],res["_R"],res["_nat"]=E,R,natpos
    res["late"]=int(((E>launch[:,None])&(E<10**6)&live[:,None]).sum())
    return res
if __name__=="__main__":
    ap=argparse.ArgumentParser(); ap.add_argument("seeds"); ap.add_argument("--tag",default="ufw085")
    for k,v in dict(p0=.3,p1=.7,kn=40,dn=2,kh=40,dh=1,beta=0.0,kr=0,dr=1,radcap=99,cap=0.0,demote=0.0,demote_indie=0.0,rf=0.22,rr=5,expo=0.62,breadth=0.0,bk=40,bq=1.0,bfloor=0.0,bthresh=0.0,unexp=1.0).items(): ap.add_argument("--"+k,type=type(v),default=v)
    ap.add_argument("--nat",default="units"); ap.add_argument("--clock",default="local"); ap.add_argument("--src",default="D"); ap.add_argument("--label",default="")
    a=vars(ap.parse_args()); seeds=[int(x) for x in a.pop("seeds").split(",")]; tag=a.pop("tag"); lab=a.pop("label")
    outs=[run(loadab(s,tag),**a) for s in seeds]
    print(f"{lab:28s}",fmt(pool(outs)),f"late {np.mean([o['late'] for o in outs]):.0f}",flush=True)
