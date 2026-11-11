"""Launch-week reads for singles by acts with a recent Top 40 entry, older versus newer cohorts. Read-only."""
import sys, json, collections, statistics
from veteran_demand_common import *

def load(seed):
    weeks, first = load_chart(seed)
    rho = collections.defaultdict(lambda: [0.0, 0.0])
    for r in rows(seed, 'market-clearing-weekly'):
        w = int(r['week']); rho[w][0] += float(r['clearedSingleUnits']); rho[w][1] += float(r['serviceableSingleIntent'])
    rho = {w: v[0] / v[1] for w, v in rho.items() if v[1] > 0}
    books = {}
    for r in rows(seed, 'following'):
        if r['kind'] != 'launch': continue
        det = r['detail'].split('|')
        if det[0] != 'Single': continue
        kv = dict(x.split('=') for x in det[1:-1])
        books[det[-1]] = dict(granted=float(kv['granted']), loss=float(kv['loss']), book=float(kv['book']), young=float(r['young']), adult=float(r['adult']))
    traj = collections.defaultdict(dict)
    for w, lst in weeks.items():
        for x in lst: traj[x['rec']][w] = x
    entries = collections.defaultdict(list)  # act -> first Top 40 week of each record
    for rec, t in traj.items():
        ws = [w for w, x in t.items() if x['pos'] <= 40]
        if ws: entries[next(iter(t.values()))['artist']].append((min(ws), rec))
    launches = []
    for r in rows(seed, 'single-launch-attribution'):
        launches.append(dict(week=int(r['week']), year=int(r['year']), rec=r['singleId'].strip('"'), artist=r['artistId'].strip('"'),
            q=float(r['quality']), aw=float(r['initialAwareness']), mom=float(r['artistMomentum']), rep=float(r['artistReputation']),
            recog=float(r['effectiveRecognition']), stock=float(r['initialStock']), career=r['career'].strip('"')))
    return weeks, first, rho, books, traj, entries, launches

def fan_share(book, age):
    s = [0.5, 0.3 * (1 - book['loss']), 0.2 * (1 - book['loss'])]
    return book['granted'] * s[age - 1] / sum(s) if 1 <= age <= 3 else 0.0

YEARS = (1962, 1964, 1966, 1968)
def main(seed):
    weeks, first, rho, books, traj, entries, launches = load(seed)
    out = {}
    for year in YEARS:
        groups = collections.defaultdict(list)
        for l in launches:
            if l['year'] != year: continue
            fy = first.get(l['artist'])
            recent = any(l['week'] - 52 <= w < l['week'] for w, rec in entries.get(l['artist'], []))
            if not recent or fy is None: continue
            g = 'older (first charted 3+ yrs before)' if fy <= year - 3 else 'newer (first charted within 1 yr)' if fy >= year - 1 else None
            if g: groups[g].append(l)
        print(f'\nseed {seed} {year}: singles launched within 52 weeks of the act\'s previous Top 40 entry')
        for g, ls in sorted(groups.items()):
            def stat(band):
                ls2 = [l for l in ls if band[0] <= l['q'] < band[1]]
                if not ls2: return None
                n = len(ls2); t = [traj.get(l['rec'], {}) for l in ls2]
                wk1 = [tr.get(l['week']) for l, tr in zip(ls2, t)]
                d = dict(n=n, quality=statistics.fmean(l['q'] for l in ls2), awareness=statistics.fmean(l['aw'] for l in ls2),
                    momentum=statistics.fmean(l['mom'] for l in ls2), recognition=statistics.fmean(l['recog'] for l in ls2),
                    book=statistics.fmean(books.get(l['rec'], {}).get('granted', 0) for l in ls2),
                    hot100_week1=sum(x is not None for x in wk1) / n, top40_week1=sum(x is not None and x['pos'] <= 40 for x in wk1) / n,
                    top40=sum(any(x['pos'] <= 40 for x in tr.values()) for tr in t) / n, top10=sum(any(x['pos'] <= 10 for x in tr.values()) for tr in t) / n)
                fs = [min(1, fan_share(books[l['rec']], 1) * rho.get(l['week'], 0.1) / x['units']) for l, x in zip(ls2, wk1) if x is not None and l['rec'] in books and x['units'] > 0]
                d['fan_share_of_week1_units'] = statistics.median(fs) if fs else None
                d['week1_units'] = statistics.median(x['units'] for x in wk1 if x is not None) if any(x is not None for x in wk1) else None
                return d
            for name, band in (('all', (0, 2)), ('q .70-.80', (.70, .80))):
                d = stat(band)
                if not d: continue
                out[f'{year}|{g}|{name}'] = d
                print(f"  {g:38s} {name:9s} n={d['n']:3d} q={d['quality']:.2f} aware={d['awareness']:.2f} mom={d['momentum']:.2f} recog={d['recognition']:.2f} book={d['book']/1e3:5.1f}k"
                      f" | wk1 Hot100 {d['hot100_week1']:.0%} wk1 Top40 {d['top40_week1']:.0%} | Top40 {d['top40']:.0%} Top10 {d['top10']:.0%}"
                      f" | wk1 units {d['week1_units'] or 0:,.0f} fan share {d['fan_share_of_week1_units'] if d['fan_share_of_week1_units'] is not None else float('nan'):.0%}")
    return out

if __name__ == '__main__':
    o = main(int(sys.argv[1]) if sys.argv[1].isdigit() else sys.argv[1])
    if len(sys.argv) > 2: json.dump(o, open(sys.argv[2], 'x'), indent=1)
