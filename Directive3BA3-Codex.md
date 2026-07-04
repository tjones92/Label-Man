# Directive 3B-A3: Youth compilation economics

## Purpose and gate

Checkpoint A3 is the final gate before Checkpoint B. Fix the sole **unconditional** Checkpoint A2 failure: youth Album choice was `0.26%-0.55%`, below the new A3 target of `4%-15%`.

Do not describe every other A2 check as passed. Every other A2 unconditional gate passed, but the conditional singles guards were not adjudicated because format mix failed; the reported seed-1001 live peak Pearson was `0.485`, below `0.50`. After A3 reaches a passing format mix, all conditional guards must pass before Checkpoint B may begin.

The authorized mechanism is narrow:

1. replace the ineffective four-resolvable-single compilation-cost gate with a deterministic expected-format cost weight aligned to the current generator;
2. add a hit-inventory term to expected Album units, weighted by the same compilation probability; and
3. calibrate one new exported scalar.

If A3 passes, proceed to Checkpoint B under the integration contract in `Directive3BA2-Codex.md`. If A3 fails, stop after the required audit.

## Source of truth and preservation rules

Work from the current Checkpoint A2 implementation and use these as the authoritative prior results and contracts:

- `Directive3BA2-Codex.md`;
- `SimTools/AlbumProjectForkRecalibrationAudit.md`;
- `SimTools/AlbumProjectPipelineDirective.md`; and
- the current `Systems/CompetitorManager.cs` and audit tools.

Preserve all existing user changes. Do not revert or rewrite unrelated work.

The following are frozen:

- the empirical Single table, its quality cut points, bucket values, sparse-bucket borrowing, and runtime formula;
- all singles-side logic;
- demand, chart, retirement, generation, and release-population constants;
- `compilationProductionMultiplier = 0.60`;
- `priorAssumedAlbumPackaging = 0.50`;
- the album-disabled early return and all A2 guard-hygiene rules;
- the enabled RNG contract: deterministic Single prior, deterministic Album prior, memory blend, one Single noise draw, then one Album noise draw;
- the definition of the Pearson guard: only the live peak-based Pearson from `SimTools/analyze-chart-audit.mjs`; and
- retired artists and bands remain retired.

Do not change `GenerateAlbum` or consume RNG in the prior. Do not change demand-side awareness, pooling, chart behavior, or realized revenue in A3.

## Cohort definitions

For generator-cost weighting, use the generator's adult set exactly:

`Jazz`, `EasyListening`, `Folk`, `TraditionalPop`, `BossaNova`, and `Country`.

Every other genre is non-adult for generator-cost weighting.

For validation, retain the established audit cohorts:

- Adult: the six genres above.
- Youth: `RockAndRoll`, `TeenPop`, `RnB`, `DooWop`, and `GirlGroup`.
- Other: every remaining genre.

Do not silently treat the validation Youth cohort as equivalent to all non-adult genres.

## Task 1: Deterministic expected compilation cost

Remove `HasFourResolvableSingles` as the Album prior's cost gate. Retain or refactor its read-only traversal only as needed for Task 2.

Define a deterministic `compCostWeight`:

```text
if genre is non-adult:                compCostWeight = 1.00
else if year <= 1963:                 compCostWeight = 0.48
else:                                 compCostWeight = 0.00

expectedFormatMultiplier =
    compCostWeight * compilationProductionMultiplier
  + (1 - compCostWeight) * 2.40

expectedProductionCost =
    labelBaseProductionCost * expectedFormatMultiplier
  + albumPackagingFixedCost * priorAssumedAlbumPackaging
```

Treat Concept, Live, Soundtrack, and Standard as standard-cost formats for this calculation.

This is an approved deterministic expectation of the current generator, not a literal row-by-row prediction. `GenerateAlbum` tests the rare Concept branch before the Compilation branch, so actual non-adult Compilation share may be slightly below `100%`, and actual early-adult Compilation share may be slightly below `48%`. Do not add RNG or duplicate the generator's random draws to imitate that preemption.

Audit the assumption as follows:

- for `compCostWeight` values of `0` or `1`, report binary anticipated-versus-actual format agreement;
- for adult decisions in years through 1963, report the actual Compilation/non-Compilation split and its deviation from the `48%/52%` expectation; do not call a blended expectation row-level agreement; and
- report Concept preemptions separately so they cannot be mistaken for a costing defect.

## Task 2: Hit-inventory demand term

The existing Album affinity units are:

```text
affinityUnits =
    priorUnitScalarAlbum
  * qualityEstimate
  * statureMultiplier
  * reachFactor
  * albumDemandFactor
```

Resolve up to four of the artist's most recent released Singles using the assembler's reverse traversal and the same live/archive eligibility rules. Resolution for the prior must be read-only: no RNG, telemetry increments, mutation, or retirement side effects.

The current `AlbumTrack` read-only snapshot does not carry peak position. Add or expose the minimum read-only live/archive metadata needed to obtain each resolved Single's best chart position **as of the decision**. Archived Singles must retain their terminal peak. Do not infer a peak from quality, units, or current position, and do not alter the assembler's track content merely to feed the prior.

Calculate:

```text
peakScore(single) =
    (101 - peakPosition) / 100, when peakPosition is in 1..100
    0, otherwise

hitScore = sum peakScore(single)
           over up to four most recent resolvable released Singles

hitUnits = compCostWeight * priorCompHitUnitScalar * hitScore

expectedAlbumUnits = affinityUnits + hitUnits
```

The term is additive. Do not multiply affinity units by hit score. Never-charted Singles, unresolved IDs, and acts with no chart history contribute zero. Do not require four resolvable Singles; use zero through four, stopping after four successful resolutions.

Use the same `compCostWeight` for Task 1 cost and Task 2 hit units. This guarantees full hit weighting for non-adult generator cohorts, `0.48` weighting for early-adult cohorts, and no hit weighting for later-adult cohorts.

Export `priorCompHitUnitScalar`. It is the only new calibration knob.

`priorUnitScalarAlbum` begins at `240000`. It may be adjusted downward only, and only if the new hit term pushes the overall or Adult Album-choice gates out of band. Document every tested and final value. Do not increase it.

## Task 3: Diagnostics

Extend the A2 diagnostics without silently changing the meaning of existing columns. Prefer additive columns or a new A3 stream when a schema change would make A2 output ambiguous.

For every economic decision, retain enough information to audit:

- `compCostWeight` and expected format multiplier;
- actual generated Album format when an Album is selected;
- number of released-Single IDs examined and number successfully resolved, with resolved Singles capped at four;
- number of those Singles that had charted by decision time;
- `hitScore`, unweighted hit units, weighted hit units, affinity units, and total expected Album units;
- prior and final projected net for both formats; and
- chosen format.

Regenerate the A2 fork-ratio table and include before-versus-after youth rows. Preserve signed differences and undefined-ratio counts.

Report completed youth Compilation realized nets, however small the sample. For completed, memory-eligible youth Compilations joined by `recordId`, report `N`, mean realized net, prior-only mean signed error, and final-projected mean signed error. Signed error is projection minus realized net.

Flag, but do not repair in A3, any absolute youth-Compilation mean signed error above `$5,000`. Small samples must be labeled rather than generalized.

## Task 4: Validation

Run 52 weeks for enabled seeds `1001`, `1002`, and `1003`, plus the album-disabled seed-1001 baseline. Run enabled seed `1001` twice in independent processes.

### Unconditional hard gates

| Check | Required result |
|---|---:|
| Album-disabled annual market units | `154,810,982` exactly |
| Album-disabled `market-revenue.csv` SHA-256 | `765897841BCB1C62225EF2A7861CF46ACC282FE206A10BADCF895330612A3866` |
| Album-disabled `release-capacity.csv` SHA-256 | `8B84BBFDBA5F8B38729F29A1FDF14F8F7598A7AF18DCBAD053BC6C8238A9A461` |
| Enabled determinism | all emitted CSVs byte-identical across the two seed-1001 runs |
| Overall Album choice share | `18%-28%` of successful economic decisions per seed |
| Adult Album choice share | `45%-75%` of successful Adult decisions per seed |
| Youth Album choice share | `4%-15%` of successful Youth decisions per seed |
| Youth selected Albums | overwhelmingly Compilation; report exact numerator and denominator |
| Adult share of Album-chart rows | at least `95%` per seed |
| Adult share of Singles-chart rows | at least `15%` per seed |
| Completed Single mean signed error | within `+/- $5,000` per seed under the existing final-projection definition |

