"""--historical-majors screen (LpGenreAllocationFollowup.md §49). Usage from the repo root:
python SimTools/analyze_historical_majors.py CONTROL ARM [--seeds 2002 1001] [--weeks 104]
Both runs are cut at --weeks (the control may be longer). Per year, mean of seeds: opening Majors; Top 10 slot share of
the five Big-5 templates and of all Majors; CR4 on Top 10 slots (operating label); labels with a Top 10 / Hot 100 record;
Hot 100 entries by tier family; Major Hot 100 entries by genre family (Country, Folk, EasyListening, Jazz, R&B/DooWop,
teen/rock); LP market and Major LPs per Major label; labels defunct (cumulative); the former procedural Majors' fate."""
import sys, pandas as pd
args = sys.argv[1:]; seeds = ['2002', '1001']; weeks = 104
if '--weeks' in args: i = args.index('--weeks'); weeks = int(args[i + 1]); args = args[:i] + args[i + 2:]
if '--seeds' in args: i = args.index('--seeds'); seeds = args[i + 1:]; args = args[:i]
control, arm = args
BIG5 = {'label_0001', 'label_0002', 'label_0003', 'label_0004', 'label_0005'}
FAM = {'Country': 'Country', 'Folk': 'Folk', 'EasyListening': 'EasyListening', 'Jazz': 'Jazz', 'TraditionalPop': 'TraditionalPop',
       'RnB': 'R&B / DooWop / Soul', 'DooWop': 'R&B / DooWop / Soul', 'Soul': 'R&B / DooWop / Soul',
       'TeenPop': 'Teen / Rock', 'RockAndRoll': 'Teen / Rock'}
def one(run, seed):
    p = f'SimLogs/{run}-{seed}-sc-'; o = {}
    d = pd.read_csv(p + 'label-directory.csv')
    o['opening Majors'] = int((d.initialTier == 'Major').sum())
    f = pd.read_csv(p + 'label-finance.csv', usecols=['week', 'labelId', 'labelTier', 'status', 'cashReserves'])
    f = f[f.week <= weeks]
    tier = f.drop_duplicates(['week', 'labelId']).set_index(['week', 'labelId']).labelTier
    dead = f[f.status == 'Defunct'].groupby('labelId').week.min()
    r = pd.read_csv(p + 'records.csv', usecols=['week', 'year', 'recordId', 'labelId', 'labelTier', 'genre', 'currentPosition'])
    r = r[(r.week <= weeks) & (r.currentPosition >= 1) & (r.currentPosition <= 100)]
    mc = pd.read_csv(p + 'market-clearing-weekly.csv', usecols=['week', 'year', 'clearedAlbumUnits'])
    rk = pd.read_csv(p + 'rack-region-weekly.csv', usecols=['year', 'units'])
    st = pd.read_csv(p + 'release-strategy.csv', usecols=['week', 'year', 'recordId', 'labelId', 'chosenFormat']).drop_duplicates('recordId')
    lp = st[(st.chosenFormat == 'Album') & (st.week <= weeks)].copy(); lp['tier'] = [tier.get((w, l)) for w, l in zip(lp.week, lp.labelId)]
    for y in sorted(r.year.unique()):
        z = r[r.year == y]; t10 = z[z.currentPosition <= 10]; n10 = len(t10)
        o[f'{y} Big-5 Top 10 slot share %'] = 100 * t10.labelId.isin(BIG5).sum() / n10
        o[f'{y} Major-tier Top 10 slot share %'] = 100 * (t10.labelTier == 'Major').sum() / n10
        o[f'{y} CR4 Top 10 slots %'] = 100 * t10.labelId.value_counts().head(4).sum() / n10
        o[f'{y} labels with a Top 10 record'] = t10.labelId.nunique()
        o[f'{y} labels with a Hot 100 record'] = z.labelId.nunique()
        e = z.drop_duplicates('recordId')
        o[f'{y} Hot 100 entries Major+Mid %'] = 100 * e.labelTier.isin(['Major', 'MidTier']).mean()
        maj = e[e.labelTier == 'Major']
        o[f'{y} Major Hot 100 entries'] = len(maj)
        fam = maj.genre.map(FAM).fillna('other').value_counts(normalize=True) * 100
        for k in ['TraditionalPop', 'Country', 'Folk', 'EasyListening', 'Jazz', 'R&B / DooWop / Soul', 'Teen / Rock', 'other']:
            o[f'{y}   Major entries {k} %'] = fam.get(k, 0.0)
        o[f'{y} LP market stores+racks (M)'] = (mc[(mc.year == y) & (mc.week <= weeks)].clearedAlbumUnits.sum() + rk[rk.year == y].units.sum()) / 1e6
        majors = f[((f.week - 1) // 52 + 1960 == y) & (f.labelTier == 'Major')].labelId.nunique()
        o[f'{y} Major LPs chosen per Major label'] = ((lp.year == y) & (lp.tier == 'Major')).sum() / max(majors, 1)
        o[f'{y} labels defunct (cumulative)'] = int((dead <= (y - 1959) * 52).sum())
    return o
res = {run: [one(run, s) for s in seeds] for run in (control, arm)}
keys = [k for k in res[arm][0] if all(k in x for x in res[control] + res[arm])]
print(f'{"":46}{control:>24}{arm:>24}   (per seed {" / ".join(seeds)})')
for k in keys:
    c = [x[k] for x in res[control]]; a = [x[k] for x in res[arm]]
    fmt = lambda v: ' / '.join(f'{x:.1f}' for x in v)
    print(f'{k:46}{sum(c) / len(c):10.1f} ({fmt(c):>11}){sum(a) / len(a):10.1f} ({fmt(a):>11})')
