# D7 genre decade calibration — live handoff

Last maintained: August 1, 2026 (the discrete station drop, §12.4t, and the build-only decade control
that reset every acceptance target, §12.4s). Branch `d7-genre-decade-calibration`, off merged `main`.

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
| `d7-majorgate-decade-522-1001` | + the MidTier→Major chart gate (§7). **Last run passing every acceptance row.** |
| `d7-v5verify-decade-522-1001` | + load-bearing airplay (§11), the state committed at `e411a65`. **Current reference.** Big chart gains, two marginal band failures — see §11.6. |
| `d7-airplay5-52-1001` | 52-week probe of the **shipped** airplay config, **`records.csv` populated**. The only artifact that can carry the §12.1 peak decomposition; the decade runs are `--lean-probe` and write an empty `records.csv`. |
| `d7-comp-probes-52-1001` | 52-week probe run, D5 + D6 1-97 green. |
| `d7-sortfix-probes-52-1001` | 52-week probe; 67/68 artifacts byte-identical to `d7-comp-probes`, sole diff the repaired median column. |
| `d7-majorgate-probes-52-1001` | 52-week probe; **68/68 byte-identical** to `d7-sortfix-probes`, proving the gate inert in 1960. |
| `d7-buildonly-decade-522-1001` | **The control for the current committed state** (`01b742a`) — airplay build, no burnout. §12.4s. Every earlier "decade" figure for the shipped state came from `d7-phase-decade`, which carried the rejected burnout, and was wrong. |
| `d7-drop1-52-1001` | 52-week probe of the station drop. Comparable to `d7-buildonly-52-1001` (neither carries a probe suite). |
| `d7-drop-probes-52-1001` | 52-week probe-suite run for the drop. D5 green, D6 **1-98** green. Never compare it against a run without the suites — §12.4b. |
| `d7-drop-decade-522-1001` | **Current reference.** The discrete station drop, §12.4t. Returns 20.7% → 12.8%; two label bands and Soul pay for it. |

Rejected airplay variants, all decade-run, all kept for the §11.5 comparison:
`d7-airplay-decade-522-1001` (convexity on the whole product), `d7-heat-decade-522-1001` (earned heat
on national units), `d7-reg-decade-522-1001` (earned heat on regional units per capita).

Validation ladder unchanged from the D7 handoff §33.15: build → D5/D6 probes via a 52-week run →
312-week checkpoint → decade → holdout seed. **A 52-week run cannot see breadth accumulate, so no
chart-mechanism change may be committed on 52-week evidence alone** — see §11.6. Never pipe a long
Godot run through a PowerShell
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

### 8.1 CLOSED: country. The problem was chart longevity in general, and airplay repaired it.

Re-derived on `d7-v5verify-decade-522-1001` against `d7-majorgate-decade-522-1001`. The original §8
country finding was a *relative* measurement — country's weeks-per-record against a chart mean that
was itself far too short. Raising the chart-wide level dissolved it.

| chart-wide | 1960 | 1963 | 1966 | 1967 | 1969 |
|---|---:|---:|---:|---:|---:|
| mean weeks per charting record, no airplay | 5.08 | 4.78 | 4.44 | 4.56 | 4.88 |
| **with airplay** | **5.74** | **5.20** | **5.16** | **5.43** | **5.80** |
| unique records charting per year, no airplay | 1023 | 1088 | 1170 | 1140 | 1065 |
| **with airplay** | **906** | **1000** | **1007** | **957** | **896** |

Top-40 median chart life went 8 → 10 (band 10-13) and Top-10 median 10 → 15 over the same change.

**Country is now at par and needs no genre-side work.**

| Country | 1960 | 1963 | 1966 | 1967 | 1968 | 1969 |
|---|---:|---:|---:|---:|---:|---:|
| longevity index, no airplay | 1.08 | 1.10 | 1.19 | 1.27 | 1.07 | 1.08 |
| **with airplay** | 1.10 | 1.13 | **1.02** | **1.08** | **0.98** | 1.17 |
| divergence, no airplay | −0.3 | +0.4 | +2.0 | +2.2 | +0.2 | +1.3 |
| **with airplay** | +0.6 | +1.5 | **+0.1** | **+0.3** | **−0.4** | +1.4 |

### 8.2 CORRECTION: airplay did **not** improve Soul overall. It moved the problem earlier.

§11.6 reported Soul's divergence improving +12.8 → +9.5 and credited airplay. That is true of 1969 and
of no other year:

| Soul divergence | 1965 | 1966 | 1967 | 1968 | 1969 | mean 66-69 |
|---|---:|---:|---:|---:|---:|---:|
| no airplay | +0.7 | +4.4 | +7.4 | +9.4 | +12.8 | +8.5 |
| **with airplay** | +0.9 | +5.6 | **+11.4** | **+11.8** | +9.5 | **+9.6** |

Averaged across 1966-69 it is slightly **worse**, and Soul's longevity index rose 1.23 → 1.40 at 1967.
Soul is now both the volume problem §8 describes *and* the chart-longevity problem §8 said country
was. Reading a single year off a genre table is what produced the wrong conclusion; read the span.

### 8.3 NEW: airplay is a genre stratifier, and two genres moved badly

Airplay widened the spread of the longevity index across genres (1967: 0.36-1.27 → 0.37-1.61). Some of
that is correct — radio access *should* separate genres — but two movements are wrong:

- **Gospel 1969**: longevity index 1.08 → **1.31**, divergence +1.9 → **+5.8**, now holding **13.3% of
  1969 chart weeks**. This stacks on the §6 authoring error (`baseline1969 = 0.75`, market share
  1.5% → 7.0%). Gospel is now the worst single genre miss in the run.
- **PsychedelicRock 1967**: longevity index 0.99 → **1.61**, divergence −0.7 → **+1.6**. The §8 note
  above — that psych *under*-charting is defensible for an album genre whose singles do not linger —
  no longer describes the run. Its singles now linger 1.6x the chart average.

Both are consequences of §11.4 item 4 (genre redistribution), which was anticipated but not measured
until now. **Re-check both after the §12 sales-curve change**, since it moves every record's chart
life and may absorb some of this on its own.

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
2. ~~Airplay (§11).~~ **Shipped at `e411a65`.** Keep-or-revert **resolved: keep** (§11.6.3). Two band
   failures remain open and are one mechanism, not two — read §11.6.2.
3. ~~Measure the sales curve (§12).~~ **Done.** The fall is the launch boost; saturation and age decay
   are exonerated; 87.6% of records debut at their peak position. Read §12 before touching demand.
   - ~~Flatten the launch window.~~ **Shipped** — the release ramp, §12.4a.
   - ~~Steepen the chart's dynamic range.~~ **Shipped** — the Hesbacher curve, §12.4d.
   - ~~Restore chart turnover.~~ **Shipped** — the survey layer, §12.4h.
   - ~~Reopen MidTier promotion.~~ **Shipped at `9757a3b`**, §12.4j / §12.4n.
   - ~~Phase airplay onto the release ramp.~~ **Build half shipped at `9757a3b`**, §12.4p / §12.4r.
   - ~~The discrete station drop.~~ **Built and run, uncommitted** — §12.4t. Returns 20.7% → 12.8%,
     one-week #1s exactly on 27%, week-20 airplay share 45.3% → 12.1%, units held at 99.8, re-add rate
     0. Costs: MidTier 1969 40 → 51 and owner-Major 1969 44.4 → 40.2 (1968 came *into* band), Soul
     ~1.5 points worse. Read §12.4s first — it is the control, and it moved every target §12.4r set.
   - **NEXT: `CHART_EXPOSURE_EXPONENT`.** It is now the named lever for both standing failures —
     owner-Major (§12.4t) and debut position (§12.4k/§12.4r) — and it is entangled with Soul, so
     sequence the Soul authoring fix (item 7 below) first, exactly as §12.4r said.
4. **Album era weight (§6.1)** — investigate before touching Comedy or Classical keyframes. Two
   separate pieces of evidence now point at it: the Comedy market inversion, and Classical charting on
   a *singles* chart at all. `GetAlbumEraWeight` ramps from 0 at 1960, and Classical and Comedy are the
   two most album-oriented genres in the catalog (`SingleOrientation` .15, album affinity .88 and .82),
   so with no album market to release into their output is pushed onto the singles chart.
5. **Integration era curve by region.** `currentIntegration` is assigned once at `MarketRegion.cs:98`
   from the authored `integrationLevel` and **never updated** — frozen at 1960 for the whole decade. It
   damps Soul and RnB acceptance growth (`GetYearEvolution` scales both by `0.5 + currentIntegration *
   0.5`) and feeds `GetSegregationFactor`. This matters more now that airplay is load-bearing and its
   weight ramps across the decade while integration stands still. Do this **before** any soul-side genre
   authoring, or the keyframes get calibrated against a segregation level that is about to move.
6. **The one-year lag (§5).** Decide keyframe shift vs supply response, then apply once across the
   catalog.
7. **Per-genre authoring:** Gospel down hard (now the worst miss — §8.3, 13.3% of 1969 chart weeks),
   Soul down (§8.2 — a longevity problem *and* a market problem, and airplay made 1967-68 worse, not
   better), Bubblegum up, Jazz flattened, Folk / Surf / Garage pulled earlier.
   EasyListening is too low for a hi-fi era staple. Classical should be near zero on a singles chart.
8. Re-run decade, re-check the §7 acceptance table **and** §11.7, then a holdout seed.

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

**Basis note:** the "distinct #1 records" row above is per record and comparable to history; the two
percentage rows beneath it were computed per *run* and are not. See §11.6.1 — measure #1 tenure per
record, from `weeks.csv` grouped by `numberOneRecordId`, never by run length.

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

Four more instruments were built on 2026-07-31 and are the fastest reads available. The first three
run on a **lean decade run** — no `records.csv` needed:

| what | source | now |
|---|---|---:|
| **#1 tenure, per record** | `weeks.csv`, group by `numberOneRecordId`, count rows | 226 records, 54.0% one week |
| **debut-at-peak share** | `lifecycles.csv`, `debutPosition == peakPosition` | **87.6%** |
| **chart-wide longevity** | `genre-decade-shape.csv`, Σ`chartRecordWeeks` ÷ Σ`uniqueChartingRecords` per year | 5.05-5.80 wk |
| **peak decomposition** | `records.csv` (needs a non-lean run) — see §12.1 | launch boost = 0.6995 of a 0.6970 fall |

Do **not** compute #1 tenure by run-length over `weeks.csv`; that is the §11.6.1 error.

### 11.6 SHIPPED. Read this before touching airplay again.

Committed at `e411a65`. Decade reference `d7-v5verify-decade-522-1001`.

#### 11.6.1 MEASUREMENT CORRECTION: #1 *runs* were compared against a #1 *records* history

The table originally published here counted **consecutive spells at #1** from `weeks.csv`
(`numberOneRecordId` run-length) and compared them against a historical figure — 203 #1s, 27% at one
week, 41% at 3+ — that counts **records**, summing a record's weeks at #1 across all its spells. The
two are not the same statistic and the gap between them is not small: 43 of 226 chart-toppers return
to #1 after being displaced, so the run-based count is 279 against a record-based 226, and every
returning record contributes two one-week runs instead of one two-week record.

