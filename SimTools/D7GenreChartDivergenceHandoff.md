# D7 genre chart-divergence — session handoff

Opened August 4, 2026. Branch `d7-genre-decade-calibration`.

This is the working handoff for the **chart-divergence** arc. It is a sibling of
`D7GenreDecadeCalibrationHandoff.md`, which remains the authority for the chart-mechanism work
(airplay, the release ramp, the Hesbacher curve, the survey layer, the station drop) and for the
genre *market-share* calibration that produced the current state. Read §12.4u and §12.5 there for
the chart machinery; do not re-derive it here.

**The market-share half of genre calibration is done.** What remains is that a genre can hold the
right share of units and the wrong share of the chart, in both directions, and that is what this
document is about.

## 1. State

| run | what it is |
|---|---|
| `d7-drop-decade-522-1001` | pre-genre-work reference. Station drop, §12.4t of the sibling doc. |
| `d7-segcurve-decade-522-1001` | + civil-rights integration curve + year-end Hot 100 telemetry. The genre baseline for this arc. |
| `d7-genretune1-decade-522-1001` | first keyframe pass. Overshot Soul, undershot British Blues — this is the run that revealed the quadratic transfer law. Keep it: the tune1/tune2 pair is what the exponent was measured from. |
| `d7-genretune2-decade-522-1001` | **CURRENT REFERENCE.** Quadratic-corrected keyframes. Both benchmarks roughly halved. |
| `d7-psychedge-decade-522-1001` | + §7 psychedelic adjacency edges. Non-lean, so it carries the per-record telemetry the lean runs suppress. The §7 effect is **inside the noise floor** — see §10. |
| `d7-airpayback-decade-522-1001` | **REJECTED, kept as evidence.** Genre acceptance divided out of the airplay exponent. Inverted the chart. See §11. |

Validation ladder is unchanged from the sibling doc §2: build → D5/D6 probes via a 52-week run →
decade → holdout seed. Never pipe a long Godot run through a PowerShell pipeline; use
`Start-Process -NoNewWindow -Wait -RedirectStandardOutput`. **Do not rebuild while a run holds the
DLL.**

Godot: `C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe`
Python: `%LOCALAPPDATA%\Programs\Python\Python314\python.exe` (Scripts not on PATH; invoke by full path).

```
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=522 --run=<name> --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --lean-probe --profile-performance
```

## 2. The two benchmarks, and the rule for using them

Both live in `SimTools/`. **Tune against both. Neither alone is sufficient, and they measure
different objects.**

| benchmark | file | what it is |
|---|---|---|
| **market share** | `AdjustedHistoricalGenreShareTargets.csv`, pivoted in `D7P3AdjustedHistoricalGenreShareComparison.csv` (`targetSharePct`) | annual share of fulfilled units, normalized to exactly 100% per year. High confidence. |
| **year-end Hot 100** | `<run>-year-end-hot100.csv` vs the user's hand count (§3) | a ranked 100-slot list by cumulative annual chart points. Guide, not gospel — see the caveats. |

**Rule established with the author: a baseline keyframe is a demand quantity, so the market table
wins on baselines. A genre that charts wrongly at a correct market share is a chart-side defect and
must not be "fixed" by moving its keyframe.** Doing so trades a benchmark you control for one you
do not. This is the entire premise of the present arc.

Year-end benchmark caveats, from the author:
- It was genre-classified with a separate model, so individual rows can be wrong.
- Its columns sum to **101-110**, not 100, because ambiguous records were double-tagged. Treat
  gaps of 1-2 slots as noise.
- **Its late-decade Rock and Roll counts are misclassification** of genre-ambiguous records
  (e.g. "Brown Eyed Girl"). The authored Rock and Roll decline is correct and is to be preserved.
- Several 1969 rows were counted against a 101-record list.

## 3. The hand-counted year-end benchmark

Reproduced here so it is versioned rather than living in an `.odt` in Downloads. Counts are slots
out of ~100 (see the sum caveat above).

| genre | 60 | 61 | 62 | 63 | 64 | 65 | 66 | 67 | 68 | 69 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Soul | 6 | 9 | 7 | 15 | 14 | 22 | 22 | 28 | 28 | 28 |
| TraditionalPop | 22 | 16 | 15 | 20 | 14 | 11 | 8 | 9 | 12 | 7 |
| TeenPop | 28 | 18 | 20 | 8 | 5 | 7 | 6 | 9 | 2 | 2 |
| RnB | 12 | 18 | 28 | 21 | 10 | 2 | 3 | 0 | 0 | 0 |
| RockAndRoll | 14 | 12 | 12 | 11 | 8 | 3 | 6 | 6 | 5 | 2 |
| Country | 9 | 7 | 7 | 8 | 2 | 1 | 2 | 2 | 1 | 3 |
| DooWop | 7 | 10 | 6 | 3 | 0 | 0 | 0 | 1 | 0 | 0 |
| EasyListening | 5 | 8 | 8 | 5 | 3 | 4 | 5 | 0 | 6 | 8 |
| SurfRock | 3 | 2 | 1 | 6 | 9 | 2 | 3 | 0 | 0 | 1 |
| Comedy | 3 | 3 | 2 | 3 | 1 | 0 | 0 | 2 | 0 | 1 |
| Folk | 1 | 1 | 3 | 7 | 3 | 2 | 1 | 2 | 2 | 0 |
| BritishBeat | 0 | 0 | 0 | 0 | 24 | 15 | 8 | 3 | 3 | 0 |
| BritishPop | 0 | 0 | 0 | 0 | 2 | 12 | 6 | 7 | 3 | 4 |
| FolkRock | 0 | 0 | 0 | 0 | 0 | 12 | 14 | 6 | 5 | 3 |
| GarageRock | 0 | 0 | 0 | 0 | 3 | 5 | 12 | 4 | 1 | 1 |
| BritishBlues | 0 | 0 | 0 | 0 | 0 | 5 | 3 | 2 | 0 | 0 |
| SunshinePop | 0 | 0 | 0 | 0 | 0 | 0 | 4 | 10 | 4 | 12 |
| PsychedelicRock | 0 | 0 | 0 | 0 | 0 | 0 | 1 | 9 | 10 | 6 |
| Bubblegum | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 7 | 9 |
| Funk | 0 | 0 | 0 | 0 | 0 | 1 | 0 | 1 | 4 | 6 |
| HardRock | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 1 | 5 | 3 |
| Jazz | 0 | 1 | 1 | 0 | 1 | 1 | 0 | 0 | 1 | 2 |
| CountryRock | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 6 |
| Classical | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |

Smaller entries: AcidRock 3 (68), BossaNova 1/1/2 (63/64/68), Blues 1 (63,66), BaroquePop 1 (66,67,69),
SingerSongwriter 3 (69), Gospel 1 (69), ProtoMetal 1 (68), BluesRock 1 (68), Ska 1 (64), TexMex 1 (65).

## 4. THE TRANSFER LAW: share goes as baseline SQUARED

Measured across 260 genre-years that moved between `d7-genretune1` and `d7-genretune2`:

| | |
|---|---:|
| median exponent | **1.98** |
| mean | 1.90 |
| IQR | 1.27 - 2.51 |

**Size every keyframe change by `sqrt(target/current)`, never by `target/current`.** The first pass
sized linearly and missed hard in both directions — Soul was raised 1.37x and returned 2.0x the
share (27.9% against a 17.5% target), British Blues was cut 2.9x and collapsed 15x (12.8% -> 0.8%
against 4.1%). Correcting with the square root halved the error in one pass.

Transfer is **also field-dependent**: it is measured against whatever else competes for a normalized
100%, so cutting a genre that held 12 points raises everyone else's efficiency. The exponent sets
the size of a move; the field sets which way the rest of the catalog drifts underneath it. Expect to
re-measure after any large cut.

This law is recorded in the header comment of `Data/GenreCatalog.cs`. Do not rediscover it.

## 5. Where the market calibration landed

`d7-genretune2`, market-share error against target, all genres, all ten years: **303.0 -> 184.8**.
Year-end Hot 100 error across four sampled years: **401 -> 296**.

Chart health was checked and did not regress; one band improved:

