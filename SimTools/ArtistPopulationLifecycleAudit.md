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

## Operating-label vacancy response closure (2026-07-14)

The user subsequently authorized the changes needed to clear Gate C after the authorized 13-week, `0.10` floor produced only `3,806 / 4,810 = 0.7913x` 1961 successful releases.

### Corrected diagnosis

The vacancy analysis correctly established that the random gate dominates label-weeks that reach a scouting roll, but that denominator excluded early branches and historical label state. The failed `0.10` run exposed two additional facts:

- 1961 contained 4,492 empty-label weeks that stopped at `EstimatedAdvanceUnaffordable` before any scouting roll; and
- `ChartManager.GetAllLabels()` retains closed labels for history, while the enabled scouting loop treated every retained label as an operating label. Joining the scouting and finance streams attributed 466 1961 signing outcomes to label-weeks marked `Defunct`. Those artists could not create release opportunity because `CompetitorManager` correctly excludes non-operating labels from weekly releases.

The first repair therefore made the enabled scouting decision explicitly operating-label-only. Closed labels remain in the observational CSV with `failureReason = InactiveLabel`, consume no scouting RNG, cannot sign, and have their urgency state cleared. Fixed probe 33 covers `Defunct`, `Bankrupt`, and `Acquired` states. The disabled path is unchanged.

The operating-label guard is a correctness repair, not sufficient capacity by itself: its first 104-week diagnostic produced only 3,742 1961 releases. The bounded urgency response then required a stronger but still localized setting. Candidate results were:

| Candidate | 1961 releases | Gate-B Albums | 1961 Albums | Decision |
|---|---:|---:|---:|---|
| Active labels, week 13, floor `0.25` | 4,081 | 1,199 | 1,571 | Near pass; eight releases short |
| Active labels, week 13, floor `0.275` | 4,055 | 1,265 | 1,573 | Reject; Gate B Album ceiling exceeded |
| Active labels, week 12, floor `0.25` | **4,255** | **1,253** | **1,626** | Accept |

The accepted response preserves the existing `0.20` base multiplier. An operating label that remains under hard roster capacity for 12 consecutive weeks receives a `0.25` minimum probability on its existing single weekly scouting draw. A successful signing or a full roster resets the age. Candidate enumeration, score threshold and weights, actual affordability, advances, formation, release selection, release cadence, formats, and economics are unchanged.

### Frozen implementation

- `Systems/RosterManager.cs` sets `ScoutingUrgencyThresholdWeeks = 12` and `ScoutingUrgencyProbabilityFloor = 0.25f`, excludes every `!label.IsActive` state from enabled scouting decisions, retains RNG-neutral historical-label telemetry, and resets closed-label urgency state.
- `Data/AILabel.cs` continues to apply the supplied minimum only after the existing roster-space and estimated-affordability branches and uses exactly one existing scouting draw.
- `SimTools/ArtistPopulationLifecycleProbeSuite.cs` updates the threshold/floor probes and adds probe 33 for the closed-label boundary.

### Final validation

The frozen candidate used these final run names:

```text
d6-active-floor025-w12-final-probes-1001
d6-active-floor025-w12-final-disabled-1001
d6-active-floor025-w12-final-gateb-1001
d6-active-floor025-w12-final-gatec-1001
```

- `dotnet build "Label Man.sln" --no-restore` passes with only the pre-existing unused `ChartManager.OnGenreMomentumChanged` warning.
- `git diff --check` passes.
- The accepted D5 probe suites and D6 probes 1-33 pass.
- The disabled replay matches all **45/45** frozen `d6-fulfillment-emerging-memory-52b-control-1001` CSV streams by SHA-256, with no missing, changed, or extra stream.
- The independent frozen 104-week diagnostic and final acceptance replay match **51/51** enabled CSV streams byte-for-byte.

| Year | Successful releases | Control | Ratio | Scheduled Albums | Control | Ratio |
|---|---:|---:|---:|---:|---:|---:|
| 1960 | 4,154 | 4,313 | 0.9631 | 1,253 | 1,090 | 1.1495 |
| 1961 | 4,255 | 4,810 | 0.8846 | 1,626 | 1,600 | 1.0163 |

The 1960 Album result is inside the retained ceiling by one project (`1,253 <= 1,253.5`) and reproduced exactly in the fresh Gate B run.

| Year | Units ratio | Gross ratio | Label-net ratio | Market-net ratio |
|---|---:|---:|---:|---:|
| 1960 | 1.0244 | 1.0354 | 1.0418 | 1.0418 |
| 1961 | 1.0117 | 1.0288 | 1.0341 | 1.0322 |

Gate C also records:

- exactly 600 runtime formations, split 305 in 1960 and 295 in 1961;
- nonzero roster first-time signings in both years: 279 and 203;
- 1,393 matured performance re-signings, minimum performance-drop age 13 weeks;
- seven inactivity transitions;
- nonzero final never-signed unsigned, eligible-dropped, and cooldown-blocked populations: 104, 467, and 432;
- 249 performance re-drops after re-signing and zero without current-contract hit/flop evidence;
- zero premature-probation, ownership, duplicate-roster, duplicate-pool, terminal-roster, terminal-release-eligibility, artist-selection, and terminal-format violations.

**Result:** Gates A, B, and C pass. The byte-identical independent 104-week repeat also satisfies Gate D's deterministic-repeat condition. No 520-week, multi-seed decade, or holdout run was launched under this amendment.

## Phase-5 decade checkpoint — seed 1001 (2026-07-14): **FAIL / hard stop**

The frozen worktree remained at `64dda5ea204c44adb5809b99290bc4e6f95c6f90`; its only pre-existing edits were this audit and the untracked decade-validation handoff. `git diff --check` passed. `dotnet build "Label Man.sln" --no-restore` passed with only the pre-existing unused `ChartManager.OnGenreMomentumChanged` warning. No source, scene, data, or constant was changed between the paired runs.

