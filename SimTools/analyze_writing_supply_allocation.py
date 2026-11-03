"""Matched rock supply experiment and observational single/LP allocation joins.

No engine changes. Composition hook is a diagnostic, not perceived suitability,
expected profit, or evidence that a counterfactual single would be a hit.
"""
import argparse
import json
from collections import Counter, defaultdict

import analyze_writing_full_access as access
from analyze_writing_concentration import iter_rows


def control_name(seed):
    return (f'writing-full-profile-market-{seed}' if seed == 1001 else
            f'writing-full-profile-extension-market-{seed}')


def validate_supply_pair(control, treatment):
    def flags(m):
        fs = m['flags']
        if len(fs) != len(set(fs)):
            raise ValueError('Duplicate run flags')
        return {f for f in fs if not f.startswith('--run=')}
    cf, tf = flags(control), flags(treatment)
    if control['assemblySha256'] != treatment['assemblySha256']:
        raise ValueError('Mixed assemblies')
    if not access.REQUIRED <= cf or '--rock-hit-writers' in cf or tf != cf | {'--rock-hit-writers'}:
        raise ValueError('Unexpected matched-profile flags')
    seeds = [f for f in cf if f.startswith('--seed=')]
    if len(seeds) != 1:
        raise ValueError('Expected one matched seed')
    return int(seeds[0].split('=')[1])


def compare_supply(seeds):
    if not seeds or len(set(seeds)) != len(seeds):
        raise ValueError('Expected distinct seeds')
    pairs = []
    for seed in seeds:
        runs = (control_name(seed), f'writing-full-rock-supply-market-{seed}')
        manifests = [json.loads((access.CAL / f'{r}-manifest.json').read_text(encoding='utf-8-sig'))
                     for r in runs]
        if validate_supply_pair(*manifests) != seed:
            raise ValueError('Seed mismatch')
        for run in runs:
            log = (access.CAL / f'{run}.log').read_text(encoding='utf-8-sig')
            errors = (access.CAL / f'{run}-errors.log').read_text(encoding='utf-8-sig')
            if f'CHART_AUDIT_COMPLETE run={run} weeks=104' not in log or 'ERROR:' in errors or 'Exception' in errors:
                raise ValueError(f'{run}: incomplete or engine errors')
        pairs.append(dict(seed=seed, control=access.arm(runs[0]), treatment=access.arm(runs[1])))
    return access.comparison_result(pairs)


def allocation(run):
    for suffix in ('writing-demos', 'writing-launches', 'writing-album-tracks', 'release-slate'):
        if not (access.CAL.parent / f'{run}-{suffix}.csv').is_file():
            raise ValueError(f'{run}: missing {suffix}')
    return allocation_rows(run)


