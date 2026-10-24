"""Completed-run screen for --hit-act-fit-floor, with fixed observation windows.

Usage: python SimTools/analyze_hit_act_fit_support.py --runs off=hfs-v2-off f06=hfs-v2-f0p6 f1=hfs-v2-f1
Comma-separated tags merge seed sources; the first completed source for a seed wins.
Only completed seeds common to all arms are compared. A longer archived control
is supported: every weekly input is truncated before analysis. No
full-run aggregate from that longer control is mixed with a shorter treatment.
"""
import argparse
import json
import math
import statistics
from pathlib import Path

import pandas as pd

from analyze_followup_chart_funnel import funnel_rows, summary
from analyze_head_hit_conversion import require_complete_chart
from analyze_hit_act_cadence import annual_metrics
from analyze_offchart_screen import ROOT, path
from analyze_studio_parent import BIG5, studio_ids


def mechanism_flag(flag):
    return flag.startswith(('--hit-act-fit-floor=', '--artist-fanbase=', '--artist-fanbase-recruit=', '--artist-fanbase-half-life=')) or flag == '--hit-act-going-concern-renewal'


def career_progress(cohorts, careers):
    """State/recognition change from the hit milestone through its complete next 52 weeks."""
    rows = []
    ranked = {'NewSigning': 0, 'Rising': 1, 'Established': 2, 'Star': 3, 'Superstar': 4}
    for hit in cohorts.itertuples():
        q = careers[(careers.artistId == hit.artistId) & careers.week.between(hit.hitWeek, hit.hitWeek + 52)].sort_values('week')
        if q.empty or q.week.duplicated().any() or len(q) != 53 or q.week.iloc[0] != hit.hitWeek or q.week.iloc[-1] != hit.hitWeek + 52:
            raise ValueError(f'Incomplete career trajectory: {hit.artistId}')
        start, end = q.iloc[0], q.iloc[-1]
        low = start.careerState in ['NewSigning', 'Rising']
        established = end.careerState in ['Established', 'Star', 'Superstar']
        established_weeks = q.careerState.isin(['Established', 'Star', 'Superstar'])
        ever_established = bool(established_weeks.any())
        owned_at_end = pd.notna(end.get('labelId')) and bool(str(end.get('labelId')).strip())
        unowned_weeks = int(q.labelId.fillna('').astype(str).str.strip().eq('').sum())
        alive_at_end = end.get('lifecycleStatus', 'Active') == 'Active' and end.careerState not in ['Disbanded', 'Retired']
        alive = q.lifecycleStatus.eq('Active') if 'lifecycleStatus' in q else ~q.careerState.isin(['Disbanded', 'Retired'])
        active_unowned_weeks = int((alive & q.labelId.fillna('').astype(str).str.strip().eq('')).sum())
        rows.append(dict(artistId=hit.artistId, group=hit.group, cohort=start.cohort,
            firstCareerTop10=int(start.top10Hits <= 1), startState=start.careerState, endState=end.careerState,
            startRecognition=float(start.publicRecognition), endRecognition=float(end.publicRecognition),
            recognitionChange=float(end.publicRecognition - start.publicRecognition),
            newEstablished=int(low and established), startedBelowEstablished=int(low),
            everNewEstablished=int(low and ever_established), weeksEstablished=int(established_weeks.sum()),
            promotedThenUnowned=int(low and ever_established and not owned_at_end and alive_at_end),
            promotedThenTerminal=int(low and ever_established and not alive_at_end),
            unownedAtEnd=int(not owned_at_end), activeUnownedAtEnd=int(not owned_at_end and alive_at_end),
            unownedWeeks=unowned_weeks, activeUnownedWeeks=active_unowned_weeks,
            establishedAtEnd=int(established), starAtEnd=int(end.careerState in ['Star', 'Superstar']),
            declinedAtEnd=int(end.careerState in ['Declining', 'Dropped', 'Unsigned']),
            rankChange=ranked.get(end.careerState, -1) - ranked.get(start.careerState, -1),
            creditedTop40s=int(end.top40Hits - start.top40Hits), creditedTop10s=int(end.top10Hits - start.top10Hits)))
    return pd.DataFrame(rows)


