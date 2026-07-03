# Directive 3A: Revenue Memory and ROI-Driven Release Choice

## Objective

Replace the supply-side album-affinity roll in `CompetitorManager.DecideRelease` with an economic choice between `ReleaseFormat.Single` and `ReleaseFormat.Album`. Each AI label should compare projected contribution net for both formats, blend those projections with its own completed-release history, add label-specific estimation noise, and choose the higher result.

This phase changes why the AI chooses a format. It does not change how either format is generated, released, stocked, simulated, charted, or retired.

## Verified starting point

- `DecideRelease` currently returns `Single` immediately when albums are disabled; otherwise it makes one `GD.Randf()` draw against `GetAlbumReleaseAffinity`.
- `CalculateLabelRevenue` already computes weekly label net after pressing/packaging COGS, distribution skim, and artist royalty as `recordRevenue`.
- Actual production cost is known in `TryReleaseRecord` only after the record has been generated, because album packaging affects its fixed cost.
- Both singles and albums retire through `ChartManager.RetireRecord(RecordRuntimeData)`.
- `ChartManager.RetireRecord` calls `CompetitorManager.RecordRetired` before removing the runtime object from `allRecords`. Pass the runtime object through directly; do not re-query it by ID and do not reorder retirement.
- `ChartManager` handles its weekly callback before `CompetitorManager` in the current scene lifecycle. A title that retires that week is therefore removed before `ProcessWeeklyRevenue`. Preserve that existing accounting behavior for baseline compatibility. In this phase, "lifetime label net" means net actually posted by `CalculateLabelRevenue`; do not synthesize a retirement-week settlement.
- `SimulatedArtist.CalculateRecordQuality()` consumes two RNG draws. It must not be used by the analytic prior. Use `CalculateBaseQuality()` so that the enabled decision path adds exactly the two projection-noise draws specified below.
- The demand-side album model is regional: `MarketRegion.GetAlbumMarketSize` combines album affinity and purchase willingness. There is no existing genre-only aggregate.
- Startup historical and prewarmed records do not have a complete simulated revenue lifetime or a recorded production cost. They may appear in outcome telemetry, but they must not train revenue memory.

## Non-goals

- No promo singles, album projects, pipelines, scheduled releases, or withheld-single logic.
- No cannibalization model.
- No compilation gating or `timesCompUsed` tagging. Compilation assembly inside `GenerateAlbum` remains unchanged.
- No changes to `GenerateAlbum`, album pooling, packaging generation, demand, chart logic, retirement thresholds, or the singles pipeline.
- No EP decision path in this phase. EP may remain in shared enums and economic dictionaries, but strategy and memory snapshots cover Single and Album only.
- No marketing-cost deduction in the learned outcome. Both the prior and realized result are contribution net after production cost but before marketing, overhead, advances, and distribution income earned from other labels.

## Task 1: Track booked lifetime net and production cost

Add runtime-only fields to `RecordRuntimeData`:

```csharp
public float lifetimeLabelNet;
public float sunkProductionCost;
public bool revenueMemoryEligible;
```

Initialize them to `0f`, `0f`, and `false` in the constructor.

In `CompetitorManager.CalculateLabelRevenue`, immediately after `recordRevenue` is computed, add it to `runtimeData.lifetimeLabelNet`. Do not change the existing cash, COGS, skim, royalty, recoupment, or telemetry routing.

In the successful `TryReleaseRecord` path:

1. Compute production cost exactly as today.
2. Call `ChartManager.ReleaseRecord(record)` as today.
3. Resolve the newly created `RecordRuntimeData` once and set `sunkProductionCost = productionCost` and `revenueMemoryEligible = true`.
4. Continue promotion and release bookkeeping unchanged.

Do not mark historical records or records created by `PopulateInitialRecords` as eligible. Their `sunkProductionCost` remains zero and their observed revenue is only a partial lifetime.

Change the common retirement handoff to pass the runtime object:

```csharp
CompetitorManager.Instance?.RecordRetired(record);
```

Update `CompetitorManager.RecordRetired` to accept `RecordRuntimeData`, remove its ID from `labelActiveRecords`, emit the outcome described in Task 5, and update memory only when `revenueMemoryEligible` is true. This one common hook must continue to receive both single retirement and album catalog retirement.

Define:

```text
realizedNet = lifetimeLabelNet - sunkProductionCost
```

Here `lifetimeLabelNet` is booked label net from the existing revenue loop. Document the known retirement-week truncation in the audit; do not alter weekly event ordering or existing finance behavior to eliminate it.

## Task 2: Add per-label format revenue memory

Add runtime-only state to `AILabel`; do not export it and do not serialize it to `.tres`:

```csharp
public Dictionary<ReleaseFormat, FormatRevenueMemory> revenueMemory = new();

public sealed class FormatRevenueMemory {
    public float emaNetPerRelease;
    public int releasesObserved;
}
```

