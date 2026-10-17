# Regional survival and chart investigation — 2026-10-16

The goal is a viable regional economy with ordinary business failure, not universal Small-label survival or a mortality rate copied from control. The handoff's proposed solutions and acceptance bands are historical context. The user's later direction explicitly rejects sales-triggered distribution and authorizes replacing artificial label-exit penalties with economic pressure.

## Historical limits and the survival denominator

Near-universal two-year closure is not an appropriate design assumption for every regional business. Smithsonian's [Arhoolie history](https://folkways.si.edu/arhoolie) describes a label founded in 1960 with a 250-copy first LP and a long subsequent life recording regional music. This establishes that enduring small-scale operation was possible. It does **not** provide a representative two-year survival rate: prominent survivors are a selected sample. No defensible cohort estimate for comparable US labels in this period was found in this investigation. Neither 44% nor any replacement percentage should be described as historically validated.

Track labels present at the start separately from later entrants. The starting cohort has 104 weeks of potential follow-up; late entrants do not. A promotion out of Small is a surviving business, not a closure. These are two years of observation, not necessarily the first two years since a historical firm's founding.

Before the distribution-access correction below, the recovered regional model gave:

| Starting Small cohort | Seed 1001 | Seed 2002 | Seed 3003 | Total |
|---|---:|---:|---:|---:|
| Initial labels | 231 | 230 | 232 | 693 |
| Closed | 96 | 102 | 104 | 302 (43.58%) |
| Still Small | 108 | 93 | 95 | 296 (42.71%) |
| Surviving as Independent | 27 | 35 | 33 | 95 (13.71%) |
| Closures called Bankruptcy | 31 | 39 | 38 | 108 |
| Competitive exits | 65 | 63 | 66 | 194 |

Control closed 301 of the same 693 starting labels. That similarity is a regression observation, not evidence that either mortality level is realistic. The regional outcome is plainly not near-universal failure. New entrants and labels promoted after launch are no longer allowed to distort this denominator.

## What the old closure rules actually represented

`CompetitorManager.UpdateLabelStatus` increments a loss counter after a negative month but only subtracts one after a positive month. It was not a strict consecutive-loss streak. A funded label can become Dying from that accumulated history.

`LabelLifecycleManager.CheckForDeath` then gives a Dying Small label a 75% monthly closure chance after the counter exceeds 12, regardless of remaining cash. There is also a separate quarterly competition roll, primarily keyed to zero/one national charting records; profit only reduces its probability. Two national charting records provide a complete safe harbor. These rules can close a funded local business and protect a charting one independently of its actual ability to pay bills.

In the three pre-fix regional runs, 92 of 108 starting Small labels recorded as bankrupt had positive cash at the closure-week census. Most had little recent record income; this does not establish profitability. It establishes that the log's old Bankruptcy category cannot be read as proven cash insolvency. Cash/runway, production spend, collections and owner exit were being conflated.

## Distribution-access correction

A waiting Single now recognizes distribution the label has actually acquired for that record since launch. The previous weekly arrival logic checked chart/local proof but never rechecked existing owned coverage, independent wholesale placement or scoped distribution contracts. Contract coverage is attached in `TrackRelease`, after launch promotion, so a contracted release could remain held despite its actual shipping access.

`TryArrive` now checks `HasDistributionInRegionForRecord`. It does not grant a new line or put an excluded catalog title into a contract. Repeated promotion seeding also clears stale held state in served regions. The removed backorder threshold remains removed: even one million backorders cannot manufacture access.

Completed 104-week comparisons, with the original exit rules retained:

| Metric | Control 1001 | Corrected regional 1001 | Control 3003 | Corrected regional 3003 |
|---|---:|---:|---:|---:|
| Initial Small labels | 231 | 231 | 232 | 232 |
| Initial Small closures | 96 | 87 (37.66%) | 105 | 92 (39.66%) |
| Initial Small bankruptcies | 34 | 34 | 31 | 30 |
| Initial Small competitive exits | 62 | 53 | 74 | 62 |
| Initial Small survivors | 135 | 144 | 127 | 140 |
| Surviving as Small / Independent | 110 / 25 | 113 / 31 | 93 / 34 | 99 / 41 |
| National Single units | 307.22M | 305.48M (-0.57%) | 313.22M | 311.39M (-0.59%) |
| Small seller median Single net | $31,744 | $34,185 (+7.69%) | $27,416 | $33,140 (+20.88%) |
| Small-cohort total Single net | $9.158M | $8.824M (-3.64%) | $9.051M | $9.846M (+8.79%) |
| Small seller bankruptcies | 9 | 7 | 10 | 3 |