def measure(tag, seed, source_weeks, through):
    p = lambda stream: path(tag, seed, stream, source_weeks)
    records = pd.read_csv(p('records'))
    records = records[records.week.le(through)]
    chart = records[records.currentPosition.between(1, 100)]
    require_complete_chart(chart, through)
    launches = pd.read_csv(p('single-launch-quality'))
    launches = launches[launches.week.le(through)]
    census = pd.read_csv(p('demand-release-census'))
    opening = set(census.loc[census.observationKind.eq('Opening'), 'recordId'])
    studios = studio_ids(p)
    records['group'] = records.labelId.map(lambda x: 'big5' if x in BIG5 else 'studio' if x in studios else 'field')
    cohorts, releases = funnel_rows(records, launches, opening, through)
    first40 = chart[chart.currentPosition.le(40)].groupby('recordId').week.min()
    releases['top40In52'] = releases.singleId.map(first40).le(releases.hitWeek + 52).fillna(False).astype(int)
    observed = chart[['recordId', 'week', 'currentPosition']].merge(releases[['singleId', 'hitWeek']], left_on='recordId', right_on='singleId', validate='many_to_one')
    peaks = observed[observed.week.le(observed.hitWeek + 52)].groupby('recordId').currentPosition.min()
    releases['peakIn52'] = releases.singleId.map(peaks).fillna(0).astype(int)
    career_path = Path(p('followup-careers'))
    careers = career_progress(cohorts, pd.read_csv(career_path)) if career_path.exists() else pd.DataFrame()
    annual = annual_metrics(records, launches, opening, years=range(1960, 1960 + through // 52))
    clearing = pd.read_csv(p('market-clearing-weekly'))
    clearing = clearing[clearing.week.le(through)]
    sales = pd.read_csv(p('label-sales-weekly'))
    sales = sales[sales.week.le(through) & sales['format'].eq('Single')]
    fin = pd.read_csv(p('label-finance'))
    fin = fin[fin.week.le(through)]
    start_mid = set(fin.loc[fin.week.eq(1) & fin.labelTier.eq('MidTier'), 'labelId']) - BIG5 - studios
    last_fin = fin.sort_values('week').drop_duplicates('labelId', keep='last')
    closed = set(last_fin.loc[last_fin.status.isin(['Bankrupt', 'Defunct']), 'labelId'])
    outcomes = pd.read_csv(p('release-outcomes'))
    mature_ids = set(launches.loc[launches.week.le(through - 26), 'singleId'])
    retired = outcomes[(outcomes.week <= through) & outcomes.recordId.isin(mature_ids) & outcomes['format'].eq('Single')].drop_duplicates('recordId')
    guards = dict(singleLaunches=len(launches), purchaseCapacity=int(clearing.purchaseCapacity.sum()),
        basePurchaseCapacity=int(clearing.baseCapacity.sum()), albumChannelCapacity=int(clearing.albumChannelCapacity.sum()),
        r=float(clearing.clearedSingleUnits.sum() / clearing.serviceableSingleIntent.sum()),
        singleUnitsM=float(sales.units.sum() / 1e6), top10UnitsM=float(sales.top10Units.sum() / 1e6),
        actualBig5UnitShare=float(sales.loc[sales.labelId.isin(BIG5), 'units'].sum() / sales.units.sum()),
        openingFieldMidClosures=len(start_mid & closed), openingFieldMidCount=len(start_mid),
        matureRetiredSingles=len(retired), matureStillActive=len(mature_ids - set(retired.recordId)),
        matureRetiredLossShare=float(retired.realizedNet.lt(0).mean()))
    stage_path = Path(p('single-demand-stages'))
    stage_columns = list(pd.read_csv(stage_path, nrows=0).columns) if stage_path.exists() else []
    if 'commercialFitWithoutSupportAtCapture' in stage_columns:
        stages = pd.read_csv(stage_path, usecols=['week', 'recordId', 'commercialFitAtCapture',
            'commercialFitWithoutSupportAtCapture', 'hitActFitFloorAtLaunch'])
        stages = stages[stages.week.le(through) & stages.recordId.isin(releases.loc[releases.entry13Eligible.eq(1), 'singleId'])]
        if stages.groupby('recordId').hitActFitFloorAtLaunch.nunique().gt(1).any():
            raise ValueError('A launched record changed its fit-support snapshot')
        stages['binding'] = stages.commercialFitAtCapture.gt(stages.commercialFitWithoutSupportAtCapture + 1e-6)
        binding = stages.groupby('recordId').agg(binding=('binding', 'mean'),
            protectedFit=('commercialFitAtCapture', 'median'), unprotectedFit=('commercialFitWithoutSupportAtCapture', 'median'),
            nationalFloor=('hitActFitFloorAtLaunch', 'first'))
        releases = releases.merge(binding, left_on='singleId', right_index=True, how='left', validate='one_to_one')
        guards['supportBindingReleaseShare'] = float(binding.binding.gt(0).mean())
        guards['snapshotInvariantCheckedRecords'] = len(binding)
    else:
        guards['supportBindingReleaseShare'] = 0.0
        guards['snapshotInvariantCheckedRecords'] = 0
    # Exact fixed-purse accounting is already exported by the settlement.
    if clearing.reconciliationDelta.abs().max() > 1 or clearing.settlementDelta.abs().max() > 1:
        raise ValueError('Market settlement fails reconciliation')
    return cohorts, releases, careers, annual, guards


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument('--runs', nargs='+', required=True)
    ap.add_argument('--weeks', type=int, default=106)
    ap.add_argument('--output', default='SimLogs/hfs-v2/review')
    args = ap.parse_args()
    runs = dict(x.split('=', 1) for x in args.runs)
    available = {}
    for arm, tags in runs.items():
        available[arm] = {}
        for tag in tags.split(','):
            m = json.loads((Path(ROOT) / tag / 'runs.json').read_text(encoding='utf-8-sig'))
            for run in m['results']:
                if run['passed']: available[arm].setdefault(run['seed'], (tag, m))
    seeds = sorted(set.intersection(*[set(sources) for sources in available.values()]))
    if not seeds:
        raise ValueError('No common completed seeds')
    control = next(iter(runs))
    base_flags = available[control][seeds[0]][1]['extraFlags']
    diagnostics = ['--followup-chart-trace', '--head-hit-attribution', '--audit-hit-act-renewals']
    base_flags = sorted(x for x in base_flags if x not in diagnostics and not mechanism_flag(x))
    for arm, sources in available.items():
        dose = sorted(x for x in sources[seeds[0]][1]['extraFlags'] if mechanism_flag(x))
        if arm == control and any(x.startswith('--hit-act-fit-floor=') for x in dose):
            raise ValueError('The first arm must have hit-act fit support off')
        for seed in seeds:
            _, m = sources[seed]
            flags = sorted(x for x in m['extraFlags'] if x not in diagnostics and not mechanism_flag(x))
            if flags != base_flags or m['profile'] != 'game':
                raise ValueError(f'Configuration mismatch: {arm}')
            if m['weeks'] < args.weeks:
                raise ValueError('Incomplete observation duration')
            if sorted(x for x in m['extraFlags'] if mechanism_flag(x)) != dose:
                raise ValueError(f'Mixed support doses: {arm}')
    out = Path(args.output); out.mkdir(parents=True, exist_ok=True)
    summaries, all_a, all_g, all_c, all_r, all_career = [], [], [], [], [], []
    for arm in runs:
        for seed in seeds:
            tag, manifest = available[arm][seed]
            c, r, careers, a, guards = measure(tag, seed, manifest['weeks'], args.weeks)
            all_c.append(c.assign(arm=arm, seed=seed)); all_r.append(r.assign(arm=arm, seed=seed))
            if len(careers): all_career.append(careers.assign(arm=arm, seed=seed))
            all_a.extend(dict(arm=arm, seed=seed, year=y, **v) for y, v in a.items())
            all_g.append(dict(arm=arm, seed=seed, **guards))
            for group, q in c.groupby('group'):
                z = r[r.group.eq(group)]
                rows = dict(arm=arm, seed=seed, group=group, **summary(q, z))
                counts = z.groupby('artistId').top40In52.sum().reindex(q.artistId, fill_value=0)
                rows['actsWithFollowupTop40'] = float(counts.gt(0).mean())
                rows['followupTop40sPerAct'] = float(counts.mean())
                rows['top10ActsWithNoFollowupTop40'] = float(counts.eq(0).mean())
                rows['lowerChartOnlyPerAct'] = float(z.peakIn52.between(41, 100).sum() / len(q))
                rows['below60OnlyPerAct'] = float(z.peakIn52.between(61, 100).sum() / len(q))
                if len(careers):
                    career = careers[careers.group.eq(group)]
                    rows.update(recognitionGain=float(career.recognitionChange.mean()),
                        newEstablishedActs=int(career.newEstablished.sum()),
                        everEstablishedActs=int(career.everNewEstablished.sum()),
                        promotedThenUnownedActs=int(career.promotedThenUnowned.sum()),
                        promotedThenTerminalActs=int(career.promotedThenTerminal.sum()),
                        unownedAtEnd=float(career.unownedAtEnd.mean()), weeksEstablished=float(career.weeksEstablished.mean()),
                        unownedWeeks=float(career.unownedWeeks.mean()),
                        activeUnownedAtEnd=float(career.activeUnownedAtEnd.mean()), activeUnownedWeeks=float(career.activeUnownedWeeks.mean()),
                        startedBelowEstablishedActs=int(career.startedBelowEstablished.sum()),
                        establishedAtEnd=float(career.establishedAtEnd.mean()), starAtEnd=float(career.starAtEnd.mean()),
                        declinedAtEnd=float(career.declinedAtEnd.mean()),
                        belowEstablishedToEstablished=float(career.newEstablished.sum() / max(1, career.startedBelowEstablished.sum())))
                summaries.append(rows)
    s, a, g = pd.DataFrame(summaries), pd.DataFrame(all_a), pd.DataFrame(all_g)
    # Effective purchaseCapacity subtracts endogenous off-chart/LP holds. The authored base
    # and album channel capacity, before those withdrawals, must remain identical.
    if g.groupby('seed')[['basePurchaseCapacity', 'albumChannelCapacity']].nunique().gt(1).any().any():
        raise ValueError('A treatment changed the authored consumer-capacity window')
    s.to_csv(out / 'cohort-summary.csv', index=False); a.to_csv(out / 'annual.csv', index=False); g.to_csv(out / 'guards.csv', index=False)
    pd.concat(all_c).to_json(out / 'cohorts.json', orient='records', indent=2)
    pd.concat(all_r).to_csv(out / 'followup-releases.csv', index=False)
    career_frame = pd.concat(all_career) if all_career else pd.DataFrame()
    career_seeds = sorted(set.intersection(*[set(career_frame.loc[career_frame.arm.eq(arm), 'seed']) for arm in runs])) if len(career_frame) else []
    if len(career_frame): career_frame.to_csv(out / 'career-progress.csv', index=False)
    fm = s[s.group.eq('field')].groupby('arm').mean(numeric_only=True).reindex(runs)
    am, gm = a.groupby('arm').mean(numeric_only=True).reindex(runs), g.groupby('arm').mean(numeric_only=True).reindex(runs)
    lines = ['# Recent-hit commercial-fit support: controlled screen', '',
        f'Common completed seeds: {seeds}. Observation through week {args.weeks}. Runs: {runs}.', '',
        'All treatment flags match the control except the named experimental mechanisms and read-only tracing. No adoption is implied.', '',
        '| Field first-hit cohort, fully followed 52 weeks | ' + ' | '.join(runs) + ' |', '|---|' + '---:|' * len(runs)]
    keys = ['acts', 'releasesPerAct', 'debutsPerAct', 'entry13Share', 'noDebutShare', 'top10PerEntry',
        'top10PerAct', 'firstDebutGap', 'actsWithFollowupTop40', 'followupTop40sPerAct', 'lowerChartOnlyPerAct', 'below60OnlyPerAct',
        ]
    for key in keys:
        if key in fm: lines.append('| ' + key + ' | ' + ' | '.join(f'{fm.loc[arm,key]:.3f}' for arm in runs) + ' |')
    if career_seeds:
        cm = s[s.group.eq('field') & s.seed.isin(career_seeds)].groupby('arm').mean(numeric_only=True).reindex(runs)
        lines += ['', f'Career trajectories: matched seeds {career_seeds} only (archived controls may lack this new trace).', '',
                  '| Career metric, field cohort | ' + ' | '.join(runs) + ' |', '|---|' + '---:|' * len(runs)]
        for key in ['recognitionGain', 'newEstablishedActs', 'everEstablishedActs', 'promotedThenUnownedActs', 'promotedThenTerminalActs',
                    'startedBelowEstablishedActs', 'establishedAtEnd', 'starAtEnd', 'declinedAtEnd', 'unownedAtEnd', 'activeUnownedAtEnd',
                    'unownedWeeks', 'activeUnownedWeeks', 'weeksEstablished', 'belowEstablishedToEstablished']:
            lines.append('| ' + key + ' | ' + ' | '.join(f'{cm.loc[arm,key]:.3f}' for arm in runs) + ' |')
        first_career = career_frame[career_frame.seed.isin(career_seeds) & career_frame.group.eq('field') & career_frame.firstCareerTop10.eq(1)]
        first_summary = []
        for (arm, seed), q in first_career.groupby(['arm', 'seed']):
            first_summary.append(dict(arm=arm, seed=seed, acts=len(q), newEstablishedActs=int(q.newEstablished.sum()),
                everEstablishedActs=int(q.everNewEstablished.sum()), promotedThenUnownedActs=int(q.promotedThenUnowned.sum()),
                establishedAtEnd=float(q.establishedAtEnd.mean()), declinedAtEnd=float(q.declinedAtEnd.mean()),
                recognitionGain=float(q.recognitionChange.mean())))
        first_summary = pd.DataFrame(first_summary)
        first_summary.to_csv(out / 'first-career-hit-summary.csv', index=False)
        first_mean = first_summary.groupby('arm').mean(numeric_only=True).reindex(runs)
        lines += ['', 'First-career-hit proxy: at most one credited career Top 10 at the first observed milestone (album credits included).', '',
                  '| Field first-career-hit cohort | ' + ' | '.join(runs) + ' |', '|---|' + '---:|' * len(runs)]
        for key in ['acts', 'newEstablishedActs', 'everEstablishedActs', 'promotedThenUnownedActs', 'establishedAtEnd', 'declinedAtEnd', 'recognitionGain']:
            lines.append('| ' + key + ' | ' + ' | '.join(f'{first_mean.loc[arm,key]:.3f}' for arm in runs) + ' |')
    lines += ['', '| Annual complete-year mean | ' + ' | '.join(runs) + ' |', '|---|' + '---:|' * len(runs)]
    for key in ['repeat2', 'repeat3', 'field_repeat2', 'big5_repeat2', 'studio_repeat2', 'hot100Records']:
        lines.append('| ' + key + ' | ' + ' | '.join(f'{am.loc[arm,key]:.3f}' for arm in runs) + ' |')
    lines += ['', '| Economic guard, same observation window | ' + ' | '.join(runs) + ' |', '|---|' + '---:|' * len(runs)]
    for key in ['singleLaunches', 'r', 'singleUnitsM', 'top10UnitsM', 'actualBig5UnitShare', 'openingFieldMidClosures', 'matureRetiredLossShare', 'matureStillActive']:
        lines.append('| ' + key + ' | ' + ' | '.join(f'{gm.loc[arm,key]:.3f}' for arm in runs) + ' |')
    lines += ['', 'Gap values are means of within-seed medians; silence stays in count/rate denominators. Entry13 '
        'uses follow-ups launched in the first 39 weeks after the hit, each observed for the same 13 weeks. '
        'Top 10 conversion uses the fixed 52-week endpoint. Cohort membership can change under treatment; '
        'read hit-act breadth alongside per-act rates. Career counters can include album credits; the release '
        'funnel measures singles only. Star is a title requiring a number one under the unchanged ladder. '
        'Recognition and Established progression give separate measures of career continuity. '
        'newEstablishedActs counts promotions still held at the endpoint; everEstablishedActs also retains promotions '
        'followed by expiry/drop. Promoted-then-unowned retains active acts only; terminal exits are separate. '
        'Unowned endpoint is shown separately because contract exit can set Dropped. '
        'Mature loss share describes retired singles launched at least 26 weeks earlier; active records are reported, not '
        'declared profitable. Big Five unit share excludes the synthetic off-chart allowance. A longer control '
        'is truncated before every weekly calculation; final whole-run aggregates are not used.', '']
    (out / 'review.md').write_text('\n'.join(lines), encoding='utf-8')
    sources = {arm: {seed: dict(tag=available[arm][seed][0], sourceWeeks=available[arm][seed][1]['weeks']) for seed in seeds} for arm in runs}
    paired = {}
    for arm in list(runs)[1:]:
        paired[arm] = {}
        career_keys = ['recognitionGain', 'newEstablishedActs', 'everEstablishedActs', 'promotedThenUnownedActs', 'belowEstablishedToEstablished', 'declinedAtEnd'] if len(career_frame) and career_seeds == seeds else []
        for frame, keys in [(s[s.group.eq('field')], ['releasesPerAct', 'debutsPerAct', 'entry13Share', 'noDebutShare',
                                'firstDebutGap', 'top10PerAct', 'top10PerEntry', 'actsWithFollowupTop40'] + career_keys),
                            (a.groupby(['arm', 'seed']).mean(numeric_only=True).reset_index(),
                                ['repeat2', 'repeat3', 'field_repeat2', 'big5_repeat2', 'studio_repeat2',
                                 'field_top10Share', 'big5_top10Share', 'studio_top10Share', 'top10WeeksPerRecord', 'numberOneRecords']),
                            (g, ['r', 'singleUnitsM', 'top10UnitsM', 'actualBig5UnitShare', 'openingFieldMidClosures'])]:
            for key in keys:
                indexed = frame.set_index(['arm', 'seed'])
                diffs = [float(indexed.loc[(arm, seed), key] - indexed.loc[(control, seed), key]) for seed in seeds]
                paired[arm][key] = dict(mean=statistics.mean(diffs),
                    se=statistics.stdev(diffs) / math.sqrt(len(diffs)) if len(diffs) > 1 else None, perSeed=diffs)
    (out / 'paired.json').write_text(json.dumps(paired, indent=2), encoding='utf-8')
    (out / 'metadata.json').write_text(json.dumps(dict(runs=runs, sources=sources, commonCompletedSeeds=seeds, careerMatchedSeeds=career_seeds, weeks=args.weeks), indent=2), encoding='utf-8')
    print('\n'.join(lines))


if __name__ == '__main__':
    main()