The A3 youth band of `4%-15%` intentionally supersedes A2's `2%-15%` band.

For Album error, report completed signed error and the censored lower-bound ceiling. There is no two-sided Album error gate.

### Conditional singles guards

Only after all format-mix gates pass, apply:

| Check | Required result |
|---|---:|
| Live peak-based Pearson from `SimTools/analyze-chart-audit.mjs` | at least `0.50` per seed |
| Closed Top-40 median life | `11-12` weeks per seed |

If format mix fails, report these values but do not use them as independent tuning targets. If format mix passes and either guard fails, A3 fails.

### Hit-inventory mechanism review

For each seed, report the share of selected youth Albums whose artist had at least one charted Single at decision time, with numerator and denominator.

This is a soft mechanism gate:

- target at least `75%`;
- if the share is above `50%` but below `75%`, report the weakness explicitly and do not claim strong mechanism validation; and
- if the share is `50%` or lower, stop and fail A3 because selected youth Albums concentrate on hitless acts.

Also compare youth Album choice rates for artists with and without charted-single inventory. If the new scalar raises hitless choices indiscriminately, stop rather than accepting a brute-force pass of the aggregate youth band.

## Calibration order

Use this order:

1. implement and verify the deterministic expected-cost weight;
2. implement and verify read-only hit resolution and peak scoring;
3. confirm the disabled baseline and seed-1001 determinism before calibration;
4. calibrate only `priorCompHitUnitScalar` to reach the youth band;
5. if necessary, reduce `priorUnitScalarAlbum` only enough to restore the overall or Adult bands;
6. evaluate all unconditional gates;
7. evaluate conditional singles guards only under a passing format mix; and
8. perform the hit-inventory mechanism review.

Do not tune against a single seed. Choose one shared scalar configuration that passes all three enabled seeds.

## Stop conditions

Stop and report without beginning Checkpoint B if:

- the youth band cannot be reached using Task 1, Task 2, `priorCompHitUnitScalar`, and an optional downward-only adjustment to `priorUnitScalarAlbum`;
- any frozen constant or Single-table value would need to change;
- any unconditional hard gate fails;
- a conditional singles guard fails after format mix passes;
- the hit-inventory soft gate is `50%` or lower; or
- determinism or the disabled baseline changes.

On failure, include the A2-format fork table, youth rows before and after, all scalar trials, hit-inventory splits, and the first failing gate. Do not change demand, chart, retirement, generation, affinity curves, or the Single table to force a pass.

## Deferred risk; log only

The prior will now expect hit-driven Compilation revenue. The realized demand side already rewards hit-laden Compilations through early-era peak-weighted pooling, but it does not explicitly transfer the Singles' existing awareness to the Album.

A3 authorizes no demand-side fix and no decade-run calibration. Use the 52-week completed youth-Compilation sample only as an early warning. Record for Checkpoint 3d that a future decade run may reveal systematically negative realized youth-Compilation nets or a material expected-versus-realized gap as revenue memory accumulates. If observed, report it; do not repair it in A3.

## Required final audit

The audit must include:

1. files changed and the exact behavioral purpose of each change;
2. starting, tested, and final scalar values;
3. proof that Single logic and all frozen constants remained unchanged;
4. baseline units and both baseline hashes;
5. the independent seed-1001 byte-identity result;
6. every unconditional and conditional validation value by seed, with counts;
7. expected-cost versus actual-format diagnostics, including Concept preemptions;
8. hit-score and hit-inventory mechanism diagnostics;
9. fork-ratio youth rows before and after;
10. completed youth-Compilation realized-net and error views;
11. Album completed and censored error views;
12. build and run results; and
13. an explicit pass/fail decision.

Proceed to Checkpoint B only after every A3 hard gate passes and the hit-inventory review does not trigger a stop condition.
