"""Chart-bottom turnover screen (Big5ChartOpportunityInvestigation.md, "Where the chart bottom's slots go").
Usage from the repo root:  python SimTools/score_chart_faller_depth.py ARM [ARM ...]   (ARM = e.g. mnsh40; seeds 1001-4004)

History is the Hot 100 of 1961-63 (debuts in chart weeks 52-207); the sim is 1961 (weeks 53-104), four seeds, per seed-year.
Chart structure: debuts and chart events (an act's debuts under 4 weeks apart count once), mean chart life, weeks after the
peak and median last position of Top 40 peakers, records by peak band, the share of slot-weeks held by records past their
chart peak, and the exit hazard of a record past its peak at 41-60. Climb: of chart events debuting in the window (sim weeks
40-92), the share reaching the Top 40 for acts with a Top 10 in the 52 weeks before ("proven") and for everyone else, with
the events a year in each group and the non-proven events debuting at 81-100. Guards: Big-5 Top 10 slot share 1960 / 1961
(history 34.2 / 20.6), the 1961 units share of chart positions 41-100 (bounds 21.4 Hesbacher / 26.1 Carroll / 32.0
Haampland) and #10 / #100 units, total single units, and opening labels closed by week 104 (dormancy / bankruptcy)."""
from pathlib import Path
import re
import sys
import pandas as pd

LOG = Path(__file__).resolve().parents[1] / "SimLogs"
SEEDS = (1001, 2002, 3003, 4004)
B5 = {f"label_000{i}" for i in range(1, 6)}
pd.set_option("display.width", 220)


def gz(p):
    p = Path(p)
    return p if p.exists() else Path(str(p) + ".gz")


def key(a, t):
    a = re.split(r" with | and his | and her | featuring | & the ", a.lower())[0]
    return re.sub(r"[^a-z0-9]", "", a), re.sub(r"[^a-z0-9]", "", a) + "|" + re.sub(r"[^a-z0-9]", "", t.lower())


def chart(c, lo, hi, ny, last_week, clo, chi, cyears):
    """c: wk, rec, act, pos. Chart structure for debuts in [lo, hi); climb for event debuts in [clo, chi)."""
    c = c.sort_values(["rec", "wk"]).reset_index(drop=True)
    g = c.groupby("rec")
    r = g.agg(act=("act", "first"), debut=("wk", "min"), last=("wk", "max"), peak=("pos", "min"), weeks=("pos", "size"), lastpos=("pos", "last"))
    r["peakwk"] = c.loc[g.pos.idxmin()].set_index("rec").wk
    c = c.join(r[["peakwk"]], on="rec")
    c["off"] = g.wk.shift(-1) != c.wk + 1
    o = {}
    d = r[(r.debut >= lo) & (r.debut < hi)]
    done = d[d["last"] < last_week]
    t40 = done[done.peak <= 40]
    rr = r.sort_values(["act", "debut"]).copy()
    rr["start"] = rr.groupby("act").debut.diff().fillna(99) >= 4
    o["debuts/yr"] = len(d) / ny
    o["events/yr"] = ((rr.debut >= lo) & (rr.debut < hi) & rr.start).sum() / ny
    o["mean life, wk"] = done.weeks.mean()
    o["T40 peakers: wk after peak"] = (t40["last"] - t40.peakwk).mean()
    o["T40 peakers: median last pos"] = t40.lastpos.median()
    o["Top 40 records/yr"] = (d.peak <= 40).sum() / ny
    o["peak 41-80 records/yr"] = d.peak.between(41, 80).sum() / ny
    o["peak 81-100 records/yr"] = (d.peak > 80).sum() / ny
    win = c[(c.wk >= lo) & (c.wk < hi)]
    o["slot-weeks past peak %"] = 100 * (win.wk > win.peakwk).mean()
    fall = win[(win.wk > win.peakwk) & win.pos.between(41, 60) & (win.wk < hi - 1)]
    o["exit % past peak at 41-60"] = 100 * fall.off.mean()
    rr["t10"] = rr.index.map(c[c.pos <= 10].groupby("rec").wk.min()).fillna(9999)
    rr["event"] = rr.groupby("act").start.cumsum()
    e = rr.reset_index().groupby(["act", "event"]).agg(debut=("debut", "min"), peak=("peak", "min")).reset_index()
    first = c.sort_values("wk").groupby("rec").pos.first()
    rr["debutpos"] = first.reindex(rr.index)
    e = e.merge(rr.reset_index().groupby(["act", "event"]).debutpos.first().reset_index(), on=["act", "event"])
    m = e.merge(rr[rr.t10 < 9999][["act", "t10"]], on="act", how="left")
    m["hot"] = (m.t10 < m.debut) & (m.t10 >= m.debut - 52)
    m = m.groupby(["act", "event"]).agg(debut=("debut", "first"), peak=("peak", "first"), debutpos=("debutpos", "first"), hot=("hot", "any")).reset_index()
    m = m[(m.debut >= clo) & (m.debut < chi)]
    o["proven events/yr"] = m.hot.sum() / cyears
    o["other events/yr"] = (~m.hot).sum() / cyears
    o["other events debuting 81-100/yr"] = ((~m.hot) & (m.debutpos > 80)).sum() / cyears
    o["T40 % proven"] = 100 * (m[m.hot].peak <= 40).mean()
    o["T40 % other"] = 100 * (m[~m.hot].peak <= 40).mean()
    o["T10 % proven"] = 100 * (m[m.hot].peak <= 10).mean()
    o["T10 % other"] = 100 * (m[~m.hot].peak <= 10).mean()
    return o


