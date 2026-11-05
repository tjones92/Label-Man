"""Select deterministic, descriptive weekly shadow trajectories for inspection."""
import argparse
import csv
import json
from collections import defaultdict
from pathlib import Path

from stature_shadow_policy import BANDS


def sample(source, output, manifest):
    acts = defaultdict(list)
    with source.open(newline='', encoding='utf-8-sig') as stream:
        for row in csv.DictReader(stream):
            acts[row['artistId']].append(row)
    choices = {}
    for artist, rows in sorted(acts.items()):
        definite = [r for r in rows if r['status'] != 'missing_evidence']
        if len(definite) < 5:
            continue
        levels = [BANDS.index(r['band']) + 1 if r['band'] else 0 for r in definite]
        if any(b > a for a, b in zip(levels, levels[1:])):
            choices.setdefault('rise', artist)
        if any(b < a for a, b in zip(levels, levels[1:])):
            choices.setdefault('decline', artist)
        if any(levels[i] < max(levels[:i]) and max(levels[i + 1:]) > levels[i]
               for i in range(1, len(levels) - 1)):
            choices.setdefault('comeback', artist)
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
        json.dump({'selectedArtistIds': choices, 'unobservedCategories': [x for x in
            ('rise', 'decline', 'comeback', 'regional', 'record', 'conversion') if x not in choices],
            'caveat': 'First matching held-out artists in ID order, descriptive six-month window; initial standing and exposures may predate it.'},
            stream, indent=2, sort_keys=True); stream.write('\n')


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('source', type=Path); p.add_argument('output', type=Path); p.add_argument('manifest', type=Path)
    a = p.parse_args(); sample(a.source, a.output, a.manifest)
