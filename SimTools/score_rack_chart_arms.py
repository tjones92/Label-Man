"""Score rack-in-chart arms (LpGenreAllocationFollowup.md §72): album top-50 head rows and gap, front-line rack and store LPs,
defunct labels, youth share of store/rack units and the four-week rack top 10 (needs --album-demand-trace).
Run from SimLogs: python ../SimTools/score_rack_chart_arms.py PREFIX [PREFIX ...]   (seeds 1001/2002)"""
import pandas as pd, sys
seeds = [1001, 2002]
T = {1960: dict(Soundtrack=16, TraditionalPop=28, Folk=14, Youth=14, Comedy=12, EasyListening=6, Country=2),
     1961: dict(Soundtrack=24, TraditionalPop=22, Folk=10, Youth=16, Comedy=12, EasyListening=10, Country=2)}
YX = {'TeenPop', 'Bubblegum', 'GirlGroup', 'BritishPop'}
G = list(T[1961])
def grp(df, youth):
    g = df.genre.where(df.albumFormat != 'Soundtrack', 'Soundtrack')
    g = g.where(~g.isin(youth), 'Youth')
    return g.where(g.isin(G), 'Other')
rows = {}
for pre in sys.argv[1:]:
    R = {}
    acc = {y: {g: 0.0 for g in G} for y in T}; rk = {g: 0.0 for g in G + ['Other']}; nwin = 0
    for s in seeds:
        t = f'{pre}-{s}-sc'
        cat = pd.read_csv(f'{t}-genre-catalog.csv.gz', usecols=['genre', 'family'])
        youth = set(cat[cat.family.isin(['Rock', 'RhythmAndSoul'])].genre) | YX
        a = pd.read_csv(f'{t}-album-chart.csv.gz', usecols=['year', 'chartSize', 'position', 'recordId', 'genre', 'albumFormat'])
        a['g'] = grp(a, youth); a['pts'] = a.chartSize + 1 - a.position
        for y in T:
            top = a[a.year == y].groupby('recordId').agg(pts=('pts', 'sum'), g=('g', 'first')).nlargest(50, 'pts')
            v = 100 * top.g.value_counts() / 50
            for g in G: acc[y][g] += float(v.get(g, 0)) / len(seeds)
        f = pd.read_csv(f'{t}-rack-finance-weekly.csv.gz', usecols=['year', 'units'])
        R.setdefault('rack M 1960/61', []).append('/'.join(f'{f[f.year == y].units.sum() / 1e6:.1f}' for y in T))
        r = pd.read_csv(f'{t}-decade-annual-rollup.csv.gz')
        R.setdefault('store M 1960/61', []).append('/'.join(f'{u / 1e6:.1f}' for u in r[r.year.isin(T)].albumUnits))
        fin = pd.read_csv(f'{t}-label-finance.csv.gz', usecols=['labelId', 'status'])
        R.setdefault('defunct', []).append(str(int((fin.groupby('labelId').tail(1).status == 'Defunct').sum())))
        try:
            d = pd.read_csv(f'{t}-album-demand-trace.csv.gz', usecols=['week', 'year', 'recordId', 'genre', 'albumFormat', 'units', 'rackUnits'])
        except FileNotFoundError:
            continue
        d['g'] = grp(d, youth)
        x = d[d.year == 1961]
        R.setdefault('youth store% / rack% 1961', []).append(f"{100 * x[x.g == 'Youth'].units.sum() / x.units.sum():.1f} / {100 * x[x.g == 'Youth'].rackUnits.sum() / x.rackUnits.sum():.1f}")
        d['win'] = d.week // 4
        w = d[d.win >= 3].groupby(['win', 'recordId']).agg(rack=('rackUnits', 'sum'), g=('g', 'first')).reset_index()
        for _, z in w.groupby('win'):
            for g, n in z.nlargest(10, 'rack').g.value_counts().items(): rk[g] += n
            nwin += 1
    for y in T:
        R[f'gap {y}'] = [f"{sum(abs(acc[y][g] - T[y][g]) for g in G):.0f}"]
        R[f'rows {y}'] = [' '.join(f'{g[:5]} {acc[y][g]:.0f}' for g in G)]
    if nwin: R['rack top10 (ST/TP/EL/Y)'] = [' / '.join(f'{rk[g] / nwin:.1f}' for g in ['Soundtrack', 'TraditionalPop', 'EasyListening', 'Youth'])]
    rows[pre] = {k: '; '.join(v) for k, v in R.items()}
print(pd.DataFrame(rows).to_string())
