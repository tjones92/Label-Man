"""Describe seed sensitivity of opening major fill without retuning population or allocation."""
import argparse
import csv
import json
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument('manifests', type=Path, nargs='+')
parser.add_argument('--output', type=Path, required=True)
args = parser.parse_args()

def read(root, name, suffix):
    with (root / f'{name}-{suffix}.csv').open(encoding='utf-8-sig',newline='') as f:
        return list(csv.DictReader(f))

launch_ids = {f'artist_{i:05d}' for i in range(1,3001)}
runs = []
for manifest_path in args.manifests:
    manifest = json.loads(manifest_path.read_text(encoding='utf-8-sig'))
    root = manifest_path.resolve().parents[2] / 'SimLogs'
    for job in manifest['results']:
        if job['mode'] != 'recruitment':
            continue
        assert job['passed'], 'Incomplete seed audit'
        name = job['name']
        labels = read(root,name,'scene-initialization-labels')
        picks = read(root,name,'scene-initialization-appointments')
        launch = {r['id'] for r in read(root,name,'scene-identity-start') if r['entity']=='act' and r['id'] in launch_ids}
        assert len(launch) == 3000
        assert len(picks) == len({r['artistId'] for r in picks})
        assert len(picks) == len({(r['round'],r['labelId']) for r in picks})
        assert all(r['artistId'] in launch for r in picks)
        signed = sum(int(r['filled']) for r in labels)
        assert signed == len(picks)
        remaining = len(launch)-signed
        majors = [r for r in labels if r['tier']=='Major' and r['hqPlaceId'] and not r['hqPlaceId'].startswith('gb_')]
        deficits = []
        for label in majors:
            target, filled = int(label['target']),int(label['filled'])
            if filled >= target:
                continue
            rounds = [int(r['round']) for r in picks if r['labelId']==label['labelId']]
            accessible = int(label['accessibleAny'])
            assert accessible == 0, 'An underfilled major has remaining accessible supply'
            deficits.append(dict(label=label['labelId'],city=label['hqPlaceId'],target=target,filled=filled,
                deficit=target-filled, fillPercent=100*filled/target, lastPickRound=max(rounds,default=-1),
                reason='National launch pool exhausted' if remaining==0 else 'Remaining launch acts outside geographic access'))
        tiers = {}
        for tier in sorted({r['tier'] for r in labels}):
            group = [r for r in labels if r['tier']==tier]
            tiers[tier] = dict(labels=len(group),desired=sum(int(r['target']) for r in group),
                              filled=sum(int(r['filled']) for r in group),empty=sum(int(r['filled'])==0 for r in group))
        runs.append(dict(seed=job['seed'],run=name,launchSupply=len(launch),desired=sum(int(r['target']) for r in labels),
                         signed=signed,unsigned=remaining,domesticMajors=len(majors),
                         domesticMajorTarget=sum(int(r['target']) for r in majors),
                         domesticMajorFilled=sum(int(r['filled']) for r in majors),
                         deficits=deficits,tiers=tiers,lastPickRound=max(int(r['round']) for r in picks)))
assert len({r['seed'] for r in runs})==len(runs), 'Duplicate seeds'
report = dict(seedCount=len(runs),seedsWithDomesticMajorShortage=sum(bool(r['deficits']) for r in runs),
    domesticMajorObservations=sum(r['domesticMajors'] for r in runs),
    underfilledDomesticMajors=sum(len(r['deficits']) for r in runs),
    totalMissingMajorActs=sum(d['deficit'] for r in runs for d in r['deficits']),
    requestedMajorActs=sum(r['domesticMajorTarget'] for r in runs),
    maxIndividualShortage=max((d['deficit'] for r in runs for d in r['deficits']),default=0),
    maxIndividualShortagePercent=max((100-d['fillPercent'] for r in runs for d in r['deficits']),default=0),
    runs=runs,productionAllocatorChanged=False)
args.output.parent.mkdir(parents=True,exist_ok=True)
args.output.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print(json.dumps({k:v for k,v in report.items() if k!='runs'},indent=2))
for r in runs:
    print(f"seed={r['seed']} demand={r['desired']} signed={r['signed']} unsigned={r['unsigned']} major={r['domesticMajorFilled']}/{r['domesticMajorTarget']} deficits={r['deficits']}")