Revenue rows use first-observed-tier cohorts including runtime entrants; survival rows use only the starting cohort. This keeps successful promotions in the original Small economic cohort without treating late entrants as full two-year survivors. Both corrected runs have zero regional/national unit mismatches, invalid inventory movements or duplicate/missing settlement bookings. The distribution fix is a correctness change; it does not change mortality probabilities.

## Measurement repairs and chart findings

The settlement timing tool mixed Albums into results labelled as Singles. It now joins the national ledger's explicit format and filters to Singles. Markets with zero sales or zero demand have no peak or arrival. The weekly change decomposition now excludes gaps rather than comparing nonconsecutive weeks as a single-week movement.

The old airplay share used `1 - units / published chart points`. Published points include multiplicative survey noise, so that residual is not pure airplay. The audit now logs the cached survey sample and points before that multiplier. The guard uses `1 - units / unsampled points`. The old quantity is retained under `sampledPointResidualTop40`; old logs without the necessary data report missing airplay share, not zero.

The telemetry change is observational: two exact 52-week seed-1001 replays match all preexisting record fields, label-finance rows and national settlement rows to the corresponding old control/regional prefixes, with zero differences.

With the distribution fix, the corrected all-Single settled median peak spread is 2 weeks on seeds 1001 and 3003; 3.76% / 4.03% of eligible records have peaks at least 8 weeks apart, and top-11 regional overlap is 86.94% / 88.27%. The original all-population timing/overlap targets still miss.

A separate sensitivity excludes prewarm titles and releases within 26 weeks of the run end, then stratifies by first-observed launch footprint. Regional launches have median 2 / 3 weeks and 7.44% / 8.32% at least 8 weeks apart; nationwide launches have median 1 week and 0% at least 8 weeks apart. Restricting each market to at least 1% of a record's sales preserves the regional results at 2 / 3 weeks and 6.85% / 7.92%. Small regional-launch subsets have median 3.5 / 4 weeks, but only 30 / 42 records qualify. These are diagnostics, not a replacement for the declared all-population target. First-observed coverage can already include launch-week placements.

The new distribution fix retains 36.29% / 35.21% local share of weekly demand changes on the full-positive-market decomposition sample. Actual sales remain regional settlements summed nationally.

Airplay remains a real calibration issue. Corrected regional airplay share is 14.91% versus 8.14% control in seed 1001's first year. Seed 3003 is 11.81% versus 8.73%, then 9.13% versus 7.71% in its second year. Seed 1001's second-year baseline lacks unsampled telemetry, so a true airplay delta is unavailable there.

An off-by-default `--regional-radio-build-clock` experiment makes station rotation build from the market's own servicing age. It was tested for 52 weeks on seeds 1001 and 3003, alongside the distribution fix. It reduces first-year airplay from 14.91% to 14.11% on 1001, but increases it from 11.81% to 13.63% on 3003. Both still exceed the +3-point guard against control, and Major-family chart share moves farther from control. It is not accepted as the calibration solution and remains disabled.

## Replacement for artificial exits

Following the user's explicit authorization, `EconomicLabelLifecycle` replaces the two mortality rolls on the enabled artist-population / Genre Market V2 path. It is the default on that path; `--legacy-label-exits` retains the old rules for controlled comparisons, and `--economic-label-exits` explicitly selects the new policy. The regional arrival/campaign system remains independently opt-in. The new policy does not rely on a preferred label count, tier market share, chart count, or label age to close a firm.

- Complete each month's existing cash accounting, including overhead, production, signing costs, wholesale collections and distribution reinvestment. Keep the last three completed net cash-flow observations.
- Forecast burn from that short history. New signings reserve the greater of two months' overhead or three months' observed burn; new production reserves the greater of one month's overhead or two months' burn. Less surplus means fewer affordable commitments and, where possible, a smaller marketing budget. No money is created and no existing contract is forcibly terminated.
- Assess status from cash and forecast runway. A funded losing label keeps its runway; a profitable local label needs no national hit to remain active. A profitable month resets the actual consecutive-loss counter rather than paying down a historical penalty score.
- Negative cash means obligations have exceeded available funds. Allow one monthly review for existing releases and receivables to recover the balance. If the next monthly review is also negative, close the firm for unpaid bills. The grace is a model assumption, not a historically measured credit term. Uncollected receivables are not treated as cash.
- Apply the same insolvency condition to every AI tier. Honor the existing bankruptcy setting and retain the player's separate credit-line rules. Persist cash-flow history and missed-payment state, so save/load cannot reset distress.

