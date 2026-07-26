# MidTier population calibration: 1965 test handoff

## Mission and authority

Run one seed-1001 simulation through the completed 1965 acceptance boundary to
test the new evidence-gated Independent-to-MidTier promotion policy.

This handoff authorizes:

1. static validation of the current worktree;
2. one fresh seed-1001 run through the 1965 strict gate;
3. analysis of label-tier population, promotion evidence, release participation,
   market share, and the inherited 1965 gates; and
4. preservation of all partial artifacts if any gate stops the run.

Stop after this one 1965 test. Do not start seeds 1002/1003, a repeat, a holdout,
or a decade run. Do not change emerging-genre Album conversion, genre acceptance,
Album demand, release-format preference, market clearing, or the inherited gate
bands during this test.

## Current diagnosis

The initial launch population was not the source of the MidTier percentage
failure:

| Run | Initial MidTier | Initial total | Initial share |
| --- | ---: | ---: | ---: |
| retained control, seed 1001 | 86 | 600 | 14.33% |
| latest seed 1001 | 86 | 600 | 14.33% |
| stopped seed 1003 | 99 | 600 | 16.50% |

The excess developed through capability-only promotion and strong survival. The
old Independent-to-MidTier rule required only two sustained capability quarters,
owned reach of at least `0.50`, and low distribution dependency. It required no
operating age, roster scale, chart success, profitability, or runway. In the
latest artifacts:

- seed 1001 ended with 16 surviving launch labels that began Independent but
  were MidTier in 1965;
- seed 1003 ended with 33 such labels;
- the retained control had eight; and
- many current-code promotion events occurred at week 22 with zero recent
  charting records.

The retained control's mean 1965 active-label share was `16.39%` MidTier. The
latest seed 1001 was `19.86%`; stopped seed 1003 was `27.30%`.

## Implemented policy

`Systems/LabelLifecycleManager.cs` now treats MidTier as an earned large,
successful independent tier. Independent-to-MidTier promotion requires all of:

```text
monthsActive > 18
sustainedCapabilityQuarters >= 4
CurrentRosterSize >= 6
recent charting records in the last year >= 2
ownedReach >= 0.50
distribution dependency < 0.35
status is Stable or Rising
consecutiveLossMonths == 0
lastMonthlyProfit > 0
cashReserves >= six months of overhead
```

No launch tier probabilities, runtime birth tiers, label death rules, release
cadence, demand, or Album conversion values were changed.

`SimTools/ArtistPopulationLifecycleProbeSuite.cs` adds D6 probe 68 covering the
successful boundary and failures for age, sustained capability, chart evidence,
roster scale, profitability, and runway.

## Completed lightweight validation

The following already passed:

- `dotnet build "Label Man.sln" --no-restore`;
- `git diff --check`;
- the focused Single hit-tail analyzer's ten self-tests;
- all D5 probes;
- all D6 probes 1-68; and
- one headless chart week:
  `d6-midtier-promotion-gate-probes-1001`.

The known post-completion `MissingSingletonsTemp.cs` autoload message remains
non-fatal when the process exits zero and `CHART_AUDIT_COMPLETE` is present.

Do not overwrite that probe family.

## Worktree preservation

The worktree also contains pre-existing user changes:

```text
M  SimTools/analyze-single-lane-hit-tail.mjs
?? SimTools/SingleHitTailHistoricalCalibrationHandoff.md
```

Preserve them. Do not reset, revert, reformat, stage, or fold them into this
calibration without explicit user direction.

## R0: preflight

Before the 1965 run:

1. verify `git diff --check`;
2. build with `dotnet build "Label Man.sln" --no-restore`;
3. confirm the new run prefix does not already exist; and
4. confirm no prior Godot audit process is still running.

Required fresh prefix:

```text
d6-midtier-promotion-gate-through-1965-1001
```

If any artifact with that prefix exists, stop and choose a new explicit suffix;
never overwrite an artifact family.

## R1: exact 1965 test

Run from the repository root:

```powershell
$godotExe = 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe'
& $godotExe --headless --path . SimTools/ChartAuditRunner.tscn -- `
  --weeks=314 `
  --run=d6-midtier-promotion-gate-through-1965-1001 `
  --seed=1001 `
  --enable-genre-market-v2 `
  --enable-artist-population-lifecycle `
  --genre-market-v2-probes `
  --artist-population-lifecycle-probes `
  --profile-performance `
  --catastrophic-fail-fast `
  --strict-1965-acceptance-gate `
  --gate-control-run=d6-transition-envelope-decade-control-1001
