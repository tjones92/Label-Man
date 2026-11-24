"""Score --act-lp-audience arms (LpGenreAllocationFollowup.md §74). Run from SimLogs:
   python ../SimTools/score_act_lp_audience.py PREFIX [PREFIX ...]     (seeds 1001/2002; needs --album-demand-trace)
Per arm, two-seed means:
- the top 10 singles acts of 1960-61 (by singles units): their LPs launched in the run, median and best store + rack units,
  and the share of those LPs reaching the album top 10;
- the 1961 year-end album top 50 (as analyze_album_head_gap.py): titles by acts with a Top 10 single in the 52 weeks
  before the LP's first chart week ("singles stars"), split youth / other;
- charted LPs (the demand trace covers charted titles; store + rack, launched weeks 1-52) at 300k+ and 1M+.
"""
import os, sys
import pandas as pd

def csv(path, **kw):
    return pd.read_csv(path if os.path.exists(path) else path + '.gz', **kw)

YX = {'TeenPop', 'Bubblegum', 'GirlGroup', 'BritishPop'}
seeds = [int(s) for s in os.environ.get('SEEDS', '1001,2002').split(',')]
out = {}
for pre in sys.argv[1:]:
    rows = []
    for s in seeds:
        t = f'{pre}-{s}-sc'
        cat = csv(f'{t}-genre-catalog.csv', usecols=['genre', 'family'])
        youth = set(cat[cat.family.isin(['Rock', 'RhythmAndSoul'])].genre) | YX
        a = csv(f'{t}-album-chart.csv', usecols=['week', 'year', 'chartSize', 'position', 'recordId', 'artistId', 'genre', 'albumFormat'])
        lpids = set(a.recordId)
        d = csv(f'{t}-album-demand-trace.csv', usecols=['week', 'recordId', 'artistId', 'albumFormat', 'units', 'rackUnits'])
        lpids |= set(d.recordId)
        r = csv(f'{t}-records.csv', usecols=['week', 'recordId', 'artistId', 'unitsThisWeek', 'currentPosition'])
        sg = r[~r.recordId.isin(lpids)]
        top10 = sg[(sg.currentPosition > 0) & (sg.currentPosition <= 10)][['week', 'artistId']].drop_duplicates()
        acts = sg.groupby('artistId').unitsThisWeek.sum().nlargest(10).index
        lp = d[d.albumFormat != 'Soundtrack'].groupby('recordId').agg(artist=('artistId', 'first'), first=('week', 'min'),
            u=('units', 'sum'), rk=('rackUnits', 'sum'))
        lp['tot'] = lp.u + lp.rk
        best = a.groupby('recordId').position.min()
        star = lp[lp.artist.isin(acts)].copy()
        star['top10'] = star.index.map(best).fillna(999) <= 10
        cohort = lp[(lp['first'] >= 1) & (lp['first'] <= 52)]
        a['pts'] = a.chartSize + 1 - a.position
        y = a[a.year == 1961].groupby('recordId').agg(pts=('pts', 'sum'), artist=('artistId', 'first'), genre=('genre', 'first'),
            fmt=('albumFormat', 'first'), w0=('week', 'min')).nlargest(50, 'pts')
        fw = a.groupby('recordId').week.min()
        def is_star(rid, artist):
            w = fw[rid]
            return bool(((top10.artistId == artist) & (top10.week < w) & (top10.week >= w - 52)).any())
        y['star'] = [is_star(rid, x.artist) if isinstance(x.artist, str) and x.fmt != 'Soundtrack' else False for rid, x in y.iterrows()]
        y['youth'] = y.genre.isin(youth) & (y.fmt != 'Soundtrack')
        rows.append({
            'top-10 singles acts: LPs': len(star),
            '... median LP units (k)': star.tot.median() / 1e3,
            '... best LP units (k)': star.tot.max() / 1e3,
            '... share reaching album top 10 %': 100 * star.top10.mean() if len(star) else 0,
            '1961 top 50: singles-star LPs': int(y.star.sum()),
            '... of which youth': int((y.star & y.youth).sum()),
            '1961 top 50: youth LPs': int(y.youth.sum()),
            'charted LPs wk1-52: 300k+ / 1M+ % (per seed)': f"{100 * (cohort.tot >= 3e5).mean():.1f} / {100 * (cohort.tot >= 1e6).mean():.2f}",
            'biggest LP (k)': lp.tot.max() / 1e3,
        })
    df = pd.DataFrame(rows)
    out[pre] = {c: (' ; '.join(map(str, df[c])) if isinstance(df[c].iloc[0], str) else round(float(df[c].mean()), 1)) for c in df.columns}
print(pd.DataFrame(out).to_string())
