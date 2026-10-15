"""Read-only evidence for CelebrityEconomyAudit-2026-10-15.md; no simulation edits.

Usage: python SimTools/analyze_celebrity_audit.py --prefix SimLogs/media-p3-hot36
       python SimTools/analyze_celebrity_audit.py --world SimLogs/worlds/<name>.world.json.gz
"""
import argparse
import collections
import csv
import gzip
import itertools
import json
import math
from pathlib import Path
import statistics


def rows(path):
    with open(path, newline='', encoding='utf-8-sig') as source:
        yield from csv.DictReader(source)


def array_items(path, key):
    """Decode one named array incrementally, avoiding materializing a whole world."""
    decoder = json.JSONDecoder()
    token = '"' + key + '": ['
    with gzip.open(path, 'rt', encoding='utf-8-sig') as source:
        buffer = ''
        while token not in buffer:
            block = source.read(1048576)
            if not block:
                raise ValueError('Array not found: ' + key)
            buffer = buffer[-len(token):] + block
        buffer = buffer.split(token, 1)[1]
        while True:
            buffer = buffer.lstrip(' \r\n\t,')
            if buffer.startswith(']'):
                return
            try:
                item, end = decoder.raw_decode(buffer)
            except json.JSONDecodeError:
                block = source.read(1048576)
                if not block:
                    raise
                buffer += block
                continue
            yield item
            buffer = buffer[end:]


def world_summary(path, artists_only=False):
    careers = ['Unsigned', 'NewSigning', 'Rising', 'Established', 'Star', 'Superstar',
               'Declining', 'Dropped', 'Disbanded', 'Retired']
    people = {}
    artists = {}
    canopy = []
    fame = []
    counts = collections.Counter()
    stars_by_cohort = collections.Counter()
    household_by_cohort = collections.Counter()
    solo_stocks = []
    for a in array_items(path, 'Artists'):
        aid = a['artistId']
        artists[aid] = {k: a.get(k) for k in ['stageName', 'careerState', 'publicRecognition', 'formedYear', 'cohort']}
        r = a.get('publicRecognition', 0)
        fame.append(r)
        counts[careers[a['careerState']]] += 1
        if a['careerState'] in (4, 5):
            stars_by_cohort[str(a.get('cohort')) + ':' + careers[a['careerState']]] += 1
        if r > .6:
            household_by_cohort[str(a.get('cohort'))] += 1
        if any(' went solo from ' in e for e in a.get('careerEvents', [])):
            solo_stocks.append({'artistId': aid, 'name': a['stageName'], 'recognition': r,
                                'members': [{k: p.get(k) for k in ['personId', 'personalRecognition', 'creativeReputation']} for p in a.get('members', [])]})
        for p in a.get('members', []):
            people[p['personId']] = aid
        if any('seeded canopy' in e for e in a.get('careerEvents', [])):
            canopy.append({k: a.get(k) for k in ['artistId', 'stageName', 'formedYear', 'primaryGenre', 'careerState',
                                               'publicRecognition', 'culturalStanding', 'totalReleases', 'charted',
                                               'top40Hits', 'numberOnes', 'isActive']})
            canopy[-1]['memberAgesAt1960'] = [1960-p.get('birthYear', 1960) for p in a.get('members', [])]
    records = []
    ranks = collections.defaultdict(list)
    for r in ([] if artists_only else array_items(path, 'Records')):
        b = r.get('baseRecord') or {}
        if b.get('format', 0) != 0:
            continue
        slim = {k: b.get(k) for k in ['recordId', 'artistId', 'artistName', 'title']}
        slim.update({k: r.get(k) for k in ['currentPosition', 'unitsThisWeek', 'weeksSinceRelease']})
        slim['regions'] = {region: d.get('unitsSoldThisWeek', 0) for region, d in r.get('regionalData', {}).items()}
        records.append(slim)
        for region, units in slim['regions'].items():
            if units > 0:
                ranks[region].append((units, slim['recordId']))
    breadth = collections.Counter()
    for region, ranked in ranks.items():
        for _, rid in sorted(ranked, key=lambda x: (-x[0], x[1]))[:11]:
            breadth[rid] += 1
    return {'world': str(path), 'artists': len(artists), 'careerCounts': counts,
            'starsByCohort': stars_by_cohort,
            'householdNamesByCohort': household_by_cohort, 'soloSpinouts': solo_stocks,
            'recognitionAbove': {str(t): sum(r > t for r in fame) for t in [0, .08, .2, .4, .6]},
            'recognitionMaximum': max(fame, default=0), 'canopy': canopy,
            'top11RegionalBreadthHistogram': dict(collections.Counter(breadth.values())),
            'snapshotTop20': sorted((r for r in records if 0 < (r.get('currentPosition') or 0) <= 20), key=lambda r: r['currentPosition'])}