Commands executed, control first:

```powershell
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=520 --run=d6-population-decade-control-1001 --seed=1001 --disable-genre-market-v2 --disable-artist-population-lifecycle --lean-probe --profile-performance
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=520 --run=d6-population-decade-enabled-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --lean-probe --profile-performance
```

Both child processes exited after writing week 520 (`1969`) and complete lean CSV families. The desktop runner's foreground wrapper has a two-minute capture limit, so its post-run console completion line and exit code were not retained by that wrapper; completion is evidenced by the complete families and their final week rows. Excluding the diagnostic-only performance stream, the control has 45 streams and the enabled treatment has 51. SHA-256 manifests (sorted `filename + hash` lines, then hashed) are respectively `465D600F893AE46219A973A32232919450D35B4EFBCB96238E05C7FBAB2C5685` and `FFA5114F68CD6AD9C018AC40CC7317BB8127120A4DDB8DE942ACBF8A9F8B8427`.

### Gate E population and chronology

The enabled run formed exactly 3,000 runtime artists and had no future-dated formation or primary native genre preceding its supply emergence. The required calendar-year distribution fails, however:

| Calendar year | Runtime formations |
|---|---:|
| 1960 | 305 |
| 1961 | 300 |
| 1962 | 300 |
| 1963 | 300 |
| 1964 | 300 |
| 1965 | 306 |
| 1966 | 300 |
| 1967 | 300 |
| 1968 | 300 |
| 1969 | 289 |

This violates the required `300 +/- 1` formations in each full calendar year. The first causal seam is the frozen `300 / 52` live-week accumulator: its formation cadence crosses the simulated calendar-year boundaries, producing five or six extra formations in 1960/1965 and leaving eleven for the first unmeasured 1970 weeks. This is a chronology/formation-cadence failure, not a release-selection correction opportunity.

Other observed population checks were healthy but do not waive that failure: final active population was 3,072 (`1.0240x` the initial 3,000); 2,101 were rostered; and inactive, retired, and disbanded populations were 995, 338, and 1,595. Maximum reported ownership, duplicate-roster, duplicate-pool, premature-probation, terminal-roster, and terminal-release-eligibility violations were all zero.

### Inherited release and market gates

The paired decade also independently fails the successful-release capacity gate: enabled successful releases total `46,036 / 57,751 = 0.7971x`, below the required `[0.85, 1.15]` range. Its annual release ratios are `0.9631`, `0.8846`, `0.7984`, `0.7540`, `0.7202`, `0.7084`, `0.7238`, `0.8095`, `0.8274`, and `0.8408` for 1960-1969. Almost all enabled rolls succeed once fired (only 37, 38, 19, 8, 3, and 2 failures in 1960-1965; none thereafter), so the deficient seam is the reduced volume of fired release rolls as rostered/release-eligible population contracts, not a cooldown or chosen-record failure.

Decade total market ratios remain within the individual-seed economic band: units `1.0493x`, gross `1.0561x`, label net `1.0459x`, and market net `1.0513x`. Format-unit ratios are Single `1.0457x` and Album `1.0760x`. These passing economic observations do not offset either the calendar-formation or release-capacity failure.

**Stop state:** Per Directive 6, do not run seeds 1002 or 1003, do not select or consume a holdout, and do not tune the frozen candidate. Preserve the complete seed-1001 artifacts and request a one-variable amendment that resolves the calendar-aligned formation requirement before any further measurement.

## Calendar formation and release-capacity repair (2026-07-14)

The user authorized the changes needed to resolve both seed-1001 decade failures, with an explicit stop before another 520-week run. No seed 1002, seed 1003, holdout, or additional 520-week process was launched.

### Calendar-aligned formation

`ArtistManager` now resets formation carry at a calendar-year boundary, caps each year at exactly 300 runtime artists, and assigns every formation from the live Friday's `GameDate`. Normal years retain the accepted `300 / 52` weekly cadence and finalize at the late-December short-checkpoint boundary; the terminal 1969 game year finalizes at December 12 because that is the last live Friday processed by the fixed 520-week checkpoint. Once the annual quota is reached, later Fridays in that year form zero artists.

Fixed probe 9 now covers 52- and 53-Friday calendar years (1960, 1961, and 1965) plus the terminal December 12, 1969 checkpoint. Each case produces exactly 300 and cannot carry formation into the next `formedYear`. This directly removes the observed `305 / 306 / 289` distribution without increasing the annual or decade formation target.

### Release-capacity diagnosis and bounded candidates

The failed decade's late-year release rolls were constrained before release selection. A treatment-only 260-week replay of the first mature-prospect candidate confirmed that merely adding `0.20` to the score of a 52-week unsigned runtime prospect was ineffective: those prospects were often absent from the top-40 candidate list, and annual release ratios still fell to `0.7960`, `0.7444`, and `0.7007` in 1962-1964. That bonus was removed.

A 52-week continuous-vacancy score floor was then tested. It also failed because labels frequently refilled briefly and reset the age while the aggregate roster continued to contract. Its 1962-1964 release ratios were `0.7923`, `0.7377`, and `0.6957`. That decision state and its constants were removed.

The next bounded candidate restored an empty operating label after 1960 by allowing a passed, affordable scouting attempt to sign its highest-ranked eligible candidate without applying the generic score cutoff. This improved the five-year release ratio from `0.8010x` to `0.8266x`, but remained below the `0.85` floor; its annual results were:

| Year | Successful releases | Control | Ratio | Scheduled Albums | Control | Ratio |
|---:|---:|---:|---:|---:|---:|---:|
| 1960 | 4,154 | 4,313 | 0.9631 | 1,253 | 1,090 | 1.1495 |
| 1961 | 4,307 | 4,810 | 0.8954 | 1,667 | 1,600 | 1.0419 |
| 1962 | 4,463 | 5,363 | 0.8322 | 2,068 | 1,958 | 1.0562 |
| 1963 | 4,425 | 5,692 | 0.7774 | 2,223 | 2,066 | 1.0760 |
| 1964 | 4,323 | 6,041 | 0.7156 | 2,160 | 2,111 | 1.0232 |

