import csv,json,hashlib
from pathlib import Path
root=Path(__file__).resolve().parents[1]
logs=root/'SimLogs';tag='scene-hq-repair-audit-v1'
def rows(name,suffix):
 with (logs/f'{name}-{suffix}.csv').open(encoding='utf-8-sig',newline='') as f:return list(csv.DictReader(f))
def hashes(name):
 return {p.name[len(name)+1:]:hashlib.sha256(p.read_bytes()).hexdigest() for p in logs.glob(name+'-*.csv') if not p.name[len(name)+1:].startswith(('scene-','city-placement-'))}
def metrics(name):
 labor=[r for r in rows(name,'artist-labor-market-weekly') if int(r['week'])<=52]
 pop=[r for r in rows(name,'artist-population-weekly') if r['labelTier']=='All' and int(r['week'])<=52]
 rev=[r for r in rows(name,'market-revenue') if r['period']=='weekly' and r['labelTier']=='All' and int(r['week'])<=52]
 vac=[r for r in rows(name,'label-scouting-vacancy-weekly') if int(r['week'])<=52]
 legacy=sum(int(r['unusedOperatingRosterSlots']) for r in vac if r['labelId'] in {'label_0026','label_0028'} and r['isActiveLabel']=='true' and r['canAffordEstimatedAdvance']=='true')
 finance=[r for r in rows(name,'label-finance') if int(r['week'])==52]
 return dict(singleUnits=sum(float(r['totalMarketUnits']) for r in rev if r['releaseFormat']=='Single'),albumUnits=sum(float(r['totalMarketUnits']) for r in rev if r['releaseFormat']=='Album'),marketNet=sum(float(r['marketNet']) for r in rev if r['releaseFormat']=='All'),affordableVacancySlotWeeksExcludingLegacyUk=sum(int(r['affordableHiringVacancies']) for r in labor)-legacy,rostered=int(pop[-1]['rostered']),defunct=sum(r['status']=='Defunct' for r in finance),integrityMax=max(int(r[k]) for r in pop for k in ('ownershipConflicts','duplicateRosterEntries','duplicatePoolEntries','terminalRostered','terminalReleaseEligible')))
a=hashes('scene4-city-placement-audit-v1-off-1001');b=hashes(tag+'-off-1001')
diff=[k for k in sorted(set(a)|set(b)) if a.get(k)!=b.get(k)]
assert len(a)==84 and not diff,(len(a),diff)
report=dict(weeks=52,offEconomicFilesIdentical=84,comparisons=[])
for seed in (1001,2002):
 before=metrics(f'scene4-four-year-v1-recruitment-{seed}');after=metrics(f'{tag}-recruitment-{seed}')
 change={k:100*(after[k]-before[k])/abs(before[k]) if before[k] else None for k in before}
 hq=rows(f'{tag}-recruitment-{seed}','scene-recruitment-labels-start')
 name_map={r['labelId']:r for r in rows(f'{tag}-recruitment-{seed}','label-geography')}
 affected=[dict(id=r['labelId'],hq=name_map[r['labelId']]['headquartersCity'],place=r['hqPlaceId'],roster=int(r['rosterSize'])) for r in hq if name_map[r['labelId']]['headquartersCity'] in {'Newark','Jackson','Indianapolis','Milwaukee'}]
 assert all(r['place'] for r in affected) and after['integrityMax']==0
 failures=[k for k in ('singleUnits','albumUnits','marketNet','rostered') if change[k] is not None and change[k]<-5]
 report['comparisons'].append(dict(seed=seed,before=before,after=after,percentChange=change,affectedHqs=affected,failedEconomicScreens=failures))
report['economicScreensPassed']=all(not r['failedEconomicScreens'] for r in report['comparisons'])
out=logs/tag/'repair-analysis.json';out.write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps({**report,'comparisons':[{k:v for k,v in r.items() if k!='affectedHqs'} for r in report['comparisons']]},indent=2))
