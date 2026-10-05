import fs from 'node:fs';
import crypto from 'node:crypto';

const summary = JSON.parse(fs.readFileSync('SimLogs/polar-final-summary.json'));
const table = JSON.parse(fs.readFileSync('Data/PolarSongTable.json'));
const perception = JSON.parse(fs.readFileSync('Data/PolarPerceptionTable.json'));
const pct = v => v == null ? '—' : (100*v).toFixed(2)+'%';
const num = v => Number(v).toLocaleString('en-US',{maximumFractionDigits:0});
const money = v => '$'+(v/1e6).toFixed(3)+'m';
const rows = [];
for (const seed of [1001,1002]) for (const mode of ['off','on']) {
  const r = summary.results.find(r => r.seed === seed && r.mode === mode);
  const s = r.summary;
  rows.push(`| ${seed} ${mode} | ${num(s.observedNewSingles)} | ${num(s.directSingles)} | ${num(s.albums)} | ${s.meanNewSingleQuality.toFixed(6)} | ${num(s.marketUnits)} | ${money(s.labelNet)} | ${money(s.endingCash)} | ${s.dyingLabels} / ${s.bankruptLabels} |`);
}
const censusRows = [];
for (const c of summary.comparisons) for (const [genre,g] of Object.entries(c.censusComparisons)) {
  censusRows.push(`| ${c.seed} | ${genre} | ${g.off ? `${g.off.songbookSlots}/${g.off.slots}` : '—'} | ${pct(g.off?.songbookShare)} | ${g.on ? `${g.on.songbookSlots}/${g.on.slots}` : '—'} | ${pct(g.on?.songbookShare)} | ${g.delta == null ? '—' : (100*g.delta).toFixed(2)} |`);
}
const annualRows = [];
for (const c of summary.comparisons) for (const [year,r] of Object.entries(c.annual)) annualRows.push(
  `| ${c.seed} | ${year} | ${num(r.off.observedNewSingles)} → ${num(r.on.observedNewSingles)} | ${pct(r.singleChange)} | ${r.off.meanNewSingleQuality.toFixed(4)} → ${r.on.meanNewSingleQuality.toFixed(4)} | ${money(r.off.labelNet)} → ${money(r.on.labelNet)} | ${r.off.dyingLabels} → ${r.on.dyingLabels} |`);
const files = new Set();
for (const name of ['PolarSongDataModelHashes.json','PolarSongShadowHashes.json','PolarSongSelectionHashes.json','PolarRockCalibrationHashes.json','PolarRecordingRealizationHashes.json','PolarPlayerPlaytestHashes.json','PolarVisualsHashes.json']) {
  const manifest = JSON.parse(fs.readFileSync('SimTools/'+name));
  const fileMap = manifest.files ?? manifest;
  const listed = Array.isArray(fileMap) ? fileMap : Object.keys(fileMap).filter(p=>fs.existsSync(p)).map(path=>({path}));
  for (const file of listed) {
    const name = file.path.replaceAll('\\','/');
    if (!name.includes(':') && fs.existsSync(name)) files.add(name);
  }
}
for (const file of ['Systems/SimulationSeedBootstrap.cs','Systems/PolarSongBehavior.cs','Systems/PlayerDesk.cs','export_presets.cfg',
  'SimTools/ChartAuditRunner.cs','SimTools/ChartAuditRunner.PolarFinal.cs','SimTools/PolarSongBehaviorChecks.cs',
  'SimTools/run-polar-realization.ps1','SimTools/run-polar-final-audit.ps1','SimTools/analyze-polar-final.mjs','SimTools/write-polar-final-report.mjs',
  'Data/PolarSongTable.json','Data/PolarPerceptionTable.json']) files.add(file);
