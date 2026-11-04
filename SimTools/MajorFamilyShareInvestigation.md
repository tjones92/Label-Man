# Major-family share, earlier composition and Independent failures — 2026-10-16

Historical follow-up: [HistoricalLabelTierBalanceResearch.md](HistoricalLabelTierBalanceResearch.md) adds sourced
company-concentration benchmarks and a matching Top 10 analysis of existing logs. In the adopted 1960 run,
MidTier first-time TeenPop signings fall 6 -> 3 against the later bundle control, while 99.24% of its TeenPop chart
units come from artists with no signing/re-signing event that year. This weighs against new talent acquisition as
the opening-year explanation. The follow-up makes no balancing or simulation-code changes.

The first-year rise is carried mainly by MidTier sales, but its size depends on the baseline. The adopted seed-2002
configuration has 62.55% Major-family chart-unit share in 1960: +12.03 percentage points against the original noon
economic-policy run, but -0.34 points against the pre-arrival economy. Increased Independent failures are observable
against the noon run, but do not account for a direct loss of existing chart sales. The separate 1962 spike is heavily
affected by successful Independents being classified as MidTier.

This investigation reads existing logs only. It changes no game settings or simulation code and runs no new simulation.
Instructions and proposals in the earlier method documents are background, not additional requests to implement them.

## What is being measured

`majorFamilyChartShare` is the fraction of annual **Single-chart-week units**, grouped by each operating label's
year-end owner and tier. Major and MidTier are combined. Independent, Boutique and Small form the other family.
It is neither the fraction of labels nor the fraction of chart entries, and it excludes off-chart and Album units.

This grouping is broader than major ownership: `IsIndependentReadyForMidTier` explicitly defines MidTier as a large,
proven independent. A successful Independent can therefore move its entire year's chart sales into the reported
Major family without being bought. The share calculation does not apply the master-control test used by the separate
owner-entry counters. There are **zero `Absorb` ledger events** in the 20 completed runs inspected here.

Implementation: `ChartAuditRunner.AccumulateConcentration`, `WriteConcentrationYear`, `IsIndieFamily` and
`CountOwnerFamilyEntries`; promotion rules: `LabelLifecycleManager.IsIndependentReadyForMidTier`.

## Earlier runs: before the Small-label cost experiments

All times below are America/New_York (EDT). Completion markers verify the runs. File modification times establish
that the original results already existed before 2 p.m.; where available, manifests also record launch times/arguments.
They are not claims that all these runs used the same assembly.

| Stage | Original evidence | Evidence available |
|---|---|---|
| Pre-arrival: automatic national launch seeding, demand-based local readers | `sig5d-{1001,2002}-104` | Oct 15 23:47 / Oct 16 00:35 |
| First arrival restriction, including the former one-stop trigger | `arr4-{1001,2002}-104` | Oct 16 00:35 |
| Regional economy recovery: sparse retail and continuing local campaign | `focus3-{1001,2002}-104` | Oct 16 11:19; launched 11:09 |
| Recognition of acquired distribution access | `regional-earned-1001-104` | Oct 16 11:51; launched 11:39 |
| Economic exits, before dealer-price/pressing work | `economic-label-1001-104`, `economic-holdout-new-2002-260` | Oct 16 12:16 / 12:39; launched 12:03 |
| First completed dealer-price/pressing treatment | `pressing2-2002-260` | Oct 16 13:59; **already a Small-cost intervention** |

The later `ab5ctl` run is useful as its own bundle control, but it is not a substitute for the original noon baseline.
Its later-year trajectory differs from the original run. Compare matched seed and simulated year, not completion
time alone. The pre-arrival controls last 104 weeks; they provide no 1962–64 counterfactual.

### 1960 composition, seed 2002

Percent of all chart-week units; the last column is Major + MidTier. Rounding can keep displayed totals from 100.

| Stage | Major | MidTier | Independent | Boutique | Small | Major family |
|---|---:|---:|---:|---:|---:|---:|
| Pre-arrival `sig5d` | 37.88 | 25.01 | 24.47 | 11.12 | 1.52 | **62.89** |
| First arrival `arr4` | 35.75 | 26.80 | 25.12 | 10.41 | 1.93 | **62.54** |
| Recovered regional economy `focus3` | 35.80 | 21.32 | 29.81 | 11.21 | 1.86 | **57.12** |
| Noon economic exits, pre-pressing | 32.55 | 17.97 | 32.29 | 14.88 | 2.31 | **50.52** |
| First pressing treatment `pressing2` | 37.32 | 24.61 | 24.91 | 9.17 | 3.99 | **61.93** |
| Adopted period costs + home territory | 36.61 | 25.94 | 24.74 | 9.62 | 3.09 | **62.55** |

