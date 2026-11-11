"""Compare short full/half pairs, including deterministic pre-treatment checks."""
import argparse
import hashlib
import json
from collections import Counter, defaultdict
from pathlib import Path

from trace_stature_marketing_mechanisms import rows
from stature_normalization import file_digest


def chart_metrics(directory, name, fixed=None):
    opening, totals, pre, zero = set(), Counter(), hashlib.sha256(), hashlib.sha256()
    chart_rows = list(rows(directory, name, 'records'))
    for r in chart_rows:
        if int(r['week']) == 0:
            opening.add(r['artistId'])
            zero.update(json.dumps(r, sort_keys=True).encode())
    if fixed is None:
        fixed = opening
    for r in chart_rows:
        if int(r['week']) <= 26:
            pre.update(json.dumps(r, sort_keys=True).encode())
        else:
            totals['postTreatmentHot100Slots'] += 1 <= int(r['currentPosition']) <= 100
            if 1 <= int(r['currentPosition']) <= 10:
                totals['postTreatmentTop10Slots'] += 1
                totals['postTreatmentFixedOpeningTop10Slots'] += r['artistId'] in fixed
    return opening, dict(totals), pre.hexdigest(), zero.hexdigest()


def compare(directory, half, full, seeds, output):
    report = {'prefixes': {'half': half, 'full': full}, 'analysisSha256': file_digest(Path(__file__)), 'seeds': {},
        'scope': '39-week startup screen; approximately 13 active weeks. Same seeded startup and verified pre-treatment chart paths; no mid-decade save-state/RNG replay claim. Fixed opening cohort means week-zero chart acts.',
        'caveat': 'Paid spending and chart displacement can be nonlinear despite halving each local envelope deviation. Two prespecified seeds and a short window cannot select a stable decade policy.'}
    metadata = directory / 'writing-calibration'
    for seed in seeds:
        reports = {intensity: json.loads((metadata / f'{prefix}-{seed}-summary.json').read_text(encoding='utf-8-sig')) for intensity, prefix in (('half', half), ('full', full))}
        result, fingerprints, config = {}, {}, []
        fixed = None
        for intensity, prefix in (('half', half), ('full', full)):
            result[intensity] = {}
            for arm in ('control', 'treatment'):
                name = f'{prefix}-{seed}-{arm}'
                request = json.loads((metadata / f'{name}-request.json').read_text(encoding='utf-8-sig'))
                config.append(tuple(request[k] if k != 'sourceContentSha256' else json.dumps(request[k], sort_keys=True) for k in ('assemblySha256', 'engineSha256', 'calibrationSha256', 'sourceContentSha256')))
                if next(f for f in request['flags'] if f.startswith('--weeks=')) != '--weeks=39':
                    raise ValueError('Unexpected experiment duration')
                opening, charts, pre, zero = chart_metrics(directory, name, fixed)
                if fixed is None:
                    fixed = opening
                if opening != fixed:
                    raise ValueError('Opening chart cohort differs')
                fingerprints[intensity, arm] = pre, zero
                economy = reports[intensity]['arms'][arm]
                budgets = defaultdict(Counter)
                for r in rows(directory, name, 'stature-marketing'):
                    if r['context'] != 'release-launch':
                        continue
                    old, paid = float(r['legacyBudget']), float(r['requestedBudget'])
                    c = budgets['fixedOpening' if r['artistId'] in fixed else 'other']
                    c['reads'] += 1; c['increases'] += paid > old; c['reductions'] += paid < old
                    c['absoluteEnvelopeChange'] += abs(paid - old)
                result[intensity][arm] = {'charts': charts, 'launch': economy['releaseCampaignCostEvents'],
                    'followUp': economy['followUpCampaignEvents'], 'slates': economy['slateEvents'],
                    'annual': economy['annualRollup'], 'singleLaunchBudgets': dict(budgets)}
        if len(set(config)) != 1 or len(set(fingerprints.values())) != 1:
            raise ValueError('Frozen sources or pre-treatment chart paths differ')
        # The duplicate controls are a same-build deterministic check.
        manifests = [json.loads((metadata / f'{prefix}-{seed}-control-outputs.json').read_text(encoding='utf-8-sig')) for prefix in (half, full)]
        normalized = [{r['name'].split(f'-{seed}-control-', 1)[1]: r['sha256'] for r in m} for m in manifests]
        if normalized[0] != normalized[1]:
            raise ValueError('Repeated controls are not byte identical')
        result['verification'] = {'openingChartActs': len(fixed), 'preTreatmentChartSha256': next(iter(fingerprints.values()))[0],
            'allRepeatedControlCsvsByteIdentical': len(normalized[0]), 'matchedFrozenInputs': True}
        report['seeds'][str(seed)] = result
    with output.open('x', encoding='utf-8') as f:
        json.dump(report, f, indent=2)
        f.write('\n')


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--directory', type=Path, default=Path('SimLogs'))
    p.add_argument('--half', default='stature-marketing-half-v1')
    p.add_argument('--full', default='stature-marketing-full-v1')
    p.add_argument('--seeds', type=int, nargs='+', default=[1001, 2002])
    p.add_argument('--output', type=Path, required=True)
    a = p.parse_args()
    compare(a.directory, a.half, a.full, a.seeds, a.output)
