# Directive 3D-R album-economy repair audit

> Historical checkpoint: Sign-Off #2 subsequently accepted M3, closed M1, and authorized M2. The final M2/acceptance result is recorded in `SimTools/DecadeRunValidationAudit.md`.

## Resumption checkpoint after formal sign-off

Measured 2026-07-05 with Godot 4.7 Mono, headless. Formal sign-off in `directive 3drs.doc` accepted the R.0B evidence, approved the `MarketRegion.cs` export scope and multi-year guard, struck the old catalog target, authorized M3 first, and made M1 conditional on end-1969 never-retired share remaining above 15% after M3.

### Decision

**M3 passes its six-seed checkpoint. M1 was authorized and tested, but its `<10%` never-retired target is unreachable inside the signed scope. Work stops before M2.** The failed M1 implementation was reverted; the passed M3 export and calibrated curve remain. No hold-out seed was run.

The sign-off is logically consistent except for one inherited-document dependency: it references Album-share soft bands without restating their numbers, while `Directive3D-Codex.md` is absent from this tree. This audit therefore reports every relevant Album share and competition ratio rather than silently inventing band limits.

### M3 implementation and equivalence

`MarketRegion` now exports one shared timing parameterization:

- `albumDemandRiseStartYear`
- `albumDemandRiseEndYear`

`GetAlbumDemandEraProgress` is the single source used by both `GetAlbumAffinity` and `GetAlbumPurchaseWillingness`. Existing consumers continue to read the same aggregate factor through `CalculateAlbumDemandFactor`; `GetYearEvolution`, `GetGenreAcceptance`, and `GetGenreMarketSize` are untouched.

With defaults set to the old 1960-1969 window, an enabled seed-1001 105-week run matched all **29** current CSV streams byte-for-byte. The calibrated window is **1964-1972**.

| Iteration | Start/end | Result |
|---:|---:|---|
| 1 | 1964 / 1969 | Removed the 1961 doubling; crossover 1966; zero standalone through 1963. Rejected because 1969 median expansion was not volume-adjudicable in both seeds. |
| 2 | 1963 / 1969 | Confirmed the pre-1964 trajectory and early guards; transition was unnecessarily abrupt. Stopped after the six-year probe. |
| 3 | 1964 / 1970 | Crossover 1966/1967; seed-1002 1969 competition still exceeded disabled during a +3 median expansion. |
| 4 | **1964 / 1972** | **Candidate:** crossover 1967 in all six seeds; standalone zero through 1963; all Pearson and early median gates pass; late +3 expansions occur with competition below disabled. |

### M3 six-seed checkpoint

Pearson values are paired to the frozen same-seed disabled decade. Median values use the lifecycle analyzer, not the calendar-edge annual-rollup fallback. “Late adjudication” lists years with a median delta above +2 and shows enabled competition ratio below disabled.

| Seed | Gross crossover | 1960-63 standalone max | Pearson decade mean / minimum delta | 1960-64 max median | Late adjudication | 1969 Album share | 1969 never retired |
|---:|---:|---:|---:|---:|---|---:|---:|
| 1001 | 1967 | 0.0% | +0.0830 / -0.0274 | 0.5 | 1968: 6.937 < 7.580; 1969: 7.535 < 7.886 | 82.6% | 37.2% |
| 1002 | 1967 | 0.0% | +0.1053 / -0.0318 | 1.0 | 1969: 7.480 < 7.886 | 83.0% | 36.9% |
| 1003 | 1967 | 0.0% | +0.1061 / +0.0125 | 1.0 | 1968: 6.770 < 7.810; 1969: 7.429 < 8.007 | 86.1% | 37.2% |
| 1004 | 1967 | 0.0% | +0.1116 / +0.0483 | 1.0 | 1968: 6.928 < 7.423; 1969: 7.335 < 7.570 | 85.2% | 36.1% |
| 1005 | 1967 | 0.0% | +0.0715 / +0.0202 | 1.0 | None required | 82.3% | 37.3% |
| 1006 | 1967 | 0.0% | +0.0818 / +0.0044 | 1.0 | 1968: 6.928 < 7.504; 1969: 7.398 < 7.669 | 82.8% | 37.1% |

