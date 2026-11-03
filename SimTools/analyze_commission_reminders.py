"""Deduplicate refused arrivals by subsequent artist/plan and inspect booking gates.

Snapshots observe existing behavior; they do not reserve a writer or model a reminder.
"""
import argparse
import json
from collections import Counter, defaultdict
from pathlib import Path
from analyze_material_funnel import rows, ROOT


def gate(row):
    if row['clientMatches'] != '1':
        return 'invalid-client'
    if row['owned'] == '1':
        return 'owned-delivery'
    if row['pending'] == '1':
        return 'pending-order'
    if row['recent'] == '1':
        return 'recent-order-only'
    if not row['writerId']:
        return 'no-available-writer'
    if row['reserve'] != '1':
        return 'cash-reserve'
    if not row['selectedPlanId']:
        return 'actual-delivery-plan'
    if row['deliveryActive'] != '1':
        return 'writer-inactive-on-delivery'
    return 'passes-observed-gates'


def summarize(run, weeks, appearance=8, outcome=26):
    if max(int(r['week']) for r in rows(run, 'records')) != weeks:
        raise ValueError(f'{run}: incomplete endpoint')
    arrivals = [r for r in rows(run, 'professional-arrivals') if r['outcome'] == 'commission-refused:no-plan-minimum-delivery']
    diagnostics = list(rows(run, 'commission-plan-diagnostic'))
    if len(arrivals) != len(diagnostics):
        raise ValueError(f'{run}: inconsistent arrival trace')
    eligible = defaultdict(list)
    for r in rows(run, 'commission-eligible-plans'):
        eligible[(r['labelId'], r['artistId'])].append(r)
    snapshots = {}
    for r in rows(run, 'commission-reminder-gates'):
        key = (r['week'], r['labelId'], r['artistId'], r['slotId'])
        if key in snapshots:
            raise ValueError(f'{run}: duplicate snapshot {key}')
        snapshots[key] = r
    pairs = {}
    event_counts = Counter()
    mature_acts, eligible_acts = set(), set()
    for arrival, diag in zip(arrivals, diagnostics):
        if any(arrival[k] != diag[k] for k in ('week', 'labelId', 'artistId')):
            raise ValueError(f'{run}: inconsistent arrival ordering')
        week = int(arrival['week'])
        if arrival['provenance'] != 'arrival' or not 1 <= week <= weeks - outcome:
            continue
        event_counts['matureEvents'] += 1
        mature_acts.add((arrival['labelId'], arrival['artistId']))
        options = [r for r in eligible[(arrival['labelId'], arrival['artistId'])] if week < int(r['week']) <= week + appearance]
        if not options:
            continue
        event_counts['eventsWithEligible'] += 1
        eligible_acts.add((arrival['labelId'], arrival['artistId']))
        first = min(options, key=lambda r: (int(r['week']), r['slotId']))
        snapkey = (first['week'], first['labelId'], first['artistId'], first['slotId'])
        if snapkey not in snapshots:
            raise ValueError(f'{run}: missing gate snapshot {snapkey}')
        pair = (first['labelId'], first['artistId'], first['slotId'])
        if pair not in pairs:
            pairs[pair] = {'arrival': week, 'reason': diag['primary'], 'snapshot': snapshots[snapkey], 'events': 0}
        pairs[pair]['events'] += 1
        if (int(first['week']), week) < (int(pairs[pair]['snapshot']['week']), pairs[pair]['arrival']):
            pairs[pair].update(arrival=week, reason=diag['primary'], snapshot=snapshots[snapkey])
    slate, reviews, searches = defaultdict(list), defaultdict(list), defaultdict(list)
    for r in rows(run, 'release-slate'):
        slate[r['slotId']].append(r)
    for r in rows(run, 'material-funnel'):
        reviews[r['slotId']].append(r)
    for r in rows(run, 'programme-material-search'):
        searches[r['slotId']].append(r)
    counts, reasons, overlaps = Counter(), defaultdict(Counter), Counter()
    for (_, _, slot), pair in pairs.items():
        row = pair['snapshot']
        snap, end = int(row['week']), pair['arrival'] + outcome
        category = gate(row)
        for c in (counts, reasons[pair['reason']]):
            c['pairs'] += 1
            c['eventsMapped'] += pair['events']
            c[category] += 1
            c['selectedPlanIsPair'] += bool(row['selectedPlanId'] and row['selectedPlanId'] == slot)
            c['selectedOtherPlan'] += bool(row['selectedPlanId'] and row['selectedPlanId'] != slot)
            c['passesAndSamePlan'] += category == 'passes-observed-gates' and row['selectedPlanId'] == slot
            c['passesAndOtherPlan'] += category == 'passes-observed-gates' and row['selectedPlanId'] != slot
            active = int(row['activeCandidates'])
            c['candidateCountUnknown'] += active < 0
            c['candidateCountUnderTwo'] += 0 <= active < 2
            c['passesGatesAndCandidateCount'] += category == 'passes-observed-gates' and 0 <= active < 2
            c['reviewedBySnapshot'] += any(int(r['week']) <= snap for r in reviews[slot])
            c['searchStartedBySnapshot'] += any(int(r['week']) <= snap and r['state'] == 'Pending' for r in searches[slot])
            c['issuedByWindow'] += any(snap <= int(r['week']) <= end and r['state'] == 'Issued' for r in slate[slot])
        for k in ('owned', 'pending', 'recent'):
            overlaps[k] += int(row[k])
        overlaps['marketWeekMismatch'] += int(int(row['marketWeek']) != snap)
    event_counts['distinctMatureLabelArtists'] = len(mature_acts)
    event_counts['distinctEligibleLabelArtists'] = len(eligible_acts)
    return {'run': run, 'events': dict(event_counts), 'distinctArtistPlanPairs': dict(counts),
            'byFirstRefusalReason': {k: dict(v) for k, v in sorted(reasons.items())},
            'overlappingFlags': dict(overlaps)}


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