| | drop | segcurve | **genretune2** | band |
|---|---:|---:|---:|---|
| units | 99.8% | 99.5% | **99.6%** | hold PASS |
| distinct #1s | 179 | 185 | **192** | ~203 |
| mean #1 tenure | 2.91 | 2.82 | **2.71** | 2.57 |
| mean chart life | 7.39 | 8.12 | **8.20** | 7.48 |
| breadth | 492 | 392 | **406** | 400-600 PASS |
| MidTier 1969 | 51 | 35 | **32** | 25-40 PASS |
| owner-Major 1968 | 46.2 | 44.6 | 44.3 | 45-52 FAIL |
| **owner-Major 1969** | 40.2 | 41.9 | **45.8** | **45-52 PASS** |

owner-Major 1969 entering band closes the standing label failure from sibling §12.4t. It was not
aimed at — it fell out of the genre field being less distorted. **Do not bank it without re-checking
the Major firm count** per sibling §7.2.

Chart life at 8.20 against 7.48 is the largest surviving chart-health miss and is *worse* than the
pre-genre run. Records × chart life = 52,100 slot-weeks always (sibling §12.4g), so this and the
record count are one variable.

## 6. THE OPEN PROBLEM: divergence, and it is two distinct failure modes

Residual error is now dominated by genres whose **market share is right and chart share is wrong**.
Measured on `d7-genretune2`, mean of (chartWeekShare − marketUnitsShare) over 1967-69:

| over-charting | | under-charting | |
|---|---:|---|---:|
| Soul | +7.6 | Classical | −1.3 |
| Country | +2.3 | TraditionalPop | −1.2 |
| FolkRock | +2.1 | Childrens | −1.0 |
| PsychedelicRock | +1.8 | EasyListening | −1.0 |
| Funk | +0.7 | Blues | −1.0 |
| HardRock | +0.6 | TeenPop | −0.9 |
| Bubblegum | +0.5 | BritishPop | −0.9 |

**Soul's +7.6 is probably CORRECT and must not be "fixed".** The hand count wants 28 of 100 year-end
slots on ~17.5% of units — soul is supposed to over-chart heavily, because it was radio-driven. Check
any proposed divergence mechanism against Soul as a control: a fix that flattens Soul is wrong.

### 6.1 Mode A — the top-40 ceiling. Some genres cannot produce a hit at all.

This is the sharper of the two and the recommended starting point. At 1967:

| genre | market | unique charting | wks/record | **top-40 weeks** | **top-10 weeks** | year-end slots (bench) |
|---|---:|---:|---:|---:|---:|---:|
| Soul | 20.4% | 192 | 7.8 | 640 | 168 | 31 (28) |
| **SunshinePop** | **1.7%** | 14 | 2.7 | **0** | **0** | **0 (10)** |
| **TeenPop** | 2.2% | 15 | 3.3 | **6** | **0** | **0 (9)** |

SunshinePop holds essentially its target market share (1.7% against 1.9%) and reaches the top 40
**zero times in the year**. Its records touch the 41-100 band for a mean of 2.7 weeks and vanish. A
year-end Hot 100 ranks by *cumulative* points, so a genre that never breaks the top 40 scores zero
slots no matter how many units it sells. TeenPop at 1960 is the same story (7 slots against a
hand-counted 28).

So the question is not "why is its share low" — it is **"why can no record of this genre become a
hit?"** Prime suspects, in order:

1. **Airplay is genre-stratified and is ~45% of chart points.** Sibling §11.7 established that every
   airplay shape concentrates on high-acceptance genres, and §8.3 that airplay widened the spread of
   the longevity index across genres. `genreRadio`, `radioDifficulty` and `GetSegregationFactor` all
   shape rotation. A genre with low radio access cannot accumulate the points a top-40 record needs.
2. **`GetRegionalRadioOpportunity`** (`GenreAcceptanceService.cs`) returns
   `clamp(.60 + routedAcceptance * .50, .35, 1.10)`. A genre with low routed acceptance is floored at
   .35 while a high one reaches 1.10 — a 3.1x access gap, then `AIRPLAY_CONVEXITY = 5` is applied to
   the record's own rotation.
3. **Units may be spread rather than concentrated.** 14 charting records on 1.7% of the market is not
   obviously wrong; what is wrong is that none of them is big. Check whether the genre's units are
   distributed across many small records (no star mechanism) or whether a few records are being
   denied rank they earned.

**Instrument:** `<run>-year-end-hot100.csv` is the direct read, and `genre-decade-shape.csv` carries
`top40RecordWeeks` / `top10RecordWeeks` / `uniqueChartingRecords` per genre per year, which is what
the table above is built from. A non-lean run adds per-record `chartPoints` and `radioPanelShare` for
the airplay decomposition (`chart.py tail`).

### 6.2 Mode B — adult/specialist genres over-charting a SINGLES chart

Country (17 year-end slots at 1969 against a hand-counted 3, on a market share that is now correct at
11.2% against 11.7%), plus Jazz, Classical and Comedy. These are genres whose records should mostly
not be singles at all.

`SingleOrientation` in `GenreCatalog` is the authored lever and was already moved for Classical
(.15 -> .04) in this pass. It did reduce Classical's slots but not to zero. Worth checking whether
`SingleOrientation` is actually load-bearing on chart outcomes or only on format choice — if a genre
releases few singles but each one charts strongly, the lever is in the wrong place.

**Note the tension deliberately left in place:** Country's baseline was *raised* in this pass to serve
the market benchmark, which made its chart over-presence worse. That was a considered choice under the
§2 rule, flagged in a comment at its `Add(...)` line. If the divergence mechanism lands, re-read
Country first.

## 7. SEPARATE INVESTIGATION: PsychedelicRock supply is capped and short

**This is probably NOT the same defect as §6 and should be investigated on its own.** PsychedelicRock
sits at baseline **1.00** — the cap — at both 1966 and 1967 and still realises only 4.6% against a
6.43% target. No keyframe can close it.

The measurement that localises it, at 1967 (`newReleases` per unit of baseline):

| genre | baseline | newReleases | **per unit baseline** |
|---|---:|---:|---:|
| Soul | 0.76 | 1070 | **1408** |
| TraditionalPop | 0.44 | 584 | 1327 |
| Country | 0.58 | 555 | 957 |
| Jazz | 0.39 | 356 | 913 |
| GarageRock | 0.38 | 224 | 589 |
| SunshinePop | 0.46 | 266 | 578 |
| BritishBlues | 0.44 | 204 | 464 |
| FolkRock | 0.80 | 350 | 438 |
| **PsychedelicRock** | **1.00** | **302** | **302** |

PsychedelicRock converts baseline into supply **~4.7x worse than Soul** and worst of every genre
measured. So the constraint is on the supply side, not on demand.

**PRIME SUSPECT — an artist-identity adjacency gate that exists only for this genre.**
`GenreSupplyService.FilterProspectivePsychedelicCandidates` (around line 146) *removes*
`Genre.PsychedelicRock` from an artist's candidate list entirely unless
`IsPsychedelicTransitionCompatible(artistIdentity, year)`, which requires

```
GenreMarketMomentumService.GetAdjacency(canonicalIdentity, Genre.PsychedelicRock) >= .12f
```

No other genre has such a filter. If most artist identities fall under the .12 adjacency bar, the
pool of artists that may *ever* record psychedelic rock is structurally small regardless of demand,
which matches the measurement exactly.

Steps, cheapest first:
1. Enumerate `GetAdjacency(identity, PsychedelicRock)` across the actual artist-identity distribution
   and compute what fraction clears .12. That is a pure offline calculation — no run needed.
2. If the fraction is small, decide whether .12 is defensible. Historically the psychedelic turn was
   taken by *existing* beat, folk-rock, garage and pop artists, which argues for a permissive bar.
3. Only then consider the radio side. The airplay hypothesis is secondary here because the deficit is
   visible in `newReleases`, which is upstream of any chart or radio effect.
4. Check `GetGlobalConcentrationBrake` and the `concentrationBrake` term as a secondary cause — a
   genre pushed hard by a 1.00 baseline may be throttling itself through the recent-supply brake.

There is also a `--genre-market-v2-probes` fixture path
(`GetProspectivePsychedelicCandidatesForProbe`) that takes an `applyCompatibility` flag, so the gate
is already independently probeable.

## 8. Recommended order

1. **§6.1, the top-40 ceiling.** Sharpest signal, cleanest instrument, and it covers SunshinePop,
   TeenPop and probably several small genres at once. Use Soul as the control — a correct fix leaves
   Soul over-charting.
2. **§7, PsychedelicRock supply.** Independent of the above, and step 1 costs nothing but a script.
3. **§6.2, adult genres on a singles chart.** Do this last; it is entangled with `SingleOrientation`
   and with the Country tension deliberately left open in §6.2.
