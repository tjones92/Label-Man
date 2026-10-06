import fs from 'node:fs';
import readline from 'node:readline';
import crypto from 'node:crypto';
import zlib from 'node:zlib';
import assert from 'node:assert/strict';
import {csvRows} from './analyze-polar-research.mjs';
const args=process.argv.slice(2),frames=args.length?args:['polar-gospel-attribution-start-1001-v2','polar-gospel-attribution-start-1002','polar-gospel-attribution-later-1001-v2','polar-gospel-attribution-later-1002-v2'];
const hashes={};
const digest=p=>hashes[p]=crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
function json(p){digest(p);return JSON.parse(fs.readFileSync(p,'utf8').replace(/^\uFEFF/,''));}
function rows(p){digest(p);const r=csvRows(p);assert.equal(r.malformed,0,p);return r.rows;}
const yes=x=>x==='True',pct=(n,d)=>d?100*n/d:null;
const musicalTable=json('Data/PolarSongTable.json');
assert.deepEqual([musicalTable.numbers.selectionCapability,musicalTable.numbers.selectionIdentity,musicalTable.numbers.selectionMoment],[.35,.45,.2],'Update score decomposition for changed weights');
const increment=(m,k,n=1)=>m[k]=(m[k]??0)+n;
const runs=[];
const frameData=new Map();
for(const prefix of frames) {
 assert(/^[a-zA-Z0-9-]+$/.test(prefix));const root='SimLogs/'+prefix;
 const summary=json(root+'-gospel-summary.json'),candidates=rows(root+'-gospel-candidates.csv'),actors=rows(root+'-gospel-actors.csv'),selected=rows(root+'-gospel-selections.csv');
 const invocation=json(root+'-invocation.json');assert.equal(invocation.seed,summary.seed);assert.equal(invocation.tableSha256,digest('Data/PolarSongTable.json').toUpperCase());assert.equal(invocation.repertoireTableSha256,digest('Data/PolarRepertoireTable.json').toUpperCase());
 assert(summary.worldUnchanged&&summary.baselineSelectorMatched);assert.equal(actors.length,summary.actors);assert.equal(candidates.length,summary.candidateInstances);
 const pools=new Map();for(const c of candidates){assert.equal(c.subjectContext,'unknown','Subject metadata changed; update context diagnosis');if(!pools.has(c.artistId))pools.set(c.artistId,[]);pools.get(c.artistId).push(c);}
 frameData.set(prefix,{summary,candidates,actors,pools,selected});
 const variants=new Map();
 for(const a of summary.summaries){const pool=pools.get(a.artistId)??[];assert.equal(pool.length,a.candidates);assert.equal(a.poolHash,crypto.createHash('sha256').update(pool.map(c=>c.songId).sort().join('\n')).digest('hex').toUpperCase());
  for(const v of a.variants){const k=v.variant;if(!variants.has(k))variants.set(k,{variant:k,acts:0,gospelOutsideTopWindow:0,bestGapSum:0,requestedCovers:0,filledCovers:0,originals:0,slots:0,inheritedSlots:0,gospelBookSlots:0});const m=variants.get(k);m.acts++;if(v.gospelWithinTopWindow===0)m.gospelOutsideTopWindow++;m.bestGapSum+=v.bestScore-(v.bestGospelScore??v.bestScore);m.requestedCovers+=v.requestedCovers;m.filledCovers+=v.filledCovers;m.originals+=v.originals;m.slots+=v.filledCovers+v.originals;m.inheritedSlots+=v.inheritedSlots;m.gospelBookSlots+=v.gospelBookSlots;
   const actual=selected.filter(r=>r.artistId===a.artistId&&r.variant===k);assert.equal(actual.length,v.filledCovers);assert.equal(new Set(actual.map(r=>r.songId)).size,actual.length);assert.equal(actual.filter(r=>['traditionalLineage','establishedStandard'].includes(r.category)).length,v.inheritedSlots);
  }
 }
 const retained=rows(`SimLogs/polar-repertoire-fit-probe-${summary.seed}.csv`);let matched=null;
 if(summary.date==='1/1/1960'){
  const observed=candidates.reduce((m,c)=>{const k=c.artistId+'|'+c.family;if(!m.has(k))m.set(k,[]);m.get(k).push(c);return m;},new Map());
  assert.equal(new Set(retained.map(r=>r.artistId)).size,actors.length);assert.equal(observed.size,retained.length);
  for(const r of retained){const pool=observed.get(r.artistId+'|'+r.family);assert(pool);assert.equal(pool.length,+r.candidates);assert(Math.abs(Math.max(...pool.map(c=>+c.score))-(+r.bestScore))<1e-6);const best=Math.max(...pools.get(r.artistId).map(c=>+c.score));assert.equal(pool.filter(c=>+c.score>best-.025).length,+r.withinTopWindow);}
  matched={actors:actors.length,familyRows:retained.length,candidateCountsAndBestScores:true,withinWindowCounts:true};
 }
 const archetypes={};for(const c of candidates.filter(c=>c.family==='Gospel Standard')){const k=c.referenceArchetype;if(!archetypes[k])archetypes[k]={candidateInstances:0,score:0,identity:0,capability:0,realized:{}};const m=archetypes[k];m.candidateInstances++;m.score+=+c.score;m.identity+=+c.identity;m.capability+=+c.capability;increment(m.realized,c.resolvedArchetype);}
 const gaps=[];for(const a of summary.summaries){const v=a.variants.find(v=>v.variant==='baseline'),pool=pools.get(a.artistId);const best=pool.find(c=>c.songId===v.bestSongId),gospel=pool.find(c=>c.songId===v.bestGospelSongId);if(best&&gospel)gaps.push({artistId:a.artistId,bestFamily:best.family,bestOrigin:best.originKind,bestArchetype:best.resolvedArchetype,gospelArchetype:gospel.resolvedArchetype,total:+best.score-(+gospel.score),capabilityContribution:.35*((+best.capability)-(+gospel.capability)),identityContribution:.45*((+best.identity)-(+gospel.identity)),momentContribution:.2*((+best.moment)-(+gospel.moment))});}
 for(const g of gaps)assert(Math.abs(g.total-g.capabilityContribution-g.identityContribution-g.momentContribution)<1e-6,'Gap decomposition');
 const families={};for(const c of candidates){if(!families[c.family])families[c.family]={candidateInstances:0,songs:new Set(),routes:{},originYears:{}};const m=families[c.family];m.candidateInstances++;m.songs.add(c.songId);for(const route of c.admissionRoutes.split(';').filter(Boolean))increment(m.routes,route);}
 const poolRoles={};for(const a of actors){const k=a.unsigned==='True'?'unsigned':'signed';if(!poolRoles[k])poolRoles[k]={acts:0,candidates:0,known:0,vocal:0,types:{},members:{}};const m=poolRoles[k];m.acts++;m.candidates+=+a.candidateCount;m.known+=yes(a.knownPerformers)?1:0;m.vocal+=yes(a.vocalist)?1:0;increment(m.types,a.artistType);increment(m.members,a.members);}
 runs.push({prefix,seed:summary.seed,date:summary.date,actors:summary.actors,candidateInstances:summary.candidateInstances,seconds:summary.seconds,context:{explicitKnown:0,unknown:candidates.length,namedGospelBookProxy:candidates.filter(c=>c.family==='Gospel Standard').length},retainedProbeMatch:matched,poolRoles,variants:[...variants.values()].map(m=>({...m,meanBestScoreGap:m.bestGapSum/m.acts,inheritedShare:pct(m.inheritedSlots,m.slots),gospelBookCoverShare:pct(m.gospelBookSlots,m.filledCovers)})),archetypes:Object.fromEntries(Object.entries(archetypes).map(([k,m])=>[k,{...m,score:m.score/m.candidateInstances,identity:m.identity/m.candidateInstances,capability:m.capability/m.candidateInstances}])),meanGapContributions:Object.fromEntries(['total','capabilityContribution','identityContribution','momentContribution'].map(k=>[k,gaps.reduce((n,g)=>n+g[k],0)/gaps.length])),gaps,families:Object.fromEntries(Object.entries(families).map(([k,m])=>[k,{candidateInstances:m.candidateInstances,distinctSongs:m.songs.size,routeCandidateInstances:m.routes}])),originalIdentityCenter:summary.originalIdentityCenter,nativeTaxonomyCenter:summary.nativeTaxonomyCenter,worldUnchanged:summary.worldUnchanged,baselineSelectorMatched:summary.baselineSelectorMatched});
}

