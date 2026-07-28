# D7 Album portfolio commitment — sign repair and capacity derivation

## Where the previous handoff was wrong

`D7FormatProjectCountRepairHandoff.md` anticipated a 1965 floor failure on
`albumUnits`. That is not what happened. The decade run aborted on the ordinary
completed-year band, a year earlier, on a metric the strict gate does not cover:

```
CompletedYearCatastrophicDivergence,scheduledAlbumProjects,851,1322,1964,261,"1/1/1965","completedYear=1964 ratio=0.643722 band=[0.70,1.30]"
```

`--strict-1965-acceptance-gate` never ran. It covers only `singleUnits`,
`albumUnits`, `totalUnits`, `grossRevenue`, `labelNet` and `marketNet`
(`ChartAuditRunner.ValidateStrict1965Acceptance`). On `albumUnits` the enabled run
was at **1.33x** control in 1964 — that floor was never at risk. The problem was
always Album project *count*, in the opposite direction from the one predicted.

## Diagnosis

Reconstructing per-year counts from both rollups (`decisions × albumDecisionShare`)
reproduces the gate's 1964 pair exactly, so the method is sound:

| year | enabled | control | ratio |
|---|---|---|---|
| 1960 | 1130 | 1104 | 1.023 |
| 1961 | 1128 | 1258 | 0.897 |
| 1962 | 1081 | 1353 | 0.799 |
| 1963 | 1068 | 1446 | 0.739 |
| 1964 | 851 | 1322 | **0.644** |
| 1965 | — | 1834 | (would need ≥1284) |
| 1966 | — | 2278 | (would need ≥1595) |

This is not a level offset that a constant thumb can close. The enabled run's Album
propensity is flat; the control's ramps. Both cohorts agree — control adult Album
share runs 0.569→0.863 across 1960–63 while enabled holds 0.465→0.458.

Neither the demand era nor the opportunity term explains the gap.
`GetAcceptedAlbumOpportunityWeight` reduces algebraically to `affinity × willingness`,
identical to `GetEnabledAlbumOpportunityWeight` — `legacyAcceptance`,
`buyingPopulation` and `segregation` all cancel. And `albumDemandRiseStartYear` is
1964, so `eraProgress` is 0 through 1963; `purchaseWillingness` is logged flat at
0.1539 for all five years on **both** routes.

The control's ramp is an artifact. It reaches 0.86 adult Album share in 1963 while
realizing **42%** of its own projected units — the disabled route's binary fork with
`revenueMemoryConfidenceK = 4` never validates the projection it runs away on:

| realized/expected | 1960 | 1961 | 1962 | 1963 | 1964 |
|---|---|---|---|---|---|
| control | 0.915 | 0.761 | 0.642 | 0.477 | 0.423 |
| enabled | 0.903 | 0.725 | 0.676 | 0.643 | 0.690 |

So both runs are wrong in opposite directions: the control ramps the whole market on
a fiction, and the enabled run ramps only the one tier that has a mechanism.

### The mechanism that already existed

`GetAlbumPortfolioCommitmentMultiplier` works, and its docstring named this exact
failure mode. Album choice share by tier, enabled run, against the Major multiplier:

| year | Major mult | **Major** | MidTier | Independent | Boutique | Small |
|---|---|---|---|---|---|---|
| 1960 | 1.000 | 0.277 | 0.284 | 0.197 | 0.226 | 0.209 |
| 1961 | 1.062 | 0.313 | 0.276 | 0.255 | 0.280 | 0.199 |
| 1962 | 1.234 | 0.402 | 0.275 | 0.260 | 0.257 | 0.209 |
| 1963 | 1.475 | 0.433 | 0.272 | 0.262 | 0.247 | 0.212 |
| 1964 | 1.750 | 0.314 | 0.229 | 0.245 | 0.182 | 0.210 |

Major climbs +56% across 1960–63 tracking the multiplier. Every other tier is flat.
Majors are 10–15% of release volume, so the aggregate barely moves. `MidTier`'s
`1 + 0.15·era` is +7.5% at 1964 and invisible in the data; the remaining ~40% of
volume had no era-linked Album term at all.

`LiveAlbumDecisionEligibilityScale` is the wrong tool for this and stays at `1f`. It
is constant in time, so it cannot produce a ramp; at 1.07 with
`FormatChoiceLogitSlope = 10` it is worth roughly +4pp of Album share at crossover,
against a 1965 gap of +52% and a 1966 gap of +87%. It was compensating for a ramp
mechanism that was running backwards — see below — which is why it read as both
harmless and necessary.

## Implemented

### Sign repair (`CalculateAlbumPortfolioCredit`)

`projectedAlbum` is a signed net (`expectedRevenueAtMargin - productionCost`) and
negative values are a designed, common case — `CalculateAlbumChoiceProbability`
returns 0 for `projectedAlbum <= 0f`. Measured on the run, **11–18% of decisions
carry a negative Album net.** The old `projectedAlbum *= commitment` pushed those
75% further below the Single hurdle at 1964, harder every year as `era` rose: the
multiplier punished precisely the marginal propositions its docstring existed to
carry. Commitment is now an additive credit on a positive scale, matching the memory
residual seam three lines above:

```csharp
Mathf.Max(0f, commitmentMultiplier - 1f) * Mathf.Max(1f, Mathf.Abs(priorAlbum))
```

Identical in magnitude when the projection sits near its prior, but always upward.
It stays outside the noise draw because it is policy, not estimation.

`LiveAlbumDecisionEligibilityScale` retains the same asymmetry at
`ResolveAlbumDecision`. At `1f` it is inert, so it was left alone; if it is ever
restored above 1, it needs the same treatment.

### Capacity derivation (`CalculateAlbumPortfolioCapacity`)

Commitment is no longer conferred by tier. It is earned from what a label actually
holds — shelf space to place an LP program and roster depth to keep it fed:

```csharp
.55f * clamp(distributionStrength) + .45f * clamp(rosterSize / 12f)
```

Cash runway was evaluated and **rejected as a term**: measured 1963 runway is
118–608 months across every tier, so it saturates and discriminates nothing.

`AlbumPortfolioCommitmentCeiling` is held at `1.50f`, the former Major coefficient,
so a fully-capable label lands where majors were already calibrated. Measured 1963
inputs and the resulting commitments:

| tier | reach | roster/label | capacity | 1963 (was) | 1964 (was) |
|---|---|---|---|---|---|
| Major | 0.893 | 27.6 | 0.941 | 1.446 (1.475) | 1.706 (1.750) |
| MidTier | 0.673 | 10.0 | 0.745 | 1.353 (1.047) | 1.559 (1.075) |
| Independent | 0.452 | 2.4 | 0.339 | 1.161 (1.000) | 1.254 (1.000) |
| Boutique | 0.389 | 2.3 | 0.298 | 1.141 (1.000) | 1.224 (1.000) |
| Small | 0.273 | 0.8 | 0.180 | 1.085 (1.000) | 1.135 (1.000) |

