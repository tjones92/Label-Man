# Codex Directive 3C.2: Pearson Gate Repair and Substitution-Structured Cannibalization

## Objective

Repair the overly tight per-seed Pearson gate that caused Directive 3C to fail by only `0.003189`, retroactively adjudicate the frozen 3C freshness checkpoint, and replace B5's revenue-scaled cannibalization estimate with a substitution-structured estimate that can produce the intended late-decade `AlbumStandalone` choice without stature or year special cases.

This is a narrow continuation of Directive 3C. Preserve the coherent 3C implementation. Do not reimplement freshness, refit expected-peak data, or tune unrelated economics.

## Sources of truth and precedence

Use, in order:

1. this directive for the Pearson repair and B4/B5 changes described below;
2. `SimTools/CompFreshnessWithheldEmergenceAudit.md` for the frozen 3C implementation and measured results;
3. `Directive3BB-Codex.md` for Checkpoint B contracts and regression gates;
4. `Directive3BA3-Codex.md` and `Directive3BA2-Codex.md` for the frozen format prior, empirical Single path, and cohort definitions;
5. `SimTools/AlbumProjectPipelineDirective.md` for unchanged B1-B8 behavior; and
6. the current working tree.

This directive supersedes those sources only where it explicitly changes the Pearson floor, B5 cannibalization estimate, or B4 applied suppression. Preserve all user changes and unrelated work.

## Part 1: Pearson gate repair and 3C adjudication

The paired live-Pearson guard continues to use only the live peak-based Pearson produced by `SimTools/analyze-chart-audit.mjs`:

```text
pearsonDelta(seed) =
    currentLivePearson(seed) - frozenAlbumDisabledLivePearson(seed)
```

The mean gate remains binding. Replace only the per-seed floor:

| Gate | Repaired requirement |
|---|---:|
| Each paired seed | `pearsonDelta >= -0.06` |
| Arithmetic mean across seeds | `meanPearsonDelta >= -0.02` |

Use the existing frozen same-seed album-disabled references. Use unrounded analyzer values when a canonical unrounded value exists; otherwise use the already frozen reference exactly as recorded. Do not recompute, refit, or select a more favorable baseline after seeing an enabled result.

### Statistical audit note

Record why the floor changed. At approximately `N = 900` charting records and `r = 0.5`, the ordinary large-sample scale of one correlation estimate is:

```text
SE(r) approximately (1 - r^2) / sqrt(N - 3)
      approximately 0.75 / sqrt(897)
      approximately 0.025
```

Thus a `-0.06` per-seed floor is roughly a two-to-two-and-a-half standard-error tolerance for an isolated seed. Treat this only as a scale justification, not as an exact paired-correlation hypothesis test: the enabled and baseline samples are dependent and their covariance is not estimated here. The unchanged `-0.02` cross-seed mean gate remains the protection against a systematic regression.

Report the actual `N` and Pearson value for both sides of every evaluated pair.

### Retroactive 3C decision

The frozen 3C results are:

| Seed | 3C live Pearson | Frozen baseline | Delta |
|---:|---:|---:|---:|
| 1001 | `0.460811` | `0.494` | `-0.033189` |
| 1002 | `0.512365` | `0.529` | `-0.016635` |
| 1003 | `0.603314` | `0.578` | `+0.025314` |

Mean paired delta is `-0.008170`. Under the repaired gate, every seed and the mean pass. Record Directive 3C as retroactively passed without changing its freshness code, counters, telemetry, fitted expected-peak table, or constants.

Do not erase or rewrite the original failure report. Preserve it as the historical result under the old gate and record the repaired adjudication in the new 3C.2 audit.

### Contract for Directive 3D

Directive 3D must use at least six prespecified paired seeds with the same structure:

- per-seed delta floor `-0.06`;
- cross-seed arithmetic mean floor `-0.02`; and
- same-seed frozen album-disabled references.

Include seeds `1001-1003` and at least three additional seeds selected before candidate results are inspected. Freeze the additional disabled references before evaluating the enabled candidate. Do not drop or replace an unfavorable seed.

## Part 2: One shared substitution propensity

Add these exported scalars:

```csharp
[Export] private float singleNetMarginPerUnit = 0.40f;
[Export] private float substitutionK = 1.00f;
[Export(PropertyHint.Range, "0,1,0.01")]
private float substitutionCap = 0.85f;
```

The defaults are binding for validation. They are exposed for future calibration, not for tuning this checkpoint.

Define:

```text
substitutionPropensity(genre, year) =
    clamp(substitutionK * albumDemandFactor(genre, year),
          0,
          substitutionCap)
```

`albumDemandFactor(genre, year)` must be the exact aggregate album-addressable share already used by the Album prior. Refactor as needed so one deterministic implementation has two readers: the Album prior and the substitution calculation. Do not copy the formula or create a near-equivalent helper.

There may be no explicit stature, career-state, decade, or year-threshold branch in the new propensity or cannibalization arithmetic. Historical change must enter only through the existing affinity-based album-demand factor.

