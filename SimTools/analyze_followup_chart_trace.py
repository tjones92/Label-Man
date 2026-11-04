"""Verify the read-only replay and diagnose observed follow-up misses.

Usage: python SimTools/analyze_followup_chart_trace.py --trace fct-v1-default-1001-106
Generated evidence is written under SimLogs/followup-chart-investigation/trace.
"""
import argparse
import csv
import itertools
import json
from pathlib import Path

import pandas as pd

from analyze_followup_chart_funnel import funnel_rows
from analyze_offchart_screen import ROOT
from analyze_studio_parent import BIG5, studio_ids


def replay_identity(reference, trace, through):
    checked = []
    # These two normally-empty diagnostic streams are intentionally populated.
    changed = {'single-release-lanes', 'single-demand-stages'}
    for old in sorted(reference.parent.glob(reference.name + '-*.csv')):
        suffix = old.name[len(reference.name) + 1:-4]
        if suffix in changed:
            continue
        with old.open(encoding='utf-8-sig', newline='') as f:
            header = next(csv.reader(f))
        if 'week' not in header:
            continue
        new = Path(str(trace) + '-' + suffix + '.csv')
        if not new.exists():
            raise ValueError(f'Missing replay stream {suffix}')

        def rows(file):
            with file.open(encoding='utf-8-sig', newline='') as f:
                reader = csv.reader(f)
                columns = next(reader)
                if columns != header:
                    raise ValueError(f'Schema drift: {suffix}')
                wi = header.index('week')
                pi = header.index('currentPosition') if suffix == 'records' else None
                for row in reader:
                    if row[wi] and 1 <= int(row[wi]) <= through:
                        if pi is None or 1 <= int(row[pi]) <= 100:
                            yield row

        for n, (a, b) in enumerate(itertools.zip_longest(rows(old), rows(new)), 1):
            if a != b:
                raise ValueError(f'Replay changed {suffix}, row {n}')
        checked.append(suffix)
    if len(checked) < 50:
        raise ValueError(f'Incomplete identity evidence: {len(checked)} streams')
    return dict(throughWeek=through, identicalWeekKeyedStreams=checked,
                note='records compared after retaining published ranks 1-100; lane/stage diagnostics intentionally differ')


