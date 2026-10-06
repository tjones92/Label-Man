import fs from 'node:fs';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {csvRows} from './analyze-polar-research.mjs';

const hash=b=>crypto.createHash('sha256').update(b).digest('hex').toUpperCase();
const json=p=>JSON.parse(fs.readFileSync(p,'utf8').replace(/^\uFEFF/,''));
const lines=p=>fs.readFileSync(p,'utf8').trim().split(/\r?\n/).filter(Boolean).map(l=>JSON.parse(l));
const csv=p=>{const c=csvRows(p);assert.equal(c.malformed,0,p);return c.rows;};
const pct=(n,d)=>d?100*n/d:null;
const quantile=(a,q)=>{a=a.filter(Number.isFinite).sort((x,y)=>x-y);return a.length?a[Math.max(0,Math.ceil(a.length*q)-1)]:null;};
const group=(a,key)=>{const m=new Map();for(const x of a){const k=key(x);if(!m.has(k))m.set(k,[]);m.get(k).push(x);}return m;};
const table=json('Data/PolarSongTable.json'),registered=json('SimTools/PolarGospelDirective3Checkpoint.json');
for(const f of registered.files)assert.equal(hash(fs.readFileSync(f.path)),f.sha256,'Immutable preregistration');
const inherited=c=>['establishedStandard','traditionalLineage'].includes(c);
const prefixes=[['start',1001,'polar-directive3-final-start-1001','polar-directive2-start-1001-v2'],['start',1002,'polar-directive3-final-start-1002','polar-directive2-start-1002'],['later',1001,'polar-directive3-final-later-1001','polar-directive2-later-1001'],['later',1002,'polar-directive3-final-later-1002','polar-directive2-later-1002']];
const preview=process.argv.includes('--shapes-only');
const shapeResults=[],frameResults=[];
let sample=null;
// Hash-ranked pair candidates, bounded to 2,000 without storing the full Cartesian product.
function pairs(n,key) {
 const total=n*(n-1)/2,target=Math.min(2000,total),seen=new Set(),result=[];
 let state=parseInt(hash(Buffer.from(key)).slice(0,8),16)>>>0;
 function next(){state^=state<<13;state^=state>>>17;state^=state<<5;return state>>>0;}
 if(total<=2000){for(let i=0;i<n;i++)for(let j=i+1;j<n;j++)result.push([i,j]);return result;}
 while(result.length<target){let i=next()%n,j=next()%n;if(i===j)continue;if(i>j)[i,j]=[j,i];const k=i+'|'+j;if(seen.has(k))continue;seen.add(k);result.push([i,j]);}
 // Fixed deterministic sampling, hash-ordered after selection; no simulation RNG.
 return result.sort((a,b)=>hash(Buffer.from(key+'|'+a)).localeCompare(hash(Buffer.from(key+'|'+b))));
}
function shapeMeasures(rr,variant,key) {
 const shapes=rr.map(r=>r.shapes[variant]),pair=pairs(rr.length,key),max=[],demandMax=[];let two=0,twoDemand=0,near=0,nearDemand=0;
 const overlaps={},pureOverlaps={};let disagree=0,pureDisagree=0;
 const boundaries=Array(10).fill(0),nn=[];
 for(const [i,j] of pair) {
  const delta=shapes[i].map((v,k)=>Math.abs(v-shapes[j][k])),a=Math.max(...delta),d=Math.max(...delta.slice(0,6));
  max.push(a);demandMax.push(d);two+=delta.filter(v=>v>=.04).length>=2;twoDemand+=delta.slice(0,6).filter(v=>v>=.04).length>=2;near+=a<.02;nearDemand+=d<.02;
 }
 for(let i=0;i<shapes.length;i++) {
  const a=shapes[i];assert(a.length===10&&a.every(v=>Number.isFinite(v)&&v>=0&&v<=1));
  const template=rr[i].template,instrumental=[0,1,4].every(k=>template[k]===0);
  if(instrumental)assert([0,1,4].every(k=>a[k]===0),'Exact instrumental zeros');
  a.forEach((v,k)=>{if(v===0||v===1)boundaries[k]++;});
  function nearest(values){return table.archetypes.reduce((best,row)=>{const distance=row.axes.reduce((s,v,k)=>s+(v-values[k])**2,0);return !best||distance<best.distance?{name:row.name,distance}:best;},null).name;}
  const neighbor=nearest(a);if(neighbor!==rr[i].archetype){disagree++;overlaps[neighbor]=(overlaps[neighbor]??0)+1;}
  if(variant==='v2'){const pure=nearest(template.map((v,k)=>v+rr[i].offsets[k]));if(pure!==rr[i].archetype){pureDisagree++;pureOverlaps[pure]=(pureOverlaps[pure]??0)+1;}}
  // Exact nearest-neighbor distances within the same archetype/source.
  if(shapes.length>1){let best=Infinity;for(let j=0;j<shapes.length;j++){if(i===j)continue;let sum=0;for(let k=0;k<10;k++)sum+=(a[k]-shapes[j][k])**2;if(sum<best)best=sum;}nn.push(Math.sqrt(best));}
 }
 const result={variant,count:rr.length,pairCount:pair.length,maxDifference:{median:quantile(max,.5),p10:quantile(max,.1)},demandMaxDifference:{median:quantile(demandMax,.5),p10:quantile(demandMax,.1)},twoAxes:pct(two,pair.length),twoDemandAxes:pct(twoDemand,pair.length),nearIdentical:pct(near,pair.length),nearIdenticalDemand:pct(nearDemand,pair.length),nearestNeighborMedian:quantile(nn,.5),disagreements:disagree,overlaps,pureDisagreements:variant==='v2'?pureDisagree:null,pureOverlaps:variant==='v2'?pureOverlaps:null,boundaryShares:boundaries.map(n=>pct(n,rr.length)),clampedShares:variant==='v2'?Array(10).fill(0):null};
 result.proposedReadability=pair.length?result.demandMaxDifference.median>=.05&&result.demandMaxDifference.p10>=.04&&result.twoDemandAxes>=75&&result.nearIdenticalDemand<=5:null;
 return result;
}
for(const [stage,seed,prefix,oldPrefix] of prefixes) {
 const root='SimLogs/'+prefix,shapePath=root+'-directive3-shapes.jsonl';
 if(!fs.existsSync(shapePath)){if(preview)continue;throw Error('Missing frame '+prefix);}
 const shapes=lines(shapePath);if(seed===1001)sample={prefix,shapes};
 const measures=[];
 for(const [key,rr] of group(shapes,r=>r.source+'|'+r.archetype)) {
  const versions=['legacy','v1','v2'].map(v=>shapeMeasures(rr,v,seed+'|'+stage+'|'+key));
  measures.push({source:rr[0].source,archetype:rr[0].archetype,versions,diversityFloor:versions[2].nearestNeighborMedian===null?null:versions[2].nearestNeighborMedian>=versions[0].nearestNeighborMedian});
 }
 const v2=measures.map(m=>m.versions[2]),n=v2.reduce((s,m)=>s+m.count,0),d=v2.reduce((s,m)=>s+m.disagreements,0),pure=v2.reduce((s,m)=>s+m.pureDisagreements,0);
 shapeResults.push({prefix,seed,stage,count:n,disagreements:d,disagreementShare:pct(d,n),pureDisagreements:pure,pureDisagreementShare:pct(pure,n),proposedSoftGuardPass:pct(d,n)<=5,groups:measures,diversityFloorFailures:measures.filter(m=>m.diversityFloor===false).map(m=>m.source+'|'+m.archetype),readabilityFailures:measures.filter(m=>m.versions[2].proposedReadability===false).map(m=>m.source+'|'+m.archetype)});
 if(preview)continue;
 const summary=json(root+'-gospel-summary.json'),old=json('SimLogs/'+oldPrefix+'-gospel-summary.json');
 assert(summary.worldUnchanged&&summary.baselineSelectorMatched);assert.equal(summary.actorHash,old.actorHash);assert.equal(summary.candidateInstances,old.candidateInstances);assert.equal(summary.worldHash,old.worldHash);
 const equivalence=json(`SimLogs/polar-directive2-equivalence-${stage}-${seed}-world-equivalence.json`);assert.equal(summary.projectedWorldHash,equivalence.projectedWorldHash);
 const actors=csv(root+'-gospel-actors.csv'),prior=csv('SimLogs/'+oldPrefix+'-gospel-actors.csv');
 assert.deepEqual(actors.map(a=>[a.artistId,a.poolHash,a.originals,a.requestedCovers]),prior.map(a=>[a.artistId,a.poolHash,a.originals,a.requestedCovers]));
 const names=new Set(old.summaries.find(a=>a.genre==='Gospel').variants.map(v=>v.variant)),gospelIds=new Set(actors.filter(a=>a.genre==='Gospel').map(a=>a.artistId));
 const retained=p=>fs.readFileSync(p,'utf8').trimEnd().split(/\r?\n/).filter((l,i)=>i===0||(gospelIds.has(l.split(',')[2])&&names.has(l.split(',')[4])));
 assert.deepEqual(retained(root+'-gospel-selections.csv'),retained('SimLogs/'+oldPrefix+'-gospel-selections.csv'),'Retained selection lines byte-exact');
 const data=lines(root+'-directive3-actors.jsonl'),metrics=[];
 for(const [key,aa] of group(data,a=>a.genre)) {
  const variants=[...new Set(aa.flatMap(a=>a.variants.map(v=>v.variant)))];
  for(const name of variants)for(const scope of ['all','signed','unsigned']) {
   const acts=aa.filter(a=>scope==='all'||a.unsigned===(scope==='unsigned'));
   const vv=acts.map(a=>a.variants.find(v=>v.variant===name));assert(vv.every(Boolean));
   for(let i=0;i<vv.length;i++){const baseline=acts[i].variants[0];assert.equal(vv[i].originals,baseline.originals);assert.equal(vv[i].requestedCovers,baseline.requestedCovers);assert.equal(new Set(vv[i].selections.map(s=>s.songId)).size,vv[i].selections.length);}
   const slots=vv.reduce((s,v)=>s+v.originals+v.filledCovers,0),count=vv.reduce((s,v)=>s+v.inheritedSlots,0),families={};let contested=0,wins=0;
   for(const v of vv){for(const [f,n] of Object.entries(v.firstFamilies))families[f]=(families[f]??0)+n;for(const a of v.attempts)if(a.bucket===0&&a.bookAndOtherAvailable&&!a.refused){contested++;wins+=a.family==='Gospel Standard';}}
   const access=vv.map(v=>v.bookAccessible);
   metrics.push({genre:key,scope,variant:name,actors:acts.length,slots,inherited:count,share:pct(count,slots),bookAccessible:{median:quantile(access,.5),p10:quantile(access,.1),p90:quantile(access,.9)},firstFamilies:families,contested,wins,winShare:pct(wins,contested)});
  }
 }
 const interactions=metrics.filter(m=>m.scope==='all'&&m.variant.startsWith('v2')).map(m=>{const ref=metrics.find(b=>b.genre===m.genre&&b.scope==='all'&&b.variant===m.variant.replace('v2','v1'));return {genre:m.genre,variant:m.variant,delta:m.inherited-ref.inherited,flag:Math.abs(m.inherited-ref.inherited)>2};});
 const strata=[];
 for(const [stratum,aa] of group(data.filter(a=>a.genre==='Gospel'),a=>a.albumCount===0?'0':a.albumCount<=12?'1–12':a.albumCount<=48?'13–48':'49+')) {
  const vv=aa.map(a=>a.variants.find(v=>v.variant==='packageABResolved')),n=vv.reduce((s,v)=>s+v.inheritedSlots,0),d=vv.reduce((s,v)=>s+v.originals+v.filledCovers,0);strata.push({stratum,acts:aa.length,inherited:n,slots:d,share:pct(n,d)});
 }
 const fitRows=csv(root+'-directive3-fits.csv'),fitBands=[];
 // The production widget displays continuous uncertainty intervals, not discrete fit tiers.
 // These equal-width bins are descriptive reporting bins, not new gameplay bands.
 const fitGroups=[...group(fitRows,r=>r.variant+'|'+r.archetype),...group(fitRows.map(r=>({...r,archetype:'overall'})),r=>r.variant)];
 for(const [key,rr] of fitGroups)for(const axis of ['capability','identity','moment']) {
  const bins=Array(5).fill(0);for(const r of rr)bins[Math.min(4,Math.floor(+r[axis]*5))]++;
  fitBands.push({variant:rr[0].variant,archetype:rr[0].archetype,axis,n:rr.length,bins:bins.map(n=>pct(n,rr.length))});
 }
 const fixedAct=data.find(a=>a.genre==='Gospel')?.artistId,spread=[];
 for(const [key,rr] of group(fitRows.filter(r=>r.artistId===fixedAct),r=>r.variant+'|'+r.archetype))spread.push({variant:rr[0].variant,archetype:rr[0].archetype,songs:rr.length,capability:[Math.min(...rr.map(r=>+r.capability)),Math.max(...rr.map(r=>+r.capability))],identity:[Math.min(...rr.map(r=>+r.identity)),Math.max(...rr.map(r=>+r.identity))],moment:[Math.min(...rr.map(r=>+r.moment)),Math.max(...rr.map(r=>+r.moment))]});
 const source=json(root+'-directive3-dose-source.json');assert.equal(source.length,350);assert(source.every(s=>['Sacred','Unknown'].includes(s.context)));
 const doseDistributions=[350,525,700,1050].map(size=>{const rows=source.concat(Array.from({length:size-350},(_,i)=>({...source[i%350],context:'Sacred'})));const numeric=keys=>Object.fromEntries(keys.map(key=>[key,{p10:quantile(rows.map(r=>r[key]),.1),median:quantile(rows.map(r=>r[key]),.5),p90:quantile(rows.map(r=>r[key]),.9),mean:rows.reduce((sum,r)=>sum+r[key],0)/size}]));return{size,archetypes:Object.fromEntries([...group(rows,r=>r.archetype)].map(([key,rr])=>[key,rr.length])),...numeric(['commercialHook','nationalFamiliarity','adultFamiliarity','teenFamiliarity','originYear']),jitter:'Exact prototype arrays copied cyclically; 525 contains the first 175 IDs twice',contexts:Object.fromEntries([...group(rows,r=>r.context)].map(([key,rr])=>[key,rr.length])),placeholderSacred:size-350};});
 frameResults.push({prefix,seed,stage,date:summary.date,worldUnchanged:true,retainedSelectionLinesByteExact:true,actorPoolsExact:true,projectedWorldHash:summary.projectedWorldHash,metrics,interactions,strata,fitBands,fixedAct,withinArchetypeFitSpread:spread,doseDistributions});
}

