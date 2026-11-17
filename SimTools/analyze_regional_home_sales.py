"""Regional home-sales decomposition (LpGenreAllocationFollowup.md §30). Needs runs made with --regional-home-sales.

Run from SimLogs/: python ../SimTools/analyze_regional_home_sales.py <run> [<run> ...] [--year 1961] [--format Single]
Per run: for each sale region, the share of its units sold by labels homed there (all tiers and small tiers), and for
the Deep South the funnel of Southern small labels' records in their home region against national product:
records in play, raw demand, aware buyers, serviceable intent, displaced demand, units, backorders, stock.
"""
import pandas as pd, sys
args = sys.argv[1:]; year = 1961; fmt = 'Single'
if '--year' in args: i = args.index('--year'); year = int(args[i + 1]); del args[i:i + 2]
if '--format' in args: i = args.index('--format'); fmt = args[i + 1]; del args[i:i + 2]
SMALL = {'Independent', 'Small', 'Boutique'}
REG = ['eastcoast', 'greatlakes', 'westcoast', 'greatplains', 'deepsouth', 'southwest', 'rockies']
COLS = ['recordsInPlay', 'rawDemand', 'awareBuyers', 'serviceableIntent', 'displacedDemand', 'units', 'backorders', 'unitsInStores', 'awarenessSum', 'radioSum', 'breakouts']
for run in args:
    d = pd.read_csv(f'{run}-regional-home-sales.csv')
    d = d[(d.year == year) & (d.format == fmt)]
    d['home'] = d.regionId == d.labelHomeRegion
    d['small'] = d.labelTier.isin(SMALL)
    tot = d.groupby('regionId').units.sum()
    print(f'== {run} {year} {fmt}: region units (M), home-label share all / small-home share / small-elsewhere share')
    for r in REG:
        x = d[d.regionId == r]
        print(f'  {r:12s} {tot[r] / 1e6:5.1f}M  home {100 * x[x.home].units.sum() / tot[r]:4.1f}%  smallHome {100 * x[x.home & x.small].units.sum() / tot[r]:4.1f}%  '
              f'smallAway {100 * x[~x.home & x.small].units.sum() / tot[r]:4.1f}%  majors/mid {100 * x[~x.small].units.sum() / tot[r]:4.1f}%')
    # Where Southern small labels sell
    s = d[(d.labelHomeRegion == 'deepsouth') & d.small].groupby('regionId').units.sum()
    print('  Southern small labels sell (M): ' + ' '.join(f'{r[:5]} {s.get(r, 0) / 1e6:.2f}' for r in REG) + f' | total {s.sum() / 1e6:.2f}')
    for r in ('deepsouth', 'eastcoast'):
        x = d[d.regionId == r]
        groups = {'home small': x[x.home & x.small], 'away small': x[~x.home & x.small], 'home mid/maj': x[x.home & ~x.small], 'away mid/maj': x[~x.home & ~x.small]}
        print(f'  -- in {r}: per record-week in play')
        for k, g in groups.items():
            n = g.recordsInPlay.sum()
            if n == 0: continue
            print(f'    {k:13s} rw {n:7d} raw {g.rawDemand.sum() / n:7.1f} aware {g.awareBuyers.sum() / n:8.0f} serv {g.serviceableIntent.sum() / n:6.1f} '
                  f'displ {g.displacedDemand.sum() / n:6.1f} units {g.units.sum() / n:6.1f} bo {g.backorders.sum() / n:6.1f} stock {g.unitsInStores.sum() / n:6.0f} '
                  f'awr {g.awarenessSum.sum() / n:.3f} radio {g.radioSum.sum() / n:.3f} brk {100 * g.breakouts.sum() / n:.1f}% | units {g.units.sum() / 1e6:.2f}M')
    # R&S by region: units, share of the region's units, and who sells it
    print(f'  -- RhythmAndSoul by sale region: units M, % of region, % of national R&S, home-label share of it')
    rs = d[d.family == 'RhythmAndSoul']; nat = rs.units.sum()
    for r in REG:
        x = rs[rs.regionId == r]
        print(f'    {r:12s} {x.units.sum() / 1e6:5.2f}M  {100 * x.units.sum() / tot[r]:4.1f}% of region  {100 * x.units.sum() / nat:4.1f}% of R&S  '
              f'home {100 * x[x.home].units.sum() / max(1, x.units.sum()):4.1f}%')
    rh = rs.groupby('labelHomeRegion').units.sum()
    print('    R&S sold by label home: ' + ' '.join(f'{k[:5]} {100 * v / nat:.0f}%' for k, v in rh.sort_values(ascending=False).items()))
    print(f'    R&S share of all units {100 * nat / d.units.sum():.1f}%')
