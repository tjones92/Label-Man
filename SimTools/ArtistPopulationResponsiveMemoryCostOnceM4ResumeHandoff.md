# Responsive-Memory Cost-Once M4 Resume Handoff

Status: **FIX IMPLEMENTED / RESUME AT M4**

Date: 2026-07-18

This handoff supplements
`ArtistPopulationAnalyzerRetirementAndEconomicGateRecoveryHandoff.md` and
supersedes the M4 continuation section of
`ArtistPopulationEconomicRecoverySimulationLadderHandoff.md`.

Do not restore `analyze-market-clearing-format-memory.mjs` and do not create a
replacement monolithic analyzer. The established raw telemetry remains the gate
authority.

## Accepted prior ladder

Retain the owner's completed evidence:

- M1 passed at
  `d6-analyzer-retirement-economic-repair-probes-r4-1001`;
- M2 disabled replay matched all 45 control CSVs byte-for-byte;
- M3 enabled candidate/repeat matched all 63 comparable CSVs and both
  catastrophic files were header-only; and
- the first M4 attempt stopped at week 209 without launching M5.

No M2 or M3 repeat is required for this continuation. The new behavioral change
is confined to enabled responsive-memory outcome estimation, is deterministic,
and consumes no RNG. The audit changes only make completed-year evidence
available and unambiguous at a fail-fast boundary.

## What the first M4 stop meant

The week-209 / 1/3/1964 abort judged the completed **1963** year. It was not a
1964 annual result:

```text
scheduled Album projects = 999 / 1446 = 0.690871
required floor           = 0.700000
minimum passing count    = 1013
shortfall                 = 14 projects
```

Release throughput and demand were not catastrophic. Successful releases were
3,465 / 3,403 control; the pre-memory economics favored 1,437 Albums versus
1,279 in control. Responsive memory changed 501 Album-favoring decisions to
Singles and only 68 Single-favoring decisions to Albums.

The surviving 1963 portfolio remained economically healthy in the completed
rollup: Album units were `0.925219x` control, Single units `1.010314x`, and total
units `1.006662x`.

## Implemented correction

`CompetitorManager.UpdateResponsiveMemoryObservation` previously estimated a
provisional terminal outcome as:

```text
(lifetime revenue - sunk production cost) / maturity
```

That divided the one-time production cost by maturity and charged it repeatedly.
The corrected calculation is:

```text
(lifetime revenue / maturity) - sunk production cost
```

Final observations remain exactly:

```text
lifetime revenue - sunk production cost
```

Age-matched expected-net telemetry now follows the same cost-once convention:

```text
(terminal expected net + sunk production cost) * maturity
    - sunk production cost
```

A fixed D5 probe proves provisional and final values with a known input.

The audit boundary is also repaired:

- catastrophic CSV rows now contain an explicit `completedYear` column;
- completed-year failure state begins with `completedYear=<year>`; and
- the prior year's authoritative `market-revenue.csv` annual rows are flushed
  before fail-fast can abort at the first checkpoint of a new year.

Thus a week-209 abort can no longer masquerade as a 1964 result or suppress the
otherwise complete 1963 annual revenue endpoint.

## Current validation boundary

At this source state:

- `git diff --check` passes;
- `dotnet build "Label Man.sln" --no-restore` passes with zero errors; and
- probe attempt
  `d6-analyzer-retirement-economic-repair-probes-r5-1001` did not reach project
  startup because the local Godot console process crashed natively with signal
  11.

The r5 native crash emitted no managed exception and no audit artifact. It is
the same intermittent launcher failure previously seen before the owner's
successful r4 run; it is not a failed fixed probe. Preserve the r5 name.

Before the M4 launch, run the one-week probe as a preflight using the owner's
known-good invocation. This is not a ladder restart and does not invalidate the
authorization to resume at M4.

```powershell
$godot = 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe'
$control = 'd6-transition-envelope-decade-control-1001'

& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=1 --run=d6-analyzer-retirement-economic-repair-probes-r6-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --genre-market-v2-probes --artist-population-lifecycle-probes --lean-probe
```

Require all D5 and D6 probe markers plus `CHART_AUDIT_COMPLETE`. If the process
again dies before project startup, retry with a fresh known-good Godot 4.7 Mono
process and another unique suffix. Do not change simulation code to address a
native launcher crash.

## M4 — resume here

```powershell
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=313 --run=d6-album-memory-cost-once-through-1965-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --profile-performance --catastrophic-fail-fast --gate-control-run=$control
```

Require explicit normal completion and a header-only catastrophic stream. If it
aborts, require the new `completedYear` field and preserve the failed artifact.

Adjudicate directly from:

- `weeks.csv`;
- `market-revenue.csv`;
- `decade-annual-rollup.csv`;
- `release-capacity.csv`;
- `fork-ratios.csv`; and
- `album-projects.csv`.

The settlement, market-clearing, spillover, and responsive-memory streams remain
diagnostic cross-checks rather than gate authorities.

### M4 required gates

Verify complete 1960–1965 rows and every inherited band, including:

- completed 1963 scheduled Album projects at least `1,013` (`0.70x`);
- 1964 Album units at least `0.80x`;
- 1964 label net at least `0.85x`;
- 1965 Single units at least `0.85x`;
- 1965 Album units at least `0.80x`; and
- 1965 total units, gross, label net, and market net at least `0.85x`.

Also report successful releases, scheduled Album projects, Album drops, and
Album-decision share for every completed year.

### Required causal checks

Use `format-memory-revisions.csv` to prove:

- first observations remain ordinal 1 with
  `replacedPriorRevision=false`;
- no backward or post-final revision exists;
- final estimated outcome equals realized net to date;
- every provisional estimate satisfies
  `lifetimeLabelNet / maturity - sunkProductionCost` when joined to its release
  economics;
- no non-finite residual exists; and
- Album residual pressure is materially less negative than in the stopped M4
  attempt, especially at age 13 and age 26.

Use `fork-ratios.csv` to report pre-memory Album wins, final Album wins, Album to
Single reversals, and Single to Album reversals. The specific repair is confirmed
when the artificial early-life penalty falls without restoring the old
production-cost-only positive bias.

## M5

M5 remains prohibited unless this M4 completes and passes. If M4 passes, freeze
the accepted source and proceed with the existing full-decade contract under a
new prefix:

```powershell
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=522 --run=d6-album-memory-cost-once-decade-enabled-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --profile-performance --catastrophic-fail-fast --gate-control-run=$control
```

Never weaken a gate, alter the retained control, change demand keyframes, or use
national capacity pooling to force passage.
