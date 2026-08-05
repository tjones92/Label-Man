# D7 emergent-genre formation — session handoff

Opened August 4, 2026. Updated August 5, 2026. Branch `d7-genre-decade-calibration`.

Sibling of `D7GenreChartDivergenceHandoff.md` (the chart-divergence arc) and
`D7GenreDecadeCalibrationHandoff.md` (the chart mechanism).

## 0. The one-line brief

**This arc is closed.** The formation regression is repaid and over-repaid, and the investigation
it was blocking has resolved: the remaining Mode A deficit is **not** supply, **not** signing, and
**not** release conversion. It is a missing chart-efficiency dimension. §8 is the handoff for that.

**August 5 session: §8 is now measured rather than inferred, and the framing changed. Sunshine Pop
is not under-charting — it sits ON the model's own slots-vs-share line, in every live year. Read
§11 before §8.2; two of §8.2's three steps are now differently posed and one proposed lever has a
hard floor that makes it unable to fix Jazz or Folk.**

## 1. State

| run | what it is |
|---|---|
| `d7-genretune2-decade-522-1001` | Pre-ladder reference. The original "−42%" figures are against this. |
| `d7-career-ladder-decade-522-1001` | §17. Emergent signings −63.0%. |
| `d7-contractterm-decade-522-1001` | §18. Emergent signings −42.4%. |
| `d7-formationbase-decade-522-1001` | **CURRENT HEAD.** This session. Formation base raised, cliff fixed, `Emerging` supply penalty removed. |

Build clean, both probe suites pass, **nothing committed.** Godot/Python invocations and the
"never pipe a long run through a PowerShell pipeline" rule are unchanged from the sibling doc §1.

```
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=522 --run=<name> --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --lean-probe --profile-performance --emergent-signing-floor=350
```

## 2. RETRACTION: §4 of the previous revision was wrong

The previous revision declared `CalculateResponsiveAnnualFormationTarget` inert — "`unmetShare` is
zero in all 1,566 measured run-weeks" — and sent the next session hunting for an unidentified
second driver. **There is no second driver. The servo was always the driver.**

The error was reading the wrong telemetry columns. The servo is fed
`seeking.Count(IsFirstContractProspect)` (`ArtistManager.cs:742-743`), published as
**`freshSeeking` / `freshLatent`**. The retracted analysis used `seekingProspects` /
`latentProspects`, which count everyone. Recomputed correctly, mean annual target x 10 years
reproduces every run's formation total to within 1%:

| run | mean target | x10 | measured |
|---|---:|---:|---:|
| genretune2 | 667 | 6,670 | 6,657 |
| career-ladder | 329 | 3,290 | 3,232 |
| contract-term | 483 | 4,830 | 4,420 |

**Lesson worth keeping:** when a mechanism looks inert, check that the telemetry column matches the
variable passed at the call site, not the one with the matching parameter name.

## 3. What actually drove the regression

The seeded first-contract reserve drains and never refills — `freshSeeking + freshLatent` goes
**4,052 (1960) -> ~15 (1965 onward)**, and `neverSignedUnsigned` hits 0.0 from 1965 in every
pre-HEAD run. From 1965 the servo's supply term is effectively zero, so formation volume became a
pure function of `affordableHiringVacancies`.

§17-18 correctly *reduced* standing vacancies (clean exits, immediate re-signing, fuller rosters:
1969 rostered 1,187 -> 1,219). Formation fell with them. Emergent genres, which emerge after 1964
and have no seeded population by construction, paid the entire tax.

**The baseline was never a restoration target.** genretune2's 6,657 formations came from vacancies
standing open because performance-dropped acts were cooldown-blocked from re-signing. Chasing that
number would have restored the bug. The previous revision's §4.1 step 4 ("back to the ~2,552
baseline") should not be followed.

## 4. The change at HEAD

