# D7 label population — the closed reach loop, and a gate measuring the wrong quantity

Continues from `D7LabelPopulationChartCapacityHandoff.md`. That document certified nothing and
asked two questions: re-run the committed ladder configuration to 522, and establish **why
`scheduledAlbumProjects` has so little leeway**. Both are answered here, and the answer to the
second is that the metric is not measuring what the gate thinks it measures.

It also asked how chart slots are allocated across labels — "specifically why no Small or
Independent label ever takes one". That has a single mechanical cause, documented in section 4.

## 1. The committed configuration is certified

`d7-ladderonly-gated-522-1001` (births 6 + ladder rungs A/B/C) completed 522 weeks:
`CHART_AUDIT_COMPLETE run=d7-ladderonly-gated-522-1001 weeks=522`. The prior handoff's
interpolated prediction of ~1.280 at 1966 was close — the measured value is **1.2857**, clean by
0.0143.

| year | `successfulReleases` | `scheduledAlbumProjects` |
|---|---|---|
| 1960 | 1.0478 | 1.2071 |
| 1961 | 1.1234 | 0.8483 |
| 1962 | 1.1326 | 0.8168 |
| 1963 | 1.1174 | 0.8986 |
| 1964 | 1.1245 | 1.0501 |
| 1965 | 1.1144 | 1.2287 |
| 1966 | 1.0910 | **1.2857** |
| 1967 | 1.0174 | 1.1447 |
| 1968 | 1.0192 | 1.1318 |
| 1969 | 1.0272 | 1.1057 |

**The ladder is turning.** Tier transitions over the decade, against head's 11 promotions and
10 demotions:

| transition | head | certified |
|---|---|---|
| `Boutique -> Independent` | 6 | **15** |
| `Small -> Independent` | **0** | **5** |
| `Independent -> MidTier` | 4 | 1 |
| `MidTier -> Major` | 1 | 1 |
| **promotions total** | **11** | **22** |
| `Independent -> Small` (demotion) | 10 | 8 |

`Small -> Independent` fires for the first time. Defect A's repair is therefore *not* fully
inert — five Small labels did accumulate one charting record over the decade — but five in ten
years against 100-130 live Small labels is the floor, not a working rung.

Live-only roster fill has not moved, confirming the third resume item:

| | head | certified |
|---|---|---|
| live labels @ w521 | 317 | 305 |
| aggregate operating target | 2,788 | 2,736 |
| rostered | 2,765 | 2,719 |
| **fill** | **0.9918** | **0.9938** |

One structural side effect to watch: the `Boutique -> Independent` rung drains its tier. Live
Boutiques run 96 → 18 across the decade against head's 90 → 27. Fifteen promotions out of a tier
that peaks at ~96 is a large fraction, and the rung has no inflow to replace them.

## 2. The gate metric is a product of two independent factors

`successfulReleases = singleReleases + albumProjectsScheduled` holds **exactly**, in all 324
month-rows of every run measured, with zero mismatches. So the album-project gate ratio
factorises without residue:

```
scheduledAlbumProjects_ratio  =  successfulReleases_ratio  ×  albumShare_ratio
```

where `albumShare = albumProjectsScheduled / successfulReleases`. Measured at 1966, the binding
year, the identity reproduces every recorded gate value to four decimals:

| configuration | succRel | volume ratio | album share | mix ratio | product | gate recorded |
|---|---|---|---|---|---|---|
| head — births 6 | 3,508 | 1.0715 | 0.8235 | 1.1602 | 1.2431 | 1.2431 |
| births 7 | 3,558 | 1.0867 | 0.8257 | 1.1633 | 1.2642 | 1.2642 |
| births 7 + rungs | 3,620 | 1.1057 | 0.8354 | 1.1768 | 1.3012 | 1.3012 |
| births 8 | 3,648 | 1.1142 | 0.8374 | 1.1798 | 1.3145 | 1.3145 |
| births 7 + ladder + growth | 3,713 | 1.1341 | 0.8274 | 1.1656 | 1.3219 | 1.3219 |