The evidence isolates the remaining limiter: after 1960, 300-400 passed and otherwise affordable scouting attempts per year still stop at the generic `0.30` score threshold while roster size falls from 2,625 to 1,867. Album capacity remains inside its inherited band, and ownership, duplicate-roster, duplicate-pool, premature-probation, and terminal-release counters remain zero.

### Final source state at the requested stop

The final implementation preserves the accepted 1960 score threshold and RNG path. Beginning in 1961, an enabled passed/affordable scouting attempt retains the existing top-40 candidate enumeration and exact score ordering, but uses a zero minimum score for a non-empty operating label; negative-fit candidates still fail. An empty operating label uses the already-tested best-candidate recovery. Scouting probability, the single random draw, candidate ranking, advances, affordability, formation volume, drops, release cadence/selection, Albums, finance, format, genre, regional, and historical inputs are unchanged.

`Data/AILabel.cs` exposes the score threshold as an optional input to the existing evaluation without adding a random draw. `Systems/RosterManager.cs` owns the enabled-only 1960/post-launch boundary. Fixed probe 34 proves that 1960 retains `0.30`, the post-launch non-empty floor is `0`, and the post-launch empty-label recovery admits the best candidate only after all existing scouting and affordability boundaries.

Final verification at the stop point:

- `dotnet build "Label Man.sln" --no-restore`: pass, with only the pre-existing unused `ChartManager.OnGenreMomentumChanged` warning;
- `git diff --check`: pass;
- `d6-calendar-postlaunch-final-probes-1001`: accepted D5 suites and D6 probes 1-34 pass, exit code 0, and `CHART_AUDIT_COMPLETE ... weeks=1` is present; and
- the known post-completion `MissingSingletonsTemp.cs` diagnostic remains non-fatal and unchanged.

The broader final post-launch score floor was intentionally not simulated after the last bounded diagnostic because the user required a stop before another long run and the final instruction was to stop after build/probes. It must therefore not be represented as a measured Gate-E pass. The next authorized validation should begin with a fresh 104-week seed-1001 treatment and a treatment-only mid-decade prefix before any new 520-week pair. The complete original control/treatment decade families and every bounded diagnostic family remain preserved.

## Repair pass stop and investigation handoff (2026-07-14)

This section supersedes the immediately preceding repair-state conclusion. The post-launch score policy and two vacancy-urgency refinements were subsequently measured in bounded seed-1001 treatment runs. None is accepted. The user stopped the pass after the final 260-week checkpoint; no 520/522-week run, seed 1002/1003 run, or holdout was launched.

### Calendar correction retained

The date-specific December catch-up described above was removed. `ArtistManager` now resets fractional formation carry at each `GameDate.year` boundary and caps a completed calendar year at 300. Fixed probe 9 proves 300 formations in complete 52- and 53-Friday years. A 260-tick run ends on December 25, 1964 and therefore correctly reports 294 formations for that partial year. A date-complete January 1, 1960 through December 26, 1969 decade is 522 chart ticks, not 520; no 522-tick validation has been run.

### Bounded replenishment results

All candidates preserve the 1960 result. Each table row below is one 52-week block compared with the preserved seed-1001 control.

| Candidate / run | Release ratios, blocks 1-5 | Album ratios, blocks 1-5 | Decision |
|---|---|---|---|
| zero post-launch score floor plus `0.30` urgency floor; `d6-calendar522-floor030-middecade-1001` | 0.9631 / 0.8827 / 0.8432 / 0.7776 / 0.7385 | 1.1495 / 1.0150 / 1.0465 / 1.1041 / 1.0853 | Fail: late release contraction |
| zero post-launch score floor plus persistent `0.25` urgency while any vacancy remains; `d6-calendar522-persistent-vacancy-middecade-1001` | 0.9631 / 0.9158 / 0.9047 / 0.9782 / 1.0086 | 1.1495 / 1.0362 / 1.1542 / 1.3742 / 1.3074 | Fail: Album overproduction |
| zero post-launch score floor plus one-interval urgency consumption; `d6-calendar522-urgency-carry-middecade-1001` | 0.9631 / 0.9075 / 0.8382 / 0.7728 / 0.7285 | 1.1495 / 1.0400 / 1.0455 / 1.0862 / 1.0246 | Fail: late release contraction |

The last candidate is the current source state, but it is experimental and explicitly unfrozen. It uses the existing `0.25` floor; after 1960, a successful signing into a still-vacant roster subtracts one 12-week urgency interval instead of always resetting the age. Its five-block aggregate is 21,814 / 26,219 releases (`0.8320x`) and 9,371 / 8,825 Albums (`1.0619x`). Build and D5/D6 fixed probes 1-34 pass. The known post-completion `MissingSingletonsTemp.cs` autoload message remains non-fatal after the completion marker.

### Next causal seam

The urgency endpoints show that scouting volume alone cannot satisfy both inherited bands: enough repeated signing to restore releases causes a disproportionate Album-project increase. The stronger upstream lead is re-sign/drop churn. In the final bounded run, performance drops were 1,576 / 1,713 / 1,643 / 1,597 in 1961-1964, plus 580 / 596 / 502 / 425 label-closure departures. Of those performance drops, 265 / 1,172 / 1,364 / 1,411 followed a prior re-signing. `ArtistManager.ReconcileSignedArtist` currently maps every free-agent signing to `NewSigning`, while the prior career tier is not retained when `ReconcileDroppedArtist` maps the artist to `Dropped`. This is a plausible source of both repeated two-flop probation churn and the changed Album mix; it is a diagnosis lead, not yet a proven correction.

