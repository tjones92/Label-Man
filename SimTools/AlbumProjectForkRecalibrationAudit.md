# Directive 3B-A2 fork recalibration audit

Measured 2026-07-04 with Godot 4.7 Mono over 52 weeks. Checkpoint A2 **failed** because youth Album choice remained below 2% in every seed. Checkpoint B was not started.

## Files changed

- `Systems/CompetitorManager.cs`: replaced the polynomial Single tail with the frozen empirical table, preserved the Album cost/proxy rules and RNG order, recalibrated `priorUnitScalarAlbum`, and exposed decision diagnostics.
- `SimTools/ChartAuditRunner.cs`: added the diagnostic-only calibration stream and the required `fork-ratios.csv` stream without changing existing CSV schemas.
- `SimTools/fit-a2-single-prior.mjs`: reproducible six-run fit, fixed cut points, sparse-bucket borrowing, and epsilon accounting.
- `SimTools/analyze-a2.mjs`: A2 format, error, censoring, fork, and conditional-guard analysis.
- `SimTools/analyze-album-project-pipeline.ps1`: renamed the completed quality-versus-units statistic `qualityUnitsCorrelationDiagnosticOnly`; it is not the Pearson guard.
- `SimTools/AlbumProjectPipelineDirective.md`: removed the retired absolute singles bands from the future Checkpoint-A template.

## Exported values and implementation contract

| Value | Start | Final |
|---|---:|---:|
| `priorUnitScalarAlbum` | 100,000 | 240,000 |
| `priorAssumedAlbumPackaging` | 0.50 | 0.50 |
| `compilationProductionMultiplier` | 0.60 | 0.60 |
| `priorSingleExpectedTailUnitScalar` | 300,000 | removed |
| `priorUnitScalarSingle` | 12,000 | removed; superseded by table |
| normalization epsilon | new | 0.000001 |

The album-disabled return precedes quality-quartile/career lookup, Album prior, memory, and decision noise. Enabled evaluation remains deterministic Single prior, deterministic Album prior, memory blend, one Single noise draw, then one Album noise draw. The table lookup and `HasFourResolvableSingles` are read-only and consume no RNG.

Unexpected `Dropped` and `Declining` states were excluded from fitting. At runtime they use the conservative New/Unsigned table column and are labeled `New/Unsigned (unexpected-state fallback)` in fork diagnostics rather than being silently mixed with fitted rows.

## Empirical Single table

Calibration used completed, memory-eligible Singles joined by exact `recordId` from album-disabled BASELINE and REF-3A seeds 1001/1002/1003. The pooled eligible population was 14,345. Fixed interpolated quartile cut points were:

| Q1/Q2 | Q2/Q3 | Q3/Q4 |
|---:|---:|---:|
| 0.465511 | 0.550559 | 0.623968 |

The normalization epsilon was invoked zero times. Excluded unexpected states were Dropped `N=109` and Declining `N=11`.

Each cell is `effective normalized contribution / raw N / source N`. Asterisks are borrowed cells.

| Quality | New/Unsigned | Rising | Established | Star/Superstar |
|---|---:|---:|---:|---:|
| Q1 | $7,765.792 / 3,481 / 3,481 | $12,767.924 / 105 / 105 | $12,767.924* / 3 / 105 (Q1 Rising) | $12,767.924* / 0 / 105 (Q1 Rising) |
| Q2 | $12,230.041 / 3,452 / 3,452 | $34,084.882 / 124 / 124 | $34,084.882* / 8 / 124 (Q2 Rising) | $34,084.882* / 0 / 124 (Q2 Rising) |
| Q3 | $19,197.010 / 3,380 / 3,380 | $39,072.383 / 191 / 191 | $119,155.241* / 16 / 22 (Q4 Established) | $39,072.383* / 0 / 191 (Q3 Rising) |
| Q4 | $47,135.125 / 3,156 / 3,156 | $84,751.250 / 407 / 407 | $119,155.241 / 22 / 22 | $119,155.241* / 0 / 22 (Q4 Established) |

Runtime calculation is exactly `bucket mean * max(0, distributionStrength) * genreSinglesMarketFactor - current Single production cost`. Margin, modifiers, and production are not applied twice.

### Calibration replay fidelity

