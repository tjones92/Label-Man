# D7 sim-runtime optimization + album LP calibration — session handoff

Opened August 5, 2026. Branch `d7-genre-decade-calibration`. Parent: `D7AlbumMarketLPRatioHandoff.md`.
Nothing committed this session; all changes are in the working tree (see §5). **Build is green.**

## 0. One-line brief

We started on the album LP:45 recalibration (task A/B from the parent handoff) but pivoted when the
author needed **faster calibration runs** (decade runs were ~90 min; you cannot calibrate at that
speed). The pivot produced the real wins: a **profiling breakthrough** (runtime is 100%
album-count-driven; album *project* handling is a non-factor at 0.1%), a **verified `--calibration`
telemetry mode**, and **two verified accidental-quadratic fixes**. The album LP calibration itself is
still open and left as WIP (see §4) — it is genuinely hard because 1960 is album-*supply*-bound.

## 1. THE PROFILING BREAKTHROUGH (the important part — read this first)

From the committed-era decade profile (`SimLogs/lp-cut2-decade-522-1001-performance-profile.csv`,
107 min wall). **Every subsystem scales with the active album count** (albums grew ×7.1 over the
decade, 2,629 → 18,670; singles stayed flat at ~2,500 → 3,562):

| phase | 1960 | 1969 | growth | implied complexity |
|---|---:|---:|---:|---|
| simulateWeek (the real sim) | 25s | 196s | ×7.7 | **O(n) — honest/linear** |
| captureWeek (audit telemetry) | 21s | 256s | ×12 | O(n^1.3) |
| bookSettlement | 3s | 201s | ×63 | O(n^2.1) |
| dailyTalentMarket | 0.7s | 83s | ×111 | O(n^2.4) |
| cullDeadRecords | 0.1s | 26.5s | ×232 | O(n^2.8) |
| populationLifecycle | 0.8s | 214s | ×265 | **O(n^2.9)** |
| **processDueAlbumProjects** | — | — | **4.5s total (0.1%)** | not a factor |

**Conclusions that reframe everything:**
- **Album *project* handling is 0.1% of runtime.** There is nothing there to refactor. (This
  overturned the initial hypothesis.)
- The runtime *is* the standing album population. `simulateWeek` is honestly linear. The other big
  phases are **accidentally super-linear** — nested scans that walk all records per artist/record.
- `inertAlbums` is 0 every year — the population is all valid/selling, no zombies to cull (confirms a
  prior finding). So you cannot reclaim runtime by retiring "weak" albums.

Profiler columns are per-year in `<run>-performance-profile.csv` (emitted with `--profile-performance`).

## 2. WHAT WAS BUILT — KEEP THESE (verified, in the tree)

### 2a. `--calibration` telemetry mode (`SimTools/ChartAuditRunner.cs`)
Sim runs at **full fidelity — nothing about the economy changes**. `CaptureWeek` emits only the
accumulations the calibration CSVs read; the ~13 per-week diagnostic `Write*` methods (which walk all
records/labels/rosters, 21% of decade wall) are suppressed. Implies `--lean-probe`/`--aggregate-only`.
- **Kept-always in CaptureWeek** (they feed calibration outputs): `WriteFormatMixRows` (LP unit share
  → `decadeAnnual.Single/Album.Units/Gross`), `CaptureRetirementCohortSnapshot` (active counts),
  `Accumulate{Concentration,GenreShape,YearEndHot100}`, the Over26/52 album loop, the `ChartingSingles`
  loop, and `ObserveRecord`/`ClosedTop40`. Decision share comes from the `OnReleaseStrategy` event
  (not in CaptureWeek), so it is automatically preserved.
- **VERIFIED byte-identical**: normal vs `--calibration` on seed 1001, 104 weeks — rollup calibration
  columns, `year-end-hot100.csv`, and `genre-decade-shape.csv` all matched exactly.
- **Speed**: 104wk 2m47s → 2m8s (~23%); the saving grows on a full decade because captureWeek scales
  ×12 with album count.

### 2b. Quadratic fix — record retirement (`Systems/ChartManager.cs`, `CullDeadRecords`/`RetireRecord`)
`RetireRecord` called `allRecords.Remove(record)` (List.Remove is O(N)) once per retired record →
O(R·N), the ~n^2.8 growth of `cullDeadRecords`. Fix: `RetireRecord` no longer removes; `CullDeadRecords`
batches into one `allRecords.RemoveAll(retiredSet.Contains)` (O(N)). Same records, same order.

