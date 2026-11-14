"""Who the hit acts are: a selection test for the follow-up gap (FollowUpGapFableHandoff.md, section 6).

usage: analyze_hitact_selection.py name=fmt ...      ({s} = seed; seeds 2002, 1001)

Hit act in Y = an act whose record first reached the Top 10 in Y - 1, tiered by that record's label (history: owner, Big-5 =
producing label (Columbia, RCA Victor, Capitol, Decca, Mercury), like the sim's flagships and the share targets;
--owner uses owner grouping, HISTORY_BIG5, as section 17aa did; everything else is "other"). Y = 1961-63. Both history and the sim chart start in
January 1960, so a 1960 hit has no 52-week lookback: the new / experienced split uses Y = 1962-63 only.
Debuts are also counted as chart events (an act's debuts under 4 weeks apart count once), since history's Hot 100 lists
both sides of a two-sided hit.

  1. chart record before the hit - Hot 100 debuts in the 52 weeks before the hit's debut, share with none since 1960
     ("new"), share with an earlier Top 10; the hit's peak and Top 10 weeks.
  2. debuts in Y per hit act, by tier and by new / experienced, history against the sim. If the sim's field hit acts
     are newer than history's, composition explains part of the gap; if the sim's new field hit acts debut less
     than history's new field hit acts, it does not.
  3. sim only, endowment and luck: the hit record's quality and hook, its keyed fit draw z (luck: independent of the
     act) and quality rank in its year; the act's career state at the hit; the act's singles' quality before and
     after the hit, and the drop from hit to follow-ups (regression to the mean).
"""
import sys
from pathlib import Path
import numpy as np, pandas as pd
import keyed_fit_draws as kfd
from analyze_studio_parent import HISTORY_BIG5
HISTORY_LABEL_BIG5 = {"Columbia", "RCA Victor", "Capitol", "Decca", "Mercury"}   # as analyze_top10_turnover.py

SEEDS = (2002, 1001)
OWNER = "--owner" in sys.argv
sys.argv = [a for a in sys.argv if a != "--owner"]
B5 = {f"label_000{i}" for i in range(1, 6)}
YEARS = (1961, 1962, 1963)   # lookback columns (prior52, new, prior T10) are censored for Y = 1961
pd.set_option("display.width", 250)
ROOT = Path(__file__).resolve().parent.parent / "SimLogs"


def chart_frame(chart, debut_cut):
    """Per record: act, debut week/year, peak, Top 10 weeks, first Top 10 week/year."""
    rec = chart.groupby("recordId").agg(artistId=("artistId", "first"), debutWeek=("week", "min"), debutYear=("year", "min"),
                                        peak=("pos", "min"))
    t10 = chart[chart.pos <= 10].groupby("recordId").agg(t10Week=("week", "min"), t10Year=("year", "min"), t10Weeks=("week", "size"))
    rec = rec.join(t10)
    rec["t10Weeks"] = rec.t10Weeks.fillna(0)
    rec["opening"] = rec.debutWeek == chart.week.min()
    rec["counted"] = rec.debutWeek <= debut_cut
    return rec


def events(weeks, gap=3):
    """Debuts by one act less than gap + 1 weeks apart count once (a two-sided hit charts both sides)."""
    w = np.sort(np.asarray(weeks))
    return int(len(w) and 1 + (np.diff(w) > gap).sum())


def hit_acts(rec, tier_of):
    rows = []
    for Y in YEARS:
        hits = rec[rec.t10Year == Y - 1].sort_values("t10Week").drop_duplicates("artistId")
        for rid, h in hits.iterrows():
            mine = rec[(rec.artistId == h.artistId) & ~rec.opening]
            before = mine[mine.debutWeek < h.debutWeek]
            prior10 = rec[(rec.artistId == h.artistId) & (rec.t10Week < h.t10Week)]
            yd = mine[(mine.debutYear == Y) & mine.counted & (mine.index != rid)]
            rows.append(dict(Y=Y, artistId=h.artistId, hitId=rid, tier=tier_of(rid, h), hitWeek=h.t10Week,
                             prior52=int(((before.debutWeek >= h.debutWeek - 52)).sum()),
                             new=int(len(before) == 0 and not (rec[(rec.artistId == h.artistId) & rec.opening].shape[0])),
                             prior10=int(len(prior10) > 0), hitPeak=h.peak, hitT10Weeks=h.t10Weeks,
                             debutsY=len(yd), eventsY=events(yd.debutWeek), t10Y=int(yd.t10Week.notna().sum())))
    return pd.DataFrame(rows)


