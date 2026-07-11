# Directive 4C - Market Seasonality Overlay

## 1. Objective

Replace the two hard-coded sales-season switches with one toggle-gated market-seasonality service. The enabled mechanic should create a recognizable annual rhythm in Singles, Albums, radio opportunity, recording costs, marketing efficiency, and artist availability while preserving approximately 1:1 long-run economic balance with the frozen Directive 4b Baseline v2.

This is a shape pass, not a market-volume recalibration. Baseline v2 is the sole active comparison. Do not use the retired v1 figures as targets.

The draft curves are accepted as moderate design priors, with these corrections:

1. Their arithmetic row sums are valid, but arithmetic mean-one is not the simulation's conservation measure. Sales execute weekly on Fridays, and month lengths produce different numbers of live ticks.
2. Baseline v2 is not mean-one: its 1960-69 Friday-weighted sales multipliers average approximately `1.029598` for Singles and `1.023008` for Albums. Replacing them with a literal mean-one curve would lower the inherited market before any nonlinear effects.
3. The February values are per-week seasonal intensities. Do not apply a second short-month discount. February already has fewer live weekly ticks.
4. Radio and marketing must not be multiplied directly into record sales. They already affect awareness, airplay, chart position, and later demand, so a second direct sales multiplier would double-charge them.
5. Venue infrastructure currently has no touring, attendance, or live-revenue consumer. Expose and audit the venue curve, but do not invent revenue or attach it to record sales in 4C.
6. The predicted December Album-gross crossover is a report-only emergence watch, not a calibration target.

As a numerical cross-check, naively multiplying the draft sales, radio, and marketing rows would produce arithmetic annual means of approximately `1.006984` for Singles and `1.009220` for Albums despite each source row being individually normalized. The more important drift would come from persistent awareness/radio feedback and release-weighted marketing, so 4C uses single-application seams plus paired simulation rather than trying to normalize a direct three-factor product.

## 2. Frozen Baseline and Toggle Contract

Add one shipping toggle, `marketSeasonalityEnabled`. Keep the scene default `false` during development and switch it to `true` only after acceptance. Add command-line overrides `--enable-market-seasonality` and `--disable-market-seasonality` for paired audit runs; reject use of both flags together. Resolve the override before `ChartManager` prewarming begins.

Disabled mode must execute the current v2 behavior exactly:

```text
Legacy Single sales by month:
Jan .90; Feb-May 1.00; Jun-Aug 1.05; Sep-Oct 1.00; Nov 1.10; Dec 1.20

Legacy Album sales by month:
Jan .90; Feb-Oct 1.00; Nov 1.12; Dec 1.25
```

With the new toggle disabled, do not change RNG calls, call order, initialization, prewarming, sales, release decisions, costs, marketing, scouting, radio, CSV values, or float-expression grouping. The frozen seed-1001 520-week Baseline v2 hashes in `BASELINE-V2.md` must remain byte-exact.

Prewarming is synthetic and repeatedly uses the January game date. It must remain identical between the enabled and disabled paired runs. Bypass the new overlay during `SimulateWeek(triggerEvents: false)` and preserve the legacy v2 prewarm behavior. The enabled treatment begins on the first live weekly tick.

The enabled implementation must consume zero new random draws. Changed outcomes may naturally cause later RNG divergence; the seasonality service itself must be pure and deterministic.

## 3. Canonical Shape Table

Store one immutable 12-entry table for each channel, indexed January through December. Reject missing rows, non-finite values, invalid months, and incorrect table lengths. Unit tests must verify the displayed arithmetic sums (`12.00`, or `0.00` for radio) within float tolerance, even though runtime conservation uses the calendar-weighted formulas in section 4.

