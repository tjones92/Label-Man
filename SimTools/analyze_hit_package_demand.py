"""Hit-package demand screen (--hit-package-demand, --hit-package-demand-youth, --hit-package-decay; LpGenreAllocationFollowup.md §10).

Run from SimLogs/: python ../SimTools/analyze_hit_package_demand.py hp-off-2002-sc hp-k03-2002-sc ...
Needs *-album-chart.csv (--album-chart-history), *-lp-assembly.csv, *-a3-economic-decisions.csv and *-genre-catalog.csv.
Prints, per run and year: the youth + R&B and Country share of the year-end album top 50 (inverse-rank points, as
analyze_album_chart_head.py), the TradPop / Folk / Easy Listening / Soundtrack / Comedy shares, and the greatest-hits
packages by group (count, share reaching the top 10, median chart weeks, median units). Billboard year-end top 50
(memory billboard-1961-63-album-genre-targets): youth + R&B 16 / 30 / 20 in 1961-63, half of them hit collections;
Country about 0. Billboard 1961 mono top 50: six hit collections (four youth), about four youth studio LPs.
"""
import pandas as pd, sys

YOUTH_EXTRA = {'TeenPop', 'Bubblegum', 'GirlGroup', 'BritishPop'}
for t in sys.argv[1:]:
    cat = pd.read_csv(f'{t}-genre-catalog.csv', usecols=['genre', 'family'])
    youth = set(cat[cat.family.isin(['Rock', 'RhythmAndSoul'])].genre) | YOUTH_EXTRA
    a = pd.read_csv(f'{t}-album-chart.csv', usecols=['week', 'year', 'chartSize', 'position', 'recordId', 'genre', 'albumFormat',
                                                     'totalUnitsSold'])
    if a.empty: print(t, 'album chart is empty (run with --album-chart-history)'); continue
    a['g'] = a.genre.where(a.albumFormat != 'Soundtrack', 'Soundtrack')
    a.loc[a.g.isin(youth), 'g'] = 'Youth+R&B'
    a['pts'] = a.chartSize + 1 - a.position
    lp = pd.read_csv(f'{t}-lp-assembly.csv', usecols=['year', 'kind', 'recordId'])
    grp = pd.read_csv(f'{t}-a3-economic-decisions.csv', usecols=['recordId', 'genre']).drop_duplicates('recordId')
    lp = lp.merge(grp, on='recordId', how='left')
    lp['group'] = lp.genre.map(lambda g: 'youth' if g in youth else 'adult/other')
    rows = {}
    for y, x in a.groupby('year'):
        if x.week.nunique() < 40: continue
        s = x.groupby('recordId').agg(pts=('pts', 'sum'), g=('g', 'first')).sort_values('pts', ascending=False).head(50)
        top = 100 * s.g.value_counts() / len(s)
        hits = set(lp[lp.kind == 'GreatestHits'].recordId)
        for g in ['Youth+R&B', 'Country', 'TraditionalPop', 'Folk', 'EasyListening', 'Soundtrack', 'Comedy']:
            rows.setdefault(g, {})[y] = top.get(g, 0.0)
        rows.setdefault('hit packages in top 50', {})[y] = 100 * s.index.isin(hits).mean()
        rows.setdefault('youth hit packages in top 50', {})[y] = 100 * (s.index.isin(hits) & (s.g == 'Youth+R&B')).mean()
        rows.setdefault('youth studio LPs in top 50', {})[y] = 100 * (~s.index.isin(hits) & (s.g == 'Youth+R&B')).mean()
    print(t); print(pd.DataFrame(rows).T.round(0).astype(int).to_string())
    per = a.groupby('recordId').agg(peak=('position', 'min'), weeks=('position', 'size'), units=('totalUnitsSold', 'max')).reset_index()
    m = lp.merge(per, on='recordId', how='left')
    m['top10'] = m.peak <= 10
    out = m.groupby(['group', 'kind']).agg(n=('recordId', 'size'), top10=('top10', 'mean'), weeks=('weeks', 'median'),
                                          units=('units', 'median')).round(2)
    print(out.to_string()); print()
