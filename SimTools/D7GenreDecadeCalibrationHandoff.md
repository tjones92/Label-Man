# D7 genre decade calibration — live handoff

Last maintained: July 31, 2026 (chart-health pass). Branch `d7-genre-decade-calibration`, off merged `main`.

This is the working handoff for genre calibration. It supersedes
`D7LabelChartAccessSystemicRepairHandoff.md` as the *active* document — that file remains the
authority for the label chart-access arc (breadth 450, the consolidation arc, the distribution
channel, runtime optimization) and is closed and merged. Read it only for background on those
systems; do not extend it.

## 1. Scope

Calibrate the decade's genre makeup against history, on two axes that had never been separated:

- **market influence** — units across the whole live record population;
- **chart influence** — presence on the chart, which is not the same thing and diverges.

Non-goals this pass: the acclaim/legitimacy loop (§9), and any further work on label chart access.

## 2. State

| run | what it is |
|---|---|
| `d7-genre-decade-522-1001` | baseline decade, telemetry added, economics unchanged. Reproduces the accepted 450 breadth exactly. |
| `d7-comp-decade-522-1001` | + compilation era curve and the 1965 statement opening. Superseded; carried the §7 MidTier regression. |
| `d7-majorgate-decade-522-1001` | + the MidTier→Major chart gate (§7). **Current reference.** All acceptance rows pass. |
| `d7-comp-probes-52-1001` | 52-week probe run, D5 + D6 1-97 green. |
| `d7-sortfix-probes-52-1001` | 52-week probe; 67/68 artifacts byte-identical to `d7-comp-probes`, sole diff the repaired median column. |
| `d7-majorgate-probes-52-1001` | 52-week probe; **68/68 byte-identical** to `d7-sortfix-probes`, proving the gate inert in 1960. |

Validation ladder unchanged from the D7 handoff §33.15: build → D5/D6 probes via a 52-week run →
312-week checkpoint → decade → holdout seed. Never pipe a long Godot run through a PowerShell
pipeline; use `Start-Process -NoNewWindow -Wait -RedirectStandardOutput`. Do not rebuild while a run
holds the DLL.

## 3. The telemetry: `<run>-genre-decade-shape.csv`

One row per year per genre. Authored intent (`baseline`, `lifecycleState`, emergence/death), supply
(`newReleases`, `activeRecordsYearEnd`), market weight (`marketUnits`, `marketUnitsShare`), chart
weight (`chartRecordWeeks`, `chartWeekShare`, `uniqueChartingRecords`, `chartUnits`,
`top40/top10/numberOneRecordWeeks`, `meanChartPosition`), and `chartWeekShareMinusMarketShare`.

Read-only; safe on any run and deliberately not gated behind `--lean-probe`.

**`uniqueChartingRecords` next to `chartRecordWeeks` is the important pairing** — it separates a genre
charting *more records* from charting them *longer*, which is what identified the country problem.

### 3.1 Do not use `genre-market-weekly.csv` for chart share — it is region-scoped

`eligibleRecords`/`chartedRecords` in that file are per-region rows. Summing across the seven regions
weights each genre by **how many regions it charts in**, not by chart share. Adult national genres
score across all seven; soul charts in fewer, partly because `GetSegregationFactor` deliberately
restricts R&B/soul/gospel reach in white markets.

Two findings were retracted after being derived from it, both stated in earlier sessions and both
wrong: "easy listening holds 25.9% of the 1965 chart against 8.9% of units" (actually 10.1% vs 8.9%)
and "soul under-charts in 1967" (it over-charts, 18.0% vs 15.2%). The broad "adult genres over-chart,
youth under-chart along a slow/fast axis" thesis does not survive. **Use `genre-decade-shape.csv`.**

## 4. Shipped this pass

### 4.1 Compilation era curve

`GenerateAlbum` made every genre outside a hardcoded six-genre adult list a `Compilation`
unconditionally in every year — the era curve lived inside the adult branch and `!adultGenre`
short-circuited before any year test. Youth genres ran 82-96% compilation from 1960 to 1969, and
classical sat at 84% purely for not being on the list.

