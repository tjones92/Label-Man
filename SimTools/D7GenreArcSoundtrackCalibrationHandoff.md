# D7 genre-arc + soundtrack calibration — handoff

Opened 2026-08-06. Branch `d7-genre-decade-calibration`. Parent: `D7SoundtrackCastAlbumHandoff.md`
(the soundtrack design doc — now IMPLEMENTED, see §1). This handoff hands off the **remaining genre
calibration** after the soundtrack subsystem and the first genre-arc pass landed.

Latest decade run scored throughout: **`d7-genrearc-decade-522-1001`** (single seed; full telemetry).
Single-seed slot counts churn ~tens of slots — treat singles slot numbers as directional and confirm
big moves on a second seed before locking.

## 0. What landed this session (all built + decade-validated on seed 1001)

**Soundtrack subsystem (the `D7SoundtrackCastAlbumHandoff.md` design, phases 1-6):**
- Removed the cosmetic `AlbumFormat.Soundtrack` roll (`CompetitorManager.GenerateAlbum`).
- Normal-album catalog decay tightened → median chart life **41wk → 12wk** (in the 12-18 band).
  `AlbumSimulator.CatalogWeeklyDecay = 0.93`, `CatalogDecayStartWeeks = 8`.
- New `ExternalMediaService` (`Systems/`) + `ExternalMediaProfile` (`Data/`): externally-originated
  soundtracks (FilmScore/FilmSong/StageCast), minted weekly in `CompetitorManager.MintSoundtrackRecord`,
  released via `ChartManager.ReleaseRecord`. Origination **22/yr**; blockbuster cap **0-3/decade**.
- Box-office demand trajectory (`AlbumSimulator.GetSoundtrackBoxOfficeMultiplier`): median class life
  ~50wk; **blockbuster ultra-tail** (`~0.994-0.9975/wk`) → a **216-week** run appeared on seed 1001
  (target: West Side Story 341wk, Sound of Music 233wk, Camelot 265wk).
- Demographic segment bypass (`MarketRegion.GetSoundtrackAlbumMarketSize`): soundtracks draw an
  AdultMOR + FamilyChildrens (+JazzHiFiClassical for scores, +Youth for FilmSong) buyer pool, so a
  small-genre soundtrack still charts.
- Soundtrack year-end share: **6-14%** (14% at 1965), vs ~13-25% target — see §2. Singles chart is
  **unaffected** (soundtracks are albums; no tie-in singles built).

**Three new genres added** (enum 48-50; `GenreCatalog` profiles both formats; `GenreSegmentRouting`;
`GetAlbumAffinityBaseline`; `Validate()` count 42→45):
- **PsychedelicPop** (emerges 1966; Pet Sounds/Donovan), **PopRock** (1967; Neil Diamond/3DN/Beatles),
  **RootsRock** (1968; CCR/Band/Dylan). Name generation left on default templates (intentional).
  They emerge with correct timing; PopRock reaches ~4.6% album / RootsRock ~3% album by 1969; both
  are still small on singles (1-2 slots) — see §4.

**Album genre affinity recal** (`MarketRegion.GetAlbumAffinityBaseline` — the direct album-chart lever;
album presence tracks units, there is no airplay channel on albums):
- Jazz 0.72→**0.35**, Country 0.58→**0.42**, Comedy 0.40→**0.68**, Soul 0.30→**0.55**.

**Singles (light touch only):** Comedy `SingleOrientation` .15→.22 (did NOT restore comedy singles —
see §3; needs a stronger lever).

## 1. THE LEVERS (where each knob lives)
- **Album genre balance:** `MarketRegion.GetAlbumAffinityBaseline(genre)` — per-genre album propensity,
  shaped by a genre-blind multiplicative era boost (`ShapeAlbumAffinity`). This is FLAT per genre (no
  year keyframes), so a rising/falling album shape must come from the genre's `GenreCatalog` baseline
  (which IS 7-year keyframed) × this affinity. Album share ≈ baseline² × affinity (roughly).
- **Singles genre balance:** `GenreCatalog.Add(...)` baseline keyframes (1960/62/64/66/67/68/69, each
  in [0,1]) + `SingleOrientation` (single vs album split) + the per-genre `RadioAcceptance` multiplier
  (the airplay chart-efficiency lever, amplified ~^5). **Transfer is quadratic**: size every change by
  `sqrt(target/current)`, not the naive ratio (documented at length atop `GenreCatalog.cs`).
- **Soundtracks:** origination rate `ExternalMediaService.OriginationsPerYear`; demand shape
  `AlbumSimulator.GetSoundtrackBoxOfficeMultiplier`; buyer pool `MarketRegion.GetSoundtrackAlbumMarketSize`.