Restated per record over 1960-69 (521 weeks), which is the basis history uses:

| metric (per record) | no airplay | **shipped** | history 1960-69 |
|---|---:|---:|---:|
| distinct #1 records | 406 | **226** | 203 |
| holding one week | 75.1% | **54.0%** | 27% |
| holding two weeks | 21.7% | 22.1% | ~32% |
| holding 3+ weeks | 3.2% | **23.9%** | 41% |
| longest run | 4 | **12** | 9 |
| **mean weeks at #1** | 1.28 | **2.31** | **2.57** |
| Top-40 median life | 8 | **10** | 10-13 PASS |
| Top-10 median life | 10 | **15** | — |
| entries/wk | 20.71 | 17.98 | 16-21 PASS |
| breadth | 493 | 406 | 400-600 PASS |
| **MidTier firms** | 27 | **23** | **25-40 FAIL** |
| owner-Major 1968 | 47.2 | 48.2 | 45-52 PASS |
| **owner-Major 1969** | 48.8 | **52.7** | **45-52 FAIL** |

On the corrected basis the airplay change is a **larger** win than first reported: distinct #1s
406 → 226 against a 203 target, and mean tenure 1.28 → 2.31 weeks against 2.57. Note the identity —
521 weeks ÷ 203 records = 2.57 — so *the count target and the mean-tenure target are the same
requirement*, and it is now nearly met.

**What remains is purely distributional, and it is a variance problem, not a level problem.** At the
right mean we hold 54% of #1s for one week against a historical 27%, and 24% for 3+ weeks against 41%.
The middle of the distribution (3-6 weeks) is what is missing: the tail already exists (five records at
10 weeks, one at 12). Any fix must move roughly 55 records out of the one-week bucket **without**
raising the mean, which already sits close to target.

#### 11.6.2 The two band failures are one mechanism, and it is not marginal

The failures were called marginal (MidTier 2 firms short, owner-Major 0.7 points over). The *outcome*
is marginal; the *mechanism underneath it* is large, and it is visible in the tier composition of
chart entries (`concentration.csv`, majorgate → v5verify):

| 1969 | no airplay | shipped | |
|---|---:|---:|---|
| total chart entries | 1065 | 896 | −16% |
| Independent entries | 485 | **316** | **−35%** |
| MidTier entries | 231 | 200 | −13% |
| Major entries | 340 | **368** | **+8%** |
| Major share of entries | 31.9% | **41.1%** | |
| Independent firms charting | 181 | 137 | |

Airplay is a **major-label advantage amplifier**. Measured over the charting record-weeks of
`d7-airplay5-52-1001`, airplay is **45.3% of a Major's chart points and 20.9% of an Independent's**,
while median weekly units are near-identical across tiers (Major 8,441, Independent 7,320, MidTier
7,173) — which is exactly what "the chart's label composition was a fixed point of a sales-only
ranking" meant in §11.7. Median `radioHeat` is 0.525 for Majors against 0.427 for Independents, a
1.23x gradient; `AIRPLAY_CONVEXITY = 5` turns 1.23x into ≈2.8x before the coefficient is applied.

`RADIO_LABEL_WEIGHT = 0.4` makes label push roughly 40% of the pre-acceptance heat target, so **the
exponent is applied to a variable that is substantially a label-tier signal, not a record-quality
signal.** The `ChartSimulator.cs:68` comment already concedes the signal is "mostly generic"; what it
does not say is which way the generic part leans.

**owner-Major did not fail in 1969.** It rose in all ten years (1960 42.2 → 46.1, 1962 37.0 → 48.1,
1966 41.0 → 47.8). The §29 shape is preserved — the slope is +6.6 points across the decade in both
runs — but the whole line moved up ~5 points, and 1969 is simply the year already nearest the ceiling.
Diagnosing this as a 1969 problem would be diagnosing the wrong thing.

**MidTier lost firms from the top of the funnel, not the bottom.** MidTier chart entries *per firm*
are essentially unchanged at 1969 (8.56 → 8.70), so incumbent MidTier labels are not sliding toward
the demotion bar. What moved is the feeder: Independent entries per firm 2.68 → 2.31 on a firm count
that itself fell 181 → 137, so far fewer Independents can reach the 8-record promotion bar. This is
consistent with §7.1 and **again confirms the MidTier bars are not the mechanism** — the third time
that hypothesis has failed.

#### 11.6.3 Keep it — but `AIRPLAY_CONVEXITY` is provisional

The open keep-or-revert decision resolves toward **keep**: the corrected table shows the chart result
is close to history on count and mean tenure, general chart longevity rose across the board (§8.1),
and the country divergence that opened this arc closed. Reverting returns a pure weekly-sales chart,
which is unphysical.

But `AIRPLAY_CONVEXITY = 5` was chosen to manufacture a plateau the sales curve refused to provide
(the `ChartSimulator.cs:59-75` comment says so). §12 now shows the sales curve can be made to provide
one directly. **Once it does, the exponent no longer has to carry the plateau, and it should come
down — which is also what gives Independent entries, MidTier headcount and owner-Major back.** Do not
tune the exponent against the label table before the sales curve is fixed; they are the same knob
seen from two sides.

### 11.7 Every airplay shape moves the label table

Four decade-run variants, and **none preserves all four label bands**:

| variant | breadth 400-600 | MidTier 25-40 | ownMaj 68 | ownMaj 69 | chart |
|---|---:|---:|---:|---:|---|
| **shipped** (convex k=5 on record rotation) | 406 | **23** | 48.2 | **52.7** | 279 runs, 18% at 3+ |
| convexity on the whole product | 422 | **17** | 49.9 | 50.6 | 311 runs, 15% at 3+ |
| earned heat, national units | **354** | 36 | **43.1** | 48.7 | 273 runs, 23% at 3+ |
| earned heat, regional units per capita | 407 | **43** | **40.7** | **43.4** | 323 runs, 13% at 3+ |

These are not four bugs, they are one fact: **the chart's label composition was a fixed point of a
sales-only ranking, and airplay moves it.** Concentrating airplay on high-acceptance genres collapses
MidTier; tying rotation to national sales starves indies of airplay and costs breadth; tying it to
regional sales floods MidTier and sinks owner-Major. Any future airplay work must be adjudicated on
the label acceptance table at decade scale, not on chart health alone.

Two mechanics are worth keeping from the rejected variants even though the variants failed:

- **Convexity must not touch the genre channel.** Genre acceptance already enters rotation twice
  (through `radioHeat` and through `GetRegionalRadioOpportunity`), so raising the product to a power
  compounds a genre disadvantage to roughly the sixth power. Over a decade that drove Soul to **+26.4**
  divergence and RnB to −4.7. The shipped version divides access out, applies the exponent to the
  record's own rotation, and repays access linearly.
- **`radioHeat` must stay generic.** It multiplies conversion rate directly in
  `CalculateRegionalSales`, so recomposing it moves the demand model rather than the chart. The earned
  signal, if it is ever reintroduced, belongs in the regional radio pass.

### 11.8 Calibration facts worth not re-deriving

- `radioHeat` separates a #1 from the bottom of the chart by only **1.48x**; sales separate them by
  **8.12x**. Once the spread is measured the convexity exponent stops being free: it is fixed by
  requiring `spread^k ≈ 8.12`.
- Heat is heavily damped by its lerp — an instantaneous target spread of ~3.7x reaches the chart as
  1.76x. At the old rise rate of 0.28 a record could not climb during the one or two weeks its sales
  peak, so #1 records measured *lower* mean heat (0.679) than top-ten records (0.722). The rise is now
  0.55 and the fall is untouched; that asymmetry is the plateau.
- Sales per week by band, used to set every sales gate: top-10 median 31,199 (p10 20,918), 11-40
  median 13,419, 41-100 median 6,757.
- Entries/week ≈ 100 slots ÷ mean chart life. **The 16-21 entry band and a 10-13 week Top-40 life are
  in arithmetic tension**; both cannot be tightened at once.
- The 0.45-0.62 Pearson band predates the §13.4 tier-population repair and should be restated, not
  chased. It has sat near 0.35 since that repair, which is the expected signature of position
  depending on tier and regional reach rather than mostly on intrinsic quality.

## 12. THE SALES CURVE: measured. It is the launch boost, and nothing else.

The decomposition §12 previously asked for has been done, on the 99 top-10 records of
`d7-airplay5-52-1001` (the shipped airplay configuration at 52 weeks; `records.csv` is populated
there, and this is a within-record weekly mechanism, so the "no chart change on 52-week evidence"
rule — which exists because a 52-week window cannot see cumulative breadth — does not bind).

### 12.1 The decomposition

Geometric-mean week-over-week ratio of every term in `CalculateRegionalSales`, taken across each
record's own sales peak:

| term | ratio | reading |
|---|---:|---|
| **observed units** | **0.6970** | the 30% fall |
| **launch boost** | **0.6995** | **the entire fall** |
| chart visibility | 0.8351 | enters only through the awareness odds, see below |
| momentum | 0.9639 | |
| **saturation / exhaustion** | **0.9821** | **1.8%** |
| **age decay** (`DEMAND_AGE_DECAY_RATE`) | **0.9924** | **0.8%** |
| awareness (post-odds-transform) | 0.9937 | |
| radio heat | **1.0294** | radio is *rising* across the peak |
| modelled product | 0.6723 | |
| residual (stock / capacity / regional mix / jitter) | 1.0368 | |

**Both suspects named in the old §12 are exonerated.** `SATURATION_POWER` contributes 1.8% of the
fall and `DEMAND_AGE_DECAY_RATE` 0.8%. There is no audience exhaustion to speak of: **median
saturation at the sales peak is 0.0030** — a hit has reached three-tenths of one percent of its
potential audience. The reachable audience is not too small and is not refilling too slowly. It is
barely touched.

The whole fall is `CalculateRegionalSales` step 6:

```
weeksSinceRelease <= 1 : 2.0 + push * 2.5      (3.25 at push 0.5)
weeksSinceRelease <= 2 : 1.5 + push * 1.0      (2.00)
weeksSinceRelease <= 3 : 1.2 + push * 0.4      (1.40)
otherwise              : 1.0
```

Measured `rLaunch` at the wk1→2 transition is 0.615 and at wk2→3 is 0.700, which reproduces that table
at push ≈ 0.5 exactly.

### 12.2 The real defect: there is no climb, and the plateau already exists underneath

Mean weekly sales as a share of each record's own peak, top-10 records, with mean chart position:

| week since release | 1 | 2 | 3 | 4 | 5 | 6 | 8 | 10 | 12 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| sales, % of own peak | 79.9 | **95.3** | 61.1 | 36.4 | 31.4 | 28.9 | 27.3 | 28.0 | 20.4 |
| mean chart position | 6.2 | **4.5** | 7.5 | 12.9 | 17.3 | 19.9 | 20.4 | 24.8 | 34.1 |

