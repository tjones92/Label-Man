"""Apply verified synthetic-history identities to existing divergent decades.

The first following log is a post-first-week observation. Its non-seeded acts
form a separate cohort; this script does not assign their new joins to a cause.
"""
import argparse
import json
import re
from collections import Counter, defaultdict
from pathlib import Path

from trace_stature_marketing_mechanisms import rows
from stature_normalization import file_digest, parse_date


def join(directory, origin_file, prefix, output):
    origin = json.loads(origin_file.read_text())
    result = {'originReportSha256': file_digest(origin_file), 'analysisSha256': file_digest(Path(__file__)),
        'prefix': prefix, 'seeds': {},
        'caveat': 'Fixed cohorts in divergent worlds, not causal act-level effects. Synthetic-history IDs/archetypes must exactly match startup career logs in each original arm. First-week-only following is not proven to originate exclusively from prewarmed records. Runtime identities are not matched.'}
    for seed, source in origin['seeds'].items():
        census = source['control']['census']
        seeded = {r['artistId']: r['openingArchetype'] for r in census}
        first, fixed_chart = set(), set()
        for r in rows(directory, f'{prefix}-{seed}-control', 'following'):
            if r['date'] != '1/1/1960':
                break
            if r['kind'] == 'act':
                first.add(r['artistId'])
        for r in rows(directory, f'{prefix}-{seed}-control', 'records'):
            if r['week'] != '0':
                break
            fixed_chart.add(r['artistId'])
        worlds = {}
        group = lambda a: 'syntheticHistoryFollowing' if a in seeded else 'firstWeekFollowingOnly' if a in first else 'other'
        for arm in ('control', 'treatment'):
            name = f'{prefix}-{seed}-{arm}'
            log = (directory / 'writing-calibration' / f'{name}.log').read_text(encoding='utf-8-sig')
            startup = dict(re.findall(r'^OPENING_CAREER artist=(\S+).*? archetype=(\S+)', log, flags=re.MULTILINE))
            if startup != seeded:
                raise ValueError('Synthetic history identities/archetypes differ from original startup')
            chart, budgets, first_chart = defaultdict(Counter), defaultdict(Counter), {}
            artist_slots = defaultdict(Counter)
            for r in rows(directory, name, 'records'):
                if r['artistId'] in seeded and 1 <= int(r['currentPosition']) <= 100:
                    first_chart.setdefault(r['artistId'], int(r['year']))
                if r['year'] not in ('1964', '1968'):
                    continue
                g = group(r['artistId']); c = chart[int(r['year']), g]
                c['hot100SlotWeeks'] += 1 <= int(r['currentPosition']) <= 100
                c['top10SlotWeeks'] += 1 <= int(r['currentPosition']) <= 10
                if r['artistId'] in seeded and 1 <= int(r['currentPosition']) <= 10:
                    artist_slots[int(r['year'])][r['artistId']] += 1
            for r in rows(directory, name, 'stature-marketing'):
                if r['context'] != 'release-launch':
                    continue
                y = parse_date(r['date']).year
                if y not in (1964, 1968):
                    continue
                c = budgets[y, group(r['artistId'])]
                old, proposed = float(r['legacyBudget']), float(r['proposedBudget'])
                c['reads'] += 1; c['available'] += r['status'] == 'Available'
                c['uplifts'] += proposed > old; c['cuts'] += proposed < old
            worlds[arm] = {'chart': [{'year': y, 'group': g, **c} for (y, g), c in sorted(chart.items())],
                'singleLaunchReads': [{'year': y, 'group': g, **c} for (y, g), c in sorted(budgets.items())],
                'syntheticHistoryActsFirstObservedChartYears': dict(sorted(Counter(first_chart.values()).items())),
                'syntheticHistoryActsNeverObservedOnChart': len(seeded) - len(first_chart),
                'syntheticHistoryFirstObservedChartYearByArtist': first_chart,
                'seededActTop10SlotsByYear': dict(artist_slots)}
        changes = {}
        for year in (1964, 1968):
            a = worlds['control']['seededActTop10SlotsByYear'].get(year, {})
            b = worlds['treatment']['seededActTop10SlotsByYear'].get(year, {})
            deltas = sorted((b.get(artist, 0) - a.get(artist, 0), artist) for artist in seeded)
            changes[year] = {'winners': list(reversed([r for r in deltas if r[0] > 0][-5:])),
                'losers': [r for r in deltas if r[0] < 0][:5],
                'controlTop10Acts': len(a), 'treatmentTop10Acts': len(b),
                'netSlotDelta': sum(b.values()) - sum(a.values())}
        result['seeds'][seed] = {'syntheticHistoryActs': len(seeded), 'firstObservedFollowingActs': len(first),
            'firstWeekOnlyActs': len(first - seeded.keys()), 'syntheticHistoryActsInWeekZeroChart': len(seeded.keys() & fixed_chart),
            'firstWeekOnlyActsInWeekZeroChart': len((first - seeded.keys()) & fixed_chart), 'arms': worlds,
            'seededActChartChanges': changes}
    with output.open('x', encoding='utf-8') as f:
        json.dump(result, f, indent=2); f.write('\n')


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('origin_report', type=Path)
    p.add_argument('output', type=Path)
    p.add_argument('--directory', type=Path, default=Path('SimLogs'))
    p.add_argument('--prefix', default='stature-marketing-decade-v2')
    a = p.parse_args()
    join(a.directory, a.origin_report, a.prefix, a.output)
