"""Early-game directive V2: is a strong record treated fairly? Summarise a `fair` opening matrix
(MATRIX=fair SimTools/run-opening-cash-lock.sh TAG), where every selling channel is worked.

    py SimTools/opening_cash_lock_fairness.py TAG  >  SimLogs/ocl-TAG-fairness.md

Report-only. Reads SimLogs/ocl-TAG-*-opening.json. Record quality is probe TRUTH (hook and production percentile
among that year's singles), never shown to the policy. Break-even is the first run's cost at the dealer price.
"""
import glob, json, sys, collections, math

import numpy as np

tag = sys.argv[1]
runs, recs = [], []
for path in sorted(glob.glob(f"SimLogs/ocl-{tag}-*-opening.json")):
    with open(path, encoding="utf-8") as fh:
        d = json.load(fh)
    acts = d["actions"]
    runs.append(dict(
        seed=d["seed"], town=d["town"], policy=d["policy"], seq=d.get("sequence", "batch"), road=d["road"], closed=d["closed"],
        resAsks=(d.get("prepared") or {}).get("residencyAsks") or 0, resWon=(d.get("prepared") or {}).get("residenciesWon") or 0,
        nights=(d.get("prepared") or {}).get("bookingsPerformed") or 0,
        closureDay=d["closureDay"], final=d["finalCash"], signed=d["signed"], sessions=d["sessionsDone"],
        pressed=d["singlesPressed"], release=d["firstReleaseDay"], stockout=d["stockoutDays"] or 0,
        unpressFirst=d.get("firstUnpressableDay"), unpressDays=d.get("unpressableDays") or 0,
        represses=sum(a["action"] in ("press-to-fill", "repress") and a["ok"] for a in acts),
        unfunded=collections.Counter(a["stage"] for a in acts if a["action"] == "unfunded"),
        trips=d.get("roadTrips") or 0, channels=d.get("channelActs") or {},
        minCash=min((x["cash"] for x in d["daily"]), default=None), red=max((x["red"] for x in d["daily"]), default=0)))
    for r in d.get("records") or []:
        recs.append(dict(policy=d["policy"], town=d["town"], seed=d["seed"], seq=d.get("sequence", "batch"), closed=d["closed"],
                         hook=r["hookStrength"], prod=r["productionQuality"], hookPct=r["hookPct"], prodPct=r["productionPct"],
                         sold=r.get("unitsSold") or 0, pressed=r.get("pressed") or 0, breakEven=r["firstRunBreakEvenCopies"]))


def f(v):
    if v is None: return "-"
    if isinstance(v, float): return f"{v:,.0f}"
    return str(v)


