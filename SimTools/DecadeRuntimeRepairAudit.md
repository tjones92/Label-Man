# Directive 3D-P decade runtime repair audit

Measured 2026-07-05 with Godot 4.7 Mono, headless, seed 1001. **Decision: pass.** The shipped changes are performance- and observation-only. No simulation constant, economic formula, demand path, retirement rule, decision branch, or RNG call/order changed.

## Pre-flight artifact resolution

Both required questions were resolved before profiling:

1. The interrupted 3D annual observation work was present as intact local, uncommitted work in `SimTools/ChartAuditRunner.cs` and `SimTools/analyze-3d.mjs`; `SimTools/DecadeRunValidationAudit.md` was also present. It was preserved and extended rather than reconstructed from the pushed `9de8f856` tree.
2. The complete 3C.2 enabled seed-1001 reference was retained in `SimLogs/` as both `3c2-enabled-1001a-*` and `3c2-enabled-1001b-*`, with all 28 CSVs. The interrupted `3d-enabled-1001-*` reference was also retained, including its buffered/truncated files. Therefore both the full 28-stream year-one check and complete-line prefix comparison were possible.

The retained representative 3C.2 hashes are:

| Stream | SHA-256 |
|---|---|
| `records.csv` | `110BE6CC8AAB67A040DD06CDC130336AA5F1344CEA1FE7CF6217DCEA66E5C788` |
| `release-strategy.csv` | `DE92DBD803F68A14560CB3E850A6EB582E5C7724A96E7234BD82F42475C91312` |
| `album-projects.csv` | `C7AA47472D96C43FD6950D04BA55ED3142AEAAFB80D999E1DF34D19F067D683E` |

## Task 1: pre-change profile

The profiler is opt-in via `--profile-performance`. It measures 52-week blocks, record count, `ChartManager.SimulateWeek`, `CalculateLabelRevenue`, its nested runtime lookups separately from the remaining work, Album updates, `ProcessDueAlbumProjects`, and `CaptureWeek`.

The unoptimized lean run was stopped after 1963. By then one year required 1,030 seconds and the two suspected linear-lookup consumers accounted for 893 seconds. Continuing the remaining unoptimized years would have consumed hours without changing the attribution. This is the one deliberate scope limitation in the audit; no missing values are estimated.

| Year | Active records | Wall s | SimulateWeek s | Revenue total s | Revenue lookup s | Revenue remainder s | Album update s | Due projects s | CaptureWeek s |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 1960 | 2,537 | 16.16 | 3.61 | 4.66 | 3.97 | 0.69 | 1.76 | 0.49 | 1.03 |
| 1961 | 4,871 | 108.38 | 43.51 | 36.71 | 34.02 | 2.69 | 40.84 | 4.92 | 1.17 |
| 1962 | 8,296 | 485.54 | 240.30 | 166.15 | 158.56 | 7.59 | 235.69 | 18.45 | 1.74 |
| 1963 | 11,160 | 1,030.31 | 562.05 | 348.92 | 337.23 | 11.69 | 555.86 | 25.55 | 2.18 |

This confirms the attribution empirically. From 1960 to 1963, revenue arithmetic grew from 0.69 to 11.69 seconds, while its linear record lookups grew from 3.97 to 337.23 seconds. Album updates independently grew from 1.76 to 555.86 seconds because linked-promo resolution used the same scan. Lean telemetry stayed near 1-2 seconds/year over this interval and was not the cause.

## Tasks 2-4: shipped runtime changes

### O(1) live-record lookup

`ChartManager` now maintains a `StringComparer.Ordinal` dictionary from record ID to `RecordRuntimeData`. `allRecords` remains the only simulation iteration source. Releases add to the list and index together; prewarm culling rebuilds the index immediately; retirement removes from both at the existing removal site. The existing retired-track dictionary was not changed.

Every existing `GetRecordRuntimeData` consumer benefits without changing call or iteration order, including `CalculateLabelRevenue` and linked-promo Album updates. `AddRadioPlay` also uses the lookup method instead of duplicating a scan.

The monthly distribution-offer scans were not changed. Profiling showed no reason to risk rewriting these monthly paths after the weekly quadratic cost was removed.

### Live due-project queue

