# Indie Breakout-Restock Funnel Diagnostic

Audit date: 2026-06-30  
Runtime: Godot 4.7 Mono, headless  
Runs: 52 weeks, seeds 1001 / 1002 / 1003

## Results

All observed regions were classified as **uncovered**. The label factory stores coverage names such as `Northeast`, `WestCoast`, and `DeepSouth`, while live chart regions use IDs such as `eastcoast`, `westcoast`, and `deepsouth`. The requested covered/uncovered comparison therefore degenerates to uncovered-only data; this is existing behavior and was not changed.

Rates and medians below are three-run ranges. Trigger and sub-threshold rates use releases as the denominator. Restock size is per triggered regional event. Week-14 outcomes exclude releases that had not matured to week 14 by the end of the run.

| Tier | Releases/run | Ever trigger, uncovered | Median restock | Restock / week-start stock | Non-triggered with 1-500 backorders | Triggered: charted by week 14 | Not triggered: charted by week 14 | Triggered median peak |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Major | 501-574 | 62.1-71.3% | 3,963-4,578 | 113-120% | 0% | 20.1-27.0% | 2.6-5.6% | 18-24.5 |
| MidTier | 1,687-1,758 | 76.3-77.5% | 911-964 | 79-81% | 0% | 2.6-2.9% | 0.9-1.7% | 34-41.5 |
| Independent | 1,415-1,594 | 84.7-85.9% | 195-222 | 67-68% | 0% | 0% | 0% | - |
| Boutique | 346-504 | 83.2-86.9% | 93-100 | 66-68% | 0% | 0% | 0% | - |
| Small | 427-527 | 97.1-97.3% | 41-44 | 63% | 0% | 0% | 0% | - |

The non-triggered/sub-threshold rate is zero because every release that produced 1-500 backorders also satisfied the separate 50% sell-through arm. Across all releases, including those that did trigger through sell-through, 41.1-45.2% of Independent, 53.5-57.9% of Boutique, and 72.5-75.7% of Small releases had backorders that never exceeded 500.

No restock was capped by regional maximum capacity in any tier or run. For Independent releases, the median restock was 252-257% of inventory remaining immediately before restock and 63-68% of that week's raw demand. Thus the tier-scaled batch is small in absolute units, but it materially replenishes the inventory actually on hand.

### Demand inputs, week 1 regional medians

| Tier | Aware buyers | Conversion rate | Raw sales demand | Week-start stock |
|---|---:|---:|---:|---:|
| Major | 66,926-69,441 | 1.292-1.443% | 857-969 | 3,703-4,098 |
| MidTier | 55,506-56,075 | 0.679-0.693% | 348-369 | 1,300-1,330 |
| Independent | 44,458-45,319 | 0.345-0.376% | 144-158 | 398-428 |
| Boutique | 42,002-42,807 | 0.250-0.275% | 99-106 | 213-222 |
| Small | 36,020-36,494 | 0.203-0.215% | 69-71 | 117-120 |

## Read

Funnel stage **3** is the dominant suppressor, with a strong upstream-demand component: indie-family releases do generate demand, trigger the mechanic more often than majors, and receive restocks large enough to move their available inventory, but neither triggered nor non-triggered Independent/Boutique/Small releases charted by week 14. Restocking is predictive for majors (20.1-27.0% charted when triggered versus 2.6-5.6% otherwise), but has no observable payoff below MidTier. The week-1 median Independent release starts with only about two-thirds of major aware buyers, one-quarter of major conversion, and roughly one-sixth of major raw demand. Enlarging restock batches alone is therefore not supported as the next fix. The next directive should first resolve or deliberately replace the broken distribution-region coverage mapping, then isolate awareness/conversion/chart-point thresholds; a distribution-deal mechanic should wait until those existing inputs have coherent tier-targeted semantics.

## Additional flagged issue (not changed)

`ChartSimulator.CalculateRegionalSales` names a capacity flag `isIndie`, but computes it from `record.baseRecord.labelId != null` plus regional infrastructure. Nearly every AI-label release, including majors, has a non-null label ID, so `INDIE_DISTRIBUTION_PENALTY` is not label-tier-targeted as named. This diagnostic does not alter that behavior.

## Artifacts

- Instrumented harness output: `SimLogs/breakout-funnel-{1,2,3}-breakout-funnel.csv`
- Aggregate analysis: `SimLogs/breakout-funnel-1_breakout-funnel-2_breakout-funnel-3-breakout-analysis.json`
- Analyzer: `SimTools/analyze-breakout-funnel.mjs`
