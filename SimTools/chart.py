"""Chart calibration analysis. Replaces the PowerShell streaming parsers.

Usage:  python chart.py score RUN [RUN ...]
        python chart.py debut RUN
        python chart.py traj RUN
        python chart.py inertia RUN
        python chart.py lockstep RUN
"""
import sys
import numpy as np
import pandas as pd

BASE = r"C:\Project\Label-Man\SimLogs"

HIST = dict(debut_mean=86.8, life=7.48, no1_mean=2.57, one_wk=27.0, three_plus=41.0, records=6964)
DEBUT_BUCKETS = [(91, 100, 44.2), (81, 90, 31.5), (71, 80, 15.6), (61, 70, 6.0), (51, 60, 1.6),
                 (41, 50, 0.7), (31, 40, 0.2), (21, 30, 0.1), (11, 20, 0.0), (1, 10, 0.0)]

_cache = {}


def records(run, cols=None):
    key = (run, tuple(cols) if cols else None)
    if key not in _cache:
        _cache[key] = pd.read_csv(f"{BASE}\\{run}-records.csv", usecols=cols, low_memory=False)
    return _cache[key]


def charting(run, cols=None):
    need = set(cols or []) | {"currentPosition"}
    df = records(run, sorted(need))
    return df[(df.currentPosition >= 1) & (df.currentPosition <= 100)]


def lifecycles(run):
    df = pd.read_csv(f"{BASE}\\{run}-lifecycles.csv")
    return df[(~df.leftCensoredAtRunStart.astype(str).str.lower().eq("true")) & (df.peakPosition >= 1)]


def weeks(run):
    df = pd.read_csv(f"{BASE}\\{run}-weeks.csv")
    return df[df.year != 1970]


def no1_tenure(run):
    t = weeks(run).groupby("numberOneRecordId").size()
    return dict(distinct=len(t), mean=t.mean(), one=100 * (t == 1).mean(),
                two=100 * (t == 2).mean(), three=100 * (t >= 3).mean(), mx=t.max())


def debut_from_records(run):
    """Unbiased debut positions: each record's FIRST charting week, from records.csv.

    lifecycles.csv only holds records that have closed, which on a 52-week run is a small,
    short-lived, low-peaking minority that debuts near the cutoff -- it read 85.4 where the
    decade run read 74.3 for the same configuration. Never compare debut across run lengths
    using lifecycles.
    """
    df = charting(run, ["week", "recordId", "currentPosition", "weeksSinceRelease"])
    first = df.sort_values("week").groupby("recordId").first()
    # drop records already charting in week 1 of the run (left-censored)
    return first[(first.week > 1) & (first.weeksSinceRelease >= 1)]


def cmd_debut2(runs):
    print(f"{'run':<28}{'n':>6}{'meanDebut':>11}{'>60%':>7}{'top10':>7}{'91-100%':>9}{'81-100%':>9}")
    print(f"{'HISTORY':<28}{'':>6}{86.8:>11}{2.6:>7}{'~0':>7}{44.2:>9}{75.7:>9}")
    for run in runs:
        d = debut_from_records(run).currentPosition
        print(f"{run:<28}{len(d):>6}{d.mean():>11.1f}{100 * (d < 61).mean():>7.1f}"
              f"{(d <= 10).sum():>7}{100 * d.between(91, 100).mean():>9.1f}{100 * d.between(81, 100).mean():>9.1f}")


def cmd_score(runs):
    hdr = f"{'run':<28}{'units%':>7}{'debutMu':>8}{'>60%':>7}{'top10':>6}{'life':>6}{'no1mean':>8}{'1wk%':>6}{'3+%':>6}{'recs':>6}"
    print(hdr)
    print(f"{'HISTORY':<28}{'100':>7}{HIST['debut_mean']:>8}{'2.6':>7}{'~0':>6}"
          f"{HIST['life']:>6}{HIST['no1_mean']:>8}{HIST['one_wk']:>6}{HIST['three_plus']:>6}{HIST['records']:>6}")
    # Compare units year-for-year against the reference, never as a whole-run total: a 52-week
    # probe covers 1960 only and totalling it against a decade is meaningless.
    refroll = pd.read_csv(f"{BASE}\\d7-v5verify-decade-522-1001-decade-annual-rollup.csv").set_index("year")
    for run in runs:
        roll = pd.read_csv(f"{BASE}\\{run}-decade-annual-rollup.csv").set_index("year")
        yrs = [y for y in roll.index if y in refroll.index and y != 1970]
        units = 100 * roll.loc[yrs].singleUnits.sum() / refroll.loc[yrs].singleUnits.sum()
        d = debut_from_records(run).currentPosition
        lc = lifecycles(run)
        t = no1_tenure(run)
        print(f"{run:<28}{units:>7.1f}{d.mean():>8.1f}"
              f"{100 * (d < 61).mean():>7.1f}{(d <= 10).sum():>6}"
              f"{lc.weeksOnChart.mean():>6.2f}{t['mean']:>8.2f}{t['one']:>6.0f}{t['three']:>6.0f}{len(lc):>6}")


