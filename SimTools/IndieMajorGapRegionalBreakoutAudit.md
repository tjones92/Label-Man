# Indie-Major Gap: Regional Breakout Audit

Audit date: 2026-06-30  
Runtime: Godot 4.7 Mono, headless  
Measured runs: 52 weeks, seeds 1001 / 1002 / 1003  
Baseline runs: `prebreakout-{1001,1002,1003}`  
Final runs: `canonical-final-{1001,1002,1003}`

## Executive result

The pass establishes an evidence-driven route from local demand to national chart entry without chart quotas, guaranteed slots, tier-specific chart points, or a distribution contract system. Independent records now remain on the week-14 chart in all three final seeds, Boutique records do so in two seeds, and the pooled results include two Independent and one Boutique Top-20 records. Major week-14 success remains roughly 58 times the pooled Independent rate.

The directional calibration is **not complete**. Pooled Independent week-14 charting is 0.206%, below the requested 0.5-3% band, and Small remains 0%. Boutique is rare but observable at 0.299%. Annual market volume also rises from 211.6-221.7M to 268.2-295.9M units. Per the directive, no universal awareness bonus or direct chart subsidy was added to force the target rates.

The distribution-deal mechanic therefore remains **premature**. The simulation now exposes the right deal inputs and demonstrates that coverage scales proven demand, but crossover survival and market volume need another demand-lifecycle calibration pass first.

## 1. Behavioral changes and rationale

### Tier responsibilities

- Budget now controls campaign capacity and endurance; weekly label push no longer multiplies marketing, distribution, and budget together.
- Marketing controls campaign efficiency. Distribution is absent from demand creation and controls launch stock, geographic access, restock service level, and capacity.
- Scouting and production retain higher Major floors but have overlapping cross-tier ranges. This allows a strong smaller label to evaluate or polish a record without giving it Major-scale reach.
- Hook strength is driven primarily by artist songwriting and performance. Label production affects recording polish rather than the creative ceiling.
- National reach controls broad launch and propagation. Strong/home regions retain local depth independently of national breadth.
- Both normal competitor promotion and the alternate `PromoteRecordAI` path use the same campaign, regional reach, and initial-stock helpers.

### Regional breakout state

Each record-region now tracks a continuous score and a transparent progression:

`None -> LocalTraction -> NeighboringMarketTest -> RegionalBreakout -> NationalCrossoverCandidate`

The score combines absolute raw demand and fulfilled sales, velocity, sustained growth, awareness, radio/jukebox activity, genre fit, quality, and recent unmet demand. Absolute volume gates the composite, so tiny allocations cannot qualify on percentage sell-through alone. The state decays when both response and meaningful absolute demand collapse.

### Discovery bridge

- Local traction produces bounded local awareness, radio, jukebox, and word-of-mouth gains.
- Sustained regional breakouts test adjacent markets through a canonical six-region adjacency map.
- Better reach and distribution accelerate propagation but are not prerequisites.
- Multi-market candidates can build national awareness up to 0.60.
- Strong regional evidence can lift an uncharted market's visibility/conversion multiplier from 0.40 to at most 0.95. This remains below the 1.00 exposure of chart position #100.
- The bridge adds no chart points. All national entry still comes through sales and existing airplay points.

### Supply response and future seam

- Restock detection now reads demand and breakout evidence rather than percentage sell-through.
- Backorders decay to 35% before new weekly unmet demand is added, preventing indefinite reuse of stale demand.
- Covered markets receive higher service levels and capacity; uncovered markets can reveal demand but hit a lower ceiling.
- `RecordRuntimeData` exposes breakout count, neighboring tests, crossover strength, peak regional strength, sustained velocity, unmet demand, and covered-region count for a future distributor evaluation.
- British-import labels now enter through canonical `eastcoast` coverage rather than the nonexistent live `UK` region. Every home/strong region is consequently a valid coverage identifier.

## 2. Before/after launch funnel by tier

Values are the mean of the three run-level medians for release ages 1-3. Conversion is measured before supply limits.

| Tier | Phase | Start stock | Aware buyers | Conversion | Raw demand | Actual sales | Restock trigger | Median positive restock |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| Major | Before | 11,882 | 89,338 | 1.30% | 1,015 | 992 | 11.2% | 10,434 |
| Major | Final | 9,932 | 100,400 | 2.77% | 2,731 | 2,076 | 11.7% | 10,200 |
| MidTier | Before | 3,386 | 71,314 | 0.40% | 323 | 317 | 15.0% | 1,433 |
| MidTier | Final | 6,581 | 65,191 | 0.74% | 518 | 422 | 4.6% | 4,512 |
| Independent | Before | 622 | 58,130 | 0.27% | 169 | 165 | 34.0% | 311 |
| Independent | Final | 1,431 | 45,828 | 0.54% | 260 | 207 | 2.2% | 2,750 |
| Boutique | Before | 272 | 53,207 | 0.22% | 124 | 108 | 48.0% | 141 |
| Boutique | Final | 1,239 | 43,477 | 0.53% | 241 | 192 | 2.4% | 2,341 |
| Small | Before | 119 | 44,877 | 0.18% | 79 | 59 | 58.1% | 58 |
| Small | Final | 860 | 35,326 | 0.40% | 142 | 116 | 0.9% | 2,158 |

