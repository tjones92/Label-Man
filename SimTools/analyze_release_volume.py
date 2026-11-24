"""Release-volume sizing (FanbaseEfficacy follow-up, 2026-11-10). Read-only. Compares runs over a common week window:
singles issued (all, by tier), signed acts and singles per act, the share of singles demand that clears (r), the
share of singles that lose money, label exits by tier, Major share of Top 10 chart weeks, the largest single week, and
the median No. 1 week. A longer run truncated to the window is a valid control (runs replay row for row).
Usage: python SimTools/analyze_release_volume.py MAX_WEEK RUN [RUN ...]"""
import sys, os, collections, statistics
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from veteran_demand_common import rows as _rows

def rows(name, kind):
    """Complete rows only: a run still writing its exports can end on a cut-off line."""
    return (r for r in _rows(name, kind) if None not in r.values() and None not in r)

def main(maxweek, name):
    out = collections.defaultdict(dict)
    tier = {}
    acts = collections.defaultdict(lambda: [0, 0])
    for r in rows(name, 'act-release-years'):
        y = int(r['year'])
        tier[(y, r['labelId'].strip('"'))] = r['tier'].strip('"')
        if int(r['signedWeeks']) >= 26: acts[y][0] += 1; acts[y][1] += int(r['singles'])
    issued = collections.Counter(); bytier = collections.Counter()
    for r in rows(name, 'single-launch-attribution'):
        w = int(r['week'])
        if w > maxweek: continue
        y = int(r['year']); issued[y] += 1; bytier[(y, tier.get((y, r['labelId'].strip('"')), '?'))] += 1
    clr = collections.defaultdict(lambda: [0.0, 0.0])
    for r in rows(name, 'market-clearing-weekly'):
        if int(r['week']) > maxweek: continue
        y = int(r['year']); clr[y][0] += float(r['clearedSingleUnits']); clr[y][1] += float(r['serviceableSingleIntent'])
    loss = collections.defaultdict(lambda: [0, 0])
    for r in rows(name, 'release-outcomes'):
        if int(r['week']) > maxweek or r['format'].strip('"') != 'Single': continue
        y = int(r['year']); loss[y][0] += 1; loss[y][1] += float(r['realizedNet'] or 0) < 0
    t10 = collections.defaultdict(collections.Counter); maxu = collections.Counter(); no1 = collections.defaultdict(list)
    for r in rows(name, 'records'):
        w = int(r['week'])
        if w > maxweek: continue
        y = int(r['year']); p = int(r['currentPosition']); u = int(r['unitsThisWeek'])
        maxu[y] = max(maxu[y], u)
        if 1 <= p <= 10: t10[y][r['labelTier'].strip('"')] += 1
        if p == 1: no1[y].append(u)
    exits = collections.Counter()
    seen = {}
    for r in rows(name, 'label-finance'):
        if int(r['week']) > maxweek: continue
        if r['exitReason'].strip('"') not in ('', 'None') and r['labelId'] not in seen:
            seen[r['labelId']] = 1; exits[(int(r['year']), r['labelTier'].strip('"'))] += 1
    print(f'\n== {name} (weeks <= {maxweek})')
    for y in sorted(issued):
        a, s = acts.get(y, [0, 0]); c = clr[y]; l = loss[y]; t = t10[y]; nt = sum(t.values()) or 1
        print(f"{y}: singles {issued[y]:5d} (Major {bytier[(y, 'Major')]}, MidTier {bytier[(y, 'MidTier')]}, other {issued[y] - bytier[(y, 'Major')] - bytier[(y, 'MidTier')]})"
              f" | signed acts {a} singles/act {s / a if a else 0:.2f} | cleared share {c[0] / c[1] if c[1] else 0:.3f}"
              f" | losing singles {l[1] / l[0] if l[0] else 0:.0%} of {l[0]} | Major Top 10 weeks {t['Major'] / nt:.0%}"
              f" | max week {maxu[y] / 1e3:.0f}k, median No.1 week {statistics.median(no1[y]) / 1e3 if no1[y] else 0:.0f}k"
              f" | exits " + ', '.join(f'{k[1]} {v}' for k, v in sorted(exits.items()) if k[0] == y))

if __name__ == '__main__':
    for n in sys.argv[2:]: main(int(sys.argv[1]), n)
