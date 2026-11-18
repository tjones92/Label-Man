"""Hit-act follow-ups, history against the sim (Big5ChartOpportunityInvestigation.md, "Hit-act follow-ups").
Usage from the repo root:  python SimTools/analyze_hit_followups.py ARM [ARM ...]   (ARM = e.g. mnsh40; seeds 1001-4004)
Also reports chart events (debuts under 4 weeks apart count once) and the climb premium of hot acts' chart events.

Both worlds are read only through the Hot 100, so misses never enter. An anchor is a record's first week in the Top 10.
For each anchor, the act's other titles that debut in the following 52 weeks are its follow-ups. Reported per anchor:
follow-up Hot 100 entries, how many reach the Top 40 and the Top 10, and Top 10 per entry. Big-5 means the anchor's label
(history: the Top 10 label tables; sim: the record's label). History anchors: 1960-63 (the window ends in 1964) and 1960
alone; sim anchors: weeks 1-52, so the window stays inside the 104-week run."""
from pathlib import Path
import re
import sys
import pandas as pd

LOG = Path(__file__).resolve().parents[1] / "SimLogs"
SEEDS = (1001, 2002, 3003, 4004)
FIRMS = {"Columbia", "RCA Victor", "Capitol", "Decca", "Mercury"}
IDS = {f"label_000{i}" for i in range(1, 6)}


def artist_key(name):
    name = re.split(r" with | and his | and her | featuring | & the ", name.lower())[0]
    return re.sub(r"[^a-z0-9]", "", name)


def title_key(title):
    return re.sub(r"[^a-z0-9]", "", title.lower())


def summarize(anchors, follow, group):
    """anchors: act, rec, t (first Top 10), big. follow: act, rec, debut, peak."""
    m = anchors.merge(follow, on="act", suffixes=("", "_f"))
    m = m[(m.rec_f != m.rec) & (m.debut > m.t) & (m.debut <= m.t + 52)]
    per = m.groupby(["big", "rec"]).agg(entries=("rec_f", "size"), t40=("peak", lambda p: (p <= 40).sum()), t10=("peak", lambda p: (p <= 10).sum()))
    per = anchors.set_index(["big", "rec"])[[]].join(per).fillna(0)
    out = per.groupby("big").agg(anchors=("entries", "size"), entries=("entries", "mean"), t40=("t40", "mean"), t10=("t10", "mean"))
    out["t10/entry %"] = 100 * out.t10 / out.entries
    out["t40/entry %"] = 100 * out.t40 / out.entries
    # Chart events: an act's debuts under 4 weeks apart count once (history's Hot 100 lists both sides of a two-sided
    # hit; the sim charts one side). An event starts at a debut 4+ weeks after the act's previous debut; the anchor's own
    # event is excluded.
    f = follow.sort_values(["act", "debut"]).copy()
    f["start"] = f.groupby("act").debut.diff().fillna(99) >= 4
    f["event"] = f.groupby("act").start.cumsum()
    ev = f[f.start][["act", "event", "debut"]].merge(f.groupby(["act", "event"]).peak.min().reset_index(), on=["act", "event"])
    own = anchors.merge(f[["rec", "event"]], on="rec")[["big", "rec", "act", "t", "event"]]
    e = own.merge(ev, on="act", suffixes=("", "_f"))
    e = e[(e.event_f != e.event) & (e.debut > e.t) & (e.debut <= e.t + 52)]
    g = e.groupby(["big", "rec"])
    per_e = anchors.set_index(["big", "rec"])[[]].join(pd.DataFrame({"events": g.size(), "ev40": g.peak.apply(lambda p: (p <= 40).sum()),
                                                                     "ev10": g.peak.apply(lambda p: (p <= 10).sum())})).fillna(0)
    out[["events", "event T40", "event T10"]] = per_e.groupby("big")[["events", "ev40", "ev10"]].mean().values
    out["T40/event %"] = 100 * out["event T40"] / out.events
    out.index = out.index.map({True: "Big-5", False: "other"})
    out.insert(0, "group", group)
    return out


