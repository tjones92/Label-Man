# D7 soundtrack / cast-album implementation — design handoff

Opened August 6, 2026. Branch `d7-genre-decade-calibration`. Parent: `D7SimRuntimeOptimizationHandoff.md`.
This is a **design + implementation** handoff. Nothing here is coded yet. It exists because the album
chart cannot be balanced until soundtracks/cast albums are a real object — they are 7–26% of the
year-end album chart historically and 0% in the model.

## 0. One-line brief
Turn the already-declared-but-cosmetic `AlbumFormat.Soundtrack` into a real subsystem: externally
originated album releases (film soundtracks, stage-cast albums, tie-in singles) with their own
origination pipeline, a box-office-driven demand curve, a demographic-segment bypass, and brutal
licensing economics — plus the calibration fix they force (normal albums must decay faster so
soundtracks can stand out). Design adapts a Gemini sketch (in `SimTools/` notes / below) to the
actual engine.

## 1. WHY (the evidence — read first)
The album-chart genre makeup is wrong, and the single biggest reason is a whole category is missing.
From `decade31` (the first full-telemetry decade run carrying `album-chart.csv`) vs the author's
year-end album estimates (`albumchart.odt`, "shape only, not objective fact"):

| album-chart share | 1960 | 1963 | 1965 | 1967 | 1969 | verdict |
|---|--:|--:|--:|--:|--:|---|
| **Soundtrack/Cast** est | ~13 | ~14 | ~14 | ~11 | ~6 | **model 0 every year** — the headline gap |
| Jazz — model | 42 | 19 | 13 | 10 | 5 | model **wildly over** (est ~0–4); Jazz is a small album presence |
| Soul — model | 0 | 0 | 0 | 3 | 2 | model **far under** (est rises 0→22 mid-late; Motown/Stax LPs) |
| EasyListening — model | 7 | 15 | 15 | 15 | 8 | model **under late** (est 21–28 in 67–68, Alpert/Tijuana era) |
| Country — model | 9 | 13 | 18 | 15 | 22 | model **over** most of decade (est ~0–4 until 69) |
| Comedy — model | 1 | 0 | 0 | 0 | 0 | model **under** (est 3–13 early; Newhart/Cosby boom) |
| British — model | 0 | 0 | 7 | 4 | 5 | model **under** mid-60s (est 14–17; Beatles LPs) |
| TradPop — model | 14 | 24 | 29 | 17 | 13 | roughly tracks (est 36→7) |
| Psych — model | 0 | 0 | 0 | 15 | 20 | roughly right shape, slightly early |

Two of these (Jazz-over, Soul-under) are album-genre-balance problems for a later pass. But
**Soundtrack is a structural hole**: ~1 in 7 top albums should be a soundtrack/cast album, and the
model has none as a distinct object. You cannot calibrate the rest of the album chart around a 14%
category that does not exist.

### The chart-life problem soundtracks expose
`decade31` album chart life is **uniform and too long**: mean 42 wk, median 41, and only **23% of
albums live <18 wk** while **52% live 40+ wk**. The author's shape is the inverse: most non-soundtrack
albums run **12–18 wk**, and it is the **soundtracks/cast albums** that run **40–60 wk (some multi-year:
Sound of Music, West Side Story, Button-Down Mind reappear across years)**. So the model today gives
*every* album the long tail that historically belonged only to soundtracks and a few evergreens.
Implementing soundtracks therefore has a twin requirement: **tighten normal-album catalog decay**
(so the median lands ~15 wk) **and** give soundtracks the long box-office tail. Do both or the chart-life
distribution stays wrong. Breadth target: **350–500 unique labels** charting albums across the decade.

## 2. WHAT ALREADY EXISTS (do not rebuild — wire these)
The engine already has most of the scaffolding; this is mostly wiring + one new origination pipeline.

- **`AlbumFormat` enum** (`Data/Album.cs:29`): `{ Standard, Compilation, Concept, Live, Soundtrack, EP }`.
  `Soundtrack` is **already a value** and is **already assigned** at `CompetitorManager.cs:2899`
  (`typeRoll < 0.12f ? AlbumFormat.Soundtrack : ...`). But it is **cosmetic**: assigned at random to
  a normal artist's album, carries the artist's genre, and has **no distinct demand, economics, routing,
  or lifecycle**. That random roll should be **removed** — soundtracks must originate externally, not as
  a dice roll on an ordinary album.
- **`AudienceSegment`** (`Data/AudienceSegment.cs:5`): includes exactly the segments the design needs —
  `AdultMOR`, `FamilyChildrens`, `JazzHiFiClassical`, `Youth`, `MainstreamAM`, etc. — with a working
  `SegmentCapacityModel` and segment-weighted acceptance in `GenreAcceptanceService`. The "genre-blind
  demographic bypass" routes through this, not a new system.