Existing competition for finite regional purchases, shelf capacity, artists and distribution still determines earnings. Existing overhead and production costs still consume resources. Poor releases can lose money; a firm that cannot cover its costs can still fail. Market share is measured as an outcome rather than enforced through a mortality quota.

This policy models financial failure. It does not substitute a new lottery for founder retirement, voluntary withdrawal or an imprint becoming dormant; those distinctions would require their own business/owner state. Existing acquisition and consolidation paths remain. Consequently its financial closure rate is not directly interchangeable with a historical rate of discontinued label names. The cost, funding and payment-risk assumptions still need to be judged on their merits if financial attrition feels too gentle.

### Validation and two-year results

Build and fixed financial checks pass. The checks cover funded losses, profitable chartless local firms, commitment contraction, recovery, unpaid bills across all tiers, bankruptcy settings, player boundaries, old-save defaults, payment-grace persistence and zero RNG consumption by financial reviews. A complete live world also passes its 13-week save/load roundtrip, including compressed disk I/O, with the new cash-flow history populated.

Both detailed 104-week runs completed. The matched comparison here retains the acquired-distribution correction in both arms and changes the economic survival policy as a whole (status, commitment budgets and exit rules). Removing the old RNG-based death checks changes subsequent simulated decisions; individual outcomes are not a one-variable causal experiment.

| Metric, starting Small cohort | Legacy 1001 | Economic 1001 | Legacy 3003 | Economic 3003 |
|---|---:|---:|---:|---:|
| Initial labels | 231 | 231 | 232 | 232 |
| Closed within 104 weeks | 87 (37.66%) | 25 (10.82%) | 92 (39.66%) | 30 (12.93%) |
| Survivors | 144 | 206 | 140 | 202 |
| Still Small | 113 | 176 | 99 | 169 |
| Surviving as Independent | 31 | 30 | 41 | 33 |

The two new runs close 29 / 38 labels across all starting tiers and later entrants. Every closure has a matching insolvency event with negative cash and two missed monthly reviews; there are no competition exits or invalid insolvency events. The second year ends with 155 / 154 surviving starting-Small businesses having positive recent net cash flow and no national Single-chart appearance in the preceding 52 weeks. Missing the Single chart is no longer treated as proof of business failure. This statistic alone does not identify a firm's geographic sales concentration or rule out success on the Album chart.

National all-format units move only -0.087% / -0.021% over the two-year horizon versus the matched legacy regional economy. At the end of the second year, Small-tier unit share is 6.20% / 6.83% versus 5.94% / 6.20%; Major-tier unit share is 22.26% / 20.30% versus 21.69% / 19.22%. Tier unit share and Major-family chart share are different measures: subsidiaries and distribution relationships matter to the latter. More surviving firms have not created proportional national volume or an overwhelming Small sector.

Major-family chart shares in the new policy are 51.12% / 56.14% (1001, years 1 / 2) and 50.13% / 48.61% (3003). Against the original pre-arrival control, those changes are -0.49 / +1.18 / +3.11 / +1.00 points. The seed-3003 first-year observation remains just outside the original +/-3-point band. Against the immediately preceding regional build, second-year chart shares move by -3.91 / +5.27 points, so the survival policy is not chart-neutral.

The original pre-arrival control also has the old mortality policy. Those historical-control deltas compare the combined changes; they do not isolate geography while holding the new financial policy constant. The direct legacy/economic regional pair is the relevant test of the survival replacement.

The corrected airplay shares are 11.93% / 13.70% (1001) and 9.83% / 10.66% (3003). Against instrumented pre-arrival control, the available deltas are +3.79 points (1001 year 1), +1.10 / +2.95 points (3003). Thus the first-seed airplay calibration is still unresolved; no change in mortality criteria is used to declare it solved.

A 52-week explicit legacy replay exactly reproduces the old regional build on 120,277 record rows, 31,370 finance rows, 178,004 national settlements, 1,246,028 regional settlements and 841,939 demand-stage rows across all preexisting columns. Both new two-year runs have zero national/regional unit mismatches, invalid regional inventory movements or invalid settlement booking counts.

Regional behavior persists under the new survival policy: local demand accounts for 36.84% / 35.47% of weekly demand changes on the full-positive-market sample. All-Single settled peak spread remains a 2-week median, with 3.08% / 3.73% at least 8 weeks apart and 87.52% / 88.42% top-11 overlap. The broad timing targets still miss; they have not been relaxed to claim acceptance.

### Completed five-year holdout

Both matched seed-2002 arms completed 260 weeks from assembly `653C718906FE5D425DDDCC5E587834EB794DF1A85A0DA8208DB62FC6946A6B52`, with empty engine-error logs and zero national/regional unit, inventory or booking mismatches.

