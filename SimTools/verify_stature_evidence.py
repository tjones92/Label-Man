"""Check basic integrity of a completed raw stature-evidence CSV."""
import argparse
import csv
import math
from collections import defaultdict


def verify(path):
    seen, markets, observed = set(), {}, defaultdict(float)
    dates, artists = set(), set()
    measures = ('singleYoungUnits', 'singleAdultUnits', 'albumYoungUnits', 'albumAdultUnits')
    joins = tuple(source + 'Join' + age for source in ('buy', 'tv', 'press', 'series', 'live') for age in ('Young', 'Adult'))
    follower = tuple(prefix + suffix for suffix in ('FollowerIntent', 'FollowerStockGrant', 'FollowerClearedUpperBound')
                     for prefix in ('single', 'album'))
    with open(path, newline='', encoding='utf-8-sig') as stream:
        reader = csv.DictReader(stream)
        present = set(joins) & set(reader.fieldnames or ())
        if present and len(present) != len(joins):
            raise ValueError('incomplete booked-join columns')
        follower_present = set(follower + ('followerLaunchMode',)) & set(reader.fieldnames or ())
        if follower_present and len(follower_present) != len(follower) + 1:
            raise ValueError('incomplete follower-launch columns')
        for row in reader:
            key = (row['date'], row['artistId'], row['regionId'])
            if key in seen:
                raise ValueError(f'duplicate act-region-week: {key}')
            seen.add(key)
            dates.add(row['date'])
            artists.add(row['artistId'])
            region_week = (row['date'], row['regionId'])
            market = tuple(float(row['market' + x[0].upper() + x[1:]]) for x in measures)
            if any(not math.isfinite(value) or value < 0 for value in market):
                raise ValueError(f'invalid market denominator: {region_week}')
            if region_week in markets and market != markets[region_week]:
                raise ValueError(f'inconsistent market denominator: {region_week}')
            markets[region_week] = market
            for index, field in enumerate(measures):
                value = float(row[field])
                if not math.isfinite(value) or value < 0:
                    raise ValueError(f'invalid {field}: {key}')
                observed[(region_week, index)] += value
            young, adult = float(row['followingYoung']), float(row['followingAdult'])
            for prefix in ('single', 'album'):
                expected = young * float(row[prefix + 'YoungRate']) + adult * float(row[prefix + 'AdultRate'])
                if prefix == 'single':
                    expected *= float(row['singleScale'])
                actual = float(row[prefix + 'AdvanceIntent'])
                if not math.isclose(actual, expected, rel_tol=1e-5, abs_tol=0.01):
                    raise ValueError(f'inconsistent {prefix} advance intent: {key}')
            for field in ('tvRegisteredYoungEstimate', 'tvRegisteredAdultEstimate', 'followingYoung', 'followingAdult'):
                value = float(row[field])
                if not math.isfinite(value) or value < 0:
                    raise ValueError(f'invalid {field}: {key}')
            for field in joins if present else ():
                value = float(row[field])
                if not math.isfinite(value) or value < 0:
                    raise ValueError(f'invalid {field}: {key}')
            if follower_present:
                mode = row['followerLaunchMode']
                if mode not in ('inactive', 'shadow', 'causal'):
                    raise ValueError(f'invalid follower-launch mode: {key}')
                for field in follower:
                    value = float(row[field])
                    if not math.isfinite(value) or value < 0:
                        raise ValueError(f'invalid {field}: {key}')
                for prefix in ('single', 'album'):
                    upper = float(row[prefix + 'FollowerClearedUpperBound'])
                    grant = float(row[prefix + 'FollowerStockGrant'])
                    sales = float(row[prefix + 'YoungUnits']) + float(row[prefix + 'AdultUnits'])
                    if upper > grant + 0.01 or upper > sales + max(0.01, sales * 1e-6) or (mode != 'causal' and upper != 0):
                        raise ValueError(f'invalid {prefix} follower-cleared bound: {key}')
            if row['followingStatus'] not in ('observed', 'no_following_record'):
                raise ValueError(f'invalid following status: {key}')
            if row['followingStatus'] == 'no_following_record' and (young != 0 or adult != 0):
                raise ValueError(f'absent following has nonzero population: {key}')
    if not seen:
        raise ValueError('no evidence rows')
    for (region_week, index), total in observed.items():
        denominator = markets[region_week][index]
        if not math.isfinite(denominator) or total > denominator + max(0.01, denominator * 1e-6):
            raise ValueError(f'AI sales exceed market denominator: {region_week} {measures[index]}')
    return len(seen), len(dates), len(artists)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('path')
    result = verify(parser.parse_args().path)
    print(f'Validated {result[0]} rows, {result[1]} weeks, {result[2]} artists')
