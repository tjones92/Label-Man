# Market Volume Regression Audit

Audit date: 2026-07-01  
Runtime: Godot 4.7 Mono, headless  
Measured runs: 52 weeks, seeds 1001 / 1002 / 1003  
Baseline: `canonical-final-{1001,1002,1003}`  
Final named runs: `volume-fix-{1001,1002,1003}`

## Executive result

This pass improves the stock model but does **not** satisfy the definition of done. Home and strong regions retain launch depth, while other covered regions now use a smaller `0.10` depth floor. `BASE_PURCHASE_RATE` is reduced from `0.10f` to `0.07f` after measured `0.09f` and `0.08f` steps and a diagnostic below the original `0.065f` guardrail.

The final named runs produce 205.3-212.0M annual units, below the 268.2-295.9M canonical range but above the 140-175M target. Pooled Independent week-14 charting is 5/3,835 (0.130%), below the 0.15% floor, although indie-family entrants remain observable in two of three seeds. Boutique is 1/1,272 (0.079%), below its 0.10% floor. Active records remain 1,829-1,902, showing that the pool-size regression is not corrected by initial stock or purchase rate.

The audit also uncovered a reproducibility limitation: `ChartAuditRunner` applies the requested seed after autoload initialization. Startup populations are therefore independently random. An earlier triplet at the identical final code settings produced 192.2-201.4M units and 0.160% pooled Independent charting. The named final triplet is reported without cherry-picking, but deterministic startup seeding is required before narrow calibration bands can be enforced reliably.

## 1. Code changes and rationale

### Label home region

- Added exported `AILabel.homeRegion`.
- `AILabelFactory.CreateFromTemplate` assigns `CityToRegion(template.city)`.
- `AILabelFactory.GenerateProceduralLabel` assigns `CityToRegion(city)`.

This makes home-market depth explicit instead of relying on a label's possibly broader `strongRegions` set.

### Region-aware initial stock

`ChartSimulator.CalculateInitialRegionalStock` now distinguishes home/strong markets from other markets:

- Home or strong: `0.25 + distributionStrength * 0.75`, `100`-unit floor.
- Other covered/uncovered: `0.10 + distributionStrength * 0.75`, no unit floor.
- Existing coverage access (`1.0` covered, `0.18` uncovered), strong-region multiplier, career scale, perceived-quality multiplier, and launch noise remain unchanged.

The first implementation used no distant depth floor, exactly as directed. When the purchase-rate calibration drove pooled Independent week-14 charting to 0.081%, the directive's overcorrection clause was applied and the distant depth floor was restored at `0.10`, not `0.25`.

No regional-sales modifiers, breakout scoring/decay/progression, discovery caps, restock logic, backorder decay, chart inertia/life/turnover, or competitor scheduling were changed.

### Backup lever

`BASE_PURCHASE_RATE` was changed from `0.10f` to **`0.07f`**. The calibration sequence was:

| Configuration | Annual units, three-run range | Pooled Independent week-14 | Read |
|---|---:|---:|---|
| Stock split, rate 0.10 | 265.1-280.3M | 0.167% | Volume still high; indie survives |
| No distant floor, rate 0.09 | 229.3-260.5M | Not promoted to full analysis | Above 200M |
| No distant floor, rate 0.08 | 230.1-238.5M | Not promoted to full analysis | Above 200M |
| No distant floor, rate 0.07 | 197.6-202.0M | 0.081% | Indie overcorrection |
| Distant floor 0.10, rate 0.07 diagnostic | 192.2-201.4M | 0.160% | Last configuration to preserve Independent floor in that triplet |
| Distant floor 0.10, rate 0.06 | 167.4-193.3M | 0.104% | Volume mixed; indie fails |
| Distant floor 0.10, rate 0.05 | 151.6-157.0M | 0.084% | Volume passes; indie and Boutique fail |
| **Final named rerun: floor 0.10, rate 0.07** | **205.3-212.0M** | **0.130%** | **Best guardrail compromise; acceptance still fails** |

The `0.05f` result proves that purchase rate can force volume into range, but it does so by removing the indie path the directive requires preserving. It was therefore rejected.

## 2. Before/after launch funnel by tier

Values are means of the three run-level medians for release ages 1-3. Conversion is measured before supply limits. "Before" is the canonical-final result; "After" is the final named volume-fix result.

| Tier | Phase | Start stock | Aware buyers | Conversion | Raw demand | Actual sales | Restock trigger | Median positive restock |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| Major | Before | 9,932 | 100,400 | 2.77% | 2,731 | 2,076 | 11.7% | 10,200 |
| Major | After | 8,762 | 101,717 | 1.90% | 1,960 | 1,584 | 9.0% | 8,790 |
| MidTier | Before | 6,581 | 65,191 | 0.74% | 518 | 422 | 4.6% | 4,512 |
| MidTier | After | 5,181 | 63,143 | 0.52% | 360 | 294 | 2.6% | 3,504 |
| Independent | Before | 1,431 | 45,828 | 0.54% | 260 | 207 | 2.2% | 2,750 |
| Independent | After | 1,182 | 45,899 | 0.39% | 185 | 150 | 1.0% | 1,568 |
| Boutique | Before | 1,239 | 43,477 | 0.53% | 241 | 192 | 2.4% | 2,341 |
| Boutique | After | 875 | 43,895 | 0.37% | 165 | 130 | 1.0% | 1,616 |
| Small | Before | 860 | 35,326 | 0.40% | 142 | 116 | 0.9% | 2,158 |
| Small | After | 594 | 36,073 | 0.30% | 110 | 85 | 0.3% | 1,430 |