| Configuration | Seed | Decisions | Singles | Completed eligible Singles |
|---|---:|---:|---:|---:|
| BASELINE | 1001 | 4,439 | 4,439 | 2,746 |
| BASELINE | 1002 | 4,492 | 4,492 | 2,831 |
| BASELINE | 1003 | 4,595 | 4,595 | 2,906 |
| REF-3A | 1001 | 4,096 | 3,186 | 1,926 |
| REF-3A | 1002 | 4,273 | 3,339 | 2,028 |
| REF-3A | 1003 | 4,434 | 3,407 | 2,028 |

These cohort counts reproduce the authoritative probe. Instrumented BASELINE seed 1001 retained both canonical hashes. The shipped enabled seed-1001 repeat was identical across all 24 CSVs. Moving final lookup classification behind the disabled return also reproduced all 24 prior measured seed-1001 CSVs byte-for-byte.

## Album-arm calibration evidence

At the starting scalar 100,000, Album choice was only 1.07%, 0.89%, and 1.15%, while completed-Single final signed error already passed at -$578, +$325, and +$380. The Single table was therefore frozen.

The retained Checkpoint-A lower-bound Album errors at scalar 100,000 were -$1,914, -$195, and -$813 with no completed Albums. The new runs provide completed Album outcomes. Scalar 230,000 still missed overall mix in seeds 1002/1003; 240,000 is the smallest tested scalar that passed overall and adult mix in all seeds. It overpredicts completed Albums by $5,555-$7,492, while lower-bound substitution produces a positive $9,628-$9,932 ceiling. These views are not a two-sided gate: 720-748 selected Albums per seed remain live, and substituting lower bounds can only bound the signed error from above.

## A2 validation

| Seed | Decisions | Album choice | Adult Album | Youth Album | Youth Comp. | Adult album-chart | Adult singles-chart | Single final error (N) |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 1001 | 4,349 | 20.14% (876) | 66.35% (619/933) | **0.37% (8/2,153)** | 8/8 | 98.40% | 20.98% | -$656 (2,127) |
| 1002 | 4,316 | 19.14% (826) | 65.74% (589/896) | **0.26% (6/2,304)** | 6/6 | 98.93% | 19.44% | -$81 (2,129) |
| 1003 | 4,340 | 19.24% (835) | 64.97% (573/882) | **0.55% (13/2,375)** | 13/13 | 98.30% | 19.10% | +$614 (2,168) |

All unconditional numeric gates pass except youth Album choice. Generated youth Albums are unanimously Compilation, but the required 2%-15% choice band fails. Therefore format mix fails and the conditional singles statistics below are reported but not applied as independent gates.

| Seed | Single prior error | Single final error | Live peak Pearson (N) | Closed Top-40 median (N) | Completed Album error (N) | Album lower-bound ceiling (N) |
|---:|---:|---:|---:|---:|---:|---:|
| 1001 | -$142 | -$656 | 0.485 (962) | 11 (208) | +$6,472 (128) | +$9,789 (876) |
| 1002 | +$396 | -$81 | 0.571 (954) | 11 (200) | +$7,492 (102) | +$9,627 (826) |
| 1003 | +$1,102 | +$614 | 0.602 (966) | 11 (185) | +$5,555 (107) | +$9,932 (835) |

The only Pearson guard is the live peak-based statistic above. `qualityUnitsCorrelationDiagnosticOnly` remains available only as a completed-lifecycle diagnostic.

### Single final error by fixed quality bucket and career band

Cells are `mean signed error (N)`; blank cells had no completed sample. `Fallback` contains unexpected career states and is not a fitted band.

| Seed | Bucket | New/Unsigned | Rising | Established | Fallback |
|---:|---|---:|---:|---:|---:|
| 1001 | Q1 | +$36 (390) | +$3,543 (15) |  | +$1,420 (3) |
| 1001 | Q2 | +$306 (507) | +$6,830 (34) | +$9,443 (1) | -$1,039 (3) |
| 1001 | Q3 | -$214 (505) | +$4,003 (38) | +$30,779 (4) | -$1,772 (6) |
| 1001 | Q4 | -$2,334 (545) | -$10,169 (67) | -$49,915 (3) | +$14,484 (6) |
| 1002 | Q1 | +$38 (430) | +$470 (21) |  | +$890 (1) |
| 1002 | Q2 | +$380 (437) | +$3,426 (29) | -$3,356 (2) | +$926 (3) |
| 1002 | Q3 | +$1,677 (512) | +$10,497 (36) | +$24,351 (5) | +$4,127 (4) |
| 1002 | Q4 | -$4,410 (566) | +$6,734 (77) | +$33,804 (3) | +$13,260 (3) |
| 1003 | Q1 | -$5 (426) | +$1,453 (10) |  | +$1,952 (5) |
| 1003 | Q2 | +$1,166 (409) | +$1,437 (40) |  | -$771 (5) |
| 1003 | Q3 | +$498 (504) | -$11,991 (44) | +$35,079 (5) | +$4,803 (5) |
| 1003 | Q4 | -$48 (612) | +$10,609 (93) | -$17,920 (7) | +$7,739 (3) |

