# Artist population decade validation handoff

> **Superseded stop notice (2026-07-14):** The seed-1001 decade failed and the source has changed since the frozen candidate described below. Do not execute any 520-week, seed-1002/1003, or holdout command in this file. The next pass is governed by `ArtistPopulationReleaseCapacityInvestigationHandoff.md`; a future date-complete decade is 522 ticks and remains unauthorized until its short gates pass.

## Status and authority

The text below is retained as the pre-failure handoff record; it no longer authorizes the decade ladder. The former frozen seed-1001 short candidate passed Gates A-C and its independent 104-week repeat, but subsequently failed Gate E and the inherited decade release floor.

This handoff authorizes the Phase-5 sequence already specified by `Directive6-Codex.md`:

1. one paired 520-week seed-1001 checkpoint;
2. only if seed 1001 passes every population and inherited gate, paired seeds 1002 and 1003 with no code or constant change; and
3. only after the three-seed candidate is frozen and accepted, selection and one-time execution of one previously unused holdout pair.

Do not resume late-decade genre implementation or calibration merely because the 520-week process completes. Directive 6 is complete only after the three measurement seeds, the untouched holdout, and the final audit pass.

## Frozen candidate

Use the current workspace based on commit `64dda5e` plus the completed evidence appended to `SimTools/ArtistPopulationLifecycleAudit.md`. Preserve that audit edit.

| Surface | Frozen value / behavior |
|---|---|
| Probation threshold | two current-contract consecutive flops; one current-contract Top-40 hit resolves probation |
| Performance-drop cooldown | 13 weeks |
| Runtime formation | exactly 300 artists per 52 live weeks (`F = 0.10` of the initial 3,000) |
| Inactivity horizon | 78 continuously unowned weeks |
| Terminal inactivity horizon | 52 additional weeks |
| Solo retirement minimum | age 35 |
| Enabled scouting multiplier | `0.20` |
| Vacancy response | operating labels only; after 12 consecutive under-capacity weeks, floor the existing single scouting roll at `0.25` |
| Vacancy reset | successful signing or full roster |
| Closed labels | observable as `InactiveLabel`; no scouting RNG, signing, or urgency state |
| Feature defaults | remain disabled until Directive 6 is fully accepted |

Do not change candidate scoring, affordability, advances, formation, drop rules, release cooldown/cadence, Album rules, finance, genre keyframes, supply weights, regional routing, seasonality, distance, specialist stock, or format memory during the measurement ladder.

## Accepted short evidence

- Build and `git diff --check`: pass.
- D5 suites and D6 probes 1-33: pass.
- Disabled seed-1001 replay: 45/45 frozen SHA-256 matches, with no missing, changed, or extra stream.
- Gate B: releases `4,154 / 4,313 = 0.9631x`; Albums `1,253 / 1,090 = 1.1495x`.
- Gate C 1961: releases `4,255 / 4,810 = 0.8846x`; Albums `1,626 / 1,600 = 1.0163x`.
- All annual Gate-B/C unit, gross, label-net, and market-net ratios pass.
- Ownership, duplicate-membership/pool, probation, terminal eligibility, release selection, and terminal-format violations are zero.
- `d6-active-floor025-w12-gatec-diagnostic-1001` and `d6-active-floor025-w12-final-gatec-1001` match 51/51 deterministic CSV streams.

The 1960 Album result is close to its ceiling. It is accepted and deterministic, but must remain a watch item in every decade seed; do not waive or tune around a later failure.

## Preflight

Before launching a decade:

1. confirm no behavior or constant changed after the accepted 104-week repeat;
2. run `dotnet build "Label Man.sln" --no-restore` and `git diff --check`;
3. confirm the D5 and D6 fixed suites still pass if any source file changed;
4. preserve all prior failed-candidate and accepted short-run artifacts;
5. ensure sufficient disk space for paired decade CSVs; and
6. run the Downloads Godot executable directly, one simulation per process.

`--lean-probe` is appropriate for decade measurement. It retains aggregate market, format, release, finance, genre, population, cohort, event, and artist-project identity telemetry while suppressing the already-established high-volume per-record causal decompositions. `--profile-performance` is diagnostic only; exclude its wall-clock CSV from deterministic hash requirements.

## Seed-1001 commands

Run the control first, then the enabled treatment without changing the workspace:

```powershell
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=520 --run=d6-population-decade-control-1001 --seed=1001 --disable-genre-market-v2 --disable-artist-population-lifecycle --lean-probe --profile-performance

& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=520 --run=d6-population-decade-enabled-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --lean-probe --profile-performance
```

Require exit code 0 and `CHART_AUDIT_COMPLETE ... weeks=520`. The known post-completion `MissingSingletonsTemp.cs` autoload diagnostic is non-fatal only when the completion marker and complete CSV family are present.

## Gate E: population and chronology

For each accepted seed, require all of the following:

- each full calendar year forms `300 +/- 1` runtime artists;
- the exact decade formation count is reported, expected to be approximately 3,000;
- no future-dated formation exists;
- no runtime artist's native primary or secondary genre predates its new-supply availability;
- first-time signings are nonzero in every calendar year;
- at end-1969, artists formed after 1960 are at least 30% of the active signable population and at least 25% of signed rosters;
- inactive, retired, and disbanded counts are each nonzero;
- active population is within `[0.85,1.50]` of the initial 3,000 while the registry retains historical entities;
- no terminal artist returns to signing, roster membership, release eligibility, or project selection;
- each late-emerging genre with at least two prospectively expected formations has a native formation;
- each late-emerging genre with at least ten native formations has an ordinarily signed native artist by end-1969;
- late-emerging project telemetry contains native-identity projects and reports native versus transitioned shares by genre/year without forcing either to 100%;
- ownership, roster/pool, cooldown, probation, terminal, chronology, and ID invariants remain zero; and
- formed-year, act-age, member-age, cohort, native-genre, roster-tier, signing, drop, inactivity, retirement, and disbandment distributions are appended to the population audit.

## Inherited market and simulation gates

A population chronology pass does not waive the inherited D5/4C gates. Against each same-seed control, require:

- individual-seed decade total units, gross, label net, and market net in `[0.90,1.10]`;
- individual-format decade units in `[0.85,1.15]`;
- successful releases and scheduled Album projects in `[0.85,1.15]`;
- every individual seed-year unit and market-net ratio inside the catastrophic `[0.75,1.25]` band;
- paired all-decade closed Top-40 median movement no greater than `+/-2` weeks;
- exact weekly finance reconciliation;
- accepted distance behavior, concentration health, distribution-deal accounting, 4C seasonality, release/project capacity, and memory health;
- the accepted historical genre shapes and the 35% annual national canonical-genre concentration ceiling; and
- Country regional preference, Southwest-highest TexMex, East-Coast-strongest Boogaloo, Gospel infrastructure response, and rising Urban R&B crossover.

For the completed three-seed set, pooled annual total units and market net must remain in `[0.85,1.15]` for every year. Report every seed separately as well as pooled results.

## Stop after seed 1001 if any gate fails

If the seed-1001 pair fails any population, invariant, release/project, format, economic, historical, regional, finance, distance, concentration, seasonality, specialist, or memory gate:

- do not run seeds 1002 or 1003;
- do not select or consume a holdout seed;
- preserve the complete failed artifacts;
- diagnose the first causal seam from existing telemetry; and
- request a new, one-variable amendment before changing behavior.

Do not sweep constants. Do not compensate for population failure by tuning release, finance, format, genre, regional, or historical surfaces. Do not change the accepted 12-week/`0.25` vacancy response during the measurement ladder.

## Seeds 1002 and 1003

Only after seed 1001 passes, run the same pair for seeds 1002 and 1003 with no intervening code, data, scene, or constant change:

```powershell
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=520 --run=d6-population-decade-control-1002 --seed=1002 --disable-genre-market-v2 --disable-artist-population-lifecycle --lean-probe --profile-performance
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=520 --run=d6-population-decade-enabled-1002 --seed=1002 --enable-genre-market-v2 --enable-artist-population-lifecycle --lean-probe --profile-performance

& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=520 --run=d6-population-decade-control-1003 --seed=1003 --disable-genre-market-v2 --disable-artist-population-lifecycle --lean-probe --profile-performance
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=520 --run=d6-population-decade-enabled-1003 --seed=1003 --enable-genre-market-v2 --enable-artist-population-lifecycle --lean-probe --profile-performance
```

Stop the ladder on the first failing measurement seed. Do not tune between seeds.

## Fresh holdout

Do not choose a holdout number in advance. After all three measurement seeds pass and the candidate is frozen:

1. search committed audits, uncommitted files, Git history, and `SimLogs` for prior seed usage;
2. select one seed absent from all four sources;
3. record the selection and frozen Git/worktree state before execution;
4. run one control and one enabled 520-week pair using the same flags and `<fresh-seed>` in the run names; and
5. apply all per-seed Gate-E and inherited gates once.

A failed holdout is a reported failure. Do not widen bands, consume another seed, or tune after seeing it without a new directive.

## Audit deliverable and completion

Append the following to `SimTools/ArtistPopulationLifecycleAudit.md`:

- exact commands, run names, completion markers, Git state, and deterministic hashes;
- per-year formation counts and exact decade total;
- end-1969 cohort shares for active signable population and signed rosters;
- yearly first-time/re-signing, roster, free-agent, drop, inactivity, retirement, and disbandment results;
- formed-year, act-age, member-age, native-genre, tier, and exit distributions;
- late-emerging native formation, ordinary signing, and project-identity evidence;
- all invariant and chronology checks;
- per-seed and pooled market, format, release/project, historical, regional, finance, distance, concentration, seasonality, specialist, and memory gates;
- all measurement and holdout hashes, excluding wall-clock performance telemetry; and
- final constants, toggle state, limitations, and completion recommendation.

Only after the three-seed checkpoint and one fresh holdout pass without post-holdout tuning may Directive 6 be marked complete and late-decade genre work resume.
