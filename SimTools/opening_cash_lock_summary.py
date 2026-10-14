"""Early-game directive V2 Phase 0: summarise an opening cash-lock matrix (SimTools/run-opening-cash-lock.sh).

    py SimTools/opening_cash_lock_summary.py TAG  >  SimLogs/ocl-TAG-summary.md

Report-only. Reads SimLogs/ocl-TAG-*-opening.json and prints markdown tables: per-run outcomes, then rollups by
town x policy and archetype. Two seeds are smoke coverage (directive §10), not an incidence estimate.
"""
import glob, json, sys, collections

tag = sys.argv[1]
rows = []
for path in sorted(glob.glob(f"SimLogs/ocl-{tag}-*-opening.json")):
    with open(path, encoding="utf-8") as f:
        d = json.load(f)
    def cash(day):
        r = d.get(f"day{day}")
        return None if r is None else r["cash"]
    unfunded = [a for a in d["actions"] if a["action"] == "unfunded"]
    sold = sum(int(a["message"].split(" took ")[1].split(" ")[0]) for a in d["actions"]
               if a["action"] == "pitch" and a["ok"] and " took " in (a["message"] or ""))
    last = d["daily"][-1] if d["daily"] else {}
    rows.append(dict(
        seed=d["seed"], town=d["town"], arch=d["archetype"], policy=d["policy"], road=d["road"], status=d["status"],
        start=d["startCash"], overhead=d["overhead"], signed=d["signed"], sessions=d["sessionsDone"],
        pressed=d["singlesPressed"], release=d["firstReleaseDay"], preL1=d["firstPreL1Day"], l1=d["firstL1Day"],
        noRoute=d["firstNoRouteDay"], closed=d["closed"], closureDay=d["closureDay"], closure=d["closure"],
        c30=cash(30), c60=cash(60), c90=cash(90), final=d["finalCash"], red=last.get("red"),
        unfunded=",".join(sorted({u["stage"] for u in unfunded})), pitchedCopies=sold,
        stockout=d["stockoutDays"], repeats=d["repeats"], routes=d["firstL1Routes"]))

def f(v, w=0):
    if v is None: return "-"
    if isinstance(v, float): return f"{v:,.0f}"
    return str(v)

print(f"# Opening cash-lock matrix `{tag}` ({len(rows)} runs)\n")
fails = [r for r in rows if r["status"] != "done"]
print(f"Probe failures: {len(fails)}\n")
print("| seed | town | archetype | policy | road | start | o/h | signed | singles | release d | pre-L1 d | L1 d | closed d | cash d30 | d60 | d90 | final | red | unfunded |")
print("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|")
for r in rows:
    print(f"| {r['seed']} | {r['town']} | {r['arch']} | {r['policy']} | {r['road']} | {f(r['start'])} | {f(r['overhead'])} | {r['signed']} | {r['pressed']} | "
          f"{f(r['release'])} | {f(r['preL1'])} | {f(r['l1'])} | {f(r['closureDay'])} | {f(r['c30'])} | {f(r['c60'])} | {f(r['c90'])} | {f(r['final'])} | {f(r['red'])} | {r['unfunded'] or '-'} |")

def rollup(key):
    g = collections.defaultdict(list)
    for r in rows:
        if r["road"] == "none": g[key(r)].append(r)
    print("\n| group | runs | released | pre-L1 | L1 | closed by d180 | median final cash |")
    print("|---|---|---|---|---|---|---|")
    for k in sorted(g):
        rs = g[k]; finals = sorted(r["final"] for r in rs)
        print(f"| {k} | {len(rs)} | {sum(r['release'] is not None for r in rs)} | {sum(r['preL1'] is not None for r in rs)} | "
              f"{sum(r['l1'] is not None for r in rs)} | {sum(r['closed'] for r in rs)} | {finals[len(finals)//2]:,.0f} |")

print("\n## By town x policy"); rollup(lambda r: f"{r['town']} / {r['policy']}")
print("\n## By archetype"); rollup(lambda r: r["arch"])
print("\n## By policy"); rollup(lambda r: r["policy"])

res = [r for r in rows if r["road"] == "residency"]
if res:
    print("\n## Two-residency fixture vs no live income (ExMusician, eager)")
    print("| seed | town | residency final | closed d | no-road final | closed d |")
    print("|---|---|---|---|---|---|")
    for r in res:
        base = next((b for b in rows if b["road"] == "none" and b["seed"] == r["seed"] and b["town"] == r["town"]
                     and b["arch"] == "ExMusician" and b["policy"] == "eager"), None)
        print(f"| {r['seed']} | {r['town']} | {f(r['final'])} | {f(r['closureDay'])} | {f(base and base['final'])} | {f(base and base['closureDay'])} |")

print("\n## First L1 / pre-L1 recovery routes (§1.2 L1b)")
for r in rows:
    if r["routes"]:
        print(f"- {r['seed']} {r['town']} {r['arch']} {r['policy']} {r['road']}: "
              f"exec {r['routes']['Executable']}, spec {r['routes']['Speculative']}, blocked {r['routes']['Blocked']} -- "
              + "; ".join(r["routes"]["Lines"][:4]))
print("\n## Closures")
for r in rows:
    if r["closed"]: print(f"- {r['seed']} {r['town']} {r['arch']} {r['policy']} {r['road']} day {r['closureDay']}: {r['closure']}")

# Closures earned or imposed? (author, 2026-10-14): outcomes by the record's TRUE hook percentile among that
# year's singles. If top-quartile records fail about as often as bottom-quartile ones, demand is unrealistic;
# if failures cluster in weak records, closures are earned and should stay.
truth = []
for path in sorted(glob.glob(f"SimLogs/ocl-{tag}-*-opening.json")):
    with open(path, encoding="utf-8") as fh:
        d = json.load(fh)
    for rec in d.get("records") or []:
        truth.append(dict(policy=d["policy"], town=d["town"], closed=d["closed"], final=d["finalCash"],
                          hookPct=rec["hookPct"], sold=rec.get("unitsSold"), breakEven=rec["firstRunBreakEvenCopies"]))
if truth:
    print("\n## Outcome by true record quality (probe truth, never shown to the policy)")
    print("| hook quartile | singles | label closed | sold >= break-even | median copies sold |")
    print("|---|---|---|---|---|")
    for lo, hi, name in [(0, .25, "bottom"), (.25, .5, "2nd"), (.5, .75, "3rd"), (.75, 1.01, "top")]:
        rs = [t for t in truth if t["hookPct"] is not None and lo <= t["hookPct"] < hi]
        if not rs: continue
        sold = sorted(t["sold"] or 0 for t in rs)
        print(f"| {name} | {len(rs)} | {sum(t['closed'] for t in rs)} | {sum((t['sold'] or 0) >= t['breakEven'] for t in rs)} | {sold[len(sold)//2]} |")