Volume-weighted mean commitment at 1963 moves 1.084 → 1.276, i.e. mean credit rises
about 3.3x, and for the first time it is distributed across the whole market rather
than concentrated in a tenth of it. Majors are held within 2% of their prior
calibration by construction.

## Verification so far

Sign repair alone, 261 weeks, seed 1001, flags matched to the original decade
command (`d7-portfolio-credit-signfix-noprobe-261-1001`):

| year | baseline | sign fix | control |
|---|---|---|---|
| 1960 | 1130 | 1130 (identical) | 1104 |
| 1961 | 1128 | 1128 (identical) | 1258 |
| 1962 | 1081 | 1063 | 1353 |
| 1963 | 1068 | **1006** | 1446 |

It aborts a year earlier at 0.688. This is expected and not a regression in the
mechanism: Major Album share *rose* (1963: 0.433 → **0.476**) — majors now carry
marginal Albums instead of pushing them away — while tiers with zero credit drifted
down as majors consumed more capacity. The old multiplicative form had been
inflating the already-strong positive tail, and the counts were leaning on that
amplification. Removing it exposes how little of the market has any mechanism at
all, which is the case for the capacity derivation.

Capacity derivation is **implemented and builds clean but has not been measured.**
A 52-week probe pass (`d7-portfolio-capacity-probecheck-52-1001`) completes with no
catastrophic rows, D5 and V2 probe suites green, and 1960 at **1105 Album
projects** — bit-identical to the pre-change baseline, as expected.

Probes added/updated in `GenreMarketV2ProbeSuite`: commitment monotonicity in both
reach and roster depth, neutrality before the era and at zero capacity, and a
sign-safety probe asserting a negative-net proposition is lifted toward the Single
hurdle rather than away from it.

## Why the re-measure cannot be short

`GetAlbumEraWeight` is a `SmoothStep` over 1960–1968, so commitment is *identically
1.0 for every label in 1960* and both changes are exact no-ops:

| year | era | ceiling-capacity mult |
|---|---|---|
| 1960 | 0.000 | 1.000 |
| 1961 | 0.041 | 1.062 |
| 1962 | 0.156 | 1.234 |
| 1963 | 0.316 | 1.475 |
| 1964 | 0.500 | 1.750 |

This is confirmed empirically, not just predicted: 1960 and 1961 came back
bit-identical under the sign repair. **A 52-week or 2-year run is provably blind to
both fixes.** Minimum informative length is 157 weeks (through completed 1962);
minimum decisive is **261 weeks**, which reaches the year that originally aborted
and costs about half a decade run.

Next run:

```powershell
$godot = 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe'
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=261 --run=d7-portfolio-capacity-261-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --profile-performance --catastrophic-fail-fast --gate-control-run=d6-transition-envelope-decade-control-1001
```

Do not add `--genre-market-v2-probes` to a run being compared against the decade
baseline. It perturbs the RNG stream: the first 261-week attempt carried it and
diverged at 1960 (4630 vs 4738 decisions) before any change could apply, which
initially read as a regression. Probe runs and comparison runs must be kept
separate.

## Capacity run result (`d7-portfolio-capacity-261-1001`)

Completes 261 weeks with **no catastrophic rows**. The monotone decay is arrested:

| year | baseline | sign fix only | **capacity** | control | ratio |
|---|---|---|---|---|---|
| 1960 | 1130 | 1130 | 1130 | 1104 | 1.024 |
| 1961 | 1128 | 1128 | 1133 | 1258 | 0.901 |
| 1962 | 1081 | 1063 | 1135 | 1353 | 0.839 |
| 1963 | 1068 | 1006 | 1196 | 1446 | 0.827 |
| 1964 | 851 | — | **1086** | 1322 | **0.821** |

Aggregate Album share now ramps (0.239 → 0.269 → 0.290 → 0.316 → 0.318) where the
baseline was flat (0.239 → 0.233). Critically, the ramp is market-wide rather than
Major-only:

| tier | 1960 | 1962 | 1963 | 1964 | baseline 1964 |
|---|---|---|---|---|---|
| Major | 0.277 | 0.395 | 0.464 | 0.293 | 0.314 |
| MidTier | 0.284 | 0.306 | 0.326 | **0.342** | 0.229 |
| Independent | 0.197 | 0.284 | 0.309 | **0.319** | 0.245 |
| Boutique | 0.226 | 0.233 | 0.261 | 0.285 | 0.182 |
| Small | 0.209 | 0.258 | 0.233 | 0.291 | 0.210 |

Every tier except Major now keeps climbing through 1964, and they carry the
aggregate past the year that previously aborted.

## Decade result (`d7-portfolio-capacity-decade-522-1001`)

522 weeks, seed 1001, `--strict-1965-acceptance-gate` armed. **The Album
project-count objective is met.** The strict 1965 gate passed. The run reached 1968
before aborting on an unrelated metric.

| year | enabled | control | ratio |
|---|---|---|---|
| 1960 | 1130 | 1104 | 1.024 |
| 1961 | 1133 | 1258 | 0.901 |
| 1962 | 1135 | 1353 | 0.839 |
| 1963 | 1196 | 1446 | 0.827 |
| 1964 | 1086 | 1325 | 0.820 |
| 1965 | 1916 | 1834 | **1.045** |
| 1966 | 2322 | 2278 | **1.019** |
| 1967 | 2381 | 2540 | 0.937 |
| 1968 | 2553 | 2651 | 0.963 |

Album decision share now reproduces the control's shape rather than a flat line —
enabled 0.585 / 0.781 / 0.809 / 0.818 against control 0.551 / 0.720 / 0.790 / 0.800
for 1965–68. The mid-decade LP takeover emerges from label capacity and era weight
rather than from any authored thumb, which was the objective.

`LiveAlbumDecisionEligibilityScale` remains `1f` and is not needed.

### New, unrelated blocker: Single unit collapse at 1967

```
CompletedYearCatastrophicDivergence,totalUnits,96073794,138762831,1968,470,"1/3/1969",ratio=0.692360 band=[0.70,1.30]
```

Albums are not implicated — 1968 Album units are 37.8M against control 33.1M
(1.14x). Singles are:

| year | enSingle | ctSingle | ratio | enTotal | ctTotal | ratio |
|---|---|---|---|---|---|---|
| 1964 | 152.6M | 152.9M | 0.998 | 161.9M | 159.5M | 1.015 |
| 1965 | 157.3M | 164.7M | 0.955 | 171.2M | 175.8M | 0.974 |
| 1966 | 145.4M | 116.8M | **1.244** | 167.3M | 136.1M | 1.230 |
| 1967 | 62.5M | 104.3M | **0.599** | 92.5M | 131.7M | 0.702 |
| 1968 | 58.3M | 105.6M | 0.552 | 96.1M | 138.8M | 0.693 |

Single units fall 145.4M → 62.5M in one year (−57%) while the control declines
gently. 1967 squeaked through at 0.702; 1968 failed at 0.693. The discontinuity
between 1966 (1.244, well *above* control) and 1967 (0.599) is a cliff, not a
drift — this is a demand-side event, not a format-mix consequence, since total
market units drop with it.

This is **not** attributable to the portfolio commitment work: commitment moves the
Album/Single fork, and Album counts and units both track control across exactly
these years. Worth noting for whoever picks it up that `Data/GenreCatalog.cs` has
uncommitted changes in the working tree and the catalog carries authored keyframes
at `baseline1966` / `baseline1967` / `baseline1968`, so a genre-baseline or
lifecycle-death authoring issue at 1967 is the first place to look, ahead of
anything in `CompetitorManager`.

## Promo-Single extinction — diagnosis and repair

### What it was

Single units collapsed 145.4M -> 62.5M between 1966 and 1967. The proximate cause was
not demand: genre acceptance was *rising* and format decisions were flat. Single
*releases* fell 3166 -> 595. `promoShare` collapsed 0.472 -> 0.0099 -> 0.0000 while
`standaloneShare` jumped to 0.82. A promo project emits two products, a standalone
emits one, so when promo dies every Album project stops emitting its Single.

Two compounding defects, found in this order:

**(a) Cannibalization scaled without bound; the promo benefit did not.**
`substitutionPropensity = substitutionK * albumDemandFactor` with `albumDemandFactor`
climbing .035 -> .389 across the decade, so `cannibalizationLoss` grew 16x, while
`expectedPromoLift` was a fixed `(1 - launchAwareness) * 10000f` and
`expectedPromoSingleNet` plateaued near 24,000. The model let a Single steal ever more
from the Album but never let it *sell* Albums. `promoAdvantage` crossed zero at 1967
and the strategy became permanently non-viable. Repaired with
`CalculatePromoAlbumSynergyGain`: recruitment now scales on the same terms as
diversion — album demand, Single reach, Album margin — so neither can outgrow the
other. Only the gating differs: diversion on shelf overlap, recruitment on awareness
headroom.

**(b) The absolute viability gate turned decline into an absorbing state.**
`ResolveAlbumDecision` required `totalProjectMemoryProjection > 0f`. Once the
AlbumWithPromo lane's residual went negative, promo was vetoed everywhere at once —
and a vetoed strategy generates no further evidence, so the lane could never recover.
The disabled route never traps because its equivalent test is relative
(`componentProjectedAlbumWithPromo > projectedAlbum`), which is why the control slides
gently .655 -> .597 -> .437 instead of collapsing. Viability is now judged on current
component economics (`componentProjectedAlbumWithPromo > 0f`); memory still ranks
strategies through the component projections but holds no permanent veto.

Fixing (a) alone only moved the cliff from 1967 to 1968: `promoAdvantage` reached
+24,763 while `promoProj <= 0` stayed at 78.9%, proving the economics were never the
binding constraint. Both fixes are required, and both are probe-covered.

### Calibration status — provisional

`PromoAlbumConversionK` (.50), `PromoAwarenessConversionFloor` (.25) and
`substitutionCap` (.60) went through three fitting passes that moved the 1967 unit
ratio only 1.382 -> 1.361. They were being fitted against a target that is itself
wrong (see below). **Do not trust these constants**; refit them after the 1966
single-market question is settled. Note `substitutionCap` at its original .85 never
engaged at all, since `albumDemandFactor` tops out near .40 — but capping it hard
(.35) left promo unopposed and overshot the ceiling. With the absorbing state removed,
a loose cap is correct: a declining promo advantage now produces a graceful slide.

## Correction — the earlier "clean decade" was clean for the wrong reason

`d7-portfolio-capacity-decade-522-1001` and `d7-promo-synergy-decade-522-1001` were
reported as passing. The latter passed 1968 at 0.734 **only because promo had already
collapsed to 0.007**, suppressing Single units to 64.3M and dragging total units down
into band. Remove that collapse — which is the correct behaviour — and 1967 rises
through the ceiling. The decade was never genuinely in band; the promo defect was
masking a Single-side excess.

## The 1966 Single contraction is a control artifact — do not chase it

Current head config breaches the *ceiling*: 1967 total units 179.3M vs control 131.7M
= 1.361. Albums are not implicated and track the control closely (1966: 21.7M vs
19.3M; 1967: 29.7M vs 27.4M). The entire divergence is Singles:

| year | control | enabled |
|---|---|---|
| 1965 | 164.7M | 157.6M |
| 1966 | **116.8M** (-29%) | **153.3M** (-3%) |
| 1967 | 104.3M | 149.7M |

Promo share is not driving it — 1966 Single units are 153.2M / 153.3M across two runs
whose promo shares were .753 and .775.

The control's one-year -29% contraction has two structural causes, neither historical:

1. **Synchronized decline at a single keyframe.** Nearly every high-baseline,
   Single-oriented genre drops hard into 1966: RockAndRoll .50 -> .24, BritishBeat
   .95 -> .70, SurfRock .65 -> .40, TeenPop .50 -> .35, TraditionalPop .31 -> .16,
   Folk .60 -> .45, DooWop .20 -> .10, EasyListening .52 -> .42. Replacements are
   Album-leaning (PsychedelicRock .10 -> .55, FolkRock .10 -> .75, SunshinePop
   .08 -> .22). Each keyframe is individually defensible; their *simultaneity* is what
   produces a market-wide cliff.
2. **The keyframe grid densifies exactly at 1966.** Baselines run 1960, 1962, 1964,
   1966, 1967, 1968, 1969 — two-year spacing before 1966, one-year after. Year-over-year
   rates of change are therefore structurally damped before 1966 and undamped from 1966
   on. This is a grid artifact rather than authored intent and is the likeliest source
   of the abrupt 1965-entrance / 1967-exit genre behaviour previously flagged.

Historically 1966 was a *strong* Singles year — "Good Vibrations", "Paperback Writer",
"Wild Thing", "96 Tears", "Last Train to Clarksville", Motown at full strength. The LP
passed Singles in dollar share back in the late 1950s, and the Singles decline through
the late 1960s was gradual, not a one-year collapse. The enabled run's -3%/yr is closer
to correct than the control's -29%, though likely too flat — some real decline belongs
there.

