# Directive 3D decade-run validation audit — interrupted diagnostic checkpoint

Measured 2026-07-05 with Godot 4.7 Mono, headless. This audit records all valid evidence recovered after the album-enabled diagnostic was stopped at the user's request because of extreme runtime. It is intentionally not an acceptance audit.

## Decision

**Directive 3D is not accepted.** The six prescribed album-disabled baselines completed and the year-one structural/RNG-inert telemetry checks pass. The first three album-enabled diagnostic seeds were interrupted during 1966 after roughly two and a half hours, so no enabled decade, measurement-seed checkpoint, determinism repeat, or hold-out pair completed.

The interruption is not the only reason for the negative decision. The complete 1960-1965 portions already establish three hard-gate failures:

1. album gross crosses above Single gross in **1963** in seeds 1001-1003, earlier than the allowed 1965 boundary;
2. `AlbumStandalone` appears in **1962** and reaches 13.22%-14.25% of all decisions in 1963, violating the required `<0.5%` standalone share through 1963; and
3. the paired closed Top-40 median is worse than `+/-2` weeks by 1964 or 1965 in every recovered seed.

No calibration knob was changed, the contingency mechanic was not built, and hold-out seeds 2001-2003 were never run.

## Scope and files changed

- `SimTools/ChartAuditRunner.cs`: adds RNG-free `decade-annual-rollup.csv` observation state and output. It aggregates format units/gross/net, decision and strategy shares, compilation freshness, confidence, year-end revenue memory, expected-versus-realized completion cohorts, and album-to-Single gross ratio.
- `SimTools/analyze-3d.mjs`: adds a streaming annual analyzer for large decade CSVs, plus compact output. It does not load multi-hundred-megabyte record streams into memory.
- `SimTools/DecadeRunValidationAudit.md`: this checkpoint audit.

No production simulation mechanic or exported calibration value was changed.

## Phase 3D-0: frozen album-disabled baselines

All six `--disable-albums --weeks=520` runs completed before any enabled decade output was inspected. Runs were batched three at a time. Competition ratio below is successful releases divided by new Top-100 entries; album-disabled format decisions bypass `OnReleaseStrategy`, so that event stream cannot supply its usual numerator.

### Annual units

| Year | Seed 1001 | Seed 1002 | Seed 1003 | Seed 1004 | Seed 1005 | Seed 1006 |
|---:|---:|---:|---:|---:|---:|---:|
| 1960 | 154,810,982 | 158,812,169 | 165,617,751 | 161,038,169 | 161,990,515 | 164,662,011 |
| 1961 | 184,816,416 | 191,481,726 | 204,741,885 | 194,614,773 | 192,597,473 | 196,712,799 |
| 1962 | 193,942,990 | 207,650,554 | 216,780,875 | 211,245,523 | 207,702,218 | 211,906,062 |
| 1963 | 197,359,432 | 204,214,989 | 223,962,982 | 220,136,193 | 209,382,724 | 216,348,689 |
| 1964 | 204,277,839 | 210,759,388 | 221,832,809 | 216,854,707 | 210,957,827 | 217,416,164 |
| 1965 | 212,735,370 | 222,691,660 | 233,007,693 | 231,356,927 | 226,202,388 | 224,470,934 |
| 1966 | 217,992,043 | 218,291,507 | 239,959,051 | 232,629,841 | 223,528,133 | 227,151,548 |
| 1967 | 227,324,481 | 228,800,558 | 243,940,045 | 234,766,803 | 229,677,422 | 231,152,905 |
| 1968 | 236,389,006 | 243,371,598 | 253,815,443 | 244,817,778 | 244,125,763 | 242,622,985 |
| 1969 | 229,598,919 | 232,967,958 | 248,424,467 | 241,196,500 | 229,869,576 | 236,673,709 |

### Annual Pearson / closed Top-40 median / competition ratio

Each cell is `Pearson / median weeks / competition ratio`.

