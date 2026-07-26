# Label Lifecycle-Calibrated 1965 Acceptance Report

## Outcome

The one authorized run, `d6-label-competition-midtier-through-1965-1001`, reached the completed-1965 boundary and then stopped at week 314 / 1966-01-07 under the strict acceptance gate. Its process exit code was `1`, solely because the gate rejected completed-1965 Album units. The authoritative strict-gate failure was:

```text
gate=Strict1965Acceptance
metric=albumUnits
candidate=7,849,301
control=11,129,114
ratio=0.705294
floor=0.80
```

This is an acceptance failure, not a replacement-run condition. No second run was launched.

## Command and preflight

Command:

```powershell
$godotExe = 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe'
& $godotExe --headless --path . SimTools/ChartAuditRunner.tscn -- `
  --weeks=314 `
  --run=d6-label-competition-midtier-through-1965-1001 `
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

- Seed: `1001`; requested duration: 314 weeks; observed runtime: about 450 seconds.
- `git diff --check` passed. Existing user changes were preserved.
- `dotnet build "Label Man.sln" --no-restore` passed with zero warnings and zero errors.
- No prior Godot process and no existing matching run prefix were present.
- Startup confirmed `600` generated labels. The retained seed-1001 directory calibration is `86` MidTier labels of `600` total.
- Source inspection confirmed lifecycle-enabled deterministic quarterly competitive exit; base chance `0.03`; zero recent-charting prerequisite; launch/runtime minimum ages of 9/12 months; and a roll isolated from Godot global RNG.
- D5 probes passed and D6 fixed probes 1-69 passed, including probe 69's bounded competitive-label-exit checks.

## Structural result

All required probes passed; finance was finite through the gate; annual, cohort, release, label, and tier telemetry exists through completed 1965; and no separate catastrophic structural event was recorded. The `catastrophic-fail-fast.csv` contains only the strict `albumUnits` rejection above. The nonzero process exit is therefore the intended gate abort, not an ambiguous crash or a reason to rerun.

## Release and lifecycle envelope

| Completed year | Successful releases (candidate / control) | Ratio | Decisions (candidate / control) | Success rate, candidate / control | Mean active labels, candidate / control | Participating labels, candidate / control | Decisions per mean-active, candidate / control |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1964 | 4,027 / 3,330 | 1.209610x | 4,022 / 3,328 | 99.876% / 99.820% | 429.29 / 347.19 | 387 / 264 | 9.369 / 9.585 |
| 1965 | 4,052 / 3,336 | 1.214628x | 4,047 / 3,327 | 99.828% / 99.850% | 420.89 / 338.75 | 388 / 254 | 9.615 / 9.821 |

The completed-1965 release result is inside the inclusive emergency envelope (`1.214628x` is below `1.30x`), as is 1964. It is, however, well above the ordinary `[0.85x, 1.15x]` release band. The excess is primarily population and participation: mean active labels were +82.13 (+24.25%), distinct participants were +134 (+52.76%), and decisions per mean active label were slightly lower than control (`0.979040x`). The success rate was essentially unchanged (-0.023 percentage points).

| Year | Run | Launch mean active / participants / decisions | Runtime-founded mean active / participants / decisions |
| --- | --- | ---: | ---: |
| 1964 | Control | 269.54 / 264 / 3,328 | 77.65 / 0 / 0 |
| 1964 | Candidate | 325.25 / 312 / 3,903 | 104.04 / 75 / 119 |
| 1965 | Control | 260.72 / 254 / 3,327 | 78.04 / 0 / 0 |
| 1965 | Candidate | 307.72 / 306 / 3,913 | 113.17 / 82 / 134 |

In 1965, 57.2% of the mean-active gap came from launch-population labels and 42.8% from runtime-founded labels. Runtime-founded labels account for 61.2% of the participation gap. This is a survival/participation topology issue, not a release-success-rate issue.

## MidTier and 1965 tier footprint

The candidate MidTier mean was `79.83` labels, or `18.97%` of its `420.89` active labels. Its percentage is barely inside the directional 14%-19% range, but it is not aligned with the retained control (`55.51` labels; `16.39%` of `338.75`) because the candidate population is materially inflated. The MidTier count is +24.32 labels (+43.81%). Therefore this is a MidTier failure for the stated acceptance purpose.

| Tier | Mean active (share) | Release decisions (share) | Mean roster | Mean release-eligible | Market units (share) | Market gross (share) | Selling-label gross share |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Major | 6.00 (1.43%) | 261 (6.45%) | 194.21 | 134.26 | 23,857,213 (14.09%) | $23,501,313 (13.43%) | 13.43% |
| MidTier | 79.83 (18.97%) | 1,770 (43.74%) | 1,024.57 | 605.19 | 92,252,331 (54.50%) | $93,643,150 (53.53%) | 53.53% |
| Independent | 138.32 (32.86%) | 1,224 (30.24%) | 642.51 | 353.79 | 36,917,463 (21.81%) | $39,301,954 (22.47%) | 22.47% |
| Small | 136.08 (32.33%) | 352 (8.70%) | 254.68 | 165.98 | 4,972,053 (2.94%) | $5,946,804 (3.40%) | 3.40% |
| Boutique | 60.66 (14.41%) | 440 (10.87%) | 295.40 | 189.98 | 11,277,568 (6.66%) | $12,536,824 (7.17%) | 7.17% |

