// Research from retained CSVs only. Never launches a simulation or changes gameplay.
import fs from 'node:fs';
import readline from 'node:readline';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';

const categories=['newlyAuthored','existingCover','establishedStandard','traditionalLineage'];
const table=JSON.parse(fs.readFileSync('Data/PolarRepertoireTable.json','utf8'));
const prior=JSON.parse(fs.readFileSync('SimLogs/polar-repertoire-baseline-analysis.json','utf8'));
const hashes={}, streams={};
function fields(line){const out=[];let s='',q=false;for(let i=0;i<line.length;i++){const c=line[i];if(c==='"'){if(q&&line[i+1]==='"'){s+='"';i++;}else q=!q;}else if(c===','&&!q){out.push(s);s='';}else s+=c;}out.push(s);return out;}
async function digest(path){const hash=crypto.createHash('sha256');for await(const chunk of fs.createReadStream(path))hash.update(chunk);hashes[path]=hash.digest('hex');}
async function* csv(path){await digest(path);const stats=streams[path]={bytes:fs.statSync(path).size,validRows:0,malformedRows:0};let header;for await(const line of readline.createInterface({input:fs.createReadStream(path),crlfDelay:Infinity})){if(!line)continue;const v=fields(line);if(!header){header=v;continue;}if(v.length!==header.length){stats.malformedRows++;continue;}stats.validRows++;yield Object.fromEntries(header.map((h,i)=>[h,v[i]]));}}
const month=r=>`${r.year}-${String(r.month).padStart(2,'0')}`;
const serial=s=>+s.slice(0,4)*12+(+s.slice(5)-1);
const population=r=>r.unsigned==='True'?'unsigned':'signed';
const increment=(o,k,n=1)=>o[k]=(o[k]??0)+n;
const pct=(n,d)=>d?100*n/d:null;
function fresh(){return {slots:0,categories:Object.fromEntries(categories.map(c=>[c,0])),publicDomain:0,songs:{},families:{},origins:{},archetypes:{},arrangements:{},recent:0,supplied:0};}
function add(m,r){m.slots++;increment(m.categories,r.category);if(r.publicDomain==='True')m.publicDomain++;if(r.category==='newlyAuthored'){if(['ProfessionalOffice','LabelStaff'].includes(r.originKind))m.supplied++;return;}increment(m.songs,r.songId);increment(m.families,r.seedFamily||r.originKind);increment(m.origins,r.originKind);increment(m.archetypes,r.archetype);if(r.arrangementArchetype)increment(m.arrangements,r.arrangementArchetype);if(r.originKind==='RecentHit'||+r.year-+r.originYear<=5)m.recent++;}
function finish(m){assert.equal(Object.values(m.categories).reduce((a,b)=>a+b,0),m.slots);const numerator=m.categories.establishedStandard+m.categories.traditionalLineage,covers=m.slots-m.categories.newlyAuthored;const ranked=o=>Object.entries(o).sort((a,b)=>b[1]-a[1]);const top=ranked(m.songs);return {...m,songs:undefined,numerator,share:pct(numerator,m.slots),originalShare:pct(m.categories.newlyAuthored,m.slots),coverSlots:covers,uniqueCovers:top.length,top10CoverShare:pct(top.slice(0,10).reduce((s,[,n])=>s+n,0),covers),topSongs:top.slice(0,10),familyShares:ranked(m.families).map(([family,count])=>({family,count,share:pct(count,covers)})),dominantArchetype:ranked(m.archetypes)[0]??null,dominantArrangement:ranked(m.arrangements)[0]??null};}
const finals=[1001,1002].map(seed=>`polar-repertoire-final-${seed}`);
const completed={};
for(const prefix of finals){const months=new Set();for await(const r of csv(`SimLogs/${prefix}-polar-live-census.csv`))months.add(month(r));completed[prefix]=[...months].sort();}
const shared=completed[finals[0]].filter(m=>completed[finals[1]].includes(m));
const cutoff=shared.at(-1);assert.equal(cutoff,'1961-09');assert.equal(shared.length,21);
const runs=[];
for(const seed of [1001,1002])for(const mode of ['baseline','treatment']){
 const prefix=mode==='baseline'?`polar-repertoire-phase0-${seed}`:`polar-repertoire-final-${seed}`,root=`SimLogs/${prefix}`;
 console.log(`Reading ${prefix}, through ${cutoff}`);
 const metrics=new Map(),books=new Map(),setMetrics=new Map(),excluded={slots:0,sets:0};
 for await(const r of csv(root+'-repertoire-slots.csv')){
  const mo=month(r);if(mo>cutoff){excluded.slots++;continue;}
  assert(categories.includes(r.category));assert(+r.originYear<=+r.year);
  const pop=population(r),cohort=r.cohort||'unverified';
  const periods=['all',String(r.year),`${r.year}-Q${Math.ceil(+r.month/3)}`,mo];
  for(const scope of ['all',cohort])for(const period of periods){const key=[pop,r.genre,scope,period].join('|');if(!metrics.has(key))metrics.set(key,fresh());add(metrics.get(key),r);}
  const key=[pop,r.genre,r.artistId,mo].join('|');if(!books.has(key))books.set(key,{population:pop,genre:r.genre,artist:r.artistId,month:mo,slots:0,covers:new Set(),numerator:0,originals:0});const b=books.get(key);b.slots++;if(r.category==='newlyAuthored')b.originals++;else b.covers.add(r.songId);if(['establishedStandard','traditionalLineage'].includes(r.category))b.numerator++;
 }
 const setKeys=new Set();
 for await(const r of csv(root+'-polar-diversity-sets.csv')){
  const mo=month(r);if(mo>cutoff){excluded.sets++;continue;}const pop=population(r),key=[pop,r.genre,r.artistId,mo].join('|');assert(!setKeys.has(key));setKeys.add(key);
  const b=books.get(key);assert.equal(b?.slots??0,+r.filledCovers+(+r.originals),`Slot/set mismatch ${prefix} ${key}`);
  for(const period of ['all',String(r.year),`${r.year}-Q${Math.ceil(+r.month/3)}`,mo]){const k=[pop,r.genre,period].join('|');if(!setMetrics.has(k))setMetrics.set(k,{sets:0,eligible:0,empty:0,short:0,covers:0,idTie:0,plateauTie:0,allOriginal:0});const m=setMetrics.get(k);m.sets++;m.eligible+=+r.eligibleSongs;m.covers+=+r.filledCovers;m.idTie+=+r.idTieSelected;m.plateauTie+=+r.exactTieSelected;if(+r.eligibleSongs===0)m.empty++;if(r.shortSet==='True')m.short++;if(+r.filledCovers===0&&+r.originals>=3)m.allOriginal++;}
 }
 for(const key of books.keys())assert(setKeys.has(key),`Missing set ${prefix} ${key}`);
 const overlaps=new Map(),prev=new Map(),firstSeen=new Map(),entryGroups=new Map();
 for(const b of [...books.values()].sort((a,b)=>a.month.localeCompare(b.month))){const actorKey=[b.population,b.artist].join('|');if(!firstSeen.has(actorKey))firstSeen.set(actorKey,b.month);const entry=firstSeen.get(actorKey)==='1960-01'?'observedAtStart':'firstObservedLater';const ek=[b.population,b.genre,entry].join('|');if(!entryGroups.has(ek))entryGroups.set(ek,{slots:0,numerator:0,originals:0,books:0,actors:new Set()});const e=entryGroups.get(ek);e.slots+=b.slots;e.numerator+=b.numerator;e.originals+=b.originals;e.books++;e.actors.add(b.artist);
  const key=[b.population,b.genre,b.artist].join('|'),old=prev.get(key);prev.set(key,b);if(!old||serial(b.month)-serial(old.month)!==1||!old.covers.size||!b.covers.size)continue;
  const intersection=[...b.covers].filter(s=>old.covers.has(s)).length,union=new Set([...old.covers,...b.covers]).size,k=[b.population,b.genre].join('|');if(!overlaps.has(k))overlaps.set(k,{pairs:0,jaccard:0,retained:0,identical:0});const o=overlaps.get(k);o.pairs++;o.jaccard+=intersection/union;o.retained+=intersection/old.covers.size;if(intersection===union)o.identical++;
 }
 const rows=[...metrics].map(([key,m])=>{const [pop,genre,cohort,period]=key.split('|'),band=table.bands.find(b=>b.genre===genre&&b.cohort===cohort);const f=finish(m);return {population:pop,genre,cohort,period,...f,band:band?[band.min,band.max]:null,distance:band?Math.max(band.min-f.share,f.share-band.max,0):null};});
 const sets=[...setMetrics].map(([key,m])=>({key,...m,meanEligible:m.eligible/m.sets,idTieShare:pct(m.idTie,m.covers),plateauTieShare:pct(m.plateauTie,m.covers)}));
 const churn=[...overlaps].map(([key,m])=>({key,pairs:m.pairs,meanJaccard:m.jaccard/m.pairs,meanRetained:m.retained/m.pairs,identicalShare:pct(m.identical,m.pairs)}));
 const entrants=[...entryGroups].map(([key,m])=>({key,slots:m.slots,numerator:m.numerator,share:pct(m.numerator,m.slots),originalShare:pct(m.originals,m.slots),books:m.books,actors:m.actors.size}));
 const weekly=[];for await(const r of csv(root+'-polar-final-weekly.csv')){const parts=r.date.split('/');if(`${parts[2]}-${parts[0].padStart(2,'0')}`<=cutoff)weekly.push(r);}
 const inv=root+'-invocation.json';await digest(inv);const invocation=JSON.parse(fs.readFileSync(inv,'utf8'));await digest(root+'.log');
 runs.push({prefix,seed,mode,cutoff,excluded,slotRows:[...books.values()].reduce((s,b)=>s+b.slots,0),books:books.size,censusSets:sets.filter(s=>s.key.endsWith('|all')).reduce((n,s)=>n+s.sets,0),rows,sets,churn,entrants,weekly:{rows:weekly.length,last:weekly.at(-1)?.date,invalidFit:weekly.reduce((s,r)=>s+(+r.invalidFit),0)},invocation});
 console.log(`Validated ${prefix}: ${books.size} sets, ${runs.at(-1).slotRows} slots`);
}
const unavailable=[];
for(const prefix of finals)for(const suffix of ['repertoire-admissions','repertoire-ordering','repertoire-inventory']){const path=`SimLogs/${prefix}-${suffix}.csv`;if(!fs.existsSync(path)){unavailable.push({path,reason:'Not written before interruption'});continue;}await digest(path);unavailable.push({path,bytes:fs.statSync(path).size,reason:fs.statSync(path).size===0?'Writer not flushed before interruption':'Partial; not used as acceptance evidence'});}
const phases=[],phaseSets=[],phaseOrdering=[],fitProbes=[];
for(const seed of [1001,1002]){
 const phaseMetrics=new Map(),phaseSetMetrics=new Map(),orderingMetrics=new Map();
 for await(const r of csv(`SimLogs/polar-repertoire-phases-${seed}-slots.csv`)){const key=[seed,r.phase,population(r),r.genre].join('|');if(!phaseMetrics.has(key))phaseMetrics.set(key,fresh());add(phaseMetrics.get(key),r);}
 for(const [key,m] of phaseMetrics)phases.push({key,...finish(m)});
 for await(const r of csv(`SimLogs/polar-repertoire-phases-${seed}-sets.csv`)){const key=[seed,r.phase,population(r),r.genre].join('|');if(!phaseSetMetrics.has(key))phaseSetMetrics.set(key,{sets:0,eligible:0,empty:0});const m=phaseSetMetrics.get(key);m.sets++;m.eligible+=+r.eligible;if(+r.eligible===0)m.empty++;}
 for(const [key,m] of phaseSetMetrics)phaseSets.push({key,...m,meanEligible:m.eligible/m.sets});
 for await(const r of csv(`SimLogs/polar-repertoire-phases-${seed}-ordering.csv`)){const key=[seed,population(r),r.genre].join('|');if(!orderingMetrics.has(key))orderingMetrics.set(key,{actors:0,requested:0,changedSlots:0,changedActors:0});const m=orderingMetrics.get(key);m.actors++;m.requested+=+r.requested;m.changedSlots+=+r.changedSlots;if(+r.changedSlots>0)m.changedActors++;}
 for(const [key,m] of orderingMetrics)phaseOrdering.push({key,...m,changedShare:pct(m.changedSlots,m.requested)});
 const actors=new Map(),familyMetrics=new Map();
 for await(const r of csv(`SimLogs/polar-repertoire-fit-probe-${seed}.csv`)){if(!actors.has(r.artistId))actors.set(r.artistId,[]);actors.get(r.artistId).push(r);if(!familyMetrics.has(r.family))familyMetrics.set(r.family,{actorBooks:0,candidates:0,withinTopWindow:0,capability:0,identity:0});const m=familyMetrics.get(r.family);m.actorBooks++;m.candidates+=+r.candidates;m.withinTopWindow+=+r.withinTopWindow;m.capability+=+r.meanCapability;m.identity+=+r.meanIdentity;}
 const gaps=[...actors.values()].map(rows=>{const g=rows.find(r=>r.family==='Gospel Standard');return g?{gap:Math.max(...rows.map(r=>+r.bestScore))-(+g.bestScore),outside:+g.withinTopWindow===0}:null;}).filter(Boolean);
 fitProbes.push({seed,actors:actors.size,knownPerformerActors:[...actors.values()].filter(rows=>rows[0].knownPerformers==='True').length,gospelOutsideTopWindow:gaps.filter(g=>g.outside).length,meanBestGospelScoreGap:gaps.reduce((n,g)=>n+g.gap,0)/gaps.length,maxBestGospelScoreGap:Math.max(...gaps.map(g=>g.gap)),families:[...familyMetrics].map(([family,m])=>({family,actorBooks:m.actorBooks,candidates:m.candidates,withinTopWindow:m.withinTopWindow,meanCapability:m.capability/m.actorBooks,meanIdentity:m.identity/m.actorBooks}))});
}
const smoke={scope:'Two-week completed smoke, seed 1001; a different assembly from final runs, used only as mechanism evidence',prefix:'polar-repertoire-fast-smoke-1001',routes:{},inventory:{}};
for await(const r of csv(`SimLogs/${smoke.prefix}-repertoire-admissions.csv`))increment(smoke.routes,r.route);
for await(const r of csv(`SimLogs/${smoke.prefix}-repertoire-inventory.csv`)){const k=[r.genre,r.seedFamily].join('|');increment(smoke.inventory,k);}
for(const path of ['SimLogs/polar-repertoire-baseline-analysis.json','Data/PolarRepertoireTable.json','Systems/LiveRepertoire.cs','Systems/PolarSongBehavior.cs','Systems/PolarCoverResolver.cs','Systems/SongMaterialSelectionService.cs','Systems/RepertoireProvenance.cs','SimTools/ChartAuditRunner.PolarRepertoire.cs','SimTools/ChartAuditRunner.PolarDiversity.cs','SimTools/ChartAuditRunner.PolarFinal.cs','SimTools/analyze-polar-interrupted.mjs',`SimLogs/${smoke.prefix}-invocation.json`])await digest(path);
const comparisons=[];for(const seed of [1001,1002]){const b=runs.find(r=>r.seed===seed&&r.mode==='baseline'),t=runs.find(r=>r.seed===seed&&r.mode==='treatment');for(const after of t.rows.filter(r=>r.cohort==='all'&&r.period==='all')){const before=b.rows.find(r=>r.population===after.population&&r.genre===after.genre&&r.cohort==='all'&&r.period==='all');if(before)comparisons.push({seed,population:after.population,genre:after.genre,before:{slots:before.slots,numerator:before.numerator,share:before.share,originalShare:before.originalShare,top10CoverShare:before.top10CoverShare,uniqueCovers:before.uniqueCovers},after:{slots:after.slots,numerator:after.numerator,share:after.share,originalShare:after.originalShare,top10CoverShare:after.top10CoverShare,uniqueCovers:after.uniqueCovers},shareChange:after.share-before.share});}}
const out={createdUtc:new Date().toISOString(),scope:'January 1960–September 1961; 21 complete census months in both seeds. October excluded. Partial research, not four-year acceptance.',stopped:{atLocal:'2026-10-04T00:48:41-04:00',pids:[9932,14540,8216,12280],reason:'User requested stop and research existing data'},definitions:{numerator:'Established standard as of observation year plus traditional lineage',denominator:'All filled live-set slots',comparability:'Equal calendar window; trajectories and population membership can differ. Cohort labels differ in baseline, so before/after comparison uses genre totals.',churn:'Adjacent-month same-act, same-genre cover sets; Jaccard and previous-cover retention; variable set sizes included.',entrants:'First observed in this population after January; can include signing/unsigning, not necessarily new births.',missing:'Unflushed data cannot be reconstructed from CSVs; absent admissions are not zero admissions.'},checks:{completeMonths:shared.length,cutoff,partition:true,noFutureMaterial:true,slotSetAgreement:true},runs,comparisons,phaseEffects:phases,phaseSets,phaseOrdering,fitProbes,smoke,bands:table.bands,unavailable,streams,hashes};
fs.writeFileSync('SimLogs/polar-repertoire-interrupted-research.json',JSON.stringify(out,null,2)+'\n');
console.log(JSON.stringify({output:'SimLogs/polar-repertoire-interrupted-research.json',checks:out.checks,runs:runs.map(r=>({prefix:r.prefix,sets:r.books,slots:r.slotRows,weekly:r.weekly})),unavailable},null,2));