The old trigger inversion is gone: Smaller releases no longer restock most often merely because their tiny allocations sell through. Positive restocks are rarer but meaningful, and the final discovery bridge raises conversion only after evidence accumulates.

Phase B, before propagation, produced pooled week-14 rates of 0.055% Independent, 0.084% Boutique, and 0% Small. Attribute decompounding alone therefore did not create the final Independent path.

## 3. Regional breakout progression

Counts are pooled matured releases that reached each cumulative stage during the three final runs.

| Tier | Local traction | Neighbor test | Regional breakout | National candidate |
|---|---:|---:|---:|---:|
| Major | 702 | 679 | 679 | 601 |
| MidTier | 1,704 | 1,234 | 1,234 | 994 |
| Independent | 648 | 343 | 343 | 287 |
| Boutique | 220 | 114 | 114 | 87 |
| Small | 90 | 34 | 34 | 25 |

Qualification is selective but not rare enough by itself to guarantee national survival: 287 Independents become candidates, while only eight remain charted at week 14. The next constraint is crossover durability against the national cutoff, not an absence of observable local demand.

## 4. Week-14 national outcomes and peak distribution

| Seed | Major | MidTier | Independent | Boutique | Small |
|---|---:|---:|---:|---:|---:|
| 1001 | 25/341 (7.33%) | 21/1,540 (1.36%) | 2/1,246 (0.16%) | 2/438 (0.46%) | 0/446 |
| 1002 | 42/244 (17.21%) | 16/1,607 (1.00%) | 3/1,251 (0.24%) | 0/467 | 0/409 |
| 1003 | 46/362 (12.71%) | 12/1,232 (0.97%) | 3/1,378 (0.22%) | 2/435 (0.46%) | 0/543 |
| **Pooled** | **113/947 (11.93%)** | **49/4,379 (1.119%)** | **8/3,875 (0.206%)** | **4/1,340 (0.299%)** | **0/1,398** |

Median career peak among records still charted at week 14 was Major #2/#2/#2, MidTier #2/#10.5/#4, Independent #1/#14/#28, and Boutique #10/-/#28.5 by seed. Week-14 Top-20 counts were 55 Major, 21 MidTier, two Independent, one Boutique, and zero Small. Indie-family Top-20 success is therefore possible and substantially rarer than Major success.

Acceptance read:

- Pass: all three seeds have at least one indie-family week-14 entrant.
- Pass: Major success remains decisive and several times higher than Independent success.
- Pass: Independent and Boutique Top-20 entry occurs without quota or subsidy.
- Pass: Boutique crossover is rare but observable in two seeds.
- Miss: Independent pooled rate remains below 0.5%.
- Miss: Small does not survive on-chart to week 14 in the final runs.

## 5. Covered versus uncovered at matched breakout strength

Values pool the three final runs. Medians are averaged across run-level medians; fulfillment is actual sales divided by raw demand.

| Score band | Coverage | Observations | Median raw demand | Median sales | Fulfillment | Median restock | Median backorders |
|---|---|---:|---:|---:|---:|---:|---:|
| 0.20-0.30 | Covered | 33,691 | 1,383 | 1,350 | 95.0% | 0 | 0 |
| 0.20-0.30 | Uncovered | 10,673 | 1,577 | 538 | 31.9% | 499 | 1,377 |
| 0.30-0.40 | Covered | 14,789 | 1,954 | 1,872 | 93.2% | 0 | 0 |
| 0.30-0.40 | Uncovered | 5,566 | 2,729 | 940 | 28.9% | 903 | 2,292 |
| 0.40-0.50 | Covered | 26,464 | 4,030 | 3,484 | 85.2% | 3,002 | 0 |
| 0.40-0.50 | Uncovered | 4,828 | 4,319 | 1,615 | 33.8% | 1,460 | 3,517 |
| 0.50+ | Covered | 24,027 | 7,332 | 6,766 | 95.0% | 5,630 | 0 |
| 0.50+ | Uncovered | 4,171 | 9,743 | 3,769 | 37.5% | 3,446 | 7,487 |