Every seed passes the Pearson floor (`mean >= -0.02`, no year below `-0.06`), has no compression below -2, and stays within +/-1 during 1960-1964. The three M3 economic predictions move together: the Youth demand factor no longer doubles in 1961, market gross crosses during 1965-1969, and standalone share remains below 0.5% through 1963.

### M1 go/no-go and reachability result

All six M3 seeds remain above the signed 15% go/no-go threshold, so M1 was authorized. Three two-seed settings were tested:

| Iteration | Weekly decay / floor | 1969 never retired (1001 / 1002) | Live age P50 (1001 / 1002) | >52-week gross | Crossover | Result |
|---:|---:|---:|---:|---:|---:|---|
| 5 | 0.96 / 5% | 28.6% / 29.7% | 44 / 45 | 8.2% / 9.3% | 1968 | Level metrics improve; `<10%` retirement target fails. |
| 6 | 0.94 / 1% | 22.7% / 23.4% | 33 / 34 | 2.6% / 2.8% | 1969 | Retirement target still fails. |
| 7 | 0.80 / 0% | 14.6% / 15.7% | 22 / 22 | approximately 0% | None by 1969 | Strongest authorized decay still misses `<10%` and now breaks crossover. |

The target conflict is structural. `ChartManager` freezes `albumChartedToleranceWeeks = 52`; charted Albums cannot retire until both chart and under-floor clocks satisfy that horizon. At iteration 7, 27,757/26,210 Albums had ever released and 4,064/4,103 remained active. The 1969 release cohort alone is large enough to keep the never-retired ratio near or above the measured 14.6%-15.7% lower bound. Stronger demand decay cannot shorten the frozen retirement clock, and already eliminates the required gross crossover.

Accordingly, M1 cannot reach `<10%` without either changing the explicitly frozen retirement rule or replacing the cumulative never-retired denominator with an age-eligible cohort metric. No such scope expansion is authorized. The M1 code was removed, and M2 was not started because the authorized order places it after M1 and the standard stop condition has fired.

### Final validation and handoff

| Check | Result |
|---|---|
| Final M3 seed-1001 decade repeat | **Pass:** all 29 deterministic CSV streams byte-identical |
| Disabled 1960 Single units | **Pass:** `154,810,982` |
| Disabled `market-revenue.csv` | **Pass:** `765897841BCB1C62225EF2A7861CF46ACC282FE206A10BADCF895330612A3866` |
| Disabled `release-capacity.csv` | **Pass:** `8B84BBFDBA5F8B38729F29A1FDF14F8F7598A7AF18DCBAD053BC6C8238A9A461` |
| Build | **Pass:** 0 errors; one pre-existing unused-event warning |
| Hold-outs | **Untouched:** no seed 2001-2003 run |

Decision required to resume: either (a) revise M1 success to an age-eligible retirement metric, (b) explicitly unfreeze the Album retirement tolerance, or (c) accept M3 alone and authorize M2 while leaving M1 deferred. Nothing in this checkpoint resolves that choice on Alice's behalf.

---

## Phase 3D-R.0B attribution checkpoint

Measured 2026-07-05 with Godot 4.7 Mono, headless, enabled seeds 1001/1002. The 1960-1961 runs captured every economic decision; the decade-wide catalog checks reuse the completed Phase 0 runs.

### 1. Decision

**Pass for the four R.0B evidence tasks; still blocked for M1-M3.** No calibration constant, economic formula, decision branch, or RNG call/order changed. No hold-out seed was run.

The reopened attribution resolves the 1961 affinity increase: `CalculateAlbumDemandFactor` accounts for more than the full increase on a one-factor-at-a-time basis, while quality, stature, and reach offset it. The hit-score decline is not new-entrant dilution. The old 1962 catalog-gross dominance target remains unsupported, so this report submits a replacement target for Alice rather than authorizing repair work.

### 2. Task 1: Youth affinity attribution, 1960 to 1961

All Youth economic calls were logged. To reconcile the directive's “every call” requirement with its prescribed checkpoint means, the attribution population below is the Youth Album-decision subset used by Phase 0 (266 to 1,303 decisions for seed 1001; 251 to 1,296 for seed 1002). Contributions replace one 1960 factor mean at a time with its 1961 mean while holding the other three at their 1960 means. The residual contains interactions and covariance because a product of marginal means is not the mean row-level product.

