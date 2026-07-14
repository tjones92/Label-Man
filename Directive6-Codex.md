# Directive 6 - Artist Population, Contract, and Career Lifecycle

## 0. Status and implementation posture

This directive authorizes a dedicated artist-population and career-lifecycle phase. It supersedes the provisional reference in Directive 5 to a Directive 6 naming overhaul; that naming work is renumbered and remains deferred.

Freeze late-decade genre expansion, genre calibration, and genre-economic tuning while this directive is active. The canonical genres and supply rules already present may be consumed by artist formation, but their emergence years, baseline keyframes, supply weights, format behavior, regional routing, momentum, and economic constants are not opened for revision here.

Preserve the enabled atomic dropped-artist ownership repair, terminal release guard, unsigned-pool reconciliation, and vacancy-responsive scouting seam already present in the working tree. The seed-1001 `d6-vacancy-scouting-104-enabled-1001` run is diagnostic evidence, not an accepted target: it removes terminal artists correctly but develops a re-sign/re-drop loop and loses release capacity in 1961.

### Capacity amendment — vacancy-responsive scouting candidate

The `C = 13` candidate passes every lifecycle, ownership, chronology, format, and economic condition but fails the 1961 release-capacity guard: `3,644 / 4,810 = 0.7576`, below the retained `[0.85, 1.15]` guardrail. Its roster declines from `2,626` to `2,281` because `2,026` departures exceed `1,408` signings. The signing path made `1,413` attempts, with only five affordability rejections, 77 score rejections, zero release-selection failures, and a remaining free-agent pool of `1,280`. Formation and the pool are therefore not binding; vacancy-responsive scouting-gate throughput is.

This amendment authorizes exactly one candidate: increase the enabled vacancy-responsive scouting multiplier in `AILabel.ShouldScoutNewArtist` from `0.15` to `0.20`. The disabled/legacy route must continue to use `0.15`, preserving its exact RNG and output boundary. Keep attempts per passing label, scoring, affordability, drop thresholds, formation, release selection, release capacity, format, finance, and all other economic rules frozen. The `[0.85, 1.15]` release and Album guardrails remain in force; specifically, do not waive the Gate C failure. Run only fixed probes, the disabled replay, and Gates A–C. Watch Gate B Albums closely because the prior result is `1.1486x`. If `0.20` fails, diagnose vacancy duration and unfilled slots before authorizing another scalar; do not automatically advance to `0.25`. Gate D and any 520-week replay remain unauthorized until Gate C passes.

Add a separately staged toggle, `artistPopulationLifecycleEnabled`, with command-line overrides `--enable-artist-population-lifecycle` and `--disable-artist-population-lifecycle`. Reject both flags together. Enabling artist population requires Genre Market V2 to be enabled; reject an artist-population-enabled/genre-market-disabled combination rather than silently falling back. Keep the new toggle disabled by default until this directive is complete.

The compatibility boundary is the accepted seed-1001 disabled replay. When artist population is disabled:

- preserve the original 3,000-artist initialization and every stored `formedYear`;
- do not generate runtime artists or musicians;
- do not evaluate new contract, cooldown, inactivity, retirement, or disbandment rules;
- do not add, remove, or reorder RNG calls;
- do not alter the values, headers, order, or set of the 45 frozen CSV streams; and
- remain byte-identical to `d6-fulfillment-emerging-memory-52b-control-1001` and the matching accepted disabled lineage.

New telemetry must be emitted only in new artist-population streams when the new toggle is enabled. Do not modify frozen streams merely to report this feature.

The treatment begins on the first live tick. Initial population generation and prewarm remain unchanged. No runtime formation or exit event may occur during prewarm.

## 1. Objective

Build one coherent system in which:

1. lifetime performance and current-contract probation are distinct;
2. a recent performance drop cannot be recycled immediately, but a mature dropped artist can receive a genuine second chance;
3. every live calendar year creates a meaningful cohort of new `SimulatedArtist` and `Musician` entities independent of roster vacancies or release results;
4. runtime cohorts enter as unsigned talent and reach labels only through ordinary scouting, candidate evaluation, affordability, and signing;
5. runtime artists store their actual formation year rather than inheriting the 1960 initialization backdate;
6. new artists receive native genres that are available for new supply in their formation year;
7. transitional projects remain distinct from the artist's native identity;
8. long-term inactivity, retirement, and disbandment create real population exits without deleting career history;
9. active, unsigned, signed, inactive, retired, and disbanded populations reconcile exactly; and
10. the 1969 world contains later formation cohorts and native late-decade identities rather than only repurposed members of the original 1960 entity cohort.

