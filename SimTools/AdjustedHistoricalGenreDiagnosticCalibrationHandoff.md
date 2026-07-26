# Adjusted historical genre diagnostic and calibration handoff

Status: **READY FOR IMPLEMENTATION / ONE NARROW BUG FIXED / NO NEW SIMULATION RUN AUTHORIZED**

## Objective

Calibrate the enabled Genre Market V2 runtime against the adjusted 1960-1969
record-market share reference without disturbing the accepted economy, label
lifecycle, release throughput, reconciliation, disabled path, or RNG
boundaries.

This supersedes any reading of the prior holdout as a one-genre Psychedelic
Rock problem. Psychedelic Rock remains a useful sentinel, but the adjusted
reference and three completed seeds reveal a catalog-wide allocation problem:

- Traditional Pop, Rock and Roll, Doo-Wop, Baroque Pop, and Sunshine Pop take
  too much market at important points;
- Soul, R&B, Easy Listening, Country, Psychedelic Rock, British Beat, and
  British Blues take too little;
- several peak years or decade-long slopes are wrong even when onset and
  survival are plausible;
- the largest break occurs around the 1965-1966 lifecycle transition, when
  R&B, Teen Pop, and Soul lose share while Country and Traditional Pop absorb
  a large part of the reallocation.

Do not repair this by fitting 420 annual quotas. The reference should constrain
onset, direction, scale, peak timing, succession, and plausible bands while the
simulation continues to produce endogenous annual results.

## Inputs and generated evidence

Human-authored inputs:

```text
C:\Users\grohl\Downloads\label-man_genre_market_share_1960-69.xlsx
C:\Users\grohl\Downloads\genrenote.odt
```

Input SHA-256:

```text
label-man_genre_market_share_1960-69.xlsx
614FE30875F5990F309E54AD25049DE243839E9CD545A025063A988215EA3240

genrenote.odt
CE9B83E9CD56039917F25F107FC613EB992C820DB468F11D24A9EC48FB43D6DF
```

Repository evidence:

```text
SimTools/AdjustedHistoricalGenreShareTargets.csv
SimTools/AdjustedHistoricalGenreShareComparison.csv
SimTools/GenreAnnualMarketShareTable.md
SimTools/GenreHistoricalBoundsAnnual.csv
SimTools/GenreHistoricalBoundsSummary.md
SimTools/GenreCalibrationPostHoldoutHandoff.md
```

The target CSV contains 42 canonical genres and ten annual percentages. Every
annual column sums to exactly 100%. The comparison CSV contains all 420
genre-years with:

```text
targetSharePct
currentThreeSeedMeanSharePct
deltaPctPoints
```

The current value is the mean of the separately normalized annual shares from
the immutable 522-week seeds 1001, 1002, and 2007. These runs all predate the
British bridge fix recorded below.

The workbook describes historically reasoned estimates, not audited market
statistics. Its market-share definition is total record-market revenue/units,
not Hot 100 entries or Single-only chart presence. Use it as a calibrated
historical envelope, not a claim that every cell is exactly measurable.

## Source-of-truth decisions

Treat these as settled:

- British Blues and American-derived Blues Rock remain distinct canonical
  genres.
- Legacy tags in the workbook are reference-only and must not be restored as
  live identities.
- Soundtracks will be implemented as their own release format. Do not use
  Traditional Pop as a permanent bucket for all soundtrack success.
- Traditional Pop should decline to roughly 5-6% by 1969.
- Rock and Roll must cease acting as a late-decade catch-all.
- Psychedelic Rock is not a Single-exclusive genre, but a 1967 commercial
  result with zero or nearly zero Singles is historically wrong.

Two workbook/catalog disagreements must be resolved explicitly before imposing
a universal pre-emergence rule:

