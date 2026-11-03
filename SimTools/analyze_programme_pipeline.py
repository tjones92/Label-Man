"""Trace a fixed opening cohort through matched programme audits, retaining unfinished intentions."""
import argparse
import csv
import json
import statistics
from collections import Counter
from pathlib import Path


def rows(path):
    with open(path, newline='', encoding='utf-8-sig') as stream:
        yield from csv.DictReader(stream)


def opening_cohort(prefix):
    opening = None
    cohort = set()
    first = {}
    for r in rows(str(prefix) + '-programme-choices.csv'):
        week = int(r['week'])
        if opening is None:
            opening = week
        if week != opening:
            break
        if r['format'] == 'Single' and r['eligible'] == '1' and r['careerState'] in ('Established', 'Star', 'Superstar'):
            cohort.add(r['artistId'])
            first.setdefault(r['artistId'], r)
    return cohort, first


def trace(prefix, cohort, opening):
    items = {}
    pressures = set()
    end = 0
    for r in rows(str(prefix) + '-release-slate.csv'):
        week = int(r['week'])
        end = max(end, week)
        if r['artistId'] not in cohort or r['format'] != 'Single':
            continue
        item = items.setdefault(r['slotId'], dict(artistId=r['artistId'], slotId=r['slotId'],
            proposedWeek=int(r['proposedWeek']), intendedWeek=int(r['intendedWeek']),
            source=r['source'], preparedWeek=None, actualWeek=None, reasons=set()))
        item.update(state=r['state'], finalReason=r['reason'], lastReviewWeek=week)
        item['recordId'] = r.get('recordId', '')
        if r['preparedWeek']:
            item['preparedWeek'] = int(r['preparedWeek'])
        if r['state'] == 'Issued':
            item['actualWeek'] = int(r['actualWeek'])
        if r['state'] == 'Postponed':
            item['reasons'].add(r['reason'])
            pressures.add((r['slotId'], week, r['reason']))
    issued = [i for i in items.values() if i['actualWeek'] is not None]
    due = [i for i in items.values() if i['intendedWeek'] <= end]
    pressure_counts = Counter(reason for _, _, reason in pressures)
    mean = lambda values: statistics.mean(values) if values else None
    summary = dict(acts=len(cohort), intentions=len(items), intentionsDueByEnd=len(due),
        states=dict(Counter(i['state'] for i in items.values())),
        dueStates=dict(Counter(i['state'] for i in due)),
        issuedMeanPlanLead=mean([i['intendedWeek']-i['proposedWeek'] for i in issued]),
        issuedMeanPreparationLateness=mean([max(0, i['preparedWeek']-(i['intendedWeek']-1)) for i in issued if i['preparedWeek'] is not None]),
        issuedMeanReadyToIssueExcess=mean([max(0, i['actualWeek']-i['preparedWeek']-1) for i in issued if i['preparedWeek'] is not None]),
        postponementItemWeeks=dict(pressure_counts.most_common()))
    refusals = Path(str(prefix) + '-programme-issue-refusals.csv')
    if refusals.exists():
        gates = {(r['slotId'],r['week'],r['gate']) for r in rows(refusals) if r['slotId'] in items}
        summary['issueRefusalItemWeeks'] = dict(Counter(gate for _,_,gate in gates).most_common())
    projects = Path(str(prefix) + '-programme-projects.csv')
    if projects.exists():
        reviews = {(r['slotId'],r['week'],r['action']) for r in rows(projects) if r['slotId'] in items}
        summary['materialReviewItemWeeks'] = dict(Counter(action for _,_,action in reviews).most_common())
    summary['cancelledPreparedMasters'] = sum(i['state']=='Cancelled' and i['preparedWeek'] is not None for i in items.values())
    act_traces = []
    covered = []
    launch_file = Path(str(prefix) + '-single-launch-quality.csv')
    launches = {act: [] for act in cohort}
    if launch_file.exists() and opening:
        offset = int(next(iter(opening.values()))['week']) - 1
        for r in rows(launch_file):
            if r['artistId'] in cohort:
                launches[r['artistId']].append(dict(pipelineWeek=int(r['week'])+offset,
                    auditWeek=int(r['week']), recordId=r['singleId'], strategy=r['strategy'],
                    gap=int(r['weeksSinceLastSingle']) if r.get('weeksSinceLastSingle') else None))
    summary['releaseObservation'] = 'all single launch routes' if launch_file.exists() else 'single intentions only (launch log unavailable)'
    for act in sorted(cohort):
        intentions = sorted((i for i in items.values() if i['artistId'] == act), key=lambda i: (i['proposedWeek'], i['intendedWeek'], i['slotId']))
        initial = opening[act]
        clock = int(initial['intendedWeek']) - int(initial['gap'])
        start = int(initial['week'])
        due_week = max(start, clock + 13)
        follow_ups = ([i['pipelineWeek'] for i in launches[act] if i['pipelineWeek'] >= start] if launch_file.exists() else
                      [i['actualWeek'] for i in intentions if i['actualWeek'] is not None and i['actualWeek'] >= start])
        if end >= due_week + 52:
            first_issue = min(follow_ups, default=None)
            covered.append(dict(artistId=act, issued52=first_issue is not None and first_issue <= due_week + 52,
                                waitCapped52=max(0, min(52, first_issue-due_week)) if first_issue is not None else 52))
        act_traces.append(dict(artistId=act, firstEligibleReviewWeek=int(initial['week']),
            firstEligibleIntendedWeek=int(initial['intendedWeek']), firstAuditedGap=int(initial['gap']),
            openingSingleClockWeek=clock, spacingEligibleWeek=max(start, clock + 6),
            quarterlyDueWeek=due_week, clockLeftCensored=clock + 6 < start,
            launches=launches[act],
            intentions=[{**i, 'reasons': sorted(i['reasons'])} for i in intentions]))
    summary['openingDue52'] = dict(fullyObservedActs=len(covered),
        issued52=sum(i['issued52'] for i in covered), noIssue52=sum(not i['issued52'] for i in covered),
        meanWaitCapped52=mean([i['waitCapped52'] for i in covered]))
    observed_gaps = [r['gap'] for records in launches.values() for r in records if r['gap'] is not None and r['gap'] >= 0]
    summary['actualRepeatSingleGaps'] = dict(n=len(observed_gaps), mean=mean(observed_gaps),
        median=statistics.median(observed_gaps) if observed_gaps else None)
    return summary, act_traces


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('base')
    parser.add_argument('treatment')
    parser.add_argument('--out', required=True)
    args = parser.parse_args()
    a, ao = opening_cohort(args.base)
    b, bo = opening_cohort(args.treatment)
    cohort = a & b
    report = dict(cohortDefinition='Same artist IDs observed Established/Star/Superstar and eligible for a single in both opening planning reviews; eligibility is observed/forecast, not inferred between reviews.',
                  baseOpeningActs=len(a), treatmentOpeningActs=len(b), matchedActs=len(cohort), arms={})
    for name, prefix, first in [('base', args.base, ao), ('treatment', args.treatment, bo)]:
        summary, traces = trace(prefix, cohort, first)
        report['arms'][name] = dict(summary=summary, acts=traces)
        print(name, json.dumps(summary, indent=2))
    Path(args.out).write_text(json.dumps(report, indent=2), encoding='utf-8')


if __name__ == '__main__':
    main()
