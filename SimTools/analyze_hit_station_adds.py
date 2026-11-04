import pandas as pd, numpy as np, sys
L="SimLogs/"; tag=sys.argv[1]; BIG5={f"label_000{i}" for i in range(1,6)}
rows=[]
for sd in range(1001,1009):
    p=L+f"hs52op{sd}-SMP-HGRPO-{tag}-"
    r=pd.read_csv(p+"records.csv",usecols=["week","year","recordId","labelId","currentPosition","unitsThisWeek"]); r=r[r.year.eq(1960)]
    c=r[r.currentPosition.between(1,100)]
    nat=c.groupby("recordId").agg(debut=("week","min"),peak=("currentPosition","min"),lab=("labelId","first"))
    nat=nat[nat.debut.between(7,46)&nat.peak.le(10)]
    x=pd.read_csv(p+"station-playlists.csv",usecols=["week","year","stationId","format","rank","recordId"])
    x=x[x.year.eq(1960)&x.format.isin(["Top40","FullService"])&x.recordId.isin(nat.index)]
    n=x.groupby(["recordId","week"]).stationId.nunique().unstack(fill_value=0)
    for rid,v in nat.iterrows():
        s=n.loc[rid] if rid in n.index else pd.Series(dtype=float)
        first=s[s>0].index.min() if (s>0).any() else np.nan
        g=lambda k: int(s.get(v.debut+k,0))
        rows.append(dict(seed=sd,b5=v.lab in BIG5,lead=v.debut-first,stDm2=g(-2),stDm1=g(-1),stD=g(0),stD1=g(1),stD3=g(3)))
d=pd.DataFrame(rows)
for b,g in d.groupby("b5"):
    print(tag,"Big-5" if b else "indie","n",len(g),"lead(first station add -> debut) dist:",g.lead.value_counts().sort_index().to_dict())
    print("  stations listing at debut-2/-1/0/+1/+3 (median):",g[["stDm2","stDm1","stD","stD1","stD3"]].median().tolist(), " mean",g[["stDm2","stDm1","stD","stD1","stD3"]].mean().round(1).tolist())
