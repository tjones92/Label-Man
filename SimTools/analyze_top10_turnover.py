"""Top 10 turnover on the reference bundle against history 1960-63 (Top10TurnoverFindings.md).

usage: analyze_top10_turnover.py [--owner] [name=fmt ...]  ({s} = seed; default arms: the fit208 ladder, seeds 2002 + 1001)
History's Big-5 is the producing label (the author's targets); --owner uses owner grouping (HISTORY_BIG5) instead.

Both frames are cut to chart weeks 0-208 (history from 1959-12-28) and years are assigned the sim's way
(weeks 0-52 = 1960, then 52 a year), so the windows line up. A "Top 10 record" reached #10 or better; "censored"
records sit in the Top 10 in the first or last week and are left out of stay statistics.
Tiers: sim Big-5 = label_0001-0005, otherwise labelTier; history Big-5 = owner in HISTORY_BIG5 (records_<y>_grouped).

Sections:
  1. count / weeks / one-week share per year, and the share split  (Big-5 T10 share = Big-5 records x weeks / slots)
  2. peak-band shape (#1-3, #4-6, #7-10): share of records, Top 10 weeks, weeks before / after the peak
  3. week-to-week moves at #4-10 (post-peak and climbing), exit chance next week
  4. one-week Top 10s by tier, and whether short runs concentrate in the Big-5
  5. hit acts per year by tier (first Top 10 of the act in the window, after the opening week)
"""
import re
import sys
from pathlib import Path

import numpy as np
import pandas as pd

sys.path.insert(0, str(Path(__file__).resolve().parent))
from analyze_studio_parent import HISTORY_BIG5

ROOT = Path(__file__).resolve().parent.parent / "SimLogs"
B5 = {f"label_000{i}" for i in range(1, 6)}
LAST = 208
SEEDS = (2002, 1001)
HISTORY_LABEL_BIG5 = {"Columbia", "RCA Victor", "Capitol", "Decca", "Mercury"}
OWNER = "--owner" in sys.argv
ARMS = {"base": "fit208-v1-{s}-base", "lw0up1": "fit208-v1-{s}-lw0up1", "ao2": "fit208-v3-{s}-lw0up1_ao2",
        "lh1": "fit208-v4-{s}-lw0up1_ao2_lh1", "he095": "fit208-v5-{s}-lw0up1_ao2_lh1_he095"}
pd.set_option("display.width", 250)
pd.set_option("display.max_columns", 40)


def akey(text):
    return re.sub(r"[^a-z0-9]", "", str(text).lower())


