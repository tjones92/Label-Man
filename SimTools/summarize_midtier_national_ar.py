"""MidTier national A&R screen (SimTools/MidTierClosureResearch.md section 5).

Usage:
  python SimTools/summarize_midtier_national_ar.py fill control=SimLogs/mtn-fill-control national=SimLogs/mtn-fill-national
  python SimTools/summarize_midtier_national_ar.py rosters control=SimLogs/mts-ab2-both national=SimLogs/mtn-ab-national ...
Each value is a run prefix without -default-<seed>-<weeks>.

fill: launch fill by tier from *-scene-initialization-labels.csv, the named MidTiers and New York's MidTiers, NationalAr
signings from *-scene-recruitment-start.csv, and what Independents opened with (fill, mean quality, strong acts).
rosters: runtime signings and failed appointments (daily-talent-appointments) and per-firm costs (company-monthly) by
initial tier from the 53-week worlds, to see whether Independents lose acts they need after launch.
"""
import csv, os, sys, statistics as st
from collections import Counter, defaultdict

SEEDS = (1001, 2002, 3003)
TIERS = ("Major", "MidTier", "Independent", "Boutique", "Small")
NAMED = ("Atlantic", "Chess", "King", "Cameo-Parkway", "Dot", "Liberty", "ABC-Paramount", "Cadence", "Vee-Jay")

def rows(path):
    with open(path, newline="", encoding="utf-8") as f:
        yield from csv.DictReader(f)

def f(x):
    try: return float(x)
    except (TypeError, ValueError): return 0.0

def fill(arms):
    data = {}
    for arm, prefix in arms.items():
        per = {}
        for s in SEEDS:
            p = f"{prefix}-default-{s}-1"
            names = {r["labelId"]: r["labelName"] for r in rows(f"{p}-label-directory.csv")}
            init = list(rows(f"{p}-scene-initialization-labels.csv"))
            routes = Counter()
            tier_of = {r["labelId"]: r["tier"] for r in init}
            for r in rows(f"{p}-scene-recruitment-start.csv"):
                if r["phase"] == "Initialization": routes[(tier_of.get(r["labelId"]), r["route"])] += 1
            per[s] = (names, init, routes)
        data[arm] = per
    for arm, per in data.items():
        print(f"\n== {arm} ==")
        for t in TIERS:
            tg = sum(int(r["target"]) for s in SEEDS for r in per[s][1] if r["tier"] == t)
            fl = sum(int(r["filled"]) for s in SEEDS for r in per[s][1] if r["tier"] == t)
            q = [f(r["meanTrueQuality"]) for s in SEEDS for r in per[s][1] if r["tier"] == t and int(r["filled"]) > 0]
            strong = sum(int(r["strongActs"]) for s in SEEDS for r in per[s][1] if r["tier"] == t)
            pool = st.mean(int(r["accessibleAny"]) for s in SEEDS for r in per[s][1] if r["tier"] == t)
            seeds = " ".join(f"{sum(int(r['filled']) for r in per[s][1] if r['tier'] == t) / max(1, sum(int(r['target']) for r in per[s][1] if r['tier'] == t)):.0%}" for s in SEEDS)
            print(f"{t:12} fill {fl}/{tg} = {fl / max(1, tg):.1%} (per seed {seeds})  meanQ {st.mean(q) if q else 0:.3f}  strong {strong}  leftover accessible {pool:.0f}")
        r_mid = Counter(); r_all = Counter()
        for s in SEEDS:
            for (t, route), n in per[s][2].items():
                r_all[route] += n
                if t == "MidTier": r_mid[route] += n
        print("MidTier routes: " + ", ".join(f"{k} {v}" for k, v in r_mid.most_common()))
        print("All routes:     " + ", ".join(f"{k} {v}" for k, v in r_all.most_common()))
        ny = [(s, r) for s in SEEDS for r in per[s][1] if r["tier"] == "MidTier" and r["hqPlaceId"] == "new_york"]
        print(f"New York MidTiers: {sum(int(r['filled']) for _, r in ny)}/{sum(int(r['target']) for _, r in ny)} over 3 seeds "
              f"(per seed {' '.join(str(sum(int(r['filled']) for s2, r in ny if s2 == s)) + '/' + str(sum(int(r['target']) for s2, r in ny if s2 == s)) for s in SEEDS)})")
        for n in NAMED:
            hits = [(s, r) for s in SEEDS for r in per[s][1] if per[s][0].get(r["labelId"], "").startswith(n)]
            if hits:
                print(f"  {n:14} " + "  ".join(f"{r['filled']}/{r['target']} q{f(r['meanTrueQuality']):.2f}" for _, r in hits))

def runtime(prefix):
    out = {}
    for s in SEEDS:
        p = f"{prefix}-default-{s}-53"
        tier = {r["labelId"]: r["initialTier"] for r in rows(f"{p}-label-directory.csv")}
        outcomes = defaultdict(Counter)
        for r in rows(f"{p}-daily-talent-appointments.csv"):
            if r["labelId"] in tier: outcomes[tier[r["labelId"]]][r["offerOutcome"]] += 1
        money = defaultdict(lambda: defaultdict(float)); firms = defaultdict(set)
        for r in rows(f"{p}-company-monthly.csv"):
            t = tier.get(r["labelId"])
            if t is None: continue
            firms[t].add(r["labelId"])
            for k in ("payroll", "artistAdvances", "recording", "recordGross", "pretaxEarnings"): money[t][k] += f(r[k])
        out[s] = (outcomes, money, {t: len(v) for t, v in firms.items()})
    return out

def rosters(arms):
    """Runtime signings after launch (daily talent appointments) and the year's costs by initial tier."""
    data = {arm: runtime(p) for arm, p in arms.items()}
    for arm, per in data.items():
        print(f"\n== {arm} ==")
        for t in TIERS:
            oc = Counter()
            for s in SEEDS: oc.update(per[s][0][t])
            signed = oc["AcceptedUncontested"] + oc["AcceptedArtistChoice"]
            n = sum(per[s][2].get(t, 0) for s in SEEDS)
            m = {k: sum(per[s][1][t][k] for s in SEEDS) / max(1, n) for k in ("payroll", "artistAdvances", "recording", "recordGross", "pretaxEarnings")}
            print(f"{t:12} signed {signed:4} (per seed {' '.join(str(per[s][0][t]['AcceptedUncontested'] + per[s][0][t]['AcceptedArtistChoice']) for s in SEEDS)})  "
                  f"noCandidate {oc['NoQualifyingCandidate']:4}  unaffordable {oc['EstimatedAdvanceUnaffordable']:4}  lostChoice {oc['LostArtistChoice']:3}  | per firm-year: "
                  + " ".join(f"{k} {v / 1000:.0f}k" for k, v in m.items()))

if __name__ == "__main__":
    mode, args = sys.argv[1], dict(a.split("=", 1) for a in sys.argv[2:])
    {"fill": fill, "rosters": rosters}[mode](args)
