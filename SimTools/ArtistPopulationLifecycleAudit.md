# Artist Population & Lifecycle Audit

## Current implementation scope

Directive 6 is isolated behind `artistPopulationLifecycleEnabled`, which defaults to false. The command-line switches are `--enable-artist-population-lifecycle` and `--disable-artist-population-lifecycle`; supplying both is rejected. Enabling the switch also requires Genre Market V2. Disabled runs do not create population streams or execute formation/lifecycle work.

The authoritative live sequence is owned by `ChartManager.OnWeekEnded`: chart callbacks complete, records are culled, then `ArtistManager.AdvancePopulationLifecycle` reconciles ownership, applies safe exits, materializes the weekly formation accumulator, and reconciles again. `RosterManager` then performs its normal vacancy-responsive scouting path. Prewarm does not call this sequence.

## Population model

- Initial generation remains the existing 3,000-artist path with its legacy backdated `formedYear` rolls.
- Runtime formation uses an accumulator of `300 / 52` artists per live week and retains the remainder. Runtime artists have cohort `RuntimeFormation`, `formedYear == current year`, immutable formation primary/secondary genres, no owner, and one unsigned-pool entry.
- Runtime attributes, regions, members, types, genre rolls, and generated fallback names use a dedicated `artist-population-v1`-derived `RandomNumberGenerator`, seeded from the audit seed. It is never constructed or used while the toggle is off.
- Runtime primary and secondary identities are selected from `GenreSupplyService.GetAvailableGenres(year)` and the existing prospective supply weights/concentration brake. Artist identity is stored separately from a record's project genre.

## Contracts, ownership, and exits

`SimulatedArtist` now has contract-cycle counters and lifecycle status distinct from `CareerState`. Free-agent signing resets only contract counters; lifetime hits, releases, sales, stature, and history are retained. New-signing progression uses contract Top-40 and flop evidence when the toggle is active. A structured `ArtistDropReason` records departures. The current, unaccepted one-variable candidate uses the directive-authorized 13-week performance cooldown; other departure types receive no performance cooldown.

Unowned active artists track continuous weeks. At 78 weeks, artists without a live chart callback or pending album project become inactive. After another 52 inactive weeks, groups disband; eligible solo artists (lead age 35+) retire. Terminal/inactive artists are removed from roster/pool membership while preserved in the registry and public-profile path.

## Outputs

When enabled, `ChartAuditRunner` writes these additional streams under `SimLogs`:

- `*-artist-population-events.csv`
- `*-artist-population-weekly.csv`
- `*-artist-cohort-annual.csv`
- `*-artist-project-identity.csv`

The streams record formation/contract/lifecycle transitions, population reconciliation counts, cohort distribution, and native artist identity versus release project identity. No frozen CSV header or row is changed when the toggle is disabled.

## Verification and gate ledger

### Pre-Gate-A repair and fixed probes (2026-07-13)

The runtime-formation scope was corrected so primary genre, secondary genre, artist type, members, attributes, region, and name generation all execute while `generatingRuntimePopulation` routes random calls to the dedicated stream.

`dotnet build "Label Man.sln" --no-restore` and `git diff --check` both pass. The build retains only the pre-existing unused `ChartManager.OnGenreMomentumChanged` warning.

The fixed suites were executed with:

```powershell
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=1 --run=d6-fixed-probes-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --genre-market-v2-probes --artist-population-lifecycle-probes
```

Both suites passed: the accepted D5 suite emitted its two pass lines and the separate D6 suite emitted `D6 fixed probes 1-21 passed`. This is a probe harness only, not a Gate B population measurement.

### Gate A - disabled replay: **PASS after command correction**

The first attempted replay was not a valid comparison. It used `--enable-genre-market-v2` while comparing against `d6-fulfillment-emerging-memory-52b-control-1001`, which is the Genre Market V2-disabled member of the accepted pair. That explains the original 7/45 match pattern and the additional `roster-lifecycle.csv`: the candidate and baseline were in different feature modes.

