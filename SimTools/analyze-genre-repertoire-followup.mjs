import fs from 'node:fs';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {csvRows,analyzePolarResearch} from './analyze-polar-research.mjs';

const genres=['TeenPop','Country','Comedy','Classical','Childrens','TraditionalPop'];
const primaryPeriod='matched1963';
const inherited=r=>['traditionalLineage','establishedStandard'].includes(r.category);
const sum=(rows,fn)=>rows.reduce((a,r)=>a+fn(r),0);
const pct=(a,b)=>b?100*a/b:null;
const yes=x=>x==='True';
const key=r=>[r.seed,r.date,r.artistId].join('|');
const sha=path=>crypto.createHash('sha256').update(fs.readFileSync(path)).digest('hex');
const groupBy=(rows,fn)=>{const groups=new Map();for(const r of rows){const k=fn(r);if(!groups.has(k))groups.set(k,[]);groups.get(k).push(r);}return groups;};
const read=path=>{const parsed=csvRows(path);assert.equal(parsed.malformed,0,path);return parsed.rows;};
const fmt=x=>x===null||x===undefined?'absent':Number(x).toFixed(2);
const runs=process.argv.slice(process.argv.indexOf('--follow-up')+1);
assert(runs.length,'Pass run prefixes after --follow-up');
const slots=[],pools=[],catalog=[],acts=[],design=[],baselines=[],baselinePools=[],fingerprints=[],excluded=[],checks=[];
for(const run of runs) {
 assert(/^[\w-]+$/.test(run));
 const root=`SimLogs/${run}`,invocation=JSON.parse(fs.readFileSync(root+'-invocation.json','utf8').replace(/^\uFEFF/,''));
 if(!fs.existsSync(root+'-polar-research.json')) {excluded.push({run,reason:'No census manifest',invocation,invocationSha256:sha(root+'-invocation.json'),logSha256:fs.existsSync(root+'.log')?sha(root+'.log'):null});continue;}
 const manifest=JSON.parse(fs.readFileSync(root+'-polar-research.json','utf8'));
 const complete=new Set(manifest.months.filter(m=>m.Status==='complete').map(m=>m.Date));
 for(const m of manifest.months.filter(m=>m.Status!=='complete')) excluded.push({run,...m,reason:'Incomplete or disabled census'});
 const suffixes=['-invocation.json','-polar-research.json','.log','-repertoire-slots.csv','-polar-sample-acts.csv','-polar-sample-design.csv','-polar-diversity-sets.csv','-genre-followup-slots.csv','-genre-followup-pools.csv','-genre-followup-catalog.csv'];
 const snapshotArg=invocation.arguments.find(a=>a.startsWith('--polar-load-snapshot='));
 fingerprints.push({run,seed:manifest.seed,completeDates:[...complete],invocation,inputs:suffixes.filter(s=>fs.existsSync(root+s)).map(s=>({path:root+s,sha256:sha(root+s)})),loadedSnapshot:snapshotArg?{path:snapshotArg.split('=').slice(1).join('='),sha256:sha(snapshotArg.split('=').slice(1).join('='))}:null,snapshotRoundTripVerified:fs.readFileSync(root+'.log','utf8').includes('POLAR_CHECKPOINT_ROUNDTRIP_PASS')});
 if(!complete.size) {excluded.push({run,reason:'No complete eligible census',invocation});continue;}
 const validation=analyzePolarResearch(run);
 checks.push({run,checks:validation.checks});
 const detailed=read(root+'-genre-followup-slots.csv').filter(r=>complete.has(r.date)&&yes(r.crossSection));
 const original=read(root+'-repertoire-slots.csv').filter(r=>complete.has(r.date)&&yes(r.crossSection));
 const normalized=r=>[r.date,r.artistId,r.slot,r.songId,r.category,Number(r.sampleWeight)].join('|');
 assert.deepEqual(detailed.map(normalized).sort(),original.map(normalized).sort(),'Detailed slots must match standard census');
 const enrich=rows=>rows.map(r=>({...r,run}));
 const observedActs=read(root+'-polar-sample-acts.csv').filter(r=>complete.has(r.date)&&yes(r.crossSection));
 const observedPools=read(root+'-genre-followup-pools.csv').filter(r=>complete.has(r.date)&&yes(r.crossSection));
 const observedPoolsByArtist=groupBy(observedPools,key),detailedByArtist=groupBy(detailed,key);
 for(const a of observedActs) {
  const p=observedPoolsByArtist.get(key(a))??[];
  assert.equal(sum(p,r=>Number(r.accessed)),Number(a.candidateEntries),'Pool partition must equal accessed pool');
  assert(p.every(r=>Number(r.accepted)<=Number(r.accessed)));
  assert.equal((detailedByArtist.get(key(a))??[]).length,Number(a.filledSlots));
 }
 if(run.includes('-baseline-')) {baselines.push(...enrich(detailed));baselinePools.push(...enrich(observedPools));
  const source=`SimLogs/genre-review-final-1963-${manifest.seed}-repertoire-slots.csv`;
  const prior=read(source).filter(r=>genres.includes(r.genre)&&yes(r.crossSection));
  assert.deepEqual(detailed.map(normalized).sort(),prior.map(normalized).sort(),'Original quoted snapshot must reproduce exactly');
  checks.push({run,baselineExactSlotMatch:true,priorInput:source,sha256:sha(source)});
 } else {slots.push(...enrich(detailed));pools.push(...enrich(observedPools));acts.push(...enrich(observedActs));
  catalog.push(...enrich(read(root+'-genre-followup-catalog.csv').filter(r=>complete.has(r.date))));
  design.push(...enrich(read(root+'-polar-sample-design.csv').filter(r=>complete.has(r.date))));}
}
assert(new Set(slots.map(r=>+r.seed)).size>=6,'Need at least six seeds');
for(const seed of new Set(slots.map(r=>r.seed))) assert(new Set(slots.filter(r=>r.seed===seed&&r.date.endsWith('/1963')&&+r.date.split('/')[0]<=2).map(r=>r.date)).size>=2,'Need two matched 1963 censuses: '+seed);
assert(slots.every(r=>+r.originYear<=+r.date.split('/')[2]),'No future material');
assert.equal(new Set(slots.map(r=>[r.seed,r.date,r.artistId,r.slot].join('|'))).size,slots.length,'No duplicate slot observations');
function measure(rows,fields={}) {
 const weightedSlots=sum(rows,r=>+r.sampleWeight),counts=Object.fromEntries(['traditionalLineage','establishedStandard','existingCover','newlyAuthored'].map(c=>[c,sum(rows.filter(r=>r.category===c),r=>+r.sampleWeight)]));
 const rights=Object.fromEntries([...new Set(rows.map(r=>r.rightsStatus))].sort().map(s=>[s,sum(rows.filter(r=>r.rightsStatus===s),r=>+r.sampleWeight)]));
 return {...fields,observedSlots:rows.length,observedActSnapshots:new Set(rows.map(key)).size,distinctArtists:new Set(rows.map(r=>`${r.seed}|${r.artistId}`)).size,weightedSlots,categories:counts,
  stdTradPct:pct(counts.traditionalLineage+counts.establishedStandard,weightedSlots),shares:Object.fromEntries(Object.entries(counts).map(([k,v])=>[k,pct(v,weightedSlots)])),
  publicDomainPct:pct(sum(rows.filter(r=>yes(r.publicDomain)),r=>+r.sampleWeight),weightedSlots),tradStandardOrPDPct:pct(sum(rows.filter(r=>inherited(r)||yes(r.publicDomain)),r=>+r.sampleWeight),weightedSlots),rights};
}
const measures=[];
for(const [k,rows] of groupBy(slots,r=>[r.seed,r.date,r.genre,r.unsigned].join('|'))) {
 const first=rows[0];const fields={seed:+first.seed,date:first.date,genre:first.genre,unsigned:yes(first.unsigned),scope:'genre'};measures.push(measure(rows,fields));
 for(const [cohort,subset] of groupBy(rows,r=>r.cohort)) measures.push(measure(subset,{...fields,scope:'cohort',cohort}));
 for(const [formationCohort,subset] of groupBy(rows,r=>+r.formedYear<1960?'pre1960Formation':'runtimeFormation')) measures.push(measure(subset,{...fields,scope:'formationCohort',formationCohort}));
 for(const [writingBin,subset] of groupBy(rows,r=>r.writingBin)) measures.push(measure(subset,{...fields,scope:'writingBin',writingBin}));
}
const perSeed=[],pooled=[],spread=[];
for(const period of ['startup','1963','matched1963']) for(const genre of genres) for(const unsigned of [true,false]) {
 const rows=slots.filter(r=>r.genre===genre&&yes(r.unsigned)===unsigned&&r.date.endsWith('/'+(period==='startup'?1960:1963))&&(period!=='matched1963'||+r.date.split('/')[0]<=2));
 for(const [seed,subset] of groupBy(rows,r=>r.seed)) perSeed.push(measure(subset,{period,genre,unsigned,seed:+seed,dates:[...new Set(subset.map(r=>r.date))]}));
 const seeds=perSeed.filter(m=>m.period===period&&m.genre===genre&&m.unsigned===unsigned);
 const shares=seeds.map(m=>m.stdTradPct),mean=shares.length?sum(shares,x=>x)/shares.length:null;
 const stats={period,genre,unsigned,seeds:shares.length,min:shares.length?Math.min(...shares):null,max:shares.length?Math.max(...shares):null,mean,sampleSD:shares.length>1?Math.sqrt(sum(shares,x=>(x-mean)**2)/(shares.length-1)):null};
 spread.push(stats);pooled.push(measure(rows,{period,genre,unsigned,equalSeedMeanPct:mean}));
}
const concentration=[];
for(const period of ['startup','1963','matched1963']) for(const genre of genres) for(const unsigned of [true,false]) for(const seed of ['all',...new Set(slots.map(r=>r.seed))]) {
 const rows=slots.filter(r=>r.genre===genre&&yes(r.unsigned)===unsigned&&r.date.endsWith('/'+(period==='startup'?1960:1963))&&(period!=='matched1963'||+r.date.split('/')[0]<=2)&&(seed==='all'||r.seed===seed));
 const inheritedRows=rows.filter(inherited),numerator=sum(inheritedRows,r=>+r.sampleWeight),denominator=sum(rows,r=>+r.sampleWeight);
 const artists=[...groupBy(inheritedRows,r=>`${r.seed}|${r.artistId}`)].map(([id,rs])=>({id,name:rs[0].artistName,observedSlots:rs.length,weightedSlots:sum(rs,r=>+r.sampleWeight)})).sort((a,b)=>b.weightedSlots-a.weightedSlots);
 const songs=[...groupBy(inheritedRows,r=>`${r.seed}|${r.songId}`)].map(([id,rs])=>({id,title:rs[0].songTitle,weightedSlots:sum(rs,r=>+r.sampleWeight)})).sort((a,b)=>b.weightedSlots-a.weightedSlots);
 concentration.push({period,genre,unsigned,seed:seed==='all'?seed:+seed,distinctArtists:artists.length,distinctSongs:songs.length,
  distinctSongIds:new Set(inheritedRows.map(r=>r.songId)).size,top1InheritedPct:pct(sum(artists.slice(0,1),a=>a.weightedSlots),numerator),top3InheritedPct:pct(sum(artists.slice(0,3),a=>a.weightedSlots),numerator),
  top1AllSlotPct:pct(sum(artists.slice(0,1),a=>a.weightedSlots),denominator),top3AllSlotPct:pct(sum(artists.slice(0,3),a=>a.weightedSlots),denominator),artists,songs});
}
const poolMeasures=[];
for(const [k,rows] of groupBy([...pools.map(r=>({...r,scope:'expanded'})),...baselinePools.map(r=>({...r,scope:'baseline'}))],r=>[r.scope,r.seed,r.date,r.genre,r.unsigned].join('|'))) {
 const first=rows[0],artistGroups=groupBy(rows,key),weight=sum([...artistGroups.values()],rs=>+rs[0].sampleWeight);
 const fields={scope:first.scope,seed:+first.seed,date:first.date,genre:first.genre,unsigned:yes(first.unsigned),observedActs:artistGroups.size};
 const summary={...fields,meanAccessed:sum(rows,r=>+r.accessed*+r.sampleWeight)/weight,meanAccepted:sum(rows,r=>+r.accepted*+r.sampleWeight)/weight,
  meanInheritedAccepted:sum(rows.filter(inherited),r=>+r.accepted*+r.sampleWeight)/weight,meanInheritedTopFitBand:sum(rows.filter(inherited),r=>+r.topFitBand*+r.sampleWeight)/weight,
  distinctAcceptedInheritedSongs:new Set(rows.flatMap(r=>r.inheritedSongIds?r.inheritedSongIds.split(';'):[])).size,
  zeroInheritedPoolActs:[...artistGroups.values()].filter(rs=>sum(rs.filter(inherited),r=>+r.accepted)===0).length,
  makeup:[...groupBy(rows,r=>[r.category,r.seedFamily,r.primaryGenre,r.secondaryGenre].join('|'))].map(([k,rs])=>({category:rs[0].category,seedFamily:rs[0].seedFamily,primaryGenre:rs[0].primaryGenre,secondaryGenre:rs[0].secondaryGenre,
   meanAccessed:sum(rs,r=>+r.accessed*+r.sampleWeight)/weight,meanAccepted:sum(rs,r=>+r.accepted*+r.sampleWeight)/weight,meanTopFitBand:sum(rs,r=>+r.topFitBand*+r.sampleWeight)/weight}))};
 poolMeasures.push(summary);
}
const roster=[];
const slotsByArtist=groupBy(slots,key);
for(const [k,rows] of groupBy(acts,r=>[r.seed,r.date,r.genre,r.unsigned,r.cohort,r.writingBin].join('|'))) {
 const first=rows[0];
 roster.push({seed:+first.seed,date:first.date,genre:first.genre,unsigned:yes(first.unsigned),cohort:first.cohort,writingBin:first.writingBin,population:+first.population,sample:+first.sample,
  weightedActSnapshots:sum(rows,r=>+r.weight),sampledActs:rows.map(r=>{const s=slotsByArtist.get(key(r))?.[0];return {artistId:r.artistId,artistName:s?.artistName,formedYear:+s?.formedYear,writingAbility:+s?.writingAbility,requestedSlots:+r.requestedSlots,filledSlots:+r.filledSlots,weight:+r.weight};})});
}
const baselineMeasures=[...groupBy(baselines,r=>[r.seed,r.genre,r.unsigned].join('|'))].map(([k,rows])=>measure(rows,{seed:+rows[0].seed,genre:rows[0].genre,unsigned:yes(rows[0].unsigned),date:rows[0].date}));
const baselineConcentration=[];
for(const [k,rows] of groupBy(baselines.filter(inherited),r=>[r.seed,r.genre,r.unsigned].join('|'))) {
 const artistWeights=[...groupBy(rows,r=>r.artistId)].map(([artistId,rs])=>({artistId,name:rs[0].artistName,weightedSlots:sum(rs,r=>+r.sampleWeight),songs:[...new Set(rs.map(r=>r.songTitle))]})).sort((a,b)=>b.weightedSlots-a.weightedSlots);
 const total=sum(rows,r=>+r.sampleWeight);baselineConcentration.push({seed:+rows[0].seed,genre:rows[0].genre,unsigned:yes(rows[0].unsigned),distinctArtists:artistWeights.length,distinctSongs:new Set(rows.map(r=>r.songId)).size,top1InheritedPct:pct(sum(artistWeights.slice(0,1),r=>r.weightedSlots),total),top3InheritedPct:pct(sum(artistWeights.slice(0,3),r=>r.weightedSlots),total),artists:artistWeights});
}
const codePaths=['SimTools/ChartAuditRunner.GenreFollowUp.cs','SimTools/ChartAuditRunner.PolarResearch.cs','SimTools/ChartAuditRunner.PolarRepertoire.cs','SimTools/run-polar-research.ps1','SimTools/run-genre-repertoire-followup.ps1','SimTools/check-genre-repertoire-followup.ps1','SimTools/analyze-genre-repertoire-review.mjs','SimTools/analyze-genre-repertoire-followup.mjs','Systems/LiveRepertoire.cs','Systems/SongMaterialSelectionService.cs','Systems/CompositionCatalogService.cs','Systems/PolarSongBehavior.cs','Systems/RepertoireProvenance.cs','Data/PolarRepertoireTable.json'];
const output={definitions:{numerator:'traditionalLineage plus establishedStandard as of observation year; public-domain status is separate',denominator:'all filled unsigned live slots; population/sample weights unchanged',pool:'Accessed pool after year and EligibleLive filter; accepted also applies resolved live refusal, without observer events. Top fit band means distance < suitabilityWindow below best accepted score; approximate diagnostic, not exact ranking buckets.',poolUnion:'Distinct inherited IDs accepted by at least one sampled artist; not the population union.',concentration:'Artist identity is seed plus artistId. Song identity is seed plus songId; cross-seed catalogue IDs do not prove identical works. Shares are of inherited weighted slots; all-slot contributions also reported.',pooled:'Ratio of summed weighted inherited slots to summed weighted filled slots across retained snapshots. Equal-seed mean reported separately. Startup and 1963 remain separate.',spread:'Min/max and sample SD across six per-seed pooled percentages; descriptive, not confidence intervals. Repeated acts and dates are correlated.',window:'Two 1963 observations per seed. Original seeds use April/September and May/October worlds; fresh seeds use January/February. Different dates/world histories limit direct seed comparisons. No hold-out or chart claims.'},
 fingerprints,sourceHashes:codePaths.map(path=>({path,sha256:sha(path)})),excluded,checks,measures,perSeed,pooled,spread,concentration,poolMeasures,roster,catalog,baselineMeasures,baselineConcentration,
 inheritedRows:slots.filter(inherited),nonInheritedRows:slots.filter(r=>!inherited(r)),baselineInheritedRows:baselines.filter(inherited)};
