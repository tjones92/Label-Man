"""Pooled 3-seed summary for the MidTier workload-staffing / British-deferral A/B (SimTools/MidTierClosureResearch.md).

Usage: python SimTools/summarize_midtier_ab.py control=SimLogs/mts-ab1-control staffing=SimLogs/mts-ab1-staffing ...
Each value is a run prefix without -default-<seed>-53. London labels are excluded from MidTier counts so every arm
compares the same US firms.
"""
import os, sys, statistics as st
sys.path.insert(0, os.path.dirname(__file__))
from analyze_midtier_closures import analyse, top10

SEEDS = (1001, 2002, 3003)
LONDON = ("Parlophone", "Pye", "EMI", "Decca UK")
HIST = ("Atlantic", "Chess", "King", "Cameo-Parkway")

def row(prefix):
    out, _ = analyse(prefix, verbose=False)
    us = [o for o in out if not o["name"].startswith(LONDON)]
    r = {}
    for t in ("MidTier", "Independent", "Boutique", "Small"):
        ts = [o for o in us if o["tier"] == t]
        r[t] = (sum(o["exit"] is not None for o in ts), len(ts))
    hist = [o for o in us if o["tier"] == "MidTier" and o["hist"] and o["name"].startswith(HIST)]
    r["hist"] = (sum(o["exit"] is None for o in hist), len(hist))
    mids = [o for o in us if o["tier"] == "MidTier"]
    r["pay"] = st.mean(o["payroll"] for o in mids)
    r["distress"] = st.mean(o["distress13"] for o in mids) / 13
    r["launches"] = sum(o["launches"] for o in mids)
    r.update(top10(prefix, {o["id"]: o["tier"] for o in out}))
    return r

arms = dict(a.split("=", 1) for a in sys.argv[1:])
data = {arm: {s: row(f"{p}-default-{s}-53") for s in SEEDS} for arm, p in arms.items()}
base = next(iter(arms))
for arm, seeds in data.items():
    def pooled(k): c = sum(seeds[s][k][0] for s in SEEDS); n = sum(seeds[s][k][1] for s in SEEDS); return c, n
    cells = []
    for k in ("MidTier", "Independent", "Boutique", "Small"):
        c, n = pooled(k)
        diffs = [seeds[s][k][0] / seeds[s][k][1] - data[base][s][k][0] / data[base][s][k][1] for s in SEEDS]
        cells.append(f"{k[:5]} {c}/{n}={c/n:.1%} ({st.mean(diffs):+.1%}, per-seed {' '.join(f'{d:+.0%}' for d in diffs)})")
    h = pooled("hist")
    print(f"{arm:9} " + " | ".join(cells) + f" | histMid alive {h[0]}/{h[1]}")
    print(" " * 10 + " ".join(f"{k} {st.mean(seeds[s][k] for s in SEEDS):.3f} ({' '.join(f'{seeds[s][k]:.3f}' for s in SEEDS)})"
                              for k in ("cr4", "major", "mid", "distress"))
          + f" pay {st.mean(seeds[s]['pay'] for s in SEEDS):.0f} launches {sum(seeds[s]['launches'] for s in SEEDS)}")
