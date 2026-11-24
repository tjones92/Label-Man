"""Following stock and flow by cohort, and follower purchases as a recruiting source. Read-only static read."""
import sys, json, collections
from veteran_demand_common import *

def main(seed):
    weeks, first = load_chart(seed)
    coh = lambda a: cohort_label(first.get(a))
    rho = collections.defaultdict(lambda: [0.0, 0.0]); lp = collections.defaultdict(lambda: [0.0, 0.0])
    for r in rows(seed, 'market-clearing-weekly'):
        y = int(r['year'])
        rho[y][0] += float(r['clearedSingleUnits']); rho[y][1] += float(r['serviceableSingleIntent'])
        lp[y][0] += float(r['clearedAlbumUnits']); lp[y][1] += float(r['serviceableAlbumIntent'])
    stock = {}; joins = collections.defaultdict(lambda: [0.0, 0.0]); grant = collections.defaultdict(lambda: [0.0, 0.0])
    series = collections.defaultdict(list)
    for r in rows(seed, 'following'):
        m, d, y = (int(x) for x in r['date'].split('/'))
        a = r['artistId']
        if r['kind'] == 'act':
            jb, jt = (float(x) for x in r['value'].split('|'))
            # joiners since the previous monthly row belong to the month before this row
            jy = y if m > 1 else y - 1
            joins[(jy, coh(a))][0] += jb; joins[(jy, coh(a))][1] += jt
            series[a].append((y * 12 + m - 1, float(r['young']), float(r['adult']), jb + jt))
            if m == 1: stock.setdefault((y, coh(a)), [0.0, 0.0]); stock[(y, coh(a))][0] += float(r['young']); stock[(y, coh(a))][1] += float(r['adult'])
        elif r['kind'] == 'launch':
            det = r['detail'].split('|'); g = float(dict(x.split('=') for x in det[1:-1])['granted'])
            grant[(y, coh(a))][0 if det[0] == 'Single' else 1] += g
    out = {}
    print(f'seed {seed}: year | singles cleared/intent | LP cleared/intent')
    for y in range(1960, 1970): print(y, f"{rho[y][0]/rho[y][1]:.3f}  {lp[y][0]/lp[y][1]:.3f}")
    tot = collections.defaultdict(float)
    for (y, c), v in joins.items(): tot[y] += v[0]
    print('\ncohort-year: stock Jan (M) | joiners buy/tv (M) | implied loss rate | follower units cleared est (M singles, LPs) | est. share of buyer joiners that are own followers')
    for y in (1961, 1962, 1964, 1966, 1968):
        for c in COHORTS[:5]:
            s0 = sum(stock.get((y, c), [0, 0])); s1 = sum(stock.get((y + 1, c), [0, 0])); j = joins[(y, c)]
            if s0 < 1e5: continue
            loss = (s0 + j[0] + j[1] - s1) / s0
            fs = grant[(y, c)][0] * rho[y][0] / rho[y][1]; fl = grant[(y, c)][1] * min(1, lp[y][0] / lp[y][1])
            out[f'{y}|{c}'] = dict(stock=round(s0), join_buy=round(j[0]), join_tv=round(j[1]), loss_rate=round(loss, 3), fan_single_units=round(fs), fan_lp_units=round(fl))
            print(f"{y} {c:8s} stock {s0/1e6:5.1f}  joins {j[0]/1e6:5.2f}/{j[1]/1e6:4.2f}  loss {loss:5.1%}  fan units {fs/1e6:5.2f}/{fl/1e6:5.2f}")
    # per-act: how long a following outlasts the hits. Stock relative to its peak, by months since the peak.
    surv = collections.defaultdict(list)
    for a, s in series.items():
        s.sort(); peak = max(s, key=lambda t: t[1] + t[2])
        if peak[1] + peak[2] < 50000: continue
        for t, yv, av, _ in s:
            k = t - peak[0]
            if k in (12, 24, 36, 48, 60): surv[k].append(((yv + av) / (peak[1] + peak[2]), yv / max(1, peak[1]), av / max(1, peak[2])))
    print('\nacts whose following peaked above 50k: median share of the peak still held N months later (total / young / adult)')
    import statistics
    for k in sorted(surv):
        v = surv[k]; print(k, len(v), ' / '.join(f"{statistics.median(x[i] for x in v):.2f}" for i in range(3)))
        out[f'survival_{k}m'] = dict(n=len(v), total=statistics.median(x[0] for x in v), young=statistics.median(x[1] for x in v), adult=statistics.median(x[2] for x in v))
    return out

if __name__ == '__main__':
    o = main(int(sys.argv[1]) if sys.argv[1].isdigit() else sys.argv[1])
    if len(sys.argv) > 2: json.dump(o, open(sys.argv[2], 'x'), indent=1)
