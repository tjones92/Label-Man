"""Compare weekly shadow transitions with observed Top 40 entry events.

Entries are chart observations, not the legacy promotion rule's full milestone
history. Record-level re-entry also qualifies. This is descriptive, not causal.
"""
import argparse
import csv
import datetime as dt
import json
from collections import defaultdict
from pathlib import Path

from stature_normalization import file_digest
from stature_shadow_policy import BANDS


def scan(records, shadow, output, first_day):
    entries = defaultdict(set)
    with records.open(newline='', encoding='utf-8-sig') as stream:
        for row in csv.DictReader(stream):
            artist = row['artistId']
            if not artist:
                continue
            position = int(row['currentPosition'] or 0)
            previous = int(row['previousPosition'] or 0)
            if 1 <= position <= 40 and (previous <= 0 or previous > 40):
                entries[artist].add(int(row['week']))
    transitions, weeks, observed = [], {}, set()
    with shadow.open(newline='', encoding='utf-8-sig') as stream:
        for row in csv.DictReader(stream):
            if row['status'] == 'missing_evidence':
                continue
            day = dt.date.fromisoformat(row['date'])
            offset = (day - first_day).days
            if offset < 0 or offset % 7:
                raise ValueError('Snapshot date does not match weekly clock')
            week = offset // 7 + 1
            artist = row['artistId']
            observed.add((artist, week))
            band = row['band'] or 'BelowRising'
            prior = weeks.get(artist)
            weeks[artist] = (week, band)
            if prior is None or prior[0] != week - 1 or prior[1] == band:
                continue
            def level(value):
                return BANDS.index(value) + 1 if value in BANDS else 0
            transitions.append({'artistId': artist, 'date': row['date'], 'week': week,
                'from': prior[1], 'to': band, 'direction': 'up' if level(band) > level(prior[1]) else 'down',
                'top40EntrySameWeek': week in entries[artist],
                'top40EntryLastFourWeeks': any(w in entries[artist] for w in range(week - 3, week + 1))})
    transition_pairs = {(x['artistId'], x['week']) for x in transitions}
    observed_entries = {(artist, week) for artist, values in entries.items() for week in values if (artist, week) in observed}
    summary = {'shadowSha256': file_digest(shadow), 'recordsSha256': file_digest(records),
        'transitions': len(transitions), 'up': sum(x['direction'] == 'up' for x in transitions),
        'down': sum(x['direction'] == 'down' for x in transitions),
        'sameWeekTop40Entry': sum(x['top40EntrySameWeek'] for x in transitions),
        'lastFourWeeksTop40Entry': sum(x['top40EntryLastFourWeeks'] for x in transitions),
        'observedArtistWeeksWithTop40Entry': len(observed_entries),
        'entryArtistWeeksWithoutTransition': len(observed_entries - transition_pairs),
        'details': transitions, 'caveat': 'Observed record Top 40 entry/re-entry, not complete legacy milestone history; held-out snapshots only.'}
    with output.open('x', encoding='utf-8') as stream:
        json.dump(summary, stream, indent=2, sort_keys=True); stream.write('\n')


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('records', type=Path); p.add_argument('shadow', type=Path); p.add_argument('output', type=Path)
    p.add_argument('--first-week-date', required=True)
    a = p.parse_args(); scan(a.records, a.shadow, a.output, dt.date.fromisoformat(a.first_week_date))