4. **Then the three new genres** — PopRock, PsychPop, RootsRock (design settled with the author, see
   sibling doc discussion). They were deliberately held back from the keyframe pass so their
   calibration would not bake in a known chart bias. Add them only against a field whose divergence
   is understood.

Deferred and unchanged: the acclaim loop (sibling §9), the album-era weight question (sibling §10
item 4), and the one-year realization lag (sibling §5).

## 9. Things not to redo

- **Do not tune a genre's baseline to fix its chart share.** §2. This is the rule the whole arc rests on.
- **Do not size a keyframe linearly.** §4. Use `sqrt(target/current)`.
- **Do not flatten Soul's positive divergence.** §6. It is historically correct.
- **Do not trust the year-end benchmark's late Rock and Roll counts.** §2. Authored decline stands.
- **Do not use `genre-market-weekly.csv` for chart share** — it is region-scoped and has produced two
  retracted findings. Sibling §3.1. Use `genre-decade-shape.csv` or `year-end-hot100.csv`.
- **`IsGenreAvailableInYear` is gone** (removed this session). It was dead code whose year table
  contradicted `GenreCatalog`'s `EmergenceYear`/`DeathYear`, which are the real gates via
  `GenreSupplyService.IsAvailableForNewSupply`.
- **A genre past its `DeathYear` gets exactly zero new supply.** That is a hard structural zero no
  keyframe can override — it is what made TeenPop unreachable until its `DeathYear` moved 1965 -> 1971.

---

# Session of August 4, 2026 — findings

Everything below was measured this session. Two changes were attempted: one kept but unvalidated,
one rejected outright. The diagnostic findings are worth more than either.

## 10. THE VALIDATION LADDER IS MISSING A SEEDS REQUIREMENT

**A single-seed decade A/B cannot resolve anything below roughly 50 points of market-share error.**

Adding adjacency edges that can only reach `PsychedelicRock` moved the whole field, because the new
edges change `ChooseRuntimeSecondaryGenre`'s candidate list, which shifts the RNG stream, and the
decade diverges from there:

| | |
|---|---:|
| total market-share churn, genretune2 -> psychedge | 48.1 pts |
| **of which from genres those edges cannot reach** | **32.7 pts** |
| change in total market-share error | +13.1 pts |

Soul moved -3.09 share points at 1969 and Bubblegum +1.99 at 1968 off a change that cannot touch
either. So `184.8 -> 197.9` is not a result, and neither is the §7 supply number.

This does **not** retroactively weaken the arc's headline figures — `303 -> 184.8` is a ~120-point
move, comfortably clear of a ~33-point floor, and the §11 rejection below was clear of it by
+100 market / +695 year-end. Large effects are resolvable at one seed. Small ones are not.

**Rule: any change whose predicted effect is under ~50 market-error points needs 2-3 seeds before it
means anything.** Add the seed sweep to the ladder in sibling §2.

The same trap one level down is already recorded — sibling "52-week probes can't resolve #1 tenure",
where probe-suite flags alone moved an identical config from 36% to 7%.

## 11. REJECTED: genre acceptance does not belong outside the airplay exponent

**Do not retry this.** It is now also recorded in the `AIRPLAY_CONVEXITY` comment block in
`ChartSimulator.cs`, which is where anyone re-deriving it will look first.

The hypothesis was good and the code reads like a half-finished repair. `CalculateChartPoints` says
the exponent applies to "the record's own rotation only, with genre access divided out and paid back
linearly", and it does divide out `access` (`GetRegionalRadioOpportunity`). But `UpdateRadioHeat`
multiplies **national genre acceptance** into `radioHeat`, and `radioHeat` is what becomes
`radioPlay` — so a second, much larger acceptance term still rides inside the fifth power. National
acceptance spans .16 (Rock and Roll) to 1.00 (Psychedelic Rock) at 1967; Soul .87 against Sunshine
Pop .50 is 1.76x, which the exponent turns into **16.7x of airplay points**.

Dividing that level out of `ownRotation` and paying it back linearly, exactly as `access` is treated,
**inverted the chart**:

| | genretune2 | psychedge | **airpayback** |
|---|---:|---:|---:|
| market-share error | 184.8 | 197.9 | **286.5** |
| year-end slot error | 643 | 649 | **1344** |

The 1968 chart became Rock and Roll 43 / Rocksteady 13 / Doo-Wop 11 / Ska 10, with **Soul scoring
zero** against a hand-counted 28. The §6 Soul control fired exactly as the handoff warned it would.

Why, algebraically: the net level term becomes `level^(1-CONVEXITY)` = `level^-4`, which is 1452x for
Rock and Roll against 1.75x for Soul.

Why, conceptually — this is the part worth keeping: **rotation and `AIRPLAY_REFERENCE_PLAY` are
absolute spin levels.** "Own rotation" is only meaningful against an absolute reference. A record in
a genre nobody programmes is genuinely in light rotation; that is a physical fact about spins, and
the chart counts spins. Normalizing it away asserts that a Rock and Roll record played six times less
than a Soul record is "equally rotated for its genre" and should earn the same convex payoff. It
should not. `access` survives the same division only because it spans 1.83x rather than 6.2x, so its
distortion stays small.

**The genre amplifier this file warns about is load-bearing, not a bug.** It is what keeps
high-acceptance genres on top.

Note also for §6.1: `GetRegionalRadioOpportunity` returns `clamp(.60 + routed * .50, .35, 1.10)`, so
the `.35` floor is **unreachable** — the real access span is .60->1.10 = 1.83x, not the 3.1x §6.1
claims.

## 12. What Mode A actually is, as far as it is now measured

The top-40 ceiling is real and §6.1's framing of it is right. The cause is not airplay access, and
not any single multiplier. Measured on 1967:

| | |
|---|---:|
| mean per-record units, Soul vs Sunshine Pop | **5.7x** |
| week-1 units, before any feedback | 407 vs 167 — **2.4x static** |
| amplification (total / week-1) | 76x vs 33x — **2.3x dynamic** |
| week-1 radioHeat | .385 vs .360 — near parity |
| week-12 radioHeat | .398 vs .181 — **2.2x, none of it present at release** |

2.4 x 2.3 = 5.7. **Half the gap is a static multiplier chain; the other half accumulates after
release through the discovery loop.** Quality, label-tier mix and p90 quality are flat across every
genre, so this is not a talent or label effect.

Supporting structure, all measured:

- **Per-record sales are lognormal with a scale-invariant shape.** p95/mean is 2.8-4.4 for *every*
  genre regardless of size (sigma ~ 1.1). Genre scales the level, not the shape. The top-40 bar is an
  absolute units threshold (~103k annual / ~11k peak week at 1967), so a genre whose mean sits far
  enough below it cannot draw a qualifying record out of its own distribution at any release count.
- **Release-count reallocation buys nothing.** Per-record sales are computed independently, not
  divided from a genre pool, so `unitsPerRel` is just mean per-record units rescaled — supply
  proportionality would not raise anyone's per-record sales.
- **`SingleOrientation` is load-bearing on chart outcomes** (it multiplies single conversion via
  `GetFormatMultiplier`), which answers §6.2's open question — but .55 -> .90 on Sunshine Pop buys
  only ~1.24x against a needed ~1.75x, so it cannot close Mode A alone.

### 12.1 Hypotheses tested and killed, so they are not retried

| hypothesis | killed by |
|---|---|
| airplay access is the discriminator (§6.1 suspect 1-2) | chart points per unit at matched sales spans only 1.7x across genres |
| `GetEnabledSingleDemandMultiplier`'s smoothstep availability gate | Sunshine Pop's gate is **0.9999**; the gate only bites below routed acceptance ~.35 (Rock and Roll .169, Surf Rock .32, Acid Rock .24, Doo-Wop .011) |
| mean units proportional to baseline squared | dies on TraditionalPop — baseline .44 at index 1.37 against Sunshine Pop's .46 at 0.60 |
| genre acceptance inside the exponent | §11 — inverts the chart |
| `GetGenreMarketReach` is the static multiplier (§12.2, §16 item 2) | §12.3 — its only channel is `exhaustionFactor`, which is **0 at week 1 by construction**; the largest repair the table allows is worth +1.4% and gives Soul *more* than Sunshine Pop |
| `GetLiveSpecialistSingleOpportunityNormalizer` is the static multiplier | §12.4 — `IsSpecialist` is `Country or TexMex or Boogaloo`, so it returns exactly `1f` for both sides of the Mode A ratio. It is a **§6.2 lever**, not a Mode A one |
| `GetEnabledAlbumOpportunityWeight` / `GetAlbumAffinity` is the static multiplier | §12.4 — `decadeLift` saturates affinity to the clamp for every genre by 1967; forcing Soul off its explicit `.30` moves the ratio 0.989x at 1967 and **1.000x at 1969** |
| quality and career mix are flat across genres (asserted in §12) | §12.4 — **both false.** Quality spans .620-.701, which `QUALITY_EXPONENT = 4` turns into 1.63x of demand; Sunshine Pop is 96.1% NewSigning against Soul's 83.0%. Only label-tier mix is genuinely flat |

