"""Read-only first-hit/follow-up attribution for completed full-profile worlds.

No counterfactual demand is inferred from chart-only observations. Launch books
are closed-book telemetry; absent books remain missing rather than becoming zero.
"""
import argparse
import json
from pathlib import Path

import pandas as pd

from analyze_follower_calibration import load, entries

LOGS = Path(__file__).resolve().parents[1] / 'SimLogs'


def build_pairs(chart, launches):
    hits, _ = entries(chart)
    first = hits.sort_values(['week', 'recordId']).drop_duplicates('artistId')
    first = first[first.week.between(5, chart.week.max()-65)
                  & first.launchCareerState.isin(['NewSigning', 'Rising'])]
    rows, actors = [], []
    for h in first.itertuples():
        q = launches[launches.artistId.eq(h.artistId)
                     & launches.week.between(h.week+1, h.week+52)].sort_values(['week', 'singleId'])
        actors.append(dict(artistId=h.artistId, hitId=h.recordId, hitWeek=h.week,
                           releases52=len(q), firstGap=None if q.empty else q.week.iloc[0]-h.week))
        for order, r in enumerate(q.itertuples(), 1):
            prior = chart[chart.artistId.eq(h.artistId) & chart.week.lt(r.week)]
            prior10 = prior[prior.currentPosition.le(10)]
            rows.append(dict(**r._asdict(), hitId=h.recordId, hitWeek=h.week,
                             order=order, hitGap=r.week-h.week,
                             recentTop10=not prior10.empty and r.week-prior10.week.max() <= 52,
                             predecessorOnChart=bool((prior.week == r.week-1).any())))
    return pd.DataFrame(rows).drop(columns='Index'), pd.DataFrame(actors)


def analyze(run):
    p = lambda stream: LOGS / f'{run}-{stream}.csv'
    chart = load(run)
    launches = pd.read_csv(p('single-launch-quality'))
    q, actors = build_pairs(chart, launches)
    first100 = chart.groupby('recordId').week.min()
    first40 = chart[chart.currentPosition.le(40)].groupby('recordId').week.min()
    q['top100_13'] = q.singleId.map(first100).between(q.week, q.week+13)
    q['top40_13'] = q.singleId.map(first40).between(q.week, q.week+13)
    q['outcome'] = q.top40_13.map({True:'Top40', False:'Below40'})
    q.loc[~q.top100_13, 'outcome'] = 'NoTop100'
    # All measured launch attributes below come from closed books / issue audits,
    # never from the subset that eventually charts.
    f = pd.read_csv(p('following'), low_memory=False)
    b = f[f.kind.eq('launch') & f.detail.str.startswith('Single|', na=False)].copy()
    b['singleId'] = b.detail.str.rsplit('|', n=1).str[-1]
    for field in ['book', 'stocked', 'granted', 'loss']:
        b[field] = pd.to_numeric(b.detail.str.extract(field+r'=([0-9.]+)')[0])
    b['asked'] = pd.to_numeric(b.value)
    q = q.merge(b[['singleId','book','stocked','granted','loss','asked']], how='left', on='singleId', validate='one_to_one')
    origin = launches.set_index('singleId')
    for field in ['runtimeQuality','tier','hook','production']:
        q['hit_'+field] = q.hitId.map(origin[field])
    q['hit_book'] = q.hitId.map(b.set_index('singleId').book)
    slate = pd.read_csv(p('release-slate'), low_memory=False)
    issued = slate[slate.state.eq('Issued') & slate.recordId.notna()].drop_duplicates('recordId')
    q['reservedMarketing'] = q.singleId.map(issued.set_index('recordId').reservedMarketing)
    q['hit_reservedMarketing'] = q.hitId.map(issued.set_index('recordId').reservedMarketing)
    decisions = pd.read_csv(p('programme-projects'))
    prepared = decisions[decisions.action.isin(['record-reviewed-material','record-own-session'])]
    prepared = prepared.sort_values('week').drop_duplicates('slotId',keep='last').set_index('slotId')
    issued['materialSource'] = issued.slotId.map(prepared.action)
    by_record = issued.set_index('recordId')
    q['materialSource'] = q.singleId.map(by_record.materialSource)
    q['hit_materialSource'] = q.hitId.map(by_record.materialSource)
    # Launch fields on records.csv are available only for eventual chart entrants.
    observed = chart.sort_values(['week','recordId']).drop_duplicates('recordId').set_index('recordId')
    for field in ['initialLaunchAwareness','initialLaunchStock','launchEffectiveRecognition']:
        q[field] = q.singleId.map(observed[field])
        q['hit_'+field] = q.hitId.map(observed[field])
    q['bookRatio'] = q.book / q.hit_book.where(q.hit_book.gt(0))
    q['qualityChange'] = q.runtimeQuality-q.hit_runtimeQuality
    q['hookChange'] = q.hook-q.hit_hook
    q['productionChange'] = q.production-q.hit_production
    q['qualityBand'] = pd.cut(q.runtimeQuality,[0,.55,.65,.75,1],include_lowest=True).astype(str)
    q['marketingRatio'] = q.reservedMarketing / q.hit_reservedMarketing.where(q.hit_reservedMarketing.gt(0))
    q['grantRatio'] = q.granted / q.asked.where(q.asked.gt(0))
    q['stockRatio'] = q.stocked / q.book.where(q.book.gt(0))
    q['run'] = run
    actors['run'] = run
    return q, actors


