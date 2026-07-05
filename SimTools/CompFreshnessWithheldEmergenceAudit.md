# Compilation freshness and withheld-emergence audit

Measured 2026-07-04 with Godot 4.7 Mono over 52 weeks. The implementation is coherent and deterministic, but this checkpoint **fails** one binding regression gate: enabled seed 1001's paired live-Pearson delta is `-0.033189`, below the `-0.03` per-seed floor. The implementation remains at this checkpoint without compensating calibration.

`AlbumStandalone` remained reachable in code but received zero decisions in all three 1960 samples. The specified late-decade closed-form vector also did not flip at `cannibalizationStrength = 0.15`; the term-by-term gap is reported below rather than hidden by raising the constant.

## Implementation and data model

- `ChartManager` now owns `compUseCountByRecordId`, independent of live/retired record state, plus `GetCompUseCount`, `RegisterCompUse`, and the exported `compStalenessFactor = 0.70`.
- `ReleasePreparedRecord` is the sole increment site. It increments once per `trackRefs` entry only for physically released Compilation Albums. Cancelled and pending projects do not reach it.
- Both `ResolveHitInventory` and Compilation assembly read `freshness = 0.70 ^ priorCompUses`. The former adjusts only its local hit accumulator. The latter passes locally adjusted quality values to `AlbumModel.CalculatePooledAppeal(IEnumerable<float>, ...)`.
- `Album.trackRefFreshnessApplied` and `trackRefCompUsesAtGeneration` are index-aligned with `trackRefs`. They preserve the exact generation-time values for later telemetry. `album-track-links.csv` adds `freshnessApplied` and `timesCompUsedAtGeneration`.
- `AlbumPriorDiagnostics.expectedRevenueAtMargin` is calculated before production-cost subtraction. B5 cannibalization now uses revenue at margin, predicted heat, and option (b) overlap rather than net prior and fixed heat.
- `expectedPromoHeat` was removed. `expectedOverlapWeeks = 10`, mean drop gap is 4, and overlap is `(10 - 4) / 10 = 0.60`.

No `AlbumTrack` snapshot is mutated. The checked reads are `GenerateAlbum` (local `track.quality * freshness`) and `ResolveHitInventory` (local hit contribution). `CreatePromoSingleFromAlbum` preserves the captured values while recomputing pooled appeal from floats. In particular, neither `TryResolveTrackSnapshot` nor `TryGetTrackSnapshot` callers write `quality` or `peakPosition` on the shared retired-archive instance.

## Expected peak fit and shared heat mapping

`SimTools/fit-3c-expected-peak.mjs` fits individual peak scores from nine frozen BASELINE/REF/A3 runs. Uncharted records use peak sentinel 101 and therefore score zero. Cells with fewer than 20 samples borrow the nearest populated cell using the same rule as the Single-prior fit. Godot cannot export multidimensional arrays, so the runtime export is the same 4x4 table flattened row-major.

Rows are Q1-Q4; columns are New/Unsigned, Rising, Established, Star/Superstar.

| Quality | New | Rising | Established | Star/Superstar |
|---|---:|---:|---:|---:|
| Q1 score | 0.008805 | 0.042022 | 0.177743 | 0.042022 |
| Q1 raw N | 6,708 | 344 | 14 | 0 |
| Q2 score | 0.025389 | 0.118921 | 0.177743 | 0.177743 |
| Q2 raw N | 6,986 | 532 | 24 | 0 |
| Q3 score | 0.056773 | 0.241402 | 0.405063 | 0.405063 |
| Q3 raw N | 7,218 | 650 | 73 | 0 |
| Q4 score | 0.178960 | 0.505133 | 0.739346 | 0.739346 |
| Q4 raw N | 7,379 | 1,566 | 120 | 19 |

Borrowed sources are Q1/C3 <- Q2/C3, Q1/C4 <- Q1/C2, Q2/C4 <- Q2/C3, Q3/C4 <- Q3/C3, and Q4/C4 <- Q4/C3.

`CalculatePromoPeakScore(peak, promoFlopThreshold)` is the single peak-to-heat helper. B5 converts the fitted expected score to a predicted peak and calls the helper; B3 calls the same helper on the realized promo peak at Album drop. Applied B3 awareness and stock mechanics are otherwise unchanged.

The B promo telemetry does not empirically support ten weeks in these 52-week samples: all 3,305 observed promos average 1.04 chart weeks, while the 681 that chart average 5.03. This is censored and dominated by zero-chart flops. The implementation retains the directive's explicit 10-week design default and reports the mismatch rather than relabeling it as a fit.

## Closed-form reachability