def history():
    w = pd.read_csv(LOG / "historical-hot100-1960-64/hot100_weekly.csv", parse_dates=["chartDate"])
    k = [key(a, t) for a, t in zip(w.artist, w.title)]
    w["act"] = [x[0] for x in k]
    w["rec"] = [x[1] for x in k]
    w["wk"] = ((w.chartDate - w.chartDate.min()).dt.days // 7).astype(int)
    o = chart(w.rename(columns={"position": "pos"})[["wk", "rec", "act", "pos"]], 52, 208, 3, w.wk.max(), 52, 208, 3)
    o.update({"Big-5 T10 share 1960 %": 34.2, "Big-5 T10 share 1961 %": 20.6})
    return o


def arm(name):
    parts, guards = [], []
    for s in SEEDS:
        run = f"{name}-{s}-sc"
        r = pd.read_csv(gz(LOG / f"{run}-records.csv"), usecols=["week", "year", "recordId", "artistId", "labelId", "currentPosition", "unitsThisWeek"])
        c = r[r.currentPosition.between(1, 100)].rename(columns={"week": "wk", "currentPosition": "pos"})
        c = c.assign(rec=str(s) + "|" + c.recordId, act=str(s) + "|" + c.artistId)
        parts.append(c)
        gd = {}
        t10 = c[c.pos <= 10]
        for y in (1960, 1961):
            ty = t10[t10.year == y]
            gd[f"Big-5 T10 share {y} %"] = 100 * ty.labelId.isin(B5).mean()
        c61 = c[c.year == 1961]
        gd["units share #41-100, 1961 %"] = 100 * c61[c61.pos > 40].unitsThisWeek.sum() / c61.unitsThisWeek.sum()
        wk = c61.pivot_table(index="wk", columns="pos", values="unitsThisWeek")
        gd["#10 / #100 units, 1961"] = (wk[10] / wk[100]).median()
        ls = pd.read_csv(gz(LOG / f"{run}-label-sales-weekly.csv"))
        ls = ls[ls.format.str.strip('"') == "Single"]
        gd["single units, M"] = ls.units.sum() / 1e6
        gd["charted share of single units %"] = 100 * ls.chartedUnits.sum() / ls.units.sum()
        gd["Big-5 share of single units %"] = 100 * ls[ls.labelId.str.strip('"').isin(B5)].units.sum() / ls.units.sum()
        f = pd.read_csv(gz(LOG / f"{run}-label-finance.csv"), usecols=lambda x: x in ("week", "labelId", "status", "exitReason"), low_memory=False)
        start = set(f[f.week == f.week.min()].labelId)
        last = f.sort_values("week").groupby("labelId").tail(1)
        last = last[last.labelId.isin(start) & last.status.isin(["Bankrupt", "Defunct"])]
        gd["closed: dormancy"] = (last.exitReason == "Dormancy").sum()
        gd["closed: bankruptcy"] = (last.exitReason == "Bankruptcy").sum()
        gd["closed: total"] = len(last)
        guards.append(gd)
    c = pd.concat(parts, ignore_index=True)
    o = chart(c[["wk", "rec", "act", "pos"]], 53, 105, 4, 104, 40, 93, 4 * 53 / 52)
    o.update(pd.DataFrame(guards).mean().to_dict())
    o["closed by seed"] = " / ".join(str(int(g["closed: total"])) for g in guards)
    o["Big-5 T10 share 1961 by seed"] = " / ".join(f"{g['Big-5 T10 share 1961 %']:.1f}" for g in guards)
    return o


cols = {"history 1961-63": history()}
for a in sys.argv[1:]:
    cols[a] = arm(a)
out = pd.DataFrame(cols)
print(out.apply(lambda col: col.map(lambda v: f"{v:.1f}" if isinstance(v, float) else v)).to_string())
