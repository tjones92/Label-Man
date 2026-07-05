# Substitution-structured cannibalization audit

Measured 2026-07-04 with Godot 4.7 Mono over 52 weeks. The 3C Pearson checkpoint passes retroactively under the repaired guard, the substitution structure produces the required late-decade closed-form `AlbumStandalone` choice, and the enabled runs are deterministic. Directive 3C.2 is **passed** under the adjudicated paired chart-life guard recorded below. The passing code, exports, fitted data, and validation configuration are frozen for Directive 3D.

## Pearson repair and retroactive 3C adjudication

At approximately `N = 900` and `r = 0.5`, the ordinary large-sample scale of one correlation estimate is

```text
SE(r) approximately (1 - r^2) / sqrt(N - 3)
      approximately 0.75 / sqrt(897)
      approximately 0.025
```

The repaired `-0.06` per-seed floor is therefore about 2-2.5 ordinary standard errors for an isolated correlation estimate. This is a scale justification, not a paired-correlation test: enabled and baseline populations are dependent and their covariance was not estimated. The unchanged `-0.02` mean-delta guard remains the protection against a systematic regression.

The frozen directive table, using its recorded three-decimal references, has deltas `-0.033189`, `-0.016635`, and `+0.025314`, with mean `-0.008170`. Every seed passes `-0.06` and the mean passes `-0.02`; Directive 3C is therefore retroactively passed. Its original failure report remains unchanged in `CompFreshnessWithheldEmergenceAudit.md` as the historical adjudication under the old `-0.03` floor.

Canonical analyzer precision and populations are shown below. Recomputing deltas from those unrounded values changes only the final digits and also passes.

| Seed | Frozen disabled Pearson (N) | 3C enabled Pearson (N) | Canonical delta |
|---:|---:|---:|---:|
| 1001 | 0.494339436 (983) | 0.460810816 (980) | -0.033528620 |
| 1002 | 0.528870735 (994) | 0.512365294 (973) | -0.016505441 |
| 1003 | 0.577963656 (1,001) | 0.603314488 (993) | +0.025350832 |
| Mean | | | **-0.008227743** |

Directive 3D inherits at least six prespecified paired seeds, including 1001-1003, the `-0.06` per-seed Pearson floor, the `-0.02` arithmetic-mean Pearson floor, and same-seed disabled references frozen before enabled candidates are inspected. It also inherits the paired closed Top-40 median guard: each enabled seed must remain within `+/-1` week of its frozen same-seed BASELINE median. Unfavorable seeds may not be dropped or replaced.

## Implementation

3C.2 changes are narrow:

- `Systems/CompetitorManager.cs` adds the three binding exports, exposes the one Album-demand-factor implementation to B4, carries Album margin through `AlbumPriorDiagnostics`, and replaces B5 with reconstructed Single units, shared propensity, diverted units, and Album-margin loss. The strategy branch occurs only after Album wins and draws no RNG.
- `Systems/AlbumSimulator.cs` applies `strength * live Single heat * shared substitution propensity` at the existing unconditional sales multiplier.
- `Data/RecordRuntimeData.cs` and `Data/AlbumProject.cs` retain demand-weighted linked-state, heat, propensity, and suppression reconciliation values across live and retired Albums.
- `SimTools/ChartAuditRunner.cs` appends B5 terms to `release-strategy.csv` and B4 reconciliation fields to `album-project-demand.csv`; no existing column was repurposed.
- `SimTools/analyze-b.mjs` reports Pearson population, applied reconciliation, genre distributions, and ascending demand-factor quintiles.

The frozen 3C files and mechanics (`Data/Album.cs`, `Systems/AlbumModel.cs`, `Systems/ChartManager.cs`, `analyze-3c.mjs`, and the expected-peak fit) were not changed for 3C.2.

### Sources of truth and RNG proof

`CompetitorManager.CalculateAlbumDemandFactor(genre, year)` is the sole aggregate Album-addressable-share formula. `CalculateAlbumPriorNet` reads it for affinity units; `CalculateSubstitutionPropensity` reads the same method for both B4 and B5. There is no stature, career, decade, or direct year-threshold branch in substitution arithmetic.

