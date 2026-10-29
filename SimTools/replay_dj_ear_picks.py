"""RegionalSyncDesign.md §9x. Replay v2: release-week DJ picks that hear a noisy view of the record's commercial fit.
Each region's stations pick K of the records released that week (age <= AGE) by fit + SIGMA * z (z keyed per region and
record; demoted records read the median, 0). A pick is a city-list entry from that week, before any sales. Lead = national
debut - first city entry (pick, or the region's sales top 40), westcoast / greatplains, indie top-10 hits, as
analyze_regional_stagger.py. Picks do not move demand here (the game's pick airplay would; see §9x).
Usage: replay_dj_ear_picks.py [SEEDS]"""
import sys, hashlib; sys.path.insert(0, __import__("os").path.dirname(__file__))
import numpy as np, pandas as pd
import replay_regional_relive as R
seeds=[int(x) for x in (sys.argv[1] if len(sys.argv)>1 else "1001,1002,1003,1004,1005,1006,1007,1008").split(",")]
zs={s:R.loadab(s) for s in seeds}
runs={s:R.run(z,clock="relive",nat="chart",demote=.28,demote_indie=.073) for s,z in zs.items()}
def zkey(rid,g):
    u=int.from_bytes(hashlib.blake2b(f"dj|{rid}|{g}".encode(),digest_size=8).digest(),"little")/2**64
    return float(np.sqrt(2)*__import__("math").erf(0) if False else __import__("statistics").NormalDist().inv_cdf(min(max(u,1e-12),1-1e-12)))
Z={s:np.array([[zkey(r,g) for g in (6,2)] for r in z["ids"]]) for s,z in zs.items()}
def go(K,sigma,age=1,indie_only=False,exposed=False,theta=0.0):
    rows=[];picks=0;flop=0;hitp=0
    for s,z in zs.items():
        r=runs[s]; nat=r["_nat"]; RR=r["_R"]; L=z["launch"]; live=L>=1; n=len(L)
        natp=np.where(nat>0,nat,10**6); ch=natp[1:]<=100
        debut=np.where(ch.any(0),ch.argmax(0)+1,0); peak=np.where(ch.any(0),natp[1:].min(0),999)
        E=r["_E"]; demoted=live&(E>=10**6).all(1)
        heard=np.where(demoted,np.minimum(z["fit"],0),z["fit"])
        first=[]
        pw=z["D"].sum((0,2)); pw=pw/pw.sum()
        if theta>0:  # picks in every region: a record picked across >= theta of the panel charts on that airplay at once
            cov=np.zeros((53,n))
            for g in range(7):
                fp=np.full(n,np.nan)
                for t in range(1,53):
                    a=t-L+1; cand=np.where(live&(a>=1)&(a<=age)&np.isnan(fp)&(E[:,g]<=L))[0]
                    if not len(cand): continue
                    zz=np.array([zkey(z["ids"][c],g) for c in cand]) if g not in (6,2) else Z[s][cand,(6,2).index(g)]
                    pick=cand[np.argsort(-(heard[cand]+sigma*zz))[:K]]; fp[pick]=t
                    for tt in range(t,min(53,t+age)): cov[tt,pick]+=pw[g]
            hit=cov>=theta; pdeb=np.where(hit.any(0),hit.argmax(0),0)
            debut=np.where((pdeb>0)&((debut==0)|(pdeb<debut))&(debut>0),pdeb,debut)
        for j,g in enumerate((6,2)):
            fp=np.full(n,np.nan); t40=RR[1:,:,g]<=40; fs=np.where(t40.any(0),t40.argmax(0)+1,np.nan)
            for t in range(1,53):
                a=t-L+1; cand=np.where(live&(a>=1)&(a<=age)&np.isnan(fp)&((~z["big5"]) if indie_only else True)&((E[:,g]<=L) if exposed else True))[0]
                if not len(cand): continue
                sc=heard[cand]+sigma*Z[s][cand,j]; pick=cand[np.argsort(-sc)[:K]]; fp[pick]=t
                picks+=len(pick); flop+=(debut[pick]==0).sum(); hitp+=(peak[pick]<=10).sum()
            first.append(np.fmin(fp,fs))
        fc=np.fmin(*first)
        m=live&(debut>=7)&(debut<=46)&(peak<=10)&~z["big5"]
        mb=live&(debut>=7)&(debut<=46)&(peak<=10)&z["big5"]
        for i in np.where(m|mb)[0]: rows.append(dict(b5=z["big5"][i],lead=debut[i]-fc[i]))
    d=pd.DataFrame(rows); i=d[~d.b5]; b=d[d.b5]
    print(f"K{K:2d} sigma{sigma:4.1f} age<={age}{' indie' if indie_only else ''}{' exposed' if exposed else ''}{f' theta{theta}' if theta else ''}: indie lead>=2 {100*(i.lead>=2).mean():4.1f} (Big-5 {100*(b.lead>=2).mean():4.1f}) | picks never chart {100*flop/max(1,picks):3.0f}% top-10 {100*hitp/max(1,picks):3.0f}% | {picks/len(zs)/52/2:.1f} picks/region-wk",flush=True)
if __name__=="__main__":
    go(0,0)
    for K in [2,4,8]:
        for sg in [0.5,1.0,2.0,4.0]: go(K,sg)
    go(4,1.0,age=2); go(4,1.0,indie_only=True)
    for K,sg in [(2,0.5),(2,1.0),(4,1.0),(4,2.0)]:
        go(K,sg,exposed=True)
        for th in (.5,.7): go(K,sg,exposed=True,theta=th)
