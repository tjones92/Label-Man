"""Compare standing, marketing requests and payments in a verified calibration pair."""
import argparse
import json
import math
from collections import Counter, defaultdict
from datetime import date, timedelta
from pathlib import Path

from compress_stature_outputs import csv_digest
from stature_normalization import file_digest, parse_date
from summarize_stature_marketing_trial import money, rows


def same_money(left, right):
    return math.isclose(left, right, rel_tol=2e-6, abs_tol=.001)


def load_arm(directory, name, summary):
    metadata = directory / 'writing-calibration'
    manifest_path = metadata / f'{name}-outputs.json'
    if file_digest(manifest_path) != summary['sha256'][manifest_path.name]:
        raise ValueError('Output manifest differs from verified summary')
    manifest = {entry['name']: entry['sha256'].upper()
                for entry in json.loads(manifest_path.read_text(encoding='utf-8-sig'))}

    def verified_rows(suffix):
        path = directory / f'{name}-{suffix}.csv'
        if csv_digest(path).upper() != manifest[path.name]:
            raise ValueError(f'Output differs from manifest: {path.name}')
        return rows(path)

    request_path = metadata / f'{name}-request.json'
    if file_digest(request_path) != summary['sha256'][request_path.name]:
        raise ValueError('Request differs from verified summary')
    request = json.loads(request_path.read_text(encoding='utf-8-sig'))
    duration, = (int(flag.split('=', 1)[1]) for flag in request['flags'] if flag.startswith('--weeks='))
    market_weeks = set()
    for row in verified_rows('market-clearing-weekly'):
        market_weeks.add(int(row['week']))
    if market_weeks != set(range(1, duration + 1)):
        raise ValueError('Missing or nonconsecutive chart weeks')
    log_path = metadata / f'{name}.log'
    if file_digest(log_path) != summary['sha256'][log_path.name]:
        raise ValueError('Log differs from verified summary')
    start_marker = f'CHART_AUDIT_STARTED run={name} weeks={duration} date='
    start, = (parse_date(line.removeprefix(start_marker)) for line in
              log_path.read_text(encoding='utf-8-sig').splitlines() if line.startswith(start_marker))
    weeks = {week: start + timedelta(weeks=week - 1) for week in range(1, duration + 1)}
    annual = defaultdict(Counter)
    week_costs = Counter()
    costs = {}
    for row in verified_rows('release-campaign-costs'):
        record, week = row['recordId'], int(row['week'])
        if record in costs:
            raise ValueError(f'Duplicate launch payment: {record}')
        amount = money(row, 'marketingCost')
        costs[record] = (week, row['labelId'], amount)
        annual[str(weeks[week].year)]['launchPayments'] += 1
        annual[str(weeks[week].year)]['launchMarketingPaid'] += amount
        week_costs[week] += amount
    for row in verified_rows('record-campaigns'):
        annual[str(weeks[int(row['week'])].year)]['followUpPaid'] += money(row, 'spend')

    reads_by_context = defaultdict(list)
    for row in verified_rows('stature-marketing'):
        if row['context'] in {'release-launch', 'programme-campaign-share'}:
            key = (row['context'], row['date'], row['labelId'], row['artistId'])
            reads_by_context[key].append(row)

    issued_rows = [row for row in verified_rows('release-slate') if row['state'] == 'Issued']
    issues_per_act_day = Counter((row['actualDate'], row['labelId'], row['artistId']) for row in issued_rows)
    issues = {}
    for row in issued_rows:
        week = int(row['actualWeek'])
        if parse_date(row['actualDate']) != weeks[week]:
            raise ValueError('Slate issue date differs from audit week')
        format_ = row['format']
        annual[str(weeks[week].year)][format_ + 'SlateIssues'] += 1
        record, label, artist = row['recordId'], row['labelId'], row['artistId']
        key = (week, format_, row['slotId'], label, artist)
        if key in issues:
            raise ValueError(f'Duplicate slate issue: {key}')
        paid_week, paid_label, amount = costs.get(record, (None, None, None))
        paid = amount if (week, label) == (paid_week, paid_label) else None
        act_day = (row['actualDate'], label, artist)
        reads = reads_by_context['release-launch', *act_day] if format_ == 'Single' else []
        share_reads = reads_by_context['programme-campaign-share', *act_day]
        envelope = (money(share_reads[0], 'requestedBudget')
                    if len(share_reads) == 1 and issues_per_act_day[act_day] == 1 else None)
        issues[key] = dict(recordId=record, date=row['actualDate'], paid=paid,
                           reserve=money(row, 'reservedMarketing'), launchReads=len(reads),
                           request=money(reads[0], 'requestedBudget') if len(reads) == 1 else None,
                           standing=reads[0]['standingCareer'] if len(reads) == 1 else None,
                           campaignEnvelope=envelope,
                           impliedCampaignShare=min(1, paid / max(1, envelope))
                           if paid is not None and envelope is not None else None)

    last_date = None
    candidate = name.endswith('-treatment')
    for row in verified_rows('stature-shadow'):
        stamp = row['snapshotDate']
        if not stamp:
            continue
        if stamp != last_date:
            if last_date and date.fromisoformat(stamp) - date.fromisoformat(last_date) != timedelta(days=7):
                raise ValueError('Nonconsecutive standing observations')
            annual[stamp[:4]]['standingWeeks'] += 1
            last_date = stamp
        counts = annual[stamp[:4]]
        counts['standingObservations'] += 1
        band = row['shadowBand']
        if row['status'] == 'MissingEvidence' or band not in {'Star', 'Superstar'}:
            continue
        counts[band] += 1
        if candidate and (float(row[band + 'Regional']) != 0 or
                          max(float(row[band + 'Record']), float(row[band + 'Conversion'])) <
                          float(row[band + 'Boundary'])):
            raise ValueError('Candidate high tier lacks national support')

    rollup = summary['arms']['treatment' if candidate else 'control']['annualRollup']
    for row in rollup:
        annual[row['year']].update({field: int(row[field]) for field in ('singleUnits', 'albumUnits')})
    return weeks, annual, issues, week_costs


