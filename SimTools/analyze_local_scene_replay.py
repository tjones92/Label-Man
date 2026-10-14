"""Validate exact legacy replay, metadata conservation and canonical scene references."""
import argparse, gc, gzip, hashlib, json
from pathlib import Path

p = argparse.ArgumentParser()
p.add_argument('root', type=Path)
p.add_argument('tag')
p.add_argument('legacy_world', type=Path)
a = p.parse_args()

def sha(path, compressed=False):
    h = hashlib.sha256()
    with (gzip.open(path, 'rb') if compressed else path.open('rb')) as f:
        for block in iter(lambda: f.read(1024*1024), b''): h.update(block)
    return h.hexdigest()

def files(side):
    prefix = f'{a.tag}-{side}-'
    return {f.name[len(prefix):]: sha(f) for f in (a.root/'SimLogs').glob(prefix+'*.csv')}
x, y = files('a'), files('b')
changed = sorted(k for k in x.keys() & y.keys() if x[k] != y[k])
missing = sorted(x.keys() ^ y.keys())
wa = a.root/'SimLogs'/'worlds'/f'{a.tag}-a-migrated.world.json.gz'
wb = a.root/'SimLogs'/'worlds'/f'{a.tag}-b-migrated.world.json.gz'
world_equal = sha(wa, True) == sha(wb, True)
with gzip.open(a.legacy_world, 'rt', encoding='utf-8-sig') as f:
    legacy = json.load(f)['World']
legacy_ids = {actor['artistId'] for actor in legacy['Artists']}
del legacy; gc.collect()
with gzip.open(wa, 'rt', encoding='utf-8-sig') as f:
    migrated = json.load(f)['World']
actors = {actor['artistId']: actor for actor in migrated['Artists']}
lost = sorted(legacy_ids-actors.keys())
false_origins = sorted(i for i in legacy_ids & actors.keys() if actors[i].get('geography', {}).get('originPlaceId'))
unbased = sorted(i for i in legacy_ids & actors.keys() if not actors[i].get('geography', {}).get('basePlaceId'))
windows = migrated['ScenePersistence']['Windows']
missing_refs = sorted({i for window in windows for i in window['ArtistIds'] if i not in actors})
duplicate_windows = len(windows) - len({(w['SceneId'], w['Venue'], w['Week']) for w in windows})
report = dict(csvFiles=len(x), different=changed, missing=missing, exactMigratedWorldMatch=world_equal,
    legacyActors=len(legacy_ids), migratedActors=len(actors), lostActors=lost, manufacturedLegacyOrigins=false_origins,
    unresolvedLegacyBases=unbased, castWindows=len(windows), duplicateWindows=duplicate_windows, missingCastRefs=missing_refs,
    passed=bool(x) and not changed and not missing and world_equal and not lost and not false_origins and
        not unbased and not missing_refs and duplicate_windows == 0)
(a.root/'SimLogs'/a.tag/'comparison.json').write_text(json.dumps(report, indent=2)+'\n')
print(json.dumps(report, indent=2))
raise SystemExit(0 if report['passed'] else 1)