**Recommendation:** treat `totalUnits` at 1966-67 as an invalid gate target against this
control, the same way `scheduledAlbumProjects` was questioned earlier. Either re-baseline
those years, widen the band for them, or accept that the enabled run should sit above the
control there. Do not tune the promo constants to close a gap that should not close.

## Outstanding — the prior over-projection (the remaining lever)

**Correction to an earlier reading of this metric.** The rollup's
`completedMeanExpected`/`completedMeanRealized` pair is *not* Album unit
over-projection. `OnReleaseOutcome` accumulates it across **all formats pooled**, and
it is an expected-vs-realized **net**, not units. The "~30% Album unit
over-projection" cited from those columns was wrong on both counts. It is, however,
horizon-fair: it keys on release completion, so there is no truncation artifact in
the pooled figure itself.

The Album-specific measurement, joining `-release-outcomes.csv` to the Album prior by
`recordId`, is **censoring-biased and usable only for the earliest cohort**. Only
retired Albums match, so later decision years contain only the fastest failures:

| decision year | matched | of scheduled | realized/projected |
|---|---|---|---|
| 1960 | 1060 | 1130 (94%) | **0.897** |
| 1961 | 788 | 1133 (70%) | 0.547 |
| 1962 | 290 | 1135 (26%) | 0.052 |
| 1963 | 183 | 1196 (15%) | −0.107 |
| 1964 | 83 | 1086 (8%) | −0.125 |

Only the 1960 row is trustworthy, and it shows roughly a **10%** Album net
over-projection — materially smaller than the pooled figure suggested. The later
rows are survivorship artifacts, not evidence of collapse.

So the premise of this lever holds but its **magnitude cannot be sized from a
261-week run**, because Album cohorts do not complete inside it. Do not recalibrate
`priorUnitScalarAlbum = 175000f` on these numbers.

What *is* confirmed is the mechanism behind the residual 1964 Major cliff
(0.464 → 0.293), and it is not the project-capacity cap — `albumCapacityReroute`
fires at **0.0%** for every tier in every year. It is confidence-weighted memory
suppression, and `confidenceAlbum` orders exactly as the cliff does:

| tier | 1962 | 1963 | 1964 |
|---|---|---|---|
| Major | 0.36 | 0.41 | **0.47** |
| MidTier | 0.27 | 0.29 | 0.31 |
| Independent | 0.12 | 0.14 | 0.15 |
| Small | 0.05 | 0.05 | 0.05 |

Majors accumulate Album history fastest, so the negative residual reaches them
first. Backing the portfolio credit out of the observed projected/prior ratio
(Major 1964: 1.375 against a credit factor of ~0.706) implies
`confidence × residual ≈ −0.33`. The residual is genuinely negative and its weight
grows with evidence, capped at `ResponsiveMemoryMaximumConfidence = .65`.

**The forward risk is that this cliff propagates.** Major hit it at confidence 0.47;
MidTier is at 0.31 and climbing, Independent 0.15. As each tier accumulates evidence
across 1965–69 it should hit the same wall in turn, which would re-open the count
gap in the back half of the decade even with commitment working. That, not the 1964
gate, is what the lever needs to address.

Evidence required before touching it — a run whose Album cohorts actually complete:

- full decade (522 weeks), which gives ≥90% cohort completion through about 1966
- per-cohort realized/projected by decision year, discarding any year below ~85%
  completion
- `affinityUnits` vs `weightedHitUnits` in `-a3-economic-decisions.csv` on the
  complete cohorts, to localize the error to `priorUnitScalarAlbum` or
  `priorCompHitUnitScalar` before moving either

### Resolved, partially — and why it stays partial

Decomposed on completed cohorts, realized production cost is only ~9% of expected
revenue, so this is genuinely a revenue/unit forecast error rather than cost leverage.
Fitting it fully — the measured correction is .65 / .66 / .74 / 1.00 for 1961-64 —
**aborts the gate at 1961**: 798 Album projects against a control 1257, ratio .635.

The control's early-decade Album counts *require* an over-projecting prior. That is not
obviously a defect. The prior is the label's belief, not ground truth, and early-60s A&R
genuinely over-committed to LPs; the error also closes on its own as the market matures
(realized/prior .949 by 1964) with the memory residual as the learning mechanism. So the
correction is deliberately partial — `AlbumPriorEarlyEraDiscount = .78` shaped by the
album era weight, damping only the pre-boom years.

Both endpoints are excluded, and both exclusions were measured wins:

| year | discounted | excluded |
|---|---|---|
| 1960 Album ratio | 0.825 | **0.957** |
| 1964 Album ratio | 0.777 | **0.845** |
| 1964 realized/prior | 1.106 (over-corrected) | ~0.95 |

1960 is the bootstrap year on a seeded catalog and already measured .913; 1964 onward is
correct without help. Better on counts *and* on honesty in both cases.

## Tree state

Head carries the correct-but-failing configuration by explicit decision: the promo
structural fixes are retained even though the gate breaches at 1967, because the breach
now points at the real remaining defect rather than hiding it. The configuration that
passed a full decade (`d7-promo-synergy-decade-522-1001`) did so on a false mechanism and
should not be restored.

Current head result: `d7-promo-rebalanced-522-1001`, aborts `totalUnits` 1967 at 1.361
(ceiling). Albums healthy throughout.

## Next steps, in order

1. **Settle the 1966 Single-market shape** — decide whether the control's -29% or the
   enabled run's -3% is the intended target, per the analysis above. Everything
   downstream is being fitted against this, so it blocks the rest. Start with the
   keyframe grid spacing at 1966, not with `CompetitorManager`.
2. **Refit the promo constants** once (1) is settled. The structural fixes stand; only
   `PromoAlbumConversionK`, `PromoAwarenessConversionFloor` and `substitutionCap` are open.
3. **Re-examine the gate control itself.** Two separate metrics have now been found to be
   chasing control behaviour that is itself modelled wrongly — `scheduledAlbumProjects`
   (control realizes 42% of its own projections and never corrects) and now `totalUnits`
   at 1966-67. `Data/GenreCatalog.cs` also has uncommitted working-tree changes that the
   control predates.

---

# Amendment — the 1966 contraction is in `Zeitgeist.cs`, and the control was stale

## Correction to step 1: it named the wrong file