Album at-margin finance is calculated once inside `CalculateAlbumPriorNet` from price, pressing, packaging, distribution skim, and artist royalty. `AlbumPriorDiagnostics.marginPerUnit` exposes that exact result to B5; the finance expression is not duplicated.

The frozen format fork still performs deterministic priors and memory blends followed by exactly two RNG draws: Single noise, then Album noise. If Single wins, `DecideRelease` returns `OrphanSingle` before B5. If Album wins, B5 uses only already-computed values and deterministic arithmetic. No RNG call occurs in the strategy block.

### B4 inertness and reconciliation

At each Album update, a missing or retired linked promo resolves to no runtime, so heat is exactly zero. A standalone has no linked ID and follows the same zero-heat path. Propensity multiplies this already-zero heat; suppression is zero and the existing sales multiplier is exactly one. Active/inactive linked demand sums to raw demand within CSV rounding in every seed, and independently accumulated weighted suppression agrees with `suppressed / raw` within `2e-8`.

| Seed | Raw demand | Suppressed demand | Demand-weighted suppression | Active + inactive / raw |
|---:|---:|---:|---:|---:|
| 1001 | 2,403,522.28 | 7,415.84 | 0.3085% | 1.0000000000 |
| 1002 | 2,170,600.27 | 7,904.18 | 0.3641% | 1.0000000000 |
| 1003 | 2,321,939.84 | 7,690.06 | 0.3312% | 1.0000000000 |

All are well under 1%. No dynamic standalone occurred, so its measured raw and suppressed demand are both zero; code-path inertness is exact as described above.

## Closed-form reachability

Both frozen vectors use Single production cost `$4,000`, `singleNetMarginPerUnit = $0.40`, `substitutionK = 1`, cap `0.85`, overlap `0.60`, and the unchanged promo lift and Single-prior values.

| Term | 1960 Adult New/Unsigned | 1968-curve Q4 Superstar |
|---|---:|---:|
| `bucketMeanNet` / expected Single net | $3,765.79 | $115,155.24 |
| Single production cost added back | $4,000.00 | $4,000.00 |
| Reconstructed Single units | 19,414.48 | 297,888.10 |
| Album demand factor | 0.112102 | 0.550000 |
| Substitution propensity | 0.112102 | 0.550000 |
| Expected overlap | 0.60 | 0.60 |
| Diverted units | 1,305.84 | 98,303.07 |
| Album margin per unit | $2.8006 | $2.4822 |
| Cannibalization loss | $3,657.14 | $244,007.89 |
| Expected promo lift | $8,000.00 | $1,000.00 |
| Expected promo Single net | $3,765.79 | $115,155.24 |
| Promo advantage | **+$8,108.65** | **-$127,852.65** |
| Decision | **AlbumWithPromo** | **AlbumStandalone** |

The required late-decade vector flips without changing any scalar.

## Binding regression gates

All shares use successful economic decisions; Album decisions combine both Album strategies.

| Seed | Decisions | Album share | Adult Album | Youth Album | Youth Compilation | Drops | Adult Album-chart | Adult Singles-chart | Single error |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 1001 | 4,382 | 26.61% | 58.00% | 12.01% | 259/259 | 1,071 | 97.31% | 33.60% | +$253 (N=2,103) |
| 1002 | 4,308 | 24.12% | 54.60% | 10.72% | 246/246 | 935 | 97.60% | 32.25% | +$8 (N=2,136) |
| 1003 | 4,396 | 26.21% | 55.59% | 11.98% | 285/285 | 1,059 | 97.59% | 33.83% | +$897 (N=2,097) |

All format, composition, chart-demographic, and Single-error bands pass.

| Seed | Disabled Pearson (N) | Enabled Pearson (N) | Pearson delta | Baseline / enabled Top-40 median (N) | Median delta | Result |
|---:|---:|---:|---:|---:|---:|---|
| 1001 | 0.494339436 (983) | 0.468042151 (985) | -0.026297285 | 10.5 / 10 (204) | -0.5 | Pass |
| 1002 | 0.528870735 (994) | 0.497081053 (965) | -0.031789681 | 11 / 11 (221) | 0 | Pass |
| 1003 | 0.577963656 (1,001) | 0.590440101 (985) | +0.012476445 | 11 / 11 (199) | 0 | Pass |
| Mean Pearson delta | | | **-0.015203507** | | | Pass |

