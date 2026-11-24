"""Separate veteran Top 10 occupancy into acts, records and weeks per record."""
import argparse
import json
import re
from bisect import bisect_left
from collections import Counter, defaultdict
from pathlib import Path
from statistics import mean
from typing import NamedTuple

from analyze_stature_media_bookings import verified_run
from analyze_veteran_follower_channels import chart_rows
from stature_normalization import file_digest
from summarize_stature_marketing_trial import rows


class Slot(NamedTuple):
    week: int
    year: int
    rank: int
    artist: str
    record: str | tuple[str, str]


def historical_artist(name):
    name = re.sub(r'^the\s+', '', name.lower()).replace('&', 'and')
    return re.sub(r'[^a-z0-9]', '', name)


def historical_chart(path, rank_limit):
    chart = list(rows(path))
    weeks = {date: week for week, date in enumerate(sorted({r['chartDate'] for r in chart}))}
    ranks = defaultdict(list)
    for r in chart:
        date, rank = r['chartDate'], int(r['position'])
        ranks[date].append(rank)
        artist = historical_artist(r['artist'])
        title = ' '.join(r['title'].lower().split())
        yield Slot(weeks[date], max(1960, int(date[:4])), rank, artist, (artist, title))
    if any(sorted(r) != list(range(1, rank_limit + 1)) for r in ranks.values()):
        raise ValueError(f'Incomplete historical chart in {path}')


def first_entries(chart, rank):
    entries = {}
    for slot in sorted(chart, key=lambda slot: slot.week):
        if slot.rank <= rank:
            entries.setdefault(slot.record, slot)
    return entries


def cohort(year, first_year):
    if year == 1964:
        return '1960' if first_year == 1960 else '1961–62' if first_year <= 1962 else '1963–64'
    return '1960–64' if first_year <= 1964 else '1965+'


def hit_recency(weeks, week):
    index = bisect_left(weeks, week)
    if not index:
        return 'no prior Top 40'
    gap = week - weeks[index - 1]
    if gap <= 52:
        return '1–52 weeks'
    return '53–104 weeks' if gap <= 104 else '105+ weeks'


def summarize(chart):
    first_chart = {}
    for slot in sorted(chart, key=lambda slot: slot.week):
        first_chart.setdefault(slot.artist, slot)
    top40, top10 = first_entries(chart, 40), first_entries(chart, 10)
    hit_weeks = defaultdict(list)
    for slot in top40.values():
        hit_weeks[slot.artist].append(slot.week)
    recency = {key: hit_recency(hit_weeks[s.artist], s.week) for key, s in top40.items()}
    annual = {}
    for year in (1964, 1968):
        groups = defaultdict(list)
        for s in chart:
            if s.year == year:
                groups[cohort(year, first_chart[s.artist].year)].append(s)
        if not groups:
            continue
        annual[year] = {}
        for name, slots in groups.items():
            top = [s for s in slots if s.rank <= 10]
            top_titles = {s.record for s in top}
            entrants = {s.record for s in slots if top40.get(s.record) == s}
            entry_types = Counter(recency[key] for key in entrants)
            annual[year][name] = dict(
                hot100Acts=len({s.artist for s in slots}), hot100Slots=len(slots),
                top40EntryRecency=entry_types,
                top40EntriesReachingTop10=Counter(recency[key] for key in entrants if key in top_titles),
                top10Acts=len({s.artist for s in top}), top10Records=len(top_titles), top10Slots=len(top),
                top10CarryInRecords=sum(top10[key].year < year for key in top_titles),
                top10SlotRecency=Counter(recency[s.record] for s in top),
            )
    return annual, first_chart, top40, top10, hit_weeks


def historical_late_top10(chart, first_chart):
    groups = defaultdict(list)
    for slot in chart:
        if slot.year == 1968:
            groups['1960–64' if slot.artist in first_chart else '1965+'].append(slot)
    return {name: dict(top10Acts=len({s.artist for s in group}),
                       top10Records=len({s.record for s in group}), top10Slots=len(group))
            for name, group in groups.items()}