## Part 3: Replace the B5 cannibalization estimate

Keep the frozen two-stage decision order:

1. run the existing Single-versus-Album format decision with the frozen prior, memory blend, and two noise draws;
2. if Single wins, return `OrphanSingle` and do not evaluate an Album strategy;
3. if Album wins, evaluate `AlbumStandalone` versus `AlbumWithPromo` deterministically after the format decision; and
4. consume no additional RNG in the strategy sub-decision.

### Resolve the Single-prior naming ambiguity

In the formulas below, `bucketMeanNet` means the output of the existing frozen empirical Single-prior path for this decision after its existing reach and genre-market factors and after Single production cost, but before revenue-memory blending and decision noise. In the current code this is `priorSingleNet`, returned by `CalculateSinglePriorNet(decision)`.

Do not use the noisy `projectedSingleNet`, an EMA-blended value, a realized promo result, or a future generated Single.

### New arithmetic

Replace the existing B5 debit based on Album revenue, `cannibalizationStrength`, and expected heat with:

```text
expectedSingleUnits =
    max(0,
        (bucketMeanNet + singleProductionCost)
        / max(singleNetMarginPerUnit, epsilon))

substitutionPropensity =
    clamp(substitutionK * albumDemandFactor(genre, year),
          0,
          substitutionCap)

divertedUnits =
    substitutionPropensity
    * expectedOverlapFraction
    * expectedSingleUnits

cannibalizationLoss =
    divertedUnits * albumMarginPerUnit

promoAdvantage =
    expectedPromoLift
    + expectedPromoSingleNet
    - cannibalizationLoss

projectedAlbumWithPromo =
    projectedAlbumStandalone + promoAdvantage
```

Use a small positive `epsilon` only as a division guard. With the binding default `singleNetMarginPerUnit = 0.40`, the guard must not affect normal results.

`albumMarginPerUnit` must be the existing label-specific at-margin value already calculated from Album price, pressing, packaging, distribution skim, and artist royalty conventions. Expose it through the existing Album-prior diagnostics or a shared helper; do not duplicate the finance formula.

`expectedPromoSingleNet` remains the unchanged result of `CalculateSinglePriorNet(decision)` and already includes Single production cost. Do not deduct production cost again. `expectedSingleUnits` adds that production cost back only to reconstruct the empirical expected contribution before converting it to implied units.

Keep the existing gap-adjusted overlap calculation. With `expectedOverlapWeeks = 10` and mean Album-drop gap `4`, the expected overlap remains `0.60`.

`cannibalizationStrength` must not appear anywhere in the B5 decision estimate after this change. It remains in B4 applied demand suppression only.

## Part 4: Make B4 applied suppression consistent

At the start of each Album update, compute:

```text
singleHeat = linked promo runtime exists and is not retired
    ? clamp(linkedPromo.radioHeat, 0, 1)
    : 0

substitutionPropensity =
    clamp(substitutionK * albumDemandFactor(albumGenre, currentYear),
          0,
          substitutionCap)

cannibalizationSuppression =
    clamp(cannibalizationStrength, 0, 1)
    * singleHeat
    * substitutionPropensity
```

Apply the existing unconditional sales multiplier exactly where B4 currently applies it:

```text
rawSales = rawDemandBeforeCannibalization
           * (1 - cannibalizationSuppression)
```

All existing inertness rules remain binding:

- `AlbumStandalone` has no linked promo and therefore suppression is exactly zero;
- missing, unlinked, or retired promo runtime makes `singleHeat` zero;
- propensity multiplies an already-zero term in those cases; and
- the sales multiplier is exactly one whenever suppression is zero.

Do not change awareness, conversion, inventory, capacity, chart, or RNG behavior.

Report demand-weighted suppression for 1960 by seed. The expected result is well under `1%`; this is a mechanism expectation, not permission to retune any scalar.

## Part 5: Telemetry and diagnostics

Preserve every existing 3C stream and column meaning. Add fields rather than silently repurposing old ones.

For each Album strategy decision, record at least:

- `bucketMeanNet`/`priorSingleNet` and `singleProductionCost`;
- `singleNetMarginPerUnit` and `expectedSingleUnits`;
- `albumDemandFactor`, `substitutionK`, `substitutionCap`, and final `substitutionPropensity`;
- `expectedOverlapFraction` and `divertedUnits`;
- `albumMarginPerUnit` and `cannibalizationLoss`;
- `expectedPromoLift`, `expectedPromoSingleNet`, and `promoAdvantage`;
- projected standalone and promo totals; and
- selected strategy.

For applied B4 behavior, retain enough telemetry to reconcile raw demand, suppression, suppressed demand, linked-promo state, Single heat, and substitution propensity.

### Promo-advantage distribution

For every enabled seed, report the 1960 `promoAdvantage` distribution for Adult, Youth, and Other genre groups: `N`, mean album-demand factor, mean, median, 10th percentile, and 90th percentile.

