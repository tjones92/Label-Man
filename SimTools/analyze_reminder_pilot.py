"""Validate and compare matched opt-in plan-reminder pilots with controls."""
import argparse
import json
from collections import Counter, defaultdict
from pathlib import Path
from analyze_material_funnel import ROOT, rows
from analyze_writing_market import latest
from analyze_writing_workflow import summarize as workflow


def manifests(runs, weeks):
    found = {}
    for run in runs:
        m = json.loads((ROOT / 'writing-calibration' / f'{run}-manifest.json').read_text(encoding='utf-8-sig'))
        flags = set(m['flags'])
        pilot = '--writing-market-plan-reminders' in flags
        seed = next(x for x in flags if x.startswith('--seed='))
        common = frozenset(x for x in flags if not x.startswith('--run=') and x != '--writing-market-plan-reminders')
        key = (seed, pilot)
        if key in found or f'--weeks={weeks}' not in flags:
            raise ValueError(f'{run}: duplicate arm/seed or wrong duration')
        found[key] = (run, m, common)
    if len({m['assemblySha256'] for _, m, _ in found.values()}) != 1:
        raise ValueError('Mixed assembly hashes')
    for seed, pilot in found:
        if (seed, not pilot) not in found or found[seed, pilot][2] != found[seed, not pilot][2]:
            raise ValueError(f'{seed}: missing or mismatched control/treatment flags')
    return found


def reminder_outcomes(run, weeks):
    events = list(rows(run, 'plan-reminder-events'))
    groups = defaultdict(list)
    for e in events:
        key = (e['labelId'], e['artistId'], e['contractSequence'], e['createdWeek'])
        groups[key].append(e)
    counts, reasons = Counter(), Counter()
    attempts, ordered = [], []
    for key, group in groups.items():
        created = [e for e in group if e['action'] == 'created']
        terminal = [e for e in group if e['action'] in ('attempt', 'expired', 'discarded')]
        if len(created) != 1 or len(terminal) > 1 or int(created[0]['week']) != int(key[3]):
            raise ValueError(f'{run}: invalid reminder lifecycle {key}')
        counts['created'] += 1
        if not terminal:
            if int(key[3]) + 9 <= weeks:
                raise ValueError(f'{run}: reminder remained open past its expiry {key}')
            counts['openAtEnd'] += 1
            continue
        e = terminal[0]
        age = int(e['week']) - int(key[3])
        if e['action'] in ('attempt', 'discarded') and not 1 <= age <= 8 or e['action'] == 'expired' and age != 9:
            raise ValueError(f'{run}: reminder outside time window {key}')
        counts[e['action']] += 1
        if e['action'] == 'attempt':
            attempts.append(e)
            reasons[e['reason']] += 1
            if (e['reason'] == 'ordered') != bool(e['orderId']):
                raise ValueError(f'{run}: inconsistent order event')
            if e['orderId']:
                ordered.append(e)
    if len({e['slotId'] for e in attempts}) != len(attempts) or len({e['orderId'] for e in ordered}) != len(ordered):
        raise ValueError(f'{run}: duplicate plan attempt or order')
    orders_history = list(rows(run, 'writing-orders'))
    orders = {r['id']: r for r in latest(orders_history, 'id')}
    by_order = defaultdict(list)
    for r in orders_history:
        by_order[r['id']].append(r)
    slate = defaultdict(list)
    for r in rows(run, 'release-slate'):
        slate[r['slotId']].append(r)
    reviews = defaultdict(list)
    for r in rows(run, 'material-funnel'):
        reviews[r['slotId']].append(r)
    launches = {r['recordId']: r for r in rows(run, 'writing-launches')}
    chart = defaultdict(list)
    for r in rows(run, 'records'):
        chart[r['recordId']].append(r)
    for e in attempts:
        if not e['slotId'] or any(int(r['week']) < int(e['week']) for r in reviews[e['slotId']]):
            raise ValueError(f"{run}: attempted after material review for {e['slotId']}")
    for e in ordered:
        o = orders.get(e['orderId'])
        if not o or o['releasePlanId'] != e['slotId'] or o['artistId'] != e['artistId'] or o['labelId'] != e['labelId'] or int(o['startedWeek']) != int(e['week']):
            raise ValueError(f"{run}: order/plan mismatch for {e['orderId']}")
        cancelled = min((int(r['week']) for r in by_order[e['orderId']] if r['status'] == 'Cancelled'), default=weeks + 1)
        pending_end = min(int(o['deliveryWeek']), cancelled)
        if any(int(o['startedWeek']) <= int(r['week']) < pending_end for r in reviews[e['slotId']]):
            raise ValueError(f"{run}: competing material review while reminder order pending for {e['slotId']}")
        counts['commissionFees'] += float(o['fee'])
        if o['status'] == 'Delivered':
            counts['delivered'] += 1
        if o['status'] != 'Delivered' or int(o['deliveryWeek']) + 26 > weeks:
            continue
        counts['matureDeliveries'] += 1
        lo, hi = int(o['deliveryWeek']), int(o['deliveryWeek']) + 26
        issued = next((r for r in slate[e['slotId']] if r['state'] == 'Issued' and lo <= int(r['actualWeek']) <= hi), None)
        if issued:
            counts['linkedPlanIssued26'] += 1
            launch = launches.get(issued['recordId'], {})
            used = launch.get('songId') == o['songId']
            counts['linkedPlanUsedSong26'] += used
            issue_week = int(issued['actualWeek'])
            if used and issue_week + 12 <= weeks:
                counts['linkedSongComplete13WeekCharts'] += 1
                positions = [int(r['currentPosition']) for r in chart[issued['recordId']]
                             if issue_week <= int(r['week']) <= issue_week + 12 and 1 <= int(r['currentPosition']) <= 100]
                counts['linkedSongTop40Within13'] += any(p <= 40 for p in positions)
                counts['linkedSongTop10Within13'] += any(p <= 10 for p in positions)
    return {'lifecycle': dict(counts), 'attemptReasons': dict(reasons)}


def main():
    p = argparse.ArgumentParser()
    p.add_argument('runs', nargs='+')
    p.add_argument('--weeks', type=int, default=104)
    p.add_argument('--output', type=Path, required=True)
    args = p.parse_args()
    matched = manifests(args.runs, args.weeks)
    results = []
    for (_, pilot), (run, _, _) in sorted(matched.items()):
        summary = workflow(run, args.weeks)
        summary['planReminders'] = pilot
        launches = list(rows(run, 'writing-launches'))
        summary['singleLaunchSourceCounts'] = dict(Counter(r['source'] for r in launches if 1 <= int(r['week']) <= args.weeks))
        summary['final52WeekSingleLaunchSources'] = dict(Counter(r['source'] for r in launches if max(1, args.weeks - 51) <= int(r['week']) <= args.weeks))
        if pilot:
            summary['reminders'] = reminder_outcomes(run, args.weeks)
        results.append(summary)
    args.output.write_text(json.dumps(results, indent=2) + '\n', encoding='utf-8')


if __name__ == '__main__':
    main()