- **`GenreTag`** (`Data/Genre.cs:83`): has `Instrumental`, `Orchestral`, `Novelty`, `Christmas`, etc.
  — the Directive-5 tags a soundtrack would carry for cost/seasonality.
- **`AlbumFormat.Compilation`** is a **fully-wired subtype** (`AlbumModel.GetCompilationChance`,
  cost/multiplier branches at `CompetitorManager.cs:2497/2903/2912`, decline curve). **Use it as the
  structural template** for how a subtype threads creation → cost → demand → chart.
- **Tie-in single linkage already exists**: `AlbumSimulator.UpdateAlbum` reads `record.linkedPromoSingleId`
  → `linkedPromoSingleHeat`, and the album's awareness already responds to a linked single's radio heat.
  Gemini's "Tie-In Single symbiosis" is **partly built** — reuse this field.
- **Economics primitives exist**: `marginSkim` / `royaltyRate` / `ownedReach` / `GetProductionCost`
  (see `CalculateAlbumPriorNet`, `CompetitorManager.cs:2403`). A licensing deal is a very high skim +
  high upfront production cost within this same margin math — no new economic engine needed.
- **Album demand curve** lives in `AlbumSimulator.CalculateRegionalSales` with catalog decay constants
  `CatalogDecayStartWeeks = 26`, `CatalogWeeklyDecay = 0.985` (`AlbumSimulator.cs:14-15`). This is the
  hook where the box-office trajectory replaces standard decay for soundtracks and where normal decay
  gets tightened.

## 3. DESIGN — adapted from the Gemini sketch, fitted to the engine
Gemini's five-part sketch is sound. Adjustments so it fits this codebase (and does not over-scope):

### 3.1 One object, a source-type flag — NOT new genres or new enum values
Keep `AlbumFormat.Soundtrack` as the single album subtype and chart-reporting category for **both** film
soundtracks and stage-cast albums. Do **not** add a `CastAlbum` enum value — the film/cast difference is
entirely in the **demand params** (below), so carry it on a new `ExternalMediaProfile.SourceType`
`{ FilmScore, FilmSong, StageCast }`. Soundtracks are a *vessel for existing genres*: the record still
maps to a real `Genre` (StageCast→`TraditionalPop`/`Comedy`; FilmScore→`Classical`/`EasyListening`;
beach/rock films→`RockAndRoll`/`SurfRock`), so it appears under that genre in `genre-decade-shape` AND
under Soundtrack in the album chart. Report BOTH so the 14% category is legible.

### 3.2 Origination — the one genuinely new subsystem (`ExternalMediaService`)
Album creation today is artist-driven inside `CompetitorManager`'s format fork. Soundtracks must NOT go
through that fork (that is the cosmetic-roll bug). Instead add an **abstract external-media engine** that,
a few times per year, emits **RFP opportunities** to labels ranked by reputation + capital + roster:
- **Original Cast/Score license** — already written/cast; label just presses & distributes. High upfront
  license, massive ceiling, thin margin, minimal creative control.
- **Artist Vehicle** — a studio wants one of the label's established artists to score/star; pauses that
  artist's normal album output (cannibalization) and risks a momentum/reputation hit on a flop.
- **Tie-in Single commission** — a `Single` (not an LP) with a guaranteed radio push; wire through the
  existing `linkedPromoSingleId`.

