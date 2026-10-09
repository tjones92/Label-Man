import csv,json,math,sys
from collections import defaultdict
from pathlib import Path
root=Path(__file__).resolve().parents[1]
tag=sys.argv[1] if len(sys.argv)>1 else 'scene5-attention-integration-v1';logs=root/'SimLogs'
def rows(suffix):
 with (logs/f'{tag}-{suffix}.csv').open(encoding='utf-8-sig',newline='') as f:return list(csv.DictReader(f))
assert json.loads((logs/tag/'result.json').read_text(encoding='utf-8-sig'))['passed']
evidence=rows('scene-attention-evidence');cities=rows('scene-attention-weekly')
ledger=rows('scene-recruitment-end')
source={f"{r['artistId']}|{r['labelId']}|{r['week']}|{r['phase']}":r for r in ledger if r['phase'] in ('DailyMarket','WeeklyMarket','PlayerRival')}
assert evidence and source and len(cities)==31*26
assert len({(r['observationWeek'],r['eventId'],r['placeId']) for r in evidence})==len(evidence)
budget=defaultdict(float);impulses=defaultdict(list)
for r in evidence:
 assert r['eventId'] in source and r['signingGenre']
 assert int(r['signingWeek'])==int(source[r['eventId']]['week'])<=int(r['observationWeek'])
 assert r['artistId']==source[r['eventId']]['artistId'] and r['labelId']==source[r['eventId']]['labelId']
 budget[r['observationWeek'],r['eventId']]+=float(r['attribution'])
 expected=.05*float(r['attribution'])*.5**((int(r['observationWeek'])-int(r['signingWeek']))/8)
 assert abs(expected-float(r['decayedImpulse']))<1e-8
 impulses[r['observationWeek'],r['placeId']].append(r)
assert all(v<=1.000001 for v in budget.values())
for r in cities:
 parts=impulses[r['week'],r['placeId']];raw=sum(float(e['decayedImpulse']) for e in parts)
 assert int(r['contributingEvents'])==len(parts)
 assert abs(raw-float(r['rawDecayedImpulse']))<1e-7
 assert abs(1-math.exp(-raw)-float(r['signingHeat']))<1e-7
pop=[r for r in rows('artist-population-weekly') if r['labelTier']=='All']
assert max(int(r[k]) for r in pop for k in ('ownershipConflicts','duplicateRosterEntries','duplicatePoolEntries','terminalRostered','terminalReleaseEligible'))==0
report=dict(passed=True,weeks=26,cityRows=len(cities),evidenceRows=len(evidence),uniqueObservedEvents=len({r['eventId'] for r in evidence}),runtimeLedgerEvents=len(source),eventGenreKnown=True,maximumSigningHeat=max(float(r['signingHeat']) for r in cities),populationIntegrityMax=0)
(logs/tag/'analysis.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report,indent=2))
