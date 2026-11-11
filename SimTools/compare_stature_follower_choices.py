"""Compare offline programme-candidate mappings under a sales sensitivity."""
import argparse
import csv
import json
from collections import Counter
from pathlib import Path

from stature_normalization import file_digest


def read(path):
    rows = {}
    with path.open(newline='', encoding='utf-8-sig') as stream:
        for row in csv.DictReader(stream):
            key = (row['week'], row['labelId'], row['slotId'], row['artistId'])
            if key in rows:
                raise ValueError('Duplicate programme candidate')
            rows[key] = row
    return rows


def compare(control, adjusted, output, comparison_kind='follower'):
    if comparison_kind not in ('follower', 'reference'):
        raise ValueError('Unknown comparison kind')
    left, right = read(control), read(adjusted)
    if not left or left.keys() != right.keys():
        raise ValueError('Programme candidate sets must be identical and nonempty')
    counts = Counter({k: 0 for k in ('allCandidates', 'eligible', 'jointlyComparable', 'differentCandidateValue',
                                     'lowerCandidateValue', 'higherCandidateValue', 'availabilityChanged')})
    by_tier = Counter()
    for key in left:
        a, b = left[key], right[key]
        if any(a[field] != b[field] for field in ('eligible', 'selected', 'legacyState', 'oldMultiplier', 'appliedMultiplier')):
            raise ValueError('Candidate control inputs differ')
        counts['allCandidates'] += 1
        if a['eligible'] != '1':
            continue
        counts['eligible'] += 1
        va, vb = a['candidateMultiplier'], b['candidateMultiplier']
        if bool(va) != bool(vb):
            counts['availabilityChanged'] += 1
        if va and vb:
            va, vb = float(va), float(vb)
            counts['jointlyComparable'] += 1
            by_tier[(a['tier'], 'comparable')] += 1
            if va != vb:
                counts['differentCandidateValue'] += 1
                counts['lowerCandidateValue' if vb < va else 'higherCandidateValue'] += 1
                by_tier[(a['tier'], 'different')] += 1
    caveat = ('The maximum-grant discount is a proxy, not a causal treatment.' if comparison_kind == 'follower' else
              'The two separately fitted references and thresholds are experimental alternatives, not causal treatments.')
    result = {'controlSha256': file_digest(control), 'adjustedSha256': file_digest(adjusted), 'comparisonKind': comparison_kind,
              'counts': dict(sorted(counts.items())),
              'tierCounts': [{'tier': t, 'kind': k, 'count': n} for (t, k), n in sorted(by_tier.items())],
              'caveat': 'Programme album-candidate observations, not distinct acts or realized releases. Both values are observational proposals; actual applied values remain the original rung table. Ineligible, unresolved lifecycle and unavailable observations remain outside jointly comparable values. ' + caveat}
    with output.open('x', encoding='utf-8') as stream:
        json.dump(result, stream, indent=2, sort_keys=True)
        stream.write('\n')


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('control', type=Path)
    p.add_argument('adjusted', type=Path)
    p.add_argument('output', type=Path)
    p.add_argument('--kind', choices=('follower', 'reference'), default='follower')
    a = p.parse_args()
    compare(a.control, a.adjusted, a.output, a.kind)
