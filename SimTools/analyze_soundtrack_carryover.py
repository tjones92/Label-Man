"""Soundtrack carry-over screen (--soundtrack-hit-tail, --soundtrack-pool-breadth; LpGenreAllocationFollowup.md §11-12).

Run from SimLogs/: python ../SimTools/analyze_soundtrack_carryover.py cs-c05b3-2002-sc sht-b2r10-2002-sc ...
Needs *-album-chart.csv (--album-chart-history). --opening-soundtrack-catalogue titles (released 1956-59) are the
soundtracks that chart by week 4 with 100k+ units already sold at their first chart week (records.csv logs no
albums); the rest are dated by their first chart year. Billboard 1960 (§13): 8 of 50, 6-7 released
before 1960 and 1-2 in 1960. Prints, per run: the soundtrack share of the 1960 and 1961 year-end top 50
(inverse-rank points, as analyze_album_chart_head.py), the 1961 share that first charted before 1961 (Billboard 1961:
24 soundtrack points, about 20 released before 1961 and about 10 of those in 1960), and the chart life of 1960's
soundtracks (all, and those that reached the top 10): median weeks, share still charting in 1961, 1961 chart weeks.
"""
import pandas as pd, sys

for t in sys.argv[1:]:
    a = pd.read_csv(f'{t}-album-chart.csv', usecols=['week', 'year', 'chartSize', 'position', 'recordId', 'albumFormat'])
    a['pts'] = a.chartSize + 1 - a.position
    first = a.groupby('recordId').year.min().rename('firstYear')
    a = a.join(first, on='recordId')
    st = a.albumFormat == 'Soundtrack'
    u = pd.read_csv(f'{t}-album-chart.csv', usecols=['week', 'recordId', 'totalUnitsSold'])[st.values]
    u = u.sort_values('week').drop_duplicates('recordId')
    opening = set(u[(u.week <= 4) & (u.totalUnitsSold >= 100_000)].recordId)
    row = {}
    for y in (1960, 1961):
        x = a[a.year == y]
        if x.week.nunique() < 40: continue
        s = x.groupby('recordId').agg(pts=('pts', 'sum'), fmt=('albumFormat', 'first'), fy=('firstYear', 'first'))
        s = s.sort_values('pts', ascending=False).head(50)
        row[f'{y} top 50: soundtrack %'] = 100 * (s.fmt == 'Soundtrack').mean()
        if y == 1960:
            row['1960 top 50: opening soundtracks (titles)'] = int(s.index.isin(opening).sum())
            row['1960 top 50: 1960-release soundtracks (titles)'] = int(((s.fmt == 'Soundtrack') & ~s.index.isin(opening)).sum())
        if y == 1961:
            row['1961 top 50: soundtrack first charted before 1961 %'] = 100 * ((s.fmt == 'Soundtrack') & (s.fy < 1961)).mean()
            row['1961 top 50: soundtrack first charted 1961 %'] = 100 * ((s.fmt == 'Soundtrack') & (s.fy == 1961)).mean()
            row['1961 top 50: opening 1956-59 soundtracks %'] = 100 * s.index.isin(opening).mean()
    fr = a[st].sort_values('week').drop_duplicates('recordId').set_index('recordId')
    fr['peak'] = a[st].groupby('recordId').position.min()
    for y in (1960, 1961):
        n = fr[(fr.firstYear == y) & ~fr.index.isin(opening)]
        row[f'new {y} soundtracks: charting / reaching top 10'] = f'{len(n)} / {int((n.peak <= 10).sum())}'
    row['opening soundtracks: n'] = len(opening)
    op = a[a.recordId.isin(opening)]
    row['opening soundtracks: best 1960 / 1961 peak'] = f"{op[op.year == 1960].position.min()} / {op[op.year == 1961].position.min()}"
    row['opening soundtracks: chart weeks 1960 / 1961'] = f"{(op.year == 1960).sum()} / {(op.year == 1961).sum()}"
    per = a[st & ~a.recordId.isin(opening)].groupby('recordId').agg(fy=('firstYear', 'first'), peak=('position', 'min'), weeks=('position', 'size'),
                                        w61=('year', lambda v: (v == 1961).sum()))
    for label, p in (('1960 soundtracks', per[per.fy == 1960]), ('1960 top-10 soundtracks', per[(per.fy == 1960) & (per.peak <= 10)])):
        row[f'{label}: n'] = len(p)
        row[f'{label}: median chart weeks'] = p.weeks.median()
        row[f'{label}: charting in 1961 %'] = 100 * (p.w61 > 0).mean() if len(p) else 0
        row[f'{label}: median 1961 weeks'] = p.w61.median()
    print(t)
    print(pd.Series({k: round(v, 1) if isinstance(v, float) else v for k, v in row.items()}).to_string())
    print()
