# Directive 3B-A3 youth compilation economics audit

Measured 2026-07-04 with Godot 4.7 Mono over 52 weeks. Checkpoint A3 **failed** at the first conditional singles guard: enabled seed 1001 live peak-based Pearson was `0.489208`, below `0.50`. All unconditional hard gates passed and the hit-inventory mechanism was strong, but the directive requires stopping. Checkpoint B was not started.

## Files changed

- `Data/AlbumTrack.cs`: carries the source Single's terminal/live peak in the existing read-only snapshot.
- `Systems/ChartManager.cs`: populates that peak from live runtime data and preserves it in the retirement archive.
- `Systems/CompetitorManager.cs`: replaces the four-resolvable cost gate with the deterministic generator-aligned weight; adds the read-only peak-scored hit term; exports `priorCompHitUnitScalar`; exposes A3 diagnostics. `GenerateAlbum` is unchanged.
- `SimTools/ChartAuditRunner.cs`: adds `a3-economic-decisions.csv`. Existing A2 columns retain their meaning; in particular, `assumedCompilationCost` still reports the legacy four-resolvable classification but no longer affects the prior.
- `SimTools/analyze-a3.mjs`: reproducible A3 cost, mechanism, error, youth-Compilation, and fork analysis.
- `SimTools/YouthCompilationEconomicsAudit.md`: this audit.

## Scalars and calibration

`priorCompHitUnitScalar` was the only new calibration knob. `priorUnitScalarAlbum` was reduced only after the hit term pushed overall mix above its band.

| Trial (seed 1001) | Album scalar | Hit scalar | Overall Album | Adult Album | Youth Album | Selected youth with charted inventory |
|---|---:|---:|---:|---:|---:|---:|
| Deterministic cost only | 240,000 | 0 | 25.41% | 71.18% | 5.62% | 4/118 (3.39%) |
| Hit trial | 240,000 | 10,000 | 30.83% | 70.30% | 13.64% | 169/292 (57.88%) |
| Selective trial | 200,000 | 20,000 | 29.54% | 61.52% | 15.25% | 295/346 (85.26%) |
| Affinity reduction | 190,000 | 20,000 | 28.40% | 59.06% | 15.95% | 307/340 (90.29%) |
| Final | **175,000** | **20,000** | **26.68%** | **56.64%** | **13.20%** | **254/281 (90.39%)** |

Starting values were `priorUnitScalarAlbum = 240000` and no hit scalar. Final values are `priorUnitScalarAlbum = 175000` and `priorCompHitUnitScalar = 20000`. `priorUnitScalarAlbum` was never increased.

## Preservation and deterministic prior

The A3 edits do not change the empirical Single table, its cut points, borrowing metadata, `CalculateSinglePriorNet`, any Singles-side logic, demand, chart, retirement, release generation, or release-population constants. The frozen values `compilationProductionMultiplier = 0.60` and `priorAssumedAlbumPackaging = 0.50` remain unchanged. The album-disabled early return remains before quality buckets, both priors, memory, and noise. Enabled evaluation remains deterministic Single prior, deterministic Album prior, memory blend, one Single noise draw, then one Album noise draw.

The shared deterministic weight is `1.00` for every non-adult generator genre, `0.48` for the six adult generator genres through 1963, and `0.00` for later adult decisions. Expected format multipliers are therefore `0.60`, `1.536`, and `2.40`. Hit resolution traverses released Single IDs in reverse, counts IDs examined, stops after four successful live/archive resolutions, reads peak only, and consumes no RNG or mutable telemetry.

## Baseline, determinism, build, and runs

| Check | Result |
|---|---|
| Album-disabled annual market units | `154,810,982` exact |
| `market-revenue.csv` SHA-256 | `765897841BCB1C62225EF2A7861CF46ACC282FE206A10BADCF895330612A3866` |
| `release-capacity.csv` SHA-256 | `8B84BBFDBA5F8B38729F29A1FDF14F8F7598A7AF18DCBAD053BC6C8238A9A461` |
| Final seed-1001 independent processes | all 25 emitted CSVs byte-identical |
| `dotnet build "Label Man.sln" --no-restore` | succeeded, 0 errors; the pre-existing unused `ChartManager.OnGenreMomentumChanged` warning remains |
| Godot runs | all completed; the pre-existing `MissingSingletonsTemp.cs` autoload warning appeared after completion |

