"""Stage A audit of booked release contribution and regional format clearing.

Requires the explicit Stage A columns; older realizedNet alone cannot establish
the marketing boundary. Uses only the standard library, consumes no simulation RNG.
"""
import argparse
import csv
import json
import math
from collections import Counter, defaultdict
from pathlib import Path


def rows(prefix, suffix, required):
    path = Path(f"{prefix}-{suffix}.csv")
    with path.open(newline="", encoding="utf-8-sig") as stream:
        reader = csv.DictReader(stream)
        missing = set(required) - set(reader.fieldnames or [])
        if missing:
            raise ValueError(f"{path}: missing Stage A columns {sorted(missing)}; rerun the current audit runner")
        yield from reader


def number(row, field):
    value = float(row[field])
    if not math.isfinite(value):
        raise ValueError(f"Nonfinite {field}: {row}")
    return value


def equal(actual, expected, context, tolerance=.05):
    if not math.isclose(actual, expected, rel_tol=1e-6, abs_tol=tolerance):
        raise ValueError(f"{context}: {actual} != {expected}")


def clearing_summary(prefix):
    totals = defaultdict(Counter)
    seen, weeks = set(), set()
    regions_by_week = defaultdict(set)
    required = ['week', 'regionId', 'localClearedSingleUnits', 'localClearedAlbumUnits',
                'serviceableSingleIntent', 'serviceableAlbumIntent', 'singleFormatBudget', 'albumFormatBudget',
                'clearedSingleUnits', 'clearedAlbumUnits', 'localCleared', 'spilloverCleared',
                'clearedTotalUnits', 'marketDisplacedDemand', 'inventoryViolationCount',
                'allocationViolationCount', 'reconciliationDelta', 'settlementDelta']
    for row in rows(prefix, 'market-clearing-weekly', required):
        key = (int(row['week']), row['regionId'])
        if key in seen:
            raise ValueError(f"Duplicate clearing region-week: {key}")
        seen.add(key)
        weeks.add(key[0])
        regions_by_week[key[0]].add(key[1])
        for field in ('inventoryViolationCount', 'allocationViolationCount', 'reconciliationDelta', 'settlementDelta'):
            equal(number(row, field), 0, f"{key} {field}", tolerance=0)
        local_total = final_total = serviceable_total = 0
        for fmt, title in [('Single', 'single'), ('Album', 'album')]:
            intent = number(row, f'serviceable{fmt}Intent')
            budget = number(row, f'{title}FormatBudget')
            local = number(row, f'localCleared{fmt}Units')
            final = number(row, f'cleared{fmt}Units')
            if min(intent, budget, local, final) < 0 or local > min(intent, budget) or final < local or final > intent:
                raise ValueError(f"Invalid {fmt} allocation: {key}")
            group = totals[fmt]
            group['regionWeeks'] += 1
            group['localBudgetBindingRegionWeeks'] += intent > budget
            group['residualUnmetRegionWeeks'] += final < intent
            group['serviceableIntent'] += intent
            group['localBudget'] += budget
            group['localCleared'] += local
            group['spilloverCleared'] += final - local
            group['finalCleared'] += final
            group['localDisplaced'] += intent - local
            group['residualDisplaced'] += intent - final
            local_total += local
            final_total += final
            serviceable_total += intent
        equal(local_total, number(row, 'localCleared'), f'{key} local partition')
        equal(final_total, number(row, 'clearedTotalUnits'), f'{key} final partition')
        equal(final_total - local_total, number(row, 'spilloverCleared'), f'{key} spillover partition')
        equal(serviceable_total - final_total, number(row, 'marketDisplacedDemand'), f'{key} displaced partition')
    if not seen:
        raise ValueError('No live market-clearing observations')
    if len(weeks) != max(weeks) - min(weeks) + 1 or any(panel != regions_by_week[min(weeks)] for panel in regions_by_week.values()):
        raise ValueError('Incomplete weekly clearing coverage or inconsistent regional panel')
    for group in totals.values():
        group['localBudgetBindingShare'] = group['localBudgetBindingRegionWeeks'] / group['regionWeeks']
        group['finalFulfillmentShare'] = group['finalCleared'] / group['serviceableIntent'] if group['serviceableIntent'] else None
    return {'weeks': len(weeks), 'firstWeek': min(weeks), 'lastWeek': max(weeks),
            'regionWeeks': len(seen), 'byFormat': dict(totals)}


