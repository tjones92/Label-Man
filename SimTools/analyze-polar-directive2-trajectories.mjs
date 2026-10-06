import fs from 'node:fs';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import zlib from 'node:zlib';
import {csvRows} from './analyze-polar-research.mjs';
import {snapshotMetadata} from './read-polar-snapshot-metadata.mjs';
const hashes={},hash=b=>crypto.createHash('sha256').update(b).digest('hex').toUpperCase();
const json=p=>{const b=fs.readFileSync(p);hashes[p]=hash(b);return JSON.parse(b.toString().replace(/^\uFEFF/,''));};
const rows=p=>{hashes[p]=hash(fs.readFileSync(p));const r=csvRows(p);assert.equal(r.malformed,0,p);return r.rows;};
const inherited=c=>['traditionalLineage','establishedStandard'].includes(c),pct=(n,d)=>d?100*n/d:null;
const genreNames=['Gospel','Folk','EasyListening','Jazz','Blues','TraditionalPop'],rep=json('Data/PolarRepertoireTable.json');
const runFiles=fs.readdirSync('SimLogs').filter(p=>/^polar-directive2-trajectory-.*runs\.json$/.test(p));
const tags=process.argv.slice(2);assert(tags.every(t=>/^[a-zA-Z0-9-]+$/.test(t)));
const runs=[...new Map(runFiles.flatMap(p=>{const value=json('SimLogs/'+p);return Array.isArray(value)?value:[value];}).filter(r=>tags.length?tags.some(t=>r.run.startsWith('polar-directive2-fresh-'+t+'-')):r.run.includes('-monthly-')).map(r=>[r.run,r])).values()];
function repertoireHash(s){let h=14695981039346656037n;const u=x=>BigInt.asUintN(64,x);for(const c of s){h^=BigInt(c.charCodeAt(0));h=u(h*1099511628211n);}h^=h>>33n;h=u(h*0xff51afd7ed558ccdn);h^=h>>33n;h=u(h*0xc4ceb9fe1a85ec53n);return h^(h>>33n);}
function baselinePanel(rr){const cells=Map.groupBy(rr,r=>[r.date,r.genre,r.unsigned,r.cohort].join('|')),out=[];for(const cell of cells.values()){
 const acts=[...new Set(cell.map(r=>r.artistId))].sort((a,b)=>{const ha=repertoireHash(a+'|repair-panel'),hb=repertoireHash(b+'|repair-panel');return ha<hb?-1:ha>hb?1:0;});
 const ids=new Set(cell[0].genre==='Gospel'?acts:acts.slice(0,4));out.push(...cell.filter(r=>ids.has(r.artistId)));}return out;}
function aggregate(rr,shape,seed){const map=new Map();for(const r of rr){const quarter=r.quarter??Math.ceil(+r.month/3),period=`${r.year}-Q${quarter}`;if(+r.year>1961||(+r.year===1961&&quarter>3))continue;
 for(const scope of [r.unsigned==='True'?'unsigned':'signed','all',r.cohort]){const key=[r.genre,period,scope].join('|');if(!map.has(key))map.set(key,{seed,shape,genre:r.genre,period,scope,slots:0,inherited:0});const m=map.get(key);m.slots++;m.inherited+=inherited(r.category)?1:0;}}
 return [...map.values()].map(m=>({...m,share:pct(m.inherited,m.slots)}));}