Population renewal is required for chronology and world coherence even if a contract-only repair restores release counts. Do not make runtime formation conditional on a capacity failure.

## 2. Evidence requiring the pivot

The current implementation has four binding defects.

### 2.1 Initial-only artist generation

`ArtistManager.GenerateInitialPool` is the only runtime caller that creates artists. It runs in 1960 and calls `GenerateArtist`, which stores:

```text
formedYear = year - (int)GD.RandRange(0, 5)
```

Consequently, every artist entity in a decade simulation belongs to the original population and normally reports a formation year from roughly 1955/56 through 1960, depending on the current `RandRange` endpoint semantics. `ArtistDetailPanel` displays that stored value directly.

### 2.2 Contract probation reads lifetime counters

Re-signing changes a dropped artist to `NewSigning` while leaving lifetime hits and consecutive flop state intact. The `NewSigning` transition then tests lifetime `top40Hits` and `consecutiveFlops`. A historical hit can end probation on the next state update; carried flops can immediately return the artist to `Dropped`.

In the 104-week diagnostic run:

- 1960 has 854 drops, 578 re-signings, and 116 re-drops within 26 weeks;
- 1961 has 2,808 drops, 1,674 re-signings, and 1,243 re-drops within 26 weeks;
- first-time signings are zero in both years; and
- the free-agent pool grows to 1,704 while the roster falls to 1,296.

### 2.3 No authored artist exits

`CareerState.Retired`, `CareerState.Disbanded`, `SimulatedArtist.RemoveMember`, musician age, and inactive flags exist, but no live population owner authors persistent artist retirement, disbandment, or long-term inactivity. Record retirement is not artist retirement. The original entity cohort therefore persists as signed or recyclable dropped talent.

### 2.4 Project genre is not artist identity

The release pipeline temporarily assigns the chosen project genre to `artist.primaryGenre`, performs the format/release work, and restores the old primary genre. A Proto-Metal record may therefore link to a Blues Rock artist profile. Transitional projects are valid, but they cannot substitute for native later cohorts.

## 3. Binding state model

### 3.1 Keep the axes separate

Do not create another all-purpose enum. Treat these as separate axes:

- **Formation identity:** immutable formation cohort, native primary genre, native secondary genre, formation date/year, and original region.
- **Current artist identity:** the stable artist-level genre shown in the artist profile.
- **Project identity:** the genre selected for one record or Album project.
- **Career performance:** lifetime hits, sales, stature, momentum, reputation, and existing performance-stage behavior.
- **Contract cycle:** owner, contract sequence, start week, probation results, and contract terms.
- **Population lifecycle:** active, inactive, retired, or disbanded.

Add an orthogonal `ArtistLifecycleStatus` with at least:

```text
Active
Inactive
Retired
Disbanded
```

Retain the existing `CareerState` for compatibility and existing performance presentation. When lifecycle status becomes `Retired` or `Disbanded`, mirror the existing terminal career state. Do not add `Inactive` into ordinal career comparisons. `isActive` becomes a compatibility mirror of `lifecycleStatus == Active` on the enabled path.

### 3.2 Structured departure reasons

Replace live-path reason strings as decision inputs with a structured `ArtistDropReason` containing at least:

```text
Performance
ContractExpired
LabelClosure
Financial
Voluntary
LifecycleReconciliation
```

Human-readable career events may still format these reasons as text. Logic must not branch by comparing display strings.

### 3.3 Ownership and availability invariants

At every enabled telemetry boundary:

- an active signed artist has one non-empty `labelId`, exactly one matching roster membership, and no unsigned-pool membership;
- an active never-signed artist has no owner and exactly one unsigned-pool membership;
- an active dropped/free-agent artist has no owner and exactly one unsigned-pool membership, whether cooldown-blocked or eligible;
- an inactive, retired, or disbanded artist has no owner, no roster membership, no unsigned-pool membership, and is not signable or release-eligible;
- no artist appears twice in a roster or unsigned pool;
- no two artists or musicians share an ID; and
- historical records and public profiles remain resolvable after an artist exits.

