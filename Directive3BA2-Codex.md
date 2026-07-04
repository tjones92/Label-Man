# Directive 3B-A2: Fork Recalibration

## Objective

Restore the `18%-28%` Album choice share by replacing the Single prior's polynomial tail term with an empirically calibrated, deterministic expectation and then rebalancing the Album prior against the best available Album evidence.

Checkpoint A2 is gated. Do not begin Checkpoint B until every unconditional Checkpoint A2 gate passes and every conditional singles guard has been evaluated under a passing target format mix.

On a Checkpoint A2 pass, execute Checkpoint B from `SimTools/AlbumProjectPipelineDirective.md`, sections B1-B8, with only the overrides listed under **Checkpoint B integration** below. Those overrides are exhaustive; all other Checkpoint B requirements remain unchanged.

## Starting point and authority

- Work from the preserved Checkpoint A implementation described in `probe-diagnostics.md` and `SimTools/AlbumProjectPipelineAudit.md`.
- Treat `probe-diagnostics.md` as authoritative for the corrected guard definitions and REF-3A measurements.
- Preserve existing user changes. Do not reset or replace the working tree to reconstruct a cleaner base.
- Demand, chart, retirement, and release-generation constants are frozen throughout this directive.
- The album-disabled branch remains the determinism baseline and must return before Album prior, memory, or decision-noise work.

## A0. Guard hygiene

### One Pearson guard

There is one Pearson guard: the live peak-based Pearson computed by `SimTools/analyze-chart-audit.mjs`.

Use exactly this population and outcome definition:

- one row per distinct record in `records.csv` that ever has `currentPosition > 0`, including records still active at week 52;
- quality = the record's first observed `quality`;
- outcome = `101 - best observed currentPosition`.

The completed-lifecycle quality-versus-lifetime-units statistic in `SimTools/analyze-album-project-pipeline.ps1` is not this guard. Rename that statistic and its output label to `qualityUnitsCorrelation` or another unambiguous name, label it diagnostic-only, and never report it as the Pearson guard.

### Rebased conditional singles guards

The old absolute bands are retired:

- Pearson `0.535-0.595`;
- closed Top-40 median `11.0-11.5`.

Remove those bands from future audit templates. Replace them with:

| Guard | Required result |
|---|---:|
| Live peak-based Pearson | at least `0.50` per seed |
| Closed Top-40 median life | `11-12` weeks per seed |

These are conditional guards because both are format-mix dependent. Apply them as gates only after all target format-mix gates pass. If format mix fails, report both statistics, mark the run failed on format mix, and do not tune any singles-side parameter to chase either statistic.

## A1. Build the empirical Single expectation

### A1.1 Calibration population

Use the six existing 52-week calibration runs only:

- album-disabled BASELINE, seeds `1001`, `1002`, and `1003`;
- REF-3A, seeds `1001`, `1002`, and `1003`.

Fit only on completed, memory-eligible Single outcomes. Join records by exact `recordId`; do not use titles, artist names, row order, or fuzzy matching.

The table must be keyed by information available when `DecideRelease` runs:

- `qualityEstimate = artist.CalculateBaseQuality()`;
- the four career bands below;
- `reachFactor = max(0, label.distributionStrength)`;
- `genreSinglesMarketFactor = CalculateSingleGenreMarketFactor(genre, year)`.

Do not use generated-record quality as a substitute for `qualityEstimate`. Generated-record quality is downstream of the decision and does not represent the value available to the prior.

The current historical CSVs may not contain all four decision-time inputs. First inspect the retained calibration artifacts. If any input is absent, add a diagnostic-only calibration stream and replay the same six configurations without changing decision, generation, economy, or RNG behavior. This is an instrumentation replay, not permission to generate a different calibration cohort. Prove the replay preserves the applicable existing checksums and enabled determinism before using it.

### A1.2 Fixed quality buckets and career bands

Pool the six calibration runs and compute three fixed cut points from the pooled decision-time `qualityEstimate` distribution. Record the numeric cut points in the audit and in the shipped table metadata. Use those same fixed cut points at runtime; do not rank-split each validation run.

Use these career bands:

- New/Unsigned: `NewSigning`, `Unsigned`;
- Rising: `Rising`;
- Established: `Established`;
- Star/Superstar: `Star`, `Superstar`.

Report any unexpected career state separately and exclude it from fitting unless its mapping is explicitly justified in the audit.

### A1.3 Normalize before applying reach and genre modifiers

Do not multiply a raw realized-net bucket mean by reach and genre factors. Realized outcomes already contain the effects of reach and genre; doing so would count those effects twice.

For each eligible completed Single outcome, calculate:

```text
realizedContributionBeforeProduction = realizedNet + sunkProductionCost

normalizedContribution = realizedContributionBeforeProduction
                       / max(reachFactor * genreSinglesMarketFactor, epsilon)
```

Use a small exported or named constant `epsilon` only as a divide-by-zero guard. Report whether it was ever invoked.

