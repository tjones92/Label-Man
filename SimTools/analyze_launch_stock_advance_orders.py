import sys, collections, pandas as pd
B5={f"label_000{i}" for i in range(1,6)}
for run in sys.argv[1:]:
    rows=[]
    for line in open(f"SimLogs/writing-calibration/{run}.log",encoding="utf-8",errors="ignore"):
        if not line.startswith("LAUNCH_STOCK_TRACE"): continue
        kv=dict(x.split("=",1) for x in line.split()[1:])
        if kv["fmt"]!="Single" or int(kv["year"])>1961: continue
        base=adv=0
        for part in kv["regions"].split("|"):
            r,f,rf,d,a=part.split(":"); d=int(d); a=int(a)
            b=0 if d<=0 else max(25,round(d*0.1)); base+=b; adv+=a-b
        rows.append(dict(rec=kv["rec"],g="Big5" if kv["label"] in B5 else kv["tier"],base=base,adv=adv))
    df=pd.DataFrame(rows)
    rec=pd.read_csv(f"SimLogs/{run}-records.csv",usecols=["recordId","currentPosition"])
    t10=rec[(rec.currentPosition>=1)&(rec.currentPosition<=10)].groupby("recordId").size()
    df["t10"]=df.rec.map(t10).fillna(0); df["hasAdv"]=df.adv>0
    print("==",run)
    print(df.groupby("g").agg(n=("rec","size"),base=("base","mean"),adv=("adv","mean"),advShare=("hasAdv","mean"),t10=("t10","mean")).round(3).to_string())
    print(df.groupby(["g","hasAdv"]).agg(n=("rec","size"),t10=("t10","mean")).round(3).unstack().to_string())