Use a small get-or-create helper rather than assuming either format already has an entry.

Add exported settings to `CompetitorManager`:

```csharp
[Export(PropertyHint.Range, "0,1,0.01")]
private float revenueMemoryAlpha = 0.30f;

[Export(PropertyHint.Range, "0.1,20,0.1")]
private float revenueMemoryConfidenceK = 4.0f;
```

On retirement of an eligible record:

```text
ema = releasesObserved == 0
    ? realizedNet
    : lerp(emaNetPerRelease, realizedNet, clamp(revenueMemoryAlpha, 0, 1))

releasesObserved += 1
```

Resolve the current label from `runtimeData.baseRecord.labelId`. If it is null, still emit the outcome row but skip the memory update. Absorption rewrites live record label IDs and transfers active-record ownership, so crediting the complete outcome to the absorbing distributor is intended for this phase.

Memory accumulation must run whether or not `enableAlbums` is true. It performs no RNG draw.

## Task 3: Build a deterministic analytic prior

Keep the helper in `CompetitorManager` or move it to a small `ReleaseEconomics` helper if that materially improves clarity. It must be deterministic and side-effect free.

For each candidate format:

```text
expectedUnits(format) = priorUnitScalar(format)
                      * qualityEstimate
                      * statureMultiplier
                      * reachFactor
                      * demandFactor(format, genre, year)

priorNet(format) = expectedUnits(format) * marginPerUnit(format, label, artist)
                 - productionCost(format, label)
```

Use these exact inputs:

- `qualityEstimate = artist.CalculateBaseQuality()`. Do not call `CalculateRecordQuality()` here.
- `statureMultiplier` mirrors the existing `stockScale` switch in `ApplyReleasePromotion`: Superstar `2.5`, Star `2.0`, Established `1.5`, Rising `1.2`, default `1.0`.
- `reachFactor = label.distributionStrength`.
- Single demand factor is `1f`.
- Album demand factor reuses the Phase 2 regional demand model:

```text
albumDemandFactor(genre, year) =
    sum(region.GetAlbumMarketSize(genre, year))
    / max(1, sum(region.GetGenreMarketSize(genre, year)))
```

This ratio is the aggregate album-addressable share of the same genre market and therefore includes the existing regional album affinity, income/youth purchase willingness, genre acceptance, and segregation effects. Do not use or copy `GetAlbumReleaseAffinity`.

Compute unit margin with the same conventions as `CalculateLabelRevenue`:

```text
assumedPackaging = Album ? priorAssumedAlbumPackaging : 0
manufacturingPerUnit = pressingCost(format)
                     + albumPackagingCostPerUnit * assumedPackaging
grossAfterManufacturing = max(0, price(format) - manufacturingPerUnit)
skimFraction = active deal marginSkim when a deal exists
             otherwise 0.25 * (1 - ownedReach)
royaltyRate = artist.royaltyRate, falling back to baseRoyaltyRate
marginPerUnit = grossAfterManufacturing * (1 - skimFraction)
              - price(format) * royaltyRate
```

Compute production cost as:

```text
Single = label.GetProductionCost()
Album  = label.GetProductionCost() * 2.4
       + albumPackagingFixedCost * priorAssumedAlbumPackaging
```

Add exported calibration settings:

```csharp
priorUnitScalarSingle
priorUnitScalarAlbum
priorAssumedAlbumPackaging // Range 0..1; default 0.50
```

The two unit scalars are the only permitted knobs for the format-share calibration in this phase. Record their starting and final values in the audit.

## Task 4: Replace the release-format decision

The disabled path is a hard determinism guard and must remain first:

```csharp
private ReleasePlan DecideRelease(AILabel label, SimulatedArtist artist, int year) {
    if (!enableAlbums) return new() { format = ReleaseFormat.Single };

    // Evaluate Single first, then Album, in this stable order.
    // Compute deterministic priors and memory blends before drawing noise.
}
```

For each format:

```text
n = releasesObserved for that label and format, or 0
confidence = n / (n + max(0.1, revenueMemoryConfidenceK))
blended = lerp(priorNet, emaNetPerRelease, confidence)
```

Then apply projection noise in a fixed order: Single first, Album second.

```text
noiseRange = lerp(0.50, 0.15, clamp(label.scoutingAbility, 0, 1))
projectedSingleNet *= 1 + RandRange(-noiseRange, +noiseRange)
projectedAlbumNet  *= 1 + RandRange(-noiseRange, +noiseRange)
```

These are the only two RNG draws added by `DecideRelease`. Choose Album only when `projectedAlbumNet > projectedSingleNet`; ties choose Single.

Extend `ReleasePlan` to retain the two projected nets and two confidences for successful-release telemetry. Emit no strategy event until the release passes its affordability check and succeeds. When albums are disabled, the strategy CSV may remain header-only; do not calculate priors, inspect memory, or draw noise merely for telemetry.