- **`BaseAnnualRuntimeFormationCount` 300 -> 2200**, **`MaximumAnnualRuntimeFormationCount`
  1200 -> 3000.** The base is the knob, not the ceiling and not the gain:
  - the old ceiling was exactly `base * (1 + gain)` = 300 x 4 = 1200, **unreachable by
    construction** — it clipped 0 of 522 weeks. Raising it alone would have been inert.
  - the servo is a negative-feedback loop whose setpoint is label vacancies. Raising `gain` only
    reaches that same setpoint faster; it can never build a surplus.
  - 2,200 is sized to measured demand: from 1965 `firstTimeSignings` equals formations exactly
    every week, ~100 roster seats sit permanently unfilled, and total slot-fills run ~2,020/yr
    (~1,030 first-time + ~990 re-signings). 2,200 is one absorption cycle — the first value that
    leaves a real surplus.
  - Applies to **all ten years**, by author's preference. Acts form because scenes exist, not
    because labels have open chairs, and there is no reason 1960 should produce a seventh as many
    new acts as 1966. The 1960 seeded reserve now reads as the pre-existing industry.
- **Mid-year formation cliff fixed.** The annual ceiling now measures against the year's peak
  target rather than the current week's. The old form let a vacancy dip retroactively declare the
  year over-supplied *and* discard the fractional carry — 1968 at the previous head asked for a
  mean 569 and delivered 473 across 35 live weeks of 52.
- **`GenreSupplyService` `Emerging => .65f` removed** (`Declining => .35f` kept). It taxed a genre
  35% during its one-year launch window; measured formation share against authored baseline share
  was 0.44-0.45 for genres in that window versus ~1.0 for established ones.
- **Formation accumulator is now `double`.** In float32 the rounding error over 52 additions scales
  with the increment and overran the 1e-5 epsilon at the raised quota, losing the last act of the
  year. The old 300/1200 quotas stayed inside it by luck.
- **`--emergent-signing-floor=N`** — standalone early abort, no control run required, fires once at
  the end of 1965. Genres are derived from the catalog (`EmergenceYear > 1960`) rather than listed,
  so retiming a genre cannot stale the gate. HEAD scored `signed=1213 formed=2700` against 350.
- **Quadratic removed from the population layer** (see §7).

## 5. Result at HEAD

**The unsigned reservoir now exists.** Emergent signed/formed **100% -> 60.1%**. The previous
revision's §3 headline — "there is no unsigned reservoir of emergent-genre artists at all, in every
run" — is no longer true. Labels can finally select instead of taking everyone who forms.

| | genretune2 | contractterm | **HEAD** | band |
|---|---:|---:|---:|---|
| all formations | 6,657 | 4,420 | **22,000** | — |
| emergent formed | 1,178 | 674 | **2,934** | — |
| emergent signed/formed | 100% | 99.9% | **60.1%** | — |
| distinct #1s | 170 | 176 | 176 | ~203 |
| mean #1 tenure | 2.76 | 2.69 | 2.69 | 2.57 |
| mean chart life | 8.20 | 7.94 | 7.99 | 7.48 |
| breadth | 454 | 470 | **479** | 400-600 PASS |
| MidTier 1969 | 32 | 27 | **37** | 25-40 PASS |
| owner-Major 1968 | 44.3 | 40.4 | 42.2 | 45-52 FAIL |
| owner-Major 1969 | 45.8 | 45.3 | **42.9** | 45-52 FAIL, lost |

`MidTier 1969` is off the floor. **owner-Major 1969 left a band it had been passing** — the
volume-denominator effect: a larger unsigned pool lets more indies fill rosters and chart, diluting
owner-Major entry share. Author's call: deprioritized, it may return with other adjustments.

Metric definitions that reproduce the sibling doc exactly, recorded because two were rediscovered
the hard way: owner-Major is `ownerMajorEntries / chartEntries` (**not** `ownerMajorFamilyEntries`);
breadth is `cumulativeFirmsCharting`; MidTier is the annual `midTierFirmsCharting`; chart life is
the mean of `weeksOnChart` over records that **charted at all**, from the last row per `recordId`.