def release_summary(prefix, launch_through_week=None):
    groups = defaultdict(Counter)
    seen = set()
    final_outcomes = {}
    required = ['recordId', 'format', 'genre', 'memoryEligible', 'lifetimeLabelNet', 'sunkProductionCost', 'sunkMarketingCost', 'bookedContributionBeforeMarketing',
                'bookedContributionAfterMarketing', 'unitsPressed', 'pressingBookSold', 'realizedNet']
    for row in rows(prefix, 'release-outcomes', required):
        rid = row['recordId']
        if not rid or rid in seen:
            raise ValueError(f'Missing or duplicate finalized record: {rid}')
        seen.add(rid)
        if row['memoryEligible'] not in ('true', 'false'):
            raise ValueError(f'Invalid memory eligibility: {rid}')
        before = number(row, 'bookedContributionBeforeMarketing')
        # Legacy columns use display precision; explicit contribution uses round-trip precision.
        equal(before, number(row, 'lifetimeLabelNet') - number(row, 'sunkProductionCost'), f'{rid} before marketing')
        known = row['sunkMarketingCost'] != ''
        after = None
        if known:
            marketing = number(row, 'sunkMarketingCost')
            if marketing < 0:
                raise ValueError(f'Negative marketing cost: {rid}')
            after = number(row, 'bookedContributionAfterMarketing')
            equal(after, before - marketing, f'{rid} marketing identity')
            equal(after, number(row, 'realizedNet'), f'{rid} legacy outcome')
        else:
            if row['bookedContributionAfterMarketing'] != '':
                raise ValueError(f'After-marketing value without attribution: {rid}')
            equal(before, number(row, 'realizedNet'), f'{rid} unattributed outcome')
        pressed, sold = number(row, 'unitsPressed'), number(row, 'pressingBookSold')
        if min(pressed, sold) < 0 or sold > pressed or not pressed.is_integer() or not sold.is_integer():
            raise ValueError(f'Invalid pressing book: {rid}')
        final_outcomes[rid] = dict(format=row['format'], before=before, after=after, sold=sold,
                                   week=int(row['week']) if 'week' in row else None)
        for key in (f"format:{row['format']}", f"formatGenre:{row['format']}|{row['genre']}",
                    f"formatMemoryEligible:{row['format']}|{row['memoryEligible']}"):
            group = groups[key]
            group['finalized'] += 1
            group['lossBeforeMarketing'] += before < 0
            group['bookedContributionBeforeMarketing'] += before
            group['knownMarketing'] += known
            group['unknownMarketing'] += not known
            if known:
                group['lossAfterMarketing'] += after < 0
                group['bookedContributionAfterMarketing'] += after
                group['attributedMarketing'] += marketing
            group['unitsPressed'] += pressed
            group['pressingBookSold'] += sold
            group['unsoldBookedCopies'] += pressed - sold
            group['zeroBookedSold'] += sold == 0
            group['under500BookedSold'] += sold < 500
            group['under1000BookedSold'] += sold < 1000
    for group in groups.values():
        group['lossBeforeMarketingShare'] = group['lossBeforeMarketing'] / group['finalized']
        group['lossAfterMarketingShare'] = group['lossAfterMarketing'] / group['knownMarketing'] if group['knownMarketing'] else None
    coverage = None
    campaign_path = Path(f'{prefix}-release-campaign-costs.csv')
    if campaign_path.exists():
        campaigns = set()
        for row in rows(prefix, 'release-campaign-costs', ['recordId']):
            rid = row['recordId']
            if not rid or rid in campaigns:
                raise ValueError(f'Missing or duplicate launch campaign: {rid}')
            campaigns.add(rid)
        coverage = {'observedLaunchCampaigns': len(campaigns), 'finalized': len(campaigns & seen),
                    'notFinalizedInWindow': len(campaigns - seen)}
    return {'finalized': len(seen), 'groups': dict(groups), 'attributedCampaignCoverage': coverage,
            'launchCohorts': cohort_summary(prefix, final_outcomes, launch_through_week)}


def quality_band(quality):
    for limit in (.4, .5, .6, .7):
        if quality <= limit:
            return f'<=0.4' if limit == .4 else f'>{limit-.1:.1f} to {limit:.1f}'
    return '>0.7'