| Metric | Legacy exits | Economic policy |
|---|---:|---:|
| Starting Small labels | 230 | 230 |
| Starting Small closures after 2 years | 96 (41.74%) | 12 (5.22%) |
| Starting Small closures after 5 years | 147 (63.91%) | 14 (6.09%) |
| Starting Small still Small after 5 years | 27 | 105 |
| Starting Small surviving as Independent after 5 years | 56 | 111 |
| Active labels across the whole world at year 5 | 584 | 645 |
| All closures, including other tiers and later entrants | 339 | 27 |
| Competitive exits | 252 | 0 |
| Year-5 Small-tier unit share, all formats | 2.38% | 3.24% |
| Year-5 Major-tier unit share, all formats | 19.41% | 19.72% |
| Year-5 Major-family chart share | 53.05% | 52.74% |

Five-year national all-format units change by **+0.033%** cumulatively. Annual unit deltas are -0.196%, -0.022%, +0.061%, +0.234%, +0.051%; annual gross deltas range from -1.15% to +1.62%. All 27 new-policy closures have valid insolvency evidence. The legacy cash-flow-history columns are unpopulated; the analysis marks them unavailable rather than interpreting their zero defaults as measured zero income.

Small-tier unit share under the new policy still falls from 7.55% in year 1 (8.31% in year 2) to 3.24% in year 5. Companies can remain viable while losing economic weight, and successful firms can graduate out of Small. Market-share evolution does not require killing those businesses. The close year-5 Major-family shares do not erase annual variation: the policy shifts that measure by -4.67 / +5.78 / -1.00 / +1.26 / -0.30 points across the five matched years.

Across all three seeds, **67 of 693 starting Small labels close in the first two years (9.67%)**, versus 275 (39.68%) under matched legacy exits with the distribution correction. New-policy survivors comprise 510 still Small and 116 promoted Independents. The seed range is 5.22-12.93%, not a guaranteed success rate.

The five-year survival result is also a calibration warning: only 6.09% of that starting Small cohort closes, and just two more close after year 2. This shows that the current funding, earnings and cost assumptions can make surviving businesses very durable once artificial exits are removed. It is not evidence that a 94% five-year survival rate is historically correct. If additional financial attrition is warranted, investigate actual loss-making projects, capital at risk, working-capital delays and operating commitments. Do not use a preferred failure percentage or national-chart safe harbor as a substitute. Voluntary imprint discontinuation remains a distinct unmodeled outcome.

The replacement is implemented and validated as a financial decision system. Historical mortality calibration, the remaining airplay/timing misses, and a full-decade holdout are not claimed complete.

### Reproduction

Use the existing headless `ChartAuditRunner.tscn` with the following user arguments (the 2002 holdout substitutes `--weeks=260 --seed=2002`):

```text
--weeks=104 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle
--local-signals-read-demand --regional-arrival --regional-retail-access
--regional-campaign-focus --economic-label-exits --run=economic-label-1001-104
```

Replace `--economic-label-exits` with `--legacy-label-exits` for the matched old mortality policy. Leave `--regional-radio-build-clock` absent. Fixed financial checks use `SaveLoadRoundTripRunner.tscn` with `--economic-label-check` and the same enable flags. Omit the check switch and set `--weeks=13` for the full-world save roundtrip.

## Evidence

Raw CSVs, stdout/stderr and manifests live in ignored `SimLogs/`. Relevant manifests: `regional-followup2-runs.json`, `regional-earned-runs.json`, `regional-radio-clock-runs.json`, and `economic-label-runs.json`. The earned-distribution assembly precedes the radio-clock helper, whose flag-off branch returns the original age unchanged; hashes are not represented as identical builds.

Analysis entry points: `analyze_regional_survival.py`, `analyze_regional_economy.py`, `analyze_regional_timing_detail.py`, `analyze_regional_sync.py`, `analyze_regional_guards.py`, and `analyze_economic_lifecycle.py`. `test_regional_analysis.py` has six passing regression cases for survival/promotions, format isolation, empty markets, survey-invariant airplay, missing-week handling and telemetry availability / insolvency evidence. Final lifecycle analyses were refreshed with that availability check; legacy zero defaults are not cash-flow observations.

Final policy evidence: `economic-label-{1001,3003}-104-analysis.json`, `economic-holdout-{legacy,new}-2002-260-analysis.json`, `economic-marketshare-two-year.json`, `economic-marketshare-five-year.json`, the two new-policy `-sync.json` files, `economic-label-legacy-replay-1001-52-comparison.json`, `economic-label-check-console.log`, and `economic-label-roundtrip-console.log`. Raw engine runs, not Python surrogate simulations, produced these observations.