A dual-disabled retry without `--aggregate-only` matched 44/45 streams. Its only difference was `records.csv`: the accepted control is header-only while the retry contained record rows, proving that the accepted control also used aggregate-only instrumentation.

The corrected replay used matching feature and instrumentation boundaries:

```powershell
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=52 --run=d6-population-gatea-disabled-corrected-aggregate-1001 --seed=1001 --disable-genre-market-v2 --disable-artist-population-lifecycle --aggregate-only
```

It completed with exit code 0 and matched all **45/45** accepted CSV hashes, with no missing or extra files and zero artist-population streams. The known non-fatal `MissingSingletonsTemp.cs` autoload diagnostic remained unchanged.

### Gate B - enabled 52 weeks: **PASS**

```powershell
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=52 --run=d6-population-gateb-enabled-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle
```

The run formed exactly 300 runtime artists. Every formation had cohort `RuntimeFormation`, `formedYear == 1960`, an available 1960 new-supply genre, and no label assignment. The roster stream recorded 278 first-time signings; the event stream recorded 280 runtime-formation signing events. No runtime project preceded its artist's signing. Ownership conflicts, duplicate roster/pool membership, terminal roster members, terminal release/format decisions, recent performance re-signings younger than 26 weeks, and reported premature probation drops were zero.

| 1960 metric | Control | Enabled | Ratio | Gate |
|---|---:|---:|---:|---|
| Successful releases | 4,313 | 4,118 | 0.9548 | Pass |
| Scheduled Album projects | 1,090 | 1,228 | 1.1266 | Pass |
| Total units | 144,005,385 | 147,142,837 | 1.0218 | Pass |
| Gross | 132,318,374.41 | 136,701,479.22 | 1.0331 | Pass |
| Label net | 71,768,752.61 | 74,574,683.70 | 1.0391 | Pass |
| Market net | 71,849,159.84 | 74,659,992.22 | 1.0391 | Pass |

### Gate C - enabled 104 weeks: **FAIL / hard stop**

```powershell
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=104 --run=d6-population-gatec-enabled-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle
```

The candidate was compared with `d6-roster-lifecycle-telemetry-104-control-1001`. It formed 600 runtime artists: 305 dated 1960 and 295 dated 1961 because the 104 observed chart weeks cross a 53-Friday 1960 boundary. Both calendar cohorts are present with correct formed years. First-time signings were nonzero in both years (278 and 206). There were 959 matured performance re-signings, zero performance re-signings younger than 26 weeks, 20 inactivity transitions, zero ownership/duplicate/terminal-decision violations, and 27 separately permitted `Declining` unexpected-state format fallbacks.

The 1961 economics remain inside `[0.90,1.10]`, but both capacity guardrails fail:

| 1961 metric | Control | Enabled | Ratio | Gate |
|---|---:|---:|---:|---|
| Successful releases | 4,810 | 3,328 | **0.6919** | **Fail** |
| Scheduled Album projects | 1,600 | 1,271 | **0.7944** | **Fail** |
| Total units | 166,698,428 | 161,166,876 | 0.9668 | Pass |
| Gross | 162,871,255.76 | 160,536,758.28 | 0.9857 | Pass |
| Label net | 89,388,519.07 | 89,266,485.23 | 0.9986 | Pass |
| Market net | 89,786,645.10 | 89,506,049.98 | 0.9969 | Pass |

The failure is accompanied by severe roster churn. In 1961 the roster stream reports 1,904 drops, 206 first-time signings, 1,203 re-signings, 266 short-window re-drops, a final roster of 1,904, and a free-agent pool of 1,676. Release selection itself did not fail (`0` artist-selection failures); the binding problem is lost roster/release opportunity.

There is also an independent probation-contract violation. Of 413 performance drops occurring after a prior re-signing, **327** had fewer than two current-contract consecutive flops. `AILabel.ShouldDropArtist` still tests lifetime `consecutiveFlops` for `NewSigning` artists during monthly review, bypassing the new contract-cycle counters. The fixed probes exercise `SimulatedArtist.UpdateCareerState`, but do not cover this monthly review seam.