## Gate results

All percentages use successful economic decisions. Youth selected Albums were unanimously Compilation.

| Seed | Decisions | Album choice | Adult Album | Youth Album | Youth Compilation | Adult album-chart | Adult singles-chart | Single final error (N) |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 1001 | 4,280 | 26.68% (1,142) | 56.64% (533/941) | 13.20% (281/2,128) | 281/281 | 98.45% (2,029/2,061) | 27.12% (1,410/5,200) | +$150 (2,031) |
| 1002 | 4,444 | 25.09% (1,115) | 53.92% (495/918) | 13.26% (320/2,413) | 320/320 | 97.41% (1,995/2,048) | 26.29% (1,367/5,200) | +$112 (2,187) |
| 1003 | 4,511 | 26.16% (1,180) | 54.35% (519/955) | 13.76% (335/2,434) | 335/335 | 98.59% (2,032/2,061) | 25.98% (1,351/5,200) | +$419 (2,177) |

Every unconditional gate passes. Because format mix passes, both conditional guards apply.

| Seed | Live peak Pearson (N) | Required | Closed Top-40 median (N) | Required | Result |
|---:|---:|---:|---:|---:|---|
| 1001 | **0.489208 (906)** | >=0.50 | 11 (192) | 11-12 | **FAIL** |
| 1002 | 0.505090 (919) | >=0.50 | 11 (202) | 11-12 | pass |
| 1003 | 0.578185 (939) | >=0.50 | 12 (185) | 11-12 | pass |

Pearson values above are from `SimTools/analyze-chart-audit.mjs`, using the mandated live peak population. Seed 1001 Pearson is the first failing gate. It was not used as a tuning target.

## Expected cost versus generated format

Only selected Albums have an actual generated format. All runs cover 1960, so no weight-0 later-adult row exists; binary weight-0 agreement is therefore not observable in this validation window.

| Seed | Weight 1 anticipated Compilation | Actual / agreement | Adult weight .48 actual Compilation | Non-Compilation | Deviation from 48% | Concept preemptions |
|---:|---:|---:|---:|---:|---:|---:|
| 1001 | 609 | 609 / 609 (100%) | 254/533 (47.65%) | 279/533 (52.35%) | -0.35 pp | 0 |
| 1002 | 620 | 620 / 620 (100%) | 237/495 (47.88%) | 258/495 (52.12%) | -0.12 pp | 0 |
| 1003 | 661 | 661 / 661 (100%) | 255/519 (49.13%) | 264/519 (50.87%) | +1.13 pp | 0 |

Adult non-Compilation formats were Standard/Live/Soundtrack: 201/47/31, 194/32/32, and 194/28/42. There were no Concept preemptions in these 52-week samples. The blended adult rows are distribution checks, not row-level agreement claims.

## Hit-inventory mechanism

| Seed | Selected youth with charted Single | Inventory-bearing choice rate | Hitless choice rate | Mean selected hit score |
|---:|---:|---:|---:|---:|
| 1001 | 254/281 (90.39%) | 254/395 (64.30%) | 27/1,733 (1.56%) | 0.6001 |
| 1002 | 299/320 (93.44%) | 299/485 (61.65%) | 21/1,928 (1.09%) | 0.5898 |
| 1003 | 300/335 (89.55%) | 300/483 (62.11%) | 35/1,951 (1.79%) | 0.6139 |

The mechanism exceeds the 75% soft target in every seed and does not raise hitless choices indiscriminately. Mean IDs examined and successfully resolved per decision were respectively 0.602/0.602, 0.637/0.637, and 0.606/0.606. Resolution was read-only and successful resolutions were capped at four.

## Projection and realized-net views

| Seed | Completed Album prior error | Completed Album final error | Completed N | Censored lower-bound ceiling | Ceiling N |
|---:|---:|---:|---:|---:|---:|
| 1001 | +$9,497 | +$9,747 | 130 | +$17,446 | 1,142 |
| 1002 | +$10,036 | +$10,348 | 138 | +$17,620 | 1,115 |
| 1003 | +$6,613 | +$6,801 | 135 | +$18,725 | 1,180 |

