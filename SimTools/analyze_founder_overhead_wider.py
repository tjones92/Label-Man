"""Economy and chart-health side effects of --graduated-founder-overhead (LpGenreAllocationFollowup.md §38).
Usage from the repo root: python SimTools/analyze_founder_overhead_wider.py CONTROL ARM [--seeds 2002 1001 3003]
Economy, per year: market single / album units and gross, rationing, singles released, money-losing singles, and per
tier group (by the label's tier that month): record gross share, pretax earnings, payroll + premises, owner salary.
Chart, per year: Top 10 label CR4 / firms / one-record firms (slots), Top 10 and Hot 100 distinct records, mean
Hot 100 weeks per record, #1 records, against Billboard 1960-61 (historical-top10-*, historical-hot100-1960-64)."""
import sys, pandas as pd
args = sys.argv[1:]; seeds = ['2002', '1001', '3003']
if '--seeds' in args: i = args.index('--seeds'); seeds = args[i + 1:]; args = args[:i]
GROUP = {'Major': 'Major', 'MidTier': 'MidTier', 'Independent': 'Indie', 'Boutique': 'Boutique', 'Small': 'Small'}

def one(run, seed):
    p = f'SimLogs/{run}-{seed}-sc-'; out = {}
    roll = pd.read_csv(p + 'decade-annual-rollup.csv').set_index('year')
    mc = pd.read_csv(p + 'market-clearing-weekly.csv', usecols=['year', 'rationingFactor', 'clearedTotalUnits', 'purchaseCapacity'])
    ro = pd.read_csv(p + 'release-outcomes.csv', usecols=['year', 'format', 'realizedNet'])
    cm = pd.read_csv(p + 'company-monthly.csv', usecols=['year', 'tier', 'active', 'recordGross', 'pretaxEarnings', 'payroll',
                                                         'premises', 'ownerSalary', 'closingCash', 'month'])
    cm['g'] = cm.tier.map(GROUP)
    r = pd.read_csv(p + 'records.csv', usecols=['year', 'week', 'recordId', 'labelId', 'currentPosition'])
    r = r[(r.currentPosition >= 1) & (r.currentPosition <= 100)]
    for y in (1960, 1961):
        out[f'E {y} single units M'] = roll.loc[y, 'singleUnits'] / 1e6
        out[f'E {y} album units M'] = roll.loc[y, 'albumUnits'] / 1e6
        out[f'E {y} single + album gross $M'] = (roll.loc[y, 'singleGross'] + roll.loc[y, 'albumGross']) / 1e6
        m = mc[mc.year == y]
        out[f'E {y} cleared / purchase capacity %'] = 100 * m.clearedTotalUnits.sum() / m.purchaseCapacity.sum()
        out[f'E {y} mean rationing factor'] = m.rationingFactor.mean()
        s = ro[(ro.year == y) & (ro.format == 'Single')]
        out[f'E {y} singles released'] = len(s)
        out[f'E {y} singles losing money %'] = 100 * (s.realizedNet < 0).mean()
        c = cm[cm.year == y]; tot = c.recordGross.sum()
        for g in ('Major', 'MidTier', 'Indie', 'Boutique', 'Small'):
            cg = c[c.g == g]
            out[f'E {y} {g} gross share %'] = 100 * cg.recordGross.sum() / tot
            out[f'E {y} {g} pretax $M'] = cg.pretaxEarnings.sum() / 1e6
            out[f'E {y} {g} payroll+premises $M'] = (cg.payroll.sum() + cg.premises.sum()) / 1e6
            out[f'E {y} {g} owner salary $M'] = cg.ownerSalary.sum() / 1e6
            dec = cg[(cg.month == c.month.max()) & (cg.active.astype(str) == 'True')]
            out[f'E {y} {g} active firms last month'] = len(dec)
            out[f'E {y} {g} median cash last month $k'] = dec.closingCash.median() / 1e3
        ry = r[r.year == y]; t10 = ry[ry.currentPosition <= 10]
        slots = t10.groupby('labelId').size().sort_values(ascending=False)
        recs = t10.groupby('labelId').recordId.nunique()
        out[f'C {y} T10 label CR4 slots %'] = 100 * slots.head(4).sum() / slots.sum()
        out[f'C {y} T10 label CR8 slots %'] = 100 * slots.head(8).sum() / slots.sum()
        out[f'C {y} T10 firms'] = len(slots)
        out[f'C {y} T10 one-record firms'] = int((recs == 1).sum())
        out[f'C {y} T10 records'] = t10.recordId.nunique()
        out[f'C {y} H100 records'] = ry.recordId.nunique()
        out[f'C {y} H100 weeks per record'] = len(ry) / ry.recordId.nunique()
        out[f'C {y} #1 records'] = ry[ry.currentPosition == 1].recordId.nunique()
    return out

def history():
    h = pd.read_csv('SimLogs/historical-hot100-1960-64/hot100_weekly.csv')
    h['year'] = h.chartDate.str[:4].astype(int); h['rec'] = h.title + '|' + h.artist
    out = {}
    for y in (1960, 1961):
        hy = h[h.year == y]
        conc = pd.read_csv(f'SimLogs/historical-top10-{y}/concentration_{y}.csv')
        c = conc[(conc.records == 'all') & (conc.grouping == 'label') & (conc.unit == 'slots')].iloc[0]
        out[f'C {y} T10 label CR4 slots %'] = c.CR4; out[f'C {y} T10 label CR8 slots %'] = c.CR8
        out[f'C {y} T10 firms'] = c.firms; out[f'C {y} T10 one-record firms'] = c.oneHitFirms
        out[f'C {y} T10 records'] = hy[hy.position <= 10].rec.nunique()
        out[f'C {y} H100 records'] = hy.rec.nunique()
        out[f'C {y} H100 weeks per record'] = len(hy) / hy.rec.nunique()
        out[f'C {y} #1 records'] = hy[hy.position == 1].rec.nunique()
    return out

runs = {run: pd.DataFrame({sd: one(run, sd) for sd in seeds}) for run in args}
hist = history()
for key in runs[args[0]].index:
    cells = [f'{runs[run].loc[key].mean():9.2f} ({" / ".join(f"{x:.1f}" for x in runs[run].loc[key])})' for run in args]
    print(f'{key:42}' + ' | '.join(cells) + (f' | hist {hist[key]:.1f}' if key in hist else ''))
print('columns: ' + ' | '.join(args) + '; mean (per seed ' + ' / '.join(seeds) + ')')