For each fixed quality-quartile x career-band bucket, store:

- mean `normalizedContribution`;
- sample count `N`;
- the source bucket if borrowing was required.

At runtime, compute the deterministic Single prior:

```text
expectedSingleContribution = bucketMeanNormalizedContribution(
    qualityQuartile,
    careerBand
) * reachFactor * genreSinglesMarketFactor

priorSingleNet = expectedSingleContribution - currentSingleProductionCost
```

Use the same Single production-cost convention as the realized outcome. Margin, production cost, reach, and genre must not be subtracted or multiplied a second time elsewhere in the Single prior.

Remove `quality^4 * priorSingleExpectedTailUnitScalar` and retire `priorSingleExpectedTailUnitScalar`. Do not leave the polynomial path available as a hidden fallback.

### A1.4 Sparse buckets

A bucket with `N < 20` must borrow from the nearest populated bucket with `N >= 20`.

Define distance on the ordered grid:

- quality: Q1, Q2, Q3, Q4;
- career: New/Unsigned, Rising, Established, Star/Superstar.

Use Manhattan distance. Break ties in this stable order:

1. same career band;
2. smallest quality distance;
3. lower career band;
4. lower quality quartile.

Ship the effective value for every bucket and metadata identifying the source bucket and its `N`. List every borrowed bucket in the audit.

### A1.5 Implementation contract

- Ship the table as explicit exported values, a read-only dictionary, or an equivalent deterministic curve with the fixed cut points beside it.
- The runtime lookup is side-effect free and consumes zero RNG draws.
- The album-disabled path remains ahead of this lookup and all memory/noise work.
- Preserve the existing enabled Checkpoint A decision-noise contract: deterministic Single prior, deterministic Album prior, memory blend, one Single noise draw, then one Album noise draw.
- Do not tune table values against the A2 format-mix results. The table is fitted once from the six calibration runs and then frozen.

## A2. Rebalance the Album arm

The Album arm has not yet been calibrated against a corrected Single arm. Recalibrate `priorUnitScalarAlbum` against the best available Album revenue evidence:

- `live-records-snapshot.csv` lower bounds from Checkpoint A;
- completed Album outcomes where available;
- Phase 2 Album sales telemetry from the `format-mix.csv` era.

Keep the Checkpoint A2 cost rules in force:

- `compilationProductionMultiplier = 0.60`;
- actual generated Album format and packaging at charge time;
- the deterministic four-resolvable-single compilation-cost proxy in the prior;
- `priorAssumedAlbumPackaging = 0.50`, unless the evidence identifies a bookkeeping error rather than a calibration preference.

`priorUnitScalarAlbum` is the format-choice calibration knob. Do not alter the frozen Single table to obtain the target mix. Do not change demand, chart, retirement, generation, or release-population constants.

Document:

- starting and final `priorUnitScalarAlbum`;
- the evidence used to choose it;
- completed Album signed error and `N`;
- live Album count and the signed-error ceiling obtained by substituting lower bounds;
- the right-censoring limitation.

A two-sided Album error gate is impossible at 52 weeks and must not be claimed.

## A3. Fork diagnostics

Emit a new diagnostic stream, `fork-ratios.csv`, rather than silently changing an existing CSV schema during Checkpoint A2.

One row per successful economic decision:

```text
week,year,recordId,labelId,artistId,genre,genreGroup,careerState,careerBand,
qualityEstimate,qualityQuartile,reachFactor,genreSinglesMarketFactor,
priorSingleNet,priorAlbumNet,projectedSingleNet,projectedAlbumNet,
albumMinusSingleNet,albumToSingleRatio,chosenFormat
```

Definitions:

- `priorSingleNet` and `priorAlbumNet` are deterministic, before memory and decision noise;
- `projectedSingleNet` and `projectedAlbumNet` are the final values after memory blending and the existing two noise draws;
- `albumMinusSingleNet = projectedAlbumNet - projectedSingleNet`;
- `albumToSingleRatio = projectedAlbumNet / projectedSingleNet` only when both projections are positive and the denominator exceeds `epsilon`; otherwise leave it blank and rely on the signed difference.

Report means and counts by genre-group x career-band for both the deterministic priors and final projections. Also report choice share and the share of rows for which the ratio is undefined.

Sanity expectations are report-only:

- adult Established acts should tend toward Album;
- youth New/Unsigned acts should tend decisively toward Single;
- the compilation-cost path should visibly narrow the Album-Single gap for youth acts with at least four resolvable singles.

Do not turn these expectations into retrospective acceptance bands.

## A4. Checkpoint A2 validation

Run 52 weeks for enabled seeds `1001`, `1002`, and `1003`, plus the album-disabled seed-1001 baseline. Run enabled seed `1001` twice in independent processes.

Use the adult and youth genre definitions already established in `SimTools/RevenueMemoryROIAudit.md` and the existing audit analyzers.