### 12.2 Still open, and where to look next

- ~~**`GetGenreMarketReach`**~~ — **killed, see §12.3.** It is a real stale-table defect and it is
  inert. Do not re-nominate it for either half of the gap.
- **The dynamic half** is the discovery loop: chart position -> radio -> awareness -> sales -> chart
  position. `RADIO_POSITION_BONUS_SALES_FLOOR` (15,000 units) is another **absolute** threshold that
  a small-genre record can never pay, so it never earns the position bonus that sustains rotation.
  The model has several absolute bars — the top-40/top-10 bands, the chart-entry cutoff, this floor —
  applied to a distribution whose level is genre-scaled, so small genres fall below all of them at
  once. Any fix probably has to act on the loop, not on a multiplier.
- **Trigger note for anything record-scoped:** `crossoverCandidateStrength` is circular and unusable
  as a gate — only 4% of Sunshine Pop records ever exceed 0.15, because it is downstream of the very
  breakout that is being suppressed. `peakRegionalBreakoutStrength` is not starved (31% of Sunshine
  Pop records clear 0.24) and p90 quality is flat across all genres, so a trigger built on local
  traction plus quality is a fair, genre-blind lottery. That is the place to start.

### 12.3 KILLED: `GetGenreMarketReach` is a real defect and it is inert

Measured on `d7-psychedge` per-record telemetry (the `saturation` column is the same
audience-weighted penetration the demand path computes).

**The defect is confirmed and is larger than §12.2 claimed.** Six legacy enum values in the table
are dead, not two — `BritishInvasion`, `Psychedelic`, `Motown`, `GirlGroup`, `Skiffle` and
`SkaRocksteady` never appear in a run's genre column. **31 of the 42 canonical genres fall to the
silent `.60` default**, carrying 33.2% of record-weeks and 28.8% of units. The one substantive
regression is `BritishBeat`, which gets `.60` where the authored intent (`BritishInvasion .80`) was
the table's second-highest value.

**It cannot reach the static half, structurally.** `GetGenreMarketReach` has exactly one caller,
`GetRegionalPotentialAudience`, and `potentialAudience` has exactly one consumer: `penetration` ->
`exhaustionFactor`. It never reaches `awareBuyers` — the staged V2 path passes **`potentialBuyers`**
(population x buying rate), not `potentialAudience`, into `CalculateSingleDemandStages`.
`UpdateSaturation` is telemetry-only. Since `penetration = unitsSoldTotal / potentialAudience`, it is
zero at first sale: mean saturation at `weeksSinceRelease` 1 is **0.000019** (max .000853), so
`exhaustionFactor >= 0.9997` for every record of every genre in week 1. Contribution to the 2.4x
static multiplier is **exactly zero**.

