# Entry Differentiation & Volatility Pass

Audit date: 2026-06-30  
Runtime: Godot 4.7 Mono, headless  
Comparison baseline: final combined Fixes #1-3 audit

## Method

The existing three-process, 52-chart-week harness was extended with launch-only telemetry. A launch-strength score was defined as:

`initial national awareness + (initial total regional stock / 100,000)`

The score is captured immediately after release promotion, before weekly sales or restocking. Coefficient of variation (CV) is used to compare spread without conflating it with each career bucket's different mean launch strength. The measured period used seeds 1001, 1002, and 1003; as in the prior audit, startup populations are independently randomized before the measured-period seed is applied.

## Pre-fix hypothesis check

The unmodified Fixes #1-3 model reproduced the reported volatility: median continuing-record movement was 6 in all three runs, and 19.74%, 20.03%, and 20.01% of continuing transitions exceeded 20 places.

Pooled launch-strength spread before the change:

| Career state | Launches | Mean score | CV |
|---|---:|---:|---:|
| New Signing | 9,726 | 0.336 | 0.345 |
| Rising | 913 | 0.606 | 0.219 |
| Established | 76 | 0.841 | 0.234 |
| Dropped | 932 | 0.299 | 0.266 |
| Declining | 56 | 0.439 | 0.160 |

The hypothesis is partly supported. Rising, Established, and Declining releases were notably tighter than the dominant New Signing bucket. The latter still mixes large label-tier effects; within fixed career/tier cells, pre-fix CV was as low as 0.104 for Major/New Signing and 0.118 for Major/Rising releases.

## Implementation

1. Each normal AI release now rolls a noisy perceived quality after generating the record. Noise narrows from +/-0.30 to +/-0.10 as label scouting ability rises.
2. The resulting 0.6x-1.4x multiplier scales the existing marketing budget and initial regional stock allocation. Career, tier, distribution, and random stock variation remain intact.
3. The alternate `PromoteRecordAI` path applies the same perceived-quality stock multiplier.
4. Never-charted records can be restocked during their first three release weeks when they sell at least 50% of their pre-sales regional allocation or generate more than 500 backorders. The existing charted-record restock rules are unchanged.
5. Perceived quality is stored only as launch telemetry and is not read by listener demand, awareness evolution, radio, word of mouth, chart points, or sales calculations.

An initial restock trial allowed any never-charted record with fast sell-through to restock indefinitely. It raised week-52 active stock to 1,821-1,849 and was rejected. The retained early-release gate produces 1,331-1,354 active records.

## Final volatility and regression check

| Metric | Prior final combined | Final run 1 | Final run 2 | Final run 3 |
|---|---:|---:|---:|---:|
| Median continuing movement | 6-7 | 6 | 7 | 6 |
| Continuing moves >20 | ~20-21% | 18.49% | 19.86% | 18.11% |
| Extreme #80 to #5 / #5 to #80 | 0 | 0 | 0 | 0 |
| Distinct #1 records | 17 / 23 / 19 | 20 | 23 | 24 |
| Median #1 tenure | 2 / 2 / 3 | 2 | 2 | 2 |
| Maximum #1 tenure | 6 / 5 / 5 | 6 | 4 | 5 |
| Closed Top-40 life, median | 15 / 14 / 14 | 13 | 13 | 13 |
| Observed #1 units, median | 1.72M / 1.64M / 1.54M | 1.80M | 1.40M | 1.58M |
| Peak #40-70 units, median | 42K / 50K / 39K | 31K | 31K | 36K |
| Annual market units | 120.4M / 131.3M / 118.3M | 144.5M | 133.1M | 146.6M |
| Active records, week 52 | 1,126 / 1,144 / 1,150 | 1,344 | 1,354 | 1,331 |

The >20-place tail improved by about 1.1 percentage points against the fresh pre-fix reproduction, but median movement did not improve. #1 turnover and chart life remain in their target ranges. Market volume remains plausible. The pool is still bounded, though its one-year endpoint is 16-20% higher than the prior combined runs and should be included in any later long-horizon validation.

### Launch-spread check

| Career state | Pre-fix CV | Post-fix CV | Delta |
|---|---:|---:|---:|
| New Signing | 0.345 | 0.352 | +0.007 |
| Rising | 0.219 | 0.216 | -0.003 |
| Established | 0.234 | 0.210 | -0.024 |
| Dropped | 0.266 | 0.294 | +0.028 |
| Declining | 0.160 | 0.196 | +0.037 |

The broad career-only score did not widen consistently. Fixed career/tier cells show the intended effect in several high-volume cohorts (Major/New Signing CV 0.104 to 0.121; Small/New Signing 0.342 to 0.369; Major/Rising 0.118 to 0.126), but other cells were flat or narrower. The mechanism is active, but existing tier and awareness variation plus independently randomized populations make the downstream launch-score widening modest rather than universal.

## Finding #4 diagnostic: indie/major gap

| Metric | Original baseline | Current run 1 | Current run 2 | Current run 3 |
|---|---:|---:|---:|---:|
| Indie Top-20 entry rate | 0 / 4,875 | 0 / 1,523 | 0 / 1,308 | 0 / 1,321 |
| Indie Top-100 success | 0.51-0.82% | 0.66% | 0.99% | 0.68% |
| Major Top-100 success | 58.6-73.4% | 75.86% | 82.10% | 79.75% |
| Indie share of #1 records | 0% | 0% | 0% | 0% |
| Major share of #1 records | not reported | 95.0% | 73.9% | 95.8% |

Mid-tier Top-20 entry rates were 1.15-1.18%, versus 18.84-21.09% for majors. No Independent, Boutique, or Small release reached #1. Indie Top-100 performance is broadly unchanged, while major success rose; the gap therefore widened. **Recommendation: Finding #4 is more urgent than at the original audit and remains the next structural priority after volatility.**

## Finding #5 diagnostic: quality determinism

| Metric | Original baseline | Current run 1 | Current run 2 | Current run 3 |
|---|---:|---:|---:|---:|
| Spearman: quality vs. `101 - peak` | 0.792 / 0.744 / 0.680 | 0.583 | 0.569 | 0.596 |
| #1 quality range | about 0.740-0.839 (reported bands) | 0.682-0.812 | 0.633-0.819 | 0.724-0.833 |

The fully current model is materially less quality-deterministic, and the #1 range now admits more lower-quality surprise winners. This combined diagnostic cannot causally isolate `DEMAND_AGE_DECAY_RATE`, but the direction after its introduction and the other retained lifecycle changes is clearly downward. **Recommendation: Finding #5 is less urgent than at the original audit.**

## Recommendation

The entry-differentiation pass modestly reduces large moves but does not restore the original 3-4-position median or 12-16% large-move range. Inertia-cap tuning now warrants its own isolated follow-up directive. Do not bundle it into this pass; retain these results as the new comparison point and include active-pool behavior in that trial.

## Artifacts

- Pre-fix analysis: `SimLogs/entry-pass-prefix-1_entry-pass-prefix-2_entry-pass-prefix-3-analysis.json`
- Final analysis: `SimLogs/entry-pass-final-b-1_entry-pass-final-b-2_entry-pass-final-b-3-analysis.json`
- Rejected broad-restock analysis: `SimLogs/entry-pass-final-1_entry-pass-final-2_entry-pass-final-3-analysis.json`
