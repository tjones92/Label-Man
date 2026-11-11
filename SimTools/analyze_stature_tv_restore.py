"""Verify a single-allocation TV intervention and locate the next economic differences."""
import argparse
import json
from collections import Counter, defaultdict
from itertools import zip_longest
from pathlib import Path

from analyze_national_stature_marketing import same_money
from analyze_stature_media_bookings import TRACE_FLAG, compare_through, verified_run
from compare_stature_campaign_share import read_json
from stature_normalization import file_digest


OVERRIDE = '--audit-tv-allocation-override='
PROVENANCE = ('assemblySha256', 'sourceContentSha256', 'sourceRevision', 'engineSha256',
              'calibrationSha256', 'referenceRequestSha256', 'gcEnvironment')
WEEKLY = ('market-clearing-weekly', 'media-demand', 'records', 'single-demand-stages',
          'release-profit-gates', 'release-campaign-costs', 'release-slate')


def by_week(read, suffix, weeks):
    return (r for r in read(suffix) if int(r['week']) <= weeks)


def first_difference(old, new, fields=None):
    for index, (a, b) in enumerate(zip_longest(old, new), 1):
        if a is None or b is None or any(a[k] != b[k] for k in (fields or a.keys())):
            return dict(index=index, original=a, candidate=b)
    return None


def weekly_spend(entries):
    totals = defaultdict(float)
    for r in entries:
        totals[int(r['week'])] += float(r['marketingCost'])
    return totals


def issued_slots(entries):
    weeks = defaultdict(Counter)
    for r in entries:
        if r['state'] == 'Issued':
            key = tuple(r[field] for field in ('slotId', 'labelId', 'artistId', 'format'))
            weeks[int(r['actualWeek'])][key] += 1
    return weeks


def economics(original, candidate, weeks):
    def pair(suffix):
        return (by_week(read, suffix, weeks) for read in (original, candidate))

    spend_a, spend_b = (weekly_spend(entries) for entries in pair('release-campaign-costs'))
    slots_a, slots_b = (issued_slots(entries) for entries in pair('release-slate'))
    return dict(
        firstMarketDifference=first_difference(*pair('market-clearing-weekly')),
        firstMediaDemandDifference=first_difference(*pair('media-demand')),
        firstProfitGateChange=first_difference(*pair('release-profit-gates'),
                                               ('week', 'labelId', 'artistId', 'strategy', 'commit')),
        firstWeeklySpendDifference=next((dict(week=w, original=spend_a[w], candidate=spend_b[w])
                                        for w in sorted(spend_a.keys() | spend_b.keys())
                                        if not same_money(spend_a[w], spend_b[w])), None),
        firstIssuedSlotDifference=next((dict(week=w, originalOnly=sorted((slots_a[w] - slots_b[w]).elements()),
                                            candidateOnly=sorted((slots_b[w] - slots_a[w]).elements()))
                                       for w in sorted(slots_a.keys() | slots_b.keys()) if slots_a[w] != slots_b[w]), None),
    )