Sparse Established results remain unstable; no completed Star/Superstar Single sample was available.

## Fork diagnostics

Each row reports `N; mean prior Single/Album; mean final Single/Album; mean Album-Single difference; Album choices; undefined ratios` in dollars. Other contains genres outside the established adult and youth definitions.

### Seed 1001

| Group / career | N | Prior S / A | Final S / A | Difference | Album choice | Undefined |
|---|---:|---:|---:|---:|---:|---:|
| Adult New/Unsigned | 740 | 12,521 / 15,223 | 13,802 / 15,152 | +1,350 | 546 (73.8%) | 3 |
| Adult Rising | 169 | 40,202 / 27,275 | 37,208 / 27,008 | -10,201 | 71 (42.0%) | 0 |
| Adult Established | 18 | 96,263 / 31,181 | 76,286 / 30,084 | -46,201 | 0 | 0 |
| Adult Star/Superstar | 1 | 25,273 / 33,518 | 73,492 / 39,086 | -34,405 | 0 | 0 |
| Adult Fallback | 5 | 22,834 / 16,761 | 17,612 / 17,037 | -575 | 2 (40.0%) | 0 |
| Youth New/Unsigned | 1,801 | 12,225 / 1,914 | 11,457 / 1,900 | -9,557 | 8 (0.4%) | 207 |
| Youth Rising | 225 | 41,512 / 4,706 | 33,748 / 4,761 | -28,987 | 0 | 8 |
| Youth Established | 12 | 71,777 / 8,031 | 57,137 / 7,731 | -49,406 | 0 | 0 |
| Youth Star/Superstar | 5 | 124,196 / 22,542 | 151,066 / 23,288 | -127,778 | 0 | 0 |
| Youth Fallback | 110 | 11,295 / 1,821 | 9,709 / 1,741 | -7,968 | 0 | 14 |
| Other New/Unsigned | 1,106 | 8,190 / 1,982 | 8,116 / 1,786 | -6,330 | 236 (21.3%) | 556 |
| Other Rising | 97 | 24,044 / 3,283 | 20,392 / 2,972 | -17,420 | 6 (6.2%) | 48 |
| Other Established | 1 | 3,062 / -5,550 | 2,560 / -5,405 | -7,965 | 0 | 1 |
| Other Fallback | 59 | 8,002 / 478 | 6,710 / 417 | -6,293 | 7 (11.9%) | 38 |

### Seed 1002

| Group / career | N | Prior S / A | Final S / A | Difference | Album choice | Undefined |
|---|---:|---:|---:|---:|---:|---:|
| Adult New/Unsigned | 711 | 12,830 / 14,474 | 12,942 / 14,482 | +1,540 | 517 (72.7%) | 3 |
| Adult Rising | 163 | 43,427 / 28,980 | 38,444 / 29,114 | -9,330 | 66 (40.5%) | 0 |
| Adult Established | 15 | 100,752 / 49,334 | 92,355 / 50,738 | -41,617 | 3 (20.0%) | 0 |
| Adult Fallback | 7 | 18,001 / 8,850 | 11,194 / 8,482 | -2,712 | 3 (42.9%) | 0 |
| Youth New/Unsigned | 1,929 | 13,170 / 1,784 | 12,549 / 1,758 | -10,791 | 5 (0.3%) | 240 |
| Youth Rising | 244 | 46,977 / 4,682 | 39,576 / 4,634 | -34,941 | 0 | 5 |
| Youth Established | 22 | 62,397 / 6,646 | 60,069 / 6,458 | -53,610 | 1 (4.5%) | 0 |
| Youth Star/Superstar | 1 | 111,653 / 20,976 | 102,484 / 23,052 | -79,432 | 0 | 0 |
| Youth Fallback | 108 | 11,272 / 1,438 | 8,483 / 1,361 | -7,122 | 0 | 17 |
| Other New/Unsigned | 966 | 8,353 / 2,225 | 7,945 / 1,963 | -5,981 | 222 (23.0%) | 507 |
| Other Rising | 104 | 26,385 / 4,141 | 21,114 / 3,768 | -17,346 | 8 (7.7%) | 47 |
| Other Established | 3 | 22,949 / 3,507 | 17,414 / 723 | -16,691 | 0 | 2 |
| Other Fallback | 43 | 8,964 / 2,781 | 7,936 / 1,722 | -6,214 | 1 (2.3%) | 21 |

