import fs from 'node:fs';
import assert from 'node:assert/strict';
import {csvRows} from './analyze-polar-research.mjs';
import {snapshotMetadata} from './read-polar-snapshot-metadata.mjs';

const json=p=>JSON.parse(fs.readFileSync(p,'utf8').replace(/^\uFEFF/,''));
const csv=p=>{const c=csvRows(p);assert.equal(c.malformed,0,p);return c.rows;};
const lines=p=>fs.readFileSync(p,'utf8').trim().split(/\r?\n/).filter(Boolean).map(l=>JSON.parse(l));
const group=(a,key)=>{const m=new Map();for(const r of a){const k=key(r);if(!m.has(k))m.set(k,[]);m.get(k).push(r);}return m;};
const pct=(n,d)=>d?100*n/d:null,q=(a,p)=>{a=a.filter(Number.isFinite).sort((x,y)=>x-y);return a.length?a[Math.max(0,Math.ceil(a.length*p)-1)]:null;};
const dateParts=d=>{const m=d.match(/^(\d+)\/(\d+)\/(\d+)/);assert(m,'US calendar date '+d);return [+m[3],+m[1],+m[2]];};
const month=d=>{const [y,m]=dateParts(d);return y+'-'+String(m).padStart(2,'0');};
const stamp=d=>{const [y,m,day]=dateParts(d);return y*10000+m*100+day;};
const period=d=>{const [y,m]=dateParts(d);return y+'-Q'+Math.ceil(m/3);};
const inWindow=r=>stamp(r.date)<=19610929;
function monthly(rr){const first=new Map();for(const r of rr){const key=month(r.date);if(!first.has(key)||stamp(r.date)<stamp(first.get(key)))first.set(key,r.date);}return rr.filter(r=>r.date===first.get(month(r.date)));}
function unique(rr,key){return [...new Map(rr.map(r=>[key(r),r])).values()];}
function quarterly(rr,seed,arm){const out=[];for(const scope of ['all','signed','unsigned','cohort'])for(const [key,aa] of group(rr,r=>r.genre+'|'+period(r.date)+(scope==='cohort'?'|'+r.cohort:''))) {
 const a=aa.filter(r=>scope==='all'||scope==='cohort'||(r.unsigned==='True')===(scope==='unsigned')),n=a.filter(r=>['establishedStandard','traditionalLineage'].includes(r.category)).length;
 out.push({seed,arm,genre:aa[0].genre,period:period(aa[0].date),scope:scope==='cohort'?aa[0].cohort:scope,slots:a.length,inherited:n,share:pct(n,a.length)});
}return out;}
const old=json('SimLogs/polar-directive2-trajectories-analysis.json'),runs=json('SimLogs/polar-directive3-trajectory-shape-v2-runs.json'),rep=json('Data/PolarRepertoireTable.json');
const completed=[],quarter=[],census=[],arrivals=[],leakage=[],leakageActs=[],economics=[],strata=[];
const retainedTailLimits=[];
function retainedEvents(path,lastObservation) {
 const parsed=csvRows(path);
 if(parsed.malformed){assert.equal(parsed.malformed,1,'Only documented interrupted final row may be excluded');const tail=fs.readFileSync(path,'utf8').trimEnd().split(/\r?\n/).at(-1),date=tail.match(/"(\d+\/\d+\/\d+)"/)?.[1];assert(date&&stamp(date)>lastObservation,'Malformed tail must occur after final monthly observation');retainedTailLimits.push({path,malformedTail:1,date,lastObservation,monthlyTransitionsAffected:false});}
 return parsed.rows.filter(inWindow);
}