Both checks use a fully owned Major-label margin, Album price `$3.98`, assumed manufacturing `$1.06`, overlap `0.60`, and strength `0.15`. The vectors are deliberately explicit because the directive does not uniquely specify genre, label tier, reach, or hit inventory.

### (a) 1960 adult New/Unsigned

Q1 quality is `0.45`, reach and Single market factor are `1.0`, projected launch awareness is `0.20`, hit units are zero, and Album demand factor is `0.112102` (the observed mean 1960 adult New/Unsigned factor across 716 seed-1001 decisions).

| Term | Value |
|---|---:|
| Expected Album units | 8,828.04 |
| Margin per Album unit | $2.8006 |
| `expectedRevenueAtMargin` | $24,723.82 |
| Predicted heat | 0.008805 |
| Expected overlap | 0.60 |
| Cannibalization loss | $19.59 |
| Expected promo lift | $8,000.00 |
| Expected promo Single net | $3,765.79 |
| Promo-side advantage | **+$11,746.20** |

Result: `AlbumWithPromo`, as required for the early-decade vector.

### (b) Q4 Superstar at the 1968 curve

This high case uses quality `0.80`, stature `2.5`, reach `1.0`, 1968-curve Album demand factor `0.55`, the maximum four-hit contribution (`80,000` units), high projected awareness `0.90`, and Single market factor `1.0`.

| Term | Value |
|---|---:|
| Expected Album units | 272,500 |
| Margin per Album unit | $2.4822 |
| `expectedRevenueAtMargin` | $676,399.50 |
| Predicted heat | 0.739346 |
| Expected overlap | 0.60 |
| Cannibalization loss | $45,008.39 |
| Expected promo lift | $1,000.00 |
| Expected promo Single net | $115,155.24 |
| Promo-side advantage | **+$71,146.85** |

Result: it does **not** flip; it still selects `AlbumWithPromo`. Holding this vector fixed would require strength approximately `0.3871` merely to break even. That is a genuine structural gap relative to `0.15`, not an encoding error. Strength was not raised because doing so is a design decision, and the directive explicitly permits this result to be reported.

## Regression gates

Shares use successful economic decisions. Albums combine both Album strategies; physical drops are separate.

| Seed | Decisions | Album share | Adult Album | Youth Album | Youth Compilation | Drops | Adult Album-chart | Adult Singles-chart | Single error |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 1001 | 4,345 | 26.86% | 58.16% | 12.40% | 266/266 | 1,073 | 97.47% | 33.65% | +$73 (N=2,082) |
| 1002 | 4,290 | 23.38% | 54.90% | 9.78% | 224/224 | 921 | 97.60% | 34.10% | +$363 (N=2,159) |
| 1003 | 4,414 | 26.21% | 56.19% | 11.91% | 283/283 | 1,049 | 97.64% | 29.31% | +$785 (N=2,071) |

All mix, composition, adult-chart, adult-Singles-chart, Top-40 median, and Single-error bands pass. Youth Album-share changes versus Checkpoint B are `-0.22`, `-0.89`, and `-0.02` percentage points for seeds 1001-1003; all remain within 4%-15%.

| Seed | Live Pearson | Frozen baseline | Delta | Closed Top-40 median | Result |
|---:|---:|---:|---:|---:|---|
| 1001 | 0.460811 | 0.494 | **-0.033189** | 11 | **Fail** |
| 1002 | 0.512365 | 0.529 | -0.016635 | 11 | Pass |
| 1003 | 0.603314 | 0.578 | +0.025314 | 11 | Pass |

Mean paired delta is `-0.008170`, which passes the `-0.02` mean gate. Seed 1001 alone fails the `-0.03` floor. The isolated movement is associated with the enabled Album/promo population under freshness, not disabled behavior: the Album-disabled reference remains byte-exact.

### Disabled baseline and determinism

| Check | Result |
|---|---|
| Album-disabled seed-1001 annual units | `154,810,982` exact |
| `market-revenue.csv` SHA-256 | `765897841BCB1C62225EF2A7861CF46ACC282FE206A10BADCF895330612A3866` |
| `release-capacity.csv` SHA-256 | `8B84BBFDBA5F8B38729F29A1FDF14F8F7598A7AF18DCBAD053BC6C8238A9A461` |
| Enabled seed-1001 repeat | all 28 CSVs byte-identical |
| Representative `records.csv` hash | `CCCBCD46A4B3535FCEDC4B19CFC12B0CB1437E171D1862C8CC2B8546B4032B37` |
| Representative `release-strategy.csv` hash | `E6A061E6A54FF70C0DB7E4A94A6DAB844CEE6CCE6D3B07EEAEECCC839E25E481` |
| Representative `album-projects.csv` hash | `2233B004604A3ED829CA97443CB343F4E410F3B84B27656AC3CB4CC71BBEDC16` |

