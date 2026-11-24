import csv, sys, collections, statistics as st
B5 = {f"label_000{i}" for i in range(1,6)}
def load(prefix):
    tier = {}
    for r in csv.DictReader(open(prefix+"single-launch-quality.csv")):
        tier[r["singleId"]] = r["tier"]
    top10 = collections.Counter(); top40 = collections.Counter(); charted=set()
    for r in csv.DictReader(open(prefix+"records.csv")):
        p = int(r["currentPosition"] or 0)
        if p<=0: continue
        charted.add(r["recordId"])
        if p<=10: top10[r["recordId"]]+=1
        if p<=40: top40[r["recordId"]]+=1
    rows=[]
    for r in csv.DictReader(open(prefix+"single-launch-attribution.csv")):
        y=int(r["year"])
        if y>1962: continue
        sid=r["singleId"]; t=tier.get(sid,"?")
        g = "Big5" if r["labelId"] in B5 else t
        rows.append(dict(g=g,y=y,prod=float(r["production"]),hook=float(r["hook"]),q=float(r["quality"]),
            lp=float(r["labelProduction"]),ci=float(r["launchCampaignImpact"]),stock=float(r["initialStock"]),
            aw=float(r["initialAwareness"]),career=r["career"],t10=top10[sid],t40=top40[sid],ch=sid in charted,lab=r["labelId"]))
    return rows
rows=[]
for p in sys.argv[1:]: rows+=load(p)
def summ(rs,label):
    if not rs: return
    n=len(rs)
    print(f"{label:28s} n={n:5d} prod={st.mean(r['prod'] for r in rs):.3f} hook={st.mean(r['hook'] for r in rs):.3f} q={st.mean(r['q'] for r in rs):.3f} ci={st.mean(r['ci'] for r in rs):.2f} stock={st.mean(r['stock'] for r in rs):7.0f} aw={st.mean(r['aw'] for r in rs):.2f} chart%={100*sum(r['ch'] for r in rs)/n:5.1f} t40/rel={sum(r['t40'] for r in rs)/n:.2f} t10/rel={sum(r['t10'] for r in rs)/n:.3f}")
G=["Big5","Major","MidTier","Independent","Boutique","Small"]
print("== by tier"); [summ([r for r in rows if r['g']==g],g) for g in G]
print("== by tier x production band")
for g in G:
    for lo,hi in [(0,.6),(.6,.68),(.68,.74),(.74,.8),(.8,1.01)]:
        summ([r for r in rows if r['g']==g and lo<=r['prod']<hi],f"{g} prod[{lo:.2f},{hi:.2f})")
print("== by tier x quality band")
for g in G:
    for lo,hi in [(0,.62),(.62,.68),(.68,.74),(.74,1.01)]:
        summ([r for r in rows if r['g']==g and lo<=r['q']<hi],f"{g} q[{lo:.2f},{hi:.2f})")
print("== by tier x stock band")
for g in G:
    for lo,hi in [(0,2000),(2000,5000),(5000,10000),(10000,1e9)]:
        summ([r for r in rows if r['g']==g and lo<=r['stock']<hi],f"{g} stock[{lo},{hi})")
