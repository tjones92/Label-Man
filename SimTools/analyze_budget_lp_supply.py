"""V2 budget supply screen and channel-gap arithmetic. No supply weight is a sales forecast.

python SimTools/analyze_budget_lp_supply.py --tag rkj-v1 --supply-run blp-v2-supply-racks-default-1001-13
"""
import argparse
import csv
import json
import math
from collections import defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / 'SimLogs'


def rows(path):
    with path.open(encoding='utf-8-sig', newline='') as handle:
        yield from csv.DictReader(handle)


def budget_share(dollars, budget_price, frontline_price):
    budget = dollars / budget_price
    return budget / (budget + (1 - dollars) / frontline_price)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--tag', required=True, help='Completed rack probe with review/annual.csv')
    parser.add_argument('--supply-run', help='Optional run prefix with budget-lp-supply.csv')
    args = parser.parse_args()
    plan = json.loads((ROOT / args.tag / 'plan.json').read_text(encoding='utf-8-sig'))
    if not plan['runs'] or any(not r['passed'] for r in plan['runs']):
        raise ValueError('Rack probe has no completed runs or includes failed runs')
    report = dict(reference=args.tag, historicalYear=1961, historicalScope='older Billboard series; soft benchmarks',
                  budgetDollarShare1962=.21, frontlineRetailPriceAssumption=3,
                  note='1962 NARM member dollars applied to 1961 national rack units is a sensitivity, not a measured 1961 share.',
                  channels=[], priceScenarios=[])
    for price in (1.25, 1.75):
        share = budget_share(.21, price, 3)
        report['priceScenarios'].append(dict(budgetRetailPrice=price, budgetUnitShare=share,
                                             inferredBudgetRackM=60 * share, inferredFrontlineRackM=60 * (1 - share)))
    for row in rows(ROOT / args.tag / 'review' / 'annual.csv'):
        if row['arm'] != 'racks' or int(row['year']) != 1961:
            continue
        store, rack, club, shelf = (float(row[k]) for k in ('storeLpM', 'rackLpM', 'clubLpM', 'rackShelfM'))
        headroom = shelf - rack
        report['channels'].append(dict(seed=int(row['seed']), storeExcessM=store - 68,
            rackShortfallM=60 - rack, clubShortfallM=45 - club,
            netLpShortfallM=173 - store - rack - club, unusedRackShelfM=headroom,
            shelfShortfallToHistoricalRacksM=60 - shelf,
            budgetScenarios=[dict(inferredBudgetRackM=s['inferredBudgetRackM'],
                                 residualNetGapIfAdditiveM=173 - store - rack - club - s['inferredBudgetRackM'],
                                 budgetAboveCurrentHeadroomM=max(0, s['inferredBudgetRackM'] - headroom))
                             for s in report['priceScenarios']]))
    if not report['channels']:
        raise ValueError('No 1961 racks rows; run the rack analyzer on a complete 106-week probe first')
    if args.supply_run:
        buckets = defaultdict(lambda: dict(observations=0, peakSources=0, peakKnownTitles=0, peakAppeal=0.0))
        weeks = defaultdict(lambda: [0, 0, 0.0])
        seen = set()
        for row in rows(ROOT / f'{args.supply_run}-budget-lp-supply.csv'):
            key = (int(row['week']), row['labelId'], row['artistId'], row['sourceKind'])
            if key in seen:
                raise ValueError(f'Duplicate source {key}')
            seen.add(key)
            kind = row['sourceKind']
            if kind not in {'Retired', 'OpeningSynthetic', 'LiveAged52'}:
                raise ValueError(f'Unknown source kind {kind}')
            appeal, adult = float(row['appeal']), float(row['adultAppeal'])
            if not math.isfinite(appeal) or not math.isfinite(adult) or appeal <= 0 or not 0 <= adult <= appeal:
                raise ValueError(f'Invalid supply weights {key}')
            if (kind == 'OpeningSynthetic') != (row['knownTitles'] == ''):
                raise ValueError('Synthetic title count must stay unknown; earned title count must be known')
            titles = int(row['knownTitles']) if row['knownTitles'] else 0
            if titles < 0:
                raise ValueError('Negative title count')
            b = buckets[kind]
            b['observations'] += 1
            w = weeks[(int(row['week']), kind)]
            w[0] += 1
            w[1] += titles
            w[2] += appeal
        if not seen:
            raise ValueError('Supply audit is empty')
        for (_, kind), (sources, titles, appeal) in weeks.items():
            b = buckets[kind]
            b['peakSources'] = max(b['peakSources'], sources)
            b['peakKnownTitles'] = max(b['peakKnownTitles'], titles)
            b['peakAppeal'] = max(b['peakAppeal'], appeal)
        report['supply'] = dict(run=args.supply_run, weeks=len({k[0] for k in seen}), byKind=dict(buckets),
                              note='Peaks are separate weekly maxima. Appeal is a decaying sales weight, not buyers, demand or units available.')
    output = ROOT / (args.supply_run or args.tag) / 'review'
    output.mkdir(parents=True, exist_ok=True)
    (output / 'budget-lp-supply.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(json.dumps(report, indent=2))


if __name__ == '__main__':
    main()
