import fs from 'node:fs';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import zlib from 'node:zlib';
import {csvRows} from './analyze-polar-research.mjs';
const hash=b=>crypto.createHash('sha256').update(b).digest('hex').toUpperCase(), hashes={};
const read=p=>{const b=fs.readFileSync(p);hashes[p]=hash(b);return b.toString('utf8').replace(/^\uFEFF/,'');};
const json=p=>JSON.parse(read(p));
const rows=p=>{read(p);const c=csvRows(p);assert.equal(c.malformed,0,p);return c.rows;};
const pct=(n,d)=>d?100*n/d:null, inherited=c=>['traditionalLineage','establishedStandard'].includes(c);
const table=json('Data/PolarSongTable.json'),rep=json('Data/PolarRepertoireTable.json'),prior=json('SimTools/PolarGenrePriorAudit.json');
const specs=[['start',1001,'-v2','-v3'],['start',1002,'','-v3'],['later',1001,'',''],['later',1002,'','']];
const frames=[],buckets=[],shapes=[],recordedGenres=[];
const genreEnum=new Map([...read('Data/Genre.cs').matchAll(/^\s*(\w+)\s*=\s*(\d+)/gm)].map(m=>[+m[2],m[1]]));
for(const [stage,seed,suffix,oldSuffix] of specs) {
 const prefix=`polar-directive2-${stage}-${seed}${suffix}`,root='SimLogs/'+prefix,old=`SimLogs/polar-repair-${stage}-${seed}${oldSuffix}`;
 const summary=json(root+'-gospel-summary.json'),before=json(old+'-gospel-summary.json'),actors=rows(root+'-gospel-actors.csv'),selection=rows(root+'-gospel-selections.csv');
 assert(summary.worldUnchanged && summary.baselineSelectorMatched);assert(summary.seconds<=120);assert.equal(summary.actorHash,before.actorHash);assert.equal(summary.candidateInstances,before.candidateInstances);
 const actorMap=new Map(actors.map(a=>[a.artistId,a]));
 const rockRoot=`SimLogs/polar-directive2-rock-${stage}-${seed}`,rock=json(rockRoot+'-gospel-summary.json');
 assert(rock.worldUnchanged && rock.baselineSelectorMatched && rock.seconds<=120);assert.equal(rock.worldHash,summary.worldHash);
 for(const actor of rock.summaries){const existing=summary.summaries.find(a=>a.artistId===actor.artistId);assert(existing);assert.equal(existing.poolHash,actor.poolHash);assert.equal(existing.candidates,actor.candidates);}
 const replacements=new Map(rock.summaries.map(a=>[a.artistId,a]));
 summary.summaries=summary.summaries.map(a=>replacements.get(a.artistId)??a);
 const previousActors=rows(old+'-gospel-actors.csv');assert.deepEqual(actors.map(a=>[a.artistId,a.poolHash,a.originals,a.requestedCovers]),previousActors.map(a=>[a.artistId,a.poolHash,a.originals,a.requestedCovers]));
 const oldNames=new Set(before.summaries.find(s=>s.genre==='Gospel').variants.map(v=>v.variant)),gospelIds=new Set(actors.filter(a=>a.genre==='Gospel').map(a=>a.artistId));
 const filter=text=>text.trimEnd().split(/\r?\n/).filter((l,i)=>i===0 || (gospelIds.has(l.split(',')[2]) && oldNames.has(l.split(',')[4])));
 // Compare complete line strings, without modifying or normalizing fields.
 assert.deepEqual(filter(read(root+'-gospel-selections.csv')),filter(read(old+'-gospel-selections.csv')),'Byte-exact retained Gospel selection lines');
 const metrics=new Map();
 for(const actor of summary.summaries)for(const v of actor.variants)for(const scope of ['all',actor.unsigned?'unsigned':'signed',actor.cohort]) {
  const key=[actor.genre,scope,v.variant].join('|');if(!metrics.has(key))metrics.set(key,{genre:actor.genre,scope,variant:v.variant,actors:0,originals:0,slots:0,inherited:0,book:0,filled:0,requested:0});
  const m=metrics.get(key);m.actors++;m.originals+=v.originals;m.slots+=v.originals+v.filledCovers;m.inherited+=v.inheritedSlots;m.book+=v.gospelBookSlots;m.filled+=v.filledCovers;m.requested+=v.requestedCovers;
 }
 const results=[...metrics.values()].map(m=>({...m,share:pct(m.inherited,m.slots)}));
 const cross=Object.keys(summary.population).map(genre=>({genre,variants:results.filter(m=>m.genre===genre && m.scope==='all' && ['baseline','packageA','packageB','packageAB','packageABResolved','packageABH','packageABHResolved','packageABNeutralPreference','packageABFamilyPreference','packageABResolvedNeutralPreference','packageABResolvedFamilyPreference'].includes(m.variant))}));
 const ranges=results.flatMap(m=>{const band=rep.bands.find(b=>b.genre===m.genre && b.cohort===m.scope);return band?[{...m,band:[band.min,band.max],pass:m.share>=band.min&&m.share<=band.max}]:[];});
 const coverage={unknown:0,sacred:0,secular:0,mixed:0};const unique=new Map();
 for(const r of rows(root+'-gospel-candidates.csv'))unique.set(r.songId,r.subjectContext);
 for(const context of unique.values()){assert(Object.hasOwn(coverage,context));coverage[context]++;}assert.equal(coverage.unknown,unique.size);
 frames.push({prefix,seed,stage,date:summary.date,seconds:summary.seconds,rockProbeSeconds:rock.seconds,rockCorrected:true,oldWorldHash:before.worldHash,newWorldHash:summary.worldHash,retainedSelectionLinesByteExact:true,actorPoolsExact:true,coverage,metrics:results,cross,ranges});
 const bucketRoot=stage==='later'?`SimLogs/polar-directive2-bucket-${seed}`:root;
 if(stage==='later'){const precise=json(bucketRoot+'-gospel-summary.json');assert.equal(precise.actorHash,summary.actorHash);assert.equal(precise.candidateInstances,summary.candidateInstances);assert(precise.worldUnchanged&&precise.seconds<=120);}
 const bucketRows=read(bucketRoot+'-within-bucket.jsonl').trim().split(/\r?\n/).filter(Boolean).map(l=>JSON.parse(l));
 fs.writeFileSync(`SimTools/PolarWithinBucket-${stage}-${seed}.json`,JSON.stringify(bucketRows.filter(r=>r.genre==='Gospel'),null,2)+'\n');
 for(const variant of ['packageAB','packageABResolved']) {
  const group=bucketRows.filter(r=>r.genre==='Gospel' && r.variant===variant),families={},ranks=[];let both=0,wins=0,slots=0;
  for(const r of group) {
   for(const [name,n] of Object.entries(r.families))families[name]=(families[name]??0)+n;
   const shared=(r.families['Gospel Standard']??0)>0 && Object.entries(r.families).some(([name,n])=>name!=='Gospel Standard'&&n>0);
   if(shared)both++;
   for(const c of r.candidates)if(c.family==='Gospel Standard')ranks.push(c.rank);
   if(r.attempts)for(const attempt of r.attempts){if(attempt.bucket===0 && attempt.bookAndOtherAvailable && !attempt.refused){slots++;if(attempt.family==='Gospel Standard')wins++;}}
   else for(const selected of r.selected)if(shared && selected.rank>0){slots++;if(selected.family==='Gospel Standard')wins++;}
  }
  const selectedFamilies={};for(const r of group){if(r.attempts){for(const attempt of r.attempts)if(attempt.bucket===0 && attempt.bookAndOtherAvailable && !attempt.refused)selectedFamilies[attempt.family]=(selectedFamilies[attempt.family]??0)+1;}else if((r.families['Gospel Standard']??0)>0 && Object.keys(r.families).some(x=>x!=='Gospel Standard'))for(const selected of r.selected)if(selected.rank>0)selectedFamilies[selected.family]=(selectedFamilies[selected.family]??0)+1;}
  buckets.push({seed,stage,variant,acts:group.length,families,bookRanks:{median:quantile(ranks,.5),p10:quantile(ranks,.1)},bothActs:both,bookWins:wins,sharedWindowSlots:slots,selectedFamilies});
 }
 const compositionRows=rows(root+'-composition-shapes.csv');
 if(stage==='later') {
  const snapshotPath=`SimLogs/polar-gospel-routes-${seed}-polar-checkpoint.json.gz`;
  hashes[snapshotPath]=hash(fs.readFileSync(snapshotPath));
  const world=JSON.parse(zlib.gunzipSync(fs.readFileSync(snapshotPath))).Save.World;
  const coverIds=new Set();
  function covers(obj){if(!obj||typeof obj!=='object')return;if(obj.isCover===true&&obj.songId)coverIds.add(obj.songId);for(const v of Object.values(obj))if(v&&typeof v==='object')covers(v);}
  covers(world.Records);covers(world.RetiredTrackArchive);
  for(const r of compositionRows)if(coverIds.has(r.songId))r.covered='True';
  const masters=Object.values(world.Composition.PolarMasters).filter(m=>m.cachedProfile);
  for(const [genre,mm] of Map.groupBy(masters,m=>genreEnum.get(m.taxonomy.primaryGenre)))recordedGenres.push({seed,genre,recordings:mm.length,distinctCompositions:new Set(mm.map(m=>m.songId)).size,distinctShapes:new Set(mm.map(m=>m.cachedProfile.axes.join(';'))).size});
  for(const variant of ['off','on'])for(const source of ['original','seeded','scouted-imported-professional','covers']) {
   const sample=compositionRows.filter(r=>r.variant===variant && (source==='covers'?r.covered==='True':r.source===source));
   const groups=Map.groupBy(sample,r=>r.archetype);
   for(const [archetype,rr] of groups) {
    const axes=rr.map(r=>r.shape.split(';').map(Number)),template=table.archetypes.find(t=>t.name===archetype),distinct=new Map(),nn=[],nearest=[],disagreements=[];
    let violations=0;for(let i=0;i<axes.length;i++) {
     const a=axes[i];assert(a.length===10 && a.every(v=>Number.isFinite(v)&&v>=0&&v<=1));
     const key=a.map(v=>Math.round(v/1e-6)).join(';');distinct.set(key,(distinct.get(key)??0)+1);
     let best=Infinity;for(let j=0;j<axes.length;j++){if(i===j)continue;let d=0;for(let k=0;k<10;k++)d+=(a[k]-axes[j][k])**2;if(d<best)best=d;}if(axes.length>1)nn.push(Math.sqrt(best));
     let nt=null;for(const row of table.archetypes){const d=row.axes.reduce((sum,v,k)=>sum+(v-a[k])**2,0);if(!nt || d<nt.d)nt={name:row.name,d};}
     nearest.push(Math.sqrt(nt.d));if(nt.name!==archetype)disagreements.push(rr[i].songId);
     if(rr[i].instrumental==='True' && [0,1,4].some(k=>a[k]!==0))violations++;
    }
    const std=Array.from({length:10},(_,k)=>{const mean=axes.reduce((sum,a)=>sum+a[k],0)/axes.length;return Math.sqrt(axes.reduce((sum,a)=>sum+(a[k]-mean)**2,0)/axes.length);});
    const minTemplate=Math.sqrt(Math.min(...table.archetypes.filter(t=>t.name!==archetype).map(t=>t.axes.reduce((sum,v,k)=>sum+(v-template.axes[k])**2,0))));
    shapes.push({seed,source,variant,archetype,compositions:rr.length,recordings:rr.reduce((n,r)=>n+(+r.recordings),0),distinctShapes:distinct.size,collisionRate:pct([...distinct.values()].filter(n=>n>1).reduce((a,n)=>a+n,0),rr.length),nnMedian:quantile(nn,.5),nnP10:quantile(nn,.1),nearestTemplateMedian:quantile(nearest,.5),nearestOtherTemplateDistance:minTemplate,std,axesAboveFloor:std.filter(v=>v>1e-4).length,nearestDisagreements:disagreements.length,disagreementShare:pct(disagreements.length,rr.length),violations});
   }
  }
 }
}
function quantile(xs,q){if(!xs.length)return null;xs=[...xs].sort((a,b)=>a-b);return xs[Math.floor((xs.length-1)*q)];}
const flagged=[];for(const genre of Object.keys(frames[0].cross.reduce((x,r)=>(x[r.genre]=1,x),{}))) {
 if(genre==='Gospel')continue;
 const observations=frames.map(f=>{const v=f.cross.find(g=>g.genre===genre)?.variants;if(!v)return null;const b=v.find(v=>v.variant==='baseline');return {seed:f.seed,stage:f.stage,A:v.find(v=>v.variant==='packageA').inherited-b.inherited,B:v.find(v=>v.variant==='packageB').inherited-b.inherited,AB:v.find(v=>v.variant==='packageAB').inherited-b.inherited,slots:b.slots};}).filter(Boolean);
 const repeats=observations.filter(o=>o.AB<0).length>=2 || observations.filter(o=>o.AB>0).length>=2;
 if(repeats)flagged.push({genre,observations});
}
const centers=prior.map(p=>{const spec=table.genreForms[p.genre],forms=spec.songbook?rep.assignments[spec.songbook].map(r=>[r.archetype,r.weight]):Object.entries(spec.forms),total=forms.reduce((s,[,w])=>s+w,0);return {genre:p.genre,provisionalMixture:!spec.songbook,songbook:spec.songbook??null,forms:forms.map(([name,w])=>[name,w/total]),center:p.derivedCenter,ranks:p.derivedCenter.map((v,i)=>1+prior.filter(other=>other.derivedCenter[i]>v+1e-6).length)};});
for(const f of frames){const eq=json(`SimLogs/polar-directive2-equivalence-${f.stage}-${f.seed}-world-equivalence.json`);assert.equal(eq.projectedWorldHash,f.oldWorldHash);assert.equal(eq.worldHash,f.newWorldHash);f.projectedWorldHash=eq.projectedWorldHash;f.hashChangesOnlyAddedFields=true;}
const report={schemaVersion:1,frames,buckets,flagged,shapes,recordedGenres,centers,hashes,holdOutReady:false};
fs.writeFileSync('SimLogs/polar-directive2-analysis.json',JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify({frames:frames.map(f=>({seed:f.seed,stage:f.stage,seconds:f.seconds,gospel:f.metrics.filter(m=>m.genre==='Gospel'&&m.scope==='all'&&['packageAB','packageABResolved','packageABH','packageABHResolved'].includes(m.variant))})),shapes:shapes.length,flags:flagged.length},null,2));
