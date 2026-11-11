"""Trace the first logged media-demand difference in a retained calibration pair."""
import argparse
import json
from collections import Counter
from datetime import timedelta
from itertools import groupby, zip_longest
from pathlib import Path

from compare_stature_campaign_share import ARMS, FLAG, first_market_difference, read_json
from compress_stature_outputs import csv_digest
from stature_normalization import file_digest, parse_date
from summarize_stature_marketing_trial import rows


def index(entries, *fields):
    result = {}
    for row in entries:
        key = tuple(row[field] for field in fields)
        if key in result:
            raise ValueError(f'Duplicate row for {key}')
        result[key] = row
    return result


def first_media_week(control, treatment):
    def by_week(entries):
        for week, group in groupby(entries, key=lambda row: int(row['week'])):
            yield week, index(group, 'regionId', 'recordId')

    previous = {}
    for old, new in zip_longest(by_week(control), by_week(treatment)):
        if old is None or new is None or old[0] != new[0]:
            raise ValueError('Media export weeks diverged before an aligned comparison')
        if old[1] != new[1]:
            return old[0], previous, old[1], new[1]
        previous = old[1]
    raise ValueError('No logged media-demand difference')


def trace(directory, prefix, seed):
    metadata = directory / 'writing-calibration'
    summary_path = metadata / f'{prefix}-{seed}-summary.json'
    summary = read_json(summary_path)
    if summary['seed'] != seed or 'comparison' not in summary:
        raise ValueError('Expected a verified calibration pair')
    verified, starts = {}, set()
    for arm in ARMS:
        name = f'{prefix}-{seed}-{arm}'
        request, manifest = (metadata / f'{name}-{suffix}.json' for suffix in ('request', 'outputs'))
        log = metadata / f'{name}.log'
        for path in (request, manifest, log):
            if file_digest(path) != summary['sha256'][path.name]:
                raise ValueError(f'Metadata differs from verified summary: {path.name}')
        flags = set(read_json(request)['effectiveArguments'])
        if not {FLAG, '--media-demand'} <= flags or flags & {
            '--media-demand-permute', '--media-demand-press', '--media-demand-controversy'
        }:
            raise ValueError('Expected the career denominator with direct TV-only demand requests')
        duration, = (flag.split('=', 1)[1] for flag in flags if flag.startswith('--weeks='))
        marker = f'CHART_AUDIT_STARTED run={name} weeks={duration} date='
        start, = (parse_date(line.removeprefix(marker)) for line in log.read_text(encoding='utf-8-sig').splitlines()
                  if line.startswith(marker))
        starts.add(start)
        hashes = {row['name']: row['sha256'].upper() for row in read_json(manifest)}
        verified[arm] = {}
        for suffix in ('media-demand', 'market-clearing-weekly', 'single-demand-stages',
                       'demand-release-census', 'records', 'stature-marketing'):
            path = directory / f'{name}-{suffix}.csv'
            if csv_digest(path).upper() != hashes[path.name]:
                raise ValueError(f'Export differs from manifest: {path.name}')
            verified[arm][suffix] = path

    start, = starts
    week, previous, old, new = first_media_week(*(rows(verified[arm]['media-demand']) for arm in ARMS))
    week_start = start + timedelta(weeks=week - 1)
    shared = old.keys() & new.keys()
    inputs = ('baseline', 'physicalLimit', 'request', 'frenzy')
    changed = {key for key in shared if any(old[key][field] != new[key][field] for field in inputs)}
    counts = Counter(field for key in shared for field in old[key] if old[key][field] != new[key][field])
    regions = {}
    for region in sorted({key[0] for key in old.keys() | new.keys()}):
        totals = {}
        for arm, data in zip(ARMS, (old, new)):
            grants = [int(row['granted']) for key, row in data.items() if key[0] == region]
            totals[arm] = dict(loggedRows=len(grants), moved=sum(g for g in grants if g > 0), net=sum(grants))
        regions[region] = totals

    titles = {key[1] for key in changed}
    details, stages = {}, {}
    for arm, data in zip(ARMS, (old, new)):
        def read(suffix):
            return rows(verified[arm][suffix])

        census = index((r for r in read('demand-release-census') if r['recordId'] in titles), 'recordId')
        records = index((r for r in read('records') if int(r['week']) == week and r['recordId'] in titles), 'recordId')
        stages[arm] = index((r for r in read('single-demand-stages') if int(r['week']) == week), 'region', 'recordId')
        acts = {(r['labelId'], r['artistId']) for r in census.values()}
        details[arm] = dict(
            changedInputs=[dict(media=data[key], census=census[(key[1],)],
                                chart=records.get((key[1],)), demand=stages[arm].get(key)) for key in sorted(changed)],
            mediaQuotes=[r for r in read('stature-marketing') if r['context'] == 'media-envelope'
                         and (r['labelId'], r['artistId']) in acts
                         and parse_date(r['date']) <= week_start],
        )

    control_stages, treatment_stages = (stages[arm] for arm in ARMS)
    stage_changes = Counter(
        (key[0], field) for key in control_stages.keys() & treatment_stages.keys()
        for field in ('rawDemand', 'serviceableDemand', 'clearedUnits')
        if control_stages[key][field] != treatment_stages[key][field]
    )
    return dict(seed=seed, prefix=prefix, summarySha256=file_digest(summary_path),
                analysisSha256=file_digest(Path(__file__)), firstLoggedMediaWeek=week,
                auditWeekStart=week_start.isoformat(),
                firstMarketDifference=first_market_difference(directory, prefix, seed),
                changedSharedFields=counts, controlOnlyKeys=sorted(old.keys() - new.keys()),
                treatmentOnlyKeys=sorted(new.keys() - old.keys()), regions=regions,
                previousMediaRows=[previous[key] for key in sorted(changed & previous.keys())],
                changedSharedDemandStages=[dict(region=region, field=field, count=count)
                                           for (region, field), count in sorted(stage_changes.items())],
                arms=details)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--directory', type=Path, default=Path('SimLogs'))
    parser.add_argument('--prefix', required=True)
    parser.add_argument('--seed', type=int, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    if args.output.exists():
        raise FileExistsError(args.output)
    result = trace(args.directory, args.prefix, args.seed)
    with args.output.open('x', encoding='utf-8') as stream:
        json.dump(result, stream, indent=2, sort_keys=True)
        stream.write('\n')


if __name__ == '__main__':
    main()