function escape(s){return String(s??'').replace(/[&<>\"]/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[c]));}
function svg(a,color,label) {
 const spoke=(v,i)=>[168+Math.cos(-Math.PI/2+i*Math.PI/3)*95*v,160+Math.sin(-Math.PI/2+i*Math.PI/3)*95*v];
 const polygon=a=>a.slice(0,6).map((v,i)=>spoke(v,i).join(',')).join(' ');
 let s=`<svg width="550" height="355" viewBox="0 0 550 355" role="img" aria-label="${label}"><text x="28" y="24">${label}</text>`;
 for(const r of [.25,.5,.75,1])s+=`<polygon points="${polygon(Array(6).fill(r))}" fill="none" stroke="#d5cfbf"/>`;
 const labels=['Vocal power','Vocal nuance','Musicianship','Ensemble','Lyric delivery','Studio craft'];
 for(let i=0;i<6;i++){const p=spoke(1,i),q=spoke(1.26,i);s+=`<path d="M168 160L${p[0]} ${p[1]}" stroke="#d5cfbf"/><text x="${q[0]}" y="${q[1]}" text-anchor="middle" font-size="12">${labels[i]}</text>`;}
 s+=`<polygon points="${polygon(a)}" fill="${color}" fill-opacity=".18" stroke="${color}" stroke-width="2"/><text x="368" y="24">Identity</text><rect x="368" y="67" width="170" height="170" fill="none" stroke="#d5cfbf"/><path d="M368 152H538M453 67V237" stroke="#d5cfbf"/><circle cx="${368+170*a[6]}" cy="${67+170*(1-a[7])}" r="4" fill="${color}"/><text x="368" y="260" font-size="12">Gentle → tough</text><text x="368" y="278" font-size="12">Up = sophisticated</text><text x="368" y="301" font-size="12">Sincerity ${a[8].toFixed(3)}</text><text x="368" y="320" font-size="12">Maturity ${a[9].toFixed(3)}</text></svg>`;return s;
}
if(sample) {
 const top=[...group(sample.shapes,s=>s.archetype)].sort((a,b)=>b[1].length-a[1].length||a[0].localeCompare(b[0])).slice(0,10);
 let html='<!doctype html><html lang="en"><meta charset="utf-8"><title>Polar shape sample sheet — Directive 3</title><style>body{font:16px system-ui;background:#faf7ef;color:#30291d;margin:32px}h1{font-size:28px}p{max-width:1000px;line-height:1.5}.song{border-top:1px solid #ded7c6;margin:20px 0;padding-top:12px}.pair{display:flex;gap:24px;width:max-content}svg{background:#fffdf7;flex:none}svg text{font-family:system-ui;fill:#30291d}h2{margin-top:52px}small{color:#75674d}</style><h1>Same archetype, different songs</h1>';
 html+=`<p>Eight deterministic samples for each of the ten most common archetypes in ${escape(sample.prefix)}. Left: v1. Right: v2. Six demand spokes, 95-pixel radius, and a 170-pixel identity projection match the real widget at both its default and minimum supported sizes. These are composition truth outlines; player uncertainty bands and act overlays are omitted. Archetype labels remain stored taxonomy labels.</p><p>Review at 100% browser zoom. A 0.05 radial difference is 4.75 pixels. Scroll horizontally if necessary; diagrams are never scaled down.</p>`;
 for(const [archetype,rr] of top){html+=`<h2>${escape(archetype)} <small>${rr.length} compositions in frame</small></h2>`;const chosen=[...rr].sort((a,b)=>hash(Buffer.from(a.songId+'|sample-sheet')).localeCompare(hash(Buffer.from(b.songId+'|sample-sheet')))).slice(0,8);assert.equal(chosen.length,8);for(const r of chosen)html+=`<div class="song"><strong>${escape(r.title)}</strong> <small>${escape(r.songId)} · ${escape(r.source)}</small><div class="pair">${svg(r.shapes.v1,'#be8840','v1')}${svg(r.shapes.v2,'#725989','v2')}</div></div>`;}
 html+='</html>';fs.writeFileSync('SimTools/PolarShapeSampleSheet.html',html);
}
const out={registeredCommit:registered.commit,phase3:'gated; historical reachable canon not established',shapeResults,frames:frameResults,definition:'Same actors, pools, originals and requested covers. D2 alone adds flagged in-memory clones; D3 alone filters admission. Pure offset overlap and full authored-profile overlap reported separately. Reporting fit bins are equal-width descriptive bins; widget bands are continuous intervals.'};
fs.writeFileSync(preview?'SimLogs/polar-directive3-shape-preview.json':'SimTools/PolarGospelRepairDirective3Validation.json',JSON.stringify(out,null,2)+'\n');
console.log(JSON.stringify({frames:frameResults.length,shapes:shapeResults.map(r=>({prefix:r.prefix,n:r.count,overlap:r.disagreementShare,pureOverlap:r.pureDisagreementShare,readabilityFailures:r.readabilityFailures.length,diversityFailures:r.diversityFloorFailures.length}))},null,2));