The authoritative next-pass instructions are in `SimTools/ArtistPopulationReleaseCapacityInvestigationHandoff.md`. Stop state remains in force until that handoff's short gates pass.

## Experienced-comeback investigation stop (2026-07-14)

This section supersedes the preceding experimental source-state description. The investigation followed `ArtistPopulationReleaseCapacityInvestigationHandoff.md`; it did not run 520/522 weeks, seeds 1002/1003, or a holdout. The one authorized 260-week seed-1001 treatment was used and failed the per-block Album boundary, so no disabled acceptance replay or decade request followed it.

### Causal finding and source correction

Joining re-sign/drop events to project identity confirmed the handoff's suspected seam. In the persistent-urgency endpoint, 6,623 Albums belonged to artists with a prior contract, including 6,361 scheduled after their history-bearing artist had been reset to `NewSigning`. The project ledger contained no overlapping Album schedules, so duplicate pipelines were not the cause. The first comeback-state treatment later showed 1,593 released Albums from third-or-later contract cycles in block 5 and 1,116 repeat re-signings in the same block.

The failed post-launch zero score floor and urgency carry were removed. The enabled-only lifecycle treatment now:

- records pre-drop and contract-entry career state, performance-drop count, contract cycle, experienced-comeback status, and scheduling snapshots;
- distinguishes a first contract from an experienced comeback without changing the disabled route;
- requires three current-contract flops for an unresolved experienced comeback, while a current-contract hit resolves the evaluation through preserved career history;
- retains the accepted 13-week first performance-drop cooldown and uses a finite 52-week recovery after a repeated performance drop; and
- preserves the accepted 12-week / `0.25` vacancy urgency, one draw/attempt per label/week, and active-label boundary.

Fixed D6 probe 34 covers the launch-year frozen path, pre-drop state, three-flop comeback window, hit resolution, monthly-review guard, and finite repeat recovery. `AILabel`, candidate ordering/score threshold, affordability, release cadence/selection, Album choice, finance, format, market, genre, and regional rules remain controls.

### First treatment: restored tier plus finite repeat recovery — **FAIL**

The measured run family was:

```text
d6-comeback-recovery-final2-probes-1001
d6-comeback-recovery-final2-gatea-1001
d6-comeback-recovery-final2-gatec-1001
d6-comeback-recovery-final2-gatec-repeat-lean-1001
d6-comeback-recovery-final2-middecade-1001
```

The enabled checkpoints used the standard seed-1001 lifecycle/market switches plus `--lean-probe`; the decisive commands were:

```text
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=104 --run=d6-comeback-recovery-final2-gatec-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --lean-probe
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=104 --run=d6-comeback-recovery-final2-gatec-repeat-lean-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --lean-probe
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=260 --run=d6-comeback-recovery-final2-middecade-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --lean-probe
```

Each command exited 0 and emitted its matching `CHART_AUDIT_COMPLETE` marker. The known post-completion `MissingSingletonsTemp.cs` diagnostic remained non-fatal. The two lean 104-week runs matched all **51/51** CSV streams byte-for-byte; their sorted suffix/hash manifest SHA-256 is `84CDAD3731647269FC58E9BC3B713B0C810D1DD9C1D3A8478F3F5A108DC6BC9B`.

| Block/year | Successful releases | Control | Ratio | Scheduled Albums | Control | Ratio |
|---|---:|---:|---:|---:|---:|---:|
| 1960 | 4,154 | 4,313 | 0.9631 | 1,253 | 1,090 | 1.1495 |
| 1961 | 4,311 | 4,810 | 0.8963 | 1,654 | 1,600 | 1.0338 |
| 1962 | 5,150 | 5,363 | 0.9603 | 2,274 | 1,958 | **1.1614** |
| 1963 | 5,212 | 5,692 | 0.9157 | 2,618 | 2,066 | **1.2672** |
| 1964 | 5,399 | 6,041 | 0.8937 | 2,542 | 2,111 | **1.2042** |

Every release block passes, but the final three Album blocks exceed `1.15`; this treatment is rejected. Annual units/gross/label-net/market-net ratios were `1.0244/1.0354/1.0418/1.0418`, `0.9759/0.9941/1.0007/0.9992`, `1.0028/1.0050/1.0013/1.0014`, `0.9780/0.9860/0.9825/0.9845`, and `0.9742/0.9813/0.9757/0.9756`. Cumulative Single and Album unit ratios were `0.9866` and `1.0638`, so the economy/format bands themselves remained controlled.

Formation counts were exactly `300 / 300 / 300 / 300 / 294`. First-time signing event counts were nonzero in every block: `323 / 523 / 552 / 553 / 537`. Ownership, duplicate-roster, duplicate-pool, premature-probation, cooldown, terminal-roster, and terminal-release-eligibility violations were zero. Among 74,254 label-week rows whose label was already defunct at the start of the week, scouting rolls, gate passes, attempts, and signings were all zero. Final population included 198 never-signed unsigned artists, 105 eligible dropped artists, and 1,722 cooldown-blocked dropped artists.

The late project identity remains dominated by experienced catalogs: experienced contracts account for 68.12%, 75.30%, and 79.79% of released Albums in blocks 3-5. In block 5, 1,273 Albums came from third-or-later contract cycles. This is why stricter repeat-signing supply policy remains the live seam even though aggregate economics pass.

### Current short-gate refinement — **UNACCEPTED / mature replay not authorized**

The first treatment mapped a pre-drop `NewSigning` state to `Declining` on comeback. Telemetry showed 3,842 mature-period Album releases with exactly that `NewSigning -> Declining` mapping. The current refinement preserves `NewSigning` as the presentation tier while retaining the separate experienced-comeback evidence window. Source inspection then confirmed that `NewSigning` and `Declining` share the same base Album prior; the excess is tied more strongly to preserved hit inventory and repeated contracts than to the display tier alone.

Current run family and exact commands:

```text
d6-comeback-tier-retention-probes-1001
d6-comeback-tier-retention-gateb-1001
d6-comeback-tier-retention-gatec-1001
d6-comeback-tier-retention-gatec-repeat-1001

dotnet build "Label Man.sln" --no-restore
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=1 --run=d6-comeback-tier-retention-probes-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --genre-market-v2-probes --artist-population-lifecycle-probes --lean-probe
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=52 --run=d6-comeback-tier-retention-gateb-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --lean-probe
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=104 --run=d6-comeback-tier-retention-gatec-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --lean-probe
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=104 --run=d6-comeback-tier-retention-gatec-repeat-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --lean-probe
```

Build passes with only the pre-existing unused `ChartManager.OnGenreMomentumChanged` warning; `git diff --check` passes; accepted D5 probes and D6 probes 1-34 pass. All Godot commands exit 0 with matching completion markers. The 52-week result remains exactly 4,154 releases and 1,253 Albums with zero invariants. The 1961 result is 4,315 / 4,810 releases (`0.8971x`) and 1,688 / 1,600 Albums (`1.0550x`); units/gross/label-net/market-net are `1.0188/1.0305/1.0335/1.0322`. The independent 104-week repeat matches **51/51** streams; run-manifest SHA-256 is `FB2576607B8C364CE336FEF1D575D40F14FE71B41FD76A31B3B29EBB8C6117F1`.

The exact current functional-source manifest covers `Data/AILabel.cs`, `Data/AlbumProject.cs`, `Data/SimulatedArtist.cs`, `Systems/ArtistManager.cs`, `Systems/CompetitorManager.cs`, `Systems/RosterManager.cs`, `SimTools/ArtistPopulationLifecycleProbeSuite.cs`, and `SimTools/ChartAuditRunner.cs`. Its sorted `path=SHA-256` manifest hash is `4F1EDCC521AC07AA2FCDCE27161A922E43E4A0A733CCAD2A06BA46AE6D5FD144`.

This refinement has passed the short gates but is not accepted: the authorized 260-week treatment was already consumed by the preceding failed source state. A fresh mature-period run requires explicit continuation authority. No disabled replay is warranted until a 260-week candidate passes every Album block.

### Initial-pool decision boundary

The measured supply split supports the user's hypothesis in one limited sense: by week 260 there are only 198 never-signed unsigned artists versus 1,722 performance-drop cooldown blocks, so a larger fresh pool would reduce pressure to recycle experienced catalogs. It is not a safe first scalar under the current contract. Raising 3,000 to 7,000-10,000 would change initial artist generation, top-candidate ordering, initial roster quality/genre mix, advances, later RNG state, first-year signings, release quality, Albums, and finance. Holding nominal market volume at 150-180 million does not isolate those effects, and the exact accepted 1960 result would almost certainly move.

The narrower next candidate remains a stricter finite guard on third-or-later performance re-signing. If that cannot retain the `0.85` release floor without fresh supply, a separately authorized enabled-only pool experiment should begin below 7,000 and must restart at 52 weeks; it must not be treated as an economy-neutral adjustment.

## Enabled 7,000-artist initial-market experiment (2026-07-14): **FAIL / maturity stop**

The user authorized an enabled-only larger initial talent market because the preceding five-year treatment ended with only 198 never-signed unsigned artists and 1,722 cooldown-blocked dropped artists. This pass tested the lower requested boundary of 7,000, treated telemetry size as an explicit acceptance constraint, and did not run 520/522 weeks, seed 1002/1003, or a holdout.

### Causal construction and rejected boundaries

The first scalar implementation generated all 7,000 artists before launch rosters. It was immediately rejected because labels filled 4,130 launch slots instead of the frozen 3,000: `d6-pool7000-gateb-1001` produced 6,306 releases (`1.4621x` control), 1,710 scheduled Albums (`1.5688x`), economic ratios of approximately `1.398-1.422x`, and 27.23 MiB of CSV telemetry.

The reserve was then separated from roster initialization. The frozen 3,000 artists are generated and allocated first; an additional 4,000 unsigned artists are materialized afterward on `ArtistManager`'s isolated population RNG, without consuming the global simulation or `NameGenerator` streams. The disabled route still generates 3,000 and never creates the reserve. That boundary fixed launch allocation but exposed hard-roster expansion: `d6-pool7000-reserve-gateb-1001` produced 5,179 releases (`1.2008x`), 1,221 Albums (`1.1202x`), economic ratios of `1.210-1.230x`, and 25.22 MiB.

An enabled-only soft operating target now records each label's actual initialized roster while retaining its hard maximum as future physical capacity. New labels use their actual launch roster, with a one-artist bootstrap target for an initially empty label. Scouting fills losses back to that target rather than treating every unused hard-max slot as latent demand. The first target run passed 52 weeks but failed at 104 because the blanket 78-week inactivity horizon retired never-signed reserve artists. Never-signed artists are now kept in the labor market; only a prior-contract career can age into inactivity.

The next 104-week diagnostic still failed because every label selected from the national top candidates. With a 7,000-artist registry this raised average release-decision quality in 1961 to `0.6277`, producing excessive economics while roster refill remained slow. The final experimental source therefore uses a deterministic regional discovery slate:

- the eligible regional pool is preferred, with national fallback only if the region cannot fill the slate;
- scouting ability maps to a slate of 4-12 artists;
- a stable label/artist hash refreshes visibility every four weeks without adding an RNG draw;
- the existing quality/supply score orders only that bounded slate; and
- the accepted one draw/one attempt rule remains intact, while 12-week urgency persists until the soft target is actually refilled.

No initial-reserve artist receives a per-artist telemetry row. The enabled-only label stream adds only four compact integers for target and discovery observation.