function censusFromRows(rr,seed,arm,evidence){const out=[];for(const [key,aa] of group(rr,r=>month(r.date)+'|'+r.genre+'|'+r.unsigned)) {
 const sets=aa.map(r=>r.slots).filter(v=>v!==null),unsigned=aa[0].unsigned==='True'||aa[0].unsigned===true;
 out.push({seed,arm,date:aa[0].date,month:month(aa[0].date),genre:aa[0].genre,unsigned,acts:aa.length,observedSlotActs:sets.length,slotsPerAct:{p10:q(sets,.1),median:q(sets,.5),p90:q(sets,.9),min:sets.length?Math.min(...sets):null,max:sets.length?Math.max(...sets):null},evidence});
}return out;}
function slotCensus(rr){return [...group(rr,r=>month(r.date)+'|'+r.artistId)].map(([_,a])=>({artistId:a[0].artistId,genre:a[0].genre,unsigned:a[0].unsigned,date:a[0].date,slots:a.length}));}
function sourceArrivals(pop,events,seed,arm,complete){const months=[...group(pop,r=>month(r.date))].sort((a,b)=>a[0].localeCompare(b[0]));let previous=new Map();for(let i=0;i<months.length;i++) {
 const [monthKey,aa]=months[i],current=new Map(aa.map(a=>[a.artistId,a])),unsignedGospel=aa.filter(a=>a.genre==='Gospel'&&(a.unsigned===true||a.unsigned==='True'));
 const sources={seededAtStart:0,spawned:0,migrated:0,releasedFromLabel:0,unknown:0};const departed={signed:0,migratedOut:0,removedFromRosterOrPool:0};
 for(const a of unsignedGospel){const p=previous.get(a.artistId),priorGospel=p?.genre==='Gospel'&&(p.unsigned===true||p.unsigned==='True');if(priorGospel)continue;
  if(i===0){sources.seededAtStart++;continue;}
  if(p&&p.genre!=='Gospel'){sources.migrated++;continue;}
  if(p&&!(p.unsigned===true||p.unsigned==='True')){sources.releasedFromLabel++;continue;}
  const formation=events.find(e=>e.artistId===a.artistId&&e.eventType==='formation'&&stamp(e.date)<=stamp(a.date));
  if(formation||a.formationCohort==='RuntimeFormation')sources.spawned++;else sources.unknown++;
 }
 if(i>0)for(const p of previous.values()){if(p.genre!=='Gospel'||!(p.unsigned===true||p.unsigned==='True'))continue;const a=current.get(p.artistId);if(!a)departed.removedFromRosterOrPool++;else if(a.genre!=='Gospel')departed.migratedOut++;else if(!(a.unsigned===true||a.unsigned==='True'))departed.signed++;}
 arrivals.push({seed,arm,month:monthKey,activeUnsignedGospel:unsignedGospel.length,sources,departed,completeIdentityCensus:complete,definition:'Observed month transitions. A disappearance is roster/pool attrition, not necessarily death/disbanding. Baseline zero-slot absences cannot be ruled out.'});previous=current;
}}