def cohort_summary(prefix, outcomes, launch_through_week=None):
    census_path = Path(f'{prefix}-demand-release-census.csv')
    if not census_path.exists():
        if Path(f'{prefix}-demand-observation-window.csv').exists():
            raise ValueError('Observation window is missing its actual-release census')
        if launch_through_week is not None:
            raise ValueError('Launch cutoff requires the actual-release census')
        return None
    windows = list(rows(prefix, 'demand-observation-window', [
        'startAuditWeek', 'endAuditWeek', 'completed', 'openingRecords', 'launchedRecords',
        'activeRecords', 'retiredRecords', 'missingRecords']))
    if len(windows) != 1 or windows[0]['completed'] != 'true':
        raise ValueError('Cohort analysis requires one completed observation window')
    window = windows[0]
    start, end = int(window['startAuditWeek']), int(window['endAuditWeek'])
    cutoff = end if launch_through_week is None else launch_through_week
    if start < 0 or end <= start or not start < cutoff <= end:
        raise ValueError('Invalid observation window or launch cutoff')
    counts = Counter({field: 0 for field in ('openingRecords', 'launchedRecords', 'activeRecords',
                                            'retiredRecords', 'missingRecords', 'launchedAfterCutoff', 'selectedLaunches')})
    groups = defaultdict(Counter)
    seen, retired_ids = set(), set()
    fields = ['recordId', 'format', 'genre', 'observationKind', 'observedAuditWeek',
              'observedYear', 'launchTier', 'runtimeQualityAtLaunch', 'status', 'retiredAuditWeek']
    for row in rows(prefix, 'demand-release-census', fields):
        rid = row['recordId']
        if not rid or rid in seen:
            raise ValueError(f'Missing or duplicate census record: {rid}')
        seen.add(rid)
        kind, status = row['observationKind'], row['status']
        observed = int(row['observedAuditWeek'])
        if kind not in ('Opening', 'Launch') or status not in ('Active', 'Retired'):
            raise ValueError(f'Invalid census kind/status: {rid}')
        counts['openingRecords' if kind == 'Opening' else 'launchedRecords'] += 1
        counts['activeRecords' if status == 'Active' else 'retiredRecords'] += 1
        final = outcomes.get(rid)
        if (final is not None) != (status == 'Retired'):
            raise ValueError(f'Census retirement/outcome mismatch: {rid}')
        if status == 'Retired':
            retired = int(row['retiredAuditWeek'])
            if not max(start + 1, observed) <= retired <= end or final['week'] != retired or final['format'] != row['format']:
                raise ValueError(f'Invalid retirement date/format: {rid}')
            retired_ids.add(rid)
        elif row['retiredAuditWeek']:
            raise ValueError(f'Active record has retirement date: {rid}')
        if kind == 'Opening':
            if observed != start or row['runtimeQualityAtLaunch'] or row['launchTier']:
                raise ValueError(f'Opening record claims launch attributes: {rid}')
            continue
        if not start < observed <= end or not row['launchTier']:
            raise ValueError(f'Invalid launch observation: {rid}')
        quality = number(row, 'runtimeQualityAtLaunch')
        if not 0 <= quality <= 1:
            raise ValueError(f'Invalid runtime launch quality: {rid}')
        if observed > cutoff:
            counts['launchedAfterCutoff'] += 1
            continue
        cohort_start = start + 1 + ((observed - start - 1) // 13) * 13
        keys = [f"format:{row['format']}", f"formatTier:{row['format']}|{row['launchTier']}",
                f"formatGenre:{row['format']}|{row['genre']}",
                f"formatQuality:{row['format']}|{quality_band(quality)}",
                f"formatLaunchCohort:{row['format']}|{cohort_start}-{min(cohort_start+12, cutoff)}"]
        counts['selectedLaunches'] += 1
        for key in keys:
            group = groups[key]
            group['launched'] += 1
            group['activeAtEnd'] += status == 'Active'
            group['finalized'] += status == 'Retired'
            if final is None:
                continue
            group['knownMarketing'] += final['after'] is not None
            group['unknownMarketing'] += final['after'] is None
            group['lossBeforeMarketing'] += final['before'] < 0
            group['bookedContributionBeforeMarketing'] += final['before']
            if final['after'] is not None:
                group['lossAfterMarketing'] += final['after'] < 0
                group['bookedContributionAfterMarketing'] += final['after']
            group['zeroBookedSold'] += final['sold'] == 0
            group['under500BookedSold'] += final['sold'] < 500
            group['under1000BookedSold'] += final['sold'] < 1000
    if retired_ids != outcomes.keys():
        raise ValueError('Finalized outcomes absent from census')
    for field in ('openingRecords', 'launchedRecords', 'activeRecords', 'retiredRecords', 'missingRecords'):
        if counts[field] != int(window[field]):
            raise ValueError(f'Census/window count mismatch: {field}')
    for group in groups.values():
        for field in ('knownMarketing', 'unknownMarketing', 'lossBeforeMarketing', 'lossAfterMarketing'):
            group.setdefault(field, 0)
        group['finalizedShare'] = group['finalized'] / group['launched']
        group['finalizedLossBeforeMarketingShare'] = group['lossBeforeMarketing'] / group['finalized'] if group['finalized'] else None
        group['finalizedLossAfterMarketingShare'] = group['lossAfterMarketing'] / group['knownMarketing'] if group['knownMarketing'] else None
        group['completeCohort'] = group['finalized'] == group['launched']
        group['cohortLifetimeLossBeforeMarketingShare'] = group['finalizedLossBeforeMarketingShare'] if group['completeCohort'] else None
        group['cohortLifetimeLossAfterMarketingShare'] = group['finalizedLossAfterMarketingShare'] if group['completeCohort'] and group['knownMarketing'] == group['launched'] else None
    return {'startAuditWeek': start, 'endAuditWeek': end, 'launchThroughWeek': cutoff,
            'coverage': dict(counts), 'groups': dict(groups)}


def physical_inventory_summary(prefix):
    if not Path(f'{prefix}-physical-inventory.csv').exists():
        return None
    quantities = ['openingUnits', 'productionReceived', 'unitsPressed', 'bookSold', 'warehouse', 'shelves',
                  'heldForArrival', 'shipments', 'saleableReturns', 'heldReturns', 'promotionalWithdrawals',
                  'damagedCopies', 'terminalWriteOffs', 'distributedWriteOffs', 'productionOrdered',
                  'unbilledInitial', 'unbilledRepress', 'unfilledShipmentRequests']
    cumulative = ['openingUnits', 'productionReceived', 'unitsPressed', 'bookSold', 'shipments',
                  'saleableReturns', 'heldReturns', 'promotionalWithdrawals', 'damagedCopies',
                  'terminalWriteOffs', 'distributedWriteOffs', 'unfilledShipmentRequests']
    latest, seen = {}, set()
    observations = 0
    for row in rows(prefix, 'physical-inventory', ['week', 'recordId', 'format', 'status', 'productionDueWeek',
                                                 'reconciliationDelta'] + quantities):
        rid, week = row['recordId'], int(row['week'])
        if not rid or (rid, week) in seen or row['status'] not in ('Active', 'Retired'):
            raise ValueError(f'Invalid physical inventory identity/status: {rid}, {week}')
        seen.add((rid, week))
        values = {field: number(row, field) for field in quantities}
        if any(value < 0 or not value.is_integer() for value in values.values()):
            raise ValueError(f'Invalid physical inventory quantity: {rid}')
        v = values
        # Integer conservation must be exact even for very large unit counts.
        def exact(actual, expected, context):
            if actual != expected:
                raise ValueError(f'{rid} {context}: {actual} != {expected}')
        exact(v['openingUnits'] + v['productionReceived'], v['unitsPressed'], 'production identity')
        distributed = v['shelves'] + v['heldForArrival']
        exact(v['unitsPressed'], v['bookSold'] + v['warehouse'] + distributed + v['promotionalWithdrawals']
              + v['damagedCopies'] + v['terminalWriteOffs'], 'unit conservation')
        exact(v['shipments'] - v['saleableReturns'] - v['heldReturns'] - v['damagedCopies']
              - v['bookSold'] - v['distributedWriteOffs'], distributed, 'shipment conservation')
        if v['distributedWriteOffs'] > v['terminalWriteOffs']:
            raise ValueError(f'Distributed write-offs exceed all write-offs: {rid}')
        exact(number(row, 'reconciliationDelta'), 0, 'reported reconciliation')
        exact(v['unbilledInitial'] + v['unbilledRepress'], 0, 'unsettled manufacturing receipts')
        due = int(row['productionDueWeek'])
        if (v['productionOrdered'] == 0 and due != -1) or (v['productionOrdered'] > 0 and due <= week):
            raise ValueError(f'Invalid production due week: {rid}')
        if row['status'] == 'Retired' and v['warehouse'] + distributed + v['productionOrdered'] != 0:
            raise ValueError(f'Retired inventory still available or committed: {rid}')
        if rid in latest:
            previous = latest[rid]
            if previous['status'] == 'Retired' or week != previous['week'] + 1 or row['format'] != previous['format']:
                raise ValueError(f'Broken physical inventory history: {rid}')
            if any(v[field] < previous[field] for field in cumulative) or v['openingUnits'] != previous['openingUnits']:
                raise ValueError(f'Physical inventory cumulative counters reversed: {rid}')
        latest[rid] = v | dict(week=week, status=row['status'], format=row['format'])
        observations += 1
    if not latest:
        raise ValueError('No physical inventory observations')
    census = {r['recordId']: r for r in rows(prefix, 'demand-release-census', ['recordId', 'status', 'retiredAuditWeek'])}
    window = list(rows(prefix, 'demand-observation-window', ['endAuditWeek', 'completed']))
    if len(window) != 1 or window[0]['completed'] != 'true':
        raise ValueError('Physical inventory requires a completed observation window')
    outcomes = {r['recordId']: r for r in rows(prefix, 'release-outcomes', ['recordId', 'unitsPressed', 'pressingBookSold'])}
    totals = defaultdict(Counter)
    for rid, v in latest.items():
        c = census.get(rid)
        if c is None or c['status'] != v['status']:
            raise ValueError(f'Physical inventory/census status mismatch: {rid}')
        expected_week = int(c['retiredAuditWeek']) if v['status'] == 'Retired' else int(window[0]['endAuditWeek'])
        if v['week'] != expected_week:
            raise ValueError(f'Incomplete physical inventory history: {rid}')
        if v['status'] == 'Retired':
            outcome = outcomes.get(rid)
            if outcome is None or number(outcome, 'unitsPressed') != v['unitsPressed'] or number(outcome, 'pressingBookSold') != v['bookSold']:
                raise ValueError(f'Physical inventory/outcome mismatch: {rid}')
        group = totals[v['format']]
        group['records'] += 1
        group['retiredRecords'] += v['status'] == 'Retired'
        for field in quantities:
            group[field] += v[field]
    return dict(recordWeeks=observations, byFormat=dict(totals),
                boundary='AI units only; shipments and saleable dealer returns are transfers, sales are retail sell-through; production orders are not receipts.')


def summarize(prefix, launch_through_week=None):
    clearing, releases = clearing_summary(prefix), release_summary(prefix, launch_through_week)
    cohort = releases['launchCohorts']
    if cohort is not None and (clearing['firstWeek'] != cohort['startAuditWeek'] + 1 or clearing['lastWeek'] != cohort['endAuditWeek']):
        raise ValueError('Clearing coverage does not match the completed observation window')
    inventory = physical_inventory_summary(prefix)
    return {'run': str(prefix), 'clearing': clearing, 'releases': releases, 'physicalInventory': inventory,
            'limitations': [
                'All-outcome groups contain finalized records only. Launch-cohort groups also count active records; finalized-only loss shares remain censored.',
                'All-outcome groups include opening/prewarm catalog; actual-launch cohorts exclude it and are unavailable in older runs without the census.',
                'Booked contribution is not cash collection, overhead-allocated profit, or artist recoupment.',
                ('Physical inventory is opt-in. Initial availability and one-week plant lead time are model assumptions; no historical return or damage rates are fitted.'
                 if inventory is not None else 'Unsold booked copies are not physical returns or write-offs; shelf inventory is a separate model.'),
                'Serviceable intent already includes stock and outlet constraints and sales jitter; backorders retain prior weeks.',
                'The existing settlementDelta column repeats the regional allocation delta; it is not an independent cash or settlement audit.'
            ], 'pass': True}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('prefixes', nargs='+', type=Path)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--launch-through-week', type=int, help='Select only launches up to this audit week; keep the full follow-up window')
    args = parser.parse_args()
    report = [summarize(prefix, args.launch_through_week) for prefix in args.prefixes]
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, indent=2, allow_nan=False) + '\n', encoding='utf-8')
    print(f'Validated {len(report)} run(s); wrote {args.output}')


if __name__ == '__main__':
    main()
