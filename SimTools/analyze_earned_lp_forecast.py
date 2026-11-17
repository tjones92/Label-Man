"""Earned LP forecast screen (LpGenreAllocationFollowup.md §44). Usage from the repo root:
python SimTools/analyze_earned_lp_forecast.py RUN [RUN ...] [--seeds 2002 1001]
The memory learns from week 53 (LPs at 52 weeks), so the arm differs in 1961 only on a 104-week run. Per tier, 1960 and
1961: LPs chosen (release-strategy chosenFormat Album), gate commit % of AlbumStandalone attempts (release-profit-gates),
and for LPs chosen in 1961 the median gate-forecast net (projectedAlbumNet) and copies sold to week 104."""
import sys, pandas as pd
args = sys.argv[1:]; seeds = ['2002', '1001']
if '--seeds' in args: i = args.index('--seeds'); seeds = args[i + 1:]; args = args[:i]
G = {'Major': 'Major', 'MidTier': 'MidTier', 'Independent': 'Indie', 'Boutique': 'Boutique', 'Small': 'Small'}
res = {}
for run in args:
    st, gt, un = [], [], []
    for s in seeds:
        p = f'SimLogs/{run}-{s}-sc-'
        x = pd.read_csv(p + 'release-strategy.csv', usecols=['week', 'year', 'recordId', 'tier', 'chosenFormat', 'projectedAlbumNet'])
        st.append(x[x.chosenFormat == 'Album'].drop_duplicates('recordId').assign(seed=s))
        f = pd.read_csv(p + 'label-finance.csv', usecols=['week', 'labelId', 'labelTier']).drop_duplicates(['week', 'labelId'])
        tier = f.set_index(['week', 'labelId']).labelTier
        g = pd.read_csv(p + 'release-profit-gates.csv', usecols=['week', 'labelId', 'strategy', 'commit'])
        g = g[g.strategy == 'AlbumStandalone'].copy(); g['tier'] = [tier.get((w, l)) for w, l in zip(g.week, g.labelId)]
        gt.append(g)
        o = pd.read_csv(p + 'release-outcomes.csv', usecols=['recordId', 'format', 'pressingBookSold'])
        v = pd.read_csv(p + 'live-records-snapshot.csv', usecols=['recordId', 'format', 'totalUnitsSold'])
        un.append(pd.concat([o[o.format == 'Album'].rename(columns={'pressingBookSold': 'units'})[['recordId', 'units']],
                             v[v.format == 'Album'].rename(columns={'totalUnitsSold': 'units'})[['recordId', 'units']]]).drop_duplicates('recordId'))
    st = pd.concat(st).merge(pd.concat(un), on='recordId', how='left'); g = pd.concat(gt); n = len(seeds)
    out = {}
    for t, tg in G.items():
        for y in (1960, 1961):
            a = st[(st.tier == t) & (st.year == y)]; w = g[(g.tier == t) & ((g.week - 1) // 52 + 1960 == y)]
            out[f'{tg} LPs chosen {y} (per seed)'] = f'{len(a) / n:7.0f}'
            out[f'{tg} gate commit {y}'] = f'{100 * w.commit.mean():6.0f}%'
        a = st[(st.tier == t) & (st.year == 1961)]
        out[f'{tg} 1961 LPs: median forecast $k / median sold'] = f'{a.projectedAlbumNet.median() / 1e3:6.1f} / {a.units.median():6.0f}'
    res[run] = out
keys = list(res[args[0]].keys())
for k in keys: print(f'{k:48}' + ' | '.join(f'{res[r][k]:>16}' for r in args))
print('columns: ' + ' | '.join(args))
