# Album project pipeline audit

Measured 2026-07-04 with Godot 4.7 Mono over 52 weeks. Checkpoint A3 is accepted under the corrected paired-Pearson gate. Checkpoint B passes all binding regression gates for seeds 1001-1003.

## Checkpoint A3 gate rebase

The retired absolute Pearson floor of `0.50` incorrectly rejected the album-disabled seed-1001 baseline (`0.494`). The frozen A3 results are therefore adjudicated against the same-seed album-disabled references:

| Seed | Frozen baseline | A3 Pearson | Delta | Per-seed gate |
|---:|---:|---:|---:|---|
| 1001 | 0.494 | 0.489208 | -0.004792 | Pass |
| 1002 | 0.529 | 0.505090 | -0.023910 | Pass |
| 1003 | 0.578 | 0.578185 | +0.000185 | Pass |

Mean delta is approximately `-0.009506`, above `-0.02`; every seed is above `-0.03`. The frozen Top-40 medians were 11, 11, and 12. Checkpoint A3 therefore passes retroactively. No A3 empirical-table value, scalar, cost weight, or format-decision draw was changed. `priorUnitScalarAlbum = 175000`, `priorCompHitUnitScalar = 20000`, and the empirical Single path remain frozen.

## Implementation

- `Data/AlbumProject.cs` adds runtime-only project, strategy, terminal-state, record-role, outcome-state, and promotion-snapshot models.
- `Data/RecordRuntimeData.cs` adds project/role linkage plus cannibalization telemetry.
- `Systems/CompetitorManager.cs` implements the two-stage decision, project generation, prepaid production, ordered scheduling, due drops, cancellation, nonterminal ownership transfer, synergy, held outcomes, cancellation redirection, and exactly-once project memory folds.
- `Systems/AlbumSimulator.cs` resolves live linked-promo `radioHeat` and applies one unconditional `(1 - suppression)` multiplier before inventory/capacity clipping.
- `SimTools/ChartAuditRunner.cs` appends the approved B fields to `release-strategy.csv` and emits `album-projects.csv` plus `album-project-demand.csv`.
- `SimTools/analyze-b.mjs` reproduces the B gate, reconciliation, memory, competition, synergy, cannibalization, and watch-cohort calculations.

New exported settings were left at their implementation defaults; no validation tuning was performed:

| Setting | Start/final |
|---|---:|
| Album drop gap | 3-5 weeks inclusive |
| Promo flop threshold | 80 |
| Awareness bonus maximum | 0.25 |
| Stock bonus maximum | 0.80 |
| Flop stock floor | 0.85 |
| Cannibalization strength | 0.15 |
| Expected promo-lift scalar | $10,000 |
| Deterministic expected promo heat | 0.50 |

All 1960 Album decisions selected `AlbumWithPromo`; `AlbumStandalone` had no acceptance band and received no decision in these samples.

## RNG order and due-drop proof

The enabled choice first executes frozen A3 order: Single prior, Album prior, memory blend, Single noise, Album noise. Only if Album wins does the deterministic standalone-versus-promo arithmetic run; it consumes no decision-noise draw. Comp-costed rows at weights 1.00 and 0.48 remain eligible.

For a promo project the post-decision order is: generate the complete Album; derive the strongest eligible non-Single track without song-generation RNG; draw the 3-5 week gap; draw Album perceived quality; snapshot Album per-region awareness and sentiment factors (Album radio is deterministically zero); then draw/apply the normal promo-Single promotion inputs. Production is charged at scheduling, promo marketing at scheduling, and Album marketing at drop.

The due-drop call graph contains no `GD.Rand*` call: it resolves the owner and stored promo peak, clamps the already-planned budget, releases the prepared Album, and applies the stored promotion snapshot. Thus each due drop consumes zero RNG. Drops run in `creationSequence` order after revenue and weekly-counter reset and before normal release rolls.

## Baseline and determinism