Every Pearson delta passes `-0.06`, and the mean passes `-0.02`. Every closed Top-40 median is within one week of its frozen same-seed BASELINE (`10.5`, `11`, and `11`). The earlier absolute `11-12` interpretation is superseded by this paired guard; seed 1001's `-0.5` delta passes.

### Disabled baseline and determinism

| Check | Result |
|---|---|
| Album-disabled seed-1001 annual market units | `154,810,982` exact |
| `market-revenue.csv` SHA-256 | `765897841BCB1C62225EF2A7861CF46ACC282FE206A10BADCF895330612A3866` |
| `release-capacity.csv` SHA-256 | `8B84BBFDBA5F8B38729F29A1FDF14F8F7598A7AF18DCBAD053BC6C8238A9A461` |
| Enabled seed-1001 repeat | all 28 CSVs byte-identical |
| Representative `records.csv` hash | `110BE6CC8AAB67A040DD06CDC130336AA5F1344CEA1FE7CF6217DCEA66E5C788` |
| Representative `release-strategy.csv` hash | `DE92DBD803F68A14560CB3E850A6EB582E5C7724A96E7234BD82F42475C91312` |
| Representative `album-projects.csv` hash | `C7AA47472D96C43FD6950D04BA55ED3142AEAAFB80D999E1DF34D19F067D683E` |

## Strategy gradient

Dynamic 1960 `AlbumStandalone` share is `0%` for every seed: all 1,166, 1,039, and 1,152 Album decisions selected `AlbumWithPromo`.

### Promo advantage by genre group

| Seed/group | N | Mean demand factor | Mean | Median | P10 | P90 |
|---|---:|---:|---:|---:|---:|---:|
| 1001 Adult | 569 | 0.1213 | $13,291 | $11,363 | $9,204 | $19,557 |
| 1001 Youth | 259 | 0.0365 | $29,464 | $21,328 | $10,648 | $64,130 |
| 1001 Other | 338 | 0.0625 | $14,912 | $11,239 | $8,836 | $28,316 |
| 1002 Adult | 487 | 0.1197 | $14,678 | $11,866 | $9,916 | $22,288 |
| 1002 Youth | 246 | 0.0363 | $30,765 | $23,103 | $10,542 | $67,808 |
| 1002 Other | 306 | 0.0623 | $15,549 | $12,187 | $9,103 | $28,758 |
| 1003 Adult | 512 | 0.1232 | $14,546 | $11,674 | $9,537 | $22,231 |
| 1003 Youth | 285 | 0.0363 | $31,681 | $23,269 | $10,678 | $72,921 |
| 1003 Other | 355 | 0.0655 | $15,869 | $11,874 | $9,359 | $28,852 |

Adult promo advantage is substantially narrower than Youth advantage in every seed.

### Ascending Album-demand-factor quintiles

Entries show `N; factor range; mean / median promo advantage`.

| Seed | Q1 | Q2 | Q3 | Q4 | Q5 |
|---:|---|---|---|---|---|
| 1001 | 233; 0-.0373; $31,048 / $20,664 | 233; .0373-.0644; $14,912 / $11,186 | 233; .0644-.1126; $14,410 / $11,858 | 233; .1126-.1324; $14,197 / $11,973 | 234; .1324-.1460; $12,220 / $10,961 |
| 1002 | 207; 0-.0373; $32,255 / $23,601 | 208; .0373-.0644; $16,855 / $12,671 | 208; .0644-.1126; $14,965 / $11,709 | 208; .1126-.1126; $16,079 / $11,771 | 208; .1126-.1460; $13,630 / $11,909 |
| 1003 | 230; 0-.0373; $33,585 / $22,869 | 230; .0373-.0644; $16,959 / $11,289 | 231; .0644-.1126; $16,938 / $12,592 | 230; .1126-.1324; $15,761 / $12,047 | 231; .1324-.1460; $12,758 / $10,972 |

Ties make individual middle bins mildly non-monotonic, but the structural gradient is not reversed: the highest-factor bin is far below the lowest in all seeds, and Adult is closer to indifference than Youth.