A top-10 record does **80% of its peak in week one** and enters the chart already at mean position
6.2. Sales peak and chart peak both land at **week 2 (median)**. Decade-wide confirmation from
`lifecycles.csv`: **87.6% of all charting records debut at their peak position** (81.5% among top-40
peakers), and **a #1 record's median debut position is #1**. Nothing climbs.

Historically a Hot 100 single entered around #70-80 and took five to eight weeks to reach its peak; no
record debuted at #1 on the Hot 100 until 1995. The model's records are born at their peak and can
only fall.

**And the flat curve the arc has been trying to build is already there, hidden under the launch
spike.** Two independent readings:

- From week 5 to week 10 sales sit on a shelf at 27-31% of peak — barely decaying at all.
- The eight records whose sales peak falls at week 9, outside the launch window entirely, show a
  peak→next-week ratio of **0.966**. Their `rLaunch` is 1.000 by construction.

So the demand model does not produce a spike. It produces a flat, persistent curve with a 3.25x
launch multiplier bolted on the front. **The spike is the launch boost; remove it and a plateau is
what is left.**

### 12.3 Radio's channel into sales is inert, exactly as it was into rank

The working hypothesis going into this pass was that radio's effect on *sales* is the next lever.
Measured, it is real but nowhere near load-bearing:

| | |
|---|---:|
| `radioHeat` among charting records, p5 → p95 | 0.362 → 0.768 |
| raw sales multiplier `0.75 + heat * 0.5` | 0.931 → 1.134 = **1.218x** |
| after the geometric-mean discovery damping (`^1/3`) | **1.068x** |
| theoretical maximum, heat 0 → 1, damped | 1.186x |
| for scale: weekly sales spread among charting records, p5 → p95 | **6.29x** |

Radio can move a record's weekly sales by about **±3.4%** around the middle of its range and by at
most 18.6% between the two extremes, against a 6.29x sales spread and a 3.25x launch multiplier. This
is the same shape of defect as §11.2 — a term that exists, is wired, and cannot reach the outcome.

The damping is deliberate and should be understood before it is touched.
`CalculateSingleDemandStages` takes chart signal, momentum and radio as *correlated views of one
discovery event* and uses their geometric mean rather than compounding them, then pushes the result
through an odds transform. That was the right call for double-counting, but its consequence is that
**no single discovery signal can move demand much** — which is why radio cannot build a record toward
a peak even in principle. Note also the two-variable split: `radioHeat` (national) is what reaches
sales; `radioPlay` (regional) is what reaches rank. They are different variables and only the second
is load-bearing today.

### 12.4 What to do, in order

1. **Reshape the launch window into a ramp.** The week-1/2 multiplier represents shipping, pre-order
   and initial curiosity; that is a *floor* on early sales, not a 3.25x multiple of everything a
   record will ever do. Flattening it is the single change that converts a spike into a plateau, and
   it is the only change §12.1 justifies. Expect the peak to move from week 2 toward weeks 4-6 and
   debut-at-peak to fall from 87.6%.
2. **Then re-examine `AIRPLAY_CONVEXITY = 5`** (§11.6.3). It exists to manufacture a plateau; once the
   demand curve supplies one, the exponent should come down, and with it the Independent-entry
   collapse, the MidTier shortfall and the owner-Major level shift.
3. **Only then consider making radio load-bearing on demand.** It is genuinely inert (§12.3), but it
   is not what is producing the one-week #1s, and §11.7 warns that recomposing `radioHeat` moves the
   demand model rather than the chart. Sequence it last so it is calibrated against a curve that has
   already stopped spiking.

Do **not** touch `SATURATION_POWER` or `DEMAND_AGE_DECAY_RATE`. They are measured at 0.982 and 0.992
across the peak and are not carrying the fall.

### 12.4a SHIPPED (uncommitted): the release ramp. Results, and four falsified hypotheses.

`ChartSimulator` step 6 now multiplies conversion by `GetReleaseRampWeight` — a linear build from a
push-widened floor (0.28 at push 0.5) to 1.0 at week 6 — times a `RELEASE_RAMP_UNIT_RENORMALIZATION`
of 1.41. Reference probe `d7-ramp8-52-1001` (= `d7-ramp6`, 72/72 byte-identical).

| target | before | **after** | history |
|---|---:|---:|---:|
| Single units | 100 | **99.9-100.2** | hold |
| week-1 sales as share of peak | 83.9% | **22-28%** | 20-35% |
| sales peak week | 2 | **8** | 3-8 |
| debut == peak position | 87.6% | **26%** | ~0 |
| top-10 debuts / 52wk | 89 | **2-5** | ~0.1 |
| mean debut position | 49.4 | **73-76** | 86.8 |
| debuts above #60 | 60.1% | **21-27%** | 2.6% |
| mean chart life | 4.08 | **6.4-7.6** | 7.6 |
| **mean weeks at #1** | 1.73 | **3.3-4.3** | **2.57** |

The trajectory is the headline: a top-10 record now runs 23% → 39% → 48% → 58% → 72% → 87% → 93% →
94% of peak across weeks 1-8, climbing from mean chart position 36 to 4.5, then descending off the
chart by week 19-20. The 1.41 renormalisation was measured, not guessed, and landed within 0.2%.

**Four hypotheses were tested against the residual #1-tenure overshoot and all four failed.** Recorded
so they are not retried:

1. *Challengers are scarce.* Refuted. Records within 10% of the leader's points went 1 → 2, within
   25% went 3 → 5. The contender pool **grew**.
2. *The ramp is too long.* Refuted. Week 5 vs week 6 moved nothing; the peak stayed at week 8 either
   way, because the **top-ten feedback loop, not the ramp, sets the peak**.
3. *`AIRPLAY_CONVEXITY` is holding leaders up.* Refuted. 5 → 3 left tenure at 3.47.
4. *Per-record ramp dispersion by campaign.* Refuted. Tenure 3.71 → 3.25 while top-10 debuts went
   2 → 6.

**The actual mechanism is the volatility of the lead, not its size.** Median week-over-week change in
the #1/#2 points gap fell **0.2497 → 0.0408-0.0496** while the gap itself only moved 1.149 → 1.07.
Under the old spiky curves the lead was smaller than its own weekly noise, so ordering flipped almost
every week — 77% one-week #1s, far too *much* churn. Smooth plateaus cut that noise five- to sixfold
and ordering became persistent. The historical distribution is bimodal (27% at one week **and** 41% at
3+), which needs a real appeal separation at the top plus enough weekly noise to displace marginal
leaders. That noise belongs in the airplay pass, where station adds and drops were genuinely lumpy,
not in demand.

### 12.4b METHOD DEFECT: 52-week probes cannot discriminate #1 tenure

`d7-ramp1` and `d7-ramp6` are the **same configuration** on different RNG streams (ramp1 was run with
the D5/D6 probe suites, which consume draws). They score:

| | ramp1 | ramp6 |
|---|---:|---:|
| #1s holding one week | **36%** | **7%** |
| #1s holding 3+ weeks | **50%** | **71%** |
| top-10 debuts | 2 | 5 |
| mean weeks at #1 | 3.71 | 3.71 |

**Every variant difference I attributed to ramp length, convexity and dispersion sat inside that
band.** A 52-week window yields ~14 distinct #1 records; the tenure *distribution* is unresolvable
there. Two rules follow:

- Never compare a probe run carrying `--genre-market-v2-probes` / `--artist-population-lifecycle-probes`
  against one without. Different RNG stream, not comparable.
- What **is** stable at 52 weeks across six runs: units (99.9-100.2), mean debut (73.1-75.6), debuts
  above #60 (20.7-26.7%), peak week (8 in every run), week-1 share (22.2-27.9%), mean tenure
  (3.25-4.33). Those are signal. The one-week/3+ split and the top-10 debut count are not.

### 12.4c OPEN: the chart's dynamic range, and the ramp made it worse

Debut position is **not** set by the ramp. Making the ramp convex (progress²), which lowers weeks 2-4
without touching the week-1 floor or the week-6 ceiling, moved mean debut only 74.9 → 74.0. That null
result located the real cause.

Median weekly units by chart position, `d7-airplay5` → `d7-ramp8`:

| position | before | share of #1 | **after** | **share of #1** |
|---|---:|---:|---:|---:|
| #1 | 46,804 | 100% | **28,838** | 100% |
| 6-10 | 20,652 | 44.1% | 15,018 | **52.1%** |
| 21-40 | 10,458 | 22.3% | 8,779 | **30.4%** |
| 61-80 | 6,318 | 13.5% | 6,137 | **21.3%** |
| **91-100** | 5,491 | **11.7%** | 5,377 | **18.6%** |

**The ramp flattened the top**: the #1's median week fell 38% (peaks are lower and broader at constant
total units) while #91-100 barely moved, so #100 went from 11.7% to 18.6% of #1. On chart points the
ratio is 12.9% → 16.1%. A 1960s #100 sold on the order of 1-3% of a #1 — *estimated, not sourced*, and
a sales ratio rather than a points ratio, so it is not directly comparable to the points column.

Two things this measurement rules out and one it points at:

- **The live population is not the constraint.** 2,826-2,990 records are live each week and the chart
  is the top 3-4% of them. At week 27 the population runs #1 29,829 → #100 4,718 → #200 3,262 → last
  place 18 units. The deep tail exists.
- **The top ten is not the problem.** #1 → #10 is 1.9x, against a plausible historical 2-3x.
- **The #10 → #100 span is.** It is 2.8x where history looks more like 15-30x. Records ranked 10-100
  sell far too much relative to the top ten, and #100 (4,718) sits only 1.45x above #200 (3,262) — a
  dense near-tied band at the cutoff. That density is also why debuts jump: a record clearing the
  cutoff with a 30-50% weekly gain vaults past dozens of near-tied records, landing at ~#75 rather
  than ~#95, no matter how it got there.

The likely levers are the chart-position floors that lift the middle of the chart —
`effectiveAwareness` is floored at 0.4 for any top-40 record and 0.7 for any top-10 record regardless
of merit, and `GetChartVisibilityMultiplier` gives a flat 1.0 to everything from 41-100 against
0.40-0.95 for an uncharted record, which is a cliff at the cutoff.

**Note the tension before acting:** steepening the top widens the #1/#2 gap and would make the §12.4a
tenure persistence *worse*. History had both a steep curve and 2.57-week mean tenure, which means the
curve must steepen from #10 downward while the top ten stays crowded and volatile.

### 12.4d SHIPPED (uncommitted): the Hesbacher rank curve

The authored target for the chart's dynamic range is **Hesbacher's Billboard weighting**, adapted to
the 1960s Hot 100:

    y(x) = 4139 - 4357 * x / (x + 10)