function fields(line){const out=[];let s='',q=false;for(let i=0;i<line.length;i++){const c=line[i];if(c==='"'){if(q&&line[i+1]==='"'){s+='"';i++;}else q=!q;}else if(c===','&&!q){out.push(s);s='';}else s+=c;}out.push(s);return q?null:out;}
const historical=[];
for(const seed of [1001,1002]) {
 const path=`SimLogs/polar-repertoire-final-${seed}-repertoire-slots.csv`;digest(path);let header;const groups=new Map(),books=new Map();let excluded=0,malformed=0;
 const initial=new Set(rows(`SimLogs/polar-repertoire-fit-probe-${seed}.csv`).map(r=>r.artistId));
 for await(const line of readline.createInterface({input:fs.createReadStream(path),crlfDelay:Infinity})) {
  if(!line)continue;if(!header){header=fields(line);continue;}
  // Genre precedes all quoted free text in this stream; skip unrelated slots cheaply.
  if(line.split(',',6)[4]!=='Gospel')continue;
  const v=fields(line);if(!v||v.length!==header.length){malformed++;continue;}
  const r=Object.fromEntries(header.map((h,i)=>[h,v[i]]));if(+r.year>1961||+r.year===1961&&+r.month>9){excluded++;continue;}
  const pop=yes(r.unsigned)?'unsigned':'signed',membership=initial.has(r.artistId)?'initialGospelActs':'otherObservedGospelActs';
  const periods=['all',r.year,`${r.year}-Q${Math.ceil(+r.month/3)}`,`${r.year}-${r.month.padStart(2,'0')}`];
  const bookKey=[pop,r.artistId,r.year,r.month].join('|');if(!books.has(bookKey))books.set(bookKey,{artistId:r.artistId,population:pop,membership,slots:0,inherited:0});const book=books.get(bookKey);book.slots++;if(['traditionalLineage','establishedStandard'].includes(r.category))book.inherited++;
  for(const period of periods)for(const scope of ['all',membership]){const key=[pop,period,scope].join('|');if(!groups.has(key))groups.set(key,{population:pop,period,scope,slots:0,inherited:0,categories:{},coverFamilies:{},coverOrigins:{},nonInheritedOrigins:{},actors:new Set()});const m=groups.get(key);m.slots++;m.actors.add(r.artistId);increment(m.categories,r.category);if(['traditionalLineage','establishedStandard'].includes(r.category))m.inherited++;else increment(m.nonInheritedOrigins,r.category==='newlyAuthored'?'newlyAuthored':r.originKind);if(r.category!=='newlyAuthored'){increment(m.coverFamilies,r.seedFamily||r.originKind);increment(m.coverOrigins,r.originKind);}}
 }
 historical.push({seed,cutoff:'1961-09',excludedOctoberAndLaterSlots:excluded,malformedGospelRows:malformed,initialGospelActorIds:initial.size,rows:[...groups.values()].map(m=>({...m,actors:m.actors.size,share:pct(m.inherited,m.slots),coverSlots:m.slots-(m.categories.newlyAuthored??0)})),actMonths:books.size});
 assert.equal(malformed,0,'Malformed retained Gospel rows');
}

