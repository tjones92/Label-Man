"""LP package 260-week check (LpGenreAllocationFollowup.md §46). Usage from the repo root:
python SimTools/analyze_lp_package_260.py RUN [RUN ...] [--seeds 2002 1001]
Per year 1960-64, mean of seeds: LP market (stores + racks + budget + clubs), singles, record gross, LPs released by
Majors per Major label (release-campaign-costs rows whose record is an LP, tier from label-finance at release; target
bsnpubs 143-209 titles a year for Capitol / Columbia), small-label share of LP units, and labels defunct (cumulative)."""
import sys, pandas as pd
args = sys.argv[1:]; seeds = ['2002', '1001']
if '--seeds' in args: i = args.index('--seeds'); seeds = args[i + 1:]; args = args[:i]
SMALL = {'Independent', 'Boutique', 'Small'}
def one(run, seed):
    p = f'SimLogs/{run}-{seed}-sc-'; out = {}
    mc = pd.read_csv(p + 'market-clearing-weekly.csv', usecols=['year', 'clearedAlbumUnits', 'clearedSingleUnits'])
    rk = pd.read_csv(p + 'rack-region-weekly.csv', usecols=['year', 'units'])
    bd = pd.read_csv(p + 'budget-region-weekly.csv', usecols=['year', 'budgetUnits'])
    cl = pd.read_csv(p + 'record-club-annual.csv', usecols=['year', 'role', 'units']); cl = cl[cl.role != 'Licensor']
    roll = pd.read_csv(p + 'decade-annual-rollup.csv').set_index('year')
    f = pd.read_csv(p + 'label-finance.csv', usecols=['week', 'labelId', 'labelTier', 'status'])
    tier = f.drop_duplicates(['week', 'labelId']).set_index(['week', 'labelId']).labelTier
    dead = f[f.status == 'Defunct'].groupby('labelId').week.min()
    st = pd.read_csv(p + 'release-strategy.csv', usecols=['week', 'year', 'recordId', 'labelId', 'chosenFormat'])
    lp = st[st.chosenFormat == 'Album'].drop_duplicates('recordId')
    c = pd.read_csv(p + 'release-campaign-costs.csv', usecols=['week', 'recordId', 'labelId'])
    c = c[c.recordId.isin(set(lp.recordId))].copy(); c['year'] = (c.week - 1) // 52 + 1960
    c['tier'] = [tier.get((w, l)) for w, l in zip(c.week, c.labelId)]
    s = pd.read_csv(p + 'label-sales-weekly.csv', usecols=['year', 'labelId', 'format', 'units'])
    d = pd.read_csv(p + 'label-directory.csv').set_index('labelId'); s['small'] = s.labelId.map(d.initialTier).isin(SMALL)
    for y in range(1960, 1965):
        tot = (mc[mc.year == y].clearedAlbumUnits.sum() + rk[rk.year == y].units.sum() + bd[bd.year == y].budgetUnits.sum()
               + cl[cl.year == y].units.sum())
        out[f'{y} LP market (M)'] = tot / 1e6
        out[f'{y} singles (M)'] = mc[mc.year == y].clearedSingleUnits.sum() / 1e6
        out[f'{y} record gross ($M)'] = (roll.loc[y, 'singleGross'] + roll.loc[y, 'albumGross']) / 1e6 if y in roll.index else float('nan')
        wk = f[(f.week - 1) // 52 + 1960 == y]; majors = wk[wk.labelTier == 'Major'].labelId.nunique()
        out[f'{y} Major LPs released per Major label'] = len(c[(c.year == y) & (c.tier == 'Major')]) / max(majors, 1)
        out[f'{y} LPs released, all labels'] = len(c[c.year == y])
        z = s[(s.year == y) & (s.format == 'Album')]
        out[f'{y} small-label share of LP units %'] = 100 * z[z.small].units.sum() / z.units.sum()
        out[f'{y} labels defunct (cumulative)'] = int((dead <= (y - 1959) * 52).sum())
    return out
res = {r: [one(r, s) for s in seeds] for r in args}
for k in res[args[0]][0]:
    print(f'{k:44}' + ' | '.join(f'{sum(o[k] for o in res[r]) / len(seeds):8.1f} ({" / ".join(f"{o[k]:.1f}" for o in res[r])})' for r in args))
print('columns: ' + ' | '.join(args) + '; mean (per seed ' + ' / '.join(seeds) + ')')