## 6. The finding this unblocked, and it is the whole point

Restoring supply was necessary and is **not sufficient** — the previous revision's §2 was right.
Sunshine Pop went 2 -> 3 year-end slots, inside the noise floor.

But the diagnosis is now measured rather than assumed, and **the original hypothesis of this whole
arc is false**. Emergent genres do not fail to build chart-worthy credibility. They convert
releases into charting records **better** than established ones:

- emergent **9.95%** of releases reach the chart vs established **5.13%**
- FolkRock (15.47%) and PsychedelicRock (14.96%) are 1st and 3rd in the entire simulation

Nor is throughput the answer: total releases moved +2.4% while the artist pool grew 5x, so release
volume is label-bound. Author's call: **this is correct and intended** — the economy is balanced
around it, and a large surplus of unsigned talent is historically accurate.

The real defect is in §8.

## 7. Population-layer quadratic (fixed)

The raised formation rate exposed cost that the 300/yr rate had hidden. `ArtistManager` ran a
`List.RemoveAll` over the unsigned pool **per artist**, inside loops over the whole registry — five
sites — and `ReconcileLifecycleAndOwnership` additionally swept **every label's roster per inactive
artist**, twice a week. `seekingMissingFromUnsignedPool` did a `List.Contains` per seeking artist.
All are quadratic in exactly the pool a raised formation rate grows. Measured cost before the fix:
1.47x wall clock, `populationLifecycleSeconds` 3.26x.

Replaced with mark-and-flush batching over a reused `HashSet`. Safe because every marked artist has
just become ineligible and the existing `ReconcileEnabledUnsignedPool` integrity sweep would drop it
regardless — the flush is an optimization on top of a guarantee that already held, so a missed flush
costs a pool entry the next sweep removes, never a stale entry that survives. `RemoveAll` is stable
and the batch is a set of identities, so survivor order is unchanged.

**Verified inert, not assumed:** both probe suites pass and all 8 compared telemetry files are
byte-identical to the pre-fix probe run (`d7-formation-probe52` vs `d7-quadfix-probe52`).

## 8. NEXT SESSION: the missing chart-efficiency dimension

**A genre's chart presence per point of market share is an independent historical dimension, and
the model does not have it.** Slots per point of unit-share, 1969:

| | SunshinePop | CountryRock | EasyListening | Soul | FolkRock | Jazz | Country |
|---|---:|---:|---:|---:|---:|---:|---:|
| history | **16.44** | 4.58 | 2.35 | 1.60 | 0.68 | 0.49 | **0.26** |
| model | **0.00** | 1.24 | 0.51 | 1.68 | 1.31 | 0.70 | **1.78** |

History spreads this **63x**. The model spreads it **3x**, and `corr(history, model) = -0.32` —
not merely compressed but **inverted**. Country, which should be the least chart-efficient genre in
the decade, is the model's most; Sunshine Pop, which should be the most, is zero.

This is one defect with two signs, and it is the two largest year-end misses at once: Country
**+47 slots** while *under* on market share, Sunshine Pop **-27 slots** while its market share is
one of the best fits in the sim (MAE 0.27; 1969 actual 0.70% vs 0.73% target).

**Why the model cannot express it:** chart conversion is a pure function of the authored demand
baseline — `chart% of releases = 27.80 x baseline²`, corr 0.841, consistent with the quadratic
transfer law. There is no per-genre term for hit concentration, so a genre cannot turn a small unit
share into heavy chart presence the way singles-driven pop did.

### 8.1 Author's correction, recorded so it is not re-litigated

An earlier reading of this session treated Sunshine Pop's 16.44 as the two benchmarks
*contradicting* each other, and proposed resolving it by moving the market-share target. **That is
wrong. Both benchmarks are authoritative.** Sunshine Pop really was a genre that took a small
market share and charted heavily with it — it wants to do exactly what Country is erroneously doing
in this model. The share table's note "Real but never a trade category" means the *tag* was absent
from 1960s trade literature, not that the genre was commercially small.