Each accepted RFP rolls an **immutable `ExternalMediaProfile`** with **correlated** stats (high
`CriticalPrestige` ⇒ lower `YouthAppeal`, so blockbusters are not also mass-youth hits):
`SourcePopularity` (launch awareness), `CastStarDraw` (initial momentum), `StudioPromotion` (multiplies
the label's own marketing), `BoxOfficeTrajectory` (**the demand-curve shape**), `AwardsPrestige`
(Q1-next-year resurrection spike). Generation is **capped to 0–3 genuine blockbuster profiles per decade**;
the vast majority are mid-tier B-movies / forgotten stage flops / modest instrumental scores.

### 3.3 Demand — box-office curve replaces catalog decay (and fixes §1's chart-life)
In `AlbumSimulator.CalculateRegionalSales`, branch on `AlbumFormat.Soundtrack`:
- **FilmScore/FilmSong**: awareness anchored to an abstract premiere date; demand follows
  `BoxOfficeTrajectory` (flop dies ~3 wk; sleeper slow-burns; blockbuster sustains a high multiplier for
  40–60+ wk). Replaces the flat `CatalogWeeklyDecay`.
- **StageCast**: very low initial peak, **absurdly slow** decay — can hover near #40 for 2–3 years
  (tourist catalog buying). This is the multi-year-run case.
- **Normal albums (Standard/Concept/Live)**: **tighten** decay so the median lands ~12–18 wk (today it is
  ~41). Candidate: raise the decay rate and/or start it earlier than week 26. Verify the album-chart-life
  histogram in §1 inverts (most albums <18 wk, a thin 40+ wk tail).

### 3.4 Demographic bypass — via the existing segments
A soundtrack overrides normal genre→segment routing to concentrate in `AdultMOR` + `FamilyChildrens`
(+`JazzHiFiClassical` for scores), so a Sound-of-Music equivalent sells millions to families/adults
**without** dominating `Youth`/`MainstreamAM` radio. Implement as a segment-reach override on the
soundtrack record inside the `GenreAcceptanceService` segment path — do not touch the genre's own
acceptance.

### 3.5 Economics & guardrails — reuse the margin math
- **Licensing fee**: blockbuster cast/score deals take **60–80% skim** (studio's cut) → huge gross &
  prestige, thin **net**. Set via `marginSkim` on the deal.
- **High upfront capital**: a large advance in `productionCost` gates Small/Boutique labels out of the
  blockbuster tier (they can still catch an indie sleeper).
- **Cannibalization**: an Artist-Vehicle pauses the artist's normal album pipeline; a flop
  (`BoxOfficeTrajectory` terrible) applies a momentum/reputation penalty.
- **Rarity**: the 0–3 blockbusters/decade cap in the generator is the primary anti-monoculture guard.

## 4. CALIBRATION TARGETS (from the author, shape-only)
- Soundtrack/cast share of year-end album chart: **~7–14%** (author estimate spikes to ~25% in 62–63).
- Soundtrack/cast chart run: **40–60 wk**, blockbusters multi-year (Sound of Music, West Side Story).
- Normal album chart run: **12–18 wk** (some evergreens multi-year, e.g. Button-Down Mind).
- Album breadth: **350–500 unique labels** charting across the decade.
- Blockbusters: **0–3 per decade.**
- Genre mapping of soundtracks: StageCast→TradPop/Comedy; FilmScore→Classical/EasyListening;
  beach/rock films→RockAndRoll/SurfRock/FolkRock.

## 5. SUGGESTED IMPLEMENTATION PHASES
1. **Remove the cosmetic roll** (`CompetitorManager.cs:2899`) and add `ExternalMediaProfile` +
   `AlbumFormat.Soundtrack` chart-reporting (a `soundtrack-chart.csv` / a genre-shape Soundtrack row).
   Baseline: confirm the model is at 0% and normal album life is ~42 wk.
2. **Tighten normal-album decay** to a ~15 wk median; re-check the §1 histogram. (Do this first — it is
   the biggest single chart-life fix and is independent of origination.)
3. **`ExternalMediaService` origination + RFP acceptance** (start Original-Cast license only; simplest).
4. **Box-office demand branch** in `AlbumSimulator` for Soundtrack; StageCast long-tail.
5. **Segment bypass** + **licensing economics** + **cannibalization/rarity guardrails**.
6. **Calibrate** to §4 across 2–3 seeds (album-genre deltas churn ~tens of slots on one seed — see
   `D7SimRuntimeOptimizationHandoff` genre-noise note; never tune album genres off a single seed).

## 6. WATCH-OUTS
- **Do not create a Soundtrack genre.** It is an `AlbumFormat`; the record keeps a real genre.
- **Do not route soundtracks through the artist-album format fork** — that is the current cosmetic bug.
- **Singles↔albums are coupled** (the same clearing + the tie-in single). Expect the singles chart to
  move when soundtracks land; validate both charts, and on 2–3 seeds (single-seed genre slots churn).
- **The 42 wk uniform album life must be fixed alongside** or soundtracks won't be distinguishable.
- Run full telemetry (NOT `--calibration`) for album work — `album-chart.csv` is suppressed under
  `--calibration` (that is why earlier decade runs carried no album genre data).

## 7. HOW TO RUN / SCORE (unchanged)
- Godot: `/c/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe`
- Python (pandas): `/c/Users/grohl/AppData/Local/Programs/Python/Python314/python.exe`
- Build: `dotnet build "Label Man.sln" -v minimal`
- Full-telemetry decade (needed for `album-chart.csv`): `--weeks=522 --seed=1001 --enable-genre-market-v2
  --enable-artist-population-lifecycle --profile-performance` (NO `--calibration`).
- Album genre scoring: `<run>-album-chart.csv` (per-week album chart: genre, albumFormat, weeksOnChart,
  units); compare to `albumchart.odt` year-end estimates (shape only). Chart life = max `weeksOnChart`
  per `recordId`.
