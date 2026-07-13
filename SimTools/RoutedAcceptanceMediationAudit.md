# Routed-acceptance mediation checkpoint

## Scope

This checkpoint uses only `d5-phase2-format-causal-52r4-enabled-1001` telemetry. It makes no behavioral change and does not authorize a new run. Its purpose is to distinguish the direct Single-transfer path already rejected from the potentially mediated radio and awareness path.

## What r4 establishes

The existing explanation sample can be direct-standardized over the same common career, quality, and reach strata used by the regional funnel. `radioFactor` is the routed regional radio opportunity at the observed record/week/region; `radioSalesMultiplier` is the contemporaneous record-level radio-heat multiplier used by conversion.

| Standardized enabled comparison | Final acceptance delta | Radio-opportunity delta | Radio-heat sales-multiplier delta |
|---|---:|---:|---:|
| Teen Pop vs Country | +0.08310 | +0.04155 | +0.01192 |
| Teen Pop vs Doo-Wop | +0.04545 | +0.02272 | +0.00214 |
| Traditional Pop vs Country | +0.19777 | +0.09888 | +0.05633 |
| Traditional Pop vs Doo-Wop | +0.15346 | +0.07673 | +0.03062 |

The ordering is consistent with the mediated-path lead: targets have higher routed acceptance, higher radio opportunity, and a higher observed radio-heat conversion multiplier than both negative-yield controls. It does **not** establish mediation. The Single-transfer counterfactual held the evolved radio/awareness states fixed, so it correctly did not test this path.

## Why r4 cannot finish the trace

The r4 explanation rows have final acceptance and radio opportunity, but not the component route values needed to separate catalog baseline, regional adjustment, segment routing, secondary blend, and momentum contribution. They also lack the regional radio-play and regional-awareness values at the moment sales were calculated. The later regional radio pass mutates both states, so reconstructing them from end-of-week state would be invalid.

Accordingly, r4 cannot determine whether the observed ordering originates in catalog baseline, regional/segment routing, a secondary blend, legacy momentum, or the mediated radio/awareness state. No repair is justified from it.

## Read-only telemetry added for the next authorized observation

`record-genre-explanation.csv` now appends, without changing a simulation calculation:

- sequential acceptance fields: catalog baseline, regional-adjusted acceptance, segment-routed acceptance, primary-weighted routed acceptance, secondary-blend contribution, legacy momentum and its acceptance contribution, and clamp delta;
- sales-time state snapshots: record awareness, regional awareness, effective awareness, record radio heat, and regional radio play.

The decomposition follows the exact acceptance calculation, including primary/secondary weighting and per-component clamping. The state snapshots are captured during the sales pass, before the subsequent regional radio/awareness update.

The updated regional funnel analyzer already ingests these fields and will report their availability, coverage, cohort means, and direct-standardized comparisons. A future observational run may populate them only after explicit authorization. The 520-week ladder remains stopped.

## r5 authorized observation

The authorized observation is `d5-phase2-format-causal-52r5-enabled-1001`, run with the enabled seed-1001 52-week aggregate-only configuration. It completed all 52 weeks. The only terminal diagnostic was the existing non-fatal `MissingSingletonsTemp.cs` autoload warning.

Observational neutrality passes:

- all 44 simulator-generated CSV artifacts comparable between r4 and r5 are byte-identical;
- `record-genre-explanation.csv` has the same 34,629 data rows in both runs;
- every r5 row retains the exact r4 29-column prefix, with zero mismatches, and appends the 13 new decomposition and sales-time fields;
- every new field has 100% coverage in each retained target and negative-control explanation cohort used below.

Reproduction:

```powershell
Godot_v4.7-stable_mono_win64_console.exe --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=52 --run=d5-phase2-format-causal-52r5-enabled-1001 --seed=1001 --enable-genre-market-v2 --aggregate-only

& $node SimTools/analyze-regional-single-yield-funnel.mjs `
  d5-phase2-format-causal-52r5-enabled-1001 `
  d5-phase2-format-causal-52r2-control-1001 `
  --output SimLogs/d5-phase2-format-causal-52r5-enabled-1001-regional-single-yield-funnel-v3.json