Step 1 above sends the next reader to the `GenreCatalog` keyframe grid. **The gate control
never reads that grid.** The control is the disabled route, and on the disabled route
`ChartManager.GetEffectiveGenreAcceptance` falls through to `Zeitgeist.genreAcceptance`,
while `GetFormatPriorMultiplier(liveOverride: false)` returns a flat `1f`. Nothing in
`Data/GenreCatalog.cs` can move the control by a single unit. The per-genre 1966 drops
quoted in the earlier analysis (RockAndRoll .50 -> .24, SurfRock .65 -> .40, and the rest)
are the *enabled* catalog's values being used to explain a *disabled*-route contraction.

The keyframe-spacing hypothesis was also wrong on its own terms. `Zeitgeist` uses the same
`{1960, 1962, 1964, 1966, 1967, 1968, 1969}` grid, so 1965 is the midpoint of 1964 and
1966 and any authored 1964->1966 decline is split evenly across both years. A grid artifact
cannot concentrate a drop on 1966 alone.

## What it actually was

`Zeitgeist.GetForYear` rebuilt each keyframe from a flat `0.3f` default and then applied a
sparse override list. Omission therefore meant **"revert to the acceptance of a genre
nobody has an opinion about"** rather than **"unchanged"**. Genres dropped out of a year's
list silently snapped to 0.3:

| year | snapped from an established level to the 0.3 default |
|---|---|
| 1962 | Jazz (.50), Country (.65), Gospel (.35) |
| 1964 | RockAndRoll (.65), RnB (.50) |
| **1966** | **EasyListening (.55), TeenPop (.50), Folk (.60), SurfRock (.65), BossaNova (.55)** |
| 1967 | GirlGroup (.45) |
| 1968 | TraditionalPop (.35), Motown (.85), BritishInvasion (.70), GarageRock (.55), BaroquePop (.65) |
| 1969 | FolkRock (.60), SunshinePop (.60) |

Total acceptance mass barely moved across 1966 (13.45 -> 13.20), which is why this never
showed up as a demand problem. What moved was *composition*: the five 1966 casualties are
the genres carrying 1964's installed artist population, and every offsetting gain
(FolkRock, BaroquePop, SunshinePop, BluesRock) went to a genre that emerges in 1965-66 with
no roster behind it. Control Single releases fell 3336 -> 2966 **and** units per release
fell 49.4k -> 39.4k, in the same year.

The defect cut upward too, which is what confirms it as an artifact rather than authoring:
doo-wop rose .05 -> **.30** in 1968 before falling to .02 in 1969, and hard rock,
proto-metal, progressive rock and proto-punk all sat at .30 from 1962 to 1966 after being
authored at .01 in 1960.

## The repair

`GetForYear` now applies each keyframe as a cumulative sparse override on top of the years
before it, so omission means unchanged. Genres never named before a year still start at
0.3 — that is the genuine "no prior value" case, and the legacy artist generator does not
place records in those genres that early, so the value is inert where it is wrong.

Carry-forward alone would freeze genres that really should fade, so explicit decay was
authored for each of the 18 snap discontinuities. Each follows the direction of the
canonical `GenreCatalog` curve for the same genre but is anchored on the legacy table's own
level, so the two routes agree on shape without this table being re-levelled underneath the
disabled-route calibration. Every genre curve is now monotone-plausible with no snap:

| genre | 1960 | 1962 | 1964 | 1966 | 1967 | 1968 | 1969 |
|---|---|---|---|---|---|---|---|
| EasyListening | .80 | .70 | .55 | .52 | .44 | .37 | .30 |
| RockAndRoll | .60 | .65 | .50 | .24 | .16 | .10 | .07 |
| TeenPop | .70 | .75 | .50 | .35 | .30 | .28 | .25 |
| Folk | .40 | .50 | .60 | .45 | .35 | .30 | .30 |
| Country | .65 | .60 | .58 | .56 | .55 | .55 | .56 |
| Jazz | .50 | .42 | .34 | .30 | .29 | .28 | .27 |
| RnB | .40 | .50 | .55 | .50 | .48 | .45 | .42 |
| BritishInvasion | .05 | .05 | .95 | .80 | .70 | .50 | .40 |

Probe-covered by `ProbeLegacyZeitgeistContinuity`: a genre named once holds its value for
the decade (Gospel), the 1966 cohort declines rather than collapsing to the default, a
genre in terminal decline never rebounds (DooWop), pre-emergent genres are not lifted to
the default, no genre loses more than a third of the scale in one keyframe step, and the
1960 bootstrap is unchanged.

## The disabled route could not run at all

Regenerating the control surfaced a second, unrelated defect: **the disabled route crashed
in week 2** with `KeyNotFoundException: BritishBlues` at
`ChartManager.UpdateGenreMomentum`. `genreMomentum` is seeded, decayed and clamped over
`GenreDomains.Current` — the legacy 33 on the disabled route — but
`ArtistManager.GetRelatedGenre` is not domain-aware and draws `BritishBlues` as a secondary
for `BluesRock`. Confirmed pre-existing by reverting `Zeitgeist.cs` to `HEAD` and
reproducing it exactly. Every *read* path in the same file already guarded with
`ContainsKey`; only the two accumulate sites did not. They now go through
`AddGenreMomentum`, which is a no-op on the enabled route (its secondaries are
canonicalized, so the key always exists).

This is why the previous control was stale rather than merely old: it could not have been
regenerated at head. Its week 1 is 1.05M chart units against the repaired route's 1.19M —
a 13% divergence before any Zeitgeist year effect can apply.

## New gate control — `d7-zeitgeist-repair-decade-control-1001`

522 weeks, seed 1001, disabled route. Command:

```powershell
$godot = 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe'
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=522 --run=d7-zeitgeist-repair-decade-control-1001 --seed=1001 --profile-performance
```

The one-year cliff is gone and the decade now grows, which is what the real market did:

| year | old control total | YoY | new control total | YoY |
|---|---|---|---|---|
| 1960 | 144.7M | — | 156.0M | — |
| 1964 | 159.5M | +12.1% | 152.8M | +1.2% |
| 1965 | 175.8M | +10.3% | 170.7M | +11.7% |
| 1966 | 136.1M | **-22.6%** | 169.9M | **-0.4%** |
| 1967 | 131.7M | -3.2% | 150.9M | -11.2% |
| 1969 | 131.8M | -5.0% | 160.6M | +3.4% |

Single units at 1966 now fall -6.6% (159.5M -> 148.9M) against the old control's -29%, and
units per Single release hold at 48.7k -> 48.8k instead of collapsing 49.4k -> 39.4k. The
enabled run's -3% and the repaired control's -6.6% are now the same shape; the earlier
"which one is right" question resolves to "neither was, and the gap was the artifact".

