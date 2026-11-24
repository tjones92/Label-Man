"""Make an offline maximum-grant sales sensitivity trace.

Within each act-region-format, subtract the store-clearing upper bound in the
observed young/adult sales proportions. This is an attribution proxy, not a
counterfactual simulation or an estimate of actual follower purchases.
"""
import argparse
import csv
import json
from pathlib import Path

from stature_normalization import file_digest
from verify_stature_evidence import verify


def discount(young, adult, bound):
    total = young + adult
    if min(young, adult, bound) < 0 or bound > total + max(.01, total * 1e-6):
        raise ValueError('Invalid sales or follower bound')
    if total == 0:
        return young, adult
    factor = max(0., 1. - min(bound, total) / total)
    return young * factor, adult * factor


def transform(source, output, manifest):
    verify(source)
    if output.exists() or manifest.exists():
        raise FileExistsError('Output and manifest must both be new files')
    totals = {'single': 0., 'album': 0.}
    with source.open(newline='', encoding='utf-8-sig') as stream, output.open('x', newline='', encoding='utf-8') as target:
        reader = csv.DictReader(stream)
        required = {p + 'FollowerClearedUpperBound' for p in totals} | {'followerLaunchMode'}
        if not required.issubset(reader.fieldnames or ()):
            raise ValueError('Source lacks follower bounds')
        fields = list(reader.fieldnames) + ['sensitivityOriginal' + p + age + 'Units' for p in ('Single', 'Album') for age in ('Young', 'Adult')]
        writer = csv.DictWriter(target, fieldnames=fields)
        writer.writeheader()
        for row in reader:
            if row['followerLaunchMode'] != 'causal':
                raise ValueError('Sensitivity requires causal follower-launch demand')
            for prefix in totals:
                young, adult = (prefix + age + 'Units' for age in ('Young', 'Adult'))
                row['sensitivityOriginal' + prefix.capitalize() + 'YoungUnits'] = row[young]
                row['sensitivityOriginal' + prefix.capitalize() + 'AdultUnits'] = row[adult]
                bound = float(row[prefix + 'FollowerClearedUpperBound'])
                original_total = float(row[young]) + float(row[adult])
                adjusted = discount(float(row[young]), float(row[adult]), bound)
                row[young], row[adult] = (format(v, '.17g') for v in adjusted)
                totals[prefix] += original_total - sum(adjusted)
            writer.writerow(row)
    result = {'sourceSha256': file_digest(source), 'outputSha256': file_digest(output), 'subtractedUpperBoundByFormat': totals,
              'caveat': 'Artificial measurement sensitivity, not a raw evidence file or counterfactual world. The upper bound is not actual follower sales. Age attribution uses observed sales shares; market denominators, booked joins, following and other evidence stay at observed values. The unchanged historical following may already incorporate follower-generated purchases. Do not run the raw-evidence validator on this transformed trace.'}
    with manifest.open('x', encoding='utf-8') as stream:
        json.dump(result, stream, indent=2, sort_keys=True)
        stream.write('\n')


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('source', type=Path)
    p.add_argument('output', type=Path)
    p.add_argument('manifest', type=Path)
    a = p.parse_args()
    transform(a.source, a.output, a.manifest)