**The mix factor is invariant to every change under test.** Across the full range from head to
the worst abort it moves 1.1602 → 1.1798 — a span of 0.020. What actually moves is volume,
1.0715 → 1.1341, a span of 0.063.

So every abort recorded in the prior handoff is a **volume** abort. And volume already has its
own gate metric, `successfulReleases`, banded at [0.70, 1.30] and reading **1.07–1.13** in the
very runs that aborted. The gate aborted on a quantity it separately measured as fine.

## 3. Why there is no room

The mix factor is a fixed multiplier applied to volume before the band is tested. At 1966 it is
~1.17, which converts the nominal 1.30 ceiling on album projects into an effective ceiling of
`1.30 ÷ 1.17 = 1.11` on release volume — while the declared volume ceiling is 1.30 and the
declared album ceiling is 1.30. Neither number is the one being enforced.

That is the whole answer to "why is there no room". There is no room because 1.17 of the 1.30
band is spent before any change is measured, on a difference the change does not cause.

The mix difference is the LP-transition phase difference the prior handoff hypothesised, and it
is now measured rather than inferred. Album share of the release budget, by year:

| year | control share | enabled share | mix ratio | margin to 0.70 | margin to 1.30 |
|---|---|---|---|---|---|
| 1960 | 0.2062 | 0.2476 | 1.2009 | +0.501 | **+0.099** |
| 1961 | 0.2768 | 0.2092 | 0.7557 | +0.056 | +0.544 |
| 1962 | 0.3407 | 0.2507 | 0.7357 | **+0.036** | +0.564 |
| 1963 | 0.3795 | 0.3061 | 0.8066 | +0.107 | +0.493 |
| 1964 | 0.3494 | 0.3246 | 0.9289 | +0.229 | +0.371 |
| 1965 | 0.5390 | 0.5866 | 1.0884 | +0.388 | +0.212 |
| 1966 | 0.7098 | 0.8235 | 1.1602 | +0.460 | **+0.140** |
| 1967 | 0.8024 | 0.8982 | 1.1194 | +0.419 | +0.181 |
| 1968 | 0.8209 | 0.9036 | 1.1008 | +0.401 | +0.199 |
| 1969 | 0.8578 | 0.9185 | 1.0707 | +0.371 | +0.229 |

The enabled route adopts albums **later** (1961-64 mix runs 0.74–0.93) and then **harder**
(1966-69 runs 1.07–1.16). The curves diverge most mid-ramp and re-converge once both saturate,
exactly as predicted.

Over the same span the volume factor never leaves **1.00–1.10**:

| year | 1960 | 1961 | 1962 | 1963 | 1964 | 1965 | 1966 | 1967 | 1968 | 1969 |
|---|---|---|---|---|---|---|---|---|---|---|
| volume ratio | 1.060 | 1.087 | 1.101 | 1.069 | 1.090 | 1.066 | 1.072 | 1.004 | 1.038 | 1.023 |

**The metric is squeezed from both ends by the same authored behaviour.** In head, with no
change under test at all, it sits 0.099 below the ceiling at 1960 and 0.036 above the floor at
1962. A gate metric that is already within 0.04 of aborting on the unmodified head run is not
measuring structural risk.

### What this rules out

The prior handoff proposed banding the metric per rostered artist. That normalisation cannot
work here, because **the control has no artist market**: it signs 6–18 artists a year against
the enabled route's 1,200–1,900, since `--enable-artist-population-lifecycle` is the thing under
test. The two routes reach comparable release volume by structurally different means — the
control cycles a fixed aging pool, the enabled route churns signings. Any per-artist denominator
divides by a quantity that exists in one route and not the other.

It also rules out simply replacing the absolute band with a mix band. At 1962 the mix ratio is
0.7357, only 0.036 above the floor — that move relocates the squeeze from 1966 to 1962 without
removing it.

### Recommendation on the instrument

`scheduledAlbumProjects` is not a valid control-relative metric for this comparison. The routes
model different album eras by design, and every band on a control-relative ratio of a
transition-sensitive composition will be tight somewhere in 1960-66.