| Seed | Factor | 1960 mean | 1961 mean | Independent units | Share of actual growth |
|---:|---|---:|---:|---:|---:|
| 1001 | Quality estimate | 0.585592 | 0.562269 | -99.5 | -5.6% |
| 1001 | Stature multiplier | 1.081579 | 1.041059 | -93.6 | -5.3% |
| 1001 | Reach factor | 0.618195 | 0.559158 | -238.7 | -13.5% |
| 1001 | Album demand factor | 0.036473 | 0.072376 | +2,460.1 | +139.3% |
| 1001 | Interaction/covariance residual | — | — | -261.9 | -14.8% |
| 1001 | **Actual mean affinity units** | **2,527.1** | **4,293.5** | **+1,766.3** | **100%** |
| 1002 | Quality estimate | 0.603915 | 0.571514 | -127.1 | -6.6% |
| 1002 | Stature multiplier | 1.090438 | 1.052315 | -82.8 | -4.3% |
| 1002 | Reach factor | 0.567162 | 0.547366 | -82.7 | -4.3% |
| 1002 | Album demand factor | 0.036249 | 0.072486 | +2,368.6 | +122.2% |
| 1002 | Interaction/covariance residual | — | — | -137.3 | -7.1% |
| 1002 | **Actual mean affinity units** | **2,397.3** | **4,335.9** | **+1,938.6** | **100%** |

The standalone demand factor nearly doubles and is the only positive material driver. Quality, mean stature, and reach all fall in the Album-decision population. Decisions for artists observed to have a career-state transition in that calendar year increase from 33.5% to 56.3% (seed 1001) and 35.1% to 51.5% (seed 1002), but population selection leaves mean stature lower, not higher. This makes the outstanding `MarketRegion.cs` demand-timing/export decision directly material to the 1961 jump.

Static disposition: `CalculateCompilationCostWeight` is exactly `1f` for all five Youth genres in every observed year, so it is structurally incapable of causing the 1961 Youth timing change. `CalculateAlbumDemandFactor` reduces to Album affinity multiplied by purchase willingness after the shared market-size terms cancel. These facts supersede the two falsified Phase 0 causal confirmations and should not be re-tested as empirical hypotheses.

### 3. Task 2: hit-inventory cohort composition

Youth Album decisions, matching the Phase 0 aggregate. “Carryover” means at least one resolved released Single has a release year before the observation year.

| Seed | Year | Cohort | Decisions | Mean hit score |
|---:|---:|---|---:|---:|
| 1001 | 1960 | Carryover | 102 | 0.6112 |
| 1001 | 1960 | New entrant | 164 | 0.5531 |
| 1001 | 1961 | Carryover | 1,220 | 0.3037 |
| 1001 | 1961 | New entrant | 83 | 0.1520 |
| 1002 | 1960 | Carryover | 102 | 0.6597 |
| 1002 | 1960 | New entrant | 149 | 0.5420 |
| 1002 | 1961 | Carryover | 1,226 | 0.3707 |
| 1002 | 1961 | New entrant | 70 | 0.1492 |

The decline is not dilution by a growing zero-history cohort. New entrants fall from 61.7%/59.4% of Album decisions in 1960 to 6.4%/5.4% in 1961. Mean hit score falls sharply within both cohorts, including carryover artists. The Phase 0 hit-term decline therefore remains a within-cohort and Album-selection effect, not an influx of new entrants.

### 4. Task 3: catalog timing target

The arithmetic uses each observed Album release week. An Album is eligible for the existing old-catalog bucket only when its age is strictly greater than 52 weeks at the annual endpoint.

