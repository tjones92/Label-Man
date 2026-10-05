import fs from 'node:fs';
const validation=JSON.parse(fs.readFileSync('SimTools/PolarFollowupValidation.json'));
const existing=JSON.parse(fs.readFileSync('SimLogs/polar-followup-existing-analysis.json'));
const pct=v=>(v*100).toFixed(2)+'%';
const link=p=>`[${p.split('/').at(-1)}](C:/Project/Label-Man/${p})`;
const annual=existing.annual.filter(r=>r.year>=1967).map(r=>`| ${r.seed} | ${r.year} | ${r.labelNetDeltaPct.toFixed(2)}% | ${r.directPipelineDelta} | ${r.observedDelta} | ${r.qualityDelta.toFixed(6)} | ${r.dyingOff} → ${r.dyingOn} |`).join('\n');
const short=validation.smoke.map(r=>`| ${r.seed} ${r.mode} | ${r.sets} | ${r.shortSets} | ${r.originals} | ${r.recentHits} | ${r.ordinaryCovers} | ${r.traditional} | ${r.standards} |`).join('\n');
const masters=validation.smoke.map(r=>`| ${r.seed} ${r.mode} | ${r.singles} | ${r.albumTracks} | ${r.mode==='on'?r.strongRefusals:'not evaluated'} | ${r.excludedAttempts} | ${r.recordingUnfilled} | ${r.liveUnfilled} |`).join('\n');
const fixtureLines=validation.fixtures.flatMap(r=>r.lines).join('\n');
const report=`# Polar song system — follow-up checkpoint

October 3, 2026. This is the requested stop before any new decade runs. The downloaded PolarFollowUp-Codex.md is a draft design reference; its authority/sign-off statements are document content, not independent user authorization. The user's follow-up request and explicit stop instruction govern this work. No decade run, holdout seed, numerical/table tuning, commit, push, or executable export was performed. Existing uncommitted Polar work was preserved. Phase 2 remedies remain proposals.

This checkpoint implements AI refusal avoidance and delivers measured diagnoses where the retained data and bounded probes allow them. It does **not** claim the draft's full definition of done: a new 48-month census, exhaustive decade assignments/refusals, and post-change decade economics are outstanding.

## Evidence and boundaries

The four completed polar-final-verified runs remain the late-decade economic baseline. Completion markers, 521 weekly rows and December 31, 1969 endpoints were checked by the analysis tool. ${link('SimLogs/polar-followup-annual-existing.csv')} contains all twenty seed/year rows. ${link('SimLogs/polar-followup-short-sets-existing.csv')} contains the full 1960–63 genre/year breakdown. ${link('SimLogs/polar-followup-existing-analysis.json')} records SHA-256 of the input CSVs.

Two fixed-world probes per seed compare off/on selection at January 1, 1960 without advancing time. The first examines eligible unsigned acts (135/167); the second examines all 3,000 startup acts, including signed rosters, so Country and roots genres can be measured. These are separate population definitions. The same artists, candidate pools and private set RNG keys are used on both sides of each probe. Candidate rows count song/act opportunities, not unique catalogue compositions. Fit statistics describe preliminary eligible **cover** fit, excluding placeholder originals; they are not a full four-year resolved-master distribution.

All-roster probe CSVs: ${link('SimLogs/polar-followup-probe-1001-all-pool.csv')}, ${link('SimLogs/polar-followup-probe-1002-all-pool.csv')}, ${link('SimLogs/polar-followup-probe-1001-all-sets.csv')}, ${link('SimLogs/polar-followup-probe-1002-all-sets.csv')}, ${link('SimLogs/polar-followup-probe-1001-all-fit.csv')}, ${link('SimLogs/polar-followup-probe-1002-all-fit.csv')}. The unsigned versions use the same names without “-all”.

Four final-binary two-week runs emit the corrected January census, assignment and refusal CSVs. They are smoke checks, not substitutes for 48 months. The two one-year disabled neutrality runs reuse the retained 79-stream controls. ${link('SimTools/PolarFollowupValidation.json')} records all checks, invocation arguments, assembly/table fingerprints and fixture outputs.

## Phase 0 diagnoses

### 0.1 Trad/std ceiling — mechanism confirmed in measured roots genres; full-period coverage incomplete

There is no measured 75–78% songbook cap. PlayerDesk.BuildLiveSet allocates originals first: zero, one or two using its existing songwriting cutoffs, then requests enough covers to aim for three to five total slots. Non-rock SelectLiveCovers ranks the live pool; the rock source-policy factors do not apply to Country, Blues, Jazz, Folk or Gospel in this probe.

The audit counterfactual keeps the same act, pool and requested total length, but requests covers for the original positions too. Blues, Jazz, Folk, Gospel and Country then reach **100% songbook slots in both seeds**. For example seed 1001 Country normally has 173 originals plus 578 songbook slots out of 751; the all-cover counterfactual fills 751/751 with songbooks. This causally rules out a hard songbook clamp at the observed cluster in these populations. The apparent ceiling is the original-slot complement when the best available covers are songbook material. ContemporaryFolk and BossaNova were absent at startup; their full-period mechanism is not causally confirmed here.

Per-genre/category pool composition is in the pool CSVs. The live policy is distinct from recording source shares: non-rock live sets do not use SourceMix/StandardShareFactor to set their songbook quota. No policy constant or writing cutoff was varied in gameplay. The audit varied the requested cover positions only.

### 0.2 Country +17pp — selection mechanism confirmed; exact four-year magnitude not re-created

In the fixed roster, seed 1001 Country moves from 455/751 (60.59%) to 578/751 (76.96%); seed 1002 moves from 417/690 (60.43%) to 516/690 (74.78%). Originals and total slots are identical within each pair. Pool entries are identical off/on. Seed 1001's pool contains 353 standards, 97 traditional songs and 120 recent-hit songs per act; seed 1002 contains 369, 81 and 120. Ordinary catalogue covers are absent in these measured pools.

Changing the selection branch alone removes all selected recent-hit covers from Country in both probes. The move is caused by deterministic fit ordering (including its genre/song-ID tie ordering), not pool expansion or the rock policy. Country category mean Capability is effectively the same; Identity is near saturation, with small category differences. Seed 1001 mean Identity: standards .997935, trad .997871, hits .997825. Do not interpret the volume shift as a large Capability advantage. The exact 1960–63 unsigned-population increase still requires the longer replay to confirm.

### 0.3 EasyListening — on-selection preference confirmed; baseline seed instability unconfirmed

The initial unsigned pools are rich in standards. In seed 1001 they contain 837 standards, 183 trad and 340 recent hits per act; seed 1002 has 825, 195 and 340. Ordinary covers are zero in these snapshots. Mean preliminary Capability on standards/trad is .589/.630, versus .766/.792 on the recent-hit category. The same-pool intervention moves selected unsigned recent-hit slots from 37 to 178 in seed 1001, and 57 to 192 in seed 1002, with unchanged originals and total slots. This confirms fit selection can favor recent hits despite a standard-heavy pool.

The all-roster intervention also sharply lowers songbook share: 361/597 → 34/597 and 329/588 → 55/588. Neither startup population reproduces the reported four-year baseline .70% versus 8.76%. The cause of that late census seed difference remains **unconfirmed**; monthly pool evolution and population composition were not captured in the historical aggregate census. No additional seed was consumed.

Possible later targets for Alice: retain the emergent share with no genre target; preserve a broad standards-led adult repertoire; or define a narrower recent-hit-oriented EasyListening scene and separate traditional orchestral material. These are qualitative options, not new numerical settings. The pool/act taxonomy needs review before choosing a percentage.

### 0.4 Short sets — attribution measured; full regression cause unconfirmed

The retained census exactly reproduces 11,488 → 13,078 short sets for seed 1001 (+1,590), and 12,778 → 15,811 for seed 1002 (+3,033). Seed 1001: LatinPop +3,025 and TexMex +2,063, offset by Classical −2,325, Comedy −713 and Childrens −460. Seed 1002: Classical +3,155, LatinPop +155, TexMex +1, offset by Comedy −170 and Childrens −108. Every other genre has zero short sets in both modes. The CSV includes each year and the denominator; raw counts are not fixed-cohort treatment effects.

The fixed startup roster shows every short set in those five genres has an empty eligible cover pool, and the count is identical off/on. No startup future-song filtering or bounded rock sampling explains those short sets. The historical regression is concentrated in the same genres, but aggregate data cannot distinguish later thin/empty pools, future filtering, or changed population size. The exact causal increase is **unconfirmed**. We did not repair it.

Phase 2 options for later review: expand or correct family fallback when the eligible pool is empty/thin; distinguish empty pools from future-only pools before deciding whether to borrow material; or show an explicitly incomplete set while retaining fit selection. Pool expansion can reduce short sets but admit weaker identity fits; relaxing suitability can fill more slots at the cost of fit; an explicit incomplete-set presentation changes interpretation without improving the count. No remedy or proposed gate was implemented.

### 0.5 Late-decade drift — signs confirmed from retained runs; economic causation unconfirmed

| Seed | Year | Label net change | Direct pipeline single delta | Observed single delta | Mean quality delta | Dying labels off → on |
|---|---|---|---|---|---|---|
${annual}

Label net is lower in all six late seed/year comparisons, while mean observed-single quality is higher in all six. Direct singles decrease over the whole decade in both seeds, but their annual sign is **not** consistent: seed 1001 rises by five in 1969, seed 1002 rises by 118 in 1968. Dying-label differences are also mixed. These are endpoint stock counts, not lifetime exits.

The calibrated decade runs' song-material, single-release-lanes and detailed records CSVs contain headers only. Therefore observed singles cannot be split exactly into direct versus album-promoted releases from these retained streams. Subtracting the direct pipeline counter would mix definitions and is not used. Detailed causal finance/promotion attribution is unconfirmed. The earlier complete decade totals and engineering reference bands in PolarSongReport.md remain authoritative **baseline** results, not acceptance of this new implementation. No new-versus-current-on decade delta is claimed.

### 0.6 Fit scale — startup saturation measured; full census distribution and reroll exploit unconfirmed

Country candidate Capability above .95: 91,770/107,730 (85.18%) and 90,630/101,460 (89.33%). On-selected covers: 491/578 (84.95%) and 457/516 (88.57%). Mean candidate Identity is .997901/.996466. Capability alone has little category discrimination in these Country pools, and near-perfect truth fits are common without any reroll.

For initial unsigned EasyListening, seed 1001 candidates above .95 are 8,320/87,040 (9.56%); selected covers move from 20/190 (10.53%) to 147/190 (77.37%). The fit CSVs provide per-genre count, means of Capability/Identity/Stretch, and the above-.95 count. These are cover candidate/selected-slot statistics at startup, not complete 1960–63 distributions or all original slots.

Existing comparison/reopen fixtures pass and establish deterministic repeated reads for the same observation. A near-perfect truth fit is not proof that reopening the UI rerolls a perception band. A full reroll/exploit assessment and distribution quantiles remain unmeasured. F1–F4 are reproduced below; no table was changed.

### 0.7 Census categories — audit implementation and two-week emission complete; 48-month replay pending

The legacy recentCovers column remains for historical analyzer compatibility and means **all other covers**. New recentHitCovers and ordinaryCovers columns partition it using the composition's existing RecentHit origin category. This is the game's source category, not a new age cutoff. No recent-hit recency threshold was invented. The validator checks both partitions sum to slots. No historical census was overwritten or retroactively reclassified from insufficient aggregate columns.

| Seed / mode | Sets | Short sets | Originals | Recent-hit covers | Ordinary covers | Trad | Standards |
|---|---:|---:|---:|---:|---:|---:|---:|
${short}

### 0.8 Recording assignments — audit implemented; short-window evidence complete; decade emission pending

CompetitorManager emits a read-only assignment observation after applying single material or registering an album-track master. This captures album-only cuts and is independent of suppressed calibration diagnostics. CSV rows retain act/project genre, year/date, planned master ID, kind, category, and resolved fit/refusal decision where enabled. Reused/promoted album performances are not counted as new master assignments. Startup assignments created before ChartAuditRunner opens its subscribers are outside this event stream; a future exhaustive run must explicitly include or exclude that inventory. Cancelled releases may retain already assigned track masters, so these counts measure assignments rather than releases sold.

Assignment CSVs use polar-followup-checkpoint-{off,on}-{1001,1002}-polar-master-assignments.csv; refusal CSVs use the same run names with -polar-refusal-avoidance.csv. The live census uses -polar-live-census.csv. All are under C:/Project/Label-Man/SimLogs.

## Phase 1 implementation and acceptance

The shared pure act-facing wrapper is PolarSongBehavior.Refuses; PolarMaterialFit.WouldRefuse remains the single numerical evaluator. PlayerDesk.MaterialRefusals calls the wrapper with the same proposal, standing and empty-songbook state as before. AI selection evaluates **resolved proposals**, rejects sampled refusals before choosing source finalists, and checks the final/forced material too. Player-owned recording selections keep their booking/override workflow. Non-rock live ranking rejects unsuitable finalists lazily; rock and recording selection retain bounded sampling. No new gameplay RNG, threshold or table setting was added.

If no sampled acceptable source remains, ChooseMaterial returns null and emits unfilled. Single release callers cancel that attempt; album generation skips that track with a bounded attempt index, preserving stable IDs and preventing an infinite fill loop; an empty album/promo attempt is cancelled safely. The album's metadata-only aggregate material is not a master and is excluded from this gate. The default unfilled fallback from the draft is used; penalties and relaxed-refusal fallback are absent.

AI empty-songbook context uses the existing PlayerDesk repertoire/written-song query, matching the player rule. AI acts generally have no entries in that desk store and therefore receive existing empty-songbook softening. This is an explicit modeling limitation: there is no general durable AI live-repertoire store. A separate AI songbook definition needs Alice's review; this pass does not invent one or a new threshold.

| Seed / mode | Single assignments | Album-track assignments | Strong refusals | Excluded attempts | Unfilled recording slots | Unfilled live positions |
|---|---:|---:|---|---:|---:|---:|
${masters}

The enabled short runs exclude zero real-world candidates; all measured master assignments pass the shared evaluator. The synthetic real-resolver fixture exercises rejection, accepted alternatives and an all-refused pool, which leaves its slot empty and emits exactly one exclusion and one unfilled observation. Unfilled live positions include pre-existing empty-pool failures and must **not** be attributed to avoidance. The initial unsigned short-set counts are unchanged versus the pre-avoidance fixed-world probe, so measured startup avoidance short-set change is zero. Longer-window effects remain unknown.

Both 52-week disabled checks match all 79 retained CSV streams byte for byte. Those runs preceded the final enabled-album-metadata guard and optional roster-probe refinement; neither edits the disabled path. Final-binary disabled/player/ownership/selection/recording/perception/rock fixtures also pass. Full four-year and decade refusal acceptance, post-change economic guardrail results, and complete player walkthroughs beyond the existing fixtures remain pending. The old decade reference bands (±3% observed singles, at most .02 quality loss, at most 5% label-net loss) are engineering references, not Alice's tolerances.

## Phase 3 royalty routing

Current MechanicalRoyaltyService.ChargeSide computes the liability from the existing composition/controller snapshots. Artist-controlled writer payment resolves the controlling artist and falls back to the performer when lookup fails. That can misdirect a missing original writer's payment. A missing other-label controller receives no credit and its slice leaks out of the simulated economy; the payer's expense is unchanged. External publisher slices also leak by design. No routing repair was made.

MechanicalRoyaltyService is player-only at its call sites; AI pressing/COGS already incorporates calibrated composition costs. The four retained headless decade runs contain no player sales, so additional player-mechanical dollars affected in those runs are zero by that path's reachability. This is not a measured count of all missing rights controllers or misrouted publishing/writer payments. Detailed controller-event frequency and dollars for the broader publishing issue cannot be recovered from the retained aggregate publishing-ledger stream and remain **unmeasured**.

Minimal future options: route an unresolved artist slice to an explicit external leak (simple, preserves the payer liability); hold it in an unresolved-rights suspense balance (recoverable, requires durable accounting state); or resolve a valid composition controller before paying (best attribution, requires a verified controller lookup contract). Preserve composition snapshots and total liability in every option. A general per-writer ledger remains out of scope.

## Phase 4 wiring plan only

Spec A is available locally in SimTools/PolarSong_SpecA_FitAndPerception.md; its Respect example describes successful reinterpretation superseding the original without changing publishing. The current helper's signed result is diagnostic and does not itself choose a definitive master.

On a completed cover outcome, associate any approved definitive outcome with the composition's recording-memory entry and master ID, never overwrite firstCommittedMasterId, the original demo, parentRecordingId, writer credits or controller snapshots. CompositionCatalogService's completion/outcome update is the natural owner; PolarMaterialFit.ReinterpretationOutcome supplies a diagnostic input, and PolarSongMetadataService retains the referenced recording identity. Alice still needs to choose what constitutes success and how competing versions supersede one another.

SongMaterialSelectionService.CoverFatigueShadow already reads the best definitiveVersionScore for recent-hit pressure. Updating that score would affect later cover sourcing. PolarSongBehavior.Reference currently chooses the latest eligible completed master, **not** the highest definitive score, so score wiring alone would not make later acts reference the definitive version. An explicit era-safe definitive-reference selection policy, and any cover-pool removal/readoption rule, need approval before implementation. PolarPlayerPerception and UI comparisons would consume the chosen reference while retaining persisted heard-version observations. Royalty routing remains composition based; the main risks are invalid/future reference IDs, changing master lineage, save migration, and accidentally rewriting settlement snapshots. No definitive/reinterpretation state or consumer was changed.

## Current fixture outputs

\`\`\`text
${fixtureLines}
\`\`\`

## Reproduction, hashes and outstanding decisions

Run node SimTools/analyze-polar-followup.mjs to reproduce retained-data tables, and node SimTools/validate-polar-followup.mjs to verify the new artifacts. Build with dotnet build --no-restore. Focused fixtures use the existing SaveLoadRoundTripRunner.tscn with --seed=1001 and the named --polar-…-check switches above. Fixed-world probes use PolarFollowupProbe.tscn with --seed=1001 or 1002; add --probe-all-acts for the 3,000-act intervention. Existing probe outputs are protected from overwrite.

For a new bounded audit, use ./SimTools/run-polar-followup-audit.ps1 -Seed 1001 -Mode on -Weeks 2 -Run <new-name>. This helper permits only seeds 1001/1002 and one through 209 weeks; it rejects decade windows and existing artifact prefixes. A future 209-week run could complete the requested 48-month census, but none was launched here. Do not use run-polar-final-audit.ps1's default 521-week window for this request.

The new PolarFollowupHashes.json fingerprints all touched source/tool/config artifacts, this report, validation, the unchanged tables and current binary; historical manifests remain untouched. Initial sandboxed Godot launches crashed before startup. Elevated retries completed; their failed logs are retained and excluded. The build required the existing user NuGet configuration and passed with four pre-existing warnings. git diff --check passes.

Alice's open decisions: keep or replace the default unfilled fallback; define the AI empty-songbook context if the desk store is insufficient; choose whether/when refusal penalties follow; choose a short-set remedy and confirm/replace the proposed gate; choose a royalty fallback; confirm definitive-version success/reference/cover-pool behavior. No numeric tuning is proposed as an implemented change.

Outstanding measurement/scope deviations: no full 48-month category/distribution replay; no post-avoidance decade run or updated economics; no exhaustive decade startup inventory/assignment coverage; no exact observed release-lane split from header-only retained streams; no causal explanation for EasyListening's four-year off-seed spread or the full short-set regression; no detailed missing-controller frequency/dollars; no complete player walkthrough rerun. January/all-roster probes must not be presented as a replacement unsigned census. The draft's full acceptance remains incomplete at this user-requested checkpoint.
`;
fs.writeFileSync('SimTools/PolarFollowupReport.md',report);
console.log('Wrote SimTools/PolarFollowupReport.md');
