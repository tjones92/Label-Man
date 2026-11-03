"""Opening-label identity comparison and pre-exit cash bridges.

Descriptive matched identities in changing worlds, not causal firm matches.
Competition, pricing, recording choices and costs all change in the broad arm.
"""
import json
import argparse
import statistics
from collections import Counter, defaultdict
from pathlib import Path

from analyze_writing_concentration import iter_rows
from analyze_writing_supply_allocation import control_name

ROOT = Path(__file__).resolve().parents[1]
CAL = ROOT / 'SimLogs/writing-calibration'
OUTFLOWS = ('plantPayments', 'recording', 'campaigns', 'artistAdvances', 'licensing',
            'otherOperating', 'payroll', 'premises', 'retainers', 'restructuring',
            'legacyOverhead', 'ownerSalary', 'distributionOperations', 'tradeSupport',
            'recordExcise', 'taxPaid', 'capitalSpending')
INFLOWS = ('recordReceipts', 'serviceIncome', 'financingNet', 'ownerTransfersNet')


def cash_terms(row):
    terms = {k: float(row[k]) for k in INFLOWS}
    terms.update({k: -float(row[k]) for k in OUTFLOWS})
    terms['cashRounding'] = float(row['cashRounding'])
    residual = float(row['closingCash']) - float(row['openingCash']) - sum(terms.values())
    if abs(residual) > .01:
        raise ValueError(f"Cash bridge does not reconcile: {row['labelId']}/{row['period']}: {residual}")
    return terms


def identity(row):
    return tuple(row[k] for k in ('labelName', 'archetype', 'isHistorical', 'initialTier'))


def read_world(run):
    manifest = json.loads((CAL / f'{run}-manifest.json').read_text(encoding='utf-8-sig'))
    if '--weeks=104' not in manifest['flags']:
        raise ValueError('Expected 104-week world')
    log = (CAL / f'{run}.log').read_text(encoding='utf-8-sig')
    errors = (CAL / f'{run}-errors.log').read_text(encoding='utf-8-sig')
    if f'CHART_AUDIT_COMPLETE run={run} weeks=104' not in log or 'ERROR:' in errors or 'Exception' in errors:
        raise ValueError(f'{run}: incomplete or engine errors')
    directory = {r['labelId']: r for r in iter_rows(run, 'label-directory')}
    histories = defaultdict(list)
    for r in iter_rows(run, 'label-finance'):
        histories[r['labelId']].append(r)
    opening = {}
    for label, rows in histories.items():
        if int(rows[0]['week']) != 1:
            continue
        key = identity(directory[label])
        if key in opening:
            raise ValueError('Ambiguous opening identity')
        opening[key] = label
    monthly, sales, releases, shortages, risks, issue_refusals, launches = [defaultdict(list) for _ in range(7)]
    for r in iter_rows(run, 'company-monthly'):
        cash_terms(r)
        monthly[r['labelId']].append(r)
    for r in iter_rows(run, 'label-sales-weekly'):
        sales[r['labelId']].append(r)
    for r in iter_rows(run, 'writing-launches'):
        releases[r['labelId']].append((int(r['week']), 'Single', r['recordId']))
        launches[r['labelId']].append(r)
    for r in iter_rows(run, 'album-projects'):
        if r['terminalState'] == 'Released' and r['wasTransferred'].lower() == 'false':
            releases[r['originalLabelId']].append((int(r['dropWeek']), 'Album', r['albumRecordId']))
    for suffix, target in (('programme-cash-shortfalls', shortages), ('single-capital-risk', risks),
                           ('programme-issue-refusals', issue_refusals)):
        for r in iter_rows(run, suffix):
            target[r['labelId']].append(r)
    return dict(run=run, manifest=manifest, directory=directory, histories=histories, opening=opening,
                monthly=monthly, sales=sales, releases=releases, shortages=shortages,
                risks=risks, issueRefusals=issue_refusals, launches=launches)


def first_exit(rows):
    return next((int(r['week']) for r in rows if r['status'] == 'Defunct'), None)


