"""Matched omission of LP assembly from the broad combined configuration.

The separate stock-aware master-cost correction remains enabled in both arms.
"""
import json
from collections import Counter

import analyze_writing_full_access as access
from analyze_writing_combined_integration import economy
from analyze_writing_concentration import iter_rows

SWITCH = '--lp-assembly'
CONTROL_TAG = 'writing-combined-integration'
TREATMENT_TAG = 'writing-combined-no-lp-assembly'


def validate_pair(control, treatment):
    def flags(m):
        fs = m['flags']
        if len(fs) != len({f.split('=')[0] for f in fs}):
            raise ValueError('Duplicate explicit flag keys')
        return {f for f in fs if not f.startswith('--run=')}
    cf, tf = flags(control), flags(treatment)
    if control['assemblySha256'] != treatment['assemblySha256']:
        raise ValueError('Mixed assemblies')
    if not access.REQUIRED <= cf or not {SWITCH, '--album-master-costs', '--album-release-practice',
            '--release-all-in-economics', '--album-prior-recalibration'} <= cf or tf != cf - {SWITCH}:
        raise ValueError('Expected omission of LP assembly only, keeping LP-cost corrections')
    seeds = [f for f in cf if f.startswith('--seed=')]
    if len(seeds) != 1:
        raise ValueError('Expected one matched seed')
    return int(seeds[0].split('=')[1])


def lp_outputs(run):
    prepared = {}
    for r in iter_rows(run, 'release-slate'):
        if r['format'] == 'Album' and r['recordId'] and r['preparedWeek']:
            prepared[r['recordId']] = r
    released = {r['albumRecordId'] for r in iter_rows(run, 'album-projects')
                if r['terminalState'] == 'Released' and int(r['scheduledWeek']) > 0}
    assembly = {}
    for r in iter_rows(run, 'lp-assembly'):
        assembly[r['recordId']] = r
    formats = {}
    for r in iter_rows(run, 'album-practice'):
        if r['albumId'] and r['format']:
            formats[r['albumId']] = r['format']
    return dict(preparedSlateAlbumMasters=len(prepared),
                paidSlateAlbumProduction=sum(float(r['paidProduction']) for r in prepared.values()),
                preparedButNoReleasedAlbumUse=sum(k not in released for k in prepared),
                releasedAlbumFormats=dict(Counter(v for k,v in formats.items() if k in released)),
                releasedAssemblyKinds=dict(Counter(r['kind'] for k,r in assembly.items() if k in released)),
                releasedAssemblyNewSessions=sum(int(r['newSessions']) for k,r in assembly.items() if k in released),
                releasedAssemblyReusedMasters=sum(int(r['releasedMasters'])+int(r['shelvedMasters'])
                    for k,r in assembly.items() if k in released))


def compare(seeds=(1001,2002)):
    if not seeds or len(set(seeds)) != len(seeds):
        raise ValueError('Expected distinct seeds')
    pairs = []
    for seed in seeds:
        runs = [f'{tag}-market-{seed}' for tag in (CONTROL_TAG,TREATMENT_TAG)]
        manifests = [json.loads((access.CAL/f'{r}-manifest.json').read_text(encoding='utf-8-sig')) for r in runs]
        if validate_pair(*manifests) != seed:
            raise ValueError('Seed does not match run name')
        for run in runs:
            log = (access.CAL/f'{run}.log').read_text(encoding='utf-8-sig')
            errors = (access.CAL/f'{run}-errors.log').read_text(encoding='utf-8-sig')
            if f'CHART_AUDIT_COMPLETE run={run} weeks=104' not in log or 'ERROR:' in errors or 'Exception' in errors:
                raise ValueError(f'{run}: incomplete or engine errors')
        pair = dict(seed=seed)
        for side,run in zip(('control','treatment'),runs):
            pair[side] = access.arm(run)
            pair[side]['economy'] = economy(run)
            pair[side]['lpOutputs'] = lp_outputs(run)
        pairs.append(pair)
    result = access.comparison_result(pairs)
    result.update(controlTag=CONTROL_TAG, treatmentTag=TREATMENT_TAG,
                  contrast='Assembly on versus omitted; all other explicit flags and assembly hash match.')
    return result


if __name__ == '__main__':
    result = compare()
    (access.CAL/'writing-lp-assembly-turnover-comparison.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(result['pooled'],indent=2))