def corr(xs, ys):
    mx, my = statistics.mean(xs), statistics.mean(ys)
    num = sum((x-mx)*(y-my) for x, y in zip(xs, ys))
    den = math.sqrt(sum((x-mx)**2 for x in xs)*sum((y-my)**2 for y in ys))
    return num/den if den else 0


def regional_summary(prefix):
    record_act = {}
    lane_path = prefix + '-single-release-lanes.csv'
    for row in rows(lane_path):
        record_act[row['recordId']] = row['artistId']
    metrics = []
    previous = {}
    all_to_none = []
    breadth_hist = collections.Counter()
    for (week, year), group in itertools.groupby(rows(prefix + '-single-demand-stages.csv'), lambda r: (int(r['week']), int(r['year']))):
        markets = collections.defaultdict(dict)
        totals = collections.Counter()
        for row in group:
            rid, region, units = row['recordId'], row['region'], int(row['clearedUnits'])
            markets[region][rid] = units
            totals[rid] += units
        top = {}
        breadth = collections.Counter()
        acts_breadth = collections.defaultdict(set)
        for region, sales in markets.items():
            ranked = sorted(((v, k) for k, v in sales.items() if v > 0), key=lambda x: (-x[0], x[1]))
            top[region] = {rid for _, rid in ranked[:11]}
            for rid in top[region]:
                breadth[rid] += 1
                acts_breadth[record_act.get(rid, 'unmapped:' + rid)].add(region)
        breadth_hist.update(breadth.values())
        pairs = list(itertools.combinations(sorted(markets), 2))
        correlations, overlap = [], []
        for a, b in pairs:
            ids = set(markets[a]) | set(markets[b])
            correlations.append(corr([markets[a].get(k, 0) for k in ids], [markets[b].get(k, 0) for k in ids]))
            overlap.append(len(top[a] & top[b]) / 11)
        national_sales_rank = {rid: k+1 for k, (rid, _) in enumerate(totals.most_common())}
        for aid, before in previous.items():
            after = len(acts_breadth.get(aid, set()))
            if len(before['regions']) == 7 and after == 0:
                rids = before['records']
                all_to_none.append({'week': week, 'year': year, 'artistId': aid,
                                    'records': rids, 'nationalSalesRanksBefore': before['ranks'],
                                    'nationalSalesRanksAfter': [national_sales_rank.get(rid) for rid in rids],
                                    'actualSellingRegionsAfter': {rid: sum(markets[x].get(rid, 0) > 0 for x in markets) for rid in rids},
                                    'unitsAfter': {rid: totals[rid] for rid in rids}})
        previous = {aid: {'regions': regs,
                          'records': [rid for rid in breadth if record_act.get(rid, 'unmapped:'+rid) == aid],
                          'ranks': [national_sales_rank.get(rid) for rid in breadth if record_act.get(rid, 'unmapped:'+rid) == aid]}
                    for aid, regs in acts_breadth.items()}
        metrics.append({'week': week, 'year': year, 'meanRegionalUnitsCorrelation': statistics.mean(correlations),
                        'meanTop11PairOverlap': statistics.mean(overlap), 'recordsInAll7Top11': sum(v == 7 for v in breadth.values()),
                        'distinctTop11Records': len(breadth)})
    clearing = list(rows(prefix + '-market-clearing-weekly.csv'))
    return {'prefix': prefix, 'weeks': len(metrics), 'weeklyMetrics': metrics,
            'meanRegionalUnitsCorrelation': statistics.mean(m['meanRegionalUnitsCorrelation'] for m in metrics),
            'meanTop11PairOverlap': statistics.mean(m['meanTop11PairOverlap'] for m in metrics),
            'regionalTop11RecordWeekBreadthHistogram': breadth_hist,
            'all7To0ActTransitions': all_to_none,
            'marketClearing': {'regionWeeks': len(clearing),
                               'inventoryViolations': sum(int(r['inventoryViolationCount']) for r in clearing),
                               'allocationViolations': sum(int(r['allocationViolationCount']) for r in clearing),
                               'nonzeroReconciliation': sum(float(r['reconciliationDelta']) != 0 for r in clearing),
                               'nonzeroSettlementDelta': sum(float(r['settlementDelta']) != 0 for r in clearing)},
            'note': 'Top11 deliberately matches MediaFans score >=0.5: 1-k/20 admits k=0..10. National ranks here are units ranks, not published chart ranks. Unknown record-artist links remain separate, never merged.'}


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--prefix')
    parser.add_argument('--world')
    parser.add_argument('--artists-only', action='store_true')
    parser.add_argument('--output', required=True)
    args = parser.parse_args()
    result = world_summary(args.world, args.artists_only) if args.world else regional_summary(args.prefix)
    Path(args.output).write_text(json.dumps(result, indent=2), encoding='utf-8')
    print('Wrote ' + args.output)
