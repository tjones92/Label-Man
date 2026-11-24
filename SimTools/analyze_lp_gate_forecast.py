"""Major LP gate forecast against realized LP net (LpGenreAllocationFollowup.md §43). Usage from the repo root:
python SimTools/analyze_lp_gate_forecast.py RUN [RUN ...] [--seeds 2002 1001]
1) release-profit-gates AlbumStandalone rows (one per gate attempt) by tier and career state: attempts, commit %, median
albumProfit. 2) LPs released in weeks 1-52: release-campaign-costs.releaseTimeExpectedNet (the forecast at release)
against realized net to week 104 (retired: release-outcomes.realizedNet; live: lifetimeLabelNet - sunkProductionCost).
Tier = the label's tier in label-finance at the gate / release week."""
import sys, numpy as np, pandas as pd
args = sys.argv[1:]; seeds = ['2002', '1001']
if '--seeds' in args: i = args.index('--seeds'); seeds = args[i + 1:]; args = args[:i]
G = {'Major': 'Major', 'MidTier': 'MidTier', 'Independent': 'Indie', 'Boutique': 'Boutique', 'Small': 'Small'}
for run in args:
    gates, lps = [], []
    for s in seeds:
        p = f'SimLogs/{run}-{s}-sc-'
        f = pd.read_csv(p + 'label-finance.csv', usecols=['week', 'labelId', 'labelTier']).drop_duplicates(['week', 'labelId'])
        tier = f.set_index(['week', 'labelId']).labelTier
        g = pd.read_csv(p + 'release-profit-gates.csv'); g = g[g.strategy == 'AlbumStandalone'].copy()
        g['tier'] = [G.get(tier.get((w, l)), 'other') for w, l in zip(g.week, g.labelId)]; gates.append(g)
        c = pd.read_csv(p + 'release-campaign-costs.csv'); c = c[c.week <= 52]
        o = pd.read_csv(p + 'release-outcomes.csv', usecols=['recordId', 'format', 'realizedNet', 'pressingBookSold'])
        v = pd.read_csv(p + 'live-records-snapshot.csv', usecols=['week', 'recordId', 'format', 'lifetimeLabelNet', 'sunkProductionCost', 'totalUnitsSold', 'artistId'])
        v = v[v.week == v.week.max()]
        r = pd.concat([o[o.format == 'Album'].rename(columns={'pressingBookSold': 'units'})[['recordId', 'realizedNet', 'units']],
                       v[v.format == 'Album'].assign(realizedNet=v.lifetimeLabelNet - v.sunkProductionCost, units=v.totalUnitsSold)
                       [['recordId', 'realizedNet', 'units']]]).drop_duplicates('recordId')
        m = c.merge(r, on='recordId')
        m['tier'] = [G.get(tier.get((w, l)), 'other') for w, l in zip(m.week, m.labelId)]; lps.append(m)
    g = pd.concat(gates); m = pd.concat(lps)
    print(f'== {run} (seeds {"/".join(seeds)}): LP gate attempts by tier x career state: n, commit %, median albumProfit $k')
    for t in ('Major', 'MidTier', 'Indie'):
        gt = g[g.tier == t]
        cells = [f'{cs} {len(x)} {100 * x.commit.mean():.0f}% {x.albumProfit.median() / 1e3:.1f}'
                 for cs, x in gt.groupby('careerState') if len(x) >= 20]
        print(f'  {t:8} all {len(gt)} {100 * gt.commit.mean():.0f}% {gt.albumProfit.median() / 1e3:.1f} | ' + ' | '.join(cells))
    print('  LPs released 1960: n, median forecast $k, median realized $k, realized/forecast (sums), share forecast<0, share realized<0')
    for t in ('Major', 'MidTier', 'Indie', 'Boutique', 'Small'):
        x = m[m.tier == t]
        if x.empty: continue
        print(f'  {t:8} {len(x):5} {x.releaseTimeExpectedNet.median() / 1e3:8.1f} {x.realizedNet.median() / 1e3:8.1f} '
              f'{x.realizedNet.sum() / max(x.releaseTimeExpectedNet.sum(), 1):6.2f} {100 * (x.releaseTimeExpectedNet < 0).mean():5.0f}% {100 * (x.realizedNet < 0).mean():5.0f}%')