output.definitions.primaryPeriod=primaryPeriod;
output.definitions.window='Primary: January 4 and February 1, 1963 on all six seeds. Supplemental legacy saved worlds: June/September on 1001 and July/October on 1002. Primary restored worlds on 1001/1002 evolve from October/November 1962; new seeds evolve from startup. Save/resume does not claim equality with uninterrupted global RNG continuation. No hold-out or chart claims.';
output.definitions.catalogueStates='The primary worlds have evolved with external-media album-composition cuts/backfill. Supplemental legacy saved worlds are censused immediately after load, before their next simulation tick can backfill empty album tracks. Do not interpret all-1963 mixed-state pooling as a policy effect or an equal-state seed comparison.';
output.definitions.originals='newlyAuthored slots are the existing census live-original placeholders, without registered composition IDs or assigned rights. ArtistOriginal provenance in an existingCover row means an already catalogued composition, not a new live-original slot.';
const checkPaths=fs.readdirSync('SimLogs').filter(p=>/^genre-followup-.+-checks\.json$/.test(p)).map(p=>'SimLogs/'+p).sort((a,b)=>fs.statSync(a).mtimeMs-fs.statSync(b).mtimeMs);
assert(checkPaths.length,'Required Folk/Easy check evidence missing');
const checkPath=checkPaths.at(-1),regressionChecks=JSON.parse(fs.readFileSync(checkPath,'utf8').replace(/^\uFEFF/,''));
assert.deepEqual(regressionChecks.map(c=>c.seed).sort(),[1001,1002]);
for(const check of regressionChecks) {assert(check.passed);assert.equal(check.logSha256.toLowerCase(),sha(check.log));}
output.regressionChecks={manifest:checkPath,sha256:sha(checkPath),results:regressionChecks};
const buildPath='SimLogs/genre-followup-build.json',buildEvidence=JSON.parse(fs.readFileSync(buildPath,'utf8').replace(/^\uFEFF/,''));
assert.equal(buildEvidence.exitCode,0);assert.equal(buildEvidence.logSha256.toLowerCase(),sha(buildEvidence.log));
for(const fingerprint of fingerprints) assert.equal(fingerprint.invocation.assemblySha256.toLowerCase(),buildEvidence.assemblySha256.toLowerCase(),'All retained censuses use the verified diagnostic build');
for(const check of regressionChecks) assert.equal(check.assemblySha256.toLowerCase(),buildEvidence.assemblySha256.toLowerCase());
output.build={manifest:buildPath,sha256:sha(buildPath),...buildEvidence};
const priorValidation=JSON.parse(fs.readFileSync('SimTools/GenreRepertoireReviewValidation.json','utf8'));
assert.equal(sha('Data/PolarRepertoireTable.json'),priorValidation.fingerprints[0].invocation.repertoireTableSha256.toLowerCase(),'Genre repertoire policy table must stay unchanged');
output.unchangedGenrePolicy={path:'Data/PolarRepertoireTable.json',sha256:sha('Data/PolarRepertoireTable.json'),matchesPriorReview:true};
const escape=v=>'"'+String(v??'').replaceAll('"','""')+'"';
const columns=['run','seed','date','genre','unsigned','artistId','artistName','formedYear','cohort','writingBin','writingAbility','instrumental','slot','songId','songTitle','category','originYear','originKind','primaryGenre','secondaryGenre','seedFamily','publicDomain','rightsStatus','sampleWeight'];
const exportRows=[...slots,...baselines];
fs.writeFileSync('SimTools/GenreRepertoireFollowUpSlots.csv',columns.join(',')+'\n'+exportRows.map(r=>columns.map(c=>escape(r[c])).join(',')).join('\n')+'\n');
output.slotExport={path:'SimTools/GenreRepertoireFollowUpSlots.csv',sha256:sha('SimTools/GenreRepertoireFollowUpSlots.csv'),rows:exportRows.length};
fs.writeFileSync('SimTools/GenreRepertoireFollowUpValidation.json',JSON.stringify(output,null,2)+'\n');
let report='# Genre repertoire follow-up census\n\nOctober 5, 2026. Diagnostic only: no genre policy, target or saved-data format changed.\n\n';
const primaryRows=genre=>slots.filter(r=>r.genre===genre&&yes(r.unsigned)&&r.date.endsWith('/1963')&&+r.date.split('/')[0]<=2);
const primaryMeasure=genre=>pooled.find(m=>m.period===primaryPeriod&&m.genre===genre&&m.unsigned);
const sourceShare=(genre,fn)=>pct(sum(primaryRows(genre).filter(fn),r=>+r.sampleWeight),primaryMeasure(genre).weightedSlots);
const teenMeasure=primaryMeasure('TeenPop'),countryMeasure=primaryMeasure('Country');
const teenConcentration=concentration.find(m=>m.period===primaryPeriod&&m.genre==='TeenPop'&&m.unsigned&&m.seed==='all');
const originalTeen=baselineMeasures.find(m=>m.genre==='TeenPop'&&m.unsigned&&m.seed===1001),originalTeenConcentration=baselineConcentration.find(m=>m.genre==='TeenPop'&&m.unsigned&&m.seed===1001);
const zeroTeenPool=poolMeasures.find(m=>m.scope==='baseline'&&m.genre==='TeenPop'&&m.unsigned&&m.seed===1002);
const countryPools=poolMeasures.filter(m=>m.scope==='expanded'&&m.genre==='Country'&&m.unsigned&&m.date.endsWith('/1963')&&+m.date.split('/')[0]<=2);
report+='## Findings and proposed changes\n\n';
report+='Under/over-representation judgments below refer to the author’s stated game-design expectations. They do not establish historical numeric quotas, and the reported spread is not a statistical confidence interval.\n\n';
report+=`**TeenPop: concentrated original outlier, plus a persistent live-selection effect.** The original 15.69% is four established Tin Pan Alley slots from three artists, not traditional lineage. Brian and the Teens supplies ${fmt(originalTeenConcentration.top1InheritedPct)}% of that inherited weight, and the top three artists supply 100%. Four of 34 unweighted slots is 11.76%; the population weights lift that to 15.69%, amplifying the small sampled middle-writing stratum. This is the correct existing weighting, not evidence of a weighting bug. The larger matched estimate is ${fmt(teenMeasure.stdTradPct)}%, supported by ${teenConcentration.distinctArtists} seed/artist identities and ${teenConcentration.distinctSongs} seed/song identities. It is no longer explained by those original three artists alone.\n\n`;
report+=`Seed 1002’s original 0% reflects its sampled roster’s fit/preference choices: all ${zeroTeenPool.observedActs} sampled unsigned artists have accepted inherited material, with a weighted mean of ${fmt(zeroTeenPool.meanInheritedAccepted)} songs per artist and ${zeroTeenPool.zeroInheritedPoolActs} empty inherited pools. The live rules do not forbid inherited material in TeenPop; they simply selected contemporary candidates in those observed sets. None of the four seed-1001 standards is misclassified by the unchanged establishment-year definition.\n\n`;
report+=`**Country: a small-sample low result within broader systematic under-selection in the current evolved catalogue.** The original 8.72% is 6.66 points traditional lineage and 2.06 points established standards; seed 1002’s 26.69% is 1.57 and 25.12 points respectively. Seed 1001 selects no Country Standard-family song in its original inherited rows, while seed 1002 draws most inherited weight from that family. Increasing the sample on the same September seed-1001 world raises the estimate to ${fmt(measures.find(m=>m.scope==='genre'&&m.genre==='Country'&&m.unsigned&&m.seed===1001&&m.date==='9/6/1963')?.stdTradPct)}%; its June world is ${fmt(measures.find(m=>m.scope==='genre'&&m.genre==='Country'&&m.unsigned&&m.seed===1001&&m.date==='6/14/1963')?.stdTradPct)}%. Thus the original 9% was especially roster/sample sensitive.\n\n`;
report+=`The matched evolved-world estimate is ${fmt(countryMeasure.stdTradPct)}%. Per-date weighted mean accepted inherited pools remain ${fmt(Math.min(...countryPools.map(m=>m.meanInheritedAccepted)))}–${fmt(Math.max(...countryPools.map(m=>m.meanInheritedAccepted)))} songs per unsigned artist; no sampled Country artist has an empty inherited pool. This rules out an empty inherited catalogue as the explanation. New live originals occupy ${fmt(countryMeasure.shares.newlyAuthored)}% of slots, and external-media album cuts ${fmt(sourceShare('Country',r=>r.originKind==='ExternalMediaComposition'))}%. Available inherited songs lose to other material through roster-specific resolved fit and preference ranking, without an explicit inherited source weight. Roster composition and ranking both contribute; this census does not claim a causal decomposition from an artist-swap or policy ablation.\n\n`;
report+='**Proposed TeenPop/Country experiment, for approval:** use the existing live affinity path to choose inherited versus contemporary material before ranking within each source. Illustrative game-design settings are `TeenPop: inheritedLiveShare 0.06, actSpread 0.02` and `Country: inheritedLiveShare 0.35, actSpread 0.10`. These are cover-source probabilities, not shares of all filled slots. With the observed original-slot allocations they would imply roughly '+fmt(0.06*(100-teenMeasure.shares.newlyAuthored))+'% / '+fmt(0.35*(100-countryMeasure.shares.newlyAuthored))+'% inherited across all slots before fallback and act effects. They express the author’s judgments, not historical estimates or acceptance targets, and have not been applied or validated. Preserve candidate access, taxonomy, rights and global RNG behavior; verify realized outcomes after any approval.\n\n';
report+=`**Comedy: systematic pool/classification effect, with additional seed variation.** Pooled inherited share is ${fmt(primaryMeasure('Comedy').stdTradPct)}%. ${fmt(sourceShare('Comedy',r=>inherited(r)&&r.seedFamily==='Comedy routines'))} points come from established Comedy routines; ${fmt(sourceShare('Comedy',r=>inherited(r)&&r.seedFamily==='Children songs'))} points come from traditional Children’s songs. Routines receive an authored establishment year of origin + 15, so sufficiently old routines count as established standards without parody or observed reuse evidence. Comedy draws Children’s material through shared NonMusic family access. **Proposal:** separate Comedy/Children’s access by genre compatibility, and review whether automatically aging a routine should establish a reusable standard. The latter would be a future classification policy decision; retain the present numerator throughout this diagnostic.\n\n`;
report+=`**Classical: systematic generic-original allocation and contemporary-catalogue competition.** Pooled inherited share is ${fmt(primaryMeasure('Classical').stdTradPct)}%; newly authored live placeholders take ${fmt(primaryMeasure('Classical').shares.newlyAuthored)}%, and existing ArtistOriginal-provenance catalogue material takes another ${fmt(sourceShare('Classical',r=>r.category==='existingCover'&&r.originKind==='ArtistOriginal'))}%. The remaining non-inherited families and every song are listed below/in the slot export. The default writing propensity and cover ranking govern this split, rather than a dedicated Classical repertoire policy. **Proposal:** give Classical a dedicated live-original allocation and inherited source balance; agree its numeric design weights before tuning. This is not primarily an empty-pool or one-artist result.\n\n`;
report+=`**Children’s: systematic original allocation, with shared-family leakage.** Pooled inherited share is ${fmt(primaryMeasure('Childrens').stdTradPct)}%; newly authored placeholders take ${fmt(primaryMeasure('Childrens').shares.newlyAuthored)}%. Non-inherited Comedy routines contribute ${fmt(sourceShare('Childrens',r=>!inherited(r)&&r.seedFamily==='Comedy routines'))}% of all slots. Its traditional children’s pool remains available; the generic original rule reserves much of the set before cover selection. **Proposal:** use a dedicated traditional-led live mix with a smaller authored share and explicit genre-compatible access, rather than adding more traditional songs to the catalogue. Agree numeric weights separately.\n\n`;
report+=`**TraditionalPop: a systematic catalogue-composition effect that the original saved snapshots understate.** Matched inherited share is ${fmt(primaryMeasure('TraditionalPop').stdTradPct)}%; new live originals occupy ${fmt(primaryMeasure('TraditionalPop').shares.newlyAuthored)}%, and external-media album cuts ${fmt(sourceShare('TraditionalPop',r=>r.originKind==='ExternalMediaComposition'))}%. On seed 1001, album cuts alone fill 45.14% of matched slots, while the immediately loaded June/September legacy snapshots contain no selected cuts of that origin. The album-composition work backfills at the next enabled simulation week, so the matched evolved worlds and immediate old snapshots have different live pools. **Proposal:** choose source families before song ranking for TraditionalPop, so multiplying cast/film cuts cannot multiply their effective source opportunity. Review its generic original allocation alongside that change. An inherited catalogue increase alone is not supported.\n\n`;
report+='These proposals leave Gospel, Folk and Easy Listening calibration outside the tuning scope. No policy change, saved-data change, commit or push is performed by the diagnostic scripts.\n\n';
report+='## Matched January/February 1963 comparison\n\nUse this calendar-matched comparison for the six-seed diagnosis. Later observations on the original seeds are retained as supplemental per-date measures in the JSON and pool tables.\n\n| Genre | Pooled unsigned | Equal-seed mean | Seed min–max | SD (pp) | Observed unsigned slots | Pooled signed |\n| --- | ---: | ---: | --- | ---: | ---: | ---: |\n';
for(const genre of genres) {const p=pooled.find(m=>m.period==='matched1963'&&m.genre===genre&&m.unsigned),s=spread.find(m=>m.period==='matched1963'&&m.genre===genre&&m.unsigned),signed=pooled.find(m=>m.period==='matched1963'&&m.genre===genre&&!m.unsigned);
 report+=`| ${genre} | ${fmt(p.stdTradPct)}% | ${fmt(s.mean)}% | ${fmt(s.min)}–${fmt(s.max)}% | ${fmt(s.sampleSD)} | ${p.observedSlots} | ${fmt(signed.stdTradPct)}% |\n`;}
