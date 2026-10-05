import fs from 'node:fs';
import crypto from 'node:crypto';
import readline from 'node:readline';
const args=process.argv.slice(2),partial=args.includes('--allow-partial');
const prefixes=args.filter(a=>a!=='--allow-partial');
if(!prefixes.length)throw Error('Supply completed run prefixes');
const table=JSON.parse(fs.readFileSync('Data/PolarRepertoireTable.json','utf8'));
const hashes={};
function hash(path){hashes[path]=crypto.createHash('sha256').update(fs.readFileSync(path)).digest('hex');}
function fields(line){const out=[];let s='',q=false;for(let i=0;i<line.length;i++){const c=line[i];if(c==='"'){if(q&&line[i+1]==='"'){s+='"';i++;}else q=!q;}else if(c===','&&!q){out.push(s);s='';}else s+=c;}out.push(s);return out;}
async function* csv(path){hash(path);let header;for await(const line of readline.createInterface({input:fs.createReadStream(path),crlfDelay:Infinity})){if(!line)continue;const v=fields(line);if(!header){header=v;continue;}if(v.length!==header.length)throw Error(`Malformed CSV ${path}: ${v.length}/${header.length}`);yield Object.fromEntries(header.map((h,i)=>[h,v[i]]));}}
function cohort(r){if(r.cohort)return r.cohort;const w=+r.writingAbility;switch(r.genre){case 'Jazz':return w>=table.numbers.composerLedCutoff?'composerLed':'standardsInterpreter';case 'SurfRock':case 'EasyListening':return 'roleUnverified';case 'BossaNova':return w>=table.numbers.composerLedCutoff?'contemporaryBrazilian':'olderSongbook';case 'ContemporaryFolk':return +r.year>=1962?'contemporaryInterpreter':'earlyRevival';case 'Folk':return 'traditionalRevival';case 'Gospel':return 'inheritedRepertoire';default:return 'mixedContemporary';}}
const categories=['newlyAuthored','existingCover','establishedStandard','traditionalLineage'];
const period=r=>[`${r.year}-${r.month.padStart(2,'0')}`,r.year,'1960-63'];
function fresh(){return {slots:0,categories:Object.fromEntries(categories.map(c=>[c,0])),publicDomain:0,suppliedNew:0,outsideWriterCovers:0,recentCover:0,contemporaryCover:0,archetypes:{},newMaterialArchetypes:{},arrangementArchetypes:{},songs:{},seedFamilies:{}};}
function finish(m){const numerator=m.categories.establishedStandard+m.categories.traditionalLineage,denominator=m.slots;const covers=m.categories.existingCover+numerator;
 const sorted=Object.entries(m.songs).sort((a,b)=>b[1]-a[1]);const arch=Object.entries(m.archetypes).sort((a,b)=>b[1]-a[1]);
 return {...m,songs:undefined,numerator,denominator,share:denominator?100*numerator/denominator:null,originalShare:denominator?100*m.categories.newlyAuthored/denominator:null,
  coverSlots:covers,uniqueCovers:sorted.length,top10CoverShare:covers?100*sorted.slice(0,10).reduce((a,[,n])=>a+n,0)/covers:null,topSongs:sorted.slice(0,10),dominantArchetype:arch[0]??null};}
