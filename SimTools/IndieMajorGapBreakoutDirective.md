# Indie-Major Gap: Regional Breakout Directive

Directive date: 2026-06-30  
Runtime: Godot 4.7 Mono, headless  
Validation runs: 52 weeks, seeds 1001 / 1002 / 1003

## Objective

Open a credible path for Independent, Boutique, and Small-label records to become regional breakouts and occasionally cross onto the national chart without flattening the structural advantages of Major labels.

Treat distribution as the mechanism that **scales proven demand**, not the mechanism that creates demand. Do not implement a full player-facing distribution-deal system in this pass. Leave a clean simulation seam for that later mechanic and evaluate it only after indie-family records can generate observable breakout demand.

## Evidence and diagnosis

The corrected coverage pass established that distribution coverage is functioning operationally:

- Nearly every observed label had both covered and uncovered regions.
- Covered release-regions received roughly 3-4x the median restock of uncovered regions.
- Independent, Boutique, and Small releases still recorded 0% charting by week 14 in both coverage classes.
- MidTier week-14 charting remained under 1% overall in the three measured runs.
- Uncovered indie-family releases often triggered restocks more frequently because their starting inventories were easier to exhaust.

The binding constraint is therefore not merely stock availability. Existing tier effects suppress several demand inputs simultaneously, and those effects compound before a record can access the chart-driven awareness loop:

- Label tier lowers budget, scouting, production, marketing, distribution, and national reach together.
- Label push combines marketing and distribution and then multiplies the result by budget.
- Record quality is strongly amplified in purchase conversion.
- A record outside the chart receives a severe visibility/conversion disadvantage.
- Chart entry itself then raises awareness, radio heat, and future conversion.

This creates an entry moat: an indie record needs national demand to chart, but charting is one of the strongest existing sources of national demand.

## Design principles

1. **Majors buy breadth; indies earn depth.** Majors should launch broadly and consistently. Smaller labels should begin narrowly but be capable of intense local traction.
2. **Tier controls capacity more than intrinsic talent.** Capital, reach, roster size, consistency, and response speed may scale strongly with tier. Songwriting potential, artist talent, originality, and genre insight must retain meaningful cross-tier variance.
3. **Breakouts must be earned from simulation state.** Do not add a flat indie chart bonus, guaranteed chart slots, tier quotas, or arbitrary dice roll that bypasses sales and audience behavior.
4. **Scarcity is not demand.** Percentage sell-through alone must not qualify a record as a breakout when the underlying inventory and audience are tiny.
5. **Distribution raises the ceiling.** A record with no demand should gain little from wider coverage. A proven regional hit should become supply-constrained without better distribution.
6. **The target is possibility, not parity.** Major releases should remain much more likely to chart and should have a higher average peak.

## Required simulation changes

### 1. Decompound the tier stack

Audit every use of `LabelTier`, `budgetLevel`, `marketingPower`, `distributionStrength`, `productionQuality`, `scoutingAbility`, and `nationalReach` in release generation and weekly demand.

Refactor their responsibilities so the same tier disadvantage is not repeatedly charged through launch awareness, label push, radio, stock, conversion, and chart access.

Use these intended semantics:

| Attribute | Primary responsibility |
|---|---|
| Budget | Number, duration, and breadth of campaigns; financial endurance |
| Marketing | Efficiency of converting spend into awareness |
| Distribution | Geographic access, stock depth, restock speed, and capacity |
| Scouting | Accuracy of evaluating talent and likely audience response |
| Production | Recording polish and consistency, not the artist's creative ceiling |
| National reach | Ability to launch or propagate across regions simultaneously |

Do not normalize all labels to equal averages. Preserve higher Major floors and consistency while widening the lower-tier upper tail. A focused indie must occasionally outperform a mediocre Major on record quality, genre fit, or local audience response.

### 2. Add a regional breakout state

Build a continuous regional breakout signal from existing simulation evidence. Prefer a transparent score or state machine over a hidden tier-specific modifier.

The signal should consider:

- Absolute weekly sales and sales velocity
- Sustained growth across multiple weeks
- Backorders relative to meaningful demand
- Regional awareness and word of mouth
- Regional radio, jukebox, live-performance, or press activity where available
- Genre fit and local sentiment
- Quality, without allowing quality alone to guarantee success

Percentage sell-through may contribute, but it must be weighted by absolute volume. Selling 80 of 100 copies is not equivalent to selling 8,000 of 10,000.

Use a progression conceptually equivalent to:

`Local traction -> Regional breakout -> Neighboring-market test -> National crossover candidate`

The progression may be continuous internally. It does not require new UI in this pass.

### 3. Create a pre-chart discovery bridge

Allow strong regional evidence to build demand before national chart entry.

Regional breakout strength should be able to produce bounded gains in regional awareness, word of mouth, radio interest, and neighboring-region discovery. This bridge must soften the current uncharted-record moat without granting full chart visibility to every release.

Requirements:

- Effects begin locally and propagate outward only after sustained evidence.
- Propagation is faster for labels with better reach and distribution, but it is not exclusive to them.
- Effects decay when sales velocity and audience response collapse.
- The bridge has a cap below the exposure generated by an actual high chart position.
- Existing chart visibility remains valuable after entry.

### 4. Separate breakout detection from restock detection

Retain the restock system as a supply response, but stop using inventory exhaustion as the principal proxy for breakout success.