| Genre | Workbook first nonzero | Catalog emergence | Disposition |
| --- | ---: | ---: | --- |
| Soul | 1960 at 7.92% | 1960 | Catalog emergence is aligned to the target's real 1960 Soul market. Do not erase the target by generic filtering. |
| British Pop | 1963 at 0.12% | 1964 | Keep the bridge aligned to the current 1964 catalog until the owner chooses whether this trace 1963 share is precursor leakage or a requested catalog change. |

The spreadsheet correctly assigns zero to Surf Rock in 1960 and Blues Rock
before 1966. Those cases do not share Soul's target exception.

## Change already implemented

### British bridge/catalog alignment

`Systems/GenreSupplyService.cs` previously hardcoded:

```text
British Beat / British Pop >= 1964
British Blues               >= 1965
```

That contradicted the catalog for British Beat (1963) and British Blues
(1964). `IsBritishSupplyBridgeActive` now canonicalizes the genre and compares
the year with the profile's authored `EmergenceYear`.

Resulting starts:

```text
British Beat  1963
British Pop   1964
British Blues 1964
```

`SimTools/GenreMarketV2ProbeSuite.cs` now covers the year immediately before
and the authored bridge year. `dotnet build "Label Man.sln" --no-restore`
passes with zero errors and the inherited unused-event warning.

This fix has not been measured in a new simulation. Do not describe the
commercial onset or share as corrected until a separately authorized run
confirms it.

## Claude-note disposition

| Finding | Static disposition | Implementation decision |
| --- | --- | --- |
| Frozen 1960 pool creates pre-emergent Surf Rock and Blues Rock identities | **Confirmed** | Repair on the enabled initial-pool path, but preserve draw count/order and redistribute their prior prospectively. |
| Frozen 1960 pool creates pre-emergent Soul | **Resolved** | Soul's catalog emergence is 1960, matching the target's 7.92% 1960 share. Do not filter it from the enabled initial pool. |
| British Beat and British Blues bridge one year late | **Confirmed and fixed** | Retain catalog-derived bridge logic and probes. |
| Runtime secondary genres default new genres to Traditional Pop | **Overstated** | `GetRelatedGenre` has that default, but runtime formations immediately replace it with `ChooseRuntimeSecondaryGenre`, which selects an available adjacent genre. Audit initial and non-runtime call sites only. |
| Runtime formation may default to Traditional Pop when availability is empty | **Fallback exists; ordinary reachability not demonstrated** | Instrument the branch. `GetAvailableGenres` contains stable profiles throughout 1960-1969, so do not tune around a hypothetical hit. |
| Other null fallbacks silently inflate Traditional Pop | **Possible defensive paths, unmeasured** | Add counters and source labels before changing enum/default behavior. |
| Country Southwest gate fails in seed 1002 | **Not supported by the current full-decade regional report** | Existing three-seed report says all preferred Country regions exceed national share. Reproduce the exact annual predicate before filing a code fix. |
| 1966 market dislocation | **Confirmed in results; cause not traced** | Highest-priority diagnostic. Do not patch a genre keyframe before decomposing conserved flow. |
| Rock and Roll lacks a strong historical decline | **Confirmed** | Repair catalog/lifecycle interpretation after structural flow diagnostics. |
| Jazz rises instead of gradually declining | **Confirmed** | Diagnose LP-format and catalog persistence contributions before lowering demand. |
| Gospel trajectory is inverted | **Confirmed** | Trace supply, retention, label fit, and format conversion; the authored baseline itself rises. |
| Acid Rock and Proto-Metal are broadly plausible | **Supported with local watches** | Protect them from collateral overcorrection. |

## Quantitative priority

`MAE` is mean absolute error in percentage points across 1960-1969. The
worst-year cell shows current three-seed mean versus adjusted target.

