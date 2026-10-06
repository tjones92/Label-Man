import fs from 'node:fs';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {readCsv,sum} from './analyze-genre-repertoire-repair-phase0.mjs';
import {analyzePolarResearch} from './analyze-polar-research.mjs';

const tag=process.argv[2]??'v1';assert(/^[a-zA-Z0-9-]+$/.test(tag));
const json=p=>JSON.parse(fs.readFileSync(p,'utf8').replace(/^\uFEFF/,''));
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const yes=x=>x==='True',fmt=x=>x==null?'absent':x.toFixed(2);
const before=[],after=[],inputs=[];
const runs=fs.readdirSync('SimLogs').filter(f=>new RegExp(`^easy-soundtrack-${tag}-matched-(1001|1002)(-resume\\d+)?-polar-research\\.json$`).test(f)).sort();
assert(runs.length,'No matched census manifests');
for(const file of runs) {
 const root='SimLogs/'+file.replace('-polar-research.json',''),manifest=json(root+'-polar-research.json');
 const complete=new Set(manifest.months.filter(m=>m.Status==='complete').map(m=>m.Date));
 for(const m of manifest.months.filter(m=>m.Status==='complete'))assert.equal(m.Observed,m.Planned);
 const invocation=json(root+'-invocation.json');assert(invocation.arguments.includes('--easy-soundtrack-census'));
 assert.equal(invocation.exitCode,0);assert(fs.readFileSync(root+'.log','utf8').includes('POLAR_CHECKPOINT_ROUNDTRIP_PASS'));
 inputs.push({run:manifest.run,invocation,months:manifest.months,checks:analyzePolarResearch(manifest.run).checks,
  files:['-invocation.json','-polar-research.json','.log','-genre-followup-slots.csv','-genre-repair-reference-slots.csv','-repertoire-slots.csv'].map(s=>({path:root+s,sha256:sha(root+s)}))});
 const select=rows=>rows.filter(r=>complete.has(r.date)&&yes(r.crossSection));
 before.push(...select(readCsv(root+'-genre-repair-reference-slots.csv')));
 const detailed=select(readCsv(root+'-genre-followup-slots.csv'));
 const standard=select(readCsv(root+'-repertoire-slots.csv'));
 const key=r=>[r.seed,r.date,r.artistId,r.slot,r.songId,r.category,+r.sampleWeight].join('|');
 assert.deepEqual(detailed.map(key).sort(),standard.map(key).sort(),'Detailed/standard slot parity');
 after.push(...detailed);
}
for(const rows of [before,after]) {
 assert.equal(new Set(rows.map(r=>[r.seed,r.date,r.artistId,r.slot].join('|'))).size,rows.length);
 for(const seed of [1001,1002])assert.equal(new Set(rows.filter(r=>+r.seed===seed).map(r=>r.date)).size,2,'Require January and February for each seed');
}
const observationKey=r=>[r.seed,r.date,r.artistId,r.slot,+r.sampleWeight].join('|');
assert.deepEqual(before.map(observationKey).sort(),after.map(observationKey).sort(),'Paired population, slot counts and weights');
const pct=(rows,all)=>sum(all)?100*sum(rows)/sum(all):null;
function measure(rows) {
 const families=[...new Set(rows.filter(r=>r.category==='existingCover').map(r=>r.seedFamily||`${r.originKind} / ${r.primaryGenre}`))].sort();
 return {observedSlots:rows.length,weightedSlots:sum(rows),
  screen:pct(rows.filter(r=>r.seedFamily==='Screen instrumental'),rows),stageFilm:pct(rows.filter(r=>r.seedFamily==='Stage and film songs'),rows),
  media:pct(rows.filter(r=>r.originKind.startsWith('ExternalMedia')),rows),
  inherited:pct(rows.filter(r=>['traditionalLineage','establishedStandard'].includes(r.category)),rows),pre1940:pct(rows.filter(r=>+r.originYear<1940),rows),
  existingCoverFamilies:Object.fromEntries(families.map(f=>[f,pct(rows.filter(r=>r.category==='existingCover'&&(r.seedFamily||`${r.originKind} / ${r.primaryGenre}`)===f),rows)]))};
}
const measures=[];
for(const genre of [...new Set(after.map(r=>r.genre))].sort())for(const unsigned of [true,false])for(const seed of [1001,1002,'pooled']) {
 const select=rows=>rows.filter(r=>r.genre===genre&&yes(r.unsigned)===unsigned&&(seed==='pooled'||+r.seed===seed));
 measures.push({genre,unsigned,seed,before:measure(select(before)),after:measure(select(after))});
}
const otherGenreMovements=measures.filter(m=>m.genre!=='EasyListening').map(m=>({genre:m.genre,unsigned:m.unsigned,seed:m.seed,
 mediaDelta:m.before.media==null?null:m.after.media-m.before.media,inheritedDelta:m.before.inherited==null?null:m.after.inherited-m.before.inherited}));
