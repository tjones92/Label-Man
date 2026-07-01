# Market Volume and Never-Charted Turnover Audit

Audit date: 2026-07-01  
Runtime: Godot 4.7 Mono, headless  
Measured runs: 52 weeks, seeds 1001 / 1002 / 1003  
Baseline: `turnover-baseline-a-{1001,1002,1003}`  
Final: `market-turnover-hybrid-{1001,1002,1003}`

## Executive result

The directive passes. Final annual market volume is **156.5-174.5M**, week-52 active records are **1,793-1,923**, and the confirmed-dead never-charted tail is zero in every seed. Charted zombies remain zero. Pooled Independent age-14 charting is **29/3,804 (0.762%)** and Boutique is **3/1,274 (0.235%)**, both above their guards.

The retained turnover rule is market-signal based: a never-charted record must be off-chart, below 50 units, and either five weeks removed from its last 50-unit week or older than 18 weeks. The second clause is a catalog backstop, not a tier penalty. It prevents an old title from resetting a short under-floor clock indefinitely while allowing a younger slow builder to recover whenever sales clear 50.

The original catalog-tail volume hypothesis was instrumented and tested first. A strong all-tier tail curve hit volume but shortened closed Top-40 life to 8-9 weeks; a chart-history-only curve erased the age-14 indie guards. Both were rejected. The retained mechanism follows the directive's fallback: Major and MidTier generated 75-85% of baseline volume, so their demand is scaled to 0.60 and 0.85 respectively. Indie-family conversion, `BASE_PURCHASE_RATE`, stock depth, launch boost, chart points, rank, and breakout mechanics are unchanged.

## 1. Phase A telemetry and turnover selection

The runner now writes `*-tier-volume.csv`, decomposed into release ages 1-3, 4-8, and 9+, and adds `floorBreachAge` to retirement telemetry. `floorBreachAge` is one week after the last sale at or above 50; cull age and the full under-floor interval are therefore directly recoverable.

The baseline exposed a flaw in the original diagnosis: age-14 culling was not consistently granting nine dead weeks. Most never-charted records remained healthy past age 14 and were then retired on their first under-floor week.

| Seed | Never-charted retired | Median floor-breach age | Median cull age | Median under-floor weeks at cull |
|---|---:|---:|---:|---:|
| 1001 | 2,756 | 18 | 18 | 1 |
| 1002 | 2,775 | 18 | 18 | 1 |
| 1003 | 2,809 | 19 | 19 | 1 |

A pure age-8 diagnostic cleared the week-52 under-floor tail but pulled one seed to 1,625 active records and continued to cull healthy older records on a single dip. Sales-floor recency produced the cleaner separation. The same-mechanism 5-8 sweep showed the equilibrium cost of each additional grace week:

| Floor-recency horizon | Annual units | Week-52 active | Week-52 never-charted already 6 weeks under floor |
|---:|---:|---:|---:|
| 5 | 160.1-176.2M | 1,668-1,760 | 0 |
| 6 | 165.9-175.4M | 1,783-1,809 | 0 |
| 7 | 165.3-179.7M | 1,849-1,894 | 58-79 |
| 8 | 163.3-179.5M | 1,854-1,982 | 135-164 |

Five weeks is retained as the primary dead-stock clock because it is the least setting in the requested range that leaves no record at six confirmed-dead weeks. The 18-week backstop was added after the final demand calibration exposed 180-215 old records per seed that were below the floor but had recently reset the clock. It operates only inside the unchanged off-chart/under-50 gate.

In the final runs, 481-536 never-charted retirements per seed were caused by the five-week clock. The remaining 2,104-2,247 were old-catalog backstop retirements, usually after a recent first dip. No never-charted record remained six weeks under floor at week 52.

## 2. Phase B: isolated turnover effect

With volume constants held at baseline, the five-week recency rule alone changed annual units from 191.9-207.2M to 185.9-200.5M and active records from 1,737-1,806 to 1,994-2,153. This counterintuitive pool increase is why raw age was not replaced by recency without a catalog backstop: intermittent healthy sales correctly reset the clock, but too many older records then accumulated.

The isolated run confirmed the desired behavior—every recency retirement occurred at exactly five under-floor weeks—but did not by itself satisfy the pool equilibrium.

## 3. Phase C: volume mechanism

Baseline pooled three-seed volume by tier and release window was:

| Tier | Ages 1-3 | Ages 4-8 | Ages 9+ | Total |
|---|---:|---:|---:|---:|
| Major | 66.3M | 108.3M | 70.2M | 244.9M |
| MidTier | 83.0M | 93.7M | 62.7M | 239.4M |
| Independent | 30.8M | 26.4M | 20.0M | 77.2M |
| Boutique | 10.1M | 8.2M | 6.1M | 24.4M |
| Small | 6.1M | 4.3M | 2.5M | 12.9M |

Major and MidTier supplied 484.3M of 598.8M pooled units (80.9%). Major's age-9+ units were almost entirely previously charted records (20.4-26.1M per seed). This justified testing catalog-tail decay before any purchase-rate or stock change.

Rejected diagnostics:

- all-tier decay of 0.68 after age 8: volume nearly passed, but active records fell to 1,623-1,704;
- chart-history decay of 0.50 after age 8: created 14-20 week-52 charted zombies;
- chart-history decay of 0.75 from age 7: volume passed in two seeds, but closed Top-40 median fell to 8-9 weeks;
- Major+MidTier scale of 0.82: chart life passed, but quality/outcome Pearson fell below 0.45 in two seeds and the active pool exceeded 2,000.