def summarize(q, **keys):
    return dict(**keys, releases=len(q), actors=q.artistId.nunique(),
                top100Pct=100*q.top100_13.mean(), top40Pct=100*q.top40_13.mean(),
                medianHitGap=q.hitGap.median(), majorPct=100*q.tier.eq('Major').mean(),
                recentTop10Pct=100*q.recentTop10.mean(), overlapPct=100*q.predecessorOnChart.mean(),
                quality=q.runtimeQuality.mean(), qualityChange=q.qualityChange.mean(),
                booksObserved=int(q.book.notna().sum()), medianBook=q.book.median(),
                medianBookRatio=q.bookRatio.median(), grantRatio=q.granted.sum()/q.asked.sum(),
                stockRatio=q.stocked.sum()/q.book.sum(), medianLoss=q.loss.median(),
                programmeIssued=int(q.reservedMarketing.notna().sum()),
                medianMarketing=q.reservedMarketing.median(),
                medianHitMarketing=q.hit_reservedMarketing.median(),
                medianMarketingRatio=q.marketingRatio.median())


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument('--tag', default='followup-v4-fresh')
    ap.add_argument('--seeds', nargs='+', type=int, default=[1001,2002,3003,4004])
    args = ap.parse_args()
    manifest = json.loads((LOGS/args.tag/'parallel-plan.json').read_text(encoding='utf-8-sig'))
    rows, actors, summaries = [], [], []
    for seed in args.seeds:
        for arm in ['control','treatment']:
            run = f'{args.tag}-{arm}-{seed}'
            if not any(r['name']==run and r['passed'] for r in manifest['runs']):
                raise ValueError(f'Incomplete run: {run}')
            q, a = analyze(run)
            rows.append(q); actors.append(a)
            for scope, data in [('all',q),('first',q[q.order.eq(1)]),('promptFirst',q[q.order.eq(1)&q.hitGap.le(13)])]:
                summaries.append(summarize(data,run=run,scope=scope,group='All'))
                for column in ['outcome','recentTop10','tier','qualityBand','materialSource']:
                    for group, subset in data.groupby(column):
                        summaries.append(summarize(subset,run=run,scope=scope,group=f'{column}={group}'))
            print(run, len(a), len(q), flush=True)
    out = LOGS/args.tag/'followup-conversion'
    out.mkdir(parents=True,exist_ok=True)
    pd.concat(rows).to_csv(out/'releases.csv',index=False)
    pd.concat(actors).to_csv(out/'actors.csv',index=False)
    pd.DataFrame(summaries).to_csv(out/'summary.csv',index=False)
    (out/'method.json').write_text(json.dumps(dict(seeds=args.seeds,
        cohort='Same first observed new Top40 cohort as concentration investigation: launch NewSigning/Rising, hit weeks 5..end-65; 52 weeks of subsequent releases, each with 13 outcome weeks.',
        prompt='First subsequent launch within 13 weeks after first observed hit; all launch routes included. Acts without further releases retained in actors.csv.',
        limitations='Descriptive changing cohorts, not paired causal effects. Top10 evidence is observed prior chart occupancy, not the saved hype clock or pre-1960 history. Absent closed books and absent programme issue rows are missing, not zero. Campaign reservation is a budget proxy, not realized exposure. records.csv launch fields only exist for charting titles and must not be used to explain offchart failures.'),indent=2),encoding='utf-8')


if __name__ == '__main__':
    main()
