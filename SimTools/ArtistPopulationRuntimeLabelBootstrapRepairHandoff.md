# Artist population runtime-label bootstrap repair handoff

## Mission and authority

Repair the enabled lifecycle's runtime label-birth path so newly founded labels enter the ordinary one-artist bootstrap and weekly talent-service market instead of executing the launch-only bulk roster initializer.

This is the authoritative S2a continuation after the rotating reserve-participation S2 stop recorded in `d6-supply-policy-s2-report.md`. It supplements `ArtistPopulationSupplyPolicyRedesignHandoff.md`; all supply-policy state, equations, constants, closed surfaces, and later validation gates remain in force unless this document explicitly changes the immediate stop decision.

Preserve the current uncommitted supply-policy implementation, explicit reserve cohort, Latent/Seeking state, 78-week rotation, deterministic activation order, enabled-only labor-market telemetry, D6 probe 55, analyzer, audit history, diagnostic reserve suppression seam, and systemic label-capacity repair. Do not reset or discard them.

This handoff authorizes:

1. one direct behavior-defect repair at runtime label creation;
2. the corresponding signing-classification and telemetry-reconciliation repairs;
3. narrowly scoped fixed probes;
4. build, diff, and complete D5/D6 probe verification; and
5. one fresh paired 52-week seed-1001 S2 rerun.

It does **not** authorize a reserve, participation, scouting, lifecycle, format, Album, release, finance, market, genre, or acceptance-band calibration. Stop again if the repaired S2 does not pass.

## Current S2 result

The rotating reserve-participation candidate passes the economic, unit, release, and structural surfaces reported by the initial S2 review, but fails scheduled Album capacity by a narrow deterministic margin:

| Measure | Control | Treatment | Ratio | Gate |
|---|---:|---:|---:|---|
| Successful releases | 4,313 | 4,236 | 0.9821 | Pass |
| Scheduled Albums | 1,090 | 1,260 | 1.1560 | **Fail** |
| Units | 144,005,385 | 148,345,071 | 1.0301 | Pass |
| Gross | 132,318,374.61 | 136,782,198.32 | 1.0337 | Pass |
| Label net | — | — | approximately 1.04 | Pass |
| Market net | — | — | approximately 1.04 | Pass |

The integer Album ceiling is 1,253 projects at a strict `1.15` ratio. Treatment is seven projects above it. Do not widen or reinterpret the gate.

The treatment has 4,236 total format decisions, including 1,260 Album choices. Album pooled appeal and released-Album yield are not the primary failure: enabled mean Standard pooled appeal remains approximately `0.58`, Compilation approximately `0.52`, and total economics remain in band. The excess is scheduled-project count.

## Demonstrated defect

### Runtime label births bypass the live talent-service contract

`LabelLifecycleManager.SpawnNewLabel` currently calls `RosterManager.InitializeRosterForLabel` before registering the new label. `InitializeRosterForLabel` calls the same `PopulateInitialRoster` method used for the original launch population.

`PopulateInitialRoster`:

- rolls a tier fill ratio;
- loops to a multi-artist target size;
- uses the launch candidate selector;
- calls `InitialSignArtist` repeatedly in one label-week; and
- sets the operating target from the resulting bulk roster.

That behavior is correct only for the frozen initial 600-label launch. It is incorrect for a label born during the live simulation because it bypasses:

- the one-label/one-evaluation/one-signing-per-week rule;
- `V_t`, the weekly affordable hiring-opportunity count;
- Latent/Seeking reserve exposure;
- the Normal/Watch/Recovery service state;
- fresh versus experienced discovery lanes;
- ordinary score thresholds and recovery fallback;
- normal advance calculation and affordability telemetry; and
- the intended one-artist empty-label bootstrap.

The S2 artifacts prove the branch is live. Eight generated labels received 35 immediate signings:

| Runtime label | Week | Immediate signings |
|---|---:|---:|
| `gen_swanstudios_6789` | 18 | 1 |
| `gen_libertystudios_5131` | 31 | 3 |
| `gen_iron_5613` | 40 | 3 |
| `gen_bell_7679` | 40 | 4 |
| `gen_royal_1247` | 44 | 7 |
| `gen_minit_3369` | 44 | 7 |
| `gen_reprise-velvet_3948` | 48 | 3 |
| `gen_cincinnatimusic_6162` | 48 | 7 |

This is not a request to reduce signings until Albums pass. It is a direct violation of the specified market-service topology.

### The launch helper launders prior-contract state

Of the 35 runtime-label birth signings:

- 33 entered with a prior contract and finished with `contractSequence == 2`;
- most followed a prior performance departure;
- two were genuine first contracts; and
- all 35 were emitted as `signing` rather than the 33 prior-contract artists being identified as re-signings.

`InitialSignArtist` sets `careerState = NewSigning` before calling `ArtistManager.SignArtist`. `ArtistManager.SignArtist` currently determines `reSigning` from the already-overwritten `careerState == Dropped`. `ReconcileSignedArtist` similarly observes `droppedFreeAgent == false`, so the runtime birth path can:

- classify a prior-contract artist as a first signing;
- select first-contract probation rather than experienced-comeback evaluation;
- overwrite signed-year, contract, release-history, hit, momentum, or reputation fields through launch-only random initialization; and
- bypass the finite comeback/exhaustion topology.

This is an enabled-path behavior defect. Correct it independently of its effect on Album totals.

### Labor-market signing telemetry does not reconcile

The event ledger records 474 `signing` rows in S2, while `artist-labor-market-weekly.csv` sums to 439 first-time signings. The exact 35-row difference is the runtime-label bulk-bootstrap path, which calls `InitialSignArtist` without `RosterManager.RecordSigning`.

The weekly labor-market row is also captured before the current week's ordinary roster-scouting callback, so its signing-flow fields describe a prior callback interval while the row is labelled with the current week. The first row reports zero signings even though week-one events contain 80, and later rows show the same offset.

S2 requires weekly stock/flow reconciliation. A zero invariant count is not authoritative while this seam omits an entire signing path and mislabels the observation interval.

### Album relevance

Artists signed through the runtime-label bulk path directly scheduled six Album projects during S2. The numerical gate miss is seven projects. This makes the defect materially relevant, but it does not guarantee that the repaired run will pass: removing the branch will alter enabled-path roster, label, release, and RNG evolution.

Implement the correction because the path violates the model, not because six is close to seven.

## Required repair

### 1. Split launch initialization from runtime label initialization

Retain `InitializeAllRosters` and `PopulateInitialRoster` unchanged for the original launch labels. The initial 3,000-artist roster allocation and its global RNG order remain frozen.

Change `InitializeRosterForLabel` or replace it with an explicitly named runtime-label initializer that:

1. creates an empty roster;
2. sets `operatingRosterTarget = 1`, clamped to `maxRosterSize`;
3. records `operatingRosterTargetSource = OneArtistBootstrap`;
4. consumes no candidate-selection, signing, advance, career-history, or launch-population RNG;
5. does not call `PopulateInitialRoster` or `InitialSignArtist`; and
6. does not sign an artist during `SpawnNewLabel`.

Register the empty active label with the label, chart, and competitor owners normally. At the next authoritative weekly scouting boundary it must enter Recovery because it is empty and one artist below its operating target. It may then perform exactly one candidate evaluation and at most one signing attempt through `TrySignFromMarket`.

Do not special-case a guaranteed contract. Genre/region discovery, fresh/experienced lane selection, recovery fallback, actual affordability, and the available Seeking/free-agent supply remain real.

### 2. Preserve contract topology on every live signing

Make signing classification depend on authoritative pre-contract history, not a mutable `careerState` value after commercial terms have been applied.

At minimum, capture before reconciliation:

```text
priorContractSequence
priorCareerState
priorDropReason
priorProspectMarketStatus
wasDroppedFreeAgent
wasFirstContractProspect
```

Then require:

- `priorContractSequence == 0` and an eligible Seeking prospect for a first signing;
- `priorContractSequence > 0` or an authoritative dropped/free-agent transition for a re-signing;
- prior-contract artists retain the intended experienced-comeback policy and pre-drop career restoration;
- first-contract probation is never assigned merely because a helper overwrote `careerState`;
- label closure and voluntary departures retain their existing semantics;
- performance-drop counts and exhaustion remain unchanged; and
- the signing event type is derived from the same authoritative classification used by contract setup.

Prefer returning a structured signing-transition result from the atomic reconciliation seam over duplicating classification logic in `RosterManager`, `ArtistManager`, and telemetry.

Do not use this repair to change the existing first-contract or experienced-comeback thresholds.

### 3. Make labor-market telemetry exact and time-labelled correctly

The enabled labor-market stream must reconcile with the population event ledger and roster flow for the same explicit observation interval.

Choose one authoritative approach:

- buffer the weekly labor-market row until current-week roster scouting has completed; or
- maintain event-owned counters keyed by chart week and write the completed prior week with its actual week/date labels.

Do not relabel prior-week flow as current-week flow.

Required outcomes:

- weekly first-time signings equal unique first-contract signing events for that week;
- weekly repeat signings equal unique prior-contract re-signing events for that week;
- their annual sums equal the event ledger;
- label births cannot bypass those counters;
- the final simulated week is flushed rather than silently omitted;
- activation, expiry, signing, Seeking, Latent, roster, and registry stock/flow identities reconcile; and
- telemetry changes consume no gameplay RNG and do not alter behavior.

If correcting callback order would risk gameplay order, use buffered observational state. Do not reorder chart, lifecycle, release, or scouting behavior merely to simplify CSV writing.

## Fixed probes

Retain accepted D5 probes and D6 probes 1-55. Add deterministic production-helper coverage for at least:

1. original launch initialization still uses the unchanged multi-label launch population path;
2. runtime label initialization creates an empty roster with target one and source `OneArtistBootstrap`;
3. runtime initialization consumes no candidate, signing, advance, or launch-history RNG;
4. a newly registered empty active label enters Recovery at its next weekly scouting boundary;
5. that label performs at most one evaluation and one signing attempt in the week;
6. an unaffordable or truly unmatched new label remains empty without a guaranteed contract;
7. a successful runtime-label first contract uses the ordinary fresh-potential path;
8. a prior-contract artist uses the experienced path and cannot be rewritten as a first-contract `NewSigning`;
9. prior performance evidence, drop count, comeback policy, and exhaustion remain intact;
10. no live runtime path calls `InitialSignArtist`;
11. signing and re-signing event classification uses the same structured transition result as contract setup;
12. weekly labor-market flow has no one-week label offset;
13. first/repeat signing rows reconcile exactly with unique event rows, including the final week;
14. runtime label birth does not create a multi-signing burst;
15. the 7,000 registry, reserve cohort, Latent/Seeking activation equation, and 78-week rotation remain unchanged; and
16. disabled behavior, RNG order, 45-stream set, headers, and values remain frozen.

Number the expanded suite from probe 56 onward without renumbering accepted probes.

## Verification and S2a ladder

### A0 - reproduce the defect from retained artifacts

Before editing, record from `d6-supply-policy-enabled-52-1001`:

- 35 runtime-label birth signing events across eight generated labels;
- per-label burst sizes `1, 3, 3, 4, 7, 7, 3, 7`;
- 33 prior-contract and two first-contract artists;
- 474 event-ledger `signing` rows versus 439 labor-market first-time rows;
- the one-week ordinary flow offset;
- six scheduled Albums belonging to birth-path-signed artists; and
- 1,260 total scheduled Albums versus the 1,253 integer ceiling.

Resolve any discrepancy before implementation.

### A1 - source verification

Run:

```powershell
dotnet build "Label Man.sln" --no-restore
git diff --check

& '<Godot-console.exe>' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=1 --run=d6-runtime-label-bootstrap-probes-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --genre-market-v2-probes --artist-population-lifecycle-probes --lean-probe
```

Require:

- build success with no new warning;
- `git diff --check` success;
- accepted D5 probes pass;
- the complete D6 suite passes;
- a functional-source manifest is recorded; and
- no behavior change outside the enabled runtime-label/signing path.

### A2 - fresh paired S2 rerun

Run a fresh pair from the repaired source:

```powershell
& '<Godot-console.exe>' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=52 --run=d6-runtime-label-bootstrap-control-52-1001 --seed=1001 --disable-genre-market-v2 --disable-artist-population-lifecycle --aggregate-only

& '<Godot-console.exe>' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=52 --run=d6-runtime-label-bootstrap-enabled-52-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle
```

Require all original S2 conditions:

- successful releases and scheduled Albums each inside `[0.85,1.15]`;
- units, gross, label net, and market net each inside `[0.90,1.10]`;
- individual-format units inside `[0.85,1.15]`;
- exactly 300 runtime formations for the complete 1960 year;
- the default 7,000-artist launch registry and explicit 4,000 reserve cohort;
- nonzero reserve activation and first-time signing;
- exact weekly and annual labor-market/event reconciliation;
- runtime labels use empty one-artist bootstrap and never bulk sign;
- no prior-contract artist is assigned first-contract probation through runtime label creation; and
- zero ownership, duplicate, latent-searchable, terminal, chronology, closed-label, hard-cap, one-attempt, contract-topology, and release-selection violations.

Use the strict integer Album result. `1,253 / 1,090` passes; `1,254 / 1,090` fails.

### Stop and continuation decisions

If A2 passes, freeze the source and resume `ArtistPopulationSupplyPolicyRedesignHandoff.md` at S3 with no further change.

If A2 fails because of a direct implementation defect in the specified runtime-label or telemetry repair, one source-correctness iteration is allowed and requires restarting at A1.

If A2 is structurally correct but scheduled Albums remain above `1.15`, stop. Attribute the remaining excess by:

- initial versus runtime label;
- InitialLegacy, EnabledInitialReserve, and RuntimeFormation cohort;
- first-contract versus experienced contract;
- contract start week and project lag;
- label tier and service mode;
- career state and quality quartile; and
- scheduled versus released/pending project state.

Do not proceed to 104 weeks, run another seed, consume a holdout, change Album rules, reduce reserve activation, add a participation multiplier, lower formation, or widen the band without a new amendment.

## Closed surfaces

Do not change:

- `V_t`, `S_t`, `L_t`, or `A_t` and the one-seeker-per-affordable-opportunity rule;
- reserve size, explicit cohort, Latent/Seeking semantics, activation ordering, 78-week rotation, or annual formation;
- launch roster allocation, initial 600-label generation, or launch RNG order;
- scouting probability, service-mode thresholds, discovery slates, fresh/experienced scoring, recovery threshold fallback, or affordability;
- first-contract, experienced-comeback, cooldown, performance-drop, exhaustion, inactivity, retirement, or disbandment thresholds;
- release cadence, release priority, release eligibility, project scheduling, Album choice, Album threshold, promo strategy, hit inventory, reuse, freshness, or format tilt;
- demand, quality exponents, buyer pools, sales, awareness, price, finance, label lifecycle birth/death rates, or distribution deals;
- genre availability, supply weights, regional routing, distance, historical inputs, or seasonality;
- acceptance bands; or
- frozen disabled streams and RNG behavior.

## Completion record

Append the completed S2a checkpoint to `ArtistPopulationLifecycleAudit.md` with:

- exact source seams and the demonstrated defect;
- retained-artifact reproduction;
- before/after source manifests;
- probe and build commands/results;
- fresh control/treatment commands and completion markers;
- runtime label births, bootstrap targets, weekly evaluations, attempts, and signings;
- first/repeat signing reconciliation by week and year;
- prior-contract classification and contract-topology invariants;
- scheduled Album attribution by label origin, artist cohort, contract kind, tier, service mode, career state, and project status;
- every S2 capacity, format, economic, population, labor-market, and structural gate; and
- the exact stop or S3-continuation decision.

The intended correction is simple: launch labels may start with launch rosters; runtime labels must enter the live labor market like runtime labels.
