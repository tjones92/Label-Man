"""The RIAA/Glover survey cohort in a model run (LpGenreAllocationFollowup.md §40). Usage from the repo root:
python SimTools/analyze_riaa_1963_cohort.py RUN [RUN ...] [--seeds 2002 1001] [--launch 157-208]
Default: a 260-week run's records released in 1963 (release week from release-campaign-costs, weeks 157-208) with sales
counted to the run's last week (260 = end of 1964); --launch 1-52 on a 104-week run gives the 1960 analogue, as the survey did (SimTools/RIAA-1965-LP-sales-research-notes.md: Mercury,
Capitol, Columbia, RCA Victor, Laurie, Kapp; 32% of 1963 releases). Retired records use release-outcomes; records still
selling at week 260 use live-records-snapshot (units and net to date; net counts no marketing).
"Fail" = realizedNet < 0. "Fail with overhead" also charges each release its label's 1963-64 overhead per release (the
survey allocated recording overhead and G&A per selection, p. 862). Tier = the label's tier at release."""
import sys, numpy as np, pandas as pd
args = sys.argv[1:]; seeds = ['2002', '1001']; launch = (157, 208)
if '--launch' in args:
    i = args.index('--launch'); a, b = args[i + 1].split('-'); launch = (int(a), int(b)); args = args[:i] + args[i + 2:]
if '--seeds' in args: i = args.index('--seeds'); seeds = args[i + 1:]; args = args[:i]
BINS = [0, 2000, 4000, 8000, 10000, 20000, 50000, 100000, 300000, 600000, 1e12]
LABELS = ['0-2k', '2-4k', '4-8k', '8-10k', '10-20k', '20-50k', '50-100k', '100-300k', '300-600k', '600k+']
RIAA = {'Single': ([53.3, 8.9, 8.3, 2.5, 7.9, 7.0, 4.1, 4.2, 2.8, 1.1], 74, 11200),
        'Album': ([35.8, 13.0, 12.9, 4.2, 10.7, 12.2, 6.1, 3.7, 0.8, 0.6], 61, 7800)}
GROUP = {'Major': 'Major', 'MidTier': 'MidTier', 'Independent': 'Indie', 'Boutique': 'Boutique', 'Small': 'Small'}
for run in args:
    frames = []
    for seed in seeds:
        p = f'SimLogs/{run}-{seed}-sc-'
        c = pd.read_csv(p + 'release-campaign-costs.csv')
        c = c[(c.week >= launch[0]) & (c.week <= launch[1])].rename(columns={'week': 'releaseWeek'})
        o = pd.read_csv(p + 'release-outcomes.csv', usecols=['recordId', 'format', 'pressingBookSold', 'realizedNet', 'sunkProductionCost', 'sunkMarketingCost'])
        live = pd.read_csv(p + 'live-records-snapshot.csv')
        live = live[live.week == live.week.max()]
        live = live.assign(pressingBookSold=live.totalUnitsSold, realizedNet=live.lifetimeLabelNet - live.sunkProductionCost, sunkMarketingCost=np.nan)
        out = pd.concat([o.assign(live=False), live[o.columns].assign(live=True)]).drop_duplicates('recordId')
        x = c.merge(out, on='recordId', how='left')
        f = pd.read_csv(p + 'label-finance.csv', usecols=['week', 'labelId', 'labelTier', 'monthlyOverhead'])
        f = f.drop_duplicates(['week', 'labelId'])
        tier = f.set_index(['week', 'labelId']).labelTier
        x['tier'] = [GROUP.get(tier.get((w, l)), 'other') for w, l in zip(x.releaseWeek, x.labelId)]
        # 1963-64 overhead per release for each label
        oh = f[f.week >= launch[0]].groupby('labelId').monthlyOverhead.sum() * 12 / 52
        rel = c.groupby('labelId').size() * 2  # 1963 releases x 2 years ~ releases over 1963-64
        x['overheadPerRelease'] = x.labelId.map(oh / rel).fillna(0)
        x['seed'] = seed; frames.append(x)
    x = pd.concat(frames)
    miss = x.format.isna().mean()
    x = x[x.format.notna()]
    print(f'== {run}: releases weeks {launch[0]}-{launch[1]}, sales to the last week, seeds {"/".join(seeds)} (unmatched {100 * miss:.1f}%)')
    for fmt in ('Single', 'Album'):
        share, fail, be = RIAA[fmt]
        for scope, sel in (('all tiers', x.format == fmt), ('Major+MidTier', (x.format == fmt) & x.tier.isin(['Major', 'MidTier']))):
            y = x[sel]; cut = pd.cut(y.pressingBookSold, BINS, right=False, labels=LABELS)
            dist = [100 * (cut == lab).mean() for lab in LABELS]
            print(f'  {fmt} {scope}: n {len(y)} (still selling {100 * y.live.mean():.0f}%)')
            print('    bins   ' + ' '.join(f'{l:>8}' for l in LABELS))
            print('    model  ' + ' '.join(f'{v:8.1f}' for v in dist))
            print('    RIAA   ' + ' '.join(f'{v:8.1f}' for v in share))
            print(f'    fail {100 * (y.realizedNet < 0).mean():.0f}%, with overhead {100 * ((y.realizedNet - y.overheadPerRelease) < 0).mean():.0f}% (RIAA {fail}%)')
        print(f'    by tier: ' + ' | '.join(
            f'{g} n {len(t)} med {t.pressingBookSold.median():.0f} <2k {100 * (t.pressingBookSold < 2000).mean():.0f}% <4k {100 * (t.pressingBookSold < 4000).mean():.0f}% '
            f'fail {100 * (t.realizedNet < 0).mean():.0f}%/{100 * ((t.realizedNet - t.overheadPerRelease) < 0).mean():.0f}% oh/rel ${t.overheadPerRelease.median():.0f}'
            for g in ('Major', 'MidTier', 'Indie', 'Boutique', 'Small') for t in [x[(x.format == fmt) & (x.tier == g)]] if len(t)))