The lifetime `albumProjects` list is unchanged for reconciliation and telemetry. Promo-linked pending projects are additionally held in an insertion/creation-sequence-ordered live list. Each week visits only pending entries, processes due entries in creation sequence, and removes released/cancelled terminal entries in place. Standalone projects are terminal immediately and never enter the pending list.

Ordering equivalence is demonstrated by all 28 retained year-one streams matching and by every complete line in every retained partial 3D stream matching through the interruption point. This covers the project weekly stream, release strategy, records, outcomes, and downstream RNG-sensitive streams; it is not an order assertion based only on code inspection.

## Optimized profile

The final profiled lean decade completed all ten years:

| Year | Active records | Wall s | SimulateWeek s | Revenue total s | Lookup s | Revenue remainder s | Album update s | Due projects s | CaptureWeek s |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 1960 | 2,537 | 7.63 | 2.29 | 0.56 | 0.05 | 0.51 | 0.12 | 0.03 | 1.17 |
| 1961 | 4,871 | 13.73 | 3.73 | 0.94 | 0.11 | 0.83 | 0.44 | 0.05 | 1.40 |
| 1962 | 8,296 | 21.39 | 5.31 | 1.57 | 0.20 | 1.37 | 0.94 | 0.11 | 1.84 |
| 1963 | 11,160 | 28.48 | 7.16 | 2.30 | 0.31 | 1.99 | 1.51 | 0.08 | 2.23 |
| 1964 | 13,193 | 33.53 | 8.00 | 2.76 | 0.39 | 2.36 | 1.59 | 0.08 | 2.76 |
| 1965 | 14,183 | 40.65 | 9.55 | 3.24 | 0.47 | 2.77 | 1.84 | 0.06 | 3.28 |
| 1966 | 14,493 | 42.21 | 9.81 | 3.37 | 0.50 | 2.87 | 1.87 | 0.07 | 3.32 |
| 1967 | 14,920 | 41.20 | 9.61 | 3.30 | 0.48 | 2.82 | 1.83 | 0.06 | 3.27 |
| 1968 | 14,729 | 42.45 | 9.88 | 3.35 | 0.50 | 2.85 | 1.82 | 0.05 | 3.45 |
| 1969 | 14,281 | 40.89 | 9.63 | 3.26 | 0.48 | 2.78 | 1.81 | 0.05 | 3.46 |

The 52-week timing blocks and calendar-year cohort snapshots differ slightly at late-year boundaries because the calendar advances in seven-day steps. The report-only calendar table below is the authority for year-end composition; the timing table is the authority for equal-duration performance comparisons.

At the identical 11,160-record checkpoint, wall time fell from 1,030.31 to 28.48 seconds (36.2x). Revenue lookup time fell from 337.23 to 0.31 seconds, Album updates from 555.86 to 1.51 seconds, and due-project processing from 25.55 to 0.08 seconds.

## Task 5: telemetry hygiene

Annual writes now flush explicitly at calendar-year transitions. The annual rollup, performance profile, weekly summary, lifecycle, concentration, market revenue, release capacity, format mix, and album-project weekly writers are explicitly flushed at the annual checkpoint. The profile CSV is created only with `--profile-performance`, because wall time is intentionally nondeterministic.

`--lean-probe` **implies `--aggregate-only`**. The existing flag already suppressed `records.csv`; lean mode incrementally suppresses `breakout-funnel.csv`, the larger per-record/per-region weekly diagnostic. Both files retain headers. Guard inputs, annual rollup, project/revenue streams, album chart/composition, lifecycle, and summary streams remain enabled. `analyze-3d.mjs` falls back to annual Pearson and closed-Top-40 fields when the record stream is suppressed.

Full telemetry remains the default. A first attempt to force a write every 52 weeks produced duplicate short calendar-year rows because a 365/366-day calendar is not divisible by seven. That attempt was corrected before final validation: writes now occur through the existing calendar-year transition, while report-only cohort state is pinned to the last completed weekly snapshot. Final output has exactly one row for each year 1960-1969.

## Task 6: retirement-cohort diagnostic

This is report-only evidence. No retirement setting changed.

