// Bounded current-world review. Only complete censuses enter reported estimates.
import fs from 'node:fs';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import {csvRows} from './analyze-polar-research.mjs';

// Keep the original bounded review reproducible without overwriting it in follow-up mode.
if(process.argv[2]==='--follow-up') { await import('./analyze-genre-repertoire-followup.mjs'); process.exit(0); }

const runs=process.argv.slice(2);
assert(runs.length,'Pass completed run prefixes');
const measures=[];
const fingerprints=[];
const categories=['traditionalLineage','establishedStandard','existingCover','newlyAuthored'];
for(const run of runs) {
 assert(/^[a-zA-Z0-9-]+$/.test(run));
 const root=`SimLogs/${run}`;
 const manifest=JSON.parse(fs.readFileSync(`${root}-polar-research.json`,'utf8'));
 const complete=new Set(manifest.months.filter(m=>m.Status==='complete').map(m=>m.Date));
 assert(complete.size,`No completed census: ${run}`);
 const path=`${root}-repertoire-slots.csv`;
 const {rows,malformed}=csvRows(path);assert.equal(malformed,0);
 const selected=rows.filter(r=>complete.has(r.date)&&r.crossSection==='True');
 const groups=new Map();
 for(const r of selected)for(const scope of ['genre','cohort']) {
  const key=[r.date,r.genre,r.unsigned,scope,scope==='cohort'?r.cohort:'all'].join('|');
  if(!groups.has(key))groups.set(key,[]);groups.get(key).push(r);
 }
 for(const [key,group] of groups) {
  const first=group[0],counts=Object.fromEntries(categories.map(c=>[c,0]));
  let total=0,legacy=0;
  for(const row of group) {
   assert(categories.includes(row.category));
   const weight=Number(row.sampleWeight);assert(weight>0);
   counts[row.category]+=weight;total+=weight;
   if(row.category==='traditionalLineage'||row.category==='establishedStandard'||row.publicDomain==='True')legacy+=weight;
  }
  assert(Math.abs(Object.values(counts).reduce((a,b)=>a+b,0)-total)<1e-6);
  measures.push({run,seed:manifest.seed,date:first.date,genre:first.genre,unsigned:first.unsigned==='True',
   cohort:group.every(r=>r.cohort===first.cohort)?first.cohort:'mixed',scope:key.split('|')[3],
   observedSlots:group.length,observedActs:new Set(group.map(r=>r.artistId)).size,weightedSlots:total,
   shares:Object.fromEntries(categories.map(c=>[c,100*counts[c]/total])),stdTradPct:100*(counts.traditionalLineage+counts.establishedStandard)/total,tradStandardOrPDPct:100*legacy/total});
 }
 fingerprints.push({run,completeDates:[...complete],input:path,sha256:crypto.createHash('sha256').update(fs.readFileSync(path)).digest('hex'),
  invocation:JSON.parse(fs.readFileSync(`${root}-invocation.json`,'utf8').replace(/^\uFEFF/,''))});
}
const output={definitions:{scope:'Startup and retained evolving-world snapshots; not a new 48-month trajectory or historical measured rates.',
 numerator:'Traditional lineage plus established standards as of observation year. Public-domain union also reported separately.',
 denominator:'All filled live slots, weighted by stratum population/sample. Signed and unsigned separate. Repeated acts are not independent observations.'},fingerprints,measures};