def cmd_debut(run):
    lc = lifecycles(run)
    print(f"{run}: {len(lc)} charting records, mean debut {lc.debutPosition.mean():.1f} (history 86.8)")
    print(f"\n{'bucket':<10}{'model':>8}{'history':>9}")
    for lo, hi, h in DEBUT_BUCKETS:
        share = 100 * lc.debutPosition.between(lo, hi).mean()
        print(f"{f'{lo}-{hi}':<10}{share:>7.1f}%{h:>8.1f}%")
    print(f"\n{'peak band':<12}{'n':>6}{'medDebut':>10}{'meanDebut':>11}{'debut==peak':>13}{'medLife':>9}")
    for lo, hi in [(1, 1), (2, 10), (11, 40), (41, 70), (71, 100)]:
        s = lc[lc.peakPosition.between(lo, hi)]
        if s.empty:
            continue
        print(f"{f'{lo}-{hi}':<12}{len(s):>6}{s.debutPosition.median():>10.0f}{s.debutPosition.mean():>11.1f}"
              f"{100 * (s.debutPosition == s.peakPosition).mean():>12.1f}%{s.weeksOnChart.median():>9.0f}")


def cmd_traj(run):
    df = charting(run, ["recordId", "weeksSinceRelease", "unitsThisWeek", "currentPosition"])
    peak = df.groupby("recordId").currentPosition.min()
    top = peak[peak <= 10].index
    all_df = records(run, ["recordId", "weeksSinceRelease", "unitsThisWeek", "currentPosition"])
    s = all_df[all_df.recordId.isin(top)].copy()
    mx = s.groupby("recordId").unitsThisWeek.transform("max")
    s = s[mx > 1000]
    s["share"] = s.unitsThisWeek / mx[mx > 1000]
    s["pos"] = s.currentPosition.where(s.currentPosition >= 1, 110)
    g = s[s.weeksSinceRelease.between(1, 22)].groupby("weeksSinceRelease").agg(
        n=("share", "size"), share=("share", "mean"), pos=("pos", "mean"))
    print(f"{run}: {len(top)} top-10 records")
    print(f"{'wk':>3}{'n':>6}{'sales%peak':>12}{'meanPos':>9}")
    for wk, r in g.iterrows():
        print(f"{wk:>3}{int(r.n):>6}{100 * r.share:>11.1f}%{r.pos:>9.1f}")


def cmd_inertia(run):
    """How often does the fall cap actually bind, and what does it cost in positions?"""
    df = charting(run, ["week", "recordId", "currentPosition", "previousPosition", "unitsThisWeek", "momentum"])
    d = df[(df.previousPosition >= 1)].copy()
    d["drop"] = d.currentPosition - d.previousPosition
    falling = d[d["drop"] > 0]
    print(f"{run}: {len(d)} charted weeks with a previous position; {len(falling)} are falls")
    print(f"  median fall {falling['drop'].median():.0f}, p90 {falling['drop'].quantile(.9):.0f}, max {falling['drop'].max()}")
    # inertia is eligible only when the record still sells, isn't 3 weeks negative, momentum > -0.20
    elig = falling[(falling.unitsThisWeek > 0) & (falling.momentum > -0.20)]
    print(f"  falls where the cap is ELIGIBLE to bind (units>0, momentum>-0.20): {len(elig)} "
          f"({100 * len(elig) / max(1, len(falling)):.1f}%)")
    for lo, hi in [(1, 1), (2, 10), (11, 40), (41, 100)]:
        s = falling[falling.previousPosition.between(lo, hi)]
        e = elig[elig.previousPosition.between(lo, hi)]
        if s.empty:
            continue
        print(f"    from {lo}-{hi}: {len(s)} falls, median {s['drop'].median():.0f}, "
              f"{100 * len(e) / len(s):.0f}% eligible for protection")


