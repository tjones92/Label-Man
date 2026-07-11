# Directive 4b — Stage 3b fresh holdout audit

Date: 2026-07-10

Alice approved the prospective paired closed Top-40 median guard widening from +/-1 to **+/-2 weeks**. The original seed-2004 Stage 3 result remains a consumed historical fail under the previous +/-1-week guard; it was not rerun or reinterpreted.

The proposed seed 2003 was not fresh: it appears in the earlier `DecadeRunValidationAudit.md` holdout history. Before this run, seed 2005 was confirmed absent from committed files, uncommitted work, and `SimLogs` scratch output. The seed-2005 disabled/enabled pair was then run once, each for 520 weeks, with the accepted Stage 2 checkpoint configuration (`distanceModelEnabled = true` for the enabled half, `reachHalfDistance = 10000`, `costPerDistance = 0.0001`, and the frozen `difficultyWeight = 0.35`). No code or parameter changed after either result was inspected.

## Results

| Guard | Result | Disposition |
|---|---:|---|
| Crossover year | 1967 | Pass (1966-1969) |
| 1960 overall/adult/youth mix | 23.306% / 48.569% / 9.063% | Pass |
| Standalone share through 1963 | 0.000% | Pass |
| Standalone share in 1969 | 47.404% | Pass |
| Paired Pearson decade-mean delta | +0.009092 | Pass |
| All-decade closed Top-40 median | 11 disabled / 11 enabled | **Pass: 0 weeks, within +/-2** |

The all-decade median uses the same terminal lifecycle population used by the Stage 1 regression: records with a terminal `peakPosition` from 1 through 40, excluding live/right-censored outcomes. Its sample sizes are 2,824 disabled and 2,820 enabled. The paired annual medians remain stable through the inherited 1960-1964 review window:

| Year | Disabled median | Enabled median | Delta |
|---:|---:|---:|---:|
| 1960 | 11 | 11 | 0 |
| 1961 | 10 | 10 | 0 |
| 1962 | 10 | 10 | 0 |
| 1963 | 10 | 10 | 0 |
| 1964 | 10 | 10 | 0 |

## Disposition

Seed 2005 clears every inherited Stage 3 guard, including the prospectively widened +/-2-week paired closed Top-40 median guard. Baseline v2 may proceed to a separate freeze sign-off. This result does not freeze Baseline v2, retire Baseline v1, or modify the historical seed-2004 fail disposition.