Two channels made it matter: compilations generate non-single tracks at `originalMaterialScale` .68
against .88, worth **+0.115 pooled appeal at equal cohesion** (Standard .587 vs Compilation .472 in
1969, at cohesion .496 vs .493 — an unconfounded contrast), and they cost .60x production against
2.4x.

Replaced with `AlbumModel.GetCompilationChance`: propensity is the catalog's authored
`SingleOrientation`, decayed by an era term weighted by per-family album-revolution susceptibility
(Rock/Folk/Jazz .80, RhythmAndSoul/Blues .55, Country/Gospel .45, Pop .12, else .30). Pop is low
because bubblegum was manufactured product for the singles market and never joined the album turn.

Result, 1969: Classical 84% → **9%**, PsychedelicRock 89% → **11%**, HardRock 85% → 13%, Soul 87% →
32%, Country 10% → 30%. Blended 47% (1960) → 24% (1969). Pooled appeal +0.030 (1960) to +0.047 (1969).

`CalculateCompilationCostWeight` now returns the same probability the roll uses; it previously
returned 1.0 for every non-adult genre, so the budgeting prior could not agree with the realised
format.

**1960 blended compilation fell 74% → 47%.** Mostly Classical, Gospel and Childrens leaving a bucket
they never belonged in. Judged acceptable: the 1960 LP market was adult-dominated and those records
genuinely were not hits-plus-filler. Flagged because it was not the intent going in.

### 4.2 The 1965 statement album

The era ramp cannot reach the 0.72 `statementViable` bar until 1967 and the pre-1965 fluke term it
replaced topped out near 0.55, so **no concept album was reachable anywhere before 1967** — the
artifacts confirm zero across 1960-66. A deliberately vanishing path now opens from 1965 for near-top
talent in a near-top room on the best few percent of rolls.

It fires zero times in 1965-66 on seed 1001, which is **accepted**: Pet Sounds and Freak Out! were not
concept albums the way Sgt. Pepper was, so 1967 as the effective start is right. The mechanism exists
and probe 97g proves it fires at the extreme; the population simply does not generate that pairing.
Whether label `productionQuality` tops out too low is a separate open question.

## 5. THE BIG ONE: a uniform one-year realization lag

Authored baseline peak year vs realized market-share peak year. **Median lag exactly +1 year**, and
it is not noise — thirteen genres land on +1: BritishBeat, BritishPop, Folk, SurfRock, FolkRock,
GarageRock, PsychedelicRock, Soul, Blues, Boogaloo, BossaNova, Comedy, DooWop.

The cause is the supply pipeline: labels must found, sign, produce and release into a demand curve
that has already moved.

**This is one systematic correction, not thirteen individual ones.** To land a realized peak in year
N the curve must be authored at N-1. Psychedelic rock is the worked example: authored 1967, realized
1968; author it 1966 to realize 1967.

Exclude from the comparison: Childrens/Classical/EasyListening (near-flat curves, "peak year" is
meaningless) and Jazz/RockAndRoll/TraditionalPop (peak in 1960, censored at the boundary).

Decide before implementing whether to shift the authored keyframes back a year or to shorten the
supply response. Shifting keyframes is the cheaper and more legible option; shortening supply
response is the more honest one and would also affect breadth.

## 6. Specific genre misalignments

Measured on `d7-comp-decade-522-1001`. Total market grows only 149.5M → 195.5M units (+31%) across
the decade, so **share movements are real genre movement, not denominator dilution** — an important
control, since it rules out the easy explanation for most of these.