const poolGrowth=[];
for(const seed of [1001,1002]){
 const start=[...frameData.values()].find(f=>f.summary.seed===seed&&f.summary.date==='1/1/1960'),later=[...frameData.values()].find(f=>f.summary.seed===seed&&f.summary.date!=='1/1/1960');if(!start||!later)continue;
 const initialIds=new Set(start.actors.map(a=>a.artistId)),pairs=[],scopes={};
 for(const a of later.actors){const p=later.pools.get(a.artistId),old=start.pools.get(a.artistId),scope=initialIds.has(a.artistId)?'initialGospelActs':'otherCurrentGospelActs',key=(yes(a.unsigned)?'unsigned':'signed')+'|'+scope;if(!scopes[key])scopes[key]={acts:0,candidates:0,originals:0,variants:{}};const m=scopes[key];m.acts++;m.candidates+=p.length;m.originals+=+a.originals;
  for(const v of later.summary.summaries.find(s=>s.artistId===a.artistId).variants){if(!m.variants[v.variant])m.variants[v.variant]={covers:0,inherited:0,slots:0,outsideTopWindow:0};const x=m.variants[v.variant];x.covers+=v.filledCovers;x.inherited+=v.inheritedSlots;x.slots+=v.filledCovers+v.originals;x.outsideTopWindow+=v.gospelWithinTopWindow===0?1:0;}
  if(old){const oldIds=new Set(old.map(c=>c.songId)),newIds=new Set(p.map(c=>c.songId)),added=p.filter(c=>!oldIds.has(c.songId)),families={};for(const c of added)increment(families,c.family);pairs.push({artistId:a.artistId,unsigned:yes(a.unsigned),before:old.length,after:p.length,added:added.length,lost:old.filter(c=>!newIds.has(c.songId)).length,addedFamilies:families});}
 }
 poolGrowth.push({seed,startDate:start.summary.date,laterDate:later.summary.date,initialActs:initialIds.size,laterActs:later.actors.length,pairedActs:pairs.length,initialNotCurrentGospel:initialIds.size-pairs.length,scopes:Object.fromEntries(Object.entries(scopes).map(([k,m])=>[k,{...m,variants:Object.fromEntries(Object.entries(m.variants).map(([v,x])=>[v,{...x,share:pct(x.inherited,x.slots)}]))}])),pairs});
}
const admissionRuns=[];
for(const seed of [1001,1002]) {
 const prefix=`polar-gospel-routes-${seed}`,path=`SimLogs/${prefix}-repertoire-admissions.csv`;if(!fs.existsSync(path))continue;
 const manifest=json(`SimLogs/${prefix}-polar-research.json`),events=rows(path),groups={};
 const checkpoint=`SimLogs/${prefix}-polar-checkpoint.json.gz`;digest(checkpoint);
 const snapshot=JSON.parse(zlib.gunzipSync(fs.readFileSync(checkpoint))),songs=snapshot.Save.World.Composition.Songs;
 const originNames=['Unknown','PreGameStandard','PreGameCatalog','Traditional','ArtistOriginal','ProfessionalOffice','LabelStaff','RecentHit'];
 for(const e of events){const song=songs[e.songId];assert(song,'Admission missing composition');const origin=originNames[song.originKind];assert(origin);const key=[e.admissionYear,origin,e.primaryGenre,e.route,e.family||origin].join('|');if(!groups[key])groups[key]={events:0,songs:new Set()};const m=groups[key];m.events++;m.songs.add(e.songId);}
 const oldWeekly=rows(`SimLogs/polar-repertoire-final-${seed}-polar-final-weekly.csv`),newWeekly=rows(`SimLogs/${prefix}-polar-final-weekly.csv`);
 const comparable=oldWeekly.filter(r=>newWeekly.some(n=>n.week===r.week));let weeklyMatches=0;
 for(const r of comparable){if(JSON.stringify(r)===JSON.stringify(newWeekly.find(n=>n.week===r.week)))weeklyMatches++;}
 const late=[...frameData.values()].find(f=>f.summary.seed===seed&&f.summary.date!=='1/1/1960'),selectedRoutes={};
 if(late){const eventRoutes=new Map();for(const e of events){if(!eventRoutes.has(e.songId))eventRoutes.set(e.songId,new Set());eventRoutes.get(e.songId).add(e.route);}for(const s of late.selected.filter(s=>s.variant==='baseline')){const routes=eventRoutes.get(s.songId)??new Set();for(const route of routes)increment(selectedRoutes,s.family+'|'+route);}}
 admissionRuns.push({prefix,seed,observedBoundary:manifest.months.at(-1)?.Date,checkpointDate:`${snapshot.Save.Month}/${snapshot.Save.Day}/${snapshot.Save.Year}`,completedWeeks:snapshot.CompletedAuditWeeks,processSeconds:json(`SimLogs/${prefix}-invocation.json`).elapsedSeconds,weeklyComparison:{comparableRows:comparable.length,identicalRows:weeklyMatches,definition:'Aggregate weekly equality supports trajectory consistency only over these rows; it does not establish complete world or lost original admission-event equality.'},events:events.length,groups:Object.entries(groups).map(([key,m])=>({key,events:m.events,distinctSongs:m.songs.size})),definition:'Admission events can repeat across routes. Year is event year; monthly flushing does not supply event-month timestamps. Origin is joined from checkpoint composition metadata. This fresh trajectory is separate from original interrupted treatments.'});
 admissionRuns.at(-1).selectedCoverRouteSlots=selectedRoutes;
 admissionRuns.at(-1).gospelMarketTaste=snapshot.Save.World.Composition.PolarTaste.Gospel;
 }