def window(world, label, end, periods):
    start = max(1, end - 11)
    finance = [r for r in world['histories'][label] if int(r['week']) <= end]
    monthly = [r for r in world['monthly'][label] if int(r['period']) in periods]
    terms = Counter()
    for r in monthly:
        terms.update(cash_terms(r))
    units = sum(int(r['units']) for r in world['sales'][label] if start <= int(r['week']) <= end)
    by_format = Counter()
    for r in world['sales'][label]:
        if start <= int(r['week']) <= end:
            by_format[r['format']] += int(r['units'])
    released = {(fmt, record) for week, fmt, record in world['releases'][label] if start <= week <= end}
    shortages = {(r['week'], r['slotId']): r for r in world['shortages'][label]
                 if start <= int(r['week']) <= end}
    risks = {(r['week'], r['slotId'], r['prepared']): r for r in world['risks'][label]
             if start <= int(r['week']) <= end}
    rejected = [r for r in risks.values() if r['commit'].lower() == 'false']
    launched = [r for r in world['launches'][label] if start <= int(r['week']) <= end]
    quality = {k: statistics.mean(float(r[k]) for r in launched) if launched else None
               for k in ('recordHook', 'runtimeQuality', 'production')}
    return dict(units12Weeks=units, units12WeeksByFormat=dict(by_format),
                releases12Weeks=dict(Counter(fmt for fmt, _ in released)),
                singleLaunchQuality=quality,
                openingFourWeekUnits=sum(int(r['units']) for r in world['sales'][label] if 1 <= int(r['week']) <= 4),
                firstCashShortfallWeek=min((int(r['week']) for r in world['shortages'][label]), default=None),
                windowWeeks=end - start + 1, monthlyPeriods=sorted(periods), cashTerms=dict(terms),
                cashFlow=sum(terms.values()), monthlyRecordUnits=sum(float(r['recordUnits']) for r in monthly),
                monthlyRecordGross=sum(float(r['recordGross']) for r in monthly),
                initialStatementCash=float(world['monthly'][label][0]['openingCash']) if world['monthly'][label] else None,
                openingStatementCash=float(monthly[0]['openingCash']) if monthly else None,
                closingStatementCash=float(monthly[-1]['closingCash']) if monthly else None,
                endpointCash=float(finance[-1]['cashReserves']),
                endpointReceivables=float(finance[-1]['outstandingWholesaleReceivables']),
                endpointPlantPayables=float(finance[-1]['outstandingPlantPayables']),
                cashShortfallSlotWeeks=len(shortages),
                shortagesWithImmediateProjectCash=sum(float(r['cash']) >= sum(float(r[k]) for k in
                    ('production', 'marketing', 'pressing')) for r in shortages.values()),
                riskReviews=len(risks), riskRefusals=len(rejected),
                riskRefusalsMeanWouldCommit=sum(r['meanCommit'].lower() == 'true' for r in rejected),
                issueRefusals=dict(Counter(r['gate'] for r in world['issueRefusals'][label]
                    if start <= int(r['week']) <= end)))


def firm_pair(control, combined, key):
    ci, ti = control['opening'][key], combined['opening'][key]
    ce, te = first_exit(control['histories'][ci]), first_exit(combined['histories'][ti])
    cohort = ('bothExit' if ce and te else 'additionalExit' if te else 'rescuedIdentity' if ce else 'bothActive')
    # Compare the same calendar horizon, before either world loses the identity.
    end = min(x for x in (ce, te, 104) if x is not None)
    periods = [{int(r['period']) for r in w['monthly'][i] if int(r['week']) <= end}
               for w, i in ((control, ci), (combined, ti))]
    common = set(sorted(periods[0] & periods[1])[-3:])
    cw, tw = window(control, ci, end, common), window(combined, ti, end, common)
    delta = {k: tw['cashTerms'].get(k, 0) - cw['cashTerms'].get(k, 0) for k in (*INFLOWS, *OUTFLOWS, 'cashRounding')}
    negatives = {k: v for k, v in delta.items() if v < -.01}
    dominant = min(negatives, key=negatives.get) if negatives else 'noNegativeFlowComponent'
    return dict(identity=list(key), controlId=ci, combinedId=ti, cohort=cohort,
                controlExitWeek=ce, combinedExitWeek=te, anchorWeek=end,
                controlReason=control['histories'][ci][-1]['exitReason'],
                combinedReason=combined['histories'][ti][-1]['exitReason'],
                control=cw, combined=tw, cashFlowDelta=tw['cashFlow']-cw['cashFlow'],
                cashTermDeltas=delta, largestNegativeCashTerm=dominant,
                unitsLower=tw['units12Weeks'] < cw['units12Weeks'],
                receiptsLower=tw['cashTerms'].get('recordReceipts', 0) < cw['cashTerms'].get('recordReceipts', 0),
                openingFourWeekUnitsLower=tw['openingFourWeekUnits'] < cw['openingFourWeekUnits'],
                releasesLower=sum(tw['releases12Weeks'].values()) < sum(cw['releases12Weeks'].values()))


