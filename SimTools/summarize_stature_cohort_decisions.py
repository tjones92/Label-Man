"""Summarize observed album candidates by first-observed singles-chart cohort."""
import argparse
import csv
import json
from collections import Counter, defaultdict
from pathlib import Path

from stature_normalization import file_digest, parse_date


def summarize(decisions, cohort_file, output):
    cohort_data = json.loads(cohort_file.read_text(encoding='utf-8-sig'))
    cohorts = {item['artistId']: item for item in cohort_data['cohorts']}
    groups = defaultdict(Counter)
    with decisions.open(newline='', encoding='utf-8-sig') as stream:
        for row in csv.DictReader(stream):
            date, week = parse_date(row['date']), int(row['week'])
            cohort = cohorts.get(row['artistId'])
            if cohort is None or cohort['availableFromDecisionWeek'] > week:
                cohort_name = 'not_yet_observed_on_chart'
            else:
                suffix = 'run_start_censored' if cohort['leftCensoredAtRunStart'] else 'observed_after_start'
                cohort_name = f"{cohort['firstObservedChartYear']}_{suffix}"
            key = (date.year, cohort_name, row['tier'])
            group = groups[key]
            group['allCandidates'] += 1
            group['eligible' if row['eligible'] == '1' else 'ineligible'] += 1
            if row['eligible'] != '1':
                continue
            group['readStatus:' + row['readStatus']] += 1
            if row['appliedMultiplier'] != row['oldMultiplier']:
                raise ValueError('Observation audit did not preserve the old multiplier')
            if row['oldMultiplier'] and row['proposedMultiplier']:
                old, proposed = float(row['oldMultiplier']), float(row['proposedMultiplier'])
                group['comparable'] += 1
                group['higher' if proposed > old else 'lower' if proposed < old else 'equal'] += 1
    if not groups:
        raise ValueError('No decision rows')
    result = {'decisionsSha256': file_digest(decisions), 'cohortsSha256': file_digest(cohort_file),
              'groups': [{'year': year, 'cohort': cohort, 'tier': tier, 'counts': dict(sorted(counts.items()))}
                         for (year, cohort, tier), counts in sorted(groups.items())],
              'caveat': 'Programme album-candidate observations, including repeated candidates and ineligible rows; not releases or distinct acts. First-observed singles chart is not first career chart. Later chart observations are unavailable until the next decision week. Pre-run history remains unknown, and proposed values are not applied.'}
    with output.open('x', encoding='utf-8') as stream:
        json.dump(result, stream, indent=2, sort_keys=True)
        stream.write('\n')


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('decisions', type=Path); p.add_argument('cohorts', type=Path); p.add_argument('output', type=Path)
    a = p.parse_args(); summarize(a.decisions, a.cohorts, a.output)