- **Album vs single pools are SEPARATE demand calcs** — raising a genre's album affinity does NOT
  reduce its singles (this bit Soul: album fixed, singles still over — §3).

## 2. ALBUM chart genre — model vs author estimate (seed 1001, share of album chart-weeks %)
Author est is shape-only (handoff §1 table); `.` = not given.

| genre | 1960 | 1962 | 1964 | 1966 | 1968 | 1969 | est(60→69) | status |
|---|--:|--:|--:|--:|--:|--:|---|---|
| Soundtrack(fmt) | 6.0 | 4.6 | 8.2 | 8.5 | 3.2 | 2.6 | 13→6 | under early+late; 1962 dip |
| TraditionalPop | 25.8 | 41.2 | 29.2 | 23.0 | 14.7 | 11.4 | 36→7 | **over mid** (41 at '62 vs ~30) |
| Classical | 23.3 | 6.1 | 4.4 | 4.6 | 4.2 | 2.3 | (~0-4) | **over 1960** (23!) |
| EasyListening | 19.8 | 13.7 | 12.7 | 12.7 | 10.5 | 9.3 | 8→14 | **inverted** (over early, under late) |
| PsychedelicRock | 0 | 0 | 0 | 6.3 | 17.6 | 10.5 | (~right) | ok shape |
| Comedy | 6.2 | 4.5 | 5.8 | 4.8 | 2.3 | 0.9 | 8→1 | ✅ close (slightly under '60) |
| Country | 2.6 | 7.0 | 5.7 | 7.6 | 10.0 | 9.8 | 2→8 | ✅ close (slightly over mid) |
| Soul | 0 | 0.5 | 2.3 | 4.9 | 8.2 | 7.3 | 0→22 | **still under late** (7 vs 22) |
| Jazz | 7.5 | 6.0 | 2.1 | 3.2 | 2.8 | 1.7 | 2→1 | ✅ big fix; slightly over '60-'62 |
| FolkRock | 0 | 0 | 0.1 | 6.4 | 7.6 | 7.0 | — | — |
| PopRock | 0 | 0 | 0 | 0 | 0.9 | 4.6 | — | new, emerges 1968 |
| RootsRock | 0 | 0 | 0 | 0 | 0.7 | 2.0 | — | new, emerges 1968 |
| PsychedelicPop | 0 | 0 | 0 | 0.6→1.0(’67) | 0.2 | 0.1 | — | new, small mid-60s bump |

## 3. SINGLES chart genre — model vs handcount (seed 1001, year-end Hot 100 slot% vs
`SimTools/AdjustedHistoricalGenreShareTargets.csv`). Format `model/bench`. Only |gap|≥2.5 shown.

| genre | 1960 | 1962 | 1964 | 1966 | 1967 | 1968 | 1969 | issue |
|---|--|--|--|--|--|--|--|---|
| **Soul** | 2/8 | 14/10 | 18/12 | 26/16 | 31/18 | 29/18 | 32/18 | **under early, WAY over late** (headline) |
| Teen Pop | 17/13 | 27/12 | 10/8 | 4/4 | 1/3 | 1/2 | 0/1 | over early-mid |
| Traditional Pop | 14/15 | 24/15 | 12/11 | 14/8 | 6/7 | 7/6 | 3/6 | over mid, under late |
| Folk Rock | 0/0 | 0/0 | 0/0 | 15/5 | 14/6 | 14/5 | 8/4 | over from '66 |
| British Beat/Pop | 0 | 0 | 13-13/8-5 | 5-3 | — | — | — | over at invasion ('64-65) |
| Garage Rock | 0/0 | 0/0 | 3/0 | 9/3 | 4/2 | 0/1 | 1/1 | over mid-60s |
| Doo-Wop | 12/5 | 1/2 | 0/0 | — | — | — | — | over 1960 |
| Bubblegum | 0 | 0 | 0 | 0 | 0/0 | 3/2 | 11/4 | over 1969 |
| Easy Listening | 7/6 | 4/6 | 3/6 | 3/6 | 6/5 | 5/4 | 1/6 | slightly under mid/late |
| Jazz | 4/6 | 5/6 | 0/5 | 2/5 | 2/4 | 1/4 | 0/4 | **now UNDER** (was over early pre-pass) |
| Comedy | 0 all years | | | | | | 0 vs ~0.7 | **absent** — needs real lever |
| Blues | 0/3 all years | | | | | | | absent vs ~2-3 |

## 4. KNOWN REMAINING ISSUES (prioritized for the next session)

**Singles (the bulk of remaining work):**
1. **Soul singles over late** — the single biggest miss (+13 at 1966-69). Album affinity raise did
   nothing here (separate pool). Lever: lower Soul `RadioAcceptance` late and/or trim the late Soul
   baseline — but Soul is UNDER early on singles too (2 vs 8), so the fix is shape, not a flat cut.
   Note the tension with §2 (Soul album is UNDER late) — Soul needs to move single→album late, which
   `SingleOrientation` (flat) can't express alone. Consider a late-decade Soul single/album re-split.
2. **Jazz singles now under** — the album-affinity cut + field shift pushed jazz singles from over-early
   to under (0-2 vs 4-6 late). Small; may self-correct or need a gentle baseline hold.
3. **Comedy singles absent** — `SingleOrientation` .22 did not produce chartable comedy singles
   (it's <1 slot of 100). Needs a `RadioAcceptance` bump or a small baseline floor, not orientation.
4. **Emergent-rock over at peaks** — FolkRock (15 vs 5 '66), Garage (9-11 vs 1-3), British Beat/Pop
   (13-18 vs 5-8), TeenPop (27 vs 13 '61-62), Bubblegum (11 vs 4 '69). Pre-existing pattern; the three
   new genres are still too small to redistribute the late field much. sqrt-size the trims.
5. **Blues / Children's absent** on singles (0 vs ~2-3 / ~0.4). Minor.

**Album:**
6. **Soul album under late** (7 vs 22) — raise Soul album affinity further (0.55→~0.70) OR give it a
   genre-specific late boost (the flat affinity + baseline² only reached 7).
7. **EasyListening album inverted** (over early 20 vs 8, under late 9 vs 14/24) — needs an early trim +
   late lift. Flat affinity can't invert; the lift must come off the EL `GenreCatalog` baseline late
   (Alpert/Tijuana Brass 1966-68) with album affinity holding.
8. **Classical album over 1960** (23% vs ~0-4) — affinity 0.82 is the highest in the table; a 1960
   over-seed. Cut classical album affinity (0.82→~0.45) — same family of fix as Jazz.
9. **TraditionalPop album over mid** (41% at 1962 vs ~30) — partly Jazz's freed share landing here;
   re-check after Classical/Jazz trims.
10. **Jazz album slightly over early** (7.5 vs 2 at 1960) — a further small affinity cut if wanted.

**Soundtrack:**
11. **Late-decade fade + 1962 dip** — share drops to 0.5% at 1969 (vs ~6) and dips to 2-4.6% at 1962.
    Origination is flat; the late chart grows to 200 slots so share dilutes. Consider an era-rising
    origination rate or a late-decade origination bump. The 1962 ~25% author spike is unmet (only ~5%).
12. **Breadth 344** — just under the 350-500 target (was 374 pre-genre-pass); watch it.

## 5. HOW TO RUN / SCORE
- Godot: `/c/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/.../Godot_v4.7-stable_mono_win64_console.exe`
- Python (pandas): `/c/Users/grohl/AppData/Local/Programs/Python/Python314/python.exe`
- Build: `dotnet build "Label Man.sln" -v minimal`
- Full-telemetry decade (needed for album-chart + genre-decade-shape; NO `--calibration`):
  `--headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=522 --run=<name> --seed=1001
  --enable-genre-market-v2 --enable-artist-population-lifecycle --profile-performance`
- **Single seed is enough for iterating** (two seeds were near-identical); use a second seed only to
  confirm before locking a pass.
- Analysis scripts used this session live in the session scratchpad (album chart-life + soundtrack
  scorecard; album genre breakdown vs est; singles per-year vs `AdjustedHistoricalGenreShareTargets.csv`
  with `&`/`-`/parenthetical name normalization). Album genre = share of album chart-weeks by
  `primaryGenre`; measure **pure-genre** (exclude `albumFormat==Soundtrack`) when comparing to the
  pure-genre handcounts, and score Soundtrack as its own format line.
- Benchmarks: singles = `SimTools/AdjustedHistoricalGenreShareTargets.csv` (per-genre year shares,
  `enumNumber` maps to the `Genre` enum). Albums = author odt / `D7SoundtrackCastAlbumHandoff.md` §1
  (shape-only). The three new genres are in the album handcount but NOT the singles CSV.

## 6. WATCH-OUTS
- Handcounts are **pure-genre** releases; soundtracks are a SEPARATE format line. Do not treat a
  soundtrack mapped to Comedy/TradPop as filling those genre targets.
- `GenreCatalog.Validate()` pins the profile count (now 45) — bump it if you add/remove a genre.
- Transfer is quadratic AND field-dependent (adding/cutting one genre moves everyone else under the
  normalized 100%). Size by sqrt(target/current) and re-read both benchmarks after each pass.
- Album and single demand pools are independent — a genre can be fixed on one chart and wrong on the
  other (Soul is the live example).