for(const path of ['SimTools/PolarGospelAttributionProbe.cs','SimTools/PolarGospelAttributionProbe.tscn','SimTools/run-polar-gospel-attribution.ps1','SimTools/analyze-polar-gospel-attribution.mjs','Data/PolarSongTable.json','Data/PolarRepertoireTable.json','Systems/PolarActProfileDeriver.cs','Systems/PolarCoverResolver.cs','Systems/PolarMaterialFit.cs','Systems/PolarSongBehavior.cs','Systems/LiveRepertoire.cs','Systems/CompositionCatalogService.cs'])digest(path);
const out={schemaVersion:1,scope:'Gospel attribution only; paired fixed-world diagnostic counterfactuals and retained history. No gameplay repair or hold-out validation.',definitions:{numerator:'Traditional lineage plus standards established as of observation year',denominator:'All filled live-set song slots, including newly authored material',context:'Explicit composition content only. Unknown is not secular. Named songbook/genre/archetype proxies are reported separately.',counterfactuals:'Same actors and candidate IDs per world; no new access, genre quotas or constant changes. Original slots stay fixed. Strict sacred-only diagnostic has no known candidates and is not a proposed repair.',historicalCausality:'Original interrupted 1960–September 1961 selection history is descriptive. Lost original admission streams cannot be reconstructed; new bounded trajectories are separate route-mechanism evidence.'},runs,historical,admissionRuns,hashes};
out.poolGrowth=poolGrowth;
fs.writeFileSync('SimLogs/polar-gospel-attribution-analysis.json',JSON.stringify(out,null,2)+'\n');
console.log(JSON.stringify({runs:runs.map(r=>({prefix:r.prefix,actors:r.actors,candidates:r.candidateInstances,retained:r.retainedProbeMatch,variants:r.variants.map(v=>({variant:v.variant,outside:v.gospelOutsideTopWindow,share:v.inheritedShare,slots:v.slots}))})),historical:historical.map(h=>({seed:h.seed,actMonths:h.actMonths})),admissionRuns:admissionRuns.map(r=>({seed:r.seed,boundary:r.observedBoundary,events:r.events}))},null,2));
