# Chart Simulation Realism Audit — Structural Fixes #1–3

Audit date: 2026-06-30  
Runtime: Godot 4.7 Mono, headless  
Execution path: `TimeManager` weekly event → `RosterManager` / `CompetitorManager` → `ChartManager.SimulateWeek` → `ChartSimulator`

## Method

The removable `ChartAuditRunner` scene advances the real autoload chain after normal artist, label, roster, region, initial-record, and eight-week prewarm initialization. The harness exports record, week, and completed-lifecycle CSVs under gitignored `SimLogs/`. Three independent 52-week processes were run for every retained or rejected mechanism. A separate aggregate-only run covered 260 actual chart weeks for pool stability.

The runner seeds the measured period after startup; each process therefore also receives an independently randomized startup population. `targetActiveRecords` stayed at 500 and no player actions were made. The harness now advances until `ChartManager.currentChartWeek` changes, so critical Grammy interruptions cannot create false measured “weeks.”

The project still emits the pre-existing non-fatal `MissingSingletonsTemp.cs` autoload filename/type warning. Its only caller is null-safe, every audit run reached `CHART_AUDIT_COMPLETE`, and it remains outside this directive.

## Original baseline

| Metric | Target | Baseline run 1 | Baseline run 2 | Baseline run 3 |
|---|---:|---:|---:|---:|
| Distinct #1 records | 20–35 | 15 | 11 | 12 |
| Median consecutive weeks at #1 | mostly 1–3 | 2.5 | 4.0 | 4.5 |
| Closed Top-40 chart life, median | 10–16 weeks | 34 | 34 | 32 |
| Observed #1 lifetime units, median | low 100Ks–low millions | 3.96M | 4.18M | 3.65M |
| Peak #40–70 lifetime units, median | tens of thousands | 121K | 90K | 110K |
| Annual market units | plausible national scale | 176.6M | 191.4M | 179.8M |
| Active records, week 52 | bounded pool | 4,509 | 4,633 | 4,698 |

## Fix 1 — unified regional exhaustion

### Implementation

- `SATURATION_POWER` is now load-bearing in the regional curve: `1 / (1 + (penetration × 3)^SATURATION_POWER)`.
- The independent flat 8,000,000-unit saturation calculation was removed.
- `record.saturation` is now the potential-audience-weighted average of the same six regional penetration values used by sales. Algebraically, this is total regional units sold divided by total potential regional audience.
- Saturation is updated after regional sales totals, making it a derived telemetry/UI value rather than a second gate.

### Trials

| Trial | Annual units, 3-run range | Closed Top-40 median | Saturation result | Decision |
|---|---:|---:|---|---|
| Baseline disconnected field | 176.6–191.4M | 32–34 | Pooled next-week change correlation ~+0.02; could exceed 1 | Replaced |
| Prescribed power `2.2` | 194.9–203.4M | 31–33.5 | Max penetration 0.264–0.330; pooled correlation +0.018 to +0.029 | Rejected: weakened exhaustion throughout observed range |
| Power `0.45` | 154.3–164.5M | 27–32 | Max penetration 0.260–0.354 | Retained as the unified exhaustion calibration |

The original pooled correlation between penetration and *proportional* next-week change remains near zero after all fixes because it mixes thousands of releases with radically different baselines and many near-zero denominators. The within-record diagnostic is meaningful: in the final three runs, median correlation between penetration and next-week sales normalized to that record’s peak is −0.959, −0.956, and −0.960. Maximum final saturation is 0.121–0.149. This shows sales falling strongly as the same record penetrates its audience, while avoiding the old cross-record aggregation artifact.

## Fix 2 — genuine lifecycle decline

### Implementation

- National awareness receives age-gated decay after week 8.
- `AWARENESS_DECAY_RATE = 0.95` is applied after normal weekly growth.
- A separate direct demand-age mechanism was tested because Top-10 records have an intentional 0.7 effective-awareness floor that bypasses awareness decline.
- `DEMAND_AGE_DECAY_RATE = 0.91` now applies to conversion after week 8. It is independent of awareness, radio fatigue, and saturation.