### A4.1 Unconditional hard gates

| Check | Required result |
|---|---:|
| Album-disabled annual market units | `154,810,982` exactly |
| Album-disabled `market-revenue.csv` SHA-256 | `765897841BCB1C62225EF2A7861CF46ACC282FE206A10BADCF895330612A3866` |
| Album-disabled `release-capacity.csv` SHA-256 | `8B84BBFDBA5F8B38729F29A1FDF14F8F7598A7AF18DCBAD053BC6C8238A9A461` |
| Enabled determinism | all emitted CSVs byte-identical across the two seed-1001 runs |
| Overall Album choice share | `18%-28%` of successful economic decisions |
| Adult Album choice share | `45%-75%` of successful adult decisions |
| Youth Album choice share | `2%-15%` of successful youth decisions; generated youth Albums overwhelmingly Compilation |
| Adult share of album-chart rows | at least `95%` |
| Adult share of singles-chart rows | at least `15%` per seed |
| Completed Single mean signed error | within `+/- $5,000` per seed |

Signed error is `projectedSingleNet - realizedNet`, using exact completed, memory-eligible Single outcomes joined by `recordId`. Report `N`, prior-only error, and final projected error separately; the gate applies to the existing final projected definition.

For Album error, report completed signed error and the censored lower-bound ceiling. There is no two-sided Album error gate.

### A4.2 Conditional singles guards

Only after every format-mix gate in A4.1 passes, apply:

| Check | Required result |
|---|---:|
| Live peak-based Pearson from `analyze-chart-audit.mjs` | at least `0.50` per seed |
| Closed Top-40 median life | `11-12` weeks per seed |

If any format-mix gate fails, report these values without treating them as independent tuning targets.

### A4.3 Required diagnostics

For every seed, report:

- all A4.1 and A4.2 values with numerators, denominators, and sample counts where applicable;
- Single projected-versus-realized error by quality quartile and career band;
- Album completed and censored error views;
- fork diagnostics from A3;
- actual versus assumed compilation classification and cost;
- baseline and determinism hashes.

## A5. Stop conditions

If Checkpoint A2 fails:

- do not begin Checkpoint B;
- do not retune the empirical Single table;
- do not change singles-side demand, chart, retirement, generation, or release-population constants;
- report the fork table by genre-group x career-band, including signed differences and undefined-ratio counts;
- identify which cohort and which arm prevent the target mix without claiming that a ratio alone proves causation.

The calibration order is binding:

1. fit and freeze the empirical Single table against completed outcomes;
2. verify the completed-Single error gate;
3. calibrate the Album arm against Album evidence;
4. evaluate format mix;
5. evaluate the conditional singles guards only under passing format mix.

## Checkpoint B integration

On a full Checkpoint A2 pass, execute sections B1-B8 of `SimTools/AlbumProjectPipelineDirective.md` as written. Apply only these substitutions:

1. Any reference to the "repaired A3 Single prior" means the frozen empirical Single prior defined in A1 of this directive.
2. Any reference to Checkpoint A hard gates means the A4.1 gates plus the A4.2 conditional guards under passing format mix.
3. B5 must continue to exclude marketing from all three expectations. `expectedPromoSingleNet` already includes Single production cost; do not deduct production or marketing a second time.
4. Preserve the canonical B1 promo construction rule: choose the strongest eligible track from `album.nonSingleTracks`, not an already released compilation `trackRef`.
5. Preserve the canonical B2 transfer rule: transfer is nonterminal. Reconciliation remains `scheduled = Released + Cancelled + PendingAtAuditEnd`; report transfer separately through `wasTransferred` and `transferCount`.
6. Preserve the canonical B3 validation sign: correlate the increasing `promoPeakScore`, not raw chart position, with launch awareness and stock.
7. Preserve the canonical B4 activity hook: use normalized live `radioHeat`; do not choose a different normalization during implementation.
8. Preserve both canonical B6 accounting identities. Do not equate memory-observation count with eligible physical-record retirements.
9. In B8, rerun the A4 gates exactly as defined here. If Checkpoint B breaks one, identify whether synergy, cannibalization, pipeline timing, or slot consumption moved it. Do not retune A1 or A2 to conceal the regression.

All other Checkpoint B requirements, schemas, RNG order, scheduling semantics, affordability rules, cooldown behavior, counters, survival rules, telemetry, reconciliation, and stop conditions remain unchanged.

## Required handoff

The final audit must include:

1. files changed and why;
2. starting and final exported values;
3. empirical table values, fixed cut points, bucket counts, and borrow map;
4. proof that calibration instrumentation and runtime lookup consume no RNG and preserve the required baseline;
5. every Checkpoint A2 validation table by seed;
6. the fork diagnostic tables;
7. completed and censored error views with explicit censoring caveats;
8. if Checkpoint B runs, every unchanged B8 validation item plus the A2 gate rerun;
9. build and test results, including any pre-existing warnings distinguished from new failures.
