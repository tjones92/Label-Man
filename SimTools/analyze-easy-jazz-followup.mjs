import fs from 'node:fs';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {readCsv,sum} from './analyze-genre-repertoire-repair-phase0.mjs';
import {analyzePolarResearch} from './analyze-polar-research.mjs';

// Easy recent-hit channel + Jazz screen opportunity (same-world matched), and the Jazz
// instrumental-lineup world change (cross-world against the v1 matched census).
const tag=process.argv[2]??'v2';assert(/^[a-zA-Z0-9-]+$/.test(tag));
const json=p=>JSON.parse(fs.readFileSync(p,'utf8').replace(/^﻿/,''));
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const yes=x=>x==='True',fmt=x=>x==null?'absent':x.toFixed(2);
const RECENT=['Recent Pop Hit','Recent Teen Hit','Recent Country Hit'];

function load(prefix,flag,referenceFile) {
 const before=[],after=[],inputs=[];
 const runs=fs.readdirSync('SimLogs').filter(f=>new RegExp(`^${prefix}-(1001|1002)(-resume\\d+)?-polar-research\\.json$`).test(f)).sort();
 assert(runs.length,'No matched census manifests for '+prefix);
 for(const file of runs) {
  const root='SimLogs/'+file.replace('-polar-research.json',''),manifest=json(root+'-polar-research.json');
  const complete=new Set(manifest.months.filter(m=>m.Status==='complete').map(m=>m.Date));
  for(const m of manifest.months.filter(m=>m.Status==='complete'))assert.equal(m.Observed,m.Planned);
  const invocation=json(root+'-invocation.json');assert(invocation.arguments.includes(flag));assert.equal(invocation.exitCode,0);
  assert(fs.readFileSync(root+'.log','utf8').includes('POLAR_CHECKPOINT_ROUNDTRIP_PASS'));
  inputs.push({run:manifest.run,invocation,months:manifest.months,checks:analyzePolarResearch(manifest.run).checks,
   files:['-invocation.json','-polar-research.json','.log','-genre-followup-slots.csv',referenceFile].map(s=>({path:root+s,sha256:sha(root+s)}))});
  const select=rows=>rows.filter(r=>complete.has(r.date)&&yes(r.crossSection));
  before.push(...select(readCsv(root+referenceFile)));
  after.push(...select(readCsv(root+'-genre-followup-slots.csv')));
 }
 return {before,after,inputs};
}
const pct=(rows,all)=>sum(all)?100*sum(rows)/sum(all):null;
const family=r=>r.seedFamily||`${r.originKind} / ${r.primaryGenre}`;
// Another act's charted original that has become a live cover: an in-game contemporary hit.
const chartedOriginal=r=>r.category==='existingCover'&&r.originKind==='ArtistOriginal';
function measure(rows) {
 const families=[...new Set(rows.filter(r=>r.category==='existingCover').map(family))].sort();
 return {observedSlots:rows.length,weightedSlots:sum(rows),
  screen:pct(rows.filter(r=>r.seedFamily==='Screen instrumental'),rows),stageFilm:pct(rows.filter(r=>r.seedFamily==='Stage and film songs'),rows),
  media:pct(rows.filter(r=>r.originKind.startsWith('ExternalMedia')),rows),
  inherited:pct(rows.filter(r=>['traditionalLineage','establishedStandard'].includes(r.category)),rows),pre1940:pct(rows.filter(r=>+r.originYear<1940),rows),
  recentHits:pct(rows.filter(r=>r.category==='existingCover'&&RECENT.includes(r.seedFamily)),rows),
  contemporaryHits:pct(rows.filter(r=>r.category==='existingCover'&&(RECENT.includes(r.seedFamily)||chartedOriginal(r))),rows),
  existingCoverFamilies:Object.fromEntries(families.map(f=>[f,pct(rows.filter(r=>r.category==='existingCover'&&family(r)===f),rows)]))};
}
const cells=(rows,fn)=>{const out=[];for(const genre of [...new Set(rows.map(r=>r.genre))].sort())for(const unsigned of [true,false])for(const seed of [1001,1002,'pooled'])
 out.push(fn(genre,unsigned,seed,r=>r.genre===genre&&yes(r.unsigned)===unsigned&&(seed==='pooled'||+r.seed===seed)));return out;};

