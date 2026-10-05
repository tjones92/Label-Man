import fs from 'node:fs';
import assert from 'node:assert/strict';
import {csvRows} from './analyze-polar-research.mjs';
import {snapshotMetadata} from './read-polar-snapshot-metadata.mjs';
const json=p=>JSON.parse(fs.readFileSync(p,'utf8').replace(/^\uFEFF/,''));
const lines=p=>fs.readFileSync(p,'utf8').trim().split(/\r?\n/).filter(Boolean).map(l=>JSON.parse(l));
const group=(a,key)=>{const m=new Map();for(const r of a){const k=key(r);if(!m.has(k))m.set(k,[]);m.get(k).push(r);}return m;};
const month=d=>{const a=d.split('/');return a[2]+'-'+a[0].padStart(2,'0');};
const quarter=d=>{const a=d.split('/');return a[2]+'-Q'+Math.ceil(+a[0]/3);};
const q=(a,p)=>{a=a.filter(Number.isFinite).sort((x,y)=>x-y);return a.length?a[Math.max(0,Math.ceil(a.length*p)-1)]:null;};
const csv=p=>{const c=csvRows(p);assert.equal(c.malformed,0,p);return c.rows;};
const runs=json('SimLogs/polar-directive3-trajectory-a-replay-runs.json'),old=json('SimLogs/polar-directive2-trajectories-analysis.json');
const completed=[],quarterly=[],census=[],comparisons=[],arrivals=[];
for(const seed of [1001,1002]) {
 const slices=runs.filter(r=>r.seed===seed).sort((a,b)=>a.slice-b.slice);assert.equal(slices.reduce((n,r)=>n+r.weeks,0),91);assert.deepEqual(slices.at(-1).date,[1961,9,29]);assert.equal(slices.at(-1).remaining,0);
 let rr=[],pop=[],slots=[],events=[];
 for(const slice of slices) {
  const root='SimLogs/'+slice.run,inv=json(root+'-invocation.json');
  for(const flag of ['--polar-directive3-a-replay','--polar-repair-legacy-world','--composition-shape-v1=off','--composition-shape-v2=off','--polar-census=none'])assert(inv.arguments.includes(flag));
  assert.equal((await snapshotMetadata(slice.snapshot)).shape,0);
  rr.push(...lines(root+'-directive3-a-replay.jsonl'));pop.push(...lines(root+'-directive3-census.jsonl'));slots.push(...csv(root+'-directive2-quarter-slots.csv'));
  events.push(...csv(root+'-artist-population-events.csv'));
 }
 rr=[...new Map(rr.map(r=>[month(r.date)+'|'+r.artistId+'|'+r.variant,r])).values()];
 pop=[...new Map(pop.map(r=>[month(r.date)+'|'+r.artistId,r])).values()];
 slots=[...new Map(slots.map(r=>[month(r.date)+'|'+r.artistId+'|'+r.slot,r])).values()];
 const slotCounts=new Map([...group(slots,r=>month(r.date)+'|'+r.artistId)].map(([k,a])=>[k,a.length]));
 assert.equal(new Set(rr.map(r=>month(r.date))).size,21);assert(rr.every(r=>r.baselineMatched));
 for(const [_,aa] of group(rr,r=>month(r.date)+'|'+r.artistId)){assert.equal(aa.length,12);assert.equal(new Set(aa.map(r=>r.poolHash+'|'+r.originals+'|'+r.requestedCovers)).size,1);}
 for(const scope of ['all','signed','unsigned'])for(const [_,aa] of group(rr,r=>quarter(r.date)+'|'+r.genre+'|'+r.variant)) {
  const a=aa.filter(r=>scope==='all'||r.unsigned===(scope==='unsigned')),inherited=a.reduce((n,r)=>n+r.inherited,0),count=a.reduce((n,r)=>n+r.slots,0);
  quarterly.push({seed,period:quarter(aa[0].date),genre:aa[0].genre,variant:aa[0].variant,scope,actObservations:a.length,inherited,slots:count,share:count?100*inherited/count:null});
 }
 for(const [_,aa] of group(pop,r=>month(r.date)+'|'+r.genre+'|'+r.unsigned)) {
  const available=aa.filter(a=>a.slots!==null).map(a=>a.slots);
  for(const a of aa.filter(a=>a.slots!==null))assert.equal(a.slots,slotCounts.get(month(a.date)+'|'+a.artistId)??0);
  census.push({seed,arm:'baseline-replay',month:month(aa[0].date),date:aa[0].date,genre:aa[0].genre,unsigned:aa[0].unsigned,acts:aa.length,observedSlotActs:available.length,slotsPerAct:{p10:q(available,.1),median:q(available,.5),p90:q(available,.9)},evidence:'Fresh original-center baseline identity census, both shapes off. Gospel slots complete; other five genres bounded; remaining slots null.'});
 }
 let previous=new Map();
 for(const [monthKey,aa] of [...group(pop,r=>month(r.date))].sort((a,b)=>a[0].localeCompare(b[0]))) {
  const current=new Map(aa.map(r=>[r.artistId,r])),sources={seededAtStart:0,spawned:0,migrated:0,releasedFromLabel:0,unknown:0},departed={signed:0,migratedOut:0,removedFromRosterOrPool:0};
  const gospel=aa.filter(r=>r.genre==='Gospel'&&r.unsigned);
  for(const r of gospel){const p=previous.get(r.artistId);if(p?.genre==='Gospel'&&p.unsigned)continue;if(!previous.size){sources.seededAtStart++;continue;}if(p&&p.genre!=='Gospel'){sources.migrated++;continue;}if(p&&!p.unsigned){sources.releasedFromLabel++;continue;}if(r.formationCohort==='RuntimeFormation'||events.some(e=>e.artistId===r.artistId&&e.eventType==='formation'))sources.spawned++;else sources.unknown++;}
  for(const p of previous.values()){if(p.genre!=='Gospel'||!p.unsigned)continue;const r=current.get(p.artistId);if(!r)departed.removedFromRosterOrPool++;else if(r.genre!=='Gospel')departed.migratedOut++;else if(!r.unsigned)departed.signed++;}
  arrivals.push({seed,arm:'baseline-replay',month:monthKey,activeUnsignedGospel:gospel.length,sources,departed,completeIdentityCensus:true,definition:'Monthly observed searchable-pool transitions, not unique lifetime formations. Latent-source absences remain unknown.'});previous=current;
 }
 for(const r of quarterly.filter(r=>r.seed===seed&&r.variant==='baseline')) {
  const ref=old.quarterly.find(a=>a.seed===seed&&a.shape==='baseline'&&a.genre===r.genre&&a.period===r.period&&a.scope===r.scope);
  comparisons.push({...r,retainedInherited:ref?.inherited,retainedSlots:ref?.slots,countsExact:!!ref&&r.inherited===ref.inherited&&r.slots===ref.slots});
 }
 completed.push({seed,weeks:91,months:21,slices:slices.length,date:[1961,9,29]});
}
const effects=quarterly.filter(r=>r.scope==='all'&&r.variant.startsWith('axis')).map(r=>{const base=quarterly.find(a=>a.seed===r.seed&&a.genre===r.genre&&a.period===r.period&&a.scope==='all'&&a.variant===(r.variant.endsWith('WithB')?'packageB':'baseline'));return{...r,referenceVariant:base.variant,deltaInherited:r.inherited-base.inherited,deltaSlots:r.slots-base.slots};});
const out={completed,quarterly,census,arrivals,effects,comparisons,definition:'Monthly held-world local frozen-arrangement A-coordinate interventions. Production baseline selector order matched. Not independently evolved axis-only trajectories.',q1Folk:comparisons.filter(r=>r.period==='1961-Q1'&&r.genre==='Folk'&&r.scope==='all'),limits:['Baseline population/history path can differ from independently evolved repaired trajectory. Exact cohort-count comparison is reported, never forced.','Non-Gospel panels are bounded; baseline slots outside observed genres are null.']};
fs.writeFileSync('SimTools/PolarGospelDirective3AReplay.json',JSON.stringify(out,null,2)+'\n');
console.log(JSON.stringify({completed,q1Folk:out.q1Folk,exactQuarterCounts:comparisons.filter(r=>r.countsExact).length,quarterCounts:comparisons.length},null,2));