Stock depth falls most for the indie-family tiers, as intended. Aware-buyer medians remain broadly stable; the lower purchase rate reduces raw demand and fulfilled sales across all tiers.

## 3. Week-14 national outcomes

| Seed | Major | MidTier | Independent | Boutique | Small |
|---|---:|---:|---:|---:|---:|
| 1001 | 41/274 (14.96%) | 17/1,514 (1.12%) | 2/1,269 (0.158%) | 0/428 | 0/398 |
| 1002 | 31/407 (7.62%) | 17/1,646 (1.03%) | 0/1,182 | 0/423 | 0/416 |
| 1003 | 32/482 (6.64%) | 11/1,295 (0.85%) | 3/1,384 (0.217%) | 1/421 (0.238%) | 0/487 |
| **Pooled** | **104/1,163 (8.94%)** | **45/4,455 (1.010%)** | **5/3,835 (0.130%)** | **1/1,272 (0.079%)** | **0/1,301** |

Major remains decisively highest. Indie-family week-14 charting appears in seeds 1001 and 1003, meeting the two-seed observability condition, but the pooled Independent and Boutique rate floors both miss.

## 4. Chart health and market-volume regression

| Metric | Canonical-final range | Final volume-fix range | Target | Read |
|---|---:|---:|---:|---|
| Annual market units | 268.2-295.9M | 205.3-212.0M | 140-175M | Fail; improved 21-31%, still high |
| Active records, week 52 | 1,765-1,903 | 1,829-1,902 | 1,450-1,700 | Fail; effectively unchanged |
| New Top-100 entries/week | 17.90-19.35 | 18.73-19.58 | 16-21 | Pass |
| Closed Top-40 life, median | 11-12 weeks | 10-12 weeks | 10-13 | Pass |
| Distinct #1 records | 19-21 | 15-22 | 16-25 | Mixed; seed 1001 is one below |
| Quality/outcome Pearson | 0.492-0.561 | 0.376-0.452 | 0.45-0.62 | Fail in two seeds |

The turnover and chart-life mechanics remain stable. Quality/outcome correlation is not stable in the final triplet, and active-record equilibrium does not respond materially to the permitted levers.

## 5. Acceptance read

| Acceptance condition | Result | Status |
|---|---:|---|
| Annual units 140-175M in all seeds | 205.3-212.0M | **Fail** |
| Active records 1,450-1,700 | 1,829-1,902 | **Fail** |
| Pooled Independent week-14 >= 0.15% | 0.130% | **Fail** |
| Pooled Boutique week-14 >= 0.10% | 0.079% | **Fail** |
| Indie-family entrant in at least two seeds | Seeds 1001 and 1003 | **Pass** |
| Major highest by a wide margin | 8.94% vs 1.01% MidTier and 0.13% Independent | **Pass** |
| New entries/week 16-21 | 18.73-19.58 | **Pass** |
| Closed Top-40 median life 10-13 | 10-12 | **Pass** |
| Distinct #1 records 16-25 | 15-22 | **Mixed** |
| Quality/outcome Pearson 0.45-0.62 | 0.376-0.452 | **Fail** |

**Overall: fail.** The code change is directionally useful, but the simultaneous volume, indie-charting, active-pool, and chart-health targets cannot be reached with initial stock and a universal purchase-rate scalar alone.

## 6. Recommendation

Distribution deals remain **premature**. The stock model is closer to supporting them conceptually--home depth, distant depth, and coverage now have clearer meanings--but a deal would amplify an unresolved lifecycle equilibrium.

The next pass should first:

1. Move audit seeding ahead of autoload population generation so the three seeds are reproducible end to end.
2. Diagnose why active records remain near 1,800-1,900 after large demand reductions; initial allocation and purchase rate are not the controlling lifecycle levers.
3. Separate market-size calibration from conversion enough to reduce aggregate volume without erasing rare indie crossovers. This should be a lifecycle/demand-capacity diagnostic, not another tier penalty or chart subsidy.
4. Repeat the same three seeds only after deterministic initialization, then add a longer pool-stability run.

Do not implement distribution deals until annual volume, active-pool equilibrium, and indie survival pass together.

## Artifacts

- Final regional analysis: `SimLogs/volume-fix-1001_volume-fix-1002_volume-fix-1003-regional-breakout-analysis.json`
- Final chart analysis: `SimLogs/volume-fix-1001_volume-fix-1002_volume-fix-1003-analysis.json`
- Final raw prefixes: `SimLogs/volume-fix-{1001,1002,1003}-*`
- Diagnostic raw prefixes: `volume-fix-rate09-*`, `volume-fix-rate08-*`, `volume-fix-floor10-rate07-*`, and `volume-fix-rate05-*`

`SimLogs` is ignored scratch output. The checked-in audit is the durable record of this pass.
