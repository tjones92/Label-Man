"""Comparable observer contract; frozen observations, not demand predictions.

Pass records.csv, station-playlists.csv and a JSON object mapping Los Angeles to
two station IDs and Minneapolis to one. Reuse that manifest across paired runs.
First dates use all history; outcome peaks use an equal post-launch horizon.
"""
import argparse
import json
from pathlib import Path
import pandas as pd

CITIES = ('Los Angeles', 'Minneapolis')


def first_events(playlists):
    x = playlists.sort_values('week').drop_duplicates(['stationId', 'recordId', 'week']).copy()
    groups = x.groupby(['stationId', 'recordId'])
    x['nextWeek'] = groups.week.shift(-1)
    first = x.groupby('recordId').week.min()
    # Two consecutive weeks at the SAME station; backdate only the start metric.
    sustained = x[x.nextWeek.eq(x.week + 1)].groupby('recordId').week.min()
    return first, sustained


def decision_events(path):
    """Explicit trial, ordinary rotation, sales and exposure evidence per station.

    Listing confirmation is not graduation: two weeks of a protected trial do
    not qualify as sustained ordinary rotation here.
    """
    events, ordinary = {}, {}
    with path.open() as stream:
        for line in stream:
            capture = json.loads(line)
            m = capture['Input']
            week, station = m['Week'], m['StationId']
            seats = {s['Id']: s for s in capture['Result']['Seats']}
            for c in m['Candidates']:
                key = (station, c['Id'])
                seat = seats.get(c['Id'])
                rank = c['LocalRank']
                exposure = c['ExposureAge']
                if not seat and not 1 <= rank <= 40 and exposure in (0, 2147483647):
                    ordinary.pop(key, None)
                    continue
                e = events.setdefault(key, dict(stationId=station, recordId=c['Id'], city=m['City'], regionId=m['RegionId']))
                if seat:
                    e.setdefault('firstAppearance', week)
                    if seat['Source'] == 'trial':
                        e.setdefault('firstTrial', week)
                    else:
                        previous, count = ordinary.get(key, (-1, 0))
                        count = count + 1 if previous == week - 1 else 1
                        ordinary[key] = (week, count)
                        if count >= 2:
                            e.setdefault('sustainedOrdinaryStart', week - 1)
                            e.setdefault('sustainedOrdinaryConfirmation', week)
                    if seat['Source'] == 'graduated':
                        e.setdefault('firstGraduation', week)
                if not seat or seat['Source'] == 'trial':
                    ordinary.pop(key, None)
                if 1 <= rank <= 40 and m['RankWeek'] > 0:
                    e.setdefault('firstLocalSalesBreak', m['RankWeek'])
                e['launchExposed'] = c['LaunchExposed']
                if exposure not in (0, 2147483647):
                    # Schedule may accelerate; preserve the final (earliest) scheduled arrival.
                    e['exposureWeek'] = week - c['ReleaseAge'] + exposure
                    e['exposureCause'] = c['ExposureCause']
    return pd.DataFrame(events.values())