`Dropped` means an active free agent, not an inactive or retired act.

## 4. Contract semantics and second chances

### 4.1 Contract-scoped probation

Add enabled-path contract-cycle fields sufficient to record:

```text
contractSequence
contractStartWeek
contractTop40Hits
contractConsecutiveFlops
contractCompletedChartRuns
```

Reset these on a new free-agent signing. Do not reset lifetime `charted`, `top40Hits`, `top10Hits`, `numberOnes`, total releases, units, reputation, momentum, or career history.

For an enabled `NewSigning`, use only current-contract evidence for the probation branches:

- one current-contract Top-40 hit advances the artist from `NewSigning`;
- two current-contract consecutive flops permit the existing probation drop; and
- lifetime results may influence later performance stature only after probation is resolved.

Keep the existing two-flop threshold. This phase changes the scope of the evidence, not the drop tolerance.

Same-label contract renewal does not create a new probation cycle unless the artist first leaves ownership and re-enters through the free-agent signing path.

### 4.2 Performance-drop cooldown

Record `lastPerformanceDropWeek` when the structured reason is `Performance` or a reconciliation of an already reached performance-terminal `Dropped` state.

The initial candidate is:

```text
performanceDropCooldownWeeks C = 26
```

Before candidate ranking, exclude an active performance-dropped artist while `currentWeek - lastPerformanceDropWeek < C`. Leave the artist in the pool. At age `C`, restore ordinary eligibility without a score penalty, quality change, reputation change, or guaranteed signing.

Contract expiry, label closure, or a purely financial departure does not receive a performance cooldown. A normal free agent may be considered immediately.

The cooldown prevents immediate recycling; it is not a permanent exclusion. A valid 104-week run must contain at least one re-signing of a matured performance-dropped artist.

## 5. Calendar-driven population formation

### 5.1 Formation is exogenous, not vacancy-triggered

Runtime formation is required in every live calendar year regardless of roster capacity, release count, label profitability, genre share, or the size of the dropped pool.

Use a deterministic weekly accumulator derived from the initial cohort size:

```text
annualFormationRate F = 0.10 of initial cohort
initial cohort = 3,000
initial annual runtime cohort = 300 artists
```

Accumulate `300 / 52` formations per live week and materialize the integer portion. Carry the fractional remainder so a full 52-week year creates 300 artists, subject only to a documented final-week rounding rule. Do not use a Poisson count, vacancy multiplier, release deficit, or roster target to decide how many artists form.

`F = 0.10` is the initial demographic candidate because it creates a runtime cohort comparable to the original population over the decade. `52` entrants per year is not an acceptable final population mechanism; it may be retained only as obsolete diagnostic evidence.

Every runtime artist:

- is registered exactly once in the artist registry;
- receives newly generated musician entities with unique IDs;
- enters exactly once in the unsigned pool;
- starts with no label, no contract, no release, and lifecycle status `Active`;
- is not directly handed to a vacant label; and
- must pass existing scouting, evaluation, and affordability logic.

### 5.2 Formation date and cohort identity

Preserve the current 1960 initial-pool backdating exactly. Add an explicit creation mode:

```text
InitialLegacyCohort -> preserve existing formedYear = 1960 - (int)GD.RandRange(0, 5)
RuntimeFormation    -> formedYear = current simulation year
```

Store an immutable cohort identifier such as `InitialLegacy` or the runtime formation year. A runtime artist formed in 1968 must display `Formed 1968`, not an inferred earlier year.

Do not backdate runtime cohorts merely to make the roster appear older or to increase late-genre counts.

### 5.3 Preserve non-population generation priors initially

For the first formation candidate, preserve the existing artist-type proportions, musician skill/personality distributions, musician age distribution, group-size rules, and region picker. Do not combine population renewal with a gender, geography, naming, talent, age, or group-composition recalibration.

The Directive 5 naming overhaul remains out of scope. Use the existing naming services and canonical naming groups.

### 5.4 Era-aware native genre selection

Runtime primary identity candidates must come from `GenreSupplyService.GetAvailableGenres(formationYear)`. A genre in `PreEmergent` or `Legacy` state is not a new native primary unless an existing explicit supply bridge authorizes it.

