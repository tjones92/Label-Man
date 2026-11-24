"""Southern home-share decomposition (LpGenreAllocationFollowup.md §33). Usage from the repo root:
python SimTools/analyze_home_share_split.py RUN [RUN ...] [--seeds 2002 1001] [--region deepsouth] [--year 1961]
Needs --regional-home-sales with the peakClass / pdDeal columns. Per run and seed: a region's small labels' single units
by the record's peak Hot 100 class to date (T40 / H100 / none) and by whether the label held a P&D deal, with each
cell's home fraction and the record-weeks in play at home and away."""
import sys, pandas as pd
args = sys.argv[1:]; seeds = ['2002', '1001']; region = 'deepsouth'; year = 1961
def opt(name):
    global args
    if name not in args: return None
    i = args.index(name); j = i + 1
    while j < len(args) and not args[j].startswith('--'): j += 1
    v = args[i + 1:j]; args = args[:i] + args[j:]; return v
seeds = opt('--seeds') or seeds; region = (opt('--region') or [region])[0]; year = int((opt('--year') or [year])[0])
SMALL = ['Independent', 'Small', 'Boutique']
for run in args:
    for seed in seeds:
        rh = pd.read_csv(f'SimLogs/{run}-{seed}-sc-regional-home-sales.csv')
        x = rh[(rh.year == year) & (rh.format == 'Single') & (rh.labelHomeRegion == region) & rh.labelTier.isin(SMALL)].copy()
        x['home'] = x.regionId == region
        tot = x.units.sum(); print(f'== {run} {seed}: {region} small singles {year} {tot / 1e6:.2f}M, home {100 * x[x.home].units.sum() / tot:.0f}%')
        for by in ('peakClass', 'pdDeal'):
            g = x.groupby([by, 'home'])[['units', 'recordsInPlay']].sum().unstack('home', fill_value=0)
            for k, r in g.iterrows():
                u = r.units.sum()
                print(f'   {by}={k}: units {u / 1e6:.2f}M ({100 * u / tot:.0f}%), home {100 * r.units[True] / max(1, u):.0f}%,'
                      f' record-weeks home {r.recordsInPlay[True]:.0f} away {r.recordsInPlay[False]:.0f},'
                      f' units/rec-wk home {r.units[True] / max(1, r.recordsInPlay[True]):.0f} away {r.units[False] / max(1, r.recordsInPlay[False]):.0f}')
        # Per tier: home fraction of all and of uncharted units, and the share of away region-weeks in play (§34).
        for t, g in x.groupby('labelTier'):
            nc = g[g.peakClass == 'none']; a = nc[~nc.home]
            print(f'   {t}: units {g.units.sum() / 1e6:.2f}M home {100 * g[g.home].units.sum() / max(1, g.units.sum()):.0f}%'
                  f' | uncharted {nc.units.sum() / 1e6:.2f}M home {100 * nc[nc.home].units.sum() / max(1, nc.units.sum()):.0f}%,'
                  f' away in play {100 * a.recordsInPlay.sum() / max(1, a.recordsInPlay.sum() + a.awaiting.sum()):.0f}%')