It reproduces the authored tier table exactly (3,743 at #1; 1,960 at #10; 1,027 at #25; 508 at #50;
295 at #75; 178 at #100) — a J-curve of inequality, with **#100 at 4.8% of #1**, not the 1-3% guessed
in §12.4c. Pre-1973 Billboard polled ~110 outlets by hand (63 stations, 25 one-stops, 22 retailers),
with a theoretical sales maximum of 1,645 points and an airplay maximum of 2,040, so rank was always a
survey-weighted composite rather than a units count.

`GetChartExposureWeight` now carries it, normalised to average 1 across the hundred slots so it
reshapes the chart without moving total units, with `CHART_EXPOSURE_EXPONENT = 0.44` because rank
already earns exposure through four other channels. Reference probe `d7-hesb1-52-1001`.

| rank | Hesbacher target | model points | **model sales** |
|---|---:|---:|---:|
| 1 | 100.0% | 100.0% | 100.0% |
| 10 | 52.3% | 40.8% | 34.8% |
| 25 | 27.4% | 22.1% | 17.2% |
| 50 | 13.6% | 12.4% | 9.6% |
| 75 | 7.9% | 8.4% | 6.5% |
| **100** | **4.8%** | 6.3% | **4.8%** |

**The sales curve lands exactly on target at #100.** The points curve sits slightly above it (6.3% vs
4.8%) because airplay compresses; that is expected and arguably correct for a composite chart. The
model now dips *below* Hesbacher between #10 and #25, i.e. the top ten is more spread out than the
curve wants.

Side effects, against `d7-ramp8`:

| | ramp only | **+ Hesbacher** | target |
|---|---:|---:|---:|
| Single units | 100.0 | 98.8 | hold |
| #1 median weekly units | 28,838 | **89,962** | ~150,000 |
| mean debut position | 74.9 | **79.9** | 86.8 |
| debuts above #60 | 23.5% | **14.7%** | 2.6% |
| **top-10 debuts / 52wk** | 5 | **0** | ~0.1 |
| mean chart life | 6.50 | 8.89 | 7.6 |
| week-1 share of peak | 27.1% | 8.1% | *retired, see §12.4e* |
| mean weeks at #1 | 3.71 | **4.33** | 2.57 |

Position feedback amplified the term well past its first-order fit — #1 weekly units went to 89,962
against a predicted 60-80k — so `CHART_EXPOSURE_EXPONENT` should be re-derived from a run, not trusted
at 0.44.

**The tenure regression was predicted and is the §12.4c tension realised**: steepening the curve widens
the #1/#2 gap and makes ordering more persistent still. Tenure is now the single worst-fitting metric
in the model and it has resisted five separate levers. It needs the volatility mechanism of §12.4a,
not another shape change.

### 12.4e RESOLVED: the debut buckets win over the week-one shares

The authored calibration gives both a debut-position distribution (mean #86.8; 44.2% into 91-100;
essentially no top-ten debuts before "Hey Jude") and a week-one-share-of-peak table (#1 records at
20-35%). **On the Hesbacher curve those cannot both hold.**

A #1 peaking at 150,000 that sells 20-35% of that in week one is selling 30,000-52,500 — and Hesbacher
puts 41,000 at rank #25 and 78,500 at rank #10. So a #1 doing 20-35% in week one *debuts around #15-30*,
which contradicts both the stated "#1s debut frequently in the 40s-80s" and the 91-100 bucket carrying
44.2% of all debuts.

The model currently satisfies the debut table and violates the week-one table: week-one share is 8.1%,
which on Hesbacher corresponds to about rank #73, against a measured mean debut of 79.9. **It is
internally consistent — with the debut distribution.** The debut table is also the more precisely
specified of the two (ten buckets with percentages, versus figures the author flagged as rough
estimates), which is why it was favoured.

**AUTHOR DECISION (2026-07-31): follow the debut buckets. They are the higher-confidence source.**

So the model's behaviour here is correct as it stands, and **week-one share of peak is retired as a
calibration target**. Do not tune against the 20-35% figures; a week-one share near 8% is the value
consistent with a mean debut of ~87 on the Hesbacher curve, and chasing both at once is chasing a
contradiction.

The three reconciliations that would have saved the week-one table, recorded in case better data
turns up: the shares may be measured from **chart debut** rather than from release; they may refer to
**points rather than units** on a chart whose airplay half compresses the spread; or they may simply
be high.

### 12.4f DECADE RESULT for ramp + Hesbacher: `d7-hesb-decade-522-1001`

| | v5verify | **hesb** | band / target |
|---|---:|---:|---:|
| Single units, decade | 1.5078B | **1.5056B (99.9%)** | hold — PASS |
| **top-10 debuts** | 89 / 52wk | **4 / decade (0.1%)** | ~1 — PASS |
| debut == peak | 87.6% | **27.0%** | ~0 |
| #100 sales as % of #1 | 18.6% | **5.8%** | 4.8% |
| #1 median weekly units | 28,838 | **70,367** | ~150,000 (level, see §12.4d) |
| mean debut position | 49.4 | 76.6 | 86.8 |
| breadth | 367 | 392 | 400-600 |
| **MidTier firms 1969** | 23 | **16** | **25-40 FAIL** |
| **owner-Major 1968** | 48.2 | **53.0** | **45-52 now FAIL** |
| owner-Major 1969 | 52.7 | 52.9 | 45-52 FAIL |
| chart entries 1969 | 896 | 695 | |
| charting records | 8,048 | **5,150** | ~6,964 |
| mean chart life | 5.92 | **9.23** | 7.48 |
| Top-40 median life | 10 | **15** | 10-13, was in band |
| **mean weeks at #1** | 2.31 | **3.80** | 2.57 |
| distinct #1s | 226 | 137 | 203 |

**The identity that governs all of this: `charting records x mean chart life = 52,100 slot-weeks`,**
pinned by a hundred slots over 521 weeks. History is 6,964 x 7.48. Record count and chart life are the
same variable and cannot be fixed independently.

A prediction recorded here was wrong: tenure was expected to read *worse* than the 4.33 measured at
1960, and it read 3.80. Per-year tenure is now **flat** (4.33 → 3.47) rather than climbing with the
airplay era ramp, because the rank-exposure feedback now dominates that ramp.

### 12.4g Two more falsified hypotheses, and the mechanism that was actually missing

The first diagnosis of the tenure and chart-life overshoot was that **rank exposure sustains leaders at
the top**, with a proposed fix of gating exposure on sales the way `RADIO_POSITION_BONUS_SALES_FLOOR`
gates radio heat. Both halves were wrong:

- **The fix is circular.** Sales are what set rank, so "still selling like a record of that rank" is
  true of every record by construction and the gate never bites. The radio precedent only works
  because its floor is an *absolute* 15,000 units, which cannot survive a decade of changing level.
- **The premise is refuted by chart life per peak band.** The number-one band overshot *least*:

| peak band | v5verify | hesb | ratio |
|---|---:|---:|---:|
| #1 | 17.67 | 20.46 | **1.16x** |
| 2-10 | 14.29 | 19.00 | 1.33x |
| 11-40 | 8.41 | 14.22 | 1.69x |
| 41-70 | 4.21 | 8.55 | **2.03x** |
| 71-100 | 1.68 | 3.20 | **1.90x** |

  The excess sits at the **bottom** of the chart, and it is the release ramp rather than Hesbacher that
  put it there: a marginal record used to spike on the 3.25x launch boost, clip the chart for a week
  and die; it now creeps up over six weeks, loiters near #80-100 and creeps down. That is inherent to
  having a climb at all.

**What was actually missing is that the chart is a survey, not a census.** Before 1973 Billboard polled
about 110 outlets by hand — 63 stations, 25 one-stops, 22 retailers — grading each return "very good"
(20), "good" (15) or "fair" (5), for a theoretical maximum of 1,645 sales and 2,040 airplay points.
Every chart this model has produced ranked on an exact continuous read of the entire live population.

Sampling error is **not** demand noise: it reorders the chart without moving a unit, which is precisely
what three simultaneous misses required. Implemented as `ChartSimulator.DrawSurveySample` — a
mean-1 lognormal whose sigma is `1/sqrt(reporting outlets)`, with outlet count scaling from 6% to 100%
of the panel by weekly units, capped at `SURVEY_MAX_SIGMA`. The draw is taken **once per record per
week in `ChartManager` step 4 and cached on `RecordRuntimeData.surveySampleThisWeek`**, never inside
`CalculateChartPoints`, because that method is re-invoked by the audit telemetry and a redraw would let
the telemetry disagree with the ranking it reports.

52-week probes, `d7-hesb1` → `d7-survey1` (cap 0.45) → `d7-survey2` (cap 0.30):

| | hesb1 | survey1 | **survey2** | target |
|---|---:|---:|---:|---:|
| Single units | 98.8% | 98.8% | **98.8%** | unchanged — the point |
| median lead-gap volatility | 0.0787 | 0.1538 | 0.0967 | — |
| volatility ÷ gap | 48% | 79% | **88%** | 167% was too much churn |
| lead changed hands | 11 | 20 | **25** | — |
| mean chart life | 8.89 | 6.50 | **6.94** | 7.6 |
| debuts above #60 | 14.7% | 20.4% | **17.3%** | 2.6% |
| top-10 debuts | 0 | 0 | **0** | ~0 |

**Units did not move at all across the change**, confirming the term reorders without inflating demand.
The 0.45 cap let a record carried by a handful of outlets publish at twice its true score and vault
onto the chart high, so it is capped at 0.30.

**Do not read `no1mean`, one-week % or 3+ % off these probes** — §12.4b: ~14 #1 records per 52-week
run, and an identical configuration scored 36% vs 7% one-week. Decade run `d7-survey-decade-522-1001`
is the instrument for those.

Known remaining cost: survey noise lowers the quality→position correlation, already at ~0.35 and
already flagged in §11.8 as a band to restate rather than chase.

### 12.4h DECADE RESULT for the survey layer: `d7-survey-decade-522-1001`

| | v5verify | hesb | **survey** | target / band |
|---|---:|---:|---:|---:|
| Single units, decade | 1.5078B | 99.9% | **99.8%** | hold — PASS |
| **mean chart life** | 5.92 | 9.23 | **7.53** | **7.48 — essentially exact** |
| **charting records** | 8,048 | 5,150 | **6,337** | ~6,964 |
| chart entries 1969 | 896 | 695 | 851 | |
| Top-40 median life | 10 | 15 | 14 | 10-13 |
| **#1s holding one week** | 54% | 16% | **27%** | **27% — exact** |
| #1s holding two weeks | 22% | 18% | 20% | 32% |
| #1s holding 3+ weeks | 24% | 66% | 53% | 41% |
| mean weeks at #1 | 2.31 | 3.80 | 3.36 | 2.57 |
| distinct #1s | 226 | 137 | 155 | 203 |
| longest #1 run | 12 | 10 | 10 | 9 |
| **breadth** | 367 | 392 | **436** | **400-600 — PASS** |
| **MidTier firms 1969** | 23 | 16 | **10** | **25-40 FAIL, worst yet** |
| **owner-Major 1968** | 48.2 | 53.0 | **42.7** | **45-52 FAIL, now below** |
| **owner-Major 1969** | 52.7 | 52.9 | **49.7** | **45-52 PASS** |
| top-10 debuts | 89/52wk | 4/decade | 9/decade | ~1 — PASS |
| debut == peak | 87.6% | 27.0% | 36.4% | ~0 |
| mean debut position | 49.4 | 76.6 | 74.3 | 86.8 |

