"""Three-seed summary of the Southern home-market question (LpGenreAllocationFollowup.md §30).

Run from SimLogs/: python ../SimTools/summarize_regional_home_sales.py CONTROL ARM [--seeds 2002 1001 3003] [--year 1961]
Needs --regional-home-sales runs. Per seed, 1961 singles: Southern small labels' units at home and away, their share of
Deep South sales, small-label vs major/mid share of sales in the five other regions, and per-record raw demand of
small-label and major/mid records away from home in those regions.
"""
import pandas as pd, sys
args = sys.argv[1:]; seeds = [2002, 1001, 3003]; year = 1961
if '--seeds' in args: i = args.index('--seeds'); seeds = [int(s) for s in args[i + 1:i + 4]]; del args[i:i + 4]
if '--year' in args: i = args.index('--year'); year = int(args[i + 1]); del args[i:i + 2]
ctl, arm = args
SMALL = {'Independent', 'Small', 'Boutique'}
OTHER = ['eastcoast', 'greatlakes', 'westcoast', 'greatplains', 'southwest']
def load(run):
    d = pd.read_csv(f'{run}-regional-home-sales.csv')
    d = d[(d.year == year) & (d.format == 'Single')].copy()
    d['small'] = d.labelTier.isin(SMALL); d['home'] = d.regionId == d.labelHomeRegion
    return d
rows = []
for seed in seeds:
    v = {'seed': seed}
    for tag, run in (('c', f'{ctl}-{seed}-sc'), ('a', f'{arm}-{seed}-sc')):
        d = load(run)
        s = d[(d.labelHomeRegion == 'deepsouth') & d.small]
        v[tag + 'SouthHome'] = s[s.regionId == 'deepsouth'].units.sum() / 1e6
        v[tag + 'SouthAway'] = s[s.regionId != 'deepsouth'].units.sum() / 1e6
        ds = d[d.regionId == 'deepsouth']
        v[tag + 'SouthShare'] = 100 * ds[ds.home & ds.small].units.sum() / ds.units.sum()
        o = d[d.regionId.isin(OTHER)]
        v[tag + 'SmallShareOther'] = 100 * o[o.small].units.sum() / o.units.sum()
        oa = o[~o.home]
        for g, m in (('Small', oa.small), ('Maj', ~oa.small)):
            x = oa[m]; v[tag + 'Raw' + g] = x.rawDemand.sum() / x.recordsInPlay.sum()
    rows.append(v)
t = pd.DataFrame(rows).set_index('seed')
pd.set_option('display.width', 200)
print(t.round(2).to_string())
m = t.mean()
print(f"\nmeans: Southern small at home {m.cSouthHome:.2f} -> {m.aSouthHome:.2f}M ({100 * (m.aSouthHome / m.cSouthHome - 1):+.0f}%), away {m.cSouthAway:.2f} -> {m.aSouthAway:.2f}M "
      f"({100 * (m.aSouthAway / m.cSouthAway - 1):+.0f}%), home fraction {100 * m.cSouthHome / (m.cSouthHome + m.cSouthAway):.0f}% -> {100 * m.aSouthHome / (m.aSouthHome + m.aSouthAway):.0f}%")
print(f"       their share of Deep South sales {m.cSouthShare:.1f} -> {m.aSouthShare:.1f}%; small-label share in other regions {m.cSmallShareOther:.1f} -> {m.aSmallShareOther:.1f}% "
      f"(paired {', '.join(f'{x:+.1f}' for x in t.aSmallShareOther - t.cSmallShareOther)})")
for g in ('Small', 'Maj'):
    d = 100 * (t['aRaw' + g] / t['cRaw' + g] - 1)
    print(f"       away raw demand per record-week, {g}: {', '.join(f'{x:+.0f}%' for x in d)} (mean {d.mean():+.0f}%)")