| Year | Seed 1001 | Seed 1002 | Seed 1003 |
|---:|---:|---:|---:|
| 1960 | .494339 / 10.5 / 4.204 | .528871 / 11 / 4.222 | .577964 / 11 / 4.315 |
| 1961 | .424083 / 10 / 5.171 | .466723 / 10 / 5.044 | .484858 / 10 / 5.443 |
| 1962 | .405224 / 10 / 5.739 | .412503 / 9 / 5.818 | .465416 / 10 / 6.132 |
| 1963 | .367873 / 10 / 6.395 | .345667 / 10 / 6.425 | .438621 / 10 / 6.470 |
| 1964 | .364771 / 10 / 6.738 | .385291 / 10 / 6.746 | .467953 / 10 / 7.067 |
| 1965 | .357560 / 10 / 7.264 | .376584 / 10 / 7.034 | .418194 / 11 / 7.179 |
| 1966 | .331085 / 10 / 7.340 | .320875 / 10 / 7.217 | .466689 / 10 / 7.457 |
| 1967 | .302052 / 10 / 7.657 | .335894 / 10 / 7.432 | .390497 / 11 / 7.478 |
| 1968 | .340726 / 10 / 7.580 | .368118 / 10 / 7.776 | .388881 / 10 / 7.810 |
| 1969 | .316002 / 10 / 7.886 | .308458 / 10 / 7.886 | .361276 / 10 / 8.007 |

| Year | Seed 1004 | Seed 1005 | Seed 1006 |
|---:|---:|---:|---:|
| 1960 | .589329 / 10 / 4.157 | .538867 / 10 / 4.286 | .552586 / 11 / 4.130 |
| 1961 | .585817 / 10 / 4.926 | .508668 / 9 / 4.986 | .542446 / 10 / 4.967 |
| 1962 | .541200 / 10 / 5.704 | .525188 / 9 / 5.653 | .509789 / 9 / 5.716 |
| 1963 | .514463 / 10 / 6.174 | .487574 / 9 / 6.143 | .477988 / 9 / 6.260 |
| 1964 | .486335 / 10 / 6.655 | .506590 / 10 / 6.402 | .490463 / 10 / 6.733 |
| 1965 | .486760 / 10 / 6.846 | .475266 / 10 / 6.818 | .465609 / 10 / 6.927 |
| 1966 | .506497 / 10 / 7.305 | .526021 / 10 / 6.999 | .425411 / 10 / 7.089 |
| 1967 | .437699 / 10 / 7.272 | .463884 / 10 / 7.194 | .414789 / 10 / 7.375 |
| 1968 | .449944 / 10 / 7.423 | .454774 / 10 / 7.381 | .409639 / 10 / 7.504 |
| 1969 | .456030 / 10 / 7.570 | .426222 / 10 / 7.434 | .397657 / 10 / 7.669 |

### Disabled runtime

| Batch | Seed | Wall time |
|---|---:|---:|
| 1 | 1001 | 345.63 s |
| 1 | 1002 | 339.30 s |
| 1 | 1003 | 355.35 s |
| 2 | 1004 | 232.37 s |
| 2 | 1005 | 235.23 s |
| 2 | 1006 | 235.00 s |

## Structural and telemetry-inertness evidence

The post-telemetry 52-week album-disabled seed-1001 verification reproduces all frozen anchors:

| Check | Result |
|---|---|
| 1960 annual market units | `154,810,982` exact |
| `market-revenue.csv` SHA-256 | `765897841BCB1C62225EF2A7861CF46ACC282FE206A10BADCF895330612A3866` exact |
| `release-capacity.csv` SHA-256 | `8B84BBFDBA5F8B38729F29A1FDF14F8F7598A7AF18DCBAD053BC6C8238A9A461` exact |
| Build | succeeded, 0 warnings, 0 errors |