| genre | issue | evidence |
|---|---|---|
| **Gospel** | far too high late | 1.5% → **7.0%**; absolute 2.2M → 13.7M, a 6x rise. `baseline1969 = 0.75` is the second-highest value in the entire catalog. Authoring error, not emergent. |
| **Soul** | too high | 18.0% at 1967; target **13-15%**. Soul+RnB combined reach 27.5% in 1969, which is above the ~20-25% the singles market plausibly supports. Late-decade level (~19%) is defensible on its own — Aretha, Otis, Sly — the 1967 level is not. |
| **Bubblegum** | too low | 1.3% at 1969. 1969 was peak bubblegum; "Sugar, Sugar" was the year's biggest single. Needs to rise. |
| **Jazz** | declines too steeply | 8.6M → 3.4M absolute, a 60% fall, against an authored decline of only 47% (.45 → .24). Wanted low but **steady**, with late-60s proto-fusion. |
| **Comedy** | shape inverted | 1.2M (1960) → 4.2M (1963). The 1963 peak is right (*The First Family*), the 1960 trough is not — comedy LPs were a hi-fi staple and *Button-Down Mind* was the best-selling album of 1960. |
| **Classical** | unexplained 1964 peak | 4.3M → **10.1M (1964)** → 6.6M, against a **flat** authored baseline of 0.40 in every year. Emergent, not authored, and not seed noise. |
| Folk / Surf / Garage | peak 1-2 years late even before the §5 lag | folk revival peaked 1963, surf died with the Invasion in 1964, garage peaked with "96 Tears" in 1966. |

### 6.1 Likely common cause for Comedy and Classical: the album era starts at zero

Both are the most album-oriented genres in the catalog (`SingleOrientation` .15, `GetAlbumAffinity`
.82 and .88) and both show the same signature — suppressed in 1960, surging into a mid-decade peak.
`AlbumModel.GetAlbumEraWeight` ramps from **0 at 1960** to 1 at 1968, so the album market barely
exists in 1960.

Historically the 1960 LP market was already substantial — it was simply *adult*: classical, jazz,
comedy, Sinatra, Broadway cast albums. What grew across the decade was the *youth* album market. A
single era weight applied to all genres cannot express that, and it is the most likely single cause
of both the Comedy inversion and the Classical hump. **Investigate before touching either genre's
keyframes**, or the fix will be applied at the wrong layer.

## 7. CLOSED: MidTier firms 27 → 21 → 27

Resolved by gating MidTier→Major on chart evidence. Reference `d7-majorgate-decade-522-1001`.

| target | baseline | comp | **majorgate** | |
|---|---:|---:|---:|---|
| breadth 400-600 | 450 | 491 | **493** | PASS |
| below-MidTier dominant | 92.7% | 92.9% | **92.9%** | PASS |
| Independent share of that | 77.9% | 74.6% | **74.9%** | PASS |
| Small tail | 8.7% | 10.6% | **10.8%** | PASS |
| **MidTier firms 25-40** | 27 | **21 FAIL** | **27** | **PASS** |
| owner-Major 1968 (45-52) | 46.5 | 49.2 | **47.2** | PASS |
| owner-Major 1969 (45-52) | 53.2 | 51.4 | **48.8** | PASS |
| Major firms 1969 | 13 | 16 | **10** | — |

The prediction that 2.3x album costs would squeeze marginal Independents and cost breadth was
**wrong** — breadth rose. Better albums sell more, and that outweighed the cost increase.

### 7.1 The §7 hypothesis was wrong

The hypothesis was that breadth thinned the tier from both ends as per-label charting counts fell
against a promotion bar of 8 and a demotion bar of 4. **The decision telemetry refutes both ends.**
Measured at the moment of each transition, in both runs:

| | comp | majorgate |
|---|---|---|
| Indep→MidTier, charting at promotion | min 8, med 8, max 15 | unchanged |
| MidTier→Indep, charting at demotion | med 2, max 3 | unchanged |
| MidTier live charting (wk 313+) | mean 7.17, med 7 | — |
| MidTier share ≥ 8 (the promotion bar) | **49.3%**, up from 43.5% | — |
| Independent live charting | **mean 1.32**, up from 1.21 | — |

Independents charted *more*, not less; MidTier labels sat *further above* the promotion bar, not
closer to the demotion bar. The MidTier bars were never the mechanism and were left untouched.

### 7.2 The actual mechanism: the top rung had no chart gate