**The survey layer did exactly what it was built to do.** Mean chart life landed on 7.53 against a
historical 7.48, distinct charting records recovered 5,150 → 6,337, and one-week #1s hit 27% on the
nose — all while Single units moved 0.1%. The §12.4g slot-weeks identity is visible working in both
directions at once.

**Remaining chart misses.** The #1 distribution is now bimodal like history but mis-proportioned: too
few two-week #1s (20% vs 32%) and too many at 3+ (53% vs 41%), so mean tenure is still 3.36 against
2.57. Debut position moved the wrong way (76.6 → 74.3) and `debut == peak` rose 27.0% → 36.4%, both
because noise lets a record enter above where its demand justifies. The 41-70 debut buckets carry ~26%
against a historical 8.3%.

**Curve, and a note on comparing it.** #100 reads 6.3% of #1 on sales and 8.2% on points, against 4.8%
— flatter than the `hesb` run's 5.8%/7.7%, because survey noise blurs the ranking and regresses the
per-rank medians toward the mean. That is not necessarily a defect: **Hesbacher's formula was fitted to
published Billboard positions, which were themselves survey output**, so a noisy measured curve is the
like-for-like comparison and the underlying curve is steeper than it reads.

### 12.4i The two open regressions, and why they are one mechanism again

**MidTier collapsed to 10 firms while breadth rose to 436.** These are the same fact. The chart is now
spread very thin: 202 Independent firms share 498 entries at 1969, or 2.46 each, against a MidTier
promotion bar of 8 recent charting records. Survey noise democratises chart *access* — more labels get
a week — while making *sustained* charting rarer, so breadth enters band and the promotion feeder
starves. This is the third distinct route to the same MidTier failure (§7.1 bars, §11.6.2 entry
collapse, now access dilution) and it is further evidence that **the MidTier bars are not the
mechanism**.

| 1969 | v5verify | hesb | survey |
|---|---:|---:|---:|
| Independent entries | 485 | 359 | **498** |
| Independent firms | 181 | 155 | **202** |
| entries per Independent firm | 2.68 | 2.32 | **2.46** |
| MidTier entries | 231 | 200 | 106 |
| MidTier firms | 23 | 16 | **10** |
| Major entries | 340 | 225 | **232** |

**Soul's divergence blew out badly** — worse than any prior run:

| Soul divergence | 1966 | 1967 | 1968 | 1969 |
|---|---:|---:|---:|---:|
| v5verify | +5.6 | +11.4 | +11.8 | +9.5 |
| **survey** | **+11.5** | **+16.8** | **+18.3** | **+15.3** |

The likely cause is that `GetChartExposureWeight` is a **genre amplifier as well as a rank amplifier**,
because position correlates with genre acceptance: high acceptance → high radio → high rank → more
exposure → more sales → higher rank. That is the §11.7 lesson recurring — "concentrating airplay on
high-acceptance genres collapses MidTier" — arriving this time through chart exposure rather than
airplay convexity. Note RnB moved the opposite way (−3.3 at 1969), which is the same signature the
rejected cubed-product airplay variant produced.

**Both regressions point at the same knob**, `CHART_EXPOSURE_EXPONENT`. Lowering it should relieve Soul
and the MidTier dilution, at the cost of flattening a curve that already reads flatter than Hesbacher.
That trade has not been measured and is the next thing to test.

### 12.4j MidTier: the promotion bar is above the population maximum

**The Independent→MidTier route is not hard, it is arithmetically closed.** Both routes in
`IsIndependentReadyForMidTier` require `chartingLastYear >= 8` — the organic route
(`ownedReach >= 0.60`, low dependency, **charting >= 8**) and the dependent-footprint route
(**charting >= 8**, roster >= 8). The base `MidTierPromotionMinimumRecentChartingRecords = 4` is dead
code, unreachable behind either route.

Reconstructing `GetRecentChartingRecordCount` (distinct records released within 52 weeks that charted
at least once) from `d7-survey-decade-522-1001-records.csv`:

| year | Independent labels charting | median | p90 | **max** | clearing 5 | clearing 6 | clearing 7 | **clearing 8** |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 1963 | 108 | 1 | 4 | **7** | 10 | 4 | 2 | **0** |
| 1966 | 174 | 1 | 4 | **11** | 8 | 5 | 1 | **1** |
| 1969 | 200 | 2 | 4 | **7** | 7 | 2 | 1 | **0** |

**In 1963 and 1969 the best Independent label in the entire year reaches 7 against a bar of 8.** Zero
labels can promote. MidTier's flat 10-12 firms across the decade is the launch population decaying with
essentially no replenishment.

The demotion side is healthy and is not the problem: MidTier incumbents run a median of 7-9 recent
charting records against a demotion bar of 4, with 0-2 firms below it in any sampled year.

**This is the fourth distinct route to the same MidTier failure and the first one that is simply a
number set out of reach.** The bar of 8 was added when independent distribution made `ownedReach` a
free pass and MidTier flooded 28 → 103 firms; it was correct for that chart. It was never re-derived
after the chart's record population changed.

**Recommendation:** lower `MidTierPromotionOrganicChartingRecords` and
`MidTierPromotionDependentChartingRecords` from 8 to **5**, which admits 7-10 candidates a year before
the reach and roster gates cut it further, and lower `MidTierDemotionChartingRecords` from 4 to **3**
to keep the hysteresis the comment at its declaration asks for. Do not touch the reach or roster gates
in the same change — they were the original flood mechanism and must stay in place to prove the
charting bar alone is what reopened the route.

### 12.4k Debut position: the miss is in the 11-70 bands, not the bottom

Debut position by peak band, `d7-survey-decade-522-1001`:

| peak band | n | median debut | mean debut | debut == peak | median life |
|---|---:|---:|---:|---:|---:|
| #1 | 142 | **30** | 38.2 | 0.7% | 20 |
| 2-10 | 425 | 48 | 51.4 | 0.7% | 19 |
| 11-40 | 1,610 | 68 | 65.2 | 8.3% | 13 |
| **41-70** | **1,990** | **73** | **73.8** | **29.5%** | 6 |
| 71-100 | 2,170 | **90** | 88.5 | 72.9% | 1 |

**The bottom band is already correct** (median debut 90). The overall mean of 74.3 is dragged up by
the 41-70 band — 1,990 records entering at #73 and peaking around #60, nearly a third of them at their
peak on debut — and by the 11-40 band at #68. For the overall mean to reach 86.8 those bands have to
enter near #85-88 and climb 20-40 places.

Note the #1 band debuts at a median of #30 against the authored "frequently 40s-80s", so it is too
high as well, just less numerous.

**Recommendation: retest the convex release ramp.** `RELEASE_RAMP_CURVE` was tried and rejected in
§12.4c because it moved mean debut only 74.9 → 74.0 — but that was measured on a **flat** chart, before
Hesbacher. Entry happens at a median of four weeks since release, where a linear ramp already sits at
0.71 of full availability; lowering weeks 2-5 now costs a much larger number of positions because the
curve beneath them is steep. The null result should not be trusted across that change.

### 12.4l Mean tenure: it is position stickiness, not plateau width

The sales plateau is only about three weeks wide (weeks 8-10 at 90.8 / 89.0 / 84.8% of peak), so plateau
width is **not** the explanation for 53% of #1s holding 3+ weeks against 41%. But chart position sits at
roughly #5 across weeks 8-11 while sales fall 90.8% → 75.1%. **Position is stickier than sales.**

Two candidate causes, not yet isolated:

1. **`BASE_INERTIA = 0.80`** in `GetInertiaPositionCap` caps how far a record may fall in one week, so a
   declining record occupies a high slot it no longer earns and delays challengers reaching the top. It
   is already gated (needs `unitsThisWeek > 0`, `weeksNegative < 3`, `momentum > -0.20`), so how much it
   actually bites is unmeasured.
2. **Every record is on the same trajectory**, so relative order is preserved even as absolute sales
   fall — the §12.4a lockstep problem, which survey noise reduced but did not remove.

The distributional shape says the same thing: one-week #1s are already exactly on target at 27%, so
this cannot be fixed with more survey noise, which moves the one-week and 3+ buckets together. It needs
a lever that acts specifically on the fat 3+ tail. Measure the inertia cap's bite first.

### 12.4m Analysis tooling moved to Python

`Import-Csv` is unusable above ~50MB and hung outright on the 384MB decade `records.csv`, which had
forced a hand-written streaming CSV parser with a quote state machine. Python 3.14 + pandas is now
installed (`%LOCALAPPDATA%\Programs\Python\Python314\python.exe`; `Scripts` is not on PATH, invoke by
full path).

**The tooling now lives in `SimTools/` and is versioned.** It previously existed only in a session
scratchpad and had to be recovered from a temp directory to be used again:

| script | what it reads |
|---|---|
| `chart.py` | `score`, `debut`, `debut2`, `traj`, `inertia`, `bite`, `tail`, `spells`, `lockstep`, and `drops` (station-drop timing and the re-add rate, §12.4t) |
| `labels.py` | the label acceptance table from `concentration.csv`, whole-decade and year by year |
| `genre.py` | per-genre divergence and longevity index from `genre-decade-shape.csv` — **never** from `genre-market-weekly.csv`, see §3.1 |
| `hazard.py` | offline replay of the station-drop survival curve against a run's real support trajectories, for re-deriving the drop constants without spending a run (§12.4t) |

**One measurement trap it exposed, worth not repeating:** `lifecycles.csv` only contains records that
have **closed**, which on a 52-week run is a short-lived, low-peaking minority that debuts near the
cutoff. It reported mean debut 85.4 where the decade run reported 74.3 for the *same configuration*.
Debut must be measured from `records.csv` (each record's first charting week, dropping those already
charting in run week 1) whenever runs of different lengths are compared — that is what `debut2` does.

### 12.4n MidTier bar lowered 8 -> 5; two probe fixtures had rotted

`MidTierPromotionOrganicChartingRecords` and `MidTierPromotionDependentChartingRecords` 8 -> 5,
`MidTierDemotionChartingRecords` 4 -> 3, per the §12.4j measurement. The reach, roster, runway,
operating-month and sustained-capability gates are deliberately untouched.

**Two D6 fixtures failed and both were rotted, not wrong:** probes 68d2 and 68j hard-coded
`chartingLastYear = 7`, chosen as "one under the old bar of 8". At a bar of 5 those silently inverted
into assertions that a *qualifying* label must not promote. Both are now expressed against
`MidTierOrganicPromotionChartingBar - 1` and `MidTierDependentPromotionChartingBar - 1` so they track
future re-derivations, and new probe 68j2 asserts the dependent bar stays above the base floor.
`MidTierDependentPromotionChartingBar` and `MidTierBaseChartingFloor` were exposed for this.

**Lesson: a probe fixture that hard-codes a value relative to a constant is a latent failure.** The
ladder probes 95g/95i/95j were already relational and survived the change untouched.