| Check | Result |
|---|---|
| Album-disabled seed-1001 annual units | `154,810,982` exact |
| `market-revenue.csv` SHA-256 | `765897841BCB1C62225EF2A7861CF46ACC282FE206A10BADCF895330612A3866` |
| `release-capacity.csv` SHA-256 | `8B84BBFDBA5F8B38729F29A1FDF14F8F7598A7AF18DCBAD053BC6C8238A9A461` |
| Disabled project rows | 0 |
| Enabled seed-1001 repeat | all 28 CSVs byte-identical |
| Representative `records.csv` hash | `68F6D82D9A621D5839D0BC78D1D4F17BC47A034AAD9CE0D3AFB1477DAC9776AF` |
| Representative `release-strategy.csv` hash | `AC92FE3D0EB4767DCCFF59536CE0AFFD1C0C040CD2F25225CB6C66BAD75DBDB3` |
| Representative `album-projects.csv` hash | `CC583EBD36B194BE793D7DEFD6946BC7520703B36B06B6AF76567DD3A3E12883` |

## Regression gates

Shares use successful economic decisions. Album decisions mean `AlbumStandalone + AlbumWithPromo`; physical drops are separate.

| Seed | Decisions | Album share | Adult Album | Youth Album | Youth Compilation | Drops | Adult Album-chart | Adult Singles-chart | Single error (N) |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 1001 | 4,299 | 26.96% (1,159) | 58.07% (536/923) | 12.61% (264/2,094) | 264/264 | 1,062 | 97.47% | 34.00% | -$32 (2,050) |
| 1002 | 4,345 | 23.38% (1,016) | 52.04% (472/907) | 10.67% (246/2,306) | 246/246 | 924 | 97.60% | 33.44% | +$163 (2,141) |
| 1003 | 4,434 | 25.48% (1,130) | 54.99% (496/902) | 11.93% (292/2,447) | 292/292 | 1,041 | 97.64% | 30.19% | +$975 (2,118) |

Every mix, composition, and completed-Single error gate passes.

| Seed | Live peak Pearson | Baseline | Delta | Closed Top-40 median | Result |
|---:|---:|---:|---:|---:|---|
| 1001 | 0.471984 | 0.494 | -0.022016 | 11 | Pass |
| 1002 | 0.518861 | 0.529 | -0.010139 | 11 | Pass |
| 1003 | 0.597200 | 0.578 | +0.019200 | 11 | Pass |

Mean Pearson delta is `-0.004318`, passing the `-0.02` mean gate.

## Singles competition

The denominator is `sum(weeks.csv newEntriesTop100)`. The all-Singles numerator includes released orphan and promo Singles. Frozen comparison is the corresponding A3 orphan-Single population.

| Seed | All Singles N/D; ratio | Orphan N/D; ratio | Frozen N/D; ratio | All change | Orphan change |
|---:|---:|---:|---:|---:|---:|
| 1001 | 4,299/1,035; 4.1536 | 3,140/1,035; 3.0338 | 3,138/1,010; 3.1069 | +1.0467 (+33.69%) | -0.0731 (-2.35%) |
| 1002 | 4,345/1,032; 4.2103 | 3,329/1,032; 3.2258 | 3,329/1,024; 3.2510 | +0.9593 (+29.51%) | -0.0252 (-0.78%) |
| 1003 | 4,434/1,041; 4.2594 | 3,304/1,041; 3.1739 | 3,331/1,029; 3.2371 | +1.0222 (+31.58%) | -0.0633 (-1.95%) |

Promo volume raised total Singles competition roughly 30%-34%, while the orphan-only ratio fell slightly. Pearson still passes its paired gates; the crowding movement is reported rather than tuned away.

## Project reconciliation and behavior

| Seed | Scheduled | Released | Cancelled | Pending | Transferred | Overdue active pending |
|---:|---:|---:|---:|---:|---:|---:|
| 1001 | 1,159 | 1,062 | 0 | 97 | 0 | 0 |
| 1002 | 1,016 | 924 | 0 | 92 | 0 | 0 |
| 1003 | 1,130 | 1,041 | 0 | 89 | 0 | 0 |

`scheduled = Released + Cancelled + PendingAtAuditEnd` exactly. Natural 52-week samples produced no transfer, so transfer counters reconcile at zero; the transfer path preserves IDs/sequence/date, rewrites only current ownership and the unreleased Album label, and remains nonterminal.

Every promo and released Album had exactly one project role link; every pending Album had no runtime/outcome record. Duplicate Album and promo links were zero for all seeds.

