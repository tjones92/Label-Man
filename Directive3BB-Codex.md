# Codex Directive 3B-B: Gate Rebase and Album Project Pipeline

## Objective

Accept the frozen Checkpoint A3 implementation under the corrected Pearson gate, then implement and validate Checkpoint B: the Album Project Pipeline.

## Sources of truth and precedence

Use:

- `SimTools/AlbumProjectPipelineDirective.md`, sections B1-B8;
- `Directive3BA2-Codex.md`, Checkpoint B integration;
- `Directive3BA3-Codex.md`;
- `SimTools/YouthCompilationEconomicsAudit.md`; and
- the current working tree.

This directive supersedes those documents only where it explicitly says so. Preserve all user changes and unrelated work.

## Part 1: Rebase and close Checkpoint A

The absolute live-Pearson floor of `0.50` is retired. It is defective because the mandated analyzer measures album-disabled BASELINE seed `1001` at `0.494`.

For this checkpoint and all future checkpoints, use a paired delta against the frozen album-disabled BASELINE for the same seed:

```text
pearsonDelta(seed) =
    currentLivePearson(seed) - frozenBaselineLivePearson(seed)
```

Frozen BASELINE references:

| Seed | Live Pearson |
|---:|---:|
| 1001 | 0.494 |
| 1002 | 0.529 |
| 1003 | 0.578 |

Calculate with unrounded analyzer output where available. Apply both gates:

- every seed: `pearsonDelta >= -0.03`;
- arithmetic mean across seeds 1001-1003: `meanPearsonDelta >= -0.02`.

Use only the live peak-based Pearson produced by `SimTools/analyze-chart-audit.mjs`. Do not substitute a completed-lifecycle or units-based correlation.

The closed Top-40 median gate remains `11-12` weeks per seed.

Checkpoint A3 is retroactively declared passed:

| Seed | A3 Pearson | Delta |
|---:|---:|---:|
| 1001 | 0.489208 | approximately `-0.005` |
| 1002 | 0.505090 | approximately `-0.024` |
| 1003 | 0.578185 | approximately `0.000` |

The cross-seed mean delta is approximately `-0.010`, so both corrected Pearson gates pass.

Record this adjudication in `SimTools/AlbumProjectPipelineAudit.md`. Do not modify any A3 code, empirical-table value, scalar, or cost weight. The current A3 configuration is the frozen Checkpoint A result.

## Part 2: Implement Checkpoint B

Implement sections B1-B7 of `SimTools/AlbumProjectPipelineDirective.md`:

- B1: `AlbumProject`, strategies, generation, scheduling, costs, cooldowns, and counters;
- B2: survival, cancellation, and ownership transfer;
- B3: promo synergy and the flop case;
- B4: linked-release cannibalization;
- B5: promo-versus-standalone decision;
- B6: project-level memory attribution; and
- B7: telemetry.

Apply every unchanged integration rule from `Directive3BA2-Codex.md`, plus the following binding overrides.

### B5: two-stage decision order

This section explicitly supersedes the original simultaneous three-way, three-noise-draw B5 decision.

First perform the frozen A3 format decision:

```text
Single prior
Album prior
memory blend
Single noise
Album noise
```

This chooses `OrphanSingle` or `Album`.

If `OrphanSingle` wins, release it normally and perform no Album-strategy sub-decision.

If `Album` wins, choose deterministically between `AlbumStandalone` and `AlbumWithPromo` using the original B5 arithmetic and already-computed scheduling-time inputs. The sub-decision consumes no decision-noise RNG.

`expectedPromoSingleNet` must come from the frozen empirical Single table through the existing `CalculateSinglePriorNet` path. It already includes Single production cost. Do not introduce new Single-side arithmetic or deduct production twice.

Comp-costed Album candidates—including weight `1.00` and blended weight `0.48` rows—are eligible for both Album strategies. Do not special-case them out of `AlbumWithPromo`; the hit-inventory value already belongs to the Album arm.

After the strategy is selected, perform project-generation and scheduling draws in the stable B1 order. A due album drop consumes zero RNG.

### B6: memory attribution

Use the existing realized-net convention:

```text
realizedNet = lifetime net - sunk production cost
```

Preserve the existing retirement-week revenue exclusion.

Hold the promo Single outcome until the linked Album outcome is available. Once both are eligible:

