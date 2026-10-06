// Sample-aware research only. Does not launch a simulation or consume the hold-out.
import fs from 'node:fs';
import assert from 'node:assert/strict';
import {pathToFileURL} from 'node:url';

export function csvRows(path) {
 const text=fs.readFileSync(path,'utf8').replace(/^\uFEFF/,'');
 const lines=text.trimEnd().split(/\r?\n/);let malformed=0;
 function fields(line) {const out=[];let s='',q=false;for(let i=0;i<line.length;i++){const c=line[i];if(c==='"'){if(q&&line[i+1]==='"'){s+='"';i++;}else q=!q;}else if(c===','&&!q){out.push(s);s='';}else s+=c;}out.push(s);return q?null:out;}
 const header=fields(lines.shift());assert(header,'Missing CSV header: '+path);
 const rows=[];for(const line of lines){if(!line)continue;const v=fields(line);if(!v||v.length!==header.length){malformed++;continue;}rows.push(Object.fromEntries(header.map((k,i)=>[k,v[i]])));}
 return {rows,malformed};
}
const yes=v=>v==='True';
const percent=(n,d)=>d?100*n/d:null;
const categories=['newlyAuthored','existingCover','establishedStandard','traditionalLineage','ownAuthored'];
export function analyzePolarResearch(prefix) {
 assert(/^[A-Za-z0-9-]+$/.test(prefix));
 const root='SimLogs/'+prefix;
 const manifest=JSON.parse(fs.readFileSync(root+'-polar-research.json','utf8'));
 assert.equal(manifest.schemaVersion,1);
 const complete=new Set(manifest.months.filter(m=>m.Status==='complete').map(m=>m.Date));
 const coverage=csvRows(root+'-polar-sample-acts.csv'), slots=csvRows(root+'-repertoire-slots.csv'), sets=csvRows(root+'-polar-diversity-sets.csv'), design=csvRows(root+'-polar-sample-design.csv');
 const completedCoverage=coverage.rows.filter(r=>complete.has(r.date)), completedSlots=slots.rows.filter(r=>complete.has(r.date)), completedSets=sets.rows.filter(r=>complete.has(r.date));
 const actorKey=r=>[r.date,r.artistId].join('|');
 const actors=new Map(completedCoverage.map(r=>[actorKey(r),r]));
 assert.equal(actors.size,completedCoverage.length,'Duplicate actor observations');
 const books=new Map(completedCoverage.map(r=>[actorKey(r),{row:r,slots:[],covers:new Set()}]));
 for(const r of completedSlots) {
  const b=books.get(actorKey(r));assert(b,'Slot without an observed set');assert(categories.includes(r.category));assert(+r.originYear<=+r.year);
  assert.equal(+r.sampleWeight,+b.row.weight);assert.equal(r.crossSection,b.row.crossSection);assert.equal(r.panel,b.row.panel);
  b.slots.push(r);if(r.category!=='newlyAuthored')b.covers.add(r.songId);
 }
 assert.equal(completedSets.length,completedCoverage.length);
 for(const r of completedSets){const b=books.get(actorKey(r));assert(b);assert.equal(b.slots.length,+r.filledCovers+(+r.originals));assert.equal(b.covers.size,+r.filledCovers);assert.equal(+b.row.filledSlots,b.slots.length);assert.equal(+b.row.requestedSlots,+r.requestedCovers+(+r.originals));}
 for(const m of manifest.months.filter(m=>m.Status==='complete'))assert.equal(completedCoverage.filter(r=>r.date===m.Date).length,m.Planned);
 for(const r of design.rows.filter(r=>complete.has(r.date))) {
  const observed=completedCoverage.filter(a=>a.date===r.date&&a.genre===r.genre&&a.cohort===r.cohort&&a.unsigned===r.unsigned&&a.writingBin===r.writingBin);
  assert.equal(observed.filter(a=>yes(a.crossSection)).length,+r.sample);
  assert.equal(observed.filter(a=>yes(a.panel)).length,+r.panelObserved);
  for(const a of observed){assert.equal(+a.population,+r.population);assert.equal(+a.sample,+r.sample);assert(Math.abs(+a.weight-(yes(a.crossSection)?+r.weight:0))<1e-9);}
  if(+r.sample){assert(+r.sample<=+r.population);assert(Math.abs(+r.weight-(+r.population)/(+r.sample))<1e-9);}
  else assert.equal(+r.population,0,'Unobserved nonempty stratum');
 }
 const metrics=new Map();
 function metric(r,period){const key=[period,r.unsigned,r.genre,r.cohort].join('|');if(!metrics.has(key))metrics.set(key,{period,population:yes(r.unsigned)?'unsigned':'signed',genre:r.genre,cohort:r.cohort,sampledActMonths:0,estimatedActMonths:0,observedSlots:0,weightedFilledSlots:0,weightedRequestedSlots:0,weightedShortSets:0,categories:Object.fromEntries(categories.map(c=>[c,0])),coverCounts:{},sizes:{}});return metrics.get(key);}
 for(const b of books.values()) {
  const r=b.row;if(!yes(r.crossSection)){assert.equal(+r.weight,0);continue;}
  assert(+r.weight>0);
  for(const period of [r.date,'all']){const m=metric(r,period),w=+r.weight;m.sampledActMonths++;m.estimatedActMonths+=w;m.observedSlots+=b.slots.length;m.weightedFilledSlots+=w*b.slots.length;m.weightedRequestedSlots+=w*(+r.requestedSlots);if(b.slots.length<3)m.weightedShortSets+=w;m.sizes[b.slots.length]=(m.sizes[b.slots.length]??0)+w;
   for(const s of b.slots){m.categories[s.category]+=w;if(s.category!=='newlyAuthored')m.coverCounts[s.songId]=(m.coverCounts[s.songId]??0)+w;}
  }
 }
 const summaries=[...metrics.values()].map(m=>{
  assert(Math.abs(Object.values(m.categories).reduce((a,b)=>a+b,0)-m.weightedFilledSlots)<1e-7);
  const numerator=m.categories.traditionalLineage+m.categories.establishedStandard;
  const covers=Object.values(m.coverCounts).sort((a,b)=>b-a);
  return {...m,coverCounts:undefined,numerator,denominator:m.weightedFilledSlots,share:percent(numerator,m.weightedFilledSlots),missingSlots:m.weightedRequestedSlots-m.weightedFilledSlots,shortSetShare:percent(m.weightedShortSets,m.estimatedActMonths),observedDistinctCovers:covers.length,sampleTopTenCoverShare:percent(covers.slice(0,10).reduce((a,b)=>a+b,0),covers.reduce((a,b)=>a+b,0))};
 });
 const previous=new Map(),churn=new Map();let panelPairs=0,changedGenre=0,changedPopulation=0,changedSetSize=0;
 const panelMonth=x=>{const [m,,y]=x.row.date.split('/');return +y*12+(+m);};
 for(const b of [...books.values()].filter(b=>yes(b.row.panel)).sort((a,b)=>panelMonth(a)-panelMonth(b))) {
  const r=b.row,old=previous.get(r.artistId);previous.set(r.artistId,b);if(!old)continue;
  if(panelMonth(b)-panelMonth(old)!==1)continue;
  panelPairs++;if(r.genre!==old.row.genre){changedGenre++;continue;}if(r.unsigned!==old.row.unsigned){changedPopulation++;continue;}if(b.slots.length!==old.slots.length)changedSetSize++;
  if(!b.covers.size||!old.covers.size)continue;
  const intersection=[...b.covers].filter(s=>old.covers.has(s)).length,union=new Set([...b.covers,...old.covers]).size;
  for(const scope of ['allSetSizes',...(b.slots.length===old.slots.length?['fixedSetSize']:[])]){const key=[r.unsigned,r.genre,scope].join('|');if(!churn.has(key))churn.set(key,{population:yes(r.unsigned)?'unsigned':'signed',genre:r.genre,scope,pairs:0,retention:0,jaccard:0});const m=churn.get(key);m.pairs++;m.retention+=intersection/old.covers.size;m.jaccard+=intersection/union;}
 }
 const output={schemaVersion:1,prefix,definitions:{...manifest.definitions,estimates:'Weighted ratios of cross-sectional slots; panel-only weight zero. Repeated acts are not independent replications. No confidence intervals from these short runs.',diversity:'Distinct covers and top-ten concentration describe observed sample support; unseen rare compositions are not estimated.'},censusMode:manifest.censusMode,completeMonths:[...complete],incompleteMonths:manifest.months.filter(m=>m.Status!=='complete'),sampleDesign:design.rows,coverage:{observedActMonths:completedCoverage.length,observedSlots:completedSlots.length,excludedPartialActMonths:coverage.rows.length-completedCoverage.length,excludedPartialSlots:slots.rows.length-completedSlots.length,malformedRows:coverage.malformed+slots.malformed+sets.malformed+design.malformed,absentStrata:design.rows.filter(r=>complete.has(r.date)&&+r.population===0)},summaries,churn:[...churn.values()].map(m=>({...m,retention:m.retention/m.pairs,jaccard:m.jaccard/m.pairs})),panelTransitions:{panelPairs,changedGenre,changedPopulation,changedSetSize},checks:{slotSetAgreement:true,partitions:true,noFutureMaterial:true,samplingWeights:true,completeMonthDenominators:true}};
 fs.writeFileSync(root+'-polar-research-analysis.json',JSON.stringify(output,null,2)+'\n');return output;
}
if(process.argv[1]&&import.meta.url===pathToFileURL(process.argv[1]).href){for(const prefix of process.argv.slice(2)){const r=analyzePolarResearch(prefix);console.log(JSON.stringify({prefix,completeMonths:r.completeMonths,coverage:{...r.coverage,absentStrata:r.coverage.absentStrata.length},checks:r.checks}));}}
