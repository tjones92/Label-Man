"""Compare chart turnover in matched follower stocking and purchase-intent trials."""
import argparse
import json
from collections import Counter, defaultdict
from pathlib import Path

from analyze_stature_marketing_decade import chart_metrics, share
from analyze_stature_media_bookings import verified_run
from stature_normalization import file_digest, parse_date


ARMS = {'full': None, 'no-stock': '--follower-demand-no-advance-stock',
        'no-intent': '--follower-demand-no-purchase-intent'}
PROVENANCE = ('assemblySha256', 'engineSha256', 'sourceContentSha256', 'sourceRevision',
              'referenceRequestSha256', 'gcEnvironment', 'profile', 'seed', 'weeks')


def chart_rows(entries, weeks):
    ranks = defaultdict(list)
    for row in entries:
        rank = int(row['currentPosition'] or 0)
        if 1 <= rank <= 100:
            ranks[int(row['week'])].append(rank)
            if not row['artistId'] or not row['recordId']:
                raise ValueError('A chart slot lacks an artist or record identity')
        yield row
    if set(ranks) != set(range(weeks + 1)) or any(sorted(r) != list(range(1, 101)) for r in ranks.values()):
        raise ValueError('Incomplete weekly Hot 100 or opening chart')


def launch_books(entries):
    annual = defaultdict(lambda: defaultdict(Counter))
    for row in entries:
        if row['kind'] != 'launch':
            continue
        format_, *parts, _ = row['detail'].split('|')
        fields = dict(part.split('=', 1) for part in parts)
        book = annual[parse_date(row['date']).year][format_]
        book.update(books=1, asked=int(row['value']), granted=int(fields['granted']), stocked=int(fields['stocked']))
    return dict(annual)


def pooled_late(chart):
    years = [chart[str(y)] for y in range(1965, 1970)]
    slots = sum(y['top10Slots'] for y in years)
    cohort = sum(y['cohort1960to64Top10Slots'] for y in years)
    entries = sum(y['top40Entries'] for y in years)
    repeats = sum(y['repeatTop40Entries104Weeks'] for y in years)
    return dict(top10Slots=slots, earlyCohortSlots=cohort, earlyCohortShare=share(cohort, slots),
                top40Entries=entries, repeatEntries=repeats, repeatShare=share(repeats, entries))


def analyze(directory, prefix, seed, weeks):
    reference = None
    result = {}
    for arm, flag in ARMS.items():
        name = f'{prefix}-{seed}-{arm}'
        request, read, hashes = verified_run(directory, name, weeks)
        flags = [f for f in request['effectiveArguments'] if not f.startswith('--run=')]
        if request['arm'] != arm or request['seed'] != seed or request['weeks'] != weeks:
            raise ValueError(f'Run request does not match {name}')
        if arm == 'full':
            reference = request
            baseline_flags = flags
            if flags.count('--follower-demand') != 1 or any(f in flags for f in ARMS.values() if f):
                raise ValueError('Control must use both follower channels')
        elif flags != baseline_flags + [flag] or any(request[k] != reference[k] for k in PROVENANCE):
            raise ValueError('Pair differs beyond the specified channel and run name')

        chart, _ = chart_metrics(chart_rows(read('records'), weeks))
        books = launch_books(read('following'))
        totals = sum((b for year in books.values() for b in year.values()), Counter())
        if not totals['books'] or not totals['asked']:
            raise ValueError('No live follower launch intent was observed')
        if bool(totals['stocked']) != (arm != 'no-stock') or bool(totals['granted']) != (arm != 'no-intent'):
            raise ValueError(f'Follower book telemetry disagrees with {arm}')
        annual = {r['year']: {k: int(r[k]) for k in ('singleUnits', 'albumUnits')}
                  for r in read('decade-annual-rollup')}
        result[arm] = dict(chart=chart, launchBooksByCloseYear=books, annualMarketUnits=annual, hashes=hashes,
                           pooled1965to69=pooled_late(chart) if weeks == 522 else None)
        print(f'Verified {name}', flush=True)

    for year in result['full']['chart']:
        weeks_seen = {d['chart'][year]['observedChartWeeks'] for d in result.values()}
        if len(weeks_seen) != 1:
            raise ValueError(f'Chart-year coverage differs in {year}')
    return dict(prefix=prefix, seed=seed, weeks=weeks, analysisSha256=file_digest(Path(__file__)),
                cohortDefinition='First observed Hot 100 year, including the opening chart. Pre-1960 history is left-censored.',
                repeatDefinition='First observed Top 40 entry for a distinct record, with an earlier entry by that act in the preceding 104 weeks.',
                arms=result)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--directory', type=Path, default=Path('SimLogs'))
    parser.add_argument('--prefix', default='veteran-follower-channels-v1')
    parser.add_argument('--seed', type=int, default=2002)
    parser.add_argument('--weeks', type=int, default=522)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    if args.output.exists():
        raise FileExistsError(args.output)
    result = analyze(args.directory, args.prefix, args.seed, args.weeks)
    with args.output.open('x', encoding='utf-8') as stream:
        json.dump(result, stream, indent=2, sort_keys=True)
        stream.write('\n')


if __name__ == '__main__':
    main()