Use the existing prospective, calendar-authored supply inputs rather than realized outcomes:

- canonical baseline for the formation year;
- lifecycle state;
- existing British supply bridge;
- existing prospective concentration brake applied to runtime formations; and
- no label fit, artist fit, chart results, realized units, release timing, or current roster deficit.

The natural initial implementation is the existing `GenreSupplyService.GetSupplyWeight` with neutral label, artist, and region inputs plus recent runtime-formation counts. Do not add or retune genre keyframes or supply coefficients in this directive.

Select the secondary identity from available canonical adjacent/family-related genres. Do not route unfamiliar late genres through the legacy `TraditionalPop` fallback. Both formation genres and the selection weights must be emitted to telemetry.

Store immutable:

```text
formationPrimaryGenre
formationSecondaryGenre
```

Initial artists receive these fields from their canonicalized initial identity. Runtime artists receive them at formation. Existing `primaryGenre` remains the stable current artist identity unless a later directive authors persistent identity change.

### 5.5 Dedicated population RNG stream

Runtime formation consumes many random values and must not perturb the global simulation RNG merely by existing. Create a deterministic population-generation RNG stream derived from the simulation seed and a stable namespace such as `artist-population-v1`.

Route runtime formation count-independent attribute, member, name, genre, and region rolls through that stream. Preserve all existing initial-generation/global-RNG calls when the feature is disabled. Two independent enabled runs with the same seed must be byte-identical in every emitted stream.

## 6. Inactivity and career exits

### 6.1 Continuous unowned time

Track `weeksContinuouslyUnowned` for every active artist with no label. Reset it on signing. Do not derive it from `formedYear`, lifetime release count, or the age of a historical record.

An active unowned artist remains eligible according to normal unsigned or matured-drop rules until the inactivity horizon. The initial candidate is:

```text
inactivityHorizonWeeks H = 78
```

This gives a performance-dropped artist 26 cooldown weeks followed by 52 ordinary signable weeks before inactivity.

At `H`, transition the artist to `Inactive` only if all are true:

- no label ownership or roster membership;
- no active chart record requiring an artist-state completion callback;
- no pending Album project or scheduled promo owned by that artist;
- no unresolved contract transaction; and
- the unsigned-pool and ownership invariants reconcile.

Inactivity removes the artist from the unsigned pool and sets `isActive = false`. It does not delete the artist, musicians, releases, or career history. This directive does not implement comeback/reactivation.

### 6.2 Retirement and disbandment

After an additional fixed inactive horizon:

```text
terminalInactivityWeeks T = 52
minimumSoloRetirementAge R = 35
```

- a Band, Duo, Trio, or Vocal Group becomes `Disbanded`;
- a solo artist whose active/lead member is at least `R` becomes `Retired`; and
- a younger solo artist remains `Inactive` until reaching the age requirement or a later explicitly authored rule.

Terminal transition sets the matching existing career state, keeps ownership and pool membership empty, and records one structured exit event. For a retired solo act or disbanded group, set remaining active members inactive with a coherent reason and year.

Do not retire or disband an artist merely because the registry is large, entrants were generated, a label needs a vacancy, releases are below target, or economics are outside a band. Population exits are authored lifecycle consequences, not a balancing sink.

Do not add mid-career member replacement, reunion, solo spin-off, death, touring burnout, marriage, military service, scandal, or health simulation in this directive. `RemoveMember` and lineup replacement remain deferred rather than being activated as an uncalibrated second population system.

## 7. Native artist identity versus project identity

Preserve the distinction between an artist and a record:

- a Blues Rock artist may make a Proto-Metal project;
- the record is Proto-Metal;
- the artist profile remains Blues Rock unless a future persistent-identity mechanic changes it; and
- runtime 1968 cohorts can also form as native Proto-Metal artists.

Replace the enabled path's temporary mutation-and-restore of `artist.primaryGenre` with an explicit project-genre argument or immutable decision context. No early return or exception may leave the artist with a project-only identity.

Record and Album generation must receive both artist identity and project identity explicitly. Artist profile click-through continues to display the artist identity and stored formation year. Record/Album UI and telemetry display the project genre without pretending it permanently changed the artist.

No persistent artist genre-transition mechanic is authorized here.

## 8. Authoritative weekly order

