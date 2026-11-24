"""Read archived paired decades without unpacking or changing the run artifacts."""
import argparse
import csv
import gzip
import json
from collections import Counter, defaultdict
from pathlib import Path


def rows(directory, name, suffix):
    path = directory / f'{name}-{suffix}.csv'
    opener = path.open if path.exists() else lambda **kw: gzip.open(str(path) + '.gz', 'rt', **kw)
    with opener(encoding='utf-8-sig', newline='') as stream:
        yield from csv.DictReader(stream)


def share(numerator, denominator):
    return numerator / denominator if denominator else None


def chart_metrics(observations):
    first, artist_year, first_entries = {}, defaultdict(Counter), {}
    annual = defaultdict(Counter)
    chart_rows = []
    for r in observations:
        pos = int(r['currentPosition'] or 0)
        if not 1 <= pos <= 100:
            continue
        week, year, artist = int(r['week']), int(r['year']), r['artistId']
        first[artist] = min(first.get(artist, (year, week)), (year, week))
        artist_year[year][artist] += 1
        chart_rows.append((year, week, pos, artist, r['labelTier']))
        if pos <= 40:
            key = r['recordId']
            candidate = (week, year, artist)
            first_entries[key] = min(first_entries.get(key, candidate), candidate)
    for year, week, pos, artist, tier in chart_rows:
        a = annual[year]
        a['hot100Slots'] += 1
        a['top10Slots'] += pos <= 10
        a['cohort1960Hot100Slots'] += first[artist][0] == 1960
        a['cohort1960Top10Slots'] += first[artist][0] == 1960 and pos <= 10
        a['cohort1963to64Hot100Slots'] += 1963 <= first[artist][0] <= 1964
        a['cohort1963to64Top10Slots'] += 1963 <= first[artist][0] <= 1964 and pos <= 10
        a['cohort1960to64Top10Slots'] += first[artist][0] <= 1964 and pos <= 10
        a['majorTierTop10Slots'] += tier == 'Major' and pos <= 10
    entries_by_artist, annual_entries = defaultdict(list), defaultdict(Counter)
    for week, year, artist in sorted(first_entries.values()):
        a = annual[year]
        a['top40Entries'] += 1
        a['repeatTop40Entries104Weeks'] += any(0 < week - old <= 104 for old in entries_by_artist[artist])
        a['openingWeekTop40Entries'] += week == 0
        annual_entries[year][artist] += 1
        entries_by_artist[artist].append(week)
    for year, a in annual.items():
        for group, condition in (
            ('cohort1960', lambda y: y == 1960),
            ('cohort1963to64', lambda y: 1963 <= y <= 1964),
            ('recentEntrants', lambda y: year - 1 <= y <= year),
        ):
            counts = [n for artist, n in artist_year[year].items() if condition(first[artist][0])]
            cohort = [artist for artist in first if condition(first[artist][0])]
            a[group + 'ChartingActs'] = len(counts)
            a[group + 'Hot100WeeksPerChartingAct'] = share(sum(counts), len(counts))
            a[group + 'ObservedCohortSize'] = len(cohort)
            a[group + 'ChartingShareOfObservedCohort'] = share(len(counts), len(cohort))
            a[group + 'Top10ActsInYear'] = len({artist for y, _, pos, artist, _ in chart_rows if y == year and pos <= 10 and condition(first[artist][0])})
        a['repeatTop40Share104Weeks'] = share(a['repeatTop40Entries104Weeks'], a['top40Entries'])
        a['cohort1960Top10Share'] = share(a['cohort1960Top10Slots'], a['top10Slots'])
        a['cohort1960to64Top10Share'] = share(a['cohort1960to64Top10Slots'], a['top10Slots'])
        a['top10ActsShareOfTop40Entries'] = share(sum(n for _, n in annual_entries[year].most_common(10)), a['top40Entries'])
        a['top40EntryHHI'] = sum((n / a['top40Entries']) ** 2 for n in annual_entries[year].values()) if a['top40Entries'] else None
        a['observedChartWeeks'] = len({w for y, w, _, _, _ in chart_rows if y == year})
    return {str(y): dict(a) for y, a in sorted(annual.items())}, first


