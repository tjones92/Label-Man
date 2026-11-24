"""Attach within-arm payment, release and record-response evidence to the audit."""
import argparse
import datetime as dt
import json
from collections import Counter, defaultdict
from pathlib import Path

from trace_stature_marketing_mechanisms import rows, initial, charts
from stature_normalization import file_digest, parse_date


def enrich(source, output, directory):
    if output.exists():
        raise FileExistsError(output)
    report = json.loads(source.read_text())
    report['parentReportSha256'] = file_digest(source)
    report['enrichmentSha256'] = file_digest(Path(__file__))
    for seed, data in report['seeds'].items():
        for arm, world in data['arms'].items():
            name = f"{report['sourcePrefix']}-{seed}-{arm}"
            acts = world['acts']
            initial_following = {}
            for r in rows(directory, name, 'following'):
                if r['date'] != '1/1/1960':
                    break
                if r['kind'] == 'act':
                    initial_following[r['artistId']] = r
            seeded_chart_response = defaultdict(Counter)
            seeded_budget_reads = defaultdict(Counter)
            fixed_chart = charts(directory, name)[0]
            opening = set(initial(directory, name))
            fixed_budgets = defaultdict(Counter)
            for r in rows(directory, name, 'stature-marketing'):
                if r['context'] != 'release-launch':
                    continue
                group = 'firstObservedOpeningFollowing' if r['artistId'] in initial_following else 'other'
                c = seeded_budget_reads[parse_date(r['date']).year, group]
                old, proposed = float(r['legacyBudget']), float(r['proposedBudget'])
                c['reads'] += 1; c['uplifts'] += proposed > old; c['cuts'] += proposed < old
                group = 'openingWeekZeroChart' if r['artistId'] in fixed_chart else 'otherOpening' if r['artistId'] in opening else 'runtime'
                c = fixed_budgets[parse_date(r['date']).year, group]
                c['reads'] += 1; c['uplifts'] += proposed > old; c['cuts'] += proposed < old
                c['absoluteFullChange'] += abs(proposed - old)
                c['absoluteHalfChange'] += abs(.5 * (proposed - old))
            identity = {r['recordId']: r['artistId'] for r in rows(directory, name, 'record-acts')}
            for r in rows(directory, name, 'single-launch-quality'):
                identity[r['singleId']] = r['artistId']
            for r in rows(directory, name, 'release-slate'):
                if r['recordId'] and r['artistId']:
                    identity[r['recordId']] = r['artistId']
            reads_by_date = defaultdict(list)
            for artist, act in acts.items():
                act.update(paidLaunches=[], paidFollowUps=[], issued=[], prewarmRecords=[], recordResponse={})
                for annual in act['annual'].values():
                    for key in ('launchMarketingPaid', 'launchCostEvents', 'followUpPaid'):
                        annual.pop(key, None)
                for r in act['reads']:
                    reads_by_date[artist, parse_date(r['date']).isoformat()].append(r)
            for r in rows(directory, name, 'records'):
                if 1 <= int(r['currentPosition']) <= 10:
                    group = 'firstObservedOpeningFollowing' if r['artistId'] in initial_following else 'other'
                    seeded_chart_response[int(r['year']), group]['top10SlotWeeks'] += 1
                if r['artistId'] not in acts:
                    continue
                act = acts[r['artistId']]
                if r['week'] == '0':
                    act['prewarmRecords'].append(r)
                response = act['recordResponse'].setdefault(r['recordId'], {'title': r['title'], 'years': {}})
                annual = response['years'].setdefault(r['year'], {'chartedUnits': 0, 'top10SlotWeeks': 0, 'hot100SlotWeeks': 0})
                annual['chartedUnits'] += float(r['unitsThisWeek'])
                annual['top10SlotWeeks'] += 1 <= int(r['currentPosition']) <= 10
                annual['hot100SlotWeeks'] += 1 <= int(r['currentPosition']) <= 100
            for suffix, field in (('release-campaign-costs', 'paidLaunches'), ('record-campaigns', 'paidFollowUps')):
                for r in rows(directory, name, suffix):
                    artist = r.get('artistId') or identity.get(r['recordId'])
                    if artist not in acts:
                        continue
                    date = (dt.date(1960, 1, 1) + dt.timedelta(weeks=int(r['week']) - 1)).isoformat()
                    event = dict(r, inferredDecisionDate=date)
                    event['sameDayBudgetReads'] = [{k: read[k] for k in ('context', 'legacyBudget', 'proposedBudget', 'requestedBudget', 'snapshotDate')}
                        for read in reads_by_date.get((artist, date), [])]
                    # These are candidate reads, not a unique event-id join. Forecasts
                    # and multiple launches can coexist on a decision date.
                    acts[artist][field].append(event)
                    annual = acts[artist]['annual'].setdefault(date[:4], {})
                    key = 'launchMarketingPaid' if field == 'paidLaunches' else 'followUpPaid'
                    annual[key] = annual.get(key, 0) + float(r['marketingCost'] if field == 'paidLaunches' else r['spend'])
                    if field == 'paidLaunches':
                        annual['launchCostEvents'] = annual.get('launchCostEvents', 0) + 1
            for r in rows(directory, name, 'release-slate'):
                if r['artistId'] in acts and r['state'] == 'Issued':
                    acts[r['artistId']]['issued'].append(r)
            buckets = defaultdict(Counter)
            for r in rows(directory, name, 'following'):
                if r['kind'] == 'act' and r['date'] == '1/1/1960':
                    buckets['initialLoggedFollowing']['acts'] += 1
                    buckets['initialLoggedFollowing']['young'] += float(r['young'])
                    buckets['initialLoggedFollowing']['adult'] += float(r['adult'])
            world['openingFollowingLogTotals'] = dict(buckets)
            world['openingFollowingCohortChart'] = [{'year': y, 'group': g, **c} for (y, g), c in sorted(seeded_chart_response.items())]
            world['openingFollowingCohortSingleLaunchBudgets'] = [{'year': y, 'group': g, **c} for (y, g), c in sorted(seeded_budget_reads.items())]
            world['singleLaunchBudgets'] = [{'year': y, 'group': g, **c} for (y, g), c in sorted(fixed_budgets.items())]
        for artist in data['arms']['control']['acts']:
            a = data['arms']['control']['acts'][artist]
            b = data['arms']['treatment']['acts'][artist]
            if a['prewarmRecords'] != b['prewarmRecords']:
                raise ValueError('Selected act prewarm records differ')
            dates = defaultdict(lambda: {'control': [], 'treatment': []})
            for arm, act in (('control', a), ('treatment', b)):
                for r in act['reads']:
                    # The applied amount intentionally differs under treatment.
                    # Compare the decision opportunities and their proposed inputs.
                    dates[parse_date(r['date']).isoformat()][arm].append(tuple(r[k] for k in
                        ('labelId', 'context', 'legacyCareer', 'standingCareer', 'legacyBudget', 'proposedBudget')))
            first = next((date for date, arms in sorted(dates.items()) if sorted(arms['control']) != sorted(arms['treatment'])), None)
            a['firstObservedDecisionInputOrOpportunityDivergence'] = b['firstObservedDecisionInputOrOpportunityDivergence'] = first
    report['caveats'].extend(['Payment-to-record identity joins are within-arm. Same-day budget reads are candidate links, not unique decision-event joins.',
        'The first following act log is emitted by FollowingWeek, after the first completed live week. Jan 1 stocks can include first-week joins and losses; neither these totals nor this cohort identify pure synthetic seeded followers. The log floor also excludes small stocks.',
        'First observed divergence compares recorded opportunities and inputs; it does not locate the first causal state or RNG divergence.'])
    with output.open('x', encoding='utf-8') as f:
        json.dump(report, f, indent=2)
        f.write('\n')


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('source', type=Path)
    p.add_argument('output', type=Path)
    p.add_argument('--directory', type=Path, default=Path('SimLogs'))
    a = p.parse_args()
    enrich(a.source, a.output, a.directory)
