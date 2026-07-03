# Revenue memory and ROI release-choice audit

Measured 2026-07-03 with Godot 4.7 Mono. The final 52-week runs used seeds 1001, 1002, and 1003; the album-disabled baseline used seed 1001.

## Implementation

- `RecordRuntimeData` now accumulates booked lifetime label net and stores the actual sunk production cost plus a runtime-only memory-eligibility flag.
- Successful generated releases become eligible only after `ChartManager.ReleaseRecord` creates their runtime object. Historical and prewarmed records remain ineligible.
- The common retirement handoff now passes `RecordRuntimeData` directly. It emits every observed outcome, removes active ownership, and updates the retiring record's current label memory only when eligible.
- Each label owns independent Single and Album EMA memories. The update uses alpha 0.30; decision confidence uses `n / (n + 4)`.
- The enabled decision compares deterministic Single and Album contribution-net priors, blends each with label-local memory, then draws exactly two projection noises in stable Single/Album order. Album wins only a strict comparison. The disabled branch still returns Single before doing any of this work.
- The analytic prior uses `CalculateBaseQuality`, the existing career-stature stock multipliers, active distribution reach/deal economics, regional album-addressable demand, existing prices/manufacturing conventions, artist royalty, and format production cost.
- `ChartAuditRunner` owns the three new output streams: `release-strategy.csv`, `release-outcomes.csv`, and `revenue-memory.csv`. Existing CSV schemas were not changed.

## Scalar calibration

The starting values were 12,000 Single and 40,000 Album. Seed 1001 produced only 0.95% albums. The final values are:

| Setting | Final value |
|---|---:|
| `priorUnitScalarSingle` | 12,000 |
| `priorUnitScalarAlbum` | 100,000 |
| `priorAssumedAlbumPackaging` | 0.50 |

The final seed-1001 share is 22.22%. Intermediate Album scalars of 92,000, 96,000, and 98,000 and paired probes of 10,000/90,000 and 14,000/110,000 stayed near the share target but did not improve the narrow singles guards consistently. Large-magnitude probes changed the format mix sharply and were rejected. No demand, retirement, chart, or generation constant was used for calibration.

## Baseline and determinism

| Check | Result | Status |
|---|---:|---|
| Album-disabled seed-1001 units | 154,810,982 | Pass |
| `market-revenue.csv` SHA-256 | `765897841BCB1C62225EF2A7861CF46ACC282FE206A10BADCF895330612A3866` | Pass |
| `release-capacity.csv` SHA-256 | `8B84BBFDBA5F8B38729F29A1FDF14F8F7598A7AF18DCBAD053BC6C8238A9A461` | Pass |
| Disabled strategy rows | Header only | Pass |
| Independent enabled seed-1001 processes | All 19 CSVs byte-identical | Pass |

The disabled result confirms that the new accounting fields, outcome event, and memory accumulation add no disabled-path RNG draw and do not alter existing finance or release-capacity output.

## Format calibration

Adult means Jazz, Easy Listening, Folk, Traditional Pop, Bossa Nova, and Country, matching the album-generation adult grouping. Youth means Rock and Roll, Teen Pop, R&B, Doo-Wop, and Girl Group.

| Seed | Successful releases | Albums | Album share | Adult Album share | Youth Album share |
|---:|---:|---:|---:|---:|---:|
| 1001 | 4,096 | 910 | 22.22% | 84.97% (684/805) | 0.05% (1/2,164) |
| 1002 | 4,273 | 934 | 21.86% | 90.89% (738/812) | 0.00% (0/2,373) |
| 1003 | 4,434 | 1,027 | 23.16% | 89.36% (773/865) | 0.04% (1/2,467) |

| Seed | Adult album-chart rows | Youth albums that were compilations |
|---:|---:|---:|
| 1001 | 99.52% (2,058/2,068) | 100% (1/1) |
| 1002 | 98.79% (2,041/2,066) | No youth album generated |
| 1003 | 99.32% (2,044/2,058) | 100% (1/1) |

All format-share, adult/youth separation, album-chart composition, and youth-compilation conditions pass.

## Singles regression guards

Quality/outcome Pearson is calculated over uncensored completed single lifecycles as record quality versus lifetime units, consistent with the existing outcome interpretation.