The retained demand scales are Major 0.60 and MidTier 0.85. They act before visibility and launch multipliers, preserve within-tier lifecycle shape, and leave the indie-family path untouched. Final pooled decomposition is:

| Tier | Ages 1-3 | Ages 4-8 | Ages 9+ | Total |
|---|---:|---:|---:|---:|
| Major | 35.3M | 43.2M | 27.9M | 106.4M |
| MidTier | 78.2M | 103.0M | 67.6M | 248.7M |
| Independent | 34.3M | 35.9M | 27.5M | 97.7M |
| Boutique | 10.0M | 9.3M | 7.3M | 26.6M |
| Small | 6.4M | 5.1M | 3.0M | 14.5M |

The non-Major increases are equilibrium feedback, not direct boosts. No indie-family multiplier changed. A blanket purchase-rate or stock-depth cut was rejected because the prior audit already demonstrated that it removed the narrow indie margin while barely moving the pool.

## 4. Before/after market, pool, and off-chart composition

| Seed | Annual units before | Annual units after | Active before | Active after |
|---|---:|---:|---:|---:|
| 1001 | 191.9M | 156.5M | 1,765 | 1,847 |
| 1002 | 199.7M | 162.9M | 1,737 | 1,793 |
| 1003 | 207.2M | 174.5M | 1,806 | 1,923 |

Week-52 off-chart composition:

| Seed | Phase | Off-chart active | Charted | Never charted | Charted under 50 | Never charted under 50 |
|---|---|---:|---:|---:|---:|---:|
| 1001 | Before | 1,665 | 258 | 1,407 | 0 | 11 |
| 1001 | After | 1,747 | 289 | 1,458 | 0 | 85 |
| 1002 | Before | 1,637 | 287 | 1,350 | 0 | 12 |
| 1002 | After | 1,693 | 285 | 1,408 | 0 | 82 |
| 1003 | Before | 1,706 | 297 | 1,409 | 0 | 22 |
| 1003 | After | 1,823 | 290 | 1,533 | 0 | 77 |

The larger under-floor snapshot is intentional: these are records inside the five-week confirmation window, not confirmed-dead stock. None has reached six weeks under floor.

## 5. Indie floors, chart health, and reproducibility

| Metric | Seed 1001 | Seed 1002 | Seed 1003 | Target | Read |
|---|---:|---:|---:|---:|---|
| Annual units | 156.5M | 162.9M | 174.5M | 150-180M | Pass |
| Active records | 1,847 | 1,793 | 1,923 | ~1,800; modestly higher accepted | Pass |
| New Top-100 entries/week | 20.62 | 20.54 | 20.21 | 16-21 | Pass |
| Closed Top-40 median life | 11 | 11 | 10 | 10-13 | Pass |
| Quality/outcome Pearson | 0.485 | 0.506 | 0.568 | 0.45-0.62 | Pass |
| Week-52 charted zombies | 0 | 0 | 0 | 0 | Pass |

Age-14 pooled tier guards:

| Tier | Seed counts | Pooled result | Floor | Read |
|---|---|---:|---:|---|
| Independent | 9/1,244; 4/1,319; 16/1,241 | 29/3,804 (0.762%) | >=0.15% | Pass |
| Boutique | 1/489; 0/406; 2/379 | 3/1,274 (0.235%) | >=0.10% | Pass |

An indie-family age-14 entrant appears in all three seeds, exceeding the two-of-three guard.

Two independent seed-1001 processes were byte-identical across all six outputs:

| Output | SHA-256 prefix |
|---|---|
| weeks.csv | `BE5F8B973097D840` |
| records.csv | `D55A7F24838DE021` |
| lifecycles.csv | `6069EAEFC1C94D01` |
| breakout-funnel.csv | `98141AE6A58BB1FB` |
| retirement.csv | `6756903182357915` |
| tier-volume.csv | `1496B3E5B56CA392` |

## 6. Acceptance read and recommendation

| Condition | Result | Status |
|---|---:|---|
| Annual market units, all seeds | 156.5-174.5M | **Pass** |
| Active records at week 52 | 1,793-1,923 | **Pass** |
| Never-charted turnover | 5-week floor clock; age-18 catalog backstop | **Pass** |
| Week-52 charted zombies | 0 / 0 / 0 | **Pass** |
| Independent age-14 pooled | 0.762% | **Pass** |
| Boutique age-14 pooled | 0.235% | **Pass** |
| Indie-family entrant | 3/3 seeds | **Pass** |
| New Top-100 entries/week | 20.21-20.62 | **Pass** |
| Closed Top-40 median | 10-11 weeks | **Pass** |
| Quality/outcome Pearson | 0.485-0.568 | **Pass** |
| Seed-1001 reproducibility | Six byte-identical outputs | **Pass** |

The three blocking targets now hold together with every carried chart-health and reproducibility guard. The indie-major gap directive is appropriately sequenced to resume. Its tier decompounding, regional-breakout, and distribution-deal work should start from this calibration without changing the retirement floor or the retained demand scales implicitly.

## Artifacts

- Instrumented runner: `SimTools/ChartAuditRunner.cs`
- Analysis pass: `SimTools/analyze-market-volume-turnover.mjs`
- Baseline raw prefixes: `SimLogs/turnover-baseline-a-{1001,1002,1003}-*`
- Final raw prefixes: `SimLogs/market-turnover-hybrid-{1001,1002,1003}-*`
- Reproducibility replay: `SimLogs/market-turnover-hybrid-repeat-1001-*`

`SimLogs` remains ignored scratch output. This report and the instrumentation are the durable audit artifacts.