def history():
    chart = pd.read_csv(ROOT / "historical-hot100-1960-64" / "hot100_weekly.csv", parse_dates=["chartDate"])
    dates = sorted(chart.chartDate.unique())
    chart["week"] = chart.chartDate.map({d: n for n, d in enumerate(dates)})
    chart["year"] = chart.chartDate.dt.year
    chart["artistId"] = chart.artist
    chart["recordId"] = chart.artist + " || " + chart.title
    chart["pos"] = chart.position
    owners = pd.concat([pd.read_csv(ROOT / f"historical-top10-{y}" / f"records_{y}_grouped.csv") for y in range(1960, 1965)])
    owners["recordId"] = owners.artist + " || " + owners.title
    owners = owners.sort_values("first").drop_duplicates("recordId").set_index("recordId")
    rec = chart_frame(chart, chart.week.max())
    # Producing label (Columbia, RCA Victor, Capitol, Decca, Mercury) matches the sim's flagship Big-5 and the author's
    # share targets; owner grouping (HISTORY_BIG5) adds Epic, Smash and other imprints. --owner selects it.
    col, big5 = ("owned", HISTORY_BIG5) if OWNER else ("label", HISTORY_LABEL_BIG5)
    return hit_acts(rec, lambda rid, h: "Big5" if owners[col].get(rid) in big5 else "other")


def sim(fmt):
    acts, launches = [], []
    for seed in SEEDS:
        name = fmt.format(s=seed); run = "SimLogs/" + name
        r = pd.read_csv(run + "-records.csv", usecols=["week", "year", "recordId", "artistId", "labelId", "currentPosition"])
        c = r[r.currentPosition.between(1, 100)].rename(columns={"currentPosition": "pos"})
        labels = c.groupby("recordId").labelId.first()
        rec = chart_frame(c, r.week.max() - 12)
        h = hit_acts(rec, lambda rid, h: "Big5" if labels[rid] in B5 else "other")
        h["seed"] = seed
        tiers = pd.read_csv(run + "-company-monthly.csv", usecols=["year", "labelId", "tier"]).drop_duplicates(["labelId", "year"]).set_index(["labelId", "year"]).tier
        h["fieldTier"] = [("Big5" if labels[rid] in B5 else {"Major": "MidTier", "Boutique": "Small"}.get(tiers.get((labels[rid], Y - 1), "Independent"),
                           tiers.get((labels[rid], Y - 1), "Independent"))) for rid, Y in zip(h.hitId, h.Y)]
        a = pd.read_csv(run + "-single-launch-attribution.csv")
        kfd.configure(*kfd.params_for_run(name))
        a["z"] = [kfd.national_z(seed, s) for s in a.singleId]
        a["qRank"] = a.groupby("year").quality.rank(pct=True)
        a["seed"] = seed
        acts.append(h); launches.append(a)
    return pd.concat(acts, ignore_index=True), pd.concat(launches, ignore_index=True)


def frame_table(h, by):
    years = h.Y.nunique()
    g = h.groupby(by)
    return pd.DataFrame({"acts/seed-yr": g.size() / (len(SEEDS) * years if "seed" in h else years),
                         "prior52": g.prior52.mean(), "new %": 100 * g.new.mean(), "prior T10 %": 100 * g.prior10.mean(),
                         "hit peak": g.hitPeak.mean(), "hit T10 wks": g.hitT10Weeks.mean(),
                         "debutsY": g.debutsY.mean(), "eventsY": g.eventsY.mean(), "T10/debut %": 100 * g.t10Y.sum() / g.debutsY.sum()})


def per_year(h, label):
    g = h.groupby(["Y", "tier"]).agg(acts=("debutsY", "size"), debuts=("debutsY", "mean"), events=("eventsY", "mean")).unstack("tier")
    g[("ratio", "debuts")] = g[("debuts", "Big5")] / g[("debuts", "other")]
    g[("ratio", "events")] = g[("events", "Big5")] / g[("events", "other")]
    print(label); print(g.round(2).to_string())



