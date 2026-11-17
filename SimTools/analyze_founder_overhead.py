"""--graduated-founder-overhead screen (LpGenreAllocationFollowup.md §36-37). Usage from the repo root:
python SimTools/analyze_founder_overhead.py CONTROL ARM [ARM ...] [--seeds 2002 1001 3003]
Per run, three-seed means: procedural opening overhead and two-year closure by tier, closure by opening overhead band
(<=$100 vs above), singles released per tier and year, unit and Hot 100 record-week shares (small labels = procedural +
template Independent / Boutique / Small), and the Big-5 (template Majors) share of Top 10 record-weeks."""
import sys, pandas as pd
args = sys.argv[1:]; seeds = ['2002', '1001', '3003']
if '--seeds' in args: i = args.index('--seeds'); seeds = args[i + 1:]; args = args[:i]
TIERS = ['Independent', 'Boutique', 'Small', 'MidTier']
SMALL = {'Independent', 'Boutique', 'Small'}

def one(run, seed):
    p = f'SimLogs/{run}-{seed}-sc-'
    d = pd.read_csv(p + 'label-directory.csv').set_index('labelId')
    f = pd.read_csv(p + 'label-finance.csv', usecols=['week', 'labelId', 'status', 'monthlyOverhead'])
    w1 = f[f.week == 1].set_index('labelId')
    dead = f[f.status == 'Defunct'].groupby('labelId').week.min()
    out = {}
    for t in TIERS:
        ids = d[(d.initialTier == t) & (~d.isHistorical)].index
        oh = w1.monthlyOverhead.reindex(ids)
        for y, wk in ((1960, 52), (1961, 104)):
            out[f'{t} defunct {y} %'] = 100 * sum(1 for l in ids if l in dead.index and dead[l] <= wk) / max(len(ids), 1)
        out[f'{t} opening overhead median $'] = oh.median()
        if t in ('Independent', 'Boutique'):
            for band, sel in (('<=$100', oh <= 100), ('>$100', oh > 100)):
                b = oh[sel].index
                out[f'{t} {band}: n'] = len(b)
                out[f'{t} {band}: defunct 1961 %'] = 100 * sum(1 for l in b if l in dead.index and dead[l] <= 104) / max(len(b), 1)
    out['all labels defunct 1961'] = sum(1 for w in dead if w <= 104)
    ro = pd.read_csv(p + 'release-outcomes.csv', usecols=['year', 'labelId', 'format'])
    ro = ro[ro.format == 'Single']
    ro['tier'] = ro.labelId.map(d.initialTier); ro['proc'] = ~ro.labelId.map(d.isHistorical).fillna(True).astype(bool)
    for y in (1960, 1961):
        for t in ('Independent', 'Boutique', 'Small'):
            out[f'singles {y} procedural {t}'] = ((ro.year == y) & (ro.tier == t) & ro.proc).sum()
    s = pd.read_csv(p + 'label-sales-weekly.csv', usecols=['year', 'labelId', 'format', 'units'])
    s = s[s.format == 'Single']; s['tier'] = s.labelId.map(d.initialTier)
    r = pd.read_csv(p + 'records.csv', usecols=['year', 'labelId', 'currentPosition'])
    r = r[(r.currentPosition >= 1) & (r.currentPosition <= 100)]
    r['tier'] = r.labelId.map(d.initialTier)
    big5 = set(d[(d.initialTier == 'Major') & d.isHistorical].index)
    for y in (1960, 1961):
        sy, ry = s[s.year == y], r[r.year == y]
        out[f'small-label unit share {y} %'] = 100 * sy[sy.tier.isin(SMALL)].units.sum() / sy.units.sum()
        out[f'small-label Hot 100 rec-wks {y} %'] = 100 * ry.tier.isin(SMALL).mean()
        t10 = ry[ry.currentPosition <= 10]
        out[f'Big-5 Top 10 rec-wks {y} %'] = 100 * t10.labelId.isin(big5).mean()
    return out

runs = {run: pd.DataFrame({sd: one(run, sd) for sd in seeds}) for run in args}
ctrl = args[0]
for key in runs[ctrl].index:
    cells = []
    for run in args:
        v = runs[run].loc[key]
        cells.append(f'{v.mean():8.1f} ({" / ".join(f"{x:.0f}" for x in v)})')
    print(f'{key:42}' + ' | '.join(cells))
print('columns: ' + ' | '.join(args) + '; mean (per seed ' + ' / '.join(seeds) + ')')