const otherGenresAccepted=otherGenreMovements.every(m=>[m.mediaDelta,m.inheritedDelta].every(v=>v==null||Math.abs(v)<=.5));
const oldTable=json('tmp/genre-repair-before/Data__PolarRepertoireTable.json'),table=json('Data/PolarRepertoireTable.json');
for(const key of ['numbers','liveSetMixes'])assert.deepEqual(table[key],oldTable[key]);
assert.equal(sha('Data/PolarRepertoireTable.json'),json('SimLogs/genre-repair-v3-matched-1001-invocation.json').repertoireTableSha256.toLowerCase(),'Entire repertoire calibration unchanged from v3');
const sources=['Systems/LiveRepertoire.cs','Systems/SongMaterialSelectionService.cs','SimTools/ChartAuditRunner.GenreFollowUp.cs','SimTools/ChartAuditRunner.PolarResearch.cs','SimTools/GenreRepertoireRepairChecks.cs','SimTools/run-polar-research.ps1','SimTools/run-easy-soundtrack-repair.ps1','SimTools/analyze-genre-repair-gospel.mjs','SimTools/analyze-easy-soundtrack-repair.mjs'].map(path=>({path,sha256:sha(path)}));
const checks=[];
for(const seed of [1001,1002])for(const check of ['genre-repertoire-repair-check','folk-easy-check','gospel-preference-check','polar-directive3-check']) {
 const suffix=seed===1002||check==='polar-directive3-check'?'-correct-flags':'';
 const path=`SimLogs/easy-soundtrack-${tag}-${check}-${seed}${suffix}.log`,log=fs.readFileSync(path,'utf8');
 const marker=check==='genre-repertoire-repair-check'?'GENRE_REPERTOIRE_REPAIR_CHECK_PASS':check==='folk-easy-check'?'FOLK_EASY_CHECK_PASS':check==='gospel-preference-check'?'GOSPEL_PREFERENCE_CHECK_PASS':'POLAR_DIRECTIVE3_CHECK_PASS';
 assert(log.includes(marker)&&!log.includes('SAVELOAD_ROUNDTRIP_ERROR'));checks.push({seed,check,path,sha256:sha(path),passed:true});
}
const gospel=json('SimTools/EasySoundtrackGospelRegression.json');
const easy=measures.filter(m=>m.genre==='EasyListening');
const vocalScreenSlots=after.filter(r=>r.genre==='EasyListening'&&!yes(r.instrumental)&&r.seedFamily==='Screen instrumental');
const inheritedAlerts=easy.filter(m=>Math.abs(m.after.inherited-m.before.inherited)>1||m.seed==='pooled'&&Math.abs(m.after.inherited-(m.unsigned?67.47:68.48))>1);
const output={definitions:{before:'v3 selector evaluated in the same world, disabling only the Easy soundtrack exemption',after:'unrestricted Easy soundtracks within the existing source-first allocation',shares:'Weighted percentage of all filled slots; existing-cover family shares also use all slots as denominator',window:'January and February 1963; development seeds 1001/1002; repeated observations are correlated',inheritedAlert:'Report per-seed/pooled movement above 1 pp versus paired before, or pooled movement above 1 pp from 67.47% unsigned / 68.48% signed; no tuning'},sources,inputs,checks,measures,otherGenreMovements,otherGenresAccepted,inheritedAlerts,vocalScreenSlots:{observed:vocalScreenSlots.length,weighted:sum(vocalScreenSlots)},gospel};
fs.writeFileSync('SimTools/EasySoundtrackRepairValidation.json',JSON.stringify(output,null,2)+'\n');
const lines=['# Easy Listening soundtrack repair','',`October 5, 2026. Run tag ${tag}; development seeds 1001/1002.`,
 '', 'Easy Listening screen themes and stage/film songs now compete with ordinary songs on fit and ranking within the existing selected source. Vocal Easy acts may take screen themes. No calibration, access tiers, source weights or original allocation changed.',
 '', '## Implementation', '',
 '- [Systems/LiveRepertoire.cs:17](C:/Project/Label-Man/Systems/LiveRepertoire.cs:17): Easy-only exemption predicate and non-persisted comparator switch; [vocal compatibility at line 40](C:/Project/Label-Man/Systems/LiveRepertoire.cs:40).',
 '- [Systems/SongMaterialSelectionService.cs:649](C:/Project/Label-Man/Systems/SongMaterialSelectionService.cs:649): Easy media joins the ordinary ranking within each source at lines 654–657; bypasses the media opportunity and family lottery. [Line 620](C:/Project/Label-Man/Systems/SongMaterialSelectionService.cs:620) also bypasses the generic media lottery for Easy without a source mix.',
 '- Census reference disables only the Easy exemption, restoring the artist repertoire state and using the same private census RNG. Other repair phases remain enabled.',
 '', '## Matched results', '', '| Population | Seed | Screen before → after | Stage/film before → after | External total before → after | Inherited before → after | Pre-1940 before → after |', '|---|---|---:|---:|---:|---:|---:|'];
