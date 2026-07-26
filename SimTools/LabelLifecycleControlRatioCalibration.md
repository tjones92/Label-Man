# Label lifecycle control-ratio calibration

## Objective

Retain the enabled artist/label lifecycle architecture while bringing its
observable throughput back inside the retained control's ratio bands. Preserved
features include:

- deterministic daily two-phase talent-market appointments;
- runtime-founded labels entering empty with target one;
- no runtime signing in the birth chart week;
- ordinary regional discovery, collision choice, signing, and release paths;
- runtime organic-growth eligibility;
- tier promotion/demotion, including the evidence-gated MidTier promotion; and
- the disabled/control code path and its global RNG schedule.

No Album, genre, demand, market-clearing, release-cadence, or acceptance-band
constant was changed.

## Implemented mechanism

`LabelLifecycleManager` now performs an enabled-only quarterly competition
review after ordinary tier and growth processing.

A label is eligible only when all of the following hold:

```text
active
not Major
no charting record in the preceding 52 weeks
launch population has operated at least 9 months, or
runtime-founded population has operated at least 12 months
```

The base quarterly probability is `0.03`. Status raises pressure for
Struggling/Dying labels, positive monthly profit reduces it to 40%, low runway
raises it by 50%, and the final probability is capped at `0.35`.

Selection uses an isolated FNV-style hash over seed, label identity, year,
quarter month, and `LabelCompetitionV1`. It consumes no Godot global RNG and
therefore does not disturb the disabled/control call schedule.

D6 probe 69 covers maturity, recent-chart safe harbor, profitability, distress,
Major exemption, runtime entry runway, probability cap, and deterministic
isolated selection.

## Validation

`git diff --check` passed. `dotnet build "Label Man.sln" --no-restore` passed
with only the inherited unused `ChartManager.OnGenreMomentumChanged` warning.

The out-of-sandbox one-week Godot harness
`d6-label-competition-calibration-probes-r7-1001` completed and reported all D5
probes and D6 fixed probes 1-69 passed. The scalar was then reduced from the
diagnostic `0.08` to `0.03`; the assertion boundaries are unchanged and the
adjusted source rebuilt successfully.

At the user's direction, exactly one 104-week run was performed from the final
scalar:

```text
d6-label-competition-calibration-104-1001
```

It completed all 104 ticks normally. No longer run, repeat, other seed, or
holdout was started.

## 104-week output ratios

All ratios below are candidate/control.

| Metric | 1960 | 1961 | Inherited short-run band |
| --- | ---: | ---: | ---: |
| Successful releases | 1.0649 | 1.1108 | 0.85-1.15 |
| Scheduled Albums | 1.1533 | 1.0294 | 0.80-1.20 |
| Single units | 1.0005 | 1.0035 | 0.85-1.15 |
| Album units | 0.9417 | 0.9516 | 0.80-1.20 |
| Total units | 0.9997 | 1.0019 | 0.85-1.15 |
| Gross | 1.0100 | 0.9974 | 0.85-1.15 |
| Label net | 1.0095 | 0.9955 | 0.85-1.15 |
| Market net | 1.0097 | 0.9897 | 0.85-1.15 |

The output and economic surfaces are therefore aligned with control through the
tested 104-week horizon. Total units are within 0.2% of control in both years,
and all total economic measures are within approximately 1.1%.

## Population and participation

| Metric | 1960 control | 1960 candidate | Ratio | 1961 control | 1961 candidate | Ratio |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Mean active labels | 591.54 | 591.73 | 1.0003 | 442.21 | 517.19 | 1.1696 |
| Distinct release participants | 402 | 517 | 1.2861 | 349 | 469 | 1.3438 |
| Release decisions | 4,377 | 4,650 | 1.0624 | 3,873 | 4,237 | 1.0940 |
| Decisions per participant | 10.89 | 8.99 | 0.8261 | 11.10 | 9.03 | 0.8141 |

The raw label population remains broader than control in 1961, but the excess
no longer becomes proportional output inflation inside the tested horizon.
Additional labels make materially fewer decisions each, while release, format,
unit, and economic ratios all remain inside their inherited bands.

The run closed 40 launch labels in 1960 versus 36 control, matching the first
year closely. In 1961 it closed 142 launch labels versus 216 control, leaving a
74.98 mean-active launch-label excess. This is a remaining population-shape
difference, not a short-run throughput failure.

## Stop decision

The single authorized 104-week checkpoint passes the inherited release, Album,
format-unit, and economic ratio bands while retaining the current lifecycle
features. Raw 1961 label and participant counts are not identical to control.

No claim is made that the earlier completed-1964 `1.311411x` release breach is
repaired; proving that would require a longer run, which was explicitly not
performed. Stop before any large simulation.