`TryPromoteLabel` gated MidTier→Major on `sustainedCapabilityQuarters >= 4 && CurrentRosterSize >= 25
&& CanSupportMajorBranches` — capability, headcount and twelve months of runway, with **no
`chartingLastYear` term**. It was the only rung of the ladder without one. So the compilation curve
raised label profitability and labels walked into Major on their books alone.

The flow ledger closes the arithmetic exactly:

| | baseline | comp | majorgate |
|---|---:|---:|---:|
| Independent → MidTier | 36 | 35 | 38 |
| MidTier → Independent | 38 | 39 | 42 |
| **MidTier → Major** | 5 | **8** | **2** |

comp: in −1, demote-out +1, graduate-to-Major +3 = −5, against a standing move of 27 → 22. Major
standing rose 13 → 16, +3, closing the other side. The graduating cohort's median charting evidence
*fell* 13 → 8 (one promoted on 2), so the extra Majors were weaker, not stronger.

`MajorPromotionChartingRecords = 16` now gates it. MidTier labels run a median of 8 recent charting
records (p90 16, p95 20) while live Majors carry 23-51 entries a year, so 16 is a label knocking on
the door — reachable, rarely reached. Cash was left alone: it never bound anything, every graduate
held 0.75-3.6M against a ~96k requirement. Probes 95i/95j pin the ladder monotonic in chart evidence.

Result: 2 graduations, both 1963, at 16 and 17 charting records — and both the same labels that
graduated in 1961-62 under comp, held back until they earned it. The route works, it just sequences
correctly now.

**owner-Major note.** The comp run's "first time in band" at 51.4 was partly riding on its 3 extra
Major firms; removing them costs ~2.6 points. The band holds anyway (48.8), which is the stronger
result, and the trajectory keeps the §29 shape — 42.2 at 1960 rising into band by 1968-69 rather than
a flat high line. Any future owner-Major movement should be checked against the Major firm count
before being banked.

### 7.3 Telemetry defect fixed alongside

`closedTop40Median` in `decade-annual-rollup.csv` reported the chart life of one arbitrary record.
`Statistic()` requires a sorted list; `albumAges` and `albumUnits` are sorted before the call but
`ClosedTop40Weeks` accumulates in closure order and was passed raw. The column read
`3,3,4,6,4,6,17.5,1,13,11` against a true median of `9,9,8,8,8,8,6,7,8,8`. Confirmed by replaying the
unsorted middle-index calculation, which reproduced all eleven published values byte-for-byte.
**Any conclusion drawn from that column before 2026-07-31 is void.**

## 8. RETRACTED: country over-charts via longevity. It is soul, and it is volume.

**The §8 table was measured on `d7-genre-decade-522-1001`, the pre-compilation baseline, and was
never re-derived against the reference run.** The compilation curve eliminated the country divergence
and created a much larger soul one. Measured on `d7-comp-decade-522-1001`:

| Country divergence (chart% − market%) | 1960 | 1963 | 1966 | 1967 | 1968 | 1969 |
|---|---:|---:|---:|---:|---:|---:|
| baseline (what §8 reported) | +0.6 | +2.1 | +7.3 | +7.9 | +8.3 | +6.9 |
| **current** | −0.3 | +0.6 | +1.1 | **−0.2** | +2.0 | **−0.5** |

| Soul divergence | 1960 | 1963 | 1966 | 1967 | 1968 | 1969 |
|---|---:|---:|---:|---:|---:|---:|
| baseline | −2.4 | −1.1 | +1.6 | +2.8 | +4.9 | +4.5 |
| **current** | −2.5 | −0.4 | +3.7 | **+10.4** | **+10.8** | **+13.5** |

Country's unique charting records fell 186 → 90 at 1967 and its longevity index against the chart
average is now **1.04** — dead at par. The cause is §4.1: the Country family carries a low .45
album-revolution susceptibility, so the compilation curve moved country the *wrong* way (10% → 30%
compilation) and cost it appeal, while soul went 87% → 32%.

**Soul now over-charts mainly on volume, not longevity** — the opposite decomposition to the one §8
described:

| | 1960 | 1965 | 1967 | 1969 |
|---|---:|---:|---:|---:|
| soul share of all charting records | 5.9% | 12.8% | 23.3% | **24.8%** |
| soul longevity index (vs chart mean) | 0.65 | 1.17 | 1.22 | **1.30** |

A quarter of every record on the chart, held 30% longer than average. This stacks on top of §6's
separate finding that soul's *market* share is already too high — two distinct problems, and the §6
authoring fix addresses only one. **Do not act on either until §11 lands**, since making airplay
load-bearing will redistribute genre chart share.

Psychedelic rock under-charts (3.0% market, 1.2% chart weeks in 1969, weeks/record *falling* 5.2 →
3.5). Arguably correct for an album genre whose singles do not linger.

## 9. Deferred: the acclaim loop

`AlbumModel.GetMaximumAchievableCohesion` is a purely exogenous year ramp. The Rubber Soul → Pet
Sounds → Sgt. Pepper knock-on was **scoped in a comment and never built** ("Exogenous curve. A future
acclaim/legitimacy loop may add a bounded nudge"). `SimulatedArtist.groupCohesion` is unrelated — that
is band interpersonal chemistry feeding talent.

Consequence, and it is expected rather than a defect: `thematicCohesion` is pinned at exactly **0.080,
the clamp floor, for every album from 1960 through 1965**. The ceiling does not clear the floor until
1966, so even a perfect artist at a perfect label computes 0.077 in 1965. This is the dimension the
acclaim loop will animate.

**Deferred to its own directive after genre balancing is finished**, by user decision. When built it
should be a bounded feedback where a high-cohesion album that *succeeds commercially* lifts the
achievable ceiling for later albums, so the escalation is earned rather than scheduled.

## 10. Resume sequence

1. ~~MidTier 27 → 21.~~ **Done** — §7, `d7-majorgate-decade-522-1001`, all acceptance rows pass.
2. **Airplay (§11).** Blocking on everything genre-side, by user decision: making airplay
   load-bearing will move genre chart share, so calibrating authoring first would be tuning against a
   chart that is about to change.
3. **The one-year lag (§5).** Decide keyframe shift vs supply response, then apply once across the
   catalog.
4. **Album era weight (§6.1)** — investigate before touching Comedy or Classical keyframes.
5. **Per-genre authoring:** Gospel down hard, Soul down (§8 — now a chart problem as well as a market
   one), Bubblegum up, Jazz flattened, Folk / Surf / Garage pulled earlier.
6. Re-run decade, re-check the §7 acceptance table, then a holdout seed.

Deferred: the acclaim loop (§9).

## 11. Chart health and the inert airplay term

Chart health was decomposed on 2026-07-31 for the first time in the arc. Turnover, breadth and volume
are stable; **record persistence is not**, and one defect explains all of it.

### 11.1 The misses

| metric | history 1960-69 | current | band |
|---|---:|---:|---|
| distinct #1 records | 203 | **380** | — |
| #1s holding exactly one week | 55 (27%) | **293 (77%)** | — |
| #1s holding 3+ weeks | 84 (41%) | **6 (1.6%)** | — |
| Top-40 median chart life | — | **8** | 10-13 |
| new Top-100 entries/wk | — | 20.6 | 16-21 PASS |
| quality→position Pearson | — | 0.355 | 0.45-0.62, see 11.4 |

Longest #1 run in the entire decade is 4 weeks.

### 11.2 Root cause: airplay contributes 0.18% of chart points

`ChartSimulator.CalculateChartPoints` returns `salesPoints + airplayPoints * 0.15f`. Measured across
156 weeks of `records.csv`, airplay is **0.18% of the #1 record's points** (max 0.28%). The chart is a
pure weekly-sales chart.

`ChartSimulator.cs:735` uses `region.population` raw. Population is authored in **millions** (east
coast 52.2, deep south 15.0) and every other absolute consumer multiplies by 1,000,000 —
`MarketRegion.cs` 104/124/131/169/225, `SingleOpportunityLedger.cs:25`, `ChartManager.cs:992`. Line
735 is the only place a millions-scaled value is summed against `unitsThisWeek`, an absolute record
count. The other raw uses (`population / 50f`, `population * 700f`, `collegeCount / population`) are
deliberate density ratios.

**A straight ×1,000,000 overshoots by ~1000x** and would make airplay swamp sales entirely. Reaching
a ~40% airplay share needs roughly **600-900x** the current contribution, so the `25f` and `0.15f`
constants must be re-derived together, not merely unit-corrected.

### 11.3 Why that produces one-week #1s

Position tracks weekly sales, and hits here are spikes rather than plateaus. Over the 135 records that
reached #1 in the diagnostic run:

| | |
|---|---:|
| week before peak, as share of peak | 65.8% |
| week after peak, as share of peak | 65.1% |
| single best week, as share of the record's whole chart run | 22.4% |
| held #1 one week / two / three | 115 / 19 / 1 |

To hold #1 twice a record's *second*-best week (~65% of peak) must beat every rival's peak week. It
almost never does, so leadership passes down a conveyor belt of records each cresting once. **Airplay
is the term that would flatten the top of that trajectory, and it is inert.**

### 11.4 The plateau engine is already built

Do not rebuild it. `ChartSimulator.UpdateRadioHeat` already carries quality/push/momentum/artistHeat
inputs, a **+0.25 top-10 / +0.10 top-40 chart-position bonus**, `RADIO_FATIGUE_DECAY 0.88^(weeks-8)`
burnout, and an asymmetric lerp (up 0.28, down 0.10 → 0.22 after week 12). Regional `radioPlay` is
seeded at `ChartManager.cs:736`, aged at `:1482`, and spread to neighbours at `:1736`/`:1759`, with
`GetRadioDifficulty` and `GetRegionalRadioOpportunity` shaping it. Its only current consumer is
awareness (`ChartManager.cs:1484`). It never reaches rank.

Four things to settle, in order of risk:

1. **Weight** — what share of chart points airplay carries, and whether it ramps across the decade
   (Top 40 radio's influence grew through the 60s).
2. **Persistence** — sales fall to 65% of peak in one week; airplay must decay slower than that or
   there is no plateau. `Lerp(radioPlay * 0.85f, target, 0.2f)` at `ChartManager.cs:1482` is the knob
   and was never tuned under load.
3. **The top-10 feedback loop** — harmless today, **positive feedback** once airplay is load-bearing:
   top 10 → more airplay → more points → stays top 10. This is what manufactures 3+ week #1s and also
   the most likely way to overshoot into records locking at #1. Gate it behind sales.
4. **Genre redistribution** — `genreRadio`, `radioDifficulty` and the segregation factor all shape
   airplay and none currently affect rank. Expect genre chart shares to move, plausibly a lot. This is
   why §8 and §6 wait.

On the Pearson band: quality→position correlation fell 0.51 → 0.345 at the §13.4 tier-population
repair and has held ~0.35 since. That is the expected signature of position depending on tier and
regional reach rather than mostly on intrinsic quality. **The 0.45-0.62 band was measured on the
pre-repair concentrated chart and should be restated, not chased.**

### 11.5 Diagnostics

These four pin the problem and are the acceptance test. All need a run **without `--lean-probe`** so
`records.csv` is populated (`d7-tier-population-diag-156-1001` was used here):

| what | source | now | want |
|---|---|---:|---:|
| airplay share of #1's chart points | `records.csv`, `chartPoints` − `unitsThisWeek` | 0.18% | a real share |
| peak sharpness (week after ÷ peak) | `records.csv` units series | 0.651 | flatter |
| #1 tenure distribution | `lifecycles.csv` `weeksAtNumberOne` | 77% one-week, 6 at 3+ | 27%, ~84 |
| Top-40 median life | `lifecycles.csv`, `peakPosition<=40` | 8 | 10-13 |

Plus `genre-decade-shape.csv` for the per-genre longevity index
(`chartRecordWeeks / uniqueChartingRecords` against the chart mean) to catch item 4 above.
