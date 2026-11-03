"""Matched full-profile test of accessible professional-demo sampling.

Covers exclude traditional/public-domain material. Keep mature intentions and
complete chart-window launches separate: their denominators are different.
"""
import argparse
import json
from collections import Counter
from pathlib import Path

from analyze_fresh_material_access import summarize as intentions
from analyze_material_funnel import summarize as funnel
from analyze_writing_concentration import iter_rows
from analyze_writing_workflow import summarize as workflow

CAL = Path(__file__).resolve().parents[1] / 'SimLogs' / 'writing-calibration'
SWITCH = '--material-accessible-sampling'
REQUIRED = {'--default-simulation-profile', '--causal-media-atmosphere',
            '--media-recognition', '--fame-registration', '--observe-following',
            '--media-residencies', '--follower-demand', '--professional-writing-market',
            '--writing-market-planned-assignments', '--writing-market-delivery-offers',
            '--writing-concentration-audit', '--material-funnel-audit', '--weeks=104'}


def validate_pair(control, treatment):
    def flags(m):
        fs = m['flags']
        if len(fs) != len(set(fs)):
            raise ValueError('Duplicate run flags')
        return {f for f in fs if not f.startswith('--run=')}
    cf, tf = flags(control), flags(treatment)
    if control['assemblySha256'] != treatment['assemblySha256']:
        raise ValueError('Mixed assemblies')
    if not REQUIRED <= cf or SWITCH in cf or tf != cf | {SWITCH}:
        raise ValueError('Unexpected matched-profile flags')
    seeds = [f for f in cf if f.startswith('--seed=')]
    if len(seeds) != 1:
        raise ValueError('Expected one matched seed')
    return int(seeds[0].split('=')[1])


def mix(sources):
    professional = sources.get('ExternalProfessional', 0)
    covers = sources.get('CoverStandard', 0) + sources.get('CoverRecentHit', 0)
    total = sum(sources.values())
    return dict(sources=dict(sources), singles=total, professional=professional,
                covers=covers, professionalShare=professional / total if total else None,
                coversPerProfessional=covers / professional if professional else None,
                professionalPerCover=professional / covers if covers else None)


def stock_destinations(run):
    """Endpoint inventory and observed uses of newly recorded demos, not causal losses."""
    opening_recorded, latest = set(), {}
    for row in iter_rows(run, 'writing-demos'):
        if int(row['week']) == 0 and row['status'] == 'Recorded':
            opening_recorded.add(row['songId'])
        latest[row['songId']] = row
    singles = {(r['songId'], r['artistId']) for r in iter_rows(run, 'writing-launches')
               if r['source'] == 'ExternalProfessional' and int(r['week']) > 0}
    albums = {(r['songId'], r['artistId']) for r in iter_rows(run, 'writing-album-tracks')
              if r['source'] == 'ExternalProfessional' and r['terminalState'] == 'Released'}
    destinations = Counter()
    for song, row in latest.items():
        if row['status'] != 'Recorded' or song in opening_recorded:
            continue
        key = (song, row['artistId'])
        destination = ('both' if key in singles and key in albums else
                       'singleOnly' if key in singles else 'albumOnly' if key in albums else
                       'noObservedReleasedUse')
        destinations[destination] += 1
    supplier_weeks = {}
    for row in iter_rows(run, 'writing-suppliers'):
        week = int(row['week'])
        counts = supplier_weeks.setdefault(week, Counter())
        for key in ('available', 'shelved', 'licensed', 'recorded', 'pending'):
            counts[key] += int(row[key])
    return dict(endpointDemoStatus=dict(Counter(r['status'] for r in latest.values())),
                newlyRecordedDemoDestinations=dict(destinations),
                supplierStockByWeek={str(w): dict(supplier_weeks[w])
                                     for w in (26, 52, 104) if w in supplier_weeks})


def complete_window_mix(run):
    sources = {r['recordId']: r['source'] for r in iter_rows(run, 'writing-launches')}
    counts = Counter()
    for row in iter_rows(run, 'single-launch-quality'):
        if 1 <= int(row['week']) <= 104 - 13:
            if row['singleId'] not in sources:
                raise ValueError(f'{run}: missing launch source')
            counts[sources[row['singleId']]] += 1
    return mix(counts)


def arm(run):
    w = workflow(run, 104)  # Endpoints, ledger, provenance and delivery-order checks.
    i = intentions(run, 104)
    f = funnel(run, 104)
    launch_mix = complete_window_mix(run)
    if abs(launch_mix['professionalShare'] - w['professionalSingleShare']) > 1e-12:
        raise ValueError('Inconsistent professional launch denominators')
    return dict(run=run, workflow=w, matureIntentions=mix(i['careers']['all']['issueSources']),
                completeChartWindowLaunches=launch_mix,
                careers=i['careers'], genres=i['issuedGenres'], funnel=f,
                stockDestinations=stock_destinations(run))


def compare(seeds):
    if not seeds or len(set(seeds)) != len(seeds):
        raise ValueError('Expected distinct seeds')
    pairs = []
    for seed in seeds:
        control = (f'writing-full-profile-market-{seed}' if seed == 1001 else
                   f'writing-full-profile-extension-market-{seed}')
        treatment = f'writing-full-access-market-{seed}'
        cm, tm = [json.loads((CAL / f'{r}-manifest.json').read_text(encoding='utf-8-sig'))
                  for r in (control, treatment)]
        if validate_pair(cm, tm) != seed:
            raise ValueError('Seed does not match run name')
        for run in (control, treatment):
            log = (CAL / f'{run}.log').read_text(encoding='utf-8-sig')
            if f'CHART_AUDIT_COMPLETE run={run} weeks=104' not in log:
                raise ValueError(f'{run}: missing completion marker')
            errors = (CAL / f'{run}-errors.log').read_text(encoding='utf-8-sig')
            if 'ERROR:' in errors or 'Exception' in errors:
                raise ValueError(f'{run}: engine errors')
        pairs.append(dict(seed=seed, control=arm(control), treatment=arm(treatment)))
    return comparison_result(pairs)


def comparison_result(pairs):
    """Combine independently validated seed comparisons using pooled counts."""
    if not pairs or len({p['seed'] for p in pairs}) != len(pairs):
        raise ValueError('Expected distinct seed comparisons')
    hashes = {p[side]['workflow']['assemblySha256'] for p in pairs for side in ('control', 'treatment')}
    if len(hashes) != 1:
        raise ValueError('Mixed assemblies across seeds')
    pooled = {}
    for key in ('matureIntentions', 'completeChartWindowLaunches'):
        pooled[key] = {}
        for side in ('control', 'treatment'):
            counts = Counter()
            for pair in pairs:
                counts.update(pair[side][key]['sources'])
            pooled[key][side] = mix(counts)
    return dict(assemblySha256=hashes.pop(), pairs=pairs, pooled=pooled)


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--seeds', type=int, nargs='+', default=[1001, 2002, 3003])
    p.add_argument('--output', type=Path, default=CAL / 'writing-full-access-comparison.json')
    args = p.parse_args()
    result = compare(args.seeds)
    args.output.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(result['pooled'], indent=2))