def compare_issues(weeks, control, treatment):
    by_format, changed = defaultdict(lambda: defaultdict(Counter)), []
    for key in sorted(control.keys() | treatment.keys()):
        week, format_, slot, label, artist = key
        counts = by_format[format_][str(weeks[week].year)]
        old, new = control.get(key), treatment.get(key)
        if old is None or new is None:
            counts['treatmentOnly' if old is None else 'controlOnly'] += 1
            continue
        counts['matchedIssues'] += 1
        paired_payment = old['paid'] is not None and new['paid'] is not None
        paid_changed = paired_payment and not same_money(old['paid'], new['paid'])
        counts['pairedPayments'] += paired_payment
        counts['paidChanged'] += paid_changed
        request_changed = False
        if format_ == 'Single':
            if old['request'] is None or new['request'] is None:
                counts['unmatchedOrAmbiguousRequest'] += 1
            else:
                counts['pairedRequests'] += 1
                request_changed = not same_money(old['request'], new['request'])
                counts['lowerRequests'] += request_changed and new['request'] < old['request']
                counts['higherRequests'] += request_changed and new['request'] > old['request']
                counts['changedRequestsWithChangedPayment'] += request_changed and paid_changed
        before_share, after_share = old['impliedCampaignShare'], new['impliedCampaignShare']
        paired_share = before_share is not None and after_share is not None
        share_changed = paired_share and not math.isclose(before_share, after_share, abs_tol=1e-6)
        counts['pairedCampaignShares'] += paired_share
        counts['changedShareAtSamePayment'] += share_changed and not paid_changed
        reserve_changed = not same_money(old['reserve'], new['reserve'])
        counts['reserveChanged'] += reserve_changed
        if request_changed or paid_changed or reserve_changed or share_changed:
            changed.append(dict(week=week, format=format_, slotId=slot, labelId=label, artistId=artist,
                                requestChanged=request_changed, reserveChanged=reserve_changed,
                                paymentChanged=paid_changed, campaignShareChanged=share_changed,
                                control=old, treatment=new))
    return by_format, changed


def analyze(directory, prefix, seed):
    summary_path = directory / 'writing-calibration' / f'{prefix}-{seed}-summary.json'
    summary = json.loads(summary_path.read_text())
    if summary['seed'] != seed or 'comparison' not in summary:
        raise ValueError('Expected a verified national calibration pair')
    control, treatment = (load_arm(directory, f'{prefix}-{seed}-{arm}', summary)
                          for arm in ('control', 'treatment'))
    weeks, control_annual, control_issues, control_costs = control
    treatment_weeks, treatment_annual, treatment_issues, treatment_costs = treatment
    if weeks != treatment_weeks:
        raise ValueError('Paired chart weeks differ')
    matches, changes = compare_issues(weeks, control_issues, treatment_issues)
    first_cost_change = next((dict(week=w, date=weeks[w].isoformat(), control=control_costs[w], treatment=treatment_costs[w])
                              for w in sorted(weeks) if not same_money(control_costs[w], treatment_costs[w])), None)
    return dict(summarySha256=file_digest(summary_path), weeks=len(weeks),
                firstRequestedDifference=summary['firstRequestedDifference'],
                firstWeeklyLaunchSpendDifference=first_cost_change,
                annual=dict(control=control_annual, treatment=treatment_annual),
                issueMatchesByFormatAndYear=matches, changedMatchedIssues=changes)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--directory', type=Path, default=Path('SimLogs'))
    parser.add_argument('--prefix', required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    if args.output.exists():
        raise FileExistsError(args.output)
    report = dict(analysisSha256=file_digest(Path(__file__)),
                  scope='Paired worlds with the marketing reader live. Slate issues match on week, format, slot, label and act. '
                        'A single issue links to a launch request with a unique same-day act and label read. '
                        'Campaign share requires a same-week payment and a unique share read and issue for that act and label that day. '
                        'Issues and payments use the calendar year of their audit week start. Market units use the engine annual rollup. '
                        'Aligned issue differences after the worlds diverge are descriptive. '
                        'Launch payments and follow-up payments are separate streams.',
                  seeds={seed: analyze(args.directory, args.prefix, seed) for seed in (1001, 2002)})
    with args.output.open('x', encoding='utf-8') as stream:
        json.dump(report, stream, indent=2, sort_keys=True)
        stream.write('\n')


if __name__ == '__main__':
    main()