The market-unit and footprint rows are the published annual telemetry. The strict-gate accumulator below is authoritative for acceptance ratios and differs slightly because the gate stops before ordinary end-of-run finalization.

## Promotion integrity

All 16 observed Independent-to-MidTier promotions were `PromotionReconciliation` events into MidTier. Every one was launch-population origin; none was runtime-founded. Each had at least two recent charting records, roster at least six, positive last-month profit, zero loss months, at least six runway months, and age above 18 months. There was no individual evidence-gate violation.

| Week | Label | Origin | Age (months) | Charts | Roster | Profit | Loss months | Runway months | Result |
| ---: | --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| 87 | label_0093 | LaunchPopulation | 20.1 | 4 | 7 | 13,199 | 0 | 107.1 | pass |
| 100 | label_0049 | LaunchPopulation | 23.1 | 3 | 9 | 51,898 | 0 | 286.8 | pass |
| 113 | label_0304 | LaunchPopulation | 26.1 | 2 | 9 | 15,690 | 0 | 60.0 | pass |
| 113 | label_0427 | LaunchPopulation | 26.1 | 2 | 13 | 2,557 | 0 | 58.4 | pass |
| 126 | label_0043 | LaunchPopulation | 29.1 | 3 | 10 | 55,390 | 0 | 218.8 | pass |
| 126 | label_0264 | LaunchPopulation | 29.1 | 2 | 9 | 8,738 | 0 | 243.0 | pass |
| 126 | label_0290 | LaunchPopulation | 29.1 | 2 | 9 | 19,147 | 0 | 54.5 | pass |
| 126 | label_0340 | LaunchPopulation | 29.1 | 2 | 12 | 21,249 | 0 | 239.8 | pass |
| 153 | label_0100 | LaunchPopulation | 35.3 | 2 | 10 | 11,317 | 0 | 209.8 | pass |
| 165 | label_0316 | LaunchPopulation | 38.1 | 2 | 8 | 3,512 | 0 | 179.7 | pass |
| 192 | label_0104 | LaunchPopulation | 44.3 | 4 | 9 | 8,628 | 0 | 187.9 | pass |
| 192 | label_0426 | LaunchPopulation | 44.3 | 3 | 10 | 16,592 | 0 | 120.6 | pass |
| 218 | label_0376 | LaunchPopulation | 50.3 | 2 | 7 | 9,453 | 0 | 158.9 | pass |
| 257 | label_0439 | LaunchPopulation | 59.3 | 2 | 10 | 19,988 | 0 | 199.4 | pass |
| 270 | label_0231 | LaunchPopulation | 62.3 | 3 | 11 | 3,393 | 0 | 165.2 | pass |
| 309 | label_0379 | LaunchPopulation | 71.3 | 3 | 10 | 14,907 | 0 | 174.9 | pass |

The `16` distinct launch-population promotions exceed the directional review condition of 12. This is consistent with the inflated MidTier count; it does not justify weakening individual promotion evidence gates.

## Inherited economic acceptance

The following are exact completed-1965 gate accumulators, candidate divided by control:

| Metric | Candidate / control | Ratio |
| --- | ---: | ---: |
| Single units | 161,266,282 / 164,692,558 | 0.979196x |
| Album units | 7,849,301 / 11,129,114 | **0.705294x** |
| Total units | 169,115,583 / 175,821,672 | 0.961859x |
| Gross | $174,930,045 / $190,463,837 | 0.918442x |
| Label net | $95,108,709 / $104,522,383 | 0.909936x |
| Market net | $95,330,393 / $104,843,478 | 0.909264x |
| Successful releases | 4,052 / 3,336 | 1.214628x |
| Scheduled Album projects | 1,889 / 1,836 | 1.028867x |
| Completed Album drops | 1,822 / 1,780 | 1.023596x |

The partial run's `album-projects.csv` was intentionally not finalized after the strict abort; therefore project-terminal-state, candidate/control pipeline slices, matched-cohort yield, and emerging-versus-non-emerging decomposition are unavailable from the authoritative artifact set. They are not substituted from another measure.

## Decision

**Next-pass classification: MidTier lifecycle/promotion.**

The next repair must address overall survival/participation and the excessive MidTier count while preserving the individual promotion evidence gates. Album project, genre, promotion, lifecycle, and release-success policy were not changed in this pass. Album count-versus-yield and emerging-genre investigation are not authorized because the MidTier acceptance condition failed and the strict inherited Album-units gate also failed.

Stop confirmed: no second simulation, alternative seed, repeat, holdout, decade run, or policy change was launched.