Also sort 1960 decisions into ascending album-demand-factor bins and report mean/median `promoAdvantage` per bin. The expected structural gradient is that promo advantage narrows as album demand factor rises; Adult acts should be closer to indifference than Youth acts. This is the observable 52-week precursor of the late-decade standalone flip.

If the measured gradient is materially reversed, first audit the shared demand factor, unit reconstruction, sign, and margin terms. Stop and report if the arithmetic is correct but the expected mechanism is absent; do not chase constants.

## Part 6: Validation

Run 52 weeks for enabled seeds `1001`, `1002`, and `1003`, plus the album-disabled seed-1001 baseline. Run enabled seed `1001` twice in independent processes.

Execute every unchanged Checkpoint B and 3C validation requirement. The binding regression gates are:

| Gate | Required result |
|---|---:|
| Album-disabled seed-1001 annual market units | `154,810,982` exactly |
| Album-disabled `market-revenue.csv` SHA-256 | `765897841BCB1C62225EF2A7861CF46ACC282FE206A10BADCF895330612A3866` |
| Album-disabled `release-capacity.csv` SHA-256 | `8B84BBFDBA5F8B38729F29A1FDF14F8F7598A7AF18DCBAD053BC6C8238A9A461` |
| Enabled seed-1001 determinism | All emitted CSVs byte-identical |
| Album-strategy share | `18%-28%` per seed |
| Adult Album-strategy share | `45%-75%` per seed |
| Youth Album-strategy share | `4%-15%` per seed; overwhelmingly Compilation |
| Adult share of Album-chart rows | At least `95%` per seed |
| Adult share of Singles-chart rows | At least `15%` per seed |
| Live Pearson | Delta at least `-0.06` per seed; mean delta at least `-0.02` |
| Closed Top-40 median | `11-12` weeks per seed |
| Completed Single mean signed error | Within `+/- $5,000` per seed |

Report all existing freshness distributions, stale-versus-fresh appeal, hit-bearing Youth share, project reconciliation, memory accounting, competition ratios, and expected-versus-realized watch cohorts unchanged.

Report 1960 dynamic `AlbumStandalone` share. Approximately `0%` remains an expected and acceptable result; do not tune the 1960 sample to manufacture standalone decisions.

### Closed-form reachability: term-by-term audit

Re-run both frozen vectors and report every term:

1. **1960 Adult New/Unsigned:** must select `AlbumWithPromo`.
2. **1968-curve Q4 Superstar:** must select `AlbumStandalone` under the new substitution structure.

Use the same inputs recorded in `SimTools/CompFreshnessWithheldEmergenceAudit.md` unless this directive explicitly changes a term. Show `bucketMeanNet`, reconstructed Single units, demand factor, propensity, overlap, diverted units, Album margin, cannibalization loss, promo lift, expected Single net, promo advantage, and final decision.

If vector 2 does not flip, stop and report. Do not alter `singleNetMarginPerUnit`, `substitutionK`, `substitutionCap`, `expectedPromoLiftScalar`, overlap, demand curves, or any other constant to force it.

## Frozen values and guardrails

The following remain frozen:

- the empirical Single table, quality cut points, bucket values, sparse-bucket borrowing, and `CalculateSinglePriorNet` behavior;
- `priorUnitScalarAlbum = 175000` and `priorCompHitUnitScalar = 20000`;
- compilation weights, Album production-cost rules, and Album margin conventions;
- the full 3C freshness mechanic, counters, increment site, snapshots, telemetry, `compStalenessFactor = 0.70`, and fitted expected-peak table;
- all promo-synergy behavior outside the B5 estimate described here;
- the frozen format decision and its RNG sequence;
- `cannibalizationStrength = 0.15` for B4 applied suppression;
- all demand, chart, retirement, generation, release-population, cooldown, scheduling, and memory constants; and
- all cohort definitions.

No stature or direct year conditional may be added to the new B4/B5 arithmetic. Do not modify constants to make a preferred narrative pass.

If the B4 change moves any seed's Adult or Youth format mix outside its band, stop and report rather than recalibrating Checkpoint A or the new substitution scalars.

## Required audit and decision

Write `SimTools/SubstitutionStructuredCannibalizationAudit.md`. Include:

1. the Pearson floor derivation, its limitations, and the retroactive 3C adjudication;
2. files changed and the exact behavioral purpose of each change;
3. proof that Album demand factor and Album margin each have one source of truth;
4. B5 term-by-term examples and proof that it consumes no RNG;
5. B4 standalone/unlinked/retired inertness proof;
6. the two closed-form reachability vectors;
7. baseline units, hashes, and enabled determinism hashes;
8. every regression gate by seed, including Pearson `N`, values, deltas, and mean delta;
9. 1960 dynamic strategy share and demand-weighted suppression;
10. promo-advantage distributions and demand-factor gradient;
11. all unchanged freshness and watch-cohort reports;
12. build and run results; and
13. an explicit pass/fail decision.

If any binding gate or stop condition fails, leave the implementation at the last coherent 3C.2 checkpoint and report the first failure plainly. Do not soften it into a pass or begin Directive 3D.