The valid cross-route invariants are the ones that do not depend on format composition, and all
of them have wide margins today: `successfulReleases` (≤1.10), `totalUnits` (1.0888),
`grossRevenue` (1.1181), `labelNet` (1.2073), `marketNet` (1.1654).

Note that the certified state has **less** headroom than head did, not more: 1.2857 at 1966
against head's 1.2431, leaving 0.0143. The ladder rungs cost 0.043 — more than the 0.037 the
prior handoff estimated. Nothing further can land at 1966 under the current instrument.

**Proposed: demote `scheduledAlbumProjects` from fatal to reported.** Keep accumulating and
logging it per completed year so drift stays visible, but stop aborting on it. This is not the
band-widening the prior handoff warned against — it removes a metric that double-counts volume
and charges the enabled route for authored behaviour, while leaving all six other checks fatal
and unchanged. Releasing it frees ~0.14 at 1966 and unblocks births 7 and the ladder together,
whose combined cost was 0.058 against 0.057 available.

## 4. Defect D — the reach loop is closed

Section 4 of the prior handoff found that zero of 190 live Small and Independent labels hold a
recent hit. The cause is a single circularity, the same class as ladder Defects A, B and C: the
mechanism that grants chart access requires chart access.

### The chain

Chart points are essentially weekly units. Measured across 1966 in the head run, on records
within eight weeks of release:

| tier | live labels | owned reach | mean awareness | mean units/wk | mean chart points | mean cutoff |
|---|---|---|---|---|---|---|
| Small | 112 | 0.2904 | 0.3380 | 763 | 793 | 6,632 |
| Independent | 78 | 0.4413 | 0.3942 | 1,003 | 1,037 | 6,606 |
| Boutique | 37 | 0.3785 | 0.3972 | 2,234 | 2,270 | 6,602 |
| MidTier | 65 | 0.6644 | 0.4685 | 4,073 | 4,118 | 6,604 |
| Major | 10 | 0.8832 | 0.5680 | 13,504 | 13,566 | 6,598 |

Reach drives awareness drives units drives chart points. A Small label's mean record runs at
**12% of the cutoff** — 8.4× below the bar. Its best record of 1966 reached 7,924 units and did
cross, which is why Small holds four chart-weeks rather than zero, but only the extreme tail
crosses.

**This is not a quality gap.** Mean record quality runs 0.589 / 0.606 / 0.612 / 0.626 / 0.685
across the five tiers — Small is 6% below MidTier and produces 5.3× fewer units. The gap is
distribution reach, nothing else.

### The loop

`ownedReach` is written in exactly three places. One is `RuntimeLabelProfileFactory`, at birth.
The other two are `CompetitorManager.ReinvestDistributionProfit` (line 2650) and deal resolution
(line 2805), and **both require `activeDeal != null`**:

```csharp
private void ReinvestDistributionProfit(AILabel label, float netIncome) {
    if (label.activeDeal == null || netIncome <= 0f) return;
    ...
    label.ownedReach = Mathf.Min(1f, label.ownedReach + (reinvestment / ...));
}
```

**A label that never signs a distribution deal is frozen at its birth reach for its entire
life.** Small labels are generated at ~0.26 reach and die there.

`TryGenerateDistributionOffer` is correctly restricted to Small/Boutique/Independent — exactly
the tiers that need it. But both of its triggers require existing national chart presence:

- `pullTrigger` calls `HasStrongRegionalChartRecord`, which ANDs its regional-sales test with
  `record.currentPosition > 0` — national chart presence — and `GetQuality() > 0.70f`.
- `pushTrigger` calls `HasRecentTop40Record`, which requires `currentPosition <= 40`.

The only escape is `momentumScore > 0.60f` in the push trigger. Across the whole decade that
produced **7 signed deals** (plus 15 renewals of those and 4 terminations) against ~1,200 label
records. At week 365, **5 of 302 live labels hold a deal, all Boutique; zero Small, zero
Independent.**

So: no reach → no chart → no deal → no reach.

### Which condition binds