for(const m of easy)lines.push(`| ${m.unsigned?'Unsigned':'Signed'} | ${m.seed} | ${['screen','stageFilm','media','inherited','pre1940'].map(k=>`${fmt(m.before[k])}% → ${fmt(m.after[k])}%`).join(' | ')} |`);
lines.push('', 'The pre-repair soundtrack share of 27.06% is a comparison point, not a cap. All tables use all filled weighted slots as their denominator.', '', '## Full existing-cover family tables');
for(const m of easy) {
 lines.push('',`### ${m.unsigned?'Unsigned':'Signed'}, seed ${m.seed}`,'','| Family | Before | After |','|---|---:|---:|');
 for(const f of [...new Set([...Object.keys(m.before.existingCoverFamilies),...Object.keys(m.after.existingCoverFamilies)])].sort())lines.push(`| ${f} | ${fmt(m.before.existingCoverFamilies[f]??0)}% | ${fmt(m.after.existingCoverFamilies[f]??0)}% |`);
}
lines.push('', '## Regression results', '', `Other genres: ${otherGenresAccepted?'PASS':'FAIL'} for the 0.5 pp external-media and inherited-share limits across per-seed and pooled signed/unsigned results.`,
 'Build: dotnet build --no-restore -v quiet passes with four existing warnings and no errors.',
 `Easy inherited alert: ${inheritedAlerts.length?JSON.stringify(inheritedAlerts.map(m=>({unsigned:m.unsigned,seed:m.seed,inherited:m.after.inherited}))):'No movement above 1 pp from the handoff references.'}`, '',
 'The repair check includes catalogue-count opportunity invariance for other genres, retained book counts, rights/save migration, Easy source preservation, ranked media competition, vocal eligibility and global-RNG preservation. All four named checks pass on both seeds.',
 `Actual resolver: ${vocalScreenSlots.length} observed vocal Easy screen-theme slots (${fmt(sum(vocalScreenSlots))} weighted slots) in the matched census. The whole repertoire calibration file has the same SHA-256 as the v3 invocation.`,
 'The first Directive 3 invocation incorrectly forced shape v2 on, conflicting with its legacy-save defaults check. The corrected invocation uses default shape flags and passes; the failed log remains retained.', '',
 '| Fresh Gospel seed | Inherited | Secular | Accepted |', '|---|---:|---:|---|');
for(const t of gospel.trajectories)lines.push(`| ${t.seed} | ${fmt(t.inheritedShare)}% | ${fmt(t.secularShare)}% | ${t.inheritedAccepted&&t.secularAccepted?'PASS':'FAIL'} |`);
lines.push('', '## Jazz observation', '', '| Population | Seed | Screen | Stage/film | External total | Inherited |','|---|---|---:|---:|---:|---:|');
for(const m of measures.filter(m=>m.genre==='Jazz'))lines.push(`| ${m.unsigned?'Unsigned':'Signed'} | ${m.seed} | ${['screen','stageFilm','media','inherited'].map(k=>fmt(m.after[k])+'%').join(' | ')} |`);
lines.push('', '## Source SHA-256 hashes', '', '| Source | SHA-256 |','|---|---|');
for(const s of sources)lines.push(`| ${s.path} | ${s.sha256} |`);
lines.push('', 'Complete per-genre deltas, invocation fingerprints, input hashes, source hashes and Gospel trajectory evidence are in EasySoundtrackRepairValidation.json. Partial census frames are excluded.');
fs.writeFileSync('SimTools/EasySoundtrackRepairReport.md',lines.join('\n')+'\n');
console.log(JSON.stringify({easy,otherGenresAccepted,maxOtherGenreDelta:Math.max(...otherGenreMovements.flatMap(m=>[Math.abs(m.mediaDelta??0),Math.abs(m.inheritedDelta??0)])),inheritedAlerts:inheritedAlerts.length,gospel:gospel.trajectories.map(t=>({seed:t.seed,inherited:t.inheritedShare,secular:t.secularShare}))},null,2));