```

## Acceptance-route conclusion

The exact decomposition places the target/control ordering upstream of the evolved radio and awareness state. Values below are direct-standardized over common career, quality, and reach strata; each entry is target minus control.

| Enabled retained comparison | Catalog baseline | Regional-adjusted | Segment-routed | Primary-weighted | Secondary blend | Momentum contribution | Clamp delta | Final acceptance |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Teen Pop vs Country | +0.11010 | +0.09977 | +0.09980 | +0.04551 | +0.05430 | +0.02994 | -0.04432 | +0.08310 |
| Teen Pop vs Doo-Wop | +0.06815 | +0.07198 | +0.07207 | +0.01906 | +0.05300 | +0.02162 | -0.04495 | +0.04545 |
| Traditional Pop vs Country | +0.26504 | +0.25411 | +0.25416 | +0.32327 | -0.06911 | +0.07625 | -0.13301 | +0.19777 |
| Traditional Pop vs Doo-Wop | +0.21550 | +0.21886 | +0.21894 | +0.28105 | -0.06211 | +0.06568 | -0.13062 | +0.15346 |

The catalog-baseline delta alone is larger than the final-acceptance delta in all four comparisons. Regional adjustment and segment routing change it only modestly. The blend then exposes two distinct structural routes: Teen Pop's advantage is largely supplied by its secondary-genre contribution, while Traditional Pop's advantage is concentrated in its primary routed component and is diluted by its secondary blend. Momentum adds to the ordering, but the final clamp more than offsets that addition. The ordering therefore does not originate in legacy momentum, the clamp, or an evolved radio/awareness state.

## Sales-time mediation check

| Enabled retained comparison | Record awareness | Regional awareness | Effective awareness | Radio heat | Regional radio play | Radio-sales multiplier |
|---|---:|---:|---:|---:|---:|---:|
| Teen Pop vs Country | +0.00983 | +0.00359 | +0.02688 | +0.02383 | +0.00971 | +0.01192 |
| Teen Pop vs Doo-Wop | +0.01708 | +0.00421 | -0.00462 | +0.00428 | +0.00253 | +0.00214 |
| Traditional Pop vs Country | +0.02252 | +0.02059 | +0.03752 | +0.11265 | +0.03007 | +0.05633 |
| Traditional Pop vs Doo-Wop | +0.02963 | +0.02213 | -0.01663 | +0.06124 | +0.01955 | +0.03062 |

Effective awareness fails the required target-over-both-controls ordering: both targets are below Doo-Wop after the actual sales-time awareness rule is applied. Awareness therefore cannot be the common leading mediator of the target conversion excess.

Radio heat, regional radio play, and the radio-sales multiplier retain the target-over-control ordering. This is consistent with downstream amplification, especially for Traditional Pop, but remains observational: routed acceptance feeds radio opportunity and the subsequent radio pass feeds future radio/awareness state. The snapshot does not intervene on that feedback loop, so it cannot assign a causal share to radio without a replay or counterfactual state intervention.

## Decision

The leading-origin hypothesis is now the structural catalog/routing prior, not mediated radio or awareness. Specifically, Teen Pop points to secondary-blend composition, while Traditional Pop points to the primary catalog/routed component. Radio remains a possible amplifier rather than the identified source; awareness is rejected as a common mediator by the inconsistent effective-awareness ordering.

This observation is sufficient to close the attribution trace but not to choose a repair constant. Any repair proposal must target and independently justify the relevant catalog or blend surface, preserve the intentionally differentiated genre system, and pass the existing fixed and 52-week economic gates before the 520-week ladder can resume. No behavior was changed and no 520-week simulation was run.
