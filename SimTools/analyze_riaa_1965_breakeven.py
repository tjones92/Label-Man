"""Model releases against the RIAA's 1964 release survey (LpGenreAllocationFollowup.md §40).
Usage from the repo root: python SimTools/analyze_riaa_1965_breakeven.py RUN [--seeds 2002 1001] [--years 1962 1964]

Source: Copyright Law Revision hearings, House Judiciary Subcommittee 3, 10 June 1965, RIAA Exhibit 10 and pp. 863-865
(survey companies' releases; break-even = weighted average of the companies' break-even points at the 2-cent rate).
45 RPM singles: 1,189 releases, break-even 11,200 units, 74% fail to earn a profit; revenue $.440 a copy, less
copyright .040, artist .025, MPTF .016, manufacturing .100, selling/G&A .108 = profit .151 before taxes.
Popular LPs: 1,603 releases, break-even 7,800, 61% fail; revenue $1.818, less .240 / .150 / .076 / .450 / .450 = .452.

release-outcomes rows are written when a record retires (`year` = retirement year), so a window well inside the run
avoids censoring. Model "fail" = realizedNet < 0: lifetime label net (revenue after skim, royalties, publishing, re-press
and sale costs) less sunk production (recording + first run) and marketing. It carries no overhead allocation, which the
RIAA's selling/G&A line does."""
import sys, numpy as np, pandas as pd
args = sys.argv[1:]
def opt(name, default):
    global args
    if name not in args: return default
    i = args.index(name); j = i + 1
    while j < len(args) and not args[j].startswith('--'): j += 1
    vals = args[i + 1:j]; args = args[:i] + args[j:]; return vals
seeds = opt('--seeds', ['2002', '1001']); years = [int(y) for y in opt('--years', ['1962', '1964'])]
BINS = [0, 2000, 4000, 8000, 10000, 20000, 50000, 100000, 300000, 600000, 1e12]
LABELS = ['0-2k', '2-4k', '4-8k', '8-10k', '10-20k', '20-50k', '50-100k', '100-300k', '300-600k', '600k+']
RIAA = {'Single': ([53.3, 8.9, 8.3, 2.5, 7.9, 7.0, 4.1, 4.2, 2.8, 1.1], 74, 11200),
        'Album': ([35.8, 13.0, 12.9, 4.2, 10.7, 12.2, 6.1, 3.7, 0.8, 0.6], 61, 7800)}
GROUP = {'Major': 'Major', 'MidTier': 'MidTier', 'Independent': 'Indie', 'Boutique': 'Boutique', 'Small': 'Small'}
for run in args:
    frames = []
    for seed in seeds:
        p = f'SimLogs/{run}-{seed}-sc-'
        ro = pd.read_csv(p + 'release-outcomes.csv')
        ro = ro[(ro.year >= years[0]) & (ro.year <= years[1])].copy()
        # tier at retirement from label-finance (weekly), falling back to the opening tier
        f = pd.read_csv(p + 'label-finance.csv', usecols=['week', 'labelId', 'labelTier'])
        tier = f.drop_duplicates(['week', 'labelId']).set_index(['week', 'labelId']).labelTier
        ro['tier'] = [tier.get((w, l), np.nan) for w, l in zip(ro.week, ro.labelId)]
        d = pd.read_csv(p + 'label-directory.csv').set_index('labelId').initialTier
        ro['tier'] = ro.tier.fillna(ro.labelId.map(d)).map(GROUP).fillna('other')
        ro['seed'] = seed; frames.append(ro)
    ro = pd.concat(frames)
    ro['sunk'] = ro.sunkProductionCost.fillna(0) + ro.sunkMarketingCost.fillna(0)
    ro['units'] = ro.pressingBookSold.fillna(0)
    for fmt in ('Single', 'Album'):
        x = ro[ro.format == fmt]
        if x.empty: continue
        share, fail, be = RIAA[fmt]
        print(f'== {run} {fmt}s retired {years[0]}-{years[1]}, seeds {"/".join(seeds)}: n={len(x)}')
        print(f'   {"bin":10}{"model %":>9}{"RIAA %":>8}{"model fail %":>14}')
        cut = pd.cut(x.units, BINS, right=False, labels=LABELS)
        for i, lab in enumerate(LABELS):
            xb = x[cut == lab]
            print(f'   {lab:10}{100 * len(xb) / len(x):9.1f}{share[i]:8.1f}{100 * (xb.realizedNet < 0).mean() if len(xb) else float("nan"):14.0f}')
        pos = x[x.units > 0]
        npu = (pos.lifetimeLabelNet / pos.units)
        print(f'   fail {100 * (x.realizedNet < 0).mean():.0f}% (RIAA {fail}%); median units {x.units.median():.0f}; '
              f'median sunk cost ${x.sunk.median():.0f} (production ${x.sunkProductionCost.median():.0f}, marketing ${x.sunkMarketingCost.fillna(0).median():.0f}); '
              f'median label net per copy ${npu.median():.3f}; implied break-even (median sunk / median net per copy) '
              f'{x.sunk.median() / max(npu.median(), 1e-6):.0f} units (RIAA {be})')
        print(f'   {"tier":9}{"n":>6}{"med units":>10}{"<2k %":>7}{"fail %":>7}{"sunk $":>8}{"net/copy":>9}{"break-even":>11}{"sum realizedNet $k":>19}')
        for g in ('Major', 'MidTier', 'Indie', 'Boutique', 'Small'):
            xg = x[x.tier == g]
            if xg.empty: continue
            pg = xg[xg.units > 0]; n = (pg.lifetimeLabelNet / pg.units).median()
            print(f'   {g:9}{len(xg):6}{xg.units.median():10.0f}{100 * (xg.units < 2000).mean():7.0f}{100 * (xg.realizedNet < 0).mean():7.0f}'
                  f'{xg.sunk.median():8.0f}{n:9.3f}{xg.sunk.median() / max(n, 1e-6):11.0f}{xg.realizedNet.sum() / 1e3:19.0f}')
