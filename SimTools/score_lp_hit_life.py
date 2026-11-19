"""Score --lp-hit-life-top / --lp-hit-life-share arms (LpGenreAllocationFollowup.md §77, §73 stage B). Run from SimLogs:
   python ../SimTools/score_lp_hit_life.py PREFIX [PREFIX ...]        (SEEDS=1001,2002 by default; runs PREFIX-SEED-sc)
Per arm, seed means, non-soundtrack LPs ("artist LPs": every format but Soundtrack) unless stated:
- slots held by titles 40+ / 20+ weeks on the chart, all titles and artist LPs only (1960, 1961; Billboard Jan 1960 38% of
  65 listings, ~18 evergreen non-soundtrack titles in the top 65);
- the same 40+ share over the run's last 26 weeks (a 1960 release needs most of a year to reach 40 weeks; Billboard
  Aug / Nov 1963 top 50: artist 16 / 20%, all 26 / 28%);
- artist LPs reaching 40 chart weeks (distinct, debuting in the run);
- median total chart weeks of artist LPs debuting weeks 2-40 (D7's 12-18 week median) and of those reaching the top 10;
- top-10 artist LPs: median weeks in the top 10, and units at chart weeks 10 / 20 / 30 as a share of their best week;
- No. 1 titles and the longest No. 1 run per year;
- artist LPs launched weeks 1-52 at 300k+ / 1M+ store + rack by week 104 (demand trace; charted titles).
"""
import os
import sys

import pandas as pd


def csv(path, **kw):
    return pd.read_csv(path if os.path.exists(path) else path + '.gz', **kw)


def longest(ids):
    best = cur = 0
    prev = None
    for i in ids:
        cur = cur + 1 if i == prev else 1
        prev = i
        best = max(best, cur)
    return best


seeds = [int(s) for s in os.environ.get('SEEDS', '1001,2002').split(',')]
table = {}
for pre in sys.argv[1:]:
    rows = []
    for s in seeds:
        t = f'{pre}-{s}-sc'
        a = csv(f'{t}-album-chart.csv', usecols=['week', 'year', 'position', 'recordId', 'albumFormat', 'unitsThisWeek', 'weeksOnChart'])
        a['artist'] = a.albumFormat != 'Soundtrack'
        first = a.groupby('recordId').week.min()
        a['first'] = a.recordId.map(first)
        a['idx'] = a.week - a['first'] + 1
        run0 = a.week.min()
        total = a.groupby('recordId').size()
        art = a[a.artist]
        fresh = art[art['first'] > run0]
        r = {}
        for y in (1960, 1961):
            x = a[a.year == y]
            r[f'old40 {y}'] = 100 * (x.weeksOnChart >= 40).mean()
            r[f'old40 artist {y}'] = 100 * ((x.weeksOnChart >= 40) & x.artist).mean()
            r[f'old20 artist {y}'] = 100 * ((x.weeksOnChart >= 20) & x.artist).mean()
            no1 = x[x.position == 1].sort_values('week').recordId
            r[f'No. 1s {y}'] = no1.nunique()
            r[f'longest No. 1 {y}'] = longest(list(no1))
        h2 = a[a.week >= a.week.max() - 25]
        r['old40 artist, last 26 wk'] = 100 * ((h2.weeksOnChart >= 40) & h2.artist).mean()
        r['old40 all, last 26 wk'] = 100 * (h2.weeksOnChart >= 40).mean()
        r['artist LPs 40+ chart wk'] = fresh[fresh.idx >= 40].recordId.nunique()
        early = fresh[(fresh['first'] >= run0 + 1) & (fresh['first'] <= 40)].recordId.unique()
        r['median run, artist LPs debuting wk 2-40'] = float(total.reindex(early).median())
        t10 = fresh[fresh.position <= 10].recordId.unique()
        t10early = [i for i in t10 if i in set(early)]
        r['median run, their top-10 titles'] = float(total.reindex(t10early).median()) if t10early else float('nan')
        r['top-10 artist LPs (debut in run)'] = len(t10)
        r['median wk in top 10'] = float(fresh[fresh.position <= 10].groupby('recordId').size().median())
        peak = fresh[fresh.recordId.isin(t10)].groupby('recordId').unitsThisWeek.max()
        for k in (10, 20, 30):
            u = fresh[fresh.recordId.isin(t10) & (fresh.idx == k)].set_index('recordId').unitsThisWeek
            # Titles off the chart by week k count as 0 (their chart life ended); titles debuting too late are dropped.
            alive = [i for i in t10 if first[i] + k - 1 <= a.week.max()]
            r[f'units wk {k} / best'] = float((u.reindex(alive).fillna(0) / peak.reindex(alive)).median()) if alive else float('nan')
        d = csv(f'{t}-album-demand-trace.csv', usecols=['week', 'recordId', 'albumFormat', 'units', 'rackUnits'])
        d = d[(d.week <= 104) & (d.albumFormat != 'Soundtrack')]
        launch = d.groupby('recordId').week.min()
        tot = (d.units + d.rackUnits).groupby(d.recordId).sum()
        coh = tot[launch.reindex(tot.index).between(2, 52)]
        r['artist LPs 300k+ (n)'] = int((coh >= 300_000).sum())
        r['artist LPs 1M+ (n)'] = int((coh >= 1_000_000).sum())
        r['biggest artist LP (k)'] = tot.max() / 1000
        rows.append(r)
    table[pre] = pd.DataFrame(rows).mean()
out = pd.DataFrame(table)
pd.set_option('display.width', 200)
print(out.round(2).to_string())