Promo score launch behavior is directionally correct:

| Seed | Released promo projects | Positive score | Corr(score, awareness) | Corr(score, stock) |
|---:|---:|---:|---:|---:|
| 1001 | 1,062 | 198 | +0.7506 | +0.8374 |
| 1002 | 924 | 153 | +0.7268 | +0.8410 |
| 1003 | 1,041 | 197 | +0.7503 | +0.8390 |

Zero-score flops receive awareness bonus 0 and stock multiplier 0.85. Positive scores map peak 1 to 1.0 and interpolate to zero at threshold 80.

Demand-weighted linked-live cannibalization was 2.762%, 3.079%, and 2.911% by seed, all greater than zero. The formula is unconditional: missing/retired links yield `singleHeat = 0`, suppression exactly `0`, and multiplier exactly `1`. No standalone decision occurred (`N=0`); standalone and unlinked inertness follows directly from that shared formula rather than a separate demand branch.

## Memory accounting and fold examples

Record-equivalent accounting reconciles exactly:

| Seed | Eligible records | Orphan Single | Standalone Album | 2 x folded promo projects | Held promo | Redirected | Unresolved Album | RHS |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 1001 | 2,816 | 2,050 | 0 | 226 | 539 | 0 | 1 | 2,816 |
| 1002 | 2,815 | 2,141 | 0 | 198 | 476 | 0 | 0 | 2,815 |
| 1003 | 2,874 | 2,118 | 0 | 222 | 534 | 0 | 0 | 2,874 |

Observation accounting is intentionally different: Single observations / Album observations / held-unresolved observations were `2050/113/540`, `2141/99/476`, and `2118/111/534`. No physical record updated two memories and no project folded into Album memory twice.

The realized convention is `lifetimeLabelNet - sunkProductionCost`, retaining retirement-week revenue exclusion. Example: seed-1001 `project_43` held promo realized net `$1,211.165` and Album realized net `-$1,157.246`; their sum `$53.919` became exactly one Album-memory observation credited to current owner `label_0234`. A cancelled project would redirect the same held promo realized net to Single memory; no final validation project cancelled.

## Expected-versus-realized watches

Pending, cancelled, and unretired cases remain in the cohort counts.

| Cohort/seed | Total | Completed | Pending | Cancelled | Unretired | Mean expected | Mean realized | Mean signed error |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Youth Compilation 1001 | 264 | 15 | 30 | 0 | 219 | $4,402 | -$1,042 | +$5,443 |
| Youth Compilation 1002 | 246 | 10 | 33 | 0 | 203 | $4,768 | -$994 | +$5,762 |
| Youth Compilation 1003 | 292 | 17 | 34 | 0 | 241 | $4,563 | -$1,126 | +$5,689 |
| AlbumWithPromo 1001 | 1,159 | 113 | 97 | 0 | 949 | $17,998 | $1,437 | +$16,561 |
| AlbumWithPromo 1002 | 1,016 | 99 | 92 | 0 | 825 | $21,018 | $1,656 | +$19,362 |
| AlbumWithPromo 1003 | 1,130 | 111 | 89 | 0 | 930 | $17,321 | $1,726 | +$15,595 |

Youth expected values are frozen A3 final projected Album net versus Album-only realized net. Promo-project expected values are B5 projected project net versus `promoSingleRealizedNet + albumRealizedNet`. These are diagnostic, not gates; both cohorts remain strongly overprojected in the small completed samples.

## Build, limitations, and decision

`dotnet build "Label Man.sln" --no-restore` succeeds with zero errors. The sole warning is the pre-existing unused `ChartManager.OnGenreMomentumChanged` event. All Godot runs completed; the pre-existing `MissingSingletonsTemp.cs` autoload warning appeared after completion.

Retirement still precedes weekly revenue booking, so retirement-week revenue is excluded. End-of-run scheduled Albums remain pending rather than force-dropped. Transfer and standalone dynamic samples were absent, although their code paths and exact inertness/ownership rules are implemented.

**Decision: Checkpoint B passes.** Every binding regression gate, reconciliation identity, determinism check, launch-sign check, and linked-live cannibalization requirement passed.
