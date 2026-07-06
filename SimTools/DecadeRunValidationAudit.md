# Directive 3D-R final validation audit

Measured 2026-07-05 through 2026-07-06 with Godot 4.7 Mono, headless. This audit supersedes the interrupted Directive 3D diagnostic previously stored at this path. The binding resumption authority is `Directive3DR2-Codex.md`.

## Decision

**FAIL - hold-out seed overfit.**

The final `hitRecencyDecay = 0.75` candidate passes every hard gate on measurement seeds 1001-1006, preserves all frozen disabled anchors, repeats deterministically across 29 simulation/telemetry streams, and materially compresses the effective source-hit age distribution. Hold-out seeds 2001 and 2003 confirm the candidate. Hold-out seed 2002 does not: its 1969 closed Top-40 median expands by +3 weeks while enabled competition is `7.375`, above the same-seed disabled value `7.255`. The approved volume adjudication therefore does not apply.

Per the directive, no tuning was performed after hold-out inspection. The implementation is left at the coherent measurement-seed candidate, but Phase 3D-R is not accepted.

## Directive review

Sign-Off #2 is internally consistent and agrees with the repository evidence:

- M3's 1964-1972 timing window is frozen.
- M1 is closed and its reverted state is retained.
- M2 alone is authorized as a level/realism mechanism, with softening toward 1.0 allowed if the suggested 0.5/year value destabilizes M3.
- M3 may not be retuned to absorb M2.
- Hold-out failure after a measurement-seed pass is a stop-and-report seed-overfit finding.

The phrase "source-hit age distributions compress" needs one measurement qualification. Generation still references the artist's four most recent resolvable Singles, so raw reference age is not mechanically forced downward. The causal M2 measure is the freshness-weighted age distribution: age contribution after multiplying recency decay by existing per-use freshness. Both raw and weighted ages are reported below.

## Files changed

- `Directive3DR2-Codex.md`: self-contained Markdown transcription of Sign-Off #2 and its binding milestone table.
- `Systems/CompetitorManager.cs`: adds exported `hitRecencyDecay`; applies `hitRecencyDecay ^ (ageWeeks / 52)` multiplicatively with existing per-use freshness in `ResolveHitInventory` and compilation pooled appeal. No RNG, source-selection order, retirement rule, generation constant, Single table, peak-fit table, or M3 curve changed.
- `SimTools/analyze-3d.mjs`: reports freshness-weighted source age, genre-stage pivots, Youth compilation arc, concept/cohesion emergence, non-adult album-chart share, standalone onset concentration, and project cancellation/transfer counts.
- `SimTools/DecadeRunValidationAudit.md`: this final audit.

Pre-existing working-tree instrumentation in `AlbumTrack.cs`, `ChartManager.cs`, `ChartAuditRunner.cs`, and `analyze-3d.mjs` remains the source of the decade telemetry used here.

## Calibration change log

### M3 - frozen result

| Iteration | Window | Result |
|---:|---:|---|
| 1 | 1964-1969 | Removed the 1961 doubling; crossover 1966; zero standalone through 1963. Rejected because the 1969 median expansion was not volume-adjudicable in both seeds. |
| 2 | 1963-1969 | Confirmed the pre-1964 trajectory and early guards; transition was unnecessarily abrupt. |
| 3 | 1964-1970 | Crossover 1966/1967; seed 1002 still had a non-adjudicable 1969 median expansion. |
| 4 | **1964-1972** | Six-seed pass; accepted and frozen by Sign-Off #2. |

### M1 - closed

The prior M1 iterations (weekly decay/floor `0.96/5%`, `0.94/1%`, and `0.80/0%`) could not reach the struck cumulative never-retired target without destroying plausible catalog contribution or crossover. Sign-Off #2 closes M1; none of its code is present.

### M2

| Iteration | `hitRecencyDecay` | Probe/checkpoint | Result |
|---:|---:|---|---|
| 1 | 0.50 | Seeds 1001/1002 probe, then 1001-1006 checkpoint | Headline arc passed and weighted source age compressed about 26%, but seed 1001 breached the Pearson floor (`-0.0845`) and seed 1005's 1969 +3 median expansion had competition above disabled. Rejected. |
| 2 | **0.75** | Adverse seeds 1001/1005, then 1001-1006 checkpoint | All measurement hard gates passed; weighted source age compressed 12.7%-15.3%. Frozen before hold-outs. |

No Album-arm decision scalar was changed. No post-hold-out iteration exists.

## Structural validation

| Check | Result |
|---|---|
| Build | Pass: 0 errors; one pre-existing unused-event warning (`ChartManager.OnGenreMomentumChanged`). |
| Disabled seed-1001 1960 Single units | Pass: `154,810,982`. |
| Disabled `market-revenue.csv` SHA-256 | Pass: `765897841BCB1C62225EF2A7861CF46ACC282FE206A10BADCF895330612A3866`. |
| Disabled `release-capacity.csv` SHA-256 | Pass: `8B84BBFDBA5F8B38729F29A1FDF14F8F7598A7AF18DCBAD053BC6C8238A9A461`. |
| Disabled-path inertness | Pass: M2 is downstream of the `enableAlbums` early return; fresh disabled decade pairs were generated for every measurement and hold-out seed. |
| Seed-1001 decade determinism | Pass: all 29 deterministic CSV streams byte-identical. The excluded performance profile contains wall-clock measurements by design. |
| RNG/order | Pass by inspection: M2 adds only snapshot lookup and deterministic arithmetic; no `GD.Rand*` call or iteration-order change. |

Every completed Godot run exited code 0 and emitted `CHART_AUDIT_COMPLETE`. Godot also prints the known post-run `MissingSingletonsTemp.cs` autoload diagnostic after completion; it did not truncate or invalidate output.

