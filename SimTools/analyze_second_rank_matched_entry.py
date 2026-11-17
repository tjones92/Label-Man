import sys, numpy as np, pandas as pd
B5=[f"label_000{i}" for i in range(1,6)]; NEW=["NewSigning","Rising","Unsigned"]
pd.set_option("display.width",250)
X=[]
for run in sys.argv[1:]:
    p=f"{run}-"
    r=pd.read_csv(p+"records.csv",usecols=["recordId","currentPosition"]); c=r[r.currentPosition.between(1,100)]
    pk=c.groupby("recordId").currentPosition.min()
    a=pd.read_csv(p+"single-launch-attribution.csv",usecols=["year","singleId","labelId","career","quality","hook","production","initialStock","actChartRecordAtLaunch"])
    q=pd.read_csv(p+"single-launch-quality.csv",usecols=["singleId","tier"]); a=a.merge(q,on="singleId",how="left")
    w=pd.read_csv(p+"writing-launches.csv",usecols=["recordId","source"]).drop_duplicates("recordId")
    a=a.merge(w,left_on="singleId",right_on="recordId",how="left")
    a["g"]=np.where(a.labelId.isin(B5),"Big5",a.tier.fillna("Small").replace({"Boutique":"Small"}))
    a["h100"]=a.singleId.map(pk).notna(); a["t10"]=a.singleId.map(pk)<=10
    X.append(a[(a.year==1961)&(a.actChartRecordAtLaunch>0)])
X=pd.concat(X); X["qb"]=pd.cut(X.quality,[0,.66,.70,.74,.78,1])
print("1961 launches by acts with a prior chart record: H100 entry by quality bin"); print(X.groupby(["qb","g"],observed=True).h100.mean().unstack().round(3).to_string())
print(X.groupby(["qb","g"],observed=True).size().unstack().to_string())
b=X[X.g=="Big5"]
for ref in ("MidTier","Independent"):
    rate=X[X.g==ref].groupby("qb",observed=True).h100.mean(); d=X[X.g==ref]
    print(f"{ref:12} actual {d.h100.mean():.3f}  at Big-5 quality mix {b.qb.map(rate).astype(float).mean():.3f}  Big-5 {b.h100.mean():.3f}  q {d.quality.mean():.3f} vs {b.quality.mean():.3f}")
print("\nby source: share, mean hook, mean quality, entry");
print(X.groupby(["g","source"]).agg(n=("h100","size"),hook=("hook","mean"),q=("quality","mean"),h100=("h100","mean")).round(3).to_string())