The aggregate artist-population rows currently under-report flow: the `All` row uses a default roster flow, so its first-time/re-signing columns remain zero, and `terminalReleaseEligible` is blank. Gate calculations above therefore use `roster-lifecycle.csv`, population events, format decisions, and release strategy rows. This telemetry defect should be repaired alongside the probation seam before another checkpoint.

| Gate | Result | Evidence |
|---|---|---|
| Fixed probes | Pass | D5 and D6 suites passed in `d6-fixed-probes-1001` |
| Gate A build/diff | Pass | Build succeeds; `git diff --check` clean |
| Gate A disabled 45-stream hash | Pass | 45/45 matching, no missing/extra files, 0 population streams |
| Gate B enabled 52 weeks | Pass | Formation, invariant, capacity, and economic gates pass |
| Gate C enabled 104 weeks | **Fail** | 1961 releases 0.6919x; Albums 0.7944x; 327/413 re-drops lack contract evidence |
| Gate D independent enabled repeat | Not run | Blocked by Gate C |

No Gate D repeat, second seed, holdout, or 520-week replay was run or authorized after the Gate C failure.

### Gate-C hard-stop continuation (2026-07-13)

The first repair corrected two independent implementation defects before testing another scalar:

- `AILabel.ShouldDropArtist` no longer lets the legacy monthly lifetime-flop review bypass current-contract probation for `NewSigning` artists.
- The aggregate `All` artist-population row now combines the real per-tier roster flow. All 28 weekly population columns are populated, including performance/other departures, recent performance re-signings, premature probation drops, candidate failure categories, and terminal eligibility.

Fixed probe 22 covers the monthly probation seam. The repaired `C = 26` candidate, `d6-gatec-repair-gatec-enabled-1001`, eliminated the contract-evidence violation: all 145 post-re-sign performance drops contained at least two current-contract consecutive flops and none lacked evidence. It still failed 1961 capacity with 3,495/4,810 successful releases (`0.7266x`) and 1,293/1,600 scheduled Album projects (`0.8081x`). Release selection failures remained zero, so this was an upstream roster-capacity failure rather than a release-selection mismatch.

Directive 6 permits only `C = 13` or `C = 52` as later one-variable cooldown candidates. `C = 13` was selected because `C = 26` left a large eligible/cooldown-separated dropped pool and the longer `C = 52` candidate would withhold second chances for longer. Formation, inactivity, exit, scouting, scoring, affordability, drop thresholds, release rules, finance, format behavior, and genre constants remained frozen.

Two additional state repairs were identified while tracing the C=13 result:

- Each live record now captures the artist contract sequence at release. Top-40 and completed-chart-run callbacks still update lifetime history, but only a record released under the current contract can update the reset probation counters. Fixed probe 23 covers stale prior-contract hit/flop isolation.
- Label bankruptcy now uses the atomic structured `LabelClosure` transition. Closure artists no longer retain an earlier `Performance` reason/cooldown, and closure departures are included in population flow telemetry. This changed classification only in the 52-week checkpoint (170 closure departures); its release, Album, and economic results were unchanged.

The final code was validated with the Downloads Godot console executable and the following runs:

```powershell
dotnet build "Label Man.sln" --no-restore
git diff --check

& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=1 --run=d6-c13-closure-fixed-probes-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --genre-market-v2-probes --artist-population-lifecycle-probes

& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=52 --run=d6-c13-closure-gatea-disabled-1001 --seed=1001 --disable-genre-market-v2 --disable-artist-population-lifecycle --aggregate-only

& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=52 --run=d6-c13-closure-gateb-enabled-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle

& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=104 --run=d6-c13-closure-gatec-enabled-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle
```

Build and `git diff --check` pass. The accepted D5 probes pass and D6 fixed probes 1-23 pass. The disabled replay matches all **45/45** frozen SHA-256 streams, has no missing/extra frozen stream, and emits no population CSV.