| Seed | Year | New Albums | Cumulative | >52-week eligible | Eligible share | Cumulative age P50/P90 | >52-week gross share |
|---:|---:|---:|---:|---:|---:|---:|---:|
| 1001 | 1961 | 2,592 | 3,663 | 1,043 | 28.5% | 34 / 82 | 7.3% |
| 1001 | 1962 | 4,410 | 8,073 | 3,617 | 44.8% | 46 / 115 | 14.8% |
| 1001 | 1963 | 5,131 | 13,204 | 7,987 | 60.5% | 68 / 148 | 20.7% |
| 1001 | 1964 | 5,638 | 18,842 | 13,102 | 69.5% | 90 / 188 | 23.7% |
| 1001 | 1965 | 5,958 | 24,800 | 18,842 | 76.0% | 113 / 230 | 24.2% |
| 1002 | 1961 | 2,473 | 3,408 | 915 | 26.8% | 33 / 79 | 6.4% |
| 1002 | 1962 | 4,163 | 7,571 | 3,359 | 44.4% | 45 / 112 | 14.7% |
| 1002 | 1963 | 4,930 | 12,501 | 7,476 | 59.8% | 68 / 147 | 20.7% |
| 1002 | 1964 | 5,541 | 18,042 | 12,403 | 68.7% | 87 / 188 | 24.1% |
| 1002 | 1965 | 5,856 | 23,898 | 18,042 | 75.5% | 110 / 229 | 25.3% |

Only 44%–45% of cumulative Albums can be in the >52-week cohort in 1962; the eligible count first becomes a majority in **1963**. Gross does not follow count dominance: >52-week gross peaks in **1965** at 24.2%/25.3%, then falls to 17.1%/16.4% by 1969 even though 87% of cumulative releases are eligible.

**Candidate replacement target for Alice:** replace “>52-week catalog gross dominant by 1962” with “>52-week-eligible Albums become a majority of cumulative releases by 1963, and their gross contribution peaks near 25% by 1965.” This is a feasibility-and-contribution target, not a claim of gross dominance. If gross majority remains the intended metric, the evidence supports no dominance year in 1960-1969 and the target must be removed or independently modeled.

### 5. Task 4: adult format-selection cliff

| Seed | Year | Adult compilations | Adult standards | Compilation share | Source-hit age mean / median |
|---:|---:|---:|---:|---:|---:|
| 1001 | 1962 | 560 | 446 | 55.7% | 52.0 / 48.0 |
| 1001 | 1963 | 606 | 455 | 57.1% | 58.9 / 53.0 |
| 1001 | 1964 | 52 | 909 | 5.4% | 49.0 / 45.0 |
| 1001 | 1965 | 0 | 1,086 | 0.0% | — |
| 1002 | 1962 | 482 | 404 | 54.4% | 51.7 / 47.0 |
| 1002 | 1963 | 545 | 466 | 53.9% | 57.8 / 51.0 |
| 1002 | 1964 | 46 | 886 | 4.9% | 53.0 / 46.5 |
| 1002 | 1965 | 0 | 1,006 | 0.0% | — |

The independent format boundary is plainly visible: Adult compilation share falls by about 50 percentage points in 1964 and reaches zero in 1965. The small 1964 tail is consistent with projects whose format was selected before the boundary and released after a scheduled delay. Source-hit age falls modestly, but the overall >52-week Album-gross series remains smooth from 1963 to 1965 (20.7% to 23.7%/24.1% to 24.2%/25.3%). Thus the format cliff changes Adult compilation composition without producing a matching discontinuity in total catalog gross.

This `GenerateAlbum` format roll is a separate use of the 1963/1964 boundary from the economic cost weighting. Neither path applies its Adult-only cliff to Youth genres.

### 6. Validation

`ChartAuditRunner.cs` is in the 3D-P guarded set, so the required disabled checks were rerun.

| Check | Result |
|---|---|
| Disabled seed 1001, 1960 Single units | **Pass:** `154,810,982` exact |
| Disabled `market-revenue.csv` SHA-256 | **Pass:** `765897841BCB1C62225EF2A7861CF46ACC282FE206A10BADCF895330612A3866` |
| Disabled `release-capacity.csv` SHA-256 | **Pass:** `8B84BBFDBA5F8B38729F29A1FDF14F8F7598A7AF18DCBAD053BC6C8238A9A461` |
| Build | **Pass:** 0 errors; one pre-existing unused-event warning |
| Seeds | **Pass:** enabled 1001/1002 only; no 2001+ run |
| Calibration/RNG | **Pass:** no exported constant, formula, simulation branch, or RNG call/order changed |

### 7. Corrections to this directive