The annual telemetry reads existing events, record state, album snapshots, label memory, and weekly revenue dictionaries. It has no `GD.Rand*` call and does not invoke simulation decision methods. The byte-identical frozen hashes prove that adding it did not change the established year-one draw order or behavior.

The decade-scale determinism repeat and decade-scale cannibalization/freshness inertness checks were not reached.

## Interrupted Phase 3D-1 evidence

Seeds 1001-1003 were launched together with albums enabled and the frozen configuration. All three remained responsive and CPU-active until manually stopped. Because `StreamWriter` buffers differ by stream, the interrupted files end at different weeks and some final lines are truncated. Final-only streams such as `decade-annual-rollup.csv` remained zero bytes because orderly end-of-run flushing never occurred.

The recovered weekly, record, lifecycle, release-strategy, market-revenue, album-chart, and album-composition streams contain six complete years, 1960-1965. Only those complete years are used below. Partial 1966 rows are excluded. These are diagnostic observations, not checkpoint measurements.

### Milestone-relevant enabled results

| Seed | Year | Album share | Adult Album | Youth Album | Standalone share | Album/Single gross |
|---:|---:|---:|---:|---:|---:|---:|
| 1001 | 1960 | 26.62% | 57.63% | 12.11% | 0.00% | 0.062 |
| 1001 | 1961 | 55.47% | 83.62% | 50.72% | 0.00% | 0.245 |
| 1001 | 1962 | 84.34% | 94.93% | 91.26% | 3.56% | 0.677 |
| 1001 | 1963 | 88.87% | 95.59% | 95.78% | 14.25% | **1.244** |
| 1001 | 1964 | 90.04% | 93.06% | 96.07% | 32.89% | 1.612 |
| 1001 | 1965 | 88.10% | 94.01% | 94.32% | 40.85% | 1.624 |
| 1002 | 1960 | 24.17% | 54.92% | 10.74% | 0.00% | 0.051 |
| 1002 | 1961 | 54.17% | 81.67% | 50.61% | 0.00% | 0.213 |
| 1002 | 1962 | 82.67% | 92.51% | 90.95% | 3.05% | 0.637 |
| 1002 | 1963 | 88.64% | 93.01% | 97.63% | 13.96% | **1.250** |
| 1002 | 1964 | 90.32% | 92.80% | 96.23% | 35.60% | 1.570 |
| 1002 | 1965 | 89.40% | 93.22% | 95.05% | 43.94% | 1.599 |
| 1003 | 1960 | 26.27% | 55.69% | 12.09% | 0.00% | 0.056 |
| 1003 | 1961 | 58.34% | 84.94% | 52.93% | 0.00% | 0.251 |
| 1003 | 1962 | 86.59% | 93.93% | 92.60% | 3.23% | 0.726 |
| 1003 | 1963 | 90.31% | 94.69% | 96.35% | 13.22% | **1.234** |
| 1003 | 1964 | 90.83% | 95.78% | 97.14% | 38.57% | 1.632 |
| 1003 | 1965 | 90.48% | 97.19% | 95.03% | 51.51% | 1.799 |

Interpretation:

- The 1960 mix gate passes in all three recovered seeds.
- Album choice then jumps to 54%-58% in 1961 and 83%-87% in 1962. The intended rising arc is an abrupt takeover.
- Revenue crosses in 1963 in all three seeds, a binding early-crossover failure.
- Standalone choice remains zero in 1960-1961 but begins in 1962, a binding withheld-ordering failure.
- The contingency trigger does not fire: Youth Album share is never below 2%; it exceeds 50% in 1961 and 90% from 1962 onward.
- Rung 1 only affects pooled appeal and cohesion ceilings. These results strongly suggest the timing failure is rooted in the decision/demand path identified in the directive's correction 1, not merely chart-quality timing. No calibration was attempted after this diagnostic.

### Recovered singles guards

Annual Pearson populations use distinct Singles charting within the year, with outcome `101 - best annual position`. Closed medians use terminal lifecycle rows mapped to the terminal year. Deltas are against the same-seed frozen disabled year.