### 12.4o Inertia does NOT hold the top of the chart. Measured.

Observed falls are post-cap and cannot show the cap's bite, so the raw ranking was re-derived each week
from published `chartPoints` and compared with the position actually assigned (`chart.py bite`):

| falling from | points implied a drop of | chart delivered |
|---|---:|---:|
| #1 | 1 | **1** |
| 2-10 | 2 | **2** |
| 11-40 | 9 | **6** |

Mean lift across all 52,200 charted record-weeks is **+0.00 positions**. `BASE_INERTIA = 0.80` does not
bind at the top at all; it only slows mid-chart descent, where it inflates chart life. **It is a
chart-life lever, not a tenure lever** — the hypothesis that it was holding #1s is refuted.

Lockstep, measured on the same run (`chart.py lockstep`):

| | |
|---|---:|
| week-to-week Spearman of top-40 order | **0.8823** (1.0 = frozen) |
| age spread inside the top 10 | sd 3.17 weeks around a mean age of 10.5 |
| mean weekly position change, top 40 | 6.14 places |
| common-mode share of that movement | 0.205 |

### 12.4p THE NEXT FIX: airplay is mis-phased against sales at both ends

Airplay's share of a top-10 record's chart points is **U-shaped across its life** (`chart.py tail`):

| week | sales as % of own peak | **airplay as % of points** |
|---:|---:|---:|
| 1 | 8.7% | **77.3%** |
| 4 | 33.5% | 47.8% |
| 8-9 (peak) | 87.7% | **37.1%** |
| 12 | 70.9% | 45.2% |
| 17 | 25.5% | 52.4% |
| 20 | 8.2% | **54.3%** |

**This is a regression introduced by the release ramp and it drives both remaining misses.** The ramp
put a six-week build on *sales* and left *airplay* on its old onset — `ChartManager.cs:741` seeds
`radioPlay` from `campaignImpact * regionStrength` with **no age term at all**. So a week-one record
sells at 8.7% of its eventual peak while already carrying full campaign rotation, and 77% of its
published points are airplay. That is a large part of why debuts land at #73 rather than #90, and it
partially supersedes §12.4c/§12.4k, which attributed the debut miss entirely to curve flatness.

At the other end, week 17 sales are 25% of peak while airplay is 52% of points, so published points are
roughly double what sales justify — which is what holds records near the top after they have
commercially died, i.e. the fat 3+ week #1 tail (53% against 41%).

**Four separate mechanisms prop the tail up, and `RADIO_FATIGUE_DECAY = 0.88` fights all four on a
fixed week-8 clock that no longer matches anything** — the sales peak is now week 9, so fatigue begins
*before* the record peaks:

- the fall lerp rate in `UpdateRadioHeat` is **0.10** (0.22 after week 12) while sales shed ~19% a week;
- `UpdateLabelPush` pins `weekFactor` at **0.85** for any top-20 record where normal decay would be 0.1;
- the top-10 position bonus of **+0.25**;
- `RegionalRadioHold = 0.92` on the regional lerp.

Tail-length variation is real but thin: weeks from sales peak to 40% of peak run median 7.0, sd 1.62,
**CV 0.240**, p10 4 / p90 9; peak week median 9, sd 1.57.

**Plan, in order.** (1) Give airplay a build matching the release ramp and let it decay with the
record's commercial trajectory rather than on a fixed clock. (2) Only then add a discrete station-drop
mechanic for tail variance — and it must **supersede** `RADIO_FATIGUE_DECAY`, not stack on it. Variance
around a biased mean only spreads the bias, so the phase fix has to land first.

**Consequence to plan for:** airplay is ~45% of chart points, so re-phasing moves every record's points.
`CHART_EXPOSURE_EXPONENT`, the Hesbacher curve fit and the survey sigma were all calibrated against the
current airplay shape and must be re-read afterwards. Budget two decade runs: one to land the phase fix,
one to re-settle the curve.

### 12.4q THE #1 TENURE MISS IS RETURNS, NOT RUN LENGTH

Decomposing per-record tenure into **spell length** and **number of spells** settles what five levers
failed to move:

| | v5verify | survey | midtier | history |
|---|---:|---:|---:|---:|
| distinct #1 records | 226 | 155 | 146 | **203** |
| #1 spells | 278 | 207 | 183 | ~213 |
| mean **spell** length | 1.87 | 2.52 | **2.85** | **~2.45** |
| mean **per-record** tenure | 2.31 | 3.36 | 3.57 | 2.57 |
| **records with 2+ separate #1 spells** | **19.0%** | **28.4%** | **24.0%** | **4-5%** |
| spells per record | 1.230 | 1.335 | 1.253 | ~1.05 |
| spell-count distribution | — | 111x1, 37x2, 6x3, **1x4** | 111x1, 33x2, 2x3 | — |

**The returns defect predates this whole arc** — `d7-v5verify` already ran 19.0%. The reshape fixed
spell length (1.87 -> 2.85, against ~2.45) and worsened returns (19.0% -> 24-28%). So this is a
long-standing hole that the earlier configuration masked by having runs that were far too short.

The arithmetic closes exactly and gives the target: history is 203 records x 1.05 spells x 2.45 weeks
= 521; the model is 146 x 1.253 x 2.85 = 521. **Eliminating returns alone would take distinct #1s from
146 to about 174; reaching 203 also needs spell length to come down from 2.85 to ~2.45.** Both, and in
that order.

**An individual #1 run is already the right length.** The entire per-record overshoot is records
*reclaiming* the top spot: a quarter of chart-toppers do, against a historical 4-5%, and one record
took #1 in four separate spells. Historically The Twist was the only record in the decade with two
genuinely separate runs, with a handful of others (Come See About Me, I Can't Help Myself, Day Tripper,
The Sound of Silence) briefly displaced and returning.

**"Shorten #1 runs" was therefore the wrong target and chasing it would have made the model worse.**
This is the third time a measurement-basis error has misdirected this metric — §11.6.1 compared runs
against records, §12.4b chased noise at 52 weeks, and now per-record tenure was read as run length.
**Always decompose #1 tenure into spells and returns before acting on it.**

Why records ping-pong: survey sigma at the top is 0.095, so the difference of two weekly draws carries
sigma 0.134, against a median #1/#2 **log**-gap of 0.144. The measurement noise is the same size as the
gap, so the top two trade places repeatedly. Historically a displacement was a *monotone crossing* — a
rising record passing a falling one — and returns were rare because the trajectories genuinely diverged.

**More survey noise is NOT the lever, and the arithmetic rules it out.** Reproducing history's flip rate
of 0.389 needs a per-record sigma near 0.362 against the 0.095 applied. The survey physics do not
support that: 110 outlets graded 20/15/5 give roughly 1.3% quantisation error once averaged, and the
panel was **fixed** week to week, which correlates the error and reduces effective noise further. Note
also that only ~13% of the observed week-to-week variance at the top is survey noise — the rest is
genuine demand movement, so the chart is not noise-dominated.

Two further facts worth keeping:

- Observed P(lead changes) is 0.350 against the 0.389 history needs — a much smaller gap than the
  per-record tenure figures suggested.
- Tenure does track record size (Spearman 0.379; median peak weekly units 62,982 at one week against
  85,711 at five-plus), which is correct — real smashes did hold for weeks. The size gradient is not
  the defect.

**The §12.4p burnout term is a direct attack on this and is already in the run.** Keying airplay to
decline from a record's own running peak means a record past its peak sheds rotation and cannot bounce
back. Read the return rate on `d7-phase-decade-522-1001` before adding anything further.

### 12.4r NEXT SESSION STARTS HERE: the discrete station drop

Committed at `9757a3b`. **The airplay build shipped; the decline-keyed burnout was built, measured and
rejected.** This section is the brief for the work that replaces it.

#### What shipped, and what it bought

| | midtier | **phase (build only)** | target |
|---|---:|---:|---:|
| mean debut | 74.1 | **78.8** | 86.8 |
| debuts above #60 | 23.6% | **14.9%** | 2.6% |
| **top-10 debuts / decade** | 15 | **1** | ~1 |
| week-1 airplay share of points | 77.5% | **60.8%** | — |
| Single units | 99.8 | **99.8** | hold |

`GetRadioBuildWeight` ramps rotation over six weeks and is applied to the **regional** pass
(`ChartManager` launch seed and `targetRegionalRadio`), never to `radioHeat`, because `radioHeat`
multiplies conversion directly and moving it moves the demand model (§11.7).

