"""Early-game directive V2 Phase 2 (§4.3): the backer matrix (MATRIX=backer SimTools/run-opening-cash-lock.sh TAG).

    py SimTools/opening_cash_lock_backer.py TAG  >  SimLogs/ocl-TAG-backer.md

Report-only. Pairs each --backer=ask run with its --backer=none twin (same seed, town, policy, sequence) and reports
closures, closure day, final cash, and the loan's own outcome (principal, rate, paid, missed, arrears, standing).
Paired runs share a seed, not a world: the meeting's two hours and the extra cash move the shared stream, so read
direction, not size.
"""
import glob, json, sys, collections
sys.stdout.reconfigure(encoding="utf-8")

tag = sys.argv[1]
runs = {}
for path in sorted(glob.glob(f"SimLogs/ocl-{tag}-*-opening.json")):
    with open(path, encoding="utf-8") as f:
        d = json.load(f)
    b = d.get("backer") or {}
    key = (d["seed"], d["town"], d["policy"], d["sequence"])
    arm = b.get("plan", "none")
    units = sum((r.get("unitsSold") or 0) for r in d.get("records", []))
    runs.setdefault(key, {})[arm] = dict(
        closed=d["closed"], day=d["closureDay"], final=d["finalCash"], units=units, c60=(d.get("day60") or {}).get("cash"),
        principal=b.get("principal"), rate=b.get("rate"), signed=b.get("signedDay"), paid=b.get("principalPaid"),
        interest=b.get("interestPaid"), missed=b.get("missed"), arrears=b.get("arrears"), standing=b.get("standing"),
        outstanding=b.get("outstanding"), agrees=b.get("ledgerAgrees"), refusals=b.get("refusals"))

def fmt(v, p=0):
    return "-" if v is None else (f"{v:.{p}f}" if isinstance(v, float) else str(v))

print(f"# Backer matrix `{tag}` ({sum(len(v) for v in runs.values())} runs, {len(runs)} pairs)\n")
print("| seed | town | policy | seq | closed none→ask | day none→ask | final $ none→ask | units none→ask | loan | signed day | principal paid | interest paid | missed | arrears | standing | ledger |")
print("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|")
for key in sorted(runs):
    n, a = runs[key].get("none"), runs[key].get("ask")
    if not n or not a:
        continue
    loan = "-" if a["principal"] is None else f"${a['principal']:.0f} @ {a['rate']:.0%}"
    print(f"| {key[0]} | {key[1]} | {key[2]} | {key[3]} | {n['closed']}→{a['closed']} | {fmt(n['day'])}→{fmt(a['day'])} | "
          f"{fmt(n['final'])}→{fmt(a['final'])} | {n['units']}→{a['units']} | {loan} | {fmt(a['signed'])} | {fmt(a['paid'], 2)} | "
          f"{fmt(a['interest'], 2)} | {fmt(a['missed'])} | {fmt(a['arrears'], 2)} | {fmt(a['standing'], 2)} | {fmt(a['agrees'])} |")

print("\n## Rollup by town x policy\n")
print("| town | policy | pairs | closed none | closed ask | later closure | saved | loans signed | any payment missed | fully current at end |")
print("|---|---|---|---|---|---|---|---|---|---|")
groups = collections.defaultdict(list)
for key, arms in runs.items():
    if "none" in arms and "ask" in arms:
        groups[(key[1], key[2])].append(arms)
for (town, policy), pairs in sorted(groups.items()):
    cn = sum(p["none"]["closed"] for p in pairs)
    ca = sum(p["ask"]["closed"] for p in pairs)
    later = sum(1 for p in pairs if p["none"]["closed"] and p["ask"]["closed"] and p["ask"]["day"] > p["none"]["day"])
    saved = sum(1 for p in pairs if p["none"]["closed"] and not p["ask"]["closed"])
    signed = sum(1 for p in pairs if p["ask"]["principal"] is not None)
    missed = sum(1 for p in pairs if (p["ask"]["missed"] or 0) > 0)
    current = sum(1 for p in pairs if p["ask"]["principal"] is not None and (p["ask"]["arrears"] or 0) < 0.01)
    print(f"| {town} | {policy} | {len(pairs)} | {cn} | {ca} | {later} | {saved} | {signed} | {missed} | {current} |")

all_pairs = [p for ps in groups.values() for p in ps]
print(f"\nTotals: closures {sum(p['none']['closed'] for p in all_pairs)} without a backer, "
      f"{sum(p['ask']['closed'] for p in all_pairs)} with; "
      f"newly closed with a backer: {sum(1 for p in all_pairs if p['ask']['closed'] and not p['none']['closed'])}; "
      f"ledgers agreeing: {sum(1 for p in all_pairs if p['ask']['agrees'])}/{sum(1 for p in all_pairs if p['ask']['principal'] is not None)}.")
