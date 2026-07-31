# D7 genre decade calibration — live handoff

Last maintained: July 31, 2026. Branch `d7-genre-decade-calibration`, off merged `main`.

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
| `d7-comp-decade-522-1001` | + compilation era curve and the 1965 statement opening. **Current reference.** |
| `d7-comp-probes-52-1001` | 52-week probe run, D5 + D6 1-97 green. |

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

## 7. OPEN REGRESSION: MidTier firms 27 → 21

The compilation curve moved every D7 acceptance metric in the right direction except one.

| target | baseline | after | |
|---|---:|---:|---|
| breadth 400-600 | 450 | **491** | PASS |
| below-MidTier dominant | 93% | 93% | PASS |
| Independent share of that | 78% | 75% | PASS |
| Small tail | 9% | 11% | PASS |
| **MidTier firms 25-40** | 27 | **21** | **FAIL** |
| owner-Major 1968 (45-52) | 46.5 | 49.2 | PASS |
| owner-Major 1969 (45-52) | 53.2 | **51.4** | **PASS — first time in band** |

Two things worth keeping: the 1969 owner-Major overshoot that survived the entire D7 arc (1.2-2.7
over on both seeds) fell into band as a side effect, and the prediction that 2.3x album costs would
squeeze marginal Independents and cost breadth was **wrong** — breadth rose. Better albums sell more,
and that outweighed the cost increase.

**Hypothesis for MidTier, not yet confirmed:** the cause is mechanical rather than economic. Breadth
up means a fixed chart is shared among more labels, so per-label charting counts fall, while MidTier
promotion needs 8 recent charting records and demotion bites below 4 — thinning the tier from both
ends. If that holds, the MidTier bars are calibrated against the old concentration and want a small
adjustment.

**Confirm from `release-strategy.csv` and the promotion/demotion events before tuning.** Prior D7 work
established that reasoning about tier flow from annual aggregates produced three wrong mechanism
claims in a row; decision telemetry settled it in one query.

## 8. Chart influence: country over-charts via longevity

The one genuine chart/market divergence, and it survives the §3.1 retraction because it is measured
directly.

| divergence (chart% − market%) | 1960 | 1963 | 1966 | 1968 | 1969 |
|---|---:|---:|---:|---:|---:|
| Country | +0.6 | +2.1 | +7.3 | **+8.3** | +6.9 |
| Soul | −2.4 | −1.1 | +1.6 | +4.9 | +4.5 |
| PsychedelicRock | — | — | −0.2 | −1.4 | −1.8 |

It is **longevity, not volume**. In 1967 country had 186 unique charting records against soul's 179 —
nearly identical — but averaged **6.6 chart weeks per record against soul's 5.2**, with a better mean
position (49.1 vs 54.5). That gap is essentially the whole +7.9.

So the question is why country singles persist ~27% longer, which is a chart-points/decay question,
not a genre-acceptance one.

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

1. **MidTier 27 → 21.** Confirm the mechanism from decision telemetry, then remedy. Blocking, since it
   is a live acceptance regression.
2. **The one-year lag (§5).** Decide keyframe shift vs supply response, then apply once across the
   catalog.
3. **Album era weight (§6.1)** — investigate before touching Comedy or Classical keyframes.
4. **Per-genre authoring:** Gospel down hard, Soul down at 1967, Bubblegum up, Jazz flattened, Folk /
   Surf / Garage pulled earlier.
5. Re-run decade, re-check the §7 acceptance table, then a holdout seed.

Deferred: the acclaim loop (§9); country chart longevity (§8) once the market-side work settles.