| Seed | Year | Pearson delta | Disabled/enabled median | Median delta |
|---:|---:|---:|---:|---:|
| 1001 | 1960 | -0.02630 | 10.5 / 10 | -0.5 |
| 1001 | 1961 | +0.02834 | 10 / 10 | 0 |
| 1001 | 1962 | -0.02807 | 10 / 10 | 0 |
| 1001 | 1963 | +0.08556 | 10 / 12 | +2 |
| 1001 | 1964 | +0.14817 | 10 / 12 | +2 |
| 1001 | 1965 | +0.16658 | 10 / 13 | **+3** |
| 1002 | 1960 | -0.03179 | 11 / 11 | 0 |
| 1002 | 1961 | +0.02790 | 10 / 10 | 0 |
| 1002 | 1962 | +0.05210 | 9 / 11 | +2 |
| 1002 | 1963 | +0.11534 | 10 / 12 | +2 |
| 1002 | 1964 | +0.17731 | 10 / 13 | **+3** |
| 1002 | 1965 | +0.16591 | 10 / 13 | **+3** |
| 1003 | 1960 | +0.01248 | 11 / 11 | 0 |
| 1003 | 1961 | +0.04715 | 10 / 10 | 0 |
| 1003 | 1962 | +0.09175 | 10 / 11 | +1 |
| 1003 | 1963 | +0.14991 | 10 / 12 | +2 |
| 1003 | 1964 | +0.11041 | 10 / 13 | **+3** |
| 1003 | 1965 | +0.19083 | 11 / 13 | +2 |

No recovered Pearson year violates the `-0.06` floor. The ten-year mean cannot be evaluated. The median hard gate is already impossible because each seed has at least one year worse than `+/-2`.

### Report-only emergence evidence

Album composition rows show zero Concept albums through 1965 in all three seeds. Mean thematic cohesion is `0.080` in every recovered seed-year despite the configured cohesion-rise window beginning in 1964. The intended concept emergence and filler-death narrative is therefore not visible in the complete recovered interval.

The 1969 substitution, non-adult album-chart share, genre pivot ordering, final freshness bound, full memory convergence, two-sided decade error, cancellation/transfer reconciliation, and final B5 gradient cannot be adjudicated from an interrupted 1966 run.

## Extreme album-enabled runtime

### Observation

- Disabled decades completed in 3.9-5.9 minutes per seed in three-process batches.
- Enabled seeds 1001-1003 ran for approximately 2.5 hours wall time without reaching the end of 1966.
- At the last recorded health sample, each simulation had consumed about `8,242-8,249` CPU seconds (roughly 137.5 CPU-minutes), remained responsive, and used only 268-312 MB. They continued running for several minutes after that sample.
- The three interrupted runs had already flushed 792,375,296; 787,443,712; and 767,275,008 bytes across 22 nonempty CSVs. Seven final/small streams remained empty.
- The last fully parseable `weeks.csv` rows were around weeks 317-318, early 1966. Other buffered streams reached later 1966 weeks, confirming continued forward progress rather than a deadlock.

This was CPU-bound pathological scaling, not a hang or a memory leak.

### Leading theory: quadratic record lookup under catalog growth

`ChartManager.GetRecordRuntimeData` uses `allRecords.FirstOrDefault`, a linear scan. `CompetitorManager.CalculateLabelRevenue` loops over every active record ID for every active label and calls that linear lookup once per ID. With `N` active records, this is approximately `O(N^2)` lookup work every week.

The enabled active pool grew from roughly 2,400-2,600 records at end-1960 to 14,125-14,680 at end-1965:

| Year end | Seed 1001 | Seed 1002 | Seed 1003 |
|---:|---:|---:|---:|
| 1960 | 2,537 | 2,439 | 2,589 |
| 1961 | 4,871 | 4,695 | 5,069 |
| 1962 | 8,296 | 8,078 | 8,562 |
| 1963 | 11,160 | 11,046 | 11,513 |
| 1964 | 13,193 | 13,058 | 13,513 |
| 1965 | 14,234 | 14,125 | 14,680 |