| Boundary / run | Releases vs control | Albums vs control | Economy | Decision |
|---|---:|---:|---:|---|
| Generate all 7,000 before rosters; `d6-pool7000-gateb-1001` | `1.4621x` | `1.5688x` | `1.398-1.422x` | Reject: launch roster expansion |
| Isolated post-roster reserve; `d6-pool7000-reserve-gateb-1001` | `1.2008x` | `1.1202x` | `1.210-1.230x` | Reject: hard-max roster expansion |
| Soft launch target; `d6-pool7000-target-gateb-1001` | `0.9930x` | `1.0358x` | `1.119-1.127x` | 52-week pass; 104-week inactivity failure |
| Never-signed inactivity repair; `d6-pool7000-target-inactivity-gatec-1001` | 1961 `0.8079x` | 1961 `0.7781x` | 1961 `1.184-1.201x` | Reject: national top-candidate quality and refill behavior |
| Bounded regional discovery; final short candidate | 1960 `1.0202x`; 1961 `0.8919x` | 1960 `1.1055x`; 1961 `0.8963x` | annual totals `1.0536-1.0768x` | Pass 52/104 short gates |

### Final experimental source and short validation

The final run family is:

```text
d6-pool7000-discovery-probes-1001
d6-pool7000-discovery-gateb-1001
d6-pool7000-discovery-gatec-1001
d6-pool7000-discovery-gatec-repeat-1001
d6-pool7000-discovery-middecade-1001
```

The decisive commands were:

```powershell
dotnet build "Label Man.sln" --no-restore
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=1 --run=d6-pool7000-discovery-probes-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --genre-market-v2-probes --artist-population-lifecycle-probes --lean-probe
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=52 --run=d6-pool7000-discovery-gateb-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --lean-probe
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=104 --run=d6-pool7000-discovery-gatec-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --lean-probe
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=104 --run=d6-pool7000-discovery-gatec-repeat-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --lean-probe
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=260 --run=d6-pool7000-discovery-middecade-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --lean-probe
```

The build passes with only the pre-existing unused `ChartManager.OnGenreMomentumChanged` warning. The accepted D5 suites and expanded D6 fixed probes 1-38 pass. Every Godot command exits 0 and emits its matching `CHART_AUDIT_COMPLETE` marker; the known post-completion `MissingSingletonsTemp.cs` diagnostic remains non-fatal.

The independent 104-week repeat matches all **51/51** CSV streams byte-for-byte. Both run manifests hash to `A8983E726BBAB88C9D200420988CA2858834DC4F06FEAFBD991190E2C13E4DBD`. At week 104 the registry contains 7,594 artists: 7,541 active, 2,305 rostered, 3,186 never-signed unsigned, 1,498 eligible dropped, 599 cooldown-blocked dropped, and 53 inactive. First-time signing events exceed re-signing events in both short blocks. All ownership, duplicate membership/pool, premature probation, terminal roster/eligibility, and release-selection counters are zero.

Telemetry did not bloat. The 52-week candidate emits 51 files totaling 23.16 MiB versus approximately 23.17 MiB for the comparable 3,000-pool run. The 104-week candidate emits 51 files totaling 48.39 MiB versus 48.75 MiB. The completed 260-week candidate emits 51 files totaling 137.97 MiB versus 144.31 MiB for `d6-comeback-recovery-final2-middecade-1001`.

### 260-week maturity checkpoint: hard release failure

The one authorized mature treatment completed all 260 weeks and has run-manifest SHA-256 `AB357DD2718A8B7793B5A017F7FFD7DDC80E1ABE4E6DA79146AA0F747303B58D`.

| Block/year | Successful releases | Control | Ratio | Scheduled Albums | Control | Ratio |
|---|---:|---:|---:|---:|---:|---:|
| 1960 | 4,400 | 4,313 | 1.0202 | 1,205 | 1,090 | 1.1055 |
| 1961 | 4,290 | 4,810 | 0.8919 | 1,434 | 1,600 | 0.8963 |
| 1962 | 4,397 | 5,363 | **0.8199** | 1,723 | 1,958 | 0.8800 |
| 1963 | 4,577 | 5,692 | **0.8041** | 2,032 | 2,066 | 0.9835 |
| 1964 | 4,345 | 6,041 | **0.7193** | 1,971 | 2,111 | 0.9337 |

Every Album block passes, but release blocks 3-5 violate the `0.85` floor. Five-block aggregate economics remain controlled: units `1.0269x`, gross `1.0180x`, label net `1.0052x`, and market net `1.0139x`. Aggregate Single and Album unit ratios are `1.0301x` and `0.9404x`. Every annual total-units and market-net ratio remains inside the catastrophic band. Calendar formations are exactly `300 / 300 / 300 / 300 / 294` for 1960 through the partial 1964 endpoint.

Final population is 8,494 registered, 6,373 active, 1,756 rostered, 2,973 never-signed unsigned, 363 eligible dropped, 2,063 cooldown-blocked dropped, 986 inactive, 89 retired, and 1,046 disbanded. Active labels have an aggregate operating target of 2,652, leaving roster size 896 below that target (918 summed clamped vacancies); 24 active labels are empty.

The larger market therefore solves supply scarcity but not selection/churn. In blocks 3-5, live scouting selected 2,676 dropped free agents but only 981 first-time artists from its bounded slates, despite the large never-signed reserve. Candidate-score rejections rose from 97 in block 1 to 1,262 in block 5. Performance drops on repeat contracts rose to 865 and 910 in blocks 4 and 5; third-or-later re-signings rose from 91 in block 3 to 229 and 497 in blocks 4 and 5. Among 62,958 rows for already-closed labels, scouting rolls, gate passes, attempts, and successes are all zero. All ownership, duplicate roster/pool, premature-probation, terminal, and artist-selection invariants remain zero.

**Result:** 7,000 is a viable enabled-only talent-market boundary for economy and telemetry, but increasing the pool alone does not pass mature release capacity. It does make a stricter finite repeat-signing policy causally meaningful: unlike the 198-artist endpoint, this candidate retains almost 3,000 never-signed alternatives. The next coherent correction should operate on discovery choice—prefer a signable never-signed prospect before permitting a third-or-later performance comeback, with a finite escape when no fresh candidate qualifies—rather than increasing the pool again or widening release/Album/economic gates. This source state is experimental and unfrozen. The failed 260-week checkpoint consumed this pass's mature treatment, so no disabled acceptance replay or additional long run followed it.

