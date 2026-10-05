import fs from 'node:fs';
const data=JSON.parse(fs.readFileSync('SimTools/FolkEasyListeningLiveValidation.json','utf8'));
if(data.partial)throw new Error('Complete trajectories required for final report');
const fmt=n=>n==null?'—':n.toFixed(2)+'%';
const lines=[
'# Folk and Easy Listening live repertoire',
'',
'October 5, 2026. The targets below come from the current user request and subsequent normalization/cohort choices. GospelLiveRepairReport.md is background evidence, not a new instruction source. These are authored game calibration targets rather than measured historical rates.',
'',
'## Authored targets',
'',
'| Scene | Traditional / public domain | Standards | Contemporary covers | Own originals |',
'| --- | ---: | ---: | ---: | ---: |',
'| Folk, 1960–1961 | 60% | 25% | 10% | 5% |',
'| Folk, 1962–1963 | 40% | 15% | 25% | 20% |',
'| Easy Listening, normalized | 3.87% | 63.54% | 30.39% | 2.21% |',
'',
'Easy Listening uses the user-approved normalization of midpoint weights: traditional 3.5, standards 57.5, contemporary 27.5, originals 2, totaling 90.5. Normalization necessarily raises some shares above the initially stated ranges. The Folk policy applies to both Folk and ContemporaryFolk, while FolkRock and SingerSongwriter keep their existing policy. The 1962 mix remains the last authored Folk policy after 1963 pending a later-decade calibration.',
'',
'## Implementation',
'',
'Live selection chooses the source before choosing a composition. Traditional/public-domain material, period standards and contemporary covers have separate weighted candidate lists, each retaining the existing fit, preference and refusal rules. Source odds do not grow with catalogue size. Missing or exhausted sources fall back using the remaining source weights and a separate deterministic draw. Covers remain unique within a set.',
'',
'Own-original counts use deterministic stochastic rounding so a 5% average is possible across three-to-five-song sets. Skill still affects the quality of the live original. Own compositions with explicit author identity or matching writer-member credits are excluded from cover slots. Unpublished supplied professional songs do not inflate these genres’ artist-original category. The generic writing-propensity and all-original exception continue for genres outside these new policies.',
'',
'Traditional/public domain is the requested combined source category; it does not replace the existing provenance taxonomy, which still distinguishes lineage from rights status. Standards require established status as of the observation year. Folk standards are restricted to origins in 1920–1959; Easy Listening standards to origins before 1955. Songs outside those period bounds are other existing covers. A new 240-composition Folk standards book explicitly authors establishment in 1960, alongside the existing 500 traditional Folk compositions. A separate 100-composition traditional Easy Listening book adds accessible material. New books use keyed generation, preserving existing composition IDs and catalogue RNG streams.',
'',
'The user authorized an instrumental Easy Listening cohort after inspection found that all generated acts previously had vocalists. The authored formation split is 50% instrumental, a design choice rather than a claimed historical share. Generated performers retain their traits and member counts; their performance roles become instrumental, and the first member becomes a bandleader/writer. This role choice consumes no population RNG. Solo instrumental leaders remain solo acts, with the existing session/ensemble machinery supplying recording arrangements. A saved explicit role prevents the legacy solo-vocal inference from assigning them a singer. Actual vocal members still take precedence if an act later adds one.',
'',
'Only instrumental acts receive Easy Listening original slots. Their expected original share is 4.42%, balancing the authored half-instrumental formation cohort to a 2.21% genre target. Survival, signing and identity changes can alter the observed cohort mix; the measured whole-genre share is therefore reported separately. Older saves retain their existing performers and absent role fields default to the prior vocal inference; existing acts are not retroactively converted.',
'',
'Film/cast albums previously had empty track lists. Each released external-media album now authors one separate representative coverable theme composition, keyed to the album’s record ID. FilmScore themes are instrumental; StageCast and FilmSong themes are vocal. The composition saves its source type, source record ID, authorship context and external-publisher rights. Admission is idempotent and consumes no global RNG. It neither treats the whole album as one song nor changes the album license economics. Easy Listening cover access now also reaches Country and Folk alongside Pop and Jazz. No fixed film/Broadway quota is imposed inside the contemporary bucket.',
'',
'Gospel retains its 350-song book, sacred filter, inherited affinity and original-count policy. Song shapes, fit formulas and genre market centers were not retuned for these targets. The new Folk standards also enter normal neighboring-family access; recording material source weights were not changed.',
'',
'## Evolving-world results',
'',
'Two fresh development seeds ran 209 weeks, through the first chart week of January 1964. The table uses only January 1960–December 1963. First-week monthly panels cap at 12 acts per genre, signed/unsigned status and instrumental role. Reported shares use population/sample weights; “slots” counts actual observed panel slots. Folk scene totals combine Folk and ContemporaryFolk. These repeated repertoire observations are not actual booking counts or independent historical observations.',
'',
'| Seed | Scene / period | Observed slots | Traditional / PD | Standards | Contemporary | Own originals |',
'| --- | --- | ---: | ---: | ---: | ---: | ---: |'
];
for(const t of data.trajectories)for(const row of t.eras)lines.push(`| ${t.seed} | ${row.genre==='Folk'?'Folk scene':row.genre}, ${row.period} | ${row.observedSlots} | ${fmt(row.weightedShares.Traditional)} | ${fmt(row.weightedShares.Standard)} | ${fmt(row.weightedShares.Contemporary)} | ${fmt(row.weightedShares.Original)} |`);
lines.push('', 'Full annual, signed/unsigned and instrumental/vocal results, raw shares, target deviations, cover concentration, media theme counts and checkpoint checks are in FolkEasyListeningLiveValidation.json. No hard per-set quota or retrospective target band is used.', '',
'| Seed | Monthly frames | Short sets | Sets with an empty accessible source | Selected media themes |',
'| --- | ---: | ---: | ---: | ---: |');
for(const t of data.trajectories)lines.push(`| ${t.seed} | ${t.observations} | ${t.eras.reduce((n,r)=>n+r.shortSets,0)} | ${t.eras.reduce((n,r)=>n+r.coversWithNoSourceAccess,0)} | ${t.eras.reduce((n,r)=>n+r.mediaThemeSlots,0)} |`);
lines.push('', 'Accessible-source counts precede refusal/arrangement filtering and do not prove every source had an eligible choice in every set. Concentration is measured rather than given an invented acceptance ceiling. Small annual and role/status cells can depart from the authored genre mean.', '',
'## Scouting follow-up', '',
'The existing TheatresAndSupperClubs venue admits Easy Listening through the Pop family. That is an applicable room today, verified through the actual admission predicate. Casinos and luxury resorts have no dedicated scouting venue. Add or distinguish those rooms, their hours, market availability and booking/deal expectations during the later scouting mechanic pass. No new venue category or scouting economy was introduced here.', '',
'## Verification and reproduction', '',
'Build succeeds with zero errors and four pre-existing compiler warnings. Both development seeds pass the new source/cohort/media checks, GospelPreferenceChecks, repertoire-access and refusal-avoidance checks, and the existing Directive 3 shape, constraints, cover/scouting, player-perception, cache and world-save checks. Fully supplied fixtures measure each authored era over 2,000 acts; checks cover catalogue-size independence, order/repeat determinism, exhausted-source fallback, original/cover ownership, period cutoffs, role compatibility, theme admission and save/load. Future-year readings of a fresh initial world are diagnostic fixtures, separate from the evolving trajectories above.', '',
'The first sandboxed Godot launch crashed before initialization; installed-runtime runs completed. An initial Directive 3 invocation with explicit shape overrides could not test the old-save default and failed that assertion; both seeds passed its documented invocation without overrides. The superseded live-v1 trajectories have no instrumental cohort and remain retained preliminary evidence, excluded from the final aggregates. No hold-out seed was used.', '',
'Late checkpoint verification ran out of memory while converting a large serialized world to a single string. The observer now streams those same serialized bytes through SHA-256 and compares digests. A fixture checks equivalence against the previous canonical-string byte hash; both seeds’ repertoire checks pass on the recovered assembly, and every actual resume verifies world identity. The runs resume retained checkpoints rather than reseeding. FolkEasyListeningCheckpointRecovery.json records both assembly hashes and proves that all ten measured gameplay-source/table fingerprints stayed unchanged. Failed attempts are retained in SimLogs; only completed slices enter the results.', '',
'```powershell',
'dotnet build --no-restore -v quiet',
"& 'C:\\Users\\grohl\\Downloads\\Godot_v4.7-stable_mono_win64\\Godot_v4.7-stable_mono_win64\\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --folk-easy-check",
'# Repeat seed 1002; use --gospel-preference-check and --polar-directive3-check for regressions.',
'./SimTools/run-folk-easy-trajectories.ps1 -Seed 1001 -RunTag YOUR-FRESH-TAG',
'./SimTools/run-folk-easy-trajectories.ps1 -Seed 1002 -RunTag YOUR-FRESH-TAG',
'node SimTools/analyze-folk-easy-live.mjs --tag=YOUR-FRESH-TAG',
'node SimTools/write-folk-easy-report.mjs',
'```', '',
'The analyzer requires complete 209-week manifests by default. --partial writes a separate partial artifact. Add -Resume to the trajectory command to continue its saved manifest after interruption. Invocations record the actual assembly/table fingerprints: these final runs span the original assembly and the documented observer-only recovery assembly, with one unchanged repertoire table. Resumes verify checkpoint round trips. Existing final artifacts use the bandleaders-v2 run tag.'
);
fs.writeFileSync('SimTools/FolkEasyListeningLiveReport.md',lines.join('\n')+'\n');
console.log('Wrote SimTools/FolkEasyListeningLiveReport.md');