def cmd_bite(run):
    """How much is the inertia cap actually holding records up?

    Observed falls are POST-cap, so they cannot show the cap's bite on their own. Re-derive the
    raw ranking each week from published chartPoints and compare it to the position actually
    assigned: the gap is what GetInertiaPositionCap (plus bubbling-under) is worth in places.
    """
    df = charting(run, ["week", "recordId", "currentPosition", "previousPosition", "chartPoints"])
    df = df.copy()
    df["rawRank"] = df.groupby("week").chartPoints.rank(ascending=False, method="first")
    df["lift"] = df.rawRank - df.currentPosition      # >0 => published ABOVE what points justify
    print(f"{run}: {len(df)} charted record-weeks")
    print(f"  mean lift from the cap: {df.lift.mean():+.2f} positions, median {df.lift.median():+.0f}")
    print(f"  share published ABOVE their points rank: {100 * (df.lift > 0).mean():.1f}%")
    print(f"\n{'published band':<16}{'n':>7}{'meanLift':>10}{'p90Lift':>9}{'>0 share':>10}")
    for lo, hi in [(1, 1), (2, 10), (11, 40), (41, 100)]:
        s = df[df.currentPosition.between(lo, hi)]
        print(f"{f'{lo}-{hi}':<16}{len(s):>7}{s.lift.mean():>+10.2f}{s.lift.quantile(.9):>+9.0f}"
              f"{100 * (s.lift > 0).mean():>9.1f}%")
    # falls specifically: how far did points say they should drop vs how far they did?
    f = df[(df.previousPosition >= 1) & (df.currentPosition > df.previousPosition)].copy()
    f["actualDrop"] = f.currentPosition - f.previousPosition
    f["rawDrop"] = f.rawRank - f.previousPosition
    print(f"\n  on falls (n={len(f)}): points implied a median drop of {f.rawDrop.median():.0f} "
          f"places, the chart delivered {f.actualDrop.median():.0f}")
    for lo, hi in [(1, 1), (2, 10), (11, 40)]:
        s = f[f.previousPosition.between(lo, hi)]
        if s.empty:
            continue
        print(f"    from {lo}-{hi}: implied {s.rawDrop.median():.0f}, delivered {s.actualDrop.median():.0f} "
              f"(n={len(s)})")


def cmd_spells(runs):
    """Decompose #1 tenure into spell length and returns.

    Per-record tenure conflates two different things and reading it as run length has misdirected
    this metric three times. Spell length is already on target (~2.57); the overshoot is records
    RECLAIMING the top spot -- 24-28% against a historical 4-5%.
    """
    print(f"{'run':<30}{'recs':>6}{'spells':>8}{'spellLen':>10}{'perRec':>8}{'2+spells':>10}")
    print(f"{'HISTORY':<30}{'203':>6}{'~213':>8}{2.57:>10}{2.57:>8}{'4-5%':>10}")
    for run in runs:
        w = weeks(run).sort_values("week")
        lead = list(w.numberOneRecordId)
        spells, cur, n = [], None, 0
        for x in lead:
            if x == cur:
                n += 1
            else:
                if cur is not None:
                    spells.append((cur, n))
                cur, n = x, 1
        spells.append((cur, n))
        d = pd.DataFrame(spells, columns=["rec", "len"])
        cnt = d.groupby("rec").size()
        tot = d.groupby("rec")["len"].sum()
        print(f"{run:<30}{len(cnt):>6}{len(d):>8}{d['len'].mean():>10.2f}{tot.mean():>8.2f}"
              f"{100 * (cnt >= 2).mean():>9.1f}%")