The exact nine-file functional-source manifest covers `Data/AILabel.cs`, `Data/AlbumProject.cs`, `Data/SimulatedArtist.cs`, `Systems/ArtistManager.cs`, `Systems/ChartManager.cs`, `Systems/CompetitorManager.cs`, `Systems/RosterManager.cs`, `SimTools/ArtistPopulationLifecycleProbeSuite.cs`, and `SimTools/ChartAuditRunner.cs`. Its sorted `path=SHA-256` manifest hash is `9ACDCB2E824D98C9CD77C1A0620823EA6F034FC89AAC499FA23C960A083EA3EC`.

The next bounded pass is governed by `SimTools/ArtistPopulationFreshProspectPreferenceHandoff.md`. It retains this 7,000-artist source as the experimental foundation and authorizes one fresh-prospect preference correction before any new mature checkpoint.

## Enabled 7,000-artist fresh-prospect preference experiment (2026-07-14): **FAIL / maturity stop**

The authorized enabled-only correction was implemented at the existing scored discovery-slate seam. `AILabel` now exposes the unchanged score for each already-enumerated slate candidate; `RosterManager` selects the highest-scoring affordable never-signed prospect only when the threshold-passing overall winner is a dropped, third-or-later performance comeback. It otherwise keeps the existing winner immediately. The policy consumes no RNG, does not rescore or expand the slate, and preserves the single signing attempt.

The existing `label-scouting-vacancy-weekly.csv` stream gained only the eight required policy fields. The two boolean values use compact `0`/`1` values; fields remain blank before a live candidate evaluation. No new stream, candidate-level row, or reserve-artist row was added.

Commands and completion evidence:

```powershell
dotnet build "Label Man.sln" --no-restore
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=1 --run=d6-pool7000-fresh-priority-probes-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --genre-market-v2-probes --artist-population-lifecycle-probes --lean-probe
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=52 --run=d6-pool7000-fresh-priority-gateb-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --lean-probe
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=104 --run=d6-pool7000-fresh-priority-gatec-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --lean-probe
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=104 --run=d6-pool7000-fresh-priority-gatec-repeat-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --lean-probe
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=260 --run=d6-pool7000-fresh-priority-middecade-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --lean-probe
```

Build passes with only the inherited unused `ChartManager.OnGenreMomentumChanged` warning, and `git diff --check` passes. The accepted D5 suites and D6 probes 1-47 pass. The 1-, 52-, 104-repeat-, and 260-week commands emitted their matching `CHART_AUDIT_COMPLETE` markers. The foreground 104-week wrapper exceeded its 60-second capture window, but its complete 51-stream family ends at week 104 and matches the independently completed repeat byte-for-byte. The known post-completion `MissingSingletonsTemp.cs` diagnostic remains non-fatal.

| Horizon/run | Streams | CSV bytes (MiB) | Suffix/hash manifest SHA-256 | Result |
|---|---:|---:|---|---|
| 52 weeks / `d6-pool7000-fresh-priority-gateb-1001` | 51 | 24,676,717 (23.534) | `D960D4CC0A11D5D3CE8B225227A44F34D175D58A4C6F5A6901E10B957BC6453F` | 4,400 releases and 1,205 Albums, exact retained launch-period result; below 24.32 MiB |
| 104 weeks / primary and repeat | 51 / 51 | 51,612,927 (49.222) each | `6760405AB7F318E26AE96800B601FC90B01D0811E133888182862AAC8CB49B4C` | all 51 suffix-matched CSVs byte-identical; below 50.81 MiB |
| 260 weeks / `d6-pool7000-fresh-priority-middecade-1001` | 51 | 147,753,280 (140.909) | `CCA710B53CCE55B90944117ACBBF87ACBBEE3F8BAF4120A3B84E21718B16F7A0` | below 144.87 MiB; hard release failure below |

The 260-week treatment's policy telemetry records 408 applied preferences, 458 `NoQualifyingNeverSigned` finite fallbacks, and 8,356 `OverallBestNotGuarded` decisions. The policy is therefore active, finite, and confined to its intended third-or-later performance-comeback scope.

| Block/year | Successful releases | Control | Ratio | Scheduled Albums | Control | Ratio |
|---|---:|---:|---:|---:|---:|---:|
| 1960 | 4,400 | 4,313 | 1.0202 | 1,205 | 1,090 | 1.1055 |
| 1961 | 4,290 | 4,810 | 0.8919 | 1,439 | 1,600 | 0.8994 |
| 1962 | 4,389 | 5,363 | **0.8184** | 1,682 | 1,958 | 0.8590 |
| 1963 | 4,693 | 5,692 | **0.8245** | 2,067 | 2,066 | 1.0005 |
| 1964 | 4,476 | 6,041 | **0.7409** | 2,032 | 2,111 | 0.9626 |

**Result:** blocks 3, 4, and 5 violate the required `0.85` successful-release floor. This pass stops on that first hard gate failure. No disabled replay, 522-week control/treatment, additional seed, holdout, or further scalar adjustment was run.

The exact nine-file functional-source manifest covers `Data/AILabel.cs`, `Data/AlbumProject.cs`, `Data/SimulatedArtist.cs`, `Systems/ArtistManager.cs`, `Systems/ChartManager.cs`, `Systems/CompetitorManager.cs`, `Systems/RosterManager.cs`, `SimTools/ArtistPopulationLifecycleProbeSuite.cs`, and `SimTools/ChartAuditRunner.cs`. Its sorted `path=SHA-256` manifest hash is `79913B9DADC4B4B478DA1CCD58353D0867DCC2474AFA7EFD29531EA6749A39EC`.