**Residual, deliberately not chased:** the new control still dips -11.2% at 1967. It is
driven by the authored 1967 decays landing on the control's most populated genres —
RockAndRoll (424 decisions), DooWop (414, at an authored .05), TraditionalPop, GirlGroup.
The disabled route has no genre migration, so its roster stays full of 1960 identities all
decade and any historically honest decay curve starves it. This was left alone on purpose:
a -11% dip inside a growing decade is a dip, not a cliff, the gate band is +/-30%, and
tightening it further would be tuning the reference toward the thing it is supposed to
measure.

## Full decade against the repaired control — `d7-zeitgeist-repair-enabled-522-1001`

522 weeks, seed 1001, ungated so the run completes and every year can be read. Verified
bit-identical to `d7-promo-rebalanced-522-1001` at week 104, confirming both repairs are
exact no-ops on the enabled route.

**Four of the six gate metrics are in band for the whole decade, and the strict 1965 gate
passes with wide margin** (singleUnits .988 / albumUnits 1.235 / totalUnits 1.004 /
grossRevenue 1.052 / labelNet 1.102 / marketNet 1.097 against floors of .85/.80/.85):

| year | totalUnits | grossRevenue | labelNet | marketNet | scheduledAlbumProjects | successfulReleases |
|---|---|---|---|---|---|---|
| 1960 | 0.952 | 0.987 | 1.021 | 1.017 | 1.160 | 1.075 |
| 1963 | 0.999 | 1.027 | 1.074 | 1.059 | **0.592** | 0.896 |
| 1964 | 1.058 | 1.110 | 1.168 | 1.161 | **0.570** | 0.869 |
| 1965 | 1.004 | 1.052 | 1.102 | 1.097 | **0.640** | 0.802 |
| 1966 | 1.030 | 1.033 | 1.077 | 1.075 | 0.747 | 0.806 |
| 1967 | 1.189 | 1.056 | 1.095 | 1.089 | **0.657** | **0.674** |
| 1968 | 1.187 | 1.045 | 1.090 | 1.081 | **0.562** | **0.576** |
| 1969 | 1.129 | 1.071 | 1.131 | 1.112 | **0.514** | **0.512** |

`totalUnits` at 1967 is 1.189 against 1.361 under the old control, and it never breaches
again. **The 1966-67 unit ceiling was entirely the control artifact.**

## Step 2 resolved: the promo constants need no refit

The handoff's own warning was correct — "do not tune the promo constants to close a gap
that should not close." The gap closed on its own when the reference was repaired.
`PromoAlbumConversionK` (.50), `PromoAwarenessConversionFloor` (.25) and `substitutionCap`
(.60) are **left exactly as they are**, on evidence rather than by default:

- Every revenue and unit metric they could plausibly move is in band across all ten years.
- The promo lane is stable and shows no trace of the absorbing state that motivated the
  structural fixes: promo share runs .568 / .775 / .751 / .709 / **.686** for 1965-69,
  against the pre-fix collapse to .007. Orphan and standalone shares stay live alongside it
  (.147 / .167 at 1969), so all three strategies remain in play to the end of the decade.
- Realized-over-expected holds flat at .60-.70 from 1962 on, i.e. the lane is
  self-correcting rather than running away.

One measurement correction for whoever revisits these: the earlier note that
`substitutionCap` "at its original .85 never engaged at all, since `albumDemandFactor` tops
out near .40" is wrong — .40 is the annual *mean*. Measured over 28,033 decisions the max
is **.857** and p99 is **.677**, so the .60 cap binds on 1.26% of decisions. It is a live
parameter, not an inert one. It is still left at .60, because the case for a cap is a
modelling claim (a large part of the 45 market never converts to LPs at any level of album
dominance) rather than a fit to the 1966 target, and .60 is healthy across the full decade.

## The real remaining defect: the enabled route runs out of artists

`scheduledAlbumProjects` and `successfulReleases` both breach from 1967, and **neither is a
format-mix problem.** Enabled Album decision share (.853 at 1969) tracks the control's
(.963) closely. What collapses is the number of decisions there are to take:

| year | enabled decisions | control decisions | ratio | enabled Single releases | units per Single release (enabled / control) |
|---|---|---|---|---|---|
| 1960 | 4678 | 4418 | 1.059 | 4602 | 31.8k / 35.6k |
| 1964 | 3524 | 3399 | 1.037 | 3535 | 43.2k / 43.2k |
| 1966 | 2649 | 3081 | 0.860 | 2612 | 58.7k / 48.8k |
| 1967 | 2341 | 3269 | 0.716 | 2118 | 70.7k / 37.7k |
| 1969 | **1876** | 3236 | **0.580** | **1556** | **87.8k / 38.1k** |

The enabled route ends the decade issuing 1556 Singles that sell 87.8k each, against a
control issuing 3045 that sell 38.1k each. `totalUnits` lands in band only because a -49%
release-count error and a +130% per-release-demand error cancel.

The source is the artist population, not the format fork. The registry only ever grows,
by exactly the formation rate, while the active pool empties:

> **Correction.** An earlier revision of this section doubled every population figure.
> `artist-population-weekly.csv` carries an `All` row *and* one row per label tier, and the
> first pass summed across all of them. Only `registryTotal`, `activeTotal`, `rostered` and
> `neverSignedUnsigned` were affected — `inactive`, `retired` and `disbanded` are written on
> the `All` row alone. Read the `All` rows only. The corrected figures below are steeper
> than the ones first reported, not milder.

| year | registry | active | rostered | neverSigned | inactive | retired | disbanded |
|---|---|---|---|---|---|---|---|
| 1960 | 7300 | 7300 | 2739 | 3639 | 0 | 0 | 0 |
| 1963 | 8194 | 5098 | 2094 | 320 | 2133 | 79 | 884 |
| 1966 | 9094 | 2513 | 1391 | 0 | 2791 | 384 | 3406 |
| 1969 | 9994 | **1680** | **950** | 0 | 3012 | 1015 | 4287 |

Active artists fall **77%** while the registry grows. Of the 7299 that leave the active
pool, retirement accounts for 1015 — the rest are 4287 disbanded and 3012 inactive.

## Root cause: the talent supply pipeline cannot sustain the market

A standing population equilibrates at (formation rate x mean career length).
`AnnualRuntimeFormationCount` was a hard **300/yr**, and measured mean career to terminal
exit is about 5.6 years, which gives a steady state near 1680 — exactly where the run
lands. But the simulation is *seeded* with 7300 artists. **The seeded population and the
replacement rate were inconsistent by a factor of about four.**

What carries the early decade is a one-time endowment: 3639 never-contracted artists parked
`Latent` at 1960. The labor market drains it and never recovers:

| year | activeRostered | expFreeAgents | freshSeeking | freshLatent | hiringLabels/wk | activations | firstTimeSignings |
|---|---|---|---|---|---|---|---|
| 1960 | 2712 | 925 | 35 | 3628 | 24 | 372 | 804 |
| 1963 | 2070 | 2684 | 33 | 311 | 33 | 1088 | 1382 |
| 1964 | 1898 | 2185 | 6 | **0** | 46 | 311 | 620 |
| 1966 | 1377 | 1130 | 6 | 0 | 118 | **0** | 294 |
| 1969 | 942 | 732 | 6 | 0 | **197** | **0** | 300 |

`freshLatent` hits zero in 1964 and prospect activations stop entirely from 1965.
`firstTimeSignings` then pins to ~300/yr — exactly the formation rate, every new act
consumed on arrival — while `affordableHiringOpportunityLabels` grows **8x**, from 24 to
197. Demand rises all decade against a fixed trickle of supply.

The scouting funnel shows the same starvation from the labels' side: mean discovery pool
falls from **8.8** candidates per label-week to **1.9**, and across 1966-69 the industry
files 201,596 nominations plus 15,328 `NoQualifyingCandidate` failures to produce roughly
500 signings a year. Labels are not choosing to shrink; there is nothing to sign.

Two secondary defects compound it and are **not** addressed here:

- **The terminal inactivity clock is opportunity-blind.** 78 unowned weeks sends any artist
  with a prior contract to `Inactive`, then 52 more to `Disbanded`/`Retired`, with no path
  back — `IsEligibleUnsignedCandidate` requires `isActive`, and nothing restores
  `lifecycleStatus` to `Active`. It runs regardless of whether the industry had a vacancy.
  The model already knows how to hold surplus talent without destroying it — that is what
  `ProspectMarketStatus.Latent` and the `hiringOpportunities` cap in
  `CalculateProspectActivationCount` are for — but that machinery is restricted to
  `contractSequence == 0`. Sign once and you lose access to the reservoir and get the death
  clock instead, which is backwards: a proven act is *more* likely to get another deal.
- **Groups disband unconditionally; solos have an age guard.** `ApplyLifecycleExits` spares
  a solo whose lead is under 35 but destroys a group of any age. That asymmetry is why
  disbandment (4287) is four times retirement (1015).

## Implemented: demand-responsive formation

`CalculateResponsiveAnnualFormationTarget(hiringOpportunities, seeking, latent)` scales the
annual formation quota by the share of hiring demand the prospect market cannot cover,
bounded to [300, 1200]:

```
unmetShare = clamp((opportunities - (seeking + latent)) / opportunities, 0, 1)
target     = clamp(300 * (1 + 3 * unmetShare), 300, 1200)
```

It is deliberately blind to the experienced free-agent pool. Those artists are already on
the terminal clock and are, by revealed preference, the ones labels keep passing over —
counting them as supply is what let the market read as well-stocked (732 free agents
against 197 openings) while discovery pools ran dry at 1.9 candidates.

The signal is last week's, because formation is materialized before prospect activation
recomputes it; the one-week lag is deterministic and avoids a circular dependency.

It is inert while supply is ample — at 1960, latent 3628 against 24 openings gives
`unmetShare = 0` and the base 300 — so the early-decade calibration and the 1960 gate year
are untouched by construction.

Probe-covered in `ProbeCalendarFormationQuota`: inert while the prospect market covers
openings, monotone in unmet demand, bounded at 1200, and the calendar quota stays exact at
the ceiling.

## Result — `d7-responsive-formation-522-1001`

522 weeks, seed 1001, ungated. 1960 is bit-identical to the run before it, confirming the
servo is inert while latent supply covers openings.

| metric vs control | before | after |
|---|---|---|
| `successfulReleases` 1969 | 0.512 | **0.899** |
| `scheduledAlbumProjects` 1967 / 68 / 69 | 0.657 / 0.562 / 0.514 | **0.921 / 0.879 / 0.904** |
| rostered artists 1969 | 950 | **1841** |
| Single releases 1969 | 1556 | **2725** (control 3045) |
| units per Single release 1969 | 87.8k | **54.6k** (control 38.1k) |

Units behaved as the pool-limited reading predicted: Single releases rose **75%** while
Single units rose **9%** (136.6M -> 148.7M). `totalUnits` moved 1.129 -> 1.206 at 1969 and
stayed in band; every revenue metric held (max 1.165, unchanged). More releases divide the
demand pool rather than adding to it, so restoring supply corrected the release-count error
without inflating the market.

`successfulReleases` is now in band for all ten years (minimum 0.859 at 1964).
`scheduledAlbumProjects` is in band from 1965 on.

## Open — the roster plateau

Rostered stabilizes at ~1841 against an aggregate operating target of ~3935: labels sit at
roughly half their own stated target, and disbandment *rose* (4816 against 4287), because
more acts formed means more acts dropped and destroyed on the same 130-week clock. The
terminal inactivity clock, not supply, is now the binding constraint. Careers stay short
and the population churns rather than accumulating.

Worth recording that the demand side was never the problem. Mean operating target per label
is flat and slightly falling across the decade (3.84 -> 3.39); the entire aggregate rise is
label count, **293 -> 1160**. Talent formation was being asked to chase a 4x growth in the
label population while pinned at 300/yr. Whether that label growth is itself governed is a
separate question and should be asked before formation is tuned again — otherwise formation
is being fitted against a moving target.

## Resolved — the control was carrying this directive's own mechanisms

`scheduledAlbumProjects` breached at 1963 (0.591) and 1964 (0.570) under the tight band. The
cause was ours. Three mechanisms introduced by this directive were applied **unconditionally**
in `EvaluateFormatDecision` and `CalculateAlbumPriorNet`, so the disabled control route was
carrying repairs authored for the route being measured against it. The capacity derivation
made it far worse than the old tier lookup: what had reached only Major labels now reached
every label.

The rule applied, after getting it wrong once:

> **Gate a mechanism to the live route only if it repairs a defect unique to the live
> route's architecture. Do not gate a correction to economics both routes share.**

| mechanism | gated? | why |
|---|---|---|
| `albumPortfolioCredit` | **yes** | Exists to stop the *lane split's* short-horizon memory abandoning Albums. The disabled route has no lane split, so no such defect. |
| `CalculateAlbumPriorEraCalibration` | **yes** | A calibration of the live Album prior against live realized outcomes. Applying it to the reference moves the yardstick by the amount it moves the measurement. |
| `promoSynergyGain` | **no** | `cannibalizationLoss` scales with `albumDemandFactor` while `expectedPromoLift` is a fixed scalar *on both routes*. The asymmetry is an error in the shared economics, not in the lane split. Only the absorbing state it interacts with is live-specific, and that is fixed separately in `ResolveAlbumDecision`. |

