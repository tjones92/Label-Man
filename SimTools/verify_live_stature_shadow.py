"""Compare live shadow snapshots with the offline experimental implementation."""
import argparse
import csv
import datetime as dt
import math
from pathlib import Path

from stature_normalization import COMPONENTS, parse_date


def verify(live, components, offline, decisions):
    expected = {}
    with offline.open(newline='', encoding='utf-8-sig') as stream:
        for row in csv.DictReader(stream):
            expected[(row['date'], row['artistId'])] = row
    scores = {}
    with components.open(newline='', encoding='utf-8-sig') as stream:
        for row in csv.DictReader(stream):
            key = (row['date'], row['artistId'])
            if key in expected:
                scores.setdefault(key, {})[row['component']] = row['peerScore']
    names = ('singleYoungScore', 'singleAdultScore', 'albumYoungScore', 'albumAdultScore',
             'tvYoungScore', 'tvAdultScore', 'followingYoungScore', 'followingAdultScore')
    counts = {'compared': 0, 'scoreDifferences': 0, 'supportDifferences': 0, 'bandDifferences': 0, 'decisionRows': 0}
    with live.open(newline='', encoding='utf-8-sig') as stream:
        for row in csv.DictReader(stream):
            key = (parse_date(row['date']).isoformat(), row['artistId'])
            if key not in expected:
                continue
            counts['compared'] += 1
            other = expected[key]
            expected_status = {'observed': 'Observed', 'below_rising': 'BelowRising', 'missing_evidence': 'MissingEvidence'}[other['status']]
            if row['status'] != expected_status or (other['status'] != 'missing_evidence' and
                row['shadowBand'] != (other['band'] or 'BelowRising')):
                counts['bandDifferences'] += 1
            for component, name in zip(COMPONENTS, names):
                left, right = row[name], scores[key][component]
                if bool(left) != bool(right) or (left and not math.isclose(float(left), float(right), rel_tol=1e-5, abs_tol=1e-8)):
                    counts['scoreDifferences'] += 1
            for band in ('Rising', 'Established', 'Star', 'Superstar'):
                for live_suffix, offline_suffix in (('Lower', 'Lower'), ('Upper', 'Upper'), ('Boundary', 'Boundary'),
                                                     ('Regional', 'RegionalLower'), ('Record', 'RecordLower'), ('Conversion', 'ConversionLower')):
                    name = band + live_suffix
                    if name in row and row[name] and not math.isclose(float(row[name]), float(other[band + offline_suffix]), rel_tol=1e-5, abs_tol=1e-8):
                        counts['supportDifferences'] += 1
    with decisions.open(newline='', encoding='utf-8-sig') as stream:
        for row in csv.DictReader(stream):
            counts['decisionRows'] += 1
            if row['eligible'] == '1':
                if row['oldMultiplier'] != row['appliedMultiplier']:
                    raise ValueError('Shadow changed applied multiplier')
                if row['readStatus'] == 'Available':
                    age = (parse_date(row['date']) - dt.date.fromisoformat(row['snapshotDate'])).days
                    if not 1 <= age <= 7 or not row['proposedMultiplier']:
                        raise ValueError('Available decision has invalid timing or proposal')
    if not counts['compared'] or not counts['decisionRows']:
        raise ValueError('No post-calibration comparison or decision rows')
    return counts


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('live', type=Path); p.add_argument('components', type=Path); p.add_argument('offline', type=Path)
    p.add_argument('decisions', type=Path)
    p.add_argument('--max-score-differences', type=int, default=0,
                   help='Explicit allowance for separately investigated off-roster continuity differences')
    p.add_argument('--max-support-differences', type=int, default=0)
    a = p.parse_args()
    result = verify(a.live, a.components, a.offline, a.decisions)
    print(result)
    if (result['bandDifferences'] or result['scoreDifferences'] > a.max_score_differences or
            result['supportDifferences'] > a.max_support_differences):
        raise SystemExit('Live/offline differences exceed the stated allowance')