def diagnose(trace, through, output):
    p = lambda stream: str(trace) + '-' + stream + '.csv'
    records = pd.read_csv(p('records'))
    launches = pd.read_csv(p('single-launch-quality'))
    census = pd.read_csv(p('demand-release-census'))
    opening = set(census.loc[census.observationKind.eq('Opening'), 'recordId'])
    studios = studio_ids(p)
    records['group'] = records.labelId.map(lambda x: 'big5' if x in BIG5 else 'studio' if x in studios else 'field')
    cohorts, releases = funnel_rows(records, launches, opening, through)
    # All releases have equal 13-week follow-up within their act's 52-week window.
    eligible = releases[releases.entry13Eligible.eq(1)].copy()
    attributed = pd.read_csv(p('single-launch-attribution'))
    eligible = eligible.merge(attributed[['singleId', 'initialStock', 'initialAwareness', 'effectiveRecognition',
                                         'spreadCampaignImpact', 'launchCampaignImpact']], on='singleId', validate='one_to_one')
    stages = pd.read_csv(p('single-demand-stages'))
    stages = stages.merge(eligible[['singleId', 'week']].rename(columns={'week': 'launchWeek'}),
                          left_on='recordId', right_on='singleId', validate='many_to_one')
    stages = stages[stages.week.between(stages.launchWeek, stages.launchWeek + 13)]
    weekly = stages.groupby(['recordId', 'week']).agg(raw=('rawDemand', 'sum'), serviceable=('serviceableDemand', 'sum'),
        cleared=('clearedUnits', 'sum'), fit=('commercialFitAtCapture', 'median'),
        awareness=('baselineAwareness', 'mean'), discovery=('earnedDiscoveryExposure', 'mean'))
    record_lookup = records.set_index(['recordId', 'week'])
    weekly = weekly.join(record_lookup[['unitsThisWeek', 'chartPoints', 'chartCutoffPoints', 'currentPosition',
                                        'unsampledChartPoints', 'radioHeat', 'radioPanelShare', 'coveredRegionCount']])
    if weekly.chartPoints.isna().any():
        raise ValueError('Trace stage observation has no corresponding record/week')
    weekly['publishedRatio'] = weekly.chartPoints / weekly.chartCutoffPoints
    weekly['unsampledRatio'] = weekly.unsampledChartPoints / weekly.chartCutoffPoints
    weekly['entry'] = weekly.currentPosition.between(1, 100)
    # Pre-entry includes the entry week; a miss contributes its full observed window.
    entry_week = weekly[weekly.entry].reset_index().groupby('recordId').week.min()
    weekly = weekly.reset_index()
    weekly = weekly[weekly.week.le(weekly.recordId.map(entry_week).fillna(through + 1))]
    per = weekly.groupby('recordId').agg(observedWeeks=('week', 'nunique'), raw=('raw', 'sum'),
        serviceable=('serviceable', 'sum'), cleared=('cleared', 'sum'),
        peakPublishedRatio=('publishedRatio', 'max'), peakUnsampledRatio=('unsampledRatio', 'max'),
        fit=('fit', 'median'), baselineAwareness=('awareness', 'median'), discovery=('discovery', 'median'),
        maxCoverage=('coveredRegionCount', 'max'), peakRadio=('radioHeat', 'max'))
    per['inventoryFulfillment'] = per.serviceable / per.raw.clip(lower=1)
    per['marketFulfillment'] = per.cleared / per.serviceable.clip(lower=1)
    per['retailSales'] = per.cleared
    eligible = eligible.merge(per, left_on='singleId', right_index=True, validate='one_to_one')
    if eligible.observedWeeks.isna().any():
        raise ValueError('A mature follow-up has no off-chart observations')
    eligible.to_csv(output / 'release-diagnosis.csv', index=False)
    summaries = []
    for (group, entered), q in eligible.groupby(['group', 'entered13']):
        summaries.append(dict(group=group, entered=int(entered), launches=len(q),
            **{key: float(q[key].median()) for key in ['quality', 'initialAwareness', 'initialStock', 'fit',
               'peakPublishedRatio', 'peakUnsampledRatio', 'maxCoverage', 'peakRadio', 'retailSales']},
            inventoryFulfillment=float(q.serviceable.sum() / max(1, q.raw.sum())),
            marketFulfillment=float(q.cleared.sum() / max(1, q.serviceable.sum())),
            within20PctOfPublishedCutoff=float(q.peakPublishedRatio.ge(.8).mean()),
            everUnsampledAbovePublishedCutoff=float(q.peakUnsampledRatio.ge(1).mean())))
    result = pd.DataFrame(summaries)
    result.to_csv(output / 'trace-summary.csv', index=False)
    print(result.round(3).to_string(index=False))
    print('Conditional on entry, pre-entry windows end at entry; failures use the full 13-week window. These are descriptive comparisons, not causal effects.')
    return dict(fullyFollowedHitActs=len(cohorts), matureFollowupLaunches=len(eligible),
                offchartRecordRows=int(records.currentPosition.le(0).sum()))


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument('--reference', default='mbk-v1-breakout-default-1001-265')
    ap.add_argument('--trace', default='fct-v1-default-1001-106')
    ap.add_argument('--weeks', type=int, default=106)
    ap.add_argument('--output', default='SimLogs/followup-chart-investigation/trace')
    args = ap.parse_args()
    out = Path(args.output)
    out.mkdir(parents=True, exist_ok=True)
    reference, trace = Path(ROOT) / args.reference, Path(ROOT) / args.trace
    # The final capture writes terminal snapshots (for example live records).
    # Compare the completed weeks before that boundary, as the existing off replay does.
    identity = replay_identity(reference, trace, args.weeks - 1)
    print(f"Identical simulation evidence: {len(identity['identicalWeekKeyedStreams'])} streams through week {args.weeks - 1}")
    details = diagnose(trace, args.weeks, out)
    (out / 'validation.json').write_text(json.dumps(dict(reference=args.reference, trace=args.trace,
        identity=identity, **details), indent=2), encoding='utf-8')


if __name__ == '__main__':
    main()