def allocation_rows(run):
    opening, first_recorded = set(), {}
    for r in iter_rows(run, 'writing-demos'):
        week = int(r['week'])
        if week == 0 and r['status'] == 'Recorded':
            opening.add(r['songId'])
        if week > 0 and r['status'] == 'Recorded' and r['songId'] not in opening:
            first_recorded.setdefault(r['songId'], r)
    launches = list(iter_rows(run, 'writing-launches'))
    professional_singles = {(r['songId'], r['artistId']) for r in launches
                            if int(r['week']) > 0 and r['source'] == 'ExternalProfessional'}
    albums = defaultdict(list)
    for r in iter_rows(run, 'writing-album-tracks'):
        if r['terminalState'] == 'Released' and r['source'] == 'ExternalProfessional':
            albums[(r['songId'], r['artistId'])].append(r)
    # Last observed state at or before the first Recorded snapshot. State is weekly;
    # ordering within a week cannot be reconstructed from these outputs.
    plans = defaultdict(list)
    album_prepared = {}
    for r in iter_rows(run, 'release-slate'):
        if r['format'] == 'Single':
            plans[(r['labelId'], r['artistId'])].append(r)
        elif r['format'] == 'Album' and r['recordId'] and r['preparedWeek']:
            album_prepared[r['recordId']] = int(r['preparedWeek'])
    singles = defaultdict(list)
    for r in launches:
        singles[(r['labelId'], r['artistId'])].append(r)
    counters, sources, examples = Counter(), Counter(), []
    unique_singles, unprepared_singles, higher_unprepared_singles = set(), set(), set()
    gaps = []
    for song, demo in first_recorded.items():
        key = (song, demo['artistId'])
        if key not in albums or key in professional_singles:
            continue
        counters['albumOnly'] += 1
        week = int(demo['week'])
        scheduled = min(int(a['scheduledWeek']) for a in albums[key])
        if any(album_prepared.get(a.get('albumRecordId')) == week for a in albums[key]):
            counters['releasedAlbumMasterPreparedAtFirstRecordedWeek'] += 1
        counters['firstAlbumScheduledSameWeekAsRecorded' if scheduled == week else
                 'firstAlbumScheduledBeforeRecorded' if scheduled < week else
                 'firstAlbumScheduledAfterRecorded'] += 1
        if week > 78:
            continue
        counters['albumOnlyRecordedBy78'] += 1
        act_key = (demo['labelId'], demo['artistId'])
        states = {}
        for p in plans[act_key]:
            if int(p['week']) <= week:
                if p['slotId'] not in states or int(p['week']) >= int(states[p['slotId']]['week']):
                    states[p['slotId']] = p
        open_plans = [p for p in states.values()
                      if p['state'] not in ('Issued', 'Cancelled')
                      and week <= int(p['intendedWeek']) <= week + 26]
        if not open_plans:
            continue
        counters['withOpenUpcomingSinglePlan'] += 1
        # Issued plan joins use actual master IDs, same label and artist, with a
        # complete 26-week follow-up. No claim that a plan could be cheaply changed.
        ids = {p['slotId'] for p in open_plans}
        issued_ids = {p['recordId'] for p in plans[act_key]
                      if p['slotId'] in ids and p['state'] == 'Issued'
                      and week < int(p['actualWeek']) <= week + 26}
        next_singles = sorted((s for s in singles[act_key] if s['recordId'] in issued_ids
                               and week < int(s['week']) <= week + 26),
                              key=lambda s: (int(s['week']), s['recordId']))
        if not next_singles:
            continue
        single = next_singles[0]
        counters['withSubsequentlyIssuedPlan'] += 1
        unique_singles.add(single['recordId'])
        matching = [p for p in open_plans if any(q['slotId'] == p['slotId'] and q['state'] == 'Issued'
                    and q['recordId'] == single['recordId'] for q in plans[act_key])]
        unprepared = any(not p['preparedWeek'] and not p['recordId'] and float(p['paidProduction']) == 0
                         and p['state'] != 'AwaitingDelivery' for p in matching)
        if unprepared:
            counters['nextIssuedPlanUnpreparedAtSnapshot'] += 1
            unprepared_singles.add(single['recordId'])
        sources[single['source']] += 1
        gap = float(demo['compositionHook']) - float(single['compositionHook'])
        gaps.append(gap)
        if gap > 0:
            counters['albumCompositionHookHigher'] += 1
        if gap >= .1:
            counters['albumCompositionHookAtLeastPoint1Higher'] += 1
            if unprepared:
                counters['unpreparedPlanAndHookAtLeastPoint1Higher'] += 1
                higher_unprepared_singles.add(single['recordId'])
            if single['source'] in ('CoverStandard', 'CoverRecentHit'):
                counters['higherByPoint1ThanCover'] += 1
        if float(demo['compositionHook']) >= .8:
            counters['albumCompositionHookAtLeastPoint8'] += 1
        examples.append(dict(songId=song, artistId=demo['artistId'], labelId=demo['labelId'],
                             recordedWeek=week, albumProjectIds=sorted({a['projectId'] for a in albums[key]}),
                             feePaid=float(demo['feePaid']), albumCompositionHook=float(demo['compositionHook']),
                             singleRecordId=single['recordId'], singleWeek=int(single['week']),
                             singleSource=single['source'], singleCompositionHook=float(single['compositionHook']),
                             hookGap=gap, nextIssuedPlanUnprepared=unprepared,
                             firstAlbumScheduledWeek=scheduled, openPlanIds=sorted(ids)))
    examples.sort(key=lambda e: (-e['hookGap'], e['songId']))
    counters['distinctNextIssuedSingles'] = len(unique_singles)
    counters['distinctUnpreparedNextIssuedSingles'] = len(unprepared_singles)
    counters['distinctUnpreparedSinglesWithAlbumHookHigherByPoint1'] = len(higher_unprepared_singles)
    return dict(run=run, counts=dict(counters), nextIssuedPlanSources=dict(sources),
                meanCompositionHookGap=sum(gaps) / len(gaps) if gaps else None,
                examples=examples[:20], stockDestinations=access.stock_destinations(run),
                limitations=['Weekly snapshots do not establish within-week event order.',
                             'Demo/first-client unit: several album songs can share one next single.',
                             'Composition hooks are not act fit, perceived reads, master quality or profitability.',
                             'No contract sequence, master ownership history or alternative-project quotation in these joins.'])


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--supply', action='store_true')
    p.add_argument('--seeds', type=int, nargs='+', default=[1001, 2002, 3003])
    args = p.parse_args()
    if not args.seeds or len(set(args.seeds)) != len(args.seeds):
        raise ValueError('Expected distinct seeds')
    if args.supply:
        result = compare_supply(args.seeds)
        name = 'writing-full-rock-supply-comparison.json'
        summary = result['pooled']
    else:
        hashes = set()
        for seed in args.seeds:
            run = control_name(seed)
            manifest = json.loads((access.CAL / f'{run}-manifest.json').read_text(encoding='utf-8-sig'))
            flags = set(manifest['flags'])
            if not access.REQUIRED <= flags or f'--seed={seed}' not in flags:
                raise ValueError(f'{run}: unexpected control profile')
            log = (access.CAL / f'{run}.log').read_text(encoding='utf-8-sig')
            errors = (access.CAL / f'{run}-errors.log').read_text(encoding='utf-8-sig')
            if f'CHART_AUDIT_COMPLETE run={run} weeks=104' not in log or 'ERROR:' in errors or 'Exception' in errors:
                raise ValueError(f'{run}: incomplete or engine errors')
            hashes.add(manifest['assemblySha256'])
        if len(hashes) != 1:
            raise ValueError('Mixed control assemblies')
        result = [allocation(control_name(s)) for s in args.seeds]
        name = 'writing-full-allocation-diagnosis.json'
        summary = [dict(run=r['run'], counts=r['counts'], nextIssuedPlanSources=r['nextIssuedPlanSources'],
                        meanCompositionHookGap=r['meanCompositionHookGap']) for r in result]
    (access.CAL / name).write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(summary, indent=2))
