"""Comparable chart, pipeline and LP outcomes for the four-seed follow-up scheduling screen.

Writes its own report directory; never overwrites the accepted follower-calibration artifacts.
"""
import argparse
import csv
import hashlib
import json
from collections import Counter
from pathlib import Path

import pandas as pd
from analyze_follower_calibration import load, summarize, history
from analyze_programme_pipeline import opening_cohort, trace

ROOT = Path(__file__).resolve().parents[1]
LOGS = ROOT / 'SimLogs'


def slate_counts(run):
    latest = {}
    issued = {}
    opening = None
    opening_lp = {}
    with open(LOGS / f'{run}-release-slate.csv', newline='', encoding='utf-8-sig') as f:
        for r in csv.DictReader(f):
            if opening is None:
                opening = r['week']
            if r['week'] == opening and r['format'] == 'Album' and r['state'] == 'Proposed':
                opening_lp[r['slotId']] = r['artistId']
            latest[r['slotId']] = r
            if r['state'] == 'Issued':
                issued[r['slotId']] = r
    return dict(issued=dict(Counter(r['format'] for r in issued.values())),
                openingLpPlans=opening_lp,
                terminal=dict(Counter(r['format'] + ':' + r['state'] for r in latest.values())))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--tag', default='followup-v4-fresh')
    parser.add_argument('--seeds', type=int, nargs='+', default=[1001,2002,3003,4004])
    parser.add_argument('--comparison-year', type=int, default=1963)
    args = parser.parse_args()
    out = LOGS / args.tag / 'analysis'
    out.mkdir(parents=True, exist_ok=True)
    summaries, annual, pairs, inventory, pipeline, hashes, finance = [], [], [], {}, {}, {}, []
    historical = history()
    _, ha, hs = summarize(historical[historical.year.le(args.comparison_year)], 'history')
    annual.extend(ha)
    summaries.append(dict(hs, arm='history', seed=0))
    for seed in args.seeds:
        names = [f'{args.tag}-{arm}-{seed}' for arm in ('control','treatment')]
        pairs.append(names)
        for arm, run in zip(('control','treatment'), names):
            _, years, summary = summarize(load(run), run)
            annual.extend(dict(y, arm=arm, seed=seed) for y in years)
            summaries.append(dict(summary, arm=arm, seed=seed))
            inventory[run] = slate_counts(run)
            accounts = pd.read_csv(LOGS / f'{run}-company-monthly.csv')
            for year, group in accounts[accounts.year.between(args.comparison_year-3,args.comparison_year)].groupby('year'):
                finance.append(dict(run=run, arm=arm, seed=seed, year=int(year),
                    pretaxEarnings=group.pretaxEarnings.sum(), afterTaxEarnings=group.afterTaxEarnings.sum(),
                    recording=group.recording.sum(), campaigns=group.campaigns.sum(),
                    maxCashReconciliationError=group.cashReconciliationError.abs().max()))
            hashes[run] = {suffix: hashlib.sha256((LOGS / f'{run}-{suffix}').read_bytes()).hexdigest()
                           for suffix in ('records.csv','release-slate.csv','console.log')}
        prefixes = [LOGS / n for n in names]
        a, ao = opening_cohort(prefixes[0]); b, bo = opening_cohort(prefixes[1]); cohort = a & b
        opening_a = inventory[names[0]].pop('openingLpPlans')
        opening_b = inventory[names[1]].pop('openingLpPlans')
        pipeline[str(seed)] = dict(matchedActs=len(cohort), arms={}, openingLpPlans=dict(
            control=len(opening_a), treatment=len(opening_b), identicalSlotAndAct=sum(opening_b.get(k)==v for k,v in opening_a.items())))
        for arm, prefix, initial in zip(('control','treatment'), prefixes, (ao,bo)):
            summary, acts = trace(prefix, cohort, initial)
            pipeline[str(seed)]['arms'][arm] = summary
            (out / f'{seed}-{arm}-act-traces.json').write_text(json.dumps(acts, indent=2), encoding='utf-8')
    ann = pd.DataFrame(annual)
    summ = pd.DataFrame(summaries)
    ann.to_csv(out / 'annual.csv', index=False)
    summ.to_csv(out / 'chart-summary.csv', index=False)
    economics = []
    for base, treatment in pairs:
        a = pd.read_csv(LOGS / f'{base}-decade-annual-rollup.csv').set_index('year')
        b = pd.read_csv(LOGS / f'{treatment}-decade-annual-rollup.csv').set_index('year')
        for year in range(args.comparison_year-3,args.comparison_year+1):
            economics.append(dict(base=base,treatment=treatment,year=year,
                singlesPct=100*(b.loc[year,'singleUnits']/a.loc[year,'singleUnits']-1),
                albumsPct=100*(b.loc[year,'albumUnits']/a.loc[year,'albumUnits']-1),
                controlSingleUnits=a.loc[year,'singleUnits'],treatmentSingleUnits=b.loc[year,'singleUnits'],
                controlAlbumUnits=a.loc[year,'albumUnits'],treatmentAlbumUnits=b.loc[year,'albumUnits']))
    pd.DataFrame(economics).to_csv(out / 'economic-pairs.csv', index=False)
    pd.DataFrame(finance).to_csv(out / 'finance.csv', index=False)
    results = []
    for arm in ('control','treatment'):
        q = summ[summ.arm.eq(arm)]
        r = ann[ann.arm.eq(arm) & ann.year.eq(args.comparison_year)]
        results.append(dict(arm=arm, seeds=len(q), repeatYear=args.comparison_year, repeatPct=100*r['repeat'].mean(),
            top5EntriesPct=100*q.top5Entries.mean(), top10SlotWeeksPct=100*q.top10SlotWeeks.mean()))
    paired_charts = []
    for seed in args.seeds:
        row = dict(seed=seed)
        for arm in ('control','treatment'):
            q = summ[summ.arm.eq(arm) & summ.seed.eq(seed)].iloc[0]
            r = ann[ann.arm.eq(arm) & ann.seed.eq(seed) & ann.year.eq(args.comparison_year)].iloc[0]
            row[arm+'RepeatPct'] = 100*r['repeat']
            row[arm+'Top5EntriesPct'] = 100*q.top5Entries
            row[arm+'Top10SlotWeeksPct'] = 100*q.top10SlotWeeks
        for metric in ('RepeatPct','Top5EntriesPct','Top10SlotWeeksPct'):
            row['delta'+metric] = row['treatment'+metric]-row['control'+metric]
        paired_charts.append(row)
    pd.DataFrame(paired_charts).to_csv(out / 'chart-pairs.csv', index=False)
    historical_repeat = next(r['repeat'] for r in ha if r['year']==args.comparison_year)
    report = dict(chartMeans=results, history=dict(repeatYear=args.comparison_year, repeatPct=100*historical_repeat,
        top5EntriesPct=100*hs['top5Entries'],top10SlotWeeksPct=100*hs['top10SlotWeeks'],startYear=hs['startYear'],endYear=hs['endYear']),
        slate=inventory, pipeline=pipeline,
        limits=f'{len(args.seeds)} matched seed pairs; complete chart occupancy; latest three complete years per source. Historical comparison ends in {args.comparison_year}. Fixed opening established-act pipeline cohort per seed. Descriptive changing title cohorts and censored work retained. Original 1966 reference is not a matched comparison for fresh 1960-63 worlds.',
        sha256=hashes)
    (out / 'report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(pd.DataFrame(results).to_string(index=False))
    print(pd.DataFrame(economics).to_string(index=False))
    print('Report:', out / 'report.json')


if __name__ == '__main__':
    main()