### Awareness-only trials

| Rate | Distinct #1s | Closed Top-40 median | Result |
|---|---:|---:|---|
| No national decay | 11–15 | 32–34 | Baseline failure |
| `0.97` | 11–12 | 16.5–18 | Lifecycle nearly fixed; #1 turnover unchanged |
| `0.95` | 9–11 | 15–17 | Best lifecycle fit; retained, but cannot overcome Top-10 awareness floor alone |

### Separate demand-age trials

| Rate / state | Distinct #1s | Median #1 tenure | Closed Top-40 median | Decision |
|---|---:|---:|---:|---|
| `0.92`, before retirement interaction | 20–22 | 2–2.5 | 15 | Successful starting point |
| `0.92`, after unified retirement | 17–18 | 2–3 | 15 | Interaction pushed turnover slightly low |
| `0.91`, final combined | 17–23 (mean 19.7) | 2–3 | 14–15 | Retained; best three-run distribution |
| `0.90`, combined | 16–21 (mean 19.0) | 2 | 14–15 | Rejected; no improvement over `0.91` |

## Fix 3 — release and retirement balance

### Implementation

- Never-charted records retire weekly after 14 weeks when weekly sales fall below 50.
- The existing charted-record criteria remain on a four-week cadence.
- `ChartManager` owns one retirement path. It finalizes the artist once, removes competitor active-record bookkeeping, then removes the runtime record.
- `RosterManager.RecordChartRunComplete` is idempotent via `artistChartRunCompleted`; never-charted releases now correctly register flops.
- The independent revenue-loop retirement criterion was removed from `CompetitorManager`.
- Weekly release chance scales as `1 + yearOffset × 0.30`. The initial 0.10 trial failed to produce realized growth because roster availability and label attrition dominated it.
- Telemetry now exports weekly new-record and retired-record flows in addition to active stock.

### One-year results

| Metric | Baseline | Final run 1 | Final run 2 | Final run 3 |
|---|---:|---:|---:|---:|
| Active records, week 52 | 4,509–4,698 | 1,126 | 1,144 | 1,150 |
| Closed lifecycles/year | 138–196 | 3,018 | 3,262 | 3,241 |
| New Top-100 entries/week | 14.2–16.7 | 28.7 | 25.6 | 25.3 |

The higher entry rate is a direct consequence of finite hit lifecycles and a continuously replenished national release market. It also raises the volatility caveat described below.

### Five-year pool stability and realized release flow

Final validation used `AnnualReleaseGrowthRate = 0.30`, 260 measured chart weeks, and aggregate-only telemetry.

| Year | New releases | Retirements | Active start | Active end | Net within year | Final-quarter slope/week |
|---:|---:|---:|---:|---:|---:|---:|
| 1960 | 3,974 | 3,230 | 566 | 1,220 | +654 | −3.92 |
| 1961 | 3,959 | 3,882 | 1,242 | 1,297 | +55 | +0.52 |
| 1962 | 4,357 | 4,242 | 1,308 | 1,412 | +104 | −0.10 |
| 1963 | 4,477 | 4,455 | 1,427 | 1,434 | +7 | −2.33 |
| 1964 | 4,606 | 4,549 | 1,453 | 1,491 | +38 | +0.59 |

The initial unretired campaign backlog peaks at 1,493 in the first half-year, then clears. Thereafter the pool follows a slowly moving equilibrium as intentional release volume rises: 1963–64 endpoints are 1,434 and 1,491, versus the old 4,500+ after only one year. The final-year regression slope is +0.88 records/week against roughly 88 new releases/week; retirement tracks 98.8% of new releases in 1964.

## Final combined audit

