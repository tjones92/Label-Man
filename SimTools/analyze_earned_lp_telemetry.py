"""Earned LP forecast telemetry (LpGenreAllocationFollowup.md §45). Usage from the repo root:
python SimTools/analyze_earned_lp_telemetry.py RUN [RUN ...] [--seeds 2002 1001]
Reads RUN-SEED-sc-earned-lp-forecast.csv (one row per LP observed at 52 weeks or retirement). Per tier at observation:
the pooled ratio (sum sold / sum forecast over every observed LP: the unbiased reference each label should learn),
then each label's last row: labels, median observations, median learned ratio, median / mean multiplier, and the share
of labels whose multiplier is below 1."""
import sys, pandas as pd
args = sys.argv[1:]; seeds = ['2002', '1001']
if '--seeds' in args: i = args.index('--seeds'); seeds = args[i + 1:]; args = args[:i]
for run in args:
    t = pd.concat([pd.read_csv(f'SimLogs/{run}-{s}-sc-earned-lp-forecast.csv').assign(seed=s) for s in seeds])
    last = t.sort_values('week').groupby(['seed', 'labelId']).tail(1)
    print(f'== {run}: {len(t)} observations, weeks {t.week.min()}-{t.week.max()}')
    print(f'  {"tier":12}{"obs":>6}{"pooled":>8}{"labels":>7}{"med n":>6}{"med learned":>12}{"med mult":>9}{"mean mult":>10}{"mult<1":>7}')
    for tier in ('Major', 'MidTier', 'Independent', 'Boutique', 'Small'):
        a = t[t.tier == tier]; b = last[last.tier == tier]
        if a.empty: continue
        print(f'  {tier:12}{len(a):6}{a.soldUnits.sum() / a.forecastUnits.sum():8.2f}{len(b):7}{b.observations.median():6.0f}'
              f'{b.learnedRatio.median():12.2f}{b.multiplier.median():9.2f}{b.multiplier.mean():10.2f}{100 * (b.multiplier < 1).mean():6.0f}%')
