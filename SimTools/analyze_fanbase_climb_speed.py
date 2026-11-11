"""Climb speed of Top 10 singles: weeks from Hot 100 debut to peak and debut position, model versus the recovered
1960-64 Billboard Hot 100. Read-only."""
import sys, csv, collections, datetime, statistics, os
from veteran_demand_common import *

def summarise(name, runs):
    """runs: list of (debut_position, weeks_from_debut_to_peak, artist)."""
    n = len(runs); tp = sorted(r[1] for r in runs)
    print(f"{name:34s} n={n:4d} debut->peak median {statistics.median(tp):.0f} wks, p10 {tp[n // 10]} | peaked within 2 wks {sum(1 for x in tp if x <= 2) / n:5.1%},"
          f" within 4 wks {sum(1 for x in tp if x <= 4) / n:5.1%} | debuted in Top 40 {sum(1 for r in runs if r[0] <= 40) / n:5.1%}, in Top 20 {sum(1 for r in runs if r[0] <= 20) / n:5.1%}")
    return dict(n=n, median=statistics.median(tp), p10=tp[n // 10], within2=sum(1 for x in tp if x <= 2) / n, within4=sum(1 for x in tp if x <= 4) / n,
                debut_top40=sum(1 for r in runs if r[0] <= 40) / n, debut_top20=sum(1 for r in runs if r[0] <= 20) / n)

def history():
    t = collections.defaultdict(dict)
    with open(os.path.join(LOGS, 'historical-hot100-1960-64', 'hot100_weekly.csv'), newline='', encoding='utf-8') as f:
        for r in csv.DictReader(f):
            d = datetime.date.fromisoformat(r['chartDate'][:10]); t[(r['title'], r['artist'])][d] = int(r['position'])
    start = min(min(v) for v in t.values()); runs = []
    for (title, artist), v in t.items():
        if min(v) <= start + datetime.timedelta(days=28): continue  # already charting when the data starts
        pk = min(v.values())
        if pk > 10: continue
        d0 = min(v); dp = min(d for d, p in v.items() if p == pk)
        runs.append((v[d0], (dp - d0).days // 7, artist))
    return runs

def model(seed):
    t = collections.defaultdict(dict); art = {}
    for r in rows(seed, 'records'):
        p = int(r['currentPosition'])
        if 1 <= p <= 100 and int(r['year']) <= 1964: t[r['recordId']][int(r['week'])] = p; art[r['recordId']] = r['artistId']
    runs = []
    for rec, v in t.items():
        if min(v) <= 8: continue
        pk = min(v.values())
        if pk > 10: continue
        w0 = min(v); runs.append((v[w0], min(w for w, p in v.items() if p == pk) - w0, art[rec]))
    return runs

if __name__ == '__main__':
    h = history(); summarise('Billboard 1960-64, Top 10 singles', h)
    acts = collections.Counter(r[2] for r in h)
    summarise('  by acts with 5+ Top 10s', [r for r in h if acts[r[2]] >= 5])
    for seed in (int(a) for a in sys.argv[1:]):
        m = model(seed); summarise(f'model seed {seed} 1960-64', m)
        acts = collections.Counter(r[2] for r in m)
        summarise('  by acts with 5+ Top 10s', [r for r in m if acts[r[2]] >= 5])
