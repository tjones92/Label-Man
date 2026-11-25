"""Empty-day metric (inner-life directive §6, Phase 0) from OpeningCashLockRunner JSON.

A day is EMPTY when the scripted player took no successful action with a sim consequence. Travel and logged refusals
don't count. A day is SIDE-ONLY when its only consequential actions are side income (demo service, booker pitches),
not label work. Usage: python SimTools/analyze_empty_days.py SimLogs/empty-*-opening.json
"""
import json, sys, glob, collections

TRAVEL = {"found", "drive-home", "drive-to"}
REFUSED_OK = {"residency-refused", "table-rights-refused"}
SIDE = {"demo-pitch", "demo-produce", "booker-pitch"}

def classify(path):
    d = json.load(open(path))
    days = len(d["daily"])
    by_day = collections.defaultdict(list)
    for a in d["actions"]:
        if a.get("ok") and a["action"] not in TRAVEL and a["action"] not in REFUSED_OK:
            by_day[a["day"]].append(a["action"])
    stages = {x["day"]: x["stage"] for x in d["daily"]}
    empty, side, streak, best = [], [], 0, 0
    for day in range(days):
        acts = by_day.get(day, [])
        if not acts:
            empty.append(day); streak += 1; best = max(best, streak)
        else:
            streak = 0
            if all(a in SIDE for a in acts): side.append(day)
    per_stage = collections.Counter(stages.get(x, "?") for x in empty)
    weeks26 = [x for x in empty if x < 182]
    months = collections.Counter(x // 30 for x in empty)
    return dict(run=d["run"], archetype=d["archetype"], town=d["town"], policy=d["policy"], sell=d["sell"], days=days,
                empty=len(weeks26), side_only=len([x for x in side if x < 182]), longest=best,
                by_stage=dict(per_stage), by_month=[months.get(m, 0) for m in range(7)],
                release=d.get("firstReleaseDay"), sale=d.get("firstSaleDay"), cash=round(d.get("finalCash") or 0))

rows = [classify(p) for a in sys.argv[1:] for p in sorted(glob.glob(a))]
print(f"{'run':44} {'days':>4} {'empty':>5} {'side':>4} {'streak':>6}  release sale  cash   empty by 30-day month   empty by stage")
for r in rows:
    print(f"{r['run']:44} {r['days']:>4} {r['empty']:>5} {r['side_only']:>4} {r['longest']:>6}  {str(r['release']):>7} {str(r['sale']):>4} {r['cash']:>5}   {r['by_month']}   {r['by_stage']}")
for cfg in sorted({r['run'].rsplit('-', 1)[-1] for r in rows}):
    sub = [r for r in rows if r['run'].endswith(cfg)]
    if sub:
        e = sorted(r['empty'] for r in sub)
        print(f"{cfg}: n={len(sub)} empty days median {e[len(e)//2]} range {e[0]}-{e[-1]} of 182; side-only median {sorted(r['side_only'] for r in sub)[len(sub)//2]}")
