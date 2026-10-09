"""Exact default/explicit/observer equality and pressure reconciliation."""
import csv, hashlib, json, sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
TAG=sys.argv[1] if len(sys.argv)>1 else 'scene5-observation-v1'
OUT=ROOT/'SimLogs'/TAG
manifest=json.loads((OUT/'runs.json').read_text(encoding='utf-8-sig'))
assert len(manifest['results'])==len(manifest['jobs'])==9
assert all(r['passed'] for r in manifest['results'])

def hashes(mode):
    prefix=f'{TAG}-{mode}-1001-'
    result={}
    for p in (ROOT/'SimLogs').glob(prefix+'*.csv'):
        suffix=p.name[len(prefix):]
        if suffix.startswith(('scene-identity-','scene-participation-','scene-room-','scene-work-',
            'scene-recruitment-','scene-initialization-','scene-dynamics-','scene-opportunity-','scene-move-','scene-attention-')): continue
        result[suffix]=hashlib.sha256(p.read_bytes()).hexdigest()
    return result

control=hashes('default')
assert len(control)==84, len(control)
comparisons=[]
for mode in ('explicit','observe'):
    other=hashes(mode)
    different=[s for s in sorted(set(control)|set(other)) if control.get(s)!=other.get(s)]
    comparisons.append(dict(mode=mode,identicalEconomicFiles=len(control)-len(different),different=different))
    assert not different, comparisons[-1]
with (ROOT/'SimLogs'/f'{TAG}-observe-1001-scene-dynamics-weekly.csv').open(encoding='utf-8-sig',newline='') as f:
    pressure=list(csv.DictReader(f))
assert len(pressure)==31*8
assert len({(r['week'],r['placeId']) for r in pressure})==len(pressure)
for r in pressure:
    assert int(r['bookedSlots'])<=int(r['performanceSlots'])
    assert int(r['bookableResidents'])<=int(r['residents'])
    assert int(r['signedResidents'])<=int(r['residents'])
    assert int(r['performanceSlots'])==18
    assert r['opportunityCoverage']=='NamedSupportingRoomsOnly' and r['fullEmploymentSlots']==''
    expected=int(r['bookableResidents'])/int(r['performanceSlots'])
    assert abs(float(r['bookableActsPerWeeklySlot'])-expected)<1e-6
with (ROOT/'SimLogs'/f'{TAG}-observe-1001-scene-opportunity-priors.csv').open(encoding='utf-8-sig',newline='') as f:
    priors=list(csv.DictReader(f))
assert all(float(r['relativeWeight'])>0 and float(r['ratioTo1960'])==1 for r in priors)
with (ROOT/'SimLogs'/f'{TAG}-observe-1001-scene-move-observations.csv').open(encoding='utf-8-sig',newline='') as f:
    moves=list(csv.DictReader(f))
assert len({(r['week'],r['fromPlaceId']) for r in moves})==len(moves)
for r in moves:
    assert r['fromPlaceId']!=r['toPlaceId']
    assert 0<float(r['roadMiles'])<=350 and int(r['travelDays'])>=1
    assert float(r['opportunityRatio'])>=1.5
    assert r['unresolvedRequirements'] and r['reason']
with (ROOT/'SimLogs'/f'{TAG}-observe-1001-scene-attention-weekly.csv').open(encoding='utf-8-sig',newline='') as f:
    attention=list(csv.DictReader(f))
with (ROOT/'SimLogs'/f'{TAG}-observe-1001-scene-attention-evidence.csv').open(encoding='utf-8-sig',newline='') as f:
    evidence=list(csv.DictReader(f))
assert len(attention)==31*8
assert len({(r['week'],r['placeId']) for r in attention})==len(attention)
assert len({(r['observationWeek'],r['eventId'],r['placeId']) for r in evidence})==len(evidence)
for r in attention:
    assert 0<=float(r['signingHeat'])<=1 and float(r['rawDecayedImpulse'])>=0
for r in evidence:
    assert int(r['signingWeek'])<=int(r['observationWeek'])
    assert 0<float(r['attribution'])<=1 and 0<=float(r['decayedImpulse'])<=.05
from collections import defaultdict
from math import exp
budgets=defaultdict(float)
by_place=defaultdict(list)
for r in evidence:
    budgets[(r['observationWeek'],r['eventId'])]+=float(r['attribution'])
    by_place[(r['observationWeek'],r['placeId'])].append(r)
assert all(v<=1.000001 for v in budgets.values())
for r in attention:
    parts=by_place[(r['week'],r['placeId'])]
    raw=sum(float(e['decayedImpulse']) for e in parts)
    assert int(r['contributingEvents'])==len(parts)
    assert abs(float(r['rawDecayedImpulse'])-raw)<1e-7
    assert abs(float(r['signingHeat'])-(1-exp(-raw)))<1e-7
report=dict(attentionRows=len(attention),attentionEvidenceRows=len(evidence),moveObservationRows=len(moves),passed=True,completedChecks=9,weeks=8,comparisons=comparisons,pressureRows=len(pressure),
    cities=31,pressureYear='1960',opportunityRows=len(priors),
    observationChangesEconomy=False,decadeValidationDeferred=True)
(OUT/'analysis.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print(json.dumps(report,indent=2))
