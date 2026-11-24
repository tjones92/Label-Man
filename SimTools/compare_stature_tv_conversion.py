"""Compare estimated ordinary-TV registrations with booked following joins.

The model assigns joins proportionally across sources after shared saturation.
Neither input is a measured individual-level conversion outcome.
"""
import argparse
import csv
import json
from collections import defaultdict
from pathlib import Path

from stature_normalization import file_digest, parse_date
from verify_stature_evidence import verify


def percentile(values, fraction):
    if not values:
        return None
    values = sorted(values)
    return values[round((len(values) - 1) * fraction)]


def compare(source, output):
    verify(source)
    observations = defaultdict(lambda: [0., 0., 0., 0.])
    with source.open(newline='', encoding='utf-8-sig') as stream:
        reader = csv.DictReader(stream)
        fields = ('tvRegisteredYoungEstimate', 'tvRegisteredAdultEstimate', 'tvJoinYoung', 'tvJoinAdult')
        if not set(fields).issubset(reader.fieldnames or ()):
            raise ValueError('Source lacks TV registration or booked-join columns')
        for row in reader:
            values = observations[(parse_date(row['date']).isoformat(), row['artistId'])]
            for index, field in enumerate(fields):
                values[index] += float(row[field])
    annual = {}
    for year in sorted({date[:4] for date, _ in observations}):
        group = [v for (date, _), v in observations.items() if date.startswith(year)]
        ages = {}
        for index, name in enumerate(('young', 'adult')):
            pairs = [(v[index], v[index + 2]) for v in group if v[index] > 0]
            ratios = [joined / registration for registration, joined in pairs]
            ages[name] = {'artistWeeksWithRegistration': len(pairs),
                          'artistWeeksWithRegistrationAndNoBookedJoin': sum(joined == 0 for _, joined in pairs),
                          'registrationEstimateTotal': sum(registration for registration, _ in pairs),
                          'bookedJoinTotal': sum(joined for _, joined in pairs),
                          'unweightedJoinPerRegistration': {f'p{p}': percentile(ratios, p / 100) for p in (0, 10, 50, 90, 100)}}
        annual[year] = ages
    result = {'sourceSha256': file_digest(source), 'artistWeeks': len(observations), 'years': annual,
              'caveat': 'AI roster only; ratios are descriptive, unweighted artist-weeks with positive modeled registrations. Shared saturation allocates joins proportionally across offers. A zero can include a discarded sub-threshold following; joining and later retention are distinct.'}
    with output.open('x', encoding='utf-8') as stream:
        json.dump(result, stream, indent=2, sort_keys=True)
        stream.write('\n')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source', type=Path)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    compare(args.source, args.output)
