"""What a following actually buys, how big a launch week the model produces, and what cold veterans do late in the
decade. Static read of a completed follower-on decade run. Read-only."""
import sys, json, collections, statistics
from veteran_demand_common import *

def med(xs):
    xs = [x for x in xs if x is not None]
    return statistics.median(xs) if xs else float('nan')
def mean(xs):
    xs = [x for x in xs if x is not None]
    return statistics.fmean(xs) if xs else float('nan')

def main(seed):
    out = {}
    # ---- chart trajectories -------------------------------------------------------------------------------------
    traj = collections.defaultdict(dict); meta = {}; first = {}
    for r in rows(seed, 'records'):
        w = int(r['week']); p = int(r['currentPosition']); rec = r['recordId'].strip('"'); a = r['artistId'].strip('"')
        traj[rec][w] = (p, int(r['unitsThisWeek']), int(r['totalUnitsSold']), int(r['weeksSinceRelease']))
        if rec not in meta: meta[rec] = dict(artist=a, genre=r['genre'].strip('"'), q=float(r['quality']), tier=r['labelTier'].strip('"'))
        if 1 <= p <= 100:
            y = int(r['year'])
            if a not in first or y < first[a]: first[a] = y
    top40_entries = collections.defaultdict(list)
    for rec, t in traj.items():
        ws = [w for w, x in t.items() if 1 <= x[0] <= 40]
        if ws: top40_entries[meta[rec]['artist']].append(min(ws))
    rho = collections.defaultdict(lambda: [0.0, 0.0]); week_year = {}
    for r in rows(seed, 'market-clearing-weekly'):
        w = int(r['week']); week_year[w] = int(r['year'])
        rho[w][0] += float(r['clearedSingleUnits']); rho[w][1] += float(r['serviceableSingleIntent'])
    rho = {w: v[0] / v[1] for w, v in rho.items() if v[1] > 0}

    # ---- launch books -------------------------------------------------------------------------------------------
    books = {}
    for r in rows(seed, 'following'):
        if r['kind'] != 'launch': continue
        det = r['detail'].split('|'); kv = dict(x.split('=') for x in det[1:-1])
        books[det[-1]] = dict(fmt=det[0], asked=float(r['value']), granted=float(kv['granted']), stocked=float(kv['stocked']),
            book=float(kv['book']), young=float(r['young']), adult=float(r['adult']), artist=r['artistId'], name=r['artist'],
            year=int(r['date'].split('/')[-1]), genre=r['genre'])
    launches = {}
    for r in rows(seed, 'single-launch-attribution'):
        launches[r['singleId'].strip('"')] = dict(week=int(r['week']), year=int(r['year']), artist=r['artistId'].strip('"'), name=r['artistName'].strip('"'),
            genre=r['genre'].strip('"'), q=float(r['quality']), stock=float(r['initialStock']), aw=float(r['initialAwareness']),
            recog=float(r['effectiveRecognition']), mom=float(r['artistMomentum']), career=r['career'].strip('"'))

    def run(rec):
        """First-three-week units while charted, entry position, peak, weeks from launch to peak, lifetime units."""
        t = traj.get(rec, {}); l = launches.get(rec)
        if not t or not l: return None
        w0 = l['week']
        charted = {w: x for w, x in t.items() if 1 <= x[0] <= 100}
        wk = [t.get(w0 + i) for i in range(3)]
        d = dict(w1=wk[0][1] if wk[0] else None, w3=sum(x[1] for x in wk if x) if any(wk) else None,
                 w1pos=wk[0][0] if wk[0] and wk[0][0] > 0 else None, life=max(x[2] for x in t.values()),
                 peak_units=max(x[1] for x in t.values()))
        if charted:
            pk = min(x[0] for x in charted.values()); d['peak'] = pk
            d['to_peak'] = min(w for w, x in charted.items() if x[0] == pk) - w0
            d['weeks'] = len(charted)
        return d

    # 1. the biggest single books of the decade, and what they sold
    print(f'\n== seed {seed}: the 15 largest single launch books ==')
    print('year act                       followers  book   stocked  initStock | wk1 units (pos)  3wk units  3wk/book | peak  wks to peak  lifetime')
    big = sorted((b for rec, b in books.items() if b['fmt'] == 'Single' and rec in launches), key=lambda b: -b['asked'])
    recs = {id(b): rec for rec, b in books.items()}
    seen = collections.Counter(); rowsout = []
    for b in big:
        if seen[b['artist']] >= 2: continue
        seen[b['artist']] += 1
        rec = recs[id(b)]; r = run(rec) or {}; l = launches[rec]
        rowsout.append(dict(year=l['year'], act=b['name'], followers=b['young'] + b['adult'], book=b['asked'], stocked=b['stocked'], init_stock=l['stock'], **r))
        print(f"{l['year']} {b['name'][:24]:24s} {(b['young'] + b['adult']) / 1e6:6.2f}M {b['asked'] / 1e3:6.0f}k {b['stocked'] / 1e3:6.0f}k {l['stock'] / 1e3:7.0f}k |"
              f" {(r.get('w1') or 0) / 1e3:6.1f}k ({r.get('w1pos') or '-':>3}) {(r.get('w3') or 0) / 1e3:7.1f}k  {(r.get('w3') or 0) / b['asked']:6.1%} |"
              f" {r.get('peak', '-'):>3}  {r.get('to_peak', '-'):>3}  {(r.get('life') or 0) / 1e3:7.0f}k")
        if len(rowsout) == 15: break
    out['largest_books'] = rowsout

    # 2. books by size band: conversion and chart behaviour
    print('\n== single books by size: what a book turns into ==')
    print('band          n   med followers  med book | 3wk units/book  wk1 Hot100  wk1 Top40  wk1 Top10 | Top40  Top10  med wks to peak  med lifetime')
    bands = [('100-999', 100, 1e3), ('1k-10k', 1e3, 1e4), ('10k-50k', 1e4, 5e4), ('50k-100k', 5e4, 1e5), ('100k-250k', 1e5, 2.5e5), ('250k+', 2.5e5, 1e12)]
    out['bands'] = {}
    for name, lo, hi in bands:
        sel = [(rec, b) for rec, b in books.items() if b['fmt'] == 'Single' and lo <= b['asked'] < hi and rec in launches and launches[rec]['year'] >= 1962]
        rs = [(b, run(rec)) for rec, b in sel]
        n = len(rs)
        if not n: continue
        ch = [(b, r) for b, r in rs if r]
        conv = med([r['w3'] / b['asked'] for b, r in ch if r.get('w3')])
        d = dict(n=n, followers=med([b['young'] + b['adult'] for b, _ in rs]), book=med([b['asked'] for b, _ in rs]), conv=conv,
            wk1_hot100=sum(1 for _, r in ch if r.get('w1pos') and r['w1pos'] <= 100) / n, wk1_top40=sum(1 for _, r in ch if r.get('w1pos') and r['w1pos'] <= 40) / n,
            wk1_top10=sum(1 for _, r in ch if r.get('w1pos') and r['w1pos'] <= 10) / n,
            top40=sum(1 for _, r in ch if r.get('peak', 999) <= 40) / n, top10=sum(1 for _, r in ch if r.get('peak', 999) <= 10) / n,
            to_peak=med([r['to_peak'] for _, r in ch if r.get('peak', 999) <= 40]), life=med([r['life'] for _, r in ch if r.get('peak', 999) <= 40]))
        out['bands'][name] = d
        print(f"{name:10s} {n:5d}  {d['followers'] / 1e3:9.0f}k  {d['book'] / 1e3:7.1f}k | {d['conv']:10.1%}  {d['wk1_hot100']:9.0%}  {d['wk1_top40']:8.0%}  {d['wk1_top10']:8.0%} |"
              f" {d['top40']:5.0%}  {d['top10']:5.0%}  {d['to_peak']:8.0f}  {d['life'] / 1e3:10.0f}k")

    # 3. the biggest launch weeks the world produced, and No. 1 pace
    print('\n== the largest first-week single sales in the run (any act) ==')
    fw = []
    for rec, l in launches.items():
        x = traj.get(rec, {}).get(l['week'])
        if x: fw.append((x[1], rec))
    fw.sort(reverse=True); out['largest_first_weeks'] = []
    for u, rec in fw[:10]:
        l = launches[rec]; b = books.get(rec, {}); r = run(rec)
        out['largest_first_weeks'].append(dict(units=u, year=l['year'], act=l['name'], book=b.get('asked', 0), pos=r.get('w1pos'), peak=r.get('peak'), life=r['life']))
        print(f"  {u / 1e3:6.1f}k  {l['year']} {l['name'][:24]:24s} book {b.get('asked', 0) / 1e3:5.0f}k  entered {r.get('w1pos') or '-':>3}  peak {r.get('peak', '-'):>3}  lifetime {r['life'] / 1e3:6.0f}k")
    by_year = collections.defaultdict(list)
    for u, rec in fw: by_year[launches[rec]['year']].append(u)
    print('  first-week units of singles that charted in week one, by year: n / median / p90 / max')
    for y in sorted(by_year):
        v = sorted(by_year[y]); print(f"   {y}: {len(v):4d} / {med(v) / 1e3:5.1f}k / {v[int(.9 * len(v))] / 1e3:5.1f}k / {v[-1] / 1e3:6.1f}k")
    print('  Top 10 singles: weeks from launch to peak, and entry position (launch 1962+)')
    t10 = [(run(rec), rec) for rec in launches if launches[rec]['year'] >= 1962]
    t10 = [(r, rec) for r, rec in t10 if r and r.get('peak', 999) <= 10]
    tp = [r['to_peak'] for r, _ in t10]
    out['top10_to_peak'] = dict(n=len(tp), median=med(tp), p10=sorted(tp)[len(tp) // 10], share_within_3=sum(1 for x in tp if x <= 3) / len(tp))
    print(f"   n={len(tp)} median {med(tp):.0f} wks, p10 {sorted(tp)[len(tp) // 10]} wks, peaked within 3 wks of launch {sum(1 for x in tp if x <= 3) / len(tp):.1%};"
          f" entered Hot 100 in launch week {sum(1 for r, _ in t10 if r.get('w1pos')) / len(tp):.0%}, entered Top 40 in launch week {sum(1 for r, _ in t10 if r.get('w1pos') and r['w1pos'] <= 40) / len(tp):.1%}")
    # 4. cold veterans late in the decade
    heat = {}
    for r in rows(seed, 'artist-heat-attribution'):
        if int(r['year']) < 1968: continue
        heat[(int(r['week']), r['artistId'].strip('"'))] = (float(r['originalHeat']), float(r['albumCausedHeatDelta']), float(r['retainedAlbumHeatDelta']), int(r['albumHits']), int(r['singleHits']), float(r['albumCurrentHeat']))
    def H(l): return heat.get((l['week'], l['artist'])) or heat.get((l['week'] - 1, l['artist'])) or (0, 0, 0, 0, 0, 0)
    first_genre = {}
    for rec, t in sorted(traj.items(), key=lambda kv: min(kv[1])):
        if any(1 <= x[0] <= 100 for x in t.values()): first_genre.setdefault(meta[rec]['artist'], meta[rec]['genre'])
    def group(l):
        fy = first.get(l['artist']); ent = [w for w in top40_entries.get(l['artist'], []) if w < l['week']]
        since = (l['week'] - max(ent)) if ent else None
        if fy is None or fy >= l['year']: return 'no chart history'
        if fy <= 1961:
            if since is None or since > 156: return 'first charted 1960-61, no Top 40 single in 3+ yrs'
            if since <= 52: return 'first charted 1960-61, Top 40 single in last year'
            return 'first charted 1960-61, Top 40 single 1-3 yrs ago'
        if fy >= 1966 and since is not None and since <= 52: return 'first charted 1966+, Top 40 single in last year'
        return None
    print('\n== 1968-69 single launches: does an old catalogue carry a cold act? ==')
    print('group                                                 n  acts   q   aware recog  heat (album part / retained peaks)  album hits  book | Top40  Top10')
    out['cold'] = {}
    def report(label, ls):
        if not ls: return
        hs = [H(l) for l in ls]
        rs = [run(l['rec']) for l in ls]
        d = dict(n=len(ls), acts=len({l['artist'] for l in ls}), q=mean([l['q'] for l in ls]), aware=mean([l['aw'] for l in ls]), recog=mean([l['recog'] for l in ls]),
            nocur=sum(1 for h in hs if h[3] > 0 and h[5] == 0) / max(1, sum(1 for h in hs if h[3] > 0)), heat=mean([h[0] for h in hs]), album=mean([h[1] for h in hs]), retained=mean([h[2] for h in hs]), album_hits=mean([h[3] for h in hs]),
            book=mean([books.get(l['rec'], {}).get('asked', 0) for l in ls]),
            top40=sum(1 for r in rs if r and r.get('peak', 999) <= 40) / len(ls), top10=sum(1 for r in rs if r and r.get('peak', 999) <= 10) / len(ls))
        out['cold'][label] = d
        print(f"{label:50s} {d['n']:4d} {d['acts']:4d}  {d['q']:.2f}  {d['aware']:.2f}  {d['recog']:.2f}  {d['heat']:.2f} ({d['album']:.2f} / {d['retained']:.2f})"
              f"            {d['album_hits']:5.2f}  {d['book'] / 1e3:5.1f}k | {d['top40']:5.1%}  {d['top10']:5.1%} | no LP on chart now {d['nocur']:.0%}")
    late = [dict(rec=rec, **l) for rec, l in launches.items() if l['year'] in (1968, 1969)]
    groups = collections.defaultdict(list)
    for l in late:
        g = group(l)
        if g: groups[g].append(l)
    for g in sorted(groups):
        report(g, groups[g]); report('   quality .70-.80', [l for l in groups[g] if .70 <= l['q'] < .80])
    cold = groups['first charted 1960-61, no Top 40 single in 3+ yrs']
    report('cold 1960-61 acts with 2+ active album hits', [l for l in cold if H(l)[3] >= 2])
    report('cold 1960-61 acts with no active album hit', [l for l in cold if H(l)[3] == 0])
    old = ('DooWop', 'GirlGroup', 'RockAndRoll', 'Rockabilly', 'TeenPop')
    for yrs in ((1964, 1965), (1966, 1967), (1968, 1969)):
        report(f'DooWop/R&R/TeenPop acts first charted 1960-61, {yrs[0]}-{yrs[1] % 100}', [dict(rec=rec, **l) for rec, l in launches.items()
            if l['year'] in yrs and first.get(l['artist'], 9999) <= 1961 and first_genre.get(l['artist']) in old])
    return out

if __name__ == '__main__':
    o = main(int(sys.argv[1]) if sys.argv[1].isdigit() else sys.argv[1])
    if len(sys.argv) > 2: json.dump(o, open(sys.argv[2], 'w'), indent=1, default=str)