```text
projectRealizedNet =
    promoSingleRealizedNet + albumRealizedNet
```

Fold that total into Album memory as exactly one observation. Do not also update Single memory for that promo outcome.

Preserve the original cancellation, transfer, duplicate-prevention, record-equivalent accounting, and observation-accounting rules. Document the fold arithmetic and ownership attribution in the audit.

## Part 3: Validation

Run enabled seeds `1001`, `1002`, and `1003` for 52 weeks. Run enabled seed `1001` twice in independent processes.

Execute every unchanged Checkpoint B validation requirement in section B8, plus the following regression gates.

| Gate | Required result |
|---|---:|
| Album-disabled seed-1001 annual market units | `154,810,982` exactly |
| Album-disabled `market-revenue.csv` SHA-256 | `765897841BCB1C62225EF2A7861CF46ACC282FE206A10BADCF895330612A3866` |
| Album-disabled `release-capacity.csv` SHA-256 | `8B84BBFDBA5F8B38729F29A1FDF14F8F7598A7AF18DCBAD053BC6C8238A9A461` |
| Enabled seed-1001 determinism | All emitted CSVs byte-identical |
| Album-strategy share | `18%-28%` |
| Adult Album-strategy share | `45%-75%` |
| Youth Album-strategy share | `4%-15%`; overwhelmingly Compilation |
| Adult share of album-chart rows | At least `95%` |
| Adult share of singles-chart rows | At least `15%` per seed |
| Live Pearson | Delta at least `-0.03` per seed; mean delta at least `-0.02` |
| Closed Top-40 median | `11-12` weeks per seed |
| Completed Single mean signed error | Within `+/- $5,000` per seed |

For strategy shares, count `AlbumStandalone + AlbumWithPromo` as Album decisions. Use successful economic decisions as the denominator. Report physical Album drops separately so end-of-run scheduling censoring remains visible.

### Singles competition

Report the P3 singles competition ratio for each seed:

```text
allSinglesCompetitionRatio =
    physical Singles entering the normal singles pool
    / sum(weeks.csv newEntriesTop100)
```

The numerator includes released orphan Singles and released promo Singles. Also report orphan-only ratio, numerator, denominator, absolute change, and percentage change against the relevant frozen comparison.

Report competition ratios beside the Pearson values and deltas. If Pearson fails, report any accompanying crowding movement; do not silently tune away promo-volume effects.

### Expected versus realized watch

Report two separate, potentially overlapping cohorts:

1. youth Compilation Albums;
2. `AlbumWithPromo` projects.

For youth Compilations, compare the frozen A3 final projected Album net with realized Album net.

For completed `AlbumWithPromo` projects, compare the B5 projected project net with:

```text
albumRealizedNet + promoSingleRealizedNet
```

Report `N`, mean expected net, mean realized net, and mean signed error. List pending, cancelled, and unretired cases separately instead of dropping them from the denominator. These are diagnostic views, not gates.

## Frozen values and guardrails

Do not change:

- the empirical Single table or `CalculateSinglePriorNet`;
- `priorUnitScalarAlbum = 175000`;
- `priorCompHitUnitScalar = 20000`;
- compilation weights or costing rules;
- demand, chart, retirement, generation, or release-population constants;
- the frozen A3 format-decision RNG sequence.

Prove cannibalization inertness for standalone and unlinked Albums: suppression must be exactly zero and the multiplier exactly one.

If any gate fails, identify whether the movement came from promo synergy, cannibalization, delayed drops, slot or cooldown consumption, or additional promo-Single volume. Do not recalibrate Checkpoint A to conceal a Checkpoint B regression.

## Required handoff

Write `SimTools/AlbumProjectPipelineAudit.md` containing:

- the Checkpoint A3 gate-rebase adjudication;
- implementation and data-model changes;
- RNG order and zero-draw drop proof;
- baseline checksums and determinism hashes;
- all Checkpoint B reconciliation and behavior results;
- every regression gate by seed;
- Pearson deltas and competition ratios;
- youth-Compilation and `AlbumWithPromo` expected-versus-realized results;
- both B6 accounting identities and fold examples;
- cannibalization inertness proof;
- build and test results; and
- a plain pass/fail decision.

If a gate fails, leave the implementation at the last coherent checkpoint and report the failure without softening it into a pass.