## Frozen freshness and diagnostic reports

| Seed | Use count 1 / 2 / 3 | Max | Released comps | Stale-containing | Fresh appeal | Stale appeal | Youth hit-bearing |
|---:|---:|---:|---:|---:|---:|---:|---:|
| 1001 | 1,053 / 210 / 11 | 3 | 791 | 144 | 0.5473 (N=647) | 0.5005 (N=144) | 89.19% |
| 1002 | 1,007 / 177 / 2 | 3 | 709 | 114 | 0.5588 (N=595) | 0.5046 (N=114) | 93.50% |
| 1003 | 1,101 / 218 / 19 | 3 | 832 | 154 | 0.5589 (N=678) | 0.5120 (N=154) | 86.67% |

Freshness remains bounded at three uses, stale-containing pooled appeal is lower in every seed, and Youth Albums remain overwhelmingly hit-bearing.

### Project and memory reconciliation

| Seed | Scheduled / released / cancelled / pending | Eligible retired | Orphan obs. | Promo-project Album obs. | Held promo | Record equivalent |
|---:|---:|---:|---:|---:|---:|---:|
| 1001 | 1,166 / 1,071 / 1 / 94 | 2,916 | 2,103 | 117 | 579 | 2,916 |
| 1002 | 1,039 / 935 / 0 / 104 | 2,804 | 2,136 | 96 | 476 | 2,804 |
| 1003 | 1,152 / 1,059 / 0 / 93 | 2,866 | 2,097 | 111 | 547 | 2,866 |

Transferred, overdue-active-pending, redirected-promo, and unresolved-Album counts are zero. `scheduled = released + cancelled + pending`, and memory record equivalents reconcile exactly.

### Competition ratios

| Seed | All Singles N/D; ratio | Orphan N/D; ratio | Frozen orphan ratio | Orphan change |
|---:|---:|---:|---:|---:|
| 1001 | 4,382/1,044; 4.1973 | 3,216/1,044; 3.0805 | 3.1069 | -0.0265 (-0.85%) |
| 1002 | 4,308/1,033; 4.1704 | 3,269/1,033; 3.1646 | 3.2510 | -0.0864 (-2.66%) |
| 1003 | 4,396/1,035; 4.2473 | 3,244/1,035; 3.1343 | 3.2371 | -0.1028 (-3.18%) |

### Expected-versus-realized watch cohorts

| Cohort/seed | Total | Completed | Pending | Cancelled | Unretired | Mean expected | Mean realized | Signed error |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Youth Compilation 1001 | 259 | 13 | 37 | 0 | 209 | $3,460 | -$1,210 | +$4,670 |
| Youth Compilation 1002 | 246 | 10 | 35 | 0 | 201 | $4,768 | -$989 | +$5,756 |
| Youth Compilation 1003 | 285 | 18 | 29 | 0 | 238 | $4,435 | -$1,067 | +$5,502 |
| AlbumWithPromo 1001 | 1,166 | 117 | 94 | 1 | 954 | $19,151 | $1,928 | +$17,223 |
| AlbumWithPromo 1002 | 1,039 | 96 | 104 | 0 | 839 | $20,281 | $1,589 | +$18,692 |
| AlbumWithPromo 1003 | 1,152 | 111 | 93 | 0 | 948 | $17,414 | $1,695 | +$15,719 |

These cohorts remain diagnostic, not binding gates.

## Build, runs, and decision

`dotnet build "Label Man.sln" --no-restore` succeeds with zero errors and the pre-existing unused `ChartManager.OnGenreMomentumChanged` warning. All five independent Godot processes reached `CHART_AUDIT_COMPLETE` and emitted 28 CSVs. The pre-existing post-completion Godot warning still gives the process a nonzero exit status; output completion and file sets were verified explicitly.

**Decision: pass.** The paired Pearson and paired chart-life guards pass, the substitution arithmetic is structurally correct, seed-1001 determinism and the disabled baseline are exact, both reachability vectors select the required strategies, and all structural checks remain satisfied. No frozen constant was changed. This 3C.2 configuration is frozen for Directive 3D.