### 2c. Quadratic fix — population lifecycle (`Systems/ArtistManager.cs`)
`ApplyLifecycleExits` and the terminal-exit paths called `HasLiveRecordOrPendingProject(artist)`, which
did `GetAllRecords().Any(r => r.artistId == artist.artistId ...)` — an O(records) scan **per candidate
artist** → O(candidates·records), the ~n^2.9 growth of `populationLifecycle`. Fix:
`RebuildLiveRecordArtistIndex()` builds a `HashSet<string> liveRecordArtistIds` once at the top of
`AdvancePopulationLifecycle` (allRecords is invariant during the lifecycle phase — records are culled in
a separate earlier phase), and `HasLiveRecordOrPendingProject` is now an O(1) lookup (made instance,
was static). The pending-project side (`HasPendingProjectForArtist`, a small-list `.Any`) was left
as-is.

- **2b + 2c VERIFIED byte-identical**: 104wk lean-probe with the fixes (`vN2`) matched the pre-fix
  run (`vN`) exactly on rollup, year-end-hot100, and genre-shape. The savings do **not** show at 104wk
  (albums only ~3,200 then) — they only bite at 18k albums in the late decade. **This has not yet been
  measured at scale — see §3.**

## 3. NEXT STEP THAT WAS QUEUED (not done): measure the quadratic fixes at scale
A profiled decade run was about to launch to (a) prove `populationLifecycle`/`cullDead` are now linear
and (b) get the headline wall-time. **Gotcha:** measure against the **committed economy** — the WIP
lever `priorUnitScalarAlbum = 55000` (§4) shrinks the album population, which shrinks the very
quadratics you're measuring and confounds the comparison to `lp-cut2`. Temporarily restore
`priorUnitScalarAlbum = 175000` (and ideally revert the other §4 WIP levers) before the measurement
run, or the numbers won't be comparable.

Command (profiled decade, calibration mode):
```
"$GODOT" --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=522 --run=optverify --seed=1001 \
  --enable-genre-market-v2 --enable-artist-population-lifecycle --calibration --profile-performance \
  > <logpath> 2>&1
```
Then compare `optverify-performance-profile.csv` per-year `populationLifecycleSeconds` /
`cullDeadRecordsSeconds` against `lp-cut2` at matched `activeAlbums`.

### Remaining quadratics NOT yet fixed
- **`dailyTalentMarket` (×111).** `RosterManager.ProcessDailyTalentMarket` → the `foreach label in due`
  loop calls `NominateFromDailySnapshot(label, ..., supplySnapshot, ...)` per label over the unsigned
  `supplySnapshot` → O(due·unsigned). Grows with label/artist count (correlates with albums over time
  but not a records scan). Investigate `NominateFromDailySnapshot` for redundant per-label filtering/
  sorting of the full snapshot.
- **`bookSettlement` (×63).** `CompetitorManager.ProcessWeeklyRevenue` loops labels ×
  `CalculateLabelRevenue`, but `settlement.EntriesForLabel` is **already indexed** (O(1) dict), so the
  per-label total is O(N), not O(labels·N). Source of the super-linearity is **not yet identified** —
  suspect a growing list scanned per week (e.g. `CollectMaturedWholesaleReceivables`, wholesale
  receivables, or `UpdateApplicableResponsiveMemoryObservations`). Needs a closer look.

## 4. WIP — album LP calibration (kept in tree, UNCONVERGED, author said keep)
These are exploratory album levers from before the runtime pivot. The author chose to keep them. They
do **change the economy**; revert to committed values (in comments) for a clean baseline if needed.

- `Systems/AlbumSimulator.cs`: `BasePurchaseRate` 0.045 → **0.080**. Sales-only lever (not the creation
  prior) that lifts early/1960 album units. Reached 1960 LP unit ~26.6%; **diminishing returns, still
  short of the 30% target.**