for(const seed of [1001,1002]) {
 const slices=runs.filter(r=>r.seed===seed).sort((a,b)=>a.slice-b.slice);assert(slices.length);assert.equal(slices.at(-1).remaining,0);assert.deepEqual(slices.at(-1).date,[1961,9,29]);assert.equal(slices.reduce((s,r)=>s+r.weeks,0),91);
 let slots=[],population=[],weeks=[],events=[],inventory=new Map();
 for(const slice of slices) {
  const root='SimLogs/'+slice.run,manifest=json(root+'-polar-research.json'),inv=json(root+'-invocation.json');assert.equal(manifest.censusMode,'none');assert(manifest.months.every(m=>m.Status==='disabled'));assert(inv.arguments.includes('--composition-shape-v2=on'));
  const metadata=await snapshotMetadata(slice.snapshot);assert.equal(metadata.shape,2);
  const log=fs.readFileSync(root+'.log','utf8');assert(/CHART_AUDIT_(STOPPED|COMPLETE)/.test(log));if(slice.slice>1)assert(log.includes('POLAR_CHECKPOINT_ROUNDTRIP_PASS'));
  slots.push(...csv(root+'-directive2-quarter-slots.csv'));population.push(...lines(root+'-directive3-census.jsonl'));weeks.push(...csv(root+'-polar-final-weekly.csv'));events.push(...csv(root+'-artist-population-events.csv'));
  for(const r of csv(root+'-repertoire-inventory.csv'))inventory.set(r.songId,r);
 }
 slots=unique(monthly(slots),r=>month(r.date)+'|'+r.artistId+'|'+r.slot);population=unique(monthly(population),r=>month(r.date)+'|'+r.artistId);weeks=unique(weeks,r=>r.date);
 assert.equal(new Set(slots.map(r=>month(r.date))).size,21);assert.equal(new Set(population.map(r=>month(r.date))).size,21);assert.equal(weeks.length,91);
 const censusSlotMap=new Map([...group(slots,r=>month(r.date)+'|'+r.artistId)].map(([k,v])=>[k,v.length]));
 for(const a of population.filter(a=>a.genre==='Gospel'))assert.equal(a.slots,censusSlotMap.get(month(a.date)+'|'+a.artistId)??0,'All Gospel acts include zero-slot identities');
 const newQuarter=quarterly(slots,seed,'shape-v2');quarter.push(...newQuarter);census.push(...censusFromRows(population,seed,'shape-v2','Full roster/pool identity census. Gospel slots complete; other five genres sampled; remaining genres no slot observation. Null is not zero.'));
 sourceArrivals(population,events,seed,'shape-v2',true);
 const baselineSlots=monthly(csv(`SimLogs/polar-repertoire-final-${seed}-repertoire-slots.csv`).filter(inWindow));
 const baselinePop=slotCensus(baselineSlots);census.push(...censusFromRows(baselinePop,seed,'baseline','Retained full-census filled-slot identities: zero-slot acts absent, so counts are lower bounds.'));
 sourceArrivals(baselinePop,retainedEvents(`SimLogs/polar-repertoire-final-${seed}-artist-population-events.csv`,baselinePop.reduce((latest,r)=>Math.max(latest,stamp(r.date)),0)),seed,'baseline',false);
 // D2 repaired v1 population for Gospel can be reconstructed from full cohort slots; other genres are bounded panels.
 const oldRuns=fs.readdirSync('SimLogs').filter(p=>/^polar-directive2-trajectory-.*runs\.json$/.test(p)).flatMap(p=>json('SimLogs/'+p)).filter(r=>r.seed===seed&&r.shape==='on');
 const oldSlotRows=monthly(oldRuns.flatMap(r=>csv('SimLogs/'+r.run+'-directive2-quarter-slots.csv'))),oldPop=slotCensus(oldSlotRows);
 census.push(...censusFromRows(oldPop,seed,'repaired-v1','Gospel filled-slot identities complete except potential zero-slot omissions; other genres are bounded panels.'));
 quarter.push(...old.quarterly.filter(r=>r.seed===seed&&r.shape==='on').map(r=>({...r,arm:'repaired-v1'})));
 for(const [key,aa] of group(slots.filter(r=>r.genre==='Gospel'),r=>period(r.date)+'|'+r.unsigned+'|'+(+r.albumCount===0?'0':+r.albumCount<=12?'1–12':+r.albumCount<=48?'13–48':'49+'))) {
  const inherited=aa.filter(r=>['establishedStandard','traditionalLineage'].includes(r.category)).length;
  strata.push({seed,period:period(aa[0].date),unsigned:aa[0].unsigned==='True',stratum:key.split('|')[2],actObservations:new Set(aa.map(r=>month(r.date)+'|'+r.artistId)).size,slots:aa.length,inherited,share:pct(inherited,aa.length)});
 }
 function contextRow(r){const source=inventory.get(r.songId),original=r.category==='newlyAuthored';if(!original)assert(source,'Composition source for selected cover');return {...r,sourceGenre:source?.genre??'unknown',sourceFamily:r.family||source?.seedFamily||r.origin,context:r.context?.toLowerCase()??'unknown',sourceLeakage:!original&&(source?.genre==='RnB'||source?.genre==='DooWop'||r.origin==='ProfessionalOffice'),original};}
 const gospel=slots.filter(r=>r.genre==='Gospel').map(contextRow);
 for(const [key,aa] of group(gospel,r=>period(r.date))) {
  const covers=aa.filter(r=>!r.original),unknown=covers.filter(r=>r.context==='unknown'),secular=covers.filter(r=>r.context==='secular'),nonSacred=covers.filter(r=>r.context!=='sacred'),leak=covers.filter(r=>r.sourceLeakage);
  leakage.push({seed,period:key,allSlots:aa.length,covers:covers.length,unknownCovers:unknown.length,knownSecular:secular.length,nonSacredOrUnknown:nonSacred.length,sourceLeakage:leak.length,shareAll:pct(leak.length,aa.length),shareCovers:pct(leak.length,covers.length),sources:Object.fromEntries([...group(leak,r=>r.sourceFamily+'|'+r.sourceGenre+'|'+r.context)].map(([k,v])=>[k,v.length]))});
  for(const [id,rr] of group(leak,r=>r.artistId)){const all=aa.filter(r=>r.artistId===id);leakageActs.push({seed,period:key,artistId:id,unsigned:rr[0].unsigned==='True',allSlots:all.length,sourceLeakage:rr.length,songs:rr.map(r=>({songId:r.songId,family:r.sourceFamily,genre:r.sourceGenre,context:r.context}))});}
 }
 const oldWeeks=new Map(old.weekly.filter(r=>r.seed===seed&&r.shape==='on').map(r=>[r.date,r]));
 const sums={marketUnits:0,referenceMarketUnits:0,labelNet:0,referenceLabelNet:0};
 for(const w of weeks){const ref=oldWeeks.get(w.date);assert(ref,'Matched Directive 2 repaired date');sums.marketUnits+=+w.marketUnits;sums.referenceMarketUnits+=+ref.marketUnits;sums.labelNet+=+w.labelNet;sums.referenceLabelNet+=+ref.labelNet;}
 economics.push({seed,matchedWeeks:91,...sums,marketUnitsDeltaPercent:pct(sums.marketUnits-sums.referenceMarketUnits,sums.referenceMarketUnits),labelNetDeltaPercent:pct(sums.labelNet-sums.referenceLabelNet,sums.referenceLabelNet),scope:'Shape-only, 350-song book. Resume boundaries differ from retained v1 trajectories, so these are descriptive deltas, not an isolated causal economic estimate.'});
 completed.push({seed,slices:slices.length,weeks:91,lastDate:[1961,9,29],monthlyObservations:21});
}
const ranges=quarter.filter(r=>r.arm==='shape-v2'&&!['all','signed','unsigned'].includes(r.scope)).flatMap(r=>{const band=rep.bands.find(b=>b.genre===r.genre&&b.cohort===r.scope);if(!band)return[];const ref=quarter.find(b=>b.arm==='repaired-v1'&&b.seed===r.seed&&b.genre===r.genre&&b.scope===r.scope&&b.period===r.period);return [{...r,band:[band.min,band.max],pass:r.share>=band.min&&r.share<=band.max,referenceShare:ref?.share,newAgainstV1:!(r.share>=band.min&&r.share<=band.max)&&!!ref&&ref.share>=band.min&&ref.share<=band.max}];});
const out={completed,quarterly:quarter,census,arrivals,leakage,leakageActs,strata,economics,ranges,gate:{gospelQ3:quarter.filter(r=>r.arm==='shape-v2'&&r.genre==='Gospel'&&r.period==='1961-Q3'&&r.scope==='all').map(r=>({...r,provisionalPass:r.share>=50&&r.share<=90,researchBand:null})),newCrossGenreFailuresAgainstV1:ranges.filter(r=>r.genre!=='Gospel'&&r.newAgainstV1),holdOutReady:false},limits:['Historical numerical band and expansion range withheld. Expanded-book trajectory not authorized by research gate.','Baseline census lacks zero-slot identities; other genres in new trajectories have partial slot observations.','Month transitions cannot rule out temporary roster/pool exits.','Resume-boundary differences are disclosed; fixed-world probes isolate selector effects.']};
out.retainedTailLimits=retainedTailLimits;
fs.writeFileSync('SimTools/PolarGospelDirective3Trajectories.json',JSON.stringify(out,null,2)+'\n');
console.log(JSON.stringify({completed,gate:out.gate,economics,censusRows:census.length,leakageRows:leakage.length},null,2));