const runs=[];
for(const prefix of prefixes){
 if(!/^[A-Za-z0-9-]+$/.test(prefix))throw Error('Invalid prefix');
 const root=`SimLogs/${prefix}`,invPath=root+'-invocation.json',inv=JSON.parse(fs.readFileSync(invPath,'utf8'));hash(invPath);
 const researchPath=root+'-polar-research.json';
 if(fs.existsSync(researchPath)&&JSON.parse(fs.readFileSync(researchPath,'utf8')).censusMode!=='full')throw Error('Use the weighted analyzer for sampled/disabled observation: node SimTools/analyze-polar-research.mjs '+prefix);
 const log=fs.readFileSync(root+'.log','utf8');hash(root+'.log');
 if(!log.includes(`CHART_AUDIT_COMPLETE run=${prefix} weeks=${inv.weeks}`))throw Error('Incomplete run '+prefix);
 if(!partial&&inv.weeks!==209)throw Error('Full report requires 209 weeks');
 const metrics=new Map(),sets=new Map(),months=new Set(),histories=new Map(),overlaps=new Map(),previous=new Map();
 let weekly=0;for await(const r of csv(root+'-polar-final-weekly.csv')){weekly++;if(+r.invalidFit!==0)throw Error('Invalid fit');}
 if(weekly!==inv.weeks)throw Error('Weekly count mismatch');
 for await(const r of csv(root+'-repertoire-slots.csv')) {
  if(!categories.includes(r.category))throw Error('Unpartitioned category '+r.category);
  if(+r.originYear>+r.year)throw Error('Future performed material');
  const population=r.unsigned==='True'?'unsigned':'signed',c=cohort(r),month=`${r.year}-${r.month.padStart(2,'0')}`;months.add(month);
  const histKey=[population,r.genre,r.artistId,month].join('|');if(!histories.has(histKey))histories.set(histKey,{population,genre:r.genre,artistId:r.artistId,month,songs:new Set()});if(r.category!=='newlyAuthored')histories.get(histKey).songs.add(r.songId);
  const scopes=['all',c];if(r.genre==='Gospel')scopes.push(+r.writingAbility>=table.numbers.composerLedCutoff?'strongWriterProxy':'otherWriterProxy');
  for(const scope of scopes)for(const p of period(r)) {
   const key=[population,r.genre,scope,p].join('|');if(!metrics.has(key))metrics.set(key,fresh());const m=metrics.get(key);m.slots++;m.categories[r.category]++;
   if(r.publicDomain==='True')m.publicDomain++;
   if(['ProfessionalOffice','LabelStaff'].includes(r.originKind)){if(r.category==='newlyAuthored')m.suppliedNew++;else m.outsideWriterCovers++;}
   if(r.category==='newlyAuthored')m.newMaterialArchetypes[r.archetype]=(m.newMaterialArchetypes[r.archetype]??0)+1;
   if(r.category!=='newlyAuthored'){m.songs[r.songId]=(m.songs[r.songId]??0)+1;m.archetypes[r.archetype]=(m.archetypes[r.archetype]??0)+1;if(r.arrangementArchetype)m.arrangementArchetypes[r.arrangementArchetype]=(m.arrangementArchetypes[r.arrangementArchetype]??0)+1;
    if(r.originKind==='RecentHit'||+r.year-+r.originYear<=5)m.recentCover++;
    if(r.category==='existingCover'&&+r.originYear>=1958)m.contemporaryCover++;
    if(r.seedFamily)m.seedFamilies[r.seedFamily]=(m.seedFamilies[r.seedFamily]??0)+1;
   }
  }
 }
 for await(const r of csv(root+'-polar-diversity-sets.csv')) {
  const population=r.unsigned==='True'?'unsigned':'signed';for(const p of period(r)) {
   const key=[population,r.genre,p].join('|');if(!sets.has(key))sets.set(key,{sets:0,eligible:0,empty:0,short:0,coverSlots:0,originals:0,idTie:0,plateauTie:0});const m=sets.get(key);m.sets++;m.eligible+=+r.eligibleSongs;m.coverSlots+=+r.filledCovers;m.originals+=+r.originals;m.idTie+=+r.idTieSelected;m.plateauTie+=+r.exactTieSelected;if(+r.eligibleSongs===0)m.empty++;if(r.shortSet==='True')m.short++;
  }
 }
 for(const h of [...histories.values()].sort((a,b)=>a.month.localeCompare(b.month))) {
  const key=[h.population,h.genre,h.artistId].join('|'),old=previous.get(key);previous.set(key,h);
  if(!old||!h.songs.size||!old.songs.size)continue;
  const serial=s=>+s.slice(0,4)*12+(+s.slice(5)-1);if(serial(h.month)-serial(old.month)!==1)continue;
  const union=new Set([...h.songs,...old.songs]).size,intersection=[...h.songs].filter(s=>old.songs.has(s)).length;
  const k=[h.population,h.genre].join('|');if(!overlaps.has(k))overlaps.set(k,{pairs:0,sumJaccard:0,sumRetained:0,minJaccard:1,maxJaccard:0});const o=overlaps.get(k);o.pairs++;o.sumJaccard+=intersection/union;o.sumRetained+=intersection/old.songs.size;o.minJaccard=Math.min(o.minJaccard,intersection/union);o.maxJaccard=Math.max(o.maxJaccard,intersection/union);
 }
 if(!partial&&months.size!==48)throw Error('Expected all 48 census months');
 const rows=[...metrics].map(([key,m])=>{const [population,genre,cohort,period]=key.split('|');const band=table.bands.find(b=>b.genre===genre&&b.cohort===cohort);const f=finish(m);if(Object.values(m.categories).reduce((a,b)=>a+b,0)!==m.slots)throw Error('Partition mismatch');return {population,genre,cohort,period,...f,band:band?[band.min,band.max]:null,distance:band&&f.share!==null?Math.max(band.min-f.share,f.share-band.max,0):null};});
 const ordering=[];for await(const r of csv(root+'-repertoire-ordering.csv'))ordering.push({genre:r.genre,unsigned:r.unsigned==='True',candidates:+r.candidates,requested:+r.requested,changed:+r.changedSlots,referenceCapability:+r.referenceCapability,resolvedCapability:+r.resolvedCapability});
 const admissionCounts={},admissions=[];for await(const r of csv(root+'-repertoire-admissions.csv')){admissionCounts[r.route]=(admissionCounts[r.route]??0)+1;admissions.push(r);}
 const generated={};if(fs.existsSync(root+'-repertoire-inventory.csv'))for await(const r of csv(root+'-repertoire-inventory.csv')){const key=[r.genre,r.seedFamily||('authored:'+r.originKind),r.originYear,r.archetype].join('|');generated[key]=(generated[key]??0)+1;}
 let assignments=0;const recording={};for await(const r of csv(root+'-polar-master-assignments.csv')){assignments++;const key=[r.actGenre,r.year,r.kind,r.source??r.category,r.provenanceCategory??'legacyCategoryOnly'].join('|');recording[key]=(recording[key]??0)+1;}
 const chartPath=root+'-first-chart-events.csv';let chartEvents=0;if(fs.existsSync(chartPath))for await(const r of csv(chartPath))chartEvents++;
 runs.push({prefix,seed:inv.seed,weeks:inv.weeks,months:[...months].sort(),rows,sets:[...sets].map(([key,m])=>({key,...m,meanEligible:m.eligible/m.sets,idTieShare:m.coverSlots?100*m.idTie/m.coverSlots:0,plateauTieShare:m.coverSlots?100*m.plateauTie/m.coverSlots:0})),churn:[...overlaps].map(([key,o])=>({key,pairs:o.pairs,meanJaccard:o.sumJaccard/o.pairs,meanRetained:o.sumRetained/o.pairs,minJaccard:o.minJaccard,maxJaccard:o.maxJaccard})),ordering,admissionCounts,admissions,generated,recording:{assignments,categories:recording,chartEvents,definition:'New AI recording assignments and first-chart events; distinct from live-set selections; no studio or chart tuning.'}});
}
for(const p of ['Data/PolarRepertoireTable.json','Data/PolarSongTable.json','SimLogs/polar-repertoire-phase0-neutrality.json'])hash(p);
const retained=JSON.parse(fs.readFileSync('SimLogs/polar-model-diagnosis.json','utf8'));hash('SimLogs/polar-model-diagnosis.json');
const references={BossaNova:77.21,ContemporaryFolk:78.00,Country:76.60,Folk:75.80,Gospel:77.88,Jazz:76.31,RnB:45.14,SurfRock:2.31};
const reproduction=Object.entries(references).map(([genre,expected])=>{const r=retained.shares.find(r=>r.seed===1001&&r.mode==='on'&&r.population==='unsigned'&&r.genre===genre);if(!r||Math.abs(r.songbookPct-expected)>.0051)throw Error('Reference reproduction failed '+genre);return {genre,expected,observed:r.songbookPct,numerator:r.songbookSlots,denominator:r.slots};});
const phases=[],phaseSets=[],phaseOrdering=[];for(const seed of [1001,1002]){const path=`SimLogs/polar-repertoire-phases-${seed}-slots.csv`;if(!fs.existsSync(path))continue;const counts={};for await(const r of csv(path)){const key=[seed,r.phase,r.unsigned==='True'?'unsigned':'signed',r.genre].join('|');counts[key]??={slots:0,categories:Object.fromEntries(categories.map(c=>[c,0])),archetypes:{},songs:{}};const m=counts[key];m.slots++;m.categories[r.category]++;if(r.category!=='newlyAuthored'){m.archetypes[r.archetype]=(m.archetypes[r.archetype]??0)+1;m.songs[r.songId]=(m.songs[r.songId]??0)+1;}}phases.push(...Object.entries(counts).map(([key,m])=>({key,...finish({...fresh(),...m})})));
 const setCounts={};for await(const r of csv(`SimLogs/polar-repertoire-phases-${seed}-sets.csv`)){const key=[seed,r.phase,r.unsigned==='True'?'unsigned':'signed',r.genre].join('|');const m=setCounts[key]??={sets:0,eligible:0,empty:0,covers:0,originals:0,slots:0};m.sets++;m.eligible+=+r.eligible;m.covers+=+r.filledCovers;m.originals+=+r.originals;m.slots+=+r.slots;if(+r.eligible===0)m.empty++;}phaseSets.push(...Object.entries(setCounts).map(([key,m])=>({key,...m,meanEligible:m.eligible/m.sets})));
 const orderCounts={};for await(const r of csv(`SimLogs/polar-repertoire-phases-${seed}-ordering.csv`)){const key=[seed,r.unsigned==='True'?'unsigned':'signed',r.genre].join('|');const m=orderCounts[key]??={actors:0,candidates:0,requested:0,changedSlots:0,changedActors:0};m.actors++;m.candidates+=+r.candidates;m.requested+=+r.requested;m.changedSlots+=+r.changedSlots;if(+r.changedSlots>0)m.changedActors++;}phaseOrdering.push(...Object.entries(orderCounts).map(([key,m])=>({key,...m,changedShare:m.requested?100*m.changedSlots/m.requested:null})));
}
const fitProbes=[];for(const seed of [1001,1002]){const path=`SimLogs/polar-repertoire-fit-probe-${seed}.csv`;if(!fs.existsSync(path))continue;const actors=new Map(),families={};for await(const r of csv(path)){if(!actors.has(r.artistId))actors.set(r.artistId,[]);actors.get(r.artistId).push(r);const m=families[r.family]??={actorBooks:0,candidates:0,withinTopWindow:0,capability:0,identity:0};m.actorBooks++;m.candidates+=+r.candidates;m.withinTopWindow+=+r.withinTopWindow;m.capability+=+r.meanCapability;m.identity+=+r.meanIdentity;}const gaps=[...actors.values()].map(rows=>{const gospel=rows.find(r=>r.family==='Gospel Standard');return gospel?{gap:Math.max(...rows.map(r=>+r.bestScore))-(+gospel.bestScore),outside:+gospel.withinTopWindow===0}:null;}).filter(Boolean);fitProbes.push({seed,actors:actors.size,knownPerformerActors:[...actors.values()].filter(rows=>rows[0].knownPerformers==='True').length,gospelOutsideTopWindow:gaps.filter(g=>g.outside).length,meanBestGospelScoreGap:gaps.reduce((n,g)=>n+g.gap,0)/gaps.length,maxBestGospelScoreGap:Math.max(...gaps.map(g=>g.gap)),families:Object.entries(families).map(([family,m])=>({family,actorBooks:m.actorBooks,candidates:m.candidates,withinTopWindow:m.withinTopWindow,meanBookCapability:m.capability/m.actorBooks,meanBookIdentity:m.identity/m.actorBooks}))});}
const output={scope:partial?'Partial diagnostic: not four-year acceptance':'January 1960–December 1963; unsigned and signed separate',definitions:{numerator:'traditional lineage plus compositions established as of observation year',denominator:'all filled live slots, including artist and supplied new material',publicDomain:'independent rights field',churn:'adjacent-month same-act cover-set Jaccard and retained fraction; variable set lengths included',idTie:'baseline: final ascending-ID tie decision; repaired runs: that rule is absent. Fit-plateau ties remain separately reported',arrangementAB:'baseline: one act per genre/population/month; treatment: one per genre/cohort/vocal role/population/month. Pre-switch phase probe covers all 3000 starting actors on identical pools.',phaseEffects:'incremental repairs on the same January 1960 actors; later trajectories are not fixed-cohort causal estimates',baselineRoles:'Surf and Easy Listening roles are marked unverified where only the earlier boolean is available; genre-level totals remain comparable.'},hashes,reproduction,bands:table.bands,runs,phases,phaseSets,phaseOrdering,fitProbes};
const out=partial?'SimLogs/polar-repertoire-partial.json':'SimLogs/polar-repertoire-analysis.json';fs.writeFileSync(out,JSON.stringify(output,null,2)+'\n');
console.log(JSON.stringify({output:out,runs:runs.map(r=>({prefix:r.prefix,weeks:r.weeks,months:r.months.length,bands:r.rows.filter(m=>m.period==='1960-63'&&m.band).map(m=>({population:m.population,genre:m.genre,cohort:m.cohort,share:m.share,band:m.band,distance:m.distance,numerator:m.numerator,denominator:m.denominator})),churn:r.churn}))},null,2));
