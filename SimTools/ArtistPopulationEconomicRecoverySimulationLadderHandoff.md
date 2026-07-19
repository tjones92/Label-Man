# Album Economic Recovery Simulation-Ladder Handoff

Status: **IMPLEMENTATION COMPLETE / M1 RUNTIME VALIDATION NOT COMPLETE**

Date: 2026-07-18

This handoff is the execution continuation for
`ArtistPopulationAnalyzerRetirementAndEconomicGateRecoveryHandoff.md`. Read that
document first. Do not restore or replace
`analyze-market-clearing-format-memory.mjs`; it has been deliberately retired.

## Stop boundary

The owner directed the current task to stop here and hand off the simulation
ladder.

Completed at this boundary:

- the monolithic analyzer is deleted;
- the raw-telemetry authority and recovery design are documented;
- prepared Album and promo memory baselines use their deterministic economic
  priors instead of `-productionCost`;
- delayed linked Albums preserve their release-time prior in `AlbumProject`;
- responsive-memory revisions have an unobserved sentinel, ordinal, and explicit
  provisional/final transition rules;
- every live uncharted Album with positive demand and a physical backorder may
  request ordinary bounded regional replenishment;
- settlement dates align with the completed audit checkpoint calendar;
- fixed D5 probes cover the Album restock seam and memory revision lifecycle;
- `git diff --check` passed; and
- `dotnet build "Label Man.sln" --no-restore` passed with zero errors and only
  the pre-existing `ChartManager.OnGenreMomentumChanged` unused-event warning.

M1 is not accepted yet. Two console-runner attempts crashed in native Godot code
with signal 11 before project/probe output:

```text
d6-analyzer-retirement-economic-repair-probes-1001
d6-analyzer-retirement-economic-repair-probes-r2-1001
```

Both produced the same native backtrace and no managed exception or audit
artifact. A third attempt using the non-console executable returned immediately,
left no process, and produced no artifact:

```text
d6-analyzer-retirement-economic-repair-probes-r3-1001
```

The console executable still answers `--version` successfully as
`4.7.stable.mono.official.5b4e0cb0f`. Treat this as a launcher/runtime problem,
not a probe pass or a simulation failure. Preserve those run names and resume
with `-r4`.

## Fixed references

```powershell
$godot = 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe'
$control = 'd6-transition-envelope-decade-control-1001'
$disabledControl = 'd6-market-clearing-disabled-52-1001'
```

All runs use seed `1001`. Never overwrite an existing prefix.

## M1 — finish build/probe validation

First resolve or work around the native launcher crash without changing
simulation behavior. Check for a stale editor/runner, retry from a fresh process,
and use another known-good Godot 4.7 Mono binary if one is available. Do not
interpret file presence as completion.

```powershell
dotnet build "Label Man.sln" --no-restore
git diff --check
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=1 --run=d6-analyzer-retirement-economic-repair-probes-r4-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --genre-market-v2-probes --artist-population-lifecycle-probes --lean-probe
```

Require exit zero, every D5/D6 probe marker, an explicit
`CHART_AUDIT_COMPLETE`, and no managed or native crash. When M1 passes, continue
directly to M2.

## M2 — disabled replay and control preflight

```powershell
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=52 --run=d6-analyzer-retirement-economic-repair-disabled-52-1001 --seed=1001 --disable-genre-market-v2 --disable-artist-population-lifecycle --aggregate-only
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- --run=d6-analyzer-retirement-economic-repair-control-preflight-1001 --seed=1001 --catastrophic-control-preflight --gate-control-run=$control
```

Require normal completion, the same suffix set as `$disabledControl`, and
byte-identical length/SHA-256 for every suffix-matched CSV. Enabled-only
settlement, spillover, and responsive-memory files must not appear. Require the
control preflight to exit zero.

## M3 — enabled 104-week deterministic pair

```powershell
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=104 --run=d6-analyzer-retirement-economic-repair-enabled-104-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --profile-performance --catastrophic-fail-fast --gate-control-run=$control
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=104 --run=d6-analyzer-retirement-economic-repair-enabled-repeat-104-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --profile-performance --catastrophic-fail-fast --gate-control-run=$control
```

Require both completion markers, header-only catastrophic output, identical
suffix sets, and byte-identical comparable CSVs. `performance-profile.csv` is
the only expected timing exclusion. Compare 1960 and 1961 annual economics
directly from `market-revenue.csv`, with release and project cross-checks from
`release-capacity.csv`, `decade-annual-rollup.csv`, and `album-projects.csv`.

Do not run or create an all-in-one analyzer.

## M4 — date-complete 1965 gate

```powershell
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=313 --run=d6-analyzer-retirement-economic-repair-through-1965-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --profile-performance --catastrophic-fail-fast --gate-control-run=$control
```

First prove that `weeks.csv` contains the complete 1960–1965 checkpoint and that
`market-revenue.csv` has the expected `Annual` rows. Then calculate
candidate/control ratios from raw rows.

Required inherited floors include:

- 1964 Album units: at least `0.80x`;
- 1964 label net: at least `0.85x`;
- 1965 Single units: at least `0.85x`;
- 1965 Album units: at least `0.80x`;
- 1965 total units, gross, label net, and market net: at least `0.85x`.

Also require all other inherited completed-year release, scheduled-Album,
format-unit, and economic bands. Reconcile annual total units to `weeks.csv` and
`decade-annual-rollup.csv`; reconcile release/project counts to
`release-capacity.csv` and `album-projects.csv`.

The newer settlement, clearing, spillover, and memory streams are explanatory
diagnostics. Report any disagreement, but do not let those streams replace the
established economic value.

For causal confirmation, verify in the diagnostics that:

- prepared Albums no longer universally have negative production-cost-only
  expected baselines;
- first memory rows have ordinal 1 and
  `replacedPriorRevision=false`;
- no revision occurs after finalization or at a backward age;
- settlement year agrees with `weeks.csv` for the same week;
- Album physical backorders and the serviceable-intent deficit materially fall;
  and
- every regional cleared-unit identity still reconciles.

If M4 fails, stop with the raw rows, ratios, and causal diagnostics. Do not launch
M5 and do not weaken a floor.

## M5 — full seed-1001 decade

Only after M4 passes, freeze the accepted source state and record its hashes.

```powershell
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=522 --run=d6-analyzer-retirement-economic-repair-decade-enabled-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --profile-performance --catastrophic-fail-fast --gate-control-run=$control
```

Require normal completion, header-only catastrophic output, all inherited annual
and decade gates from the authoritative telemetry, and complete unit/economic
reconciliation. Update `ArtistPopulationLifecycleAudit.md` with M1–M5 evidence,
including exact run names, raw ratios, deterministic exclusions, source hashes,
and any retained failed artifacts.

## Protected boundary

Do not repair the result by changing gate floors, control artifacts, demand
keyframes, genre acceptance, the `1.34` common-market capacity multiplier, or
nationally pooling capacity. The new Album replenishment path may be tuned only
if raw telemetry proves it violates regional capacity/inventory conservation or
causes a different inherited hard gate to fail.