Do not rely on incidental Godot event subscription order. Establish one explicit live population/lifecycle sequence:

1. finish the week's record/chart callbacks and contract-performance updates;
2. perform atomic ownership and terminal reconciliation;
3. apply eligible inactivity/retirement/disbandment transitions;
4. materialize the calendar formation accumulator;
5. reconcile roster and unsigned-pool invariants;
6. run the existing vacancy-responsive scouting and ordinary signing path; and
7. capture population and roster telemetry after all transitions.

Formation before scouting permits a new artist to be considered that week but does not guarantee consideration. Exits must never invalidate a live chart callback or pending Album project.

## 9. Implementation phases

### Phase 0 - Freeze, toggle, inventory, and observation

1. Add the toggle and new telemetry shells with no enabled behavior.
2. Inventory every artist/musician generation call, career-state write, `isActive` write, roster/pool mutation, contract transition, project-genre mutation, save/serialization seam, and public-profile consumer.
3. Capture the original cohort's formation-year, native-genre, type, member-age, roster, and unsigned distributions.
4. Prove toggle-off byte identity across all 45 frozen streams.

Stop if the disabled boundary is not exact.

### Phase 1A - Contract-scoped probation

1. Add contract-cycle counters and structured departure reasons.
2. Keep the probation threshold at two current-contract consecutive flops.
3. Add fixed probes for historical-hit/flop separation and same-label renewal behavior.
4. Preserve current candidate eligibility; do not add cooldown or formation yet.

Run fixed probes, disabled replay, and one seed-1001 enabled 52-week checkpoint. Stop if historical totals change or a carried lifetime result resolves probation.

### Phase 1B - Performance-drop cooldown

1. Add `C = 26` at the enabled candidate-filter seam.
2. Add drop-to-re-sign age and matured-second-chance telemetry.
3. Do not change scouting chance, candidate score, affordability, contract offer, or drop thresholds.

Run fixed probes, disabled replay, seed-1001 52 weeks, then seed-1001 104 weeks. No 520-week run is authorized.

### Phase 2 - Calendar formation and native identity

1. Add the dedicated population RNG and deterministic formation accumulator.
2. Add `F = 0.10`, runtime formation years, immutable formation identity, and era-aware genre selection.
3. Enter every runtime artist through the unsigned pool.
4. Replace temporary project-genre mutation on the enabled population path.
5. Do not add exits yet.

Run fixed probes, disabled replay, 52 weeks, and 104 weeks. Require nonzero first-time signings and exact formation counts before proceeding.

### Phase 3A - Long-term inactivity

1. Add lifecycle status, continuous-unowned time, and `H = 78`.
2. Add the atomic inactive transition and its pool/ownership guards.
3. Preserve history and block inactive signing, release, and project selection.
4. Do not add terminal exits, member replacement, or comeback behavior yet.

Run fixed probes, disabled replay, 52 weeks, and 104 weeks. Nonzero inactivity must be observed in the 104-week run.

### Phase 3B - Retirement and disbandment

1. Freeze `H`, then add `T = 52` and the fixed solo/group terminal classification using `R = 35`.
2. Add atomic retired and disbanded transitions.
3. Preserve history and block terminal signing, release, and project selection.
4. Do not add member replacement, reunion, or comeback behavior.

Run fixed probes, disabled replay, 52 weeks, and 104 weeks. Retirement and disbandment must be exercised by fixed probes; a short run need not fabricate a terminal event merely to make its count nonzero. No 520-week run is authorized until the integrated Gate D below passes.

### Phase 4 - Integrated 104-week acceptance

Freeze `P`, `C`, `F`, `H`, `T`, and `R`. Run one fresh seed-1001 enabled 104-week full-telemetry candidate against its matching disabled control. Apply every ownership, chronology, second-chance, formation, exit, capacity, format, and economic gate.

Passing Phase 4 authorizes decade validation. It does not authorize tuning genre constants.

### Phase 5 - Decade population validation

After Phase 4 passes, run one seed-1001 520-week enabled/control pair. If it passes the decade population gates, run seeds 1002 and 1003 without changing constants. Only after the three-seed candidate is frozen may one fresh holdout pair be selected and run once.

Late-decade genre implementation and calibration remain frozen until Directive 6 is accepted. Population results may reveal future genre work; they do not authorize it inside this directive.

