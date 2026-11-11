"""Join a verified title-level TV divergence to allocations, bookings and airings."""
import argparse
import json
from datetime import date, timedelta
from itertools import zip_longest
from pathlib import Path

from compare_stature_campaign_share import ARMS, read_json
from compress_stature_outputs import csv_digest
from stature_normalization import file_digest, parse_date
from summarize_stature_marketing_trial import rows


TRACE_FLAG = '--observe-media-demand-bookings'


def verified_run(directory, name, weeks):
    metadata = directory / 'writing-calibration'
    log = (metadata / f'{name}.log').read_text(encoding='utf-8-sig').splitlines()
    if log.count(f'CHART_AUDIT_COMPLETE run={name} weeks={weeks}') != 1:
        raise ValueError(f'Missing completion marker for {name}')
    if (metadata / f'{name}-errors.log').stat().st_size:
        raise ValueError(f'Nonempty error log for {name}')
    manifest = metadata / f'{name}-outputs.json'
    hashes = {r['name']: r['sha256'].upper() for r in read_json(manifest)}

    def verified_rows(suffix):
        path = directory / f'{name}-{suffix}.csv'
        if csv_digest(path).upper() != hashes[path.name]:
            raise ValueError(f'Output differs from manifest: {path.name}')
        return rows(path)

    request = metadata / f'{name}-request.json'
    return read_json(request), verified_rows, dict(requestSha256=file_digest(request), manifestSha256=file_digest(manifest))


def compare_through(old, new, key, last):
    paired = zip_longest((row for row in old if key(row) <= last), (row for row in new if key(row) <= last))
    count = 0
    for first, second in paired:
        if first != second:
            raise ValueError(f'Replay differs on row {count + 1}: {first} != {second}')
        count += 1
    return count


def analyze(directory, reference, replay_prefix, replay_weeks):
    seed = reference['seed']
    week = reference['firstLoggedMediaWeek']
    cutoff = date.fromisoformat(reference['auditWeekStart']) - timedelta(days=7)
    market_week = reference['firstMarketDifference']['week']
    if replay_weeks < market_week:
        raise ValueError('Replay must include the first market difference')
    result = {}
    for arm in ARMS:
        archive_name = f"{reference['prefix']}-{seed}-{arm}"
        archive, old, old_hashes = verified_run(directory, archive_name, 104)
        replay_name = f'{replay_prefix}-{seed}-{arm}'
        replay, new, new_hashes = verified_run(directory, replay_name, replay_weeks)
        for field in ('engineSha256', 'calibrationSha256', 'referenceRequestSha256', 'gcEnvironment'):
            if archive[field] != replay[field]:
                raise ValueError(f'Replay provenance differs in {field}')
        expected = [f'--weeks={replay_weeks}' if flag == '--weeks=104'
                    else f'--run={replay_name}' if flag == f'--run={archive_name}' else flag
                    for flag in archive['effectiveArguments']]
        if replay['effectiveArguments'] != expected + [TRACE_FLAG]:
            raise ValueError('Replay flags differ beyond the run name, horizon and audit flag')
        checked = {suffix: compare_through(old(suffix), new(suffix), lambda row: int(row['week']), market_week)
                   for suffix in ('market-clearing-weekly', 'media-demand', 'single-demand-stages', 'records')}
        checked['stature-marketing'] = compare_through(old('stature-marketing'), new('stature-marketing'),
                                                      lambda row: parse_date(row['date']), cutoff)

        changes = reference['arms'][arm]['changedInputs']
        targets = {(r['media']['recordId'], r['media']['regionId']): r['media'] for r in changes}
        prior = {(r['recordId'], r['regionId']): r for r in reference['previousMediaRows']}
        deposits = [r for r in new('media-demand-deposits')
                    if r['tallyThrough'] == cutoff.isoformat() and (r['recordId'], r['regionId']) in targets]
        for key, target in targets.items():
            if prior[key]['week'] != str(week - 1) or prior[key]['role'] not in ('NeutralDonor', 'NeutralRecipient'):
                raise ValueError(f'Cannot reconstruct {key} from fresh deposits alone')
            total = sum(float(r['request']) for r in deposits if (r['recordId'], r['regionId']) == key)
            if abs(total - float(target['request'])) > 0.00000501:
                raise ValueError(f'Deposits do not reconstruct the logged request for {key}')

        title_ids = {record for record, _ in targets}
        allocations = [r for r in new('media-allocations')
                       if r['recordId'] in title_ids and date.fromisoformat(r['decisionDate']) <= cutoff]
        attempts = [r for r in new('media-booking-attempts')
                    if r['recordId'] in title_ids and date.fromisoformat(r['decisionDate']) <= cutoff]
        result[arm] = dict(archive=old_hashes, replay=new_hashes, verifiedRows=checked,
                           deposits=deposits, allocations=allocations, bookingAttempts=attempts)

    return dict(seed=seed, firstLoggedMediaWeek=week, tallyThrough=cutoff.isoformat(),
                archiveComparisonThroughWeek=market_week, analysisSha256=file_digest(Path(__file__)), arms=result)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--directory', type=Path, default=Path('SimLogs'))
    parser.add_argument('--reference', type=Path, required=True)
    parser.add_argument('--replay-prefix', required=True)
    parser.add_argument('--weeks', type=int, default=55)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    if args.output.exists():
        raise FileExistsError(args.output)
    result = analyze(args.directory, read_json(args.reference), args.replay_prefix, args.weeks)
    result['referenceSha256'] = file_digest(args.reference)
    with args.output.open('x', encoding='utf-8') as stream:
        json.dump(result, stream, indent=2, sort_keys=True)
        stream.write('\n')


if __name__ == '__main__':
    main()
