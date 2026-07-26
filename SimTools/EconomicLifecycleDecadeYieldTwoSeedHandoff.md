# Economic/lifecycle decade economy-yield handoff

Status: **CORRECTED / READY FOR TWO ENABLED DECADE RUNS**

## Purpose

Verify that the strict-1965 economic/lifecycle repair remains stable through
completed 1969. Run exactly two enabled seeds from unchanged source. These are
heavy runs; do not add a control, repeat, shorter checkpoint, third seed, or
holdout.

The accepted launch point is:

- `d6-economic-lifecycle-rebalance-through-1965-1001`;
- strict completed-1965 acceptance passed;
- D5 probes and D6 probes 1-69 passed;
- 1965 Single, Album, total units, gross, label net, and market net were all
  approximately `0.96x-1.01x` control;
- 252 labels participated versus 254 control.

Raw active-label count is descriptive only. The control's additional
nonparticipating labels are a removed pre-lifecycle artifact and must not be
restored or treated as a decade failure.

## Corrected execution-path requirement

The first attempted decade prefix,
`d6-economic-lifecycle-rebalance-decade-enabled-1001`, omitted the D5/D6 probe
flags and is not equivalent to the accepted 1965 execution. `ChartAuditRunner`
re-seeds before running the probes, so probe execution advances the subsequent
simulation RNG path. The omitted-probe attempt diverged at week 1 and stopped
at completed 1964 with 911 scheduled Albums. Preserve it as invalid
execution-path evidence; do not use it to adjudicate the accepted candidate.

The one-week equivalence run
`d6-economic-lifecycle-decade-flag-equivalence-probe-1001` proved that restoring
both probe flags while retaining `--lean-probe` exactly reproduces the accepted
week-1 chart units, market units, number-one record, active/new records, and
successful releases. Both corrected decade runs must therefore include both
probe flags.

## Frozen reference and preflight

Use the retained benchmark:

```powershell
$control = 'd6-transition-envelope-decade-control-1001'
$godot = 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe'
```

Before seed 1001:

```powershell
git diff --check
dotnet build "Label Man.sln" --no-restore
```

Record the source diff/hash state and confirm both corrected prefixes are
unused. Do not spend another simulation on a separate probe-only run; the
probes run at the start of each authorized decade command because they are part
of the accepted RNG path.

## The only authorized simulations

Run sequentially. Seed 1002 is authorized only if seed 1001 completes and
passes its hard gates.

```powershell
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- `
  --weeks=522 `
  --run=d6-economic-lifecycle-rebalance-decade-accepted-path-1001 `
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
  --run=d6-economic-lifecycle-rebalance-decade-accepted-path-1002 `
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

The seed-1001 control is the fixed acceptance benchmark for both runs; seed
1002 is a robustness test, not a same-seed causal comparison. Do not tune,
change flags, or replace the benchmark between seeds.

If a wrapper times out, check the Godot process and the matching `weeks.csv`
tail. Never relaunch over a live or completed prefix. The known
`MissingSingletonsTemp.cs` diagnostic is nonfatal only after process completion,
522 captured weeks, and `CHART_AUDIT_COMPLETE`.

## Minimal adjudication

For each seed require:

- normal completion through week 522;
- header-only `catastrophic-fail-fast.csv`;
- the completed-1965 strict gate and all later annual gates;
- finite, reconciled annual rows through 1969;
- no source change between runs.

Use `market-revenue.csv`, `decade-annual-rollup.csv`,
`release-capacity.csv`, `album-projects.csv`, and the Single-lane streams.
Report, for 1960-1969 and the full decade:

- Single, Album, and total units;
- gross, label net, and market net;
- successful releases, Single decisions, scheduled Album projects, and
  completed Album drops;
- Single units per Single release and Album units per completed drop, clearly
  labelling the latter a market-year/catalog-carryover proxy;
- participating-label count and decisions per participant;
- results by label tier, especially Major/MidTier versus lower tiers.

Run the existing focused yield analyses after each successful simulation:

```powershell
node SimTools/analyze-single-lane-hit-tail.mjs SimLogs d6-economic-lifecycle-rebalance-decade-accepted-path-1001 --control-prefix=$control --json=SimLogs/d6-economic-lifecycle-rebalance-decade-accepted-path-1001-single-yield.json
node --max-old-space-size=8192 SimTools/analyze-m5-album-catalog-cohorts.mjs d6-economic-lifecycle-rebalance-decade-accepted-path-1001 $control
```

Repeat those two analysis commands with the seed-1002 prefix only after its run
completes. Preserve raw analyzer output.

Stop at the first hard failure. Do not widen a band or repair between seeds.
Distinguish count failure from yield failure and Single failure from Album
failure. If both seeds pass, hand back one compact annual table per seed plus a
two-seed summary; no additional simulation is needed.