| Month | Single sales | Album sales | Radio delta | Venue attendance | Recording cost | Marketing efficiency | Artist availability |
|---|---:|---:|---:|---:|---:|---:|---:|
| January | 0.84 | 0.75 | -0.04 | 0.85 | 0.88 | 0.82 | 1.16 |
| February | 0.89 | 0.83 | -0.03 | 0.89 | 0.92 | 0.90 | 1.11 |
| March | 0.95 | 0.91 | -0.02 | 0.95 | 0.97 | 0.95 | 1.06 |
| April | 0.98 | 0.97 | 0.00 | 1.00 | 1.00 | 0.99 | 1.02 |
| May | 1.03 | 1.00 | +0.02 | 1.03 | 1.02 | 1.02 | 0.99 |
| June | 1.07 | 1.01 | +0.05 | 1.10 | 1.03 | 1.04 | 0.95 |
| July | 1.10 | 1.00 | +0.05 | 1.13 | 1.04 | 1.03 | 0.92 |
| August | 1.05 | 0.97 | +0.03 | 1.08 | 1.02 | 1.00 | 0.94 |
| September | 0.99 | 0.99 | +0.01 | 1.00 | 1.03 | 0.98 | 1.00 |
| October | 1.02 | 1.06 | +0.02 | 1.03 | 1.06 | 1.08 | 1.00 |
| November | 1.04 | 1.16 | -0.03 | 0.98 | 1.08 | 1.13 | 0.95 |
| December | 1.04 | 1.35 | -0.06 | 0.96 | 0.95 | 1.06 | 0.90 |

Interpretation:

- Singles retain roughly the same peak-to-trough amplitude as v2 but move their crest from Christmas to summer.
- Albums receive the strongest holiday shape. The `1.35 / 0.75 = 1.80` raw December-to-January ratio is deliberately more pronounced than v2's `1.25 / 0.90 = 1.389`, but remains a starting shape rather than a guaranteed observed sales ratio.
- The radio delta is for ordinary, non-holiday repertoire. Because the simulation has no Christmas-content tag, apply the small generic opportunity curve uniformly and record this limitation.
- Recording cost represents studio-market price/capacity pressure at the time production is commissioned. November-scheduled projects may therefore make December Album drops more expensive even though December's own raw cost is `0.95`.
- Marketing efficiency changes the awareness return from a fixed spend; it does not change the dollars charged.
- Artist availability represents the ease of securing artists for scouting and release scheduling, not artist quality.
- Venue attendance is a reserved, public seasonality channel until a real live-performance consumer exists.