## Measurement-seed milestone table

All shares are decision shares. Pearson values are paired against each seed's fresh disabled decade. Late median expansions are volume-adjudicated only when positive and enabled competition is below disabled.

| Seed | Crossover | Standalone onset | 1960 overall / adult / youth | 1969 Album | Pearson mean / min | Early median max | 1969 deflation |
|---:|---:|---:|---:|---:|---:|---:|---:|
| 1001 | 1967 | 1966 | 25.7% / 59.3% / 11.7% | 82.4% | +0.0742 / -0.0389 | 0.5 | 21.5% |
| 1002 | 1968 | 1965 | 23.3% / 50.9% / 11.1% | 82.9% | +0.0864 / -0.0422 | 1.0 | 20.8% |
| 1003 | 1967 | 1966 | 24.2% / 55.3% / 11.1% | 85.4% | +0.0868 / -0.0032 | 1.0 | 24.0% |
| 1004 | 1968 | 1966 | 25.6% / 54.4% / 10.4% | 85.0% | +0.0846 / +0.0048 | 1.0 | 20.0% |
| 1005 | 1967 | 1966 | 24.3% / 53.9% / 12.0% | 81.8% | +0.0387 / -0.0540 | 1.0 | 17.0% |
| 1006 | 1967 | 1966 | 23.4% / 52.8% / 9.8% | 79.4% | +0.0757 / -0.0147 | 1.0 | 22.5% |

Measurement decision: all six pass. Standalone is exactly zero through 1963 in every seed. No median compression is worse than -1. All late breaches are positive and have enabled competition below the paired disabled value.

## M2 source-age result

At 1969, raw referenced-hit mean age is 63.4-75.5 weeks. The freshness-weighted mean falls from the M3 range of 36.6-39.5 weeks to 31.8-34.1 weeks under M2, a 12.7%-15.3% compression across all six seeds. This is the distribution actually consumed by hit inventory and compilation pooled appeal.

## Soft bands and report-only watches

- **Album trend:** pass. 1969 Album decision share is more than 3x 1960 in every measurement seed. Seeds 1005/1006 finish slightly below the report-level 82%-86% range; this is not a hard gate.
- **Substitution deflation:** pass, 17.0%-24.0% below same-seed disabled 1969 combined units.
- **Adult ghetto dissolution:** miss. Non-adult album-chart rows rise from 1.4%-3.2% in 1960 to 16.1%-21.4% in 1969, below the expected >=30%. The direction is correct but magnitude is insufficient; no frozen mechanic was changed to force it.
- **Withheld concentration:** onset is 1965-1966. At onset, 43.9%-100% of standalone decisions are Established-or-higher, and mean demand factor rises thereafter as standalone broadens.
- **Youth compilation arc:** pass in level/continuity. Youth compilations are never extinct and rise from 185-248 releases in 1960 to 2,588-3,008 in 1969; they remain 90.5%-91.7% of Youth Albums in 1969.
- **Genre stagger:** pass using the declared 50% Album-decision pivot. Adult genres pivot in 1960; country/blues in 1961-1962; rock/R&B/soul in 1965-1966.
- **Concept/cohesion:** concepts emerge in 1967 in every seed and reach 477-534 releases in 1969. Mean cohesion rises from 0.08 in 1960 to 0.51-0.52 in 1969.
- **Memory:** 1969 mean confidence is 0.971-0.978 for Singles and 0.922-0.927 for Albums.
- **Promo overprojection:** carried. 1969 promo signed error remains +$19.3K to +$22.5K; M2 was not authorized as a promo repair.
- **Youth-comp overprojection:** carried. 1969 signed error is +$15.4K to +$20.6K despite the healthy release arc.
- **Lifecycle:** cancellations are nonzero (2-10 per seed). Transfers are zero in five seeds and one in seed 1003; transfer scarcity remains report-only.

## Hold-out results - consumed once

| Seed | Crossover | Standalone onset | 1960 overall / adult / youth | Pearson mean / min | Early median max | 1969 deflation | Decision |
|---:|---:|---:|---:|---:|---:|---:|---|
| 2001 | 1968 | 1966 | 23.8% / 55.2% / 10.3% | +0.1150 / +0.0176 | 1.0 | 22.9% | Confirm |
| 2002 | 1967 | 1966 | 25.6% / 51.6% / 13.0% | +0.0833 / -0.0096 | 1.0 | 21.0% | **Fail: 1969 median adjudication** |
| 2003 | 1967 | 1965 | 24.6% / 54.8% / 10.7% | +0.1081 / -0.0006 | 0.0 | 23.3% | Confirm |

Seed 2002's 1969 median delta is `+3`; enabled competition is `7.375`, disabled competition is `7.255`. Because enabled is not below disabled, condition (iii) of the approved late-decade volume adjudication fails. This is precisely the directive's seed-overfit stop condition.

## Runtime and batching

Runs used `--aggregate-only`, which preserves the milestone, project, composition, strategy, and annual telemetry while omitting the largest per-record stream. Enabled decades were batched two to four at a time and generally completed in about 247-341 seconds per batch; the six disabled measurement baselines completed together in about 146 seconds. Hold-out enabled+disabled pairs completed together in about 337 seconds.

## Final disposition

The implementation is technically coherent and passes the full measurement checkpoint, but the one-shot hold-out result prevents acceptance. Do not tune against seed 2002. Any future resumption requires new written direction: either accept the observed hold-out variance, revise the late-median guard before a newly prespecified hold-out set, or reject M2/Phase 3D-R. The current directive is exhausted at its mandated stop condition.