Thus the current first-year composition resembles the pre-arrival one, although that does not establish that the
same mechanism produced it. The reduction to 50.52% occurred during the intervening regional/survival work.

### The initial coverage restriction did raise MidTier share on seed 1001

| 1960, seed 1001 | Major | MidTier | Independent | Boutique | Small | Major family |
|---|---:|---:|---:|---:|---:|---:|
| Pre-arrival `sig5d` | 32.32 | 19.30 | 33.18 | 11.28 | 3.93 | **51.62** |
| First arrival `arr4` | 28.42 | 27.26 | 32.85 | 9.15 | 2.32 | **55.69** |
| Recovered regional economy `focus3` | 36.21 | 20.73 | 30.77 | 10.60 | 1.69 | **56.94** |
| Noon economic exits, pre-pressing | 28.95 | 22.18 | 38.51 | 8.12 | 2.25 | **51.12** |
| Adopted configuration, one-year holdout | 32.91 | 21.48 | 35.81 | 6.85 | 2.94 | **54.40** |

The first restriction adds **4.07 points of family share**, comprising **+7.97 MidTier** and **-3.90 Major**.
Independent-tier share falls only 0.32 points; most of the offset is Boutique and Small. Starting-Independent closures
are 11 versus 12, a one-firm difference. This supports the user's recollection of an early MidTier-led increase, not an
explanation based on a large wave of Independent failures.

It is not a sustained or seed-universal increase:

| First arrival versus pre-arrival | 1960 family change | 1961 family change |
|---|---:|---:|
| Seed 1001 | +4.07 pp | -4.56 pp |
| Seed 2002 | -0.35 pp | -5.57 pp |

The two-year pre-arrival family shares are 51.62 / 54.97% (1001) and 62.89 / 56.82% (2002).
Current seed 2002 is 62.55 / 52.37%; the original geographic restriction and today's home-only starting footprint are
different interventions. The former gated servicing/promotion outside actual access; the latter removes the factory's
0–2 extra starting distribution regions for Small labels.

## The adopted run versus the original noon run

| Year, seed 2002 | Noon Major family | Adopted Major family | Family change | Major change | MidTier change |
|---|---:|---:|---:|---:|---:|
| 1960 | 50.52 | 62.55 | **+12.03** | +4.06 | **+7.97** |
| 1961 | 54.57 | 52.37 | -2.20 | +1.80 | -4.00 |
| 1962 | 48.14 | 52.13 | **+3.98** | -3.34 | **+7.33** |
| 1963 | 51.56 | 51.41 | -0.15 | -2.10 | +1.95 |
| 1964 | 52.74 | 46.49 | -6.26 | -3.41 | -2.85 |

Across all five years, weighted by actual chart units, family share is **51.47% -> 53.42% (+1.95 pp)**.
There is an opening-year imbalance, not a steadily rising family share throughout this window.

### The latest home-only experiment also moves first-year shares without Independent failures

Keeping corrected period costs and the other accepted switches on:

| 1960 | Home off: family | Home on: family | Change | MidTier change | Major change | Starting Independent failures, off / on |
|---|---:|---:|---:|---:|---:|---:|
| Seed 2002 | 59.41 | 62.55 | +3.14 | +6.37 | -3.23 | 0 / 0 |
| Seed 1001 | 52.64 | 54.40 | +1.76 | +1.01 | +0.75 | 0 / 0 |

Neither arm has any Independent-tier closure in 1960 on these two seeds. These observations support a shift in
release performance/competition when reach narrows, rather than a first-year chart-share gain caused by Independent
closures. They are still two seed outcomes with diverging random sequences, not a precise causal estimate.
The seed-2002 home effect reverses in 1963–64 (-5.79 / -4.25 pp versus home off).

## What actually carries the opening-year MidTier gain

The original noon and later `ab5ctl` runs have the same 1960 chart data for this comparison:

| MidTier, 1960, seed 2002 | Noon control | Adopted |
|---|---:|---:|
| Chart-week units | 13.80M | **20.99M** |
| Distinct charting records | 145 | **137** |
| Chart units per distinct charting record | 95,204 | **153,241 (+61%)** |
| Release-strategy decisions, all formats | 492 | 467 |
| Mean decision quality estimate | 0.5645 | 0.5658 |
| Seeded-catalogue chart units | 1.270M | 1.275M |
| In-run release chart units | 12.535M | **19.719M** |
| TeenPop chart units | 3.733M | **8.767M** |
| Top five records' share of all chart units | 3.98% | 4.98% |

