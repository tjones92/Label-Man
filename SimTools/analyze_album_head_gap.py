"""Gap between the model's year-end album top 50 and Billboard's, 1960-61 (LpGenreAllocationFollowup.md §9, §13).

Run from SimLogs/: python ../SimTools/analyze_album_head_gap.py sla-k25 sft-t10k25 ... [--seeds 2002 1001]
Each argument is a run prefix; runs are <prefix>-<seed>-sc. Needs *-album-chart.csv, *-genre-catalog.csv,
*-decade-annual-rollup.csv and *-label-finance.csv. Prints, per prefix: each genre group's share of the 1960 and 1961
top 50 (count of titles, as analyze_hit_package_demand.py) per seed, the sum of absolute gaps of the two-seed means to
Billboard over the seven rows, store LPs and defunct labels.
Billboard: 1960 from "Chart Toppers of 1960" (19 Dec 1960, p. 23); 1961 from §9.
"""
import pandas as pd, sys

TARGETS = {1960: dict(Soundtrack=16, TraditionalPop=28, Folk=14, Youth=14, Comedy=12, EasyListening=6, Country=2),
           1961: dict(Soundtrack=24, TraditionalPop=22, Folk=10, Youth=16, Comedy=12, EasyListening=10, Country=2)}
YOUTH_EXTRA = {'TeenPop', 'Bubblegum', 'GirlGroup', 'BritishPop'}
args = sys.argv[1:]
seeds = [2002, 1001]
if '--seeds' in args:
    i = args.index('--seeds'); seeds = [int(s) for s in args[i + 1:]]; args = args[:i]
for prefix in args:
    shares = {y: {g: [] for g in TARGETS[y]} for y in TARGETS}
    store, defunct = [], []
    for seed in seeds:
        t = f'{prefix}-{seed}-sc'
        cat = pd.read_csv(f'{t}-genre-catalog.csv', usecols=['genre', 'family'])
        youth = set(cat[cat.family.isin(['Rock', 'RhythmAndSoul'])].genre) | YOUTH_EXTRA
        a = pd.read_csv(f'{t}-album-chart.csv', usecols=['year', 'chartSize', 'position', 'recordId', 'genre', 'albumFormat'])
        a['g'] = a.genre.where(a.albumFormat != 'Soundtrack', 'Soundtrack')
        a.loc[a.g.isin(youth), 'g'] = 'Youth'
        a['pts'] = a.chartSize + 1 - a.position
        for y in TARGETS:
            s = a[a.year == y].groupby('recordId').agg(pts=('pts', 'sum'), g=('g', 'first')).nlargest(50, 'pts')
            v = 100 * s.g.value_counts() / 50
            for g in TARGETS[y]: shares[y][g].append(round(float(v.get(g, 0))))
        r = pd.read_csv(f'{t}-decade-annual-rollup.csv')
        store.append('/'.join(f'{u / 1e6:.1f}' for u in r[r.year.isin(TARGETS)].albumUnits))
        f = pd.read_csv(f'{t}-label-finance.csv', usecols=['labelId', 'status'])
        defunct.append(int((f.groupby('labelId').tail(1).status == 'Defunct').sum()))
    print(prefix)
    for y, T in TARGETS.items():
        gap = sum(abs(sum(shares[y][g]) / len(seeds) - T[g]) for g in T)
        print(f'  {y} gap {gap:.0f}: ' + ', '.join(f'{g} {"/".join(map(str, shares[y][g]))} ({T[g]})' for g in T))
    print(f'  store LPs 1960/1961 (M): {"; ".join(store)}   defunct: {"; ".join(map(str, defunct))}')
