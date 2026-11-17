"""Opening-cohort closure by tier at several horizons (LpGenreAllocationFollowup.md §39). Usage from the repo root:
python SimTools/analyze_closure_horizon.py RUN [RUN ...] [--seeds 2002 1001] [--weeks 104 208 260]
Per run and seed: share of opening labels (procedural and template) defunct by each week, by initial tier, the exit
reason split for procedural Independents / Boutiques / Smalls, runtime births and their closures."""
import sys, pandas as pd
args = sys.argv[1:]; seeds = ['2002', '1001']; weeks = [104, 208, 260]
def opt(name, cast):
    global args
    if name not in args: return None
    i = args.index(name); j = i + 1
    while j < len(args) and not args[j].startswith('--'): j += 1
    vals = [cast(a) for a in args[i + 1:j]]; args = args[:i] + args[j:]; return vals
seeds = opt('--seeds', str) or seeds; weeks = opt('--weeks', int) or weeks
for run in args:
    for seed in seeds:
        p = f'SimLogs/{run}-{seed}-sc-'
        d = pd.read_csv(p + 'label-directory.csv')
        f = pd.read_csv(p + 'label-finance.csv', usecols=['week', 'labelId', 'status', 'exitReason'])
        last = f.week.max()
        dead = f[f.status == 'Defunct'].groupby('labelId').week.min()
        reason = f[f.exitReason.notna() & (f.exitReason != 'None')].groupby('labelId').exitReason.first()
        opening = set(d.labelId)
        print(f'== {run} {seed} (last week {last})')
        for tier in ['Major', 'MidTier', 'Independent', 'Boutique', 'Small']:
            for src, sel in (('proc', ~d.isHistorical), ('tmpl', d.isHistorical)):
                ids = d[(d.initialTier == tier) & sel].labelId
                if not len(ids): continue
                cells = [f'wk{w} {100 * sum(1 for l in ids if l in dead.index and dead[l] <= w) / len(ids):5.1f}%' for w in weeks if w <= last]
                r = reason.reindex([l for l in ids if l in dead.index]).value_counts().to_dict() if src == 'proc' else {}
                print(f'  {tier:12}{src} n={len(ids):3}  ' + '  '.join(cells) + (f'  reasons {r}' if r else ''))
        born = f[~f.labelId.isin(opening)].labelId.unique()
        bd = [l for l in born if l in dead.index]
        print(f'  births {len(born)}, defunct {len(bd)}; all defunct by last week {len(dead)}')
