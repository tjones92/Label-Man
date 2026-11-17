"""Census population and adult-weighted LP purse screen (LpGenreAllocationFollowup.md §29).

Run from SimLogs/: python ../SimTools/analyze_census_population.py bsc13 cen cenad [--seeds 2002 1001 3003]
Per run prefix and seed, 1961 (1960 in brackets where useful): singles and LP units by channel, envelope use,
each region's share of singles and store LPs, Hot 100 year-end genre shares for the rows a larger South moves,
Southern labels alive at the end, and defunct labels.
"""
import pandas as pd, sys, os
args = sys.argv[1:]; seeds = [2002, 1001, 3003]
if '--seeds' in args:
    i = args.index('--seeds'); seeds = [int(s) for s in args[i + 1:]]; args = args[:i]
REG = ['eastcoast', 'greatlakes', 'westcoast', 'greatplains', 'deepsouth', 'southwest', 'rockies']
M = 1e6
for prefix in args:
    print(f'== {prefix}')
    for seed in seeds:
        t = f'{prefix}-{seed}-sc'
        if not os.path.exists(f'{t}-decade-annual-rollup.csv'): print(f'  {seed}: missing'); continue
        r = pd.read_csv(f'{t}-decade-annual-rollup.csv').set_index('year')
        rack = pd.read_csv(f'{t}-rack-region-weekly.csv'); bud = pd.read_csv(f'{t}-budget-region-weekly.csv')
        club = pd.read_csv(f'{t}-record-club-annual.csv'); club = club[club.role != 'Licensor']
        env = pd.read_csv(f'{t}-retail-purchase-envelope.csv')
        mc = pd.read_csv(f'{t}-market-clearing-weekly.csv', usecols=['year', 'regionId', 'clearedSingleUnits', 'clearedAlbumUnits'])
        out = []
        for y in (1960, 1961):
            e = env[env.year == y]
            out.append(f'{y}: singles {r.loc[y, "singleUnits"] / M:.1f} stores {r.loc[y, "albumUnits"] / M:.1f} '
                       f'racks {rack[rack.year == y].units.sum() / M:.1f} budget {bud[bud.year == y].budgetUnits.sum() / M:.1f} '
                       f'clubs {club[club.year == y].units.sum() / M:.1f} | purse {e.budget.sum() / M:.1f} unspent {e.unspent.sum() / M:.1f} '
                       f'bound {100 * (e.unspent <= .01 * e.budget).mean():.0f}%')
        m = mc[mc.year == 1961].groupby('regionId')[['clearedSingleUnits', 'clearedAlbumUnits']].sum()
        m = 100 * m / m.sum()
        reg = ' '.join(f'{x[:6]} {m.loc[x, "clearedSingleUnits"]:.0f}/{m.loc[x, "clearedAlbumUnits"]:.0f}' for x in REG if x in m.index)
        h = pd.read_csv(f'{t}-year-end-hot100.csv'); h = h[h.year == 1961]
        fam = h.groupby('family').yearEndSlots.sum()
        gen = h.groupby('genre').yearEndSlots.sum()
        geo = pd.read_csv(f'{t}-label-geography.csv', usecols=['labelId', 'homeRegion'])
        fin = pd.read_csv(f'{t}-label-finance.csv', usecols=['labelId', 'status']).groupby('labelId').tail(1)
        fin = fin.merge(geo, on='labelId', how='left')
        south = fin[fin.homeRegion == 'deepsouth']
        print(f'  {seed}  ' + '\n        '.join(out))
        print(f'        1961 region share singles/store LPs %: {reg}')
        print(f'        1961 Hot 100 slots by family: ' + ', '.join(f'{k} {v}' for k, v in fam.sort_values(ascending=False).items()) +
              f' | Country {gen.get("Country", 0)}')
        print(f'        defunct {int((fin.status == "Defunct").sum())}; Deep South labels {len(south)}, defunct {int((south.status == "Defunct").sum())}')
