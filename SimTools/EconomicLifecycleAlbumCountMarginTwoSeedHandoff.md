# Economic/lifecycle Album-count margin decade handoff

Status: **ACCEPTED SHORT REPLAYS / READY FOR TWO SEQUENTIAL DECADE RUNS**

## Purpose

Verify that the accepted economic/lifecycle repair remains stable through
completed 1969 after adding deliberate Album-project count margin. The prior
candidate completed seed 1001 but seed 1002 stopped at the completed-1963
catastrophic gate:

```text
scheduledAlbumProjects=1010 / 1446 = 0.698479
required band=[0.70,1.30]
```

The failure was isolated to project count. Seed-1002 releases, Single units,
Album units, total units, gross, label net, and market net remained healthy.
Do not widen the catastrophic band or alter realized Album demand/yield.

## Accepted source adjustment

The enabled responsive-memory format path now applies an explicit `1.07`
multiplier to the final physical Album eligibility projection before comparing
it with the unchanged orphan-Single/delay hurdle.

The late-decade repeat-artist workload guard now activates at a 75% annual
Album-project share rather than two-thirds. Its other bounds are unchanged:

- at least 100 sampled format decisions;
- no more than two Album projects per artist/year once pressure is active.

This is not a quota and does not force an Album decision. The underlying Album
projection must still win. The disabled path retains scale `1.0`, the promo
eligibility weight remains `0.75`, the delay hurdle is unchanged, and no
realized demand, pricing, cost, market-clearing, lifecycle, or gate constant was
changed.

`GenreMarketV2ProbeSuite` now proves the explicit `1.07` eligibility scale and
the inclusive 75% pressure boundary. All prior D5 and D6 probes remain enabled.

## Matched completed-1963 validation

Both short replays used the fixed seed-1001 decade control and the exact
accepted probe-bearing RNG path:

- `d6-economic-lifecycle-album-count-margin-through-1963-1001`;
- `d6-economic-lifecycle-album-count-margin-through-1963-1002`.

Both:

- completed all 209 weeks through the January 3, 1964 boundary;
- emitted `CHART_AUDIT_COMPLETE`;
- passed both D5 probe groups and D6 fixed probes 1-69;
- have header-only `catastrophic-fail-fast.csv` streams;
- have finite, reconciled annual economics;
- were run from unchanged source.

### Scheduled Album projects

| Completed year | Control | Seed 1001 | Ratio | Seed 1002 | Ratio |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1960 | 1,083 | 1,305 | 1.204986 | 1,222 | 1.128347 |
| 1961 | 1,257 | 1,229 | 0.977725 | 1,202 | 0.956245 |
| 1962 | 1,349 | 1,189 | 0.881394 | 1,150 | 0.852483 |
| 1963 | 1,446 | **1,127** | **0.779391** | **1,140** | **0.788382** |

The repair therefore puts both seeds inside the requested `0.75x-0.80x`
completed-1963 range. The highest early annual count ratio is seed 1001's
`1.204986x`, safely below the catastrophic ceiling.

### Completed-1963 economic ratios

| Metric | Seed 1001 / control | Seed 1002 / control |
| --- | ---: | ---: |
| Successful releases | 0.981487 | 0.932706 |
| Participating labels | 1.024648 | 1.010563 |
| Single units | 1.059944 | 1.059184 |
| Album units | 1.055927 | 0.978896 |
| Total units | 1.059772 | 1.055739 |
| Gross | 1.060663 | 1.047147 |
| Label net | 1.102775 | 1.082206 |
| Market net | 1.100699 | 1.078905 |

The additional projects did not create an early economic overshoot. Seed 1002
also improved from 1,010 to 1,140 completed-1963 scheduled projects while its
Album units remained approximately control-like.

## Frozen control and preflight

Use:

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

Record `$sourceState` and confirm these prefixes are unused:

- `d6-economic-lifecycle-album-count-margin-decade-1001`;
- `d6-economic-lifecycle-album-count-margin-decade-1002`.

Do not run another probe-only or short checkpoint. The probes are part of each
authorized decade command and advance the accepted RNG path.

## The only authorized simulations

Run sequentially. Seed 1002 is authorized only after seed 1001 completes and
passes the gates below.

```powershell
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- `
  --weeks=522 `
  --run=d6-economic-lifecycle-album-count-margin-decade-1001 `
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

```powershell
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- `
  --weeks=522 `
  --run=d6-economic-lifecycle-album-count-margin-decade-1002 `
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

The seed-1001 control remains the fixed benchmark for both seeds. Do not tune,
change flags, replace the benchmark, or edit source between runs. After seed
1001, recompute `git diff --binary | git hash-object --stdin` and require it to
equal `$sourceState` before launching seed 1002.

## Adjudication

For each seed require:

- normal completion through all 522 captured weeks;
- `CHART_AUDIT_COMPLETE`;
- header-only `catastrophic-fail-fast.csv`;
- both D5 probe groups and D6 probes 1-69 pass;
- completed-1965 strict format/economic acceptance passes;
- all later completed-year catastrophic gates remain inside inclusive
  `[0.70,1.30]`;
- completed-1963 scheduled Album projects remain at least `0.75x` control;
- finite annual rows with exact format-unit and market-net reconciliation;
- no source-state change between seeds.

Stop at the first hard failure. Do not widen a band or repair between seeds.

After each successful run, execute:

```powershell
node SimTools/analyze-single-lane-hit-tail.mjs SimLogs d6-economic-lifecycle-album-count-margin-decade-1001 --control-prefix=$control --json=SimLogs/d6-economic-lifecycle-album-count-margin-decade-1001-single-yield.json
```

Repeat with the seed-1002 prefix after seed 1002 completes. The current
`analyze-m5-album-catalog-cohorts.mjs` endpoint contract requires exactly 469
weeks, so do not invoke it on a 522-week artifact as presently written.

Report annual 1960-1969 and full-decade:

- successful releases and participating labels;
- scheduled projects, completed Album drops, and their control ratios;
- Single, Album, and total units;
- gross, label net, and market net;
- Single units per Single release;
- Album units per completed drop, explicitly labelled a
  market-year/catalog-carryover proxy;
- results by label tier;
- the minimum annual scheduled-Album ratio, especially 1963, 1968, and 1969.

The 75% pressure change is specifically a late-decade robustness adjustment.
Its effect must be assessed from the full runs, not inferred from the completed-
1963 short replays.
