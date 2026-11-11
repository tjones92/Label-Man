"""Compare two offline shadows for a maximum-grant sales sensitivity.

Each input must be computed independently with the same frozen policy and
reference settings. No result is a causal intervention or actual attribution.
"""
import argparse
import csv
import datetime as dt
import json
from collections import Counter
from pathlib import Path

from stature_normalization import file_digest
from stature_shadow_policy import BANDS


def read(path):
    result = {}
    with path.open(newline='', encoding='utf-8-sig') as stream:
        for row in csv.DictReader(stream):
            key = (row['date'], row['artistId'])
            if key in result:
                raise ValueError('Duplicate shadow observation')
            result[key] = row
    return result


def compare(baseline, sensitivity, output):
    left, right = read(baseline), read(sensitivity)
    if not left or left.keys() != right.keys():
        raise ValueError('Shadows must have the same nonempty observation keys')
    counts = Counter({key: 0 for key in ('observations', 'availabilityChanged', 'comparable', 'differentBand',
        'lowerAfterDiscount', 'higherAfterDiscount', 'baselineRises', 'sensitivityRises', 'bothRise',
        'baselineOnlyRise', 'sensitivityOnlyRise')})
    bands, previous = Counter(), {}
    for date, artist in sorted(left):
        day = dt.date.fromisoformat(date)
        a, b = left[(date, artist)], right[(date, artist)]
        counts['observations'] += 1
        valid_a, valid_b = a['status'] != 'missing_evidence', b['status'] != 'missing_evidence'
        if valid_a != valid_b:
            counts['availabilityChanged'] += 1
        if valid_a and valid_b:
            band_a, band_b = a['band'] or 'BelowRising', b['band'] or 'BelowRising'
            levels = ('BelowRising', *BANDS)
            level_a, level_b = levels.index(band_a), levels.index(band_b)
            counts['comparable'] += 1
            bands[(band_a, band_b)] += 1
            if level_a != level_b:
                counts['differentBand'] += 1
                counts['lowerAfterDiscount' if level_b < level_a else 'higherAfterDiscount'] += 1
            old = previous.get(artist)
            if old is not None and day - old[0] == dt.timedelta(days=7):
                up_a, up_b = level_a > old[1], level_b > old[2]
                counts['baselineRises'] += int(up_a)
                counts['sensitivityRises'] += int(up_b)
                counts['bothRise'] += int(up_a and up_b)
                counts['baselineOnlyRise'] += int(up_a and not up_b)
                counts['sensitivityOnlyRise'] += int(up_b and not up_a)
            previous[artist] = (day, level_a, level_b)
        else:
            previous.pop(artist, None)
    report = {'baselineSha256': file_digest(baseline), 'sensitivitySha256': file_digest(sensitivity),
              'counts': dict(sorted(counts.items())),
              'bandPairs': [{'baseline': a, 'sensitivity': b, 'artistWeeks': n} for (a, b), n in sorted(bands.items())],
              'caveat': 'Matched offline artist-week observations; rises require consecutive comparable determinate weeks in each independently hysteretic shadow. Maximum-grant discount and modeled age split are proxies; unchanged following can retain earlier feedback. Changes in peer references can raise a relative score when some raw sales are discounted. No simulation decision or behavior is changed.'}
    with output.open('x', encoding='utf-8') as stream:
        json.dump(report, stream, indent=2, sort_keys=True)
        stream.write('\n')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('baseline', type=Path)
    parser.add_argument('sensitivity', type=Path)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    compare(args.baseline, args.sensitivity, args.output)
