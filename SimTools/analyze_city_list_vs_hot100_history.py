"""History: of city-survey entries, how many sit on the Hot 100 that week; of records first listed in a city, when they reach the Hot 100."""
import sys; sys.path.insert(0,"SimTools")
import pandas as pd, numpy as np, re
from analyze_station_survey_overlap import words, same
L="SimLogs/"
s=pd.read_csv(L+"historical-station-surveys/surveys_1960.csv"); s=s[s.station.isin(["KFWB","KRLA","KDWB"])].copy()
s["d"]=pd.to_datetime(s.date); s=s[(s.d>="1960-02-01")&(s.d<="1960-11-30")&(s["rank"]<=40)]
s["w"]=s.text.map(words)
h=pd.read_csv(L+"historical-hot100-1960-64/hot100_weekly.csv"); h["d"]=pd.to_datetime(h.chartDate)
h=h[(h.d>="1959-10-01")&(h.d<="1961-04-01")]; h["w"]=h.title.map(words)
nat=h.groupby(["title","artist"]).agg(debut=("d","min")).reset_index(); nat["w"]=nat.title.map(words)
def ak(a):
    ws=[w for w in re.findall(r"[a-z]+",a.lower()) if w not in {"the","and","with","featuring","his","orchestra"}]
    return [w[:6] for w in ws if len(w)>=4] or ws[:1]
nat["ak"]=nat.artist.map(ak)
cache={}
def match(text,w):
    if text in cache: return cache[text]
    t=text.lower(); m=nat[nat.w.map(lambda x: same(w,x)) & nat.ak.map(lambda k: any(x in t for x in k))]
    cache[text]=None if m.empty else (m.title.iloc[0],m.artist.iloc[0],m.debut.min()); return cache[text]
s["m"]=[match(t,w) for t,w in zip(s.text,s.w)]
chart=set(zip(h.title,h.artist,h.d))
hd=sorted(h.d.unique())
def on100(m,d):
    if m is None: return False
    near=[x for x in hd if abs((x-d).days)<=3]
    return any((m[0],m[1],x) in chart for x in near)
s["on"]=[on100(m,d) for m,d in zip(s.m,s.d)]
for city,g in s.groupby("city"):
    print(city,"entries",len(g),"matched to any Hot100 record %.1f%%"%(g.m.notna().mean()*100),"on Hot 100 that week %.1f%%"%(g.on.mean()*100),
          "top10 entries on %.1f%%"%(g[g['rank']<=10].on.mean()*100),"rank 21-40 on %.1f%%"%(g[g['rank']>20].on.mean()*100))
# first listing per record per city vs Hot 100 debut
s["key"]=s.m.map(lambda m: None if m is None else (m[0],m[1]))
f=s[s.key.notna()].groupby(["city","key"]).agg(first=("d","min"),deb=("m",lambda x: x.iloc[0][2])).reset_index()
f["lead"]=(f.deb-f["first"]).dt.days/7
print("records first listed, lead of city over Hot 100 debut (weeks):"); print(f.groupby("city").lead.describe(percentiles=[.25,.5,.75,.9]).round(1))
print("share listed >=2 wk before Hot 100:",f.groupby("city").lead.apply(lambda x:(x>=2).mean()*100).round(1).to_dict())
