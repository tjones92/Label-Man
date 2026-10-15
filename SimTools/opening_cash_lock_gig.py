"""Early-game directive V2 Phase 5B (F5): residency against none with the flat road inflow removed, paired by seed,
town and opening.

    py SimTools/opening_cash_lock_gig.py TAG  >  SimLogs/ocl-TAG-gig.md

Report-only. Reads SimLogs/ocl-TAG-*-opening.json from MATRIX=gig. A residency is now booked nights on a room's bill; the
label's commission comes out of each played set's fee. Paired runs share a world seed, not a world: the player's own
actions move the shared stream, so a paired difference is the plan plus that drift.
"""
import glob, json, sys, statistics

tag = sys.argv[1]
runs = {}
for path in sorted(glob.glob(f"SimLogs/ocl-{tag}-*-opening.json")):
    with open(path, encoding="utf-8") as f:
        d = json.load(f)
    p = d.get("prepared") or {}
    recs = [r for r in (d.get("records") or []) if r]
    seq = d.get("sequence") or "batch"
    runs[(d["seed"], d["town"], d["policy"], seq, d["road"])] = dict(
        status=d["status"], closed=d["closed"], closureDay=d["closureDay"], final=d["finalCash"],
        units=sum(r.get("unitsSold") or 0 for r in recs), singles=len(recs),
        asks=p.get("residencyAsks", 0), won=p.get("residenciesWon", 0),
        booked=p.get("residencyNightsBooked", 0), played=p.get("residencyNightsPlayed", 0),
        nights=p.get("commissionNights", 0), commission=p.get("commissionsCollected", 0.0),
        bookingsWon=p.get("bookingsWon", 0), violations=len(d.get("violations") or []))

print(f"# Gig pay (F5) `{tag}` ({len(runs)} runs)\n")
print(f"Probe failures: {sum(r['status'] != 'done' for r in runs.values())}; invariant violations: {sum(r['violations'] for r in runs.values())}\n")
print("| seed | town | opening | road | residency asks/won | residency nights booked/played | paid nights | commission $ | units | final $ | closed |")
print("|---|---|---|---|---|---|---|---|---|---|---|")
for k in sorted(runs):
    r = runs[k]
    print(f"| {k[0]} | {k[1]} | {k[2]} {k[3]} | {k[4]} | {r['asks']}/{r['won']} | {r['booked']}/{r['played']} | {r['nights']} | "
          f"{r['commission']:.2f} | {r['units']} | {r['final']:,.0f} | {r['closureDay'] if r['closed'] else '-'} |")

res = [r for k, r in runs.items() if k[4] == "residency"]
won = [r for r in res if r["won"] > 0]
print(f"\n## Residency arm\n")
print(f"- {len(won)}/{len(res)} runs won a residency ({sum(r['won'] for r in res)} from {sum(r['asks'] for r in res)} asks).")
if won:
    weekly = [r["commission"] / max(1, r["played"]) * 3 for r in won if r["played"] > 0]
    print(f"- Nights booked {sum(r['booked'] for r in won)}, played {sum(r['played'] for r in won)}; commission per run median "
          f"${statistics.median([r['commission'] for r in won]):.2f} (range ${min(r['commission'] for r in won):.2f}-${max(r['commission'] for r in won):.2f}).")
    if weekly:
        print(f"- Per played night x3 (a residency week): median ${statistics.median(weekly):.2f}/week.")

for road in ("none", "residency"):
    arm = [r for k, r in runs.items() if k[4] == road]
    print(f"- Closures, {road}: {sum(r['closed'] for r in arm)}/{len(arm)}.")

pairs = [k[:4] for k in runs if k[4] == "none" and (*k[:4], "residency") in runs]
print(f"\n## Paired differences, residency minus none ({len(pairs)} pairs)\n")
print("| seed | town | opening | commission $ | units | final cash | closed (none / residency) |")
print("|---|---|---|---|---|---|---|")
dc = []
for k in sorted(pairs):
    a, b = runs[(*k, "none")], runs[(*k, "residency")]
    dc.append(b["final"] - a["final"])
    print(f"| {k[0]} | {k[1]} | {k[2]} {k[3]} | {b['commission']:.2f} | {b['units'] - a['units']:+d} | {b['final'] - a['final']:+,.0f} | "
          f"{a['closureDay'] if a['closed'] else '-'} / {b['closureDay'] if b['closed'] else '-'} |")
if dc:
    print(f"\nMedian paired final cash {statistics.median(dc):+,.0f}; residency ahead in {sum(x > 0 for x in dc)}/{len(dc)} pairs.")
