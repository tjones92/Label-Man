"""Validate the broad integration screen against the current-profile controls.

This is a many-feature scenario comparison, never an isolated causal arm.
"""
import argparse
import hashlib
import json
from collections import Counter
from pathlib import Path

import analyze_writing_full_access as access
from analyze_writing_supply_allocation import allocation, control_name
from analyze_writing_concentration import iter_rows

FLAGS = Path(__file__).with_name('writing_combined_integration_flags.txt')


def economy(run):
    singles = {r['recordId'] for r in iter_rows(run, 'writing-launches') if int(r['week']) > 0}
    albums = {r['albumRecordId'] for r in iter_rows(run, 'album-projects')
              if int(r['scheduledWeek']) > 0 and r['terminalState'] == 'Released'}
    annual = {}
    for r in iter_rows(run, 'market-clearing-weekly'):
        year = r['year']
        counts = annual.setdefault(year, Counter())
        for key in ('clearedSingleUnits', 'clearedAlbumUnits', 'inventoryViolationCount',
                    'allocationViolationCount', 'reconciliationDelta', 'settlementDelta'):
            counts[key] += int(r[key])
        if any(int(r[k]) != 0 for k in ('inventoryViolationCount', 'allocationViolationCount',
                                        'reconciliationDelta', 'settlementDelta')):
            raise ValueError(f'{run}: market clearing invariant failure')
    labels, first_labels = {}, {}
    for r in iter_rows(run, 'label-finance'):
        first_labels.setdefault(r['labelId'], r)
        labels[r['labelId']] = r
    exited = [r for r in labels.values() if r['status'] == 'Defunct']
    directory = {r['labelId']: r for r in iter_rows(run, 'label-directory')}
    cash_error = max((abs(float(r['cashReconciliationError']))
                      for r in iter_rows(run, 'company-monthly')), default=None)
    return dict(singleLaunches= len(singles), releasedAlbumProjects=len(albums),
                annualStoreClearing={k: dict(v) for k, v in annual.items()},
                endpointLabelStatus=dict(Counter(r['status'] for r in labels.values())),
                exitReasons=dict(Counter(r['exitReason'] for r in exited)),
                exitTiers=dict(Counter(r['labelTier'] for r in exited)),
                exitHistorical=dict(Counter(r['isHistorical'] for r in exited)),
                openingCohortExitsByInitialTier=dict(Counter(directory[r['labelId']]['initialTier'] for r in exited
                    if first_labels[r['labelId']]['week'] == '1' and r['labelId'] in directory)),
                maxMonthlyCashReconciliationError=cash_error,
                scope='Clearing units are the store clearing observer, not summed autonomous trade-channel sales.')


def compare(seeds, tag):
    extras = [s.strip() for s in FLAGS.read_text(encoding='utf-8').splitlines()
              if s.strip() and not s.lstrip().startswith('#')]
    if not seeds or len(set(seeds)) != len(seeds):
        raise ValueError('Expected distinct seeds')
    pairs = []
    for seed in seeds:
        control, treatment = control_name(seed), f'{tag}-market-{seed}'
        cm, tm = [json.loads((access.CAL / f'{r}-manifest.json').read_text(encoding='utf-8-sig'))
                  for r in (control, treatment)]
        def flags(manifest):
            fs = manifest['flags']
            if len(fs) != len({f.split('=')[0] for f in fs}):
                raise ValueError('Duplicate explicit flag keys')
            return {f for f in fs if not f.startswith('--run=')}
        cf, tf = flags(cm), flags(tm)
        if not access.REQUIRED <= cf or f'--seed={seed}' not in cf or tf != cf | set(extras):
            raise ValueError('Unexpected combined configuration')
        if cm['assemblySha256'] != tm['assemblySha256']:
            raise ValueError('Mixed assemblies')
        for run in (control, treatment):
            log = (access.CAL / f'{run}.log').read_text(encoding='utf-8-sig')
            errors = (access.CAL / f'{run}-errors.log').read_text(encoding='utf-8-sig')
            if f'CHART_AUDIT_COMPLETE run={run} weeks=104' not in log or 'ERROR:' in errors or 'Exception' in errors:
                raise ValueError(f'{run}: incomplete or engine errors')
        pair = dict(seed=seed, control=access.arm(control), treatment=access.arm(treatment))
        pair['control']['economy'] = economy(control)
        pair['treatment']['economy'] = economy(treatment)
        pairs.append(pair)
    result = access.comparison_result(pairs)
    result['scenario'] = 'Broad integration; effects cannot be attributed to individual features.'
    result['extraFlags'] = extras
    result['flagFileSha256'] = hashlib.sha256(FLAGS.read_bytes()).hexdigest()
    result['allocation'] = [allocation(f'{tag}-market-{s}') for s in seeds]
    return result


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--seeds', type=int, nargs='+', default=[1001, 2002])
    p.add_argument('--tag', default='writing-combined-integration')
    args = p.parse_args()
    result = compare(args.seeds, args.tag)
    (access.CAL / f'{args.tag}-comparison.json').write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(result['pooled'], indent=2))
