"""Derive experimental regional response and reach from weekly evidence.

Parameters are explicit, fixed and dated. Local sales share is smoothed once;
following density already includes churn. Population-weighted reach is the
mean of local strength clipped to one. This is an offline proxy, not a fitted
or adopted policy. Market-zero and missing following remain unknown.
"""
import argparse
import csv
import datetime as dt
import json
import math
from pathlib import Path

from stature_normalization import SALES, parse_date


def derive(source, output, config):
    effective = dt.date.fromisoformat(config['effectiveDate'])
    half_life = float(config['halfLifeWeeks'])
    local_ref = float(config['localSalesShareReference'])
    density_ref = float(config['followingPerMillionReference'])
    floor = float(config['marketFloorUnits'])
    if not config.get('version') or not all(math.isfinite(v) and v > 0 for v in (half_life, local_ref, density_ref, floor)):
        raise ValueError('Positive finite parameters and version required')
    alpha = 1 - 0.5 ** (1 / half_life)
    old, old_date = {}, None
    fields = ['date', 'artistId', 'regionalScore', 'regionalDraw', 'regionalDrawUpper', 'regionalReach', 'recordReach',
              'recordReachLower', 'recordReachUpper', 'conversionReach', 'conversionReachUpper', 'followingStatus',
              'singleAdvanceIntent', 'albumAdvanceIntent', 'status', 'referenceVersion']
    with output.open('x', newline='', encoding='utf-8') as target:
        writer = csv.DictWriter(target, fieldnames=fields)
        writer.writeheader()
        current_date, groups = None, {}

        def flush(day, acts):
            nonlocal old, old_date
            consecutive = old_date is not None and (day - old_date).days == 7
            prior = old if consecutive else {}
            new = {}
            expected_regions = {r['region'] for regions in acts.values() for r in regions}
            for artist, regions in sorted(acts.items()):
                if {r['region'] for r in regions} != expected_regions:
                    raise ValueError('Incomplete regional coverage')
                total_population = sum(r['population'] for r in regions)
                if total_population <= 0:
                    raise ValueError('Missing regional population')
                regional_scores, record_parts, conversion_parts = [], [], []
                for r in regions:
                    key = (artist, r['region'])
                    past = prior.get(key)
                    values = []
                    for i, raw in enumerate(r['shares']):
                        previous = past[i] if past else None
                        values.append(None if raw is None else raw if previous is None else alpha * raw + (1 - alpha) * previous)
                    new[key] = values
                    # All format/age markets must be measurable to establish the maximum.
                    strength = min(1, max(values) / local_ref) if all(v is not None for v in values) else None
                    density = min(1, r['following'] / r['population'] / density_ref) if r['following'] is not None else None
                    regional_scores.append(strength)
                    record_parts.append((max(strength, density) if strength is not None and density is not None else None, r['population']))
                    conversion_parts.append((density, r['population']))
                def weighted(parts):
                    return sum(v * pop for v, pop in parts) / total_population if all(v is not None for v, _ in parts) else None
                local_score = max(regional_scores) if all(v is not None for v in regional_scores) else None
                record_reach, conversion_reach = weighted(record_parts), weighted(conversion_parts)
                local_draw = max(v for v, _ in conversion_parts) if all(v is not None for v, _ in conversion_parts) else None
                sales_reach = weighted([(strength, r['population']) for strength, r in zip(regional_scores, regions)])
                # In this observation path following is active and the post-update
                # dictionary prunes only below 25 followers. This is a bound for
                # an absent record, not a claim its historical following was zero.
                absent = all(r['following'] is None for r in regions)
                if not absent and any(r['following'] is None for r in regions):
                    raise ValueError('Inconsistent following status across regions')
                local_upper = min(1, 25 / min(r['population'] for r in regions) / density_ref) if absent and min(r['population'] for r in regions) > 0 else None
                conversion_upper = min(1, 25 / total_population / density_ref) if absent else None
                record_upper = min(1, sales_reach + conversion_upper) if absent and sales_reach is not None else None
                if day >= effective:
                    writer.writerow(dict(date=day.isoformat(), artistId=artist, regionalScore=local_score, regionalDraw=local_draw,
                        regionalReach=record_reach, recordReach=record_reach, conversionReach=conversion_reach,
                        regionalDrawUpper=local_upper, recordReachLower=sales_reach if absent else None,
                        recordReachUpper=record_upper, conversionReachUpper=conversion_upper,
                        followingStatus='no_following_record' if absent else 'observed',
                        singleAdvanceIntent=sum(r['singleIntent'] for r in regions), albumAdvanceIntent=sum(r['albumIntent'] for r in regions),
                        status='complete' if local_score is not None and conversion_reach is not None else 'missing_inputs',
                        referenceVersion=config['version']))
            old, old_date = new, day

        with source.open(newline='', encoding='utf-8-sig') as stream:
            seen = set()
            for row in csv.DictReader(stream):
                day = parse_date(row['date'])
                if current_date is not None and day != current_date:
                    if day <= current_date:
                        raise ValueError('Weeks must be strictly ordered')
                    flush(current_date, groups)
                    groups, seen = {}, set()
                current_date = day
                artist, region = row['artistId'], row['regionId']
                if (artist, region) in seen:
                    raise ValueError('Duplicate artist-region-week')
                seen.add((artist, region))
                population = float(row['regionPopulationMillions'])
                sales = [float(row[k]) for k in SALES]
                markets = [float(row['market' + k[0].upper() + k[1:]]) for k in SALES]
                following = float(row['followingYoung']) + float(row['followingAdult']) if row['followingStatus'] == 'observed' else None
                intents = [float(row['singleAdvanceIntent']), float(row['albumAdvanceIntent'])]
                if any(not math.isfinite(x) or x < 0 for x in [population, *sales, *markets, *intents] + ([] if following is None else [following])):
                    raise ValueError('Invalid regional evidence')
                groups.setdefault(artist, []).append(dict(region=region, population=population,
                    shares=[sale / max(market, floor) if market > 0 else None for sale, market in zip(sales, markets)],
                    following=following, singleIntent=intents[0], albumIntent=intents[1]))
        if current_date is None:
            raise ValueError('No evidence')
        flush(current_date, groups)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source', type=Path)
    parser.add_argument('config', type=Path)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    derive(args.source, args.output, json.loads(args.config.read_text(encoding='utf-8-sig')))
