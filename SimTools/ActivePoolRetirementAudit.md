# Active-Pool Retirement Audit

Audit date: 2026-07-01  
Runtime: Godot 4.7 Mono, headless  
Measured runs: 52 weeks, seeds 1001 / 1002 / 1003  
Before: `retirement-baseline-{1001,1002,1003}`  
After: `retirement-fix-{1001,1002,1003}`

## Executive result

The charted-record retirement asymmetry is confirmed and fixed. Before the change, charted records retired at median age 31, 25 weeks after their last Top-100 appearance. At week 52, 55-79 charted records per seed were simultaneously off-chart, below 50 units/week, and still active. After the change, that zombie cohort is zero in all three seeds and charted retirement age falls to 23 weeks.

Active records improve from 1,844-1,893 to 1,737-1,806, but do **not** reach the 1,450-1,700 target. The remaining off-chart pool is overwhelmingly recent never-charted records or records still selling at least 50 units/week. Reaching the target would require changing release inflow, the never-charted horizon, or the sales relevance floor; none is authorized by this retirement-tail pass.

The seed fix is complete. Two independent seed-1001 processes produced byte-identical weekly, record, lifecycle, breakout, and retirement CSVs.

## 1. Deterministic startup seeding

`SimulationSeedBootstrap` is now the first autoload. When `--seed=N` is present, it calls `GD.Seed(N)` before `ChartManager` creates labels and before artist, roster, competitor, record, or prewarm generation. `ChartAuditRunner` reapplies the same seed after prewarm, preserving deterministic measured-period random state as well.

The seed-1001 verification hashes matched for all five outputs:

| Output | Identical across processes | SHA-256 prefix |
|---|---|---|
| weeks.csv | Yes | `4F3A4A56A35FD949` |
| records.csv | Yes | `38285908DCF6678C` |
| lifecycles.csv | Yes | `E98045D5E6F9C2A4` |
| breakout-funnel.csv | Yes | `91BEBD2388DEEC38` |
| retirement.csv | Yes | `6D4DE92F3BA0010B` |

## 2. Instrumentation and baseline diagnosis

Each record now tracks the release age when it last held a Top-100 position and when it last sold at least `RetirementSalesFloor` units. The audit runner writes every retirement plus every active off-chart record at week 52 to `*-retirement.csv`, including:

- age and cumulative chart weeks;
- weeks since last Top-100 position;
- weeks since sales last cleared 50 units;
- current sales;
- the radio signal used for retirement.

Baseline charted-retirement evidence:

| Seed | Charted retired | Median age | Median weeks since chart | Median weeks since sales >=50 | Median total radio | Week-52 charted zombies |
|---|---:|---:|---:|---:|---:|---:|
| 1001 | 592 | 31 | 25 | 8 | 0.075 | 79 |
| 1002 | 599 | 31 | 25 | 7 | 0.076 | 69 |
| 1003 | 587 | 31 | 25 | 7 | 0.077 | 55 |

This confirms the decay-clock hypothesis directly from the live region resources. A one-week chart appearance placed a record on a much longer retirement track; summed regional radio delayed eligibility long after national chart relevance ended.

## 3. Retirement behavior change

The existing outer gates remain: a record must be off-chart and below 50 units/week.

For charted records, retirement now occurs when either:

1. the existing four-week radio check finds retirement radio below `0.1`; or
2. the record has gone eight weeks since its last Top-100 appearance or eight weeks since it last cleared the sales floor.

The time bound is evaluated weekly. A brief historical chart appearance can no longer grant an open-ended reprieve.

For retirement only, each region's radio contribution is capped at `0.05` before summing. This retains radio as protection for genuinely broad catalog play while preventing one or two low-difficulty regions from dominating the national cull signal. Normal regional radio, awareness, sales, chart points, and breakout mechanics are unchanged.

`BASE_PURCHASE_RATE` remains `0.07f`, and the stock depth/floors from the market-volume pass were not changed.

## 4. Retirement and active-pool results