Delete `GetAlbumReleaseAffinity` from `CompetitorManager`. Search the repository first. If any caller other than the current `DecideRelease` exists, stop and report it rather than preserving a second supply-side affinity path.

## Task 5: Add audit telemetry

Follow the current audit architecture: gameplay code exposes events or read-only snapshots; `ChartAuditRunner` owns CSV writers and file output. Do not add direct CSV file I/O to `AILabel`, `ChartManager`, or normal gameplay paths.

Add three CSVs without changing any existing CSV schema.

### `release-strategy.csv`

One row per successful, economics-evaluated release:

```text
week,year,recordId,labelId,tier,artistId,genre,careerState,projectedSingleNet,projectedAlbumNet,confidenceSingle,confidenceAlbum,chosenFormat
```

`recordId` is mandatory because validation must join this row to the eventual outcome. The two projected values are the post-memory, post-noise values actually compared by the decision.

### `release-outcomes.csv`

One row for every retirement observed by the common retirement hook:

```text
week,year,labelId,recordId,format,memoryEligible,lifetimeLabelNet,sunkProductionCost,realizedNet
```

Ineligible startup records remain visible for auditability but do not update memory and are excluded from expected-versus-realized calibration joins.

### `revenue-memory.csv`

One weekly snapshot per known label for each of `Single` and `Album`, including zero-observation rows:

```text
week,year,labelId,format,emaNetPerRelease,releasesObserved
```

Keep row ordering deterministic: label ID ordinal order, then Single, then Album. Format floats with the same invariant helper used by existing audit streams.

## Validation

Run 52-week audits for seeds `1001`, `1002`, and `1003` with albums enabled, plus the required disabled baseline.

### Baseline integrity

- Album-disabled seed `1001` must reproduce `154,810,982` units.
- Existing `market-revenue.csv` checksum must remain `765897841BCB1C62225EF2A7861CF46ACC282FE206A10BADCF895330612A3866`.
- Existing `release-capacity.csv` checksum must remain `8B84BBFDBA5F8B38729F29A1FDF14F8F7598A7AF18DCBAD053BC6C8238A9A461`.
- The disabled path must add, remove, or reorder no RNG draw.

### Determinism

Run album-enabled seed `1001` in two independent processes. All emitted CSVs, including the three new files, must be byte-identical.

### Format calibration

- 1960 Album share of successful releases: `18%` to `28%` in every seed. Phase 2 produced `22.4%` to `23.1%`.
- Calibrate only `priorUnitScalarSingle` and `priorUnitScalarAlbum`.
- Report Album share separately for adult genres and youth genres. Adult share must materially exceed youth share; do not force the old `0.58` versus `0.11` roll values as acceptance targets.
- Adult genres must account for at least `95%` of album-chart rows in every seed.
- Youth-genre albums must remain overwhelmingly compilations; report the exact share.

### Singles regression guards

- Mean weekly Top-100 entries/exits remains within the accepted Phase 2 range (`19.31` to `20.06`).
- Closed Top-40 median life remains `11.0` to `11.5` weeks.
- Quality/outcome Pearson remains `0.535` to `0.595`.
- Week 52 has zero charted zombies.
- Every seed retains nonzero age-14 Independent/Boutique charting.

### Memory and projection reporting

- Confirm `releasesObserved` increases only for eligible retired releases and that Single and Album memories remain label-local.
- Report eligible retired-release count, mean, median, and distribution sanity notes for `realizedNet`, separately by format.
- Inner-join `release-strategy.csv` to eligible `release-outcomes.csv` by `recordId`.
- For each format, report join coverage and mean signed error defined as `projectedFormatNet - realizedNet`. There is no acceptance band in 3A; this is a calibration instrument for later phases.
- Explicitly report how many retired startup records were ineligible and confirm they did not change memory.
- Explicitly note the preserved retirement-week accounting truncation described in Task 1.

If the Album-share band cannot be reached with the two unit scalars alone, stop and report the failure mode, the attempted scalar values, memory coverage, and expected-versus-realized error. Do not add another parameter or change demand-side constants.

## Guardrails and completion criteria

- Do not change retirement constants, chart logic, album demand, `GenerateAlbum`, or existing CSV schemas.
- Preserve the exact semantics of `WeeklyReleaseRollsFired`, `WeeklySuccessfulReleases`, `WeeklyFailedReleaseRolls`, and `WeeklyCooldownMismatchRolls`.
- Do not train on historical/prewarmed partial lifetimes.
- Do not introduce RNG into the prior, memory update, retirement path, or disabled decision path.
- `dotnet build "Label Man.sln" --no-restore` must succeed with no new warnings.
- Write the implementation/validation report to `SimTools/RevenueMemoryROIAudit.md`, including final scalar values, all acceptance results, known limitations, and the expected-versus-realized tables.