**It is inert on the dynamic half too, and points the wrong way.** Units-weighted `exhaustionFactor`
at 1967, against the largest repair the table permits (raise to TraditionalPop's `.95`):

| genre | reach | actual | at .95 | lift |
|---|---:|---:|---:|---:|
| Soul | .70 | 0.8833 | 0.8963 | **1.0147x** |
| SunshinePop | .60 | 0.9239 | 0.9368 | **1.0139x** |
| TeenPop | .75 | 0.9454 | 0.9506 | 1.0055x |
| TraditionalPop | .95 | 0.9147 | — | 1.0000x |

The repair gives Soul *more* than Sunshine Pop, so it **widens** the gap ~0.1% against a needed
1.75x. Worst case anywhere in the run is Folk at +6.2% on its peak week.

**The direction is inverted from the hypothesis.** Sunshine Pop suffers *less* exhaustion than Soul
because it sells less and therefore penetrates less. Reach modulates a term whose level is dominated
by sales — TraditionalPop at the table's top value still eats worse exhaustion than Sunshine Pop at
the default. This is a mild negative feedback on winners, not a handicap on losers.

Both figures are an **upper bound**: the column is the audience-weighted aggregate while the live
code computes penetration per region, and `exh(p)` is convex, so by Jensen the real per-region term
is even closer to 1.

This only re-confirms what the `SATURATION_POWER` comment block in `ChartSimulator.cs` already
recorded from the airplay work — *"Median saturation AT the sales peak is 0.0030 ... there is no
exhaustion to model."* **That comment is the general result: nothing routed through
`exhaustionFactor` can move this model.** Check that channel before nominating any future suspect
that lives on it.

Repairing the table is therefore a **correctness change, not a calibration one**. At ~1.4% it is two
orders of magnitude under §10's ~50-point noise floor, so no seed sweep could ever confirm it. It
must not be run as an A/B or banked as a result.

### 12.4 The static half, decomposed. The format multiplier is real; the two tables feeding it are not.

Both §16-item-3 suspects were run down. One is structurally inert, one is inert by saturation, and
the term they share turns out to be carried entirely by an authored lever.

**`GetLiveSpecialistSingleOpportunityNormalizer` returns exactly `1f` for Sunshine Pop and for Soul.**
`IsSpecialist` is `Country or TexMex or Boogaloo` and nothing else, so the normalizer cannot appear in
any Mode A ratio. **It is a Country-scoped multiplier**, which moves it out of Mode A and into §6.2 —
see below.

**`GetEnabledAlbumOpportunityWeight` is inert, and it is the same stale-table disease as §12.3.**
`MarketRegion.GetAlbumAffinity` switches on the raw enum with no `MapLegacy`, lists legacy values
(`GirlGroup`, `Psychedelic`) and defaults everything else to `_ => 0.40f`. But the table is squeezed
dead from both ends by `decadeLift = SmoothStep(0, .58, eraProgress)`:

| year | decadeLift | album opportunity `w` | Soul affinity | SunshinePop affinity |
|---|---:|---:|---:|---:|
| 1960-63 | 0.000 | .046-.061 | .220 | .294 |
| 1965 | 0.119 | .092-.112 | .348 | .424 |
| 1967 | **0.714** | .375-.391 | .959 | **1.000 (clamped)** |
| 1969 | **1.000** | .533 | 1.000 (clamped) | 1.000 (clamped) |

Early, the genre spread survives but `w` is so small the format normalizer has no leverage. Late,
`w` is large but affinity has saturated to the clamp for every genre. Counterfactual — force Soul
from its explicit `.30` to the `.40` default: the Soul/Sunshine Pop format ratio moves **0.9892x at
1967 and 1.0000x at 1969**. The affinity table contributes ~1% and by 1969 exactly nothing.

**What is real is `GetFormatMultiplier`, and it is `SingleOrientation` alone.** Soul `.80` against
Sunshine Pop `.55`, population-weighted across the authored regions:

| | 1960 | 1965 | 1967 | 1969 |
|---|---:|---:|---:|---:|
| Soul / SunshinePop format multiplier | 1.018x | 1.038x | **1.199x** | **1.318x** |
| full-genre span (PsychedelicRock -> TeenPop) | 1.06x | 1.11x | 1.44x | **1.77x** |

**This term ramps with the decade.** It is ~1.0 for everyone through 1963 and only opens up as `w`
rises, because the normalizer's leverage scales with `w`. Mode A's failure is a 1967-69 failure,
which is exactly when this term is strongest — the timing matches.

#### The week-1 decomposition (1967, per-record, `d7-psychedge`)

Week-1 units, Soul 447.0 against Sunshine Pop 251.6 = **1.777x**:

| factor | Soul | SunshinePop | ratio |
|---|---:|---:|---:|
| `GetFormatMultiplier` (computed) | 1.2551 | 1.0465 | **1.199x** |
| `demandCurve` = quality^4 | .6517 | .6354 | **1.123x** |
| `initialLaunchAwareness` | .2240 | .2007 | **1.117x** |
| `initialLaunchStock` | 62,996 | 62,188 | 1.013x |
| radioHeat (cube-rooted in the discovery mean) | .3836 | .3503 | 1.006x |
| `coveredRegionCount` / `perceivedQualityMultiplier` | — | — | 1.003x |
| **product** | | | **~1.52x of 1.78x** |

#### The quality and awareness terms are ONE cause, not two — and §12's "flat" claim was wrong

§12 asserted quality and career mix are flat across genres. Both parts are wrong, and correcting
them collapses two of the three factors into one.

**Sunshine Pop has no established artists.** `launchCareerState` at release:

| | NewSigning | Rising | Established | Star/Superstar |
|---|---:|---:|---:|---:|
| Soul | 83.0% | 9.5% | 3.9% | 0.8% |
| SunshinePop | **96.1%** | 0.8% | **0.0%** | 0.8% |

Career state drives both quality and launch awareness, pooled across all genres:

| state | n | quality | launch awareness |
|---|---:|---:|---:|
| NewSigning | 4409 | .6412 | .1965 |
| Rising | 234 | .7642 | .3074 |
| Established | 105 | .7919 | .5201 |
| Star | 38 | .8242 | .9417 |
| Superstar | 20 | .8434 | **1.0000** |

Reweighting Soul onto Sunshine Pop's career mix (the well-defined direction — Sunshine Pop has empty
Established and Star cells, so it cannot be reweighted onto Soul's):

| | raw ratio | after controlling for career mix | composition explains |
|---|---:|---:|---:|
| quality | 1.0257x | **1.0058x** | 77% |
| launch awareness | 1.1166x | **0.9962x** | 103% |

Within `NewSigning` alone — 83% of Soul and 96% of Sunshine Pop — quality is 1.0088x and launch
awareness 1.0258x. **Essentially parity.** So the 1.123x and 1.117x are not two genre effects; they
are one artist-population effect counted twice. This is the "chart slot-weeks identity" pattern
repeating: decompose before counting.

**Revised static decomposition of the 1.777x:**

| cause | size | kind |
|---|---:|---|
| `SingleOrientation` via `GetFormatMultiplier` | **1.199x** | authored chart-side lever |
| career-state composition (quality x launch awareness) | **1.254x** | **artist-population, upstream of the chart** |
| residual | 1.182x | unlocated |

**Neither identified cause is a chart bug.** One is an authored lever working as designed; the other
is that Sunshine Pop never accumulates a career artist, which belongs with the §13 stock-and-flow
finding and not with the chart machinery. Label-tier mix *is* flat, as §12 said.

Residual candidates, not yet measured: the actual level (not the gate) of
`GetEnabledSingleDemandMultiplier`, and `GetSegregationFactor`, which is both genre- and
region-scoped and so can tilt *which* regions a genre's covered-region count is spent in.

#### This hands §6.2 two levers

`SingleOrientation` is now measured as the dominant static genre term, and its leverage **grows
through the decade** — 1.14x for Country at 1967, 1.20x at 1969. Country over-charts worst at 1969
(17 slots against a hand-counted 3), so the lever and the failure share a time signature. That makes
`SingleOrientation` the §6.2 lever, and §6.2 better-posed than §6.1.

**One caveat, measured:** the specialist normalizer is *not* a second lever, despite Country being
one of only three genres it touches. The probe suite reports it at **1.0059 for Country** (TexMex
1.0623, Boogaloo 1.1054), so it is worth 0.6% and cannot move Country's chart presence.

### 12.5 LANDED: both stale tables repaired, correctness-only

Built clean; `--genre-market-v2-probes` passes both D5 phases; 52-week run completes.
**Not A/B'd, deliberately** — predicted effect is <1% and structurally zero after 1966, i.e. two
orders of magnitude under §10's noise floor. Do not treat any later run difference as evidence
about these.

1. **`GetGenreMarketReach` now derives from `GenreCatalog.GetBaseline(year)`**
   (`.35 + baseline * .60`, reproducing the old table's authored .35-.95 range). It applies
   `MapLegacy`, so the two entries that could never fire are gone, and reach now moves with the
   decade instead of freezing 1960 assumptions — British Beat was collecting the `.60` default while
   the authored table named it second-highest at `.80`. A genre's reach *is* its demand baseline,
   which the catalog already owns; the switch was a duplicate statement of it.

2. **Format centering no longer carries a genre term** (the option-(b) repair). `GetFormatMultiplier`'s
   contract is to center a genre's tilt "against the accepted era opportunity" — the genre's tilt is
   `SingleOrientation`, and the weight it is conserved against is the *market's* album split. Passing
   `GetAlbumAffinity` there put the genre on both sides of the normalization, using a second,
   independently authored statement of the same fact that **disagreed with the first** (r = 0.88
   against `1 - SingleOrientation`, differing by up to .28 on Folk, Gospel and Country).
   `MarketRegion.GetMarketAlbumOpportunityWeight` is the new genre-blind centering weight, sitting at
   the former `_ => 0.40f` default so the majority of the catalog keeps its level.
   `GetAlbumAffinity` and `GetEnabledAlbumOpportunityWeight` survive unchanged for Album demand
   *sizing*, where genre is legitimate; the accepted/legacy route is untouched.

**The seam this surfaced, worth remembering:** `CompetitorManager.GetFormatPriorMultiplier` documents
itself as sharing "the realized demand tilt seam", so the AI's Album prior had to follow realized
demand onto the genre-blind weight or the two would silently disagree. The probe suite caught exactly
this (`Album AI-prior enabled denominator/centering/tilt parity`), because it had been asserting one
quantity where there are now two. It now probes both: `UntiltedAlbumDemandFactor` against the
genre-scoped **sizing** weight, `FormatTilt` against the genre-blind **centering** weight, plus a new
assertion that the live format route carries no genre term at all.

### 12.6 THE DYNAMIC HALF: it is a launch-week threshold, and `RADIO_POSITION_BONUS_SALES_FLOOR` is a no-op

Measured on the 1967 release cohort (4,965 records whose week 1 falls in 1967, followed through life).
**Note this is a different cohort from §12's**, which included carryover records; it does not reproduce
§12's 5.7x / 76x / 33x figures and is not meant to. On this cohort Soul/Sunshine Pop is 1.777x at
week 1 and 3.13x on total units, so the dynamic factor is 1.76x by ratio-of-means.

#### `RADIO_POSITION_BONUS_SALES_FLOOR` is not a constraint. Kill it as a suspect.

§12.2 predicted a small-genre record "can never pay" the 15,000-unit floor and so never earns the
position bonus that sustains rotation. **The opposite is true, because the gate only applies to a
record that is already charting:**

| band | record-weeks | mean gate | weeks at full gate | median units |
|---|---:|---:|---:|---:|
| top 10 | 520 | **.9985** | **99.2%** | 38,424 |
| 11-40 | 1,560 | .8716 | 50.0% | 15,010 |

By the time a record is top-40 it necessarily sells enough to pay the floor. Bonus lost to the gate
runs 3.6% (EasyListening) to 12.5% (PsychedelicRock) of a bonus worth ~0.14 of `targetHeat` — about
0.012, and **not ordered against the failing genres**. It cannot be a Mode A cause.

#### The real bar is at release, and it is steep

Top-40 attainment against week-1 units, all genres pooled:

| week-1 units | n | reach top 40 |
|---|---:|---:|
| 0-200 | 2744 | 0.0% |
| 200-400 | 1204 | 0.1% |
| 400-600 | 471 | 1.9% |
| **600-800** | 212 | **14.2%** |
| 800-1200 | 145 | 32.4% |
| 1200-2000 | 86 | 44.2% |
| 2000+ | 103 | 68.9% |

**Below 600 week-1 units: 0.23%. Above: 34.1%.** A record that does not launch big essentially never
climbs. This confirms §12's lognormal argument and *localises it to the launch week* — it is not
accumulated sales that must clear an absolute bar, it is the first week.

#### Mode A is two multiplied filters, and only one of them is actionable

**Filter 1 — clear the launch bar.** Share of each genre's records above 600 week-1 units:
Soul 17.8%, FolkRock 19.3%, TraditionalPop 17.1% ... **SunshinePop 3.9%**, Classical 2.9%,
RockAndRoll 2.1%. Soul clears it **4.6x** as often as Sunshine Pop. This is the static half (§12.4)
passing through a threshold: a 1.78x difference in the *mean* of a lognormal becomes a 4.6x
difference in *exceedance* of a fixed bar.

**Filter 2 — convert a big launch into a top-40 record.** Conditional on clearing 600, top-40 rate by
genre: PsychedelicRock 78.6%, FolkRock 63.9%, Soul 52.3%, TraditionalPop 24.6%, TeenPop 8.3%,
BritishBeat 7.7%. A ~10x genre spread, ordered by national genre acceptance — this is
`UpdateRadioHeat`'s `* genreAcceptance` (the only place genre enters the dynamic loop) amplified by
`AIRPLAY_CONVEXITY = 5`. **§11 already established this amplifier is load-bearing and must not be
removed.** Sunshine Pop cannot even be measured here — only 5 of its records clear the bar.

Multiplying: Soul .178 x .523 = 9.3% against an observed 11.3%; Sunshine Pop .039 x ~.20 = 0.8%
against an observed 0.8%.

**Consequence for the arc.** The 2.4x-static x 2.3x-dynamic factorization is not two causes. The
dynamic factor is mostly the static gap re-expressed through the launch-week threshold, plus a
genre-acceptance term that §11 forbids touching. Matched-bin evidence: at equal week-1 units the
genre-attributable amplification is only **1.18x** in the three lowest bins (Soul 83.7/85.1/84.9
against Sunshine Pop 71.2/72.3/70.9), and Soul reweighted onto Sunshine Pop's week-1 distribution
gives 1.24x of the raw 1.42x. **The whole tractable problem is week-1 units**, and §12.4 says those
are set by career-state composition and `SingleOrientation`.

**So Mode A resolves to: Sunshine Pop has no career artists, so its records launch small, so almost
none clear the launch bar, so it scores no year-end slots.** The next move is the artist-population
side (§13's stock-and-flow), not the chart machinery.

## 13. §7 revisited: the psychedelic gate is real but minor

The diagnosis in §7 is correct as far as it goes. `GetAdjacency` had exactly one explicit edge into
`PsychedelicRock` (AcidRock .80); every other identity cleared the `>= .12f` bar only through the
same-family term, and PsychedelicRock's family is Rock. So **only Rock-family identities could ever
record it — 21.1% of 1966-69 supply selections** — which locked out FolkRock, BritishPop and all of
Pop, i.e. most of who actually made the turn. Eight explicit edges were added (FolkRock .55,
GarageRock .58, BritishBeat .52, BritishBlues .45, BritishPop .45, SunshinePop .40, BaroquePop .38,
SurfRock .30), widening the pool to 29.6%.

**But the gate cannot be the 4.7x supply deficit, and the fix is near-inert.** Measured:
1098 -> 1197 releases over 1966-69, **+9.0%**, against a 6.3% median noise floor (§10).

The reason is stock-and-flow, and §7 misses it: **~71% of all supply selections are identity-retained
and never reach the weighted lottery where the gate applies**, and ~70% of PsychedelicRock's own
releases are retained by existing psych artists. The gate can therefore only touch ~30% of psych
supply, predicting **+12%** — which is what was observed. Sizing it off the whole selection
population, as §7 implicitly does, overstates it by more than 3x.

The edges are kept: the case for them is structural and historical, and a measurement that cannot
confirm them cannot refute them either. **They are unvalidated.** The remaining deficit is upstream,
in `GetSupplyWeight`'s `artistFit`/`labelFit` terms and the concentration brakes — §7 step 4 already
points at the brakes and is still the right next move.

## 14. `GenreMarketMomentumService` IS DEAD CODE — the adjacency/donor market never ran

This is the largest structural finding of the session and it is a separate arc.

The file implements exactly the mechanism the author remembered: `DistributeAdjacency` spreads a
hit's impulse to adjacent genres, `ChargeDonors` charges competitors by segment overlap weighted by
`(1 - adjacency)`, and Classical/Comedy/Childrens/Gospel are explicitly excluded from being charged.

**None of it runs.** `AdvanceWeek`, `DrainEvents`, `GetShock`, `GetZeitgeistFactor`,
`GetEmergenceAdvanceWeeks` and `SnapshotStates` have **zero callers anywhere in the codebase**. The
only surviving consumer of the whole file is `GetAdjacency`, used as a pure lookup by
`ArtistManager.cs:894` and the psychedelic gate at `GenreSupplyService.cs:139`.
`ChartAuditRunner.cs:989` creates the `genre-events.csv` writer and it emits **header-only** — 105
bytes on the non-lean `d7-drop` run, 0 rows on `genretune2`. That is the tell, and it is why
`genre-market-weekly.csv` carries `preShock`/`adjacentImpulse`/`donorPressure` columns that are
always zero.

It is dead at **both** ends: even if `AdvanceWeek` were called weekly, the `Shock` state it
accumulates feeds nothing, because the live acceptance path reads `ChartManager.GetGenreMomentum` —
the legacy accumulator, which has no adjacency structure at all. Wiring it means connecting both
ends and will re-open the market-share calibration, so it is its own pass with its own decade run.

One bug to fix when wiring it: `GetAdjacency` returns exactly `.12f` for same-family pairs, but
`DistributeAdjacency` filters on `Weight > .12f` — **strictly greater**. Family-only adjacency would
therefore never distribute even once the engine is live; only the explicit edges would.

## 15. Probe-suite repairs (pre-existing failures on HEAD)

`--genre-market-v2-probes` was **failing on HEAD before any change this session**. The genretune2
commit (42f7a1b) raised Country's 1968 baseline .54 -> .64 and Jazz's 1964 baseline .28 -> .34 to
serve the market benchmark, and landed without the probe suite passing. Three consequences, all
fixed:

1. A fixture hardcoded supply weights `.316/.525/.2875`; Jazz's is now `.05 + .95 x .34 = .373`.
   Re-anchored to `.05f + .95f * GetProspectiveSupplyAcceptanceForProbe(...)`, with an explicit
   lifecycle-plateau assertion so the formula stays complete.
2. A fixture probed the Country/TexMex selection boundary at rolls `.64`/`.65`. The boundary moved to
   `.666`, so **both literals now land on Country** and the fixture had silently inverted.
   Re-anchored to `countryWeight / (countryWeight + texMexWeight)` and probed either side of it.
3. Country's centered-texture conservation loss grew to .010765 against a 1% tolerance, because a
   higher baseline pushes the southwest route further into the 1.0 acceptance cap. Tolerance widened
   to 2% with the cause documented — it bounds an acknowledged clamp loss, not a conservation
   guarantee.

This is the "probe fixtures must be relational" lesson repeating: every one of these hardcoded an
absolute that was correct when written and became a false failure when the catalog moved.

## 16. Recommended order from here

1. **Add the seed requirement to the ladder** (§10) before trusting any further small result.
2. ~~`GetGenreMarketReach`~~ — **killed (§12.3) and repaired (§12.5).** Both stale tables are done.
3. **The static half is decomposed, and it contains no chart bug — see §12.4.** It is
   `SingleOrientation` 1.20x (an authored lever) x career-state composition 1.25x (an
   artist-population effect — Sunshine Pop has zero Established artists), with ~1.18x unlocated.
   Four suspects killed along the way. Residual candidates:
   `GetEnabledSingleDemandMultiplier`'s level and `GetSegregationFactor`.
4. ~~The discovery loop~~ — **done, §12.6.** `RADIO_POSITION_BONUS_SALES_FLOOR` is a no-op; the real
   bar is at ~600 week-1 units and the dynamic half is mostly the static half passing through it.
5. ~~**THE OPEN FRONT: why Sunshine Pop never grows a career artist.**~~ — **diagnosed and addressed,
   see §17.** It was never a Sunshine Pop problem: no genre grows career artists, because the ladder's
   first rung is a 1.87%-per-release event offered twice.
6. **Wire the momentum engine** (§14) — its own pass, expects to re-open the market calibration.
7. §6.2 adult genres via `SingleOrientation` (§12.4), then the three new genres, unchanged from §8.

---

# Session of August 4, 2026 (part 2) — the career ladder

## 17. THE LADDER'S FIRST RUNG IS A 1.87% COIN FLIP OFFERED TWICE

§16 item 5 framed this as "why Sunshine Pop never grows a career artist." **The framing was wrong
and the defect is population-wide.** Measured on `d7-psychedge`, per-release outcome by the career
state the record launched from:

| launch state | releases | top 40 | top 10 | flop (peak 0 or >60) |
|---|---:|---:|---:|---:|
| **NewSigning** | 40,222 | **1.87%** | 0.34% | **96.46%** |
| Rising | 2,141 | 28.1% | 8.1% | 58.0% |
| Established | 744 | 40.9% | 14.5% | 46.9% |
| Star | 413 | 62.5% | 32.5% | 22.8% |

`UpdateCareerState` gated `NewSigning -> Rising` on `contractTop40Hits >= 1`, and
`ShouldDepartForCurrentContractPerformance` demanded that hit inside `FirstContractFlopThreshold = 2`
completed runs. **P(escape) = 3.7%.** The population behaved accordingly:

- **9,601 of 10,655** performance departures were `FirstContractProbation`; 10,116 at
  `contractSequence == 1`; 9,357 at exactly 2 completed runs with exactly 2 flops.
- Mean contracts per ever-signed artist **1.38**; 8,713 of 12,310 got exactly one.
- Best career state ever reached, ever-signed artists: **0.51% Rising, 0.09% Established, 0.00% Star.**
  Runtime-formed acts 0.22% / 0.01% / 0. **No runtime artist reached Star in a decade.**
- Counting only `careerState` undercounts this ~4x; you must union it with `careerStateBeforeDrop`
  and `contractEntryCareerState`, because a promoted act emits no event until it is dropped.

So the model has a rich career-advantage curve — Established converts 22x better than NewSigning —
and hands it almost exclusively to the seeded 1960 population. Sunshine Pop is that mechanism at its
worst end (0.28% NewSigning top-40 rate), not a special case; AcidRock, BaroquePop and BluesRock are
at 0.00% and are structurally sterile.

**Why loosening the drop rule alone could not have worked:** every rung was a national chart outcome,
and §12.6 established the chart is an absolute units bar applied to a genre-scaled distribution.
Surviving longer without a reachable rung just means flopping longer.

### 17.1 The regional rung, and why it is the right quantity

`regionalBreakoutCount >= 1` fires on **11.9%** of NewSigning releases against 1.87% for Top 40, and
it compresses the genre spread without erasing it:

| | Soul | SunshinePop | ratio |
|---|---:|---:|---:|
| top-40 rate, NewSigning releases | 3.15% | 0.25% | **12.6x** |
| regional breakout rate | 19.07% | 7.89% | **2.4x** |

Genre-level correlation between the two is **r = 0.698**, so the rung still ranks genres correctly —
it is not genre-blind noise, and it leaves Soul ahead. That is the §6 Soul control satisfied by
construction. It is also the historically right shape: the psychedelic, folk-rock and sunshine-pop
turns were carried to national notice by regional records.

### 17.2 What landed

Bundled deliberately — the ladder and the drop rule are one mechanism, and either alone is predicted
near-inert (see above). The label-buzz repair (§17.4) is held back as a separate change.

1. **`SimulatedArtist` gains contract-scoped evidence counters** — `contractChartedRecords`,
   `contractRegionalBreakouts`, and lifetime `regionalBreakouts`, fed from
   `RecordRuntimeData.regionalBreakoutCount` through a new optional argument on `CompleteChartRun` /
   `UpdateAfterChartRun`. All are reset by the free-agent cycle alongside the existing contract
   counters; `regionalBreakouts` is lifetime and is not.

2. **A regional rung on the ladder.** `HasBreakthroughEvidence()` promotes NewSigning -> Rising on
   `contractTop40Hits >= 1` **OR** `contractRegionalBreakouts >= RegionalBreakoutPromotionThreshold (2)`.
   `Rising -> Established` also accepts `top40Hits >= 3` alongside the authored `top10Hits >= 2`, so a
   consistent charting act can establish without a Top 10.

3. **The probation window is 4 releases and renewable, not 2 and terminal.**
   `FirstContractFlopThreshold` 2 -> 4, `ExperiencedComebackFlopThreshold` 3 -> 5 (preserving the
   authored ordering that a comeback gets more rope than a first contract). **`contractConsecutiveFlops`
   now resets on commercial evidence** — any chart entry, any regional breakout, or a Top 40 — which it
   previously never did, unlike its lifetime counterpart. So evidence restarts the window rather than
   granting permanent immunity: one regional hit buys another four sides.

4. **Recoupment is a standing exemption.** `HasRecoupedCurrentContract()` — `unrecoupedAdvance <= 0f`
   **and** `totalRoyaltyEarnings > 0f` — blocks a performance departure outright. Both clauses are
   load-bearing: the balance is per-contract (reset at each signing, charged per production) but reads
   0 before it is ever charged, so the earnings clause is what prevents an unset field from reading as
   profitability.

5. **`RosterManager.ShouldResignArtist` reads the same evidence.** It previously refused renewal to any
   NewSigning act with 2+ releases and no Top 40, which would have re-closed at expiry exactly the route
   the window opens.

6. **Telemetry**: `artist-population-events.csv` gains `contractChartedRecords`,
   `contractRegionalBreakouts`, `regionalBreakouts`, `top40Hits`.

### 17.3 DECADE RESULT: the ladder opened, chart health improved, and Mode A got WORSE

Run `d7-career-ladder-decade-522-1001`, seed 1001, clean, against `d7-genretune2`.

**The ladder did open, and chart health held or improved.**

| | genretune2 | **career-ladder** | band |
|---|---:|---:|---|
| ever-signed artists reaching Rising | 0.50% | **2.80%** | — |
| ... runtime-formed only | 0.18% | **1.98%** | — |
| ever reaching Established | 0.07% | **0.22%** | — |
| ever reaching Star | 0.00% | **0.00%** | still zero |
| distinct #1s | 170 | 172 | ~203 |
| mean #1 tenure | 2.76 | 2.77 | 2.57 |
| **mean chart life** | 8.20 | **7.97** | 7.48 — improved |
| breadth | 455 | 449 | 400-600 PASS |
| MidTier 1969 | 32 | 35 | 25-40 PASS |
| **owner-Major 1969** | 45.8 | **44.8** | 45-52 **back to FAIL** |
| year-end slot error | 783 | 793 | noise |
| market-share error | 145.5 | 159.4 | +13.9, under the §10 floor |

**Breadth was the wrong band to worry about — it held. The regression is in signing *composition*.**

| | first-time signings, ref | new | change |
|---|---:|---:|---:|
| **emergent genres (1964+)** | 2,552 | **945** | **−63.0%** |
| established genres (1960) | 8,268 | 6,379 | −22.8% |

The ordering is almost exactly emergence order: BaroquePop −74%, SunshinePop −73%, FolkRock −66%,
BritishPop −66%, PsychedelicRock −65% ... against RockAndRoll −7%, Folk −10%, RnB −12%, TeenPop −17%.

**The mechanism, and it is the point of this section.** Exits by channel:

| | ref | new |
|---|---:|---:|
| Performance drop | 10,554 | 6,401 |
| PerformanceExhaustion | 2,898 | 700 |
| LabelClosure | 2,896 | 2,991 |
| **ContractExpired** | **248** | **618** |

**Roster turnover in this model is ~90% firing.** Contract expiry is 1.5% of exits because
`AILabel.CalculateContractLength` gives a new signing `RandRange(4, 7)` years, which almost never
matures inside a decade. A vacancy is the only door onto a roster, so cutting the firing rate by 39%
cut the doors by the same amount — and **an emergent genre has no incumbents, so 100% of its roster
presence must come through that door.** Established genres hold their seats and simply keep releasing
(successful releases rose 95,662 -> 101,650 from 29% fewer signed artists).

Cutting the firing rate without opening another exit is therefore a **regressive tax that falls almost
entirely on emergent genres** — the exact population Mode A is about. The corresponding year-end result:
SunshinePop scores 0 slots in 1967-69 in both runs, and PsychedelicRock's 1969 count fell 6 -> 2 against
a hand count of 6.

**Do not revert this.** The 2-flop rule is indefensible on its own terms (§17) and the ladder repair
carries real gains — chart life moved toward band, and the runtime population finally produces career
artists at 11x its old rate. The defect this exposed is upstream and was previously invisible:
**the model has only one working turnover channel, and it is punitive.**

### 17.3.1 The next move: open a non-punitive turnover channel

Ranked, cheapest first:

1. **Shorten first contracts.** `CalculateContractLength`'s `_ => RandRange(4, 7)` years is
   ahistorical for a new act — the 1960s norm was a 1-2 year deal with options. Shortening it restores
   turnover through expiry rather than through firing, which is exactly the distinction the author drew
   ("judge the act's financial contribution", not "keep them forever"). This is the one-variable change
   that most directly repays the −63%.
2. **Roster capacity.** `OperatingRosterTarget` is occupancy-gated by `HasDailyVacancy`; a longer tenure
   consumes a seat that a shorter contract would have recycled.
3. **Label formation.** `maxMonthlyBirths = 6` is acknowledged in-code as a duct-tape cap that flattens
   the mid-60s micro-label explosion — new labels are the other way an emergent genre gets signed.

**Sizing rule established here: any change to a career-length or drop rule must be sized against
first-time signings by genre-emergence cohort, not against breadth.** Breadth counts firms that chart
and moved −6 on a change that cut emergent-genre signings by 63%. It is not a sensitive instrument for
this class of change.

## 18. LANDED: the contract term, and the turnover channel it opens

`d7-contractterm-decade-522-1001`, seed 1001, clean. Built on top of §17, so read the two together.

**What changed.** `CalculateContractLength` kept the authored leverage ordering (bigger act, shorter
deal) and compressed only the new-signing end, which was the ahistorical part: NewSigning 4-7 -> 1-2,
Rising 4-7 -> 2-3, everything above unchanged. `GetContractTermBias` adds house variance — Major/MidTier
+1, `artistLoyalty > .65` +1, Small/Boutique −1, clamped 1-7 — so a loyal major writes 3-4 years to a
new act where a small independent writes 1, and 5-7 years survives as an established act at a label
that locks its roster down. `CalculateContractSinglesObligation` gives a new act signed through 1966 a
3-6 side delivery commitment (Rising 4-8); established acts and everything after 1966 are pure term
deals as the album era takes over. `IsContractMatured` ends the deal on **whichever matures first**.

Two corrections the shorter terms forced, both of which would have silently wrecked the result:
- **Expiry is week-based.** `contractExpiresYear <= year` checked at a monthly review was tolerable at
  4-7 years and turns a one-year deal signed in November into a two-month one. `contractExpiresWeek`
  is authoritative where recorded; the year field survives for display and legacy rows.
- **The seeded 1960 roster no longer expires as a cliff.** It was dated `year + length` while ignoring
  the already-backdated `signedYear`, so every launch act matured 3-6 years from 1960 together.

`ShouldResignArtist`'s refusal bar moved to **2 sides delivered under this deal** (from 4 lifetime
releases). Declining an option is not a firing: it exits as `ContractExpired`, carries no cooldown and
no exhaustion charge, and returns the act to the market intact.

### 18.1 The turnover channel inverted, which was the point

| exit channel | genretune2 | career-ladder | **contract-term** |
|---|---:|---:|---:|
| Performance drop | 10,554 | 6,401 | **1,945** |
| PerformanceExhaustion | 2,898 | 700 | **183** |
| LabelClosure | 2,896 | 2,991 | 2,746 |
| **ContractExpired** | **248** | 618 | **11,284** |
| **total exits** | 16,611 | 10,748 | **16,187** |

Total turnover is restored to baseline while its composition inverted from ~80% firing to ~70% expiry.
`PerformanceExhaustion` — the second-drop event that ends a career outright — fell **2,898 -> 183**, so
careers now end by non-renewal rather than being destroyed. Realised mean term is **1.84 years** and
mean sides obligation **3.58**, both where they were aimed.

### 18.2 Chart health is the best of the three runs

| | genretune2 | career-ladder | **contract-term** | band |
|---|---:|---:|---:|---|
| distinct #1s | 170 | 172 | **176** | ~203 |
| mean #1 tenure | 2.76 | 2.77 | **2.69** | 2.57 |
| mean chart life | 8.20 | 7.97 | **7.94** | 7.48 |
| breadth | 455 | 449 | **470** | 400-600 PASS |
| MidTier 1969 | 32 | 35 | 27 | 25-40 PASS, near floor |
| owner-Major 1968 | 44.3 | 44.2 | **40.4** | 45-52 FAIL, worsened |
| owner-Major 1969 | 45.8 | 44.8 | **45.3** | 45-52 PASS, recovered |
| ever-signed reaching Rising | 0.50% | 2.80% | **3.56%** | — |
| ...runtime-formed only | 0.18% | 1.98% | **2.43%** | — |
| ever reaching Star | 0.00% | 0.00% | **0.00%** | still zero |

Every headline chart metric moved toward its target, and the §17 ladder gains not only survived the
extra turnover but improved. **Two soft spots:** MidTier 1969 fell 32 -> 27, still in band but near the
floor, and owner-Major 1968 fell 44.3 -> 40.4 on a band that was already failing.

Emergent-genre first-time signings recovered from **−63.0% to −42.4%** against the genretune2 baseline
(established genres −14.1%). Total contract starts are essentially restored — 17,379 -> 17,040 — but the
mix shifted toward re-signings (6,559 -> 8,467) and away from new signings (10,820 -> 8,573), because a
matured term is frequently renewed in place rather than freeing the seat.

### 18.3 MODE A DID NOT MOVE, and §12.4 predicted that

**Sunshine Pop still scores 0 year-end slots in every year of 1966-69** against a hand count of 4/10/4/12,
in all three runs. This is the honest headline: two correct mechanism repairs, and the failure they were
reached for is untouched.

§12.4's own decomposition already said so. The static gap is 1.777x = `SingleOrientation` 1.199x x
career-state composition 1.254x x residual 1.182x. **Perfectly equalising career composition still leaves
1.42x**, and §12.6 established the launch bar is a threshold, so 1.42x of the mean still produces a large
exceedance gap. The ladder was never sufficient on its own.

**CORRECTED — see `D7EmergentGenreFormationHandoff.md` §2.** This section originally cited Sunshine
Pop's 89 signings a decade as a standing supply constraint and named supply as an independent lever.
That was circular: 89 is down from 140 at baseline and the fall is caused by §17-18 themselves. The
regression is this arc's to repay, not a new lever to turn. What survives is the measured fact that at
the **baseline** 140 signings Sunshine Pop still scored 0 slots and had 0 artists ever reach Rising, so
recovering the formation deficit is necessary but is not shown to be sufficient.

Movement elsewhere against the hand count, worth keeping:
- **Country's §6.2 over-charting materially improved**: 1966 11 -> 7, 1967 9 -> 6, 1968 15 -> 8 (targets 2/2/1).
- **TeenPop's early decade improved**: 1960 7 -> 18 (target 28), 1961 12 -> 28 (target 18).
- **Soul over-charts worse**: 1968 36 -> 42 against a target of 28. The §6 control is satisfied — nothing
  flattened Soul — but the largest divergence grew.
- Year-end slot error 783 -> 803 and market-share error 145.5 -> 169.3, both inside §10's noise floor and
  therefore **not results**; they need a seed sweep before they mean anything.

### 18.4 Where the remaining Mode A leverage is

1. **`SingleOrientation`** — measured at 1.199x of the static gap and the largest identified term, an
   authored lever, and §12.4 shows its leverage grows through the decade exactly when Mode A fails.
2. **The 1.182x residual** — never located. Candidates named in §12.4: the level (not the gate) of
   `GetEnabledSingleDemandMultiplier`, and `GetSegregationFactor`.
3. **Repay the emergent-genre formation deficit FIRST.** −42.4% against baseline is this arc's own
   regression and blocks everything else — see `D7EmergentGenreFormationHandoff.md`. Those artists are
   signed at 100%, so the constraint is formation, not signing, and the documented formation servo is
   provably inert (`unmetShare` == 0 in all 1,566 measured run-weeks).

### 17.4 Held back: label buzz in the artist-choice utility

Confirmed as a real and adequately sized channel, not yet built. `CalculateArtistChoiceUtility`
(`RosterManager.cs:589`) decides **3,176 of 17,019 signings (18.7%)** — 52.6% of nominations are
contested, with collisions up to 61 labels on one artist. It carries no chart-momentum term at all,
and its second-heaviest weight (`rosterOpportunity`, .18) **rewards an empty roster**, so an artist
actively prefers the label that cannot fill its seats.

**Do not implement this with `momentumScore`,** which is what `D7LabelChartAccessSystemicRepairHandoff.md`
§28.5 suggests. It is `reputation + 0.05 x lifetime top40Hits`, and `D7LabelChartAccessLoopHandoff.md`
§216 measured `momentumScore > 0.60` firing **7 times in a decade** — it is a dud measure and the term
would be inert. Build the buzz term from recent chart presence over a trailing window, normalized
against the live label field.