The broad holiday/summer directions are historically flavorful. The precise monthly values are game-design priors, not a claim of a measured 1960s national record-industry series. The U.S. Census Bureau's [seasonal-adjustment guidance](https://www.census.gov/data/software/x13as.References.html) confirms that Christmas, month length, and trading-day effects require explicit treatment, and its [December 1962 retail release](https://www2.census.gov/marts/adv6212.pdf) shows the period distinction between adjusted and unadjusted holiday sales. Billboard's [December 6, 1969 issue](https://www.worldradiohistory.com/Archive-All-Music/Billboard/60s/1969/Billboard%201969-12-06.pdf) separately charted Christmas LPs and Singles. Do not overstate the table's empirical precision in the audit.

## 4. Calendar-Weighted Conservation

Create a pure service, preferably `Systems/MarketSeasonality.cs`, as the sole owner of the tables, legacy sales curves, normalization, and public getters. Do not leave separate month switches in `ChartSimulator` and `AlbumSimulator`.

For each calendar year `y`, calculate `W(y,m)`, the number of live Friday sales ticks in month `m`. Derive this from the same `GameDate`/`DateTime` calendar convention used by `TimeManager`; do not hard-code the 1960-69 counts.

For audit comparison, the full 1960-69 calendar contains `522` Fridays, distributed January through December as `44, 40, 45, 43, 44, 43, 44, 44, 44, 43, 43, 45`. The implementation must calculate, not embed, those values.

For the two sales channels, preserve the inherited v2 annual multiplier budget separately by format:

```text
salesScale(format, y) =
    sum_m(W(y,m) * legacySales(format,m))
    / sum_m(W(y,m) * rawNewSales(format,m))

enabledSales(format,y,m) = rawNewSales(format,m) * salesScale(format,y)
```

This guarantees that, before feedback, stock limits, retirement, releases, and rounding, each enabled year's Single and Album sales multipliers have exactly the same Friday-weighted total as that format's legacy v2 curve.

For multiplicative non-sales channels, normalize to a Friday-weighted annual mean of one:

```text
meanOneScale(channel,y) = sum_m W(y,m) / sum_m(W(y,m) * raw(channel,m))
enabledMultiplier(channel,y,m) = raw(channel,m) * meanOneScale(channel,y)
```

For radio, preserve a Friday-weighted annual additive mean of zero:

```text
radioOffset(y) = sum_m(W(y,m) * rawRadioDelta(m)) / sum_m W(y,m)
radioOpportunity(y,m) = 1 + rawRadioDelta(m) - radioOffset(y)
```

Clamp only at final domain boundaries, not inside normalization. Validate that all final multiplicative channels are positive. Cache year/channel normalizers if useful, but keep the result deterministic.

Do not normalize against the number of releases, campaigns, scouting rolls, or projects that happened in a run. That would make the mechanic endogenous and seed-dependent. Calendar opportunity is conserved ex ante; paired simulation gates adjudicate realized nonlinear drift.

## 5. Required Integration

### 5.1 Format demand

- Replace `ChartSimulator.GetSeasonalSalesMultiplier(month)` with the central enabled/disabled Single getter.
- Replace the inline Album month switch in `AlbumSimulator.CalculateRegionalSales` with the central enabled/disabled Album getter.
- Pass year as well as month so normalization is calendar-correct.
- Apply the sales multiplier exactly once to conversion before raw demand is calculated, preserving the present stock, capacity, random fulfillment, backorder, restock, breakout, and chart paths.
- Do not multiply radio, marketing, recording cost, artist availability, or venue attendance directly into sales.

### 5.2 Radio opportunity

Do not seasonally multiply national `radioHeat`; it is a persistent latent state already feeding awareness and sales.

Apply the radio opportunity factor to ordinary regional radio opportunity at these seams:

- initial regional `radioPlay` for newly promoted Singles;
- the `targetRegionalRadio` pull in `ChartManager.UpdateRecordRegionalData`.

Pass the current year/month and the live/prewarm treatment flag explicitly. Keep Albums at zero radio wherever they are zero today. Do not seasonally scale player-authored `AddRadioPlay` injections, breakout discovery bonuses, jukebox play, radio difficulty, retirement thresholds, or chart-point weights. Those represent explicit actions, earned local propagation, infrastructure, or evaluation rather than general programming capacity.

Clamp `radioPlay` at its existing final boundaries. Do not add a second factor when radio is converted to awareness, chart points, breakout media evidence, or sales.

### 5.3 Recording cost

Apply the recording-cost multiplier once, when production is commissioned:

- orphan Single production;
- standalone Album production;
- linked Album project scheduling, including both Album and promo-Single production;
- any other normal AI release path that calls the centralized production-cost helper.

Use the scheduling/current date, not the future Album drop date. Store the actual charged seasonal production cost in `sunkProductionCost`, project fields, expenses, advances/recoupment, and telemetry exactly as today.

The decision model must compare like with like. Update `BuildDecisionContext`, `CalculateSinglePriorNet`, `CalculateAlbumPriorNet`, and compilation/Album production assumptions to use the same current-month seasonal production-cost convention as the eventual charge. Do not season only the charge while leaving the prior neutral, and do not deduct the cost twice.

Historical/prewarmed records remain untouched. Pressing COGS, packaging-per-unit cost, fixed Album packaging cost, overhead, advances, royalties, and distribution-distance cost are not recording costs and must not receive this multiplier.

### 5.4 Marketing efficiency

The label pays the same marketing budget. Apply the current-month marketing-efficiency multiplier once to the awareness impact returned from that budget:

```text
seasonalMarketingImpact = clamp(BudgetToImpact(budget,tier) * marketingEfficiency(year,month), 0, 1)
marketingAwareness = seasonalMarketingImpact * ChartSimulator.GetCampaignImpact(label) * 0.35
```

Use this convention consistently in:

- `ProjectLaunchAwareness` at the format/strategy decision;
- ordinary `ApplyReleasePromotion`;
- scheduled-Album `ApplyPromotionSnapshot` at the actual drop month.

The stored promotion snapshot must not freeze a scheduling-month marketing multiplier. The fixed budget is planned earlier; its efficiency is realized when the campaign/drop occurs. Do not modify marketing expense, `GetMarketingBudget`, `marketingPower`, campaign impact, label capability, stock directly, or sales directly.

### 5.5 Artist availability

Use artist availability to shape opportunities without changing artist quality or cooldown clocks:

- multiply `RosterManager`'s weekly scouting trigger probability by the normalized current-month availability multiplier;
- multiply `CompetitorManager.CalculateWeeklyReleaseChance` by the same multiplier at the final probability stage, then clamp the final probability to `[0,1]`.

Do not alter candidate ordering, candidate scores, roster capacities, contract rules, artist stats, or `weeksSinceLastRelease`. This is the only channel authorized to reshape release timing. It consumes no additional RNG draw: apply it to the existing thresholds.

Because release timing can feed every downstream system, report its paired effect and obey the release-count gates in section 8. If this two-seam treatment causes a hard gate failure, first retain the release-chance seam and remove the scouting seam; do not tune artist quality, roster capacity, cooldowns, or release growth to compensate.

### 5.6 Venue channel

Expose `GetVenueAttendanceMultiplier(year,month)` and include it in static and runtime telemetry. No current code consumes `concertVenueCount`, `clubCount`, or `theaterCount` as attendance or live revenue, and the UI explicitly says touring records are not kept. Therefore 4C must not fabricate venue income, awareness, sales, artist heat, or costs.

Document the unused seam as intentional. A later touring mechanic may consume the getter once it owns actual events, capacity, ticketing, and expenses.

## 6. Code Shape and Guardrails

- One canonical seasonality service; no duplicate arrays or switches.
- Public getters take explicit `year`, `month`, and treatment/prewarm context as needed. Avoid hidden reads from `TimeManager` inside pure calculation methods.
- Invalid months fail fast in debug/audit code rather than silently using January.
- No new resources or per-region copies are needed; 4C is a national overlay. Do not add regional weather curves in this pass.
- Do not change base purchase rates, demand age decay, awareness decay, radio fatigue, chart weights, chart sizes, retirement rules, stock formulas, restock formulas, prices, pressing costs, distribution/distance settings, Album era curves, substitution/cannibalization, release growth, or label tier values.
- Do not add Christmas records, holiday genre tags, tours, live revenue, weather, school calendars, or region-specific seasonality.
- Do not retune the 4b distance calibration or the Phase 3 Album crossover curve to make 4C pass.
- Preserve the user's existing unrelated worktree changes in `BASELINE-V2.md`, `Systems/ArtistManager.cs`, and `SimTools/Stage3bHoldoutAudit.md`.

## 7. Required Telemetry

Extend the removable audit harness with both seasonality command-line overrides. Emit `seasonality-monthly.csv` with at least:

```text
seed,enabled,year,month,liveWeeks,
singleSalesMultiplier,albumSalesMultiplier,radioOpportunity,
venueAttendanceMultiplier,recordingCostMultiplier,marketingEfficiencyMultiplier,artistAvailabilityMultiplier,
singleUnits,albumUnits,singleGross,albumGross,
releaseRolls,successfulReleases,singleReleases,albumProjectsScheduled,albumDrops,
productionSpend,productionEvents,marketingSpend,marketingEvents,
scoutingRolls,signings,meanRadioPlay
```

Add enough internal counters to distinguish opportunity from realization. Counters are telemetry-only and must not affect behavior or RNG.

Also emit an annual/decade summary containing:

- paired enabled/disabled total and format units, gross, label net, market net, release counts, production spend, marketing spend, signings, and mean radio play;
- calendar-weighted raw and effective means for every channel;
- monthly enabled/disabled ratios by format;
- first December in which Album gross exceeds Single gross;
- first full year in which Album gross exceeds Single gross;
- the lead/lag between those milestones when both exist.

The December crossover watch is descriptive. Report absent milestones honestly. Do not tune the curves to manufacture the proposed two-to-three-year lead.

## 8. Validation Sequence and Gates

### Checkpoint A - Static and exact-off checks

1. Unit-test all table lengths, sums, positivity, radio zero-sum, invalid-month handling, and calendar normalizers for 1960-69.
2. Verify for every year that enabled Single and Album Friday-weighted modifier totals equal their corresponding legacy v2 totals within `1e-6` relative tolerance.
3. Verify every non-sales multiplicative channel has Friday-weighted mean `1` and radio delta has Friday-weighted mean `0` within `1e-6`.
4. Run disabled seed 1001 for 520 weeks. The frozen v2 hashes in `BASELINE-V2.md`, including `market-revenue.csv` and `release-capacity.csv`, must be byte-exact.
5. Repeat one enabled seed-1001 run in an independent process. All emitted streams must be byte-identical between repeats.

Any disabled hash mismatch is a hard stop. Repair the toggle boundary; do not bless new baseline hashes.

### Checkpoint B - Three-seed paired calibration

Run enabled and disabled 520-week pairs for seeds `1001`, `1002`, and `1003`. The disabled runs are the per-seed v2 controls.

Hard economic gates:

- each seed's decade total market units: enabled/disabled in `[0.97, 1.03]`;
- each seed-year total market units: enabled/disabled in `[0.95, 1.05]`;
- each seed's decade Single units and Album units separately: enabled/disabled in `[0.95, 1.05]`;
- each seed's decade gross and market net: enabled/disabled in `[0.95, 1.05]`;
- each seed-year market net: enabled/disabled in `[0.92, 1.08]`;
- each seed's total successful releases and scheduled Album projects: enabled/disabled in `[0.95, 1.05]`;
- each seed-year successful releases: enabled/disabled in `[0.90, 1.10]`.

Inherited simulation-health gates:

- preserve the accepted 4b chart, distance, concentration, distribution-deal, and determinism checks;
- preserve the accepted Album crossover window and 1960 format-mix gates from the current baseline documents;
- paired all-decade closed Top-40 median may move by at most `+/-2` weeks;
- no NaN, infinity, negative cost, negative probability, or out-of-range radio/awareness value;
- venue-driven revenue and venue-driven record sales must remain exactly zero because no venue consumer is authorized.

Seasonal signal checks:

- effective multiplier unit tests must preserve the intended extrema: Single July crest/January trough; Album December crest/January trough; radio summer crest/December trough; venue July crest/January trough; recording cost November crest/January trough; marketing November crest/January trough; availability January crest/December trough;
- aggregate enabled/disabled monthly ratios should show the intended directions for Single and Album units, radio play, production cost per event, marketing awareness return for fixed-budget probes, and release timing. Report correlations and exceptions; do not require observed sales to equal raw multipliers because stock, age, awareness, release mix, and feedback remain live;
- verify by fixed-input probes that production is multiplied once, marketing impact is multiplied once, and sales never include a direct radio/marketing/venue factor;
- report December and annual format-crossover watches without gating them.

Only two calibration scalars are authorized if all integration bugs are excluded and the paired decade unit gates still fail: one global enabled Single sales level scalar and one global enabled Album sales level scalar, each constrained to `[0.97,1.03]`. Apply them after the calendar/legacy normalization, log every attempted value, and use no more than three two-seed probes before the three-seed checkpoint. Do not tune monthly shape values, radio, marketing, cost, availability, demand constants, or baseline systems to chase aggregate parity.

If a required scalar falls outside that band, stop and report which nonlinear pathway defeats ex-ante conservation.

### Checkpoint C - Fresh holdout

After Checkpoint B is frozen, run one fresh enabled/disabled 520-week seed pair exactly once. Confirm from repository audit history that the seed has not been used for prior calibration or holdout work. Apply all Checkpoint B hard gates without further tuning.

A failed holdout is a reported failure. Do not widen a band, consume another seed, or recalibrate after seeing it without a new directive.

## 9. Audit Deliverable

Write `SimTools/MarketSeasonalityAudit.md` containing:

1. code-path map and exact toggle/prewarm behavior;
2. canonical raw tables and per-year effective normalized tables;
3. proof of legacy sales-budget conservation and mean-one/zero channel checks;
4. disabled hash comparison and enabled determinism repeat;
5. complete paired results by seed and year, not only pooled means;
6. monthly shape plots or compact tables for units, radio, releases, costs, and marketing;
7. inherited regression results;
8. December/full-year crossover watch and any observed lead/lag;
9. full calibration log, including failed probes;
10. limitations: no holiday-content classification, no regional/weather variation, no venue consumer, persistent radio feedback, launch-only nonlinear marketing, and endogenous release timing;
11. exact final constants, shipping toggle state, commands, output locations, and final file hashes.

Update `BASELINE-V2.md` only after the fresh holdout passes. Preserve the frozen 4b section and append a clearly labeled 4C acceptance section; do not rewrite historical evidence.

## 10. Completion Condition

4C is complete when the enabled simulation has a visible, explainable seasonal rhythm; disabled mode is byte-identical to frozen v2; each format preserves the inherited v2 annual sales-opportunity budget before feedback; realized decade economics remain approximately 1:1 under the paired gates; release, cost, marketing, and radio effects occur once at their correct seams; the unused venue channel is honestly exposed rather than fabricated; and a fresh holdout passes without post-holdout tuning.