### Freshness reporting

The year-end distribution counts source record IDs by final Compilation-use count. “Stale Compilation” means at least one track had `timesCompUsedAtGeneration >= 1`.

| Seed | Use count 1 / 2 / 3 | Max | Released Compilations | Stale-containing | Fresh appeal mean | Stale appeal mean | Youth hit-bearing |
|---:|---:|---:|---:|---:|---:|---:|---:|
| 1001 | 1,050 / 238 / 15 | 3 | 815 | 161 | 0.5515 (N=654) | 0.5051 (N=161) | 90.98% |
| 1002 | 985 / 156 / 9 | 3 | 689 | 105 | 0.5576 (N=584) | 0.5106 (N=105) | 93.75% |
| 1003 | 1,106 / 249 / 11 | 3 | 839 | 162 | 0.5605 (N=677) | 0.5047 (N=162) | 87.99% |

Reuse is nonzero but bounded at three uses. Stale-containing Compilation pooled appeal is lower by roughly 0.047-0.056 in every seed. Selected youth Albums remain overwhelmingly hit-bearing.

### Strategy, projects, competition, and applied mechanics

All 1960 Album decisions remained `AlbumWithPromo`: 1,167, 1,003, and 1,157 by seed; `AlbumStandalone` share is 0% in each sample.

| Seed | Scheduled / released / pending | Promo score N / positive | Score-awareness corr. | Score-stock corr. | Demand-weighted suppression |
|---:|---:|---:|---:|---:|---:|
| 1001 | 1,167 / 1,073 / 94 | 1,073 / 201 | +0.7280 | +0.8174 | 2.677% |
| 1002 | 1,003 / 921 / 82 | 921 / 164 | +0.7537 | +0.8552 | 3.061% |
| 1003 | 1,157 / 1,049 / 108 | 1,049 / 183 | +0.7552 | +0.8355 | 2.749% |

Cancelled, transferred, and overdue-active-pending counts are zero. `scheduled = released + pending` exactly. Standalone suppression remains exactly zero by the unchanged demand formula; no standalone dynamic row occurred.

| Seed | All-Singles N/D; ratio | Orphan N/D; ratio | Frozen orphan ratio | Orphan change |
|---:|---:|---:|---:|---:|
| 1001 | 4,345/1,040; 4.1779 | 3,178/1,040; 3.0558 | 3.1069 | -0.0512 (-1.65%) |
| 1002 | 4,290/1,032; 4.1570 | 3,287/1,032; 3.1851 | 3.2510 | -0.0659 (-2.03%) |
| 1003 | 4,414/1,037; 4.2565 | 3,257/1,037; 3.1408 | 3.2371 | -0.0963 (-2.98%) |

### Expected-versus-realized watches

| Cohort/seed | Total | Completed | Pending | Unretired | Mean expected | Mean realized | Mean signed error |
|---|---:|---:|---:|---:|---:|---:|---:|
| Youth Compilation 1001 | 266 | 15 | 30 | 221 | $4,308 | -$989 | +$5,297 |
| Youth Compilation 1002 | 224 | 10 | 29 | 185 | $4,768 | -$994 | +$5,762 |
| Youth Compilation 1003 | 283 | 18 | 40 | 225 | $4,435 | -$1,071 | +$5,506 |
| AlbumWithPromo 1001 | 1,167 | 114 | 94 | 959 | $19,841 | $6,092 | +$13,749 |
| AlbumWithPromo 1002 | 1,003 | 96 | 82 | 825 | $21,479 | $1,575 | +$19,904 |
| AlbumWithPromo 1003 | 1,157 | 112 | 108 | 937 | $18,305 | $1,696 | +$16,609 |

Cancelled cases are zero. These cohorts remain diagnostic rather than gated.

## Build and decision

`dotnet build "Label Man.sln" --no-restore` succeeds with zero errors and the pre-existing unused `ChartManager.OnGenreMomentumChanged` warning. All five Godot processes completed; the pre-existing `MissingSingletonsTemp.cs` autoload warning appeared after completion.

**Decision: fail.** Seed 1001's live-Pearson delta misses the binding floor by `0.003189`. The new mechanics themselves are deterministic, snapshot-safe, inactive when Albums are disabled, and show the intended freshness telemetry. Withheld emergence is still absent at strength 0.15 both dynamically in 1960 and in the specified late-decade-style closed form. No constant was inflated to disguise either result.
