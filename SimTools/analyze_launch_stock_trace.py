import sys, re, collections
B5={f"label_000{i}" for i in range(1,6)}
def parse(path):
    recs=[]
    for line in open(path,encoding="utf-8",errors="ignore"):
        if not line.startswith("LAUNCH_STOCK_TRACE"): continue
        kv=dict(x.split("=",1) for x in line.split()[1:])
        if kv["fmt"]!="Single" or int(kv["year"])>1961: continue
        g="Big5" if kv["label"] in B5 else kv["tier"]
        regs=[]
        for part in kv["regions"].split("|"):
            r,flags,rf,d,a=part.split(":")
            regs.append(dict(c=flags[0]=="1",s=flags[1]=="1",h=flags[2]=="1",rf=float(rf),d=int(d),a=int(a)))
        recs.append(dict(g=g,dist=float(kv["dist"]),pqm=float(kv["pqm"]),regs=regs))
    return recs
for path in sys.argv[1:]:
    recs=parse(path); print("==",path.split("/")[-1], len(recs))
    by=collections.defaultdict(list)
    for r in recs: by[r["g"]].append(r)
    print(f"{'tier':12s} {'n':>5s} {'cov':>4s} {'drawn':>7s} {'alloc':>7s} {'covD':>7s} {'uncD':>6s} {'uncA':>6s} {'rfCov':>5s} {'rfUnc':>5s} {'dist':>5s}")
    for g in ["Big5","Major","MidTier","Independent","Boutique","Small"]:
        rs=by.get(g,[])
        if not rs: continue
        n=len(rs); m=lambda f: sum(f(r) for r in rs)/n
        cov=m(lambda r: sum(x["c"] for x in r["regs"]))
        unc=[x for r in rs for x in r["regs"] if not x["c"]]; cv=[x for r in rs for x in r["regs"] if x["c"]]
        print(f"{g:12s} {n:5d} {cov:4.1f} {m(lambda r: sum(x['d'] for x in r['regs'])):7.0f} {m(lambda r: sum(x['a'] for x in r['regs'])):7.0f} "
              f"{m(lambda r: sum(x['d'] for x in r['regs'] if x['c'])):7.0f} {m(lambda r: sum(x['d'] for x in r['regs'] if not x['c'])):6.0f} {m(lambda r: sum(x['a'] for x in r['regs'] if not x['c'])):6.0f} "
              f"{(sum(x['rf'] for x in cv)/len(cv) if cv else 0):5.2f} {(sum(x['rf'] for x in unc)/len(unc) if unc else 0):5.2f} {m(lambda r:r['dist']):5.2f}")
