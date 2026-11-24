"""Compare fitted fixed and lagged reference shadows by calendar year."""
import argparse
import csv
import json
from collections import Counter, defaultdict
from pathlib import Path

from stature_normalization import file_digest
from stature_shadow_policy import BANDS


def read(path):
    rows = {}
    with path.open(newline='', encoding='utf-8-sig') as stream:
        for row in csv.DictReader(stream):
            key = (row['date'], row['artistId'])
            if key in rows:
                raise ValueError('Duplicate shadow observation')
            rows[key] = row
    return rows


def compare(fixed, peer, output):
    left, right = read(fixed), read(peer)
    if not left or left.keys() != right.keys():
        raise ValueError('Shadow observations must be nonempty and match')
    counts, pairs = defaultdict(Counter), defaultdict(Counter)
    for (date, artist), a in left.items():
        b = right[(date, artist)]
        if a['careerState'] != b['careerState']:
            raise ValueError('Legacy state differs across shadows')
        year = date[:4]
        counts[year]['observations'] += 1
        known_a, known_b = a['status'] != 'missing_evidence', b['status'] != 'missing_evidence'
        if not known_a or not known_b:
            counts[year]['fixedMissing'] += int(not known_a)
            counts[year]['peerMissing'] += int(not known_b)
            continue
        ba, bb = a['band'] or 'BelowRising', b['band'] or 'BelowRising'
        counts[year]['comparable'] += 1
        counts[year]['differentBand'] += int(ba != bb)
        if a['careerState'] in BANDS:
            counts[year]['legacyComparable'] += 1
            counts[year]['fixedDiffersFromLegacy'] += int(ba != a['careerState'])
            counts[year]['peerDiffersFromLegacy'] += int(bb != a['careerState'])
        pairs[year][(ba, bb)] += 1
    years = []
    for year in sorted(counts):
        years.append({'year': year, 'counts': dict(sorted(counts[year].items())),
                      'bandPairs': [{'fixed': a, 'peer': b, 'artistWeeks': n} for (a, b), n in sorted(pairs[year].items())]})
    with output.open('x', encoding='utf-8') as stream:
        json.dump({'fixedSha256': file_digest(fixed), 'peerSha256': file_digest(peer), 'years': years,
            'caveat': 'Both are exploratory, separately fitted policies on held-out artist IDs; roster-only offline histories reset at gaps. Counts are artist-weeks, not decisions or distinct acts. Different bands reflect both reference and fitted thresholds, not a causal treatment.'}, stream, indent=2, sort_keys=True)
        stream.write('\n')


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('fixed', type=Path); p.add_argument('peer', type=Path); p.add_argument('output', type=Path)
    a = p.parse_args(); compare(a.fixed, a.peer, a.output)