| Seed | Active before | Active after | Change | Charted retired after | Median retirement age after | Week-52 charted zombies after |
|---|---:|---:|---:|---:|---:|---:|
| 1001 | 1,849 | 1,765 | -84 | 679 | 23 | 0 |
| 1002 | 1,844 | 1,737 | -107 | 676 | 23 | 0 |
| 1003 | 1,893 | 1,806 | -87 | 656 | 23 | 0 |

The mechanism is responsive: charted retirements rise by 57-87 and the active pool falls by 84-107. A four-week diagnostic did not improve the result; two seeds were unchanged and one worsened through release-scheduling feedback. The eight-week setting is therefore retained as the least aggressive effective bound.

At week 52 after the fix, no charted/off-chart record below 50 units remains. The remaining off-chart composition is:

| Seed | Off-chart active | Charted | Never charted | Charted under 50 | Never charted under 50 |
|---|---:|---:|---:|---:|---:|
| 1001 | 1,665 | 258 | 1,407 | 0 | 11 |
| 1002 | 1,637 | 287 | 1,350 | 0 | 12 |
| 1003 | 1,706 | 297 | 1,409 | 0 | 22 |

The residual active-pool miss is therefore not another charted zombie tail.

## 5. Chart health, volume, and indie survival

| Metric | Before | After | Read |
|---|---:|---:|---|
| Annual market units | 191.1-205.7M | 191.9-207.2M | Stable; retirement does not control demand volume |
| Active records, week 52 | 1,844-1,893 | 1,737-1,806 | Improved; target still missed |
| New Top-100 entries/week | 19.10-19.19 | 18.87-19.40 | Stable |
| Closed Top-40 life, median | 11 weeks | 11 weeks | Stable |
| Distinct #1 records | 15-19 | 16-19 | Stable |
| Quality/outcome Pearson | 0.455-0.542 | 0.467-0.565 | Stable/improved |
| Pooled Independent week-14 | 11/3,769 (0.292%) | 8/3,755 (0.213%) | Passes 0.15% floor |
| Pooled Boutique week-14 | 2/1,342 (0.149%) | 3/1,384 (0.217%) | Passes 0.10% floor |
| Seeds with indie-family entrant | 3/3 | 3/3 | Pass |

The retirement correction does not restructure the chart. Indie rates remain rare but observable, and Major remains the decisive tier.

## 6. Acceptance read and recommendation

| Condition | Result | Status |
|---|---:|---|
| Deterministic startup and measured period | Byte-identical seed-1001 outputs | **Pass** |
| Charted zombie mechanism removed | 0 week-52 charted zombies in all seeds | **Pass** |
| Active records 1,450-1,700 | 1,737-1,806 | **Fail** |
| Indie-family charting survives | All seeds; pooled tier floors pass | **Pass** |
| Chart health stable | Entry, life, #1, and correlation stable | **Pass** |

The reproducibility and charted-retirement work is complete, but the broader active-pool target is not. Further retirement tightening is not justified: the eight-week rule already leaves no charted under-floor zombies, and the four-week diagnostic did not help.

The next lifecycle pass should measure release inflow against the 14-week never-charted residence time and determine whether 50 units/week is an appropriate relevance floor for a 1,800-record market. That is distinct from charted retirement and should be calibrated without changing conversion, stock, launch boost, or chart rank.

Distribution deals remain premature until the residual recent/never-charted pool and market volume are resolved together.

## Artifacts

- Baseline retirement telemetry: `SimLogs/retirement-baseline-{1001,1002,1003}-retirement.csv`
- Final retirement telemetry: `SimLogs/retirement-fix-{1001,1002,1003}-retirement.csv`
- Baseline chart analysis: `SimLogs/retirement-baseline-1001_retirement-baseline-1002_retirement-baseline-1003-analysis.json`
- Final chart analysis: `SimLogs/retirement-fix-1001_retirement-fix-1002_retirement-fix-1003-analysis.json`
- Final regional analysis: `SimLogs/retirement-fix-1001_retirement-fix-1002_retirement-fix-1003-regional-breakout-analysis.json`

`SimLogs` remains ignored scratch output. This audit and the runner instrumentation are the durable artifacts.