Gating all three was measured first and was wrong. It fixed `scheduledAlbumProjects`
everywhere but dropped the control's promo share to .366 by 1969; since a promo project
emits two products and a standalone emits one, the control lost the Singles those Album
projects carried — Single units fell 116M -> 99.3M and took `totalUnits` (1.398/1.419),
`grossRevenue`, `labelNet` and `marketNet` out of band at 1968-69. Restoring
`promoSynergyGain` recovered them without re-inflating Album share, because the two are
separable: the credit feeds `projectedAlbum` and therefore `albumWins`, while the synergy
gain feeds `promoAdvantage` and only sets `promoPreferred` on the disabled route.

Control Album decision share, across the three configurations:

| year | contaminated | all three gated | **shipped** | old d6 control |
|---|---|---|---|---|
| 1963 | .502 | .380 | **.380** | .424 |
| 1965 | .840 | .541 | **.541** | .551 |
| 1969 | .963 | .846 | **.846** | .849 |

The shipped control lands within a point or two of the historical d6 reference it replaces,
which is the outcome to expect once the contamination is removed.

**The band was never widened.** A per-metric widening to [0.50,2.00] was implemented,
measured and reverted by explicit decision before the real cause was found; every metric
remains at [0.70,1.30].

Worth recording for the directive's own history: `scheduledAlbumProjects` is the metric whose
0.644 abort opened this document. Part of the gap it was chasing was self-inflicted. The
portfolio-commitment work is still sound and is what fixed Album *share* — but the *count*
target it was measured against had been inflated by the same directive's mechanisms leaking
into the reference.

## Clean decade — `d7-portfolio-gated-decade-522-1001`

522 weeks, seed 1001, `--catastrophic-fail-fast` and `--strict-1965-acceptance-gate` armed
against `d7-portfolio-gated-decade-control-1001`. **Completes all 522 weeks with an empty
catastrophic-fail-fast file — no rows.** All six gate metrics in band for every completed
year:

| metric | range 1960-1969 |
|---|---|
| `scheduledAlbumProjects` | 0.757 - 1.160 |
| `successfulReleases` | 0.915 - 1.075 |
| `totalUnits` | 0.952 - 1.084 |
| `grossRevenue` | 0.987 - 1.115 |
| `labelNet` | 1.021 - 1.193 |
| `marketNet` | 1.000 - 1.154 |

Strict 1965 clears with margin: singleUnits .948, albumUnits 1.597, totalUnits .980,
grossRevenue 1.071, labelNet 1.137, marketNet 1.120, against floors of .85/.80/.85.

The enabled route is bit-identical to the ungated `d7-responsive-formation-522-1001` at week
400, confirming the gating changed only the control.

## Revised next steps, in order

1. **The roster plateau.** See `D7ArtistPopulationPlateauHandoff.md`, which carries the full
   diagnosis, the evidence, and the decisions already settled. It is the only substantive
   modelling defect left open by this directive. Everything below is a sub-item of it and is
   restated there.
2. **Give the terminal inactivity clock an opportunity model**, or extend the latent
   reservoir to experienced free agents. This is what the roster plateau needs. Note that
   `BuildLaborMarketSnapshot` currently counts a prospect status on an experienced artist
   as an integrity violation (`prospectStatusContractConflicts`), so extending the
   reservoir means revisiting that invariant rather than working around it. Expect it to
   push `successfulReleases` from 0.915-1.075 upward and to raise cost pressure on labels,
   so it needs its own measured pass rather than being stacked on the formation change.
3. **Ask whether label formation is governed** before tuning artist formation again. 293
   -> 1160 labels in a decade is what set the demand the servo is now answering.
4. **Leave the promo constants alone** unless the above changes the picture. See above.

## Tree state at this amendment

- `Data/Zeitgeist.cs` — cumulative sparse overrides plus authored decay for the 18 snap
  discontinuities. Affects the disabled route only.
- `Systems/ChartManager.cs` — `AddGenreMomentum` guard. No-op on the enabled route.
- `Systems/CompetitorManager.cs` — gates `albumPortfolioCredit` and
  `CalculateAlbumPriorEraCalibration` to the live route so the control stops carrying this
  directive's own repairs; `promoSynergyGain` stays ungated by explicit decision, see
  above. Also moves an orphaned `<summary>` off `CalculatePromoAlbumSynergyGain` and back
  onto `CalculateAlbumPortfolioCredit`, where it belongs. The enabled route is unchanged:
  verified bit-identical at week 400.
- `Systems/ArtistManager.cs` — demand-responsive formation
  (`CalculateResponsiveAnnualFormationTarget`), plus the annual-target parameter on
  `CalculateCalendarFormationCount`. Inert at 1960 by construction.
- `SimTools/ArtistPopulationLifecycleProbeSuite.cs` — extends
  `ProbeCalendarFormationQuota` to cover the responsive target: inert while the prospect
  market covers openings, monotone in unmet demand, bounded at 1200, and the calendar
  quota exact at the ceiling.
- `SimTools/ChartAuditRunner.cs` — **unchanged**. A per-metric band for
  `scheduledAlbumProjects` was implemented and reverted; see above.
- `SimTools/GenreMarketV2ProbeSuite.cs` — adds `ProbeLegacyZeitgeistContinuity`, and
  repairs a stale assertion that had left the suite **red at head**. The promo synergy
  probe required `CalculatePromoAlbumSynergyGain(albumDemand, 0f, ...) == 0f` — recruitment
  vanishing without awareness headroom — which is exactly the behaviour
  `PromoAwarenessConversionFloor` was introduced to remove. The two shipped together and
  the assertion was never updated. It now asserts the real contract: recruitment falls to
  the floor share (.25 of the full-headroom value, exactly) and never below it, and still
  vanishes without album demand. Both suites now pass:
  `d7-zeitgeist-repair-probecheck-52-1001`.
- **Gate control: `d7-portfolio-gated-decade-control-1001`.** Use this one.
  `d6-transition-envelope-decade-control-1001` is superseded and should not be used again —
  it could not have been regenerated at head, since the disabled route crashed in week 2.
  Two intermediate controls generated during this work are also superseded and kept only
  for the record: `d7-zeitgeist-repair-decade-control-1001` (carries the contamination) and
  `d7-uncontaminated-decade-control-1001` (over-gated, promo synergy removed).
- Reference runs: **`d7-portfolio-gated-decade-522-1001`** is the clean gated decade;
  `d7-responsive-formation-522-1001` is the same configuration ungated, for reading years a
  gated run would not reach. Probes: `d7-zeitgeist-repair-probecheck-52-1001`, re-verified
  after every subsequent change.
