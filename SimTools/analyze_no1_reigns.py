"""What ends a #1 reign (findings §11.7). 208-week runs against the recovered Hot 100, same 209 chart weeks.

usage: analyze_no1_reigns.py NAME=fmt[:seed,...] ...   (fmt holds {s}; seeds default 2002,1001,3003)

1. hazard by reign week: share of reigns still running at week j that end after it (history and sim).
2. rank paths (history and sim): where the next #1 sat in the weeks before taking over, and where the old #1 went.
3. sim only, by reign week j: the #1's weekly true slope (log unsampledChartPoints), its weeks past its units peak,
   the true gap to the best challenger, and how often a challenger sits within 0.10 / 0.25.
4. sim only, reign ends: the gap's closing split into the old #1's fall and the new #1's rise in the handover week.
5. sim only, the #1 at the start of a reign, by eventual reign length: true lead, slope and weeks past its units peak.
"""
import sys
from pathlib import Path
import numpy as np
import pandas as pd

sys.path.insert(0, str(Path(__file__).resolve().parent))
from analyze_no1_returns import history, sim, spells  # noqa: E402


def reigns(c):
    _, sp = spells(c)
    return sp


def hazard(sp, years):
    out = {}
    for j in range(1, 8):
        alive = sp[sp.len >= j]
        out[j] = (round(100 * (alive.len == j).mean(), 1), round(len(alive) / years, 1))
    return out


def rank_paths(c):
    """Per handover week w (new #1 B, old #1 A): B's position at w-1, w-2; A's position at w, w+1."""
    top, sp = spells(c)
    pos = c.set_index(["week", "rid"]).pos
    inc = top.set_index("week").rid
    rows = []
    for _, s in sp.iterrows():
        w = s.start
        if w - 1 not in inc.index:
            continue
        a, b = inc[w - 1], s.rid
        if a == b:
            continue
        g = lambda wk, r: pos.get((wk, r), 41)
        rows.append(dict(b1=g(w - 1, b), b2=g(w - 2, b), b3=g(w - 3, b), a0=g(w, a), a1=g(w + 1, a)))
    d = pd.DataFrame(rows)
    return {"next #1 at #2 a week before %": 100 * (d.b1 == 2).mean(),
            "next #1 in #2-3 a week before %": 100 * d.b1.between(2, 3).mean(),
            "next #1 jumped from #4+ %": 100 * (d.b1 >= 4).mean(),
            "next #1 pos 1 / 2 / 3 wk before (median)": f"{d.b1.median():.0f} / {d.b2.median():.0f} / {d.b3.median():.0f}",
            "old #1 at #2 in handover week %": 100 * (d.a0 == 2).mean(),
            "old #1 pos handover / +1 wk (median)": f"{d.a0.median():.0f} / {d.a1.median():.0f}"}


def by_reign_week(c):
    top, sp = spells(c)
    idx = c.set_index(["week", "rid"])
    t5 = c[c.pos <= 5]
    best_other = {}
    for w, g in t5.groupby("week"):
        best_other[w] = g[["rid", "unsampledChartPoints"]].values
    rows = []
    for _, s in sp.iterrows():
        for j, w in enumerate(range(s.start, s.end + 1), start=1):
            try:
                me, prev = idx.loc[(w, s.rid)], idx.loc[(w - 1, s.rid)]
            except KeyError:
                continue
            others = [p for r, p in best_other.get(w, []) if r != s.rid]
            if not others:
                continue
            gaps = np.log(me.unsampledChartPoints / np.array(others, float))
            rows.append(dict(j=min(j, 6), length=s.len, last=w == s.end, slope=np.log(me.unsampledChartPoints / prev.unsampledChartPoints),
                             wsp=me.weeksSincePeakUnits, gap=gaps.min(), near10=(gaps < 0.10).any(), near25=(gaps < 0.25).any()))
    return pd.DataFrame(rows)


def reign_ends(c):
    top, sp = spells(c)
    idx = c.set_index(["week", "rid"])
    inc = top.set_index("week").rid
    rows = []
    for w in inc.index:
        if w - 1 not in inc.index or inc[w - 1] == inc[w]:
            continue
        a, b = inc[w - 1], inc[w]
        try:
            a0, a1, b0, b1 = idx.loc[(w - 1, a)], idx.loc[(w, a)], idx.loc[(w - 1, b)], idx.loc[(w, b)]
        except KeyError:
            continue
        p = lambda r: np.log(r.unsampledChartPoints)
        dA, dB = p(a1) - p(a0), p(b1) - p(b0)
        rows.append(dict(dA=dA, dB=dB, closed=(p(a0) - p(b0)) - (p(a1) - p(b1)), A_wsp=a1.weeksSincePeakUnits,
                         B_wsp=b1.weeksSincePeakUnits))
    return pd.DataFrame(rows)


def main():
    arms = {}
    for a in (x for x in sys.argv[1:] if "=" in x):
        name, spec = a.split("=", 1)
        fmt, _, seeds = spec.partition(":")
        arms[name] = (fmt, [int(x) for x in (seeds or "2002,1001,3003").split(",")])
    pd.set_option("display.width", 250)
    h = history()
    frames = {"history": (h, 209 / 52)}
    for name, (fmt, seeds) in arms.items():
        frames[name] = (sim(fmt, seeds), len(seeds) * 209 / 52)
    print("== 1. hazard: % of reigns alive at week j that end after it (reigns alive per year)")
    print(pd.DataFrame({n: hazard(reigns(c), y) for n, (c, y) in frames.items()}).to_string())
    print("\n== 2. rank paths around a handover")
    print(pd.DataFrame({n: rank_paths(c) for n, (c, y) in frames.items()}).round(1).to_string())
    for name, (c, y) in frames.items():
        if name == "history":
            continue
        d = by_reign_week(c)
        print(f"\n== 3. {name}: the #1 by reign week j (6 = 6+); last = reign ends after this week")
        print(d.groupby("j").agg(n=("slope", "size"), slope=("slope", "median"), wsp=("wsp", "median"), gap=("gap", "median"),
                                 near10=("near10", "mean"), near25=("near25", "mean"), end=("last", "mean")).round(3).to_string())
        print(f"-- {name}: reign weeks split by whether a challenger is within 0.10: P(end)")
        print(d.groupby(["j", "near10"])["last"].mean().unstack().round(2).to_string())
        e = reign_ends(c)
        print(f"\n== 4. {name}: handover week, median old-#1 change dA, new-#1 change dB; share of the gap closed by A's fall")
        print(f"n={len(e)} dA={e.dA.median():.3f} dB={e.dB.median():.3f} A-share={(-e.dA / e.closed).clip(0, 1).median():.2f} "
              f"A_wsp={e.A_wsp.median():.0f} B_wsp={e.B_wsp.median():.0f}")
        st = d[d.j == 1].copy()
        st["cls"] = pd.cut(st.length, [0, 1, 3, 99], labels=["1 wk", "2-3 wk", "4+ wk"])
        print(f"-- {name}: first reign week by eventual length")
        print(st.groupby("cls", observed=True).agg(n=("gap", "size"), gap=("gap", "median"), slope=("slope", "median"),
                                                    wsp=("wsp", "median"), near25=("near25", "mean")).round(3).to_string())
        # 5. long reigns: slope of the #1 across its reign
        lg = d[d.length >= 4]
        print(f"-- {name}: reigns of 4+ weeks, by reign week")
        print(lg.groupby("j").agg(slope=("slope", "median"), wsp=("wsp", "median"), gap=("gap", "median"),
                                  near25=("near25", "mean")).round(3).to_string())


if __name__ == "__main__":
    main()