| Genre | MAE | Worst genre-year | Target peak -> current peak | Reading |
| --- | ---: | --- | --- | --- |
| Traditional Pop | 10.67 | 1967: 26.44 vs 7.30 (+19.14) | 1960 -> 1967 | Dominant structural failure; wrong slope and late spike. |
| Soul | 5.34 | 1968: 5.62 vs 18.47 (-12.85) | 1968 -> 1965 | Mid-decade crest is early and late market collapses. |
| R&B | 3.74 | 1966: 1.21 vs 9.33 (-8.12) | 1963 -> 1963 | Peak timing is acceptable; post-1965 death handling is far too abrupt. |
| Rock and Roll | 3.51 | 1968: 10.15 vs 1.85 (+8.30) | 1960 -> 1960 | Early peak is right; tail remains much too large. |
| Doo-Wop | 3.07 | 1960: 14.03 vs 5.09 (+8.94) | 1960 -> 1960 | Correct peak year but oversized frozen cohort. |
| Easy Listening | 2.91 | 1960: 0.71 vs 6.50 (-5.79) | 1962 -> 1969 | Wrong level and wrong decade direction. |
| Country | 2.21 | 1968: 6.55 vs 11.36 (-4.81) | 1969 -> 1966 | One-year 1966 spike replaces the intended stable rise. |
| Psychedelic Rock | 1.92 | 1967: 0.12 vs 6.43 (-6.31) | 1967 -> 1969 | Intended crossover year fails to convert commercially. |
| Jazz | 1.78 | 1966: 8.21 vs 5.07 (+3.14) | 1961 -> 1966 | LP-era economics overpower intended gradual decline. |
| Teen Pop | 1.66 | maximum gap 2.47 | 1960 -> 1960 | Direction is right; decline is too aggressive around 1965-1966. |
| Sunshine Pop | 1.62 | 1969: 4.94 vs 0.73 (+4.21) | 1967 -> 1967 | Peak year is right; scale and tail are too large. |
| Blues | 1.53 | maximum gap 2.50 | 1961 -> 1969 | Persistent niche is too small early and peaks too late. |
| Classical | 1.53 | maximum gap 1.94 | 1968 -> 1966 | Consistently under-sized specialist market. |
| British Beat | 1.39 | 1964: 3.75 vs 8.47 (-4.72) | 1964 -> 1965 | The fixed bridge should restore runway; remeasure before magnitude tuning. |
| Baroque Pop | 1.30 | 1967: 5.48 vs 0.80 (+4.68) | 1967 -> 1967 | Correct timing but implausibly large retrospective niche. |
| Gospel | 1.18 | 1960: 3.14 vs 1.13 (+2.01) | 1969 -> 1960 | Full trajectory is reversed. |
| Blues Rock | 1.05 | 1968: 4.64 vs 1.56 (+3.08) | 1969 -> 1969 | Late direction is correct; scale is too large. |
| British Blues | 1.04 | 1969: 0.72 vs 4.09 (-3.37) | 1969 -> 1965 | Corrected onset may help, but current growth is too weak and ends early. |

The remaining genres have MAE below 1 point or are already close enough to be
treated as regression sentinels. The complete, non-curated results are in
`AdjustedHistoricalGenreShareComparison.csv`; do not infer that omission from
the table above means exact agreement.

Especially protect:

- Reggae, Rocksteady, Singer-Songwriter, Progressive Rock, Boogaloo,
  Children's, Tex-Mex, Country Rock, Proto-Metal, and Proto-Punk from broad
  changes that destroy their already-small errors;
- Surf Rock's 1963 peak and subsequent decline;
- Folk/Folk Rock succession;
- Bubblegum and late Hard Rock emergence, while allowing scale correction;
- specialist nonzero survival and regional ordering.

## Structural diagnosis

### 1. The initial pool is a commercial prior

The frozen launch bands are not neutral population flavor. With high 1960
identity retention, they materially seed the decade's releases:

```text
Rock and Roll 18% general-band interval
R&B           14%
Traditional   10%
Doo-Wop        8%
Soul           8%
...
Surf Rock      3%
Blues Rock     5%
```

