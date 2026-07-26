# Economic/lifecycle founder-emergence two-seed handoff

Status: **SOURCE IMPLEMENTED / BUILD AND PROBES PASS / FULL REPLAY NOT RUN**

## Purpose

Validate the lifecycle-emergence repair through completed 1969, first on seed
1001 and then on seed 1002. A higher share or count of active labels is an
acceptable and intended result. The candidate must still keep the remaining
format, throughput, economic, reconciliation, and single-lane checks healthy.

Do not tune between seeds. Stop after the first hard failure.

## Failure being repaired

The prior seed-1001 candidate completed all 522 weeks but failed the final
scheduled-Album gate:

```text
1969 scheduled Albums: 1872 / 2802 = 0.668094
required band: [0.70, 1.30]
```

Artifact:

```text
SimLogs/d6-economic-lifecycle-album-count-margin-decade-1001-catastrophic-fail-fast.csv
```

The 1969 Album share was already `1872 / 2504 = 0.7476`, approximately the
historical 74% target. Raising the percentage of releases selected as Albums
was therefore rejected as the primary repair.

## Root-cause evidence

The failure is primarily a lifecycle-emergence trap, not a tier-wide release
capacity shortage:

- Failed versus accepted-path 1969 mean active labels was
  `225.71 / 233.19 = 0.9679`.
- Decisions per participating label were slightly higher in the failed path
  (`12.11` versus `11.81`), so participating labels were not broadly
  under-scheduling.
- The failed path had 17 fewer participating labels, including 10 fewer
  runtime-founded participants.
- Of 668 runtime founders born through completed 1969, 607 entered `Dying`.
- Median time from birth to first `Dying` status was exactly 13 weeks; 405
  founders reached it within 13 weeks.
- At the end of 1969, 94 of 98 active runtime founders still had an operating
  roster target of one.
- Only five runtime organic-growth events occurred during the entire failed
  decade.
- Runtime births were fixed at 72 per mature year while runtime closures were
  commonly 63-77 per year, creating churn rather than emergence.

The old path made a one-artist founder accrue three loss months before it had a
reasonable chance to establish a release. `Dying` status then sharply reduced
release opportunity. Organic growth simultaneously required recent chart
success, so most founders could neither recover nor add the release lanes
needed to mature.

## Implemented repair

The provisional late-era `Independent`/`Small` release-capacity multiplier was
removed completely. No tier-wide release probability uplift remains.

The enabled lifecycle path now provides bounded founder emergence:

1. A runtime founder with adequate cash runway retains normal `Stable` status
   during its first nine operating months. Bankruptcy and low-cash
   `Struggling` checks remain authoritative.
2. A filled, profitable founder with at least six months of runway and a recent
   release may grow its operating target one slot per quarterly review until it
   reaches the three-lane emergence floor.
3. Growth beyond three operating artists retains the stricter established-label
   requirements: no consecutive losses and at least one recent charting record.
4. Births still bootstrap at one operating artist. The repair does not create a
   birth-week roster burst.
5. Album choice logic is unchanged by this lifecycle repair. The already
   present `1.07` Album eligibility scale and inclusive 75% pressure threshold
   remain in source.
6. Operating-target telemetry now records both recent charting count and recent
   release count.

Relevant source:

- `Systems/CompetitorManager.cs`
- `Systems/LabelLifecycleManager.cs`
- `SimTools/ArtistPopulationLifecycleProbeSuite.cs`
- `SimTools/ChartAuditRunner.cs`

## Verification already completed

Source-state fingerprint before the full replay:

```text
d4e8461006c5e087926101da686cf89d2c625a28
```

This is the output of:

```powershell
git diff --binary | git hash-object --stdin
```

Completed checks:

- `git diff --check`: passed.
- `dotnet build "Label Man.sln" --no-restore`: passed with the existing unused
  `ChartManager.OnGenreMomentumChanged` warning only.
- D5 probes: passed.
- D6 fixed probes 1-69: passed.
- One-week outside-sandbox probe:
  `d6-economic-lifecycle-founder-emergence-probe2-1001`: completed with
  `CHART_AUDIT_COMPLETE`.

The sandboxed Godot executable crashes in native code with signal 11 before
initialization. Run Godot outside the sandbox. The outside-sandbox probe
completed normally.

No full founder-emergence simulation has been run. The decade prefixes below
were unused when this handoff was written.

