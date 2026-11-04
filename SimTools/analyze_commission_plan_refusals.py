"""Describe arrival-time plan refusals and later observed eligibility, not causal effects."""
import argparse
import json
from collections import Counter, defaultdict
from pathlib import Path
from analyze_material_funnel import rows, ROOT


def summarize(run, weeks, appearance=8, outcome=26):
    if max(int(r['week']) for r in rows(run, 'records')) != weeks:
        raise ValueError(f'{run}: incomplete endpoint')
    eligible = defaultdict(list)
    for r in rows(run, 'commission-eligible-plans'):
        eligible[(r['labelId'], r['artistId'])].append(r)
    slate = defaultdict(list)
    for r in rows(run, 'release-slate'):
        slate[r['slotId']].append(r)
    launches = {r['recordId']: r for r in rows(run, 'writing-launches')}
    all_reasons, initialization, overlap, mature = Counter(), Counter(), Counter(), defaultdict(Counter)
    diagnostics = list(rows(run, 'commission-plan-diagnostic'))
    arrivals = [r for r in rows(run, 'professional-arrivals') if r['outcome'] == 'commission-refused:no-plan-minimum-delivery']
    if len(diagnostics) != len(arrivals):
        raise ValueError(f'{run}: diagnostic and arrival counts differ')
    for r, arrival in zip(diagnostics, arrivals):
        if any(r[k] != arrival[k] for k in ('week', 'labelId', 'artistId')):
            raise ValueError(f'{run}: diagnostic and arrival ordering differs')
        week = int(r['week'])
        if arrival['provenance'] == 'initialization':
            initialization[r['primary']] += 1
            continue
        if arrival['provenance'] != 'arrival':
            raise ValueError(f"{run}: unexpected provenance {arrival['provenance']}")
        all_reasons[r['primary']] += 1
        for k in ('closed', 'preparing', 'material', 'lead', 'horizon'):
            overlap[k] += int(r[k])
        if week < 1 or week > weeks - outcome:
            continue
        c = mature[r['primary']]
        c['events'] += 1
        candidates = [x for x in eligible[(r['labelId'], r['artistId'])]
                      if week < int(x['week']) <= week + appearance]
        if not candidates:
            c['noObservedEligible'] += 1
            continue
        first = min(candidates, key=lambda x: (int(x['week']), x['slotId']))
        c['observedEligible'] += 1
        c['waitWeeksTotal'] += int(first['week']) - week
        history = [x for x in slate[first['slotId']] if int(first['week']) <= int(x['week']) <= week + outcome]
        issued = next((x for x in history if x['state'] == 'Issued'), None)
        c['issued'] += bool(issued)
        if issued:
            c['issuedSource_' + launches.get(issued['recordId'], {}).get('source', 'missing')] += 1
        c['cancelled'] += any(x['state'] == 'Cancelled' for x in history)
        c['prepared'] += any(x['preparedWeek'] and int(x['preparedWeek']) <= week + outcome for x in history)
    return {'run': run, 'weeks': weeks, 'allPrimary': dict(all_reasons), 'initializationPrimary': dict(initialization), 'overlapFlags': dict(overlap),
            'matureByPrimary': {k: dict(v) for k, v in sorted(mature.items())}}


def main():
    p = argparse.ArgumentParser()
    p.add_argument('runs', nargs='+')
    p.add_argument('--weeks', type=int, default=52)
    p.add_argument('--output', type=Path, required=True)
    args = p.parse_args()
    manifests = [json.loads((ROOT / 'writing-calibration' / f'{r}-manifest.json').read_text(encoding='utf-8-sig')) for r in args.runs]
    if len({m['assemblySha256'] for m in manifests}) != 1:
        raise ValueError('Mixed assemblies')
    args.output.write_text(json.dumps([summarize(r, args.weeks) for r in args.runs], indent=2) + '\n', encoding='utf-8')


if __name__ == '__main__':
    main()