def cmd_tail(run):
    """Is airplay propping up the post-peak tail, and how much does tail length vary?

    chartPoints = (sales + airplay*eraWeight) * surveySample. surveySample has mean 1, so
    averaging chartPoints/units across many records recovers the airplay share of points.
    """
    df = records(run, ["recordId", "weeksSinceRelease", "unitsThisWeek", "chartPoints", "currentPosition"])
    peak = df[df.currentPosition >= 1].groupby("recordId").currentPosition.min()
    top = peak[peak <= 10].index
    s = df[df.recordId.isin(top) & (df.unitsThisWeek > 0)].copy()
    s["ratio"] = s.chartPoints / s.unitsThisWeek           # 1 + airplay share
    mx = s.groupby("recordId").unitsThisWeek.transform("max")
    s["share"] = s.unitsThisWeek / mx
    g = s[s.weeksSinceRelease.between(1, 20)].groupby("weeksSinceRelease").agg(
        n=("ratio", "size"), ratio=("ratio", "median"), share=("share", "mean"))
    print(f"{run}: {len(top)} top-10 records -- does airplay hold the tail up?")
    print(f"{'wk':>3}{'n':>7}{'sales%peak':>12}{'pts/units':>11}{'airplay%ofPts':>15}")
    for wk, r in g.iterrows():
        print(f"{wk:>3}{int(r.n):>7}{100 * r.share:>11.1f}%{r.ratio:>11.2f}{100 * (1 - 1 / r.ratio):>14.1f}%")

    # How much does tail LENGTH vary between records? Weeks from sales peak to falling under 40% of it.
    def tail_len(grp):
        grp = grp.sort_values("weeksSinceRelease")
        pk = grp.unitsThisWeek.idxmax()
        after = grp.loc[pk:]
        below = after[after.unitsThisWeek < 0.40 * grp.unitsThisWeek.max()]
        return (below.weeksSinceRelease.iloc[0] - grp.loc[pk, "weeksSinceRelease"]) if len(below) else np.nan
    tails = s.groupby("recordId").apply(tail_len, include_groups=False).dropna()
    print(f"\n  weeks from sales peak to 40% of peak: median {tails.median():.1f}, "
          f"sd {tails.std():.2f}, p10 {tails.quantile(.1):.0f}, p90 {tails.quantile(.9):.0f}")
    print(f"  coefficient of variation: {tails.std() / tails.mean():.3f}  (0 = every record identical)")

    # And the peak-week spread: if every record peaks at the same age, they cannot cross.
    pw = s.loc[s.groupby("recordId").unitsThisWeek.idxmax(), "weeksSinceRelease"]
    print(f"  week of sales peak: median {pw.median():.0f}, sd {pw.std():.2f}, "
          f"p10 {pw.quantile(.1):.0f}, p90 {pw.quantile(.9):.0f}")


def cmd_drops(run):
    """Station-drop dynamics, and the re-add rate the handoff asks for explicitly.

    radioPanelShare is the reach-weighted share of the national radio panel still carrying the
    record. The latch means it may only ever FALL: any record-week where it rises against the same
    record's previous week is a leak in the latch, not a re-add we chose.
    """
    df = records(run, ["week", "recordId", "weeksSinceRelease", "unitsThisWeek",
                       "currentPosition", "radioPanelShare"])
    d = df.sort_values(["recordId", "week"]).copy()
    d["prev"] = d.groupby("recordId").radioPanelShare.shift()
    moved = d[d.prev.notna()]
    readds = moved[moved.radioPanelShare > moved.prev + 1e-6]
    print(f"{run}: {len(d)} record-weeks, {d.recordId.nunique()} records")
    print(f"  RE-ADD RATE: {len(readds)} of {len(moved)} record-weeks "
          f"({100 * len(readds) / max(1, len(moved)):.4f}%)  -- the latch requires exactly 0")
    print(f"  records ever partly dropped: "
          f"{100 * (d.groupby('recordId').radioPanelShare.min() < 0.999).mean():.1f}%")
    print(f"  records fully dropped:       "
          f"{100 * (d.groupby('recordId').radioPanelShare.min() < 0.001).mean():.1f}%")

    # When does a record lose its first market, and when its last?
    peak = d[d.currentPosition >= 1].groupby("recordId").currentPosition.min()
    for label, ids in [("top-10 records", peak[peak <= 10].index),
                       ("11-40 records", peak[peak.between(11, 40)].index),
                       ("41-100 records", peak[peak.between(41, 100)].index)]:
        s = d[d.recordId.isin(ids)]
        first = s[s.radioPanelShare < 0.999].groupby("recordId").weeksSinceRelease.min()
        last = s[s.radioPanelShare < 0.001].groupby("recordId").weeksSinceRelease.min()
        if first.empty:
            continue
        print(f"\n  {label} (n={len(ids)}): first market cut at age median {first.median():.0f} "
              f"(p10 {first.quantile(.1):.0f}, p90 {first.quantile(.9):.0f}, sd {first.std():.2f})")
        if not last.empty:
            print(f"    off the air entirely at age median {last.median():.0f} "
                  f"(p10 {last.quantile(.1):.0f}, p90 {last.quantile(.9):.0f}, sd {last.std():.2f}), "
                  f"reached by {100 * len(last) / len(ids):.0f}%")

    # Panel share against the sales trajectory, for top-10 records: is the drop phased with sales?
    top = peak[peak <= 10].index
    s = d[d.recordId.isin(top)].copy()
    mx = s.groupby("recordId").unitsThisWeek.transform("max")
    s = s[mx > 1000]
    s["share"] = s.unitsThisWeek / mx[mx > 1000]
    g = s[s.weeksSinceRelease.between(1, 24)].groupby("weeksSinceRelease").agg(
        n=("radioPanelShare", "size"), panel=("radioPanelShare", "mean"), sales=("share", "mean"))
    print(f"\n{'wk':>3}{'n':>7}{'sales%peak':>12}{'panelCarrying':>15}")
    for wk, r in g.iterrows():
        print(f"{wk:>3}{int(r.n):>7}{100 * r.sales:>11.1f}%{100 * r.panel:>14.1f}%")