const hash = file => crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex').toUpperCase();
const hashes = [...files].sort().map(path => ({path,sha256:hash(path)}));
fs.writeFileSync('SimTools/PolarSongFinalHashes.json',JSON.stringify({date:'2026-10-03',scope:'Current fingerprints across all prior Polar slice manifests plus the default-on/final-audit slice; historical manifests retain their original hashes.',files:hashes},null,2)+'\n');
fs.writeFileSync('SimTools/PolarSongFinalValidation.json',JSON.stringify(summary,null,2)+'\n');
const fixtureLog = fs.readFileSync('SimLogs/polar-final-verified-polar-song-fit-check.log','utf8');
const fixtures = fixtureLog.split(/\r?\n/).filter(l => l.startsWith('POLAR_FIXTURE'));
const fixtureValue = (name,key) => fixtures.find(l=>l.startsWith('POLAR_FIXTURE '+name)).match(new RegExp(key+'=([^ ]+)'))[1];
function numberSource(key) {
  if (/^(capability|headroom|identityBase|rigidityPenalty|momentPenalty|neutralMoment)/.test(key)) return 'Spec A §5 fit';
  if (/^(wordDensity|productionDemand|instrumentationDemand|techniqueDemand|formDemand|plasticity)/.test(key)) return 'Spec A §4 derivation; Spec B/C taxonomy';
  if (/^(actor|ensemble|delivery|session|missingSession|backupVocal|traitIdentity|historyIdentity|neutralTrait|reach|missingDisposition|rigidity)/.test(key)) return 'Spec A §3 minimal act adapter; explicit provisional mapping';
  if (/^(candidate|resolver|nearTie|lyricFlip)/.test(key)) return 'Directive Phase 4 deterministic resolver; provisional mapping';
  if (/^reinterpretation/.test(key)) return 'Spec A §6 reinterpretation diagnostic';
  if (/^(refusal|newSigning|emptySongbook)/.test(key)) return 'Spec A §7 resistance';
  if (key === 'tasteDrift') return 'Directive C10 lagged market-centroid proposal';
  if (key === 'maxShadowRowsPerWeek') return 'Shadow instrumentation budget; implementation limit';
  if (/^selection/.test(key)) return 'Spec A §10 AI-only selection';
  if (/^(execution|recording|hookCeiling|promotion)/.test(key)) return 'Spec A §5 consumer separation; PolarRecordingRealizationReport calibration';
  if (/^(critic|momentConversion)/.test(key)) return 'Spec A §5 component consumers; behavior/calibration report';
  if (/^rock/.test(key)) return 'PolarRockCalibrationReport authorized rare-exception proposal';
  return 'Provisional implementation proposal; table provenance';
}
const checks = ['polar-song-data-check','polar-song-fit-check','polar-song-behavior-check','polar-player-perception-check','polar-rock-songbook-check','polar-recording-realization-check'];
const checkResults = checks.map(name => {
  const log = fs.readFileSync(`SimLogs/polar-final-verified-${name}.log`,'utf8');
  const marker = log.split(/\r?\n/).find(l => /POLAR_.*PASS/.test(l));
  if (!marker) throw Error('Missing fixture marker: '+name);
  return marker;
});
const main = `# Polar song system — consolidated release report

October 3, 2026. The user's current request authorizes default-on new games, the outstanding 1960–63 live-set census, decade validation, and this consolidated report. The downloaded draft and the three specs are design references, not additional execution/approval instructions. Older-save adoption is expressly outside this request. No tuning, holdout consumption, commit, push, or Windows executable export was performed in this closure pass.

## Shipped activation

Normal startup enables Polar before population generation. No opt-in argument is required. \`--disable-polar-fit-selection\` provides an explicit audit baseline. Existing saves retain their stored flag. The local Windows export preset explicitly includes both Polar JSON tables. That preset is Git-ignored; other checkouts must include Data/PolarSongTable.json and Data/PolarPerceptionTable.json in their own export preset. Debug compilation passes with the four existing obsolete-helper/unused-event warnings. This is a source/build activation, not a claim that a new exported executable has been delivered.

The current recording table is \`${table.version}\` (SHA-256 \`${hash('Data/PolarSongTable.json')}\`); perception is \`${perception.version}\` (SHA-256 \`${hash('Data/PolarPerceptionTable.json')}\`). Neither table was changed in this pass. The current user instruction supplies release-activation authority; existing provisional mapping/uncertainty provenance is retained and does not become a claim of historical accuracy or individually named numerical sign-off.

## Delivered phases and evidence

| Phase | Final implementation | Evidence |
|---|---|---|
| Recon | Existing symbols/rights/selection traced; baseline gap identified | PolarSongRecon.md and this measured census |
| Ownership | Composition/demo and immutable plasticity; durable master metadata; singles, album cuts, B-sides and reuse | PolarSongDataModelReport.md; data/save fixtures |
| Profiles/shadow | 34 archetypes, six demand/four identity axes, pure derivation, separate fit components, deterministic resolver | PolarSongShadowReport.md; 79-stream neutrality and F1–F4 |
| Selection/consumers | Act-specific bounded AI selection, rock songbook policy, reusable promo masters; Capability execution, Identity critics, lagged Moment conversion | PolarSongSelectionReport.md; PolarRockCalibrationReport.md; PolarRecordingRealizationReport.md |
| Perception/UI | Persisted observations; comparison radar/identity/bands; preview/playback; strong player resistance and explicit override | PolarVisualsReport.md; perception and refusal fixtures; two-seed UI/player workflows |
| Activation/closure | Default-on initialization, paired decade audit and complete monthly 1960–63 unsigned-population census | Current four completed runs; PolarSongFinalValidation.json |

Historical reports and hash manifests remain snapshots of earlier code/tables. Current source fingerprints are in \`PolarSongFinalHashes.json\`; no historical manifest was overwritten. Save/session coverage includes named references, paid booked sessions, take/kept indices, printed masters, both sides, pressing/release/promotion, and persistent perception observations. The latest UI report supersedes the earlier playtest report's missing-chart observations.

## Decade method and results

Four matched runs use development seeds 1001/1002, the real autoload/event chain, genre-market V2 and artist-population lifecycle. All four use the same assembly and table fingerprints. The enabled side exercises normal startup without \`--use-polar-fit-selection\`; controls pass the explicit disable flag. Calibration telemetry suppresses expensive diagnostics without changing simulation behavior. Each run completes 521 weekly observations (January 8, 1960 through December 26, 1969), then advances the real clock through December 31, 1969. The engine keys each completed settlement to its next-week checkpoint (ChartManager.FreezeCompletedWeekSettlement uses callback date + 7 days). The December 26 day-end during the final calendar tail therefore creates a January 2, 1970 keyed settlement; it is outside the 1960–69 comparison and is not added to these totals. Cash/status tables are the December 26 final audited checkpoint, not a separately captured December 31 stock. Completion requires process exit zero, the 521-week marker and the exact final calendar date.

Observed new singles include standalone and album-promoted singles first seen after initialization; prewarm records are excluded. Direct singles and pipeline album drops are separate counters. Mean quality is weighted by observed new-single count, not the mean of weekly means. Units use the completed-week ledger; label net is the sum of weekly label net flows, cash/status are last-checkpoint stocks. Four-component committed fit checks cover newly observed single masters; album-only master correctness is covered by the recording/player fixtures and preceding autonomous-album playtests, not claimed as a new exhaustive decade master audit.

| Seed / mode | Observed new singles | Direct singles | Album drops | Mean new-single quality | Market units | Summed label net | Last-checkpoint cash | Dying / bankrupt |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
${rows.join('\n')}

${summary.comparisons.map(c => `Seed ${c.seed}: observed single volume ${pct(c.observedSingleChange)}, album drops ${pct(c.albumChange)}, quality delta ${c.qualityDelta.toFixed(6)}, market units ${pct(c.marketUnitsChange)}, label net ${pct(c.labelNetChange)}, ending cash ${pct(c.endingCashChange)}. Prior one-year reference guardrails (±3% singles, at most .02 quality loss, at most 5% label-net loss): volume **${c.referenceGuardrails.singleVolume?'within':'outside'}**, quality **${c.referenceGuardrails.quality?'within':'outside'}**, net **${c.referenceGuardrails.labelNet?'within':'outside'}**.`).join('\n\n')}

Those engineering reference bands were recorded before the prior fresh-seed validation; they are not a new user-specified decade tolerance. Annual divergence and label distress remain visible below. Status counts describe labels retained in ChartManager.GetAllLabels at that checkpoint, not cumulative lifetime closures: zero bankrupt status entries does not mean no bankruptcy occurred (closures are present in the run logs). No seed was retuned or selected to obtain a pass. Zero invalid stored fit components were observed in all four runs. Numerical/structural audit completion is distinct from acceptance of every economic or historical distribution.

| Seed | Year | Observed singles off → on | Change | Quality off → on | Label net off → on | Dying off → on |
|---|---|---:|---:|---:|---:|---:|
${annualRows.join('\n')}

The retained genre-decade-shape streams also cover all ten years in all four runs. Release/unit/chart counts are finite and nonnegative; market/chart shares are bounded, and annual market shares sum to one within CSV rounding tolerance. Full genre-year release, unit/share and chart-breadth rows are retained in the validation JSON. This checks continuing genre/market observability, not an invented historical target for each emerging genre.

## 1960–63 live-set census

Every eligible unsigned act returned by \`ArtistManager.GetUnsignedArtists\` is sampled once per month, January 1960–December 1963: 48 observed months per run. This is the population available to scouting, before the player chooses a city/slate; it is not a signed-roster or all-acts census. Eligibility/population can evolve differently between the baseline and Polar, so later observations are repeated population snapshots rather than the same fixed cohort. First snapshots occur January 1 and subsequent snapshots on the first audited Friday in each month. Worlds continue beyond December 31, 1963, verifying the full requested interval.

Sampling calls the real \`PlayerDesk.BuildLiveSet\` with a private seeded RNG for set-length/read/legacy cover draws and placeholder original titles. The enabled suitability/rock selection is unchanged. No songs are registered, no player action is performed, and the global GD stream is untouched: the regression checks both flag states, and a two-week census/no-census pair produced byte-identical output across 76 other CSV streams. The disabled baseline initially stalled in its pre-existing duplicate-pool exhaustion loop; an audit-only distinct-candidate bound now reports the exhausted short set. Ordinary disabled gameplay is unchanged. Aborted initial artifacts are retained and excluded from final analysis.

The numerator is disjoint traditional/public-domain plus standards (traditional takes precedence when flags overlap). The denominator is all slots, including originals and recent-hit covers. Raw census CSVs retain both categories separately, set counts, originals, recent covers, short sets and duplicate cover counts. Short or empty sets are not dropped. All completed census runs contain zero duplicate cover slots. These are simulated material categories, not a claim of historically authored repertoire titles. Cover-only shares and per-year results are retained in the validation JSON.

| Seed | Act genre | Off songbook / all slots | Off share | On songbook / all slots | On share | Delta pp |
|---|---|---:|---:|---:|---:|---:|
${censusRows.join('\n')}

Rock-family aggregation: ${summary.comparisons.map(c=>`seed ${c.seed}: ${c.rockCensus.off.songbookSlots}/${c.rockCensus.off.slots} (${pct(c.rockCensus.off.songbookShare)}) off → ${c.rockCensus.on.songbookSlots}/${c.rockCensus.on.slots} (${pct(c.rockCensus.on.songbookShare)}) on`).join('; ')}. This measures rare exceptions under the previously authorized rock calibration. The strict draft objective of reducing standard/traditional live-set share cannot be claimed where the baseline is zero; the earlier reported high shares were recording assignments, not this population live-set measure. The final census is evidence of the actual distinction, not a retroactive assertion that the historical reduction gate passed.

Census coverage/shortages: ${summary.results.map(r=>{const values=Object.values(r.census); return `${r.seed} ${r.mode}: ${num(values.reduce((n,g)=>n+g.sets,0))} act-month sets, ${num(values.reduce((n,g)=>n+g.shortSets,0))} sets with fewer than three slots`;}).join('; ')}. Empty pools include genres with no cover repertoire; they remain in coverage totals. The raw column named recentCovers counts all remaining nonstandard/nontraditional covers, including ordinary catalogue covers, rather than proving every song was a recent hit.

## Fixtures and determinism

Current final-binary fixture output:

\`\`\`text
${fixtures.join('\n')}
${checkResults.join('\n')}
\`\`\`

C5 uses the normalized identity-weighted mean movement for Stretch (0–1). C6 subtracts the dominant demand once. C7 retains the written formula: the Beatles zero-deficit Capability is 1.0, not the sketch's .94. The other fixtures report actual implementation outputs rather than forcing sketch examples; no special historical artist/title whitelist is used. C8 supplies optional actual session support; absent context uses label support/inferred fallback. Tenderness changes recording archetype/pace/mood/genre while preserving composition and prior-master lineage. Purity, deterministic repeat proposals, craft-independent demands, inactive performers, and earlier-only market evidence pass.

| Worked-example quantity | Spec A §9 sketch | Current fixture | Discrepancy / interpretation |
|---|---:|---:|---|
| F1 Capability | .94 | ${fixtureValue('F1','capability')} | Zero deficit gives exp(0)=1, then clamp; C7 explicitly requires the formula |
| F1 Identity | .61 | ${fixtureValue('F1','identity')} | Current weighted distance after pull; sketch values are not numerical acceptance targets |
| F1 Stretch | .72 (sum) | ${fixtureValue('F1','stretch')} | C5 normalizes weighted movement to 0–1 |
| F1 refusal | Does not refuse | Does not refuse | New-signing fixture agrees; no listless-take penalty is invented |
| F2 Capability used in landed example | .93 | ${fixtureValue('F2','capability')} | Current realized taxonomy and written fit calculation |
| F2 Stretch | .44 (sum) | ${fixtureValue('F2','stretch')} | Normalized committed arrangement movement, not the sketch's axis sum |
| F2 reinterpretation outcome | +.16 | ${fixtureValue('F2','outcome')} | Positive diagnostic result; does not newly update definitive-version outcomes |
| F3 garage mismatch | No numeric target | Capability ${fixtureValue('F3','capability')}, Identity ${fixtureValue('F3','identity')} | VocalNuance worst deficit; ensemble/sophistication/maturity remain unsupported |
| F4 Tenderness | Recording-level transformation; no Spec A §9 target | Capability ${fixtureValue('F4','capability')}, Identity ${fixtureValue('F4','identity')}, Stretch ${fixtureValue('F4','stretch')} | DeepSoulPleader/dramatic-build/yearning; rights/reference unchanged |

No new gameplay RNG stream was added. The census RNG is isolated audit instrumentation. Prior repeat-start player workflows, full-world serialization/gzip round trips, ownership/reference/taste persistence and disabled 79-stream neutrality remain documented in the phase reports. Exact equivalence between uninterrupted and resumed whole-world RNG continuations is not established by serialization equality and is not claimed here.

## Final symbol map and ownership

| Spec name | Implemented location / role |
|---|---|
| Composition identity | Data/SongComposition.cs: songId; credits/rights/demo/defaults/plasticity |
| Recording identity | Data/PolarSongMetadata.cs: SongMasterMetadata; Systems/PolarSongMetadataService.cs: durable registry; Record.masterId / AlbumTrack.masterId |
| SongProfile / ActProfile | Data/PolarSongProfiles.cs; Systems/SongProfileDeriver.cs; Systems/PolarActProfileDeriver.cs |
| MaterialFit / Max6Have / Stretch | Systems/PolarMaterialFit.cs: Evaluate; dominant-axis headroom and weighted normalized movement |
| Cover re-resolution | Systems/PolarCoverResolver.cs: Propose; Systems/SongMaterialApplicationService.cs: commit |
| SampleBest / SampleBestAcross | Systems/SongMaterialSelectionService.cs: bounded act-specific sampling; legacy helpers retained only for explicit disabled controls |
| GetCraftScore | Data/SongComposition.cs: composition/compatibility/raw-read helper; enabled selection is act-specific fit |
| InterpretationFit / ExternalPenalty | Compatibility legacy helpers in SongMaterialSelectionService; not the enabled material-fit path |
| ReadProfile | Data/PolarPlayerReads.cs; Systems/PolarPlayerPerception.cs: persisted estimated bands; UI/PolarComparisonWidget.cs accepts only read DTOs |
| MarketTasteService | Systems/PolarSongBehavior.cs: completed-week per-genre centroid with drift; not an independent market engine |
| GetLeadVocalist | Systems/PolarActProfileDeriver.cs: active lead/solo/backing adapter, with marked inference |
| SongRecordingMemory / definitiveVersionScore | Data/SongComposition.cs; Systems/CompositionCatalogService.cs: existing completed-recording memory/outcomes |
| CoverFatigueShadow | Systems/SongMaterialSelectionService.cs: existing recent-hit pressure; new definitive-version breakthrough mechanics are deferred |
| CompositionSaveData | Systems/WorldSaveData.cs; Systems/CompositionCatalogService.cs: flag/taste/masters/compositions |
| Player observations / paid sessions | Systems/SaveGameService.cs; Systems/PlayerDesk.cs: observation and session capture/restore |

Composition IDs and rights/credits survive new performances. Each committed version owns its master identity, taxonomy and realized components; album/promo/compilation reuse keeps those facts. Plasticity freezes once on composition rather than changing with a transformative cover. Existing composition-based royalty/mechanical snapshots remain authoritative. This pass did not redesign writer-payment routing.

## Configuration, scope and remaining deviations

The complete 34-row archetype table, family/genre resolver priors, numerical settings and perception settings as shipped are reproduced in \`PolarSongTables.md\`. The original table's provenance identifies six spec example rows and provisional proposals. Numeric settings are grouped by their relevant spec/implementation area; the current activation request accepts the current build operationally, but no fabricated Alice signature is recorded for every numeric literal.

Explicit remaining boundaries against the original unsigned draft:

- Strong refusal is wired on the player booking path with insist/own material/shelve options. AI refusal avoidance and new morale/conviction/lineup penalties remain unimplemented draft items.
- Existing recording-success/fatigue memory remains. The proposed hit-success reinterpretation/definitive-version breakthrough behavior is not newly wired; ReinterpretationOutcome remains a diagnostic helper.
- Historical master taxonomy and several performer/style axes are inferred. Automatic hearing/reference history is conservative and year-granular; the market estimate remains a lagged chart-derived centroid.
- Meter/form defaults and overrides are represented/preserved, but the general resolver does not invent a new meter/form rewrite policy. Tag lifecycles/economy, Rumoured events and per-arranger writing credits remain deferred.
- The unresolved missing-original-controller mechanical-routing fallback and lack of a general per-writer payment ledger remain reported scope deviations; preservation of credits is not proof of every writer's payment equivalence.
- Legacy helpers remain for explicit audit controls, despite the draft's literal deletion wording. New-game enabled selection does not use the global craft rank.
- Comparison prose is currently deterministic code in PolarPlayerPerception, with uncertainty/standing values in the perception JSON; the draft's separate editorial template-library data file has not been delivered. Compatibility hook/quality floats also remain; their later removal is not part of this activation pass.
- No untouched three-seed/holdout suite was consumed. Prior fresh validation seeds 1011/1012 remain historical evidence; the requested census/decade closure uses only documented development seeds 1001/1002.
- The new audit completes the previously missing windows. It does not establish historical truth, universal label improvement, exact resumed RNG continuity or completion of every optional/unimplemented draft behavior.

## Reproduction and artifacts

Build Debug first, then run each mode for each development seed with a fresh run name:

\`\`\`powershell
dotnet build 'Label Man.csproj' --no-restore
./SimTools/run-polar-final-audit.ps1 -Seed 1001 -Mode off -Run polar-review-off-1001
./SimTools/run-polar-final-audit.ps1 -Seed 1001 -Mode on -Run polar-review-on-1001
# Repeat with seed 1002; preserve the same build/table.
node SimTools/analyze-polar-final.mjs SimLogs polar-review
node SimTools/write-polar-final-report.mjs
\`\`\`

The launcher refuses an existing artifact family and requires exit/completion/date markers. Final logs/CSVs/invocations: \`SimLogs/polar-final-verified-{off,on}-{1001,1002}-*\`. Analysis: \`SimLogs/polar-final-summary.json\`, copied to \`SimTools/PolarSongFinalValidation.json\`. Full phase details reside in the referenced reports. \`PolarSongFinalHashes.json\` records current SHA-256 values across prior slice manifests and every source/config/tool modified by this closure; report/validation/table artifacts are hashed separately without a self-reference.
`;
const appendix = `# Polar shipped tables and constants

October 3, 2026. Exact values from the current JSON files; no retuning in the default-on/final-audit pass. The configuration provenance remains provisional. Unit/algorithm values (six demand axes, four identity axes, FNV hash constants) are schema/algorithm definitions, not gameplay calibration. The historical numbered sign-offs in the unsigned draft are not independently verified signatures; release activation follows the current user's request.

## Numbers (${table.version})

General sources: Spec A §§3–7; the current derivation, fit, resolver and behavior implementations; rock/recording calibration reports. Capability/headroom/identity/Moment numbers implement fit; actor/ensemble/session/history/reach/rigidity numbers implement the minimal adapter; candidate/resolver/lyric/reinterpretation numbers govern arrangements; refusal/standing/empty-songbook numbers govern resistance; selection numbers govern AI-only ranking; execution/hook/quality/promotion/critic/conversion numbers govern calibrated consumers; rock factors govern rare songbook exceptions. maxShadowRowsPerWeek is a telemetry budget. Exact original section-level author comments are available in the spec and implementation; provisional keys have no fabricated individual signed gate.

| Key | Value | Source / rationale |
|---|---:|---|
${Object.entries(table.numbers).map(([k,v])=>`| ${k} | ${v} | ${numberSource(k)} |`).join('\n')}

Identity distance/Stretch weights, in Toughness/Sophistication/Sincerity/Maturity order: ${table.identityWeights.join(', ')}.

## Archetypes

Axis order: ${table.axisOrder.join(', ')}. Data rows below are the shipped resolver mappings (archetype, era/family eligibility, pace/mood and axis base), not a separate hidden switch table.

| Archetype | Ten axes in declared order | Plasticity | From year | Families | Pace | Mood |
|---|---|---:|---:|---|---|---|
${table.archetypes.map(r=>`| ${r.name} | ${(r.axes??r.base??[]).join(', ')} | ${r.plasticity} | ${r.fromYear} | ${r.families.join(', ')} | ${r.pace} | ${r.mood} |`).join('\n')}

## Family/genre priors

Four identity axes in the same identity order. The resolver permits era/family rows plus the reference shape; plasticity × reach bounds movement; session support does not invent vocalist skill. Zero reach/plasticity or an empty eligible candidate pool preserves the reference reading.

Shipped resolver procedure: copy the reference/demo taxonomy; replace archetype, pace and mood with the selected row; set primary genre to the project genre and retain the prior primary as secondary when it changes. Known lead vocal approach can replace delivery. RomanticAddress flips to DemandBoast (plus CallAndResponse for RhythmAndSoul) when **projected** Stretch and reach meet lyricFlipStretch/lyricFlipReach; the final discrete arrangement's committed Stretch may be smaller. Meter/form, vocal presence and content/tag data are inherited. Candidate demand movement is bounded by reach × plasticity × candidateDemandReach, then rows are ranked by mean squared demand mismatch, weighted identity mismatch and execution shortfall; near ties use the planned master ID's stable FNV hash. This is the actual general procedure in PolarCoverResolver.Propose, not a per-title mapping.

| Scope | Identity | Fallback |
|---|---|---|
${[...Object.entries(table.families).map(([k,v])=>['Family '+k,v]),...Object.entries(table.genreOverrides).map(([k,v])=>['Genre '+k,v])].map(([k,v])=>`| ${k} | ${v.identity.join(', ')} | ${v.fallback} |`).join('\n')}

Additional modifier tables and exact perception settings are included verbatim below. Spec B governs taxonomy; typed/legacy tag mappings follow the compatibility adapter and Spec C (no lifecycle/economy). Spec A perception plus the authorized UI report supplies the uncertainty/gate/standing proposal provenance.

\`\`\`json
${JSON.stringify(Object.fromEntries(Object.entries(table).filter(([k])=>!['numbers','identityWeights','archetypes','families','genreOverrides','axisOrder','version','provenance'].includes(k))),null,2)}
\`\`\`

\`\`\`json
${JSON.stringify(perception,null,2)}
\`\`\`
`;
fs.writeFileSync('SimTools/PolarSongReport.md',main);
fs.writeFileSync('SimTools/PolarSongTables.md',appendix);
const artifacts = ['SimTools/PolarSongReport.md','SimTools/PolarSongTables.md','SimTools/PolarSongFinalValidation.json','SimTools/PolarSongFinalHashes.json'];
fs.writeFileSync('SimTools/PolarSongReportArtifacts.json',JSON.stringify({date:'2026-10-03',files:artifacts.map(path=>({path,sha256:hash(path)}))},null,2)+'\n');
console.log('Wrote consolidated report, shipped-table appendix, validation copy and SHA-256 manifests.');
