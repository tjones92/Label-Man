"""Static read of the known-act demand stack by cohort (follower-on decade controls). Read-only."""
import sys, json, statistics, collections
from veteran_demand_common import *

def main(seed):
    weeks, first = load_chart(seed)
    coh = lambda a: cohort_label(first.get(a))
    out = {}
    # 1. following stock each January by cohort (acts with >=1000 followers are logged monthly)
    stock = collections.defaultdict(lambda: [0.0, 0.0, 0]); launches = []
    for r in rows(seed, 'following'):
        m, d, y = r['date'].split('/')
        if r['kind'] == 'act' and m == '1':
            s = stock[(int(y), coh(r['artistId']))]; s[0] += float(r['young']); s[1] += float(r['adult']); s[2] += 1
        elif r['kind'] == 'launch':
            det = r['detail'].split('|'); kv = dict(x.split('=') for x in det[1:-1])
            launches.append(dict(year=int(y), artist=r['artistId'], fmt=det[0], rec=det[-1], asked=float(r['value']),
                granted=float(kv['granted']), stocked=float(kv['stocked']), book=float(kv['book']),
                young=float(r['young']), adult=float(r['adult'])))
    out['following_jan'] = {f'{y}|{c}': dict(young=round(v[0]), adult=round(v[1]), acts=v[2]) for (y, c), v in sorted(stock.items())}
    print('January following stock, millions (young/adult) and logged acts')
    for y in range(1960, 1970):
        print(y, '  '.join(f"{c}: {stock[(y,c)][0]/1e6:5.2f}/{stock[(y,c)][1]/1e6:5.2f} n={stock[(y,c)][2]:4d}" for c in COHORTS))
    # 2. follower grant vs units of charting singles in their first three chart-observed weeks
    book = {l['rec']: l for l in launches if l['fmt'] == 'Single'}
    byrec = collections.defaultdict(list)
    for wk, lst in weeks.items():
        for x in lst: byrec[x['rec']].append((wk, x))
    print('\nSingles reaching Top 40: follower grant vs lifetime units, by cohort and entry year')
    res = collections.defaultdict(lambda: [0, 0.0, 0.0, 0])
    for rec, lst in byrec.items():
        lst.sort(key=lambda t: t[0])
        if min(x['pos'] for _, x in lst) > 40: continue
        x0 = lst[0][1]; tot = max(x['total'] for _, x in lst); g = book.get(rec, {}).get('granted', 0.0)
        k = (x0['year'], coh(x0['artist'])); res[k][0] += 1; res[k][1] += g; res[k][2] += tot
        if tot > 0 and g / tot >= 0.25: res[k][3] += 1
    for y in (1961, 1962, 1964, 1966, 1968):
        print(y, '  '.join(f"{c}: n={res[(y,c)][0]:3d} grant/units={res[(y,c)][1]/max(1,res[(y,c)][2]):.2f} >=25%:{res[(y,c)][3]:3d}" for c in COHORTS[:5]))
    out['top40_grant_share'] = {f'{y}|{c}': dict(n=v[0], granted=round(v[1]), units=v[2], quarter_or_more=v[3]) for (y, c), v in sorted(res.items())}
    # 3. all single books by cohort/year
    b = collections.defaultdict(list)
    for l in launches:
        if l['fmt'] == 'Single': b[(l['year'], coh(l['artist']))].append(l['granted'])
    print('\nSingle launch books: count / mean granted / sum granted (M)')
    for y in (1961, 1962, 1964, 1966, 1968):
        print(y, '  '.join(f"{c}: {len(b[(y,c)]):4d}/{statistics.fmean(b[(y,c)] or [0])/1e3:5.1f}k/{sum(b[(y,c)])/1e6:5.2f}" for c in COHORTS))
    out['single_books'] = {f'{y}|{c}': dict(n=len(v), granted=round(sum(v))) for (y, c), v in sorted(b.items())}
    return out

if __name__ == '__main__':
    seed = int(sys.argv[1]) if sys.argv[1].isdigit() else sys.argv[1]; o = main(seed)
    if len(sys.argv) > 2: json.dump(o, open(sys.argv[2], 'x'), indent=1)