def reaches_within_26_weeks(entries, launch):
    slot = entries.get(launch['singleId'])
    if slot is None:
        return False
    lag = slot.week - int(launch['week'])
    if slot.artist != launch['artistId'] or lag < 0:
        raise ValueError(f"Chart entry precedes launch or has a different act: {launch['singleId']}")
    return lag < 26


def funnel_counts(outcomes):
    return dict(launches=len(outcomes), top40=sum(top40 for _, top40, _ in outcomes),
                top10=sum(top10 for _, _, top10 in outcomes))


def launch_funnel(launches, first_chart, top40, top10, hit_weeks):
    annual = defaultdict(lambda: defaultdict(list))
    seen = set()
    for r in launches:
        year, week = int(r['year']), int(r['week'])
        if year not in (1964, 1968):
            continue
        artist, record = r['artistId'], r['singleId']
        if record in seen or not record or not artist:
            raise ValueError(f'Invalid launch identity {record} / {artist}')
        seen.add(record)
        prior = first_chart.get(artist)
        group = cohort(year, prior.year) if prior and prior.week < week else 'no prior Hot 100'
        recency = hit_recency(hit_weeks.get(artist, []), week)
        annual[year][(group, recency)].append(r)
    result = {}
    for year, groups in annual.items():
        result[year] = []
        for (group, recency), records in sorted(groups.items()):
            outcomes = [(r, reaches_within_26_weeks(top40, r), reaches_within_26_weeks(top10, r)) for r in records]
            band = [outcome for outcome in outcomes if .70 <= float(outcome[0]['quality']) < .80]
            result[year].append(dict(
                cohort=group, recency=recency, acts=len({r['artistId'] for r in records}),
                **funnel_counts(outcomes), quality70to80=funnel_counts(band),
                **{key: mean(float(r[key]) for r in records) for key in (
                    'quality', 'initialAwareness', 'effectiveRecognition', 'launchCampaignImpact', 'initialStock'
                )},
            ))
    return result


def analyze(directory):
    early = directory / 'historical-hot100-1960-64/hot100_weekly.csv'
    late = directory / 'historical-top10-1968/top10_weekly_1968.csv'
    history, first_chart, *_ = summarize(list(historical_chart(early, 100)))
    history[1968] = historical_late_top10(historical_chart(late, 10), first_chart)
    result = dict(history=history, historicalInputHashes={path.name: file_digest(path) for path in (early, late)},
                  controls={})
    for seed in (1001, 2002):
        name = f'stature-marketing-decade-v2-{seed}-control'
        request, read, hashes = verified_run(directory, name, 522)
        chart = [Slot(int(r['week']), int(r['year']), int(r['currentPosition']), r['artistId'], r['recordId'])
                 for r in chart_rows(read('records'), 522) if 1 <= int(r['currentPosition'] or 0) <= 100]
        annual, first_chart, top40, top10, hit_weeks = summarize(chart)
        funnel = launch_funnel(read('single-launch-attribution'), first_chart, top40, top10, hit_weeks)
        result['controls'][seed] = dict(run=name, assemblySha256=request['assemblySha256'], hashes=hashes,
                                        chart=annual, launchFunnel26Weeks=funnel)
    for annual in (history, *(c['chart'] for c in result['controls'].values())):
        for year, groups in annual.items():
            if sum(group['top10Slots'] for group in groups.values()) != 520:
                raise ValueError(f'Expected 520 Top 10 slots in {year}')
    return result


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--directory', type=Path, default=Path('SimLogs'))
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    result = analyze(args.directory)
    with args.output.open('x', encoding='utf-8') as stream:
        json.dump(result, stream, indent=2, ensure_ascii=False)
        stream.write('\n')