def analyze_run(directory, name, economy):
    chart, first = chart_metrics(rows(directory, name, 'records'))
    print(f'Analyzed chart: {name}', flush=True)
    # Finance rows include closed firms. End-of-year counts distinguish active firms.
    finance = defaultdict(dict)
    for r in rows(directory, name, 'label-finance'):
        finance[int(r['year'])][r['labelId']] = r
    labels = {}
    for year, firms in sorted(finance.items()):
        active = [r for r in firms.values() if r['status'] not in ('Defunct', 'Closed')]
        labels[str(year)] = dict(observedLabels=len(firms), activeLabels=len(active),
            statusCounts=dict(Counter(r['status'] for r in firms.values())),
            activeTierCounts=dict(Counter(r['labelTier'] for r in active)),
            activeCash=sum(float(r['cashReserves']) for r in active),
            closedLabels=sum(r['status'] in ('Defunct', 'Closed') for r in firms.values()))
    career = {}
    for r in rows(directory, name, 'career-ladder'):
        career[r['year']] = r
    concentration = {r['year']: r for r in rows(directory, name, 'concentration')}
    issue_unique = defaultdict(set)
    for r in rows(directory, name, 'release-slate'):
        if r['state'] == 'Issued' and r['recordId']:
            issue_unique[r['format']].add(r['recordId'])
    formation, signing, terminal = {}, Counter(), Counter()
    for r in rows(directory, name, 'artist-population-events'):
        if r['eventType'] == 'formation':
            formation[r['artistId']] = int(r['week'])
        signing[r['eventType']] += 1
        if r['lifecycleStatus'] in ('Retired', 'Disbanded'):
            terminal[r['eventType']] += 1
    deals = Counter()
    for r in rows(directory, name, 'deal-ledger'):
        deals['events:' + r['resolution']] += 1
        if r['resolution'] == 'Signed':
            deals['signedAdvance'] += float(r['advance'])
            deals['signedTermWeeks'] += int(r['termWeeks'])
    profiles = []
    if (directory / f'{name}-performance-profile.csv.gz').exists() or (directory / f'{name}-performance-profile.csv').exists():
        profiles = list(rows(directory, name, 'performance-profile'))
    return dict(chart=chart, labels=labels, careerYearEnd=career, concentration=concentration,
                uniqueIssuedRecords={k: len(v) for k, v in issue_unique.items()},
                populationEventCounts=dict(signing), distributionDealEvents=dict(deals),
                profile=profiles, economy=economy,
                cohortCoverage=dict(observedChartArtists=len(first), openingWeekArtists=sum(w == 0 for y, w in first.values()),
                                    observedRuntimeFormations=len(formation)))


def compare(control, treatment):
    def delta(a, b):
        return dict(control=a, treatment=b, difference=b-a, relativeChange=share(b-a, a))
    result = {}
    for label, key, fields in (
        ('launch', 'releaseCampaignCostEvents', ('events', 'marketingCost', 'productionCost')),
        ('followup', 'followUpCampaignEvents', ('spend:FollowUp', 'events:FollowUp')),
    ):
        result[label] = {f: delta(control['economy'][key].get(f, 0), treatment['economy'][key].get(f, 0)) for f in fields}
    result['units1960to1969'] = {f: delta(sum(int(r[f]) for r in control['economy']['annualRollup'] if int(r['year']) <= 1969),
                                         sum(int(r[f]) for r in treatment['economy']['annualRollup'] if int(r['year']) <= 1969))
                                  for f in ('singleUnits', 'albumUnits')}
    result['annualUnits'] = {}
    ca = {r['year']: r for r in control['economy']['annualRollup']}
    for r in treatment['economy']['annualRollup']:
        result['annualUnits'][r['year']] = {f: delta(int(ca[r['year']][f]), int(r[f])) for f in ('singleUnits', 'albumUnits')}
    return result


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--directory', type=Path, default=Path('SimLogs'))
    p.add_argument('--prefix', default='stature-marketing-decade-v2')
    p.add_argument('--output', type=Path, required=True)
    args = p.parse_args()
    result = dict(prefix=args.prefix, seeds={}, definitions={
        'cohort': 'First observed Hot 100 year, including week-zero opening chart; earlier career history is left-censored.',
        'repeat': 'Distinct record first observed in Top 40, with a different record by that act entering strictly earlier in the preceding 104 weeks. Opening Top 40 is left-censored.',
        'windows': 'Annual unit totals use 1960-1969; 522-week event totals and profiles also include the final partial 1970 week.',
        'chartWeeksPerAct': 'Record slot-weeks divided by charting acts; simultaneous records contribute multiple slot-weeks.',
        'labels': 'Last observed finance row per label per calendar year; Defunct and Closed are excluded from active counts.',
        'causality': 'Paired worlds diverge. Differences are net system effects, not matched-act causal effects.',
    })
    for seed in (1001, 2002):
        summary = json.loads((args.directory / 'writing-calibration' / f'{args.prefix}-{seed}-summary.json').read_text(encoding='utf-8-sig'))
        arms = {arm: analyze_run(args.directory, f'{args.prefix}-{seed}-{arm}', summary['arms'][arm]) for arm in ('control', 'treatment')}
        result['seeds'][str(seed)] = dict(arms=arms, comparison=compare(arms['control'], arms['treatment']))
    with args.output.open('x', encoding='utf-8') as stream:
        json.dump(result, stream, indent=2)
        stream.write('\n')
    print(f'Wrote {args.output}', flush=True)


if __name__ == '__main__':
    main()
