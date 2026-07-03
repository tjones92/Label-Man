# Revenue and release-format substitution audit

Measured 2026-07-02 on Godot 4.7 Mono. Validation used 52-week runs with seeds 1001, 1002, and 1003 against the accepted `CapabilityTierDistributionDealAudit` baseline.

## Phase 0 confirmation

No drift was found before implementation.

- `MarketRegion` still retains `youthPercentage`, `GetBuyingPopulationPercentage`, `GetGenreMarketSize`, and the year-evolution path from `GetYearEvolution` into `currentGenreAcceptance`.
- `CompetitorManager.CalculateLabelRevenue` still computes retail gross, COGS, distribution skim, artist royalty, and label net per record, then accumulates the existing `AILabel` weekly finance fields. The missing layer was only a market-wide rollup.
- Market net revenue is explicitly defined as **sum of `weeklyNetRevenue` plus sum of `weeklyDistributionIncome` across labels**. Distribution income remains a separate output column so routed skim is inspectable.
- The release gates remain intentionally unreconciled: `CalculateWeeklyReleaseChance` uses a flat 10-week roster count, while `AILabel.GetArtistForRelease` enforces the actual 8/10/12-week career-state cooldown. `RosterManager.GetArtistForRelease` remains a pass-through. A release still resets `artist.weeksSinceLastRelease` to zero, so the artist cooldown is the shared release-capacity constraint future formats will consume.

## Implemented architecture

- `Record` now carries `ReleaseFormat`; the only live value is `Single`.
- Format pricing is exported and keyed by format name. `Single` remains $0.89 and `Album` is reserved at $3.98 without introducing an album format or entity.
- The existing per-record finance loop now also accumulates read-only telemetry by label and format. Gross and label net are assigned to the owning label; routed distribution income is assigned to the distributor. Tier is resolved from the label at CSV capture time, matching the existing finance/tier cadence.
- `TryReleaseRecord` selects the artist, calls the single explicit `DecideRelease(label, artist, year)` fork, and then generates the planned format. The decision currently always returns `Single` and consumes no randomness.
- Existing CSV schemas, including `label-finance.csv`, were not changed. Two outputs were added: `market-revenue.csv` and `release-capacity.csv`.

## Market revenue schema

`market-revenue.csv` uses:

```text
period,week,year,labelTier,releaseFormat,totalMarketUnits,gross,labelNet,distributionIncome,marketNet
```

It emits weekly and annual rows for `All/All`, each tier with all formats, each format across all tiers, and each tier/format intersection. Units and revenue therefore appear side by side in one series. Annual rows use the same accumulate-and-flush boundary pattern as concentration telemetry.

Seed 1001 sample total rows:

```csv
weekly,26,1960,"All","All",3244328,2719299.660049,1497251.877002,1240.232666,1498492.109668
annual,,1960,"All","All",154810982,136105309.658338,75235207.373569,21882.643265,75257090.016834
```

Because only singles exist, every `All/All` annual value exactly equals its `All/Single` counterpart.

## Reconciliation

For seed 1001, week 26, the market row was accumulated directly from the same in-memory label fields written by `label-finance.csv`:

| Component | Market row | Sum of serialized label rows | Serialization delta |
|---|---:|---:|---:|
| Gross | $2,719,299.660049 | $2,719,299.596880 | $0.063169 |
| Label net | $1,497,251.877002 | $1,497,251.963830 | -$0.086828 |
| Distribution income | $1,240.232666 | $1,240.233000 | -$0.000334 |
| Combined market net | $1,498,492.109668 | $1,498,492.196830 | -$0.087162 |

The underlying aggregation is exact. The sub-dollar displayed deltas arise because each of 600 per-label values is independently rounded to six decimal places in the unchanged finance CSV, while the market row rounds only once after summation.

## Release-gate mismatch

`release-capacity.csv` records every random release roll that fires, successful releases, all `TryReleaseRecord` failures, and the subset that fails at actual artist selection. The latter is the observable disagreement between the flat label-level availability count and the real candidate filter. Other failures are finance/reserve failures after a valid artist was selected.

| Seed | Fired rolls | All failed rolls | Failed/week | Failed rate | Null-candidate mismatches | Mismatches/week | Mismatch rate |
|---:|---:|---:|---:|---:|---:|---:|---:|
| 1001 | 4,459 | 20 | 0.3846 | 0.4485% | 14 | 0.2692 | 0.3140% |
| 1002 | 4,539 | 47 | 0.9038 | 1.0355% | 30 | 0.5769 | 0.6609% |
| 1003 | 4,622 | 27 | 0.5192 | 0.5842% | 16 | 0.3077 | 0.3462% |

No failed roll creates a release or bypasses artist cooldown. The gate was measured, not changed.

## Before/after guardrails

All nine pre-existing aggregate CSVs were byte-identical before and after for every seed. Consequently the established baseline calculations have zero delta.

| Seed | Annual units before | Annual units after | Delta | Week-52 active before | Week-52 active after | Closed Top-40 median before/after | Charted zombies before/after |
|---:|---:|---:|---:|---:|---:|---:|---:|
| 1001 | 154,810,982 | 154,810,982 | 0.000% | 1,693 | 1,693 | 9 / 9 | 0 / 0 |
| 1002 | 158,812,169 | 158,812,169 | 0.000% | 1,661 | 1,661 | 10 / 10 | 0 / 0 |
| 1003 | 165,617,751 | 165,617,751 | 0.000% | 1,689 | 1,689 | 9 / 9 | 0 / 0 |

The accepted seed-1001 soft guards are likewise unchanged: Independent age-14 charting remains 11/1,020 and Boutique remains 4/429. The pre/post `label-finance.csv` files are byte-identical for all three seeds, covering gross, COGS, skim, royalty, net, and distribution income.

## Reproducibility

Two independent final seed-1001 processes were byte-identical across all 12 emitted CSVs. This includes both new outputs:

- `market-revenue.csv`: SHA-256 `765897841BCB1C62225EF2A7861CF46ACC282FE206A10BADCF895330612A3866`
- `release-capacity.csv`: SHA-256 `8B84BBFDBA5F8B38729F29A1FDF14F8F7598A7AF18DCBAD053BC6C8238A9A461`

`dotnet build "Label Man.sln" --no-restore` succeeds with no errors (two unchanged unused-event warnings in `ChartManager`).
