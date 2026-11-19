"""Offline album-chart re-rank with rack units (LpGenreAllocationFollowup.md §70). Run from SimLogs:
   python ../SimTools/analyze_album_chart_rack_mix.py PREFIX   (needs --album-demand-trace, rack-title-weekly, album-chart)
Ranks the traced LPs by store + W x rack', where rack' rescales each non-soundtrack group's rack units toward the
all-artist-LP rack/store ratio by exponent a (0 = model mix, 1 = racks proportional to store).
Also a hit-tier variant: only rack units sold from the hit tier are rescaled (catalogue keeps the adult tilt)."""
import pandas as pd, sys
pre = sys.argv[1]; seeds = [1001, 2002]
T = {1960: dict(Soundtrack=16, TraditionalPop=28, Folk=14, Youth=14, Comedy=12, EasyListening=6, Country=2),
     1961: dict(Soundtrack=24, TraditionalPop=22, Folk=10, Youth=16, Comedy=12, EasyListening=10, Country=2)}
YX = {'TeenPop', 'Bubblegum', 'GirlGroup', 'BritishPop'}
ds = []
for s in seeds:
    t = f'{pre}-{s}-sc'
    cat = pd.read_csv(f'{t}-genre-catalog.csv.gz', usecols=['genre', 'family'])
    youth = set(cat[cat.family.isin(['Rock', 'RhythmAndSoul'])].genre) | YX
    d = pd.read_csv(f'{t}-album-demand-trace.csv.gz', usecols=['week', 'year', 'recordId', 'genre', 'albumFormat', 'units', 'rackUnits'])
    d['g'] = d.genre.where(d.albumFormat != 'Soundtrack', 'Soundtrack')
    d.loc[d.g.isin(youth), 'g'] = 'Youth'
    d.loc[~d.g.isin(list(T[1961])), 'g'] = 'Other'
    r = pd.read_csv(f'{t}-rack-title-weekly.csv.gz', usecols=['week', 'recordId', 'tier', 'units'])
    hit = r[r.tier.astype(str).isin(['1', 'Hit', 'TierHit'])].groupby(['week', 'recordId']).units.sum().rename('hitRack')
    d = d.merge(hit, left_on=['week', 'recordId'], right_index=True, how='left').fillna({'hitRack': 0})
    ch = pd.read_csv(f'{t}-album-chart.csv.gz', usecols=['week', 'chartSize']).drop_duplicates()
    d = d.merge(ch, on='week', how='left'); d['seed'] = s; ds.append(d)
d = pd.concat(ds)
print('rack tier values seen:', sorted(pd.read_csv(f'{pre}-1001-sc-rack-title-weekly.csv.gz', usecols=['tier'], nrows=200000).tier.astype(str).unique()))
art = d[d.g != 'Soundtrack']
ratio = (art.groupby(['year', 'g']).rackUnits.sum() / art.groupby(['year', 'g']).units.sum())
ratio_all = art.groupby('year').rackUnits.sum() / art.groupby('year').units.sum()
hratio = (art.groupby(['year', 'g']).hitRack.sum() / art.groupby(['year', 'g']).units.sum())
hratio_all = art.groupby('year').hitRack.sum() / art.groupby('year').units.sum()
print('rack/store by group:'); print(pd.concat({'all': ratio.unstack(0).round(2), 'hitTier': hratio.unstack(0).round(2)}, axis=1).to_string())
def score(col, W):
    d['p'] = d.units + W * d[col]
    d['rk'] = d.groupby(['seed', 'week']).p.rank(ascending=False, method='first')
    c = d[d.rk <= d.chartSize].copy(); c['pts'] = c.chartSize + 1 - c.rk
    out = []
    for y in (1960, 1961):
        sh = {g: 0 for g in T[y]}
        for s in seeds:
            top = c[(c.year == y) & (c.seed == s)].groupby('recordId').agg(pts=('pts', 'sum'), g=('g', 'first')).nlargest(50, 'pts')
            v = 100 * top.g.value_counts() / 50
            for g in T[y]: sh[g] += float(v.get(g, 0)) / len(seeds)
        out.append(f'{y} gap {sum(abs(sh[g] - T[y][g]) for g in T[y]):.0f} ' + ' '.join(f'{g[:5]} {sh[g]:.0f}' for g in T[y]))
    return ' | '.join(out)
print('W 0      :', score('units', 0))
for a in (0, 0.5, 1.0):
    f = d.apply(lambda r: 1.0 if r.g == 'Soundtrack' else (ratio_all[r.year] / ratio[(r.year, r.g)]) ** a, axis=1) if a else 1.0
    d['rk_all'] = d.rackUnits * f
    fh = d.apply(lambda r: 1.0 if r.g == 'Soundtrack' or hratio.get((r.year, r.g), 0) == 0 else (hratio_all[r.year] / hratio[(r.year, r.g)]) ** a, axis=1) if a else 1.0
    d['rk_hit'] = (d.rackUnits - d.hitRack) + d.hitRack * fh
    for W in (0.5, 1.0):
        print(f'a {a} W {W} all :', score('rk_all', W))
        if a: print(f'a {a} W {W} hit :', score('rk_hit', W))
