"""Small-label share by label home region (LpGenreAllocationFollowup.md §35). Usage from the repo root:
python SimTools/analyze_region_small_share.py RUN [RUN ...] [--seeds 2002 1001] [--year 1960]
Per run/seed and label home region: opening small labels (count share), small-label Hot 100 record-weeks (share of all
small), small-label single units (share), home fraction of all / uncharted units, and the region's share of buyers."""
import sys, pandas as pd
args = sys.argv[1:]; seeds = ['2002', '1001']; year = 1960
if '--year' in args: i = args.index('--year'); year = int(args[i + 1]); args = args[:i] + args[i + 2:]
if '--seeds' in args: i = args.index('--seeds'); seeds = args[i + 1:]; args = args[:i]
SMALL = ['Independent', 'Small', 'Boutique']
for run in args:
    for seed in seeds:
        p = f'SimLogs/{run}-{seed}-sc-'
        geo = pd.read_csv(p + 'label-geography.csv', usecols=['labelId', 'homeRegion'])
        try: d = pd.read_csv(p + 'label-directory.csv', usecols=['labelId', 'isHistorical', 'initialTier'])
        except pd.errors.EmptyDataError:  # written at run end; mid-run, read week-1 tiers from label-finance
            f = pd.read_csv(p + 'label-finance.csv', usecols=['week', 'labelId', 'labelTier'], nrows=3000)
            d = f[f.week == f.week.min()].rename(columns={'labelTier': 'initialTier'})
        op = d[d.initialTier.isin(SMALL)].merge(geo, on='labelId')
        r = pd.read_csv(p + 'records.csv', usecols=['year', 'labelId', 'labelTier', 'currentPosition'])
        r = r[(r.year == year) & (r.currentPosition > 0) & (r.currentPosition <= 100)].merge(geo, on='labelId', how='left')
        rs = r[r.labelTier.isin(SMALL)]
        rh = pd.read_csv(p + 'regional-home-sales.csv')
        rh = rh[(rh.year == year) & (rh.format == 'Single')]
        buyers = rh.groupby('regionId').rawDemand.sum(); buyers = buyers / buyers.sum()
        s = rh[rh.labelTier.isin(SMALL)]
        out = []
        for reg in sorted(buyers.index):
            x = s[s.labelHomeRegion == reg]; u = x.units.sum(); nc = x[x.peakClass == 'none']
            out.append({'region': reg, 'labels%': 100 * (op.homeRegion == reg).mean(),
                        'H100%': 100 * (rs.homeRegion == reg).mean(), 'units%': 100 * u / s.units.sum(),
                        'home%': 100 * x[x.regionId == reg].units.sum() / max(1, u),
                        'unchartedHome%': 100 * nc[nc.regionId == reg].units.sum() / max(1, nc.units.sum()),
                        'demand%': 100 * buyers[reg],
                        'smallShareOfH100': 100 * len(rs[rs.homeRegion == reg]) / len(r)})
        print(f'== {run} {seed} {year}: small labels {100 * len(rs) / len(r):.0f}% of Hot 100 record-weeks')
        print(pd.DataFrame(out).set_index('region').round(1).to_string())