The final 52-week Gate B passes: 300 runtime formations, 276 roster first-time signings, 173 matured performance re-signings with minimum drop age 13, and zero recent re-signing, premature probation, ownership, duplicate-membership, or terminal-eligibility violations.

| 1960 Gate-B metric | Control | C=13 final | Ratio | Gate |
|---|---:|---:|---:|---|
| Successful releases | 4,313 | 4,225 | 0.9796 | Pass |
| Scheduled Album projects | 1,090 | 1,252 | 1.1486 | Pass |
| Total units | - | - | 1.0238 | Pass |
| Gross | - | - | 1.0344 | Pass |
| Label net | - | - | 1.0402 | Pass |
| Market net | - | - | 1.0396 | Pass |

The final 104-week Gate C passes every contract, cooldown, formation, exit, chronology, pool-separation, ownership, terminal, Album-capacity, and economic condition:

- exactly 600 formations, split 305 with `formedYear = 1960` and 295 with `formedYear = 1961`;
- nonzero first-time roster signings in both years (276 and 168);
- 1,247 matured performance re-signings, minimum age 13, and zero younger-than-C re-signings;
- 39 inactivity transitions;
- nonzero never-signed, eligible-dropped, and cooldown-blocked populations;
- 168 post-re-sign performance drops, all with valid current-contract evidence;
- zero premature probation drops, ownership conflicts, duplicate roster/pool entries, terminal roster members, terminal release eligibility, and artist-selection failures; and
- zero terminal-state format fallbacks. The 31 known `Declining -> New/Unsigned` fallbacks are reported separately as permitted by the directive.

It still fails the 1961 successful-release capacity gate:

| Year | Metric | Control | C=13 final | Ratio | Gate |
|---:|---|---:|---:|---:|---|
| 1960 | Successful releases | 4,313 | 4,225 | 0.9796 | Pass |
| 1960 | Scheduled Album projects | 1,090 | 1,252 | 1.1486 | Pass |
| 1961 | Successful releases | 4,810 | 3,644 | **0.7576** | **Fail** |
| 1961 | Scheduled Album projects | 1,600 | 1,403 | 0.8769 | Pass |

| Year | Units ratio | Gross ratio | Label-net ratio | Market-net ratio | Gate |
|---:|---:|---:|---:|---:|---|
| 1960 | 1.0238 | 1.0344 | 1.0402 | 1.0396 | Pass |
| 1961 | 1.0288 | 1.0450 | 1.0486 | 1.0480 | Pass |

The sequential C=13 diagnostics were retained rather than overwritten:

| Run | 1961 releases | Release ratio | 1961 Albums | Album ratio | Post-re-sign performance drops lacking evidence |
|---|---:|---:|---:|---:|---:|
| `d6-c13-final-gatec-enabled-1001` | 3,626 | 0.7538 | 1,405 | 0.8781 | 0/252 |
| `d6-c13-contract-provenance-gatec-enabled-1001` | 3,644 | 0.7576 | 1,403 | 0.8769 | 0/168 |
| `d6-c13-closure-gatec-enabled-1001` | 3,644 | 0.7576 | 1,403 | 0.8769 | 0/168 |

The provenance repair removed false new-contract evidence and the closure repair restored structured departure semantics, but neither can supply the roughly 445 additional 1961 releases needed to reach the `0.85` floor. The remaining deficit is driven by upstream roster/release opportunity: there are zero artist-selection failures, and the directive explicitly forbids increasing scouting frequency or multiplier, attempts per label, candidate scores, weakening drop thresholds, or tuning release, finance, format, and genre surfaces to force the guardrail.

| Continuation gate | Result | Evidence |
|---|---|---|
| Final build/diff | Pass | Build succeeds; `git diff --check` clean |
| Final fixed probes | Pass | D5 accepted suites and D6 probes 1-23 pass |
| Gate A | Pass | 45/45 frozen hashes; no population stream |
| Gate B | Pass | All 52-week lifecycle, capacity, format, and economic conditions pass |
| Gate C | **Fail / hard stop** | 1961 successful releases `0.7576x` control |
| Gate D independent repeat | **Not run** | Blocked by Gate C under Directive 6 |
| 520-week replay | **Not run** | Explicit user stop point and Gate-C hard stop |

