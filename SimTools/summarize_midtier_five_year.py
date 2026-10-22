"""Five-year screen of the MidTier opening pair (SimTools/MidTierClosureResearch.md section 7).

Usage: python SimTools/summarize_midtier_five_year.py control=SimLogs/mtn5-control pair=SimLogs/mtn5-pair
Each value is a run prefix without -default-<seed>-265.

Per year 1960-64, mean over 3 seeds (per-seed in brackets): Top 10 CR4 over operating labels (the 30-40 band is for
1960-61), Major and MidTier Top 10 share by initial tier, majorFamilyChartShare and the owner-Major-family share of
chart entries from *-concentration.csv (the consolidation reading), and cumulative closures of starting firms by
initial tier.
"""
import csv, statistics as st, sys
from collections import Counter, defaultdict
import pandas as pd

SEEDS = (1001, 2002, 3003)
YEARS = range(1960, 1965)
TIERS = ("Major", "MidTier", "Independent", "Boutique", "Small")

def rows(path):
    with open(path, newline="", encoding="utf-8") as f: yield from csv.DictReader(f)

def world(prefix):
    tier = {r["labelId"]: r["initialTier"] for r in rows(f"{prefix}-label-directory.csv")}
    rec = pd.read_csv(f"{prefix}-records.csv", usecols=["year", "labelId", "currentPosition"])
    rec["labelId"] = rec.labelId.astype(str).str.strip('"')
    top = rec[(rec.currentPosition > 0) & (rec.currentPosition <= 10)]
    out = {}
    for y in YEARS:
        t = top[top.year == y]
        if not len(t): continue
        tiers = t.labelId.map(tier).fillna("Runtime")
        out[y] = dict(cr4=t.labelId.value_counts().head(4).sum() / len(t), major=(tiers == "Major").mean(), mid=(tiers == "MidTier").mean())
    for r in rows(f"{prefix}-concentration.csv"):
        y = int(r["year"])
        if y in out:
            entries = max(1.0, float(r["chartEntries"]))
            out[y].update(family=float(r["majorFamilyChartShare"]), ownerFamily=float(r["ownerMajorFamilyEntries"]) / entries,
                          imprintMajor=float(r["chartEntriesMajor"]) / entries)
    exits = {}
    for r in rows(f"{prefix}-label-finance.csv"):
        if r["exitReason"] not in ("", "None") and r["labelId"] not in exits and r["labelId"] in tier: exits[r["labelId"]] = int(r["year"])
    starting = Counter(tier.values())
    closed = {y: Counter(tier[l] for l, ey in exits.items() if ey <= y) for y in YEARS}
    transitions = Counter((r["direction"], r["fromTier"], r["toTier"]) for r in rows(f"{prefix}-label-tier-transitions.csv"))
    return out, closed, starting, transitions

def fmt(vals): return f"{st.mean(vals):.3f} ({' '.join(f'{v:.3f}' for v in vals)})"

arms = dict(a.split("=", 1) for a in sys.argv[1:])
data = {arm: {s: world(f"{p}-default-{s}-265") for s in SEEDS} for arm, p in arms.items()}
for arm, per in data.items():
    print(f"\n== {arm} ==")
    print("year  CR4                         Major T10   MidTier T10  majorFamily  ownerFamily entries  imprint-Major entries")
    for y in YEARS:
        def v(k): return [per[s][0][y][k] for s in SEEDS if y in per[s][0] and k in per[s][0][y]]
        print(f"{y}  {fmt(v('cr4')):27} {st.mean(v('major')):.3f}       {st.mean(v('mid')):.3f}        {st.mean(v('family')):.3f}        {st.mean(v('ownerFamily')):.3f}                {st.mean(v('imprintMajor')):.3f}")
    for y in (1960, 1962, 1964):
        print(f"closed by end {y}: " + "  ".join(f"{t} {sum(per[s][1][y][t] for s in SEEDS)}/{sum(per[s][2][t] for s in SEEDS)}" for t in TIERS))
    tr = Counter()
    for s in SEEDS: tr.update(per[s][3])
    print("tier transitions (3 seeds): " + ", ".join(f"{d} {a}->{b} {n}" for (d, a, b), n in tr.most_common(8)))
