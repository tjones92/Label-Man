"""Compare complete matched runs; ignore only the opted-in scene metadata/diagnostics."""
import argparse, gzip, hashlib, json
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument('manifest', type=Path)
parser.add_argument('--persistence', action='store_true')
args = parser.parse_args()
manifest = json.loads(args.manifest.read_text(encoding='utf-8-sig'))
tag = args.manifest.parent.name
ignored_fields = {'SceneIdentity', 'geography', 'Geography'}
if args.persistence:
    ignored_fields |= {'ScenePersistence', 'sceneParticipations'}

def digest(path):
    h = hashlib.sha256()
    with path.open('rb') as f:
        for part in iter(lambda: f.read(1024*1024), b''): h.update(part)
    return h.hexdigest()

def csvs(root, name):
    out = {}
    for p in (root/'SimLogs').glob(name+'-*.csv'):
        suffix = p.name[len(name)+1:]
        if suffix.startswith('scene-identity-') or args.persistence and suffix.startswith('scene-participation-'): continue
        out[suffix] = digest(p)
    return out

def normalized(value):
    if isinstance(value, dict): return {k: normalized(v) for k,v in value.items() if k not in ignored_fields}
    if isinstance(value, list): return [normalized(v) for v in value]
    return value

def world(root, name):
    with gzip.open(root/'SimLogs'/'worlds'/(name+'-start.world.json.gz'), 'rt', encoding='utf-8-sig') as f:
        return normalized(json.load(f)['World'])

completed = {r['name'] for r in manifest['results'] if r['passed']}
comparisons = []
for job in manifest['jobs']:
    mode, seed, root = job['mode'], job['seed'], Path(job['root'])
    if mode == 'control': continue
    name = job.get('name', f'{tag}-{mode}-{seed}')
    base_job = next(j for j in manifest['jobs'] if j['mode']=='control' and j['seed']==seed)
    base = base_job.get('name', f'{tag}-control-{seed}')
    if name not in completed or base not in completed: continue
    base_root = Path(next(j['root'] for j in manifest['jobs'] if j['mode']=='control' and j['seed']==seed))
    a, b = csvs(base_root, base), csvs(root, name)
    changed = [s for s in sorted(a.keys() & b.keys()) if a[s]!=b[s]]
    missing = sorted(a.keys()-b.keys()); unexpected = sorted(b.keys()-a.keys())
    identical = len(a.keys() & b.keys()) - len(changed)
    snapshots_match = world(base_root, base) == world(root, name)
    comparisons.append(dict(baseline=base, candidate=name, identicalFiles=identical, changed=changed,
        missing=missing, unexpected=unexpected, normalizedWorldMatch=snapshots_match,
        passed=bool(a) and not changed and not missing and not unexpected and snapshots_match))
report = dict(ignoredMetadata=sorted(ignored_fields), comparisons=comparisons,
    completedRuns=len(completed), expectedRuns=len(manifest['jobs']),
    passed=len(completed)==len(manifest['jobs']) and bool(comparisons) and all(c['passed'] for c in comparisons))
out = args.manifest.parent/'comparison.json'
out.write_text(json.dumps(report, indent=2)+'\n')
print(json.dumps(report, indent=2))
raise SystemExit(0 if report['passed'] else 1)
