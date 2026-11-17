"""LP flop-share screen guards (LpGenreAllocationFollowup.md §41). Usage from the repo root:
python SimTools/analyze_lp_flop_screen.py CONTROL ARM [ARM ...] [--seeds 2002 1001]
Per run, means (per seed): LP market by channel per year (stores = market-clearing clearedAlbumUnits, racks =
rack-region-weekly units, budget = budget-region-weekly budgetUnits, clubs = record-club-annual without Licensor rows;
`lp-units-need-all-four-channels`), record gross, procedural opening-cohort closures by tier at week 104, all labels
defunct, small-label share of LP and single units, and the share of surviving procedural Independents with no Hot 100
record in the second year."""
import sys, pandas as pd
args = sys.argv[1:]; seeds = ['2002', '1001']
if '--seeds' in args: i = args.index('--seeds'); seeds = args[i + 1:]; args = args[:i]
SMALL = {'Independent', 'Boutique', 'Small'}

def one(run, seed):
    p = f'SimLogs/{run}-{seed}-sc-'; out = {}
    mc = pd.read_csv(p + 'market-clearing-weekly.csv', usecols=['year', 'clearedAlbumUnits', 'clearedSingleUnits'])
    rk = pd.read_csv(p + 'rack-region-weekly.csv', usecols=['year', 'units'])
    bd = pd.read_csv(p + 'budget-region-weekly.csv', usecols=['year', 'budgetUnits'])
    cl = pd.read_csv(p + 'record-club-annual.csv', usecols=['year', 'role', 'units'])
    cl = cl[cl.role != 'Licensor']
    roll = pd.read_csv(p + 'decade-annual-rollup.csv').set_index('year')
    for y in sorted(mc.year.unique()):
        s, r, b, c = (mc[mc.year == y].clearedAlbumUnits.sum(), rk[rk.year == y].units.sum(),
                      bd[bd.year == y].budgetUnits.sum(), cl[cl.year == y].units.sum())
        out[f'LP {y} stores / racks / budget / clubs (M)'] = f'{s / 1e6:.1f} / {r / 1e6:.1f} / {b / 1e6:.1f} / {c / 1e6:.1f}'
        out[f'LP {y} total (M)'] = (s + r + b + c) / 1e6
        out[f'Singles {y} (M)'] = mc[mc.year == y].clearedSingleUnits.sum() / 1e6
        out[f'Single + album gross {y} ($M)'] = (roll.loc[y, 'singleGross'] + roll.loc[y, 'albumGross']) / 1e6
    d = pd.read_csv(p + 'label-directory.csv').set_index('labelId')
    f = pd.read_csv(p + 'label-finance.csv', usecols=['week', 'labelId', 'status'])
    dead = f[f.status == 'Defunct'].groupby('labelId').week.min()
    for t in ('Independent', 'Boutique', 'Small', 'MidTier'):
        ids = d[(d.initialTier == t) & (~d.isHistorical)].index
        out[f'{t} defunct by wk 104 %'] = 100 * sum(1 for l in ids if l in dead.index and dead[l] <= 104) / len(ids)
    out['All labels defunct by wk 104'] = int((dead <= 104).sum())
    s = pd.read_csv(p + 'label-sales-weekly.csv', usecols=['year', 'labelId', 'format', 'units'])
    s['small'] = s.labelId.map(d.initialTier).isin(SMALL)
    for y in (1960, 1961):
        for fmt in ('Single', 'Album'):
            z = s[(s.year == y) & (s.format == fmt)]
            out[f'Small-label share of {fmt} units {y} %'] = 100 * z[z.small].units.sum() / z.units.sum()
    r = pd.read_csv(p + 'records.csv', usecols=['year', 'labelId', 'currentPosition'])
    charted = set(r[(r.year == 1961) & (r.currentPosition >= 1) & (r.currentPosition <= 100)].labelId)
    alive = [l for l in d[(d.initialTier == 'Independent') & (~d.isHistorical)].index if not (l in dead.index and dead[l] <= 104)]
    out['Surviving proc. Independents with no 1961 Hot 100 record %'] = 100 * sum(l not in charted for l in alive) / max(len(alive), 1)
    return out

res = {run: {sd: one(run, sd) for sd in seeds} for run in args}
keys = list(res[args[0]][seeds[0]].keys())
for k in keys:
    cells = []
    for run in args:
        vals = [res[run][sd].get(k) for sd in seeds]
        if isinstance(vals[0], str): cells.append(' ; '.join(vals))
        else: cells.append(f'{sum(vals) / len(vals):7.1f} ({" / ".join(f"{v:.1f}" for v in vals)})')
    print(f'{k:58}' + ' | '.join(cells))
print('columns: ' + ' | '.join(args) + '; mean (per seed ' + ' / '.join(seeds) + ')')
