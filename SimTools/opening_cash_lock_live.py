"""Early-game directive V2 Phase 3 (F1-F3): live-first preparation against waiting, paired by seed and town.

    py SimTools/opening_cash_lock_live.py TAG  >  SimLogs/ocl-TAG-live.md

Report-only. Reads SimLogs/ocl-TAG-*-opening.json from MATRIX=live (discerning act, every channel worked). Pairs the
`none` and `live` runs of each seed x town. The two runs share a world seed but not a world: the player's own actions
move the shared stream, so a paired difference is the plan plus that drift, not the plan alone.
"""
import glob, json, sys, collections, statistics

tag = sys.argv[1]
runs = {}
for path in sorted(glob.glob(f"SimLogs/ocl-{tag}-*-opening.json")):
    with open(path, encoding="utf-8") as f:
        d = json.load(f)
    p = d["prepared"]
    recs = d.get("records") or []
    units = sum(r.get("unitsSold") or 0 for r in recs if r)
    breakeven = sum(1 for r in recs if r and (r.get("unitsSold") or 0) >= (r.get("firstRunBreakEvenCopies") or 1e9))
    hook = statistics.median([r["hookPct"] for r in recs if r and r.get("hookPct") is not None] or [float("nan")])
    tables = [a for a in d["actions"] if a["action"] == "room-table" and a["ok"]]
    cut = sum(float(a["message"].split("the room took $")[1].rstrip(".")) for a in tables if "the room took $" in (a["message"] or ""))
    follow = max([x["home"] for x in p.get("following") or []] or [0])
    lift = max([x["lift"] for x in p.get("following") or []] or [0])
    runs[(d["seed"], d["town"], d["prep"])] = dict(
        status=d["status"], closed=d["closed"], closureDay=d["closureDay"], final=d["finalCash"],
        c90=(d.get("day90") or {}).get("cash"), units=units, breakeven=breakeven, singles=len([r for r in recs if r]), hook=hook,
        bookings=p.get("bookingsWon", 0), played=p.get("bookingsPerformed", 0), fell=p.get("bookingsFellThrough", 0),
        asks=p.get("tableAsks", 0), rights=p.get("tableRightsWon", 0), nights=p.get("tableNights", 0),
        own=p.get("tableNightsOwnAct", 0), cancelled=p.get("tableNightsCancelled", 0), empty=p.get("tableNightsEmpty", 0),
        tableCopies=p.get("tableCopies", 0), cut=cut, follow=follow, lift=lift,
        channels=d.get("channelActs") or {}, violations=len(d.get("violations") or []))

def f(v, nd=0):
    if v is None: return "-"
    if isinstance(v, float): return f"{v:,.{nd}f}"
    return str(v)

print(f"# Live-first vs waiting `{tag}` ({len(runs)} runs)\n")
print(f"Probe failures: {sum(r['status'] != 'done' for r in runs.values())}; invariant violations: {sum(r['violations'] for r in runs.values())}\n")
print("| seed | town | plan | hook pct | units | break-even | bookings won/played/fell | table asks/rights | table nights (own, empty, cancelled) | table copies | room cut $ | following | lift | cash d90 | final | closed |")
print("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|")
for k in sorted(runs):
    r = runs[k]
    print(f"| {k[0]} | {k[1]} | {k[2]} | {f(r['hook'],2)} | {r['units']} | {r['breakeven']}/{r['singles']} | {r['bookings']}/{r['played']}/{r['fell']} | "
          f"{r['asks']}/{r['rights']} | {r['nights']} ({r['own']}, {r['empty']}, {r['cancelled']}) | {r['tableCopies']} | {f(r['cut'],2)} | "
          f"{f(r['follow'],3)} | {f(r['lift'],3)} | {f(r['c90'])} | {f(r['final'])} | {r['closureDay'] if r['closed'] else '-'} |")

pairs = [(k[0], k[1]) for k in runs if k[2] == "none" and (k[0], k[1], "live") in runs]
print(f"\n## Paired differences, live minus none ({len(pairs)} pairs)\n")
print("| seed | town | units | table copies | following | cash d90 | final cash |")
print("|---|---|---|---|---|---|---|")
du, df = [], []
for s, t in sorted(pairs):
    a, b = runs[(s, t, "none")], runs[(s, t, "live")]
    du.append(b["units"] - a["units"]); df.append(b["final"] - a["final"])
    print(f"| {s} | {t} | {b['units'] - a['units']:+d} | {b['tableCopies'] - a['tableCopies']:+d} | {b['follow'] - a['follow']:+.3f} | "
          f"{(b['c90'] or 0) - (a['c90'] or 0):+,.0f} | {b['final'] - a['final']:+,.0f} |")
if pairs:
    print(f"\nMedian paired units {statistics.median(du):+.0f}, final cash {statistics.median(df):+,.0f}; live ahead on units in "
          f"{sum(x > 0 for x in du)}/{len(du)} pairs, on cash in {sum(x > 0 for x in df)}/{len(df)}.")

tot = collections.Counter()
for r in runs.values():
    for ch, n in r["channels"].items(): tot[ch] += n
print("\nChannel acts (all runs): " + ", ".join(f"{k} {v}" for k, v in tot.most_common()))
