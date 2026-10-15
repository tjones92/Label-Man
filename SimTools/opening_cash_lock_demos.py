"""Early-game directive V2 Phase 4 (§7.1): the paid-demo matrix (MATRIX=demo SimTools/run-opening-cash-lock.sh TAG).

    py SimTools/opening_cash_lock_demos.py TAG  >  SimLogs/ocl-TAG-demos.md

Report-only. Pairs each --demos=work run with its --demos=none twin (same seed, town, policy) and reports the demo
trade (pitches, bookings, deliveries, misses, fees by month) beside closure, final cash and units. The balance
diagnostic of §7.1 compares the monthly fee with 30-60% of the town's overhead; it is a reading, not a target.
"""
import glob, json, sys

sys.stdout.reconfigure(encoding="utf-8")
tag = sys.argv[1]
runs = {}
for path in sorted(glob.glob(f"SimLogs/ocl-{tag}-*-opening.json")):
    with open(path, encoding="utf-8") as f:
        d = json.load(f)
    m = d.get("demos") or {}
    units = sum((r.get("unitsSold") or 0) for r in d.get("records", []))
    runs.setdefault((d["seed"], d["town"], d["policy"]), {})[m.get("plan", "none")] = dict(
        closed=d["closed"], final=d["finalCash"], units=units, overhead=d["overhead"], days=len(d.get("daily") or []), **m)

print(f"# Paid-demo matrix `{tag}` ({sum(len(v) for v in runs.values())} runs)\n")
print("| seed | town | closed none→work | final $ none→work | units none→work | nights | pitches | booked | delivered | missed | client off | fees | fee/month | vs overhead | blocks |")
print("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|")
tot = dict(pitches=0, booked=0, delivered=0, missed=0, fees=0.0, months=0.0)
for key in sorted(runs):
    n, w = runs[key].get("none"), runs[key].get("work")
    if not n or not w:
        continue
    months = max(1.0, w["days"] / 30.4)
    per = w["fees"] / months
    blocks = ", ".join(f"{k} {v}" for k, v in sorted(w["blocks"].items(), key=lambda kv: -kv[1]))
    print(f"| {key[0]} | {key[1]} | {n['closed']}→{w['closed']} | {n['final']:.0f}→{w['final']:.0f} | {n['units']}→{w['units']} | {w['nightsTried']} | "
          f"{w['pitches']} | {w['booked']} | {w['delivered']} | {w['missed']} | {w['clientCancelled']} | ${w['fees']:.0f} | ${per:.1f} | "
          f"{per / w['overhead']:.0%} of ${w['overhead']:.0f} | {blocks} |")
    for k in ("pitches", "booked", "delivered", "missed"):
        tot[k] += w[k]
    tot["fees"] += w["fees"]; tot["months"] += months
print(f"\nTotals: {tot['pitches']} pitches, {tot['booked']} booked, {tot['delivered']} delivered, {tot['missed']} missed; "
      f"${tot['fees']:.0f} in fees over {tot['months']:.1f} run-months = ${tot['fees'] / max(1, tot['months']):.1f}/month.")