def history(years):
    w = pd.read_csv(LOG / "historical-hot100-1960-64/hot100_weekly.csv", parse_dates=["chartDate"])
    w["act"] = w.artist.map(artist_key)
    w["rec"] = w.act + "|" + w.title.map(title_key)
    t0 = w.chartDate.min()
    w["wk"] = ((w.chartDate - t0).dt.days // 7).astype(int)
    rec = w.groupby(["act", "rec"]).agg(debut=("wk", "min"), peak=("position", "min")).reset_index()
    first10 = w[w.position <= 10].groupby("rec").wk.min()
    labels = []
    for year in range(1960, 1965):
        d = "historical-1960-top10" if year == 1960 else f"historical-top10-{year}"
        lab = pd.read_csv(LOG / d / f"records_{year}_final.csv")
        lab["rec"] = lab.artist.map(artist_key) + "|" + lab.title.map(title_key)
        labels.append(lab[["rec", "label"]])
    labels = pd.concat(labels).drop_duplicates("rec").set_index("rec").label
    a = rec[rec.rec.isin(first10.index)].copy()
    a["t"] = a.rec.map(first10)
    a["year"] = a.t.map(lambda k: (t0 + pd.Timedelta(weeks=int(k))).year)
    a = a[a.year.isin(years)]
    a["big"] = a.rec.map(labels).isin(FIRMS)
    missing = a.rec.map(labels).isna().sum()
    return a[["act", "rec", "t", "big"]], rec[["act", "rec", "debut", "peak"]], missing


def sim(arm):
    out = []
    for s in SEEDS:
        p = LOG / f"{arm}-{s}-sc-records.csv"
        p = p if p.exists() else Path(str(p) + ".gz")
        r = pd.read_csv(p, usecols=["week", "recordId", "artistId", "labelId", "currentPosition"])
        r = r[r.currentPosition.between(1, 100)].copy()
        r["act"] = str(s) + "|" + r.artistId
        r["rec"] = str(s) + "|" + r.recordId
        rec = r.groupby(["act", "rec"]).agg(debut=("week", "min"), peak=("currentPosition", "min"), label=("labelId", "first")).reset_index()
        first10 = r[r.currentPosition <= 10].groupby("rec").week.min()
        a = rec[rec.rec.isin(first10.index)].copy()
        a["t"] = a.rec.map(first10)
        a = a[a.t <= 52]
        a["big"] = a.label.isin(IDS)
        out.append((a[["act", "rec", "t", "big"]], rec[["act", "rec", "debut", "peak"]]))
    return pd.concat(x[0] for x in out), pd.concat(x[1] for x in out)


pd.set_option("display.width", 200)
rows = []
for years, name in (((1960, 1961, 1962, 1963), "history 1960-63"), ((1960,), "history 1960")):
    a, f, missing = history(years)
    rows.append(summarize(a, f, name))
    if missing: print(f"{name}: {missing} anchors without a label")
for arm in sys.argv[1:]:
    a, f = sim(arm)
    rows.append(summarize(a, f, arm + " (4 seeds, 1960)"))
print(pd.concat(rows).round(2).to_string())


# Climb premium: of all chart events (an act's debuts under 4 weeks apart count once), the share reaching the Top 40 and
# Top 10, for acts with a Top 10 in the 52 weeks before the event's debut ("hot") against everyone else. History: debuts
# 1961-63 (weeks 52-207, so every event has a full lookback); sim: debuts in weeks 40-92 (lookback from week 1, outcome
# window to week 104), opening catalog excluded.
def climb(rec, lo, hi, label):
    f = rec.sort_values(["act", "debut"]).copy()
    f["start"] = f.groupby("act").debut.diff().fillna(99) >= 4
    f["event"] = f.groupby("act").start.cumsum()
    e = f.groupby(["act", "event"]).agg(debut=("debut", "min"), peak=("peak", "min"), t10=("t10", "min")).reset_index()
    h = e.loc[e.t10 < 9999, ["act", "t10"]]
    m = e.merge(h, on="act", how="left", suffixes=("", "_h"))
    m["hot"] = (m.t10_h < m.debut) & (m.t10_h >= m.debut - 52)
    g = m.groupby(["act", "event"]).agg(debut=("debut", "first"), peak=("peak", "first"), hot=("hot", "any")).reset_index()
    g = g[(g.debut >= lo) & (g.debut < hi)]
    out = g.groupby("hot").agg(events=("peak", "size"), t40=("peak", lambda p: 100 * (p <= 40).mean()),
                               t10=("peak", lambda p: 100 * (p <= 10).mean()))
    return pd.DataFrame({"group": label, "hot events": out.events.get(True, 0), "hot T40 %": out.t40.get(True), "other T40 %": out.t40.get(False),
                         "hot T10 %": out.t10.get(True), "other T10 %": out.t10.get(False)}, index=[0])


w = pd.read_csv(LOG / "historical-hot100-1960-64/hot100_weekly.csv", parse_dates=["chartDate"])
w["act"] = w.artist.map(artist_key)
w["rec"] = w.act + "|" + w.title.map(title_key)
w["wk"] = ((w.chartDate - w.chartDate.min()).dt.days // 7).astype(int)
hr = w.groupby(["act", "rec"]).agg(debut=("wk", "min"), peak=("position", "min")).reset_index()
hr["t10"] = hr.rec.map(w[w.position <= 10].groupby("rec").wk.min()).fillna(9999)
crows = [climb(hr, 52, 208, "history 1961-63")]
for arm in sys.argv[1:]:
    parts = []
    for s in SEEDS:
        p = LOG / f"{arm}-{s}-sc-records.csv"
        r = pd.read_csv(p if p.exists() else Path(str(p) + ".gz"), usecols=["week", "recordId", "artistId", "currentPosition"])
        r = r[r.currentPosition.between(1, 100)]
        rr = r.groupby("recordId").agg(act=("artistId", "first"), debut=("week", "min"), peak=("currentPosition", "min")).reset_index()
        rr["t10"] = rr.recordId.map(r[r.currentPosition <= 10].groupby("recordId").week.min()).fillna(9999)
        rr["act"] = str(s) + "|" + rr.act
        parts.append(rr[rr.debut > 1])
    crows.append(climb(pd.concat(parts), 40, 93, arm + " (4 seeds)"))
print()
print(pd.concat(crows).round(1).to_string(index=False))