if __name__ == "__main__":
    hist = history()
    print("######## history (hit act in Y = first Top 10 in Y-1; Y = 1961-63)")
    print(frame_table(hist, "tier").round(2).to_string())
    print("new / experienced, Y = 1962-63")
    print(frame_table(hist[hist.Y >= 1962], ["tier", "new"]).round(2).to_string())
    SIM_ACTS = {}

    for name, fmt in [x.split("=", 1) for x in sys.argv[1:]]:
        h, a = sim(fmt)
        SIM_ACTS[name] = h
        print(f"\n######## {name}")
        print("1-2. chart record before the hit, and debuts in Y")
        print(frame_table(h, "tier").round(2).to_string())
        print("new / experienced, Y = 1962-63")
        print(frame_table(h[h.Y >= 1962], ["tier", "new"]).round(2).to_string())
        print(frame_table(h, "fieldTier").round(2).to_string())
        # 3. endowment and luck on the hit record and the act's other singles
        a = a.merge(h[["seed", "artistId", "hitId", "hitWeek", "tier", "fieldTier", "Y"]], on=["seed", "artistId"])
        hitrow = a[a.singleId == a.hitId]
        before = a[a.week < a.hitWeek - 0].groupby(["seed", "artistId", "hitId"]).quality.mean().rename("qBefore")
        after = a[(a.week > a.hitWeek) & (a.week <= a.hitWeek + 52)]
        afterq = after.groupby(["seed", "artistId", "hitId"]).agg(qAfter=("quality", "mean"), hookAfter=("hook", "mean"), zAfter=("z", "mean"),
                                                                 nAfter=("quality", "size"))
        x = hitrow.set_index(["seed", "artistId", "hitId"]).join(before).join(afterq)
        x["qDrop"] = x.quality - x.qAfter
        print("3. hit record and the act's singles (sim; career = state at the hit's launch)")
        print(x.groupby("fieldTier").agg(n=("quality", "size"), hitQ=("quality", "mean"), hitHook=("hook", "mean"), hitQRank=("qRank", "mean"),
                                         hitZ=("z", "mean"), qBefore=("qBefore", "mean"), qAfter=("qAfter", "mean"), hookAfter=("hookAfter", "mean"),
                                         qDrop=("qDrop", "mean"), zAfter=("zAfter", "mean"), singlesAfter=("nAfter", "mean"),
                                         awareAtHit=("initialAwareness", "mean")).round(3).to_string())
        print(pd.crosstab(x.fieldTier, x.career, normalize="index").mul(100).round(0).to_string())
        # launches in Y and their Hot 100 entry, per hit act, by tier and new / experienced
        charted = set()
        for seed in SEEDS:
            r = pd.read_csv("SimLogs/" + fmt.format(s=seed) + "-records.csv", usecols=["recordId", "currentPosition"])
            charted |= {(seed, rid) for rid in r[r.currentPosition.between(1, 100)].recordId.unique()}
        ly = a[(a.year == a.Y) & (a.singleId != a.hitId)].copy()
        ly["entered"] = [(sd, rid) in charted for sd, rid in zip(ly.seed, ly.singleId)]
        k = ly.groupby(["seed", "artistId", "Y"]).agg(launchesY=("entered", "size"), enteredY=("entered", "sum"))
        hh = h.set_index(["seed", "artistId", "Y"]).join(k).fillna({"launchesY": 0, "enteredY": 0})
        print("launches in Y and their Hot 100 entry, Y = 1962-63")
        for by in (["tier", "new"], ["fieldTier", "new"]):
            g = hh[hh.index.get_level_values("Y") >= 1962].groupby(by)
            print(pd.DataFrame({"acts": g.size(), "launchesY": g.launchesY.mean(), "entry %": 100 * g.enteredY.sum() / g.launchesY.sum(),
                                "debutsY": g.debutsY.mean()}).round(2).to_string())


    per_year(hist, "history per Y")
    for name, h in SIM_ACTS.items():
        per_year(h, name + " per Y (acts summed over seeds)")