| Seed | Top-100 entries/week | exits/week | Closed Top-40 median life | Quality/outcome Pearson | Week-52 charted zombies | Age-14 Independent / Boutique charting |
|---:|---:|---:|---:|---:|---:|---:|
| 1001 | 20.00 | 20.00 | 11.0 | 0.402 | 0 | 20 / 5 |
| 1002 | 20.00 | 20.00 | 10.0 | 0.408 | 0 | 18 / 2 |
| 1003 | 19.77 | 19.77 | 11.0 | 0.412 | 0 | 9 / 1 |

Turnover, zombies, and age-14 tier observability pass. Seed 1002 misses the 11.0-11.5 median-life band, and all three quality/outcome correlations miss 0.535-0.595. Scalar-only probes did not remove those regressions consistently. Per the guardrails, no chart, retirement, demand, or generation mechanism was changed to chase them.

## Revenue memory and realized outcomes

The last weekly snapshot exactly matched eligible outcomes for every `(labelId, format)` pair, not merely in aggregate. This confirms that observation counts increase only for eligible retirements and remain label-local.

| Seed | Eligible retired | Ineligible startup retired | Single observations | Album observations |
|---:|---:|---:|---:|---:|
| 1001 | 2,002 | 477 | 1,926 | 76 |
| 1002 | 2,094 | 487 | 2,028 | 66 |
| 1003 | 2,093 | 482 | 2,028 | 65 |

The 1,446 retired startup records remained visible in outcomes and changed no memory.

| Seed | Format | Count | Mean realized net | Median | P10 | P90 | Positive share |
|---:|---|---:|---:|---:|---:|---:|---:|
| 1001 | Single | 1,926 | $13,412 | $3,666 | $791 | $23,643 | 97.0% |
| 1001 | Album | 76 | -$264 | -$1,431 | -$4,724 | $7,073 | 31.6% |
| 1002 | Single | 2,028 | $13,210 | $4,033 | $912 | $25,035 | 97.3% |
| 1002 | Album | 66 | -$1,235 | -$1,311 | -$4,849 | $1,110 | 31.8% |
| 1003 | Single | 2,028 | $14,048 | $3,867 | $865 | $24,806 | 97.3% |
| 1003 | Album | 65 | -$358 | -$1,637 | -$2,177 | $5,867 | 20.0% |

The distributions are strongly right-skewed: most singles are profitable with a small blockbuster tail, while the observed 1960 album cohort is usually loss-making but has occasional profitable releases.

## Projection calibration join

Every eligible outcome joined to exactly one strategy row by `recordId`. Strategy coverage is lower for albums because the album catalog retirement horizon is long; this is expected censoring in a 52-week run.

| Seed | Format | Joined / eligible outcomes | Joined / strategy releases | Mean signed error (`projected - realized`) |
|---:|---|---:|---:|---:|
| 1001 | Single | 1,926/1,926 (100%) | 60.45% | -$12,445 |
| 1001 | Album | 76/76 (100%) | 8.35% | $1,974 |
| 1002 | Single | 2,028/2,028 (100%) | 60.74% | -$12,097 |
| 1002 | Album | 66/66 (100%) | 7.07% | $3,245 |
| 1003 | Single | 2,028/2,028 (100%) | 59.52% | -$12,978 |
| 1003 | Album | 65/65 (100%) | 6.33% | $1,657 |

The analytic Single prior materially underprojects realized outcomes, while Album projections overstate the mostly loss-making retired cohort. These are calibration instruments for a later phase; 3A defines no acceptance band for them.

## Accounting limitation and build

The existing weekly ordering is preserved: `ChartManager` retires a record before `CompetitorManager.ProcessWeeklyRevenue` runs. Therefore a title's retirement-week revenue is not posted to `lifetimeLabelNet`. No synthetic settlement was added. Realized net remains booked label contribution after manufacturing, skim, and artist royalty, less sunk production cost, and excludes marketing, overhead, advances, and other-label distribution income.

`dotnet build "Label Man.sln" --no-restore` succeeds with zero errors and only the pre-existing unused `ChartManager.OnGenreMomentumChanged` warning. Headless completion continues to print the pre-existing `MissingSingletonsTemp.cs` autoload warning after `CHART_AUDIT_COMPLETE`.

Overall, the implementation, disabled baseline, determinism, format calibration, telemetry joins, and memory invariants pass. The phase remains short of a clean acceptance result because of the explicitly reported singles median-life and quality/outcome regression guards.