### Seed 1003

| Group / career | N | Prior S / A | Final S / A | Difference | Album choice | Undefined |
|---|---:|---:|---:|---:|---:|---:|
| Adult New/Unsigned | 693 | 13,822 / 15,794 | 13,868 / 15,823 | +1,954 | 495 (71.4%) | 0 |
| Adult Rising | 171 | 38,581 / 27,547 | 35,311 / 27,610 | -7,701 | 72 (42.1%) | 0 |
| Adult Established | 11 | 99,321 / 41,572 | 81,024 / 43,989 | -37,035 | 3 (27.3%) | 0 |
| Adult Fallback | 7 | 32,704 / 22,085 | 29,565 / 22,246 | -7,319 | 3 (42.9%) | 0 |
| Youth New/Unsigned | 2,016 | 13,615 / 1,956 | 12,980 / 1,941 | -11,039 | 12 (0.6%) | 279 |
| Youth Rising | 223 | 47,948 / 5,115 | 39,076 / 5,135 | -33,941 | 0 | 2 |
| Youth Established | 23 | 83,498 / 9,457 | 72,553 / 8,926 | -63,626 | 0 | 0 |
| Youth Star/Superstar | 5 | 94,924 / 16,494 | 134,080 / 14,595 | -119,485 | 0 | 0 |
| Youth Fallback | 108 | 12,873 / 1,815 | 11,367 / 1,848 | -9,519 | 1 (0.9%) | 16 |
| Other New/Unsigned | 914 | 8,443 / 2,366 | 8,655 / 2,282 | -6,373 | 234 (25.6%) | 410 |
| Other Rising | 114 | 26,182 / 8,496 | 24,578 / 8,430 | -16,147 | 11 (9.6%) | 27 |
| Other Established | 9 | 41,815 / 8,693 | 29,804 / 8,276 | -21,529 | 0 | 3 |
| Other Fallback | 46 | 10,782 / 2,796 | 10,043 / 2,481 | -7,562 | 4 (8.7%) | 25 |

Undefined ratios were 875/4,349 (20.1%), 842/4,316 (19.5%), and 762/4,340 (17.6%). The signed difference remains defined for every row.

The cohort preventing the full target is youth, specifically the Album arm: youth New/Unsigned deterministic Album priors average only $1,784-$1,956 versus $12,225-$13,615 for Single. This is descriptive, not proof from a ratio alone. The four-resolvable-single proxy was true for only 1/876, 1/826, and 3/835 chosen Albums, while actual Compilation counts were 560, 532, and 558. No youth row with the proxy supplied a useful sanity sample; actual youth Albums were nevertheless all Compilation.

## Baseline, determinism, build, and stop decision

| Check | Result |
|---|---|
| Album-disabled annual units | 154,810,982 exact |
| `market-revenue.csv` SHA-256 | `765897841BCB1C62225EF2A7861CF46ACC282FE206A10BADCF895330612A3866` |
| `release-capacity.csv` SHA-256 | `8B84BBFDBA5F8B38729F29A1FDF14F8F7598A7AF18DCBAD053BC6C8238A9A461` |
| Enabled seed-1001 independent processes | all 24 CSVs byte-identical |
| Shipped lookup-order verification | all 24 CSVs byte-identical to measured seed 1001 |
| `dotnet build "Label Man.sln" --no-restore` | succeeds; 0 errors; pre-existing unused `ChartManager.OnGenreMomentumChanged` warning only |
| Godot runs | complete; pre-existing `MissingSingletonsTemp.cs` autoload warning follows completion |

Checkpoint A2's sole unconditional A4.1 failure is the youth Album-choice gate. Per A5, the empirical Single table was not retuned, no singles-side or frozen constants were changed, conditional singles statistics were not used as tuning targets, and Checkpoint B was not begun.