## Market-clearing correction handoff (2026-07-15)

The failed fresh-prospect preference exposed a coupled market-clearing defect rather than a remaining pool-size problem. By 1964 the treatment averaged 1,887.9 rostered and 791.7 release-eligible artists, ended 908 slots below active operating targets, rejected 1,243 of 2,517 candidate evaluations on score, and signed 814 free agents versus 456 first-time artists. All 2,517 evaluated slates contained never-signed supply, but median best-never-signed score was only `0.2542` because the shared evaluator treats missing reputation as negative evidence. The block also produced 929 performance drops on contract sequence two or later.

The next authoritative pass is governed by `SimTools/ArtistPopulationMarketClearingHandoff.md`. It supersedes the narrow fresh-preference handoff and authorizes a coherent three-part correction: deterministic service recovery for deep/persistent roster or release-lane deficits, separate fresh-potential and experienced-production discovery/evaluation, and a contract-scoped one-comeback lifecycle ending in structured performance exhaustion after a second performance failure. The 7,000-market source remains an experimental foundation, not accepted shipping behavior.

## Market-clearing Gate M1/M2 stop (2026-07-15): **FAIL / no M3+ run**

The authorized market-clearing implementation replaced the enabled urgency floor and third-plus preference use with service modes, deterministic fresh-potential and experienced-production lanes, and current-contract performance exhaustion. The disabled route was not exercised or changed by this gate.

`dotnet build "Label Man.sln" --no-restore` passed with only the inherited unused `ChartManager.OnGenreMomentumChanged` warning; `git diff --check` passed. The fixed one-week command below emitted `D5_PROBE_PASS`, `D6_PROBE_PASS`, and its completion marker. The known `MissingSingletonsTemp.cs` autoload diagnostic remains non-fatal after completion.

```powershell
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=1 --run=d6-market-clearing-probes-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --genre-market-v2-probes --artist-population-lifecycle-probes --lean-probe
```

The updated suite reports all 47 fixed probes passing, including service-mode, separate-lane, no-career-penalty, three-current-contract-flop, Top-40 clearance, first-departure cooldown, second-departure exhaustion, non-performance departure, and no-third-comeback coverage.

The 52-week treatment command completed (`CHART_AUDIT_COMPLETE run=d6-market-clearing-gateb-1001 weeks=52`):

```powershell
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=52 --run=d6-market-clearing-gateb-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --lean-probe
```

It emitted 51 CSV streams totaling 29,083,736 bytes (27.73 MiB). The source was the documented unfrozen `d6-pool7000-fresh-priority` state plus this correction; no acceptance manifest is recorded because M2 failed.

| Measure | Treatment | Control | Ratio / limit | Result |
|---|---:|---:|---:|---|
| Successful release-outcome rows | 4,141 | 4,313 | 0.9601 | Pass |
| Scheduled Album projects | 1,326 | 1,090 | 1.2165 | **Fail** (`>1.15`) |
| Lean CSV bytes | 29,083,736 | 24,676,717 prior lean family | +17.86% | Observed; no longer an acceptance gate |
| Recovery label-weeks | 7,391 | — | exercised | Observed |
| Fresh-potential selections | 2,981 | — | exercised | Observed |
| Experienced-production selections | 7 | — | bounded fallback | Observed |

The diagnostic was initially read through the cumulative tier lifecycle counter, which is not the release acceptance measure. The authoritative `release-outcomes.csv` count is 4,141 and passes the release band. The M2 stop is decisive on Album overproduction. The telemetry increase is retained for reproducibility but, by user direction on 2026-07-15, is not an acceptance failure.

**Stop decision:** Gate M2 did not pass because scheduled Albums were `1.2165x` control. No 104-week repeat, 260-week checkpoint, disabled replay, date-complete decade, additional seed, or holdout was launched. This source is unaccepted and remains subject to the headcount-only recovery correction below rather than a constant sweep.

### M2 diagnosis and headcount-only recovery correction (2026-07-15)

Telemetry size is no longer a gate. It should still avoid obviously catastrophic multiplication, but byte totals and the prior `+5%` limit cannot stop a candidate.

The first service implementation treated instantaneous release cooldown as a staffing deficit. That was incorrect. A label could be at its operating target while fewer than three artists were release-eligible; `releaseLaneDeficit` then entered Recovery and permitted hiring above target. Across 52 weeks this produced 2,981 first-time signings, only 7 free-agent signings, and an ending roster of 5,169. The prior fresh-priority candidate produced 493 first-time signings, 29 free-agent signings, and an ending roster of 2,573. Label-closure departures rose from 134 to 910, consistent with the advance and roster expansion shock.

The Album excess is concentrated in new-contract projects: 1,190 of 1,326 scheduled projects had `careerStateAtSchedule = NewSigning`, versus 1,035 of 1,205 in the prior candidate. Total authoritative releases still passed at `0.9601x`, so release cadence does not need expansion.

The authorized correction retains separate fresh-potential scoring, deterministic discovery lanes, three-current-contract-flop evidence, and second-performance-departure exhaustion. It changes service recovery to headcount only:

- `serviceDeficit = max(0, OperatingRosterTarget - rosterSize)`;
- `releaseLaneDeficit` remains observational and cannot trigger Recovery;
- no recovery signing may occur at or above `OperatingRosterTarget`;
- no temporary release-lane buffer or recovery ceiling above target is permitted; and
- the corrected candidate restarts at build/probes and the 52-week M2 treatment.

The next checkpoint must bring Albums inside `[0.85,1.15]` while retaining the passing release ratio. Telemetry volume is recorded, not gated.

The focused Codex execution contract for this correction is `SimTools/ArtistPopulationHeadcountRecoveryHandoff.md`. It supersedes the broader market-clearing handoff where they differ and authorizes the corrected 52/104/260/disabled/522-week ladder plus later seeds after a complete seed-1001 pass.
