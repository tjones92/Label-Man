"""Trace first-review capacity blocks and commission-arrival outcomes.

Outcomes use a common 26-week window from each single plan's proposal. Arrival
counts are events, not unique acts. These descriptive joins are not causal.
"""
import argparse
import json
from collections import Counter, defaultdict
from pathlib import Path
from analyze_material_funnel import rows, ROOT


def summarize(run, weeks):
    if max(int(r['week']) for r in rows(run, 'records')) != weeks:
        raise ValueError(f'{run}: incomplete endpoint')
    plans = defaultdict(list)
    for r in rows(run, 'release-slate'):
        p = int(r['proposedWeek'])
        if r['format'] == 'Single' and 1 <= p <= weeks - 26:
            plans[r['slotId']].append(r)
    material = defaultdict(list)
    for r in rows(run, 'programme-material-search'):
        material[r['slotId']].append(r)
    launches = {r['recordId']: r for r in rows(run, 'writing-launches')}
    first = {}
    for r in rows(run, 'material-funnel'):
        if r['slotId'] in plans and int(r['week']) <= int(plans[r['slotId']][0]['proposedWeek']) + 26:
            first.setdefault(r['slotId'], r)
    result = Counter()
    groups = defaultdict(Counter)
    blockers = Counter()
    for slot, review in first.items():
        if review['selected'] != 'professional' or review['startBlock'] != 'label-capacity':
            continue
        c = groups[f"{review['career']}|{review['genre']}|" + ('low' if float(review['craft']) < .4 else 'middle' if float(review['craft']) < .6 else 'high')]
        end = int(plans[slot][0]['proposedWeek']) + 26
        start = int(review['week'])
        later = [r for r in material[slot] if start <= int(r['week']) <= end and r['state'] == 'Pending']
        slate = [r for r in plans[slot] if start <= int(r['week']) <= end]
        issued_rows = [r for r in slate if r['state'] == 'Issued']
        issued = bool(issued_rows)
        launch = launches.get(issued_rows[0]['recordId']) if issued else None
        same = [r for r in later if r['songId'] == review['selectedSongId']]
        for target in (result, c):
            target['blockedPlans'] += 1
            target['anySearchStarted'] += bool(later)
            target['sameSongStarted'] += bool(same)
            target['issued'] += issued
            target['noIssueByWindow'] += not issued
            target['prepared'] += any(r.get('preparedWeek') and 1 <= int(r['preparedWeek']) <= end for r in slate)
            target['cancelled'] += any(r['state'] == 'Cancelled' for r in slate)
            if issued:
                target['issuedSource_' + (launch['source'] if launch else 'missingProvenance')] += 1
                target['issuedSameSong'] += bool(launch and launch['songId'] == review['selectedSongId'])
            if same:
                target['sameSongWaitWeeksTotal'] += int(same[0]['week']) - start
        for blocker in filter(None, review['blockingSlots'].split(';')):
            observations = [r for r in material[blocker] if int(r['week']) <= start]
            if not observations:
                blockers['missingHistory'] += 1
                continue
            last = observations[-1]
            blockers['professional' if last['professional'] == '1' else 'cover'] += 1
            blockers['ageWeeksTotal'] += start - int(last['startedWeek'])
    arrivals = defaultdict(Counter)
    for r in rows(run, 'professional-arrivals'):
        arrivals[r['provenance']][r['outcome']] += 1
    return {'run': run, 'weeks': weeks, 'maturePlans': len(plans), 'firstReviews': len(first),
            'capacityBlockedProfessional': dict(result), 'blockerSearches': dict(blockers),
            'blockedByCareerGenreCraft': {k: dict(v) for k, v in sorted(groups.items())},
            'arrivalEvents': {k: dict(v) for k, v in sorted(arrivals.items())}}


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
