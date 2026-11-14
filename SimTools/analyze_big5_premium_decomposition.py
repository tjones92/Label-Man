# Big-5 Top 10 premium decomposition: launch volume, per-launch premium, quality x awareness mix, label-given awareness.
# Usage: python SimTools/analyze_big5_premium_decomposition.py <run> [<run> ...]   (runs pooled; reads SimLogs/<run>-*.csv)
import pandas as pd, numpy as np, re, sys
B5={f"label_000{i}" for i in range(1,6)}
pd.set_option("display.width",280); pd.set_option("display.max_columns",50); pd.set_option("display.max_rows",300)
def load(run):
    p=f"SimLogs/{run}-"
    la=pd.read_csv(p+"single-launch-attribution.csv")
    q=pd.read_csv(p+"single-launch-quality.csv",usecols=["singleId","tier","weeksSinceLastSingle"])
    la=la.merge(q,on="singleId",how="left")
    la["g"]=np.where(la.labelId.isin(B5),"Big5",np.where(la.tier.isin(["MidTier","Major"]),"MidTier","Other"))
    fo=pd.read_csv(p+"following.csv",usecols=["kind","young","adult","detail"]); fo=fo[fo.kind=="launch"]
    d=fo.detail.str.extract(r"^(Single|Album)\|.*book=(\d+)\|(.+)$"); fo=fo.assign(fmt=d[0],book=d[1].astype(float),rid=d[2])
    fo=fo[fo.fmt=="Single"].drop_duplicates("rid").set_index("rid")
    la["book"]=la.singleId.map(fo.book).fillna(0); la["young"]=la.singleId.map(fo.young).fillna(0); la["adult"]=la.singleId.map(fo.adult).fillna(0)
    rec=pd.read_csv(p+"records.csv")
    rec=rec[rec.currentPosition>=1]
    t=rec[rec.currentPosition<=10]
    la["t10"]=la.singleId.map(t.groupby("recordId").size()).fillna(0)
    g=rec.groupby("recordId")
    la["h100"]=la.singleId.map(g.size()).fillna(0); la["peak"]=la.singleId.map(g.currentPosition.min())
    la["peakHeat"]=la.singleId.map(g.radioHeat.max()); la["peakAw"]=la.singleId.map(g.awareness.max())
    la["cls"]=np.select([la.book>=50000,la.book>=10000,la.initialAwareness>=0.2],["A:book50k+","B:book10-50k","C:known"],"D:unknown")
    la["run"]=run
    return la,rec
RUNS=sys.argv[1:]
L=pd.concat([load(r)[0] for r in RUNS]); L=L[L.year<=1961]
L["qb"]=pd.cut(L.quality,[0,.68,.74,.80,1],labels=["<.68",".68-.74",".74-.80",".80+"])
L["ab"]=pd.cut(L.initialAwareness,[-1,.2,.4,.6,.8,2],labels=["<.2",".2-.4",".4-.6",".6-.8",".8+"])
L["b5"]=L.g=="Big5"
for y in (1960,1961):
    d=L[L.year==y]
    print("=======",y,"both seeds pooled")
    c=d.groupby(["ab","qb","b5"],observed=False).agg(n=("t10","size"),w=("t10","sum")).unstack("b5").fillna(0)
    c.columns=["nO","nB","wO","wB"]; c["rO"]=c.wO/c.nO.replace(0,np.nan); c["rB"]=c.wB/c.nB.replace(0,np.nan)
    c["b5ShareOfLaunches"]=(100*c.nB/(c.nB+c.nO)).round(1)
    print(c.round(2).to_string())
    B=c.wB.sum(); O=c.wO.sum()
    # Big-5 singles at field (non-Big-5) cell rates
    Bcf=(c.nB*c.rO.fillna(c.rB)).sum()
    # Big-5 with the field's cell mix, own rates: scale nB to field distribution
    mixO=c.nO/c.nO.sum(); Bmix=(mixO*c.nB.sum()*c.rB.fillna(c.rO)).sum()
    # awareness-only mix and quality-only mix
    print(f"Big-5 T10 weeks {B:.0f} of {B+O:.0f} launched-single weeks = {100*B/(B+O):.1f}%; launches {100*c.nB.sum()/(c.nB.sum()+c.nO.sum()):.1f}%")
    print(f" at field conversion in the same quality x awareness cell: {Bcf:.0f} -> {100*Bcf/(Bcf+O):.1f}%")
    print(f" with the field's quality x awareness mix, own conversion: {Bmix:.0f} -> {100*Bmix/(Bmix+O):.1f}%")
    for dim,name in (("ab","awareness"),("qb","quality")):
        other="qb" if dim=="ab" else "ab"
        # reweight Big-5 so its marginal over dim matches field, keeping its conditional over other
        nB=d[d.b5].groupby([dim,other],observed=False).size(); margB=nB.groupby(level=0).sum(); margO=d[~d.b5].groupby(dim,observed=False).size()
        w=(margO/margO.sum())/(margB/margB.sum())
        dd=d[d.b5].copy(); dd["w"]=dd[dim].map(w).astype(float)
        Bx=(dd.t10*dd.w).sum()
        print(f" Big-5 reweighted to the field's {name} mix only: {Bx:.0f} -> {100*Bx/(Bx+O):.1f}%")