def analyze(directory, archive, booking_reference, baseline, restored, weeks):
    archive_request, original_read, original_hashes = verified_run(directory, f'{archive}-control', 104)
    previous_request, previous_read, previous_hashes = verified_run(directory, f'{archive}-treatment', 104)
    baseline_request, baseline_read, baseline_hashes = verified_run(directory, baseline, weeks)
    restored_request, restored_read, restored_hashes = verified_run(directory, restored, weeks)

    def run_flags(request):
        return [f for f in request['effectiveArguments'] if not f.startswith('--run=')]

    baseline_flags = run_flags(baseline_request)
    old_flags = [f'--weeks={weeks}' if f == '--weeks=104' else f for f in run_flags(previous_request)]
    if baseline_flags != old_flags + [TRACE_FLAG]:
        raise ValueError('Baseline flags differ beyond name, horizon and audit observation')
    restored_flags = run_flags(restored_request)
    flag, = (f for f in restored_flags if f.startswith(OVERRIDE))
    if restored_flags != baseline_flags + [flag] or any(baseline_request[k] != restored_request[k] for k in PROVENANCE):
        raise ValueError('Intervention differs from the baseline beyond the TV override')
    if any(baseline_request[k] != previous_request[k] for k in ('engineSha256', 'calibrationSha256',
                                                               'referenceRequestSha256', 'gcEnvironment')):
        raise ValueError('Baseline archive provenance differs')
    date, episode, record, label, artist, expected, replacement = flag.removeprefix(OVERRIDE).split(',')
    control_flags = [f for f in run_flags(archive_request) if not f.startswith('--stature-calibration=')]
    previous_flags = [f for f in run_flags(previous_request) if not f.startswith('--stature-calibration=')]
    if control_flags != previous_flags or any(archive_request[k] != previous_request[k] for k in PROVENANCE
                                               if k != 'calibrationSha256'):
        raise ValueError('Archived worlds do not form a matched calibration pair')

    verified = {s: compare_through(previous_read(s), baseline_read(s), lambda r: int(r['week']), weeks) for s in WEEKLY}
    booking_report = read_json(booking_reference)
    cutoff = booking_report['tallyThrough']
    archived_arms = booking_report['arms']
    for arm, hashes in (('control', original_hashes), ('treatment', previous_hashes)):
        if archived_arms[arm]['archive'] != hashes:
            raise ValueError('Booking report comes from a different archive')
    original, = (r for r in archived_arms['control']['allocations'] if r['recordId'] == record)
    if original['tvCents'] != replacement:
        raise ValueError('Intervention does not restore the original TV allocation')
    allocation = {'original': original}
    deposits = {'original': archived_arms['control']['deposits']}
    targets = {r['recordId'] for r in deposits['original']}
    for arm, read in (('baseline', baseline_read), ('restored', restored_read)):
        item, = (r for r in read('media-allocations')
                 if (r['decisionDate'], r['episodeId'], r['recordId'], r['labelId'], r['artistId']) ==
                 (date, episode, record, label, artist))
        allocation[arm] = item
        deposits[arm] = [r for r in read('media-demand-deposits')
                         if r['tallyThrough'] == cutoff and r['regionId'] == 'southwest' and r['recordId'] in targets]
    if deposits['baseline'] != archived_arms['treatment']['deposits']:
        raise ValueError('Baseline deposits do not reproduce the prior booking replay')
    if (allocation['baseline']['tvCents'], allocation['restored']['tvCents']) != (expected, replacement):
        raise ValueError('Allocation CSVs do not record the specified intervention')
    if {k for k in allocation['baseline'] if allocation['baseline'][k] != allocation['restored'][k]} != {'tvCents'}:
        raise ValueError('The intervened allocation changed beyond its TV line')
    log = directory / 'writing-calibration' / f'{restored}.log'
    marker, = (line for line in log.read_text(encoding='utf-8-sig').splitlines()
               if line.startswith('AUDIT_TV_ALLOCATION_OVERRIDE '))
    return dict(analysisSha256=file_digest(Path(__file__)), bookingReferenceSha256=file_digest(booking_reference),
                weeks=weeks, override=flag, marker=marker,
                runs=dict(archiveControl=original_hashes, archiveTreatment=previous_hashes,
                          baseline=baseline_hashes, restored=restored_hashes), baselineMatchesArchive=verified,
                allocations=allocation, week53SouthwestDeposits=deposits,
                originalVsBaseline=economics(original_read, baseline_read, weeks),
                originalVsRestored=economics(original_read, restored_read, weeks),
                baselineVsRestored=economics(baseline_read, restored_read, weeks))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--directory', type=Path, default=Path('SimLogs'))
    parser.add_argument('--archive', default='stature-campaign-share-104w-v1-legacy-2002')
    parser.add_argument('--booking-reference', type=Path, required=True)
    parser.add_argument('--baseline', required=True)
    parser.add_argument('--restored', required=True)
    parser.add_argument('--weeks', type=int, default=60)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    if args.output.exists():
        raise FileExistsError(args.output)
    result = analyze(args.directory, args.archive, args.booking_reference, args.baseline, args.restored, args.weeks)
    with args.output.open('x', encoding='utf-8') as stream:
        json.dump(result, stream, indent=2, sort_keys=True)
        stream.write('\n')


if __name__ == '__main__':
    main()