The sibling doc §2 rule still stands and is not in tension with this: a baseline keyframe is a
demand quantity, the market table wins on baselines, and a genre that charts wrongly at a correct
market share is a chart-side defect. Sunshine Pop is precisely that case.

### 8.2 Order of work

1. **Add a chart-efficiency term independent of the demand baseline.** This is the mechanism fix and
   it is what makes both Country and Sunshine Pop right. Size against the history row above; the
   target is a ~63x spread, so the term needs real range, not a nudge.
2. **Retime Sunshine Pop's late keyframes** to strengthen 1969 — authorized by the author. It is
   wanted independently, but note it cannot produce a 63x spread on its own and must not be used as
   a substitute for (1). Size by `sqrt(target/current)` for the quadratic.
3. **Re-audit all genres on both benchmarks.** Current totals: year-end absolute slot error **642**,
   market-share absolute error **173.6 pts**. Worst year-end offenders after Country: RnB and
   TraditionalPop have correct decade totals but wrong shapes (RnB +35 late, TraditionalPop -12
   late) — pure timing errors; Jazz +37 and Folk +35 are large over-charts on small genres;
   BritishBeat -30, GarageRock -23, BritishPop -15 under-chart. RockAndRoll's -31 is **expected and
   correct** per the sibling doc's misclassification caveat.

## 9. Things not to redo

- **Do not re-derive the career ladder, the probation window, or the contract term.** Sibling §17
  and §18 are settled and probe-covered (`2d-2r`).
- **Do not look for a scouting, lane-competition, artist-choice or roster-occupancy explanation for
  the emergent deficit.** Dead before, and now doubly so: emergent acts are no longer signed at
  100%, and they out-convert established genres.
- **Do not treat formation, signing, or release conversion as the Mode A lever.** All three are
  measured and closed by §5-6.
- **Do not raise `MaximumAnnualRuntimeFormationCount` or `FormationDemandGain` to move formation
  volume.** §4: the ceiling was unreachable by construction and the gain cannot beat the servo's
  own setpoint. The base is the knob.
- **Do not trust the previous revision's §4.** Retracted in full by §2.
- **Do not resolve Sunshine Pop by moving its market-share target.** §8.1.
- **Do not read single-seed genre deltas under ~50 points as signal** (sibling §10). Sunshine Pop
  2 -> 3 is noise; PsychedelicRock 32 -> 53 and FolkRock 25 -> 45 are not.
- **Do not look for a Sunshine Pop-specific chart defect.** §11. Its residual against the model's
  own slots-vs-share line is **+0.33 slots/year** — it charts slightly *better* than its share
  buys. Nothing in its chart path is broken.
- **Do not treat release dilution / units-per-record as the lever.** §11.2. Adding units-per-release
  to the slots regression moves R² from 0.864 to 0.865.
- **Do not try to fix Jazz or Folk by cutting airplay.** §11.4. Airplay is bounded above by 100% of
  itself, so zeroing it only reaches ~0.42x chart points per unit. Jazz needs 0.16x.

## 10. Still open, unchanged and out of scope

- **owner-Major 1968/1969** below band. Deprioritized by the author this session.
- **Label buzz in `CalculateArtistChoiceUtility`** — sibling §17.4. Real channel (18.7% of
  signings); `momentumScore` is a dud measure that would make it inert.
- **Wire the momentum engine** — sibling §14.
- **`SingleOrientation` and the unlocated 1.182x residual** — sibling §18.4.

## 11. WHY SUNSHINE POP DOES NOT CHART — measured, August 5

All figures from `d7-formationbase-decade-522-1001` (HEAD). No new run was spent.

### 11.1 The whole model is one line, and Sunshine Pop is on it

Across every genre-year with >=30 releases and nonzero share:

```
yearEndSlots = 1.409 x marketUnitsShare%  -  1.528        R2 = 0.864
```

That is the entire genre-to-chart relationship. Sunshine Pop's residual against it:

| year | baseline | share% | releases | slots | line predicts |
|---|---:|---:|---:|---:|---:|
| 1965 | .29 | 0.68 | 93 | 1 | −0.6 |
| 1966 | .49 | 1.21 | 202 | 0 | 0.2 |
| 1967 | .46 | 2.49 | 281 | 2 | **2.0** |
| 1968 | .35 | 1.31 | 229 | 0 | 0.3 |
| 1969 | .22 | 0.70 | 175 | 0 | −0.5 |

**Mean residual +0.33 slots/year.** Sunshine Pop charts *slightly better* than its market share
buys. It is not refusing to chart; it is being paid exactly what it earns.

The intercept explains the **zero specifically**: −1.528 means a genre needs 1.79% market share to
score its first slot, and Sunshine Pop's authored targets are 1.47 / 1.87 / 1.42 / 0.73 for
1966-69. **But the intercept is not the deficit** — see §11.2a. Removing it entirely moves Sunshine
Pop 1969 from 0 slots to 0.70 against a benchmark of 12.

**Country is the same fact read at the other end**, and is *also* not a defect. Its residual is
−0.49 slots/year — Country charts slightly *worse* than its line. Its +47 decade slot error is
entirely that its authored baseline gives it 6-11% of units and the line converts that at 1.409.
The two largest year-end misses in the sim are one line and two authored baselines, not two bugs.
**Soul is the control and it is exact: model 179 decade slots against a hand-counted 179.**

### 11.2a No share-only rule can fix this — proved, not argued

History is **also** close to a line on share: `benchSlots = 1.146 x targetShare% − 0.319`, R² 0.619
(model: `1.409x − 1.528`, R² 0.864). Reallocating the 100 slots each year under three different
share-only rules, against the model's own actual shares:

| rule | abs slot error (per genre-year) |
|---|---:|
| model, as it runs | 683.0 |
| model line, refit | 638.5 |
| **history's line applied to model shares** | 674.0 |
| **pure proportional, no threshold at all** | 691.2 |