The ceiling substitutes observed lower bounds for still-live releases and is not a two-sided Album error gate.

| Seed | Completed youth Compilations | Mean realized net | Prior-only mean error | Final mean error | Early-warning flag |
|---:|---:|---:|---:|---:|---|
| 1001 | 18 | -$1,060 | +$7,501 | +$7,330 | >$5,000 |
| 1002 | 13 | -$948 | +$6,126 | +$7,065 | >$5,000 |
| 1003 | 29 | -$988 | +$5,414 | +$5,735 | >$5,000 |

These are small samples and are not generalized. All three trigger the requested warning, but A3 does not repair demand or awareness. Log for Checkpoint 3d: a future decade run must test whether negative realized youth-Compilation nets or the expected-versus-realized gap persist as revenue memory accumulates.

## Fork table, final A3

Values are dollars. `Diff` is final Album minus final Single. Undefined counts are retained.

### Seed 1001

| Group | N | Prior S/A | Final S/A | Diff | Albums | Undefined |
|---|---:|---:|---:|---:|---:|---:|
| Adult Established | 18 | 83,224 / 45,814 | 81,892 / 47,920 | -33,972 | 3 | 0 |
| Adult New/Unsigned | 740 | 12,487 / 12,318 | 13,030 / 12,239 | -791 | 459 | 1 |
| Adult Fallback | 9 | 18,950 / 10,186 | 19,040 / 9,387 | -9,653 | 2 | 0 |
| Adult Rising | 173 | 39,436 / 29,866 | 37,690 / 30,286 | -7,403 | 68 | 0 |
| Adult Star/Superstar | 1 | 121,322 / 130,779 | 91,375 / 111,410 | +20,034 | 1 | 0 |
| Other Established | 1 | 43,475 / 22,896 | 39,766 / 22,943 | -16,824 | 0 | 0 |
| Other New/Unsigned | 1,071 | 8,254 / 4,140 | 7,933 / 3,932 | -4,001 | 261 | 489 |
| Other Fallback | 49 | 8,161 / 3,851 | 5,393 / 3,492 | -1,901 | 13 | 28 |
| Other Rising | 90 | 21,960 / 27,231 | 18,971 / 26,009 | +7,038 | 54 | 11 |
| Youth Established | 10 | 72,193 / 65,483 | 55,157 / 66,377 | +11,221 | 7 | 0 |
| Youth New/Unsigned | 1,802 | 12,344 / 4,769 | 11,681 / 4,667 | -7,014 | 166 | 46 |
| Youth Fallback | 101 | 12,525 / 5,107 | 9,078 / 4,965 | -4,113 | 12 | 5 |
| Youth Rising | 215 | 49,090 / 40,665 | 41,926 / 40,113 | -1,813 | 96 | 0 |

### Seed 1002

| Group | N | Prior S/A | Final S/A | Diff | Albums | Undefined |
|---|---:|---:|---:|---:|---:|---:|
| Adult Established | 18 | 102,080 / 54,567 | 71,192 / 53,484 | -17,709 | 8 | 0 |
| Adult New/Unsigned | 722 | 13,737 / 11,673 | 13,792 / 11,616 | -2,176 | 411 | 1 |
| Adult Fallback | 8 | 8,723 / 12,584 | 13,624 / 11,535 | -2,088 | 3 | 0 |
| Adult Rising | 168 | 40,908 / 30,340 | 35,445 / 30,184 | -5,261 | 71 | 0 |
| Adult Star/Superstar | 2 | 31,905 / 77,047 | 33,918 / 70,518 | +36,600 | 2 | 0 |
| Other Established | 5 | 49,546 / 32,323 | 37,968 / 28,792 | -9,176 | 2 | 0 |
| Other New/Unsigned | 955 | 8,906 / 4,459 | 9,089 / 4,189 | -4,901 | 240 | 460 |
| Other Fallback | 52 | 10,745 / 6,999 | 9,822 / 6,348 | -3,474 | 10 | 21 |
| Other Rising | 101 | 24,968 / 25,310 | 21,449 / 22,911 | +1,462 | 48 | 17 |
| Youth Established | 18 | 80,720 / 48,187 | 56,761 / 48,385 | -8,376 | 8 | 0 |
| Youth New/Unsigned | 2,023 | 12,914 / 4,968 | 12,465 / 4,886 | -7,579 | 185 | 29 |
| Youth Fallback | 134 | 11,524 / 5,904 | 8,740 / 5,740 | -2,999 | 25 | 0 |
| Youth Rising | 238 | 44,975 / 38,120 | 41,112 / 37,573 | -3,539 | 102 | 2 |