def year_of(week):
    return np.where(week <= 52, 1960, 1960 + (week - 1) // 52)


def history():
    h = pd.read_csv(ROOT / "historical-hot100-1960-64" / "hot100_weekly.csv")
    dates = sorted(h.chartDate.unique())
    h["week"] = h.chartDate.map({d: n for n, d in enumerate(dates)})
    h = h[h.week <= LAST].copy()
    h["rid"] = h.title.map(akey) + "|" + h.artist.map(akey)
    h["act"] = h.artist.map(akey)
    owners = pd.concat([pd.read_csv(ROOT / f"historical-top10-{y}" / f"records_{y}_grouped.csv") for y in range(1960, 1965)])
    owners["rid"] = owners.title.map(akey) + "|" + owners.artist.map(akey)
    owners = owners.sort_values("first").drop_duplicates("rid").set_index("rid")
    # Producing label, not owner: the author's targets (1961-63 Big-5 Top 10 20.6 / 21.2 / 26.0) are this measure, and the
    # sim's label_0001-0005 are the five flagship labels. Owner grouping (HISTORY_BIG5) reads 22.1 / 26.0 / 38.8.
    big5 = HISTORY_BIG5 if OWNER else HISTORY_LABEL_BIG5
    h["tier"] = np.where(h.rid.map(owners.owned if OWNER else owners.label).isin(big5), "Big5", "other")
    owners = owners.owned
    t10 = set(h.rid[h.position <= 10])
    matched = len(t10 & set(owners.index)) / len(t10)
    return h.rename(columns={"position": "pos"})[["week", "rid", "act", "pos", "tier"]], matched


def sim(run):
    r = pd.read_csv(ROOT / f"{run}-records.csv",
                    usecols=["week", "recordId", "artistId", "labelId", "labelTier", "currentPosition", "unitsThisWeek",
                             "unsampledChartPoints", "chartPoints", "radioHeat", "weeksSinceRelease", "quality"])
    r = r[r.currentPosition.between(1, 100) & (r.week <= LAST)].copy()
    r["tier"] = np.where(r.labelId.isin(B5), "Big5", r.labelTier)
    return r.rename(columns={"recordId": "rid", "artistId": "act", "currentPosition": "pos"})


def shapes(c):
    """One row per Top 10 record."""
    c = c.sort_values(["rid", "week"])
    t = c[c.pos <= 10]
    rows = []
    hot = {rid: g for rid, g in c.groupby("rid", sort=False)}
    for rid, g in t.groupby("rid", sort=False):
        wk, pos = g.week.values, g.pos.values
        best = pos.min()
        pk = wk[pos == best][0]
        hh = hot[rid]
        rows.append(dict(rid=rid, act=g.act.iloc[0], tier=g.tier.iloc[0], t10Weeks=len(wk), best=int(best),
                         spells=1 + int((np.diff(wk) > 1).sum()), firstT10=wk[0],
                         toPeakT10=pk - wk[0], afterPeakT10=wk[-1] - pk,
                         toPeakH100=pk - hh.week.values[0], afterPeakH100=hh.week.values[-1] - pk,
                         h100Weeks=len(hh), censored=bool(wk[0] == 0 or wk[-1] == LAST or hh.week.values[0] == 0)))
    s = pd.DataFrame(rows)
    s["year"] = year_of(s.firstT10)
    s["band"] = pd.cut(s.best, [0, 3, 6, 10], labels=["1-3", "4-6", "7-10"])
    s["oneWeek"] = s.t10Weeks == 1
    return s


def moves(c):
    """Next-week position change for records at #4-10, split post-peak (already at or past their Hot 100 best) / rising."""
    c = c.sort_values(["rid", "week"]).copy()
    c["bestSoFar"] = c.groupby("rid").pos.cummin()
    nxt = c.groupby("rid").shift(-1)
    c["next"] = np.where(nxt.week == c.week + 1, nxt.pos, 101)
    c = c[c.pos.between(4, 10) & (c.week < LAST)]
    c["phase"] = np.where(c.pos > c.bestSoFar, "post-peak", np.where(c.next < c.pos, "rising", "at-peak"))
    c["delta"] = c.next - c.pos
    c["exit10"] = c.next > 10
    return c


def summarize(name, c, per_seed=1):
    s = shapes(c)
    u = s[~s.censored]
    yr = s.groupby("year").agg(records=("rid", "size"), b5=("tier", lambda x: (x == "Big5").sum()))
    yr["records"] = yr.records / per_seed
    yr["b5"] = yr.b5 / per_seed
    out = dict(arm=name, t10PerYr=round(yr.records[yr.index <= 1963].mean(), 1),
               b5T10PerYr=round(yr.b5[yr.index <= 1963].mean(), 1),
               wk=round(u.t10Weeks.mean(), 2), wkB5=round(u[u.tier == "Big5"].t10Weeks.mean(), 2),
               wkField=round(u[u.tier != "Big5"].t10Weeks.mean(), 2),
               oneWkPct=round(100 * u.oneWeek.mean(), 1), returnsPct=round(100 * (u.spells > 1).mean(), 1))
    slots = c[c.pos <= 10]
    for y in (1961, 1962, 1963):
        sl = slots[year_of(slots.week) == y]
        out[f"b5Share{y}"] = round(100 * (sl.tier == "Big5").mean(), 1)
    return out, s, u


def band_table(u):
    g = u.groupby("band", observed=False)
    return pd.DataFrame(dict(sharePct=100 * g.size() / len(u), t10Wk=g.t10Weeks.mean(), toPeakT10=g.toPeakT10.mean(),
                             afterPeakT10=g.afterPeakT10.mean(), toPeakH100=g.toPeakH100.mean(),
                             afterPeakH100=g.afterPeakH100.mean(), h100Wk=g.h100Weeks.mean(),
                             oneWkPct=100 * g.oneWeek.mean())).round(2)


def move_table(m):
    g = m.groupby("phase")
    t = pd.DataFrame(dict(n=g.size(), meanDelta=g.delta.mean(), medDelta=g.delta.median(), exitPct=100 * g.exit10.mean()))
    pp = m[m.phase == "post-peak"]
    by = pp.groupby("pos").agg(meanDelta=("delta", "mean"), exitPct=("exit10", "mean"))
    by["exitPct"] *= 100
    return t.round(2), by.round(2).T


def short_concentration(u, tiers):
    rows = {}
    for tier in tiers:
        x = u[u.tier == tier] if tier != "field" else u[u.tier != "Big5"]
        rows[tier] = dict(records=len(x), oneWkPct=100 * x.oneWeek.mean(), le2WkPct=100 * (x.t10Weeks <= 2).mean(),
                          wk=x.t10Weeks.mean(), peak1to3Pct=100 * (x.band == "1-3").mean(),
                          wkIfPeak7to10=x[x.band == "7-10"].t10Weeks.mean(), wkIfPeak1to3=x[x.band == "1-3"].t10Weeks.mean())
    t = pd.DataFrame(rows).T.round(2)
    b5 = u.tier == "Big5"
    t.loc["Big5 share of one-week T10s %", "records"] = round(100 * b5[u.oneWeek].mean(), 1)
    t.loc["Big5 share of all T10 records %", "records"] = round(100 * b5.mean(), 1)
    return t


def hit_acts(s, per_seed=1):
    """Acts whose first Top 10 in the window comes in year Y (acts already in the Top 10 in week 0 excluded)."""
    first = s.sort_values("firstT10").drop_duplicates("act")
    first = first[first.firstT10 > 0]
    t = first.groupby(["year", "tier"]).size().unstack(fill_value=0) / per_seed
    return t


def run_arm(name, fmt):
    frames = []
    for seed in SEEDS:
        c = sim(fmt.format(s=seed))
        c["rid"] = f"{seed}:" + c.rid
        c["act"] = f"{seed}:" + c.act.astype(str)
        frames.append(c)
    return pd.concat(frames)


def main():
    arms = dict(a.split("=", 1) for a in sys.argv[1:] if "=" in a) or ARMS
    h, matched = history()
    print(f"history Top 10 records matched to an owner: {100 * matched:.1f}%")
    frames = {"history": (h, 1)}
    for name, fmt in arms.items():
        frames[name] = (run_arm(name, fmt), len(SEEDS))
    heads, details = [], {}
    for name, (c, k) in frames.items():
        out, s, u = summarize(name, c, k)
        heads.append(out)
        details[name] = (c, s, u, k)
    print("\n== 1. count, length, Big-5 (per seed-year, 1960-63; shares 1961-63) ==")
    print(pd.DataFrame(heads).set_index("arm").to_string())
    print("\n== 1b. per year: Top 10 records, weeks (uncensored), one-week % ==")
    for name, (c, s, u, k) in details.items():
        g = s.groupby("year")
        print(name, {int(y): (round(len(x) / k, 1), round(x[~x.censored].t10Weeks.mean(), 2),
                              round(100 * x[~x.censored].oneWeek.mean(), 1)) for y, x in g if y <= 1963})
    print("\n== 2. peak-band shape (uncensored) ==")
    for name, (c, s, u, k) in details.items():
        print(f"-- {name}\n{band_table(u).to_string()}")
    print("\n== 3. moves at #4-10 ==")
    for name, (c, s, u, k) in details.items():
        t, by = move_table(moves(c))
        print(f"-- {name}\n{t.to_string()}\n post-peak by position:\n{by.to_string()}")
    print("\n== 4. short runs by tier ==")
    for name, (c, s, u, k) in details.items():
        tiers = ["Big5", "field"] + ([] if name == "history" else ["MidTier", "Independent", "Boutique", "Small"])
        print(f"-- {name}\n{short_concentration(u, tiers).to_string()}")
    print("\n== 5. new Top 10 acts per year by tier ==")
    for name, (c, s, u, k) in details.items():
        print(f"-- {name}\n{hit_acts(s, k).round(1).to_string()}")


if __name__ == "__main__":
    main()