const {before,after,inputs}=load(`easy-jazz-${tag}-matched`,'--easy-jazz-followup-census','-genre-repair-reference-slots.csv');
for(const rows of [before,after]) {
 assert.equal(new Set(rows.map(r=>[r.seed,r.date,r.artistId,r.slot].join('|'))).size,rows.length);
 for(const seed of [1001,1002])assert.equal(new Set(rows.filter(r=>+r.seed===seed).map(r=>r.date)).size,2,'Require January and February for each seed');
}
const observationKey=r=>[r.seed,r.date,r.artistId,r.slot,+r.sampleWeight].join('|');
assert.deepEqual(before.map(observationKey).sort(),after.map(observationKey).sort(),'Paired population, slot counts and weights');
const measures=cells(after,(genre,unsigned,seed,f)=>({genre,unsigned,seed,before:measure(before.filter(f)),after:measure(after.filter(f))}));
const otherGenreMovements=measures.filter(m=>!['EasyListening','Jazz'].includes(m.genre)).map(m=>({genre:m.genre,unsigned:m.unsigned,seed:m.seed,
 mediaDelta:m.before.media==null?null:m.after.media-m.before.media,inheritedDelta:m.before.inherited==null?null:m.after.inherited-m.before.inherited,
 identical:JSON.stringify(m.before)===JSON.stringify(m.after)}));
const otherGenresAccepted=otherGenreMovements.every(m=>[m.mediaDelta,m.inheritedDelta].every(v=>v==null||Math.abs(v)<=.5));
const easy=measures.filter(m=>m.genre==='EasyListening'),jazz=measures.filter(m=>m.genre==='Jazz');
const jazzAcceptance=jazz.map(m=>({unsigned:m.unsigned,seed:m.seed,screen:m.after.screen,inScreenBand:m.after.screen>=3&&m.after.screen<=5,
 inheritedDelta:m.after.inherited-m.before.inherited,stageFilmDelta:m.after.stageFilm-m.before.stageFilm}));
const easyInheritedDelta=easy.map(m=>({unsigned:m.unsigned,seed:m.seed,inheritedDelta:m.after.inherited-m.before.inherited}));

// Per-act spread of Easy's contemporary hits (does the disposition separate Faith from Vaughn?).
const actShares=[...new Set(after.filter(r=>r.genre==='EasyListening').map(r=>r.seed+'|'+r.artistId))].map(k=>{
 const rows=after.filter(r=>r.genre==='EasyListening'&&r.seed+'|'+r.artistId===k);
 return 100*rows.filter(r=>r.category==='existingCover'&&(RECENT.includes(r.seedFamily)||chartedOriginal(r))).length/rows.length;
}).sort((a,b)=>a-b);
const q=p=>actShares.length?actShares[Math.min(actShares.length-1,Math.floor(p*actShares.length))]:null;
const easyActSpread={acts:actShares.length,zero:actShares.filter(x=>x===0).length,p25:q(.25),median:q(.5),p75:q(.75),p90:q(.9),max:actShares.at(-1)};