def summarize(rows):
    terms = Counter()
    for r in rows:
        terms.update(r['cashTermDeltas'])
    cash_observed = [r for r in rows if r['control']['monthlyPeriods']]
    quality_observed = [r for r in rows if r['control']['singleLaunchQuality']['recordHook'] is not None
                        and r['combined']['singleLaunchQuality']['recordHook'] is not None]
    formats = {}
    for side in ('control','combined'):
        counts = Counter()
        for r in rows:
            counts.update(r[side]['units12WeeksByFormat'])
        formats[side] = dict(counts)
    return dict(identities=len(rows), initialTiers=dict(Counter(r['identity'][3] for r in rows)),
                combinedExitReasons=dict(Counter(r['combinedReason'] for r in rows)),
                unitsLower=sum(r['unitsLower'] for r in rows), receiptsLower=sum(r['receiptsLower'] for r in rows),
                releasesLower=sum(r['releasesLower'] for r in rows),
                noCombinedSingleLaunch=sum(r['combined']['releases12Weeks'].get('Single',0) == 0 for r in rows),
                noControlSingleLaunch=sum(r['control']['releases12Weeks'].get('Single',0) == 0 for r in rows),
                openingFourWeekUnitsLower=sum(r['openingFourWeekUnitsLower'] for r in rows),
                cashShortfallsByWeekFour=sum(r['combined']['firstCashShortfallWeek'] is not None and r['combined']['firstCashShortfallWeek'] <= 4 for r in rows),
                sameInitialStatementCash=sum(abs(r['control']['initialStatementCash']-r['combined']['initialStatementCash']) < .01
                    for r in rows if r['control']['initialStatementCash'] is not None and r['combined']['initialStatementCash'] is not None),
                comparableSingleQuality=len(quality_observed),
                singleHookLower=sum(r['combined']['singleLaunchQuality']['recordHook'] < r['control']['singleLaunchQuality']['recordHook'] for r in quality_observed),
                medianSingleHookDelta=statistics.median(r['combined']['singleLaunchQuality']['recordHook']-r['control']['singleLaunchQuality']['recordHook'] for r in quality_observed) if quality_observed else None,
                medianUnitsDelta=statistics.median(r['combined']['units12Weeks']-r['control']['units12Weeks'] for r in rows) if rows else None,
                cashObservedIdentities=len(cash_observed), fewerThanThreeCommonMonths=sum(len(r['control']['monthlyPeriods']) < 3 for r in rows),
                aggregateCashTermDeltas=dict(terms),
                largestNegativeCashTerms=dict(Counter(r['largestNegativeCashTerm'] for r in cash_observed)),
                cashFlowLower=sum(r['cashFlowDelta'] < 0 for r in cash_observed),
                withCashShortfalls=sum(r['combined']['cashShortfallSlotWeeks'] > 0 for r in rows),
                withRiskRefusals=sum(r['combined']['riskRefusals'] > 0 for r in rows),
                withRiskRefusalsMeanWouldCommit=sum(r['combined']['riskRefusalsMeanWouldCommit'] > 0 for r in rows),
                unitsByFormat=formats,
                controls={k: sum(r['control'][k] for r in rows) for k in ('units12Weeks', 'cashShortfallSlotWeeks', 'riskRefusals', 'shortagesWithImmediateProjectCash')},
                combined={k: sum(r['combined'][k] for r in rows) for k in ('units12Weeks', 'cashShortfallSlotWeeks', 'riskReviews', 'riskRefusals', 'riskRefusalsMeanWouldCommit', 'shortagesWithImmediateProjectCash')})


def compare(seeds=(1001, 2002), control_tag=None, treatment_tag='writing-combined-integration'):
    if not seeds or len(set(seeds)) != len(seeds):
        raise ValueError('Expected distinct seeds')
    pairs, all_rows, hashes = [], [], set()
    for seed in seeds:
        control_run = control_name(seed) if control_tag is None else f'{control_tag}-market-{seed}'
        target_run = f'{treatment_tag}-market-{seed}'
        if control_run == target_run:
            raise ValueError('Comparison arms must differ')
        control, combined = read_world(control_run), read_world(target_run)
        hashes.update(w['manifest']['assemblySha256'] for w in (control, combined))
        if set(control['opening']) != set(combined['opening']):
            raise ValueError('Opening identity sets differ')
        rows = [dict(seed=seed, **firm_pair(control, combined, k)) for k in sorted(control['opening'])]
        all_rows.extend(rows)
        pairs.append(dict(seed=seed, cohorts={c: summarize([r for r in rows if r['cohort'] == c])
                     for c in ('additionalExit', 'rescuedIdentity', 'bothExit', 'bothActive')}))
    if len(hashes) != 1:
        raise ValueError('Mixed assemblies')
    return dict(assemblySha256=hashes.pop(), controlTag=control_tag, treatmentTag=treatment_tag, pairs=pairs,
                pooled={c: summarize([r for r in all_rows if r['cohort'] == c])
                        for c in ('additionalExit', 'rescuedIdentity', 'bothExit', 'bothActive')},
                identities=all_rows,
                limitations=['Identity matching does not freeze initial assets, rosters or world trajectories.',
                             'Pre-exit units include opening catalogue and changing format/channel mix.',
                             'Cash bridges compare up to three common calendar months, not equal firm lifetimes.',
                             'Refusals are deduplicated reviews, not identified lost releases.',
                             'Largest negative cash component is an accounting description, not a causal attribution.'])


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--control-tag')
    parser.add_argument('--treatment-tag', default='writing-combined-integration')
    parser.add_argument('--output', type=Path, default=CAL / 'writing-combined-label-turnover.json')
    args = parser.parse_args()
    result = compare(control_tag=args.control_tag, treatment_tag=args.treatment_tag)
    args.output.write_text(json.dumps(result, indent=2)+'\n', encoding='utf-8')
    print(json.dumps(result['pooled'], indent=2))