| Year | Live Singles | Live Albums | Album age P50/P90 | Weekly Album units P25/P50/P90 | Below 10 units | Ever released | Retired | Never retired share |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 1960 | 1,583 | 954 | 20 / 42 | 19 / 54 / 231 | 13.52% | 1,071 | 117 | 89.08% |
| 1961 | 1,803 | 3,068 | 29 / 72 | 21 / 40 / 164 | 4.89% | 3,663 | 595 | 83.76% |
| 1962 | 1,940 | 6,356 | 38 / 94 | 22 / 40 / 158 | 2.11% | 8,073 | 1,717 | 78.73% |
| 1963 | 1,852 | 9,308 | 50 / 124 | 21 / 37 / 140 | 2.22% | 13,204 | 3,896 | 70.49% |
| 1964 | 1,513 | 11,680 | 59 / 147 | 19 / 33 / 131 | 2.29% | 18,842 | 7,162 | 61.99% |
| 1965 | 1,446 | 12,788 | 62 / 155 | 18 / 31 / 117 | 2.28% | 24,800 | 12,012 | 51.56% |
| 1966 | 1,350 | 13,207 | 64 / 152 | 17 / 30 / 133 | 3.60% | 30,687 | 17,480 | 43.04% |
| 1967 | 1,341 | 13,640 | 66 / 154 | 17 / 31 / 165 | 4.68% | 36,637 | 22,997 | 37.23% |
| 1968 | 1,376 | 13,374 | 66 / 152 | 17 / 33 / 193 | 5.59% | 42,521 | 29,147 | 31.45% |
| 1969 | 1,235 | 13,046 | 62 / 151 | 17 / 36 / 215 | 6.45% | 48,573 | 35,527 | 26.86% |

At end-1969, 13,046 of 48,573 released Albums have never retired. Only 842 live Albums (6.45%) are below the 10-unit floor at the snapshot; 12,204 are at or above it. Median Album age is 62 weeks and P90 is 151 weeks. This supports the next directive's economic diagnosis: the large pool is primarily sustained above the retirement floor, not stuck below it because of a code-level retirement check. No action was taken on that finding here.

## Validation

| Check | Result |
|---|---|
| Disabled 1960 units | **Pass:** `154,810,982` exact |
| Disabled `market-revenue.csv` | **Pass:** `765897841BCB1C62225EF2A7861CF46ACC282FE206A10BADCF895330612A3866` |
| Disabled `release-capacity.csv` | **Pass:** `8B84BBFDBA5F8B38729F29A1FDF14F8F7598A7AF18DCBAD053BC6C8238A9A461` |
| Enabled 52-week representative hashes | **Pass:** all three exact |
| Enabled 52-week full reference | **Pass:** all 28 retained 3C.2 streams byte-identical |
| Interrupted 3D prefix | **Pass:** every retained stream matched through its final complete newline; truncated buffered tails were excluded |
| Final-code decade determinism | **Pass:** all 29 deterministic streams byte-identical across two independent 520-week lean runs, including corrected annual cohorts |
| Full-telemetry decade determinism | **Pass:** all 29 deterministic streams byte-identical across two independent full runs before the final cohort-snapshot-only telemetry correction; the final correction touched only annual observation fields |
| Full runtime | **Pass:** 344.1 s / 5.74 min end-to-end versus 30 min target |
| Lean runtime | **Pass:** 315.2 s / 5.25 min end-to-end versus 20 min target |
| Build | **Pass:** final `dotnet build "Label Man.sln" --no-restore` reported 0 warnings and 0 errors |

Godot prints the pre-existing post-completion `MissingSingletonsTemp.cs` autoload error after `CHART_AUDIT_COMPLETE`; the audit process still exits successfully and all files close normally.

## Files and guardrails

- `Systems/ChartManager.cs`: lookup-only live-record index and synchronized maintenance.
- `Systems/CompetitorManager.cs`: live pending Album-project queue and opt-in timing hooks.
- `Systems/SimulationPerformanceProfiler.cs`: new opt-in, RNG-free timing accumulator.
- `SimTools/ChartAuditRunner.cs`: profiling flag/output, lean-probe split, annual flushes, analyzer fallback fields, and retirement cohorts.
- `SimTools/analyze-3d.mjs`: lean annual fallback for Pearson and closed Top-40 median.
- `SimTools/DecadeRuntimeRepairAudit.md`: this audit.

`retiredTrackArchive`, economic calibration, Album demand, strategy decisions, retirement constants, and RNG-adjacent code were not changed. No optimization attempt failed equivalence, so no runtime optimization needed to be reverted.
