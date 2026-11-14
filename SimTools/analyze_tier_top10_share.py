import pandas as pd, sys
B5={f"label_000{i}" for i in range(1,6)}
for run in sys.argv[1:]:
    p=f"SimLogs/{run}-"
    r=pd.read_csv(p+"records.csv",usecols=["year","recordId","labelId","labelTier","currentPosition","unitsThisWeek"])
    r=r[r.year<=1961]
    t10=r[(r.currentPosition>=1)&(r.currentPosition<=10)]
    a=pd.read_csv(p+"single-launch-attribution.csv",usecols=["year","singleId","labelId","initialStock"])
    q=pd.read_csv(p+"single-launch-quality.csv",usecols=["singleId","tier"])
    a=a[a.year<=1961].merge(q,on="singleId",how="left")
    a.loc[a.labelId.isin(B5),"tier"]="Big5"
    cnt=t10.groupby("recordId").size()
    a["t10"]=a.singleId.map(cnt).fillna(0)
    print("==",run)
    for y,d in t10.groupby("year"):
        n=len(d); g=d.labelTier.where(~d.labelId.isin(B5),"Big5")
        print(f"  {y} T10 share: "+" ".join(f"{k}={100*v/n:.1f}" for k,v in g.value_counts().items()))
    s=a.groupby("tier").agg(n=("t10","size"),stock=("initialStock","mean"),t10rel=("t10","mean"))
    print(s.round(3).to_string())