No Gate D repeat or 520-week process was launched.

## Vacancy-responsive scouting capacity amendment — 0.20 candidate (2026-07-13)

The final `C = 13` Gate-C failure was re-examined under the narrowly authorized Directive 6 capacity amendment. The only behavioral candidate is the enabled vacancy-responsive scouting multiplier in `AILabel.ShouldScoutNewArtist`, raised from `0.15` to `0.20`. The disabled route explicitly retains `0.15`; attempts per passing label, candidate scoring, affordability, drops, formation, release selection/capacity, format, finance, and every other economic rule remain unchanged.

The first implementation applied `0.20` to the shared helper and made the disabled replay differ in 33 of 45 streams. It was rejected before any enabled measurement. The final candidate branches on the live Genre Market V2 path, and the repeated disabled replay is again byte-identical.

```powershell
dotnet build "Label Man.sln" --no-restore
git diff --check

& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=1 --run=d6-scout020-fixed-probes-r2-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --genre-market-v2-probes --artist-population-lifecycle-probes

& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=52 --run=d6-scout020-gatea-disabled-r2-1001 --seed=1001 --disable-genre-market-v2 --disable-artist-population-lifecycle --aggregate-only

& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=52 --run=d6-scout020-gateb-enabled-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle

& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=104 --run=d6-scout020-gatec-enabled-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle
```

- Build passes with only the existing unused `ChartManager.OnGenreMomentumChanged` warning; `git diff --check` passes.
- The accepted D5 suite and D6 fixed probes 1–23 pass.
- Gate A passes: all 45 disabled CSV streams match `d6-fulfillment-emerging-memory-52b-control-1001` byte-for-byte, with no missing or extra stream.
- Gate B passes: 1960 successful releases are `4,110 / 4,313 = 0.9529x`; scheduled Albums are `1,223 / 1,090 = 1.1220x`, below the `1.15` ceiling. Units, gross, label net, and market net are `1.0380x`, `1.0466x`, `1.0552x`, and `1.0555x`. Ownership, duplicate-pool, terminal-release, premature-probation, and artist-selection violations remain zero.
- Gate C fails the retained release-capacity guard in 1961: successful releases are `3,803 / 4,810 = 0.7906x`, below `0.85`. Albums pass at `1,531 / 1,600 = 0.9569x`; units, gross, label net, and market net are `0.9951x`, `1.0094x`, `1.0132x`, and `1.0121x`.

The scalar improves the failed 1961 release result from `0.7576x` to `0.7906x`, but does not clear the guardrail. The new roster-flow evidence still points to a replenishment gap rather than release selection: 1961 has `2,066` drops, `197` first-time signings, `1,536` re-signings, `1,966` scouting-gate passes, `1,753` signing attempts, `213` score rejections, `20` affordability rejections, and zero release artist-selection failures. The roster ends at `2,589` with `993` free agents. An average of `127.7` labels has an empty roster each week (peak `187`), and every 1961 week has at least one scouting pass and signing attempt. The current telemetry does not record label-level vacancy start/end times or unused roster-slot capacity, so it cannot establish an exact vacancy-duration or unfilled-slot distribution without a separately authorized observational telemetry addition.

**Stop:** do not advance automatically to `0.25`. Gate D and every 520-week replay remain unrun and unauthorized. Any future scalar request must first resolve the vacancy-duration and unfilled-slot observability gap, then distinguish persistent vacancy from capacity that is intentionally unfilled.

## Deferred work

Member replacement, reunions, reactivation/comeback, solo spin-offs, and persistent artist genre transitions remain out of scope. The existing naming service is retained for initial artists; runtime formation uses deterministic local fallback names to protect the dedicated RNG boundary until the naming service exposes an injectable RNG seam.

## Scouting vacancy observability amendment (2026-07-13)

### Implementation map