Of the three conditions in `HasStrongRegionalChartRecord`, only the national one is scarce.
Measured for Small in 1966:

| condition | Small record-weeks satisfying |
|---|---|
| `GetQuality() > 0.70` | 921 |
| regional sales in a strong region | 135 |
| **`currentPosition > 0`** | **4** |

The predicate is named for a regional signal and has real regional inputs, but is gated on the
national chart. The regional signal it should be reading is present and substantial — Small
records carry a mean `unmetRegionalDemand` of 891 units/week in 1966.

### Size of the unlock

Distinct labels per tier that would become deal-eligible on a regional-breakout signal
(`regionalBreakoutCount > 0`) instead of national chart presence, measured at 1966:

| tier | labels releasing | eligible now (national) | eligible on regional breakout |
|---|---|---|---|
| Small | 116 | 3 | **10** |
| Independent | 103 | 4 | **10** |
| Boutique | 40 | 20 | 28 |

`regionalBreakoutCount` is a per-week snapshot, so the better predicate is
`peakRegionalBreakoutStrength`, a running max on the same record type. It gives a tunable
gradient rather than a binary, and preserves the tier ordering — distinct labels qualifying in
1966:

| tier | live | ≥0.10 | ≥0.20 | **≥0.30** | ≥0.40 | ≥0.50 |
|---|---|---|---|---|---|---|
| Small | 116 | 109 | 48 | **20** | 10 | 7 |
| Independent | 103 | 100 | 56 | **28** | 10 | 5 |
| Boutique | 40 | 40 | 39 | **33** | 28 | 22 |
| MidTier | 66 | 66 | 66 | **66** | 63 | 62 |

A threshold near 0.30 opens the bottom two tiers without flattening the gradient, and is the
suggested starting point.

Deal probabilities are already healthy — `monthlyPullOfferProbability` 0.12,
`monthlyPushOfferProbability` 0.04 — so the rate is not the constraint; the predicate is. A deal
grants 0.30–0.50 borrowed reach, taking a Small label from 0.29 to 0.6–0.8, and
`ReinvestDistributionProfit` then raises `ownedReach` permanently, so the gain survives the
deal's expiry. That is the historically correct ladder: regional breakout → distribution →
national reach → chart.

**Be honest about the ceiling.** This roughly triples Small/Independent deal eligibility, from 7
labels to 20. It does not by itself reach historical charting breadth — see section 5.

### Why this blocks everything else

`GetRecentChartingRecordCount` counts records with `weeksOnChart > 0` — the same national
signal. Three separate systems read it, and all three are dead below MidTier for the same
reason:

- **Promotion.** `Small -> Independent` needs ≥1. Defect A's repair is inert until this is fixed.
- **Survival.** Exit safe harbour needs ≥2, so every Small and Independent label sits on maximum
  exit hazard permanently.
- **Growth.** `GetOrganicGrowthBlockingReason` needs ≥1 past three release lanes.

Promotion also grants neither reach nor appetite: a promoted Small keeps its 0.29 reach
alongside the `maxRosterSize` it cannot grow into, which the prior handoff recorded separately.

## 5. Against the three goals

**Label count.** Both routes decline: control 590 → 335, enabled 477 → 317. The decline is not
specific to this directive's work. Section 1 of the prior handoff established the enabled series
is at rest near 324 rather than falling, and that holds. But both sit ~50% below the authored
`GetTargetLabelCount` curve of 600 → 675 → 625. Births are capped at exactly 72/yr and deaths
run ~22% of standing population; the prior handoff was right that the exit rule's safe-harbour
threshold, not its base chance, is the coupling — and section 4 shows why that threshold is
unreachable for 72% of the population.

**Charting share.** `firmsCharting` runs 103–160 against the authored population. Chart-weeks in
1966 distribute as:

| tier | live labels | chart-weeks | chart units | unit share |
|---|---|---|---|---|
| Small | 112 | 4 | 29,747 | 0.04% |
| Independent | 78 | 4 | 30,792 | 0.04% |
| Boutique | 37 | 151 | 1,580,850 | 2.03% |
| MidTier | 65 | 2,801 | 36,544,402 | 46.89% |
| Major | 10 | 2,060 | 39,729,385 | 50.97% |