At 14,000 active records, an `N`-by-linear-scan pattern can imply on the order of 196 million record comparisons per simulated week before other simulation work. This is the strongest code-grounded explanation for the superlinear slowdown.

Album updates add a second instance of the same risk: every linked Album calls `GetRecordRuntimeData(linkedPromoSingleId)` during `AlbumSimulator.UpdateAlbum`, again scanning the full active list.

### Catalog retention amplifies every weekly pass

Albums retire only when they are off-chart, below `albumCatalogSalesFloor = 10`, and either never-charted for 26 weeks or charted and simultaneously 52 weeks past both charting and above-floor sales. The recovered decision mix creates thousands of Albums annually, while long-tail demand can keep them at or above ten units. This makes the active catalog much larger than the disabled Single-only pool.

`ChartManager.SimulateWeek` then performs several full active-record passes, including six regional sales/state iterations per record, chart construction/sorting, relevance updates, and retirement checks. These paths are mostly `O(N * regions)` or `O(N log N)` rather than quadratic, but at 14,000 records they become substantial and magnify the lookup problem.

### Lifetime project scans are a secondary suspect

`CompetitorManager.ProcessDueAlbumProjects` filters and orders the entire lifetime `albumProjects` list every week. Released and cancelled projects remain in that list for audit/reconciliation. With tens of thousands of decisions, this adds a growing `O(P)` scan and avoidable ordering work each week. It is unlikely to dominate the quadratic record lookup, but it contributes to the decade slope.

### Telemetry volume contributes but does not explain the shape alone

The full harness observes and writes every active record across several streams. By interruption it had emitted about 2.35 GB for only three partial runs. That increases serialization and disk cost, and `CaptureWeek` itself performs multiple `GetAllRecords`/grouping passes. However, all Godot workers remained continuously CPU-saturated with stable memory, and the production weekly loops already contain the catalog-size hot paths above. Telemetry is an amplifier, not a sufficient explanation for the disabled/enabled runtime ratio.

### Recommended performance-only investigation before another decade run

These are investigation targets, not changes authorized or made by this pass:

1. profile one enabled seed by simulated year and record active count;
2. index live records by ID so `GetRecordRuntimeData` is `O(1)`, preserving `allRecords` order separately for deterministic iteration;
3. maintain a due-project queue/index instead of filtering and sorting all historical projects weekly;
4. measure Album retirement cohorts to determine why so many titles remain active at or above ten units;
5. add explicit periodic flush/progress rows for long runs so an interrupted process retains the annual rollup; and
6. compare a no-telemetry enabled run against the full harness to isolate observation overhead without changing simulation behavior.

Any optimization must preserve iteration order, RNG calls, and the frozen year-one hashes before its output can be used for Directive 3D.

## Calibration and contingency log

No Rung-1 or Rung-2 value changed. Therefore there is no before/after calibration mix to report.

The comp launch-awareness contingency trigger did not fire in the recovered complete years: Youth Album share was 10.74%-12.11% in 1960, exceeded 50% in 1961, and exceeded 90% thereafter. `compAwarenessScalar` was not added.

## Work not completed

- enabled diagnostic seeds 1004-1006;
- any six-seed calibration checkpoint;
- decade determinism repeat;
- decade-long project and memory reconciliation;
- decade cannibalization/freshness inertness proof;
- complete 1969 milestone table and emergent-arc narrative;
- one-shot enabled/disabled hold-outs 2001-2003; and
- a pass decision.

## Final disposition

**Fail/incomplete checkpoint.** Phase 3D cannot pass from this evidence, and the recovered complete interval already contains binding early-crossover, withheld-ordering, and median-guard failures. The implementation remains at a coherent telemetry-only checkpoint with frozen simulation mechanics and no calibration changes.
