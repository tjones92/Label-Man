"""Summarize follower-launch intent, stock grants and store-clearing bounds.

The bound is not an attribution of sales to followers. It caps how many of a
record's cleared store units could correspond to its pre-clearing launch grant.
"""
import argparse
import csv
import json
from collections import defaultdict
from pathlib import Path

from stature_normalization import file_digest, parse_date
from verify_stature_evidence import verify


def summarize(source, output):
    rows, weeks, artists = verify(source)
    needed = [prefix + suffix for prefix in ('single', 'album') for suffix in
              ('FollowerIntent', 'FollowerStockGrant', 'FollowerClearedUpperBound')]
    annual = defaultdict(lambda: defaultdict(float))
    regions = defaultdict(set)
    with source.open(newline='', encoding='utf-8-sig') as stream:
        reader = csv.DictReader(stream)
        if not set(needed + ['followerLaunchMode']).issubset(reader.fieldnames or ()):
            raise ValueError('Source lacks follower-launch bounds')
        for row in reader:
            key = (parse_date(row['date']).year, row['followerLaunchMode'])
            for prefix in ('single', 'album'):
                for suffix in ('FollowerIntent', 'FollowerStockGrant', 'FollowerClearedUpperBound', 'YoungUnits', 'AdultUnits'):
                    annual[key][prefix + suffix] += float(row[prefix + suffix])
                if float(row[prefix + 'FollowerClearedUpperBound']) > 0:
                    regions[(key, prefix)].add((row['date'], row['artistId'], row['regionId']))
    report = []
    for (year, mode), totals in sorted(annual.items()):
        report.append({'year': year, 'mode': mode, 'totals': dict(sorted(totals.items())),
                       'artistRegionWeeksWithPositiveBound': {p: len(regions[((year, mode), p)]) for p in ('single', 'album')}})
    result = {'sourceSha256': file_digest(source), 'rows': rows, 'weeks': weeks, 'artists': artists, 'annual': report,
              'caveat': 'AI-roster evidence only. Intent and stock grant are pre-market quantities. The bound sums min(pre-clearing grant, record store units cleared) per record and region in causal mode; it is not actual follower-attributed sales or incremental demand, and excludes rack and other non-store channels. It cannot by itself establish the effect of discounting on a standing band.'}
    with output.open('x', encoding='utf-8') as stream:
        json.dump(result, stream, indent=2, sort_keys=True)
        stream.write('\n')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source', type=Path)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    summarize(args.source, args.output)
