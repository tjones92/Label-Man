"""Year-end album top 50 by genre from the weekly album chart (LP channel handoff issue 1, 2026-11-15).

Run from SimLogs/: python ../SimTools/analyze_album_chart_head.py fh-v1-2002-lref fh-v1-2002-fa ...
Needs *-album-chart.csv, which --calibration leaves empty unless the run had --album-chart-history.
A title's year-end score is its inverse-rank points over the year's chart weeks (chart size + 1 - position). The table
gives each genre's share of the year's 50 best titles, of all chart weeks, and of No. 1 weeks. Soundtrack-format
albums are counted as Soundtrack whatever their mapped genre. Author's top-50 estimate (albumchart.odt), 1960-64:
TradPop 30/28/24/36/28, Folk 13/6/18/18/13, Jazz 0/4/2/2/2, Easy Listening 6/12/10/2/9, Comedy 11/6/6/8/3,
Bossa Nova -/-/2/4/2, Soundtrack and cast about 7-14 (about 25 in 1962-63).
"""
import pandas as pd, sys
G = ['Folk', 'TraditionalPop', 'EasyListening', 'Soundtrack', 'Comedy', 'Classical', 'Jazz', 'Country', 'Soul', 'BossaNova',
     'TeenPop', 'RockAndRoll']
for t in sys.argv[1:]:
    a = pd.read_csv(f'{t}-album-chart.csv', usecols=['week', 'year', 'chartSize', 'position', 'recordId', 'genre', 'albumFormat'])
    if a.empty: print(t, 'album chart is empty (run with --album-chart-history)'); continue
    a['g'] = a.genre.where(a.albumFormat != 'Soundtrack', 'Soundtrack')
    a['pts'] = a.chartSize + 1 - a.position
    rows = {}
    for y, x in a.groupby('year'):
        if x.week.nunique() < 40: continue
        s = x.groupby('recordId').agg(pts=('pts', 'sum'), g=('g', 'first')).sort_values('pts', ascending=False).head(50)
        top = 100 * s.g.value_counts() / len(s)
        weeks = 100 * x[x.position <= 50].g.value_counts(normalize=True)
        one = 100 * x[x.position == 1].g.value_counts(normalize=True)
        for g in G:
            rows.setdefault(g, {})[f'{y} top50'] = top.get(g, 0.0)
            rows[g][f'{y} wk'] = weeks.get(g, 0.0)
            rows[g][f'{y} no1'] = one.get(g, 0.0)
    print(t); print(pd.DataFrame(rows).T.round(0).astype(int).to_string())
