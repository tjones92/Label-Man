"""Select deterministic, descriptive weekly shadow trajectories for inspection."""
import argparse
import csv
import datetime as dt
import json
from collections import defaultdict
from pathlib import Path

from stature_shadow_policy import BANDS
from stature_normalization import file_digest


def sample(source, output, manifest):
    acts = defaultdict(list)
    with source.open(newline='', encoding='utf-8-sig') as stream:
        for row in csv.DictReader(stream):
            acts[row['artistId']].append(row)
    choices = {}
    for artist, rows in sorted(acts.items()):
        rows.sort(key=lambda r: r['date'])
        segments, segment, previous = [], [], None
        for row in rows:
            day = dt.date.fromisoformat(row['date'])
            if previous is not None and day == previous:
                raise ValueError(f'Duplicate artist observation: {artist} {day}')
            if previous is None or (day - previous).days != 7 or row['status'] == 'missing_evidence':
                if segment:
                    segments.append(segment)
                segment = []
            if row['status'] != 'missing_evidence':
                segment.append(row)
            previous = day
        if segment:
            segments.append(segment)
        definite = [r for segment in segments for r in segment]
        for segment in segments:
            if len(segment) < 5:
                continue
            levels = [BANDS.index(r['band']) + 1 if r['band'] else 0 for r in segment]
            if any(b > a for a, b in zip(levels, levels[1:])):
                choices.setdefault('rise', artist)
            if any(b < a for a, b in zip(levels, levels[1:])):
                choices.setdefault('decline', artist)
            if any(levels[i] < max(levels[:i]) and max(levels[i + 1:]) > levels[i]
                   for i in range(1, len(levels) - 1)):
                choices.setdefault('comeback', artist)
        if len(definite) < 5:
            continue
        for route in ('regional', 'record', 'conversion'):
            if any(r['band'] and r[r['band'] + 'LeadingRoute'] == route for r in definite):
                choices.setdefault(route, artist)
    selected = set(choices.values())
    with source.open(newline='', encoding='utf-8-sig') as stream, output.open('x', newline='', encoding='utf-8') as target:
        reader = csv.DictReader(stream)
        writer = csv.DictWriter(target, fieldnames=reader.fieldnames); writer.writeheader()
        for row in reader:
            if row['artistId'] in selected:
                writer.writerow(row)
    with manifest.open('x', encoding='utf-8') as stream:
        json.dump({'sourceSha256': file_digest(source), 'selectedArtistIds': choices, 'unobservedCategories': [x for x in
            ('rise', 'decline', 'comeback', 'regional', 'record', 'conversion') if x not in choices],
            'caveat': 'First matching artists in ID order; rises, declines and reversals require a consecutive determinate segment of at least five weeks. Route leadership may reflect durable draw. Initial standing and exposures may predate the observed window; selection does not establish a career-long comeback.'},
            stream, indent=2, sort_keys=True); stream.write('\n')


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('source', type=Path); p.add_argument('output', type=Path); p.add_argument('manifest', type=Path)
    a = p.parse_args(); sample(a.source, a.output, a.manifest)