fs.writeFileSync('SimTools/GenreRepertoireReviewValidation.json',JSON.stringify(output,null,2)+'\n');
const late=measures.filter(m=>m.scope==='genre'&&m.unsigned&&m.date.endsWith('/1963'));
if(new Set(late.map(m=>m.seed)).size===2) {
 const cell=(genre,seed)=>late.find(m=>m.genre===genre&&m.seed===seed);
 const show=m=>m?`${m.stdTradPct.toFixed(2)}% (${m.observedSlots} slots)`:'absent';
 const references={BossaNova:'77.21%',ContemporaryFolk:'78.00%',Country:'76.60%',RnB:'45.14%',SurfRock:'2.31%'};
 const main=Object.entries(references).map(([g,old])=>`| ${g} | ${old} | ${show(cell(g,1001))} | ${show(cell(g,1002))} |`).join('\n');
 const broad=[...new Set(late.map(m=>m.genre))].sort().map(g=>`| ${g} | ${show(cell(g,1001))} | ${show(cell(g,1002))} |`).join('\n');
 const text=`# Genre repertoire review and cast/film album compositions

October 5, 2026. The prior work is committed and pushed as 2f492e5 on codex/polar-song-data-model. This follow-up adds album compositions and records a bounded genre review; it does not retune additional genre targets.

## Previously flagged genres

The quoted percentages reproduce the old 1960–63 unsigned legacy census in PolarRepertoireReport.md. They are historical diagnostic artifacts, not current measurements. The new columns use the corrected traditional-lineage plus established-as-of-year standard numerator, weighted by cross-sectional population/sample, over all filled unsigned live slots. Rights status is reported separately in GenreRepertoireReviewValidation.json. Different definitions and observation windows mean these are not exact before/after effect estimates.

| Genre | Old legacy reference | Seed 1001, September 6, 1963 | Seed 1002, October 4, 1963 |
| --- | ---: | ---: | ---: |
${main}

Bossa Nova, Country and RnB no longer reproduce their old excessive shares in these completed samples. Contemporary Folk was explicitly included in the Folk pass: 60% traditional + 25% standards in 1960–61, then 40% + 15% from 1962 onward. Its roughly 50–55% late sample is consistent with that existing policy. SurfRock is already rare; zero selections in a small sample does not establish zero exceptions in the population. No blanket elimination of instrumental adaptations was added.

Historical sources support the qualitative distinctions, not exact numeric quotas: the Library of Congress discusses new Jobim compositions in [bossa nova](https://blogs.loc.gov/now-see-hear/2021/08/from-the-recording-registry-the-girl-from-ipanema-1963/), while the Country Music Hall of Fame documents [Harlan Howard's professional songwriting](https://countrymusichalloffame.org/hall-of-fame/harlan-howard) and [Hank Cochran's staff-writing work](https://countrymusichalloffame.org/hall-of-fame/hank-cochran). Songs written for another performer are contemporary material, not automatically standards or artist originals. The Library of Congress also describes [Motown's songwriting](https://blogs.loc.gov/loc/2022/04/motowns-songwriting-stars-and-reach-out-ill-be-there/) and the traditional tune [Misirlou becoming surf repertoire](https://wwws.loc.gov/folklife/sampler/FLaudio.html).

## Other genres and remaining questions

| Unsigned genre | Seed 1001, September 1963 | Seed 1002, October 1963 |
| --- | ---: | ---: |
${broad}

The largest inherited shares remain in the intentionally tuned Gospel/Easy Listening scenes and in Classical/Children's material. This scan found no additional convincing genre-wide excess requiring an immediate numerical change. It covers observed early-1960s genres, not later-emerging genres or a decade.

Country has substantial seed variation (approximately 9% versus 27% unsigned); the low seed warrants a larger follow-up before selecting a target. Signed Jazz standards interpreters also fall below the old provisional band in these snapshots. Cohort/status estimates, including Bossa Nova's older-songbook versus composer-led split, are retained in the validation JSON. These are potential underrepresentation issues rather than the excess the user flagged. The table's older provisional bands are not acceptance targets for the subsequently revised Gospel/Folk/Easy Listening policies.

## Album composition addition and singles boundary

Released StageCast, FilmScore and FilmSong albums now get separate AlbumTrack and SongComposition identities. The existing representative theme keeps its ID and keyed shape and becomes the first actual album track. Other songs/cues receive their own stable IDs, external-publisher rights, source album/type and recording masters. FilmScore remains instrumental; cast and film songs remain vocal. Runtime/three minutes supplies 8–16 cuts (12 when runtime is unavailable), an authored procedural sizing rule rather than a historical statistic. Titles are procedural theme/cue/song labels.

Only album tracks are created. No Record singles, lead-single references or album promo projects are created. The promo extractor explicitly returns null for external-media soundtrack/cast albums. Added cuts are removed from non-Easy Listening recording pools before sampling so those catalogue positions and draw inputs stay unchanged. Easy Listening may record separate interpretations through its existing release pipeline. Existing representative themes keep their prior access. Live repertoire can access the new cuts through existing scene rules.

Album pooled appeal, scalar recording quality, licensing fees, production spend, market demand and the weekly origination draw are not recomputed. Authoring consumes keyed hashes rather than global randomness. Existing active external-media albums with empty track lists backfill on the next enabled simulation week using their original release date; populated albums are retained. Repeated admission is idempotent. The same ordinary composition/master/album save data stores the addition, with the new origin enum appended for compatibility.

## Verification and limits

The build passes with four existing warnings. Final FolkEasyListeningChecks pass on seeds 1001 and 1002. They cover all three media types, distinct compositions/masters, instrumental versus vocal context, rights/source metadata, album and composition serialization, repeated release, album economics, no chart-record creation, explicit promo-extraction refusal, unchanged non-Easy recording pool ordering and Easy Listening recording access. The pre-existing Folk/Easy source-selection and role checks also pass.

Completed censuses include startup on both seeds and one retained evolving-world snapshot per seed, with sampling weights, slot/set agreement, category partitions and future-material checks passing. Restored late snapshots pass world digest round trips before simulation resumes. Invocation/CSV hashes and per-status/per-cohort measures are in GenreRepertoireReviewValidation.json. The first 1964 attempt produced no eligible census because the harness ends in 1963; an initial 1963 sample hit its time limit. Neither attempt enters the estimates. Smaller completed samples replace them.

This is a bounded review, not a new 48-month trajectory or decade economic-neutrality test. Original cast/film singles are structurally excluded; no claim of byte-identical future singles charts is made, since intended Easy Listening interpretations can affect charts. The old retained Folk/Easy 48-month results remain documented in FolkEasyListeningLiveReport.md. No hold-out seed was used.

Reproduce the retained complete review with:

\`\`\`powershell
node SimTools/analyze-genre-repertoire-review.mjs ${runs.join(' ')}
dotnet build --no-restore -v quiet
# Run SaveLoadRoundTripRunner.tscn with --folk-easy-check for seeds 1001 and 1002.
\`\`\`
`;
 fs.writeFileSync('SimTools/GenreRepertoireReviewReport.md',text);
}
for(const m of measures.filter(m=>m.scope==='genre'&&m.unsigned))console.log(`${m.seed}\t${m.date}\t${m.genre}\t${m.stdTradPct.toFixed(2)}%\t${m.observedSlots}`);