All four are the same number. **The dimension is orthogonal to market share by construction**, so
no reshaping of the share→slot curve — slope, intercept, or threshold — can recover it. Sunshine
Pop 1969 goes 0.00 → 0.47 (history's line) → 0.70 (pure proportional) against a benchmark of 12.

The reason is visible in *which* small genres chart. History's 16 sub-1%-share slots in 1969 go to
five named genres (SunshinePop 12, Comedy 1, GarageRock 1, BaroquePop 1, SurfRock 1) out of the 15
genres in that band — not spread across them. Size does not predict which; identity does.

### 11.2b The one table that states the whole problem

Where the 100 slots sit, bucketed by the genre's own market share:

| 1969 | under 1% | 1-2% | 2-5% | over 5% |
|---|---:|---:|---:|---:|
| model | **0** slots / 0 genres | 2 / 1 | 17 / 5 | **81** / 7 |
| hand count | **16** slots / 5 genres | 17 / 5 | 38 / 8 | **38** / 3 |

History puts **33 of 100 slots on genres holding under 2% of the market. The model puts 2.** And
this is not a share-distribution artefact — the two agree almost exactly on how many genres exist
at each size (1969: 31 vs 30 genres above 0.5% share, 23 vs 23 above 1%, 15 vs 15 above 2%).

### 11.2c Market share is already calibrated; slots are not

Decade, mean annual share against `AdjustedHistoricalGenreShareTargets`, all 42 genres:

| | total absolute error |
|---|---:|
| market share | **17.8 points** |
| year-end slots | **399 slots** |

Only three genres miss share by more than 1.3 points (Country −2.19, RockAndRoll −1.75,
ContemporaryFolk +1.77). **Sunshine Pop's share is +0.11.** There is no volume problem and no
volume-allocation problem left to solve.

**Country is not eating Sunshine Pop's units.** It is the single largest share *deficit* in the
sim — 7.75% against a 9.94% target — while holding +47 surplus slots. It needs *more* units and
*fewer* slots, so its share cannot be the donor.

The slot books:

- **surplus, 148 slots:** Country +47, Jazz +37, Folk +35, PsychedelicRock +18, ContemporaryFolk +11
- **deficit, −205 slots:** RockAndRoll −31 (the misclassification caveat, expected), BritishBeat −30,
  SunshinePop −27, GarageRock −23, TeenPop −21, BritishPop −15, SurfRock −13, Comedy −13,
  DooWop −10, Bubblegum −8, EasyListening −8, HardRock −6
- **exact or near-exact:** Soul 179/179, TraditionalPop 132/134, RnB 92/94, FolkRock 43/40

Every surplus genre except PsychedelicRock is album/adult/specialist. Every deficit genre except
RockAndRoll is a singles / AM-Top-40 genre. That is the split, and it is the same one §11.5 names.

### 11.2 Three explanations killed on the way

- **"Its records are diluted across too many releases."** Sunshine Pop does carry ~1,405 releases
  per point of unit share against ~580 for genres that chart, and units-per-release does track
  the baseline at r=0.90. But adding units-per-release to the regression moves R² **0.864 ->
  0.865**. Concentration is a *consequence* of the baseline, not an independent channel. Sized
  the channel before the suspect; the channel is inert.
- **"Mode A — no record of the genre can become a hit"** (sibling §6.1). **Dead at HEAD.** Sunshine
  Pop scores 35 top-40 weeks and **17 top-10 weeks** in 1967, and 18/9 in 1965. It makes hits.
- **"It's the singles/album split."** At 100% single orientation Sunshine Pop reaches ~0.90% of the
  singles market. Still one slot. Worth fixing for other reasons (§11.4) but it is not this.

### 11.3 The arithmetic ceiling — check this before designing anything

1969: total market 195.5M units, of which 62.8M flow through charting records. A year-end slot
costs **~550k chart units** at the median and ~300k at the margin. Sunshine Pop's **entire 1969
market is 1.37M units.** Its hard ceiling, if every unit it sells went through a charting record,
is **~2-4 slots.** The hand count wants **12**.

On the model's own line, 12 slots requires **9.6% market share**. The authored target is 0.73%.
**No amount of concentration, format tilt, or supply routing closes a 13x gap** — the missing
factor has to be a genuine per-genre multiplier on chart points *per unit*, or it does not exist.

### 11.4 Where the 63x actually has to come from, and where it runs out

Decade required multiplier on slots-per-unit-share, model against hand count:

| | Jazz | Folk | Country | PsychRock | **Soul** | DooWop | HardRock | GarageRock | **SunshinePop** |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| model slots | 44 | 57 | 89 | 44 | **179** | 17 | 3 | 3 | **3** |
| hand count | 7 | 22 | 42 | 26 | **179** | 27 | 9 | 26 | **30** |
| needs | **0.16x** | 0.39x | 0.47x | 0.59x | **1.00x** | 1.59x | 3.0x | 8.7x | **10.0x** |

0.16x to 10x is the 63x §8 named, now derived per genre. Soul at exactly 1.00x is the control.

**The channel exists and the up-lever has the range.** Chart points are
`(units + airplay x eraWeight) x survey`, and airplay is **58% of points at the chart bar in 1969**
(14% in 1960 — `GetAirplayEraWeight` ramps 0.60 -> 1.00 across 1960-68). `AIRPLAY_CONVEXITY = 5`
applies to the record's own rotation, and `genreAcceptance` sits **inside** that fifth power via
`UpdateRadioHeat` (`ChartSimulator.cs:840`, `targetHeat = (...) * genreAcceptance`) — genre radio
*access* is divided out and paid back linearly, but this term is not. So a radio-side genre ratio
of `r` yields `r^5` on airplay points. Sunshine Pop's 10x needs `r ≈ 1.75-2.0x`. Measured rotation
per eligible record is 0.005-0.019 against a `AIRPLAY_REFERENCE_PLAY` of 0.30 and a [0,1] clamp on
`radioHeat`, so there is no saturation in the way.

**The down-lever has a hard floor and it is era-dependent.** Airplay can only be removed down to
zero, so a genre stripped of all airplay keeps its sales points. The floor is `1 − A` where `A` is
airplay's share of chart points, and `A` is **not constant across the decade**: measured at the
chart bar it runs **14% (1960), 37% (1962), 47% (1965), 45% (1967), 58% (1969)**. So a full airplay
strip is worth a 0.42x cut in 1969 and only a **0.86x** cut in 1960.

**That splits the three over-charters into two unrelated problems.** Slot surplus by era:

| | 1960-64 | 1965-69 | total | airplay lever there |
|---|---:|---:|---:|---|
| **Country** | 0 | **+47** | +47 | strong (A = 45-58%) |
| **Jazz** | **+32** | +5 | +37 | **near-useless** (A = 14-37%) |
| **Folk** | +23 | +12 | +35 | weak, mostly early |
| PsychedelicRock | 0 | +18 | +18 | strong |
| **SunshinePop** | 0 | **−27** | −27 | **strong** |

- **Country's entire surplus is 1965-69** (+14 and +17 in 1968-69 alone); it is *under* in 1960-63.
  That is exactly where airplay is worth most, so the radio down-lever does the bulk of Country.
- **Jazz's surplus is 32 of 37 in 1960-64**, where cutting *all* of its airplay removes only 14-37%
  of its points. Airplay cannot fix Jazz. Same for Folk's early two-thirds.
- **Sunshine Pop's deficit is entirely 1966-69**, the airplay-rich end. The up-lever is strongest
  exactly where it is needed — this is a real alignment, not a coincidence.

So: **Country and PsychedelicRock are airplay work; Jazz and Folk are format/denominator work.** A
session that tries to fix all four with one radio term will burn a decade run finding this out.

### 11.5 The two-sided design this implies

1. **Split `genreAcceptance` into a sales acceptance and a radio acceptance.** Today one authored
   scalar drives both — sales roughly as `baseline²` (the transfer law) and airplay as
   `baseline⁵`. That single number is *why* there is no chart-efficiency dimension: a genre
   physically cannot be small-selling and heavily-programmed. Feed the new radio value to
   `UpdateRadioHeat:840` only. This is the up-lever (SunshinePop, GarageRock, Comedy, HardRock,
   DooWop) and it is historically the right object — Sunshine Pop was an AM Top-40 format genre.
2. **Make the chart's denominator the pop-singles universe.** `genre-decade-shape`'s `marketUnits`
   is accumulated over **all formats** (`ChartAuditRunner.cs:2544`, fed the full `records` list —
   1969: 195.5M, of which 44.9M album) while the Hot 100 it is scored against is singles-only.
   The market-share benchmark's denominator and the slot benchmark's numerator are measured over
   different universes. This is the down-lever for Jazz/Folk/Country/EasyListening, and
   `SingleOrientation` is the existing authored home for it.

**Sunshine Pop's 1969 keyframe: settled, and it is worth ~1 slot of the 12.** Its hand-counted
slots go 4 -> 12 across 1968-69 while the authored share target goes 1.42% -> 0.73%. Author's
ruling (carried from the prior session): **the 1969 keyframe decline is erroneous** and should be
raised — the year-end count shows the genre peaking in 1969, so the demand curve should not be
falling into it. This is a real fix and is wanted.

It is not, however, the lever. On the model's line a corrected 1969 share of 1.87% (its 1967 level)
buys **1.1 slots**; even 3.0% buys 2.7. The benchmark wants 12. **Raise the keyframe because it is
wrong, then size the efficiency term against the remaining ~10 slots** — and size it on the
1966-68 pairs (2.7-5.3 slots per point), not on 1969 alone, which is the single most extreme year
in the whole benchmark and will overshoot the rest of the genre's decade if aimed at directly.