const quarterly=[],taste=[],weekly=[],routes=[],completed=[];
for(const seed of [1001,1002]) {
 const baseline=rows(`SimLogs/polar-repertoire-final-${seed}-repertoire-slots.csv`).filter(r=>genreNames.includes(r.genre)&&(+r.year<1961||(+r.year===1961&&+r.month<=9)));
 quarterly.push(...aggregate(baselinePanel(baseline),'baseline',seed));
 // The interrupted repertoire treatment retains 21 complete monthly streams but only 52
 // flushed weekly rows. The completed 209-week baseline supplies the same 91 calendar weeks.
 const baseWeeks=rows(`SimLogs/polar-census209-on-${seed}-polar-final-weekly.csv`),baseByDate=new Map(baseWeeks.map(r=>[r.date,r]));
 for(const shape of ['off','on']) {
  const slices=runs.filter(r=>r.seed===seed&&r.shape===shape).sort((a,b)=>a.slice-b.slice);assert(slices.length);assert.equal(slices.at(-1).remaining,0);assert.deepEqual(slices.at(-1).date,[1961,9,29]);assert.equal(slices.reduce((n,s)=>n+s.weeks,0),91);
  const slots=[],tastes=[],weeks=[],source=new Map();
  for(const slice of slices){const root='SimLogs/'+slice.run,manifest=json(root+'-polar-research.json'),inv=json(root+'-invocation.json');assert.equal(manifest.censusMode,'none');assert(manifest.months.every(m=>m.Status==='disabled'));assert.equal(inv.maxSeconds,180);
   hashes[slice.snapshot]=hash(fs.readFileSync(slice.snapshot));const snap=await snapshotMetadata(slice.snapshot);assert.equal(snap.shape,shape==='on'?1:0);
   hashes[root+'.log']=hash(fs.readFileSync(root+'.log'));const log=fs.readFileSync(root+'.log','utf8');assert(/CHART_AUDIT_(STOPPED|COMPLETE)/.test(log));if(slice.slice>1)assert(log.includes('POLAR_CHECKPOINT_ROUNDTRIP_PASS'));
   for(const r of rows(root+'-repertoire-inventory.csv'))source.set(r.songId,r);
   slots.push(...rows(root+'-directive2-quarter-slots.csv'));tastes.push(...rows(root+'-directive2-taste.csv'));weeks.push(...rows(root+'-polar-final-weekly.csv'));
  }
  const uniqueSlots=[...new Map(slots.map(r=>[[r.year,r.quarter,r.date,r.artistId,r.slot].join('|'),r])).values()];
  // Duplicate resumed observations within one month are collapsed to the first date.
  const monthDate=new Map();for(const r of uniqueSlots){const month=+r.date.split('/')[0],key=r.year+'|'+month;if(!monthDate.has(key)||Date.parse(r.date)<Date.parse(monthDate.get(key)))monthDate.set(key,r.date);}
  const monthlySlots=uniqueSlots.filter(r=>monthDate.get(r.year+'|'+(+r.date.split('/')[0]))===r.date);assert.equal(monthDate.size,21,'All January 1960–September 1961 monthly observations required');
  quarterly.push(...aggregate(monthlySlots,shape,seed));
  for(const [period,tt] of Map.groupBy(tastes,t=>`${t.year}-Q${Math.ceil(+t.month/3)}`)){const last=tt.at(-1);taste.push({seed,shape,period,date:last.date,n:+last.n,raw:last.raw.split(';').map(Number),adjusted:last.adjusted.split(';').map(Number)});}
  const routeCounts={};for(const r of monthlySlots.filter(r=>r.genre==='Gospel'&&r.category!=='newlyAuthored')){const song=source.get(r.songId);assert(song);const family=song.seedFamily||song.originKind;const key=family==='ArtistOriginal'?family+':'+(r.admissionRoutes||'noRecordedRoute'):family;routeCounts[key]=(routeCounts[key]??0)+1;}
  routes.push({seed,shape,selectedCoverSlots:monthlySlots.filter(r=>r.genre==='Gospel'&&r.category!=='newlyAuthored').length,counts:routeCounts});
  const distinctWeeks=[...new Map(weeks.map(w=>[w.date,w])).values()];assert.equal(distinctWeeks.length,91);
  for(const r of distinctWeeks){const old=baseByDate.get(r.date);assert(old,'Same retained baseline week required');weekly.push({seed,shape,date:r.date,singles:+r.singles,baselineSingles:+old.singles,newSingleCount:+r.newSingleCount,baselineNewSingleCount:+old.newSingleCount,marketUnits:+r.marketUnits,baselineMarketUnits:+old.marketUnits,labelNet:+r.labelNet,baselineLabelNet:+old.labelNet,endingCash:+r.endingCash,baselineEndingCash:+old.endingCash});}
  completed.push({seed,shape,slices:slices.length,weeks:91,lastDate:slices.at(-1).date,monthlyObservations:monthDate.size});
 }
}
const rangeFailures=[];for(const m of quarterly){const band=rep.bands.find(b=>b.genre===m.genre&&b.cohort===m.scope);if(!band||m.shape==='baseline'||m.share>=band.min&&m.share<=band.max)continue;
 const b=quarterly.find(b=>b.shape==='baseline'&&b.seed===m.seed&&b.genre===m.genre&&b.scope===m.scope&&b.period===m.period);assert(b);rangeFailures.push({...m,band:[band.min,band.max],baseline:b,alreadyFailed:!(b.share>=band.min&&b.share<=band.max)});}
const q3=quarterly.filter(m=>m.genre==='Gospel'&&m.period==='1961-Q3'&&m.scope==='all'&&m.shape!=='baseline').map(m=>({...m,pass:m.share>=50&&m.share<=90}));
const gate={gospel:q3,bothSeedsOff:q3.filter(m=>m.shape==='off').every(m=>m.pass),bothSeedsOn:q3.filter(m=>m.shape==='on').every(m=>m.pass),newCrossGenreRangeFailures:rangeFailures.filter(m=>m.genre!=='Gospel'&&!m.alreadyFailed).length,holdOutReady:false};
const out={schemaVersion:1,scope:'Monthly complete Gospel cohorts; other five genres use at most four acts per genre/cohort/signing cell. Retained repertoire-final monthly baseline is reduced to the same sampling rule. Raw slot shares, no population weighting. Census none.',weeklyBaseline:'Completed polar-census209-on baseline: 91 matching calendar dates. This predates the repertoire repairs; weekly economic differences cannot isolate A+B alone. The interrupted repertoire-final treatment only flushed 52 weekly rows, so it cannot supply the full requested calendar window.',completed,quarterly,taste,routes,weekly,rangeFailures,gate,hashes};
fs.writeFileSync('SimLogs/polar-directive2-trajectories-analysis.json',JSON.stringify(out,null,2)+'\n');
console.log(JSON.stringify({completed,gate,rangeFailures:rangeFailures.length},null,2));
