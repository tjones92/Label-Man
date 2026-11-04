"""RegionalSyncDesign.md §9s. 1960 history: how records reach the national chart through local surveys. Los Angeles (KFWB+KRLA) and Minneapolis
(KDWB) are weekly all year, top 40. For every record on either city's survey, and for the 102 labelled Top-10 hits:
first week on each city's survey, Hot 100 debut, the city lead over the Hot 100, and the gap between the two cities.
Matching on title words as in SimTools/analyze_station_survey_overlap.py."""
import sys
from pathlib import Path
import pandas as pd
ROOT = str(Path(__file__).resolve().parents[1]).replace("\\", "/") + "/"
sys.path.insert(0, ROOT + "SimTools")
from analyze_station_survey_overlap import words, same  # noqa: E402

L = ROOT + "SimLogs/"
BIG5 = {"Columbia", "RCA Victor", "Capitol", "Decca", "Mercury"}
s = pd.read_csv(L + "historical-station-surveys/surveys_1960.csv")
s = s[s.station.isin(["KFWB", "KRLA", "KDWB"])].copy()
s["d"] = pd.to_datetime(s.date)
s["w"] = s.text.map(words)
h = pd.read_csv(L + "historical-hot100-1960-64/hot100_weekly.csv")
h["d"] = pd.to_datetime(h.chartDate)
h = h[(h.d >= "1959-10-01") & (h.d <= "1961-04-01")]
h["w"] = h.title.map(words)
top = pd.read_csv(L + "historical-1960-top10/records_1960_final.csv")

# Hot 100 records keyed by (title words, artist)
nat = h.groupby(["title", "artist"]).agg(debut=("d", "min"), peak=("position", "min"), weeks=("d", "nunique")).reset_index()
nat["w"] = nat.title.map(words)
first40 = h[h.position <= 40].groupby(["title", "artist"]).d.min().rename("t40")
first10 = h[h.position <= 10].groupby(["title", "artist"]).d.min().rename("t10")
nat = nat.join(first40, on=["title", "artist"]).join(first10, on=["title", "artist"])


def artist_key(a):
    import re
    ws = [w for w in re.findall(r"[a-z]+", a.lower()) if w not in {"the", "and", "with", "featuring", "his", "orchestra"}]
    return [w[:6] for w in ws if len(w) >= 4] or ws[:1]


def city_first(wk, city_rows, artist, debut):
    """First survey date and best rank in a city for a title (title words + an artist word, within 26 weeks)."""
    ak = artist_key(artist)
    lo, hi = debut - pd.Timedelta(weeks=26), debut + pd.Timedelta(weeks=26)
    c = city_rows[(city_rows.d >= lo) & (city_rows.d <= hi)]
    m = c[c.w.map(lambda x: same(wk, x)) & c.text.str.lower().map(lambda x: any(k in x for k in ak))]
    if m.empty: return pd.NaT, None, pd.NaT
    t10 = m[m["rank"] <= 10].d.min()
    return m.d.min(), int(m["rank"].min()), t10


la, mp = s[s.city.eq("Los Angeles")], s[s.city.eq("Minneapolis")]
rows = []
for _, r in nat.iterrows():
    if not (pd.Timestamp("1960-02-15") <= r.debut <= pd.Timestamp("1960-11-15")): continue
    laf, lar, la10 = city_first(r.w, la, r.artist, r.debut)
    mpf, mpr, mp10 = city_first(r.w, mp, r.artist, r.debut)
    lab = top[(top.title.str.lower() == r.title.lower())]
    rows.append(dict(title=r.title, artist=r.artist, debut=r.debut, peak=r.peak, t40=r.t40, t10=r.t10,
                     la=laf, laBest=lar, la10=la10, mp=mpf, mpBest=mpr, mp10=mp10,
                     label=lab.label.iloc[0] if len(lab) else None))
d = pd.DataFrame(rows)
wk = lambda a, b: (a - b).dt.days / 7
d["laLead"], d["mpLead"] = wk(d.debut, d.la), wk(d.debut, d.mp)
d["firstCity"] = d[["la", "mp"]].min(axis=1)
d["lead"] = wk(d.debut, d.firstCity)
d["gap"] = wk(d.la, d.mp).abs()
d["top10"] = d.peak.le(10)
d["cls"] = d.label.map(lambda x: None if x is None else ("Big-5" if x in BIG5 else "Indie"))
d.to_csv(L + "regprobe/regional-break-history-1960.csv", index=False)

pd.set_option("display.width", 220)


def summarize(g, name):
    both = g.la.notna() & g.mp.notna()
    one = g.la.notna() ^ g.mp.notna()
    out = dict(group=name, n=len(g), onAnySurvey=(g.firstCity.notna()).mean() * 100,
               bothCities=both.mean() * 100, oneCityOnly=one.mean() * 100,
               leadMedian=g.lead.median(), leadGE2=(g.lead >= 2).mean() * 100,
               gapMedian=g.gap[both].median(), gapGE3=(g.gap[both] >= 3).mean() * 100,
               gapGE6=(g.gap[both] >= 6).mean() * 100)
    # a city break: top 10 in one city while not yet on the other city's survey
    br = 0
    for _, r in g[both].iterrows():
        if (pd.notna(r.la10) and r.la10 < r.mp) or (pd.notna(r.mp10) and r.mp10 < r.la): br += 1
    out["cityBreak%"] = br / max(1, both.sum()) * 100
    return out


tab = [summarize(d, "all Hot 100 debuts Feb-Nov"),
       summarize(d[d.peak.le(40)], "national top 40"),
       summarize(d[d.top10], "national top 10"),
       summarize(d[d.top10 & d.cls.eq("Big-5")], "top 10, Big-5"),
       summarize(d[d.top10 & d.cls.eq("Indie")], "top 10, indie"),
       summarize(d[d.peak.between(41, 100)], "peak 41-100")]
print(pd.DataFrame(tab).round(1).to_string(index=False))
print("\nTop-10 hits with a gap >= 4 weeks or a one-city break:")
t = d[d.top10 & (d.gap.ge(4) | d.la.isna() | d.mp.isna())].sort_values("debut")
print(t[["title", "artist", "label", "cls", "debut", "peak", "la", "laBest", "mp", "mpBest", "lead", "gap"]].to_string(index=False))