## 10. Required fixed probes

Before each simulation checkpoint, deterministic probes must cover:

1. a re-signed artist with historical hits and five lifetime flops remains in probation until current-contract evidence resolves it;
2. one current-contract Top-40 hit advances probation and two current-contract consecutive flops permit a drop;
3. contract counters reset on free-agent signing but lifetime history does not;
4. structured performance drops receive cooldown; contract-expiry and label-closure departures do not;
5. a performance-dropped artist is ineligible at `C - 1`, eligible at `C`, and remains an active pool member throughout;
6. a matured dropped artist can be re-signed, receives one owner, leaves the pool, and later can be dropped only from new evidence;
7. one runtime formation creates exactly one artist, the required unique musicians, and one unsigned-pool entry with no label or release;
8. runtime `formedYear` equals the formation year while initial-cohort backdating remains unchanged;
9. formation counts from the accumulator equal the expected weekly, annual, and carry behavior;
10. a genre cannot be selected before `IsAvailableForNewSupply` allows it;
11. Hard Rock/Proto-Metal/Progressive Rock and other late genres become eligible at their authored dates without changing catalog constants;
12. runtime secondary genres are canonical, available, and related rather than silently defaulting to Traditional Pop;
13. population RNG is deterministic, independent of the global simulation RNG, and untouched when disabled;
14. an inactive transition removes pool/ownership membership exactly once and preserves history;
15. a group becomes disbanded and a qualified solo artist becomes retired after the authored inactive horizon;
16. inactive, retired, and disbanded artists cannot be signed, released, or scheduled for a project;
17. an artist with a live chart record or pending Album project defers exit;
18. project generation does not mutate the artist's stored identity;
19. native-identity and transitioned-project telemetry classify the same fixed cases correctly;
20. registry, roster, pool, and terminal-population counts reconcile exactly; and
21. all existing specialist, memory, supply, atomic-drop, and disabled-neutrality probes remain unchanged and pass.

## 11. Required telemetry

Add removable enabled-only streams. Telemetry must not consume RNG.

### `artist-population-events.csv`

One row for every formation, signing, re-signing, drop, inactivity, retirement, and disbandment event:

```text
seed,week,date,eventType,artistId,artistType,cohort,formedYear,
formationPrimaryGenre,formationSecondaryGenre,currentPrimaryGenre,
homeRegion,lifecycleStatus,careerState,labelId,labelTier,
dropReason,contractSequence,contractStartWeek,contractTop40Hits,
contractConsecutiveFlops,contractCompletedChartRuns,
weeksSincePerformanceDrop,weeksContinuouslyUnowned,
artistAge,leadMemberAge
```

### `artist-population-weekly.csv`

One row by week and label tier plus an `All` row:

```text
week,year,labelTier,registryTotal,activeTotal,rostered,
neverSignedUnsigned,eligibleDropped,cooldownBlockedDropped,
inactive,retired,disbanded,formedThisWeek,formedYtd,
firstTimeSignings,reSignings,performanceDrops,otherDepartures,
recentPerformanceReSignings,prematureProbationDrops,
noEligibleCandidatePasses,scoreRejections,affordabilityRejections,
ownershipConflicts,duplicateRosterEntries,duplicatePoolEntries,
terminalRostered,terminalReleaseEligible
```

### `artist-cohort-annual.csv`

One row by year, formation cohort/year, native genre, lifecycle status, and current roster tier. Include counts, first-time signings, repeat signings, releases, active unsigned count, median act age, median member age, retirement/disbandment/inactivity counts, and cohort shares of active population and signed rosters.

### `artist-project-identity.csv`

One row per release decision/project:

```text
week,year,recordId,projectId,artistId,formedYear,cohort,
formationPrimaryGenre,currentArtistGenre,projectGenre,
nativeIdentityProject,transitionedProject,labelId,labelTier,format
```

The audit may add fields but must not remove the distinctions above.

## 12. Validation gates

### Gate A - Build, fixed probes, and disabled byte exactness

- `dotnet build "Label Man.sln" --no-restore` passes with no new warning.
- The full fixed probe suite passes.
- `git diff --check` passes.
- All 45 disabled seed-1001 CSV streams remain byte-identical to the accepted control.
- Toggle-off initial artist and musician registries, IDs, attributes, formed years, rosters, and RNG order remain exact.

