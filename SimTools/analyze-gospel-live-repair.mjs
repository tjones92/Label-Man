import fs from 'node:fs';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {csvRows} from './analyze-polar-research.mjs';
const json=p=>JSON.parse(fs.readFileSync(p,'utf8').replace(/^\uFEFF/,''));
const pct=(n,d)=>d?100*n/d:null;
const quantile=(a,q)=>{a.sort((x,y)=>x-y);return a.length?a[Math.max(0,Math.ceil(a.length*q)-1)]:null;};
const inherited=r=>['traditionalLineage','establishedStandard'].includes(r.category);
function concentration(rows) {
 const counts=new Map();for(const r of rows)counts.set(r.songId,(counts.get(r.songId)??0)+1);
 return {slots:rows.length,uniqueSongs:counts.size,top10Share:pct([...counts.values()].sort((a,b)=>b-a).slice(0,10).reduce((s,n)=>s+n,0),rows.length)};
}
const trajectories=[];
for(const seed of [1001,1002]) {
 const value=json(`SimLogs/polar-directive3-trajectory-gospel-live-${seed}-runs.json`),manifest=Array.isArray(value)?value:[value];
 const weeks=manifest.reduce((s,r)=>s+r.weeks,0);
 if(!process.argv.includes('--partial'))assert.equal(weeks,91,'Complete 91-week trajectory required');
 const raw=manifest.flatMap(r=>{const c=csvRows('SimLogs/'+r.run+'-directive2-quarter-slots.csv');assert.equal(c.malformed,0);return c.rows;});
 const rows=[...new Map(raw.map(r=>[r.date+'|'+r.artistId+'|'+r.slot,r])).values()];
 const g=rows.filter(r=>r.genre==='Gospel');
 const quarters=[];
 for(const scope of ['all','signed','unsigned'])for(const [period,rr] of Map.groupBy(g.filter(r=>scope==='all'||(r.unsigned==='True')===(scope==='unsigned')),r=>r.year+'-Q'+r.quarter)) {
  quarters.push({period,scope,slots:rr.length,inherited:rr.filter(inherited).length,inheritedShare:pct(rr.filter(inherited).length,rr.length),
   secular:rr.filter(r=>r.context==='Secular').length,secularShare:pct(rr.filter(r=>r.context==='Secular').length,rr.length),unknown:rr.filter(r=>r.context==='Unknown').length,
   concentration:concentration(rr.filter(inherited))});
 }
 const byAct=[...Map.groupBy(g.filter(inherited),r=>r.artistId).values()].map(concentration);
 const retained=json('SimLogs/polar-directive3-trajectory-shape-v2-runs.json').filter(r=>r.seed===seed).flatMap(r=>csvRows('SimLogs/'+r.run+'-directive2-quarter-slots.csv').rows);
 const retainedRows=[...new Map(retained.map(r=>[r.date+'|'+r.artistId+'|'+r.slot,r])).values()].filter(r=>r.genre==='Gospel');
 trajectories.push({seed,weeks,lastDate:manifest.at(-1).date,monthlyObservations:new Set(g.map(r=>r.date)).size,
  slots:g.length,inherited:g.filter(inherited).length,inheritedShare:pct(g.filter(inherited).length,g.length),
  secular:g.filter(r=>r.context==='Secular').length,secularShare:pct(g.filter(r=>r.context==='Secular').length,g.length),unknown:g.filter(r=>r.context==='Unknown').length,
  concentration:concentration(g.filter(inherited)),retainedV2:{slots:retainedRows.length,inheritedShare:pct(retainedRows.filter(inherited).length,retainedRows.length),concentration:concentration(retainedRows.filter(inherited))},
  actConcentration:{medianTop10Share:quantile(byAct.map(r=>r.top10Share),.5),medianDistinctSongs:quantile(byAct.map(r=>r.uniqueSongs),.5),p90DistinctSongs:quantile(byAct.map(r=>r.uniqueSongs),.9)},quarters,
  nonGospel:[...Map.groupBy(rows.filter(r=>r.genre!=='Gospel'),r=>r.genre)].map(([genre,rr])=>({genre,slots:rr.length,inheritedShare:pct(rr.filter(inherited).length,rr.length)})),
  invocations:manifest.map(r=>json('SimLogs/'+r.run+'-invocation.json'))});
}
const fixed=[];
// Retained-frame final affinity evidence: the smooth alternative was rejected.
for(const seed of [1001,1002]) {
 const data=json(`SimLogs/gospel-source-final-later-${seed}-preference.json`);
 for(const variant of [...new Set(data.rows.map(r=>r.variant))]) {
  const acts=data.rows.filter(r=>r.genre==='Gospel'&&r.variant===variant),rows=acts.flatMap(r=>r.slots);
  fixed.push({seed,date:data.date,variant,actors:acts.length,slots:rows.length,inheritedShare:pct(rows.filter(r=>r.inherited).length,rows.length),
   secularShare:pct(rows.filter(r=>r.context==='Secular').length,rows.length),concentration:concentration(rows.filter(r=>r.inherited)),
   medianAccessibleBook:quantile(acts.map(a=>a.access),.5),diagnosticContextFixtures:data.diagnosticContextFixtures});
 }
 const baseline=new Map(data.rows.filter(r=>r.variant==='v2Legacy'&&r.genre!=='Gospel').map(r=>[r.artistId,JSON.stringify(r.slots)]));
 for(const r of data.rows.filter(r=>r.variant==='v2AffinityOnly'&&r.genre!=='Gospel'))assert.equal(JSON.stringify(r.slots),baseline.get(r.artistId),'Non-Gospel fixed-panel choices unchanged');
}
const files=['Data/PolarRepertoireTable.json','Systems/PolarRepertoireTable.cs','Systems/LiveRepertoire.cs','Systems/SongMaterialSelectionService.cs','Systems/CompositionCatalogService.cs','Systems/PlayerDesk.cs','Systems/SaveGameService.cs','Systems/PolarSongBehavior.cs','SimTools/GospelPreferenceChecks.cs'];
const validation={trajectories,fixed,sourceFit:json('SimTools/GospelSourceFitComparison.json'),populationTrace:json('SimTools/GospelPopulationJump.json'),
 fingerprints:files.map(path=>({path,sha256:crypto.createHash('sha256').update(fs.readFileSync(path)).digest('hex')}))};
fs.writeFileSync('SimTools/GospelLiveRepairValidation.json',JSON.stringify(validation,null,2)+'\n');
console.log(JSON.stringify(trajectories.map(({quarters,invocations,nonGospel,...r})=>r),null,2));
