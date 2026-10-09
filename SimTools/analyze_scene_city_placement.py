"""Bounded city-placement audit: opening cohorts plus a full-year recruitment-off control."""
import argparse
import csv
import hashlib
import json
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument('manifest', type=Path)
parser.add_argument('--previous-tag', default='scene4-initialization-audit-v1')
args = parser.parse_args()
manifest = json.loads(args.manifest.read_text(encoding='utf-8-sig'))
root = args.manifest.resolve().parents[2] / 'SimLogs'

def rows(name, suffix):
    with (root / f'{name}-{suffix}.csv').open(encoding='utf-8-sig', newline='') as f:
        return list(csv.DictReader(f))

def hashes(name):
    result = {}
    for path in root.glob(name + '-*.csv'):
        suffix = path.name[len(name) + 1:]
        if suffix.startswith(('scene-identity-', 'scene-participation-', 'scene-room-', 'scene-work-', 'scene-recruitment-', 'scene-initialization-')):
            continue
        result[suffix] = hashlib.sha256(path.read_bytes()).hexdigest()
    return result

report = dict(controls=[], opening=[], longHorizonRecruitmentAcceptance=False)
for job in manifest['results']:
    assert job['passed'], 'Incomplete audit'
    name, seed = job['name'], job['seed']
    old_name = f'{args.previous_tag}-{job["mode"]}-{seed}'
    if job['mode'] == 'off':
        old, new = hashes(old_name), hashes(name)
        changed = [s for s in sorted(old.keys() & new.keys()) if old[s] != new[s]]
        result = dict(name=name, weeks=job['weeks'], identicalEconomicFiles=len(old.keys() & new.keys())-len(changed),
                      changed=changed, missing=sorted(old.keys()-new.keys()), unexpected=sorted(new.keys()-old.keys()))
        assert job['weeks'] == 52 and len(old) == len(new) == 84 and not changed and old.keys() == new.keys(), result
        report['controls'].append(result)
    else:
        old = rows(old_name, 'scene-initialization-labels')
        new = rows(name, 'scene-initialization-labels')
        places = {r['id']: r['basePlaceId'] for r in rows(name,'scene-identity-start') if r['entity']=='act'}
        prior_places = {r['id']: r['basePlaceId'] for r in rows(old_name,'scene-identity-start') if r['entity']=='act'}
        launch_ids = {f'artist_{i:05d}' for i in range(1,3001)}
        assert launch_ids <= places.keys()
        appointments = rows(name,'scene-initialization-appointments')
        assert len(appointments) == len({a['artistId'] for a in appointments})
        assert len(appointments) == len({(a['round'],a['labelId']) for a in appointments})
        city_counts = []
        for city in sorted(set(places.values())):
            count = sum(places[a] == city for a in launch_ids)
            city_counts.append(dict(city=city, launchActs=count, launchPercent=100*count/3000,
                                    priorLaunchActs=sum(prior_places[a] == city for a in launch_ids),
                                    registryStart=sum(p == city for p in places.values())))
        city_counts.sort(key=lambda r: -r['launchActs'])
        assert sum(r['launchActs'] for r in city_counts) == 3000
        counts = {r['city']:r['launchActs'] for r in city_counts}
        assert counts['new_york'] > 20*counts.get('billings',0)
        assert sum(int(r['filled'])<int(r['target']) and int(r['accessibleAny'])>0 for r in new) == 0
        report['opening'].append(dict(seed=seed, cities=city_counts,
            previousFilled=sum(int(r['filled']) for r in old), filled=sum(int(r['filled']) for r in new),
            previousEmpty=sum(int(r['filled'])==0 for r in old), empty=sum(int(r['filled'])==0 for r in new),
            domesticMajorUnderfill=[r['labelId'] for r in new if r['tier']=='Major' and r['hqPlaceId'] and not r['hqPlaceId'].startswith('gb_') and int(r['filled'])<int(r['target'])]))
assert len(report['controls']) == 1 and len(report['opening']) == 2
report['passed'] = True
out = args.manifest.parent / 'analysis.json'
out.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print(json.dumps(report,indent=2))
