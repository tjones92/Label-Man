import fs from 'node:fs';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {csvRows} from './analyze-polar-research.mjs';

const json=p=>JSON.parse(fs.readFileSync(p,'utf8').replace(/^\uFEFF/,''));
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const tag=process.argv[2]??'genre-repair-gospel-v3';assert(/^[a-zA-Z0-9-]+$/.test(tag));
const manifestPath=`SimLogs/polar-directive3-trajectory-${tag}-runs.json`;
const manifest=json(manifestPath);
const inherited=r=>['traditionalLineage','establishedStandard'].includes(r.category);
const concentration=rows=>{const counts=new Map();for(const r of rows)counts.set(r.songId,(counts.get(r.songId)??0)+1);
 return {slots:rows.length,uniqueSongs:counts.size,top10Share:100*[...counts.values()].sort((a,b)=>b-a).slice(0,10).reduce((n,x)=>n+x,0)/rows.length};};
const quantile=(values,q)=>values.sort((a,b)=>a-b)[Math.max(0,Math.ceil(values.length*q)-1)]??null;
const measure=rows=>({slots:rows.length,inherited:rows.filter(inherited).length,
 inheritedShare:100*rows.filter(inherited).length/rows.length,
 secular:rows.filter(r=>r.context==='Secular').length,secularShare:100*rows.filter(r=>r.context==='Secular').length/rows.length,
 unknown:rows.filter(r=>r.context==='Unknown').length});
const trajectories=[];
for(const seed of [1001,1002]) {
 const runs=manifest.filter(r=>r.seed===seed).sort((a,b)=>a.slice-b.slice);
 assert.equal(runs.reduce((n,r)=>n+r.weeks,0),91,'Full 91-week window required');
 assert.equal(runs.at(-1).remaining,0);
 const files=runs.map(r=>'SimLogs/'+r.run+'-directive2-quarter-slots.csv');
 const raw=files.flatMap(path=>{const value=csvRows(path);assert.equal(value.malformed,0);return value.rows;});
 const rows=[...new Map(raw.map(r=>[[r.date,r.artistId,r.slot].join('|'),r])).values()].filter(r=>r.genre==='Gospel');
 assert.equal(new Set(rows.map(r=>r.date)).size,21,'Require all 21 monthly observations');
 const total=measure(rows);
 const actBooks=[...Map.groupBy(rows.filter(inherited),r=>r.artistId).values()].map(concentration);
 const periods=[];
 for(const scope of ['all','signed','unsigned']) {
  const selected=rows.filter(r=>scope==='all'||(r.unsigned==='True')===(scope==='unsigned'));
  periods.push({scope,period:'full',...measure(selected)});
  for(const [period,rr] of Map.groupBy(selected,r=>r.year+'-Q'+r.quarter))periods.push({scope,period,...measure(rr)});
 }
 const inputs=runs.map(r=>{
  const root='SimLogs/'+r.run,invocation=json(root+'-invocation.json');
  const log=fs.readFileSync(root+'.log','utf8');
  if(r.slice>1)assert(log.includes('POLAR_CHECKPOINT_ROUNDTRIP_PASS'));
  return {run:r.run,weeks:r.weeks,date:r.date,invocation,files:['-invocation.json','.log','-directive2-quarter-slots.csv','-polar-research.json'].map(s=>({path:root+s,sha256:sha(root+s)}))};
 });
 trajectories.push({seed,weeks:91,lastDate:runs.at(-1).date,monthlyObservations:21,...total,
  inheritedAccepted:total.inheritedShare>=65&&total.inheritedShare<=70,secularAccepted:total.secularShare<=2,
  concentration:concentration(rows.filter(inherited)),actBooks:{medianDistinctSongs:quantile(actBooks.map(r=>r.uniqueSongs),.5),p90DistinctSongs:quantile(actBooks.map(r=>r.uniqueSongs),.9),medianTop10Share:quantile(actBooks.map(r=>r.top10Share),.5)},periods,inputs});
}
const output={definition:'Fresh 1960–September 1961 monthly Gospel trajectory; all live slots, all signing statuses. Pre-existing acceptance: 65–70% inherited, <=2% secular. Unknown contexts remain explicit.',manifest:{path:manifestPath,sha256:sha(manifestPath)},trajectories};
fs.writeFileSync('SimTools/GenreRepertoireRepairGospelRegression.json',JSON.stringify(output,null,2)+'\n');
console.log(JSON.stringify(trajectories.map(({periods,inputs,...r})=>r),null,2));
