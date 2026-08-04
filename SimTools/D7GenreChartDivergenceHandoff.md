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
