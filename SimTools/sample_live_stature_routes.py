"""Select observable rising transitions and attach raw weekly evidence.

Route labels identify model support at a transition, not causal explanations.
This does not establish sustained breakthroughs or complete exposure histories.
"""
import argparse
import csv
import datetime as dt
import json
from collections import defaultdict
from pathlib import Path

from stature_normalization import file_digest, parse_date
from verify_stature_evidence import verify

BANDS = ('BelowRising', 'Rising', 'Established', 'Star', 'Superstar')
ROUTES = ('Regional', 'Record', 'Conversion')
MEASURES = ('singleYoungUnits', 'singleAdultUnits', 'albumYoungUnits', 'albumAdultUnits',
            'tvRegisteredYoungEstimate', 'tvRegisteredAdultEstimate', 'followingYoung', 'followingAdult',
            'buyJoinYoung', 'buyJoinAdult', 'tvJoinYoung', 'tvJoinAdult', 'pressJoinYoung', 'pressJoinAdult',
            'seriesJoinYoung', 'seriesJoinAdult', 'liveJoinYoung', 'liveJoinAdult')


def number(row, field):
    return float(row[field]) if row.get(field) else None


def sample(shadow, evidence, output, earliest_date=None):
    verify(evidence)
    cutoff = dt.date.fromisoformat(earliest_date) if earliest_date else None
    roster = set()
    with evidence.open(newline='', encoding='utf-8-sig') as stream:
        for row in csv.DictReader(stream):
            roster.add((parse_date(row['date']), row['artistId']))
    choices, previous = {}, {}
    with shadow.open(newline='', encoding='utf-8-sig') as stream:
        for row in csv.DictReader(stream):
            artist, day = row['artistId'], parse_date(row['date'])
            old = previous.get(artist)
            if old is not None and day <= old[0]:
                raise ValueError('Artist snapshots must be strictly increasing')
            valid = row['status'] in ('Observed', 'BelowRising') and row['shadowBand'] in BANDS
            previous[artist] = (day, row['shadowBand'], valid)
            if not valid or old is None or not old[2] or day - old[0] != dt.timedelta(days=7) or (day, artist) not in roster:
                continue
            if cutoff is not None and day < cutoff:
                continue
            band = row['shadowBand']
            if band == 'BelowRising':
                lower, boundary = number(row, 'RisingLower'), number(row, 'RisingBoundary')
                if lower is not None and boundary is not None and boundary > 0 and 0 <= lower < boundary:
                    ratio = lower / boundary
                    if 'closest_below_rising' not in choices or ratio > choices['closest_below_rising']['supportFraction']:
                        choices['closest_below_rising'] = {'artistId': artist, 'date': day.isoformat(), 'band': band,
                            'careerState': row['careerState'], 'supportFraction': ratio, 'support': lower, 'boundary': boundary}
            if BANDS.index(band) < BANDS.index(old[1]) and row['careerState'] in ('Established', 'Star', 'Superstar', 'Declining'):
                choices.setdefault('declining_incumbent', {'artistId': artist, 'date': day.isoformat(),
                    'fromBand': old[1], 'toBand': band, 'careerState': row['careerState']})
            if BANDS.index(band) <= BANDS.index(old[1]):
                continue
            support = {route: number(row, band + route) for route in ROUTES}
            if any(value is None for value in support.values()):
                continue
            lead = max(ROUTES, key=lambda route: (support[route], -ROUTES.index(route)))
            candidate = {'artistId': artist, 'date': day.isoformat(), 'fromBand': old[1], 'toBand': band,
                         'careerState': row['careerState'], 'support': support}
            choices.setdefault(lead.lower() + '_rise', candidate)
            sales = [number(row, name) for name in ('singleYoungScore', 'singleAdultScore', 'albumYoungScore', 'albumAdultScore')]
            following = [number(row, name) for name in ('followingYoungScore', 'followingAdultScore')]
            if lead == 'Record' and all(x is not None for x in sales + following) and sales[3] == max(sales) and sales[3] > max(following):
                choices.setdefault('adult_lp_response_rise', candidate)
            tv = [number(row, name) for name in ('tvYoungScore', 'tvAdultScore')]
            if lead == 'Conversion' and all(x is not None for x in tv + following) and max(tv) > max(following):
                choices.setdefault('tv_response_rise', candidate)
    selected = {entry['artistId'] for entry in choices.values()}
    raw = defaultdict(lambda: {field: 0. for field in MEASURES})
    metadata = {}
    with evidence.open(newline='', encoding='utf-8-sig') as stream:
        for row in csv.DictReader(stream):
            artist = row['artistId']
            if artist not in selected:
                continue
            key = (parse_date(row['date']).isoformat(), artist)
            for field in MEASURES:
                raw[key][field] += float(row[field])
            metadata[key] = {'artist': row['artist'], 'labelId': row['labelId'], 'labelTier': row['labelTier'],
                             'genre': row['genre'], 'followingStatus': row['followingStatus']}
    trajectories = defaultdict(list)
    with shadow.open(newline='', encoding='utf-8-sig') as stream:
        for row in csv.DictReader(stream):
            artist = row['artistId']
            key = (parse_date(row['date']).isoformat(), artist)
            if artist not in selected or key not in raw:
                continue
            trajectories[artist].append({'date': key[0], 'careerState': row['careerState'], 'band': row['shadowBand'],
                'status': row['status'], 'snapshot': {field: row[field] for field in row if field.endswith('Score')},
                'raw': raw[key], **metadata[key]})
    categories = ('regional_rise', 'record_rise', 'conversion_rise', 'adult_lp_response_rise', 'tv_response_rise',
                  'declining_incumbent', 'closest_below_rising')
    result = {'shadowSha256': file_digest(shadow), 'evidenceSha256': file_digest(evidence), 'earliestTransitionDate': earliest_date,
              'selected': choices,
              'unobservedCategories': [key for key in categories if key not in choices], 'weekly': dict(trajectories),
              'caveat': 'First observed rises and incumbent decline in source order; the below-Rising near miss has the closest observed lower support to its boundary. Support-route and score dominance do not establish causation or sustained success. Adult LP and TV categories require their response score to exceed following; ties between sales formats qualify. Raw evidence is AI-roster only and omitted outside that roster. Opportunities, spend, full chart history, and resulting decisions are not attached.'}
    with output.open('x', encoding='utf-8') as stream:
        json.dump(result, stream, indent=2, sort_keys=True)
        stream.write('\n')


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('shadow', type=Path)
    p.add_argument('evidence', type=Path)
    p.add_argument('output', type=Path)
    p.add_argument('--earliest-date', help='ISO date; earlier observations still establish the prior band')
    args = p.parse_args()
    sample(args.shadow, args.evidence, args.output, args.earliest_date)