- `Systems/CompetitorManager.cs`: `priorUnitScalarAlbum` 175000 → **55000** (album decision share
  62%→33%, 1960 active albums 2750→1552); new `[Export]`s `albumPrewarmAwarenessFloor = 0.85`,
  `albumPrewarmStockMultiplier = 1.0`; **task-B album seeding** in `PopulateInitialRecords` (genre-
  affinity-gated album/single split of the 1960 catalog, kept out of `releasedSingleIds`) + album
  awareness-floor/stock handling in `BootstrapPrewarmRecord`.
- `Data/MarketRegion.cs`: `GetAlbumSeedAffinity` accessor added; `EasyListening` album affinity
  0.88 → **0.65** (§4 of parent — keep EL on the singles chart).

### The core tension that stalled LP calibration (KEY FINDINGS — do not relearn)
- **Album creation is margin-driven, NOT demand-driven.** `projectedAlbumNet` ran ~2.2× `projectedSingleNet`
  (an LP is ~4.5× the price), so 62% of releases were albums. The demand levers (`BasePurchaseRate`,
  willingness, `eraBoost`) **cannot** reduce album titles — the earlier "demand pullback" bought zero
  runtime. `priorUnitScalarAlbum` (scales the album *prior's* expected units, not sales) is the clean
  creation/title lever.
- **1960 is album-supply/demand-bound BELOW the channel — not channel-bound.** Evidence: cutting album
  supply lowered 1960 LP share (20.3%), and raising the channel did NOT lift it, while seeding was
  nearly inert. So more 1960 album *units* need more album *titles* (or higher per-title `BasePurchaseRate`,
  which hits a ~27% ceiling on few titles). **This conflicts directly with cutting titles for runtime.**
  Late years ARE channel-bound (intent ~2× channel), so surplus late albums don't affect LP share.
- Net: hitting 30% at 1960 needs album supply; low album count needs few titles. They trade off. The
  author declined cutting live albums (economy-fidelity concern) — which is *why* the session pivoted
  to making big runs cheap instead. The runtime work (§2) is the enabler; the LP calibration resumes
  once decade runs are fast.

## 5. Working-tree state (all uncommitted)
```
Data/MarketRegion.cs          (WIP §4: EL 0.65, GetAlbumSeedAffinity)
Systems/AlbumSimulator.cs     (WIP §4: BasePurchaseRate 0.080)
Systems/CompetitorManager.cs  (WIP §4: priorUnitScalar 55000, seeding, prewarm floors)
Systems/ChartManager.cs       (KEEP §2b: cull batch-removal)
Systems/ArtistManager.cs      (KEEP §2c: population live-record index)
SimTools/ChartAuditRunner.cs  (KEEP §2a: --calibration mode)
```
Suggested commit split: one commit for §2 (runtime: calibration mode + quadratic fixes, all verified),
a separate WIP commit or stash for §4 (album LP levers).

## 6. How to run / score (unchanged from parent §5)
- Godot: `/c/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe`
- Python (pandas): `/c/Users/grohl/AppData/Local/Programs/Python/Python314/python.exe`
- Build: `dotnet build "Label Man.sln" -v minimal`
- Fast calibration run: add `--calibration` (implies lean/aggregate). Probes still use the two
  `--*-probes` flags (parent §5); calibration mode is for scoring runs, not probe runs.
- Scoring: `<run>-decade-annual-rollup.csv` (`albumUnits/(single+album)` = LP unit share;
  `albumDecisionShare`, `activeAlbums`), `<run>-year-end-hot100.csv` (`yearEndSlots`, decade-summed vs
  the hand count in the parent handoff §6, RockAndRoll excluded), `<run>-release-strategy.csv`
  (per-decision album/single economics).

## 7. Do not redo
- Do not try to cut album *titles* with demand levers (`BasePurchaseRate`/willingness/`eraBoost`) —
  creation is margin-driven; only `priorUnitScalarAlbum` moves it.
- Do not raise the album channel to fix 1960 — 1960 is supply-bound below the channel, not capped by it.
- Do not "optimize album project handling" — it is 0.1% of runtime.
- Do not gate `WriteFormatMixRows` or `CaptureRetirementCohortSnapshot` under `--calibration` — they
  feed the rollup. (They are deliberately kept-always.)
- Do not measure the quadratic fixes with `priorUnitScalarAlbum = 55000` in place — it shrinks the
  population you're measuring.
