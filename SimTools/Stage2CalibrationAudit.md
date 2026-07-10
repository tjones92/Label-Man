# Directive 4b — Stage 2 distance calibration audit

Date: 2026-07-10

Stage 2 activated the already-wired distance model only after Stage 1 was accepted. Taxonomy values remained frozen throughout. Calibration changed only the two inspector values below:

| Field | Initial | checkpoint value |
|---|---:|---:|
| `reachHalfDistance` | 65 | 10,000 |
| `costPerDistance` | 0.003 | 0.0001 |
| `difficultyWeight` | 0.35 | 0.35 (unchanged) |

The probe ladder deliberately started with the default and increased reach before reducing cost. Relative to the same-seed Stage 1 disabled reference, default reach produced roughly -58% to -67% annual-unit changes. Reach 300 produced about -40% to -49%; 1,200 about -16% to -27%; 5,000 about -5% early and -13% to -20% late; and 10,000 at the default cost about -5% to -18%. Reducing only the distance cost at reach 10,000 produced the checkpoint candidate.

## Disabled national guard

| Seed | Minimum annual delta | Maximum annual delta | Mean annual delta | +/-5% annual gate |
|---:|---:|---:|---:|---|
| 1001 | -3.577% | +0.829% | -1.152% | Pass |
| 1002 | -4.287% | +2.925% | -0.549% | Pass |
| 1003 | -2.506% | +2.658% | +0.101% | Pass |

The calibrated disabled runs are `directive4b-stage2-probe6-disabled-1001`, `directive4b-stage2-calibrated-disabled-1002`, and `directive4b-stage2-calibrated-disabled-1003` in `SimLogs`.

## Geographic and deal observability

Home-market shares of charted units demonstrate material tiered concentration rather than a uniform national allocation:

| Seed | Small | Boutique | Major |
|---:|---:|---:|---:|
| 1001 | 47.9% | 49.5% | 32.8% |
| 1002 | 49.7% | 46.1% | 37.7% |
| 1003 | 48.0% | 48.7% | 41.5% |

Distribution deals remain active: offers/acceptances are 18/8, 14/5, and 11/7 for seeds 1001-1003. The regional telemetry routes unmet demand through region-hub destinations, so non-national backorders are reported in the available tiers rather than as city tiers T3/T4. For example, seed 1001 records 367,177,287 non-national Tier-1 hub backorders, 106,751,757 Tier-2, and 5,436,648 Tier-3. This is an observability limitation of the current routing aggregation, not evidence that no non-national demand occurred.

## Enabled regression and repeat

| Seed | Crossover | 1960 overall/adult/youth mix | Standalone through 1963 | Standalone 1969 | Paired Pearson delta | Median delta |
|---:|---:|---|---:|---:|---:|---:|
| 1001 | 1967 | 25.274% / 54.790% / 11.116% | 0.000% | 44.713% | +0.100922 | +1 |
| 1002 | 1967 | 24.875% / 55.708% / 10.488% | 0.000% | 43.629% | +0.091013 | +1 |
| 1003 | 1967 | 25.094% / 54.862% / 11.554% | 0.000% | 53.260% | +0.121143 | +1 |

All enabled Stage 2 measurement cells meet the inherited timing, mix, ordering, Pearson, and +/-1 closed Top-40 median checks. The seed-1001 enabled repeat emitted the same 34 CSV streams as its primary and every paired file was byte-identical (34/34).

## Checkpoint disposition

Stage 2 passes its calibration checkpoint. The next authorized operation was the terminal seed-2004 holdout; its result is recorded separately in `Stage3HoldoutAudit.md`. This document does not freeze Baseline v2.