def score(records, playlists, panel, start=5, end=38, horizon=12):
    if set(panel) != set(CITIES) or len(panel[CITIES[0]]) != 2 or len(panel[CITIES[1]]) != 1:
        raise ValueError('Panel must name exactly two Los Angeles and one Minneapolis station')
    ids = [sid for city in CITIES for sid in panel[city]]
    if len(set(ids)) != 3:
        raise ValueError('Panel station IDs must be distinct')
    for city, stations in panel.items():
        for sid in stations:
            observed = playlists[playlists.stationId.eq(sid)]
            if observed.empty or not observed.city.eq(city).all():
                raise ValueError(f'Missing or wrong-city panel station: {sid}')
    r = records.copy()
    r['launch'] = r.week - r.weeksSinceRelease + 1
    meta = r.sort_values('week').drop_duplicates('recordId').set_index('recordId')
    # Require run coverage, not record survival. Retired records disappear from
    # records.csv; requiring their own final row at the horizon selects survivors.
    meta = meta[meta.launch.between(start, end) & (meta.launch + horizon - 1).le(r.week.max())]
    h = r[r.weeksSinceRelease.between(1, horizon) & r.currentPosition.between(1, 100)]
    outcomes = h.groupby('recordId').agg(debut=('week', 'min'), peak=('currentPosition', 'min'))
    meta = meta[['launch', 'labelId']].join(outcomes)
    meta['big5'] = meta.labelId.isin([f'label_000{i}' for i in range(1, 6)])
    meta['opportunity'] = meta.debut - meta.launch >= 2
    rows, events, listings = [], [], []
    city_lists = playlists[playlists.city.isin(CITIES) & playlists['format'].isin(['Top40', 'FullService'])]
    for name, x in [('full-world', city_lists), ('fixed-2+1', city_lists[city_lists.stationId.isin(ids)])]:
        first, sustained = first_events(x)
        v = meta.join(first.rename('firstAudition')).join(sustained.rename('sustainedStart'))
        v['sustainedConfirmation'] = v.sustainedStart + 1
        v['panel'] = name
        events.append(v.reset_index())
        masks = {'indie10': v.peak.le(10) & ~v.big5, 'big5_10': v.peak.le(10) & v.big5,
                 '11-40': v.peak.between(11, 40), '41-100': v.peak.between(41, 100),
                 'all-debuts': v.peak.notna()}
        for cohort, mask in masks.items():
            for event in ['firstAudition', 'sustainedStart', 'sustainedConfirmation']:
                d = v[mask]
                leads = (d.debut - d[event]).ge(2)
                opportunity = d.opportunity
                rows.append(dict(panel=name, cohort=cohort, event=event, n=len(d),
                                 eligible=int(opportunity.sum()), lead2=int(leads.sum()),
                                 leadPct=100 * leads.mean() if len(d) else None,
                                 opportunityAdjustedPct=100 * leads[opportunity].mean() if opportunity.any() else None))
        for city in CITIES:
            # Find first ever, THEN restrict dates. Carryovers cannot become new listings.
            first_city = x[x.city.eq(city)].groupby('recordId').week.min()
            fresh = first_city[first_city.between(start, end)]
            matched = fresh.index.intersection(meta.index)
            listings.append(dict(panel=name, city=city, firstListings=len(fresh),
                                 perWeek=len(fresh)/(end-start+1), matchedFollowup=len(matched),
                                 unchartedWithinHorizon=int(meta.loc[matched].peak.isna().sum())))
    return pd.DataFrame(rows), pd.concat(events, ignore_index=True), pd.DataFrame(listings)


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--records', required=True, type=Path)
    p.add_argument('--playlists', required=True, type=Path)
    p.add_argument('--panel', required=True, type=Path)
    p.add_argument('--out', required=True, type=Path)
    p.add_argument('--singles', required=True, type=Path, help='single-launch-quality CSV (singleId)')
    p.add_argument('--decisions', type=Path, help='Optional station-decisions JSONL for explicit decision events')
    p.add_argument('--start', type=int, default=5)
    p.add_argument('--end', type=int, default=38)
    p.add_argument('--horizon', type=int, default=12)
    args = p.parse_args()
    if args.horizon < 1 or args.end < args.start:
        p.error('Require positive horizon and end >= start')
    records = pd.read_csv(args.records)
    singles = set(pd.read_csv(args.singles).singleId)
    records = records[records.recordId.isin(singles)]
    panel = json.loads(args.panel.read_text())
    outputs = score(records, pd.read_csv(args.playlists), panel, args.start, args.end, args.horizon)
    args.out.mkdir(parents=True, exist_ok=True)
    for name, data in zip(['metrics', 'events', 'first-listings'], outputs):
        data.to_csv(args.out / f'{name}.csv', index=False)
    if args.decisions:
        decision_events(args.decisions).to_csv(args.out / 'decision-events.csv', index=False)
    (args.out / 'contract.json').write_text(json.dumps(dict(panel=panel, start=args.start, end=args.end,
                                                        horizon=args.horizon, records=str(args.records),
                                                        playlists=str(args.playlists)), indent=2))


if __name__ == '__main__':
    main()