Any disabled difference is a hard stop. Do not freeze a new disabled baseline.

### Gate B - Enabled 52-week checkpoint

With `F = 0.10` and an initial cohort of 3,000:

- runtime formations equal `300 +/- 1` under the documented accumulator boundary;
- every runtime artist has `formedYear = 1960` and cohort `1960`;
- first-time signings are nonzero and every one passed through unsigned/scouting/signing;
- no runtime artist is directly assigned to a label or release;
- no formation genre violates new-supply availability;
- ownership conflicts, duplicate memberships, terminal roster members, and terminal release candidates are zero;
- recent performance re-signings younger than `C` are zero;
- premature probation drops are zero;
- successful releases and scheduled Album projects remain within `[0.85,1.15]` of control; and
- total units, gross, label net, and market net remain within `[0.90,1.10]` of control.

These economic and release bands are regression guardrails, not optimization targets.

### Gate C - Enabled 104-week checkpoint

- cumulative runtime formations equal `600 +/- 2`;
- both 1960 and 1961 runtime cohorts exist with correct formed years;
- first-time signings are nonzero in both years;
- a matured performance-dropped artist has a nonzero re-signing path;
- at least one full-run artist reaches `Inactive` through the authored unowned horizon;
- every re-drop after re-signing contains the required current-contract evidence;
- pool composition separates never-signed, eligible dropped, and cooldown-blocked dropped artists;
- ownership and terminal eligibility violations remain zero in both years;
- successful releases and scheduled Album projects remain within `[0.85,1.15]` of control in each year; and
- units, gross, label net, and market net remain within `[0.90,1.10]` of control in each year.

The current diagnostic's 1961 successful-release ratio of approximately `.623` and scheduled-Album ratio of approximately `.719` fail this gate even though headline economics remain near control.

Zero terminal-state format fallback is required. The known `Declining -> New/Unsigned (unexpected-state fallback)` mapping is reported separately and is not repaired through format-economic retuning in this directive.

### Gate D - Integrated authorization for a decade run

Repeat Gate C after all contract, cooldown, formation, identity, inactivity, and exit code is frozen. Require an independent enabled deterministic repeat to be byte-identical in every enabled stream.

Do not launch a 520-week run if any Gate A-C condition fails.

### Gate E - 520-week population and chronology

For each accepted seed:

- each full calendar year creates `300 +/- 1` runtime artists at `F = 0.10`;
- the decade creates approximately 3,000 runtime artists, reported exactly rather than inferred;
- no artist has a formation year later than the current simulation year;
- no runtime-cohort native primary or secondary genre predates its available-supply boundary;
- first-time signings occur in every calendar year;
- at least 30% of the active signable population and at least 25% of signed rosters at the end of 1969 formed after 1960;
- inactive, retired, and disbanded counts are each nonzero;
- active population remains within `[0.85,1.50]` of the initial cohort while the registry retains all historical entities;
- no terminal artist returns to signing or release eligibility;
- every late-emerging genre whose prospective formation model expected at least two artists has at least one native formation;
- every late-emerging genre with at least ten native formations has at least one ordinarily signed native artist by the end of 1969;
- late-emerging project telemetry contains native-identity projects rather than being entirely supplied by transitioned original-cohort artists;
- native and transitioned project shares are reported by genre and year without forcing either to 100%; and
- formed-year, act-age, member-age, cohort, native-genre, roster-tier, and exit distributions are included in the audit.

Continue to apply the inherited capacity, format, economic, distance, concentration, finance-reconciliation, seasonality, specialist, and memory health gates. A chronology pass does not waive an economic or invariant failure.

## 13. One-variable candidate authority

Fix integration and state errors before trying alternative constants. Change only one authorized scalar between candidates:

1. probation threshold `P = 2` is inherited and locked; changing it requires a new directive;
2. performance cooldown starts at `C = 26`; only `13` or `52` may be tested later, one at a time, if causal cooldown/second-chance gates fail;
3. annual formation rate starts at `F = 0.10`; only `0.08` or `0.12` may be tested later, one at a time, against chronology and active-population gates;
4. inactivity horizon starts at `H = 78`; only `52` or `104` may be tested later, one at a time, against second-chance and active-population gates;
5. terminal inactive horizon starts at `T = 52`; change it only after `H` is frozen; and
6. solo retirement minimum starts at `R = 35`; change it only after `T` is frozen.