**Caveat on the committed state:** the only decade run of build-only is at 52 weeks
(`d7-buildonly-52-1001`: units 99.0, debut 80.9, above-#60 9.5%, life 7.11, top-10 debuts 0). The
decade figures above come from `d7-phase-decade-522-1001`, which carried build **and** the rejected
burnout. **A build-only decade run is the first thing the next session should launch** — though note
the drop mechanic will change the tail again, so it may be worth building the drop first and running
once.

#### Why the smooth burnout failed, so it is not rebuilt

`Lerp(0.15, 1, unitsThisWeek / peakWeeklyUnits)` was neutral through the climb and bit only past the
peak, which is the right *shape*. It failed on **magnitude**: at week twenty it returns 0.366 where the
`0.88^(weeks-8)` clock it replaced gives 0.216. It was gentler than what it replaced.

| week | midtier airplay % of points | phase (with burnout) |
|---:|---:|---:|
| 15 | 50.1 | 51.5 |
| 17 | 51.6 | 56.3 |
| 20 | **52.9** | **64.0** |

Sales at week twenty went 8.4% → 25.4% of peak, peak-to-40% 7.0 → 8.0 weeks, chart life 7.49 → 8.31,
charting records 6,350 → 5,719, and returns 24.0% → 27.2%. It is also **self-reinforcing**: more
airplay → higher rank → more exposure → more sales → higher support ratio → less burnout.

**A linear lerp from a floor cannot do this job.** Being neutral at the climb *and* reaching 0.216 at
0.254 support requires a negative floor. Do not retry it with a different floor or a power term as a
first move — the successor is a discrete event, which is both more severe and the only version that
supplies tail variance (CV stayed flat at 0.242 against 0.254 without it).

#### The station drop: what to build

Stations dropped records from rotation as a **decision**, not as an exponential. That is the mechanic:
per record per region, once it has faded far enough, rotation ends — abruptly, and at different times
for different records.

`RecordRuntimeData.peakWeeklyUnits` is already maintained (running max, set in `FinalizeWeeklySales`)
and currently has **no consumer** — it is kept precisely for this. `unitsThisWeek / peakWeeklyUnits` is
the fade signal and is neutral during the climb by construction.

**FLAG — hysteresis or a floor on re-add.** A drop keyed to a noisy weekly ratio will drop and re-add
the same record as its sales wobble, and re-adds are exactly the wrong direction: **returns are already
the single largest #1 defect at 24-28% against a historical 4-5% (§12.4q)**, and a flapping drop would
make them worse. Build in one of:

- a one-way latch — once dropped, a record does not return to rotation at all, which is closest to how
  a 1960s playlist actually worked;
- or a hysteresis band — drop below one support level, re-add only above a distinctly higher one;
- or a minimum weeks-since-drop before any re-add is considered.

The latch is the strongest default. Whatever is chosen, **measure the re-add rate explicitly** and
report it alongside the return rate.

It must also **supersede `RADIO_FATIGUE_DECAY`, not stack with it** — otherwise rotation decays twice.

#### How to know it worked

> **SUPERSEDED — the targets below were read off the wrong run.** Every figure in this list comes from
> `d7-phase-decade-522-1001`, which carried the rejected burnout, or from `midtier` before the airplay
> build. The build-only decade control now exists (`d7-buildonly-decade-522-1001`) and is materially
> closer to history on every one of them. Use §12.4s for the real baseline; the list is kept only
> because the *order* of reads is still right.

Read in this order, all on a decade run:

1. **Airplay tail share** (`chart.py tail`): week-20 share should fall well below the 52.9% baseline,
   not rise. This is the direct test.
2. **Chart life and charting records** (`chart.py score`): 8.31 → toward 7.48, and 5,719 → toward
   6,964. These move together — §12.4g, `records x life = 52,100` always.
3. **Returns** (`chart.py spells`): 24-28% → toward 4-5%, and the re-add rate.
4. **Tail variance** (`chart.py tail`): the CV of peak-to-40% should rise above 0.25; a discrete drop
   at varying times is what supplies it.
5. **The label acceptance table**: breadth 454, MidTier 51, owner-Major 45.6 / 49.5.

#### Still open, in priority order

> **Item 1 is superseded — same defect as the list above.** "MidTier 1969 at 51" is a
> `d7-midtier-decade-522-1001` figure that was never re-derived after the phase build. On the actual
> committed state it reads 40, and 1967-68 are the years out of band, not 1969. See §12.4s. The
> standing label failure is now owner-Major, from below.

- **MidTier 1969 at 51** against a 25-40 band — the only year out of band. Do not lower the bar to 6;
  that scales the whole line down ~20% and pushes most years *below*.
- **Debut mean 78.8 against 86.8.** §12.4k showed this is downstream of how steep the bottom of the
  published curve is (#75 at 10.4% of #1 against Hesbacher's 1.66 ratio), and the lever is
  `CHART_EXPOSURE_EXPONENT`, entangled with Soul. Sequence the Soul authoring fix first.
- **Soul divergence +18.3 at 1968** — to be fixed from the genre-authoring side by author decision, not
  by lowering chart exposure.
- **The convex release ramp is closed.** Tried twice, null both times (74.9→74.0 flat chart, 77.2→76.7
  steep chart). Do not try a third time.
- **`BASE_INERTIA` is a chart-life lever, not a tenure lever** (§12.4o): it does not bind at the top at
  all, and only slows mid-chart descent 9 → 6.

### 12.4s FIRST: the build-only decade control, and it moves every acceptance target

`d7-buildonly-decade-522-1001`. §12.4r flagged that the committed state had no decade run behind it and
that the decade figures quoted throughout came from `d7-phase-decade-522-1001`, which carried the
rejected burnout. It was launched before anything else was built. **The committed state is better than
the run it was being judged against on every reliable metric, and the §12.4r acceptance targets were
therefore all set against the wrong reference.**

| | phase (build + rejected burnout) | **buildonly (committed)** | history |
|---|---:|---:|---:|
| Single units | 99.8 | **99.7** | hold — PASS |
| mean chart life | 8.31 | **7.72** | 7.48 |
| charting records | 5,719 | **6,168** | 6,964 |
| mean debut | 78.8 | **77.5** | 86.8 |
| debuts above #60 | 14.9% | 17.1% | 2.6% |
| top-10 debuts / decade | 1 | **1** | ~1 — PASS |
| distinct #1s | 147 | **164** | 203 |
| **#1 spells** | 197 | **202** | **~213** |
| **mean spell length** | 2.64 | **2.58** | **~2.45** |
| mean per-record tenure | 3.54 | **3.18** | 2.57 |
| **records with 2+ spells** | 27.2% | **20.7%** | **4-5%** |
| week-20 airplay share of points | — | **45.3%** | — |
| CV of peak-to-40% | — | **0.254** | — |

Two things follow, and both change what the drop is for:

- **Spell count and spell length are already on target.** 202 spells against ~213, and 2.58 weeks
  against ~2.45. `164 x 1.232 x 2.58 = 521` closes exactly. **Returns are now the only remaining #1
  defect**: eliminating them alone takes distinct #1s from 164 to 202, which is the 203 target. This
  is the sharpest the metric has ever been — §12.4q said "both, and in that order", and the first of
  the two is done.
- **Two of the five §12.4r acceptance reads were already met before the drop existed.** The week-20
  airplay share is 45.3%, not the 52.9% quoted (that figure was `midtier`, i.e. pre-build), and the CV
  of peak-to-40% is **already 0.254**, above the ">0.25" the drop was supposed to supply. The build
  delivered the tail variance; the drop's job on that metric is to not lose it.

Chart life at 7.72 against 7.48 leaves the drop a **3% budget**, not the 11% §12.4r implied. Read the
life and records rows before anything else.

#### The label acceptance table was also quoting the wrong run, and the open failure has swapped ends

§12.4r's item 5 — "breadth 454, MidTier 51, owner-Major 45.6 / 49.5" — and its "**Still open:** MidTier
1969 at 51 against a 25-40 band" are **`d7-midtier-decade-522-1001`** figures, i.e. the run *before*
the airplay phase build. They were never re-derived after it.

| 1969 unless noted | midtier | phase | **buildonly (committed)** | band |
|---|---:|---:|---:|---|
| breadth | 454 | 455 | **449** | 400-600 — PASS |
| **MidTier firms** | **51 FAIL** | 34 | **40** | **25-40 — PASS** |
| **owner-Major 1968** | 45.6 | 37.7 | **42.8** | **45-52 — FAIL, below** |
| **owner-Major 1969** | 49.5 | 39.8 | **44.4** | **45-52 — FAIL, below** |
| Major firms | — | 8 | 10 | — |

**The phase build fixed MidTier and cost owner-Major.** MidTier 51 → 40 is the §12.4r "only year out of
band" closing on its own, and it needs no further work — the recommendation there not to lower the
promotion bar to 6 stands, and is now moot. What replaced it is owner-Major failing from *below* for
the first time in the arc, on both years, having been in band at the majorgate and v5verify runs. Per
the §7.2 note this is checked against the Major firm count: 10 firms in 1969, up from 8, so it is not
a headcount artifact.

MidTier at 40 sits on the top edge of the band, so anything that adds MidTier headcount is now a
regression risk rather than a repair. Note the shape too: MidTier peaks at 47 in 1968 and falls to 40
by 1969, so the band is only just held at the end of a rising line.

### 12.4t SHIPPED (uncommitted): the discrete station drop. Returns 20.7% → 12.8%.

Reference `d7-drop-decade-522-1001`, against the `d7-buildonly-decade-522-1001` control of §12.4s.
52-week probe `d7-drop1-52-1001`; probe-suite run `d7-drop-probes-52-1001`, D5 green and D6 **1-98**
green.

#### Decade result

| | buildonly (control) | **drop** | history / band |
|---|---:|---:|---:|
| Single units | 99.7 | **99.8** | hold — PASS |
| **records with 2+ #1 spells** | 20.7% | **12.8%** | **4-5%** |
| distinct #1s | 164 | **179** | 203 |
| #1 spells | 202 | 204 | ~213 |
| mean spell length | 2.58 | **2.55** | ~2.45 |
| mean per-record #1 tenure | 3.18 | **2.91** | 2.57 |
| **#1s holding one week** | 25% | **27%** | **27% — exact** |
| #1s holding 3+ weeks | 53% | **49%** | 41% |
| **mean chart life** | 7.72 | **7.39** | 7.48 |
| charting records | 6,168 | **6,453** | 6,964 |
| mean debut | 77.5 | **78.2** | 86.8 |
| debuts above #60 | 17.1% | **16.1%** | 2.6% |
| top-10 debuts / decade | 1 | 1 | ~1 — PASS |
| **week-20 airplay share of points** | 45.3% | **12.1%** | — |
| **re-add rate** | — | **0 of 1,577,566 record-weeks** | 0 |
| CV of peak-to-40% | 0.254 | **0.217** | >0.25 — **REGRESSION** |
| breadth | 449 | 492 | 400-600 — PASS |
| **MidTier firms 1969** | 40 | **51** | **25-40 — FAIL** |
| **owner-Major 1968** | 42.8 | **46.2** | **45-52 — PASS** |
| **owner-Major 1969** | 44.4 | **40.2** | **45-52 — FAIL** |

**Returns are the headline and they are the point.** 20.7% → 12.8% is the first movement any lever in
this arc has produced on that metric, and §12.4s established it was the *only* remaining #1 defect.
It moved without disturbing what was already right: spell length held at 2.55 against ~2.45, spell
count 204 against ~213, and one-week #1s landed exactly on 27%. Distinct #1s 164 → 179 against 203.

**The airplay phase test passes decisively.** The U-shape's right arm is not merely flattened, it is
inverted — airplay's share of a top-ten record's points now peaks at 43.5% in week fourteen and
*falls* to 12.1% by week twenty, against 13.3% of peak sales. It rose to 45.3% in the control.

| top-10 record | wk 12 | wk 14 | wk 17 | wk 19 | wk 20 |
|---|---:|---:|---:|---:|---:|
| sales, % of own peak | 80.0 | 66.7 | 37.6 | 19.7 | 13.3 |
| airplay % of points, control | 38.8 | 42.8 | 43.6 | 43.2 | **45.3** |
| **airplay % of points, drop** | 41.6 | 43.5 | **32.8** | **19.1** | **12.1** |
| **panel still carrying (drop)** | — | — | — | — | ~6% |

Drop timing carries real spread: a top-ten record loses its first market at a median age of 14
(p10 12, p90 17) and is off the air entirely at a median of 20 (p10 17, p90 25, sd 3.10). The 41-100
band, which peaks earlier and lower, is cut earlier on the same curve with no band-specific term —
first market at 12, off entirely at 19. 96.9% of all records lose at least one market, 90.9% lose all
of them.

#### Three costs, stated plainly

**1. MidTier 1969 goes back out of band, 40 → 51 — but the drop is not the mechanism.** Read the whole
line rather than the reported year:

| MidTier firms | 1965 | 1966 | 1967 | 1968 | 1969 |
|---|---:|---:|---:|---:|---:|
| buildonly control | 32 | 36 | **44** | **47** | 40 |
| drop | 36 | 41 | **46** | **46** | **51** |

**The control already breaches the 25-40 band at 1967 and 1968** and only dips back under at exactly
1969; the two runs are within two firms of each other at 1967 and identical at 1968. Reporting the
1969 value alone hid a line that has been running hot since 1967 in both. This is the *fifth* distinct
route to a MidTier miss and the fourth time the answer has been "the bars are not the mechanism" —
shorter chart lives mean more records chart, more Independents clear the bar of 5, and the tier fills.
Do not tune the drop against it.

**2. owner-Major 1969 falls 44.4 → 40.2**, while 1968 rises into band (42.8 → **46.2**). Major firms
9 against the control's 10, so per the §7.2 discipline part of the 1969 fall is a headcount effect and
it is not a clean read. The year-to-year path is 40.2 → 46.2 → 40.2, a swing large enough that single
years should not be banked. **This is now the standing label failure**, having replaced MidTier at
§12.4s, and both ends of it are entangled with `CHART_EXPOSURE_EXPONENT`, which §11.6.3 and §12.4r
both queue as the next lever.

**3. Soul gets worse by about 1.5 points**, which is the §11.7 law recurring: every airplay shape
concentrates on high-acceptance genres.

| divergence | 1967 | 1968 | 1969 |
|---|---:|---:|---:|
| Soul, control | +15.2 | +16.2 | +16.6 |
| **Soul, drop** | **+16.6** | **+18.0** | **+17.7** |
| PsychedelicRock, control | +3.3 | +5.5 | +2.0 |
| **PsychedelicRock, drop** | **+1.4** | **+3.3** | **+0.4** |

Soul is already the worst genre miss and is queued for an authoring fix by author decision, so this
adds to a bill already being paid. **PsychedelicRock, the other genre §8.3 named, largely closes** —
its longevity index falls 1.31 → 1.12 at 1968 and 1.10 → 0.82 at 1969, which is what an album genre
whose singles do not linger should look like. Gospel is unmoved (+5.2 → +4.9).

#### The tail-variance criterion is structurally unreachable by this mechanic

§12.4r item 4 wanted the CV of peak-to-40%-of-peak to rise above 0.25. It **fell**, 0.254 → 0.217, and
the same thing happened at 52 weeks (0.228 → 0.201), so it is not noise.

**The hazard keys on the record's own support ratio, and peak-to-40% is also a support-ratio measure.**
A slow-fading record is dropped later by construction, so the drop shortens each record's tail roughly
in proportion to how long that tail already was — it is self-normalising, and self-normalising
mechanisms compress a distribution rather than spread it. Variance in the *airplay* tail is real and
large (sd 3.10 weeks on the age at full drop); it simply does not land on this metric.

Two further facts: the control already sat at 0.254, i.e. **the criterion was met by the phase build
before the drop existed** (§12.4s), and the peak-week spread *rose*, sd 1.65 → 2.06. If tail-length
dispersion is wanted later it needs a term keyed to something **independent of the record's own
trajectory** — a per-record leash drawn at release, say — not a differently-shaped support curve.

#### Two measurement bases for chart life, and they disagree in sign

Recorded because this document has now been misdirected by a basis error four times:

| | control | drop | history |
|---|---:|---:|---:|
| `lifecycles.csv`, per closed record | 7.72 | **7.39** | 7.48 |
| Σ`chartRecordWeeks` ÷ Σ`uniqueChartingRecords`, per year | 6.77 | **6.53** | 7.48 |

The per-year sum is what makes `records x life = 52,100` exact, but it counts a record that charts
either side of New Year **twice**, so its record count (7,698 → 7,978) is not comparable to a
historical 6,964 counted per record. Every decade figure in §12.4f, §12.4h and §12.4r uses the
`lifecycles` basis; **stay on it**, and read the identity as a constraint on direction rather than as a
target either column can be scored against.

On that basis chart life lands at 7.39 against 7.48 — a 1.2% undershoot, from 7.72. If it is wanted
exactly on target, `STATION_DROP_MAX_WEEKLY_CHANCE` is the knob and roughly 0.32 rather than 0.40
would give back the 0.09. That was **not** run: it trades the headline returns result for a 1.2%
correction, and the label table it would also relieve is queued behind `CHART_EXPOSURE_EXPONENT`
anyway.

#### What it is

`RegionalRecordData.stationsDropped` is a **one-way latch per record per region**. Each week, once a
record is genuinely past its own peak, every market that still carries it rolls once against
`ChartSimulator.GetStationDropChance(support, weeksSincePeakUnits)`. A market that cuts the record
sets the latch, its rotation is cut to `STATION_DROP_RESIDUAL` of its previous level *instead of*
being lerped, and nothing anywhere puts it back.

- `support = unitsThisWeek / peakWeeklyUnits`, the signal §12.4r reserved. `peakWeeklyUnits` is a
  running maximum, so support is exactly 1 all the way up the climb and **the hazard is structurally
  incapable of firing on a rising record.**
- Two competing reasons to cut, combined as `1 − (1−fade)(1−burn)`: the local sales reports have gone
  soft (`fade`, opening at 80% of peak and maxed at 25%), or the record has simply been on too long
  (`burn`, a weak backstop from eight weeks past peak). Burn exists to guarantee termination — a
  support-only hazard leaves a record that never fades on the playlist forever.
- **The latch is the chosen answer to the §12.4r hysteresis flag**, and it is enforced at every writer
  of `radioPlay`, not just the regional pass: `ApplyBreakoutDiscovery` (both the source-region and the
  neighbour-propagation gains) and the public `AddRadioPlay` hook now honour it. Awareness and jukebox
  gains still land in a dropped market — people still hear about a record — but rotation does not.
  Re-add rate is therefore 0 by construction, and `radioPanelShare` in `records.csv` is the instrument
  that proves it: it may only ever fall, so any row where it rises is a leak.

New telemetry: `records.csv` carries `weeksSincePeakUnits` and `radioPanelShare` (the reach × population
weighted share of the panel still carrying the record). `chart.py drops` reads drop timing by peak
band, the re-add rate, and the panel curve against the sales trajectory.

D6 probe 98 (98–98q) covers it. Every fixture is relational per the §12.4n lesson — the grace window is
*discovered* from the function rather than asserted at a number, so re-deriving the constants cannot
rot them. It asserts the hazard is zero on the climb, monotone in both inputs, bounded, terminating
via burn, and — the one that matters most — that a latched market is never a candidate again even if
its rotation is externally restored.

#### The fatigue clock is re-keyed, not deleted, and that decision is measured

§12.4r says the drop must **supersede** `RADIO_FATIGUE_DECAY`, not stack on it. Deleting it outright
was costed first and is wrong, for a reason that only shows up in the trajectory data:

| top-10 record, `d7-buildonly-52-1001` | wk 9 (peak) | wk 12 | wk 20 |
|---|---:|---:|---:|
| median `radioHeat` | 0.738 | 0.706 | 0.282 |
| `targetHeat` with the fatigue term removed | ~0.95 | ~0.92 | ~0.90 |

`UpdateRadioHeat`'s `qualityFactor` is an **ageless constant** — `quality^1.8 * 0.7` is 0.51 of the
target for a quality-0.837 record — so with the clock gone, rotation *rises* from 0.73 to ~0.90 across
weeks ten to fourteen instead of falling. `AIRPLAY_CONVEXITY = 5` turns that 1.28x into **≈3.4x the
airplay points**, precisely where the U-shaped share is already too high. The station drop is linear in
surviving panel reach and cannot counter a multiplicative rise of that size.

So what was wrong with the clock was its **phase, not its existence** — exactly the §12.4p diagnosis —
and it is now keyed to `weeksSincePeakUnits` rather than `weeksSinceRelease > 8`. That fixes both ends
at once: a hit is no longer fatigued the week *before* it peaks, and a marginal record that peaked at
week four no longer keeps undamped rotation for five weeks after it was commercially finished. During
the climb the term is exactly 1. **One decay, re-phased, plus one terminating event — not two decays,
and not none.**

#### Why the drop is deliberately kept off `radioHeat`

`radioPanelShare` is computed and logged but **nothing reads it back**. Airplay points are then
*linear* in surviving panel reach, which is the well-conditioned lever. Applying the same share
multiplicatively to `radioHeat` as well would compound through the convexity to `p^6`: at a surviving
share of 0.40 that is 0.4% of the airplay points rather than 40%, and the mechanic's severity would be
an artifact of an exponent §11.6.3 already calls provisional. It also keeps the change off the demand
model entirely, the same discipline the §12.4p build followed — `radioHeat` multiplies conversion
directly (§11.7), and holding units still is what makes the chart result attributable.

#### Sizing, replayed offline before the run

The hazard was sized against real trajectories rather than guessed: `hazard.py` reconstructs
`support` and `weeksSincePeakUnits` from `d7-buildonly-52-1001-records.csv` exactly as
`FinalizeWeeklySales` maintains them and replays the survival curve. At ceiling 0.80 / floor 0.25 /
max 0.40:

| top-10 peakers | wk 9 | wk 12 | wk 15 | wk 17 | wk 20 |
|---|---:|---:|---:|---:|---:|
| median support | 1.000 | 0.734 | 0.461 | 0.273 | 0.085 |
| weekly hazard | 0.004 | 0.076 | 0.239 | 0.360 | 0.400 |
| **panel still carrying** | **99.6%** | **87.1%** | **47.2%** | **21.1%** | **4.7%** |

Half the panel is gone about six weeks after the sales peak — around the point a hit is falling
through the twenties — and 95% by week twenty. The 41-100 band, which peaks earlier and lower, is cut
earlier on the same curve without any band-specific term.

Two effects this static replay cannot show: losing rotation costs rank, rank costs
`GetChartExposureWeight`, and lost exposure costs sales — which lowers support and accelerates the
next drop; and dropped markets stop counting toward `GetTotalRadioPlay`, so `chartedExpired`
retirement arrives sooner and the live population thins. Both were expected to make the realised curve
*steeper* than the replay.

**They did not.** Measured on `d7-drop1-52-1001` the panel runs 82.7% at week twelve against a
predicted 87.1%, 51.1% against 47.2% at fifteen, and 26.8% against 21.1% at seventeen — within a few
points and slightly *gentler*, not steeper. **The offline replay is a good predictor and is worth
running before any re-derivation of these constants**; the feedback loop is real but second-order at
this severity.

#### What was deliberately left out

- **No size or rank leniency.** A #1 and a #80 record face the same curve at the same relative fade.
  A "protect the smash" term would be a fifth mechanism feeding the §11.4 item-3 positive-feedback
  loop, and §12.4q already found the tenure size gradient is correct and not the defect.
- **No per-region support signal.** The roll is per market; the signal is national. With only seven
  regions a per-region ratio is a noisy read of a small number, and under a one-way latch one bad
  week would cut a market permanently.

### 12.5 This is still a demand-model change

Item 1 moves units, revenue, label economics and every acceptance band downstream, so it needs:

1. A units and revenue check against `d7-v5verify-decade-522-1001` before anything else is read.
   Flattening the launch window will cut gross units unless the curve is re-normalised; decide
   deliberately whether total decade units should be held constant.
2. The full label acceptance table at decade scale (§11.7 — standing requirement).
3. The §11.5 diagnostics, plus the two new instruments this pass built: the peak decomposition of
   §12.1 and the debut-vs-peak-position measure of §12.2. Debut-at-peak share is the single cheapest
   read on whether the change worked.
