"""Decompose writing-market chart concentration and fully observed launch conversion.

Leaders are retrospective within each run, not a causal or prospective grouping.
Only launches with a complete 13-week outcome window enter conversion tables.
"""
import argparse
import csv
import json
import math
from collections import Counter, defaultdict
from pathlib import Path

LOGS = Path(__file__).resolve().parents[1] / 'SimLogs'


def read(run, suffix):
    path = LOGS / f'{run}-{suffix}.csv'
    if not path.exists():
        return []
    with path.open(encoding='utf-8-sig', newline='') as f:
        return list(csv.DictReader(f))


def iter_rows(run, suffix):
    path = LOGS / f'{run}-{suffix}.csv'
    if path.exists():
        with path.open(encoding='utf-8-sig', newline='') as f:
            yield from csv.DictReader(f)


def mean(values):
    values = list(values)
    return sum(values) / len(values) if values else None


def chart_data(run):
    chart = [r for r in read(run, 'records') if int(r['week']) > 0 and 1 <= int(r['currentPosition']) <= 100]
    if not chart:
        raise ValueError(f'{run}: missing chart observations')
    end = max(int(r['week']) for r in chart)
    if len({(r['week'], r['recordId']) for r in chart}) != len(chart):
        raise ValueError('Duplicate chart observations')
    return chart, end


def summarize(run, fixed_n=None):
    chart, end = chart_data(run)
    all_observed = read(run, 'records')
    first100, first40 = {}, {}
    for r in sorted(all_observed, key=lambda r: int(r['week'])):
        pos = int(r['currentPosition'])
        if 1 <= pos <= 100:
            first100.setdefault(r['recordId'], int(r['week']))
        if 1 <= pos <= 40:
            first40.setdefault(r['recordId'], int(r['week']))
    slots = Counter(r['artistId'] for r in chart)
    ranked = sorted(slots, key=lambda a: (-slots[a], a))
    n = max(1, math.ceil(len(ranked) * .1))
    leader_ids = set(ranked[:n])
    leader_rows = [r for r in chart if r['artistId'] in leader_ids]
    titles = {r['recordId'] for r in chart}
    leader_titles = {r['recordId'] for r in leader_rows}
    title_share = len(leader_titles) / len(titles)
    duration_ratio = (len(leader_rows) / len(leader_titles)) / (len(chart) / len(titles))
    slot_share = len(leader_rows) / len(chart)
    assert abs(slot_share - title_share * duration_ratio) < 1e-12
    simultaneous = Counter((r['week'], r['artistId']) for r in chart)
    launches = [r for r in read(run, 'single-launch-quality') if 1 <= int(r['week']) <= end - 13]
    source_rows = read(run, 'writing-launches')
    source = {r['recordId']: r for r in source_rows}
    if len(source) != len(source_rows):
        raise ValueError(f'{run}: duplicate material launch provenance')
    groups = defaultdict(list)
    for r in launches:
        row = dict(r)
        w = int(r['week'])
        row['hit100'] = w <= first100.get(r['singleId'], -1) <= w + 13
        row['hit40'] = w <= first40.get(r['singleId'], -1) <= w + 13
        row['source'] = source.get(r['singleId'], {}).get('source', 'Unobserved')
        row['route'] = source.get(r['singleId'], {}).get('route', 'Unobserved')
        row['compositionHook'] = source.get(r['singleId'], {}).get('compositionHook')
        for definition, key in [('leaders', 'leaders' if r['artistId'] in leader_ids else 'others'),
                                ('tier', r['tier']), ('careerAtLaunch', r['careerState']),
                                ('source', row['source']), ('route', row['route']),
                                ('careerSource', r['careerState'] + '|' + row['source']),
                                ('leaderSource', ('leaders' if r['artistId'] in leader_ids else 'others') + '|' + row['source'])]:
            groups[definition, key].append(row)
    conversions = []
    for (definition, key), group in sorted(groups.items()):
        conversions.append(dict(definition=definition, group=key, releases=len(group),
            acts=len({r['artistId'] for r in group}),
            top100Within13=mean(r['hit100'] for r in group), top40Within13=mean(r['hit40'] for r in group),
            meanHook=mean(float(r['hook']) for r in group),
            meanCompositionHook=mean(float(r['compositionHook']) for r in group if r['compositionHook'] is not None),
            meanProduction=mean(float(r['production']) for r in group),
            meanRuntimeQuality=mean(float(r['runtimeQuality']) for r in group)))
    act_launches = defaultdict(list)
    for r in launches:
        act_launches[r['artistId']].append(r)
    acts = [dict(artistId=a, slots=slots[a], titles=len({r['recordId'] for r in chart if r['artistId'] == a}),
                 matureLaunches=len(act_launches[a]), leader=a in leader_ids) for a in ranked]
    fixed_n = fixed_n or n
    return dict(run=run, weeks=end, chartActs=len(ranked), leaderActs=n,
        slotShare=slot_share, fixedLeaderN=fixed_n,
        fixedLeaderSlotShare=sum(slots[a] for a in ranked[:fixed_n]) / len(chart),
        leaderTitleShare=title_share, relativeTitleDuration=duration_ratio,
        leaderObservedWeeksPerTitle=len(leader_rows) / len(leader_titles),
        othersObservedWeeksPerTitle=(len(chart)-len(leader_rows))/(len(titles)-len(leader_titles)),
        leaderMultipleTitleSlotShare=mean(simultaneous[r['week'], r['artistId']] > 1 for r in leader_rows),
        allMultipleTitleSlotShare=mean(simultaneous[r['week'], r['artistId']] > 1 for r in chart),
        missingMaterialProvenance=sum(r['singleId'] not in source for r in launches),
        conversions=conversions, material=material_flow(run, end, leader_ids, source_rows), acts=acts)