- `Data/AILabel.cs` now exposes the existing scouting-gate calculation as a structured evaluation. The production path takes the same single `GD.RandRange` roll at the same branch; full-roster and estimated-advance failures take no roll. Candidate evaluation now returns the same winning candidate together with its already-calculated best score.
- `Systems/RosterManager.cs` records enabled-only, telemetry-owned label observations and vacancy/empty-roster ages keyed by `labelId`. It does not add gameplay state, enumerate candidates before a passed gate, or add random draws. The initial chart capture is an explicit no-RNG snapshot because it precedes the first Friday scouting tick.
- `SimTools/ChartAuditRunner.cs` writes `*-label-scouting-vacancy-weekly.csv` only when both Artist Population Lifecycle and Genre Market V2 are enabled. Its roster/age snapshot is finalized at the audit capture boundary, after live-tick reconciliation, so its end-of-week capacity fields reconcile to `roster-lifecycle.csv`.
- `SimTools/ArtistPopulationLifecycleProbeSuite.cs` adds telemetry probes 24-29: no-roll early branches, one-roll probability capture, no-candidate boundary, best-score/structured signing outcomes, age increment/reset, and telemetry RNG neutrality. D6 probes 1-23 remain unchanged.

The new CSV retains the required fields and adds `scoutingRosterSize`, `scoutingUnusedRosterSlots`, and `scoutingIsEmptyRoster` to distinguish the decision snapshot from the end-of-week roster snapshot. A blank roll/failure on the initial snapshot means no scouting tick occurred; it is not a new behavioral branch.

### Validation

`dotnet build "Label Man.sln" --no-restore` passes with only the existing unused `ChartManager.OnGenreMomentumChanged` warning. `git diff --check` passes. The final fixed command emitted the accepted D5 lines and `D6 fixed probes 1-29 passed (contract/cooldown/formation/identity/lifecycle/scouting telemetry)`.

| Gate | Result | Evidence |
|---|---|---|
| O1 build, diff, probes | Pass | Build passes; D5 and D6 probes 1-29 pass |
| O2 disabled 52-week replay | Pass | `d6-scoutvac-gatea-disabled-1001`: 45/45 frozen streams byte-identical to `d6-fulfillment-emerging-memory-52b-control-1001`; no population or vacancy CSV |
| O3 enabled 104-week observational replay | Pass | `d6-scoutvac-o3-enabled-1001`: all 50 pre-existing streams byte-identical to `d6-scout020-gatec-enabled-1001`; the sole extra stream is `label-scouting-vacancy-weekly.csv` |
| O3 row/reconciliation checks | Pass | 65,021 label-week rows, exactly one for each label present in each capture; all 104 weekly aggregate comparisons reconcile for empty rosters, gate passes, attempts, candidate rejections (`CandidateScore + NoEligibleCandidate`), affordability, first-time signings, and free-agent signings |

The replay is observational only. Its release, Album, economic, formation, probation, cooldown, ownership, terminal, and roster-flow streams remain the rejected `0.20` candidate's byte-identical outputs; it does not reopen Gate C.

### Vacancy and unused-capacity analysis

`unusedRosterSlots` totals 504,923 slot-weeks across 63,666 vacant label-weeks. This is hard-cap capacity, not evidence that every label targets a full roster.

| Tier | Unused slot-weeks | Vacant label-weeks | Empty label-weeks | Vacancy age p50/p75/p90/p95/max | Empty age p50/p90/p95/max |
|---|---:|---:|---:|---|---|
| All | 504,923 | 63,666 | 15,453 | 49 / 76 / 93 / 99 / 104 | 29 / 68 / 83 / 104 |
| Major | 19,473 | 954 | 187 | 48 / 75 / 93 / 99 / 104 | 24 / 52 / 61 / 70 |
| MidTier | 192,254 | 11,525 | 2,923 | 54 / 79 / 94 / 99 / 104 | 31 / 69 / 86 / 104 |
| Independent | 129,697 | 14,533 | 3,051 | 47 / 76 / 93 / 99 / 104 | 24 / 58 / 67 / 104 |
| Small | 108,955 | 25,439 | 6,838 | 47 / 75 / 93 / 98 / 104 | 30 / 73 / 87 / 104 |
| Boutique | 54,544 | 11,215 | 2,454 | 49 / 76 / 93 / 99 / 104 | 28 / 63 / 81 / 104 |