The final three-seed results use all retained Fixes 1–3 together. The 1960 year multiplier is 1.0, so the later release-scaling coefficient does not change these runs.

| Metric | Target | Baseline | Final run 1 | Final run 2 | Final run 3 | Assessment |
|---|---:|---:|---:|---:|---:|---|
| Distinct #1 records | 20–35 | 11–15 | 17 | 23 | 19 | Mean 19.7; essentially on lower bound, with population variance |
| Median #1 tenure | mostly 1–3 | 2.5–4.5 | 2 | 2 | 3 | In range |
| Maximum #1 tenure | rare up to 8–10 | 8–10 | 6 | 5 | 5 | Healthy, no parked leaders |
| Closed Top-40 chart life | 10–16 weeks | 32–34 | 15 | 14 | 14 | In range |
| Observed #1 lifetime units | low 100Ks–low millions | 3.65–4.18M | 1.72M | 1.64M | 1.54M | In range |
| Peak #40–70 lifetime units | tens of thousands | 90–121K | 42K | 50K | 39K | In range |
| Annual market units | plausible national scale | 176.6–191.4M | 120.4M | 131.3M | 118.3M | Still plausible; materially less hit inflation |
| Active records, week 52 | bounded | 4,509–4,698 | 1,126 | 1,144 | 1,150 | Fixed |
| Top-2 point/unit ties | none expected | 0 | 0 | 0 | 0 | Sorting artifacts remain ruled out |

## Remaining findings for human review

1. **Published-chart volatility increased.** Median continuing-record movement is now 6–7 positions and roughly 20–21% of continuing transitions exceed 20 places, versus 3–4 and 12–16% at baseline. The extreme #80↔#5 cases remain zero. This likely needs a separate inertia/entry-volume audit; changing it here would exceed Findings #1–3 and bundle another mechanism.
2. **Pooled saturation correlation remains misleading.** The requested cross-record proportional-change statistic stays near zero, while within-record normalized correlations are about −0.96. The field is now structurally correct; any UI interpretation should describe penetration level, not promise a cross-catalog sales predictor.
3. **Late-decade release volume remains model-dependent.** Realized releases rise through 1964 and the configured factor reaches 3.7× by 1969, but roster availability, bankruptcies, and label lifecycle constrain actual output. A full-decade economy audit is needed before claiming the 6,000–9,000 historical assumption is met.
4. **Findings #4 and #5 remain out of scope.** The indie/major gap and quality determinism were measured but intentionally not modified in this pass.
5. **Additional disconnected configuration remains.** `ChartManager.targetActiveRecords`, `ChartManager.recordIdCounter`, and `CompetitorManager`'s `historicalRecordsCount`, `baseRoyaltyRate`, and `monthlyOverheadRate` are declared but not read. They were not changed because they do not feed the requested weekly chart math, but should be removed or connected in a separate cleanup.

## Final retained values

| Setting | Final value |
|---|---:|
| `SATURATION_POWER` | `0.45f` |
| `AWARENESS_DECAY_RATE` | `0.95f` |
| `DEMAND_AGE_DECAY_RATE` | `0.91f` |
| Never-charted horizon | 14 weeks |
| Retirement sales floor | 50 units/week |
| `AnnualReleaseGrowthRate` | `0.30f` per year offset |

## Artifacts

- Harness: `SimTools/ChartAuditRunner.cs` and `ChartAuditRunner.tscn`
- Audit analyzer: `SimTools/analyze-chart-audit.mjs`
- Stability analyzer: `SimTools/analyze-pool-stability.mjs`
- Final three-run analysis: `SimLogs/combined-r091-1_combined-r091-2_combined-r091-3-analysis.json`
- Final five-year stability analysis: `SimLogs/release-scale-030-5y-stability.json`
- Full raw final run: `SimLogs/combined-r091-1-records.csv`, `-weeks.csv`, and `-lifecycles.csv`

Raw logs are scratch output and remain gitignored.
