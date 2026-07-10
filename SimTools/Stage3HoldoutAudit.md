# Directive 4b — Stage 3 terminal holdout audit

Date: 2026-07-10

The prespecified seed-2004 disabled/enabled pair was run once with the accepted Stage 2 checkpoint configuration (`distanceModelEnabled = true`, `reachHalfDistance = 10000`, `costPerDistance = 0.0001`). No code or parameter changed after this holdout was inspected.

## Results

| Guard | Result | Disposition |
|---|---:|---|
| Crossover year | 1967 | Pass (1966-1969) |
| 1960 overall/adult/youth mix | 24.527% / 57.791% / 10.394% | Pass |
| Standalone share through 1963 | 0.000% | Pass |
| Standalone share in 1969 | 45.412% | Pass |
| Paired Pearson decade-mean delta | +0.099541 | Pass |
| All-decade closed Top-40 median | 9 disabled / 11 enabled | **Fail: +2 weeks** |

The all-decade median uses the same terminal lifecycle population used by the Stage 1 regression: records with a terminal `peakPosition` from 1 through 40, excluding live/right-censored outcomes. Its sample sizes are 3,291 disabled and 2,883 enabled. The paired annual medians confirm that the breach is not a rounding artifact:

| Year | Disabled median | Enabled median | Delta |
|---:|---:|---:|---:|
| 1960 | 9 | 9 | 0 |
| 1961 | 9 | 10 | +1 |
| 1962 | 9 | 10 | +1 |
| 1963 | 9 | 10 | +1 |
| 1964 | 9 | 11 | **+2** |

## Stop condition

Directive 4b inherits the paired closed Top-40 median guard of +/-1 week. Seed 2004 therefore fails the Stage 3 holdout despite passing the headline album/mix and Pearson gates. The holdout is consumed. In accordance with the no-post-holdout-tuning rule, Baseline v2 is not frozen, no v2 hashes or anchors are declared, and Baseline v1 is not retired. Any resumption requires separate direction rather than calibration against this seed.
