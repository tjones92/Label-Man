"""Join an experimental shadow to programme-choice audits using a prior week.

The audit's pipeline week 1 occurs at first-week-date, before that day's
completed-week observation. Only the previous dated snapshot is eligible.
This screens programme candidates, not every forecast or a causal treatment.
"""
import argparse
import csv
import datetime as dt
import json
from collections import Counter
from pathlib import Path

from fit_stature_experiment import is_fit
from stature_normalization import file_digest


# --album-prior-recalibration, live market, no debut-scale override.
TABLE = {'Rising': 2.282491535, 'Established': 3.870309897, 'Star': 4.881536394,
         'Superstar': 4.881536394 * 1.25}
BASE = 1.107276574


def screen(choices, shadow, output, summary, first_day):
    snapshots = {}
    with shadow.open(newline='', encoding='utf-8-sig') as source:
        for row in csv.DictReader(source):
            key = (row['date'], row['artistId'])
            if key in snapshots:
                raise ValueError('Duplicate snapshot')
            snapshots[key] = row
    if not snapshots:
        raise ValueError('No shadow snapshots')
    first_snapshot_date = min(key[0] for key in snapshots)
    counts = Counter()
    cross = Counter()
    fields = ['week', 'date', 'snapshotDate', 'labelId', 'tier', 'artistId', 'eligible', 'selected', 'reason',
              'legacyState', 'shadowBand', 'shadowStatus', 'oldMultiplier', 'candidateMultiplier', 'appliedMultiplier',
              'expectedNet', 'qualityEstimate', 'slotId']
    with choices.open(newline='', encoding='utf-8-sig') as source, output.open('x', newline='', encoding='utf-8') as target:
        writer = csv.DictWriter(target, fieldnames=fields); writer.writeheader()
        for row in csv.DictReader(source):
            if row['format'] != 'Album':
                continue
            week = int(row['week'])
            date = first_day + dt.timedelta(days=7 * (week - 1))
            snapshot_date = date - dt.timedelta(days=7)
            artist = row['artistId']
            old = TABLE.get(row['careerState'], BASE) if row['eligible'] == '1' else None
            snapshot = snapshots.get((snapshot_date.isoformat(), artist))
            reason = ('ineligible' if row['eligible'] != '1' else 'fit_partition' if is_fit(artist) else
                      'before_calibration' if snapshot_date.isoformat() < first_snapshot_date else
                      'snapshot_unavailable' if snapshot is None else snapshot['status'])
            if row['eligible'] == '1' and row['careerState'] not in ('NewSigning', *TABLE):
                reason = 'lifecycle_rule_unresolved'
            candidate = TABLE.get(snapshot['band']) if snapshot and reason == 'observed' else None
            # Below Rising has no approved mapping to a lifecycle/contract state.
            if reason == 'below_rising':
                if row['careerState'] == 'NewSigning':
                    reason = 'below_rising_preserved_newsigning'
                    candidate = old
                else:
                    reason = 'below_rising_mapping_unresolved'
            counts['all_album_candidates'] += 1
            counts['eligible' if row['eligible'] == '1' else 'ineligible'] += 1
            counts['status:' + reason] += 1
            if candidate is not None:
                counts['comparable'] += 1
                counts['same_table' if candidate == old else 'different_table'] += 1
                counts['higher' if candidate > old else 'lower' if candidate < old else 'equal'] += 1
                cross[(row['tier'], row['careerState'], snapshot['band'], row['selected'])] += 1
            writer.writerow(dict(week=week, date=date.isoformat(), snapshotDate=snapshot_date.isoformat(),
                labelId=row['labelId'], tier=row['tier'], artistId=artist, eligible=row['eligible'], selected=row['selected'],
                reason=reason, legacyState=row['careerState'], shadowBand=snapshot['band'] if snapshot else '',
                shadowStatus=snapshot['status'] if snapshot else '', oldMultiplier=old, candidateMultiplier=candidate,
                appliedMultiplier=old, expectedNet=row['expectedNet'], qualityEstimate=row['qualityEstimate'], slotId=row['slotId']))
    report = {'counts': dict(counts), 'choicesSha256': file_digest(choices), 'shadowSha256': file_digest(shadow),
              'timing': 'strictly previous pipeline-week snapshot', 'table': TABLE, 'baseMultiplier': BASE,
              'crossTab': [{'tier': tier, 'legacy': legacy, 'shadow': band, 'selected': selected, 'count': count}
                           for (tier, legacy, band, selected), count in sorted(cross.items())],
              'caveat': 'Programme album candidates only; shadow is offline and candidate values were not applied. Below Rising preserves NewSigning; other lifecycle mappings unresolved.'}
    with summary.open('x', encoding='utf-8') as target:
        json.dump(report, target, indent=2, sort_keys=True); target.write('\n')


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('choices', type=Path); p.add_argument('shadow', type=Path); p.add_argument('output', type=Path)
    p.add_argument('summary', type=Path); p.add_argument('--first-week-date', required=True)
    a = p.parse_args(); screen(a.choices, a.shadow, a.output, a.summary, dt.date.fromisoformat(a.first_week_date))