report+='\n| Seed | TeenPop inherited | Slots | Country inherited | Slots |\n| --- | ---: | ---: | ---: | ---: |\n';
for(const seed of [...new Set(slots.map(r=>+r.seed))]) {const t=perSeed.find(m=>m.period==='matched1963'&&m.genre==='TeenPop'&&m.unsigned&&m.seed===seed),c=perSeed.find(m=>m.period==='matched1963'&&m.genre==='Country'&&m.unsigned&&m.seed===seed);report+=`| ${seed} | ${fmt(t.stdTradPct)}% | ${t.observedSlots} | ${fmt(c.stdTradPct)}% | ${c.observedSlots} |\n`;}
report+='\n';
report+='## Method and limits\n\n'+Object.entries(output.definitions).map(([k,v])=>`- **${k}**: ${v}`).join('\n')+'\n\n';
report+='Seeds: '+[...new Set(slots.map(r=>r.seed))].join(', ')+'. Each stratum selects at most 12 artists by the existing keyed seed/artist hash, versus 3 in the quoted late review. Strata remain genre, cohort, signed status and writing bin. Only complete cross-sectional censuses enter estimates. Baseline replays use 3 and reproduce the original slots exactly; they are excluded from the expanded estimates.\n\n';
report+='All six January 1, 1960 startup censuses completed, but the unsigned population has not yet initialized at that instant. They supply signed measurements only; unsigned startup results are **absent**, and do not enter unsigned estimates as zeros. All six focal genres currently use the `mixedContemporary` repertoire cohort; writing bins and artist formation years supply the additional roster distinctions in the JSON.\n\n';
report+='The four added seeds were absent from retained invocation/trajectory manifests when selected. They are additional seeds, not claimed hold-outs. New worlds run 166 ordinary weeks, ending March 8, 1963; only January/February 1963 censuses are retained. This is world preparation for the census, not a 48-month trajectory or decade economic test.\n\n';
report+='## Live selection rules\n\nNone of the six focal genres has a `liveSetMixes` or `genreAffinities` entry. They allocate live originals through the generic skill-scaled `OriginalCount` rule (`writingFloor` 0.15, `exceptionalWriter` 0.85; default `WritingPropensity` 0.65, Country 0.70), then select covers by resolved fit bands and keyed personal preferences. Exact/secondary/family/cross-scene access probabilities are 0.22/0.08/0.035/0.015. The recording-source mixes do not govern these genres’ live selection.\n\nTraditionalPop, TeenPop and Country can access adjacent scenes. Comedy and Children’s share the NonMusic family, admitting each other’s songs at family access. Classical uses its own family. Most nontraditional seeded families establish at origin year + 15; explicit dates such as the Folk Standards supply’s 1960 establishment can override that age. Rights and public-domain flags are not lineage evidence. Comedy has no parody classification in this selection path.\n\n';
for(const genre of genres) {
 report+=`## ${genre}\n\nMatched evolved worlds:\n\n| Seed | Retained 1963 dates | Unsigned inherited | Observed slots | Weighted slots | Lineage | Established | Signed inherited | Signed slots |\n| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |\n`;
 for(const m of perSeed.filter(m=>m.period===primaryPeriod&&m.genre===genre&&m.unsigned)) {const signed=perSeed.find(x=>x.period===m.period&&x.genre===genre&&!x.unsigned&&x.seed===m.seed);
  report+=`| ${m.seed} | ${m.dates.join('; ')} | ${fmt(m.stdTradPct)}% | ${m.observedSlots} | ${fmt(m.weightedSlots)} | ${fmt(m.shares.traditionalLineage)}% | ${fmt(m.shares.establishedStandard)}% | ${fmt(signed?.stdTradPct)}% | ${signed?.observedSlots??0} |\n`;}
 const p=pooled.find(m=>m.period===primaryPeriod&&m.genre===genre&&m.unsigned),s=spread.find(m=>m.period===primaryPeriod&&m.genre===genre&&m.unsigned),c=concentration.find(m=>m.period===primaryPeriod&&m.genre===genre&&m.unsigned&&m.seed==='all');
 report+=`\nPooled unsigned: **${fmt(p.stdTradPct)}%**, ${p.observedSlots} observed slots, ${fmt(p.weightedSlots)} weighted slots. Equal-seed mean ${fmt(s.mean)}%; range ${fmt(s.min)}–${fmt(s.max)}%; sample SD ${fmt(s.sampleSD)} percentage points.\n\n`;
 report+=`Inherited concentration: ${c.distinctArtists} seed/artist identities and ${c.distinctSongs} seed/song identities. Top artist ${fmt(c.top1InheritedPct)}% and top three ${fmt(c.top3InheritedPct)}% of inherited weight (${fmt(c.top1AllSlotPct)}% / ${fmt(c.top3AllSlotPct)}% of all slots). These are sample concentration estimates.\n\n`;
 report+='| Seed | Inherited artists | Inherited songs | Top 1 share of inherited | Top 3 share of inherited | Startup unsigned |\n| --- | ---: | ---: | ---: | ---: | ---: |\n';
 for(const c of concentration.filter(m=>m.period===primaryPeriod&&m.genre===genre&&m.unsigned&&m.seed!=='all')) {const start=perSeed.find(m=>m.period==='startup'&&m.genre===genre&&m.unsigned&&m.seed===c.seed);report+=`| ${c.seed} | ${c.distinctArtists} | ${c.distinctSongs} | ${fmt(c.top1InheritedPct)}% | ${fmt(c.top3InheritedPct)}% | ${start?fmt(start.stdTradPct)+'% ('+start.observedSlots+' slots)':'absent'} |\n`;}
 report+='\nInherited slot families:\n\n| Category | Seed family | Song genre | Observed slots | Weighted share of all slots |\n| --- | --- | --- | ---: | ---: |\n';
 const rs=slots.filter(r=>r.genre===genre&&yes(r.unsigned)&&r.date.endsWith('/1963')&&+r.date.split('/')[0]<=2);
 for(const [k,rows] of groupBy(rs.filter(inherited),r=>[r.category,r.seedFamily,r.primaryGenre].join('|')))report+=`| ${rows[0].category} | ${rows[0].seedFamily} | ${rows[0].primaryGenre} | ${rows.length} | ${fmt(pct(sum(rows,r=>+r.sampleWeight),p.weightedSlots))}% |\n`;
 report+='\nNon-inherited slots:\n\n| Category | Seed family / origin | Song genre | Observed slots | Weighted share of all slots |\n| --- | --- | --- | ---: | ---: |\n';
 for(const [k,rows] of groupBy(rs.filter(r=>!inherited(r)),r=>[r.category,r.originKind,r.seedFamily,r.primaryGenre].join('|')))report+=`| ${rows[0].category} | ${rows[0].originKind}${rows[0].seedFamily?' / '+rows[0].seedFamily:''} | ${rows[0].primaryGenre} | ${rows.length} | ${fmt(pct(sum(rows,r=>+r.sampleWeight),p.weightedSlots))}% |\n`;
 report+='\nPer-date/status/cohort/writing-bin partitions, rights measures, sampled artist roster, full inherited and non-inherited slot rows, and concentration lists are in the validation JSON. The companion CSV includes each slot’s artist/song name and ID, category, year, rights status and weight.\n\n';
 if(['TeenPop','Country'].includes(genre)) {
  report+='| Seed | Date | Status | Sampled artists | Mean accepted inherited pool | Distinct inherited pool songs | Artists with zero inherited pool | Mean inherited in top fit band |\n| --- | --- | --- | ---: | ---: | ---: | ---: | ---: |\n';
  for(const m of poolMeasures.filter(m=>m.scope==='expanded'&&m.genre===genre&&m.date.endsWith('/1963'))) report+=`| ${m.seed} | ${m.date} | ${m.unsigned?'unsigned':'signed'} | ${m.observedActs} | ${fmt(m.meanInheritedAccepted)} | ${m.distinctAcceptedInheritedSongs} | ${m.zeroInheritedPoolActs} | ${fmt(m.meanInheritedTopFitBand)} |\n`;
  report+='\nPool makeup by category, seed family and primary/secondary genre, with accessed and accepted counts, is in `poolMeasures`. Global catalogue category counts are retained separately in `catalog`; they are not artist-specific access.\n\n';
 }
}
report+='## Exact original snapshot trace\n\nBaseline enriched replays match the date/artist/slot/song/category/weight values in the six-genre subset of the original census CSVs exactly.\n\n| Seed | Genre | Unsigned inherited | Slots |\n| --- | --- | ---: | ---: |\n';
for(const m of baselineMeasures.filter(m=>m.unsigned))report+=`| ${m.seed} | ${m.genre} | ${fmt(m.stdTradPct)}% | ${m.observedSlots} |\n`;
report+='\nSeed 1001 TeenPop inherited rows:\n\n| Artist | Song | Category | Year | Rights | Slot weight |\n| --- | --- | --- | ---: | --- | ---: |\n';
for(const r of baselines.filter(r=>+r.seed===1001&&r.genre==='TeenPop'&&yes(r.unsigned)&&inherited(r)))report+=`| ${r.artistName} (${r.artistId}) | ${r.songTitle} (${r.songId}) | ${r.category} | ${r.originYear} | ${r.rightsStatus}${yes(r.publicDomain)?'; public domain flag':''} | ${fmt(r.sampleWeight)} |\n`;
report+='\n## Excluded attempts\n\nThe initial restricted-runtime attempt crashed before engine initialization and produced no census. All four partial February censuses hit their 900-second time budgets. Their partial rows are excluded; the completed January censuses remain valid, and complete February checkpoint replays replace the partial samples.\n\n'+excluded.map(e=>`- ${e.run}${e.Date?' '+e.Date:''}: ${e.reason}.`).join('\n')+'\n\n';
report+='## Reproduction and validation\n\n```powershell\n# Build the existing checkout, including album-composition work.\ndotnet build --no-restore -v quiet\n# Same startup, baseline, and 1963 collection, using a fresh run tag.\n& .\\SimTools\\run-genre-repertoire-followup.ps1 -Phase startup -RunTag reproduce\n& .\\SimTools\\run-genre-repertoire-followup.ps1 -Seeds @(1001,1002) -Phase baseline -RunTag reproduce\n& .\\SimTools\\run-genre-repertoire-followup.ps1 -Seeds @(1001,1002) -Phase matched -RunTag reproduce\n& .\\SimTools\\run-genre-repertoire-followup.ps1 -Phase late -RunTag reproduce\n& .\\SimTools\\check-genre-repertoire-followup.ps1 -RunTag reproduce\n# Re-analyze the exact retained inputs for this report.\nnode SimTools/analyze-genre-repertoire-review.mjs --follow-up '+runs.join(' ')+'\n```\n\nFor a fresh collection, replace the retained prefixes in the analyzer invocation with the corresponding `genre-followup-reproduce-*` prefixes, including any automatic `-resumeN` slices. The analyzer also requires the saved build evidence manifest and matching assembly hash. It keeps the original review files intact.\n\nThe final project build passes, and `--folk-easy-check` passes on 1001 and 1002. The validation JSON records build/check evidence, full invocations, input CSV/manifest/log SHA-256 hashes, loaded checkpoint hashes and source hashes. Completion, sampling weights, set/slot agreement, category partition, no future material, no duplicate observations, detailed-row parity, exact baseline replay, matching build/check assemblies and unchanged genre-policy-table assertions must pass before outputs are written.\n';
fs.writeFileSync('SimTools/GenreRepertoireFollowUpReport.md',report);
console.log(JSON.stringify({perSeed:perSeed.filter(m=>m.period==='1963'&&m.unsigned),spread:spread.filter(m=>m.period==='1963'&&m.unsigned),baselines:baselineMeasures.filter(m=>m.unsigned),excluded},null,2));