def cmd_lockstep(run):
    """Do records move together? Correlate week-over-week position change across the top 40."""
    df = charting(run, ["week", "recordId", "currentPosition", "unitsThisWeek", "weeksSinceRelease"])
    d = df.sort_values(["recordId", "week"])
    d["dpos"] = d.groupby("recordId").currentPosition.diff()
    d["dunits"] = d.groupby("recordId").unitsThisWeek.pct_change()
    t = d[(d.currentPosition <= 40) & d.dpos.notna()]
    print(f"{run}: lockstep diagnostics over {len(t)} top-40 record-weeks")
    # spread of weeksSinceRelease among the top 10 in a given week: if everyone is the same age,
    # they are riding the same ramp at the same time and cannot cross.
    top10 = df[df.currentPosition <= 10]
    age = top10.groupby("week").weeksSinceRelease.agg(["std", "mean", "count"])
    print(f"  age spread inside the top 10: mean sd {age['std'].mean():.2f} weeks, "
          f"mean age {age['mean'].mean():.1f}")
    # how much of position change is shared across the chart each week (common-mode)?
    wk = t.groupby("week").dpos.agg(["mean", "std"])
    print(f"  weekly mean |position change| across top 40: {t.dpos.abs().mean():.2f}")
    print(f"  common-mode (|weekly mean| / weekly sd): {(wk['mean'].abs() / wk['std']).mean():.3f}")
    # rank correlation of this week's order with last week's, inside the top 40
    piv = t.pivot_table(index="week", columns="recordId", values="currentPosition")
    cors = []
    for i in range(1, len(piv)):
        a, b = piv.iloc[i - 1], piv.iloc[i]
        m = a.notna() & b.notna()
        if m.sum() > 5:
            # rank-then-Pearson == Spearman, and avoids a scipy dependency
            cors.append(a[m].rank().corr(b[m].rank()))
    print(f"  week-to-week Spearman of top-40 order: {np.mean(cors):.4f} (1.0 = frozen)")


if __name__ == "__main__":
    cmd, args = sys.argv[1], sys.argv[2:]
    {"score": lambda: cmd_score(args), "debut": lambda: cmd_debut(args[0]),
     "traj": lambda: cmd_traj(args[0]), "inertia": lambda: cmd_inertia(args[0]),
     "lockstep": lambda: cmd_lockstep(args[0]), "debut2": lambda: cmd_debut2(args), "bite": lambda: cmd_bite(args[0]), "tail": lambda: cmd_tail(args[0]), "spells": lambda: cmd_spells(args),
     "drops": lambda: cmd_drops(args[0])}[cmd]()