def material_flow(run, end, leaders, launches):
    orders, cancelled_at = {}, {}
    for row in iter_rows(run, 'writing-orders'):
        orders[row['id']] = row
        if row['status'] == 'Cancelled':
            cancelled_at.setdefault(row['id'], int(row['week']))
    latest, first, shortlisted, episodes, by26 = {}, {}, set(), {}, {}
    for row in iter_rows(run, 'writing-demos'):
        song = row['songId']
        latest[song] = row
        first.setdefault(song, row)
        if any(int(row[k]) for k in ('shortlistActive','shortlistRecorded','shortlistArchived')):
            shortlisted.add((song, row['artistId']))
        order = orders.get(row['orderId'])
        if order and row['artistId'] == order['artistId'] and row['labelId'] == order['labelId'] and int(row['week']) <= int(order['deliveryWeek']) + 26:
            observed = by26.setdefault(order['id'], dict(prepared=False, released=False, shortlisted=False, retained=False))
            observed['prepared'] |= row['status'] == 'Recorded'
            observed['released'] |= row['released'].lower() == 'true'
            observed['shortlisted'] |= any(int(row[k]) for k in ('shortlistActive','shortlistRecorded','shortlistArchived'))
            observed['retained'] |= int(row['shortlistActive']) > 0 or int(row['shortlistRecorded']) > 0
        if row['status'] not in ('Licensed', 'Recorded') or not row['artistId'] or int(row['holdUntilWeek']) <= 0:
            continue
        key = (song, row['labelId'], row['artistId'], row['holdUntilWeek'])
        episode = episodes.setdefault(key, dict(songId=song, artistId=row['artistId'], start=int(row['holdUntilWeek'])-12,
            end=int(row['holdUntilWeek']), leftCensored=int(row['week']) == 0, orderId=row['orderId'],
            preparedByExpiry=False, releasedByExpiry=False, releasedObserved=False))
        if int(row['week']) <= episode['end']:
            episode['preparedByExpiry'] |= row['status'] == 'Recorded'
            episode['releasedByExpiry'] |= row['released'].lower() == 'true'
        episode['releasedObserved'] |= row['released'].lower() == 'true'
    release_ids = defaultdict(set)
    release_any_label = defaultdict(set)
    release_weeks = defaultdict(list)
    launch_by_id = {r['recordId']:r for r in launches}
    for r in launches:
        release_ids[r['songId'], r['artistId'], r['labelId']].add(r['recordId'])
        release_any_label[r['songId'], r['artistId']].add(r['recordId'])
        release_weeks[r['songId'], r['artistId'], r['labelId']].append(int(r['week']))
    plans, early_work = {}, set()
    linked_orders = defaultdict(list)
    for o in orders.values():
        if o.get('releasePlanId'):
            linked_orders[o['releasePlanId']].append(o)
    for r in iter_rows(run, 'release-slate'):
        plans[r['slotId']] = r
        if r['state'] not in ('Ready','Scheduled','Issued') or not r.get('preparedWeek'):
            continue
        for o in linked_orders[r['slotId']]:
            if int(r['week']) >= int(o['startedWeek']) and int(r['preparedWeek']) < int(o['deliveryWeek']) and int(r['preparedWeek']) < cancelled_at.get(o['id'], float('inf')):
                early_work.add(o['id'])
    albums = read(run, 'writing-album-tracks')
    album_songs = {(r['songId'],r['artistId']) for r in albums if r['terminalState'] == 'Released'}
    destinations = Counter()
    for order in orders.values():
        if order['status'] != 'Delivered':
            continue
        key = (order['songId'], order['artistId'])
        in_album, in_single = key in album_songs, bool(release_any_label[key])
        destinations['both' if in_album and in_single else 'albumOnly' if in_album else 'singleOnly' if in_single else 'neitherObserved'] += 1
    groups = defaultdict(list)
    for order in orders.values():
        if order['status'] != 'Delivered' or int(order['deliveryWeek']) + 26 > end:
            continue
        song = order['songId']
        last = latest.get(song, {})
        original_client = last.get('artistId') == order['artistId'] and last.get('labelId') == order['labelId']
        due = int(order['deliveryWeek']) + 26
        plan = plans.get(order.get('releasePlanId'), {})
        issued_plan = plan.get('state') == 'Issued' and int(order['deliveryWeek']) <= int(plan.get('actualWeek',-1)) <= due
        planned_master = launch_by_id.get(plan.get('recordId'), {})
        detail = dict(orderId=order['id'], artistId=order['artistId'],
            prepared=original_client and last.get('status') == 'Recorded',
            releasedAnyObserved=original_client and last.get('released','').lower() == 'true',
            singleReleased=bool(release_ids[song, order['artistId'], order['labelId']]),
            singleReleasedWithin26=any(int(order['deliveryWeek']) <= w <= due for w in release_weeks[song, order['artistId'], order['labelId']]),
            planLinked=bool(order.get('releasePlanId')), linkedPlanIssued26=issued_plan,
            linkedPlanUsed26=issued_plan and planned_master.get('songId') == song and planned_master.get('artistId') == order['artistId'],
            singleAnyLabel=bool(release_any_label[song, order['artistId']]),
            albumReleased=(song,order['artistId']) in album_songs,
            releasedOriginalAct=last.get('artistId') == order['artistId'] and last.get('released','').lower() == 'true',
            releasedOtherAct=bool(last.get('artistId')) and last.get('artistId') != order['artistId'] and last.get('released','').lower() == 'true',
            releasedAnyAct=last.get('released','').lower() == 'true',
            shortlistedObserved=(song, order['artistId']) in shortlisted,
            preparedWithin26=by26.get(order['id'], {}).get('prepared', False),
            releasedWithin26=by26.get(order['id'], {}).get('released', False),
            shortlistedWithin26=by26.get(order['id'], {}).get('shortlisted', False),
            retainedWithin26=by26.get(order['id'], {}).get('retained', False),
            hook=float(first.get(song, {}).get('compositionHook', 0)))
        for definition, key in [('all', 'all'), ('leaders', 'leaders' if order['artistId'] in leaders else 'others'),
                                ('careerAtDeliverySnapshot', first.get(song, {}).get('careerState','Unknown'))]:
            groups[definition, key].append(detail)
    commissions = [dict(definition=d, group=k, mature26WeekDeliveries=len(g),
        prepared=sum(r['prepared'] for r in g), releasedAnyObserved=sum(r['releasedAnyObserved'] for r in g),
        singleReleased=sum(r['singleReleased'] for r in g), shortlistedObserved=sum(r['shortlistedObserved'] for r in g),
        singleReleasedWithin26=sum(r['singleReleasedWithin26'] for r in g),
        planLinked=sum(r['planLinked'] for r in g), linkedPlanIssued26=sum(r['linkedPlanIssued26'] for r in g),
        linkedPlanUsed26=sum(r['linkedPlanUsed26'] for r in g),
        preparedWithin26=sum(r['preparedWithin26'] for r in g), releasedWithin26=sum(r['releasedWithin26'] for r in g),
        shortlistedWithin26=sum(r['shortlistedWithin26'] for r in g),
        retainedWithin26=sum(r['retainedWithin26'] for r in g),
        releasedWithoutObservedSingle=sum(r['releasedAnyObserved'] and not r['singleAnyLabel'] for r in g),
        releasedOriginalAct=sum(r['releasedOriginalAct'] for r in g), releasedOtherAct=sum(r['releasedOtherAct'] for r in g),
        releasedAnyAct=sum(r['releasedAnyAct'] for r in g),
        albumReleased=sum(r['albumReleased'] for r in g) if (LOGS / f'{run}-writing-album-tracks.csv').exists() else None,
        preparedButUnreleased=sum(r['prepared'] and not r['releasedAnyObserved'] for r in g),
        meanDeliveredHook=mean(r['hook'] for r in g)) for (d,k),g in sorted(groups.items())]
    holds = defaultdict(list)
    for e in episodes.values():
        if e['leftCensored'] or e['end'] + 1 > end:
            continue
        order = orders.get(e['orderId'])
        route = 'ExistingDemo' if not order else 'OriginalCommission' if e['artistId'] == order['artistId'] and e['start'] == int(order['deliveryWeek']) else 'RecycledCommissionLicense'
        holds[route].append(e)
    return dict(hasDemoTrace=(LOGS / f'{run}-writing-demos.csv').exists(),
        hasAlbumTrace=(LOGS / f'{run}-writing-album-tracks.csv').exists(),
        orders=len(orders), linkedPlanOrders=sum(bool(o.get('releasePlanId')) for o in orders.values()),
        waitingPlansPreparedEarly=len(early_work),
        offeredDeliveries=sum(o.get('deliveryOffered','').lower() == 'true' for o in orders.values()) if any('deliveryOffered' in o for o in orders.values()) else None,
        deliveredDestinations=dict(destinations) if (LOGS / f'{run}-writing-album-tracks.csv').exists() else None,
        commissions=commissions,
        completedHolds=[dict(route=k, holds=len(g), preparedByExpiry=sum(e['preparedByExpiry'] for e in g),
            releasedByExpiry=sum(e['releasedByExpiry'] for e in g), releasedObserved=sum(e['releasedObserved'] for e in g)) for k,g in sorted(holds.items())])


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('runs', nargs='+')
    parser.add_argument('--fixed-leaders', type=int)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    args.output.write_text(json.dumps([summarize(r, args.fixed_leaders) for r in args.runs], indent=2) + '\n', encoding='utf-8')
