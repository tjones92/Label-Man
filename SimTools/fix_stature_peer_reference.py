"""Use baseline lagged peer references for a discounted component trace.

This isolates reference movement in an offline sensitivity. It keeps the
discounted smoothed response and the baseline's prior-week reference; it does
not claim the discounted series is a counterfactual simulated world.
"""
import argparse
import csv
import json
from itertools import zip_longest
from pathlib import Path

from stature_normalization import file_digest, normalize


def fix(baseline, adjusted, output, manifest):
    if output.exists() or manifest.exists():
        raise FileExistsError('Output and manifest must both be new files')
    rows = 0
    with baseline.open(newline='', encoding='utf-8-sig') as a, adjusted.open(newline='', encoding='utf-8-sig') as b, output.open('x', newline='', encoding='utf-8') as target:
        left, right = csv.DictReader(a), csv.DictReader(b)
        if left.fieldnames != right.fieldnames:
            raise ValueError('Component schemas differ')
        writer = csv.DictWriter(target, fieldnames=right.fieldnames)
        writer.writeheader()
        for x, y in zip_longest(left, right):
            if x is None or y is None or any(x[k] != y[k] for k in ('date', 'artistId', 'component')):
                raise ValueError('Component keys or order differ')
            reference = float(x['peerReference']) if x['peerReference'] else None
            value = float(y['smoothed']) if y['smoothed'] else None
            score = normalize(value, reference)
            for key in ('peerReference', 'peerReferenceDate', 'peerCount', 'peerStatus'):
                y[key] = x[key]
            y['peerScore'] = '' if score is None else format(score, '.17g')
            writer.writerow(y)
            rows += 1
    if not rows:
        raise ValueError('No component observations')
    with manifest.open('x', encoding='utf-8') as stream:
        json.dump({'baselineSha256': file_digest(baseline), 'adjustedSha256': file_digest(adjusted),
                   'outputSha256': file_digest(output), 'rows': rows,
                   'caveat': 'Offline adjusted response normalized against the baseline prior-week peer reference. Regional reach is still separately recalculated and following remains observed.'}, stream, indent=2, sort_keys=True)
        stream.write('\n')


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('baseline', type=Path)
    p.add_argument('adjusted', type=Path)
    p.add_argument('output', type=Path)
    p.add_argument('manifest', type=Path)
    a = p.parse_args()
    fix(a.baseline, a.adjusted, a.output, a.manifest)