Do not sweep combinations. Do not change `F` and an exit horizon in the same candidate. Log every attempted value, command, seed, hash, and result, including failed candidates.

No second measurement seed is authorized until seed 1001 passes fixed, disabled, 52-week, and 104-week gates. After that pass, at most four autonomous paired two-seed probes are allowed for any one authorized scalar before a three-seed checkpoint.

## 14. Explicitly rejected approaches

- Do not make formation conditional on vacancies, release shortfall, label failure, pool exhaustion, genre quota, unit targets, or economic targets.
- Do not retire or disband artists to force a target registry size, roster size, release count, genre share, or profit level.
- Do not tune formation or exit rates to reproduce a specific historical artist roster.
- Do not erase lifetime hits, sales, reputation, or career history on re-signing.
- Do not permanently ban dropped artists or make every departure receive a performance cooldown.
- Do not weaken drop thresholds to preserve roster volume.
- Except for the one authorized `0.15 -> 0.20` vacancy-responsive scouting candidate above, do not increase scouting frequency, the scout multiplier, attempts per label, or candidate score to force new-cohort signing.
- Do not directly sign generated artists, fabricate releases, schedule synthetic Album projects, or inject revenue.
- Do not tune advances, royalties, affordability, cash reserves, contract lengths, label overhead, distribution deals, or label closure rules.
- Do not tune release cooldowns, quotas, release growth, chart weights, record retirement, Album scheduling, format priors/noise, substitution, or cannibalization.
- Do not tune genre keyframes, emergence years, supply weights, adjacency, momentum, regional acceptance, specialist texture, or historical demand.
- Do not rewrite an artist's native identity merely because one project uses another genre.
- Do not activate member replacement, reunion, solo spin-offs, or the naming overhaul under this directive.
- Do not accept headline unit or revenue health while population chronology, ownership, or release/project capacity fails.

Outcome metrics are guardrails against unintended regression. They are not objectives to optimize and cannot justify broad economic or release tuning.

## 15. Audit deliverables

Create `SimTools/ArtistPopulationLifecycleAudit.md` containing:

1. the exact code-path and event-order map;
2. toggle, prewarm, CLI, and RNG-stream behavior;
3. the initial-cohort snapshot and formation-year proof;
4. contract-cycle and structured-departure schema;
5. ownership, roster, pool, lifecycle, and project invariants;
6. formation accumulator math and exact yearly counts;
7. era-aware genre-selection inputs and availability proof;
8. native artist identity versus project identity examples;
9. inactivity, retirement, and disbandment transition maps;
10. all fixed-probe results;
11. disabled hash comparisons across all 45 frozen streams;
12. 52-week and 104-week commands, outputs, and per-year/tier results;
13. every one-variable candidate, including failed settings;
14. the 520-week cohort, formed-year, age, genre, signing, and exit distributions;
15. release, project, format, economic, distance, finance, specialist, and memory guardrail results;
16. deterministic-repeat and holdout hashes;
17. known limitations, including deferred member replacement, comeback, and persistent artist genre transition; and
18. final constants, toggle state, output locations, and completion recommendation.

Do not append this work as an incidental paragraph to `GenreMarketV2Audit.md`. The artist population phase needs its own auditable artifact. The genre audit may link to the completed population audit after acceptance.

## 16. Completion and resumption condition

Directive 6 is complete when contract probation uses current-contract evidence; performance-drop cooldown prevents immediate recycling while preserving mature second chances; structured departure reasons replace decision-making strings; meaningful calendar cohorts form every year; runtime formation years and native genres are era-correct; every entrant passes through ordinary unsigned/scouting/signing paths; inactivity, retirement, and disbandment create real exits; native and transitioned project identities are explicit; ownership and terminal eligibility reconcile; disabled mode remains byte-exact; enabled mode repeats deterministically; fixed, 52-week, 104-week, three-seed decade, and one fresh holdout gates pass without post-holdout tuning; and `ArtistPopulationLifecycleAudit.md` is complete.

Only then may late-decade genre work resume. That later work must consume the accepted population system rather than compensating for its absence with project reassignment, release tuning, or historical outcome targeting.