// Jazz lineups in the sampled population, and the world change against the v1 matched census.
const jazzActs=new Map(after.filter(r=>r.genre==='Jazz').map(r=>[r.seed+'|'+r.artistId,yes(r.instrumental)]));
const jazzInstrumental={acts:jazzActs.size,instrumental:[...jazzActs.values()].filter(Boolean).length};
const v1=load('easy-soundtrack-v1-matched','--easy-soundtrack-census','-genre-repair-reference-slots.csv');
const v1Jazz=new Map(v1.after.filter(r=>r.genre==='Jazz').map(r=>[r.seed+'|'+r.artistId,yes(r.instrumental)]));
const worldChange=cells(v1.after,(genre,unsigned,seed,f)=>{
 const a=measure(v1.after.filter(f)),b=measure(before.filter(f));
 return {genre,unsigned,seed,v1:{media:a.media,inherited:a.inherited,screen:a.screen},newWorldReference:{media:b.media,inherited:b.inherited,screen:b.screen},
  mediaDelta:b.media==null||a.media==null?null:b.media-a.media,inheritedDelta:b.inherited==null||a.inherited==null?null:b.inherited-a.inherited};
});

const sources=['Systems/ArtistManager.cs','Systems/LiveRepertoire.cs','Systems/SongMaterialSelectionService.cs','Systems/PolarRepertoireTable.cs','Data/PolarRepertoireTable.json',
 'SimTools/ChartAuditRunner.GenreFollowUp.cs','SimTools/ChartAuditRunner.PolarResearch.cs','SimTools/GenreRepertoireRepairChecks.cs','SimTools/run-polar-research.ps1',
 'SimTools/run-easy-jazz-followup.ps1','SimTools/analyze-easy-jazz-followup.mjs','SimTools/write-easy-jazz-followup-report.mjs'].map(path=>({path,sha256:sha(path)}));
const checks=[];
for(const seed of [1001,1002])for(const [check,marker] of [['genre-repertoire-repair-check','GENRE_REPERTOIRE_REPAIR_CHECK_PASS'],['folk-easy-check','FOLK_EASY_CHECK_PASS'],['gospel-preference-check','GOSPEL_PREFERENCE_CHECK_PASS'],['polar-directive3-check','POLAR_DIRECTIVE3_CHECK_PASS']]) {
 const path=`SimLogs/easy-jazz-${tag}-${check}-${seed}.log`;
 const log=fs.existsSync(path)?fs.readFileSync(path,'utf8'):'';
 checks.push({seed,check,path,sha256:log?sha(path):null,passed:log.includes(marker)&&!log.includes('SAVELOAD_ROUNDTRIP_ERROR')});
}
const output={definitions:{before:'Same world, same private census RNG, with only the Easy recent-hit channel and the Jazz screen opportunity disabled',
 after:'Both channels enabled',worldChange:'v1 matched census (old world, v1 selector) against this run\'s reference (new Jazz lineups, v1 selector); different worlds and samples, so descriptive only',
 shares:'Weighted percentage of all filled slots',contemporaryHits:'Recent Pop/Teen/Country Hit seed families plus other acts\' charted originals covered live',
 window:'January and February 1963; development seeds 1001/1002'},
 sources,inputs,checks,measures,otherGenreMovements,otherGenresAccepted,jazzAcceptance,easyInheritedDelta,easyActSpread,jazzInstrumental,
 v1JazzInstrumental:{acts:v1Jazz.size,instrumental:[...v1Jazz.values()].filter(Boolean).length},worldChange};
fs.writeFileSync('SimLogs/EasyJazzFollowUpValidation.json',JSON.stringify(output,null,2)+'\n');
console.log(JSON.stringify({easy:easy.filter(m=>m.seed==='pooled').map(m=>({unsigned:m.unsigned,before:{recent:m.before.recentHits,contemporary:m.before.contemporaryHits,screen:m.before.screen,media:m.before.media,inherited:m.before.inherited},after:{recent:m.after.recentHits,contemporary:m.after.contemporaryHits,screen:m.after.screen,media:m.after.media,inherited:m.after.inherited}})),
 jazzAcceptance,easyActSpread,jazzInstrumental,otherGenresAccepted,maxOtherGenreDelta:Math.max(...otherGenreMovements.flatMap(m=>[Math.abs(m.mediaDelta??0),Math.abs(m.inheritedDelta??0)])),
 othersIdentical:otherGenreMovements.every(m=>m.identical),checks:checks.map(c=>c.check+':'+c.seed+':'+c.passed)},null,2));