### Seed 1003

| Group | N | Prior S/A | Final S/A | Diff | Albums | Undefined |
|---|---:|---:|---:|---:|---:|---:|
| Adult Established | 17 | 73,255 / 47,060 | 66,164 / 49,526 | -16,638 | 7 | 0 |
| Adult New/Unsigned | 758 | 12,999 / 12,414 | 12,763 / 12,417 | -346 | 443 | 0 |
| Adult Fallback | 7 | 21,901 / 12,231 | 14,787 / 11,646 | -3,142 | 3 | 0 |
| Adult Rising | 173 | 40,565 / 29,666 | 35,405 / 29,526 | -5,878 | 66 | 0 |
| Other Established | 5 | 36,690 / 34,523 | 22,850 / 26,794 | +3,944 | 2 | 1 |
| Other New/Unsigned | 965 | 8,911 / 4,663 | 8,625 / 4,483 | -4,142 | 254 | 400 |
| Other Fallback | 44 | 12,358 / 7,646 | 9,334 / 7,208 | -2,126 | 12 | 17 |
| Other Rising | 108 | 27,512 / 27,750 | 21,725 / 27,358 | +5,633 | 58 | 19 |
| Youth Established | 20 | 85,853 / 68,056 | 74,896 / 69,713 | -5,183 | 10 | 0 |
| Youth New/Unsigned | 2,055 | 13,654 / 5,122 | 13,109 / 5,052 | -8,057 | 184 | 39 |
| Youth Fallback | 100 | 12,078 / 6,953 | 10,130 / 6,353 | -3,777 | 15 | 2 |
| Youth Rising | 259 | 47,327 / 39,394 | 38,652 / 37,999 | -654 | 126 | 0 |

## Youth fork rows before and after

The authoritative A2 youth rows are retained in `AlbumProjectForkRecalibrationAudit.md`. The principal change is visible in the final Album arm and choices:

| Seed | Youth band | A2 final S/A; Albums | A3 final S/A; Albums |
|---:|---|---:|---:|
| 1001 | New/Unsigned | 11,457 / 1,900; 8 | 11,681 / 4,667; 166 |
| 1001 | Rising | 33,748 / 4,761; 0 | 41,926 / 40,113; 96 |
| 1001 | Established | 57,137 / 7,731; 0 | 55,157 / 66,377; 7 |
| 1001 | Fallback | 9,709 / 1,741; 0 | 9,078 / 4,965; 12 |
| 1002 | New/Unsigned | 12,549 / 1,758; 5 | 12,465 / 4,886; 185 |
| 1002 | Rising | 39,576 / 4,634; 0 | 41,112 / 37,573; 102 |
| 1002 | Established | 60,069 / 6,458; 1 | 56,761 / 48,385; 8 |
| 1002 | Fallback | 8,483 / 1,361; 0 | 8,740 / 5,740; 25 |
| 1003 | New/Unsigned | 12,980 / 1,941; 12 | 13,109 / 5,052; 184 |
| 1003 | Rising | 39,076 / 5,135; 0 | 38,652 / 37,999; 126 |
| 1003 | Established | 72,553 / 8,926; 0 | 74,896 / 69,713; 10 |
| 1003 | Fallback | 11,367 / 1,848; 1 | 10,130 / 6,353; 15 |

A2 Star/Superstar youth rows had no Album choices; the final A3 samples had no Star/Superstar youth decisions.

## Decision

**Checkpoint A3 fails.** All unconditional gates pass and the hit-inventory review is strong, but seed 1001 live peak Pearson fails after format mix passes. Per the stop condition, no frozen constant or Single-table value was changed to chase it, and Checkpoint B was not begun.
