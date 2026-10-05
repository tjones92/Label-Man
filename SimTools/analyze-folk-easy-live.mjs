import fs from 'node:fs';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {csvRows} from './analyze-polar-research.mjs';

const tag=process.argv.find(a=>a.startsWith('--tag='))?.slice(6)??'bandleaders-v2';
assert(/^[a-zA-Z0-9-]+$/.test(tag));
const partial=process.argv.includes('--partial');
const read=p=>JSON.parse(fs.readFileSync(p,'utf8').replace(/^\uFEFF/,''));
const categories=['Traditional','Standard','Contemporary','Original'];
const table=read('Data/PolarRepertoireTable.json');
const recoveryPath='SimTools/FolkEasyListeningCheckpointRecovery.json';
const recovery=fs.existsSync(recoveryPath)?read(recoveryPath):null;
const percent=(n,d)=>d?100*n/d:null;
const weight=r=>Number(r.population)/Number(r.sample);
function measure(rows) {
 const raw=Object.fromEntries(categories.map(c=>[c,rows.filter(r=>r.category===c).length]));
 const weighted=Object.fromEntries(categories.map(c=>[c,rows.filter(r=>r.category===c).reduce((n,r)=>n+weight(r),0)]));
 const total=Object.values(weighted).reduce((a,b)=>a+b,0);
 const covered=rows.filter(r=>r.category!=='Original');
 const songs=new Map();for(const r of covered)songs.set(r.songId,(songs.get(r.songId)??0)+1);
 const setRows=[...new Map(rows.map(r=>[r.date+'|'+r.artistId,r])).values()];
 const weightedInstrumental=rows.filter(r=>r.instrumental==='True').reduce((n,r)=>n+weight(r),0);
 return {observedSlots:rows.length,observedSets:setRows.length,distinctActs:new Set(rows.map(r=>r.artistId)).size,
  months:new Set(rows.map(r=>r.date)).size,rawCounts:raw,rawShares:Object.fromEntries(categories.map(c=>[c,percent(raw[c],rows.length)])),
  weightedSlots:total,weightedCounts:weighted,weightedShares:Object.fromEntries(categories.map(c=>[c,percent(weighted[c],total)])),
  instrumentalSlotShare:percent(weightedInstrumental,total),shortSets:setRows.filter(r=>Number(r.setSize)<3).length,
  coversWithNoSourceAccess:setRows.filter(r=>['traditionalAccess','standardAccess','contemporaryAccess'].some(k=>Number(r[k])===0)).length,
  mediaThemeSlots:covered.filter(r=>r.mediaRecordId).length,mediaTypes:[...new Set(covered.filter(r=>r.mediaSource).map(r=>r.mediaSource))],
  coverConcentration:{distinctSongs:songs.size,top10Share:percent([...songs.values()].sort((a,b)=>b-a).slice(0,10).reduce((a,b)=>a+b,0),covered.length)}};
}
function summarize(rows,genre,period) {
 const m=measure(rows);
 const year=Number(rows[0]?.year??1960),config=table.liveSetMixes[genre].find(r=>year>=r.fromYear&&year<=(r.toYear??Infinity));
 const weights=[config.traditional,config.standards,config.contemporary,config.originals],sum=weights.reduce((a,b)=>a+b,0);
 const target=Object.fromEntries(categories.map((c,i)=>[c,100*weights[i]/sum]));
 return {genre,period,...m,target,weightedDeltaPoints:Object.fromEntries(categories.map(c=>[c,m.weightedShares[c]===null?null:m.weightedShares[c]-target[c]]))};
}
const trajectories=[];
for(const seed of [1001,1002]) {
 const value=read(`SimLogs/folk-easy-${tag}-${seed}-runs.json`),manifest=Array.isArray(value)?value:[value];
 const weeks=manifest.reduce((n,r)=>n+r.weeks,0);
 if(!partial) {assert.equal(weeks,209,'Full 1960-1963 trajectory required');assert.equal(manifest.at(-1).remaining,0);}
 const invocations=manifest.map(r=>read(`SimLogs/${r.run}-invocation.json`));
 const assemblies=[...new Set(invocations.map(r=>r.assemblySha256))];
 if(assemblies.length>1) {
  assert.equal(recovery?.tag,tag,'Mixed assemblies require documented checkpoint recovery');
  assert(assemblies.every(h=>[recovery.previousAssemblySha256,recovery.currentAssemblySha256].includes(h)),'Only the documented diagnostic recovery assembly is allowed');
  for(const row of recovery.unchangedGameplayFingerprints)assert.equal(crypto.createHash('sha256').update(fs.readFileSync(row.path)).digest('hex'),row.sha256,'Gameplay unchanged during recovery');
 }
 assert.equal(new Set(invocations.map(r=>r.repertoireTableSha256)).size,1,'One repertoire table per trajectory');
 const raw=manifest.flatMap(r=> {const parsed=csvRows(`SimLogs/${r.run}-folk-easy-slots.csv`);assert.equal(parsed.malformed,0);return parsed.rows;});
 const allObserved=[...new Map(raw.map(r=>[r.date+'|'+r.artistId+'|'+r.slot,r])).values()];
 const rows=allObserved.filter(r=>Number(r.year)>=1960&&Number(r.year)<=1963);
 for(const r of rows) {
  assert(categories.includes(r.category));assert(Number(r.originYear||r.year)<=Number(r.year));assert(Number(r.population)>=Number(r.sample));assert(Number(r.sample)>0);
  if(r.genre==='EasyListening'&&r.category==='Original')assert.equal(r.instrumental,'True','Bandleader-only originals');
  if(r.category==='Standard') {assert(Number(r.originYear)<=(r.genre==='EasyListening'?1954:1959));if(r.genre!=='EasyListening')assert(Number(r.originYear)>=1920);}
 }
 const sets=Map.groupBy(rows,r=>r.date+'|'+r.artistId);
 for(const set of sets.values()) {
  assert.equal(set.length,Number(set[0].setSize));
  const coverIds=set.filter(r=>r.category!=='Original').map(r=>r.songId);assert.equal(coverIds.length,new Set(coverIds).size,'Unique covers');
 }
 const annual=[...Map.groupBy(rows,r=>r.genre+'|'+r.year)].map(([key,rr])=>summarize(rr,rr[0].genre,rr[0].year));
 const eras=[...Map.groupBy(rows,r=>(r.genre==='EasyListening'?'EasyListening':'Folk scene')+'|'+(Number(r.year)<=1961?'1960-1961':'1962-1963'))]
  .map(([key,rr])=>summarize(rr,rr[0].genre==='EasyListening'?'EasyListening':'Folk',key.split('|')[1]));
 const scopes=[...Map.groupBy(rows,r=>r.genre+'|'+r.year+'|'+r.unsigned+'|'+r.instrumental)]
  .map(([key,rr])=>({unsigned:rr[0].unsigned==='True',instrumental:rr[0].instrumental==='True',...summarize(rr,rr[0].genre,rr[0].year)}));
 trajectories.push({seed,weeks,lastDate:manifest.at(-1).date,observations:new Set(rows.map(r=>r.date)).size,excludedOutsideWindowSlots:allObserved.length-rows.length,assemblies,annual,eras,scopes,invocations,
  checkpointRoundtripMarkers:manifest.slice(1).map(r=>({run:r.run,passed:fs.readFileSync(`SimLogs/${r.run}.log`,'utf8').includes('POLAR_CHECKPOINT_ROUNDTRIP_PASS')}))});
}
for(const trajectory of trajectories)for(const marker of trajectory.checkpointRoundtripMarkers)assert(marker.passed,'Every resume verifies checkpoint identity');
const files=['Data/PolarRepertoireTable.json','Data/SimulatedArtist.cs','Data/SongComposition.cs','Systems/PolarRepertoireTable.cs','Systems/LiveRepertoire.cs','Systems/SongMaterialSelectionService.cs','Systems/CompositionCatalogService.cs','Systems/ArtistManager.cs','Systems/PolarActProfileDeriver.cs','Systems/PlayerDesk.cs','SimTools/FolkEasyListeningChecks.cs','SimTools/ChartAuditRunner.FolkEasy.cs','SimTools/ChartAuditRunner.PolarResearch.cs'];
const checks=[1001,1002].map(seed=>({seed,fixtureCalibration:read(`SimLogs/folk-easy-check-${seed}.json`).rows.filter(r=>r.kind==='fullySuppliedFixture'),
 logs:[`SimLogs/folk-easy-final-${seed}-folk-easy-check.log`,`SimLogs/folk-easy-final-${seed}-gospel-preference-check.log`,`SimLogs/folk-easy-directive3-${seed}.log`].map(path=>({path,passMarkers:fs.readFileSync(path,'utf8').split(/\r?\n/).filter(l=>/^(FOLK_EASY_CHECK_PASS|GOSPEL_PREFERENCE_CHECK_PASS|POLAR_DIRECTIVE3_CHECK_PASS)/.test(l))}))}));