Restocking should answer: **Can the label replenish demonstrated demand?**  
Breakout scoring should answer: **Is the audience expanding enough to justify new exposure and markets?**

Validate that:

- Covered regions respond with greater stock depth and/or faster replenishment.
- Uncovered regions can reveal demand but hit a supply ceiling.
- Tiny low-volume releases do not gain large discovery effects merely by selling through tiny allocations.
- Backorders are reduced when stock arrives and do not accumulate indefinitely as reusable demand.

### 5. Preserve a future distribution-deal seam

Do not build contract negotiation, deal UI, royalty splits, or distributor AI in this pass.

Expose or retain enough state that a later distribution deal can react to:

- Number and strength of regional breakouts
- Sustained sales velocity
- Unmet demand/backorders
- Existing geographic coverage
- Label reputation and financial health

The eventual deal should offer wider coverage, higher capacity, and faster restocks in exchange for margin, fees, term, or control. It should amplify a crossover candidate rather than manufacture one.

## Likely code touchpoints

Inspect at minimum:

- `Systems/AILabelFactory.cs`
- `Systems/CompetitorManager.cs`
- `Systems/ChartManager.cs`
- `Systems/ChartSimulator.cs`
- `Data/AILabel.cs`
- `Data/RecordRuntimeData.cs`
- `Data/RegionalRecordData.cs`

Also verify that corrected distribution-region identifiers remain canonical and that a label's strong/home region receives the intended coverage semantics.

Do not assume all code paths promote releases identically. Check both normal competitor releases and any alternate `PromoteRecordAI` path.

## Implementation sequence

### Phase A: Instrument before changing behavior

Add or extend telemetry that decomposes, by release, week, tier, and region:

- Starting stock and coverage status
- Aware buyers
- Conversion rate before supply limits
- Raw demand
- Actual sales
- Backorders
- Restock trigger and amount
- Regional breakout inputs and resulting score/state
- Awareness, radio, and word-of-mouth gains attributable to breakout propagation
- Chart points and distance from the #100 cutoff

Run the three fixed seeds and preserve a pre-change baseline.

### Phase B: Decompound attributes

Separate tier-scaled responsibilities and widen meaningful cross-tier variance. Run the fixed seeds again before adding breakout propagation. Confirm which portion of the gap changes from attribute semantics alone.

### Phase C: Add local breakout and propagation

Implement the regional signal, pre-chart discovery bridge, decay, and neighboring-market tests. Tune using simulation evidence rather than fixed chart quotas.

### Phase D: Validate distribution as a scaler

Compare covered and uncovered regions for records with similar breakout strength. Coverage should materially affect fulfillment and crossover ceiling after demand appears, while having limited effect on records with no audience response.

Only after this phase should a separate directive decide whether to build the full distribution-deal mechanic.

## Acceptance targets

Use matured releases for week-14 rates and report each seed separately plus pooled counts. Treat these as directional calibration bands, not quotas to enforce in code.

| Tier | Desired week-14 national chart behavior |
|---|---|
| Major | Retains a decisive lead and broad-launch consistency |
| MidTier | Clearly nonzero, materially below Major, no longer effectively shut out |
| Independent | Approximately 0.5-3% charting, driven by strong regional evidence |
| Boutique | Rare but observable crossover across pooled runs |
| Small | Very rare but possible across pooled runs |

Additional acceptance criteria:

- At least two of three seeds produce one or more Independent/Boutique/Small week-14 chart entrants in aggregate.
- Indie-family Top-20 entries are possible but remain substantially rarer than Major Top-20 entries.
- Do not require an indie #1 within only three annual runs; report any that occur and verify their causal path.
- Major chart success remains several times higher than Independent success.
- Indie success is concentrated among records with strong breakout evidence, not distributed as a flat tier uplift.
- Covered and uncovered regions with comparable demand show a meaningful fulfillment difference.
- Existing chart turnover, chart-life, movement-volatility, active-record count, and annual-market-volume checks remain within the accepted ranges from prior audits or any regression is explicitly justified.
- Quality-versus-peak correlation must not return to the earlier highly deterministic regime.

If the targets require a large universal awareness bonus or direct chart-point subsidy, stop and report that the model still lacks a coherent demand pathway rather than forcing the rates.

## Required final report

Create a follow-up Markdown audit containing:

1. Exact behavioral changes and their rationale
2. Before/after funnel tables by tier
3. Regional breakout progression counts
4. Week-14 chart rates and peak distributions
5. Covered-versus-uncovered comparisons at matched breakout strength
6. Regression metrics for chart health and market volume
7. Examples of at least three successful and three failed indie breakout paths, with causal telemetry
8. A recommendation on whether the distribution-deal mechanic is now justified, still premature, or unnecessary

## Non-goals

- No player-facing distribution contracts or negotiation UI
- No guaranteed indie representation
- No chart quotas or tier-specific chart-point bonuses
- No blanket reduction of Major capabilities
- No restock-only solution
- No broad retuning of unrelated chart inertia or lifecycle systems unless a measured regression requires it

## Definition of done

This pass is complete when smaller labels possess a measurable, explainable, and rare route from local audience response to national chart entry; Major labels retain their structural advantage; distribution meaningfully scales proven demand; and all conclusions are supported by repeatable three-seed telemetry rather than anecdotal chart examples.