## Frozen control and executable

```powershell
$control = 'd6-transition-envelope-decade-control-1001'
$godot = 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe'
```

Before seed 1001:

```powershell
git diff --check
dotnet build "Label Man.sln" --no-restore
$sourceState = git diff --binary | git hash-object --stdin
```

Require `$sourceState` to equal:

```text
d4e8461006c5e087926101da686cf89d2c625a28
```

Also confirm that these prefixes are unused:

- `d6-economic-lifecycle-founder-emergence-decade-1001`
- `d6-economic-lifecycle-founder-emergence-decade-1002`

## Authorized simulations

Run sequentially. Seed 1002 is authorized only if seed 1001 completes and
passes the adjudication below.

### Seed 1001

```powershell
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- `
  --weeks=522 `
  --run=d6-economic-lifecycle-founder-emergence-decade-1001 `
  --seed=1001 `
  --enable-genre-market-v2 `
  --enable-artist-population-lifecycle `
  --genre-market-v2-probes `
  --artist-population-lifecycle-probes `
  --lean-probe `
  --profile-performance `
  --catastrophic-fail-fast `
  --strict-1965-acceptance-gate `
  --gate-control-run=$control
```

After seed 1001, recompute the source fingerprint and require an exact match
before launching seed 1002:

```powershell
$after1001 = git diff --binary | git hash-object --stdin
if ($after1001 -ne $sourceState) {
  throw "Source changed after seed 1001: $after1001 != $sourceState"
}
```

### Seed 1002

```powershell
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- `
  --weeks=522 `
  --run=d6-economic-lifecycle-founder-emergence-decade-1002 `
  --seed=1002 `
  --enable-genre-market-v2 `
  --enable-artist-population-lifecycle `
  --genre-market-v2-probes `
  --artist-population-lifecycle-probes `
  --lean-probe `
  --profile-performance `
  --catastrophic-fail-fast `
  --strict-1965-acceptance-gate `
  --gate-control-run=$control
```

## Adjudication

For each seed require:

- normal completion through all 522 captured weeks;
- `CHART_AUDIT_COMPLETE`;
- header-only `catastrophic-fail-fast.csv`;
- both D5 probe groups and D6 fixed probes 1-69 pass;
- completed-1965 strict acceptance passes;
- every completed-year catastrophic ratio stays inside inclusive
  `[0.70, 1.30]`;
- finite annual values and exact format-unit and market-net reconciliation;
- the single-lane analysis passes;
- no source-state change between seeds.

An increase in active or participating labels is acceptable. Do not reject the
candidate merely because it restores more of the configured label population.
Instead, verify that the additional labels do not cause:

- releases, scheduled Album projects, completed Album drops, total units,
  gross, label net, or market net to exceed the existing upper gates;
- a material deterioration in Single economics or hit-tail health;
- a material increase from the approximately 74% historical Album share.

The target is not a bare `0.70` scheduled-project result. Prefer a stable 1969
scheduled-Album ratio around `0.75-0.80` while keeping Album share near its
historical neighborhood and all economic metrics healthy.

## Required post-run analysis

For both seeds, report annual 1960-1969 and full-decade:

- mean, start, and end active labels;
- launch-population and runtime-founded births, closures, active labels, and
  participating labels;
- runtime operating-target distribution and organic-growth event counts;
- successful releases and participating labels;
- scheduled Album projects, completed Album drops, and control ratios;
- Album project share of all release decisions;
- Single, Album, and total units;
- gross, label net, distribution income, and market net;
- results by label tier and label origin;
- Single units per Single release;
- Album units per completed drop, explicitly labelled as a
  market-year/catalog-carryover proxy;
- the minimum annual scheduled-Album ratio, especially 1963, 1968, and 1969.

Use the existing lifecycle comparison helper:

```powershell
node SimTools/analyze-label-survival-participation.mjs `
  d6-economic-lifecycle-album-count-margin-decade-1001 `
  d6-economic-lifecycle-founder-emergence-decade-1001 `
  --year 1969
```

If `node` is not on `PATH`, use:

```powershell
& 'C:\Users\grohl\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe' `
  SimTools/analyze-label-survival-participation.mjs `
  d6-economic-lifecycle-album-count-margin-decade-1001 `
  d6-economic-lifecycle-founder-emergence-decade-1001 `
  --year 1969
```

Repeat for seed 1002 after it is authorized and completed.