for(const c of checks)for(const log of c.logs)assert.equal(log.passMarkers.length,1,'Expected verification pass marker');
const streamingChecks=recovery?.tag===tag?[1001,1002].map(seed=> {
 const path=`SimLogs/folk-easy-streaming-check-${seed}.log`,passed=fs.readFileSync(path,'utf8').includes('FOLK_EASY_CHECK_PASS');
 assert(passed,'Recovered assembly streaming/repertoire check');return {seed,path,passed};
}):[];
const regressions=[1001,1002].flatMap(seed=>['polar-repertoire-check','polar-refusal-avoidance-check'].map(check=> {
 const path=`SimLogs/folk-easy-regression-${seed}-${check}.log`,passed=/^POLAR_(REPERTOIRE|REFUSAL_AVOIDANCE)_PASS /m.test(fs.readFileSync(path,'utf8'));
 assert(passed,'Repertoire/access and refusal regressions');return {seed,check,path,passed};
}));
const output={tag,partial,recovery,streamingChecks,regressions,method:'Monthly deterministic panels capped at 12 acts per genre, signed/unsigned status and instrumental role. Shares use population/sample weights. Repeated act observations are not independent historical observations.',trajectories,checks,
 scouting:{existingVenue:'TheatresAndSupperClubs',admitsEasyListening:true,missingDedicatedVenues:['Casinos','Luxury Resorts']},
 fingerprints:files.map(path=>({path,sha256:crypto.createHash('sha256').update(fs.readFileSync(path)).digest('hex')}))};
const destination=partial?`SimLogs/folk-easy-${tag}-partial-validation.json`:'SimTools/FolkEasyListeningLiveValidation.json';
fs.writeFileSync(destination,JSON.stringify(output,null,2)+'\n');
console.log(JSON.stringify(trajectories.map(t=>({seed:t.seed,weeks:t.weeks,lastDate:t.lastDate,observations:t.observations,
 eras:t.eras.map(r=>({genre:r.genre,period:r.period,slots:r.observedSlots,shares:r.weightedShares,media:r.mediaThemeSlots,shortSets:r.shortSets}))})),null,2));
