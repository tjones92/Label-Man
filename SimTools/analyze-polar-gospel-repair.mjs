import fs from 'node:fs';
import readline from 'node:readline';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import zlib from 'node:zlib';
import {csvRows} from './analyze-polar-research.mjs';
const hashes={},hash=v=>crypto.createHash('sha256').update(v).digest('hex').toUpperCase();
const digest=p=>hashes[p]=hash(fs.readFileSync(p));
const json=p=>{digest(p);return JSON.parse(fs.readFileSync(p,'utf8').replace(/^\uFEFF/,''));};
const rows=p=>{digest(p);const r=csvRows(p);assert.equal(r.malformed,0,p);return r.rows;};
const pct=(n,d)=>d?100*n/d:null, inherited=c=>['traditionalLineage','establishedStandard'].includes(c);
function fields(line){const out=[];let s='',q=false;for(let i=0;i<line.length;i++){const c=line[i];if(c==='"'){if(q&&line[i+1]==='"'){s+='"';i++;}else q=!q;}else if(c===','&&!q){out.push(s);s='';}else s+=c;}out.push(s);assert(!q);return out;}
async function* stream(p){digest(p);let header;for await(const line of readline.createInterface({input:fs.createReadStream(p),crlfDelay:Infinity})){if(!line)continue;const v=fields(line);if(!header){header=v;continue;}assert.equal(v.length,header.length,p);yield Object.fromEntries(header.map((h,i)=>[h,v[i]]));}}
const frameSpecs=[['start',1001,'-v3','polar-gospel-attribution-start-1001-v2'],['start',1002,'-v3','polar-gospel-attribution-start-1002'],['later',1001,'','polar-gospel-attribution-later-1001-v2'],['later',1002,'','polar-gospel-attribution-later-1002-v2']];
const table=json('Data/PolarSongTable.json'),repertoire=json('Data/PolarRepertoireTable.json'),audit=json('SimTools/PolarGenrePriorAudit.json');
const legacy=json('SimTools/PolarSongTable.LegacyAttribution.json');
for(const key of ['archetypes','identityWeights','lyricModifiers','vocalModifiers'])assert.deepEqual(table[key],legacy[key],key+' changed');
for(const [key,value] of Object.entries(legacy.numbers))assert.equal(table.numbers[key],value,'Existing tuning changed: '+key);
const frames=[],shapes=[]; const main=['baseline','packageA','packageB','packageAB','packageABResolved','packageABHalfK','packageABDoubleK'];
for(const [stage,seed,suffix,old] of frameSpecs){
 const prefix=`polar-repair-${stage}-${seed}${suffix}`,root=`SimLogs/${prefix}`,s=json(root+'-gospel-summary.json'),a=rows(root+'-gospel-actors.csv'),c=rows(root+'-gospel-candidates.csv'),selected=rows(root+'-gospel-selections.csv'),inv=json(root+'-invocation.json');
 assert(s.worldUnchanged&&s.baselineSelectorMatched);assert.equal(s.seed,seed);assert.equal(inv.tableSha256,digest('Data/PolarSongTable.json'));assert.equal(inv.repertoireTableSha256,digest('Data/PolarRepertoireTable.json'));
 assert.equal(a.length,s.actors);assert.equal(c.length,s.candidateInstances);assert.equal(s.actorHash,hash(a.map(r=>r.artistId).join('\n')));
 const pools=new Map();for(const r of c){if(!pools.has(r.artistId))pools.set(r.artistId,new Map());const p=pools.get(r.artistId);assert(!p.has(r.songId));p.set(r.songId,r);}
 const selections=new Map();for(const r of selected){const key=r.artistId+'|'+r.variant;if(!selections.has(key))selections.set(key,[]);selections.get(key).push(r);assert(pools.get(r.artistId).has(r.songId));}
 const aggregates=new Map(),gospelIds=new Set(a.filter(r=>r.genre==='Gospel').map(r=>r.artistId));
 for(const actor of s.summaries){const pool=pools.get(actor.artistId)??new Map();assert.equal(pool.size,actor.candidates);assert.equal(actor.poolHash,hash([...pool.keys()].sort().join('\n')));
  for(const v of actor.variants){const chosen=selections.get(actor.artistId+'|'+v.variant)??[];assert.equal(chosen.length,v.filledCovers);assert.equal(new Set(chosen.map(r=>r.songId)).size,chosen.length);assert.equal(chosen.filter(r=>inherited(r.category)).length,v.inheritedSlots);
   assert.equal(v.requestedCovers,actor.variants[0].requestedCovers);assert.equal(v.originals,actor.variants[0].originals);
   if(main.includes(v.variant)&&v.gapCapability!==null)assert(Math.abs(v.bestScore-v.bestGospelScore-v.gapCapability-v.gapIdentity-v.gapMoment)<2e-6);
   for(const scope of ['all',actor.unsigned?'unsigned':'signed',actor.cohort]){const key=[actor.genre,scope,v.variant].join('|');if(!aggregates.has(key))aggregates.set(key,{genre:actor.genre,scope,variant:v.variant,acts:0,slots:0,inherited:0,requested:0,filled:0,outside:0,gapN:0,gapCapability:0,gapIdentity:0,gapMoment:0,retainedCovers:0,baselineCovers:0,shortSets:0});const m=aggregates.get(key);m.acts++;m.slots+=v.filledCovers+v.originals;m.inherited+=v.inheritedSlots;m.requested+=v.requestedCovers+v.originals;m.filled+=v.filledCovers+v.originals;m.shortSets+=v.filledCovers<v.requestedCovers?1:0;m.outside+=v.gospelWithinTopWindow===0?1:0;
    if(v.gapCapability!==null){m.gapN++;for(const k of ['gapCapability','gapIdentity','gapMoment'])m[k]+=v[k];}
    const base=selections.get(actor.artistId+'|baseline')??[];m.baselineCovers+=base.length;const ids=new Set(base.map(r=>r.songId));m.retainedCovers+=chosen.filter(r=>ids.has(r.songId)).length;
   }
  }
 }
 const oldS=json('SimLogs/'+old+'-gospel-summary.json');assert.equal(oldS.worldHash,s.worldHash,'World capture differs from retained frame');
 const oldInv=json('SimLogs/'+old+'-invocation.json');assert.equal(oldInv.tableSha256,digest('SimTools/PolarSongTable.LegacyAttribution.json'));assert.equal(oldInv.repertoireTableSha256,digest('Data/PolarRepertoireTable.json'));
 if(stage==='start')for(const id of gospelIds)assert.deepEqual((selections.get(id+'|packageB')??[]).map(r=>r.songId),(selections.get(id+'|baseline')??[]).map(r=>r.songId),'Startup Package B selection order changed');
 // Compare the exact CSV bytes of every retained variant for the complete Gospel cohort.
 for(const kind of ['candidates','selections']){const path=`SimLogs/${old}-gospel-${kind}.csv`;digest(path);const oldLines=fs.readFileSync(path,'utf8').trimEnd().split(/\r?\n/);const newLines=fs.readFileSync(root+`-gospel-${kind}.csv`,'utf8').trimEnd().split(/\r?\n/);const oldVariants=new Set(oldS.summaries[0].variants.map(v=>v.variant));
  const filtered=newLines.filter((line,i)=>i===0||(gospelIds.has(fields(line)[2])&&(kind!=='selections'||oldVariants.has(fields(line)[4]))));assert.deepEqual(filtered,oldLines,`Retained ${kind} changed`);
 }
 const metrics=[...aggregates.values()].map(m=>({...m,share:pct(m.inherited,m.slots),fill:pct(m.filled,m.requested),retention:pct(m.retainedCovers,m.baselineCovers),gapCapability:m.gapN?m.gapCapability/m.gapN:null,gapIdentity:m.gapN?m.gapIdentity/m.gapN:null,gapMoment:m.gapN?m.gapMoment/m.gapN:null}));
 const genreResults=[];for(const g of Object.keys(s.population)){const b=metrics.find(m=>m.genre===g&&m.scope==='all'&&m.variant==='baseline'),after=metrics.find(m=>m.genre===g&&m.scope==='all'&&m.variant==='packageAB');assert(b&&after);genreResults.push({genre:g,population:s.population[g],observed:b.acts,before:b,after,shareChange:after.share-b.share,fillChange:after.fill-b.fill});}
 const acceptance=metrics.filter(m=>['baseline','packageAB'].includes(m.variant)).flatMap(m=>{const band=repertoire.bands.find(b=>b.genre===m.genre&&b.cohort===m.scope);return band?[{...m,target:[band.min,band.max],sharePass:m.share!==null&&m.share>=band.min&&m.share<=band.max,fillPass:m.filled===m.requested}]:[];});
 const shapeGroups=new Map();let fitRows=0;
 for await(const r of stream(root+'-repair-fits.csv')){fitRows++;assert(pools.get(r.artistId).has(r.songId));assert(Math.abs(+r.score-(.35*+r.capability+.45*+r.identity+.2*+r.moment))<2e-6);if(!['baseline','packageABResolved'].includes(r.variant))continue;
  const key=r.variant+'|'+r.archetype;if(!shapeGroups.has(key))shapeGroups.set(key,{variant:r.variant,archetype:r.archetype,n:0,sums:Array(10).fill(0),squares:Array(10).fill(0),distinct:new Set(),changed:0,instrumentalViolations:0,nearestTemplateDisagreements:0});const m=shapeGroups.get(key),axes=r.shape.split(';').map(Number);assert.equal(axes.length,10);assert(axes.every(v=>Number.isFinite(v)&&v>=0&&v<=1));m.n++;m.distinct.add(r.shape);m.changed+=r.archetype!==r.referenceArchetype?1:0;for(let i=0;i<10;i++){m.sums[i]+=axes[i];m.squares[i]+=axes[i]**2;}if(r.instrumental==='True'&&[0,1,4].some(i=>axes[i]!==0))m.instrumentalViolations++;
  const nearest=table.archetypes.reduce((best,row)=>{const d=row.axes.reduce((n,x,i)=>n+(x-axes[i])**2,0);return !best||d<best.d?{name:row.name,d}:best;},null);m.nearestTemplateDisagreements+=nearest.name!==r.archetype?1:0;
 }
 assert.equal(fitRows,c.length*7);shapes.push({prefix,scope:'candidate arrangements, repeated composition/act instances, not independent completed recordings',rows:[...shapeGroups.values()].map(m=>({variant:m.variant,archetype:m.archetype,instances:m.n,distinctShapes:m.distinct.size,axisStdDev:m.sums.map((sum,i)=>Math.sqrt(Math.max(0,m.squares[i]/m.n-(sum/m.n)**2))),changedFromReferencePercent:pct(m.changed,m.n),instrumentalViolations:m.instrumentalViolations,nearestTemplateDisagreements:m.nearestTemplateDisagreements}))});
 const contextCoverage={unknown:0,sacred:0,secular:0,mixed:0};for(const r of c){assert(Object.hasOwn(contextCoverage,r.subjectContext));contextCoverage[r.subjectContext]++;}
 frames.push({prefix,stage,seed,date:s.date,seconds:s.seconds,worldHash:s.worldHash,actors:s.actors,candidates:s.candidateInstances,gospel:metrics.filter(m=>m.genre==='Gospel'&&m.scope==='all'&&main.includes(m.variant)),metrics,crossGenre:genreResults,acceptance,contextCoverage,retainedVariantsByteIdentical:true});
 console.log(JSON.stringify({prefix,gospel:frames.at(-1).gospel.map(m=>({variant:m.variant,share:m.share,outside:m.outside}))}));
}
const historical=[];
for(const seed of [1001,1002]){const groups=new Map();for await(const r of stream(`SimLogs/polar-repertoire-final-${seed}-repertoire-slots.csv`)){if(!['Folk','Gospel'].includes(r.genre)||+r.year>1961||(+r.year===1961&&+r.month>9))continue;for(const period of [r.year,`${r.year}-Q${Math.ceil(+r.month/3)}`,`${r.year}-${r.month.padStart(2,'0')}`]){const key=[r.genre,r.unsigned,period].join('|');if(!groups.has(key))groups.set(key,{genre:r.genre,population:r.unsigned==='True'?'unsigned':'signed',period,slots:0,inherited:0});const m=groups.get(key);m.slots++;m.inherited+=inherited(r.category)?1:0;}}
 historical.push({seed,scope:'Retained baseline trajectory only; weekly aggregates have no genre/setlist fields. Monthly slot streams provide this measure.',rows:[...groups.values()].map(m=>({...m,share:pct(m.inherited,m.slots)}))});
}
const regressions=frames.flatMap(f=>f.crossGenre.filter(g=>g.genre!=='Gospel'&&(g.fillChange< -1e-6||g.shareChange< -1)).map(g=>({frame:f.prefix,genre:g.genre,shareChange:g.shareChange,fillChange:g.fillChange,definition:'Any >1 percentage point inherited-share decrease or any fill decrease; descriptive panel flag, not population inference.'})));
const gate={gospelBothSeedsAllFrames:frames.every(f=>{const m=f.gospel.find(m=>m.variant==='packageAB');return m.share>=50&&m.share<=90;}),crossGenreRegressionFlags:regressions.length,packageDAllowed:false,holdOutReady:false};gate.packageDAllowed=gate.gospelBothSeedsAllFrames&&regressions.length===0;
const archetypeNames=fs.readFileSync('Data/PolarSongMetadata.cs','utf8').match(/public enum SongArchetype\s*{([\s\S]*?)}/)[1].split(',').map(s=>s.trim());
const recordedShapes=[];
for(const seed of [1001,1002]){
 const path=`SimLogs/polar-gospel-routes-${seed}-polar-checkpoint.json.gz`;digest(path);const snapshot=JSON.parse(zlib.gunzipSync(fs.readFileSync(path))),composition=snapshot.Save.World.Composition,masters=composition.PolarMasters;
 const groups=new Map();let missing=0,covers=0,changed=0,missingOriginal=0;
 for(const m of Object.values(masters)){
  const p=m.cachedProfile;if(!p){missing++;continue;}const axes=p.axes,name=archetypeNames[p.archetype];assert(name);if(!groups.has(name))groups.set(name,{archetype:name,n:0,sums:Array(10).fill(0),squares:Array(10).fill(0),distinct:new Set(),instrumentalViolations:0,nearestTemplateDisagreements:0});const g=groups.get(name);g.n++;g.distinct.add(axes.join(';'));for(let i=0;i<10;i++){g.sums[i]+=axes[i];g.squares[i]+=axes[i]**2;}
  if(m.taxonomy.vocalPresence===2&&[0,1,4].some(i=>axes[i]!==0))g.instrumentalViolations++;
  const nearest=table.archetypes.reduce((best,row)=>{const d=row.axes.reduce((n,x,i)=>n+(x-axes[i])**2,0);return !best||d<best.d?{name:row.name,d}:best;},null);g.nearestTemplateDisagreements+=nearest.name!==name?1:0;
  if(m.parentRecordingId){covers++;const original=masters[composition.Songs[m.songId]?.firstCommittedMasterId];if(!original?.cachedProfile)missingOriginal++;else if(original.cachedProfile.archetype!==p.archetype)changed++;}
 }
 recordedShapes.push({seed,date:`${snapshot.Save.Month}/${snapshot.Save.Day}/${snapshot.Save.Year}`,scope:'Persisted recording caches in retained baseline checkpoints; frozen A+B leaves these recordings unchanged. Resolved candidate arrangements are reported separately.',masters:Object.keys(masters).length,missingCachedProfiles:missing,covers,missingOriginal,changedOriginalArchetype:changed,changedOriginalArchetypePercent:pct(changed,covers-missingOriginal),rows:[...groups.values()].map(g=>({archetype:g.archetype,recordings:g.n,distinctShapes:g.distinct.size,axisStdDev:g.sums.map((sum,i)=>Math.sqrt(Math.max(0,g.squares[i]/g.n-(sum/g.n)**2))),instrumentalViolations:g.instrumentalViolations,nearestTemplateDisagreements:g.nearestTemplateDisagreements}))});
}
const out={schemaVersion:1,scope:'Development seeds 1001/1002; complete Gospel cohorts and deterministic bounded cross-genre panels on four retained worlds.',pseudoCount:4,sensitivity:[2,4,8],audit,frames,regressions,historical,shapes,recordedShapes,gate,hashes};
fs.writeFileSync('SimLogs/polar-gospel-repair-analysis.json',JSON.stringify(out,null,2)+'\n');
const checkpointPackages=[{name:'infrastructure',paths:['SimTools/ChartAuditRunner.PolarResearch.cs','SimTools/run-polar-research.ps1','SimTools/run-polar-final-audit.ps1','SimTools/run-polar-followup-audit.ps1','SimTools/ChartAuditRunner.PolarRepertoire.cs','Systems/RepertoireProvenance.cs']},{name:'A',paths:['Data/PolarSongTable.json','Systems/PolarSongTable.cs','Systems/SimulationSeedBootstrap.cs','SimTools/PolarGenrePriorAudit.json']},{name:'B',paths:['Data/PolarSongProfiles.cs','Systems/PolarSongBehavior.cs','SimTools/PolarGospelRepairChecks.cs']},{name:'C',paths:['SimTools/PolarGospelRepairProbe.cs','SimTools/PolarGospelRepairProbe.tscn','SimTools/run-polar-gospel-repair.ps1','SimTools/analyze-polar-gospel-repair.mjs']}];
for(const p of checkpointPackages){p.sourceHashes=Object.fromEntries(p.paths.map(path=>[path,digest(path)]));p.status=p.name==='C'?'measured-gate-failed':'implemented';p.scope='Final package source checkpoint; no commits or prior user work reset.';fs.writeFileSync(`SimTools/PolarGospelRepairCheckpoint-${p.name}.json`,JSON.stringify(p,null,2)+'\n');}
for(const path of ['SimLogs/polar-repair-build.log','SimLogs/polar-repair-check-final.log','SimLogs/polar-repair-fit-check.log','SimLogs/polar-repair-behavior-check.log'])digest(path);
const buildLog=fs.readFileSync('SimLogs/polar-repair-build.log','utf8');assert(buildLog.includes('Build succeeded.')&&/4 Warning\(s\)/.test(buildLog)&&/0 Error\(s\)/.test(buildLog));
assert(fs.readFileSync('SimLogs/polar-repair-check-final.log','utf8').includes('POLAR_GOSPEL_REPAIR_CHECK_PASS'));
assert(fs.readFileSync('SimLogs/polar-repair-fit-check.log','utf8').includes('POLAR_SONG_FIT_PASS'));
assert(fs.readFileSync('SimLogs/polar-repair-behavior-check.log','utf8').includes('POLAR_SONG_BEHAVIOR_PASS'));
const infrastructure=[];
for(const prefix of ['polar-repair-infrastructure-1001','polar-repair-resume-1001']){
 const root='SimLogs/'+prefix,manifest=json(root+'-polar-research.json'),invocation=json(root+'-invocation.json');assert.equal(manifest.censusMode,'none');assert(manifest.months.every(m=>m.Status==='disabled'));
 digest(root+'.log');const log=fs.readFileSync(root+'.log','utf8');
 const path=root+'-polar-checkpoint.json.gz';digest(path);const checkpoint=JSON.parse(zlib.gunzipSync(fs.readFileSync(path)));
 const admissions=rows(root+'-repertoire-admissions.csv');assert(admissions.every(r=>r.admissionMonth===''||+r.admissionMonth>=1&&+r.admissionMonth<=12));
 if(prefix.includes('resume')){assert(log.includes('POLAR_CHECKPOINT_ROUNDTRIP_PASS'));assert.equal(checkpoint.CompletedAuditWeeks,1);assert.equal(checkpoint.Save.Day,8);}
 else assert(log.includes('CHART_AUDIT_STOPPED')&&log.includes('reason=stopped-time-budget'));
 infrastructure.push({prefix,censusMode:manifest.censusMode,completedWeeks:checkpoint.CompletedAuditWeeks,date:`${checkpoint.Save.Month}/${checkpoint.Save.Day}/${checkpoint.Save.Year}`,admissionEvents:admissions.length,monthsKnown:admissions.filter(r=>r.admissionMonth!=='').length,elapsedSeconds:invocation.elapsedSeconds});
}
digest('SimTools/write-polar-gospel-repair-report.mjs');
fs.writeFileSync('SimTools/PolarGospelRepairValidation.json',JSON.stringify({schemaVersion:1,build:{errors:0,existingWarnings:4,newWarnings:0},checks:{csvShape:true,actorPoolHashes:true,selectedSlotUniqueness:true,provenance:true,decompositionSums:true,baselineProductionOrder:true,worldCaptureHashesUnchanged:true,retainedVariantsByteIdentical:true,archetypeAxesAndExistingTuningUnchanged:true,startupPackageBIdentical:true},gate,infrastructure,checkpointPackages,hashes},null,2)+'\n');
