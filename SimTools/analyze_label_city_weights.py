"""--label-city-weights screen (ProceduralLabelCityWeightingHandoff.md). Usage, from the repo root:
python SimTools/analyze_label_city_weights.py CONTROL ARM [--seeds 2002 1001 3003]
Per run: opening (procedural) and runtime-born labels by region, Deep South cities, Deep South labels' share of Hot 100
and Top 10 record-weeks (history: 3.1% / 7.1% of 1960 / 1961 Top 10 slots), their home fraction of singles units,
small-label share of Hot 100 record-weeks, defunct labels, and the genre unit-share error with Pop / R&S / Country."""
import sys, pandas as pd
sys.path.insert(0, 'SimTools')
from genre_share_breakdown import share_targets, norm
args = sys.argv[1:]; seeds = [2002, 1001, 3003]
if '--seeds' in args: i = args.index('--seeds'); seeds = [int(s) for s in args[i + 1:]]; args = args[:i]
T = share_targets(); SMALL = ['Independent', 'Small', 'Boutique']
FAM = {'Pop': ['traditionalpop', 'teenpop', 'easylistening'], 'R&S': ['rnb', 'soul', 'doowop', 'gospel', 'girlgroup', 'motown'], 'Country': ['country']}
for run in args:
    print(f'== {run}')
    for seed in seeds:
        p = f'SimLogs/{run}-{seed}-sc-'
        geo = pd.read_csv(p + 'label-geography.csv'); d = pd.read_csv(p + 'label-directory.csv', usecols=['labelId', 'isHistorical', 'initialTier'])
        g = geo.merge(d, on='labelId', how='left')
        op = g[g.isHistorical == False]
        reg = (100 * op.homeRegion.value_counts(normalize=True)).round(0).to_dict()
        rp = pd.read_csv(p + 'runtime-label-profiles.csv', usecols=['labelId', 'homeRegion'])
        bornreg = (100 * rp.homeRegion.value_counts(normalize=True)).round(0).to_dict()
        bornreg['n'] = len(rp)
        ds = g[g.homeRegion == 'deepsouth'].homeCityName.value_counts().head(5).to_dict()
        geo = pd.concat([geo[['labelId', 'homeRegion']], rp]).drop_duplicates('labelId')
        r = pd.read_csv(p + 'records.csv', usecols=['year', 'labelId', 'labelTier', 'currentPosition'])
        r = r[(r.currentPosition > 0) & (r.currentPosition <= 100)].merge(geo, on='labelId', how='left')
        ch = []
        for y in (1960, 1961):
            x = r[r.year == y]; s = x.homeRegion == 'deepsouth'
            ch.append(f'{y} H100 {100 * s.mean():.1f}% T10 {100 * s[x.currentPosition <= 10].mean():.1f}% small {100 * x.labelTier.isin(SMALL).mean():.0f}%')
        rh = pd.read_csv(p + 'regional-home-sales.csv', usecols=['year', 'regionId', 'labelHomeRegion', 'labelTier', 'format', 'units'])
        rh = rh[(rh.year == 1961) & (rh.format == 'Single') & (rh.labelHomeRegion == 'deepsouth') & rh.labelTier.isin(SMALL)]
        home = 100 * rh[rh.regionId == 'deepsouth'].units.sum() / max(1, rh.units.sum())
        fin = pd.read_csv(p + 'label-finance.csv', usecols=['labelId', 'status']).groupby('labelId').tail(1)
        defunct = int((fin.status == 'Defunct').sum())
        gs = pd.read_csv(p + 'genre-decade-shape.csv'); gs['g'] = gs.genre.map(norm)
        gs = gs.groupby(['year', 'g']).marketUnitsShare.sum() * 100
        err = []
        for y in (1960, 1961):
            x = gs.xs(y); keys = set(x.index) | {k for k, yy in T if yy == y and T[(k, yy)] > 0}
            e = sum(abs(x.get(k, 0) - T.get((k, y), 0)) for k in keys)
            fams = ' '.join(f'{f} {sum(x.get(k, 0) for k in ks):.1f}' for f, ks in FAM.items())
            err.append(f'{y} err {e:.1f} {fams}')
        print(f'  {seed}: opening {reg} | born {bornreg}\n     DS cities {ds}\n     {" | ".join(ch)} | S-small home {home:.0f}% | defunct {defunct}\n     {" | ".join(err)}')