L=pd.concat([load(r)[0] for r in RUNS]); L=L[L.year<=1961]; L["b5"]=L.g=="Big5"
print("per seed: Big-5 launches, launch share, T10 wk/launch Big-5 vs field, premium, share of launched-single T10 weeks")
for (run,y),d in L.groupby(["run","year"]):
    b=d[d.b5]; o=d[~d.b5]
    print(f"{run[-13:]:>14} {y} n={len(b):4d} launchShare={100*len(b)/len(d):5.1f}% rate {b.t10.mean():.3f} vs {o.t10.mean():.3f} premium {b.t10.mean()/o.t10.mean():.1f}x share {100*b.t10.sum()/d.t10.sum():.1f}% | acts releasing {b.artistId.nunique()} singles/act {len(b)/b.artistId.nunique():.2f} | MidTier n={(d.g=='MidTier').sum()} rate {d[d.g=='MidTier'].t10.mean():.3f}")
# monthly Big-5 launch counts
L["month"]=((L.week-1)//13)+1
print(L[L.b5].groupby(["run","month"]).size().unstack().to_string())
print(L[L.b5].groupby(["run","year","career"]).size().unstack().fillna(0).astype(int).to_string())
# counterfactual: Big-5 awareness with field label terms
L["qb"]=pd.cut(L.quality,[0,.68,.74,.80,1],labels=["<.68",".68-.74",".74-.80",".80+"])
bins=[-1,.2,.4,.6,.8,2]; labs=["<.2",".2-.4",".4-.6",".6-.8",".8+"]
L["ab"]=pd.cut(L.initialAwareness,bins,labels=labs)
for y in (1960,1961):
    d=L[L.year==y]; b=d[d.b5].copy(); o=d[~d.b5]
    lab=lambda x:(.1*x.labelReputation).mean()
    mk=lambda x:(x.initialAwareness-.08-.5*x.artistMomentum-.3*x.artistReputation-.1*x.labelReputation-.115*x.effectiveRecognition).mean()
    oth=d[d.g=="Other"]
    delta=(lab(b)+mk(b))-(lab(oth)+mk(oth))
    rate=b.groupby(["ab","qb"],observed=False).t10.mean()
    b["ab2"]=pd.cut((b.initialAwareness-delta).clip(lower=0),bins,labels=labs)
    cf=sum(rate.get((a,q),np.nan) if not np.isnan(rate.get((a,q),np.nan)) else 0 for a,q in zip(b.ab2,b.qb))
    print(y,f"label-given awareness gap Big-5 vs Other {delta:.3f} (label rep {lab(b)-lab(oth):.3f}, marketing {mk(b)-mk(oth):.3f}); Big-5 T10 weeks {b.t10.sum():.0f} -> {cf:.0f}; share {100*b.t10.sum()/d.t10.sum():.1f} -> {100*cf/(cf+o.t10.sum()):.1f}")