The 18% vocal-group cohort adds more Doo-Wop, Soul/R&B, and legacy
Girl Group/Motown identities before migration. This explains both the
oversized early Doo-Wop market and the Surf/Blues Rock ghost cohorts. It may
also make later decline tuning ineffective because catalog demand is fighting
a large retained identity stock.

Required repair:

- create an enabled-only initial primary prior from explicit 1960 commercial
  targets or from emergence-valid catalog profiles;
- preserve the number of RNG draws and their order;
- preserve the disabled path byte-for-byte;
- map the removed Surf Rock and Blues Rock probability mass to documented 1960
  genres rather than deleting artists;
- expose the realized initial identity histogram and its authored prior in a
  fixed probe;
- retain Soul as a valid 1960 identity while finalizing the filter.

Do not alter `InitialGeneralGenreBands` globally if that changes disabled
replay behavior.

### 2. Lifecycle “death” is acting like a cliff

R&B has a catalog death year of 1965. Its target declines gradually from 10.04%
in 1965 through 7.74% in 1969, while the simulation falls from 10.31% to 4.81%
to 1.21% across 1965-1967. Teen Pop also contracts too sharply.

`GetProjectIdentityRetention` has only coarse lifecycle levels:

```text
Legacy     .12
Declining  .30
Other      .78 (.95 before 1961)
```

That produces a discontinuity when a genre crosses into Legacy and gives
finite-wave genres no slope-aware taper before or after the transition.
Conversely, no-death Rock and Roll remains Established and receives .78
retention indefinitely even though its historical market should collapse.

Required design:

- separate “no new artist supply” from “existing catalog and artists retain a
  declining commercial identity”;
- make retention respond to immutable profile level, profile slope, years
  since peak/death, and prospective same-artist project history;
- conserve project opportunity by rerouting reduced retention into the normal
  candidate set;
- do not consult realized sales, charts, backorders, or the target table during
  a runtime decision;
- allow R&B a fading tail while forcing Rock and Roll out of the generic
  Established plateau.

### 3. The 1966 reallocation needs a flow ledger

Current mean shares:

```text
                 1965    1966    1967
R&B             10.31    4.81    1.21
Teen Pop         7.31    3.35    1.53
Soul            12.60    6.50    4.80
Country          8.09   12.34    7.69
Traditional Pop 19.18   22.85   26.44
```

R&B and Teen Pop have lifecycle reasons to decline, but Soul does not have a
death year and should not halve with them. Country's spike-and-revert has no
matching target shape. This looks like conserved opportunity being redirected
through candidate availability, label/artist fit, annual floors, format
capacity, or release conversion.

Add one prospective annual ledger that reconciles, by source identity and
selected genre:

```text
eligible artist-project opportunities
retained selections
transition selections
annual-floor selections
unavailable/death rejections
capacity reroutes
Single decisions
Album decisions
orphan/promo decisions
commercially fulfilled units
```

Include `fromGenre -> toGenre` counts for non-retained choices. The sum of
outgoing routes must equal the conserved opportunity total. Use this to answer
whether the disappearing R&B/Teen Pop/Soul opportunity is flowing mainly to
Traditional Pop, Country, or newly emerging genres.

### 4. Format conversion remains structurally inverted

The prior holdout handoff found that authored Single orientation has almost no
positive relationship with realized Single decision share. Psychedelic Rock's
1967 failure is the clearest symptom: it is Album-leaning, not Single-absent,
yet seeds produce zero or nearly zero 1967 Singles. Late cap-forced Singles
then help move its commercial peak to 1969.

The implementation must retain the earlier ordering:

1. stop `Album wins -> per-artist Album cap rejects -> emit Single`;
2. make the denied project opportunity reroute prospectively;
3. make identity retention slope-aware;
4. only then recalibrate centered format influence across all profiles;
5. diagnose momentum if the late Psychedelic peak survives.

Do not make Psychedelic Rock Single-dominant to manufacture a 1967 share. The
target requires a mixed crossover market in which both major Singles and
Albums can convert.

### 5. Traditional Pop fallbacks require telemetry, not assumption

