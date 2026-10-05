import fs from 'node:fs';
import {csvRows} from './analyze-polar-research.mjs';
const manifest=JSON.parse(fs.readFileSync('SimLogs/polar-directive3-trajectory-shape-v2-runs.json','utf8').replace(/^\uFEFF/,''));
const out=[];
for(const seed of [1001,1002]) {
 const runs=manifest.filter(r=>r.seed===seed);
 const events=runs.flatMap(r=>csvRows('SimLogs/'+r.run+'-artist-population-events.csv').rows);
 const raw=runs.flatMap(r=>fs.readFileSync('SimLogs/'+r.run+'-directive3-census.jsonl','utf8').trim().split(/\r?\n/).map(JSON.parse));
 const census=[...new Map(raw.map(r=>[r.year+'|'+r.month+'|'+r.artistId,r])).values()];
 const previous=census.filter(r=>r.year===1960&&r.month===12),next=census.filter(r=>r.year===1961&&r.month===1);
 const priorIds=new Set(previous.map(r=>r.artistId));
 const newIds=next.filter(r=>!priorIds.has(r.artistId));
 const byGenre={};
 for(const r of newIds) {
  const relevant=events.filter(e=>e.artistId===r.artistId&&Date.parse(e.date)<=Date.parse(r.date));
  const rotations=relevant.filter(e=>e.eventType==='prospect-rotated'),formations=relevant.filter(e=>e.eventType==='formation');
  const source=rotations.length?'latentRotation':formations.length?'runtimeFormation':'other';
  const g=byGenre[r.genre]??={prior:previous.filter(a=>a.genre===r.genre).length,next:next.filter(a=>a.genre===r.genre).length,arrivals:0,unsignedArrivals:0,unsignedRotations:0,sources:{},cohorts:{},rotationDates:{}};
  g.unsignedArrivals+=r.unsigned;g.unsignedRotations+=r.unsigned&&source==='latentRotation';
  g.arrivals++;g.sources[source]=(g.sources[source]??0)+1;g.cohorts[r.formationCohort]=(g.cohorts[r.formationCohort]??0)+1;
  if(rotations.length)g.rotationDates[rotations.at(-1).date]=(g.rotationDates[rotations.at(-1).date]??0)+1;
 }
 const disappearing=[];
 const months=[...new Set(census.map(r=>r.year*12+r.month))].sort((a,b)=>a-b);
 for(let i=1;i<months.length;i++) {
  const last=census.filter(r=>r.year*12+r.month===months[i-1]),current=census.filter(r=>r.year*12+r.month===months[i]);
  const ids=new Set(current.map(r=>r.artistId));
  for(const r of last.filter(r=>r.genre==='Gospel'&&!ids.has(r.artistId))) {
   const relevant=events.filter(e=>e.artistId===r.artistId&&Date.parse(e.date)>Date.parse(r.date)&&Date.parse(e.date)<=Date.parse(current[0].date));
   disappearing.push({month:current[0].date,artistId:r.artistId,events:relevant.map(e=>({type:e.eventType,date:e.date,unownedWeeks:e.weeksContinuouslyUnowned,cohort:e.cohort}))});
  }
 }
 const expirations=events.filter(e=>e.eventType==='prospect-search-expired');
 out.push({seed,byGenre,firstSearchExpiration:expirations[0],firstGospelSearchExpiration:expirations.find(e=>e.currentPrimaryGenre==='Gospel'),disappearing});
}
fs.writeFileSync('SimTools/GospelPopulationJump.json',JSON.stringify(out,null,2)+'\n');
console.log(JSON.stringify(out.map(r=>({seed:r.seed,gospel:r.byGenre.Gospel,allGenreArrivals:Object.fromEntries(Object.entries(r.byGenre).map(([g,v])=>[g,v.arrivals])),firstGospelSearchExpiration:r.firstGospelSearchExpiration,disappearing:r.disappearing.slice(0,2)})),null,2));
