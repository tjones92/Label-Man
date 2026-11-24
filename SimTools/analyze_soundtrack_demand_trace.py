"""Why new soundtracks reach the album top 10 (LpGenreAllocationFollowup.md §17).

Run from SimLogs/: python ../SimTools/analyze_soundtrack_demand_trace.py slt-k25-2002-sc slt-k25-1001-sc
Needs *-album-demand-trace.csv (--album-demand-trace, with the appeal / regionalAwareness / blendedAwareness columns)
and *-album-chart.csv. Compares new 1960 soundtracks that reached the top 10 with ordinary 1960 LPs that reached the
top 10, by age band, on each store demand term (store raw demand = pool × blended awareness × conversion × store
scale; conversion carries appeal^2.5, exhaustion, box-office or catalogue decay, format tilt and the tier scale).
Opening-catalogue titles (charting by week 4 with 100k+ units sold) are left out.
"""
import pandas as pd, numpy as np, sys

TERMS = ['units', 'rackUnits', 'rawDemand', 'pool', 'blendedAwareness', 'awareness', 'regionalAwareness', 'conversion',
         'appeal', 'exhaustion', 'decay', 'formatTilt', 'wordOfMouth', 'regions']
BANDS = [(1, 4), (5, 8), (9, 12), (13, 20), (21, 40)]
frames = []
for t in sys.argv[1:]:
    c = pd.read_csv(f'{t}-album-chart.csv', usecols=['week', 'year', 'position', 'recordId', 'albumFormat', 'totalUnitsSold'])
    f = c.sort_values('week').drop_duplicates('recordId').set_index('recordId')
    f['peak'] = c.groupby('recordId').position.min()
    opening = set(f[(f.albumFormat == 'Soundtrack') & (f.week <= 4) & (f.totalUnitsSold >= 100_000)].index)
    hits = f[(f.year == 1960) & (f.peak <= 10) & ~f.index.isin(opening)]
    tr = pd.read_csv(f'{t}-album-demand-trace.csv')
    tr = tr[tr.recordId.isin(hits.index) & (tr.age >= 1)].copy()
    tr['kind'] = np.where(tr.albumFormat == 'Soundtrack', 'soundtrack', 'ordinary')
    tr['run'] = t
    frames.append(tr)
    print(t, 'top-10 1960 launches:', hits.albumFormat.eq('Soundtrack').sum(), 'soundtracks,',
          (~hits.albumFormat.eq('Soundtrack')).sum(), 'ordinary')
d = pd.concat(frames)
d['band'] = pd.cut(d.age, [b[0] - 1 for b in BANDS] + [BANDS[-1][1]], labels=[f'{a}-{b}' for a, b in BANDS])
med = d.groupby(['band', 'kind'], observed=True)[TERMS].median()
print('\nMedians by age band (weeks since release):')
print(med.round(3).to_string())
print('\nSoundtrack ÷ ordinary (median ratio):')
ratio = med.xs('soundtrack', level='kind') / med.xs('ordinary', level='kind')
print(ratio.round(2).to_string())
