# Regional Single-yield funnel

## Scope and reproduction

This is an offline analysis of `d5-phase2-format-causal-52r3-enabled-1001` and `d5-phase2-format-causal-52r2-control-1001`. It does not modify supply allocation, demand, finance, noise, or the format fork, and it does not run a simulation.

```powershell
& $node SimTools/analyze-regional-single-yield-funnel.mjs `
  d5-phase2-format-causal-52r3-enabled-1001 `
  d5-phase2-format-causal-52r2-control-1001 `
  --output SimLogs/d5-phase2-format-causal-52r3-enabled-1001-regional-single-yield-funnel-v2.json
```

The analyzer joins corrected Single decisions and supply routes to all matching `breakout-funnel` rows, the bounded `record-genre-explanation` sample, lifecycle chart rows, and all-format `geography-metrics` regional context. It direct-standardizes over common career-band, quality-quartile, and reach-bucket project strata.

## Funnel classification

The observed breakout window isolates aware-buyer exposure, conversion, raw demand, stock/restock, capacity, and fulfilled sales through week 14.

| Standardized comparison | Conversion delta | Raw demand/project | Fulfillment-rate delta | Units/project |
|---|---:|---:|---:|---:|
| Enabled Teen Pop vs control | +0.001954 | +16.772K | -4.87 pp | +10.630K |
| Enabled Traditional Pop vs control | +0.003433 | +31.934K | -0.53 pp | +27.666K |
| Enabled Country vs control | -0.000391 | -8.600K | +4.83 pp | -6.472K |
| Enabled Doo-Wop vs control | -0.000742 | -8.145K | +1.88 pp | -4.440K |

Thus the target excess exists before fulfillment. Neither larger starting stock nor better fulfillment creates it: enabled Teen Pop and Traditional Pop have lower fulfillment rates despite more restock. Country and Doo-Wop provide the inverse controls—lower conversion/raw demand despite better fulfillment.

The gain is concentrated by week 4-14, not merely launch: Teen Pop has +1.414K standardized units in weeks 1-3 and +9.216K in weeks 4-14; Traditional Pop has +6.469K and +21.197K respectively.

## Retained versus incoming projects

The higher target yield is not a Soul-transition quality windfall.

| Standardized retained vs incoming | Conversion delta | Raw demand/project | Fulfillment-rate delta | Units/project |
|---|---:|---:|---:|---:|
| Teen Pop retained vs incoming | +0.001753 | +11.555K | -2.28 pp | +7.166K |
| Traditional Pop retained vs incoming | +0.006494 | +85.923K | +5.78 pp | +71.834K |

Observed early-window means are consistent with the supply bridge: retained Teen Pop is 52.1K units/project through the diagnostic window versus 35.8K for Soul -> Teen Pop; retained Traditional Pop is 130.2K versus 84.9K for Soul -> Traditional Pop. The full observed project totals remain higher because the breakout stream intentionally stops after week 14.

The bounded explanation sample also points in the same direction. Retained Teen Pop's mean sampled acceptance is `0.841` versus `0.746` for Soul -> Teen Pop; retained Traditional Pop is `0.953` versus `0.831` for Soul -> Traditional Pop. These are descriptive sample values, not an accepted-legacy comparison.

## Radio, charts, and regional context

Breakout radio/media indicators are higher for the targets, particularly Traditional Pop, but completed-lifecycle chart persistence differs only modestly in the enabled-versus-control standardized comparison (+0.19 Teen Pop chart weeks and +0.51 Traditional Pop chart weeks). This does not establish a late chart-persistence mechanism.

The breakout diagnostic has no observations after week 14 by design. Aggregate-only runs also lack active-record weekly chart rows. Therefore post-week-14 yield cannot be classified from the existing logs; it must not be inferred as zero.

Regional all-format context places the largest positive target deltas on the East Coast and Great Lakes: Teen Pop is +8.598M and +4.667M units respectively; Traditional Pop is +5.661M and +4.206M. These rows provide regional context only, since `geography-metrics.csv` is not record- or format-attributable.

## Read-only comparator telemetry

The existing breakout funnel already isolates observed conversion (`raw demand / aware buyers`), but the explanation stream lacked the accepted legacy Single comparator needed to attribute the conversion gap to the V2 transfer rather than another multiplier.

`record-genre-explanation.csv` now appends read-only fields for:

- blended legacy acceptance comparator;
- legacy and enabled Single-demand multipliers plus their ratio;
- chart visibility, radio-sales, sentiment, award, distribution, and seasonality multipliers.

The v2 analyzer ingests each field, reports header-level availability and cohort coverage, exposes cohort means, and direct-standardizes target-versus-negative-control plus retained-versus-incoming comparisons over the same career, quality, and reach strata. It remains backward-compatible: r3 correctly reports zero coverage and null means for fields that did not exist at that time.

No gameplay calculation uses these fields. A single authorized enabled 52-week aggregate-only observation will populate them; it is not a tuning authorization and does not reopen the 520-week ladder.

## r4 observational checkpoint and causal accounting

`d5-phase2-format-causal-52r4-enabled-1001` completed with the authorized 52-week, seed-1001, aggregate-only enabled configuration. Observational neutrality holds: all 44 simulator-generated artifacts comparable to r3 are byte-identical. The only simulator telemetry expansion is `record-genre-explanation.csv`: all 34,629 prior rows retain their exact 19-column prefix and append the ten new comparator fields.

The analyzer now joins an explanation row to its same record/week/region breakout observation and recomputes raw demand with only `enabledSingleDemandMultiplier` replaced by `legacySingleDemandMultiplier`. All observed awareness and every other multiplier are held fixed.

| Retained cohort | Matched observations | Enabled raw demand | Legacy-transfer counterfactual | Transfer-only increment | Share of enabled raw demand |
|---|---:|---:|---:|---:|---:|
| Teen Pop | 2,583 | 15.503M | 13.581M | +1.922M | +12.4% |
| Traditional Pop | 2,485 | 17.671M | 16.176M | +1.495M | +8.5% |
| Country | 686 | 2.790M | 2.432M | +0.358M | +12.8% |
| Doo-Wop | 1,932 | 13.443M | 11.430M | +2.013M | +15.0% |

This rejects the narrow Single-transfer attribution. The transfer is positive, but it is not target-specific and does not reproduce the conversion ordering: the largest proportional transfer uplift is Doo-Wop, followed by Country, while both are negative-yield controls. `formatTilt` remains exactly `1.0`, award and distribution are constant, and the remaining sampled downstream multipliers do not provide a stable target-only ordering. The absolute routed Single multiplier is higher for the two targets, but that is a routed-acceptance state observation rather than evidence that the V2-versus-legacy transfer causes the enabled/control excess.

## Decision gate

- Do not authorize a Single-transfer repair or a short repair checkpoint. The accepted-legacy transfer ratio fails the required magnitude-and-ordering attribution test.
- Retain the observed pre-fulfillment classification. Any next causal trace must isolate the routed-acceptance state itself or another upstream pre-fulfillment input; it must not tune the transfer, stock, distribution, finance, noise, or the format fork.
- Fulfillment is not the active branch; do not alter stock, restock, or distribution from this evidence.
- Post-week-14 persistence remains unobserved, not disproven. Its classification awaits observational telemetry, not a calibration run.
- Defer the Soul lifecycle product decision. The independent Teen Pop and Traditional Pop conversion excess is sufficient to investigate first.
