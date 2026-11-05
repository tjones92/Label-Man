"""Attach observed sales, following and TV-registration estimates to samples."""
import argparse
import csv
import json
from pathlib import Path

from stature_normalization import parse_date


def enrich(evidence, sample_manifest, trajectories, output):
    selection = json.loads(sample_manifest.read_text(encoding='utf-8-sig'))['selectedArtistIds']
    selected = set(selection.values())
    observations = {}
    with evidence.open(newline='', encoding='utf-8-sig') as stream:
        for row in csv.DictReader(stream):
            artist = row['artistId']
            if artist not in selected:
                continue
            key = (parse_date(row['date']).isoformat(), artist)
            obs = observations.setdefault(key, {'artist': row['artist'], 'labelId': row['labelId'], 'tier': row['labelTier'],
                'genre': row['genre'], 'followingStatus': row['followingStatus'], 'salesSingle': 0., 'salesAlbum': 0.,
                'tvYoungEstimate': 0., 'tvAdultEstimate': 0., 'followingYoung': 0., 'followingAdult': 0.,
                'singleIntent': 0., 'albumIntent': 0.})
            for output_key, columns in {'salesSingle': ('singleYoungUnits', 'singleAdultUnits'),
                'salesAlbum': ('albumYoungUnits', 'albumAdultUnits'), 'tvYoungEstimate': ('tvRegisteredYoungEstimate',),
                'tvAdultEstimate': ('tvRegisteredAdultEstimate',), 'followingYoung': ('followingYoung',),
                'followingAdult': ('followingAdult',), 'singleIntent': ('singleAdvanceIntent',),
                'albumIntent': ('albumAdvanceIntent',)}.items():
                obs[output_key] += sum(float(row[column]) for column in columns)
    with trajectories.open(newline='', encoding='utf-8-sig') as stream, output.open('x', newline='', encoding='utf-8') as target:
        reader = csv.DictReader(stream)
        extra = ['categories', 'artist', 'labelId', 'tier', 'genre', 'followingStatus', 'salesSingle', 'salesAlbum',
                 'tvYoungEstimate', 'tvAdultEstimate', 'followingYoung', 'followingAdult', 'singleIntent', 'albumIntent']
        writer = csv.DictWriter(target, fieldnames=reader.fieldnames + extra); writer.writeheader()
        for row in reader:
            artist = row['artistId']
            obs = observations.get((row['date'], artist))
            if obs is None:
                raise ValueError('Selected trajectory lacks raw observation')
            row.update(obs); row['categories'] = '|'.join(k for k, v in sorted(selection.items()) if v == artist)
            writer.writerow(row)


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('evidence', type=Path); p.add_argument('manifest', type=Path)
    p.add_argument('trajectories', type=Path); p.add_argument('output', type=Path)
    a = p.parse_args(); enrich(a.evidence, a.manifest, a.trajectories, a.output)