This is new-release performance rather than an opening-catalogue windfall, more releases, or more charting titles.
TeenPop contributes **5.03M of the 7.19M extra MidTier chart units (70%)**. The top five records account for only
about 1 point of the 7.97-point MidTier share increase, so the increase extends beyond a few exceptional hits.

Independent charting titles increase **288 -> 311**, while their units per title fall **86,158 -> 64,373**.
Independent release-strategy decisions also rise **2,309 -> 2,329**. In the adopted run, their lower first-year share
cannot be explained by fewer Independent chart entries or fewer release decisions. Boutique entries fall 140 -> 115.

The earlier `pressing3` analysis in the Small-label method document found a similar broad MidTier demand-per-record
effect concentrated in TeenPop and artists already at MidTier. The present result confirms the output pattern on the
adopted run; it does not independently reproduce that earlier demand-stage decomposition, because the compact
adopted logs omit the regional demand-stage ledger and most off-chart record rows.

The live demand multiplier is continuous in actual distribution strength and effective reach
(`ChartSimulator.GetLiveLabelDemandScale`); it does not grant a discrete purchase-rate bonus merely for becoming
MidTier. Production attributes, format choice, promotion, radio and chart feedback can still change outcomes.
Hook/production/originality and other record attributes also use the shared `GD` random stream. Different affordable
releases and distribution decisions change which later records receive those draws. Stable mean **estimated** quality
is not proof that realized record attributes, exposure or sales conversion were held constant.

The logs establish where the excess is, but cannot partition the 1960 effect into changed competition, format mix,
distribution access, feedback and reshuffled record draws. The existing evidence does not justify calling all 12 points
either a deterministic MidTier subsidy or random noise.

## The 1962 spike: successful Independents change the family classification

The +7.91-point guard breach in the Small-label cost report uses **old costs + home territory** as its comparator,
not the original noon run. Both comparisons can be decomposed using a frozen first-observed tier for each owner:

| 1962, seed 2002 | Published family | Family with owner birth tiers frozen | Net tier reclassification |
|---|---:|---:|---:|
| Original noon economic policy | 48.14 | 44.25 | +3.90 |
| Old costs + home territory (`ab5trt`) | 44.22 | 43.28 | +0.94 |
| Adopted period costs + home territory | 52.13 | 42.81 | **+9.32** |

Against old-home, the arithmetic is **+7.91 = -0.47 frozen-cohort change + 8.38 reclassification change**.
Against noon it is **+3.98 = -1.43 + 5.42**. These are accounting decompositions of observed sales, not independently
identified causal effects of changing the promotion rule.

In the adopted run, birth-Independents now in the family supply **10.62%** of all chart units; starting family labels
now outside the family subtract **1.30%**, giving the +9.32 net. Old-home has only +1.89% / -0.95%.
Adopted cumulative Independent -> MidTier promotion events are 12 versus 7; cumulative reverse demotions are 9 versus 7.
The live MidTier stock is 23 versus 20. Important contributors include Pharaohs Records, Justice Records, National
Records, Lightning and Specialty Records. They are not evidence of Major acquisitions.

Year-end classification also assigns pre-promotion sales to the new tier. Using the operating tier recorded in each
chart week gives **49.37%** in the adopted run versus the published **52.13%**. That timing convention contributes
2.75 points to its reported level; it is a subset of the broader classification issue, not an additional effect to add.

## Are more Independents failing?

**Versus the original noon economic-policy run, yes in counts, but with no observed chart sales to remove directly.**

| Five years, seed 2002 | Noon pre-pressing | Old costs + home | Adopted |
|---|---:|---:|---:|
| Starting-Independent businesses closing, n=197 | 0 | 7 | **2 (1.02%)** |
| Later Independent births | 63 | 131 | 101 |
| Closures in those Independent birth cohorts, including later demotions | 12 | 31 | 21 |
| All labels closing while currently Independent | 12 | 46 | 19 |

The 2 adopted starting-Independent closures are `label_0149` (Liberty Records, the procedural label) and `label_0389`
(Weathered Records), both at week 109. Both had zero chart units. There are no starting-Independent closures in
1960–61, when the large opening-year share gain is already present.

