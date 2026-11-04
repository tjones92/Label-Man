"""Are MidTier closures earned? Q1 launch quality, distress weeks and launches of closing vs surviving US MidTiers.

Usage: python SimTools/compare_midtier_closure_quality.py <arm>=<53-week prefix> ...  (prefix omits -default-<seed>-53)
"""
import os, statistics as st, sys
sys.path.insert(0, os.path.dirname(__file__))
from analyze_midtier_closures import analyse, rows

SEEDS = (1001, 2002, 3003)
LONDON = ("Parlophone", "Pye", "EMI", "Decca UK")

for arg in sys.argv[1:]:
    arm, prefix = arg.split("=", 1)
    groups = {True: [], False: []}
    for s in SEEDS:
        p = f"{prefix}-default-{s}-53"
        out, _ = analyse(p, verbose=False)
        quality = {}
        for r in rows(p, "demand-release-census"):
            if r["observationKind"] != "Opening" and int(float(r["observedAuditWeek"])) <= 13:
                quality.setdefault(r["labelId"], []).append(float(r["runtimeQualityAtLaunch"]))
        for o in out:
            if o["tier"] == "MidTier" and not o["name"].startswith(LONDON):
                groups[o["exit"] is not None].append((st.mean(quality[o["id"]]) if o["id"] in quality else None, o["distress13"], o["launches13"]))
    for closed, g in groups.items():
        q = [x[0] for x in g if x[0] is not None]
        print(f"{arm:10} {'closed' if closed else 'survived':8} n={len(g):2}  Q1 launch quality {st.mean(q):.3f}  "
              f"Q1 distress weeks {st.mean(x[1] for x in g):.1f}/13  Q1 launches {st.mean(x[2] for x in g):.1f}")
