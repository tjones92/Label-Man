"""Apply an experimental policy to normalized components and supplied reach.

Reach CSV must contain date, artistId, regionalScore, regionalDraw, regionalReach, recordReach and
conversionReach. Blank values are unknown. This is an offline diagnostic;
it does not derive or calibrate reach or change the simulation.
"""
import argparse
import csv
import datetime as dt
import json
from pathlib import Path

from stature_normalization import COMPONENTS
from stature_shadow_policy import evaluate
from fit_stature_experiment import is_fit


def number(value):
    return float(value) if value != '' else None


def max_known(values):
    # A maximum is only known when every competing component is observed.
    return max(values) if all(value is not None for value in values) else None


def run(components_path, reach_path, calibration, score_field, output_path, holdout_only=False, bound_pruned=False):
    reach = {}
    with reach_path.open(newline='', encoding='utf-8-sig') as stream:
        for row in csv.DictReader(stream):
            key = (row['date'], row['artistId'])
            if key in reach:
                raise ValueError('Duplicate reach row')
            reach[key] = {route: number(row[route + 'Reach']) for route in ('regional', 'record', 'conversion')}
            reach[key]['regionalScore'] = number(row['regionalScore'])
            reach[key]['regionalDraw'] = number(row['regionalDraw'])
            if bound_pruned:
                for field in ('regionalDrawUpper', 'recordReachLower', 'recordReachUpper', 'conversionReachUpper'):
                    reach[key][field] = number(row[field])
                reach[key]['followingStatus'] = row['followingStatus']
    previous = {}
    last_key = None
    closed = set()
    with components_path.open(newline='', encoding='utf-8-sig') as source, output_path.open('x', newline='', encoding='utf-8') as target:
        writer = csv.DictWriter(target, fieldnames=['date', 'artistId', 'careerState', 'previousBand', 'band', 'status',
            *[name + suffix for name in ('Rising', 'Established', 'Star', 'Superstar') for suffix in
              ('Lower', 'Upper', 'Boundary', 'Status', 'LeadingRoute', 'RegionalLower', 'RecordLower', 'ConversionLower')]])
        writer.writeheader()
        group = {}

        def emit(key, values):
            if set(values) != set(COMPONENTS):
                raise ValueError('Incomplete or unexpected component set')
            date, artist = key
            if date < calibration['effectiveDate'] or (holdout_only and is_fit(artist)):
                return
            day = dt.date.fromisoformat(date)
            prior_date, prior_band = previous.get(artist, (None, None))
            if prior_date is not None and day <= prior_date:
                raise ValueError('Artist weeks must be strictly increasing')
            if prior_date is None or (day - prior_date).days != 7:
                prior_band = None
            scores = {k: number(v[score_field]) for k, v in values.items()}
            sales = max_known([scores[k] for k in COMPONENTS[:4]])
            tv = max_known([scores[k] for k in COMPONENTS[4:6]])
            draw = max_known([scores[k] for k in COMPONENTS[6:]])
            reaches = reach.get(key, {})
            if bound_pruned and reaches.get('followingStatus') == 'no_following_record':
                ref_field = 'peerReference' if score_field == 'peerScore' else 'fixedReference'
                refs = [number(values[k][ref_field]) for k in COMPONENTS[6:]]
                if all(ref is not None and ref > 0 for ref in refs):
                    draw = [0, min(1, max(25 / ref for ref in refs))]
                if reaches.get('regionalDrawUpper') is not None:
                    reaches['regionalDraw'] = [0, reaches['regionalDrawUpper']]
                if reaches.get('recordReachLower') is not None and reaches.get('recordReachUpper') is not None:
                    reaches['regional'] = reaches['record'] = [reaches['recordReachLower'], reaches['recordReachUpper']]
                if reaches.get('conversionReachUpper') is not None:
                    reaches['conversion'] = [0, reaches['conversionReachUpper']]
            inputs = {'regional': {'response': reaches.get('regionalScore'), 'draw': None, 'reach': reaches.get('regional')},
                      'record': {'response': sales, 'draw': draw, 'reach': reaches.get('record')},
                      'conversion': {'response': tv, 'draw': draw, 'reach': reaches.get('conversion')}}
            inputs['regional']['draw'] = reaches.get('regionalDraw')
            result = evaluate(inputs, calibration, date, prior_band)
            # Unknown evidence cannot establish a new hysteresis state.
            next_band = result['band'] if result['status'] != 'missing_evidence' else prior_band
            previous[artist] = (day, next_band)
            row = {'date': date, 'artistId': artist, 'careerState': values[COMPONENTS[0]]['careerState'],
                   'previousBand': prior_band, 'band': result['band'], 'status': result['status']}
            for band, info in result['bands'].items():
                for field in ('lower', 'upper', 'boundary', 'status'):
                    row[band + field.capitalize()] = info[field]
                for route, support in info['routes'].items():
                    row[band + route.capitalize() + 'Lower'] = support['lower']
                row[band + 'LeadingRoute'] = (max(sorted(info['routes']), key=lambda r: info['routes'][r]['lower'])
                                              if info['lower'] > 0 else '')
            writer.writerow(row)

        for row in csv.DictReader(source):
            key = (row['date'], row['artistId'])
            if key != last_key and last_key is not None:
                emit(last_key, group)
                closed.add(last_key)
                group = {}
            if key in closed or row['component'] in group:
                raise ValueError('Duplicate or noncontiguous component group')
            group[row['component']] = row
            last_key = key
        if last_key is None:
            raise ValueError('No components')
        emit(last_key, group)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('components', type=Path)
    parser.add_argument('reach', type=Path)
    parser.add_argument('calibration', type=Path)
    parser.add_argument('output', type=Path)
    parser.add_argument('--score', choices=('fixedScore', 'peerScore'), required=True)
    parser.add_argument('--holdout-only', action='store_true')
    parser.add_argument('--bound-pruned-following', action='store_true', help='Use the current active-following prune bound (25) for absent records')
    args = parser.parse_args()
    run(args.components, args.reach, json.loads(args.calibration.read_text(encoding='utf-8-sig')), args.score, args.output,
        args.holdout_only, args.bound_pruned_following)