`Genre.TraditionalPop` is enum value zero and appears in defensive fallbacks.
However:

- `ChooseRuntimeFormationGenre` receives an availability list that should
  remain nonempty throughout the decade;
- runtime formations replace `GenerateArtist`'s legacy secondary with the
  adjacency-aware runtime secondary;
- several null coalesces are defensive and have not been counted in a live
  run.

Add counters with caller/source labels for every enabled-path fallback to
Traditional Pop. A fixed probe should prove normal 1960-1969 catalog
availability never takes the empty-list branch. If runtime counts remain zero,
remove this hypothesis from the causal chain and focus on retained stock,
transition weights, release formats, and catalog persistence.

Avoid changing the enum numeric layout merely to catch defaults; that risks
serialization compatibility and is not required for diagnosis.

### 6. The target includes product-category pressure

The workbook gives Traditional Pop and Easy Listening more early/mid-decade
weight partly because soundtrack and adult-market products were commercially
large. The planned soundtrack release format must carry that product demand
without permanently assigning every soundtrack-adjacent record to
Traditional Pop.

Genre calibration and soundtrack implementation therefore need an explicit
boundary:

- genre identity answers musical market;
- release format/product answers Single, Album, soundtrack, and related
  commercial packaging;
- product success may distribute across Traditional Pop, Easy Listening,
  Broadway/film-adjacent Pop, Rock, Soul, and other genres;
- do not compensate for a missing format by raising Traditional Pop's late
  genre baseline.

## Implementation plan

### Phase A — targets and deterministic probes

1. Load `AdjustedHistoricalGenreShareTargets.csv` in analysis tooling only.
   Runtime code must not read historical target shares.
2. Extend the analyzer to emit for every genre:
   - MAE and signed bias;
   - worst genre-year;
   - target/current first nonzero year;
   - target/current peak year and share;
   - 1960-to-1969 slope;
   - shape classification: rise, fall, finite wave, stable niche.
3. Add catalog/target consistency output for nonzero-before-emergence cells.
4. Add fixed probes for British bridge/catalog parity, enabled initial-prior
   normalization, unchanged RNG draw count/order, and disabled neutrality.

### Phase B — observability checkpoint

Add the annual flow ledger and fallback counters described above. Also record:

- initial cohort versus runtime-formation cohort;
- primary versus secondary identity;
- artist and label fit factors;
- lifecycle state, baseline level, and baseline slope;
- pre-cap preferred format, capacity rejection, final format, and reroute;
- direct and adjacent momentum contribution when full telemetry is enabled.

Lean telemetry may aggregate these counters, but the first diagnostic
checkpoint should retain enough full telemetry to explain 1965-1967.

### Phase C — initial cohort repair

Implement an enabled-only, emergence-aware 1960 initial prior. Soul is a valid
1960 catalog identity. Preserve artist count, draw count/order, and disabled output.
Verify:

- Surf Rock commercial share is zero in 1960 and begins in 1961;
- Blues Rock is zero through 1965 and begins in 1966;
- Soul remains available as a documented 1960 catalog identity;
- Doo-Wop's 1960 share is no longer forced toward 14%;
- removed probability mass is visibly conserved.

### Phase D — lifecycle and transition repair

Replace cliff/plateau retention with a bounded curve based on authored
prospective inputs. First tune shape, not exact percentage:

- Rock and Roll declines continuously and is a small 1969 identity;
- R&B retains a meaningful fading tail after 1965;
- Teen Pop declines without disappearing too early;
- Soul continues growing through the late decade;
- Gospel can rise and Jazz can gradually decline;
- total release opportunity and accepted economy remain conserved.

### Phase E — format-capacity repair

Apply the prospective capacity reroute from the prior handoff. Prove that an
Album-favored project denied by the per-artist cap is not silently emitted as
a Single. Then recalibrate centered orientation so fixed equal-input
portfolios satisfy:

```text
Single-lean decision share > middle > Album-lean
```

