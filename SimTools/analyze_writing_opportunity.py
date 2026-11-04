"""Compare mature single intentions and material flows in writing-workflow runs.

This is an observational audit of proposed plans, not a census of eligible acts.
Each plan is followed for 26 weeks from proposal. Later events are excluded.
"""
import argparse
import json
from collections import Counter, defaultdict
from pathlib import Path

from analyze_writing_concentration import LOGS, chart_data, iter_rows, material_flow


def plan_funnel(rows, launches, searches, end, window=26):
    plans = {}
    for r in rows:
        if r['format'] != 'Single':
            continue
        proposed = int(r['proposedWeek'])
        if proposed < 1 or proposed + window > end or int(r['week']) > proposed + window:
            continue
        p = plans.setdefault(r['slotId'], dict(artist=r['artistId'], proposed=proposed,
            career='Unknown', prepared=False, issued=False, cancelled=False, reasons=set(), record=''))
        for part in r['evidence'].split(';'):
            if part.startswith('career='):
                p['career'] = part.split('=', 1)[1]
        p['prepared'] |= bool(r['preparedWeek']) and int(r['preparedWeek']) <= proposed + window
        if r['state'] == 'Issued' and r['actualWeek'] and int(r['actualWeek']) <= proposed + window:
            p['issued'], p['record'] = True, r['recordId']
        p['cancelled'] |= r['state'] == 'Cancelled'
        if r['state'] in ('Cancelled', 'Postponed'):
            p['reasons'].add(r['reason'])
    for r in searches:
        p = plans.get(r['slotId'])
        if p and int(r['week']) <= p['proposed'] + window:
            p['searched'] = True
    sources = {r['recordId']: r for r in launches}
    groups = defaultdict(list)
    for p in plans.values():
        groups['all'].append(p)
        groups[p['career']].append(p)
    result = []
    for career, group in sorted(groups.items()):
        issued = [p for p in group if p['issued']]
        missing = sum(p['record'] not in sources for p in issued)
        result.append(dict(career=career, plans=len(group), acts=len({p['artist'] for p in group}),
            searched=sum(p.get('searched', False) for p in group), prepared=sum(p['prepared'] for p in group),
            issued=len(issued), cancelled=sum(p['cancelled'] for p in group), missingLaunchProvenance=missing,
            issueSources=dict(Counter(sources[p['record']]['source'] for p in issued if p['record'] in sources)),
            delayedOrCancelledReasons=dict(Counter(reason for p in group for reason in p['reasons']))))
    return result


def choice_funnel(rows):
    """Count unique acts and candidate-slot observations, not independent people per row."""
    groups = defaultdict(lambda: dict(eligible=set(), selected=set(), observations=0, selectedObservations=0))
    for r in rows:
        if r['format'] != 'Single' or r['eligible'] != '1':
            continue
        for career in ('all', r['careerState']):
            g = groups[career]
            g['eligible'].add(r['artistId'])
            g['observations'] += 1
            if r['selected'] == '1':
                g['selected'].add(r['artistId'])
                g['selectedObservations'] += 1
    return [dict(career=k, eligibleActs=len(v['eligible']), selectedActs=len(v['selected']),
                 eligibleCandidateSlotObservations=v['observations'], selectedObservations=v['selectedObservations'])
            for k, v in sorted(groups.items())]


def demo_inventory(rows):
    weeks = defaultdict(lambda: dict(available=0, priority=0, hook=0.0))
    for r in rows:
        week = int(r['week'])
        counts = weeks[week]
        if r['status'] == 'Available' and int(r['availableWeek']) <= week:
            counts['available'] += 1
            counts['priority'] += int(int(r['priorityUntilWeek']) > week)
            counts['hook'] += float(r['compositionHook'])
    if not weeks:
        raise ValueError('Empty demo inventory trace')
    ordered = sorted(weeks)
    return dict(observedWeeks=len(ordered), firstWeek=ordered[0], lastWeek=ordered[-1],
                meanAvailable=sum(x['available'] for x in weeks.values()) / len(weeks),
                meanPriority=sum(x['priority'] for x in weeks.values()) / len(weeks),
                meanAvailableHook=(sum(x['hook'] for x in weeks.values()) /
                                   sum(x['available'] for x in weeks.values())) if any(x['available'] for x in weeks.values()) else None,
                endingAvailable=weeks[ordered[-1]]['available'])


def summarize(run):
    _, end = chart_data(run)
    if end != 104:
        raise ValueError(f'{run}: expected complete 104-week window, got {end}')
    for suffix in ('writing-launches', 'release-slate', 'programme-material-search', 'writing-orders', 'writing-demos'):
        if not (LOGS / f'{run}-{suffix}.csv').exists():
            raise ValueError(f'{run}: missing required {suffix} audit')
    launches = list(iter_rows(run, 'writing-launches'))
    plans = plan_funnel(iter_rows(run, 'release-slate'), launches,
                        iter_rows(run, 'programme-material-search'), end)
    if any(p['missingLaunchProvenance'] for p in plans):
        raise ValueError(f'{run}: issued plan missing launch provenance')
    material = material_flow(run, end, set(), launches)
    inventory = demo_inventory(iter_rows(run, 'writing-demos'))
    if inventory['firstWeek'] != 0 or inventory['lastWeek'] != end or inventory['observedWeeks'] != end + 1:
        raise ValueError(f'{run}: incomplete weekly inventory trace')
    choices = LOGS / f'{run}-programme-choices.csv'
    return dict(run=run, weeks=end, plans=plans,
                choices=choice_funnel(iter_rows(run, 'programme-choices')) if choices.exists() else None,
                demoInventory=inventory,
                completedHolds=material['completedHolds'],
                matureOrders=next(x for x in material['commissions'] if x['definition'] == 'all'))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('runs', nargs='+')
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    manifests = [json.loads((LOGS / 'writing-calibration' / f'{r}-manifest.json').read_text(encoding='utf-8-sig')) for r in args.runs]
    if len({m['assemblySha256'] for m in manifests}) != 1:
        raise ValueError('Runs must use the same frozen assembly')
    args.output.write_text(json.dumps([summarize(r) for r in args.runs], indent=2) + '\n', encoding='utf-8')