def med(xs):
    xs = sorted(xs)
    return xs[len(xs) // 2] if xs else None


print(f"# Fairness matrix `{tag}` ({len(runs)} runs, {len(recs)} pressed singles)\n")
print("Every channel worked: home shops and ops (pitch, consign, service, returns), hop tables, the one-stop, station drops,")
print("a weekly trip to one of the three nearest towns, and a repress when a title sells out with calls open or half a run gone.\n")

print("## Outcomes by policy\n")
print("| policy | order | road | runs | released | closed by d180 | median final cash | min cash (median) | represses (median) | stockout days (median) | days a finished single sat unpressable |")
print("|---|---|---|---|---|---|---|---|---|---|---|")
g = collections.defaultdict(list)
for r in runs: g[(r["policy"], r["seq"], r["road"])].append(r)
for (pol, seq, road), rs in sorted(g.items()):
    print(f"| {pol} | {seq} | {road} | {len(rs)} | {sum(r['release'] is not None for r in rs)} | {sum(r['closed'] for r in rs)} | {f(med([r['final'] for r in rs]))} | "
          f"{f(med([r['minCash'] for r in rs]))} | {med([r['represses'] for r in rs])} | {med([r['stockout'] for r in rs])} | "
          f"{sum(r['unpressDays'] for r in rs)} over {sum(r['unpressDays'] > 0 for r in rs)} runs |")

print("\n## Outcomes by town (all policies)\n")
print("| town | runs | released | closed | median final cash | median copies per single |")
print("|---|---|---|---|---|---|")
for town in sorted({r["town"] for r in runs}):
    rs = [r for r in runs if r["town"] == town]; ss = [x["sold"] for x in recs if x["town"] == town]
    print(f"| {town} | {len(rs)} | {sum(r['release'] is not None for r in rs)} | {sum(r['closed'] for r in rs)} | {f(med([r['final'] for r in rs]))} | {f(med(ss))} |")

print("\n## Sales by true record quality\n")
print("| hook quartile | singles | median hook | median production pct | median copies sold | sold >= first-run break-even | on a label that closed |")
print("|---|---|---|---|---|---|---|")
for lo, hi, name in [(0, .25, "bottom"), (.25, .5, "2nd"), (.5, .75, "3rd"), (.75, 1.01, "top")]:
    rs = [t for t in recs if lo <= t["hookPct"] < hi]
    if not rs: print(f"| {name} | 0 | | | | | |"); continue
    print(f"| {name} | {len(rs)} | {med([t['hook'] for t in rs]):.2f} | {med([t['prodPct'] for t in rs]):.2f} | {med([t['sold'] for t in rs])} | "
          f"{sum(t['sold'] >= t['breakEven'] for t in rs)} | {sum(t['closed'] for t in rs)} |")

if len(recs) >= 8:
    # How much of the spread in copies is hook and how much production (log copies on the two raw values, one town each).
    towns = sorted({t["town"] for t in recs})
    X = np.array([[t["hook"], t["prod"]] + [1.0 if t["town"] == tw else 0.0 for tw in towns] for t in recs])
    y = np.array([math.log1p(t["sold"]) for t in recs])
    coef, *_ = np.linalg.lstsq(X, y, rcond=None)
    pred = X @ coef; r2 = 1 - ((y - pred) ** 2).sum() / ((y - y.mean()) ** 2).sum()
    sd_h, sd_p = np.std([t["hook"] for t in recs]), np.std([t["prod"] for t in recs])
    print(f"\nlog(copies) ~ hook + production + town: hook {coef[0]:+.2f}/unit, production {coef[1]:+.2f}/unit, R² {r2:.2f}.")
    print(f"One SD of hook ({sd_h:.2f}) moves copies x{math.exp(coef[0] * sd_h):.2f}; one SD of production ({sd_p:.2f}) moves them x{math.exp(coef[1] * sd_p):.2f}.")
    print(f"Production range seen: {min(t['prod'] for t in recs):.2f}-{max(t['prod'] for t in recs):.2f}; hook range {min(t['hook'] for t in recs):.2f}-{max(t['hook'] for t in recs):.2f}.")

print("\n## Channel mix (successful actions, all runs)\n")
tot = collections.Counter()
for r in runs: tot.update(r["channels"])
print(", ".join(f"{k} {v}" for k, v in tot.most_common()))

print("\n## Playtest order: second single cut after the first is out\n")
print("| seed | town | policy | sessions | singles pressed | first day unpressable | days unpressable | unfunded (stage x weeks) | represses | min cash | closed d | final |")
print("|---|---|---|---|---|---|---|---|---|---|---|---|")
for r in runs:
    if r["seq"] != "staggered": continue
    uf = ", ".join(f"{k} x{v}" for k, v in r["unfunded"].items()) or "-"
    print(f"| {r['seed']} | {r['town']} | {r['policy']} | {r['sessions']} | {r['pressed']} | {f(r['unpressFirst'])} | {r['unpressDays']} | {uf} | {r['represses']} | {f(r['minCash'])} | {f(r['closureDay'])} | {f(r['final'])} |")

res = [r for r in runs if r["road"] == "residency"]
if res:
    print("\n## Residencies through a booker\n")
    print("| town | runs | booked nights played (sum) | residency asks | residencies won | closed |")
    print("|---|---|---|---|---|---|")
    for town in sorted({r["town"] for r in res}):
        rs = [r for r in res if r["town"] == town]
        print(f"| {town} | {len(rs)} | {sum(r['nights'] for r in rs)} | {sum(r['resAsks'] for r in rs)} | {sum(r['resWon'] for r in rs)} | {sum(r['closed'] for r in rs)} |")

print("\n## Closures\n")
for r in runs:
    if r["closed"]: print(f"- {r['seed']} {r['town']} {r['policy']} {r['seq']} {r['road']}: day {r['closureDay']}, min cash {f(r['minCash'])}, represses {r['represses']}")
