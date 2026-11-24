"""Who closes, and does it touch the economy? (LpGenreAllocationFollowup.md §47). Usage from the repo root:
python SimTools/analyze_closure_economy.py RUN [RUN ...] [--seeds 2002 1001]
Per run, mean of seeds. Closures: labels defunct by the last week, by tier at closure; share that ever charted a Hot 100
record (first-chart-events, release label); LPs and singles issued; realized net by format (release-outcomes); exit
reasons. Economy, per year: Hot 100 entries by tier family, firms charting, distinct acts releasing (release-strategy), singles market."""
import sys, pandas as pd
args = sys.argv[1:]; seeds = ['2002', '1001']
if '--seeds' in args: i = args.index('--seeds'); seeds = args[i + 1:]; args = args[:i]
SMALL = {'Independent', 'Boutique', 'Small'}
def one(run, seed):
    p = f'SimLogs/{run}-{seed}-sc-'; o = {}
    f = pd.read_csv(p + 'label-finance.csv', usecols=['week', 'labelId', 'labelTier', 'status', 'exitReason', 'cashReserves', 'isHistorical'])
    last = f.week.max()
    live = f[f.status != 'Defunct'].sort_values('week')
    tier = live.groupby('labelId').labelTier.last()
    dead = f[f.status == 'Defunct'].groupby('labelId').week.min()
    reason = f[f.exitReason.notna() & (f.exitReason != 'None')].groupby('labelId').exitReason.first()
    peak = live.groupby('labelId').cashReserves.max()
    ch = pd.read_csv(p + 'first-chart-events.csv', usecols=['week', 'releaseLabelId', 'leftCensoredAtRunStart'])
    charted = set(ch.releaseLabelId)
    ro = pd.read_csv(p + 'release-outcomes.csv', usecols=['labelId', 'recordId', 'format', 'realizedNet'])
    ro = ro.drop_duplicates('recordId', keep='last')
    lpn = ro[ro.format == 'Album'].groupby('labelId').realizedNet; sgn = ro[ro.format != 'Album'].groupby('labelId').realizedNet
    sa = pd.read_csv(p + 'release-strategy.csv', usecols=['year', 'recordId', 'labelId', 'tier', 'artistId', 'chosenFormat']).drop_duplicates('recordId')
    st = sa
    nlp = st[st.chosenFormat == 'Album'].groupby('labelId').size(); nsg = st[st.chosenFormat != 'Album'].groupby('labelId').size()
    lpsum, sgsum = lpn.sum(), sgn.sum()
    for grp, tiers in (('small', SMALL), ('MidTier', {'MidTier'}), ('Major', {'Major'})):
        ids = [l for l in dead.index if tier.get(l) in tiers]
        o[f'defunct {grp}'] = len(ids)
        if not ids: continue
        o[f'  {grp}: ever charted %'] = 100 * sum(l in charted for l in ids) / len(ids)
        o[f'  {grp}: LPs / singles issued per label'] = f'{nlp.reindex(ids).fillna(0).mean():.1f} / {nsg.reindex(ids).fillna(0).mean():.1f}'
        o[f'  {grp}: LP net / single net per label ($k)'] = f'{lpsum.reindex(ids).fillna(0).mean() / 1e3:.0f} / {sgsum.reindex(ids).fillna(0).mean() / 1e3:.0f}'
        o[f'  {grp}: LP net < 0 %'] = 100 * (lpsum.reindex(ids).fillna(0) < 0).mean()
        o[f'  {grp}: never above $50k cash %'] = 100 * (peak.reindex(ids).fillna(0) < 5e4).mean()
        o[f'  {grp}: reasons'] = reason.reindex(ids).value_counts().to_dict()
    cc = pd.read_csv(p + 'concentration.csv').set_index('year')
    mc = pd.read_csv(p + 'market-clearing-weekly.csv', usecols=['year', 'clearedSingleUnits'])
    for y in range(1960, 1965):
        if y not in cc.index: continue
        r = cc.loc[y]; n = r.chartEntries
        o[f'{y} Hot 100 entries Major+Mid / small %'] = f'{100 * (r.chartEntriesMajor + r.chartEntriesMidTier) / n:.1f} / {100 * (r.chartEntriesIndependent + r.chartEntriesBoutique + r.chartEntriesSmall) / n:.1f}'
        o[f'{y} firms charting'] = r.firmsCharting
        o[f'{y} C4 chart share'] = r.c4ChartShare
        z = sa[sa.year == y]
        o[f'{y} acts releasing / on small labels'] = f'{z.artistId.nunique()} / {z[z.tier.isin(SMALL)].artistId.nunique()}'
        o[f'{y} singles (M)'] = mc[mc.year == y].clearedSingleUnits.sum() / 1e6
    return o
for run in args:
    res = [one(run, s) for s in seeds]
    print(f'== {run} ({" / ".join(seeds)})')
    for k in res[0]:
        vals = [r.get(k) for r in res]
        if all(isinstance(v, (int, float)) for v in vals):
            print(f'{k:46}{sum(vals) / len(vals):9.2f}  ({" / ".join(f"{v:.2f}" for v in vals)})')
        else:
            print(f'{k:46}' + '  |  '.join(str(v) for v in vals))