The other 17 closures at Independent tier are runtime entrants. Another 4 runtime Independent births demote to Small
before closing. **All 23 failed Independent-born businesses in the adopted run have zero observed chart units over
the entire run**, as do the 12 failed Independent-born businesses in the noon run. The two adopted failed launch
labels also never chart in the original noon control. Their removal supplies no direct observed chart-unit denominator
effect. A 20.8% versus 19.0% crude runtime-cohort closure fraction is not an age-adjusted failure comparison: births
are differently numerous and have different follow-up durations.

Relative to the earlier legacy-exit economy, starting-Independent closure actually falls: `sig5d` closes 24 starting
Independents by two years; `focus3` closes 23; the adopted run closes zero by that horizon. Old exit-rule closure
counts include competitive exits and are not equivalent to verified cash insolvency.

This does not prove the failures have zero indirect effect. Off-chart Single and Album competition, releases that
never happen after failure, and artists re-signing elsewhere could matter. No share calculation transfers a dead
label's chart units automatically to a Major, and the present logs do not identify the size of those indirect effects.
The evidence therefore weighs against Independent failure as the main explanation for the reported increases.

## Implications

Report actual Major share, MidTier share, and the combined family separately. Retain the existing concentration
metric for continuity, but accompany it with chart-week tiers and frozen starting cohorts so successful independent
growth is visible. The 1962 breach should not be treated as a direct measure of Major consolidation.

The remaining opening-year investigation should focus on new MidTier TeenPop releases: actual per-record attributes,
format/release lane, regional demand/conversion and chart/radio feedback, compared across multiple seeds. A diagnostic
with record-generation draws keyed to stable label/artist/release identities would help isolate policy response from
random-stream reordering. No such engine change is made here. The current evidence does not support reversing the
sourced pressing costs, increasing Independent survival subsidies or changing promotion thresholds to fit the family
share.

## Evidence and reproduction

`SimTools/analyze_major_family_share.py` reconstructs every annual chart-unit total and Major-family share from the
record rows, finance census and acquisition ledger, checking each against `concentration.csv`. It reads only chart rows
from either full or compact record logs. Frozen tiers use the owner's first finance-observed tier, including runtime
births. Release-strategy decisions are identified as decisions, not asserted to equal the number of released records.
The seeded-catalogue split uses release age greater than elapsed audit week; remaining chart units are in-run releases.

All **64 annual observations across 20 runs** match published chart units exactly and published family shares within
0.000001 as fractions. All 20 runs have completion markers. Provenance distinguishes original logs, completion
timestamps and later replays. No incomplete seed-1001 `ab5*-260` arm or stopped `focus2` arm is used.

Outputs in `SimLogs/major-family-investigation/`:

- `earlier-1001.json`, `earlier-2002.json`: original pre-2-p.m. compositions and cohort closures.
- `decomposition.json`: six five-year later-control/treatment arms, including both adopted-cost territory settings.
- `holdout-1001.json`: the three one-year cost/territory arms on the second seed.
- `annual-composition.csv`: all 64 annual tier compositions and starting-Independent closure counts.
- `provenance.json`: original file timestamps in New York time and exact completion markers/log paths.

Example commands (use an available Python with pandas):

```text
python SimTools/analyze_major_family_share.py SimLogs/sig5d-2002-104 SimLogs/arr4-2002-104 SimLogs/focus3-2002-104 SimLogs/economic-holdout-legacy-2002-260 SimLogs/economic-holdout-new-2002-260 SimLogs/pressing2-2002-260 --output SimLogs/major-family-investigation/earlier-2002.json
python SimTools/analyze_major_family_share.py SimLogs/sig5d-1001-104 SimLogs/arr4-1001-104 SimLogs/focus3-1001-104 SimLogs/regional-earned-1001-104 SimLogs/economic-label-1001-104 --output SimLogs/major-family-investigation/earlier-1001.json
python SimTools/analyze_major_family_share.py SimLogs/ab5ctl-2002-260 SimLogs/ab5press-2002-260 SimLogs/ab5nohome-2002-260 SimLogs/ab5trt-2002-260 SimLogs/smallcost-fiveyear-v1-paired-2002-260 SimLogs/smallcost-fiveyear-v1-paired-home-2002-260 --output SimLogs/major-family-investigation/decomposition.json
python SimTools/analyze_major_family_share.py SimLogs/smallcost-paired-year1-holdout-v1-legacy-1001-52 SimLogs/smallcost-paired-year1-holdout-v1-paired-1001-52 SimLogs/smallcost-paired-year1-holdout-v1-paired-home-1001-52 --output SimLogs/major-family-investigation/holdout-1001.json
```