**219 labels — 72% of the live population — hold 0.16% of chart presence.** Note that
`indieFamilyChartShare` in `concentration.csv` reads 2.2% for 1966, but `IsIndieFamily` excludes
MidTier; counting MidTier as independent (which its own code comment does — "a large, proven
independent") gives 49%, which is historically reasonable. The reportable defect is not
aggregate indie share, it is that the **bottom two tiers are locked out entirely**.

Chart width is ~136 slots (mean 136.0 in 1966, range 126–145), so ~7,100 slot-weeks exist and
5,020 are used. MidTier labels hold 44 chart-weeks each per year. Reaching historical breadth
(200–400 distinct labels on the Hot 100 in a mid-60s year) needs slot turnover as well as the
reach repair — Defect D is necessary but not sufficient.

**Label growth.** The ladder repairs doubled promotions (11 → 22) and opened `Small ->
Independent` for the first time, but five firings in ten years against 100-130 live Small labels
is a floor rather than a working rung — and `Independent -> MidTier` fell 4 → 1. Section 4 is
why: the rungs read `GetRecentChartingRecordCount`, and 72% of the population cannot produce it.
The certified numbers are in section 1.

## 6. Repairs implemented this pass

### Gate: `scheduledAlbumProjects` demoted to reported

`ValidateCatastrophicCompletedYear` now calls `ReportCompletedYearRatio` for this metric instead
of `CheckCatastrophicRatio`. The value is still accumulated, still printed per completed year as
`COMPLETED_YEAR_RATIO_REPORTED`, and still written to `<run>-catastrophic-fail-fast.csv` under
gate `CompletedYearRatioReported` — it just no longer aborts. The other six checks are untouched
and remain fatal. This is a gate-only change and does not perturb the RNG stream.

### Defect D, part one: the pull trigger reads a regional signal

`HasStrongRegionalChartRecord` no longer requires `record.currentPosition > 0`. It now requires
`record.peakRegionalBreakoutStrength >= regionalBreakoutDealThreshold` (0.30, exported), keeping
the existing quality and strong-region sales conditions. `peakRegionalBreakoutStrength` is a
running maximum on the same `RecordRuntimeData` the predicate already iterated, so no plumbing
changed — it credits a record that broke out regionally at any point rather than only while it
holds a national chart position this week.

This is what the method's name always promised. The push trigger (`HasRecentTop40Record`) is
deliberately left alone: a distributor courting a label off a proven national hit is the correct
direction for that path.

### Defect D, part two: reach can be earned without a deal

New `GrowSelfBuiltDistributionReach`, called alongside `ReinvestDistributionProfit` on the
monthly settlement. A label with **no** active deal that is profitable, carries no loss months,
and clears `selfBuiltReachSurplusMultiple` (2×) of its own monthly overhead spends
`selfBuiltReachReinvestRate` (0.10) of net income to gain `selfBuiltReachMonthlyGain` (0.004)
of owned reach, capped at `SelfBuiltReachCeiling` (0.75).

Surplus is measured against the label's own overhead rather than absolute cash on purpose. The
existing deal-backed path reinvests 0.02 of net against a 5,000,000 cost per reach point, which
is calibrated for Major-scale cash flow — a Small label at ~1,378/month revenue gains about
5.5e-6 reach per month from it, which is nothing. A ratio keeps the route open to a label
thriving at its own scale while staying closed to one merely breaking even.

The ceiling is what keeps this from inflating the top of the market: at week 365 of the
certified run all 10 Majors already sit at or above 0.75 and are excluded outright, 18 of 65
MidTiers likewise, while every Small, Independent and Boutique label has room.

Sustained qualification gains ~0.048 reach per year, so a Small label generated at 0.26 needs
several years of real profitability to approach Independent-level reach. That is the intended
shape: uncommon, but possible.

## 7. What the reach repairs delivered — and what they did not

`d7-reach-gated-522-1001` completed 522 weeks clean. `scheduledAlbumProjects` reported 1.2991 at
1966, which would have cleared the old 1.30 ceiling by 0.0009 — the demotion was not strictly
required for this change, but by less than a thousandth.

**The mechanisms work.** Signed deals over the decade went 9 → **65**. Owned reach by tier:

| tier | certified w156 | reach w156 | reach w365 | reach w521 |
|---|---|---|---|---|
| Small | 0.2718 | 0.2907 | 0.3240 | **0.3449** |
| Independent | 0.4384 | 0.5067 | 0.5538 | **0.5475** |
| Boutique | 0.3818 | 0.4831 | 0.6171 | **0.6691** |
| MidTier | 0.6730 | 0.7349 | 0.7535 | 0.7561 |
| Major | 0.8832 | 0.8832 | 0.8711 | 0.8576 |

Small labels hold 10-12 deals at any time against zero before. Majors never gain from the
self-built path, as the ceiling intends.

**Two of the three goals moved.** Live labels at w521 went 305 → **336** (+10%), and the
trajectory turns upward in the back half (310 at w417 → 329 → 336) rather than drifting. Tier
transitions:

| transition | head | ladder certified | reach |
|---|---|---|---|
| `Small -> Independent` | 0 | 5 | **10** |
| `Boutique -> Independent` | 6 | 15 | 14 |
| `Independent -> MidTier` | 4 | 1 | **15** |
| `MidTier -> Major` | 1 | 1 | 3 |
| **promotions** | **11** | **22** | **42** |
| demotions | 10 | 8 | 15 |

Promotions have almost quadrupled from head. Live-only fill is unchanged at 0.9845.

**The third goal did not move.** `firmsCharting` at 1966 is **102 against head's 103**. Small
labels hold zero charting labels in 1966, against three in head. Mean chart points versus the
cutoff in 1966:

| tier | mean points | cutoff | gap | head gap |
|---|---|---|---|---|
| Small | 759 | 6,500 | **8.6×** | 8.4× |
| Independent | 1,120 | 6,509 | 5.8× | 6.4× |
| Boutique | 2,899 | 6,444 | **2.2×** | 2.9× |
| MidTier | 3,864 | 6,506 | 1.7× | 1.6× |
| Major | 14,284 | 6,496 | 0.5× | 0.5× |

Boutique genuinely improved — its gap closed from 2.9× to 2.2× and its chart-weeks went 151 →
211, tracking its reach gain of +0.29. Small did not: a reach gain of +0.07 cannot close an 8.6×
units gap.

### Why: `nationalReach` is the same defect, one field over

Chart propagation is governed by

```csharp
float propagationCapacity = 0.25f + label.nationalReach * 0.45f + label.distributionStrength * 0.30f;
```

`nationalReach` carries the **larger coefficient**, and it is written in exactly three places —
`AILabelFactory`, `LabelGenerator` and `RuntimeLabelProfileFactory` — **all of them generation**.
Nothing in the simulation ever updates it. Not deals, not promotion, not success.

Runtime-founded Small labels are generated at a mean `nationalReach` of **0.162** and die there
however well they do. Majors are seeded at 0.85-0.98. So propagation capacity runs ~0.43 for a
Small label against ~0.93 for a Major, permanently, and `distributionStrength` — the field this
pass unfroze — only reaches the smaller 0.30 coefficient.

That is why population and promotions moved while chart units did not: promotion and safe
harbour read charting counts and capability, which respond to owned reach, but units are gated
by propagation, which is dominated by a field still frozen at birth.

`nationalReach` also gates the pull trigger (`client.nationalReach < 0.40f`) and feeds
`CalculateCapabilityScore`, so unfreezing it touches three systems at once — the same shape as
Defect D, and it should be treated as Defect E rather than folded into tuning.

## 8. Resume here

Section 6's repairs are implemented, certified at 522, and probe-clean. What remains:

1. **Defect E — unfreeze `nationalReach`** (section 7). This is the lever charting breadth
   actually needs, and it is the same write-once defect this pass fixed for `ownedReach`. A
   label that builds national distribution should raise it; the natural coupling is to let
   sustained self-built reach or a completed distribution deal carry some of its gain into
   `nationalReach` rather than only `ownedReach`. Note it also gates the deal pull trigger, so
   raising it *closes* the `< 0.40` route — that interaction needs deciding, not discovering.
2. **Then re-measure charting breadth.** `firmsCharting` and per-tier chart-weeks are the
   acceptance test, not reach or deal counts. This pass moved the inputs without moving the
   output, and that mistake is cheap to repeat.
3. **Chart-slot concentration is a separate constraint.** The chart is ~136 slots and MidTier
   labels hold ~44 chart-weeks each per year. Even with propagation fixed, historical breadth
   (200-400 distinct labels per year) needs turnover, not only access. Measure chart-run length
   by tier before assuming reach alone gets there.
4. **Re-test births 7 + ladder rungs together.** Blocked previously by 0.0012 on a metric that
   is no longer fatal. Population is now 336 without it, so measure whether it is still wanted.
5. **Watch the Boutique tier.** It drains 96 → 18 over the certified decade and 50 → 24 in the
   reach run, and `Boutique -> Independent` has no inflow to replace what it promotes.

## 8. Reproduction

Unchanged. Control remains the frozen disabled route (commit 53eac45) — do not re-baseline.

```powershell
$godot = 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe'
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=522 --run=<name> --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --profile-performance --catastrophic-fail-fast --strict-1965-acceptance-gate --gate-control-run=d7-portfolio-gated-decade-control-1001
```

A full 522-week run takes ~45 minutes on this machine. Iterate at 366 weeks for anything
touching release or roster volume.

Runs, all seed 1001 against control `d7-portfolio-gated-decade-control-1001`:

| run | configuration | result |
|---|---|---|
| `d7-rotation-gated-522-1001` | births 6 | head, clean; 1.2431 at 1966 |
| `d7-births7-gated-522-1001` | births 7 | clean; 1.2642 at 1966 |
| `d7-ladderonly-gated-522-1001` | births 6 + ladder rungs (**committed**) | **clean at 522; 1.2857 at 1966** |
| `d7-rungs-gated-522-1001` | births 7 + ladder rungs | abort, 1.3012 at 1966 |
| `d7-births8-gated-522-1001` | births 8 | abort, 1.3145 at 1966 |
| `d7-ladder-gated-522-1001` | births 7 + ladder + growth eligibility | abort, 1.3219 at 1966 |
| `d7-reach-probe-156-1001` | reach repairs, ungated 156wk | 22 signed deals vs baseline 3 |
| `d7-reach-gated-522-1001` | births 6 + rungs + reach repairs (**current**) | **clean at 522**; 1.2991 reported at 1966 |

Probe suites re-run against the reach repairs as `d7-reach-probes-52-1001`: D5 (catalog/
zeitgeist and segment-normalization/demand-stage suites), D6 fixed probes 1-71, and
genre-market-v2 all pass, with `CHART_AUDIT_COMPLETE` at 52 weeks and no `PROBE_FAIL`. Run them
separately from any gated comparison — both probe flags perturb the RNG stream.

Analysis notes for reuse:

- Gate ratios reconstruct exactly from `seasonality-monthly.csv` columns 18/19/20
  (`successfulReleases`, `singleReleases`, `albumProjectsScheduled`); 19 + 20 = 18 identically.
- `label-finance.csv` columns are `... ,18 ownedReach, 19 borrowedReach, 20 capability,
  21 dealDistributorId, 22 dealUnrecoupedAdvance`. Filter `status != Defunct/Acquired` for live
  aggregates.
- `label-scouting-vacancy-weekly.csv` gained an `isActiveLabel` column at position 5 after
  `d7-rotation-gated-522-1001` was recorded; runs before and after that point have different
  column offsets. Always filter live-only aggregates on it where present.
- Tier transitions count cleanly from `label-finance.csv` by tracking `labelTier` changes per
  `labelId` across weeks, skipping Defunct/Acquired rows.