Coverage now behaves as a scaler. At comparable score, covered markets fulfill roughly 85-95% of demand versus 29-38% uncovered. Uncovered high-score markets often show greater raw demand but convert less than half as much of it, leaving visible distributor-facing unmet demand.

## 6. Chart-health and market-volume regressions

| Metric | Immediate baseline range | Final range | Read |
|---|---:|---:|---|
| Annual market units | 211.6-221.7M | 268.2-295.9M | Regression: +21-40% |
| Active records, week 52 | 1,495-1,604 | 1,765-1,903 | Higher equilibrium; long-horizon check needed |
| New Top-100 entries/week | 18.58-19.21 | 17.90-19.35 | Stable |
| Closed Top-40 life, median | 11-12 weeks | 11-12 weeks | Stable |
| Continuing movement, median | 8 | 9 | Slightly higher |
| Distinct #1 records | 17-23 | 19-21 | Stable |
| Quality/outcome Pearson | 0.583-0.607 | 0.492-0.561 | Less deterministic; no return to the old regime |

Turnover, chart life, #1 turnover, and extreme movement remain broadly healthy. Market volume and active-record count do not remain in the immediate baseline range. Most of the increase appears in Phase B (268.7-298.3M before breakout propagation), tying it primarily to attribute/launch decompounding rather than the regional bridge. This regression is not hidden or justified as harmless; it needs a separate economy/lifecycle validation before the pass can be called fully calibrated.

## 7. Causal paths

### Successful indie paths

1. **“Her Frug” — Boutique, seed 1001.** Quality 0.876. Its covered East Coast market reached a 0.713 candidate score at age 8 on 125,638 raw demand, 91,269 fulfilled sales, +0.49 velocity, and a 127,674 restock against 37,698 backorders. It peaked at #1 and was #6 at age 14 with 122,947 weekly units.
2. **“My Coy Vow” — Independent, seed 1002.** Quality 0.793. A covered East Coast breakout reached 0.596 at age 7 on 25,279 raw demand and 25,231 fulfilled sales. It peaked at #14 and remained #73 at age 14 with 10,146 units.
3. **“The Tears Hears” — Independent, seed 1003.** Quality 0.765. Its covered East Coast market reached 0.610 at age 9 on 8,289 raw demand and 8,136 sales. It peaked at #25 and remained #63 at age 14 with 8,407 units, only 215 raw points below the uninertial cutoff.

These successes share strong absolute demand, multi-week expansion, and candidate-level scores. Coverage changes fulfillment and peak ceiling, not qualification.

### Failed indie-family paths

1. **“That Time” — Boutique, seed 1001.** Quality 0.735. It reached a 0.636 covered East Coast candidate score on 6,744 raw demand and 6,967 sales, but never charted; at age 14 it sold 2,717 and sat 7,660 points below the cutoff.
2. **“Lonely Day” — Small, seed 1001.** Quality 0.758. It reached a 0.635 covered East Coast regional-breakout score on 7,332 raw demand and 5,051 sales. By age 14 it sold 2,666 and remained 9,345 points short.
3. **“Do You Phone” — Independent, seed 1001.** Quality 0.753. It reached a 0.635 covered East Coast candidate score on 8,014 raw demand and 7,815 sales, peaked at #96, then fell out; age-14 sales were 3,496 and points were 6,644 below the cutoff.

## 8. Distribution-deal recommendation

**Still premature.** The seam is now justified technically: a future distributor can evaluate breakout count and strength, sustained velocity, recent unmet demand, current coverage, label reputation, and financial health. The matched-score comparison also proves that wider coverage and capacity would materially improve fulfillment for an already proven record.

The player-facing mechanic should wait because the current model still underproduces durable Independent crossovers, produces no week-14 Small survivors in the fixed runs, and raises annual market volume substantially. A deal system added now would amplify an incompletely calibrated lifecycle and make it harder to distinguish demand-path corrections from supply expansion.

Recommended next pass:

1. Audit why candidate-level Boutique/Small records peak nationally but decay below the cutoff by age 14.
2. Reconcile Phase B's market-volume and active-pool increase without restoring compounded tier penalties.
3. Repeat the same three seeds plus a longer pool-stability run.
4. Build distribution deals only if crossover durability reaches the directional bands while the market regression is controlled.

## Artifacts

- Instrumented runner: `SimTools/ChartAuditRunner.cs`
- Regional analyzer: `SimTools/analyze-regional-breakout.mjs`
- Aggregate final analysis: `SimLogs/canonical-final-1001_canonical-final-1002_canonical-final-1003-regional-breakout-analysis.json`
- Aggregate chart-health analysis: `SimLogs/canonical-final-1001_canonical-final-1002_canonical-final-1003-analysis.json`

`SimLogs` is scratch/ignored output; the analyzer and this audit are the reproducible checked-in artifacts.