```

Week 314 is intentional: it crosses the completed-1965 boundary where the strict
gate is evaluated. A strict-gate abort at week 314 is a valid measured result;
preserve and analyze its partial artifacts.

Successful execution requires:

- process exit code `0`;
- `CHART_AUDIT_COMPLETE ... weeks=314`;
- a completed 1965 annual row in `market-revenue.csv`;
- D5 and D6 probe passes in console output; and
- a header-only catastrophic-fail-fast stream.

If the process exits `2`, treat the catastrophic row as authoritative. Do not
restart merely because the gate failed.

## Primary acceptance: label population

Calculate mean weekly active-label counts for 1965 from `label-finance.csv`.
Exclude statuses:

```text
Bankrupt
Defunct
Acquired
```

For each tier, report:

```text
mean active labels
share of total mean active labels
minimum weekly count
maximum weekly count
week-313 ending count
```

The primary MidTier cohort target is:

```text
mean 1965 active-label share: 14% to 19%
control reference: 16.39% (55.51 of 338.75 mean active labels)
```

Interpretation:

- `14%-19%`: cohort calibration passes;
- below `14%`: promotion is probably over-constrained; diagnose the binding
  evidence requirement before changing it;
- above `19%`: split the excess into surviving initial MidTier labels,
  launch-Independent promotions, and runtime-founded promotions before proposing
  any further policy.

Do not use market-unit share as a substitute for label-count share. Market share
is a required secondary diagnostic, not the primary acceptance metric in this
pass.

## Promotion integrity

Read `label-operating-target-events.csv` rows where:

```text
reason == PromotionReconciliation
tier == MidTier
```

Separate `LaunchPopulation` and `RuntimeFounded`. Report event count, distinct
labels, earliest week, and distributions of:

```text
recentChartingCount
rosterSize
lastMonthlyProfit
consecutiveLossMonths
runwayMonths
```

Every new MidTier promotion must have:

```text
recentChartingCount >= 2
rosterSize >= 6
lastMonthlyProfit > 0
consecutiveLossMonths == 0
runwayMonths >= 6
```

No Independent-to-MidTier promotion should occur in the first 18 months. Any
violation is an implementation failure, not a calibration miss.

As a directional guardrail, more than 12 distinct launch-population promotions
by the completed-1965 boundary requires review even if the aggregate MidTier
share passes.

## Required secondary reporting

### Tier footprint

For all five tiers, report 1965 shares of:

1. mean active labels;
2. release decisions from `release-strategy.csv`;
3. mean roster and release-eligible artists from `roster-lifecycle.csv`;
4. annual total-market units and gross from `market-revenue.csv`; and
5. selling-label gross concentration from `label-finance.csv`.

Retained references:

| Metric | Control MidTier | Latest seed 1001 | Stopped seed 1003 |
| --- | ---: | ---: | ---: |
| active-label share | 16.39% | 19.86% | 27.30% |
| release share | 37.39% | unavailable here | 53.35% |
| market-unit share | 32.72% | 57.31% | 64.00% |

The current change is expected to repair cohort share first. A high MidTier
market-unit share after the cohort passes must be reported separately as an
output-concentration or Major-underweight question. Do not answer it by changing
genre or Album conversion in this run.

### Release-count gate

Report the inherited release-count ratio and exact strict-gate result. Preventing
promotion leaves blocked labels active as Independents, so it may improve tier
composition without fully correcting total release count.

If the release-count gate still fails while MidTier label share passes, attribute
the excess through:

```text
total active labels
active labels by origin
participating labels by tier
release decisions per active label
release success rate
```

The next repair would then concern overall launch-label survival or participation,
not MidTier classification.

### Album and genre observations

Report inherited 1965 Single, Album, total-unit, gross, label-net, and market-net
ratios exactly as emitted by the strict gate.

If Album units still fail after the MidTier cohort passes:

1. preserve the result;
2. restate the existing legacy-versus-emerging Album decomposition;
3. do not change emerging-genre Album conversion in this test; and
4. stop for user review before implementing the next calibration.

## Stop conditions

Stop immediately and preserve artifacts if:

- a D5 or D6 fixed probe fails;
- the build or `git diff --check` fails;
- the initial seed-1001 label directory no longer contains exactly
  `86 MidTier / 600 total`;
- a MidTier promotion violates any production gate;
- any catastrophic or strict 1965 gate aborts;
- required annual or tier telemetry is missing or non-finite; or
- the run reaches the completed-1965 result, whether pass or fail.

Do not tune and rerun in the same pass. Return the measured 1965 result and a
single attributed recommendation.

## Final report format

Lead with:

```text
1965 test: PASS or STOP
MidTier active-label share: X% versus 16.39% control
MidTier mean active labels: X versus 55.51 control
Launch/runtime promotions to MidTier: X / Y
Strict gate: PASS or first failing metric and ratio
```

Then provide:

1. the five-tier population/footprint table;
2. promotion-integrity evidence;
3. release-count attribution;
4. inherited Album/market ratios; and
5. one next-step recommendation.

Do not call the calibration successful solely because the strict economic gate
passes; the MidTier cohort metric is the primary purpose of this test.