| Minimum consecutive vacant weeks | Label-weeks | Share of vacant label-weeks | Slot-weeks | Share of vacant slot-weeks |
|---:|---:|---:|---:|---:|
| 4 | 61,505 | 96.61% | 490,887 | 97.22% |
| 8 | 58,665 | 92.14% | 472,368 | 93.55% |
| 13 | 55,176 | 86.66% | 449,567 | 89.04% |
| 26 | 46,522 | 73.07% | 390,851 | 77.41% |
| 52 | 30,264 | 47.54% | 263,887 | 52.26% |

Of 721 vacancy episodes, 536 reached a later successful signing; their first-vacancy-to-signing wait is p50 45 weeks, p90 67, max 102. Of 375 empty-roster episodes, only 10 reached a later successful signing; their wait is p50 0, p90 56, max 60, while 365 remained unresolved in the observed window. These are observed outcomes, not proof that each label intends to fill every remaining hard-cap slot.

### Scouting and failure taxonomy

There are 55,462 actual scouting-roll label-weeks (the remaining 9,559 rows are full-roster, estimated-unaffordable, or the initial no-tick snapshot). The probability sum predicts 4,110.38 passes; 4,135 occurred (7.46%), which is consistent with the existing roll.

The vacant-label probability distribution is p50 0.067634, p75 0.099879, p90 0.130875, p95 0.149062, max 0.200555. Recent-hit labels pass 4.31% at mean predicted probability 4.28%, versus 7.81%/7.76% without a recent hit. By decision-snapshot fullness, observed versus mean predicted pass rate is 11.78%/11.87% below 25% full, 7.44%/7.42% at 25-50%, 5.06%/4.88% at 50-75%, and 2.56%/2.65% at 75%+.

| Outcome | Count | Share of actual scouting rolls |
|---|---:|---:|
| `ScoutingRandomGate` | 51,327 | 92.54% |
| `CandidateScore` | 1,797 | 3.24% |
| `SignedFreeAgent` | 1,779 | 3.21% |
| `SignedFirstTime` | 472 | 0.85% |
| `NoEligibleCandidate` | 66 | 0.12% |
| `ActualAdvanceUnaffordable` | 21 | 0.04% |

No estimated-advance failure occurred. By tier, gate passes are Major 56, MidTier 767, Independent 1,085, Small 1,487, and Boutique 740; score rejections are 14, 291, 425, 732, and 335 respectively. Candidate-score telemetry records only a structured score, not candidate genre/supply provenance, so no genre/new-supply concentration claim is warranted from this stream alone.

181 distinct labels were empty on 989 passed-gate weeks that had at least one eligible candidate and an affordable estimated advance. Those outcomes are predominantly `CandidateScore` (970), with 10 `SignedFreeAgent` and 9 actual-advance affordability failures. Separately, 492 labels passed scouting while still under capacity at least four times (3,874 such label-weeks; maximum 20 passes for one label). This supports measuring a roster-target concept before equating hard-cap vacancies with desired hiring.

### Next-surface recommendation

**Decision rule A — random scouting gate is binding.** Long vacancy and unused-slot exposure coincides overwhelmingly with `ScoutingRandomGate` (92.54% of actual scouting-roll label-weeks); no eligible candidates (0.12%) and affordability (0.04%) are secondary. Candidate scoring is material after a pass but does not explain the aggregate cadence gap.

Request explicit authorization for a separate vacancy-response policy amendment. The next comparison should evaluate vacancy-age urgency or a bounded scouting-cadence floor, without changing `scoutingAbility`, formation, candidate scores, affordability, or the `0.20` multiplier under this directive. Gate C remains failed and Directive 6 remains incomplete.