Protect the global Album mix and gross crossover gates.

### Phase F — calibrated demand and scale

Only after Phases C-E:

- adjust immutable catalog keyframes, era curves, supply weights, label fit,
  and transition adjacency where the flow ledger supports the change;
- reduce oversized retrospective microgenres such as Baroque Pop and Sunshine
  Pop without eliminating them;
- allow British Beat and British Blues to remeasure after the bridge fix
  before raising their weights;
- keep Blues Rock distinct and reduce its scale without donating all of its
  share to British Blues;
- preserve small late genres already near target.

### Phase G — momentum, if still necessary

If Psychedelic Rock, Country, or Traditional Pop still has a late/one-year
spike after supply, retention, and format repairs, decompose momentum into
direct evidence, secondary identity, each adjacency donor, decay, saturation,
fatigue, and Zeitgeist delta. Tune momentum only from that decomposition.

## Provisional acceptance envelope

Exact cell matching is neither expected nor desired. Use these initial
cross-seed mean bands:

| Target share | Provisional absolute tolerance |
| ---: | ---: |
| at least 10% | +/- 3.0 percentage points |
| 2% to under 10% | +/- 1.5 points |
| under 2% | +/- 0.75 points |

Additionally:

- first material commercial onset should match the target/catalog decision;
- finite-wave peaks should land within one year of target;
- decade direction must match even if the exact endpoint is inside tolerance;
- a target-zero pre-emergence year should remain zero except for a documented
  precursor/seed-scene exception;
- no canonical genre should exceed 35% of an annual market;
- no genre-year with at least 20 release decisions may lose an entire format
  lane without a documented product exception;
- assess both pooled mean and individual seeds so averaging does not hide a
  collapse.

Priority historical endpoints:

```text
Traditional Pop 1969 about 5-6%
Rock and Roll    1969 about 1-2%
Soul             1968-69 about 18%
R&B              1969 about 8%
Psychedelic Rock peak 1967 or, within tolerance, 1968
British Blues    grows through 1969
Gospel           rises across the decade
Jazz             gradually declines
Country          stable-to-rising without a 1966 spike-and-revert
```

These genre gates sit below the already accepted system gates. Any candidate
that reaches historical shares by breaking economy, reconciliation, release
capacity, label lifecycle, or deterministic boundaries fails.

## Validation ladder

1. Run fixed catalog, supply, population, format, and conservation probes.
2. Build with zero errors.
3. Prove the disabled path and RNG boundaries are unchanged.
4. Do not run Godot until separately authorized.
5. With authorization, run one full-telemetry diagnostic seed first. Use a
   previously measured tuning seed, not a fresh holdout.
6. Compare that seed with its old output using both the historical analyzer and
   the annual flow ledger.
7. Stop if economy, reconciliation, scheduled Albums, successful releases,
   label lifecycle, regional specialist gates, or the Single-lane analyzer
   regresses.
8. After one seed passes, run the second tuning seed.
9. Preselect and preserve a new untouched holdout seed. Seed 2007 is consumed
   and cannot serve as a fresh holdout.

## Definition of done

The pass is complete when:

- the initial ghost cohort and British bridge timing are resolved and measured;
- the 1966 flow ledger explains and repairs the R&B/Soul/Country/Traditional
  Pop reallocation;
- lifecycle tails decline or persist according to historical shape rather than
  a generic death cliff or no-death plateau;
- realized format ordering follows authored orientation without forcing any
  historically mixed genre into one lane;
- all 42 genres are reported against the adjusted target envelope;
- Psychedelic Rock has meaningful 1967 Single and Album commerce and no longer
  peaks in 1969;
- the accepted economic and lifecycle system remains passing;
- a genuinely untouched holdout confirms the result.

The implementation should be judged as a catalog-wide allocation,
lifecycle-tail, and product-format calibration. A one-off Psychedelic
keyframe, Traditional Pop penalty, or per-year market quota is not sufficient.