| # | Severity | Finding |
|---|---|---|
| 1 | **High** | The proposed new-entrant dilution explanation is reversed by the data. New entrants become a much smaller share of Youth Album decisions in 1961, and hit score falls within carryover and new-entrant cohorts alike. |
| 2 | **Medium** | A majority of releases being old enough does not imply gross dominance. Eligibility crosses 50% in 1963, but >52-week gross peaks near 25% in 1965 and then declines. No gross-dominance year can be derived from this decade. |
| 3 | **Medium** | Career transitions become more prevalent among Album decisions, but mean stature falls. The directive's proposed 1.0-to-1.2 step is real at artist level but does not explain the aggregate growth because the decision population changes. |
| 4 | **Low** | The existing cohort implementation is `weeksSinceRelease > 52`, not `>= 52`; this report therefore labels it “>52 weeks” rather than “52+ weeks.” |
| 5 | **Informational** | The 1963/1964 format boundary appears in release-year output as a large 1964 residual rather than an instantaneous zero because scheduled projects can cross the calendar boundary; it reaches zero in 1965. |

### 8. Outstanding Alice sign-offs

All three decision points remain unresolved:

1. **`MarketRegion.cs` export scope:** approve, reject, or revise the proposed export boundary for the hardcoded demand-timing curve. R.0B shows this curve is the dominant 1961 affinity driver but does not decide its calibration.
2. **Multi-year Pearson/median guard form:** approve the exact multi-year Pearson and volume-aware median adjudication form before any calibration ladder or hold-out use.
3. **Catalog target replacement:** approve, reject, or revise the candidate “eligible-count majority by 1963 / approximately 25% peak gross contribution by 1965” target before it replaces the old 1962 gross-dominance requirement in any future M1.

---

## Original Phase 0 checkpoint

Measured 2026-07-05 with Godot 4.7 Mono, headless, enabled seeds 1001/1002.

## Decision

**Stopped at the Phase 0 gate. M1-M3 were not implemented and calibration/hold-out runs were not started.**

Two prescribed causal confirmations failed on both seeds:

1. The 1961 youth Album-decision jump is not caused by an increase in the hit-inventory term. Mean youth `weightedHitUnits` falls from 11,508 to 5,882 in seed 1001 and from 11,796 to 7,174 in seed 1002 while youth Album share rises from roughly 12% to 51%. Hit inventory remains an important level term, but it does not explain the timing of the jump as written.
2. Catalog revenue is substantial but is not dominant by 1962. Albums older than 26 weeks contribute 42.56%/42.72% of 1962 Album gross; albums older than 52 weeks contribute 14.80%/14.71%. This misses the directive's expected dominant-by-1962 attribution.

The directive explicitly requires stopping before repair work if any mechanism confirmation fails. The two Alice sign-offs also remain unresolved: no written authorization was found for the narrowed `MarketRegion.cs` export scope or for volume-aware median adjudication.

## Phase 0 results

### 1. Prior-term decomposition

Album decisions only; contribution share is `weightedHitUnits / (affinityUnits + weightedHitUnits)`.

| Seed | Year | Group | Decisions | Mean affinity units | Mean weighted-hit units | Hit share |
|---:|---:|---|---:|---:|---:|---:|
| 1001 | 1960 | Adult | 578 | 6,367 | 1,279 | 16.7% |
| 1001 | 1960 | Youth | 266 | 2,527 | 11,508 | 82.0% |
| 1001 | 1961 | Adult | 888 | 10,077 | 3,287 | 24.6% |
| 1001 | 1961 | Youth | 1,303 | 4,294 | 5,882 | 57.8% |
| 1002 | 1960 | Adult | 502 | 6,302 | 1,221 | 16.2% |
| 1002 | 1960 | Youth | 251 | 2,397 | 11,796 | 83.1% |
| 1002 | 1961 | Adult | 811 | 10,481 | 3,046 | 22.5% |
| 1002 | 1961 | Youth | 1,296 | 4,336 | 7,174 | 62.3% |

Finding: youth decisions are hit-heavy in 1960-1961, but the hit term declines during the jump. Demand/affinity and decision-blend timing need separate attribution before M2 can be claimed as the repair for that jump.

### 2. Memory-versus-prior blend

`confidence > 0.5` is counted as memory-led. The analyzer also emits this split for every label tier and format; the aggregate timing is:

| Seed | Year | Single mean / memory-led | Album mean / memory-led |
|---:|---:|---:|---:|
| 1001 | 1960 | .239 / 20.3% | .010 / 0.1% |
| 1001 | 1961 | .691 / 84.7% | .120 / 6.3% |
| 1001 | 1962 | .768 / 90.9% | .300 / 27.4% |
| 1001 | 1963 | .784 / 91.9% | .503 / 54.9% |
| 1002 | 1960 | .247 / 21.8% | .013 / 0.2% |
| 1002 | 1961 | .715 / 87.6% | .119 / 6.1% |
| 1002 | 1962 | .782 / 91.9% | .276 / 22.3% |
| 1002 | 1963 | .794 / 91.8% | .477 / 51.1% |

Finding: Album decisions remain prior-led through 1962 and cross the operational memory-led threshold around 1963. This confirms when the prior is influential, but not the proposed hit-inventory explanation for the 1961 jump.

### 3. Album gross by age cohort

All Albums use one retail price, so weekly unit share and retail-gross share are identical.

| Year | Seed 1001 >26 / >52 weeks | Seed 1002 >26 / >52 weeks |
|---:|---:|---:|
| 1960 | 16.62% / 0.00% | 15.82% / 0.00% |
| 1961 | 35.79% / 7.34% | 35.46% / 6.43% |
| 1962 | 42.56% / 14.80% | 42.72% / 14.71% |
| 1963 | 48.16% / 20.67% | 48.08% / 20.74% |
| 1964 | 49.64% / 23.72% | 50.00% / 24.14% |
| 1965 | 51.61% / 24.23% | 52.15% / 25.34% |
| 1969 | 41.61% / 17.09% | 40.86% / 16.35% |

Finding: the catalog tail materially amplifies Album revenue, but it becomes a majority only around 1964-1965, after the observed 1963 crossover. The expected 70%+ old-catalog figure is not present under either threshold.

### 4. Compilation source-hit age

| Seed | Year | References | Mean weeks | Median weeks | Maximum weeks |
|---:|---:|---:|---:|---:|---:|
| 1001 | 1960 | 1,506 | 17.3 | 6 | 70 |
| 1001 | 1963 | 21,518 | 51.4 | 47 | 225 |
| 1001 | 1966 | 19,911 | 87.2 | 68 | 378 |
| 1001 | 1969 | 17,968 | 134.5 | 80 | 527 |
| 1002 | 1960 | 1,367 | 18.1 | 6 | 68 |
| 1002 | 1963 | 20,622 | 50.6 | 47 | 223 |
| 1002 | 1966 | 19,233 | 91.3 | 72 | 383 |
| 1002 | 1969 | 17,446 | 139.9 | 88 | 523 |

Finding: source hits age strongly and continue to be referenced for many years. This supports a recency mechanism in isolation, subject to resolving the failed jump attribution above.

## Instrumentation and structural checks

- `AlbumTrack.releaseDate` is a plain, non-exported absolute `GameDate`, populated by `ChartManager.CreateTrackSnapshot`.
- `album-track-links.csv` now reports `sourceHitAgeWeeks` at Album release observation time.
- The annual rollup now reports Album gross shares for the >26-week and >52-week cohorts.
- `analyze-3d.mjs` reports all four Phase 0 diagnostics, including confidence by year/tier/format.
- Disabled seed 1001, 52 weeks: `154,810,982` Single units exact.
- Disabled `market-revenue.csv`: `765897841BCB1C62225EF2A7861CF46ACC282FE206A10BADCF895330612A3866` exact.
- Disabled `release-capacity.csv`: `8B84BBFDBA5F8B38729F29A1FDF14F8F7598A7AF18DCBAD053BC6C8238A9A461` exact.
- Build succeeds. The sole compiler warning is the pre-existing unused `ChartManager.OnGenreMomentumChanged` event.

## Required direction before resuming

Alice must (a) decide whether a large-but-sub-majority catalog tail is sufficient to authorize M1, (b) revise or waive the requirement that hit inventory explain the 1961 youth jump before M2, and (c) provide both written decision-point sign-offs from Directive 3D-R. No hold-out seed has been consumed.
